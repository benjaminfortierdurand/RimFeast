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

		public static GatheringDef RimFeast_Feast;
		public static GatheringDef RimFeast_GuestFeast;

		public static DutyDef RimFeast_ToastDuty;
		public static DutyDef RimFeast_MinstrelDuty;

		public static LetterDef RimFeast_TradePact;
		public static LetterDef RimFeast_MarriageProposal;
		public static LetterDef RimFeast_FeastRequest;

		public static JobDef RimFeast_AssassinateJob;
		public static JobDef RimFeast_SitAtFeastJob;
		public static JobDef RimFeast_EatAtSeatJob;
		public static JobDef RimFeast_DanceJob;
		public static JobDef RimFeast_CongaJob;
		public static JobDef RimFeast_RoundJob;
		public static JobDef RimFeast_SetDanceJob;
		public static JobDef RimFeast_CheerJob;
		public static JobDef RimFeast_ContestJob;
		public static JobDef RimFeast_WatchContestJob;

		public static SongDef RimFeast_RedWeddingSong;
		public static SoundDef RimFeast_RedWeddingPerformance;
		public static SongDef RimFeast_CensoredSong;
		public static SoundDef RimFeast_CensoredPerformance;
		public static SoundDef RimFeast_TavernPerformance1;
		public static SoundDef RimFeast_TavernPerformance2;

		public static ThoughtDef RimFeast_AttendedFeast;
		public static ThoughtDef RimFeast_ColonyFeast;
		public static ThoughtDef RimFeast_Wake;
		public static ThoughtDef RimFeast_VictoryFeast;
		public static ThoughtDef RimFeast_HarvestFeast;
		public static ThoughtDef RimFeast_WonContest;
		public static ThoughtDef RimFeast_DancedWith;
		public static ThoughtDef RimFeast_NightWithNoble;
		public static ThoughtDef RimFeast_GuestRightBroken;

		public static ConceptDef RimFeast_Concept_Guide;

		static RimFeastDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(RimFeastDefOf));
		}
	}
}
