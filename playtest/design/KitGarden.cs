using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The reference look's garden pieces (spec 005 <c>specs/005-reference-look/contracts/look.md</c> §3): materials drawn
    /// as cached <see cref="UiRaster"/> pictures, candy tiles, wooden signs, jam choices, the stone furniture of the board
    /// and the win, pods, Waiting Slots, booster tiles, decorations and the wooden wordmark. Each piece marks the asset
    /// slots it draws; screens only call them.
    /// </summary>
    public static partial class Kit
    {
        // ---- Materials (§2) ----

        /// <summary>
        /// A wooden plank picture filling <paramref name="box"/> (<c>mat.wood.light</c> or <c>mat.wood.dark</c>): its corner
        /// radius is <paramref name="radiusShare"/> of its height, its outline <paramref name="outlineShare"/> of it (at
        /// least 2 px) and its slightly deeper bottom band <paramref name="lipShare"/> of it (the button rim passes less);
        /// <paramref name="seed"/> picks the grain.
        /// </summary>
        public static void WoodPlank(IPainter p, Box box, float radiusShare, int seed, WoodTone tone = WoodTone.Light, float outlineShare = 0.025f, float lipShare = UiRaster.PlankLip)
        {
            p.Mark(tone == WoodTone.Light ? "mat.wood.light" : "mat.wood.dark");
            string key = "mat.wood." + (tone == WoodTone.Light ? "light" : "dark") + "/plank/" + Share(radiusShare) + "/" + Share(outlineShare) + "/" + Share(lipShare) + "/" + seed;
            p.Picture(key, box, (w, h) => UiRaster.Plank(w, h, h * radiusShare, Math.Max(2f, h * outlineShare), tone, seed, lipShare));
        }

        /// <summary>A stone block picture filling <paramref name="box"/> (<c>mat.stone</c>), its corners rounded by <paramref name="radiusShare"/> of its shorter side.</summary>
        public static void StoneBlock(IPainter p, Box box, int seed, float radiusShare = 0.3f)
        {
            p.Mark("mat.stone");
            p.Picture("mat.stone/block/" + Share(radiusShare) + "/" + seed, box, (w, h) =>
            {
                float side = Math.Min(w, h);
                return UiRaster.Stone(w, h, side * radiusShare, Math.Max(1f, side * 0.05f), seed);
            });
        }

        // ---- Candy tiles (§3.1) ----

        /// <summary>
        /// A variant's candy tile (§3.1) in the largest square inside <paramref name="box"/>: the board style (a small
        /// raised bead of the symbol) or the sticker style (pods, slots, the jam row). A null <paramref name="variant"/> is a mystery tile.
        /// <paramref name="pressed"/> sinks the face into its lip.
        /// </summary>
        public static void CandyTile(IPainter p, Box box, VariantId? variant, TileStyle style, TileState state = TileState.Normal, bool pressed = false)
        {
            if (!variant.HasValue || !VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info))
            {
                CandyTile(p, box, C.TileMystery, "mystery", style, TileState.Mystery, pressed);
                return;
            }

            CandyTile(p, box, Visuals.ColorOf(variant.Value), info.IconId, style, state, pressed);
        }

        /// <summary>A candy tile of any color and variant icon (the win picture draws its roles' colors this way).</summary>
        public static void CandyTile(IPainter p, Box box, Rgba color, string iconId, TileStyle style, TileState state = TileState.Normal, bool pressed = false)
        {
            p.Mark(style == TileStyle.Board ? "tile.candy" : "tile.candy.sticker");
            p.Mark(state == TileState.Mystery ? "tile.mystery" : ShapeLibrary.SymbolId(iconId));
            float s = Math.Min(box.Width, box.Height);
            if (s < 1f)
            {
                return;
            }

            Box square = Box.FromCenter(box.CenterX, box.CenterY, s, s);
            string key = "tile.candy/" + (style == TileStyle.Board ? "board" : "sticker") + "/" + state + "/" + iconId + "/" + color.Hex;
            Func<int, int, byte[]> render = (w, h) => UiRaster.Tile(Math.Min(w, h), color, iconId, style, state);
            if (!pressed)
            {
                p.Picture(key, square, render);
                return;
            }

            // Pressed: the lower part of the lip stays, the face slides down over the rest of it.
            float lip = s * UiRaster.TileLipShare(style);
            float sink = lip * 0.7f;
            p.PushClip(new Box(square.Left, square.Bottom - lip + sink, square.Right, square.Bottom));
            p.Picture(key, square, render);
            p.PopClip();
            p.PushClip(new Box(square.Left, square.Top + sink, square.Right, square.Bottom - lip + sink));
            p.Picture(key, square.Offset(0f, sink), render);
            p.PopClip();
        }

        // ---- Wooden signs (§3.2) ----

        /// <summary>
        /// A wooden sign (§3.2; the gameplay level, the win and banner titles, the Home level plaque): a light wood plank
        /// filling <paramref name="box"/> (radius 28% of its height) over a soft shadow, the text centered in
        /// <c>ink.brown</c> (or <paramref name="letters"/>) with a light emboss, at most 82% of the plank wide, and its
        /// decoration. Never a touch target.
        /// </summary>
        public static void WoodSign(IPainter p, Box box, string text, TypeStyle style, SignDecor decor = SignDecor.None, Rgba? letters = null)
        {
            p.Mark("ui.sign.wood");
            float h = box.Height;
            SoftShadow(p, box, h * 0.28f, 0.22f, 0.07f);
            if (decor == SignDecor.Ivy)
            {
                // The back leaves of each cluster hang behind the plank's ends.
                IvyCluster(p, IvyBox(box, left: true), flipped: false, back: true);
                IvyCluster(p, IvyBox(box, left: false), flipped: true, back: true);
            }

            WoodPlank(p, box, 0.28f, 7);
            float scale = Math.Min(1f, (h * 0.62f) / Math.Max(1f, p.U(style.Size)));
            p.Text(text, box.CenterX, box.CenterY - (h * 0.04f), style, letters ?? C.InkBrown, box.Width * 0.82f, scale, GardenLook.SignLetters(letters ?? C.InkBrown));
            switch (decor)
            {
                case SignDecor.Ivy:
                    IvyCluster(p, IvyBox(box, left: true), flipped: false, back: false);
                    IvyCluster(p, IvyBox(box, left: false), flipped: true, back: false);
                    break;
                case SignDecor.Flowers:
                    p.Mark("ui.sign.flowers");
                    float size = h * 1.35f;
                    FlowerCluster(p, Box.FromCenter(box.Left + (h * 0.1f), box.Top + (h * 0.08f), size, size), flipped: false);
                    FlowerCluster(p, Box.FromCenter(box.Right - (h * 0.08f), box.Bottom - (h * 0.04f), size * 0.92f, size * 0.92f), flipped: true);
                    break;
            }
        }

        private static Box IvyBox(Box sign, bool left)
        {
            float size = sign.Height * 1.25f;
            float x = left ? sign.Left + (sign.Height * 0.06f) : sign.Right - (sign.Height * 0.06f);
            return Box.FromCenter(x, sign.CenterY, size, size);
        }

        /// <summary>
        /// A cluster of clover leaves (§3.9, <c>ui.sign.ivy</c>) gathered at the top and bottom corners of a sign's end:
        /// pointed leaflets in yellow-green <c>ivy.leaf</c> shades over dark green <c>ivy.line</c> outlines (the shadows
        /// between them), each with a light spot and its midribs, mirrored when <paramref name="flipped"/>.
        /// <paramref name="back"/> draws only the back half of the leaves (behind a sign), false only the front half; null
        /// draws all.
        /// </summary>
        public static void IvyCluster(IPainter p, Box box, bool flipped, bool? back = null)
        {
            p.Mark("ui.sign.ivy");
            string side = flipped ? "/r" : "/l";
            for (int i = 0; i < ShapeLibrary.IvyLeafCount; i++)
            {
                bool behind = i % 2 == 1;
                if (back.HasValue && back.Value != behind)
                {
                    continue;
                }

                Func<float, float, float> leaf = ShapeLibrary.IvyLeafSdf(i, 0f, flipped);
                p.ShapeOf("ui.sign.ivy/" + i + side + "/line", ShapeLibrary.IvyLeafSdf(i, 0.055f, flipped), box, C.IvyLine);
                p.ShapeOf("ui.sign.ivy/" + i + side, leaf, box, GardenLook.IvyShade(i));
                p.ShapeOf("ui.sign.ivy/" + i + side + "/light", (x, y) => Math.Max(leaf(x + 0.05f, y - 0.06f) + 0.07f, leaf(x, y) + 0.02f), box, C.IvyLeaf.Lighten(0.45f).WithAlpha(0.45f));
                Func<float, float, float> veins = ShapeLibrary.IvyVeinSdf(i, 0.018f, flipped);
                p.ShapeOf("ui.sign.ivy/" + i + side + "/vein", (x, y) => Math.Max(veins(x, y), leaf(x, y) + 0.03f), box, C.IvyLine.WithAlpha(0.5f));
            }
        }

        /// <summary>
        /// A lush cluster for the win sign's ends (§3.9; <c>ui.deco.garden</c>): five big almond leaves in three greens
        /// with <c>ivy.line</c> midribs, fanned up, left and down from the cluster's base, and two white five-petal flowers
        /// with yellow middles over them; turned half way when <paramref name="flipped"/>.
        /// </summary>
        public static void FlowerCluster(IPainter p, Box box, bool flipped)
        {
            p.Mark("ui.deco.garden");
            string side = flipped ? "/flipped" : string.Empty;
            Rgba[] greens = { C.GardenLeaf1, C.GardenLeaf3, C.GardenLeaf2 };
            for (int i = 0; i < ShapeLibrary.FlowerClusterLeafCount; i++)
            {
                string key = "ui.deco.garden/cluster/leaf" + i + side;
                Func<float, float, float> leaf = ShapeLibrary.ClusterLeafSdf(i, 0f, flipped);
                Func<float, float, float> vein = ShapeLibrary.ClusterVeinSdf(i, 0.022f, flipped);
                p.ShapeOf(key + "/line", ShapeLibrary.ClusterLeafSdf(i, 0.04f, flipped), box, C.GardenLeafLine);
                p.ShapeOf(key, leaf, box, greens[i % greens.Length]);
                p.ShapeOf(key + "/light", (x, y) => Math.Max(leaf(x + 0.05f, y - 0.06f) + 0.08f, leaf(x, y) + 0.03f), box, C.GardenLeaf2.Lighten(0.35f).WithAlpha(0.4f));
                p.ShapeOf(key + "/vein", (x, y) => Math.Max(vein(x, y), leaf(x, y) + 0.04f), box, C.IvyLine.WithAlpha(0.55f));
            }

            for (int i = 0; i < ShapeLibrary.FlowerClusterFlowerCount; i++)
            {
                string key = "ui.deco.garden/cluster/flower" + i + side;
                p.ShapeOf(key + "/line", ShapeLibrary.ClusterFlowerSdf(i, false, 0.035f, flipped), box, C.GardenFlowerLine);
                p.ShapeOf(key, ShapeLibrary.ClusterFlowerSdf(i, false, 0f, flipped), box, C.GardenFlower);
                p.ShapeOf(key + "/center/line", ShapeLibrary.ClusterFlowerSdf(i, true, 0.025f, flipped), box, C.GardenFlowerCenterLine);
                p.ShapeOf(key + "/center", ShapeLibrary.ClusterFlowerSdf(i, true, 0f, flipped), box, C.GardenFlowerCenter);
            }
        }

        // ---- Jam choices and cost pills (§3.3, §3.4) ----

        /// <summary>
        /// A jam choice (§3.3): a glossy rounded rectangle in <paramref name="set"/> (green or blue, radius 22% of its
        /// height) with the icon (its box half the height) in its upper half, the white outlined label below it, and the cost
        /// pill centered on its bottom edge, overlapping by 40% of the pill's height. <paramref name="box"/> holds the
        /// button and the pill below it; the whole box is the touch target.
        /// </summary>
        public static void ChoiceButton(IPainter p, Box box, ColorSet set, IReadOnlyList<IconPart>? icon, string label, Cost? cost, Action? action)
        {
            p.Mark("ui.button.choice");
            bool enabled = action != null;
            ColorSet colors = enabled ? set : set.Disabled();
            const float pillShare = 0.3f;
            float buttonHeight = cost.HasValue ? box.Height / (1f + (0.6f * pillShare)) : box.Height;
            var button = new Box(box.Left, box.Top, box.Right, box.Top + buttonHeight);
            float depth = Press(p, box, enabled);
            p.PushAlpha(enabled ? 1f : 0.55f);
            Squash(p, button, depth);
            float radius = buttonHeight * 0.22f;
            SoftShadow(p, button, radius, 0.24f, 0.05f);
            Box f = Face(p, button, colors, radius, depth, enabled, (buttonHeight / p.Scale) * 0.075f, gloss: true);
            // As measured on the reference's jam card: the icon about 44% of the face (its box a little more, for the
            // shapes' margins), the label's letters about 21% of the button's height.
            float iconSize = buttonHeight * 0.5f;
            if (icon != null)
            {
                IconParts(p, Box.FromCenter(f.CenterX, f.Top + (f.Height * 0.36f), iconSize, iconSize), icon, grey: !enabled);
            }

            float labelY = icon != null ? f.Top + (f.Height * 0.78f) : f.CenterY;
            float scale = Math.Min(1f, (buttonHeight * 0.21f) / p.U(T.ButtonSecondary.Size));
            p.Text(label, f.CenterX, labelY, T.ButtonSecondary, C.TextOnColor, f.Width * 0.88f, scale, TextLook.OnGloss(colors));
            p.PopTransform();
            if (cost.HasValue)
            {
                float pillHeight = buttonHeight * pillShare;
                CostPill(p, Box.FromCenter(button.CenterX, button.Bottom + (pillHeight * 0.1f), button.Width * 0.64f, pillHeight), cost.Value);
            }

            p.PopAlpha();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        // ---- Board furniture (§3.6) ----

        /// <summary>
        /// The stone border around a board's grid (§3.6): a dark gap of 0.04 cell around <paramref name="grid"/>, then
        /// blocks of stone <paramref name="thickness"/> cells thick (0.42 on the board, 0.3 around the win picture) whose
        /// lengths alternate 1.0 and 0.8 cell, square rounded blocks at the corners, and dark joints between them. Draw it
        /// before the tiles: the gap shows between them as their thin dark separation. Seeds follow the block's place, so
        /// the border never flickers.
        /// </summary>
        public static void StoneBorder(IPainter p, Box grid, float cell, float thickness = 0.42f)
        {
            p.Mark("board.border.stone");
            float gap = cell * 0.04f;
            float t = cell * thickness;
            float joint = cell * 0.04f;
            Box inner = grid.Inset(-gap);
            Box outer = inner.Inset(-t);
            p.FillRound(outer.Offset(0f, t * 0.22f).Inset(-t * 0.05f, 0f), t * 0.5f, C.GardenShadow.WithAlpha(0.22f));
            p.FillRound(outer.Inset(joint * 0.5f), t * 0.4f, C.StoneLine.WithAlpha(0.6f));
            p.FillRound(inner, gap * 2f, GardenLook.BoardGap);

            // The corners, then each side's run of blocks between them.
            float half = joint / 2f;
            StoneBlock(p, new Box(outer.Left, outer.Top, inner.Left, inner.Top).Inset(half), 1, 0.4f);
            StoneBlock(p, new Box(inner.Right, outer.Top, outer.Right, inner.Top).Inset(half), 2, 0.4f);
            StoneBlock(p, new Box(outer.Left, inner.Bottom, inner.Left, outer.Bottom).Inset(half), 3, 0.4f);
            StoneBlock(p, new Box(inner.Right, inner.Bottom, outer.Right, outer.Bottom).Inset(half), 4, 0.4f);
            StoneRun(p, inner.Left, inner.Right, cell, 0, (a, b) => new Box(a, outer.Top, b, inner.Top), half);
            StoneRun(p, inner.Left, inner.Right, cell, 1, (a, b) => new Box(a, inner.Bottom, b, outer.Bottom), half);
            StoneRun(p, inner.Top, inner.Bottom, cell, 2, (a, b) => new Box(outer.Left, a, inner.Left, b), half);
            StoneRun(p, inner.Top, inner.Bottom, cell, 3, (a, b) => new Box(inner.Right, a, outer.Right, b), half);
        }

        /// <summary>One side of the border: blocks of 1.0 and 0.8 cell, stretched together to fill the side exactly.</summary>
        private static void StoneRun(IPainter p, float from, float to, float cell, int side, Func<float, float, Box> place, float half)
        {
            float length = to - from;
            int count = Math.Max(1, (int)Math.Round(length / (cell * 0.9f)));
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                total += Pattern(i, side);
            }

            float k = length / total;
            float at = from;
            for (int i = 0; i < count; i++)
            {
                float next = i == count - 1 ? to : at + (Pattern(i, side) * k);
                // A few stone looks, chosen by the block's place, keep the picture cache small.
                int seed = 11 + (((side * 7) + (i * 3)) % 8);
                StoneBlock(p, place(at, next).Inset(half), seed, 0.3f);
                at = next;
            }
        }

        private static float Pattern(int i, int side) => ((i + side) % 2) == 0 ? 1f : 0.8f;

        /// <summary>
        /// A Garden Entry's stone arch (§3.6, <c>board.arch</c>): a big half ring of nine sandy stone blocks (outer radius
        /// 1.5 cells, so it is three cells wide) around an opening that shows the lawn, where the walkers stand.
        /// (<paramref name="cx"/>, <paramref name="cy"/>) is the middle of its open base, where the
        /// Bloomlings come out; its crown points to the board, which lies beyond the <paramref name="side"/> the entry is
        /// on (for <see cref="EntrySide.Bottom"/> the arch stands below the board, crown up, as in the reference).
        /// </summary>
        public static void StoneArch(IPainter p, float cx, float cy, float cell, EntrySide side)
        {
            p.Mark("board.arch");
            p.Mark("mat.stone");
            float r = cell * 1.5f;
            (int turns, Box box) = side switch
            {
                EntrySide.Left => (1, new Box(cx, cy - r, cx + r, cy + r)),
                EntrySide.Top => (2, new Box(cx - r, cy, cx + r, cy + r)),
                EntrySide.Right => (3, new Box(cx - r, cy - r, cx, cy + r)),
                _ => (0, new Box(cx - r, cy - r, cx + r, cy)),
            };
            p.Picture("board.arch/" + turns, box, (w, h) => UiRaster.Arch(w, h, turns, 5));
        }

        /// <summary>
        /// The stone pedestal the heroes stand on (§3.6, <c>ui.pedestal</c>; win, milestone, Home, Wardrobe): an
        /// ellipse-topped stone drum filling <paramref name="box"/> over a soft ground shadow. Returns the top ellipse's
        /// box, where the heroes' feet go.
        /// </summary>
        public static Box StonePedestal(IPainter p, Box box)
        {
            p.Mark("ui.pedestal");
            p.Mark("mat.stone");
            p.PushSquash(1f, 0.22f, box.CenterX, box.Bottom);
            p.FillCircle(box.CenterX, box.Bottom, box.Width * 0.56f, C.GardenShadow.WithAlpha(0.22f));
            p.PopTransform();
            p.Picture("ui.pedestal/5", box, (w, h) => UiRaster.Pedestal(w, h, 5));
            float rx = (box.Width / 2f) - 1f;
            float ry = Math.Max(1f, Math.Min(rx * 0.28f, (box.Height - 4f) * 0.3f));
            return new Box(box.Left, box.Top + 1f, box.Right, box.Top + 1f + (2f * ry));
        }

        // ---- Tray pieces (§3.7) ----

        /// <summary>
        /// A pod's wooden frame (§3.7, <c>mat.wood.dark</c>): a soft shadow, the short wooden handle on top
        /// (<paramref name="handle"/>, exposed pods only), the inner panel and the dark wood frame (radius 18%, border 11% of
        /// the width) over it. The panel is pale <paramref name="tint"/> (the variant's color) fading to cream at the
        /// bottom, plain cream without a tint, <c>state.lock_bg</c> when locked. A queued pod (<see cref="PodLook.Next"/>)
        /// is dimmed toward <c>parchment.bottom</c>; a pressed one sinks a little. Returns the inner panel, where the tile
        /// and the count go.
        /// </summary>
        public static Box PodFrame(IPainter p, Box box, PodLook look, bool handle = true, Rgba? tint = null)
        {
            p.Mark("mat.wood.dark");
            float w = box.Width;
            bool queued = look == PodLook.Next;
            Box frame = look == PodLook.Pressed ? box.Offset(0f, w * 0.035f) : box;
            float radius = w * 0.18f;
            float border = w * 0.11f;
            p.FillRound(frame.Offset(0f, w * (look == PodLook.Pressed ? 0.02f : 0.05f)).Inset(w * 0.03f, 0f), radius, C.GardenShadow.WithAlpha(queued ? 0.12f : 0.26f));
            if (handle && !queued)
            {
                // A short wooden handle on the frame's top edge, with a small stem.
                float hw = w * 0.34f;
                float hh = w * 0.15f;
                Box knob = Box.FromCenter(frame.CenterX, frame.Top - (hh * 0.12f), hw, hh);
                p.FillRound(Box.FromCenter(knob.CenterX, knob.Top - (hh * 0.12f), w * 0.04f, hh * 0.45f), w * 0.02f, C.WoodDarkLine);
                WoodPlank(p, knob, 0.5f, 9, WoodTone.Dark);
            }

            Box panel = frame.Inset(border * 0.8f);
            float panelRadius = Math.Max(0f, radius - (border * 0.8f));
            if (look == PodLook.Locked)
            {
                p.FillRoundGradient(panel, panelRadius, C.StateLockBg.Lighten(0.2f), C.StateLockBg);
            }
            else if (tint.HasValue)
            {
                p.FillRoundGradient(panel, panelRadius, tint.Value.Mix(C.CreamTop, 0.78f), C.CreamFace);
            }
            else
            {
                p.FillRoundGradient(panel, panelRadius, C.CreamTop, C.CreamFace);
            }

            p.Picture("mat.wood.dark/frame/4", frame, (pw, ph) => UiRaster.Frame(pw, ph, pw * 0.18f, pw * 0.11f, WoodTone.Dark, 4));
            if (queued)
            {
                p.FillRound(frame, radius, C.ParchmentBottom.WithAlpha(0.45f));
            }

            return frame.Inset(border);
        }

        /// <summary>
        /// A whole pod (§3.7): the wooden frame with its panel tinted by the variant, the variant's sticker tile at 70% of
        /// the panel near its top, and the count below it in <c>type.count</c> <c>ink.brown</c> with no "x". A null
        /// <paramref name="variant"/> is a mystery pod (the lilac "?" tile); a locked pod shows the padlock; a queued one is
        /// dimmed; a pressed one sinks. Queued, locked and mystery pods keep the plain cream panel.
        /// </summary>
        public static void Pod(IPainter p, Box box, VariantId? variant, int count, PodLook look, bool handle = true)
        {
            p.Mark("pod.card");
            bool queued = look == PodLook.Next;
            Rgba? tint = variant.HasValue && !queued && look != PodLook.Locked ? Visuals.ColorOf(variant.Value) : (Rgba?)null;
            Box panel = PodFrame(p, box, look, handle, tint);
            float tile = panel.Width * 0.7f;
            Box tileBox = Box.FromCenter(panel.CenterX, panel.Top + (panel.Height * 0.04f) + (tile / 2f), tile, tile);
            if (look == PodLook.Locked)
            {
                p.Mark("pod.state.locked");
                float g = tile * 0.62f;
                p.Shape("ui.lock", Box.FromCenter(tileBox.CenterX, tileBox.CenterY, g, g), C.StateLock.Darken(0.2f));
            }
            else
            {
                if (!variant.HasValue)
                {
                    p.Mark("pod.state.mystery");
                }

                CandyTile(p, tileBox, variant, TileStyle.Sticker, queued ? TileState.Dimmed : TileState.Normal);
            }

            CountBelow(p, new Box(panel.Left, tileBox.Bottom, panel.Right, panel.Bottom), count, queued || look == PodLook.Locked);
        }

        /// <summary>A pod's or slot's count under its tile (§3.7): plain digits in <c>ink.brown</c>, softer when dimmed.</summary>
        public static void CountBelow(IPainter p, Box area, int count, bool dim)
        {
            p.Mark("pod.count");
            Rgba ink = dim ? C.InkBrownSoft.Mix(C.ParchmentBottom, 0.3f) : C.InkBrown;
            float scale = (area.Height * 0.86f) / p.U(T.Count.Size);
            p.Text(count.ToString(CultureInfo.InvariantCulture), area.CenterX, area.CenterY, T.Count, ink, area.Width, scale, TextLook.Plain(ink));
        }

        /// <summary>
        /// A Waiting Slot (§3.7): a raised cream plate (radius 20%) with the sticker tile at 70% of its width and the count
        /// below while a pod works; the grey tile with the hourglass badge while it is stuck; a slightly sunk face with a
        /// dashed inner outline when empty (in <c>state.danger</c> with "!" for the last free slot); a grey face with the
        /// padlock when locked. <paramref name="extra"/> adds the green "+" badge of the Extra Slot booster. A null
        /// <paramref name="variant"/> in a filled slot is a mystery pod. Returns the plate's face.
        /// </summary>
        public static Box SlotPlate(IPainter p, Box box, SlotPlateState state, VariantId? variant = null, int count = 0, bool extra = false)
        {
            float s = Math.Min(box.Width, box.Height);
            float radius = s * 0.2f;
            float line = Math.Max(1f, s * 0.018f);
            Box face;
            if (state == SlotPlateState.Empty || state == SlotPlateState.Danger)
            {
                p.Mark("slot.empty");
                p.FillRound(box, radius, C.CreamFace.Mix(C.ParchmentWell, 0.35f));
                p.PushClip(box);
                p.FillRoundGradient(new Box(box.Left, box.Top, box.Right, box.Top + (s * 0.3f)), radius, C.GardenShadow.WithAlpha(0.1f), C.GardenShadow.WithAlpha(0f));
                p.PopClip();
                p.StrokeRound(box.Inset(line / 2f), radius - (line / 2f), line, C.CreamLine.WithAlpha(0.35f));
                bool danger = state == SlotPlateState.Danger;
                Box dashed = box.Inset(s * 0.09f);
                float dash = Math.Max(1.5f, s * 0.028f);
                p.StrokeRound(dashed, radius * 0.7f, dash, danger ? C.StateDanger : C.CreamLine.WithAlpha(0.8f), s * 0.09f, s * 0.06f);
                if (danger)
                {
                    p.Mark("slot.state.danger");
                    p.FillRound(dashed, radius * 0.7f, C.StateDanger.WithAlpha(0.07f));
                    p.Shape("slot.state.jam_risk", box.Inset(s * 0.32f), C.StateDanger);
                }

                face = box;
            }
            else
            {
                bool locked = state == SlotPlateState.Locked;
                float lip = s * 0.07f;
                SoftShadow(p, box, radius, 0.18f, 0.04f);
                p.FillRound(box, radius, locked ? C.StateLockBg.Darken(0.15f) : C.CreamLip);
                face = new Box(box.Left, box.Top, box.Right, box.Bottom - lip);
                p.FillRoundGradient(face, radius, locked ? C.StateLockBg.Lighten(0.25f) : C.CreamTop, locked ? C.StateLockBg : C.CreamFace);
                p.StrokeRound(box.Inset(line / 2f), radius - (line / 2f), line, locked ? C.StateLock : C.CreamLine);
                if (locked)
                {
                    p.Mark("slot.state.locked");
                    float g = s * 0.44f;
                    p.Shape("ui.lock", Box.FromCenter(face.CenterX, face.CenterY, g, g), C.StateLock.Darken(0.15f));
                }
                else
                {
                    bool stuck = state == SlotPlateState.Stuck;
                    p.Mark(stuck ? "slot.state.stuck" : "slot.state.working");
                    if (!variant.HasValue)
                    {
                        p.Mark("pod.state.mystery");
                    }

                    float tile = face.Width * 0.64f;
                    Box tileBox = Box.FromCenter(face.CenterX, face.Top + (face.Height * 0.06f) + (tile / 2f), tile, tile);
                    CandyTile(p, tileBox, variant, TileStyle.Sticker, stuck && variant.HasValue ? TileState.Grey : TileState.Normal);
                    CountBelow(p, new Box(face.Left, tileBox.Bottom, face.Right, face.Bottom - (face.Height * 0.02f)), count, stuck);
                    if (stuck && count > 0)
                    {
                        float b = s * 0.17f;
                        float bx = box.Right - (b * 0.55f);
                        float by = box.Top + (b * 0.55f);
                        p.FillCircle(bx, by + (b * 0.12f), b + Math.Max(1f, s * 0.012f), C.GardenShadow.WithAlpha(0.2f));
                        p.FillCircle(bx, by, b + Math.Max(1f, s * 0.012f), C.CreamLine);
                        p.FillCircle(bx, by, b, Rgba.White);
                        p.Shape("slot.state.waiting", Box.FromCenter(bx, by, b * 1.3f, b * 1.3f), C.InkBrownSoft);
                    }
                }
            }

            if (extra)
            {
                p.Mark("slot.extra");
                float r = s * 0.17f;
                float ex = box.Left + (r * 0.55f);
                float ey = box.Top + (r * 0.55f);
                p.FillCircle(ex, ey, r + Math.Max(1f, s * 0.02f), Rgba.White);
                p.FillCircle(ex, ey, r, GardenLook.Green.Face);
                p.Shape("ui.plus", Box.FromCenter(ex, ey, r * 1.2f, r * 1.2f), C.TextOnColor);
            }

            return face;
        }

        /// <summary>
        /// A booster tile (§3.7; the booster bar): a cream squircle (radius 26%) in a grey-beige rim with the booster's
        /// colored icon at 62%, and the count badge over its bottom-right corner, or the cost pill under it and a small
        /// green "+" when no charges are left. A selected tile is raised with the pulsing golden glow; a disabled one is
        /// greyed (spec 003 FR-031).
        /// </summary>
        public static void BoosterTile(IPainter p, Box box, string boosterId, BoosterTileState state, Action? action)
        {
            float s = Math.Min(box.Width, box.Height);
            float depth = Press(p, box, action != null);
            Box tile = state.Selected ? box.Offset(0f, -s * 0.08f) : box;
            p.PushAlpha(state.Disabled ? 0.55f : 1f);
            float radius = s * 0.26f;
            if (state.Selected)
            {
                // The selected glow: a golden halo pulsing every motion.glow, and a ring around the tile.
                float glow = GardenLook.Glow(p.Now);
                for (int ring = 3; ring >= 1; ring--)
                {
                    float grow = s * 0.18f * ring / 3f;
                    p.FillRound(tile.Inset(-grow), radius + grow, C.GardenGlow.WithAlpha(0.22f * glow));
                }

                p.StrokeRound(tile.Inset(-s * 0.035f), radius + (s * 0.035f), s * 0.035f, C.GardenGlow.WithAlpha(glow));
            }

            Squash(p, tile, depth, tile: true);
            Rgba rim = GardenLook.BoosterRim;
            var set = new ColorSet("set.cream.booster_tile", C.CreamFace, rim.Lighten(0.62f), rim.Lighten(0.08f), rim.Darken(0.22f));
            Box f = IconFace(p, tile, state.Disabled ? set.Disabled() : set, radius, depth);
            float icon = s * 0.62f;
            BoosterIcon(p, boosterId, Box.FromCenter(f.CenterX, f.CenterY, icon, icon), grey: state.Disabled);
            p.PopTransform();

            if (state.ShowsCharges)
            {
                float badge = s * 0.34f;
                CountBadge(p, tile.Right - (badge / 6f), tile.Bottom - (badge / 6f), badge, state.Charges.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                float pill = s * 0.3f;
                CostPill(p, Box.FromCenter(tile.CenterX, tile.Bottom + (pill * 0.12f), s * 0.86f, pill), Cost.Petals(state.Price));
                float plus = s * 0.3f;
                float px = tile.Right - (plus * 0.3f);
                float py = tile.Top + (plus * 0.3f);
                p.FillCircle(px, py + (plus * 0.07f), (plus / 2f) + Math.Max(1f, s * 0.012f), GardenLook.Green.Line);
                p.FillCircle(px, py, plus / 2f, GardenLook.Green.Face);
                p.Shape("ui.plus", Box.FromCenter(px, py, plus * 0.62f, plus * 0.62f), Rgba.White);
            }

            p.PopAlpha();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>A booster's colored icon (§3.8, <see cref="GardenLook.BoosterIcon"/>), all grey when <paramref name="grey"/>.</summary>
        public static void BoosterIcon(IPainter p, string boosterId, Box box, bool grey = false)
        {
            p.Mark("booster." + boosterId);
            IconParts(p, box, GardenLook.BoosterIcon(boosterId), grey);
        }

        // ---- Celebration (§3.9) ----

        /// <summary>
        /// Light rays behind the celebrating heroes (§3.9, <c>fx.rays</c>): ten soft <c>ray.light</c> wedges from
        /// (<paramref name="cx"/>, <paramref name="cy"/>), turning 0.05 turn per second, brightest at the center.
        /// <paramref name="seconds"/> is the time since the win.
        /// </summary>
        public static void LightRays(IPainter p, float cx, float cy, float radius, float seconds)
        {
            p.Mark("fx.rays");
            double turn = seconds * 0.05 * 2.0 * Math.PI;
            p.FillCircle(cx, cy, radius * 0.42f, C.RayLight.WithAlpha(0.18f));
            p.FillCircle(cx, cy, radius * 0.24f, C.RayLight.WithAlpha(0.22f));
            for (int i = 0; i < 10; i++)
            {
                double a = turn + (i * Math.PI / 5.0);
                float wide = i % 2 == 0 ? 1f : 0.7f;
                for (int j = -2; j <= 2; j++)
                {
                    double aj = a + (j * 0.034 * wide);
                    float x = (float)Math.Cos(aj);
                    float y = (float)Math.Sin(aj);
                    p.Line(cx, cy, cx + (x * radius), cy + (y * radius), radius * 0.05f * wide, C.RayLight.WithAlpha(0.06f));
                    p.Line(cx, cy, cx + (x * radius * 0.62f), cy + (y * radius * 0.62f), radius * 0.045f * wide, C.RayLight.WithAlpha(0.07f));
                }
            }
        }

        /// <summary>
        /// Ten pink petals drifting down through <paramref name="area"/> and swaying (§3.9, <c>fx.petals</c>), turning over
        /// as they fall. <paramref name="seconds"/> is the time since the win; the same time gives the same frame.
        /// </summary>
        public static void FallingPetals(IPainter p, Box area, float seconds)
        {
            p.Mark("fx.petals");
            for (int i = 0; i < 10; i++)
            {
                float a = Frac(i * 0.6180339f);
                float b = Frac((i * 0.3819660f) + 0.17f);
                float size = p.U(40f) * (0.75f + (0.5f * b));
                float fall = area.Height * (0.1f + (0.07f * b));
                float span = area.Height + (size * 2f);
                float y = area.Top - size + (((a * span) + (seconds * fall)) % span);
                float x = area.Left + (area.Width * (0.05f + (0.9f * Frac(a * 7.31f)))) + ((float)Math.Sin((seconds * 1.3f) + (i * 1.7f)) * area.Width * 0.04f);
                float turn = (float)Math.Cos((seconds * 2.1f) + (i * 1.3f));
                p.PushSquash(Math.Max(0.25f, Math.Abs(turn)), 0.8f + (0.2f * b), x, y);
                p.Shape("fx.petals", Box.FromCenter(x, y, size, size), GardenLook.PetalShade(i));
                p.PopTransform();
            }
        }

        // ---- The wordmark (§4.5) ----

        /// <summary>
        /// The wooden wordmark (§4.5, <c>ui.logo.wood</c>; the stand-in for the owner's logo): <paramref name="text"/> in
        /// <c>type.wordmark</c> fitted into <paramref name="box"/> with light wood letters, a <c>wood.line</c> outline and a
        /// darker extrusion, ivy over both ends and a small pink flower.
        /// </summary>
        public static void WoodLogo(IPainter p, Box box, string text)
        {
            p.Mark("ui.logo.wood");
            TypeStyle style = T.Wordmark;
            float natural = Math.Max(1f, p.MeasureText(text, style));
            float scale = Math.Min(box.Width * 0.86f / natural, box.Height * 0.72f / p.U(style.Size));
            float width = natural * scale;
            float em = p.U(style.Size) * scale;
            float cy = box.CenterY - (em * 0.04f);
            float leaf = em * 1.0f;
            IvyCluster(p, Box.FromCenter(box.CenterX - (width / 2f) + (leaf * 0.05f), cy - (em * 0.12f), leaf, leaf), flipped: false, back: true);
            IvyCluster(p, Box.FromCenter(box.CenterX + (width / 2f) - (leaf * 0.05f), cy - (em * 0.12f), leaf, leaf), flipped: true, back: true);
            p.Text(text, box.CenterX, cy, style, C.WoodLight, sizeScale: scale, look: GardenLook.WoodLetters);
            IvyCluster(p, Box.FromCenter(box.CenterX - (width / 2f) + (leaf * 0.05f), cy - (em * 0.12f), leaf, leaf), flipped: false, back: false);
            IvyCluster(p, Box.FromCenter(box.CenterX + (width / 2f) - (leaf * 0.05f), cy - (em * 0.12f), leaf, leaf), flipped: true, back: false);
            (Rgba petals, Rgba line, Rgba center) = GardenLook.PinkFlower;
            float flower = em * 0.4f;
            Box bloom = Box.FromCenter(box.CenterX + (width / 2f) + (flower * 0.1f), cy - (em * 0.46f), flower, flower);
            Func<float, float, float> sdf = ShapeLibrary.Get("fx.petal_burst");
            p.ShapeOf("ui.logo.wood/flower/line", (x, y) => sdf(x, y) - 0.07f, bloom, line);
            p.Shape("fx.petal_burst", bloom, petals);
            p.FillCircle(bloom.CenterX, bloom.CenterY, flower * 0.13f, center);
        }

        private static float Frac(float v) => v - (float)Math.Floor(v);

        /// <summary>A share as a short cache-key text (0.28 → "0.28").</summary>
        private static string Share(float share) => share.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
