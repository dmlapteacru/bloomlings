using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloomlings.Client.Services.Analytics;
using Bloomlings.Client.Services.Content;
using Bloomlings.Core.Definitions;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The analytics catalog mirrors contracts/analytics-events.md, and nothing is sent before consent (T147).</summary>
    public class AnalyticsContractTests
    {
        private sealed class Recorder : IAnalyticsService, ICrashReporter
        {
            public readonly List<(string Name, IReadOnlyDictionary<string, object> Parameters)> Events = new List<(string, IReadOnlyDictionary<string, object>)>();
            public readonly Dictionary<string, string> Keys = new Dictionary<string, string>();

            public bool Initialized { get; private set; }

            public bool Stopped { get; private set; }

            public bool Personalized { get; private set; }

            public void Initialize(bool personalized)
            {
                Initialized = true;
                Stopped = false;
                Personalized = personalized;
            }

            public void Stop() => Stopped = true;

            public void Initialize() => Initialized = true;

            public void Log(string eventName, IReadOnlyDictionary<string, object> parameters) => Events.Add((eventName, parameters));

            public void SetCustomKey(string key, string value) => Keys[key] = value;

            public void RecordException(Exception exception)
            {
            }
        }

        private static readonly LevelInfo Level = new LevelInfo(42, 1, DifficultyClass.SuperHard, "fish_01");

        private static string ContractPath => Path.Combine(DevContent.RepositoryContentFolder, "..", "specs", "001-core-game-mvp", "contracts", "analytics-events.md");

        [Test]
        public void Catalog_MatchesTheContract()
        {
            var contract = new Dictionary<string, string[]>();
            var row = new Regex(@"^\| `(?<name>[a-z_]+)` \| [^|]+ \| (?<extra>.*) \|$");
            foreach (string line in File.ReadAllLines(ContractPath))
            {
                Match match = row.Match(line.Trim());
                if (match.Success)
                {
                    contract[match.Groups["name"].Value] = Regex.Matches(match.Groups["extra"].Value, @"`([a-z_]+)`(?!/)(?![^(]*\))")
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .ToArray();
                }
            }

            Assert.That(contract.Keys, Is.EquivalentTo(AnalyticsEvents.Extra.Keys));
            foreach (KeyValuePair<string, string[]> pair in contract)
            {
                Assert.That(AnalyticsEvents.Extra[pair.Key], Is.EqualTo(pair.Value), pair.Key);
            }
        }

        [Test]
        public void EveryEvent_CarriesTheCommonLevelAndExtraParameters()
        {
            var recorder = new Recorder();
            var analytics = new GameAnalytics("1.2.3", () => 7, () => 43, "session");
            analytics.Attach(recorder, recorder, personalized: false);

            analytics.LevelStart(Level, 1);
            analytics.LevelWin(Level, 1000, 12, 0, true, 3, 1);
            analytics.LevelJam(Level, "jam", 900, 10, 5, 7);
            analytics.LevelRecover(Level, "extra_slot");
            analytics.LevelRestart(Level, "pause");
            analytics.LevelQuit(Level, 500);
            analytics.BoosterUse(Level, "shuffle", "charge");
            analytics.TutorialStep(Level, "mechanic.key", 1, true);
            analytics.Unlock("mechanic.key", "mechanic");
            analytics.MilestoneClaim(100, "major");
            analytics.DailyRewardClaim(3);
            analytics.DailyChallengeComplete("2026-09-29");
            analytics.LeaderboardView(1234);
            analytics.CosmeticEquip("drop", "hat.leaf_cap");
            analytics.CollectionOpen(12);
            analytics.StoreOpen("home");
            analytics.Purchase("petals_s", 990000, "USD", "tx1");
            analytics.AdRewarded("rescue");
            analytics.AdInterstitial(3, 200);
            analytics.Consent("granted", "not_required");
            analytics.ContentUpdate(1, 2);
            analytics.ContentError("levels_0001_0250", 12, "hash");

            Assert.That(recorder.Events.Select(e => e.Name), Is.EquivalentTo(AnalyticsEvents.Extra.Keys), "one method per contract event");
            foreach ((string name, IReadOnlyDictionary<string, object> parameters) in recorder.Events)
            {
                IEnumerable<string> expected = AnalyticsEvents.Common
                    .Concat(AnalyticsEvents.WithLevel.Contains(name) ? AnalyticsEvents.Level : Array.Empty<string>())
                    .Concat(AnalyticsEvents.Extra[name]);
                Assert.That(parameters.Keys, Is.EquivalentTo(expected.Distinct()), name);
                Assert.That(parameters.Values.All(v => v is string || v is long || v is double), Is.True, name + ": strings, longs or doubles only");
            }

            IReadOnlyDictionary<string, object> win = recorder.Events.Single(e => e.Name == AnalyticsEvents.LevelWin).Parameters;
            Assert.That((win["app_version"], win["content_version"], win["player_level"], win["session_id"]), Is.EqualTo(("1.2.3", 7L, 43L, "session")));
            Assert.That((win["level_number"], win["difficulty_class"], win["picture_id"], win["clean_clear"]), Is.EqualTo((42L, "super_hard", "fish_01", 1L)));
        }

        [Test]
        public void NothingIsSent_BeforeConsent_AndARefusalDropsTheQueue()
        {
            var analytics = new GameAnalytics("1.0.0", () => 1, () => 1);
            analytics.Unlock("system.core", "system");
            analytics.LevelStart(Level, 1);
            Assert.That(analytics.QueuedCount, Is.EqualTo(2));

            var recorder = new Recorder();
            analytics.SetLevelContext(Level);
            analytics.Attach(recorder, recorder, personalized: true);
            Assert.That(recorder.Initialized, Is.True);
            Assert.That(recorder.Events.Select(e => e.Name), Is.EqualTo(new[] { "unlock", "level_start" }), "queued events are sent in order after consent");
            Assert.That(recorder.Keys.Keys, Is.EquivalentTo(CrashKeys.All), "every crash report carries the R14 keys");
            Assert.That(recorder.Keys[CrashKeys.LevelNumber], Is.EqualTo("42"));

            var refused = new GameAnalytics("1.0.0", () => 1, () => 1);
            refused.Unlock("system.core", "system");
            refused.Disable();
            var never = new Recorder();
            refused.Attach(never, never, personalized: false);
            Assert.That(never.Events, Is.Empty);
            Assert.That(never.Initialized, Is.False, "refused consent never initializes the SDKs");
        }

        [Test]
        public void ConsentChangedInThePrivacyOptions_StopsAndRestartsCollection()
        {
            var analytics = new GameAnalytics("1.0.0", () => 1, () => 1);
            var recorder = new Recorder();
            analytics.Attach(recorder, recorder, personalized: true);

            analytics.ConsentChanged(false, false, () => recorder, () => recorder);
            analytics.LevelStart(Level, 1);
            Assert.That(recorder.Stopped, Is.True);
            Assert.That(recorder.Events, Is.Empty, "nothing is sent after consent is withdrawn");

            analytics.ConsentChanged(true, false, () => new Recorder(), () => new Recorder());
            analytics.LevelStart(Level, 2);
            Assert.That(recorder.Stopped, Is.False);
            Assert.That(recorder.Personalized, Is.False, "the started backend takes the new personalization");
            Assert.That(recorder.Events.Select(e => e.Name), Is.EqualTo(new[] { "level_start" }));

            var refused = new GameAnalytics("1.0.0", () => 1, () => 1);
            refused.Disable();
            var later = new Recorder();
            refused.ConsentChanged(true, true, () => later, () => later);
            Assert.That(later.Initialized, Is.True, "consent given later in the session starts the backends");
        }
    }
}
