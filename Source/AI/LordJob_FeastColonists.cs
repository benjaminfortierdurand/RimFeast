using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimFeast.AI
{
	// la fete cote colons, greffee sur le flux vanilla des parties
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

		// la fete des colons a son propre toil: il designe le menestrel maison
		protected override LordToil CreateGatheringToil(IntVec3 spot, Pawn organizer, GatheringDef gatheringDef)
		{
			return new LordToil_ColonistFeast(spot, gatheringDef);
		}

		// vanilla annule la fete des que l'organisateur la quitte. pour une fete spontanee
		// c'est logique, pour un banquet paye et deja servi c'est absurde: il suffit qu'un
		// mod de besoins (hygiene) l'envoie se soulager pour tout faire tomber. ici le
		// banquet appartient a la colonie, pas a son hote
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

		// plus de dependance a l'organisateur: seules les conditions de la carte comptent
		protected override bool ShouldBeCalledOff() =>
			!GatheringsUtility.AcceptableGameConditionsToContinueGathering(Map);

		// meme souvenir que vanilla, module par le temps reellement passe a table
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

		// pas d'enfants ni de non-violents a une table qu'on a prevu de finir au couteau
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
