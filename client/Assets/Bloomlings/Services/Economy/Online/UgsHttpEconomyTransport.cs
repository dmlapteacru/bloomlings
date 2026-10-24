using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Economy.Online
{
    /// <summary>Memory-only player session for a native host. No admin keys, token files or platform-account switching.</summary>
    public sealed class UgsPlayerSession
    {
        private readonly object _gate = new object();
        private string? _owner, _token;
        private DateTimeOffset _expires;
        private bool _online;
        private long _version;
        public string? PlayerId { get { lock (_gate) return _owner; } }
        public long SessionVersion { get { lock (_gate) return _version; } }
        public bool IsAvailable { get { lock (_gate) return Available; } }
        private bool Available => _online && _owner != null && _token != null && _expires > DateTimeOffset.UtcNow;
        public void SetNetworkAvailable(bool online) { lock (_gate) _online = online; }
        /// <summary>Called only by the authenticated native sign-in/refresh provider, never by a display-name/profile picker.</summary>
        public void SetAuthenticated(string owner, string accessToken, DateTimeOffset expires)
        {
            if (!EconomyProtocol.IsId(owner) || string.IsNullOrEmpty(accessToken) || accessToken.Length > 16384
                || !System.Text.RegularExpressions.Regex.IsMatch(accessToken, @"\A[A-Za-z0-9._~-]+\z") || expires <= DateTimeOffset.UtcNow)
                throw new ArgumentException("Invalid player session.");
            lock (_gate) { _owner = owner; _token = accessToken; _expires = expires; _version++; }
        }
        public void SignOut() { lock (_gate) { _token = null; _owner = null; _version++; } }
        internal bool Access(string owner, long version, out string token)
        {
            lock (_gate) { token = ""; if (!Available || _owner != owner || _version != version) return false; token = _token!; return true; }
        }
    }

    /// <summary>
    /// UGS Cloud Code's player REST API, usable in .NET Android without the Unity engine.
    /// One HTTP attempt only. The durable economy client, not HTTP retries, owns transaction replay.
    /// </summary>
    public sealed class UgsHttpEconomyTransport : IEconomyTransport, IDisposable
    {
        private readonly UgsPlayerSession _session;
        private readonly HttpClient _http;
        private readonly Uri _endpoint;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private bool _disposed;
        private const int MaxResponseBytes = 1024 * 1024;
        public UgsHttpEconomyTransport(string projectId, UgsPlayerSession session, HttpMessageHandler? handler = null)
        {
            if (!Guid.TryParseExact(projectId, "D", out Guid project)) throw new ArgumentException("A UGS project UUID is required.");
            _session = session;
            _endpoint = new Uri("https://cloud-code.services.api.unity.com/v1/projects/" + project.ToString("D") + "/scripts/ExecuteEconomy");
            _http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }, true);
            _http.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        }
        public bool IsAvailable => !_disposed && _session.IsAvailable;
        public string? PlayerId => _session.PlayerId;
        public long SessionVersion => _session.SessionVersion;
        public IEnumerator Read(string owner, long session, string installationId, Action<EconomyReply> done) => Call(owner, session,
            new JObject { ["action"] = "read", ["expectedOwnerId"] = owner, ["installationId"] = installationId }, done);
        public IEnumerator Execute(string owner, long session, string operationJson, Action<EconomyReply> done) => Call(owner, session,
            new JObject { ["action"] = "execute", ["expectedOwnerId"] = owner, ["operationJson"] = operationJson }, done);
        private IEnumerator Call(string owner, long session, JObject parameters, Action<EconomyReply> done)
        {
            if (_disposed || !EconomyProtocol.IsId(owner) || !_session.Access(owner, session, out string token))
            { done(EconomyReply.Unavailable); yield break; }
            Task<EconomyReply> request = Send(parameters, token);
            while (!request.IsCompleted) yield return null;
            // Access also verifies expiry/network/session. A → B → A is a different session.
            if (_disposed || !_session.Access(owner, session, out _) || request.IsFaulted || request.IsCanceled)
            { done(EconomyReply.Unavailable); yield break; }
            EconomyReply reply = request.Result;
            done(reply.State != null && reply.State.OwnerId != owner ? EconomyReply.Unavailable : reply);
        }
        private async Task<EconomyReply> Send(JObject parameters, string token)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(20));
                using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(CanonicalJson.Write(new JObject { ["params"] = parameters }, false), Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxResponseBytes) return EconomyReply.Unavailable;
                using Stream input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var bytes = new MemoryStream();
                var buffer = new byte[8192];
                int count;
                while ((count = await input.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                {
                    if (bytes.Length + count > MaxResponseBytes) return EconomyReply.Unavailable;
                    bytes.Write(buffer, 0, count);
                }
                JObject envelope = JsonDoc.ParseObject(new UTF8Encoding(false, true).GetString(bytes.ToArray()), "cloud code response");
                // The REST service wraps the script return value in output; the Unity SDK unwraps it for callers.
                if (!(envelope["output"] is JObject output)) return EconomyReply.Unavailable;
                return new EconomyReply(output);
            }
            catch (Exception) { return EconomyReply.Unavailable; } // no token/body/receipt logging
        }
        public void Dispose() { if (_disposed) return; _disposed = true; _lifetime.Cancel(); _http.Dispose(); _lifetime.Dispose(); }
    }
}
