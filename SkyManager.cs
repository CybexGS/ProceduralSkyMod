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
			StartCoroutine(WeatherSource.UpdateCloudRenderTex());
		}

		void Update (float deltaSeconds)
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

			TimeSource.CalculateTimeProgress(deltaSeconds);
			DateToSkyMapper.ApplyDate(TimeSource.GetCurrentTime());

			// rotations
			// daily rotation of skybox night
			SkyboxNight.localRotation = DateToSkyMapper.SkyboxNightRotation;

			// daily rotation of the sun
			SunPivot.localRotation = DateToSkyMapper.SunPivotRotation;
			// appropriate simulation of seasonal changes of sun's relative position
			SunPivot.localPosition = DateToSkyMapper.SunOffset;

			// daily rotation of the moon
			MoonBillboard.localRotation = DateToSkyMapper.MoonRotation;
			// TODO: seasonal offset for the moon too?

#if DEBUG
			DevGUI devGui = GetComponent<DevGUI>();
			if (devGui != null && devGui.posOverride)
			{
				devGui.CalculateRotationOverride(this);
				SkyboxNight.localRotation = devGui.skyRot;
				SunPivot.localRotation = devGui.sunRot;
				MoonBillboard.localRotation = devGui.moonRot;
			}
#endif

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
			CloudMaterial.SetFloat("_ClearSky", WeatherSource.SkyClarity);
#if DEBUG
			if (devGui != null && devGui.cloudOverride)
			{
				CloudMaterial.SetFloat("_NScale", devGui.cloudNoiseScale);
				CloudMaterial.SetFloat("_ClearSky", devGui.cloudClearSky);
				CloudMaterial.SetFloat("_CloudBright", devGui.cloudBrightness);
				CloudMaterial.SetFloat("_CloudSpeed", devGui.cloudSpeed);
				CloudMaterial.SetFloat("_CloudChange", devGui.cloudChange);
				CloudMaterial.SetFloat("_CloudGradient", devGui.cloudGradient);
			}
#endif

			RenderSettings.fogColor = Color.Lerp(nightFog, defaultFog, Sun.intensity);
			RenderSettings.ambientSkyColor = Color.Lerp(ambientNight, ambientDay, Sun.intensity);


			// TODO particle system
			// - rain amount
			// - color (fog color lightened)
			// - audio control (calc from rain intensity and render tex over player pos)
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
		public bool posOverride = false;
		public bool cloudOverride = false;

		private Quaternion cameraLockRot;

		private float sunRotOverride = 0;
		public Quaternion sunRot;
		private float skyRotOverride = 0;
		public Quaternion skyRot;
		private float moonRotOverride = 0;
		public Quaternion moonRot;

		public float cloudNoiseScale, cloudClearSky, cloudBrightness, cloudSpeed, cloudChange, cloudGradient;

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
		}

		public void CalculateRotationOverride (SkyManager mngr)
		{
			Vector3 euler = mngr.SunPivot.eulerAngles;
			sunRot = Quaternion.Euler(new Vector3(euler.x, euler.y, 360f * sunRotOverride));
			euler = mngr.SkyboxNight.eulerAngles;
			skyRot = Quaternion.Euler(new Vector3(euler.x, euler.y, 360f * skyRotOverride));
			euler = mngr.MoonBillboard.eulerAngles;
			moonRot = Quaternion.Euler(new Vector3(euler.x, euler.y, 360f * moonRotOverride));
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

			GUILayout.BeginVertical();

			// cloud render box
			GUILayout.BeginVertical(GUI.skin.box);

			Texture2D tex;
			Rect r;
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

			GUILayout.EndVertical();


			GUILayout.Space(10);
			// sky override box
			GUILayout.BeginVertical(GUI.skin.box);

			posOverride = GUILayout.Toggle(posOverride, "Position Override");
			if (!posOverride) GUI.enabled = false;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Sun Pivot");
			GUILayout.Label(sunRotOverride.ToString("n2"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			sunRotOverride = GUILayout.HorizontalSlider(sunRotOverride, 0, 1);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Starbox");
			GUILayout.Label(skyRotOverride.ToString("n2"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			skyRotOverride = GUILayout.HorizontalSlider(skyRotOverride, 0, 1);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Moon");
			GUILayout.Label(moonRotOverride.ToString("n2"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			moonRotOverride = GUILayout.HorizontalSlider(moonRotOverride, 0, 1);

			GUI.enabled = true;

			GUILayout.EndVertical();


			GUILayout.Space(10);
			// cloud override box
			GUILayout.BeginVertical(GUI.skin.box);

			cloudOverride = GUILayout.Toggle(cloudOverride, "Cloud Override");
			if (!cloudOverride) GUI.enabled = false;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Noise Scale");
			GUILayout.Label(cloudNoiseScale.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			cloudNoiseScale = GUILayout.HorizontalSlider(cloudNoiseScale, 1, 8);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Clear Sky");
			GUILayout.Label(cloudClearSky.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			cloudClearSky = GUILayout.HorizontalSlider(cloudClearSky, 0, 10);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Brightness");
			GUILayout.Label(cloudBrightness.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			cloudBrightness = GUILayout.HorizontalSlider(cloudBrightness, 0, 1);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Speed");
			GUILayout.Label(cloudSpeed.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			cloudSpeed = GUILayout.HorizontalSlider(cloudSpeed, 0.01f, 0.5f);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Change");
			GUILayout.Label(cloudChange.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			cloudChange = GUILayout.HorizontalSlider(cloudChange, 0.1f, 0.5f);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Gradient");
			GUILayout.Label(cloudGradient.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			cloudGradient = GUILayout.HorizontalSlider(cloudGradient, 0, 0.5f);

			GUI.enabled = true;

			GUILayout.EndVertical();



			GUILayout.EndVertical();
		}
	}
#endif
}
