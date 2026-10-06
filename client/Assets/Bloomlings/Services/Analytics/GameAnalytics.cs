using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Client.Services.Analytics
{
    /// <summary>The level parameters of an event (contracts/analytics-events.md).</summary>
    public sealed record LevelInfo(int LevelNumber, int DefinitionVersion, DifficultyClass Difficulty, string PictureId)
    {
        public static LevelInfo Of(LevelDefinition definition, int levelNumber) =>
            new LevelInfo(levelNumber, definition.DefinitionVersion, definition.Difficulty.Class, definition.Picture.Id);

        public string DifficultyName => Difficulty switch
        {
            DifficultyClass.Hard => "hard",
            DifficultyClass.SuperHard => "super_hard",
            _ => "normal",
        };
    }

    /// <summary>
    /// The game's analytics and crash-report facade (FR-086, research R14, T147). It adds the common parameters
    /// (<c>app_version</c>, <c>content_version</c>, <c>player_level</c>, <c>session_id</c>) and, where a level is
    /// involved, the level parameters, then logs the event. Until consent is known (FR-090) nothing leaves the device:
    /// events wait in a bounded queue and are sent once <see cref="Attach"/> connects the backends, or dropped by
    /// <see cref="Disable"/> when consent is refused. The crash custom keys follow the level being played. Engine-free.
    /// </summary>
    public sealed class GameAnalytics
    {
        public const int MaxQueued = 200;

        private readonly Queue<(string Name, Dictionary<string, object> Parameters)> _queue = new Queue<(string, Dictionary<string, object>)>();
        private readonly string _appVersion;
        private readonly Func<int> _contentVersion;
        private readonly Func<int> _playerLevel;
        private readonly Dictionary<string, string> _crashKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        private IAnalyticsService? _analytics;
        private ICrashReporter? _crashes;
        private bool _disabled;

        public GameAnalytics(string appVersion, Func<int> contentVersion, Func<int> playerLevel, string? sessionId = null)
        {
            _appVersion = appVersion;
            _contentVersion = contentVersion;
            _playerLevel = playerLevel;
            SessionId = sessionId ?? Guid.NewGuid().ToString("N");
            _crashKeys[CrashKeys.AppVersion] = appVersion;
        }

        public string SessionId { get; }

        public int QueuedCount => _queue.Count;

        public bool IsAttached => _analytics != null;

        /// <summary>Consent allows analytics: starts the backends and sends what was queued.</summary>
        public void Attach(IAnalyticsService analytics, ICrashReporter crashes, bool personalized)
        {
            if (_disabled || _analytics != null)
            {
                return;
            }

            _analytics = analytics;
            _crashes = crashes;
            analytics.Initialize(personalized);
            crashes.Initialize();
            _crashKeys[CrashKeys.ContentVersion] = Str(_contentVersion());
            foreach (KeyValuePair<string, string> key in _crashKeys)
            {
                crashes.SetCustomKey(key.Key, key.Value);
            }

            while (_queue.Count > 0)
            {
                (string name, Dictionary<string, object> parameters) = _queue.Dequeue();
                analytics.Log(name, parameters);
            }
        }

        /// <summary>Consent refused or withdrawn: nothing more is collected, and a started backend stops.</summary>
        public void Disable()
        {
            _disabled = true;
            _queue.Clear();
            _analytics?.Stop();
        }

        /// <summary>
        /// The player changed consent in the privacy options (FR-090). Withdrawn: collection stops. Given: the backends
        /// start (created only then), or a started one takes the new personalization.
        /// </summary>
        public void ConsentChanged(bool allowed, bool personalized, Func<IAnalyticsService> analytics, Func<ICrashReporter> crashes)
        {
            if (!allowed)
            {
                Disable();
                return;
            }

            _disabled = false;
            if (_analytics != null)
            {
                _analytics.Initialize(personalized);
                return;
            }

            Attach(analytics(), crashes(), personalized);
        }

        /// <summary>Sets the crash keys for the level about to be played (R14).</summary>
        public void SetLevelContext(LevelInfo? level)
        {
            SetCrashKey(CrashKeys.ContentVersion, Str(_contentVersion()));
            SetCrashKey(CrashKeys.LevelNumber, level == null ? string.Empty : Str(level.LevelNumber));
            SetCrashKey(CrashKeys.DefinitionVersion, level == null ? string.Empty : Str(level.DefinitionVersion));
            SetCrashKey(CrashKeys.PictureId, level?.PictureId ?? string.Empty);
        }

        /// <summary>A non-fatal error with the current custom keys.</summary>
        public void RecordException(Exception exception) => _crashes?.RecordException(exception);

        // ---- Gameplay ----

        public void LevelStart(LevelInfo level, int attemptIndex) =>
            Log(AnalyticsEvents.LevelStart, level, ("attempt_index", attemptIndex));

        public void LevelWin(LevelInfo level, long durationMs, int taps, int boostersUsed, bool cleanClear, int peakSlots, int attemptIndex) =>
            Log(AnalyticsEvents.LevelWin, level, ("duration_ms", durationMs), ("taps", taps), ("boosters_used", boostersUsed), ("clean_clear", cleanClear), ("peak_slots", peakSlots), ("attempt_index", attemptIndex));

        /// <param name="kind"><c>jam</c> or <c>stuck</c>.</param>
        public void LevelJam(LevelInfo level, string kind, long durationMs, int taps, int slotsUsed, int remainingWork) =>
            Log(AnalyticsEvents.LevelJam, level, ("kind", kind), ("duration_ms", durationMs), ("taps", taps), ("slots_used", slotsUsed), ("remaining_work", remainingWork));

        /// <param name="method"><c>extra_slot</c>, <c>return</c>, <c>bloom_burst</c> or <c>ad_rescue</c>.</param>
        public void LevelRecover(LevelInfo level, string method) => Log(AnalyticsEvents.LevelRecover, level, ("method", method));

        /// <param name="from"><c>pause</c> or <c>jam</c>.</param>
        public void LevelRestart(LevelInfo level, string from) => Log(AnalyticsEvents.LevelRestart, level, ("from", from));

        public void LevelQuit(LevelInfo level, long durationMs) => Log(AnalyticsEvents.LevelQuit, level, ("duration_ms", durationMs));

        /// <param name="source"><c>charge</c>, <c>petals</c>, <c>ad</c> or <c>demo</c> (the free guided use at its unlock, spec 005 FR-035).</param>
        public void BoosterUse(LevelInfo level, string booster, string source) =>
            Log(AnalyticsEvents.BoosterUse, level, ("booster", booster), ("source", source));

        public void TutorialStep(LevelInfo? level, string unlockId, int step, bool completed) =>
            Log(AnalyticsEvents.TutorialStep, level, ("unlock_id", unlockId), ("step", step), ("completed", completed));

        // ---- Progression and meta ----

        public void Unlock(string unlockId, string kind) => Log(AnalyticsEvents.Unlock, null, ("unlock_id", unlockId), ("kind", kind));

        public void MilestoneClaim(int milestoneLevel, string bundleId) =>
            Log(AnalyticsEvents.MilestoneClaim, null, ("milestone_level", milestoneLevel), ("bundle_id", bundleId));

        public void DailyRewardClaim(int streak) => Log(AnalyticsEvents.DailyRewardClaim, null, ("streak", streak));

        public void DailyChallengeComplete(string utcDate) => Log(AnalyticsEvents.DailyChallengeComplete, null, ("utc_date", utcDate));

        /// <param name="rank">The player's rank, or 0 when unknown.</param>
        public void LeaderboardView(int rank) => Log(AnalyticsEvents.LeaderboardView, null, ("rank", rank));

        public void CosmeticEquip(string family, string skinId) => Log(AnalyticsEvents.CosmeticEquip, null, ("family", family), ("skin_id", skinId));

        public void CollectionOpen(int entries) => Log(AnalyticsEvents.CollectionOpen, null, ("entries", entries));

        // ---- Monetization ----

        public void StoreOpen(string from) => Log(AnalyticsEvents.StoreOpen, null, ("from", from));

        public void Purchase(string productId, long priceMicros, string currency, string transactionId) =>
            Log(AnalyticsEvents.Purchase, null, ("product_id", productId), ("price_micros", priceMicros), ("currency", currency), ("transaction_id", transactionId));

        public void AdRewarded(string placement) => Log(AnalyticsEvents.AdRewarded, null, ("placement", placement));

        public void AdInterstitial(int levelsSinceLast, long secondsSinceLast) =>
            Log(AnalyticsEvents.AdInterstitial, null, ("levels_since_last", levelsSinceLast), ("seconds_since_last", secondsSinceLast));

        public void Consent(string gdpr, string att) => Log(AnalyticsEvents.Consent, null, ("gdpr", gdpr), ("att", att));

        // ---- Content quality ----

        public void ContentUpdate(int fromVersion, int toVersion) =>
            Log(AnalyticsEvents.ContentUpdate, null, ("from_version", fromVersion), ("to_version", toVersion));

        public void ContentError(string packId, int levelNumber, string error) =>
            Log(AnalyticsEvents.ContentError, null, ("pack_id", packId), ("level_number", levelNumber), ("error", error));

        private void Log(string name, LevelInfo? level, params (string Key, object Value)[] extra)
        {
            if (_disabled)
            {
                return;
            }

            var parameters = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["app_version"] = _appVersion,
                ["content_version"] = (long)_contentVersion(),
                ["player_level"] = (long)_playerLevel(),
                ["session_id"] = SessionId,
            };
            if (level != null && AnalyticsEvents.WithLevel.Contains(name))
            {
                parameters["level_number"] = (long)level.LevelNumber;
                parameters["definition_version"] = (long)level.DefinitionVersion;
                parameters["difficulty_class"] = level.DifficultyName;
                parameters["picture_id"] = level.PictureId;
            }

            foreach ((string key, object value) in extra)
            {
                parameters[key] = value switch
                {
                    int i => (long)i,
                    bool b => b ? 1L : 0L,
                    _ => value,
                };
            }

            if (_analytics != null)
            {
                _analytics.Log(name, parameters);
                return;
            }

            if (_queue.Count >= MaxQueued)
            {
                _queue.Dequeue();
            }

            _queue.Enqueue((name, parameters));
        }

        private void SetCrashKey(string key, string value)
        {
            _crashKeys[key] = value;
            _crashes?.SetCustomKey(key, value);
        }

        private static string Str(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
