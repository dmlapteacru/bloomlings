using System;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using SkiaSharp;

namespace Bloomlings.ArtGen
{
    /// <summary>Renders any picture of the set by its name (<see cref="CharacterArt"/>) to PNG bytes.</summary>
    public static class ArtSet
    {
        public static byte[] Render(string name)
        {
            if (name.StartsWith("2d/", StringComparison.Ordinal))
            {
                string stem = name.Substring(3);
                int dash = stem.LastIndexOf('-');
                string icon = stem.Substring(0, dash);
                CharacterMood mood = CharacterArt.Moods.First(m => CharacterArt.MoodName(m) == stem.Substring(dash + 1));
                VariantInfo variant = VariantCatalog.Default.All.First(v => v.IconId == icon);
                using SKBitmap bitmap = Characters2D.Render(variant, mood);
                return Png.Encode(bitmap);
            }

            (int w, int h) = CharacterArt.SizeOf(name);
            using SKBitmap hero = Png.FromRgba(w, h, Render3D(name, 1));
            return Png.Encode(hero);
        }

        /// <summary>
        /// A fresh render's straight-alpha RGBA rows, for the art check. 3D pictures render only every
        /// <paramref name="rowStride"/>-th row (rows are independent, so those rows equal a full render's).
        /// </summary>
        public static (int Width, int Height, byte[] Rgba) Pixels(string name, int rowStride)
        {
            if (name.StartsWith("2d/", StringComparison.Ordinal))
            {
                return Png.Decode(Render(name));
            }

            (int w, int h) = CharacterArt.SizeOf(name);
            return (w, h, Render3D(name, rowStride));
        }

        private static byte[] Render3D(string name, int rowStride)
        {
            if (name == CharacterArt.Group)
            {
                return Heroes3D.Group(rowStride);
            }

            string hero = name.Substring(3);
            bool blank = hero.EndsWith("-blank", StringComparison.Ordinal);
            string familyName = blank ? hero.Substring(0, hero.Length - 6) : hero;
            Family family = CharacterArt.Families.First(f => CharacterArt.FamilyName(f) == familyName);
            return Heroes3D.Solo(family, blank, rowStride);
        }
    }
}
