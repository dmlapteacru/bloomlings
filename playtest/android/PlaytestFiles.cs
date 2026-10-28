using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// The files a playtest build carries, by their names (<c>backgrounds/home</c>, <c>icons/leaf.png</c>,
    /// <c>heromotion/sprig-idle-00</c>, <c>rounds/manifest.json</c>, <c>rounds/levels/round_levels_0001_0250.jsonl.gz</c>; the pictures' names are
    /// <c>PainterBase.SpriteResource</c>'s). The preview and the check read them from their embedded resources (the
    /// default); an APK from its assets (<c>AndroidAssetFiles</c>, which the activity sets first). An APK packs its
    /// assemblies once for every processor it runs on, so the pictures and the round packs are its assets, kept once, and
    /// not embedded resources, which it would keep three times. Engine-free.
    /// </summary>
    public interface IBundledFiles
    {
        /// <summary>Whether the build carries the file.</summary>
        bool Has(string name);

        /// <summary>The file's bytes, or null when the build does not carry it.</summary>
        Stream? Open(string name);

        /// <summary>The names of the files in a folder (<paramref name="folder"/> with its slash: <c>levels/</c>).</summary>
        IEnumerable<string> Names(string folder);
    }

    /// <summary>The build's files (<see cref="IBundledFiles"/>): its embedded resources unless the host set its own.</summary>
    public static class PlaytestFiles
    {
        private static IBundledFiles? s_source;

        /// <summary>Where the files come from: the APK sets its assets before it loads anything.</summary>
        public static IBundledFiles Source
        {
            get => s_source ??= new EmbeddedFiles(typeof(PlaytestFiles).Assembly);
            set => s_source = value;
        }

        public static bool Has(string name) => Source.Has(name);

        public static Stream? Open(string name) => Source.Open(name);

        public static IEnumerable<string> Names(string folder) => Source.Names(folder);

        /// <summary>A text file (UTF-8), or null when the build does not carry it.</summary>
        public static string? ReadText(string name)
        {
            using Stream? stream = Open(name);
            if (stream == null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>A file's bytes, or null when the build does not carry it.</summary>
        public static byte[]? ReadBytes(string name)
        {
            using Stream? stream = Open(name);
            if (stream == null)
            {
                return null;
            }

            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            return copy.ToArray();
        }
    }

    /// <summary>An assembly's embedded resources as the build's files (the preview's and the check's).</summary>
    public sealed class EmbeddedFiles : IBundledFiles
    {
        private readonly Assembly _assembly;
        private readonly HashSet<string> _names;

        public EmbeddedFiles(Assembly assembly)
        {
            _assembly = assembly;
            _names = new HashSet<string>(assembly.GetManifestResourceNames(), StringComparer.Ordinal);
        }

        public bool Has(string name) => _names.Contains(name);

        public Stream? Open(string name) => _names.Contains(name) ? _assembly.GetManifestResourceStream(name) : null;

        public IEnumerable<string> Names(string folder)
        {
            foreach (string name in _names)
            {
                if (name.StartsWith(folder, StringComparison.Ordinal))
                {
                    yield return name;
                }
            }
        }
    }

    /// <summary>
    /// A folder on disk as the build's files under one name prefix (<c>rounds/</c> → a published round folder): the
    /// check, and the preview with <c>--rounds</c>, read a published set from disk as the APK reads its assets.
    /// </summary>
    public sealed class FolderFiles : IBundledFiles
    {
        private readonly string _prefix;
        private readonly string _folder;

        public FolderFiles(string prefix, string folder)
        {
            _prefix = prefix;
            _folder = Path.GetFullPath(folder);
        }

        public bool Has(string name) => PathOf(name) is string path && File.Exists(path);

        public Stream? Open(string name) => Has(name) ? File.OpenRead(PathOf(name)!) : null;

        public IEnumerable<string> Names(string folder)
        {
            string? path = folder.StartsWith(_prefix, StringComparison.Ordinal) ? Path.Combine(_folder, folder.Substring(_prefix.Length)) : null;
            if (path == null || !Directory.Exists(path))
            {
                yield break;
            }

            foreach (string file in Directory.GetFiles(path))
            {
                yield return folder + Path.GetFileName(file);
            }
        }

        private string? PathOf(string name) =>
            name.StartsWith(_prefix, StringComparison.Ordinal) ? Path.Combine(_folder, name.Substring(_prefix.Length).Replace('/', Path.DirectorySeparatorChar)) : null;
    }
}
