using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
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
    /// The Wardrobe (FR-063, T143), open from L40, in the reference Wardrobe's look (spec 005 contracts/look.md §4.6), laid
    /// out by <see cref="WardrobeLayout"/>:
    /// <list type="bullet">
    /// <item><description>a cream round back button, the wooden "Wardrobe" banner with ivy and the Petals pill (its "+"
    /// opens the Store);</description></item>
    /// <item><description>the chosen family's 3D hero in its outfit on a stone pedestal, with cream ‹ › arrows to the
    /// other families;</description></item>
    /// <item><description>a parchment name card: the name and the family's role on a cream tab, and a line about
    /// it;</description></item>
    /// <item><description>tabs with each family's hero and name, and a last one for the profile (the Home avatar), joined
    /// to the lighter panel below;</description></item>
    /// <item><description>on the panel, the kinds as chips (skin, hat, trail, face; or frame, badge, marker), then the
    /// owned items of the kind as outfit cards, the worn one green with a check, the "Default" look (none of the kind)
    /// first; a tap wears an item, and each family wears one of each kind;</description></item>
    /// <item><description>the footer "Earn special outfits as you play!" between the page arrows.</description></item>
    /// </list>
    /// Cosmetics only change how Bloomlings look; the variant colors and icons stay as they are.
    /// </summary>
    public sealed class WardrobeScreen : MonoBehaviour
    {
        private readonly List<FamilyTabView> _tabs = new List<FamilyTabView>();
        private readonly List<BloomlingFigure> _tabHeroes = new List<BloomlingFigure>();
        private readonly List<GameObject> _cards = new List<GameObject>();
        private GameObject _root = null!;
        private WardrobeService _wardrobe = null!;
        private Func<long>? _petalsSource;
        private bool _store;
        private PetalsPill? _petals;
        private long _petalsShown = -1;
        private RectTransform _back = null!;
        private RectTransform _banner = null!;
        private RectTransform _pedestal = null!;
        private BloomlingFigure _hero = null!;
        private ProfileAvatar _avatar = null!;
        private ProfileAvatar _tabAvatar = null!;
        private Button _previous = null!;
        private Button _next = null!;
        private Image _nameCard = null!;
        private RectTransform _nameTab = null!;
        private TextMeshProUGUI _name = null!;
        private TextMeshProUGUI _role = null!;
        private TextMeshProUGUI _about = null!;
        private Image _panel = null!;
        private Image _panelLine = null!;
        private RectTransform _chips = null!;
        private TabsView _wornKinds = null!;
        private TabsView _profileKinds = null!;
        private RectTransform _items = null!;
        private TextMeshProUGUI _empty = null!;
        private TextMeshProUGUI _footer = null!;
        private Button _pagePrevious = null!;
        private Button _pageNext = null!;
        private WardrobeRegions _regions = null!;
        private Family _selected = Family.Sprig;
        private bool _profileMode;
        private CosmeticKind _kind = CosmeticKind.Skin;
        private int _pageIndex;

        public bool IsOpen => _root.activeSelf;

        /// <param name="petals">The Petals balance for the pill; null hides the pill.</param>
        /// <param name="onStore">Opens the Store from the pill's "+"; null shows the pill without it.</param>
        public static WardrobeScreen Create(Transform parent, WardrobeService wardrobe, Func<long>? petals = null, Action? onStore = null)
        {
            // A full screen over Home that takes every tap, on the owner's Wardrobe garden (pictures.md B7) or the drawn one.
            Image shade = UiFactory.CreateImage("Wardrobe", parent, null, Color.clear, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var screen = shade.gameObject.AddComponent<WardrobeScreen>();
            screen._root = shade.gameObject;
            screen._wardrobe = wardrobe;
            screen._petalsSource = petals;
            screen._store = onStore != null;
            Transform root = shade.transform;
            BackdropView.Create(shade.rectTransform, OwnerPictures.Wardrobe, BackdropScene.Home);

            // The hero on its stone pedestal, and the profile avatar in its place on the Profile tab.
            screen._pedestal = UiKit.StonePedestal("Pedestal", root);
            screen._hero = BloomlingFigure.Create("Hero", root);
            screen._hero.Body.raycastTarget = false;
            screen._avatar = ProfileAvatar.Create("Avatar", root);
            screen._previous = UiKit.ArrowButton("Previous", root, next: false, () => screen.Turn(-1));
            screen._next = UiKit.ArrowButton("Next", root, next: true, () => screen.Turn(1));

            // The name card: parchment, with the name and the role on a cream tab and a line about the family below.
            screen._nameCard = UiKit.Paper("NameCard", root, b => b.Height * 0.16f, DesignTokens.Garden.FrameWidth, 8f, raycast: false);
            (screen._nameTab, screen._name, screen._role) = NameTab(root);
            screen._about = UiKit.Label("About", root, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft), look: TextLook.Plain(C.InkBrownSoft));
            screen._about.textWrappingMode = TextWrappingModes.Normal;

            // The lighter panel, then the tabs over its top edge (the selected one flows into it).
            screen._panel = UiKit.RoundGradient("Panel", root, C.ParchmentTop, C.CreamTop, _ => UiKit.Units(26f));
            screen._panelLine = UiKit.RoundRing("PanelLine", root, UiTheme.Of(C.CreamLine), _ => UiKit.Units(26f), _ => Mathf.Max(UiKit.Units(2f), UiKit.Units(DesignTokens.Garden.OutlineWidth) * 0.8f));
            IReadOnlyList<Family> families = WardrobeService.Families;
            for (int i = 0; i < families.Count; i++)
            {
                Family family = families[i];
                FamilyTabView tab = UiKit.FamilyTab(family.ToString(), root, Loc.T("family." + WardrobeService.FamilyKey(family)), () => screen.Select(family));
                BloomlingFigure figure = BloomlingFigure.Create("Hero", tab.Picture);
                UiFactory.Stretch(figure.Rect);
                figure.Body.raycastTarget = false;
                screen._tabs.Add(tab);
                screen._tabHeroes.Add(figure);
            }

            FamilyTabView profile = UiKit.FamilyTab("Profile", root, Loc.T("wardrobe.tab_profile"), () => screen.SetMode(true));
            screen._tabAvatar = ProfileAvatar.Create("Avatar", profile.Picture);
            UiFactory.Stretch(screen._tabAvatar.Rect);
            screen._tabs.Add(profile);

            // The kinds as chips, the outfit cards, the empty line and the footer between the page arrows.
            screen._chips = UiFactory.CreateRect("Kinds", root);
            screen._wornKinds = UiKit.Tabs("Worn", screen._chips, Labels(CosmeticCatalog.WornKinds), screen.SetKind);
            screen._profileKinds = UiKit.Tabs("Profile", screen._chips, Labels(CosmeticCatalog.ProfileKinds), screen.SetKind);
            screen._items = UiFactory.CreateRect("Items", root);
            screen._empty = UiKit.Label("Empty", root, Loc.T("wardrobe.more"), T.Body, UiTheme.Of(C.InkBrownSoft), look: TextLook.Plain(C.InkBrownSoft));
            screen._footer = UiKit.Label("Footer", root, Loc.T("wardrobe.footer"), T.Body, UiTheme.Of(C.InkBrownSoft), look: TextLook.Plain(C.InkBrownSoft));
            screen._pagePrevious = UiKit.ArrowButton("PagePrevious", root, next: false, () => screen.TurnPage(-1));
            screen._pageNext = UiKit.ArrowButton("PageNext", root, next: true, () => screen.TurnPage(1));

            // The top bar last: the back button, the banner with ivy, the Petals pill.
            screen._back = (RectTransform)UiKit.RoundIconButton("Back", root, "ui.back", screen.Hide).transform;
            screen._banner = (RectTransform)UiKit.WoodSign("Banner", root, Loc.T("wardrobe.title"), T.Title, SignDecor.Ivy).transform;
            if (petals != null)
            {
                screen._petals = UiKit.PetalsPill("Petals", root, onStore);
            }

            shade.gameObject.SetActive(false);
            return screen;
        }

        public void Show()
        {
            // Shown first, so the new cards' text engine is awake when their layouts measure them.
            _root.SetActive(true);
            Refresh();
        }

        /// <summary>Opens on the Profile tab (the Home avatar).</summary>
        public void ShowProfile()
        {
            _root.SetActive(true);
            SetMode(true);
        }

        public void Hide() => _root.SetActive(false);

        /// <summary>A cosmetic's display name: its localized entry (a level badge or marker names its level), else the catalog name.</summary>
        public static string Name(CosmeticItem item)
        {
            if (item.MilestoneLevel.HasValue)
            {
                return Loc.F(item.Kind == CosmeticKind.Marker ? "cosmetic.level_marker" : "cosmetic.level_badge", item.MilestoneLevel.Value);
            }

            return Loc.T("cosmetic." + item.Id, item.Name);
        }

        /// <summary>The icon of an item in grids and the Store: its accessory, or its pattern on a round swatch for a skin.</summary>
        public static Sprite Icon(CosmeticItem item) => ProceduralSprites.Accessory(item.Shape);

        public static string KindLabel(CosmeticKind kind) => kind switch
        {
            CosmeticKind.Skin => Loc.T("wardrobe.kind_skin"),
            CosmeticKind.Hat => Loc.T("wardrobe.kind_hat"),
            CosmeticKind.Trail => Loc.T("wardrobe.kind_trail"),
            CosmeticKind.Expression => Loc.T("wardrobe.kind_expression"),
            CosmeticKind.Frame => Loc.T("wardrobe.kind_frame"),
            CosmeticKind.Badge => Loc.T("wardrobe.kind_badge"),
            _ => Loc.T("wardrobe.kind_marker"),
        };

        private IReadOnlyList<CosmeticKind> Kinds => _profileMode ? CosmeticCatalog.ProfileKinds : CosmeticCatalog.WornKinds;

        private static string[] Labels(IReadOnlyList<CosmeticKind> kinds)
        {
            var labels = new string[kinds.Count];
            for (int i = 0; i < kinds.Count; i++)
            {
                labels[i] = KindLabel(kinds[i]);
            }

            return labels;
        }

        /// <summary>
        /// The name tab (§4.6): a raised cream plate (a soft shadow, the <c>cream.lip</c>, the <c>cream.top</c> to
        /// <c>parchment.bottom</c> face, a <c>cream.line</c> outline) with the name in <c>type.title</c> and the role below.
        /// </summary>
        private static (RectTransform Tab, TextMeshProUGUI Name, TextMeshProUGUI Role) NameTab(Transform parent)
        {
            (RectTransform tab, BoxLayout layout) = UiKit.Element("NameTab", parent);
            float R(Box b) => b.Height * 0.3f;
            UiKit.SoftShadow(layout, b => b, R, 0.2f, 0.05f);
            Image lip = UiKit.RoundRect("Lip", tab, UiTheme.Of(C.CreamLip), R);
            Image face = UiKit.RoundGradient("Face", tab, C.CreamTop, C.ParchmentBottom, R);
            Image line = UiKit.RoundRing("Line", tab, UiTheme.Of(C.CreamLine), R, b => Mathf.Max(UiKit.Units(2f), b.Height * 0.025f));
            layout.Add(lip.rectTransform, b => b.Offset(0f, b.Height * 0.06f));
            layout.Add(face.rectTransform, b => b);
            layout.Add(line.rectTransform, b => b);
            TextMeshProUGUI name = UiKit.KitLabel("Name", tab, string.Empty, T.Title, TextLook.Plain(C.InkBrown));
            TextMeshProUGUI role = UiKit.KitLabel("Role", tab, string.Empty, T.Body, TextLook.Plain(C.InkBrownSoft));
            layout.Watch(name).Watch(role).Then(b =>
            {
                KitText.Place(name, T.Title, b.CenterX, b.Top + (b.Height * 0.36f), Mathf.Min(UiKit.Units(T.Title.Size), b.Height * 0.46f), b.Width * 0.86f);
                KitText.Place(role, T.Body, b.CenterX, b.Top + (b.Height * 0.76f), Mathf.Min(UiKit.Units(T.Body.Size), b.Height * 0.3f), b.Width * 0.86f);
            });
            return (tab, name, role);
        }

        private void SetMode(bool profile)
        {
            _profileMode = profile;
            _kind = Kinds[0];
            _pageIndex = 0;
            Refresh();
        }

        private void SetKind(int index)
        {
            if (index < Kinds.Count)
            {
                _kind = Kinds[index];
                _pageIndex = 0;
                Refresh();
            }
        }

        private void Select(Family family)
        {
            bool wasProfile = _profileMode;
            _selected = family;
            _profileMode = false;
            if (wasProfile)
            {
                _kind = Kinds[0];
            }

            _pageIndex = 0;
            Refresh();
        }

        /// <summary>The ‹ › arrows: the previous or next family, around the four.</summary>
        private void Turn(int by)
        {
            IReadOnlyList<Family> families = WardrobeService.Families;
            int index = 0;
            for (int i = 0; i < families.Count; i++)
            {
                if (families[i] == _selected)
                {
                    index = i;
                }
            }

            Select(families[(index + by + families.Count) % families.Count]);
        }

        private void TurnPage(int by)
        {
            _pageIndex += by;
            RefreshItems();
        }

        private void Update()
        {
            // The balance follows the economy (a purchase in the Store opened from the "+").
            if (_petals != null && _petalsSource != null)
            {
                long now = _petalsSource();
                if (now != _petalsShown)
                {
                    _petals.Show(now, _store);
                    _petalsShown = now;
                }
            }
        }

        /// <summary>Places every region for the screen's shape (<see cref="WardrobeLayout"/>).</summary>
        private void Layout()
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            WardrobeRegions r = WardrobeLayout.Wardrobe(w, h, insets);
            _regions = r;
            float u = DesignTokens.ScaleFor(w, h);
            UiKit.PlaceScreen(_back, r.Back);
            UiKit.PlaceScreen(_banner, r.Banner);
            if (_petals != null)
            {
                UiKit.PlaceScreen((RectTransform)_petals.transform, r.Petals);
            }

            UiKit.PlaceScreen(_pedestal, r.Pedestal);
            UiKit.PlaceScreen(_hero.Rect, r.Hero);
            Box top = UiKit.PedestalTop(r.Pedestal);
            float avatar = Mathf.Min(r.Hero.Width, r.Hero.Height) * 0.9f;
            UiKit.PlaceScreen(_avatar.Rect, Box.FromCenter(r.Hero.CenterX, top.CenterY - (avatar * 0.5f), avatar, avatar));
            UiKit.PlaceScreen((RectTransform)_previous.transform, r.Previous);
            UiKit.PlaceScreen((RectTransform)_next.transform, r.Next);
            UiKit.PlaceScreen(_nameCard.rectTransform, r.NameCard);
            UiKit.PlaceScreen(_nameTab, r.NameTab);
            UiKit.PlaceScreen(_about.rectTransform, r.About);
            UiKit.PlaceScreen(_panel.rectTransform, r.Panel);
            UiKit.PlaceScreen(_panelLine.rectTransform, r.Panel);

            // The tabs: the others a little lower, the selected one flowing into the panel.
            int selected = _profileMode ? _tabs.Count - 1 : IndexOf(_selected);
            Box[] cells = ScreenLayout.Row(r.Tabs, _tabs.Count, 10f * u, float.MaxValue, square: false);
            float sunk = r.Tabs.Height * 0.07f;
            for (int i = 0; i < _tabs.Count; i++)
            {
                Box cell = cells[i];
                UiKit.PlaceScreen((RectTransform)_tabs[i].transform, i == selected ? cell : new Box(cell.Left, cell.Top + sunk, cell.Right, cell.Bottom));
                _tabs[i].Select(i == selected);
            }

            UiKit.PlaceScreen(_chips, r.Chips);
            UiKit.PlaceScreen(_items, r.Grid);
            UiKit.PlaceScreen(_empty.rectTransform, r.Grid);
            float room = r.Footer.Height + (12f * u);
            UiKit.PlaceScreen(_footer.rectTransform, new Box(r.Footer.Left + room, r.Footer.Top, r.Footer.Right - room, r.Footer.Bottom));
            UiKit.PlaceScreen((RectTransform)_pagePrevious.transform, r.PagePrevious);
            UiKit.PlaceScreen((RectTransform)_pageNext.transform, r.PageNext);
        }

        private static int IndexOf(Family family)
        {
            IReadOnlyList<Family> families = WardrobeService.Families;
            for (int i = 0; i < families.Count; i++)
            {
                if (families[i] == family)
                {
                    return i;
                }
            }

            return 0;
        }

        private void Refresh()
        {
            Layout();
            string key = WardrobeService.FamilyKey(_selected);
            _hero.Rect.gameObject.SetActive(!_profileMode);
            _avatar.Rect.gameObject.SetActive(_profileMode);
            _previous.gameObject.SetActive(!_profileMode);
            _next.gameObject.SetActive(!_profileMode);
            _hero.ShowHero(_selected, _wardrobe.OutfitOf(_selected));
            _avatar.Show(_wardrobe.Profile, _wardrobe.OutfitOf(Family.Bloom));
            _tabAvatar.Show(_wardrobe.Profile, _wardrobe.OutfitOf(Family.Bloom));
            _name.text = _profileMode ? Loc.T("wardrobe.tab_profile") : Loc.T("family." + key);
            _role.text = _profileMode ? Loc.T("wardrobe.role.profile") : Loc.T("wardrobe.role." + key);
            _about.text = _profileMode ? Loc.T("wardrobe.about.profile") : Loc.T("wardrobe.about." + key);

            IReadOnlyList<Family> families = WardrobeService.Families;
            for (int i = 0; i < families.Count && i < _tabHeroes.Count; i++)
            {
                _tabHeroes[i].ShowHero(families[i], _wardrobe.OutfitOf(families[i]));
            }

            _wornKinds.gameObject.SetActive(!_profileMode);
            _profileKinds.gameObject.SetActive(_profileMode);
            IReadOnlyList<CosmeticKind> kinds = Kinds;
            int kind = 0;
            for (int i = 0; i < kinds.Count; i++)
            {
                if (kinds[i] == _kind)
                {
                    kind = i;
                }
            }

            (_profileMode ? _profileKinds : _wornKinds).Select(kind);
            RefreshItems();
        }

        private void RefreshItems()
        {
            foreach (GameObject card in _cards)
            {
                Destroy(card);
            }

            _cards.Clear();

            // Worn kinds start with the "Default" look (none of the kind); a profile kind always shows one of its items.
            var options = new List<CosmeticItem?>();
            if (!_profileMode)
            {
                options.Add(null);
            }

            foreach (CosmeticItem item in _wardrobe.OwnedOf(_kind))
            {
                options.Add(item);
            }

            int perPage = _regions.PerPage;
            int pages = Mathf.Max(1, (options.Count + perPage - 1) / perPage);
            _pageIndex = Mathf.Clamp(_pageIndex, 0, pages - 1);
            _pagePrevious.gameObject.SetActive(pages > 1);
            _pageNext.gameObject.SetActive(pages > 1);
            _pagePrevious.interactable = _pageIndex > 0;
            _pageNext.interactable = _pageIndex < pages - 1;
            _empty.gameObject.SetActive(options.Count == 0);

            // The worn item (or the shown profile item) is the green card with the check.
            CosmeticItem? current = _profileMode ? _wardrobe.Shown(_kind) : _wardrobe.EquippedFor(_selected, _kind);
            Outfit outfit = _wardrobe.OutfitOf(_selected);
            for (int slot = 0; slot < perPage; slot++)
            {
                int index = (_pageIndex * perPage) + slot;
                if (index >= options.Count)
                {
                    break;
                }

                CosmeticItem? item = options[index];
                OutfitCardView card = UiKit.OutfitCard(item?.Id ?? "Default", _items, item == null ? Loc.T("wardrobe.default") : Name(item), () => Choose(item));
                UiKit.PlaceBox((RectTransform)card.transform, _regions.Card(slot), _regions.Grid);
                Preview(card.Picture, item, outfit);
                card.Show(item?.Id == current?.Id);
                _cards.Add(card.gameObject);
            }
        }

        /// <summary>
        /// An outfit card's picture: the chosen family's hero in its outfit with this item in its kind's place (or none of
        /// the kind for the Default card); a frame around a small portrait of the hero; a badge or a marker as its shape.
        /// </summary>
        private void Preview(RectTransform well, CosmeticItem? item, Outfit outfit)
        {
            if (item == null || item.IsWorn)
            {
                BloomlingFigure figure = BloomlingFigure.Create("Hero", well);
                UiFactory.Stretch(figure.Rect);
                figure.Body.raycastTarget = false;
                figure.ShowHero(_selected, With(outfit, item?.Kind ?? _kind, item));
                return;
            }

            if (item.Kind == CosmeticKind.Frame)
            {
                BloomlingFigure portrait = BloomlingFigure.Create("Hero", well);
                UiFactory.Place(portrait.Rect, 0.16f, 0.16f, 0.84f, 0.84f);
                portrait.Body.raycastTarget = false;
                portrait.ShowHero(Family.Bloom, null);
            }

            Image mark = UiFactory.CreateImage("Item", well, Icon(item), BloomlingFigure.Tint(item));
            mark.preserveAspect = true;
            mark.raycastTarget = false;
            float inset = item.Kind == CosmeticKind.Frame ? 0f : 0.08f;
            UiFactory.Place(mark.rectTransform, inset, inset, 1f - inset, 1f - inset);
        }

        /// <summary>An outfit with <paramref name="item"/> (or nothing) in <paramref name="kind"/>'s place.</summary>
        private static Outfit With(Outfit outfit, CosmeticKind kind, CosmeticItem? item) => kind switch
        {
            CosmeticKind.Skin => outfit with { Skin = item },
            CosmeticKind.Hat => outfit with { Hat = item },
            CosmeticKind.Trail => outfit with { Trail = item },
            CosmeticKind.Expression => outfit with { Expression = item },
            _ => outfit,
        };

        private void Choose(CosmeticItem? item)
        {
            if (_profileMode)
            {
                if (item != null)
                {
                    _wardrobe.Show(item.Id);
                }
            }
            else if (item == null)
            {
                _wardrobe.Unequip(_selected, _kind);
            }
            else
            {
                _wardrobe.Equip(_selected, item.Id);
            }

            Refresh();
        }
    }
}
