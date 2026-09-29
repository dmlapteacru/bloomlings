using System;
using System.IO;
using Bloomlings.Client.Services.Content;
using Bloomlings.Content.Packs;
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
    /// </summary>
    public static class ImportContentMenu
    {
        public static string PublishedFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "build", "content"));

        public static string StreamingContentFolder => Path.Combine(Application.dataPath, "StreamingAssets", BundledContentLoader.ContentFolder);

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

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & BuildOptions.Development) != 0)
            {
                return;
            }

            if (!File.Exists(Path.Combine(ImportContentMenu.StreamingContentFolder, BundledContentLoader.ManifestFile)))
            {
                throw new BuildFailedException("Release builds need published content: run `publish`, then Tools/Bloomlings/Import Published Content.");
            }

            if (Directory.Exists(DevFolder))
            {
                if (Directory.Exists(ParkedFolder))
                {
                    Directory.Delete(ParkedFolder, true);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(ParkedFolder)!);
                Directory.Move(DevFolder, ParkedFolder);
                AssetDatabase.Refresh();
            }
        }

        public void OnPostprocessBuild(BuildReport report) => Restore();

        /// <summary>Also run from the menu if a build was interrupted before the post-process step.</summary>
        [MenuItem("Tools/Bloomlings/Restore Dev Content Folder")]
        public static void Restore()
        {
            if (Directory.Exists(ParkedFolder) && !Directory.Exists(DevFolder))
            {
                Directory.Move(ParkedFolder, DevFolder);
                AssetDatabase.Refresh();
            }
        }
    }
}
