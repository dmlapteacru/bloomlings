using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Linq;
using System;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Simulation;
using Bloomlings.Generator;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Readability;
using Bloomlings.Pipeline.Review;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using Newtonsoft.Json.Linq;
using Command = System.CommandLine.Command;

namespace Bloomlings.Pipeline.Commands
{
    /// <summary><c>solve</c>, <c>validate</c>, <c>score</c>, <c>review</c>, <c>publish</c>, <c>replay</c> and <c>diff</c>.</summary>
    public static class CatalogCommands
    {
        /// <summary><c>--defs</c>, also spelled <c>--catalog</c> as in contracts/pipeline-cli.md.</summary>
        private static Option<string> Defs() => Cli.Path("--defs", "content/catalog", "Catalog or batch folder (levels/ and validation/), or a folder of level files.", "--catalog");

        private static Option<string> Lib() => Cli.Path("--lib", "content/pictures/lib", "Picture library.");

        private static Option<int> Budget() => Cli.Int("--node-budget", 200_000, "Solver node budget per level (never time-based).");

        public static Command Solve()
        {
            var command = new Command("solve", "Solve levels: winning trace, jam witness and metrics (R8).");
            Option<string> defs = Defs();
            Option<int> level = Cli.Int("--level", 0, "Only this level number (0 = all).");
            Option<string> lib = Lib();
            Option<int> budget = Budget();
            Option<string> outDir = Cli.Path("--out", string.Empty, "Write validation records to <out>/validation (optional).");
            foreach (Option option in new Option[] { defs, level, lib, budget, outDir })
            {
                command.Options.Add(option);
            }

            command.SetAction(parse => Cli.Run(parse, report =>
            {
                Dictionary<string, BasePicture> pictures = Pictures(parse.GetValue(lib)!);
                var solver = new Solver.Solver();
                var options = new SolveOptions(parse.GetValue(budget));
                var items = new JArray();
                int unsolved = 0;
                foreach ((string _, LevelDefinition definition) in ContentStore.LoadLevels(parse.GetValue(defs)!))
                {
                    if (parse.GetValue(level) != 0 && definition.LevelNumber != parse.GetValue(level))
                    {
                        continue;
                    }

                    BasePicture picture = Usable(pictures[Key(definition.Picture)], parse, definition.LevelNumber);
                    LevelAnalysis analysis = solver.Analyze(LevelSession.Load(definition, picture, new SessionOptions(1, 20000)), options);
                    unsolved += analysis.Win.Status == SolveStatus.Solvable ? 0 : 1;
                    var metrics = new JObject(analysis.Metrics.ToDictionary().Select(m => new JProperty(m.Key, m.Value)));
                    items.Add(new JObject
                    {
                        ["level"] = definition.LevelNumber,
                        ["result"] = analysis.Win.Status.ToString().ToLowerInvariant(),
                        ["trace"] = new JArray(analysis.Win.Trace.Select(CommandText.Format).ToArray()),
                        ["jamWitness"] = analysis.Jam.Status == SolveStatus.Solvable ? new JArray(analysis.Jam.Trace.Select(CommandText.Format).ToArray()) : null,
                        ["nodes"] = analysis.NodesUsed,
                        ["metrics"] = metrics,
                    });
                    Cli.Say(parse, $"L{definition.LevelNumber}: {analysis.Win.Status.ToString().ToLowerInvariant()} in {analysis.Win.Trace.Count} taps, jam {(analysis.Jam.Status == SolveStatus.Solvable ? analysis.Jam.Trace.Count + " taps" : analysis.Jam.Status.ToString().ToLowerInvariant())}, {analysis.NodesUsed} nodes");
                    if (parse.GetValue(outDir)!.Length > 0)
                    {
                        ContentStore.WriteLevel(parse.GetValue(outDir)!, definition, Record(definition, analysis, options));
                    }
                }

                report["levels"] = items;
                return unsolved == 0 ? ExitCodes.Success : ExitCodes.ValidationFailed;
            }));
            return command;
        }

        public static Command Validate()
        {
            var command = new Command("validate", "Run every FR-080 invariant plus FR-081 and FR-083 over a catalog or batch.");
            Option<string> defs = Defs();
            Option<string> lib = Lib();
            Option<string> pairs = Cli.Path("--pairs", "content/readability/approved-pairs.json", "Approved readability pairs.");
            Option<int> budget = Budget();
            var changedOnly = new Option<bool>("--changed-only") { Description = "Solve only levels whose files changed against --base (FR-083 still covers the whole catalog)." };
            Option<string> baseRef = Cli.Path("--base", "origin/main", "Git ref for --changed-only.");
            var writeRecords = new Option<bool>("--write-records") { Description = "Write the validation records next to the levels." };
            Option<string> context = Cli.Path("--context", string.Empty, "Neighbouring levels validated elsewhere (e.g. content/curated next to the catalog): they join the FR-083 and families windows but are not checked.");
            Option<string> level8 = Cli.Level8();
            foreach (Option option in new Option[] { defs, lib, pairs, budget, changedOnly, baseRef, writeRecords, context, level8 })
            {
                command.Options.Add(option);
            }

            command.SetAction(parse => Cli.Run(parse, report =>
            {
                string folder = parse.GetValue(defs)!;
                var levels = ContentStore.LoadLevels(folder);
                ISet<int>? only = null;
                if (parse.GetValue(changedOnly))
                {
                    var changed = new HashSet<string>(ContentStore.ChangedFiles(parse.GetValue(baseRef)!).Select(Path.GetFullPath));
                    only = new HashSet<int>(levels.Where(l => changed.Contains(Path.GetFullPath(l.File))).Select(l => l.Level.LevelNumber));
                }

                var validator = new CatalogValidator(
                    ContentStore.LoadLibrary(parse.GetValue(lib)!),
                    UnlockRoadmap.ForLevel8(parse.GetValue(level8)!),
                    ContentStore.LoadPairs(parse.GetValue(pairs)!),
                    new SolveOptions(parse.GetValue(budget)));
                List<LevelDefinition>? neighbours = parse.GetValue(context)!.Length > 0
                    ? ContentStore.LoadLevels(parse.GetValue(context)!).Select(l => l.Level).ToList()
                    : null;
                CatalogReport result = validator.Validate(levels.Select(l => l.Level).ToList(), only, neighbours);
                if (parse.GetValue(writeRecords))
                {
                    foreach ((string _, LevelDefinition level) in levels)
                    {
                        if (result.Records.TryGetValue(level.LevelNumber, out ValidationRecord? record))
                        {
                            ContentStore.WriteLevel(folder, level, record);
                        }
                    }
                }

                report["levels"] = levels.Count;
                report["solved"] = result.Records.Count;
                report["errors"] = result.ErrorCount;
                report["issues"] = new JArray(result.Issues.Select(i => new JObject { ["level"] = i.Level, ["check"] = i.Check, ["message"] = i.Message, ["error"] = i.IsError }).ToArray());
                foreach (LevelIssue issue in result.Issues)
                {
                    Cli.Say(parse, $"  {(issue.IsError ? "error" : "warning")} L{issue.Level} {issue.Check}: {issue.Message}");
                }

                Cli.Say(parse, $"validate: {levels.Count} levels, {result.Records.Count} solved, {result.ErrorCount} errors, {result.Issues.Count - result.ErrorCount} warnings.");
                return result.HasErrors ? ExitCodes.ValidationFailed : ExitCodes.Success;
            }));
            return command;
        }

        public static Command Score()
        {
            var command = new Command("score", "Compute each level's difficulty score and class (FR-082), report the class distribution against FR-059 and the picture similarity statistics (SC-012).");
            Option<string> defs = Defs();
            Option<string> curated = Cli.Path("--curated", "content/curated", "Curated levels, included in the statistics when not in --defs.");
            Option<string> lib = Lib();
            Option<string> profiles = Cli.Path("--profiles", "content/profiles", "Band profiles: which thresholds apply to each level.");
            Option<string> thresholds = Cli.Path("--thresholds", "content/profiles/difficulty-thresholds.json", "Difficulty weights and thresholds per band.");
            Option<int> budget = Budget();
            foreach (Option option in new Option[] { defs, curated, lib, profiles, thresholds, budget })
            {
                command.Options.Add(option);
            }

            command.SetAction(parse => Cli.Run(parse, report =>
            {
                var levels = ContentStore.LoadLevels(parse.GetValue(defs)!).Select(l => l.Level).ToDictionary(l => l.LevelNumber);
                (JArray scores, int mismatches) = ComputeScores(
                    levels.Values.OrderBy(l => l.LevelNumber).ToList(),
                    ContentStore.LoadRecords(parse.GetValue(defs)!),
                    parse.GetValue(lib)!,
                    parse.GetValue(profiles)!,
                    parse.GetValue(thresholds)!,
                    new SolveOptions(parse.GetValue(budget)),
                    parse);
                report["scores"] = scores;
                foreach ((string _, LevelDefinition level) in ContentStore.LoadLevels(parse.GetValue(curated)!))
                {
                    levels.TryAdd(level.LevelNumber, level);
                }
                var blocks = new JArray();
                int violations = 0;
                int max = levels.Count == 0 ? 0 : levels.Keys.Max();
                for (int start = 11; start + 99 <= max; start += 100)
                {
                    int hard = 0;
                    int superHard = 0;
                    for (int n = start; n < start + 100; n++)
                    {
                        if (levels.TryGetValue(n, out LevelDefinition? level))
                        {
                            hard += level.Difficulty.Class == DifficultyClass.Hard ? 1 : 0;
                            superHard += level.Difficulty.Class == DifficultyClass.SuperHard ? 1 : 0;
                        }
                    }

                    bool ok = hard >= 15 && hard <= 25 && superHard >= 6 && superHard <= 10;
                    violations += ok ? 0 : 1;
                    blocks.Add(new JObject { ["from"] = start, ["to"] = start + 99, ["hard"] = hard, ["superHard"] = superHard, ["ok"] = ok });
                    Cli.Say(parse, $"  L{start}–{start + 99}: {hard} Hard, {superHard} Super Hard{(ok ? string.Empty : "  ← outside Hard 15–25 / Super Hard 6–10")}");
                }

                var relief = new JArray();
                foreach (LevelDefinition level in levels.Values.Where(l => l.Difficulty.Class == DifficultyClass.SuperHard && l.LevelNumber >= 11))
                {
                    if (levels.TryGetValue(level.LevelNumber + 1, out LevelDefinition? next) && next.Difficulty.Class != DifficultyClass.Normal)
                    {
                        relief.Add(level.LevelNumber + 1);
                        violations++;
                        Cli.Say(parse, $"  L{level.LevelNumber + 1} follows a Super Hard level but is not Normal (FR-059).");
                    }
                }

                // SC-012: Levels 1–100 use 100 different pictures; no picture repeats within 50 consecutive levels.
                var firstHundred = levels.Values.Where(l => l.LevelNumber <= 100).ToList();
                int distinct = firstHundred.Select(l => l.Picture.Id).Distinct(StringComparer.Ordinal).Count();
                int repeatsInFirstHundred = firstHundred.Count - distinct;
                var lastUse = new Dictionary<string, int>(StringComparer.Ordinal);
                int minGap = int.MaxValue;
                var closeRepeats = new JArray();
                foreach (LevelDefinition level in levels.Values.OrderBy(l => l.LevelNumber))
                {
                    if (lastUse.TryGetValue(level.Picture.Id, out int previous))
                    {
                        int gap = level.LevelNumber - previous;
                        minGap = Math.Min(minGap, gap);
                        if (gap < 50)
                        {
                            closeRepeats.Add(new JObject { ["picture"] = level.Picture.Id, ["from"] = previous, ["to"] = level.LevelNumber });
                        }
                    }

                    lastUse[level.Picture.Id] = level.LevelNumber;
                }

                violations += repeatsInFirstHundred + closeRepeats.Count + mismatches;
                report["similarity"] = new JObject
                {
                    ["levelsUpTo100"] = firstHundred.Count,
                    ["distinctPicturesUpTo100"] = distinct,
                    ["distinctPictures"] = lastUse.Count,
                    ["minReuseGap"] = minGap == int.MaxValue ? (JToken)JValue.CreateNull() : minGap,
                    ["repeatsWithin50"] = closeRepeats,
                };
                Cli.Say(parse, $"  similarity: {distinct} distinct pictures in {firstHundred.Count} levels up to L100, {lastUse.Count} pictures overall, smallest reuse gap {(minGap == int.MaxValue ? "none" : minGap.ToString(CultureInfo.InvariantCulture))}, {closeRepeats.Count} repeats within 50 levels (SC-012).");

                report["blocks"] = blocks;
                report["reliefViolations"] = relief;
                report["counts"] = new JObject(levels.Values.GroupBy(l => l.Difficulty.Class.ToString()).OrderBy(g => g.Key).Select(g => new JProperty(g.Key, g.Count())));
                Cli.Say(parse, $"score: {levels.Count} levels, {blocks.Count} complete blocks of 100 from L11, {violations} violations.");
                return violations == 0 ? ExitCodes.Success : ExitCodes.ValidationFailed;
            }));
            return command;
        }

        /// <summary>
        /// FR-082: each level's score from its metrics (the stored validation record when it matches the definition,
        /// else a fresh solve) with its band's weights and thresholds, and whether the class and score stored in the
        /// definition agree. Levels outside every band profile (the hand-curated L1–10) are skipped.
        /// </summary>
        private static (JArray Scores, int Mismatches) ComputeScores(
            IReadOnlyList<LevelDefinition> levels,
            IReadOnlyDictionary<int, ValidationRecord> records,
            string lib,
            string profilesFolder,
            string thresholdsPath,
            SolveOptions options,
            ParseResult parse)
        {
            var bands = Directory.Exists(profilesFolder)
                ? Directory.GetFiles(profilesFolder, "band-*.json").Select(ProfileLoader.ReadFile).OrderBy(p => p.LevelRange.Min).ToList()
                : new List<GenerationProfile>();
            string thresholdsJson = File.Exists(thresholdsPath) ? File.ReadAllText(thresholdsPath) : string.Empty;
            Dictionary<string, BasePicture>? pictures = null;
            var solver = new Solver.Solver();
            var scores = new JArray();
            int mismatches = 0;
            foreach (LevelDefinition level in levels)
            {
                GenerationProfile? band = bands.FirstOrDefault(b => b.LevelRange.Contains(level.LevelNumber));
                if (band == null || thresholdsJson.Length == 0)
                {
                    continue;
                }

                IReadOnlyDictionary<string, int> metrics;
                if (records.TryGetValue(level.LevelNumber, out ValidationRecord? record) && record.DefinitionHash == ValidationRecord.HashOf(level))
                {
                    metrics = record.Metrics;
                }
                else
                {
                    pictures ??= Pictures(lib);
                    BasePicture picture = Usable(pictures[Key(level.Picture)], parse, level.LevelNumber);
                    metrics = solver.Analyze(LevelSession.Load(level, picture, new SessionOptions(1, 20000)), options).Metrics.ToDictionary();
                }

                DifficultyThresholds thresholds = ProfileLoader.ReadThresholds(thresholdsJson, band.BandId);
                int score = DifficultyScorer.Score(metrics, thresholds);
                DifficultyClass computed = DifficultyScorer.Classify(score, thresholds);
                bool agrees = level.Difficulty.Overridden || (computed == level.Difficulty.Class && score == level.Difficulty.Score);
                if (!agrees)
                {
                    mismatches++;
                    Cli.Say(parse, $"  L{level.LevelNumber}: stored {level.Difficulty.Class} ({level.Difficulty.Score}), computed {computed} ({score}) with {band.BandId}");
                }

                scores.Add(new JObject
                {
                    ["level"] = level.LevelNumber,
                    ["band"] = band.BandId,
                    ["score"] = score,
                    ["class"] = computed.ToString(),
                    ["stored"] = level.Difficulty.Class.ToString(),
                    ["agrees"] = agrees,
                });
            }

            Cli.Say(parse, $"  scores: {scores.Count} levels scored, {mismatches} disagree with their stored class or score (FR-082).");
            return (scores, mismatches);
        }

        public static Command ReviewSheet()
        {
            var command = new Command("review", "Create the review sheet of a batch: renders, metrics and QA flags (FR-084).");
            Option<string> defs = Defs();
            Option<string> lib = Lib();
            Option<string> outDir = Cli.Required("--out", "Output folder for index.html and PNGs.");
            command.Options.Add(defs);
            command.Options.Add(lib);
            command.Options.Add(outDir);
            command.SetAction(parse => Cli.Run(parse, report =>
            {
                Dictionary<string, BasePicture> pictures = Pictures(parse.GetValue(lib)!);
                Dictionary<int, ValidationRecord> records = ContentStore.LoadRecords(parse.GetValue(defs)!);
                var batch = ContentStore.LoadLevels(parse.GetValue(defs)!)
                    .Select(l => (l.Level, pictures[Key(l.Level.Picture)], records.TryGetValue(l.Level.LevelNumber, out ValidationRecord? r) ? r : null))
                    .ToList();
                ReviewRenderer.Render(batch, parse.GetValue(outDir)!);
                report["levels"] = batch.Count;
                Cli.Say(parse, $"review: {batch.Count} levels → {Path.Combine(parse.GetValue(outDir)!, "index.html")}");
                return ExitCodes.Success;
            }));
            return command;
        }

        public static Command Publish()
        {
            var command = new Command("publish", "Validate the whole catalog again (the release gate), then assemble level, picture and daily packs and the content-manifest.v1 (R5, R6).");
            Option<string> defs = Cli.Path("--catalog", "content/catalog", "Catalog folder.");
            Option<string> lib = Lib();
            Option<int> version = new Option<int>("--content-version") { Description = "Content version.", Required = true };
            Option<string> outDir = Cli.Path("--out", "build/content", "Output folder.");
            Option<string> minApp = Cli.Path("--min-app-version", "1.0.0", "Minimum app version.");
            Option<int> pictureVersion = Cli.Int("--picture-library-version", 0, "Picture library version (default: the content version).");
            Option<int> shuffleBudget = Cli.Int("--shuffle-node-budget", 50_000, "Shuffle node budget, fixed for this content version (R10).");
            Option<string> daily = Cli.Path("--daily", string.Empty, "Daily pool folder (optional).");
            var allowDraft = new Option<bool>("--allow-draft") { Description = "Playtest builds only: include pictures that are not approved yet, marked as draft previews. Never for release." };
            Option<string> pairs = Cli.Path("--pairs", "content/readability/approved-pairs.json", "Approved readability pairs.");
            Option<int> budget = Budget();
            Option<string> level8 = Cli.Level8();
            foreach (Option option in new Option[] { defs, lib, version, outDir, minApp, pictureVersion, shuffleBudget, daily, allowDraft, pairs, budget, level8 })
            {
                command.Options.Add(option);
            }

            command.SetAction(parse => Cli.Run(parse, report =>
            {
                var levels = ContentStore.LoadLevels(parse.GetValue(defs)!).Select(l => l.Level).OrderBy(l => l.LevelNumber).ToList();
                if (levels.Count == 0 || levels[0].LevelNumber != 1 || levels.Last().LevelNumber != levels.Count)
                {
                    throw new IOException("the catalog must hold levels 1..N without gaps.");
                }

                Dictionary<string, BasePicture> library = Pictures(parse.GetValue(lib)!);
                List<LevelDefinition> dailyLevels = parse.GetValue(daily)!.Length > 0
                    ? ContentStore.LoadLevels(parse.GetValue(daily)!).Select(l => l.Level).ToList()
                    : new List<LevelDefinition>();

                // The release gate (FR-080, FR-081, FR-083): the whole catalog is validated again, and any error stops the
                // publish. The daily pool is checked level by level (its numbers are pool indexes, not Level N). With
                // --allow-draft only unapproved pictures are tolerated, never an unsolvable or otherwise invalid level.
                List<LevelIssue> issues = GateIssues(parse.GetValue(lib)!, parse.GetValue(pairs)!, parse.GetValue(budget), levels, dailyLevels, UnlockRoadmap.ForLevel8(parse.GetValue(level8)!));
                var blocking = issues.Where(i => i.IsError && !(parse.GetValue(allowDraft) && i.Check == "picture-approved")).ToList();
                report["gateErrors"] = new JArray(blocking.Select(i => new JObject { ["level"] = i.Level, ["check"] = i.Check, ["message"] = i.Message }).ToArray());
                if (blocking.Count > 0)
                {
                    foreach (LevelIssue issue in blocking)
                    {
                        Cli.Say(parse, $"  error L{issue.Level} {issue.Check}: {issue.Message}");
                    }

                    Cli.Say(parse, $"publish refused: {blocking.Count} validation errors (run validate for the full report).");
                    return ExitCodes.ValidationFailed;
                }

                var drafts = new SortedSet<string>(StringComparer.Ordinal);
                BasePicture Publishable(string key)
                {
                    BasePicture picture = library[key];
                    if (picture.Review.Status == ReviewStatus.Approved)
                    {
                        return picture;
                    }

                    if (!parse.GetValue(allowDraft))
                    {
                        throw new IOException($"picture {picture.Id} is {picture.Review.Status.ToString().ToLowerInvariant()}; only approved pictures ship (FR-084). Use --allow-draft for a playtest build.");
                    }

                    drafts.Add(picture.Id);
                    return PicturePicker.AsPreview(picture) with { Review = new PictureReview(ReviewStatus.Approved, null, null, "draft preview for playtests, not for release") };
                }

                var used = levels.Select(l => Key(l.Picture)).Distinct().Select(Publishable).ToList();
                var pool = new List<DailyPoolEntry>();
                if (dailyLevels.Count > 0)
                {
                    foreach (LevelDefinition level in dailyLevels)
                    {
                        pool.Add(new DailyPoolEntry(level.LevelNumber - 1, level));
                        if (!used.Any(p => p.Id == level.Picture.Id && p.Version == level.Picture.Version))
                        {
                            used.Add(Publishable(Key(level.Picture)));
                        }
                    }
                }

                int contentVersion = parse.GetValue(version);
                ContentManifest manifest = ManifestWriter.Publish(
                    new PublishRequest(
                        contentVersion,
                        parse.GetValue(minApp)!,
                        parse.GetValue(pictureVersion) > 0 ? parse.GetValue(pictureVersion) : contentVersion,
                        parse.GetValue(shuffleBudget),
                        levels,
                        used,
                        pool),
                    parse.GetValue(outDir)!);
                report["contentVersion"] = contentVersion;
                report["levels"] = levels.Count;
                report["packs"] = manifest.Packs.Count;
                report["draftPictures"] = new JArray(drafts.ToArray());
                if (drafts.Count > 0)
                {
                    Cli.Say(parse, $"  WARNING: {drafts.Count} draft pictures published as previews; this content is for playtest builds only (FR-084).");
                }
                Cli.Say(parse, $"publish v{contentVersion}: {levels.Count} levels, {used.Count} pictures, {pool.Count} daily entries, {manifest.Packs.Count} packs → {parse.GetValue(outDir)}");
                return ExitCodes.Success;
            }));
            return command;
        }

        /// <summary>The validation issues of a catalog and its daily pool, for the publish gate.</summary>
        public static List<LevelIssue> GateIssues(string lib, string pairsPath, int budget, IReadOnlyList<LevelDefinition> levels, IReadOnlyList<LevelDefinition> dailyLevels, UnlockRoadmap? roadmap = null)
        {
            List<BasePicture> library = ContentStore.LoadLibrary(lib);
            ApprovedPairs? pairs = ContentStore.LoadPairs(pairsPath);
            roadmap ??= UnlockRoadmap.Default;
            var issues = new CatalogValidator(library, roadmap, pairs, new SolveOptions(budget)).Validate(levels).Issues;
            if (dailyLevels.Count > 0)
            {
                var daily = new CatalogValidator(library, roadmap, pairs, new SolveOptions(budget)) { CheckBandGuidelines = false, CheckSequences = false };
                issues.AddRange(daily.Validate(dailyLevels).Issues.Select(i => i with { Check = "daily-" + i.Check }));
            }

            return issues;
        }

        public static Command Replay()
        {
            var command = new Command("replay", "Replay a command log against a level and print the event summary and the final StateHash (support).");
            Option<string> defs = Defs();
            Option<string> lib = Lib();
            Option<int> level = new Option<int>("--level") { Description = "Level number.", Required = true };
            Option<string> log = Cli.Required("--log", "Command log: one command per line (tap:p1, restart, …).");
            Option<int> contentVersion = Cli.Int("--content-version", 0, "Content version of the session (salts Shuffle). With --content it must match the manifest.");
            Option<int> shuffleBudget = Cli.Int("--shuffle-node-budget", 50_000, "Shuffle node budget of that content version (taken from the manifest with --content).");
            Option<string> published = Cli.Path("--content", string.Empty, "A published content version (publish output): the level, picture, content version and Shuffle budget come from its manifest, as on the player's device.");
            foreach (Option option in new Option[] { defs, lib, level, log, contentVersion, shuffleBudget, published })
            {
                command.Options.Add(option);
            }

            command.SetAction(parse => Cli.Run(parse, report =>
            {
                LevelDefinition definition;
                BasePicture picture;
                SessionOptions sessionOptions;
                if (parse.GetValue(published)!.Length > 0)
                {
                    ContentSet content = ContentStore.LoadPublished(parse.GetValue(published)!);
                    int wanted = parse.GetValue(contentVersion);
                    if (wanted != 0 && wanted != content.ContentVersion)
                    {
                        throw new ArgumentException($"--content holds content version {content.ContentVersion}, not {wanted}.");
                    }

                    definition = content.GetLevel(parse.GetValue(level));
                    picture = content.GetPicture(definition.Picture);
                    sessionOptions = new SessionOptions(content.ContentVersion, content.ShuffleNodeBudget);
                }
                else
                {
                    definition = ContentStore.LoadLevels(parse.GetValue(defs)!).Select(l => l.Level).Single(l => l.LevelNumber == parse.GetValue(level));
                    picture = Usable(Pictures(parse.GetValue(lib)!)[Key(definition.Picture)], parse, definition.LevelNumber);
                    sessionOptions = new SessionOptions(Math.Max(1, parse.GetValue(contentVersion)), parse.GetValue(shuffleBudget));
                }

                report["contentVersion"] = sessionOptions.ContentVersion;
                LevelSession session = LevelSession.Load(definition, picture, sessionOptions);
                var lines = new JArray();
                foreach (string line in File.ReadAllLines(parse.GetValue(log)!).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal)))
                {
                    CommandResult result = session.Apply(CommandText.Parse(line));
                    string summary = result.Accepted ? $"{line}: {result.Events.Count} events" : $"{line}: rejected {result.Reason}";
                    lines.Add(summary);
                    Cli.Say(parse, "  " + summary);
                }

                report["commands"] = lines;
                report["status"] = session.Status.ToString().ToLowerInvariant();
                report["stateHash"] = EventText.Hex(session.StateHash);
                Cli.Say(parse, $"replay L{definition.LevelNumber}: {session.Status}, StateHash {EventText.Hex(session.StateHash)}");
                return ExitCodes.Success;
            }));
            return command;
        }

        public static Command Diff()
        {
            var command = new Command("diff", "Compare two content versions or catalogs: every changed level needs a deliberate definitionVersion bump (FR-076).");
            Option<string> from = Cli.Required("--from", "Earlier content: a published content version (publish output), a catalog folder, or a git ref such as main.");
            Option<string> to = Cli.Path("--to", "content/catalog", "Later content: a published content version or a catalog folder.");
            Option<string> lib = Lib();
            command.Options.Add(from);
            command.Options.Add(to);
            command.Options.Add(lib);
            command.SetAction(parse => Cli.Run(parse, report =>
            {
                string toFolder = parse.GetValue(to)!;
                (List<LevelDefinition> before, int? fromVersion) = ContentStore.LoadAny(parse.GetValue(from)!, toFolder);
                (List<LevelDefinition> later, int? toVersion) = ContentStore.LoadAny(toFolder, toFolder);
                var after = later.ToDictionary(l => l.LevelNumber);
                report["fromContentVersion"] = fromVersion;
                report["toContentVersion"] = toVersion;
                if (fromVersion != null && toVersion != null && toVersion <= fromVersion)
                {
                    throw new ArgumentException($"--to (v{toVersion}) must be a later content version than --from (v{fromVersion}).");
                }
                var changed = new JArray();
                var errors = new JArray();
                foreach (LevelDefinition old in before)
                {
                    if (!after.TryGetValue(old.LevelNumber, out LevelDefinition? current))
                    {
                        errors.Add($"L{old.LevelNumber} was removed");
                        continue;
                    }

                    if (DefinitionJson.Write(old) == DefinitionJson.Write(current))
                    {
                        continue;
                    }

                    changed.Add(old.LevelNumber);
                    if (current.DefinitionVersion <= old.DefinitionVersion)
                    {
                        errors.Add($"L{old.LevelNumber} changed without a definitionVersion bump ({old.DefinitionVersion} → {current.DefinitionVersion})");
                    }
                }

                // A picture that changed under the same id and version would silently change shipped levels.
                var pictureChanges = new JArray();
                Dictionary<string, BasePicture> earlierPictures = ContentStore.PicturesOf(parse.GetValue(from)!, parse.GetValue(lib)!).ToDictionary(p => p.Id + "@" + p.Version);
                foreach (BasePicture picture in ContentStore.PicturesOf(toFolder, parse.GetValue(lib)!))
                {
                    if (earlierPictures.TryGetValue(picture.Id + "@" + picture.Version, out BasePicture? earlier) && Comparable(earlier) != Comparable(picture))
                    {
                        pictureChanges.Add(picture.Id);
                        errors.Add($"picture {picture.Id} v{picture.Version} changed without a new picture version");
                    }
                }

                report["changedPictures"] = pictureChanges;
                int added = after.Keys.Count(k => before.All(b => b.LevelNumber != k));
                report["changed"] = changed;
                report["added"] = added;
                report["errors"] = errors;
                foreach (JToken error in errors)
                {
                    Cli.Say(parse, "  error: " + error);
                }

                Cli.Say(parse, $"diff: {changed.Count} changed, {added} added, {errors.Count} errors.");
                return errors.Count == 0 ? ExitCodes.Success : ExitCodes.ValidationFailed;
            }));
            return command;
        }

        /// <summary>A picture's content without its review metadata: approving a picture is not a change to it.</summary>
        private static string Comparable(BasePicture picture) =>
            BasePictureJson.Write(picture with { Review = new PictureReview(ReviewStatus.Approved, null, null, null) }, indented: false);

        private static ValidationRecord Record(LevelDefinition definition, LevelAnalysis analysis, SolveOptions options) => new ValidationRecord(
            definition.LevelNumber,
            definition.DefinitionVersion,
            ValidationRecord.HashOf(definition),
            Solver.Solver.Version,
            options.NodeBudget,
            analysis.NodesUsed,
            analysis.Win.Status == SolveStatus.Solvable ? ValidationResult.Solvable : analysis.Win.Status == SolveStatus.Unsolvable ? ValidationResult.Unsolvable : ValidationResult.Unknown,
            analysis.Win.Trace,
            analysis.Jam.Status == SolveStatus.Solvable ? analysis.Jam.Trace : null,
            null,
            analysis.Metrics.ToDictionary(),
            analysis.Win.Status == SolveStatus.Solvable ? new[] { "solvable" } : Array.Empty<string>());

        /// <summary>
        /// Solve, replay and review work on draft pictures too (review is where they get approved): a draft is used
        /// through an in-memory preview copy, with a note. Certification (<c>validate</c>) and <c>publish</c> still refuse it.
        /// </summary>
        private static BasePicture Usable(BasePicture picture, ParseResult parse, int level)
        {
            if (picture.Review.Status == ReviewStatus.Approved)
            {
                return picture;
            }

            Cli.Say(parse, $"  note: L{level} uses {picture.Review.Status.ToString().ToLowerInvariant()} picture {picture.Id} (preview only).");
            return PicturePicker.AsPreview(picture);
        }

        private static Dictionary<string, BasePicture> Pictures(string folder) =>
            ContentStore.LoadLibrary(folder).ToDictionary(p => p.Id + "@" + p.Version, StringComparer.Ordinal);

        private static string Key(PictureRef picture) => picture.Id + "@" + picture.Version;
    }
}
