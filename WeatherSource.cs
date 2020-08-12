using System.Collections;
using UnityEngine;

namespace ProceduralSkyMod
{
	public class WeatherSource
	{
		private static float cloudTarget = 2, cloudCurrent = 1;
		private static RenderTexture cloudRendTex;

		public static float SkyClarity { get { cloudCurrent = Mathf.Lerp(cloudCurrent, cloudTarget, Time.deltaTime * 0.1f); return cloudCurrent; } }
		public static RenderTexture CloudRendTex
		{
			get
			{
				if (cloudRendTex == null) SetupCloudRenderTex();
				return cloudRendTex;
			}
			set => cloudRendTex = value;
		}

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

		private static void SetupCloudRenderTex ()
		{
			cloudRendTex = new RenderTexture(32, 32, 8, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
			cloudRendTex.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
			cloudRendTex.antiAliasing = 0;
			cloudRendTex.depth = 0;
			cloudRendTex.useMipMap = false;
			cloudRendTex.useDynamicScale = false;
			cloudRendTex.wrapMode = TextureWrapMode.Clamp;
			cloudRendTex.filterMode = FilterMode.Point;
			cloudRendTex.anisoLevel = 0;
		}
	}
}
