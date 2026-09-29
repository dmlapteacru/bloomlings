using System;
using Bloomlings.Client.Services.Consent;

namespace Bloomlings.Client.Services.Ads
{
    /// <summary>
    /// Rewarded and interstitial ads (FR-052, FR-053, research R12; T128). Placement rules live in <see cref="AdPolicy"/>,
    /// not here: this only loads and shows. Every rewarded ad is started by the player.
    /// </summary>
    public interface IAdsService
    {
        /// <summary>False offline, before consent allows ad requests, or without an ad SDK (then the ad options hide).</summary>
        bool IsAvailable { get; }

        bool IsRewardedReady { get; }

        bool IsInterstitialReady { get; }

        /// <summary>Initializes after consent (FR-090) and starts loading; personalization follows the consent state.</summary>
        void Initialize(ConsentState consent);

        /// <summary>Shows a rewarded ad; the callback gets true only when the reward was earned.</summary>
        void ShowRewarded(string placement, Action<bool> onCompleted);

        /// <summary>Shows an interstitial; the callback runs when it closes (or at once when none is ready).</summary>
        void ShowInterstitial(Action onClosed);
    }

    /// <summary>No ads: offline, in the Editor without the SDK, or before consent. Rewarded options are hidden.</summary>
    public sealed class UnavailableAdsService : IAdsService
    {
        public bool IsAvailable => false;

        public bool IsRewardedReady => false;

        public bool IsInterstitialReady => false;

        public void Initialize(ConsentState consent)
        {
        }

        public void ShowRewarded(string placement, Action<bool> onCompleted) => onCompleted(false);

        public void ShowInterstitial(Action onClosed) => onClosed();
    }

    /// <summary>The rewarded placements (FR-052), for analytics and ad unit mapping.</summary>
    public static class AdPlacements
    {
        public const string JamRescue = "jam_rescue";
        public const string FreeBooster = "free_booster";
        public const string DoubleWin = "double_win";
        public const string DailyBonus = "daily_bonus";
    }
}
