using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
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
    /// One Waiting Slot as the jam sheet shows it (spec 005 §4.3, <c>ui.jam.slots</c>): a pod's variant and its count
    /// (<see cref="SlotPlateState.Working"/>; a null variant is a mystery pod), a free slot
    /// (<see cref="SlotPlateState.Empty"/>) or a locked one (<see cref="SlotPlateState.Locked"/>).
    /// </summary>
    public sealed record JamSlot(SlotPlateState State, VariantId? Variant, int Count);

    /// <summary>
    /// The jam bottom sheet of the design board's frame 10 (spec 002 FR-019; spec 001 FR-027, T051, T121) in the
    /// reference look of spec 005 (contracts/look.md §4.3; the playtest's <c>EndCards.Jam</c>):
    /// <list type="bullet">
    /// <item><description>parchment rising from the bottom, "No more space!" (or "No pod can move!" when Stuck) and its
    /// subtitle, broken after its first sentence into two lines;</description></item>
    /// <item><description>the Waiting Slots' contents in an inset well: each pod's sticker tile with its count, a free
    /// slot as a small dashed plate, a locked one with its padlock;</description></item>
    /// <item><description>one big colored choice per usable recovery (green Extra Slot and Shuffle, blue Return and
    /// Bloom Burst) with its icon and its cost pill (×N charges, or the lotus and the price), in one row of up to three,
    /// a 2 × 2 grid of four, or rows of three;</description></item>
    /// <item><description>the free rescue (a rewarded ad, once per attempt) as one more green choice with the "▶ Free"
    /// pill;</description></item>
    /// <item><description>Restart as a cream button with ⟳ at the size of a card's main button.</description></item>
    /// </list>
    /// The sheet covers only the bottom of the screen, so the board stays visible. It never opens the Store. A short
    /// phone shrinks the well, the choices and the gaps together.
    /// </summary>
    public sealed class JamScreen : MonoBehaviour
    {
        private RectTransform _host = null!;
        private TextMeshProUGUI _probe = null!;
        private SheetView? _sheet;
        private Action _onRestart = () => { };
        private Action<Recovery> _onRecovery = _ => { };

        public bool IsOpen => _host.gameObject.activeSelf;

        public static JamScreen Create(Transform parent, Action onRestart, Action<Recovery> onRecovery)
        {
            // The sheet is built on each Show (its height follows the choices); this host keeps the screen in its place.
            RectTransform host = UiFactory.Stretch(UiFactory.CreateRect("JamScreen", parent));
            var screen = host.gameObject.AddComponent<JamScreen>();
            screen._host = host;
            screen._onRestart = onRestart;
            screen._onRecovery = onRecovery;

            // Measures the subtitle's lines; it never shows.
            screen._probe = UiKit.Label("Probe", host, string.Empty, T.Body, Color.clear);
            host.gameObject.SetActive(false);
            return screen;
        }

        /// <param name="recoveries">Only the recoveries the player can use now: owned, or affordable with Petals (FR-027).</param>
        /// <param name="cost">A recovery's cost pill: ×N charges, or the lotus and its price; null shows none.</param>
        /// <param name="slots">The Waiting Slots' contents, left to right (<see cref="SlotsOf"/>).</param>
        /// <param name="rescue">The rewarded rescue (a free use of that booster, once per attempt), or null when not offered.</param>
        public void Show(bool stuck, IReadOnlyList<Recovery> recoveries, Func<Recovery, Cost?> cost, IReadOnlyList<JamSlot> slots, (Recovery Booster, Action Watch)? rescue = null)
        {
            if (_sheet != null)
            {
                Destroy(_sheet.Root);
                _sheet = null;
            }

            _host.gameObject.SetActive(true);
            Build(stuck, recoveries, cost, slots, rescue);
        }

        public void Hide() => _host.gameObject.SetActive(false);

        /// <summary>The Waiting Slots of <paramref name="view"/> as the sheet shows them: every present slot, left to right.</summary>
        public static IReadOnlyList<JamSlot> SlotsOf(LevelView view)
        {
            var slots = new List<JamSlot>();
            for (int i = 0; i < view.SlotCapacity; i++)
            {
                SlotState state = view.SlotStateOf(i);
                if (state == SlotState.Absent)
                {
                    continue;
                }

                string? pod = state == SlotState.Locked ? null : view.PodInSlot(i);
                if (state == SlotState.Locked)
                {
                    slots.Add(new JamSlot(SlotPlateState.Locked, null, 0));
                }
                else if (pod == null)
                {
                    slots.Add(new JamSlot(SlotPlateState.Empty, null, 0));
                }
                else
                {
                    PodInfo info = view.Pod(pod);
                    slots.Add(new JamSlot(SlotPlateState.Working, info.Variant, info.Remaining));
                }
            }

            return slots;
        }

        public static string Label(Recovery recovery) => recovery switch
        {
            Recovery.ExtraSlot => Loc.T("booster.extra_slot"),
            Recovery.Shuffle => Loc.T("booster.shuffle"),
            Recovery.Return => Loc.T("booster.return"),
            _ => Loc.T("booster.bloom_burst"),
        };

        private static string Id(Recovery recovery) => recovery switch
        {
            Recovery.ExtraSlot => "extra_slot",
            Recovery.Shuffle => "shuffle",
            Recovery.Return => "return",
            _ => "bloom_burst",
        };

        /// <summary>The color of a recovery's choice, as on the reference's jam card: Extra Slot and Shuffle green, Return and Bloom Burst blue.</summary>
        private static ColorSet ChoiceSet(string boosterId) => boosterId == "extra_slot" || boosterId == "shuffle" ? GardenLook.Green : GardenLook.Blue;

        private void Build(bool stuck, IReadOnlyList<Recovery> recoveries, Func<Recovery, Cost?> cost, IReadOnlyList<JamSlot> slots, (Recovery Booster, Action Watch)? rescue)
        {
            var choices = new List<(string Id, ColorSet Set, string Label, Cost? Cost, Action Action)>();
            foreach (Recovery recovery in recoveries)
            {
                Recovery chosen = recovery;
                string id = Id(recovery);
                choices.Add((id, ChoiceSet(id), Label(recovery), cost(recovery), () => _onRecovery(chosen)));
            }

            if (rescue.HasValue)
            {
                // The free rescue: a green choice with the ▶ Free pill (a rewarded ad).
                choices.Add((Id(rescue.Value.Booster), GardenLook.Green, Label(rescue.Value.Booster), Cost.Free, rescue.Value.Watch));
            }

            int columns = choices.Count <= 3 ? Math.Max(1, choices.Count) : choices.Count == 4 ? 2 : 3;
            int rows = (choices.Count + columns - 1) / columns;
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            float subtitleWidth = ScreenLayout.Sheet(w, h, insets, 0f).Subtitle.Width / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            List<string> subtitle = UiKit.BalancedLines(_probe, stuck ? Loc.T("jam.stuck_subtitle") : Loc.T("jam.subtitle"), UiKit.Units(T.Body.Size), subtitleWidth * 0.94f);

            // Wanted heights (units): the subtitle's second line, the well, the rows of choices (a button and its pill's
            // overhang), Restart. A short phone shrinks the well, the choices and the gaps together.
            float lineUnits = subtitle.Count > 1 ? 50f : 0f;
            const float wellUnits = 196f;
            float rowUnits = columns <= 2 ? 236f : 222f;
            const float rowGap = 30f;
            const float gap = 34f;
            float flexible = wellUnits + gap + (rows * rowUnits) + (Math.Max(0, rows - 1) * rowGap) + (rows > 0 ? gap : 0f);
            float fixedUnits = lineUnits + DesignTokens.Size.CardPrimaryHeight + 34f;
            SheetView sheet = UiKit.Sheet("Sheet", _host, stuck ? Loc.T("jam.stuck") : Loc.T("jam.title"), subtitle[0], flexible + fixedUnits + 10f);
            _sheet = sheet;
            SheetRegions regions = sheet.Regions;
            Box body = regions.Body;
            float k = Mathf.Clamp(((body.Height / u) - fixedUnits) / flexible, 0.62f, 1f);
            float y = body.Top;
            if (subtitle.Count > 1)
            {
                TextMeshProUGUI second = UiKit.Label("Subtitle2", sheet.SheetRect, subtitle[1], T.Body, UiTheme.Of(C.InkBrownSoft));
                UiKit.PlaceBox(second.rectTransform, regions.Subtitle.Offset(0f, 48f * u), regions.Sheet);
                y += lineUnits * u;
            }

            // The slots' contents, as in the reference's inset row.
            var wellBox = new Box(body.Left + (14f * u), y, body.Right - (14f * u), y + (wellUnits * k * u));
            Image well = UiKit.Well("Slots", sheet.Body, Mathf.Min(36f, wellUnits * k * 0.2f));
            UiKit.PlaceBox(well.rectTransform, wellBox, body);
            SlotRow(well.rectTransform, slots);
            y = wellBox.Bottom + (gap * k * u);

            float rowHeight = rowUnits * k * u;
            float gapX = 30f * u;
            float cellWidth = Mathf.Min((columns <= 2 ? 440f : 300f) * u, (body.Width - (gapX * (columns - 1))) / columns);
            for (int row = 0; row < rows; row++)
            {
                int first = row * columns;
                int inRow = Math.Min(columns, choices.Count - first);
                Box[] cells = ScreenLayout.Row(new Box(body.Left, y, body.Right, y + rowHeight), inRow, gapX, cellWidth, square: false);
                for (int i = 0; i < inRow; i++)
                {
                    (string id, ColorSet set, string label, Cost? price, Action action) = choices[first + i];
                    ChoiceButtonView choice = UiKit.ChoiceButton(id, sheet.Body, set, GardenLook.BoosterIcon(id), label, price, action);
                    UiKit.PlaceBox((RectTransform)choice.transform, cells[i], body);
                }

                y += rowHeight + ((row < rows - 1 ? rowGap : gap) * k * u);
            }

            // Restart is as big as a card's main button, as on the reference's jam card.
            Button restart = UiKit.SecondaryButton("Restart", sheet.Body, Loc.T("common.restart"), _onRestart, "ui.restart", T.Button);
            UiKit.PlaceBox((RectTransform)restart.transform, ScreenLayout.CardButton(body, y + (8f * u), true, u), body);
        }

        /// <summary>
        /// The Waiting Slots' contents in the well (the playtest's <c>EndCards.SlotContents</c>): tiles of 54% of the well's
        /// height (at most 112 units) in cells of 1.62 tiles, each pod's sticker tile with its count below it, a free slot
        /// as a small dashed plate, a locked one with its padlock; laid out from the well's own box.
        /// </summary>
        private static void SlotRow(RectTransform well, IReadOnlyList<JamSlot> slots)
        {
            if (slots.Count == 0)
            {
                return;
            }

            var cells = new List<(RectTransform Tile, TextMeshProUGUI? Count)>();
            for (int i = 0; i < slots.Count; i++)
            {
                JamSlot slot = slots[i];
                string index = i.ToString(CultureInfo.InvariantCulture);
                if (slot.State == SlotPlateState.Working)
                {
                    CandyTileView tile = UiKit.CandyTile("Tile" + index, well, slot.Variant, TileStyle.Sticker);
                    TextMeshProUGUI count = UiKit.KitLabel("Count" + index, well, slot.Count.ToString(CultureInfo.InvariantCulture), T.Count, TextLook.Plain(C.InkBrown));
                    cells.Add(((RectTransform)tile.transform, count));
                }
                else
                {
                    SlotPlateView plate = UiKit.SlotPlate("Slot" + index, well);
                    plate.Show(slot.State == SlotPlateState.Locked ? SlotPlateState.Locked : SlotPlateState.Empty);
                    cells.Add(((RectTransform)plate.transform, null));
                }
            }

            BoxLayout.On(well).Then(b =>
            {
                float tile = Mathf.Min(b.Height * 0.54f, UiKit.Units(112f));
                float cell = Mathf.Min(tile * 1.62f, (b.Width - UiKit.Units(24f)) / cells.Count);
                Box[] boxes = ScreenLayout.Row(b.Inset(UiKit.Units(12f), 0f), cells.Count, 0f, cell, square: false);
                float top = b.Top + ((b.Height - (tile * 1.5f)) / 2f);
                for (int i = 0; i < cells.Count; i++)
                {
                    Box tileBox = Box.FromCenter(boxes[i].CenterX, top + (tile / 2f), tile, tile);
                    BoxLayout.Place(cells[i].Tile, tileBox);
                    TextMeshProUGUI? count = cells[i].Count;
                    if (count != null)
                    {
                        UiKit.PlaceCount(count, new Box(tileBox.Left - (tile * 0.2f), tileBox.Bottom + (tile * 0.06f), tileBox.Right + (tile * 0.2f), tileBox.Bottom + (tile * 0.5f)), false);
                    }
                }
            });
        }
    }
}
