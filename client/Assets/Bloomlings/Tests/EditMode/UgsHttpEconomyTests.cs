using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bloomlings.Client.Services.Economy.Online;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public class UgsHttpEconomyTests
    {
        private const string Project = "41bb787e-16e7-4bdc-8e56-35eecbe0d5fe";
        private static JObject Fixture()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "backend/tests/fixtures/economy-client.json"))) dir = dir.Parent;
            return JsonDoc.ParseObject(File.ReadAllText(Path.Combine(dir!.FullName, "backend/tests/fixtures/economy-client.json")), "fixture");
        }
        private sealed class Handler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnRequest = (_, __) => throw new AssertionException("unexpected request");
            public int Calls;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { Calls++; return OnRequest(request, token); }
        }
        private static UgsPlayerSession Session()
        {
            var session = new UgsPlayerSession(); session.SetNetworkAvailable(true);
            session.SetAuthenticated("alice", "dummy.player.token", DateTimeOffset.UtcNow.AddHours(1)); return session;
        }
        private static void Run(IEnumerator routine)
        {
            var until = DateTime.UtcNow.AddSeconds(5);
            try
            {
                while (routine.MoveNext())
                {
                    if (routine.Current is IEnumerator child) Run(child); else Thread.Sleep(1);
                    if (DateTime.UtcNow > until) Assert.Fail("HTTP coroutine failed to complete against the in-memory handler.");
                }
            }
            finally { (routine as IDisposable)?.Dispose(); }
        }
        private static HttpResponseMessage Response(JObject output) => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(new JObject { ["output"] = output.DeepClone() }.ToString(), Encoding.UTF8, "application/json") };
        [Test] public void NativeRestReadUsesPlayerTokenFixedEndpointAndUnwrapsCloudCodeOutput()
        {
            var session = Session(); var handler = new Handler(); JObject? sent = null;
            handler.OnRequest = async (request, _) =>
            {
                Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(request.RequestUri!.ToString(), Is.EqualTo("https://cloud-code.services.api.unity.com/v1/projects/" + Project + "/scripts/ExecuteEconomy"));
                Assert.That(request.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
                Assert.That(request.Headers.Authorization.Parameter, Is.EqualTo("dummy.player.token"));
                sent = JsonDoc.ParseObject(await request.Content!.ReadAsStringAsync(), "sent");
                return Response((JObject)Fixture()["read"]!);
            };
            using var transport = new UgsHttpEconomyTransport(Project, session, handler); EconomyReply? reply = null;
            Run(transport.Read("alice", session.SessionVersion, "install-a", value => reply = value));
            Assert.That(reply!.Ok, Is.True); Assert.That(reply.State!.Petals, Is.EqualTo(10000));
            Assert.That((string?)sent!["params"]!["expectedOwnerId"], Is.EqualTo("alice"));
            Assert.That((string?)sent["params"]!["installationId"], Is.EqualTo("install-a")); Assert.That(handler.Calls, Is.EqualTo(1));
        }
        [Test] public void ExecutePreservesExactOperationStringInsideParams()
        {
            var session = Session(); var handler = new Handler(); string original = "{ \"kind\":\"buy_cosmetic\",\"sequence\":1 }";
            handler.OnRequest = async (request, _) =>
            {
                var sent = JsonDoc.ParseObject(await request.Content!.ReadAsStringAsync(), "sent");
                Assert.That((string?)sent["params"]!["operationJson"], Is.EqualTo(original));
                return Response((JObject)Fixture()["purchase"]!);
            };
            using var transport = new UgsHttpEconomyTransport(Project, session, handler); EconomyReply? reply = null;
            Run(transport.Execute("alice", session.SessionVersion, original, value => reply = value));
            Assert.That(reply!.Consumed, Is.True); Assert.That(reply.State!.Petals, Is.EqualTo(9350)); Assert.That(handler.Calls, Is.EqualTo(1));
        }
        [TestCase(401)] [TestCase(429)] [TestCase(500)] [TestCase(302)]
        public void HttpErrorsNeverBecomeAcknowledgementsOrTriggerAutomaticRetries(int status)
        {
            var session = Session(); var handler = new Handler { OnRequest = (_, __) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)) };
            using var transport = new UgsHttpEconomyTransport(Project, session, handler); EconomyReply? reply = null;
            Run(transport.Read("alice", session.SessionVersion, "install-a", value => reply = value));
            Assert.That(reply!.Consumed, Is.False); Assert.That(reply.Retryable, Is.True); Assert.That(handler.Calls, Is.EqualTo(1));
        }
        [Test] public void OldSessionAndWrongOwnerNeverDispatch()
        {
            var session = Session(); long old = session.SessionVersion; var handler = new Handler();
            session.SignOut(); session.SetAuthenticated("alice", "new.player.token", DateTimeOffset.UtcNow.AddHours(1));
            using var transport = new UgsHttpEconomyTransport(Project, session, handler);
            Run(transport.Read("alice", old, "install-a", _ => { })); Run(transport.Read("bob", session.SessionVersion, "install-a", _ => { }));
            Assert.That(handler.Calls, Is.Zero);
        }
        [Test] public void ChangedSessionDiscardsOtherwiseSuccessfulResponse()
        {
            var session = Session(); var handler = new Handler();
            handler.OnRequest = (_, __) => { session.SignOut(); return Task.FromResult(Response((JObject)Fixture()["read"]!)); };
            using var transport = new UgsHttpEconomyTransport(Project, session, handler); EconomyReply? reply = null;
            Run(transport.Read("alice", session.SessionVersion, "install-a", value => reply = value));
            Assert.That(reply!.Ok, Is.False); Assert.That(reply.Consumed, Is.False);
        }
        [Test] public void MalformedOversizedAndWrongOwnerResponsesCannotReplaceWallet()
        {
            foreach (var content in new[] { "{}", "{\"output\":\"wrong\"}", new string('x', 1024*1024+1) })
            {
                var session = Session(); var handler = new Handler { OnRequest = (_, __) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) }) };
                using var transport = new UgsHttpEconomyTransport(Project, session, handler); EconomyReply? reply = null;
                Run(transport.Read("alice", session.SessionVersion, "install-a", value => reply = value)); Assert.That(reply!.Ok, Is.False);
            }
            var s = Session(); var wrong = (JObject)Fixture()["read"]!.DeepClone(); wrong["state"]!["ownerId"] = "bob";
            var h = new Handler { OnRequest = (_, __) => Task.FromResult(Response(wrong)) };
            using var t = new UgsHttpEconomyTransport(Project, s, h); EconomyReply? result = null;
            Run(t.Read("alice", s.SessionVersion, "install-a", value => result = value)); Assert.That(result!.State, Is.Null);
        }
        [Test] public void ExpiredOrMalformedAccessTokensAreRejectedAndNetworkLossDoesNotSwitchIdentity()
        {
            var s = Session(); long epoch = s.SessionVersion; s.SetNetworkAvailable(false);
            Assert.That(s.PlayerId, Is.EqualTo("alice")); Assert.That(s.SessionVersion, Is.EqualTo(epoch)); Assert.That(s.IsAvailable, Is.False);
            Assert.Throws<ArgumentException>(() => s.SetAuthenticated("alice", "token\n", DateTimeOffset.UtcNow.AddHours(1)));
            Assert.Throws<ArgumentException>(() => s.SetAuthenticated("alice", "token", DateTimeOffset.UtcNow.AddSeconds(-1)));
        }
    }
}
