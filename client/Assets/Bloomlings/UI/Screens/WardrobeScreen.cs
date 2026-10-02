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
    /// The Wardrobe (FR-063, T143), open from L40, in the reference Wardrobe's look and layout (spec 005 FR-025,
    /// contracts/look.md §4.6 and §6.5), every element placed from <see cref="ScreenLayout.ReferenceWardrobe"/>:
    /// <list type="bullet">
    /// <item><description>a cream round back button at the top left, the wooden "Wardrobe" banner with ivy and the Petals
    /// pill at the top right (its "+" opens the Store);</description></item>
    /// <item><description>the chosen family's 3D hero in its outfit standing on a stone pedestal, with cream ‹ › arrows
    /// to the other families at the screen's sides;</description></item>
    /// <item><description>a parchment name card: the name on a raised cream tab, the family's role under it and two lines
    /// about it;</description></item>
    /// <item><description>tabs with each family's hero and name, and a last one for the profile (the Home avatar), joined
    /// to the lighter panel below;</description></item>
    /// <item><description>on the panel to the bottom of the screen, the kinds as chips (skin, hat, trail, face; or frame,
    /// badge, marker), then the owned items of the kind as outfit cards, three to a page, the worn one green with a
    /// check, the "Default" look (none of the kind) first; a tap wears an item, and each family wears one of each
    /// kind;</description></item>
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
        private TextMeshProUGUI _probe = null!;
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
        private ReferenceWardrobeRegions _regions = null!;
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
            // The ‹ › arrows: a 0.09 W cushion in a touch-sized square (size.touch_min).
            screen._previous = UiKit.PageArrow("Previous", root, next: false, () => screen.Turn(-1));
            screen._next = UiKit.PageArrow("Next", root, next: true, () => screen.Turn(1));

            // The name card: parchment under a raised cream tab with the name, the role under the tab and two lines about
            // the family below it.
            screen._nameCard = UiKit.Paper("NameCard", root, b => b.Height * 0.16f, DesignTokens.Garden.FrameWidth, 8f, raycast: false);
            (screen._nameTab, screen._name) = NameTab(root);
            screen._role = UiKit.Label("Role", root, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft), look: TextLook.Plain(C.InkBrownSoft));
            screen._about = UiKit.Label("About", root, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft), look: TextLook.Plain(C.InkBrownSoft));
            screen._about.textWrappingMode = TextWrappingModes.Normal;
            screen._probe = UiKit.Label("Probe", root, string.Empty, T.Body, Color.clear);

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
            screen._pagePrevious = UiKit.PageArrow("PagePrevious", root, next: false, () => screen.TurnPage(-1));
            screen._pageNext = UiKit.PageArrow("PageNext", root, next: true, () => screen.TurnPage(1));

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

        // While open, the screen follows the wardrobe: a choice here, or a cosmetic bought in the Store opened from the
        // Petals pill's "+", shows at once.
        private void OnEnable()
        {
            if (_wardrobe != null)
            {
                _wardrobe.Changed -= OnWardrobeChanged;
                _wardrobe.Changed += OnWardrobeChanged;
            }
        }

        private void OnDisable()
        {
            if (_wardrobe != null)
            {
                _wardrobe.Changed -= OnWardrobeChanged;
            }
        }

        private void OnDestroy() => OnDisable();

        private void OnWardrobeChanged()
        {
            if (_root != null && _root.activeSelf)
            {
                Refresh();
            }
        }

        /// <summary>A cosmetic's display name: its localized entry (a level badge or marker names its level), else the catalog name.</summary>
        public static string Name(CosmeticItem item)
        {
            if (item.MilestoneLevel.HasValue)
            {
                return item.Kind == CosmeticKind.Marker ? Loc.F("cosmetic.level_marker", item.MilestoneLevel.Value) : Loc.F("cosmetic.level_badge", item.MilestoneLevel.Value);
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
        /// The name tab (§4.6, §6.5): a raised cream plate (a soft shadow, the <c>cream.lip</c>, the <c>cream.top</c> to
        /// <c>parchment.bottom</c> face, a <c>cream.line</c> outline) rising over the name card's top edge, with the name in
        /// <c>type.title</c>.
        /// </summary>
        private static (RectTransform Tab, TextMeshProUGUI Name) NameTab(Transform parent)
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
            layout.Watch(name).Then(b =>
            {
                KitText.Place(name, T.Title, b.CenterX, b.CenterY - (b.Height * 0.02f), Mathf.Min(UiKit.Units(T.Title.Size), b.Height * 0.62f), b.Width * 0.86f);
            });
            return (tab, name);
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

        /// <summary>Places every element on the reference regions for the screen's shape (contracts/look.md §6.5).</summary>
        private void Layout()
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            ReferenceWardrobeRegions r = ScreenLayout.ReferenceWardrobe(w, h, insets, hasChips: true);
            _regions = r;
            float u = DesignTokens.ScaleFor(w, h);
            float touch = DesignTokens.Size.TouchMin * u;
            Box Touch(Box b) => Box.FromCenter(b.CenterX, b.CenterY, Mathf.Max(b.Width, touch), Mathf.Max(b.Height, touch));
            UiKit.PlaceScreen(_back, r.Back);
            UiKit.PlaceScreen(_banner, r.Banner);
            if (_petals != null)
            {
                UiKit.PlaceScreen((RectTransform)_petals.transform, r.Petals);
            }

            // The hero stands on the pedestal's top ellipse (its feet at 90% of its picture), its picture from the hero
            // box's top; on the Profile tab the avatar stands there instead.
            UiKit.PlaceScreen(_pedestal, r.Pedestal);
            float feet = UiKit.PedestalTop(r.Pedestal).CenterY;
            Box hero = HomeStage.Figure(r.Hero.CenterX, feet, Mathf.Max(1f, (feet - r.Hero.Top) / HomeStage.FeetShare));
            UiKit.PlaceScreen(_hero.Rect, hero);
            float avatar = Mathf.Min(r.Hero.Width, feet - r.Hero.Top) * 0.84f;
            UiKit.PlaceScreen(_avatar.Rect, Box.FromCenter(r.Hero.CenterX, feet - (avatar * 0.52f), avatar, avatar));

            // The ‹ › arrows: their 0.09 W cushions centered in touch-sized squares.
            UiKit.PlaceScreen((RectTransform)_previous.transform, Touch(r.Previous));
            UiKit.PlaceScreen((RectTransform)_next.transform, Touch(r.Next));

            // The name card's parchment starts a little under its tab's top, so the tab rises over its edge.
            UiKit.PlaceScreen(_nameCard.rectTransform, new Box(r.NameCard.Left, r.NameTab.Top + (r.NameTab.Height * 0.36f), r.NameCard.Right, r.NameCard.Bottom));
            UiKit.PlaceScreen(_nameTab, r.NameTab);
            UiKit.PlaceScreen(_role.rectTransform, r.Role);
            UiKit.PlaceScreen(_about.rectTransform, r.About);

            // The panel runs to the bottom of the screen: its bottom corners go past the edge.
            var panel = new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + (60f * u));
            UiKit.PlaceScreen(_panel.rectTransform, panel);
            UiKit.PlaceScreen(_panelLine.rectTransform, panel);

            // The tabs: the others a little lower, the selected one flowing into the panel.
            int selected = _profileMode ? _tabs.Count - 1 : IndexOf(_selected);
            for (int i = 0; i < _tabs.Count; i++)
            {
                Box cell = r.Tab(i, _tabs.Count);
                float sunk = cell.Height * 0.07f;
                UiKit.PlaceScreen((RectTransform)_tabs[i].transform, i == selected ? cell : new Box(cell.Left, cell.Top + sunk, cell.Right, cell.Bottom));
                _tabs[i].Select(i == selected);
            }

            UiKit.PlaceScreen(_chips, r.Chips);
            UiKit.PlaceScreen(_items, r.Grid);
            UiKit.PlaceScreen(_empty.rectTransform, r.Grid);
            UiKit.PlaceScreen(_footer.rectTransform, r.Footer);
            UiKit.PlaceScreen((RectTransform)_pagePrevious.transform, Touch(r.PagePrevious));
            UiKit.PlaceScreen((RectTransform)_pageNext.transform, Touch(r.PageNext));
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
            // The description in two balanced lines, as the reference's (§6.5).
            string about = _profileMode ? Loc.T("wardrobe.about.profile") : Loc.T("wardrobe.about." + key);
            float aboutWidth = _regions.About.Width * 0.7f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            _about.text = string.Join("\n", UiKit.BalancedLines(_probe, about, UiKit.Units(T.Body.Size), aboutWidth));

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

            int perPage = ReferenceWardrobeRegions.Columns;
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
                // The hero fills the well with its feet near the bottom, as on the Store's outfit cards.
                Outfit worn = With(outfit, item?.Kind ?? _kind, item);
                BloomlingFigure figure = BloomlingFigure.Create("Hero", well);
                figure.Body.raycastTarget = false;
                figure.ShowHero(_selected, worn);
                OutfitCardView.PlaceHero(figure.Rect, well, hat: worn.Hat != null);
                return;
            }

            // A profile item as its own mark, 80% of the well (the Store's previews, the playtest's MetaCards.Preview).
            BoxLayout layout = BoxLayout.On(well);
            Box Mark(Box w)
            {
                float size = Mathf.Min(w.Width, w.Height) * 0.8f;
                return Box.FromCenter(w.CenterX, w.CenterY, size, size);
            }

            if (item.Kind == CosmeticKind.Frame)
            {
                BloomlingFigure portrait = BloomlingFigure.Create("Hero", well);
                portrait.Body.raycastTarget = false;
                portrait.ShowHero(Family.Bloom, null);
                layout.Add(portrait.Rect, w => Mark(w).Inset(Mark(w).Width * 0.16f));
            }

            Image mark = UiFactory.CreateImage("Item", well, Icon(item), BloomlingFigure.Tint(item));
            mark.preserveAspect = true;
            mark.raycastTarget = false;
            layout.Add(mark.rectTransform, w => item.Kind == CosmeticKind.Frame ? Mark(w) : Mark(w).Inset(Mark(w).Width * 0.08f));
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

            // A change raises the wardrobe's Changed, which refreshes the screen (OnWardrobeChanged); no change, nothing to redraw.
        }
    }
}
