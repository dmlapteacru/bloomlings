using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>The Store's tabs: Petal packs, boosters and Remove Ads; then the cosmetics for Petals.</summary>
    public enum StoreTab
    {
        Shop,
        Cosmetics,
    }

    /// <summary>One row of the Store; a booster or cosmetic row has its icon, and a Petal price shows with the Petal symbol.</summary>
    public sealed record StoreItem(string Id, string Title, string PriceText, bool Enabled, Action Buy, StoreTab Tab = StoreTab.Shop, Sprite? Icon = null, Color? IconTint = null, int? PetalPrice = null);

    /// <summary>
    /// The Store of the design board's frame 17 (spec 002 FR-025; FR-051, T133): the Petals balance pill, then one
    /// row per item with its icon, its name and its price (the Petal symbol for Petal prices). Open from L12, it sells Petal packs, boosters for Petals, Remove Ads and the starter pack
    /// (offered once). Cosmetics for Petals join on their own tab after the Wardrobe unlock (L40). Real-money items need
    /// the platform store: offline they show as unavailable (FR-074), while everything for Petals always works. Nothing in
    /// a level ever requires it (FR-056).
    /// </summary>
    public sealed class StoreScreen : MonoBehaviour
    {
        private const int RowsPerPage = 7;

        private readonly List<Image> _tabs = new List<Image>();
        private GameObject _root = null!;
        private GameObject _tabRow = null!;
        private RectTransform _list = null!;
        private TextMeshProUGUI _status = null!;
        private TextMeshProUGUI _petals = null!;
        private TextMeshProUGUI _page = null!;
        private Button _previous = null!;
        private Button _next = null!;
        private IReadOnlyList<StoreItem> _items = Array.Empty<StoreItem>();
        private StoreTab _tab = StoreTab.Shop;
        private int _pageIndex;

        public bool IsOpen => _root.activeSelf;

        private PetalsPill _balance = null!;

        public static StoreScreen Create(Transform parent)
        {
            CardView card = UiKit.Card("Store", parent, Loc.T("store.title"), 1350f, null);
            var screen = card.Root.AddComponent<StoreScreen>();
            screen._root = card.Root;
            Button close = UiKit.RoundIconButton("Close", card.CardRect, "ui.close", screen.Hide);
            UiKit.PlaceBox((RectTransform)close.transform, card.Regions.Close, card.Regions.Card);
            RectTransform body = card.Body;
            screen._balance = UiKit.PetalsPill("Petals", body, null);
            UiFactory.Place((RectTransform)screen._balance.transform, 0.3f, 0.93f, 0.7f, 0.995f);
            screen._petals = screen._balance.Balance;
            RectTransform tabs = UiFactory.Place(UiFactory.CreateRect("Tabs", body), 0.04f, 0.845f, 0.96f, 0.905f);
            screen._tabRow = tabs.gameObject;
            string[] labels = { Loc.T("store.tab_shop"), Loc.T("store.tab_cosmetics") };
            for (int i = 0; i < labels.Length; i++)
            {
                StoreTab tab = (StoreTab)i;
                Image pill = UiKit.Pill(tab.ToString(), tabs, UiTheme.Sunk, raycast: true);
                UiFactory.Place(pill.rectTransform, (i * 0.5f) + 0.01f, 0f, (i * 0.5f) + 0.49f, 1f);
                var button = pill.gameObject.AddComponent<Button>();
                button.targetGraphic = pill;
                button.onClick.AddListener(() => screen.SetTab(tab));
                TextMeshProUGUI text = UiKit.Label("Label", pill.transform, labels[i], Design.DesignTokens.Type.ButtonSecondary, UiTheme.Text);
                UiFactory.Place(text.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f);
                screen._tabs.Add(pill);
            }

            screen._status = UiKit.Label("Status", body, string.Empty, Design.DesignTokens.Type.Caption, UiTheme.TextSecondary);
            UiFactory.Place(screen._status.rectTransform, 0f, 0.8f, 1f, 0.84f);
            screen._list = UiFactory.Place(UiFactory.CreateRect("Items", body), 0f, 0.08f, 1f, 0.8f);
            screen._previous = UiKit.SecondaryButton("Previous", body, "‹", () => screen.Turn(-1));
            UiFactory.Place((RectTransform)screen._previous.transform, 0.1f, 0f, 0.3f, 0.07f);
            screen._page = UiKit.Label("Page", body, string.Empty, Design.DesignTokens.Type.Caption, UiTheme.TextSecondary);
            UiFactory.Place(screen._page.rectTransform, 0.3f, 0f, 0.7f, 0.07f);
            screen._next = UiKit.SecondaryButton("Next", body, "›", () => screen.Turn(1));
            UiFactory.Place((RectTransform)screen._next.transform, 0.7f, 0f, 0.9f, 0.07f);
            card.Root.SetActive(false);
            return screen;
        }

        /// <param name="storeAvailable">False offline or without a store: real-money rows show as unavailable.</param>
        public void Show(IReadOnlyList<StoreItem> items, int petals, bool storeAvailable)
        {
            _items = items;
            _balance.Show(petals, storeUnlocked: false);
            _status.text = storeAvailable ? string.Empty : Loc.T("store.offline");
            bool cosmetics = false;
            foreach (StoreItem item in items)
            {
                cosmetics |= item.Tab == StoreTab.Cosmetics;
            }

            _tabRow.SetActive(cosmetics);
            if (!cosmetics)
            {
                _tab = StoreTab.Shop;
            }

            Refresh();
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private void SetTab(StoreTab tab)
        {
            _tab = tab;
            _pageIndex = 0;
            Refresh();
        }

        private void Turn(int by)
        {
            _pageIndex += by;
            Refresh();
        }

        private void Refresh()
        {
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                Destroy(_list.GetChild(i).gameObject);
            }

            for (int i = 0; i < _tabs.Count; i++)
            {
                bool selected = (StoreTab)i == _tab;
                _tabs[i].color = selected ? UiTheme.Accent : UiTheme.Sunk;
                _tabs[i].GetComponentInChildren<TextMeshProUGUI>().color = selected ? UiTheme.TextOnColor : UiTheme.Text;
            }

            var shown = new List<StoreItem>();
            foreach (StoreItem item in _items)
            {
                if (item.Tab == _tab)
                {
                    shown.Add(item);
                }
            }

            int pages = Mathf.Max(1, (shown.Count + RowsPerPage - 1) / RowsPerPage);
            _pageIndex = Mathf.Clamp(_pageIndex, 0, pages - 1);
            bool paged = pages > 1;
            _page.gameObject.SetActive(paged);
            _previous.gameObject.SetActive(paged);
            _next.gameObject.SetActive(paged);
            _page.text = Loc.F("common.page", _pageIndex + 1, pages);
            _previous.interactable = _pageIndex > 0;
            _next.interactable = _pageIndex < pages - 1;
            if (shown.Count == 0)
            {
                TextMeshProUGUI empty = UiFactory.CreateText("Empty", _list, Loc.T("store.all_owned"), 40f, UiTheme.Text);
                UiFactory.Stretch(empty.rectTransform);
                return;
            }

            float row = 1f / RowsPerPage;
            for (int slot = 0; slot < RowsPerPage; slot++)
            {
                int index = (_pageIndex * RowsPerPage) + slot;
                if (index >= shown.Count)
                {
                    break;
                }

                StoreItem item = shown[index];
                float top = 1f - (slot * row);
                Image background = UiKit.Rounded(item.Id, _list, Color.white, 30f);
                UiFactory.Place(background.rectTransform, 0f, top - row + 0.012f, 1f, top - 0.012f);
                float textStart = 0.05f;
                if (item.Icon != null)
                {
                    Image icon = UiFactory.CreateImage("Icon", background.transform, item.Icon, item.IconTint ?? Color.white);
                    icon.preserveAspect = true;
                    UiFactory.Place(icon.rectTransform, 0.03f, 0.14f, 0.15f, 0.86f);
                    textStart = 0.18f;
                }

                TextMeshProUGUI label = UiKit.Label("Title", background.transform, item.Title, Design.DesignTokens.Type.Body, UiTheme.Text, TextAlignmentOptions.Left);
                UiFactory.Place(label.rectTransform, textStart, 0f, 0.64f, 1f);

                // The price: a sunk pill with the Petal symbol for Petal prices, or the store's price text.
                Image price = UiKit.Pill("Price", background.transform, UiTheme.Sunk, raycast: true);
                UiFactory.Place(price.rectTransform, 0.66f, 0.14f, 0.97f, 0.86f);
                Button buy = price.gameObject.AddComponent<Button>();
                buy.targetGraphic = price;
                buy.onClick.AddListener(() => item.Buy());
                price.gameObject.AddComponent<PressMotion>();
                string priceText = item.PetalPrice.HasValue ? Design.NumberText.Group(item.PetalPrice.Value) : item.PriceText;
                TextMeshProUGUI amount = UiKit.Label("Amount", price.transform, priceText, Design.DesignTokens.Type.Count, UiTheme.Text);
                amount.outlineWidth = 0f;
                UiFactory.Place(amount.rectTransform, 0.08f, 0.06f, item.PetalPrice.HasValue ? 0.66f : 0.92f, 0.94f);
                if (item.PetalPrice.HasValue)
                {
                    Image petal = UiKit.PetalIcon("Petal", price.transform);
                    UiFactory.Place(petal.rectTransform, 0.68f, 0.12f, 0.92f, 0.88f);
                }

                buy.interactable = item.Enabled;
            }
        }
    }
}
