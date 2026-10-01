using System;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
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
        bool LevelAvailable = true,
        ProfileLook? Profile = null,
        Outfit? AvatarOutfit = null,
        Color? Accent = null,
        BackgroundTheme? Theme = null,
        int DailyChallengePetals = 0);

    /// <summary>The Home buttons of the long-run features (US7); a null action hides its button.</summary>
    public sealed record HomeFeatureActions(Action? OnDailyChallenge, Action? OnWardrobe, Action? OnCollection, Action? OnLeaderboard, Action? OnProfile = null);

    /// <summary>
    /// Home of the design board's frames 2 and 3 (spec 002 FR-017; FR-058, T063).
    /// <list type="bullet">
    /// <item><description>Always shown: the Petals pill (its green "+" opens the Store once unlocked, L12), the round
    /// Settings button, "LEVEL N" and the big PLAY button.</description></item>
    /// <item><description>Early on, two Bloomlings sit on a stone under the wordmark.</description></item>
    /// <item><description>Once the Wardrobe is open (L40), the player's hero stands there, with round Wardrobe and
    /// Collection buttons beside it and the profile avatar (frame, badge) at the top.</description></item>
    /// <item><description>The milestone teaser reads "N levels to reward" with a gift.</description></item>
    /// <item><description>The rank row "Rank #N >" (L10) opens the Leaderboard and carries the marker.</description></item>
    /// <item><description>The Daily Challenge card (L50) shows "New today" and its reward.</description></item>
    /// <item><description>The optional free-booster ad offer is a small secondary button.</description></item>
    /// </list>
    /// Regions come from <see cref="ScreenLayout.Home"/> and <see cref="HomeLook"/>, shared with the playtest. There is
    /// no level map, and no button chooses a level: PLAY always continues Level N.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        private RectTransform _root = null!;
        private BackdropView _backdrop = null!;
        private RectTransform _topBar = null!;
        private RectTransform _settings = null!;
        private PetalsPill _petals = null!;
        private RectTransform _hero = null!;
        private GameObject _early = null!;
        private GameObject _progressed = null!;
        private BloomlingFigure _heroFigure = null!;
        private RectTransform _earlyGroup = null!;
        private Image _guestEarly = null!;
        private Image _guestLater = null!;
        private TextMeshProUGUI _level = null!;
        private TextMeshProUGUI _playLabel = null!;
        private Button _playButton = null!;
        private Image _teaser = null!;
        private TextMeshProUGUI _milestone = null!;
        private Image _rankRow = null!;
        private TextMeshProUGUI _rank = null!;
        private Image _rankMarker = null!;
        private Image _daily = null!;
        private TextMeshProUGUI _dailyTitle = null!;
        private TextMeshProUGUI _dailyReward = null!;
        private Image _dailyChevron = null!;
        private Image _dailyDone = null!;
        private GameObject _wardrobe = null!;
        private GameObject _collection = null!;
        private GameObject _freeBooster = null!;
        private ProfileAvatar _avatar = null!;
        private GameObject _profile = null!;
        private bool _freeBoosterShown;
        private HomeModel? _model;

        public static HomeScreen Create(RectTransform root, Action onPlay, Action onSettings, Action onStore, Action? onFreeBooster = null, HomeFeatureActions? features = null)
        {
            var screen = root.gameObject.AddComponent<HomeScreen>();
            screen._root = root;
            screen._backdrop = BackdropView.Create(root, BackdropScene.Home);

            screen._topBar = UiFactory.CreateRect("TopBar", root);
            screen._settings = (RectTransform)UiKit.RoundIconButton("Settings", screen._topBar, "ui.settings", onSettings).transform;
            screen._petals = UiKit.PetalsPill("Petals", screen._topBar, onStore);
            Button profile = UiFactory.CreateButton("Profile", screen._topBar, string.Empty, new Color(1f, 1f, 1f, 0f), () => features?.OnProfile?.Invoke());
            screen._avatar = ProfileAvatar.Create("Avatar", profile.transform);
            UiFactory.Stretch(screen._avatar.Rect);
            screen._profile = profile.gameObject;

            // The hero area: the early scene (frame 2) or the player's hero with its feature buttons (frame 3).
            screen._hero = UiFactory.CreateRect("Hero", root);
            RectTransform early = UiFactory.Stretch(UiFactory.CreateRect("Early", screen._hero));
            screen._early = early.gameObject;
            TextMeshProUGUI wordmark = UiKit.Label("Wordmark", early, Loc.T("home.logo"), DesignTokens.Type.Wordmark, UiTheme.Of(DesignTokens.Colors.WordmarkFill));
            wordmark.outlineColor = UiTheme.Of(DesignTokens.Colors.WordmarkOutline);
            UiFactory.Place(wordmark.rectTransform, 0.08f, 0.6f, 0.92f, 0.86f);
            screen._earlyGroup = HeroPictures.Group("Heroes", early);
            screen._guestEarly = HeroPictures.Guest("Leafling", early);

            RectTransform progressed = UiFactory.Stretch(UiFactory.CreateRect("Progressed", screen._hero));
            screen._progressed = progressed.gameObject;
            screen._heroFigure = BloomlingFigure.Create("HeroFigure", progressed);
            UiFactory.Place(screen._heroFigure.Rect, 0.2f, 0.04f, 0.8f, 0.96f);
            screen._guestLater = HeroPictures.Guest("Leafling", progressed);
            screen._wardrobe = UiKit.RoundIconButton("Wardrobe", screen._hero, "ui.shirt", () => features?.OnWardrobe?.Invoke()).gameObject;
            screen._collection = UiKit.RoundIconButton("Collection", screen._hero, "ui.grid", () => features?.OnCollection?.Invoke()).gameObject;

            screen._level = UiKit.Label("Level", root, Loc.F("common.level", 1), DesignTokens.Type.LevelHome, UiTheme.TextOnColor, look: TextLook.Headline);
            screen._teaser = UiKit.Pill("Teaser", root, new Color(UiTheme.Panel.r, UiTheme.Panel.g, UiTheme.Panel.b, 0.92f));
            screen._milestone = UiKit.Label("Text", screen._teaser.transform, string.Empty, DesignTokens.Type.Body, UiTheme.Of(DesignTokens.Colors.GardenLabelPlain));
            UiFactory.Place(screen._milestone.rectTransform, 0.06f, 0.08f, 0.82f, 0.92f);
            Image gift = UiFactory.CreateImage("Gift", screen._teaser.transform, ProceduralSprites.Shape("ui.gift"), UiTheme.Of(DesignTokens.Colors.BoosterBloomBurst));
            gift.preserveAspect = true;
            UiFactory.Place(gift.rectTransform, 0.84f, 0.14f, 0.96f, 0.86f);

            // PLAY: shorter and taller, with its ▶, leaves and flowers, breathing while it waits (spec 003 FR-010, FR-011a, FR-019).
            screen._playButton = UiKit.PrimaryButton("Play", root, Loc.T("common.play"), onPlay, DesignTokens.Type.ButtonLarge, decorate: true, playArrow: true);
            screen._playButton.GetComponent<GardenButton>().Breathe = true;
            screen._playLabel = screen._playButton.GetComponentInChildren<TextMeshProUGUI>();

            screen._rankRow = UiKit.Pill("Rank", root, new Color(1f, 1f, 1f, 0f), raycast: true);
            var rankButton = screen._rankRow.gameObject.AddComponent<Button>();
            rankButton.targetGraphic = screen._rankRow;
            rankButton.onClick.AddListener(() => features?.OnLeaderboard?.Invoke());
            Image trophy = UiFactory.CreateImage("Trophy", screen._rankRow.transform, ProceduralSprites.Shape("ui.trophy"), UiTheme.Of(DesignTokens.Colors.MedalGold));
            trophy.preserveAspect = true;
            UiFactory.Place(trophy.rectTransform, 0.02f, 0.12f, 0.16f, 0.88f);
            screen._rank = UiKit.Label("Text", screen._rankRow.transform, string.Empty, DesignTokens.Type.Body, UiTheme.Text);
            UiFactory.Place(screen._rank.rectTransform, 0.18f, 0.05f, 0.86f, 0.95f);
            Image chevron = UiFactory.CreateImage("Chevron", screen._rankRow.transform, ProceduralSprites.Shape("ui.chevron"), UiTheme.TextSecondary);
            chevron.preserveAspect = true;
            UiFactory.Place(chevron.rectTransform, 0.88f, 0.25f, 0.98f, 0.75f);
            screen._rankMarker = UiFactory.CreateImage("Marker", trophy.transform, ProceduralSprites.DoubleStar, Color.white);
            screen._rankMarker.preserveAspect = true;
            UiFactory.Place(screen._rankMarker.rectTransform, 0.5f, -0.2f, 1.2f, 0.5f);
            screen._rankMarker.gameObject.SetActive(false);

            // The Daily Challenge card (frame 3): sun, title, "New today · +N" with the Petal symbol, chevron.
            screen._daily = UiKit.Rounded("Daily", root, UiTheme.Panel, 40f, raycast: true);
            UiKit.CardShadow(screen._daily);
            var dailyButton = screen._daily.gameObject.AddComponent<Button>();
            dailyButton.targetGraphic = screen._daily;
            dailyButton.onClick.AddListener(() => features?.OnDailyChallenge?.Invoke());
            Image sun = UiFactory.CreateImage("Sun", screen._daily.transform, ProceduralSprites.Shape("ui.sun"), UiTheme.PetalCenter);
            sun.preserveAspect = true;
            UiFactory.Place(sun.rectTransform, 0.03f, 0.18f, 0.15f, 0.82f);
            screen._dailyTitle = UiKit.Label("Title", screen._daily.transform, Loc.T("daily.title"), DesignTokens.Type.Body, UiTheme.Text, TextAlignmentOptions.Left);
            UiFactory.Place(screen._dailyTitle.rectTransform, 0.18f, 0.5f, 0.86f, 0.92f);
            screen._dailyReward = UiKit.Label("Reward", screen._daily.transform, string.Empty, DesignTokens.Type.Caption, UiTheme.TextSecondary, TextAlignmentOptions.Left);
            UiFactory.Place(screen._dailyReward.rectTransform, 0.18f, 0.08f, 0.8f, 0.5f);
            screen._dailyChevron = UiFactory.CreateImage("Chevron", screen._daily.transform, ProceduralSprites.Shape("ui.chevron"), UiTheme.TextSecondary);
            screen._dailyChevron.preserveAspect = true;
            UiFactory.Place(screen._dailyChevron.rectTransform, 0.88f, 0.3f, 0.96f, 0.7f);
            screen._dailyDone = UiFactory.CreateImage("Done", screen._daily.transform, ProceduralSprites.Shape("ui.check"), UiTheme.Accent);
            screen._dailyDone.preserveAspect = true;
            UiFactory.Place(screen._dailyDone.rectTransform, 0.86f, 0.25f, 0.97f, 0.75f);

            // The optional rewarded offer: a free booster, started only by the player (FR-052).
            screen._freeBooster = UiKit.SecondaryButton("FreeBooster", root, Loc.T("home.free_booster"), () => onFreeBooster?.Invoke(), "ui.ad").gameObject;
            screen._freeBooster.SetActive(false);
            return screen;
        }

        public void SetFreeBoosterOffer(bool visible)
        {
            _freeBoosterShown = visible;
            _freeBooster.SetActive(visible);
            if (_model != null)
            {
                Layout(_model);
            }
        }

        /// <summary>What a system's Home demo points at (roadmap L10–L100), or null while it is not on screen.</summary>
        public RectTransform? DemoTarget(string unlockId) => unlockId switch
        {
            "system.leaderboard" => Visible(_rankRow.gameObject),
            "system.store" => Visible(_petals.Plus),
            "system.wardrobe" => Visible(_wardrobe),
            "system.daily_challenge" => Visible(_daily.gameObject),
            "system.milestone_25" => Visible(_teaser.gameObject),
            _ => null,
        };

        /// <summary>Whether a system's demo can play now: its button is shown (the theme demo needs none).</summary>
        public bool CanDemo(string unlockId) => unlockId == "system.theme_rotation" || DemoTarget(unlockId) != null;

        private static RectTransform? Visible(GameObject target) => target.activeSelf ? (RectTransform)target.transform : null;

        private HomeLook LookOf(HomeModel model) => new HomeLook(
            Store: model.StoreUnlocked,
            Teaser: model.NextMilestoneLevel.HasValue,
            Hero: model.WardrobeAvailable,
            Wardrobe: model.WardrobeAvailable,
            Collection: model.CollectionAvailable,
            Rank: model.LeaderboardUnlocked,
            DailyChallenge: model.DailyChallengeAvailable,
            FreeBoosterOffer: _freeBoosterShown);

        /// <summary>Places the regions of frame 2 or 3 for this look (data-model rule 4: locked features collapse).</summary>
        private void Layout(HomeModel model)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            HomeLook look = LookOf(model);
            HomeRegions r = ScreenLayout.Home(w, h, insets, look);
            var screen = new Box(0f, 0f, w, h);
            float u = DesignTokens.ScaleFor(w, h);
            UiKit.PlaceBox(_topBar, r.TopBar, screen);
            float bar = r.TopBar.Height;
            UiKit.PlaceBox(_settings, new Box(r.TopBar.Right - bar, r.TopBar.Top, r.TopBar.Right, r.TopBar.Bottom), r.TopBar);
            UiKit.PlaceBox((RectTransform)_petals.transform, new Box(r.TopBar.Right - bar - (24f * u) - (330f * u), r.TopBar.CenterY - (bar * 0.36f), r.TopBar.Right - bar - (24f * u), r.TopBar.CenterY + (bar * 0.36f)), r.TopBar);
            UiKit.PlaceBox((RectTransform)_profile.transform, new Box(r.TopBar.Left, r.TopBar.Top, r.TopBar.Left + bar, r.TopBar.Bottom), r.TopBar);
            UiKit.PlaceBox(_hero, r.Hero, screen);

            // The group with the Leafling guest early on, the guest beside the player's hero later (spec 004 R17).
            Box hero = r.Hero;
            (Box group, Box guest) = CharacterArt.GroupWithGuest(new Box(hero.Left, hero.Top + (hero.Height * 0.36f), hero.Right, hero.Bottom));
            UiKit.PlaceBox(_earlyGroup, group, hero);
            UiKit.PlaceBox(_guestEarly.rectTransform, guest, hero);
            var figure = new Box(hero.Left + (hero.Width * 0.2f), hero.Top + (hero.Height * 0.04f), hero.Left + (hero.Width * 0.8f), hero.Top + (hero.Height * 0.96f));
            UiKit.PlaceBox(_guestLater.rectTransform, CharacterArt.GuestBesideHero(CharacterArt.FitBox(figure, CharacterArt.HeroWidth, CharacterArt.HeroHeight), hero), hero);
            if (!r.Features.IsEmpty)
            {
                float button = DesignTokens.Size.IconButton * u;
                var first = new Box(r.Features.Left, r.Features.Top, r.Features.Right, r.Features.Top + button);
                UiKit.PlaceBox((RectTransform)_wardrobe.transform, first, r.Hero);
                UiKit.PlaceBox((RectTransform)_collection.transform, look.Wardrobe ? first.Offset(0f, button * 1.25f) : first, r.Hero);
            }

            UiKit.PlaceBox(_level.rectTransform, r.Level, screen);
            UiKit.PlaceBox(_teaser.rectTransform, r.Teaser, screen);
            UiKit.PlaceBox((RectTransform)_playButton.transform, r.Play, screen);
            UiKit.PlaceBox(_rankRow.rectTransform, r.Rank, screen);
            UiKit.PlaceBox(_daily.rectTransform, r.Daily, screen);
            UiKit.PlaceBox((RectTransform)_freeBooster.transform, Box.FromCenter(r.Extra.CenterX, r.Extra.CenterY, Mathf.Min(r.Extra.Width, 560f * u), r.Extra.Height), screen);
        }

        public void Show(HomeModel model)
        {
            _model = model;
            Layout(model);
            _level.text = Loc.F("common.level", NumberText.Group(model.CurrentLevel));
            _playLabel.text = !model.LevelAvailable ? Loc.T("home.more_levels_soon") : Loc.T("common.play");
            _playButton.interactable = model.LevelAvailable;
            _petals.Show(model.Petals, model.StoreUnlocked);
            _rankRow.gameObject.SetActive(model.LeaderboardUnlocked);
            _rank.text = model.RankText ?? Loc.T("home.rank_unknown");
            _daily.gameObject.SetActive(model.DailyChallengeAvailable);
            _dailyReward.text = model.DailyChallengeDone ? Loc.T("home.daily_done") : Loc.F("home.daily_new", model.DailyChallengePetals);
            _dailyDone.enabled = model.DailyChallengeDone;
            _dailyChevron.enabled = !model.DailyChallengeDone;
            _early.SetActive(!model.WardrobeAvailable);
            _progressed.SetActive(model.WardrobeAvailable);
            _wardrobe.SetActive(model.WardrobeAvailable);
            _collection.SetActive(model.CollectionAvailable);
            _profile.SetActive(model.WardrobeAvailable);
            if (model.WardrobeAvailable)
            {
                _avatar.Show(model.Profile, model.AvatarOutfit);
                _heroFigure.ShowHero(Family.Bloom, model.AvatarOutfit);
            }

            CosmeticItem? marker = model.Profile?.Marker;
            _rankMarker.gameObject.SetActive(marker != null);
            if (marker != null)
            {
                _rankMarker.color = BloomlingFigure.Tint(marker);
            }

            _backdrop.Show(model.Theme);
            _teaser.gameObject.SetActive(model.NextMilestoneLevel.HasValue);
            if (model.NextMilestoneLevel.HasValue)
            {
                int toGo = model.LevelsToMilestone.GetValueOrDefault();
                _milestone.text = toGo == 1 ? Loc.T("home.level_to_reward") : Loc.F("home.levels_to_reward", toGo);
            }
        }
    }
}
