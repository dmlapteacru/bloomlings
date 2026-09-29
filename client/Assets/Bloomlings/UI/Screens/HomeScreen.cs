using System;
using System.Globalization;
using Bloomlings.Client.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>What Home shows (FR-058).</summary>
    public sealed record HomeModel(
        int CurrentLevel,
        int Petals,
        bool StoreUnlocked,
        bool LeaderboardUnlocked,
        string? RankText,
        int? NextMilestoneLevel,
        int? LevelsToMilestone);

    /// <summary>
    /// Home (FR-058, T063): the logo, Level N, one Play/Continue button and Petals; Settings; the Store button once
    /// unlocked (L12); the next-milestone teaser; the leaderboard rank once unlocked (L10). There is no level map.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        private TextMeshProUGUI _level = null!;
        private TextMeshProUGUI _petals = null!;
        private TextMeshProUGUI _playLabel = null!;
        private TextMeshProUGUI _milestone = null!;
        private TextMeshProUGUI _rank = null!;
        private GameObject _store = null!;
        private GameObject _freeBooster = null!;

        public static HomeScreen Create(RectTransform root, Action onPlay, Action onSettings, Action onStore, Action? onFreeBooster = null)
        {
            var screen = root.gameObject.AddComponent<HomeScreen>();
            Image background = UiFactory.CreateImage("Background", root, null, UiTheme.Background);
            UiFactory.Stretch(background.rectTransform);

            TextMeshProUGUI logo = UiFactory.CreateText("Logo", root, "Bloomlings", 120f, UiTheme.Accent);
            logo.fontStyle = FontStyles.Bold;
            UiFactory.Place(logo.rectTransform, 0.05f, 0.72f, 0.95f, 0.84f);

            Image petalIcon = UiFactory.CreateImage("PetalIcon", root, ProceduralSprites.Icon("flower"), UiTheme.Warning);
            petalIcon.preserveAspect = true;
            UiFactory.Place(petalIcon.rectTransform, 0.62f, 0.93f, 0.7f, 0.98f);
            screen._petals = UiFactory.CreateText("Petals", root, "0", 56f, UiTheme.Text, TextAlignmentOptions.Left);
            UiFactory.Place(screen._petals.rectTransform, 0.71f, 0.93f, 0.97f, 0.98f);

            Button settings = UiFactory.CreateButton("Settings", root, string.Empty, UiTheme.Text, onSettings);
            UiFactory.Place((RectTransform)settings.transform, 0.03f, 0.925f, 0.15f, 0.985f);
            Image gear = UiFactory.CreateImage("Gear", settings.transform, ProceduralSprites.Gear, Color.white);
            gear.preserveAspect = true;
            UiFactory.Place(gear.rectTransform, 0.15f, 0.15f, 0.85f, 0.85f);

            screen._level = UiFactory.CreateText("Level", root, "Level 1", 84f, UiTheme.Text);
            UiFactory.Place(screen._level.rectTransform, 0.05f, 0.52f, 0.95f, 0.6f);

            Button play = UiFactory.CreateButton("Play", root, "Play", UiTheme.Accent, onPlay, 84f);
            UiFactory.Place((RectTransform)play.transform, 0.2f, 0.38f, 0.8f, 0.49f);
            screen._playLabel = play.GetComponentInChildren<TextMeshProUGUI>();

            screen._milestone = UiFactory.CreateText("Milestone", root, string.Empty, 44f, UiTheme.Text);
            UiFactory.Place(screen._milestone.rectTransform, 0.05f, 0.31f, 0.95f, 0.36f);

            screen._rank = UiFactory.CreateText("Rank", root, string.Empty, 44f, UiTheme.Text);
            UiFactory.Place(screen._rank.rectTransform, 0.05f, 0.25f, 0.95f, 0.3f);

            Button store = UiFactory.CreateButton("Store", root, "Store", UiTheme.Warning, onStore, 56f);
            UiFactory.Place((RectTransform)store.transform, 0.3f, 0.08f, 0.7f, 0.15f);
            screen._store = store.gameObject;

            // The optional rewarded offer: a free booster, started only by the player (FR-052).
            Button free = UiFactory.CreateButton("FreeBooster", root, "Free booster ▶", UiTheme.Accent, () => onFreeBooster?.Invoke(), 40f);
            UiFactory.Place((RectTransform)free.transform, 0.3f, 0.17f, 0.7f, 0.23f);
            screen._freeBooster = free.gameObject;
            screen._freeBooster.SetActive(false);
            return screen;
        }

        public void SetFreeBoosterOffer(bool visible) => _freeBooster.SetActive(visible);

        public void Show(HomeModel model)
        {
            string level = model.CurrentLevel.ToString(CultureInfo.InvariantCulture);
            _level.text = "Level " + level;
            _playLabel.text = model.CurrentLevel == 1 ? "Play" : "Continue";
            _petals.text = model.Petals.ToString(CultureInfo.InvariantCulture);
            _store.SetActive(model.StoreUnlocked);
            _rank.gameObject.SetActive(model.LeaderboardUnlocked);
            _rank.text = model.RankText ?? "Rank: —";
            _milestone.text = model.NextMilestoneLevel.HasValue
                ? $"Level {model.NextMilestoneLevel.Value.ToString(CultureInfo.InvariantCulture)} reward in {model.LevelsToMilestone.GetValueOrDefault().ToString(CultureInfo.InvariantCulture)}"
                : string.Empty;
        }
    }
}
