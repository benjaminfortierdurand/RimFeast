using UnityEngine;
using Verse;

namespace RimFeast
{
	[StaticConstructorOnStartup]
	public static class RimFeastTex
	{
		public static readonly Texture2D Invite = ContentFinder<Texture2D>.Get("UI/Commands/RimFeast_Invite");
		public static readonly Texture2D Slaughter = ContentFinder<Texture2D>.Get("UI/Commands/RimFeast_Slaughter");
		// vocabulaire neutre: un couteau a la place des epees
		public static readonly Texture2D SlaughterNeutral = ContentFinder<Texture2D>.Get("UI/Commands/RimFeast_SlaughterNeutral");
		public static readonly Texture2D ColonyFeast = ContentFinder<Texture2D>.Get("UI/Commands/RimFeast_ColonyFeast");
		public static readonly Texture2D Cancel = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
		public static readonly Texture2D Crown = ContentFinder<Texture2D>.Get("UI/RimFeastIcon");
		public static readonly Texture2D AreaBadge = ContentFinder<Texture2D>.Get("UI/Commands/RimFeast_AreaBadge");
		public static readonly Texture2D AreaExpand =
			ContentFinder<Texture2D>.Get("UI/Designators/AreaAllowedExpand", reportFailure: false) ?? TexButton.Plus;
		public static readonly Texture2D AreaClear =
			ContentFinder<Texture2D>.Get("UI/Designators/AreaAllowedClear", reportFailure: false) ?? TexButton.Minus;
	}
}
