using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimFeast
{
	public class CompProperties_FeastSeat : CompProperties_AssignableToPawn
	{
		public CompProperties_FeastSeat()
		{
			compClass = typeof(CompFeastSeat);
			drawUnownedAssignmentOverlay = false;
		}
	}

	public class CompFeastSeat : CompAssignableToPawn
	{
		private bool inHall;
		private float hallStamp = -999f;

		private bool InHallCached
		{
			get
			{
				if (Time.realtimeSinceStartup - hallStamp > 1f)
				{
					inHall = FeastSeats.InAnyHall(parent);
					hallStamp = Time.realtimeSinceStartup;
				}
				return inHall;
			}
		}

		public override IEnumerable<Pawn> AssigningCandidates =>
			base.AssigningCandidates.Where(FeastSeats.CanEatAtTable);

		protected override bool ShouldShowAssignmentGizmo() =>
			parent.Faction == Faction.OfPlayer && (assignedPawns.Count > 0 || InHallCached);

		protected override string GetAssignmentGizmoLabel() => "RimFeast_SeatAssign".Translate();

		protected override string GetAssignmentGizmoDesc() => "RimFeast_SeatDesc".Translate();

		public override bool AssignedAnything(Pawn pawn) => FeastSeats.SeatOf(pawn) != null;

		public override void TryAssignPawn(Pawn pawn)
		{
			CompFeastSeat old = FeastSeats.SeatOf(pawn);
			if (old != null && old != this) old.TryUnassignPawn(pawn);
			while (assignedPawns.Count >= MaxAssignedPawnsCount && !assignedPawns.Contains(pawn))
				TryUnassignPawn(assignedPawns[assignedPawns.Count - 1]);
			base.TryAssignPawn(pawn);
		}

		public override string CompInspectStringExtra()
		{
			if (assignedPawns.Count == 0) return null;
			return "RimFeast_SeatOf".Translate(assignedPawns.Select(p => p.LabelShort).ToCommaList(true));
		}

		// les chaises d'avant RimFeast n'ont pas ces listes dans la sauvegarde: vanilla les laisse a null
		public override void PostExposeData()
		{
			Scribe_Collections.Look(ref assignedPawns, "assignedPawns", LookMode.Reference);
			Scribe_Collections.Look(ref uninstalledAssignedPawns, "uninstalledAssignedPawns", LookMode.Reference);
			if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
			if (assignedPawns == null) assignedPawns = new List<Pawn>();
			if (uninstalledAssignedPawns == null) uninstalledAssignedPawns = new List<Pawn>();
			assignedPawns.RemoveAll(p => p == null);
			uninstalledAssignedPawns.RemoveAll(p => p == null);
		}
	}
}
