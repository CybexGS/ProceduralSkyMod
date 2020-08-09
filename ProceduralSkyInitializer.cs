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
		private Light dirLight;
		private Camera mainCam;

		public void Init ()
		{
#if DEBUG
			Debug.Log(">>> >>> >>> Cybex_ProceduralSkyMod : Initializer Starting Setup...");
			Debug.Log(">>> >>> >>> Loading Asset Bundle...");
#endif
			// Load the asset bundle
			AssetBundle assets = AssetBundle.LoadFromFile(Main.Path + "Resources/dynamicskymod");
			GameObject bundle = assets.LoadAsset<GameObject>("Assets/Prefabs/BundleResources.prefab");
			assets.Unload(false);

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Skybox Material...");
#endif
			// Set skybox material
			Material skyMaterial = bundle.transform.Find("Sky").GetComponent<MeshRenderer>().sharedMaterial;

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
			Debug.Log(">>> >>> >>> Setting Up Dynamic Sky Master...");
#endif
			// Setup dynamic sky
			GameObject dsMaster = new GameObject() { name = "DynamicSkyMod" };
			dsMaster.transform.Reset();
			SkyManager skyManager = dsMaster.AddComponent<SkyManager>();
			skyManager.latitude = 44.7872f;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Cameras...");
#endif
			// main cam
			mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
			mainCam.clearFlags = CameraClearFlags.Depth;
			mainCam.cullingMask = -1;
			mainCam.cullingMask &= ~(1 << 31);
			mainCam.depth = 1;

			// sky cam
			Camera skyCam = new GameObject() { name = "SkyCam" }.AddComponent<Camera>();
			skyCam.transform.SetParent(dsMaster.transform);
			skyCam.transform.ResetLocal();
			SkyCamConstraint constraint = skyCam.gameObject.AddComponent<SkyCamConstraint>();
			skyCam.clearFlags = CameraClearFlags.Depth;
			skyCam.cullingMask = 0;
			skyCam.cullingMask |= 1 << 31;
			skyCam.depth = 0;
			skyCam.fieldOfView = mainCam.fieldOfView;
			skyCam.nearClipPlane = mainCam.nearClipPlane;
			skyCam.farClipPlane = 100;

			// clear cam
			Camera clearCam = new GameObject() { name = "ClearCam" }.AddComponent<Camera>();
			clearCam.transform.SetParent(mainCam.transform);
			clearCam.transform.ResetLocal();
			clearCam.clearFlags = CameraClearFlags.Skybox;
			clearCam.cullingMask = 0;
			clearCam.depth = -1;
			clearCam.fieldOfView = mainCam.fieldOfView;
			clearCam.nearClipPlane = mainCam.nearClipPlane;
			clearCam.farClipPlane = mainCam.farClipPlane;

			constraint.main = mainCam;
			constraint.sky = skyCam;
			constraint.clear = clearCam;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Cloud Plane...");
#endif
			GameObject cloudPlane = new GameObject();

			MeshFilter filter = cloudPlane.AddComponent<MeshFilter>();
			filter.sharedMesh = bundle.transform.Find("CloudPlane").GetComponent<MeshFilter>().sharedMesh;
			MeshRenderer renderer = cloudPlane.AddComponent<MeshRenderer>();
			Material cloudMat = renderer.sharedMaterial = bundle.transform.Find("CloudPlane").GetComponent<MeshRenderer>().sharedMaterial;

			cloudPlane.transform.SetParent(dsMaster.transform);
			cloudPlane.transform.ResetLocal();
			cloudPlane.layer = 31;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Skybox Night...");
#endif
			GameObject dsSkyboxNight = new GameObject() { name = "SkyboxNight" };
			dsSkyboxNight.transform.SetParent(dsMaster.transform);
			dsSkyboxNight.transform.ResetLocal();

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Sun Position...");
#endif
			dirLight.transform.SetParent(dsSkyboxNight.transform);
			dirLight.transform.ResetLocal();
			dirLight.transform.position += Vector3.up * 10;
			dirLight.transform.localRotation = Quaternion.Euler(new Vector3(90, 0, 0));

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Starbox...");
#endif
			GameObject starBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
			starBox.GetComponent<MeshRenderer>().sharedMaterial = bundle.transform.Find("StarBox").GetComponent<MeshRenderer>().sharedMaterial;
			starBox.transform.SetParent(dsSkyboxNight.transform);
			starBox.transform.ResetLocal();
			starBox.transform.localRotation = Quaternion.Euler(new Vector3(0, 68.5f, 28.9f));
			starBox.transform.localScale = Vector3.one * 20;
			starBox.layer = 31;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Moon Billboard...");
#endif
			GameObject moonBillboard = new GameObject() { name = "MoonBillboard" };

			filter = moonBillboard.AddComponent<MeshFilter>();
			filter.sharedMesh = bundle.transform.Find("Moon").GetComponent<MeshFilter>().sharedMesh;
			renderer = moonBillboard.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = bundle.transform.Find("Moon").GetComponent<MeshRenderer>().sharedMaterial;

			moonBillboard.transform.SetParent(dsMaster.transform);
			moonBillboard.transform.ResetLocal();
			moonBillboard.transform.localScale = Vector3.one * 5;
			moonBillboard.layer = 31;

#if DEBUG
			Debug.Log(">>> >>> >>> Setting Up Sky Manager Properties...");
#endif
			// assign skyboxNight after sun is positioned to get correct sun rotation
			skyManager.SkyboxNight = dsSkyboxNight.transform;
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

			// fog setup
			//RenderSettings.fog = false;

#if DEBUG
			Debug.Log(">>> >>> >>> Cybex_ProceduralSkyMod : Initializer Finished Setup...");
#endif

			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.transform.ResetLocal();
			go.transform.position += Vector3.up * 130;
			go.transform.localScale *= 10;
		}
	}
}
