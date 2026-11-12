using System.Collections.Generic;
using Verse.AI;

namespace RimFeast.Jobs
{
	// avant la 2.0 le repas a sa place etait un travail a part. il n'en reste que le nom, pour les sauvegardes
	// faites en plein repas: GameComponent_FeastState.FinalizeInit l'arrete au chargement
	public class JobDriver_EatAtSeat : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield break;
		}
	}
}
