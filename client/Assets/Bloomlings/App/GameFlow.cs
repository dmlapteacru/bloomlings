using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Content;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bloomlings.Client.App
{
    /// <summary>
    /// The linear flow (FR-057, T062): Launch → Home → Play → Level N → Win → Next → Level N+1. On the first launch
    /// (no save) Level 1 starts directly with no menus or sign-in (US2 scenario 1); later launches open Home. There is
    /// no level map or chooser.
    /// With published round content (spec 006 FR-060, T060) the levels are round levels only: Level N is round level N
    /// (<see cref="RoundCatalogService"/>), started through the one round entry point <see cref="StartRoundLevel"/>, and
    /// past the last one the game shows "new levels soon" on Home (<see cref="ShowNewLevelsSoon"/>); no old level follows.
    /// Without round content (an Editor or test setup without it) the legacy catalog plays as before.
    /// </summary>
    public sealed class GameFlow
    {
        public const string HomeScene = "Home";
        public const string GameplayScene = "Gameplay";

        private readonly ProgressionService _progression;
        private readonly CatalogService _catalog;
        private readonly RoundCatalogService? _rounds;
        private readonly System.Action<string> _loadScene;

        /// <param name="loadScene">Loads a scene by name; the default loads it when it is in the build settings. The
        /// engine-free tests pass their own to follow the flow.</param>
        public GameFlow(ProgressionService progression, CatalogService catalog, RoundCatalogService? rounds = null, System.Action<string>? loadScene = null)
        {
            _progression = progression;
            _catalog = catalog;
            _rounds = rounds;
            _loadScene = loadScene ?? LoadBuiltScene;
        }

        /// <summary>The round levels, when the build carries published round content (spec 006 FR-060); else null.</summary>
        public RoundCatalogService? Rounds => _rounds;

        private int _lastWon;

        /// <summary>A level was newly completed (ad caps count it).</summary>
        public event System.Action<int>? LevelWon;

        /// <summary>
        /// Runs between the Win screen's Next and the next level, with the won level and the continuation: the only place
        /// an interstitial may show (FR-053). Boot sets it; without it Next goes straight on.
        /// </summary>
        public System.Action<int, System.Action>? PostWinTransition { get; set; }

        /// <summary>The attempt the Gameplay scene plays, pinned to its content version (R6).</summary>
        public LevelAttempt? CurrentAttempt { get; private set; }

        /// <summary>The round level attempt started last (spec 006, <see cref="StartRoundLevel"/>); null on a legacy or Daily attempt.</summary>
        public RoundAttempt? CurrentRoundAttempt { get; private set; }

        /// <summary>
        /// Whether Play (or the last level's Next) found no level after the last round level, so Home owes the player the
        /// "new levels soon" card (spec 006 FR-060). Home takes it with <see cref="TakeNewLevelsSoon"/>.
        /// </summary>
        public bool NewLevelsSoonPending { get; private set; }

        /// <summary>
        /// Raised when "new levels soon" is due while Home is showing (Home subscribes while it lives); without a listener
        /// the flow goes Home, which shows the card from <see cref="NewLevelsSoonPending"/>.
        /// </summary>
        public event System.Action? NewLevelsSoon;

        /// <summary>Whether Level N is past the last round level, so "new levels soon" shows instead of a level.</summary>
        public bool IsPastLastRoundLevel(int levelNumber) => _rounds != null && _rounds.IsPastLastLevel(levelNumber);

        /// <summary>Whether Home's Play can start (or answer) Level N: always with round levels, which answer past the last one.</summary>
        public bool CanPlay(int levelNumber) => _rounds != null || _catalog.HasLevel(levelNumber);

        /// <summary>The SHA-256 of the last winning command log, submitted with the leaderboard progress (FR-062).</summary>
        public string LastCommandLogHash { get; private set; } = string.Empty;

        public void Begin(bool firstLaunch)
        {
            if (firstLaunch)
            {
                Play();
            }
            else
            {
                GoHome();
            }
        }

        /// <summary>
        /// Home's Play/Continue: always the current level. With round levels, round level N through
        /// <see cref="StartRoundLevel"/>, or "new levels soon" past the last one. Past the end of the legacy catalog Home
        /// stays, saying more levels are coming.
        /// </summary>
        public void Play()
        {
            if (_rounds != null)
            {
                RoundAttempt? round = _rounds.BeginAttempt(_progression.CurrentLevel);
                if (round == null)
                {
                    ShowNewLevelsSoon();
                }
                else
                {
                    StartRoundLevel(round);
                }

                return;
            }

            LevelAttempt? attempt = _catalog.BeginAttempt(_progression.CurrentLevel);
            if (attempt == null)
            {
                GoHome();
                return;
            }

            CurrentAttempt = attempt;
            CurrentRoundAttempt = null;
            Load(GameplayScene);
        }

        /// <summary>
        /// THE ROUND LEVEL ENTRY POINT (spec 006 T060, T058): every round level the game starts comes through here, from
        /// Home's Play, the first launch and the win's Next. The Gameplay scene plays <see cref="CurrentRoundAttempt"/> on the
        /// round rules (<c>GameplayController</c> hands it to <c>RoundGameplayController</c>, which plays it on the kit's
        /// <c>RoundPlay</c>: <c>RoundTimeline</c> and <c>RoundAnimator</c>); a round attempt never falls back to a legacy or dev
        /// level (<c>GameplayRouting</c>).
        /// </summary>
        private void StartRoundLevel(RoundAttempt attempt)
        {
            CurrentAttempt = null;
            CurrentRoundAttempt = attempt;
            Load(GameplayScene);
        }

        /// <summary>
        /// "New levels soon" (spec 006 FR-060): the last round level is won and no level follows yet. Shown on Home: at once
        /// when Home is showing, else after the flow goes Home.
        /// </summary>
        public void ShowNewLevelsSoon()
        {
            CurrentAttempt = null;
            CurrentRoundAttempt = null;
            NewLevelsSoonPending = true;
            if (NewLevelsSoon != null)
            {
                NewLevelsSoon();
            }
            else
            {
                GoHome();
            }
        }

        /// <summary>True once per pending "new levels soon" (see <see cref="NewLevelsSoonPending"/>); Home shows the card then.</summary>
        public bool TakeNewLevelsSoon()
        {
            bool pending = NewLevelsSoonPending;
            NewLevelsSoonPending = false;
            return pending;
        }

        /// <summary>
        /// Records the win at once (it survives a kill during the win animation) and saves; true when the level was newly
        /// completed, so its reward is due.
        /// </summary>
        public bool OnLevelWon(int levelNumber, string commandLogHash = "")
        {
            if (CurrentAttempt != null && CurrentAttempt.IsDaily)
            {
                return false; // The Daily Challenge never changes Level N (FR-064).
            }

            LastCommandLogHash = commandLogHash;
            if (!_progression.CompleteLevel(levelNumber))
            {
                return false;
            }

            _lastWon = levelNumber;
            LevelWon?.Invoke(levelNumber);
            return true;
        }

        /// <summary>Plays today's Daily Challenge (FR-064); Next and Leave return Home.</summary>
        public void PlayDaily(LevelAttempt attempt)
        {
            CurrentAttempt = attempt;
            CurrentRoundAttempt = null;
            Load(GameplayScene);
        }

        /// <summary>The Win screen's Next: the following level starts, after the post-win transition if one is set.</summary>
        public void Next()
        {
            if (CurrentAttempt != null && CurrentAttempt.IsDaily)
            {
                Leave();
                return;
            }

            if (IsPastLastRoundLevel(_progression.CurrentLevel))
            {
                // The last round level is won: no level follows yet, so no iris with "Level N" either (spec 006 FR-060).
                ShowNewLevelsSoon();
                return;
            }

            if (PostWinTransition != null)
            {
                PostWinTransition(_lastWon, Play);
            }
            else
            {
                Play();
            }
        }

        /// <summary>Leaving a level costs nothing (FR-030, FR-040).</summary>
        public void Leave()
        {
            CurrentAttempt = null;
            CurrentRoundAttempt = null;
            GoHome();
        }

        public void GoHome() => Load(HomeScene);

        private void Load(string scene) => _loadScene(scene);

        private static void LoadBuiltScene(string scene)
        {
            if (Application.CanStreamedLevelBeLoaded(scene))
            {
                SceneManager.LoadScene(scene);
            }
            else
            {
                Debug.LogWarning($"[GameFlow] Scene '{scene}' is not in the build settings (Tools/Bloomlings/Create ... Scene).");
            }
        }
    }
}
