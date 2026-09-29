namespace Bloomlings.Core.Definitions
{
    public sealed record LockedSlotDef(int SlotIndex, string KeyId);

    /// <summary>The Waiting Buffer: always 5 slots, optionally one locked (FR-015, FR-039).</summary>
    public sealed record SlotsDef(int Count, LockedSlotDef? Locked)
    {
        public const int DefaultCount = 5;
    }
}
