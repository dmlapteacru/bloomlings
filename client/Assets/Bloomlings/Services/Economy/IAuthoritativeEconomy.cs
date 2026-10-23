using System;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Services.Economy
{
    /// <summary>Explicit server mode. A local grant/spend must never be used as a fallback from this authority.</summary>
    public interface IAuthoritativeEconomy
    {
        string OwnerId { get; }
        bool IsOnline { get; }
        int Petals { get; }
        int Charges(BoosterKind kind);
        int Price(BoosterKind kind);
        int ItemPrice(string id);
        string? UtcDay { get; }
        int DailyClaimed { get; }
        bool CanClaimDaily { get; }
        void Bind(PlayerSave save, Action persist);
        bool TryTakeCharge(BoosterKind kind);
        bool RequestDaily(int step);
        bool RecordWin(string claimId, int level, string evidenceJson, bool challenge = false);
        void Synchronize();
    }
}
