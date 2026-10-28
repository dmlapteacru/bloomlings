using System;
using System.IO;
using Bloomlings.Client.Services.Content;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// "Tools/Bloomlings/Import Published Content" (T091, FR-078): copies the output of the pipeline's <c>publish</c>
    /// command (<c>build/content/</c> at the repository root) into <c>Assets/StreamingAssets/content/</c>, replacing the
    /// previous manifest and packs. The manifest and every pack are verified before anything is replaced. The loose
    /// <c>dev/</c> folder is kept for the Editor; <see cref="ReleaseContentBuildStep"/> leaves it out of release builds.
    /// "Tools/Bloomlings/Import Round Content" (spec 006 T060) does the same for the round levels: it copies the published
    /// round content committed at <c>content/game/rounds/published/</c> (<c>rounds publish</c>) into
    /// <c>Assets/StreamingAssets/rounds/</c>, where player builds read it (<see cref="RoundContentLoader"/>);
    /// <see cref="CiBuild.Build"/> runs it before every game build.
    /// </summary>
    public static class ImportContentMenu
    {
        public static string PublishedFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "build", "content"));

        public static string StreamingContentFolder => Path.Combine(Application.dataPath, "StreamingAssets", BundledContentLoader.ContentFolder);

        /// <summary>The bundled copy of the round content: <c>Assets/StreamingAssets/rounds/</c>.</summary>
        public static string StreamingRoundsFolder => Path.Combine(Application.dataPath, "StreamingAssets", RoundContentLoader.StreamingFolder);

        [MenuItem("Tools/Bloomlings/Import Round Content")]
        public static void ImportRounds()
        {
            try
            {
                RoundContentManifest manifest = ImportRoundsFrom(RoundContentLoader.RepositoryPublishedFolder, StreamingRoundsFolder);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Bloomlings", $"Imported round content v{manifest.ContentVersion} ({manifest.Levels.Count} levels, {manifest.Packs.Count} packs).", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Bloomlings", "Import failed: " + ex.Message + "\nRun `rounds publish --out content/game/rounds/published` first.", "OK");
            }
        }

        /// <summary>
        /// Verifies the published round content (every pack's SHA-256 and length, the manifest's levels 1..N in their packs,
        /// every level and picture readable) and replaces the bundled copy with it: the manifest and the packs it lists,
        /// nothing else. Engine-free.
        /// </summary>
        public static RoundContentManifest ImportRoundsFrom(string published, string streamingRounds)
        {
            RoundContentSet set = RoundContentLoader.ReadFolder(published);
            for (int n = 1; n <= set.LevelCount; n++)
            {
                set.GetPicture(set.GetLevel(n));
            }

            RoundContentManifest manifest = set.Manifest;
            if (Directory.Exists(streamingRounds))
            {
                Directory.Delete(streamingRounds, true);
            }

            Directory.CreateDirectory(streamingRounds);
            foreach (RoundPackEntry pack in manifest.Packs)
            {
                string target = Path.Combine(streamingRounds, pack.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(Path.Combine(published, pack.Path), target, true);
            }

            File.Copy(Path.Combine(published, RoundContentManifest.FileName), Path.Combine(streamingRounds, RoundContentManifest.FileName), true);
            return manifest;
        }

        [MenuItem("Tools/Bloomlings/Import Published Content")]
        public static void Import()
        {
            try
            {
                ContentManifest manifest = ImportFrom(PublishedFolder, StreamingContentFolder);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Bloomlings", $"Imported content v{manifest.ContentVersion} ({manifest.Packs.Count} packs, max level {manifest.MaxLevel}).", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Bloomlings", "Import failed: " + ex.Message + "\nRun `publish --out build/content` first.", "OK");
            }
        }

        /// <summary>Verifies the published content and copies it over the bundled manifest and packs. Engine-free.</summary>
        public static ContentManifest ImportFrom(string published, string streamingContent)
        {
            string manifestPath = Path.Combine(published, BundledContentLoader.ManifestFile);
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("No published manifest.", manifestPath);
            }

            ContentManifest manifest = ContentManifest.Read(File.ReadAllText(manifestPath));
            foreach (PackEntry entry in manifest.Packs)
            {
                if (entry.Path == null)
                {
                    throw new InvalidDataException($"Pack '{entry.Id}' has a URL; bundled packs need a path.");
                }

                PackIntegrity.Verify(entry, File.ReadAllBytes(Path.Combine(published, entry.Path)));
            }

            // Replace everything except the dev folder, which stays for Editor play.
            Directory.CreateDirectory(streamingContent);
            foreach (string dir in Directory.GetDirectories(streamingContent))
            {
                if (!string.Equals(Path.GetFileName(dir), BundledContentLoader.DevFolder, StringComparison.Ordinal))
                {
                    Directory.Delete(dir, true);
                }
            }

            foreach (string file in Directory.GetFiles(streamingContent))
            {
                File.Delete(file);
            }

            foreach (PackEntry entry in manifest.Packs)
            {
                string target = Path.Combine(streamingContent, entry.Path!);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(Path.Combine(published, entry.Path!), target, true);
            }

            File.Copy(manifestPath, Path.Combine(streamingContent, BundledContentLoader.ManifestFile), true);
            return manifest;
        }
    }

    /// <summary>
    /// Leaves the loose <c>StreamingAssets/content/dev/</c> folder out of release builds (FR-078): it is moved aside
    /// before a non-development build and restored afterwards. A release build without a manifest fails early.
    /// </summary>
    public sealed class ReleaseContentBuildStep : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static string DevFolder => Path.Combine(ImportContentMenu.StreamingContentFolder, BundledContentLoader.DevFolder);

        private static string ParkedFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "bloomlings-dev-content"));

        private static string ParkedGoldenFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "bloomlings-golden"));

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & BuildOptions.Development) != 0)
            {
                return;
            }

            // Round builds carry the round content (spec 006 FR-060); older builds the legacy packs.
            if (!File.Exists(Path.Combine(ImportContentMenu.StreamingRoundsFolder, RoundContentManifest.FileName))
                && !File.Exists(Path.Combine(ImportContentMenu.StreamingContentFolder, BundledContentLoader.ManifestFile)))
            {
                throw new BuildFailedException("Release builds need published content: run Tools/Bloomlings/Import Round Content (or `publish`, then Tools/Bloomlings/Import Published Content).");
            }

            // The dev levels and the device-test golden corpus never ship in a release build.
            Park(DevFolder, ParkedFolder);
            Park(DeviceTestMenu.CorpusFolder, ParkedGoldenFolder);
        }

        private static void Park(string folder, string parked)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            if (Directory.Exists(parked))
            {
                Directory.Delete(parked, true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(parked)!);
            Directory.Move(folder, parked);
            AssetDatabase.Refresh();
        }

        public void OnPostprocessBuild(BuildReport report) => Restore();

        /// <summary>Also run from the menu if a build was interrupted before the post-process step.</summary>
        [MenuItem("Tools/Bloomlings/Restore Dev Content Folder")]
        public static void Restore()
        {
            foreach ((string parked, string folder) in new[] { (ParkedFolder, DevFolder), (ParkedGoldenFolder, DeviceTestMenu.CorpusFolder) })
            {
                if (Directory.Exists(parked) && !Directory.Exists(folder))
                {
                    Directory.Move(parked, folder);
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
