using System;

namespace Bloomlings.Pipeline.Readability
{
    public enum Vision
    {
        Normal,
        Protanopia,
        Deuteranopia,
        Tritanopia,
    }

    /// <summary>A CIE L*a*b* color (D65).</summary>
    public readonly record struct Lab(double L, double A, double B);

    /// <summary>
    /// Color math for the readability check (T075): sRGB to CIE Lab (D65), CIEDE2000 distance (Sharma, Wu and Dalal
    /// 2005), and color vision deficiency simulation with the Machado, Oliveira and Fernandes (2009) matrices at full
    /// severity, applied in linear RGB. This is a tools-only module; floating point is fine outside the game rules.
    /// </summary>
    public static class ColorScience
    {
        private static readonly double[,] Protan =
        {
            { 0.152286, 1.052583, -0.204868 },
            { 0.114503, 0.786281, 0.099216 },
            { -0.003882, -0.048116, 1.051998 },
        };

        private static readonly double[,] Deutan =
        {
            { 0.367322, 0.860646, -0.227968 },
            { 0.280085, 0.672501, 0.047413 },
            { -0.011820, 0.042940, 0.968881 },
        };

        private static readonly double[,] Tritan =
        {
            { 1.255528, -0.076749, -0.178779 },
            { -0.078411, 0.930809, 0.147602 },
            { 0.004733, 0.691367, 0.303900 },
        };

        public static (double R, double G, double B) ParseHex(string hex)
        {
            string h = hex.TrimStart('#');
            return (Convert.ToInt32(h.Substring(0, 2), 16) / 255.0, Convert.ToInt32(h.Substring(2, 2), 16) / 255.0, Convert.ToInt32(h.Substring(4, 2), 16) / 255.0);
        }

        /// <summary>The color as seen with the given vision, in sRGB 0–1.</summary>
        public static (double R, double G, double B) Simulate((double R, double G, double B) srgb, Vision vision)
        {
            if (vision == Vision.Normal)
            {
                return srgb;
            }

            double[,] m = vision == Vision.Protanopia ? Protan : vision == Vision.Deuteranopia ? Deutan : Tritan;
            double r = ToLinear(srgb.R);
            double g = ToLinear(srgb.G);
            double b = ToLinear(srgb.B);
            return (
                ToSrgb(Clamp((m[0, 0] * r) + (m[0, 1] * g) + (m[0, 2] * b))),
                ToSrgb(Clamp((m[1, 0] * r) + (m[1, 1] * g) + (m[1, 2] * b))),
                ToSrgb(Clamp((m[2, 0] * r) + (m[2, 1] * g) + (m[2, 2] * b))));
        }

        public static Lab ToLab((double R, double G, double B) srgb)
        {
            double r = ToLinear(srgb.R);
            double g = ToLinear(srgb.G);
            double b = ToLinear(srgb.B);
            double x = ((0.4124564 * r) + (0.3575761 * g) + (0.1804375 * b)) / 0.95047;
            double y = (0.2126729 * r) + (0.7151522 * g) + (0.0721750 * b);
            double z = ((0.0193339 * r) + (0.1191920 * g) + (0.9503041 * b)) / 1.08883;
            double fx = F(x);
            double fy = F(y);
            double fz = F(z);
            return new Lab((116 * fy) - 16, 500 * (fx - fy), 200 * (fy - fz));
        }

        /// <summary>Relative luminance (WCAG) of an sRGB color.</summary>
        public static double Luminance((double R, double G, double B) srgb) =>
            (0.2126 * ToLinear(srgb.R)) + (0.7152 * ToLinear(srgb.G)) + (0.0722 * ToLinear(srgb.B));

        /// <summary>CIEDE2000 color difference.</summary>
        public static double DeltaE2000(Lab c1, Lab c2)
        {
            double c1ab = Math.Sqrt((c1.A * c1.A) + (c1.B * c1.B));
            double c2ab = Math.Sqrt((c2.A * c2.A) + (c2.B * c2.B));
            double cMean = (c1ab + c2ab) / 2;
            double g = 0.5 * (1 - Math.Sqrt(Math.Pow(cMean, 7) / (Math.Pow(cMean, 7) + Math.Pow(25, 7))));
            double a1 = (1 + g) * c1.A;
            double a2 = (1 + g) * c2.A;
            double cp1 = Math.Sqrt((a1 * a1) + (c1.B * c1.B));
            double cp2 = Math.Sqrt((a2 * a2) + (c2.B * c2.B));
            double hp1 = HueDegrees(c1.B, a1);
            double hp2 = HueDegrees(c2.B, a2);

            double dL = c2.L - c1.L;
            double dC = cp2 - cp1;
            double dh;
            if (cp1 * cp2 == 0)
            {
                dh = 0;
            }
            else if (Math.Abs(hp2 - hp1) <= 180)
            {
                dh = hp2 - hp1;
            }
            else
            {
                dh = hp2 - hp1 > 180 ? hp2 - hp1 - 360 : hp2 - hp1 + 360;
            }

            double dH = 2 * Math.Sqrt(cp1 * cp2) * Math.Sin(Radians(dh / 2));
            double lMean = (c1.L + c2.L) / 2;
            double cpMean = (cp1 + cp2) / 2;
            double hMean;
            if (cp1 * cp2 == 0)
            {
                hMean = hp1 + hp2;
            }
            else if (Math.Abs(hp1 - hp2) <= 180)
            {
                hMean = (hp1 + hp2) / 2;
            }
            else
            {
                hMean = hp1 + hp2 < 360 ? (hp1 + hp2 + 360) / 2 : (hp1 + hp2 - 360) / 2;
            }

            double t = 1 - (0.17 * Math.Cos(Radians(hMean - 30))) + (0.24 * Math.Cos(Radians(2 * hMean)))
                + (0.32 * Math.Cos(Radians((3 * hMean) + 6))) - (0.20 * Math.Cos(Radians((4 * hMean) - 63)));
            double dTheta = 30 * Math.Exp(-Math.Pow((hMean - 275) / 25, 2));
            double rc = 2 * Math.Sqrt(Math.Pow(cpMean, 7) / (Math.Pow(cpMean, 7) + Math.Pow(25, 7)));
            double sl = 1 + (0.015 * Math.Pow(lMean - 50, 2) / Math.Sqrt(20 + Math.Pow(lMean - 50, 2)));
            double sc = 1 + (0.045 * cpMean);
            double sh = 1 + (0.015 * cpMean * t);
            double rt = -Math.Sin(Radians(2 * dTheta)) * rc;
            return Math.Sqrt(Math.Pow(dL / sl, 2) + Math.Pow(dC / sc, 2) + Math.Pow(dH / sh, 2) + (rt * (dC / sc) * (dH / sh)));
        }

        private static double HueDegrees(double b, double a)
        {
            if (a == 0 && b == 0)
            {
                return 0;
            }

            double h = Math.Atan2(b, a) * 180 / Math.PI;
            return h < 0 ? h + 360 : h;
        }

        private static double Radians(double degrees) => degrees * Math.PI / 180;

        private static double F(double t) => t > 216.0 / 24389 ? Math.Cbrt(t) : ((24389.0 / 27 * t) + 16) / 116;

        private static double ToLinear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

        private static double ToSrgb(double c) => c <= 0.0031308 ? c * 12.92 : (1.055 * Math.Pow(c, 1 / 2.4)) - 0.055;

        private static double Clamp(double c) => Math.Max(0, Math.Min(1, c));
    }
}
