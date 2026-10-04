using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Droid
{
    /// <summary>
    /// The full playtest's view (spec 002 FR-003): it hosts the engine-free <see cref="DesignApp"/> on an Android canvas
    /// through <see cref="AndroidPainter"/>. It passes the safe-area insets, the finger (for pressed looks) and taps,
    /// draws frames while something animates, and plays the sound cues and haptics (<see cref="PlaytestSound"/>). The
    /// level tester keeps its own minimal <see cref="TesterView"/>.
    /// </summary>
    public sealed class DesignView : View
    {
        private readonly AndroidPainter _painter = new AndroidPainter();
        private readonly DesignApp _app;
        private long _lastFrame;
        private int _insetTop;
        private int _insetBottom;

        public DesignView(Context context)
            : base(context)
        {
            var sound = new PlaytestSound(context);
            var output = new SoundOut(sound);
            _app = new DesignApp(context.FilesDir!.AbsolutePath, PlaytestContent.Load(), output);
            output.App = _app;
            sound.Enabled = _app.Meta.Save.Settings.Sfx;
            // A lawn rendered on a worker thread asks for the frame that shows it.
            _painter.Redraw = PostInvalidate;
        }

        public override WindowInsets OnApplyWindowInsets(WindowInsets? insets)
        {
            if (insets != null)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    Insets bars = insets.GetInsets(WindowInsets.Type.SystemBars());
                    _insetTop = bars.Top;
                    _insetBottom = bars.Bottom;
                }
                else
                {
#pragma warning disable CA1422, CS0618 // The pre-Android 11 inset API.
                    _insetTop = insets.SystemWindowInsetTop;
                    _insetBottom = insets.SystemWindowInsetBottom;
#pragma warning restore CA1422, CS0618
                }

                Invalidate();
            }

            return base.OnApplyWindowInsets(insets)!;
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            long now = SystemClock.UptimeMillis();
            float dt = _lastFrame == 0 ? 0f : Math.Min(0.1f, (now - _lastFrame) / 1000f);
            _lastFrame = now;
            _painter.Now = now / 1000f;
            _painter.Begin(canvas, Width, Height, new Client.UI.Design.Insets(_insetTop, _insetBottom));
            _app.Draw(_painter, dt);
            if (_app.NeedsFrames || _painter.Springing || _painter.Finger.HasValue)
            {
                if (_app.Calm && !_painter.Springing && !_painter.Finger.HasValue)
                {
                    // Only slow motion is left (the win card's, or Home's 24 fps heroes under a card): about 30 frames a
                    // second is enough.
                    PostInvalidateDelayed(33);
                }
                else
                {
                    PostInvalidateOnAnimation();
                }
            }
            else
            {
                _lastFrame = 0;
            }
        }

        /// <summary>
        /// The system back: closes the Store page or the Wardrobe as their back buttons do (<see cref="DesignApp.Back"/>);
        /// false when there is nothing to close, so the activity does what the system does.
        /// </summary>
        public bool Back()
        {
            bool handled = _app.Back();
            if (handled)
            {
                Invalidate();
            }

            return handled;
        }

        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (e == null)
            {
                return false;
            }

            _painter.Now = SystemClock.UptimeMillis() / 1000f;
            switch (e.Action)
            {
                case MotionEventActions.Down:
                case MotionEventActions.Move:
                    _painter.Finger = (e.GetX(), e.GetY());
                    break;
                case MotionEventActions.Up:
                    _painter.Finger = null;
                    _painter.Dispatch(e.GetX(), e.GetY());
                    break;
                case MotionEventActions.Cancel:
                    _painter.Finger = null;
                    break;
            }

            Invalidate();
            return true;
        }

        /// <summary>The screens' sounds, with the Settings haptics toggle.</summary>
        private sealed class SoundOut : ISoundOut
        {
            private readonly PlaytestSound _sound;

            public SoundOut(PlaytestSound sound) => _sound = sound;

            public DesignApp? App { get; set; }

            public bool Enabled
            {
                get => _sound.Enabled;
                set => _sound.Enabled = value;
            }

            public void Play(SoundCue cue)
            {
                _sound.Haptics = App?.Meta.Save.Settings.Haptics ?? true;
                _sound.Play(cue);
            }
        }
    }
}
