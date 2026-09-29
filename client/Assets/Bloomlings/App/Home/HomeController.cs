using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bloomlings.Client.App.Home
{
    /// <summary>
    /// The Home scene (FR-058): builds <see cref="HomeScreen"/> and <see cref="SettingsScreen"/> from the save and the
    /// progression. Opened without Boot (in the Editor), it loads Boot first.
    /// </summary>
    public sealed class HomeController : MonoBehaviour
    {
        private void Start()
        {
            AppServices? services = AppServices.Current;
            if (services == null)
            {
                SceneManager.LoadScene(0);
                return;
            }

            PlayerSave save = services.Get<PlayerSave>();
            SaveService saves = services.Get<SaveService>();
            ProgressionService progression = services.Get<ProgressionService>();
            GameFlow flow = services.Get<GameFlow>();

            Canvas canvas = UiFactory.CreateCanvas("HomeCanvas", 0);
            canvas.transform.SetParent(transform, false);
            var root = (RectTransform)canvas.transform;
            SettingsScreen? settings = null;
            HomeScreen home = HomeScreen.Create(
                UiFactory.Stretch(UiFactory.CreateRect("Home", root)),
                flow.Play,
                () => settings!.Show(),
                () => Debug.Log("[Home] The Store opens with US6 (T133)."));
            settings = SettingsScreen.Create(root, save.Settings, saves.Save);

            (int? milestone, int? toGo) = NextMilestone(progression);
            home.Show(new HomeModel(
                progression.CurrentLevel,
                save.Wallet.Petals,
                progression.IsUnlocked("system.store"),
                progression.IsUnlocked("system.leaderboard"),
                null,
                milestone,
                toGo));
        }

        /// <summary>Milestones come every 25 levels (FR-061).</summary>
        public const int MilestoneCadence = 25;

        /// <summary>The next milestone level and the number of wins still needed to reach it (FR-058 teaser).</summary>
        public static (int? Level, int? WinsToGo) NextMilestone(ProgressionService progression)
        {
            int highest = progression.HighestCompletedLevel;
            int next = ((highest / MilestoneCadence) + 1) * MilestoneCadence;
            return (next, next - highest);
        }
    }
}
