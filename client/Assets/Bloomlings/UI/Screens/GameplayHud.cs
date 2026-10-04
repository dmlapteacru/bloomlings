using System;
using System.Collections.Generic;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Slots;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The gameplay screen of the design board's frames 7–9 (spec 002 FR-009, FR-010; spec 001 FR-068) in the reference
    /// layout (spec 005 FR-020, FR-021, contracts/look.md §6.1; the playtest's <c>LevelScreen</c>), top to bottom:
    /// <list type="bullet">
    /// <item><description>the top bar: the cream Pause squircle (0.13 W), the level on a wide wooden sign with ivy at both
    /// ends (0.42 W × 0.115 W) and the cream speed pill (0.2 W × 0.115 W);</description></item>
    /// <item><description>the HARD or SUPER HARD badge under the sign (a Super Hard level also tints the sign's
    /// letters);</description></item>
    /// <item><description>the board on the lawn inside its stone border, at most 0.86 W wide, with the thin entry strip of
    /// lawn under it (<see cref="FitBoard"/>; the Garden Entries have no arch);</description></item>
    /// <item><description>one parchment tray from the entry strip to the bottom of the screen, its rows on bands parted by
    /// grooves: the Waiting Slots, the four booster boxes (left out before the first booster unlocks) and the Source
    /// stacks, one column each, their pods one after another and never on each other (the owner's gameplay rule,
    /// 2026-10-03): three rows, four from 19.5:9.</description></item>
    /// </list>
    /// Everything sits over the level band's lawn. The regions come from the shared
    /// <see cref="ScreenLayout.ReferenceGameplay"/>, so the playtest and this client lay out alike; the views take their
    /// places from <see cref="SlotCells"/>, <see cref="PodGrid"/>, <see cref="BoosterCells"/> and <see cref="FitBoard"/>.
    /// There is no goals panel.
    /// </summary>
    public sealed class GameplayHud : MonoBehaviour
    {
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
        private TrayPanelView _tray = null!;
        private TextMeshProUGUI _toast = null!;
        private Image _toastPill = null!;
        private ReferenceGameplayRegions? _regions;
        private int _stackCount = 4;
        private bool _hasBadge;
        private bool _hasBoosters = true;
        private Box _boardBox;
        private float _toastUntil;

        /// <summary>
        /// The top bar (Pause, the level sign and the speed pill). It stays live under the win card: the card's shade lets
        /// taps through over it (FR-002).
        /// </summary>
        public RectTransform TopBar => _topBar;

        /// <summary>The lawn from the board's top to the entry strip's bottom across the safe width: the board's room.</summary>
        public RectTransform BoardArea { get; private set; } = null!;

        /// <summary>The Waiting Slots' row on the tray.</summary>
        public RectTransform SlotArea { get; private set; } = null!;

        /// <summary>The pod grid at the bottom of the tray: one column per Source stack (<see cref="PodGrid"/>).</summary>
        public RectTransform TrayArea { get; private set; } = null!;

        /// <summary>The row of the four booster boxes (frame 14).</summary>
        public RectTransform BoosterArea { get; private set; } = null!;

        /// <summary>The regions the screen was last laid out with (screen pixels, y down).</summary>
        public ReferenceGameplayRegions? Regions => _regions;

        public bool DoubleSpeed { get; private set; }

        private bool _autoSpeed;

        public static GameplayHud Create(RectTransform root, Action onPause, Action<bool> onSpeedChanged)
        {
            var hud = root.gameObject.AddComponent<GameplayHud>();
            hud._backdrop = BackdropView.Create(root, BackdropScene.Gameplay);

            hud._topBar = UiFactory.CreateRect("TopBar", root);
            hud._pause = (RectTransform)UiKit.RoundIconButton("Pause", hud._topBar, "ui.pause", onPause, squircle: true).transform;
            hud._level = UiKit.LevelPill("Level", hud._topBar, out hud._levelFace);
            hud._level.text = Loc.F("common.level", 1);
            hud._levelPill = (RectTransform)hud._levelFace.transform;
            Button speed = UiKit.SpeedPill("Speed", hud._topBar, "1×", () =>
            {
                hud.DoubleSpeed = !hud.DoubleSpeed;
                hud.ShowSpeed();
                onSpeedChanged(hud.DoubleSpeed);
            });
            hud._speed = (RectTransform)speed.transform;
            hud._speedLabel = speed.GetComponentInChildren<TextMeshProUGUI>();

            // The badge sits in a holder over the whole screen that takes no taps.
            RectTransform badgeHolder = UiFactory.Stretch(UiFactory.CreateRect("BadgeHolder", root));
            hud._badgeLabel = UiKit.Badge("Badge", badgeHolder, string.Empty, GardenLook.Red, out hud._badge);
            hud._badge.gameObject.SetActive(false);
            badgeHolder.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            // The badge's letters at type.badge, centered on its face (at most 86% of it wide), as the playtest's Kit.Badge.
            TextMeshProUGUI badgeLabel = hud._badgeLabel;
            BoxLayout.On(hud._badge.Content).Watch(badgeLabel).Then(f => KitText.Place(badgeLabel, DesignTokens.Type.Badge, f.CenterX, f.CenterY, UiKit.Units(DesignTokens.Type.Badge.Size), f.Width * 0.86f));

            hud.BoardArea = UiFactory.CreateRect("BoardArea", root);

            // The tray (spec 005 FR-020, §6.1): one parchment tray across the screen behind the slots, the booster boxes and
            // the pod columns; it takes no taps.
            hud._tray = UiKit.TrayPanel("Tray", root);
            hud._tray.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            hud.SlotArea = UiFactory.CreateRect("SlotArea", root);
            hud.BoosterArea = UiFactory.CreateRect("BoosterArea", root);
            hud.TrayArea = UiFactory.CreateRect("TrayArea", root);

            // A short message over the board's lower edge (a refused tap, a hint): a parchment pill with brown text.
            hud._toastPill = UiKit.Paper("Toast", root, b => b.Height / 2f, DesignTokens.Garden.OutlineWidth, 5f, raycast: false);
            hud._toast = UiKit.KitLabel("Text", hud._toastPill.transform, string.Empty, DesignTokens.Type.Body, TextLook.Plain(C.InkBrown));
            BoxLayout.On(hud._toastPill.rectTransform).Watch(hud._toast).Then(b => KitText.Place(hud._toast, DesignTokens.Type.Body, b.CenterX, b.CenterY, UiKit.Units(DesignTokens.Type.Body.Size), b.Width - UiKit.Units(40f)));
            hud._toastPill.gameObject.SetActive(false);
            hud.Layout(hasBadge: false, hasBoosters: true);
            return hud;
        }

        /// <summary>
        /// Places every region with the last level's stacks (four stacks before the first level); see
        /// <see cref="Layout(bool, bool, int)"/>.
        /// </summary>
        public void Layout(bool hasBadge, bool hasBoosters) => Layout(hasBadge, hasBoosters, _stackCount);

        /// <summary>
        /// Places every region for the coming level (data-model rules 1–3; spec 005 §6.1): a Hard or Super Hard badge takes
        /// a line under the sign; the booster row and its band of parchment are left out before any booster unlocks; the pod
        /// row holds one column per Source stack (<paramref name="stackCount"/>). The Garden Entries take no room (no arch).
        /// Call it before the board, slots and tray are built.
        /// </summary>
        public void Layout(bool hasBadge, bool hasBoosters, int stackCount)
        {
            _hasBadge = hasBadge;
            _hasBoosters = hasBoosters;
            _stackCount = Mathf.Max(0, stackCount);
            ReferenceGameplayRegions r = Compute(WaitingSlots.DefaultCount);
            _regions = r;
            (float w, float h, Insets _) = UiKit.ScreenFrame();
            var screen = new Box(0f, 0f, w, h);

            // The top bar (§6.1): Pause, the sign and the speed pill at the reference's sizes.
            UiKit.PlaceBox(_topBar, r.TopBar, screen);
            UiKit.PlaceBox(_pause, r.Pause, r.TopBar);
            UiKit.PlaceBox(_levelPill, r.Sign, r.TopBar);
            UiKit.PlaceBox(_speed, r.Speed, r.TopBar);

            // HARD or SUPER HARD under the sign; the board's lawn and its entry strip.
            UiKit.PlaceBox((RectTransform)_badge.transform, r.Badge, screen);
            UiKit.PlaceBox(BoardArea, r.BoardArea, screen);
            _boardBox = r.Board;

            // The tray from the entry strip to past the bottom of the screen, with a groove at each separator's middle.
            var frame = new Box(r.Tray.Left, r.Tray.Top, r.Tray.Right, r.Tray.Bottom + r.TrayRadius);
            UiKit.PlaceBox((RectTransform)_tray.transform, frame, screen);
            var cuts = new List<float>();
            foreach (Box line in new[] { r.SeparatorTop, r.SeparatorBottom })
            {
                if (!line.IsEmpty)
                {
                    cuts.Add(line.CenterY);
                }
            }

            _tray.Set(frame, r.W, r.TrayRadius, cuts);
            UiKit.PlaceBox(SlotArea, r.SlotRow, screen);
            UiKit.PlaceBox(TrayArea, r.PodRow, screen);
            UiKit.PlaceBox(BoosterArea, hasBoosters ? r.BoosterRow : r.SlotRow, screen);
            BoosterArea.gameObject.SetActive(hasBoosters);
            LayToast();
        }

        /// <summary>The reference regions for this level's badge, boosters and stacks with <paramref name="slotCount"/> Waiting Slots.</summary>
        private ReferenceGameplayRegions Compute(int slotCount)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            return ScreenLayout.ReferenceGameplay(w, h, insets, _stackCount, slotCount, _hasBoosters, _hasBadge);
        }

        /// <summary>
        /// The plates of <paramref name="count"/> Waiting Slots (the sixth one too once Extra Slot opened it) in
        /// <see cref="SlotArea"/>'s top-down canvas units: portrait plates spread evenly across the row (§6.1).
        /// </summary>
        public IReadOnlyList<Box> SlotCells(int count)
        {
            ReferenceGameplayRegions r = Compute(Mathf.Max(1, count));
            return Local(r.Slots, r.SlotRow);
        }

        /// <summary>
        /// The pod grid of <paramref name="stackCount"/> Source stacks (§6.1, <see cref="ReferenceGameplayRegions.Pod"/>) in
        /// <see cref="TrayArea"/>'s top-down canvas units (<see cref="InPodRow(ReferenceGameplayRegions)"/>).
        /// </summary>
        public ReferenceGameplayRegions PodGrid(int stackCount)
        {
            if (_regions == null || stackCount != _stackCount)
            {
                // A level laid out without its stacks: the columns for this many, in the same pod row.
                _stackCount = Mathf.Max(0, stackCount);
                _regions = Compute(WaitingSlots.DefaultCount);
            }

            return InPodRow(_regions);
        }

        /// <summary>
        /// The pod grid of <paramref name="r"/> in the top-down canvas units of a rect placed at its pod row: the columns,
        /// the pod row, the pods' heights, their gap and W, so <see cref="ReferenceGameplayRegions.Pod"/>,
        /// <see cref="ReferenceGameplayRegions.Chip"/> and <see cref="ReferenceGameplayRegions.Shows"/> give the pods' boxes
        /// there (the other regions stay in screen pixels).
        /// </summary>
        public static ReferenceGameplayRegions InPodRow(ReferenceGameplayRegions r) => InPodRow(r, 1f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit));

        /// <summary>
        /// <see cref="InPodRow(ReferenceGameplayRegions)"/> at <paramref name="unitsPerPixel"/> canvas units per screen
        /// pixel (engine-free).
        /// </summary>
        public static ReferenceGameplayRegions InPodRow(ReferenceGameplayRegions r, float unitsPerPixel)
        {
            float k = unitsPerPixel;
            Box row = r.PodRow;
            Box InRow(Box b) => new Box((b.Left - row.Left) * k, (b.Top - row.Top) * k, (b.Right - row.Left) * k, (b.Bottom - row.Top) * k);
            var columns = new Box[r.Columns.Count];
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i] = InRow(r.Columns[i]);
            }

            return r with
            {
                W = r.W * k,
                PodRow = InRow(row),
                Columns = columns,
                FrontHeight = r.FrontHeight * k,
                QueueHeight = r.QueueHeight * k,
                PodGap = r.PodGap * k,
            };
        }

        /// <summary>The four booster boxes in <see cref="BoosterArea"/>'s top-down canvas units (§6.1); none without boosters.</summary>
        public IReadOnlyList<Box> BoosterCells()
        {
            if (_regions == null || !_hasBoosters)
            {
                return Array.Empty<Box>();
            }

            return Local(_regions.Boosters, _regions.BoosterRow);
        }

        /// <summary>
        /// The board's layout in <see cref="BoardArea"/>'s own top-down canvas units (<paramref name="local"/> is its box):
        /// <see cref="ReferenceGameplayRegions.FitBoard"/>, the stone border at most 0.86 W wide (§6.1).
        /// </summary>
        public BoardLayout FitBoard(Box local, int width, int height)
        {
            if (_regions == null || _regions.BoardArea.Width <= 0f)
            {
                return BoardLayout.Fit(local, width, height);
            }

            // FitBoard reads only the board area (the safe width from the board's top to the entry strip's bottom) and W:
            // the same regions in the board area's own units.
            ReferenceGameplayRegions r = _regions;
            float scale = local.Width / r.BoardArea.Width;
            ReferenceGameplayRegions inArea = r with
            {
                Safe = local,
                W = r.W * scale,
                Board = new Box(local.Left, local.Top, local.Right, local.Top),
                EntryStrip = new Box(local.Left, local.Bottom, local.Right, local.Bottom),
            };
            return inArea.FitBoard(width, height);
        }

        /// <summary>Screen boxes in the top-down canvas units of a rect placed at <paramref name="area"/>.</summary>
        private static IReadOnlyList<Box> Local(IReadOnlyList<Box> boxes, Box area)
        {
            var local = new Box[boxes.Count];
            for (int i = 0; i < boxes.Count; i++)
            {
                local[i] = UiKit.ToLocal(boxes[i], area);
            }

            return local;
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
            ShowSpeed();
        }

        /// <summary>
        /// Whether the animation plays at 2× on its own (no pod can be tapped, GameplayController.RefreshSpeed): the pill
        /// then shows 2× whatever the toggle; the toggle itself stays as the player set it.
        /// </summary>
        public void ShowAutoSpeed(bool on)
        {
            _autoSpeed = on;
            ShowSpeed();
        }

        private void ShowSpeed() => _speedLabel.text = DoubleSpeed || _autoSpeed ? "2×" : "1×";

        public void SetLevel(int levelNumber) => _level.text = Loc.F("common.level", NumberText.Group(levelNumber));

        /// <summary>A title in place of "Level N" (the Daily Challenge).</summary>
        public void SetTitle(string title) => _level.text = title;

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
            if (_toastPill.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
            {
                _toastPill.gameObject.SetActive(false);
            }
        }
    }
}
