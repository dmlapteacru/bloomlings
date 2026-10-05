using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Where one guided step's pieces go on the screen (<see cref="Spotlight.Layout"/>), in screen pixels, top-down.</summary>
    public readonly struct SpotlightLayout
    {
        public SpotlightLayout(Box hole, float radius, Box bubble, bool tailDown, float tailX, Box hand)
        {
            Hole = hole;
            Radius = radius;
            Bubble = bubble;
            TailDown = tailDown;
            TailX = tailX;
            Hand = hand;
        }

        /// <summary>The lit place: the only place a forced step takes a tap.</summary>
        public Box Hole { get; }

        /// <summary>The hole's corner radius.</summary>
        public float Radius { get; }

        /// <summary>The parchment message bubble.</summary>
        public Box Bubble { get; }

        /// <summary>The bubble sits above the hole, its tail pointing down at it (else below it, the tail up).</summary>
        public bool TailDown { get; }

        /// <summary>Where the tail meets the bubble's edge, toward the hole's middle.</summary>
        public float TailX { get; }

        /// <summary>The pointing hand of a forced step (its fingertip on the hole's bottom edge); empty otherwise.</summary>
        public Box Hand { get; }
    }

    /// <summary>
    /// The guided spotlight (spec 005 FR-035, contracts/look.md §6.10; the owner, 2026-10-05: "dim the screen and
    /// highlight it"): the whole screen dimmed by <c>surface.scrim</c> at <see cref="ScrimAlpha"/> but for one lit hole
    /// with a soft edge (<see cref="UiRaster.SpotlightScrim"/>), a <c>garden.glow</c> ring breathing round the hole, a
    /// parchment bubble with the message (and an icon) above or below it with a tail pointing at it, and for a forced step a
    /// pointing hand bobbing under the hole. A step that is not forced says "Tap to continue" in its bubble. Both builds
    /// lay it out here. Engine-free.
    /// </summary>
    public static class Spotlight
    {
        /// <summary>The scrim's alpha outside the hole (over <c>surface.scrim</c>'s color).</summary>
        public const float ScrimAlpha = 0.8f;

        /// <summary>The lit room kept round the target, in reference units.</summary>
        public const float PadUnits = 14f;

        /// <summary>The hole's corner radius, in reference units.</summary>
        public const float RadiusUnits = 30f;

        /// <summary>The corner radius of the holes over single tiles (the tiles that block the way), in reference units.</summary>
        public const float CellRadiusUnits = 8f;

        /// <summary>The soft edge of the hole, in reference units.</summary>
        public const float FeatherUnits = 12f;

        /// <summary>The bubble's width as a share of the safe width.</summary>
        public const float BubbleShare = 0.88f;

        /// <summary>One message line's height, in reference units.</summary>
        public const float LineUnits = 58f;

        /// <summary>The bubble's inner padding, in reference units.</summary>
        public const float BubblePadUnits = 30f;

        /// <summary>The bubble's icon (a tile or a booster) side, in reference units.</summary>
        public const float IconUnits = 120f;

        /// <summary>The "Tap to continue" caption under the message, in reference units.</summary>
        public const float CaptionUnits = 48f;

        /// <summary>The bubble's tail length, and the gap between the tail's tip and the hole, in reference units.</summary>
        public const float TailUnits = 30f;

        public const float GapUnits = 14f;

        /// <summary>The room kept for the top bar above a bubble, in reference units below the safe top.</summary>
        public const float TopBarUnits = 170f;

        /// <summary>The pointing hand's side, in reference units.</summary>
        public const float HandUnits = 130f;

        /// <summary>The scrim picture is drawn at this share of the screen's size, then stretched (its edge is soft anyway).</summary>
        public const float RasterShare = 0.25f;

        /// <summary>The ring's breath, in seconds.</summary>
        public const float PulseSeconds = 1.2f;

        /// <summary>The lit hole round <paramref name="target"/>: <see cref="PadUnits"/> of room on every side.</summary>
        public static Box HoleAround(Box target, float scale) => target.Inset(-PadUnits * scale);

        /// <summary>The message's width inside the bubble (the icon takes its room at the left).</summary>
        public static float TextWidth(float width, float height, Insets insets, bool icon)
        {
            float u = DesignTokens.ScaleFor(width, height);
            float bubble = ScreenLayout.SafeArea(width, height, insets).Width * BubbleShare;
            return bubble - (2f * BubblePadUnits * u) - (icon ? (IconUnits + BubblePadUnits) * u : 0f);
        }

        /// <summary>
        /// Lays out a step lighting <paramref name="hole"/> with a message of <paramref name="lines"/> lines: the bubble above
        /// the hole when it fits under the top bar, else below it (under the hand of a forced step), else at the bottom of the
        /// safe area; the hand's fingertip on the hole's bottom edge.
        /// </summary>
        public static SpotlightLayout Layout(float width, float height, Insets insets, Box hole, int lines, bool icon, bool caption, bool hand)
        {
            float u = DesignTokens.ScaleFor(width, height);
            Box safe = ScreenLayout.SafeArea(width, height, insets);
            float bubbleWidth = safe.Width * BubbleShare;
            float text = (Math.Max(1, lines) * LineUnits) + (caption ? CaptionUnits : 0f);
            float bubbleHeight = (2f * BubblePadUnits * u) + (Math.Max(text, icon ? IconUnits : 0f) * u);
            float reach = (TailUnits + GapUnits) * u;
            float handSide = HandUnits * u;
            Box handBox = hand
                ? new Box(hole.CenterX - (handSide * 0.3f), hole.Bottom - (handSide * 0.08f), hole.CenterX + (handSide * 0.7f), hole.Bottom + (handSide * 0.92f))
                : new Box(0f, 0f, 0f, 0f);

            // The bubble stays in the middle of the screen; its tail turns toward the hole.
            float left = safe.Left + ((safe.Width - bubbleWidth) / 2f);
            float aboveTop = hole.Top - reach - bubbleHeight;
            bool tailDown;
            float top;
            if (aboveTop >= safe.Top + (TopBarUnits * u))
            {
                tailDown = true;
                top = aboveTop;
            }
            else
            {
                tailDown = false;
                float below = (hand ? handBox.Bottom : hole.Bottom) + reach;
                top = Math.Min(below, safe.Bottom - bubbleHeight - (GapUnits * u));
            }

            var bubble = new Box(left, top, left + bubbleWidth, top + bubbleHeight);
            float tailX = Clamp(hole.CenterX, bubble.Left + (BubblePadUnits * 2f * u), bubble.Right - (BubblePadUnits * 2f * u));
            float radius = Math.Min(RadiusUnits * u, Math.Min(hole.Width, hole.Height) / 2f);
            return new SpotlightLayout(hole, radius, bubble, tailDown, tailX, handBox);
        }

        /// <summary>The ring's breath, 0 to 1 and back every <see cref="PulseSeconds"/>.</summary>
        public static float Pulse(float seconds) => 0.5f - (0.5f * (float)Math.Cos(seconds * 2.0 * Math.PI / PulseSeconds));

        /// <summary>The hand bobbing toward the hole: its box moved up and down by a tenth of its side.</summary>
        public static Box HandAt(SpotlightLayout layout, float seconds) =>
            layout.Hand.Offset(0f, layout.Hand.Height * 0.08f * (float)Math.Sin(seconds * 6.0));

        /// <summary>The scrim picture's size for a screen side.</summary>
        public static int RasterSize(float pixels) => Math.Max(8, (int)Math.Round(pixels * RasterShare));

        /// <summary>The scrim picture's cache key: the holes in raster pixels, so each step renders once.</summary>
        public static string ScrimKey(IReadOnlyList<Box> holes, float width, float height)
        {
            float k = RasterShare;
            var key = new System.Text.StringBuilder("ui.spotlight/" + RasterSize(width) + "x" + RasterSize(height));
            foreach (Box hole in holes)
            {
                key.Append('/').Append((int)(hole.Left * k)).Append(',').Append((int)(hole.Top * k)).Append(',').Append((int)(hole.Right * k)).Append(',').Append((int)(hole.Bottom * k));
            }

            return key.ToString();
        }

        /// <summary>Renders the scrim with <paramref name="holes"/> for a screen of <paramref name="width"/> × <paramref name="height"/> at a picture size.</summary>
        public static byte[] Scrim(IReadOnlyList<Box> holes, float width, float height, int pictureWidth, int pictureHeight, float radiusUnits = RadiusUnits)
        {
            float sx = pictureWidth / Math.Max(1f, width);
            float sy = pictureHeight / Math.Max(1f, height);
            float u = DesignTokens.ScaleFor(width, height);
            var scaled = new List<Box>();
            foreach (Box hole in holes)
            {
                scaled.Add(new Box(hole.Left * sx, hole.Top * sy, hole.Right * sx, hole.Bottom * sy));
            }

            return UiRaster.SpotlightScrim(pictureWidth, pictureHeight, scaled, radiusUnits * u * sx, Math.Max(1f, FeatherUnits * u * sx));
        }

        /// <summary>The box holding every hole (the bubble and the hand point at it).</summary>
        public static Box Union(IReadOnlyList<Box> boxes)
        {
            if (boxes.Count == 0)
            {
                return new Box(0f, 0f, 0f, 0f);
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

        private static float Clamp(float v, float min, float max) => max < min ? min : Math.Max(min, Math.Min(max, v));
    }
}
