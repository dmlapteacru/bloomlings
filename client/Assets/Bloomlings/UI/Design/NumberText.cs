using System.Globalization;
using System.Text;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// Numbers as the design board writes them: grouped by three with a no-break space ("1 240", "12 345"), for Petals,
    /// levels, ranks and scores (spec edge cases, research R9). Engine-free.
    /// </summary>
    public static class NumberText
    {
        /// <summary>The group separator: a no-break space (present in every system and default UI font).</summary>
        public const char Separator = ' ';

        public static string Group(long value)
        {
            string digits = (value < 0 ? -value : value).ToString(CultureInfo.InvariantCulture);
            var text = new StringBuilder(digits.Length + (digits.Length / 3) + 1);
            if (value < 0)
            {
                text.Append('-');
            }

            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0)
                {
                    text.Append(Separator);
                }

                text.Append(digits[i]);
            }

            return text.ToString();
        }

        /// <summary>"+35", "+1 240": a reward amount.</summary>
        public static string Plus(long value) => (value >= 0 ? "+" : string.Empty) + Group(value);
    }
}
