using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// The files a playtest build carries, by their names (<c>backgrounds/home</c>, <c>icons/leaf.png</c>,
    /// <c>heromotion/sprig-idle-00</c>, <c>levels/level-0001.json</c>, <c>pictures/burger_01.json</c>; the pictures' names are
    /// <c>PainterBase.SpriteResource</c>'s). The preview and the check read them from their embedded resources (the
    /// default); an APK from its assets (<c>AndroidAssetFiles</c>, which the activity sets first). An APK packs its
    /// assemblies once for every processor it runs on, so the pictures, the levels and the picture library are its assets,
    /// kept once, and not embedded resources, which it would keep three times. Engine-free.
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
}
