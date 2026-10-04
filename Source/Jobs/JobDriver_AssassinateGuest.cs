using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.Jobs
{
	public class JobDriver_AssassinateGuest : JobDriver
	{
		private Pawn Victim => job?.GetTarget(TargetIndex.A).Thing as Pawn;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
		}

		private const int SignalPauseTicks = 300;

		private static bool OpeningKill =>
			GameComponent_FeastState.Get()?.CurrentCase?.state == FeastCase.Feasting;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOn(() => Victim == null || Victim.Dead);

			Toil chase = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return chase;

			if (OpeningKill)
			{
				Toil steady = ToilMaker.MakeToil("Steady");
				steady.tickAction = delegate
				{
					if (Victim != null) pawn.rotationTracker.FaceTarget(Victim);
				};
				steady.handlingFacing = true;
				steady.defaultCompleteMode = ToilCompleteMode.Delay;
				steady.defaultDuration = SignalPauseTicks;
				yield return steady;
			}

			Toil cut = ToilMaker.MakeToil("Cut");
			cut.initAction = delegate
			{
				Pawn v = Victim;
				if (v == null || v.Dead || !v.Spawned)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				if (!pawn.Position.AdjacentTo8WayOrInside(v))
				{
					JumpToToil(chase);
					return;
				}
				ExecutionUtility.DoExecutionByCut(pawn, v);
				ThoughtUtility.GiveThoughtsForPawnExecuted(v, pawn, PawnExecutionKind.GenericBrutal);
				TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, pawn, v);
			};
			cut.defaultCompleteMode = ToilCompleteMode.Instant;
			cut.activeSkill = () => SkillDefOf.Melee;
			yield return cut;
		}
	}
}
