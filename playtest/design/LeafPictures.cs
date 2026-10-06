using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The leaf clusters of the reference look baked into one straight-alpha RGBA picture each (for
    /// <see cref="IPainter.Picture"/>): the ivy at a wooden sign's ends (one picture per half-cluster, behind or in front of
    /// the plank), the win sign's flower clusters and the leaves behind the wordmark. They paint the same layers as the
    /// shapes they replace (per leaf an outline, an edge, the fill, a light side and the midribs; per flower its outline,
    /// petals and middle), each over the ones before with its color's alpha, with the anti-aliasing of
    /// <see cref="ShapeRaster.Mask(Func{float, float, float}, int, bool)"/> and the unit square fitted the same way. Each
    /// leaf's distance is evaluated once per pixel and every layer derived from it (the outlines are that shape grown),
    /// and a pixel outside a leaf's outline skips the rest of it. Engine-free.
    /// </summary>
    public static class LeafPictures
    {
        /// <summary>
        /// The ivy cluster at a sign's end (<c>ui.sign.ivy</c>), mirrored when <paramref name="flipped"/>: the leaves behind
        /// the plank when <paramref name="back"/> is true, those in front when false, all of them when null.
        /// </summary>
        public static byte[] Ivy(int width, int height, bool flipped, bool? back)
        {
            var leaves = new List<Leaf>();
            for (int i = 0; i < ShapeLibrary.IvyLeafCount; i++)
            {
                bool behind = i % 2 == 1;
                if (back.HasValue && back.Value != behind)
                {
                    continue;
                }

                leaves.Add(new Leaf(
                    ShapeLibrary.IvyLeafSdf(i, 0f, flipped),
                    ShapeLibrary.IvyVeinSdf(i, 0.018f, flipped),
                    0.055f,
                    C.IvyLine.WithAlpha(0.5f),
                    0.025f,
                    C.IvyLeaf.Darken(0.35f).WithAlpha(0.7f),
                    GardenLook.IvyShade(i),
                    (0.09f, -0.1f, 0.1f),
                    C.IvyLeaf.Lighten(0.3f).WithAlpha(0.5f),
                    0.03f,
                    C.IvyLeaf.Darken(0.35f).WithAlpha(0.45f)));
            }

            return Bake(width, height, leaves, Array.Empty<Flower>());
        }

        /// <summary>
        /// The lush cluster at the win sign's ends (<c>ui.deco.garden</c>): five leaves in three greens with their midribs
        /// and two white flowers with yellow middles over them, turned half way when <paramref name="flipped"/>.
        /// </summary>
        public static byte[] FlowerCluster(int width, int height, bool flipped)
        {
            var leaves = new List<Leaf>();
            for (int i = 0; i < ShapeLibrary.FlowerClusterLeafCount; i++)
            {
                leaves.Add(ClusterLeaf(i, ShapeLibrary.ClusterLeafSdf(i, 0f, flipped), ShapeLibrary.ClusterVeinSdf(i, 0.022f, flipped)));
            }

            var flowers = new List<Flower>();
            for (int i = 0; i < ShapeLibrary.FlowerClusterFlowerCount; i++)
            {
                flowers.Add(new Flower(ShapeLibrary.ClusterFlowerSdf(i, false, 0f, flipped), ShapeLibrary.ClusterFlowerSdf(i, true, 0f, flipped)));
            }

            return Bake(width, height, leaves, flowers);
        }

        /// <summary>The broad leaves behind one end of the wordmark: the flower cluster's leaves fanned to the left, or mirrored.</summary>
        public static byte[] LogoLeaves(int width, int height, bool mirrored)
        {
            float m = mirrored ? -1f : 1f;
            var leaves = new List<Leaf>();
            for (int i = 0; i < ShapeLibrary.FlowerClusterLeafCount; i++)
            {
                Func<float, float, float> shape = ShapeLibrary.ClusterLeafSdf(i, 0f, false);
                Func<float, float, float> rib = ShapeLibrary.ClusterVeinSdf(i, 0.022f, false);
                leaves.Add(ClusterLeaf(i, (x, y) => shape(m * x, y), (x, y) => rib(m * x, y)));
            }

            return Bake(width, height, leaves, Array.Empty<Flower>());
        }

        private static Leaf ClusterLeaf(int i, Func<float, float, float> leaf, Func<float, float, float> vein)
        {
            Rgba[] greens = { C.GardenLeaf1, C.GardenLeaf3, C.GardenLeaf2 };
            return new Leaf(
                leaf,
                vein,
                0.04f,
                C.GardenLeafLine,
                null,
                default,
                greens[i % greens.Length],
                (0.05f, -0.06f, 0.08f),
                C.GardenLeaf2.Lighten(0.35f).WithAlpha(0.4f),
                0.04f,
                C.IvyLine.WithAlpha(0.55f));
        }

        /// <summary>
        /// Paints the leaves, then the flowers, back to front, into straight-alpha RGBA rows from the top. The picture is
        /// painted in blocks of 8 × 8 pixels, and a leaf or flower whose outline is farther from a block's middle than the
        /// block reaches is left out of that block (all these distances are exact or lower bounds, so no pixel it covers is
        /// missed).
        /// </summary>
        private static byte[] Bake(int width, int height, IReadOnlyList<Leaf> leaves, IReadOnlyList<Flower> flowers)
        {
            const int Block = 8;
            var rgba = new byte[width * height * 4];
            // As ShapeRaster.Mask: the unit square, a little enlarged by its margin, fills the picture; y points up.
            int size = Math.Max(1, Math.Min(width, height));
            float pixel = 2f / size;
            float half = pixel * 0.5f;
            float stepX = 2f / width * ShapeRaster.Margin;
            float stepY = 2f / height * ShapeRaster.Margin;
            var leafOn = new bool[leaves.Count];
            var flowerOn = new bool[flowers.Count];
            for (int top = 0; top < height; top += Block)
            {
                int rows = Math.Min(Block, height - top);
                for (int left = 0; left < width; left += Block)
                {
                    int cols = Math.Min(Block, width - left);
                    float midX = X(left + ((cols - 1) / 2f), width);
                    float midY = Y(top + ((rows - 1) / 2f), height);
                    float reach = 0.5f * (float)Math.Sqrt(((cols - 1) * stepX * (cols - 1) * stepX) + ((rows - 1) * stepY * (rows - 1) * stepY));
                    bool any = false;
                    for (int i = 0; i < leaves.Count; i++)
                    {
                        leafOn[i] = leaves[i].Shape(midX, midY) - leaves[i].LineGrow - reach < half;
                        any |= leafOn[i];
                    }

                    for (int i = 0; i < flowers.Count; i++)
                    {
                        flowerOn[i] = flowers[i].Petals(midX, midY) - 0.035f - reach < half;
                        any |= flowerOn[i];
                    }

                    if (!any)
                    {
                        continue;
                    }

                    for (int row = top; row < top + rows; row++)
                    {
                        float y = Y(row, height);
                        for (int col = left; col < left + cols; col++)
                        {
                            float x = X(col, width);
                            var paint = default(Paint);
                            for (int i = 0; i < leaves.Count; i++)
                            {
                                if (leafOn[i])
                                {
                                    PaintLeaf(ref paint, leaves[i], x, y, pixel, half);
                                }
                            }

                            for (int i = 0; i < flowers.Count; i++)
                            {
                                if (flowerOn[i])
                                {
                                    PaintFlower(ref paint, flowers[i], x, y, pixel, half);
                                }
                            }

                            paint.Write(rgba, ((row * width) + col) * 4);
                        }
                    }
                }
            }

            return rgba;
        }

        /// <summary>A column's x in shape units (its pixel's middle), as <see cref="ShapeRaster"/> samples it.</summary>
        private static float X(float col, int width) => (((col + 0.5f) * (2f / width)) - 1f) * ShapeRaster.Margin;

        /// <summary>A row's y in shape units (rows from the top, y up).</summary>
        private static float Y(float row, int height) => ((((height - 1 - row) + 0.5f) * (2f / height)) - 1f) * ShapeRaster.Margin;

        /// <summary>One leaf's layers over a pixel: its outline, edge and fill, then its light side and midribs inside it.</summary>
        private static void PaintLeaf(ref Paint paint, Leaf leaf, float x, float y, float pixel, float half)
        {
            float d = leaf.Shape(x, y);
            if (d - leaf.LineGrow >= half)
            {
                // Outside this leaf's outline: none of its layers covers the pixel.
                return;
            }

            paint.Over(Cover(d - leaf.LineGrow, pixel), leaf.Line);
            if (leaf.EdgeGrow.HasValue)
            {
                paint.Over(Cover(d - leaf.EdgeGrow.Value, pixel), leaf.Edge);
            }

            paint.Over(Cover(d, pixel), leaf.Fill);
            if (d + Math.Min(0.03f, leaf.VeinInset) < half)
            {
                // The light side and the midribs stay inside the leaf.
                float light = Math.Max(leaf.Shape(x + leaf.Light.Dx, y + leaf.Light.Dy) + leaf.Light.Lift, d + 0.03f);
                paint.Over(Cover(light, pixel), leaf.LightColor);
                float vein = Math.Max(leaf.Vein(x, y), d + leaf.VeinInset);
                paint.Over(Cover(vein, pixel), leaf.VeinColor);
            }
        }

        /// <summary>One flower's layers over a pixel: its outline and petals, then its middle's ring and the middle.</summary>
        private static void PaintFlower(ref Paint paint, Flower flower, float x, float y, float pixel, float half)
        {
            float d = flower.Petals(x, y);
            if (d - 0.035f >= half)
            {
                return;
            }

            paint.Over(Cover(d - 0.035f, pixel), C.GardenFlowerLine);
            paint.Over(Cover(d, pixel), C.GardenFlower);
            float c = flower.Middle(x, y);
            paint.Over(Cover(c - 0.025f, pixel), C.GardenFlowerCenterLine);
            paint.Over(Cover(c, pixel), C.GardenFlowerCenter);
        }

        /// <summary>The anti-aliased coverage of a distance, as <see cref="ShapeRaster"/> computes it.</summary>
        private static float Cover(float d, float pixel) => Math.Max(0f, Math.Min(1f, 0.5f - (d / pixel)));

        /// <summary>One leaf's distance function and how its layers are drawn from it.</summary>
        private sealed class Leaf
        {
            public Leaf(Func<float, float, float> shape, Func<float, float, float> vein, float lineGrow, Rgba line, float? edgeGrow, Rgba edge, Rgba fill, (float Dx, float Dy, float Lift) light, Rgba lightColor, float veinInset, Rgba veinColor)
            {
                Shape = shape;
                Vein = vein;
                LineGrow = lineGrow;
                Line = line;
                EdgeGrow = edgeGrow;
                Edge = edge;
                Fill = fill;
                Light = light;
                LightColor = lightColor;
                VeinInset = veinInset;
                VeinColor = veinColor;
            }

            public Func<float, float, float> Shape { get; }

            /// <summary>The midribs, already their width (a distance below 0 is on a rib).</summary>
            public Func<float, float, float> Vein { get; }

            public float LineGrow { get; }

            public Rgba Line { get; }

            public float? EdgeGrow { get; }

            public Rgba Edge { get; }

            public Rgba Fill { get; }

            /// <summary>The light side: the leaf moved by (Dx, Dy) and shrunk by Lift, kept inside the leaf.</summary>
            public (float Dx, float Dy, float Lift) Light { get; }

            public Rgba LightColor { get; }

            /// <summary>How far inside the leaf's edge the midribs stop.</summary>
            public float VeinInset { get; }

            public Rgba VeinColor { get; }
        }

        private readonly struct Flower
        {
            public Flower(Func<float, float, float> petals, Func<float, float, float> middle)
            {
                Petals = petals;
                Middle = middle;
            }

            public Func<float, float, float> Petals { get; }

            public Func<float, float, float> Middle { get; }
        }

        /// <summary>A pixel being painted, in premultiplied floats.</summary>
        private struct Paint
        {
            private float _r;
            private float _g;
            private float _b;
            private float _a;

            /// <summary>Paints <paramref name="color"/> over the pixel where it covers <paramref name="cover"/> of it.</summary>
            public void Over(float cover, Rgba color)
            {
                float a = cover * (color.A / 255f);
                if (a <= 0f)
                {
                    return;
                }

                float keep = 1f - a;
                _r = (color.R * a) + (_r * keep);
                _g = (color.G * a) + (_g * keep);
                _b = (color.B * a) + (_b * keep);
                _a = a + (_a * keep);
            }

            /// <summary>Writes the pixel as straight-alpha bytes.</summary>
            public void Write(byte[] rgba, int at)
            {
                if (_a <= 0f)
                {
                    return;
                }

                rgba[at] = Channel(_r / _a);
                rgba[at + 1] = Channel(_g / _a);
                rgba[at + 2] = Channel(_b / _a);
                rgba[at + 3] = Channel(_a * 255f);
            }

            private static byte Channel(float v) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v)));
        }
    }
}
