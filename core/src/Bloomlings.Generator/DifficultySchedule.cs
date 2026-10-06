using Bloomlings.Core.Definitions;
using Bloomlings.Core.Random;

namespace Bloomlings.Generator
{
    /// <summary>
    /// The target class of each level from L11 on (FR-059): Hard every 4–6 levels and Super Hard every 10–15, spaced
    /// irregularly by a seeded PRNG, the level after a Super Hard always Normal (relief), and milestone levels (every 25)
    /// never Super Hard. Every 100 consecutive levels then hold about 15–25 Hard and 6–10 Super Hard levels.
    /// Levels 1–10 are curated (L5 Hard, L10 Super Hard). A big level (<see cref="Profiles.BandGuidelines.IsBigLevel"/>,
    /// every milestone from L525) is never Hard either: on its board of up to 616 cells a premature pod finds tiles almost
    /// at once, so the tray tuner cannot make it Hard (2026-10-06, research R8b); a Hard due on it moves to the next level.
    /// </summary>
    public sealed class DifficultySchedule
    {
        public const int FirstScheduledLevel = 11;

        private readonly ulong _seed;
        private DifficultyClass[] _classes = new DifficultyClass[0];

        public DifficultySchedule(ulong seed)
        {
            _seed = seed;
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

            _classes = classes;
        }
    }
}
