using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	// un convive emeche lance la chenille et entraine ceux qui ont bu
	public class JobGiver_CongaAtFeast : ThinkNode_JobGiver
	{
		private const int MaxLine = 8;
		private const float LeaderDrunk = 0.25f;
		private const float FollowerDrunk = 0.15f;
		private const int RouteCells = 60;
		private const int ChairCost = 6;
		private const int Fold = 8;

		protected override Job TryGiveJob(Pawn pawn) => TryConga(pawn, false, out _);

		public static Job TryConga(Pawn pawn, bool force, out string why)
		{
			why = null;
			PawnDuty duty = pawn.mindState.duty;
			Map map = pawn.Map;
			if (duty == null || map == null || !FeastUtility.IsFeaster(pawn)) { why = "not at a feast"; return null; }
			if (!FeastUtility.CanDance(pawn)) { why = "cant dance right now"; return null; }
			if (!force && FeastUtility.DrunkennessOf(pawn) < LeaderDrunk) { why = "not tipsy"; return null; }
			IntVec3 spot = duty.focus.Cell;
			if (!force && !FeastUtility.HallIsLively(spot, map)) { why = "no music yet"; return null; }
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(spot, map);

			List<Pawn> feasters = map.mapPawns.AllPawnsSpawned
				.Where(p => p != pawn && FeastUtility.IsFeaster(p) && area.Contains(p.Position)).ToList();
			if (feasters.Any(p => p.CurJobDef == RimFeastDefOf.RimFeast_CongaJob)) { why = "a conga is already going"; return null; }
			List<Pawn> line = feasters
				.Where(p => FeastUtility.CanDance(p) && JobGiver_DanceAtFeast.Free(p)
					&& (force || FeastUtility.DrunkennessOf(p) >= FollowerDrunk))
				.OrderBy(p => p.Position.DistanceToSquared(pawn.Position))
				.Take(MaxLine - 1).ToList();
			if (line.Count < 2) { why = "not enough people who drank around"; return null; }
			List<LocalTargetInfo> route = Route(pawn, spot, area);
			if (route.Count < 12) { why = "no room to go round the hall"; return null; }

			int place = 0;
			foreach (Pawn p in line)
			{
				Job follow = JobMaker.MakeJob(RimFeastDefOf.RimFeast_CongaJob, pawn);
				follow.count = place + 1;
				follow.locomotionUrgency = LocomotionUrgency.Jog;
				follow.expiryInterval = 8000;
				if (p.jobs.TryTakeOrderedJob(follow, JobTag.Misc)) place++;
			}
			if (place == 0) { why = "nobody took the job"; return null; }
			Messages.Message("RimFeast_CongaStarts".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NeutralEvent,
				historical: false);
			GameComponent_FeastState.Get()?.Notify_Conga(pawn, place + 1);

			Job lead = JobMaker.MakeJob(RimFeastDefOf.RimFeast_CongaJob);
			lead.targetQueueB = route;
			lead.expiryInterval = 8000;
			return lead;
		}

		// tout le trajet, case par case, calcule au depart: du meneur vers une boucle de cases a egale distance du marqueur,
		// une par huitieme de tour, puis autant de tours qu'il faut pour une danse d'une bonne demi-minute
		private static List<LocalTargetInfo> Route(Pawn pawn, IntVec3 spot, FeastUtility.FeastArea area)
		{
			Map map = pawn.Map;
			var route = new List<LocalTargetInfo>();
			var cost = new Dictionary<IntVec3, int>();
			foreach (IntVec3 c in area.Cells())
			{
				int k = StepCost(c, map, pawn);
				if (k > 0) cost[c] = k;
			}
			if (!cost.ContainsKey(pawn.Position)) cost[pawn.Position] = 1;
			List<IntVec3> floor = cost.Where(kv => kv.Value == 1 && kv.Key != spot).Select(kv => kv.Key).ToList();
			if (floor.Count < 8) return route;

			float ring = Mathf.Max(2f, floor.Max(c => c.DistanceTo(spot)) * 0.6f);
			var picks = new List<IntVec3>();
			for (int s = 0; s < 8; s++)
			{
				IntVec3 best = IntVec3.Invalid;
				float bestErr = 2.5f;
				foreach (IntVec3 c in floor)
				{
					if (Sector(c, spot) != s) continue;
					float err = Mathf.Abs(c.DistanceTo(spot) - ring);
					if (err < bestErr)
					{
						bestErr = err;
						best = c;
					}
				}
				if (best.IsValid) picks.Add(best);
			}
			if (picks.Count < 3) return route;
			// on part vers l'etape du secteur d'apres, jamais vers une etape deja derriere soi
			int here = Sector(pawn.Position, spot);
			int start = Mathf.Max(0, picks.FindIndex(p => Sector(p, spot) > here));

			var cells = new List<IntVec3> { pawn.Position };
			for (int i = 0; i < picks.Count * 4 && (i < picks.Count || cells.Count < RouteCells); i++)
				Leg(cells[cells.Count - 1], picks[(start + i) % picks.Count], cost, cells);
			foreach (IntVec3 c in cells) route.Add(c);
			return route;
		}

		// sol libre 1, chaise 6: la file contourne les meubles et ne passe sur une chaise que faute d'autre chemin.
		// tables et portes sont exclues
		private static int StepCost(IntVec3 c, Map map, Pawn pawn)
		{
			if (!c.InBounds(map) || !c.Walkable(map) || c.IsForbidden(pawn)) return 0;
			Building b = c.GetEdifice(map);
			if (b == null) return 1;
			return b.def.building != null && b.def.building.isSittable ? ChairCost : 0;
		}

		// plus court chemin sur les cases de la zone, sans couper les coins. une etape injoignable est sautee
		private static void Leg(IntVec3 from, IntVec3 to, Dictionary<IntVec3, int> cost, List<IntVec3> into)
		{
			if (from == to) return;
			var best = new Dictionary<IntVec3, int> { [from] = 0 };
			var prev = new Dictionary<IntVec3, IntVec3>();
			var open = new SortedSet<(int, int, int)> { (0, from.x, from.z) };
			while (open.Count > 0)
			{
				(int d, int x, int z) = open.Min;
				open.Remove(open.Min);
				var c = new IntVec3(x, 0, z);
				if (c == to) break;
				if (d > best[c]) continue;
				for (int i = 0; i < 8; i++)
				{
					IntVec3 n = c + GenAdj.AdjacentCells[i];
					if (!cost.TryGetValue(n, out int k)) continue;
					bool diag = n.x != c.x && n.z != c.z;
					if (diag && (!cost.ContainsKey(new IntVec3(n.x, 0, c.z)) || !cost.ContainsKey(new IntVec3(c.x, 0, n.z))))
						continue;
					int nd = d + k * (diag ? 14 : 10);
					if (best.TryGetValue(n, out int old) && old <= nd) continue;
					best[n] = nd;
					prev[n] = c;
					open.Add((nd, n.x, n.z));
				}
			}
			if (!prev.ContainsKey(to)) return;
			var leg = new List<IntVec3>();
			for (IntVec3 c = to; c != from; c = prev[c]) leg.Add(c);
			for (int i = leg.Count - 1; i >= 0; i--) Append(into, leg[i], cost);
		}

		// une etape au fond d'un cul-de-sac fait faire demi-tour au meneur, et la file se replie sur elle-meme.
		// des que le trajet revient sur une case qu'il vient de quitter, ou la frole, on coupe la boucle
		private static void Append(List<IntVec3> into, IntVec3 c, Dictionary<IntVec3, int> cost)
		{
			for (int j = Mathf.Max(0, into.Count - Fold); j < into.Count - 1; j++)
			{
				if (!Touching(into[j], c, cost)) continue;
				into.RemoveRange(j + 1, into.Count - j - 1);
				break;
			}
			if (into[into.Count - 1] != c) into.Add(c);
		}

		private static bool Touching(IntVec3 a, IntVec3 c, Dictionary<IntVec3, int> cost)
		{
			int dx = Mathf.Abs(a.x - c.x), dz = Mathf.Abs(a.z - c.z);
			if (dx > 1 || dz > 1) return false;
			return dx + dz < 2 || (cost.ContainsKey(new IntVec3(a.x, 0, c.z)) && cost.ContainsKey(new IntVec3(c.x, 0, a.z)));
		}

		private static int Sector(IntVec3 c, IntVec3 spot)
		{
			float a = Mathf.Atan2(c.z - spot.z, c.x - spot.x) + Mathf.PI;
			return Mathf.Min(7, (int)(a / (2f * Mathf.PI) * 8f));
		}
	}
}
