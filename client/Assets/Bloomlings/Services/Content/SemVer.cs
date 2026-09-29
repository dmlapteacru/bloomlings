using System;
using System.Globalization;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>Compares <c>MAJOR.MINOR.PATCH</c> versions, such as a manifest's <c>minAppVersion</c> and the app version.</summary>
    public static class SemVer
    {
        /// <summary>Negative, zero or positive as <paramref name="a"/> is lower than, equal to or higher than <paramref name="b"/>.</summary>
        public static int Compare(string a, string b)
        {
            int[] x = Parse(a);
            int[] y = Parse(b);
            for (int i = 0; i < 3; i++)
            {
                if (x[i] != y[i])
                {
                    return x[i].CompareTo(y[i]);
                }
            }

            return 0;
        }

        /// <summary>
        /// Parses the leading <c>MAJOR.MINOR.PATCH</c>; missing parts count as 0 and a suffix such as <c>-beta</c> or
        /// <c>+42</c> is ignored, so any Unity <c>Application.version</c> can be compared.
        /// </summary>
        public static int[] Parse(string version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            var parts = new int[3];
            string core = version.Split('-', '+')[0];
            string[] fields = core.Split('.');
            for (int i = 0; i < 3 && i < fields.Length; i++)
            {
                if (!int.TryParse(fields[i], NumberStyles.None, CultureInfo.InvariantCulture, out parts[i]))
                {
                    throw new FormatException($"'{version}' is not a MAJOR.MINOR.PATCH version.");
                }
            }

            return parts;
        }
    }
}
