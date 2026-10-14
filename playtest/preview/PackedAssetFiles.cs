using System;
using System.Collections.Generic;
using System.IO;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>Exercises the APK's generated image bytes with the desktop painter, retaining embedded content/fonts.</summary>
    internal sealed class PackedAssetFiles : IBundledFiles
    {
        private readonly IBundledFiles _fallback = new EmbeddedFiles(typeof(SkiaPainter).Assembly);
        private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

        public PackedAssetFiles(string root)
        {
            root = Path.Combine(Path.GetFullPath(root), "Assets");
            foreach (string path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                if (!path.EndsWith(".png", StringComparison.Ordinal) && !path.EndsWith(".jpg", StringComparison.Ordinal)) continue;
                string name = Path.GetRelativePath(root, path).Replace('\\', '/');
                _files.Add(name, path);
                _files.TryAdd(name.Substring(0, name.LastIndexOf('.')), path);
            }
        }

        public bool Has(string name) => _files.ContainsKey(name) || _fallback.Has(name);
        public Stream? Open(string name) => _files.TryGetValue(name, out string? path) ? File.OpenRead(path) : _fallback.Open(name);
        public IEnumerable<string> Names(string folder) => _fallback.Names(folder);
    }
}
