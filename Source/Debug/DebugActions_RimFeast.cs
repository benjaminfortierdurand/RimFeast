using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace RimFeast.Debug
{
	public static class DebugActions_RimFeast
	{
		[DebugAction("RimFeast", "Cortege now with the duke: pick a house",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void CortegeNowWithLeader()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp != null) comp.debugForceLeader = true;
			CortegeNow();
		}

		[DebugAction("RimFeast", "Trap cortege now (red wedding): pick a house",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void CortegeNowTrap()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp != null) comp.debugForcePlanned = true;
			CortegeNow();
		}

		[DebugAction("RimFeast", "Treacherous cortege now: pick a house",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void CortegeNowTreacherous()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp != null) comp.debugForceTreachery = true;
			CortegeNow();
		}

		[DebugAction("RimFeast", "Cortege now: pick a house",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void CortegeNow()
		{
			Map map = Find.CurrentMap;
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (map == null || comp == null) return;
			if (comp.Busy)
			{
				Messages.Message("RimFeast: a feast is already planned or underway.",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}

			Thing spot = null;
			foreach (Building b in map.listerBuildings.AllBuildingsColonistOfDef(RimFeastDefOf.RimFeast_FeastSpot))
			{
				spot = b;
				break;
			}
			if (spot == null)
			{
				Messages.Message("RimFeast: no feast spot on this map.",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}

			var options = new List<DebugMenuOption>();
			foreach (Faction house in FeastUtility.InvitableHouses())
			{
				Faction h = house;
				options.Add(new DebugMenuOption(h.Name, DebugMenuOptionMode.Action, delegate
				{
					comp.StartInvite(map, h, spot);
					comp.DebugRush();
				}));
			}
			if (options.Count == 0)
			{
				Messages.Message("RimFeast: no invitable noble house found.",
					MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
		}

		[DebugAction("RimFeast", "Rush current stage",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void RushStage()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp == null || !comp.Busy)
			{
				Messages.Message("RimFeast: no feast to rush.", MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			comp.DebugRush();
		}

		[DebugAction("RimFeast", "Force marriage proposal",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void ForceProposal()
		{
			GameComponent_FeastState.Get()?.DebugForceProposal();
		}

		[DebugAction("RimFeast", "Force a request at the table: pick one",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void ForceRequest()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp == null) return;
			var options = new List<DebugMenuOption>
			{
				new DebugMenuOption("A blade from your forge", DebugMenuOptionMode.Action,
					() => comp.DebugForceRequest(GameComponent_FeastState.ReqWeapon)),
				new DebugMenuOption("A piece of art from the hall", DebugMenuOptionMode.Action,
					() => comp.DebugForceRequest(GameComponent_FeastState.ReqArt)),
				new DebugMenuOption("A child as a page (ward)", DebugMenuOptionMode.Action,
					() => comp.DebugForceRequest(GameComponent_FeastState.ReqWard)),
				new DebugMenuOption("A night with one of yours", DebugMenuOptionMode.Action,
					() => comp.DebugForceRequest(GameComponent_FeastState.ReqBed)),
				new DebugMenuOption("Swear off a rival house", DebugMenuOptionMode.Action,
					() => comp.DebugForceRequest(GameComponent_FeastState.ReqRival)),
			};
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
		}

		[DebugAction("RimFeast", "Invitation to their hall now",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void AwayInviteNow()
		{
			GameComponent_FeastState.Get()?.DebugAwayInviteNow();
		}

		[DebugAction("RimFeast", "Force an away feast outcome: pick one",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void AwayOutcomeNow()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp == null) return;
			var options = new List<DebugMenuOption>
			{
				new DebugMenuOption("Knives at their table", DebugMenuOptionMode.Action,
					() => comp.DebugAwayOutcome(GameComponent_FeastState.AwayKnives)),
				new DebugMenuOption("It was never a feast (ambush on the road, generates a map)",
					DebugMenuOptionMode.Action,
					() => comp.DebugAwayOutcome(GameComponent_FeastState.AwayAmbush)),
				new DebugMenuOption("Seated below the salt", DebugMenuOptionMode.Action,
					() => comp.DebugAwayOutcome(GameComponent_FeastState.AwaySlight)),
				new DebugMenuOption("A quiet evening", DebugMenuOptionMode.Action,
					() => comp.DebugAwayOutcome(GameComponent_FeastState.AwayDull)),
				new DebugMenuOption("A good evening", DebugMenuOptionMode.Action,
					() => comp.DebugAwayOutcome(GameComponent_FeastState.AwayFine)),
				new DebugMenuOption("They will be talking about it", DebugMenuOptionMode.Action,
					() => comp.DebugAwayOutcome(GameComponent_FeastState.AwaySongs)),
			};
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
		}

		[DebugAction("RimFeast", "Bring wards home now",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void RushWards()
		{
			GameComponent_FeastState.Get()?.DebugRushWards();
		}

		[DebugAction("RimFeast", "Log wards abroad",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void LogWards()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			Log.Message(comp?.DebugWardReport() ?? "RimFeast: no game component.");
		}

		[DebugAction("RimFeast", "Rush pending caravans/raids",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void RushCaravans()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			comp?.DebugRushCaravans();
		}

		[DebugAction("RimFeast", "Log why colonists are not at the table",
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void LogColonists()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			Log.Message(comp?.DebugColonistReport() ?? "RimFeast: no game component.");
		}
	}
}
