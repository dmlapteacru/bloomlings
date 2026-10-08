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
    /// One Waiting Slot as the jam card shows it (spec 005 §4.3, <c>ui.jam.slots</c>): a pod's variant and its count
    /// (<see cref="SlotPlateState.Working"/>; a null variant is a mystery pod), a free slot
    /// (<see cref="SlotPlateState.Empty"/>) or a locked one (<see cref="SlotPlateState.Locked"/>).
    /// </summary>
    public sealed record JamSlot(SlotPlateState State, VariantId? Variant, int Count);

    /// <summary>
    /// The jam card of the design board's frame 10 (spec 002 FR-019; spec 001 FR-027, T051, T121) in the reference layout
    /// of spec 005 (FR-022, contracts/look.md §4.3 and §6.2; the playtest's <c>EndCards.Jam</c>): a centered modal
    /// parchment card over the dimmed gameplay, laid out by <see cref="ScreenLayout.JamCard"/>, top to bottom:
    /// <list type="bullet">
    /// <item><description>"No more space!" (or "No pod can move!" when Stuck) and its subtitle in up to two balanced
    /// lines;</description></item>
    /// <item><description>the Waiting Slots' contents in an inset well: each pod's sticker tile with its count, a free
    /// slot as a small dashed plate, a locked one with its padlock;</description></item>
    /// <item><description>one big colored choice per usable recovery (green Extra Slot and Shuffle, blue Return and
    /// Bloom Burst) with its icon, in a two-column grid (an odd last one centered), each with its cost pill (×N charges,
    /// or the lotus and the price) hanging under its bottom edge; the free rescue (a rewarded ad, once per attempt) as
    /// one more green choice with the "▶ Free" pill;</description></item>
    /// <item><description>Restart as a cream button with ⟳.</description></item>
    /// </list>
    /// The card pops in over a warm scrim that keeps the board visible and takes every tap off the card, except over the
    /// gameplay top bar, so Pause stays usable during a jam. The rules give no way back to play but the card's choices,
    /// so it shows no close button. It never opens the Store. A short phone shrinks every height and gap together
    /// (<see cref="JamCardRegions.Scale"/>).
    /// </summary>
    public sealed class JamScreen : MonoBehaviour
    {
        /// <summary>The title's letters fill at most this share of its box's height (the playtest's <c>EndCards.TitleFill</c>).</summary>
        private const float TitleFill = 0.68f;

        private RectTransform _host = null!;
        private TextMeshProUGUI _probe = null!;
        private GameObject? _card;
        private Action _onRestart = () => { };
        private Action<Recovery> _onRecovery = _ => { };

        public bool IsOpen => _host.gameObject.activeSelf;

        /// <param name="tapThrough">
        /// The gameplay top bar: the scrim lets taps through over it, so Pause stays usable while the card shows (spec 005
        /// FR-002); null: the scrim takes every tap.
        /// </param>
        public static JamScreen Create(Transform parent, Action onRestart, Action<Recovery> onRecovery, RectTransform? tapThrough = null)
        {
            // The card is built on each Show (its height follows the choices); the scrim keeps the screen in its place.
            Image scrim = UiFactory.CreateImage("JamScreen", parent, null, UiTheme.PanelShade, raycast: true);
            RectTransform host = UiFactory.Stretch(scrim.rectTransform);
            scrim.gameObject.AddComponent<RaycastHole>().Hole = tapThrough;
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
            if (_card != null)
            {
                Destroy(_card);
                _card = null;
            }

            _host.gameObject.SetActive(true);
            Build(stuck, recoveries, cost, slots, rescue);
        }

        public void Hide() => _host.gameObject.SetActive(false);

        /// <summary>The Waiting Slots of <paramref name="view"/> as the card shows them: every present slot, left to right.</summary>
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

            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            JamCardRegions r = ScreenLayout.JamCard(w, h, insets, choices.Count, hasClose: false);
            float k = Mathf.Min(1f, r.Scale);
            Box cardBox = r.Card;

            // The popups' wooden frame and the corner sprigs (spec 005 FR-045) that pop in (motion.pop); everything on it
            // pops with it.
            Image card = UiKit.CardFrame("Card", _host);
            UiKit.PlaceScreen(card.rectTransform, cardBox);
            card.gameObject.AddComponent<PopMotion>();
            UiKit.CardDecoration(card.transform, BoxLayout.On(card.rectTransform));
            _card = card.gameObject;

            // The title in ink.title (its letters about 0.68 of its box tall, as the reference's "No More Space!"), and the
            // subtitle in up to two balanced lines.
            Box title = UiKit.ToLocal(r.Title, cardBox);
            TextMeshProUGUI titleLabel = UiKit.KitLabel("Title", card.transform, stuck ? Loc.T("jam.stuck") : Loc.T("jam.title"), T.Title, TextLook.Plain(C.InkTitle));
            KitText.Place(titleLabel, T.Title, title.CenterX, title.CenterY, Mathf.Min(UiKit.Units(T.Title.Size) * k, title.Height * TitleFill), title.Width);

            Box subtitle = UiKit.ToLocal(r.Subtitle, cardBox);
            float bodySize = UiKit.Units(T.Body.Size) * k;
            List<string> lines = UiKit.BalancedLines(_probe, stuck ? Loc.T("jam.stuck_subtitle") : Loc.T("jam.subtitle"), bodySize, subtitle.Width);
            float line = Mathf.Min(bodySize * 1.25f, subtitle.Height / Mathf.Max(1, lines.Count));
            float first = subtitle.CenterY - (line * (lines.Count - 1) / 2f);
            for (int i = 0; i < lines.Count; i++)
            {
                TextMeshProUGUI label = UiKit.KitLabel("Subtitle" + i.ToString(CultureInfo.InvariantCulture), card.transform, lines[i], T.Body, TextLook.Plain(C.InkBrownSoft));
                KitText.Place(label, T.Body, subtitle.CenterX, first + (i * line), bodySize, subtitle.Width);
            }

            // The slots' contents, as in the reference's inset well.
            Image well = UiKit.Well("Slots", card.transform, Mathf.Min(36f, r.Well.Height / Mathf.Max(0.0001f, UiKit.PixelsPerUnit) * 0.2f));
            UiKit.PlaceBox(well.rectTransform, r.Well, cardBox);
            SlotRow(well.rectTransform, r, slots);

            // The choices in two columns, each with its cost pill hanging under it; the pill takes the choice's tap too.
            for (int i = 0; i < choices.Count; i++)
            {
                (string id, ColorSet set, string label, Cost? price, Action action) = choices[i];
                IReadOnlyList<IconPart> icon = GardenLook.BoosterIcon(id);
                ChoiceButtonView choice = UiKit.ChoiceButton(id, card.transform, set, icon, label, null, action);
                UiKit.PlaceBox((RectTransform)choice.transform, r.Choices[i], cardBox);
                if (price.HasValue)
                {
                    CostPillView pill = UiKit.CostPill(id + "Cost", card.transform, price.Value);
                    pill.SetChargeIcon(icon);
                    UiKit.PlaceBox((RectTransform)pill.transform, r.CostPills[i], cardBox);
                    Image touch = UiFactory.CreateImage("Touch", pill.transform, null, Color.clear, raycast: true);
                    UiFactory.Stretch(touch.rectTransform);
                    UiKit.TapTarget(touch, action);
                }
            }

            // Restart: a cream button with ⟳, as on the reference's jam card.
            Button restart = UiKit.SecondaryButton("Restart", card.transform, Loc.T("common.restart"), _onRestart, "ui.restart", T.Button);
            UiKit.PlaceBox((RectTransform)restart.transform, r.Restart, cardBox);
        }

        /// <summary>
        /// The Waiting Slots' contents in the well (<see cref="JamCardRegions.WellCell"/>; the playtest's
        /// <c>EndCards.SlotContents</c>): each pod's sticker tile with its count below it, a free slot as a small dashed
        /// plate, a locked one with its padlock.
        /// </summary>
        private static void SlotRow(RectTransform well, JamCardRegions r, IReadOnlyList<JamSlot> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                JamSlot slot = slots[i];
                string index = i.ToString(CultureInfo.InvariantCulture);
                (Box tileBox, Box countBox) = r.WellCell(i, slots.Count);
                if (slot.State == SlotPlateState.Working)
                {
                    CandyTileView tile = UiKit.CandyTile("Tile" + index, well, slot.Variant, TileStyle.Sticker);
                    UiKit.PlaceBox((RectTransform)tile.transform, tileBox, r.Well);
                    TextMeshProUGUI count = UiKit.KitLabel("Count" + index, well, slot.Count.ToString(CultureInfo.InvariantCulture), T.Count, TextLook.Plain(C.InkBrown));
                    Box local = UiKit.ToLocal(countBox, r.Well);
                    float digits = Mathf.Min(local.Height * 0.9f, UiKit.ToLocal(tileBox, r.Well).Height * 0.42f);
                    UiKit.PlaceCount(count, Box.FromCenter(local.CenterX, local.Top + (digits * 0.62f), local.Width, digits), false, 1f);
                }
                else
                {
                    SlotPlateView plate = UiKit.SlotPlate("Slot" + index, well);
                    plate.Show(slot.State == SlotPlateState.Locked ? SlotPlateState.Locked : SlotPlateState.Empty);
                    UiKit.PlaceBox((RectTransform)plate.transform, tileBox, r.Well);
                }
            }
        }
    }
}
