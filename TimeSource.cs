using System.Collections;
using UnityEngine;

namespace ProceduralSkyMod
{
	public static class TimeSource
	{
		private static float dayProgress = 0f;

		public static float DayProgressDelta { get; private set; }
		public static float YearProgress { get; private set; } = 0.5f;
		public static float YearProgressDelta { get; private set; }

		public static void CalculateTimeProgress ()
		{
			float newDayProgress = (Time.time % Main.settings.dayLengthSeconds) / Main.settings.dayLengthSeconds;
			DayProgressDelta = (newDayProgress < dayProgress) ? newDayProgress + 1 - dayProgress : newDayProgress - dayProgress;

			// yearProgress increses by dayProgress divided by days in year (Julian year of 365.25 days)
			float newYearProgress = (YearProgress + DayProgressDelta / 365.25f) % 365.25f;
			YearProgressDelta = (newYearProgress < YearProgress) ? newYearProgress + 1 - YearProgress : newYearProgress - YearProgress;

			dayProgress = newDayProgress;
			YearProgress = newYearProgress;
		}
	}
}
