using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimFeast.Planet
{
	public class WorldObjectCompProperties_FeastInvite : WorldObjectCompProperties
	{
		public WorldObjectCompProperties_FeastInvite()
		{
			compClass = typeof(WorldObjectComp_FeastInvite);
		}
	}

	// greffe sur les colonies vanilla, ajoutee par Patches/. WorldObject.GetFloatMenuOptions
	// parcourt les comps et Settlement appelle bien base en premier, donc l'option apparait
	// dans le menu de la caravane sans toucher a une seule ligne de vanilla
	public class WorldObjectComp_FeastInvite : WorldObjectComp
	{
		public Settlement Seat => parent as Settlement;

		private bool Open =>
			Seat != null && Seat.Faction != null
			&& (GameComponent_FeastState.Get()?.AwayInviteOpen(Seat.Faction) ?? false);

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
		{
			if (!Open) yield break;
			foreach (FloatMenuOption o in CaravanArrivalAction_VisitFeast.GetFloatMenuOptions(caravan, Seat))
				yield return o;
		}

		// la carte du monde n'a plus de marqueur a nous: c'est ici que le joueur lit qu'une
		// table l'attend, et combien de temps elle l'attend encore
		public override string CompInspectStringExtra()
		{
			if (!Open) return null;
			int left = GameComponent_FeastState.Get().AwayInviteDaysLeft();
			return "RimFeast_AwaySeatInspect".Translate(left);
		}
	}
}
