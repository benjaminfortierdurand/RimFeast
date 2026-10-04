using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	// sans DLC l'instrument crache une Log.ErrorOnce: pas de job
	public class JobGiver_PlayAtFeast : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!FeastUtility.MusicPossible) return null;

			PawnDuty duty = pawn.mindState.duty;
			if (duty == null) return null;
			IntVec3 spot = duty.focus.Cell;

			FeastUtility.FeastArea area = FeastUtility.FeastArea.For(spot, pawn.Map);
			Building_MusicalInstrument inst = pawn.Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_MusicalInstrument>()
				.Where(i => area.Contains(i.InteractionCell)
					&& GatheringWorker_Concert.InstrumentAccessible(i, pawn))
				.RandomElementWithFallback();
			if (inst == null) return null;

			return JobMaker.MakeJob(JobDefOf.Play_MusicalInstrument, inst, inst.InteractionCell);
		}
	}
}
