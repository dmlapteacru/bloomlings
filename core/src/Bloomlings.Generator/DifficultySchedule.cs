using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Random;
using Bloomlings.Generator.Overlays;

namespace Bloomlings.Generator
{
    /// <summary>
    /// The target class of each level from L11 on (FR-059): Hard every 4–6 levels and Super Hard every 10–15, spaced
    /// irregularly by a seeded PRNG, the level after a Super Hard always Normal (relief), and milestone levels (every 25)
    /// never Super Hard. Every 100 consecutive levels then hold about 15–25 Hard and 6–10 Super Hard levels.
    /// Levels 1–10 are curated (L5 Hard, L10 Super Hard). A big level (<see cref="Profiles.BandGuidelines.IsBigLevel"/>,
    /// every milestone from L525) is never Hard either: on its board of up to 616 cells a premature pod finds tiles almost
    /// at once, so the tray tuner cannot make it Hard (2026-10-06, research R8b); a Hard due on it moves to the next level.
    /// A practice level (FR-031) is never Super Hard either (the owner, 2026-10-06): it uses one mechanic alone, and its
    /// tuned candidates top out far below the Super Hard minimum. With the roadmap given, a Super Hard the schedule puts
    /// on a practice level moves to the nearest later level that is not a showcase, a practice or a milestone level and
    /// keeps its relief, and the practice level takes that level's class (<see cref="MovePracticeSuperHards"/>).
    /// </summary>
    public sealed class DifficultySchedule
    {
        public const int FirstScheduledLevel = 11;

        private readonly ulong _seed;
        private readonly UnlockRoadmap? _roadmap;
        private DifficultyClass[] _classes = new DifficultyClass[0];

        public DifficultySchedule(ulong seed)
            : this(seed, null)
        {
        }

        /// <param name="seed">The schedule's seed.</param>
        /// <param name="roadmap">
        /// The unlock roadmap of the catalog, whose practice levels are never Super Hard; null for a schedule without
        /// showcases (the Daily Challenge pool, whose numbers are pool indexes).
        /// </param>
        public DifficultySchedule(ulong seed, UnlockRoadmap? roadmap)
        {
            _seed = seed;
            _roadmap = roadmap;
        }

        /// <summary>The practice levels (FR-031) under this schedule, from L11; empty without a roadmap.</summary>
        public IReadOnlyCollection<int> PracticeLevels()
        {
            if (_roadmap == null)
            {
                return new int[0];
            }

            ClassFor(FirstScheduledLevel);
            return Practices(_roadmap, l => ClassAt(_classes, l));
        }

        public DifficultyClass ClassFor(int level)
        {
            if (level < FirstScheduledLevel)
            {
                return level == 5 ? DifficultyClass.Hard : level == 10 ? DifficultyClass.SuperHard : DifficultyClass.Normal;
            }

            if (level >= FirstScheduledLevel + _classes.Length)
            {
                Extend(level);
            }

            return _classes[level - FirstScheduledLevel];
        }

        private void Extend(int upTo)
        {
            int count = upTo - FirstScheduledLevel + 1;
            count = System.Math.Max(count, System.Math.Max(512, _classes.Length * 2));
            var classes = new DifficultyClass[count];
            var rng = new Xoshiro256StarStar(_seed ^ 0xD1FF1C017EUL);
            int nextHard = FirstScheduledLevel + 2 + rng.NextInt(3);
            int nextSuper = FirstScheduledLevel + 9 + rng.NextInt(6);
            bool relief = false;
            for (int i = 0; i < count; i++)
            {
                int level = FirstScheduledLevel + i;
                DifficultyClass c = DifficultyClass.Normal;
                if (relief)
                {
                    relief = false;
                    if (nextHard <= level)
                    {
                        nextHard = level + 1;
                    }

                    if (nextSuper <= level)
                    {
                        nextSuper = level + 1;
                    }
                }
                else if (level >= nextSuper && level % 25 != 0)
                {
                    c = DifficultyClass.SuperHard;
                    relief = true;
                    nextSuper = level + 10 + rng.NextInt(6);
                    if (nextHard <= level + 1)
                    {
                        nextHard = level + 2;
                    }
                }
                else if (level >= nextHard && !Profiles.BandGuidelines.IsBigLevel(level))
                {
                    c = DifficultyClass.Hard;
                    nextHard = level + 4 + rng.NextInt(3);
                }

                classes[i] = c;
            }

            if (_roadmap != null)
            {
                MovePracticeSuperHards(classes, _roadmap);
            }

            _classes = classes;
        }

        /// <summary>
        /// The owner's rule of 2026-10-06: no practice level is Super Hard. Each Super Hard on a practice level, earliest
        /// first, swaps classes with the nearest later level that is not a showcase, a practice or a milestone level (FR-059:
        /// milestones are never Super Hard) and whose next level is Normal (the relief), and that does not follow another
        /// Super Hard. Both levels are a few apart, so the counts per 100 levels keep their ranges, and the rest of the schedule
        /// is untouched. The Connected Triple's practice is the first Hard or Super Hard level after its showcase, so a swap
        /// can move it; the pass repeats until no practice level is Super Hard.
        /// </summary>
        private static void MovePracticeSuperHards(DifficultyClass[] classes, UnlockRoadmap roadmap)
        {
            var showcases = new HashSet<int>(MechanicNames.All.Select(m => roadmap.LevelOf(MechanicNames.UnlockId(m))).Where(l => l != null).Select(l => l!.Value));
            for (int pass = 0; pass < 64; pass++)
            {
                HashSet<int> practices = Practices(roadmap, l => ClassAt(classes, l));
                int practice = practices.Where(l => ClassAt(classes, l) == DifficultyClass.SuperHard).DefaultIfEmpty(0).Min();
                if (practice == 0)
                {
                    return;
                }

                int target = 0;
                for (int t = practice + 1; t + 1 < FirstScheduledLevel + classes.Length; t++)
                {
                    bool eligible = !showcases.Contains(t)
                        && !practices.Contains(t)
                        && t % 25 != 0
                        && !Profiles.BandGuidelines.IsBigLevel(t)
                        && ClassAt(classes, t) != DifficultyClass.SuperHard
                        && ClassAt(classes, t + 1) == DifficultyClass.Normal
                        && (t - 1 == practice || ClassAt(classes, t - 1) != DifficultyClass.SuperHard);
                    if (eligible)
                    {
                        target = t;
                        break;
                    }
                }

                if (target == 0)
                {
                    return;
                }

                classes[practice - FirstScheduledLevel] = classes[target - FirstScheduledLevel];
                classes[target - FirstScheduledLevel] = DifficultyClass.SuperHard;
            }
        }

        private static HashSet<int> Practices(UnlockRoadmap roadmap, System.Func<int, DifficultyClass> classOf) =>
            new HashSet<int>(MechanicNames.All
                .Select(m => MechanicNames.PracticeLevel(m, roadmap, classOf))
                .Where(l => l != null && l.Value >= FirstScheduledLevel)
                .Select(l => l!.Value));

        private static DifficultyClass ClassAt(DifficultyClass[] classes, int level)
        {
            if (level < FirstScheduledLevel)
            {
                return level == 5 ? DifficultyClass.Hard : level == 10 ? DifficultyClass.SuperHard : DifficultyClass.Normal;
            }

            int i = level - FirstScheduledLevel;
            return i < classes.Length ? classes[i] : DifficultyClass.Normal;
        }
    }
}
