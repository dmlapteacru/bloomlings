using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>What a finger did from going down to lifting (<see cref="TouchGesture.Lift"/>).</summary>
    public enum GestureKind
    {
        /// <summary>Nothing: no finger was down, or the touch was cancelled.</summary>
        None,

        /// <summary>A tap: the finger lifted within the slop of where it went down (or it went down off a page that scrolls).</summary>
        Tap,

        /// <summary>A drag on a page that scrolls: it never taps; a long enough one turns the page (<see cref="GestureEnd.Page"/>).</summary>
        Drag,
    }

    /// <summary>How a touch ended: a tap where the finger lifted, or a drag and the page it turns.</summary>
    public readonly struct GestureEnd
    {
        public GestureEnd(GestureKind kind, float x, float y, float dx, float dy, int page)
        {
            Kind = kind;
            X = x;
            Y = y;
            Dx = dx;
            Dy = dy;
            Page = page;
        }

        public GestureKind Kind { get; }

        /// <summary>Where the finger lifted (a tap's point, as the hosts dispatched it before).</summary>
        public float X { get; }

        public float Y { get; }

        /// <summary>How far the finger moved from where it went down, in pixels (y down).</summary>
        public float Dx { get; }

        public float Dy { get; }

        /// <summary>A drag's page turn: +1 the next page, −1 the previous one, 0 none (<see cref="TouchGesture.PageStep"/>).</summary>
        public int Page { get; }
    }

    /// <summary>
    /// Tap or scroll (spec 005 FR-041, the owner's request of 2026-10-06: "In the cosmetics shop I try to scroll, and the
    /// cells are pressed instead and every cosmetic is bought in a row"), both builds: a finger that goes down on a page
    /// that scrolls (the Store, the Wardrobe, the Collection, the Leaderboard, the profile's edit card) taps only when it
    /// lifts within <c>touch.slop</c> of where it went down; once it moves farther the touch is a drag, which never taps
    /// (nothing looks pressed any more) and, at least <c>touch.swipe</c> long along its main direction, turns a paged
    /// list's page (<see cref="PageStep"/>). A finger that goes down anywhere else taps where it lifts, as before (the
    /// board, the tray and the buttons of a level keep their quick taps). The playtest's painter runs it on the host's
    /// touches; Unity gets the same from its <c>EventSystem</c> (<c>pixelDragThreshold</c> from <see cref="SlopPixels"/>)
    /// and a drag handler on each page (<c>SwipePager</c>). Pixels are the host's, y down. Engine-free.
    /// </summary>
    public sealed class TouchGesture
    {
        private float _startX;
        private float _startY;

        /// <param name="slopPixels">The slop in the host's pixels (<see cref="SlopPixels"/>).</param>
        /// <param name="swipePixels">The page-turning drag in the host's pixels (<see cref="SwipePixels"/>).</param>
        public TouchGesture(float slopPixels, float swipePixels)
        {
            Slop = Math.Max(0f, slopPixels);
            Swipe = Math.Max(Slop, swipePixels);
        }

        /// <summary>A gesture with the tokens' slop and swipe for a screen of <paramref name="dpi"/> (0: unknown) and <paramref name="screenWidth"/> pixels.</summary>
        public static TouchGesture For(float dpi, float screenWidth) => new TouchGesture(SlopPixels(dpi, screenWidth), SwipePixels(dpi, screenWidth));

        /// <summary><c>touch.slop</c> in a host's pixels (<see cref="DesignTokens.Touch.Pixels"/>).</summary>
        public static float SlopPixels(float dpi, float screenWidth) => DesignTokens.Touch.Pixels(DesignTokens.Touch.SlopDp, dpi, screenWidth);

        /// <summary><c>touch.swipe</c> in a host's pixels.</summary>
        public static float SwipePixels(float dpi, float screenWidth) => DesignTokens.Touch.Pixels(DesignTokens.Touch.SwipeDp, dpi, screenWidth);

        public float Slop { get; }

        public float Swipe { get; }

        /// <summary>Whether a finger is down.</summary>
        public bool Down { get; private set; }

        /// <summary>Whether the finger went down on a page that scrolls (only such a touch can become a drag).</summary>
        public bool Scrolls { get; private set; }

        /// <summary>Whether the touch is a drag: on a page that scrolls, the finger has moved farther than the slop.</summary>
        public bool Dragging { get; private set; }

        /// <summary>Where the finger went down.</summary>
        public (float X, float Y) Start => (_startX, _startY);

        /// <summary>A finger goes down at (x, y); <paramref name="scrolls"/> when that point lies on a page that scrolls.</summary>
        public void Press(float x, float y, bool scrolls)
        {
            Down = true;
            Scrolls = scrolls;
            Dragging = false;
            _startX = x;
            _startY = y;
        }

        /// <summary>The finger moves to (x, y): on a page that scrolls, past the slop the touch becomes a drag, for good.</summary>
        public void Move(float x, float y)
        {
            if (Down && Scrolls && !Dragging && Beyond(x - _startX, y - _startY, Slop))
            {
                Dragging = true;
            }
        }

        /// <summary>The finger lifts at (x, y): a drag (with its page turn) or a tap there; nothing without a finger down.</summary>
        public GestureEnd Lift(float x, float y)
        {
            if (!Down)
            {
                return new GestureEnd(GestureKind.None, x, y, 0f, 0f, 0);
            }

            Move(x, y);
            Down = false;
            float dx = x - _startX;
            float dy = y - _startY;
            if (Dragging)
            {
                Dragging = false;
                return new GestureEnd(GestureKind.Drag, x, y, dx, dy, PageStep(dx, dy, Swipe));
            }

            return new GestureEnd(GestureKind.Tap, x, y, dx, dy, 0);
        }

        /// <summary>The touch is cancelled (the system took it, a second finger came): nothing taps.</summary>
        public void Cancel()
        {
            Down = false;
            Dragging = false;
        }

        /// <summary>
        /// The page a drag of (dx, dy) pixels (y down) turns, along its main direction once at least
        /// <paramref name="swipe"/> long: a finger moving up or to the left brings the next page (+1), as a list scrolls on
        /// with it; down or to the right the previous one (−1); a shorter drag none (0).
        /// </summary>
        public static int PageStep(float dx, float dy, float swipe)
        {
            float ax = Math.Abs(dx);
            float ay = Math.Abs(dy);
            float along = Math.Max(ax, ay);
            if (along < swipe || along <= 0f)
            {
                return 0;
            }

            float d = ay >= ax ? dy : dx;
            return d < 0f ? 1 : -1;
        }

        private static bool Beyond(float dx, float dy, float distance) => (dx * dx) + (dy * dy) > distance * distance;
    }
}
