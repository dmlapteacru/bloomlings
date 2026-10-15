using System;
using System.Diagnostics;
using System.IO;
using Bloomlings.AssetPack;
using NUnit.Framework;
using SkiaSharp;

public sealed class TextureTests
{
    [Test]
    public void PhoneProfilePreservesAtlasDetailAndNeverUpscalesOrResizesHeroFrames()
    {
        Assert.That(PhoneTextureProfile.Size("Assets/heromotion/bloom-idle-01.png", 384, 384, 512), Is.EqualTo((384, 384)));
        Assert.That(PhoneTextureProfile.Size("Assets/shop/offer-atlas.png", 2048, 1365, 1024), Is.EqualTo((1024, 682)));
        Assert.That(PhoneTextureProfile.Size("Assets/backgrounds/home.jpg", 852, 1846, 1024), Is.EqualTo((852, 1846)));
        Assert.That(PhoneTextureProfile.Size("Assets/icons/booster.png", 64, 48, 1024), Is.EqualTo((64, 48)));
    }

    [Test]
    public void PackerActuallyResizesPngAndJpegAndReusesTheirVerifiedOutputs()
    {
        string root = Path.Combine(Path.GetTempPath(), "bloomlings-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var pixels = new SKBitmap(new SKImageInfo(1024, 512, SKColorType.Rgba8888, SKAlphaType.Premul));
            pixels.Erase(new SKColor(230, 70, 120, 128));
            using var view = pixels.PeekPixels();
            using var png = view.Encode(SKEncodedImageFormat.Png, 100)!;
            using var jpeg = view.Encode(SKEncodedImageFormat.Jpeg, 90)!;
            File.WriteAllBytes(Path.Combine(root, "icon.png"), png.ToArray());
            File.WriteAllBytes(Path.Combine(root, "avatar.jpg"), jpeg.ToArray());
            string manifest = Path.Combine(root, "input.txt"), output = Path.Combine(root, "out");
            File.WriteAllLines(manifest, new[] { Path.Combine(root, "icon.png") + "|Assets/icons/icon.png", Path.Combine(root, "avatar.jpg") + "|Assets/avatars/avatar.jpg" });
            void Pack()
            {
                var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add(typeof(PhoneTextureProfile).Assembly.Location);
                start.ArgumentList.Add(manifest); start.ArgumentList.Add(output); start.ArgumentList.Add("1024");
                using var process = Process.Start(start)!;
                string text = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                Assert.That(process.WaitForExit(30000), Is.True);
                Assert.That(process.ExitCode, Is.Zero, text);
            }
            Pack();
            string icon = Path.Combine(output, "Assets/icons/icon.png"), avatar = Path.Combine(output, "Assets/avatars/avatar.jpg");
            using var a = SKBitmap.Decode(icon); using var b = SKBitmap.Decode(avatar);
            Assert.That((a.Width, a.Height), Is.EqualTo((384, 192)));
            Assert.That((b.Width, b.Height), Is.EqualTo((256, 128)));
            Assert.That(a.GetPixel(100, 100).Alpha, Is.InRange(127, 129));
            byte[] original = File.ReadAllBytes(icon); DateTime modified = File.GetLastWriteTimeUtc(icon);
            Pack();
            Assert.That(File.ReadAllBytes(icon), Is.EqualTo(original));
            Assert.That(File.GetLastWriteTimeUtc(icon), Is.EqualTo(modified));
        }
        finally { Directory.Delete(root, true); }
    }
}
