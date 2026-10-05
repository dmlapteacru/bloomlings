using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Profile;
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
    /// marker (FR-061 prestige rewards).
    /// <para>
    /// A full-screen page since the owner's request of 2026-10-04 ("All the menu's places must be a separate page. Not
    /// popups."; spec 005 FR-030, contracts/look.md §6.8), every element placed from
    /// <see cref="ScreenLayout.ReferenceLeaderboard"/>, as the playtest's <c>LeaderboardScreen</c>: over the Wardrobe's
    /// garden, the page header on one line (<see cref="UiKit.PageHeader"/>: the back button, the wooden "Leaderboard" banner
    /// with ivy, the Petals pill, whose "+" opens the Store page over it once the Store is open); a parchment panel to the
    /// bottom of the screen with cream rows (the player's raised and green), outlined medals, portraits on cream discs,
    /// brown names and scores, as many lines as the page fits around the player's own row, the offline line and the cream
    /// Refresh with ⟳; and the bottom menu over the panel's foot, the Leaderboard in its medallion. It lies over Home; its
    /// back hides it, so Home shows again.
    /// </para>
    /// <para>
    /// Before the Leaderboard unlocks (L10) the bottom menu's Leaderboard opens the page locked (<see cref="ShowLocked"/>,
    /// the playtest's <c>LeaderboardScreen.Locked</c>): the same garden, header and panel, the panel holding the locked
    /// notice (<see cref="LockedNoticeView"/>: "Available from level 10") instead of the ranks, the offline line and
    /// Refresh.
    /// </para>
    /// </summary>
    public sealed class LeaderboardScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private PageHeaderView _header = null!;
        private Image _panel = null!;
        private RectTransform _list = null!;
        private TextMeshProUGUI _status = null!;
        private RectTransform _refresh = null!;
        private LockedNoticeView _notice = null!;
        private BottomNavView? _nav;
        private Func<HomeLook>? _navLook;
        private Func<long>? _petalsSource;
        private bool _store;
        private bool _plus;
        private long _petalsShown = -1;
        private bool _locked;
        private ReferenceLeaderboardRegions _regions = null!;
        private AvatarItem? _ownAvatar;

        /// <summary>Whether the page shows, locked or not.</summary>
        public bool IsOpen => _root.activeSelf;

        /// <summary>Whether the page shows the ranks (open and not locked), so a new rank read shows at once.</summary>
        public bool ShowsRanks => _root.activeSelf && !_locked;

        /// <param name="onRefresh">The Refresh button: reads the ranks again in the background.</param>
        /// <param name="petals">The Petals balance for the header's pill; null hides the pill.</param>
        /// <param name="onStore">Opens the Store page from the pill's "+" (once the Store is open); null shows no "+".</param>
        /// <param name="onNav">A tap on another place of the bottom menu (spec 005 FR-030); null shows no menu.</param>
        /// <param name="navLook">The look that tells which places of the bottom menu are open (<see cref="BottomNav.IsOpen"/>); null: all of them.</param>
        public static LeaderboardScreen Create(Transform parent, Action onRefresh, Func<long>? petals = null, Action? onStore = null, Action<NavPlace>? onNav = null, Func<HomeLook>? navLook = null)
        {
            // A full screen over Home that takes every tap, on the Wardrobe's garden (the owner's picture B7, or the drawn
            // one), as the other pages.
            Image shade = UiFactory.CreateImage("Leaderboard", parent, null, Color.clear, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var screen = shade.gameObject.AddComponent<LeaderboardScreen>();
            screen._root = shade.gameObject;
            screen._petalsSource = petals;
            screen._store = onStore != null;
            Transform root = shade.transform;
            BackdropView.Create(shade.rectTransform, OwnerPictures.Wardrobe, BackdropScene.Home);

            // The parchment panel (a card's radius), the rows' area, the offline line and Refresh.
            screen._panel = UiKit.Paper("Panel", root, b => Mathf.Max(UiKit.Units(DesignTokens.Radius.CardMin), b.Width * DesignTokens.Radius.Card), DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard, raycast: false);
            screen._list = UiFactory.CreateRect("Rows", root);
            screen._status = UiKit.Label("Status", root, string.Empty, T.Caption, UiTheme.Of(C.InkBrownSoft));
            Button refresh = UiKit.SecondaryButton("Refresh", root, Loc.T("leaderboard.refresh"), onRefresh, "ui.restart");
            screen._refresh = (RectTransform)refresh.transform;

            // The locked notice in the rows' place, shown only before the Leaderboard unlocks (FR-030).
            screen._notice = UiKit.LockedNotice("Locked", root);
            screen._notice.gameObject.SetActive(false);

            // The bottom menu over the panel's foot, the Leaderboard in its medallion (FR-030).
            if (onNav != null)
            {
                screen._navLook = navLook;
                screen._nav = UiKit.BottomNav("BottomNav", root, place =>
                {
                    if (place != NavPlace.Leaderboard)
                    {
                        onNav(place);
                    }
                });
            }

            // The header last, as on the other pages.
            screen._header = UiKit.PageHeader(root, Loc.T("leaderboard.title"), screen.Hide, petals != null, onStore);
            shade.gameObject.SetActive(false);
            return screen;
        }

        /// <param name="own">The player's frame, badge and marker, drawn on their own row (others' are not known offline).</param>
        /// <param name="avatar">The player's avatar picture (spec 005 FR-037) on their own row; none shows the profile hero.</param>
        public void Show(LeaderboardPage? page, bool stale, ProfileLook? own = null, AvatarItem? avatar = null)
        {
            _ownAvatar = avatar;
            _root.SetActive(true);
            _locked = false;
            SyncPlus();
            Layout();
            ClearRows();
            ReferenceLeaderboardRegions r = _regions;
            if (page == null || page.Entries.Count == 0)
            {
                // An empty board's line in the middle of the rows' box.
                _status.text = stale ? Loc.T("leaderboard.offline_empty") : Loc.T("leaderboard.empty");
                UiKit.PlaceScreen(_status.rectTransform, r.Empty);
                return;
            }

            _status.text = stale ? Loc.T("leaderboard.offline") : string.Empty;
            UiKit.PlaceScreen(_status.rectTransform, r.Status);

            // The lines: every entry, and a gap marker (null) where the ranks jump (between the top and the player's
            // neighbours); as many as fit, around the player's own row.
            var lines = new List<LeaderboardEntry?>();
            int previous = 0;
            int focus = -1;
            foreach (LeaderboardEntry entry in page.Entries)
            {
                if (previous > 0 && entry.Rank > previous + 1)
                {
                    lines.Add(null);
                }

                if (entry.IsPlayer)
                {
                    focus = lines.Count;
                }

                lines.Add(entry);
                previous = entry.Rank;
            }

            int shown = r.LinesShown(lines.Count);
            int first = ReferenceLeaderboardRegions.FirstLine(lines.Count, shown, focus);
            float grow = r.RowHeight(lines.Count) / (r.W * ReferenceLeaderboardRegions.RowTypeShare);
            for (int i = 0; i < shown; i++)
            {
                LeaderboardEntry? entry = lines[first + i];
                Box line = r.Row(i, lines.Count);
                if (entry == null)
                {
                    // The gap between the top ranks and the player's neighbourhood.
                    TextMeshProUGUI gap = UiKit.Label("Gap", _list, "…", T.Title, UiTheme.Of(C.InkBrownSoft));
                    Grow(gap, T.Title, grow);
                    UiKit.PlaceBox(gap.rectTransform, line, r.Rows);
                    continue;
                }

                Row(entry, line, entry.IsPlayer ? own : null, grow);
            }
        }

        /// <summary>
        /// Shows the page locked (spec 005 FR-030, contracts/look.md §6.7; <see cref="ScreenLayout.LockedPage"/>): the page
        /// header as usual, the panel holding the locked notice of the Leaderboard, available from <paramref name="level"/>
        /// (the roadmap's, <see cref="BottomNav.UnlockLevel"/>), and the bottom menu with the Leaderboard raised; the rows,
        /// the offline line and Refresh hide. Its back hides it as usual.
        /// </summary>
        public void ShowLocked(int level)
        {
            _root.SetActive(true);
            _locked = true;
            SyncPlus();
            ClearRows();
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            LockedPageRegions r = ScreenLayout.LockedPage(w, h, insets);
            _header.Place(r.Header);
            float radius = r.PanelRadius(DesignTokens.ScaleFor(w, h));
            UiKit.PlaceScreen(_panel.rectTransform, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            _list.gameObject.SetActive(false);
            _status.gameObject.SetActive(false);
            _refresh.gameObject.SetActive(false);
            _notice.gameObject.SetActive(true);
            UiKit.PlaceScreen((RectTransform)_notice.transform, r.Notice);
            _notice.Show(NavPlace.Leaderboard, level);
            _nav?.Show(NavPlace.Leaderboard, _navLook?.Invoke() ?? HomeLook.All);
        }

        public void Hide() => _root.SetActive(false);

        private void Update()
        {
            // The balance follows the economy (a purchase in the Store opened from the "+").
            if (_header.Petals != null && _petalsSource != null)
            {
                long now = _petalsSource();
                if (now != _petalsShown)
                {
                    _header.Petals.Show(now, _plus);
                    _petalsShown = now;
                }
            }
        }

        /// <summary>Whether the Petals pill shows its "+" as the page opens: with a Store to open, once the Store is open (as on Home).</summary>
        private void SyncPlus()
        {
            _plus = _store && (_navLook == null || _navLook().Store);
            _petalsShown = -1;
        }

        /// <summary>Places the page on the kit's regions for the screen's shape (contracts/look.md §6.8).</summary>
        private void Layout()
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            ReferenceLeaderboardRegions r = ScreenLayout.ReferenceLeaderboard(w, h, insets);
            _regions = r;
            _header.Place(r.Header);

            // The panel runs to the bottom of the screen: its bottom corners go past the edge.
            float radius = r.PanelRadius(DesignTokens.ScaleFor(w, h));
            UiKit.PlaceScreen(_panel.rectTransform, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            _notice.gameObject.SetActive(false);
            _list.gameObject.SetActive(true);
            UiKit.PlaceScreen(_list, r.Rows);
            _status.gameObject.SetActive(true);
            UiKit.PlaceScreen(_status.rectTransform, r.Status);
            _refresh.gameObject.SetActive(true);
            UiKit.PlaceScreen(_refresh, r.Refresh);
            _nav?.Show(NavPlace.Leaderboard, _navLook?.Invoke() ?? HomeLook.All);
        }

        private void ClearRows()
        {
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                Destroy(_list.GetChild(i).gameObject);
            }
        }

        /// <summary>A label's letters grown with its row (<paramref name="grow"/>: the row's height over a <see cref="ReferenceLeaderboardRegions.RowTypeShare"/> row's).</summary>
        private static void Grow(TextMeshProUGUI label, TypeStyle style, float grow)
        {
            label.fontSizeMax = UiKit.Units(style.Size * grow);
            label.fontSize = label.fontSizeMax;
        }

        /// <summary>
        /// One rank's row in its parts (<see cref="ReferenceLeaderboardRegions.Parts"/>; the playtest's
        /// <c>LeaderboardScreen</c>): the medal (gold, silver, bronze) with its number or the plain rank, the portrait on a
        /// cream disc, the name, the player's marker and badge, and the score.
        /// </summary>
        private void Row(LeaderboardEntry entry, Box line, ProfileLook? own, float grow)
        {
            Image background = UiKit.Row("Row", _list, entry.IsPlayer);
            UiKit.PlaceBox(background.rectTransform, line, _regions.Rows);
            Transform row = background.transform;
            LeaderboardRowParts parts = ReferenceLeaderboardRegions.Parts(line);
            string number = NumberText.Group(entry.Rank);
            Rgba? medal = C.Medal(entry.Rank);
            if (medal.HasValue)
            {
                Image badge = UiKit.OutlinedIcon("Medal", row, "ui.medal", medal.Value, medal.Value.Darken(0.42f), 0.06f);
                UiKit.PlaceBox(badge.rectTransform, parts.Medal, line);
                TextMeshProUGUI digits = UiKit.Label("Rank", row, number, T.Badge, UiTheme.Of(medal.Value.Darken(0.55f)));
                Grow(digits, T.Badge, grow);
                UiKit.PlaceBox(digits.rectTransform, Box.FromCenter(parts.Medal.CenterX, parts.Medal.CenterY + (parts.Medal.Height * 0.11f), parts.Medal.Width * 0.72f, parts.Medal.Height * 0.5f), line);
            }
            else
            {
                TextMeshProUGUI rank = UiKit.Label("Rank", row, number, T.Body, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
                Grow(rank, T.Body, grow);
                UiKit.PlaceBox(rank.rectTransform, parts.Rank, line);
            }

            // The player's own row shows their family's 3D hero, small (spec 004 FR-017); other gardeners a person.
            Portrait(row, line, parts.Portrait, entry.IsPlayer, entry.IsPlayer ? _ownAvatar : null);

            TypeStyle nameStyle = entry.IsPlayer ? T.ButtonSecondary : T.Body;
            TextMeshProUGUI name = UiKit.Label("Name", row, entry.IsPlayer ? Loc.T("leaderboard.you") : Short(entry.Name), nameStyle, UiTheme.Of(C.InkBrown), TextAlignmentOptions.Left, TextLook.Plain(C.InkBrown));
            Grow(name, nameStyle, grow);
            UiKit.PlaceBox(name.rectTransform, parts.Name, line);
            if (own != null)
            {
                Decorate(row, line, own.Marker, parts.Marker);
                Decorate(row, line, own.Badge, parts.Badge);
                if (own.Frame != null)
                {
                    float width = Mathf.Max(UiKit.Units(3f), UiKit.Units(DesignTokens.Garden.OutlineWidth) * 1.4f);
                    Image frame = UiKit.RoundRing("Frame", row, BloomlingFigure.Tint(own.Frame), b => b.Height * DesignTokens.Radius.Row, _ => width);
                    UiFactory.Stretch(frame.rectTransform);
                }
            }

            TextMeshProUGUI score = UiKit.Label("Score", row, NumberText.Group(entry.Level), T.Count, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
            Grow(score, T.Count, grow);
            UiKit.PlaceBox(score.rectTransform, parts.Score, line);
        }

        /// <summary>
        /// A round portrait on a cream disc (the playtest's <c>LeaderboardScreen.Portrait</c>): the <c>cream.lip</c> below, a
        /// <c>cream.line</c> ring, the cream face, and the player's hero or the anonymous figure of another gardener.
        /// </summary>
        private static void Portrait(Transform row, Box line, Box face, bool player, AvatarItem? avatar)
        {
            float ring = Mathf.Max(1f, face.Width * 0.044f);
            Image lip = UiKit.RoundRect("PortraitLip", row, UiTheme.Of(C.CreamLip));
            UiKit.PlaceBox(lip.rectTransform, face.Inset(-ring).Offset(0f, ring * 0.8f), line);
            Image edge = UiKit.RoundRect("PortraitLine", row, UiTheme.Of(C.CreamLine));
            UiKit.PlaceBox(edge.rectTransform, face.Inset(-ring), line);
            Image disc = UiKit.RoundGradient("Portrait", row, C.CreamTop, C.CreamFace);
            UiKit.PlaceBox(disc.rectTransform, face, line);
            // The player's avatar picture in a round mask (spec 005 FR-037), else their hero.
            Texture2D? own = avatar == null ? null : OwnerArt.Avatar(avatar.Picture);
            if (own != null)
            {
                Image mask = UiFactory.CreateImage("AvatarMask", row, ProceduralSprites.Circle, Color.white);
                mask.raycastTarget = false;
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                UiKit.PlaceBox(mask.rectTransform, face, line);
                RawImage picture = UiFactory.CreateRect("Avatar", mask.transform).gameObject.AddComponent<RawImage>();
                picture.texture = own;
                picture.raycastTarget = false;
                UiFactory.Stretch(picture.rectTransform);
                return;
            }

            Sprite? hero = player ? CharacterSprites.Hero(avatar?.Family ?? Family.Bloom, blank: false) : null;
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

        /// <summary>The player's marker or badge in its part of their own row (<see cref="LeaderboardRowParts.Marker"/>, <see cref="LeaderboardRowParts.Badge"/>).</summary>
        private static void Decorate(Transform row, Box line, CosmeticItem? item, Box box)
        {
            if (item == null)
            {
                return;
            }

            Image icon = UiFactory.CreateImage(item.Kind.ToString(), row, ProceduralSprites.Accessory(item.Shape), BloomlingFigure.Tint(item));
            icon.preserveAspect = true;
            UiKit.PlaceBox(icon.rectTransform, box, line);
        }

        private static string Short(string name) => string.IsNullOrEmpty(name) ? Loc.T("leaderboard.anonymous") : (name.Length > 16 ? name.Substring(0, 16) : name);
    }
}
