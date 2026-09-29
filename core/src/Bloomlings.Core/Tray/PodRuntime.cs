namespace Bloomlings.Core.Tray
{
    /// <summary>Where a pod is (data-model §2.1).</summary>
    public enum PodLocation
    {
        Tray,
        Slot,

        /// <summary>Count reached 0 and the pod left (FR-022).</summary>
        Done,

        /// <summary>Removed by Bloom Burst (FR-050).</summary>
        Removed,
    }

    /// <summary>
    /// Mutable per-pod state. It is a value type so that cloning a session copies the pod array in one step; the
    /// immutable part (variant, flags, ids) stays in the pod's <see cref="Definitions.PodDef"/>.
    /// </summary>
    public struct PodRuntime
    {
        /// <summary>Work units still to do; never negative.</summary>
        public int Remaining { get; internal set; }

        public PodLocation Location { get; internal set; }

        /// <summary>The stack the pod started in; Return puts it back there (FR-045).</summary>
        public int HomeStack { get; internal set; }

        /// <summary>The slot holding the pod, or -1.</summary>
        public int SlotIndex { get; internal set; }

        /// <summary>False only for a mystery pod that has not been committed yet (FR-039).</summary>
        public bool VariantRevealed { get; internal set; }
    }
}
