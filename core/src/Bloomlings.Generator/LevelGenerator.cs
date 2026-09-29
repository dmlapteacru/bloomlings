using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Random;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
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

        private readonly GenerationProfile _profile;
        private readonly PicturePicker _pictures;
        private readonly DifficultyThresholds _thresholds;
        private readonly Func<VariantId, VariantId, bool> _readablePair;
        private readonly DifficultySchedule _schedule;

        public LevelGenerator(
            GenerationProfile profile,
            PicturePicker pictures,
            DifficultyThresholds thresholds,
            Func<VariantId, VariantId, bool> readablePair,
            DifficultySchedule schedule)
        {
            _profile = profile;
            _pictures = pictures;
            _thresholds = thresholds;
            _readablePair = readablePair;
            _schedule = schedule;
        }

        /// <param name="history">Earlier levels by number, for the FR-083 windows; accepted levels are added to it.</param>
        public GenerationResult Generate(int firstLevel, int lastLevel, ulong seed, IDictionary<int, LevelDefinition> history)
        {
            var result = new GenerationResult();
            var readOnlyHistory = new ReadOnlyHistory(history);
            for (int level = firstLevel; level <= lastLevel; level++)
            {
                bool accepted = false;
                for (int attempt = 0; attempt < _profile.MaxCandidatesPerLevel && !accepted; attempt++)
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
            }

            return result;
        }

        private GeneratedLevel? TryGenerate(int level, ulong levelSeed, IReadOnlyDictionary<int, LevelDefinition> history, out string? reason)
        {
            var rng = new Xoshiro256StarStar(levelSeed);
            DifficultyClass target = _schedule.ClassFor(level);

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

            var mechanics = new List<string>();
            if (PicturePicker.HasStones(picture))
            {
                mechanics.Add("stone");
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
                mechanics);

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
            IReadOnlyList<PlannedPod>? pods = PodPartitioner.Partition(waves, _profile.PodCount, _profile.PodSize, extra, ref rng);
            if (pods == null)
            {
                reason = $"partition:{waves.Count}-waves-outside-{_profile.PodCount}-pods";
                return null;
            }

            // 7–8. Tray, difficulty injection, validation and scoring.
            int stacks = Math.Min(pods.Count, _profile.Stacks.Min + rng.NextInt(_profile.Stacks.Max - _profile.Stacks.Min + 1));
            stacks = Math.Max(2, stacks);
            var options = new SolveOptions(_profile.SolverNodeBudget);
            TrayOutcome? tray = TrayBuilder.Tune(skeleton, picture, pods, stacks, target, requireLosable: true, _profile.HardMode.MaxInjections, _thresholds, options, ref rng, out string? trayReason);
            if (tray == null)
            {
                reason = trayReason;
                return null;
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
                null,
                analysis.Metrics.ToDictionary(),
                new[] { "solvable", "losable", "accounting", "board", "picture-approved" });
            reason = null;
            return new GeneratedLevel(definition, record);
        }

        /// <summary>FR-083: no 3 consecutive levels share the same active variant set.</summary>
        private static bool RepeatsVariantSet(int level, IReadOnlyDictionary<string, VariantId> mapping, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            var set = new SortedSet<VariantId>(mapping.Values);
            return history.TryGetValue(level - 1, out LevelDefinition? a) && history.TryGetValue(level - 2, out LevelDefinition? b)
                && set.SetEquals(VariantSet(a!)) && set.SetEquals(VariantSet(b!));
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
