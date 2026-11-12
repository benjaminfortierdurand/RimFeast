using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace RimFeast.HarmonyPatches
{
	// la musique de taverne: a un banquet, l'instrument dont on joue fait entendre un air de taverne a la place de
	// ses propres notes. le jeu ne cree son son que si ce champ est vide: on y met le notre juste avant
	[HarmonyPatch(typeof(Building_MusicalInstrument), "Tick")]
	internal static class Patch_Building_MusicalInstrument_Tick
	{
		private static void Prefix(Building_MusicalInstrument __instance, ref Sustainer ___soundPlaying, Pawn ___currentPlayer)
		{
			if (___currentPlayer == null) return;
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp == null) return;
			// la soiree est finie et quelqu'un joue encore: l'instrument retrouve son propre son
			if (___soundPlaying != null && ___soundPlaying.Ended && GameComponent_FeastState.IsTavern(___soundPlaying.def))
				___soundPlaying = null;
			if (___soundPlaying == null) ___soundPlaying = comp.TavernFor(__instance);
			else comp.TavernHeard(___soundPlaying);
		}
	}
}
