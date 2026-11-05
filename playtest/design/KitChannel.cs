using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The water channel and the wooden shutters on a round board (rules 17 §8, spec 006 FR-069, FR-070; the owner's looks A and
    /// B of 2026-11-05), drawn from the kit's <see cref="ChannelLook"/>, <see cref="ShutterLook"/> and their pictures (the Unity
    /// client's <c>ChannelView</c> and <c>ShutterView</c> draw the same).
    /// </summary>
    public static partial class Kit
    {
        private static readonly List<(float X, float Y)> ChannelDots = new List<(float X, float Y)>();
        private static readonly List<(Box Box, bool Across)> ChannelFoamDashes = new List<(Box Box, bool Across)>();
        private static readonly List<Box> ChannelSplash = new List<Box>();
        private static readonly List<ShutterBit> ShutterBits = new List<ShutterBit>();

        /// <summary>
        /// The channel over the cubes in <paramref name="pose"/> at <paramref name="seconds"/>: the routes' dots, the chosen
        /// route's stream (whole, or band by band with its head while the water runs) and its foam, the spring with its calling
        /// ring and drop, and the exits with their rings, splash and drops.
        /// </summary>
        public static void Channel(IPainter p, ChannelLook look, ChannelPose pose, float seconds)
        {
            p.Mark(ChannelLook.Slot);
            for (int e = 0; e < look.ExitCount; e++)
            {
                float alpha = pose.DotsAlpha(e);
                if (alpha <= 0.01f)
                {
                    continue;
                }

                look.Dots(e, ChannelDots);
                p.PushAlpha(alpha);
                foreach ((float x, float y) in ChannelDots)
                {
                    p.Picture(UiRaster.ChannelDotKey, look.DotBox((x, y)), (w, h) => UiRaster.ChannelDot(Math.Min(w, h)));
                }

                p.PopAlpha();
            }

            int chosen = pose.Chosen;
            if (chosen >= 0 && pose.Run > 0f)
            {
                if (pose.Run >= 1f)
                {
                    (Box box, string key, Func<int, int, byte[]> render) = look.Stream(chosen, 0, look.Bands(chosen) - 1);
                    p.Picture(key, box, render);
                }
                else
                {
                    int bands = look.GrownBands(chosen, pose.Run);
                    for (int b = 0; b < bands; b++)
                    {
                        (Box box, string key, Func<int, int, byte[]> render) = look.Stream(chosen, b, b);
                        p.Picture(key, box, render);
                    }
                }

                look.Foam(chosen, seconds, pose.Run, ChannelFoamDashes);
                foreach ((Box box, bool across) in ChannelFoamDashes)
                {
                    p.Picture(UiRaster.ChannelFoamKey(across), box, (w, h) => UiRaster.ChannelFoam(w, h));
                }

                if (pose.Run < 1f)
                {
                    p.Picture(UiRaster.ChannelDropKey, look.Head(chosen, pose.Run), (w, h) => UiRaster.ChannelDrop(Math.Min(w, h)));
                }
            }

            // The spring, waking.
            Well(p, look.SpringBox(), pose.Awake, seconds);
            if (pose.Calling)
            {
                Box ring = look.SpringRing(pose.Pulse);
                float share = look.GlowStroke * 0.75f / Math.Max(1f, Math.Min(ring.Width, ring.Height));
                p.PushAlpha(0.35f + (0.3f * pose.Pulse));
                p.Picture(UiRaster.ChannelGlowKey(share), ring, (w, h) => UiRaster.ChannelGlowRing(w, h, share));
                p.PopAlpha();
            }

            if (pose.Awake > 0f)
            {
                Drop(p, look.Drop(seconds), pose.Awake);
            }

            // The exits.
            for (int e = 0; e < look.ExitCount; e++)
            {
                float alpha = pose.ExitAlpha(e);
                if (alpha <= 0.01f)
                {
                    continue;
                }

                if (pose.Calling)
                {
                    Box ring = look.GlowRing(e, pose.Pulse);
                    float share = look.GlowStroke / Math.Max(1f, ring.Width);
                    p.PushAlpha(RoundBoardLayout.BridgeRingAlpha(pose.Pulse));
                    p.Picture(UiRaster.ChannelGlowKey(share), ring, (w, h) => UiRaster.ChannelGlowRing(w, h, share));
                    p.PopAlpha();
                }
                else if (pose.Chosen < 0)
                {
                    p.PushAlpha(Math.Min(1f, alpha * 1.4f));
                    p.Picture(UiRaster.ChannelDashKey, look.DashRing(e), (w, h) => UiRaster.ChannelDashRing(Math.Min(w, h)));
                    p.PopAlpha();
                }

                look.Splash(e, pose.ExitOpen(e), ChannelSplash);
                foreach (Box drop in ChannelSplash)
                {
                    p.Picture(UiRaster.ChannelDropKey, drop, (w, h) => UiRaster.ChannelDrop(Math.Min(w, h)));
                }

                float scale = ChannelLook.ExitScale(pose, e);
                p.PushAlpha(alpha);
                Well(p, look.ExitBox(e, scale), pose.ExitAwake(e), seconds + (0.4f * (e + 1)));
                Drop(p, look.ExitDrop(e, seconds, scale), pose.ExitAwake(e));
                p.PopAlpha();
            }
        }

        /// <summary>The walkers riding the stream this frame, each in a bubble of its material's colour (<see cref="ChannelLook.Rider"/>).</summary>
        public static void ChannelRiders(IPainter p, ChannelLook look, IReadOnlyList<RoundWalk> walkers, long nowUs)
        {
            foreach (RoundWalk walk in walkers)
            {
                if (look.Rider(walk, nowUs, out Box bubble))
                {
                    ChannelBubble(p, bubble, walk.Material);
                }
            }
        }

        /// <summary>A rider's bubble of round material <paramref name="material"/>.</summary>
        public static void ChannelBubble(IPainter p, Box bubble, int material)
        {
            RoundMaterial m = RoundMaterial.All[Math.Max(0, material)];
            Rgba color = RoundLook.ColorOf(m);
            p.Picture(UiRaster.ChannelBubbleKey(m.Code), bubble, (w, h) => UiRaster.ChannelBubble(Math.Min(w, h), color));
        }

        // A well: still, with the awake picture over it by how awake it is.
        private static void Well(IPainter p, Box box, float awake, float seconds)
        {
            if (awake < 1f)
            {
                p.Picture(ChannelLook.SpringKey(false, 0), box, (w, h) => UiRaster.ChannelSpring(w, h, false, 0));
            }

            if (awake > 0f)
            {
                int frame = ChannelLook.RingFrame(seconds);
                p.PushAlpha(awake);
                p.Picture(ChannelLook.SpringKey(true, frame), box, (w, h) => UiRaster.ChannelSpring(w, h, true, frame));
                p.PopAlpha();
            }
        }

        // The Drop's white drop in a dark water outline.
        private static void Drop(IPainter p, Box drop, float alpha)
        {
            if (alpha <= 0.01f)
            {
                return;
            }

            p.PushAlpha(alpha);
            p.Shape("fx.droplet", drop.Inset(-Math.Max(1.5f, drop.Width * 0.12f)), C.WaterDeep);
            p.Shape("fx.droplet", drop, Rgba.White);
            p.PopAlpha();
        }

        /// <summary>
        /// A shutter's crate lid at <paramref name="open"/> of its opening (0 closed): its shadow and front edge, its slats and its
        /// side battens. Draw it after the cubes of its bottom row (<see cref="ShutterLook.BottomRow"/>).
        /// </summary>
        public static void ShutterLid(IPainter p, ShutterLook look, float open)
        {
            p.Mark(ShutterLook.Slot);
            float baseAlpha = ShutterLook.BaseAlpha(open);
            if (baseAlpha > 0f)
            {
                (Box box, string key, Func<int, int, byte[]> render) = look.Base();
                p.PushAlpha(baseAlpha);
                p.Picture(key, box, render);
                p.PopAlpha();
            }

            for (int k = 0; k < look.SlatCount; k++)
            {
                ShutterSlat slat = look.Slat(k, open);
                if (slat.Alpha <= 0f)
                {
                    continue;
                }

                int seed = slat.Seed;
                p.PushAlpha(slat.Alpha);
                p.PushRotate(slat.Degrees, slat.Box.CenterX, slat.Box.CenterY);
                p.Picture(ShutterLook.SlatKey(seed), slat.Box, (w, h) => ShutterLook.SlatPicture(w, h, seed));
                p.PopTransform();
                p.PopAlpha();
            }

            float sides = ShutterLook.SidesAlpha(open);
            if (sides > 0f)
            {
                (Box left, Box right) = look.Battens();
                p.PushAlpha(sides);
                p.Picture(UiRaster.ShutterBattenKey, left, UiRaster.ShutterBatten);
                p.Picture(UiRaster.ShutterBattenKey, right, UiRaster.ShutterBatten);
                p.PopAlpha();
            }
        }

        /// <summary>
        /// What a shutter shows over the board: the sparks of its counted cubes flying to the tag, the opening's dust and leaves,
        /// and the tag on its nail with <paramref name="left"/> still to go, swaying and popping <paramref name="sinceCount"/>
        /// seconds after the last counted cube.
        /// </summary>
        public static void ShutterOver(IPainter p, ShutterLook look, RoundBoardLayout board, RoundAnimator? a, int left, float sinceCount, float open, float seconds, bool symbols)
        {
            Rgba color = RoundLook.ColorOf(look.Material);
            if (a != null && open <= 0f)
            {
                foreach (int cell in a.ShutterCells(look.Index))
                {
                    long at = a.ClearedAt(cell);
                    if (at < 0 || at > a.Now || look.Covers(cell))
                    {
                        continue;
                    }

                    Box? spark = look.Spark(board.CellBox(cell), (a.Now - at) / 1_000_000f);
                    if (spark.HasValue)
                    {
                        char code = look.Material.Code;
                        p.Picture(ShutterLook.SparkKey(code), spark.Value, (w, h) => UiRaster.ShutterSpark(Math.Min(w, h), color));
                    }
                }
            }

            look.Dust(open, ShutterBits);
            foreach (ShutterBit bit in ShutterBits)
            {
                p.PushAlpha(bit.Alpha);
                p.Picture(UiRaster.ShutterDustKey, bit.Box, (w, h) => UiRaster.ShutterDust(Math.Min(w, h)));
                p.PopAlpha();
            }

            look.Leaves(open, ShutterBits);
            foreach (ShutterBit bit in ShutterBits)
            {
                p.PushAlpha(bit.Alpha);
                p.PushRotate(bit.Degrees, bit.Box.CenterX, bit.Box.CenterY);
                p.Shape("symbol.leaf", bit.Box, C.StoneMoss);
                p.PopTransform();
                p.PopAlpha();
            }

            float fade = ShutterLook.TagAlpha(open);
            if (fade <= 0f)
            {
                return;
            }

            (float nx, float ny) = look.Nail;
            Box tag = look.Tag;
            float scale = ShutterLook.TagScale(sinceCount, open);
            p.PushAlpha(fade);
            p.PushRotate(ShutterLook.Sway(seconds), nx, ny);
            p.Picture(UiRaster.ShutterHangerKey, look.Hanger, UiRaster.ShutterHanger);
            PlacedCube(p, Box.FromCenter(tag.CenterX, tag.CenterY, tag.Width * scale, tag.Height * scale), look.Material, left, false, symbols);
            p.PopTransform();
            p.PopAlpha();
        }
    }
}
