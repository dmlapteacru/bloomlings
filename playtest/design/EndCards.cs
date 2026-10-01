using System;
using System.Collections.Generic;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The cards at a level's end and before it: the win (frame 15), the milestone (frame 16), the jam sheet (frame 10)
    /// and the demo card (spec 002 FR-019 to FR-021).
    /// </summary>
    public static class EndCards
    {
        /// <summary>
        /// The win card: the finished picture, the Petals earned with the Petal symbol, NEXT, and the optional ×2 reward.
        /// The playtest has no ads, so ×2 shows as unavailable. The spec 001 sequence stays: reveal, then reward, then
        /// Next.
        /// </summary>
        public static void Win(IPainter p, LevelScreen s, float since)
        {
            LevelReward? reward = s.Payout?.Reward;
            float content = 470f + 130f + (reward?.DroppedBooster != null ? 60f : 0f) + DesignTokens.Size.CardPrimaryHeight + DesignTokens.Size.SecondaryHeight + 100f;
            CardRegions r = Kit.Card(p, content, PlaytestText.T("win.title"), null, Kit.Pop(since));
            float y = r.Body.Top;

            // The finished picture in a frame.
            p.Mark("fx.win_shine");
            var frame = new Box(r.Body.Left + p.U(30f), y, r.Body.Right - p.U(30f), y + p.U(470f));
            Kit.Paper(p, frame, p.U(40f), DesignTokens.Garden.FrameWidthSlots, DesignTokens.Garden.FrameDepthSlots);
            BoardPainter.Picture(p, frame.Inset(p.U(24f)), s.Session.Definition, s.Session.Picture);
            if (since < 1.2f)
            {
                // A light band sweeps the finished picture once.
                float band = frame.Left + ((frame.Width + p.U(200f)) * (since / 1.2f)) - p.U(100f);
                p.PushClip(frame.Inset(p.U(24f)));
                p.Line(band - p.U(60f), frame.Bottom, band + p.U(60f), frame.Top, p.U(70f), Rgba.White.WithAlpha(0.35f));
                p.PopClip();
            }

            y = frame.Bottom + p.U(24f);

            // The reward rises in after the reveal (motion.reward).
            float rise = Kit.Ease((since - 0.15f) / DesignTokens.Motion.Reward.Seconds);
            p.PushAlpha(rise);
            p.PushTransform(0f, (1f - rise) * p.U(30f), 1f, 0f, 0f);
            if (reward != null)
            {
                // The earned Petals count up from 0 (spec 003 FR-020); NEXT works at once.
                string amount = NumberText.Plus(GardenLook.CountUp(reward.Petals, since - 0.15f));
                float w = p.MeasureText(NumberText.Plus(reward.Petals), T.Reward);
                float icon = p.U(80f);
                float cx = r.Body.CenterX - ((icon + p.U(12f)) / 2f);
                p.Text(amount, cx, y + p.U(56f), T.Reward, C.GardenLabelPlain, look: TextLook.Plain(C.GardenLabelPlain));
                Kit.SparkleBurst(p, cx + (w / 2f) + p.U(12f) + (icon / 2f), y + p.U(56f), icon * 1.4f, since - 0.15f);
                Kit.Petal(p, Box.FromCenter(cx + (w / 2f) + p.U(12f) + (icon / 2f), y + p.U(56f), icon, icon));
                if (since < 1.4f)
                {
                    // Petals burst out around the reward.
                    float k = Visuals.Clamp01((since - 0.15f) / 1.2f);
                    for (int i = 0; i < 6; i++)
                    {
                        double a = i * Math.PI / 3;
                        float d = p.U(120f + (90f * k));
                        float size = p.U(34f) * (1f - (0.5f * k));
                        p.PushAlpha(1f - k);
                        p.Shape("fx.petal_burst", Box.FromCenter(cx + (float)(Math.Cos(a) * d), y + p.U(56f) + (float)(Math.Sin(a) * d * 0.55f), size, size), C.PetalFill);
                        p.PopAlpha();
                    }
                }
                y += p.U(120f);
                if (reward.DroppedBooster.HasValue)
                {
                    p.Text(PlaytestText.F("win.drop", BoosterName(reward.DroppedBooster.Value)), r.Body.CenterX, y + p.U(24f), T.Body, C.TextSecondary);
                    y += p.U(60f);
                }
            }

            p.PopTransform();
            p.PopAlpha();

            Box next = ScreenLayout.CardButton(r.Body, y + p.U(16f), true, p.Scale);
            Kit.PrimaryButton(p, next, PlaytestText.T("common.next"), s.Next, decorate: true);
            Box twice = ScreenLayout.CardButton(r.Body, next.Bottom + p.U(24f), false, p.Scale).Inset(p.U(60f), 0f);
            Kit.SecondaryButton(p, twice, PlaytestText.T("win.double"), null, "ui.ad");
            p.Text("no ads in the playtest", r.Body.CenterX, twice.Bottom + p.U(34f), T.Caption, C.TextSecondary);
            Kit.EndCard(p);

            Confetti(p, s, since);
        }

        /// <summary>The milestone card: LEVEL N, "Milestone reached!", one icon with an amount per reward, CONTINUE.</summary>
        public static void Milestone(IPainter p, LevelScreen s, float since)
        {
            MilestoneGrant grant = s.Payout!.Milestone!;
            CardRegions r = Kit.Card(p, 60f + 300f + DesignTokens.Size.CardPrimaryHeight + 70f, PlaytestText.F("common.level", NumberText.Group(grant.Level)), null, Kit.Pop(since), T.TitleCaps);
            p.Text(PlaytestText.T("milestone.reached"), r.Body.CenterX, r.Body.Top + p.U(20f), T.Body, C.TextSecondary);

            var items = new List<(string Shape, Rgba Color, string Amount, bool Petal)>();
            if (grant.Item != null)
            {
                items.Add(("cosmetic.cap", Rgba.FromHex("#6B5B4E"), PlaytestText.T("wardrobe.kind_hat"), false));
            }

            if (grant.Petals > 0)
            {
                items.Add(("currency.petal", C.PetalFill, NumberText.Plus(grant.Petals), true));
            }

            BoosterGrant? boosters = grant.Boosters;
            if (boosters != null)
            {
                foreach ((string id, int count) in new[] { ("extra_slot", boosters.ExtraSlot), ("shuffle", boosters.Shuffle), ("return", boosters.Return), ("bloom_burst", boosters.BloomBurst) })
                {
                    if (count > 0)
                    {
                        items.Add(("booster." + id, DesignTokens.BoosterColor(id), "+" + count, false));
                    }
                }
            }

            var row = new Box(r.Body.Left, r.Body.Top + p.U(70f), r.Body.Right, r.Body.Top + p.U(370f));
            Box[] cells = ScreenLayout.Row(row, Math.Max(1, items.Count), p.U(24f), p.U(230f), square: false);
            for (int i = 0; i < items.Count; i++)
            {
                (string shape, Rgba color, string amount, bool petal) = items[i];
                Box cell = cells[i];
                float icon = Math.Min(cell.Width * 0.7f, cell.Height * 0.6f);
                Box iconBox = Box.FromCenter(cell.CenterX, cell.Top + (icon / 2f) + p.U(10f), icon, icon);
                if (petal)
                {
                    Kit.Petal(p, iconBox);
                }
                else if (shape.StartsWith("booster.", StringComparison.Ordinal))
                {
                    p.FillCircle(iconBox.CenterX, iconBox.CenterY, icon / 2f, color);
                    p.Shape(shape, iconBox.Inset(icon * 0.22f), Rgba.White);
                }
                else
                {
                    p.Shape(shape, iconBox, color);
                }

                p.Text(amount, cell.CenterX, iconBox.Bottom + p.U(40f), T.Count, C.GardenLabelPlain, cell.Width);
            }

            Box go = ScreenLayout.CardButton(r.Body, r.Body.Bottom - p.U(DesignTokens.Size.CardPrimaryHeight) - p.U(16f), true, p.Scale);
            Kit.PrimaryButton(p, go, PlaytestText.T("milestone.continue"), s.Next, decorate: true, breathe: true);
            Kit.EndCard(p);
            Confetti(p, s, since);
        }

        /// <summary>
        /// The jam bottom sheet: NO MOVES LEFT, the recovery boosters the player can use with their costs, the Free
        /// rescue once per attempt, and Restart. The board stays visible above it (spec 001 FR-027).
        /// </summary>
        public static void Jam(IPainter p, LevelScreen s, float since)
        {
            var options = new List<(BoosterKind Kind, Recovery Recovery, string Id)>();
            foreach (Recovery recovery in s.Session.EligibleRecoveries())
            {
                foreach ((BoosterKind kind, Recovery r, string id) in LevelScreen.Boosters)
                {
                    if (r == recovery && s.Meta.Economy.IsUnlocked(kind) && s.Meta.Economy.CanAfford(kind))
                    {
                        options.Add((kind, r, id));
                    }
                }
            }

            (BoosterKind Kind, Command Command)? rescue = s.RescueOffer();
            float content = (options.Count > 0 ? 250f + 30f : 0f) + (rescue.HasValue ? DesignTokens.Size.CardPrimaryHeight + 24f : 0f) + DesignTokens.Size.SecondaryHeight + 30f;
            string title = PlaytestText.T(s.Session.Status == LevelStatus.Stuck ? "jam.stuck" : "jam.title");
            SheetRegions sheet = Kit.Sheet(p, content, title, PlaytestText.T("jam.subtitle"), Kit.SheetRise(since));
            float y = sheet.Body.Top;
            if (options.Count > 0)
            {
                var row = new Box(sheet.Body.Left, y, sheet.Body.Right, y + p.U(250f));
                Box[] cells = ScreenLayout.Row(row, options.Count, p.U(22f), p.U(280f), square: false);
                for (int i = 0; i < options.Count; i++)
                {
                    (BoosterKind kind, Recovery recovery, string id) = options[i];
                    Box cell = cells[i];
                    bool pressed = p.Pressed(cell);
                    Box face = Kit.Raised(p, cell, Rgba.White, C.SurfacePanelEdge, p.U(36f), pressed);
                    float icon = p.U(100f);
                    Rgba color = DesignTokens.BoosterColor(id);
                    p.FillCircle(face.CenterX, face.Top + p.U(70f), icon / 2f, color);
                    p.Shape("booster." + id, Box.FromCenter(face.CenterX, face.Top + p.U(70f), icon * 0.56f, icon * 0.56f), id == "bloom_burst" ? C.PetalCenter : Rgba.White);
                    p.Text(BoosterName(kind), face.CenterX, face.Top + p.U(150f), T.Caption, C.GardenLabelPlain, face.Width * 0.92f);
                    int charges = s.Meta.Economy.Charges(kind);
                    if (charges > 0)
                    {
                        p.Text("×" + charges, face.CenterX, face.Top + p.U(198f), T.Count, C.GardenLabelPlain);
                    }
                    else
                    {
                        string price = NumberText.Group(s.Meta.Economy.Price(kind));
                        float w = p.MeasureText(price, T.Count);
                        Kit.Petal(p, Box.FromCenter(face.CenterX - (w / 2f) - p.U(8f), face.Top + p.U(198f), p.U(40f), p.U(40f)));
                        p.Text(price, face.CenterX + p.U(16f), face.Top + p.U(198f), T.Count, C.GardenLabelPlain);
                    }

                    p.Hit(cell, () => s.PressBooster(kind, recovery));
                }

                y = row.Bottom + p.U(30f);
            }

            if (rescue.HasValue)
            {
                Box free = ScreenLayout.CardButton(sheet.Body, y, true, p.Scale);
                Kit.PrimaryButton(p, free, PlaytestText.T("jam.rescue"), s.UseRescue, iconId: "ui.ad");
                y = free.Bottom + p.U(24f);
            }

            Box restart = ScreenLayout.CardButton(sheet.Body, y, false, p.Scale);
            Kit.SecondaryButton(p, restart, PlaytestText.T("common.restart"), s.Restart, "ui.restart");
            Kit.EndSheet(p);
        }

        /// <summary>A demo card shown once before play; a tap anywhere closes it.</summary>
        public static void Demo(IPainter p, LevelScreen s, float since)
        {
            DemoCard demo = s.Demo!;
            float content = (demo.Lines.Count * 70f) + (demo.Variants.Count > 0 ? 260f : 0f) + 60f;
            CardRegions r = Kit.Card(p, content, string.Empty, null, Kit.Pop(since));
            float y = r.Card.Top + p.U(70f);
            foreach (string line in demo.Lines)
            {
                p.Text(line, r.Body.CenterX, y, T.Body, C.GardenLabelPlain, r.Body.Width);
                y += p.U(70f);
            }

            if (demo.Variants.Count > 0)
            {
                float size = p.U(190f);
                Box row = new Box(r.Body.Left, y + p.U(10f), r.Body.Right, y + p.U(10f) + size);
                Box[] cells = ScreenLayout.Row(row, demo.Variants.Count, size * 0.6f, size, square: true);
                for (int i = 0; i < cells.Length; i++)
                {
                    Visuals.VariantTile(p, cells[i], demo.Variants[i]);
                }

                if (demo.ShowIgnore)
                {
                    p.Shape("ui.cross", Box.FromCenter(row.CenterX, row.CenterY, size * 0.45f, size * 0.45f), C.StateDanger);
                }
            }

            p.Text("tap to continue", r.Body.CenterX, r.Card.Bottom - p.U(46f), T.Caption, C.TextSecondary);
            Kit.EndCard(p);
            p.Hit(new Box(0f, 0f, p.Width, p.Height), s.CloseDemo);
        }

        public static string BoosterName(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => PlaytestText.T("booster.extra_slot"),
            BoosterKind.Shuffle => PlaytestText.T("booster.shuffle"),
            BoosterKind.Return => PlaytestText.T("booster.return"),
            _ => PlaytestText.T("booster.bloom_burst"),
        };

        /// <summary>Confetti in the level's variant colors for the first seconds of a win (fx.confetti).</summary>
        private static void Confetti(IPainter p, LevelScreen s, float since)
        {
            if (since > 2.2f)
            {
                return;
            }

            p.Mark("fx.confetti");
            var colors = new List<Rgba>();
            foreach (Core.Definitions.PodDef pod in s.Session.Definition.Pods)
            {
                Rgba c = Visuals.ColorOf(pod.Variant);
                if (!colors.Contains(c))
                {
                    colors.Add(c);
                }
            }

            colors.Add(C.PetalFill);
            colors.Add(C.PetalCenter);
            for (int i = 0; i < 36; i++)
            {
                float seed = (i * 0.6180339f) % 1f;
                float x = p.Width * ((seed + (0.05f * (float)Math.Sin((since * 2f) + i))) % 1f);
                float y = (-p.U(40f)) + ((since * (p.Height * (0.35f + (0.25f * ((i * 0.37f) % 1f))))) % (p.Height + p.U(80f)));
                float size = p.U(14f + (8f * ((i * 0.53f) % 1f)));
                p.PushAlpha(Visuals.Clamp01(2.2f - since));
                p.FillRound(Box.FromCenter(x, y, size, size * 0.6f), p.U(3f), colors[i % colors.Count]);
                p.PopAlpha();
            }
        }
    }
}
