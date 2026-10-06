using System;
using System.IO;
using SkiaSharp;

namespace Bloomlings.AppIcon
{
    /// <summary>
    /// Cuts the owner's app icon picture (a 1254 × 1254 rounded square on white) into the launcher icons:
    /// <list type="bullet">
    /// <item>the full-bleed square: the picture inside its rounded corners (<see cref="CornerInset"/> in from each side),
    /// so no white corner is left; the stores and the launchers mask it themselves;</item>
    /// <item>the adaptive icon's background (108 dp): the full-bleed square at <see cref="AdaptiveShare"/> of the layer,
    /// so the masked 72 dp in the middle shows the picture's middle 94%, over a blurred, enlarged copy that fills the
    /// rest (seen only when the launcher moves the layer); its foreground is transparent;</item>
    /// <item>the round icon: the full-bleed square in a circle; the legacy icon: it in a rounded square, both with a
    /// small transparent margin.</item>
    /// </list>
    /// Writes the Unity client's textures (<c>client/Assets/Bloomlings/Art/Brand/AppIcon/</c>, applied by
    /// <c>CiBuild</c>) and the playtest's mipmaps (<c>playtest/icon/</c>, linked by <c>Playtest.Shared.props</c>).
    /// </summary>
    public static class Program
    {
        /// <summary>How far the picture's rounded corners reach in from each side of the 1254 px source.</summary>
        private const float CornerInset = 96f / 1254f;

        /// <summary>The full-bleed square's share of the adaptive layer: the 72 dp viewport (2/3) shows its middle 94%.</summary>
        private const float AdaptiveShare = (72f / 108f) / 0.94f;

        /// <summary>The round and legacy icons' share of their canvas (a small transparent margin).</summary>
        private const float LegacyShare = 0.92f;

        /// <summary>The legacy icon's corner radius, as a share of its side (the owner's rounded square).</summary>
        private const float LegacyRadius = 0.22f;

        private static readonly (string Folder, int Scale4)[] Densities =
        {
            ("mipmap-mdpi", 4), ("mipmap-hdpi", 6), ("mipmap-xhdpi", 8), ("mipmap-xxhdpi", 12), ("mipmap-xxxhdpi", 16),
        };

        public static int Main(string[] args)
        {
            string root = FindRoot();
            string source = Path.Combine(root, "tools", "appicon", "source", "app-icon.png");
            using SKImage original = SKImage.FromEncodedData(source) ?? throw new FileNotFoundException(source);
            using SKImage full = FullBleed(original);

            string unity = Directory.CreateDirectory(Path.Combine(root, "client", "Assets", "Bloomlings", "Art", "Brand", "AppIcon")).FullName;
            Save(Path.Combine(unity, "app-icon.png"), Resize(full, 1024));
            Save(Path.Combine(unity, "app-icon-adaptive-background.png"), Adaptive(full, 432));
            Save(Path.Combine(unity, "app-icon-adaptive-foreground.png"), Transparent(432));
            Save(Path.Combine(unity, "app-icon-round.png"), Round(full, 432));
            Save(Path.Combine(unity, "app-icon-legacy.png"), Legacy(full, 432));

            string playtest = Path.Combine(root, "playtest", "icon");
            foreach ((string folder, int scale4) in Densities)
            {
                string dir = Directory.CreateDirectory(Path.Combine(playtest, folder)).FullName;
                int icon = 48 * scale4 / 4;
                int layer = 108 * scale4 / 4;
                Save(Path.Combine(dir, "ic_launcher.png"), Legacy(full, icon));
                Save(Path.Combine(dir, "ic_launcher_round.png"), Round(full, icon));
                Save(Path.Combine(dir, "ic_launcher_background.png"), Adaptive(full, layer));
            }

            string anydpi = Directory.CreateDirectory(Path.Combine(playtest, "mipmap-anydpi-v26")).FullName;
            const string Xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                + "<!-- Written by tools/appicon: the owner's app icon (spec 005 pictures.md C3). -->\n"
                + "<adaptive-icon xmlns:android=\"http://schemas.android.com/apk/res/android\">\n"
                + "    <background android:drawable=\"@mipmap/ic_launcher_background\" />\n"
                + "    <foreground android:drawable=\"@android:color/transparent\" />\n"
                + "</adaptive-icon>\n";
            File.WriteAllText(Path.Combine(anydpi, "ic_launcher.xml"), Xml);
            File.WriteAllText(Path.Combine(anydpi, "ic_launcher_round.xml"), Xml);
            Console.WriteLine("app icons written to " + unity + " and " + playtest);
            return 0;
        }

        /// <summary>The picture inside its rounded corners, as a square.</summary>
        private static SKImage FullBleed(SKImage original)
        {
            int inset = (int)MathF.Round(original.Width * CornerInset);
            return original.Subset(new SKRectI(inset, inset, original.Width - inset, original.Height - inset))!;
        }

        private static SKImage Adaptive(SKImage full, int size)
        {
            using SKSurface surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
            SKCanvas canvas = surface.Canvas;
            using (SKImage backdrop = Resize(full, Math.Max(16, size / 4)))
            using (var blur = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(size * 0.02f, size * 0.02f) })
            {
                // The blurred copy, a little larger than the layer so its edges never show.
                float grow = size * 0.08f;
                canvas.DrawImage(backdrop, new SKRect(-grow, -grow, size + grow, size + grow), new SKSamplingOptions(SKFilterMode.Linear), blur);
            }

            int side = (int)MathF.Round(size * AdaptiveShare);
            using SKImage picture = Resize(full, side);
            float at = (size - side) / 2f;
            canvas.DrawImage(picture, at, at);
            return surface.Snapshot();
        }

        private static SKImage Round(SKImage full, int size)
        {
            using SKSurface surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            int side = (int)MathF.Round(size * LegacyShare);
            float at = (size - side) / 2f;
            using SKImage picture = Resize(full, side);
            using var path = new SKPath();
            path.AddCircle(size / 2f, size / 2f, side / 2f);
            canvas.ClipPath(path, antialias: true);
            canvas.DrawImage(picture, at, at);
            return surface.Snapshot();
        }

        private static SKImage Legacy(SKImage full, int size)
        {
            using SKSurface surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            int side = (int)MathF.Round(size * LegacyShare);
            float at = (size - side) / 2f;
            using SKImage picture = Resize(full, side);
            using var clip = new SKRoundRect(new SKRect(at, at, at + side, at + side), side * LegacyRadius);
            canvas.ClipRoundRect(clip, antialias: true);
            canvas.DrawImage(picture, at, at);
            return surface.Snapshot();
        }

        private static SKImage Transparent(int size)
        {
            using SKSurface surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
            surface.Canvas.Clear(SKColors.Transparent);
            return surface.Snapshot();
        }

        /// <summary>A square resize that halves first (box-like), then finishes with Mitchell, so small icons stay crisp.</summary>
        private static SKImage Resize(SKImage image, int size)
        {
            SKImage current = image;
            bool owned = false;
            while (current.Width / 2 >= size * 2)
            {
                SKImage half = Draw(current, current.Width / 2, new SKSamplingOptions(SKFilterMode.Linear));
                if (owned)
                {
                    current.Dispose();
                }

                current = half;
                owned = true;
            }

            SKImage result = Draw(current, size, new SKSamplingOptions(SKCubicResampler.Mitchell));
            if (owned)
            {
                current.Dispose();
            }

            return result;
        }

        private static SKImage Draw(SKImage image, int size, SKSamplingOptions sampling)
        {
            using SKSurface surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
            surface.Canvas.DrawImage(image, new SKRect(0, 0, size, size), sampling);
            return surface.Snapshot();
        }

        private static void Save(string path, SKImage image)
        {
            using (image)
            using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                File.WriteAllBytes(path, data.ToArray());
            }
        }

        private static string FindRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
}
