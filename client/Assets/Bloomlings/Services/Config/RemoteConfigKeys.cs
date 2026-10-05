using System;
using System.Collections.Generic;

namespace Bloomlings.Client.Services.Config
{
    /// <summary>An integer Remote Config key with its bundled default and allowed range (contracts/backend-services.md).</summary>
    public sealed class IntKey
    {
        public IntKey(string name, int defaultValue, int min, int max)
        {
            if (defaultValue < min || defaultValue > max)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultValue), $"{name}: default {defaultValue} is outside {min}..{max}.");
            }

            Name = name;
            Default = defaultValue;
            Min = min;
            Max = max;
        }

        public string Name { get; }

        public int Default { get; }

        public int Min { get; }

        public int Max { get; }

        public int Clamp(int value) => value < Min ? Min : value > Max ? Max : value;
    }

    public sealed class BoolKey
    {
        public BoolKey(string name, bool defaultValue)
        {
            Name = name;
            Default = defaultValue;
        }

        public string Name { get; }

        public bool Default { get; }
    }

    /// <summary>A string key. <see cref="Validate"/> decides whether a remote value is usable; otherwise the default wins.</summary>
    public sealed class StringKey
    {
        public StringKey(string name, string defaultValue, Func<string, bool> validate)
        {
            Name = name;
            Default = defaultValue;
            Validate = validate;
        }

        public string Name { get; }

        public string Default { get; }

        public Func<string, bool> Validate { get; }
    }

    /// <summary>
    /// Every Remote Config key the client reads, with the bundled defaults and ranges from
    /// contracts/backend-services.md. Core puzzle rules and level definitions are never remotely configurable (FR-085).
    /// </summary>
    public static class RemoteConfigKeys
    {
        // Economy (FR-041, FR-042, FR-047, FR-048).
        public static readonly IntKey PetalsBase = new IntKey("economy.petals.base", 12, 5, 50);
        public static readonly IntKey PetalsCleanBonus = new IntKey("economy.petals.cleanBonus", 8, 0, 50);
        public static readonly IntKey PetalsHardBonus = new IntKey("economy.petals.hardBonus", 10, 0, 100);
        public static readonly IntKey PetalsSuperHardBonus = new IntKey("economy.petals.superHardBonus", 20, 0, 100);
        public static readonly IntKey PriceExtraSlot = new IntKey("economy.price.extraSlot", 40, 10, 500);
        public static readonly IntKey PriceShuffle = new IntKey("economy.price.shuffle", 40, 10, 500);
        public static readonly IntKey PriceReturn = new IntKey("economy.price.return", 50, 10, 500);
        /// <summary>Up to 1000, so it can always stay above the other prices (at most 500) as FR-048 asks.</summary>
        public static readonly IntKey PriceBloomBurst = new IntKey("economy.price.bloomBurst", 60, 10, 1000);
        public static readonly IntKey UnlockGrant = new IntKey("economy.unlockGrant", 1, 1, 3);
        public static readonly IntKey DropEveryLevels = new IntKey("economy.drop.everyLevels", 5, 2, 20);

        // Daily reward (FR-055).
        public static readonly IntKey DailyRewardPetals = new IntKey("daily.reward.petals", 20, 5, 200);
        public static readonly IntKey DailyStreakBonusPetals = new IntKey("daily.reward.streakBonusPetals", 5, 0, 50);
        public static readonly IntKey DailyStreakMaxDays = new IntKey("daily.reward.streakMaxDays", 7, 1, 30);

        // Ads (FR-027, FR-048, FR-053, SC-013).
        public static readonly IntKey InterstitialFirstLevel = new IntKey("ads.interstitial.firstLevel", 11, 11, 100);
        public static readonly IntKey InterstitialMinSeconds = new IntKey("ads.interstitial.minSeconds", 180, 60, 1800);
        public static readonly IntKey InterstitialMinLevels = new IntKey("ads.interstitial.minLevels", 3, 1, 10);
        public static readonly IntKey RescuePerAttempt = new IntKey("ads.rescue.perAttempt", 1, 0, 1);

        // Feature flags (FR-062 to FR-064).
        public static readonly BoolKey DailyChallengeEnabled = new BoolKey("feature.dailyChallenge", true);
        public static readonly BoolKey WardrobeEnabled = new BoolKey("feature.wardrobe", true);
        public static readonly BoolKey LeaderboardEnabled = new BoolKey("feature.leaderboard", true);

        // Content updates (FR-078). Empty skips the update check; anything else must be an https URL.
        public static readonly StringKey ContentManifestUrl = new StringKey(
            "content.manifestUrl",
            string.Empty,
            value => value.Length == 0 || (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps));

        // Presentation (research R4). The backlog beyond which the timeline plays faster: 6 s since the waves of
        // different taps play side by side at the halved clearing pace (the owner's report of 2026-10-03; it was 1.5 s).
        public static readonly IntKey FxBacklogThresholdMs = new IntKey("fx.backlogThresholdMs", 12000, 2000, 20000);

        public static IReadOnlyList<IntKey> AllInts { get; } = new[]
        {
            PetalsBase, PetalsCleanBonus, PetalsHardBonus, PetalsSuperHardBonus,
            PriceExtraSlot, PriceShuffle, PriceReturn, PriceBloomBurst, UnlockGrant, DropEveryLevels,
            DailyRewardPetals, DailyStreakBonusPetals, DailyStreakMaxDays,
            InterstitialFirstLevel, InterstitialMinSeconds, InterstitialMinLevels, RescuePerAttempt,
            FxBacklogThresholdMs,
        };

        public static IReadOnlyList<BoolKey> AllBools { get; } = new[] { DailyChallengeEnabled, WardrobeEnabled, LeaderboardEnabled };

        public static IReadOnlyList<StringKey> AllStrings { get; } = new[] { ContentManifestUrl };
    }
}
