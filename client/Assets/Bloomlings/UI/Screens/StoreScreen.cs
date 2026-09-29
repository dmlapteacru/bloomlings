using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>One row of the Store.</summary>
    public sealed record StoreItem(string Id, string Title, string PriceText, bool Enabled, Action Buy);

    /// <summary>
    /// The Store (FR-051, T133): open from L12, it sells Petal packs, boosters for Petals, Remove Ads and the starter pack
    /// (offered once). Real-money items need the platform store: offline they show as unavailable (FR-074), while
    /// boosters for Petals always work. Nothing in a level ever requires it (FR-056).
    /// </summary>
    public sealed class StoreScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private RectTransform _list = null!;
        private TextMeshProUGUI _status = null!;
        private TextMeshProUGUI _petals = null!;

        public bool IsOpen => _root.activeSelf;

        public static StoreScreen Create(Transform parent)
        {
            RectTransform card = UiFactory.CreateModal("Store", parent, 0.8f, out GameObject root);
            var screen = root.AddComponent<StoreScreen>();
            screen._root = root;
            TextMeshProUGUI title = UiFactory.CreateText("Title", card, Loc.T("store.title"), 72f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.9f, 1f, 0.98f);
            screen._petals = UiFactory.CreateText("Petals", card, string.Empty, 44f, UiTheme.Text);
            UiFactory.Place(screen._petals.rectTransform, 0f, 0.85f, 1f, 0.9f);
            screen._status = UiFactory.CreateText("Status", card, string.Empty, 36f, UiTheme.Warning);
            UiFactory.Place(screen._status.rectTransform, 0f, 0.8f, 1f, 0.85f);
            screen._list = UiFactory.Place(UiFactory.CreateRect("Items", card), 0.05f, 0.12f, 0.95f, 0.8f);
            Button close = UiFactory.CreateButton("Close", card, Loc.T("common.close"), UiTheme.Text, screen.Hide);
            UiFactory.Place((RectTransform)close.transform, 0.3f, 0.02f, 0.7f, 0.1f);
            root.SetActive(false);
            return screen;
        }

        /// <param name="storeAvailable">False offline or without a store: real-money rows show as unavailable.</param>
        public void Show(IReadOnlyList<StoreItem> items, int petals, bool storeAvailable)
        {
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                Destroy(_list.GetChild(i).gameObject);
            }

            _petals.text = Loc.F("common.petals", petals);
            _status.text = storeAvailable ? string.Empty : Loc.T("store.offline");
            float row = 1f / Mathf.Max(items.Count, 1);
            for (int i = 0; i < items.Count; i++)
            {
                StoreItem item = items[i];
                float top = 1f - (i * row);
                Image background = UiFactory.CreateImage(item.Id, _list, ProceduralSprites.RoundedSquare, UiTheme.Panel);
                UiFactory.Place(background.rectTransform, 0f, top - row + 0.01f, 1f, top - 0.01f);
                TextMeshProUGUI label = UiFactory.CreateText("Title", background.transform, item.Title, 40f, UiTheme.Text, TextAlignmentOptions.Left);
                UiFactory.Place(label.rectTransform, 0.04f, 0f, 0.6f, 1f);
                Button buy = UiFactory.CreateButton("Buy", background.transform, item.PriceText, UiTheme.Accent, item.Buy, 36f);
                UiFactory.Place((RectTransform)buy.transform, 0.62f, 0.1f, 0.98f, 0.9f);
                buy.interactable = item.Enabled;
            }

            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);
    }
}
