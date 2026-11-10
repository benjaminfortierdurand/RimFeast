using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	// le convive cherche un partenaire et deux cases libres, puis lui donne la meme danse
	public class JobGiver_DanceAtFeast : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn) => TryDance(pawn, false, out _);

		public static Job TryDance(Pawn pawn, bool force, out string why)
		{
			why = null;
			PawnDuty duty = pawn.mindState.duty;
			Map map = pawn.Map;
			if (duty == null || map == null || !FeastUtility.IsFeaster(pawn)) { why = "not at a feast"; return null; }
			if (!FeastUtility.CanDance(pawn)) { why = "cant dance right now"; return null; }
			IntVec3 spot = duty.focus.Cell;
			if (!force && !FeastUtility.HallIsLively(spot, map)) { why = "no music yet"; return null; }
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(spot, map);

			List<Pawn> feasters = map.mapPawns.AllPawnsSpawned
				.Where(p => p != pawn && FeastUtility.IsFeaster(p) && area.Contains(p.Position)).ToList();
			int dancing = feasters.Count(p => p.CurJobDef == RimFeastDefOf.RimFeast_DanceJob);
			if (!force && dancing >= Mathf.Max(2, (feasters.Count + 1) * 2 / 5)) { why = "the floor is full"; return null; }

			Pawn partner = PickPartner(pawn, feasters);
			if (partner == null) { why = "no partner among " + feasters.Count + " feasters in the area"; return null; }
			if (!FindFloor(pawn, partner, spot, area, out IntVec3 mine, out IntVec3 theirs))
			{
				why = "no two free cells side by side for " + partner.LabelShort;
				return null;
			}

			Job theirJob = JobMaker.MakeJob(RimFeastDefOf.RimFeast_DanceJob, pawn, theirs, mine);
			if (!partner.jobs.TryTakeOrderedJob(theirJob, JobTag.Misc)) return null;
			if (pawn.Faction != Faction.OfPlayer && partner.Faction == Faction.OfPlayer)
				Messages.Message("RimFeast_DanceInvite".Translate(pawn.LabelShort, partner.LabelShort), pawn,
					MessageTypeDefOf.NeutralEvent, historical: false);
			return JobMaker.MakeJob(RimFeastDefOf.RimFeast_DanceJob, partner, mine, theirs);
		}

		// la premiere danse des maries: pas de tirage ni de plafond, juste deux cases libres
		public static bool StartDance(Pawn a, Pawn b, IntVec3 spot)
		{
			Map map = a.Map;
			if (map == null || b.Map != map || !FeastUtility.CanDance(a) || !FeastUtility.CanDance(b)) return false;
			if (a.CurJobDef == RimFeastDefOf.RimFeast_DanceJob || b.CurJobDef == RimFeastDefOf.RimFeast_DanceJob) return false;
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(spot, map);
			if (!FindFloor(a, b, spot, area, out IntVec3 mine, out IntVec3 theirs)) return false;
			Job theirJob = JobMaker.MakeJob(RimFeastDefOf.RimFeast_DanceJob, a, theirs, mine);
			if (!b.jobs.TryTakeOrderedJob(theirJob, JobTag.Misc)) return false;
			return a.jobs.TryTakeOrderedJob(JobMaker.MakeJob(RimFeastDefOf.RimFeast_DanceJob, b, mine, theirs), JobTag.Misc);
		}

		// un conjoint present passe avant tout, et on ne vole pas celui d'un autre
		private static Pawn PickPartner(Pawn pawn, List<Pawn> feasters)
		{
			Pawn own = LovePartnerRelationUtility.ExistingLovePartner(pawn, false);
			bool ownHere = own != null && feasters.Contains(own);
			Pawn best = null;
			float bestScore = 0f;
			foreach (Pawn other in feasters)
			{
				if (ownHere && other != own) continue;
				if (!FeastUtility.CanDance(other) || !Free(other)) continue;
				if (pawn.DevelopmentalStage.Adult() != other.DevelopmentalStage.Adult()) continue;
				Pawn theirs = LovePartnerRelationUtility.ExistingLovePartner(other, false);
				if (theirs != null && theirs != pawn && feasters.Contains(theirs)) continue;
				float score = Score(pawn, other);
				if (score > bestScore)
				{
					bestScore = score;
					best = other;
				}
			}
			return best;
		}

		private static float Score(Pawn a, Pawn b)
		{
			if (LovePartnerRelationUtility.LovePartnerRelationExists(a, b)) return 100f;
			int opinion = Mathf.Min(a.relations?.OpinionOf(b) ?? 0, b.relations?.OpinionOf(a) ?? 0);
			bool attracted = a.DevelopmentalStage.Adult()
				&& RelationsUtility.AttractedToGender(a, b.gender) && RelationsUtility.AttractedToGender(b, a.gender);
			if (attracted && opinion >= -10) return 30f + opinion * 0.5f + Rand.Range(0f, 15f);
			if (opinion >= 15) return 10f + opinion * 0.3f + Rand.Range(0f, 10f);
			return 0f;
		}

		internal static bool Free(Pawn p)
		{
			if (p.Drafted || p.InMentalState || p.Downed || !p.Awake()) return false;
			PawnDuty duty = p.mindState.duty;
			if (duty != null && (duty.def == RimFeastDefOf.RimFeast_ToastDuty || duty.def == RimFeastDefOf.RimFeast_MinstrelDuty))
				return false;
			Job cur = p.CurJob;
			if (cur == null) return true;
			if (cur.playerForced || cur.def == RimFeastDefOf.RimFeast_DanceJob || cur.def == RimFeastDefOf.RimFeast_CongaJob
				|| cur.def == RimFeastDefOf.RimFeast_RoundJob)
				return false;
			if (cur.def == JobDefOf.Ingest || cur.def == RimFeastDefOf.RimFeast_EatAtSeatJob) return false;
			if (p.jobs.curDriver is JobDriver_PlayMusicalInstrument) return false;
			return cur.def.casualInterruptible;
		}

		private static bool FindFloor(Pawn a, Pawn b, IntVec3 spot, FeastUtility.FeastArea area, out IntVec3 ca, out IntVec3 cb)
		{
			Map map = a.Map;
			int n = GenRadial.NumCellsInRadius(area.Reach());
			int start = Rand.Range(0, Mathf.Min(n, 40));
			for (int k = 0; k < n; k++)
			{
				IntVec3 c = spot + GenRadial.RadialPattern[(start + k) % n];
				IntVec3 d = c + IntVec3.East;
				if (!Floor(c, map, area, a) || !Floor(d, map, area, b)) continue;
				if (!a.CanReserveAndReach(c, PathEndMode.OnCell, Danger.None)) continue;
				if (!b.CanReserveAndReach(d, PathEndMode.OnCell, Danger.None)) continue;
				ca = c;
				cb = d;
				return true;
			}
			ca = cb = IntVec3.Invalid;
			return false;
		}

		private static bool Floor(IntVec3 c, Map map, FeastUtility.FeastArea area, Pawn p) =>
			c.InBounds(map) && area.Contains(c) && c.Standable(map) && c.GetEdifice(map) == null
			&& !c.IsForbidden(p) && c.GetFirstPawn(map) == null
			&& map.pawnDestinationReservationManager.CanReserve(c, p);
	}
}
