using System;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Backend;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The Leaderboard (FR-062, T141): open from L10, it shows the player's global rank by highest completed level and
    /// a few neighbours above and below. Offline, the last rank read stays on screen with a stale label. The player's
    /// own row carries their frame, badge and marker (FR-061 prestige rewards).
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
            TextMeshProUGUI title = UiFactory.CreateText("Title", card, Loc.T("leaderboard.title"), 72f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.89f, 1f, 0.98f);
            screen._status = UiFactory.CreateText("Status", card, string.Empty, 36f, UiTheme.Warning);
            UiFactory.Place(screen._status.rectTransform, 0f, 0.83f, 1f, 0.89f);
            screen._list = UiFactory.Place(UiFactory.CreateRect("Rows", card), 0.05f, 0.14f, 0.95f, 0.82f);
            Button refresh = UiFactory.CreateButton("Refresh", card, Loc.T("leaderboard.refresh"), UiTheme.SlotLocked, onRefresh, 40f);
            UiFactory.Place((RectTransform)refresh.transform, 0.08f, 0.02f, 0.46f, 0.11f);
            Button close = UiFactory.CreateButton("Close", card, Loc.T("common.close"), UiTheme.Text, screen.Hide);
            UiFactory.Place((RectTransform)close.transform, 0.54f, 0.02f, 0.92f, 0.11f);
            root.SetActive(false);
            return screen;
        }

        /// <param name="own">The player's frame, badge and marker, drawn on their own row (others' are not known offline).</param>
        public void Show(LeaderboardPage? page, bool stale, ProfileLook? own = null)
        {
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                Destroy(_list.GetChild(i).gameObject);
            }

            if (page == null || page.Entries.Count == 0)
            {
                _status.text = stale ? Loc.T("leaderboard.offline_empty") : Loc.T("leaderboard.empty");
                _root.SetActive(true);
                return;
            }

            _status.text = stale ? Loc.T("leaderboard.offline") : string.Empty;
            float row = 1f / Mathf.Max(7, page.Entries.Count);
            for (int i = 0; i < page.Entries.Count; i++)
            {
                LeaderboardEntry entry = page.Entries[i];
                float top = 1f - (i * row);
                Image background = UiFactory.CreateImage("Row", _list, ProceduralSprites.RoundedSquare, entry.IsPlayer ? UiTheme.Light(UiTheme.Accent) : UiTheme.Panel);
                UiFactory.Place(background.rectTransform, 0f, top - row + 0.01f, 1f, top - 0.01f);
                TextMeshProUGUI rank = UiFactory.CreateText("Rank", background.transform, Loc.F("leaderboard.rank", entry.Rank.ToString("N0", CultureInfo.InvariantCulture)), 40f, UiTheme.Text, TextAlignmentOptions.Left);
                UiFactory.Place(rank.rectTransform, 0.04f, 0f, 0.3f, 1f);
                TextMeshProUGUI name = UiFactory.CreateText("Name", background.transform, entry.IsPlayer ? Loc.T("leaderboard.you") : Short(entry.Name), 40f, UiTheme.Text, TextAlignmentOptions.Left);
                UiFactory.Place(name.rectTransform, 0.3f, 0f, 0.7f, 1f);
                if (entry.IsPlayer && own != null)
                {
                    Decorate(background.transform, own.Marker, 0.52f);
                    Decorate(background.transform, own.Badge, 0.6f);
                    if (own.Frame != null)
                    {
                        Image frame = UiFactory.CreateImage("Frame", background.transform, ProceduralSprites.RoundedSquare, BloomlingFigure.Tint(own.Frame));
                        frame.type = Image.Type.Sliced;
                        frame.fillCenter = false;
                        UiFactory.Stretch(frame.rectTransform);
                    }
                }
                TextMeshProUGUI level = UiFactory.CreateText("Level", background.transform, Loc.F("common.level", entry.Level), 40f, UiTheme.Text, TextAlignmentOptions.Right);
                UiFactory.Place(level.rectTransform, 0.7f, 0f, 0.96f, 1f);
            }

            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private static void Decorate(Transform row, CosmeticItem? item, float x0)
        {
            if (item == null)
            {
                return;
            }

            Image icon = UiFactory.CreateImage(item.Kind.ToString(), row, ProceduralSprites.Accessory(item.Shape), BloomlingFigure.Tint(item));
            icon.preserveAspect = true;
            UiFactory.Place(icon.rectTransform, x0, 0.15f, x0 + 0.07f, 0.85f);
        }

        private static string Short(string name) => string.IsNullOrEmpty(name) ? Loc.T("leaderboard.anonymous") : (name.Length > 16 ? name.Substring(0, 16) : name);
    }
}
