using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimFeast.HarmonyPatches
{
	// une place attribuee n'est pas libre pour les autres tant que son proprietaire est sur la carte.
	// le jeu passe par ici pour le repas, le verre, la detente a table et le biberon. la lecture a sa propre copie
	[HarmonyPatch]
	internal static class Patch_TryFindFreeSittingSpotOnThing
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(Toils_Ingest), nameof(Toils_Ingest.TryFindFreeSittingSpotOnThing));
			MethodInfo reading = AccessTools.Method(typeof(JobDriver_Reading), "TryFindFreeSittingSpotOnThing");
			if (reading != null) yield return reading;
		}

		private static void Postfix(Thing t, Pawn pawn, ref IntVec3 cell, ref bool __result)
		{
			if (!__result || FeastSeats.LastResort || t == null || pawn == null) return;
			if (!FeastSeats.ClaimedByOther(t, pawn)) return;
			// reglage decoche: les places ne comptent qu'au banquet
			if (!RimFeastMod.S.seatMeals && !FeastSeats.AtFeast(pawn)) return;
			__result = false;
			cell = IntVec3.Invalid;
			FeastSeats.Hidden++;
		}
	}
}
