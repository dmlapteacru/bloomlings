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
    /// builds (fill <see cref="AdUnits"/> before release). Placement rules stay in <c>AdPolicy</c>.
    /// </summary>
    internal sealed class GoogleMobileAdsService : IAdsService
    {
        private RewardedAd? _rewarded;
        private InterstitialAd? _interstitial;
        private bool _initialized;
        private bool _personalized;

        public bool IsAvailable => _initialized;

        public bool IsRewardedReady => _rewarded != null && _rewarded.CanShowAd();

        public bool IsInterstitialReady => _interstitial != null && _interstitial.CanShowAd();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            ServiceProviders.Ads = () => new GoogleMobileAdsService();
            ServiceProviders.Consent = () => new UmpConsentProvider();
        }

        public void Initialize(ConsentState consent)
        {
            if (_initialized || !(consent == ConsentState.NonPersonalized || consent == ConsentState.Personalized))
            {
                return;
            }

            _personalized = consent == ConsentState.Personalized;
            MobileAds.Initialize(_ =>
            {
                _initialized = true;
                LoadRewarded();
                LoadInterstitial();
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

    /// <summary>Ad unit ids per environment. The development ids are Google's public test units.</summary>
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

        private static bool UseTestUnits => Application.isEditor || Debug.isDebugBuild || ReleaseRewarded.Length == 0;

        public static string Rewarded => UseTestUnits ? TestRewarded : ReleaseRewarded;

        public static string Interstitial => UseTestUnits ? TestInterstitial : ReleaseInterstitial;
    }

    /// <summary>
    /// Google UMP (GDPR and US-state rules) and, on iOS, the ATT prompt before personalized ads (FR-090; T127). Without
    /// an answer the state stays Unknown, the most restrictive default.
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

            onState(ConsentInformation.CanRequestAds() ? Personalization() : ConsentState.Denied);
        }

        public void ShowPrivacyOptions(Action onClosed) => ConsentForm.ShowPrivacyOptionsForm(_ => onClosed());

        /// <summary>UMP decides whether ads may be requested; on iOS personalization also needs ATT authorization.</summary>
        private static ConsentState Personalization()
        {
#if UNITY_IOS && BLOOMLINGS_ATT
            return Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED
                ? ConsentState.Personalized
                : ConsentState.NonPersonalized;
#elif UNITY_IOS
            return ConsentState.NonPersonalized;
#else
            return ConsentState.Personalized;
#endif
        }
    }
}
#endif
