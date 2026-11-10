using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimFeast.Jobs
{
	// les deux partenaires tournent autour du milieu de leurs deux cases, face a face. seul le dessin bouge, comme JobDriver_Dance
	public class JobDriver_DanceAtFeast : JobDriver
	{
		private const TargetIndex PartnerInd = TargetIndex.A;
		private const TargetIndex MineInd = TargetIndex.B;
		private const TargetIndex TheirsInd = TargetIndex.C;
		private const int WaitTicks = 900;
		private const float TurnTicks = 300f;
		private const float Closeness = 0.65f;

		private int danceStart = -1;
		private float seedY = float.NaN;

		private Pawn Partner => job.GetTarget(PartnerInd).Pawn;
		private IntVec3 Mine => job.GetTarget(MineInd).Cell;
		private IntVec3 Theirs => job.GetTarget(TheirsInd).Cell;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref danceStart, "danceStart", -1);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed) =>
			pawn.Reserve(Mine, job, 1, -1, null, errorOnFailed);

		private bool Together()
		{
			Pawn p = Partner;
			return p != null && p.Spawned && !p.Downed && p.Position == Theirs && pawn.Position == Mine
				&& p.CurJob != null && p.CurJob.def == job.def && p.CurJob.GetTarget(PartnerInd).Pawn == pawn;
		}

		public override Vector3 ForcedBodyOffset
		{
			get
			{
				if (danceStart < 0 || !Together())
				{
					DrawOrder.Restore(pawn, ref seedY);
					return Vector3.zero;
				}
				Vector3 a = Mine.ToVector3Shifted();
				Vector3 center = (a + Theirs.ToVector3Shifted()) * 0.5f;
				float r = (a - center).MagnitudeHorizontal() * Closeness;
				int now = Find.TickManager.TicksGame;
				// le tour suit l'horloge du jeu: les deux partenaires restent a l'oppose sans se parler
				float theta = Mathf.Atan2(a.z - center.z, a.x - center.x) + now / TurnTicks * 2f * Mathf.PI;
				Vector3 orbit = center + new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta)) * r;
				Vector3 pos = Vector3.Lerp(a, orbit, Mathf.Clamp01((now - danceStart) / 60f));
				DrawOrder.Set(pawn, ref seedY, pos.z < center.z ? Altitudes.AltInc : -Altitudes.AltInc);
				float bob = Mathf.Abs(Mathf.Sin(now / 12f)) * 0.05f;
				return new Vector3(pos.x - a.x, 0f, pos.z - a.z + bob);
			}
		}

		private void FacePartner()
		{
			Vector3 a = Mine.ToVector3Shifted();
			Vector3 center = (a + Theirs.ToVector3Shifted()) * 0.5f;
			Vector3 d = center - (a + ForcedBodyOffset);
			if (d.x * d.x + d.z * d.z < 0.0001f) d = Theirs.ToVector3Shifted() - a;
			pawn.Rotation = Mathf.Abs(d.x) > Mathf.Abs(d.z)
				? (d.x > 0f ? Rot4.East : Rot4.West)
				: (d.z > 0f ? Rot4.North : Rot4.South);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => Partner == null || Partner.Dead || Partner.Downed || !Partner.Spawned);
			yield return Toils_Goto.GotoCell(MineInd, PathEndMode.OnCell);

			Toil wait = ToilMaker.MakeToil("WaitForDancePartner");
			wait.initAction = delegate { pawn.rotationTracker.FaceCell(Theirs); };
			wait.tickIntervalAction = delegate(int delta)
			{
				if (Together()) ReadyForNextToil();
			};
			wait.defaultCompleteMode = ToilCompleteMode.Delay;
			wait.defaultDuration = WaitTicks;
			yield return wait;

			Toil dance = ToilMaker.MakeToil("DanceAtFeast");
			dance.initAction = delegate
			{
				if (!Together()) EndJobWith(JobCondition.Incompletable);
				else danceStart = Find.TickManager.TicksGame;
			};
			dance.tickIntervalAction = delegate(int delta)
			{
				if (!Together())
				{
					EndJobWith(JobCondition.Succeeded);
					return;
				}
				FacePartner();
				pawn.needs?.joy?.GainJoy(delta * 0.6f / 2500f, JoyKindDefOf.Social);
			};
			dance.AddFinishAction(delegate
			{
				DrawOrder.Restore(pawn, ref seedY);
				Pawn p = Partner;
				if (p == null || danceStart < 0 || Find.TickManager.TicksGame - danceStart < 600) return;
				pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RimFeastDefOf.RimFeast_DancedWith, p);
				// les deux partenaires passent ici: un seul ecrit la ligne
				if (pawn.thingIDNumber < p.thingIDNumber) GameComponent_FeastState.Get()?.Notify_Danced(pawn, p);
			});
			dance.handlingFacing = true;
			dance.socialMode = RandomSocialMode.SuperActive;
			dance.defaultCompleteMode = ToilCompleteMode.Delay;
			dance.defaultDuration = Rand.Range(1200, 2400);
			yield return dance;
		}
	}
}
