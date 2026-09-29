namespace Bloomlings.Core.Hashing
{
    /// <summary>
    /// Incremental XOR hash of the logical level state. Every state change toggles the key of the old value off and
    /// the key of the new value on, so equal states always have equal hashes regardless of the path taken.
    /// </summary>
    public sealed class StateHasher
    {
        public ulong Value { get; private set; }

        public void Toggle(ulong key) => Value ^= key;

        public void Toggle(ZobristFeature feature, int a, int b = 0, int c = 0) => Value ^= ZobristKeys.Key(feature, a, b, c);

        public void Reset() => Value = 0;

        public StateHasher Clone() => new StateHasher { Value = Value };
    }
}
