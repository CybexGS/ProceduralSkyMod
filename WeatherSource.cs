using System.Collections;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class WeatherState
	{
		public WeatherState (string name, float cloudClearSky, float cloudNoiseScale, float cloudChange, float cloudSpeed, float cloudBrightness, float cloudGradient, float rainParticleStrength)
		{
			this.name = name;
			this.cloudClearSky = cloudClearSky;
			this.cloudNoiseScale = cloudNoiseScale;
			this.cloudChange = cloudChange;
			this.cloudSpeed = cloudSpeed;
			this.cloudBrightness = cloudBrightness;
			this.cloudGradient = cloudGradient;
			this.rainParticleStrength = rainParticleStrength;
		}

		public string name;

		public float cloudClearSky;
		public float cloudNoiseScale;
		public float cloudChange;
		public float cloudSpeed;
		public float cloudBrightness;
		public float cloudGradient;

		public float rainParticleStrength;
	}

	public delegate void CloudRenderDelegate ();

	public class WeatherSource
	{
		private static float cloudTarget = 2, cloudCurrent = 1;
		private static RenderTexture cloudRendTex;
		private static RenderTexture sunShadowRendTex;

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
		public static Camera SunShadowRenderTexCam { get; set; }
		public static RenderTexture SunShadowRenderTex
		{
			get
			{
				if (sunShadowRendTex == null) SetupShadowRenderTex();
				return sunShadowRendTex;
			}
		}
		public static Texture2D CloudRenderImage0 { get; private set; }
		public static Texture2D CloudRenderImage1 { get; private set; }
		public static Texture2D CloudRenderImage2 { get; private set; }
		public static Texture2D SunShadowRenderImage { get; private set; }

		public static WeatherState WeatherState { get; set; }
		public static float RainStrength { get; set; } = 1f;

		public static event CloudRenderDelegate CloudRenderEvent;
		public static void OnCloudRendered () { CloudRenderEvent?.Invoke(); }

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

				CloudRenderImage0 = new Texture2D(16, 16);
				CloudRenderImage0.ReadPixels(new Rect(24, 24, 16, 16), 0, 0);
				CloudRenderImage0.Apply();
				CloudRenderImage1 = new Texture2D(32, 32);
				CloudRenderImage1.ReadPixels(new Rect(16, 16, 32, 32), 0, 0);
				CloudRenderImage1.Apply();
				CloudRenderImage2 = new Texture2D(64, 64);
				CloudRenderImage2.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
				CloudRenderImage2.Apply();

				OnCloudRendered();

				for (int i = 0; i < 16; i++)
                {
					RenderTexture.active = SunShadowRenderTex;
					SunShadowRenderTexCam.Render();

					SunShadowRenderImage = new Texture2D(SunShadowRenderTex.width, SunShadowRenderTex.height);
					SunShadowRenderImage.ReadPixels(new Rect(0, 0, SunShadowRenderTex.width, SunShadowRenderTex.height), 0, 0);
					SunShadowRenderImage.Apply();

					Texture2D tex = new Texture2D(WeatherSource.SunShadowRenderImage.width, WeatherSource.SunShadowRenderImage.height);
					for (int x = 0; x < tex.width; x++)
					{
						for (int y = 0; y < tex.height; y++)
						{
							tex.SetPixel(x, y, new Color(1, 1, 1, 1 - WeatherSource.SunShadowRenderImage.GetPixel(x, y).a));
						}
					}
					tex.Apply();
					SkyManager.SunLight.cookie = tex;

					RenderTexture.active = current;
					yield return new WaitForSeconds(0.03f); // 0.03s * 16 = ~0.5s
					current = RenderTexture.active;
                }
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

		private static void SetupShadowRenderTex()
		{
			sunShadowRendTex = new RenderTexture(64, 64, 8, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
			sunShadowRendTex.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
			sunShadowRendTex.antiAliasing = 1;
			sunShadowRendTex.depth = 0;
			sunShadowRendTex.useMipMap = false;
			sunShadowRendTex.useDynamicScale = false;
			//sunShadowRendTex.wrapMode = TextureWrapMode.Mirror;
			sunShadowRendTex.wrapMode = TextureWrapMode.Clamp;
			sunShadowRendTex.filterMode = FilterMode.Bilinear;
			sunShadowRendTex.anisoLevel = 0;
		}
	}
}
