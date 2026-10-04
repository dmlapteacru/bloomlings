using System;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

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
    /// marker (FR-061 prestige rewards). In the reference look of spec 005 (contracts/look.md §4.6; the playtest's
    /// <c>MetaCards.Leaderboard</c>): a parchment card under a wooden sign, cream rows (the player's raised and green),
    /// outlined medals, portraits on cream discs, brown names and scores, and the cream Refresh with ⟳. Before the
    /// Leaderboard unlocks (L10) the bottom menu's Leaderboard opens its locked card instead (<see cref="ShowLocked"/>; spec
    /// 005 FR-030, the playtest's <c>MetaCards.LockedCard</c>): the title, the close button and the locked notice
    /// ("Available from level 10").
    /// </summary>
    public sealed class LeaderboardScreen : MonoBehaviour
    {
        private const int Lines = 9;
        private const float RowUnits = 96f;
        private const float RowGapUnits = 14f;
        private const float StatusUnits = 50f;

        private GameObject _root = null!;
        private CardView _locked = null!;
        private LockedNoticeView _notice = null!;
        private RectTransform _list = null!;
        private Box _listBox;
        private TextMeshProUGUI _status = null!;

        /// <summary>Whether the ranks card is open (the locked card shows no ranks to refresh).</summary>
        public bool IsOpen => _root.activeSelf;

        public static LeaderboardScreen Create(Transform parent, Action onRefresh)
        {
            float content = (Lines * (RowUnits + RowGapUnits)) + StatusUnits + DesignTokens.Size.SecondaryHeight + 20f;
            LeaderboardScreen screen = null!;
            CardView card = UiKit.Card("Leaderboard", parent, Loc.T("leaderboard.title"), content, () => screen.Hide(), sign: SignDecor.None);
            screen = card.Root.AddComponent<LeaderboardScreen>();
            screen._root = card.Root;
            Box body = card.Regions.Body;
            float u = Scale;
            screen._listBox = new Box(body.Left, body.Top, body.Right, body.Top + (Lines * (RowUnits + RowGapUnits) * u));
            screen._list = UiKit.PlaceBox(UiFactory.CreateRect("Rows", card.Body), screen._listBox, body);
            float y = screen._listBox.Bottom;
            screen._status = UiKit.Label("Status", card.Body, string.Empty, T.Caption, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(screen._status.rectTransform, new Box(body.Left, y, body.Right, y + (StatusUnits * u)), body);
            y += (StatusUnits + 10f) * u;
            Button refresh = UiKit.SecondaryButton("Refresh", card.Body, Loc.T("leaderboard.refresh"), onRefresh, "ui.restart");
            UiKit.PlaceBox((RectTransform)refresh.transform, ScreenLayout.CardButton(body, y, false, u), body);
            card.Root.SetActive(false);

            // The locked card (FR-030): the title, the close button and the locked notice filling its body.
            CardView locked = UiKit.Card("LeaderboardLocked", parent, Loc.T("leaderboard.title"), LockedNoticeRegions.CardContent, () => screen.Hide(), sign: SignDecor.None);
            screen._locked = locked;
            screen._notice = UiKit.LockedNotice("Locked", locked.Body);
            UiFactory.Stretch((RectTransform)screen._notice.transform);
            locked.Root.SetActive(false);
            return screen;
        }

        private static float Scale => DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);

        /// <param name="own">The player's frame, badge and marker, drawn on their own row (others' are not known offline).</param>
        public void Show(LeaderboardPage? page, bool stale, ProfileLook? own = null)
        {
            _locked.Root.SetActive(false);
            _root.SetActive(true);
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

            // The lines: every entry, and a gap marker where the ranks jump (between the top and the player's neighbours).
            int lines = 0;
            int previous = 0;
            foreach (LeaderboardEntry entry in page.Entries)
            {
                lines += previous > 0 && entry.Rank > previous + 1 ? 2 : 1;
                previous = entry.Rank;
            }

            float u = Scale;
            float pitch = Mathf.Min((RowUnits + RowGapUnits) * u, _listBox.Height / Mathf.Max(1, lines));
            float row = pitch * RowUnits / (RowUnits + RowGapUnits);
            int line = 0;
            previous = 0;
            foreach (LeaderboardEntry entry in page.Entries)
            {
                if (previous > 0 && entry.Rank > previous + 1)
                {
                    // The gap between the top ranks and the player's neighbourhood.
                    TextMeshProUGUI gap = UiKit.Label("Gap", _list, "…", T.Title, UiTheme.Of(C.InkBrownSoft));
                    float gapTop = _listBox.Top + (line * pitch);
                    UiKit.PlaceBox(gap.rectTransform, new Box(_listBox.Left, gapTop, _listBox.Right, gapTop + row), _listBox);
                    line++;
                }

                previous = entry.Rank;
                float top = _listBox.Top + (line * pitch);
                line++;
                Row(entry, new Box(_listBox.Left, top, _listBox.Right, top + row), entry.IsPlayer ? own : null, u);
            }

            _root.SetActive(true);
        }

        /// <summary>
        /// Shows the locked card (spec 005 FR-030, contracts/look.md §6.7): the Leaderboard's title, its close button and the
        /// locked notice of the Leaderboard, available from <paramref name="level"/> (the roadmap's,
        /// <see cref="BottomNav.UnlockLevel"/>).
        /// </summary>
        public void ShowLocked(int level)
        {
            _root.SetActive(false);
            _locked.Root.SetActive(true);
            _notice.Show(NavPlace.Leaderboard, level);
        }

        public void Hide()
        {
            _root.SetActive(false);
            _locked.Root.SetActive(false);
        }

        /// <summary>
        /// One rank's row (the playtest's <c>MetaCards.Leaderboard</c>): the medal (gold, silver, bronze) with its number or
        /// the plain rank, the portrait on a cream disc, the name, the player's marker and badge, and the score.
        /// </summary>
        private void Row(LeaderboardEntry entry, Box line, ProfileLook? own, float u)
        {
            Image background = UiKit.Row("Row", _list, entry.IsPlayer);
            UiKit.PlaceBox(background.rectTransform, line, _listBox);
            Transform row = background.transform;
            float cy = line.CenterY;
            float cx = line.Left + (70f * u);
            string number = NumberText.Group(entry.Rank);
            Rgba? medal = C.Medal(entry.Rank);
            if (medal.HasValue)
            {
                Image badge = UiKit.OutlinedIcon("Medal", row, "ui.medal", medal.Value, medal.Value.Darken(0.42f), 0.06f);
                UiKit.PlaceBox(badge.rectTransform, Box.FromCenter(cx, cy, 72f * u, 72f * u), line);
                TextMeshProUGUI digits = UiKit.Label("Rank", row, number, T.Badge, UiTheme.Of(medal.Value.Darken(0.55f)));
                UiKit.PlaceBox(digits.rectTransform, Box.FromCenter(cx, cy + (8f * u), 52f * u, 36f * u), line);
            }
            else
            {
                TextMeshProUGUI rank = UiKit.Label("Rank", row, number, T.Body, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
                UiKit.PlaceBox(rank.rectTransform, Box.FromCenter(cx, cy, 120f * u, line.Height), line);
            }

            // The player's own row shows their family's 3D hero, small (spec 004 FR-017); other gardeners a person.
            float portrait = Mathf.Min(68f * u, line.Height * 0.72f);
            Portrait(row, line, Box.FromCenter(line.Left + (170f * u), cy, portrait, portrait), entry.IsPlayer, u);

            TypeStyle nameStyle = entry.IsPlayer ? T.ButtonSecondary : T.Body;
            TextMeshProUGUI name = UiKit.Label("Name", row, entry.IsPlayer ? Loc.T("leaderboard.you") : Short(entry.Name), nameStyle, UiTheme.Of(C.InkBrown), TextAlignmentOptions.Left, TextLook.Plain(C.InkBrown));
            float nameLeft = line.Left + (230f * u);
            UiKit.PlaceBox(name.rectTransform, new Box(nameLeft, line.Top, line.Left + (line.Width * 0.62f), line.Bottom), line);
            if (own != null)
            {
                Decorate(row, line, own.Marker, line.Left + (line.Width * 0.66f));
                Decorate(row, line, own.Badge, line.Left + (line.Width * 0.74f));
                if (own.Frame != null)
                {
                    float width = Mathf.Max(UiKit.Units(3f), UiKit.Units(DesignTokens.Garden.OutlineWidth) * 1.4f);
                    Image frame = UiKit.RoundRing("Frame", row, BloomlingFigure.Tint(own.Frame), b => b.Height * DesignTokens.Radius.Row, _ => width);
                    UiFactory.Stretch(frame.rectTransform);
                }
            }

            TextMeshProUGUI score = UiKit.Label("Score", row, NumberText.Group(entry.Level), T.Count, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
            UiKit.PlaceBox(score.rectTransform, Box.FromCenter(line.Right - (90f * u), cy, 160f * u, line.Height), line);
        }

        /// <summary>
        /// A round portrait on a cream disc (the playtest's <c>MetaCards.Portrait</c>): the <c>cream.lip</c> below, a
        /// <c>cream.line</c> ring, the cream face, and the player's hero or the anonymous figure of another gardener.
        /// </summary>
        private static void Portrait(Transform row, Box line, Box face, bool player, float u)
        {
            float ring = Mathf.Max(1f, 3f * u);
            Image lip = UiKit.RoundRect("PortraitLip", row, UiTheme.Of(C.CreamLip));
            UiKit.PlaceBox(lip.rectTransform, face.Inset(-ring).Offset(0f, ring * 0.8f), line);
            Image edge = UiKit.RoundRect("PortraitLine", row, UiTheme.Of(C.CreamLine));
            UiKit.PlaceBox(edge.rectTransform, face.Inset(-ring), line);
            Image disc = UiKit.RoundGradient("Portrait", row, C.CreamTop, C.CreamFace);
            UiKit.PlaceBox(disc.rectTransform, face, line);
            Sprite? hero = player ? CharacterSprites.Hero(Family.Bloom, blank: false) : null;
            if (hero != null)
            {
                Image picture = UiFactory.CreateImage("Hero", row, hero, Color.white);
                picture.preserveAspect = true;
                UiKit.PlaceBox(picture.rectTransform, Box.FromCenter(face.CenterX, face.CenterY + (face.Height * 0.02f), face.Width * 0.92f, face.Height * 0.92f), line);
            }
            else
            {
                Image person = UiFactory.CreateImage("Person", row, ProceduralSprites.Shape("ui.person"), UiTheme.Of(C.CreamLine));
                person.preserveAspect = true;
                UiKit.PlaceBox(person.rectTransform, Box.FromCenter(face.CenterX, face.CenterY + (face.Height * 0.04f), face.Width * 0.7f, face.Height * 0.7f), line);
            }
        }

        private static void Decorate(Transform row, Box line, CosmeticItem? item, float x)
        {
            if (item == null)
            {
                return;
            }

            Image icon = UiFactory.CreateImage(item.Kind.ToString(), row, ProceduralSprites.Accessory(item.Shape), BloomlingFigure.Tint(item));
            icon.preserveAspect = true;
            float size = line.Height * 0.6f;
            UiKit.PlaceBox(icon.rectTransform, Box.FromCenter(x, line.CenterY, size, size), line);
        }

        private static string Short(string name) => string.IsNullOrEmpty(name) ? Loc.T("leaderboard.anonymous") : (name.Length > 16 ? name.Substring(0, 16) : name);
    }
}
