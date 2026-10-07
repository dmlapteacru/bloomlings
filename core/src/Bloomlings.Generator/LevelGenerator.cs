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
    /// A level keeps both its profile and the spec's Level Band Guidelines (<see cref="BandGuidelines"/>): the variant
    /// count, work and pod count are the overlap of the two for its level and class, pods keep the minimum size, all
    /// four families recur from L20, and the winning line's peak slot use meets the band's buffer-pressure target
    /// (Normal levels stay within it; Hard and Super Hard reach at least its minimum, tighter is their point).
    /// The board follows the level's board rule (<see cref="BandGuidelines.Board"/>, FR-008 as amended on 2026-10-07):
    /// from L11 a picture of any size from 14 × 16 to 22 × 28, drawn about evenly, whose cells scale the level's pods,
    /// work and class thresholds (<see cref="BandGuidelines.For(int, int)"/>, <see cref="BandGuidelines.ThresholdsFor"/>).
    /// Every level stores its board look from the cell count (<see cref="BoardLooks.For"/>); an icons board (over 288
    /// cells) gets fewer hidden layers, no mystery, a lower buffer pressure and the hidden-layer fairness check
    /// (<see cref="FairnessChecker"/>, research R8b).
    /// </summary>
    public sealed class LevelGenerator
    {
        /// <summary>Recorded in each definition; bump it when generator behavior changes.</summary>
        public const string Version = "gen-1.3.0";

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

        /// <summary>
        /// Whether the Level Band Guidelines apply (default). The Daily Challenge pool turns them off: its level numbers
        /// are pool indexes, not Level N, and its profile holds the mid-game band values it uses.
        /// </summary>
        public bool UseBandGuidelines { get; set; } = true;

        /// <summary>Which expansion variants join the pool, and when (FR-060).</summary>
        public VariantPool Pool { get; set; } = VariantPool.Default;

        /// <summary>A fixed difficulty class instead of the schedule (showcase levels are Normal).</summary>
        public DifficultyClass? ForcedClass { get; set; }

        /// <summary>
        /// The class of each level instead of the schedule, when <see cref="ForcedClass"/> is not set (the Daily Challenge
        /// pool's week, <see cref="DailyPlan.WeeklyClass"/>).
        /// </summary>
        public Func<int, DifficultyClass>? ClassOf { get; set; }

        /// <summary>
        /// The Level N whose unlocks apply to every level generated (mechanics and their combinations, layers below a top,
        /// the advanced Hard pressure), instead of each level's own number; null uses the level's. The Daily Challenge pool,
        /// whose numbers are pool indexes, plays as at its unlock (<c>system.daily_challenge</c>, L50).
        /// </summary>
        public int? RulesLevel { get; set; }

        /// <summary>
        /// The band's class thresholds on the biggest board, 22 × 28 (its <c>big</c> thresholds in
        /// <c>difficulty-thresholds.json</c>): a big board's thresholds grow from the band's own toward them with its cells
        /// (<see cref="BandGuidelines.ThresholdsFor"/>), since its scores grow with the board. Null keeps the band's own.
        /// </summary>
        public DifficultyThresholds? BigBoardThresholds { get; set; }

        /// <summary>Called after each level with the level number, the accepted level (null when every candidate failed) and the candidates tried.</summary>
        public Action<int, GeneratedLevel?, int>? Progress { get; set; }

        /// <param name="history">Earlier levels by number, for the FR-083 windows; accepted levels are added to it.</param>
        /// <param name="keep">Levels that are already fixed (curated or showcase): they stay in the history and are not generated.</param>
        public GenerationResult Generate(int firstLevel, int lastLevel, ulong seed, IDictionary<int, LevelDefinition> history, ISet<int>? keep = null)
        {
            var result = new GenerationResult();
            for (int level = firstLevel; level <= lastLevel; level++)
            {
                if (keep != null && keep.Contains(level))
                {
                    continue;
                }

                GenerateOne(level, seed, history, result);
            }

            return result;
        }

        /// <summary>
        /// Generates <paramref name="levels"/> again between the levels around them (the seams of a parallel build): each
        /// one leaves the history first, so its candidates are judged against both its earlier and its later neighbours
        /// (<see cref="Conflicts"/>). Accepted levels replace theirs in the history.
        /// </summary>
        public GenerationResult Regenerate(IEnumerable<int> levels, ulong seed, IDictionary<int, LevelDefinition> history)
        {
            var result = new GenerationResult();
            foreach (int level in levels)
            {
                history.Remove(level);
                GenerateOne(level, seed, history, result);
            }

            return result;
        }

        private void GenerateOne(int level, ulong seed, IDictionary<int, LevelDefinition> history, GenerationResult result)
        {
            var readOnlyHistory = new ReadOnlyHistory(history);
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

        /// <summary>
        /// The FR-083 and band-guideline repetition rules <paramref name="level"/> breaks with the levels around it in
        /// <paramref name="history"/>, on both sides: the same picture within 50 levels (or twice in Levels 1–100), the
        /// same Source layout within 50, three in a row with the same variant set or the same mechanics, and a window of
        /// <see cref="BandGuidelines.FamilyWindow"/> levels from L<see cref="BandGuidelines.AllFamiliesFrom"/> without all
        /// four families. Empty when the level fits (the seam check of a parallel build).
        /// </summary>
        /// <param name="mechanicsOf">
        /// The mechanics a level uses as the validator judges them (<see cref="MechanicsUsed"/>); null compares the levels'
        /// own <c>mechanics</c> lists.
        /// </param>
        public static IReadOnlyList<string> Conflicts(int level, IReadOnlyDictionary<int, LevelDefinition> history, Func<LevelDefinition, IReadOnlyList<string>?>? mechanicsOf = null)
        {
            var conflicts = new List<string>();
            if (!history.TryGetValue(level, out LevelDefinition? definition))
            {
                return conflicts;
            }

            string signature = SourceSignature(definition!);
            foreach (KeyValuePair<int, LevelDefinition> entry in history)
            {
                if (entry.Key == level)
                {
                    continue;
                }

                bool near = Math.Abs(entry.Key - level) < PicturePicker.RepeatWindow;
                bool uniqueTier = level <= PicturePicker.UniqueUpToLevel && entry.Key <= PicturePicker.UniqueUpToLevel;
                if ((near || uniqueTier) && string.Equals(entry.Value.Picture.Id, definition!.Picture.Id, StringComparison.Ordinal))
                {
                    conflicts.Add($"picture:{definition.Picture.Id}-also-at-L{entry.Key}");
                }

                if (near && string.Equals(SourceSignature(entry.Value), signature, StringComparison.Ordinal))
                {
                    conflicts.Add($"source:{signature}-also-at-L{entry.Key}");
                }
            }

            foreach (int earlier in FarReuses(level, history))
            {
                conflicts.Add($"reuse:{definition!.Picture.Id}-like-L{earlier}");
            }

            SortedSet<VariantId> variants = VariantSet(definition!);
            if (ThreeInARow(level, history, other => VariantSet(other).SetEquals(variants)))
            {
                conflicts.Add("similarity:variant-set-3-in-a-row");
            }

            bool sameMechanics = mechanicsOf == null
                ? !(definition!.Mechanics.Count == 0 && level <= 10)
                    && ThreeInARow(level, history, other => new SortedSet<string>(other.Mechanics, StringComparer.Ordinal).SetEquals(definition.Mechanics))
                : mechanicsOf(definition!) is IReadOnlyList<string> used && RepeatsMechanicsUsed(level, used, history, mechanicsOf);
            if (sameMechanics)
            {
                conflicts.Add("similarity:mechanics-3-in-a-row");
            }

            if (FamilyGap(level, definition.Mapping, history, judgeIncomplete: false))
            {
                conflicts.Add("families:not-all-four-in-" + BandGuidelines.FamilyWindow.ToString(CultureInfo.InvariantCulture));
            }

            return conflicts;
        }

        private GeneratedLevel? TryGenerate(int level, ulong levelSeed, IReadOnlyDictionary<int, LevelDefinition> history, out string? reason)
        {
            // The unlocks that apply: the level's own, or the fixed rules level of a pool (the Daily Challenge's).
            int rules = RulesLevel ?? level;

            // A showcase's mechanics must be allowed and unlocked (FR-031): a wrong request is refused before any candidate.
            foreach (string mechanic in ForcedMechanics ?? Array.Empty<string>())
            {
                int? at = _roadmap.LevelOf(MechanicNames.UnlockId(mechanic));
                if (!_profile.Allows(mechanic) || at == null || at.Value > rules)
                {
                    throw new ArgumentException($"{mechanic} is not allowed by {_profile.BandId} or not unlocked at L{rules} (FR-031).");
                }
            }

            var rng = new Xoshiro256StarStar(levelSeed);
            DifficultyClass target = ForcedClass ?? ClassOf?.Invoke(level) ?? _schedule.ClassFor(level);

            // The profile and the band guidelines both apply: their overlap for this level and class.
            IntRange? variantCount = UseBandGuidelines ? BandGuidelines.Intersect(_profile.VariantCount, BandGuidelines.Variants(level, target)) : _profile.VariantCount;
            if (variantCount == null)
            {
                reason = $"profile:{_profile.BandId}-outside-guidelines-at-L{level}";
                return null;
            }

            // The pool's expansion variants for this level: a picture whose role needs one that has not joined yet is
            // left out (the owner, 2026-10-07).
            IReadOnlyList<VariantId> expansions = UseBandGuidelines ? Pool.ExpansionsAt(level, _roadmap) : Array.Empty<VariantId>();

            // 1. Picture, of the level's board rule: from L11 any size from 14 × 16 to 22 × 28, about evenly (FR-008 as
            // amended on 2026-10-07).
            BasePicture? picture = _pictures.Pick(_profile, level, history, ref rng, UseBandGuidelines ? BandGuidelines.Board(level) : null, expansions);
            if (picture == null)
            {
                reason = "picture:none-available";
                return null;
            }

            // The board's pods, work and thresholds grow with its cells (a big board's, BandGuidelines.For).
            int cells = picture.Width * picture.Height;
            IntRange? workRange = UseBandGuidelines ? BandGuidelines.Intersect(_profile.Work, BandGuidelines.Work(level, target, cells)) : _profile.Work;
            IntRange? podRange = UseBandGuidelines ? BandGuidelines.Intersect(_profile.PodCount, BandGuidelines.For(level, cells).Pods) : _profile.PodCount;
            DifficultyThresholds thresholds = UseBandGuidelines ? BandGuidelines.ThresholdsFor(_thresholds, BigBoardThresholds, cells) : _thresholds;
            if (workRange == null || podRange == null)
            {
                reason = $"profile:{_profile.BandId}-outside-guidelines-at-L{level}-on-{picture.Width}x{picture.Height}";
                return null;
            }

            // The board look follows the cell count and is stored in the level (FR-036 as amended on 2026-10-06).
            BoardLook look = BoardLooks.For(picture.Width, picture.Height);

            // 2. Mapping, avoiding the variant set of the two previous levels when they share one (FR-083).
            // A variant joining the pool gets one clean level (it is used and no mechanic is) and then one mixed level
            // (it is used again), FR-031.
            (VariantId? introduce, bool cleanIntro) = UseBandGuidelines ? Introduction(level) : (null, false);
            IReadOnlyList<SortedDictionary<string, VariantId>> mappings = RoleMapper.Mappings(picture, _profile, _readablePair, variantCount: variantCount, extraVariants: expansions);
            if (introduce != null)
            {
                mappings = mappings.Where(m => m.Values.Contains(introduce.Value)).ToList();
            }

            if (mappings.Count == 0)
            {
                reason = introduce != null ? $"intro:no-mapping-with-{introduce.Value.Key}-on-{picture.Id}" : $"mapping:none-for-{picture.Id}";
                return null;
            }

            SortedDictionary<string, VariantId> mapping = mappings[rng.NextInt(mappings.Count)];
            if (RepeatsVariantSet(level, mapping, history))
            {
                reason = "similarity:variant-set-3-in-a-row";
                return null;
            }

            if (UseBandGuidelines && MissesAFamily(level, mapping, history))
            {
                reason = "families:not-all-four-in-" + BandGuidelines.FamilyWindow.ToString(CultureInfo.InvariantCulture);
                return null;
            }

            // 3. Entries and mirroring toward the structure target.
            string layout = _profile.EntryLayouts[rng.NextInt(_profile.EntryLayouts.Count)];
            Mirror mirror = rng.NextInt(2) == 0 ? Mirror.None : Mirror.Horizontal;

            // FR-083: a picture used again beyond the window differs from each other use in its look (mapping or mirror).
            if (ReusesLook(level, picture.Id, mirror, mapping, history))
            {
                reason = "similarity:reuse-same-look";
                return null;
            }
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
            }
            else if (cleanIntro)
            {
                chosen = new List<string>();
            }
            else if (UseBandGuidelines && PracticeAt(level) is string practice)
            {
                // FR-031: the practice level uses the newly shown mechanic again, alone.
                chosen = new List<string> { practice };
            }
            else
            {
                int? combinations = _roadmap.LevelOf(AdvancedCombinationsUnlock);
                chosen = OverlayPlanner.Choose(_profile, rules, _roadmap, combinations != null && rules >= combinations.Value ? 3 : 2, ref rng, target);
            }

            // No mystery on an icons board: its hidden layers are hidden information already, and the fairness check
            // covers them alone (research R8b).
            if (look == BoardLook.Icons)
            {
                foreach (string mystery in new[] { MechanicNames.MysteryTile, MechanicNames.MysteryPod })
                {
                    if (chosen.Remove(mystery) && (ForcedMechanics != null || IsPracticeOf(level, mystery)))
                    {
                        reason = "look:no-" + mystery + "-on-an-icons-board";
                        return null;
                    }
                }
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
                Array.Empty<string>())
            {
                BoardLook = look,
            };

            var active = new List<VariantId>(new SortedSet<VariantId>(mapping.Values));
            Board plain = BoardBuilder.Build(skeleton, picture, VariantCatalog.Default);
            BoardPlan boardPlan = OverlayPlanner.BoardOverlays(plain, BackgroundCells(picture, mirror), chosen, _profile, rules, active, ref rng, look);
            List<CellOverlay> boardOverlays = boardPlan.Overlays;
            skeleton = skeleton with { Overlays = boardOverlays };
            Board board = BoardBuilder.Build(skeleton, picture, VariantCatalog.Default);
            int work = board.CountAllLayers();
            if (!workRange.Contains(work))
            {
                reason = $"work:{work}-outside-{workRange}";
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
            IReadOnlyList<PlannedPod>? planned = PodPartitioner.Partition(waves, podRange, _profile.PodSize, extra, ref rng);
            if (planned == null)
            {
                reason = $"partition:{waves.Count}-waves-outside-{podRange}-pods-of-{BandGuidelines.MinPodSize}+";
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

            if (UseBandGuidelines && ForcedMechanics == null && PracticeAt(level) is string practiced && locks.Dropped.Contains(practiced))
            {
                reason = "practice:cannot-place-" + practiced;
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
                tray = TrayBuilder.Tune(skeleton, picture, pods, stacks, target, requireLosable: true, _profile.HardMode.MaxInjections, thresholds, tuning, ref rng, out string? trayReason);
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

            // 7b. A connected group at the same depth (a pair, or the optional triple on Hard levels), and a mystery pod,
            // if the level uses them and one keeps the level as planned.
            foreach ((string mechanic, int size) in new[] { (MechanicNames.ConnectedTriple, 3), (MechanicNames.ConnectedPair, 2) })
            {
                if (!mechanics.Contains(mechanic))
                {
                    continue;
                }

                // One group per level: a triple, when chosen, stands for the pair too.
                TrayOutcome? connected = tray.Definition.Pods.Any(p => p.ConnectedGroupId != null)
                    ? null
                    : TrayBuilder.Connect(tray, picture, target, thresholds, tuning, ref rng, size);
                if (connected != null)
                {
                    tray = connected;
                }
                else if (ForcedMechanics != null && ForcedMechanics.Contains(mechanic))
                {
                    reason = "showcase:cannot-place-" + mechanic;
                    return null;
                }
                else if (IsPracticeOf(level, mechanic))
                {
                    // FR-031: the practice level must use its mechanic; another candidate may fit it.
                    reason = "practice:cannot-place-" + mechanic;
                    return null;
                }
                else
                {
                    mechanics.Remove(mechanic);
                    tray = tray with { Definition = tray.Definition with { Mechanics = mechanics } };
                }
            }

            if (mechanics.Contains(MechanicNames.MysteryPod))
            {
                TrayOutcome? hidden = TrayBuilder.HidePod(tray, picture, target, thresholds, tuning, ref rng);
                if (hidden != null)
                {
                    tray = hidden;
                }
                else if (ForcedMechanics != null)
                {
                    reason = "showcase:cannot-place-mystery_pod";
                    return null;
                }
                else if (IsPracticeOf(level, MechanicNames.MysteryPod))
                {
                    reason = "practice:cannot-place-mystery_pod";
                    return null;
                }
                else
                {
                    mechanics.Remove(MechanicNames.MysteryPod);
                    tray = tray with { Definition = tray.Definition with { Mechanics = mechanics } };
                }
            }

            // FR-083 again, as the validator judges it: the mechanics the level's content now uses, which differ from the
            // planned ones when a connected group or a mystery pod found no place, or a gate became a key door (the owner's
            // catalog run of 2026-10-07: L4987 repeated chest and stone after its pair was dropped).
            if (RepeatsMechanicsUsed(level, LevelMechanics.UnlocksUsed(tray.Definition, picture), history, MechanicsUsed))
            {
                reason = "similarity:mechanics-used-3-in-a-row";
                return null;
            }

            // Buffer pressure (the band's target, as peak occupied slots on the winning line; an icons board's, of a big
            // board or a Daily Challenge entry, is lower and capped for every class, slack for its hidden layers).
            bool icons = look == BoardLook.Icons;
            IntRange pressure = icons ? BandGuidelines.BigBoardPeakSlots(tray.Class) : BandGuidelines.PeakSlots(PressureFor(rules, tray.Class));
            int peak = tray.Analysis.Metrics.PeakBuffer;
            if (peak < pressure.Min || ((icons || tray.Class == DifficultyClass.Normal) && peak > pressure.Max))
            {
                reason = $"pressure:peak-{peak}-outside-{pressure}";
                return null;
            }

            // Mystery tiles and pods, and the hidden layers of an icons board: no blind guesses (FR-036, FR-039, R8, R8b).
            bool? playerInfoFair = null;
            bool hiddenLayers = look == BoardLook.Icons && tray.Definition.Overlays.Any(o => o.LayersBelow.Count > 0);
            if (hiddenLayers || mechanics.Contains(MechanicNames.MysteryTile) || mechanics.Contains(MechanicNames.MysteryPod))
            {
                FairnessResult fairness = FairnessChecker.Check(tray.Definition, picture, new SessionOptions(1, 20000), tuning.NodeBudget, BandGuidelines.MaxLayersBelow(rules), BandGuidelines.MaxHiddenLayersOnIcons);
                if (fairness.Status != FairnessStatus.Fair)
                {
                    reason = (hiddenLayers ? "fairness:hidden-layers-" : "fairness:") + fairness.Status.ToString().ToLowerInvariant();
                    return null;
                }

                playerInfoFair = fairness.PlayerInfoFair;
            }

            if (RepeatsSourceLayout(level, tray.Definition, history))
            {
                reason = "similarity:source-layout-within-50";
                return null;
            }

            // FR-083: and in its Source design.
            if (ReusesSource(level, picture.Id, tray.Definition, history))
            {
                reason = "similarity:reuse-same-source";
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

        private bool IsPracticeOf(int level, string mechanic) => UseBandGuidelines && ForcedMechanics == null && PracticeAt(level) == mechanic;

        /// <summary>The mechanic whose practice level this is (FR-031), when the profile allows it.</summary>
        private string? PracticeAt(int level)
        {
            foreach (string mechanic in MechanicNames.All)
            {
                if (_profile.Allows(mechanic) && MechanicNames.PracticeLevel(mechanic, _roadmap, _schedule.ClassFor) == level)
                {
                    return mechanic;
                }
            }

            return null;
        }

        /// <summary>
        /// The variant this level must introduce: at the level a pool expansion joins, the clean level (true); on the
        /// next level, the mixed level (false). Only when some picture can carry it (its color group), else nothing.
        /// </summary>
        private (VariantId? Variant, bool Clean) Introduction(int level)
        {
            foreach ((int at, bool clean) in new[] { (level, true), (level - 1, false) })
            {
                foreach (VariantId variant in Pool.JoiningAt(at, _roadmap))
                {
                    if (_pictures.HasColorGroup(VariantCatalog.Default.Get(variant).ColorGroup))
                    {
                        return (variant, clean);
                    }
                }
            }

            return (null, false);
        }

        /// <summary>Unlock of the advanced connected/locked combinations profile (roadmap L175): up to 3 mechanics.</summary>
        public const string AdvancedCombinationsUnlock = "profile.advanced_combinations";

        /// <summary>Unlock of the advanced Hard profile (roadmap L225): Hard and Super Hard press the buffer critically.</summary>
        public const string AdvancedHardUnlock = "profile.advanced_hard";

        /// <summary>The buffer-pressure target of a level's class; from the advanced Hard profile, critical for Hard too.</summary>
        private BufferPressure PressureFor(int level, DifficultyClass difficulty)
        {
            if (difficulty == DifficultyClass.Normal)
            {
                return _profile.BufferPressureTarget;
            }

            int? advanced = _roadmap.LevelOf(AdvancedHardUnlock);
            if (advanced != null && level >= advanced.Value)
            {
                return BufferPressure.Critical;
            }

            return difficulty == DifficultyClass.SuperHard ? _profile.HardMode.SuperHardPressure : _profile.HardMode.HardPressure;
        }

        /// <summary>
        /// From L20 all four families are regular: the <see cref="BandGuidelines.FamilyWindow"/> levels ending at this one
        /// use all four, and so does every later window through it whose levels are all known (a level generated between
        /// its neighbours, <see cref="Regenerate"/>).
        /// </summary>
        private static bool MissesAFamily(int level, IReadOnlyDictionary<string, VariantId> mapping, IReadOnlyDictionary<int, LevelDefinition> history) =>
            FamilyGap(level, mapping, history, judgeIncomplete: false);

        private static bool FamilyGap(int level, IReadOnlyDictionary<string, VariantId> mapping, IReadOnlyDictionary<int, LevelDefinition> history, bool judgeIncomplete)
        {
            if (level < BandGuidelines.AllFamiliesFrom)
            {
                return false;
            }

            int window = BandGuidelines.FamilyWindow;
            for (int start = level - window + 1; start <= level; start++)
            {
                // The window ending at the level is judged from L20 as before; a later one only from its own start at L20.
                if (start > level - window + 1 && start < BandGuidelines.AllFamiliesFrom)
                {
                    continue;
                }

                var families = new HashSet<Family>();
                foreach (VariantId variant in mapping.Values)
                {
                    families.Add(VariantCatalog.Default.Get(variant).Family);
                }

                bool complete = true;
                for (int l = start; l < start + window && complete; l++)
                {
                    if (l == level)
                    {
                        continue;
                    }

                    if (!history.TryGetValue(l, out LevelDefinition? other))
                    {
                        complete = false; // Not enough history to judge (the start of a batch without its predecessors).
                        break;
                    }

                    foreach (VariantId variant in other!.Mapping.Values)
                    {
                        families.Add(VariantCatalog.Default.Get(variant).Family);
                    }
                }

                if ((complete || judgeIncomplete) && families.Count < 4)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>FR-083: no 3 consecutive levels share the same active variant set (this level first, middle or last).</summary>
        private static bool RepeatsVariantSet(int level, IReadOnlyDictionary<string, VariantId> mapping, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            var set = new SortedSet<VariantId>(mapping.Values);
            return ThreeInARow(level, history, other => set.SetEquals(VariantSet(other)));
        }

        /// <summary>Whether two known neighbours of <paramref name="level"/> next to it in a row of three both match.</summary>
        private static bool ThreeInARow(int level, IReadOnlyDictionary<int, LevelDefinition> history, Func<LevelDefinition, bool> same)
        {
            foreach ((int a, int b) in new[] { (-2, -1), (-1, 1), (1, 2) })
            {
                if (history.TryGetValue(level + a, out LevelDefinition? x) && history.TryGetValue(level + b, out LevelDefinition? y) && same(x!) && same(y!))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The roadmap unlocks a level uses (<see cref="LevelMechanics.UnlocksUsed"/>), which is how the validator judges
        /// FR-083's mechanics rule, or null when the library has no such picture.
        /// </summary>
        public IReadOnlyList<string>? MechanicsUsed(LevelDefinition level)
        {
            BasePicture? picture = _pictures.Find(level.Picture.Id, level.Picture.Version);
            return picture == null ? null : LevelMechanics.UnlocksUsed(level, picture);
        }

        /// <summary>
        /// FR-083 as <c>CatalogValidator</c> judges it: no 3 consecutive levels use the same mechanics (sorted unlock ids,
        /// <see cref="MechanicsUsed"/>); from L11 none at all counts too. A neighbour whose picture is unknown matches
        /// nothing, as in the validator.
        /// </summary>
        private static bool RepeatsMechanicsUsed(int level, IReadOnlyList<string> used, IReadOnlyDictionary<int, LevelDefinition> history, Func<LevelDefinition, IReadOnlyList<string>?> mechanicsOf)
        {
            if (used.Count == 0 && level <= 10)
            {
                return false;
            }

            return ThreeInARow(level, history, other => mechanicsOf(other) is IReadOnlyList<string> theirs && theirs.SequenceEqual(used, StringComparer.Ordinal));
        }

        /// <summary>FR-083: no 3 consecutive levels share the same set of mechanics; from L11 the empty set counts too.</summary>
        private static bool RepeatsMechanics(int level, IReadOnlyList<string> mechanics, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            if (mechanics.Count == 0 && level <= 10)
            {
                return false;
            }

            var set = new SortedSet<string>(mechanics, StringComparer.Ordinal);
            return ThreeInARow(level, history, other => set.SetEquals(other.Mechanics));
        }

        /// <summary>
        /// FR-083: the earlier levels that the level's picture repeats beyond the repetition window with the same look (the
        /// same mirroring and mapping, <see cref="SameLook"/>) or the same Source design (<see cref="SourceSignature"/>).
        /// Only earlier levels count, as the validator judges them: of two such levels the later one is redone.
        /// </summary>
        public static IReadOnlyList<int> FarReuses(int level, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            var reuses = new List<int>();
            if (!history.TryGetValue(level, out LevelDefinition? definition))
            {
                return reuses;
            }

            string signature = SourceSignature(definition!);
            foreach (KeyValuePair<int, LevelDefinition> entry in history)
            {
                if (entry.Key < level && level - entry.Key >= PicturePicker.RepeatWindow
                    && string.Equals(entry.Value.Picture.Id, definition!.Picture.Id, StringComparison.Ordinal)
                    && (SameLook(definition, entry.Value) || string.Equals(SourceSignature(entry.Value), signature, StringComparison.Ordinal)))
                {
                    reuses.Add(entry.Key);
                }
            }

            return reuses;
        }

        /// <summary>The same mirroring and the same mapping of roles to variants (FR-083's "look" of a picture's use).</summary>
        public static bool SameLook(LevelDefinition a, LevelDefinition b) =>
            a.Picture.Mirror == b.Picture.Mirror && SameMapping(a.Mapping, b.Mapping);

        public static bool SameMapping(IReadOnlyDictionary<string, VariantId> a, IReadOnlyDictionary<string, VariantId> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, VariantId> pair in a)
            {
                if (!b.TryGetValue(pair.Key, out VariantId other) || other != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// FR-083: another use of the picture beyond the window with the same mirroring and mapping, earlier or later. A
        /// level generated between known levels (a seam repair, or a gap filled after its band) must not repeat a later
        /// level's look either, or that later level would break the rule (the owner's catalog run of 2026-10-07: L3063
        /// repeated L2741's alarm_clock_09).
        /// </summary>
        public static bool ReusesLook(int level, string pictureId, Mirror mirror, IReadOnlyDictionary<string, VariantId> mapping, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            foreach (KeyValuePair<int, LevelDefinition> entry in history)
            {
                if (entry.Key != level && Math.Abs(level - entry.Key) >= PicturePicker.RepeatWindow
                    && string.Equals(entry.Value.Picture.Id, pictureId, StringComparison.Ordinal)
                    && entry.Value.Picture.Mirror == mirror && SameMapping(entry.Value.Mapping, mapping))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>FR-083: another use of the picture beyond the window with the same Source design, earlier or later (as <see cref="ReusesLook"/>).</summary>
        public static bool ReusesSource(int level, string pictureId, LevelDefinition definition, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            string signature = SourceSignature(definition);
            foreach (KeyValuePair<int, LevelDefinition> entry in history)
            {
                if (entry.Key != level && Math.Abs(level - entry.Key) >= PicturePicker.RepeatWindow
                    && string.Equals(entry.Value.Picture.Id, pictureId, StringComparison.Ordinal)
                    && string.Equals(SourceSignature(entry.Value), signature, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>FR-083: a Source layout signature never repeats within 50 levels (on either side).</summary>
        private static bool RepeatsSourceLayout(int level, LevelDefinition definition, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            string signature = SourceSignature(definition);
            for (int l = level - 49; l <= level + 49; l++)
            {
                if (l != level && history.TryGetValue(l, out LevelDefinition? other) && string.Equals(SourceSignature(other!), signature, StringComparison.Ordinal))
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
