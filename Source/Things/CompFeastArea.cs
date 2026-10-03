using System.Collections.Generic;
using RimWorld;
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

		private List<IntVec3> drawCells;
		private float drawStamp = -999f;

		public float Radius => radius;
		public bool WholeRoom => wholeRoom;

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref radius, "radius", DefRadius);
			Scribe_Values.Look(ref wholeRoom, "wholeRoom");
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			yield return new Command_Action
			{
				defaultLabel = "RimFeast_AreaPlus".Translate(radius.ToString("F0")),
				defaultDesc = "RimFeast_AreaDesc".Translate(),
				icon = TexButton.Plus,
				action = () => SetRadius(radius + 1f),
			};
			yield return new Command_Action
			{
				defaultLabel = "RimFeast_AreaMinus".Translate(radius.ToString("F0")),
				defaultDesc = "RimFeast_AreaDesc".Translate(),
				icon = TexButton.Minus,
				action = () => SetRadius(radius - 1f),
			};
			yield return new Command_Action
			{
				defaultLabel = "RimFeast_AreaAuto".Translate(),
				defaultDesc = "RimFeast_AreaAutoDesc".Translate(),
				icon = TexButton.AutoRebuild,
				action = FitHall,
			};
		}

		private void SetRadius(float r)
		{
			radius = Mathf.Clamp(r, MinRadius, MaxRadius);
			wholeRoom = false;
			drawStamp = -999f;
		}

		private void FitHall()
		{
			Room room = parent.Spawned ? parent.GetRoom() : null;
			if (room == null || room.PsychologicallyOutdoors)
			{
				Messages.Message("RimFeast_AreaNotEnclosed".Translate(), parent,
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			wholeRoom = true;
			drawStamp = -999f;
			Messages.Message("RimFeast_AreaFitted".Translate(room.CellCount), parent,
				MessageTypeDefOf.NeutralEvent, historical: false);
		}

		public override void PostDrawExtraSelectionOverlays()
		{
			base.PostDrawExtraSelectionOverlays();
			if (!parent.Spawned) return;
			if (drawCells == null || Time.realtimeSinceStartup - drawStamp > 1f)
			{
				drawCells = FeastUtility.FeastArea.For(parent.Position, parent.Map).Cells();
				drawStamp = Time.realtimeSinceStartup;
			}
			GenDraw.DrawFieldEdges(drawCells);
		}

		public string InspectLine()
		{
			if (!parent.Spawned) return null;
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(parent.Position, parent.Map);
			return area.WholeRoom
				? "RimFeast_SpotAreaRoom".Translate(area.Cells().Count)
				: "RimFeast_SpotAreaRadius".Translate(radius.ToString("F0"));
		}
	}
}
