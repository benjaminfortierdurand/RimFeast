using UnityEngine;
using Verse;

namespace RimFeast
{
	[StaticConstructorOnStartup]
	public static class RimFeastTex
	{
		public static readonly Texture2D Invite = ContentFinder<Texture2D>.Get("UI/Commands/RimFeast_Invite");
		public static readonly Texture2D Slaughter = ContentFinder<Texture2D>.Get("UI/Commands/RimFeast_Slaughter");
		public static readonly Texture2D ShowMarker = ContentFinder<Texture2D>.Get("UI/Commands/RimFeast_ShowMarker");
		public static readonly Texture2D Cancel = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
	}
}
