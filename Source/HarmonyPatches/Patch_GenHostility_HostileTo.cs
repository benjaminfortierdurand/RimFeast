using HarmonyLib;
using RimWorld;
using Verse;

namespace RimFeast.HarmonyPatches
{
	// la treve du banquet. depuis que les invites sont de vraies factions, deux maisons ennemies entre elles
	// s'entretuaient des qu'un devoir de combat s'activait (invite blesse sur la route, caravane de passage).
	// tant qu'un cortege est sur la carte, ses invites ne sont hostiles ni entre eux ni aux groupes en paix avec le joueur.
	// le test du jeu est appele tres souvent: on sort au premier booleen quand il n'y a pas de banquet
	[HarmonyPatch(typeof(GenHostility), nameof(GenHostility.HostileTo), new[] { typeof(Thing), typeof(Thing) })]
	internal static class Patch_GenHostility_HostileTo
	{
		private static bool Prefix(Thing a, Thing b, ref bool __result)
		{
			if (!GameComponent_FeastState.TruceOn) return true;
			if (!GameComponent_FeastState.UnderTruce(a as Pawn, b as Pawn)) return true;
			__result = false;
			return false;
		}
	}
}
