using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The guided spotlight over the gameplay screen (spec 005 FR-035, <see cref="Spotlight"/>, <see cref="GuideTour"/>):
    /// the screen dimmed but for the step's lit place, a glow ring breathing round it, the message on a parchment bubble
    /// pointing at it, and on a forced step the pointing hand. The scrim takes every tap; a forced step lays its lit
    /// place's own targets over it again (the pod, the booster, Return's slots, Bloom Burst's tiles), so only those work;
    /// any other step goes on with a tap anywhere. The Unity client's <c>GuideOverlay</c> is its twin.
    /// </summary>
    public static class GuidePainter
    {
        /// <summary>How long a step fades in.</summary>
        public const float FadeSeconds = 0.25f;

        public static void Draw(IPainter p, LevelScreen s, BoardLayout board, ReferenceGameplayRegions r)
        {
            GuideStep step = s.Guide!;
            List<Box> holes = Holes(p, s, step, board);
            if (holes.Count == 0)
            {
                // Its place is not on screen (a booster bar not laid out yet): the step waits a frame.
                return;
            }

            float t = s.App.Now - s.GuideOpenedAt;
            bool icon = Icon(step) != null || step.Kind == GuideKind.Blocked;
            (string Text, bool Strong)[] lines = Lines(p, step, icon);
            SpotlightLayout layout = Spotlight.Layout(p.Width, p.Height, p.Insets, Spotlight.Union(holes), lines.Length, icon, !step.Forced, step.Forced);
            float radius = step.Kind == GuideKind.Blocked ? Spotlight.CellRadiusUnits : Spotlight.RadiusUnits;

            p.PushAlpha(Visuals.Clamp01(t / FadeSeconds));
            Scrim(p, holes, radius);

            // The ring breathes round the lit place (round the arch only when the lit place is many tiles).
            Box ringed = holes[0];
            Ring(p, ringed, Math.Min(p.U(radius), Math.Min(ringed.Width, ringed.Height) / 2f), t);
            Bubble(p, layout, step, lines, s);
            if (step.Forced)
            {
                Box hand = Spotlight.HandAt(layout, t);
                p.Shape("ui.pointer", hand.Inset(-p.U(5f)), C.InkBrown);
                p.Shape("ui.pointer", hand, Rgba.White);
            }

            p.PopAlpha();

            // The scrim takes every tap; a forced step's lit targets are laid over it again.
            p.Hit(new Box(0f, 0f, p.Width, p.Height), step.Forced ? () => { } : (Action)s.NextGuideStep);
            if (step.Forced)
            {
                LitTargets(p, s, step, board);
            }
        }

        /// <summary>
        /// The lit holes of a step on screen this frame, the first one ringed: the arches (the entry), the arch and each tile
        /// that blocks the way, the guided pod, the booster, Return's plates, the board's tiles. Empty while its place is not
        /// drawn yet.
        /// </summary>
        public static List<Box> Holes(IPainter p, LevelScreen s, GuideStep step, BoardLayout board)
        {
            LevelView view = s.Session.View;
            var holes = new List<Box>();
            void Lit(Box? box)
            {
                if (box.HasValue)
                {
                    holes.Add(Spotlight.HoleAround(box.Value, p.Scale));
                }
            }

            switch (step.Kind)
            {
                case GuideKind.Entry:
                    Lit(Union(Arches(view, board)));
                    break;
                case GuideKind.Blocked:
                    Lit(Union(Arches(view, board)));
                    foreach (CellPos cell in GuideTour.BlockingCells(view))
                    {
                        // The tiles themselves, a hair wider so neighbours join.
                        holes.Add(board.CellBox(cell.X, cell.Y).Inset(-p.U(2f)));
                    }

                    break;
                case GuideKind.FirstTap:
                    Lit(s.GuidePod != null && s.PodBoxes.TryGetValue(s.GuidePod, out Box pod) ? Kit.Touch(p, pod) : (Box?)null);
                    break;
                case GuideKind.BoosterTarget when step.Booster == BoosterKind.Return:
                    Lit(Union(ReturnSlots(s)));
                    break;
                case GuideKind.BoosterTarget:
                    Lit(board.Grid);
                    break;
                default:
                    Lit(step.Booster.HasValue && s.BoosterBoxes.TryGetValue(step.Booster.Value, out Box booster) ? booster : (Box?)null);
                    break;
            }

            return holes;
        }

        /// <summary>The plates of the pods Return can take back now, with the slot each one is in the rules.</summary>
        public static List<(Box Box, int Slot)> ReturnPlates(LevelScreen s)
        {
            var plates = new List<(Box, int)>();
            LevelView view = s.Session.View;
            for (int i = 0; i < s.SlotBoxes.Length; i++)
            {
                SlotLook look = s.Animator.Slots[i];
                int rules = LevelAnimator.RulesSlotOf(view, look.PodId);
                if (s.SlotBoxes[i] is Box box && look.PodId != null && !look.IsLeaving && rules >= 0 && s.Session.Check(new UseReturn(rules)).IsAllowed)
                {
                    plates.Add((box, rules));
                }
            }

            return plates;
        }

        private static List<Box> ReturnSlots(LevelScreen s)
        {
            var boxes = new List<Box>();
            foreach ((Box box, int _) in ReturnPlates(s))
            {
                boxes.Add(box);
            }

            return boxes;
        }

        private static List<Box> Arches(LevelView view, BoardLayout board)
        {
            var boxes = new List<Box>();
            foreach (EntryDef entry in view.Entries)
            {
                boxes.Add(BoardLayout.ArchBounds(board.Arch(entry)));
            }

            return boxes;
        }

        private static Box? Union(List<Box> boxes)
        {
            if (boxes.Count == 0)
            {
                return null;
            }

            float l = boxes[0].Left, t = boxes[0].Top, r = boxes[0].Right, b = boxes[0].Bottom;
            foreach (Box box in boxes)
            {
                l = Math.Min(l, box.Left);
                t = Math.Min(t, box.Top);
                r = Math.Max(r, box.Right);
                b = Math.Max(b, box.Bottom);
            }

            return new Box(l, t, r, b);
        }

        /// <summary>The scrim with the soft lit holes, rendered at a quarter of the screen and stretched (its edges are soft).</summary>
        private static void Scrim(IPainter p, List<Box> holes, float radiusUnits)
        {
            p.Mark("ui.spotlight");
            float k = Spotlight.RasterShare;
            p.PushTransform(0f, 0f, 1f / k, 0f, 0f);
            float w = p.Width;
            float h = p.Height;
            p.Picture(Spotlight.ScrimKey(holes, w, h), new Box(0f, 0f, w * k, h * k), (pw, ph) => Spotlight.Scrim(holes, w, h, pw, ph, radiusUnits));
            p.PopTransform();
        }

        private static void Ring(IPainter p, Box hole, float radius, float t)
        {
            float pulse = Spotlight.Pulse(t);
            float grow = p.U(3f + (7f * pulse));
            p.StrokeRound(hole.Inset(-grow), radius + grow, p.U(6f), C.GardenGlow.WithAlpha(0.95f - (0.45f * pulse)));
        }

        private static void Bubble(IPainter p, SpotlightLayout layout, GuideStep step, (string Text, bool Strong)[] lines, LevelScreen s)
        {
            Box b = layout.Bubble;
            float pad = p.U(Spotlight.BubblePadUnits);
            float tail = p.U(Spotlight.TailUnits) * 1.3f;
            float edge = layout.TailDown ? b.Bottom : b.Top;
            p.PushRotate(45f, layout.TailX, edge);
            Box diamond = Box.FromCenter(layout.TailX, edge, tail, tail);
            p.FillRound(diamond, p.U(6f), layout.TailDown ? C.ParchmentBottom : C.ParchmentTop);
            p.StrokeRound(diamond, p.U(6f), p.U(DesignTokens.Garden.OutlineWidth) * 0.8f, C.ParchmentLine);
            p.PopTransform();
            Kit.Paper(p, b, p.U(34f), DesignTokens.Garden.OutlineWidth, 6f);

            // The icon at the left: the booster, or the tile that blocks the way.
            float left = b.Left + pad;
            float iconSide = p.U(Spotlight.IconUnits);
            var iconBox = new Box(left, b.CenterY - (iconSide / 2f), left + iconSide, b.CenterY + (iconSide / 2f));
            string? booster = Icon(step);
            bool hasIcon = false;
            if (booster != null)
            {
                Box face = Kit.IconFace(p, iconBox, GardenLook.White, iconSide * 0.26f, 0f);
                Kit.BoosterIcon(p, booster, Box.FromCenter(face.CenterX, face.CenterY, iconSide * 0.7f, iconSide * 0.7f));
                hasIcon = true;
            }
            else if (step.Kind == GuideKind.Blocked && GuideTour.BlockingVariant(s.Session.View) is VariantId variant)
            {
                Kit.CandyTile(p, iconBox, variant, TileStyle.Sticker);
                hasIcon = true;
            }

            float textLeft = hasIcon ? iconBox.Right + pad : b.Left + pad;
            float textWidth = b.Right - pad - textLeft;
            float lineH = p.U(Spotlight.LineUnits);
            float captionH = step.Forced ? 0f : p.U(Spotlight.CaptionUnits);
            float y = b.CenterY - (((lines.Length * lineH) + captionH) / 2f) + (lineH / 2f);
            foreach ((string text, bool strong) in lines)
            {
                Rgba ink = strong ? C.InkBrown : C.InkBrownSoft;
                p.Text(text, textLeft + (textWidth / 2f), y, strong ? T.ButtonSecondary : T.Body, ink, textWidth, look: TextLook.Plain(ink));
                y += lineH;
            }

            if (!step.Forced)
            {
                p.Text(PlaytestText.T("demo.tap_continue"), textLeft + (textWidth / 2f), y - (lineH / 2f) + (captionH / 2f), T.Caption, C.InkBrownSoft, textWidth);
            }
        }

        /// <summary>The booster icon of a booster's steps, or null.</summary>
        private static string? Icon(GuideStep step) => step.Booster.HasValue ? GuideTour.Key(step.Booster.Value) : null;

        /// <summary>The message lines wrapped to the bubble: the first key strong, the next ones soft.</summary>
        private static (string, bool)[] Lines(IPainter p, GuideStep step, bool icon)
        {
            float width = Spotlight.TextWidth(p.Width, p.Height, p.Insets, icon);
            var lines = new List<(string, bool)>();
            for (int i = 0; i < step.MessageKeys.Count; i++)
            {
                foreach (string line in EndCards.Lines(p, PlaytestText.T(step.MessageKeys[i]), i == 0 ? T.ButtonSecondary : T.Body, width, 2))
                {
                    lines.Add((line, i == 0));
                }
            }

            return lines.ToArray();
        }

        /// <summary>A forced step's own targets over the scrim: only the lit place takes the tap.</summary>
        private static void LitTargets(IPainter p, LevelScreen s, GuideStep step, BoardLayout board)
        {
            switch (step.Kind)
            {
                case GuideKind.FirstTap:
                    if (s.GuidePod != null && s.PodBoxes.TryGetValue(s.GuidePod, out Box pod))
                    {
                        string id = s.GuidePod;
                        p.Hit(Kit.Touch(p, pod), () => s.Tap(id));
                    }

                    break;
                case GuideKind.Booster:
                    if (step.Booster.HasValue && s.BoosterBoxes.TryGetValue(step.Booster.Value, out Box button))
                    {
                        BoosterKind kind = step.Booster.Value;
                        p.Hit(Kit.Touch(p, button), () => s.PressBooster(kind, LevelScreen.RecoveryOf(kind)));
                    }

                    break;
                case GuideKind.BoosterTarget when step.Booster == BoosterKind.Return:
                    foreach ((Box box, int slot) in ReturnPlates(s))
                    {
                        p.Hit(Kit.Touch(p, box), () => s.UseBooster(BoosterKind.Return, new UseReturn(slot)));
                    }

                    break;
                case GuideKind.BoosterTarget:
                    // The whole board takes the tap; the tile under the finger picks the symbol (cells are smaller than a
                    // touch target).
                    p.Hit(board.Grid, () =>
                    {
                        (float tx, float ty) = p.TapPoint;
                        int x = (int)Math.Floor((tx - board.Grid.Left) / board.Cell);
                        int y = board.Height - 1 - (int)Math.Floor((ty - board.Grid.Top) / board.Cell);
                        LevelView view = s.Session.View;
                        if (x < 0 || y < 0 || x >= view.Width || y >= view.Height)
                        {
                            return;
                        }

                        CellInfo info = view.Cell(new CellPos(x, y));
                        if (info.Visible.HasValue && !info.MysteryHidden && s.Session.Check(new UseBloomBurst(info.Visible.Value)).IsAllowed)
                        {
                            s.UseBooster(BoosterKind.BloomBurst, new UseBloomBurst(info.Visible.Value));
                        }
                    });

                    break;
            }
        }
    }
}
