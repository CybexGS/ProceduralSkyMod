using System.Collections;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class WeatherSource
	{
		private static float cloudTarget = 2, cloudCurrent = 1;
		private static RenderTexture cloudRendTex;

		public static float SkyClarity { get { cloudCurrent = Mathf.Lerp(cloudCurrent, cloudTarget, Time.deltaTime * 0.1f); return cloudCurrent; } }

		public static Camera CloudRenderTexCam { get; set; }
		public static RenderTexture CloudRenderTex
		{
			get
			{
				if (cloudRendTex == null) SetupCloudRenderTex();
				return cloudRendTex;
			}
			set => cloudRendTex = value;
		}
		public static Texture2D CloudRenderImage0 { get; private set; }
		public static Texture2D CloudRenderImage1 { get; private set; }
		public static Texture2D CloudRenderImage2 { get; private set; }

		public static ParticleSystem[] RainParticleSystems { get; set; }
		private static ParticleSystem.ShapeModule shapeModule;

		public static AudioSource RainAudio { get; set; }

		public static IEnumerator CloudChanger ()
		{
			while (true)
			{
				yield return new WaitForSeconds(60);
				// .5 to 5 to test it
				cloudTarget = Mathf.Clamp(Random.value * 5, .5f, 5f);
#if DEBUG
				Debug.Log(string.Format("New Cloud Target of {0}, current {1}", cloudTarget, cloudCurrent));
#endif
			}
		}

		public static IEnumerator UpdateCloudRenderTex ()
		{
			while (true)
			{
				RenderTexture current = RenderTexture.active;

				RenderTexture.active = CloudRenderTex;
				CloudRenderTexCam.Render();

				for (int i = 0; i < RainParticleSystems.Length; i++)
				{
					if (RainParticleSystems[i].gameObject.name.Contains("RainDrop"))
					{
						CloudRenderImage0 = new Texture2D(16, 16);
						CloudRenderImage0.ReadPixels(new Rect(24, 24, 16, 16), 0, 0);
						CloudRenderImage0.Apply();
						shapeModule = RainParticleSystems[i].shape;
						shapeModule.texture = CloudRenderImage0;
					}
					else if (RainParticleSystems[i].gameObject.name.Contains("RainCluster"))
					{
						CloudRenderImage1 = new Texture2D(32, 32);
						CloudRenderImage1.ReadPixels(new Rect(16, 16, 32, 32), 0, 0);
						CloudRenderImage1.Apply();
						shapeModule = RainParticleSystems[i].shape;
						shapeModule.texture = CloudRenderImage1;
					}
					else if (RainParticleSystems[i].gameObject.name.Contains("RainHaze"))
					{
						CloudRenderImage2 = new Texture2D(CloudRenderTex.width, CloudRenderTex.height);
						CloudRenderImage2.ReadPixels(new Rect(0, 0, CloudRenderTex.width, CloudRenderTex.height), 0, 0);
						CloudRenderImage2.Apply();
						shapeModule = RainParticleSystems[i].shape;
						shapeModule.texture = CloudRenderImage2;
					}
					else Debug.LogWarning(string.Format("ProSkyMod Weather ERR 00: No name match for {0}", RainParticleSystems[i].gameObject.name));
				}

				RenderTexture.active = current;
				yield return new WaitForSeconds(0.5f);
			}
		}

		private static void SetupCloudRenderTex ()
		{
			cloudRendTex = new RenderTexture(64, 64, 8, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
			cloudRendTex.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
			cloudRendTex.antiAliasing = 1;
			cloudRendTex.depth = 0;
			cloudRendTex.useMipMap = false;
			cloudRendTex.useDynamicScale = false;
			cloudRendTex.wrapMode = TextureWrapMode.Clamp;
			cloudRendTex.filterMode = FilterMode.Point;
			cloudRendTex.anisoLevel = 0;
		}
	}
}
