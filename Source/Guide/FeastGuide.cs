using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimFeast
{
	public enum GuideLine
	{
		Required,
		Optional,
		Info,
		Problem,
	}

	public class GuideCheck
	{
		public string label;
		public bool done;
		public GuideLine kind;

		public GuideCheck(string label, bool done, GuideLine kind)
		{
			this.label = label;
			this.done = done;
			this.kind = kind;
		}
	}

	public class GuideAction
	{
		public string label;
		public Action<Map> act;
		public Func<Map, bool> enabled;
	}

	public class GuideChapter
	{
		public string key;
		public Texture2D art;
		public bool advanced;
		public bool diagnostic;
		public Func<Map, List<GuideCheck>> checks;
		public List<GuideAction> actions = new List<GuideAction>();

		public string Title => ("RimFeast_Guide" + key + "Title").Translate();
		public string Body => ("RimFeast_Guide" + key + "Body").Translate();
	}

	// le guide en jeu, repris de RimMarket: des chapitres, chacun avec un texte, une image, une liste de points
	// lus sur la carte et des boutons. la derniere page dit pourquoi un banquet ne peut pas se tenir
	[StaticConstructorOnStartup]
	public static class FeastGuide
	{
		public static readonly List<GuideChapter> Chapters = new List<GuideChapter>
		{
			new GuideChapter { key = "Spot", art = Art("Spot"), checks = SpotChecks, actions = { PlaceSpot(), ShowSpot() } },
			new GuideChapter { key = "Hall", art = Art("Hall"), checks = HallChecks, actions = { ShowSpot() } },
			new GuideChapter { key = "Invite", art = Art("Invite"), checks = InviteChecks, actions = { ShowSpot() } },
			new GuideChapter { key = "Evening", art = Art("Evening"), checks = EveningLines, actions = { Settings() } },
			new GuideChapter { key = "Seats", art = Art("Seats"), checks = SeatChecks, actions = { Settings() } },
			new GuideChapter { key = "Colony", art = Art("Colony"), checks = ColonyLines, actions = { ShowSpot() } },
			new GuideChapter { key = "Dance", art = Art("Dance"), advanced = true, checks = DanceChecks, actions = { Settings() } },
			new GuideChapter { key = "Grand", art = Art("Grand"), advanced = true, checks = GrandLines },
			new GuideChapter { key = "Red", art = Art("Red"), advanced = true, checks = RedLines, actions = { Settings() } },
			new GuideChapter { key = "Away", art = Art("Away"), advanced = true, checks = AwayLines, actions = { Settings() } },
			new GuideChapter { key = "Diag", diagnostic = true, checks = Diagnose },
		};

		private static Texture2D Art(string name) => ContentFinder<Texture2D>.Get("UI/Guide/RimFeast_Guide" + name, false);

		private static string L(string key) => ("RimFeast_Guide" + key).Translate();

		private static GuideAction PlaceSpot()
		{
			return new GuideAction
			{
				label = "RimFeast_GuidePlaceSpot",
				enabled = m => m != null && BuildCopyCommandUtility.FindAllowedDesignator(RimFeastDefOf.RimFeast_FeastSpot) != null,
				act = m =>
				{
					Designator_Build des = BuildCopyCommandUtility.FindAllowedDesignator(RimFeastDefOf.RimFeast_FeastSpot);
					if (des == null) return;
					Find.WindowStack.TryRemove(typeof(Window_FeastGuide));
					Find.DesignatorManager.Select(des);
				},
			};
		}

		private static GuideAction ShowSpot()
		{
			return new GuideAction
			{
				label = "RimFeast_GuideShowSpot",
				enabled = m => Spot(m) != null,
				act = m =>
				{
					Building_FeastSpot spot = Spot(m);
					if (spot == null) return;
					Find.WindowStack.TryRemove(typeof(Window_FeastGuide));
					Find.Selector.ClearSelection();
					Find.Selector.Select(spot, playSound: false, forceDesignatorDeselect: false);
					CameraJumper.TryJump(spot);
				},
			};
		}

		private static GuideAction Settings()
		{
			return new GuideAction
			{
				label = "RimFeast_GuideOpenSettings",
				enabled = m => true,
				act = m =>
				{
					Mod mod = LoadedModManager.GetMod<RimFeastMod>();
					if (mod != null) Find.WindowStack.Add(new Dialog_ModSettings(mod));
				},
			};
		}

		// le point du banquet en cours s'il y en a un, sinon le premier de la carte
		private static Building_FeastSpot Spot(Map map)
		{
			if (map == null) return null;
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			Thing busy = comp?.CurrentCase?.spotThing ?? comp?.ColonyCase?.spotThing;
			if (busy is Building_FeastSpot b && b.Spawned && b.Map == map) return b;
			return map.listerBuildings.AllBuildingsColonistOfClass<Building_FeastSpot>().FirstOrDefault(s => s.Spawned);
		}

		private static int UsualGuests => Mathf.Max(2, RimFeastMod.S.corteges);

		private static int ChairsAtTable(Building_FeastSpot spot)
		{
			Map map = spot.Map;
			var seen = new HashSet<Thing>();
			foreach (IntVec3 c in FeastUtility.FeastArea.For(spot.Position, map).Cells())
			{
				if (!c.InBounds(map)) continue;
				Building chair = c.GetEdifice(map);
				if (chair?.def.building == null || !chair.def.building.isSittable) continue;
				for (int i = 0; i < 4; i++)
				{
					IntVec3 n = c + GenAdj.CardinalDirections[i];
					if (!n.InBounds(map) || !n.HasEatSurface(map)) continue;
					seen.Add(chair);
					break;
				}
			}
			return seen.Count;
		}

		private static List<Faction> Willing()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			return FeastUtility.InvitableHouses().Where(h => comp == null || !comp.HouseRefuses(h, out _)).ToList();
		}

		private static int CheapestInvite(List<Faction> willing)
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp == null || willing.Count == 0) return GameComponent_FeastState.BaseInviteCost;
			return willing.Min(h => comp.InviteCostFor(h));
		}

		private static GuideCheck OnOff(string settingKey, bool on) =>
			new GuideCheck("RimFeast_GuideSetting".Translate(settingKey.Translate(), L(on ? "On" : "Off")), on, GuideLine.Info);

		private static List<GuideCheck> SpotChecks(Map map)
		{
			Building_FeastSpot spot = Spot(map);
			Room room = spot?.Position.GetRoom(map);
			var lines = new List<GuideCheck>
			{
				new GuideCheck(L("SpotPlaced"), spot != null, GuideLine.Required),
				new GuideCheck(L("SpotIndoors"), room != null && !room.PsychologicallyOutdoors, GuideLine.Optional),
			};
			string area = spot?.GetComp<CompFeastArea>()?.InspectLine();
			if (!area.NullOrEmpty()) lines.Add(new GuideCheck(area, true, GuideLine.Info));
			return lines;
		}

		private static List<GuideCheck> HallChecks(Map map)
		{
			Building_FeastSpot spot = Spot(map);
			if (spot == null) return new List<GuideCheck> { new GuideCheck(L("NoSpot"), false, GuideLine.Info) };
			int guests = UsualGuests;
			GameComponent_FeastState.Assess(map, spot.Position, guests, out _, out float table, out _, out int drinks);
			int chairs = ChairsAtTable(spot);
			var lines = new List<GuideCheck>
			{
				new GuideCheck("RimFeast_GuideHallChairs".Translate(chairs), chairs > 0, GuideLine.Required),
				new GuideCheck(L("HallMeals"), table > 0f, GuideLine.Required),
				new GuideCheck("RimFeast_GuideHallDrinks".Translate(drinks, guests), drinks >= guests, GuideLine.Optional),
				InstrumentLine(spot),
				new GuideCheck(GameComponent_FeastState.HallPreview(map, spot.Position, guests).Replace("\n", " "), true, GuideLine.Info),
			};
			return lines;
		}

		private static GuideCheck InstrumentLine(Building_FeastSpot spot)
		{
			if (!FeastUtility.MusicPossible) return new GuideCheck(L("NoMusic"), false, GuideLine.Info);
			return new GuideCheck(L("HallInstrument"), FeastUtility.AnyInstrumentIn(spot.Position, spot.Map), GuideLine.Optional);
		}

		private static List<GuideCheck> InviteChecks(Map map)
		{
			List<Faction> willing = Willing();
			int cost = CheapestInvite(willing);
			int silver = map == null ? 0 : FeastUtility.SilverAvailable(map);
			var lines = new List<GuideCheck>
			{
				new GuideCheck("RimFeast_GuideInviteHouses".Translate(willing.Count), willing.Count > 0, GuideLine.Required),
				new GuideCheck("RimFeast_GuideInviteSilver".Translate(silver, cost), silver >= cost, GuideLine.Required),
			};
			if (GameComponent_FeastState.Get()?.Busy ?? false) lines.Add(new GuideCheck(L("InviteBusy"), true, GuideLine.Info));
			return lines;
		}

		private static List<GuideCheck> EveningLines(Map map)
		{
			RimFeastSettings s = RimFeastMod.S;
			return new List<GuideCheck>
			{
				new GuideCheck("RimFeast_GuideEveningHours".Translate(s.feastHours), true, GuideLine.Info),
				OnOff("RimFeast_SetSummon", s.summonColonists),
				OnOff("RimFeast_SetLeader", s.leaderEnabled),
				OnOff("RimFeast_SetRequests", s.requestsEnabled),
				OnOff("RimFeast_SetMarriage", s.marriageEnabled),
				OnOff("RimFeast_SetTreachery", s.treacheryEnabled),
			};
		}

		private static List<GuideCheck> SeatChecks(Map map)
		{
			bool any = false;
			if (map != null)
			{
				List<Building> list = map.listerBuildings.allBuildingsColonist;
				for (int i = 0; i < list.Count && !any; i++)
				{
					if (list[i].def.building?.isSittable != true) continue;
					CompFeastSeat seat = list[i].GetComp<CompFeastSeat>();
					any = seat != null && seat.AssignedPawnsForReading.Count > 0;
				}
			}
			return new List<GuideCheck>
			{
				new GuideCheck(L("SeatsAssigned"), any, GuideLine.Optional),
				OnOff("RimFeast_SetSeatMeals", RimFeastMod.S.seatMeals),
			};
		}

		// le banquet ordinaire de la colonie: possible, ou la raison que donne le bouton grise
		private static GuideCheck ColonyPlain(GameComponent_FeastState comp, Map map)
		{
			string blocked = comp.ColonyFeastBlocked(map);
			return blocked.NullOrEmpty()
				? new GuideCheck(L("ColonyOk"), true, GuideLine.Problem)
				: new GuideCheck("RimFeast_GuideColonyNo".Translate(blocked), false, GuideLine.Problem);
		}

		private static List<GuideCheck> ColonyLines(Map map)
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp == null || map == null) return new List<GuideCheck>();
			if (Spot(map) == null) return new List<GuideCheck> { new GuideCheck(L("NoSpot"), false, GuideLine.Info) };
			int mourned = comp.Mourned(map).Count;
			string victory = comp.VictoryBlocked(map);
			string harvest = comp.HarvestBlocked(map);
			return new List<GuideCheck>
			{
				ColonyPlain(comp, map),
				new GuideCheck("RimFeast_GuideColonyWake".Translate(mourned), mourned > 0, GuideLine.Problem),
				new GuideCheck(victory ?? L("ColonyVictoryOk"), victory == null, GuideLine.Problem),
				new GuideCheck(harvest ?? L("ColonyHarvestOk"), harvest == null, GuideLine.Problem),
			};
		}

		private static List<GuideCheck> DanceChecks(Map map)
		{
			Building_FeastSpot spot = Spot(map);
			if (spot == null) return new List<GuideCheck> { new GuideCheck(L("NoSpot"), false, GuideLine.Info) };
			return new List<GuideCheck> { InstrumentLine(spot), OnOff("RimFeast_SetTavernMusic", RimFeastMod.S.tavernMusic) };
		}

		private static GuideCheck GrandLine(GameComponent_FeastState comp, Map map)
		{
			string blocked = comp.GrandBlocked(map, out List<Faction> houses, out int cost);
			return blocked == null
				? new GuideCheck("RimFeast_GuideGrandOk".Translate(houses.Count, cost), true, GuideLine.Problem)
				: new GuideCheck("RimFeast_GuideGrandNo".Translate(blocked), false, GuideLine.Problem);
		}

		private static List<GuideCheck> GrandLines(Map map)
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp == null || map == null) return new List<GuideCheck>();
			if (Spot(map) == null) return new List<GuideCheck> { new GuideCheck(L("NoSpot"), false, GuideLine.Info) };
			return new List<GuideCheck> { GrandLine(comp, map) };
		}

		private static List<GuideCheck> RedLines(Map map) =>
			new List<GuideCheck> { OnOff("RimFeast_SetMisfortune", RimFeastMod.S.misfortune) };

		private static List<GuideCheck> AwayLines(Map map)
		{
			RimFeastSettings s = RimFeastMod.S;
			var lines = new List<GuideCheck> { OnOff("RimFeast_SetAwayFeasts", s.awayFeastsEnabled) };
			if (s.awayFeastsEnabled) lines.Add(new GuideCheck("RimFeast_GuideAwayEvery".Translate(s.awayFeastDays), true, GuideLine.Info));
			return lines;
		}

		// pourquoi un banquet ne peut pas se tenir: chaque ligne est une condition, cochee quand elle est remplie
		private static List<GuideCheck> Diagnose(Map map)
		{
			var lines = new List<GuideCheck>();
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (map == null || comp == null) return lines;
			Building_FeastSpot spot = Spot(map);
			lines.Add(new GuideCheck(L("SpotPlaced"), spot != null, GuideLine.Problem));
			lines.Add(new GuideCheck(L("DiagFree"), !comp.Busy, GuideLine.Problem));

			List<Faction> known = FeastUtility.InvitableHouses().ToList();
			lines.Add(new GuideCheck("RimFeast_GuideDiagHouses".Translate(known.Count), known.Count > 0, GuideLine.Problem));
			bool refused = false;
			foreach (Faction h in known)
			{
				if (!comp.HouseRefuses(h, out string reason)) continue;
				refused = true;
				lines.Add(new GuideCheck("RimFeast_GuideDiagRefuse".Translate(h.Name, reason), false, GuideLine.Problem));
			}
			if (known.Count > 0 && !refused) lines.Add(new GuideCheck(L("DiagNoRefuse"), true, GuideLine.Problem));

			List<Faction> willing = Willing();
			int cost = CheapestInvite(willing);
			int silver = FeastUtility.SilverAvailable(map);
			lines.Add(new GuideCheck("RimFeast_GuideInviteSilver".Translate(silver, cost), silver >= cost, GuideLine.Problem));

			if (spot == null) return lines;
			lines.Add(ColonyPlain(comp, map));
			lines.Add(GrandLine(comp, map));
			if (!FeastUtility.MusicPossible) lines.Add(new GuideCheck(L("NoMusic"), false, GuideLine.Problem));
			else
			{
				bool music = FeastUtility.AnyInstrumentIn(spot.Position, map);
				lines.Add(new GuideCheck(L(music ? "HallInstrument" : "DiagNoDance"), music, GuideLine.Problem));
			}
			return lines;
		}
	}
}
