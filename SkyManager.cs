using System;
using UnityEngine;
#if DEBUG
using System.IO;
using System.Xml;
#endif

namespace ProceduralSkyMod
{
	public class SkyManager : MonoBehaviour
	{
		private Color ambientDay = new Color(.282f, .270f, .243f, 1f);
		private Color ambientNight = new Color(.079f, .079f, .112f, 1f);
		private Color defaultFog, nightFog;
		private float defaultFogDensity;

		private Vector3 worldPos;

		public Transform SkyboxNight { get; set; }
		public Transform SunPathCenter { get; set; }
		public Transform MoonPathCenter { get; set; }

		public Light SunLight { get; set; }
		public Material StarMaterial { get; set; }
		public Material SkyMaterial { get; set; }
		public Material CloudMaterial { get; set; }
		public Material MoonMaterial { get; set; }

		public Camera SkyCam { get; set; }
		public RenderTexture SkyCamTex { get; set; }
		public Camera ClearCam { get; set; }
		public RenderTexture ClearCamTex { get; set; }
		public Transform CloudPlane { get; set; }

		void Start ()
		{
			defaultFog = RenderSettings.fogColor;
			nightFog = new Color(defaultFog.r * 0.05f, defaultFog.g * 0.05f, defaultFog.b * 0.05f, 1f);
			defaultFogDensity = RenderSettings.fogDensity;

			CloudMaterial.SetFloat("_CloudSpeed", 0.03f);
			StarMaterial.SetFloat("_Exposure", 2.0f);

			WeatherSource.CurrentWeatherState = WeatherState.LoadFromXML(WeatherSource.XMLWeatherStatePath + "PSWS_FALLBACK");
			WeatherSource.NextWeatherState = null;
			WeatherSource.WeatherStateBlending = 0;

			StartCoroutine(WeatherSource.WeatherStateChanger());
			StartCoroutine(WeatherSource.UpdateCloudRenderTex());
			StartCoroutine(ReflectionProbeUpdater.UpdateProbe());
		}

		void Update ()
		{
			if (Main.settings.cloudShadowsEnabled)
				SunLight.cookie = WeatherSource.SunShadowRenderImage;
			else
				SunLight.cookie = null;

			ClearCam.RenderToCubemap(ClearCamTex, 63 & ~(1 << (int)CubemapFace.NegativeY));
			SkyCam.RenderToCubemap(SkyCamTex, 63 & ~(1 << (int)CubemapFace.NegativeY));

#if !CYBEX_TIME
			// fauxnik time algo
			ProceduralSkyTimeSource.Instance.CalculateTimeProgress(Time.deltaTime);
			var currentTime = TimeSourceAdapter.GetCurrentTime();
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
				devGui.CalculateRotationOverride();
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
			float sunOverHorizonFac = Mathf.Clamp01(sunPos.y);
			SunLight.intensity = sunOverHorizonFac * 1.5f;
			SunLight.color = Color.Lerp(new Color(1f, 0.5f, 0), Color.white, sunOverHorizonFac);

			StarMaterial.SetFloat("_Visibility", Mathf.Clamp01(-4 * sunOverHorizonFac + 1) * 1f);
			StarMaterial.SetFloat("_Exposure", .5f);

			MoonMaterial.SetFloat("_MoonDayNight", Mathf.Lerp(2.19f, 1.5f, sunOverHorizonFac));
			// gives aproximate moon phase
			MoonMaterial.SetFloat("_MoonPhase", Vector3.SignedAngle(SunPathCenter.right, MoonPathCenter.right, SunPathCenter.forward) / 180);
			MoonMaterial.SetFloat("_Exposure", Mathf.Lerp(2f, 4f, sunOverHorizonFac));

			SkyMaterial.SetFloat("_Exposure", Mathf.Lerp(.01f, 1f, sunOverHorizonFac));
			SkyMaterial.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.1f, 1f, Mathf.Clamp01(sunOverHorizonFac * 10)));

			CloudMaterial.SetFloat("_NScale", WeatherSource.CloudNoiseScaleBlend);
			CloudMaterial.SetFloat("_ClearSky", WeatherSource.CloudClearSkyBlend);
			float facC = Mathf.Lerp(.002f, 1f, sunOverHorizonFac);
			CloudMaterial.SetFloat("_CloudBright", WeatherSource.CloudBrightnessBlend * facC);
			float facG = Mathf.Lerp(.25f, 0.0f, sunOverHorizonFac);
			CloudMaterial.SetFloat("_CloudGradient", WeatherSource.CloudGradientBlend + facG);
			CloudMaterial.SetFloat("_CloudSpeed", WeatherSource.CloudSpeedBlend);
			CloudMaterial.SetFloat("_CloudChange", WeatherSource.CloudChangeBlend);
#if DEBUG
			if (devGui != null && devGui.cloudOverride)
			{
				CloudMaterial.SetFloat("_NScale", devGui.loadedWeatherState.cloudNoiseScale);
				CloudMaterial.SetFloat("_ClearSky", devGui.loadedWeatherState.cloudClearSky);
				CloudMaterial.SetFloat("_CloudBright", devGui.loadedWeatherState.cloudBrightness * facC);
				CloudMaterial.SetFloat("_CloudGradient", devGui.loadedWeatherState.cloudGradient + facG);
				CloudMaterial.SetFloat("_CloudSpeed", devGui.loadedWeatherState.cloudSpeed);
				CloudMaterial.SetFloat("_CloudChange", devGui.loadedWeatherState.cloudChange);
			}
#endif

			RenderSettings.fogColor = Color.Lerp(nightFog, defaultFog, sunOverHorizonFac);
			RenderSettings.ambientSkyColor = Color.Lerp(ambientNight, ambientDay, sunOverHorizonFac);
			
#if DEBUG
			if (devGui != null && devGui.rainOverride)
			{
				RenderSettings.fogDensity = Mathf.Lerp(defaultFogDensity, defaultFogDensity * 3, devGui.loadedWeatherState.rainParticleStrength);
				RainController.SetRainStrength(devGui.loadedWeatherState.rainParticleStrength);
			}
			else
			{
				RenderSettings.fogDensity = Mathf.Lerp(defaultFogDensity, defaultFogDensity * 3, WeatherSource.RainStrengthBlend);
				RainController.SetRainStrength(WeatherSource.RainStrengthBlend);
			}
#else
			RenderSettings.fogDensity = Mathf.Lerp(defaultFogDensity, defaultFogDensity * 3, WeatherSource.RainStrengthBlend);
			RainController.SetRainStrength(WeatherSource.RainStrengthBlend);
#endif

			RainController.SetRainColor(new Color(RenderSettings.fogColor.r + 0.5f, RenderSettings.fogColor.g + 0.5f, RenderSettings.fogColor.b + 0.5f, 1));
		}

		void OnDisable ()
		{
			StopCoroutine(WeatherSource.WeatherStateChanger());
			StopCoroutine(WeatherSource.UpdateCloudRenderTex());
			StopCoroutine(ReflectionProbeUpdater.UpdateProbe());
		}
	}



#if DEBUG
	public class DevGUI : MonoBehaviour
	{
		private Color defaultGUIColor;

		public bool active = true;
		public bool camLocked = false;
		private Quaternion cameraLockRot;

		public bool dateTimeOverride = false, posOverride = false, cloudOverride = false, timeOverride = false, rainOverride = false;

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

		public WeatherState loadedWeatherState;
		private bool creatingNewWeatherStateFile;
		private string newWeatherFileName;
		private bool newFileNameError;
		private readonly char[] forbiddenChar = new char[] { '.', '<','>',':',',',';','"','|','?','*', Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar };
		private bool showFileSelection;

		private SkyManager mngr = null;

		void Start ()
		{
			if (!Directory.Exists(WeatherSource.XMLWeatherStatePath)) Directory.CreateDirectory(WeatherSource.XMLWeatherStatePath);

			try
			{
				LoadWeatherStateFromXML(WeatherSource.XMLWeatherStatePath + "PSWS_FALLBACK");
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"Failed to load WeatherState \n {ex}");
				throw;
			}
		}

		void Update ()
		{
			if (mngr == null) mngr = GetComponent<SkyManager>();

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

		public void CalculateRotationOverride ()
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

		private void LoadWeatherStateFromXML (string filepath)
		{
			if (!string.IsNullOrEmpty(filepath)) loadedWeatherState = WeatherState.LoadFromXML(filepath);
			showFileSelection = false;
		}

		void OnGUI ()
		{
			if (!active) return;
			Event guiEvent = Event.current;
			defaultGUIColor = GUI.color;

			GUILayout.BeginHorizontal(); // total start
			GUILayout.Space(3);

			GUILayout.BeginVertical(GUILayout.Width(256)); // row 0 begin
			GUILayout.Space(3);

#if !CYBEX_TIME
			// date & time override box (fauxnik time algo compatible)
			GUILayout.BeginVertical(GUI.skin.box);

			dateTimeOverride = GUILayout.Toggle(dateTimeOverride, "Date/Time Override");
			if (!dateTimeOverride) GUI.enabled = false;

			GUILayout.Label("Date");
			GUILayout.BeginHorizontal();
			GUILayout.Label("Year");
			int year = (dateTimeOverride) ? yearOverride : ProceduralSkyTimeSource.Instance.LocalTime.Year;
			yearOverride = int.Parse(GUILayout.TextField(year.ToString(), GUILayout.Width(40)));
			GUILayout.Label("Month");
			int month = (dateTimeOverride) ? monthOverride : ProceduralSkyTimeSource.Instance.LocalTime.Month;
			monthOverride = Mathf.Clamp(int.Parse(GUILayout.TextField(monthOverride.ToString(), GUILayout.Width(20))), 1, 12);
			GUILayout.Label("Day");
			int day = (dateTimeOverride) ? dayOverride : ProceduralSkyTimeSource.Instance.LocalTime.Day;
			dayOverride = Mathf.Clamp(int.Parse(GUILayout.TextField(day.ToString(), GUILayout.Width(20))), 1, new DateTime(yearOverride, monthOverride % 12 + 1, 1).AddDays(-1).Day);
			GUILayout.EndHorizontal();
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Time");
			float time = (dateTimeOverride) ? dayProgressOverride : ProceduralSkyTimeSource.Instance.LocalTime.Hour + ProceduralSkyTimeSource.Instance.LocalTime.Minute * 0.01f;
			GUILayout.Label(time.ToString("n2"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			dayProgressOverride = GUILayout.HorizontalSlider(dayProgressOverride, 0, 24);

			GUI.enabled = true;

			GUILayout.EndVertical(); // date & time override box end

#else
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

			GUILayout.EndVertical(); // sky override box end


			// time override box (cybex time algo compatible)
			GUILayout.BeginVertical(GUI.skin.box);

			timeOverride = GUILayout.Toggle(timeOverride, "Time Override");
			if (!timeOverride) GUI.enabled = false;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Year");
			GUILayout.Label(TimeSource.YearProgress.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			CybexTime.YearProgress = GUILayout.HorizontalSlider(CybexTime.YearProgress, 0, 1.01f);
			GUILayout.Space(2);

			GUI.enabled = true;

			GUILayout.EndVertical(); // time override box end
#endif

			// cloud override box
			GUILayout.BeginVertical(GUI.skin.box);

			bool newCloudOverride = GUILayout.Toggle(cloudOverride, "Cloud Override");
			if (newCloudOverride != cloudOverride)
			{
				if(newCloudOverride == true)
				{
					loadedWeatherState.cloudBrightness = WeatherSource.CloudBrightnessBlend;
					loadedWeatherState.cloudChange = WeatherSource.CloudChangeBlend;
					loadedWeatherState.cloudClearSky = WeatherSource.CloudClearSkyBlend;
					loadedWeatherState.cloudGradient = WeatherSource.CloudGradientBlend;
					loadedWeatherState.cloudNoiseScale = WeatherSource.CloudNoiseScaleBlend;
					loadedWeatherState.cloudSpeed = WeatherSource.CloudSpeedBlend;
				}

				cloudOverride = newCloudOverride;
			}
			if (!cloudOverride) GUI.enabled = false;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Noise Scale");
			float cloudNoiseScale = (cloudOverride) ? loadedWeatherState.cloudNoiseScale : WeatherSource.CloudNoiseScaleBlend;
			GUILayout.Label(cloudNoiseScale.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			loadedWeatherState.cloudNoiseScale = GUILayout.HorizontalSlider(cloudNoiseScale, 1, 8);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Clear Sky");
			float cloudClearSky = (cloudOverride) ? loadedWeatherState.cloudClearSky : WeatherSource.CloudClearSkyBlend;
			GUILayout.Label(cloudClearSky.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			loadedWeatherState.cloudClearSky = GUILayout.HorizontalSlider(cloudClearSky, 0, 10);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Brightness");
			float cloudBrightness = (cloudOverride) ? loadedWeatherState.cloudBrightness : WeatherSource.CloudBrightnessBlend;
			GUILayout.Label(cloudBrightness.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			loadedWeatherState.cloudBrightness = GUILayout.HorizontalSlider(cloudBrightness, 0, 1);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Speed");
			float cloudSpeed = (cloudOverride) ? loadedWeatherState.cloudSpeed : WeatherSource.CloudSpeedBlend;
			GUILayout.Label(cloudSpeed.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			loadedWeatherState.cloudSpeed = GUILayout.HorizontalSlider(cloudSpeed, 0.001f, 0.2f);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Change");
			float cloudChange = (cloudOverride) ? loadedWeatherState.cloudChange : WeatherSource.CloudChangeBlend;
			GUILayout.Label(cloudChange.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			loadedWeatherState.cloudChange = GUILayout.HorizontalSlider(cloudChange, 0.1f, 0.5f);
			GUILayout.Space(2);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Gradient");
			float cloudGradient = (cloudOverride) ? loadedWeatherState.cloudGradient : WeatherSource.CloudGradientBlend;
			GUILayout.Label(cloudGradient.ToString("n4"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			loadedWeatherState.cloudGradient = GUILayout.HorizontalSlider(cloudGradient, 0, 0.5f);

			GUI.enabled = true;

			GUILayout.EndVertical(); // cloud override box end

			// rain override box
			GUILayout.BeginVertical(GUI.skin.box);

			bool newRainOverride = GUILayout.Toggle(rainOverride, "Rain Override");
			if (newRainOverride != rainOverride)
			{
				if (newRainOverride == true)
				{
					loadedWeatherState.rainParticleStrength = WeatherSource.RainStrengthBlend;
				}

				rainOverride = newRainOverride;
			}
			if (!rainOverride) GUI.enabled = false;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Rain Strength");
			float rainStrength = (rainOverride) ? loadedWeatherState.rainParticleStrength : WeatherSource.RainStrengthBlend;
			GUILayout.Label(rainStrength.ToString("n2"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			loadedWeatherState.rainParticleStrength = GUILayout.HorizontalSlider(rainStrength, 0, 1f);
			GUILayout.BeginHorizontal();
			GUILayout.Label("System 0 (Rain Drop)");
			GUILayout.Label(((int)RainController.RainParticleSystems[0].emission.rateOverTime.constant).ToString(), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("System 1 (Rain Cluster)");
			GUILayout.Label(((int)RainController.RainParticleSystems[1].emission.rateOverTime.constant).ToString(), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("System 2 (Rain Haze)");
			GUILayout.Label(((int)RainController.RainParticleSystems[2].emission.rateOverTime.constant).ToString(), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Audio Volume");
			GUILayout.Label(RainController.RainAudio.volume.ToString("n2"), GUILayout.Width(50), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			GUI.enabled = true;

			GUILayout.EndVertical(); // rain override box end

			// weather state box
			GUILayout.BeginVertical(GUI.skin.box);

			GUILayout.Label("Weather State");

			GUILayout.BeginHorizontal();
			GUILayout.Label("Change Probability");
			GUILayout.Label(WeatherSource.WeatherChangeProbability.ToString("n2"), GUILayout.Width(80), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Last RND Change");
			GUILayout.Label(WeatherSource.LastRNDWeatherChange.ToString("n2"), GUILayout.Width(80), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Current State");
			GUILayout.Label(WeatherSource.CurrentWeatherState?.name ?? "NULL", GUILayout.Width(80), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Next State");
			GUILayout.Label(WeatherSource.NextWeatherState?.name ?? "NULL", GUILayout.Width(80), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Blend");
			GUILayout.Label(WeatherSource.WeatherStateBlending.ToString("n6"), GUILayout.Width(80), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			GUILayout.EndVertical(); // weather state box end

			// xml weather state handler
			GUILayout.BeginVertical(GUI.skin.box);

			GUILayout.Label("XML Weather States");
			GUILayout.BeginHorizontal();
			GUIContent btn = new GUIContent("New", "Create new file from current values");
			if (GUILayout.Button(btn))
			{
				creatingNewWeatherStateFile = true;
				newWeatherFileName = "New File";
				loadedWeatherState = new WeatherState(newWeatherFileName, loadedWeatherState);
				loadedWeatherState.name = "State Name";
			}
			btn = new GUIContent("Save", "Save current values to loaded file");
			if (GUILayout.Button(btn))
			{
				if (!creatingNewWeatherStateFile)
				{
					if (loadedWeatherState.fileName == "FALLBACK") return;
					WeatherState.CreateNewXML(loadedWeatherState);
				}
				else
				{
					if (!newFileNameError)
					{
						loadedWeatherState = new WeatherState(newWeatherFileName, loadedWeatherState);
						WeatherState.CreateNewXML(loadedWeatherState);
						creatingNewWeatherStateFile = false;
					}
				}
			}
			btn = new GUIContent("Load", "Load existing file");
			if (GUILayout.Button(btn))
			{
				showFileSelection = true;
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Filename");
			if (!creatingNewWeatherStateFile)
			{
				GUILayout.Label(loadedWeatherState.fileName ?? "None Loaded", GUILayout.Width(120));
			}
			else
			{
				bool fileExists = File.Exists(WeatherSource.XMLWeatherStatePath + newWeatherFileName);
				newFileNameError = false;
				if (fileExists || newWeatherFileName.IndexOfAny(forbiddenChar) != -1 || string.IsNullOrEmpty(newWeatherFileName))
				{
					newFileNameError = true;
					GUI.color = Color.red;
				}
				newWeatherFileName = GUILayout.TextField(newWeatherFileName, GUILayout.Width(120));
				GUI.color = defaultGUIColor;
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("State Name");
			if (loadedWeatherState == null) GUI.enabled = false;
			loadedWeatherState.name = GUILayout.TextField(loadedWeatherState.name, GUILayout.Width(120));
			GUI.enabled = true;
			GUILayout.EndHorizontal();

			if (showFileSelection) DevGUIFileSelection.Show(WeatherSource.XMLWeatherStatePath, LoadWeatherStateFromXML);

			GUILayout.EndVertical();// xml weather state handler end


			GUILayout.EndVertical(); // row 0 end
			GUILayout.Space(10);
			GUILayout.BeginVertical(); // row 1 begin

			//// moon observer
			//GUILayout.BeginVertical(GUI.skin.box);

			//GUILayout.Label("Moon Observer");
			//GUILayout.Space(2);
			//GUILayout.Label("Transform");
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("Position", GUILayout.Width(80));
			//GUILayout.Label(mngr.MoonBillboard.position.x.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.MoonBillboard.position.y.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.MoonBillboard.position.z.ToString("n2"), GUILayout.Width(40));
			//GUILayout.EndHorizontal();
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("Roatation", GUILayout.Width(80));
			//GUILayout.Label(mngr.MoonBillboard.eulerAngles.x.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.MoonBillboard.eulerAngles.y.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.MoonBillboard.eulerAngles.z.ToString("n2"), GUILayout.Width(40));
			//GUILayout.EndHorizontal();
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("Scale", GUILayout.Width(80));
			//GUILayout.Label(mngr.MoonBillboard.localScale.x.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.MoonBillboard.localScale.y.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.MoonBillboard.localScale.z.ToString("n2"), GUILayout.Width(40));
			//GUILayout.EndHorizontal();

			//GUILayout.EndVertical(); // moon observer end

			//// sun observer
			//GUILayout.BeginVertical(GUI.skin.box);

			//GUILayout.Label("Sun Observer");
			//GUILayout.Space(2);
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("Pivot Rotation", GUILayout.Width(120));
			//GUILayout.Label(mngr.SunPathCenter.parent.eulerAngles.x.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.SunPathCenter.parent.eulerAngles.y.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.SunPathCenter.parent.eulerAngles.z.ToString("n2"), GUILayout.Width(40));
			//GUILayout.EndHorizontal();
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("Slider Local Position", GUILayout.Width(120));
			//GUILayout.Label(mngr.SunPathCenter.localPosition.x.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.SunPathCenter.localPosition.y.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.SunPathCenter.localPosition.z.ToString("n2"), GUILayout.Width(40));
			//GUILayout.EndHorizontal();
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("World Coordinates", GUILayout.Width(120));
			//GUILayout.Label(mngr.SunLight.transform.position.x.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.SunLight.transform.position.y.ToString("n2"), GUILayout.Width(40));
			//GUILayout.Label(mngr.SunLight.transform.position.z.ToString("n2"), GUILayout.Width(40));
			//GUILayout.EndHorizontal();

			//GUILayout.EndVertical(); // sun observer end

			GUILayout.EndVertical(); // row 1 end


			// row spacer
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1920));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical(); // row spacer end


			// row last
			GUILayout.BeginVertical();

			// cloud render box
			GUILayout.BeginVertical(GUI.skin.box);

			Texture2D tex;
			Rect r;
			GUILayout.Label("PS 0: " + RainController.RainParticleSystems[0].gameObject.name);
			tex = RainController.RainParticleSystems[0].shape.texture;
			r = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(false));
			GUI.DrawTexture(r, tex);

			GUILayout.Label("PS 1: " + RainController.RainParticleSystems[1].gameObject.name);
			tex = RainController.RainParticleSystems[1].shape.texture;
			r = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(false));
			GUI.DrawTexture(r, tex);

			GUILayout.Label("PS 2: " + RainController.RainParticleSystems[2].gameObject.name);
			tex = RainController.RainParticleSystems[2].shape.texture;
			r = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(false));
			GUI.DrawTexture(r, tex);

			GUILayout.Label("Sun Shadows");
			tex = WeatherSource.SunShadowRenderImage;
			r = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(false));
			GUI.DrawTexture(r, tex);

			GUILayout.EndVertical(); // cloud render box end

			GUILayout.EndVertical(); // row last end

			GUILayout.Space(3);
			GUILayout.EndHorizontal(); // total end
		}
	}

	public static class DevGUIFileSelection
	{
		private static string[] files;
		private static string[] filenames;
		private static bool active = false;
		private static int selected = 0;
		private static Vector2 scrollPos;

		public static void Show (string path, Action<string> callback)
		{
			if (!active)
			{
				files = Directory.GetFiles(path);
				filenames = new string[files.Length];
				for (int i = 0; i < files.Length; i++)
				{
					filenames[i] = Path.GetFileName(files[i]);
					string add = "incompatible file";
					try
					{
						XmlDocument doc = new XmlDocument();
						doc.Load(files[i]);
						add = doc.DocumentElement.FirstChild.LastChild.InnerText;
					} catch { }
					filenames[i] += $" ({add})";
				}
				active = true;
			}

			Rect r = new Rect(Screen.width / 2 - 250, Screen.height / 2 - 250, 500, 500);
			GUILayout.BeginArea(r, GUI.skin.box);

			scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(470));
			selected = GUILayout.SelectionGrid(selected, filenames, 2);
			GUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("LOAD"))
			{
				callback.Invoke(files[selected]);
				active = false;
			}
			if (GUILayout.Button("CANCEL"))
			{
				callback.Invoke(string.Empty);
				active = false;
			}
			GUILayout.EndHorizontal();

			GUILayout.EndArea();
		}
	}
#endif
}
