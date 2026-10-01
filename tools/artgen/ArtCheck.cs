using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;

namespace Bloomlings.ArtGen
{
    /// <summary>
    /// The art check (research R12): the committed pictures match a fresh render within tolerance, keep a transparent
    /// margin, the launch characters differ in shape at small size, and the manifest matches the files.
    /// </summary>
    public static class ArtCheck
    {
        /// <summary>A pixel differs when a channel moves by more than this.</summary>
        public const int ChannelTolerance = 2;

        /// <summary>At most this share of pixels may differ.</summary>
        public const double PixelTolerance = 0.001;

        /// <summary>The transparent border on every side, as a share of the size.</summary>
        public const double Margin = 0.02;

        /// <summary>The readability mask size (play size of a board tile character).</summary>
        public const int MaskSize = 48;

        /// <summary>
        /// The least shape difference of two launch characters: the share of their joined silhouette (alpha ≥ 128 at
        /// <see cref="MaskSize"/> px) that only one of them covers.
        /// </summary>
        public const double MinShapeDifference = 0.15;

        /// <summary>The check re-renders every this many rows of a 3D picture (rows render independently).</summary>
        public const int RowStride3D = 4;

        /// <summary>How far a hero's face may lie from <see cref="CharacterArt.FaceCenterHero"/>, as a share of the picture.</summary>
        public const double FaceTolerance = 0.02;

        public static List<string> Run(string folder, IReadOnlyList<string> all, IReadOnlyList<string> names)
        {
            var problems = new List<string>();

            // The manifest lists exactly the files present, with their hashes, and the set is complete.
            Manifest manifest = Manifest.Read(folder);
            var listed = manifest.Files.ToDictionary(f => f.Path, StringComparer.Ordinal);
            string[] present = Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(folder, f).Replace('\\', '/')).ToArray()
                : Array.Empty<string>();
            foreach (string name in all)
            {
                if (!present.Contains(name + ".png"))
                {
                    problems.Add($"{name}.png is missing");
                }
            }

            foreach (string file in present)
            {
                if (!listed.TryGetValue(file, out ManifestFile? entry))
                {
                    problems.Add($"{file} is not in {Manifest.FileName}");
                }
                else if (entry.Sha256 != Manifest.Hash(Path.Combine(folder, file)))
                {
                    problems.Add($"{file}: the manifest hash is stale (run build)");
                }
            }

            foreach (string file in listed.Keys.Where(k => !present.Contains(k)))
            {
                problems.Add($"{Manifest.FileName} lists {file}, which does not exist");
            }

            // Each picture re-renders within tolerance and keeps its margin.
            foreach (string name in names)
            {
                string path = Path.Combine(folder, name + ".png");
                if (!File.Exists(path))
                {
                    continue;
                }

                (int ew, int eh) = CharacterArt.SizeOf(name);
                int stride = name.StartsWith("2d/", StringComparison.Ordinal) ? 1 : RowStride3D;
                Compare(name, path, ew, eh, ArtSet.Pixels(name, stride), stride, problems);
            }

            // The launch characters differ in shape at small size, same-family pairs included (FR-022, SC-003).
            bool complete = VariantCatalog.Default.All.Where(v => v.Status == VariantStatus.Launch)
                .All(v => File.Exists(Path.Combine(folder, CharacterArt.Picture2D(v.IconId, CharacterMood.Happy) + ".png")));
            if (complete)
            {
                foreach ((string a, string b, double diff) in ShapeDifferences(folder).Where(p => p.Difference < MinShapeDifference))
                {
                    problems.Add($"{a} and {b} differ in only {diff:P1} of their joined silhouette at {MaskSize} px (at least {MinShapeDifference:P0})");
                }
            }

            // The kit places worn expressions on the heroes' faces where the renderer draws them.
            foreach (Family family in CharacterArt.Families)
            {
                (double x, double y) = Heroes3D.FaceCenter(family);
                (float kx, float ky) = CharacterArt.FaceCenterHero(family);
                if (Math.Abs(x - kx) > FaceTolerance || Math.Abs(y - ky) > FaceTolerance)
                {
                    problems.Add($"CharacterArt.FaceCenterHero({family}) is ({kx:0.00}, {ky:0.00}); the renderer draws the face at ({x:0.00}, {y:0.00})");
                }
            }

            return problems;
        }

        /// <summary>
        /// Compares a committed picture with a fresh render (every <paramref name="stride"/>-th row) within the tolerance,
        /// and checks its size and transparent margin.
        /// </summary>
        public static void Compare(string name, string path, int ew, int eh, (int Width, int Height, byte[] Rgba) fresh, int stride, List<string> problems)
        {
            (int w, int h, byte[] committed) = Png.Decode(File.ReadAllBytes(path));
            if (w != ew || h != eh)
            {
                problems.Add($"{name}: {w} × {h}, expected {ew} × {eh}");
            }

            if (fresh.Width != w || fresh.Height != h)
            {
                problems.Add($"{name}: a fresh render is {fresh.Width} × {fresh.Height}");
                return;
            }

            int differing = 0;
            int compared = 0;
            int worst = 0;
            for (int y = 0; y < h; y += stride)
            {
                for (int i = y * w * 4; i < (y + 1) * w * 4; i += 4)
                {
                    int d = Math.Max(Math.Max(Math.Abs(committed[i] - fresh.Rgba[i]), Math.Abs(committed[i + 1] - fresh.Rgba[i + 1])), Math.Max(Math.Abs(committed[i + 2] - fresh.Rgba[i + 2]), Math.Abs(committed[i + 3] - fresh.Rgba[i + 3])));
                    worst = Math.Max(worst, d);
                    compared++;
                    if (d > ChannelTolerance)
                    {
                        differing++;
                    }
                }
            }

            if (differing > PixelTolerance * compared)
            {
                problems.Add($"{name}: {differing} pixels differ from a fresh render (worst {worst}); run build or review the tool change");
            }

            int border = (int)Math.Ceiling(Margin * Math.Min(w, h));
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool edge = x < border || y < border || x >= w - border || y >= h - border;
                    if (edge && committed[(((y * w) + x) * 4) + 3] > 8)
                    {
                        problems.Add($"{name}: not transparent at ({x}, {y}) inside the {Margin:P0} margin");
                        return;
                    }
                }
            }
        }

        /// <summary>The shape difference of every launch pair: the share of the joined silhouette only one covers.</summary>
        public static IEnumerable<(string A, string B, double Difference)> ShapeDifferences(string folder)
        {
            VariantInfo[] launch = VariantCatalog.Default.All.Where(v => v.Status == VariantStatus.Launch).ToArray();
            var masks = launch.ToDictionary(v => v.IconId, v => Png.Mask(File.ReadAllBytes(Path.Combine(folder, CharacterArt.Picture2D(v.IconId, CharacterMood.Happy) + ".png")), MaskSize));
            for (int a = 0; a < launch.Length; a++)
            {
                for (int b = a + 1; b < launch.Length; b++)
                {
                    bool[] ma = masks[launch[a].IconId];
                    bool[] mb = masks[launch[b].IconId];
                    int only = ma.Zip(mb, (p, q) => p != q).Count(d => d);
                    int joined = ma.Zip(mb, (p, q) => p || q).Count(d => d);
                    yield return (launch[a].IconId, launch[b].IconId, joined == 0 ? 0 : only / (double)joined);
                }
            }
        }
    }
}
