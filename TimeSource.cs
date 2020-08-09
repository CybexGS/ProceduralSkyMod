using System;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class TimeSource
	{
		public static float dayLengthInSeconds = Main.settings.dayLengthSeconds;
		public static float dayProgress = 0f;
		public static float dayProgressDelta = 0f;

		public static float DayProgressDelta { get { CalculateDayProgress(); return dayProgressDelta; } }

		public static void CalculateDayProgress ()
		{
			float newProgress = (Time.time % dayLengthInSeconds) / dayLengthInSeconds;
			dayProgressDelta = (newProgress < dayProgress) ? newProgress + 1 - dayProgress : newProgress - dayProgress;
			dayProgress = newProgress;
		}
	}
}
