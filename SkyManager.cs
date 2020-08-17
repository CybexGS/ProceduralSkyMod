﻿using System;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class SkyManager : MonoBehaviour
	{
		private Color ambientDay = new Color(.282f, .270f, .243f, 1f);
		private Color ambientNight = new Color(.079f, .079f, .112f, 1f);
		private Color defaultFog, nightFog;

		private Vector3 worldPos;

		public Transform SkyboxNight { get; set; }
		public Transform SunPathCenter { get; set; }
		public Transform MoonPathCenter { get; set; }

		public Light SunLight { get; set; }
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

#if !CYBEX_TIME
			// fauxnik time algo
			TimeSource.CalculateTimeProgress(Time.deltaTime);
			var currentTime = TimeSource.GetCurrentTime();
#if DEBUG
			DevGUI devGui = GetComponent<DevGUI>();
			if (devGui != null && devGui.dateTimeOverride)
			{
				currentTime = devGui.CurrentTime;
			}
#endif

			DateToSkyMapper.ApplyDate(currentTime);

			// daily & yearly rotation of skybox night
			SkyboxNight.localRotation = DateToSkyMapper.SkyboxNightRotation;

			// daily & seasonal rotation of the sun
			SunPathCenter.parent.localRotation = DateToSkyMapper.SunPivotRotation;
			SunPathCenter.localPosition = DateToSkyMapper.SunOffsetFromPath;

			// daily & seasonal rotation of the moon
			MoonPathCenter.parent.localRotation = DateToSkyMapper.MoonPivotRotation;
			MoonPathCenter.localPosition = DateToSkyMapper.MoonOffsetFromPath;
#else
			// cybex time algo
			CybexTime.CalculateTimeProgress(Main.settings.latitude, 0);

			// rotations
			SkyboxNight.localRotation = Quaternion.Euler(CybexTime.SkyboxNightRotation);
			SunPathCenter.parent.localRotation = Quaternion.Euler(CybexTime.SunPivotRotation);
			MoonPathCenter.parent.localRotation = Quaternion.Euler(CybexTime.MoonRotation);

#if DEBUG
			// TODO: update this
			DevGUI devGui = GetComponent<DevGUI>();
			if (devGui != null && devGui.posOverride)
			{
				devGui.CalculateRotationOverride(this);
				SkyboxNight.localRotation = devGui.skyRot;
				SunPathCenter.localRotation = devGui.sunRot;
				MoonPathCenter.localRotation = devGui.moonRot;
			}
#endif
#endif

			// movement
			worldPos = PlayerManager.PlayerTransform.position - WorldMover.currentMove;
			transform.position = new Vector3(worldPos.x * .001f, 0, worldPos.z * .001f);

			Vector3 highLatitudeCorrection = SunPathCenter.parent.TransformVector(SunPathCenter.localPosition) - SunPathCenter.parent.position / DateToSkyMapper.maxProjectedSunOffset;
			Vector3 sunPos = SunLight.transform.position - SunPathCenter.position + highLatitudeCorrection;
			SunLight.intensity = Mathf.Clamp01(sunPos.y);
			SunLight.color = Color.Lerp(new Color(1f, 0.5f, 0), Color.white, SunLight.intensity);
			Debug.Log($"sun distance above sky cam {sunPos.y}");

			StarMaterial.SetFloat("_Visibility", (-SunLight.intensity + 1) * .01f);

			MoonMaterial.SetFloat("_MoonDayNight", Mathf.Lerp(2.19f, 1.5f, SunLight.intensity));
			// gives aproximate moon phase
			MoonMaterial.SetFloat("_MoonPhase", Vector3.SignedAngle(SunPathCenter.right, MoonPathCenter.right, SunPathCenter.forward) / 180);
			MoonMaterial.SetFloat("_Exposure", Mathf.Lerp(2f, 4f, SunLight.intensity));

			SkyMaterial.SetFloat("_Exposure", Mathf.Lerp(.01f, 1f, SunLight.intensity));
			SkyMaterial.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.1f, 1f, Mathf.Clamp01(SunLight.intensity * 10)));

			CloudMaterial.SetFloat("_CloudBright", Mathf.Lerp(.002f, .9f, SunLight.intensity));
			CloudMaterial.SetFloat("_CloudGradient", Mathf.Lerp(.45f, .2f, SunLight.intensity));
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

			RenderSettings.fogColor = Color.Lerp(nightFog, defaultFog, SunLight.intensity);
			RenderSettings.ambientSkyColor = Color.Lerp(ambientNight, ambientDay, SunLight.intensity);


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
		public bool dateTimeOverride = false;
		public bool posOverride = false;
		public bool cloudOverride = false;

		private Quaternion cameraLockRot;

		private int yearOverride = 2020;
		private int monthOverride = 8;
		private int dayOverride = 16;
		private float dayProgressOverride = 12;
		public DateTime CurrentTime { get => new DateTime(yearOverride, monthOverride, dayOverride).AddHours(dayProgressOverride); }

		private float sunRotOverride = 0;
		public Quaternion sunRot;
		private float skyRotOverride = 0;
		public Quaternion skyRot;
		private float moonRotOverride = 0;
		public Quaternion moonRot;

		public float cloudNoiseScale, cloudClearSky, cloudBrightness, cloudSpeed, cloudChange, cloudGradient;

		void Update ()
		{
			if (Input.GetKeyDown(KeyCode.KeypadDivide))
			{
				active = !active;
				if (!active) SwitchCamLock(false);
			}

			if (Input.GetKeyDown(KeyCode.KeypadMultiply))
			{
				if (!active) return;
				SwitchCamLock(!camLocked);
			}

			if (camLocked) Camera.main.transform.rotation = cameraLockRot;
		}

		public void CalculateRotationOverride (SkyManager mngr)
		{
			Vector3 euler = mngr.SunPathCenter.eulerAngles;
			sunRot = Quaternion.Euler(new Vector3(euler.x, euler.y, 360f * sunRotOverride));
			euler = mngr.SkyboxNight.eulerAngles;
			skyRot = Quaternion.Euler(new Vector3(euler.x, euler.y, 360f * skyRotOverride));
			euler = mngr.MoonPathCenter.eulerAngles;
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

#if !CYBEX_TIME
			GUILayout.Space(10);
			// date & time override box (fauxnik time algo compatible)
			GUILayout.BeginVertical(GUI.skin.box);

			dateTimeOverride = GUILayout.Toggle(dateTimeOverride, "Date/Time Override");
			if (!dateTimeOverride) GUI.enabled = false;

			GUILayout.Label("Date");
			GUILayout.BeginHorizontal();
			GUILayout.Label("Year:");
			yearOverride = int.Parse(GUILayout.TextField(yearOverride.ToString()));
			GUILayout.Label("Month:");
			monthOverride = Mathf.Clamp(int.Parse(GUILayout.TextField(monthOverride.ToString())), 1, 12);
			GUILayout.Label("Day:");
			dayOverride = Mathf.Clamp(int.Parse(GUILayout.TextField(dayOverride.ToString())), 1, new DateTime(yearOverride, monthOverride % 12 + 1, 1).AddDays(-1).Day);
			GUILayout.EndHorizontal();
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Time");
			GUILayout.Label(dayProgressOverride.ToString("n2"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			dayProgressOverride = GUILayout.HorizontalSlider(dayProgressOverride, 0, 24);

			GUI.enabled = true;

			GUILayout.EndVertical();

#else
			GUILayout.Space(10);
			// sky override box (cybex time algo compatible)
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
#endif

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
