using System;
using System.Collections;
using Bloomlings.Client.Services.Consent;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The consent mapping (FR-090): a refusal is never read as consent, and a change applies in the session.</summary>
    public class ConsentTests
    {
        private const string Everything = "1111111111";

        [TestCase(false, 1, Everything, TrackingAuthorization.NotApplicable, ConsentState.Denied)]
        [TestCase(true, 0, "", TrackingAuthorization.NotApplicable, ConsentState.Personalized)]
        [TestCase(true, 1, Everything, TrackingAuthorization.NotApplicable, ConsentState.Personalized)]
        [TestCase(true, 1, "0000000000", TrackingAuthorization.NotApplicable, ConsentState.Denied)]
        [TestCase(true, 1, "", TrackingAuthorization.NotApplicable, ConsentState.Denied)]
        [TestCase(true, 1, "1100000000", TrackingAuthorization.NotApplicable, ConsentState.NonPersonalized)]
        [TestCase(true, 1, "1110000000", TrackingAuthorization.NotApplicable, ConsentState.NonPersonalized)]
        [TestCase(true, 1, "1011", TrackingAuthorization.NotApplicable, ConsentState.Personalized)]
        [TestCase(true, 0, "", TrackingAuthorization.NotAuthorized, ConsentState.NonPersonalized)]
        [TestCase(true, 1, Everything, TrackingAuthorization.Authorized, ConsentState.Personalized)]
        [TestCase(true, 1, Everything, TrackingAuthorization.NotAuthorized, ConsentState.NonPersonalized)]
        public void Resolve_ReadsTheTcfValues(bool canRequestAds, int gdprApplies, string purposes, TrackingAuthorization tracking, ConsentState expected)
        {
            Assert.That(TcfConsent.Resolve(canRequestAds, gdprApplies, purposes, tracking), Is.EqualTo(expected));
        }

        [Test]
        public void AnEeaRefusal_ThatStillAllowsLimitedAds_IsNotConsent()
        {
            // UMP says ads may be requested (limited ads), but the player refused every purpose.
            ConsentState state = TcfConsent.Resolve(true, 1, "0000000000", TrackingAuthorization.NotApplicable);

            var service = new ConsentService(new FakeProvider(state));
            Drain(service.Gather());

            Assert.That(service.State, Is.EqualTo(ConsentState.Denied));
            Assert.That(service.AnalyticsAllowed, Is.False);
            Assert.That(service.CanRequestAds, Is.False);
        }

        [Test]
        public void PrivacyOptions_ChangeTheStateAndRaiseChanged()
        {
            var provider = new FakeProvider(ConsentState.Personalized);
            var service = new ConsentService(provider);
            Drain(service.Gather());
            ConsentState? changed = null;
            service.Changed += state => changed = state;
            bool closed = false;

            provider.Next = ConsentState.NonPersonalized;
            service.ShowPrivacyOptions(() => closed = true);

            Assert.That(closed, Is.True);
            Assert.That(service.State, Is.EqualTo(ConsentState.NonPersonalized));
            Assert.That(changed, Is.EqualTo(ConsentState.NonPersonalized));

            changed = null;
            service.ShowPrivacyOptions(() => { });
            Assert.That(changed, Is.Null, "no event when nothing changed");
        }

        private static void Drain(IEnumerator routine)
        {
            while (routine.MoveNext())
            {
                if (routine.Current is IEnumerator inner)
                {
                    Drain(inner);
                }
            }
        }

        private sealed class FakeProvider : IConsentProvider
        {
            private readonly ConsentState _first;

            public FakeProvider(ConsentState first)
            {
                _first = first;
                Next = first;
            }

            public ConsentState Next { get; set; }

            public bool PrivacyOptionsRequired => true;

            public IEnumerator Update(Action<ConsentState> onState)
            {
                onState(_first);
                yield break;
            }

            public void ShowPrivacyOptions(Action<ConsentState> onClosed) => onClosed(Next);
        }
    }
}
