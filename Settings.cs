using System;
using UnityModManagerNet;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class Settings : UnityModManager.ModSettings, IDrawable
	{
		[Draw(Label = "Day length in minutes realtime", Min = 1, Max = 3600)]
		public int dayLengthMinutesRT = 60;

		[Draw(Label = "Latitude (+N/-S)", Min = -90, Max = 90)]
		public float latitude = 44.7872f;

		[Draw(Label = "Longitude (+E/-W)", Min = -180, Max = 180)]
		public float longitude = (float)(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalHours + (TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now) ? -1 : 0)) * 15;

		override public void Save (UnityModManager.ModEntry entry)
		{
			Save<Settings>(this, entry);
		}

		public void OnChange () { }
	}
}
