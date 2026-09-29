using Bloomlings.Core.Random;

namespace Bloomlings.Core.Hashing
{
    /// <summary>The kinds of state that contribute to the logical state hash.</summary>
    public enum ZobristFeature
    {
        /// <summary>(cell index, depth from the original top, variant catalog index) is still present.</summary>
        CellLayer = 1,

        /// <summary>(cell index) is open ground.</summary>
        CellOpen = 2,

        /// <summary>(cell index) still hides its mystery variant.</summary>
        CellMysteryHidden = 3,

        /// <summary>(pod index, remaining count).</summary>
        PodRemaining = 4,

        /// <summary>(pod index, location code, stack or slot index).</summary>
        PodLocation = 5,

        /// <summary>(pod index, depth in its stack).</summary>
        TrayDepth = 6,

        /// <summary>(slot index, state code).</summary>
        SlotState = 7,

        /// <summary>(key index) collected.</summary>
        KeyCollected = 8,

        /// <summary>(special index, progress, open flag).</summary>
        SpecialState = 9,

        /// <summary>Extra Slot already used this level.</summary>
        ExtraSlotUsed = 10,

        /// <summary>(shuffle uses).</summary>
        ShuffleUses = 11,

        /// <summary>(pod index) mystery variant revealed.</summary>
        PodRevealed = 12,

        /// <summary>(slot index, state code, age rank + 1). Recomputed from the at most 6 slots on demand.</summary>
        SlotOrder = 13,
    }

    /// <summary>
    /// Zobrist keys for the incremental state hash (research R3, R8). Instead of large random tables, every key is
    /// derived with the SplitMix64 mixer from the fixed seed <see cref="Seed"/>, the feature and its arguments.
    /// This is deterministic on every platform and needs no memory.
    /// </summary>
    public static class ZobristKeys
    {
        public const ulong Seed = 0xB100B100B100B100UL;

        public static ulong Key(ZobristFeature feature, int a, int b = 0, int c = 0)
        {
            unchecked
            {
                ulong x = Seed ^ ((ulong)feature * 0x9E3779B97F4A7C15UL);
                x = SplitMix64.Mix(x + ((ulong)(uint)a * 0xD1B54A32D192ED03UL));
                x = SplitMix64.Mix(x + ((ulong)(uint)b * 0xABC98388FB8FAC03UL));
                x = SplitMix64.Mix(x + ((ulong)(uint)c * 0x8CB92BA72F3D8DD7UL));
                return x;
            }
        }
    }
}
