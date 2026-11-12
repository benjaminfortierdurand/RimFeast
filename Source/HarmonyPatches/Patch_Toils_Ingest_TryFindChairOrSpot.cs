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
			if (!FeastSeats.TryGetMealSeat(pawn, ingestible, out IntVec3 seat)) return true;
			cell = seat;
			__result = true;
			return false;
		}
	}
}
