using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimFeast.AI
{
	// branche sur Humanlike_PreMain: apres le travail d'urgence, la faim extreme et les fetes, avant le tri des besoins
	public class JobGiver_EatAtSeat : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!RimFeastMod.S.seatMeals) return null;
			if (!pawn.IsColonist || !pawn.Spawned || pawn.Drafted || pawn.GetLord() != null) return null;
			Need_Food food = pawn.needs?.food;
			if (food == null || food.CurLevelPercentage >= pawn.RaceProps.FoodLevelPercentageWantEat) return null;
			if (FoodUtility.ShouldBeFedBySomeone(pawn) || !FeastSeats.CanEatAtTable(pawn)) return null;
			if (!FeastSeats.TryGetSeat(pawn, out IntVec3 seat)) return null;

			if (!FoodUtility.TryFindBestFoodSourceFor(pawn, pawn, food.CurCategory == HungerCategory.Starving,
				out Thing source, out ThingDef foodDef, canRefillDispenser: false, canUseInventory: false,
				allowCorpse: false)) return null;
			if (!FeastSeats.Carriable(source, foodDef)) return null;
			return FeastSeats.EatJob(pawn, source, foodDef, seat);
		}
	}
}
