using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimFeast
{
	public class CompProperties_FeastArea : CompProperties
	{
		public CompProperties_FeastArea()
		{
			compClass = typeof(CompFeastArea);
		}
	}

	public class CompFeastArea : ThingComp
	{
		public const float MinRadius = 6f;
		public const float MaxRadius = 30f;
		public const float DefRadius = 18f;

		private float radius = DefRadius;
		private bool wholeRoom;
		private List<IntVec3> regionCells;
		private HashSet<IntVec3> regionSet;

		private List<IntVec3> drawCells;
		private float drawStamp = -999f;

		public float Radius => radius;
		public bool WholeRoom => wholeRoom;
		public bool HasRegion => regionCells != null && regionCells.Count > 0;

		public HashSet<IntVec3> Region
		{
			get
			{
				if (!HasRegion) return null;
				if (regionSet == null) regionSet = new HashSet<IntVec3>(regionCells);
				return regionSet;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref radius, "radius", DefRadius);
			Scribe_Values.Look(ref wholeRoom, "wholeRoom");
			Scribe_Collections.Look(ref regionCells, "regionCells", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.PostLoadInit) regionSet = null;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			yield return new Designator_FeastArea(this, add: true);
			yield return new Designator_FeastArea(this, add: false);
		}

		public bool InReach(IntVec3 c) => parent.Spawned && c.DistanceTo(parent.Position) <= MaxRadius + 0.5f;

		public bool IsSpotCell(IntVec3 c) => parent.Spawned && parent.OccupiedRect().Contains(c);

		public bool InArea(IntVec3 c) => parent.Spawned && FeastUtility.FeastArea.For(parent.Position, parent.Map).Contains(c);

		// premier coup de pinceau: la forme remplace le cercle, comme RimMarket
		public void Paint(List<IntVec3> cells, bool add)
		{
			if (!parent.Spawned || cells == null) return;
			HashSet<IntVec3> set = HasRegion ? new HashSet<IntVec3>(regionCells)
				: add ? new HashSet<IntVec3>()
				: new HashSet<IntVec3>(FeastUtility.FeastArea.For(parent.Position, parent.Map).Cells());
			for (int i = 0; i < cells.Count; i++)
			{
				if (add) set.Add(cells[i]);
				else if (!IsSpotCell(cells[i])) set.Remove(cells[i]);
			}
			foreach (IntVec3 c in parent.OccupiedRect()) set.Add(c);
			regionCells = new List<IntVec3>(set);
			regionSet = null;
			wholeRoom = false;
			drawStamp = -999f;
			float far = 0f;
			for (int i = 0; i < regionCells.Count; i++) far = Mathf.Max(far, regionCells[i].DistanceTo(parent.Position));
			radius = Mathf.Clamp(Mathf.Ceil(far), MinRadius, MaxRadius);
		}

		public void DrawArea()
		{
			if (!parent.Spawned) return;
			if (drawCells == null || Time.realtimeSinceStartup - drawStamp > 1f)
			{
				drawCells = FeastUtility.FeastArea.For(parent.Position, parent.Map).Cells();
				drawStamp = Time.realtimeSinceStartup;
			}
			GenDraw.DrawFieldEdges(drawCells);
		}

		public override void PostDrawExtraSelectionOverlays()
		{
			base.PostDrawExtraSelectionOverlays();
			DrawArea();
		}

		public string InspectLine()
		{
			if (!parent.Spawned) return null;
			if (HasRegion) return "RimFeast_SpotAreaPainted".Translate(regionCells.Count);
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(parent.Position, parent.Map);
			return area.WholeRoom
				? "RimFeast_SpotAreaRoom".Translate(area.Cells().Count)
				: "RimFeast_SpotAreaRadius".Translate(radius.ToString("F0"));
		}
	}
}
