using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralSkyMod
{
	public class ProceduralSkyInitializer : MonoBehaviour
	{
		public const float sunDistanceToCamera = 10;
		public const float moonDistanceToCamera = 10;

		private Light dirLight;

		private Material _layeredCubemap;
		private Material _skyMaterial;
		private AudioClip _rainAudioClip;
		private GameObject _cloudPrefab;
		private Material _starMaterial;
		private GameObject _moonPrefab;
		private GameObject _rainPrefab;

		public void Init ()
		{
#if DEBUG
			Debug.Log(">>> >>> >>> Cybex_ProceduralSkyMod : Initializer Starting Setup...");
			Debug.Log(">>> >>> >>> Loading Asset Bundle...");
#endif
			// Load the asset bundle
			AssetBundle assets = AssetBundle.LoadFromFile(Main.ModPath + "Resources/proceduralskymod");

			_layeredCubemap = assets.LoadAsset<Material>("Assets/Materials/CubemapOverlay.mat");
			_skyMaterial = assets.LoadAsset<Material>("Assets/Materials/Sky.mat");
			_rainAudioClip = assets.LoadAsset<AudioClip>("Assets/Audio/rain-03.wav");
			_cloudPrefab = assets.LoadAsset<GameObject>("Assets/Prefabs/CloudPlane.prefab");
			_starMaterial = assets.LoadAsset<Material>("Assets/Materials/StarBox.mat");
			_moonPrefab = assets.LoadAsset<GameObject>("Assets/Prefabs/Moon.prefab");
			_rainPrefab = assets.LoadAsset<GameObject>("Assets/Prefabs/RainDrop.prefab");

			assets.Unload(false);

#if DEBUG
			Debug.Log(">>> >>> >>> Loading Saved State...");
#endif
			SkySaveManager.Load();
			ProceduralSkyTimeSource.LoadSavedTime();

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Skybox Material...");
#endif
			// Set skybox material
			Material skyMaterial = _skyMaterial;

			skyMaterial.SetColor("_SkyTint", new Color(0.3f, 0.3f, 0.8f, 1f));
			skyMaterial.SetColor("_GroundColor", new Color(0.369f, 0.349f, 0.341f, 1f));

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Procedural Sky Master...");
#endif
			// Setup dynamic sky
			GameObject psMaster = new GameObject() { name = "ProceduralSkyMod" };
			psMaster.transform.Reset();
			SkyManager skyManager = psMaster.AddComponent<SkyManager>();

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Directional Light...");
#endif
			// Find directional light and setup
			GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
			for (int i = 0; i < roots.Length; i++)
			{
				if (roots[i].name == "Directional Light") roots[i].SetActive(false);
			}

			dirLight = new GameObject() { name = "Sun" }.AddComponent<Light>();

			dirLight.type = LightType.Directional;
			dirLight.shadows = LightShadows.Soft;
			dirLight.shadowStrength = 0.9f;
			dirLight.gameObject.AddComponent<LookAtConstraintOnPreCull>().target = psMaster.transform;
			dirLight.cookieSize = 2000;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Cameras...");
#endif
			Camera.main.cullingMask = -1;
			Camera.main.cullingMask &= ~(1 << 31);
			
			// clear cam
			Camera clearCam = new GameObject() { name = "ClearCam" }.AddComponent<Camera>();
			clearCam.transform.SetParent(psMaster.transform, false);
			clearCam.clearFlags = CameraClearFlags.Skybox;
			clearCam.cullingMask = 0;
			clearCam.enabled = false;
			
			// override clearCam's skybox with skyMaterial to render sun disk
			Skybox clearCamSkybox = clearCam.gameObject.AddComponent<Skybox>();
			clearCamSkybox.material = skyMaterial;
			
			// sky cam
			Camera skyCam = new GameObject() { name = "SkyCam" }.AddComponent<Camera>();
			skyCam.transform.SetParent(psMaster.transform, false);
			skyCam.clearFlags = CameraClearFlags.Color;
			skyCam.backgroundColor = Color.clear;
			skyCam.cullingMask = 0;
			skyCam.cullingMask |= 1 << 31;
			skyCam.farClipPlane = 100;
			skyCam.enabled = false;
			
			// skyCamOutputMat will be used for global skybox
			int skyCamTexSize = 4096;
			RenderTexture clearCamTex = new RenderTexture(skyCamTexSize/4, skyCamTexSize/4, 0, RenderTextureFormat.DefaultHDR);
			clearCamTex.dimension = UnityEngine.Rendering.TextureDimension.Cube;
			RenderTexture skyCamTex = new RenderTexture(skyCamTexSize, skyCamTexSize, 0, RenderTextureFormat.DefaultHDR);
			skyCamTex.dimension = UnityEngine.Rendering.TextureDimension.Cube;
			Material skyCamOutputMat = _layeredCubemap;
			skyCamOutputMat.SetTexture("_Tex", clearCamTex); // shader: Skybox/Cubemap
			skyCamOutputMat.SetTexture("_AlphaTex", skyCamTex); // shader: Skybox/CubemapOverlay

			// initialize skybox
			clearCam.RenderToCubemap(clearCamTex);
			skyCam.RenderToCubemap(skyCamTex);
			
			// cloud render texture cam
			Camera cloudRendTexCam = new GameObject() { name = "CloudRendTexCam" }.AddComponent<Camera>();
			cloudRendTexCam.transform.SetParent(psMaster.transform);
			cloudRendTexCam.transform.ResetLocal();
			cloudRendTexCam.transform.localPosition = new Vector3(0, 3, 0);
			cloudRendTexCam.transform.localRotation = Quaternion.Euler(new Vector3(90, 0, 0));
			cloudRendTexCam.clearFlags = CameraClearFlags.Color;
			cloudRendTexCam.backgroundColor = Color.clear;
			cloudRendTexCam.cullingMask = 0;
			cloudRendTexCam.cullingMask |= 1 << 31;
			cloudRendTexCam.orthographic = true;
			cloudRendTexCam.orthographicSize = 3;
			cloudRendTexCam.nearClipPlane = 0;
			cloudRendTexCam.farClipPlane = 3;
			cloudRendTexCam.renderingPath = RenderingPath.Forward;
			cloudRendTexCam.targetTexture = WeatherSource.CloudRenderTex;
			cloudRendTexCam.useOcclusionCulling = false;
			cloudRendTexCam.allowHDR = false;
			cloudRendTexCam.allowMSAA = false;
			cloudRendTexCam.allowDynamicResolution = false;
			cloudRendTexCam.forceIntoRenderTexture = true;
			WeatherSource.CloudRenderTexCam = cloudRendTexCam;
			cloudRendTexCam.enabled = false; // disable the camera, renders will be triggered by script

			// sun shadow render texture cam
			Camera sunShadowRendTexCam = new GameObject() { name = "SunShadowRendTextCam" }.AddComponent<Camera>();
			sunShadowRendTexCam.transform.SetParent(dirLight.transform);
			sunShadowRendTexCam.transform.ResetLocal();

			sunShadowRendTexCam.clearFlags = CameraClearFlags.Color;
			sunShadowRendTexCam.backgroundColor = Color.clear;
			sunShadowRendTexCam.cullingMask = 0;
			sunShadowRendTexCam.cullingMask |= 1 << 31;

			//sunShadowRendTexCam.fieldOfView = dirLight.spotAngle;
			sunShadowRendTexCam.orthographic = true;
			sunShadowRendTexCam.orthographicSize = 2;
			sunShadowRendTexCam.nearClipPlane = 0;
			sunShadowRendTexCam.farClipPlane = 100;

			sunShadowRendTexCam.renderingPath = RenderingPath.Forward;
			sunShadowRendTexCam.targetTexture = WeatherSource.SunShadowRenderTex;
			sunShadowRendTexCam.useOcclusionCulling = false;
			sunShadowRendTexCam.allowHDR = false;
			sunShadowRendTexCam.allowMSAA = false;
			sunShadowRendTexCam.allowDynamicResolution = false;
			sunShadowRendTexCam.forceIntoRenderTexture = true;
			WeatherSource.SunShadowRenderTexCam = sunShadowRendTexCam;
			sunShadowRendTexCam.enabled = false; // disable the camera, renders will be triggered by script

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Audio Sources...");
#endif
			GameObject psAudio = new GameObject() { name = "ProceduralSkyAudio" };
			psAudio.transform.SetParent(Camera.main.transform);
			RainController.RainAudio = psAudio.AddComponent<AudioSource>();

			RainController.RainAudio.clip = _rainAudioClip;
			RainController.RainAudio.mute = false;
			RainController.RainAudio.bypassEffects = false;
			RainController.RainAudio.bypassListenerEffects = false;
			RainController.RainAudio.bypassReverbZones = false;
			RainController.RainAudio.playOnAwake = true;
			RainController.RainAudio.loop = true;
			RainController.RainAudio.priority = 128;
			RainController.RainAudio.volume = 0; // always ramp up from 0 when the game loads

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Cloud Plane...");
#endif
			GameObject cloudPlane = new GameObject();

			MeshFilter filter = cloudPlane.AddComponent<MeshFilter>();
			filter.sharedMesh = _cloudPrefab.GetComponent<MeshFilter>().sharedMesh;
			MeshRenderer renderer = cloudPlane.AddComponent<MeshRenderer>();
			Material cloudMat = renderer.sharedMaterial = _cloudPrefab.GetComponent<MeshRenderer>().sharedMaterial;

			cloudPlane.transform.SetParent(psMaster.transform);
			cloudPlane.transform.ResetLocal();
			cloudPlane.layer = 31;
			
#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Skybox Night...");
#endif
			GameObject skyboxNight = new GameObject() { name = "SkyboxNight" };
			skyboxNight.transform.SetParent(psMaster.transform);
			skyboxNight.transform.ResetLocal();

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Sun Position...");
#endif
			GameObject sunPivot = new GameObject() { name = "SunPivot" };
			GameObject sunSlider = new GameObject() { name = "SunSlider" }; // sunSlider mimics moonBillboards in-built mesh offset
			sunPivot.transform.SetParent(psMaster.transform, false);
			sunSlider.transform.SetParent(sunPivot.transform, false);
			dirLight.transform.SetParent(sunSlider.transform, false);
			dirLight.transform.position += Vector3.up * sunDistanceToCamera;
			dirLight.transform.localRotation = Quaternion.Euler(new Vector3(90, 0, 0));

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Starbox...");
#endif
			GameObject starBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
			_starMaterial.SetFloat("_Exposure", .5f);
			starBox.GetComponent<MeshRenderer>().sharedMaterial = _starMaterial;
			starBox.transform.SetParent(skyboxNight.transform);
			starBox.transform.ResetLocal();
			starBox.transform.localRotation = Quaternion.Euler(new Vector3(0, 68.5f, 28.9f));
			starBox.transform.localScale = Vector3.one * 20;
			starBox.layer = 31;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Moon Billboard...");
#endif
			GameObject moonBillboard = new GameObject() { name = "MoonBillboard" };
			GameObject moonPivot = new GameObject() { name = "MoonPivot" };

			filter = moonBillboard.AddComponent<MeshFilter>();
			filter.sharedMesh = _moonPrefab.GetComponent<MeshFilter>().sharedMesh;
			renderer = moonBillboard.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = _moonPrefab.GetComponent<MeshRenderer>().sharedMaterial;

			moonPivot.transform.SetParent(psMaster.transform, false);
			moonBillboard.transform.SetParent(moonPivot.transform, false);
			moonBillboard.transform.localScale = Vector3.one * moonDistanceToCamera / 2; // moonBillboard mesh has in-built offset of 2
			moonBillboard.layer = 31;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Rain Particle System...");
#endif
			GameObject psRainParticleSys = new GameObject() { name = "ProceduralSkyRainParticleSystem" };

			PositionConstraintOnUpdate psRainParticleSysconstraint = psRainParticleSys.AddComponent<PositionConstraintOnUpdate>();
			psRainParticleSysconstraint.source = Camera.main.transform;

			GameObject rainObj = GameObject.Instantiate(_rainPrefab);
			rainObj.transform.SetParent(psRainParticleSys.transform);
			rainObj.transform.ResetLocal();
			rainObj.transform.Translate(Vector3.up * 16);

			RainController.SetRainParticleSystemArray(psRainParticleSys.GetComponentsInChildren<ParticleSystem>(true));
			WeatherSource.CloudRenderEvent += RainController.SetShapeTextures;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Weather Source...");
#endif
			WeatherSource.CurrentWeatherState = WeatherState.LoadFromXML(WeatherSource.XMLWeatherStatePath + SkySaveManager.State.currentWeatherState);
			if (!string.IsNullOrEmpty(SkySaveManager.State.nextWeatherState))
				WeatherSource.NextWeatherState = WeatherState.LoadFromXML(WeatherSource.XMLWeatherStatePath + SkySaveManager.State.nextWeatherState);
			WeatherSource.WeatherStateBlending = SkySaveManager.State.weatherStateBlending;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Sky Manager Properties...");
#endif
			// assign skyboxNight after sun is positioned to get correct sun rotation
			skyManager.SkyboxNight = skyboxNight.transform;
			skyManager.SunPathCenter = sunSlider.transform;
			skyManager.SunLight = dirLight;

			skyManager.CloudPlane = cloudPlane.transform;
			skyManager.CloudMaterial = cloudMat;

			skyManager.StarMaterial = starBox.GetComponent<MeshRenderer>().sharedMaterial;

			skyManager.ClearCam = clearCam;
			skyManager.ClearCamTex = clearCamTex;
			skyManager.SkyCam = skyCam;
			skyManager.SkyCamTex = skyCamTex;
			skyManager.SkyMaterial = skyMaterial;

			skyManager.MoonPathCenter = moonBillboard.transform;
			skyManager.MoonMaterial = moonBillboard.GetComponent<MeshRenderer>().sharedMaterial;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Render Settings...");
#endif
			// Set render settings
			RenderSettings.sun = dirLight;
			RenderSettings.skybox = skyCamOutputMat;
			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Sky Save...");
#endif
			DV.AppUtil.GamePaused += SkySaveManager.Save;
			
#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Reflection Probe Updater...");
#endif
			ReflectionProbeUpdater.probe = FindObjectOfType<DynamicReflectionProbe>().GetComponent<ReflectionProbe>();

#if DEBUG
			psMaster.AddComponent<DevGUI>();
			Debug.Log(">>> >>> >>> Cybex_ProceduralSkyMod : Initializer Finished Setup...");
#endif
			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.transform.ResetLocal();
			go.transform.position += Vector3.up * 130;
			go.transform.localScale *= 10;
		}
	}
}
