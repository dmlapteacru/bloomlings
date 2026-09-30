namespace Bloomlings.Playtest
{
    /// <summary>
    /// Which playtest APK this build is. The full playtest (playtest/android) has Home, progression, economy, demos and
    /// animations; the level tester (playtest/tester, built with <c>PLAYTEST_TESTER</c>) opens levels straight away,
    /// moves between them with ◀ ▶, gives every booster for free and shows each result at once.
    /// </summary>
    public static class PlaytestFlavor
    {
#if PLAYTEST_TESTER
        public static readonly bool Tester = true;
        public const string Title = "Bloomlings Tester";
#else
        public static readonly bool Tester = false;
        public const string Title = "Bloomlings Playtest";
#endif
    }
}
