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
    /// An occupied identity never changes the signed-in player implicitly.
    /// </summary>
    internal sealed class UgsAuthService : IAuthService, IAccountLinkStatus
    {
        private bool _linking;
        public AccountLinkResult LastLinkResult { get; private set; }
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
            if (_linking) { done(false); yield break; }
            _linking = true;
            LastLinkResult = AccountLinkResult.None;
            try
            {
                Exception? error = null;
                yield return UgsBootstrap.Await(UgsBootstrap.EnsureSignedInAsync(), e => error = e);
                if (error != null) { LastLinkResult = AccountLinkResult.Failed; done(false); yield break; }
                string? owner = PlayerId;
                long session = UgsBootstrap.AuthSessionVersion;
                Func<Action<string?>, IEnumerator>? source = provider == LinkProvider.Apple ? ServiceProviders.AppleIdToken : ServiceProviders.GooglePlayGamesAuthCode;
                string? token = null;
                if (source != null) yield return source(t => token = t);
                if (string.IsNullOrEmpty(token)) { LastLinkResult = AccountLinkResult.Cancelled; done(false); yield break; }
                if (owner != PlayerId || session != UgsBootstrap.AuthSessionVersion)
                { LastLinkResult = AccountLinkResult.Failed; done(false); yield break; }
                yield return UgsBootstrap.Await(LinkAsync(provider, token!), e => error = e);
                if (error != null || owner != PlayerId || session != UgsBootstrap.AuthSessionVersion)
                {
                    LastLinkResult = error is AuthenticationException authError && authError.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked
                        ? AccountLinkResult.ExistingAccount : AccountLinkResult.Failed;
                    done(false);
                    yield break;
                }
                LinkedIdentity = LinkedIdentities.Name(provider);
                LastLinkResult = AccountLinkResult.Linked;
                done(true);
            }
            finally { _linking = false; }
        }

        private static async Task LinkAsync(LinkProvider provider, string token)
        {
            IAuthenticationService auth = AuthenticationService.Instance;
            // A link conflict is NOT permission to sign into that other player. Keep the current
            // authenticated owner and let an explicit account-restore flow handle the conflict.
            if (provider == LinkProvider.Apple)
                await auth.LinkWithAppleAsync(token);
            else
                await auth.LinkWithGooglePlayGamesAsync(token);
        }
    }
}
#endif
