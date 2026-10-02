using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The Waiting Slots in the states of frame 13 (spec 002 FR-013), as the reference's cream plates on the tray's
    /// parchment (spec 005 contracts/look.md §3.7, research D3; <see cref="Kit.SlotPlate"/>):
    /// <list type="bullet">
    /// <item><description>empty: a slightly sunk plate with a dashed inner outline;</description></item>
    /// <item><description>working: a raised plate with the pod's variant tile and its plain count below;</description></item>
    /// <item><description>stuck: the tile greyed, with the hourglass;</description></item>
    /// <item><description>locked: a grey plate with the padlock;</description></item>
    /// <item><description>danger: the last free usable slot, its dashed outline red with "!";</description></item>
    /// <item><description>extra: the sixth slot from Extra Slot, marked with a green "+".</description></item>
    /// </list>
    /// A pod pops into its plate, its count bumps as Bloomlings land, a mystery tile flips over when revealed, and a
    /// finished pod puffs away off the plate, which is empty again under it.
    /// </summary>
    public static class SlotPainter
    {
        /// <summary>A plate's width for its height: a little taller than wide, like the reference's slots.</summary>
        public const float PlateAspect = 0.82f;

        public static void DrawRow(IPainter p, Box area, LevelScreen s)
        {
            LevelView view = s.Session.View;
            var slots = new List<int>();
            for (int i = 0; i < view.SlotCapacity; i++)
            {
                s.SlotBoxes[i] = null;
                if (view.SlotStateOf(i) != SlotState.Absent)
                {
                    slots.Add(i);
                }
            }

            int free = 0;
            foreach (int slot in slots)
            {
                if (view.SlotStateOf(slot) == SlotState.Free && !s.Animator.HeldSlotLocks.Contains(slot) && s.Animator.Slots[slot].PodId == null)
                {
                    free++;
                }
            }

            Box[] cells = Cells(p, area, slots.Count);
            for (int n = 0; n < slots.Count; n++)
            {
                int slot = slots[n];
                Box box = cells[n];
                s.SlotBoxes[slot] = box;
                bool locked = view.SlotStateOf(slot) == SlotState.Locked || s.Animator.HeldSlotLocks.Contains(slot);
                SlotLook look = s.Animator.Slots[slot];
                bool danger = !locked && look.PodId == null && free == 1 && s.Session.Status == LevelStatus.Playing;
                bool target = s.Targeting == Recovery.Return && look.PodId != null && !look.IsLeaving;
                if (target)
                {
                    // Return is choosing its slot: every pod it can take back glows like the selected booster tile.
                    TargetGlow(p, box);
                }

                // A pod whose clears the rules have resolved but the animation has not played yet still has work: it is
                // not drawn stuck while its Bloomlings wait for their wave.
                bool pending = look.PodId != null && view.Pod(look.PodId).Remaining < look.Count;
                Slot(p, box, look, locked, danger, extra: slot >= WaitingSlots.DefaultCount, s.Animator.Now, pending, Arrival(s, slot, look));
                if (target)
                {
                    int index = slot;
                    p.Hit(box, () => s.UseBooster(Client.Services.Save.BoosterKind.Return, new UseReturn(index)));
                }
            }
        }

        /// <summary>
        /// The plates of the slot row: as tall as the band allows (less a margin for their shadows), <see cref="PlateAspect"/>
        /// as wide, a quarter of a plate apart and centered; narrower when six do not fit.
        /// </summary>
        public static Box[] Cells(IPainter p, Box area, int count)
        {
            var cells = new Box[Math.Max(0, count)];
            if (count <= 0)
            {
                return cells;
            }

            float height = area.Height - p.U(12f);
            float width = height * PlateAspect;
            float gap = width * 0.27f;
            float fit = (area.Width - p.U(12f)) / ((count * 1.27f) - 0.27f);
            if (fit < width)
            {
                width = fit;
                gap = width * 0.27f;
            }

            float total = (width * count) + (gap * (count - 1));
            float x = area.CenterX - (total / 2f);
            float top = area.CenterY - (height / 2f) - p.U(2f);
            for (int i = 0; i < count; i++)
            {
                cells[i] = new Box(x, top, x + width, top + height);
                x += width + gap;
            }

            return cells;
        }

        /// <summary>How far the pod shown in a slot has flown in from the tray (0–1; 1 when it is not flying).</summary>
        private static float Arrival(LevelScreen s, int slot, SlotLook look)
        {
            foreach (Flight flight in s.Animator.Flights)
            {
                if (!flight.ToTray && flight.Slot == slot && flight.PodId == look.PodId)
                {
                    return Visuals.Clamp01((s.Animator.Now - flight.Start) / LevelAnimator.FlightSeconds);
                }
            }

            return 1f;
        }

        /// <summary>
        /// One slot in a state of frame 13, with its pop, count bump, reveal flip and puff (<paramref name="now"/> is the
        /// animation clock). A pod with <paramref name="pending"/> work is drawn working; while it is still flying in
        /// (<paramref name="arrival"/> below 1) it fades in over the empty plate as its tile lands.
        /// </summary>
        public static void Slot(IPainter p, Box box, SlotLook look, bool locked, bool danger, bool extra, float now, bool pending = false, float arrival = 1f)
        {
            if (locked)
            {
                Kit.SlotPlate(p, box, SlotPlateState.Locked, extra: extra);
                return;
            }

            if (look.PodId == null)
            {
                Kit.SlotPlate(p, box, danger ? SlotPlateState.Danger : SlotPlateState.Empty, extra: extra);
                return;
            }

            // The pod pops into the plate, and when it is done it puffs away over the empty plate.
            float scale = 1f;
            if (now - look.PoppedAt < 0.16f)
            {
                scale += 0.12f * (float)Math.Sin((now - look.PoppedAt) / 0.16f * Math.PI);
            }

            float alpha = 1f;
            if (look.IsLeaving)
            {
                float k = Visuals.Clamp01((now - look.LeavingAt) / LevelAnimator.ExitSeconds);
                scale += 0.3f * k;
                alpha = 1f - k;
                p.Mark("fx.puff");
                Kit.SlotPlate(p, box, SlotPlateState.Empty, extra: extra);
            }
            else if (arrival < 1f)
            {
                alpha = 0.25f + (0.75f * arrival * arrival);
                Kit.SlotPlate(p, box, SlotPlateState.Empty, extra: extra);
            }

            // A mystery pod's "?" flips over to its variant (FR-039).
            float flip = 1f;
            bool hidden = !look.Variant.HasValue;
            float turn = (now - look.RevealAt) / 0.3f;
            if (turn >= 0f && turn < 1f)
            {
                flip = Math.Max(0.04f, Math.Abs((float)Math.Cos(turn * Math.PI)));
                hidden = turn < 0.5f;
            }

            bool working = look.InFlight > 0 || look.IsLeaving || pending;
            float bump = now - look.BumpedAt < 0.15f ? 1f + (0.25f * (float)Math.Sin((now - look.BumpedAt) / 0.15f * Math.PI)) : 1f;
            p.PushAlpha(alpha);
            p.PushTransform(0f, 0f, scale, box.CenterX, box.CenterY);
            Kit.SlotPlate(p, box, working ? SlotPlateState.Working : SlotPlateState.Stuck, hidden ? null : look.Variant, look.Count, extra && !look.IsLeaving, flip, bump);
            p.PopTransform();
            p.PopAlpha();
        }

        /// <summary>The golden glow around a plate Return can take its pod back from (the selected booster's glow, FR-031).</summary>
        public static void TargetGlow(IPainter p, Box box)
        {
            p.Mark("slot.state.target");
            float s = Math.Min(box.Width, box.Height);
            float radius = s * 0.2f;
            float glow = GardenLook.Glow(p.Now);
            for (int ring = 3; ring >= 1; ring--)
            {
                float grow = s * 0.16f * ring / 3f;
                p.FillRound(box.Inset(-grow), radius + grow, C.GardenGlow.WithAlpha(0.24f * glow));
            }

            p.StrokeRound(box.Inset(-s * 0.04f), radius + (s * 0.04f), s * 0.04f, C.GardenGlow.WithAlpha(glow));
        }
    }
}
