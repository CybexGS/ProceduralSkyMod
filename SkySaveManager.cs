using System;
using System.IO;
using UnityEngine;

namespace ProceduralSkyMod
{
	[Serializable]
	public class SkySaveData
	{
		public SkySaveData () { }
		public SkySaveData (string modVersion) { version = modVersion; }

		public string version = string.Empty;

		public string internalDate = DateTime.Now.ToString();

		public string currentWeatherState = "PSWS_FALLBACK"; // save filename
		public string nextWeatherState = string.Empty; // save filename
		public float weatherStateBlending = 0;
	}

	public static class SkySaveManager
	{
		static SkySaveManager ()
		{
			State = new SkySaveData();
		}

		public static void Save ()
		{
			try
			{
				string path = Path.Combine(Main.ModPath, "SkySave.json");
				if (!File.Exists(path)) File.Create(path).Close();

				State.version = Main.ModVersion;

				State.internalDate = ProceduralSkyTimeSource.Instance.LocalTime.ToString();

				State.currentWeatherState = WeatherSource.CurrentWeatherState.fileName;
				State.nextWeatherState = WeatherSource.NextWeatherState?.fileName ?? string.Empty;
				State.weatherStateBlending = WeatherSource.WeatherStateBlending;

				File.WriteAllText(path, JsonUtility.ToJson(State, true));

				Debug.Log($"Saved Procedural Sky State: {path}");
			}
			catch (Exception ex)
			{
				Debug.Log(ex);
				throw;
			}
		}

		public static void Load ()
		{
			try
			{
				string path = Path.Combine(Main.ModPath, "SkySave.json");
				if (!File.Exists(path))
				{
					File.Create(path).Close();

					State = new SkySaveData(Main.ModVersion);

					File.WriteAllText(path, JsonUtility.ToJson(State, true));
				}
				else
				{
					State = JsonUtility.FromJson<SkySaveData>(File.ReadAllText(path));
					Debug.Log($">>> >>> >>> LOAD Version: [{State.version}]");
					if (string.IsNullOrEmpty(State.version) || State.version != JsonUtility.FromJson<UnityModManagerNet.UnityModManager.ModInfo>(File.ReadAllText(Path.Combine(Main.ModPath, "Info.json"))).Version)
					{
						if (string.IsNullOrEmpty(State.version)) Debug.LogWarning(">>> >>> >>> No Version Available");
						else if (State.version != Main.ModVersion) Debug.LogWarning(">>> >>> >>> No Version Compatibility");

						State = new SkySaveData(Main.ModVersion);
						File.WriteAllText(path, JsonUtility.ToJson(State, true));
						Debug.LogWarning($">>> >>> >>> Created New File with Version: [{State.version}]");
					}
				}
			}
			catch (Exception ex)
			{
				Debug.Log(ex);
				throw;
			}
		}

		public static SkySaveData State { get; private set; }
	}
}
