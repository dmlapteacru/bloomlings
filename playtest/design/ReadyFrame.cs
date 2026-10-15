using System;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// Publishes complete display lists only. While resources arrive, the last complete frame stays alive;
    /// no extra full-screen RGBA bitmap is needed. Cache ages must advance only after a successful publish.
    /// </summary>
    public sealed class ReadyFrame<T> : IDisposable where T : class
    {
        private readonly Action<T> _dispose;
        private int _missing;
        public ReadyFrame(Action<T> dispose) => _dispose = dispose;
        public T? Current { get; private set; }
        public bool Waiting { get; private set; }
        public int MissingCount => _missing;
        public void Begin() => _missing = 0;
        public void Missing() => _missing++;
        public bool Publish(T candidate)
        {
            Waiting = _missing != 0;
            if (Waiting) { _dispose(candidate); return false; }
            T? previous = Current;
            Current = candidate;
            if (previous != null) _dispose(previous);
            return true;
        }
        public void Dispose()
        {
            if (Current != null) _dispose(Current);
            Current = null;
            Waiting = false;
        }
    }
}
