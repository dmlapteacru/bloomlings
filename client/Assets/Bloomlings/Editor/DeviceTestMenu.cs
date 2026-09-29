using System;
using System.IO;
using System.Linq;
using Bloomlings.Client.Tests.Device;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// The IL2CPP device determinism check (SC-005, SC-011, T151). <b>Prepare Golden Replays</b> copies
    /// <c>core/tests/golden/*.golden.json</c> into <c>StreamingAssets/golden/</c> (with an index, since Android cannot
    /// list StreamingAssets) and creates the scene <see cref="ScenePath"/>. <b>Build Golden Replays</b> builds a player
    /// with only that scene and the IL2CPP backend; run it on each reference device and compare the
    /// <c>[GoldenReplay] RESULT</c> log line (every case must pass, and the corpus digest must match across devices).
    /// Release builds leave the corpus out.
    /// </summary>
    public static class DeviceTestMenu
    {
        public const string ScenePath = "Assets/Bloomlings/Tests/Device/RunGoldenReplays.unity";

        public static string CorpusFolder => Path.Combine(Application.streamingAssetsPath, RunGoldenReplays.Folder);

        private static string GoldenSource => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "core", "tests", "golden"));

        [MenuItem("Tools/Bloomlings/Device Tests/Prepare Golden Replays")]
        public static void Prepare()
        {
            string[] cases = Directory.GetFiles(GoldenSource, "*.golden.json").OrderBy(f => f, StringComparer.Ordinal).ToArray();
            if (Directory.Exists(CorpusFolder))
            {
                Directory.Delete(CorpusFolder, true);
            }

            Directory.CreateDirectory(CorpusFolder);
            foreach (string file in cases)
            {
                File.Copy(file, Path.Combine(CorpusFolder, Path.GetFileName(file)));
            }

            File.WriteAllText(Path.Combine(CorpusFolder, RunGoldenReplays.IndexFile), string.Join("\n", cases.Select(Path.GetFileName)) + "\n");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            new GameObject("GoldenReplays").AddComponent<RunGoldenReplays>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[DeviceTests] {cases.Length} golden cases copied; scene {ScenePath} created.");
        }

        [MenuItem("Tools/Bloomlings/Device Tests/Build Golden Replays (Android)")]
        public static void BuildAndroid() => Build(BuildTarget.Android, BuildTargetGroup.Android, "Builds/GoldenReplays/golden-replays.apk");

        [MenuItem("Tools/Bloomlings/Device Tests/Build Golden Replays (iOS)")]
        public static void BuildIos() => Build(BuildTarget.iOS, BuildTargetGroup.iOS, "Builds/GoldenReplays/ios");

        private static void Build(BuildTarget target, BuildTargetGroup group, string output)
        {
            Prepare();
            var named = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
            ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(named);
            PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(named, Il2CppCompilerConfiguration.Master);
            try
            {
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    target = target,
                    targetGroup = group,
                    locationPathName = output,
                    options = BuildOptions.Development,
                });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException($"Golden replay build failed: {report.summary.result}");
                }

                Debug.Log($"[DeviceTests] Built {output}.");
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(named, backend);
            }
        }
    }
}
