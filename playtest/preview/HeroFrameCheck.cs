using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;
using Bloomlings.Playtest.Design;
using SkiaSharp;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// Checks the animated heroes' frames (spec 005 FR-028) as both painters draw them: every baked frame is embedded,
    /// decodes with the playtest's own palette decoder (<see cref="PalettePng"/>, the device's path too) to the size the
    /// kit's data gives (<see cref="HeroMotion.Frame"/>), and to the same straight-alpha pixels as SkiaSharp's PNG decoder.
    /// </summary>
    public static class HeroFrameCheck
    {
        public static IEnumerable<string> Run()
        {
            System.Reflection.Assembly assembly = typeof(HeroFrameCheck).Assembly;
            foreach (Family family in CharacterArt.Families)
            {
                foreach (MotionClip clip in HeroMotion.Clips)
                {
                    for (int i = 0; i < HeroMotion.FrameCount(family, clip); i++)
                    {
                        HeroFrame frame = HeroMotion.Frame(family, clip, i);
                        string name = Visuals.MotionFrame(frame);
                        string? problem = Check(assembly, name, frame);
                        if (problem != null)
                        {
                            yield return "hero frame " + name + ": " + problem;
                        }
                    }
                }
            }
            var twig = new List<HeroFrame> { TwigGearArt.Frame(null) };
            foreach (string id in new[] { TwigGearArt.AcornHatId, TwigGearArt.FlowerCapId })
            {
                var hat = new CosmeticItem(id, CosmeticKind.Hat, id, "", "#FFFFFF", false);
                twig.Add(TwigGearArt.Frame(Outfit.None with { Hat = hat }));
                twig.Add(TwigGearArt.Icon(id)!.Value);
            }
            foreach (HeroFrame frame in twig)
            {
                string name = Visuals.MotionFrame(frame);
                string? problem = Check(assembly, name, frame, allowFullColor: true);
                if (problem != null) yield return "Twig sprite " + name + ": " + problem;
            }
        }

        private static string? Check(System.Reflection.Assembly assembly, string name, HeroFrame frame, bool allowFullColor = false)
        {
            byte[] png;
            using (Stream? stream = PlaytestFiles.Open(PainterBase.SpriteResource(name)))
            {
                if (stream == null)
                {
                    return "not embedded";
                }

                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                png = memory.ToArray();
            }

            PalettePicture? own = PalettePng.Decode(png);
            if (own == null)
            {
                if (allowFullColor)
                {
                    // Same fallback as the painters: the APK packer must preserve every RGBA pixel and its alpha.
                    using var image = SKBitmap.Decode(png, new SKImageInfo(frame.Width, frame.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
                    using Stream? original = assembly.GetManifestResourceStream(PainterBase.SpriteResource(name));
                    using var masterImage = original != null ? SKBitmap.Decode(original, new SKImageInfo(frame.Width, frame.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul)) : null;
                    using var codec = SKCodec.Create(new SKMemoryStream(png));
                    if (image == null || masterImage == null || codec == null) return "full-color PNG does not decode";
                    if (codec.Info.Width != frame.Width || codec.Info.Height != frame.Height) return "full-color dimensions differ from the kit";
                    return image.Bytes.AsSpan().SequenceEqual(masterImage.Bytes) ? null : "full-color pixels differ after packaging";
                }
                return "not an 8-bit palette PNG";
            }

            if (own.Width != frame.Width || own.Height != frame.Height)
            {
                return "decodes to " + own.Width + " x " + own.Height + ", the kit's data says " + frame.Width + " x " + frame.Height;
            }

            // Compare shipped PNG/BPF1 frames with the original master PNG, independently of the packer's verification.
            using Stream? master = assembly.GetManifestResourceStream(PainterBase.SpriteResource(name));
            using SKBitmap? skia = master != null ? SKBitmap.Decode(master, new SKImageInfo(own.Width, own.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul)) : null;
            if (skia == null)
            {
                return "SkiaSharp does not decode it";
            }

            var mine = new byte[own.Width * own.Height * 4];
            own.Expand(mine, own.Width, premultiplied: false);
            byte[] theirs = skia.Bytes;
            int differ = 0;
            for (int i = 0; i < mine.Length && i < theirs.Length; i++)
            {
                differ += mine[i] != theirs[i] ? 1 : 0;
            }

            return differ == 0 && theirs.Length == mine.Length ? null : differ + " bytes differ from SkiaSharp's decode";
        }
    }
}
