using System;
using UnityEngine;

namespace ProceduralSkyMod
{
	public static class TimeSource
	{
		private static float dayProgress = 0f;

		public static float DayProgressDelta { get { return CalculateDayProgress(); } }

		public static float CalculateDayProgress ()
		{
			float newProgress = (Time.time % Main.settings.dayLengthSeconds) / Main.settings.dayLengthSeconds;
			float delta = (newProgress < dayProgress) ? newProgress + 1 - dayProgress : newProgress - dayProgress;
			dayProgress = newProgress;
			return delta;
		}
	}
}
