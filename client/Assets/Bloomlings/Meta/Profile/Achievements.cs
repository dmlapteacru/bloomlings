using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Meta.Profile
{
    /// <summary>What an achievement counts.</summary>
    public enum AchievementKind
    {
        /// <summary>Levels won (the save's <c>levelsWon</c> counter).</summary>
        LevelsWon,

        /// <summary>Pictures in the Collection.</summary>
        Pictures,

        /// <summary>Daily Challenges won (the save's <c>dailyChallengesWon</c> counter).</summary>
        DailyChallenges,
    }

    /// <summary>An achievement: what it counts, its name's key and the counts of its bronze, silver and gold tiers.</summary>
    public sealed record AchievementDef(AchievementKind Kind, string NameKey, IReadOnlyList<long> Tiers);

    /// <summary>
    /// An achievement as the profile shows it: the player's count, the tier reached (0 for none, 1 bronze, 2 silver,
    /// 3 gold) and the count of the next tier (the gold one once it is reached).
    /// </summary>
    public readonly struct AchievementState
    {
        public AchievementState(AchievementDef def, long value)
        {
            Def = def;
            Value = Math.Max(0, value);
            int tier = 0;
            while (tier < def.Tiers.Count && Value >= def.Tiers[tier])
            {
                tier++;
            }

            Tier = tier;
            Goal = def.Tiers[Math.Min(tier, def.Tiers.Count - 1)];
        }

        public AchievementDef Def { get; }

        public long Value { get; }

        public int Tier { get; }

        public long Goal { get; }

        /// <summary>Whether the gold tier is reached.</summary>
        public bool Complete => Tier >= Def.Tiers.Count;

        /// <summary>The count shown toward the next tier, at most its goal.</summary>
        public long Shown => Math.Min(Value, Goal);
    }

    /// <summary>
    /// The profile's achievements (spec 005 FR-037 as amended on 2026-10-06; the owner left their names to us): three
    /// achievements from what the save already counts, each with a bronze, a silver and a gold tier. Green Thumb wins
    /// levels (50, 500, 2500), Picture Keeper collects pictures (10, 100, 1000) and Daily Gardener wins Daily Challenges
    /// (7, 30, 100). They are shown only: no reward, nothing in a level. The look's colors are the kit's
    /// (<c>AchievementLook</c>). Engine-free.
    /// </summary>
    public static class Achievements
    {
        /// <summary>The save counter of Daily Challenges won.</summary>
        public const string DailyCounter = "dailyChallengesWon";

        /// <summary>The save counter of levels won.</summary>
        public const string LevelsCounter = "levelsWon";

        public static IReadOnlyList<AchievementDef> All { get; } = new[]
        {
            new AchievementDef(AchievementKind.LevelsWon, "profile.achievement.levels", new long[] { 50, 500, 2500 }),
            new AchievementDef(AchievementKind.Pictures, "profile.achievement.pictures", new long[] { 10, 100, 1000 }),
            new AchievementDef(AchievementKind.DailyChallenges, "profile.achievement.daily", new long[] { 7, 30, 100 }),
        };

        /// <summary>The achievements' states for the player's counts, in <see cref="All"/>'s order.</summary>
        public static IReadOnlyList<AchievementState> Of(long levelsWon, long pictures, long dailyWon)
        {
            var states = new List<AchievementState>(All.Count);
            foreach (AchievementDef def in All)
            {
                long value = def.Kind switch
                {
                    AchievementKind.LevelsWon => levelsWon,
                    AchievementKind.Pictures => pictures,
                    _ => dailyWon,
                };
                states.Add(new AchievementState(def, value));
            }

            return states;
        }

        /// <summary>The save's count of <paramref name="counter"/> (0 when it has none).</summary>
        public static long Count(PlayerSave save, string counter) => save.Stats.Counters.TryGetValue(counter, out long value) ? value : 0;
    }
}
