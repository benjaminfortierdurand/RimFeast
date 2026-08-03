using UnityEngine;
using Verse;

namespace RimFeast
{
	public class RimFeastSettings : ModSettings
	{
		public int inviteCost = 200;      // presents du messager, x1.5 par banquet memorise
		public int giftSilver = 400;      // bourse laissee apres un banquet de legende
		public int feastHours = 8;        // duree du festin une fois a table
		public int corteges = 6;          // taille type du cortege, +/-2 selon l'estime

		public bool summonColonists = true; // on lache la pelle quand le duc s'assied
		public bool leaderEnabled = true;   // le chef de maison peut venir en personne
		public bool marriageEnabled = true;
		public float proposalChance = 0.02f;
		public bool tradePactEnabled = true;
		public bool requestsEnabled = true;   // le seigneur demande une faveur a table
		public float requestChance = 0.35f;   // tire une seule fois, a mi-banquet

		public bool awayFeastsEnabled = true; // les maisons rendent l'invitation
		public int awayFeastDays = 20;        // delai moyen entre deux invitations recues
		public int awayInviteWindowDays = 12; // plancher du delai, allonge selon la distance
		public int awayMaxTravelDays = 30;    // au-dela, la maison est trop loin pour inviter

		// vanilla: 0.25 = eméché, 0.4 = ivre. au-dela on ramasse les convives a la petite cuiller
		public float drinkLimit = 0.35f;
		public float brawlChance = 0.03f;

		public bool treacheryEnabled = true;
		public float treacheryChance = 0.35f;
		public int treacheryGoodwillMax = -10;

		public int fadeDays = 20;         // au bout de ca, la maison oublie un banquet
		public int redWeddingDays = 60;   // duree du blacklist apres des noces pourpres

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref inviteCost, "inviteCost", 200);
			Scribe_Values.Look(ref giftSilver, "giftSilver", 400);
			Scribe_Values.Look(ref feastHours, "feastHours", 8);
			Scribe_Values.Look(ref corteges, "corteges", 6);
			Scribe_Values.Look(ref summonColonists, "summonColonists", true);
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
			Scribe_Values.Look(ref drinkLimit, "drinkLimit", 0.35f);
			Scribe_Values.Look(ref brawlChance, "brawlChance", 0.03f);
			Scribe_Values.Look(ref treacheryEnabled, "treacheryEnabled", true);
			Scribe_Values.Look(ref treacheryChance, "treacheryChance", 0.35f);
			Scribe_Values.Look(ref treacheryGoodwillMax, "treacheryGoodwillMax", -10);
			Scribe_Values.Look(ref fadeDays, "fadeDays", 20);
			Scribe_Values.Look(ref redWeddingDays, "redWeddingDays", 60);
		}
	}

	public class RimFeastMod : Mod
	{
		private static RimFeastSettings settings;

		// jamais null: si la classe Mod n'a pas ete instanciee (ou a echoue), tout le mod
		// tomberait en NullRef au premier reglage lu
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
		}

		public override string SettingsCategory() => "RimFeast";

		public override void DoSettingsWindowContents(Rect inRect)
		{
			var l = new Listing_Standard();
			l.Begin(inRect);

			l.Label("RimFeast_SetInviteCost".Translate(S.inviteCost));
			S.inviteCost = Mathf.RoundToInt(l.Slider(S.inviteCost, 0f, 1000f) / 25f) * 25;
			l.Label("RimFeast_SetGift".Translate(S.giftSilver));
			S.giftSilver = Mathf.RoundToInt(l.Slider(S.giftSilver, 0f, 2000f) / 50f) * 50;
			l.Label("RimFeast_SetHours".Translate(S.feastHours));
			S.feastHours = Mathf.RoundToInt(l.Slider(S.feastHours, 2f, 24f));
			l.Label("RimFeast_SetCortege".Translate(S.corteges, Mathf.Max(2, S.corteges - 3), S.corteges + 3));
			S.corteges = Mathf.RoundToInt(l.Slider(S.corteges, 3f, 16f));

			l.GapLine();
			l.CheckboxLabeled("RimFeast_SetSummon".Translate(), ref S.summonColonists,
				"RimFeast_SetSummonTip".Translate());
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

			l.GapLine();
			l.Label("RimFeast_SetFadeDays".Translate(S.fadeDays));
			S.fadeDays = Mathf.RoundToInt(l.Slider(S.fadeDays, 1f, 60f));
			l.Label("RimFeast_SetRedWeddingDays".Translate(S.redWeddingDays));
			S.redWeddingDays = Mathf.RoundToInt(l.Slider(S.redWeddingDays, 0f, 180f) / 5f) * 5;

			l.End();
		}
	}
}
