using System;
using System.Globalization;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The Leaderboard as a page (preview frames 5 and 31; spec 002 FR-023, spec 005 FR-030; the owner's request of
    /// 2026-10-04: "All the menu's places must be a separate page. Not popups.") in the reference layout
    /// (contracts/look.md §6.8, <see cref="ScreenLayout.ReferenceLeaderboard"/>), opened from the bottom menu's
    /// Leaderboard (open from L10); its back (and the system back) returns to Home:
    /// <list type="bullet">
    /// <item><description>the Store page's frame: the Wardrobe's garden, the page header (<see cref="Kit.PageHeader"/>: the
    /// back button, the wooden "Leaderboard" banner with ivy and the Petals pill, whose "+" opens the Store page once it is
    /// open, its back returning here) and the parchment panel to the bottom of the screen;</description></item>
    /// <item><description>on the panel, in its offline form (the server is deferred): the top ranks with gold, silver and
    /// bronze medals as placeholder rows (no invented players), the gap, and the player's own row highlighted as "You"
    /// with the hero's portrait and the highest completed level (research R12);</description></item>
    /// <item><description>the offline line and the cream Refresh with ⟳, which says the leaderboard is offline;</description></item>
    /// <item><description>the bottom menu over the panel's foot, the Leaderboard in its medallion (FR-030).</description></item>
    /// </list>
    /// Before the Leaderboard unlocks (L10) the bottom menu's Leaderboard still opens the page, locked (<see cref="Locked"/>):
    /// the same garden, header and panel, the panel holding the locked notice ("Available from level 10") instead of the
    /// ranks, the offline line and Refresh. Unity's twin is <c>LeaderboardScreen</c>.
    /// </summary>
    public static class LeaderboardScreen
    {
        /// <summary>
        /// The top ranks the offline Leaderboard shows as placeholder rows on <paramref name="r"/>: as many as fill its rows'
        /// box with the gap and the player's own row (the owner's choice of 2026-10-04: the list over the panel's whole
        /// height), at least one.
        /// </summary>
        public static int PlaceholderRanks(ReferenceLeaderboardRegions r) => Math.Max(1, r.LinesFitting - 2);

        public static void Draw(IPainter p, DesignApp app)
        {
            HomeLook look = HomeScreen.Look(app);
            if (!BottomNav.IsOpen(NavPlace.Leaderboard, look))
            {
                Locked(p, app, look);
                return;
            }

            ReferenceLeaderboardRegions r = ScreenLayout.ReferenceLeaderboard(p.Width, p.Height, p.Insets);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, app.Meta.CurrentLevel, OwnerPictures.Wardrobe);

            // The parchment panel, its bottom corners below the screen's edge (the Store page's).
            float radius = r.PanelRadius(p.Scale);
            Kit.CardFrame(p, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));

            // The rows fill the rows' box: the placeholder ranks, the gap and the player's own row. They grow their letters
            // with them (sized for a RowTypeShare row).
            int ranks = PlaceholderRanks(r);
            int lines = ranks + 2;
            float grow = r.RowHeight(lines) / (r.W * ReferenceLeaderboardRegions.RowTypeShare);
            for (int i = 0; i < ranks; i++)
            {
                Box line = r.Row(i, lines);
                LeaderboardRowParts parts = ReferenceLeaderboardRegions.Parts(line);
                Kit.Row(p, line, false);
                Rank(p, parts, i + 1, grow);
                Portrait(p, parts.Portrait, null);
                Placeholder(p, Box.FromCenter(parts.Name.Left + (parts.Name.Width * 0.38f), line.CenterY, parts.Name.Width * 0.76f, line.Height * 0.3f));
                Placeholder(p, Box.FromCenter(parts.Score.CenterX, line.CenterY, parts.Score.Width * 0.6f, line.Height * 0.3f));
            }

            Box gap = r.Row(ranks, lines);
            p.Text("…", gap.CenterX, gap.CenterY, T.Title, C.InkBrownSoft, sizeScale: grow);

            Box you = r.Row(ranks + 1, lines);
            LeaderboardRowParts own = ReferenceLeaderboardRegions.Parts(you);
            Kit.Row(p, you, highlighted: true);
            p.Text("—", own.Rank.CenterX, you.CenterY, T.Body, C.InkBrown, own.Rank.Width, grow);
            Portrait(p, own.Portrait, null);
            Kit.AvatarPicture(p, own.Portrait, app.Meta.Profile.Avatar);
            p.TextLeft(PlaytestText.T("leaderboard.you"), own.Name.Left, you.CenterY, T.ButtonSecondary, C.InkBrown, own.Name.Width, grow, TextLook.Plain(C.InkBrown));
            p.Text(NumberText.Group(app.Meta.Progression.HighestCompletedLevel), own.Score.CenterX, you.CenterY, T.Count, C.InkBrown, own.Score.Width, grow, TextLook.Plain(C.InkBrown));
            // The flowers over the own row's top corners (spec 005 FR-047, the owner's references of 2026-10-08).
            float flowers = you.Height * YouFlowerShare;
            Kit.CornerFlowers(p, CardLook.CornerBox(you, Corner.TopLeft, flowers), Corner.TopLeft);
            Kit.CornerFlowers(p, CardLook.CornerBox(you, Corner.TopRight, flowers), Corner.TopRight);

            // The ranks are a list that scrolls: a drag over them never taps (spec 005 FR-041).
            p.Scroll(r.Rows);

            // The offline line and Refresh: the playtest has no leaderboard server, so Refresh says it is offline.
            p.Text(PlaytestText.T("leaderboard.offline_empty"), r.Status.CenterX, r.Status.CenterY, T.Caption, C.InkBrownSoft, r.Status.Width);
            Kit.SecondaryButton(p, r.Refresh, PlaytestText.T("leaderboard.refresh"), () => app.HomeToast(PlaytestText.T("leaderboard.offline_empty")), "ui.restart");

            // The bottom menu, the Leaderboard in its medallion (FR-030); the header last, as on the other pages.
            Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Leaderboard), look, app.Navigate);
            Kit.PageFlowers(p, r.Panel, ScreenLayout.BottomNavTop(p.Width, p.Height, p.Insets));
            Kit.PageHeader(p, r.Header, PlaytestText.T("leaderboard.title"), app.CloseLeaderboard, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Panel.Top, r.Safe.Right, r.Rows.Bottom), toast);
            }
        }

        /// <summary>The flowers over the own row's corners, as a share of its height.</summary>
        public const float YouFlowerShare = 0.62f;

        /// <summary>
        /// The locked Leaderboard page (spec 005 FR-030, contracts/look.md §6.7; <see cref="ScreenLayout.LockedPage"/>): the
        /// Wardrobe's garden, the parchment panel holding the locked notice (<see cref="Kit.LockedNotice"/>: the
        /// Leaderboard's icon with its padlock, "Available from level N" from the roadmap) where the ranks would be, the
        /// bottom menu with the Leaderboard raised, and the header with its back and the Petals pill ("+" once the Store is
        /// open).
        /// </summary>
        private static void Locked(IPainter p, DesignApp app, HomeLook look)
        {
            LockedPageRegions r = ScreenLayout.LockedPage(p.Width, p.Height, p.Insets);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, app.Meta.CurrentLevel, OwnerPictures.Wardrobe);
            float radius = r.PanelRadius(p.Scale);
            Kit.CardFrame(p, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            Kit.LockedNotice(p, r.Notice, NavPlace.Leaderboard, app.UnlockLevel(NavPlace.Leaderboard));
            Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Leaderboard), look, app.Navigate);
            Kit.PageFlowers(p, r.Panel, ScreenLayout.BottomNavTop(p.Width, p.Height, p.Insets));
            Kit.PageHeader(p, r.Header, PlaytestText.T("leaderboard.title"), app.CloseLeaderboard, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Panel.Top, r.Safe.Right, r.Notice.Bottom), toast);
            }
        }

        /// <summary>
        /// A row's rank (<see cref="LeaderboardRowParts.Rank"/>): ranks 1–3 on their outlined medal (<c>ui.medal</c>, gold,
        /// silver, bronze) with the number in <c>type.badge</c>, the others as a brown number.
        /// </summary>
        private static void Rank(IPainter p, LeaderboardRowParts parts, int rank, float grow)
        {
            Rgba? medal = C.Medal(rank);
            string number = rank.ToString(CultureInfo.InvariantCulture);
            if (medal.HasValue)
            {
                Box box = parts.Medal;
                Func<float, float, float> sdf = ShapeLibrary.Get("ui.medal");
                p.ShapeOf("ui.medal/line/0.06", (x, y) => sdf(x, y) - 0.06f, box, medal.Value.Darken(0.42f));
                p.Shape("ui.medal", box, medal.Value);
                p.Text(number, box.CenterX, box.CenterY + (box.Height * 0.11f), T.Badge, medal.Value.Darken(0.55f), box.Width, grow);
            }
            else
            {
                p.Text(number, parts.Rank.CenterX, parts.Rank.CenterY, T.Body, C.InkBrown, parts.Rank.Width, grow, TextLook.Plain(C.InkBrown));
            }
        }

        /// <summary>
        /// A portrait on a cream rounded square (the avatar's shape, <see cref="AvatarLook.Radius"/>; the owner, 2026-10-06;
        /// it was round): the player's hero, or the anonymous figure of a placeholder row.
        /// </summary>
        private static void Portrait(IPainter p, Box face, Family? family)
        {
            float size = face.Width;
            float cx = face.CenterX;
            float cy = face.CenterY;
            float ring = Math.Max(1f, size * 0.044f);
            float r = AvatarLook.Radius(face);
            p.FillRound(face.Inset(-ring).Offset(0f, ring * 0.8f), r + ring, C.CreamLip);
            p.FillRound(face.Inset(-ring), r + ring, C.CreamLine);
            p.FillRoundGradient(face, r, C.CreamTop, C.CreamFace);
            if (family.HasValue)
            {
                Visuals.Hero(p, Box.FromCenter(cx, cy + (size * 0.02f), size * 0.92f, size * 0.92f), family.Value, null);
            }
            else
            {
                p.Shape("ui.person", Box.FromCenter(cx, cy + (size * 0.04f), size * 0.7f, size * 0.7f), C.CreamLine);
            }
        }

        /// <summary>A sunk parchment bar where a name or a score will show once the leaderboard is online.</summary>
        private static void Placeholder(IPainter p, Box box)
        {
            p.FillRound(box, box.Height / 2f, C.ParchmentWell);
            p.StrokeRound(box.Inset(0.5f), (box.Height / 2f) - 0.5f, Math.Max(1f, p.U(2f)), C.ParchmentEdge);
        }
    }
}
