using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.Jobs
{
	// la version banquet de l'execution: on rattrape le convive et on tranche au contact.
	// tout est garde contre le job nettoye en cours de route: la cible peut mourir
	// ou sortir de la carte pendant qu'on marche vers elle
	public class JobDriver_AssassinateGuest : JobDriver
	{
		private Pawn Victim => job?.GetTarget(TargetIndex.A).Thing as Pawn;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
		}

		// la premiere gorge est le signal: on laisse la chanson monter avant de trancher.
		// les suivantes sont une chasse, pas une mise en scene
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
				// la cible a repris de la distance entre deux toils: on la rechasse
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
