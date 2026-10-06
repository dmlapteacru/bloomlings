#if BLOOMLINGS_ADMOB
using System;
using System.Collections;
using Bloomlings.Client.Services;
using Bloomlings.Client.Services.Ads;
using Bloomlings.Client.Services.Consent;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Bloomlings.Integrations.Ads
{
    /// <summary>
    /// <see cref="IAdsService"/> over the Google Mobile Ads plugin, mediation-ready (research R12; T128). Ad units come
    /// per environment: Google's test units in the Editor and development builds, the production units in release
    /// builds (fill <see cref="AdUnits"/> before release; without them a release build shows no ads). Placement rules
    /// stay in <c>AdPolicy</c>.
    /// </summary>
    internal sealed class GoogleMobileAdsService : IAdsService
    {
        private RewardedAd? _rewarded;
        private InterstitialAd? _interstitial;
        private bool _initialized;
        private bool _allowed;
        private bool _personalized;

        public bool IsAvailable => _initialized && _allowed;

        public bool IsRewardedReady => _allowed && _rewarded != null && _rewarded.CanShowAd();

        public bool IsInterstitialReady => _allowed && _interstitial != null && _interstitial.CanShowAd();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            ServiceProviders.Ads = () => new GoogleMobileAdsService();
            ServiceProviders.Consent = () => new UmpConsentProvider();
        }

        /// <summary>
        /// Called after consent is gathered and again whenever the player changes it: a refusal drops the loaded ads and
        /// stops new requests; a change of personalization reloads them with the new request.
        /// </summary>
        public void Initialize(ConsentState consent)
        {
            _allowed = consent == ConsentState.NonPersonalized || consent == ConsentState.Personalized;
            _personalized = consent == ConsentState.Personalized;
            _rewarded?.Destroy();
            _rewarded = null;
            _interstitial?.Destroy();
            _interstitial = null;
            if (!_allowed)
            {
                return;
            }

            if (!AdUnits.Configured)
            {
                Debug.LogError("[Ads] Release build without production ad unit ids: ads stay off (fill AdUnits before release).");
                return;
            }

            if (_initialized)
            {
                LoadRewarded();
                LoadInterstitial();
                return;
            }

            MobileAds.Initialize(_ =>
            {
                _initialized = true;
                if (_allowed)
                {
                    LoadRewarded();
                    LoadInterstitial();
                }
            });
        }

        public void ShowRewarded(string placement, Action<bool> onCompleted)
        {
            if (!IsRewardedReady)
            {
                onCompleted(false);
                return;
            }

            bool earned = false;
            RewardedAd ad = _rewarded!;
            _rewarded = null;
            ad.OnAdFullScreenContentClosed += () =>
            {
                ad.Destroy();
                LoadRewarded();
                onCompleted(earned);
            };
            ad.OnAdFullScreenContentFailed += _ =>
            {
                ad.Destroy();
                LoadRewarded();
                onCompleted(false);
            };
            ad.Show(_ => earned = true);
        }

        public void ShowInterstitial(Action onClosed)
        {
            if (!IsInterstitialReady)
            {
                onClosed();
                return;
            }

            InterstitialAd ad = _interstitial!;
            _interstitial = null;
            ad.OnAdFullScreenContentClosed += () =>
            {
                ad.Destroy();
                LoadInterstitial();
                onClosed();
            };
            ad.OnAdFullScreenContentFailed += _ =>
            {
                ad.Destroy();
                LoadInterstitial();
                onClosed();
            };
            ad.Show();
        }

        private AdRequest Request()
        {
            var request = new AdRequest();
            if (!_personalized)
            {
                request.Extras.Add("npa", "1");
            }

            return request;
        }

        private void LoadRewarded() => RewardedAd.Load(AdUnits.Rewarded, Request(), (ad, error) =>
        {
            if (error == null)
            {
                _rewarded = ad;
            }
        });

        private void LoadInterstitial() => InterstitialAd.Load(AdUnits.Interstitial, Request(), (ad, error) =>
        {
            if (error == null)
            {
                _interstitial = ad;
            }
        });
    }

    /// <summary>
    /// Ad unit ids per environment. The development ids are Google's public test units, used in the Editor and in
    /// development builds only; a release build without its production ids shows no ads rather than test ads.
    /// </summary>
    internal static class AdUnits
    {
#if UNITY_IOS
        private const string TestRewarded = "ca-app-pub-3940256099942544/1712485313";
        private const string TestInterstitial = "ca-app-pub-3940256099942544/4411468910";
        private const string ReleaseRewarded = "";
        private const string ReleaseInterstitial = "";
#else
        private const string TestRewarded = "ca-app-pub-3940256099942544/5224354917";
        private const string TestInterstitial = "ca-app-pub-3940256099942544/1033173712";
        private const string ReleaseRewarded = "";
        private const string ReleaseInterstitial = "";
#endif

        private static bool UseTestUnits => Application.isEditor || Debug.isDebugBuild;

        public static bool Configured => UseTestUnits || (ReleaseRewarded.Length > 0 && ReleaseInterstitial.Length > 0);

        public static string Rewarded => UseTestUnits ? TestRewarded : ReleaseRewarded;

        public static string Interstitial => UseTestUnits ? TestInterstitial : ReleaseInterstitial;
    }

    /// <summary>
    /// Google UMP (GDPR and US-state rules) and, on iOS, the ATT prompt before personalized ads (FR-090; T127). The state
    /// comes from the TCF values the form stored (<see cref="TcfConsent"/>), not from "ads may be requested" alone, which
    /// is also true after a refusal. Without an answer the state stays Unknown, the most restrictive default.
    /// On iOS the ATT prompt is shown after the UMP form when UMP did not already show it (its IDFA explainer); it needs
    /// the iOS 14 Advertising Support package (<c>BLOOMLINGS_ATT</c>) and an <c>NSUserTrackingUsageDescription</c>.
    /// </summary>
    internal sealed class UmpConsentProvider : IConsentProvider
    {
        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public IEnumerator Update(Action<ConsentState> onState)
        {
            bool done = false;
            ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
            {
                if (updateError != null)
                {
                    done = true;
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(_ => done = true);
            });
            while (!done)
            {
                yield return null;
            }

            yield return RequestTracking();
            onState(Current());
        }

        public void ShowPrivacyOptions(Action<ConsentState> onClosed) => ConsentForm.ShowPrivacyOptionsForm(_ => onClosed(Current()));

        private static ConsentState Current() =>
            TcfConsent.Resolve(ConsentInformation.CanRequestAds(), TcfValues.GdprApplies(), TcfValues.PurposeConsents(), Tracking());

        private static IEnumerator RequestTracking()
        {
#if UNITY_IOS && BLOOMLINGS_ATT
            if (Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                Unity.Advertisement.IosSupport.ATTrackingStatusBinding.RequestAuthorizationTracking();
                float until = Time.realtimeSinceStartup + 120f;
                while (Time.realtimeSinceStartup < until
                    && Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                    == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    yield return null;
                }
            }
#endif
            yield break;
        }

        private static TrackingAuthorization Tracking()
        {
#if UNITY_IOS && BLOOMLINGS_ATT
            return Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED
                ? TrackingAuthorization.Authorized
                : TrackingAuthorization.NotAuthorized;
#elif UNITY_IOS
            return TrackingAuthorization.NotAuthorized;
#else
            return TrackingAuthorization.NotApplicable;
#endif
        }
    }

    /// <summary>
    /// Reads the TCF values the UMP form stores: the default SharedPreferences on Android, NSUserDefaults (which Unity's
    /// PlayerPrefs uses) on iOS. A value that cannot be read counts as "the GDPR applies, nothing given", the most
    /// restrictive reading.
    /// </summary>
    internal static class TcfValues
    {
        public static int GdprApplies()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using AndroidJavaObject prefs = Preferences();
                return prefs.Call<int>("getInt", TcfConsent.GdprAppliesKey, 0);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Consent] " + ex.Message);
                return 1;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            return PlayerPrefs.GetInt(TcfConsent.GdprAppliesKey, 0);
#else
            return 0;
#endif
        }

        public static string PurposeConsents()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using AndroidJavaObject prefs = Preferences();
                return prefs.Call<string>("getString", TcfConsent.PurposeConsentsKey, string.Empty) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Consent] " + ex.Message);
                return string.Empty;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            return PlayerPrefs.GetString(TcfConsent.PurposeConsentsKey, string.Empty);
#else
            return string.Empty;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject Preferences()
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var manager = new AndroidJavaClass("android.preference.PreferenceManager");
            return manager.CallStatic<AndroidJavaObject>("getDefaultSharedPreferences", activity);
        }
#endif
    }
}
#endif
