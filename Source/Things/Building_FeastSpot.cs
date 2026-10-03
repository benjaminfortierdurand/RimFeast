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

		// l'apercu scanne la nourriture de la salle: pas a chaque frame d'inspection
		private string cachedPreview;
		private int cachedPreviewTick = -99999;

		// le marqueur est un outil de joueur, pas du mobilier: on doit pouvoir le faire
		// disparaitre d'une belle salle sans perdre sa fonction. il reste selectionnable
		public override void Print(SectionLayer layer)
		{
			if (!hideMarker) base.Print(layer);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref hideMarker, "hideMarker");
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo g in base.GetGizmos()) yield return g;

			yield return new Command_Toggle
			{
				defaultLabel = "RimFeast_ShowMarkerLabel".Translate(),
				defaultDesc = "RimFeast_ShowMarkerDesc".Translate(),
				icon = RimFeastTex.ShowMarker,
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

			// la meme invitation, un autre dessein
			var trap = new Command_Action
			{
				defaultLabel = "RimFeast_TrapGizmoLabel".Translate(),
				defaultDesc = "RimFeast_TrapGizmoDesc".Translate(),
				icon = RimFeastTex.Slaughter,
				action = () => OpenInviteMenu(planned: true),
			};
			if (comp != null && comp.Busy) trap.Disable("RimFeast_InviteDisabledBusy".Translate());
			yield return trap;

			// visible des l'invitation piege, grise tant qu'ils ne sont pas a table:
			// un bouton absent se cherche, un bouton grise s'explique
			// un seul bouton pour le massacre: tu designes la premiere gorge, et c'est elle
			// qui donne le signal. tuer un convive met fin au banquet de toute facon, un
			// assassinat "discret" separe n'aurait rien voulu dire
			FeastCase cur = comp?.CurrentCase;

			// tant que le cortege n'est pas parti, on peut rappeler le messager. les 200
			// d'argent, eux, ne reviendront pas
			if (cur != null && cur.state == FeastCase.Invited && cur.spotThing == this)
				yield return new Command_Action
				{
					defaultLabel = "RimFeast_CancelInviteLabel".Translate(),
					defaultDesc = "RimFeast_CancelInviteDesc".Translate(cur.house?.Name),
					icon = RimFeastTex.Cancel,
					action = () => GameComponent_FeastState.Get()?.CancelInvite(this),
				};

			if (cur != null && cur.planned && cur.spotThing == this)
			{
				// combien des tiens sont deja a table: frapper avant qu'ils arrivent, c'est
				// envoyer un seul bourreau contre tout un cortege
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
				// Leaving compte aussi: la description promet qu'on peut les egorger en chemin
				if (cur.state != FeastCase.Feasting && cur.state != FeastCase.Fleeing
					&& cur.state != FeastCase.Leaving)
					kill.Disable("RimFeast_AssassinateDisabledNotSeated".Translate());
				yield return kill;
			}
		}

		private void OpenInviteMenu(bool planned)
		{
			GameComponent_FeastState comp = GameComponent_FeastState.Get();
			var options = new List<FloatMenuOption>();
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
				// une maison qui te deteste peut venir avec autre chose que des cadeaux
				if (h.PlayerGoodwill <= -10)
					label += " " + "RimFeast_InviteRisky".Translate();
				options.Add(new FloatMenuOption(label,
					delegate
					{
						// deux menus ouverts, ou un debug entre-temps: sans ce test on paie
						// 200 d'argent pour une invitation que StartInvite refusera
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
			if (c == null || c.spotThing != this)
				line = "RimFeast_SpotIdle".Translate();
			else if (c.state == FeastCase.Invited)
			{
				line = "RimFeast_SpotInvited".Translate(c.house?.Name);
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
				line = "RimFeast_SpotTraveling".Translate(c.house?.Name);
			else if (c.state == FeastCase.Fleeing)
				line = "RimFeast_SpotFleeing".Translate(c.house?.Name);
			else if (c.state == FeastCase.Leaving)
				line = "RimFeast_SpotLeaving".Translate(c.house?.Name);
			else
				line = "RimFeast_SpotFeasting".Translate(c.house?.Name)
					+ "\n" + GameComponent_FeastState.HallReport(c);

			// le duc se lit ici toute la soiree, pas seulement dans la lettre d'arrivee:
			// c'est lui qui change le prix de la soiree, et celui d'un massacre
			Pawn head = c != null && c.leaderCame ? c.house?.leader : null;
			if (head != null && c.guests.Contains(head)
				&& c.state != FeastCase.Invited && c.state != FeastCase.Fleeing)
				line += "\n" + "RimFeast_SpotLeaderLine".Translate(head.LabelShortCap);

			string areaLine = GetComp<CompFeastArea>()?.InspectLine();
			if (!areaLine.NullOrEmpty()) line += "\n" + areaLine;

			string wardsLine = GameComponent_FeastState.Get()?.WardsInspect();
			if (!wardsLine.NullOrEmpty()) line += "\n" + wardsLine;
			return s.NullOrEmpty() ? line : s + "\n" + line;
		}
	}
}
