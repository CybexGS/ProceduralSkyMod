using System;
using System.IO;
using UnityEngine;

namespace ProceduralSkyMod
{
	[Serializable]
	public class SkySaveData
	{
		public DateTime internalDate = DateTime.Now;
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
				string path = Path.Combine(Main.Path, "SkySave.json");
				if (!File.Exists(path)) File.Create(path).Close();

				State.internalDate = TimeSource.InternalDate;

				File.WriteAllText(path, JsonUtility.ToJson(State, true));

				Debug.Log($"Saved Procedural Sky State: {path}");
			}
			catch (Exception ex)
			{
				Debug.Log(ex);
				throw;
			}
		}

		public static SkySaveData Load ()
		{
			try
			{
				string path = Path.Combine(Main.Path, "SkySave.json");
				if (!File.Exists(path))
				{
					File.Create(path).Close();

					SkyManager instance = GameObject.Find("ProceduralSkyMod").GetComponent<SkyManager>();

					State = new SkySaveData();

					File.WriteAllText(path, JsonUtility.ToJson(State));
					return State;
				}
				else
				{
					return JsonUtility.FromJson<SkySaveData>(File.ReadAllText(path));
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
