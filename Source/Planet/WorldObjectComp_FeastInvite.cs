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

	// Settlement appelle base en premier: l'option s'ajoute sans patch
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

		public override string CompInspectStringExtra()
		{
			if (!Open) return null;
			int left = GameComponent_FeastState.Get().AwayInviteDaysLeft();
			return "RimFeast_AwaySeatInspect".Translate(left);
		}
	}
}
