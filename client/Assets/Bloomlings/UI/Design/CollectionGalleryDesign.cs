using System;

namespace Bloomlings.Client.UI.Design
{
    public sealed record CollectionGalleryRegions(Box Safe, float W, PageHeader Header, Box Count, Box Area, Box Viewport)
    {
        public float RowHeight => W * .60f;
        public Box Row(int row) => new Box(Area.Left, Area.Top + row * RowHeight, Area.Right, Area.Top + (row + 1) * RowHeight);
        public Box Cell(int index)
        {
            Box row = Row(index / 2);
            float center = row.Left + row.Width * (index % 2 == 0 ? .265f : .735f);
            return Box.FromCenter(center, row.CenterY - W * .014f, W * .39f, W * .53f);
        }
        public float ContentHeight(int count) => Math.Max(Area.Height, ((count + 1) / 2) * RowHeight + W * .025f);
        public float Maximum(int count) => Math.Max(0, ContentHeight(count) - Area.Height);
        public (int First, int Last) VisibleRows(int count, float offset) =>
            (Math.Max(0, (int)(offset / RowHeight) - 1), Math.Min((count + 1) / 2 - 1, (int)Math.Ceiling((offset + Area.Height) / RowHeight) + 1));
        public Box Picture
        {
            get
            {
                float side = Math.Min(W * .82f, Area.Height - W * .36f);
                return Box.FromCenter(Area.CenterX, Area.Top + W * .035f + side * .5f, side, side);
            }
        }
        public Box Name => Box.FromCenter(Area.CenterX, Picture.Bottom + W * .09f, W * .80f, W * .13f);
        public Box Level => Box.FromCenter(Area.CenterX, Name.Bottom + W * .055f, W * .84f, W * .07f);
        public Box DetailClose => Box.FromCenter(Area.Right - W * .06f, Area.Top + W * .06f, W * .105f, W * .105f);
    }

    /// <summary>Large collectible mosaics in a two-column garden cabinet; only visible shelves are drawn.</summary>
    public static class CollectionGalleryDesign
    {
        public static readonly TypeStyle Caption = new TypeStyle("type.shop.gallery_caption", 40, true, false, 0, 20);
        public static CollectionGalleryRegions Page(float width, float height, Insets insets)
        {
            var page = ScreenLayout.ReferenceStore(width, height, insets, false, false);
            float w = page.W, top = page.Header.Row.Bottom + w * .075f;
            Box count = Box.FromCenter(page.Safe.CenterX, top + w * .055f, w * .68f, w * .11f);
            Box area = new Box(page.Safe.Left + w * .025f, count.Bottom + w * .035f, page.Safe.Right - w * .025f, page.NavTop - w * .025f);
            return new CollectionGalleryRegions(page.Safe, w, page.Header, count, area, new Box(area.Left, area.Top, area.Right, page.Safe.Bottom));
        }
        public static Box Art(Box cell) => new Box(cell.Left, cell.Top, cell.Right, cell.Top + cell.Width);
        public static Box Name(Box cell) => ProfileGameDesign.In(cell, -.025f, .755f, 1.025f, .895f);
        public static Box Level(Box cell) => ProfileGameDesign.In(cell, .03f, .905f, .97f, .99f);
    }
}
