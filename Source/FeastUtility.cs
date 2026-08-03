using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimFeast
{
	public static class FeastUtility
	{
		private static readonly string[] Houses = { "Amboise", "Hesse", "Oswin", "Soren" };

		private static HediffDef alcoholHigh;
		private static bool? musicPossible;

		public static HediffDef AlcoholHigh =>
			alcoholHigh ?? (alcoholHigh = DefDatabase<HediffDef>.GetNamedSilentFail("AlcoholHigh"));

		// les instruments viennent de Royalty/Ideology. le verrou est dans le code vanilla
		// (Log.ErrorOnce), pas seulement dans les defs, donc on teste les deux
		public static bool MusicPossible
		{
			get
			{
				if (!musicPossible.HasValue)
					musicPossible = (ModLister.RoyaltyInstalled || ModLister.IdeologyInstalled)
						&& DefDatabase<ThingDef>.AllDefsListForReading.Any(d =>
							d.thingClass != null && typeof(Building_MusicalInstrument).IsAssignableFrom(d.thingClass));
				return musicPossible.Value;
			}
		}

		// maisons MO, sniffees sur les noms de kinds. null = pas de maison
		public static string HouseOf(Faction f)
		{
			if (f?.def?.pawnGroupMakers != null)
				foreach (PawnGroupMaker gm in f.def.pawnGroupMakers)
					if (gm.options != null)
						foreach (PawnGenOption o in gm.options)
							foreach (string h in Houses)
								if (o.kind != null && o.kind.defName.Contains(h))
									return h;
			return null;
		}

		// toute faction humanlike frequentable. les maisons MO gardent juste les plus
		// beaux corteges
		public static IEnumerable<Faction> InvitableHouses()
		{
			foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
			{
				if (f.IsPlayer || f.defeated || f.def.hidden || f.temporary) continue;
				if (f.def.permanentEnemy || !f.def.humanlikeFaction) continue;
				if (f.HostileTo(Faction.OfPlayer)) continue;
				if (CrewKindFor(f) == null) continue;
				yield return f;
			}
		}

		// seigneur de fortune quand la faction n'a pas de maison noble
		public static PawnKindDef EliteKindFor(Faction f)
		{
			PawnKindDef best = null;
			if (f?.def?.pawnGroupMakers != null)
				foreach (PawnGroupMaker gm in f.def.pawnGroupMakers)
					if (gm.options != null)
						foreach (PawnGenOption o in gm.options)
							if (IsHumanlike(o.kind) && (best == null || o.kind.combatPower > best.combatPower))
								best = o.kind;
			return best;
		}

		public static PawnKindDef HouseLordKind(Faction f)
		{
			string h = HouseOf(f);
			return h == null ? null : DefDatabase<PawnKindDef>.GetNamedSilentFail("DankPyon_" + h + "_LocalLord");
		}

		public static PawnKindDef HouseStandardKind(Faction f)
		{
			string h = HouseOf(f);
			return h == null ? null : DefDatabase<PawnKindDef>.GetNamedSilentFail("DankPyon_Standard" + h);
		}

		public static PawnKindDef HouseKnightKind(Faction f)
		{
			string h = HouseOf(f);
			return h == null ? null : DefDatabase<PawnKindDef>.GetNamedSilentFail("DankPyon_Knight" + h);
		}

		public static PawnKindDef CrewKindFor(Faction f)
		{
			if (f == null) return null;
			if (IsHumanlike(f.def.basicMemberKind)) return f.def.basicMemberKind;
			if (f.def.pawnGroupMakers != null)
				foreach (PawnGroupMaker gm in f.def.pawnGroupMakers)
					if (gm.options != null)
						foreach (PawnGenOption o in gm.options)
							if (IsHumanlike(o.kind)) return o.kind;
			return null;
		}

		private static bool IsHumanlike(PawnKindDef k) => k?.race?.race?.Humanlike ?? false;

		public static Faction GuestFaction()
		{
			Faction f = Find.FactionManager.FirstFactionOfDef(RimFeastDefOf.RimFeast_Guests);
			if (f == null)
			{
				// les vieilles saves n'en ont jamais genere
				FactionGenerator.CreateFactionAndAddToManager(RimFeastDefOf.RimFeast_Guests);
				f = Find.FactionManager.FirstFactionOfDef(RimFeastDefOf.RimFeast_Guests);
			}
			return f;
		}

		public static bool SingleAndFree(Pawn p) =>
			p.relations != null
			&& p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Spouse) == null
			&& p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance) == null
			&& p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover) == null;

		// on garde les verifs vanilla (genre, orientation, age, celibat) mais pas le portail
		// d'opinion: un noble qui vient d'arriver est a 0, la fiction c'est que le banquet
		// a fait le reste. invites generes sans parents, donc rien a tester cote inceste
		public static bool TryFindMarriagePair(Lord cortege, out Pawn noble, out Pawn colonist)
		{
			noble = null;
			colonist = null;
			if (cortege == null) return false;
			Map map = cortege.Map;
			if (map == null) return false;

			var colonists = map.mapPawns.FreeColonistsSpawned
				.Where(col => SingleAndFree(col)
					&& RelationsUtility.RomanceEligible(col, initiator: false, forOpinionExplanation: false).Accepted)
				.ToList();
			if (colonists.Count == 0) return false;

			// le plus haut rang d'abord, pour le panache. le chef de maison avant tout
			foreach (Pawn g in cortege.ownedPawns.OrderByDescending(p =>
				p.Faction?.leader == p ? 99999f : p.kindDef?.combatPower ?? 0f))
			{
				if (g.Dead || !g.Spawned || !g.RaceProps.Humanlike) continue;
				if (!SingleAndFree(g)) continue;
				if (!RelationsUtility.RomanceEligible(g, initiator: true, forOpinionExplanation: false).Accepted) continue;

				foreach (Pawn col in colonists.InRandomOrder())
				{
					if (!RelationsUtility.AttractedToGender(g, col.gender)) continue;
					if (!RelationsUtility.AttractedToGender(col, g.gender)) continue;
					noble = g;
					colonist = col;
					return true;
				}
			}
			return false;
		}

		// equivalent de GatheringsUtility.InGatheringArea sans la requete de pathfinding.
		// vanilla teste "meme piece et atteignable sans ouvrir de porte", or une piece est
		// un ensemble de regions connectees sans porte: le CanReach est redondant
		public struct FeastArea
		{
			private Map map;
			private Room room;
			private IntVec3 spot;
			private bool wholeRoom;

			public static FeastArea For(IntVec3 spot, Map map)
			{
				var a = new FeastArea { map = map, spot = spot };
				a.room = spot.GetRoom(map);
				a.wholeRoom = a.room != null && GatheringsUtility.UseWholeRoomAsGatheringArea(spot, map);
				return a;
			}

			public bool Contains(IntVec3 cell)
			{
				if (room == null) return false;
				if (!wholeRoom && !cell.InHorDistOf(spot, 18f)) return false;
				return cell.GetRoom(map) == room;
			}
		}

		// sans instrument dans la salle, pas de menestrel: le duty le sortirait de table
		// pour rien, et JobGiver_PlayAtFeast le laisserait planter debout a bavarder
		public static bool AnyInstrumentIn(IntVec3 spot, Map map)
		{
			if (!MusicPossible || map == null) return false;
			FeastArea area = FeastArea.For(spot, map);
			foreach (Building_MusicalInstrument b in
				map.listerBuildings.AllBuildingsColonistOfClass<Building_MusicalInstrument>())
				if (area.Contains(b.InteractionCell)) return true;
			return false;
		}

		public static float DrunkennessOf(Pawn p)
		{
			if (AlcoholHigh == null) return 0f;
			return p?.health?.hediffSet?.GetFirstHediffOfDef(AlcoholHigh)?.Severity ?? 0f;
		}

		// vanilla ne compte l'argent que sous une balise orbitale alimentee. en medieval
		// il n'y en a pas: on compte la zone domestique et les stockages
		public static int SilverAvailable(Map map)
		{
			if (map == null) return 0;
			int total = 0;
			List<Thing> silver = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
			for (int i = 0; i < silver.Count; i++)
			{
				Thing t = silver[i];
				if (t.Spawned && !t.Position.Fogged(map)
					&& (map.areaManager.Home[t.Position] || t.IsInAnyStorage()))
					total += t.stackCount;
			}
			return total;
		}

		public static bool TryPaySilver(Map map, int amount)
		{
			if (SilverAvailable(map) < amount) return false;
			List<Thing> silver = map.listerThings.ThingsOfDef(ThingDefOf.Silver)
				.Where(t => t.Spawned && !t.Position.Fogged(map)
					&& (map.areaManager.Home[t.Position] || t.IsInAnyStorage()))
				.ToList();
			foreach (Thing t in silver)
			{
				int take = Mathf.Min(t.stackCount, amount);
				t.SplitOff(take).Destroy();
				amount -= take;
				if (amount <= 0) return true;
			}
			return false;
		}
	}
}
