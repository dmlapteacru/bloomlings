using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Meta.Profile
{
    public enum AchievementKind { LevelsWon, Pictures, DailyChallenges, WinFact, Prestige, Theme }

    /// <summary>A stable achievement line. Its id also names its badge image and profile cosmetic.</summary>
    public sealed record AchievementDef(string Id, AchievementKind Kind, string NameKey, IReadOnlyList<long> Tiers)
    {
        public string BadgeId => "badge.achievement." + Id;
    }

    public readonly struct AchievementState
    {
        public AchievementState(AchievementDef def, long value)
        {
            Def = def;
            Value = Math.Max(0, value);
            int tier = 0;
            while (tier < def.Tiers.Count && Value >= def.Tiers[tier]) tier++;
            Tier = tier;
            Goal = def.Tiers[Math.Min(tier, def.Tiers.Count - 1)];
        }

        public AchievementDef Def { get; }
        public long Value { get; }
        public int Tier { get; }
        public long Goal { get; }
        public bool Complete => Tier >= Def.Tiers.Count;
        public long Shown => Math.Min(Value, Goal);
    }

    /// <summary>
    /// The game's badge catalog. Level facts are stored per level, so replaying a win and merging offline saves cannot
    /// double-count it. Old saves can reconstruct first win, collection, daily and prestige; mastery begins with this build.
    /// </summary>
    public static class Achievements
    {
        public const string DailyCounter = "dailyChallengesWon";
        public const string LevelsCounter = "levelsWon";
        public const string FactPrefix = "achievement.";
        public const string ThemePrefix = "achievement.theme.";

        private static AchievementDef Def(string id, AchievementKind kind, params long[] tiers) =>
            new AchievementDef(id, kind, "profile.achievement." + id, tiers);

        public static IReadOnlyList<AchievementDef> All { get; } = new[]
        {
            Def("first_bloom", AchievementKind.LevelsWon, 1),
            Def("natural_talent", AchievementKind.WinFact, 1, 10, 50, 250),
            Def("breathing_room", AchievementKind.WinFact, 1, 10, 50),
            Def("tough_bloom", AchievementKind.WinFact, 1, 10, 50),
            Def("lotus_mind", AchievementKind.WinFact, 1, 5, 25),
            Def("chain_reaction", AchievementKind.WinFact, 1, 10, 50),
            Def("key_keeper", AchievementKind.WinFact, 5, 25, 100),
            Def("deep_roots", AchievementKind.WinFact, 1, 10, 50),
            Def("better_together", AchievementKind.WinFact, 1, 10, 50),
            Def("spring_keeper", AchievementKind.WinFact, 1, 10, 50),
            Def("picture_explorer", AchievementKind.Pictures, 5, 25, 100),
            Def("daily_bloom", AchievementKind.DailyChallenges, 1, 7, 30, 100),
            Def("young_gardener", AchievementKind.Prestige, 25),
            Def("seasoned_gardener", AchievementKind.Prestige, 100),
            Def("garden_master", AchievementKind.Prestige, 250),
            Def("grove_keeper", AchievementKind.Prestige, 500),
            Def("golden_gardener", AchievementKind.Prestige, 1000),
            Def("bloom_legend", AchievementKind.Prestige, 2500),
            Def("heart_of_bloomlings", AchievementKind.Prestige, 5000),
            Def("forest_friend", AchievementKind.Theme, 10),
            Def("sweet_life", AchievementKind.Theme, 10),
            Def("sea_stories", AchievementKind.Theme, 10),
            Def("space_garden", AchievementKind.Theme, 5),
        };

        private static readonly IReadOnlyDictionary<string, string> ThemeOf = new Dictionary<string, string>
        {
            ["forest_friend"] = "forest", ["sweet_life"] = "sweets",
            ["sea_stories"] = "sea", ["space_garden"] = "space",
        };

        public static AchievementDef? ById(string id)
        {
            foreach (AchievementDef def in All) if (def.Id == id) return def;
            return null;
        }

        public static IReadOnlyList<AchievementState> Of(PlayerSave save)
        {
            var pictures = new HashSet<string>(StringComparer.Ordinal);
            foreach (CollectionEntry entry in save.Collection) pictures.Add(entry.PictureId);
            var states = new List<AchievementState>(All.Count);
            foreach (AchievementDef def in All)
            {
                long value = def.Kind switch
                {
                    AchievementKind.LevelsWon => Math.Min(1, save.Progression.HighestCompletedLevel),
                    AchievementKind.Pictures => pictures.Count,
                    AchievementKind.DailyChallenges => Count(save, DailyCounter),
                    AchievementKind.Prestige => Math.Min(save.Progression.HighestCompletedLevel, def.Tiers[0]),
                    AchievementKind.Theme => SumGroup(save, ThemePrefix + ThemeOf[def.Id]),
                    _ => SumGroup(save, FactPrefix + def.Id),
                };
                states.Add(new AchievementState(def, value));
            }
            return states;
        }

        public static bool Unlocked(PlayerSave save, string id)
        {
            foreach (AchievementState state in Of(save))
                if (state.Def.Id == id) return state.Tier > 0;
            return false;
        }

        public static long Count(PlayerSave save, string counter) =>
            save.Stats.Counters.TryGetValue(counter, out long value) ? value : 0;

        private static long SumGroup(PlayerSave save, string group)
        {
            if (!save.Stats.Groups.TryGetValue(group, out SortedDictionary<string, long>? facts)) return 0;
            long sum = 0;
            foreach (long value in facts.Values) sum += value;
            return sum;
        }
    }
}
