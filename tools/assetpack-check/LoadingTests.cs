using System;
using System.Collections.Generic;
using System.Threading;
using Bloomlings.Playtest.Design;
using NUnit.Framework;

public sealed class LoadingTests
{
    [Test]
    public void WorkerStillHandlesEvictionFaultsAndDuplicateOwnership()
        => Assert.That(Bloomlings.Playtest.Check.PictureWorkChecks.Run(), Is.Empty);

    [Test]
    public void WaitingForAnotherScreenRetainsItsPreviousBitmapsUntilPublish()
    {
        var disposed = new List<object>();
        var cache = new PictureCache<object>(8, disposed.Add);
        using var frames = new ReadyFrame<object>(disposed.Add);
        object home = new(), shop = new(), homeBitmap = new(), shopBitmap = new();
        cache.Add("home", 0, 0, homeBitmap, 8);
        frames.Begin(); Assert.That(frames.Publish(home), Is.True); cache.NextFrame();
        cache.Add("shop", 0, 0, shopBitmap, 8);
        // A slow cold load can last hundreds of display frames. Those must not age out the retained page.
        for (int i = 0; i < 200; i++)
        {
            frames.Begin(); frames.Missing();
            if (frames.Publish(new object())) cache.NextFrame();
            Assert.That(frames.Current, Is.SameAs(home));
            Assert.That(disposed, Does.Not.Contain(homeBitmap));
        }
        frames.Begin(); Assert.That(frames.Publish(shop), Is.True); cache.NextFrame();
        Assert.That(frames.Current, Is.SameAs(shop));
        Assert.That(disposed, Does.Contain(home));
        cache.TryGet("shop", 0, 0, out _); cache.NextFrame();
        Assert.That(disposed, Does.Contain(homeBitmap));
        Assert.That(disposed, Does.Not.Contain(shopBitmap));
    }

    [Test]
    public void VisibleRequestsOvertakeIdleWorkWithoutDuplicatingQueuedDecode()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var order = new List<string>();
        using var queue = new PictureWorkQueue<object>(64, _ => { });
        queue.Request("running", 1, () => { started.Set(); release.Wait(TimeSpan.FromSeconds(5)); return new object(); });
        Assert.That(started.Wait(TimeSpan.FromSeconds(5)), Is.True);
        queue.Request("idle", 1, () => { lock (order) order.Add("idle"); return new object(); });
        queue.Request("shop", 1, () => { lock (order) order.Add("shop"); return new object(); });
        Assert.That(queue.Request("shop", 1, () => throw new Exception("duplicate"), urgent: true), Is.False);
        queue.Request("visible", 1, () => { lock (order) order.Add("visible"); return new object(); }, urgent: true);
        release.Set();
        Assert.That(SpinWait.SpinUntil(() => queue.Pending == 0, TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(order, Is.EqualTo(new[] { "shop", "visible", "idle" }));
        Assert.That(queue.Requested, Is.EqualTo(4));
    }
}
