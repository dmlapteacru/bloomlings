using System;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The gameplay screen of the design board's frames 7–9 (spec 002 FR-009, FR-010; spec 001 FR-068), top to bottom:
    /// <list type="bullet">
    /// <item><description>the top bar: round Pause, the "LEVEL N" pill and the dark 2× pill;</description></item>
    /// <item><description>the HARD or SUPER HARD badge under the pill;</description></item>
    /// <item><description>the board on a soft light panel;</description></item>
    /// <item><description>the Waiting Slots on a soft band;</description></item>
    /// <item><description>the Source Tray;</description></item>
    /// <item><description>the booster bar at the bottom, hidden before the first booster unlocks.</description></item>
    /// </list>
    /// Everything sits over the level band's garden backdrop. The regions come from the shared
    /// <see cref="ScreenLayout.Gameplay"/>, so the playtest and this client lay out alike. There is no goals panel.
    /// </summary>
    public sealed class GameplayHud : MonoBehaviour
    {
        private RectTransform _root = null!;
        private BackdropView _backdrop = null!;
        private RectTransform _topBar = null!;
        private RectTransform _pause = null!;
        private RectTransform _speed = null!;
        private RectTransform _levelPill = null!;
        private Image _levelFace = null!;
        private TextMeshProUGUI _level = null!;
        private TextMeshProUGUI _speedLabel = null!;
        private Image _badge = null!;
        private TextMeshProUGUI _badgeLabel = null!;
        private Image _boardPanel = null!;
        private Image _slotBand = null!;
        private TextMeshProUGUI _toast = null!;
        private Image _toastPill = null!;
        private float _toastUntil;

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

            hud._boardPanel = UiKit.Rounded("BoardPanel", root, new Color(UiTheme.Panel.r, UiTheme.Panel.g, UiTheme.Panel.b, 0.72f), 48f);
            hud._slotBand = UiKit.Rounded("SlotBand", root, new Color(UiTheme.Panel.r, UiTheme.Panel.g, UiTheme.Panel.b, 0.55f), 36f);

            hud._topBar = UiFactory.CreateRect("TopBar", root);
            hud._pause = (RectTransform)UiKit.RoundIconButton("Pause", hud._topBar, "ui.pause", onPause).transform;
            hud._level = UiKit.LevelPill("Level", hud._topBar, out hud._levelFace);
            hud._level.text = Loc.F("common.level", 1);
            hud._levelPill = (RectTransform)hud._levelFace.transform.parent;
            Button speed = UiKit.DarkPill("Speed", hud._topBar, "1×", () =>
            {
                hud.DoubleSpeed = !hud.DoubleSpeed;
                hud._speedLabel.text = hud.DoubleSpeed ? "2×" : "1×";
                onSpeedChanged(hud.DoubleSpeed);
            });
            hud._speed = (RectTransform)speed.transform;
            hud._speedLabel = speed.GetComponentInChildren<TextMeshProUGUI>();

            hud._badgeLabel = UiKit.Badge("Badge", root, string.Empty, UiTheme.Of(DesignTokens.Colors.BadgeHard), out hud._badge);
            hud._badge.gameObject.SetActive(false);

            hud.BoardArea = UiFactory.CreateRect("BoardArea", root);
            hud.SlotArea = UiFactory.CreateRect("SlotArea", root);
            hud.TrayArea = UiFactory.CreateRect("TrayArea", root);
            hud.BoosterArea = UiFactory.CreateRect("BoosterArea", root);

            hud._toastPill = UiKit.Pill("Toast", root, UiTheme.Panel);
            hud._toast = UiKit.Label("Text", hud._toastPill.transform, string.Empty, DesignTokens.Type.Body, UiTheme.Text);
            UiFactory.Place(hud._toast.rectTransform, 0.05f, 0.1f, 0.95f, 0.9f);
            hud._toastPill.gameObject.SetActive(false);
            hud.Layout(hasBadge: false, hasBoosters: true);
            return hud;
        }

        /// <summary>
        /// Places every region for the coming level (data-model rules 1–3). A Hard or Super Hard badge takes a line
        /// under the pill, and the booster bar is left out before any booster unlocks. Call it before the board, slots
        /// and tray are built.
        /// </summary>
        public void Layout(bool hasBadge, bool hasBoosters)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            GameplayRegions r = ScreenLayout.Gameplay(w, h, insets, hasBadge, hasBoosters);
            var screen = new Box(0f, 0f, w, h);
            UiKit.PlaceBox(_topBar, r.TopBar, screen);
            float bar = r.TopBar.Height;
            UiKit.PlaceBox(_pause, new Box(r.TopBar.Left, r.TopBar.Top, r.TopBar.Left + bar, r.TopBar.Bottom), r.TopBar);
            UiKit.PlaceBox(_levelPill, Box.FromCenter(r.TopBar.CenterX, r.TopBar.CenterY, Mathf.Min(420f * DesignTokens.ScaleFor(w, h), r.TopBar.Width - (bar * 3.4f)), bar * 0.82f), r.TopBar);
            UiKit.PlaceBox(_speed, new Box(r.TopBar.Right - (bar * 1.3f), r.TopBar.CenterY - (bar * 0.36f), r.TopBar.Right, r.TopBar.CenterY + (bar * 0.36f)), r.TopBar);
            UiKit.PlaceBox(_badge.rectTransform, r.Badge.IsEmpty ? r.Badge : r.Badge.Offset(0f, -10f * DesignTokens.ScaleFor(w, h)), screen);
            UiKit.PlaceBox(BoardArea, r.Board, screen);
            UiKit.PlaceBox(_boardPanel.rectTransform, r.Board.Inset(-12f), screen);
            UiKit.PlaceBox(SlotArea, r.Slots, screen);
            UiKit.PlaceBox(_slotBand.rectTransform, r.Slots.Inset(-10f, -8f), screen);
            UiKit.PlaceBox(TrayArea, r.Tray, screen);
            UiKit.PlaceBox(BoosterArea, r.Boosters, screen);
            BoosterArea.gameObject.SetActive(hasBoosters);
            float toastHeight = 96f * DesignTokens.ScaleFor(w, h);
            UiKit.PlaceBox(_toastPill.rectTransform, new Box(r.Board.Left + (r.Board.Width * 0.08f), r.Board.Bottom - toastHeight - 16f, r.Board.Right - (r.Board.Width * 0.08f), r.Board.Bottom - 16f), screen);
        }

        /// <summary>The HARD or SUPER HARD badge under the level pill (FR-010); Normal levels show none.</summary>
        public void SetDifficulty(DifficultyClass difficulty, bool labelUnlocked)
        {
            bool show = labelUnlocked && difficulty != DifficultyClass.Normal;
            _badge.gameObject.SetActive(show);
            bool super = difficulty == DifficultyClass.SuperHard;
            _levelFace.color = UiTheme.Of(show && super ? DesignTokens.Colors.PillLevelSuperHard : DesignTokens.Colors.PillLevel);
            if (show)
            {
                _badge.color = UiTheme.Of(super ? DesignTokens.Colors.BadgeSuperHard : DesignTokens.Colors.BadgeHard);
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

        /// <summary>The garden backdrop of the level band's theme (FR-066, spec 002 FR-008); visual only.</summary>
        public void SetTheme(BackgroundTheme? theme) => _backdrop.Show(theme);

        /// <summary>A short message for a refused tap, shown at once (SC-008).</summary>
        public void Toast(string message)
        {
            _toast.text = message;
            _toastPill.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + 1.2f;
        }

        private void Update()
        {
            if (_toastPill.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
            {
                _toastPill.gameObject.SetActive(false);
            }
        }
    }
}
