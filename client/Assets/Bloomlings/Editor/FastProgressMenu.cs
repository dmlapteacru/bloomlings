using Bloomlings.Client.App;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Progression;
using UnityEditor;
using UnityEngine;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// "Tools/Bloomlings/Fast Progress" (T060, quickstart §5 step 7): sets the highest completed level to N and fires
    /// every roadmap unlock passed on the way. In Play mode it changes the running game's save; otherwise the save on
    /// disk.
    /// </summary>
    public sealed class FastProgressMenu : EditorWindow
    {
        private int _target = 10;

        [MenuItem("Tools/Bloomlings/Fast Progress")]
        public static void Open() => GetWindow<FastProgressMenu>(true, "Fast Progress");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Set the highest completed level (unlocks fire on the way).");
            _target = Mathf.Max(0, EditorGUILayout.IntField("Highest completed level", _target));
            if (GUILayout.Button("Apply"))
            {
                Apply(_target);
            }
        }

        public static void Apply(int highestCompleted)
        {
            SaveService saves;
            PlayerSave save;
            if (EditorApplication.isPlaying && AppServices.Current != null && AppServices.Current.TryGet(out SaveService? live))
            {
                saves = live!;
                save = saves.Current;
            }
            else
            {
                saves = SaveService.CreateDefault(new SystemClock());
                save = saves.Load();
            }

            var progression = new ProgressionService(save, UnlockRoadmap.Default, saves.Save);
            progression.Initialize();
            if (highestCompleted < progression.HighestCompletedLevel)
            {
                save.Progression.HighestCompletedLevel = highestCompleted;
                saves.Save();
                Debug.Log($"[Fast Progress] Highest completed level set back to {highestCompleted}.");
                return;
            }

            foreach (UnlockEntry entry in progression.FastForward(highestCompleted))
            {
                Debug.Log($"[Fast Progress] Unlocked {entry.UnlockId} (L{entry.Level}).");
            }

            Debug.Log($"[Fast Progress] Current level is now {progression.CurrentLevel}.");
        }
    }
}
