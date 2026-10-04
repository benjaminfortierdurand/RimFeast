using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	public class JobGiver_SitAtFeast : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			PawnDuty duty = pawn.mindState.duty;
			if (duty == null) return null;
			IntVec3 spot = duty.focus.Cell;
			if (!spot.IsValid || pawn.Map == null) return null;

			Thing focus = spot.GetFirstBuilding(pawn.Map);
			if (focus == null) return null;

			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(spot, pawn.Map);
			Thing chair = FindChair(pawn, spot, area);
			if (chair == null) return null;

			if (!Toils_Ingest.TryFindFreeSittingSpotOnThing(chair, pawn, out IntVec3 seat))
				seat = chair.Position;

			Job job = JobMaker.MakeJob(RimFeastDefOf.RimFeast_SitAtFeastJob, focus, seat);
			Thing drink = FindDrink(pawn, area);
			if (drink != null)
			{
				job.targetC = drink;
				job.count = 1;
			}
			return job;
		}

		private static Thing FindChair(Pawn pawn, IntVec3 spot, FeastUtility.FeastArea area)
		{
			Thing loose = null;
			int cells = GenRadial.NumCellsInRadius(area.Reach());
			for (int i = 0; i < cells; i++)
			{
				IntVec3 c = spot + GenRadial.RadialPattern[i];
				if (!c.InBounds(pawn.Map)) continue;

				Building edifice = c.GetEdifice(pawn.Map);
				if (edifice == null || edifice.def.building == null || !edifice.def.building.isSittable) continue;
				if (!area.Contains(c)) continue;
				if (edifice.IsForbidden(pawn)) continue;
				if (!Toils_Ingest.TryFindFreeSittingSpotOnThing(edifice, pawn, out IntVec3 sitCell)) continue;
				if (!pawn.CanReserveSittableOrSpot(sitCell)) continue;

				if (BesideTable(edifice)) return edifice;
				if (loose == null) loose = edifice;
			}
			return loose;
		}

		private static bool BesideTable(Building chair)
		{
			foreach (IntVec3 c in GenAdj.CellsAdjacentCardinal(chair))
			{
				if (!c.InBounds(chair.Map)) continue;
				Building b = c.GetEdifice(chair.Map);
				if (b != null && b.def.surfaceType == SurfaceType.Eat) return true;
			}
			return false;
		}

		private static Thing FindDrink(Pawn pawn, FeastUtility.FeastArea area)
		{
			if (pawn.DevelopmentalStage.Juvenile()) return null;
			if (pawn.IsTeetotaler()) return null;
			if (FeastUtility.DrunkennessOf(pawn) > RimFeastMod.S.drinkLimit) return null;
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
				ThingRequest.ForGroup(ThingRequestGroup.Drug), PathEndMode.OnCell,
				TraverseParms.For(pawn), 14f,
				x => x.IngestibleNow && x.def.IsDrug
					&& x.def.ingestible != null && x.def.ingestible.drugCategory == DrugCategory.Social
					&& !x.IsForbidden(pawn) && area.Contains(x.Position) && pawn.CanReserve(x));
		}
	}
}
