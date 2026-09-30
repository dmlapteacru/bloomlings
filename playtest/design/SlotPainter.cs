using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The Waiting Slots in the states of frame 13 (spec 002 FR-013):
    /// <list type="bullet">
    /// <item><description>empty: a soft sunk tile;</description></item>
    /// <item><description>working: the pod bright;</description></item>
    /// <item><description>stuck: greyed, with the hourglass;</description></item>
    /// <item><description>locked: a padlock;</description></item>
    /// <item><description>danger: the last free usable slot in a red dashed frame;</description></item>
    /// <item><description>extra: the sixth slot from Extra Slot, marked with a green "+".</description></item>
    /// </list>
    /// </summary>
    public static class SlotPainter
    {
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

            // A soft band behind the row.
            p.FillRound(area.Inset(-p.U(10f), -p.U(8f)), p.U(36f), C.SurfacePanel.WithAlpha(0.55f));
            Box[] cells = ScreenLayout.Row(area.Inset(p.U(6f), p.U(6f)), slots.Count, p.U(16f), p.U(170f), square: true);
            for (int n = 0; n < slots.Count; n++)
            {
                int slot = slots[n];
                Box box = cells[n];
                s.SlotBoxes[slot] = box;
                bool locked = view.SlotStateOf(slot) == SlotState.Locked || s.Animator.HeldSlotLocks.Contains(slot);
                SlotLook look = s.Animator.Slots[slot];
                bool danger = !locked && look.PodId == null && free == 1 && s.Session.Status == LevelStatus.Playing;
                Slot(p, box, look, locked, danger, extra: slot >= WaitingSlots.DefaultCount, s.Animator.Now);
                if (s.Targeting == Recovery.Return && look.PodId != null && !look.IsLeaving)
                {
                    int index = slot;
                    p.StrokeRound(box.Inset(-p.U(6f)), box.Width * 0.28f, p.U(5f), C.BoosterReturn);
                    p.Hit(box, () => s.UseBooster(Client.Services.Save.BoosterKind.Return, new UseReturn(index)));
                }
            }
        }

        /// <summary>One slot in a state of frame 13.</summary>
        public static void Slot(IPainter p, Box box, SlotLook look, bool locked, bool danger, bool extra, float now)
        {
            float radius = box.Width * DesignTokens.Radius.Slot;
            if (locked)
            {
                p.Mark("slot.state.locked");
                Kit.Raised(p, box, C.StateLockBg, C.StateLockBg.Darken(0.2f), radius);
                p.Shape("ui.lock", box.Inset(box.Width * 0.28f).Offset(0f, -p.U(3f)), C.StateLock.Darken(0.15f));
                return;
            }

            if (extra)
            {
                p.Mark("slot.extra");
            }

            if (look.PodId == null)
            {
                p.Mark("slot.empty");
                p.FillRound(box, radius, C.SurfaceSunk);
                p.FillRound(new Box(box.Left, box.Top, box.Right, box.Top + (box.Height * 0.2f)), radius, C.SurfacePanelEdge.WithAlpha(0.5f));
                p.FillRound(box.Inset(0f, 0f).Offset(0f, box.Height * 0.06f).Inset(p.U(2f)), radius, C.SurfaceSunk);
                if (danger)
                {
                    p.Mark("slot.state.danger");
                    p.FillRound(box, radius, C.StateDanger.WithAlpha(0.08f));
                    p.StrokeRound(box.Inset(p.U(3f)), radius, p.U(5f), C.StateDanger, p.U(16f), p.U(10f));
                    p.Shape("slot.state.jam_risk", box.Inset(box.Width * 0.32f), C.StateDanger);
                }
            }
            else
            {
                SlotPod(p, box, look, now);
            }

            if (extra)
            {
                float r = box.Width * 0.17f;
                p.FillCircle(box.Left + (r * 0.6f), box.Top + (r * 0.6f), r + p.U(3f), Rgba.White);
                p.FillCircle(box.Left + (r * 0.6f), box.Top + (r * 0.6f), r, C.AccentPlus);
                p.Shape("ui.plus", Box.FromCenter(box.Left + (r * 0.6f), box.Top + (r * 0.6f), r * 1.2f, r * 1.2f), C.TextOnColor);
            }
        }

        /// <summary>A pod in a slot: bright while its Bloomlings work, greyed with the hourglass while it waits.</summary>
        private static void SlotPod(IPainter p, Box box, SlotLook look, float now)
        {
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
            }

            bool working = look.InFlight > 0 || look.IsLeaving;
            float scaleX = scale;
            bool hidden = !look.Variant.HasValue;
            float flip = (now - look.RevealAt) / 0.3f;
            if (flip >= 0f && flip < 1f)
            {
                scaleX *= Math.Abs((float)Math.Cos(flip * Math.PI));
                hidden = flip < 0.5f;
            }

            Box b = box.Scale(scaleX, scale);
            float radius = b.Width * DesignTokens.Radius.Slot;
            p.PushAlpha(alpha);
            if (hidden)
            {
                p.Mark("pod.state.mystery");
                Rgba card = Rgba.FromHex("#F7D6E6");
                Box face = Kit.Raised(p, b, card, card.Darken(0.18f), radius);
                p.Shape("tile.mystery", Box.FromCenter(face.CenterX, face.Top + (face.Height * 0.4f), face.Width * 0.46f, face.Width * 0.46f), Rgba.FromHex("#C0508A"));
                PodPainter.CountPill(p, face, look.Count, false);
                p.PopAlpha();
                return;
            }

            Rgba color = Visuals.ColorOf(look.Variant!.Value);
            if (working)
            {
                p.Mark("slot.state.working");
            }
            else
            {
                p.Mark("slot.state.stuck");
            }

            Rgba tint = working ? DesignTokens.PodCard(color) : DesignTokens.PodCard(color).Grey();
            Box f = Kit.Raised(p, b, tint, working ? DesignTokens.PodCardEdge(color) : DesignTokens.PodCardEdge(color).Grey(), radius);
            Rgba body = working ? color : color.Grey().Mix(C.StateStuck, 0.35f);
            Box figure = Box.FromCenter(f.CenterX, f.Top + (f.Height * 0.4f), f.Width * 0.76f, f.Width * 0.76f);
            Visuals.Bloomling(p, figure, Visuals.FamilyOf(look.Variant.Value), body, Visuals.SymbolOf(look.Variant.Value), face: working, symbolScale: 0.52f);
            float bump = now - look.BumpedAt < 0.15f ? 1f + (0.25f * (float)Math.Sin((now - look.BumpedAt) / 0.15f * Math.PI)) : 1f;
            p.PushTransform(0f, 0f, bump, f.CenterX, f.Bottom);
            PodPainter.CountPill(p, f, look.Count, !working);
            p.PopTransform();
            if (!working && look.Count > 0)
            {
                float r = f.Width * 0.17f;
                p.FillCircle(f.Right - (r * 0.55f), f.Top + (r * 0.55f), r, Rgba.White);
                p.Shape("slot.state.waiting", Box.FromCenter(f.Right - (r * 0.55f), f.Top + (r * 0.55f), r * 1.3f, r * 1.3f), C.TextSecondary);
            }

            p.PopAlpha();
        }
    }
}
