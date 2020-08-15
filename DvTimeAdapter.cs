using System;
using UnityEngine;

namespace ProceduralSkyMod
{
    public static class DvTimeAdapter
    {
		// TODO: re-enable integration with DvTime
		/*
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
			_ = CurrentTime.Time;
			GetTime = () => CurrentTime.Time;
		}
		*/

		public static bool Available => false;// GetTime != null;
		public static Func<DateTime> GetTime;
	}
}
