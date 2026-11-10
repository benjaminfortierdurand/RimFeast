using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimFeast.Jobs
{
	// le concours de boisson: chacun va prendre ses chopes sur les tables du banquet, puis les deux se font face
	// et en vident une par manche, jusqu'a ce que l'un des deux cale.
	// celui qui a le plus petit numero tient les comptes pour les deux. count est le nombre de chopes a emporter
	public class JobDriver_DrinkContest : JobDriver
	{
		public const int MaxRounds = 6;
		private const TargetIndex RivalInd = TargetIndex.A;
		private const TargetIndex MineInd = TargetIndex.B;
		private const TargetIndex MugInd = TargetIndex.C;
		private const int WaitTicks = 1500;
		private const int FirstGulp = 90;
		private const int RoundTicks = 200;
		private const int GulpTicks = 30;
		private const int SettleTicks = 120;
		// un invite qui tombe ivre mort compterait comme blesse sous ton toit: il cale toujours avant
		private const float GuestCeiling = 0.85f;

		private int beginTick = -1;
		private int overTick = -1;
		private int gulpTick = -1;
		private int rounds;
		private int result = -1;
		private float myLimit = 1f;
		private float theirLimit = 1f;
		private IntVec3 shelf = IntVec3.Invalid;
		private bool atTable;
		private SoundDef sip;
		private Sustainer sipSound;
		private int sipTick = -1;

		private Pawn Rival => job.GetTarget(RivalInd).Pawn;
		private IntVec3 Mine => job.GetTarget(MineInd).Cell;
		private Thing Held => pawn.carryTracker?.CarriedThing;
		private bool Judging => Rival != null && pawn.thingIDNumber < Rival.thingIDNumber;

		private JobDriver_DrinkContest Judge
		{
			get
			{
				if (Judging) return this;
				return Rival?.jobs?.curDriver is JobDriver_DrinkContest d && d.Rival == pawn ? d : null;
			}
		}

		// vu depuis celui qui tient les comptes: 1 il a gagne, 2 c'est l'autre, 0 egalite
		private Pawn Loser => result == 1 ? Rival : result == 2 ? pawn : null;

		public int GulpTick => gulpTick;
		public bool Over => result >= 0;
		public int OverTick => overTick;

		public static Pawn JudgeOf(Pawn a, Pawn b) => a.thingIDNumber < b.thingIDNumber ? a : b;

		public static bool IsDrink(Thing t) =>
			t.def.IsDrug && t.IngestibleNow && t.def.ingestible?.drugCategory == DrugCategory.Social
			&& FeastUtility.AlcoholIn(t.def) > 0f;

		public static bool IsMug(Thing t) => t.Spawned && IsDrink(t);

		public static int Mugs(Map map, FeastUtility.FeastArea area)
		{
			int n = 0;
			List<Thing> drugs = map.listerThings.ThingsInGroup(ThingRequestGroup.Drug);
			for (int i = 0; i < drugs.Count; i++)
				if (IsMug(drugs[i]) && area.Contains(drugs[i].Position)) n += drugs[i].stackCount;
			return n;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref beginTick, "beginTick", -1);
			Scribe_Values.Look(ref overTick, "overTick", -1);
			Scribe_Values.Look(ref gulpTick, "gulpTick", -1);
			Scribe_Values.Look(ref rounds, "rounds");
			Scribe_Values.Look(ref result, "result", -1);
			Scribe_Values.Look(ref myLimit, "myLimit", 1f);
			Scribe_Values.Look(ref theirLimit, "theirLimit", 1f);
			Scribe_Values.Look(ref shelf, "shelf", IntVec3.Invalid);
		}

		// la pile se reserve comme pour un repas vanilla: plusieurs peuvent y prendre, chacun sa part
		public override bool TryMakePreToilReservations(bool errorOnFailed) =>
			pawn.Reserve(Mine, job, 1, -1, null, errorOnFailed)
			&& pawn.Reserve(job.GetTarget(MugInd), job, 10, job.count, null, errorOnFailed);

		private bool Together()
		{
			Pawn p = Rival;
			return p != null && p.Spawned && !p.Downed && pawn.Position == Mine
				&& p.CurJob != null && p.CurJob.def == job.def && p.CurJob.GetTarget(RivalInd).Pawn == pawn
				&& p.Position == p.CurJob.GetTarget(MineInd).Cell;
		}

		private int SinceGulp(JobDriver_DrinkContest judge) =>
			judge.gulpTick < 0 ? int.MaxValue : Find.TickManager.TicksGame - judge.gulpTick;

		public override Vector3 ForcedBodyOffset
		{
			get
			{
				JobDriver_DrinkContest judge = Judge;
				if (!atTable || judge == null) return Vector3.zero;
				int now = Find.TickManager.TicksGame;
				int since = SinceGulp(judge);
				if (since < GulpTicks) return new Vector3(0f, 0f, Mathf.Sin(since / (float)GulpTicks * Mathf.PI) * 0.05f);
				if (!judge.Over) return Vector3.zero;
				Pawn loser = judge.Loser;
				// celui qui a cale titube, l'autre saute de joie
				if (loser == pawn) return new Vector3(Mathf.Sin(now / 7f) * 0.06f, 0f, 0f);
				if (loser != null) return new Vector3(0f, 0f, Mathf.Abs(Mathf.Sin(now / 8f)) * 0.14f);
				return Vector3.zero;
			}
		}

		// la chope se porte comme n'importe quel objet, et monte a la bouche a chaque gorgee.
		// rendre vrai court-circuite le placement vanilla: on repart donc du meme point (0.18 sur le cote, 0.1 plus bas)
		public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool flip)
		{
			JobDriver_DrinkContest judge = Judge;
			if (!atTable || judge == null) return false;
			int since = SinceGulp(judge);
			if (since >= GulpTicks) return false;
			float lift = Mathf.Sin(since / (float)GulpTicks * Mathf.PI);
			float side = pawn.Rotation == Rot4.East ? 0.18f : pawn.Rotation == Rot4.West ? -0.18f : 0f;
			drawPos += new Vector3(side * (1f - 0.5f * lift), 0f, -0.1f + 0.38f * lift);
			return true;
		}

		// chacun a sa limite, tiree au debut. un colon peut aller jusqu'au trou noir
		private static float Limit(Pawn p) =>
			(p.Faction == Faction.OfPlayer ? 1.05f : GuestCeiling) - Rand.Range(0f, 0.25f);

		// un petit gabarit prend la chope plus fort que sa valeur de base
		private static float Next(Pawn p, Thing mug) =>
			FeastUtility.DrunkennessOf(p) + FeastUtility.AlcoholIn(mug.def) / Mathf.Clamp(p.BodySize, 0.5f, 1f);

		private static void Drink(Pawn p)
		{
			Thing mug = p.carryTracker?.CarriedThing;
			if (mug == null) return;
			mug.Ingested(p, 0.01f);
			if (p.Faction == Faction.OfPlayer || FeastUtility.AlcoholHigh == null) return;
			Hediff high = p.health?.hediffSet?.GetFirstHediffOfDef(FeastUtility.AlcoholHigh);
			if (high != null && high.Severity > GuestCeiling) high.Severity = GuestCeiling;
		}

		// le bruit de boisson vanilla est une boucle, pas un son bref: le jouer d'un coup jette une erreur.
		// on l'entretient le temps de la gorgee, puis on le coupe. chaque buveur fait le sien
		private void SipSound(JobDriver_DrinkContest judge)
		{
			if (Held != null) sip = Held.def.ingestible?.ingestSound;
			if (judge == null || SinceGulp(judge) >= GulpTicks)
			{
				StopSip();
				return;
			}
			if (sipTick != judge.gulpTick)
			{
				sipTick = judge.gulpTick;
				StopSip();
				if (sip != null && sip.sustain)
					sipSound = sip.TrySpawnSustainer(SoundInfo.InMap(pawn, MaintenanceType.PerTick));
				else if (sip != null) sip.PlayOneShot(SoundInfo.InMap(pawn));
			}
			if (sipSound != null && !sipSound.Ended) sipSound.Maintain();
		}

		private void StopSip()
		{
			if (sipSound != null && !sipSound.Ended) sipSound.End();
			sipSound = null;
		}

		private void Round()
		{
			Pawn rival = Rival;
			Thing mine = Held;
			Thing theirs = rival.carryTracker?.CarriedThing;
			// plus de chope en main: egalite
			if (mine == null || theirs == null || !IsDrink(mine) || !IsDrink(theirs))
			{
				Finish(null);
				return;
			}
			bool iCan = Next(pawn, mine) <= myLimit;
			bool theyCan = Next(rival, theirs) <= theirLimit;
			if (!iCan || !theyCan)
			{
				Finish(iCan ? pawn : theyCan ? rival : null);
				return;
			}
			gulpTick = Find.TickManager.TicksGame;
			rounds++;
			// l'arbitre boit en dernier: s'il s'ecroule, sa tache est deja finie quand on revient ici
			Drink(rival);
			Drink(pawn);
			if (pawn.Downed || rival.Downed) Finish(pawn.Downed == rival.Downed ? null : pawn.Downed ? rival : pawn);
			else if (rounds >= MaxRounds) Finish(null);
		}

		private void Finish(Pawn winner)
		{
			result = winner == null ? 0 : winner == pawn ? 1 : 2;
			overTick = Find.TickManager.TicksGame;
			GameComponent_FeastState.Get()?.Notify_Contest(pawn, Rival, winner, rounds);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => Rival == null || Rival.Dead || !Rival.Spawned);

			Toil note = ToilMaker.MakeToil("NoteShelf");
			note.initAction = delegate
			{
				Thing mugs = job.GetTarget(MugInd).Thing;
				if (!shelf.IsValid && mugs != null && mugs.Spawned) shelf = mugs.Position;
			};
			note.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return note;

			yield return Toils_Goto.GotoThing(MugInd, PathEndMode.ClosestTouch).FailOnDestroyedNullOrForbidden(MugInd);
			yield return Toils_Haul.StartCarryThing(MugInd);
			yield return Toils_Goto.GotoCell(MineInd, PathEndMode.OnCell);

			Toil wait = ToilMaker.MakeToil("WaitForRival");
			wait.tickIntervalAction = delegate(int delta)
			{
				Pawn rival = Rival;
				if (rival != null && rival.Position != pawn.Position) pawn.rotationTracker.FaceCell(rival.Position);
				if (Together()) ReadyForNextToil();
			};
			wait.handlingFacing = true;
			wait.defaultCompleteMode = ToilCompleteMode.Delay;
			wait.defaultDuration = WaitTicks;
			yield return wait;

			Toil contest = ToilMaker.MakeToil("DrinkContest");
			contest.initAction = delegate
			{
				if (!Together())
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				if (!Judging || beginTick >= 0) return;
				beginTick = Find.TickManager.TicksGame;
				myLimit = Limit(pawn);
				theirLimit = Limit(Rival);
			};
			contest.tickAction = delegate
			{
				int now = Find.TickManager.TicksGame;
				atTable = true;
				Pawn rival = Rival;
				if (rival != null && rival.Position != pawn.Position) pawn.rotationTracker.FaceCell(rival.Position);
				JobDriver_DrinkContest judge = Judge;
				SipSound(judge);
				if (judge == null)
				{
					EndJobWith(JobCondition.Succeeded);
					return;
				}
				if (judge.Over)
				{
					if (now - judge.overTick >= SettleTicks) ReadyForNextToil();
					return;
				}
				if (!Judging) return;
				if (!Together())
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				int t = now - beginTick - FirstGulp;
				if (t >= 0 && t % RoundTicks == 0) Round();
			};
			contest.AddFinishAction(delegate
			{
				atTable = false;
				StopSip();
			});
			contest.handlingFacing = true;
			contest.defaultCompleteMode = ToilCompleteMode.Never;
			contest.socialMode = RandomSocialMode.Off;
			yield return contest;

			// les chopes qui restent retournent la ou on les a prises, plutot que par terre au milieu de la salle
			Toil back = ToilMaker.MakeToil("CarryMugsBack");
			back.initAction = delegate
			{
				if (Held == null || !shelf.IsValid || !pawn.CanReach(shelf, PathEndMode.Touch, Danger.None))
				{
					EndJobWith(JobCondition.Succeeded);
					return;
				}
				pawn.pather.StartPath(shelf, PathEndMode.Touch);
			};
			back.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			yield return back;

			Toil put = ToilMaker.MakeToil("PutMugsBack");
			put.initAction = delegate
			{
				if (Held != null) pawn.carryTracker.TryDropCarriedThing(shelf, ThingPlaceMode.Near, out Thing _);
			};
			put.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return put;
		}
	}

	// les curieux font cercle: un saut apres chaque chope, et une ovation quand il y a un vainqueur
	public class JobDriver_WatchContest : JobDriver
	{
		private const TargetIndex JudgeInd = TargetIndex.A;
		private const TargetIndex CellInd = TargetIndex.B;

		private JobDriver_DrinkContest Contest =>
			job.GetTarget(JudgeInd).Pawn?.jobs?.curDriver as JobDriver_DrinkContest;

		public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

		public override Vector3 ForcedBodyOffset
		{
			get
			{
				JobDriver_DrinkContest c = Contest;
				if (c == null || pawn.pather.Moving) return Vector3.zero;
				int now = Find.TickManager.TicksGame;
				if (c.Over) return new Vector3(0f, 0f, Mathf.Abs(Mathf.Sin((now + pawn.thingIDNumber) / 8f)) * 0.14f);
				int since = now - c.GulpTick;
				if (c.GulpTick < 0 || since < 10 || since >= 36) return Vector3.zero;
				return new Vector3(0f, 0f, Mathf.Sin((since - 10) / 26f * Mathf.PI) * 0.2f);
			}
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			// les curieux recoivent leur tache avant celui qui lance le defi: sans ce delai ils lachent aussitot
			this.FailOn(() => Contest == null && Find.TickManager.TicksGame - startTick > 120);
			yield return Toils_Goto.GotoCell(CellInd, PathEndMode.OnCell);

			Toil watch = ToilMaker.MakeToil("WatchContest");
			watch.tickIntervalAction = delegate(int delta)
			{
				Pawn judge = job.GetTarget(JudgeInd).Pawn;
				if (judge != null && judge.Spawned && judge.Position != pawn.Position)
					pawn.rotationTracker.FaceCell(judge.Position);
				JobDriver_DrinkContest c = Contest;
				if (c != null && c.Over && Find.TickManager.TicksGame - c.OverTick >= 110) ReadyForNextToil();
				pawn.needs?.joy?.GainJoy(delta * 0.36f / 2500f, JoyKindDefOf.Social);
			};
			watch.handlingFacing = true;
			watch.defaultCompleteMode = ToilCompleteMode.Never;
			watch.socialMode = RandomSocialMode.SuperActive;
			yield return watch;
		}
	}
}
