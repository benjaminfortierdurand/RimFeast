using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimFeast
{
	public class Building_FeastSpot : Building
	{
		private bool hideMarker;

		private string cachedPreview;
		private int cachedPreviewTick = -99999;

		public override void Print(SectionLayer layer)
		{
			if (!hideMarker) base.Print(layer);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref hideMarker, "hideMarker");
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad && Faction == Faction.OfPlayer) GameComponent_FeastState.Get()?.OfferGuide(this);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo g in base.GetGizmos()) yield return g;
			if (Faction == Faction.OfPlayer)
				LessonAutoActivator.TeachOpportunity(RimFeastDefOf.RimFeast_Concept_Guide, OpportunityType.GoodToKnow);

			yield return new Command_Action
			{
				defaultLabel = "RimFeast_GuideGizmoLabel".Translate(),
				defaultDesc = "RimFeast_GuideGizmoDesc".Translate(),
				icon = TexButton.Info,
				action = () => Window_FeastGuide.Open(),
			};

			yield return new Command_Toggle
			{
				defaultLabel = "RimFeast_ShowMarkerLabel".Translate(),
				defaultDesc = "RimFeast_ShowMarkerDesc".Translate(),
				icon = TexCommand.GatherSpotActive,
				isActive = () => !hideMarker,
				toggleAction = delegate
				{
					hideMarker = !hideMarker;
					if (Spawned) Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
				},
			};

			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			var cmd = new Command_Action
			{
				defaultLabel = "RimFeast_InviteGizmoLabel".Translate(),
				defaultDesc = "RimFeast_InviteGizmoDesc".Translate(GameComponent_FeastState.BaseInviteCost),
				icon = RimFeastTex.Invite,
				action = () => OpenInviteMenu(planned: false),
			};
			if (comp != null && comp.Busy) cmd.Disable("RimFeast_InviteDisabledBusy".Translate());
			yield return cmd;

			var trap = new Command_Action
			{
				defaultLabel = "RimFeast_TrapGizmoLabel".Translate(),
				defaultDesc = "RimFeast_TrapGizmoDesc".Translate(),
				icon = RimFeastTex.Slaughter,
				action = () => OpenInviteMenu(planned: true),
			};
			if (comp != null && comp.Busy) trap.Disable("RimFeast_InviteDisabledBusy".Translate());
			yield return trap;

			var party = new Command_Action
			{
				defaultLabel = "RimFeast_ColonyGizmoLabel".Translate(),
				defaultDesc = "RimFeast_ColonyGizmoDesc".Translate(RimFeastMod.S.colonyFeastDays),
				icon = RimFeastTex.ColonyFeast,
				action = OpenColonyMenu,
			};
			string blocked = comp?.ColonyGatherBlocked(Map);
			if (blocked != null) party.Disable(blocked);
			yield return party;

			FeastCase cur = comp?.CurrentCase;

			if (cur != null && cur.state == FeastCase.Invited && cur.spotThing == this && !cur.wedding)
				yield return new Command_Action
				{
					defaultLabel = "RimFeast_CancelInviteLabel".Translate(),
					defaultDesc = "RimFeast_CancelInviteDesc".Translate(GameComponent_FeastState.GuestsName(cur)),
					icon = RimFeastTex.Cancel,
					action = () => GameComponent_FeastState.Get()?.CancelInvite(this),
				};

			if (cur != null && cur.planned && cur.spotThing == this)
			{
				int atTable = cur.colonistLord != null
					&& Map.lordManager.lords.Contains(cur.colonistLord)
					? cur.colonistLord.ownedPawns.Count : 0;

				var kill = new Command_Target
				{
					defaultLabel = "RimFeast_StartKillingsLabel".Translate(),
					defaultDesc = "RimFeast_StartKillingsDesc".Translate()
						+ "\n\n" + "RimFeast_KillingsReady".Translate(atTable, cur.guestCount),
					icon = TexCommand.Draft,
					targetingParams = new TargetingParameters
					{
						canTargetPawns = true,
						canTargetBuildings = false,
						validator = t => t.Thing is Pawn v && !v.Dead
							&& (cur.lord?.ownedPawns.Contains(v) ?? false),
					},
					action = t => GameComponent_FeastState.Get()?.OrderSlaughter((Pawn)t.Thing),
				};
				// Leaving compte aussi: on peut les egorger en chemin
				if (cur.state != FeastCase.Feasting && cur.state != FeastCase.Fleeing
					&& cur.state != FeastCase.Leaving)
					kill.Disable("RimFeast_AssassinateDisabledNotSeated".Translate());
				yield return kill;
			}
		}

		// le banquet ordinaire et les trois occasions. une occasion indisponible reste affichee, grisee, avec sa raison
		private void OpenColonyMenu()
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			if (comp == null) return;
			var options = new List<FloatMenuOption>();

			string plain = comp.ColonyFeastBlocked(Map);
			options.Add(plain == null
				? new FloatMenuOption("RimFeast_OccasionPlain".Translate(), () => comp.StartColonyFeast(this))
				: new FloatMenuOption("RimFeast_OccasionPlain".Translate() + " (" + plain + ")", null));

			List<Pawn> mourned = comp.Mourned(Map);
			if (mourned.Count == 0) options.Add(new FloatMenuOption("RimFeast_OccasionWakeNone".Translate(), null));
			foreach (Pawn dead in mourned)
			{
				Pawn d = dead;
				options.Add(new FloatMenuOption("RimFeast_OccasionWake".Translate(d.LabelShortCap),
					() => comp.StartColonyFeast(this, GameComponent_FeastState.OccasionWake, d)));
			}

			string victory = comp.VictoryBlocked(Map);
			options.Add(victory == null
				? new FloatMenuOption("RimFeast_OccasionVictory".Translate(),
					() => comp.StartColonyFeast(this, GameComponent_FeastState.OccasionVictory))
				: new FloatMenuOption(victory, null));

			string harvest = comp.HarvestBlocked(Map);
			options.Add(harvest == null
				? new FloatMenuOption("RimFeast_OccasionHarvest".Translate(),
					() => comp.StartColonyFeast(this, GameComponent_FeastState.OccasionHarvest))
				: new FloatMenuOption(harvest, null));

			Find.WindowStack.Add(new FloatMenu(options));
		}

		// le grand banquet: toutes les maisons d'un coup, en tete du menu d'invitation. confirmation avant de payer
		private FloatMenuOption GrandOption(GameComponent_FeastState comp, bool planned)
		{
			string blocked = comp.GrandBlocked(Map, out List<Faction> houses, out int cost);
			if (blocked != null) return new FloatMenuOption("RimFeast_GrandOptionBlocked".Translate(blocked), null);
			return new FloatMenuOption("RimFeast_GrandOption".Translate(houses.Count, cost), delegate
			{
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(comp.GrandConfirmText(houses, cost, planned),
					delegate
					{
						if (GameComponent_FeastState.Get()?.Busy ?? true)
						{
							Messages.Message("RimFeast_InviteDisabledBusy".Translate().CapitalizeFirst(),
								MessageTypeDefOf.RejectInput, historical: false);
							return;
						}
						if (!FeastUtility.TryPaySilver(Map, cost))
						{
							Messages.Message("RimFeast_NotEnoughSilver".Translate(cost),
								MessageTypeDefOf.RejectInput, historical: false);
							return;
						}
						GameComponent_FeastState.Get()?.StartGrandInvite(Map, this, planned);
					}));
			});
		}

		private void OpenInviteMenu(bool planned)
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			var options = new List<FloatMenuOption>();
			if (comp != null) options.Add(GrandOption(comp, planned));
			foreach (Faction house in FeastUtility.InvitableHouses())
			{
				Faction h = house;
				if (comp != null && comp.HouseRefuses(h, out string refusal))
				{
					options.Add(new FloatMenuOption("RimFeast_InviteOptionRefused".Translate(h.Name, refusal), null));
					continue;
				}
				int cost = comp?.InviteCostFor(h) ?? GameComponent_FeastState.BaseInviteCost;
				int been = comp?.FeastsRemembered(h) ?? 0;
				string label = been > 0
					? "RimFeast_InviteOptionAgain".Translate(h.Name, h.PlayerGoodwill, cost, been)
					: "RimFeast_InviteOption".Translate(h.Name, h.PlayerGoodwill, cost);
				if (h.PlayerGoodwill <= -10)
					label += " " + "RimFeast_InviteRisky".Translate();
				options.Add(new FloatMenuOption(label,
					delegate
					{
						// deux menus ouverts: on paierait pour une invitation refusee
						if (GameComponent_FeastState.Get()?.Busy ?? true)
						{
							Messages.Message("RimFeast_InviteDisabledBusy".Translate().CapitalizeFirst(),
								MessageTypeDefOf.RejectInput, historical: false);
							return;
						}
						if (!FeastUtility.TryPaySilver(Map, cost))
						{
							Messages.Message("RimFeast_NotEnoughSilver".Translate(cost),
								MessageTypeDefOf.RejectInput, historical: false);
							return;
						}
						GameComponent_FeastState.Get()?.StartInvite(Map, h, this, planned);
					}));
			}
			if (options.Count == 0)
				options.Add(new FloatMenuOption("RimFeast_InviteDisabledNoHouses".Translate(), null));
			Find.WindowStack.Add(new FloatMenu(options));
		}

		public override string GetInspectString()
		{
			string s = base.GetInspectString();
			FeastCase c = GameComponent_FeastState.Get()?.CurrentCase;
			string line;
			if (hideMarker) s = (s.NullOrEmpty() ? "" : s + "\n") + "RimFeast_MarkerHidden".Translate();
			FeastCase colonyCase = GameComponent_FeastState.Get()?.ColonyCase;
			if (c == null || c.spotThing != this)
				line = colonyCase != null && colonyCase.spotThing == this
					? GameComponent_FeastState.Get().ColonySpotLine()
					: (string)"RimFeast_SpotIdle".Translate();
			else if (c.state == FeastCase.Invited)
			{
				line = (c.wedding ? "RimFeast_SpotWeddingInvited" : "RimFeast_SpotInvited").Translate(GameComponent_FeastState.GuestsName(c));
				string eta = GameComponent_FeastState.ArrivalEta(c);
				if (!eta.NullOrEmpty()) line += "\n" + eta;
				if (Find.TickManager.TicksGame - cachedPreviewTick > 250)
				{
					cachedPreview = GameComponent_FeastState.HallPreview(c);
					cachedPreviewTick = Find.TickManager.TicksGame;
				}
				if (!cachedPreview.NullOrEmpty()) line += "\n" + cachedPreview;
			}
			else if (c.state == FeastCase.Traveling)
				line = "RimFeast_SpotTraveling".Translate(GameComponent_FeastState.GuestsName(c));
			else if (c.state == FeastCase.Fleeing)
				line = "RimFeast_SpotFleeing".Translate(GameComponent_FeastState.GuestsName(c));
			else if (c.state == FeastCase.Leaving)
				line = "RimFeast_SpotLeaving".Translate(GameComponent_FeastState.GuestsName(c));
			else
				line = "RimFeast_SpotFeasting".Translate(GameComponent_FeastState.GuestsName(c))
					+ "\n" + GameComponent_FeastState.HallReport(c);

			Pawn head = c != null && c.leaderCame ? c.house?.leader : null;
			if (head != null && c.guests.Contains(head)
				&& c.state != FeastCase.Invited && c.state != FeastCase.Fleeing)
				line += "\n" + "RimFeast_SpotLeaderLine".Translate(head.LabelShortCap);

			string areaLine = GetComp<CompFeastArea>()?.InspectLine();
			if (!areaLine.NullOrEmpty()) line += "\n" + areaLine;

			string wardsLine = GameComponent_FeastState.Get()?.WardsInspect();
			if (!wardsLine.NullOrEmpty()) line += "\n" + wardsLine;
			string weddingLine = GameComponent_FeastState.Get()?.WeddingsInspect();
			if (!weddingLine.NullOrEmpty()) line += "\n" + weddingLine;
			return s.NullOrEmpty() ? line : s + "\n" + line;
		}
	}
}
