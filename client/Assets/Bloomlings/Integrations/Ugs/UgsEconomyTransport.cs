#if BLOOMLINGS_UGS && BLOOMLINGS_UGS_CLOUDCODE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bloomlings.Client.Services.Economy.Online;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using UnityEngine;

namespace Bloomlings.Integrations.Ugs
{
    /// <summary>Opt-in transport only. Does not register or enable authoritative currency by SDK detection.</summary>
    internal sealed class UgsEconomyTransport : IEconomyTransport
    {
        public bool IsAvailable => UgsBootstrap.IsSignedIn && Application.internetReachability != NetworkReachability.NotReachable;
        public string? PlayerId => UgsBootstrap.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
        public long SessionVersion => UgsBootstrap.AuthSessionVersion;
        public IEnumerator Read(string owner, long session, string installationId, Action<EconomyReply> done) => Call(owner, session,
            new Dictionary<string, object> { ["action"] = "read", ["expectedOwnerId"] = owner, ["installationId"] = installationId }, done);
        public IEnumerator Execute(string owner, long session, string operationJson, Action<EconomyReply> done) => Call(owner, session,
            new Dictionary<string, object> { ["action"] = "execute", ["expectedOwnerId"] = owner, ["operationJson"] = operationJson }, done);
        private IEnumerator Call(string owner, long session, Dictionary<string, object> args, Action<EconomyReply> done)
        {
            bool Matches() => IsAvailable && owner == PlayerId && session == SessionVersion;
            if (!Matches()) { done(EconomyReply.Unavailable); yield break; }
            Task<JObject>? call = null;
            try { call = CloudCodeService.Instance.CallEndpointAsync<JObject>("ExecuteEconomy", args); }
            catch (Exception) { }
            if (call == null) { done(EconomyReply.Unavailable); yield break; }
            Exception? error = null;
            yield return UgsBootstrap.Await(call, e => error = e);
            if (error != null || !Matches()) { done(EconomyReply.Unavailable); yield break; }
            EconomyReply result;
            try { result = new EconomyReply(call.Result); }
            catch (ContentFormatException) { result = EconomyReply.Unavailable; }
            done(result);
        }
    }
}
#endif
