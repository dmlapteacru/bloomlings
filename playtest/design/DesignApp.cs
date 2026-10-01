using System;
using System.Collections.Generic;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The full playtest's screens (spec 002 contracts/screen-map.md).</summary>
    public enum Screen
    {
        Splash,
        Home,
        Level,
    }

    /// <summary>The cards shown over a screen.</summary>
    public enum Overlay
    {
        Pause,
        Settings,
        DailyReward,
        Leaderboard,
        Collection,
        Store,
    }

    /// <summary>
    /// The full playtest in the design board's look (spec 002 FR-003):
    /// <list type="bullet">
    /// <item><description>the splash;</description></item>
    /// <item><description>Home (frames 2 and 3);</description></item>
    /// <item><description>the level (frames 7–14);</description></item>
    /// <item><description>the cards over them (frames 4–6, 10, 11 and 15–17).</description></item>
    /// </list>
    /// The very first launch goes straight into Level 1, and later launches open Home (spec 001 US2). Progress, Petals,
    /// booster charges, milestones, the Daily Reward and the Collection come from the Unity client's engine-free
    /// services (<see cref="PlaytestMeta"/>). This class only navigates and draws. Engine-free: the APK hosts it in a
    /// <c>DesignView</c>, and the preview tool renders it to PNG files.
    /// </summary>
    public sealed class DesignApp
    {
        /// <summary>How long the splash shows at launch (it never waits for a tap, FR-016).</summary>
        public const float SplashSeconds = 1.4f;

        private readonly string _dataFolder;
        private readonly List<(Overlay Overlay, float OpenedAt)> _overlays = new List<(Overlay, float)>();
        private readonly bool _firstLaunch;

        public DesignApp(string dataFolder, ContentSet content, ISoundOut sound, bool showSplash = true)
        {
            _dataFolder = dataFolder;
            Content = content;
            Sound = sound;
            Meta = new PlaytestMeta(dataFolder);
            _firstLaunch = Meta.FirstLaunch;
            Screen = showSplash ? Screen.Splash : Screen.Home;
            if (!showSplash)
            {
                AfterSplash();
            }
        }

        public ContentSet Content { get; }

        public ISoundOut Sound { get; }

        public PlaytestMeta Meta { get; private set; }

        public Screen Screen { get; private set; }

        public LevelScreen? Level { get; private set; }

        /// <summary>Seconds since launch (menu motion and toasts).</summary>
        public float Now { get; private set; }

        /// <summary>The Collection's opened picture, or −1.</summary>
        public int CollectionDetail { get; set; } = -1;

        /// <summary>The Store's selected tab (0 shop, 1 cosmetics).</summary>
        public int StoreTab { get; set; }

        public IReadOnlyList<(Overlay Overlay, float OpenedAt)> Overlays => _overlays;

        /// <summary>
        /// Whether the host should draw another frame soon: animations, the splash, toasts, and the one breathing button
        /// (PLAY on Home, CLAIM on the Daily Reward; spec 003 FR-019).
        /// </summary>
        public bool NeedsFrames =>
            Screen == Screen.Splash
            || (Level != null && Screen == Screen.Level && Level.NeedsFrames)
            || (_overlays.Count > 0 && Now - _overlays[_overlays.Count - 1].OpenedAt < 0.4f)
            || (_homeToastUntil > Now)
            || (Screen == Screen.Home && _overlays.Count == 0)
            || IsOpen(Overlay.DailyReward);

        private string? _homeToast;
        private float _homeToastUntil;

        /// <summary>A level number the playtest can play: its own content, repeating past the last level.</summary>
        public int Resolve(int levelNumber) =>
            Content.TryGetLevel(levelNumber, out _) ? levelNumber : Content.LevelNumbers[(levelNumber - 1) % Content.LevelCount];

        // ---- Navigation ----

        private void AfterSplash()
        {
            if (_firstLaunch)
            {
                StartLevel();
            }
            else
            {
                GoHome();
            }
        }

        public void StartLevel() => LoadLevel(Meta.CurrentLevel);

        public void LoadLevel(int levelNumber)
        {
            _overlays.Clear();
            Level = new LevelScreen(this, levelNumber);
            Screen = Screen.Level;
        }

        public void GoHome()
        {
            _overlays.Clear();
            Level = null;
            Screen = Screen.Home;
            if (Meta.DailyReward.CanClaim)
            {
                OpenOverlay(Overlay.DailyReward);
            }
        }

        public void OpenOverlay(Overlay overlay)
        {
            Sound.Play(SoundCue.Click);
            _overlays.Add((overlay, Now));
            if (overlay == Overlay.Collection)
            {
                CollectionDetail = -1;
            }
        }

        public void CloseOverlay()
        {
            if (_overlays.Count > 0)
            {
                _overlays.RemoveAt(_overlays.Count - 1);
                Sound.Play(SoundCue.Click);
            }
        }

        public bool IsOpen(Overlay overlay)
        {
            foreach ((Overlay o, float _) in _overlays)
            {
                if (o == overlay)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A claimed reward (spec 003 FR-020): the Petals pill counts up from <paramref name="before"/> and sparkles burst
        /// around it.
        /// </summary>
        public void RewardBurst(long before)
        {
            _burstAt = Now;
            _burstFrom = before;
        }

        /// <summary>Seconds since the last <see cref="RewardBurst"/>.</summary>
        public float SinceRewardBurst => Now - _burstAt;

        /// <summary>The Petals balance as shown: counting up after a claim.</summary>
        public long ShownPetals
        {
            get
            {
                long now = Meta.Economy.Petals;
                return SinceRewardBurst < DesignTokens.Motion.CountUp.Seconds && now > _burstFrom
                    ? _burstFrom + GardenLook.CountUp(now - _burstFrom, SinceRewardBurst)
                    : now;
            }
        }

        private float _burstAt = -10f;
        private long _burstFrom;

        public void HomeToast(string message)
        {
            _homeToast = message;
            _homeToastUntil = Now + 1.6f;
        }

        public string? HomeToastText => _homeToastUntil > Now ? _homeToast : null;

        /// <summary>Playtest control: a brand-new profile (Level 1, nothing unlocked).</summary>
        public void ResetProfile()
        {
            Meta = PlaytestMeta.ResetProfile(_dataFolder);
            HomeToast("New profile");
        }

        // ---- Frame ----

        /// <summary>Advances the clock and the level's animation, then draws the current screen and its cards.</summary>
        public void Draw(IPainter p, float dt)
        {
            Now += Math.Max(0f, Math.Min(0.1f, dt));
            if (Screen == Screen.Splash && Now >= SplashSeconds)
            {
                AfterSplash();
            }

            switch (Screen)
            {
                case Screen.Splash:
                    SplashScreen.Draw(p, this);
                    return;
                case Screen.Home:
                    HomeScreen.Draw(p, this);
                    break;
                case Screen.Level:
                    Level!.Advance(dt);
                    Level.Draw(p);
                    break;
            }

            for (int i = 0; i < _overlays.Count; i++)
            {
                (Overlay overlay, float openedAt) = _overlays[i];
                float since = Now - openedAt;
                switch (overlay)
                {
                    case Overlay.Pause:
                        MenuCards.Pause(p, this, since);
                        break;
                    case Overlay.Settings:
                        MenuCards.Settings(p, this, since);
                        break;
                    case Overlay.DailyReward:
                        MetaCards.DailyReward(p, this, since);
                        break;
                    case Overlay.Leaderboard:
                        MetaCards.Leaderboard(p, this, since);
                        break;
                    case Overlay.Collection:
                        MetaCards.Collection(p, this, since);
                        break;
                    case Overlay.Store:
                        MetaCards.Store(p, this, since);
                        break;
                }
            }
        }

        /// <summary>The garden backdrop of a level band's theme (FR-008, FR-066), cached by the painter.</summary>
        public static void DrawBackdrop(IPainter p, BackdropScene scene, int level)
        {
            BackgroundTheme theme = ThemeRotation.Default.ThemeFor(Math.Max(1, level));
            p.Mark(scene == BackdropScene.Gameplay ? "bg.theme." + theme.Id : scene == BackdropScene.Home ? "bg.home" : "bg.splash");
            p.Backdrop(new Box(0f, 0f, p.Width, p.Height), DesignTokens.Backdrop(theme.Background, theme.Accent), scene, theme.Id + "/" + scene);
        }
    }
}
