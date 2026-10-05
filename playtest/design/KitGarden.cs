using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.UI.Design;
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
            (string key, Func<int, int, byte[]> render) = Recipe(Planks, (Tone: tone, Radius: radiusShare, Outline: outlineShare, Lip: lipShare, Seed: seed), k =>
                ("mat.wood." + (k.Tone == WoodTone.Light ? "light" : "dark") + "/plank/" + Share(k.Radius) + "/" + Share(k.Outline) + "/" + Share(k.Lip) + "/" + k.Seed,
                    (w, h) => UiRaster.Plank(w, h, h * k.Radius, Math.Max(2f, h * k.Outline), k.Tone, k.Seed, k.Lip)));
            p.Picture(key, box, render);
        }

        /// <summary>A stone block picture filling <paramref name="box"/> (<c>mat.stone</c>), its corners rounded by <paramref name="radiusShare"/> of its shorter side.</summary>
        public static void StoneBlock(IPainter p, Box box, int seed, float radiusShare = 0.3f)
        {
            p.Mark("mat.stone");
            (string key, Func<int, int, byte[]> render) = Recipe(Stones, (Radius: radiusShare, Seed: seed), k =>
                ("mat.stone/block/" + Share(k.Radius) + "/" + k.Seed, (w, h) =>
                {
                    float side = Math.Min(w, h);
                    return UiRaster.Stone(w, h, side * k.Radius, Math.Max(1f, side * 0.035f), k.Seed);
                }));
            p.Picture(key, box, render);
        }

        // Each picture recipe's cache key and render function, made once per recipe instead of on every draw.
        private static readonly Dictionary<(WoodTone Tone, float Radius, float Outline, float Lip, int Seed), (string, Func<int, int, byte[]>)> Planks =
            new Dictionary<(WoodTone, float, float, float, int), (string, Func<int, int, byte[]>)>();

        private static readonly Dictionary<(float Radius, int Seed), (string, Func<int, int, byte[]>)> Stones =
            new Dictionary<(float, int), (string, Func<int, int, byte[]>)>();

        private static readonly Dictionary<(TileStyle Style, TileState State, string Icon, Rgba Color), TileRecipe> Tiles =
            new Dictionary<(TileStyle, TileState, string, Rgba), TileRecipe>();

        private static readonly Dictionary<(bool Flipped, bool? Back), (string, Func<int, int, byte[]>)> Ivies =
            new Dictionary<(bool, bool?), (string, Func<int, int, byte[]>)>();

        private static readonly Func<int, int, byte[]> FlowerClusterPicture = (w, h) => LeafPictures.FlowerCluster(w, h, flipped: false);

        private static readonly Func<int, int, byte[]> FlippedFlowerClusterPicture = (w, h) => LeafPictures.FlowerCluster(w, h, flipped: true);

        private static readonly Func<int, int, byte[]> LogoLeavesPicture = (w, h) => LeafPictures.LogoLeaves(w, h, mirrored: false);

        private static readonly Func<int, int, byte[]> MirroredLogoLeavesPicture = (w, h) => LeafPictures.LogoLeaves(w, h, mirrored: true);

        /// <summary>A picture recipe's key and render function: made by <paramref name="make"/> the first time, then reused.</summary>
        private static TValue Recipe<TKey, TValue>(Dictionary<TKey, TValue> recipes, TKey key, Func<TKey, TValue> make)
            where TKey : notnull
        {
            if (!recipes.TryGetValue(key, out TValue? recipe))
            {
                recipe = make(key);
                recipes[key] = recipe;
            }

            return recipe;
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

        /// <summary>
        /// A candy tile of any color and variant icon (the win picture draws its roles' colors this way). With the owner's
        /// icon picture of <paramref name="iconId"/> embedded (spec 005 pictures.md G9–G24,
        /// <see cref="OwnerPictures.TileIcon"/>), the tile is its drawn face (<see cref="UiRaster.TileFace"/>) with the
        /// picture over it (<see cref="OwnerPictures.TileIconBox"/>): faded on a queued pod, a grey copy on a stuck slot,
        /// sinking with the face when pressed. The mystery tile keeps its "?".
        /// </summary>
        public static void CandyTile(IPainter p, Box box, Rgba color, string iconId, TileStyle style, TileState state = TileState.Normal, bool pressed = false)
        {
            TileRecipe tile = Recipe(Tiles, (Style: style, State: state, Icon: iconId, Color: color), k => new TileRecipe(k.Style, k.State, k.Icon, k.Color));
            bool owner = tile.Picture != null && p.HasSprite(tile.Picture);
            string key = owner ? tile.FaceKey : tile.Key;
            Func<int, int, byte[]> render = owner ? tile.Face : tile.Render;
            p.Mark(style == TileStyle.Sticker ? "tile.candy.sticker" : "tile.candy");
            p.Mark(owner ? tile.PictureSlot! : tile.Slot);
            float s = Math.Min(box.Width, box.Height);
            if (s < 1f)
            {
                return;
            }

            Box square = Box.FromCenter(box.CenterX, box.CenterY, s, s);
            float sink = 0f;
            if (!pressed)
            {
                p.Picture(key, square, render);
            }
            else
            {
                // Pressed: the lower part of the lip stays, the face slides down over the rest of it.
                float lip = s * UiRaster.TileLipShare(style);
                sink = lip * 0.7f;
                p.PushClip(new Box(square.Left, square.Bottom - lip + sink, square.Right, square.Bottom));
                p.Picture(key, square, render);
                p.PopClip();
                p.PushClip(new Box(square.Left, square.Top + sink, square.Right, square.Bottom - lip + sink));
                p.Picture(key, square.Offset(0f, sink), render);
                p.PopClip();
            }

            if (owner)
            {
                // One picture over the face's middle: faded when queued, its grey copy when stuck.
                p.PushAlpha(OwnerPictures.TileIconAlpha(state));
                p.Sprite(state == TileState.Grey ? tile.GreyPicture! : tile.Picture!, OwnerPictures.TileIconBox(square.Offset(0f, sink), style));
                p.PopAlpha();
            }
        }

        /// <summary>
        /// A candy tile's picture recipes, made once per style, state, icon and color: the drawn tile, its face alone for
        /// the owner's icon picture, the picture's painter names (with its grey copy) and the slots each marks.
        /// </summary>
        private sealed class TileRecipe
        {
            public TileRecipe(TileStyle style, TileState state, string icon, Rgba color)
            {
                string kind = style == TileStyle.Board ? "board" : style == TileStyle.Flat ? "flat" : "sticker";
                Key = "tile.candy/" + kind + "/" + state + "/" + icon + "/" + color.Hex;
                Render = (w, h) => UiRaster.Tile(Math.Min(w, h), color, icon, style, state);
                Slot = state == TileState.Mystery ? "tile.mystery" : ShapeLibrary.SymbolId(icon);
                FaceKey = "tile.face/" + kind + "/" + state + "/" + color.Hex;
                Face = (w, h) => UiRaster.TileFace(Math.Min(w, h), color, style, state);
                string? picture = OwnerPictures.TileIcon(icon, style, state);
                if (picture != null)
                {
                    Picture = PainterBase.IconPrefix + picture;
                    GreyPicture = Picture + PainterBase.GreySuffix;
                    PictureSlot = style == TileStyle.Sticker ? OwnerPictures.IconSlot(icon) : OwnerPictures.GemSlot(icon);
                }
            }

            public string Key { get; }

            public Func<int, int, byte[]> Render { get; }

            public string Slot { get; }

            public string FaceKey { get; }

            public Func<int, int, byte[]> Face { get; }

            /// <summary>The owner's icon picture's painter name (<c>icon/variant-leaf</c>), or null for the mystery tile.</summary>
            public string? Picture { get; }

            /// <summary>The painter name of the picture's grey copy (a stuck slot's tile).</summary>
            public string? GreyPicture { get; }

            /// <summary>The slot the owner's picture fills (<c>tile.icon.leaf</c>, <c>tile.gem.leaf</c>).</summary>
            public string? PictureSlot { get; }
        }

        // ---- Wooden signs (§3.2) ----

        /// <summary>
        /// A wooden sign (§3.2; the gameplay level, the win and banner titles, the Home level plaque): a light wood plank
        /// filling <paramref name="box"/> (radius 28% of its height) over a soft shadow, the text centered in
        /// <c>ink.brown</c> (<c>ink.title</c> on the win's flower sign, or <paramref name="letters"/>) with a light emboss,
        /// at most 82% of the plank wide, and its
        /// decoration (the ivy clusters at <paramref name="ivyScale"/> of their size: <see cref="GardenLook.IvyBox"/>).
        /// Never a touch target.
        /// </summary>
        public static void WoodSign(IPainter p, Box box, string text, TypeStyle style, SignDecor decor = SignDecor.None, Rgba? letters = null, float ivyScale = 1f)
        {
            p.Mark("ui.sign.wood");
            float h = box.Height;
            SoftShadow(p, box, h * 0.28f, 0.22f, 0.07f);
            if (decor == SignDecor.Ivy)
            {
                // The back leaves of each cluster hang behind the plank's ends.
                IvyCluster(p, GardenLook.IvyBox(box, left: true, ivyScale), flipped: false, back: true);
                IvyCluster(p, GardenLook.IvyBox(box, left: false, ivyScale), flipped: true, back: true);
            }

            WoodPlank(p, box, 0.28f, 7);
            float scale = Math.Min(1f, (h * 0.62f) / Math.Max(1f, p.U(style.Size)));
            Rgba ink = letters ?? (decor == SignDecor.Flowers ? C.InkTitle : C.InkBrown);

            // The letters keep clear of the leaves on the plank's ends: the owner's ivy cluster reaches 0.585 h into each
            // end (at full size), the flower clusters about 0.64 h.
            float maxWidth = box.Width * 0.82f;
            if (decor == SignDecor.Ivy && p.HasSprite(PainterBase.DecorPrefix + OwnerPictures.Ivy))
            {
                maxWidth = Math.Min(maxWidth, box.Width - (h * GardenLook.IvyShare * ivyScale));
            }
            else if (decor == SignDecor.Flowers)
            {
                maxWidth = Math.Min(maxWidth, box.Width - (h * 1.35f * 0.95f));
            }

            p.Text(text, box.CenterX, box.CenterY - (h * 0.04f), style, ink, Math.Max(h, maxWidth), scale, GardenLook.SignLetters(ink));
            switch (decor)
            {
                case SignDecor.Ivy:
                    IvyCluster(p, GardenLook.IvyBox(box, left: true, ivyScale), flipped: false, back: false);
                    IvyCluster(p, GardenLook.IvyBox(box, left: false, ivyScale), flipped: true, back: false);
                    break;
                case SignDecor.Flowers:
                    p.Mark("ui.sign.flowers");
                    FlowerCluster(p, GardenLook.FlowerBox(box, left: true), flipped: false);
                    FlowerCluster(p, GardenLook.FlowerBox(box, left: false), flipped: true);
                    break;
            }
        }

        /// <summary>
        /// A cluster of clover leaves (§3.9, <c>ui.sign.ivy</c>) gathered at the top and bottom corners of a sign's end:
        /// soft pointed leaflets in yellow-green <c>ivy.leaf</c> shades over a soft <c>ivy.line</c> shadow between them,
        /// with a thin outline of their own darker shade, a light top-left side and faint midribs, mirrored when
        /// <paramref name="flipped"/>.
        /// <paramref name="back"/> draws only the back half of the leaves (behind a sign), false only the front half; null
        /// draws all.
        /// </summary>
        public static void IvyCluster(IPainter p, Box box, bool flipped, bool? back = null)
        {
            p.Mark("ui.sign.ivy");
            if (p.HasSprite(PainterBase.DecorPrefix + OwnerPictures.Ivy))
            {
                // The owner's cluster (pictures.md D5) is one picture over the plank's end, mirrored for the right end.
                if (back != true)
                {
                    OwnerPicture(p, PainterBase.DecorPrefix + OwnerPictures.Ivy, box, mirror: flipped);
                }

                return;
            }

            // One baked picture per half-cluster (LeafPictures), not five masks per leaf.
            (string key, Func<int, int, byte[]> render) = Recipe(Ivies, (Flipped: flipped, Back: back), k =>
                ("ui.sign.ivy/" + (k.Flipped ? "r" : "l") + "/" + (k.Back.HasValue ? (k.Back.Value ? "back" : "front") : "all"),
                    (w, h) => LeafPictures.Ivy(w, h, k.Flipped, k.Back)));
            p.Picture(key, box, render);
        }

        /// <summary>
        /// A lush cluster for the win sign's ends (§3.9; <c>ui.deco.garden</c>): five big almond leaves in three greens
        /// with <c>ivy.line</c> midribs, fanned up, left and down from the cluster's base, and two white five-petal flowers
        /// with yellow middles over them; turned half way when <paramref name="flipped"/>.
        /// </summary>
        public static void FlowerCluster(IPainter p, Box box, bool flipped)
        {
            p.Mark("ui.deco.garden");
            if (OwnerPicture(p, PainterBase.DecorPrefix + OwnerPictures.Flowers, box, mirror: flipped))
            {
                // The owner's cluster (pictures.md D6), mirrored for the other end.
                return;
            }

            // One baked picture per cluster (LeafPictures), not 28 masks.
            p.Picture(flipped ? "ui.deco.garden/cluster/flipped" : "ui.deco.garden/cluster", box, flipped ? FlippedFlowerClusterPicture : FlowerClusterPicture);
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
                CostPill(p, Box.FromCenter(button.CenterX, button.Bottom + (pillHeight * 0.1f), button.Width * 0.64f, pillHeight), cost.Value, chargeIcon: icon);
            }

            p.PopAlpha();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        // ---- Board furniture (§3.6) ----

        /// <summary>
        /// A Garden Entry's small stone arch set in the border (spec 005 FR-034, <see cref="UiRaster.EntryArch"/>), drawn
        /// upright in its box and turned to its side (<see cref="BoardLayout.ArchOf"/>): over the border and the foot of the
        /// entry cell, under the walkers.
        /// </summary>
        public static void EntryArch(IPainter p, (Box Box, float Degrees) arch)
        {
            p.Mark("board.entry.arch");
            bool turned = Math.Abs(arch.Degrees) > 0.5f;
            if (turned)
            {
                p.PushRotate(arch.Degrees, arch.Box.CenterX, arch.Box.CenterY);
            }

            p.Picture("board.entry.arch", arch.Box, UiRaster.EntryArch);
            if (turned)
            {
                p.PopTransform();
            }
        }

        /// <summary>
        /// The stone border around a board's grid (§3.6): a dark gap of 0.04 cell around <paramref name="grid"/>, then
        /// blocks of stone <paramref name="thickness"/> cells thick (0.42 on the board, 0.3 around the win picture) whose
        /// lengths alternate 1.0 and 0.8 cell (nearly rectangular, rounded 14%), square blocks rounded 30% at the corners,
        /// and thin dark joints between them. Draw it
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
            p.FillRound(outer.Inset(joint * 0.5f), t * 0.4f, C.StoneLine.WithAlpha(0.45f));
            p.FillRound(inner, gap * 2f, GardenLook.BoardGap);

            // The corners, then each side's run of blocks between them.
            float half = joint / 2f;
            StoneBlock(p, new Box(outer.Left, outer.Top, inner.Left, inner.Top).Inset(half), 1, 0.3f);
            StoneBlock(p, new Box(inner.Right, outer.Top, outer.Right, inner.Top).Inset(half), 2, 0.3f);
            StoneBlock(p, new Box(outer.Left, inner.Bottom, inner.Left, outer.Bottom).Inset(half), 3, 0.3f);
            StoneBlock(p, new Box(inner.Right, inner.Bottom, outer.Right, outer.Bottom).Inset(half), 4, 0.3f);
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
                StoneBlock(p, place(at, next).Inset(half), seed, 0.14f);
                at = next;
            }
        }

        private static float Pattern(int i, int side) => ((i + side) % 2) == 0 ? 1f : 0.8f;

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
        /// How far the variant's color is lightened for the top of an exposed pod's panel: clearly tinted, keeping the
        /// color's hue (the reference's lime, pink, sky blue and orange panels; spec 005 FR-020).
        /// </summary>
        public const float PodPanelTop = 0.5f;

        /// <summary>How far the variant's color is lightened for the bottom of an exposed pod's panel.</summary>
        public const float PodPanelBottom = 0.8f;

        /// <summary>How far the variant's color is lightened for the top of a waiting pod's panel: a light wash.</summary>
        public const float PodWaitingPanelTop = 0.74f;

        /// <summary>How far the variant's color is lightened for the bottom of a waiting pod's panel.</summary>
        public const float PodWaitingPanelBottom = 0.9f;

        /// <summary>The veil of <c>parchment.bottom</c> that mutes a waiting pod's frame and panel (its tile and count stay readable).</summary>
        public const float PodWaitingVeil = 0.4f;

        /// <summary>
        /// A pod's wooden frame in the tray's grid (§3.7, <c>mat.wood.dark</c>; the owner's rule of 2026-10-03): a box
        /// wider than tall, its corners rounded by 20% of its height and its border <see cref="PodChip.Border"/> of it, over
        /// a soft shadow and around the inner panel. The panel runs from <paramref name="panelTop"/> to
        /// <paramref name="panelBottom"/> (the variant's color lightened), plain cream without them, <c>state.lock_bg</c>
        /// when locked. A <paramref name="waiting"/> pod (the next ones of the stack, under the exposed one) is muted by a
        /// veil of <c>parchment.bottom</c> over the frame and the panel, with a lighter shadow; a pressed one sinks a little.
        /// Returns the inner panel (<see cref="PodChip.Inner"/>, moved down with a pressed frame), where the tile and the
        /// count go.
        /// </summary>
        public static Box PodFrame(IPainter p, Box box, PodLook look, bool waiting, Rgba? panelTop = null, Rgba? panelBottom = null)
        {
            p.Mark("mat.wood.dark");
            float h = box.Height;
            bool pressed = look == PodLook.Pressed;
            Box frame = pressed ? box.Offset(0f, h * 0.04f) : box;
            float radius = h * 0.2f;
            float border = h * PodChip.Border;
            p.FillRound(frame.Offset(0f, h * (pressed ? 0.015f : 0.06f)).Inset(h * 0.04f, 0f), radius, C.GardenShadow.WithAlpha(waiting ? 0.1f : 0.26f));
            Box panel = frame.Inset(border * 0.8f);
            float panelRadius = Math.Max(0f, radius - (border * 0.8f));
            if (look == PodLook.Locked)
            {
                p.FillRoundGradient(panel, panelRadius, C.StateLockBg.Lighten(0.2f), C.StateLockBg);
            }
            else if (panelTop.HasValue)
            {
                p.FillRoundGradient(panel, panelRadius, panelTop.Value, panelBottom ?? C.CreamFace);
            }
            else
            {
                p.FillRoundGradient(panel, panelRadius, C.CreamTop, C.CreamFace);
            }

            p.Picture("mat.wood.dark/chip/4", frame, (pw, ph) => UiRaster.Frame(pw, ph, ph * 0.2f, ph * PodChip.Border, WoodTone.Dark, 4));
            if (waiting)
            {
                p.FillRound(frame, radius, C.ParchmentBottom.WithAlpha(PodWaitingVeil));
            }
            else if (pressed)
            {
                // Pressed: the whole pod is a little darker as it sinks.
                p.FillRound(frame, radius, C.GardenShadow.WithAlpha(0.08f));
            }

            return frame.Inset(border);
        }

        /// <summary>
        /// A whole pod of the tray's grid (§3.7, <c>pod.card</c>; the owner's choice "E" of 2026-10-03) in
        /// <paramref name="chip"/>: the wooden frame (<see cref="PodFrame"/>) with its panel tinted by the variant, the
        /// owner's icon of the variant alone over the panel's middle (<see cref="PodChip.Icon"/>, no candy tile under it)
        /// and the count's small <c>ink.brown</c> digits in a white outline over the panel's bottom right corner
        /// (<see cref="PodChip.Count"/>). The exposed pod (<paramref name="waiting"/> false) is bright; a waiting one shows
        /// the same parts muted (a lighter wash, the veiled frame, the icon at <see cref="PodChip.WaitingIconAlpha"/> and a
        /// softer count) so its variant and count still read (spec 001 FR-013). A null <paramref name="variant"/> is a
        /// hidden mystery pod (the lilac "?" tile on plain cream); a locked pod shows the padlock on a grey panel and its
        /// softer count; without the owner's picture the variant's sticker tile stands in for the icon
        /// (<see cref="PodChip.Tile"/>); a pressed pod sinks. Returns the tile's place (pods fly to a slot from there).
        /// </summary>
        public static Box Pod(IPainter p, PodChip chip, VariantId? variant, int count, PodLook look, bool waiting)
        {
            p.Mark("pod.card");
            bool locked = look == PodLook.Locked;
            Rgba? color = variant.HasValue && !locked ? Visuals.ColorOf(variant.Value) : (Rgba?)null;
            Rgba? top = color?.Lighten(waiting ? PodWaitingPanelTop : PodPanelTop);
            Rgba? bottom = color?.Lighten(waiting ? PodWaitingPanelBottom : PodPanelBottom);
            Box panel = PodFrame(p, chip.Frame, look, waiting, top, bottom);

            // A pressed frame sinks: its icon and count go with it.
            float sink = panel.Top - chip.Inner.Top;
            Box tile = chip.Tile.Offset(0f, sink);
            string? iconId = !locked && variant.HasValue && VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info) ? info.IconId : null;
            string? icon = iconId == null ? null : PainterBase.IconPrefix + OwnerPictures.VariantIcon(iconId);
            if (icon != null && p.HasSprite(icon))
            {
                p.Mark(OwnerPictures.IconSlot(iconId!));
                p.PushAlpha(waiting ? PodChip.WaitingIconAlpha : 1f);
                p.Sprite(icon, chip.Icon.Offset(0f, sink));
                p.PopAlpha();
            }
            else if (locked)
            {
                p.Mark("pod.state.locked");
                float g = tile.Width * 0.62f;
                p.Shape("ui.lock", Box.FromCenter(tile.CenterX, tile.CenterY, g, g), C.StateLock.Darken(waiting ? 0.05f : 0.2f));
            }
            else
            {
                if (!variant.HasValue)
                {
                    p.Mark("pod.state.mystery");
                }

                CandyTile(p, tile, variant, TileStyle.Sticker, waiting ? TileState.Dimmed : TileState.Normal, pressed: look == PodLook.Pressed);
                if (waiting && !variant.HasValue)
                {
                    // The mystery tile has no dimmed picture: a veil of the parchment dims it like the others.
                    p.FillRound(tile, tile.Width * 0.2f, C.ParchmentBottom.WithAlpha(0.4f));
                }
            }

            PodCount(p, chip.Count.Offset(0f, sink), count, waiting || locked);
            return tile;
        }

        /// <summary>
        /// A pod's count (§3.7, <c>pod.count</c>): small <c>ink.brown</c> digits in a white outline
        /// (<see cref="PodChip.CountLook"/>) centered on <paramref name="area"/>, its height the type size, at most its
        /// width wide.
        /// </summary>
        public static void PodCount(IPainter p, Box area, int count, bool dim)
        {
            p.Mark("pod.count");
            TextLook look = PodChip.CountLook(dim);
            p.Text(count.ToString(CultureInfo.InvariantCulture), area.CenterX, area.CenterY, T.Count, look.FillTop, area.Width, area.Height / p.U(T.Count.Size), look);
        }

        /// <summary>
        /// A pod's or slot's count under its tile (§3.7): plain digits in <c>ink.brown</c>, softer when dimmed, the type's
        /// size scaled to <paramref name="fill"/> of the area's height (slots 0.86, pods 1.05: the digits' caps fill it).
        /// </summary>
        public static void CountBelow(IPainter p, Box area, int count, bool dim, float fill = 0.86f)
        {
            p.Mark("pod.count");
            Rgba ink = dim ? C.InkBrownSoft.Mix(C.ParchmentBottom, 0.3f) : C.InkBrown;
            float scale = (area.Height * fill) / p.U(T.Count.Size);
            p.Text(count.ToString(CultureInfo.InvariantCulture), area.CenterX, area.CenterY, T.Count, ink, area.Width, scale, TextLook.Plain(ink));
        }

        /// <summary>
        /// The side of the tile on a working or stuck slot plate filling <paramref name="plate"/> (<see cref="SlotPlate"/>,
        /// <see cref="ReferenceGameplayRegions.SlotTile"/>): 74% of the face's width, or 66% of its height on a squarer
        /// plate, so the count below it keeps about a quarter of the face. The pods flying to a slot are drawn at this
        /// size and scaled, so they share its picture.
        /// </summary>
        public static float SlotTileSize(Box plate) => ReferenceGameplayRegions.SlotTile(plate).Width;

        /// <summary>
        /// A Waiting Slot (§3.7): a raised cream plate (radius 20%) with the sticker tile at 70% of its width and the count
        /// below while a pod works; the grey tile with the hourglass badge while it is stuck; a slightly sunk face with a
        /// dashed inner outline when empty (in <c>state.danger</c> with "!" for the last free slot); a grey face with the
        /// padlock when locked. <paramref name="extra"/> adds the green "+" badge of the Extra Slot booster. A null
        /// <paramref name="variant"/> in a filled slot is a mystery pod. <paramref name="tileFlip"/> narrows the tile about
        /// its middle (a mystery tile turning over) and <paramref name="countBump"/> scales the count about its baseline
        /// (a Bloomling landing). Returns the plate's face.
        /// </summary>
        public static Box SlotPlate(IPainter p, Box box, SlotPlateState state, VariantId? variant = null, int count = 0, bool extra = false, float tileFlip = 1f, float countBump = 1f)
        {
            float s = Math.Min(box.Width, box.Height);
            float radius = s * 0.2f;
            float line = Math.Max(1f, s * 0.018f);
            Box face;
            if (state == SlotPlateState.Empty || state == SlotPlateState.Danger)
            {
                p.Mark("slot.empty");
                // A plate pressed into the parchment: a thin lower edge, the face a little sunk, a light ring inside the
                // outline, and the dashed inner outline stitched in with a light line under each dash.
                p.FillRound(box.Offset(0f, s * 0.025f), radius, C.CreamLip.WithAlpha(0.55f));
                p.FillRound(box, radius, C.CreamFace.Mix(C.ParchmentWell, 0.35f));
                p.PushClip(box);
                p.FillRoundGradient(new Box(box.Left, box.Top, box.Right, box.Top + (s * 0.3f)), radius, C.GardenShadow.WithAlpha(0.1f), C.GardenShadow.WithAlpha(0f));
                p.PopClip();
                float ring = Math.Max(1f, s * 0.022f);
                p.StrokeRound(box.Inset(line + (ring / 2f)), Math.Max(0f, radius - line - (ring / 2f)), ring, C.CreamTop.WithAlpha(0.75f));
                p.StrokeRound(box.Inset(line / 2f), radius - (line / 2f), line, C.CreamLine.WithAlpha(0.6f));
                bool danger = state == SlotPlateState.Danger;
                Box dashed = box.Inset(s * 0.085f);
                float dash = Math.Max(1.5f, s * 0.03f);
                p.StrokeRound(dashed.Offset(0f, dash * 0.45f), radius * 0.7f, dash, C.CreamTop.WithAlpha(0.8f), s * 0.09f, s * 0.06f);
                p.StrokeRound(dashed, radius * 0.7f, dash, danger ? C.StateDanger : C.CreamLine.WithAlpha(0.85f), s * 0.09f, s * 0.06f);
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
                float lip = s * 0.055f;
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

                    // The tile near the top (about 74% of a portrait plate's width, as in the reference; less on a square
                    // one), so the count below it keeps about a quarter of the face.
                    Box tileBox = ReferenceGameplayRegions.SlotTile(box);
                    p.PushSquash(tileFlip, 1f, tileBox.CenterX, tileBox.CenterY);
                    CandyTile(p, tileBox, variant, TileStyle.Sticker, stuck && variant.HasValue ? TileState.Grey : TileState.Normal);
                    p.PopTransform();
                    var countArea = new Box(face.Left, tileBox.Bottom, face.Right, face.Bottom - (face.Height * 0.03f));
                    p.PushTransform(0f, 0f, countBump, countArea.CenterX, countArea.Bottom);
                    CountBelow(p, countArea, count, stuck);
                    p.PopTransform();
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
        /// A booster tile (§3.7; the booster bar, <c>booster.tile</c>): a cream squircle (radius 26%) set in a cream-white
        /// bezel with the booster's colored icon (about two thirds of the tile), and the count badge over its lower right
        /// corner, or the cost pill under it and a small green "+" when no charges are left. A selected tile is raised with
        /// the pulsing golden glow; a disabled one is greyed (spec 003 FR-031).
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
            Box f = BoosterBezel(p, tile, radius, depth, state.Disabled);
            // The icon's box: its shapes keep a margin, so the icon itself is about two thirds of the tile, as in the
            // reference.
            float icon = s * 0.74f;
            BoosterIcon(p, boosterId, Box.FromCenter(f.CenterX, f.CenterY, icon, icon), grey: state.Disabled);
            p.PopTransform();

            if (state.ShowsCharges)
            {
                // The badge sits over the tile's lower right corner, mostly on the tile, as in the reference.
                float badge = s * 0.34f;
                CountBadge(p, tile.Right - (badge * 0.55f), tile.Bottom - (badge * 0.55f), badge, state.Charges.ToString(CultureInfo.InvariantCulture));
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

        /// <summary>
        /// A booster tile's body (§3.7): a cream face set in a cream-white bezel with a faint silver tint
        /// (<see cref="GardenLook.BoosterRim"/>, light at the top), a cream lip along its bottom
        /// (<see cref="GardenLook.BoosterLip"/>), a light edge where the face meets the bezel, a soft tan outline
        /// (<see cref="GardenLook.BoosterLine"/>) and a soft shadow; the face sinks into the lip by the press
        /// <paramref name="depth"/>. A disabled tile's face is grey. Returns the face.
        /// </summary>
        private static Box BoosterBezel(IPainter p, Box box, float radius, float depth, bool disabled)
        {
            p.Mark("booster.tile");
            float s = Math.Min(box.Width, box.Height);
            float lip = s * 0.085f;
            float bezel = s * 0.065f;
            float line = Math.Max(p.U(2f), s * 0.018f);
            float shift = lip * 0.7f * Math.Max(-0.25f, Math.Min(1f, depth));
            float dark = 0.08f * Math.Max(0f, Math.Min(1f, depth));
            float r = Math.Min(radius, s / 2f);
            Rgba rim = GardenLook.BoosterRim;
            Rgba face = disabled ? C.CreamFace.Grey().Lighten(0.25f) : C.CreamFace;
            Rgba middle = disabled ? C.CreamTop.Grey().Lighten(0.3f) : C.CreamTop;

            SoftShadow(p, box, r, 0.22f, 0.06f);
            var whole = new Box(box.Left, box.Top + Math.Max(0f, shift), box.Right, box.Bottom);
            p.FillRound(whole, r, GardenLook.BoosterLip.Darken(dark));
            var top = new Box(box.Left, box.Top + shift, box.Right, box.Bottom - lip + shift);
            float topRadius = Math.Min(r, top.Height / 2f);
            p.FillRoundGradient(top, topRadius, rim.Lighten(0.62f).Darken(dark), rim.Lighten(0.22f).Darken(dark));

            Box inner = top.Inset(bezel);
            float innerRadius = Math.Max(0f, topRadius - bezel);
            p.FillRoundGradient(inner, innerRadius, face.Darken(0.02f + dark), face.Darken(dark));
            for (int k = 0; k < 3; k++)
            {
                // A lighter middle, feathered in, as on the reference's cream tiles.
                float inset = s * (0.1f + (0.05f * k));
                p.FillRoundGradient(inner.Inset(inset), Math.Max(0f, innerRadius - inset), middle.Darken(dark).WithAlpha(0.35f), middle.WithAlpha(0f));
            }

            // The face lies a little below the bezel: a faint shade inside its top edge and a light edge around it.
            p.PushClip(inner);
            p.FillRoundGradient(new Box(inner.Left, inner.Top, inner.Right, inner.Top + (inner.Height * 0.14f)), innerRadius, C.GardenShadow.WithAlpha(0.08f), C.GardenShadow.WithAlpha(0f));
            p.PopClip();
            p.StrokeRound(inner, innerRadius, Math.Max(1f, s * 0.014f), C.CreamTop.WithAlpha(0.9f));

            var outline = new Box(box.Left, Math.Min(top.Top, whole.Top), box.Right, box.Bottom);
            p.StrokeRound(outline.Inset(line / 2f), Math.Min(r, outline.Height / 2f) - (line / 2f), line, GardenLook.BoosterLine);
            return inner;
        }

        /// <summary>
        /// A board cell of the picture's background (spec 005 FR-020, <c>tile.grass</c>): a square of lawn
        /// (<see cref="UiRaster.Grass"/>, one of <see cref="UiRaster.GrassVariants"/> by <paramref name="seed"/>,
        /// <see cref="UiRaster.GrassSeed"/>) filling <paramref name="full"/>, so the ground around the picture reads as
        /// garden; restored role cells stay pale picture colors (<c>tile.ground</c>).
        /// </summary>
        public static void GrassCell(IPainter p, Box full, int seed)
        {
            p.Mark("tile.grass");
            int k = ((seed % UiRaster.GrassVariants) + UiRaster.GrassVariants) % UiRaster.GrassVariants;
            p.Picture("tile.grass/" + k, full, (w, h) => UiRaster.Grass(w, h, k));
        }

        /// <summary>
        /// A booster's colored icon (§3.8, <see cref="GardenLook.BoosterIcon"/>), all grey when <paramref name="grey"/>, or
        /// the owner's icon picture (pictures.md D1–D4) when it is embedded.
        /// </summary>
        public static void BoosterIcon(IPainter p, string boosterId, Box box, bool grey = false)
        {
            p.Mark("booster." + boosterId);
            // IconParts draws the owner's icon picture instead when it is embedded (pictures.md D1–D4), faded when grey.
            IconParts(p, box, GardenLook.BoosterIcon(boosterId), grey);
        }

        /// <summary>
        /// One of the owner's pictures (spec 005 pictures.md D) fitted into <paramref name="box"/>, mirrored left to right
        /// about the box's middle when <paramref name="mirror"/> and upside down when <paramref name="turn"/> (both: turned
        /// half way); false, drawing nothing, when the picture is not embedded, so the caller draws its stand-in.
        /// </summary>
        public static bool OwnerPicture(IPainter p, string name, Box box, bool mirror = false, bool turn = false)
        {
            if (!p.HasSprite(name))
            {
                return false;
            }

            bool flip = mirror || turn;
            if (flip)
            {
                p.PushSquash(mirror ? -1f : 1f, turn ? -1f : 1f, box.CenterX, box.CenterY);
            }

            p.Sprite(name, box);
            if (flip)
            {
                p.PopTransform();
            }

            return true;
        }

        // ---- Celebration (§3.9) ----

        /// <summary>
        /// Light rays behind the celebrating heroes (§3.9, <c>fx.rays</c>): ten soft <c>ray.light</c> wedges from
        /// (<paramref name="cx"/>, <paramref name="cy"/>) over a soft radial glow, turning 0.05 turn per second, brightest
        /// at the center.
        /// <paramref name="seconds"/> is the time since the win.
        /// </summary>
        public static void LightRays(IPainter p, float cx, float cy, float radius, float seconds)
        {
            p.Mark("fx.rays");
            double turn = seconds * 0.05 * 2.0 * Math.PI;
            for (int k = 7; k >= 0; k--)
            {
                // A soft radial glow: eight faint discs, so no edge shows.
                p.FillCircle(cx, cy, radius * (0.06f + (0.06f * k)), C.RayLight.WithAlpha(0.035f));
            }

            for (int i = 0; i < 10; i++)
            {
                double a = turn + (i * Math.PI / 5.0);
                float wide = i % 2 == 0 ? 1f : 0.7f;
                for (int j = -2; j <= 2; j++)
                {
                    double aj = a + (j * 0.034 * wide);
                    float x = (float)Math.Cos(aj);
                    float y = (float)Math.Sin(aj);
                    p.Line(cx, cy, cx + (x * radius), cy + (y * radius), radius * 0.05f * wide, C.RayLight.WithAlpha(0.09f));
                    p.Line(cx, cy, cx + (x * radius * 0.62f), cy + (y * radius * 0.62f), radius * 0.045f * wide, C.RayLight.WithAlpha(0.11f));
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
        /// darker extrusion inside a mossy band, as the reference's logo, broad leaves behind both ends and small pink
        /// flowers over them.
        /// </summary>
        public static void WoodLogo(IPainter p, Box box, string text)
        {
            p.Mark("ui.logo.wood");
            TypeStyle style = T.Wordmark;
            float natural = Math.Max(1f, p.MeasureText(text, style));
            float scale = Math.Min(box.Width * 0.8f / natural, box.Height * 0.72f / p.U(style.Size));
            float width = natural * scale;
            float em = p.U(style.Size) * scale;
            float cy = box.CenterY - (em * 0.04f);
            float leaf = em * 1.7f;
            float left = box.CenterX - (width / 2f);
            float right = box.CenterX + (width / 2f);
            LogoLeaves(p, Box.FromCenter(left + (em * 0.02f), cy - (em * 0.04f), leaf, leaf), mirrored: false);
            LogoLeaves(p, Box.FromCenter(right - (em * 0.02f), cy - (em * 0.14f), leaf, leaf), mirrored: true);

            // The mossy band around the letters (the reference's olive rim), then the wooden letters.
            Rgba moss = C.IvyLine.Mix(C.WoodLine, 0.35f).Lighten(0.12f);
            p.Text(text, box.CenterX, cy, style, moss, sizeScale: scale, look: new TextLook(moss, moss, moss, 0.14f, 0.14f, 0.36f));
            p.Text(text, box.CenterX, cy, style, C.WoodLight, sizeScale: scale, look: GardenLook.WoodLetters);

            LogoFlower(p, Box.FromCenter(left - (em * 0.12f), cy + (em * 0.42f), em * 0.36f, em * 0.36f));
            LogoFlower(p, Box.FromCenter(right + (em * 0.08f), cy - (em * 0.5f), em * 0.4f, em * 0.4f));
        }

        /// <summary>
        /// The broad leaves behind one end of the wordmark: the win sign's cluster leaves (<c>ui.deco.garden</c>) fanned to
        /// the left, or mirrored to the right.
        /// </summary>
        private static void LogoLeaves(IPainter p, Box box, bool mirrored)
        {
            p.Mark("ui.deco.garden");
            if (OwnerPicture(p, PainterBase.DecorPrefix + OwnerPictures.LogoLeaves, box, mirror: mirrored))
            {
                // The owner's leaves (pictures.md D8), mirrored for the right end.
                return;
            }

            // One baked picture per end (LeafPictures), not 20 masks.
            p.Picture(mirrored ? "ui.logo.wood/leaves/m" : "ui.logo.wood/leaves/l", box, mirrored ? MirroredLogoLeavesPicture : LogoLeavesPicture);
        }

        /// <summary>A small pink five-petal flower with a yellow middle over the wordmark's leaves.</summary>
        private static void LogoFlower(IPainter p, Box bloom)
        {
            (Rgba petals, Rgba line, Rgba center) = GardenLook.PinkFlower;
            Func<float, float, float> sdf = ShapeLibrary.Get("fx.petal_burst");
            p.ShapeOf("ui.logo.wood/flower/line", (x, y) => sdf(x, y) - 0.07f, bloom, line);
            p.Shape("fx.petal_burst", bloom, petals);
            p.FillCircle(bloom.CenterX, bloom.CenterY, bloom.Width * 0.14f, C.GardenFlowerCenterLine);
            p.FillCircle(bloom.CenterX, bloom.CenterY, bloom.Width * 0.11f, center);
        }

        private static float Frac(float v) => v - (float)Math.Floor(v);

        /// <summary>A share as a short cache-key text (0.28 → "0.28").</summary>
        private static string Share(float share) => share.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
