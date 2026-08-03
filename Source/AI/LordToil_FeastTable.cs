using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimFeast.AI
{
	// la tablee des colons. le menestrel est de la maison qui recoit, pas du cortege
	public class LordToil_ColonistFeast : LordToil_Party
	{
		public LordToil_ColonistFeast(IntVec3 spot, GatheringDef gatheringDef)
			: base(spot, gatheringDef) { }

		public override void UpdateAllDuties()
		{
			base.UpdateAllDuties();
			if (!FeastUtility.AnyInstrumentIn(spot, Map)) return;

			Pawn minstrel = null;
			int best = -1;
			foreach (Pawn p in lord.ownedPawns)
			{
				int lvl = p.skills?.GetSkill(SkillDefOf.Artistic)?.Level ?? -1;
				if (lvl > best) { best = lvl; minstrel = p; }
			}
			if (minstrel != null)
				minstrel.mindState.duty = new PawnDuty(RimFeastDefOf.RimFeast_MinstrelDuty, spot);
		}
	}

	// l'orateur au haut bout, les autres en cercle. duty Spectate vanilla: chez des invites
	// aucun travail ne peut passer devant
	public class LordToil_Toast : LordToil
	{
		private IntVec3 spot;
		private Pawn speaker;

		public LordToil_Toast(IntVec3 spot, Pawn speaker)
		{
			this.spot = spot;
			this.speaker = speaker;
		}

		public override bool AllowSatisfyLongNeeds => false;

		public override void UpdateAllDuties()
		{
			foreach (Pawn p in lord.ownedPawns)
			{
				if (p == speaker)
				{
					p.mindState.duty = new PawnDuty(RimFeastDefOf.RimFeast_ToastDuty, spot);
					continue;
				}
				var duty = new PawnDuty(DutyDefOf.Spectate);
				duty.spectateRect = CellRect.CenteredOn(spot, 0);
				duty.spectateRectAllowedSides = SpectateRectSide.All;
				p.mindState.duty = duty;
			}
		}
	}
}
