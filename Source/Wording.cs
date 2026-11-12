using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RimWorld;
using Verse;

namespace RimFeast
{
	// le vocabulaire neutre: pas de second jeu de textes a tenir a jour. les textes du mod sont reecrits en memoire,
	// expression par expression, avec la table que fournit chaque langue (RimFeast_NeutralWords). decocher remet les originaux
	[StaticConstructorOnStartup]
	internal static class Wording
	{
		private const string TableKey = "RimFeast_NeutralWords";
		// les deux textes du reglage citent les mots d'origine: on ne les reecrit pas
		private const string OwnPrefix = "RimFeast_SetNeutral";

		private static readonly List<Action> undo = new List<Action>();
		private static readonly FieldInfo StageCap =
			typeof(ThoughtStage).GetField("cachedLabelCap", BindingFlags.Instance | BindingFlags.NonPublic);

		static Wording()
		{
			Apply();
		}

		public static void Apply()
		{
			for (int i = undo.Count - 1; i >= 0; i--) undo[i]();
			undo.Clear();
			if (!RimFeastMod.S.neutralWording) return;
			try
			{
				LoadedLanguage active = LanguageDatabase.activeLanguage;
				LoadedLanguage fallback = LanguageDatabase.defaultLanguage;
				List<KeyValuePair<Regex, string>> table = Table(active);
				Keyed(active, table);
				// une cle absente de la langue active retombe sur l'anglais
				if (fallback != null && fallback != active)
				{
					List<KeyValuePair<Regex, string>> english = Table(fallback);
					Keyed(fallback, english);
					// langue sans traduction du mod: ses defs sont restees en anglais
					if (table.Count == 0 && !Translated(active)) table = english;
				}
				// les defs portent le texte de la langue active
				if (table.Count > 0) Defs(table);
			}
			catch (Exception e)
			{
				Log.Warning("RimFeast: neutral wording: " + e.Message);
			}
		}

		// "avant=apres|avant=apres". les plus longues expressions d'abord, sinon "maison" passe avant "maison noble".
		// une regle s'ecrit donc toujours sur le texte d'origine, jamais sur le resultat d'une autre
		private static List<KeyValuePair<Regex, string>> Table(LoadedLanguage lang)
		{
			var table = new List<KeyValuePair<Regex, string>>();
			// HaveTextForKey lit la langue si le jeu ne l'a pas encore fait: l'anglais, quand le jeu tourne dans une autre langue
			if (lang == null || !lang.HaveTextForKey(TableKey)
				|| !lang.keyedReplacements.TryGetValue(TableKey, out LoadedLanguage.KeyedReplacement raw)
				|| raw.value.NullOrEmpty())
				return table;
			foreach (string pair in raw.value.Split('|').OrderByDescending(p => p.IndexOf('=')))
			{
				int cut = pair.IndexOf('=');
				if (cut <= 0 || cut == pair.Length - 1) continue;
				string from = pair.Substring(0, cut).Trim();
				table.Add(new KeyValuePair<Regex, string>(
					new Regex(@"(?<![\p{L}])" + Regex.Escape(from) + @"(?![\p{L}])", RegexOptions.IgnoreCase),
					pair.Substring(cut + 1).Trim()));
			}
			return table;
		}

		private static bool Translated(LoadedLanguage lang) =>
			lang != null && lang.keyedReplacements.Keys.Any(k => k.StartsWith("RimFeast_"));

		// la majuscule du mot remplace est gardee
		private static string Swap(string text, List<KeyValuePair<Regex, string>> table)
		{
			if (text.NullOrEmpty()) return text;
			for (int i = 0; i < table.Count; i++)
			{
				string to = table[i].Value;
				text = table[i].Key.Replace(text,
					m => char.IsUpper(m.Value[0]) ? char.ToUpper(to[0]) + to.Substring(1) : to);
			}
			return text;
		}

		private static void Keyed(LoadedLanguage lang, List<KeyValuePair<Regex, string>> table)
		{
			if (lang == null || table.Count == 0) return;
			foreach (KeyValuePair<string, LoadedLanguage.KeyedReplacement> kv in lang.keyedReplacements)
			{
				if (!kv.Key.StartsWith("RimFeast_") || kv.Key == TableKey || kv.Key.StartsWith(OwnPrefix)) continue;
				LoadedLanguage.KeyedReplacement entry = kv.Value;
				Field(entry.value, table, v => entry.value = v);
			}
		}

		private static void Defs(List<KeyValuePair<Regex, string>> table)
		{
			foreach (Type type in GenDefDatabase.AllDefTypesWithDatabases())
				foreach (Def def in GenDefDatabase.GetAllDefsInDatabaseForDef(type))
				{
					if (def?.defName == null || !def.defName.StartsWith("RimFeast_")) continue;
					Def d = def;
					Field(d.description, table, v => d.description = v);
					if (d is JobDef job) Field(job.reportString, table, v => job.reportString = v);
					if (!(d is ThoughtDef thought) || thought.stages == null) continue;
					foreach (ThoughtStage stage in thought.stages)
					{
						if (stage == null) continue;
						ThoughtStage s = stage;
						// le libelle en majuscule est mis en cache a la premiere lecture
						Field(s.label, table, v =>
						{
							s.label = v;
							StageCap?.SetValue(s, null);
						});
						Field(s.description, table, v => s.description = v);
					}
				}
		}

		private static void Field(string was, List<KeyValuePair<Regex, string>> table, Action<string> set)
		{
			string now = Swap(was, table);
			if (now == was) return;
			set(now);
			undo.Add(() => set(was));
		}
	}
}
