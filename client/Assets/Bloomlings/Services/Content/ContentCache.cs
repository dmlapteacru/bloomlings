using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Bloomlings.Content.Packs;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>
    /// Downloaded content versions in persistent storage (FR-078, research R6). Engine-free, so it runs in the .NET
    /// check. Layout under the root folder:
    /// <list type="bullet">
    /// <item><c>v000042/manifest.json</c> plus the packs at their manifest paths: one installed content version;</item>
    /// <item><c>active.txt</c>: the folder name of the active version, replaced atomically;</item>
    /// <item><c>staging/</c>: a version being installed; it never becomes active unless every pack verified.</item>
    /// </list>
    /// Packs are verified by SHA-256 when installed and again when loaded, so a damaged file is never used.
    /// </summary>
    public sealed class ContentCache
    {
        public const string ActiveFile = "active.txt";
        public const string StagingFolder = "staging";

        public ContentCache(string root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public string Root { get; }

        public static string VersionFolder(int contentVersion) => "v" + contentVersion.ToString("000000", CultureInfo.InvariantCulture);

        /// <summary>The active installed version, or null when nothing is installed.</summary>
        public int? ActiveVersion
        {
            get
            {
                string path = Path.Combine(Root, ActiveFile);
                if (!File.Exists(path))
                {
                    return null;
                }

                string folder = File.ReadAllText(path, Encoding.UTF8).Trim();
                return folder.Length > 1 && folder[0] == 'v' && int.TryParse(folder.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out int version)
                    ? version
                    : (int?)null;
            }
        }

        /// <summary>The manifest of the active version, or null.</summary>
        public ContentManifest? ActiveManifest()
        {
            int? version = ActiveVersion;
            if (version == null)
            {
                return null;
            }

            string path = Path.Combine(Root, VersionFolder(version.Value), ManifestWriter.ManifestFileName);
            return File.Exists(path) ? ContentManifest.Read(File.ReadAllText(path, Encoding.UTF8)) : null;
        }

        /// <summary>Loads the active version, verifying every pack. Throws when a file is missing or damaged.</summary>
        public ContentSet LoadActive()
        {
            ContentManifest manifest = ActiveManifest() ?? throw new FileNotFoundException("No content version is installed.", Path.Combine(Root, ActiveFile));
            string folder = Path.Combine(Root, VersionFolder(manifest.ContentVersion));
            return ContentLoader.FromPacks(manifest, entry => File.ReadAllBytes(Path.Combine(folder, LocalPath(entry))));
        }

        /// <summary>
        /// Installs and activates a version: writes the verified packs to <c>staging/</c>, moves it into place, then
        /// replaces <c>active.txt</c>. A crash at any point leaves the previous active version intact. Older versions
        /// are removed afterwards.
        /// </summary>
        public void Install(ContentManifest manifest, IReadOnlyDictionary<string, byte[]> packs)
        {
            string staging = Path.Combine(Root, StagingFolder);
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, true);
            }

            Directory.CreateDirectory(staging);
            foreach (PackEntry entry in manifest.Packs)
            {
                if (!packs.TryGetValue(entry.Id, out byte[]? bytes))
                {
                    throw new InvalidDataException($"Pack '{entry.Id}' was not downloaded.");
                }

                PackIntegrity.Verify(entry, bytes);
                string path = Path.Combine(staging, LocalPath(entry));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, bytes);
            }

            File.WriteAllText(Path.Combine(staging, ManifestWriter.ManifestFileName), manifest.Write(), Encoding.UTF8);

            string name = VersionFolder(manifest.ContentVersion);
            string target = Path.Combine(Root, name);
            if (Directory.Exists(target))
            {
                Directory.Delete(target, true);
            }

            Directory.Move(staging, target);

            string active = Path.Combine(Root, ActiveFile);
            string temp = active + ".tmp";
            File.WriteAllText(temp, name, Encoding.UTF8);
            if (File.Exists(active))
            {
                File.Replace(temp, active, null);
            }
            else
            {
                File.Move(temp, active);
            }

            foreach (string dir in Directory.GetDirectories(Root))
            {
                string dirName = Path.GetFileName(dir);
                if (dirName.Length > 1 && dirName[0] == 'v' && !string.Equals(dirName, name, StringComparison.Ordinal))
                {
                    Directory.Delete(dir, true);
                }
            }
        }

        /// <summary>A pack's file inside a version folder: its manifest path, or <c>packs/&lt;id&gt;.jsonl.gz</c> for URL packs.</summary>
        public static string LocalPath(PackEntry entry) =>
            entry.Path ?? Path.Combine("packs", entry.Id + ".jsonl.gz");
    }
}
