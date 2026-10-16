using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Effects;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Clearing;
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
    /// The Store's tabs: Petal packs, boosters and Remove Ads; the cosmetics for Petals; and the board's clearing styles
    /// (spec 005 FR-038).
    /// </summary>
    public enum StoreTab
    {
        Shop,
        Cosmetics,
        Animations,
    }

    /// <summary>
    /// One row of the Store; a booster or cosmetic row has its icon, and a Petal price shows with the Petal symbol.
    /// <paramref name="BoosterId"/> and <paramref name="Charges"/> show a booster on its tile with its count badge,
    /// <paramref name="Cosmetic"/> an outfit card on the Cosmetics tab, and <paramref name="Name"/> replaces the title on
    /// them (spec 005 §4.6).
    /// </summary>
    public sealed record StoreItem(
        string Id,
        string Title,
        string PriceText,
        bool Enabled,
        Action Buy,
        StoreTab Tab = StoreTab.Shop,
        Sprite? Icon = null,
        Color? IconTint = null,
        int? PetalPrice = null,
        string? BoosterId = null,
        int? Charges = null,
        CosmeticItem? Cosmetic = null,
        string? Name = null,
        string? Badge = null,
        string? Description = null,
        bool PreviewOnly = false);

    /// <summary>
    /// The Store of the design board's frame 17 (spec 002 FR-025; FR-051, T133): the Petals balance pill, then one
    /// row per item with its icon, its name and its price (the Petal symbol for Petal prices). Open from L12, it sells
    /// boosters for Petals and previews the planned money packs without purchase actions. Cosmetics for Petals join on
    /// their own tab after the Wardrobe unlock (L40). The money offers await platform products; everything for Petals works. Nothing in
    /// a level ever requires it (FR-056).
    /// <para>
    /// The approved garden Shop uses a fixed header, cream segmented tabs and bottom menu. Only the content scrolls
    /// vertically. Packs use the illustrated offer layout; Cosmetics uses a fixed family row and two-column cards
    /// with existing heroes; Animations uses looping previews, six regular cards and a wide Confetti Parade card.
    /// All card skins are shared with the Skia/Android painter through ShopCatalogDesign.
    /// </para>
    /// <para>
    /// Petals purchases require confirmation and preserve existing ownership, prices and level gates. Dragging the
    /// ScrollRect does not buy a card. Money products currently show the owner-requested coming-soon stub.
    /// </para>
    /// <para>
    /// Before the Store unlocks (L12) the bottom menu's Shop opens the page locked (<see cref="ShowLocked"/>, the
    /// playtest's <c>StoreScreen.Locked</c>; the owner's request of 2026-10-04): the same garden, header and panel, the
    /// panel holding the locked notice (<see cref="LockedNoticeView"/>: "Available from level 12") instead of the tabs,
    /// the offline line, the rows and the page arrows.
    /// </para>
    /// </summary>
    public sealed class StoreScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private PageHeaderView _header = null!;
        private Image _panel = null!;
        private Image _shopBackground = null!;
        private RectTransform _shopViewport = null!;
        private RectTransform _shopContent = null!;
        private ScrollRect _shopScroll = null!;
        private TextMeshProUGUI _shopToast = null!;
        private string? _shopNote;
        private bool _storeAvailable;
        private Image[] _flowers = null!;
        private RectTransform _tabsBox = null!;
        private TabsView? _tabs;
        private TextMeshProUGUI _status = null!;
        private RectTransform _list = null!;
        private LockedNoticeView _notice = null!;
        private BottomNavView? _nav;
        private Func<HomeLook>? _navLook;
        private ReferenceStoreRegions _regions = null!;
        private IReadOnlyList<StoreItem> _items = Array.Empty<StoreItem>();
        private WardrobeService? _wardrobe;
        private StoreTab _tab = StoreTab.Shop;
        private readonly List<StoreTab> _tabOrder = new List<StoreTab>();
        private ClearingService? _clearing;
        private int _level;
        private Action? _clearingChanged;
        private string? _clearingNote;
        private int _pageIndex;
        private int _family;
        private int _outfitPage;
        private int _pages = 1;
        private int _petals;
        private PurchaseCardView _confirm = null!;

        public bool IsOpen => _root.activeSelf;

        /// <param name="onNav">A tap on another place of the bottom menu (spec 005 FR-030); null shows no menu.</param>
        /// <param name="navLook">The look that tells which places of the bottom menu are open (<see cref="BottomNav.IsOpen"/>); null: all of them.</param>
        public static StoreScreen Create(Transform parent, Action<NavPlace>? onNav = null, Func<HomeLook>? navLook = null)
        {
            // A full screen over Home (or over the Wardrobe, opened from its Petals "+") that takes every tap, on the
            // Wardrobe's garden (the owner's picture B7, or the drawn one), as the Wardrobe.
            Image shade = UiFactory.CreateImage("Store", parent, null, Color.clear, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var screen = shade.gameObject.AddComponent<StoreScreen>();
            screen._root = shade.gameObject;
            Transform root = shade.transform;
            BackdropView.Create(shade.rectTransform, OwnerPictures.Wardrobe, BackdropScene.Home);
            screen._shopBackground = UiFactory.CreateImage("ShopGarden", root, ShopArt.Get("shop/background"), Color.white);
            UiFactory.Stretch(screen._shopBackground.rectTransform);

            // The pages' wooden frame round its cream panel (spec 005 FR-047, the popups' card frame), the tabs, the offline line and the list's area.
            screen._panel = UiKit.CardFrame("Panel", root, raycast: false);
            // The selected tab is a glossy green button on a plate, the other a parchment well (spec 005 §3.5).
            // They are built for the tabs a visit shows (SetTabs).
            screen._tabsBox = UiFactory.CreateRect("Tabs", root);
            UiFactory.Stretch(UiFactory.CreateImage("TabInput",screen._tabsBox,null,Color.clear,raycast:true).rectTransform);
            screen._status = UiKit.Label("Status", root, Loc.T("store.offline"), T.Caption, UiTheme.Of(C.InkBrownSoft));
            screen._list = UiFactory.CreateRect("Items", root);
            Image viewport = UiFactory.CreateImage("ShopViewport", root, null, Color.clear, raycast: true);
            screen._shopViewport = viewport.rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();
            screen._shopContent = UiFactory.CreateRect("ShopContent", viewport.transform);
            screen._shopScroll = viewport.gameObject.AddComponent<ScrollRect>();
            screen._shopScroll.viewport = screen._shopViewport;
            screen._shopScroll.content = screen._shopContent;
            screen._shopScroll.horizontal = false;
            screen._shopScroll.vertical = true;
            screen._shopScroll.movementType = ScrollRect.MovementType.Clamped;
            screen._shopScroll.inertia = true;
            screen._shopScroll.decelerationRate = .015f;
            screen._shopScroll.scrollSensitivity = 40f;
            screen._shopToast = UiKit.Label("ShopNotice", viewport.transform, "", ShopBodyDesign.Small, Color.white, look: ShopBodyDesign.Letters);
            screen._shopToast.gameObject.SetActive(false);
            screen._shopViewport.gameObject.SetActive(false);
            screen._list.SetSiblingIndex(screen._shopViewport.GetSiblingIndex()+1);
            screen._tabsBox.SetSiblingIndex(screen._list.GetSiblingIndex()+1);

            // The locked notice in the list's place, shown only before the Store unlocks (FR-030).
            screen._notice = UiKit.LockedNotice("Locked", root);
            screen._notice.gameObject.SetActive(false);

            // The bottom menu over the panel's foot, the Shop in its medallion (FR-030).
            if (onNav != null)
            {
                screen._navLook = navLook;
                screen._nav = UiKit.BottomNav("BottomNav", root, place =>
                {
                    if (place != NavPlace.Shop)
                    {
                        onNav(place);
                    }
                });
            }

            // The flowers over the frame's corners and the bottom menu's ends (spec 005 FR-047), over the page and the menu.
            screen._flowers = UiKit.PageFlowers(root);

            // The header last, as on the Wardrobe; the pill shows the balance only (Petal packs are rows of the Shop).
            screen._header = UiKit.PageHeader(root, Loc.T("store.title"), screen.Hide, petals: true);

            // ScrollRect owns catalog drags; navigation remains outside its viewport. Purchase confirmation overlays it.
            screen._confirm = UiKit.PurchaseCard(root);
            shade.gameObject.SetActive(false);
            return screen;
        }

        /// <param name="storeAvailable">False offline or without a store: real-money rows show as unavailable.</param>
        /// <param name="wardrobe">Shows the cosmetics as outfit cards on the families' heroes; without it they are rows.</param>
        /// <param name="clearing">The board's clearing styles: shows the Animations tab (spec 005 FR-038); null: none.</param>
        /// <param name="level">The player's level, for the styles bought from L40.</param>
        /// <param name="clearingChanged">Called after a style was bought or chosen (the balance and Home refresh).</param>
        public void Show(IReadOnlyList<StoreItem> items, int petals, bool storeAvailable, WardrobeService? wardrobe = null, ClearingService? clearing = null, int level = 0, Action? clearingChanged = null)
        {
            _items = items;
            _storeAvailable = storeAvailable;
            _status.text = Loc.T("store.preview_status");
            _wardrobe = wardrobe;
            _clearing = clearing;
            _level = level;
            _clearingChanged = clearingChanged;
            bool cosmetics = false;
            foreach (StoreItem item in items)
            {
                cosmetics |= item.Tab == StoreTab.Cosmetics;
            }

            // Cosmetics includes the free Sprig equipment before the paid Wardrobe unlock.
            var tabs = new List<StoreTab> { StoreTab.Shop };
            if (cosmetics)
            {
                tabs.Add(StoreTab.Cosmetics);
            }

            if (clearing != null)
            {
                tabs.Add(StoreTab.Animations);
            }

            SetTabs(tabs);
            if (!_tabOrder.Contains(_tab))
            {
                _tab = StoreTab.Shop;
            }

            // Active before it is laid out, so the prices' widths are measured at once. A purchase shows the Store again
            // on the same tab and page.
            _root.SetActive(true);
            Layout(_tabOrder.Count > 1, storeAvailable);
            _petals = petals;
            _header.Petals?.Show(petals, storeUnlocked: false);
            Refresh();
        }

        public void SelectShop() => SetTab(StoreTab.Shop);

        /// <summary>
        /// Shows the page locked (spec 005 FR-030, contracts/look.md §6.7; <see cref="ScreenLayout.LockedPage"/>): the page
        /// header with the Petals pill showing <paramref name="petals"/>, the panel holding the locked notice of the Shop,
        /// available from <paramref name="level"/> (the roadmap's, <see cref="BottomNav.UnlockLevel"/>), and the bottom menu
        /// with the Shop raised. Its back hides it as usual.
        /// </summary>
        public void ShowLocked(int level, int petals)
        {
            _items = Array.Empty<StoreItem>();
            _shopBackground.gameObject.SetActive(false);
            _shopViewport.gameObject.SetActive(false);
            _panel.gameObject.SetActive(true);
            foreach (Image flower in _flowers) flower.gameObject.SetActive(true);
            _root.SetActive(true);
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            LockedPageRegions r = ScreenLayout.LockedPage(w, h, insets);
            _header.Place(r.Header);
            float radius = r.PanelRadius(DesignTokens.ScaleFor(w, h));
            UiKit.PlaceScreen(_panel.rectTransform, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            UiKit.PlacePageFlowers(_flowers, r.Panel, ScreenLayout.BottomNavTop(w, h, insets));
            _tabsBox.gameObject.SetActive(false);
            _status.gameObject.SetActive(false);
            _list.gameObject.SetActive(false);
            _notice.gameObject.SetActive(true);
            UiKit.PlaceScreen((RectTransform)_notice.transform, r.Notice);
            _notice.Show(NavPlace.Shop, level);
            _nav?.Show(NavPlace.Shop, _navLook?.Invoke() ?? HomeLook.All);
            _header.Petals?.Show(petals, storeUnlocked: false);
        }

        /// <summary>Hides the page; a purchase still asking is dropped, nothing bought.</summary>
        public void Hide()
        {
            _confirm.Cancel();
            _shopScroll.StopMovement();
            _shopContent.anchoredPosition = Vector2.zero;
            _shopNote = null;
            _root.SetActive(false);
        }

        /// <summary>A swipe over the list (<see cref="SwipePager"/>): the next or the previous page of the tab, when there is one.</summary>
        private void Swipe(int step)
        {
            if (_confirm.IsOpen)
            {
                return;
            }

            if (_tab == StoreTab.Shop)
            {
                return;
            }

            if (_outfitPage + step >= 0 && _outfitPage + step < _pages)
            {
                TurnOutfits(step);
            }
        }

        /// <summary>
        /// A row's or a card's tap (spec 005 FR-040): the purchase confirmation asks first with the item's picture, name
        /// and price, and the item is bought only on its Buy; short Petals say so without asking. A real-money item asks
        /// too, then the platform's own purchase sheet follows (the store confirms the payment once more).
        /// </summary>
        private void Confirm(StoreItem item)
        {
            if (item.PreviewOnly)
            {
                _shopNote = Loc.T("store.preview_status");
                Refresh();
                return;
            }
            string name = item.Name ?? item.Title;
            Family family = WardrobeService.Families[Mathf.Clamp(_family, 0, WardrobeService.Families.Count - 1)];
            Action<RectTransform> picture = rect => ItemPicture(rect, item, family);
            if (item.PetalPrice.HasValue)
            {
                _confirm.ShowPetals(PurchaseOffer.ForPetals(name, item.PetalPrice.Value), _petals, picture, item.Buy, () => { });
            }
            else
            {
                // The store's price, or while it is still loading ("…") the store's own price, named so.
                string price = item.PriceText.Trim().Length <= 1 ? Loc.T("purchase.price_unknown") : item.PriceText;
                _confirm.Show(PurchaseOffer.ForMoney(name, price), picture, item.Buy);
            }
        }

        /// <summary>An item's picture in the confirmation's well: a booster's icon, the hero in the cosmetic, else the lotus.</summary>
        private static void ItemPicture(RectTransform well, StoreItem item, Family family)
        {
            if (item.BoosterId != null)
            {
                Image icon = UiKit.BoosterIcon("Icon", well, item.BoosterId);
                UiFactory.Place(icon.rectTransform, 0.08f, 0.08f, 0.92f, 0.92f);
                return;
            }

            if (item.Cosmetic != null)
            {
                Preview(well, family, item.Cosmetic);
                return;
            }

            Image lotus = UiKit.PetalIcon("Lotus", well);
            UiFactory.Place(lotus.rectTransform, 0.14f, 0.14f, 0.86f, 0.86f);
        }

        private static float Scale => DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);

        /// <summary>Builds the tab row for <paramref name="tabs"/> when they differ from the ones shown.</summary>
        private void SetTabs(List<StoreTab> tabs)
        {
            if (_tabs != null && tabs.Count == _tabOrder.Count && tabs.TrueForAll(_tabOrder.Contains))
            {
                return;
            }

            if (_tabs != null)
            {
                Destroy(_tabs.gameObject);
            }

            _tabOrder.Clear();
            _tabOrder.AddRange(tabs);
            string[] labels = tabs.ConvertAll(tab => Loc.T(tab switch
            {
                StoreTab.Cosmetics => "store.tab_cosmetics",
                StoreTab.Animations => "store.tab_animations",
                _ => "store.tab_shop",
            })).ToArray();
            _tabs = UiKit.Tabs("TabRow", _tabsBox, labels, i => SetTab(_tabOrder[i]), tabs.ConvertAll(t => t == StoreTab.Cosmetics ? "store.tab_cosmetics" : t == StoreTab.Animations ? "store.tab_animations" : "store.tab_shop").ToArray());
        }

        /// <summary>Places the page on the kit's regions for the screen's shape (contracts/look.md §6.6).</summary>
        private void Layout(bool tabs, bool storeAvailable)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            Sprite? garden = _shopBackground.sprite;
            if (garden != null)
            {
                float cover = Mathf.Max(w / garden.texture.width, h / garden.texture.height);
                UiKit.PlaceScreen(_shopBackground.rectTransform, Box.FromCenter(w * 0.5f, h * 0.5f, garden.texture.width * cover, garden.texture.height * cover));
            }
            ReferenceStoreRegions r = ScreenLayout.ReferenceStore(w, h, insets, tabs, !storeAvailable && _tab != StoreTab.Shop);
            _regions = r;
            Box viewport = _tab == StoreTab.Shop ? ShopBodyDesign.Bounds(r) : ShopCatalogDesign.Viewport(r,_tab==StoreTab.Cosmetics);
            UiKit.PlaceScreen(_shopViewport, ShopBodyDesign.ScrollViewport(r,viewport));
            UiKit.PlaceBox(_shopToast.rectTransform, new Box(viewport.Left, viewport.Bottom - r.W * 0.12f, viewport.Right, viewport.Bottom - r.W * 0.02f), viewport);
            _header.Place(r.Header);

            // The panel runs to the bottom of the screen: its bottom corners go past the edge.
            float radius = r.PanelRadius(DesignTokens.ScaleFor(w, h));
            UiKit.PlaceScreen(_panel.rectTransform, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            UiKit.PlacePageFlowers(_flowers, r.Panel, ScreenLayout.BottomNavTop(w, h, insets));
            _tabsBox.gameObject.SetActive(tabs);
            UiKit.PlaceScreen(_tabsBox, r.Tabs);
            _status.gameObject.SetActive(false);
            UiKit.PlaceScreen(_status.rectTransform, r.Status);
            _notice.gameObject.SetActive(false);
            _list.gameObject.SetActive(true);
            UiKit.PlaceScreen(_list, r.List);
            _nav?.Show(NavPlace.Shop, _navLook?.Invoke() ?? HomeLook.All);
        }

        private void SetTab(StoreTab tab)
        {
            _tab = tab;
            _pageIndex = 0;
            _outfitPage = 0;
            _clearingNote = null;
            _shopNote = null;
            _shopScroll.StopMovement();
            _shopContent.anchoredPosition = Vector2.zero;
            Layout(_tabOrder.Count > 1, _storeAvailable);
            Refresh();
        }

        private void Refresh()
        {
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                Destroy(_list.GetChild(i).gameObject);
            }

            _tabs?.Select(_tabOrder.IndexOf(_tab));
            bool shop = _tab == StoreTab.Shop;
            _list.gameObject.SetActive(_tab==StoreTab.Cosmetics);
            _shopViewport.gameObject.SetActive(true);
            string? note=shop ? _shopNote : _clearingNote;
            _shopToast.gameObject.SetActive(note!=null);
            _shopToast.text=note??"";
            _shopBackground.gameObject.SetActive(true);
            _panel.gameObject.SetActive(false);
            foreach(Image flower in _flowers)flower.gameObject.SetActive(false);
            _pages = 1;
            if (_tab == StoreTab.Animations && _clearing != null)
            {
                Animations(_clearing);
                return;
            }

            var shown = new List<StoreItem>();
            foreach (StoreItem item in _items)
            {
                if (item.Tab == _tab)
                {
                    shown.Add(item);
                }
            }

            if (_tab == StoreTab.Cosmetics && _wardrobe != null)
            {
                Outfits(shown, _wardrobe);
            }
            else if (_tab == StoreTab.Shop)
            {
                ShopCards(shown);
            }
            else
            {
                ShopRows(shown);
            }
        }

        private void ShopCards(List<StoreItem> shown)
        {
            var byId = new Dictionary<string, StoreItem>();
            foreach (StoreItem item in shown) byId[item.Id] = item;
            StoreItem? Item(string id) => byId.TryGetValue(id, out StoreItem? found) ? found : null;
            Box bounds = ShopBodyDesign.Bounds(_regions);
            _pages = 1;
            int Price(Services.Save.BoosterKind kind) => Item("booster_" + kind)?.PetalPrice ?? 0;
            bool Open(Services.Save.BoosterKind kind) => Item("booster_" + kind) != null;
            List<ShopVisual> nodes = ShopBodyDesign.Build(bounds, Loc.T, id => Item(id) != null, Price, Open);
            float height = ShopBodyDesign.ContentHeight(nodes, bounds);
            float offset = Mathf.Clamp(_shopContent.anchoredPosition.y, 0, (height - bounds.Height) / UiKit.PixelsPerUnit);
            Box viewport=ShopBodyDesign.ScrollViewport(_regions,bounds);height+=viewport.Height-bounds.Height;
            for (int i = _shopContent.childCount - 1; i >= 0; i--) Destroy(_shopContent.GetChild(i).gameObject);
            _shopContent.anchorMin = new Vector2(0, 1);
            _shopContent.anchorMax = new Vector2(1, 1);
            _shopContent.pivot = new Vector2(0.5f, 1);
            _shopContent.sizeDelta = new Vector2(0, height / UiKit.PixelsPerUnit);
            _shopContent.anchoredPosition = new Vector2(0, offset);
            ShopBodyView.Draw(_shopContent, new Box(bounds.Left, viewport.Top, bounds.Right, viewport.Top + height), nodes, Item, Confirm);
        }

        /// <summary>Places a child of the list on a screen box (relative to the list's own box).</summary>
        private RectTransform PlaceInList(RectTransform rect, Box box) => UiKit.PlaceBox(rect, box, _regions.List);

        // ---- The Shop's rows (spec 005 §4.6) ----

        private void ShopRows(List<StoreItem> shown)
        {
            ReferenceStoreRegions r = _regions;
            if (shown.Count == 0)
            {
                TextMeshProUGUI empty = UiKit.Label("Empty", _list, Loc.T("store.all_owned"), T.Body, UiTheme.Of(C.InkBrownSoft));
                PlaceInList(empty.rectTransform, r.Row(0, 1));
                return;
            }

            // A page of rows filling the list (the rows grow on a taller page), the footer when there are more.
            int perPage = r.RowsPerPage(shown.Count);
            int pages = Mathf.Max(1, (shown.Count + perPage - 1) / perPage);
            _pages = pages;
            _pageIndex = Mathf.Clamp(_pageIndex, 0, pages - 1);
            float grow = r.RowHeight(shown.Count) / (r.W * ReferenceStoreRegions.RowTypeShare);
            for (int slot = 0; slot < perPage; slot++)
            {
                int index = (_pageIndex * perPage) + slot;
                if (index >= shown.Count)
                {
                    break;
                }

                Row(shown[index], r.Row(slot, shown.Count), grow);
            }

            if (pages > 1)
            {
                Footer(Loc.F("common.page", _pageIndex + 1, pages), T.Caption, _pageIndex > 0 ? () => Turn(-1) : (Action?)null, _pageIndex < pages - 1 ? () => Turn(1) : (Action?)null);
            }
        }

        private void Turn(int by)
        {
            _pageIndex += by;
            Refresh();
        }

        /// <summary>
        /// A Shop row (the playtest's <c>StoreScreen.BoosterRow</c> and <c>MoneyRow</c>): a cream row, the item's tile at
        /// its left end (a booster's colored icon with its count badge, else the lotus), the name in brown, and the price at
        /// its right end: a cost pill with the lotus for Petals, the store's price on a cream pill, or "Unavailable" while
        /// real money cannot be spent (the row then fades to half). A tap on the row buys; a price the player cannot pay
        /// fades its pill. The tile is 0.8 of the row tall and the letters grow with the row (<paramref name="grow"/>: its
        /// height over a <see cref="ReferenceStoreRegions.RowTypeShare"/> row's).
        /// </summary>
        private void Row(StoreItem item, Box line, float grow)
        {
            Image row = UiKit.Row(item.Id, _list, highlighted: false);
            PlaceInList(row.rectTransform, line);
            bool petals = item.PetalPrice.HasValue;
            bool unavailable = !petals && !item.Enabled && !item.PreviewOnly;
            if (unavailable)
            {
                row.gameObject.AddComponent<CanvasGroup>().alpha = 0.5f;
            }

            // The item's tile: the booster tile's cream squircle raised on its wooden plate (§3.7, spec 005 FR-047), 0.8 of the
            // row tall (0.12 W on the smallest row, as the reference's booster boxes), its icon 74% of it as on a booster box.
            float size = line.Height * 0.8f;
            Box tile = Box.FromCenter(line.Left + (line.Height * 0.14f) + (size / 2f), line.CenterY - (line.Height * 0.02f), size, size);
            GardenButton face = UiKit.RaisedButton("Tile", row.transform, GardenLook.Cream, 0.26f, raycast: false, square: true);
            UiKit.PlaceBox((RectTransform)face.transform, tile, line);
            if (item.BoosterId != null)
            {
                Image icon = UiKit.BoosterIcon("Icon", row.transform, item.BoosterId);
                UiKit.PlaceBox(icon.rectTransform, Box.FromCenter(tile.CenterX, tile.CenterY, size * 0.74f, size * 0.74f), line);
                if (item.Charges.HasValue)
                {
                    float badge = size * 0.3f;
                    TextMeshProUGUI count = UiKit.CountBadge("Charges", row.transform, out Image disc);
                    count.text = item.Charges.Value.ToString(CultureInfo.InvariantCulture);
                    UiKit.PlaceBox(disc.rectTransform, Box.FromCenter(tile.Right - (badge * 0.1f), tile.Bottom - (badge * 0.1f), badge * 1.26f, badge * 1.26f), line);
                }
            }
            else if (item.Icon != null)
            {
                Image icon = UiFactory.CreateImage("Icon", row.transform, item.Icon, item.IconTint ?? Color.white);
                icon.preserveAspect = true;
                UiKit.PlaceBox(icon.rectTransform, tile.Inset(size * 0.16f), line);
            }
            else
            {
                Image lotus = UiKit.PetalIcon("Icon", row.transform);
                UiKit.PlaceBox(lotus.rectTransform, tile.Inset(size * 0.14f), line);
            }

            TextMeshProUGUI title = UiKit.Label("Title", row.transform, item.Name ?? item.Title, T.ButtonSecondary, UiTheme.Of(C.InkBrown), TextAlignmentOptions.Left, TextLook.Plain(C.InkBrown));
            title.fontSizeMax = UiKit.Units(T.ButtonSecondary.Size * grow);
            title.fontSize = title.fontSizeMax;
            float titleLeft = tile.Right + (size * 0.22f);
            // At most 42% of the row, and never under the price pill (its width estimated as the pill's layout below
            // does before it is measured: a 4-digit booster price since 2026-10-05).
            float pillH = line.Height * 0.7f;
            string priceText = unavailable ? string.Empty : petals ? NumberText.Group(item.PetalPrice!.Value) : item.PriceText;
            float pillLeft = line.Right - (line.Height * 0.14f) - ((pillH * 0.45f * 0.6f * Mathf.Max(1, priceText.Length)) + (pillH * (petals ? 1.75f : 0.9f)));
            float titleRight = unavailable ? titleLeft + (line.Width * 0.42f) : Mathf.Min(titleLeft + (line.Width * 0.42f), pillLeft - (line.Height * 0.12f));
            float titleTop = item.Badge == null ? line.Top : line.Top + (line.Height * 0.22f);
            float titleBottom = item.Description == null ? line.Bottom : line.Top + (line.Height * 0.64f);
            UiKit.PlaceBox(title.rectTransform, new Box(titleLeft, titleTop, titleRight, titleBottom), line);
            if (item.Badge != null)
            {
                TextMeshProUGUI badge = UiKit.Label("Badge", row.transform, item.Badge, T.Caption, UiTheme.Of(C.BoosterBloomBurst), TextAlignmentOptions.Left, TextLook.Plain(C.BoosterBloomBurst));
                badge.fontSizeMax = UiKit.Units(T.Caption.Size * grow);
                badge.fontSize = badge.fontSizeMax;
                UiKit.PlaceBox(badge.rectTransform, new Box(titleLeft, line.Top, titleRight, line.Top + (line.Height * 0.27f)), line);
            }

            if (item.Description != null)
            {
                TextMeshProUGUI details = UiKit.Label("Details", row.transform, item.Description, T.Caption, UiTheme.Of(C.InkBrownSoft), TextAlignmentOptions.Left, TextLook.Plain(C.InkBrownSoft));
                details.fontSizeMax = UiKit.Units(T.Caption.Size * grow);
                details.fontSize = details.fontSizeMax;
                UiKit.PlaceBox(details.rectTransform, new Box(titleLeft, line.Top + (line.Height * 0.61f), titleRight, line.Bottom), line);
            }

            if (unavailable)
            {
                TextMeshProUGUI note = UiKit.Label("Unavailable", row.transform, Loc.T("store.unavailable"), T.Caption, UiTheme.Of(C.InkBrownSoft));
                note.fontSizeMax = UiKit.Units(T.Caption.Size * grow);
                note.fontSize = note.fontSizeMax;
                UiKit.PlaceBox(note.rectTransform, Box.FromCenter(line.Right - (line.Height * 0.85f), line.CenterY, line.Width * 0.3f, line.Height * 0.6f), line);
                return;
            }

            // The price at the row's right end, as wide as its text: the green buy pill raised on its plate (spec 005 FR-049, as
            // the Animations tab's prices) with the lotus and the Petals, or the store's price.
            CostPillView pill = UiKit.CostPill("Price", row.transform, petals ? Cost.Petals(item.PetalPrice!.Value) : Cost.Charges(0), button: true);
            TextMeshProUGUI amount = pill.GetComponentInChildren<TextMeshProUGUI>();
            if (!petals)
            {
                amount.text = item.PriceText;
            }

            if (!item.Enabled)
            {
                pill.gameObject.AddComponent<CanvasGroup>().alpha = 0.45f;
            }

            BoxLayout.On(row.rectTransform).Watch(amount).Add((RectTransform)pill.transform, b =>
            {
                float h = b.Height * 0.7f;
                float measured = KitText.Measure(amount, h * 0.45f);
                float text = measured > 0f ? measured : h * 0.45f * 0.6f * Mathf.Max(1, amount.text.Length);
                float width = text + (h * (petals ? 1.75f : 0.9f));
                float right = b.Right - (b.Height * 0.14f);
                return new Box(right - width, b.CenterY - (h / 2f), right, b.CenterY + (h / 2f));
            });

            if (item.Enabled)
            {
                UiKit.TapTarget(row, () => Confirm(item));
            }
        }

        /// <summary>
        /// The footer line between the two cream page arrows (<see cref="ReferenceStoreRegions.Footer"/>, the playtest's
        /// <c>StoreScreen.Footer</c>): <paramref name="text"/> centered in <c>ink.brown_soft</c>, the ‹ and › buttons
        /// (their cushions in touch-sized squares) at its ends, greyed where there is no page to turn to.
        /// </summary>
        private void Footer(string text, TypeStyle style, Action? previous, Action? next)
        {
            ReferenceStoreRegions r = _regions;
            Box line = r.Footer;
            float touch = DesignTokens.Size.TouchMin * Scale;
            float room = line.Height + (12f * Scale);
            TextMeshProUGUI label = UiKit.Label("Footer", _list, text, style, UiTheme.Of(C.InkBrownSoft));
            PlaceInList(label.rectTransform, new Box(line.Left + room, line.Top, line.Right - room, line.Bottom));
            if (previous == null && next == null)
            {
                return;
            }

            Button back = UiKit.PageArrow("Previous", _list, next: false, previous ?? (() => { }));
            back.interactable = previous != null;
            PlaceInList((RectTransform)back.transform, Box.FromCenter(r.PagePrevious.CenterX, r.PagePrevious.CenterY, touch, touch));
            Button forward = UiKit.PageArrow("Next", _list, next: true, next ?? (() => { }));
            forward.interactable = next != null;
            PlaceInList((RectTransform)forward.transform, Box.FromCenter(r.PageNext.CenterX, r.PageNext.CenterY, touch, touch));
        }

        // ---- The board's clearing styles (spec 005 FR-038, contracts/look.md §6.12) ----

        /// <summary>
        /// Rebuilds the vertical catalog while preserving its clamped scroll offset.
        /// </summary>
        private Box CatalogContent(Box area,float height)
        {
            _pages=1;
            float offset=Mathf.Clamp(_shopContent.anchoredPosition.y,0,Mathf.Max(0,height-area.Height)/UiKit.PixelsPerUnit);
            Box viewport=ShopBodyDesign.ScrollViewport(_regions,area);height+=viewport.Height-area.Height;
            for(int i=_shopContent.childCount-1;i>=0;i--)Destroy(_shopContent.GetChild(i).gameObject);
            _shopContent.anchorMin=new Vector2(0,1);_shopContent.anchorMax=new Vector2(1,1);
            _shopContent.pivot=new Vector2(.5f,1);
            _shopContent.sizeDelta=new Vector2(0,height/UiKit.PixelsPerUnit);
            _shopContent.anchoredPosition=new Vector2(0,offset);
            return new Box(area.Left,viewport.Top,area.Right,viewport.Top+height);
        }
        private void Animations(ClearingService clearing)
        {
            Box area=ShopCatalogDesign.Viewport(_regions,false);
            Box content=CatalogContent(area,ShopCatalogDesign.AnimationsHeight(area));
            var styles=new List<ClearStyle>(ClearStyles.Free);styles.AddRange(ClearStyles.Bought);
            for(int i=0;i<styles.Count;i++)ClearingCatalogCard(ShopCatalogDesign.AnimationCard(area,i),content,styles[i],clearing,i==6);
            TextMeshProUGUI caption=UiKit.KitLabel("Caption",_shopContent,Loc.T("store.animations_caption"),ShopBodyDesign.Small,ShopBodyDesign.Letters);
            UiKit.PlaceBox(caption.rectTransform,ShopCatalogDesign.Caption(area),content);
        }

        /// <summary>
        /// A clearing style's card (<c>ui.card.clearing</c> in an outfit card): its live preview in the well, looping all the
        /// time, its name and its action button
        /// (<see cref="UiKit.ClearingButton"/>, spec 005 FR-038 as amended on 2026-10-06): the green "Buy" with the price,
        /// "Choose", or "Chosen" with the check; before L40 the price pill and the padlock badge (the preview stays bright).
        /// The whole card is the button; the chosen card takes no tap.
        /// </summary>
        private void ClearingCatalogCard(Box box,Box content,ClearStyle style,ClearingService clearing,bool wide)
        {
            ClearingAction action=clearing.ActionOf(style,_level);
            string name=Loc.T(ClearStyles.NameKey(style));
            Action? tap=action==ClearingAction.Chosen ? (Action?)null : ()=>TapClearing(style,name,clearing);
            RectTransform card=ShopCatalogView.Create(_shopContent,false,wide,name,action,clearing.PriceOf(style),action==ClearingAction.Chosen,ClearStyles.IsFree(style),tap,
                picture=>ClearPreviewView.Create(picture,style,_wardrobe!=null ? _wardrobe.OutfitOf : (Func<Family,Outfit>?)null,catalog:true));
            UiKit.PlaceBox(card,box,content);
        }

        /// <summary>The action button's box in a card's own box (<see cref="Design.ClearingCard.Button"/>).</summary>
        private static Box ClearingCardButton(Box card) => Design.ClearingCard.Button(card);

        /// <summary>
        /// A tap on a clearing style's card: a purchase asks the purchase confirmation first and buys only on its Buy
        /// (FR-040); choosing an owned style happens at once; a refused tap says why on the footer line (from which level,
        /// too few Petals).
        /// </summary>
        private void TapClearing(ClearStyle style, string name, ClearingService clearing)
        {
            if (clearing.Check(style, _level) == ClearingTap.Bought)
            {
                _confirm.ShowPetals(PurchaseOffer.ForPetals(name, clearing.PriceOf(style)), _petals, well => ClearPreviewView.Create(well, style, _wardrobe != null ? _wardrobe.OutfitOf : (Func<Family, Outfit>?)null), () => Tapped(clearing.Tap(style, _level)), () => Tapped(ClearingTap.Short));
                return;
            }

            Tapped(clearing.Tap(style, _level));
        }

        /// <summary>What a clearing tap did: the page again with the new balance, or why not on the footer line.</summary>
        private void Tapped(ClearingTap tap)
        {
            switch (tap)
            {
                case ClearingTap.Locked:
                    _clearingNote = Loc.F("locked.message", ClearStyles.BuyFromLevel);
                    break;
                case ClearingTap.Short:
                    _clearingNote = Loc.T("gameplay.not_enough_petals");
                    break;
                case ClearingTap.Unavailable:
                    _clearingNote = Loc.T("store.unavailable");
                    break;
                default:
                    // Bought or chosen: the page shows again with the new balance (or here, without a caller).
                    _clearingNote = null;
                    if (_clearingChanged != null)
                    {
                        _clearingChanged();
                        return;
                    }

                    break;
            }

            Refresh();
        }

        // ---- The cosmetics in the reference Wardrobe's look (spec 005 §4.6) ----

        /// <summary>
        /// Cosmetics: a fixed family row and a two-column catalog of Default, free equipment and items still for sale.
        /// </summary>
        private void Outfits(List<StoreItem> shown,WardrobeService wardrobe)
        {
            ReferenceStoreRegions r=_regions;
            IReadOnlyList<Family> families=WardrobeService.Families;
            _family=Mathf.Clamp(_family,0,families.Count-1);
            Family family=families[_family];
            shown=shown.FindAll(i=>i.Cosmetic==null||i.Cosmetic.Fits(family));
            FamilyTabs(ShopCatalogDesign.FamilyTabs(r),r.OutfitPanel,families,wardrobe);
            Box area=ShopCatalogDesign.Viewport(r,true);
            Box content=CatalogContent(area,ShopCatalogDesign.CosmeticsHeight(area,shown.Count+1));
            bool chosen=wardrobe.OutfitOf(family).IsEmpty;
            string status=Loc.T(chosen ? "clearing.chosen" : "wardrobe.default");
            RectTransform plain=ShopCatalogView.Create(_shopContent,true,false,Loc.T("wardrobe.default"),chosen ? ClearingAction.Chosen : ClearingAction.Choose,0,chosen,false,()=>{wardrobe.UnequipAll(family);Refresh();},picture=>Preview(picture,family,null),status);
            UiKit.PlaceBox(plain,ShopCatalogDesign.CosmeticCard(area,0),content);
            for(int i=0;i<shown.Count;i++)
            {
                StoreItem item=shown[i];
                CosmeticItem? cosmetic=item.Cosmetic;
                bool owned=cosmetic!=null&&wardrobe.Owns(cosmetic),equipped=cosmetic!=null&&wardrobe.EquippedFor(family,cosmetic.Kind)?.Id==cosmetic.Id;
                Action? tap=owned ? ()=>{if(equipped)wardrobe.Unequip(family,cosmetic!.Kind);else wardrobe.Equip(family,cosmetic!.Id);Refresh();} : item.Enabled ? ()=>Confirm(item) : (Action?)null;
                RectTransform card=ShopCatalogView.Create(_shopContent,true,false,item.Name??item.Title,owned ? equipped ? ClearingAction.Chosen : ClearingAction.Choose : ClearingAction.Buy,item.PetalPrice??0,equipped,cosmetic?.Free==true,tap,picture=>Preview(picture,family,cosmetic),owned&&equipped ? Loc.T("wardrobe.remove") : null);
                UiKit.PlaceBox(card,ShopCatalogDesign.CosmeticCard(area,i+1),content);
            }
        }

        private void TurnOutfits(int by)
        {
            _outfitPage += by;
            _clearingNote = null;
            Refresh();
        }

        private void SelectFamily(int index)
        {
            _family = index;
            _shopScroll.StopMovement();
            _shopContent.anchoredPosition=Vector2.zero;
            _outfitPage = 0;
            Refresh();
        }

        /// <summary>
        /// Fixed cream family navigation outside the scrolling catalog, with the shared family icons and live labels.
        /// </summary>
        private void FamilyTabs(Box tabs, Box panel, IReadOnlyList<Family> families, WardrobeService wardrobe)
        {
            Box barBox=NavigationSkin.FamilyBar(tabs,families.Count);
            RectTransform bar=UiKit.NavigationBar("FamilyNavigation",_list,families.Count);
            PlaceInList(bar,barBox);
            FamilyTabView? selected = null;
            for (int i = 0; i < families.Count; i++)
            {
                int index = i;
                bool on = i == _family;
                Family family = families[i];
                FamilyTabView tab = UiKit.FamilyTab(family.ToString(), _list, Loc.T("family." + WardrobeService.FamilyKey(family)), () => SelectFamily(index));
                Box cell = NavigationSkin.Segment(barBox,i,families.Count);
                PlaceInList((RectTransform)tab.transform, cell);
                tab.Select(on);
                if (on)
                {
                    selected = tab;
                }
            }

            selected?.transform.SetAsLastSibling();
        }

        /// <summary>
        /// An outfit card (the shared <see cref="UiKit.OutfitCard"/>, the playtest's <c>Kit.OutfitCard</c>) in
        /// <paramref name="box"/>: the family's hero wearing the item (or the item's own mark for a profile item) in the
        /// well, the name below it; the worn one green with the check. Every card keeps the cost pill's room so a row lines
        /// up; a <paramref name="cost"/> shows the pill, and the card is the touch target when <paramref name="buy"/> is set.
        /// </summary>
        private void OutfitCard(Box box, string name, bool worn, Family family, CosmeticItem? item, Cost? cost, Action? buy)
        {
            OutfitCardView card = UiKit.OutfitCard("Outfit", _list, name, buy, cost, pillRoom: true);
            PlaceInList((RectTransform)card.transform, box);
            Preview(card.Picture, family, item);
            card.Show(worn);
        }

        /// <summary>
        /// An outfit card's picture (the playtest's <c>StoreScreen.Preview</c>): the family's hero wearing the item (skins,
        /// hats, trails, faces), or its plain look for the Default card, its feet near the well's bottom and clipped by the
        /// well; a profile item (frame, badge, marker) as its own mark, a frame around a small hero.
        /// </summary>
        private static void Preview(RectTransform picture, Family family, CosmeticItem? item)
        {
            HeroFrame? icon=item!=null ? SprigGearArt.Icon(item.Id) : null;
            if(icon.HasValue)
            {
                Image image=UiFactory.CreateImage("Accessory",picture,SprigGearView.Picture(icon.Value.Name),Color.white);
                image.preserveAspect=true;image.raycastTarget=false;UiFactory.Stretch(image.rectTransform);return;
            }
            if (item == null || item.IsWorn)
            {
                Outfit? outfit = item == null ? null : item.Kind switch
                {
                    CosmeticKind.Skin => new Outfit(item, null, null, null),
                    CosmeticKind.Hat => new Outfit(null, item, null, null),
                    CosmeticKind.Trail => new Outfit(null, null, item, null),
                    _ => new Outfit(null, null, null, item),
                };
                BloomlingFigure hero = BloomlingFigure.Create("Hero", picture);
                hero.ShowHero(family, outfit);
                hero.Body.raycastTarget = false;
                OutfitCardView.PlaceHero(hero.Rect, picture, hat: item != null && item.Kind == CosmeticKind.Hat);
                return;
            }

            BoxLayout layout = BoxLayout.On(picture);
            Box Mark(Box well)
            {
                float size = Mathf.Min(well.Width, well.Height) * 0.8f;
                return Box.FromCenter(well.CenterX, well.CenterY, size, size);
            }

            if (item.Kind == CosmeticKind.Frame)
            {
                BloomlingFigure small = BloomlingFigure.Create("Hero", picture);
                small.ShowHero(family, null);
                small.Body.raycastTarget = false;
                layout.Add(small.Rect, well => Mark(well).Inset(Mark(well).Width * 0.16f));
            }

            string? asset=ShopCatalogDesign.Accessory(item.Id);
            Sprite? illustration=asset!=null ? ShopArt.Get(asset) : null;
            Image mark = UiFactory.CreateImage("Mark", picture, illustration??ProceduralSprites.Accessory(item.Shape), illustration!=null ? Color.white : BloomlingFigure.Tint(item));
            mark.preserveAspect = true;
            layout.Add(mark.rectTransform, well => item.Kind == CosmeticKind.Frame ? Mark(well) : Mark(well).Inset(Mark(well).Width * 0.08f));
        }
    }
}
