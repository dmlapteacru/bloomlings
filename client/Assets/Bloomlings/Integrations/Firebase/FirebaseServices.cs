#if BLOOMLINGS_FIREBASE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bloomlings.Client.Services;
using Bloomlings.Client.Services.Analytics;
using Firebase;
using Firebase.Analytics;
using UnityEngine;
#if BLOOMLINGS_FIREBASE_CRASHLYTICS
using Firebase.Crashlytics;
#endif

namespace Bloomlings.Integrations.Firebase
{
    /// <summary>
    /// Firebase (research R14, T147): Analytics, and Crashlytics when its package is installed. Registered before the
    /// first scene; nothing is collected until <see cref="GameAnalytics.Attach"/> runs after consent (FR-090). Automatic
    /// collection must also be off in the native configuration until then (see client/README.md).
    /// </summary>
    internal static class FirebaseBootstrap
    {
        private static Task<bool>? _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            ServiceProviders.Analytics = () => new FirebaseAnalyticsService();
#if BLOOMLINGS_FIREBASE_CRASHLYTICS
            ServiceProviders.Crashes = () => new CrashlyticsCrashReporter();
#endif
        }

        /// <summary>Checks the native dependencies once; true when Firebase can be used.</summary>
        internal static Task<bool> EnsureReadyAsync() => _ready ??= CheckAsync();

        private static async Task<bool> CheckAsync()
        {
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                Debug.LogWarning("[Firebase] Unavailable: " + status);
                return false;
            }

            return true;
        }
    }

    /// <summary>Firebase Analytics behind <see cref="IAnalyticsService"/>; events logged before the SDK is ready wait in order.</summary>
    internal sealed class FirebaseAnalyticsService : IAnalyticsService
    {
        private readonly Queue<(string, IReadOnlyDictionary<string, object>)> _pending = new Queue<(string, IReadOnlyDictionary<string, object>)>();
        private bool _ready;

        public void Initialize(bool personalized)
        {
            FirebaseBootstrap.EnsureReadyAsync().ContinueWith(
                task =>
                {
                    if (!task.Result)
                    {
                        return;
                    }

                    FirebaseAnalytics.SetConsent(new Dictionary<ConsentType, ConsentStatus>
                    {
                        [ConsentType.AnalyticsStorage] = ConsentStatus.Granted,
                        [ConsentType.AdStorage] = personalized ? ConsentStatus.Granted : ConsentStatus.Denied,
                        [ConsentType.AdUserData] = personalized ? ConsentStatus.Granted : ConsentStatus.Denied,
                        [ConsentType.AdPersonalization] = personalized ? ConsentStatus.Granted : ConsentStatus.Denied,
                    });
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    _ready = true;
                    while (_pending.Count > 0)
                    {
                        (string name, IReadOnlyDictionary<string, object> parameters) = _pending.Dequeue();
                        Send(name, parameters);
                    }
                },
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void Stop()
        {
            _pending.Clear();
            if (!_ready)
            {
                return;
            }

            _ready = false;
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(false);
            FirebaseAnalytics.SetConsent(new Dictionary<ConsentType, ConsentStatus>
            {
                [ConsentType.AnalyticsStorage] = ConsentStatus.Denied,
                [ConsentType.AdStorage] = ConsentStatus.Denied,
                [ConsentType.AdUserData] = ConsentStatus.Denied,
                [ConsentType.AdPersonalization] = ConsentStatus.Denied,
            });
        }

        public void Log(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            if (_ready)
            {
                Send(eventName, parameters);
            }
            else
            {
                _pending.Enqueue((eventName, parameters));
            }
        }

        private static void Send(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            var list = new List<Parameter>(parameters.Count);
            foreach (KeyValuePair<string, object> pair in parameters)
            {
                list.Add(pair.Value switch
                {
                    long l => new Parameter(pair.Key, l),
                    double d => new Parameter(pair.Key, d),
                    _ => new Parameter(pair.Key, Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty),
                });
            }

            FirebaseAnalytics.LogEvent(eventName, list.ToArray());
        }
    }

#if BLOOMLINGS_FIREBASE_CRASHLYTICS
    /// <summary>Firebase Crashlytics behind <see cref="ICrashReporter"/>, with the R14 custom keys.</summary>
    internal sealed class CrashlyticsCrashReporter : ICrashReporter
    {
        private readonly Dictionary<string, string> _keys = new Dictionary<string, string>();
        private bool _ready;

        public void Initialize()
        {
            FirebaseBootstrap.EnsureReadyAsync().ContinueWith(
                task =>
                {
                    if (!task.Result)
                    {
                        return;
                    }

                    Crashlytics.IsCrashlyticsCollectionEnabled = true;
                    Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                    _ready = true;
                    foreach (KeyValuePair<string, string> key in _keys)
                    {
                        Crashlytics.SetCustomKey(key.Key, key.Value);
                    }
                },
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void SetCustomKey(string key, string value)
        {
            _keys[key] = value;
            if (_ready)
            {
                Crashlytics.SetCustomKey(key, value);
            }
        }

        public void RecordException(Exception exception)
        {
            if (_ready)
            {
                Crashlytics.LogException(exception);
            }
        }
    }
#endif
}
#endif
