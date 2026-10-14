using System;
using System.Collections.Generic;
using System.Threading;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Check
{
    internal static class PictureWorkChecks
    {
        public static IEnumerable<string> Run()
        {
            var failures = new List<string>();
            void Expect(bool condition, string message) { if (!condition) failures.Add(message); }
            bool Finished(PictureWorkQueue<object> queue) => SpinWait.SpinUntil(() => queue.Pending == 0, TimeSpan.FromSeconds(5));
            int caller = Environment.CurrentManagedThreadId, maker = caller, calls = 0;
            using var started = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            using var queue = new PictureWorkQueue<object>(8, _ => { });
            object result = new object();
            Func<object> make = () => { maker = Environment.CurrentManagedThreadId; Interlocked.Increment(ref calls); started.Set(); release.Wait(TimeSpan.FromSeconds(5)); return result; };
            queue.Request("level", 4, make);
            Expect(started.Wait(TimeSpan.FromSeconds(5)), "picture producer starts");
            queue.Request("level", 4, make);
            Expect(queue.Pending == 1 && queue.Progress < 1 && calls == 1, "pending pictures deduplicate and keep loading incomplete");
            release.Set();
            Expect(Finished(queue) && maker != caller, "rasterization finishes on a different thread");
            Expect(queue.Progress == 1 && queue.TryTake("level", out object made) && ReferenceEquals(made, result), "ready results are adopted without regeneration");
            queue.Request("level", 4, make);
            Expect(Finished(queue) && calls == 2, "an evicted picture can be prepared again");

            int disposed = 0;
            using (var bounded = new PictureWorkQueue<object>(4, _ => Interlocked.Increment(ref disposed)))
            {
                bounded.Request("first", 4, () => new object());
                Expect(Finished(bounded), "first bounded picture finishes");
                bounded.Request("second", 4, () => new object());
                Expect(Finished(bounded) && bounded.ReadyBytes == 4 && disposed == 1, "ready images obey the memory budget and dispose the oldest");
                Expect(!bounded.TryTake("first", out _) && bounded.TryTake("second", out _), "the latest prepared image survives eviction");
                bounded.Request("first", 4, () => new object());
                Expect(Finished(bounded) && bounded.TryTake("first", out _), "evicted requests are not permanently marked as warmed");
            }
            int errors = 0;
            using (var faulted = new PictureWorkQueue<object>(8, _ => { }, error: _ => Interlocked.Increment(ref errors)))
            {
                faulted.Request("broken", 4, () => throw new InvalidOperationException("fixture"));
                faulted.Request("valid", 4, () => new object());
                Expect(Finished(faulted) && errors == 1 && faulted.Failed("broken") && faulted.TryTake("valid", out _), "a failed decode is reported and does not strand later work");
            }

            var evicted = new List<object>();
            var cache = new PictureCache<object>(100, evicted.Add);
            object previous = new object(), visible = new object();
            cache.Add("old screen", 1, 1, previous, 4);
            cache.NextFrame(); cache.NextFrame(); cache.NextFrame();
            cache.Add("visible", 1, 1, visible, 4);
            cache.TrimUnused();
            Expect(evicted.Count == 1 && ReferenceEquals(evicted[0], previous) && cache.TryGet("visible", 1, 1, out _), "memory pressure releases old screens while retaining current drawings");
            return failures;
        }
    }
}
