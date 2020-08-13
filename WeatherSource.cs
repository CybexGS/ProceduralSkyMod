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
		public static Texture2D CloudRenderImage { get; private set; }

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
				CloudRenderImage = new Texture2D(CloudRenderTex.width, CloudRenderTex.height);
				CloudRenderImage.ReadPixels(new Rect(0, 0, CloudRenderTex.width, CloudRenderTex.height), 0, 0);
				CloudRenderImage.Apply();
				for (int i = 0; i < RainParticleSystems.Length; i++)
				{
					shapeModule = RainParticleSystems[i].shape;
					shapeModule.texture = CloudRenderImage;
				}

				RenderTexture.active = current;

				yield return new WaitForSeconds(0.5f);
			}
		}

		private static void SetupCloudRenderTex ()
		{
			cloudRendTex = new RenderTexture(32, 32, 8, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
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
