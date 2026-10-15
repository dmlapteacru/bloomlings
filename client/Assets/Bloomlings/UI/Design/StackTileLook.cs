namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared stack silhouette and counter geometry, fully inside the board cell.</summary>
    public static class StackTileLook
    {
        public static Box Face(Box tile) => new Box(tile.Left, tile.Top, tile.Right, tile.Bottom - tile.Height * 0.14f);
        public static Box Tier(Box tile, int tier) => new Box(tile.Left + tile.Width * 0.04f * tier,
            tile.Bottom - tile.Height * (0.22f - 0.06f * tier), tile.Right - tile.Width * 0.04f * tier,
            tile.Bottom - tile.Height * (0.07f - 0.06f * tier));
        public static Box Counter(Box tile) => Box.FromCenter(Face(tile).CenterX, Face(tile).CenterY,
            tile.Width * 0.70f, tile.Height * 0.78f);
    }
}
