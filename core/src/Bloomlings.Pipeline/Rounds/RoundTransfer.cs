using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Rounds
{
    /// <summary>The result of <c>rounds import</c>: the issues that refused it, or the files it copied.</summary>
    public sealed record RoundImportResult(IReadOnlyList<RoundIssue> Issues, IReadOnlyList<string> Copied)
    {
        public bool Imported => !Issues.Any(i => i.IsError);
    }

    /// <summary>
    /// <c>rounds import &lt;export-dir&gt;</c> (contracts/pipeline-rounds-cli.md, T037): an export of
    /// <c>tools/rounds/export.mjs</c> is checked together with the content already in the rounds folder (the export's files
    /// replacing those of the same name), and only then copied, byte for byte, into <c>levels/</c>, <c>pictures/</c>,
    /// <c>proofs/</c> and <c>conformance/</c>. The checks are the schemas, the definition rules and the set's consistency
    /// (<see cref="RoundSetValidator"/> without the replays and the palette, which <c>rounds validate</c> runs), plus the
    /// export's own <c>export.json</c> fingerprints when present. Nothing is copied when any check fails.
    /// </summary>
    public static class RoundImporter
    {
        private static readonly string[] Folders = { RoundContent.LevelsFolder, RoundContent.PicturesFolder, RoundContent.ProofsFolder, RoundContent.ConformanceFolder };

        public static RoundImportResult Import(string exportDir, string roundsDir, RoundValidationOptions options)
        {
            RoundContent export = RoundContent.Load(exportDir);
            if (export.Levels.Count == 0)
            {
                return new RoundImportResult(new[] { new RoundIssue("export", "sequence", $"{exportDir} holds no levels/L####.json") }, Array.Empty<string>());
            }

            RoundContent current = Directory.Exists(roundsDir) ? RoundContent.Load(roundsDir) : new RoundContent(roundsDir);
            RoundContent merged = Merge(current, export, out List<RoundIssue> notes);
            var issues = new List<RoundIssue>(notes);
            issues.AddRange(ExportManifest(exportDir, export));
            issues.AddRange(RoundSetValidator.Validate(merged, new RoundValidationOptions
            {
                DefinitionRules = options.DefinitionRules,
                Conformance = options.Conformance,
                RunConformance = false,
                RunReadability = false,
            }).Issues);
            if (issues.Any(i => i.IsError))
            {
                return new RoundImportResult(issues, Array.Empty<string>());
            }

            var copied = new List<string>();
            foreach (string folder in Folders)
            {
                string from = Path.Combine(exportDir, folder);
                if (!Directory.Exists(from))
                {
                    continue;
                }

                string to = Directory.CreateDirectory(Path.Combine(roundsDir, folder)).FullName;
                foreach (string file in Directory.GetFiles(from, "*.json").OrderBy(f => f, StringComparer.Ordinal))
                {
                    File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
                    copied.Add(folder + "/" + Path.GetFileName(file));
                }
            }

            return new RoundImportResult(issues, copied);
        }

        /// <summary>The rounds content with the export's files in place of those of the same name.</summary>
        private static RoundContent Merge(RoundContent current, RoundContent export, out List<RoundIssue> notes)
        {
            notes = new List<RoundIssue>();
            var merged = new RoundContent(current.Folder);
            var replaced = new HashSet<string>(StringComparer.Ordinal);
            void Take<TKey, TValue>(SortedDictionary<TKey, RoundFile<TValue>> into, SortedDictionary<TKey, RoundFile<TValue>> mine, SortedDictionary<TKey, RoundFile<TValue>> theirs)
                where TKey : notnull
                where TValue : class
            {
                foreach (KeyValuePair<TKey, RoundFile<TValue>> pair in mine)
                {
                    into[pair.Key] = pair.Value;
                }

                foreach (KeyValuePair<TKey, RoundFile<TValue>> pair in theirs)
                {
                    into[pair.Key] = pair.Value;
                    replaced.Add(Path.GetFileNameWithoutExtension(pair.Value.Path));
                }
            }

            Take(merged.Levels, current.Levels, export.Levels);
            Take(merged.Pictures, current.Pictures, export.Pictures);
            Take(merged.Proofs, current.Proofs, export.Proofs);
            Take(merged.Fixtures, current.Fixtures, export.Fixtures);
            merged.Issues.AddRange(current.Issues.Where(i => !replaced.Contains(i.Where)));
            merged.Issues.AddRange(export.Issues);

            foreach (KeyValuePair<int, RoundFile<RoundLevel>> pair in export.Levels)
            {
                if (current.Levels.TryGetValue(pair.Key, out RoundFile<RoundLevel>? old) && old.Value != null && pair.Value.Value != null
                    && RoundJson.WriteLevel(old.Value) != RoundJson.WriteLevel(pair.Value.Value)
                    && pair.Value.Value.DefinitionVersion <= old.Value.DefinitionVersion)
                {
                    notes.Add(new RoundIssue(RoundContent.Where(pair.Key), "import", $"replaces a different level of the same definitionVersion {old.Value.DefinitionVersion}; a published level needs a higher one (rounds diff)", IsError: false));
                }
            }

            return merged;
        }

        /// <summary><c>export.json</c>: when it names engine fingerprints, every exported level must carry them.</summary>
        private static IEnumerable<RoundIssue> ExportManifest(string exportDir, RoundContent export)
        {
            string path = Path.Combine(exportDir, "export.json");
            if (!File.Exists(path))
            {
                yield break;
            }

            JObject root = JsonDoc.ParseObject(File.ReadAllText(path), "export.json");
            JToken? engineToken = JsonDoc.Optional(root, "engine");
            if (engineToken == null)
            {
                yield break;
            }

            RoundEngineVersion engine = RoundJson.ReadEngine(engineToken, "export.json engine");
            foreach (KeyValuePair<int, RoundFile<RoundLevel>> pair in export.Levels)
            {
                if (pair.Value.Value != null && !RoundJson.SameEngine(pair.Value.Value.Engine, engine))
                {
                    yield return new RoundIssue(RoundContent.Where(pair.Key), "engine", "the level's engine fingerprints differ from export.json's");
                }
            }
        }
    }

    /// <summary>The result of <c>rounds publish</c>.</summary>
    public sealed record RoundPublishResult(RoundContentManifest Manifest, int Pictures, IReadOnlyList<string> Removed);

    /// <summary>
    /// <c>rounds publish --out &lt;dir&gt;</c> (T052): writes the packs and <c>manifest.json</c>
    /// (<c>round-content-manifest.v1</c>) of a validated rounds folder. The caller refuses on any validation error first.
    /// Without an explicit content version, the version in the output folder is kept when the packs are the same bytes,
    /// and raised by one otherwise (1 for a new folder). Old round packs no longer listed are removed.
    /// </summary>
    public static class RoundPublisher
    {
        public static RoundPublishResult Publish(RoundContent content, string outDir, int? contentVersion)
        {
            var levels = content.Levels.Values.Select(f => f.Value ?? throw new InvalidDataException($"{f.Path} did not read; validate first")).ToList();
            var pictures = levels.Select(l => l.PictureId!).Distinct(StringComparer.Ordinal)
                .Select(id => content.Pictures[id].Value ?? throw new InvalidDataException($"{content.Pictures[id].Path} did not read; validate first"))
                .ToList();

            RoundPacks.Build(1, levels, pictures, out IReadOnlyList<RoundPackFile> files);
            string manifestPath = Path.Combine(outDir, RoundContentManifest.FileName);
            RoundContentManifest? existing = File.Exists(manifestPath) ? RoundContentManifest.Read(File.ReadAllText(manifestPath)) : null;
            bool same = existing != null
                && existing.Packs.Select(p => p.Id + ":" + p.Sha256).SequenceEqual(files.Select(f => f.Entry.Id + ":" + f.Entry.Sha256));
            int version;
            if (contentVersion.HasValue)
            {
                version = contentVersion.Value;
                if (existing != null && existing.ContentVersion == version && !same)
                {
                    throw new ArgumentException($"content version {version} is already published in {outDir} with other content; give a higher --content-version");
                }
            }
            else
            {
                version = existing == null ? 1 : same ? existing.ContentVersion : existing.ContentVersion + 1;
            }

            RoundContentManifest manifest = RoundPacks.Build(version, levels, pictures, out files);
            var keep = new HashSet<string>(StringComparer.Ordinal);
            foreach (RoundPackFile file in files)
            {
                string path = Path.Combine(outDir, file.Entry.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, file.Bytes);
                keep.Add(Path.GetFullPath(path));
            }

            var removed = new List<string>();
            foreach (string sub in new[] { "levels", "pictures" })
            {
                string dir = Path.Combine(outDir, sub);
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                foreach (string stale in Directory.GetFiles(dir, "round_*.jsonl.gz").Where(f => !keep.Contains(Path.GetFullPath(f))).OrderBy(f => f, StringComparer.Ordinal))
                {
                    File.Delete(stale);
                    removed.Add(sub + "/" + Path.GetFileName(stale));
                }
            }

            File.WriteAllText(manifestPath, manifest.Write());
            return new RoundPublishResult(manifest, pictures.Count, removed);
        }
    }
}
