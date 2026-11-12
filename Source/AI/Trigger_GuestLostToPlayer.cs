using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RimFeast.AI
{
	// l'hospitalite rompue par l'hote: c'est la guerre. a ajouter avant la transition harmed, qui prend tout le reste.
	// un accident (balle perdue, explosion) n'en est pas: le cortege s'en va, et le jeu compte le coup a la maison
	// comme pour n'importe quel visiteur
	public class Trigger_GuestLostToPlayer : Trigger
	{
		public override bool ActivateOn(Lord lord, TriggerSignal signal)
		{
			if (signal.type != TriggerSignalType.PawnLost) return false;
			// le jeu rend lui-meme la maison hostile quand on capture l'un des siens
			if (signal.condition == PawnLostCondition.MadePrisoner) return true;
			if (signal.condition != PawnLostCondition.Incapped
				&& signal.condition != PawnLostCondition.Killed) return false;
			return Deliberate(signal);
		}

		private static bool Deliberate(TriggerSignal signal)
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp != null && comp.SlaughterUnderway) return true;
			if (!(signal.dinfo.Instigator is Pawn p)) return false;
			if (p.CurJobDef == RimFeastDefOf.RimFeast_AssassinateJob) return true;
			// un colon que le joueur a envoye sur cet invite
			return p.Faction == Faction.OfPlayer && p.CurJob != null && p.CurJob.playerForced
				&& p.CurJob.targetA.Thing == signal.Pawn;
		}
	}
}
