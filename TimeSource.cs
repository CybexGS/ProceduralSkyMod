using System;

namespace ProceduralSkyMod
{
	public static class TimeSource
	{

		static TimeSource ()
        {
			// load saved date from sky save manager
			InternalDate = SkySaveManager.State.internalDate;

			if (DvTimeAdapter.Available)
            {
				GetCurrentTime = DvTimeAdapter.GetTime;
            }
			else
            {
				GetCurrentTime = () => InternalDate;
            }
        }

		public static readonly Func<DateTime> GetCurrentTime;
		public static DateTime InternalDate { get; private set; }
		private static float DayLengthInSeconds { get => Main.settings.dayLengthMinutesRT * 60f; }

		public static void CalculateTimeProgress (float deltaSeconds)
		{
			float deltaDayProgress = deltaSeconds / DayLengthInSeconds;
			InternalDate = InternalDate.AddDays(deltaDayProgress);
		}
	}
}
