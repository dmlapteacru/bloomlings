using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// Build entry points for CI (<c>.github/workflows/unity-build.yml</c>, T152), called with <c>-executeMethod</c>. GameCI
    /// passes <c>-buildTarget</c> and <c>-customBuildPath</c>. The game build creates the scenes if needed and is a
    /// development build unless published content has been imported (release builds need it, FR-084).
    /// </summary>
    public static class CiBuild
    {
        public static void Build()
        {
            SceneSetupMenu.CreateAllScenes();
            bool release = File.Exists(Path.Combine(ImportContentMenu.StreamingContentFolder, Services.Content.BundledContentLoader.ManifestFile));
            Run(new[] { SceneSetupMenu.BootScenePath, SceneSetupMenu.HomeScenePath, SceneSetupMenu.GameplayScenePath }, release ? BuildOptions.None : BuildOptions.Development);
        }

        /// <summary>The IL2CPP golden replay player (T151).</summary>
        public static void BuildGoldenReplays()
        {
            DeviceTestMenu.Prepare();
            var named = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(named, Il2CppCompilerConfiguration.Master);
            Run(new[] { DeviceTestMenu.ScenePath }, BuildOptions.Development);
        }

        private static void Run(string[] scenes, BuildOptions options)
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string path = Argument("-customBuildPath") ?? Path.Combine("Builds", target.ToString(), "Bloomlings");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                locationPathName = path,
                options = options,
            });
            Debug.Log($"[CiBuild] {target} → {path}: {report.summary.result}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        private static string? Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
