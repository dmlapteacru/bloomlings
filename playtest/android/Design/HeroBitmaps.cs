using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Android.Graphics;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Droid
{
    /// <summary>
    /// The animated heroes' frames on screen (spec 005 FR-028), expanded from their palette pictures
    /// (<see cref="HeroFrameStore"/>) into a few reused ARGB_8888 bitmaps, all as large as the largest frame (each frame is
    /// drawn from its bitmap's top-left corner, the rest clear). A frame drawn in this frame or the two before it keeps its
    /// bitmap, as <see cref="PictureCache{T}"/> keeps its pictures (a hardware canvas may still hold a recent frame's
    /// draws); any other bitmap takes the next frame asked for, the least recently drawn first. Past the budget, bitmaps
    /// left unused are disposed, so Home's four heroes animate on about eight bitmaps without allocating per frame.
    /// One thread (the UI thread).
    /// </summary>
    internal sealed class HeroBitmaps
    {
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly long _budget;
        private readonly int _width;
        private readonly int _height;
        private long _frame;
        private byte[] _pixels = Array.Empty<byte>();
        private Java.Nio.ByteBuffer? _buffer;
        private IntPtr _address;
        private int _bufferBytes;

        public HeroBitmaps(long budgetBytes)
        {
            _budget = budgetBytes;
            (_width, _height) = LargestFrame();
        }

        /// <summary>The bytes of all bitmaps held.</summary>
        public long Bytes { get; private set; }

        /// <summary>Starts a frame: disposes bitmaps not drawn in the last two frames, the oldest first, while over budget.</summary>
        public void NextFrame()
        {
            _frame++;
            while (Bytes > _budget)
            {
                Entry? oldest = null;
                foreach (Entry entry in _entries)
                {
                    if (entry.LastFrame < _frame - 2 && (oldest == null || entry.LastFrame < oldest.LastFrame))
                    {
                        oldest = entry;
                    }
                }

                if (oldest == null)
                {
                    return;
                }

                _entries.Remove(oldest);
                Bytes -= oldest.Bitmap.ByteCount;
                oldest.Bitmap.Dispose();
            }
        }

        /// <summary>The bitmap holding the frame <paramref name="name"/> in its top-left corner, expanded now when it is not held.</summary>
        public Bitmap Get(string name, PalettePicture picture)
        {
            Entry? free = null;
            foreach (Entry entry in _entries)
            {
                if (entry.Name == name)
                {
                    entry.LastFrame = _frame;
                    return entry.Bitmap;
                }

                bool fits = entry.Bitmap.Width >= picture.Width && entry.Bitmap.Height >= picture.Height;
                if (fits && entry.LastFrame < _frame - 2 && (free == null || entry.LastFrame < free.LastFrame))
                {
                    free = entry;
                }
            }

            if (free == null)
            {
                Bitmap bitmap = Bitmap.CreateBitmap(Math.Max(_width, picture.Width), Math.Max(_height, picture.Height), Bitmap.Config.Argb8888!)!;
                free = new Entry(bitmap);
                _entries.Add(free);
                Bytes += bitmap.ByteCount;
            }

            Fill(free.Bitmap, picture);
            free.Name = name;
            free.LastFrame = _frame;
            return free.Bitmap;
        }

        // The frame's premultiplied pixels into the bitmap's top-left corner, the rest clear, through one direct buffer
        // (no Java array per frame).
        private void Fill(Bitmap bitmap, PalettePicture picture)
        {
            int bytes = bitmap.ByteCount;
            Java.Nio.ByteBuffer? buffer = _buffer;
            if (buffer == null || _bufferBytes < bytes)
            {
                buffer?.Dispose();
                buffer = Java.Nio.ByteBuffer.AllocateDirect(bytes)!;
                _buffer = buffer;
                _address = Android.Runtime.JNIEnv.GetDirectBufferAddress(buffer.Handle);
                _bufferBytes = bytes;
                _pixels = new byte[bytes];
            }

            Array.Clear(_pixels, 0, bytes);
            picture.Expand(_pixels, bitmap.RowBytes / 4, premultiplied: true);
            Marshal.Copy(_pixels, 0, _address, bytes);
            buffer.Rewind();
            bitmap.CopyPixelsFromBuffer(buffer);
        }

        /// <summary>The largest frame width and height of the baked clips (<see cref="HeroMotion"/>).</summary>
        private static (int Width, int Height) LargestFrame()
        {
            int width = 1;
            int height = 1;
            foreach (Family family in CharacterArt.Families)
            {
                foreach (MotionClip clip in HeroMotion.Clips)
                {
                    for (int i = 0; i < HeroMotion.FrameCount(family, clip); i++)
                    {
                        HeroFrame frame = HeroMotion.Frame(family, clip, i);
                        width = Math.Max(width, frame.Width);
                        height = Math.Max(height, frame.Height);
                    }
                }
            }

            return (width, height);
        }

        private sealed class Entry
        {
            public Entry(Bitmap bitmap) => Bitmap = bitmap;

            public Bitmap Bitmap { get; }

            public string? Name { get; set; }

            public long LastFrame { get; set; } = long.MinValue / 2;
        }
    }
}
