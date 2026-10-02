using System;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The gameplay screen of the design board's frames 7–9 (spec 002 FR-009, FR-010; spec 001 FR-068) in the reference
    /// look (spec 005 contracts/look.md §4.1, the playtest's <c>LevelScreen</c>), top to bottom:
    /// <list type="bullet">
    /// <item><description>the top bar: the cream Pause squircle, the level on a wide wooden sign with ivy at both ends,
    /// and the cream speed pill, as tall as Pause and half as wide again;</description></item>
    /// <item><description>the HARD or SUPER HARD badge hanging from the sign (a Super Hard level also tints the sign's
    /// letters);</description></item>
    /// <item><description>the board on the lawn, inside its stone border;</description></item>
    /// <item><description>the tray's parchment from just below the board to past the bottom of the screen: one band for
    /// the Waiting Slots, one for the Source Tray and one for the booster bar, parted by grooves;</description></item>
    /// <item><description>the booster bar at the bottom, hidden before the first booster unlocks.</description></item>
    /// </list>
    /// Everything sits over the level band's lawn. The regions come from the shared <see cref="ScreenLayout.Gameplay"/>,
    /// so the playtest and this client lay out alike. There is no goals panel.
    /// </summary>
    public sealed class GameplayHud : MonoBehaviour
    {
        /// <summary>The level sign's width in top bar heights (the reference's plank is about 2.9 Pause buttons wide).</summary>
        public const float SignWidth = 2.9f;

        /// <summary>The level sign's height in top bar heights.</summary>
        public const float SignHeight = 0.9f;

        /// <summary>The speed pill's width in top bar heights.</summary>
        public const float SpeedWidth = 1.5f;

        private RectTransform _root = null!;
        private BackdropView _backdrop = null!;
        private RectTransform _topBar = null!;
        private RectTransform _pause = null!;
        private RectTransform _speed = null!;
        private RectTransform _levelPill = null!;
        private GardenButton _levelFace = null!;
        private TextMeshProUGUI _level = null!;
        private TextMeshProUGUI _speedLabel = null!;
        private GardenButton _badge = null!;
        private TextMeshProUGUI _badgeLabel = null!;
        private RectTransform _trayFrame = null!;
        private RectTransform _slotBand = null!;
        private RectTransform _trayBand = null!;
        private RectTransform _boosterBand = null!;
        private TextMeshProUGUI _toast = null!;
        private Image _toastPill = null!;
        private Box _boardBox;
        private float _toastUntil;
        private CanvasGroup _topFade = null!;
        private CanvasGroup _badgeFade = null!;
        private float _topBarFadingSince = -1f;

        /// <summary>How long the top bar takes to fade out once the win card shows.</summary>
        public const float TopBarFadeSeconds = 0.3f;

        public RectTransform BoardArea { get; private set; } = null!;

        public RectTransform SlotArea { get; private set; } = null!;

        public RectTransform TrayArea { get; private set; } = null!;

        /// <summary>The booster bar at the bottom (frame 14).</summary>
        public RectTransform BoosterArea { get; private set; } = null!;

        public bool DoubleSpeed { get; private set; }

        public static GameplayHud Create(RectTransform root, Action onPause, Action<bool> onSpeedChanged)
        {
            var hud = root.gameObject.AddComponent<GameplayHud>();
            hud._root = root;
            hud._backdrop = BackdropView.Create(root, BackdropScene.Gameplay);

            hud._topBar = UiFactory.CreateRect("TopBar", root);
            hud._pause = (RectTransform)UiKit.RoundIconButton("Pause", hud._topBar, "ui.pause", onPause, squircle: true).transform;
            hud._level = UiKit.LevelPill("Level", hud._topBar, out hud._levelFace);
            hud._level.text = Loc.F("common.level", 1);
            hud._levelPill = (RectTransform)hud._levelFace.transform;
            Button speed = UiKit.SpeedPill("Speed", hud._topBar, "1×", () =>
            {
                hud.DoubleSpeed = !hud.DoubleSpeed;
                hud._speedLabel.text = hud.DoubleSpeed ? "2×" : "1×";
                onSpeedChanged(hud.DoubleSpeed);
            });
            hud._speed = (RectTransform)speed.transform;
            hud._speedLabel = speed.GetComponentInChildren<TextMeshProUGUI>();

            // The badge sits in a holder over the whole screen, so it fades with the top bar.
            RectTransform badgeHolder = UiFactory.Stretch(UiFactory.CreateRect("BadgeHolder", root));
            hud._badgeLabel = UiKit.Badge("Badge", badgeHolder, string.Empty, GardenLook.Red, out hud._badge);
            hud._badge.gameObject.SetActive(false);
            hud._topFade = hud._topBar.gameObject.AddComponent<CanvasGroup>();
            hud._badgeFade = badgeHolder.gameObject.AddComponent<CanvasGroup>();
            hud._badgeFade.blocksRaycasts = false;

            // The badge's letters at type.badge, centered on its face (at most 86% of it wide), as the playtest's Kit.Badge.
            TextMeshProUGUI badgeLabel = hud._badgeLabel;
            BoxLayout.On(hud._badge.Content).Watch(badgeLabel).Then(f => KitText.Place(badgeLabel, DesignTokens.Type.Badge, f.CenterX, f.CenterY, UiKit.Units(DesignTokens.Type.Badge.Size), f.Width * 0.86f));

            hud.BoardArea = UiFactory.CreateRect("BoardArea", root);

            // The tray's parchment (spec 005 FR-012, §4.1): a frame with one band per region, behind the slots, the tray and
            // the booster bar.
            hud._trayFrame = UiKit.TrayFrame("TrayFrame", root);
            hud._slotBand = UiKit.TrayBand("SlotBand", root, 34f);
            hud._trayBand = UiKit.TrayBand("TrayBand", root, 22f);
            hud._boosterBand = UiKit.TrayBand("BoosterBand", root, 22f);

            hud.SlotArea = UiFactory.CreateRect("SlotArea", root);
            hud.TrayArea = UiFactory.CreateRect("TrayArea", root);
            hud.BoosterArea = UiFactory.CreateRect("BoosterArea", root);

            // A short message over the board's lower edge (a refused tap, a hint): a parchment pill with brown text.
            hud._toastPill = UiKit.Paper("Toast", root, b => b.Height / 2f, DesignTokens.Garden.OutlineWidth, 5f, raycast: false);
            hud._toast = UiKit.KitLabel("Text", hud._toastPill.transform, string.Empty, DesignTokens.Type.Body, TextLook.Plain(C.InkBrown));
            BoxLayout.On(hud._toastPill.rectTransform).Watch(hud._toast).Then(b => KitText.Place(hud._toast, DesignTokens.Type.Body, b.CenterX, b.CenterY, UiKit.Units(DesignTokens.Type.Body.Size), b.Width - UiKit.Units(40f)));
            hud._toastPill.gameObject.SetActive(false);
            hud.Layout(hasBadge: false, hasBoosters: true);
            return hud;
        }

        /// <summary>
        /// Places every region for the coming level (data-model rules 1–3). A Hard or Super Hard badge takes a line
        /// under the sign, and the booster bar (and its band of parchment) is left out before any booster unlocks. Call it
        /// before the board, slots and tray are built.
        /// </summary>
        public void Layout(bool hasBadge, bool hasBoosters)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            GameplayRegions r = ScreenLayout.Gameplay(w, h, insets, hasBadge, hasBoosters);
            float u = DesignTokens.ScaleFor(w, h);
            var screen = new Box(0f, 0f, w, h);

            // The top bar (frame 7, §3.3, §4.1): Pause, the sign and the speed pill, sized like the reference's.
            UiKit.PlaceBox(_topBar, r.TopBar, screen);
            float bar = r.TopBar.Height;
            UiKit.PlaceBox(_pause, new Box(r.TopBar.Left, r.TopBar.Top, r.TopBar.Left + bar, r.TopBar.Bottom), r.TopBar);
            UiKit.PlaceBox(_levelPill, Box.FromCenter(r.TopBar.CenterX, r.TopBar.CenterY, Mathf.Min(bar * SignWidth, r.TopBar.Width - (bar * 4f)), bar * SignHeight), r.TopBar);
            UiKit.PlaceBox(_speed, new Box(r.TopBar.Right - (bar * SpeedWidth), r.TopBar.CenterY - (bar / 2f), r.TopBar.Right, r.TopBar.CenterY + (bar / 2f)), r.TopBar);

            // HARD or SUPER HARD hangs from the sign's lower edge.
            UiKit.PlaceBox((RectTransform)_badge.transform, r.Badge.IsEmpty ? r.Badge : r.Badge.Inset(0f, 2f * u).Offset(0f, -10f * u), screen);
            UiKit.PlaceBox(BoardArea, r.Board, screen);
            _boardBox = r.Board;
            LayTray(r, hasBoosters, u, h, screen);
            UiKit.PlaceBox(SlotArea, r.Slots, screen);
            UiKit.PlaceBox(TrayArea, r.Tray, screen);
            UiKit.PlaceBox(BoosterArea, r.Boosters, screen);
            BoosterArea.gameObject.SetActive(hasBoosters);
            LayToast();
        }

        /// <summary>
        /// The tray's parchment (the playtest's <c>LevelScreen.TrayBoard</c>): the frame from 14 units above the slots to
        /// past the bottom of the screen, 8 units in from the safe area's sides; inside it, 7 units in, the slot band down to
        /// 30% of the gap between slots and tray, then the tray band and the booster band, parted by 8-unit grooves.
        /// </summary>
        private void LayTray(GameplayRegions r, bool hasBoosters, float u, float screenHeight, Box screen)
        {
            float margin = 7f * u;
            float groove = 4f * u;
            var board = new Box(r.Safe.Left + (8f * u), r.Slots.Top - (14f * u), r.Safe.Right - (8f * u), screenHeight + (80f * u));
            UiKit.PlaceBox(_trayFrame, board, screen);
            float left = board.Left + margin;
            float right = board.Right - margin;
            float seam = r.Slots.Bottom + ((r.Tray.Top - r.Slots.Bottom) * 0.3f);
            UiKit.PlaceBox(_slotBand, new Box(left, board.Top + margin, right, seam - groove), screen);
            _boosterBand.gameObject.SetActive(hasBoosters);
            if (!hasBoosters)
            {
                UiKit.PlaceBox(_trayBand, new Box(left, seam + groove, right, board.Bottom), screen);
                return;
            }

            float seam2 = r.Tray.Bottom + ((r.Boosters.Top - r.Tray.Bottom) * 0.5f);
            UiKit.PlaceBox(_trayBand, new Box(left, seam + groove, right, seam2 - groove), screen);
            UiKit.PlaceBox(_boosterBand, new Box(left, seam2 + groove, right, board.Bottom), screen);
        }

        /// <summary>The toast: 96 units tall, as wide as its text plus 80 units (at most the board), 16 units above the board's bottom.</summary>
        private void LayToast()
        {
            (float w, float h, Insets _) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            float height = 96f * u;
            float textWidth = KitText.Measure(_toast, UiKit.Units(DesignTokens.Type.Body.Size)) * UiKit.PixelsPerUnit;
            float width = Mathf.Min(_boardBox.Width, (textWidth > 0f ? textWidth : _boardBox.Width * 0.6f) + (80f * u));
            Box box = Box.FromCenter(_boardBox.CenterX, _boardBox.Bottom - (height / 2f) - (16f * u), width, height);
            UiKit.PlaceBox(_toastPill.rectTransform, box, new Box(0f, 0f, w, h));
        }

        /// <summary>The HARD or SUPER HARD badge under the level sign (FR-010); Normal levels show none.</summary>
        public void SetDifficulty(DifficultyClass difficulty, bool labelUnlocked)
        {
            bool show = labelUnlocked && difficulty != DifficultyClass.Normal;
            _badge.gameObject.SetActive(show);
            bool super = difficulty == DifficultyClass.SuperHard;

            // A Super Hard level tints the sign's letters badge.super_hard, darkened for the pale wood (§1.3); any other set
            // keeps them ink.brown.
            _levelFace.SetColors(show && super ? GardenLook.Lilac : GardenLook.Blue);
            if (show)
            {
                _badge.SetColors(super ? GardenLook.Purple : GardenLook.Red);
                _badgeLabel.text = super ? Loc.T("difficulty.super_hard") : Loc.T("difficulty.hard");
            }
        }

        /// <summary>Sets the 2× toggle without raising its callback (the saved default, FR-069).</summary>
        public void SetDoubleSpeed(bool on)
        {
            DoubleSpeed = on;
            _speedLabel.text = on ? "2×" : "1×";
        }

        public void SetLevel(int levelNumber) => _level.text = Loc.F("common.level", NumberText.Group(levelNumber));

        /// <summary>A title in place of "Level N" (the Daily Challenge).</summary>
        public void SetTitle(string title) => _level.text = title;

        /// <summary>
        /// Fades the top bar (Pause, the level sign, the speed pill and the badge) out over
        /// <see cref="TopBarFadeSeconds"/> and stops its taps, so the win card's sign, heroes and rays own the top (the
        /// playtest's <c>LevelScreen</c>). <see cref="ShowTopBar"/> brings it back for the next level.
        /// </summary>
        public void FadeOutTopBar()
        {
            _topBarFadingSince = Time.unscaledTime;
            _topFade.interactable = false;
            _topFade.blocksRaycasts = false;
        }

        /// <summary>Shows the top bar again at once, taking taps (a new level or a restart).</summary>
        public void ShowTopBar()
        {
            _topBarFadingSince = -1f;
            _topFade.alpha = 1f;
            _topFade.interactable = true;
            _topFade.blocksRaycasts = true;
            _badgeFade.alpha = 1f;
        }

        /// <summary>The level band's lawn (FR-066, spec 002 FR-008, spec 005 §4.2); visual only.</summary>
        public void SetTheme(BackgroundTheme? theme) => _backdrop.Show(theme);

        /// <summary>A short message for a refused tap, shown at once (SC-008).</summary>
        public void Toast(string message)
        {
            _toast.text = message;
            _toastPill.gameObject.SetActive(true);
            LayToast();
            _toastUntil = Time.unscaledTime + 1.2f;
        }

        private void Update()
        {
            if (_topBarFadingSince >= 0f)
            {
                float alpha = 1f - FadeIn.Ease((Time.unscaledTime - _topBarFadingSince) / TopBarFadeSeconds);
                _topFade.alpha = alpha;
                _badgeFade.alpha = alpha;
            }

            if (_toastPill.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
            {
                _toastPill.gameObject.SetActive(false);
            }
        }
    }
}
