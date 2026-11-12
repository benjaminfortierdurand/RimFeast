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
	public class FeastCase : IExposable
	{
		public const int Invited = 0;
		public const int Traveling = 1;
		public const int Feasting = 2;
		public const int Fleeing = 3;
		public const int Leaving = 4;

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

		// requestRolled: le tirage a eu lieu, meme a vide
		public bool requestRolled;
		public int requestKind;
		public Thing requestThing;
		public Pawn requestChild;
		public Pawn requestSpeaker;
		public Pawn requestTarget;
		public Faction requestRival;
		public List<Pawn> guests = new List<Pawn>();
		public bool leaderCame;

		public List<Building_MusicalInstrument> hallInstruments;
		public int instrumentsTick = -1;
		public bool planned;
		public bool slaughterOrdered;
		public bool musicStarted;
		public int fleeStartTick = -1;
		public bool treacherous;
		public Pawn poisoner;
		public bool tainted;
		public int taintTick = -1;
		public bool exposed;
		public int sickBaseline;
		// noces: la maison revient marier les fiances
		public bool wedding;
		public bool wed;
		public int wedTick = -1;
		public bool firstDanceDone;
		public int ballUntilTick = -1;
		public int minstrelTick = -1;
		public List<Pawn> hosts;
		public int lastScore = -1;
		// banquet de la colonie tenu pour une occasion. le mort d'une veillee est note par son numero: sa reference peut disparaitre
		public int occasion;
		public int honoredId = -1;
		public string honoredName;
		public int contests;
		// grand banquet: plusieurs maisons dans le meme cortege. guestHouse suit guests rang pour rang
		public bool grand;
		public List<Faction> houses;
		public List<Faction> guestHouse;
		public List<Faction> toasted;
		public List<Faction> leaderHouses;
		public List<Faction> brawlHouses;
		public bool feudBrawl;

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
			Scribe_Values.Look(ref wedding, "wedding");
			Scribe_Values.Look(ref wed, "wed");
			Scribe_Values.Look(ref wedTick, "wedTick", -1);
			Scribe_Values.Look(ref firstDanceDone, "firstDanceDone");
			Scribe_Values.Look(ref ballUntilTick, "ballUntilTick", -1);
			Scribe_Collections.Look(ref hosts, "hosts", LookMode.Reference);
			Scribe_Values.Look(ref occasion, "occasion");
			Scribe_Values.Look(ref honoredId, "honoredId", -1);
			Scribe_Values.Look(ref honoredName, "honoredName");
			Scribe_Values.Look(ref contests, "contests");
			Scribe_Values.Look(ref grand, "grand");
			Scribe_Collections.Look(ref houses, "houses", LookMode.Reference);
			Scribe_Collections.Look(ref guestHouse, "guestHouse", LookMode.Reference);
			Scribe_Collections.Look(ref toasted, "toasted", LookMode.Reference);
			Scribe_Collections.Look(ref leaderHouses, "leaderHouses", LookMode.Reference);
			Scribe_Collections.Look(ref brawlHouses, "brawlHouses", LookMode.Reference);
			Scribe_Values.Look(ref feudBrawl, "feudBrawl");
			if (Scribe.mode == LoadSaveMode.PostLoadInit && grand)
			{
				if (houses == null) houses = new List<Faction>();
				if (guestHouse == null) guestHouse = new List<Faction>();
				if (toasted == null) toasted = new List<Faction>();
				if (leaderHouses == null) leaderHouses = new List<Faction>();
				if (brawlHouses == null) brawlHouses = new List<Faction>();
				houses.RemoveAll(h => h == null);
			}
			if (Scribe.mode == LoadSaveMode.PostLoadInit && guests == null)
				guests = new List<Pawn>();
		}
	}

	public class HouseFeastMemory : IExposable
	{
		public Faction house;
		public int timesFeasted;
		public int lastFeastTick = -1;
		public bool betrayed;

		public void ExposeData()
		{
			Scribe_References.Look(ref house, "house");
			Scribe_Values.Look(ref timesFeasted, "timesFeasted");
			Scribe_Values.Look(ref lastFeastTick, "lastFeastTick", -1);
			Scribe_Values.Look(ref betrayed, "betrayed");
		}
	}

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

	// epingle dans ForcefullyKeptPawns, sinon le menage des pawns du monde l'efface
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

	// des fiancailles en attente de noces. le noble est epingle comme une pupille: entre les deux banquets il n'est sur aucune carte
	public class PendingWedding : IExposable
	{
		public Faction house;
		public Pawn noble;
		public Pawn colonist;
		public Thing spot;
		public int fireTick;

		public void ExposeData()
		{
			Scribe_References.Look(ref house, "house");
			Scribe_References.Look(ref noble, "noble");
			Scribe_References.Look(ref colonist, "colonist");
			Scribe_References.Look(ref spot, "spot");
			Scribe_Values.Look(ref fireTick, "fireTick");
		}
	}

	// hors du dossier du banquet: le cortege peut partir avant la fin, les lits doivent etre rendus
	public class LovinScene : IExposable
	{
		public const int Pending = 0;
		public const int Running = 1;

		public int caseId;
		public int state;
		public int startTick = -1;
		public Pawn her;
		public Pawn him;
		public Building_Bed bed;
		public List<Pawn> owners = new List<Pawn>();
		public Building_Bed herBed;

		public void ExposeData()
		{
			Scribe_Values.Look(ref caseId, "caseId");
			Scribe_Values.Look(ref state, "state");
			Scribe_Values.Look(ref startTick, "startTick", -1);
			Scribe_References.Look(ref her, "her");
			Scribe_References.Look(ref him, "him");
			Scribe_References.Look(ref bed, "bed");
			Scribe_Collections.Look(ref owners, "owners", LookMode.Reference);
			Scribe_References.Look(ref herBed, "herBed");
			if (Scribe.mode == LoadSaveMode.PostLoadInit && owners == null) owners = new List<Pawn>();
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
		private LovinScene lovin;
		private FeastCase colony;
		private int lastColonyFeastTick = -1;
		private List<PendingWedding> weddings = new List<PendingWedding>();
		private List<ChronicleEntry> chronicle = new List<ChronicleEntry>();
		private const int MaxChronicle = 80;
		private List<int> wakesHeld = new List<int>();
		private int lastVictoryThreatTick = -1;
		private int lastHarvestFeastTick = -1;
		private int lastGrandFeastTick = -1;
		// les querelles refermees a un grand banquet, une cle par paire de maisons
		private List<int> peace = new List<int>();

		private Faction awayHouse;
		private int awayExpireTick = -1;

		public bool debugForceLeader;
		public bool debugForceTreachery;
		public bool debugForcePlanned;

		// transient: apres un chargement en plein massacre la musique ne reprend pas
		private Sustainer redWeddingSound;

		private const int CheckInterval = 250;
		private const int BrawlGraceTicks = 5000;
		private const int BrawlBreakupTicks = 2500;

		public static int BaseInviteCost => RimFeastMod.S.inviteCost;
		public static int FeastDurationTicks => RimFeastMod.S.feastHours * 2500;
		private static int GiftSilver => RimFeastMod.S.giftSilver;

		private static int FadeTicks => RimFeastMod.S.fadeDays * 60000;

		private const int CaravanRetryTicks = 15000;
		private const int CaravanMaxTries = 8;

		// sans ce filet, des fuyards bloques gardent current occupe pour toujours
		private const int FleeBackstopTicks = 60000;

		private static int TreacheryGoodwillMax => RimFeastMod.S.treacheryGoodwillMax;
		private static float TreacheryChance =>
			RimFeastMod.S.treacheryEnabled ? RimFeastMod.S.treacheryChance : 0f;
		private const int TaintDelayTicks = 15000;

		public const int ToastDurationTicks = 1500;

		public static int ToastIntervalTicks => Mathf.Clamp(FeastDurationTicks / 4, 1500, 10000);

		private static int ToastsForFull =>
			Mathf.Clamp(FeastDurationTicks / (ToastIntervalTicks + ToastDurationTicks), 1, 3);

		private const int TriumphScore = 85;
		private const int GoodScore = 60;
		private const int DecentScore = 35;

		// 150 = entre extremely et unbelievably impressive
		private const float ImpressivenessCap = 150f;

		// en nutrition, pas en piles: le miel MO est fin a 0.05
		private const float ServingNutrition = 0.9f;
		private static readonly float[] TierValue = { 20f, 14f, 8f, 3f };

		public GameComponent_FeastState(Game game) { }

		public static GameComponent_FeastState Get() => Current.Game?.GetComponent<GameComponent_FeastState>();

		public bool Busy => current != null || colony != null;

		public FeastCase CurrentCase => current;

		public FeastCase ColonyCase => colony;

		public int FeastStartTick(IntVec3 spot)
		{
			if (current != null && current.state == FeastCase.Feasting && current.spotCell == spot) return current.arrivedTick;
			if (colony != null && colony.spotCell == spot) return colony.arrivedTick;
			return -1;
		}

		public bool SlaughterUnderway => current != null && current.slaughterOrdered;

		// pas de toast pendant la scene: EndAllJobs couperait le noble
		public bool LovinUnderway(int id) => lovin != null && lovin.caseId == id && lovin.state == LovinScene.Running;

		// vrai entre le moment ou un empoisonneur est demasque et le depart de son cortege
		private static bool anyExposed;

		// le cortege d'un empoisonneur demasque: ce qui lui arrive en sortant ne coute rien aupres de sa maison.
		// les invites sont de leur vraie faction, il faut donc le dire au jeu: HarmonyPatches/Patch_Faction_ExposedGuests
		public static bool FairGame(Pawn member)
		{
			if (!anyExposed || member == null) return false;
			FeastCase c = Get()?.current;
			return c != null && c.exposed && c.guests.Contains(member);
		}

		private static Faction HostileHouse(FeastCase c)
		{
			if (!c.grand || c.houses == null)
				return c.house != null && c.house.HostileTo(Faction.OfPlayer) ? c.house : null;
			for (int i = 0; i < c.houses.Count; i++)
				if (c.houses[i] != null && c.houses[i].HostileTo(Faction.OfPlayer)) return c.houses[i];
			return null;
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

		public int FeastsRemembered(Faction f)
		{
			HouseFeastMemory m = MemoryFor(f, false);
			if (m == null || m.timesFeasted <= 0) return 0;
			if (m.lastFeastTick < 0) return m.timesFeasted;
			int forgotten = (Find.TickManager.TicksGame - m.lastFeastTick) / FadeTicks;
			return Mathf.Max(0, m.timesFeasted - forgotten);
		}

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

				// forced saute le conteur, CanFireNow garde le veto d'un lord hostile
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

		public static string HallPreview(FeastCase c)
		{
			if (c?.map == null || c.house == null) return null;
			Map map = c.map;
			Room room = c.spotCell.GetRoom(map);
			float hall = room != null && !room.PsychologicallyOutdoors
				? room.GetStat(RoomStatDefOf.Impressiveness) : 0f;

			int expected = c.grand ? GrandExpected(c) : Mathf.Max(2, RimFeastMod.S.corteges
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

		public void CancelInvite(Thing spot)
		{
			FeastCase c = current;
			if (c == null || c.state != FeastCase.Invited || c.spotThing != spot) return;
			Messages.Message("RimFeast_MessageInviteCancelled".Translate(GuestsName(c)),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.NeutralEvent, historical: false);
			// un grand banquet annule ne coute pas l'annee
			if (c.grand) lastGrandFeastTick = -1;
			current = null;
		}

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
			sb.AppendLine("  house->player: " + c.house?.RelationKindWith(Faction.OfPlayer)
				+ "   goodwill: " + c.house?.PlayerGoodwill);

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

		public void DebugRush()
		{
			if (current == null)
			{
				colony?.colonistLord?.ReceiveMemo(LordJob_FeastColonists.MemoEnd);
				return;
			}
			if (current.state == FeastCase.Invited)
				current.eventTick = Find.TickManager.TicksGame;
			else if (current.state == FeastCase.Feasting)
				current.lord?.ReceiveMemo(LordJob_FeastGuests.MemoEndFeast);
		}

		// a 3h du matin personne ne vient a table: arrivee decalee en fin d'apres-midi
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

		public static string ArrivalEta(FeastCase c)
		{
			if (c == null || c.map == null || c.eventTick < 0) return null;
			float lon = Find.WorldGrid.LongLatOf(c.map.Tile).x;
			long shift = GenDate.LocalTicksOffsetFromLongitude(lon);
			int offset = Find.TickManager.TicksAbs - Find.TickManager.TicksGame;
			long arrival = c.eventTick + offset;

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

		private const int GrandMinHouses = 2;
		private const int GrandMaxHouses = 5;
		private const int GrandMaxGuests = 28;

		private static readonly string[] TierKeys =
		{
			"RimFeast_LabelFeastPoor", "RimFeast_LabelFeastDecent", "RimFeast_LabelFeastGood", "RimFeast_LabelFeastTriumph",
		};

		public static string TierLabel(int tier) => TierKeys[Mathf.Clamp(tier, 0, 3)].Translate();

		// le nom a donner aux invites dans un message: une maison, ou "les maisons nobles" au grand banquet
		public static string GuestsName(FeastCase c) =>
			c == null ? null : c.grand ? (string)"RimFeast_GrandGuests".Translate() : c.house?.Name;

		private static Faction HouseOfGuest(FeastCase c, Pawn p)
		{
			if (c.guestHouse != null)
			{
				int i = c.guests.IndexOf(p);
				if (i >= 0 && i < c.guestHouse.Count && c.guestHouse[i] != null) return c.guestHouse[i];
			}
			return c.house;
		}

		private static int PairKey(Faction a, Faction b) =>
			Gen.HashCombineInt(Mathf.Min(a.loadID, b.loadID), Mathf.Max(a.loadID, b.loadID));

		// une vieille querelle entre deux maisons: tiree une fois pour toutes dans ce monde, jusqu'a ce qu'un grand banquet la referme
		public bool Feud(Faction a, Faction b)
		{
			if (a == null || b == null || a == b) return false;
			int key = PairKey(a, b);
			if (peace.Contains(key)) return false;
			return a.HostileTo(b) || Rand.ValueSeeded(key ^ Find.World.info.Seed) < 0.25f;
		}

		private List<Pair<Faction, Faction>> Feuds(List<Faction> houses)
		{
			var list = new List<Pair<Faction, Faction>>();
			for (int i = 0; i < houses.Count; i++)
				for (int j = i + 1; j < houses.Count; j++)
					if (Feud(houses[i], houses[j])) list.Add(new Pair<Faction, Faction>(houses[i], houses[j]));
			return list;
		}

		private string FeudsText(List<Faction> houses)
		{
			List<Pair<Faction, Faction>> feuds = Feuds(houses);
			if (feuds.Count == 0) return null;
			return "RimFeast_GrandFeuds".Translate(feuds
				.Select(f => (string)"RimFeast_GrandFeudPair".Translate(f.First.Name, f.Second.Name))
				.ToCommaList(useAnd: true));
		}

		private static int GrandSize(Faction h, int houseCount)
		{
			int cap = Mathf.Clamp(GrandMaxGuests / Mathf.Max(1, houseCount), 3, 6);
			return Mathf.Clamp(3 + Mathf.RoundToInt(Mathf.Clamp(h.PlayerGoodwill, -100f, 100f) / 50f), 3, cap);
		}

		private static int GrandExpected(FeastCase c) =>
			c.houses == null ? 0 : c.houses.Where(h => h != null).Sum(h => GrandSize(h, c.houses.Count));

		// null si le grand banquet peut se tenir. les maisons sont rangees de celle qui vous estime le plus a celle qui vous estime le moins
		public string GrandBlocked(Map map, out List<Faction> houses, out int cost)
		{
			houses = FeastUtility.InvitableHouses().Where(h => !HouseRefuses(h, out _))
				.OrderByDescending(h => h.PlayerGoodwill).Take(GrandMaxHouses).ToList();
			cost = houses.Sum(h => InviteCostFor(h));
			if (Busy) return "RimFeast_InviteDisabledBusy".Translate();
			int wait = lastGrandFeastTick < 0 ? 0
				: lastGrandFeastTick + GenDate.TicksPerYear - Find.TickManager.TicksGame;
			if (wait > 0) return "RimFeast_GrandCooldown".Translate(Mathf.CeilToInt(wait / (float)GenDate.TicksPerDay));
			if (houses.Count < GrandMinHouses) return "RimFeast_GrandNeedHouses".Translate(GrandMinHouses);
			return null;
		}

		public string GrandConfirmText(List<Faction> houses, int cost, bool planned)
		{
			TaggedString text = (planned ? "RimFeast_GrandConfirmPlanned" : "RimFeast_GrandConfirm").Translate(
				houses.Select(h => h.Name).ToCommaList(useAnd: true), cost);
			string feuds = FeudsText(houses);
			if (feuds != null) text += "\n\n" + feuds;
			return text;
		}

		// un dossier comme les autres, a plusieurs maisons. pas de demande en mariage ni de requete a table: trop de maisons a la fois
		public void StartGrandInvite(Map map, Thing spot, bool planned, bool force = false)
		{
			if (map == null || spot == null || Busy) return;
			string blocked = GrandBlocked(map, out List<Faction> houses, out _);
			if ((blocked != null && !force) || houses.Count < 2) return;
			planned = planned || debugForcePlanned;
			debugForcePlanned = false;
			current = new FeastCase
			{
				id = nextId++,
				map = map,
				house = houses[0],
				spotCell = spot.Position,
				spotThing = spot,
				state = FeastCase.Invited,
				eventTick = ArrivalTick(map, Rand.Range(45000, 75000)),
				planned = planned,
				grand = true,
				houses = houses,
				guestHouse = new List<Faction>(),
				toasted = new List<Faction>(),
				leaderHouses = new List<Faction>(),
				brawlHouses = new List<Faction>(),
				proposalDone = true,
				requestRolled = true,
			};
			lastGrandFeastTick = Find.TickManager.TicksGame;

			TaggedString body = (planned ? "RimFeast_TextGrandSentPlanned" : "RimFeast_TextGrandSent").Translate(
				houses.Count, houses.Select(h => h.Name).ToCommaList(useAnd: true));
			string eta = ArrivalEta(current);
			if (!eta.NullOrEmpty()) body += "\n\n" + eta;
			Find.LetterStack.ReceiveLetter("RimFeast_LabelGrandSent".Translate(), body,
				LetterDefOf.PositiveEvent, new TargetInfo(current.spotCell, map));
		}

		private static IntVec3 GrandEntry(Map map, int i)
		{
			if (CellFinder.TryFindRandomEdgeCellWith(
				x => x.Standable(map) && !x.Fogged(map) && map.reachability.CanReachColony(x), map, new Rot4(i % 4),
				CellFinder.EdgeRoadChance_Neutral, out IntVec3 cell))
				return cell;
			return RCellFinder.TryFindRandomPawnEntryCell(out cell, map, CellFinder.EdgeRoadChance_Neutral)
				? cell : CellFinder.RandomEdgeCell(map);
		}

		private static void Welcome(Pawn p, Map map, IntVec3 entry)
		{
			if (p.needs?.food != null) p.needs.food.CurLevel = 0.35f * p.needs.food.MaxLevel;
			if (p.needs?.rest != null) p.needs.rest.CurLevel = Mathf.Max(p.needs.rest.CurLevel, 0.8f);
			GenSpawn.Spawn(p, CellFinder.RandomClosewalkCellNear(entry, map, 6), map);
		}

		// un vrai membre de sa maison, comme un visiteur du jeu: ce qu'on lui fait, le jeu le compte a la maison
		private static Pawn MakeGuest(PawnKindDef kind, Faction house, Map map) =>
			PawnGenerator.GeneratePawn(new PawnGenerationRequest(
				kind, house, PawnGenerationContext.NonPlayer,
				tile: map.Tile, forceGenerateNewPawn: true, canGeneratePawnRelations: false));

		// chaque maison entre par un bord different et toutes convergent vers la salle.
		// un seul cortege et un seul lord, au nom de la premiere maison: tout le reste du banquet marche tel quel
		private void SpawnGrandCortege(FeastCase c)
		{
			Map map = c.map;
			c.houses.RemoveAll(h => h == null || h.defeated || h.HostileTo(Faction.OfPlayer));
			if (c.houses.Count < 2)
			{
				Messages.Message("RimFeast_MessageInviteVoid".Translate(GuestsName(c)), MessageTypeDefOf.NegativeEvent);
				current = null;
				return;
			}
			c.house = c.houses[0];

			var guests = new List<Pawn>();
			var heads = new List<string>();
			c.guestHouse = new List<Faction>();
			Pawn speaker = null;
			for (int i = 0; i < c.houses.Count; i++)
			{
				Faction h = c.houses[i];
				PawnKindDef crew = FeastUtility.CrewKindFor(h);
				PawnKindDef lordKind = FeastUtility.HouseLordKind(h) ?? FeastUtility.HouseKnightKind(h)
					?? FeastUtility.EliteKindFor(h) ?? crew;
				PawnKindDef standard = FeastUtility.HouseStandardKind(h) ?? crew;
				PawnKindDef knight = FeastUtility.HouseKnightKind(h) ?? FeastUtility.EliteKindFor(h) ?? crew;
				if (lordKind == null) continue;
				var kinds = new List<PawnKindDef> { lordKind };
				if (standard != null) kinds.Add(standard);
				PawnKindDef filler = crew ?? knight ?? lordKind;
				int size = GrandSize(h, c.houses.Count);
				while (kinds.Count < size) kinds.Add(kinds.Count % 2 == 0 ? (knight ?? filler) : filler);

				// le vrai chef garde sa vraie faction, pas une copie
				Pawn leader = RimFeastMod.S.leaderEnabled && h.PlayerGoodwill >= 40 && FeastsRemembered(h) >= 1
					&& Rand.Chance(0.6f) ? TakeLeader(h) : null;
				if (leader != null) kinds.Remove(lordKind);

				IntVec3 entry = GrandEntry(map, i);
				Pawn first = null;
				foreach (PawnKindDef kind in kinds)
				{
					Pawn p = MakeGuest(kind, h, map);
					Welcome(p, map, entry);
					guests.Add(p);
					c.guestHouse.Add(h);
					if (first == null) first = p;
				}
				if (leader != null)
				{
					Welcome(leader, map, entry);
					guests.Add(leader);
					c.guestHouse.Add(h);
					c.leaderHouses.Add(h);
					heads.Add("RimFeast_GrandHead".Translate(leader.LabelShortCap, h.Name));
					first = leader;
				}
				if (speaker == null) speaker = first;
			}
			if (guests.Count == 0)
			{
				current = null;
				return;
			}

			c.lord = LordMaker.MakeNewLord(c.house,
				new LordJob_FeastGuests(c.id, c.house, c.spotCell, speaker ?? guests[0]), map, guests);
			c.guests = guests;
			c.guestCount = guests.Count;
			c.leaderCame = c.leaderHouses.Count > 0;
			c.state = FeastCase.Traveling;

			TaggedString text = "RimFeast_TextGrandArrive".Translate(c.houses.Count, guests.Count,
				c.houses.Select(h => h.Name).ToCommaList(useAnd: true));
			if (heads.Count > 0) text += "\n\n" + string.Join("\n", heads);
			string feuds = FeudsText(c.houses);
			if (feuds != null) text += "\n\n" + feuds;
			Find.LetterStack.ReceiveLetter("RimFeast_LabelGrandArrive".Translate(), text,
				LetterDefOf.PositiveEvent, guests[0]);
		}

		// au grand banquet les toasts tournent, la maison qui vous estime le plus d'abord. celles qu'on oublie s'en souviennent
		private static string ToastedName(FeastCase c)
		{
			if (!c.grand || c.houses == null || c.houses.Count == 0) return c.house?.Name;
			Faction h = c.houses[(Mathf.Max(1, c.toastsGiven) - 1) % c.houses.Count];
			if (h != null && !c.toasted.Contains(h)) c.toasted.Add(h);
			return h?.Name;
		}

		private static void Blame(FeastCase c, int goodwill)
		{
			if (c.grand && c.houses != null)
				foreach (Faction h in c.houses)
					h?.TryAffectGoodwillWith(Faction.OfPlayer, goodwill, canSendMessage: false, canSendHostilityLetter: false);
			else
				c.house?.TryAffectGoodwillWith(Faction.OfPlayer, goodwill, canSendMessage: true, canSendHostilityLetter: false);
		}

		private void GrandBrawl(FeastCase c, Pawn a, Pawn b)
		{
			Faction ha = HouseOfGuest(c, a);
			Faction hb = HouseOfGuest(c, b);
			if (ha != null && !c.brawlHouses.Contains(ha)) c.brawlHouses.Add(ha);
			if (hb != null && !c.brawlHouses.Contains(hb)) c.brawlHouses.Add(hb);
			if (!Feud(ha, hb)) return;
			c.feudBrawl = true;
			Note(c, ChronicleLine.FeudBrawl, ha.Name, hb.Name);
			Messages.Message("RimFeast_GrandFeudBrawl".Translate(ha.Name, hb.Name), new TargetInfo(c.spotCell, c.map),
				MessageTypeDefOf.NegativeEvent, historical: false);
		}

		// deux maisons en froid dans la meme salle: la rixe n'attend pas que quelqu'un soit ivre
		private void TryFeudBrawl(FeastCase c)
		{
			if (!c.grand || c.brawled || c.lord == null) return;
			if (Find.TickManager.TicksGame - c.arrivedTick < BrawlGraceTicks || !Rand.Chance(0.009f)) return;
			FeastUtility.FeastArea hall = FeastUtility.FeastArea.For(c.spotCell, c.map);
			foreach (Pawn a in c.lord.ownedPawns.InRandomOrder().ToList())
			{
				if (!a.Spawned || a.Downed || a.interactions == null || !hall.Contains(a.Position)) continue;
				Faction ha = HouseOfGuest(c, a);
				Pawn b = c.lord.ownedPawns
					.Where(p => p != a && p.Spawned && !p.Downed && hall.Contains(p.Position)
						&& Feud(ha, HouseOfGuest(c, p)) && a.interactions.SocialFightPossible(p))
					.RandomElementWithFallback();
				if (b == null) continue;
				a.interactions.StartSocialFight(b, "RimFeast_MessageBrawl");
				Note(c, ChronicleLine.Brawl, Who(a), Who(b));
				c.brawled = true;
				c.brawlTick = Find.TickManager.TicksGame;
				c.brawlerA = a;
				c.brawlerB = b;
				GrandBrawl(c, a, b);
				return;
			}
		}

		// la note d'une maison: la salle et la table sont a tout le monde, mais ses gens ont-ils mange,
		// l'a-t-on saluee d'un toast, s'est-elle battue
		private int ScoreFor(FeastCase c, Faction h)
		{
			float score = Mathf.Clamp01(c.maxImpressiveness / ImpressivenessCap) * 20f + c.maxTableScore
				+ Mathf.Min(c.maxVariety, 5) * 3f;
			if (c.guestCount > 0) score += Mathf.Min((float)c.maxDrinkUnits / c.guestCount, 1f) * 10f;
			if (c.drankAny) score += 5f;

			int own = 0;
			int fed = 0;
			for (int i = 0; i < c.guests.Count && i < c.guestHouse.Count; i++)
			{
				Pawn p = c.guests[i];
				if (p == null || c.guestHouse[i] != h) continue;
				own++;
				if (p.Spawned && !p.Dead && p.needs?.food != null && p.needs.food.CurLevelPercentage > 0.6f) fed++;
			}
			if (own > 0) score += (float)fed / own * 15f;

			bool music = FeastUtility.MusicPossible;
			if (c.toasted.Contains(h)) score += Mathf.Min(c.toastsGiven / (float)ToastsForFull, 1f) * (music ? 10f : 15f);
			if (music) score += Mathf.Min((c.samples > 0 ? (float)c.musicSamples / c.samples : 0f) / 0.5f, 1f) * 5f;
			if (c.brawlHouses.Contains(h)) score -= 10f;
			return Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
		}

		private void ApplyGrandOutcome(FeastCase c)
		{
			ChronicleEntry entry = EntryFor(c);
			var lines = new StringBuilder();
			var scores = new Dictionary<Faction, int>();
			int sum = 0;
			foreach (Faction h in c.houses)
			{
				if (h == null || h.defeated) continue;
				int score = ScoreFor(c, h);
				int tier = TierOf(score);
				int goodwill = tier == 3 ? 25 : tier == 2 ? 15 : tier == 1 ? 8 : -5;
				// calcule avant d'enregistrer ce banquet, sinon il se penalise lui-meme
				int remembered = FeastsRemembered(h);
				if (goodwill > 0 && remembered > 0)
					goodwill = Mathf.Max(1, Mathf.RoundToInt(goodwill * GoodwillFactorFor(h)));
				if (c.leaderHouses.Contains(h)) goodwill = Mathf.RoundToInt(goodwill * 1.5f);
				h.TryAffectGoodwillWith(Faction.OfPlayer, goodwill, canSendMessage: false, canSendHostilityLetter: goodwill < 0);
				HouseFeastMemory m = MemoryFor(h, true);
				if (m != null)
				{
					m.timesFeasted = remembered + 1;
					m.lastFeastTick = Find.TickManager.TicksGame;
				}
				scores[h] = score;
				sum += score;

				string why = !c.toasted.Contains(h) ? "RimFeast_GrandNoToast"
					: c.brawlHouses.Contains(h) ? "RimFeast_GrandBrawled" : null;
				lines.Append("RimFeast_GrandVerdictLine".Translate(h.Name, TierLabel(tier), goodwill.ToStringWithSign()));
				if (why != null) lines.Append(" " + why.Translate());
				lines.AppendLine();
				entry.Add(ChronicleLine.HouseVerdict, h.Name, tier.ToString(), goodwill);
			}
			if (scores.Count == 0) return;
			int average = sum / scores.Count;

			// une belle soiree sans rixe entre elles referme une vieille querelle, et les deux maisons vous en savent gre
			if (!c.feudBrawl)
				foreach (Pair<Faction, Faction> f in Feuds(c.houses))
				{
					if (!scores.TryGetValue(f.First, out int a) || !scores.TryGetValue(f.Second, out int b)) continue;
					if (a < GoodScore || b < GoodScore) continue;
					peace.Add(PairKey(f.First, f.Second));
					f.First.TryAffectGoodwillWith(Faction.OfPlayer, 10, canSendMessage: false, canSendHostilityLetter: false);
					f.Second.TryAffectGoodwillWith(Faction.OfPlayer, 10, canSendMessage: false, canSendHostilityLetter: false);
					lines.AppendLine();
					lines.Append("RimFeast_GrandPeace".Translate(f.First.Name, f.Second.Name));
					entry.Add(ChronicleLine.Peace, f.First.Name, f.Second.Name);
				}

			entry.score = average;
			entry.tier = TierOf(average);
			Stamp(entry, c);

			TaggedString text = "RimFeast_TextGrandVerdict".Translate() + "\n\n" + lines.ToString().TrimEnd();
			if (average >= TriumphScore)
			{
				DropGift(c);
				Note(c, ChronicleLine.Silver, null, null, GiftSilver);
				text += "\n\n" + "RimFeast_GrandSilver".Translate(GiftSilver);
			}
			text += "\n\n" + HallReport(c);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelGrandVerdict".Translate(), text,
				average >= DecentScore ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent,
				new TargetInfo(c.spotCell, c.map));

			// une seule caravane promise: celle de la maison qui vous estime le plus, si elle repart contente
			if (c.map != null && scores.TryGetValue(c.house, out int first) && first >= GoodScore)
				OfferTradePact(c, first >= TriumphScore);
		}

		// le banquet rouge de toutes les maisons a la fois: la guerre avec chacune
		private void ResolveGrandBetrayal(FeastCase c, ChronicleEntry entry, int total, int slain)
		{
			int now = Find.TickManager.TicksGame;
			List<Faction> there = c.houses.Where(h => h != null && !h.defeated).ToList();
			if (total > 0 && slain * 2 >= total)
			{
				redWeddingTick = now;
				for (int i = 0; i < there.Count; i++)
				{
					Faction h = there[i];
					HouseFeastMemory m = MemoryFor(h, true);
					if (m != null) m.betrayed = true;
					// faction a goodwill: l'hostilite passe par le goodwill, SetRelationDirect refuse en silence
					h.TryAffectGoodwillWith(Faction.OfPlayer, -200, canSendMessage: false, canSendHostilityLetter: false);
					raids.Add(new PendingRaid
					{
						house = h,
						map = c.map,
						fireTick = now + (Rand.Range(3, 8) + i * 2) * 60000,
						points = StorytellerUtility.DefaultThreatPointsNow(c.map) * 0.8f,
					});
					entry.Add(ChronicleLine.War, h.Name);
				}
				foreach (Faction f in FeastUtility.InvitableHouses().ToList())
					if (!there.Contains(f))
						f.TryAffectGoodwillWith(Faction.OfPlayer, -25, canSendMessage: false, canSendHostilityLetter: false);
				foreach (Pawn col in c.map.mapPawns.FreeColonistsSpawned)
					col.needs?.mood?.thoughts?.memories?.TryGainMemory(RimFeastDefOf.RimFeast_GuestRightBroken);
				Find.LetterStack.ReceiveLetter("RimFeast_LabelRedWedding".Translate(),
					"RimFeast_TextRedGrand".Translate(slain, there.Count),
					LetterDefOf.ThreatBig, new TargetInfo(c.spotCell, c.map));
				return;
			}
			entry.Add(ChronicleLine.Blood);
			foreach (Faction h in there)
				if (h.HostileTo(Faction.OfPlayer)) entry.Add(ChronicleLine.War, h.Name);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelGuestsHarmed".Translate(),
				"RimFeast_TextBloodSpilled".Translate(GuestsName(c)),
				LetterDefOf.NegativeEvent, new TargetInfo(c.spotCell, c.map));
		}

		public void StartInvite(Map map, Faction house, Thing spot, bool planned = false)
		{
			if (Busy || map == null || house == null || spot == null) return;
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

		public override void FinalizeInit()
		{
			base.FinalizeInit();
			anyExposed = current != null && current.exposed;

			// le set est sauvegarde, mais perdre un enfant du joueur serait irreparable
			foreach (PendingWard w in wards)
				if (w?.ward != null && !w.ward.Dead)
					Find.WorldPawns.ForcefullyKeptPawns.Add(w.ward);

			foreach (PendingWedding w in weddings)
				if (w?.noble != null && !w.noble.Dead)
					Find.WorldPawns.ForcefullyKeptPawns.Add(w.noble);
			if (current != null && current.wedding && current.state == FeastCase.Invited && current.betrothedNoble != null)
				Find.WorldPawns.ForcefullyKeptPawns.Add(current.betrothedNoble);

			// une sauvegarde d'avant la 2.0 faite en plein repas: ce travail est vide, le colon en reprend un au tick suivant
			foreach (Map map in Find.Maps)
				foreach (Pawn p in map.mapPawns.AllPawnsSpawned.ToList())
					if (p.CurJobDef == RimFeastDefOf.RimFeast_EatAtSeatJob)
						p.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
		}

		private bool oldGuestsChecked;

		// avant la 2.0 les invites vivaient dans une faction cachee, rendue hostile le temps d'un combat.
		// ceux d'un banquet sauvegarde en cours rejoignent leur vraie maison. sans SetFaction: il les sortirait du lord
		private void MoveOldGuestsHome()
		{
			FeastCase c = current;
			if (c == null) return;
			bool moved = false;
			foreach (Pawn p in c.guests)
			{
				if (p == null || p.Dead || p.Faction?.def != RimFeastDefOf.RimFeast_Guests) continue;
				Faction home = HouseOfGuest(c, p);
				if (home == null) continue;
				if (p.Spawned) p.Map.mapPawns.DeRegisterPawn(p);
				p.SetFactionDirect(home);
				if (p.Spawned)
				{
					p.Map.mapPawns.RegisterPawn(p);
					p.Map.attackTargetsCache.UpdateTarget(p);
				}
				moved = true;
			}
			if (!moved) return;
			if (c.lord != null) c.lord.faction = c.house;
			// le combat etait engage: sous la nouvelle regle, c'est la guerre avec la maison
			if (c.state == FeastCase.Fleeing && !c.exposed) Blame(c, -200);
		}

		// meme sortie que la transition betrayed du lord, pour une hostilite qui ne vient pas d'un invite tombe
		private static void SendFighting(Lord lord)
		{
			LordToil fight = lord?.Graph?.lordToils.FirstOrDefault(t => t is LordToil_FightOut);
			if (fight == null || lord.CurLordToil == fight) return;
			lord.GotoToil(fight);
			foreach (Pawn p in lord.ownedPawns)
				if (p.jobs?.curJob != null) p.jobs.EndCurrentJob(JobCondition.InterruptForced);
		}

		public override void GameComponentTick()
		{
			// le sustainer s'entretient a chaque tick, avant la barriere des 250
			if (redWeddingSound != null)
			{
				if (redWeddingSound.Ended) redWeddingSound = null;
				else redWeddingSound.Maintain();
			}

			if (Find.TickManager.TicksGame % CheckInterval != 0) return;

			// caravanes et raids vivent apres la fin du banquet, hors de current
			if (caravans.Count > 0) TickCaravans();
			if (raids.Count > 0) TickRaids();
			if (reveals.Count > 0) TickReveals();
			if (wards.Count > 0) TickWards();
			if (lovin != null) TickLovin();
			TickAwayInvites();
			if (colony != null) TickColony();
			if (weddings.Count > 0) TickWeddings();

			if (!oldGuestsChecked)
			{
				oldGuestsChecked = true;
				MoveOldGuestsHome();
			}
			if (current == null) return;

			FeastCase c = current;
			if (c.map == null || !Find.Maps.Contains(c.map)) { AbandonCase(c); return; }
			DropDeadLords(c);

			// une maison devenue hostile par les regles du jeu (coups portes a ses gens, capture, verdict de la soiree):
			// ses invites ne restent pas a table, ils sortent en se battant
			if (c.state != FeastCase.Invited && c.state != FeastCase.Fleeing)
			{
				Faction enemy = HostileHouse(c);
				if (enemy != null)
				{
					SendFighting(c.lord);
					TurnOnHost(c, "RimFeast_TextTurnedHostile", enemy.Name);
				}
			}

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
					if (Find.TickManager.TicksGame - c.lastSummonTick >= ResummonTicks)
						SummonColonists(c);
					Sample(c);
					TryFeudBrawl(c);
					TryBrawl(c);
					BreakUpBrawl(c);
					TryProposal(c);
					TryFirstDance(c);
					TryRequest(c);
					TryPoison(c);
					CheckPoisonSymptoms(c);
					break;
				case FeastCase.Fleeing:
					// vieille save figee en fuite
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

		private void AbandonCase(FeastCase c)
		{
			if (redWeddingSound != null)
			{
				redWeddingSound.End();
				redWeddingSound = null;
				Find.MusicManagerPlay?.ForceSilenceFor(1f);
			}
			anyExposed = false;
			WeddingUndone(c, false);
			current = null;
		}

		// un lord termine encore reference fait raler le chargement suivant
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
				Messages.Message("RimFeast_MessageSpotDestroyed".Translate(GuestsName(c)),
					MessageTypeDefOf.NegativeEvent);
				WeddingUndone(c, true);
				current = null;
				return;
			}

			if (c.grand)
			{
				SpawnGrandCortege(c);
				return;
			}
			if (c.house == null || c.house.defeated || c.house.HostileTo(Faction.OfPlayer))
			{
				Messages.Message("RimFeast_MessageInviteVoid".Translate(c.house?.Name ?? "?"),
					MessageTypeDefOf.NegativeEvent);
				WeddingUndone(c, true);
				current = null;
				return;
			}

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

			int size = RimFeastMod.S.corteges
				+ Mathf.RoundToInt(Mathf.Clamp(c.house.PlayerGoodwill, -100f, 100f) / 50f)
				+ Rand.RangeInclusive(-1, 1);
			if (c.wedding) size += 2;
			size = Mathf.Max(size, 2);
			PawnKindDef filler = crew ?? knight ?? lord;
			while (kinds.Count < size && filler != null)
				kinds.Add(kinds.Count % 2 == 0 ? (knight ?? filler) : filler);
			if (kinds.Count == 0) { current = null; return; }

			// le fiance fait le voyage: il doit etre libre dans le monde, comme le chef
			Pawn groom = null;
			if (c.wedding)
			{
				if (WeddingStillOn(c.betrothedNoble, c.betrothedColonist, c.house)) groom = TakeFromWorld(c.betrothedNoble);
				if (groom == null)
				{
					WeddingUndone(c, true);
					current = null;
					return;
				}
				Find.WorldPawns.ForcefullyKeptPawns.Remove(groom);
				// fiance d'avant la 2.0: il attendait dans la faction cachee
				if (groom.Faction != c.house) groom.SetFaction(c.house);
			}

			// le vrai chef garde sa vraie faction, pas une copie
			Pawn leader = null;
			if ((groom == null || groom != c.house.leader) && (debugForceLeader
				|| (RimFeastMod.S.leaderEnabled && c.house.PlayerGoodwill >= 40
					&& FeastsRemembered(c.house) >= 1 && Rand.Chance(0.4f))))
				leader = TakeLeader(c.house);
			debugForceLeader = false;
			if (leader != null) kinds.Remove(lord);

			var guests = new List<Pawn>();
			foreach (PawnKindDef kind in kinds)
			{
				Pawn p = MakeGuest(kind, c.house, map);
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

			if (groom != null)
			{
				if (groom.needs?.food != null) groom.needs.food.CurLevel = 0.35f * groom.needs.food.MaxLevel;
				if (groom.needs?.rest != null) groom.needs.rest.CurLevel = Mathf.Max(groom.needs.rest.CurLevel, 0.8f);
				GenSpawn.Spawn(groom, CellFinder.RandomClosewalkCellNear(entry, map, 6), map);
				guests.Add(groom);
			}

			Pawn speaker = leader ?? guests.FirstOrDefault(p => p.kindDef == lord) ?? guests[0];

			if (c.treacherous)
				c.poisoner = guests.Where(p => p != speaker && p.kindDef != standard)
					.RandomElementWithFallback() ?? guests.FirstOrDefault(p => p != speaker);

			c.lord = LordMaker.MakeNewLord(c.house,
				new LordJob_FeastGuests(c.id, c.house, c.spotCell, speaker), map, guests);
			c.guests = guests;
			c.guestCount = guests.Count;
			c.leaderCame = leader != null || (groom != null && groom == c.house.leader);
			c.state = FeastCase.Traveling;

			TaggedString arriveText = groom != null
				? "RimFeast_TextWeddingArrive".Translate(c.house.Name, guests.Count, groom.LabelShortCap,
					c.betrothedColonist.LabelShortCap)
				: "RimFeast_TextGuestsArrive".Translate(c.house.Name, guests.Count);
			if (leader != null)
				arriveText += "\n\n" + "RimFeast_TextLeaderRides".Translate(leader.LabelShortCap, c.house.Name);
			Find.LetterStack.ReceiveLetter(
				(groom != null ? "RimFeast_LabelWeddingArrive" : "RimFeast_LabelGuestsArrive").Translate(), arriveText,
				LetterDefOf.PositiveEvent, guests[0], c.house);
		}

		// doit etre libre dans le monde: kidnappe, en quete ou en transporteur, GenSpawn planterait ou le doublerait
		private static Pawn TakeLeader(Faction house) => TakeFromWorld(house?.leader);

		private static Pawn TakeFromWorld(Pawn l)
		{
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
			Messages.Message("RimFeast_MessageFeastBegins".Translate(GuestsName(c)),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.PositiveEvent);
			StartColonistLord(c);
			TryBringGift(c);
		}

		// meme generateur et meme bareme que le cadeau des visiteurs vanilla
		private static void TryBringGift(FeastCase c)
		{
			if (c.house == null || c.lord == null || c.map == null) return;
			int esteem = c.house.PlayerGoodwill;
			// aux noces la maison ne vient jamais les mains vides
			if (!c.wedding && (esteem < 20 || !Rand.Chance(Mathf.InverseLerp(0f, 100f, esteem)))) return;

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
			Get()?.Note(c, ChronicleLine.Gift);
		}

		public void Notify_ToastGiven(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id || c.state != FeastCase.Feasting) return;
			c.toastsGiven++;
			bool vows = c.wedding && !c.wed && c.betrothedNoble != null && c.betrothedColonist != null;
			Messages.Message(vows
					? "RimFeast_MessageWeddingToast".Translate(c.betrothedNoble.LabelShortCap,
						c.betrothedColonist.LabelShortCap)
					: "RimFeast_MessageToast".Translate(ToastedName(c)),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.PositiveEvent, historical: false);
		}

		// la fin du toast: aux noces, c'est le moment des voeux
		public void Notify_ToastOver(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id || c.state != FeastCase.Feasting) return;
			TryWed(c, false);
			Cheer(c);
		}

		public void Notify_ColonyToastOver() => Cheer(colony);

		// la ola a la fin d'un toast. les invites sortent tout juste du toast.
		// un colon qui porte son assiette, qui joue ou qu'on a envoye danser reste a ce qu'il fait
		private void Cheer(FeastCase c)
		{
			if (c?.map == null) return;
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(c.spotCell, c.map);
			var crowd = new List<Pawn>();
			if (c.lord != null) crowd.AddRange(c.lord.ownedPawns);
			if (c.colonistLord != null && c.colonistLord != c.lord) crowd.AddRange(c.colonistLord.ownedPawns);
			foreach (Pawn p in crowd)
			{
				if (!p.Spawned || p.Drafted || !area.Contains(p.Position) || !FeastUtility.CanDance(p)) continue;
				if (p.carryTracker?.CarriedThing != null || (p.CurJob != null && p.CurJob.playerForced)) continue;
				if (p.jobs.curDriver is JobDriver_PlayMusicalInstrument) continue;
				p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(RimFeastDefOf.RimFeast_CheerJob, c.spotCell), JobTag.Misc);
			}
		}

		public void DebugCheer()
		{
			FeastCase c = current != null && current.state == FeastCase.Feasting ? current : colony;
			if (c == null)
				Messages.Message("RimFeast: no feast at the table right now.", MessageTypeDefOf.RejectInput,
					historical: false);
			else Cheer(c);
		}

		private const int MaxContests = 2;

		public bool ContestAllowed(Pawn p)
		{
			FeastCase c = CaseAt(p);
			return c != null && c.contests < MaxContests;
		}

		public void Notify_ContestStarted(Pawn p)
		{
			FeastCase c = CaseAt(p);
			if (c != null) c.contests++;
		}

		public void Notify_Contest(Pawn a, Pawn b, Pawn winner, int mugs)
		{
			if (a == null || b == null) return;
			if (winner == null)
			{
				Note(CaseAt(a), ChronicleLine.ContestDraw, Who(a), Who(b), mugs);
				Messages.Message("RimFeast_ContestDraw".Translate(a.LabelShort, b.LabelShort, mugs), a,
					MessageTypeDefOf.NeutralEvent, historical: false);
				return;
			}
			Pawn loser = winner == a ? b : a;
			winner.needs?.mood?.thoughts?.memories?.TryGainMemory(RimFeastDefOf.RimFeast_WonContest);
			Note(CaseAt(winner), ChronicleLine.Contest, Who(winner), Who(loser), mugs);
			Messages.Message("RimFeast_ContestWon".Translate(winner.LabelShort, loser.LabelShort, mugs), winner,
				MessageTypeDefOf.PositiveEvent, historical: false);
		}

		public void Notify_SetDance(Pawn leader, int size) =>
			Note(CaseAt(leader), ChronicleLine.SetDance, Who(leader), null, size);

		public void Notify_FeastEnded(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id) return;

			if (c.state != FeastCase.Feasting)
			{
				Messages.Message("RimFeast_MessageGuestsLeftNoFeast".Translate(GuestsName(c)),
					MessageTypeDefOf.NegativeEvent);
				WeddingUndone(c, true);
				current = null;
				return;
			}

			CloseFeast(c);
		}

		// le dossier reste ouvert tant qu'ils sont sur la carte: les egorger en partant reste un festin rouge
		private void CloseFeast(FeastCase c)
		{
			Sample(c);
			// dernier moment pour les voeux, meme si le fiance n'est pas dans la salle
			TryWed(c, true);
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

		// un ivrogne assomme par un des siens reste dans le lord, sinon harmed fait partir tout le monde
		public bool IsBrawler(int id, Pawn p)
		{
			FeastCase c = current;
			if (c == null || c.id != id || !c.brawled || c.state != FeastCase.Feasting) return false;
			return p == c.brawlerA || p == c.brawlerB;
		}

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

		private static void FlashRed(FeastCase c)
		{
			if (c?.map == null || Find.CurrentMap != c.map) return;
			// deux StartFade dans la meme frame: le second efface le premier
			ScreenFader.SetColor(new Color(0.5f, 0.03f, 0.03f, 0.5f));
			ScreenFader.StartFade(Color.clear, 0.7f);
		}

		public void Notify_GuestsBetrayed(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id) return;
			if (c.state != FeastCase.Feasting && c.state != FeastCase.Traveling
				&& c.state != FeastCase.Leaving) return;

			// la guerre pour de bon, des le premier coup: le ciblage ignore les neutres, et les invites sont de leur vraie maison
			Blame(c, -200);
			StartRedWeddingMusic(c);
			FlashRed(c);
			TurnOnHost(c, "RimFeast_TextBetrayed", GuestsName(c));
		}

		// des ennemis sous le toit: la soiree est finie, le bilan se fait quand ils ont quitte la carte
		private void TurnOnHost(FeastCase c, string textKey, string who)
		{
			c.state = FeastCase.Fleeing;
			c.fleeStartTick = Find.TickManager.TicksGame;
			// pour la chronique: qui tenait la salle quand les lames sont sorties
			c.hosts = c.map.mapPawns.FreeColonistsSpawned.ToList();
			WeddingUndone(c, false);

			if (c.colonistLord != null)
			{
				if (c.map.lordManager.lords.Contains(c.colonistLord))
					c.map.lordManager.RemoveLord(c.colonistLord);
				c.colonistLord = null;
			}

			Find.LetterStack.ReceiveLetter("RimFeast_LabelBetrayed".Translate(),
				textKey.Translate(who),
				LetterDefOf.NegativeEvent, new TargetInfo(c.spotCell, c.map), c.house);

			if (c.slaughterOrdered) LaunchAttackWave(c);
		}

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

				// AttackStatic ne deplace pas le pawn: seulement si la cible est deja tirable
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
					+ " | house hostile=" + (c.house?.HostileTo(Faction.OfPlayer)));
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

		private void TryPoison(FeastCase c)
		{
			if (!c.treacherous || c.tainted || c.exposed) return;
			if (Find.TickManager.TicksGame - c.arrivedTick < TaintDelayTicks) return;
			Pawn p = c.poisoner;
			if (p == null || p.Dead || !p.Spawned || p.Downed) return;
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(c.spotCell, c.map);
			if (!area.Contains(p.Position)) return;
			if (!Rand.Chance(0.1f)) return;

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
			if (stacks == 0) return;

			c.tainted = true;
			c.taintTick = Find.TickManager.TicksGame;
			c.sickBaseline = ColonistsWithFoodPoisoning(c.map);
		}

		private void CheckPoisonSymptoms(FeastCase c)
		{
			if (!c.tainted || c.exposed) return;
			if (ColonistsWithFoodPoisoning(c.map) > c.sickBaseline)
				Expose(c, harmDone: true);
		}

		private static int ColonistsWithFoodPoisoning(Map map) =>
			map.mapPawns.FreeColonistsSpawned
				.Count(p => p.health.hediffSet.HasHediff(HediffDefOf.FoodPoisoning));

		private void Expose(FeastCase c, bool harmDone)
		{
			c.exposed = true;
			anyExposed = true;
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
			Note(c, harmDone ? ChronicleLine.Poisoned : ChronicleLine.Poisoner, Who(c.poisoner));
			EntryFor(c).goodwill += harmDone ? -75 : -30;
			Find.LetterStack.ReceiveLetter(
				(harmDone ? "RimFeast_LabelPoisonSymptoms" : "RimFeast_LabelPoisonerCaught").Translate(),
				(harmDone ? "RimFeast_TextPoisonSymptoms" : "RimFeast_TextPoisonerCaught")
					.Translate(c.poisoner?.LabelShortCap ?? "?", c.house?.Name),
				LetterDefOf.NegativeEvent, new TargetInfo(c.spotCell, c.map), c.house);
		}

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

				bool fallen = w.house == null || w.house.defeated;
				if (!fallen && Find.TickManager.TicksGame < w.returnTick) continue;

				if (w.map == null || !Find.Maps.Contains(w.map))
				{
					w.map = Find.AnyPlayerHomeMap;
					if (w.map == null) continue;
				}

				wards.RemoveAt(i);
				Find.WorldPawns.ForcefullyKeptPawns.Remove(p);

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

			// EnsureMinLevelWithMargin releve un plancher sans ecraser l'existant
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
			try
			{
				if (redWeddingSound != null)
				{
					// le fondu de 5s vient du sustainFadeoutTime du def
					redWeddingSound.End();
					redWeddingSound = null;
					Find.MusicManagerPlay?.ForceSilenceFor(20f);
				}
				else
				{
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

			if (c.exposed)
			{
				anyExposed = false;
				current = null;
				return;
			}

			// les refs mortes perdues au chargement comptent comme tombees
			int total = c.guests.Count;
			int slain = 0;
			foreach (Pawn g in c.guests)
				if (g == null || g.Dead || g.Downed || g.IsPrisonerOfColony) slain++;

			ChronicleEntry entry = EntryFor(c);
			entry.red = true;
			Stamp(entry, c);
			int nameless = 0;
			foreach (Pawn g in c.guests)
			{
				if (g == null) nameless++;
				else if (g.Dead) entry.Add(ChronicleLine.Slain, Who(g));
				else if (g.Downed || g.IsPrisonerOfColony) entry.Add(ChronicleLine.Fallen, Who(g));
			}
			if (nameless > 0) entry.Add(ChronicleLine.Nameless, null, null, nameless);
			if (c.hosts != null)
				foreach (Pawn h in c.hosts)
					if (h != null && h.Dead) entry.Add(ChronicleLine.Lost, Who(h));

			if (c.grand)
			{
				ResolveGrandBetrayal(c, entry, total, slain);
				current = null;
				return;
			}

			HouseFeastMemory m = MemoryFor(c.house, true);
			if (total > 0 && slain * 2 >= total)
			{
				if (m != null) m.betrayed = true;
				redWeddingTick = Find.TickManager.TicksGame;

				// faction a goodwill: l'hostilite passe par le goodwill, SetRelationDirect refuse en silence
				c.house?.TryAffectGoodwillWith(Faction.OfPlayer, -200, canSendMessage: false,
					canSendHostilityLetter: false);

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

				entry.Add(ChronicleLine.War, c.house?.Name);
				Find.LetterStack.ReceiveLetter("RimFeast_LabelRedWedding".Translate(),
					"RimFeast_TextRedWedding".Translate(c.house?.Name, slain),
					LetterDefOf.ThreatBig, new TargetInfo(c.spotCell, c.map), c.house);
			}
			else
			{
				// la guerre est la depuis le premier coup. assez d'invites sont rentres: les autres maisons ne s'en melent pas
				entry.Add(ChronicleLine.Blood);
				if (c.house != null && c.house.HostileTo(Faction.OfPlayer)) entry.Add(ChronicleLine.War, c.house.Name);
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

		public bool HouseBetrayed(Faction f) => MemoryFor(f, false)?.betrayed ?? false;

		public const int AwayKnives = 1;
		public const int AwayAmbush = 2;
		public const int AwaySlight = 3;
		public const int AwayDull = 4;
		public const int AwayFine = 5;
		public const int AwaySongs = 6;

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

		private void TickAwayInvites()
		{
			int now = Find.TickManager.TicksGame;

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

			if (awayHouse != null) return;

			Map home = Find.AnyPlayerHomeMap;
			if (home == null) return;

			TryOpenAwayInvite(home, force: false);
		}

		private bool TryOpenAwayInvite(Map home, bool force)
		{
			int now = Find.TickManager.TicksGame;

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

			int windowDays = Mathf.Max(RimFeastMod.S.awayInviteWindowDays, travel * 2 + 3);
			awayHouse = house;
			awayExpireTick = now + windowDays * 60000;

			Find.LetterStack.ReceiveLetter("RimFeast_LabelAwayInvited".Translate(),
				"RimFeast_TextAwayInvited".Translate(house.Name, seat.Label, travel, windowDays),
				LetterDefOf.PositiveEvent, seat, house);
			return true;
		}

		// filtre a vol d'oiseau avant le pathfinder monde
		private static int TravelDaysTo(PlanetTile from, PlanetTile to)
		{
			if (!from.Valid || !to.Valid) return 0;
			// pas de chemin entre couches planetaires, le pathfinder monde jette une erreur
			if (from.Layer != to.Layer) return 0;
			if (Find.WorldGrid.ApproxDistanceInTiles(from, to) > RimFeastMod.S.awayMaxTravelDays * 20f)
				return 0;
			int ticks = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(from, to, null);
			return ticks <= 0 ? 0 : Mathf.CeilToInt(ticks / 60000f);
		}

		// meme courbe que les pourparlers vanilla
		private static readonly SimpleCurve AwayBadByNegotiation = new SimpleCurve
		{
			new CurvePoint(0f, 4f),
			new CurvePoint(1f, 1f),
			new CurvePoint(1.5f, 0.4f),
		};

		private float AwayBadFactor(Pawn diplomat, Faction house)
		{
			float f = AwayBadByNegotiation.Evaluate(diplomat.GetStatValue(StatDefOf.NegotiationAbility));

			f *= Mathf.Lerp(2f, 0.35f,
				Mathf.InverseLerp(-20f, 100f, house.PlayerGoodwill));

			f /= 1f + 0.25f * FeastsRemembered(house);
			if (HouseBetrayed(house)) f *= 4f;
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
				// cale sur les pourparlers vanilla: environ 15% de mauvaises issues
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

		private static void AwayOutcome_Knives(Caravan caravan, Faction house)
		{
			List<Pawn> guests = caravan.PawnsListForReading
				.Where(p => p.RaceProps.Humanlike && !p.Dead).ToList();
			if (guests.Count == 0) { AwayOutcome_Slight(caravan, house); return; }
			Get()?.NoteAway(caravan, house, AwayKnives);

			var dead = new List<string>();
			var hurt = new List<string>();
			foreach (Pawn p in guests.InRandomOrder().Take(Mathf.Min(guests.Count, Rand.RangeInclusive(1, 2))))
			{
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

		// GetOrGenerateMapForIncident prend la tuile de la caravane: sur une tuile occupee il genere leur colonie ou ta base
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

		private static void AwayOutcome_Ambush(Caravan caravan, Faction house)
		{
			if (!MoveCaravanToOpenGround(caravan)) { AwayOutcome_Knives(caravan, house); return; }
			Get()?.NoteAway(caravan, house, AwayAmbush);

			LongEventHandler.QueueLongEvent(delegate
			{
				// hostiles avant la generation, sinon bataille gagnee aussitot. GoodwillToMakeHostile = -75 moins le goodwill
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
			Get()?.NoteAway(caravan, house, AwaySlight);
			house.TryAffectGoodwillWith(Faction.OfPlayer, -10, canSendMessage: false,
				canSendHostilityLetter: false);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelAwaySlight".Translate(),
				"RimFeast_TextAwaySlight".Translate(house.Name), LetterDefOf.NegativeEvent, caravan, house);
		}

		private static void AwayOutcome_Dull(Caravan caravan, Faction house)
		{
			Get()?.NoteAway(caravan, house, AwayDull);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelAwayDull".Translate(),
				"RimFeast_TextAwayDull".Translate(house.Name), LetterDefOf.NeutralEvent, caravan, house);
		}

		private static void AwayOutcome_Fine(Caravan caravan, Faction house)
		{
			Get()?.NoteAway(caravan, house, AwayFine);
			int gain = Rand.RangeInclusive(12, 20);
			house.TryAffectGoodwillWith(Faction.OfPlayer, gain, canSendMessage: false,
				canSendHostilityLetter: false);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelAwayFine".Translate(),
				"RimFeast_TextAwayFine".Translate(house.Name, gain), LetterDefOf.PositiveEvent, caravan, house);
		}

		private static void AwayOutcome_Songs(Caravan caravan, Faction house)
		{
			Get()?.NoteAway(caravan, house, AwaySongs);
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

			if (c.brawled && ((c.brawlerA?.Dead ?? false) || (c.brawlerB?.Dead ?? false)))
			{
				Note(c, ChronicleLine.BrawlDeath);
				Find.LetterStack.ReceiveLetter("RimFeast_LabelBrawlDeath".Translate(),
					"RimFeast_TextBrawlDeath".Translate(GuestsName(c)),
					LetterDefOf.NegativeEvent, new TargetInfo(c.spotCell, c.map), c.house);
				LeaveUnderOurRoof(c);
				return;
			}

			// des lames sorties sur la carte: l'hote n'y est pour rien
			if (GenHostility.AnyHostileActiveThreatToPlayer(c.map))
			{
				bool judged = c.state == FeastCase.Feasting
					&& Find.TickManager.TicksGame - c.arrivedTick >= FeastDurationTicks / 2;
				TaggedString text = "RimFeast_TextGuestsLeaveFighting".Translate(GuestsName(c));
				if (judged) text += "\n\n" + "RimFeast_TextGuestsLeaveFightingJudged".Translate();
				Find.LetterStack.ReceiveLetter("RimFeast_LabelGuestsLeaveFighting".Translate(), text,
					LetterDefOf.NeutralEvent, new TargetInfo(c.spotCell, c.map), c.house);
				if (judged) CloseFeast(c);
				else LeaveUnderOurRoof(c);
				return;
			}

			Note(c, ChronicleLine.Blood);
			EntryFor(c).goodwill -= 15;
			Blame(c, -15);
			Find.LetterStack.ReceiveLetter("RimFeast_LabelGuestsHarmed".Translate(),
				"RimFeast_TextGuestsHarmed".Translate(GuestsName(c)),
				LetterDefOf.NegativeEvent, new TargetInfo(c.spotCell, c.map), c.house);
			LeaveUnderOurRoof(c);
		}

		// encore sous notre toit: lacher le dossier ici rendait gratuit le massacre en chemin
		private void LeaveUnderOurRoof(FeastCase c)
		{
			WeddingUndone(c, true);
			c.state = FeastCase.Leaving;
			c.fleeStartTick = Find.TickManager.TicksGame;
		}

		private void StartColonistLord(FeastCase c)
		{
			GatheringDef def = RimFeastDefOf.RimFeast_Feast;
			Pawn organizer = GatheringsUtility.FindRandomGatheringOrganizer(Faction.OfPlayer, c.map, def);
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

		// seuil Rested de vanilla
		private const float WakeRestFloor = 0.28f;

		private const int ResummonTicks = 2500;

		// vanilla n'arrache personne a sa tache pour une fete
		private void SummonColonists(FeastCase c)
		{
			c.lastSummonTick = Find.TickManager.TicksGame;
			if (!RimFeastMod.S.summonColonists) return;
			var job = c.colonistLord?.LordJob as LordJob_VoluntarilyJoinable;
			if (job == null || !c.map.lordManager.lords.Contains(c.colonistLord)) return;

			foreach (Pawn p in c.map.mapPawns.FreeColonistsSpawned.ToList())
			{
				if (p.Drafted || p.GetLord() != null) continue;

				// un dormeur est bloque deux fois: VoluntaryJoinPriorityFor teste Awake, et LayDown empeche l'arbre de tourner
				if (!p.Awake())
				{
					if ((p.needs?.rest?.CurLevel ?? 0f) < WakeRestFloor) continue;
					if (c.planned && !LordJob_FeastColonists.FitForTrap(p)) continue;
					RestUtility.WakeUp(p);
					continue;
				}

				if (job.VoluntaryJoinPriorityFor(p) <= 0f) continue;
				Job cur = p.CurJob;
				if (cur == null || !cur.def.casualInterruptible) continue;
				p.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
		}

		public int ColonyFeastDaysLeft()
		{
			if (lastColonyFeastTick < 0) return 0;
			int ready = lastColonyFeastTick + RimFeastMod.S.colonyFeastDays * GenDate.TicksPerDay;
			return Mathf.Max(0, Mathf.CeilToInt((ready - Find.TickManager.TicksGame) / (float)GenDate.TicksPerDay));
		}

		// pas les heures ni le nombre de colons des fetes vanilla: c'est le joueur qui lance
		public string ColonyFeastBlocked(Map map)
		{
			string gather = ColonyGatherBlocked(map);
			if (gather != null) return gather;
			int days = ColonyFeastDaysLeft();
			if (days > 0) return "RimFeast_ColonyDisabledCooldown".Translate(days);
			return null;
		}

		// ce qui empeche tout banquet de la colonie. le delai entre deux banquets ne vaut que pour le banquet ordinaire
		public string ColonyGatherBlocked(Map map)
		{
			if (Busy) return "RimFeast_InviteDisabledBusy".Translate();
			if (map == null) return "";
			// le seuil ou vanilla interrompt une fete: une bete hostile au fond de la carte suffit a sortir de None
			if (map.dangerWatcher.DangerRating == StoryDanger.High) return "RimFeast_ColonyDisabledDanger".Translate();
			if (GatheringsUtility.AnyLordJobPreventsNewGatherings(map)) return "RimFeast_ColonyDisabledGathering".Translate();
			return null;
		}

		public const int OccasionNone = 0;
		public const int OccasionWake = 1;
		public const int OccasionVictory = 2;
		public const int OccasionHarvest = 3;

		private const int VictoryDays = 3;
		private const int HarvestStockDays = 12;
		// la part du chagrin qui reste apres la veillee, du pire banquet au meilleur
		private static readonly float[] WakeEase = { 0.75f, 0.6f, 0.5f, 0.4f };

		public static ThoughtDef OccasionThought(int occasion) =>
			occasion == OccasionWake ? RimFeastDefOf.RimFeast_Wake
			: occasion == OccasionVictory ? RimFeastDefOf.RimFeast_VictoryFeast
			: occasion == OccasionHarvest ? RimFeastDefOf.RimFeast_HarvestFeast
			: RimFeastDefOf.RimFeast_ColonyFeast;

		public ThoughtDef ColonyThought() => OccasionThought(colony?.occasion ?? OccasionNone);

		// un chagrin: un souvenir d'humeur negatif qui porte sur un mort
		private static bool Grief(Thought_Memory m) =>
			m.otherPawn != null && m.otherPawn.Dead && !m.def.IsSocial && m.CurStage != null
			&& m.CurStage.baseMoodEffect < 0f;

		// les morts que la colonie pleure encore et qui n'ont pas eu leur veillee.
		// on les lit dans les souvenirs des colons: rien a suivre, et une vieille sauvegarde marche
		public List<Pawn> Mourned(Map map)
		{
			var dead = new List<Pawn>();
			if (map == null) return dead;
			foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
			{
				List<Thought_Memory> memories = p.needs?.mood?.thoughts?.memories?.Memories;
				if (memories == null) continue;
				for (int i = 0; i < memories.Count; i++)
				{
					if (!Grief(memories[i])) continue;
					Pawn o = memories[i].otherPawn;
					if (dead.Contains(o) || wakesHeld.Contains(o.thingIDNumber) || !o.RaceProps.Humanlike) continue;
					if (o.Faction != Faction.OfPlayer && !PawnUtility.EverBeenColonistOrTameAnimal(o)) continue;
					dead.Add(o);
				}
			}
			return dead;
		}

		// null si le banquet de victoire peut se tenir, sinon la ligne grisee du menu
		public string VictoryBlocked(Map map)
		{
			int threat = map?.StoryState?.LastThreatBigTick ?? -1;
			if (threat < 0 || Find.TickManager.TicksGame - threat > VictoryDays * GenDate.TicksPerDay
				|| GenHostility.AnyHostileActiveThreatToPlayer(map))
				return "RimFeast_OccasionVictoryNone".Translate(VictoryDays);
			if (threat == lastVictoryThreatTick) return "RimFeast_OccasionVictoryHeld".Translate();
			return null;
		}

		// une fois l'an, quand les reserves tiennent assez de jours pour tout le monde
		public string HarvestBlocked(Map map)
		{
			if (map == null) return "";
			int wait = lastHarvestFeastTick < 0 ? 0
				: lastHarvestFeastTick + GenDate.TicksPerYear - Find.TickManager.TicksGame;
			if (wait > 0)
				return "RimFeast_OccasionHarvestHeld".Translate(Mathf.CeilToInt(wait / (float)GenDate.TicksPerDay));
			int mouths = Mathf.Max(1, map.mapPawns.FreeColonistsSpawned.Count(p => !p.DevelopmentalStage.Baby()));
			int days = Mathf.FloorToInt(map.resourceCounter.TotalHumanEdibleNutrition / (mouths * 1.6f));
			if (days < HarvestStockDays) return "RimFeast_OccasionHarvestLow".Translate(HarvestStockDays, days);
			return null;
		}

		private bool OccasionBlocked(Map map, int occasion, Pawn honored)
		{
			if (occasion == OccasionNone) return ColonyFeastBlocked(map) != null;
			if (ColonyGatherBlocked(map) != null) return true;
			if (occasion == OccasionWake) return honored == null || !Mourned(map).Contains(honored);
			if (occasion == OccasionVictory) return VictoryBlocked(map) != null;
			return HarvestBlocked(map) != null;
		}

		private static TaggedString BeginsText(FeastCase c, string who) =>
			c.occasion == OccasionWake ? "RimFeast_WakeBegins".Translate(who, c.honoredName)
			: c.occasion == OccasionVictory ? "RimFeast_VictoryBegins".Translate(who)
			: c.occasion == OccasionHarvest ? "RimFeast_HarvestBegins".Translate(who)
			: "RimFeast_ColonyFeastBegins".Translate(who);

		private static TaggedString ToastText(FeastCase c, string who) =>
			c.occasion == OccasionWake ? "RimFeast_WakeToast".Translate(who, c.honoredName)
			: c.occasion == OccasionVictory ? "RimFeast_VictoryToast".Translate(who)
			: c.occasion == OccasionHarvest ? "RimFeast_HarvestToast".Translate(who)
			: "RimFeast_ColonyToast".Translate(who);

		public string ColonySpotLine()
		{
			FeastCase c = colony;
			if (c == null) return null;
			return c.occasion == OccasionWake ? "RimFeast_SpotWake".Translate(c.honoredName)
				: c.occasion == OccasionVictory ? "RimFeast_SpotVictory".Translate()
				: c.occasion == OccasionHarvest ? "RimFeast_SpotHarvest".Translate()
				: "RimFeast_SpotColonyFeast".Translate();
		}

		// pour chaque colon present a la fin de la soiree: la veillee allege son chagrin pour le mort qu'on a veille
		public void ApplyOccasion(Pawn p, int stage)
		{
			FeastCase c = colony;
			if (c == null || c.occasion != OccasionWake) return;
			List<Thought_Memory> memories = p.needs?.mood?.thoughts?.memories?.Memories;
			if (memories == null) return;
			float ease = WakeEase[Mathf.Clamp(stage, 0, WakeEase.Length - 1)];
			for (int i = 0; i < memories.Count; i++)
				if (Grief(memories[i]) && memories[i].otherPawn.thingIDNumber == c.honoredId)
					memories[i].moodPowerFactor *= ease;
		}

		public void StartColonyFeast(Thing spot, int occasion = OccasionNone, Pawn honored = null)
		{
			Map map = spot?.Map;
			if (map == null || OccasionBlocked(map, occasion, honored)) return;
			GatheringDef def = RimFeastDefOf.RimFeast_Feast;
			Pawn organizer = GatheringsUtility.FindRandomGatheringOrganizer(Faction.OfPlayer, map, def);
			if (organizer == null)
			{
				Messages.Message("RimFeast_ColonyDisabledNobody".Translate(), spot,
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			var c = new FeastCase
			{
				id = nextId++,
				map = map,
				spotCell = spot.Position,
				spotThing = spot,
				state = FeastCase.Feasting,
				arrivedTick = Find.TickManager.TicksGame,
				guestCount = Mathf.Max(1, map.mapPawns.FreeColonistsSpawned.Count(p => !p.DevelopmentalStage.Baby())),
			};
			c.occasion = occasion;
			if (occasion == OccasionWake)
			{
				c.honoredId = honored.thingIDNumber;
				c.honoredName = honored.LabelShortCap;
			}
			// eventTick ne sert pas a un banquet de la colonie: il garde l'attaque que l'on fete
			if (occasion == OccasionVictory) c.eventTick = map.StoryState.LastThreatBigTick;
			var job = new LordJob_FeastColonists(spot.Position, organizer, def, trap: false, colony: true);
			c.colonistLord = LordMaker.MakeNewLord(Faction.OfPlayer, job, map,
				job.OrganizerIsStartingPawn ? new List<Pawn> { organizer } : null);
			c.lord = c.colonistLord;
			colony = c;
			// une occasion ne lance pas le delai du banquet ordinaire
			if (occasion == OccasionNone) lastColonyFeastTick = Find.TickManager.TicksGame;
			SummonColonists(c);
			Messages.Message(BeginsText(c, organizer.LabelShort),
				new TargetInfo(spot.Position, map), MessageTypeDefOf.PositiveEvent);
		}

		private void TickColony()
		{
			FeastCase c = colony;
			if (c.map == null || !Find.Maps.Contains(c.map) || c.colonistLord == null
				|| !c.map.lordManager.lords.Contains(c.colonistLord))
			{
				colony = null;
				return;
			}
			if (Find.TickManager.TicksGame - c.lastSummonTick >= ResummonTicks) SummonColonists(c);
			Sample(c);
		}

		public void Notify_ColonyToast(Pawn speaker)
		{
			FeastCase c = colony;
			if (c == null) return;
			c.toastsGiven++;
			if (speaker != null)
				Messages.Message(ToastText(c, speaker.LabelShort), new TargetInfo(c.spotCell, c.map),
					MessageTypeDefOf.PositiveEvent, historical: false);
		}

		// le bareme des invites, l'organisateur porte les toasts
		public int ColonyFeastStage(out string report)
		{
			report = null;
			FeastCase c = colony;
			if (c == null) return 0;
			float hall = Mathf.Clamp01(c.maxImpressiveness / ImpressivenessCap);
			float drink = Mathf.Min((float)c.maxDrinkUnits / Mathf.Max(1, c.guestCount), 1f);
			float score = hall * 20f + c.maxTableScore + Mathf.Min(c.maxVariety, 5) * 3f + drink * 10f;
			if (c.drankAny) score += 5f;
			List<Pawn> pawns = c.colonistLord?.ownedPawns;
			if (pawns != null && pawns.Count > 0)
				score += (float)pawns.Count(p => p.needs?.food != null && p.needs.food.CurLevelPercentage > 0.6f)
					/ pawns.Count * 15f;
			bool music = FeastUtility.MusicPossible;
			float toasts = Mathf.Min(c.toastsGiven / (float)ToastsForFull, 1f);
			score += toasts * (music ? 10f : 15f);
			if (music)
				score += Mathf.Min((c.samples > 0 ? (float)c.musicSamples / c.samples : 0f) / 0.5f, 1f) * 5f;
			float ent = c.samples > 0 && c.musicSamples > 0 ? Mathf.Max(toasts, 0.7f) : toasts;
			report = Qual(hall, "RimFeast_QualHall") + " " + Qual(Mathf.Clamp01(c.maxTableScore / 20f), "RimFeast_QualTable")
				+ " " + Qual(drink, "RimFeast_QualDrink") + " " + Qual(ent, "RimFeast_QualEnt");
			c.lastScore = Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
			return score >= TriumphScore ? 3 : score >= GoodScore ? 2 : score >= DecentScore ? 1 : 0;
		}

		public void Notify_ColonyFeastEnded(string report)
		{
			FeastCase c = colony;
			if (c?.map != null && !report.NullOrEmpty() && c.lastScore >= 0)
			{
				ChronicleEntry entry = EntryFor(c);
				entry.score = c.lastScore;
				entry.tier = TierOf(c.lastScore);
				Stamp(entry, c);
			}
			colony = null;
			if (c?.map == null || report.NullOrEmpty()) return;
			// l'occasion n'est consommee que si la soiree est allee au bout
			if (c.occasion == OccasionWake) wakesHeld.Add(c.honoredId);
			else if (c.occasion == OccasionVictory) lastVictoryThreatTick = c.eventTick;
			else if (c.occasion == OccasionHarvest) lastHarvestFeastTick = Find.TickManager.TicksGame;
			Messages.Message(c.occasion == OccasionWake
					? "RimFeast_WakeOver".Translate(c.honoredName, report)
					: "RimFeast_ColonyFeastOver".Translate(report),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.PositiveEvent);
		}

		private void Sample(FeastCase c)
		{
			Map map = c.map;
			Room room = c.spotCell.GetRoom(map);
			if (room != null && !room.PsychologicallyOutdoors)
				c.maxImpressiveness = Mathf.Max(c.maxImpressiveness, room.GetStat(RoomStatDefOf.Impressiveness));

			var dishes = new HashSet<ThingDef>();
			int drinkUnits = 0;
			var tierNutrition = new float[TierValue.Length];
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
				Note(c, ChronicleLine.Brawl, Who(drunk), Who(other));
				if (c.grand) GrandBrawl(c, drunk, other);
				c.brawled = true;
				c.brawlTick = Find.TickManager.TicksGame;
				c.brawlerA = drunk;
				c.brawlerB = other;
				return;
			}
		}

		private void TryProposal(FeastCase c)
		{
			if (c.proposalDone || c.lord == null) return;
			if (Find.TickManager.TicksGame - c.arrivedTick < BrawlGraceTicks) return;
			if (!RimFeastMod.S.marriageEnabled) return;
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

		// les liaisons vanilla naissent n'importe quand: on revalide a l'ouverture
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

			// le oui ne marie plus sur-le-champ: fiancailles, et la maison revient quelques jours plus tard pour les noces
			colonist.relations.AddDirectRelation(PawnRelationDefOf.Fiance, noble);
			// epingle avant qu'il quitte la carte
			Find.WorldPawns.ForcefullyKeptPawns.Add(noble);
			weddings.Add(new PendingWedding
			{
				house = c.house, noble = noble, colonist = colonist, spot = c.spotThing,
				fireTick = Find.TickManager.TicksGame + Rand.RangeInclusive(2, 4) * GenDate.TicksPerDay,
			});

			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, 10, canSendMessage: true,
				canSendHostilityLetter: false);
			Note(c, ChronicleLine.Betrothed, Who(noble), Who(colonist));
			Find.LetterStack.ReceiveLetter("RimFeast_LabelBetrothed".Translate(),
				"RimFeast_TextBetrothed".Translate(colonist.LabelShortCap, noble.LabelShortCap, c.house?.Name),
				LetterDefOf.PositiveEvent, colonist, c.house);
		}

		private static bool WeddingStillOn(Pawn noble, Pawn colonist, Faction house) =>
			noble != null && !noble.Dead && !noble.Discarded
			&& colonist != null && !colonist.Dead && colonist.Faction == Faction.OfPlayer
			&& house != null && !house.defeated && !house.HostileTo(Faction.OfPlayer)
			&& colonist.relations != null && colonist.relations.DirectRelationExists(PawnRelationDefOf.Fiance, noble);

		private static void CallOffWedding(Pawn noble, Pawn colonist, Faction house, bool letter)
		{
			if (noble != null) Find.WorldPawns.ForcefullyKeptPawns.Remove(noble);
			if (noble != null && colonist?.relations != null)
				colonist.relations.TryRemoveDirectRelation(PawnRelationDefOf.Fiance, noble);
			if (!letter) return;
			LookTargets look = colonist != null && colonist.Spawned ? new LookTargets(colonist) : null;
			Find.LetterStack.ReceiveLetter("RimFeast_LabelWeddingOff".Translate(),
				"RimFeast_TextWeddingOff".Translate(colonist?.LabelShortCap ?? "?", noble?.LabelShortCap ?? "?",
					house?.Name ?? "?"),
				LetterDefOf.NeutralEvent, look, house);
		}

		// le cortege repart, ou ne viendra pas, sans que les voeux aient ete prononces
		private void WeddingUndone(FeastCase c, bool letter)
		{
			if (!c.wedding || c.wed || c.betrothedNoble == null) return;
			Note(c, ChronicleLine.Unwed, Who(c.betrothedColonist), Who(c.betrothedNoble));
			CallOffWedding(c.betrothedNoble, c.betrothedColonist, c.house, letter);
			c.betrothedNoble = null;
		}

		private void TickWeddings()
		{
			int now = Find.TickManager.TicksGame;
			for (int i = weddings.Count - 1; i >= 0; i--)
			{
				PendingWedding w = weddings[i];
				// quinze jours sans pouvoir se tenir (colon en voyage, plus de marqueur, noble retenu): on n'attend plus
				if (!WeddingStillOn(w.noble, w.colonist, w.house) || now > w.fireTick + 15 * GenDate.TicksPerDay)
				{
					weddings.RemoveAt(i);
					CallOffWedding(w.noble, w.colonist, w.house, true);
					continue;
				}
				if (now < w.fireTick || Busy) continue;
				if (w.noble.Spawned || !Find.WorldPawns.Contains(w.noble)) continue;
				Map map = w.colonist.Spawned ? w.colonist.Map : null;
				if (map == null || !map.IsPlayerHome) continue;
				Thing spot = w.spot != null && w.spot.Spawned && w.spot.Map == map ? w.spot
					: map.listerBuildings.AllBuildingsColonistOfDef(RimFeastDefOf.RimFeast_FeastSpot).FirstOrDefault();
				if (spot == null) continue;
				weddings.RemoveAt(i);
				StartWedding(w, map, spot);
				return;
			}
		}

		// un dossier comme les autres, sans messager ni empoisonneur, et sans nouvelle demande a table
		private void StartWedding(PendingWedding w, Map map, Thing spot)
		{
			current = new FeastCase
			{
				id = nextId++,
				map = map,
				house = w.house,
				spotCell = spot.Position,
				spotThing = spot,
				state = FeastCase.Invited,
				eventTick = ArrivalTick(map, Rand.Range(45000, 75000)),
				wedding = true,
				proposalDone = true,
				requestRolled = true,
				betrothedNoble = w.noble,
				betrothedColonist = w.colonist,
			};
			TaggedString body = "RimFeast_TextWeddingComing".Translate(w.house.Name, w.noble.LabelShortCap,
				w.colonist.LabelShortCap);
			string eta = ArrivalEta(current);
			if (!eta.NullOrEmpty()) body += "\n\n" + eta;
			Find.LetterStack.ReceiveLetter("RimFeast_LabelWeddingComing".Translate(), body,
				LetterDefOf.PositiveEvent, new TargetInfo(spot.Position, map), w.house);
		}

		// les voeux se prononcent a la fin du premier toast ou les deux fiances sont dans la salle
		private void TryWed(FeastCase c, bool anywhere)
		{
			if (!c.wedding || c.wed || c.lord == null) return;
			Pawn noble = c.betrothedNoble;
			Pawn colonist = c.betrothedColonist;
			if (noble == null || noble.Dead || !noble.Spawned || !c.lord.ownedPawns.Contains(noble)) return;
			if (colonist == null || colonist.Dead || !colonist.Spawned || colonist.Map != c.map
				|| colonist.Faction != Faction.OfPlayer || colonist.Downed || colonist.InMentalState) return;
			if (!colonist.relations.DirectRelationExists(PawnRelationDefOf.Fiance, noble)) return;
			if (!anywhere && !FeastUtility.FeastArea.For(c.spotCell, c.map).Contains(colonist.Position)) return;

			c.wed = true;
			c.wedTick = Find.TickManager.TicksGame;
			MarriageCeremonyUtility.Married(colonist, noble);
			if (c.colonistLord != null)
				foreach (Pawn col in c.colonistLord.ownedPawns)
					if (col != colonist)
						col.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.AttendedWedding);

			// sortir du lord avant le changement de faction
			if (colonist.Drafted) colonist.drafter.Drafted = false;
			colonist.jobs?.StopAll();
			colonist.GetLord()?.Notify_PawnLost(colonist, PawnLostCondition.LeftVoluntarily);
			colonist.SetFaction(c.house);
			c.lord.AddPawn(colonist);
			c.guests.Add(colonist);
			c.guestCount++;

			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, 15, canSendMessage: true,
				canSendHostilityLetter: false);
			Note(c, ChronicleLine.Wed, Who(colonist), Who(noble));
			Find.LetterStack.ReceiveLetter("RimFeast_LabelWed".Translate(),
				"RimFeast_TextWed".Translate(colonist.LabelShortCap, noble.LabelShortCap, c.house?.Name),
				LetterDefOf.PositiveEvent, colonist, c.house);
		}

		// la premiere danse attend qu'un instrument joue, et doit finir avant le toast suivant. sans instrument, pas de danse
		private void TryFirstDance(FeastCase c)
		{
			if (!c.wed || c.firstDanceDone) return;
			int now = Find.TickManager.TicksGame;
			Pawn a = c.betrothedColonist;
			Pawn b = c.betrothedNoble;
			if (a == null || b == null || now - c.wedTick > 4000 || !FeastUtility.AnyInstrumentIn(c.spotCell, c.map))
			{
				c.firstDanceDone = true;
				return;
			}
			if (!FeastUtility.MusicPlayingIn(c.spotCell, c.map))
			{
				CallMinstrel(c);
				return;
			}
			if (!JobGiver_DanceAtFeast.StartDance(a, b, c.spotCell)) return;
			c.firstDanceDone = true;
			c.ballUntilTick = now + 5000;
			Note(c, ChronicleLine.FirstDance, Who(a), Who(b));
			Messages.Message("RimFeast_MessageFirstDance".Translate(a.LabelShortCap, b.LabelShortCap), a,
				MessageTypeDefOf.PositiveEvent, historical: false);
		}

		// le meilleur artiste encore a table va jouer, comme le menestrel du banquet
		private static void CallMinstrel(FeastCase c)
		{
			int now = Find.TickManager.TicksGame;
			if (now - c.minstrelTick < 750 || c.colonistLord == null) return;
			c.minstrelTick = now;
			foreach (Pawn p in c.colonistLord.ownedPawns
				.OrderByDescending(x => x.skills?.GetSkill(SkillDefOf.Artistic)?.Level ?? -1).ToList())
			{
				if (!p.Spawned || p.Downed || p.InMentalState || p.Drafted || !p.Awake()) continue;
				if (p.CurJob != null && (p.CurJob.playerForced || !p.CurJob.def.casualInterruptible)) continue;
				Building_MusicalInstrument inst = HallInstruments(c)
					.FirstOrDefault(i => i.Spawned && GatheringWorker_Concert.InstrumentAccessible(i, p));
				if (inst == null) continue;
				p.jobs.TryTakeOrderedJob(FeastUtility.PlayJob(inst), JobTag.Misc);
				return;
			}
		}

		public bool BallOpen(IntVec3 spot) =>
			current != null && current.spotCell == spot && current.ballUntilTick > Find.TickManager.TicksGame;

		public string WeddingsInspect()
		{
			if (weddings.Count == 0) return null;
			var sb = new StringBuilder();
			foreach (PendingWedding w in weddings)
			{
				if (w?.noble == null || w.colonist == null) continue;
				if (sb.Length > 0) sb.AppendLine();
				int days = Mathf.Max(1, Mathf.CeilToInt((w.fireTick - Find.TickManager.TicksGame) / 60000f));
				sb.Append("RimFeast_WeddingPendingLine".Translate(w.colonist.LabelShortCap, w.noble.LabelShortCap,
					w.house?.Name ?? "?", days));
			}
			return sb.Length == 0 ? null : sb.ToString();
		}

		public string DebugMusicReport()
		{
			var sb = new StringBuilder();
			sb.AppendLine("RimFeast music diagnostic:");
			sb.AppendLine("  music possible (Royalty or Ideology, and an instrument def): " + FeastUtility.MusicPossible);
			FeastCase c = current ?? colony;
			if (c?.map == null) { sb.AppendLine("  NO FEAST RUNNING"); return sb.ToString(); }
			sb.AppendLine("  feast: " + (c == colony ? "colony" : c.wedding ? "wedding" : "guests") + ", state=" + c.state
				+ ", spot=" + c.spotCell);
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(c.spotCell, c.map);
			var all = c.map.listerBuildings.AllBuildingsColonistOfClass<Building_MusicalInstrument>().ToList();
			sb.AppendLine("  instruments owned on this map: " + all.Count);
			foreach (Building_MusicalInstrument b in all)
				sb.AppendLine("   - " + b.LabelShort + " at " + b.Position + ": play cell " + b.InteractionCell
					+ " in feast area=" + area.Contains(b.InteractionCell) + ", being played=" + b.IsBeingPlayed);
			bool alive = c.colonistLord != null && c.map.lordManager.lords.Contains(c.colonistLord);
			sb.AppendLine("  colonist lord: " + (c.colonistLord == null ? "NEVER CREATED" : alive
				? c.colonistLord.ownedPawns.Count + " pawns, toil=" + c.colonistLord.CurLordToil?.GetType().Name
				: "ENDED"));
			if (!alive) return sb.ToString();
			foreach (Pawn p in c.colonistLord.ownedPawns)
			{
				Building_MusicalInstrument free = all.FirstOrDefault(i => area.Contains(i.InteractionCell)
					&& GatheringWorker_Concert.InstrumentAccessible(i, p));
				sb.AppendLine("   " + p.LabelShort.PadRight(14)
					+ " duty=" + (p.mindState.duty?.def?.defName ?? "none")
					+ " job=" + (p.CurJobDef?.defName ?? "none")
					+ " artistic=" + (p.skills?.GetSkill(SkillDefOf.Artistic)?.Level ?? -1)
					+ " joy=" + (p.needs?.joy?.CurLevel.ToString("0.00") ?? "n/a")
					+ " food=" + (p.needs?.food?.CurLevelPercentage.ToString("0.00") ?? "n/a")
					+ " schedule=" + (p.timetable?.CurrentAssignment?.defName ?? "n/a")
					+ " can take an instrument=" + (free != null));
			}
			return sb.ToString();
		}

		public void DebugRushWeddings()
		{
			if (weddings.Count == 0)
			{
				Messages.Message("RimFeast: no wedding pending.", MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			foreach (PendingWedding w in weddings) w.fireTick = Find.TickManager.TicksGame;
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

		private static bool Brazen(Pawn speaker, Faction house)
		{
			if (FeastUtility.DrunkennessOf(speaker) > 0.3f) return true;
			if (house != null && house.PlayerGoodwill <= 0) return true;
			TraitSet t = speaker.story?.traits;
			return t != null && (t.HasTrait(TraitDefOf.Abrasive) || t.HasTrait(TraitDefOf.Psychopath)
				|| t.HasTrait(TraitDefOf.Bloodlust));
		}

		private const int WardMinAge = 8;
		private const int WardReturnAge = 13;

		private static Pawn SpeakerOf(FeastCase c)
		{
			if (c.lord == null) return null;
			return c.lord.ownedPawns
				.Where(p => !p.Dead && p.Spawned && !p.Downed)
				.OrderByDescending(p => p.Faction == c.house ? 99999f : p.kindDef?.combatPower ?? 0f)
				.FirstOrDefault();
		}

		// l'equipement porte n'est pas sur la carte, listerThings ne le voit pas
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
			return bestValue >= 100f ? best : null;
		}

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
				// Active = l'oeuvre a une histoire, donc un titre
				if (t.TryGetComp<CompArt>()?.Active != true) continue;
				if (!area.Contains(t.Position)) continue;
				float v = t.MarketValue;
				if (v > bestValue) { bestValue = v; best = t; }
			}
			return bestValue >= 100f ? best : null;
		}

		private static bool PickRequest(FeastCase c, Pawn speaker, int forceKind = ReqNone)
		{
			var options = new List<int>();

			Thing weapon = BestGiftWeapon(c.map);
			if (weapon != null) options.Add(ReqWeapon);

			Thing art = BestGiftArt(c);
			if (art != null) options.Add(ReqArt);

			Pawn child = c.map.mapPawns.FreeColonistsSpawned
				.Where(p => p.DevelopmentalStage == DevelopmentalStage.Child
					&& p.ageTracker.AgeBiologicalYears >= WardMinAge
					&& !p.IsQuestLodger()
					&& !QuestUtility.IsReservedByQuestOrQuestBeingGenerated(p))
				.OrderByDescending(p => p.ageTracker.AgeBiologicalYears)
				.FirstOrDefault();
			if (child != null && c.map.mapPawns.FreeColonistsSpawnedCount >= 4) options.Add(ReqWard);

			// pas de nuit qui creerait un lien psychique (gene des highmates): l'invite repart le soir meme
			// et le colon garderait le malus de l'eloignement
			Pawn desired = null;
			if (Brazen(speaker, c.house) || forceKind == ReqBed)
				desired = c.map.mapPawns.FreeColonistsSpawned
					.Where(p => p.DevelopmentalStage.Adult() && !p.Downed && !p.InMentalState
						&& RelationsUtility.AttractedToGender(speaker, p.gender)
						&& !InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(speaker, p))
					.InRandomOrder()
					.OrderByDescending(p => PartnerOf(p) != null ? 1 : 0)
					.FirstOrDefault();
			if (desired != null) options.Add(ReqBed);

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

		private static int WardReturnTick(Pawn child)
		{
			long toAge = (long)WardReturnAge * GenDate.TicksPerYear - child.ageTracker.AgeBiologicalTicks;
			return Find.TickManager.TicksGame + (int)Mathf.Max(toAge, GenDate.TicksPerYear);
		}

		private static int WardYears(Pawn child) =>
			Mathf.Max(1, Mathf.RoundToInt(
				(WardReturnTick(child) - Find.TickManager.TicksGame) / (float)GenDate.TicksPerYear));

		private static string ArtLabel(Thing t) =>
			t.TryGetComp<CompArt>()?.Title.NullOrEmpty() == false
				? t.TryGetComp<CompArt>().Title
				: t.LabelCap;

		private void TryRequest(FeastCase c)
		{
			if (c.requestRolled || c.lord == null) return;
			if (!RimFeastMod.S.requestsEnabled) return;
			if (Find.TickManager.TicksGame - c.arrivedTick < FeastDurationTicks / 2) return;

			c.requestRolled = true;
			if (!Rand.Chance(RimFeastMod.S.requestChance)) return;

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
				goodwill = Mathf.Clamp(Mathf.RoundToInt(c.requestThing.MarketValue / 50f), 8, 20);
				c.requestThing.Destroy();
				done = "RimFeast_MessageRequestWeaponDone".Translate(label, c.house?.Name);
				Note(c, ChronicleLine.Gave, label);
			}
			else if (c.requestKind == ReqArt)
			{
				string label = ArtLabel(c.requestThing);
				goodwill = Mathf.Clamp(Mathf.RoundToInt(c.requestThing.MarketValue / 50f), 10, 22);
				c.requestThing.Destroy();
				done = "RimFeast_MessageRequestArtDone".Translate(label, c.house?.Name);
				Note(c, ChronicleLine.Gave, label);
			}
			else if (c.requestKind == ReqWard)
			{
				Pawn kid = c.requestChild;
				string label = kid.LabelShortCap;
				int years = WardYears(kid);
				int back = WardReturnTick(kid);

				if (kid.Drafted) kid.drafter.Drafted = false;
				kid.jobs?.StopAll();
				kid.GetLord()?.Notify_PawnLost(kid, PawnLostCondition.LeftVoluntarily);
				kid.SetFaction(c.house);
				c.lord.AddPawn(kid);

				// epingle avant qu'il quitte la carte
				Find.WorldPawns.ForcefullyKeptPawns.Add(kid);
				wards.Add(new PendingWard
				{
					ward = kid, house = c.house, map = c.map,
					leftTick = Find.TickManager.TicksGame, returnTick = back,
				});

				goodwill = 25;
				done = "RimFeast_MessageRequestWardDone".Translate(label, c.house?.Name, years);
				Note(c, ChronicleLine.Ward, label);
			}
			else if (c.requestKind == ReqBed)
			{
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
				Note(c, ChronicleLine.Rival, rival.Name);
			}

			c.requestKind = ReqNone;
			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, goodwill, canSendMessage: true,
				canSendHostilityLetter: false);
			Messages.Message(done, new TargetInfo(c.spotCell, c.map),
				MessageTypeDefOf.PositiveEvent, historical: false);
		}

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
			StartRedWeddingMusic(c);

			Job job = JobMaker.MakeJob(RimFeastDefOf.RimFeast_AssassinateJob, victim);
			job.locomotionUrgency = LocomotionUrgency.Sprint;
			assassin.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			Messages.Message("RimFeast_MessageSignalGiven".Translate(assassin.LabelShortCap, victim.LabelShortCap),
				victim, MessageTypeDefOf.NeutralEvent, historical: false);
			return true;
		}

		public bool CanAnswerInSteel(int id)
		{
			FeastCase c = current;
			return c != null && c.id == id && c.planned && c.requestKind != ReqNone
				&& c.state == FeastCase.Feasting
				&& c.requestSpeaker != null && !c.requestSpeaker.Dead && c.requestSpeaker.Spawned;
		}

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

		private const int LovinGiveUpTicks = 15000;

		private void TickLovin()
		{
			LovinScene s = lovin;
			if (s.state == LovinScene.Pending)
			{
				FeastCase c = current;
				if (c == null || c.id != s.caseId || c.state != FeastCase.Feasting || c.lord == null)
				{
					lovin = null;
					return;
				}
				if (c.lord.CurLordToil is LordToil_Toast) return;
				if (!StartLovin(c, s)) lovin = null;
				return;
			}
			bool busy = InLovin(s.her) || InLovin(s.him);
			if (busy && Find.TickManager.TicksGame - s.startTick < LovinGiveUpTicks) return;
			RestoreLovin(s);
			lovin = null;
		}

		private static bool InLovin(Pawn p) => p != null && p.Spawned && p.CurJobDef == JobDefOf.Lovin;

		// vanilla fait marcher le noble seul et ne lance la partenaire qu'a son arrivee
		private static bool StartLovin(FeastCase c, LovinScene s)
		{
			Pawn her = s.her, him = s.him;
			if (her == null || him == null || !her.Spawned || !him.Spawned || her.Dead || him.Dead
				|| her.Downed || him.Downed || her.InMentalState || him.InMentalState) return false;
			Building_Bed bed = FindLovinBed(c.map, him);
			if (bed == null) return false;

			s.owners = bed.OwnersForReading.ToList();
			s.herBed = her.ownership?.OwnedBed;
			him.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Lovin, her, bed), JobTag.Misc);
			if (him.CurJobDef != JobDefOf.Lovin) return false;
			s.bed = bed;
			s.state = LovinScene.Running;
			s.startTick = Find.TickManager.TicksGame;
			return true;
		}

		// le driver vanilla fait revendiquer le lit aux deux: chacun retrouve le sien
		private static void RestoreLovin(LovinScene s)
		{
			if (s.bed != null && !s.bed.Destroyed)
			{
				foreach (Pawn p in s.bed.OwnersForReading.ToList()) p.ownership?.UnclaimBed();
				foreach (Pawn p in s.owners)
					if (p != null && !p.Dead) p.ownership?.ClaimBedIfNonMedical(s.bed);
			}
			if (s.herBed != null && !s.herBed.Destroyed && s.her != null && !s.her.Dead
				&& s.her.ownership != null && s.her.ownership.OwnedBed != s.herBed)
				s.her.ownership.ClaimBedIfNonMedical(s.herBed);
		}

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
				Note(c, ChronicleLine.BedNo, Who(her), Who(him));
				return;
			}

			her.needs?.mood?.thoughts?.memories?.TryGainMemory(RimFeastDefOf.RimFeast_NightWithNoble);
			partner?.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.CheatedOnMe, her);
			lovin = new LovinScene { caseId = c.id, her = her, him = him };
			Note(c, ChronicleLine.BedYes, Who(her), Who(him));

			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, 18, canSendMessage: true,
				canSendHostilityLetter: false);
			Messages.Message("RimFeast_MessageBedAccepted".Translate(her.LabelShortCap, him.LabelShortCap),
				her, MessageTypeDefOf.PositiveEvent, historical: false);
		}

		public void RefuseRequest(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id) return;
			Note(c, ChronicleLine.Refused);
			c.requestKind = ReqNone;
			c.house?.TryAffectGoodwillWith(Faction.OfPlayer, -8, canSendMessage: true,
				canSendHostilityLetter: false);
			Messages.Message("RimFeast_MessageRequestRefused".Translate(c.house?.Name),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.NegativeEvent, historical: false);
		}

		public void DeclineProposal(int id)
		{
			FeastCase c = current;
			if (c == null || c.id != id) return;
			Note(c, ChronicleLine.Declined, Who(c.betrothedNoble), Who(c.betrothedColonist));
			Messages.Message("RimFeast_MessageProposalDeclined".Translate(c.house?.Name),
				new TargetInfo(c.spotCell, c.map), MessageTypeDefOf.NeutralEvent, historical: false);
		}

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
			if (c.grand)
			{
				ApplyGrandOutcome(c);
				return;
			}
			int score = ComputeScore(c);

			string label, text;
			int goodwill;
			if (score >= TriumphScore)
			{
				label = "RimFeast_LabelFeastTriumph".Translate();
				text = "RimFeast_TextFeastTriumph".Translate(c.house?.Name, GiftSilver);
				goodwill = 25;
				DropGift(c);
				Note(c, ChronicleLine.Silver, null, null, GiftSilver);
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

			// calcule avant d'enregistrer ce banquet, sinon il se penalise lui-meme
			int remembered = FeastsRemembered(c.house);
			if (!c.wedding && goodwill > 0 && remembered > 0)
			{
				goodwill = Mathf.Max(1, Mathf.RoundToInt(goodwill * GoodwillFactorFor(c.house)));
				text += "\n\n" + "RimFeast_TextJaded".Translate(c.house?.Name, remembered);
			}

			if (c.leaderCame && goodwill != 0)
			{
				goodwill = Mathf.RoundToInt(goodwill * 1.5f);
				text += "\n\n" + (goodwill > 0 ? "RimFeast_TextLeaderHonored" : "RimFeast_TextLeaderShamed")
					.Translate(c.house?.Name);
			}

			text += "\n\n" + HallReport(c);

			Find.LetterStack.ReceiveLetter(label, text,
				score >= DecentScore ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent,
				new TargetInfo(c.spotCell, c.map), c.house);

			if (goodwill != 0)
				c.house?.TryAffectGoodwillWith(Faction.OfPlayer, goodwill, canSendMessage: true,
					canSendHostilityLetter: goodwill < 0);

			ChronicleEntry entry = EntryFor(c);
			entry.score = score;
			entry.tier = TierOf(score);
			entry.goodwill += goodwill;
			Stamp(entry, c);

			// des noces ne lassent pas la maison: c'est elle qui les a voulues
			HouseFeastMemory m = MemoryFor(c.house, true);
			if (m != null && !c.wedding)
			{
				m.timesFeasted = remembered + 1;
				m.lastFeastTick = Find.TickManager.TicksGame;
			}

			if (score >= GoodScore && c.house != null && c.map != null)
				OfferTradePact(c, score >= TriumphScore);
		}

		private void OfferTradePact(FeastCase c, bool playerPicks)
		{
			if (!RimFeastMod.S.tradePactEnabled) return;
			if (c.house?.def?.caravanTraderKinds == null || c.house.def.caravanTraderKinds.Count == 0) return;
			PendingCaravan p = ScheduleCaravan(c.house, c.map);
			Note(c, ChronicleLine.Caravan);
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

			// sans DLC pas d'instrument: la musique est reversee au toast
			bool music = FeastUtility.MusicPossible;
			score += Mathf.Min(c.toastsGiven / (float)ToastsForFull, 1f) * (music ? 10f : 15f);

			if (music)
			{
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

		public List<ChronicleEntry> Chronicle => chronicle;

		private static string Who(Pawn p) => p?.LabelShortCap ?? "?";

		private static int TierOf(int score) =>
			score >= TriumphScore ? 3 : score >= GoodScore ? 2 : score >= DecentScore ? 1 : 0;

		// la fiche de la soiree s'ouvre a la premiere chose a noter, le verdict s'y ajoute a la fin
		private ChronicleEntry EntryFor(FeastCase c)
		{
			ChronicleEntry entry = chronicle.FirstOrDefault(x => x.caseId == c.id);
			if (entry != null) return entry;
			entry = new ChronicleEntry
			{
				caseId = c.id,
				kind = c == colony ? ChronicleEntry.Colony : c.wedding ? ChronicleEntry.Wedding : ChronicleEntry.Feast,
				absTick = Find.TickManager.TicksAbs,
				place = c.map != null ? Find.WorldGrid.LongLatOf(c.map.Tile) : Vector2.zero,
				house = c.house,
				houseName = c.house?.Name,
				guests = c.guestCount,
				occasion = c.occasion,
				grand = c.grand,
			};
			if (c.occasion == OccasionWake) entry.Add(ChronicleLine.InMemory, c.honoredName);
			if (c.grand)
			{
				entry.house = null;
				entry.houseName = null;
				foreach (Faction h in c.leaderHouses)
					if (h?.leader != null) entry.Add(ChronicleLine.Leader, Who(h.leader));
			}
			Pawn head = c.leaderCame && !c.grand ? c.house?.leader : null;
			if (head != null) entry.Add(ChronicleLine.Leader, Who(head));
			chronicle.Add(entry);
			if (chronicle.Count > MaxChronicle) chronicle.RemoveAt(0);
			return entry;
		}

		private void Note(FeastCase c, int kind, string a = null, string b = null, int n = 0)
		{
			if (c != null) EntryFor(c).Add(kind, a, b, n);
		}

		private static void Stamp(ChronicleEntry entry, FeastCase c)
		{
			entry.guests = c.guestCount;
			entry.toasts = c.toastsGiven;
			entry.hall = Mathf.Clamp01(c.maxImpressiveness / ImpressivenessCap);
			entry.table = Mathf.Clamp01(c.maxTableScore / 20f);
			entry.drink = c.guestCount > 0 ? Mathf.Min((float)c.maxDrinkUnits / c.guestCount, 1f) : 0f;
			entry.ent = Mathf.Min(c.toastsGiven / (float)ToastsForFull, 1f);
			if (c.samples > 0 && c.musicSamples > 0) entry.ent = Mathf.Max(entry.ent, 0.7f);
		}

		private void NoteAway(Caravan caravan, Faction house, int outcome)
		{
			chronicle.Add(new ChronicleEntry
			{
				kind = ChronicleEntry.Away,
				absTick = Find.TickManager.TicksAbs,
				place = caravan != null ? Find.WorldGrid.LongLatOf(caravan.Tile) : Vector2.zero,
				house = house,
				houseName = house?.Name,
				tier = outcome,
			});
			if (chronicle.Count > MaxChronicle) chronicle.RemoveAt(0);
		}

		private FeastCase CaseAt(Pawn p)
		{
			if (p?.Map == null) return null;
			if (current != null && current.map == p.Map && current.state == FeastCase.Feasting) return current;
			return colony != null && colony.map == p.Map ? colony : null;
		}

		public void Notify_Danced(Pawn a, Pawn b) => Note(CaseAt(a), ChronicleLine.Dance, Who(a), Who(b));

		public void Notify_Conga(Pawn leader, int size) => Note(CaseAt(leader), ChronicleLine.Conga, Who(leader), null, size);

		public void Notify_Round(Pawn leader, int size) => Note(CaseAt(leader), ChronicleLine.Round, Who(leader), null, size);

		public void DebugChronicleSamples()
		{
			Map map = Find.CurrentMap;
			Vector2 place = map != null ? Find.WorldGrid.LongLatOf(map.Tile) : Vector2.zero;
			Faction house = FeastUtility.InvitableHouses().FirstOrDefault();
			List<Pawn> cols = map?.mapPawns.FreeColonistsSpawned.ToList() ?? new List<Pawn>();
			string Col(int i) => cols.Count > 0 ? Who(cols[i % cols.Count]) : "Colonist " + (i + 1);
			int now = Find.TickManager.TicksAbs;

			var feast = new ChronicleEntry
			{
				kind = ChronicleEntry.Feast, absTick = now - 9 * GenDate.TicksPerDay, place = place, house = house,
				houseName = house?.Name, score = 88, tier = 3, goodwill = 25, guests = 7, toasts = 3,
				hall = 0.9f, table = 0.85f, drink = 1f, ent = 1f,
			};
			feast.Add(ChronicleLine.Leader, "Lord Aldric");
			feast.Add(ChronicleLine.Gift);
			feast.Add(ChronicleLine.Dance, Col(1), "Ser Rowan");
			feast.Add(ChronicleLine.Conga, Col(2), null, 6);
			feast.Add(ChronicleLine.Round, Col(3), null, 5);
			feast.Add(ChronicleLine.Betrothed, "Ser Rowan", Col(0));
			feast.Add(ChronicleLine.Silver, null, null, GiftSilver);
			feast.Add(ChronicleLine.Caravan);
			chronicle.Add(feast);

			var wedding = new ChronicleEntry
			{
				kind = ChronicleEntry.Wedding, absTick = now - 5 * GenDate.TicksPerDay, place = place, house = house,
				houseName = house?.Name, score = 71, tier = 2, goodwill = 15, guests = 9, toasts = 3,
				hall = 0.8f, table = 0.6f, drink = 0.7f, ent = 1f,
			};
			wedding.Add(ChronicleLine.Gift);
			wedding.Add(ChronicleLine.Wed, Col(0), "Ser Rowan");
			wedding.Add(ChronicleLine.FirstDance, Col(0), "Ser Rowan");
			wedding.Add(ChronicleLine.Dance, Col(1), Col(2));
			wedding.Add(ChronicleLine.Brawl, "Knight Osric", "Knight Hale");
			chronicle.Add(wedding);

			var colonyFeast = new ChronicleEntry
			{
				kind = ChronicleEntry.Colony, absTick = now - 3 * GenDate.TicksPerDay, place = place,
				score = 52, tier = 1, guests = Mathf.Max(3, cols.Count), toasts = 2,
				hall = 0.7f, table = 0.4f, drink = 0.5f, ent = 0.7f,
			};
			colonyFeast.Add(ChronicleLine.Dance, Col(1), Col(3));
			chronicle.Add(colonyFeast);

			var red = new ChronicleEntry
			{
				kind = ChronicleEntry.Feast, absTick = now, place = place, house = house, houseName = house?.Name,
				guests = 6, toasts = 1, hall = 0.9f, table = 0.5f, drink = 0.8f, ent = 0.4f, red = true,
			};
			red.Add(ChronicleLine.Slain, "Lord Aldric");
			red.Add(ChronicleLine.Slain, "Knight Osric");
			red.Add(ChronicleLine.Slain, "Knight Hale");
			red.Add(ChronicleLine.Fallen, "Dame Ysolde");
			red.Add(ChronicleLine.Lost, Col(2));
			red.Add(ChronicleLine.War, house?.Name ?? "House Amboise");
			chronicle.Add(red);

			chronicle.Add(new ChronicleEntry
			{
				kind = ChronicleEntry.Away, absTick = now - 14 * GenDate.TicksPerDay, place = place, house = house,
				houseName = house?.Name, tier = AwayFine,
			});
			chronicle.SortBy(x => x.absTick);
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
			Scribe_Deep.Look(ref lovin, "lovin");
			Scribe_Deep.Look(ref colony, "colony");
			Scribe_Values.Look(ref lastColonyFeastTick, "lastColonyFeastTick", -1);
			Scribe_Collections.Look(ref weddings, "weddings", LookMode.Deep);
			Scribe_Collections.Look(ref chronicle, "chronicle", LookMode.Deep);
			Scribe_Collections.Look(ref wakesHeld, "wakesHeld", LookMode.Value);
			Scribe_Values.Look(ref lastVictoryThreatTick, "lastVictoryThreatTick", -1);
			Scribe_Values.Look(ref lastHarvestFeastTick, "lastHarvestFeastTick", -1);
			Scribe_Values.Look(ref lastGrandFeastTick, "lastGrandFeastTick", -1);
			Scribe_Collections.Look(ref peace, "peace", LookMode.Value);
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
				if (weddings == null) weddings = new List<PendingWedding>();
				weddings.RemoveAll(x => x == null);
				if (chronicle == null) chronicle = new List<ChronicleEntry>();
				chronicle.RemoveAll(x => x == null);
				if (wakesHeld == null) wakesHeld = new List<int>();
				if (peace == null) peace = new List<int>();
			}
		}
	}
}
