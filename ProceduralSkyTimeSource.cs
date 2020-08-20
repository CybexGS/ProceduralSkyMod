using Newtonsoft.Json.Linq;
using RedworkDE.DvTime;
using System;
using UnityEngine;

namespace ProceduralSkyMod
{
	class ProceduralSkyTimeSource : ITimeSource
	{
		public static ProceduralSkyTimeSource Instance { get => instance == null ? new ProceduralSkyTimeSource() : instance; }
		private static ProceduralSkyTimeSource instance = null;

		private ProceduralSkyTimeSource()
		{
#if DEBUG
			Debug.Log(">>> >>> >>> Creating instance of ProceduralSkyTimeSource...");
#endif
			instance = this;

			// load saved date from sky save manager
			LocalTime = DateTime.Parse(SkySaveManager.State.internalDate);
		}

		public string Id => "proceduralsky";
		public DateTime LocalTime { get; private set; }
		private const string dayLengthKey = "MinutesPerDay";

		public void Save(JObject shared, JObject settings)
		{
			settings[dayLengthKey] = Main.settings.dayLengthMinutesRT;
		}

		public void Load(JObject shared, JObject settings)
		{
			Main.settings.dayLengthMinutesRT = settings[dayLengthKey]?.ToObject<int>() ?? Main.settings.dayLengthMinutesRT;
		}

		public static void LoadSavedTime ()
		{
			Debug.Log($"BEFORE LOAD {Instance.LocalTime}");
			Instance.LocalTime = DateTime.Parse(SkySaveManager.State.internalDate);
			Debug.Log($"AFTER LOAD {Instance.LocalTime}, PARSED {DateTime.Parse(SkySaveManager.State.internalDate)} (string {SkySaveManager.State.internalDate})");
		}

		public void CalculateTimeProgress(float deltaSeconds)
		{
			float deltaDayProgress = deltaSeconds / Main.settings.DayLengthSecondsRT;
			LocalTime = LocalTime.AddDays(deltaDayProgress);
		}
	}
}
