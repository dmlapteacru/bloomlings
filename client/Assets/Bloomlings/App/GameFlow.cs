using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bloomlings.Client.App
{
    /// <summary>
    /// Routes between screens. This is the T025 stub: it always starts Level 1 in the Gameplay scene. T062 (US2) adds
    /// the real flow: first launch straight into Level 1 (FR-045), Home on later launches (FR-046) and Win → Next →
    /// Level N+1, with no level map (FR-057).
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
