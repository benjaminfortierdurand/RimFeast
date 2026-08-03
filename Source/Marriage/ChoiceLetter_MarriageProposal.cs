using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimFeast
{
	// accepter, c'est perdre la colon pour toujours: le choix reste sur la pile jusqu'a decision
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
					// RemoveLetter obligatoire: sinon un refus n'est pas definitif, on peut
					// rouvrir la lettre et accepter quand meme
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

				// le cortege est parti ou l'un des promis n'est plus la
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
