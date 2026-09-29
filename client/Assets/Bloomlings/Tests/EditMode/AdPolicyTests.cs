using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Ads;
using Bloomlings.Client.Services.Config;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>Interstitial and rescue placement rules (FR-052, FR-053, SC-013; T124).</summary>
    public class AdPolicyTests
    {
        private static readonly DateTime Start = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

        private static AdPolicy Policy(Dictionary<string, string>? overrides = null) =>
            new AdPolicy(new BundledRemoteConfigService(overrides ?? new Dictionary<string, string>()), Start);

        private static AdPolicy WonThree(AdPolicy policy)
        {
            policy.OnLevelWon();
            policy.OnLevelWon();
            policy.OnLevelWon();
            return policy;
        }

        [Test]
        public void NoInterstitial_BeforeTheFirstLevel_DuringALevel_OrAfterAFail()
        {
            AdPolicy policy = WonThree(Policy());
            DateTime later = Start.AddMinutes(10);

            Assert.That(policy.MayShowInterstitial(AdMoment.PostWin, 10, removeAds: false, later), Is.False, "onboarding L1–10");
            Assert.That(policy.MayShowInterstitial(AdMoment.InLevel, 20, removeAds: false, later), Is.False);
            Assert.That(policy.MayShowInterstitial(AdMoment.PostFail, 20, removeAds: false, later), Is.False);
            Assert.That(policy.MayShowInterstitial(AdMoment.PostWin, 11, removeAds: false, later), Is.True);
        }

        [Test]
        public void TimeAndLevelCaps_BothApply()
        {
            AdPolicy policy = Policy();
            policy.OnLevelWon();
            policy.OnLevelWon();
            Assert.That(policy.MayShowInterstitial(AdMoment.PostWin, 30, false, Start.AddMinutes(10)), Is.False, "only 2 of 3 levels");

            policy.OnLevelWon();
            Assert.That(policy.MayShowInterstitial(AdMoment.PostWin, 30, false, Start.AddSeconds(179)), Is.False, "under 180 s");
            Assert.That(policy.MayShowInterstitial(AdMoment.PostWin, 30, false, Start.AddSeconds(180)), Is.True);

            policy.OnInterstitialShown(Start.AddSeconds(180));
            WonThree(policy);
            Assert.That(policy.MayShowInterstitial(AdMoment.PostWin, 33, false, Start.AddSeconds(300)), Is.False, "the timer restarted");
            Assert.That(policy.MayShowInterstitial(AdMoment.PostWin, 33, false, Start.AddSeconds(360)), Is.True);
        }

        [Test]
        public void RemoveAds_DisablesInterstitials()
        {
            AdPolicy policy = WonThree(Policy());

            Assert.That(policy.MayShowInterstitial(AdMoment.PostWin, 50, removeAds: true, Start.AddHours(1)), Is.False);
        }

        [Test]
        public void Rescue_HappensAtMostOncePerAttempt()
        {
            AdPolicy policy = Policy();
            policy.OnAttemptStarted();
            Assert.That(policy.MayOfferRescue, Is.True);

            policy.OnRescueUsed();
            Assert.That(policy.MayOfferRescue, Is.False);

            policy.OnAttemptStarted();
            Assert.That(policy.MayOfferRescue, Is.True, "a restart is a new attempt");
        }

        [Test]
        public void RemoteValues_AreClamped()
        {
            // firstLevel below 11 is clamped to 11: onboarding stays ad-free whatever Remote Config says.
            AdPolicy policy = WonThree(Policy(new Dictionary<string, string> { ["ads.interstitial.firstLevel"] = "2", ["ads.rescue.perAttempt"] = "5" }));

            Assert.That(policy.MayShowInterstitial(AdMoment.PostWin, 5, false, Start.AddHours(1)), Is.False);
            policy.OnRescueUsed();
            Assert.That(policy.MayOfferRescue, Is.False, "rescue per attempt is capped at 1");
        }
    }
}
