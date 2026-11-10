using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using RimFeast.Jobs;

namespace RimFeast.AI
{
	// un convive qui a deja bu en defie un autre. chacun va chercher ses chopes, et les voisins libres viennent faire cercle
	public class JobGiver_ContestAtFeast : ThinkNode_JobGiver
	{
		private const float Tipsy = 0.15f;
		private const int MinMugs = 6;
		private const int MaxWatchers = 6;

		protected override Job TryGiveJob(Pawn pawn) => TryContest(pawn, false, out _);

		private static bool Fit(Pawn p) =>
			FeastUtility.CanDance(p) && !p.DevelopmentalStage.Juvenile() && !p.IsTeetotaler();

		public static Job TryContest(Pawn pawn, bool force, out string why)
		{
			why = null;
			PawnDuty duty = pawn.mindState.duty;
			Map map = pawn.Map;
			if (duty == null || map == null || !FeastUtility.IsFeaster(pawn)) { why = "not at a feast"; return null; }
			if (FeastUtility.AlcoholHigh == null || !Fit(pawn)) { why = "wont drink"; return null; }
			if (!force && FeastUtility.DrunkennessOf(pawn) < Tipsy) { why = "has not had a drink yet"; return null; }
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp == null || (!force && !comp.ContestAllowed(pawn))) { why = "enough contests for one feast"; return null; }
			IntVec3 spot = duty.focus.Cell;
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(spot, map);
			if (JobDriver_DrinkContest.Mugs(map, area) < MinMugs) { why = "less than " + MinMugs + " drinks on the tables"; return null; }

			List<Pawn> feasters = map.mapPawns.AllPawnsSpawned
				.Where(p => p != pawn && FeastUtility.IsFeaster(p) && area.Contains(p.Position)).ToList();
			if (feasters.Any(p => p.CurJobDef == RimFeastDefOf.RimFeast_ContestJob)) { why = "a contest is already going"; return null; }
			Pawn rival = feasters
				.Where(p => Fit(p) && JobGiver_DanceAtFeast.Free(p) && (force || FeastUtility.DrunkennessOf(p) >= Tipsy))
				.OrderBy(p => p.Position.DistanceToSquared(pawn.Position)).FirstOrDefault();
			if (rival == null) { why = "nobody around who has been drinking"; return null; }
			if (!FindMugs(pawn, area, null, 0, out Thing mugsMine, out int countMine)
				|| !FindMugs(rival, area, mugsMine, countMine, out Thing mugsTheirs, out int countTheirs))
			{
				why = "no drink both can reach and take";
				return null;
			}
			if (!JobGiver_DanceAtFeast.FindFloor(pawn, rival, spot, area, out IntVec3 mine, out IntVec3 theirs))
			{
				why = "no two free cells side by side";
				return null;
			}

			Job theirJob = JobMaker.MakeJob(RimFeastDefOf.RimFeast_ContestJob, pawn, theirs, mugsTheirs);
			theirJob.count = countTheirs;
			theirJob.expiryInterval = 5000;
			if (!rival.jobs.TryTakeOrderedJob(theirJob, JobTag.Misc)) return null;
			comp.Notify_ContestStarted(pawn);
			Messages.Message("RimFeast_ContestStarts".Translate(pawn.LabelShort, rival.LabelShort), pawn,
				MessageTypeDefOf.NeutralEvent, historical: false);

			// le cercle: une case chacun autour des deux buveurs. ils regardent celui qui tient les comptes
			Pawn judge = JobDriver_DrinkContest.JudgeOf(pawn, rival);
			CellRect ring = CellRect.FromLimits(mine, theirs);
			var used = new List<IntVec3> { mine, theirs };
			int watchers = 0;
			foreach (Pawn p in feasters
				.Where(x => x != rival && FeastUtility.CanDance(x) && JobGiver_DanceAtFeast.Free(x)
					&& x.carryTracker?.CarriedThing == null)
				.OrderBy(x => x.Position.DistanceToSquared(pawn.Position)))
			{
				if (watchers >= MaxWatchers) break;
				if (!SpectatorCellFinder.TryFindSpectatorCellFor(p, ring, map, out IntVec3 cell, SpectateRectSide.All, 1, used,
					null, (c, who, m) => area.Contains(c)))
					continue;
				Job watch = JobMaker.MakeJob(RimFeastDefOf.RimFeast_WatchContestJob, judge, cell);
				watch.expiryInterval = 4000;
				if (!p.jobs.TryTakeOrderedJob(watch, JobTag.Misc)) continue;
				used.Add(cell);
				watchers++;
			}

			Job job = JobMaker.MakeJob(RimFeastDefOf.RimFeast_ContestJob, rival, mine, mugsMine);
			job.count = countMine;
			job.expiryInterval = 5000;
			return job;
		}

		// la pile de boissons ou se servir: assez pour toutes les manches si possible, la plus proche sinon.
		// la meme pile peut servir aux deux, une fois deduit ce que le premier emporte
		private static bool FindMugs(Pawn p, FeastUtility.FeastArea area, Thing taken, int takenCount, out Thing mugs,
			out int count)
		{
			mugs = null;
			count = 0;
			float best = float.MaxValue;
			List<Thing> drugs = p.Map.listerThings.ThingsInGroup(ThingRequestGroup.Drug);
			for (int i = 0; i < drugs.Count; i++)
			{
				Thing t = drugs[i];
				if (!JobDriver_DrinkContest.IsMug(t) || !area.Contains(t.Position) || t.IsForbidden(p)) continue;
				int n = Mathf.Min(JobDriver_DrinkContest.MaxRounds, t.stackCount - (t == taken ? takenCount : 0));
				if (n < 2 || !p.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.None, 10, n)) continue;
				float score = t.Position.DistanceToSquared(p.Position) + (n < JobDriver_DrinkContest.MaxRounds ? 100000f : 0f);
				if (score >= best) continue;
				best = score;
				mugs = t;
				count = n;
			}
			return mugs != null;
		}
	}
}
