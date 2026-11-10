using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimFeast
{
	// une ligne de la chronique. les noms sont copies en texte: la plupart des convives n'existent plus quand on la relit
	public class ChronicleLine : IExposable
	{
		public const int Leader = 1;
		public const int Gift = 2;
		public const int Dance = 3;
		public const int Conga = 4;
		public const int Round = 5;
		public const int Brawl = 6;
		public const int Betrothed = 7;
		public const int Declined = 8;
		public const int Wed = 9;
		public const int FirstDance = 10;
		public const int Gave = 11;
		public const int Rival = 12;
		public const int Ward = 13;
		public const int Refused = 14;
		public const int BedYes = 15;
		public const int BedNo = 16;
		public const int Poisoner = 17;
		public const int Poisoned = 18;
		public const int Slain = 19;
		public const int Fallen = 20;
		public const int Lost = 21;
		public const int Nameless = 22;
		public const int War = 23;
		public const int Blood = 24;
		public const int BrawlDeath = 25;
		public const int Unwed = 26;
		public const int Caravan = 27;
		public const int Silver = 28;

		// meme ordre que les constantes, et le numero est sauvegarde: on ajoute a la fin, on ne deplace rien
		private static readonly string[] Keys =
		{
			null, "Leader", "Gift", "Dance", "Conga", "Round", "Brawl", "Betrothed", "Declined", "Wed", "FirstDance",
			"Gave", "Rival", "Ward", "Refused", "BedYes", "BedNo", "Poisoner", "Poisoned", "Slain", "Fallen", "Lost",
			"Nameless", "War", "Blood", "BrawlDeath", "Unwed", "Caravan", "Silver",
		};

		public int kind;
		public string a;
		public string b;
		public int n;

		public bool Grim => kind == Slain || kind == Lost || kind == Nameless || kind == War || kind == Poisoned
			|| kind == BrawlDeath;

		public bool Glad => kind == Betrothed || kind == Wed || kind == FirstDance;

		public string Label => kind > 0 && kind < Keys.Length
			? (string)("RimFeast_Chron" + Keys[kind]).Translate(a ?? "", b ?? "", n) : "";

		public void ExposeData()
		{
			Scribe_Values.Look(ref kind, "kind");
			Scribe_Values.Look(ref a, "a");
			Scribe_Values.Look(ref b, "b");
			Scribe_Values.Look(ref n, "n");
		}
	}

	public class ChronicleEntry : IExposable
	{
		public const int Feast = 0;
		public const int Wedding = 1;
		public const int Colony = 2;
		public const int Away = 3;
		private const int MaxLines = 40;

		public int caseId = -1;
		public int kind;
		public int absTick;
		public Vector2 place;
		public Faction house;
		public string houseName;
		public int score = -1;
		// banquet: 0 a 3 du pire au meilleur. a leur table: le numero de l'issue
		public int tier = -1;
		public int goodwill;
		public int guests;
		public int toasts;
		public float hall = -1f;
		public float table = -1f;
		public float drink = -1f;
		public float ent = -1f;
		public bool red;
		public List<ChronicleLine> lines = new List<ChronicleLine>();

		public void Add(int lineKind, string a = null, string b = null, int n = 0)
		{
			if (lines.Count >= MaxLines) return;
			// un couple ne s'ecrit qu'une fois, et la premiere danse des maries l'emporte sur une danse ordinaire
			if (lineKind == ChronicleLine.Dance || lineKind == ChronicleLine.FirstDance)
				for (int i = 0; i < lines.Count; i++)
				{
					ChronicleLine l = lines[i];
					if (l.kind != ChronicleLine.Dance && l.kind != ChronicleLine.FirstDance) continue;
					if (!(l.a == a && l.b == b) && !(l.a == b && l.b == a)) continue;
					if (lineKind == ChronicleLine.FirstDance) l.kind = ChronicleLine.FirstDance;
					return;
				}
			lines.Add(new ChronicleLine { kind = lineKind, a = a, b = b, n = n });
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref caseId, "caseId", -1);
			Scribe_Values.Look(ref kind, "kind");
			Scribe_Values.Look(ref absTick, "absTick");
			Scribe_Values.Look(ref place, "place");
			Scribe_References.Look(ref house, "house");
			Scribe_Values.Look(ref houseName, "houseName");
			Scribe_Values.Look(ref score, "score", -1);
			Scribe_Values.Look(ref tier, "tier", -1);
			Scribe_Values.Look(ref goodwill, "goodwill");
			Scribe_Values.Look(ref guests, "guests");
			Scribe_Values.Look(ref toasts, "toasts");
			Scribe_Values.Look(ref hall, "hall", -1f);
			Scribe_Values.Look(ref table, "table", -1f);
			Scribe_Values.Look(ref drink, "drink", -1f);
			Scribe_Values.Look(ref ent, "ent", -1f);
			Scribe_Values.Look(ref red, "red");
			Scribe_Collections.Look(ref lines, "lines", LookMode.Deep);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && lines == null) lines = new List<ChronicleLine>();
		}
	}
}
