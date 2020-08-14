using System.Collections;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class SkyManager : MonoBehaviour
	{
		private Color ambientDay = new Color(.282f, .270f, .243f, 1f);
		private Color ambientNight = new Color(.079f, .079f, .112f, 1f);
		private Color defaultFog, nightFog;

		public float latitude = 0f;

		private Vector3 worldPos;

		public static float DayLengthInSeconds { get => Main.settings.dayLengthMinutesRT * 60f; }

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

			// load data from file, put this in initializer?
			SkySaveData saveData = SkySaveLoad.Load();
			TimeSource.DayProgress = saveData.dayProgress;
			TimeSource.YearProgress = saveData.yearProgress;
			TimeSource.SkyboxNightRotation = saveData.skyRotation;
			TimeSource.SunPivotRotation = saveData.sunRotation;
			TimeSource.MoonRotation = saveData.moonRotation;

			StartCoroutine(WeatherSource.CloudChanger());
			StartCoroutine(WeatherSource.UpdateCloudRenderTex());
		}

		void Update ()
		{
			// <<<<<<<<<< <<<<<<<<<< WORKS AS POC >>>>>>>>>> >>>>>>>>>>
			//
			//Sun.cookieSize = 1000;
			//Texture2D tex = new Texture2D(WeatherSource.CloudRenderImage2.width, WeatherSource.CloudRenderImage2.height);
			//Graphics.CopyTexture(WeatherSource.CloudRenderImage2, tex);
			//for (int x = 0; x < tex.width; x++)
			//{
			//	for (int y = 0; y < tex.height; y++)
			//	{
			//		tex.SetPixel(x, y, new Color(1, 1, 1, 1 - tex.GetPixel(x, y).a));
			//	}
			//}
			//tex.Apply();
			//Sun.cookie = tex;
			//
			// <<<<<<<<<< <<<<<<<<<< WORKS AS POC >>>>>>>>>> >>>>>>>>>>

			TimeSource.CalculateTimeProgress();

			// rotations
			// daily rotation of skybox night
			SkyboxNight.localRotation = Quaternion.Euler(TimeSource.SkyboxNightRotation);

			// daily rotation of the sun
			SunPivot.localRotation = Quaternion.Euler(TimeSource.SunPivotRotation);
			// yearly rotation of sun pivot's x axis from -23.4 to 23.4 degrees to aproximately simulate seasonal changes of sun's relative position
			SunPivot.localRotation = Quaternion.Euler(new Vector3(-latitude + 23.4f * (TimeSource.YearProgress * 2 - 1), SunPivot.eulerAngles.y, SunPivot.eulerAngles.z));

			// daily rotation of the moon
			MoonBillboard.localRotation = Quaternion.Euler(TimeSource.MoonRotation);
			// yearly rotation of moon pivot's x axis from -23.4 to 23.4 degrees to aproximately simulate seasonal changes of moon's relative position + 5.14 for moon's orbital offset
			MoonBillboard.localRotation = Quaternion.Euler(new Vector3(-latitude + 23.4f * (TimeSource.YearProgress * 2 - 1) + 5.14f, MoonBillboard.eulerAngles.y, MoonBillboard.eulerAngles.z));


			// movement
			worldPos = PlayerManager.PlayerTransform.position - WorldMover.currentMove;
			transform.position = new Vector3(worldPos.x * .001f, 0, worldPos.z * .001f);


			Vector3 sunPos = Sun.transform.position - transform.position;
			Sun.intensity = Mathf.Clamp01(sunPos.y);
			Sun.color = Color.Lerp(new Color(1f, 0.5f, 0), Color.white, Sun.intensity);

			StarMaterial.SetFloat("_Visibility", (-Sun.intensity + 1) * .01f);

			MoonMaterial.SetFloat("_MoonDayNight", Mathf.Lerp(2.19f, 1.5f, Sun.intensity));
			// gives aproximate moon phase
			MoonMaterial.SetFloat("_MoonPhase", Vector3.SignedAngle(SunPivot.right, MoonBillboard.right, SunPivot.forward) / 180);
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
			StopCoroutine(WeatherSource.UpdateCloudRenderTex());
		}
	}

#if DEBUG
	public class DevGUI : MonoBehaviour
	{
		public bool active = true;
		public bool camLocked = false;

		private Quaternion cameraLockRot;

		private bool posOverride = false;
		private float sunRotOverride = 0;
		private float skyRotOverride = 0;
		private float moonRotOverride = 0;

		private SkyManager skyManager = null;

		void Update ()
		{
			if (Input.GetKeyDown(KeyCode.Keypad1))
			{
				active = !active;
				if (!active) SwitchCamLock(false);
			}

			if (Input.GetKeyDown(KeyCode.Keypad2))
			{
				if (!active) return;
				SwitchCamLock(!camLocked);
			}

			if (camLocked) Camera.main.transform.rotation = cameraLockRot;

			if (posOverride)
			{
				if (skyManager == null) skyManager = GetComponent<SkyManager>();

				//Vector3 euler = skyManager.SunPivot.eulerAngles;
				//skyManager.SunPivot.localRotation = Quaternion.Euler(new Vector3(euler.x, euler.y, 360f * sunRotOverride));
				//euler = skyManager.SkyboxNight.eulerAngles;
				//skyManager.SkyboxNight.localRotation = Quaternion.Euler(new Vector3(euler.x, euler.y, 360f * skyRotOverride));
				//euler = skyManager.MoonBillboard.eulerAngles;
				//skyManager.MoonBillboard.localRotation = Quaternion.Euler(new Vector3(euler.x, euler.y, 360f * moonRotOverride));
			}
		}

		private void SwitchCamLock (bool state = false)
		{
			cameraLockRot = Camera.main.transform.rotation;
			Cursor.visible = camLocked = state;
			Cursor.lockState = (state) ? CursorLockMode.None : CursorLockMode.Locked;
		}

		void OnGUI ()
		{
			if (!active) return;

			Texture2D tex;
			Rect r;

			GUILayout.BeginVertical(GUI.skin.box);

			GUILayout.Label("PS 0: " + WeatherSource.RainParticleSystems[0].gameObject.name);
			tex = WeatherSource.RainParticleSystems[0].shape.texture;
			r = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
			GUI.DrawTexture(r, tex);

			GUILayout.Label("PS 1: " + WeatherSource.RainParticleSystems[1].gameObject.name);
			tex = WeatherSource.RainParticleSystems[1].shape.texture;
			r = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
			GUI.DrawTexture(r, tex);

			GUILayout.Label("PS 2: " + WeatherSource.RainParticleSystems[2].gameObject.name);
			tex = WeatherSource.RainParticleSystems[2].shape.texture;
			r = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
			GUI.DrawTexture(r, tex);

			GUILayout.Label("RenderTex");
			if (WeatherSource.CloudRenderImage2 == null) return;
			r = GUILayoutUtility.GetRect(256, 256);
			GUI.DrawTexture(r, WeatherSource.CloudRenderImage2);

			GUILayout.Space(20);
			posOverride = GUILayout.Toggle(posOverride, "Position Override");
			if (!posOverride) GUI.enabled = false;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Sun Pivot", GUILayout.Width(50), GUILayout.ExpandWidth(false));
			sunRotOverride = GUILayout.HorizontalSlider(sunRotOverride, 0, 1);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Starbox", GUILayout.Width(50), GUILayout.ExpandWidth(false));
			skyRotOverride = GUILayout.HorizontalSlider(skyRotOverride, 0, 1);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Moon", GUILayout.Width(50), GUILayout.ExpandWidth(false));
			moonRotOverride = GUILayout.HorizontalSlider(moonRotOverride, 0, 1);
			GUILayout.EndHorizontal();

			GUI.enabled = true;

			GUILayout.EndVertical();
		}
	}
#endif
}
