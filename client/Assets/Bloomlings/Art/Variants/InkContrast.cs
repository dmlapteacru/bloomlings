using System;

namespace Bloomlings.Client.Art.Variants
{
    /// <summary>
    /// The ink (icon and count color) drawn on a variant color: white or a dark ink, whichever has the higher WCAG
    /// contrast ratio. Light variants (Leaf, Moss, Flower, Dew) get the dark ink, so their icons and counts stay readable
    /// (FR-005, FR-072). Engine-free; channels in 0–1 (sRGB).
    /// </summary>
    public static class InkContrast
    {
        /// <summary>The dark ink, <c>#2B2B2B</c>.</summary>
        public const double DarkInk = 0x2B / 255.0;

        public static bool UseDarkInk(double r, double g, double b)
        {
            double background = RelativeLuminance(r, g, b);
            double dark = RelativeLuminance(DarkInk, DarkInk, DarkInk);
            return Ratio(background, dark) > Ratio(background, 1.0);
        }

        /// <summary>The WCAG contrast ratio of the chosen ink on the color.</summary>
        public static double BestRatio(double r, double g, double b)
        {
            double background = RelativeLuminance(r, g, b);
            return Math.Max(Ratio(background, RelativeLuminance(DarkInk, DarkInk, DarkInk)), Ratio(background, 1.0));
        }

        public static double RelativeLuminance(double r, double g, double b) =>
            (0.2126 * Linear(r)) + (0.7152 * Linear(g)) + (0.0722 * Linear(b));

        private static double Ratio(double a, double b) => (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);

        private static double Linear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
