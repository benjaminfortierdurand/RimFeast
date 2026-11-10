using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimFeast.Jobs
{
	// la ronde: quelques convives tournent autour du coin commun de quatre cases libres, face au centre,
	// un tour dans un sens puis un tour dans l'autre. comme pour la chenille, seul le dessin tourne et la case suit en dessous
	public class JobDriver_Round : JobDriver
	{
		private const TargetIndex HeadInd = TargetIndex.A;
		private const TargetIndex FloorInd = TargetIndex.B;
		private const int MinRound = 3;
		private const int GatherTicks = 500;
		private const int JoinTicks = 45;
		private const int SettleTicks = 40;
		private const float TurnTicks = 360f;
		private const float Ease = 0.95f;
		private const float Spacing = 0.95f;
		private const float MaxRadius = 0.95f;
		private const float JoinDist = 1.2f;
		private const float DepthPerCell = 0.15f;
		private const float MaxDepth = 0.3f;
		private const float RankDepth = 0.003f;

		private int goTick = -1;
		private int endTick = -1;
		private int joinTick = -1;
		private int turns = 4;
		private float seedY = float.NaN;

		private bool Leader => !job.GetTarget(HeadInd).HasThing;
		private int Place => Leader ? 0 : job.count;
		private int Size => job.targetQueueB?.Count ?? 0;

		private JobDriver_Round Head
		{
			get
			{
				if (Leader) return this;
				Pawn h = job.GetTarget(HeadInd).Pawn;
				if (h == null || !h.Spawned || h.CurJobDef != job.def) return null;
				return h.jobs.curDriver is JobDriver_Round d && d.Leader ? d : null;
			}
		}

		// le coin nord-est de la case d'ancrage, donc le milieu du carre de quatre cases
		private Vector3 Center => job.GetTarget(FloorInd).Cell.ToVector3() + new Vector3(1f, 0f, 1f);

		// au-dela de 0.95 le cercle deborde des quatre cases
		private float Radius => Mathf.Min(MaxRadius, Spacing / (2f * Mathf.Sin(Mathf.PI / Mathf.Max(3, Size))));

		// un triangle aux pointes arrondies: la ronde part doucement, fait un tour, ralentit et repart dans l'autre sens.
		// elle s'arrete toujours sur une pointe, chacun revenu a sa place de depart
		private float Phase
		{
			get
			{
				if (goTick < 0) return 0f;
				int until = endTick >= 0 ? endTick : Find.TickManager.TicksGame;
				float x = ((until - goTick) / TurnTicks - 0.5f) * Mathf.PI;
				return Mathf.PI * (1f + Mathf.Asin(Ease * Mathf.Sin(x)) / Mathf.Asin(Ease));
			}
		}

		// le huitieme de tour de depart garde chaque place a l'interieur d'une case: pile sur une limite, la case du pion clignote
		private Vector3 Slot(int place)
		{
			float a = Mathf.PI / 4f + place * 2f * Mathf.PI / Mathf.Max(1, Size) + Phase;
			return Center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Radius;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref goTick, "goTick", -1);
			Scribe_Values.Look(ref endTick, "endTick", -1);
			Scribe_Values.Look(ref joinTick, "joinTick", -1);
			Scribe_Values.Look(ref turns, "turns", 4);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

		public override Vector3 ForcedBodyOffset
		{
			get
			{
				JobDriver_Round head = Head;
				if (head == null || joinTick < 0)
				{
					DrawOrder.Restore(pawn, ref seedY);
					return Vector3.zero;
				}
				int now = Find.TickManager.TicksGame;
				Vector3 on = head.Slot(Place);
				// celui du sud passe devant, et le rang departage deux danseurs a la meme hauteur
				float depth = (head.Center.z - on.z) * DepthPerCell;
				DrawOrder.Set(pawn, ref seedY, Mathf.Clamp(depth, -MaxDepth, MaxDepth) + Place * RankDepth);
				on.z += Mathf.Abs(Mathf.Sin(now / 12f)) * 0.05f;
				// on entre dans le cercle et on en sort en fondu, sinon le pion saute d'un coup
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
			Toil round = ToilMaker.MakeToil("RoundDance");
			// tant qu'on marche vers le cercle, le jeu tourne le pion tout seul. une fois dedans, c'est nous
			round.handlingFacing = joinTick >= 0;
			round.tickAction = delegate
			{
				if (Leader) Lead();
				else Follow();
				round.handlingFacing = joinTick >= 0;
			};
			round.tickIntervalAction = Joy;
			round.defaultCompleteMode = ToilCompleteMode.Never;
			round.socialMode = RandomSocialMode.SuperActive;
			yield return round;
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
				if (there < MinRound) endTick = now;
				else
				{
					goTick = now;
					turns = Rand.RangeInclusive(3, 5);
				}
				return;
			}
			if (now - goTick >= turns * TurnTicks) endTick = now;
			else if (pawn.IsHashIntervalTick(60))
			{
				Count(out int there, out _);
				if (there < MinRound) endTick = now;
			}
		}

		private void Count(out int there, out int coming)
		{
			there = coming = 0;
			foreach (LocalTargetInfo t in job.targetQueueB)
			{
				Pawn p = t.Pawn;
				JobDriver_Round d = p == pawn ? this : p?.jobs?.curDriver as JobDriver_Round;
				if (d == null || !p.Spawned || d.Head != this) continue;
				if (d.joinTick >= 0) there++;
				else coming++;
			}
		}

		private void Follow()
		{
			JobDriver_Round head = Head;
			if (head == null) return;
			if (head.endTick >= 0 && (joinTick < 0 || Find.TickManager.TicksGame - head.endTick >= SettleTicks))
			{
				EndJobWith(JobCondition.Succeeded);
				return;
			}
			if (joinTick >= 0) Glide(head);
			else Approach(head);
		}

		// on marche vers sa place dans le cercle, et on s'y glisse une fois tout pres
		private void Approach(JobDriver_Round head)
		{
			Vector3 on = head.Slot(Place);
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

		// la case du pion suit le dessin, sans passer par le pather, et il regarde le centre
		private void Glide(JobDriver_Round head)
		{
			Vector3 on = head.Slot(Place);
			IntVec3 c = on.ToIntVec3();
			if (pawn.Position != c && c.InBounds(pawn.Map) && c.Walkable(pawn.Map))
			{
				pawn.Position = c;
				pawn.Notify_Teleported(false, false);
			}
			Vector3 d = head.Center - on;
			pawn.Rotation = Mathf.Abs(d.x) > Mathf.Abs(d.z)
				? (d.x > 0f ? Rot4.East : Rot4.West)
				: (d.z > 0f ? Rot4.North : Rot4.South);
		}
	}
}
