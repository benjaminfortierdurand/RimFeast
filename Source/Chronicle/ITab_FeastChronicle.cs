using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimFeast
{
	// la chronique des soirees, en onglet sur le marqueur. chaque soiree tient dans un cadre, pour la capture d'ecran
	public class ITab_FeastChronicle : ITab
	{
		private static readonly Vector2 WinSize = new Vector2(500f, 580f);
		private static readonly Color Gold = new Color(0.94f, 0.81f, 0.38f);
		private static readonly Color Green = new Color(0.56f, 0.80f, 0.52f);
		private static readonly Color Grey = new Color(0.66f, 0.66f, 0.66f);
		private static readonly Color Red = new Color(0.87f, 0.36f, 0.36f);
		private static readonly Color Rose = new Color(0.94f, 0.62f, 0.72f);
		private static readonly Color Dim = new Color(0.62f, 0.62f, 0.62f);
		private static readonly Color Track = new Color(1f, 1f, 1f, 0.08f);

		private static readonly string[] KindKeys =
		{
			"RimFeast_ChronKindFeast", "RimFeast_ChronKindWedding", "RimFeast_ChronKindColony", "RimFeast_ChronKindAway",
		};
		private static readonly string[] OccasionKeys =
		{
			null, "RimFeast_ChronKindWake", "RimFeast_ChronKindVictory", "RimFeast_ChronKindHarvest",
		};
		private static readonly string[] TierKeys =
		{
			"RimFeast_LabelFeastPoor", "RimFeast_LabelFeastDecent", "RimFeast_LabelFeastGood", "RimFeast_LabelFeastTriumph",
		};
		private static readonly string[] AwayKeys =
		{
			null, "RimFeast_LabelAwayKnives", "RimFeast_LabelAwayAmbush", "RimFeast_LabelAwaySlight",
			"RimFeast_LabelAwayDull", "RimFeast_LabelAwayFine", "RimFeast_LabelAwaySongs",
		};

		private Vector2 scroll;
		private float listHeight = 600f;

		public ITab_FeastChronicle()
		{
			size = WinSize;
			labelKey = "RimFeast_TabChronicle";
		}

		protected override void FillTab()
		{
			List<ChronicleEntry> all = GameComponent_FeastState.Get()?.Chronicle;
			Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(12f);

			GUI.DrawTexture(new Rect(rect.x, rect.y, 40f, 40f), RimFeastTex.Crown);
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(rect.x + 50f, rect.y - 2f, rect.width - 80f, 30f), "RimFeast_ChronTitle".Translate());
			Text.Font = GameFont.Small;
			GUI.color = Dim;
			Widgets.Label(new Rect(rect.x + 50f, rect.y + 24f, rect.width - 50f, 22f), Summary(all));
			GUI.color = Color.white;

			Rect outRect = new Rect(rect.x, rect.y + 52f, rect.width, rect.height - 52f);
			if (all == null || all.Count == 0)
			{
				GUI.color = Dim;
				Widgets.Label(outRect, "RimFeast_ChronEmpty".Translate());
				GUI.color = Color.white;
				return;
			}

			// la plus recente en haut. on mesure tout, on ne dessine que ce qui est a l'ecran
			float width = outRect.width - 18f;
			Widgets.BeginScrollView(outRect, ref scroll, new Rect(0f, 0f, width, listHeight));
			float y = 0f;
			for (int i = all.Count - 1; i >= 0; i--)
			{
				float h = Card(all[i], new Rect(0f, y, width, 0f), false);
				if (y + h >= scroll.y && y <= scroll.y + outRect.height)
					Card(all[i], new Rect(0f, y, width, h), true);
				y += h + 8f;
			}
			listHeight = y;
			Widgets.EndScrollView();
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}

		private static string Summary(List<ChronicleEntry> all)
		{
			if (all == null || all.Count == 0) return "";
			int best = -1;
			for (int i = 0; i < all.Count; i++)
				if (all[i].score > best) best = all[i].score;
			return best >= 0 ? "RimFeast_ChronSummary".Translate(all.Count, best)
				: "RimFeast_ChronSummaryPlain".Translate(all.Count);
		}

		private static Color Tone(ChronicleEntry e)
		{
			if (e.red) return Red;
			if (e.kind == ChronicleEntry.Away) return e.tier >= 5 ? Green : e.tier == 4 ? Grey : Red;
			if (e.kind == ChronicleEntry.Wedding && e.tier != 0) return Rose;
			return e.tier == 3 ? Gold : e.tier == 2 ? Green : e.tier == 0 ? Red : Grey;
		}

		private static string Verdict(ChronicleEntry e)
		{
			if (e.tier < 0) return null;
			if (e.kind == ChronicleEntry.Away)
				return e.tier >= 1 && e.tier < AwayKeys.Length ? (string)AwayKeys[e.tier].Translate() : null;
			if (e.tier > 3) return null;
			if (e.kind == ChronicleEntry.Colony)
				return GameComponent_FeastState.OccasionThought(e.occasion).stages[e.tier].LabelCap;
			return TierKeys[e.tier].Translate();
		}

		private static string KindKey(ChronicleEntry e)
		{
			if (e.red) return "RimFeast_ChronKindRed";
			if (e.grand) return "RimFeast_ChronKindGrand";
			if (e.kind == ChronicleEntry.Colony && e.occasion >= 1 && e.occasion < OccasionKeys.Length)
				return OccasionKeys[e.occasion];
			return KindKeys[Mathf.Clamp(e.kind, 0, 3)];
		}

		// meme code pour mesurer et pour dessiner: la hauteur rendue est celle du cadre
		private static float Card(ChronicleEntry e, Rect r, bool draw)
		{
			float x = r.x + 14f;
			float w = r.width - 24f;
			float y = r.y + 8f;
			Color tone = Tone(e);
			bool own = e.kind == ChronicleEntry.Colony;
			if (draw)
			{
				Widgets.DrawMenuSection(r);
				Widgets.DrawBoxSolid(new Rect(r.x + 1f, r.y + 1f, 4f, r.height - 2f), tone);

				Text.Font = GameFont.Tiny;
				GUI.color = Dim;
				Widgets.Label(new Rect(x, y, w, 20f), GenDate.DateFullStringAt(e.absTick, e.place));
				Text.Anchor = TextAnchor.UpperRight;
				if (e.red) GUI.color = Red;
				Widgets.Label(new Rect(x, y, w, 20f), KindKey(e).Translate());
				Text.Anchor = TextAnchor.UpperLeft;

				Faction f = own ? Faction.OfPlayer : e.house;
				Texture2D icon = own || f?.def == null ? RimFeastTex.Crown : f.def.FactionIcon;
				GUI.color = own || f == null ? Color.white : f.Color;
				if (icon != null) GUI.DrawTexture(new Rect(x, y + 22f, 26f, 26f), icon);
				Text.Font = GameFont.Medium;
				string name = e.grand ? (string)"RimFeast_GrandGuests".Translate().CapitalizeFirst()
					: (own ? f?.Name : e.houseName ?? f?.Name) ?? "?";
				Widgets.Label(new Rect(x + 34f, y + 20f, w - 34f, 32f), name.Truncate(w - 34f));
				GUI.color = Color.white;
			}
			y += 54f;

			string verdict = Verdict(e);
			if (verdict != null || e.score >= 0)
			{
				if (draw)
				{
					float vx = x;
					if (e.score >= 0)
					{
						GUI.color = tone;
						Text.Font = GameFont.Medium;
						Widgets.Label(new Rect(vx, y - 3f, 46f, 30f), e.score.ToString());
						vx += 46f;
					}
					Text.Font = GameFont.Small;
					GUI.color = Color.white;
					Widgets.Label(new Rect(vx, y + 2f, w - (vx - x), 24f), verdict ?? "");
					string gain = null;
					bool good = true;
					if (own && e.tier >= 0 && e.tier <= 3)
						gain = "RimFeast_ChronMood".Translate(
							((int)GameComponent_FeastState.OccasionThought(e.occasion).stages[e.tier].baseMoodEffect)
								.ToStringWithSign());
					else if (!own && e.goodwill != 0)
					{
						gain = "RimFeast_ChronGoodwill".Translate(e.goodwill.ToStringWithSign());
						good = e.goodwill > 0;
					}
					if (gain != null)
					{
						Text.Anchor = TextAnchor.UpperRight;
						GUI.color = good ? Green : Red;
						Widgets.Label(new Rect(x, y + 2f, w, 24f), gain);
						Text.Anchor = TextAnchor.UpperLeft;
						GUI.color = Color.white;
					}
				}
				y += 28f;
			}

			if (e.hall >= 0f)
			{
				if (draw)
				{
					float bw = (w - 30f) / 4f;
					Bar(new Rect(x, y, bw, 26f), "RimFeast_ChronHall", e.hall, tone);
					Bar(new Rect(x + bw + 10f, y, bw, 26f), "RimFeast_ChronTable", e.table, tone);
					Bar(new Rect(x + (bw + 10f) * 2f, y, bw, 26f), "RimFeast_ChronDrink", e.drink, tone);
					Bar(new Rect(x + (bw + 10f) * 3f, y, bw, 26f), "RimFeast_ChronEnt", e.ent, tone);
				}
				y += 32f;
			}

			if (e.guests > 0 && e.kind != ChronicleEntry.Away)
			{
				if (draw)
				{
					Text.Font = GameFont.Tiny;
					GUI.color = Dim;
					Widgets.Label(new Rect(x, y, w, 20f),
						(own ? "RimFeast_ChronStatsColony" : "RimFeast_ChronStats").Translate(e.guests, e.toasts));
					GUI.color = Color.white;
				}
				y += 20f;
			}

			Text.Font = GameFont.Small;
			for (int i = 0; i < e.lines.Count; i++)
			{
				ChronicleLine l = e.lines[i];
				string s = l.Label;
				float h = Text.CalcHeight(s, w - 14f);
				if (draw)
				{
					GUI.color = l.Grim ? Red : l.Glad ? Rose : Dim;
					Widgets.Label(new Rect(x, y, 14f, h), "-");
					if (!l.Grim && !l.Glad) GUI.color = Color.white;
					Widgets.Label(new Rect(x + 14f, y, w - 14f, h), s);
					GUI.color = Color.white;
				}
				y += h;
			}
			return y + 8f - r.y;
		}

		private static void Bar(Rect r, string key, float value, Color tone)
		{
			Text.Font = GameFont.Tiny;
			GUI.color = Dim;
			Widgets.Label(new Rect(r.x, r.y, r.width, 18f), key.Translate());
			GUI.color = Color.white;
			Rect track = new Rect(r.x, r.y + 19f, r.width, 6f);
			Widgets.DrawBoxSolid(track, Track);
			Widgets.DrawBoxSolid(new Rect(track.x, track.y, track.width * Mathf.Clamp01(value), track.height), tone);
		}
	}
}
