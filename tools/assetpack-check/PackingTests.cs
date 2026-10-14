using System;
using System.Buffers.Binary;
using System.IO;
using Bloomlings.Playtest.Design;
using NUnit.Framework;

public sealed class PackingTests
{
    [Test]
    public void IndexedFrame_RoundTripsEveryPaletteEntryAndAlpha()
    {
        var palette = new byte[1024];
        var indices = new byte[384 * 384];
        for (int i = 0; i < palette.Length; i++) palette[i] = (byte)(i * 37);
        for (int i = 0; i < indices.Length; i++) indices[i] = (byte)i;
        byte[] bytes = PackedPalette.Encode(384, 384, palette, indices);
        Assert.That(PackedPalette.TryDecode(bytes, out int w, out int h, out byte[] colors, out byte[] pixels), Is.True);
        Assert.That((w, h), Is.EqualTo((384, 384)));
        Assert.That(colors, Is.EqualTo(palette));
        Assert.That(pixels, Is.EqualTo(indices));
    }

    [Test]
    public void OriginalPng_IsLeftForThePngDecoder()
    {
        Assert.That(PackedPalette.TryDecode(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, out _, out _, out _, out _), Is.False);
    }

    [Test]
    public void TruncatedFrame_FailsInsteadOfDrawingPartialPixels()
    {
        byte[] bytes = PackedPalette.Encode(64, 64, new byte[1024], new byte[4096]);
        Assert.Throws<EndOfStreamException>(() => PackedPalette.TryDecode(bytes[..13], out _, out _, out _, out _));
    }

    [Test]
    public void OversizedFrame_IsRejectedBeforeAllocatingPixels()
    {
        byte[] bytes = PackedPalette.Encode(1, 1, new byte[1024], new byte[1]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), int.MaxValue);
        Assert.Throws<InvalidDataException>(() => PackedPalette.TryDecode(bytes, out _, out _, out _, out _));
    }

    [Test]
    public void Cache_EvictsOldScreensButProtectsCurrentFrame()
    {
        int disposed = 0;
        var cache = new PictureCache<object>(10, _ => disposed++);
        object old = new(), current = new();
        cache.Add("old", 0, 0, old, 9);
        cache.NextFrame();
        cache.Add("current", 0, 0, current, 9);
        cache.NextFrame();
        Assert.That(disposed, Is.Zero);
        cache.NextFrame();
        Assert.That(cache.TryGet("current", 0, 0, out object kept), Is.True);
        Assert.That(kept, Is.SameAs(current));
        Assert.That(cache.Bytes, Is.EqualTo(9));
        Assert.That(disposed, Is.EqualTo(1));
        cache.NextFrame();
        Assert.That(disposed, Is.EqualTo(1));
    }
}
