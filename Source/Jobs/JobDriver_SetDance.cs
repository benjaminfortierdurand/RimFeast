using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimFeast.Jobs
{
	// la contredanse: deux rangees face a face sur une bande de deux cases de large. meme principe que la ronde:
	// le dessin danse et la case du pion suit en dessous
	public class JobDriver_SetDance : JobDriver
	{
		private const TargetIndex HeadInd = TargetIndex.A;
		private const TargetIndex FloorInd = TargetIndex.B;
		private const int MinDancers = 4;
		private const int GatherTicks = 500;
		private const int JoinTicks = 45;
		private const int SettleTicks = 40;
		private const int Figure = 240;
		private const int Figures = 5;
		private const float JoinDist = 1.2f;
		private const float Step = 0.22f;
		private const float Pass = 0.3f;
		private const float DepthPerCell = 0.15f;
		private const float MaxDepth = 0.3f;
		private const float RankDepth = 0.003f;

		private int goTick = -1;
		private int endTick = -1;
		private int joinTick = -1;
		private int rounds = 1;
		private float seedY = float.NaN;

		private bool Leader => !job.GetTarget(HeadInd).HasThing;
		private int Place => Leader ? 0 : job.count;
		private int Size => job.targetQueueB?.Count ?? 0;

		private JobDriver_SetDance Head
		{
			get
			{
				if (Leader) return this;
				Pawn h = job.GetTarget(HeadInd).Pawn;
				if (h == null || !h.Spawned || h.CurJobDef != job.def) return null;
				return h.jobs.curDriver is JobDriver_SetDance d && d.Leader ? d : null;
			}
		}

		// le meneur garde dans count le sens de la bande: 0 elle court vers le nord, 1 vers l'est
		private Vector3 Along => job.count == 0 ? new Vector3(0f, 0f, 1f) : new Vector3(1f, 0f, 0f);

		private Vector3 Across => job.count == 0 ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f);

		// les places vont par deux: 0 et 1 se font face, puis 2 et 3, et ainsi de suite le long de la bande
		private Vector3 Home(int place) =>
			job.GetTarget(FloorInd).Cell.ToVector3Shifted() + Along * (place / 2) + Across * (place % 2);

		private float MidZ => job.GetTarget(FloorInd).Cell.ToVector3Shifted().z + Along.z * ((Size / 2 - 1) * 0.5f)
			+ Across.z * 0.5f;

		private int Elapsed
		{
			get
			{
				if (goTick < 0) return 0;
				return (endTick >= 0 ? endTick : Find.TickManager.TicksGame) - goTick;
			}
		}

		private static float Ease(float t) => t * t * (3f - 2f * t);

		// cinq figures de quatre secondes: s'avancer deux fois, changer de cote en se croisant par la droite,
		// s'avancer encore, revenir, puis tourner a deux. la derniere ramene chacun chez soi
		private Vector3 Spot(int place)
		{
			Vector3 home = Home(place);
			Vector3 far = Home(place ^ 1);
			Vector3 d = far - home;
			Vector3 right = new Vector3(d.z, 0f, -d.x);
			int e = Elapsed;
			float t = e % Figure / (float)Figure;
			switch (e / Figure % Figures)
			{
				case 0:
					return home + d * (Step * (1f - Mathf.Cos(t * 4f * Mathf.PI)) * 0.5f);
				case 1:
					return home + d * Ease(t) + right * (Pass * Mathf.Sin(Ease(t) * Mathf.PI));
				case 2:
					return far - d * (Step * (1f - Mathf.Cos(t * 4f * Mathf.PI)) * 0.5f);
				case 3:
					return far - d * Ease(t) - right * (Pass * Mathf.Sin(Ease(t) * Mathf.PI));
				default:
					Vector3 mid = (home + far) * 0.5f;
					float a = Mathf.Atan2(home.z - mid.z, home.x - mid.x) + Ease(t) * 2f * Mathf.PI;
					float r = 0.5f - 0.24f * Mathf.Sin(t * Mathf.PI);
					return mid + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref goTick, "goTick", -1);
			Scribe_Values.Look(ref endTick, "endTick", -1);
			Scribe_Values.Look(ref joinTick, "joinTick", -1);
			Scribe_Values.Look(ref rounds, "rounds", 1);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

		public override Vector3 ForcedBodyOffset
		{
			get
			{
				JobDriver_SetDance head = Head;
				if (head == null || joinTick < 0)
				{
					DrawOrder.Restore(pawn, ref seedY);
					return Vector3.zero;
				}
				int now = Find.TickManager.TicksGame;
				Vector3 on = head.Spot(Place);
				// celui du sud passe devant, et le rang departage deux danseurs a la meme hauteur
				float depth = (head.MidZ - on.z) * DepthPerCell;
				DrawOrder.Set(pawn, ref seedY, Mathf.Clamp(depth, -MaxDepth, MaxDepth) + Place * RankDepth);
				on.z += Mathf.Abs(Mathf.Sin(now / 10f)) * 0.04f;
				// on entre dans la danse et on en sort en fondu, sinon le pion saute d'un coup
				float blend = Mathf.SmoothStep(0f, 1f, (now - joinTick) / (float)JoinTicks);
				if (head.endTick >= 0) blend *= 1f - Mathf.SmoothStep(0f, 1f, (now - head.endTick) / (float)SettleTicks);
				Vector3 off = (on - pawn.Drawer.tweener.TweenedPos) * blend;
				off.y = 0f;
				return off;
			}
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			AddFinishAction(delegate { DrawOrder.Restore(pawn, ref seedY); });
			// les suiveurs recoivent leur tache avant le meneur: sans ce delai ils lachent aussitot
			if (!Leader)
				this.FailOn(delegate
				{
					Pawn h = job.GetTarget(HeadInd).Pawn;
					return h == null || h.Dead || h.Downed || !h.Spawned
						|| (Head == null && Find.TickManager.TicksGame - startTick > 120);
				});
			Toil dance = ToilMaker.MakeToil("SetDance");
			// tant qu'on marche vers sa place, le jeu tourne le pion tout seul. une fois dans la danse, c'est nous
			dance.handlingFacing = joinTick >= 0;
			dance.tickAction = delegate
			{
				if (Leader) Lead();
				else Follow();
				dance.handlingFacing = joinTick >= 0;
			};
			dance.tickIntervalAction = Joy;
			dance.defaultCompleteMode = ToilCompleteMode.Never;
			dance.socialMode = RandomSocialMode.SuperActive;
			yield return dance;
		}

		private void Joy(int delta) => pawn.needs?.joy?.GainJoy(delta * 0.6f / 2500f, JoyKindDefOf.Social);

		private void Lead()
		{
			int now = Find.TickManager.TicksGame;
			bool late = now - startTick > GatherTicks;
			if (joinTick < 0)
			{
				if (late) EndJobWith(JobCondition.Incompletable);
				else Approach(this);
				return;
			}
			Glide(this);
			if (endTick >= 0)
			{
				if (now - endTick >= SettleTicks) ReadyForNextToil();
				return;
			}
			if (goTick < 0)
			{
				// on attend que tout le monde soit en place, mais pas indefiniment
				Count(out int there, out int coming);
				if (coming > 0 && !late) return;
				if (there < MinDancers) endTick = now;
				else
				{
					goTick = now;
					rounds = Rand.RangeInclusive(1, 2);
				}
				return;
			}
			if (now - goTick >= rounds * Figures * Figure) endTick = now;
			else if (pawn.IsHashIntervalTick(60))
			{
				Count(out int there, out _);
				if (there < 2) endTick = now;
			}
		}

		private void Count(out int there, out int coming)
		{
			there = coming = 0;
			foreach (LocalTargetInfo t in job.targetQueueB)
			{
				Pawn p = t.Pawn;
				JobDriver_SetDance d = p == pawn ? this : p?.jobs?.curDriver as JobDriver_SetDance;
				if (d == null || !p.Spawned || d.Head != this) continue;
				if (d.joinTick >= 0) there++;
				else coming++;
			}
		}

		private void Follow()
		{
			JobDriver_SetDance head = Head;
			if (head == null) return;
			if (head.endTick >= 0 && (joinTick < 0 || Find.TickManager.TicksGame - head.endTick >= SettleTicks))
			{
				EndJobWith(JobCondition.Succeeded);
				return;
			}
			if (joinTick >= 0) Glide(head);
			else Approach(head);
		}

		// on marche vers sa place, et on s'y glisse une fois tout pres
		private void Approach(JobDriver_SetDance head)
		{
			Vector3 on = head.Spot(Place);
			IntVec3 c = on.ToIntVec3();
			if ((on - pawn.Position.ToVector3Shifted()).MagnitudeHorizontal() <= JoinDist)
			{
				joinTick = Find.TickManager.TicksGame;
				pawn.pather.StopDead();
				Glide(head);
				return;
			}
			if (!pawn.pather.Moving || (pawn.IsHashIntervalTick(15) && pawn.pather.Destination.Cell != c))
				pawn.pather.StartPath(c, PathEndMode.OnCell);
		}

		// la case du pion suit le dessin, sans passer par le pather, et il regarde son vis-a-vis
		private void Glide(JobDriver_SetDance head)
		{
			Vector3 on = head.Spot(Place);
			IntVec3 c = on.ToIntVec3();
			if (pawn.Position != c && c.InBounds(pawn.Map) && c.Walkable(pawn.Map))
			{
				pawn.Position = c;
				pawn.Notify_Teleported(false, false);
			}
			Vector3 v = head.Spot(Place ^ 1) - on;
			if (v.x * v.x + v.z * v.z < 0.0004f) return;
			pawn.Rotation = Mathf.Abs(v.x) > Mathf.Abs(v.z)
				? (v.x > 0f ? Rot4.East : Rot4.West)
				: (v.z > 0f ? Rot4.North : Rot4.South);
		}
	}
}
