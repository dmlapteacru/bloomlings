using System;
using System.IO;
using SkiaSharp;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>Contact sheets of actual UI renders at the same scale; never changes the game configuration.</summary>
    public static class PurchaseComparison
    {
        public static void Write(string root)
        {
            string[] folders = { "current", "minus-15", "minus-25", "minus-50" };
            string[] titles = { "Сейчас · 100%", "−15% · 85%", "−25% · 75%", "−50% · 50%" };
            using var face = SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Bold);
            using var font = new SKFont(face, 22);
            using var small = new SKFont(face, 17);
            using var ink = new SKPaint { Color = new SKColor(0x57, 0x35, 0x20), IsAntialias = true };
            using var paint = new SKPaint { IsAntialias = true };
            const int col = 400, gap = 18, pad = 24, width = pad * 2 + 4 * col + 3 * gap;
            void Header(SKCanvas canvas, string text, float x, float y, SKFont type) => canvas.DrawText(text, x, y, type, ink);
            void Save(SKSurface surface, string filename)
            {
                using SKImage result = surface.Snapshot();
                using SKData data = result.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(Path.Combine(root, filename), data.ToArray());
            }
            using (var sheet = SKSurface.Create(new SKImageInfo(width, 960)))
            {
                var canvas = sheet.Canvas; canvas.Clear(new SKColor(0xF6, 0xEF, 0xE1));
                Header(canvas, "Размер плашек покупки · Shop сохраняет текущий размер", pad, 35, font);
                for (int i = 0; i < 4; i++) Header(canvas, titles[i], pad + i * (col + gap), 82, font);
                var rows = new[] {
                    ("Геймплей · все бусты без зарядов", "86-gameplay-purchase-sizes", new SKRect(90, 1550, 990, 1810), 100f),
                    ("Wardrobe · предметы за петали", "88-wardrobe-purchase-sizes", new SKRect(35, 1545, 1045, 1980), 280f),
                    ("Edit profile · аватарки по три в ряд", "40-profile-edit", new SKRect(40, 1090, 1040, 1540), 590f)
                };
                foreach (var row in rows)
                {
                    Header(canvas, row.Item1, pad, row.Item4 + 23, small);
                    for (int i = 0; i < 4; i++)
                    {
                        using SKImage image = SKImage.FromEncodedData(Path.Combine(root, folders[i], row.Item2 + "-19.5x9.png"));
                        float x = pad + i * (col + gap), y = row.Item4 + 36;
                        float h = row.Item3.Height * col / row.Item3.Width;
                        canvas.DrawImage(image, row.Item3, new SKRect(x, y, x + col, y + h), new SKSamplingOptions(SKFilterMode.Linear), paint);
                    }
                }
                Header(canvas, "Проценты относятся к ширине и высоте плашки. Зона нажатия сохраняется.", pad, 895, small);
                Header(canvas, "В геймплее иконки в уменьшенных вариантах снова занимают центр слота.", pad, 925, small);
                Save(sheet, "purchase-size-comparison.png");
            }
            using (var sheet = SKSurface.Create(new SKImageInfo(width, 990)))
            {
                var canvas = sheet.Canvas; canvas.Clear(new SKColor(0xF6, 0xEF, 0xE1));
                for (int i = 0; i < 4; i++)
                {
                    float x = pad + i * (col + gap); Header(canvas, titles[i], x, 40, font);
                    using SKImage image = SKImage.FromEncodedData(Path.Combine(root, folders[i], "86-gameplay-purchase-sizes-19.5x9.png"));
                    canvas.DrawImage(image, new SKRect(x, 64, x + col, 64 + col * 2340f / 1080), new SKSamplingOptions(SKFilterMode.Linear), paint);
                }
                Save(sheet, "gameplay-size-comparison.png");
            }
        }
    }
}
