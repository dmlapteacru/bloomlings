using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using SkiaSharp;

namespace Bloomlings.ArtGen
{
    /// <summary>
    /// The review sheet (FR-023): every 2D character in every mood, the launch characters at play size with their shape
    /// differences, and the 3D heroes and the group. Written to <c>tools/artgen/out/sheet.png</c> (gitignored).
    /// </summary>
    public static class Sheet
    {
        private const int Cell = 132;
        private const int Pad = 24;

        public static byte[] Render(string folder, IReadOnlyList<string> all)
        {
            string fontPath = Path.Combine(folder, "..", "..", "..", "..", "UI", "Fonts", "Resources", "Nunito-ExtraBold.ttf");
            using SKTypeface typeface = SKTypeface.FromFile(fontPath) ?? SKTypeface.Default;
            using var title = new SKFont(typeface, 30);
            using var label = new SKFont(typeface, 17);
            using var text = new SKPaint { Color = new SKColor(0x3A, 0x24, 0x18), IsAntialias = true };
            using var sampling = new SKPaint { IsAntialias = true };
            var filter = new SKSamplingOptions(SKCubicResampler.Mitchell);

            VariantInfo[] variants = VariantCatalog.Default.All.ToArray();
            IReadOnlyList<CharacterMood> moods = CharacterArt.Moods;
            int labelWidth = 150;
            int gridWidth = labelWidth + (moods.Count * Cell);
            int smallTop = Pad + 50 + (variants.Length * Cell) + 40;
            int heroTop = smallTop + 150;
            int heroW = CharacterArt.HeroWidth / 2;
            int heroH = CharacterArt.HeroHeight / 2;
            int groupW = CharacterArt.GroupWidth * 3 / 4;
            int groupH = CharacterArt.GroupHeight * 3 / 4;
            int width = Math.Max(gridWidth + 420, Math.Max(4 * heroW, groupW)) + (2 * Pad);
            int height = heroTop + 40 + (2 * heroH) + 50 + groupH + Pad;

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(new SKColor(0xFB, 0xF4, 0xE4));

            void Text(string s, float x, float y, SKFont font) => canvas.DrawText(s, x, y, SKTextAlign.Left, font, text);

            void Picture(string name, SKRect box)
            {
                string file = Path.Combine(folder, name + ".png");
                if (!File.Exists(file))
                {
                    Text("missing " + name, box.Left, box.MidY, label);
                    return;
                }

                using SKImage image = SKImage.FromEncodedData(file);
                canvas.DrawImage(image, box, filter, sampling);
            }

            Text("Bloomlings characters (spec 004): 2D variants in every mood", Pad, Pad + 30, title);
            for (int m = 0; m < moods.Count; m++)
            {
                Text(CharacterArt.MoodName(moods[m]), Pad + labelWidth + (m * Cell) + 30, Pad + 62, label);
            }

            using var tile = new SKPaint { IsAntialias = true };
            for (int v = 0; v < variants.Length; v++)
            {
                float top = Pad + 70 + (v * Cell);
                Text(variants[v].IconId + (variants[v].Status == VariantStatus.Launch ? string.Empty : " (exp.)"), Pad, top + (Cell / 2f), label);
                for (int m = 0; m < moods.Count; m++)
                {
                    var box = SKRect.Create(Pad + labelWidth + (m * Cell), top, Cell - 8, Cell - 8);
                    if (m == 0)
                    {
                        Rgba t = DesignTokens.CharacterTile(Rgba.FromHex(variants[v].ColorHex));
                        tile.Color = new SKColor(t.R, t.G, t.B, t.A);
                        canvas.DrawRoundRect(box, 14, 14, tile);
                    }

                    Picture(CharacterArt.Picture2D(variants[v].IconId, moods[m]), box);
                }
            }

            // The launch characters at play size (48 px) and their shape differences.
            Text("Launch characters at 48 px", Pad, smallTop, label);
            VariantInfo[] launch = variants.Where(v => v.Status == VariantStatus.Launch).ToArray();
            for (int i = 0; i < launch.Length; i++)
            {
                Picture(CharacterArt.Picture2D(launch[i].IconId, CharacterMood.Happy), SKRect.Create(Pad + (i * 60), smallTop + 16, 48, 48));
            }

            float listX = Pad + gridWidth + 40;
            Text("Shape difference at 48 px (least first)", listX, Pad + 92, label);
            int row = 0;
            try
            {
                foreach ((string a, string b, double d) in ArtCheck.ShapeDifferences(folder).OrderBy(p => p.Difference).Take(40))
                {
                    Text($"{a} / {b}: {d:P1}{(d < ArtCheck.MinShapeDifference ? "  FAIL" : string.Empty)}", listX, Pad + 122 + (row * 24), label);
                    row++;
                }
            }
            catch (FileNotFoundException)
            {
                Text("(2D set incomplete)", listX, Pad + 122, label);
            }

            // The 3D heroes, with and without faces, and the group.
            Text("3D heroes (meta screens only)", Pad, heroTop, title);
            for (int f = 0; f < CharacterArt.Families.Count; f++)
            {
                Family family = CharacterArt.Families[f];
                Picture(CharacterArt.Hero(family), SKRect.Create(Pad + (f * heroW), heroTop + 20, heroW, heroH));
                Picture(CharacterArt.Hero(family, blank: true), SKRect.Create(Pad + (f * heroW), heroTop + 20 + heroH, heroW, heroH));
            }

            Picture(CharacterArt.Group, SKRect.Create(Pad, heroTop + 40 + (2 * heroH), groupW, groupH));

            using SKImage snapshot = surface.Snapshot();
            using SKData data = snapshot.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }
}
