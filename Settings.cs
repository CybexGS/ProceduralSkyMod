using System;
using UnityModManagerNet;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class Settings : UnityModManager.ModSettings, IDrawable
	{
		[Draw(Label = "Day length in minutes realtime", Min = 1, Max = 3600)] public int dayLengthMinutesRT = 60;

		override public void Save (UnityModManager.ModEntry entry)
		{
			Save<Settings>(this, entry);
		}

		public void OnChange () { }
	}
}
