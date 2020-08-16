using System.Linq;
using System.IO;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;
using System.Collections.Generic;

namespace ProceduralSkyMod
{
	public class ProceduralSkyInitializer : MonoBehaviour
	{
		// TODO: fix VR

		private Light dirLight;
		private Camera mainCam;

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
			AssetBundle assets = AssetBundle.LoadFromFile(Main.Path + "Resources/proceduralskymod");

			_skyMaterial = assets.LoadAsset<Material>("Assets/Materials/Sky.mat");
			_rainAudioClip = assets.LoadAsset<AudioClip>("Assets/Audio/rain-03.wav");
			_cloudPrefab = assets.LoadAsset<GameObject>("Assets/Prefabs/CloudPlane.prefab");
			_starMaterial = assets.LoadAsset<Material>("Assets/Materials/StarBox.mat");
			_moonPrefab = assets.LoadAsset<GameObject>("Assets/Prefabs/Moon.prefab");
			_rainPrefab = assets.LoadAsset<GameObject>("Assets/Prefabs/RainDrop.prefab");

			assets.Unload(false);

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Skybox Material...");
#endif
			// Set skybox material
			Material skyMaterial = _skyMaterial;

			skyMaterial.SetColor("_SkyTint", new Color(0.3f, 0.3f, 0.8f, 1f));
			skyMaterial.SetColor("_GroundColor", new Color(0.369f, 0.349f, 0.341f, 1f));

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

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Procedural Sky Master...");
#endif
			// Setup dynamic sky
			GameObject psMaster = new GameObject() { name = "ProceduralSkyMod" };
			psMaster.transform.Reset();
			SkyManager skyManager = psMaster.AddComponent<SkyManager>();
			skyManager.latitude = 44.7872f;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Cameras...");
#endif
			// main cam
			mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
			mainCam.clearFlags = CameraClearFlags.Depth;
			mainCam.cullingMask = -1;
			mainCam.cullingMask &= ~(1 << 31);
			//mainCam.depth = -1; // original setting

			// sky cam
			Camera skyCam = new GameObject() { name = "SkyCam" }.AddComponent<Camera>();
			GameObject skyCamGimbal = new GameObject { name = "SkyCamGimbal" };
			skyCamGimbal.transform.SetParent(psMaster.transform, false);
			skyCam.transform.SetParent(skyCamGimbal.transform, false);
			skyCam.clearFlags = CameraClearFlags.Depth;
			skyCam.cullingMask = 0;
			skyCam.cullingMask |= 1 << 31;
			skyCam.depth = -2;
			skyCam.fieldOfView = mainCam.fieldOfView;
			skyCam.nearClipPlane = mainCam.nearClipPlane;
			skyCam.farClipPlane = 100;
			// this localScale negates VR stereo separation
			skyCamGimbal.transform.localScale = Vector3.zero;
			skyCamGimbal.AddComponent<PositionConstraintOnPreCull>().source = psMaster.transform;

			// clear cam
			Camera clearCam = new GameObject() { name = "ClearCam" }.AddComponent<Camera>();
			clearCam.clearFlags = CameraClearFlags.Skybox;
			clearCam.cullingMask = 0;
			clearCam.depth = -3;
			clearCam.fieldOfView = mainCam.fieldOfView;

			SkyCamConstraint constraint = skyCam.gameObject.AddComponent<SkyCamConstraint>();
			constraint.main = mainCam;
			constraint.sky = skyCam;
			constraint.clear = clearCam;

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

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Audio Sources...");
#endif
			GameObject psAudio = new GameObject() { name = "ProceduralSkyAudio" };
			psAudio.transform.SetParent(mainCam.transform);
			RainController.RainAudio = psAudio.AddComponent<AudioSource>();

			RainController.RainAudio.clip = _rainAudioClip;
			RainController.RainAudio.mute = false;
			RainController.RainAudio.bypassEffects = false;
			RainController.RainAudio.bypassListenerEffects = false;
			RainController.RainAudio.bypassReverbZones = false;
			RainController.RainAudio.playOnAwake = true;
			RainController.RainAudio.loop = true;
			RainController.RainAudio.priority = 128;

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
			sunPivot.transform.SetParent(psMaster.transform);
			dirLight.transform.SetParent(sunPivot.transform);
			dirLight.transform.ResetLocal();
			dirLight.transform.position += Vector3.up * 10;
			dirLight.transform.localRotation = Quaternion.Euler(new Vector3(90, 0, 0));

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Starbox...");
#endif
			GameObject starBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
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

			filter = moonBillboard.AddComponent<MeshFilter>();
			filter.sharedMesh = _moonPrefab.GetComponent<MeshFilter>().sharedMesh;
			renderer = moonBillboard.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = _moonPrefab.GetComponent<MeshRenderer>().sharedMaterial;

			moonBillboard.transform.SetParent(psMaster.transform);
			moonBillboard.transform.ResetLocal();
			moonBillboard.transform.localScale = Vector3.one * 5;
			moonBillboard.layer = 31;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Rain Particle System...");
#endif
			GameObject psRainParticleSys = new GameObject() { name = "ProceduralSkyRainParticleSystem" };

			PositionConstraintOnUpdate psRainParticleSysconstraint = psRainParticleSys.AddComponent<PositionConstraintOnUpdate>();
			psRainParticleSysconstraint.source = mainCam.transform;

			GameObject rainObj = GameObject.Instantiate(_rainPrefab);
			rainObj.transform.SetParent(psRainParticleSys.transform);
			rainObj.transform.ResetLocal();
			rainObj.transform.Translate(Vector3.up * 16);

			RainController.SetRainParticleSystemArray(psRainParticleSys.GetComponentsInChildren<ParticleSystem>(true));
			WeatherSource.CloudRenderEvent += RainController.SetShapeTextures;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Sky Manager Properties...");
#endif
			// assign skyboxNight after sun is positioned to get correct sun rotation
			skyManager.SkyboxNight = skyboxNight.transform;
			skyManager.SunPivot = sunPivot.transform;
			skyManager.Sun = dirLight;

			skyManager.CloudPlane = cloudPlane.transform;
			skyManager.CloudMaterial = cloudMat;

			skyManager.StarMaterial = starBox.GetComponent<MeshRenderer>().sharedMaterial;

			skyManager.SkyCam = skyCam.transform;
			skyManager.SkyMaterial = skyMaterial;

			skyManager.ClearCam = clearCam.transform;

			skyManager.MoonBillboard = moonBillboard.transform;
			skyManager.MoonMaterial = moonBillboard.GetComponent<MeshRenderer>().sharedMaterial;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Render Settings...");
#endif
			// Set render settings
			RenderSettings.sun = dirLight;
			RenderSettings.skybox = skyMaterial;
			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Sky Save...");
#endif
			DV.AppUtil.GamePaused += SkySaveLoad.Save;

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
