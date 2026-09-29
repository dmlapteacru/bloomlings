namespace Bloomlings.Client.Services.Consent
{
    /// <summary>The iOS App Tracking Transparency answer; not applicable on other platforms.</summary>
    public enum TrackingAuthorization
    {
        NotApplicable,
        Authorized,
        NotAuthorized,
    }

    /// <summary>
    /// Turns what the consent form stored into a <see cref="ConsentState"/> (FR-090). The Google UMP form writes the IAB
    /// TCF v2 values <see cref="GdprAppliesKey"/> (1 where the GDPR applies) and <see cref="PurposeConsentsKey"/> (one
    /// '0' or '1' per purpose, purpose 1 first). "Ads may be requested" alone is not consent: it is also true after a
    /// refusal, when Google serves limited ads. So:
    /// <list type="bullet">
    /// <item>ads may not be requested: <see cref="ConsentState.Denied"/>;</item>
    /// <item>the GDPR applies and purpose 1 (store and access information on the device) was refused:
    /// <see cref="ConsentState.Denied"/>;</item>
    /// <item>the GDPR applies and purposes 3 and 4 (personalized ads profile and selection) were not both given, or iOS
    /// tracking is not authorized: <see cref="ConsentState.NonPersonalized"/>;</item>
    /// <item>otherwise <see cref="ConsentState.Personalized"/>.</item>
    /// </list>
    /// Engine-free, so the mapping is tested without the SDK.
    /// </summary>
    public static class TcfConsent
    {
        public const string GdprAppliesKey = "IABTCF_gdprApplies";
        public const string PurposeConsentsKey = "IABTCF_PurposeConsents";

        public static ConsentState Resolve(bool canRequestAds, int gdprApplies, string? purposeConsents, TrackingAuthorization tracking)
        {
            if (!canRequestAds)
            {
                return ConsentState.Denied;
            }

            bool gdpr = gdprApplies == 1;
            if (gdpr && !Purpose(purposeConsents, 1))
            {
                return ConsentState.Denied;
            }

            bool personalized = (!gdpr || (Purpose(purposeConsents, 3) && Purpose(purposeConsents, 4)))
                && tracking != TrackingAuthorization.NotAuthorized;
            return personalized ? ConsentState.Personalized : ConsentState.NonPersonalized;
        }

        /// <summary>Whether a purpose (1-based) was given in a TCF purpose consent string.</summary>
        public static bool Purpose(string? purposeConsents, int purpose) =>
            purposeConsents != null && purpose >= 1 && purposeConsents.Length >= purpose && purposeConsents[purpose - 1] == '1';
    }
}
