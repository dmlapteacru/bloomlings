using System;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
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
    /// <summary>What the profile page's stat cells count: levels won, pictures collected, milestones reached.</summary>
    public readonly struct ProfileStats
    {
        public ProfileStats(long levelsWon, int pictures, int milestones)
        {
            LevelsWon = levelsWon;
            Pictures = pictures;
            Milestones = milestones;
        }

        public long LevelsWon { get; }

        public int Pictures { get; }

        public int Milestones { get; }
    }

    /// <summary>
    /// The profile page (spec 005 FR-037; the playtest's <c>ProfileScreen</c>), every element placed from
    /// <see cref="ScreenLayout.ReferenceProfile"/>: over the Wardrobe's garden, the page header (back, the wooden "Profile"
    /// banner, the Petals pill whose "+" opens the Store once it is open) and the parchment panel; on it the player's card
    /// (the round avatar in its frame and badge, a tap opening the edit card on Avatar; the name with the pencil, opening it
    /// on Name; the short ID; "Playing since 10/2026"; the wooden "Level N" plaque), three stat cells (levels won,
    /// pictures, milestones) and the Achievements: locked placeholder tiles, "Coming soon", until the owner names them. It
    /// lies over Home (Home's avatar opens it, <c>HomeFeatureActions.OnProfile</c>); its back hides it. It has no bottom
    /// menu: it is not a menu place.
    /// </summary>
    public sealed class ProfileScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private PageHeaderView _header = null!;
        private Image _panel = null!;
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
        private TextMeshProUGUI _title = null!;
        private readonly RectTransform[] _tiles = new RectTransform[ReferenceProfileRegions.AchievementCount];
        private readonly TextMeshProUGUI[] _tileLabels = new TextMeshProUGUI[ReferenceProfileRegions.AchievementCount];
        private TextMeshProUGUI _note = null!;
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
            screen._level = level;
            screen._statsSource = stats;
            screen._outfitOf = outfitOf;
            screen._petalsSource = petals;
            screen._store = onStore != null;
            screen._navLook = navLook;
            Transform root = shade.transform;
            BackdropView.Create(shade.rectTransform, OwnerPictures.Wardrobe, BackdropScene.Home);
            screen._panel = UiKit.Paper("Panel", root, b => Mathf.Max(UiKit.Units(DesignTokens.Radius.CardMin), b.Width * DesignTokens.Radius.Card), DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard, raycast: false);

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
            screen._plaque = UiKit.WoodSign("Level", root, Loc.F("common.level", 1), T.LevelPill);

            // The stat cells.
            string[] labels = { Loc.T("profile.stat_levels"), Loc.T("profile.stat_pictures"), Loc.T("profile.stat_milestones") };
            for (int i = 0; i < 3; i++)
            {
                screen._stats[i] = UiKit.Well("Stat" + i, root);
                screen._statValues[i] = UiKit.Label("Value", screen._stats[i].transform, "0", T.LevelPill, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
                screen._statLabels[i] = UiKit.Label("Label", screen._stats[i].transform, labels[i], T.Caption, UiTheme.Of(C.InkBrownSoft));
            }

            // The Achievements: placeholders until the owner names them.
            screen._title = UiKit.Label("Achievements", root, Loc.T("profile.achievements"), T.Title, UiTheme.Of(C.InkTitle), look: TextLook.Plain(C.InkTitle));
            for (int i = 0; i < screen._tiles.Length; i++)
            {
                RectTransform tile = UiFactory.CreateRect("Achievement" + i, root);
                Image well = UiKit.Well("Well", tile);
                Image trophy = UiKit.ShapeImage("Trophy", well.transform, "ui.trophy", C.InkBrownSoft.WithAlpha(0.35f));
                RectTransform badge = UiKit.LockBadge("Lock", well.transform);
                TextMeshProUGUI label = UiKit.Label("Label", tile, Loc.T("profile.achievement_soon"), T.Caption, UiTheme.Of(C.InkBrownSoft));
                BoxLayout.On(tile)
                    .Add(well.rectTransform, ReferenceProfileRegions.AchievementWell)
                    .Add(label.rectTransform, ReferenceProfileRegions.AchievementLabel);
                BoxLayout.On(well.rectTransform)
                    .Add(trophy.rectTransform, b => b.Inset(b.Width * 0.24f))
                    .Add(badge, b => Box.FromCenter(b.Right - (b.Width * 0.14f), b.Bottom - (b.Height * 0.14f), b.Width * 0.3f, b.Width * 0.3f));
                screen._tiles[i] = tile;
                screen._tileLabels[i] = label;
            }

            screen._note = UiKit.Label("Note", root, Loc.T("profile.achievements_note"), T.Caption, UiTheme.Of(C.InkBrownSoft));

            // The header last, as on the other pages; the edit card over everything.
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
            _statValues[2].text = NumberText.Group(stats.Milestones);
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

            UiKit.PlaceScreen(_title.rectTransform, r.AchievementsTitle);
            for (int i = 0; i < _tiles.Length; i++)
            {
                UiKit.PlaceScreen(_tiles[i], r.Achievements[i]);
            }

            UiKit.PlaceScreen(_note.rectTransform, r.AchievementsNote);
        }
    }
}
