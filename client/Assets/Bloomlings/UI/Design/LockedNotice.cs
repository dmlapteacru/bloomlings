using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The notice of a locked place of the bottom menu (spec 005 FR-030, contracts/look.md §6.7; the owner's request of
    /// 2026-10-04: "on entering the menu's place the page must say that it is only available after reaching level N"),
    /// both builds (<c>Kit.LockedNotice</c>, <c>UiKit.LockedNotice</c>), top to bottom and centered in its area:
    /// <list type="bullet">
    /// <item><description><see cref="Icon"/>: the place's icon (the owner's picture, <see cref="OwnerPictures.NavIcon"/>, or
    /// its stand-in glyph), <see cref="IconShare"/> of the area's width, smaller when the area is short;</description></item>
    /// <item><description><see cref="Badge"/>: the padlock badge on the icon's lower right (<see cref="BottomNav.LockBox"/>);</description></item>
    /// <item><description><see cref="Message"/>: "Available from level N" (<c>locked.message</c>) in <c>type.title</c>
    /// <c>ink.brown</c>, <see cref="MessageTextShare"/> of its box tall and shrunk to its width;</description></item>
    /// <item><description><see cref="Hint"/>: "Keep playing to unlock it!" (<c>locked.hint</c>) in <c>type.body</c>
    /// <c>ink.brown_soft</c>, <see cref="HintTextShare"/> of its box tall and shrunk to its width.</description></item>
    /// </list>
    /// Every share is of the area's width, so the notice keeps its proportions in screen pixels and in canvas units alike.
    /// It is never a touch target. Engine-free.
    /// </summary>
    public sealed record LockedNoticeRegions(Box Area, Box Icon, Box Badge, Box Message, Box Hint)
    {
        /// <summary>The icon's side, as a share of the area's width (the most it takes).</summary>
        public const float IconShare = 0.4f;

        /// <summary>The smallest icon side on a short area, as a share of its width (below it the whole notice shrinks).</summary>
        public const float MinIconShare = 0.2f;

        /// <summary>The gap under the icon, as a share of the area's width.</summary>
        public const float IconGapShare = 0.05f;

        /// <summary>The message's line, as a share of the area's width.</summary>
        public const float MessageShare = 0.1f;

        /// <summary>The gap between the message and the hint, as a share of the area's width.</summary>
        public const float HintGapShare = 0.012f;

        /// <summary>The hint's line, as a share of the area's width.</summary>
        public const float HintShare = 0.07f;

        /// <summary>The text lines' width, as a share of the area's width.</summary>
        public const float TextWidthShare = 0.94f;

        /// <summary>The message's letters, as a share of its line's height (before they shrink to its width).</summary>
        public const float MessageTextShare = 0.72f;

        /// <summary>The hint's letters, as a share of its line's height (before they shrink to its width).</summary>
        public const float HintTextShare = 0.66f;

        /// <summary>
        /// The content height of a card holding the notice in its body (the locked Leaderboard and Collection cards), in
        /// reference units: the whole notice at its full size on a card's body (about 850 units wide) with a little room
        /// above and below.
        /// </summary>
        public const float CardContent = 600f;

        /// <summary>The notice's parts in their screen order, top to bottom.</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered => new[] { ("Icon", Icon), ("Message", Message), ("Hint", Hint) };

        /// <summary>The whole notice: from the icon's top to the hint's bottom, as wide as its widest part.</summary>
        public Box Bounds => new Box(Math.Min(Icon.Left, Message.Left), Icon.Top, Math.Max(Icon.Right, Message.Right), Hint.Bottom);
    }

    /// <summary>
    /// A locked page of the bottom menu (spec 005 FR-030, contracts/look.md §6.7: the Store page before L12, the Wardrobe
    /// before L40), both builds: the page header (<see cref="Header"/>: back, the page's banner, the Petals pill), the
    /// page's panel (<see cref="Panel"/>, the Store page's: from under the header row to the bottom of the screen) and,
    /// in it, the notice's area (<see cref="Notice"/>, where the Store page's list would be: above the bottom menu's top,
    /// <see cref="NavTop"/>), laid out by <see cref="LockedNoticeRegions"/>. Engine-free.
    /// </summary>
    public sealed record LockedPageRegions(Box Safe, float W, PageHeader Header, Box Panel, Box Notice, float NavTop)
    {
        /// <summary>The panel's corner radius on the Store page (a card's, <c>radius.card</c> of its width, at least <c>radius.card_min</c>).</summary>
        public float PanelRadius(float scale) => Math.Max(DesignTokens.Radius.CardMin * scale, Panel.Width * DesignTokens.Radius.Card);
    }

    /// <content>The locked places' notice and pages (contracts/look.md §6.7).</content>
    public static partial class ScreenLayout
    {
        /// <summary>
        /// The locked notice in <paramref name="area"/> (<see cref="LockedNoticeRegions"/>), in shares of the area's width A:
        /// the icon <c>0.4A</c> square, then <c>0.05A</c> lower the message's line <c>0.1A</c> tall, <c>0.012A</c> lower
        /// the hint's line <c>0.07A</c> tall, both <c>0.94A</c> wide; the stack centered in the area. On a shorter area the
        /// icon shrinks to fit, down to <c>0.2A</c>; below that the whole stack shrinks with it, so everything stays inside
        /// the area. The padlock badge is <see cref="Design.BottomNav.LockBox"/> of the icon.
        /// </summary>
        public static LockedNoticeRegions LockedNotice(Box area)
        {
            float a = Math.Max(1f, area.Width);
            float text = (LockedNoticeRegions.IconGapShare + LockedNoticeRegions.MessageShare + LockedNoticeRegions.HintGapShare + LockedNoticeRegions.HintShare) * a;
            float icon = Math.Min(LockedNoticeRegions.IconShare * a, area.Height - text);
            float k = 1f;
            if (icon < LockedNoticeRegions.MinIconShare * a)
            {
                k = Math.Max(0f, area.Height) / ((LockedNoticeRegions.MinIconShare * a) + text);
                icon = LockedNoticeRegions.MinIconShare * a * k;
            }

            float stack = icon + (text * k);
            float top = area.CenterY - (stack / 2f);
            Box iconBox = Box.FromCenter(area.CenterX, top + (icon / 2f), icon, icon);
            float width = LockedNoticeRegions.TextWidthShare * a;
            float messageTop = iconBox.Bottom + (LockedNoticeRegions.IconGapShare * a * k);
            Box message = new Box(area.CenterX - (width / 2f), messageTop, area.CenterX + (width / 2f), messageTop + (LockedNoticeRegions.MessageShare * a * k));
            float hintTop = message.Bottom + (LockedNoticeRegions.HintGapShare * a * k);
            Box hint = new Box(message.Left, hintTop, message.Right, hintTop + (LockedNoticeRegions.HintShare * a * k));
            return new LockedNoticeRegions(area, iconBox, Design.BottomNav.LockBox(iconBox), message, hint);
        }

        /// <summary>
        /// A locked page (<see cref="LockedPageRegions"/>): the Store page's layout without its tabs and offline line
        /// (<see cref="ReferenceStore"/>), its list's box holding the notice: the page header, the panel from <c>0.03W</c>
        /// under the header row to the bottom of the screen, and the notice's area <c>0.88W</c> wide from <c>0.045W</c>
        /// under the panel's top to <c>0.02W</c> over the bottom menu's top.
        /// </summary>
        public static LockedPageRegions LockedPage(float width, float height, Insets insets)
        {
            ReferenceStoreRegions store = ReferenceStore(width, height, insets, hasCosmetics: false, hasStatus: false);
            return new LockedPageRegions(store.Safe, store.W, store.Header, store.Panel, store.List, store.NavTop);
        }
    }
}
