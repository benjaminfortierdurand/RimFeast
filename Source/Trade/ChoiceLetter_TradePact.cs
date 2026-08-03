using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimFeast
{
	// la caravane est deja programmee quand cette lettre part: choisir ne fait que fixer
	// le marchand. ignorer la lettre ne coute donc rien, la maison enverra qui elle veut
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
							// resolveTree ne ferme que le dialogue: sans RemoveLetter la lettre
							// reste sur la pile et on peut rechoisir a l'infini
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

				// deja arrivee, ou l'accord n'existe plus
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
