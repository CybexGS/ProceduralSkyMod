using System;
using System.Collections;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class SkyManager : MonoBehaviour
	{
		// TODO: solve cloud hopping problem

		private Color ambientDay = new Color(.282f, .270f, .243f, 1f);
		private Color ambientNight = new Color(.079f, .079f, .112f, 1f);
		private Color defaultFog, nightFog;

		private Vector3 worldPos;

		public Transform SunPivot { get; set; }
		public Transform SkyboxNight { get; set; }
		public Transform MoonBillboard { get; set; }

		public Light Sun { get; set; }
		public Material StarMaterial { get; set; }
		public Material SkyMaterial { get; set; }
		public Material CloudMaterial { get; set; }
		public Material MoonMaterial { get; set; }

		public Transform SkyCam { get; set; }
		public Transform CloudPlane { get; set; }
		public Transform ClearCam { get; set; }

		void Start ()
		{
			defaultFog = RenderSettings.fogColor;
			nightFog = new Color(defaultFog.r * 0.05f, defaultFog.g * 0.05f, defaultFog.b * 0.05f, 1f);

			CloudMaterial.SetFloat("_CloudSpeed", 0.03f);
			StarMaterial.SetFloat("_Exposure", 2.0f);

			StartCoroutine(CloudChanger());
		}

		void Update ()
		{
			// rotation
			DateTime clockTime = TimeSource.GetCurrentTime();
			DateTime dayStart = new DateTime(clockTime.Year, clockTime.Month, clockTime.Day);
			DateTime yearEnd = new DateTime(clockTime.Year, 12, 31);
			int daysInYear = yearEnd.DayOfYear;

			DateTime utcTime = clockTime - TimeZoneInfo.Local.GetUtcOffset(clockTime);
			if (TimeZoneInfo.Local.IsDaylightSavingTime(clockTime)) { utcTime.AddHours(-1); }
			DateTime solarTime = utcTime.AddHours(Main.settings.longitude / 15);
			TimeSpan timeSinceMidnight = solarTime.Subtract(dayStart);
			float dayFration = (float)timeSinceMidnight.TotalHours / 24;
			float yearFraction = (clockTime.DayOfYear + dayFration) / daysInYear;

			// rotating the skybox 1 extra rotation per year causes the night sky to differ between summer and winter
			float yearlyAngle = 360 * yearFraction;
			float dailyAngle = 360 * dayFration + 180; // +180 swaps midnight & noon
			SkyboxNight.localRotation = Quaternion.Euler(-Main.settings.latitude, 0, (dailyAngle + yearlyAngle) % 360);
			// anti-rotating the sun 1 rotation per year keeps the solar day centered on solar noon
			// +x rotation moves sun toward southern hemisphere
			SunPivot.localRotation = Quaternion.Euler(0, 0, -yearlyAngle);
			Sun.transform.localPosition = new Vector3(0, 0, -10 * Mathf.Tan(23.4f * Mathf.PI / 180 * Mathf.Cos(2 * Mathf.PI * yearFraction)));
			// moon is new when rotation around self.forward is 0
			float phaseAngle = ComputeMoonPhase(solarTime);
			MoonBillboard.localRotation = Quaternion.Euler(-Main.settings.latitude + 23.4f + 5.14f, 0, (dailyAngle - phaseAngle) % 360);

			// movement
			worldPos = PlayerManager.PlayerTransform.position - WorldMover.currentMove;
			transform.position = new Vector3(worldPos.x * .001f, 0, worldPos.z * .001f);


			Vector3 sunPos = Sun.transform.position - transform.position;
			Sun.intensity = Mathf.Clamp01(sunPos.y);
			Sun.color = Color.Lerp(new Color(1f, 0.5f, 0), Color.white, Sun.intensity);

			StarMaterial.SetFloat("_Visibility", (-Sun.intensity + 1) * .01f);

			MoonMaterial.SetFloat("_MoonDayNight", Mathf.Lerp(2.19f, 1.5f, Sun.intensity));
			// phaseAngle is new at the extremes, but _MoonPhase is full at the extremes, thus +180
			MoonMaterial.SetFloat("_MoonPhase", Mathf.Lerp(1f, -1f, (phaseAngle + 180) % 360 / 360));
			MoonMaterial.SetFloat("_Exposure", Mathf.Lerp(2f, 4f, Sun.intensity));

			SkyMaterial.SetFloat("_Exposure", Mathf.Lerp(.01f, 1f, Sun.intensity));
			SkyMaterial.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.1f, 1f, Mathf.Clamp01(Sun.intensity * 10)));

			CloudMaterial.SetFloat("_CloudBright", Mathf.Lerp(.002f, .9f, Sun.intensity));
			CloudMaterial.SetFloat("_CloudGradient", Mathf.Lerp(.45f, .2f, Sun.intensity));

			RenderSettings.fogColor = Color.Lerp(nightFog, defaultFog, Sun.intensity);
			RenderSettings.ambientSkyColor = Color.Lerp(ambientNight, ambientDay, Sun.intensity);

			cloudCurrent = Mathf.Lerp(cloudCurrent, cloudTarget, Time.deltaTime * 0.1f);
			CloudMaterial.SetFloat("_ClearSky", cloudCurrent);
		}

		void OnDisable ()
		{
			StopCoroutine(CloudChanger());
		}

		float cloudTarget = 2, cloudCurrent = 1;
		private IEnumerator CloudChanger ()
		{
			while (true)
			{
				yield return new WaitForSeconds(60);
				// .5 to 5 to test it
				cloudTarget = Mathf.Clamp(UnityEngine.Random.value * 5, .5f, 5f);
#if DEBUG
				Debug.Log(string.Format("New Cloud Target of {0}, current {1}", cloudTarget, cloudCurrent));
#endif
			}
		}

		// phase range is [0-360)
		// taken from https://www.subsystems.us/uploads/9/8/9/4/98948044/moonphase.pdf
		float ComputeMoonPhase (DateTime now)
		{
			var jDays = ToJulianDays(now);
			var jDaysSinceKnownNewMoon = jDays - 2451549.5; // known new moon on 2000 January 6
			var newMoonsSinceKnownNewMoon = jDaysSinceKnownNewMoon / 29.53;
			var fractionOfCycleSinceLastNewMoon = newMoonsSinceKnownNewMoon % 1;
			return (float)(360 * fractionOfCycleSinceLastNewMoon);
		}

		double ToJulianDays (DateTime now)
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
	}
}
