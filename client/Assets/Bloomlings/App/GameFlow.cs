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
    /// </summary>
    public sealed class GameFlow
    {
        public const string HomeScene = "Home";
        public const string GameplayScene = "Gameplay";

        private readonly ProgressionService _progression;
        private readonly CatalogService _catalog;

        public GameFlow(ProgressionService progression, CatalogService catalog)
        {
            _progression = progression;
            _catalog = catalog;
        }

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

        /// <summary>Home's Play/Continue: always the current level.</summary>
        public void Play()
        {
            CurrentAttempt = _catalog.BeginAttempt(_progression.CurrentLevel);
            Load(GameplayScene);
        }

        /// <summary>
        /// Records the win at once (it survives a kill during the win animation) and saves; true when the level was newly
        /// completed, so its reward is due.
        /// </summary>
        public bool OnLevelWon(int levelNumber)
        {
            if (!_progression.CompleteLevel(levelNumber))
            {
                return false;
            }

            _lastWon = levelNumber;
            LevelWon?.Invoke(levelNumber);
            return true;
        }

        /// <summary>The Win screen's Next: the following level starts, after the post-win transition if one is set.</summary>
        public void Next()
        {
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
            GoHome();
        }

        public void GoHome() => Load(HomeScene);

        private static void Load(string scene)
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
