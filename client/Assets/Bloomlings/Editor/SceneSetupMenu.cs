using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// Creates the Boot scene from code (T025), so the scene never has to be hand-built: a "Boot" object with the
    /// <see cref="Boot"/> component and a camera, saved to <c>Assets/Bloomlings/Scenes/Boot.unity</c> and registered as
    /// build index 0.
    /// </summary>
    public static class SceneSetupMenu
    {
        public const string ScenesFolder = "Assets/Bloomlings/Scenes";
        public const string BootScenePath = ScenesFolder + "/Boot.unity";

        [MenuItem("Tools/Bloomlings/Create Boot Scene")]
        public static void CreateBootScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Directory.CreateDirectory(ScenesFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.96f, 0.95f, 0.90f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("Boot").AddComponent<Boot>();

            EditorSceneManager.SaveScene(scene, BootScenePath);
            AddToBuildSettingsFirst(BootScenePath);
            Debug.Log($"[Bloomlings] Created {BootScenePath} and set it as build index 0.");
        }

        private static void AddToBuildSettingsFirst(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
