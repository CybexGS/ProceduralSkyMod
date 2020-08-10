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

		public float latitude = 0f;

		private Vector3 worldPos;

		public Transform SkyboxNight { get; set; }
		public Transform SunPivot { get; set; }
		public Transform MoonBillboard { get; set; }

		public Light Sun { get; set; }
		public Material StarMaterial { get; set; }
		public Material SkyMaterial { get; set; }
		public Material CloudMaterial { get; set; }
		public Material MoonMaterial { get; set; }

		public Transform ClearCam { get; set; }
		public Transform SkyCam { get; set; }
		public Transform CloudPlane { get; set; }

		void Start ()
		{
			defaultFog = RenderSettings.fogColor;
			nightFog = new Color(defaultFog.r * 0.05f, defaultFog.g * 0.05f, defaultFog.b * 0.05f, 1f);

			CloudMaterial.SetFloat("_CloudSpeed", 0.03f);
			StarMaterial.SetFloat("_Exposure", 2.0f);

			StartCoroutine(WeatherSource.CloudChanger());
		}

		void Update ()
		{
			TimeSource.CalculateTimeProgress();

			// rotation
			// daily rotation of skybox night
			SkyboxNight.Rotate(Vector3.forward, 360f * TimeSource.DayProgressDelta, Space.Self);

			// daily rotation sun including correcting reduction for year progress
			SunPivot.Rotate(Vector3.forward, 360f * TimeSource.DayProgressDelta - 360f * TimeSource.YearProgressDelta, Space.Self);
			// yearly rotation of sun pivot's x axis from -23.4 to 23.4 degrees to aproximately simulate seasonal changes of sun's relative position
			SunPivot.Rotate(new Vector3(-latitude + 23.4f * (TimeSource.YearProgress * 2 - 1), SunPivot.eulerAngles.y, SunPivot.eulerAngles.z), Space.Self);

			// daily rotation of the moon
			// the moon looses per day about 50 minutes (time not angle) to the sun. 50min / 1440min = 0.03472222222... percent loss, therefore multiply by 1 - 0.03472222
			MoonBillboard.Rotate(Vector3.forward, 360f * TimeSource.DayProgressDelta * 0.96527778f, Space.Self);
			// yearly rotation of moon pivot's x axis from -23.4 to 23.4 degrees to aproximately simulate seasonal changes of moon's relative position + 5.14 for moon's orbital offset
			MoonBillboard.Rotate(new Vector3(-latitude + 23.4f * (TimeSource.YearProgress * 2 - 1) + 5.14f, SunPivot.eulerAngles.y, SunPivot.eulerAngles.z), Space.Self);


			// movement
			worldPos = PlayerManager.PlayerTransform.position - WorldMover.currentMove;
			transform.position = new Vector3(worldPos.x * .001f, 0, worldPos.z * .001f);


			Vector3 sunPos = Sun.transform.position - transform.position;
			Sun.intensity = Mathf.Clamp01(sunPos.y);
			Sun.color = Color.Lerp(new Color(1f, 0.5f, 0), Color.white, Sun.intensity);

			StarMaterial.SetFloat("_Visibility", (-Sun.intensity + 1) * .01f);

			MoonMaterial.SetFloat("_MoonDayNight", Mathf.Lerp(2.19f, 1.5f, Sun.intensity));
			MoonMaterial.SetFloat("_Exposure", Mathf.Lerp(2f, 4f, Sun.intensity));

			SkyMaterial.SetFloat("_Exposure", Mathf.Lerp(.01f, 1f, Sun.intensity));
			SkyMaterial.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.1f, 1f, Mathf.Clamp01(Sun.intensity * 10)));

			CloudMaterial.SetFloat("_CloudBright", Mathf.Lerp(.002f, .9f, Sun.intensity));
			CloudMaterial.SetFloat("_CloudGradient", Mathf.Lerp(.45f, .2f, Sun.intensity));

			RenderSettings.fogColor = Color.Lerp(nightFog, defaultFog, Sun.intensity);
			RenderSettings.ambientSkyColor = Color.Lerp(ambientNight, ambientDay, Sun.intensity);

			CloudMaterial.SetFloat("_ClearSky", WeatherSource.SkyClarity);
		}

		void OnDisable ()
		{
			StopCoroutine(WeatherSource.CloudChanger());
		}
	}
}
