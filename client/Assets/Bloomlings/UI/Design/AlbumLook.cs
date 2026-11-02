using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The Scene Album page's regions (spec 007-scene-album, the owner's UX and mockup of 2026-11-02; the progress bar and
    /// the chest of the owner's change of the same day), both builds, screen pixels with y down:
    /// <list type="bullet">
    /// <item><description><see cref="Header"/>: the pages' header row (back, the "Scene Album" wooden sign, the Petals pill).</description></item>
    /// <item><description><see cref="Ribbon"/>: the scene's name on the cream name plate.</description></item>
    /// <item><description><see cref="Card"/>: the centre card, the wooden card frame snug round <see cref="Scene"/>, the
    /// scene contain-fitted (never cropped); the neighbours stand <see cref="Step"/> to each side, their edges showing.</description></item>
    /// <item><description><see cref="Previous"/>, <see cref="Next"/>: the raised ‹ › buttons over the neighbours' edges, on
    /// the scene's middle.</description></item>
    /// <item><description><see cref="Strip"/>: the progress strip under the card, above the bottom menu: the objects'
    /// medallions in a row (<see cref="Medallion"/>), under them the progress bar (<see cref="Bar"/>, its fill
    /// <see cref="BarFill"/>, "1 / 4" on it) running into the chest at its right end (<see cref="Chest"/>, over the bar's
    /// end), and a caption row under the bar ("Scene complete" or "Reward claimed" at <see cref="StatusLine"/>, "Scene 2 / N"
    /// at <see cref="SceneLine"/>).</description></item>
    /// </list>
    /// Engine-free.
    /// </summary>
    public sealed record AlbumRegions(
        Box Safe,
        float W,
        PageHeader Header,
        Box Ribbon,
        Box Card,
        Box Scene,
        float Step,
        Box Previous,
        Box Next,
        Box Strip,
        Box Bar,
        Box Chest,
        Box StatusLine,
        Box SceneLine,
        float NavTop,
        float TouchMin)
    {
        /// <summary>The medallion row's middle, 0.088 W under the strip's top.</summary>
        public float MedallionY => Strip.Top + (W * 0.088f);

        /// <summary>
        /// Medallion <paramref name="index"/> of <paramref name="count"/>: a disc of up to 0.13 W in the strip's row left of
        /// the chest, the discs evenly spaced.
        /// </summary>
        public Box Medallion(int index, int count)
        {
            (float left, float cell) = MedallionColumns(count);
            float side = Math.Min(W * 0.13f, cell - (W * 0.015f));
            return Box.FromCenter(left + (cell * (index + 0.5f)), MedallionY, side, side);
        }

        /// <summary>Medallion <paramref name="index"/>'s touch box: its whole column of the row above the bar, at least the touch minimum.</summary>
        public Box MedallionTouch(int index, int count)
        {
            (float left, float cell) = MedallionColumns(count);
            var column = new Box(left + (cell * index), Strip.Top, left + (cell * (index + 1)), Bar.Top - (W * 0.008f));
            return Box.FromCenter(column.CenterX, column.CenterY, Math.Max(column.Width, TouchMin), Math.Max(column.Height, TouchMin));
        }

        /// <summary>The chest's touch box: the chest, at least the touch minimum.</summary>
        public Box ChestTouch => Box.FromCenter(Chest.CenterX, Chest.CenterY, Math.Max(Chest.Width, TouchMin), Math.Max(Chest.Height, TouchMin));

        /// <summary>
        /// The bar's fill at <paramref name="share"/> (0 empty, 1 full) inside <see cref="Bar"/>'s pressed-in track: inset by
        /// 0.16 of the bar's height, never narrower than it is tall (a round pill), null when empty.
        /// </summary>
        public Box? BarFill(float share)
        {
            if (share <= 0f)
            {
                return null;
            }

            Box inner = Bar.Inset(Bar.Height * 0.16f);
            float width = Math.Max(inner.Height, inner.Width * Math.Min(1f, share));
            return new Box(inner.Left, inner.Top, inner.Left + width, inner.Bottom);
        }

        /// <summary>The card <paramref name="offset"/> places from the centre one (−1 the previous, +1 the next), shifted by <paramref name="slide"/> pixels.</summary>
        public Box CardAt(int offset, float slide = 0f) => Card.Offset((offset * Step) + slide, 0f);

        /// <summary>The scene <paramref name="offset"/> places from the centre one, shifted by <paramref name="slide"/> pixels.</summary>
        public Box SceneAt(int offset, float slide = 0f) => Scene.Offset((offset * Step) + slide, 0f);

        /// <summary>The green check badge of a complete scene, over the card's top-right corner (0.09 W).</summary>
        public Box CompleteBadge(Box card) => Box.FromCenter(card.Right - (W * 0.035f), card.Top + (W * 0.035f), W * 0.09f, W * 0.09f);

        /// <summary>The claimed chest's check ball over its bottom-right (0.075 W).</summary>
        public Box ChestCheck => Box.FromCenter(Chest.Right - (W * 0.03f), Chest.Bottom - (W * 0.035f), W * 0.075f, W * 0.075f);

        /// <summary>The menu's "!" over an icon's upper right (<see cref="BottomNav.LockBox"/>'s size, mirrored up).</summary>
        public static Box NewsBadge(Box icon)
        {
            float side = Math.Min(icon.Width, icon.Height);
            float badge = side * BottomNav.LockShare;
            float inset = side * BottomNav.LockInsetShare;
            return new Box(icon.Right - inset - badge, icon.Top + inset, icon.Right - inset, icon.Top + inset + badge);
        }

        private (float Left, float Cell) MedallionColumns(int count)
        {
            float left = Strip.Left + (W * 0.03f);
            float right = Chest.Left - (W * 0.01f);
            return (left, (right - left) / Math.Max(1, count));
        }
    }

    /// <summary>The chest's three states (spec 007 FR-012): the scene still filling, complete with the chest waiting, claimed.</summary>
    public enum AlbumChestState
    {
        /// <summary>The scene is not complete: the chest dimmed (partly grey), its tooltip says how it opens.</summary>
        Filling,

        /// <summary>Complete and not claimed: the chest in colour, glowing and bouncing gently; its tooltip offers Claim.</summary>
        Ready,

        /// <summary>Claimed: the chest in colour with the check ball; its tooltip says "Reward claimed".</summary>
        Claimed,
    }

    /// <summary>
    /// The chest's tooltip (spec 007 FR-012, the owner's change of 2026-11-02): the guides' parchment bubble (the popups' card
    /// frame with a turned-square tail, <see cref="TailX"/> on its bottom edge pointing at the chest) above the chest: the
    /// title (<see cref="Title"/>), one row a content (<see cref="Icons"/>, <see cref="Texts"/>: the Petals lotus and
    /// "200 Petals", the Return booster and "1 Return", the Extra Slot booster and "1 Extra Slot") and the footer
    /// (<see cref="Footer"/>: the green Claim when the chest is ready, else a line). Engine-free.
    /// </summary>
    public sealed record AlbumChestTipRegions(Box Bubble, float TailX, float Tail, Box Title, IReadOnlyList<Box> Icons, IReadOnlyList<Box> Texts, Box Footer, Box FooterIcon);

    /// <summary>
    /// The Scene Album's object sheet (spec 007 FR-009, FR-010; the mockup's middle state): a bottom sheet about a third of
    /// the screen in the popups' card frame (<see cref="Panel"/>, from <see cref="Top"/> to past the screen's bottom), its
    /// grab handle (<see cref="Grip"/>), the close over its top-right corner (<see cref="Close"/>), the object's colour
    /// preview in a well (<see cref="Preview"/>), its name (<see cref="Name"/>), two lines (<see cref="Line1"/>: the
    /// padlock and "Unlocks after Level N", or the check and "Level N completed"; <see cref="Line2"/>: "Next level: M") and
    /// the green primary button "Continue playing" (<see cref="Button"/>). Engine-free.
    /// </summary>
    public sealed record AlbumSheetRegions(Box Panel, float Top, Box Grip, Box Close, Box Preview, Box Name, Box Line1, Box Line2, Box Icon1, Box Button);

    /// <summary>
    /// The Scene Album page's layout and motion (spec 007-scene-album), both builds: the page (<see cref="Page"/>), the
    /// object sheet (<see cref="Sheet"/>), where a layer lies on a fitted scene (<see cref="LayerBox"/>), which object a tap
    /// hits (<see cref="HitObject"/>, on the layers' alpha), the lift of a card the sheet would cover (<see cref="Lift"/>),
    /// the colour's return of a new reveal (<see cref="Reveal"/>), the snap of a turn (<see cref="Snap"/>) and the golden
    /// outline's pulse (<see cref="OutlineAlpha"/>). A locked layer is the owner's colour picture in greyscale at 82 %
    /// brightness (<see cref="LockedPixels"/>, the pack's lockedStyle). Engine-free.
    /// </summary>
    public static class AlbumLook
    {
        /// <summary>A locked layer's brightness (the pack's lockedStyle: saturation 0, brightness 0.82).</summary>
        public const float LockedBrightness = 0.82f;

        /// <summary>The colour's return of a new reveal, in seconds (the pack's crossfade of 0.4–0.6 s).</summary>
        public const float RevealSeconds = 0.5f;

        /// <summary>When the first new reveal starts after the scene shows, and the stagger between reveals, in seconds.</summary>
        public const float RevealDelay = 0.35f;

        public const float RevealStagger = 0.3f;

        /// <summary>A turn's snap, in seconds: the card slides the rest of its way in.</summary>
        public const float SnapSeconds = 0.28f;

        /// <summary>The sheet's lift of the card, in seconds.</summary>
        public const float LiftSeconds = 0.25f;

        /// <summary>The golden outline's width as a share of W.</summary>
        public const float OutlineShare = 0.011f;

        /// <summary>The alpha a tap must hit in a layer to choose its object (its transparent area never does).</summary>
        public const byte HitAlpha = 40;

        /// <summary>The wooden card frame's width round the scene, in reference units (the popups' frame picture's wood).</summary>
        public const float FrameUnits = 50f;

        /// <summary>The scene's corner radius inside the frame, in reference units.</summary>
        public const float SceneRadiusUnits = 22f;

        /// <summary>The chest's dim while its scene fills: its colour picture at this alpha over its grey copy.</summary>
        public const float ChestFillingColour = 0.4f;

        /// <summary>How long the ready chest waits between its gentle hops, and one hop's length, in seconds.</summary>
        public const float ChestHopEvery = 2.4f;

        public const float ChestHopSeconds = 0.45f;

        /// <summary>The chest's tooltip's pop, in seconds.</summary>
        public const float TipSeconds = 0.16f;

        /// <summary>
        /// The page for a scene canvas of <paramref name="canvasWidth"/> × <paramref name="canvasHeight"/> (every scene of the
        /// pack shares one), in shares of the safe width W: the pages' header row; the name plate 0.66 W × 0.1 W under it;
        /// above the bottom menu the progress strip 0.92 W × 0.3 W (the medallion row, under it the bar 0.062 W high from
        /// 0.04 W in to the chest's middle, the chest 0.215 W over its right end, the caption row under the bar); the card
        /// between the name plate and the strip, the scene contain-fitted at most 0.78 W wide, the frame
        /// <see cref="FrameUnits"/> round it; the neighbours a card's width and 0.05 W to each side; the ‹ › buttons 0.115 W
        /// (at least the touch minimum) 0.075 W in from the safe sides, on the scene's middle.
        /// </summary>
        public static AlbumRegions Page(float width, float height, Insets insets, int canvasWidth, int canvasHeight)
        {
            ReferenceStoreRegions store = ScreenLayout.ReferenceStore(width, height, insets, hasCosmetics: false, hasStatus: false);
            Box safe = store.Safe;
            float w = store.W;
            float unit = DesignTokens.ScaleFor(width, height);
            float touch = DesignTokens.Size.TouchMin * unit;
            float cx = safe.CenterX;
            var ribbon = new Box(cx - (0.33f * w), store.Header.Row.Bottom + (0.012f * w), cx + (0.33f * w), store.Header.Row.Bottom + (0.112f * w));
            float navTop = store.NavTop;
            var strip = new Box(cx - (0.46f * w), navTop - (0.02f * w) - (0.3f * w), cx + (0.46f * w), navTop - (0.02f * w));
            float barY = strip.Top + (0.198f * w);
            var chest = Box.FromCenter(strip.Right - (0.125f * w), barY - (0.035f * w), 0.215f * w, 0.215f * w);
            var bar = new Box(strip.Left + (0.04f * w), barY - (0.031f * w), chest.CenterX, barY + (0.031f * w));
            var status = new Box(bar.Left + (0.01f * w), strip.Top + (0.242f * w), bar.Left + (0.37f * w), strip.Top + (0.284f * w));
            var sceneLine = new Box(status.Right, status.Top, chest.Left - (0.01f * w), status.Bottom);
            float frame = FrameUnits * unit;
            float top = ribbon.Bottom + (0.02f * w) + frame;
            float bottom = strip.Top - (0.022f * w) - frame;
            float aspect = canvasWidth / (float)Math.Max(1, canvasHeight);
            float sceneHeight = Math.Max(1f, Math.Min(bottom - top, 0.78f * w / aspect));
            float sceneWidth = sceneHeight * aspect;
            var scene = new Box(cx - (sceneWidth / 2f), top, cx + (sceneWidth / 2f), top + sceneHeight);
            Box card = scene.Inset(-frame);
            float arrow = Math.Max(0.115f * w, touch);
            float arrowY = scene.CenterY;
            var previous = Box.FromCenter(safe.Left + (0.075f * w), arrowY, arrow, arrow);
            var next = Box.FromCenter(safe.Right - (0.075f * w), arrowY, arrow, arrow);
            return new AlbumRegions(safe, w, store.Header, ribbon, card, scene, card.Width + (0.05f * w), previous, next, strip, bar, chest, status, sceneLine, navTop, touch);
        }

        /// <summary>The chest's state for a scene <paramref name="complete"/> or not, its chest <paramref name="claimed"/> or not.</summary>
        public static AlbumChestState ChestState(bool complete, bool claimed) =>
            claimed ? AlbumChestState.Claimed : complete ? AlbumChestState.Ready : AlbumChestState.Filling;

        /// <summary>
        /// The chest's tooltip over <paramref name="r"/>'s chest, in shares of W: 0.6 W wide, its right edge 0.17 W right of
        /// the chest's middle (inside the safe area), its bottom 0.035 W over the chest's top with the tail between; padding
        /// 0.045 W; the title 0.07 W; three rows 0.085 W (the icon 0.078 W); the footer 0.115 W (the green Claim, 0.42 W) when
        /// <paramref name="claim"/>, else a line 0.06 W.
        /// </summary>
        public static AlbumChestTipRegions ChestTip(AlbumRegions r, bool claim)
        {
            float w = r.W;
            float pad = 0.045f * w;
            float titleH = 0.07f * w;
            float rowH = 0.085f * w;
            float footerH = claim ? 0.115f * w : 0.06f * w;
            float height = pad + titleH + (0.012f * w) + (3f * rowH) + (0.018f * w) + footerH + pad;
            float bubbleWidth = 0.6f * w;
            float right = Math.Min(r.Safe.Right - (0.02f * w), r.Chest.CenterX + (0.17f * w));
            float tail = 0.045f * w;
            float bottom = r.Chest.Top + (0.01f * w) - tail;
            var bubble = new Box(right - bubbleWidth, bottom - height, right, bottom);
            float tailX = Math.Max(bubble.Left + (0.08f * w), Math.Min(bubble.Right - (0.08f * w), r.Chest.CenterX));
            var title = new Box(bubble.Left + pad, bubble.Top + pad, bubble.Right - pad, bubble.Top + pad + titleH);
            var icons = new Box[3];
            var texts = new Box[3];
            float y = title.Bottom + (0.012f * w);
            float icon = 0.078f * w;
            for (int i = 0; i < 3; i++)
            {
                icons[i] = Box.FromCenter(bubble.Left + pad + (0.02f * w) + (icon / 2f), y + (rowH / 2f), icon, icon);
                texts[i] = new Box(icons[i].Right + (0.03f * w), y, bubble.Right - pad, y + rowH);
                y += rowH;
            }

            y += 0.018f * w;
            Box footer = claim
                ? Box.FromCenter(bubble.CenterX, y + (footerH / 2f), 0.42f * w, footerH)
                : new Box(bubble.Left + pad + (0.07f * w), y, bubble.Right - pad, y + footerH);
            Box footerIcon = Box.FromCenter(bubble.Left + pad + (0.03f * w), y + (footerH / 2f), 0.055f * w, 0.055f * w);
            return new AlbumChestTipRegions(bubble, tailX, tail, title, icons, texts, footer, footerIcon);
        }

        /// <summary>
        /// The bar's share <paramref name="seconds"/> after the scene showed: the objects open before
        /// (<paramref name="open"/> less the <paramref name="reveals"/> new ones) plus each new reveal as far as its colour has
        /// come back (<see cref="Reveal"/>), of <paramref name="total"/>: the fill grows with the reveals.
        /// </summary>
        public static float BarShare(int open, int total, int reveals, float seconds)
        {
            if (total <= 0)
            {
                return 0f;
            }

            float filled = Math.Max(0, open - reveals);
            for (int i = 0; i < reveals; i++)
            {
                filled += Reveal(seconds, i);
            }

            return Math.Max(0f, Math.Min(1f, filled / total));
        }

        /// <summary>
        /// The ready chest's motion <paramref name="seconds"/> after the scene showed: its scale (the waiting button's breath,
        /// <see cref="GardenLook.Breathe"/>) and its hop as a share of its height (up is negative: one gentle hop every
        /// <see cref="ChestHopEvery"/> seconds), and its glow's alpha (<see cref="GardenLook.Glow"/>). Still otherwise.
        /// </summary>
        public static (float Scale, float Hop, float Glow) ChestMotion(AlbumChestState state, float seconds)
        {
            if (state != AlbumChestState.Ready)
            {
                return (1f, 0f, 0f);
            }

            float t = seconds % ChestHopEvery;
            float hop = t < ChestHopSeconds ? -0.06f * (float)Math.Sin(Math.PI * t / ChestHopSeconds) : 0f;
            return (GardenLook.Breathe(seconds), hop, GardenLook.Glow(seconds));
        }

        /// <summary>The tooltip's pop <paramref name="seconds"/> after it opened: its scale about the tail, from 0.85 to 1, eased.</summary>
        public static float TipPop(float seconds) => 0.85f + (0.15f * Ease(seconds / TipSeconds));

        /// <summary>
        /// The object sheet, in shares of the safe width W: the panel 0.96 W wide, its top at a third of the screen's height
        /// from the bottom (at least 0.72 W, at most 45 % of the screen), its bottom past the screen's bottom; the grip 0.14 W ×
        /// 0.016 W on its top's middle; the close 0.1 W over its top-right corner; the preview well 0.3 W at its left; the
        /// name, the two lines at its right; the button 0.78 W × 0.14 W above the safe bottom.
        /// </summary>
        public static AlbumSheetRegions Sheet(float width, float height, Insets insets)
        {
            Box safe = ScreenLayout.SafeArea(width, height, insets);
            float w = safe.Width;
            float sheetHeight = Math.Min(height * 0.45f, Math.Max(height * 0.34f, 0.72f * w));
            float top = height - sheetHeight;
            var panel = new Box(safe.Left + (0.02f * w), top, safe.Right - (0.02f * w), height + (0.08f * w));
            var grip = Box.FromCenter(safe.CenterX, top + (0.045f * w), 0.14f * w, 0.016f * w);
            var close = Box.FromCenter(panel.Right - (0.085f * w), top + (0.085f * w), 0.1f * w, 0.1f * w);
            float buttonBottom = safe.Bottom - (0.035f * w);
            var button = new Box(safe.CenterX - (0.39f * w), buttonBottom - (0.14f * w), safe.CenterX + (0.39f * w), buttonBottom);
            float previewSide = Math.Max(0.16f * w, Math.Min(0.3f * w, button.Top - (0.04f * w) - (top + (0.1f * w))));
            var preview = new Box(panel.Left + (0.07f * w), top + (0.1f * w), panel.Left + (0.07f * w) + previewSide, top + (0.1f * w) + previewSide);
            float textLeft = preview.Right + (0.05f * w);
            float textRight = panel.Right - (0.06f * w);
            var name = new Box(textLeft, preview.Top + (0.01f * w), textRight - (0.06f * w), preview.Top + (0.1f * w));
            var line1 = new Box(textLeft + (0.075f * w), name.Bottom + (0.025f * w), textRight, name.Bottom + (0.095f * w));
            var icon1 = Box.FromCenter(textLeft + (0.03f * w), line1.CenterY, 0.06f * w, 0.06f * w);
            var line2 = new Box(textLeft, line1.Bottom + (0.02f * w), textRight, line1.Bottom + (0.075f * w));
            return new AlbumSheetRegions(panel, top, grip, close, preview, name, line1, line2, icon1, button);
        }

        /// <summary>A picture of <paramref name="width"/> × <paramref name="height"/> contain-fitted into <paramref name="box"/>, centred.</summary>
        public static Box Fit(Box box, float width, float height)
        {
            float k = Math.Min(box.Width / Math.Max(1f, width), box.Height / Math.Max(1f, height));
            return Box.FromCenter(box.CenterX, box.CenterY, width * k, height * k);
        }

        /// <summary>Where a layer cropped to (<paramref name="x"/>, <paramref name="y"/>, <paramref name="w"/>, <paramref name="h"/>) of a canvas <paramref name="canvasWidth"/> wide lies on the fitted <paramref name="scene"/>.</summary>
        public static Box LayerBox(Box scene, int canvasWidth, int x, int y, int w, int h)
        {
            float k = scene.Width / Math.Max(1, canvasWidth);
            return new Box(scene.Left + (x * k), scene.Top + (y * k), scene.Left + ((x + w) * k), scene.Top + ((y + h) * k));
        }

        /// <summary>
        /// Which layer a tap at (<paramref name="px"/>, <paramref name="py"/>) hits: the topmost of <paramref name="layers"/>
        /// (listed back to front, each with its box on the screen) whose picture's alpha at that point is at least
        /// <see cref="HitAlpha"/>; −1 for none (a transparent area never chooses an object). <paramref name="alpha"/> gives a
        /// layer's alpha (one byte a pixel, rows from the top) with its size, or null while it is missing.
        /// </summary>
        public static int HitObject(IReadOnlyList<Box> layers, Func<int, (byte[] Alpha, int Width, int Height)?> alpha, float px, float py)
        {
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                Box box = layers[i];
                if (px < box.Left || px >= box.Right || py < box.Top || py >= box.Bottom)
                {
                    continue;
                }

                (byte[] Alpha, int Width, int Height)? pixels = alpha(i);
                if (pixels == null)
                {
                    continue;
                }

                int x = Math.Min(pixels.Value.Width - 1, Math.Max(0, (int)((px - box.Left) / box.Width * pixels.Value.Width)));
                int y = Math.Min(pixels.Value.Height - 1, Math.Max(0, (int)((py - box.Top) / box.Height * pixels.Value.Height)));
                if (pixels.Value.Alpha[(y * pixels.Value.Width) + x] >= HitAlpha)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// How much the card shrinks about its top's middle so the chosen object's bottom (<paramref name="objectBottom"/>)
        /// clears the sheet's top (<paramref name="sheetTop"/>) by 0.03 W, the whole composition kept: 1 when it already
        /// does, never below 0.35.
        /// </summary>
        public static float Lift(Box card, float objectBottom, float sheetTop, float w)
        {
            float room = sheetTop - (0.03f * w) - card.Top;
            float need = objectBottom - card.Top;
            return need <= room || need <= 0f ? 1f : Math.Max(0.35f, room / need);
        }

        /// <summary>The lift <paramref name="seconds"/> after the sheet opened: from 1 to <paramref name="target"/>, eased.</summary>
        public static float LiftAt(float target, float seconds) => 1f + ((target - 1f) * Ease(seconds / LiftSeconds));

        /// <summary>
        /// How far new reveal <paramref name="index"/> (in its scene's order) has come back to colour
        /// <paramref name="seconds"/> after its scene showed: 0 grey, 1 colour.
        /// </summary>
        public static float Reveal(float seconds, int index) =>
            Math.Max(0f, Math.Min(1f, (seconds - RevealDelay - (index * RevealStagger)) / RevealSeconds));

        /// <summary>Whether reveals of <paramref name="count"/> objects are still playing <paramref name="seconds"/> after the scene showed.</summary>
        public static bool Revealing(float seconds, int count) => count > 0 && seconds < RevealDelay + ((count - 1) * RevealStagger) + RevealSeconds;

        /// <summary>
        /// The share of a card's step still to slide <paramref name="seconds"/> after a turn (1 at the turn, 0 settled), eased
        /// out: the card snaps into place.
        /// </summary>
        public static float Snap(float seconds) => 1f - Ease(seconds / SnapSeconds);

        /// <summary>The golden outline's alpha <paramref name="seconds"/> after the tap: one bright pulse, then steady.</summary>
        public static float OutlineAlpha(float seconds)
        {
            if (seconds < 0f)
            {
                return 0f;
            }

            if (seconds < 0.25f)
            {
                return seconds / 0.25f;
            }

            return seconds < 0.8f ? 1f - (0.15f * ((seconds - 0.25f) / 0.55f)) : 0.85f;
        }

        /// <summary>
        /// Turns straight-alpha RGBA bytes into a locked layer in place (the pack's lockedStyle): greyscale with the luma of
        /// <see cref="OwnerPictures.GreyPixels"/>, at <see cref="LockedBrightness"/>, the alpha kept.
        /// </summary>
        public static void LockedPixels(byte[] rgba)
        {
            for (int i = 0; i + 3 < rgba.Length; i += 4)
            {
                int l = ((299 * rgba[i]) + (587 * rgba[i + 1]) + (114 * rgba[i + 2]) + 500) / 1000;
                int g = (int)((l * LockedBrightness) + 0.5f);
                byte v = (byte)(g > 255 ? 255 : g);
                rgba[i] = v;
                rgba[i + 1] = v;
                rgba[i + 2] = v;
            }
        }

        private static float Ease(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return 1f - ((1f - t) * (1f - t) * (1f - t));
        }
    }
}
