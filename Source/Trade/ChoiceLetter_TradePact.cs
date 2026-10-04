using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimFeast
{
	// la caravane est deja programmee, la lettre ne fait que fixer le marchand
	public class ChoiceLetter_TradePact : ChoiceLetter
	{
		public int pactId;
		public Faction house;

		public override bool CanDismissWithRightClick => false;

		public override IEnumerable<DiaOption> Choices
		{
			get
			{
				GameComponent_FeastState comp = GameComponent_FeastState.Get();
				if (comp != null && comp.PactPending(pactId) && house?.def?.caravanTraderKinds != null)
				{
					foreach (TraderKindDef kind in house.def.caravanTraderKinds)
					{
						TraderKindDef k = kind;
						yield return new DiaOption("RimFeast_PactPick".Translate(k.label))
						{
							// resolveTree ne ferme que le dialogue: sans RemoveLetter on rechoisit a l'infini
							action = delegate
							{
								comp.SetPactKind(pactId, k);
								Find.LetterStack.RemoveLetter(this);
							},
							resolveTree = true,
						};
					}
					yield return Option_Postpone;
					yield break;
				}

				yield return Option_Close;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref pactId, "pactId");
			Scribe_References.Look(ref house, "house");
		}
	}
}
