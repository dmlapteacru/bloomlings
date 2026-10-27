using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Rounds
{
    /// <summary>Round content to compare: a published version, a rounds folder or a git ref.</summary>
    public sealed class RoundSource
    {
        public RoundSource(string name, IEnumerable<RoundLevel> levels, IEnumerable<RoundPicture> pictures, int? contentVersion)
        {
            Name = name;
            ContentVersion = contentVersion;
            foreach (RoundLevel level in levels)
            {
                if (Levels.ContainsKey(level.Number))
                {
                    throw new InvalidDataException($"{name}: level {level.Number} appears twice");
                }

                Levels[level.Number] = level;
            }

            foreach (RoundPicture picture in pictures)
            {
                Pictures[picture.Key] = picture;
            }
        }

        public string Name { get; }

        /// <summary>The published content version, or null for a folder or a git ref.</summary>
        public int? ContentVersion { get; }

        public SortedDictionary<int, RoundLevel> Levels { get; } = new SortedDictionary<int, RoundLevel>();

        /// <summary>Pictures by <c>P####@version</c>.</summary>
        public SortedDictionary<string, RoundPicture> Pictures { get; } = new SortedDictionary<string, RoundPicture>(StringComparer.Ordinal);

        /// <summary>
        /// Loads <paramref name="source"/>: a folder with a round <c>manifest.json</c> (publish output), a rounds folder (with
        /// <c>levels/</c>), or else a git ref whose <paramref name="gitFolder"/> is read (<c>git show</c>).
        /// </summary>
        public static RoundSource Load(string source, string gitFolder)
        {
            if (Directory.Exists(source))
            {
                string manifestPath = Path.Combine(source, RoundContentManifest.FileName);
                if (File.Exists(manifestPath))
                {
                    return LoadPublished(source);
                }

                if (Directory.Exists(Path.Combine(source, RoundContent.LevelsFolder)) || Directory.Exists(Path.Combine(source, RoundContent.PicturesFolder)))
                {
                    return LoadFolder(source);
                }

                throw new DirectoryNotFoundException($"{source} is neither published round content (manifest.json) nor a rounds folder (levels/)");
            }

            return LoadGitRef(source, gitFolder);
        }

        public static RoundSource LoadPublished(string folder)
        {
            RoundContentManifest manifest = RoundContentManifest.Read(File.ReadAllText(Path.Combine(folder, RoundContentManifest.FileName)));
            var levels = new List<RoundLevel>();
            var pictures = new List<RoundPicture>();
            foreach (RoundPackEntry pack in manifest.Packs)
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(folder, pack.Path));
                if (pack.Kind == RoundPackKind.Levels)
                {
                    levels.AddRange(RoundPacks.ReadLevels(pack, bytes));
                }
                else
                {
                    pictures.AddRange(RoundPacks.ReadPictures(pack, bytes));
                }
            }

            return new RoundSource(folder, levels, pictures, manifest.ContentVersion);
        }

        public static RoundSource LoadFolder(string folder)
        {
            var levels = new List<RoundLevel>();
            var pictures = new List<RoundPicture>();
            foreach (string file in Json(Path.Combine(folder, RoundContent.LevelsFolder)))
            {
                levels.Add(Read(file, File.ReadAllText(file), RoundJson.ReadLevel));
            }

            foreach (string file in Json(Path.Combine(folder, RoundContent.PicturesFolder)))
            {
                pictures.Add(Read(file, File.ReadAllText(file), RoundJson.ReadPicture));
            }

            return new RoundSource(folder, levels, pictures, null);
        }

        public static RoundSource LoadGitRef(string gitRef, string folder)
        {
            var levels = new List<RoundLevel>();
            var pictures = new List<RoundPicture>();

            // Git runs in the folder when it exists (so the folder may lie in any repository), else in the current one.
            bool inFolder = Directory.Exists(folder);
            string? workDir = inFolder ? folder : null;
            foreach ((string sub, bool isLevel) in new[] { (RoundContent.LevelsFolder, true), (RoundContent.PicturesFolder, false) })
            {
                string spec = (inFolder ? sub : Path.Combine(folder, sub).Replace('\\', '/')) + "/";
                string listing = Git(workDir, "ls-tree", "-r", "--full-name", "--name-only", gitRef, "--", spec);
                foreach (string file in listing.Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(f => f.EndsWith(".json", StringComparison.Ordinal)))
                {
                    string text = Git(workDir, "show", gitRef + ":" + file);
                    if (isLevel)
                    {
                        levels.Add(Read(gitRef + ":" + file, text, RoundJson.ReadLevel));
                    }
                    else
                    {
                        pictures.Add(Read(gitRef + ":" + file, text, RoundJson.ReadPicture));
                    }
                }
            }

            return new RoundSource(gitRef, levels, pictures, null);
        }

        private static IEnumerable<string> Json(string dir) =>
            Directory.Exists(dir) ? Directory.GetFiles(dir, "*.json").OrderBy(f => f, StringComparer.Ordinal) : Enumerable.Empty<string>();

        private static T Read<T>(string where, string text, Func<string, T> read)
        {
            try
            {
                return read(text);
            }
            catch (Bloomlings.Content.Json.ContentFormatException ex)
            {
                throw new InvalidDataException($"{where}: {ex.Message}", ex);
            }
        }

        private static string Git(string? workDir, params string[] args)
        {
            var info = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            if (workDir != null)
            {
                info.WorkingDirectory = workDir;
            }

            foreach (string arg in args)
            {
                info.ArgumentList.Add(arg);
            }

            using Process process = Process.Start(info) ?? throw new IOException("git could not start");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new IOException("git " + string.Join(" ", args) + ": " + error.Trim());
            }

            return output;
        }
    }

    /// <summary>What changed between two round sources, and the changes that break FR-052.</summary>
    public sealed record RoundDiffResult(
        IReadOnlyList<int> Changed,
        IReadOnlyList<int> Added,
        IReadOnlyList<string> ChangedPictures,
        IReadOnlyList<string> AddedPictures,
        IReadOnlyList<string> Errors);

    /// <summary>
    /// <c>rounds diff</c> (T051, FR-052): a published round level changes only by a higher <c>definitionVersion</c>, which
    /// the diff lists as deliberate; a picture changes only by a higher <c>version</c> (its approval record is not part of
    /// the comparison); nothing published is removed; and a later content version is higher than the earlier one.
    /// </summary>
    public static class RoundDiff
    {
        public static RoundDiffResult Compare(RoundSource from, RoundSource to)
        {
            var changed = new List<int>();
            var errors = new List<string>();
            if (from.ContentVersion.HasValue && to.ContentVersion.HasValue && to.ContentVersion <= from.ContentVersion)
            {
                errors.Add($"the later content version (v{to.ContentVersion}) must be higher than the earlier (v{from.ContentVersion})");
            }

            foreach (RoundLevel old in from.Levels.Values)
            {
                string where = RoundContent.Where(old.Number);
                if (!to.Levels.TryGetValue(old.Number, out RoundLevel? current))
                {
                    errors.Add($"{where} was removed");
                    continue;
                }

                if (RoundJson.WriteLevel(old) == RoundJson.WriteLevel(current))
                {
                    continue;
                }

                changed.Add(old.Number);
                if (current.DefinitionVersion <= old.DefinitionVersion)
                {
                    errors.Add($"{where} changed without a definitionVersion bump ({old.DefinitionVersion} → {current.DefinitionVersion})");
                }
            }

            var changedPictures = new List<string>();
            foreach (RoundPicture old in from.Pictures.Values)
            {
                if (to.Pictures.TryGetValue(old.Key, out RoundPicture? current))
                {
                    if (Comparable(old) != Comparable(current))
                    {
                        changedPictures.Add(old.Id);
                        errors.Add($"picture {old.Id} v{old.Version} changed without a new picture version");
                    }

                    continue;
                }

                int? latest = to.Pictures.Values.Where(p => p.Id == old.Id).Select(p => (int?)p.Version).Max();
                if (latest.HasValue && latest.Value < old.Version)
                {
                    errors.Add($"picture {old.Id} went back from v{old.Version} to v{latest.Value}");
                }
                else if (latest.HasValue)
                {
                    changedPictures.Add(old.Id);
                }
            }

            var added = to.Levels.Keys.Where(n => !from.Levels.ContainsKey(n)).ToList();
            var addedPictures = to.Pictures.Values.Where(p => from.Pictures.Values.All(o => o.Id != p.Id)).Select(p => p.Id).Distinct(StringComparer.Ordinal).ToList();
            return new RoundDiffResult(changed, added, changedPictures.Distinct(StringComparer.Ordinal).ToList(), addedPictures, errors);
        }

        /// <summary>A picture's content without its approval record: approving again is not a change to it.</summary>
        private static string Comparable(RoundPicture picture)
        {
            JObject json = RoundJson.ToJObject(picture);
            json.Remove("approval");
            return json.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
