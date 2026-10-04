using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.Jobs
{
	// SocialRelax vanilla est trop patche par d'autres mods (CommonSense plante dessus)
	public class JobDriver_SitAtFeast : JobDriver
	{
		private const TargetIndex FocusInd = TargetIndex.A;
		private const TargetIndex SeatInd = TargetIndex.B;
		private const TargetIndex DrinkInd = TargetIndex.C;

		private bool HasDrink => job.GetTarget(DrinkInd).HasThing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.ReserveSittableOrSpot(job.GetTarget(SeatInd).Cell, job, errorOnFailed)) return false;
			if (HasDrink && !pawn.Reserve(job.GetTarget(DrinkInd), job, 1, -1, null, errorOnFailed)) return false;
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(FocusInd);

			if (HasDrink)
			{
				this.FailOnDestroyedNullOrForbidden(DrinkInd);
				yield return Toils_Goto.GotoThing(DrinkInd, PathEndMode.OnCell)
					.FailOnSomeonePhysicallyInteracting(DrinkInd);
				yield return Toils_Haul.StartCarryThing(DrinkInd);
			}

			yield return Toils_Goto.GotoCell(SeatInd, PathEndMode.OnCell);

			Toil sit = ToilMaker.MakeToil("SitAtFeast");
			sit.tickIntervalAction = delegate(int delta)
			{
				pawn.rotationTracker.FaceCell(job.GetTarget(FocusInd).Cell);
				pawn.GainComfortFromCellIfPossible(delta);
				// les invites n'ont pas tous une jauge de loisir
				pawn.needs?.joy?.GainJoy(delta * 0.36f / 2500f, JoyKindDefOf.Social);
			};
			sit.handlingFacing = true;
			sit.socialMode = RandomSocialMode.SuperActive;
			sit.defaultCompleteMode = ToilCompleteMode.Delay;
			sit.defaultDuration = Rand.Range(2000, 4000);
			if (HasDrink) Toils_Ingest.AddIngestionEffects(sit, pawn, DrinkInd, TargetIndex.None);
			yield return sit;

			if (HasDrink) yield return Toils_Ingest.FinalizeIngest(pawn, DrinkInd);
		}
	}
}
