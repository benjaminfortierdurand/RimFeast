using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	public class ThinkNode_ConditionalInFeastArea : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			PawnDuty duty = pawn.mindState.duty;
			if (duty == null || pawn.Map == null) return false;
			return FeastUtility.FeastArea.For(duty.focus.Cell, pawn.Map).Contains(pawn.Position);
		}
	}

	public class JobGiver_EatAtFeast : ThinkNode_JobGiver
	{
		protected virtual float MaxFood => 0.9f;

		protected override Job TryGiveJob(Pawn pawn)
		{
			PawnDuty duty = pawn.mindState.duty;
			if (duty == null || pawn.needs?.food == null) return null;
			if (pawn.needs.food.CurLevelPercentage > MaxFood) return null;

			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(duty.focus.Cell, pawn.Map);
			Predicate<Thing> validator = x =>
				x.IngestibleNow && x.def.IsNutritionGivingIngestible && !x.def.IsDrug
				&& (int)x.def.ingestible.preferability > (int)FoodPreferability.RawBad
				&& area.Contains(x.Position)
				&& pawn.WillEat(x) && !x.IsForbidden(pawn) && x.IsSociallyProper(pawn)
				&& pawn.CanReserve(x);
			Thing food = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
				ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree), PathEndMode.ClosestTouch,
				TraverseParms.For(TraverseMode.NoPassClosedDoors), 14f, validator, null, 0, 12);
			if (food == null) return null;

			if (FeastSeats.Carriable(food, food.def) && FeastSeats.CanEatAtTable(pawn)
				&& FeastSeats.TryGetSeat(pawn, out IntVec3 seat, area))
				return FeastSeats.EatJob(pawn, food, food.def, seat);

			Job job = JobMaker.MakeJob(JobDefOf.Ingest, food);
			job.count = FoodUtility.WillIngestStackCountOf(pawn, food.def, FoodUtility.NutritionForEater(pawn, food));
			return job;
		}
	}

	// sans tirage au sort: on est a un banquet, on mange d'abord
	public class JobGiver_EatAtFeastHungry : JobGiver_EatAtFeast
	{
		protected override float MaxFood => 0.7f;
	}

	// les loisirs vanilla boivent pour le plaisir sans regarder la limite du banquet
	public class JobGiver_GetJoyInFeastArea : JobGiver_GetJoyInGatheringArea
	{
		protected override Job TryGiveJobFromJoyGiverDefDirect(JoyGiverDef def, Pawn pawn)
		{
			Job job = base.TryGiveJobFromJoyGiverDefDirect(def, pawn);
			if (job == null) return null;
			if (TooMuch(pawn, job.targetA) || TooMuch(pawn, job.targetC)) return null;
			return job;
		}

		private static bool TooMuch(Pawn pawn, LocalTargetInfo t) =>
			t.HasThing && t.Thing.def.IsDrug && !FeastUtility.CanDrink(pawn, t.Thing);
	}

	public class JobGiver_WanderInFeastArea : JobGiver_Wander
	{
		protected override IntVec3 GetExactWanderDest(Pawn pawn)
		{
			PawnDuty duty = pawn.mindState.duty;
			if (duty == null || pawn.Map == null) return IntVec3.Invalid;
			IntVec3 spot = duty.focus.Cell;
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(spot, pawn.Map);
			Predicate<IntVec3> validator = x =>
				area.Contains(x) && x.Standable(pawn.Map) && !x.IsForbidden(pawn)
				&& pawn.CanReserveAndReach(x, PathEndMode.OnCell, Danger.None);
			return CellFinder.TryFindRandomReachableNearbyCell(spot, pawn.Map, area.Reach(),
				TraverseParms.For(TraverseMode.NoPassClosedDoors), validator, null, out IntVec3 result, 40)
				? result : IntVec3.Invalid;
		}

		protected override IntVec3 GetWanderRoot(Pawn pawn) =>
			pawn.mindState.duty?.focus.Cell ?? pawn.Position;
	}
}
