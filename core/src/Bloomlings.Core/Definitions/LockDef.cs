namespace Bloomlings.Core.Definitions
{
    public enum LockTargetKind
    {
        Pod,
        Slot,
        Special,
    }

    /// <summary>Pairs one key with one lock (FR-033).</summary>
    public sealed record LockDef(string KeyId, LockTargetKind TargetKind, string TargetId);
}
