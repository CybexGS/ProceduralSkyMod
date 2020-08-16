using System;
using UnityEngine;

namespace ProceduralSkyMod
{
	class DateToSkyMapper
	{
		public static Quaternion SkyboxNightRotation { get; private set; }
		public static Quaternion SunPivotRotation { get; private set; }
		public static Vector3 SunOffsetFromPath { get; private set; }
		public static Quaternion MoonPivotRotation { get; private set; }
		public static Vector3 MoonOffsetFromPath { get; private set; }
		public static float DayProgress { get; private set; }
		public static float YearProgress { get; private set; }

		public static void ApplyDate(DateTime clockTime)
		{
			float sunDistance = ProceduralSkyInitializer.sunDistanceToCamera;
			float moonDistance = ProceduralSkyInitializer.moonDistanceToCamera;
			float latitude = Main.settings.latitude;

			DateTime dayStart = new DateTime(clockTime.Year, clockTime.Month, clockTime.Day);
			DateTime yearEnd = new DateTime(clockTime.Year, 12, 31);
			int daysInYear = yearEnd.DayOfYear;

			DateTime utcTime = clockTime - TimeZoneInfo.Local.GetUtcOffset(clockTime);
			if (TimeZoneInfo.Local.IsDaylightSavingTime(clockTime)) { utcTime.AddHours(-1); }
			DateTime solarTime = utcTime.AddHours(Main.settings.longitude / 15);
			TimeSpan timeSinceMidnight = solarTime.Subtract(dayStart);
			DayProgress = (float)timeSinceMidnight.TotalHours / 24;
			YearProgress = (clockTime.DayOfYear + DayProgress) / daysInYear;

			// stars
			// rotating the skybox 1 extra rotation per year causes the night sky to differ between summer and winter
			float yearlyAngle = 360 * YearProgress;
			float dailyAngle = 360 * DayProgress + 180; // +180 swaps midnight & noon
			SkyboxNightRotation = Quaternion.Euler(-latitude, 0, (dailyAngle + yearlyAngle) % 360);

			// sun
			float sunSeasonalOffset = -23.4f * Mathf.Cos(2 * Mathf.PI * YearProgress);
			SunPivotRotation = Quaternion.Euler(-latitude + Mathf.Lerp(sunSeasonalOffset, 0, Mathf.Abs(latitude) / 90), 0, dailyAngle % 360);
			float sunProjectedSeasonalOffset = sunDistance * Mathf.Tan(Mathf.Deg2Rad * sunSeasonalOffset);
			SunOffsetFromPath = new Vector3(0, 0, Mathf.Lerp(0, sunProjectedSeasonalOffset, Mathf.Abs(latitude) / 90));

			// moon
			double jSolarTime = ToJulianDays(solarTime);
			float phaseAngle = ComputeMoonPhase(jSolarTime);
			float precessionAngle = ComputeMoonPrecession(jSolarTime);
			float moonSeasonalOffset = -23.4f * Mathf.Cos(2 * Mathf.PI * ((yearlyAngle + phaseAngle) % 360) / 360) - 5.14f * Mathf.Cos(2 * Mathf.PI * ((yearlyAngle + phaseAngle - precessionAngle) % 360) / 360);
			MoonPivotRotation = Quaternion.Euler(-latitude + Mathf.Lerp(moonSeasonalOffset, 0, Mathf.Abs(latitude) / 90), 0, (dailyAngle - phaseAngle) % 360);
			float moonProjectedSeasonalOffset = moonDistance * Mathf.Tan(Mathf.Deg2Rad * moonSeasonalOffset);
			MoonOffsetFromPath = new Vector3(0, 0, Mathf.Lerp(0, moonProjectedSeasonalOffset, Mathf.Abs(latitude) / 90));
		}

		// phase range is [0-360), new moon at 0/360
		// taken from https://www.subsystems.us/uploads/9/8/9/4/98948044/moonphase.pdf
		private static float ComputeMoonPhase(double jDays)
		{
			var jDaysSinceKnownNewMoon = jDays - jDaysOfKnownNewMoon;
			var newMoonsSinceKnownNewMoon = jDaysSinceKnownNewMoon / 29.53;
			var fractionOfCycleSinceLastNewMoon = newMoonsSinceKnownNewMoon % 1;
			return (float)(360 * fractionOfCycleSinceLastNewMoon);
		}

		// precession range is [0-360), major standstill at 0/360
		private static float ComputeMoonPrecession(double jDays)
		{
			var jDaysSinceKnownMajorStandstill = jDays - jDaysOfKnownMajorStandstill;
			var majorStandstillsSinceKnownMajorStandstill = jDaysSinceKnownMajorStandstill / (jDaysPerLunarPrecession);
			var fractionOfProcessionSinceLastMajorStandstill = majorStandstillsSinceKnownMajorStandstill % 1;
			return (float)(360 * fractionOfProcessionSinceLastMajorStandstill);
		}

		private static double ToJulianDays(DateTime now)
		{
			var fractionOfDaySinceMidnight = now.Subtract(new DateTime(now.Year, now.Month, now.Day)).TotalHours / 24;
			int Y = now.Year, M = now.Month, D = now.Day;
			int A = Y / 100;
			int B = A / 4;
			int C = 2 - A + B;
			int E = (int)(365.25 * (Y + 4716));
			int F = (int)(30.6001 * (M + 1));
			return C + D + E + F - 1524.5 + fractionOfDaySinceMidnight;
		}

		private static readonly double jDaysOfKnownNewMoon = 2451549.5; // known new moon on 2000 January 6
		// day of month of major standstill is unknown, but using 1st day of the month is close enough for now
		private static readonly double jDaysOfKnownMajorStandstill = ToJulianDays(new DateTime(2006, 6, 1));
		private static readonly double jDaysPerLunarPrecession = 18.6 * 365.25; // 18.6 years per precessions, 365.25 jdays per year
	}
}
