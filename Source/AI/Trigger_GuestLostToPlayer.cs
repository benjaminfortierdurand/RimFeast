using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RimFeast.AI
{
	// perte causee par la main du joueur. a ajouter avant la transition harmed
	public class Trigger_GuestLostToPlayer : Trigger
	{
		public override bool ActivateOn(Lord lord, TriggerSignal signal)
		{
			if (signal.type != TriggerSignalType.PawnLost) return false;
			if (signal.condition != PawnLostCondition.Incapped
				&& signal.condition != PawnLostCondition.Killed
				&& signal.condition != PawnLostCondition.MadePrisoner) return false;

			if (signal.condition == PawnLostCondition.MadePrisoner) return true;

			// un massacre commande ne s'excuse ni par une bataille alentour ni par la victime
			if (!Deliberate(signal))
			{
				if (lord.Map != null && GenHostility.AnyHostileActiveThreatToPlayer(lord.Map)) return false;
				if (signal.Pawn?.MentalStateDef?.IsAggro == true) return false;
			}

			// tourelles et pieges engagent le joueur autant que ses pawns
			return signal.dinfo.Instigator?.Faction == Faction.OfPlayer;
		}

		private static bool Deliberate(TriggerSignal signal)
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp != null && comp.SlaughterUnderway) return true;
			return signal.dinfo.Instigator is Pawn p
				&& p.CurJobDef == RimFeastDefOf.RimFeast_AssassinateJob;
		}
	}
}
