using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimFeast
{
	[StaticConstructorOnStartup]
	public static class FeastSeats
	{
		// ajoute au demarrage: un patch XML ne voit pas les chaises qui heritent isSittable d'un parent abstrait
		static FeastSeats()
		{
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
			{
				if (def.building == null || !def.building.isSittable || def.IsFrame || def.IsBlueprint) continue;
				if (!typeof(ThingWithComps).IsAssignableFrom(def.thingClass)) continue;
				if (def.HasAssignableCompFrom(typeof(CompAssignableToPawn))) continue;
				def.comps.Add(new CompProperties_FeastSeat { maxAssignedPawnsCount = Mathf.Max(1, def.size.x * def.size.z) });
				// sans overlay le nom du proprietaire ne s'affiche jamais, et sans ca la qualite s'afficherait
				if (!def.drawGUIOverlay)
				{
					def.drawGUIOverlay = true;
					def.drawGUIOverlayQuality = false;
				}
			}
		}

		public static bool CanEatAtTable(Pawn p) =>
			p.RaceProps.ToolUser && !p.DevelopmentalStage.Baby()
			&& p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
			&& !(p.IsMutant && p.mutant.Def.disableEatingAtTable);

		public static CompFeastSeat SeatOf(Pawn pawn)
		{
			Map map = pawn?.MapHeld;
			if (map == null) return null;
			List<Building> list = map.listerBuildings.allBuildingsColonist;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].def.building?.isSittable != true) continue;
				CompFeastSeat seat = list[i].GetComp<CompFeastSeat>();
				if (seat != null && seat.AssignedPawnsForReading.Contains(pawn)) return seat;
			}
			return null;
		}

		public static bool ClaimedByOther(Thing chair, Pawn pawn)
		{
			CompFeastSeat seat = chair.TryGetComp<CompFeastSeat>();
			return seat != null && seat.AssignedPawnsForReading.Count > 0 && !seat.AssignedPawnsForReading.Contains(pawn);
		}

		public static bool InAnyHall(Thing chair)
		{
			if (!chair.Spawned) return false;
			foreach (Building_FeastSpot spot in chair.Map.listerBuildings.AllBuildingsColonistOfClass<Building_FeastSpot>())
				if (FeastUtility.FeastArea.For(spot.Position, chair.Map).Contains(chair.Position)) return true;
			return false;
		}

		public static bool TryGetSeat(Pawn pawn, out IntVec3 cell, FeastUtility.FeastArea? hall = null)
		{
			cell = IntVec3.Invalid;
			Thing chair = SeatOf(pawn)?.parent;
			if (chair == null || !chair.Spawned || chair.Map != pawn.Map) return false;
			if (hall.HasValue ? !hall.Value.Contains(chair.Position) : !InAnyHall(chair)) return false;
			if (chair.IsForbidden(pawn) || chair.IsBurning()) return false;
			foreach (IntVec3 c in chair.OccupiedRect())
			{
				if (!pawn.CanReserveSittableOrSpot(c)) continue;
				if (!BesideTable(c, chair.Map)) continue;
				if (c.GetDangerFor(pawn, chair.Map) != Danger.None) continue;
				if (!pawn.CanReach(c, PathEndMode.OnCell, Danger.Some)) continue;
				cell = c;
				return true;
			}
			return false;
		}

		private static bool BesideTable(IntVec3 c, Map map)
		{
			for (int i = 0; i < 4; i++)
				if ((c + GenAdj.CardinalDirections[i]).HasEatSurface(map)) return true;
			return false;
		}

		public static bool Carriable(Thing food, ThingDef foodDef)
		{
			if (food == null || !food.Spawned || foodDef?.ingestible == null) return false;
			if (food is Building_NutrientPasteDispenser dispenser) return dispenser.HasEnoughFeedstockInHoppers();
			if (food is Pawn || food is Plant || food is Corpse) return false;
			return food.def == foodDef && foodDef.ingestible.chairSearchRadius > 0f;
		}

		public static Job EatJob(Pawn pawn, Thing food, ThingDef foodDef, IntVec3 seat)
		{
			Job job = JobMaker.MakeJob(RimFeastDefOf.RimFeast_EatAtSeatJob, food, LocalTargetInfo.Invalid, seat);
			job.count = FoodUtility.WillIngestStackCountOf(pawn, foodDef, FoodUtility.GetNutrition(pawn, food, foodDef));
			return job;
		}
	}
}
