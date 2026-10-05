using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>The Store's tabs: Petal packs, boosters and Remove Ads; then the cosmetics for Petals.</summary>
    public enum StoreTab
    {
        Shop,
        Cosmetics,
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
    /// banner with ivy, the Petals pill); a parchment panel to the bottom of the screen with the green and parchment tabs,
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
        private RectTransform _tabsBox = null!;
        private TabsView _tabs = null!;
        private TextMeshProUGUI _status = null!;
        private RectTransform _list = null!;
        private LockedNoticeView _notice = null!;
        private BottomNavView? _nav;
        private Func<HomeLook>? _navLook;
        private ReferenceStoreRegions _regions = null!;
        private IReadOnlyList<StoreItem> _items = Array.Empty<StoreItem>();
        private WardrobeService? _wardrobe;
        private StoreTab _tab = StoreTab.Shop;
        private int _pageIndex;
        private int _family;
        private int _outfitPage;

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

            // The parchment panel (a card's radius), the tabs, the offline line and the list's area.
            screen._panel = UiKit.Paper("Panel", root, b => Mathf.Max(UiKit.Units(DesignTokens.Radius.CardMin), b.Width * DesignTokens.Radius.Card), DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard, raycast: false);
            // The selected tab is a glossy green button on a plate, the other a parchment well (spec 005 §3.5).
            screen._tabsBox = UiFactory.CreateRect("Tabs", root);
            string[] labels = { Loc.T("store.tab_shop"), Loc.T("store.tab_cosmetics") };
            screen._tabs = UiKit.Tabs("TabRow", screen._tabsBox, labels, i => screen.SetTab((StoreTab)i));
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

            // The header last, as on the Wardrobe; the pill shows the balance only (Petal packs are rows of the Shop).
            screen._header = UiKit.PageHeader(root, Loc.T("store.title"), screen.Hide, petals: true);
            shade.gameObject.SetActive(false);
            return screen;
        }

        /// <param name="storeAvailable">False offline or without a store: real-money rows show as unavailable.</param>
        /// <param name="wardrobe">Shows the cosmetics as outfit cards on the families' heroes; without it they are rows.</param>
        public void Show(IReadOnlyList<StoreItem> items, int petals, bool storeAvailable, WardrobeService? wardrobe = null)
        {
            _items = items;
            _wardrobe = wardrobe;
            bool cosmetics = false;
            foreach (StoreItem item in items)
            {
                cosmetics |= item.Tab == StoreTab.Cosmetics;
            }

            if (!cosmetics)
            {
                _tab = StoreTab.Shop;
            }

            // Active before it is laid out, so the prices' widths are measured at once. A purchase shows the Store again
            // on the same tab and page.
            _root.SetActive(true);
            Layout(cosmetics, storeAvailable);
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
            _tabsBox.gameObject.SetActive(false);
            _status.gameObject.SetActive(false);
            _list.gameObject.SetActive(false);
            _notice.gameObject.SetActive(true);
            UiKit.PlaceScreen((RectTransform)_notice.transform, r.Notice);
            _notice.Show(NavPlace.Shop, level);
            _nav?.Show(NavPlace.Shop, _navLook?.Invoke() ?? HomeLook.All);
            _header.Petals?.Show(petals, storeUnlocked: false);
        }

        public void Hide() => _root.SetActive(false);

        private static float Scale => DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);

        /// <summary>Places the page on the kit's regions for the screen's shape (contracts/look.md §6.6).</summary>
        private void Layout(bool cosmetics, bool storeAvailable)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            ReferenceStoreRegions r = ScreenLayout.ReferenceStore(w, h, insets, cosmetics, !storeAvailable);
            _regions = r;
            _header.Place(r.Header);

            // The panel runs to the bottom of the screen: its bottom corners go past the edge.
            float radius = r.PanelRadius(DesignTokens.ScaleFor(w, h));
            UiKit.PlaceScreen(_panel.rectTransform, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            _tabsBox.gameObject.SetActive(cosmetics);
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
            Refresh();
        }

        private void Refresh()
        {
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                Destroy(_list.GetChild(i).gameObject);
            }

            _tabs.Select((int)_tab);
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

            // The item's tile: the booster tile's cream squircle in its cream-white bezel (§3.7), 0.8 of the row tall (0.12 W
            // on the smallest row, as the reference's booster boxes), its icon 74% of it as on a booster box.
            float size = line.Height * 0.8f;
            Box tile = Box.FromCenter(line.Left + (line.Height * 0.14f) + (size / 2f), line.CenterY - (line.Height * 0.02f), size, size);
            var set = new ColorSet("set.cream.booster_tile", C.CreamFace, GardenLook.BoosterRim.Lighten(0.62f), GardenLook.BoosterLip, GardenLook.BoosterLine);
            GardenButton face = UiKit.IconFace("Tile", row.transform, set, b => Mathf.Min(b.Width, b.Height) * 0.26f, square: true);
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
            float pillH = line.Height * 0.56f;
            string priceText = unavailable ? string.Empty : petals ? NumberText.Group(item.PetalPrice!.Value) : item.PriceText;
            float pillLeft = line.Right - (line.Height * 0.14f) - ((pillH * 0.56f * 0.6f * Mathf.Max(1, priceText.Length)) + (pillH * (petals ? 1.9f : 1f)));
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

            // The price at the row's right end, as wide as its text: the lotus and the Petals, or the store's price.
            CostPillView pill = UiKit.CostPill("Price", row.transform, petals ? Cost.Petals(item.PetalPrice!.Value) : Cost.Charges(0));
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
                float h = b.Height * 0.56f;
                float measured = KitText.Measure(amount, h * 0.56f);
                float text = measured > 0f ? measured : h * 0.56f * 0.6f * Mathf.Max(1, amount.text.Length);
                float width = text + (h * (petals ? 1.9f : 1f));
                float right = b.Right - (b.Height * 0.14f);
                return new Box(right - width, b.CenterY - (h / 2f), right, b.CenterY + (h / 2f));
            });

            if (item.Enabled)
            {
                UiKit.TapTarget(row, item.Buy);
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
                    OutfitCard(box, item.Name ?? item.Title, false, family, item.Cosmetic, price, item.Enabled ? item.Buy : (Action?)null);
                }
            }

            Footer(Loc.T("wardrobe.footer"), T.Body, _outfitPage > 0 ? () => TurnOutfits(-1) : (Action?)null, _outfitPage < pages - 1 ? () => TurnOutfits(1) : (Action?)null);
        }

        private void TurnOutfits(int by)
        {
            _outfitPage += by;
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
