using System;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

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
    /// Home of the design board's frames 2 and 3 (spec 002 FR-017; FR-058, T063) in the reference look of spec 005
    /// (contracts/look.md §4.5; the playtest's <c>HomeScreen</c>).
    /// <list type="bullet">
    /// <item><description>Always shown: the Petals pill (its green "+" opens the Store once unlocked, L12), the cream
    /// round Settings button, "Level N" on a wooden plaque and the big Play button in its wooden rim.</description></item>
    /// <item><description>Early on, the wooden logo (the owner's logo picture when it exists) over the four heroes around
    /// the lotus fountain on a stone pedestal, and the guest.</description></item>
    /// <item><description>Once the Wardrobe is open (L40), the player's hero stands on a stone pedestal, with cream round
    /// Wardrobe and Collection buttons beside it and the profile avatar (frame, badge) at the top.</description></item>
    /// <item><description>The milestone teaser "N levels to reward" with the pink gift and the rank row "Rank #N >" with
    /// the gold trophy (L10, it opens the Leaderboard and carries the marker) are parchment pills.</description></item>
    /// <item><description>The Daily Challenge card (L50) is parchment with the sun on a cream disc, "New today" and its
    /// reward.</description></item>
    /// <item><description>The optional free-booster ad offer is a small cream secondary button.</description></item>
    /// </list>
    /// Regions come from <see cref="ScreenLayout.Home"/>, <see cref="HomeLook"/> and <see cref="HomeStage"/>, shared with
    /// the playtest. There is no level map, and no button chooses a level: Play always continues Level N.
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
        private RectTransform _logo = null!;
        private HomeStageView _stage = null!;
        private RectTransform _pedestal = null!;
        private BloomlingFigure _heroFigure = null!;
        private Image _guestLater = null!;
        private WoodSignView _level = null!;
        private TextMeshProUGUI _playLabel = null!;
        private Button _playButton = null!;
        private Image _teaser = null!;
        private TextMeshProUGUI _milestone = null!;
        private Image _rankRow = null!;
        private TextMeshProUGUI _rank = null!;
        private Image _rankMarker = null!;
        private Image _daily = null!;
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

            // Home lies under the popups opened from it (Wardrobe, Collection, Daily Challenge, Leaderboard), which are
            // created before it on the same canvas.
            root.SetAsFirstSibling();
            screen._backdrop = BackdropView.Create(root, BackdropScene.Home);

            screen._topBar = UiFactory.CreateRect("TopBar", root);
            screen._settings = (RectTransform)UiKit.RoundIconButton("Settings", screen._topBar, "ui.settings", onSettings).transform;
            screen._petals = UiKit.PetalsPill("Petals", screen._topBar, onStore);
            // The profile avatar is its own cream disc; the button around it is a clear touch target.
            Image profile = UiFactory.CreateImage("Profile", screen._topBar, null, Color.clear, raycast: true);
            var profileButton = profile.gameObject.AddComponent<Button>();
            profileButton.transition = Selectable.Transition.None;
            profileButton.targetGraphic = profile;
            profileButton.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                features?.OnProfile?.Invoke();
            });
            profile.gameObject.AddComponent<PressMotion>();
            screen._avatar = ProfileAvatar.Create("Avatar", profile.transform);
            UiFactory.Stretch(screen._avatar.Rect);
            screen._profile = profile.gameObject;

            // The hero area: the logo over the drawn stage (frame 2) or the player's hero on its pedestal (frame 3).
            screen._hero = UiFactory.CreateRect("Hero", root);
            RectTransform early = UiFactory.Stretch(UiFactory.CreateRect("Early", screen._hero));
            screen._early = early.gameObject;
            screen._stage = HeroPictures.Stage("Stage", early);
            screen._logo = OwnerArt.Logo("Logo", early, Loc.T("home.logo"));

            RectTransform progressed = UiFactory.Stretch(UiFactory.CreateRect("Progressed", screen._hero));
            screen._progressed = progressed.gameObject;
            screen._pedestal = UiKit.StonePedestal("Pedestal", progressed);
            screen._heroFigure = BloomlingFigure.Create("HeroFigure", progressed);
            screen._heroFigure.Body.raycastTarget = false;
            screen._guestLater = HeroPictures.Guest("Leafling", progressed);
            screen._wardrobe = UiKit.RoundIconButton("Wardrobe", screen._hero, "ui.shirt", () => features?.OnWardrobe?.Invoke()).gameObject;
            screen._collection = UiKit.RoundIconButton("Collection", screen._hero, "ui.grid", () => features?.OnCollection?.Invoke()).gameObject;

            // "Level N" on a plain wooden plaque (spec 005 §4.5).
            screen._level = UiKit.WoodSign("Level", root, Loc.F("common.level", 1), T.LevelHome);

            // The milestone teaser: a parchment pill with the text and the pink gift after it.
            screen._teaser = UiKit.ParchmentPill("Teaser", root);
            screen._milestone = UiKit.KitLabel("Text", screen._teaser.transform, string.Empty, T.Body, TextLook.Plain(C.InkBrown));
            Image gift = UiKit.OutlinedGlyph("Gift", screen._teaser.transform, "ui.gift", C.LotusFill, C.LotusLine);
            TextMeshProUGUI milestone = screen._milestone;
            BoxLayout.On(screen._teaser.rectTransform).Watch(milestone).Then(b =>
            {
                float giftSize = b.Height * 0.68f;
                float gap = UiKit.Units(18f);
                float size = UiKit.Units(T.Body.Size);
                float textWidth = Fit(milestone, size, b.Width - (giftSize * 2.6f));
                float start = b.CenterX - ((textWidth + gap + giftSize) / 2f);
                KitText.Place(milestone, T.Body, start + (textWidth / 2f), b.CenterY, size, textWidth + 1f);
                BoxLayout.Place(gift.rectTransform, Box.FromCenter(start + textWidth + gap + (giftSize / 2f), b.CenterY - (giftSize * 0.03f), giftSize, giftSize));
            });

            // Play: shorter and taller, with its ▶, leaves and flowers, breathing while it waits (spec 003 FR-010,
            // FR-011a, FR-019), in its wooden rim (spec 005 §3.3).
            screen._playButton = UiKit.PrimaryButton("Play", root, Loc.T("common.play"), onPlay, T.ButtonLarge, decorate: true, playArrow: true, breathe: true);
            screen._playLabel = screen._playButton.GetComponentInChildren<TextMeshProUGUI>();

            // The rank row: a parchment pill with the gold trophy, the rank and a brown chevron; it opens the Leaderboard.
            screen._rankRow = UiKit.ParchmentPill("Rank", root, raycast: true);
            var rankButton = screen._rankRow.gameObject.AddComponent<Button>();
            rankButton.transition = Selectable.Transition.None;
            rankButton.targetGraphic = screen._rankRow;
            rankButton.onClick.AddListener(() => features?.OnLeaderboard?.Invoke());
            Image trophy = UiKit.OutlinedGlyph("Trophy", screen._rankRow.transform, "ui.trophy", C.MedalGold, C.MedalGold.Darken(0.42f));
            screen._rank = UiKit.KitLabel("Text", screen._rankRow.transform, string.Empty, T.Body, TextLook.Plain(C.InkBrown));
            Image chevron = UiKit.ShapeImage("Chevron", screen._rankRow.transform, "ui.chevron", C.InkBrownSoft);
            screen._rankMarker = UiFactory.CreateImage("Marker", trophy.transform, ProceduralSprites.DoubleStar, Color.white);
            screen._rankMarker.preserveAspect = true;
            UiFactory.Place(screen._rankMarker.rectTransform, 0.5f, -0.2f, 1.2f, 0.5f);
            screen._rankMarker.gameObject.SetActive(false);
            TextMeshProUGUI rank = screen._rank;
            BoxLayout.On(screen._rankRow.rectTransform).Watch(rank).Then(b =>
            {
                float icon = b.Height * 0.66f;
                float chevronSize = icon * 0.5f;
                float gap = UiKit.Units(16f);
                float size = UiKit.Units(T.Body.Size);
                float textWidth = Fit(rank, size, b.Width - (icon * 3f));
                float start = b.CenterX - ((icon + gap + textWidth + gap + chevronSize) / 2f);
                BoxLayout.Place(trophy.rectTransform, Box.FromCenter(start + (icon / 2f), b.CenterY, icon, icon));
                KitText.Place(rank, T.Body, start + icon + gap + (textWidth / 2f), b.CenterY, size, textWidth + 1f);
                BoxLayout.Place(chevron.rectTransform, Box.FromCenter(start + icon + gap + textWidth + gap + (chevronSize / 2f), b.CenterY, chevronSize, chevronSize));
            });

            // The Daily Challenge card (frame 3) on parchment: the sun on a cream disc, the title, "New today · +N", a chevron.
            screen._daily = UiKit.Paper("Daily", root, b => b.Height * 0.3f, DesignTokens.Garden.OutlineWidth, 6f, raycast: true);
            var dailyButton = screen._daily.gameObject.AddComponent<Button>();
            dailyButton.transition = Selectable.Transition.None;
            dailyButton.targetGraphic = screen._daily;
            dailyButton.onClick.AddListener(() => features?.OnDailyChallenge?.Invoke());
            GardenButton disc = UiKit.IconFace("Disc", screen._daily.transform, GardenLook.White, b => b.Height / 2f, square: true);
            Image sun = UiKit.OutlinedGlyph("Sun", screen._daily.transform, "ui.sun", C.GardenFlowerCenter, C.GardenFlowerCenterLine);
            TextMeshProUGUI title = UiKit.KitLabel("Title", screen._daily.transform, Loc.T("daily.title"), T.ButtonSecondary, TextLook.Plain(C.InkBrown));
            screen._dailyReward = UiKit.KitLabel("Reward", screen._daily.transform, string.Empty, T.Caption, TextLook.Plain(C.InkBrownSoft));
            screen._dailyChevron = UiKit.ShapeImage("Chevron", screen._daily.transform, "ui.chevron", C.InkBrownSoft);
            screen._dailyDone = UiKit.ShapeImage("Done", screen._daily.transform, "ui.check", GardenLook.Green.Face);
            TextMeshProUGUI reward = screen._dailyReward;
            Image dailyChevron = screen._dailyChevron;
            Image dailyDone = screen._dailyDone;
            BoxLayout.On(screen._daily.rectTransform).Then(b =>
            {
                float h = b.Height;
                float discSize = h * 0.66f;
                float cx = b.Left + (h * 0.56f);
                BoxLayout.Place((RectTransform)disc.transform, Box.FromCenter(cx, b.CenterY, discSize, discSize));
                BoxLayout.Place(sun.rectTransform, Box.FromCenter(cx, b.CenterY - (discSize * 0.03f), discSize * 1.04f, discSize * 1.04f));
                float left = b.Left + (h * 1.05f);
                float width = b.Width - (h * 1.7f);
                PlaceLeft(title, T.ButtonSecondary, left, b.Top + (h * 0.37f), UiKit.Units(T.ButtonSecondary.Size) * 0.9f, width);
                PlaceLeft(reward, T.Caption, left, b.Top + (h * 0.69f), UiKit.Units(T.Caption.Size), width);
                BoxLayout.Place(dailyChevron.rectTransform, Box.FromCenter(b.Right - (h * 0.42f), b.CenterY, h * 0.3f, h * 0.3f));
                BoxLayout.Place(dailyDone.rectTransform, Box.FromCenter(b.Right - (h * 0.42f), b.CenterY, h * 0.4f, h * 0.4f));
            });

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

            Box hero = r.Hero;
            UiKit.PlaceBox(_hero, hero, screen);
            if (!look.Hero)
            {
                // Frame 2: the logo, then the four heroes around the lotus fountain on the stone, the guest at the front.
                float logoWidth = Mathf.Min(hero.Width * 0.8f, 760f * u);
                UiKit.PlaceBox(_logo, Box.FromCenter(hero.CenterX, hero.Top + (hero.Height * 0.22f), logoWidth, Mathf.Min(logoWidth * 0.37f, T.Wordmark.Size * u * 1.5f)), hero);
                _stage.Place(new Box(hero.Left, hero.Top + (hero.Height * 0.36f), hero.Right, hero.Bottom), hero, BackdropScene.Home, guest: true);
            }
            else
            {
                // Frame 3: the player's hero on the stone pedestal (until the owner's garden brings its own ground), the
                // guest beside it.
                (Box pedestal, Box body, Box guest) = HomeStage.HeroOnPedestal(new Box(hero.Left, hero.Top, hero.Right, hero.Bottom - (8f * u)));
                _pedestal.gameObject.SetActive(OwnerArt.Background(OwnerPictures.Home) == null);
                UiKit.PlaceBox(_pedestal, pedestal, hero);
                UiKit.PlaceBox(_heroFigure.Rect, body, hero);
                UiKit.PlaceBox(_guestLater.rectTransform, guest, hero);
            }

            if (!r.Features.IsEmpty)
            {
                float button = DesignTokens.Size.IconButton * u;
                float y = r.Features.Top + (button * 0.6f);
                UiKit.PlaceBox((RectTransform)_wardrobe.transform, Box.FromCenter(r.Features.CenterX, y, button, button), hero);
                UiKit.PlaceBox((RectTransform)_collection.transform, Box.FromCenter(r.Features.CenterX, look.Wardrobe ? y + (button * 1.25f) : y, button, button), hero);
            }

            // The level plaque: as tall as its row and as wide as its letters plus 1.5 × its height.
            float plaque = r.Level.Height;
            float ppu = Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            float font = Mathf.Min(T.LevelHome.Size * u, plaque * 0.62f);
            float letters = KitText.Measure(_level.Label, font / ppu) * ppu;
            float plaqueWidth = Mathf.Min(r.Level.Width * 0.8f, (letters > 0f ? letters : font * 3.5f) + (plaque * 1.5f));
            UiKit.PlaceBox((RectTransform)_level.transform, Box.FromCenter(r.Level.CenterX, r.Level.CenterY, plaqueWidth, plaque), screen);
            UiKit.PlaceBox(_teaser.rectTransform, r.Teaser, screen);
            UiKit.PlaceBox((RectTransform)_playButton.transform, r.Play, screen);
            UiKit.PlaceBox(_rankRow.rectTransform, r.Rank, screen);
            UiKit.PlaceBox(_daily.rectTransform, r.Daily, screen);
            UiKit.PlaceBox((RectTransform)_freeBooster.transform, Box.FromCenter(r.Extra.CenterX, r.Extra.CenterY, Mathf.Min(r.Extra.Width, 560f * u), r.Extra.Height), screen);
        }

        public void Show(HomeModel model)
        {
            _model = model;
            _level.Text = Loc.F("common.level", NumberText.Group(model.CurrentLevel));
            Layout(model);
            _playLabel.text = !model.LevelAvailable ? Loc.T("home.more_levels_soon") : Loc.T("common.play");
            _playButton.interactable = model.LevelAvailable;
            _petals.Show(model.Petals, model.StoreUnlocked);
            _rankRow.gameObject.SetActive(model.LeaderboardUnlocked);
            _rank.text = model.RankText ?? Loc.T("home.rank_unknown");
            _daily.gameObject.SetActive(model.DailyChallengeAvailable);
            _dailyReward.text = model.DailyChallengeDone ? Loc.T("home.daily_done") : Loc.F("home.daily_new", model.DailyChallengePetals);
            _dailyDone.gameObject.SetActive(model.DailyChallengeDone);
            _dailyChevron.gameObject.SetActive(!model.DailyChallengeDone);
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

        /// <summary>A label's width at <paramref name="size"/> canvas units, at most <paramref name="room"/> (all of it before the text engine can tell).</summary>
        private static float Fit(TextMeshProUGUI label, float size, float room)
        {
            float measured = KitText.Measure(label, size);
            return Mathf.Max(1f, Mathf.Min(measured > 0f ? measured : room, room));
        }

        /// <summary>A left-aligned label from <paramref name="left"/>, centered on <paramref name="cy"/>, shrinking to <paramref name="width"/>.</summary>
        private static void PlaceLeft(TextMeshProUGUI label, TypeStyle style, float left, float cy, float size, float width)
        {
            label.alignment = TextAlignmentOptions.Left;
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSize = size;
            label.fontSizeMin = Mathf.Min(size, size * style.Min / Mathf.Max(1f, style.Size));
            BoxLayout.Place(label.rectTransform, new Box(left, cy - (size * 0.8f), left + Mathf.Max(1f, width), cy + (size * 0.8f)));
        }
    }
}
