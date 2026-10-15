using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Bloomlings.Pipeline.Catalog
{
    /// <summary>The same checked-in launch boundary used by MSBuild and both local catalog scripts.</summary>
    public static class LaunchScope
    {
        public static int ReadLimit(string path)
        {
            if (!File.Exists(path) && path == "content/launch.props")
            {
                for (DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
                {
                    string candidate = Path.Combine(dir.FullName, "content", "launch.props");
                    if (File.Exists(candidate)) { path = candidate; break; }
                }
            }
            var values = XDocument.Load(path).Descendants("LaunchLevelCount").ToList();
            string? value = values.Count == 1 ? values[0].Value : null;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int limit) || limit < 1)
                throw new InvalidDataException(path + " must define a positive LaunchLevelCount.");
            return limit;
        }
    }
}
