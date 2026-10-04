using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimFeast.Planet
{
	// calque de CaravanArrivalAction_VisitPeaceTalks
	public class CaravanArrivalAction_VisitFeast : CaravanArrivalAction
	{
		private Settlement seat;

		public override string Label => "RimFeast_AwayVisitLabel".Translate(seat.Label);

		public override string ReportString => "RimFeast_AwayVisitReport".Translate(seat.Label);

		public CaravanArrivalAction_VisitFeast() { }

		public CaravanArrivalAction_VisitFeast(Settlement seat)
		{
			this.seat = seat;
		}

		public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
		{
			FloatMenuAcceptanceReport report = base.StillValid(caravan, destinationTile);
			if (!report) return report;
			if (seat != null && seat.Tile != destinationTile) return false;
			return CanVisit(caravan, seat);
		}

		public override void Arrived(Caravan caravan)
		{
			GameComponent_FeastState.Get()?.ResolveAwayFeast(caravan, seat);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref seat, "seat");
		}

		public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, Settlement seat)
		{
			if (seat == null || !seat.Spawned || seat.Faction == null) return false;
			return GameComponent_FeastState.Get()?.AwayInviteOpen(seat.Faction) ?? false;
		}

		public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, Settlement seat)
		{
			return CaravanArrivalActionUtility.GetFloatMenuOptions(
				() => CanVisit(caravan, seat),
				() => new CaravanArrivalAction_VisitFeast(seat),
				"RimFeast_AwayVisitLabel".Translate(seat.Label),
				caravan, seat.Tile, seat);
		}
	}
}
