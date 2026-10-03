using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimFeast.AI
{
	// la tablee des colons. le menestrel est de la maison qui recoit, pas du cortege
	// la tablee lit la zone du marqueur, pas celle de vanilla: presence et loisir suivent
	// ce que le joueur a regle
	public class LordToil_FeastParty : LordToil_Party
	{
		private FeastUtility.FeastArea area;
		private int areaTick = -1;

		public LordToil_FeastParty(IntVec3 spot, GatheringDef gatheringDef)
			: base(spot, gatheringDef) { }

		public override void LordToilTick()
		{
			int now = Find.TickManager.TicksGame;
			if (areaTick < 0 || now - areaTick >= 250)
			{
				area = FeastUtility.FeastArea.For(spot, Map);
				areaTick = now;
			}
			List<Pawn> pawns = lord.ownedPawns;
			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn p = pawns[i];
				if (!area.Contains(p.Position)) continue;
				Data.presentForTicks.TryGetValue(p, out int t);
				Data.presentForTicks[p] = t + 1;
				p.needs?.joy?.GainJoy(DefaultJoyPerTick, JoyKindDefOf.Social);
			}
		}
	}

	public class LordToil_ColonistFeast : LordToil_FeastParty
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
