using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Readability;
using Bloomlings.Solver;

namespace Bloomlings.Pipeline.Validation
{
    /// <summary>One finding of the catalog validator. Errors fail the release (FR-080); warnings are reported.</summary>
    public sealed record LevelIssue(int Level, string Check, string Message, bool IsError);

    public sealed class CatalogReport
    {
        public List<LevelIssue> Issues { get; } = new List<LevelIssue>();

        public SortedDictionary<int, ValidationRecord> Records { get; } = new SortedDictionary<int, ValidationRecord>();

        public bool HasErrors => Issues.Exists(i => i.IsError);

        public int ErrorCount => Issues.Count(i => i.IsError);
    }

    /// <summary>
    /// The catalog validator (T080). Per level it checks every FR-080 invariant (winnable without boosters with a
    /// stored trace, exact accounting, no inaccessible content, no mechanic or variant before its unlock, no hidden-
    /// information failure, readable variant pairs), FR-081 (losable unless a tutorial level), FR-004/FR-060 (variant
    /// count per band), FR-008 (board limits and occupancy, and as amended on 2026-10-06 the level's board rule: regular
    /// boards of 224–288 cells from L11 and big ones of 289–616 cells for big levels), FR-036 as amended (the stored
    /// board look follows the cell count; the hidden layers of an icons board pass the hidden-layer fairness check, and
    /// it holds no mystery) and the data-model rules (keys and locks 1:1, at most one locked slot and only from L80,
    /// layer depth ≤ 2 before L125 and ≤ 3 after, 2–6 stacks, connected members at the same depth). Across the catalog it
    /// checks the FR-083 repetition rules.
    /// </summary>
    public sealed class CatalogValidator
    {
        public const int LastTutorialLevel = 10;

        private readonly IReadOnlyDictionary<string, BasePicture> _pictures;
        private readonly UnlockRoadmap _roadmap;
        private readonly ApprovedPairs? _pairs;
        private readonly SolveOptions _options;
        private readonly Solver.Solver _solver = new Solver.Solver();

        /// <param name="pictures">The picture library by id (latest version per id is enough; versions are checked).</param>
        public CatalogValidator(IEnumerable<BasePicture> pictures, UnlockRoadmap roadmap, ApprovedPairs? pairs, SolveOptions options)
        {
            var byKey = new Dictionary<string, BasePicture>(StringComparer.Ordinal);
            foreach (BasePicture picture in pictures)
            {
                byKey[picture.Id + "@" + picture.Version] = picture;
            }

            _pictures = byKey;
            _roadmap = roadmap;
            _pairs = pairs;
            _options = options;
        }

        /// <summary>
        /// Whether the Level Band Guidelines are checked (variant count, board, pods, work, pod size, families; default
        /// on). Off only for test fixtures built on small pictures and for the Daily Challenge pool, whose numbers are
        /// pool indexes; the readability and layer depth rules always apply.
        /// </summary>
        public bool CheckBandGuidelines { get; set; } = true;

        /// <summary>
        /// Whether the level-sequence rules run (FR-083 similarity, the families window; default on). Off for the Daily
        /// Challenge pool, whose numbers are pool indexes, not Level N.
        /// </summary>
        public bool CheckSequences { get; set; } = true;

        /// <summary>Which expansion variants join the pool, and when (FR-060).</summary>
        public VariantPool Pool { get; set; } = VariantPool.Default;

        /// <param name="levels">The catalog (any order); FR-083 checks look at level-number neighbours.</param>
        /// <param name="solveOnly">When set, only these levels are solved (the others still take part in FR-083).</param>
        /// <param name="context">
        /// Neighbouring levels that are validated elsewhere (the curated L1–10 next to the catalog): they take part in
        /// the FR-083 windows and the families window, so those cross the boundary, but they are not checked here.
        /// </param>
        public CatalogReport Validate(IReadOnlyList<LevelDefinition> levels, ISet<int>? solveOnly = null, IReadOnlyList<LevelDefinition>? context = null)
        {
            var report = new CatalogReport();
            var sorted = levels.OrderBy(l => l.LevelNumber).ToList();
            var own = new HashSet<int>(sorted.Select(l => l.LevelNumber));
            var numbers = new HashSet<int>();
            foreach (LevelDefinition level in sorted)
            {
                if (!numbers.Add(level.LevelNumber))
                {
                    Error(report, level.LevelNumber, "catalog", "duplicate level number");
                }
            }

            foreach (LevelDefinition level in sorted)
            {
                if (solveOnly == null || solveOnly.Contains(level.LevelNumber))
                {
                    ValidateLevel(level, report);
                }
            }

            if (CheckSequences)
            {
                var neighbours = (context ?? Array.Empty<LevelDefinition>()).Where(l => !own.Contains(l.LevelNumber));
                ValidateSequences(sorted.Concat(neighbours).OrderBy(l => l.LevelNumber).ToList(), own, report);
            }

            return report;
        }

        private void ValidateLevel(LevelDefinition level, CatalogReport report)
        {
            int n = level.LevelNumber;
            var passed = new List<string>();
            if (!_pictures.TryGetValue(level.Picture.Id + "@" + level.Picture.Version, out BasePicture? picture))
            {
                Error(report, n, "picture", $"picture {level.Picture.Id} v{level.Picture.Version} is not in the library");
                return;
            }

            if (picture.Review.Status != ReviewStatus.Approved)
            {
                Error(report, n, "picture-approved", $"picture {picture.Id} is {picture.Review.Status}, not approved (FR-084)");

                // Keep checking the rest of the level on an in-memory copy, so a preview batch reports every issue.
                picture = picture with { Review = picture.Review with { Status = ReviewStatus.Approved } };
            }
            else
            {
                passed.Add("picture-approved");
            }

            LevelSession session;
            try
            {
                session = LevelSession.Load(level, picture, new SessionOptions(1, 20000));
                passed.Add("accounting");
                passed.Add("board-build");
            }
            catch (InvalidLevelException ex)
            {
                Error(report, n, "accounting", ex.Message);
                return;
            }

            CheckBoard(level, picture, report, passed);
            if (CheckBandGuidelines)
            {
                CheckBand(level, picture, report, passed);
            }

            CheckVariants(level, report, passed);
            CheckUnlocks(level, picture, report, passed);
            CheckDataModel(level, report, passed);

            LevelAnalysis analysis = _solver.Analyze(session, _options);
            ValidationResult result = analysis.Win.Status switch
            {
                SolveStatus.Solvable => ValidationResult.Solvable,
                SolveStatus.Unsolvable => ValidationResult.Unsolvable,
                _ => ValidationResult.Unknown,
            };
            if (result == ValidationResult.Solvable)
            {
                passed.Add("solvable");
                passed.Add("reachability");
            }
            else
            {
                Error(report, n, "solvable", $"not winnable without boosters ({result.ToString().ToLowerInvariant()}, {analysis.Win.NodesUsed} nodes)");
            }

            bool losable = analysis.Jam.Status == SolveStatus.Solvable;
            if (losable)
            {
                passed.Add("losable");
            }
            else if (n > LastTutorialLevel)
            {
                Error(report, n, "losable", "no tap sequence jams this level (FR-081)");
            }

            // Mystery pods and tiles, and the hidden layers of an icons board: no level may force a blind guess (FR-036 as
            // amended, FR-039, FR-080, R8, R8b).
            bool icons = BoardLooks.Of(level) == BoardLook.Icons;
            FairnessResult fairness = FairnessChecker.Check(level, picture, new SessionOptions(1, 20000), _options.NodeBudget, BandGuidelines.MaxLayersBelow(n), BandGuidelines.MaxHiddenLayersOnIcons);
            if (fairness.Status == FairnessStatus.Fair)
            {
                passed.Add("player-info-fair");
            }
            else
            {
                string why = fairness.Status switch
                {
                    FairnessStatus.OverCap when icons => fairness.Detail ?? $"more than {BandGuidelines.MaxHiddenLayersOnIcons} hidden layers on an icons board",
                    FairnessStatus.OverCap => $"more than {FairnessChecker.MaxMysteryPods} mystery pods or {FairnessChecker.MaxMysteryTiles} mystery tiles",
                    FairnessStatus.Unknown => $"the fairness search ran out of budget ({fairness.NodesUsed} nodes)",
                    FairnessStatus.Uncovered => fairness.Detail ?? "hidden information the check does not cover",
                    _ when icons => (fairness.Detail ?? "winning needs to know where the hidden layers lie") + $" ({fairness.Worlds} worlds)",
                    _ => $"winning needs hidden knowledge in {fairness.Worlds} indistinguishable worlds",
                };
                Error(report, n, "player-info-fair", why + (icons ? " (FR-036, FR-039)" : " (FR-039)"));
            }

            report.Records[n] = new ValidationRecord(
                n,
                level.DefinitionVersion,
                ValidationRecord.HashOf(level),
                Solver.Solver.Version,
                _options.NodeBudget,
                analysis.NodesUsed,
                result,
                analysis.Win.Trace,
                losable ? analysis.Jam.Trace : null,
                fairness.PlayerInfoFair,
                analysis.Metrics.ToDictionary(),
                passed);
        }

        /// <summary>
        /// FR-008: the board limits and the occupancy, and FR-036 as amended on 2026-10-06: the board look the level stores
        /// (Peek when it stores none) is the one its cell count asks for. The level's own board rule by level number is a
        /// band check (<see cref="CheckBand"/>).
        /// </summary>
        private static void CheckBoard(LevelDefinition level, BasePicture picture, CatalogReport report, List<string> passed)
        {
            int n = level.LevelNumber;
            bool ok = true;
            if (picture.Width < BandGuidelines.MinBoardWidth || picture.Width > BasePicture.MaxWidth
                || picture.Height < BandGuidelines.MinBoardHeight || picture.Height > BasePicture.MaxHeight
                || picture.Width * picture.Height > BandGuidelines.BigMaxCells)
            {
                Error(report, n, "board", $"board {picture.Width}×{picture.Height} is outside {BandGuidelines.MinBoardWidth}×{BandGuidelines.MinBoardHeight}–{BasePicture.MaxWidth}×{BasePicture.MaxHeight} (FR-008)");
                ok = false;
            }

            BoardLook wanted = BoardLooks.For(picture.Width, picture.Height);
            BoardLook stored = BoardLooks.Of(level);
            if (stored != wanted)
            {
                int cells = picture.Width * picture.Height;
                string stated = level.BoardLook == null ? " (not stated)" : string.Empty;
                Error(report, n, "board", $"boardLook is {Wire(stored)}{stated}, but a board of {cells} cells shows {Wire(wanted)}: up to {BoardLooks.MaxPeekCells} cells peek, more icons (FR-036 as amended on 2026-10-06)");
                ok = false;
            }

            int filled = 0;
            foreach (IReadOnlyList<int> row in picture.Grid)
            {
                foreach (int cell in row)
                {
                    if (cell != BasePicture.Empty)
                    {
                        filled++;
                    }
                }
            }

            int occupancy = filled * 1000 / (picture.Width * picture.Height);
            if (occupancy < 750 || occupancy > 950)
            {
                Error(report, n, "board", $"occupancy {occupancy / 10.0:0.0}% is outside 75–95% (FR-008)");
                ok = false;
            }

            if (ok)
            {
                passed.Add("board");
            }
        }

        /// <summary>FR-004 and FR-060: the variant count by level band (<see cref="BandGuidelines.Variants"/>).</summary>
        public static (int Min, int Max) VariantRange(int level, DifficultyClass difficulty)
        {
            IntRange range = BandGuidelines.Variants(level, difficulty);
            return (range.Min, range.Max);
        }

        /// <summary>
        /// The spec's Level Band Guidelines (<see cref="BandGuidelines"/>): board size, Source Pod count and work by band
        /// and class, and the minimum pod size. The hand-curated tutorial levels (L1–10) only warn on small pods.
        /// The typical duration is not checked: the solver's estimate (<c>estimatedDurationMs</c>) is not calibrated yet
        /// and runs at about half the spec's durations (L1–10 estimate 10–17 s for 20–45 s); playtests calibrate it
        /// (T155), and the check follows then.
        /// </summary>
        private static void CheckBand(LevelDefinition level, BasePicture picture, CatalogReport report, List<string> passed)
        {
            int n = level.LevelNumber;
            GuidelineBand band = BandGuidelines.For(n);
            bool ok = true;
            void Fail(string message)
            {
                string row = BandGuidelines.IsBigLevel(n) ? $"a big level of the {BandGuidelines.BandOf(n).Name} band" : $"{band.Name} band";
                Error(report, n, "band", message + $" ({row}, Level Band Guidelines)");
                ok = false;
            }

            if (!band.Board.Allows(picture.Width, picture.Height))
            {
                Fail($"board {picture.Width}×{picture.Height} ({picture.Width * picture.Height} cells) is outside {band.Board} (FR-008 as amended on 2026-10-06)");
            }

            if (!band.Pods.Contains(level.Pods.Count))
            {
                Fail($"{level.Pods.Count} Source Pods; the band allows {band.Pods}");
            }

            IntRange work = BandGuidelines.Work(n, level.Difficulty.Class);
            int total = level.Pods.Sum(p => p.Count);
            if (!work.Contains(total))
            {
                Fail($"work {total} is outside {work} for a {level.Difficulty.Class} level");
            }

            foreach (PodDef pod in level.Pods.Where(p => p.Count < BandGuidelines.MinPodSize))
            {
                string message = $"pod {pod.Id} has {pod.Count} tiles; the smallest pod class is {BandGuidelines.MinPodSize}–15";
                if (n <= LastTutorialLevel)
                {
                    Warning(report, n, "band", message + " (hand-curated tutorial level)");
                }
                else
                {
                    Fail(message);
                }
            }

            if (ok)
            {
                passed.Add("band");
            }
        }

        private void CheckVariants(LevelDefinition level, CatalogReport report, List<string> passed)
        {
            int n = level.LevelNumber;
            var variants = new SortedSet<VariantId>();
            foreach (PodDef pod in level.Pods)
            {
                variants.Add(pod.Variant);
            }

            (int min, int max) = VariantRange(n, level.Difficulty.Class);
            if (!CheckBandGuidelines)
            {
                passed.Add("variant-count");
            }
            else if (variants.Count == 7 && n > 500)
            {
                Warning(report, n, "variant-count", "7 variants: exceptional, needs the readability sign-off (FR-004)");
            }
            else if (variants.Count < min || variants.Count > max)
            {
                Error(report, n, "variant-count", $"{variants.Count} variants; L{n} allows {min}–{max} (FR-004, FR-060)");
            }
            else
            {
                passed.Add("variant-count");
            }

            var list = variants.ToList();
            bool readable = true;
            for (int a = 0; a < list.Count; a++)
            {
                for (int b = a + 1; b < list.Count; b++)
                {
                    if (_pairs == null || !_pairs.IsApproved(list[a], list[b]))
                    {
                        Error(report, n, "readability", $"{list[a]} and {list[b]} are not an approved pair (FR-005)");
                        readable = false;
                    }
                }
            }

            if (readable)
            {
                passed.Add("readability");
                if (_pairs != null && _pairs.IsProvisional && list.Count > 1)
                {
                    Warning(report, n, "readability", "variant pairs are approved provisionally; needs the human readability sign-off");
                }
            }
        }

        /// <summary>The roadmap unlock each mechanic needs (FR-031); see <see cref="LevelMechanics"/>.</summary>
        public static IReadOnlyList<string> MechanicsUsed(LevelDefinition level, BasePicture picture) => LevelMechanics.UnlocksUsed(level, picture);

        private void CheckUnlocks(LevelDefinition level, BasePicture picture, CatalogReport report, List<string> passed)
        {
            int n = level.LevelNumber;
            bool ok = true;
            foreach (string unlock in MechanicsUsed(level, picture))
            {
                int? at = _roadmap.LevelOf(unlock);
                if (at == null)
                {
                    Error(report, n, "unlock", $"{unlock} is not on the unlock roadmap");
                    ok = false;
                }
                else if (n < at.Value)
                {
                    Error(report, n, "unlock", $"{unlock} is used before its unlock level L{at.Value} (FR-031)");
                    ok = false;
                }
            }

            foreach (VariantId variant in level.Pods.Select(p => p.Variant).Distinct())
            {
                if (!Pool.IsAvailable(variant, n, _roadmap))
                {
                    int? at = Pool.IntroducedAt(variant, _roadmap);
                    Error(report, n, "unlock", at == null
                        ? $"variant {variant} is not in the pool (FR-060)"
                        : $"variant {variant} is used before it joins the pool at L{at.Value} (FR-060)");
                    ok = false;
                }
            }

            if (ok)
            {
                passed.Add("unlock");
            }
        }

        private static void CheckDataModel(LevelDefinition level, CatalogReport report, List<string> passed)
        {
            int n = level.LevelNumber;
            bool ok = true;
            void Fail(string message)
            {
                Error(report, n, "data-model", message);
                ok = false;
            }

            if (level.Tray.Stacks.Count < 2 || level.Tray.Stacks.Count > 6)
            {
                Fail($"{level.Tray.Stacks.Count} stacks; 2–6 are allowed");
            }

            if (level.Slots.Locked != null && n < 80)
            {
                Fail("a locked slot before L80 (FR-039)");
            }

            int maxBelow = BandGuidelines.MaxLayersBelow(n);
            foreach (CellOverlay overlay in level.Overlays)
            {
                if (overlay.LayersBelow.Count > maxBelow)
                {
                    Fail($"cell {overlay.Cell} has depth {overlay.LayersBelow.Count + 1}; the limit is {maxBelow + 1} at L{n} (FR-036)");
                }
            }

            var keysOnBoard = level.Overlays.Where(o => o.KeyId != null).Select(o => o.KeyId!).ToList();
            var lockKeys = level.Locks.Select(l => l.KeyId).ToList();
            foreach (PodDef pod in level.Pods.Where(p => p.LockKeyId != null))
            {
                if (!lockKeys.Contains(pod.LockKeyId!))
                {
                    lockKeys.Add(pod.LockKeyId!);
                }
            }

            if (level.Slots.Locked != null && !lockKeys.Contains(level.Slots.Locked.KeyId))
            {
                lockKeys.Add(level.Slots.Locked.KeyId);
            }

            if (keysOnBoard.Count != keysOnBoard.Distinct().Count() || level.Locks.Select(l => l.KeyId).Distinct().Count() != level.Locks.Count)
            {
                Fail("a key or lock appears twice (FR-033)");
            }

            if (!new HashSet<string>(keysOnBoard).SetEquals(lockKeys))
            {
                Fail("keys and locks do not pair 1:1 (FR-033)");
            }

            var depthOf = new Dictionary<string, (int Stack, int Depth)>(StringComparer.Ordinal);
            for (int s = 0; s < level.Tray.Stacks.Count; s++)
            {
                for (int d = 0; d < level.Tray.Stacks[s].Count; d++)
                {
                    depthOf[level.Tray.Stacks[s][d]] = (s, d);
                }
            }

            foreach (IGrouping<string, PodDef> group in level.Pods.Where(p => p.ConnectedGroupId != null).GroupBy(p => p.ConnectedGroupId!))
            {
                var places = group.Select(p => depthOf[p.Id]).ToList();
                if (places.Count < 2 || places.Select(p => p.Depth).Distinct().Count() != 1 || places.Select(p => p.Stack).Distinct().Count() != places.Count)
                {
                    Fail($"connected group {group.Key} must have 2+ members at the same depth in different stacks (FR-035)");
                }
            }

            if (ok)
            {
                passed.Add("data-model");
            }
        }

        /// <param name="own">The levels issues are reported for; the others are context.</param>
        private void ValidateSequences(List<LevelDefinition> sorted, ISet<int> own, CatalogReport report)
        {
            var byNumber = sorted.GroupBy(l => l.LevelNumber).ToDictionary(g => g.Key, g => g.First());
            foreach (LevelDefinition level in sorted)
            {
                int n = level.LevelNumber;
                if (!own.Contains(n))
                {
                    continue;
                }

                BasePicture? picture = _pictures.TryGetValue(level.Picture.Id + "@" + level.Picture.Version, out BasePicture? p) ? p : null;

                // Pictures: unique in 1–100, no repeat within 50, and a reuse must differ in mapping or mirror and in Source design.
                foreach (LevelDefinition other in sorted)
                {
                    if (other.LevelNumber >= n || other.Picture.Id != level.Picture.Id)
                    {
                        continue;
                    }

                    if (n <= 100 && other.LevelNumber <= 100)
                    {
                        Error(report, n, "similarity", $"picture {level.Picture.Id} already used by L{other.LevelNumber}; Levels 1–100 use distinct pictures (FR-083)");
                    }
                    else if (n - other.LevelNumber < 50)
                    {
                        Error(report, n, "similarity", $"picture {level.Picture.Id} repeats L{other.LevelNumber} within 50 levels (FR-083)");
                    }
                    else
                    {
                        bool sameLook = Generator.LevelGenerator.SameLook(level, other);
                        bool sameSource = Generator.LevelGenerator.SourceSignature(level) == Generator.LevelGenerator.SourceSignature(other);
                        if (sameLook || sameSource)
                        {
                            Error(report, n, "similarity", $"reuse of {level.Picture.Id} from L{other.LevelNumber} must differ in mapping or mirroring and in Source design (FR-083)");
                        }
                    }
                }

                if (byNumber.TryGetValue(n - 1, out LevelDefinition? a) && byNumber.TryGetValue(n - 2, out LevelDefinition? b))
                {
                    var set = Generator.LevelGenerator.VariantSet(level);
                    if (set.SetEquals(Generator.LevelGenerator.VariantSet(a)) && set.SetEquals(Generator.LevelGenerator.VariantSet(b)))
                    {
                        Error(report, n, "similarity", "the same active variant set 3 levels in a row (FR-083)");
                    }

                    if (picture != null
                        && _pictures.TryGetValue(a.Picture.Id + "@" + a.Picture.Version, out BasePicture? pa)
                        && _pictures.TryGetValue(b.Picture.Id + "@" + b.Picture.Version, out BasePicture? pb))
                    {
                        // An empty set is a set too, once mechanics exist (from L11; the tutorial levels have none).
                        string mechanics = string.Join(",", MechanicsUsed(level, picture));
                        if ((mechanics.Length > 0 || n > LastTutorialLevel) && mechanics == string.Join(",", MechanicsUsed(a, pa)) && mechanics == string.Join(",", MechanicsUsed(b, pb)))
                        {
                            Error(report, n, "similarity", $"the same mechanics ({(mechanics.Length > 0 ? mechanics : "none")}) 3 levels in a row (FR-083)");
                        }
                    }
                }

                // From L20 all four families are regular: every window of levels uses all four.
                if (CheckBandGuidelines && n >= BandGuidelines.AllFamiliesFrom + BandGuidelines.FamilyWindow - 1)
                {
                    var families = new HashSet<Family>();
                    bool complete = true;
                    for (int l = n - BandGuidelines.FamilyWindow + 1; l <= n && complete; l++)
                    {
                        if (!byNumber.TryGetValue(l, out LevelDefinition? windowLevel))
                        {
                            complete = false;
                            break;
                        }

                        foreach (VariantId variant in Generator.LevelGenerator.VariantSet(windowLevel))
                        {
                            families.Add(VariantCatalog.Default.Get(variant).Family);
                        }
                    }

                    if (complete && families.Count < 4)
                    {
                        Error(report, n, "families", $"L{n - BandGuidelines.FamilyWindow + 1}–{n} use {families.Count} of the 4 families; all four are regular from L{BandGuidelines.AllFamiliesFrom}");
                    }
                }

                // FR-031 showcase → practice: the unlock level and the practice level use the mechanic, and no other
                // (stones aside, and the key that a locked pod or slot needs). An optional mechanic (mystery tile, chest, statue/bridge, triple) is checked only when
                // the catalog uses it somewhere.
                if (picture != null)
                {
                    IReadOnlyList<string> used = MechanicsUsed(level, picture);
                    foreach (string mechanic in Generator.Overlays.MechanicNames.All)
                    {
                        string unlock = Generator.Overlays.MechanicNames.UnlockId(mechanic);
                        int? showcase = _roadmap.LevelOf(unlock);
                        int? practice = Generator.Overlays.MechanicNames.PracticeLevel(mechanic, _roadmap, l => byNumber.TryGetValue(l, out LevelDefinition? d) ? d.Difficulty.Class : DifficultyClass.Normal);
                        bool isShowcase = showcase == n;
                        if (!isShowcase && practice != n)
                        {
                            continue;
                        }

                        UnlockEntry? entry = _roadmap.Entries.FirstOrDefault(e => e.UnlockId == unlock);
                        if (entry != null && entry.Optional && !UsedAnywhere(sorted, unlock))
                        {
                            continue;
                        }

                        string role = isShowcase ? "showcase" : "practice";
                        if (!used.Contains(unlock))
                        {
                            Error(report, n, role, $"L{n} is the {role} of {mechanic}: it must use it (FR-031)");
                        }
                        else if (used.Any(u => u != unlock && u.StartsWith("mechanic.", StringComparison.Ordinal) && !Companion(mechanic, u)))
                        {
                            Error(report, n, role, $"L{n} is the {role} of {mechanic}: no other mechanic may join it (FR-031)");
                        }

                        // The owner, 2026-10-06: a practice level is never Super Hard (the schedule moves that Super Hard on).
                        if (!isShowcase && level.Difficulty.Class == DifficultyClass.SuperHard)
                        {
                            Error(report, n, role, $"L{n} is the practice of {mechanic}: a practice level is never Super Hard (FR-059 as amended)");
                        }
                    }
                }

                // FR-031: a variant joining the pool gets one clean level (it is used, no mechanic is), then one mixed
                // level (it is used again). Without any picture of its color group it cannot be shown yet: a warning.
                foreach ((int joinedAt, bool clean) in new[] { (n, true), (n - 1, false) })
                {
                    foreach (VariantId variant in Pool.JoiningAt(joinedAt, _roadmap))
                    {
                        bool used = Generator.LevelGenerator.VariantSet(level).Contains(variant);
                        bool quiet = picture == null || MechanicsUsed(level, picture).All(m => m == "mechanic.stone");
                        if (used && (!clean || quiet))
                        {
                            continue;
                        }

                        string message = clean
                            ? $"{variant} joins the pool here: this level must use it, with no mechanic (FR-031 clean level)"
                            : $"{variant} joined the pool at L{joinedAt}: this level must use it again (FR-031 mixed level)";
                        bool drawable = _pictures.Values.Any(p => p.Roles.Any(r => r.ColorGroup == VariantCatalog.Default.Get(variant).ColorGroup));
                        if (drawable)
                        {
                            Error(report, n, "variant-intro", message);
                        }
                        else
                        {
                            Warning(report, n, "variant-intro", message + $"; no picture has a {VariantCatalog.Default.Get(variant).ColorGroup} role yet");
                        }
                    }
                }

                string signature = Generator.LevelGenerator.SourceSignature(level);
                for (int l = n - 49; l < n; l++)
                {
                    if (byNumber.TryGetValue(l, out LevelDefinition? other) && Generator.LevelGenerator.SourceSignature(other) == signature)
                    {
                        Error(report, n, "similarity", $"Source layout {signature} repeats L{l} within 50 levels (FR-083)");
                        break;
                    }
                }
            }
        }

        /// <summary>Mechanics that come with another on its showcase and practice: stones anywhere, keys with locks.</summary>
        private static bool Companion(string mechanic, string unlock) =>
            unlock == "mechanic.stone"
            || (unlock == "mechanic.key" && (mechanic == Generator.Overlays.MechanicNames.LockedPod || mechanic == Generator.Overlays.MechanicNames.LockedSlot));

        private bool UsedAnywhere(IEnumerable<LevelDefinition> levels, string unlock) =>
            levels.Any(l => _pictures.TryGetValue(l.Picture.Id + "@" + l.Picture.Version, out BasePicture? p) && MechanicsUsed(l, p).Contains(unlock));

        private static string Wire(BoardLook look) => look == BoardLook.Icons ? "icons" : "peek";

        private static void Error(CatalogReport report, int level, string check, string message) =>
            report.Issues.Add(new LevelIssue(level, check, message, true));

        private static void Warning(CatalogReport report, int level, string check, string message) =>
            report.Issues.Add(new LevelIssue(level, check, message, false));
    }
}
