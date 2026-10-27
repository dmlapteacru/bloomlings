using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Rounds
{
    /// <summary>
    /// One of the 16 materials of the round palette (spec 006 FR-004, research R6; the reference is
    /// <c>tools/rounds/engine/palette.cjs</c>). A pod clears only cells of its own material; the four materials of one
    /// hero are not interchangeable. <see cref="Index"/> is the channel index of the reference rules
    /// (<c>CHANNELS = "GPBACDVYOLMRKNSE"</c>), the value a board cell holds.
    /// </summary>
    public sealed class RoundMaterial
    {
        /// <summary>The material codes in channel order, the wire ids of round matrices and pods.</summary>
        public const string Codes = "GPBACDVYOLMRKNSE";

        private RoundMaterial(int index, string name, Family family, string colorHex, string accessibleHex, string symbol)
        {
            Index = index;
            Code = Codes[index];
            Name = name;
            Family = family;
            ColorHex = colorHex;
            AccessibleHex = accessibleHex;
            Symbol = symbol;
        }

        public int Index { get; }

        public char Code { get; }

        /// <summary>The material's name as the owner knows it; the clients show a localized key.</summary>
        public string Name { get; }

        /// <summary>The hero the material belongs to: its walkers are that hero, tinted (FR-048).</summary>
        public Family Family { get; }

        /// <summary>The normal palette colour (readability-checked under normal vision, FR-047).</summary>
        public string ColorHex { get; }

        public string AccessibleHex { get; }

        /// <summary>The symbol shown on cubes, pods and places in the symbols mode (FR-047).</summary>
        public string Symbol { get; }

        /// <summary>The 16 materials in channel order.</summary>
        public static IReadOnlyList<RoundMaterial> All { get; } = new[]
        {
            new RoundMaterial(0, "Лист", Family.Sprig, "#6DBE45", "#009E73", "♧"),
            new RoundMaterial(1, "Лепесток", Family.Bloom, "#EB799C", "#CC79A7", "✿"),
            new RoundMaterial(2, "Вода", Family.Drop, "#36B7DA", "#56B4E9", "◆"),
            new RoundMaterial(3, "Янтарь", Family.Twig, "#A45F28", "#E69F00", "●"),
            new RoundMaterial(4, "Жемчуг", Family.Drop, "#F5E6BD", "#F0E442", "◇"),
            new RoundMaterial(5, "Земля", Family.Twig, "#334B48", "#333333", "▰"),
            new RoundMaterial(6, "Слива", Family.Bloom, "#7851A5", "#0072B2", "✦"),
            new RoundMaterial(7, "Солнце", Family.Sprig, "#F6CB3E", "#FFFFFF", "☀"),
            new RoundMaterial(8, "Тыква", Family.Twig, "#EF8A32", "#D55E00", "▲"),
            new RoundMaterial(9, "Лайм", Family.Sprig, "#CDEB5E", "#B8E186", "✚"),
            new RoundMaterial(10, "Мох", Family.Sprig, "#2F7D45", "#1B7837", "♣"),
            new RoundMaterial(11, "Ягода", Family.Bloom, "#D8394B", "#B2182B", "♥"),
            new RoundMaterial(12, "Румянец", Family.Bloom, "#F9C6D6", "#FDDBC7", "❀"),
            new RoundMaterial(13, "Глубина", Family.Drop, "#2C5AA8", "#2166AC", "▼"),
            new RoundMaterial(14, "Небо", Family.Drop, "#A8E2F5", "#D1E5F0", "○"),
            new RoundMaterial(15, "Кора", Family.Twig, "#6A3A1E", "#8C510A", "■"),
        };

        /// <summary>The channel index of a material code, or -1.</summary>
        public static int IndexOf(char code) => Codes.IndexOf(code);

        public static RoundMaterial Of(char code)
        {
            int index = IndexOf(code);
            if (index < 0)
            {
                throw new ArgumentException($"Unknown round material code '{code}'.", nameof(code));
            }

            return All[index];
        }
    }
}
