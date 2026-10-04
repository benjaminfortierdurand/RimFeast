using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimFeast
{
	public class ChoiceLetter_FeastRequest : ChoiceLetter
	{
		public int caseId;

		public override bool CanDismissWithRightClick => false;

		public override IEnumerable<DiaOption> Choices
		{
			get
			{
				GameComponent_FeastState comp = GameComponent_FeastState.Get();
				if (comp != null && comp.RequestPending(caseId))
				{
					// RemoveLetter obligatoire, sinon un refus se rouvre et s'accepte
					yield return new DiaOption(
						(comp.RequestIsBed(caseId) ? "RimFeast_RequestBedAllow" : "RimFeast_RequestAccept")
							.Translate())
					{
						action = delegate
						{
							comp.AcceptRequest(caseId);
							Find.LetterStack.RemoveLetter(this);
						},
						resolveTree = true,
					};
					yield return new DiaOption("RimFeast_RequestRefuse".Translate())
					{
						action = delegate
						{
							comp.RefuseRequest(caseId);
							Find.LetterStack.RemoveLetter(this);
						},
						resolveTree = true,
					};
					if (comp.CanAnswerInSteel(caseId))
						yield return new DiaOption("RimFeast_RequestSteel".Translate())
						{
							action = delegate
							{
								comp.AnswerInSteel(caseId);
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
