using System;
using System.Globalization;
using Bloomlings.Client.Art.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// An sRGB color with alpha, in bytes: the engine-free color of the design kit. The Unity client converts it to
    /// <c>UnityEngine.Color</c>, the playtest to <c>Android.Graphics.Color</c> or a Skia color. Contrast uses the same
    /// WCAG formula as <see cref="InkContrast"/>.
    /// </summary>
    public readonly struct Rgba : IEquatable<Rgba>
    {
        public Rgba(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public byte R { get; }

        public byte G { get; }

        public byte B { get; }

        public byte A { get; }

        public static Rgba White => new Rgba(255, 255, 255);

        public static Rgba Black => new Rgba(0, 0, 0);

        public static Rgba Transparent => new Rgba(255, 255, 255, 0);

        /// <summary>The <c>#RRGGBB</c> or <c>#RRGGBBAA</c> form.</summary>
        public string Hex => A == 255
            ? string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", R, G, B)
            : string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", R, G, B, A);

        /// <summary>Parses <c>#RRGGBB</c> or <c>#RRGGBBAA</c>.</summary>
        public static Rgba FromHex(string hex)
        {
            string s = hex.StartsWith("#", StringComparison.Ordinal) ? hex.Substring(1) : hex;
            if (s.Length != 6 && s.Length != 8)
            {
                throw new FormatException("Expected #RRGGBB or #RRGGBBAA, got '" + hex + "'.");
            }

            byte Channel(int i) => byte.Parse(s.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new Rgba(Channel(0), Channel(1), Channel(2), s.Length == 8 ? Channel(3) : (byte)255);
        }

        /// <summary>The same color with an alpha of 0–1.</summary>
        public Rgba WithAlpha(float alpha) => new Rgba(R, G, B, ToByte(alpha * 255f));

        /// <summary>A linear blend toward <paramref name="other"/> (0 = this color, 1 = the other), alpha included.</summary>
        public Rgba Mix(Rgba other, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return new Rgba(Lerp(R, other.R, t), Lerp(G, other.G, t), Lerp(B, other.B, t), Lerp(A, other.A, t));
        }

        /// <summary>Toward white by <paramref name="amount"/> (0–1), keeping alpha.</summary>
        public Rgba Lighten(float amount) => Mix(new Rgba(255, 255, 255, A), amount);

        /// <summary>Toward black by <paramref name="amount"/> (0–1), keeping alpha.</summary>
        public Rgba Darken(float amount) => Mix(new Rgba(0, 0, 0, A), amount);

        /// <summary>
        /// The color with its HSL saturation scaled by <paramref name="factor"/> (0 grey, 1 the same), its lightness, hue
        /// and alpha kept: each channel moved toward <c>(max + min) / 2</c>. The saturation ladder's transform (spec 005
        /// FR-031, <see cref="DesignTokens.Saturation"/>; the owner's pictures get the same in tools/heroanim/saturation.mjs).
        /// </summary>
        public Rgba Saturate(float factor)
        {
            float l = (Math.Max(R, Math.Max(G, B)) + Math.Min(R, Math.Min(G, B))) / 2f;
            byte Scale(byte v) => ToByte(l + (factor * (v - l)));
            return new Rgba(Scale(R), Scale(G), Scale(B), A);
        }

        /// <summary>A grey of the same luminance (for stuck and next-in-stack looks).</summary>
        public Rgba Grey()
        {
            byte l = ToByte((0.299f * R) + (0.587f * G) + (0.114f * B));
            return new Rgba(l, l, l, A);
        }

        public double Luminance => InkContrast.RelativeLuminance(R / 255.0, G / 255.0, B / 255.0);

        /// <summary>The WCAG contrast ratio of two opaque colors.</summary>
        public static double Contrast(Rgba a, Rgba b)
        {
            double la = a.Luminance;
            double lb = b.Luminance;
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        /// <summary>This color drawn over an opaque background (alpha compositing).</summary>
        public Rgba Over(Rgba background)
        {
            float a = A / 255f;
            return new Rgba(Lerp(background.R, R, a), Lerp(background.G, G, a), Lerp(background.B, B, a));
        }

        /// <summary>The ink (symbol and count color) on this color: white or the dark ink (<see cref="InkContrast"/>).</summary>
        public Rgba Ink => InkContrast.UseDarkInk(R / 255.0, G / 255.0, B / 255.0)
            ? new Rgba((byte)Math.Round(InkContrast.DarkInk * 255), (byte)Math.Round(InkContrast.DarkInk * 255), (byte)Math.Round(InkContrast.DarkInk * 255))
            : White;

        public bool Equals(Rgba other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public override bool Equals(object? obj) => obj is Rgba other && Equals(other);

        public override int GetHashCode() => (R << 24) | (G << 16) | (B << 8) | A;

        public override string ToString() => Hex;

        private static byte Lerp(byte a, byte b, float t) => ToByte(a + ((b - a) * t));

        private static byte ToByte(float v) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v)));
    }
}
