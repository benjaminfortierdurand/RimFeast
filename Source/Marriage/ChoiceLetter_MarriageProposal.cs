using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimFeast
{
	public class ChoiceLetter_MarriageProposal : ChoiceLetter
	{
		public int caseId;

		public override bool CanDismissWithRightClick => false;

		public override IEnumerable<DiaOption> Choices
		{
			get
			{
				GameComponent_FeastState comp = GameComponent_FeastState.Get();
				if (comp != null && comp.ProposalPending(caseId))
				{
					// RemoveLetter obligatoire, sinon un refus se rouvre et s'accepte
					yield return new DiaOption("RimFeast_ProposalAccept".Translate())
					{
						action = delegate
						{
							comp.AcceptProposal(caseId);
							Find.LetterStack.RemoveLetter(this);
						},
						resolveTree = true,
					};
					yield return new DiaOption("RimFeast_ProposalDecline".Translate())
					{
						action = delegate
						{
							comp.DeclineProposal(caseId);
							Find.LetterStack.RemoveLetter(this);
						},
						resolveTree = true,
					};
					yield return Option_Postpone;
					yield break;
				}

				yield return Option_Close;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref caseId, "caseId");
		}
	}
}
