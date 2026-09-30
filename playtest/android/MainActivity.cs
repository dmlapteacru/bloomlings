using Android.App;
using Android.Content.PM;
using Android.OS;

// Short vibration pulses with the sound cues (PlaytestSound).
[assembly: UsesPermission(Android.Manifest.Permission.Vibrate)]

namespace Bloomlings.Playtest
{
    /// <summary>
    /// The single activity of the playtest APKs: the full playtest shows the design board's screens
    /// (<see cref="Droid.DesignView"/>), the level tester its minimal view (<see cref="TesterView"/>).
    /// </summary>
    [Activity(
        Label = PlaytestFlavor.Title,
        MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Portrait,
        Theme = "@android:style/Theme.Material.Light.NoActionBar",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)]
    public sealed class MainActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
#if PLAYTEST_TESTER
            SetContentView(new TesterView(this));
#else
            SetContentView(new Droid.DesignView(this));
#endif
        }
    }
}
