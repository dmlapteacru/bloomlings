using System;
using System.Collections;

namespace Bloomlings.Client.Services.Consent
{
    /// <summary>What the player allowed (FR-090). The default is the most restrictive: nothing until consent is known.</summary>
    public enum ConsentState
    {
        /// <summary>Not gathered yet: no ad requests and no analytics.</summary>
        Unknown,

        /// <summary>Consent refused or not obtainable: no ads, no analytics.</summary>
        Denied,

        /// <summary>Ads may be requested without personalization; analytics without personal data.</summary>
        NonPersonalized,

        /// <summary>Personalized ads and analytics are allowed.</summary>
        Personalized,
    }

    /// <summary>
    /// Consent for ads and analytics (FR-090, research R12; T127): Google UMP for GDPR and US-state rules, Apple ATT on
    /// iOS. It runs before ads or analytics initialize.
    /// </summary>
    public interface IConsentService
    {
        ConsentState State { get; }

        bool CanRequestAds { get; }

        bool AnalyticsAllowed { get; }

        /// <summary>Coroutine: updates the consent information and shows the forms the rules require.</summary>
        IEnumerator Gather();

        /// <summary>The privacy options entry point in Settings, when the rules require one.</summary>
        bool PrivacyOptionsRequired { get; }

        /// <summary>Shows the privacy options form; the state is read again when it closes.</summary>
        void ShowPrivacyOptions(Action onClosed);

        /// <summary>The state changed after <see cref="Gather"/> (the player changed it in the privacy options).</summary>
        event Action<ConsentState>? Changed;
    }

    /// <summary>What a consent SDK reports (the UMP/ATT integration implements it, with <see cref="TcfConsent"/>).</summary>
    public interface IConsentProvider
    {
        IEnumerator Update(Action<ConsentState> onState);

        bool PrivacyOptionsRequired { get; }

        /// <summary>Shows the privacy options form and reports the state after it closes.</summary>
        void ShowPrivacyOptions(Action<ConsentState> onClosed);
    }
}
