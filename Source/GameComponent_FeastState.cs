using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimFeast.AI;

namespace RimFeast
{
	// un banquet suivi de l'invitation au bilan
	public class FeastCase : IExposable
	{
		public const int Invited = 0;   // messager parti, cortege pas encore la
		public const int Traveling = 1; // cortege sur la carte, en route vers la salle
		public const int Feasting = 2;  // a table
		public const int Fleeing = 3;   // trahis par l'hote, ils fuient. bilan quand tout le monde est sorti
		public const int Leaving = 4;   // banquet fini, ils marchent vers la sortie. encore sous notre toit

		public int id;
		public Map map;
		public Faction house;
		public IntVec3 spotCell = IntVec3.Invalid;
		public Thing spotThing;
		public int state;
		public int eventTick = -1;
		public Lord lord;
		public Lord colonistLord;
		public int arrivedTick = -1;
		public int lastSummonTick = -1;
		public int guestCount;
		public int toastsGiven;
		public int samples;
		public int musicSamples;

		public float maxImpressiveness;
		public int bestFoodPref;
		public float maxTableScore;
		public int maxVariety;
		public int maxDrinkUnits;
		public bool drankAny;
		public bool brawled;
		public int brawlTick = -1;
		public Pawn brawlerA;
		public Pawn brawlerB;
		public bool proposalDone;
		public Pawn betrothedNoble;
		public Pawn betrothedColonist;

		// la faveur demandee a table. requestRolled = le tirage a eu lieu, qu'il ait donne
		// quelque chose ou non: on ne redemande pas toutes les 250 ticks
		public bool requestRolled;
		public int requestKind;
		public Thing requestThing;   // l'arme ou l'oeuvre convoitee, selon requestKind
		public Pawn requestChild;    // le page qu'on reclame
		public Pawn requestSpeaker;  // celui qui s'est leve pour demander, donc la premiere gorge
		public Pawn requestTarget;   // celle a qui il fait les yeux doux
		public Faction requestRival;
		public List<Pawn> guests = new List<Pawn>();
		public bool leaderCame;

		// instruments de la salle. transient, rafraichi de loin en loin: assez rare pour ne
		// pas rescanner tous les batiments a chaque passe, assez frequent pour voir une
		// harpe construite en cours de banquet
		public List<Building_MusicalInstrument> hallInstruments;
		public int instrumentsTick = -1;
		public bool planned; // noces pourpres premeditees: debloque l'ordre d'assassinat
		public bool slaughterOrdered; // le signal est donne: au premier sang, tout le monde attaque
		public bool musicStarted;
		public int fleeStartTick = -1;
		public bool treacherous;
		public Pawn poisoner;
		public bool tainted;
		public int taintTick = -1;
		public bool exposed;
		public int sickBaseline;

		public void ExposeData()
		{
			Scribe_Values.Look(ref id, "id");
			Scribe_References.Look(ref map, "map");
			Scribe_References.Look(ref house, "house");
			Scribe_Values.Look(ref spotCell, "spotCell", IntVec3.Invalid);
			Scribe_References.Look(ref spotThing, "spotThing");
			Scribe_Values.Look(ref state, "state");
			Scribe_Values.Look(ref eventTick, "eventTick", -1);
			Scribe_References.Look(ref lord, "lord");
			Scribe_References.Look(ref colonistLord, "colonistLord");
			Scribe_Values.Look(ref arrivedTick, "arrivedTick", -1);
			Scribe_Values.Look(ref lastSummonTick, "lastSummonTick", -1);
			Scribe_Values.Look(ref guestCount, "guestCount");
			Scribe_Values.Look(ref toastsGiven, "toastsGiven");
			Scribe_Values.Look(ref samples, "samples");
			Scribe_Values.Look(ref musicSamples, "musicSamples");
			Scribe_Values.Look(ref maxImpressiveness, "maxImpressiveness");
			Scribe_Values.Look(ref bestFoodPref, "bestFoodPref");
			Scribe_Values.Look(ref maxTableScore, "maxTableScore");
			Scribe_Values.Look(ref maxVariety, "maxVariety");
			Scribe_Values.Look(ref maxDrinkUnits, "maxDrinkUnits");
			Scribe_Values.Look(ref drankAny, "drankAny");
			Scribe_Values.Look(ref brawled, "brawled");
			Scribe_Values.Look(ref brawlTick, "brawlTick", -1);
			Scribe_References.Look(ref brawlerA, "brawlerA");
			Scribe_References.Look(ref brawlerB, "brawlerB");
			Scribe_Values.Look(ref proposalDone, "proposalDone");
			Scribe_References.Look(ref betrothedNoble, "betrothedNoble");
			Scribe_References.Look(ref betrothedColonist, "betrothedColonist");
			Scribe_Values.Look(ref requestRolled, "requestRolled");
			Scribe_Values.Look(ref requestKind, "requestKind");
			Scribe_References.Look(ref requestThing, "requestThing");
			Scribe_References.Look(ref requestChild, "requestChild");
			Scribe_References.Look(ref requestSpeaker, "requestSpeaker");
			Scribe_References.Look(ref requestTarget, "requestTarget");
			Scribe_References.Look(ref requestRival, "requestRival");
			Scribe_Collections.Look(ref guests, "guests", LookMode.Reference);
			Scribe_Values.Look(ref leaderCame, "leaderCame");
			Scribe_Values.Look(ref planned, "planned");
			Scribe_Values.Look(ref slaughterOrdered, "slaughterOrdered");
			Scribe_Values.Look(ref fleeStartTick, "fleeStartTick", -1);
			Scribe_Values.Look(ref treacherous, "treacherous");
			Scribe_References.Look(ref poisoner, "poisoner");
			Scribe_Values.Look(ref tainted, "tainted");
			Scribe_Values.Look(ref taintTick, "taintTick", -1);
			Scribe_Values.Look(ref exposed, "exposed");
			Scribe_Values.Look(ref sickBaseline, "sickBaseline");
			if (Scribe.mode == LoadSaveMode.PostLoadInit && guests == null)
				guests = new List<Pawn>();
		}
	}

	// une maison qui a deja vu ta salle est moins facile a impressionner, et se fait prier
	public class HouseFeastMemory : IExposable
	{
		public Faction house;
		public int timesFeasted;
		public int lastFeastTick = -1;
		public bool betrayed; // noces pourpres: plus jamais d'invitation acceptee

		public void ExposeData()
		{
			Scribe_References.Look(ref house, "house");
			Scribe_Values.Look(ref timesFeasted, "timesFeasted");
			Scribe_Values.Look(ref lastFeastTick, "lastFeastTick", -1);
			Scribe_Values.Look(ref betrayed, "betrayed");
		}
	}

	// verite differee: le poison a fait effet apres le depart du cortege
	public class PendingReveal : IExposable
	{
		public Faction house;
		public Map map;
		public int fireTick;

		public void ExposeData()
		{
			Scribe_References.Look(ref house, "house");
			Scribe_References.Look(ref map, "map");
			Scribe_Values.Look(ref fireTick, "fireTick");
		}
	}

	// expedition punitive promise apres des noces pourpres
	public class PendingRaid : IExposable
	{
		public Faction house;
		public Map map;
		public int fireTick;
		public float points;
		public int tries;

		public void ExposeData()
		{
			Scribe_References.Look(ref house, "house");
			Scribe_References.Look(ref map, "map");
			Scribe_Values.Look(ref fireTick, "fireTick");
			Scribe_Values.Look(ref points, "points");
			Scribe_Values.Look(ref tries, "tries");
		}
	}

	// un enfant parti se former dans une maison noble. il vit dans le monde pendant tout ce
	// temps, epingle dans ForcefullyKeptPawns pour que le ramasse-miettes n'y touche pas
	public class PendingWard : IExposable
	{
		public Pawn ward;
		public Faction house;
		public Map map;
		public int leftTick = -1;
		public int returnTick;

		public void ExposeData()
		{
			Scribe_References.Look(ref ward, "ward");
			Scribe_References.Look(ref house, "house");
			Scribe_References.Look(ref map, "map");
			Scribe_Values.Look(ref leftTick, "leftTick", -1);
			Scribe_Values.Look(ref returnTick, "returnTick");
		}
	}

	// caravane promise par une maison apres un bon banquet. traderKind null = leur choix
	public class PendingCaravan : IExposable
	{
		public int id;
		public Faction house;
		public Map map;
		public TraderKindDef traderKind;
		public int fireTick;
		public int tries;

		public void ExposeData()
		{
			Scribe_Values.Look(ref id, "id");
			Scribe_References.Look(ref house, "house");
			Scribe_References.Look(ref map, "map");
			Scribe_Defs.Look(ref traderKind, "traderKind");
			Scribe_Values.Look(ref fireTick, "fireTick");
			Scribe_Values.Look(ref tries, "tries");
		}
	}

	public class GameComponent_FeastState : GameComponent
	{
		private FeastCase current;
		private List<HouseFeastMemory> memories = new List<HouseFeastMemory>();
		private List<PendingCaravan> caravans = new List<PendingCaravan>();
		private List<PendingRaid> raids = new List<PendingRaid>();
		private List<PendingReveal> reveals = new List<PendingReveal>();
		private List<PendingWard> wards = new List<PendingWard>();
		private int nextId = 1;
		private int redWeddingTick = -1;
		private int nextAwayInviteTick = -1;

		// l'invitation en cours chez eux. pas d'objet sur la carte du monde: on se rend a
		// leur vraie colonie, le comp greffe dessus ouvre l'option dans le menu de caravane
		private Faction awayHouse;
		private int awayExpireTick = -1;

		// debug: force la venue du chef de maison, la traitrise ou la premeditation
		public bool debugForceLeader;
		public bool debugForceTreachery;
		public bool debugForcePlanned;

		// la chanson jouee depuis l'instrument de la salle. transient: apres un chargement
		// en plein massacre, la musique ne reprend pas, tant pis
		private Sustainer redWeddingSound;

		private const int CheckInterval = 250;
		private const int BrawlGraceTicks = 5000;
		private const int BrawlBreakupTicks = 2500;

		// reglages joueur. le bareme du score reste interne: c'est la regle du jeu, pas un bouton
		public static int BaseInviteCost => RimFeastMod.S.inviteCost;
		public static int FeastDurationTicks => RimFeastMod.S.feastHours * 2500;
		private static int GiftSilver => RimFeastMod.S.giftSilver;

		// au bout de N jours sans banquet, la maison oublie une soiree
		private static int FadeTicks => RimFeastMod.S.fadeDays * 60000;

		private const int CaravanRetryTicks = 15000;
		private const int CaravanMaxTries = 8;

		// en dessous, une invitation peut etre acceptee avec de mauvaises intentions
		// des fuyards mures ne videront jamais le lord: sans ce filet, current reste occupe
		// pour toujours et le joueur ne peut plus jamais organiser de banquet
		private const int FleeBackstopTicks = 60000;

		private static int TreacheryGoodwillMax => RimFeastMod.S.treacheryGoodwillMax;
		private static float TreacheryChance =>
			RimFeastMod.S.treacheryEnabled ? RimFeastMod.S.treacheryChance : 0f;
		private const int TaintDelayTicks = 15000;

		public const int ToastDurationTicks = 1500;

		// l'intervalle suit la duree du banquet. a 8h on retombe pile sur les 5000 ticks
		// d'origine, le reglage par defaut ne bouge pas
		public static int ToastIntervalTicks => Mathf.Clamp(FeastDurationTicks / 4, 1500, 10000);

		// on n'exige que ce qui tient dans la soiree: avec un banquet de 2h, trois toasts
		// sont physiquement impossibles et le pilier restait plafonne au tiers quoi que
		// fasse le joueur, sans que rien ne le lui dise
		private static int ToastsForFull =>
			Mathf.Clamp(FeastDurationTicks / (ToastIntervalTicks + ToastDurationTicks), 1, 3);

		private const int TriumphScore = 85;
		private const int GoodScore = 60;
		private const int DecentScore = 35;

		// 150 = entre "extremely" et "unbelievably impressive" sur l'échelle vanilla des pièces
		private const float ImpressivenessCap = 150f;

		// une part = un repas vanilla. on compte en nutrition, pas en piles: le miel MO est
		// "fin" a 0.05 de nutrition, une pile passerait pour un festin
		private const float ServingNutrition = 0.9f;
		private static readonly float[] TierValue = { 20f, 14f, 8f, 3f };

		public GameComponent_FeastState(Game game) { }

		public static GameComponent_FeastState Get() => Current.Game?.GetComponent<GameComponent_FeastState>();

		public bool Busy => current != null;

		public FeastCase CurrentCase => current;

		// le signal du massacre est donne: plus rien de ce qui arrive aux convives n'est un accident
		public bool SlaughterUnderway => current != null && current.slaughterOrdered;

		// bascule symetrique. SetRelationDirect ecrit un sens, notifie (gros traitement qui
		// peut jeter en plein vol, et nos postActions avalent les exceptions), puis ecrit
		// l'autre sens: en cas d'accroc la relation reste boiteuse, hostile dans un sens et
		// neutre dans l'autre, et la logique de fuite des colons spamme des erreurs
		private static void SetMutualRelation(Faction a, Faction b, FactionRelationKind kind)
		{
			if (a == null || b == null) return;

			// on ecrit le kind a la main, donc il ne suit pas le goodwill. or capturer un
			// convive cloue celui de la faction cachee au plancher, et le premier ajustement
			// venu le relit (CheckKindThresholds) et rebascule tout hostile en plein banquet.
			// on remet le compteur a zero avec le kind. jamais sur une vraie maison
			bool puppet = a.def == RimFeastDefOf.RimFeast_Guests || b.def == RimFeastDefOf.RimFeast_Guests;
			if (puppet && kind == FactionRelationKind.Neutral)
			{
				a.RelationWith(b).baseGoodwill = 0;
				b.RelationWith(a).baseGoodwill = 0;
			}

			// les deux sens doivent etre lus: une relation boiteuse laissee par une vieille
			// version se serait vue "deja neutre" d'un cote et jamais reparee de l'autre
			if (a.RelationKindWith(b) == kind && b.RelationKindWith(a) == kind) return;
			FactionRelationKind prev = a.RelationKindWith(b);
			a.RelationWith(b).kind = kind;
			b.RelationWith(a).kind = kind;
			try { a.Notify_RelationKindChanged(b, prev, false, null, GlobalTargetInfo.Invalid, out _); }
			catch (System.Exception e) { Log.Warning("RimFeast: relation notify: " + e.Message); }
			try { b.Notify_RelationKindChanged(a, prev, false, null, GlobalTargetInfo.Invalid, out _); }
			catch (System.Exception e) { Log.Warning("RimFeast: relation notify: " + e.Message); }
			// la notif rafraichit le cache des cibles, mais elle a pu echouer avant d'y arriver
			foreach (Map m in Find.Maps)
				m.attackTargetsCache.Notify_FactionHostilityChanged(a, b);
		}

		private HouseFeastMemory MemoryFor(Faction f, bool create)
		{
			HouseFeastMemory m = memories.FirstOrDefault(x => x.house == f);
			if (m == null && create && f != null)
			{
				m = new HouseFeastMemory { house = f };
				memories.Add(m);
			}
			return m;
		}

		// banquets dont la maison se souvient encore
		public int FeastsRemembered(Faction f)
		{
			HouseFeastMemory m = MemoryFor(f, false);
			if (m == null || m.timesFeasted <= 0) return 0;
			if (m.lastFeastTick < 0) return m.timesFeasted;
			int forgotten = (Find.TickManager.TicksGame - m.lastFeastTick) / FadeTicks;
			return Mathf.Max(0, m.timesFeasted - forgotten);
		}

		// il faut de plus beaux presents pour faire revenir des gens qui ont deja tout vu
		public int InviteCostFor(Faction f) =>
			Mathf.RoundToInt(BaseInviteCost * (1f + 0.5f * FeastsRemembered(f)));

		private float GoodwillFactorFor(Faction f) => 1f / (1f + 0.6f * FeastsRemembered(f));

		public bool PactPending(int pactId) => caravans.Any(x => x.id == pactId);

		public void SetPactKind(int pactId, TraderKindDef kind)
		{
			PendingCaravan p = caravans.FirstOrDefault(x => x.id == pactId);
			if (p == null) return;
			p.traderKind = kind;
			Messages.Message("RimFeast_MessagePactSet".Translate(p.house?.Name, kind.label),
				MessageTypeDefOf.PositiveEvent, historical: false);
		}

		private PendingCaravan ScheduleCaravan(Faction house, Map map)
		{
			var p = new PendingCaravan
			{
				id = nextId++,
				house = house,
				map = map,
				traderKind = null,
				fireTick = Find.TickManager.TicksGame + Rand.Range(60000, 180000),
			};
			caravans.Add(p);
			return p;
		}

		private void TickCaravans()
		{
			for (int i = caravans.Count - 1; i >= 0; i--)
			{
				PendingCaravan p = caravans[i];
				if (Find.TickManager.TicksGame < p.fireTick) continue;

				if (p.house == null || p.house.defeated || p.house.HostileTo(Faction.OfPlayer)
					|| p.map == null || !Find.Maps.Contains(p.map))
				{
					caravans.RemoveAt(i);
					continue;
				}

				// forced saute le filtrage du conteur (c'est une recompense, pas un evenement),
				// mais CanFireNow garde le veto CanFireNowSub sur un lord hostile qui bloque
				var parms = new IncidentParms
				{
					target = p.map,
					faction = p.house,
					traderKind = p.traderKind,
					forced = true,
				};
				IncidentWorker worker = IncidentDefOf.TraderCaravanArrival.Worker;
				if (worker.CanFireNow(parms) && worker.TryExecute(parms))
				{
					caravans.RemoveAt(i);
					continue;
				}

				p.tries++;
				if (p.tries >= CaravanMaxTries)
				{
					Messages.Message("RimFeast_MessagePactLapsed".Translate(p.house.Name),
						MessageTypeDefOf.NeutralEvent);
					caravans.RemoveAt(i);
				}
				else p.fireTick = Find.TickManager.TicksGame + CaravanRetryTicks;
			}
		}

		// outil debug: force une demande en mariage pendant un festin en cours
		public void DebugForceProposal()
		{
			FeastCase c = current;
			if (c == null || c.state != FeastCase.Feasting || c.lord == null)
			{
				Messages.Message("RimFeast: no feast at the table right now.",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			if (c.map.mapPawns.FreeColonistsSpawnedCount < 2)
			{
				Messages.Message("RimFeast: need at least two colonists at the feast.",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			if (!FeastUtility.TryFindMarriagePair(c.lord, out Pawn noble, out Pawn colonist))
			{
				Messages.Message("RimFeast: no eligible noble/colonist pair found.",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			c.proposalDone = true;
			c.betrothedNoble = noble;
			c.betrothedColonist = colonist;
			var letter = (ChoiceLetter_MarriageProposal)LetterMaker.MakeLetter(
				"RimFeast_LabelProposal".Translate(),
				"RimFeast_TextProposal".Translate(noble.LabelShortCap, c.house?.Name, colonist.LabelShortCap),
				RimFeastDefOf.RimFeast_MarriageProposal, new LookTargets(new[] { noble, colonist }));
			letter.caseId = c.id;
			letter.StartTimeout(Mathf.Max(2500, c.arrivedTick + FeastDurationTicks - Find.TickManager.TicksGame));
			Find.LetterStack.ReceiveLetter(letter);
		}

		public void DebugRushCaravans()
		{
			if (caravans.Count == 0 && raids.Count == 0)
			{
				Messages.Message("RimFeast: nothing pending.", MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			foreach (PendingCaravan p in caravans) p.fireTick = Find.TickManager.TicksGame;
			foreach (PendingRaid r in raids) r.fireTick = Find.TickManager.TicksGame;
		}

		private static string Qual(float v, string prefix) =>
			(prefix + (v >= 0.66f ? "High" : v >= 0.33f ? "Mid" : "Low")).Translate();

		// ce que le banquet donne a voir, sans chiffres: le joueur apprend le systeme en
		// regardant sa salle, pas en lisant un tableur. lit les valeurs deja echantillonnees
		public static string HallReport(FeastCase c)
		{
			float hall = Mathf.Clamp01(c.maxImpressiveness / ImpressivenessCap);
			float table = Mathf.Clamp01(c.maxTableScore / 20f);
			float drink = c.guestCount > 0
				? Mathf.Min((float)c.maxDrinkUnits / c.guestCount, 1f) : 0f;

			float ent = Mathf.Min(c.toastsGiven / (float)ToastsForFull, 1f);
			if (c.samples > 0 && c.musicSamples > 0) ent = Mathf.Max(ent, 0.7f);

			return Qual(hall, "RimFeast_QualHall") + " " + Qual(table, "RimFeast_QualTable")
				+ " " + Qual(drink, "RimFeast_QualDrink") + " " + Qual(ent, "RimFeast_QualEnt");
		}

		// le meme bareme, lu en direct pendant l'attente: la journee doit servir a corriger
		// la table, pas a la decouvrir une fois le cortege assis. calcule sur le cortege
		// attendu, sans l'alea du tirage
		public static string HallPreview(FeastCase c)
		{
			if (c?.map == null || c.house == null) return null;
			Map map = c.map;
			Room room = c.spotCell.GetRoom(map);
			float hall = room != null && !room.PsychologicallyOutdoors
				? room.GetStat(RoomStatDefOf.Impressiveness) : 0f;

			int expected = Mathf.Max(2, RimFeastMod.S.corteges
				+ Mathf.RoundToInt(Mathf.Clamp(c.house.PlayerGoodwill, -100f, 100f) / 50f));

			var tierNutrition = new float[TierValue.Length];
			int drinkUnits = 0;
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(c.spotCell, map);
			List<Thing> foods = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);
			for (int i = 0; i < foods.Count; i++)
			{
				Thing t = foods[i];
				if (!t.Spawned || !t.IngestibleNow) continue;
				if (!area.Contains(t.Position)) continue;
				if (t.def.IsDrug)
				{
					if (t.def.ingestible?.drugCategory == DrugCategory.Social) drinkUnits += t.stackCount;
				}
				else if ((int)t.def.ingestible.preferability > (int)FoodPreferability.RawBad)
					tierNutrition[TierIndexOf(t.def.ingestible.preferability)] +=
						t.stackCount * t.def.ingestible.CachedNutrition;
			}

			return "RimFeast_PreviewHeader".Translate(expected) + "\n"
				+ Qual(Mathf.Clamp01(hall / ImpressivenessCap), "RimFeast_QualHall")
				+ " " + Qual(Mathf.Clamp01(TableScore(tierNutrition, expected) / 20f), "RimFeast_QualTable")
				+ " " + Qual(Mathf.Min((float)drinkUnits / expected, 1f), "RimFeast_QualDrink");
		}

		// rappeler le messager tant que le cortege n'est pas en route. les presents, eux,
		// sont deja partis
		public void CancelInvite(Thing spot)
		{
			FeastCase c = current;
			if (c == null || c.state != FeastCase.Invited || c.spotThing != spot) return;
			Messages.Message("RimFeast_MessageInviteCancelled".Translate(c.house?.Name),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.NeutralEvent, historical: false);
			current = null;
		}

		// les pupilles au loin, lisibles sur le marqueur plutot que dans un menu debug
		public string WardsInspect()
		{
			if (wards.Count == 0) return null;
			var sb = new StringBuilder();
			for (int i = 0; i < wards.Count; i++)
			{
				PendingWard w = wards[i];
				if (w?.ward == null) continue;
				if (sb.Length > 0) sb.AppendLine();
				int left = Mathf.Max(0, w.returnTick - Find.TickManager.TicksGame);
				TaggedString period = left >= GenDate.TicksPerYear
					? "RimFeast_PeriodYears".Translate((left / (float)GenDate.TicksPerYear).ToString("0.0"))
					: "RimFeast_PeriodDays".Translate(Mathf.CeilToInt(left / 60000f));
				sb.Append("RimFeast_WardAwayLine".Translate(
					w.ward.LabelShortCap, w.house?.Name ?? "?", period));
			}
			return sb.Length == 0 ? null : sb.ToString();
		}

		// outil debug: pourquoi les colons ne viennent-ils pas a table
		public string DebugColonistReport()
		{
			var sb = new StringBuilder();
			sb.AppendLine("RimFeast colonist diagnostic:");
			sb.AppendLine("  feast duration ticks: " + FeastDurationTicks);
			FeastCase c = current;
			if (c == null) { sb.AppendLine("  NO FEAST CASE (nothing running)"); return sb.ToString(); }

			sb.AppendLine("  state=" + c.state + " planned=" + c.planned + " guests=" + c.guestCount);
			sb.AppendLine("  map danger: " + c.map.dangerWatcher.DangerRating
				+ "  (High blocks any vanilla gathering)");
			Faction gf = FeastUtility.GuestFaction();
			sb.AppendLine("  guests->player: " + gf?.RelationKindWith(Faction.OfPlayer)
				+ "   player->guests: " + Faction.OfPlayer.RelationKindWith(gf));

			bool lordAlive = c.colonistLord != null && c.map.lordManager.lords.Contains(c.colonistLord);
			sb.AppendLine("  colonist lord: " + (c.colonistLord == null ? "NEVER CREATED"
				: lordAlive ? c.colonistLord.ownedPawns.Count + " pawns, toil="
					+ c.colonistLord.CurLordToil?.GetType().Name
				: "ENDED/REMOVED"));

			sb.AppendLine("  last summon " + (Find.TickManager.TicksGame - c.lastSummonTick)
				+ " ticks ago (resummon every " + ResummonTicks + ")");

			var job = lordAlive ? c.colonistLord.LordJob as LordJob_VoluntarilyJoinable : null;
			foreach (Pawn p in c.map.mapPawns.FreeColonistsSpawned)
			{
				// pourquoi celui-la n'est pas a table: tout ce que SummonColonists regarde,
				// plus l'etat de sommeil que l'arbre de decision lit avant tout le reste
				sb.AppendLine("   " + p.LabelShort.PadRight(14)
					+ " prio=" + (job != null ? job.VoluntaryJoinPriorityFor(p).ToString("0.##") : "n/a")
					+ " lord=" + (p.GetLord()?.LordJob?.GetType().Name ?? "none")
					+ " awake=" + p.Awake()
					+ " rest=" + (p.needs?.rest?.CurLevel.ToString("0.00") ?? "n/a")
					+ " laying=" + p.GetPosture().Laying()
					+ " timetable=" + (p.timetable?.CurrentAssignment?.defName ?? "n/a")
					+ " job=" + (p.CurJobDef?.defName ?? "none")
					+ " interruptible=" + (p.CurJob?.def.casualInterruptible.ToString() ?? "n/a")
					+ " drafted=" + p.Drafted
					+ " keepGathering=" + GatheringsUtility.ShouldPawnKeepGathering(p, RimFeastDefOf.RimFeast_Feast)
					+ " canGather=" + GatheringsUtility.PawnCanStartOrContinueGathering(p)
					+ " spotForbidden=" + c.spotCell.IsForbidden(p));
			}
			return sb.ToString();
		}

		// outil debug: saute l'attente du messager, ou termine le festin tout de suite
		public void DebugRush()
		{
			if (current == null) return;
			if (current.state == FeastCase.Invited)
				current.eventTick = Find.TickManager.TicksGame;
			else if (current.state == FeastCase.Feasting)
				current.lord?.ReceiveMemo(LordJob_FeastGuests.MemoEndFeast);
		}

		// un cortege qui debarque a 3h du matin trouve la colonie au lit et personne ne
		// vient a table. on decale l'arrivee sur la fin d'apres-midi
		private static int ArrivalTick(Map map, int delay)
		{
			int t = Find.TickManager.TicksGame + delay;
			float longitude = Find.WorldGrid.LongLatOf(map.Tile).x;
			int offset = Find.TickManager.TicksAbs - Find.TickManager.TicksGame;
			for (int i = 0; i < 48; i++)
			{
				int hour = GenDate.HourOfDay(t + offset, longitude);
				if (hour >= 16 && hour <= 19) return t;
				t += GenDate.TicksPerHour;
			}
			return t;
		}

		// combien de temps il reste pour dresser la table. tout le mod repose sur cette
		// preparation, et le decalage sur la fin d'apres-midi peut repousser l'arrivee de
		// presque deux jours: sans ca le joueur attend sans savoir ce qu'il attend
		public static string ArrivalEta(FeastCase c)
		{
			if (c == null || c.map == null || c.eventTick < 0) return null;
			float lon = Find.WorldGrid.LongLatOf(c.map.Tile).x;
			long shift = GenDate.LocalTicksOffsetFromLongitude(lon);
			int offset = Find.TickManager.TicksAbs - Find.TickManager.TicksGame;
			long arrival = c.eventTick + offset;

			// on compare des jours locaux entiers, pas un delai en heures: "demain" veut dire
			// quelque chose pour le joueur, "dans 31 heures" beaucoup moins
			int days = (int)((arrival + shift) / GenDate.TicksPerDay
				- (Find.TickManager.TicksAbs + shift) / GenDate.TicksPerDay);
			TaggedString when = days <= 0 ? "RimFeast_EtaToday".Translate()
				: days == 1 ? "RimFeast_EtaTomorrow".Translate()
				: "RimFeast_EtaDays".Translate(days);

			int hour = GenDate.HourOfDay(arrival, lon);
			TaggedString band = (hour < 11 ? "RimFeast_EtaMorning"
				: hour < 16 ? "RimFeast_EtaMidday"
				: hour < 20 ? "RimFeast_EtaAfternoon"
				: "RimFeast_EtaEvening").Translate();

			return "RimFeast_EtaLine".Translate(when, band);
		}

		public void StartInvite(Map map, Faction house, Thing spot, bool planned = false)
		{
			if (current != null || map == null || house == null || spot == null) return;
			planned = planned || debugForcePlanned;
			debugForcePlanned = false;
			current = new FeastCase
			{
				id = nextId++,
				map = map,
				house = house,
				spotCell = spot.Position,
				spotThing = spot,
				state = FeastCase.Invited,
				eventTick = ArrivalTick(map, Rand.Range(45000, 75000)),
				planned = planned,
			};
			// une maison qui te deteste accepte parfois pour de mauvaises raisons
			current.treacherous = debugForceTreachery
				|| (house.PlayerGoodwill <= TreacheryGoodwillMax && Rand.Chance(TreacheryChance));
			debugForceTreachery = false;

			TaggedString body =
				(planned ? "RimFeast_TextInviteSentPlanned" : "RimFeast_TextInviteSent").Translate(house.Name);
			string eta = ArrivalEta(current);
			if (!eta.NullOrEmpty()) body += "\n\n" + eta;
			Find.LetterStack.ReceiveLetter("RimFeast_LabelInviteSent".Translate(), body,
				LetterDefOf.PositiveEvent, new TargetInfo(current.spotCell, map), house);
		}

		// filet de reparation au chargement: une partie ou la faction cachee est restee hostile
		// (massacre interrompu, vieille version) rend tout banquet impossible cote colons:
		// un cortege arme et hostile met la carte en danger eleve, et vanilla annule les fetes
		public override void FinalizeInit()
		{
			base.FinalizeInit();

			// ceinture et bretelles sur les pupilles: le set est bien sauvegarde par vanilla,
			// mais perdre un enfant du joueur a un ramasse-miettes serait irreparable
			foreach (PendingWard w in wards)
				if (w?.ward != null && !w.ward.Dead)
					Find.WorldPawns.ForcefullyKeptPawns.Add(w.ward);

			if (current != null && current.state == FeastCase.Fleeing) return;
			Faction guests = Find.FactionManager.FirstFactionOfDef(RimFeastDefOf.RimFeast_Guests);
			if (guests != null)
				SetMutualRelation(guests, Faction.OfPlayer, FactionRelationKind.Neutral);
		}

		public override void GameComponentTick()
		{
			// entretien du sustainer a chaque tick, avant la barriere des 250
			if (redWeddingSound != null)
			{
				if (redWeddingSound.Ended) redWeddingSound = null;
				else redWeddingSound.Maintain();
			}

			if (Find.TickManager.TicksGame % CheckInterval != 0) return;

			// les caravanes promises arrivent bien apres la fin du banquet: elles ne peuvent
			// pas dependre de current, qui est deja null a ce moment
			if (caravans.Count > 0) TickCaravans();
			if (raids.Count > 0) TickRaids();
			if (reveals.Count > 0) TickReveals();
			if (wards.Count > 0) TickWards();
			TickAwayInvites();

			if (current == null) return;

			FeastCase c = current;
			if (c.map == null || !Find.Maps.Contains(c.map)) { AbandonCase(c); return; }
			DropDeadLords(c);

			switch (c.state)
			{
				case FeastCase.Invited:
					if (Find.TickManager.TicksGame >= c.eventTick) SpawnCortege(c);
					break;
				case FeastCase.Traveling:
					if (LordGone(c)) current = null;
					break;
				case FeastCase.Feasting:
					if (LordGone(c)) { current = null; break; }
					// on rappelle a intervalle regulier, pas une seule fois a l'arrivee: un
					// colon pris dans une tache non interruptible, ou couche juste apres,
					// etait perdu pour toute la soiree
					if (Find.TickManager.TicksGame - c.lastSummonTick >= ResummonTicks)
						SummonColonists(c);
					Sample(c);
					TryBrawl(c);
					BreakUpBrawl(c);
					TryProposal(c);
					TryRequest(c);
					TryPoison(c);
					CheckPoisonSymptoms(c);
					break;
				case FeastCase.Fleeing:
					// vieille save figee en fuite: on demarre le compte a rebours maintenant
					if (c.fleeStartTick < 0) c.fleeStartTick = Find.TickManager.TicksGame;
					if (LordGone(c) || Find.TickManager.TicksGame - c.fleeStartTick > FleeBackstopTicks)
						ResolveBetrayal(c);
					break;
				case FeastCase.Leaving:
					if (c.fleeStartTick < 0) c.fleeStartTick = Find.TickManager.TicksGame;
					if (LordGone(c) || Find.TickManager.TicksGame - c.fleeStartTick > FleeBackstopTicks)
						current = null;
					break;
			}
		}

		// carte perdue en plein banquet: on coupe la musique et on rend la paix a la faction
		// cachee, sinon le prochain cortege arriverait hostile sur une autre carte
		private void AbandonCase(FeastCase c)
		{
			if (redWeddingSound != null)
			{
				redWeddingSound.End();
				redWeddingSound = null;
				Find.MusicManagerPlay?.ForceSilenceFor(1f);
			}
			if (c.state == FeastCase.Fleeing)
				SetMutualRelation(FeastUtility.GuestFaction(), Faction.OfPlayer, FactionRelationKind.Neutral);
			current = null;
		}

		// un lord termine mais toujours reference pend au chargement suivant et fait raler
		// le jeu. on lache la reference des qu'il quitte le manager
		private static void DropDeadLords(FeastCase c)
		{
			if (c.map == null) return;
			if (c.colonistLord != null && !c.map.lordManager.lords.Contains(c.colonistLord))
				c.colonistLord = null;
			if (c.lord != null && !c.map.lordManager.lords.Contains(c.lord))
				c.lord = null;
		}

		private static bool LordGone(FeastCase c) =>
			c.lord == null || c.lord.ownedPawns.Count == 0;

		private void SpawnCortege(FeastCase c)
		{
			Map map = c.map;
			if (c.spotThing == null || c.spotThing.Destroyed)
			{
				Messages.Message("RimFeast_MessageSpotDestroyed".Translate(c.house?.Name),
					MessageTypeDefOf.NegativeEvent);
				current = null;
				return;
			}

			// la maison a pu tomber ou nous declarer la guerre pendant le voyage du messager
			if (c.house == null || c.house.defeated || c.house.HostileTo(Faction.OfPlayer))
			{
				Messages.Message("RimFeast_MessageInviteVoid".Translate(c.house?.Name ?? "?"),
					MessageTypeDefOf.NegativeEvent);
				current = null;
				return;
			}

			Faction guestFaction = FeastUtility.GuestFaction();
			if (guestFaction == null) { current = null; return; }

			// la faction cachee emprunte le nom de la maison recue: le joueur doit lire
			// "Maison Hesse" sous ses convives, pas le nom technique du porte-manteau
			if (c.house.HasName || c.house.def != null) guestFaction.Name = c.house.Name;
			// ceinture et bretelles: une vieille save peut avoir garde l'hostilite d'un massacre
			SetMutualRelation(guestFaction, Faction.OfPlayer, FactionRelationKind.Neutral);

			if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 entry, map, CellFinder.EdgeRoadChance_Neutral))
				entry = CellFinder.RandomEdgeCell(map);

			var kinds = new List<PawnKindDef>();
			PawnKindDef crew = FeastUtility.CrewKindFor(c.house);
			PawnKindDef lord = FeastUtility.HouseLordKind(c.house) ?? FeastUtility.HouseKnightKind(c.house)
				?? FeastUtility.EliteKindFor(c.house) ?? crew;
			PawnKindDef standard = FeastUtility.HouseStandardKind(c.house) ?? crew;
			PawnKindDef knight = FeastUtility.HouseKnightKind(c.house) ?? FeastUtility.EliteKindFor(c.house) ?? crew;
			if (lord != null) kinds.Add(lord);
			if (standard != null) kinds.Add(standard);

			// une maison qui t'estime deplace sa cour, une maison qui te tolere envoie
			// le strict necessaire. et deux banquets ne se ressemblent pas
			int size = RimFeastMod.S.corteges
				+ Mathf.RoundToInt(Mathf.Clamp(c.house.PlayerGoodwill, -100f, 100f) / 50f)
				+ Rand.RangeInclusive(-1, 1);
			size = Mathf.Max(size, 2);
			PawnKindDef filler = crew ?? knight ?? lord;
			while (kinds.Count < size && filler != null)
				kinds.Add(kinds.Count % 2 == 0 ? (knight ?? filler) : filler);
			if (kinds.Count == 0) { current = null; return; }

			// le duc en personne, si la maison t'estime deja et que ta table a fait ses preuves.
			// c'est le vrai pawn du monde et il garde sa vraie faction, pas une copie jetable
			Pawn leader = null;
			if (debugForceLeader
				|| (RimFeastMod.S.leaderEnabled && c.house.PlayerGoodwill >= 40
					&& FeastsRemembered(c.house) >= 1 && Rand.Chance(0.4f)))
				leader = TakeLeader(c.house);
			debugForceLeader = false;
			if (leader != null) kinds.Remove(lord); // le duc tient le role du seigneur local

			var guests = new List<Pawn>();
			foreach (PawnKindDef kind in kinds)
			{
				Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
					kind, guestFaction, PawnGenerationContext.NonPlayer,
					tile: map.Tile, forceGenerateNewPawn: true, canGeneratePawnRelations: false));
				// ils arrivent avec un petit creux, c'est le but de la soiree
				if (p.needs?.food != null) p.needs.food.CurLevel = 0.35f * p.needs.food.MaxLevel;
				if (p.needs?.rest != null) p.needs.rest.CurLevel = Mathf.Max(p.needs.rest.CurLevel, 0.8f);
				GenSpawn.Spawn(p, CellFinder.RandomClosewalkCellNear(entry, map, 6), map);
				guests.Add(p);
			}

			if (leader != null)
			{
				if (leader.needs?.food != null) leader.needs.food.CurLevel = 0.35f * leader.needs.food.MaxLevel;
				if (leader.needs?.rest != null) leader.needs.rest.CurLevel = Mathf.Max(leader.needs.rest.CurLevel, 0.8f);
				GenSpawn.Spawn(leader, CellFinder.RandomClosewalkCellNear(entry, map, 6), map);
				guests.Add(leader);
			}

			// le plus haut rang porte les toasts. le menestrel n'est plus du cortege:
			// la musique fait partie de ton hospitalite, c'est un colon qui joue
			Pawn speaker = leader ?? guests.FirstOrDefault(p => p.kindDef == lord) ?? guests[0];

			// la main discrete du complot: pas l'orateur, trop en vue
			if (c.treacherous)
				c.poisoner = guests.Where(p => p != speaker && p.kindDef != standard)
					.RandomElementWithFallback() ?? guests.FirstOrDefault(p => p != speaker);

			c.lord = LordMaker.MakeNewLord(guestFaction,
				new LordJob_FeastGuests(c.id, c.house, c.spotCell, speaker), map, guests);
			c.guests = guests;
			c.guestCount = guests.Count;
			c.leaderCame = leader != null;
			c.state = FeastCase.Traveling;

			TaggedString arriveText = "RimFeast_TextGuestsArrive".Translate(c.house.Name, guests.Count);
			if (leader != null)
				arriveText += "\n\n" + "RimFeast_TextLeaderRides".Translate(leader.LabelShortCap, c.house.Name);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelGuestsArrive".Translate(), arriveText,
				LetterDefOf.PositiveEvent, guests[0], c.house);
		}

		// le vrai chef de maison, tire des pawns du monde. null = indisponible.
		// il doit etre libre dans le monde: un leader deja tenu ailleurs (kidnappe, pris par
		// une quete, dans un transporteur) ferait planter GenSpawn ou serait duplique
		private static Pawn TakeLeader(Faction house)
		{
			Pawn l = house?.leader;
			if (l == null || l.Dead || l.Spawned || l.IsPrisoner || !l.RaceProps.Humanlike) return null;
			if (!Find.WorldPawns.Contains(l)) return null;
			if (l.ParentHolder != null && !(l.ParentHolder is WorldPawns)) return null;
			if (QuestUtility.IsReservedByQuestOrQuestBeingGenerated(l) || l.IsQuestLodger()) return null;
			Find.WorldPawns.RemovePawn(l);
			return l;
		}

		public void Notify_GuestsArrived(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id || c.state != FeastCase.Traveling) return;
			c.state = FeastCase.Feasting;
			c.arrivedTick = Find.TickManager.TicksGame;
			Messages.Message("RimFeast_MessageFeastBegins".Translate(c.house?.Name),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.PositiveEvent);
			StartColonistLord(c);
			TryBringGift(c);
		}

		// le cortege ne vient pas les mains vides quand la maison t'estime. meme generateur
		// et meme bareme de richesse que le present des visiteurs vanilla, gradue sur
		// l'estime. pose au haut bout par le plus haut rang, la ou le joueur regarde
		private static void TryBringGift(FeastCase c)
		{
			if (c.house == null || c.lord == null || c.map == null) return;
			int esteem = c.house.PlayerGoodwill;
			if (esteem < 20 || !Rand.Chance(Mathf.InverseLerp(0f, 100f, esteem))) return;

			var parms = default(ThingSetMakerParams);
			parms.techLevel = c.house.def.techLevel;
			parms.totalMarketValueRange = DiplomacyTuning.VisitorGiftTotalMarketValueRangeBase
				* DiplomacyTuning.VisitorGiftTotalMarketValueFactorFromPlayerWealthCurve
					.Evaluate(c.map.wealthWatcher.WealthTotal)
				* Mathf.Lerp(0.5f, 1.5f, Mathf.InverseLerp(20f, 100f, esteem));
			List<Thing> gifts = ThingSetMakerDefOf.VisitorGift.root.Generate(parms);

			TargetInfo look = TargetInfo.Invalid;
			for (int i = 0; i < gifts.Count; i++)
			{
				if (GenPlace.TryPlaceThing(gifts[i], c.spotCell, c.map, ThingPlaceMode.Near)) look = gifts[i];
				else gifts[i].Destroy();
			}
			Pawn giver = SpeakerOf(c);
			if (!look.IsValid || giver == null) return;

			Find.LetterStack.ReceiveLetter("RimFeast_LabelCortegeGift".Translate(c.house.Name),
				"RimFeast_TextCortegeGift".Translate(c.house.Name, giver.LabelShortCap,
					gifts.Where(g => g.Spawned).Select(g => g.LabelCap).ToLineList("   -")),
				LetterDefOf.PositiveEvent, look, c.house);
		}

		public void Notify_ToastGiven(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id || c.state != FeastCase.Feasting) return;
			c.toastsGiven++;
			Messages.Message("RimFeast_MessageToast".Translate(c.house?.Name),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.PositiveEvent, historical: false);
		}

		public void Notify_FeastEnded(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id) return;

			if (c.state != FeastCase.Feasting)
			{
				// jamais arrives a table: salle inaccessible ou meteo
				Messages.Message("RimFeast_MessageGuestsLeftNoFeast".Translate(c.house?.Name),
					MessageTypeDefOf.NegativeEvent);
				current = null;
				return;
			}

			CloseFeast(c);
		}

		// bilan et verite differee du poison. le dossier reste ouvert tant qu'ils sont sur
		// la carte: le bonus est verse mais les egorger en chemin declenchera quand meme
		// les noces pourpres
		private void CloseFeast(FeastCase c)
		{
			Sample(c);
			ApplyOutcome(c);

			if (c.tainted && !c.exposed)
				reveals.Add(new PendingReveal
				{
					house = c.house,
					map = c.map,
					fireTick = Find.TickManager.TicksGame + Rand.Range(30000, 60000),
				});

			LeaveUnderOurRoof(c);
		}

		// un ivrogne assomme par un des siens reste des notres: le lord ne doit pas le perdre,
		// sinon la transition harmed fait partir tout le monde pour une rixe de taverne
		public bool IsBrawler(int id, Pawn p)
		{
			FeastCase c = current;
			if (c == null || c.id != id || !c.brawled || c.state != FeastCase.Feasting) return false;
			return p == c.brawlerA || p == c.brawlerB;
		}

		// lancee des le signal, pas au premier sang: la chanson doit monter pendant que le
		// bourreau traverse la salle. depuis un instrument si la salle en a un, sinon la
		// bande-son. sous filet, un clip pas charge emporterait tout le reste avec lui
		// deux morceaux, un reglage. l'air d'origine est une reprise, certains n'en veulent pas
		private static SongDef RedWeddingSong => RimFeastMod.S.censoredMusic
			? RimFeastDefOf.RimFeast_CensoredSong : RimFeastDefOf.RimFeast_RedWeddingSong;

		private static SoundDef RedWeddingPerformance => RimFeastMod.S.censoredMusic
			? RimFeastDefOf.RimFeast_CensoredPerformance : RimFeastDefOf.RimFeast_RedWeddingPerformance;

		public void StartRedWeddingMusic(FeastCase c)
		{
			if (c == null || c.musicStarted) return;
			c.musicStarted = true;
			try
			{
				Building_MusicalInstrument inst = HallInstruments(c)
					.Where(b => b.Spawned)
					.RandomElementWithFallback();
				if (inst != null)
				{
					Find.MusicManagerPlay?.ForceFadeoutAndSilenceFor(600f, 2f, preventDangerTransition: true);
					redWeddingSound = RedWeddingPerformance.TrySpawnSustainer(
						SoundInfo.InMap(new TargetInfo(inst.Position, c.map), MaintenanceType.PerTick));
					if (redWeddingSound == null)
					{
						Find.MusicManagerPlay?.ForceSilenceFor(1f);
						inst = null;
					}
				}
				if (inst == null)
				{
					SongDef song = RedWeddingSong;
					if (song?.clip == null)
						Log.Warning("RimFeast: red wedding song clip not loaded (" + song?.clipPath + ")");
					else
						Find.MusicManagerPlay?.ForcePlaySong(song, false);
				}
			}
			catch (System.Exception e)
			{
				Log.Warning("RimFeast: couldnt start the song: " + e.Message);
			}
		}

		// le premier sang, vu de l'ecran. StartFade va de la couleur courante vers la cible,
		// donc un flash c'est aller au rouge vite puis revenir lentement. rouge sombre et
		// semi-transparent: on doit continuer a voir sa salle et pouvoir agir
		private static void FlashRed(FeastCase c)
		{
			if (c?.map == null || Find.CurrentMap != c.map) return;
			// SetColor pose la couleur instantanement, StartFade repart de la couleur
			// courante. deux StartFade enchaines dans la meme frame ne marchent pas: le
			// second lit un fondu qui n'a pas encore bouge et efface le premier
			ScreenFader.SetColor(new Color(0.5f, 0.03f, 0.03f, 0.5f));
			ScreenFader.StartFade(Color.clear, 0.7f);
		}

		// premier sang de la main du joueur: on note, la sentence tombera au dernier sorti
		public void Notify_GuestsBetrayed(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id) return;
			if (c.state != FeastCase.Feasting && c.state != FeastCase.Traveling
				&& c.state != FeastCase.Leaving) return;
			c.state = FeastCase.Fleeing;
			c.fleeStartTick = Find.TickManager.TicksGame;

			StartRedWeddingMusic(c);
			FlashRed(c);

			// trahis, ils degainent: hostiles le temps du combat, le ciblage exige ca.
			// la resolution remet la faction cachee au neutre pour les banquets suivants
			SetMutualRelation(FeastUtility.GuestFaction(), Faction.OfPlayer, FactionRelationKind.Hostile);

			// la fete des colons s'arrete net, et sans bon souvenir
			if (c.colonistLord != null)
			{
				if (c.map.lordManager.lords.Contains(c.colonistLord))
					c.map.lordManager.RemoveLord(c.colonistLord);
				c.colonistLord = null;
			}

			Find.LetterStack.ReceiveLetter("RimFeast_LabelBetrayed".Translate(),
				"RimFeast_TextBetrayed".Translate(c.house?.Name),
				LetterDefOf.NegativeEvent, new TargetInfo(c.spotCell, c.map), c.house);

			// le signal etait donne: tout le monde tombe sur les survivants
			if (c.slaughterOrdered) LaunchAttackWave(c);
		}

		// drafte chaque combattant et le jette sur l'invite le plus proche. apres ces ordres
		// initiaux, la bataille appartient au joueur, comme n'importe quel combat drafte
		private void LaunchAttackWave(FeastCase c)
		{
			int sent = 0;
			var skipped = new StringBuilder();
			foreach (Pawn col in c.map.mapPawns.FreeColonistsSpawned.ToList())
			{
				if (col.Downed || col.InMentalState) { skipped.Append(col.LabelShort + "(downed/mental) "); continue; }
				if (!LordJob_FeastColonists.FitForTrap(col)) { skipped.Append(col.LabelShort + "(child/pacifist) "); continue; }
				if (col.CurJobDef == RimFeastDefOf.RimFeast_AssassinateJob) { skipped.Append(col.LabelShort + "(executioner) "); continue; }

				Pawn target = NearestGuest(c, col.Position);
				if (target == null) { skipped.Append("no target left "); break; }

				if (col.drafter != null) col.drafter.Drafted = true;

				// AttackStatic ne deplace pas le pawn: un archer hors de portee reste plante
				// la. on ne l'utilise que si la cible est deja tirable, sinon tout le monde
				// marche sur la salle
				Verb verb = col.TryGetAttackVerb(target);
				bool canShootNow = verb != null && !verb.IsMeleeAttack && verb.CanHitTarget(target);
				Job job = JobMaker.MakeJob(canShootNow ? JobDefOf.AttackStatic : JobDefOf.AttackMelee, target);
				bool ok = col.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
				if (ok) sent++;
				else skipped.Append(col.LabelShort + "(job refuse) ");
			}
			if (Prefs.DevMode)
				Log.Message("RimFeast: attack wave, " + sent + " sent. skipped: "
					+ (skipped.Length == 0 ? "none" : skipped.ToString())
					+ " | guests hostile=" + (FeastUtility.GuestFaction()?.HostileTo(Faction.OfPlayer)));
		}

		private static Pawn NearestGuest(FeastCase c, IntVec3 from)
		{
			Pawn best = null;
			float bestDist = float.MaxValue;
			if (c.lord == null) return null;
			foreach (Pawn g in c.lord.ownedPawns)
			{
				if (g.Dead || !g.Spawned || g.Downed) continue;
				float d = (g.Position - from).LengthHorizontalSquared;
				if (d < bestDist) { bestDist = d; best = g; }
			}
			return best;
		}

		// l'empoisonneur attend que la soiree soit avancee, puis agit si personne ne le surprend
		private void TryPoison(FeastCase c)
		{
			if (!c.treacherous || c.tainted || c.exposed) return;
			if (Find.TickManager.TicksGame - c.arrivedTick < TaintDelayTicks) return;
			Pawn p = c.poisoner;
			if (p == null || p.Dead || !p.Spawned || p.Downed) return;
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(c.spotCell, c.map);
			if (!area.Contains(p.Position)) return;
			if (!Rand.Chance(0.1f)) return;

			// chaque colon dans la salle peut le surprendre la main dans la marmite
			int witnesses = c.map.mapPawns.FreeColonistsSpawned
				.Count(col => area.Contains(col.Position));
			if (Rand.Chance(Mathf.Min(0.2f + 0.06f * witnesses, 0.55f)))
			{
				Expose(c, harmDone: false);
				return;
			}

			int stacks = 0;
			List<Thing> foods = c.map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);
			for (int i = 0; i < foods.Count; i++)
			{
				Thing t = foods[i];
				if (!t.Spawned || t.def.IsDrug) continue;
				if (!area.Contains(t.Position)) continue;
				CompFoodPoisonable comp = t.TryGetComp<CompFoodPoisonable>();
				if (comp == null) continue;
				comp.SetPoisoned(FoodPoisonCause.Unknown);
				stacks++;
			}
			if (stacks == 0) return; // rien sur la table, il retentera

			c.tainted = true;
			c.taintTick = Find.TickManager.TicksGame;
			c.sickBaseline = ColonistsWithFoodPoisoning(c.map);
		}

		// le poison peut se trahir tout seul si un colon tombe malade en pleine fete
		private void CheckPoisonSymptoms(FeastCase c)
		{
			if (!c.tainted || c.exposed) return;
			if (ColonistsWithFoodPoisoning(c.map) > c.sickBaseline)
				Expose(c, harmDone: true);
		}

		private static int ColonistsWithFoodPoisoning(Map map) =>
			map.mapPawns.FreeColonistsSpawned
				.Count(p => p.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning));

		// demasques: le complot echoue, le cortege detale, et ce qui leur arrive en sortant
		// ne regarde qu'eux. c'est eux qui ont rompu le droit de l'hote
		private void Expose(FeastCase c, bool harmDone)
		{
			c.exposed = true;
			c.state = FeastCase.Fleeing;
			c.fleeStartTick = Find.TickManager.TicksGame;
			if (c.colonistLord != null)
			{
				if (c.map.lordManager.lords.Contains(c.colonistLord))
					c.map.lordManager.RemoveLord(c.colonistLord);
				c.colonistLord = null;
			}
			c.lord?.ReceiveMemo(LordJob_FeastGuests.MemoExposed);
			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, harmDone ? -75 : -30,
				canSendMessage: true, canSendHostilityLetter: true);
			Find.LetterStack.ReceiveLetter(
				(harmDone ? "RimFeast_LabelPoisonSymptoms" : "RimFeast_LabelPoisonerCaught").Translate(),
				(harmDone ? "RimFeast_TextPoisonSymptoms" : "RimFeast_TextPoisonerCaught")
					.Translate(c.poisoner?.LabelShortCap ?? "?", c.house?.Name),
				LetterDefOf.NegativeEvent, new TargetInfo(c.spotCell, c.map), c.house);
		}

		// le pupille rentre a la maison, forme, ou ne rentre pas du tout
		private void TickWards()
		{
			for (int i = wards.Count - 1; i >= 0; i--)
			{
				PendingWard w = wards[i];
				Pawn p = w.ward;

				if (p == null || p.Dead)
				{
					wards.RemoveAt(i);
					if (p != null) Find.WorldPawns.ForcefullyKeptPawns.Remove(p);
					if (p != null)
						Find.LetterStack.ReceiveLetter("RimFeast_LabelWardLost".Translate(),
							"RimFeast_TextWardDied".Translate(p.LabelShortCap, w.house?.Name),
							LetterDefOf.NegativeEvent, null, w.house);
					continue;
				}

				// une maison tombee ne rend plus personne a l'heure: il rentre tout de suite,
				// ou il est perdu si c'est toi qui l'as brulee
				bool fallen = w.house == null || w.house.defeated;
				if (!fallen && Find.TickManager.TicksGame < w.returnTick) continue;

				// la colonie a change de carte: on attend plutot que de perdre l'enfant
				if (w.map == null || !Find.Maps.Contains(w.map))
				{
					w.map = Find.AnyPlayerHomeMap;
					if (w.map == null) continue;
				}

				wards.RemoveAt(i);
				Find.WorldPawns.ForcefullyKeptPawns.Remove(p);

				// il etait sous leur toit, comme eux etaient sous le tien
				if (w.house != null && w.house.HostileTo(Faction.OfPlayer))
				{
					Find.LetterStack.ReceiveLetter("RimFeast_LabelWardLost".Translate(),
						"RimFeast_TextWardLost".Translate(p.LabelShortCap, w.house.Name),
						LetterDefOf.NegativeEvent, null, w.house);
					continue;
				}

				ReturnWard(w, p, fallen);
			}
		}

		private static void ReturnWard(PendingWard w, Pawn p, bool early)
		{
			if (p.Spawned) return;
			if (Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);

			// des annees de cour et de cour d'armes. EnsureMinLevelWithMargin est le geste
			// vanilla: on releve un plancher sans ecraser ce qu'il savait deja faire.
			// rentre avant l'heure, il n'a appris qu'au prorata du temps passe la-bas
			float done = !early || w.leftTick < 0 ? 1f
				: Mathf.Clamp01((Find.TickManager.TicksGame - w.leftTick)
					/ (float)Mathf.Max(1, w.returnTick - w.leftTick));
			p.skills?.GetSkill(SkillDefOf.Melee)?.EnsureMinLevelWithMargin(Mathf.RoundToInt(8f * done));
			p.skills?.GetSkill(SkillDefOf.Social)?.EnsureMinLevelWithMargin(Mathf.RoundToInt(8f * done));
			p.skills?.GetSkill(SkillDefOf.Shooting)?.EnsureMinLevelWithMargin(Mathf.RoundToInt(6f * done));
			p.skills?.GetSkill(SkillDefOf.Intellectual)?.EnsureMinLevelWithMargin(Mathf.RoundToInt(6f * done));

			p.SetFaction(Faction.OfPlayer);
			if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 entry, w.map,
				CellFinder.EdgeRoadChance_Neutral))
				entry = CellFinder.RandomEdgeCell(w.map);
			GenSpawn.Spawn(p, CellFinder.RandomClosewalkCellNear(entry, w.map, 4), w.map);

			Find.LetterStack.ReceiveLetter("RimFeast_LabelWardReturns".Translate(),
				(early ? "RimFeast_TextWardHouseFell" : "RimFeast_TextWardReturns")
					.Translate(p.LabelShortCap, w.house?.Name ?? "?", p.ageTracker.AgeBiologicalYears),
				LetterDefOf.PositiveEvent, p, w.house);
		}

		private void TickReveals()
		{
			for (int i = reveals.Count - 1; i >= 0; i--)
			{
				PendingReveal r = reveals[i];
				if (Find.TickManager.TicksGame < r.fireTick) continue;
				reveals.RemoveAt(i);
				if (r.house == null || r.house.defeated) continue;
				r.house.TryAffectGoodwillWith(Faction.OfPlayer, -75,
					canSendMessage: true, canSendHostilityLetter: true);
				Find.LetterStack.ReceiveLetter("RimFeast_LabelPoisonRevealed".Translate(),
					"RimFeast_TextPoisonRevealed".Translate(r.house.Name),
					LetterDefOf.NegativeEvent, null, r.house);
			}
		}

		private void ResolveBetrayal(FeastCase c)
		{
			// la chanson s'eteint en fondu avec la lettre, puis un silence de circonstance
			try
			{
				if (redWeddingSound != null)
				{
					// le fondu de 5s vient du sustainFadeoutTime du def
					redWeddingSound.End();
					redWeddingSound = null;
					Find.MusicManagerPlay?.ForceSilenceFor(20f); // raccourcit la grande fenetre
				}
				else
				{
					// chemin de repli global. garde sur CurrentSong: morceau deja fini = on ne touche a rien
					MusicManagerPlay music = Find.MusicManagerPlay;
					if (music != null && music.IsPlaying
						&& (music.CurrentSong == RimFeastDefOf.RimFeast_RedWeddingSong
							|| music.CurrentSong == RimFeastDefOf.RimFeast_CensoredSong))
						music.ForceFadeoutAndSilenceFor(20f, 5f, preventDangerTransition: true);
				}
			}
			catch (System.Exception e)
			{
				Log.Warning("RimFeast: fadeout: " + e.Message);
			}

			// le combat est fini: la faction cachee redevient neutre, elle sert a tous les corteges
			Faction guests = FeastUtility.GuestFaction();
			SetMutualRelation(guests, Faction.OfPlayer, FactionRelationKind.Neutral);

			// les blesses qui rampent gardent leur rancune individuelle contre tes colons:
			// on l'efface, sinon la fuite des colons voit une menace impossible a classer
			if (guests != null && c.map != null)
				foreach (Pawn p in c.map.mapPawns.SpawnedPawnsInFaction(guests))
					p.mindState.enemyTarget = null;

			// empoisonneurs demasques en fuite: pas de bilan, pas de penalite, bon debarras
			if (c.exposed)
			{
				current = null;
				return;
			}

			// les refs mortes perdues au chargement comptent comme tombees, c'est le bon biais
			int total = c.guests.Count;
			int slain = 0;
			foreach (Pawn g in c.guests)
				if (g == null || g.Dead || g.Downed || g.IsPrisonerOfColony) slain++;

			HouseFeastMemory m = MemoryFor(c.house, true);
			if (total > 0 && slain * 2 >= total)
			{
				// noces pourpres. la maison victime ne pardonnera jamais
				if (m != null) m.betrayed = true;
				redWeddingTick = Find.TickManager.TicksGame;

				// pour une faction a goodwill, l'hostilite se force par le goodwill: -200 cloue
				// le compteur a -100 et le kind bascule tout seul. SetRelationDirect refuse ces
				// factions (le goodwill ecraserait le kind) et echouait en silence
				c.house?.TryAffectGoodwillWith(Faction.OfPlayer, -200, canSendMessage: false,
					canSendHostilityLetter: false);

				// le bris du droit de l'hote est un crime contre toutes les maisons
				foreach (Faction f in FeastUtility.InvitableHouses())
					if (f != c.house)
						f.TryAffectGoodwillWith(Faction.OfPlayer, -25, canSendMessage: false,
							canSendHostilityLetter: false);

				raids.Add(new PendingRaid
				{
					house = c.house,
					map = c.map,
					fireTick = Find.TickManager.TicksGame + Rand.Range(3, 8) * 60000,
					points = StorytellerUtility.DefaultThreatPointsNow(c.map) * 1.2f,
				});

				foreach (Pawn col in c.map.mapPawns.FreeColonistsSpawned)
					col.needs?.mood?.thoughts?.memories?.TryGainMemory(RimFeastDefOf.RimFeast_GuestRightBroken);

				Find.LetterStack.ReceiveLetter("RimFeast_LabelRedWedding".Translate(),
					"RimFeast_TextRedWedding".Translate(c.house?.Name, slain),
					LetterDefOf.ThreatBig, new TargetInfo(c.spotCell, c.map), c.house);
			}
			else
			{
				c.house?.TryAffectGoodwillWith(Faction.OfPlayer, -40, canSendMessage: true,
					canSendHostilityLetter: false);
				Find.LetterStack.ReceiveLetter("RimFeast_LabelGuestsHarmed".Translate(),
					"RimFeast_TextBloodSpilled".Translate(c.house?.Name),
					LetterDefOf.NegativeEvent, new TargetInfo(c.spotCell, c.map), c.house);
			}
			current = null;
		}

		private void TickRaids()
		{
			for (int i = raids.Count - 1; i >= 0; i--)
			{
				PendingRaid r = raids[i];
				if (Find.TickManager.TicksGame < r.fireTick) continue;
				// paix signee entre-temps: ils renoncent a la vengeance
				if (r.house == null || r.house.defeated || !r.house.HostileTo(Faction.OfPlayer)
					|| r.map == null || !Find.Maps.Contains(r.map))
				{
					raids.RemoveAt(i);
					continue;
				}
				var parms = new IncidentParms
				{
					target = r.map,
					faction = r.house,
					points = r.points,
					forced = true,
				};
				if (IncidentDefOf.RaidEnemy.Worker.TryExecute(parms)) { raids.RemoveAt(i); continue; }
				r.tries++;
				if (r.tries >= 4) raids.RemoveAt(i);
				else r.fireTick = Find.TickManager.TicksGame + CaravanRetryTicks;
			}
		}

		// la maison a-t-elle vu son cortege egorge sous ton toit. les issues du banquet chez
		// eux s'en souviennent
		public bool HouseBetrayed(Faction f) => MemoryFor(f, false)?.betrayed ?? false;

		public const int AwayKnives = 1;
		public const int AwayAmbush = 2;
		public const int AwaySlight = 3;
		public const int AwayDull = 4;
		public const int AwayFine = 5;
		public const int AwaySongs = 6;

		// outil debug: joue une issue precise sur la premiere caravane du joueur, sans avoir
		// a l'envoyer jusqu'au chateau ni a esperer le bon tirage
		public void DebugAwayOutcome(int which)
		{
			Caravan caravan = Find.WorldObjects.Caravans.FirstOrDefault(c => c.IsPlayerControlled);
			if (caravan == null)
			{
				Messages.Message("RimFeast: no player caravan on the world map.",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			Faction house = awayHouse ?? FeastUtility.InvitableHouses().FirstOrDefault();
			if (house == null)
			{
				Messages.Message("RimFeast: no invitable noble house found.",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}

			awayHouse = null;
			awayExpireTick = -1;

			switch (which)
			{
				case AwayKnives: AwayOutcome_Knives(caravan, house); break;
				case AwayAmbush: AwayOutcome_Ambush(caravan, house); break;
				case AwaySlight: AwayOutcome_Slight(caravan, house); break;
				case AwayDull: AwayOutcome_Dull(caravan, house); break;
				case AwayFine: AwayOutcome_Fine(caravan, house); break;
				default: AwayOutcome_Songs(caravan, house); break;
			}
		}

		// outil debug: fait tomber l'invitation tout de suite, sans le filtre d'estime et en
		// disant pourquoi quand ca echoue
		public void DebugAwayInviteNow()
		{
			if (awayHouse != null)
			{
				Messages.Message("RimFeast: an invitation is already open with " + awayHouse.Name + ".",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			Map home = Find.AnyPlayerHomeMap;
			if (home == null)
			{
				Messages.Message("RimFeast: no player home map.",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			nextAwayInviteTick = Find.TickManager.TicksGame + AwayInviteInterval();
			if (!TryOpenAwayInvite(home, force: true))
				Messages.Message("RimFeast: no non-hostile house has a settlement within "
					+ RimFeastMod.S.awayMaxTravelDays + " days of travel. Raise that setting or move closer.",
					MessageTypeDefOf.RejectInput, historical: false);
		}

		private static int AwayInviteInterval() =>
			Mathf.RoundToInt(RimFeastMod.S.awayFeastDays * 60000f * Rand.Range(0.7f, 1.3f));

		public bool AwayInviteOpen(Faction f) =>
			awayHouse != null && awayHouse == f && Find.TickManager.TicksGame < awayExpireTick;

		public int AwayInviteDaysLeft() =>
			Mathf.Max(0, Mathf.CeilToInt((awayExpireTick - Find.TickManager.TicksGame) / 60000f));

		// une maison finit par rendre l'invitation. on ne la propose qu'a une maison qui a
		// une raison de te connaitre: elle a mange chez toi, ou elle t'estime deja
		private void TickAwayInvites()
		{
			int now = Find.TickManager.TicksGame;

			// l'invitation se perime toute seule, et une maison devenue hostile la retire
			if (awayHouse != null && (now >= awayExpireTick
				|| awayHouse.defeated || awayHouse.HostileTo(Faction.OfPlayer)))
			{
				awayHouse = null;
				awayExpireTick = -1;
			}

			if (!RimFeastMod.S.awayFeastsEnabled) return;
			if (nextAwayInviteTick < 0) { nextAwayInviteTick = now + AwayInviteInterval(); return; }
			if (now < nextAwayInviteTick) return;
			nextAwayInviteTick = now + AwayInviteInterval();

			// une seule table dressee a la fois
			if (awayHouse != null) return;

			Map home = Find.AnyPlayerHomeMap;
			if (home == null) return;

			TryOpenAwayInvite(home, force: false);
		}

		// force = outil debug: on ignore la condition "elle a une raison de te connaitre",
		// qu'une partie neuve ne peut de toute facon jamais remplir
		private bool TryOpenAwayInvite(Map home, bool force)
		{
			int now = Find.TickManager.TicksGame;

			// on cherche la maison ET le chateau ensemble: inviter une maison pour decouvrir
			// ensuite que son seul siege est a trente jours de cheval ne sert a rien
			Faction house = null;
			Settlement seat = null;
			int travel = 0;
			foreach (Faction f in FeastUtility.InvitableHouses()
				.Where(x => force || FeastsRemembered(x) >= 1 || x.PlayerGoodwill >= 20)
				.InRandomOrder())
			{
				Settlement best = null;
				int bestDays = 0;
				foreach (Settlement s in Find.WorldObjects.SettlementBases)
				{
					if (s.Faction != f || !s.Spawned) continue;
					int d = TravelDaysTo(home.Tile, s.Tile);
					if (d <= 0 || d > RimFeastMod.S.awayMaxTravelDays) continue;
					if (best == null || d < bestDays) { best = s; bestDays = d; }
				}
				if (best != null) { house = f; seat = best; travel = bestDays; break; }
			}
			if (house == null) return false;

			// la fenetre doit laisser le temps d'y aller et d'en revenir, le reglage n'est
			// qu'un plancher
			int windowDays = Mathf.Max(RimFeastMod.S.awayInviteWindowDays, travel * 2 + 3);
			awayHouse = house;
			awayExpireTick = now + windowDays * 60000;

			Find.LetterStack.ReceiveLetter("RimFeast_LabelAwayInvited".Translate(),
				"RimFeast_TextAwayInvited".Translate(house.Name, seat.Label, travel, windowDays),
				LetterDefOf.PositiveEvent, seat, house);
			return true;
		}

		// duree du trajet en jours. 0 = aucune route. le filtre a vol d'oiseau evite de
		// lancer un calcul de chemin monde sur chaque colonie de la planete
		private static int TravelDaysTo(PlanetTile from, PlanetTile to)
		{
			if (!from.Valid || !to.Valid) return 0;
			// une caravane ne passe pas d'une couche planetaire a l'autre, et le pathfinder
			// monde jette une erreur si on le lui demande
			if (from.Layer != to.Layer) return 0;
			if (Find.WorldGrid.ApproxDistanceInTiles(from, to) > RimFeastMod.S.awayMaxTravelDays * 20f)
				return 0;
			int ticks = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(from, to, null);
			return ticks <= 0 ? 0 : Mathf.CeilToInt(ticks / 60000f);
		}

		// meme courbe que les pourparlers vanilla: un bon negociateur divise le risque par dix
		private static readonly SimpleCurve AwayBadByNegotiation = new SimpleCurve
		{
			new CurvePoint(0f, 4f),
			new CurvePoint(1f, 1f),
			new CurvePoint(1.5f, 0.4f),
		};

		// leur estime pese autant que l'eloquence de ton envoye. une maison bien recue chez
		// toi te recoit bien chez elle, une maison dont tu as egorge le cortege t'invite
		// pour une tout autre raison
		private float AwayBadFactor(Pawn diplomat, Faction house)
		{
			float f = AwayBadByNegotiation.Evaluate(diplomat.GetStatValue(StatDefOf.NegotiationAbility));

			// leur estime pese autant que le reste: une maison qui t'adore ne te tend pas
			// d'embuscade, une maison qui te tolere tout juste y pense
			f *= Mathf.Lerp(2f, 0.35f,
				Mathf.InverseLerp(-20f, 100f, house.PlayerGoodwill));

			f /= 1f + 0.25f * FeastsRemembered(house);
			if (HouseBetrayed(house)) f *= 4f;
			// curseur dedie: la traitrise chez eux n'a rien a voir avec l'empoisonnement
			// qu'on subit chez soi, et elle n'avait aucun reglage jusqu'ici
			return f * RimFeastMod.S.awayRiskFactor;
		}

		public void ResolveAwayFeast(Caravan caravan, Settlement seat)
		{
			if (caravan == null || seat?.Faction == null) return;
			Faction house = seat.Faction;
			if (!AwayInviteOpen(house)) return;

			Pawn diplomat = BestCaravanPawnUtility.FindBestDiplomat(caravan);
			if (diplomat == null)
			{
				Messages.Message("RimFeast_MessageAwayNoDiplomat".Translate(), caravan,
					MessageTypeDefOf.NegativeEvent, historical: false);
				return;
			}

			awayHouse = null;
			awayExpireTick = -1;

			float bad = AwayBadFactor(diplomat, house);
			float good = 1f / bad;
			var outcomes = new List<Pair<System.Action, float>>
			{
				// cale sur les pourparlers vanilla: environ 15% de mauvaises issues avec un
				// negociateur moyen chez une maison amie, pas le double
				new Pair<System.Action, float>(() => AwayOutcome_Knives(caravan, house), 0.06f * bad),
				new Pair<System.Action, float>(() => AwayOutcome_Ambush(caravan, house), 0.03f * bad),
				new Pair<System.Action, float>(() => AwayOutcome_Slight(caravan, house), 0.08f * bad),
				new Pair<System.Action, float>(() => AwayOutcome_Dull(caravan, house), 0.20f),
				new Pair<System.Action, float>(() => AwayOutcome_Fine(caravan, house), 0.55f * good),
				new Pair<System.Action, float>(() => AwayOutcome_Songs(caravan, house), 0.10f * good),
			};
			outcomes.RandomElementByWeight(x => x.Second).First();

			diplomat.skills?.Learn(SkillDefOf.Social, 4000f, direct: true);
		}

		// le miroir de ton empoisonnement: le repas a bien eu lieu, et les couteaux sont
		// sortis au milieu. tout se joue a table, pas de carte, et la lettre dit qui s'en
		// est tire. savoir se battre sauve, donc le choix de qui part compte vraiment
		private static void AwayOutcome_Knives(Caravan caravan, Faction house)
		{
			List<Pawn> guests = caravan.PawnsListForReading
				.Where(p => p.RaceProps.Humanlike && !p.Dead).ToList();
			if (guests.Count == 0) { AwayOutcome_Slight(caravan, house); return; }

			var dead = new List<string>();
			var hurt = new List<string>();
			foreach (Pawn p in guests.InRandomOrder().Take(Mathf.Min(guests.Count, Rand.RangeInclusive(1, 2))))
			{
				// les autres ne regardent pas mourir leur monde: une escorte armee et
				// aguerrie arrache les siens de la salle. compte a demi si le compagnon
				// est mauvais bretteur, a demi encore s'il n'a rien pour se battre
				float escort = 0f;
				foreach (Pawn x in guests)
				{
					if (x == p || x.Dead || x.Downed) continue;
					int m = x.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
					escort += (m >= 6 ? 1f : 0.5f) * (x.equipment?.Primary != null ? 1f : 0.5f);
				}

				int melee = p.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
				float odds = 0.35f + 0.03f * melee + Mathf.Min(escort * 0.06f, 0.30f);
				if (Rand.Chance(Mathf.Clamp(odds, 0.35f, 0.95f))) continue;
				if (Rand.Chance(0.34f))
				{
					HealthUtility.DamageUntilDead(p);
					dead.Add(p.LabelShortCap);
				}
				else
				{
					HealthUtility.DamageUntilDowned(p);
					hurt.Add(p.LabelShortCap);
				}
			}

			// ils viennent d'essayer de tuer ta delegation a leur table: la guerre est la
			// seule suite qui tienne debout
			house.TryAffectGoodwillWith(Faction.OfPlayer,
				Mathf.Min(-75, Faction.OfPlayer.GoodwillToMakeHostile(house)),
				canSendMessage: false, canSendHostilityLetter: true);

			TaggedString text = "RimFeast_TextAwayKnives".Translate(house.Name);
			if (dead.Count > 0)
				text += "\n\n" + "RimFeast_TextAwayKnivesDead".Translate(dead.ToCommaList(useAnd: true));
			if (hurt.Count > 0)
				text += "\n\n" + "RimFeast_TextAwayKnivesHurt".Translate(hurt.ToCommaList(useAnd: true));
			if (dead.Count == 0 && hurt.Count == 0)
				text += "\n\n" + "RimFeast_TextAwayKnivesClean".Translate();

			Find.LetterStack.ReceiveLetter("RimFeast_LabelAwayKnives".Translate(), text,
				dead.Count > 0 ? LetterDefOf.Death : LetterDefOf.NegativeEvent, caravan, house);
		}

		// GetOrGenerateMapForIncident prend la tuile de la caravane, et s'il y a deja un objet
		// de carte dessus il genere celui-la. Sur la tuile du chateau ca donnerait LEUR
		// colonie, sur la tuile du joueur ca donnerait sa propre base. On recule donc d'une
		// tuile, sur du terrain vide, avant de generer la carte de rencontre
		private static bool MoveCaravanToOpenGround(Caravan caravan)
		{
			if (!Find.WorldObjects.AnyMapParentAt(caravan.Tile)
				&& Current.Game.FindMap(caravan.Tile) == null) return true;

			var neighbours = new List<PlanetTile>();
			Find.WorldGrid.GetTileNeighbors(caravan.Tile, neighbours);
			foreach (PlanetTile t in neighbours.InRandomOrder())
			{
				if (Find.World.Impassable(t)) continue;
				if (Find.WorldObjects.AnyMapParentAt(t)) continue;
				if (Current.Game.FindMap(t) != null) continue;
				caravan.pather?.StopDead();
				caravan.Tile = t;
				return true;
			}
			return false;
		}

		// ils n'ont jamais eu l'intention de te nourrir, et ils te cueillent avant les murs.
		// carte de rencontre, flux vanilla des caravanes prises a partie
		private static void AwayOutcome_Ambush(Caravan caravan, Faction house)
		{
			// pas de terrain libre autour: on ne genere rien, les couteaux racontent la
			// meme trahison sans risquer de larguer le joueur dans leur base
			if (!MoveCaravanToOpenGround(caravan)) { AwayOutcome_Knives(caravan, house); return; }

			LongEventHandler.QueueLongEvent(delegate
			{
				// ils doivent etre reellement hostiles avant la generation, sinon les
				// assaillants s'affichent en neutre et la bataille est declaree gagnee aussitot.
				// GoodwillToMakeHostile rend "-75 moins le goodwill actuel", donc on prend le
				// minimum comme le fait vanilla pour garantir la bascule
				house.TryAffectGoodwillWith(Faction.OfPlayer,
					Mathf.Min(-50, Faction.OfPlayer.GoodwillToMakeHostile(house)),
					canSendMessage: false, canSendHostilityLetter: false);

				IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, caravan);
				parms.faction = house;
				PawnGroupMakerParms groupParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(
					PawnGroupKindDefOf.Combat, parms, ensureCanGenerateAtLeastOnePawn: true);
				groupParms.generateFightersOnly = true;
				List<Pawn> attackers = PawnGroupMakerUtility.GeneratePawns(groupParms).ToList();

				Map map = CaravanIncidentUtility.SetupCaravanAttackMap(caravan, attackers,
					sendLetterIfRelatedPawns: false);
				if (attackers.Any())
					LordMaker.MakeNewLord(house, new LordJob_AssaultColony(house), map, attackers);
				Find.TickManager.Notify_GeneratedPotentiallyHostileMap();

				GlobalTargetInfo target = attackers.Any()
					? new GlobalTargetInfo(attackers[0].Position, map) : GlobalTargetInfo.Invalid;
				Find.LetterStack.ReceiveLetter("RimFeast_LabelAwayAmbush".Translate(),
					"RimFeast_TextAwayAmbush".Translate(house.Name), LetterDefOf.ThreatBig, target, house);
			}, "GeneratingMapForNewEncounter", doAsynchronously: false, null);
		}

		private static void AwayOutcome_Slight(Caravan caravan, Faction house)
		{
			house.TryAffectGoodwillWith(Faction.OfPlayer, -10, canSendMessage: false,
				canSendHostilityLetter: false);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelAwaySlight".Translate(),
				"RimFeast_TextAwaySlight".Translate(house.Name), LetterDefOf.NegativeEvent, caravan, house);
		}

		private static void AwayOutcome_Dull(Caravan caravan, Faction house)
		{
			Find.LetterStack.ReceiveLetter("RimFeast_LabelAwayDull".Translate(),
				"RimFeast_TextAwayDull".Translate(house.Name), LetterDefOf.NeutralEvent, caravan, house);
		}

		private static void AwayOutcome_Fine(Caravan caravan, Faction house)
		{
			int gain = Rand.RangeInclusive(12, 20);
			house.TryAffectGoodwillWith(Faction.OfPlayer, gain, canSendMessage: false,
				canSendHostilityLetter: false);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelAwayFine".Translate(),
				"RimFeast_TextAwayFine".Translate(house.Name, gain), LetterDefOf.PositiveEvent, caravan, house);
		}

		// on repart les bras charges: le butin entre directement dans la caravane
		private static void AwayOutcome_Songs(Caravan caravan, Faction house)
		{
			int gain = Rand.RangeInclusive(25, 35);
			house.TryAffectGoodwillWith(Faction.OfPlayer, gain, canSendMessage: false,
				canSendHostilityLetter: false);

			var parms = default(ThingSetMakerParams);
			parms.makingFaction = house;
			parms.techLevel = house.def.techLevel;
			parms.maxTotalMass = 20f;
			parms.totalMarketValueRange = new FloatRange(400f, 1000f);
			parms.tile = caravan.Tile;
			List<Thing> gifts = ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate(parms);
			for (int i = 0; i < gifts.Count; i++)
				caravan.AddPawnOrItem(gifts[i], addCarriedPawnToWorldPawnsIfAny: true);

			Find.LetterStack.ReceiveLetter("RimFeast_LabelAwaySongs".Translate(),
				"RimFeast_TextAwaySongs".Translate(house.Name, gain, GenLabel.ThingsLabel(gifts)),
				LetterDefOf.PositiveEvent, caravan, house);
		}

		// une maison peut refuser ton invitation: trahie elle-meme, ou rumeur d'un banquet rouge
		public bool HouseRefuses(Faction f, out string reason)
		{
			reason = null;
			HouseFeastMemory m = MemoryFor(f, false);
			if (m != null && m.betrayed)
			{
				reason = "RimFeast_InviteRefusedBetrayed".Translate();
				return true;
			}
			if (redWeddingTick > 0
				&& Find.TickManager.TicksGame - redWeddingTick < RimFeastMod.S.redWeddingDays * 60000)
			{
				reason = "RimFeast_InviteRefusedWord".Translate();
				return true;
			}
			return false;
		}

		public void Notify_GuestsHarmed(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id) return;
			if (c.state == FeastCase.Fleeing) return;

			// un des leurs tue par un des leurs: leur affaire, pas d'affront a nous reprocher.
			// la soiree s'arrete quand meme, un mort sous la table clot n'importe quel banquet
			if (c.brawled && ((c.brawlerA?.Dead ?? false) || (c.brawlerB?.Dead ?? false)))
			{
				Find.LetterStack.ReceiveLetter("RimFeast_LabelBrawlDeath".Translate(),
					"RimFeast_TextBrawlDeath".Translate(c.house?.Name),
					LetterDefOf.NegativeEvent, new TargetInfo(c.spotCell, c.map), c.house);
				LeaveUnderOurRoof(c);
				return;
			}

			// des lames sont sorties sur la carte, raid ou meute: on ne reproche pas a l'hote
			// ce qu'il vient de repousser. le trigger l'excuse deja, le bilan doit suivre.
			// s'ils ont eu le temps de juger la table, elle est jugee
			if (GenHostility.AnyHostileActiveThreatToPlayer(c.map))
			{
				bool judged = c.state == FeastCase.Feasting
					&& Find.TickManager.TicksGame - c.arrivedTick >= FeastDurationTicks / 2;
				TaggedString text = "RimFeast_TextGuestsLeaveFighting".Translate(c.house?.Name);
				if (judged) text += "\n\n" + "RimFeast_TextGuestsLeaveFightingJudged".Translate();
				Find.LetterStack.ReceiveLetter("RimFeast_LabelGuestsLeaveFighting".Translate(), text,
					LetterDefOf.NeutralEvent, new TargetInfo(c.spotCell, c.map), c.house);
				if (judged) CloseFeast(c);
				else LeaveUnderOurRoof(c);
				return;
			}

			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, -15, canSendMessage: true,
				canSendHostilityLetter: false);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelGuestsHarmed".Translate(),
				"RimFeast_TextGuestsHarmed".Translate(c.house?.Name),
				LetterDefOf.NegativeEvent, new TargetInfo(c.spotCell, c.map), c.house);
			LeaveUnderOurRoof(c);
		}

		// la soiree est finie mais ils sont encore chez nous. lacher le dossier ici rendait
		// gratuit le massacre du cortege sur le chemin de la porte: Notify_GuestsBetrayed
		// trouvait current a null et repartait sans rien faire
		private static void LeaveUnderOurRoof(FeastCase c)
		{
			c.state = FeastCase.Leaving;
			c.fleeStartTick = Find.TickManager.TicksGame;
		}

		private void StartColonistLord(FeastCase c)
		{
			GatheringDef def = RimFeastDefOf.RimFeast_Feast;
			Pawn organizer = GatheringsUtility.FindRandomGatheringOrganizer(Faction.OfPlayer, c.map, def);
			// l'hote d'un banquet piege doit pouvoir tenir un couteau
			if (c.planned && organizer != null && !LordJob_FeastColonists.FitForTrap(organizer))
				organizer = c.map.mapPawns.FreeColonistsSpawned
					.Where(p => LordJob_FeastColonists.FitForTrap(p)
						&& GatheringsUtility.PawnCanStartOrContinueGathering(p))
					.RandomElementWithFallback();
			if (organizer == null) return;
			var job = new LordJob_FeastColonists(c.spotCell, organizer, def, c.planned);
			c.colonistLord = LordMaker.MakeNewLord(Faction.OfPlayer, job, c.map,
				job.OrganizerIsStartingPawn ? new List<Pawn> { organizer } : null);
			SummonColonists(c);
		}

		// seuil Rested de vanilla. en dessous le colon est vraiment fatigue, on le laisse
		private const float WakeRestFloor = 0.28f;

		// on repasse les rappeler regulierement pendant tout le festin
		private const int ResummonTicks = 2500;

		// vanilla n'arrache personne a sa corvee pour une fete: les colons rejoignent au
		// compte-gouttes, quand leur job se termine. un banquet d'Etat merite mieux, alors
		// on coupe les taches interruptibles et l'arbre de decision les envoie a table
		private void SummonColonists(FeastCase c)
		{
			c.lastSummonTick = Find.TickManager.TicksGame;
			if (!RimFeastMod.S.summonColonists) return;
			var job = c.colonistLord?.LordJob as LordJob_VoluntarilyJoinable;
			if (job == null || !c.map.lordManager.lords.Contains(c.colonistLord)) return;

			foreach (Pawn p in c.map.mapPawns.FreeColonistsSpawned.ToList())
			{
				if (p.Drafted || p.GetLord() != null) continue;

				// un dormeur est doublement bloque: VoluntaryJoinPriorityFor le refuse
				// (ShouldGuestKeepAttendingGathering teste Awake) et son job LayDown empeche
				// l'arbre de decision de tourner. sans coup de pouce il ne rejoint qu'a son
				// reveil naturel, soit des heures plus tard, souvent apres la fin du banquet
				if (!p.Awake())
				{
					if ((p.needs?.rest?.CurLevel ?? 0f) < WakeRestFloor) continue;
					if (c.planned && !LordJob_FeastColonists.FitForTrap(p)) continue;
					RestUtility.WakeUp(p);
					continue;
				}

				// la priorite fait le tri du reste: blesses, enfants d'un banquet piege
				if (job.VoluntaryJoinPriorityFor(p) <= 0f) continue;
				// on ne leve pas un chirurgien de sa table d'operation
				Job cur = p.CurJob;
				if (cur == null || !cur.def.casualInterruptible) continue;
				p.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
		}

		// echantillonne salle, table et boisson pendant le festin
		private void Sample(FeastCase c)
		{
			Map map = c.map;
			Room room = c.spotCell.GetRoom(map);
			if (room != null && !room.PsychologicallyOutdoors)
				c.maxImpressiveness = Mathf.Max(c.maxImpressiveness, room.GetStat(RoomStatDefOf.Impressiveness));

			var dishes = new HashSet<ThingDef>();
			int drinkUnits = 0;
			var tierNutrition = new float[TierValue.Length];
			// la zone est calculee une fois pour toute la passe, pas par item
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(c.spotCell, map);
			List<Thing> foods = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);
			for (int i = 0; i < foods.Count; i++)
			{
				Thing t = foods[i];
				if (!t.Spawned || !t.IngestibleNow) continue;
				if (!area.Contains(t.Position)) continue;
				if (t.def.IsDrug)
				{
					if (t.def.ingestible?.drugCategory == DrugCategory.Social)
						drinkUnits += t.stackCount;
				}
				else if ((int)t.def.ingestible.preferability > (int)FoodPreferability.RawBad)
				{
					dishes.Add(t.def);
					c.bestFoodPref = Mathf.Max(c.bestFoodPref, (int)t.def.ingestible.preferability);
					tierNutrition[TierIndexOf(t.def.ingestible.preferability)] +=
						t.stackCount * t.def.ingestible.CachedNutrition;
				}
			}
			c.maxVariety = Mathf.Max(c.maxVariety, dishes.Count);
			c.maxDrinkUnits = Mathf.Max(c.maxDrinkUnits, drinkUnits);
			c.maxTableScore = Mathf.Max(c.maxTableScore, TableScore(tierNutrition, c.guestCount));

			if (!c.drankAny && c.lord != null)
				foreach (Pawn p in c.lord.ownedPawns)
					if (FeastUtility.DrunkennessOf(p) > 0.01f) { c.drankAny = true; break; }

			c.samples++;
			if (FeastUtility.MusicPossible && AnyMusicPlaying(c))
				c.musicSamples++;
		}

		// scanner tous les batiments colons a chaque passe pour trouver 0 ou 1 harpe est du
		// gachis: on releve la salle une fois par banquet
		public static List<Building_MusicalInstrument> HallInstruments(FeastCase c)
		{
			int now = Find.TickManager.TicksGame;
			if (c.hallInstruments == null || now - c.instrumentsTick > 2500)
			{
				FeastUtility.FeastArea area = FeastUtility.FeastArea.For(c.spotCell, c.map);
				c.hallInstruments = c.map.listerBuildings
					.AllBuildingsColonistOfClass<Building_MusicalInstrument>()
					.Where(b => area.Contains(b.InteractionCell))
					.ToList();
				c.instrumentsTick = now;
			}
			return c.hallInstruments;
		}

		private static bool AnyMusicPlaying(FeastCase c)
		{
			List<Building_MusicalInstrument> insts = HallInstruments(c);
			for (int i = 0; i < insts.Count; i++)
				if (insts[i].Spawned && insts[i].IsBeingPlayed) return true;
			return false;
		}

		private static int TierIndexOf(FoodPreferability p)
		{
			if (p >= FoodPreferability.MealLavish) return 0;
			if (p >= FoodPreferability.MealFine) return 1;
			if (p >= FoodPreferability.MealSimple) return 2;
			return 3;
		}

		// assez de bon pour tout le monde: on sert les convives du meilleur plat au moins bon,
		// une place vide vaut zero. un seul plat fastueux pour six ne vaut donc presque rien
		private static float TableScore(float[] tierNutrition, int guestCount)
		{
			if (guestCount <= 0) return 0f;
			float slots = guestCount;
			float total = 0f;
			for (int i = 0; i < TierValue.Length && slots > 0f; i++)
			{
				float take = Mathf.Min(tierNutrition[i] / ServingNutrition, slots);
				total += take * TierValue[i];
				slots -= take;
			}
			return total / guestCount;
		}

		private void TryBrawl(FeastCase c)
		{
			if (c.brawled || c.lord == null) return;
			if (Find.TickManager.TicksGame - c.arrivedTick < BrawlGraceTicks) return;

			foreach (Pawn drunk in c.lord.ownedPawns)
			{
				if (FeastUtility.DrunkennessOf(drunk) < 0.3f) continue;
				if (!Rand.Chance(RimFeastMod.S.brawlChance)) continue;

				FeastUtility.FeastArea hall = FeastUtility.FeastArea.For(c.spotCell, c.map);
				Pawn other = c.lord.ownedPawns
					.Where(p => p != drunk && p.Spawned && !p.Downed
						&& drunk.interactions != null && drunk.interactions.SocialFightPossible(p)
						&& hall.Contains(p.Position))
					.RandomElementWithFallback();
				if (other == null) continue;

				drunk.interactions.StartSocialFight(other, "RimFeast_MessageBrawl");
				c.brawled = true;
				c.brawlTick = Find.TickManager.TicksGame;
				c.brawlerA = drunk;
				c.brawlerB = other;
				return;
			}
		}

		// un noble s'entiche d'un colon quand la soiree tourne bien. une seule fois par banquet,
		// et jamais ta derniere colonie: on ne te vide pas la base par une lettre
		private void TryProposal(FeastCase c)
		{
			if (c.proposalDone || c.lord == null) return;
			if (Find.TickManager.TicksGame - c.arrivedTick < BrawlGraceTicks) return;
			if (!RimFeastMod.S.marriageEnabled) return;
			// les gates bon marche d'abord: ComputeScore ne doit pas tourner pour rien
			if (c.map.mapPawns.FreeColonistsSpawnedCount < 2) return;
			if (!Rand.Chance(RimFeastMod.S.proposalChance)) return;
			if (ComputeScore(c) < GoodScore) return;
			if (!FeastUtility.TryFindMarriagePair(c.lord, out Pawn noble, out Pawn colonist)) return;

			c.proposalDone = true;
			c.betrothedNoble = noble;
			c.betrothedColonist = colonist;

			int timeout = Mathf.Max(2500,
				c.arrivedTick + FeastDurationTicks - Find.TickManager.TicksGame);
			var letter = (ChoiceLetter_MarriageProposal)LetterMaker.MakeLetter(
				"RimFeast_LabelProposal".Translate(),
				"RimFeast_TextProposal".Translate(noble.LabelShortCap, c.house?.Name, colonist.LabelShortCap),
				RimFeastDefOf.RimFeast_MarriageProposal, new LookTargets(new[] { noble, colonist }));
			letter.caseId = c.id;
			letter.StartTimeout(timeout);
			Find.LetterStack.ReceiveLetter(letter);
		}

		// revalide a l'ouverture de la lettre: entre l'envoi et le clic, la promise a pu se
		// marier ailleurs (les liaisons vanilla naissent n'importe quand) ou tomber au tapis
		public bool ProposalPending(int id)
		{
			FeastCase c = current;
			return c != null && c.id == id && c.proposalDone
				&& c.state == FeastCase.Feasting
				&& c.betrothedNoble != null && !c.betrothedNoble.Dead
				&& c.betrothedColonist != null && !c.betrothedColonist.Dead
				&& c.betrothedColonist.Faction == Faction.OfPlayer
				&& !c.betrothedColonist.Downed && !c.betrothedColonist.InMentalState
				&& FeastUtility.SingleAndFree(c.betrothedColonist)
				&& FeastUtility.SingleAndFree(c.betrothedNoble)
				&& (c.lord?.ownedPawns.Contains(c.betrothedNoble) ?? false);
		}

		public void AcceptProposal(int id)
		{
			if (!ProposalPending(id)) return;
			FeastCase c = current;
			Pawn noble = c.betrothedNoble;
			Pawn colonist = c.betrothedColonist;

			// scelle le mariage par le flux vanilla. Married attend un lien fiance a retirer
			colonist.relations.AddDirectRelation(PawnRelationDefOf.Fiance, noble);
			MarriageCeremonyUtility.Married(colonist, noble);

			// la colon quitte la colonie pour la maison: on la sort de son lord de fete avant
			// le changement de faction, puis on l'ajoute au cortege pour qu'elle reparte avec eux
			if (colonist.Drafted) colonist.drafter.Drafted = false;
			colonist.jobs?.StopAll();
			colonist.GetLord()?.Notify_PawnLost(colonist, PawnLostCondition.LeftVoluntarily);
			// elle entre dans la maison, pas dans la faction cachee des corteges: sinon elle
			// redeviendrait hostile a chaque futur massacre d'un cortege qui n'est pas le sien
			colonist.SetFaction(c.house);
			c.lord.AddPawn(colonist);
			c.guests.Add(colonist);
			c.guestCount++;

			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, 25, canSendMessage: true,
				canSendHostilityLetter: false);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelBetrothed".Translate(),
				"RimFeast_TextBetrothed".Translate(colonist.LabelShortCap, noble.LabelShortCap, c.house?.Name),
				LetterDefOf.PositiveEvent, colonist, c.house);
		}

		public const int ReqNone = 0;
		public const int ReqWeapon = 1;
		public const int ReqArt = 2;
		public const int ReqRival = 3;
		public const int ReqWard = 4;
		public const int ReqBed = 5;

		private static Pawn PartnerOf(Pawn p)
		{
			if (p?.relations == null) return null;
			Pawn q = p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Spouse)
				?? p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance)
				?? p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover);
			return q != null && q.Faction == Faction.OfPlayer && !q.Dead ? q : null;
		}

		// il faut une raison a l'affront: le vin, le mepris, ou un caractere qui s'en moque
		private static bool Brazen(Pawn speaker, Faction house)
		{
			if (FeastUtility.DrunkennessOf(speaker) > 0.3f) return true;
			if (house != null && house.PlayerGoodwill <= 0) return true;
			TraitSet t = speaker.story?.traits;
			return t != null && (t.HasTrait(TraitDefOf.Abrasive) || t.HasTrait(TraitDefOf.Psychopath)
				|| t.HasTrait(TraitDefOf.Bloodlust));
		}

		// un page entre en service vers sept ans et rentre chez lui quand il devient assez
		// grand pour porter les armes. vanilla fait passer Child a Adult a treize ans
		private const int WardMinAge = 8;
		private const int WardReturnAge = 13;

		// le plus haut rang de la table. le chef de maison garde sa vraie faction quand il
		// vient en personne, c'est a ca qu'on le reconnait parmi les figurants
		private static Pawn SpeakerOf(FeastCase c)
		{
			if (c.lord == null) return null;
			return c.lord.ownedPawns
				.Where(p => !p.Dead && p.Spawned && !p.Downed)
				.OrderByDescending(p => p.Faction == c.house ? 99999f : p.kindDef?.combatPower ?? 0f)
				.FirstOrDefault();
		}

		// une lame qui dort dans un coffre, jamais celle qu'un colon porte a la ceinture:
		// l'equipement n'est pas pose sur la carte, donc listerThings ne le voit pas
		private static Thing BestGiftWeapon(Map map)
		{
			Thing best = null;
			float bestValue = 0f;
			List<Thing> weapons = map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon);
			for (int i = 0; i < weapons.Count; i++)
			{
				Thing t = weapons[i];
				if (!t.Spawned || t.Position.Fogged(map)) continue;
				if (!map.areaManager.Home[t.Position] && !t.IsInAnyStorage()) continue;
				float v = t.MarketValue;
				if (v > bestValue) { bestValue = v; best = t; }
			}
			// en dessous ce n'est plus un present, c'est une insulte
			return bestValue >= 100f ? best : null;
		}

		// il demande ce qu'il a sous les yeux depuis le debut du repas. le cout mord sur la
		// beaute de la salle, donc sur le score de tes prochains banquets
		private static Thing BestGiftArt(FeastCase c)
		{
			Thing best = null;
			float bestValue = 0f;
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(c.spotCell, c.map);
			List<Thing> built = c.map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
			for (int i = 0; i < built.Count; i++)
			{
				Thing t = built[i];
				if (!t.Spawned || t.Faction != Faction.OfPlayer) continue;
				// Active = l'oeuvre a bien une histoire attachee, donc un titre a citer
				if (t.TryGetComp<CompArt>()?.Active != true) continue;
				if (!area.Contains(t.Position)) continue;
				float v = t.MarketValue;
				if (v > bestValue) { bestValue = v; best = t; }
			}
			return bestValue >= 100f ? best : null;
		}

		// on ne retient que les faveurs qui ont un sens ce soir-la: pas d'arme a offrir si
		// la forge est vide, pas d'oeuvre a ceder si la salle est nue
		// forceKind sert au debug: on ne garde que cette faveur, et on echoue franchement si
		// elle n'a rien a viser ce soir-la plutot que d'en servir une autre en douce
		private static bool PickRequest(FeastCase c, Pawn speaker, int forceKind = ReqNone)
		{
			var options = new List<int>();

			Thing weapon = BestGiftWeapon(c.map);
			if (weapon != null) options.Add(ReqWeapon);

			Thing art = BestGiftArt(c);
			if (art != null) options.Add(ReqArt);

			// il reclame le plus grand: c'est celui qui reviendra le plus tot, et un page de
			// douze ans est bien plus credible qu'un gamin de quatre
			Pawn child = c.map.mapPawns.FreeColonistsSpawned
				.Where(p => p.DevelopmentalStage == DevelopmentalStage.Child
					&& p.ageTracker.AgeBiologicalYears >= WardMinAge
					&& !p.IsQuestLodger()
					&& !QuestUtility.IsReservedByQuestOrQuestBeingGenerated(p))
				.OrderByDescending(p => p.ageTracker.AgeBiologicalYears)
				.FirstOrDefault();
			// on ne demande pas un enfant a une colonie de deux personnes
			if (child != null && c.map.mapPawns.FreeColonistsSpawnedCount >= 4) options.Add(ReqWard);

			// seul son gout a lui ouvre la demande. ce qu'elle en pense se joue apres, et
			// c'est elle qui le dira. on vise en priorite quelqu'un de deja pris: l'affront
			// n'a de sel que devant un mari
			Pawn desired = null;
			if (Brazen(speaker, c.house) || forceKind == ReqBed)
				desired = c.map.mapPawns.FreeColonistsSpawned
					.Where(p => p.DevelopmentalStage.Adult() && !p.Downed && !p.InMentalState
						&& RelationsUtility.AttractedToGender(speaker, p.gender))
					.InRandomOrder()
					.OrderByDescending(p => PartnerOf(p) != null ? 1 : 0)
					.FirstOrDefault();
			if (desired != null) options.Add(ReqBed);

			// on lui demande de renier celui qu'il estime le plus: sans ca, promettre ne coute rien
			Faction rival = FeastUtility.InvitableHouses()
				.Where(f => f != c.house && f.PlayerGoodwill >= 0)
				.OrderByDescending(f => f.PlayerGoodwill)
				.FirstOrDefault();
			if (rival != null) options.Add(ReqRival);

			if (forceKind != ReqNone)
			{
				if (!options.Contains(forceKind)) return false;
				c.requestKind = forceKind;
			}
			else
			{
				if (options.Count == 0) return false;
				c.requestKind = options.RandomElement();
			}
			c.requestThing = c.requestKind == ReqWeapon ? weapon : c.requestKind == ReqArt ? art : null;
			c.requestChild = c.requestKind == ReqWard ? child : null;
			c.requestTarget = c.requestKind == ReqBed ? desired : null;
			c.requestRival = c.requestKind == ReqRival ? rival : null;
			return true;
		}

		// dans combien de temps il repassera la porte. calcule sur son age reel, pas un delai
		// fixe: envoyer un grand de douze ans est un pret d'un an, un petit de huit un pari
		private static int WardReturnTick(Pawn child)
		{
			long toAge = (long)WardReturnAge * GenDate.TicksPerYear - child.ageTracker.AgeBiologicalTicks;
			// on ne se separe pas d'un enfant pour deux mois
			return Find.TickManager.TicksGame + (int)Mathf.Max(toAge, GenDate.TicksPerYear);
		}

		private static int WardYears(Pawn child) =>
			Mathf.Max(1, Mathf.RoundToInt(
				(WardReturnTick(child) - Find.TickManager.TicksGame) / (float)GenDate.TicksPerYear));

		// le titre de l'oeuvre plutot que "grande sculpture": c'est lui qu'un seigneur cite
		private static string ArtLabel(Thing t) =>
			t.TryGetComp<CompArt>()?.Title.NullOrEmpty() == false
				? t.TryGetComp<CompArt>().Title
				: t.LabelCap;

		// a mi-soiree: assez tard pour qu'on ait bu et parle, assez tot pour laisser le
		// temps de repondre avant que le cortege se leve
		private void TryRequest(FeastCase c)
		{
			if (c.requestRolled || c.lord == null) return;
			if (!RimFeastMod.S.requestsEnabled) return;
			if (Find.TickManager.TicksGame - c.arrivedTick < FeastDurationTicks / 2) return;

			// un seul tirage par banquet, quel qu'en soit le resultat
			c.requestRolled = true;
			if (!Rand.Chance(RimFeastMod.S.requestChance)) return;

			// l'orateur d'abord: certaines demandes dependent de qui se leve et de son etat
			Pawn speaker = SpeakerOf(c);
			if (speaker == null) return;
			c.requestSpeaker = speaker;
			if (!PickRequest(c, speaker)) { c.requestSpeaker = null; return; }
			SendRequestLetter(c, speaker);
		}

		private void SendRequestLetter(FeastCase c, Pawn speaker)
		{
			string who = speaker.LabelShortCap;
			string house = c.house?.Name;
			TaggedString text =
				c.requestKind == ReqWeapon
					? "RimFeast_TextRequestWeapon".Translate(who, house, c.requestThing.LabelCap)
				: c.requestKind == ReqArt
					? "RimFeast_TextRequestArt".Translate(who, house, ArtLabel(c.requestThing))
				: c.requestKind == ReqWard
					? "RimFeast_TextRequestWard".Translate(who, house,
						c.requestChild.LabelShortCap, WardYears(c.requestChild))
				: c.requestKind == ReqBed
					? (PartnerOf(c.requestTarget) != null
						? "RimFeast_TextRequestBedTaken".Translate(who, house,
							c.requestTarget.LabelShortCap, PartnerOf(c.requestTarget).LabelShortCap)
						: "RimFeast_TextRequestBed".Translate(who, house, c.requestTarget.LabelShortCap))
					: "RimFeast_TextRequestRival".Translate(who, house, c.requestRival.Name);

			var letter = (ChoiceLetter_FeastRequest)LetterMaker.MakeLetter(
				"RimFeast_LabelRequest".Translate(), text, RimFeastDefOf.RimFeast_FeastRequest,
				new LookTargets(speaker), c.house);
			letter.caseId = c.id;
			letter.StartTimeout(Mathf.Max(2500,
				c.arrivedTick + FeastDurationTicks - Find.TickManager.TicksGame));
			Find.LetterStack.ReceiveLetter(letter);
		}

		// revalide a l'ouverture: entre la demande et le clic, l'arme a pu bruler et le
		// prisonnier mourir ou s'evader
		public bool RequestPending(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id || c.requestKind == ReqNone) return false;
			if (c.state != FeastCase.Feasting || c.lord == null) return false;
			if (c.requestKind == ReqWeapon || c.requestKind == ReqArt)
				return c.requestThing != null && !c.requestThing.Destroyed && c.requestThing.Spawned;
			if (c.requestKind == ReqWard)
				return c.requestChild != null && !c.requestChild.Dead && c.requestChild.Spawned
					&& c.requestChild.Faction == Faction.OfPlayer && !c.requestChild.Downed;
			if (c.requestKind == ReqBed)
				return c.requestTarget != null && !c.requestTarget.Dead && c.requestTarget.Spawned
					&& c.requestTarget.Faction == Faction.OfPlayer && !c.requestTarget.Downed
					&& c.requestSpeaker != null && !c.requestSpeaker.Dead && c.requestSpeaker.Spawned;
			return c.requestRival != null && !c.requestRival.defeated;
		}

		// outil debug: sort la faveur demandee maintenant, sans attendre la mi-soiree ni le de
		public void DebugForceRequest(int kind)
		{
			FeastCase c = current;
			if (c == null || c.state != FeastCase.Feasting || c.lord == null)
			{
				Messages.Message("RimFeast: no feast at the table right now.",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			Pawn speaker = SpeakerOf(c);
			if (speaker == null) return;

			c.requestRolled = true;
			c.requestSpeaker = speaker;
			if (!PickRequest(c, speaker, kind))
			{
				Messages.Message("RimFeast: that request has nothing to aim at right now.",
					MessageTypeDefOf.RejectInput, historical: false);
				c.requestSpeaker = null;
				return;
			}
			SendRequestLetter(c, speaker);
		}

		// outil debug: fait rentrer tous les pupilles tout de suite
		public void DebugRushWards()
		{
			if (wards.Count == 0)
			{
				Messages.Message("RimFeast: no ward away.", MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			foreach (PendingWard w in wards) w.returnTick = Find.TickManager.TicksGame;
		}

		public string DebugWardReport()
		{
			if (wards.Count == 0) return "RimFeast: no ward away.";
			var sb = new StringBuilder();
			sb.AppendLine("RimFeast wards abroad:");
			foreach (PendingWard w in wards)
				sb.AppendLine("  " + (w.ward?.LabelShortCap ?? "?").PadRight(16)
					+ " age " + (w.ward?.ageTracker.AgeBiologicalYears ?? -1)
					+ ", with " + (w.house?.Name ?? "?")
					+ ", home in " + ((w.returnTick - Find.TickManager.TicksGame)
						/ (float)GenDate.TicksPerYear).ToString("0.0") + " years"
					+ ", pinned=" + (w.ward != null && Find.WorldPawns.ForcefullyKeptPawns.Contains(w.ward)));
			return sb.ToString();
		}

		// "accorder" ne veut rien dire quand la reponse ne t'appartient pas: la lettre change
		// son libelle en "le laisser lui demander lui-meme"
		public bool RequestIsBed(int id)
		{
			FeastCase c = current;
			return c != null && c.id == id && c.requestKind == ReqBed;
		}

		public void AcceptRequest(int id)
		{
			if (!RequestPending(id)) return;
			FeastCase c = current;
			int goodwill;
			TaggedString done;

			if (c.requestKind == ReqWeapon)
			{
				string label = c.requestThing.LabelCap;
				// une belle lame vaut mieux qu'un couperet rouille
				goodwill = Mathf.Clamp(Mathf.RoundToInt(c.requestThing.MarketValue / 50f), 8, 20);
				c.requestThing.Destroy();
				done = "RimFeast_MessageRequestWeaponDone".Translate(label, c.house?.Name);
			}
			else if (c.requestKind == ReqArt)
			{
				string label = ArtLabel(c.requestThing);
				// ceder une piece de sa salle coute plus qu'une lame de reserve: la salle
				// perd de sa beaute, et c'est un pilier du score des banquets suivants
				goodwill = Mathf.Clamp(Mathf.RoundToInt(c.requestThing.MarketValue / 50f), 10, 22);
				c.requestThing.Destroy();
				done = "RimFeast_MessageRequestArtDone".Translate(label, c.house?.Name);
			}
			else if (c.requestKind == ReqWard)
			{
				Pawn kid = c.requestChild;
				string label = kid.LabelShortCap;
				int years = WardYears(kid);
				int back = WardReturnTick(kid);

				// meme depart que la promise: on le sort de son lord avant le changement de
				// faction, puis il repart dans le cortege
				if (kid.Drafted) kid.drafter.Drafted = false;
				kid.jobs?.StopAll();
				kid.GetLord()?.Notify_PawnLost(kid, PawnLostCondition.LeftVoluntarily);
				kid.SetFaction(c.house);
				c.lord.AddPawn(kid);

				// epingle avant meme qu'il quitte la carte: des qu'il passe cote monde, le
				// ramasse-miettes le verra deja marque ForceKept et n'y touchera pas
				Find.WorldPawns.ForcefullyKeptPawns.Add(kid);
				wards.Add(new PendingWard
				{
					ward = kid, house = c.house, map = c.map,
					leftTick = Find.TickManager.TicksGame, returnTick = back,
				});

				goodwill = 25;
				done = "RimFeast_MessageRequestWardDone".Translate(label, c.house?.Name, years);
			}
			else if (c.requestKind == ReqBed)
			{
				// tu l'autorises a demander, tu ne reponds pas a sa place. le pari se joue ici
				c.requestKind = ReqNone;
				ResolveBedRequest(c);
				return;
			}
			else
			{
				Faction rival = c.requestRival;
				rival.TryAffectGoodwillWith(Faction.OfPlayer, -20, canSendMessage: true,
					canSendHostilityLetter: true);
				goodwill = 15;
				done = "RimFeast_MessageRequestRivalDone".Translate(rival.Name, c.house?.Name);
			}

			c.requestKind = ReqNone;
			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, goodwill, canSendMessage: true,
				canSendHostilityLetter: false);
			Messages.Message(done, new TargetInfo(c.spotCell, c.map),
				MessageTypeDefOf.PositiveEvent, historical: false);
		}

		// la premiere victime est designee, le bourreau est le colon le plus proche d'elle.
		// vit ici et plus dans le gizmo: la lettre de requete doit pouvoir la declencher aussi
		public bool OrderSlaughter(Pawn victim)
		{
			FeastCase c = current;
			if (c == null || victim == null || c.map == null) return false;

			Pawn assassin = c.map.mapPawns.FreeColonistsSpawned
				.Where(p => !p.Downed && !p.InMentalState
					&& LordJob_FeastColonists.FitForTrap(p)
					&& p.CurJobDef != RimFeastDefOf.RimFeast_AssassinateJob)
				.OrderBy(p => (p.Position - victim.Position).LengthHorizontalSquared)
				.FirstOrDefault();
			if (assassin == null)
			{
				Messages.Message("RimFeast_MessageNoAssassin".Translate(),
					MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}

			c.slaughterOrdered = true;
			// la chanson part maintenant, pas au premier sang: le temps que le bourreau
			// traverse la salle, la salle a compris
			StartRedWeddingMusic(c);

			Job job = JobMaker.MakeJob(RimFeastDefOf.RimFeast_AssassinateJob, victim);
			// une cible en fuite sprinte: l'assassin aussi, sinon la chasse est perdue d'avance
			job.locomotionUrgency = LocomotionUrgency.Sprint;
			assassin.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			Messages.Message("RimFeast_MessageSignalGiven".Translate(assassin.LabelShortCap, victim.LabelShortCap),
				victim, MessageTypeDefOf.NeutralEvent, historical: false);
			return true;
		}

		// on ne repond par l'acier que si on est venu pour ca: sur un banquet ordinaire les
		// enfants et les pacifistes sont a table, et l'invitation piege n'aurait plus de sens
		public bool CanAnswerInSteel(int id)
		{
			FeastCase c = current;
			return c != null && c.id == id && c.planned && c.requestKind != ReqNone
				&& c.state == FeastCase.Feasting
				&& c.requestSpeaker != null && !c.requestSpeaker.Dead && c.requestSpeaker.Spawned;
		}

		// il s'est leve devant toute la salle pour demander. il ne se rassiera pas
		public void AnswerInSteel(int id)
		{
			if (!CanAnswerInSteel(id)) return;
			FeastCase c = current;
			Pawn target = c.requestSpeaker;
			if (!OrderSlaughter(target)) return;
			c.requestKind = ReqNone;
			Messages.Message("RimFeast_MessageAnsweredInSteel".Translate(target.LabelShortCap),
				target, MessageTypeDefOf.NeutralEvent, historical: false);
		}

		// deux places, libre, atteignable. on prefere un lit que personne ne possede: il n'y a
		// alors ni delogement ni restitution a faire
		private static Building_Bed FindLovinBed(Map map, Pawn user)
		{
			Building_Bed best = null;
			int bestScore = -1;
			foreach (Building_Bed bed in map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>())
			{
				if (!bed.Spawned || bed.Medical || bed.ForPrisoners) continue;
				if (bed.SleepingSlotsCount < 2 || bed.AnyOccupants) continue;
				if (bed.IsForbidden(user)) continue;
				if (!user.CanReserve(bed, bed.SleepingSlotsCount, 0)) continue;
				if (!user.CanReach(bed, PathEndMode.OnCell, Danger.Deadly)) continue;
				int score = bed.OwnersForReading.Count == 0 ? 2 : 1;
				if (score > bestScore) { bestScore = score; best = bed; }
			}
			return best;
		}

		// on joue la scene pour de vrai quand la colonie a un lit pour ca. le driver vanilla
		// revendique le lit au passage et delaisse son proprietaire, alors on note qui l'avait
		// et on le lui rend a la fin du job. sans lit trouvable, l'ellipse suffira
		private static void TryStageLovin(FeastCase c, Pawn her, Pawn him)
		{
			Building_Bed bed = FindLovinBed(c.map, him);
			if (bed == null) return;

			List<Pawn> owners = bed.OwnersForReading.ToList();
			Job job = JobMaker.MakeJob(JobDefOf.Lovin, her, bed);
			him.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			if (him.CurJobDef != JobDefOf.Lovin) return;

			him.jobs.curDriver?.AddFinishAction(delegate(JobCondition _)
			{
				if (bed.Destroyed) return;
				foreach (Pawn p in bed.OwnersForReading.ToList()) p.ownership?.UnclaimBed();
				foreach (Pawn p in owners)
					if (p != null && !p.Dead) p.ownership?.ClaimBedIfNonMedical(bed);
			});
		}

		// c'est elle qui repond. l'orientation tranche d'abord, le couple pese lourd, et le
		// vin fait le reste. accepter rapporte gros, se faire rembarrer coute plus cher que
		// d'avoir refuse tout de suite: laisser demander est un pari, pas une facilite
		private void ResolveBedRequest(FeastCase c)
		{
			Pawn her = c.requestTarget;
			Pawn him = c.requestSpeaker;
			Pawn partner = PartnerOf(her);

			float chance = 0.5f;
			if (!RelationsUtility.AttractedToGender(her, him.gender)) chance = 0f;
			else
			{
				if (partner != null) chance -= 0.35f;
				chance += FeastUtility.DrunkennessOf(her) * 0.4f;
				if (her.story?.traits?.HasTrait(TraitDefOf.Psychopath) == true) chance += 0.1f;
			}

			if (!Rand.Chance(Mathf.Clamp01(chance)))
			{
				c.house?.TryAffectGoodwillWith(Faction.OfPlayer, -10, canSendMessage: true,
					canSendHostilityLetter: false);
				Messages.Message("RimFeast_MessageBedRefused".Translate(her.LabelShortCap, c.house?.Name),
					her, MessageTypeDefOf.NegativeEvent, historical: false);
				return;
			}

			her.needs?.mood?.thoughts?.memories?.TryGainMemory(RimFeastDefOf.RimFeast_NightWithNoble);
			// pas de rupture: une nuit de banquet ne dissout pas un mariage, mais il l'apprendra
			partner?.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.CheatedOnMe, her);
			TryStageLovin(c, her, him);

			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, 18, canSendMessage: true,
				canSendHostilityLetter: false);
			Messages.Message("RimFeast_MessageBedAccepted".Translate(her.LabelShortCap, him.LabelShortCap),
				her, MessageTypeDefOf.PositiveEvent, historical: false);
		}

		public void RefuseRequest(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id) return;
			c.requestKind = ReqNone;
			// refuser devant ses chevaliers, ca ne s'oublie pas
			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, -8, canSendMessage: true,
				canSendHostilityLetter: false);
			Messages.Message("RimFeast_MessageRequestRefused".Translate(c.house?.Name),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.NegativeEvent, historical: false);
		}

		public void DeclineProposal(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id) return;
			Messages.Message("RimFeast_MessageProposalDeclined".Translate(c.house?.Name),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.NeutralEvent, historical: false);
		}

		// on separe les coqs avant que ca finisse a l'infirmerie
		private void BreakUpBrawl(FeastCase c)
		{
			if (!c.brawled || c.brawlTick < 0 || c.lord == null) return;
			if (Find.TickManager.TicksGame < c.brawlTick + BrawlBreakupTicks) return;
			foreach (Pawn p in c.lord.ownedPawns)
				if (p.InMentalState && p.MentalStateDef == MentalStateDefOf.SocialFighting)
					p.MentalState.RecoverFromState();
		}

		private void ApplyOutcome(FeastCase c)
		{
			int score = ComputeScore(c);

			string label, text;
			int goodwill;
			if (score >= TriumphScore)
			{
				label = "RimFeast_LabelFeastTriumph".Translate();
				text = "RimFeast_TextFeastTriumph".Translate(c.house?.Name, GiftSilver);
				goodwill = 25;
				DropGift(c);
			}
			else if (score >= GoodScore)
			{
				label = "RimFeast_LabelFeastGood".Translate();
				text = "RimFeast_TextFeastGood".Translate(c.house?.Name);
				goodwill = 15;
			}
			else if (score >= DecentScore)
			{
				label = "RimFeast_LabelFeastDecent".Translate();
				text = "RimFeast_TextFeastDecent".Translate(c.house?.Name);
				goodwill = 8;
			}
			else
			{
				label = "RimFeast_LabelFeastPoor".Translate();
				text = "RimFeast_TextFeastPoor".Translate(c.house?.Name)
					+ "\n\n" + "RimFeast_TextPoorTravels".Translate(c.house?.Name);
				goodwill = -5;
			}

			// la nouveaute s'emousse: le 4e banquet de la meme maison ne vaut plus le 1er.
			// calcule avant d'enregistrer celui-ci, sinon il se penaliserait lui-meme
			int remembered = FeastsRemembered(c.house);
			if (goodwill > 0 && remembered > 0)
			{
				goodwill = Mathf.Max(1, Mathf.RoundToInt(goodwill * GoodwillFactorFor(c.house)));
				text += "\n\n" + "RimFeast_TextJaded".Translate(c.house?.Name, remembered);
			}

			// recevoir le chef de maison en personne grandit l'honneur de la soiree, et la
			// honte d'une table maigre servie sous ses yeux
			if (c.leaderCame && goodwill != 0)
			{
				goodwill = Mathf.RoundToInt(goodwill * 1.5f);
				text += "\n\n" + (goodwill > 0 ? "RimFeast_TextLeaderHonored" : "RimFeast_TextLeaderShamed")
					.Translate(c.house?.Name);
			}

			// le meme bilan que le marqueur affichait pendant la soiree. sans lui le joueur
			// lit "banquet correct" sans jamais savoir ce qui a peche
			text += "\n\n" + HallReport(c);

			Find.LetterStack.ReceiveLetter(label, text,
				score >= DecentScore ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent,
				new TargetInfo(c.spotCell, c.map), c.house);

			if (goodwill != 0)
				c.house?.TryAffectGoodwillWith(Faction.OfPlayer, goodwill, canSendMessage: true,
					canSendHostilityLetter: goodwill < 0);

			HouseFeastMemory m = MemoryFor(c.house, true);
			if (m != null)
			{
				m.timesFeasted = remembered + 1;
				m.lastFeastTick = Find.TickManager.TicksGame;
			}

			// les affaires sont les affaires: l'accord ne s'emousse pas comme les relations.
			// c'est ce qui garde un interet au banquet quand la maison est deja au plafond
			if (score >= GoodScore && c.house != null && c.map != null)
				OfferTradePact(c, score >= TriumphScore);
		}

		private void OfferTradePact(FeastCase c, bool playerPicks)
		{
			if (!RimFeastMod.S.tradePactEnabled) return;
			// une faction sans marchands de caravane n'a rien a promettre
			if (c.house?.def?.caravanTraderKinds == null || c.house.def.caravanTraderKinds.Count == 0) return;
			PendingCaravan p = ScheduleCaravan(c.house, c.map);
			int delayTicks = p.fireTick - Find.TickManager.TicksGame;

			if (!playerPicks)
			{
				Find.LetterStack.ReceiveLetter("RimFeast_LabelPactOffered".Translate(),
					"RimFeast_TextPactOffered".Translate(c.house.Name),
					LetterDefOf.PositiveEvent, new TargetInfo(c.spotCell, c.map), c.house);
				return;
			}

			var letter = (ChoiceLetter_TradePact)LetterMaker.MakeLetter(
				"RimFeast_LabelPactChoice".Translate(),
				"RimFeast_TextPactChoice".Translate(c.house.Name),
				RimFeastDefOf.RimFeast_TradePact, c.house);
			letter.pactId = p.id;
			letter.house = c.house;
			letter.StartTimeout(delayTicks);
			Find.LetterStack.ReceiveLetter(letter);
		}

		private int ComputeScore(FeastCase c)
		{
			float score = 0f;

			score += Mathf.Clamp01(c.maxImpressiveness / ImpressivenessCap) * 20f;
			score += c.maxTableScore;
			score += Mathf.Min(c.maxVariety, 5) * 3f;

			if (c.guestCount > 0)
				score += Mathf.Min((float)c.maxDrinkUnits / c.guestCount, 1f) * 10f;
			if (c.drankAny) score += 5f;

			if (c.lord != null && c.guestCount > 0)
			{
				int fed = c.lord.ownedPawns.Count(p => p.Spawned && !p.Dead
					&& p.needs?.food != null && p.needs.food.CurLevelPercentage > 0.6f);
				score += (float)fed / c.guestCount * 15f;
			}

			// sans DLC aucun instrument n'existe: la musique est reversee au toast,
			// sinon ces joueurs seraient plafonnes pour une raison hors de leur controle
			bool music = FeastUtility.MusicPossible;
			score += Mathf.Min(c.toastsGiven / (float)ToastsForFull, 1f) * (music ? 10f : 15f);

			if (music)
			{
				// jouer la moitie du festin suffit: le menestrel doit marcher jusqu'a
				// l'instrument et sa jauge de loisir coupe le job par moments
				float ratio = c.samples > 0 ? (float)c.musicSamples / c.samples : 0f;
				score += Mathf.Min(ratio / 0.5f, 1f) * 5f;
			}

			if (c.brawled) score -= 10f;

			return Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
		}

		private void DropGift(FeastCase c)
		{
			int rest = GiftSilver;
			while (rest > 0)
			{
				Thing coin = ThingMaker.MakeThing(ThingDefOf.Silver);
				coin.stackCount = Mathf.Min(coin.def.stackLimit, rest);
				rest -= coin.stackCount;
				GenPlace.TryPlaceThing(coin, c.spotCell, c.map, ThingPlaceMode.Near);
			}
		}

		public override void ExposeData()
		{
			Scribe_Deep.Look(ref current, "current");
			Scribe_Collections.Look(ref memories, "memories", LookMode.Deep);
			Scribe_Collections.Look(ref caravans, "caravans", LookMode.Deep);
			Scribe_Collections.Look(ref raids, "raids", LookMode.Deep);
			Scribe_Collections.Look(ref reveals, "reveals", LookMode.Deep);
			Scribe_Collections.Look(ref wards, "wards", LookMode.Deep);
			Scribe_Values.Look(ref nextId, "nextId", 1);
			Scribe_Values.Look(ref redWeddingTick, "redWeddingTick", -1);
			Scribe_Values.Look(ref nextAwayInviteTick, "nextAwayInviteTick", -1);
			Scribe_References.Look(ref awayHouse, "awayHouse");
			Scribe_Values.Look(ref awayExpireTick, "awayExpireTick", -1);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (memories == null) memories = new List<HouseFeastMemory>();
				memories.RemoveAll(m => m == null || m.house == null);
				if (caravans == null) caravans = new List<PendingCaravan>();
				caravans.RemoveAll(x => x == null || x.house == null);
				if (raids == null) raids = new List<PendingRaid>();
				raids.RemoveAll(x => x == null || x.house == null);
				if (reveals == null) reveals = new List<PendingReveal>();
				reveals.RemoveAll(x => x == null || x.house == null);
				if (wards == null) wards = new List<PendingWard>();
				wards.RemoveAll(x => x == null || x.ward == null);
			}
		}
	}
}
