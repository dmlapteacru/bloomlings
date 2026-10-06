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
    /// margin, the launch characters differ in shape at small size, and the manifest matches the files. The owner's
    /// pictures (spec 005 pictures.md A, recorded by <c>adopt</c>) are checked by size, margin, hash and source record
    /// instead of a fresh render.
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

        /// <summary>
        /// Runs the check over the art folder. <paramref name="all"/> is the generated set and <paramref name="names"/> the
        /// pictures to re-render (<c>--only</c>); owner pictures are never re-rendered but checked by
        /// <see cref="CheckOwner"/>. Hints that are not failures go to <paramref name="notes"/>.
        /// </summary>
        public static List<string> Run(string root, string folder, IReadOnlyList<string> all, IReadOnlyList<string> names, List<string> notes)
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
                    problems.Add($"{file} is not in {Manifest.FileName} (an owner picture: run adopt {file}; otherwise delete it)");
                }
                else if (entry.Sha256 != Manifest.Hash(Path.Combine(folder, file)))
                {
                    problems.Add(entry.IsOwner
                        ? $"{file}: the owner picture changed since it was adopted (run adopt {file} again)"
                        : $"{file}: differs from its manifest hash (an owner picture: run adopt {file}; otherwise build --force)");
                }
                else if (entry.IsOwner)
                {
                    CheckOwner(root, folder, entry, all, problems);
                }
            }

            foreach (string file in listed.Keys.Where(k => !present.Contains(k)))
            {
                problems.Add($"{Manifest.FileName} lists {file}, which does not exist (run build)");
            }

            // Each generated picture re-renders within tolerance and keeps its margin.
            var owner = new HashSet<string>(manifest.Files.Where(f => f.IsOwner).Select(f => Stem(f.Path)), StringComparer.Ordinal);
            foreach (string name in names.Where(n => !owner.Contains(n)))
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

            // The kit places worn expressions on the heroes' faces where the renderer draws them. An owner's hero has its
            // face where the owner drew it, so the kit's place is then kept by hand.
            foreach (Family family in CharacterArt.Families)
            {
                string hero = CharacterArt.Hero(family);
                string blank = CharacterArt.Hero(family, blank: true);
                // The kit's lists of owner heroes and blanks decide whether a worn expression may swap a hero for its blank.
                if (owner.Contains(hero) != CharacterArt.OwnerHeroes.Contains(family))
                {
                    problems.Add($"CharacterArt.OwnerHeroes {(owner.Contains(hero) ? "misses" : "lists")} {family}, but {hero}.png is {(owner.Contains(hero) ? "the owner's" : "generated")}");
                }

                if (owner.Contains(blank) != CharacterArt.OwnerBlanks.Contains(family))
                {
                    problems.Add($"CharacterArt.OwnerBlanks {(owner.Contains(blank) ? "misses" : "lists")} {family}, but {blank}.png is {(owner.Contains(blank) ? "the owner's" : "generated")}");
                }

                if (owner.Contains(hero) != owner.Contains(blank))
                {
                    (string theirs, string generated) = owner.Contains(hero) ? (hero, blank) : (blank, hero);
                    notes.Add($"{theirs}.png is the owner's but {generated}.png is generated: a worn expression shows as a badge beside the hero until the owner's blank twin exists (pictures.md A5)");
                }

                if (owner.Contains(hero))
                {
                    notes.Add($"{hero}.png is the owner's: CharacterArt.FaceCenterHero({family}) must sit on its face (set by hand, not checked)");
                    continue;
                }

                (double x, double y) = Heroes3D.FaceCenter(family);
                (float kx, float ky) = CharacterArt.FaceCenterHero(family);
                if (Math.Abs(x - kx) > FaceTolerance || Math.Abs(y - ky) > FaceTolerance)
                {
                    problems.Add($"CharacterArt.FaceCenterHero({family}) is ({kx:0.00}, {ky:0.00}); the renderer draws the face at ({x:0.00}, {y:0.00})");
                }
            }

            return problems;
        }

        /// <summary>A file's picture name: <c>3d/sprig.png</c> → <c>3d/sprig</c>.</summary>
        public static string Stem(string path) => path.EndsWith(".png", StringComparison.Ordinal) ? path.Substring(0, path.Length - 4) : path;

        /// <summary>The size the hosts expect of a picture: the generated set's and the celebrating heroes'; null for others.</summary>
        public static (int Width, int Height)? ExpectedSize(string name, IReadOnlyList<string> all) =>
            all.Contains(name) || CharacterArt.Families.Any(f => CharacterArt.Cheer(f) == name) ? CharacterArt.SizeOf(name) : null;

        /// <summary>
        /// Checks an owner picture (spec 005 pictures.md A) without re-rendering it: it lies in <c>3d/</c>, has the size
        /// its entry lists and the hosts expect, keeps the transparent margin, and its source record exists. The caller
        /// checks the hash.
        /// </summary>
        public static void CheckOwner(string root, string folder, ManifestFile entry, IReadOnlyList<string> all, List<string> problems)
        {
            string name = Stem(entry.Path);
            if (!name.StartsWith("3d/", StringComparison.Ordinal))
            {
                problems.Add($"{entry.Path}: owner pictures are the 3D heroes in 3d/ (pictures.md A)");
            }

            (int w, int h, byte[] rgba) = Png.Decode(File.ReadAllBytes(Path.Combine(folder, entry.Path)));
            if (w != entry.Width || h != entry.Height)
            {
                problems.Add($"{entry.Path}: {w} × {h}, the manifest says {entry.Width} × {entry.Height}");
            }

            if (ExpectedSize(name, all) is (int ew, int eh) && (w != ew || h != eh))
            {
                problems.Add($"{entry.Path}: {w} × {h}, the hosts expect {ew} × {eh} (pictures.md A)");
            }

            CheckMargin(entry.Path, w, h, rgba, problems);
            if (string.IsNullOrEmpty(entry.Record))
            {
                problems.Add($"{entry.Path}: no source record (adopt names one: the tool, the author and the licence)");
            }
            else if (entry.Record.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(entry.Record))
            {
                problems.Add($"{entry.Path}: its source record {entry.Record} must be a file in the repository");
            }
            else if (!File.Exists(Path.Combine(root, entry.Record)))
            {
                problems.Add($"{entry.Path}: its source record {entry.Record} does not exist (write it first: the tool, the author and the licence)");
            }
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

            CheckMargin(name, w, h, committed, problems);
        }

        /// <summary>Checks the transparent border of <see cref="Margin"/> on every side of straight-alpha RGBA rows.</summary>
        public static void CheckMargin(string name, int w, int h, byte[] rgba, List<string> problems)
        {
            int border = (int)Math.Ceiling(Margin * Math.Min(w, h));
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool edge = x < border || y < border || x >= w - border || y >= h - border;
                    if (edge && rgba[(((y * w) + x) * 4) + 3] > 8)
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
