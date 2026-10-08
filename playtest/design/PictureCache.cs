using System;
using System.Collections.Generic;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The painters' cache of rendered <see cref="IPainter.Picture"/> bitmaps, keyed by (key, width, height) without building
    /// a string per draw. It is bounded by bytes, least recently used first: at the start of each frame
    /// (<see cref="NextFrame"/>), while the cache holds more than its budget, the pictures that were not drawn in the last two
    /// frames are dropped, the oldest first, and handed to the dispose action. A picture drawn in this frame or the two
    /// before it is never dropped, so nothing on screen renders again mid-animation. Engine-free; one thread.
    /// </summary>
    public sealed class PictureCache<T>
        where T : class
    {
        private readonly Dictionary<(string Key, int Width, int Height), Entry> _entries = new Dictionary<(string, int, int), Entry>();
        private readonly List<KeyValuePair<(string Key, int Width, int Height), Entry>> _old = new List<KeyValuePair<(string, int, int), Entry>>();
        private readonly Action<T> _dispose;
        private long _frame;

        public PictureCache(long budgetBytes, Action<T> dispose)
        {
            BudgetBytes = budgetBytes;
            _dispose = dispose;
        }

        /// <summary>How many bytes of pictures the cache keeps across frames.</summary>
        public long BudgetBytes { get; }

        /// <summary>The bytes of all cached pictures.</summary>
        public long Bytes { get; private set; }

        public int Count => _entries.Count;

        /// <summary>How many draws found their picture cached (the harness's report).</summary>
        public long Hits { get; private set; }

        /// <summary>How many draws did not (the picture then renders and is added).</summary>
        public long Misses { get; private set; }

        /// <summary>How many pictures were dropped over budget, and their bytes.</summary>
        public long Evictions { get; private set; }

        public long EvictedBytes { get; private set; }

        /// <summary>Starts a frame: drops the least recently used pictures not drawn in the last two frames while over budget.</summary>
        public void NextFrame()
        {
            _frame++;
            if (Bytes <= BudgetBytes)
            {
                return;
            }

            _old.Clear();
            foreach (KeyValuePair<(string Key, int Width, int Height), Entry> entry in _entries)
            {
                if (entry.Value.LastFrame < _frame - 2)
                {
                    _old.Add(entry);
                }
            }

            _old.Sort((a, b) => a.Value.LastFrame.CompareTo(b.Value.LastFrame));
            foreach (KeyValuePair<(string Key, int Width, int Height), Entry> entry in _old)
            {
                if (Bytes <= BudgetBytes)
                {
                    break;
                }

                _entries.Remove(entry.Key);
                Bytes -= entry.Value.Bytes;
                Evictions++;
                EvictedBytes += entry.Value.Bytes;
                _dispose(entry.Value.Picture);
            }

            _old.Clear();
        }

        /// <summary>A cached picture, marked as drawn in this frame.</summary>
        public bool TryGet(string key, int width, int height, out T picture)
        {
            if (_entries.TryGetValue((key, width, height), out Entry? entry))
            {
                entry.LastFrame = _frame;
                picture = entry.Picture;
                Hits++;
                return true;
            }

            picture = null!;
            Misses++;
            return false;
        }

        /// <summary>Caches a picture of <paramref name="bytes"/> bytes, drawn in this frame.</summary>
        public void Add(string key, int width, int height, T picture, long bytes)
        {
            if (_entries.TryGetValue((key, width, height), out Entry? old))
            {
                Bytes -= old.Bytes;
                _dispose(old.Picture);
            }

            _entries[(key, width, height)] = new Entry(picture, bytes, _frame);
            Bytes += bytes;
        }

        private sealed class Entry
        {
            public Entry(T picture, long bytes, long frame)
            {
                Picture = picture;
                Bytes = bytes;
                LastFrame = frame;
            }

            public T Picture { get; }

            public long Bytes { get; }

            public long LastFrame { get; set; }
        }
    }
}
