using RedworkDE.DvTime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralSkyMod
{
	public static class DvTimeAdapter
	{
		static DvTimeAdapter()
		{
			try
			{
				DoInitialize(); // separate function to be able to catch dll load exceptions when DvTime is not installed
			}
			catch (Exception ex)
			{
#if DEBUG
				Debug.Log($"unable to load DVTime: {ex}");
#endif
			}
		}

		private static void DoInitialize()
		{
#if DEBUG
			Debug.Log($">>> >>> >>> initializing DvTimeAdapter");
#endif
			_ = CurrentTime.Time;
			GetTime = () => CurrentTime.Time;
		}

		public static void InstallProSkyTimeSource (List<ITimeSource> sources)
        {
#if DEBUG
			Debug.Log($">>> >>> >>> installing ProceduralSkyTimeSource");
#endif
			sources.Insert(0, ProceduralSkyTimeSource.Instance);
		}

		public static bool Available => GetTime != null;
		public static Func<DateTime> GetTime;
	}
}
