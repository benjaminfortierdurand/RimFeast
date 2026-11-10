using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	// un convive appelle la contredanse: les voisins libres se rangent deux par deux, face a face
	public class JobGiver_SetDanceAtFeast : ThinkNode_JobGiver
	{
		private const int MinDancers = 4;
		private const int MaxDancers = 8;

		protected override Job TryGiveJob(Pawn pawn) => TrySetDance(pawn, false, out _);

		public static Job TrySetDance(Pawn pawn, bool force, out string why)
		{
			why = null;
			PawnDuty duty = pawn.mindState.duty;
			Map map = pawn.Map;
			if (duty == null || map == null || !FeastUtility.IsFeaster(pawn)) { why = "not at a feast"; return null; }
			if (!FeastUtility.CanDance(pawn)) { why = "cant dance right now"; return null; }
			IntVec3 spot = duty.focus.Cell;
			if (!force && !FeastUtility.HallIsLively(spot, map)) { why = "no music yet"; return null; }
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(spot, map);

			List<Pawn> feasters = map.mapPawns.AllPawnsSpawned
				.Where(p => p != pawn && FeastUtility.IsFeaster(p) && area.Contains(p.Position)).ToList();
			if (feasters.Any(p => p.CurJobDef == RimFeastDefOf.RimFeast_SetDanceJob)) { why = "a country dance is already going"; return null; }
			List<Pawn> others = feasters
				.Where(p => FeastUtility.CanDance(p) && JobGiver_DanceAtFeast.Free(p))
				.OrderBy(p => p.Position.DistanceToSquared(pawn.Position))
				.Take(MaxDancers - 1).ToList();
			// toujours un nombre pair, le meneur compris
			if (others.Count % 2 == 0 && others.Count > 0) others.RemoveAt(others.Count - 1);
			if (others.Count + 1 < MinDancers) { why = "not enough free people around"; return null; }

			// la bande la plus longue qui tient, quitte a laisser des couples de cote
			int pairs = (others.Count + 1) / 2;
			IntVec3 floor = IntVec3.Invalid;
			bool north = true;
			for (; pairs >= MinDancers / 2; pairs--)
				if (FindFloor(pawn, others, spot, area, pairs, out floor, out north)) break;
			if (!floor.IsValid) { why = "no free strip two cells wide"; return null; }
			if (others.Count > pairs * 2 - 1) others.RemoveRange(pairs * 2 - 1, others.Count - (pairs * 2 - 1));

			// la place de chacun est son rang dans cette liste: les rangs pairs d'un cote, les impairs en face
			List<Pawn> order = Pairs(pawn, others);
			var members = new List<LocalTargetInfo> { pawn };
			for (int i = 1; i < order.Count; i++)
			{
				Job follow = JobMaker.MakeJob(RimFeastDefOf.RimFeast_SetDanceJob, pawn);
				follow.count = members.Count;
				follow.locomotionUrgency = LocomotionUrgency.Jog;
				follow.expiryInterval = 5000;
				if (order[i].jobs.TryTakeOrderedJob(follow, JobTag.Misc)) members.Add(order[i]);
			}
			if (members.Count < MinDancers) { why = "too few took the job"; return null; }
			Messages.Message("RimFeast_SetDanceStarts".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NeutralEvent,
				historical: false);
			GameComponent_FeastState.Get()?.Notify_SetDance(pawn, members.Count);

			// la bande va en cible B, et count dit dans quel sens elle court
			Job lead = JobMaker.MakeJob(RimFeastDefOf.RimFeast_SetDanceJob);
			lead.targetB = floor;
			lead.targetQueueB = members;
			lead.count = north ? 0 : 1;
			lead.locomotionUrgency = LocomotionUrgency.Jog;
			lead.expiryInterval = 5000;
			return lead;
		}

		// un homme et une femme face a face quand c'est possible, comme dans une vraie contredanse
		private static List<Pawn> Pairs(Pawn starter, List<Pawn> others)
		{
			var order = new List<Pawn>();
			var pool = new List<Pawn>(others);
			Pawn cur = starter;
			while (cur != null)
			{
				order.Add(cur);
				Pawn mate = pool.FirstOrDefault(p => p.gender != cur.gender) ?? pool.FirstOrDefault();
				if (mate == null) break;
				pool.Remove(mate);
				order.Add(mate);
				cur = pool.FirstOrDefault();
				if (cur != null) pool.Remove(cur);
			}
			return order;
		}

		// une bande libre de deux cases de large et d'une case de long par couple. la case rendue est celle du sud-ouest
		private static bool FindFloor(Pawn pawn, List<Pawn> others, IntVec3 spot, FeastUtility.FeastArea area, int pairs,
			out IntVec3 floor, out bool north)
		{
			Map map = pawn.Map;
			int n = GenRadial.NumCellsInRadius(area.Reach());
			for (int k = 0; k < n; k++)
			{
				IntVec3 c = spot + GenRadial.RadialPattern[k];
				for (int turn = 0; turn < 2; turn++)
				{
					if (!Strip(c, turn == 0, pairs, map, area, pawn, others)) continue;
					if (!pawn.CanReach(c, PathEndMode.OnCell, Danger.None)) continue;
					floor = c;
					north = turn == 0;
					return true;
				}
			}
			floor = IntVec3.Invalid;
			north = true;
			return false;
		}

		private static bool Strip(IntVec3 c, bool north, int pairs, Map map, FeastUtility.FeastArea area, Pawn pawn,
			List<Pawn> others)
		{
			for (int i = 0; i < pairs; i++)
				for (int side = 0; side < 2; side++)
				{
					IntVec3 d = north ? new IntVec3(c.x + side, 0, c.z + i) : new IntVec3(c.x + i, 0, c.z + side);
					if (!d.InBounds(map) || !area.Contains(d) || !d.Standable(map) || d.GetEdifice(map) != null) return false;
					if (d.IsForbidden(pawn) || JobGiver_RoundAtFeast.Taken(d, map, pawn, others)) return false;
				}
			return true;
		}
	}
}
