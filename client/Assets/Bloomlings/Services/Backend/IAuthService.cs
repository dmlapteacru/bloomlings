using System;
using System.Collections;

namespace Bloomlings.Client.Services.Backend
{
    /// <summary>A platform identity that can be linked to the anonymous player.</summary>
    public enum LinkProvider
    {
        Apple,
        GooglePlayGames,
    }

    /// <summary>
    /// Player identity (FR-087, T138). The first launch creates a local profile with no sign-in; the anonymous sign-in
    /// runs in the background and never blocks play. Linking Sign in with Apple or Google Play Games is optional, from
    /// Settings, and lets the cloud save follow the player to another device. The initial provider is UGS
    /// Authentication (<c>Integrations/Ugs</c>); offline, only the local profile exists and sign-in is retried later.
    /// </summary>
    public interface IAuthService
    {
        bool IsSignedIn { get; }

        /// <summary>The backend player id, or null while signed out.</summary>
        string? PlayerId { get; }

        /// <summary><c>apple</c>, <c>google_play_games</c> or null (the save's <c>linkedIdentity</c> values).</summary>
        string? LinkedIdentity { get; }

        /// <summary>The platform sign-in for this provider is available on this device and build.</summary>
        bool CanLink(LinkProvider provider);

        /// <summary>Coroutine: signs in anonymously (or restores the cached session); <paramref name="done"/> gets the result.</summary>
        IEnumerator SignIn(Action<bool> done);

        /// <summary>Coroutine: links a platform identity to the signed-in player.</summary>
        IEnumerator Link(LinkProvider provider, Action<bool> done);
    }

    /// <summary>The offline fallback: the local profile only.</summary>
    public sealed class LocalOnlyAuthService : IAuthService
    {
        public bool IsSignedIn => false;

        public string? PlayerId => null;

        public string? LinkedIdentity => null;

        public bool CanLink(LinkProvider provider) => false;

        public IEnumerator SignIn(Action<bool> done)
        {
            done(false);
            yield break;
        }

        public IEnumerator Link(LinkProvider provider, Action<bool> done)
        {
            done(false);
            yield break;
        }
    }

    /// <summary>The save's name for a linked identity.</summary>
    public static class LinkedIdentities
    {
        public static string Name(LinkProvider provider) => provider == LinkProvider.Apple ? "apple" : "google_play_games";
    }
}
