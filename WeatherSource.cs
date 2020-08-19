using System.Collections;
using UnityEngine;
using System.IO;
#if DEBUG
using System;
using System.Xml;
#endif

namespace ProceduralSkyMod
{
	public class WeatherState
	{
		public WeatherState ()
		{
			this.fileName = "FOO";
			this.name = "BAR";
			this.cloudClearSky = 0;
			this.cloudNoiseScale = 0;
			this.cloudChange = 0;
			this.cloudSpeed = 0;
			this.cloudBrightness = 0;
			this.cloudGradient = 0;
			this.rainParticleStrength = 0;
		}

		public WeatherState (string fileName, string name, float cloudClearSky, float cloudNoiseScale, float cloudChange, float cloudSpeed, float cloudBrightness, float cloudGradient, float rainParticleStrength)
		{
			this.fileName = fileName;
			this.name = name;
			this.cloudClearSky = cloudClearSky;
			this.cloudNoiseScale = cloudNoiseScale;
			this.cloudChange = cloudChange;
			this.cloudSpeed = cloudSpeed;
			this.cloudBrightness = cloudBrightness;
			this.cloudGradient = cloudGradient;
			this.rainParticleStrength = rainParticleStrength;
		}

		public WeatherState (string fileName, string name, WeatherState copyState)
		{
			this.fileName = fileName;
			this.name = name;
			this.cloudClearSky = copyState.cloudClearSky;
			this.cloudNoiseScale = copyState.cloudNoiseScale;
			this.cloudChange = copyState.cloudChange;
			this.cloudSpeed = copyState.cloudSpeed;
			this.cloudBrightness = copyState.cloudBrightness;
			this.cloudGradient = copyState.cloudGradient;
			this.rainParticleStrength = copyState.rainParticleStrength;
		}

		public string fileName;
		public string name;

		public float cloudClearSky;
		public float cloudNoiseScale;
		public float cloudChange;
		public float cloudSpeed;
		public float cloudBrightness;
		public float cloudGradient;

		public float rainParticleStrength;

		public static WeatherState LoadFromXML (string filePath)
		{
			WeatherState state = new WeatherState();
			XmlDocument doc = new XmlDocument();
			doc.Load(filePath);
			foreach (XmlNode nodes0 in doc.DocumentElement)
			{
				foreach (XmlNode nodes1 in nodes0.ChildNodes)
				{
					switch (nodes0.Name)
					{
						case "Names":
							if (nodes1.Name == "fileName") state.fileName =	nodes1.InnerText;
							if (nodes1.Name == "name") state.name = nodes1.InnerText;
							break;
						case "Clouds":
							if (nodes1.Name == "cloudClearSky") state.cloudClearSky = float.Parse(nodes1.InnerText);
							if (nodes1.Name == "cloudNoiseScale") state.cloudNoiseScale = float.Parse(nodes1.InnerText);
							if (nodes1.Name == "cloudChange") state.cloudChange = float.Parse(nodes1.InnerText);
							if (nodes1.Name == "cloudSpeed") state.cloudSpeed =	float.Parse(nodes1.InnerText);
							if (nodes1.Name == "cloudBrightness") state.cloudBrightness = float.Parse(nodes1.InnerText);
							if (nodes1.Name == "cloudGradient") state.cloudGradient = float.Parse(nodes1.InnerText);
							break;
						case "Rain":
							if (nodes1.Name == "rainParticleStrength") state.rainParticleStrength = float.Parse(nodes1.InnerText);
							break;
						default:
							Debug.LogWarning("FOO");
							break;
					}
				}
			}
			return state;
		}

#if DEBUG
		public static void CreateNewXML (WeatherState state)
		{
			XmlDocument doc = new XmlDocument();
			XmlNode rootNode = doc.CreateElement("WeatherState");
			doc.AppendChild(rootNode);

			// name data
			XmlNode namesNode = doc.CreateElement("Names");
			rootNode.AppendChild(namesNode);

			XmlNode filenameNode = doc.CreateElement("fileName");
			filenameNode.InnerText = state.fileName;
			namesNode.AppendChild(filenameNode);

			XmlNode nameNode = doc.CreateElement("name");
			nameNode.InnerText = state.name;
			namesNode.AppendChild(nameNode);

			// cloud data
			XmlNode cloudsNode = doc.CreateElement("Clouds");
			rootNode.AppendChild(cloudsNode);

			XmlNode cloudClearSky = doc.CreateElement("cloudClearSky");
			cloudClearSky.InnerText = state.cloudClearSky.ToString();
			cloudsNode.AppendChild(cloudClearSky);

			XmlNode cloudNoiseScale = doc.CreateElement("cloudNoiseScale");
			cloudNoiseScale.InnerText = state.cloudNoiseScale.ToString();
			cloudsNode.AppendChild(cloudNoiseScale);

			XmlNode cloudChange = doc.CreateElement("cloudChange");
			cloudChange.InnerText = state.cloudChange.ToString();
			cloudsNode.AppendChild(cloudChange);

			XmlNode cloudSpeed = doc.CreateElement("cloudSpeed");
			cloudSpeed.InnerText = state.cloudSpeed.ToString();
			cloudsNode.AppendChild(cloudSpeed);

			XmlNode cloudBrightness = doc.CreateElement("cloudBrightness");
			cloudBrightness.InnerText = state.cloudBrightness.ToString();
			cloudsNode.AppendChild(cloudBrightness);

			XmlNode cloudGradient = doc.CreateElement("cloudGradient");
			cloudGradient.InnerText = state.cloudGradient.ToString();
			cloudsNode.AppendChild(cloudGradient);

			// rain data
			XmlNode rainNode = doc.CreateElement("Rain");
			rootNode.AppendChild(rainNode);

			XmlNode rainParticleStrength = doc.CreateElement("rainParticleStrength");
			rainParticleStrength.InnerText = state.rainParticleStrength.ToString();
			rainNode.AppendChild(rainParticleStrength);

			doc.Save(WeatherSource.XMLWeatherStatePath + Path.DirectorySeparatorChar + state.fileName);
		}
#endif
	}

	public delegate void CloudRenderDelegate ();

	public class WeatherSource
	{
		private static RenderTexture cloudRendTex;

		public static string XMLWeatherStatePath { get => Main.Path + "ManagedData"; }

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

		public static WeatherState CurrentWeatherState { get; set; }
		public static WeatherState TargettWeatherState { get; set; }
		public static float WeatherStateBlending { get; set; }

		public static float CloudClearSkyBlend
		{ get => (TargettWeatherState == null) ? CurrentWeatherState.cloudClearSky : Mathf.Lerp(CurrentWeatherState.cloudClearSky, TargettWeatherState.cloudClearSky, WeatherStateBlending); }
		public static float CloudNoiseScaleBlend
		{ get => (TargettWeatherState == null) ? CurrentWeatherState.cloudNoiseScale : Mathf.Lerp(CurrentWeatherState.cloudNoiseScale, TargettWeatherState.cloudNoiseScale, WeatherStateBlending); }
		public static float CloudChangeBlend
		{ get => (TargettWeatherState == null) ? CurrentWeatherState.cloudChange : Mathf.Lerp(CurrentWeatherState.cloudChange, TargettWeatherState.cloudChange, WeatherStateBlending); }
		public static float CloudSpeedBlend
		{ get => (TargettWeatherState == null) ? CurrentWeatherState.cloudSpeed : Mathf.Lerp(CurrentWeatherState.cloudSpeed, TargettWeatherState.cloudSpeed, WeatherStateBlending); }
		public static float CloudBrightnessBlend
		{ get => (TargettWeatherState == null) ? CurrentWeatherState.cloudBrightness : Mathf.Lerp(CurrentWeatherState.cloudBrightness, TargettWeatherState.cloudBrightness, WeatherStateBlending); }
		public static float CloudGradientBlend
		{ get => (TargettWeatherState == null) ? CurrentWeatherState.cloudGradient : Mathf.Lerp(CurrentWeatherState.cloudGradient, TargettWeatherState.cloudGradient, WeatherStateBlending); }

		public static float RainStrengthBlend
		{ get => (TargettWeatherState == null) ? CurrentWeatherState.rainParticleStrength : Mathf.Lerp(CurrentWeatherState.rainParticleStrength, TargettWeatherState.rainParticleStrength, WeatherStateBlending); }


		public static event CloudRenderDelegate CloudRenderEvent;
		public static void OnCloudRendered () { CloudRenderEvent?.Invoke(); }

		public static IEnumerator CloudChanger ()
		{
			while (true)
			{
				yield return new WaitForSeconds(60);
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
			cloudRendTex.wrapMode = TextureWrapMode.Clamp; // use mirror if used for cloud shadows
			cloudRendTex.filterMode = FilterMode.Point; // use billinear if used for cloud shadows
			cloudRendTex.anisoLevel = 0;
		}
	}
}
