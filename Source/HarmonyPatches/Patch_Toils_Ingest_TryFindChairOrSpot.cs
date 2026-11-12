using HarmonyLib;
using RimWorld;
using Verse;

namespace RimFeast.HarmonyPatches
{
	// le colon qui a une place va y manger. le jeu appelle cette recherche deux fois par repas: quand il choisit
	// le repas, puis l'assiette en main. tout le reste est le repas du jeu, avec les mods qui y touchent
	[HarmonyPatch(typeof(Toils_Ingest), nameof(Toils_Ingest.TryFindChairOrSpot))]
	internal static class Patch_Toils_Ingest_TryFindChairOrSpot
	{
		private static bool Prefix(Pawn pawn, Thing ingestible, ref IntVec3 cell, ref bool __result)
		{
			if (FeastSeats.LastResort) return true;
			FeastSeats.Hidden = 0;
			if (!FeastSeats.TryGetMealSeat(pawn, ingestible, out IntVec3 seat)) return true;
			cell = seat;
			__result = true;
			return false;
		}

		// la recherche du jeu a saute les places des autres. s'il ne reste que manger debout, on la refait
		// sans le filtre: mieux vaut la chaise d'un autre que le malus du repas sans table
		private static void Postfix(Pawn pawn, Thing ingestible, ref IntVec3 cell, ref bool __result)
		{
			if (FeastSeats.LastResort || FeastSeats.Hidden == 0 || pawn?.Map == null) return;
			if (__result && cell.IsValid && cell.GetEdifice(pawn.Map)?.def.building?.isSittable == true) return;
			FeastSeats.LastResort = true;
			try
			{
				__result = Toils_Ingest.TryFindChairOrSpot(pawn, ingestible, out cell);
			}
			finally
			{
				FeastSeats.LastResort = false;
			}
		}
	}
}
