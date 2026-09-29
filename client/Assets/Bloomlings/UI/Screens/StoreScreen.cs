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

    /// <summary>One row of the Store; a cosmetic row has its icon.</summary>
    public sealed record StoreItem(string Id, string Title, string PriceText, bool Enabled, Action Buy, StoreTab Tab = StoreTab.Shop, Sprite? Icon = null, Color? IconTint = null);

    /// <summary>
    /// The Store (FR-051, T133): open from L12, it sells Petal packs, boosters for Petals, Remove Ads and the starter pack
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

        public static StoreScreen Create(Transform parent)
        {
            RectTransform card = UiFactory.CreateModal("Store", parent, 0.84f, out GameObject root);
            var screen = root.AddComponent<StoreScreen>();
            screen._root = root;
            TextMeshProUGUI title = UiFactory.CreateText("Title", card, Loc.T("store.title"), 72f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.91f, 1f, 0.98f);
            screen._petals = UiFactory.CreateText("Petals", card, string.Empty, 44f, UiTheme.Text);
            UiFactory.Place(screen._petals.rectTransform, 0f, 0.86f, 1f, 0.91f);
            RectTransform tabs = UiFactory.Place(UiFactory.CreateRect("Tabs", card), 0.06f, 0.8f, 0.94f, 0.855f);
            screen._tabRow = tabs.gameObject;
            string[] labels = { Loc.T("store.tab_shop"), Loc.T("store.tab_cosmetics") };
            for (int i = 0; i < labels.Length; i++)
            {
                StoreTab tab = (StoreTab)i;
                Button button = UiFactory.CreateButton(tab.ToString(), tabs, labels[i], UiTheme.SlotLocked, () => screen.SetTab(tab), 40f);
                UiFactory.Place((RectTransform)button.transform, (i * 0.5f) + 0.01f, 0f, (i * 0.5f) + 0.49f, 1f);
                screen._tabs.Add(button.GetComponent<Image>());
            }

            screen._status = UiFactory.CreateText("Status", card, string.Empty, 36f, UiTheme.Warning);
            UiFactory.Place(screen._status.rectTransform, 0f, 0.75f, 1f, 0.8f);
            screen._list = UiFactory.Place(UiFactory.CreateRect("Items", card), 0.05f, 0.18f, 0.95f, 0.75f);
            screen._previous = UiFactory.CreateButton("Previous", card, "‹", UiTheme.SlotLocked, () => screen.Turn(-1), 56f);
            UiFactory.Place((RectTransform)screen._previous.transform, 0.2f, 0.115f, 0.34f, 0.17f);
            screen._page = UiFactory.CreateText("Page", card, string.Empty, 34f, UiTheme.Text);
            UiFactory.Place(screen._page.rectTransform, 0.34f, 0.115f, 0.66f, 0.17f);
            screen._next = UiFactory.CreateButton("Next", card, "›", UiTheme.SlotLocked, () => screen.Turn(1), 56f);
            UiFactory.Place((RectTransform)screen._next.transform, 0.66f, 0.115f, 0.8f, 0.17f);
            Button close = UiFactory.CreateButton("Close", card, Loc.T("common.close"), UiTheme.Text, screen.Hide);
            UiFactory.Place((RectTransform)close.transform, 0.3f, 0.02f, 0.7f, 0.1f);
            root.SetActive(false);
            return screen;
        }

        /// <param name="storeAvailable">False offline or without a store: real-money rows show as unavailable.</param>
        public void Show(IReadOnlyList<StoreItem> items, int petals, bool storeAvailable)
        {
            _items = items;
            _petals.text = Loc.F("common.petals", petals);
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
                _tabs[i].color = (StoreTab)i == _tab ? UiTheme.Accent : UiTheme.SlotLocked;
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
                Image background = UiFactory.CreateImage(item.Id, _list, ProceduralSprites.RoundedSquare, UiTheme.Panel);
                UiFactory.Place(background.rectTransform, 0f, top - row + 0.01f, 1f, top - 0.01f);
                float textStart = 0.04f;
                if (item.Icon != null)
                {
                    Image icon = UiFactory.CreateImage("Icon", background.transform, item.Icon, item.IconTint ?? Color.white);
                    icon.preserveAspect = true;
                    UiFactory.Place(icon.rectTransform, 0.02f, 0.1f, 0.14f, 0.9f);
                    textStart = 0.16f;
                }

                TextMeshProUGUI label = UiFactory.CreateText("Title", background.transform, item.Title, 40f, UiTheme.Text, TextAlignmentOptions.Left);
                label.enableAutoSizing = true;
                label.fontSizeMin = 24f;
                label.fontSizeMax = 40f;
                UiFactory.Place(label.rectTransform, textStart, 0f, 0.6f, 1f);
                Button buy = UiFactory.CreateButton("Buy", background.transform, item.PriceText, UiTheme.Accent, item.Buy, 36f);
                UiFactory.Place((RectTransform)buy.transform, 0.62f, 0.1f, 0.98f, 0.9f);
                buy.interactable = item.Enabled;
            }
        }
    }
}
