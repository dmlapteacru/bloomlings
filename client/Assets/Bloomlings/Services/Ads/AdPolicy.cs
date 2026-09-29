using System;
using Bloomlings.Client.Services.Config;

namespace Bloomlings.Client.Services.Ads
{
    /// <summary>Where the game is when it asks for an interstitial.</summary>
    public enum AdMoment
    {
        /// <summary>Between the Win screen and the next level: the only moment an interstitial may show (FR-053).</summary>
        PostWin,

        PostFail,

        InLevel,
    }

    /// <summary>
    /// The ad placement rules (FR-052, FR-053, SC-013, research R12; T129). Engine-free and driven by <see cref="IClock"/>
    /// times, so the rules are tested whatever ad SDK is used:
    /// <list type="bullet">
    /// <item>interstitials only at the post-win transition, never in a level or right after a fail;</item>
    /// <item>none for levels before <c>ads.interstitial.firstLevel</c> (onboarding, L1–10);</item>
    /// <item>at least <c>ads.interstitial.minSeconds</c> since the last one (or since the session started) and at least
    /// <c>ads.interstitial.minLevels</c> won levels in between;</item>
    /// <item>none with Remove Ads, which keeps the optional rewarded ads (FR-054);</item>
    /// <item>at most <c>ads.rescue.perAttempt</c> rewarded jam rescues per attempt.</item>
    /// </list>
    /// </summary>
    public sealed class AdPolicy
    {
        private DateTime _lastInterstitial;
        private int _levelsSince;
        private int _rescuesThisAttempt;

        public AdPolicy(IRemoteConfigService config, DateTime sessionStartUtc)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _lastInterstitial = sessionStartUtc;
        }

        public IRemoteConfigService Config { get; }

        /// <summary>Whether an interstitial may show now, after the player won <paramref name="wonLevel"/>.</summary>
        public bool MayShowInterstitial(AdMoment moment, int wonLevel, bool removeAds, DateTime nowUtc)
        {
            if (moment != AdMoment.PostWin || removeAds)
            {
                return false;
            }

            if (wonLevel < Config.Get(RemoteConfigKeys.InterstitialFirstLevel))
            {
                return false;
            }

            return _levelsSince >= Config.Get(RemoteConfigKeys.InterstitialMinLevels)
                && (nowUtc - _lastInterstitial).TotalSeconds >= Config.Get(RemoteConfigKeys.InterstitialMinSeconds);
        }

        /// <summary>Levels won since the last interstitial (the <c>ad_interstitial</c> event).</summary>
        public int LevelsSinceLast => _levelsSince;

        public long SecondsSinceLast(DateTime nowUtc) => (long)Math.Max(0, (nowUtc - _lastInterstitial).TotalSeconds);

        /// <summary>A level was won (it counts toward the level cap).</summary>
        public void OnLevelWon() => _levelsSince++;

        public void OnInterstitialShown(DateTime nowUtc)
        {
            _lastInterstitial = nowUtc;
            _levelsSince = 0;
        }

        /// <summary>A new attempt started (level start or restart): its rescue allowance resets.</summary>
        public void OnAttemptStarted() => _rescuesThisAttempt = 0;

        /// <summary>The rewarded jam rescue may be offered (the player still decides to watch, FR-052).</summary>
        public bool MayOfferRescue => _rescuesThisAttempt < Config.Get(RemoteConfigKeys.RescuePerAttempt);

        public void OnRescueUsed() => _rescuesThisAttempt++;
    }
}
