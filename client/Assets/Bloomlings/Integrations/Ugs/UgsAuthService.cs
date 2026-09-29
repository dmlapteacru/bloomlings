#if BLOOMLINGS_UGS
using System;
using System.Collections;
using System.Threading.Tasks;
using Bloomlings.Client.Services;
using Bloomlings.Client.Services.Backend;
using Unity.Services.Authentication;
using UnityEngine;

namespace Bloomlings.Integrations.Ugs
{
    /// <summary>
    /// UGS Authentication (FR-087, T138): anonymous sign-in on the first launch, in the background, and optional linking
    /// of Sign in with Apple or Google Play Games from Settings. The platform tokens come from the platform sign-in
    /// plugins through <see cref="ServiceProviders.AppleIdToken"/> and <see cref="ServiceProviders.GooglePlayGamesAuthCode"/>.
    /// An identity already linked to another player (a new device) signs in as that player instead; the cloud save
    /// merge (R15) then brings its progress here.
    /// </summary>
    internal sealed class UgsAuthService : IAuthService
    {
        public bool IsSignedIn => UgsBootstrap.IsSignedIn;

        public string? PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : null;

        public string? LinkedIdentity { get; private set; }

        public bool CanLink(LinkProvider provider) =>
            (provider == LinkProvider.Apple ? ServiceProviders.AppleIdToken : ServiceProviders.GooglePlayGamesAuthCode) != null;

        public IEnumerator SignIn(Action<bool> done)
        {
            Exception? error = null;
            yield return UgsBootstrap.Await(UgsBootstrap.EnsureSignedInAsync(), e => error = e);
            if (error != null)
            {
                Debug.LogWarning("[Auth] Anonymous sign-in failed; playing with the local profile. " + error.Message);
            }

            done(IsSignedIn);
        }

        public IEnumerator Link(LinkProvider provider, Action<bool> done)
        {
            Func<Action<string?>, IEnumerator>? source = provider == LinkProvider.Apple ? ServiceProviders.AppleIdToken : ServiceProviders.GooglePlayGamesAuthCode;
            string? token = null;
            if (source != null)
            {
                yield return source(t => token = t);
            }

            if (string.IsNullOrEmpty(token))
            {
                done(false);
                yield break;
            }

            Exception? error = null;
            yield return UgsBootstrap.Await(UgsBootstrap.EnsureSignedInAsync(), e => error = e);
            if (error == null)
            {
                yield return UgsBootstrap.Await(LinkAsync(provider, token!), e => error = e);
            }

            if (error != null)
            {
                Debug.LogWarning($"[Auth] Linking {provider} failed: {error.Message}");
                done(false);
                yield break;
            }

            LinkedIdentity = LinkedIdentities.Name(provider);
            done(true);
        }

        private static async Task LinkAsync(LinkProvider provider, string token)
        {
            IAuthenticationService auth = AuthenticationService.Instance;
            try
            {
                if (provider == LinkProvider.Apple)
                {
                    await auth.LinkWithAppleAsync(token);
                }
                else
                {
                    await auth.LinkWithGooglePlayGamesAsync(token);
                }
            }
            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                // The identity belongs to an existing player (another device): continue as that player.
                auth.SignOut();
                if (provider == LinkProvider.Apple)
                {
                    await auth.SignInWithAppleAsync(token);
                }
                else
                {
                    await auth.SignInWithGooglePlayGamesAsync(token);
                }
            }
        }
    }
}
#endif
