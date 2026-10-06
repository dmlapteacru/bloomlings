using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The Collection as a page (preview frames 6 and 20; spec 002 FR-024, spec 005 FR-030; the owner's request of
    /// 2026-10-04: "All the menu's places must be a separate page. Not popups.") in the reference layout
    /// (contracts/look.md §6.9, <see cref="ScreenLayout.ReferenceCollection"/>), opened from the bottom menu's Collection
    /// (open once a picture is won, here also from Level 2 while it is empty, <see cref="HomeScreen.Look"/>):
    /// <list type="bullet">
    /// <item><description>the Store page's frame: the Wardrobe's garden, the page header (<see cref="Kit.PageHeader"/>: the
    /// back button, the wooden "Collection" banner with ivy and the Petals pill, whose "+" opens the Store page once it is
    /// open, its back returning here) and the parchment panel to the bottom of the screen;</description></item>
    /// <item><description>on the panel, the count ("N pictures") and the finished pictures newest first in their cream
    /// frames (<see cref="Kit.PictureFrame"/>), three to a row and as large as fit, as many rows as the page holds, with
    /// "n / m" between the cream page arrows when they take more than one page;</description></item>
    /// <item><description>a tap on a picture shows its detail on the page: the picture large in its frame, its name and
    /// "Completed at Level N". The back button (and the system back) returns to the grid, a second back to Home
    /// (<see cref="DesignApp.CollectionBack"/>). It only shows pictures: it is never a level selector;</description></item>
    /// <item><description>the bottom menu over the panel's foot, the Collection in its medallion (FR-030).</description></item>
    /// </list>
    /// Before its first picture the bottom menu's Collection still opens the page, locked (<see cref="Locked"/>): the same
    /// garden, header and panel, the panel holding the locked notice ("Available from level 2") instead of the count and
    /// the pictures. Unity's twin is <c>CollectionScreen</c>.
    /// </summary>
    public static class CollectionScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            HomeLook look = HomeScreen.Look(app);
            if (!BottomNav.IsOpen(NavPlace.Collection, look))
            {
                Locked(p, app, look);
                return;
            }

            ReferenceCollectionRegions r = ScreenLayout.ReferenceCollection(p.Width, p.Height, p.Insets);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, app.Meta.CurrentLevel, OwnerPictures.Wardrobe);

            // The parchment panel, its bottom corners below the screen's edge (the Store page's).
            float radius = r.PanelRadius(p.Scale);
            Kit.Paper(p, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius), radius, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);

            IReadOnlyList<CollectionEntry> entries = app.Meta.Collection.Entries;
            if (app.CollectionDetail >= 0 && app.CollectionDetail < entries.Count)
            {
                Detail(p, app, r, entries[app.CollectionDetail]);
            }
            else
            {
                Grid(p, app, r, entries);
            }

            // The bottom menu, the Collection in its medallion (FR-030); the header last, as on the other pages. Its back
            // leaves a picture's detail for the grid, then the page for Home.
            Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Collection), look, app.Navigate);
            Kit.PageHeader(p, r.Header, PlaytestText.T("collection.title"), app.CollectionBack, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Panel.Top, r.Safe.Right, r.Area.Bottom), toast);
            }
        }

        /// <summary>
        /// The grid: the count line, then a page of pictures newest first (<see cref="ReferenceCollectionRegions.PerPage"/>),
        /// each a tap target opening its detail, and the footer "n / m" between the page arrows when there are more pages.
        /// </summary>
        private static void Grid(IPainter p, DesignApp app, ReferenceCollectionRegions r, IReadOnlyList<CollectionEntry> entries)
        {
            string count = entries.Count == 1 ? PlaytestText.F("collection.count_one", 1) : PlaytestText.F("collection.count_many", entries.Count);
            p.Text(count, r.Count.CenterX, r.Count.CenterY, T.Caption, C.InkBrownSoft, r.Count.Width);

            int perPage = r.PerPage(entries.Count);
            int pages = r.Pages(entries.Count);
            int page = Math.Max(0, Math.Min(pages - 1, app.CollectionPage));
            for (int slot = 0; slot < perPage; slot++)
            {
                int index = entries.Count - 1 - ((page * perPage) + slot);
                if (index < 0)
                {
                    break;
                }

                Box box = r.Cell(slot);
                float depth = Kit.Press(p, box, true);
                Kit.Squash(p, box, depth, tile: true);
                Frame(p, box, entries[index], app);
                p.PopTransform();
                p.Hit(box, () =>
                {
                    app.Sound.Play(SoundCue.Click);
                    app.CollectionDetail = index;
                });
            }

            Action? previous = page > 0 ? () => app.CollectionPage = page - 1 : (Action?)null;
            Action? next = page < pages - 1 ? () => app.CollectionPage = page + 1 : (Action?)null;
            if (pages > 1)
            {
                Box line = r.Footer;
                float room = line.Height + p.U(12f);
                p.Text(PlaytestText.F("common.page", page + 1, pages), line.CenterX, line.CenterY, T.Caption, C.InkBrownSoft, line.Width - (2f * room));
                Kit.ArrowButton(p, r.PagePrevious.CenterX, r.PagePrevious.CenterY, r.PagePrevious.Width, next: false, previous);
                Kit.ArrowButton(p, r.PageNext.CenterX, r.PageNext.CenterY, r.PageNext.Width, next: true, next);
            }

            // A drag over the pictures never opens one (spec 005 FR-041); a swipe turns their page.
            p.Scroll(r.Grid, previous, next);
        }

        /// <summary>
        /// A picture's detail (<c>collection.detail_frame</c>, preview frame 20): the picture large in its frame
        /// (<see cref="ReferenceCollectionRegions.Picture"/>), its name in <c>type.title</c> <c>ink.brown</c> and
        /// "Completed at Level N" in <c>type.body</c> <c>ink.brown_soft</c>, centered on the page. No button: back returns
        /// to the grid.
        /// </summary>
        private static void Detail(IPainter p, DesignApp app, ReferenceCollectionRegions r, CollectionEntry entry)
        {
            p.Mark("collection.detail_frame");
            Frame(p, r.Picture, entry, app);
            p.Text(PictureName(entry.PictureId), r.Name.CenterX, r.Name.CenterY, T.Title, C.InkBrown, r.Name.Width, look: TextLook.Plain(C.InkBrown));
            p.Text(PlaytestText.F("collection.completed", NumberText.Group(entry.LevelNumber)), r.Level.CenterX, r.Level.CenterY, T.Body, C.InkBrownSoft, r.Level.Width);
        }

        /// <summary>
        /// The locked Collection page (spec 005 FR-030, contracts/look.md §6.7; <see cref="ScreenLayout.LockedPage"/>): the
        /// Wardrobe's garden, the parchment panel holding the locked notice (<see cref="Kit.LockedNotice"/>: the
        /// Collection's icon with its padlock, "Available from level 2") where the pictures would be, the bottom menu with the
        /// Collection raised, and the header with its back and the Petals pill ("+" once the Store is open).
        /// </summary>
        private static void Locked(IPainter p, DesignApp app, HomeLook look)
        {
            LockedPageRegions r = ScreenLayout.LockedPage(p.Width, p.Height, p.Insets);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, app.Meta.CurrentLevel, OwnerPictures.Wardrobe);
            float radius = r.PanelRadius(p.Scale);
            Kit.Paper(p, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius), radius, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            Kit.LockedNotice(p, r.Notice, NavPlace.Collection, app.UnlockLevel(NavPlace.Collection));
            Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Collection), look, app.Navigate);
            Kit.PageHeader(p, r.Header, PlaytestText.T("collection.title"), app.CollectionBack, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Panel.Top, r.Safe.Right, r.Notice.Bottom), toast);
            }
        }

        /// <summary>A picture's name from its id, in sentence case ("tulip_pot" → "Tulip pot").</summary>
        private static string PictureName(string id)
        {
            string name = id.Replace('_', ' ');
            return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>A finished picture in its cream frame (<see cref="Kit.PictureFrame"/>): the full-color tiles in their stone border.</summary>
        private static void Frame(IPainter p, Box box, CollectionEntry entry, DesignApp app)
        {
            Box well = Kit.PictureFrame(p, box);
            if (app.Content.TryGetLevel(app.Resolve(entry.LevelNumber), out LevelDefinition? definition) && definition != null)
            {
                BoardPainter.Picture(p, well.Inset(well.Width * 0.04f), definition, app.Content.GetPicture(definition.Picture));
            }
        }
    }
}
