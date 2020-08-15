﻿using System.Collections;
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

		public static void CalculateTimeProgress (float latitude, float longitude)
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
			float snrZ = (SkyboxNightRotation.z + 360f * DayProgressDelta) % 360;
			SkyboxNightRotation = new Vector3(SkyboxNightRotation.x, SkyboxNightRotation.y, snrZ);

			// daily rotation of the sun including correcting reduction for year progress
			float sprZ = (SunPivotRotation.z + 360f * DayProgressDelta - 360f * YearProgressDelta) % 360;
			// yearly rotation of sun pivot's x axis from -23.4 to 23.4 degrees to aproximately simulate seasonal changes of sun's relative position
			float sprX = -latitude + 23.4f * (YearProgress * 2 - 1);
			SunPivotRotation = new Vector3(sprX, SunPivotRotation.y, sprZ);

			// the moon looses per day about 50 minutes (time not angle) to the sun. 50min / 1440min = 0.03472222222... percent loss, therefore multiply by 1 - 0.03472222
			float mrZ = (MoonRotation.z + 360f * DayProgressDelta * 0.96527778f) % 360;
			// yearly rotation of moon pivot's x axis from -23.4 to 23.4 degrees to aproximately simulate seasonal changes of moon's relative position + 5.14 for moon's orbital offset
			float mrX = -latitude + 23.4f * (YearProgress * 2 - 1) + 5.14f;
			MoonRotation = new Vector3(mrX, MoonRotation.y, mrZ);
		}
	}
}
