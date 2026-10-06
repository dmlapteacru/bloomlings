using System;
using System.Collections;

namespace Bloomlings.Client.Services.Consent
{
    /// <summary>
    /// <see cref="IConsentService"/> over an optional provider (T127). Without a provider (no SDK, Editor) or before it
    /// answers, the state stays <see cref="ConsentState.Unknown"/>: no ad requests and no analytics, the most restrictive
    /// default (FR-090). Gameplay never waits for it (FR-074). A change made later in the privacy options raises
    /// <see cref="Changed"/>, so ads and analytics follow it in the same session.
    /// </summary>
    public sealed class ConsentService : IConsentService
    {
        private readonly IConsentProvider? _provider;

        public ConsentService(IConsentProvider? provider)
        {
            _provider = provider;
        }

        public ConsentState State { get; private set; } = ConsentState.Unknown;

        public bool CanRequestAds => State == ConsentState.NonPersonalized || State == ConsentState.Personalized;

        public bool AnalyticsAllowed => State == ConsentState.Personalized || State == ConsentState.NonPersonalized;

        public bool PrivacyOptionsRequired => _provider?.PrivacyOptionsRequired ?? false;

        public event Action<ConsentState>? Changed;

        public IEnumerator Gather()
        {
            if (_provider == null)
            {
                yield break;
            }

            yield return _provider.Update(state => State = state);
        }

        public void ShowPrivacyOptions(Action onClosed)
        {
            if (_provider == null)
            {
                onClosed();
                return;
            }

            _provider.ShowPrivacyOptions(state =>
            {
                if (state != State)
                {
                    State = state;
                    Changed?.Invoke(state);
                }

                onClosed();
            });
        }
    }
}
