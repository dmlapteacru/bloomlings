using System;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bloomlings.Client.UI
{
    /// <summary>Tap or scroll (spec 005 FR-041), the playtest's <c>IPainter.Scroll</c> and <c>TouchGesture</c>.</summary>
    public static partial class UiKit
    {
        /// <summary>
        /// The EventSystem's drag threshold at <c>touch.slop</c> in the screen's pixels (<see cref="TouchGesture.SlopPixels"/>:
        /// 10 dp; Unity's default 10 px is a third of that on a 420 dpi phone): past it a finger on a page that scrolls drags
        /// it (<see cref="SwipePager"/>) and the button it went down on gets no click.
        /// </summary>
        public static void ApplyTouchSlop(EventSystem? system)
        {
            if (system != null)
            {
                system.pixelDragThreshold = Mathf.Max(1, Mathf.RoundToInt(TouchGesture.SlopPixels(Screen.dpi, Screen.width)));
            }
        }

        /// <summary>
        /// Makes <paramref name="page"/> (a page's root, or its list) a page that scrolls (<see cref="SwipePager"/>):
        /// <paramref name="turn"/> gets +1 (the next page) or −1 (the previous one) for a swipe that started inside
        /// <paramref name="area"/> (the page itself when null); without it a drag only never taps.
        /// </summary>
        public static SwipePager Scrolls(GameObject page, Action<int>? turn = null, RectTransform? area = null)
        {
            SwipePager pager = page.GetComponent<SwipePager>();
            if (pager == null)
            {
                pager = page.AddComponent<SwipePager>();
            }

            pager.Turn = turn;
            pager.Area = area;
            return pager;
        }
    }

    /// <summary>
    /// A page that scrolls (spec 005 FR-041; the playtest's <c>IPainter.Scroll</c>): its drag handler takes every drag that
    /// starts on the page or on a button in it, so once a finger moves past the EventSystem's drag threshold
    /// (<see cref="UiKit.ApplyTouchSlop"/>, <c>touch.slop</c>) the button it went down on gets its pointer up and no click:
    /// a drag never taps. A drag at least <c>touch.swipe</c> long along its main direction that started inside
    /// <see cref="Area"/> turns the page (<see cref="TouchGesture.PageStep"/>: up or left the next page, down or right the
    /// previous one). Taps (a finger lifting within the threshold) go to the buttons as before.
    /// </summary>
    public sealed class SwipePager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Turns the page: +1 the next page, −1 the previous one; null: the drag only never taps.</summary>
        public Action<int>? Turn { get; set; }

        /// <summary>Where a swipe must start to turn the page (the list); null: anywhere on the page.</summary>
        public RectTransform? Area { get; set; }

        public void OnBeginDrag(PointerEventData e)
        {
        }

        public void OnDrag(PointerEventData e)
        {
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (Turn == null || (Area != null && !RectTransformUtility.RectangleContainsScreenPoint(Area, e.pressPosition, null!)))
            {
                return;
            }

            // Screen pixels with y up: a finger moving up has a positive dy, the kit's y runs down.
            float dx = e.position.x - e.pressPosition.x;
            float dy = e.position.y - e.pressPosition.y;
            int step = TouchGesture.PageStep(dx, -dy, TouchGesture.SwipePixels(Screen.dpi, Screen.width));
            if (step != 0)
            {
                Turn(step);
            }
        }
    }
}
