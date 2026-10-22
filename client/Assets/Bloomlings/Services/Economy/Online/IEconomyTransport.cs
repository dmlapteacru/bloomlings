using System;
using System.Collections;

namespace Bloomlings.Client.Services.Economy.Online
{
    /// <summary>Methods never throw; a session change must fail before dispatch and discard late responses.</summary>
    public interface IEconomyTransport
    {
        bool IsAvailable { get; }
        string? PlayerId { get; }
        long SessionVersion { get; }
        IEnumerator Read(string owner, long session, string installationId, Action<EconomyReply> done);
        IEnumerator Execute(string owner, long session, string operationJson, Action<EconomyReply> done);
    }
    public sealed class OfflineEconomyTransport : IEconomyTransport
    {
        public bool IsAvailable => false;
        public string? PlayerId => null;
        public long SessionVersion => 0;
        public IEnumerator Read(string owner, long session, string installationId, Action<EconomyReply> done) { done(EconomyReply.Unavailable); yield break; }
        public IEnumerator Execute(string owner, long session, string operationJson, Action<EconomyReply> done) { done(EconomyReply.Unavailable); yield break; }
    }
}
