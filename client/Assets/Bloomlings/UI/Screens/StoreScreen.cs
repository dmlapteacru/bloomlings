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
        string? Name = null);

    /// <summary>
    /// The Store of the design board's frame 17 (spec 002 FR-025; FR-051, T133): the Petals balance pill, then one
    /// row per item with its icon, its name and its price (the Petal symbol for Petal prices). Open from L12, it sells Petal packs, boosters for Petals, Remove Ads and the starter pack
    /// (offered once). Cosmetics for Petals join on their own tab after the Wardrobe unlock (L40). Real-money items need
    /// the platform store: offline they show as unavailable (FR-074), while everything for Petals always works. Nothing in
    /// a level ever requires it (FR-056).
    /// <para>
    /// A full-screen page since the owner's note of 2026-10-04 (spec 005 FR-029; contracts/look.md §6.6), every element
    /// placed from <see cref="ScreenLayout.ReferenceStore"/>, as the playtest's <c>StoreScreen</c>: over the Wardrobe's
    /// garden, the Wardrobe's page header on one line (<see cref="UiKit.PageHeader"/>: the back button, the wooden "Store"
    /// banner with ivy, the Petals pill); a wooden-framed cream panel (spec 005 FR-047) to the bottom of the screen with the raised tabs,
    /// the offline line, and cream rows with the booster's tile and count badge, the name and a cost pill with the lotus (a
    /// tap on the row buys), growing to fill a taller page, a page of them at a time between cream page arrows; and the
    /// cosmetics in the reference Wardrobe's look: the four family tabs with their heroes over a lighter panel, outfit
    /// cards three to a row, as many rows as the page holds (the "Default" look first, worn while the family wears
    /// nothing; each item shown on the family's hero with its cost pill, a tap buys) and the footer line between the page
    /// arrows. It opens over Home (the bottom menu's Shop, the Petals pill's "+") or over the Wardrobe (the same two); its
    /// back hides it, so the screen under it shows again. The bottom menu stands over the panel's foot, the Shop in its
    /// medallion (spec 005 FR-030); the list ends above it.
    /// </para>
    /// <para>
    /// The Animations tab (spec 005 FR-038, contracts/look.md §6.12; the playtest's <c>StoreScreen.Animations</c>), from
    /// the Store's unlock: the free pair's card first, then each bought clearing style's, three to a row from the list's
    /// top (<see cref="ReferenceStoreRegions.ClearingCard"/>), each well holding the style's live preview
    /// (<see cref="ClearPreviewView"/>); a style not owned shows its price, with the padlock badge (the preview still
    /// bright) before L40; the chosen one is checked. Since the owner's request of 2026-10-06 the previews loop all the time
    /// and each card carries its action button (<see cref="UiKit.ClearingButton"/>: Buy with the price, Choose, Chosen). A
    /// tap asks to buy the style from L40 and chooses an owned one; a refused tap says why on the footer line (from which
    /// level, or too few Petals).
    /// </para>
    /// <para>
    /// Every purchase here asks the purchase confirmation first (spec 005 FR-040, <see cref="UiKit.PurchaseCard"/>): the
    /// boosters, the cosmetics and the clearing styles for Petals, and the real-money rows, whose platform purchase sheet
    /// follows the confirmation. Nothing is spent until its Buy. The list is a page that scrolls (FR-041,
    /// <see cref="SwipePager"/>): a drag never buys, a swipe turns its page.
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

            // The pages' wooden frame round its cream panel (spec 005 FR-047, the popups' card frame), the tabs, the offline line and the list's area.
            screen._panel = UiKit.CardFrame("Panel", root, raycast: false);
            // The selected tab is a glossy green button on a plate, the other a parchment well (spec 005 §3.5).
            // They are built for the tabs a visit shows (SetTabs).
            screen._tabsBox = UiFactory.CreateRect("Tabs", root);
            screen._status = UiKit.Label("Status", root, Loc.T("store.offline"), T.Caption, UiTheme.Of(C.InkBrownSoft));
            screen._list = UiFactory.CreateRect("Items", root);

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

            // A drag over the list never buys and a swipe turns its page (spec 005 FR-041); the purchase confirmation over
            // everything (FR-040).
            UiKit.Scrolls(screen._root, screen.Swipe, screen._list);
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
            _wardrobe = wardrobe;
            _clearing = clearing;
            _level = level;
            _clearingChanged = clearingChanged;
            bool cosmetics = false;
            foreach (StoreItem item in items)
            {
                cosmetics |= item.Tab == StoreTab.Cosmetics;
            }

            // The tabs: the Shop, the Cosmetics while there are any (L40), the Animations with the clearing styles.
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

        /// <summary>
        /// Shows the page locked (spec 005 FR-030, contracts/look.md §6.7; <see cref="ScreenLayout.LockedPage"/>): the page
        /// header with the Petals pill showing <paramref name="petals"/>, the panel holding the locked notice of the Shop,
        /// available from <paramref name="level"/> (the roadmap's, <see cref="BottomNav.UnlockLevel"/>), and the bottom menu
        /// with the Shop raised. Its back hides it as usual.
        /// </summary>
        public void ShowLocked(int level, int petals)
        {
            _items = Array.Empty<StoreItem>();
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
                if (_pageIndex + step >= 0 && _pageIndex + step < _pages)
                {
                    Turn(step);
                }

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
            _tabs = UiKit.Tabs("TabRow", _tabsBox, labels, i => SetTab(_tabOrder[i]));
        }

        /// <summary>Places the page on the kit's regions for the screen's shape (contracts/look.md §6.6).</summary>
        private void Layout(bool tabs, bool storeAvailable)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            ReferenceStoreRegions r = ScreenLayout.ReferenceStore(w, h, insets, tabs, !storeAvailable);
            _regions = r;
            _header.Place(r.Header);

            // The panel runs to the bottom of the screen: its bottom corners go past the edge.
            float radius = r.PanelRadius(DesignTokens.ScaleFor(w, h));
            UiKit.PlaceScreen(_panel.rectTransform, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            UiKit.PlacePageFlowers(_flowers, r.Panel, ScreenLayout.BottomNavTop(w, h, insets));
            _tabsBox.gameObject.SetActive(tabs);
            UiKit.PlaceScreen(_tabsBox, r.Tabs);
            _status.gameObject.SetActive(!storeAvailable);
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
            Refresh();
        }

        private void Refresh()
        {
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                Destroy(_list.GetChild(i).gameObject);
            }

            _tabs?.Select(_tabOrder.IndexOf(_tab));
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
            else
            {
                ShopRows(shown);
            }
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
            bool unavailable = !petals && !item.Enabled;
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
            UiKit.PlaceBox(title.rectTransform, new Box(titleLeft, line.Top, titleRight, line.Bottom), line);

            if (unavailable)
            {
                TextMeshProUGUI note = UiKit.Label("Unavailable", row.transform, Loc.T("store.unavailable"), T.Caption, UiTheme.Of(C.InkBrownSoft));
                note.fontSizeMax = UiKit.Units(T.Caption.Size * grow);
                note.fontSize = note.fontSizeMax;
                UiKit.PlaceBox(note.rectTransform, Box.FromCenter(line.Right - (line.Height * 0.85f), line.CenterY, line.Width * 0.3f, line.Height * 0.6f), line);
                return;
            }

            // The price at the row's right end, as wide as its text, raised on its plate as a button (spec 005 FR-047): the
            // lotus and the Petals, or the store's price.
            CostPillView pill = UiKit.CostPill("Price", row.transform, petals ? Cost.Petals(item.PetalPrice!.Value) : Cost.Charges(0), button: true);
            UiKit.Decoration(pill.transform);
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
        /// The Animations tab (the playtest's <c>StoreScreen.Animations</c>): the free pair's card, then each bought style's
        /// card with its live preview, a page of them at a time, and the footer line between the page arrows.
        /// </summary>
        private void Animations(ClearingService clearing)
        {
            ReferenceStoreRegions r = _regions;
            var styles = new List<ClearStyle> { ClearStyle.Blossom };
            styles.AddRange(ClearStyles.Bought);
            int perPage = r.ClearingsPerPage;
            int pages = Mathf.Max(1, (styles.Count + perPage - 1) / perPage);
            _pages = pages;
            _outfitPage = Mathf.Clamp(_outfitPage, 0, pages - 1);
            for (int slot = 0; slot < perPage; slot++)
            {
                int index = (_outfitPage * perPage) + slot;
                if (index >= styles.Count)
                {
                    break;
                }

                ClearingCard(r.ClearingCard(slot), styles[index], clearing);
            }

            Footer(_clearingNote ?? Loc.T("store.animations_footer"), T.Body, _outfitPage > 0 ? () => TurnOutfits(-1) : (Action?)null, _outfitPage < pages - 1 ? () => TurnOutfits(1) : (Action?)null);
        }

        /// <summary>
        /// A clearing style's card (<c>ui.card.clearing</c> in an outfit card): its live preview in the well, looping all the
        /// time (the free card showing Blossom and Munchers by turns), its name and its action button
        /// (<see cref="UiKit.ClearingButton"/>, spec 005 FR-038 as amended on 2026-10-06): the green "Buy" with the price,
        /// "Choose", or "Chosen" with the check; before L40 the price pill and the padlock badge (the preview stays bright).
        /// The whole card is the button; the chosen card takes no tap.
        /// </summary>
        private void ClearingCard(Box box, ClearStyle style, ClearingService clearing)
        {
            bool free = ClearStyles.IsFree(style);
            ClearingAction action = clearing.ActionOf(style, _level);
            bool locked = action == ClearingAction.Locked;
            Cost? price = locked ? Cost.Petals(clearing.Price) : (Cost?)null;
            string name = Loc.T(free ? "clearing.free_pair" : ClearStyles.NameKey(style));
            Action? tap = action == ClearingAction.Chosen ? (Action?)null : () => TapClearing(style, name, clearing);
            OutfitCardView card = UiKit.OutfitCard("Clearing", _list, name, tap, price, pillRoom: true);
            PlaceInList((RectTransform)card.transform, box);
            ClearPreviewView.Create(card.Picture, style, pair: free, _wardrobe != null ? _wardrobe.OutfitOf : (Func<Family, Outfit>?)null);
            card.Show(action == ClearingAction.Chosen);
            if (!locked)
            {
                ClearingButtonView button = UiKit.ClearingButton("Action", card.transform);
                BoxLayout.On((RectTransform)card.transform).Add((RectTransform)button.transform, ClearingCardButton);
                button.Show(action, clearing.Price);
                return;
            }

            // The padlock badge at the well's lower right, where the check of a chosen card goes (its disc 0.22 of the card's
            // width; the badge's rect holds its ring too), and the name in the softer brown.
            RectTransform badge = UiKit.LockBadge("Lock", card.transform);
            BoxLayout.On((RectTransform)card.transform).Add(badge, b =>
            {
                Box well = OutfitCardView.WellBox(b, pillRoom: true);
                float disc = OutfitCardView.CardBox(b, pillRoom: true).Width * 0.22f;
                return Box.FromCenter(well.Right - (disc * 0.42f), well.Bottom - (disc * 0.42f), disc * 1.16f, disc * 1.16f);
            });
            card.Label.color = UiTheme.Of(C.InkBrownSoft);
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
                _confirm.ShowPetals(PurchaseOffer.ForPetals(name, clearing.Price), _petals, well => ClearPreviewView.Create(well, style, pair: false, _wardrobe != null ? _wardrobe.OutfitOf : (Func<Family, Outfit>?)null), () => Tapped(clearing.Tap(style, _level)), () => Tapped(ClearingTap.Short));
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
        /// The Cosmetics tab (the playtest's <c>StoreScreen.Outfits</c>): the four family tabs with their heroes over the
        /// lighter panel, the outfit cards of the chosen family three to a row, as many rows as the page holds
        /// (<see cref="ReferenceStoreRegions.OutfitsPerPage"/>), and the footer line between the page arrows.
        /// </summary>
        private void Outfits(List<StoreItem> shown, WardrobeService wardrobe)
        {
            ReferenceStoreRegions r = _regions;
            IReadOnlyList<Family> families = WardrobeService.Families;
            _family = Mathf.Clamp(_family, 0, families.Count - 1);
            Family family = families[_family];
            FamilyTabs(r.FamilyTabs, r.OutfitPanel, families, wardrobe);

            var cards = new List<StoreItem?> { null };
            cards.AddRange(shown);
            int perPage = r.OutfitsPerPage;
            int pages = Mathf.Max(1, (cards.Count + perPage - 1) / perPage);
            _pages = pages;
            _outfitPage = Mathf.Clamp(_outfitPage, 0, pages - 1);
            Outfit worn = wardrobe.OutfitOf(family);
            for (int slot = 0; slot < perPage; slot++)
            {
                int index = (_outfitPage * perPage) + slot;
                if (index >= cards.Count)
                {
                    break;
                }

                Box box = r.OutfitCard(slot);
                StoreItem? item = cards[index];
                if (item == null)
                {
                    OutfitCard(box, Loc.T("wardrobe.default"), worn.IsEmpty, family, null, null, null);
                }
                else
                {
                    Cost? price = item.PetalPrice.HasValue ? Cost.Petals(item.PetalPrice.Value) : (Cost?)null;
                    StoreItem buy = item;
                    OutfitCard(box, item.Name ?? item.Title, false, family, item.Cosmetic, price, item.Enabled ? () => Confirm(buy) : (Action?)null);
                }
            }

            Footer(Loc.T("wardrobe.footer"), T.Body, _outfitPage > 0 ? () => TurnOutfits(-1) : (Action?)null, _outfitPage < pages - 1 ? () => TurnOutfits(1) : (Action?)null);
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
            _outfitPage = 0;
            Refresh();
        }

        /// <summary>
        /// The family tabs joined to the panel below them (the playtest's <c>Kit.FamilyTabs</c>, <c>ui.tab.family</c>):
        /// the other tabs a little lower behind the panel's edge, the lighter panel with its tan outline, and the selected
        /// tab over it, flowing into it; each tab shows the family's 3D hero in its outfit and its name.
        /// </summary>
        private void FamilyTabs(Box tabs, Box panel, IReadOnlyList<Family> families, WardrobeService wardrobe)
        {
            Box[] cells = ScreenLayout.Row(tabs, families.Count, 10f * Scale, float.MaxValue, square: false);
            float sunk = tabs.Height * 0.07f;
            FamilyTabView? selected = null;
            for (int i = 0; i < cells.Length; i++)
            {
                int index = i;
                bool on = i == _family;
                Family family = families[i];
                FamilyTabView tab = UiKit.FamilyTab(family.ToString(), _list, Loc.T("family." + WardrobeService.FamilyKey(family)), () => SelectFamily(index));
                Box cell = cells[i];
                PlaceInList((RectTransform)tab.transform, new Box(cell.Left, cell.Top + (on ? 0f : sunk), cell.Right, panel.Top));
                BloomlingFigure hero = BloomlingFigure.Create("Hero", tab.Picture);
                UiFactory.Stretch(hero.Rect);
                hero.ShowHero(family, wardrobe.OutfitOf(family));
                hero.Body.raycastTarget = false;
                tab.Select(on);
                if (on)
                {
                    selected = tab;
                }
            }

            // The panel: lighter than the page's parchment, with a thin tan outline; the selected tab is drawn over it.
            float radius = UiKit.Units(26f);
            Image face = UiKit.RoundGradient("Panel", _list, C.ParchmentTop, C.CreamTop, _ => radius);
            PlaceInList(face.rectTransform, panel);
            Image outline = UiKit.RoundRing("PanelLine", _list, UiTheme.Of(C.CreamLine), _ => radius, _ => Mathf.Max(UiKit.Units(1f), UiKit.Units(DesignTokens.Garden.OutlineWidth) * 0.8f));
            PlaceInList(outline.rectTransform, panel);
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

            Image mark = UiFactory.CreateImage("Mark", picture, ProceduralSprites.Accessory(item.Shape), BloomlingFigure.Tint(item));
            mark.preserveAspect = true;
            layout.Add(mark.rectTransform, well => item.Kind == CosmeticKind.Frame ? Mark(well) : Mark(well).Inset(Mark(well).Width * 0.08f));
        }
    }
}
