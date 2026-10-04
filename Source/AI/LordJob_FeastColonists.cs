using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimFeast.AI
{
	public class LordJob_FeastColonists : LordJob_Joinable_Party
	{
		private bool trap;

		public LordJob_FeastColonists() { }

		public LordJob_FeastColonists(IntVec3 spot, Pawn organizer, GatheringDef gatheringDef, bool trap)
			: base(spot, organizer, gatheringDef)
		{
			this.trap = trap;
			durationTicks = GameComponent_FeastState.FeastDurationTicks;
		}

		protected override LordToil CreateGatheringToil(IntVec3 spot, Pawn organizer, GatheringDef gatheringDef)
		{
			return new LordToil_ColonistFeast(spot, gatheringDef);
		}

		// vanilla annule la fete si l'organisateur part, un mod d'hygiene suffit a tout faire tomber
		public override StateGraph CreateGraph()
		{
			var graph = new StateGraph();
			LordToil party = CreateGatheringToil(spot, organizer, gatheringDef);
			graph.AddToil(party);
			var end = new LordToil_End();
			graph.AddToil(end);

			var calledOff = new Transition(party, end);
			calledOff.AddTrigger(new Trigger_TickCondition(ShouldBeCalledOff));
			calledOff.AddTrigger(new Trigger_PawnKilled());
			calledOff.AddPreAction(new TransitionAction_Custom((Action)delegate { ApplyFeastOutcome(party); }));
			calledOff.AddPreAction(new TransitionAction_Message(gatheringDef.calledOffMessage,
				MessageTypeDefOf.NegativeEvent, new TargetInfo(spot, Map)));
			graph.AddTransition(calledOff);

			timeoutTrigger = GetTimeoutTrigger();
			var finished = new Transition(party, end);
			finished.AddTrigger(timeoutTrigger);
			finished.AddPreAction(new TransitionAction_Custom((Action)delegate { ApplyFeastOutcome(party); }));
			finished.AddPreAction(new TransitionAction_Message(gatheringDef.finishedMessage,
				MessageTypeDefOf.SituationResolved, new TargetInfo(spot, Map)));
			graph.AddTransition(finished);
			return graph;
		}

		protected override bool ShouldBeCalledOff() =>
			!GatheringsUtility.AcceptableGameConditionsToContinueGathering(Map);

		private void ApplyFeastOutcome(LordToil toil)
		{
			LordToilData_Gathering data = ((LordToil_Gathering)toil).Data;
			if (data == null) return;
			foreach (Pawn p in lord.ownedPawns)
			{
				if (!data.presentForTicks.TryGetValue(p, out int ticks) || ticks <= 0) continue;
				if (p.needs?.mood != null)
				{
					ThoughtDef def = RimFeastDefOf.RimFeast_AttendedFeast;
					float floor = 0.5f / def.stages[0].baseMoodEffect;
					var memory = (Thought_Memory)ThoughtMaker.MakeThought(def);
					memory.moodPowerFactor = Mathf.Min((float)ticks / durationTicks + floor, 1f);
					p.needs.mood.thoughts.memories.TryGainMemory(memory);
				}
				TaleRecorder.RecordTale(TaleDefOf.AttendedParty, p, organizer ?? p);
			}
		}

		public override float VoluntaryJoinPriorityFor(Pawn p)
		{
			if (trap && !FitForTrap(p)) return 0f;
			return base.VoluntaryJoinPriorityFor(p);
		}

		public static bool FitForTrap(Pawn p) =>
			p.DevelopmentalStage.Adult() && !p.WorkTagIsDisabled(WorkTags.Violent);

		protected override ThoughtDef AttendeeThought => RimFeastDefOf.RimFeast_AttendedFeast;

		protected override ThoughtDef OrganizerThought => RimFeastDefOf.RimFeast_AttendedFeast;

		public override string GetReport(Pawn pawn) => "RimFeast_ReportFeasting".Translate();

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref trap, "trap");
		}
	}
}
