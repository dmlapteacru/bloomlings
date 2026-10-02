using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.ArtGen
{
    /// <summary>
    /// <c>adopt</c>: records a picture the owner made (spec 005 pictures.md A: the 3D heroes, the group, the celebrating
    /// heroes) in the art folder's manifest as <c>"source": "owner"</c>, with its size, hash, slot and source record. From
    /// then on <c>build</c> keeps the file and <c>check</c> verifies it as it was adopted instead of re-rendering it.
    /// To go back to the generated picture, delete the owner's file and run <c>build</c>.
    /// </summary>
    public static class Adopt
    {
        /// <summary>The source record used when <c>--record</c> is not given (repository path).</summary>
        public const string DefaultRecord = "tools/artgen/models/owner-pictures.md";

        /// <summary>
        /// Adopts <paramref name="picture"/>: a name (<c>3d/sprig-cheer</c>), a file name inside the art folder
        /// (<c>3d/sprig-cheer.png</c>) or a path to the file there. Returns the recorded entry, or null with the
        /// <paramref name="problems"/> that kept it out (the manifest is then unchanged).
        /// </summary>
        public static ManifestFile? Run(string root, string folder, IReadOnlyList<string> all, string picture, string record, List<string> problems)
        {
            string? relative = Resolve(folder, picture);
            if (relative == null)
            {
                problems.Add($"{picture}: no such picture in {Path.GetRelativePath(root, folder)}/ (drop the file into 3d/ first)");
                return null;
            }

            string name = ArtCheck.Stem(relative);
            var entry = new ManifestFile
            {
                Path = relative,
                Slot = CharacterArt.SlotOf(name),
                Source = ManifestFile.Owner,
                Record = record.Replace('\\', '/'),
            };
            try
            {
                (entry.Width, entry.Height) = Png.Size(Path.Combine(folder, relative));
            }
            catch (InvalidDataException)
            {
                problems.Add($"{relative}: not a PNG");
                return null;
            }

            entry.Sha256 = Manifest.Hash(Path.Combine(folder, relative));
            int before = problems.Count;
            ArtCheck.CheckOwner(root, folder, entry, all, problems);
            if (problems.Count > before)
            {
                return null;
            }

            Manifest manifest = Manifest.Read(folder);
            manifest.Files.RemoveAll(f => f.Path == relative);
            manifest.Files.Add(entry);
            manifest.Save(folder);
            return entry;
        }

        /// <summary>The picture's path inside the art folder (<c>3d/sprig-cheer.png</c>), or null when no such file is there.</summary>
        private static string? Resolve(string folder, string picture)
        {
            string candidate = picture.EndsWith(".png", StringComparison.Ordinal) ? picture : picture + ".png";
            foreach (string path in new[] { Path.GetFullPath(candidate), Path.GetFullPath(Path.Combine(folder, candidate)) })
            {
                string relative = Path.GetRelativePath(folder, path).Replace('\\', '/');
                if (File.Exists(path) && !relative.StartsWith("../", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
                {
                    return relative;
                }
            }

            return null;
        }
    }
}
