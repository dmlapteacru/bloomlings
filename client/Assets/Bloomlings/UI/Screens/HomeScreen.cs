using System;
using System.Collections.Generic;
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
    /// <summary>
    /// What Home shows (FR-058), with the long-run features once unlocked (US7): the unlocks also decide the bottom menu's
    /// places (spec 005 FR-030; the rank shows on the Leaderboard card it opens).
    /// </summary>
    public sealed record HomeModel(
        int CurrentLevel,
        int Petals,
        bool StoreUnlocked,
        bool LeaderboardUnlocked,
        int? NextMilestoneLevel,
        int? LevelsToMilestone,
        bool DailyChallengeAvailable = false,
        bool DailyChallengeDone = false,
        bool WardrobeAvailable = false,
        bool CollectionAvailable = false,
        Color? Background = null,
        bool LevelAvailable = true,
        Color? Accent = null,
        BackgroundTheme? Theme = null,
        int DailyChallengePetals = 0,
        Func<Family, Outfit?>? OutfitOf = null);

    /// <summary>
    /// The Home button of the long-run features (US7) that is not a place of the bottom menu: the Daily Challenge. The
    /// Store, the Wardrobe, the Leaderboard and the Collection are the bottom menu's places (spec 005 FR-030).
    /// </summary>
    public sealed record HomeFeatureActions(Action? OnDailyChallenge);

    /// <summary>
    /// Home of the design board's frames 2 and 3 (spec 002 FR-017; FR-058, T063) in the reference look and layout of
    /// spec 005 (FR-024, contracts/look.md §4.5 and §6.4; the playtest's <c>HomeScreen</c>), every element placed from
    /// <see cref="ScreenLayout.ReferenceHome"/>:
    /// <list type="bullet">
    /// <item><description>Always shown: the cream round Settings button at the top left, the Petals pill at the top right
    /// (its green "+" opens the Store once unlocked, L12), the wooden logo across the top (the owner's logo picture when
    /// it exists), "Level N" on the wooden plaque and the big Play button in its wooden rim below it.</description></item>
    /// <item><description>The diorama in the middle (<see cref="HeroPictures.Stage"/>): over the owner's Home picture,
    /// its layered fountain with the four animated heroes where the reference stands them (spec 005 FR-028,
    /// <see cref="HomeLayersView"/>: Sprig at the left, Bloom behind the lotus, Drop at the right back, Twig at the right
    /// front), each idling and taking turns to react, reacting at once to a tap, with petals drifting over them; without
    /// the picture, the drawn diorama (the stone ring, the lotus fountain and the four still heroes), where the player's
    /// hero (<see cref="ProfileAvatar.HeroFamily"/>) stands at the left front. Once the Wardrobe is open (L40) each hero
    /// wears its outfit.</description></item>
    /// <item><description>The Daily Challenge (the sun, with a green check when done today, L50) as a small cream round
    /// side button at the right, once unlocked.</description></item>
    /// <item><description>Under Play, the milestone teaser "N levels to reward" with the pink gift on a parchment pill, and
    /// the optional free-booster ad offer as a cream "Free" pill beside it.</description></item>
    /// <item><description>The bottom menu (spec 005 FR-030, <see cref="BottomNavView"/>): the wooden bar with the
    /// unlocked places (the Shop from L12, the Wardrobe from L40, Home, the Leaderboard from L10, the Collection once a
    /// picture is won), Home in the raised medallion. It replaced Home's Store, Wardrobe (the profile avatar) and
    /// Collection side buttons and the rank pill (the owner's request of 2026-10-04).</description></item>
    /// </list>
    /// There is no level map, and no button chooses a level: Play always continues Level N.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        private RectTransform _root = null!;
        private BackdropView _backdrop = null!;
        private RectTransform _settings = null!;
        private PetalsPill _petals = null!;
        private RectTransform _logo = null!;
        private HomeStageView _stage = null!;
        private WoodSignView _level = null!;
        private TextMeshProUGUI _playLabel = null!;
        private Button _playButton = null!;
        private Image _teaser = null!;
        private TextMeshProUGUI _milestone = null!;
        private GameObject _daily = null!;
        private GameObject _dailyDone = null!;
        private RectTransform _freeBooster = null!;
        private RectTransform _freePill = null!;
        private BottomNavView _nav = null!;
        private bool _freeBoosterShown;
        private HomeModel? _model;

        /// <param name="onStore">The Petals pill's "+" (the Store page).</param>
        /// <param name="onNav">A tap on a place of the bottom menu (not Home's own).</param>
        public static HomeScreen Create(RectTransform root, Action onPlay, Action onSettings, Action onStore, Action? onFreeBooster = null, HomeFeatureActions? features = null, Action<NavPlace>? onNav = null)
        {
            var screen = root.gameObject.AddComponent<HomeScreen>();
            screen._root = root;

            // Home lies under the popups opened from it (Wardrobe, Collection, Daily Challenge, Leaderboard), which are
            // created before it on the same canvas.
            root.SetAsFirstSibling();
            screen._backdrop = BackdropView.Create(root, BackdropScene.Home);

            // The heroes (frames 2 and 3): the owner's layered fountain with the animated heroes, or the drawn diorama.
            // Everything built after it (the logo, the buttons, the plaque, Play, the pills) lies above it and keeps its
            // taps; a tap on a hero elsewhere makes it react.
            screen._stage = HeroPictures.Stage("Stage", root);

            // The logo across the top, over the garden in both looks.
            screen._logo = OwnerArt.Logo("Logo", root, Loc.T("home.logo"));

            // The Daily Challenge side button at the right (the other features are the bottom menu's places).
            Button daily = UiKit.RoundPictureButton("Daily", root, parent => UiKit.OutlinedGlyph("Sun", parent, "ui.sun", C.GardenFlowerCenter, C.GardenFlowerCenterLine), 0.72f, () => features?.OnDailyChallenge?.Invoke());
            screen._daily = daily.gameObject;
            screen._dailyDone = UiKit.CornerCheck("Done", daily.transform);

            // "Level N" on the wooden plaque (spec 005 §4.5, §6.4).
            screen._level = UiKit.WoodSign("Level", root, Loc.F("common.level", 1), T.LevelHome);

            // Play: the big primary button with its leaves and flowers, breathing while it waits (spec 003 FR-010, FR-011a,
            // FR-019), in its wooden rim (spec 005 §3.3); its label alone, centered and as big as the reference's "PLAY"
            // (ReferenceHomeRegions.PlayLabelShare of the button's height).
            (float sw, float sh, Insets si) = UiKit.ScreenFrame();
            float su = DesignTokens.ScaleFor(sw, sh);
            float playHeight = ScreenLayout.ReferenceHome(sw, sh, si).Play.Height;
            TypeStyle playStyle = T.ButtonLarge with { Size = Mathf.Max(T.ButtonLarge.Size, playHeight * ReferenceHomeRegions.PlayLabelShare / Mathf.Max(0.0001f, su)) };
            screen._playButton = UiKit.PrimaryButton("Play", root, Loc.T("common.play"), onPlay, playStyle, decorate: true, playArrow: false, breathe: true);
            screen._playLabel = screen._playButton.GetComponentInChildren<TextMeshProUGUI>();

            // The milestone teaser under Play: a parchment pill with the text and the pink gift after it.
            screen._teaser = UiKit.ParchmentPill("Teaser", root);
            screen._milestone = UiKit.KitLabel("Text", screen._teaser.transform, string.Empty, T.Body, TextLook.Plain(C.InkBrown));
            Image gift = UiKit.OutlinedGlyph("Gift", screen._teaser.transform, "ui.gift", C.LotusFill, C.LotusLine);
            TextMeshProUGUI milestone = screen._milestone;
            BoxLayout.On(screen._teaser.rectTransform).Watch(milestone).Then(b =>
            {
                float giftSize = b.Height * 0.72f;
                float gap = b.Height * 0.2f;
                float size = Mathf.Min(UiKit.Units(T.Body.Size), b.Height * 0.5f);
                float textWidth = Fit(milestone, size, b.Width - giftSize - gap - (b.Height * 0.8f));
                float start = b.CenterX - ((textWidth + gap + giftSize) / 2f);
                KitText.Place(milestone, T.Body, start + (textWidth / 2f), b.CenterY, size, textWidth + 1f);
                BoxLayout.Place(gift.rectTransform, Box.FromCenter(start + textWidth + gap + (giftSize / 2f), b.CenterY - (giftSize * 0.03f), giftSize, giftSize));
            });

            // The optional rewarded offer beside the teaser, started only by the player (FR-052): a cream pill with the green
            // ▶ and "Free", in a clear touch box at least size.touch_min.
            Image free = UiFactory.CreateImage("FreeBooster", root, null, Color.clear, raycast: true);
            UiKit.TapTarget(free, () => onFreeBooster?.Invoke(), press: true);
            screen._freeBooster = free.rectTransform;
            screen._freePill = (RectTransform)UiKit.CostPill("Pill", free.transform, Cost.Free).transform;
            free.gameObject.SetActive(false);

            // The top corners: Settings at the left, the Petals pill at the right; the bottom menu last, over the stage.
            screen._settings = (RectTransform)UiKit.RoundIconButton("Settings", root, "ui.settings", onSettings).transform;
            screen._petals = UiKit.PetalsPill("Petals", root, onStore);
            screen._nav = UiKit.BottomNav("BottomNav", root, place =>
            {
                if (place != NavPlace.Home)
                {
                    onNav?.Invoke(place);
                }
            });
            return screen;
        }

        public void SetFreeBoosterOffer(bool visible)
        {
            _freeBoosterShown = visible;
            _freeBooster.gameObject.SetActive(visible);
            if (_model != null)
            {
                Layout(_model);
            }
        }

        /// <summary>
        /// What a system's Home demo points at (roadmap L10–L100), or null while it is not on screen: the bottom menu's
        /// Leaderboard, Shop and Wardrobe places (spec 005 FR-030), the Daily Challenge button and the milestone teaser.
        /// </summary>
        public RectTransform? DemoTarget(string unlockId) => unlockId switch
        {
            "system.leaderboard" => _nav.PlaceRect(NavPlace.Leaderboard),
            "system.store" => _nav.PlaceRect(NavPlace.Shop),
            "system.wardrobe" => _nav.PlaceRect(NavPlace.Wardrobe),
            "system.daily_challenge" => Visible(_daily),
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

        /// <summary>
        /// Places every element of frame 2 or 3 on the reference regions (contracts/look.md §6.4; data-model rule 4:
        /// locked features collapse, so the bottom menu shows only the unlocked places).
        /// </summary>
        private void Layout(HomeModel model)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            HomeLook look = LookOf(model);
            ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets);
            var screen = new Box(0f, 0f, w, h);
            float u = DesignTokens.ScaleFor(w, h);
            UiKit.PlaceBox(_settings, r.Settings, screen);
            UiKit.PlaceBox((RectTransform)_petals.transform, r.Petals, screen);
            UiKit.PlaceBox(_logo, OwnerArt.LogoBox(r), screen);

            // The four heroes (frames 2 and 3): on the owner's layered fountain, or around the drawn diorama's lotus
            // fountain with the player's hero at the left front; once the Wardrobe is open each in its outfit.
            _stage.Place(r.Diorama, screen, BackdropScene.Home, outfitOf: look.Hero ? model.OutfitOf : null, front: look.Hero ? ProfileAvatar.HeroFamily : Family.Sprig);

            // The Daily Challenge, the right column's first side button.
            UiKit.PlaceBox((RectTransform)_daily.transform, r.Daily, screen);
            float touch = DesignTokens.Size.TouchMin * u;
            Box Touch(Box b) => Box.FromCenter(b.CenterX, b.CenterY, Mathf.Max(b.Width, touch), Mathf.Max(b.Height, touch));

            // The level plaque: 0.5 W, wider when its letters need more (at most 0.8 W).
            float plaque = r.Plaque.Height;
            float ppu = Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            float font = Mathf.Min(T.LevelHome.Size * u, plaque * 0.62f);
            float letters = KitText.Measure(_level.Label, font / ppu) * ppu;
            float plaqueWidth = Mathf.Min(r.W * 0.8f, Mathf.Max(r.Plaque.Width, letters + (plaque * 1.5f)));
            UiKit.PlaceBox((RectTransform)_level.transform, Box.FromCenter(r.Plaque.CenterX, r.Plaque.CenterY, plaqueWidth, plaque), screen);
            UiKit.PlaceBox((RectTransform)_playButton.transform, r.Play, screen);
            UiKit.PlaceBox(_teaser.rectTransform, r.Teaser, screen);

            // The free booster's pill in its region, its touch box grown to size.touch_min around it.
            UiKit.PlaceBox(_freeBooster, Touch(r.FreeBooster), screen);
            UiKit.PlaceBox(_freePill, r.FreeBooster, Touch(r.FreeBooster));

            // The bottom menu, Home in its medallion (spec 005 FR-030).
            _nav.Show(BottomNav.Places(look), NavPlace.Home);
        }

        public void Show(HomeModel model)
        {
            _model = model;
            _level.Text = Loc.F("common.level", NumberText.Group(model.CurrentLevel));
            Layout(model);
            _playLabel.text = !model.LevelAvailable ? Loc.T("home.more_levels_soon") : Loc.T("common.play");
            _playButton.interactable = model.LevelAvailable;
            _petals.Show(model.Petals, model.StoreUnlocked);
            _daily.SetActive(model.DailyChallengeAvailable);
            _dailyDone.SetActive(model.DailyChallengeDone);
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
    }
}
