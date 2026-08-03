using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	// les instruments sont du contenu Royalty/Ideology. sans DLC, Building_MusicalInstrument
	// et son driver crachent une Log.ErrorOnce: on ne cree simplement pas le job
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
