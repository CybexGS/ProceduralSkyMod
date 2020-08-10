using System.Collections;
using UnityEngine;

namespace ProceduralSkyMod
{
	public static class TimeSource
	{
		private static float timeProgress = 0f;

		public static float DayProgress { get; set; } = 0.5f;
		public static float DayProgressDelta { get; set; }
		public static float YearProgress { get; set; } = 0.5f;
		public static float YearProgressDelta { get; set; }

		public static void CalculateTimeProgress ()
		{
			float newTimeProgress = (Time.time % SkyManager.DayLengthInSeconds) / SkyManager.DayLengthInSeconds;

			DayProgressDelta = (newTimeProgress < timeProgress) ? newTimeProgress + 1 - timeProgress : newTimeProgress - timeProgress;
			DayProgress = (DayProgress + DayProgressDelta) % 1;
			timeProgress = newTimeProgress;

			// yearProgress increses by dayProgress divided by days in year (Julian year of 365.25 days)
			float newYearProgress = (YearProgress + DayProgressDelta / 365.25f) % 365.25f;
			YearProgressDelta = (newYearProgress < YearProgress) ? newYearProgress + 1 - YearProgress : newYearProgress - YearProgress;
			YearProgress = newYearProgress;
		}
	}
}
