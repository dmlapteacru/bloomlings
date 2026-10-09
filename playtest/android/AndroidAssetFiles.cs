using System;
using System.Collections.Generic;
using System.IO;
using Android.Content.Res;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// The APK's assets as the build's files (<see cref="IBundledFiles"/>): one copy of every picture, level and library
    /// picture, whatever processors the APK carries code for. The folders are listed once; a picture is found by its name
    /// with its extension or without it (the backgrounds, the hero frames and the avatars go by their names alone, as when
    /// they were embedded, so a PNG or a JPEG works).
    /// </summary>
    public sealed class AndroidAssetFiles : IBundledFiles
    {
        /// <summary>The asset folders (the csproj's <c>AndroidAsset</c> links).</summary>
        private static readonly string[] Folders =
        {
            "backgrounds", "brand", "icons", "decor", "avatars", "heromotion", "characters/2d", "characters/3d", "levels", "pictures",
        };

        private readonly AssetManager _assets;
        private readonly Dictionary<string, string> _paths = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> _folders = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public AndroidAssetFiles(AssetManager assets)
        {
            _assets = assets;
            foreach (string folder in Folders)
            {
                var files = new List<string>();
                foreach (string file in assets.List(folder) ?? Array.Empty<string>())
                {
                    string path = folder + "/" + file;
                    files.Add(path);
                    _paths[path] = path;
                    int dot = file.LastIndexOf('.');
                    if (dot > 0)
                    {
                        _paths.TryAdd(folder + "/" + file.Substring(0, dot), path);
                    }
                }

                _folders[folder + "/"] = files;
            }
        }

        public bool Has(string name) => _paths.ContainsKey(name);

        public Stream? Open(string name) => _paths.TryGetValue(name, out string? path) ? _assets.Open(path) : null;

        public IEnumerable<string> Names(string folder) =>
            _folders.TryGetValue(folder, out List<string>? files) ? files : (IEnumerable<string>)Array.Empty<string>();
    }
}
