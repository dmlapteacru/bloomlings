using Android.App;
using Android.Content.PM;
using Android.OS;

// Short vibration pulses with the sound cues (PlaytestSound).
[assembly: UsesPermission(Android.Manifest.Permission.Vibrate)]

namespace Bloomlings.Playtest
{
    /// <summary>The single screen of the playtest client.</summary>
    [Activity(
        Label = "Bloomlings Playtest",
        MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Portrait,
        Theme = "@android:style/Theme.Material.Light.NoActionBar",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)]
    public sealed class MainActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(new GameView(this));
        }
    }
}
