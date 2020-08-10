﻿using System.Linq;
using System.IO;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace ProceduralSkyMod
{
	public class Main
	{
		public static bool enabled;
		public static bool initialized;
		public static Settings settings;

		public static string Path { get; private set; }

		static bool Load (UnityModManager.ModEntry modEntry)
		{
			try { settings = Settings.Load<Settings>(modEntry); } catch { }
			Path = modEntry.Path;
			modEntry.OnToggle = OnToggle;
			modEntry.OnGUI = OnGUI;
			modEntry.OnSaveGUI = OnSaveGUI;
			return true; // If false the mod will show an error
		}

		static void OnGUI (UnityModManager.ModEntry modEntry)
		{
			settings.Draw(modEntry);
		}

		static void OnSaveGUI (UnityModManager.ModEntry modEntry)
		{
			settings.Save(modEntry);
		}

		static bool OnToggle (UnityModManager.ModEntry modEntry, bool value)
		{
			if (value) Run(modEntry);
			else Stop(modEntry);
			enabled = value;
			return true; // If true, the mod will switch the state. If not, the state will not change.
		}

		static void Run (UnityModManager.ModEntry modEntry)
		{
#if DEBUG
			Debug.Log(">>> >>> >>> Cybex_ProceduralSkyMod : Run Mod...");
#endif
			modEntry.OnUpdate = OnUpdate;
		}

		static void Stop (UnityModManager.ModEntry modEntry)
		{
#if DEBUG
			Debug.Log(">>> >>> >>> Cybex_ProceduralSkyMod : Stop Mod...");
#endif
			initialized = false;
		}

		static void OnUpdate (UnityModManager.ModEntry modEntry, float delta)
		{
			if (!initialized)
			{
				if (LoadingScreenManager.IsLoading || !WorldStreamingInit.IsLoaded || !InventoryStartingItems.itemsLoaded) return;
				else
				{
					ProceduralSkyInitializer initializer = new ProceduralSkyInitializer();
					initializer.Init();
					initialized = true;
					modEntry.OnUpdate = null;
				}
			}
		}
	}
}
