using Bloomlings.Content.Packs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bloomlings.Client.App
{
    /// <summary>
    /// Routes between screens. This is the T025 stub: it starts Level 1 in the Gameplay scene, Next goes to the
    /// following level, and Leave replays the current one. T062 (US2) adds the real flow: first launch straight into
    /// Level 1 (FR-045), Home on later launches (FR-046) and Win → Next → Level N+1, with no level map (FR-057).
    /// </summary>
    public sealed class GameFlow
    {
        public const string GameplayScene = "Gameplay";

        private readonly AppServices _services;

        public GameFlow(AppServices services)
        {
            _services = services;
        }

        /// <summary>The level the Gameplay scene should play.</summary>
        public int CurrentLevel { get; private set; } = 1;

        public void Begin()
        {
            CurrentLevel = 1;
            LoadGameplay();
        }

        /// <summary>After a win: the next level if the content has it (FR-057), otherwise the same one again.</summary>
        public void Next()
        {
            if (_services.TryGet(out ContentSet? content) && content!.TryGetLevel(CurrentLevel + 1, out _))
            {
                CurrentLevel++;
            }

            LoadGameplay();
        }

        /// <summary>Leaving a level costs nothing (FR-030); until Home exists (T063) it reloads the level.</summary>
        public void Leave() => LoadGameplay();

        private static void LoadGameplay()
        {
            if (Application.CanStreamedLevelBeLoaded(GameplayScene))
            {
                SceneManager.LoadScene(GameplayScene);
            }
            else
            {
                Debug.LogWarning($"[GameFlow] Scene '{GameplayScene}' is not in the build settings yet (T052).");
            }
        }
    }
}
