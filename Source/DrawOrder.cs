using System.Reflection;
using Verse;

namespace RimFeast
{
	// le jeu tire une fois par pion sa hauteur de dessin (SeededYOffset): deux pions colles se coupent n'importe comment.
	// les danseurs la reglent selon qui est au sud, et la rendent a la fin
	internal static class DrawOrder
	{
		private static readonly FieldInfo Seed =
			typeof(Pawn_DrawTracker).GetField("<SeededYOffset>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
		private static bool locked;

		public static void Set(Pawn p, ref float saved, float y)
		{
			if (float.IsNaN(saved)) saved = p.Drawer.SeededYOffset;
			if (p.Drawer.SeededYOffset != y) Write(p, y);
		}

		public static void Restore(Pawn p, ref float saved)
		{
			if (float.IsNaN(saved)) return;
			Write(p, saved);
			saved = float.NaN;
		}

		// si un runtime refuse d'ecrire le champ, on danse sans, plutot qu'une erreur a chaque image
		private static void Write(Pawn p, float y)
		{
			if (Seed == null || locked) return;
			try { Seed.SetValue(p.Drawer, y); }
			catch { locked = true; }
		}
	}
}
