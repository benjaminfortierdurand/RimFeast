using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimFeast.AI
{
	public class LordJob_FeastColonists : LordJob_Joinable_Party
	{
		public const string MemoEnd = "RimFeast_ColonyEnd";

		private bool trap;
		private bool colony;

		public LordJob_FeastColonists() { }

		public LordJob_FeastColonists(IntVec3 spot, Pawn organizer, GatheringDef gatheringDef, bool trap, bool colony = false)
			: base(spot, organizer, gatheringDef)
		{
			this.trap = trap;
			this.colony = colony;
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
			LordToil toast = null;
			if (colony)
			{
				toast = new LordToil_ColonyToast(spot, organizer, (LordToil_FeastParty)party);
				graph.AddToil(toast);
			}

			var calledOff = new Transition(party, end);
			if (toast != null) calledOff.AddSources(toast);
			calledOff.AddTrigger(new Trigger_TickCondition(ShouldBeCalledOff));
			calledOff.AddTrigger(new Trigger_PawnKilled());
			calledOff.AddPreAction(new TransitionAction_Custom((Action)delegate { ApplyFeastOutcome(party, false); }));
			calledOff.AddPreAction(new TransitionAction_Message(gatheringDef.calledOffMessage,
				MessageTypeDefOf.NegativeEvent, new TargetInfo(spot, Map)));
			graph.AddTransition(calledOff);

			timeoutTrigger = GetTimeoutTrigger();
			// toast source aussi, sinon le compteur de duree repart a zero apres chaque toast
			var finished = new Transition(party, end);
			if (toast != null) finished.AddSources(toast);
			finished.AddTrigger(timeoutTrigger);
			// seulement pour le banquet de la colonie: un trigger de plus decalerait les sauvegardes des autres
			if (colony) finished.AddTrigger(new Trigger_Memo(MemoEnd));
			finished.AddPreAction(new TransitionAction_Custom((Action)delegate { ApplyFeastOutcome(party, true); }));
			if (!colony)
				finished.AddPreAction(new TransitionAction_Message(gatheringDef.finishedMessage,
					MessageTypeDefOf.SituationResolved, new TargetInfo(spot, Map)));
			graph.AddTransition(finished);

			if (toast != null)
			{
				var raise = new Transition(party, toast);
				raise.AddTrigger(new Trigger_TickCondition(
					() => lord.ticksInToil >= GameComponent_FeastState.ToastIntervalTicks && SpeakerReady(), 60));
				raise.AddPreAction(new TransitionAction_Custom((Action)delegate
				{
					GameComponent_FeastState.Get()?.Notify_ColonyToast(organizer);
				}));
				raise.AddPostAction(new TransitionAction_EndAllJobs());
				graph.AddTransition(raise);

				var sitDown = new Transition(toast, party);
				sitDown.AddTrigger(new Trigger_TicksPassed(GameComponent_FeastState.ToastDurationTicks));
				sitDown.AddPostAction(new TransitionAction_EndAllJobs());
				sitDown.AddPostAction(new TransitionAction_Custom((Action)delegate
				{
					GameComponent_FeastState.Get()?.Notify_ColonyToastOver();
				}));
				graph.AddTransition(sitDown);
			}
			return graph;
		}

		private bool SpeakerReady() =>
			organizer != null && organizer.Spawned && !organizer.Dead && !organizer.Downed
			&& !organizer.InMentalState && organizer.Awake() && lord.ownedPawns.Contains(organizer);

		protected override bool ShouldBeCalledOff() =>
			!GatheringsUtility.AcceptableGameConditionsToContinueGathering(Map);

		private void ApplyFeastOutcome(LordToil toil, bool finished)
		{
			GameComponent_FeastState comp = colony ? GameComponent_FeastState.Get() : null;
			string report = null;
			int stage = comp?.ColonyFeastStage(out report) ?? 0;
			ThoughtDef def = colony ? comp?.ColonyThought() ?? RimFeastDefOf.RimFeast_ColonyFeast
				: RimFeastDefOf.RimFeast_AttendedFeast;
			LordToilData_Gathering data = ((LordToil_Gathering)toil).Data;
			if (data != null)
				foreach (Pawn p in lord.ownedPawns)
				{
					if (!data.presentForTicks.TryGetValue(p, out int ticks) || ticks <= 0) continue;
					if (p.needs?.mood != null)
					{
						float floor = 0.5f / def.stages[stage].baseMoodEffect;
						var memory = (Thought_Memory)ThoughtMaker.MakeThought(def);
						// deux niveaux differents ne se regroupent pas: sans ca un banquet rapproche s'empilerait
						if (colony)
						{
							memory.SetForcedStage(stage);
							p.needs.mood.thoughts.memories.RemoveMemoriesOfDef(def);
						}
						memory.moodPowerFactor = Mathf.Min((float)ticks / durationTicks + floor, 1f);
						p.needs.mood.thoughts.memories.TryGainMemory(memory);
						if (finished) comp?.ApplyOccasion(p, stage);
					}
					TaleRecorder.RecordTale(TaleDefOf.AttendedParty, p, organizer ?? p);
				}
			comp?.Notify_ColonyFeastEnded(finished ? report : null);
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
			Scribe_Values.Look(ref colony, "colony");
		}
	}
}
