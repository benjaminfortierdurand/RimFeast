using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimFeast
{
	// le seigneur s'est leve et a demande quelque chose devant toute la salle. refuser
	// devant temoins coute, alors le choix reste sur la pile jusqu'a decision
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
					// RemoveLetter obligatoire: sans lui un refus se rouvre et s'accepte quand meme
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
					// seulement sur une invitation piege: il vient de se lever devant tout
					// le monde pour demander, c'est le meilleur moment pour lui repondre
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

				// l'arme a brule, le prisonnier est mort, ou le cortege est deja parti
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
