using RedworkDE.DvTime;
using Newtonsoft.Json;
using System;

namespace ProceduralSkyMod
{
	public static class TimeSourceAdapter
	{
		static TimeSourceAdapter ()
		{
			if (DvTimeAdapter.Available)
			{
				GetCurrentTime = DvTimeAdapter.GetTime;
			}
			else
			{
				GetCurrentTime = () => ProceduralSkyTimeSource.Instance.LocalTime;
			}
		}

		public static readonly Func<DateTime> GetCurrentTime;
	}
}
