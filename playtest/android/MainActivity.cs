using System;
using Android.App;
using Android.Content.PM;
using Android.OS;

// Short vibration pulses with the sound cues (PlaytestSound).
[assembly: UsesPermission(Android.Manifest.Permission.Vibrate)]

namespace Bloomlings.Playtest
{
    /// <summary>
    /// The single activity of the playtest APKs: the full playtest shows the design board's screens
    /// (<see cref="Droid.DesignView"/>), the level tester its minimal view (<see cref="TesterView"/>). In the full
    /// playtest the system back closes the Store page or the Wardrobe (<see cref="Droid.DesignView.Back"/>) and does what
    /// the system does anywhere else.
    /// </summary>
    [Activity(
        Label = PlaytestFlavor.Title,
        MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Portrait,
        Theme = "@android:style/Theme.Material.Light.NoActionBar",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)]
    public sealed class MainActivity : Activity
    {
#if !PLAYTEST_TESTER
        private Droid.DesignView? _view;
#endif

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
#if PLAYTEST_TESTER
            SetContentView(new TesterView(this));
#else
            _view = new Droid.DesignView(this);
            SetContentView(_view);
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                // From Android 13, once the system routes the back gesture to callbacks (every phone from Android 16 for
                // this target), it calls this one instead of OnBackPressed.
                OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(0, new BackCallback(this));
            }
#endif
        }

#if !PLAYTEST_TESTER
#pragma warning disable CS0618, CS0672, CA1422 // The older back key path, still used while the system does not route back to callbacks.
        public override void OnBackPressed()
        {
            if (_view == null || !_view.Back())
            {
                base.OnBackPressed();
            }
        }
#pragma warning restore CS0618, CS0672, CA1422

        /// <summary>The back callback (Android 13 and later): an open page closes, else the app goes to the background as the system's back would.</summary>
        private sealed class BackCallback : Java.Lang.Object, Android.Window.IOnBackInvokedCallback
        {
            private readonly MainActivity _activity;

            public BackCallback(MainActivity activity) => _activity = activity;

            public void OnBackInvoked()
            {
                if (_activity._view == null || !_activity._view.Back())
                {
                    _activity.MoveTaskToBack(true);
                }
            }
        }
#endif
    }
}
