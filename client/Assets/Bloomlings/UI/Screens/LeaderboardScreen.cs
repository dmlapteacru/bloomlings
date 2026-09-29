using System;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Services.Backend;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The Leaderboard (FR-062, T141): open from L10, it shows the player's global rank by highest completed level and
    /// a few neighbours above and below. Offline, the last rank read stays on screen with a stale label.
    /// </summary>
    public sealed class LeaderboardScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private RectTransform _list = null!;
        private TextMeshProUGUI _status = null!;

        public bool IsOpen => _root.activeSelf;

        public static LeaderboardScreen Create(Transform parent, Action onRefresh)
        {
            RectTransform card = UiFactory.CreateModal("Leaderboard", parent, 0.75f, out GameObject root);
            var screen = root.AddComponent<LeaderboardScreen>();
            screen._root = root;
            TextMeshProUGUI title = UiFactory.CreateText("Title", card, "Leaderboard", 72f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.89f, 1f, 0.98f);
            screen._status = UiFactory.CreateText("Status", card, string.Empty, 36f, UiTheme.Warning);
            UiFactory.Place(screen._status.rectTransform, 0f, 0.83f, 1f, 0.89f);
            screen._list = UiFactory.Place(UiFactory.CreateRect("Rows", card), 0.05f, 0.14f, 0.95f, 0.82f);
            Button refresh = UiFactory.CreateButton("Refresh", card, "Refresh", UiTheme.SlotLocked, onRefresh, 40f);
            UiFactory.Place((RectTransform)refresh.transform, 0.08f, 0.02f, 0.46f, 0.11f);
            Button close = UiFactory.CreateButton("Close", card, "Close", UiTheme.Text, screen.Hide);
            UiFactory.Place((RectTransform)close.transform, 0.54f, 0.02f, 0.92f, 0.11f);
            root.SetActive(false);
            return screen;
        }

        public void Show(LeaderboardPage? page, bool stale)
        {
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                Destroy(_list.GetChild(i).gameObject);
            }

            if (page == null || page.Entries.Count == 0)
            {
                _status.text = stale ? "Offline: the leaderboard will update when you reconnect" : "No ranks yet";
                _root.SetActive(true);
                return;
            }

            _status.text = stale ? "Offline: showing the last known ranks" : string.Empty;
            float row = 1f / Mathf.Max(7, page.Entries.Count);
            for (int i = 0; i < page.Entries.Count; i++)
            {
                LeaderboardEntry entry = page.Entries[i];
                float top = 1f - (i * row);
                Image background = UiFactory.CreateImage("Row", _list, ProceduralSprites.RoundedSquare, entry.IsPlayer ? UiTheme.Light(UiTheme.Accent) : UiTheme.Panel);
                UiFactory.Place(background.rectTransform, 0f, top - row + 0.01f, 1f, top - 0.01f);
                TextMeshProUGUI rank = UiFactory.CreateText("Rank", background.transform, "#" + entry.Rank.ToString("N0", CultureInfo.InvariantCulture), 40f, UiTheme.Text, TextAlignmentOptions.Left);
                UiFactory.Place(rank.rectTransform, 0.04f, 0f, 0.3f, 1f);
                TextMeshProUGUI name = UiFactory.CreateText("Name", background.transform, entry.IsPlayer ? "You" : Short(entry.Name), 40f, UiTheme.Text, TextAlignmentOptions.Left);
                UiFactory.Place(name.rectTransform, 0.3f, 0f, 0.7f, 1f);
                TextMeshProUGUI level = UiFactory.CreateText("Level", background.transform, "Level " + entry.Level.ToString(CultureInfo.InvariantCulture), 40f, UiTheme.Text, TextAlignmentOptions.Right);
                UiFactory.Place(level.rectTransform, 0.7f, 0f, 0.96f, 1f);
            }

            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private static string Short(string name) => string.IsNullOrEmpty(name) ? "Gardener" : (name.Length > 16 ? name.Substring(0, 16) : name);
    }
}
