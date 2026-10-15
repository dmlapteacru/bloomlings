using System.IO;
using Bloomlings.Client.Services.Content;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// "Tools/Bloomlings/Play Dev Level" (T052): plays a level from the repository's <c>content/</c> folder in the
    /// Gameplay scene, without Boot. The Gameplay scene is created on first use.
    /// </summary>
    public static class DevLevelMenu
    {
        [MenuItem("Tools/Bloomlings/Play Dev Level/Level dev-001 (Tulip)")]
        public static void PlayDev1() => Play(Path.Combine(DevContent.RepositoryContentFolder, "curated", "dev", "level-dev-001.json"));

        [MenuItem("Tools/Bloomlings/Play Dev Level/Level dev-002 (Mushroom, Leaf + Moss)")]
        public static void PlayDev2() => Play(Path.Combine(DevContent.RepositoryContentFolder, "curated", "dev", "level-dev-002.json"));

        [MenuItem("Tools/Bloomlings/Play Dev Level/Level dev-003 (Watering can)")]
        public static void PlayDev3() => Play(Path.Combine(DevContent.RepositoryContentFolder, "curated", "dev", "level-dev-003.json"));

        [MenuItem("Tools/Bloomlings/Play Dev Level/Mosaic prototype 24x24 (Flower)")]
        public static void PlayMosaic24() => Play(Path.Combine(DevContent.RepositoryContentFolder, "curated", "dev", "level-dev-mosaic-24x24.json"));

        [MenuItem("Tools/Bloomlings/Play Dev Level/Mosaic prototype 30x32 (Balloon)")]
        public static void PlayMosaic30() => Play(Path.Combine(DevContent.RepositoryContentFolder, "curated", "dev", "level-dev-mosaic-30x32.json"));

        [MenuItem("Tools/Bloomlings/Play Dev Level/Mosaic prototype 36x36 (Butterfly)")]
        public static void PlayMosaic36() => Play(Path.Combine(DevContent.RepositoryContentFolder, "curated", "dev", "level-dev-mosaic-36x36.json"));

        [MenuItem("Tools/Bloomlings/Play Dev Level/Counted stacks 24x24")]
        public static void PlayCountedStacks() => Play(Path.Combine(DevContent.RepositoryContentFolder, "curated", "dev", "level-dev-counted-stack-24x24.json"));

        [MenuItem("Tools/Bloomlings/Play Dev Level/Large gate 36x36")]
        public static void PlayLargeGate() => Play(Path.Combine(DevContent.RepositoryContentFolder, "curated", "dev", "level-dev-large-gate-36x36.json"));

        [MenuItem("Tools/Bloomlings/Play Dev Level/Large fountain 36x36")]
        public static void PlayLargeFountain() => Play(Path.Combine(DevContent.RepositoryContentFolder, "curated", "dev", "level-dev-large-fountain-36x36.json"));

        [MenuItem("Tools/Bloomlings/Play Dev Level/Choose File...")]
        public static void Choose()
        {
            string file = EditorUtility.OpenFilePanel("Play level", Path.Combine(DevContent.RepositoryContentFolder, "curated"), "json");
            if (!string.IsNullOrEmpty(file))
            {
                Play(file);
            }
        }

        private static void Play(string levelFile)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            EditorPrefs.SetString(DevContent.SelectedLevelKey, levelFile);
            if (!File.Exists(SceneSetupMenu.GameplayScenePath))
            {
                SceneSetupMenu.CreateGameplayScene();
            }
            else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(SceneSetupMenu.GameplayScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
