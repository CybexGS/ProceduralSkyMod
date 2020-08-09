using System;
using UnityModManagerNet;

namespace ProceduralSkyMod
{
	public class Settings : UnityModManager.ModSettings, IDrawable
	{
		[Draw(Label = "Day length in seconds")]
		public float dayLengthSeconds = 3600;

		override public void Save (UnityModManager.ModEntry entry)
		{
			Save<Settings>(this, entry);
		}

		public void OnChange () { }
	}
}
