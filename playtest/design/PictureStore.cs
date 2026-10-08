using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The made <see cref="IPainter.Picture"/> pixels kept on disk between launches (the APK's cache folder), so a screen a
    /// player saw before opens without making its pictures again. A picture is a pure function of its key and size, so
    /// the bytes stay right as long as the code that made them does: the host gives each build its own folder
    /// (<paramref name="fingerprint"/>, the APK's install time and version) and folders of other builds are deleted. Each
    /// file holds a small header (the key and size, checked on reading, so a hash collision never shows another picture)
    /// and the painter's bytes as it uploads them. Files are written and old ones dropped on one background thread; a
    /// read is a plain file read on the caller's thread. The folder is kept under a byte budget, the least recently used
    /// files dropped first. Every I/O failure only turns the store off (the pictures are then made as before).
    /// Engine-free.
    /// </summary>
    public sealed class PictureStore
    {
        private const uint Magic = 0x58504C42; // "BLPX"
        private const string Extension = ".px";

        private readonly string _folder;
        private readonly long _budgetBytes;
        private readonly BlockingCollection<Action> _work = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
        private long _bytes;
        private volatile bool _broken;

        /// <summary>
        /// Opens the store in <c>root/fingerprint</c> (made when missing), deletes the other folders under
        /// <paramref name="root"/> and trims this one to <paramref name="budgetBytes"/>, in the background.
        /// </summary>
        public PictureStore(string root, string fingerprint, long budgetBytes)
        {
            _folder = Path.Combine(root, Safe(fingerprint));
            _budgetBytes = budgetBytes;
            try
            {
                Directory.CreateDirectory(_folder);
            }
            catch (Exception e)
            {
                Fail(e);
                return;
            }

            var worker = new Thread(Work) { IsBackground = true, Name = "PictureStore", Priority = ThreadPriority.BelowNormal };
            worker.Start();
            Enqueue(() =>
            {
                foreach (string other in Directory.GetDirectories(root))
                {
                    if (!string.Equals(Path.GetFullPath(other), Path.GetFullPath(_folder), StringComparison.Ordinal))
                    {
                        Directory.Delete(other, recursive: true);
                    }
                }

                long bytes = 0;
                foreach (string file in Directory.GetFiles(_folder, "*" + Extension))
                {
                    bytes += new FileInfo(file).Length;
                }

                Interlocked.Add(ref _bytes, bytes);
                Trim();
            });
        }

        /// <summary>Whether the store works (false after an I/O failure: pictures are then only made, as without a store).</summary>
        public bool Enabled => !_broken;

        /// <summary>How many reads found their picture, and how many did not (the harness's report).</summary>
        public long Hits { get; private set; }

        public long Misses { get; private set; }

        /// <summary>Called with the message of the I/O failure that turned the store off (the host logs it).</summary>
        public Action<string>? Failed { get; set; }

        /// <summary>
        /// Reads the stored bytes of (<paramref name="key"/>, <paramref name="width"/>, <paramref name="height"/>) into the
        /// start of <paramref name="into"/> (at least <c>width × height × bytesPerPixel</c> long): false when it is not
        /// stored, or stored for another key or size.
        /// </summary>
        public bool TryRead(string key, int width, int height, int bytesPerPixel, byte[] into)
        {
            int length = width * height * bytesPerPixel;
            if (_broken || into.Length < length)
            {
                return false;
            }

            string path = PathOf(key, width, height);
            try
            {
                if (!File.Exists(path))
                {
                    Misses++;
                    return false;
                }

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
                using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                {
                    if (reader.ReadUInt32() != Magic || reader.ReadInt32() != width || reader.ReadInt32() != height || reader.ReadInt32() != length || reader.ReadString() != key)
                    {
                        Misses++;
                        return false;
                    }

                    int read = 0;
                    while (read < length)
                    {
                        int n = stream.Read(into, read, length - read);
                        if (n <= 0)
                        {
                            Misses++;
                            return false;
                        }

                        read += n;
                    }
                }

                Hits++;

                // Recently used: trimming drops it last.
                Enqueue(() => File.SetLastWriteTimeUtc(path, DateTime.UtcNow));
                return true;
            }
            catch (Exception e)
            {
                Fail(e);
                return false;
            }
        }

        /// <summary>
        /// Stores <paramref name="length"/> bytes of <paramref name="pixels"/> for (<paramref name="key"/>,
        /// <paramref name="width"/>, <paramref name="height"/>) in the background: the caller hands the array over and must
        /// not change it afterwards.
        /// </summary>
        public void Write(string key, int width, int height, byte[] pixels, int length)
        {
            if (_broken)
            {
                return;
            }

            string path = PathOf(key, width, height);
            Enqueue(() =>
            {
                string temp = path + ".tmp";
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024))
                using (var writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    writer.Write(Magic);
                    writer.Write(width);
                    writer.Write(height);
                    writer.Write(length);
                    writer.Write(key);
                    writer.Write(pixels, 0, length);
                }

                long size = new FileInfo(temp).Length;
                long before = File.Exists(path) ? new FileInfo(path).Length : 0;
                File.Move(temp, path, overwrite: true);
                Interlocked.Add(ref _bytes, size - before);
                Trim();
            });
        }

        /// <summary>Waits until the queued writes are done (tests and the harness), at most <paramref name="timeout"/>.</summary>
        public bool Flush(TimeSpan timeout)
        {
            using var done = new ManualResetEventSlim(false);
            Enqueue(() => done.Set());
            return done.Wait(timeout);
        }

        /// <summary>The file of a picture: a 64-bit FNV-1a hash of its key and size (the header names it exactly).</summary>
        private string PathOf(string key, int width, int height)
        {
            ulong hash = 14695981039346656037UL;
            foreach (char c in key)
            {
                hash = (hash ^ c) * 1099511628211UL;
            }

            hash = (hash ^ (uint)width) * 1099511628211UL;
            hash = (hash ^ (uint)height) * 1099511628211UL;
            return Path.Combine(_folder, hash.ToString("x16") + "-" + width + "x" + height + Extension);
        }

        /// <summary>Drops the least recently used files while the folder holds more than the budget (on the worker).</summary>
        private void Trim()
        {
            if (Interlocked.Read(ref _bytes) <= _budgetBytes)
            {
                return;
            }

            var files = new DirectoryInfo(_folder).GetFiles("*" + Extension);
            Array.Sort(files, (a, b) => a.LastWriteTimeUtc.CompareTo(b.LastWriteTimeUtc));
            foreach (FileInfo file in files)
            {
                if (Interlocked.Read(ref _bytes) <= _budgetBytes * 3 / 4)
                {
                    break;
                }

                long size = file.Length;
                file.Delete();
                Interlocked.Add(ref _bytes, -size);
            }
        }

        private void Enqueue(Action action)
        {
            if (!_broken)
            {
                _work.Add(action);
            }
        }

        private void Work()
        {
            foreach (Action action in _work.GetConsumingEnumerable())
            {
                if (_broken)
                {
                    continue;
                }

                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Fail(e);
                }
            }
        }

        private void Fail(Exception e)
        {
            if (_broken)
            {
                return;
            }

            _broken = true;
            Failed?.Invoke("The picture store is off: " + e.GetType().Name + ": " + e.Message);
        }

        private static string Safe(string name)
        {
            var b = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                b.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' ? c : '_');
            }

            return b.Length == 0 ? "build" : b.ToString();
        }
    }
}
