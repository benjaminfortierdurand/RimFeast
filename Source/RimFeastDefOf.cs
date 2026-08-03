using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast
{
	[DefOf]
	public static class RimFeastDefOf
	{
		public static FactionDef RimFeast_Guests;

		public static ThingDef RimFeast_FeastSpot;

		public static GatheringDef RimFeast_Feast;      // colons a table
		public static GatheringDef RimFeast_GuestFeast; // cortege invite

		public static DutyDef RimFeast_ToastDuty;
		public static DutyDef RimFeast_MinstrelDuty;

		public static LetterDef RimFeast_TradePact;
		public static LetterDef RimFeast_MarriageProposal;
		public static LetterDef RimFeast_FeastRequest;

		public static JobDef RimFeast_AssassinateJob;
		public static JobDef RimFeast_SitAtFeastJob;

		public static SongDef RimFeast_RedWeddingSong;
		public static SoundDef RimFeast_RedWeddingPerformance;

		public static ThoughtDef RimFeast_AttendedFeast;
		public static ThoughtDef RimFeast_NightWithNoble;
		public static ThoughtDef RimFeast_GuestRightBroken;

		static RimFeastDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(RimFeastDefOf));
		}
	}
}
