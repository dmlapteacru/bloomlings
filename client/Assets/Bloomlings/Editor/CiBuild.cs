using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// Build entry points for CI (<c>.github/workflows/android-apk.yml</c>, run by hand), called with
    /// <c>-executeMethod</c>. GameCI passes <c>-customBuildPath</c>. Before building, the project is prepared the way the
    /// first-open steps of client/README.md describe, so a fresh checkout builds without the Editor UI:
    /// <list type="bullet">
    /// <item>the TextMeshPro essential resources are imported;</item>
    /// <item>the Android player settings are applied (portrait, IL2CPP ARM64, API 26+, APK);</item>
    /// <item>the Boot, Home and Gameplay scenes are created;</item>
    /// <item>the packs published to <c>build/content/</c> are imported into StreamingAssets.</item>
    /// </list>
    /// The game is a release build when published content was imported, unless <c>-bloomlingsDevelopment</c> is given.
    /// </summary>
    public static class CiBuild
    {
        public const string ApplicationId = "com.bloomlings.game";

        public static void Build()
        {
            ImportTmpEssentials();
            ConfigureAndroid();
            SceneSetupMenu.CreateAllScenes();
            bool content = ImportPublishedContent();
            bool development = HasFlag("-bloomlingsDevelopment") || !content;
            Run(new[] { SceneSetupMenu.BootScenePath, SceneSetupMenu.HomeScenePath, SceneSetupMenu.GameplayScenePath }, development ? BuildOptions.Development : BuildOptions.None);
        }

        /// <summary>The IL2CPP golden replay player (T151).</summary>
        public static void BuildGoldenReplays()
        {
            ImportTmpEssentials();
            ConfigureAndroid();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId + ".golden");
            PlayerSettings.productName = "Bloomlings Golden Replays";
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Master);
            DeviceTestMenu.Prepare();
            Run(new[] { DeviceTestMenu.ScenePath }, BuildOptions.Development);
        }

        /// <summary>The player settings of client/README.md step 3, for Android.</summary>
        public static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "Bloomlings";
            PlayerSettings.productName = "Bloomlings";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.bundleVersion = "0.1.0";
            if (int.TryParse(Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER"), out int run) && run > 0)
            {
                PlayerSettings.Android.bundleVersionCode = run;
            }

            EditorUserBuildSettings.buildAppBundle = false;
        }

        /// <summary>
        /// Imports the TextMeshPro essential resources (fonts and <c>TMP Settings</c>) once; all UI text uses TextMeshPro.
        /// The importer is found by reflection so a change in its location never breaks compilation.
        /// </summary>
        public static void ImportTmpEssentials()
        {
            if (File.Exists(Path.Combine(Application.dataPath, "TextMesh Pro", "Resources", "TMP Settings.asset")))
            {
                return;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                MethodInfo? import = assembly.GetType("TMPro.TMP_PackageResourceImporter")?.GetMethod(
                    "ImportResources",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(bool), typeof(bool), typeof(bool) },
                    null);
                if (import != null)
                {
                    import.Invoke(null, new object[] { true, false, false });
                    AssetDatabase.Refresh();
                    Debug.Log("[CiBuild] TMP essential resources imported.");
                    return;
                }
            }

            foreach (string package in new[]
            {
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage",
            })
            {
                if (File.Exists(Path.GetFullPath(package)))
                {
                    AssetDatabase.ImportPackage(package, false);
                    AssetDatabase.Refresh();
                    Debug.Log("[CiBuild] TMP essential resources imported from " + package + ".");
                    return;
                }
            }

            Debug.LogWarning("[CiBuild] TMP essential resources not found; text may not render.");
        }

        /// <summary>Imports the packs published to <c>build/content/</c>, if any; true when content was imported.</summary>
        private static bool ImportPublishedContent()
        {
            if (!File.Exists(Path.Combine(ImportContentMenu.PublishedFolder, Services.Content.BundledContentLoader.ManifestFile)))
            {
                Debug.LogWarning("[CiBuild] No published content in build/content; building a development player without packs.");
                return false;
            }

            var manifest = ImportContentMenu.ImportFrom(ImportContentMenu.PublishedFolder, ImportContentMenu.StreamingContentFolder);
            AssetDatabase.Refresh();
            Debug.Log($"[CiBuild] Content v{manifest.ContentVersion} imported ({manifest.Packs.Count} packs).");
            return true;
        }

        private static void Run(string[] scenes, BuildOptions options)
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string path = Argument("-customBuildPath") ?? Path.Combine("Builds", target.ToString(), "Bloomlings.apk");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                locationPathName = path,
                options = options,
            });
            Debug.Log($"[CiBuild] {target} ({options}) → {path}: {report.summary.result}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        private static bool HasFlag(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

        private static string? Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
