using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Xml;
#if DEBUG
using System;
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

		public WeatherState (string fileName, WeatherState copyState)
		{
			this.fileName = fileName;
			this.name = copyState.name;
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
			try
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
								if (nodes1.Name == "fileName") state.fileName = nodes1.InnerText;
								if (nodes1.Name == "name") state.name = nodes1.InnerText;
								break;
							case "Clouds":
								if (nodes1.Name == "cloudClearSky") state.cloudClearSky = float.Parse(nodes1.InnerText);
								if (nodes1.Name == "cloudNoiseScale") state.cloudNoiseScale = float.Parse(nodes1.InnerText);
								if (nodes1.Name == "cloudChange") state.cloudChange = float.Parse(nodes1.InnerText);
								if (nodes1.Name == "cloudSpeed") state.cloudSpeed = float.Parse(nodes1.InnerText);
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
			catch
			{
				Debug.LogWarning($"WeatherState.cs: Load From XML Error While Trying To Load\n{filePath}");
				return GetFALLBACK();
			}
		}

		private static WeatherState GetFALLBACK ()
		{
			if (File.Exists(WeatherSource.XMLWeatherStatePath + "PSWS_FALLBACK"))
			{
				Debug.Log(">>> >>> >>> Get FALLBACK From File");
				return LoadFromXML(WeatherSource.XMLWeatherStatePath + "PSWS_FALLBACK");
			}
			WeatherState fallback = new WeatherState("PSWS_FALLBACK", "FALLBACK", 0, 1, 0.1f, 0.001f, 0, 0, 1);
			CreateNewXML(fallback);
			Debug.Log(">>> >>> >>> Created New FALLBACK File");
			return fallback;
		}

#if DEBUG
		public static void CreateNewXML (WeatherState state)
#else
		private static void CreateNewXML (WeatherState state)
#endif
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

			doc.Save(WeatherSource.XMLWeatherStatePath + state.fileName);
		}
	}

	public delegate void CloudRenderDelegate ();

	public class WeatherSource
	{
		private static RenderTexture cloudRendTex;
		private static RenderTexture sunShadowRendTex;

		public static string XMLWeatherStatePath { get => Main.ModPath + "ManagedData" + Path.DirectorySeparatorChar; }

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

		public static string[] AvailableWeatherStateFilesXML { get; private set; }
		public static WeatherState CurrentWeatherState { get; set; }
		public static WeatherState NextWeatherState { get; set; }
		public static float WeatherStateBlending { get; set; }
		public static float WeatherChangeProbability { get; private set; }
#if DEBUG
		public static float LastRNDWeatherChange { get; set; }
		public static float LastRNDFileSelect { get; set; }
#endif

		public static float CloudClearSkyBlend
		{ get => (NextWeatherState == null) ? CurrentWeatherState.cloudClearSky : Mathf.Lerp(CurrentWeatherState.cloudClearSky, NextWeatherState.cloudClearSky, WeatherStateBlending); }
		public static float CloudNoiseScaleBlend
		{ get => (NextWeatherState == null) ? CurrentWeatherState.cloudNoiseScale : Mathf.Lerp(CurrentWeatherState.cloudNoiseScale, NextWeatherState.cloudNoiseScale, WeatherStateBlending); }
		public static float CloudChangeBlend
		{ get => (NextWeatherState == null) ? CurrentWeatherState.cloudChange : Mathf.Lerp(CurrentWeatherState.cloudChange, NextWeatherState.cloudChange, WeatherStateBlending); }
		public static float CloudSpeedBlend
		{ get => (NextWeatherState == null) ? CurrentWeatherState.cloudSpeed : Mathf.Lerp(CurrentWeatherState.cloudSpeed, NextWeatherState.cloudSpeed, WeatherStateBlending); }
		public static float CloudBrightnessBlend
		{ get => (NextWeatherState == null) ? CurrentWeatherState.cloudBrightness : Mathf.Lerp(CurrentWeatherState.cloudBrightness, NextWeatherState.cloudBrightness, WeatherStateBlending); }
		public static float CloudGradientBlend
		{ get => (NextWeatherState == null) ? CurrentWeatherState.cloudGradient : Mathf.Lerp(CurrentWeatherState.cloudGradient, NextWeatherState.cloudGradient, WeatherStateBlending); }

		public static float RainStrengthBlend
		{ get => (NextWeatherState == null) ? CurrentWeatherState.rainParticleStrength : Mathf.Lerp(CurrentWeatherState.rainParticleStrength, NextWeatherState.rainParticleStrength, WeatherStateBlending); }


		public static event CloudRenderDelegate CloudRenderEvent;
		public static void OnCloudRendered () { CloudRenderEvent?.Invoke(); }

		public static IEnumerator WeatherStateChanger ()
		{
			AvailableWeatherStateFilesXML = SearchAvailableWeatherStateFilesXML();
			if (CurrentWeatherState.fileName == "PSWS_FALLBACK" && AvailableWeatherStateFilesXML.Length > 0)
				CurrentWeatherState = WeatherState.LoadFromXML(AvailableWeatherStateFilesXML[0]);
			else
			{
				Debug.LogError("WeatherSource.cs: Weather State Changer Error");
				yield break;
			}
			
			WeatherChangeProbability = 0.2f;
			float frameRate = 30f;
			while (true)
			{
				for (int i = 0; i < Mathf.Max(Main.settings.DayLengthSecondsRT / 4, 600) * frameRate; i++) // break out of loop 4 times a day but wait a minimum of 10 minutes
				{
					if (NextWeatherState != null && !DV.AppUtil.IsPaused)
					{
						WeatherStateBlending += 0.0033334f / frameRate; // it will take just over 5 minutes to change state copletely to target
						if (WeatherStateBlending > 1)
						{
							CurrentWeatherState = NextWeatherState;
							NextWeatherState = null;
							WeatherStateBlending = 0;
						}
					}

					if (DV.AppUtil.IsPaused) yield return null;
					else
					{
						i++;
						yield return new WaitForSeconds(1f / frameRate);
					}
				}

				float rnd = UnityEngine.Random.value;
#if DEBUG
				LastRNDWeatherChange = rnd;
#endif
				if (WeatherChangeProbability > rnd)
				{
					NextWeatherState = WeatherState.LoadFromXML(AvailableWeatherStateFilesXML[(int)(UnityEngine.Random.value * AvailableWeatherStateFilesXML.Length)]);
					if (NextWeatherState == CurrentWeatherState)
					{
						NextWeatherState = null;
						WeatherChangeProbability += 0.2f;
					}
					else WeatherChangeProbability = 0.2f;
				}
				else WeatherChangeProbability += 0.2f;
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
					SunShadowRenderImage = tex;

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

		private static void SetupShadowRenderTex ()
		{
			sunShadowRendTex = new RenderTexture(64, 64, 8, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
			sunShadowRendTex.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
			sunShadowRendTex.antiAliasing = 1;
			sunShadowRendTex.depth = 0;
			sunShadowRendTex.useMipMap = false;
			sunShadowRendTex.useDynamicScale = false;
			sunShadowRendTex.wrapMode = TextureWrapMode.Clamp;
			sunShadowRendTex.filterMode = FilterMode.Bilinear;
			sunShadowRendTex.anisoLevel = 0;
		}

		public static string[] SearchAvailableWeatherStateFilesXML ()
		{
#if DEBUG
			Debug.Log(">>> >>> >>> Loading Weather State Files...");
#endif
			List<string> allFiles = Directory.GetFiles(WeatherSource.XMLWeatherStatePath).ToList();
			XmlDocument doc;
			for (int i = 0; i < allFiles.Count;)
			{
				if (Path.GetFileName(allFiles[i]).Contains("PSWS_FALLBACK")) allFiles.RemoveAt(i);
				else
				{
					try
					{
						doc = new XmlDocument();
						doc.Load(allFiles[i]);
						if (doc.DocumentElement.Name == "WeatherState") i++;
						else allFiles.RemoveAt(i);
					}
					catch { allFiles.RemoveAt(i); }
				}
			}
#if DEBUG
			Debug.Log($">>> >>> >>> Weather File Loading: {allFiles.Count} Weather Files found!");
#endif
			return allFiles.ToArray();
		}
	}
}
