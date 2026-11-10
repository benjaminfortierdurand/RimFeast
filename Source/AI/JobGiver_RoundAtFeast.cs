using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	// un convive lance la ronde et entraine ses voisins libres autour de quatre cases degagees
	public class JobGiver_RoundAtFeast : ThinkNode_JobGiver
	{
		private const int MinRound = 4;
		private const int MaxRound = 6;

		protected override Job TryGiveJob(Pawn pawn) => TryRound(pawn, false, out _);

		public static Job TryRound(Pawn pawn, bool force, out string why)
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
			if (feasters.Any(p => p.CurJobDef == RimFeastDefOf.RimFeast_RoundJob)) { why = "a circle dance is already going"; return null; }
			List<Pawn> ring = feasters
				.Where(p => FeastUtility.CanDance(p) && JobGiver_DanceAtFeast.Free(p))
				.OrderBy(p => p.Position.DistanceToSquared(pawn.Position))
				.Take(MaxRound - 1).ToList();
			if (ring.Count < MinRound - 1) { why = "not enough free people around"; return null; }
			if (!FindFloor(pawn, ring, spot, area, out IntVec3 floor)) { why = "no four free cells in a square"; return null; }

			// la place de chacun dans le cercle est son rang dans cette liste
			var members = new List<LocalTargetInfo> { pawn };
			foreach (Pawn p in ring)
			{
				Job follow = JobMaker.MakeJob(RimFeastDefOf.RimFeast_RoundJob, pawn);
				follow.count = members.Count;
				follow.locomotionUrgency = LocomotionUrgency.Jog;
				follow.expiryInterval = 4000;
				if (p.jobs.TryTakeOrderedJob(follow, JobTag.Misc)) members.Add(p);
			}
			if (members.Count < MinRound - 1) { why = "too few took the job"; return null; }
			Messages.Message("RimFeast_RoundStarts".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NeutralEvent,
				historical: false);
			GameComponent_FeastState.Get()?.Notify_Round(pawn, members.Count);

			// le carre va en cible B: une cible A tracerait un trait vers lui quand on selectionne le meneur
			Job lead = JobMaker.MakeJob(RimFeastDefOf.RimFeast_RoundJob);
			lead.targetB = floor;
			lead.targetQueueB = members;
			lead.locomotionUrgency = LocomotionUrgency.Jog;
			lead.expiryInterval = 4000;
			return lead;
		}

		// quatre cases libres en carre dans la zone, la case rendue est celle du sud-ouest.
		// entre plusieurs carres on prend celui qui a le plus de place autour, et le plus proche du marqueur a egalite
		private static bool FindFloor(Pawn pawn, List<Pawn> ring, IntVec3 spot, FeastUtility.FeastArea area, out IntVec3 floor)
		{
			Map map = pawn.Map;
			floor = IntVec3.Invalid;
			int best = -1;
			int n = GenRadial.NumCellsInRadius(area.Reach());
			for (int k = 0; k < n; k++)
			{
				IntVec3 c = spot + GenRadial.RadialPattern[k];
				int room = Room(c, map, area, pawn, ring);
				if (room <= best || !pawn.CanReach(c, PathEndMode.OnCell, Danger.None)) continue;
				best = room;
				floor = c;
			}
			return floor.IsValid;
		}

		// -1 si le carre n'est pas libre, sinon le nombre de cases degagees parmi les douze qui l'entourent
		private static int Room(IntVec3 c, Map map, FeastUtility.FeastArea area, Pawn pawn, List<Pawn> ring)
		{
			int room = 0;
			for (int dx = -1; dx <= 2; dx++)
				for (int dz = -1; dz <= 2; dz++)
				{
					var d = new IntVec3(c.x + dx, 0, c.z + dz);
					bool clear = d.InBounds(map) && d.Standable(map) && d.GetEdifice(map) == null;
					if (dx < 0 || dx > 1 || dz < 0 || dz > 1)
					{
						if (clear) room++;
					}
					else if (!clear || !area.Contains(d) || d.IsForbidden(pawn) || Taken(d, map, pawn, ring)) return -1;
				}
			return room;
		}

		private static bool Taken(IntVec3 c, Map map, Pawn pawn, List<Pawn> ring)
		{
			List<Thing> things = c.GetThingList(map);
			for (int i = 0; i < things.Count; i++)
				if (things[i] is Pawn p && p != pawn && !ring.Contains(p)) return true;
			return false;
		}
	}
}
