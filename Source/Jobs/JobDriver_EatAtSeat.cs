using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimFeast.Jobs
{
	// le flux de JobDriver_Ingest, avec la place du colon au lieu de la chaise libre la plus proche
	public class JobDriver_EatAtSeat : JobDriver, IEatingDriver
	{
		private const TargetIndex FoodInd = TargetIndex.A;
		private const TargetIndex TableInd = TargetIndex.B;
		private const TargetIndex SeatInd = TargetIndex.C;

		private bool fromDispenser;
		private Toil chewing;

		private Thing Food => job.GetTarget(FoodInd).Thing;

		public bool GainingNutritionNow =>
			!Food.DestroyedOrNull() && Food.def.IsNutritionGivingIngestible && CurToil == chewing;

		private float ChewDurationMultiplier =>
			Food.def.ingestible != null && !Food.def.ingestible.useEatingSpeedStat
				? 1f : 1f / pawn.GetStatValue(StatDefOf.EatingSpeed);

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref fromDispenser, "fromDispenser");
		}

		public override void Notify_Starting()
		{
			base.Notify_Starting();
			fromDispenser = Food is Building_NutrientPasteDispenser;
		}

		public override string GetReport()
		{
			if (fromDispenser)
				return JobUtility.GetResolvedJobReportRaw(job.def.reportString, ThingDefOf.MealNutrientPaste.label,
					ThingDefOf.MealNutrientPaste, "", "", "", "");
			Thing t = job.targetA.Thing;
			if (t?.def.ingestible != null && !t.def.ingestible.ingestReportString.NullOrEmpty())
				return t.def.ingestible.ingestReportString.Formatted(t.LabelShort, t);
			return base.GetReport();
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!(Food is Building_NutrientPasteDispenser))
			{
				int count = FoodUtility.GetMaxAmountToPickup(Food, pawn, job.count);
				if (!pawn.Reserve(Food, job, 10, count, null, errorOnFailed)) return false;
				if (count > 0) job.count = count;
			}
			return pawn.ReserveSittableOrSpot(job.GetTarget(SeatInd).Cell, job, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			if (!fromDispenser) this.FailOn(() => !Food.Destroyed && !Food.IngestibleNow);
			chewing = Toils_Ingest.ChewIngestible(pawn, ChewDurationMultiplier, FoodInd, TableInd)
				.FailOn(() => !Food.Spawned && (pawn.carryTracker == null || pawn.carryTracker.CarriedThing != Food))
				.FailOnCannotTouch(FoodInd, PathEndMode.Touch);

			if (fromDispenser)
			{
				yield return Toils_Goto.GotoThing(FoodInd, PathEndMode.InteractionCell).FailOnDespawnedNullOrForbidden(FoodInd);
				yield return Toils_Ingest.TakeMealFromDispenser(FoodInd, pawn);
			}
			else
			{
				yield return Toils_Goto.GotoThing(FoodInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(FoodInd);
				yield return Toils_Ingest.PickupIngestible(FoodInd, pawn);
			}

			Toil toSeat = ToilMaker.MakeToil("CarryToFeastSeat");
			toSeat.initAction = delegate
			{
				IntVec3 seat = job.GetTarget(SeatInd).Cell;
				pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job, seat);
				pawn.pather.StartPath(seat, PathEndMode.OnCell);
			};
			toSeat.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			yield return toSeat.FailOnDestroyedOrNull(FoodInd);

			yield return Toils_Ingest.FindAdjacentEatSurface(TableInd, FoodInd);
			yield return chewing;
			yield return Toils_Ingest.FinalizeIngest(pawn, FoodInd);
		}

		public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool flip) =>
			JobDriver_Ingest.ModifyCarriedThingDrawPosWorker(ref drawPos, ref flip, job.GetTarget(TableInd).Cell, pawn);
	}
}
