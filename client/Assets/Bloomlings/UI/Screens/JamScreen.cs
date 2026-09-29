using System;
using System.Collections.Generic;
using Bloomlings.Client.UI;
using Bloomlings.Core.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The Jam screen (FR-027, T051, T121). It covers only the bottom of the screen, so the board stays visible. It lists
    /// the eligible boosters the player owns or can afford with Petals (the rewarded rescue joins with US6) and
    /// Restart. It never opens the Store.
    /// </summary>
    public sealed class JamScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private TextMeshProUGUI _title = null!;
        private RectTransform _options = null!;
        private Action<Recovery> _onRecovery = _ => { };

        public static JamScreen Create(Transform parent, Action onRestart, Action<Recovery> onRecovery)
        {
            Image panel = UiFactory.CreateImage("JamScreen", parent, Art.ProceduralSprites.RoundedSquare, UiTheme.Panel, raycast: true);
            UiFactory.Place(panel.rectTransform, 0.05f, 0.03f, 0.95f, 0.30f);
            var screen = panel.gameObject.AddComponent<JamScreen>();
            screen._root = panel.gameObject;
            screen._onRecovery = onRecovery;
            screen._title = UiFactory.CreateText("Title", panel.transform, "No room left!", 64f, UiTheme.Warning);
            UiFactory.Place(screen._title.rectTransform, 0f, 0.74f, 1f, 0.96f);
            screen._options = UiFactory.Place(UiFactory.CreateRect("Recoveries", panel.transform), 0.05f, 0.40f, 0.95f, 0.72f);
            Button restart = UiFactory.CreateButton("Restart", panel.transform, "Restart", UiTheme.Text, onRestart);
            UiFactory.Place((RectTransform)restart.transform, 0.25f, 0.06f, 0.75f, 0.34f);
            panel.gameObject.SetActive(false);
            return screen;
        }

        /// <param name="recoveries">Only the recoveries the player can use now: owned, or affordable with Petals (FR-027).</param>
        /// <param name="label">The button text, e.g. "Extra Slot ×1" or "Shuffle 40 ✿".</param>
        /// <param name="rescue">The rewarded rescue (a free booster use, once per attempt), or null when not offered.</param>
        public void Show(bool stuck, IReadOnlyList<Recovery> recoveries, Func<Recovery, string>? label = null, (string Label, Action Watch)? rescue = null)
        {
            label ??= Label;
            _title.text = stuck ? "No pod can move!" : "No room left!";
            for (int i = _options.childCount - 1; i >= 0; i--)
            {
                Destroy(_options.GetChild(i).gameObject);
            }

            int count = recoveries.Count + (rescue.HasValue ? 1 : 0);
            float width = count == 0 ? 0f : 1f / count;
            for (int i = 0; i < recoveries.Count; i++)
            {
                Recovery recovery = recoveries[i];
                Button button = UiFactory.CreateButton(recovery.ToString(), _options, label(recovery), UiTheme.Accent, () => _onRecovery(recovery), 40f);
                UiFactory.Place((RectTransform)button.transform, (i * width) + 0.01f, 0f, ((i + 1) * width) - 0.01f, 1f);
            }

            if (rescue.HasValue)
            {
                Button watch = UiFactory.CreateButton("Rescue", _options, rescue.Value.Label, UiTheme.Warning, rescue.Value.Watch, 36f);
                UiFactory.Place((RectTransform)watch.transform, (recoveries.Count * width) + 0.01f, 0f, 0.99f, 1f);
            }

            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        public static string Label(Recovery recovery) => recovery switch
        {
            Recovery.ExtraSlot => "Extra Slot",
            Recovery.Shuffle => "Shuffle",
            Recovery.Return => "Return",
            _ => "Bloom Burst",
        };
    }
}
