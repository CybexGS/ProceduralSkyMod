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

		public static Vector3 SkyboxNightRotation { get; set; }
		public static Vector3 SunPivotRotation { get; set; }
		public static Vector3 MoonRotation { get; set; }

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

			// daily rotation of skybox night
			SkyboxNightRotation = new Vector3(SkyboxNightRotation.x, SkyboxNightRotation.y, (SkyboxNightRotation.z + 360f * DayProgressDelta) % 360);
			// daily rotation of the sun including correcting reduction for year progress
			SunPivotRotation = new Vector3(SunPivotRotation.x, SunPivotRotation.y, (SunPivotRotation.z + 360f * DayProgressDelta - 360f * YearProgressDelta) % 360);
			// the moon looses per day about 50 minutes (time not angle) to the sun. 50min / 1440min = 0.03472222222... percent loss, therefore multiply by 1 - 0.03472222
			MoonRotation = new Vector3(MoonRotation.y, MoonRotation.y, (MoonRotation.z + 360f * DayProgressDelta * 0.96527778f) % 360);
		}
	}
}
