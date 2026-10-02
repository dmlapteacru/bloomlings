using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
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
    /// In the reference look of spec 005 (contracts/look.md §4.3, §4.6; the playtest's <c>MetaCards.Store</c>): a parchment
    /// card under its wooden banner with ivy, the cream Petals pill, the green and parchment tabs, cream rows with the
    /// booster's tile and count badge, the name and a cost pill with the lotus (a tap on the row buys), and the cosmetics in
    /// the reference Wardrobe's look: the four family tabs with their heroes over a lighter panel, outfit cards six to a
    /// page (the "Default" look first, worn while the family wears nothing; each item shown on the family's hero with its
    /// cost pill, a tap buys) and the footer line between cream page arrows.
    /// </para>
    /// </summary>
    public sealed class StoreScreen : MonoBehaviour
    {
        private const int RowsPerPage = 7;
        private const int OutfitColumns = 3;
        private const int OutfitsPerPage = 6;
        private const float RowUnits = 118f;
        private const float RowGapUnits = 16f;
        private const float FooterUnits = 110f;

        private RectTransform _host = null!;
        private CardView? _card;
        private TabsView? _tabs;
        private PetalsPill _balance = null!;
        private RectTransform _list = null!;
        private Box _column;
        private float _contentUnits = -1f;
        private IReadOnlyList<StoreItem> _items = Array.Empty<StoreItem>();
        private WardrobeService? _wardrobe;
        private StoreTab _tab = StoreTab.Shop;
        private int _pageIndex;
        private int _family;
        private int _outfitPage;

        public bool IsOpen => _host.gameObject.activeSelf;

        public static StoreScreen Create(Transform parent)
        {
            // The card is built when the Store opens (its height follows the tabs, the offline notice and the pages).
            RectTransform host = UiFactory.Stretch(UiFactory.CreateRect("Store", parent));
            var screen = host.gameObject.AddComponent<StoreScreen>();
            screen._host = host;
            host.gameObject.SetActive(false);
            return screen;
        }

        /// <param name="storeAvailable">False offline or without a store: real-money rows show as unavailable.</param>
        /// <param name="wardrobe">Shows the cosmetics as outfit cards on the families' heroes; without it they are rows.</param>
        public void Show(IReadOnlyList<StoreItem> items, int petals, bool storeAvailable, WardrobeService? wardrobe = null)
        {
            _items = items;
            _wardrobe = wardrobe;
            bool cosmetics = false;
            int shop = 0;
            foreach (StoreItem item in items)
            {
                cosmetics |= item.Tab == StoreTab.Cosmetics;
                shop += item.Tab == StoreTab.Shop ? 1 : 0;
            }

            if (!cosmetics)
            {
                _tab = StoreTab.Shop;
            }

            // Active before it is built, so the prices' widths are measured at once.
            _host.gameObject.SetActive(true);

            // The same card stays while its height does (a purchase re-shows the Store without a new pop).
            float content = 130f + (cosmetics ? 116f : 0f) + (storeAvailable ? 0f : 50f) + (RowsPerPage * (RowUnits + RowGapUnits)) + (shop > RowsPerPage ? FooterUnits : 0f);
            if (_card == null || content != _contentUnits)
            {
                Build(content, cosmetics, storeAvailable);
            }

            _balance.Show(petals, storeUnlocked: false);
            Refresh();
        }

        public void Hide() => _host.gameObject.SetActive(false);

        private static float Scale => DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);

        private void Build(float content, bool cosmetics, bool storeAvailable)
        {
            if (_card != null)
            {
                Destroy(_card.Root);
            }

            _contentUnits = content;
            CardView card = UiKit.Card("Card", _host, Loc.T("store.title"), content, Hide, sign: SignDecor.Ivy);
            _card = card;
            Box body = card.Regions.Body;
            float u = Scale;
            _balance = UiKit.PetalsPill("Petals", card.Body, null);
            UiKit.PlaceBox((RectTransform)_balance.transform, Box.FromCenter(body.CenterX, body.Top + (40f * u), 360f * u, 84f * u), body);
            float y = body.Top + (130f * u);
            _tabs = null;
            if (cosmetics)
            {
                // The selected tab is a glossy green button on a plate, the other a parchment well (spec 005 §3.5).
                RectTransform tabs = UiKit.PlaceBox(UiFactory.CreateRect("Tabs", card.Body), new Box(body.Left + (40f * u), y, body.Right - (40f * u), y + (84f * u)), body);
                string[] labels = { Loc.T("store.tab_shop"), Loc.T("store.tab_cosmetics") };
                _tabs = UiKit.Tabs("TabRow", tabs, labels, i => SetTab((StoreTab)i));
                y += 116f * u;
            }

            if (!storeAvailable)
            {
                TextMeshProUGUI status = UiKit.Label("Status", card.Body, Loc.T("store.offline"), T.Caption, UiTheme.Of(C.InkBrownSoft));
                UiKit.PlaceBox(status.rectTransform, new Box(body.Left, y, body.Right, y + (40f * u)), body);
                y += 50f * u;
            }

            _column = new Box(body.Left, y, body.Right, body.Bottom);
            _list = UiKit.PlaceBox(UiFactory.CreateRect("Items", card.Body), _column, body);
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

            _tabs?.Select((int)_tab);
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

        // ---- The Shop's rows (spec 005 §4.6) ----

        private void ShopRows(List<StoreItem> shown)
        {
            float u = Scale;
            if (shown.Count == 0)
            {
                TextMeshProUGUI empty = UiKit.Label("Empty", _list, Loc.T("store.all_owned"), T.Body, UiTheme.Of(C.InkBrownSoft));
                UiKit.PlaceBox(empty.rectTransform, new Box(_column.Left, _column.Top, _column.Right, _column.Top + (RowUnits * u)), _column);
                return;
            }

            int pages = Mathf.Max(1, (shown.Count + RowsPerPage - 1) / RowsPerPage);
            _pageIndex = Mathf.Clamp(_pageIndex, 0, pages - 1);
            Box[] rows = ScreenLayout.Column(_column, RowsPerPage, RowUnits * u, RowGapUnits * u);
            for (int slot = 0; slot < RowsPerPage; slot++)
            {
                int index = (_pageIndex * RowsPerPage) + slot;
                if (index >= shown.Count)
                {
                    break;
                }

                Row(shown[index], rows[slot], u);
            }

            if (pages > 1)
            {
                float top = rows[RowsPerPage - 1].Bottom + (RowGapUnits * u);
                var footer = new Box(_column.Left, top, _column.Right, top + ((FooterUnits - 10f) * u));
                Footer(footer, Loc.F("common.page", _pageIndex + 1, pages), T.Caption, _pageIndex > 0 ? () => Turn(-1) : (Action?)null, _pageIndex < pages - 1 ? () => Turn(1) : (Action?)null, u);
            }
        }

        private void Turn(int by)
        {
            _pageIndex += by;
            Refresh();
        }

        /// <summary>
        /// A Shop row (the playtest's <c>MetaCards.ShopRows</c>): a cream row, the item's tile at its left end (a booster's
        /// colored icon with its count badge, else the lotus), the name in brown, and the price at its right end: a cost
        /// pill with the lotus for Petals, the store's price on a cream pill, or "Unavailable" while real money cannot be
        /// spent (the row then fades to half). A tap on the row buys; a price the player cannot pay fades its pill.
        /// </summary>
        private void Row(StoreItem item, Box line, float u)
        {
            Image row = UiKit.Row(item.Id, _list, highlighted: false);
            UiKit.PlaceBox(row.rectTransform, line, _column);
            bool petals = item.PetalPrice.HasValue;
            bool unavailable = !petals && !item.Enabled;
            if (unavailable)
            {
                row.gameObject.AddComponent<CanvasGroup>().alpha = 0.5f;
            }

            // The item's tile: the booster tile's cream squircle in its silver-grey rim (§3.7).
            float size = line.Height * 0.76f;
            Box tile = Box.FromCenter(line.Left + (22f * u) + (size / 2f), line.CenterY - (line.Height * 0.02f), size, size);
            Rgba rim = GardenLook.BoosterRim;
            var set = new ColorSet("set.cream.booster_tile", C.CreamFace, rim.Lighten(0.62f), rim.Lighten(0.08f), rim.Darken(0.22f));
            GardenButton face = UiKit.IconFace("Tile", row.transform, set, b => Mathf.Min(b.Width, b.Height) * 0.26f, square: true);
            UiKit.PlaceBox((RectTransform)face.transform, tile, line);
            if (item.BoosterId != null)
            {
                Image icon = UiKit.BoosterIcon("Icon", row.transform, item.BoosterId);
                UiKit.PlaceBox(icon.rectTransform, Box.FromCenter(tile.CenterX, tile.CenterY, size * 0.66f, size * 0.66f), line);
                if (item.Charges.HasValue)
                {
                    float badge = size * 0.38f;
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
            title.fontSizeMax = UiKit.Units(T.ButtonSecondary.Size * 0.86f);
            title.fontSize = title.fontSizeMax;
            float titleLeft = line.Left + (150f * u);
            UiKit.PlaceBox(title.rectTransform, new Box(titleLeft, line.Top, titleLeft + (line.Width * 0.42f), line.Bottom), line);

            if (unavailable)
            {
                TextMeshProUGUI note = UiKit.Label("Unavailable", row.transform, Loc.T("store.unavailable"), T.Caption, UiTheme.Of(C.InkBrownSoft));
                UiKit.PlaceBox(note.rectTransform, Box.FromCenter(line.Right - (120f * u), line.CenterY, 220f * u, line.Height * 0.6f), line);
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
                float right = b.Right - UiKit.Units(22f);
                return new Box(right - width, b.CenterY - (h / 2f), right, b.CenterY + (h / 2f));
            });

            if (item.Enabled)
            {
                UiKit.TapTarget(row, item.Buy);
            }
        }

        /// <summary>
        /// A footer line between two cream page arrows (the playtest's cosmetics footer): <paramref name="text"/> centered
        /// in <c>ink.brown_soft</c>, the ‹ and › buttons (100 units) at its ends, greyed where there is no page to turn to.
        /// </summary>
        private void Footer(Box line, string text, TypeStyle style, Action? previous, Action? next, float u)
        {
            float touch = DesignTokens.Size.TouchMin * u;
            float room = touch + (12f * u);
            TextMeshProUGUI label = UiKit.Label("Footer", _list, text, style, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(label.rectTransform, new Box(line.Left + room, line.Top, line.Right - room, line.Bottom), _column);
            if (previous == null && next == null)
            {
                return;
            }

            // The arrows' rects are touch-sized; their cushions are 100 units.
            Button back = UiKit.PageArrow("Previous", _list, next: false, previous ?? (() => { }));
            back.interactable = previous != null;
            UiKit.PlaceBox((RectTransform)back.transform, Box.FromCenter(line.Left + (touch / 2f), line.CenterY, touch, touch), _column);
            Button forward = UiKit.PageArrow("Next", _list, next: true, next ?? (() => { }));
            forward.interactable = next != null;
            UiKit.PlaceBox((RectTransform)forward.transform, Box.FromCenter(line.Right - (touch / 2f), line.CenterY, touch, touch), _column);
        }

        // ---- The cosmetics in the reference Wardrobe's look (spec 005 §4.6) ----

        /// <summary>
        /// The Cosmetics tab (the playtest's <c>MetaCards.Outfits</c>): the four family tabs with their heroes over the
        /// lighter panel, the outfit cards of the chosen family six to a page (3 × 2) and the footer line between the page
        /// arrows.
        /// </summary>
        private void Outfits(List<StoreItem> shown, WardrobeService wardrobe)
        {
            float u = Scale;
            IReadOnlyList<Family> families = WardrobeService.Families;
            _family = Mathf.Clamp(_family, 0, families.Count - 1);
            Family family = families[_family];
            var tabs = new Box(_column.Left, _column.Top + (6f * u), _column.Right, _column.Top + (206f * u));
            var panel = new Box(_column.Left, tabs.Bottom, _column.Right, _column.Bottom);
            Box content = FamilyTabs(tabs, panel, families, wardrobe, u);

            var cards = new List<StoreItem?> { null };
            cards.AddRange(shown);
            int pages = Mathf.Max(1, (cards.Count + OutfitsPerPage - 1) / OutfitsPerPage);
            _outfitPage = Mathf.Clamp(_outfitPage, 0, pages - 1);
            float footer = DesignTokens.Size.TouchMin * u;
            float gap = 22f * u;
            var grid = new Box(content.Left, content.Top, content.Right, content.Bottom - footer - (8f * u));
            float cardWidth = (grid.Width - (gap * (OutfitColumns - 1))) / OutfitColumns;
            float cardHeight = (grid.Height - gap) / 2f;
            Outfit worn = wardrobe.OutfitOf(family);
            for (int slot = 0; slot < OutfitsPerPage; slot++)
            {
                int index = (_outfitPage * OutfitsPerPage) + slot;
                if (index >= cards.Count)
                {
                    break;
                }

                float x = grid.Left + ((slot % OutfitColumns) * (cardWidth + gap));
                float y = grid.Top + ((slot / OutfitColumns) * (cardHeight + gap));
                var box = new Box(x, y, x + cardWidth, y + cardHeight);
                StoreItem? item = cards[index];
                if (item == null)
                {
                    OutfitCard(box, Loc.T("wardrobe.default"), worn.IsEmpty, family, null, null, null, u);
                }
                else
                {
                    Cost? price = item.PetalPrice.HasValue ? Cost.Petals(item.PetalPrice.Value) : (Cost?)null;
                    OutfitCard(box, item.Name ?? item.Title, false, family, item.Cosmetic, price, item.Enabled ? item.Buy : (Action?)null, u);
                }
            }

            var line = new Box(content.Left, content.Bottom - footer, content.Right, content.Bottom);
            Footer(line, Loc.T("wardrobe.footer"), T.Body, _outfitPage > 0 ? () => TurnOutfits(-1) : (Action?)null, _outfitPage < pages - 1 ? () => TurnOutfits(1) : (Action?)null, u);
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
        /// tab over it, flowing into it; each tab shows the family's 3D hero in its outfit and its name. Returns the panel's
        /// content box.
        /// </summary>
        private Box FamilyTabs(Box tabs, Box panel, IReadOnlyList<Family> families, WardrobeService wardrobe, float u)
        {
            Box[] cells = ScreenLayout.Row(tabs, families.Count, 10f * u, float.MaxValue, square: false);
            float sunk = tabs.Height * 0.07f;
            FamilyTabView? selected = null;
            for (int i = 0; i < cells.Length; i++)
            {
                int index = i;
                bool on = i == _family;
                Family family = families[i];
                FamilyTabView tab = UiKit.FamilyTab(family.ToString(), _list, Loc.T("family." + WardrobeService.FamilyKey(family)), () => SelectFamily(index));
                Box cell = cells[i];
                UiKit.PlaceBox((RectTransform)tab.transform, new Box(cell.Left, cell.Top + (on ? 0f : sunk), cell.Right, panel.Top), _column);
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

            // The panel: lighter than the card's parchment, with a thin tan outline; the selected tab is drawn over it.
            float radius = UiKit.Units(26f);
            Image face = UiKit.RoundGradient("Panel", _list, C.ParchmentTop, C.CreamTop, _ => radius);
            UiKit.PlaceBox(face.rectTransform, panel, _column);
            Image outline = UiKit.RoundRing("PanelLine", _list, UiTheme.Of(C.CreamLine), _ => radius, _ => Mathf.Max(UiKit.Units(1f), UiKit.Units(DesignTokens.Garden.OutlineWidth) * 0.8f));
            UiKit.PlaceBox(outline.rectTransform, panel, _column);
            selected?.transform.SetAsLastSibling();
            return panel.Inset(22f * u);
        }

        /// <summary>
        /// An outfit card (the playtest's <c>Kit.OutfitCard</c>, <c>ui.card.outfit</c>): a raised cream card with a beige
        /// picture well holding the family's hero wearing the item (or the item's own mark for a profile item) and the name
        /// below it; the worn one has a green-tinted well, a green border and the check badge. A <paramref name="cost"/>
        /// adds the cost pill on the card's bottom edge (the box keeps that room for every card, so a row lines up); the
        /// whole box is the touch target when <paramref name="buy"/> is set.
        /// </summary>
        private void OutfitCard(Box box, string name, bool worn, Family family, CosmeticItem? item, Cost? cost, Action? buy, float u)
        {
            const float pillShare = 0.2f;
            (RectTransform root, BoxLayout layout) = Element("Outfit", _list, buy != null);
            UiKit.PlaceBox(root, box, _column);
            ColorSet green = GardenLook.Green;
            float r = 0f;
            float line = 0f;
            float wellRadius = 0f;
            Box Card(Box b) => new Box(b.Left, b.Top, b.Right, b.Top + (b.Height / (1f + (0.6f * pillShare))));
            Box Face(Box b)
            {
                Box card = Card(b);
                return new Box(card.Left, card.Top, card.Right, card.Bottom - (card.Width * 0.035f));
            }

            Box Well(Box b)
            {
                Box f = Face(b);
                float pad = f.Width * 0.075f;
                return new Box(f.Left + pad, f.Top + pad, f.Right - pad, f.Top + pad + (f.Height * 0.64f));
            }

            UiKit.SoftShadow(layout, Card, b => b.Width * 0.11f, 0.18f, 0.035f);
            Image lip = UiKit.RoundRect("Lip", root, UiTheme.Of(C.CreamLip), _ => r);
            Image face = UiKit.RoundGradient("Face", root, C.CreamTop, C.CreamFace, _ => r);
            Image cardLine = UiKit.RoundRing("Line", root, UiTheme.Of(C.CreamLine), _ => r, _ => line);
            Image well = worn
                ? UiKit.RoundGradient("Well", root, green.Top.Mix(C.CreamTop, 0.55f), green.Face.Mix(C.CreamTop, 0.5f), _ => wellRadius)
                : UiKit.RoundGradient("Well", root, C.ParchmentWell.Mix(C.CreamTop, 0.35f), C.ParchmentWell, _ => wellRadius);
            well.gameObject.AddComponent<Mask>();
            Image shade = UiKit.RoundRect("Shade", well.transform, Color.white, _ => wellRadius);
            UiKit.Gradient(shade, UiTheme.Of(C.GardenShadow.WithAlpha(0.1f)), UiTheme.Of(C.GardenShadow.WithAlpha(0f)));
            shade.GetComponent<VerticalGradient>().Stop = 0.2f;
            UiFactory.Stretch(shade.rectTransform);
            RectTransform picture = UiFactory.Stretch(UiFactory.CreateRect("Picture", well.transform));
            Preview(picture, family, item);
            float border = 0f;
            Image wellLine = UiKit.RoundRing("WellLine", root, UiTheme.Of(worn ? green.Face : C.ParchmentEdge.Darken(0.08f)), _ => wellRadius, _ => border);
            TextMeshProUGUI label = UiKit.KitLabel("Name", root, name, T.ButtonSecondary, TextLook.Plain(C.InkBrown));

            layout.Add(lip.rectTransform, b =>
            {
                Box card = Card(b);
                r = card.Width * 0.11f;
                line = Mathf.Max(UiKit.Units(1f), card.Width * 0.011f);
                wellRadius = r * 0.7f;
                border = worn ? Mathf.Max(UiKit.Units(4f), card.Width * 0.022f) : line;
                return card;
            });
            layout.Add(face.rectTransform, Face);
            layout.Add(cardLine.rectTransform, Card);
            layout.Add(well.rectTransform, Well);
            layout.Add(wellLine.rectTransform, Well);
            if (worn)
            {
                CheckBadge(root, layout, b =>
                {
                    Box w = Well(b);
                    float badge = Card(b).Width * 0.22f;
                    return Box.FromCenter(w.Right - (badge * 0.42f), w.Bottom - (badge * 0.42f), badge, badge);
                });
            }

            layout.Watch(label).Then(b =>
            {
                Box card = Card(b);
                Box f = Face(b);
                float top = Well(b).Bottom;
                float bottom = f.Bottom - ((b.Height - card.Height) * 0.5f);
                float size = Mathf.Min(UiKit.Units(T.ButtonSecondary.Size), (bottom - top) * 0.62f);
                KitText.Place(label, T.ButtonSecondary, card.CenterX, (top + bottom) / 2f, size, card.Width * 0.88f);
                foreach (Image image in new[] { lip, face, cardLine, well, shade, wellLine })
                {
                    image.GetComponent<RoundShape>().Apply();
                }
            });

            if (cost.HasValue)
            {
                CostPillView pill = UiKit.CostPill("Cost", root, cost.Value);
                if (buy == null)
                {
                    pill.gameObject.AddComponent<CanvasGroup>().alpha = 0.45f;
                }

                layout.Add((RectTransform)pill.transform, b =>
                {
                    Box card = Card(b);
                    float h = card.Height * pillShare;
                    return Box.FromCenter(card.CenterX, card.Bottom + (h * 0.1f), card.Width * 0.78f, h);
                });
            }

            if (buy != null)
            {
                UiKit.TapTarget(root.GetComponent<Image>(), buy, press: true);
            }
        }

        /// <summary>An element root with its layout (a clear raycast image when it takes taps).</summary>
        private static (RectTransform Root, BoxLayout Layout) Element(string name, Transform parent, bool raycast)
        {
            RectTransform root = UiFactory.CreateImage(name, parent, null, Color.clear, raycast).rectTransform;
            return (root, BoxLayout.On(root));
        }

        /// <summary>The worn item's badge (the playtest's <c>Kit.CheckBadge</c>): a green disc in a white ring with a white check, over a soft shadow.</summary>
        private static void CheckBadge(RectTransform root, BoxLayout layout, Func<Box, Box> disc)
        {
            ColorSet green = GardenLook.Green;
            Box Outer(Box b)
            {
                Box d = disc(b);
                return d.Inset(-d.Width * 0.1f);
            }

            UiKit.SoftShadow(layout, Outer, b => Outer(b).Width / 2f, 0.25f, 0.06f);
            Image ring = UiKit.RoundRect("CheckRing", root, Color.white);
            Image face = UiKit.RoundGradient("CheckDisc", root, green.Top, green.Face);
            Image edge = UiKit.RoundRing("CheckLine", root, UiTheme.Of(green.Line), null, b => Mathf.Max(UiKit.Units(1f), b.Width * 0.04f));
            Image check = UiFactory.CreateImage("Check", root, ProceduralSprites.Shape("ui.check"), Color.white);
            check.preserveAspect = true;
            layout.Add(ring.rectTransform, Outer);
            layout.Add(face.rectTransform, disc);
            layout.Add(edge.rectTransform, disc);
            layout.Add(check.rectTransform, b =>
            {
                Box d = disc(b);
                return Box.FromCenter(d.CenterX, d.CenterY, d.Width * 0.58f, d.Width * 0.58f);
            });
        }

        /// <summary>
        /// An outfit card's picture (the playtest's <c>MetaCards.Preview</c>): the family's hero wearing the item (skins,
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
                bool hat = item != null && item.Kind == CosmeticKind.Hat;
                BloomlingFigure hero = BloomlingFigure.Create("Hero", picture);
                hero.ShowHero(family, outfit);
                hero.Body.raycastTarget = false;
                BoxLayout.On(picture).Add(hero.Rect, well =>
                {
                    // The hero fills the well; with a hat it is a little smaller and lower, so the hat stays inside.
                    float height = well.Height * (hat ? 0.96f : 1.1f);
                    return HomeStage.Figure(well.CenterX, well.Bottom - (well.Height * (hat ? 0.02f : 0.05f)), height);
                });
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
