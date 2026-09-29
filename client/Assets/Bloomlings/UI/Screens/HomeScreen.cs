using System;
using System.Globalization;
using Bloomlings.Client.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>What Home shows (FR-058), with the long-run features once unlocked (US7).</summary>
    public sealed record HomeModel(
        int CurrentLevel,
        int Petals,
        bool StoreUnlocked,
        bool LeaderboardUnlocked,
        string? RankText,
        int? NextMilestoneLevel,
        int? LevelsToMilestone,
        bool DailyChallengeAvailable = false,
        bool DailyChallengeDone = false,
        bool WardrobeAvailable = false,
        bool CollectionAvailable = false,
        Color? Background = null,
        bool LevelAvailable = true);

    /// <summary>The Home buttons of the long-run features (US7); a null action hides its button.</summary>
    public sealed record HomeFeatureActions(Action? OnDailyChallenge, Action? OnWardrobe, Action? OnCollection, Action? OnLeaderboard);

    /// <summary>
    /// Home (FR-058, T063): the logo, Level N, one Play/Continue button and Petals; Settings; the Store button once
    /// unlocked (L12); the next-milestone teaser; the leaderboard rank once unlocked (L10), which opens the board. A
    /// row of small buttons opens the Daily Challenge (L50), the Wardrobe (L40) and the Collection. There is no level
    /// map, and none of these buttons chooses a level: Play always continues Level N.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        private TextMeshProUGUI _level = null!;
        private TextMeshProUGUI _petals = null!;
        private TextMeshProUGUI _playLabel = null!;
        private Button _playButton = null!;
        private TextMeshProUGUI _milestone = null!;
        private TextMeshProUGUI _rank = null!;
        private Image _background = null!;
        private GameObject _rankButton = null!;
        private GameObject _daily = null!;
        private TextMeshProUGUI _dailyLabel = null!;
        private GameObject _wardrobe = null!;
        private GameObject _collection = null!;
        private GameObject _store = null!;
        private GameObject _freeBooster = null!;

        public static HomeScreen Create(RectTransform root, Action onPlay, Action onSettings, Action onStore, Action? onFreeBooster = null, HomeFeatureActions? features = null)
        {
            var screen = root.gameObject.AddComponent<HomeScreen>();
            Image background = UiFactory.CreateImage("Background", root, null, UiTheme.Background);
            UiFactory.Stretch(background.rectTransform);
            screen._background = background;

            TextMeshProUGUI logo = UiFactory.CreateText("Logo", root, Loc.T("home.logo"), 120f, UiTheme.Accent);
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

            screen._level = UiFactory.CreateText("Level", root, Loc.F("common.level", 1), 84f, UiTheme.Text);
            UiFactory.Place(screen._level.rectTransform, 0.05f, 0.52f, 0.95f, 0.6f);

            Button play = UiFactory.CreateButton("Play", root, Loc.T("common.play"), UiTheme.Accent, onPlay, 84f);
            UiFactory.Place((RectTransform)play.transform, 0.2f, 0.38f, 0.8f, 0.49f);
            screen._playLabel = play.GetComponentInChildren<TextMeshProUGUI>();
            screen._playButton = play;

            screen._milestone = UiFactory.CreateText("Milestone", root, string.Empty, 44f, UiTheme.Text);
            UiFactory.Place(screen._milestone.rectTransform, 0.05f, 0.31f, 0.95f, 0.36f);

            Button rank = UiFactory.CreateButton("Rank", root, string.Empty, UiTheme.Panel, () => features?.OnLeaderboard?.Invoke(), 44f);
            UiFactory.Place((RectTransform)rank.transform, 0.2f, 0.245f, 0.8f, 0.305f);
            screen._rank = rank.GetComponentInChildren<TextMeshProUGUI>();
            screen._rank.color = UiTheme.Text;
            screen._rankButton = rank.gameObject;

            // Long-run features (US7): small buttons between the logo and Level N.
            screen._daily = Feature(root, "Daily", Loc.T("home.daily"), 0.05f, features?.OnDailyChallenge);
            screen._dailyLabel = screen._daily.GetComponentInChildren<TextMeshProUGUI>();
            screen._wardrobe = Feature(root, "Wardrobe", Loc.T("home.wardrobe"), 0.36f, features?.OnWardrobe);
            screen._collection = Feature(root, "Collection", Loc.T("home.collection"), 0.67f, features?.OnCollection);

            Button store = UiFactory.CreateButton("Store", root, Loc.T("home.store"), UiTheme.Warning, onStore, 56f);
            UiFactory.Place((RectTransform)store.transform, 0.3f, 0.08f, 0.7f, 0.15f);
            screen._store = store.gameObject;

            // The optional rewarded offer: a free booster, started only by the player (FR-052).
            Button free = UiFactory.CreateButton("FreeBooster", root, Loc.T("home.free_booster"), UiTheme.Accent, () => onFreeBooster?.Invoke(), 40f);
            UiFactory.Place((RectTransform)free.transform, 0.3f, 0.17f, 0.7f, 0.23f);
            screen._freeBooster = free.gameObject;
            screen._freeBooster.SetActive(false);
            return screen;
        }

        public void SetFreeBoosterOffer(bool visible) => _freeBooster.SetActive(visible);

        private static GameObject Feature(RectTransform root, string name, string label, float x0, Action? onClick)
        {
            Button button = UiFactory.CreateButton(name, root, label, UiTheme.SlotLocked, () => onClick?.Invoke(), 40f);
            UiFactory.Place((RectTransform)button.transform, x0, 0.625f, x0 + 0.28f, 0.685f);
            button.gameObject.SetActive(false);
            return button.gameObject;
        }

        public void Show(HomeModel model)
        {
            string level = model.CurrentLevel.ToString(CultureInfo.InvariantCulture);
            _level.text = Loc.F("common.level", level);
            _playLabel.text = !model.LevelAvailable ? Loc.T("home.more_levels_soon")
                : model.CurrentLevel == 1 ? Loc.T("common.play")
                : Loc.T("home.continue");
            _playButton.interactable = model.LevelAvailable;
            _petals.text = model.Petals.ToString(CultureInfo.InvariantCulture);
            _store.SetActive(model.StoreUnlocked);
            _rankButton.SetActive(model.LeaderboardUnlocked);
            _rank.text = model.RankText ?? Loc.T("home.rank_unknown");
            _daily.SetActive(model.DailyChallengeAvailable);
            _dailyLabel.text = model.DailyChallengeDone ? Loc.T("home.daily_done") : Loc.T("home.daily");
            _wardrobe.SetActive(model.WardrobeAvailable);
            _collection.SetActive(model.CollectionAvailable);
            _background.color = model.Background ?? UiTheme.Background;
            _milestone.text = model.NextMilestoneLevel.HasValue
                ? Loc.F("home.milestone_teaser", model.NextMilestoneLevel.Value, model.LevelsToMilestone.GetValueOrDefault())
                : string.Empty;
        }
    }
}
