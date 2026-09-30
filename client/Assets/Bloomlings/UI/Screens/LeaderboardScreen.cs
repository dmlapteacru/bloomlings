using System;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The Leaderboard of the design board's frame 5 (spec 002 FR-023; FR-062, T141). Open from L10, it shows the
    /// player's global rank by highest completed level:
    /// <list type="bullet">
    /// <item><description>the top ranks with gold, silver and bronze medals;</description></item>
    /// <item><description>avatar circles and names, with each row's score (the highest completed level);</description></item>
    /// <item><description>a gap marker, then the player's neighbours, with the player's own row highlighted as "You".</description></item>
    /// </list>
    /// Offline, the last rank read stays on screen with a notice. The player's own row carries their frame, badge and
    /// marker (FR-061 prestige rewards).
    /// </summary>
    public sealed class LeaderboardScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private RectTransform _list = null!;
        private TextMeshProUGUI _status = null!;

        public bool IsOpen => _root.activeSelf;

        public static LeaderboardScreen Create(Transform parent, Action onRefresh)
        {
            CardView card = UiKit.Card("Leaderboard", parent, Loc.T("leaderboard.title"), 1100f, null);
            var screen = card.Root.AddComponent<LeaderboardScreen>();
            screen._root = card.Root;
            Button close = UiKit.RoundIconButton("Close", card.CardRect, "ui.close", screen.Hide);
            UiKit.PlaceBox((RectTransform)close.transform, card.Regions.Close, card.Regions.Card);
            screen._list = UiFactory.Place(UiFactory.CreateRect("Rows", card.Body), 0f, 0.16f, 1f, 1f);
            screen._status = UiKit.Label("Status", card.Body, string.Empty, DesignTokens.Type.Caption, UiTheme.TextSecondary);
            UiFactory.Place(screen._status.rectTransform, 0f, 0.1f, 1f, 0.16f);
            Button refresh = UiKit.SecondaryButton("Refresh", card.Body, Loc.T("leaderboard.refresh"), onRefresh, "ui.restart");
            UiFactory.Place((RectTransform)refresh.transform, 0.25f, 0f, 0.75f, 0.09f);
            card.Root.SetActive(false);
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
            int slots = page.Entries.Count + 1;
            float row = 1f / Mathf.Max(9, slots);
            int line = 0;
            int previous = 0;
            foreach (LeaderboardEntry entry in page.Entries)
            {
                if (previous > 0 && entry.Rank > previous + 1)
                {
                    // The gap between the top ranks and the player's neighbourhood.
                    TextMeshProUGUI gap = UiKit.Label("Gap", _list, "…", DesignTokens.Type.Title, UiTheme.TextSecondary);
                    float gapTop = 1f - (line * row);
                    UiFactory.Place(gap.rectTransform, 0f, gapTop - row, 1f, gapTop);
                    line++;
                }

                previous = entry.Rank;
                float top = 1f - (line * row);
                line++;
                Image background = UiKit.Rounded("Row", _list, entry.IsPlayer ? UiTheme.Of(DesignTokens.Colors.SurfaceRowHighlight) : Color.white, 28f);
                UiFactory.Place(background.rectTransform, 0f, top - row + 0.008f, 1f, top - 0.008f);
                Rgba? medal = DesignTokens.Colors.Medal(entry.Rank);
                if (medal.HasValue)
                {
                    Image badge = UiFactory.CreateImage("Medal", background.transform, ProceduralSprites.Shape("ui.medal"), UiTheme.Of(medal.Value));
                    badge.preserveAspect = true;
                    UiFactory.Place(badge.rectTransform, 0.02f, 0.08f, 0.14f, 0.92f);
                }

                TextMeshProUGUI rank = UiKit.Label("Rank", background.transform, NumberText.Group(entry.Rank), DesignTokens.Type.Body, UiTheme.Text);
                UiFactory.Place(rank.rectTransform, medal.HasValue ? 0.02f : 0.01f, 0f, medal.HasValue ? 0.14f : 0.16f, medal.HasValue ? 0.6f : 1f);
                Image avatar = UiFactory.CreateImage("Avatar", background.transform, ProceduralSprites.Shape("ui.person"), UiTheme.Stuck);
                avatar.preserveAspect = true;
                UiFactory.Place(avatar.rectTransform, 0.17f, 0.12f, 0.27f, 0.88f);
                TextMeshProUGUI name = UiKit.Label("Name", background.transform, entry.IsPlayer ? Loc.T("leaderboard.you") : Short(entry.Name), DesignTokens.Type.Body, UiTheme.Text, TextAlignmentOptions.Left);
                UiFactory.Place(name.rectTransform, 0.3f, 0f, 0.62f, 1f);
                if (entry.IsPlayer && own != null)
                {
                    Decorate(background.transform, own.Marker, 0.62f);
                    Decorate(background.transform, own.Badge, 0.7f);
                    if (own.Frame != null)
                    {
                        Image frame = UiFactory.CreateImage("Frame", background.transform, ProceduralSprites.RoundedSquare, BloomlingFigure.Tint(own.Frame));
                        frame.type = Image.Type.Sliced;
                        frame.fillCenter = false;
                        UiFactory.Stretch(frame.rectTransform);
                    }
                }

                TextMeshProUGUI level = UiKit.Label("Score", background.transform, NumberText.Group(entry.Level), DesignTokens.Type.Count, UiTheme.Text, TextAlignmentOptions.Right);
                level.outlineWidth = 0f;
                UiFactory.Place(level.rectTransform, 0.78f, 0f, 0.96f, 1f);
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
