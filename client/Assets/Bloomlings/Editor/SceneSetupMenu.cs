using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.App;
using Bloomlings.Client.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// Creates the scenes from code, so they never have to be hand-built:
    /// <list type="bullet">
    /// <item>Boot (T025): a "Boot" object with <see cref="Boot"/> and a camera, as build index 0;</item>
    /// <item>Gameplay (T052): a "Gameplay" object with <see cref="GameplayController"/> and a camera, right after Boot.</item>
    /// </list>
    /// All screens are built at runtime by the scripts.
    /// </summary>
    public static class SceneSetupMenu
    {
        public const string ScenesFolder = "Assets/Bloomlings/Scenes";
        public const string BootScenePath = ScenesFolder + "/Boot.unity";
        public const string GameplayScenePath = ScenesFolder + "/Gameplay.unity";

        [MenuItem("Tools/Bloomlings/Create Boot Scene")]
        public static void CreateBootScene()
        {
            if (CreateScene(BootScenePath, "Boot", go => go.AddComponent<Boot>()))
            {
                SetBuildIndex(BootScenePath, 0);
            }
        }

        [MenuItem("Tools/Bloomlings/Create Gameplay Scene")]
        public static void CreateGameplayScene()
        {
            if (CreateScene(GameplayScenePath, "Gameplay", go => go.AddComponent<GameplayController>()))
            {
                SetBuildIndex(GameplayScenePath, 1);
            }
        }

        private static bool CreateScene(string path, string rootName, System.Action<GameObject> addRoot)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            Directory.CreateDirectory(ScenesFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.96f, 0.95f, 0.90f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            addRoot(new GameObject(rootName));
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[Bloomlings] Created {path}.");
            return true;
        }

        private static void SetBuildIndex(string path, int index)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(Mathf.Min(index, scenes.Count), new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
