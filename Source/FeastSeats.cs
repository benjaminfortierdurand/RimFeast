using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

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

		// la recherche de chaise du jeu saute les places des autres (Patch_TryFindFreeSittingSpotOnThing).
		// LastResort leve ce filtre le temps d'une seconde recherche, Hidden compte ce que la premiere a saute
		internal static bool LastResort;
		internal static int Hidden;

		// vrai quand s'asseoir la prendrait la place de quelqu'un qui est sur la carte. un proprietaire parti
		// en caravane ne garde rien, et un banc a deux places n'est pris que si ses deux proprietaires l'attendent
		public static bool ClaimedByOther(Thing chair, Pawn pawn)
		{
			CompFeastSeat seat = chair.TryGetComp<CompFeastSeat>();
			if (seat == null || !chair.Spawned) return false;
			List<Pawn> owners = seat.AssignedPawnsForReading;
			if (owners.Count == 0 || owners.Contains(pawn)) return false;
			CellRect rect = chair.OccupiedRect();
			int waiting = 0;
			for (int i = 0; i < owners.Count; i++)
			{
				Pawn o = owners[i];
				if (o != null && o.Spawned && o.Map == chair.Map && !Seated(o, chair, rect)) waiting++;
			}
			if (waiting == 0 || !InAnyHall(chair)) return false;
			if (rect.Area == 1) return true;
			int free = 0;
			foreach (IntVec3 c in rect)
				if (pawn.CanReserveSittableOrSpot(c)) free++;
			return free <= waiting;
		}

		private static bool Seated(Pawn owner, Thing chair, CellRect rect)
		{
			if (rect.Contains(owner.Position)) return true;
			if (rect.Area == 1) return false;
			ReservationManager held = chair.Map.reservationManager;
			foreach (IntVec3 c in rect)
				if (held.ReservedBy(c, owner)) return true;
			return false;
		}

		public static bool AtFeast(Pawn pawn)
		{
			LordJob job = pawn.GetLord()?.LordJob;
			return job is AI.LordJob_FeastColonists || job is AI.LordJob_FeastGuests;
		}

		// la recherche de place libre du jeu, sans le filtre: le banquet garde les places des autres en dernier recours
		public static bool FreeCellOn(Thing chair, Pawn pawn, out IntVec3 cell)
		{
			foreach (IntVec3 c in chair.OccupiedRect())
				if (pawn.CanReserveSittableOrSpot(c))
				{
					cell = c;
					return true;
				}
			cell = IntVec3.Invalid;
			return false;
		}

		public static bool InAnyHall(Thing chair)
		{
			if (!chair.Spawned) return false;
			foreach (Building_FeastSpot spot in chair.Map.listerBuildings.AllBuildingsColonistOfClass<Building_FeastSpot>())
				if (FeastUtility.FeastArea.For(spot.Position, chair.Map).Contains(chair.Position)) return true;
			return false;
		}

		// atTable: pour y manger il faut une table a cote. pour s'y asseoir au banquet, non: un trone contre le mur compte
		public static bool TryGetSeat(Pawn pawn, out IntVec3 cell, FeastUtility.FeastArea? hall = null, bool atTable = true)
		{
			cell = IntVec3.Invalid;
			Thing chair = SeatOf(pawn)?.parent;
			if (chair == null || !chair.Spawned || chair.Map != pawn.Map) return false;
			if (hall.HasValue ? !hall.Value.Contains(chair.Position) : !InAnyHall(chair)) return false;
			if (chair.IsForbidden(pawn) || chair.IsBurning()) return false;
			foreach (IntVec3 c in chair.OccupiedRect())
			{
				if (!pawn.CanReserveSittableOrSpot(c)) continue;
				if (atTable && !BesideTable(c, chair.Map)) continue;
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

		// la place ou ce colon va manger ou boire ca. false: le jeu cherche la chaise libre la plus proche, comme d'habitude.
		// a un banquet la place doit etre dans la salle du banquet, et le verre s'y boit aussi.
		// hors banquet elle ne vaut que pour un repas, et si le reglage est coche
		public static bool TryGetMealSeat(Pawn pawn, Thing food, out IntVec3 cell)
		{
			cell = IntVec3.Invalid;
			if (pawn == null || food == null || !pawn.Spawned || !pawn.IsColonist) return false;
			if (ChairRadius(food) <= 0f || !CanEatAtTable(pawn)) return false;
			Lord lord = pawn.GetLord();
			if (lord == null) return RimFeastMod.S.seatMeals && IsMeal(food) && TryGetSeat(pawn, out cell);
			if (!(lord.LordJob is AI.LordJob_FeastColonists feast)) return false;
			return TryGetSeat(pawn, out cell, FeastUtility.FeastArea.For(feast.Spot, pawn.Map), IsMeal(food));
		}

		// 0: le jeu ne porte pas ca jusqu'a une chaise
		private static float ChairRadius(Thing food)
		{
			if (food is Building_NutrientPasteDispenser dispenser)
				return dispenser.DispensableDef?.ingestible?.chairSearchRadius ?? 0f;
			if (food is Pawn || food is Plant || food is Corpse) return 0f;
			return food.def.ingestible?.chairSearchRadius ?? 0f;
		}

		private static bool IsMeal(Thing food) =>
			food is Building_NutrientPasteDispenser || (food.def.IsNutritionGivingIngestible && !food.def.IsDrug);
	}
}
