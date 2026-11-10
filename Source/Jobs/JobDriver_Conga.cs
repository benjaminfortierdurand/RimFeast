using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimFeast.Jobs
{
	// la chenille: le trajet est fixe au depart et toute la file glisse dessus, chacun a sa place derriere le meneur.
	// seul le dessin suit la courbe, comme pour la danse, et la case du pion saute de l'une a l'autre en dessous.
	// avec un vrai deplacement la file avance par a-coups: le jeu calcule les chemins en differe, un par case et par suiveur
	public class JobDriver_Conga : JobDriver
	{
		private const TargetIndex HeadInd = TargetIndex.A;
		private const int GatherTicks = 150;
		private const int JoinTicks = 45;
		private const int SettleTicks = 40;
		private const float Speed = 0.03f;
		private const float Spacing = 0.75f;
		private const float JoinDist = 2.2f;
		private const float Round = 0.4f;
		private const float MidLine = 2.6f;
		private const float DepthPerCell = 0.08f;
		private const float MaxDepth = 0.3f;
		private const float RankDepth = 0.003f;

		private int goTick = -1;
		private int endTick = -1;
		private int joinTick = -1;
		private float seedY = float.NaN;
		private List<Vector3> pts;
		private List<float> len;

		private bool Leader => !job.GetTarget(HeadInd).HasThing;
		private int Place => Leader ? 0 : job.count;

		private JobDriver_Conga Head
		{
			get
			{
				if (Leader) return this;
				Pawn h = job.GetTarget(HeadInd).Pawn;
				if (h == null || !h.Spawned || h.CurJobDef != job.def) return null;
				return h.jobs.curDriver is JobDriver_Conga d && d.Leader ? d : null;
			}
		}

		private float Length
		{
			get
			{
				Build();
				return len.Count == 0 ? 0f : len[len.Count - 1];
			}
		}

		private float Progress
		{
			get
			{
				if (goTick < 0) return 0f;
				int until = endTick >= 0 ? endTick : Find.TickManager.TicksGame;
				return Mathf.Min((until - goTick) * Speed, Length);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref goTick, "goTick", -1);
			Scribe_Values.Look(ref endTick, "endTick", -1);
			Scribe_Values.Look(ref joinTick, "joinTick", -1);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

		private void Build()
		{
			if (pts != null) return;
			pts = new List<Vector3>();
			len = new List<float>();
			List<LocalTargetInfo> route = job.targetQueueB;
			if (route == null) return;
			float total = 0f;
			for (int i = 0; i < route.Count; i++)
			{
				Vector3 p = route[i].Cell.ToVector3Shifted();
				if (i > 0) total += (p - pts[i - 1]).MagnitudeHorizontal();
				pts.Add(p);
				len.Add(total);
			}
		}

		private int Segment(float s)
		{
			int i = len.BinarySearch(s);
			if (i < 0) i = ~i - 1;
			return Mathf.Clamp(i, 0, len.Count - 2);
		}

		private Vector3 Raw(float s)
		{
			Build();
			if (pts.Count == 0) return pawn.Position.ToVector3Shifted();
			if (pts.Count == 1 || s <= 0f) return pts[0];
			if (s >= Length) return pts[pts.Count - 1];
			int i = Segment(s);
			return Vector3.Lerp(pts[i], pts[i + 1], (s - len[i]) / (len[i + 1] - len[i]));
		}

		// la moyenne de deux points voisins arrondit les angles du trajet
		private Vector3 At(float s) => (Raw(s - Round) + Raw(s + Round)) * 0.5f;

		private IntVec3 CellAt(float s)
		{
			Build();
			if (pts.Count == 0) return pawn.Position;
			if (pts.Count == 1 || s <= 0f) return job.targetQueueB[0].Cell;
			if (s >= Length) return job.targetQueueB[pts.Count - 1].Cell;
			int i = Segment(s);
			return job.targetQueueB[s - len[i] < len[i + 1] - s ? i : i + 1].Cell;
		}

		public override Vector3 ForcedBodyOffset
		{
			get
			{
				JobDriver_Conga head = Head;
				if (head == null || joinTick < 0)
				{
					DrawOrder.Restore(pawn, ref seedY);
					return Vector3.zero;
				}
				int now = Find.TickManager.TicksGame;
				float lead = head.Progress;
				Vector3 on = head.At(Mathf.Max(lead - Place * Spacing, 0f));
				// celui du sud passe devant: une marche de hauteur par case, comptee depuis le milieu de la file.
				// cote a cote, le rang departage, sinon deux pions a la meme hauteur clignotent
				float dz = head.At(Mathf.Max(lead - MidLine, 0f)).z - on.z;
				DrawOrder.Set(pawn, ref seedY, Mathf.Clamp(dz * DepthPerCell, -MaxDepth, MaxDepth) + Place * RankDepth);
				// toute la file se dandine en meme temps
				float beat = now / 9f;
				on += new Vector3(Mathf.Sin(beat) * 0.06f, 0f, Mathf.Abs(Mathf.Cos(beat)) * 0.04f);
				// on entre dans la file et on en sort en fondu, sinon le pion saute d'un coup
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
			if (Leader)
			{
				// le meneur laisse aux autres le temps de venir pres de lui
				Toil gather = ToilMaker.MakeToil("GatherConga");
				gather.initAction = delegate { pawn.pather.StopDead(); };
				gather.defaultCompleteMode = ToilCompleteMode.Delay;
				gather.defaultDuration = GatherTicks;
				yield return gather;

				Toil lead = ToilMaker.MakeToil("LeadConga");
				lead.initAction = delegate
				{
					if (goTick < 0) goTick = joinTick = Find.TickManager.TicksGame;
				};
				lead.tickAction = delegate
				{
					int now = Find.TickManager.TicksGame;
					Glide(this, Progress);
					if (endTick < 0 && Progress >= Length) endTick = now;
					if (endTick >= 0 && now - endTick >= SettleTicks) ReadyForNextToil();
				};
				lead.tickIntervalAction = Joy;
				lead.handlingFacing = true;
				lead.defaultCompleteMode = ToilCompleteMode.Never;
				lead.socialMode = RandomSocialMode.SuperActive;
				yield return lead;
				yield break;
			}

			// les suiveurs recoivent leur tache avant le meneur: sans ce delai ils lachent aussitot
			this.FailOn(delegate
			{
				Pawn h = job.GetTarget(HeadInd).Pawn;
				return h == null || h.Dead || h.Downed || !h.Spawned
					|| (Head == null && Find.TickManager.TicksGame - startTick > 120);
			});
			Toil follow = ToilMaker.MakeToil("FollowInConga");
			// tant qu'il marche vers la file, le jeu tourne le pion tout seul. une fois dedans, c'est nous
			follow.handlingFacing = joinTick >= 0;
			follow.tickAction = delegate
			{
				Follow();
				follow.handlingFacing = joinTick >= 0;
			};
			follow.tickIntervalAction = Joy;
			follow.defaultCompleteMode = ToilCompleteMode.Never;
			follow.socialMode = RandomSocialMode.SuperActive;
			yield return follow;
		}

		private void Joy(int delta) => pawn.needs?.joy?.GainJoy(delta * 0.6f / 2500f, JoyKindDefOf.Social);

		private void Follow()
		{
			JobDriver_Conga head = Head;
			if (head == null) return;
			float s = head.Progress - Place * Spacing;
			if (joinTick >= 0)
			{
				Glide(head, s);
				return;
			}
			// pas encore dans la file: on marche vers l'endroit ou sa place passera, et on s'y glisse une fois tout pres
			IntVec3 c = head.CellAt(s);
			float away = (head.At(Mathf.Max(s, 0f)) - pawn.Position.ToVector3Shifted()).MagnitudeHorizontal();
			if (s >= 0f && away <= JoinDist)
			{
				joinTick = Find.TickManager.TicksGame;
				pawn.pather.StopDead();
				Glide(head, s);
				return;
			}
			if (pawn.Position.AdjacentTo8WayOrInside(c))
			{
				if (pawn.pather.Moving) pawn.pather.StopDead();
				pawn.rotationTracker.FaceCell(c);
			}
			else if (!pawn.pather.Moving || (pawn.IsHashIntervalTick(15) && pawn.pather.Destination.Cell != c))
				pawn.pather.StartPath(c, PathEndMode.Touch);
		}

		// la case du pion suit le dessin, sans passer par le pather
		private void Glide(JobDriver_Conga head, float s)
		{
			IntVec3 c = head.CellAt(s);
			if (pawn.Position != c)
			{
				pawn.Position = c;
				pawn.Notify_Teleported(false, false);
			}
			Vector3 d = head.Raw(s + 0.5f) - head.Raw(s - 0.5f);
			if (d.x * d.x + d.z * d.z < 0.0001f) return;
			// en diagonale on garde le profil, comme le jeu
			pawn.Rotation = Mathf.Abs(d.x) >= Mathf.Abs(d.z) * 0.9f
				? (d.x > 0f ? Rot4.East : Rot4.West)
				: (d.z > 0f ? Rot4.North : Rot4.South);
		}
	}
}
