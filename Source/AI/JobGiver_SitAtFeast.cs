using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	// attable, chope en main: on reutilise SocialRelax, le flux des tavernes vanilla.
	// le pawn s'assoit, fait face au haut bout, bavarde beaucoup et sirote sa boisson
	public class JobGiver_SitAtFeast : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			PawnDuty duty = pawn.mindState.duty;
			if (duty == null) return null;
			IntVec3 spot = duty.focus.Cell;
			if (!spot.IsValid || pawn.Map == null) return null;

			// cible A = ce qu'on regarde en mangeant: le haut bout de table
			Thing focus = spot.GetFirstBuilding(pawn.Map);
			if (focus == null) return null;

			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(spot, pawn.Map);
			Thing chair = FindChair(pawn, spot, area);
			if (chair == null) return null;

			// la chaise nous donne une case assise libre, c'est elle que le driver reserve
			if (!Toils_Ingest.TryFindFreeSittingSpotOnThing(chair, pawn, out IntVec3 seat))
				seat = chair.Position;

			Job job = JobMaker.MakeJob(RimFeastDefOf.RimFeast_SitAtFeastJob, focus, seat);
			Thing drink = FindDrink(pawn, area);
			if (drink != null)
			{
				job.targetC = drink;
				job.count = 1;
			}
			return job;
		}

		private static Thing FindChair(Pawn pawn, IntVec3 spot, FeastUtility.FeastArea area)
		{
			// meme portee que la zone de banquet: a 9 les tables du fond d'une grande salle
			// etaient invisibles et les convives restaient debout a cote de chaises libres.
			// on paie l'elargissement en testant l'edifice avant la piece, un sol vide sort
			// sur une lecture de grille au lieu d'une recherche de region
			Thing loose = null;
			int cells = GenRadial.NumCellsInRadius(18f);
			for (int i = 0; i < cells; i++)
			{
				IntVec3 c = spot + GenRadial.RadialPattern[i];
				if (!c.InBounds(pawn.Map)) continue;

				Building edifice = c.GetEdifice(pawn.Map);
				if (edifice == null || edifice.def.building == null || !edifice.def.building.isSittable) continue;
				if (!area.Contains(c)) continue;
				if (edifice.IsForbidden(pawn)) continue;
				if (!Toils_Ingest.TryFindFreeSittingSpotOnThing(edifice, pawn, out IntVec3 sitCell)) continue;
				if (!pawn.CanReserveSittableOrSpot(sitCell)) continue;

				// une chaise contre une table, c'est la vraie tablee. sinon on prend ce qu'il y a
				if (BesideTable(edifice)) return edifice;
				if (loose == null) loose = edifice;
			}
			return loose;
		}

		private static bool BesideTable(Building chair)
		{
			foreach (IntVec3 c in GenAdj.CellsAdjacentCardinal(chair))
			{
				if (!c.InBounds(chair.Map)) continue;
				Building b = c.GetEdifice(chair.Map);
				if (b != null && b.def.surfaceType == SurfaceType.Eat) return true;
			}
			return false;
		}

		private static Thing FindDrink(Pawn pawn, FeastUtility.FeastArea area)
		{
			// pas d'alcool aux enfants. les ados restent a table, comme au moyen age
			if (pawn.DevelopmentalStage.Juvenile()) return null;
			if (pawn.IsTeetotaler()) return null;
			if (FeastUtility.DrunkennessOf(pawn) > RimFeastMod.S.drinkLimit) return null;
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
				ThingRequest.ForGroup(ThingRequestGroup.Drug), PathEndMode.OnCell,
				TraverseParms.For(pawn), 14f,
				x => x.IngestibleNow && x.def.IsDrug
					&& x.def.ingestible != null && x.def.ingestible.drugCategory == DrugCategory.Social
					&& !x.IsForbidden(pawn) && area.Contains(x.Position) && pawn.CanReserve(x));
		}
	}
}
