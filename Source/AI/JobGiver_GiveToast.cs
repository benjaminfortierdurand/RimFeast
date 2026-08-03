using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast.AI
{
	// le JobGiver vanilla exige un trone assigne (def Royalty, et l'assignation vise les colons).
	// le driver lui gere la cible cellule: TargetThingA null = pas de trone, branches alternatives
	public class JobGiver_GiveToast : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			PawnDuty duty = pawn.mindState.duty;
			if (duty == null) return null;

			IntVec3 cell = duty.focus.Cell;
			if (!cell.IsValid) return null;
			if (pawn.Position != cell && !pawn.CanReach(cell, PathEndMode.OnCell, Danger.None)) return null;

			Job job = JobMaker.MakeJob(JobDefOf.GiveSpeech, cell);

			// il tourne vers B. sa propre case ne ferait rien tourner du tout
			IntVec3 face = cell + IntVec3.North;
			job.SetTarget(TargetIndex.B, face.InBounds(pawn.Map) ? face : cell);

			job.speechSoundMale = SoundDefOf.Speech_Leader_Male;
			job.speechSoundFemale = SoundDefOf.Speech_Leader_Female;
			job.showSpeechBubbles = true;
			return job;
		}
	}
}
