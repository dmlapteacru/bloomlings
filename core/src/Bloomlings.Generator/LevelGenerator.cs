using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Random;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Overlays;
using Bloomlings.Generator.Profiles;
using Bloomlings.Solver;

namespace Bloomlings.Generator
{
    /// <summary>An accepted level with its validation record.</summary>
    public sealed record GeneratedLevel(LevelDefinition Definition, ValidationRecord Record);

    /// <summary>A discarded candidate and why (T089).</summary>
    public sealed record Rejection(int Level, int Attempt, string Reason);

    public sealed class GenerationResult
    {
        public List<GeneratedLevel> Accepted { get; } = new List<GeneratedLevel>();

        public List<Rejection> Rejections { get; } = new List<Rejection>();

        /// <summary>Levels for which every candidate was rejected.</summary>
        public List<int> Failed { get; } = new List<int>();
    }

    /// <summary>
    /// The picture-first, solution-first generator (R9 steps 1–9, FR-079, T089). Everything is driven by a seeded
    /// PRNG per (seed, level, attempt), so the same profile and seed give byte-identical definitions. Each discarded
    /// candidate records its reason.
    /// </summary>
    public sealed class LevelGenerator
    {
        /// <summary>Recorded in each definition; bump it when generator behavior changes.</summary>
        public const string Version = "gen-1.0.0";

        /// <summary>Node budget of each search while the tray is tuned.</summary>
        public const int TuningNodeBudget = 10_000;

        private readonly GenerationProfile _profile;
        private readonly PicturePicker _pictures;
        private readonly DifficultyThresholds _thresholds;
        private readonly Func<VariantId, VariantId, bool> _readablePair;
        private readonly DifficultySchedule _schedule;
        private readonly UnlockRoadmap _roadmap;

        public LevelGenerator(
            GenerationProfile profile,
            PicturePicker pictures,
            DifficultyThresholds thresholds,
            Func<VariantId, VariantId, bool> readablePair,
            DifficultySchedule schedule,
            UnlockRoadmap? roadmap = null)
        {
            _profile = profile;
            _pictures = pictures;
            _thresholds = thresholds;
            _readablePair = readablePair;
            _schedule = schedule;
            _roadmap = roadmap ?? UnlockRoadmap.Default;
        }

        /// <summary>
        /// Showcase levels (T111): exactly these mechanics instead of a seeded choice; a candidate that cannot place
        /// one of them is rejected. Every mechanic must still be allowed by the profile and unlocked at the level.
        /// </summary>
        public IReadOnlyList<string>? ForcedMechanics { get; set; }

        /// <summary>A fixed difficulty class instead of the schedule (showcase levels are Normal).</summary>
        public DifficultyClass? ForcedClass { get; set; }

        /// <summary>Called after each level with the level number, the accepted level (null when every candidate failed) and the candidates tried.</summary>
        public Action<int, GeneratedLevel?, int>? Progress { get; set; }

        /// <param name="history">Earlier levels by number, for the FR-083 windows; accepted levels are added to it.</param>
        /// <param name="keep">Levels that are already fixed (curated or showcase): they stay in the history and are not generated.</param>
        public GenerationResult Generate(int firstLevel, int lastLevel, ulong seed, IDictionary<int, LevelDefinition> history, ISet<int>? keep = null)
        {
            var result = new GenerationResult();
            var readOnlyHistory = new ReadOnlyHistory(history);
            for (int level = firstLevel; level <= lastLevel; level++)
            {
                if (keep != null && keep.Contains(level))
                {
                    continue;
                }

                bool accepted = false;
                int attempt = 0;
                for (; attempt < _profile.MaxCandidatesPerLevel && !accepted; attempt++)
                {
                    ulong levelSeed = SplitMix64.Mix(seed ^ SplitMix64.Mix((ulong)level * 0x9E3779B97F4A7C15UL + (ulong)attempt));
                    GeneratedLevel? generated = TryGenerate(level, levelSeed, readOnlyHistory, out string? reason);
                    if (generated == null)
                    {
                        result.Rejections.Add(new Rejection(level, attempt, reason ?? "unknown"));
                        continue;
                    }

                    result.Accepted.Add(generated);
                    history[level] = generated.Definition;
                    accepted = true;
                }

                if (!accepted)
                {
                    result.Failed.Add(level);
                }

                Progress?.Invoke(level, accepted ? result.Accepted[result.Accepted.Count - 1] : null, attempt);
            }

            return result;
        }

        private GeneratedLevel? TryGenerate(int level, ulong levelSeed, IReadOnlyDictionary<int, LevelDefinition> history, out string? reason)
        {
            var rng = new Xoshiro256StarStar(levelSeed);
            DifficultyClass target = ForcedClass ?? _schedule.ClassFor(level);

            // 1. Picture.
            BasePicture? picture = _pictures.Pick(_profile, level, history, ref rng);
            if (picture == null)
            {
                reason = "picture:none-available";
                return null;
            }

            // 2. Mapping, avoiding the variant set of the two previous levels when they share one (FR-083).
            IReadOnlyList<SortedDictionary<string, VariantId>> mappings = RoleMapper.Mappings(picture, _profile, _readablePair);
            if (mappings.Count == 0)
            {
                reason = $"mapping:none-for-{picture.Id}";
                return null;
            }

            SortedDictionary<string, VariantId> mapping = mappings[rng.NextInt(mappings.Count)];
            if (RepeatsVariantSet(level, mapping, history))
            {
                reason = "similarity:variant-set-3-in-a-row";
                return null;
            }

            // 3. Entries and mirroring toward the structure target.
            string layout = _profile.EntryLayouts[rng.NextInt(_profile.EntryLayouts.Count)];
            Mirror mirror = rng.NextInt(2) == 0 ? Mirror.None : Mirror.Horizontal;
            IReadOnlyList<EntryDef>? entries = EntryPlanner.Entries(layout, picture, mirror);
            if (entries == null)
            {
                reason = $"entry:{layout}-on-stone";
                return null;
            }

            // 4a. Mechanics for this level (FR-031) and the board overlays: hidden layers and mystery tiles.
            List<string> chosen;
            if (ForcedMechanics != null)
            {
                chosen = new List<string>(ForcedMechanics);
                foreach (string mechanic in chosen)
                {
                    int? at = _roadmap.LevelOf(MechanicNames.UnlockId(mechanic));
                    if (!_profile.Allows(mechanic) || at == null || at.Value > level)
                    {
                        throw new ArgumentException($"{mechanic} is not allowed by {_profile.BandId} or not unlocked at L{level} (FR-031).");
                    }
                }
            }
            else
            {
                chosen = OverlayPlanner.Choose(_profile, level, _roadmap, 2, ref rng);
            }

            var skeleton = new LevelDefinition(
                level,
                1,
                levelSeed,
                Version,
                new PictureRef(picture.Id, picture.Version, mirror, "default"),
                mapping,
                entries,
                Array.Empty<CellOverlay>(),
                Array.Empty<SpecialDef>(),
                Array.Empty<LockDef>(),
                new SlotsDef(SlotsDef.DefaultCount, null),
                new TrayDef(Array.Empty<IReadOnlyList<string>>()),
                Array.Empty<PodDef>(),
                new DifficultyDef(target, 0, false),
                "standard",
                Array.Empty<string>());

            var active = new List<VariantId>(new SortedSet<VariantId>(mapping.Values));
            Board plain = BoardBuilder.Build(skeleton, picture, VariantCatalog.Default);
            BoardPlan boardPlan = OverlayPlanner.BoardOverlays(plain, BackgroundCells(picture, mirror), chosen, _profile, level, active, ref rng);
            List<CellOverlay> boardOverlays = boardPlan.Overlays;
            skeleton = skeleton with { Overlays = boardOverlays };
            Board board = BoardBuilder.Build(skeleton, picture, VariantCatalog.Default);
            int work = board.CountAllLayers();
            if (!_profile.Work.Contains(work))
            {
                reason = $"work:{work}-outside-{_profile.Work}";
                return null;
            }

            int depth = Solver.Solver.DependencyDepth(board);
            if (!_profile.StructureTargets.NestingDepth.Contains(depth))
            {
                reason = $"structure:depth-{depth}-outside-{_profile.StructureTargets.NestingDepth}";
                return null;
            }

            // 5. Plan the solution waves.
            IReadOnlyList<Wave>? waves = SolutionPlanner.Plan(board, VariantCatalog.Default);
            if (waves == null)
            {
                reason = "plan:unreachable-region";
                return null;
            }

            // 6. Pods along the waves.
            int extra = target == DifficultyClass.Normal ? 0 : _profile.HardMode.ExtraPods;
            IReadOnlyList<PlannedPod>? planned = PodPartitioner.Partition(waves, _profile.PodCount, _profile.PodSize, extra, ref rng);
            if (planned == null)
            {
                reason = $"partition:{waves.Count}-waves-outside-{_profile.PodCount}-pods";
                return null;
            }

            // 4b. Keys on tiles the plan clears early, locks on what it needs later; key doors, gates and Fountains.
            LockPlan locks = OverlayPlanner.Locks(board, chosen, waves, planned, boardPlan, ref rng);
            var pods = new List<PlannedPod>(planned);
            foreach (KeyValuePair<int, string> locked in locks.LockedPods)
            {
                pods[locked.Key] = pods[locked.Key] with { LockKeyId = locked.Value };
            }

            if (ForcedMechanics != null && locks.Dropped.Count > 0)
            {
                reason = "showcase:cannot-place-" + string.Join(",", locks.Dropped);
                return null;
            }

            var mechanics = new List<string>(chosen.Where(m => !locks.Dropped.Contains(m)));
            if (PicturePicker.HasStones(picture) && !mechanics.Contains(MechanicNames.Stone))
            {
                mechanics.Add(MechanicNames.Stone);
            }

            mechanics.Sort(StringComparer.Ordinal);
            if (RepeatsMechanics(level, mechanics, history))
            {
                reason = "similarity:mechanics-3-in-a-row";
                return null;
            }

            skeleton = skeleton with
            {
                Overlays = MergeOverlays(boardOverlays, locks.KeyOverlays),
                Specials = locks.Specials,
                Locks = locks.Locks,
                Slots = new SlotsDef(SlotsDef.DefaultCount, locks.LockedSlot),
                Mechanics = mechanics,
            };

            // 7–8. Tray, difficulty injection, validation and scoring. Tuning uses a smaller budget. The searches are
            // deterministic depth-first walks, so a trace found within it is the one the full budget finds, and the
            // metrics walk is capped (Solver.MetricsNodeCap): an accepted level's analysis is identical under the full
            // profile budget recorded below.
            int stacks = Math.Min(pods.Count, _profile.Stacks.Min + rng.NextInt(_profile.Stacks.Max - _profile.Stacks.Min + 1));
            stacks = Math.Max(2, stacks);
            var options = new SolveOptions(_profile.SolverNodeBudget);
            var tuning = new SolveOptions(Math.Min(_profile.SolverNodeBudget, TuningNodeBudget));
            TrayOutcome? tray;
            try
            {
                tray = TrayBuilder.Tune(skeleton, picture, pods, stacks, target, requireLosable: true, _profile.HardMode.MaxInjections, _thresholds, tuning, ref rng, out string? trayReason);
                if (tray == null)
                {
                    reason = trayReason;
                    return null;
                }
            }
            catch (InvalidLevelException ex)
            {
                reason = "mechanics:" + ex.Message;
                return null;
            }

            // 7b. A connected pair at the same depth, if the level uses them and one keeps the level as planned.
            if (mechanics.Contains(MechanicNames.ConnectedPair))
            {
                TrayOutcome? connected = TrayBuilder.Connect(tray, picture, target, _thresholds, tuning, ref rng);
                if (connected != null)
                {
                    tray = connected;
                }
                else if (ForcedMechanics != null)
                {
                    reason = "showcase:cannot-place-connected_pair";
                    return null;
                }
                else
                {
                    mechanics.Remove(MechanicNames.ConnectedPair);
                    tray = tray with { Definition = tray.Definition with { Mechanics = mechanics } };
                }
            }

            // Mystery tiles: no blind guesses (FR-039, R8).
            bool? playerInfoFair = null;
            if (mechanics.Contains(MechanicNames.MysteryTile))
            {
                FairnessResult fairness = FairnessChecker.Check(tray.Definition, picture, new SessionOptions(1, 20000), tuning.NodeBudget);
                if (fairness.Status != FairnessStatus.Fair)
                {
                    reason = "fairness:" + fairness.Status.ToString().ToLowerInvariant();
                    return null;
                }

                playerInfoFair = fairness.PlayerInfoFair;
            }

            if (RepeatsSourceLayout(level, tray.Definition, history))
            {
                reason = "similarity:source-layout-within-50";
                return null;
            }

            LevelDefinition definition = tray.Definition with { Difficulty = new DifficultyDef(tray.Class, tray.Score, false) };

            // 9. Emit.
            LevelAnalysis analysis = tray.Analysis;
            var record = new ValidationRecord(
                level,
                definition.DefinitionVersion,
                ValidationRecord.HashOf(definition),
                Solver.Solver.Version,
                options.NodeBudget,
                analysis.NodesUsed,
                ValidationResult.Solvable,
                analysis.Win.Trace,
                analysis.Jam.Trace,
                playerInfoFair,
                analysis.Metrics.ToDictionary(),
                playerInfoFair == null
                    ? new[] { "solvable", "losable", "accounting", "board", "picture-approved" }
                    : new[] { "solvable", "losable", "accounting", "board", "picture-approved", "player-info-fair" });
            reason = null;
            return new GeneratedLevel(definition, record);
        }

        /// <summary>Per board cell (after mirroring): whether the picture has its background role there.</summary>
        private static bool[] BackgroundCells(BasePicture picture, Mirror mirror)
        {
            var background = new bool[picture.Width * picture.Height];
            for (int y = 0; y < picture.Height; y++)
            {
                for (int x = 0; x < picture.Width; x++)
                {
                    int sourceX = mirror == Mirror.Horizontal ? picture.Width - 1 - x : x;
                    int role = picture.CellAt(sourceX, y);
                    background[(y * picture.Width) + x] = role >= 0 && picture.Roles[role].IsBackground;
                }
            }

            return background;
        }

        /// <summary>The board overlays plus the key overlays, one per cell, sorted by row and column.</summary>
        private static List<CellOverlay> MergeOverlays(IReadOnlyList<CellOverlay> board, IReadOnlyList<CellOverlay> keys)
        {
            var byCell = new Dictionary<CellPos, CellOverlay>();
            foreach (CellOverlay overlay in board)
            {
                byCell[overlay.Cell] = overlay;
            }

            foreach (CellOverlay key in keys)
            {
                byCell[key.Cell] = byCell.TryGetValue(key.Cell, out CellOverlay? existing) ? existing with { KeyId = key.KeyId } : key;
            }

            var merged = new List<CellOverlay>(byCell.Values);
            merged.Sort((a, b) => a.Cell.Y != b.Cell.Y ? a.Cell.Y.CompareTo(b.Cell.Y) : a.Cell.X.CompareTo(b.Cell.X));
            return merged;
        }

        /// <summary>FR-083: no 3 consecutive levels share the same active variant set.</summary>
        private static bool RepeatsVariantSet(int level, IReadOnlyDictionary<string, VariantId> mapping, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            var set = new SortedSet<VariantId>(mapping.Values);
            return history.TryGetValue(level - 1, out LevelDefinition? a) && history.TryGetValue(level - 2, out LevelDefinition? b)
                && set.SetEquals(VariantSet(a!)) && set.SetEquals(VariantSet(b!));
        }

        /// <summary>FR-083: no 3 consecutive levels share the same (non-empty) set of mechanics.</summary>
        private static bool RepeatsMechanics(int level, IReadOnlyList<string> mechanics, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            if (mechanics.Count == 0)
            {
                return false;
            }

            var set = new SortedSet<string>(mechanics, StringComparer.Ordinal);
            return history.TryGetValue(level - 1, out LevelDefinition? a) && history.TryGetValue(level - 2, out LevelDefinition? b)
                && set.SetEquals(a!.Mechanics) && set.SetEquals(b!.Mechanics);
        }

        /// <summary>FR-083: a Source layout signature never repeats within 50 levels.</summary>
        private static bool RepeatsSourceLayout(int level, LevelDefinition definition, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            string signature = SourceSignature(definition);
            for (int l = level - 49; l < level; l++)
            {
                if (history.TryGetValue(l, out LevelDefinition? other) && string.Equals(SourceSignature(other!), signature, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static SortedSet<VariantId> VariantSet(LevelDefinition definition)
        {
            var set = new SortedSet<VariantId>();
            foreach (PodDef pod in definition.Pods)
            {
                set.Add(pod.Variant);
            }

            return set;
        }

        /// <summary>Stack count plus the ordered pod counts per stack (FR-083).</summary>
        public static string SourceSignature(LevelDefinition definition)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (PodDef pod in definition.Pods)
            {
                counts[pod.Id] = pod.Count;
            }

            var parts = new List<string>();
            foreach (IReadOnlyList<string> stack in definition.Tray.Stacks)
            {
                var ids = new List<string>();
                foreach (string id in stack)
                {
                    ids.Add(counts[id].ToString(CultureInfo.InvariantCulture));
                }

                parts.Add(string.Join(",", ids));
            }

            return definition.Tray.Stacks.Count.ToString(CultureInfo.InvariantCulture) + ":" + string.Join("|", parts);
        }

        private sealed class ReadOnlyHistory : IReadOnlyDictionary<int, LevelDefinition>
        {
            private readonly IDictionary<int, LevelDefinition> _inner;

            public ReadOnlyHistory(IDictionary<int, LevelDefinition> inner)
            {
                _inner = inner;
            }

            public LevelDefinition this[int key] => _inner[key];

            public IEnumerable<int> Keys => _inner.Keys;

            public IEnumerable<LevelDefinition> Values => _inner.Values;

            public int Count => _inner.Count;

            public bool ContainsKey(int key) => _inner.ContainsKey(key);

            public bool TryGetValue(int key, out LevelDefinition value) => _inner.TryGetValue(key, out value!);

            public IEnumerator<KeyValuePair<int, LevelDefinition>> GetEnumerator() => _inner.GetEnumerator();

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();
        }
    }
}
