using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>A contact sheet of every frame, numbered like the design board, for the side-by-side review (SC-001).</summary>
    public static class Sheet
    {
        public static byte[] Render(IReadOnlyList<(int Number, string Title, SKImage Image)> frames)
        {
            const int columns = 6;
            const int thumbWidth = 300;
            int thumbHeight = (int)(thumbWidth * (frames[0].Image.Height / (float)frames[0].Image.Width));
            const int label = 44;
            const int pad = 18;
            int rows = (frames.Count + columns - 1) / columns;
            int width = (columns * (thumbWidth + pad)) + pad;
            int height = (rows * (thumbHeight + label + pad)) + pad;
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(new SKColor(0xF4, 0xF1, 0xEA));
            using var font = new SKFont(SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Bold), 20);
            using var text = new SKPaint { Color = new SKColor(0x2E, 0x34, 0x40), IsAntialias = true };
            using var badge = new SKPaint { Color = new SKColor(0x5E, 0x9F, 0xD3), IsAntialias = true };
            using var white = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var frame = new SKPaint { Color = new SKColor(0, 0, 0, 40), IsAntialias = true };
            for (int i = 0; i < frames.Count; i++)
            {
                int x = pad + ((i % columns) * (thumbWidth + pad));
                int y = pad + ((i / columns) * (thumbHeight + label + pad));
                canvas.DrawCircle(x + 14, y + 16, 14, badge);
                string number = frames[i].Number.ToString();
                canvas.DrawText(number, x + 14 - (font.MeasureText(number) / 2f), y + 23, font, white);
                canvas.DrawText(frames[i].Title, x + 36, y + 24, font, text);
                var rect = new SKRect(x, y + label, x + thumbWidth, y + label + thumbHeight);
                canvas.DrawRoundRect(new SKRect(rect.Left - 2, rect.Top - 2, rect.Right + 2, rect.Bottom + 2), 18, 18, frame);
                canvas.Save();
                canvas.ClipRoundRect(new SKRoundRect(rect, 16, 16), antialias: true);
                canvas.DrawImage(frames[i].Image, rect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                canvas.Restore();
            }

            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }
}
