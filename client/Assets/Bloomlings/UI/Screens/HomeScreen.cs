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
    /// Home of the design board's frames 2 and 3 (spec 002 FR-017; FR-058, T063) in the reference look and layout of
    /// spec 005 (FR-024, contracts/look.md §4.5 and §6.4; the playtest's <c>HomeScreen</c>), every element placed from
    /// <see cref="ScreenLayout.ReferenceHome"/>:
    /// <list type="bullet">
    /// <item><description>Always shown: the cream round Settings button at the top left, the Petals pill at the top right
    /// (its green "+" opens the Store once unlocked, L12), the wooden logo across the top (the owner's logo picture when
    /// it exists), "Level N" on the wooden plaque and the big Play button in its wooden rim below it.</description></item>
    /// <item><description>The diorama in the middle: early on, the four heroes around the lotus fountain on a stone
    /// pedestal and the guest; once the Wardrobe is open (L40), the player's hero on its pedestal with the guest beside
    /// it.</description></item>
    /// <item><description>Small cream round side buttons, each once unlocked, packed from the top of their column:
    /// Wardrobe, Collection and the profile avatar (frame, badge) at the left; the Daily Challenge (the sun, with a green
    /// check when done today, L50) and the Store (the lotus) at the right, with the rank pill "Rank #N >" and its gold
    /// trophy under them (L10; it opens the Leaderboard and carries the marker).</description></item>
    /// <item><description>Under Play, the milestone teaser "N levels to reward" with the pink gift on a parchment pill, and
    /// the optional free-booster ad offer as a cream "Free" pill beside it.</description></item>
    /// </list>
    /// There is no level map, and no button chooses a level: Play always continues Level N.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        private RectTransform _root = null!;
        private BackdropView _backdrop = null!;
        private RectTransform _settings = null!;
        private PetalsPill _petals = null!;
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
        private RectTransform _rankTouch = null!;
        private Image _rankRow = null!;
        private TextMeshProUGUI _rank = null!;
        private Image _rankMarker = null!;
        private GameObject _daily = null!;
        private GameObject _dailyDone = null!;
        private GameObject _store = null!;
        private GameObject _wardrobe = null!;
        private GameObject _collection = null!;
        private RectTransform _freeBooster = null!;
        private RectTransform _freePill = null!;
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

            // The diorama: the drawn stage with the four heroes (frame 2) or the player's hero on its pedestal (frame 3).
            RectTransform early = UiFactory.Stretch(UiFactory.CreateRect("Early", root));
            screen._early = early.gameObject;
            screen._stage = HeroPictures.Stage("Stage", early);
            RectTransform progressed = UiFactory.Stretch(UiFactory.CreateRect("Progressed", root));
            screen._progressed = progressed.gameObject;
            screen._pedestal = UiKit.StonePedestal("Pedestal", progressed);
            screen._heroFigure = BloomlingFigure.Create("HeroFigure", progressed);
            screen._heroFigure.Body.raycastTarget = false;
            screen._guestLater = HeroPictures.Guest("Leafling", progressed);

            // The logo across the top, over the garden in both looks.
            screen._logo = OwnerArt.Logo("Logo", root, Loc.T("home.logo"));

            // The side buttons: Wardrobe, Collection and the profile avatar at the left; the Daily Challenge and the Store
            // at the right.
            screen._wardrobe = UiKit.RoundIconButton("Wardrobe", root, "ui.shirt", () => features?.OnWardrobe?.Invoke()).gameObject;
            screen._collection = UiKit.RoundIconButton("Collection", root, "ui.grid", () => features?.OnCollection?.Invoke()).gameObject;

            // The profile avatar is its own cream disc; the button around it is a clear touch target.
            Image profile = UiFactory.CreateImage("Profile", root, null, Color.clear, raycast: true);
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

            Button daily = UiKit.RoundPictureButton("Daily", root, parent => UiKit.OutlinedGlyph("Sun", parent, "ui.sun", C.GardenFlowerCenter, C.GardenFlowerCenterLine), 0.72f, () => features?.OnDailyChallenge?.Invoke());
            screen._daily = daily.gameObject;
            screen._dailyDone = UiKit.CornerCheck("Done", daily.transform);
            screen._store = UiKit.RoundPictureButton("Store", root, parent => UiKit.PetalIcon("Lotus", parent), 0.7f, onStore).gameObject;

            // The rank pill under the right column: the gold trophy, the rank and a brown chevron; it opens the Leaderboard
            // from a clear touch box at least size.touch_min tall around it.
            Image rankTouch = UiFactory.CreateImage("RankTouch", root, null, Color.clear, raycast: true);
            UiKit.TapTarget(rankTouch, () => features?.OnLeaderboard?.Invoke());
            screen._rankTouch = rankTouch.rectTransform;
            screen._rankRow = UiKit.ParchmentPill("Rank", rankTouch.transform);
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
                float gap = b.Height * 0.14f;
                float size = Mathf.Min(UiKit.Units(T.Body.Size), b.Height * 0.46f);
                float textWidth = Fit(rank, size, b.Width - icon - chevronSize - (gap * 2f) - (b.Height * 0.5f));
                float start = b.CenterX - ((icon + gap + textWidth + gap + chevronSize) / 2f);
                BoxLayout.Place(trophy.rectTransform, Box.FromCenter(start + (icon / 2f), b.CenterY, icon, icon));
                KitText.Place(rank, T.Body, start + icon + gap + (textWidth / 2f), b.CenterY, size, textWidth + 1f);
                BoxLayout.Place(chevron.rectTransform, Box.FromCenter(start + icon + gap + textWidth + gap + (chevronSize / 2f), b.CenterY, chevronSize, chevronSize));
            });

            // "Level N" on the wooden plaque (spec 005 §4.5, §6.4).
            screen._level = UiKit.WoodSign("Level", root, Loc.F("common.level", 1), T.LevelHome);

            // Play: the big primary button with its ▶, leaves and flowers, breathing while it waits (spec 003 FR-010,
            // FR-011a, FR-019), in its wooden rim (spec 005 §3.3).
            screen._playButton = UiKit.PrimaryButton("Play", root, Loc.T("common.play"), onPlay, T.ButtonLarge, decorate: true, playArrow: true, breathe: true);
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

            // The top corners last: Settings at the left, the Petals pill at the right.
            screen._settings = (RectTransform)UiKit.RoundIconButton("Settings", root, "ui.settings", onSettings).transform;
            screen._petals = UiKit.PetalsPill("Petals", root, onStore);
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

        /// <summary>What a system's Home demo points at (roadmap L10–L100), or null while it is not on screen.</summary>
        public RectTransform? DemoTarget(string unlockId) => unlockId switch
        {
            "system.leaderboard" => _rankTouch.gameObject.activeSelf ? _rankRow.rectTransform : null,
            "system.store" => Visible(_petals.Plus),
            "system.wardrobe" => Visible(_wardrobe),
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
        /// locked features collapse, so each side column packs the buttons it shows from its top).
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
            UiKit.PlaceBox(_logo, r.Logo, screen);

            if (!look.Hero)
            {
                // Frame 2: the four heroes around the lotus fountain on the stone, the guest at the front (or the group over
                // the owner's Home picture).
                _stage.Place(r.Diorama, screen, BackdropScene.Home, guest: true);
            }
            else
            {
                // Frame 3: the player's hero on the stone pedestal (until the owner's garden brings its own ground), the
                // guest beside it; the pedestal's foot goes behind the plaque as the reference's well does.
                (Box pedestal, Box body, Box guest) = HomeStage.HeroOnPedestal(new Box(r.Diorama.Left, r.Diorama.Top, r.Diorama.Right, r.Plaque.CenterY));
                _pedestal.gameObject.SetActive(OwnerArt.Background(OwnerPictures.Home) == null);
                UiKit.PlaceBox(_pedestal, pedestal, screen);
                UiKit.PlaceBox(_heroFigure.Rect, body, screen);
                UiKit.PlaceBox(_guestLater.rectTransform, guest, screen);
            }

            // The side columns, packed from the top: Wardrobe, Collection, the avatar; the Daily Challenge, the Store.
            var left = new List<GameObject>();
            if (look.Wardrobe)
            {
                left.Add(_wardrobe);
            }

            if (look.Collection)
            {
                left.Add(_collection);
            }

            if (look.Wardrobe)
            {
                left.Add(_profile);
            }

            var right = new List<GameObject>();
            if (look.DailyChallenge)
            {
                right.Add(_daily);
            }

            if (look.Store)
            {
                right.Add(_store);
            }

            for (int i = 0; i < left.Count; i++)
            {
                UiKit.PlaceBox((RectTransform)left[i].transform, r.SideButton(false, i), screen);
            }

            for (int i = 0; i < right.Count; i++)
            {
                UiKit.PlaceBox((RectTransform)right[i].transform, r.SideButton(true, i), screen);
            }

            // The rank pill under the right column's last button (at the column's top when it is empty).
            float touch = DesignTokens.Size.TouchMin * u;
            Box Touch(Box b) => Box.FromCenter(b.CenterX, b.CenterY, Mathf.Max(b.Width, touch), Mathf.Max(b.Height, touch));
            float rankTop = right.Count > 0 ? r.SideButton(true, right.Count - 1).Bottom + (r.W * ReferenceHomeRegions.SideGapShare) : r.Daily.Top;
            var rank = new Box(r.Rank.Left, rankTop, r.Rank.Right, rankTop + r.Rank.Height);
            UiKit.PlaceBox(_rankTouch, Touch(rank), screen);
            UiKit.PlaceBox(_rankRow.rectTransform, rank, Touch(rank));

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
        }

        public void Show(HomeModel model)
        {
            _model = model;
            _level.Text = Loc.F("common.level", NumberText.Group(model.CurrentLevel));
            Layout(model);
            _playLabel.text = !model.LevelAvailable ? Loc.T("home.more_levels_soon") : Loc.T("common.play");
            _playButton.interactable = model.LevelAvailable;
            _petals.Show(model.Petals, model.StoreUnlocked);
            _store.SetActive(model.StoreUnlocked);
            _rankTouch.gameObject.SetActive(model.LeaderboardUnlocked);
            _rank.text = model.RankText ?? Loc.T("home.rank_unknown");
            _daily.SetActive(model.DailyChallengeAvailable);
            _dailyDone.SetActive(model.DailyChallengeDone);
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
    }
}
