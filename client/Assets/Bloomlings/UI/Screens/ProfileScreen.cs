using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// What the profile page counts: levels won, pictures collected, milestones reached (its stat cells) and Daily
    /// Challenges won (an achievement's).
    /// </summary>
    public readonly struct ProfileStats
    {
        public ProfileStats(long levelsWon, int pictures, int milestones, long dailyWon = 0)
        {
            LevelsWon = levelsWon;
            Pictures = pictures;
            Milestones = milestones;
            DailyWon = dailyWon;
        }

        public long LevelsWon { get; }

        public int Pictures { get; }

        public int Milestones { get; }

        public long DailyWon { get; }
    }

    /// <summary>
    /// The profile page (spec 005 FR-037; the playtest's <c>ProfileScreen</c>), every element placed from
    /// <see cref="ScreenLayout.ReferenceProfile"/>: over the Wardrobe's garden, the page header (back, the wooden "Profile"
    /// banner, the Petals pill whose "+" opens the Store once it is open) and the wooden-framed cream panel (spec 005 FR-047); on it the player's card
    /// (the round avatar in its frame and badge, a tap opening the edit card on Avatar; the name with the pencil, opening it
    /// on Name; the short ID; "Playing since 10/2026"; the wooden "Level N" plaque), three stat cells (levels won,
    /// pictures, Daily wins) and three illustrated achievements per page. It
    /// lies over Home (Home's avatar opens it, <c>HomeFeatureActions.OnProfile</c>); its back hides it. It has no bottom
    /// menu: it is not a menu place.
    /// </summary>
    public sealed class ProfileScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private PageHeaderView _header = null!;
        private Image _panel = null!;
        private Image[] _flowers = null!;
        private Image _card = null!;
        private ProfileAvatar _avatar = null!;
        private RectTransform _avatarButton = null!;
        private TextMeshProUGUI _name = null!;
        private RectTransform _edit = null!;
        private TextMeshProUGUI _id = null!;
        private TextMeshProUGUI _joined = null!;
        private WoodSignView _plaque = null!;
        private readonly Image[] _stats = new Image[3];
        private readonly TextMeshProUGUI[] _statValues = new TextMeshProUGUI[3];
        private readonly TextMeshProUGUI[] _statLabels = new TextMeshProUGUI[3];
        private WoodSignView _title = null!;
        private readonly RectTransform[] _tiles = new RectTransform[ReferenceProfileRegions.AchievementCount];
        private readonly TextMeshProUGUI[] _tileLabels = new TextMeshProUGUI[ReferenceProfileRegions.AchievementCount];
        private readonly (Image Badge, TextMeshProUGUI Count, GameObject Lock, GameObject Check)[] _tileParts = new (Image, TextMeshProUGUI, GameObject, GameObject)[ReferenceProfileRegions.AchievementCount];
        private TextMeshProUGUI _note = null!;
        private RectTransform _previous = null!;
        private RectTransform _next = null!;
        private PlayerSave _save = null!;
        private Action? _onChanged;
        private int _page;
        private string? _message;
        private ProfileEditCard _editCard = null!;
        private ProfileService _profile = null!;
        private WardrobeService _wardrobe = null!;
        private Func<int> _level = () => 1;
        private Func<ProfileStats> _statsSource = () => default;
        private Func<Family, Outfit>? _outfitOf;
        private Func<long>? _petalsSource;
        private Func<HomeLook>? _navLook;
        private bool _store;
        private bool _plus;
        private long _petalsShown = -1;

        public bool IsOpen => _root.activeSelf;

        /// <param name="level">The level Home shows (the plaque's).</param>
        /// <param name="stats">The stat cells' counts.</param>
        /// <param name="wardrobeLevel">The level the Wardrobe opens at (the edit card's locked Frame and Badge tabs).</param>
        /// <param name="outfitOf">What the avatar's hero wears while its picture is missing (once the Wardrobe is open).</param>
        /// <param name="petals">The Petals balance for the header's pill; null hides the pill.</param>
        /// <param name="onStore">Opens the Store page from the pill's "+" (once the Store is open); null shows no "+".</param>
        /// <param name="navLook">Tells whether the Store is open (<see cref="HomeLook.Store"/>) for the pill's "+".</param>
        /// <param name="onChanged">The profile changed (Home redraws its avatar).</param>
        public static ProfileScreen Create(
            Transform parent,
            ProfileService profile,
            WardrobeService wardrobe,
            PlayerSave save,
            Func<int> level,
            Func<ProfileStats> stats,
            int wardrobeLevel,
            Func<Family, Outfit>? outfitOf = null,
            Func<long>? petals = null,
            Action? onStore = null,
            Func<HomeLook>? navLook = null,
            Action? onChanged = null)
        {
            // A full screen over Home that takes every tap, on the Wardrobe's garden, as the other pages.
            Image shade = UiFactory.CreateImage("Profile", parent, null, Color.clear, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var screen = shade.gameObject.AddComponent<ProfileScreen>();
            screen._root = shade.gameObject;
            screen._profile = profile;
            screen._wardrobe = wardrobe;
            screen._save = save;
            screen._onChanged = onChanged;
            screen._level = level;
            screen._statsSource = stats;
            screen._outfitOf = outfitOf;
            screen._petalsSource = petals;
            screen._store = onStore != null;
            screen._navLook = navLook;
            Transform root = shade.transform;
            BackdropView.Create(shade.rectTransform, OwnerPictures.Wardrobe, BackdropScene.Home);
            screen._panel = UiKit.CardFrame("Panel", root, raycast: false);

            // The player's card.
            screen._card = UiKit.Row("Card", root, false);
            screen._avatar = ProfileAvatar.Create("Avatar", root);
            Image avatarHit = UiFactory.CreateImage("AvatarButton", root, null, Color.clear, raycast: true);
            UiKit.TapTarget(avatarHit, () => screen._editCard.Show(ProfileTab.Avatar), press: true);
            screen._avatarButton = avatarHit.rectTransform;
            screen._name = UiKit.Label("Name", root, string.Empty, T.Title, UiTheme.Of(C.InkTitle), TextAlignmentOptions.Left, TextLook.Plain(C.InkTitle));
            screen._edit = (RectTransform)UiKit.RoundIconButton("Edit", root, "ui.edit", () => screen._editCard.Show(ProfileTab.Name)).transform;
            screen._id = UiKit.Label("Id", root, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft), TextAlignmentOptions.Left);
            screen._joined = UiKit.Label("Joined", root, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft), TextAlignmentOptions.Left);
            // No flowers in the page's content (spec 005 FR-049, the owner, 2026-10-08): the plaque, the title sign, the avatar
            // and the tiles stand plain; only the page's frame keeps its corner flowers, as every page's.
            screen._plaque = UiKit.WoodSign("Level", root, Loc.F("common.level", 1), T.LevelPill);

            // The stat cells.
            string[] labels = { Loc.T("profile.stat_levels"), Loc.T("profile.stat_pictures"), Loc.T("profile.stat_daily") };
            for (int i = 0; i < 3; i++)
            {
                // A tile in a thin wooden rim (spec 005 FR-047).
                screen._stats[i] = UiKit.Tile("Stat" + i, root, 0.22f);
                screen._statValues[i] = UiKit.Label("Value", screen._stats[i].transform, "0", T.LevelPill, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
                screen._statLabels[i] = UiKit.Label("Label", screen._stats[i].transform, labels[i], T.Caption, UiTheme.Of(C.InkBrownSoft));
            }

            // Three illustrated achievement tiles at a time; the arrows page through the full catalog.
            screen._title = UiKit.WoodSign("Achievements", root, Loc.T("profile.achievements"), T.Title);
            RectTransform PageButton(string name, string glyph, Action tap)
            {
                Image hit = UiFactory.CreateImage(name, root, null, Color.clear, raycast: true);
                UiKit.TapTarget(hit, tap, press: true);
                TextMeshProUGUI arrow = UiKit.Label("Arrow", hit.transform, glyph, T.Title, UiTheme.Of(C.InkBrown));
                UiFactory.Stretch(arrow.rectTransform);
                return hit.rectTransform;
            }
            screen._previous = PageButton("PreviousAchievements", "‹", () => screen.ChangePage(-1));
            screen._next = PageButton("NextAchievements", "›", () => screen.ChangePage(1));
            Box Badge(Box b) => Box.FromCenter(b.Right - (b.Width * 0.14f), b.Bottom - (b.Height * 0.14f), b.Width * 0.3f, b.Width * 0.3f);
            for (int i = 0; i < screen._tiles.Length; i++)
            {
                Image hit = UiFactory.CreateImage("Achievement" + i, root, null, Color.clear, raycast: true);
                int index = i;
                UiKit.TapTarget(hit, () => screen.SelectAchievement(index), press: true);
                RectTransform tile = hit.rectTransform;
                Image well = UiKit.Tile("Well", tile, 0.24f);
                Image trophy = UiFactory.CreateImage("Badge", well.transform, null, Color.white);
                trophy.preserveAspect = true;
                trophy.raycastTarget = false;
                TextMeshProUGUI count = UiKit.Label("Count", well.transform, string.Empty, T.Caption, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
                RectTransform badge = UiKit.LockBadge("Lock", well.transform);
                RectTransform check = UiFactory.Stretch(UiFactory.CreateRect("Check", well.transform));
                UiKit.CheckBadge(BoxLayout.On(check), Badge);
                TextMeshProUGUI label = UiKit.Label("Label", tile, string.Empty, T.Caption, UiTheme.Of(C.InkBrownSoft));
                BoxLayout.On(tile)
                    .Add(well.rectTransform, ReferenceProfileRegions.AchievementWell)
                    .Add(label.rectTransform, ReferenceProfileRegions.AchievementLabel);
                BoxLayout.On(well.rectTransform)
                    .Add(trophy.rectTransform, ReferenceProfileRegions.AchievementTrophy)
                    .Add(count.rectTransform, ReferenceProfileRegions.AchievementProgress)
                    .Add(badge, Badge);
                screen._tiles[i] = tile;
                screen._tileLabels[i] = label;
                screen._tileParts[i] = (trophy, count, badge.gameObject, check.gameObject);
            }

            screen._note = UiKit.Label("Note", root, Loc.F("profile.achievements_page", 1,
                (Achievements.All.Count + ReferenceProfileRegions.AchievementCount - 1) / ReferenceProfileRegions.AchievementCount), T.Caption, UiTheme.Of(C.InkBrownSoft));

            // The header last, as on the other pages; the edit card over everything.
            // The flowers over the frame's corners, over the page's content (spec 005 FR-047).
            screen._flowers = UiKit.PageFlowers(root);

            screen._header = UiKit.PageHeader(root, Loc.T("profile.title"), screen.Hide, petals != null, onStore);
            screen._editCard = ProfileEditCard.Create(root, profile, wardrobe, wardrobeLevel, () =>
            {
                screen.Refresh();
                onChanged?.Invoke();
            });
            shade.gameObject.SetActive(false);
            return screen;
        }

        public void Show()
        {
            _root.SetActive(true);
            _page = 0;
            _message = null;
            _plus = _store && (_navLook == null || _navLook().Store);
            _petalsShown = -1;
            Layout();
            Refresh();
        }

        public void Hide()
        {
            _editCard.Hide();
            _root.SetActive(false);
        }

        /// <summary>The name the page shows: the chosen one, else "Gardener 4821".</summary>
        public static string NameOf(ProfileService profile) => profile.Name ?? Loc.F("profile.default_name", profile.DefaultNumber);

        private void Refresh()
        {
            AvatarItem avatar = _profile.Avatar;
            _avatar.Show(_wardrobe.Profile with { Marker = null }, _wardrobe.IsAvailable ? _outfitOf?.Invoke(avatar.Family) : null, avatar);
            _name.text = NameOf(_profile);
            _id.text = Loc.F("profile.id", _profile.ShortId);
            _joined.text = Loc.F("profile.joined", _profile.JoinedMonth);
            _plaque.Text = Loc.F("common.level", NumberText.Group(_level()));
            ProfileStats stats = _statsSource();
            _statValues[0].text = NumberText.Group(stats.LevelsWon);
            _statValues[1].text = NumberText.Group(stats.Pictures);
            _statValues[2].text = NumberText.Group(stats.DailyWon);
            IReadOnlyList<AchievementState> achievements = Achievements.Of(_save);
            for (int i = 0; i < _tileParts.Length; i++)
            {
                int index = _page * _tileParts.Length + i;
                _tiles[i].gameObject.SetActive(index < achievements.Count);
                if (index >= achievements.Count) continue;
                AchievementState state = achievements[index];
                (Image trophy, TextMeshProUGUI count, GameObject padlock, GameObject check) = _tileParts[i];
                trophy.sprite = OwnerArt.Badge(state.Def.Id) ?? ProceduralSprites.Shape("ui.trophy");
                trophy.color = state.Tier == 0 ? new Color(0.62f, 0.62f, 0.62f, 0.5f) : Color.white;
                count.text = state.Complete ? NumberText.Group(state.Value) : Loc.F("profile.achievement_progress", NumberText.Group(state.Shown), NumberText.Group(state.Goal));
                padlock.SetActive(state.Tier == 0);
                check.SetActive(state.Complete);
                _tileLabels[i].text = Loc.T(state.Def.NameKey);
                _tileLabels[i].color = UiTheme.Of(state.Tier == 0 ? C.InkBrownSoft : C.InkBrown);
            }
            _note.text = _message ?? Loc.F("profile.achievements_page", _page + 1,
                (achievements.Count + _tileParts.Length - 1) / _tileParts.Length);
        }

        private void ChangePage(int direction)
        {
            int pages = (Achievements.All.Count + _tileParts.Length - 1) / _tileParts.Length;
            _page = (_page + direction + pages) % pages;
            _message = null;
            Refresh();
        }

        private void SelectAchievement(int tile)
        {
            IReadOnlyList<AchievementState> states = Achievements.Of(_save);
            int index = _page * _tileParts.Length + tile;
            if (index >= states.Count) return;
            AchievementState state = states[index];
            if (state.Tier > 0 && _wardrobe.Show(state.Def.BadgeId))
            {
                _message = Loc.T("profile.badge_equipped");
                _onChanged?.Invoke();
            }
            else _message = Loc.T("profile.achievement_help." + state.Def.Id);
            Refresh();
        }

        private void Update()
        {
            // The balance follows the economy (an avatar bought in the edit card, a purchase in the Store).
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

        /// <summary>Places the page on the kit's regions for the screen's shape.</summary>
        private void Layout()
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            ReferenceProfileRegions r = ScreenLayout.ReferenceProfile(w, h, insets);
            _header.Place(r.Header);
            float radius = r.PanelRadius(DesignTokens.ScaleFor(w, h));
            UiKit.PlaceScreen(_panel.rectTransform, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            UiKit.PlacePageFlowers(_flowers, r.Panel, r.Panel.Bottom);
            UiKit.PlaceScreen(_card.rectTransform, r.Card);
            UiKit.PlaceScreen(_avatar.Rect, r.Avatar);
            UiKit.PlaceScreen(_avatarButton, r.Avatar);
            UiKit.PlaceScreen(_name.rectTransform, r.Name);
            UiKit.PlaceScreen(_edit, r.Edit);
            UiKit.PlaceScreen(_id.rectTransform, r.Id);
            UiKit.PlaceScreen(_joined.rectTransform, r.Joined);
            UiKit.PlaceScreen((RectTransform)_plaque.transform, r.Plaque);
            for (int i = 0; i < _stats.Length; i++)
            {
                Box cell = r.Stats[i];
                UiKit.PlaceScreen(_stats[i].rectTransform, cell);
                UiKit.PlaceBox(_statValues[i].rectTransform, ReferenceProfileRegions.StatValue(cell), cell);
                UiKit.PlaceBox(_statLabels[i].rectTransform, ReferenceProfileRegions.StatLabel(cell), cell);
            }

            // The sign as wide as its title (the playtest's CardLook.TitleSign).
            float titleWidth = KitText.Measure(_title.Label, UiKit.Units(T.Title.Size)) * UiKit.PixelsPerUnit;
            UiKit.PlaceScreen((RectTransform)_title.transform, CardLook.TitleSign(r.AchievementsTitle, titleWidth > 0f ? titleWidth : r.AchievementsTitle.Width * 0.4f));
            UiKit.PlaceScreen(_previous, Box.FromCenter(r.AchievementsTitle.Left + r.AchievementsTitle.Height * 0.5f, r.AchievementsTitle.CenterY, r.AchievementsTitle.Height, r.AchievementsTitle.Height));
            UiKit.PlaceScreen(_next, Box.FromCenter(r.AchievementsTitle.Right - r.AchievementsTitle.Height * 0.5f, r.AchievementsTitle.CenterY, r.AchievementsTitle.Height, r.AchievementsTitle.Height));
            for (int i = 0; i < _tiles.Length; i++)
            {
                UiKit.PlaceScreen(_tiles[i], r.Achievements[i]);
            }

            UiKit.PlaceScreen(_note.rectTransform, r.AchievementsNote);
        }
    }
}
