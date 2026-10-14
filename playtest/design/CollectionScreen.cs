using System;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>A scrollable garden gallery: large mosaics, live labels and reusable illustrated shelves.</summary>
    public static class CollectionScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            HomeLook look = HomeScreen.Look(app);
            var r = CollectionGalleryDesign.Page(p.Width, p.Height, p.Insets);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, app.Meta.CurrentLevel, OwnerPictures.Wardrobe);
            if (!BottomNav.IsOpen(NavPlace.Collection, look))
            {
                var locked = ScreenLayout.LockedPage(p.Width, p.Height, p.Insets);
                Kit.CardFrame(p, locked.Panel);
                Kit.LockedNotice(p, locked.Notice, NavPlace.Collection, app.UnlockLevel(NavPlace.Collection));
            }
            else
            {
                var entries = app.Meta.Collection.Entries;
                if (app.CollectionDetail >= 0 && app.CollectionDetail < entries.Count)
                {
                    CollectionEntry entry = entries[app.CollectionDetail];
                    p.Mark("collection.detail_frame");
                    Kit.CardFrame(p, new Box(r.Area.Left, r.Area.Top, r.Area.Right, Math.Min(r.Area.Bottom, r.Level.Bottom + r.W * .08f)));
                    Frame(p, r.Picture, entry, app);
                    Kit.SlicedSprite(p, MetaScreenArt.Tag, r.Name, .16f, .30f, .30f);
                    ProfileGamePainter.Label(p, r.Name.Inset(r.W * .03f), PictureName(entry.PictureId), ProfileGameDesign.Name);
                    ProfileGamePainter.Label(p, r.Level, PlaytestText.F("collection.completed", NumberText.Group(entry.LevelNumber)), ProfileGameDesign.Info);
                    Kit.RoundButton(p, r.DetailClose.CenterX, r.DetailClose.CenterY, r.DetailClose.Width, "ui.close", app.CollectionBack);
                }
                else
                {
                    Kit.SlicedSprite(p, MetaScreenArt.Tag, r.Count, .16f, .30f, .30f);
                    ProfileGamePainter.Label(p, r.Count.Inset(r.W * .015f), PlaytestText.F(entries.Count == 1 ? "collection.count_one" : "collection.count_many", entries.Count), ShopCatalogDesign.Name);
                    float maximum = r.Maximum(entries.Count);
                    app.CollectionScrollOffset = Math.Max(0, Math.Min(maximum, app.CollectionScrollOffset));
                    float offset = app.CollectionScrollOffset;
                    var visible = r.VisibleRows(entries.Count, offset);
                    p.PushClip(r.Viewport);
                    for (int row = visible.First; row <= visible.Last; row++)
                    {
                        p.Sprite(MetaScreenArt.Shelf, r.Row(row).Offset(0, -offset));
                        for (int column = 0; column < 2; column++)
                        {
                            int slot = row * 2 + column;
                            int index = entries.Count - 1 - slot;
                            if (index < 0) continue;
                            CollectionEntry entry = entries[index];
                            Box cell = r.Cell(slot).Offset(0, -offset);
                            if (cell.Bottom <= r.Area.Top || cell.Top >= r.Area.Bottom) continue;
                            Frame(p, CollectionGalleryDesign.Art(cell), entry, app);
                            Box name = CollectionGalleryDesign.Name(cell);
                            Kit.SlicedSprite(p, MetaScreenArt.Tag, name, .16f, .30f, .30f);
                            ProfileGamePainter.Label(p, name.Inset(name.Height * .10f), PictureName(entry.PictureId), CollectionGalleryDesign.Caption);
                            ProfileGamePainter.Label(p, CollectionGalleryDesign.Level(cell), PlaytestText.F("common.level", NumberText.Group(entry.LevelNumber)), ProfileGameDesign.Info, ShopBodyDesign.Letters);
                            Box hit = new Box(cell.Left, Math.Max(cell.Top, r.Viewport.Top), cell.Right, Math.Min(cell.Bottom, r.Area.Bottom));
                            if (hit.Height >= p.U(DesignTokens.Size.TouchMin)) p.Hit(hit, () => app.CollectionDetail = index);
                        }
                    }
                    p.PopClip();
                    p.VerticalScroll(r.Viewport, dy => app.CollectionScrollOffset = Math.Max(0, Math.Min(maximum, app.CollectionScrollOffset - dy)));
                }
            }
            Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Collection), look, app.Navigate);
            Kit.PageHeader(p, r.Header, PlaytestText.T("collection.title"), app.CollectionBack, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);
            if (app.HomeToastText != null) Kit.Toast(p, r.Area, app.HomeToastText);
        }

        public static string PictureName(string id)
        {
            string name = id.Replace('_', ' ');
            return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        private static void Frame(IPainter p, Box box, CollectionEntry entry, DesignApp app)
        {
            Box well = Kit.PictureFrame(p, box);
            if (app.Content.TryGetLevel(app.Resolve(entry.LevelNumber), out LevelDefinition? definition) && definition != null)
            {
                var picture = app.Content.GetPicture(definition.Picture);
                Box art = well.Inset(well.Width * .04f);
                p.Mark("tile.picture");
                if (p is ICachedIllustration cache)
                    cache.Illustration("collection/" + app.Content.ContentVersion + "/" + entry.LevelNumber + "/" + entry.PictureId
                        + "/" + entry.PictureVersion + "/" + entry.MappingHash,
                        art, () => BoardPainter.Picture(p, art, definition, picture, shadow: false));
                else BoardPainter.Picture(p, art, definition, picture, shadow: false);
            }
        }
    }
}
