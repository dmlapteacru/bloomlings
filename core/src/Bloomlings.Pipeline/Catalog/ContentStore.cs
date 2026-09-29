using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Definitions;
using Bloomlings.Pipeline.Readability;

namespace Bloomlings.Pipeline.Catalog
{
    /// <summary>
    /// File layout of the pipeline (contracts/pipeline-cli.md). A catalog or batch folder holds
    /// <c>levels/level-NNNNN.json</c> and <c>validation/level-NNNNN.json</c>; a plain folder of level files
    /// (<c>content/curated</c>) is read too.
    /// </summary>
    public static class ContentStore
    {
        public const string LevelsFolder = "levels";
        public const string ValidationFolder = "validation";

        public static string LevelFileName(int level) => "level-" + level.ToString("00000", CultureInfo.InvariantCulture) + ".json";

        public static List<BasePicture> LoadLibrary(string folder)
        {
            var pictures = new List<BasePicture>();
            if (!Directory.Exists(folder))
            {
                return pictures;
            }

            foreach (string file in Sorted(Directory.GetFiles(folder, "*.json")))
            {
                pictures.Add(BasePictureJson.Read(File.ReadAllText(file)));
            }

            return pictures;
        }

        /// <summary>Level definitions of a catalog or batch (<c>levels/</c>) or of a plain folder (top level only).</summary>
        public static List<(string File, LevelDefinition Level)> LoadLevels(string folder)
        {
            string levels = Path.Combine(folder, LevelsFolder);
            string source = Directory.Exists(levels) ? levels : folder;
            var result = new List<(string, LevelDefinition)>();
            if (!Directory.Exists(source))
            {
                return result;
            }

            foreach (string file in Sorted(Directory.GetFiles(source, "level-*.json")))
            {
                result.Add((file, DefinitionJson.Read(File.ReadAllText(file))));
            }

            return result.OrderBy(r => r.Item2.LevelNumber).ToList();
        }

        public static Dictionary<int, ValidationRecord> LoadRecords(string folder)
        {
            var records = new Dictionary<int, ValidationRecord>();
            string path = Path.Combine(folder, ValidationFolder);
            if (!Directory.Exists(path))
            {
                return records;
            }

            foreach (string file in Sorted(Directory.GetFiles(path, "level-*.json")))
            {
                ValidationRecord record = ValidationRecord.Read(File.ReadAllText(file));
                records[record.LevelNumber] = record;
            }

            return records;
        }

        public static void WriteLevel(string folder, LevelDefinition level, ValidationRecord? record)
        {
            string levels = Directory.CreateDirectory(Path.Combine(folder, LevelsFolder)).FullName;
            File.WriteAllText(Path.Combine(levels, LevelFileName(level.LevelNumber)), DefinitionJson.Write(level));
            if (record != null)
            {
                string validation = Directory.CreateDirectory(Path.Combine(folder, ValidationFolder)).FullName;
                File.WriteAllText(Path.Combine(validation, LevelFileName(level.LevelNumber)), record.Write());
            }
        }

        public static ApprovedPairs? LoadPairs(string path) => File.Exists(path) ? ApprovedPairs.Read(path) : null;

        /// <summary>Level definitions at a git ref (for <c>diff --from main</c>).</summary>
        public static List<LevelDefinition> LoadLevelsAtGitRef(string gitRef, string folder)
        {
            string listing = Git("ls-tree", "-r", "--name-only", gitRef, "--", folder);
            var levels = new List<LevelDefinition>();
            foreach (string file in listing.Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(f => Path.GetFileName(f).StartsWith("level-", StringComparison.Ordinal) && f.EndsWith(".json", StringComparison.Ordinal) && !f.Contains("/" + ValidationFolder + "/", StringComparison.Ordinal)))
            {
                levels.Add(DefinitionJson.Read(Git("show", gitRef + ":" + file)));
            }

            return levels;
        }

        /// <summary>Files changed against a git ref (for <c>validate --changed-only</c>).</summary>
        public static IReadOnlyList<string> ChangedFiles(string gitRef) =>
            Git("diff", "--name-only", gitRef).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        private static string Git(params string[] args)
        {
            var info = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
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

        private static IEnumerable<string> Sorted(string[] files)
        {
            Array.Sort(files, StringComparer.Ordinal);
            return files;
        }
    }
}
