using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Original RGBA art; Unity, Skia and Android crop the same atlas coordinates at runtime.</summary>
    public static class MetaScreenArt
    {
        public const string Stage = "shop/meta-wardrobe-stage-v1";
        public const string Podium = "shop/meta-leaderboard-podium-v1";
        public const string Shelf = "shop/meta-shelf";
        public const string Plank = "shop/meta-plank";
        public const string Base = "shop/meta-base";
        public const string Tag = "shop/meta-tag";
        public static bool Is(string name) => name.StartsWith("shop/meta-", StringComparison.Ordinal);
        public static string Resource(string name) => name.StartsWith("shop/meta-hat-",StringComparison.Ordinal) ? "meta-hats-atlas-v1" : name == Stage || name == Podium ? name.Substring(5) : "meta-shelf-atlas-v1";
        private static readonly string[] HatIds = {"hat.sprout_cap","hat.leaf_cap","hat.acorn_cap","hat.straw_hat","hat.flower_crown","hat.mushroom_cap","hat.night_cap","hat.golden_crown","hat.star_crown","hat.bucket_hat","hat.bell_cap"};
        private static readonly Box[] HatBounds = {
            new Box(76,53,393,325),new Box(488,73,838,328),new Box(912,44,1265,326),
            new Box(25,389,465,628),new Box(479,405,868,611),new Box(898,383,1279,619),
            new Box(48,644,454,878),new Box(478,641,860,875),new Box(895,665,1278,885),
            new Box(31,915,435,1151),new Box(470,881,840,1153)};
        public static string? Hat(string id) {int index=Array.IndexOf(HatIds,id);return index<0?null:"shop/meta-hat-"+index;}
        public static Box FitHat(string name,Box box)
        {
            var crop=Crop(name,1306,1204);float scale=Math.Min(box.Width/crop.Width,box.Height/crop.Height);
            return Box.FromCenter(box.CenterX,box.CenterY,crop.Width*scale,crop.Height*scale);
        }
        public static (int Left, int Top, int Width, int Height) Crop(string name, int w, int h)
        {
            if(name.StartsWith("shop/meta-hat-",StringComparison.Ordinal))
            {
                int index=int.Parse(name.Substring("shop/meta-hat-".Length),System.Globalization.CultureInfo.InvariantCulture);
                Box hat=HatBounds[index];return ((int)(hat.Left*w/1306),(int)(hat.Top*h/1204),(int)(hat.Width*w/1306),(int)(hat.Height*h/1204));
            }
            // Crops leave original RGBA source pixels unchanged.
            Box b = name switch
            {
                Shelf => new Box(40f/1774,133f/887,915f/1774,483f/887),
                Plank => new Box(955f/1774,208f/887,1730f/1774,393f/887),
                Base => new Box(160f/1774,588f/887,778f/1774,781f/887),
                Tag => new Box(1056f/1774,645f/887,1633f/1774,781f/887),
                _ => new Box(0, 0, 1, 1),
            };
            return ((int)(b.Left*w), (int)(b.Top*h), Math.Max(1,(int)(b.Width*w)), Math.Max(1,(int)(b.Height*h)));
        }
        public static (float X, float Y) Borders(string name) => name == Plank || name == Tag ? (.16f, .30f) : (0, 0);
    }
}
