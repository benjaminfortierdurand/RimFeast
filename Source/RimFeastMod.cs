using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimFeast
{
	public class RimFeastSettings : ModSettings
	{
		public int inviteCost = 200;
		public int giftSilver = 400;
		public int feastHours = 8;
		public int corteges = 6;
		public int colonyFeastDays = 7;

		public bool summonColonists = true;
		public bool seatMeals = true;
		public bool leaderEnabled = true;
		public bool marriageEnabled = true;
		public float proposalChance = 0.02f;
		public bool tradePactEnabled = true;
		public bool requestsEnabled = true;
		public float requestChance = 0.35f;

		public bool awayFeastsEnabled = true;
		public int awayFeastDays = 20;
		public int awayInviteWindowDays = 12;
		public int awayMaxTravelDays = 30;
		public float awayRiskFactor = 1f;

		// vanilla: 0.25 = eméché, 0.4 = ivre
		public float drinkLimit = 0.35f;
		public float brawlChance = 0.03f;

		public bool treacheryEnabled = true;
		public float treacheryChance = 0.35f;
		public int treacheryGoodwillMax = -10;

		public int fadeDays = 20;
		public int redWeddingDays = 60;
		public bool censoredMusic;
		public bool neutralWording;
		public bool misfortune;
		public bool tavernMusic = true;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref inviteCost, "inviteCost", 200);
			Scribe_Values.Look(ref giftSilver, "giftSilver", 400);
			Scribe_Values.Look(ref feastHours, "feastHours", 8);
			Scribe_Values.Look(ref corteges, "corteges", 6);
			Scribe_Values.Look(ref colonyFeastDays, "colonyFeastDays", 7);
			Scribe_Values.Look(ref summonColonists, "summonColonists", true);
			Scribe_Values.Look(ref seatMeals, "seatMeals", true);
			Scribe_Values.Look(ref leaderEnabled, "leaderEnabled", true);
			Scribe_Values.Look(ref marriageEnabled, "marriageEnabled", true);
			Scribe_Values.Look(ref proposalChance, "proposalChance", 0.02f);
			Scribe_Values.Look(ref tradePactEnabled, "tradePactEnabled", true);
			Scribe_Values.Look(ref requestsEnabled, "requestsEnabled", true);
			Scribe_Values.Look(ref requestChance, "requestChance", 0.35f);
			Scribe_Values.Look(ref awayFeastsEnabled, "awayFeastsEnabled", true);
			Scribe_Values.Look(ref awayFeastDays, "awayFeastDays", 20);
			Scribe_Values.Look(ref awayInviteWindowDays, "awayInviteWindowDays", 12);
			Scribe_Values.Look(ref awayMaxTravelDays, "awayMaxTravelDays", 30);
			Scribe_Values.Look(ref awayRiskFactor, "awayRiskFactor", 1f);
			Scribe_Values.Look(ref drinkLimit, "drinkLimit", 0.35f);
			Scribe_Values.Look(ref brawlChance, "brawlChance", 0.03f);
			Scribe_Values.Look(ref treacheryEnabled, "treacheryEnabled", true);
			Scribe_Values.Look(ref treacheryChance, "treacheryChance", 0.35f);
			Scribe_Values.Look(ref treacheryGoodwillMax, "treacheryGoodwillMax", -10);
			Scribe_Values.Look(ref fadeDays, "fadeDays", 20);
			Scribe_Values.Look(ref redWeddingDays, "redWeddingDays", 60);
			Scribe_Values.Look(ref censoredMusic, "censoredMusic", false);
			Scribe_Values.Look(ref neutralWording, "neutralWording", false);
			Scribe_Values.Look(ref misfortune, "misfortune", false);
			Scribe_Values.Look(ref tavernMusic, "tavernMusic", true);
		}
	}

	public class RimFeastMod : Mod
	{
		private static RimFeastSettings settings;

		// sans vue defilante, Listing bascule en seconde colonne et cache la suite
		private static Vector2 scrollPos;
		private static float viewHeight = 1200f;

		// jamais null, meme si le Mod n'a pas ete instancie
		public static RimFeastSettings S
		{
			get
			{
				if (settings == null)
					settings = LoadedModManager.GetMod<RimFeastMod>()?.GetSettings<RimFeastSettings>()
						?? new RimFeastSettings();
				return settings;
			}
		}

		public RimFeastMod(ModContentPack content) : base(content)
		{
			settings = GetSettings<RimFeastSettings>();
			// un patch qui ne trouve plus sa cible apres une mise a jour du jeu ne doit pas emporter les reglages avec lui
			try { new Harmony("forti.rimfeast").PatchAll(); }
			catch (Exception e) { Log.Error("RimFeast: Harmony patch failed, colonists will not eat at their own seat. " + e); }
		}

		public override string SettingsCategory() => "RimFeast";

		// a la fermeture de la fenetre: le vocabulaire suit la case tout de suite
		public override void WriteSettings()
		{
			base.WriteSettings();
			Wording.Apply();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			// l'instance que WriteSettings ecrira, sinon la coche disparait a la fermeture
			settings = GetSettings<RimFeastSettings>();

			var l = new Listing_Standard();
			var view = new Rect(0f, 0f, inRect.width - 24f, viewHeight);
			Widgets.BeginScrollView(inRect, ref scrollPos, view);
			l.maxOneColumn = true;
			l.Begin(view);

			if (l.ButtonText("RimFeast_SetOpenGuide".Translate())) Window_FeastGuide.Open();
			l.Gap(6f);
			l.Label("RimFeast_SetInviteCost".Translate(S.inviteCost));
			S.inviteCost = Mathf.RoundToInt(l.Slider(S.inviteCost, 0f, 1000f) / 25f) * 25;
			l.Label("RimFeast_SetGift".Translate(S.giftSilver));
			S.giftSilver = Mathf.RoundToInt(l.Slider(S.giftSilver, 0f, 2000f) / 50f) * 50;
			l.Label("RimFeast_SetHours".Translate(S.feastHours));
			S.feastHours = Mathf.RoundToInt(l.Slider(S.feastHours, 2f, 24f));
			l.Label("RimFeast_SetColonyDays".Translate(S.colonyFeastDays));
			S.colonyFeastDays = Mathf.RoundToInt(l.Slider(S.colonyFeastDays, 1f, 30f));
			l.Label("RimFeast_SetCortege".Translate(S.corteges, Mathf.Max(2, S.corteges - 3), S.corteges + 3));
			S.corteges = Mathf.RoundToInt(l.Slider(S.corteges, 3f, 16f));

			l.GapLine();
			l.CheckboxLabeled("RimFeast_SetSummon".Translate(), ref S.summonColonists,
				"RimFeast_SetSummonTip".Translate());
			l.CheckboxLabeled("RimFeast_SetSeatMeals".Translate(), ref S.seatMeals,
				"RimFeast_SetSeatMealsTip".Translate());
			l.CheckboxLabeled("RimFeast_SetLeader".Translate(), ref S.leaderEnabled,
				"RimFeast_SetLeaderTip".Translate());
			l.CheckboxLabeled("RimFeast_SetTradePact".Translate(), ref S.tradePactEnabled,
				"RimFeast_SetTradePactTip".Translate());
			l.CheckboxLabeled("RimFeast_SetRequests".Translate(), ref S.requestsEnabled,
				"RimFeast_SetRequestsTip".Translate());
			if (S.requestsEnabled)
			{
				l.Label("RimFeast_SetRequestChance".Translate(Mathf.RoundToInt(S.requestChance * 100f)));
				S.requestChance = Mathf.Round(l.Slider(S.requestChance, 0f, 1f) * 20f) / 20f;
			}
			l.CheckboxLabeled("RimFeast_SetAwayFeasts".Translate(), ref S.awayFeastsEnabled,
				"RimFeast_SetAwayFeastsTip".Translate());
			if (S.awayFeastsEnabled)
			{
				l.Label("RimFeast_SetAwayDays".Translate(S.awayFeastDays));
				S.awayFeastDays = Mathf.RoundToInt(l.Slider(S.awayFeastDays, 5f, 60f));
				l.Label("RimFeast_SetAwayWindow".Translate(S.awayInviteWindowDays));
				S.awayInviteWindowDays = Mathf.RoundToInt(l.Slider(S.awayInviteWindowDays, 3f, 30f));
				l.Label("RimFeast_SetAwayMaxTravel".Translate(S.awayMaxTravelDays));
				S.awayMaxTravelDays = Mathf.RoundToInt(l.Slider(S.awayMaxTravelDays, 2f, 30f));
				l.Label("RimFeast_SetAwayRisk".Translate(Mathf.RoundToInt(S.awayRiskFactor * 100f)));
				S.awayRiskFactor = Mathf.Round(l.Slider(S.awayRiskFactor, 0f, 2f) * 20f) / 20f;
			}
			l.CheckboxLabeled("RimFeast_SetMarriage".Translate(), ref S.marriageEnabled,
				"RimFeast_SetMarriageTip".Translate());
			if (S.marriageEnabled)
			{
				l.Label("RimFeast_SetProposalChance".Translate((S.proposalChance * 100f).ToString("0.0")));
				S.proposalChance = Mathf.Round(l.Slider(S.proposalChance, 0f, 0.1f) * 1000f) / 1000f;
			}

			l.GapLine();
			l.Label("RimFeast_SetDrinkLimit".Translate(Mathf.RoundToInt(S.drinkLimit * 100f),
				("RimFeast_DrunkStage" + (S.drinkLimit >= 0.4f ? "Drunk"
					: S.drinkLimit >= 0.25f ? "Tipsy" : "Warm")).Translate()));
			S.drinkLimit = Mathf.Round(l.Slider(S.drinkLimit, 0f, 1f) * 20f) / 20f;
			l.Label("RimFeast_SetBrawlChance".Translate((S.brawlChance * 100f).ToString("0.0")));
			S.brawlChance = Mathf.Round(l.Slider(S.brawlChance, 0f, 0.15f) * 1000f) / 1000f;
			l.CheckboxLabeled("RimFeast_SetTreachery".Translate(), ref S.treacheryEnabled,
				"RimFeast_SetTreacheryTip".Translate());
			if (S.treacheryEnabled)
			{
				l.Label("RimFeast_SetTreacheryChance".Translate(Mathf.RoundToInt(S.treacheryChance * 100f)));
				S.treacheryChance = Mathf.Round(l.Slider(S.treacheryChance, 0f, 1f) * 20f) / 20f;
				l.Label("RimFeast_SetTreacheryGoodwill".Translate(S.treacheryGoodwillMax));
				S.treacheryGoodwillMax = Mathf.RoundToInt(l.Slider(S.treacheryGoodwillMax, -100f, 0f) / 5f) * 5;
			}

			l.CheckboxLabeled("RimFeast_SetTavernMusic".Translate(), ref S.tavernMusic,
				"RimFeast_SetTavernMusicTip".Translate());
			l.CheckboxLabeled("RimFeast_SetCensoredMusic".Translate(), ref S.censoredMusic,
				"RimFeast_SetCensoredMusicTip".Translate());
			l.CheckboxLabeled("RimFeast_SetNeutral".Translate(), ref S.neutralWording,
				"RimFeast_SetNeutralTip".Translate());

			l.GapLine();
			l.Label("RimFeast_SetFadeDays".Translate(S.fadeDays));
			S.fadeDays = Mathf.RoundToInt(l.Slider(S.fadeDays, 1f, 60f));
			l.CheckboxLabeled("RimFeast_SetMisfortune".Translate(), ref S.misfortune,
				"RimFeast_SetMisfortuneTip".Translate(GameComponent_FeastState.MisfortuneCount, GameComponent_FeastState.MisfortuneDays));
			l.Label("RimFeast_SetRedWeddingDays".Translate(S.redWeddingDays));
			S.redWeddingDays = Mathf.RoundToInt(l.Slider(S.redWeddingDays, 0f, 180f) / 5f) * 5;

			viewHeight = l.CurHeight + 24f;
			l.End();
			Widgets.EndScrollView();
		}
	}
}
