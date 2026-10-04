namespace FPSGame
{
    /// <summary>
    /// Cached number strings so the HUD does not allocate every time a counter changes.
    /// </summary>
    public static class NumberStrings
    {
        private const int CacheSize = 1000;
        private static readonly string[] Cache = new string[CacheSize];
        private static readonly string[] TwoDigitCache = new string[100];

        public static string Get(int value)
        {
            if (value < 0 || value >= CacheSize) return value.ToString();
            string cached = Cache[value];
            if (cached == null)
            {
                cached = value.ToString();
                Cache[value] = cached;
            }
            return cached;
        }

        public static string TwoDigits(int value)
        {
            if (value < 0 || value >= 100) return value.ToString();
            string cached = TwoDigitCache[value];
            if (cached == null)
            {
                cached = value.ToString("00");
                TwoDigitCache[value] = cached;
            }
            return cached;
        }

        public static string FormatTime(float seconds)
        {
            int total = seconds < 0f ? 0 : (int)seconds;
            return TwoDigits(total / 60) + ":" + TwoDigits(total % 60);
        }
    }
}
