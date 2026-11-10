using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimFeast.Jobs
{
	// l'acclamation: chacun saute a son tour et la vague fait deux fois le tour du marqueur. seul le dessin bouge.
	// tous recoivent la tache au meme tick, c'est ce qui les accorde
	public class JobDriver_Cheer : JobDriver
	{
		private const TargetIndex SpotInd = TargetIndex.A;
		private const int LapTicks = 90;
		private const int JumpTicks = 26;
		private const int Laps = 2;
		private const float Height = 0.32f;

		public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

		// le retard suit l'angle autour du marqueur
		private int Delay
		{
			get
			{
				IntVec3 spot = job.GetTarget(SpotInd).Cell;
				float a = Mathf.Atan2(pawn.Position.z - spot.z, pawn.Position.x - spot.x) + Mathf.PI;
				return Mathf.RoundToInt(a / (2f * Mathf.PI) * LapTicks);
			}
		}

		public override Vector3 ForcedBodyOffset
		{
			get
			{
				int t = Find.TickManager.TicksGame - startTick - Delay;
				if (t < 0 || t / LapTicks >= Laps) return Vector3.zero;
				int into = t % LapTicks;
				if (into >= JumpTicks) return Vector3.zero;
				return new Vector3(0f, 0f, Mathf.Sin(into / (float)JumpTicks * Mathf.PI) * Height);
			}
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Toil cheer = ToilMaker.MakeToil("Cheer");
			cheer.initAction = delegate
			{
				pawn.pather.StopDead();
				IntVec3 spot = job.GetTarget(SpotInd).Cell;
				if (spot != pawn.Position) pawn.rotationTracker.FaceCell(spot);
			};
			cheer.handlingFacing = true;
			cheer.defaultCompleteMode = ToilCompleteMode.Delay;
			cheer.defaultDuration = Laps * LapTicks + JumpTicks + 10;
			yield return cheer;
		}
	}
}
