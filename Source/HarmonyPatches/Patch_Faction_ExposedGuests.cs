using HarmonyLib;
using RimWorld;
using Verse;

namespace RimFeast.HarmonyPatches
{
	// les invites sont de vrais membres de leur maison: le jeu lui compte chaque coup qu'ils recoivent, leur capture
	// et leur depouille. le cortege d'un empoisonneur demasque fait exception, personne ne reproche a l'hote ce qui
	// lui arrive en sortant. hors de ce cas les trois methodes du jeu tournent telles quelles
	[HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberTookDamage))]
	internal static class Patch_Faction_Notify_MemberTookDamage
	{
		private static bool Prefix(Pawn member) => !GameComponent_FeastState.FairGame(member);
	}

	[HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberCaptured))]
	internal static class Patch_Faction_Notify_MemberCaptured
	{
		private static bool Prefix(Pawn member) => !GameComponent_FeastState.FairGame(member);
	}

	[HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberStripped))]
	internal static class Patch_Faction_Notify_MemberStripped
	{
		private static bool Prefix(Pawn member) => !GameComponent_FeastState.FairGame(member);
	}
}
