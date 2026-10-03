using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimFeast.AI
{
	// cortege d'invites: arrive sous banniere, festoie, porte des toasts, repart
	public class LordJob_FeastGuests : LordJob
	{
		public const string MemoEndFeast = "RimFeast_EndFeast";
		public const string MemoExposed = "RimFeast_Exposed";

		private int caseId;
		private Faction house;
		private IntVec3 spot;
		private Pawn speaker;

		public LordJob_FeastGuests() { }

		public LordJob_FeastGuests(int caseId, Faction house, IntVec3 spot, Pawn speaker)
		{
			this.caseId = caseId;
			this.house = house;
			this.spot = spot;
			this.speaker = speaker;
		}

		// la grande salle est forcement derriere des portes
		public override bool CanOpenAnyDoor(Pawn p) => true;

		// une rixe de taverne ne rompt pas l'hospitalite: l'assomme reste des notres.
		// meme hook que LordJob_Ritual
		public override bool ShouldRemovePawn(Pawn p, PawnLostCondition reason)
		{
			if (reason == PawnLostCondition.Incapped
				&& (GameComponent_FeastState.Get()?.IsBrawler(caseId, p) ?? false))
				return false;
			return base.ShouldRemovePawn(p, reason);
		}

		public override string GetReport(Pawn pawn) => "RimFeast_ReportFeasting".Translate();

		// un orateur au tapis ne porte pas de toast, et le score ne doit pas en compter
		private bool SpeakerReady() =>
			speaker != null && speaker.Spawned && !speaker.Dead && !speaker.Downed && !speaker.InMentalState;

		public override StateGraph CreateGraph()
		{
			var graph = new StateGraph();
			// la route a deux toils: la marche, et la defense apres un premier coup. le coup
			// arrive avant la mort, donc un convive tue en chemin tombe toujours depuis la
			// defense. sans elle en source, l'egorger sur la route ne coutait rien
			StateGraph road = graph.AttachSubgraph(new LordJob_Travel(spot).CreateGraph());
			LordToil travel = road.StartingToil;
			graph.StartingToil = travel;
			LordToil[] onRoad = road.lordToils.Where(t => t != travel).ToArray();

			var feast = new LordToil_FeastParty(spot, RimFeastDefOf.RimFeast_GuestFeast);
			graph.AddToil(feast);
			var toast = new LordToil_Toast(spot, speaker);
			graph.AddToil(toast);
			var exit = new LordToil_ExitMap(LocomotionUrgency.Walk, canDig: false);
			graph.AddToil(exit);
			var flee = new LordToil_ExitMap(LocomotionUrgency.Sprint, canDig: false);
			graph.AddToil(flee);
			var fightOut = new LordToil_FightOut();
			graph.AddToil(fightOut);
			var takeWounded = new LordToil_TakeWoundedGuest();
			graph.AddToil(takeWounded);

			var arrive = new Transition(travel, feast);
			arrive.AddTrigger(new Trigger_Memo("TravelArrived"));
			arrive.AddPostAction(new TransitionAction_Custom((System.Action)delegate
			{
				GameComponent_FeastState.Get()?.Notify_GuestsArrived(caseId);
			}));
			graph.AddTransition(arrive);

			// le graph garde la meme forme quoi qu'il arrive: le lord reapplique les
			// TriggerData par index au chargement, une geometrie variable les decalerait
			var raise = new Transition(feast, toast);
			raise.AddTrigger(new Trigger_TickCondition(
				() => lord.ticksInToil >= GameComponent_FeastState.ToastIntervalTicks && SpeakerReady(), 60));
			raise.AddPreAction(new TransitionAction_Custom((System.Action)delegate
			{
				GameComponent_FeastState.Get()?.Notify_ToastGiven(caseId);
			}));
			raise.AddPostAction(new TransitionAction_EndAllJobs());
			graph.AddTransition(raise);

			var sitDown = new Transition(toast, feast);
			sitDown.AddTrigger(new Trigger_TicksPassed(GameComponent_FeastState.ToastDurationTicks));
			sitDown.AddPostAction(new TransitionAction_EndAllJobs());
			graph.AddTransition(sitDown);

			// toast doit etre source ici: sinon le compteur se remet a zero a chaque retour
			// a la tablee et le banquet ne finit jamais
			var done = new Transition(feast, exit);
			done.AddSources(toast);
			done.AddTrigger(new Trigger_TicksPassed(GameComponent_FeastState.FeastDurationTicks));
			done.AddTrigger(new Trigger_Memo(MemoEndFeast));
			done.AddPreAction(new TransitionAction_Custom((System.Action)delegate
			{
				GameComponent_FeastState.Get()?.Notify_FeastEnded(caseId);
			}));
			done.AddPostAction(new TransitionAction_WakeAll());
			graph.AddTransition(done);

			// en partant ils ramassent leurs blesses
			var wounded = new Transition(exit, takeWounded);
			wounded.AddTrigger(new Trigger_WoundedGuestPresent());
			wounded.AddPreAction(new TransitionAction_Message(
				"RimFeast_MessageTakingWounded".Translate(house?.Name), MessageTypeDefOf.NeutralEvent));
			graph.AddTransition(wounded);

			// le droit de l'hote court jusqu'a la derniere case. le toil flee n'est pas
			// source: massacrer des empoisonneurs demasques reste gratuit
			var betrayedLeaving = new Transition(exit, fightOut);
			betrayedLeaving.AddSources(takeWounded);
			betrayedLeaving.AddTrigger(new Trigger_GuestLostToPlayer());
			betrayedLeaving.AddPostAction(new TransitionAction_Custom((System.Action)delegate
			{
				GameComponent_FeastState.Get()?.Notify_GuestsBetrayed(caseId);
			}));
			betrayedLeaving.AddPostAction(new TransitionAction_WakeAll());
			betrayedLeaving.AddPostAction(new TransitionAction_EndAllJobs());
			graph.AddTransition(betrayedLeaving);

			// empoisonneurs demasques: le cortege detale sans demander son reste
			var caught = new Transition(feast, flee);
			caught.AddSources(toast);
			caught.AddTrigger(new Trigger_Memo(MemoExposed));
			caught.AddPostAction(new TransitionAction_WakeAll());
			caught.AddPostAction(new TransitionAction_EndAllJobs());
			graph.AddTransition(caught);

			// la main du joueur sur un convive: on degaine et on se bat vers la sortie.
			// ajoutee avant harmed, le premier match dans le graph gagne
			var betrayed = new Transition(travel, fightOut);
			betrayed.AddSources(feast, toast);
			betrayed.AddSources(onRoad);
			betrayed.AddTrigger(new Trigger_GuestLostToPlayer());
			betrayed.AddPostAction(new TransitionAction_Custom((System.Action)delegate
			{
				GameComponent_FeastState.Get()?.Notify_GuestsBetrayed(caseId);
			}));
			betrayed.AddPostAction(new TransitionAction_WakeAll());
			betrayed.AddPostAction(new TransitionAction_EndAllJobs());
			graph.AddTransition(betrayed);

			// du sang par une autre main. les exits comptent pas
			var harmed = new Transition(travel, exit);
			harmed.AddSources(feast, toast);
			harmed.AddSources(onRoad);
			harmed.AddTrigger(new Trigger_PawnLost(PawnLostCondition.Incapped));
			harmed.AddTrigger(new Trigger_PawnLost(PawnLostCondition.Killed));
			harmed.AddTrigger(new Trigger_PawnLost(PawnLostCondition.MadePrisoner));
			harmed.AddPostAction(new TransitionAction_Custom((System.Action)delegate
			{
				GameComponent_FeastState.Get()?.Notify_GuestsHarmed(caseId);
			}));
			harmed.AddPostAction(new TransitionAction_EndAllJobs());
			graph.AddTransition(harmed);

			var weather = new Transition(travel, exit);
			weather.AddSources(feast, toast);
			weather.AddSources(onRoad);
			weather.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
			weather.AddPreAction(new TransitionAction_Message("RimFeast_MessageGuestsLeaveWeather".Translate(house?.Name)));
			weather.AddPreAction(new TransitionAction_Custom((System.Action)delegate
			{
				GameComponent_FeastState.Get()?.Notify_FeastEnded(caseId);
			}));
			weather.AddPostAction(new TransitionAction_EndAllJobs());
			graph.AddTransition(weather);

			// backstop pour ne jamais squatter la carte. le compteur court depuis l'arrivee,
			// trajet compris, donc il doit suivre le reglage de duree et pas rester cable
			var stale = new Transition(travel, exit);
			stale.AddSources(feast, toast);
			stale.AddSources(onRoad);
			stale.AddTrigger(new Trigger_TicksPassed(
				GameComponent_FeastState.FeastDurationTicks + 30000));
			stale.AddPreAction(new TransitionAction_Custom((System.Action)delegate
			{
				GameComponent_FeastState.Get()?.Notify_FeastEnded(caseId);
			}));
			graph.AddTransition(stale);

			return graph;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref caseId, "caseId");
			Scribe_References.Look(ref house, "house");
			Scribe_Values.Look(ref spot, "spot");
			Scribe_References.Look(ref speaker, "speaker");
		}
	}

	// trahis a table: ils gagnent la sortie en rendant les coups. le composant bascule la
	// faction invitee en hostile le temps du combat, sinon le ciblage ignore des neutres
	public class LordToil_FightOut : LordToil
	{
		public override bool AllowSatisfyLongNeeds => false;

		public override void UpdateAllDuties()
		{
			foreach (Pawn p in lord.ownedPawns)
				p.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBestAndDefendSelf)
				{
					locomotion = LocomotionUrgency.Jog,
				};
		}
	}
}
