using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	// le pendant boisson de JobGiver_EatInGatheringArea, qui refuse les drogues
	public class JobGiver_DrinkAtFeast : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			PawnDuty duty = pawn.mindState.duty;
			if (duty == null) return null;
			// assez bu pour ce soir. les enfants ne boivent pas, les ados oui: on est au
			// moyen age, la petite biere se sert a table des l'adolescence
			if (pawn.DevelopmentalStage.Juvenile()) return null;
			if (pawn.IsTeetotaler()) return null;
			if (FeastUtility.DrunkennessOf(pawn) > RimFeastMod.S.drinkLimit) return null;

			Thing drink = FindDrink(pawn, duty.focus.Cell);
			if (drink == null) return null;
			Job job = JobMaker.MakeJob(JobDefOf.Ingest, drink);
			job.count = 1;
			return job;
		}

		private Thing FindDrink(Pawn pawn, IntVec3 spot)
		{
			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(spot, pawn.Map);
			Predicate<Thing> validator = delegate(Thing x)
			{
				if (!x.IngestibleNow || !x.def.IsDrug) return false;
				if (x.def.ingestible == null || x.def.ingestible.drugCategory != DrugCategory.Social) return false;
				// les rejets gratuits d'abord, le test de zone ensuite
				if (x.IsForbidden(pawn)) return false;
				if (!area.Contains(x.Position)) return false;
				return pawn.CanReserve(x);
			};
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
				ThingRequest.ForGroup(ThingRequestGroup.Drug), PathEndMode.ClosestTouch,
				TraverseParms.For(TraverseMode.NoPassClosedDoors), 14f, validator, null, 0, 12);
		}
	}
}
