using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Bloomlings.Playtest.Design
{
    /// <summary>One low-priority producer, deduplicated requests and bounded ready results. Renderers never run on the caller.</summary>
    public sealed class PictureWorkQueue<T> : IDisposable where T : class
    {
        private readonly object _gate = new object();
        private readonly BlockingCollection<(string Key, long Bytes, Func<T> Make)> _jobs = new BlockingCollection<(string, long, Func<T>)>();
        private readonly HashSet<string> _pending = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _failed = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, (T Value, long Bytes, LinkedListNode<string> Node)> _ready = new Dictionary<string, (T, long, LinkedListNode<string>)>(StringComparer.Ordinal);
        private readonly LinkedList<string> _order = new LinkedList<string>();
        private readonly Action<T> _dispose;
        private readonly Action? _completed;
        private readonly Action<Exception>? _error;
        private readonly long _budget;
        private readonly Func<T, long>? _measure;
        private bool _closed;
        private long _bytes;
        private int _total, _done;
        private Thread? _worker;
        public PictureWorkQueue(long budget, Action<T> dispose, Action? completed = null, Action<Exception>? error = null, Func<T, long>? measure = null)
        { _budget = budget; _dispose = dispose; _completed = completed; _error = error; _measure = measure; }
        public int Pending { get { lock (_gate) return _pending.Count; } }
        public int Requested { get { lock (_gate) return _total; } }
        public bool Contains(string key) { lock (_gate) return _pending.Contains(key) || _ready.ContainsKey(key) || _failed.Contains(key); }
        public long ReadyBytes { get { lock (_gate) return _bytes; } }
        public float Progress { get { lock (_gate) return _total == 0 ? 1f : (float)_done / _total; } }
        public bool Failed(string key) { lock (_gate) return _failed.Contains(key); }
        public bool TryTake(string key, out T value)
        {
            lock (_gate)
            {
                if (_ready.TryGetValue(key, out var entry))
                {
                    _ready.Remove(key); _order.Remove(entry.Node); _bytes -= entry.Bytes; value = entry.Value; return true;
                }
                value = null!; return false;
            }
        }
        public bool Request(string key, long bytes, Func<T> make)
        {
            lock (_gate)
            {
                if (_closed || _ready.ContainsKey(key) || _failed.Contains(key) || !_pending.Add(key)) return false;
                _total++; _jobs.Add((key, bytes, make));
                if (_worker == null)
                {
                    _worker = new Thread(Work) { IsBackground = true, Name = "GamePictures", Priority = ThreadPriority.BelowNormal };
                    _worker.Start();
                }
                return true;
            }
        }
        private void Work()
        {
            foreach (var job in _jobs.GetConsumingEnumerable())
            {
                lock (_gate) { if (_closed) break; }
                T? made = null;
                try
                {
                    made = job.Make();
                    long bytes = _measure?.Invoke(made) ?? job.Bytes;
                    lock (_gate)
                    {
                        if (!_closed)
                        {
                            // A single large image may exceed the budget; other ready results make way for it.
                            while (_order.First != null && _bytes + bytes > _budget)
                            {
                                string oldest = _order.First.Value; var old = _ready[oldest];
                                _ready.Remove(oldest); _order.RemoveFirst(); _bytes -= old.Bytes; _dispose(old.Value);
                            }
                            _ready[job.Key] = (made, bytes, _order.AddLast(job.Key)); _bytes += bytes; made = null;
                        }
                    }
                }
                catch (Exception e) { lock (_gate) _failed.Add(job.Key); _error?.Invoke(e); }
                finally
                {
                    if (made != null) _dispose(made);
                    lock (_gate) { _pending.Remove(job.Key); _done++; }
                    _completed?.Invoke();
                }
            }
        }
        public void Dispose()
        {
            lock (_gate)
            {
                if (_closed) return;
                _closed = true; _jobs.CompleteAdding();
                foreach (var entry in _ready.Values) _dispose(entry.Value);
                _ready.Clear(); _order.Clear(); _bytes = 0;
            }
        }
    }
}
