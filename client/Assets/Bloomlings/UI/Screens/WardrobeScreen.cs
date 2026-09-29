using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The Wardrobe (FR-063, T143): open from L40. On the Bloomlings tab, pick a family and a kind (skin, hat, trail,
    /// expression), then one owned item of that kind (or none): each family wears one of each kind. On the Profile tab,
    /// pick the frame, badge and marker shown on Home and on the leaderboard. Cosmetics only change how Bloomlings look;
    /// the variant colors and icons stay as they are.
    /// </summary>
    public sealed class WardrobeScreen : MonoBehaviour
    {
        private const int Columns = 3;
        private const int Rows = 3;
        private const int PerPage = Columns * Rows;

        private readonly List<Image> _familyFrames = new List<Image>();
        private readonly List<BloomlingFigure> _familyFigures = new List<BloomlingFigure>();
        private readonly List<Image> _modeTabs = new List<Image>();
        private readonly List<Button> _kindTabs = new List<Button>();
        private GameObject _root = null!;
        private GameObject _familyRow = null!;
        private ProfileAvatar _avatar = null!;
        private RectTransform _items = null!;
        private TextMeshProUGUI _page = null!;
        private Button _previous = null!;
        private Button _next = null!;
        private WardrobeService _wardrobe = null!;
        private Family _selected = Family.Sprig;
        private bool _profileMode;
        private CosmeticKind _kind = CosmeticKind.Skin;
        private int _pageIndex;

        public bool IsOpen => _root.activeSelf;

        public static WardrobeScreen Create(Transform parent, WardrobeService wardrobe)
        {
            RectTransform card = UiFactory.CreateModal("Wardrobe", parent, 0.84f, out GameObject root);
            var screen = root.AddComponent<WardrobeScreen>();
            screen._root = root;
            screen._wardrobe = wardrobe;
            TextMeshProUGUI title = UiFactory.CreateText("Title", card, Loc.T("wardrobe.title"), 72f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.91f, 1f, 0.98f);

            string[] modes = { Loc.T("wardrobe.tab_bloomlings"), Loc.T("wardrobe.tab_profile") };
            for (int i = 0; i < modes.Length; i++)
            {
                bool profile = i == 1;
                Button tab = UiFactory.CreateButton(profile ? "ProfileTab" : "BloomlingsTab", card, modes[i], UiTheme.SlotLocked, () => screen.SetMode(profile), 40f);
                UiFactory.Place((RectTransform)tab.transform, 0.06f + (i * 0.45f), 0.845f, 0.49f + (i * 0.45f), 0.9f);
                screen._modeTabs.Add(tab.GetComponent<Image>());
            }

            RectTransform familyRow = UiFactory.Place(UiFactory.CreateRect("Families", card), 0.04f, 0.66f, 0.96f, 0.83f);
            screen._familyRow = familyRow.gameObject;
            IReadOnlyList<Family> families = WardrobeService.Families;
            for (int i = 0; i < families.Count; i++)
            {
                Family family = families[i];
                float x0 = i * 0.25f;
                Button button = UiFactory.CreateButton(family.ToString(), familyRow, string.Empty, UiTheme.Panel, () => screen.Select(family));
                UiFactory.Place((RectTransform)button.transform, x0 + 0.01f, 0f, x0 + 0.24f, 1f);
                screen._familyFrames.Add(button.GetComponent<Image>());
                BloomlingFigure figure = BloomlingFigure.Create("Figure", button.transform);
                UiFactory.Place(figure.Rect, 0.2f, 0.1f, 0.8f, 0.7f);
                screen._familyFigures.Add(figure);
            }

            screen._avatar = ProfileAvatar.Create("Avatar", card);
            UiFactory.Place(screen._avatar.Rect, 0.38f, 0.66f, 0.62f, 0.83f);

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                Button tab = UiFactory.CreateButton("Kind" + i, card, string.Empty, UiTheme.Panel, () => screen.SetKind(index), 34f);
                UiFactory.Place((RectTransform)tab.transform, 0.04f + (i * 0.23f), 0.59f, 0.26f + (i * 0.23f), 0.645f);
                screen._kindTabs.Add(tab);
            }

            screen._items = UiFactory.Place(UiFactory.CreateRect("Items", card), 0.04f, 0.18f, 0.96f, 0.58f);
            screen._previous = UiFactory.CreateButton("Previous", card, "‹", UiTheme.SlotLocked, () => screen.Turn(-1), 56f);
            UiFactory.Place((RectTransform)screen._previous.transform, 0.2f, 0.115f, 0.34f, 0.17f);
            screen._page = UiFactory.CreateText("Page", card, string.Empty, 34f, UiTheme.Text);
            UiFactory.Place(screen._page.rectTransform, 0.34f, 0.115f, 0.66f, 0.17f);
            screen._next = UiFactory.CreateButton("Next", card, "›", UiTheme.SlotLocked, () => screen.Turn(1), 56f);
            UiFactory.Place((RectTransform)screen._next.transform, 0.66f, 0.115f, 0.8f, 0.17f);
            Button close = UiFactory.CreateButton("Close", card, Loc.T("common.close"), UiTheme.Text, screen.Hide, 44f);
            UiFactory.Place((RectTransform)close.transform, 0.3f, 0.02f, 0.7f, 0.1f);
            root.SetActive(false);
            return screen;
        }

        public void Show()
        {
            Refresh();
            _root.SetActive(true);
        }

        /// <summary>Opens on the Profile tab (the Home avatar).</summary>
        public void ShowProfile()
        {
            SetMode(true);
            _root.SetActive(true);
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
            _selected = family;
            Refresh();
        }

        private void Turn(int by)
        {
            _pageIndex += by;
            Refresh();
        }

        private void Refresh()
        {
            _modeTabs[0].color = _profileMode ? UiTheme.SlotLocked : UiTheme.Accent;
            _modeTabs[1].color = _profileMode ? UiTheme.Accent : UiTheme.SlotLocked;
            _familyRow.SetActive(!_profileMode);
            _avatar.Rect.gameObject.SetActive(_profileMode);
            _avatar.Show(_wardrobe.Profile, _wardrobe.OutfitOf(Family.Bloom));

            IReadOnlyList<Family> families = WardrobeService.Families;
            for (int i = 0; i < families.Count; i++)
            {
                _familyFrames[i].color = families[i] == _selected ? UiTheme.Light(UiTheme.Accent) : UiTheme.Panel;
                _familyFigures[i].Show(families[i], UiTheme.SlotLocked, _wardrobe.OutfitOf(families[i]));
            }

            IReadOnlyList<CosmeticKind> kinds = Kinds;
            for (int i = 0; i < _kindTabs.Count; i++)
            {
                bool used = i < kinds.Count;
                _kindTabs[i].gameObject.SetActive(used);
                if (used)
                {
                    _kindTabs[i].GetComponent<Image>().color = kinds[i] == _kind ? UiTheme.Light(UiTheme.Accent) : UiTheme.Panel;
                    TextMeshProUGUI label = _kindTabs[i].GetComponentInChildren<TextMeshProUGUI>();
                    label.text = KindLabel(kinds[i]);
                    label.color = UiTheme.Text;
                }
            }

            RefreshItems();
        }

        private void RefreshItems()
        {
            for (int i = _items.childCount - 1; i >= 0; i--)
            {
                Destroy(_items.GetChild(i).gameObject);
            }

            // Worn kinds start with "none"; a profile kind always shows one of its owned items.
            var options = new List<CosmeticItem?>();
            if (!_profileMode)
            {
                options.Add(null);
            }

            foreach (CosmeticItem item in _wardrobe.OwnedOf(_kind))
            {
                options.Add(item);
            }

            int pages = Mathf.Max(1, (options.Count + PerPage - 1) / PerPage);
            _pageIndex = Mathf.Clamp(_pageIndex, 0, pages - 1);
            _page.text = Loc.F("common.page", _pageIndex + 1, pages);
            _previous.interactable = _pageIndex > 0;
            _next.interactable = _pageIndex < pages - 1;
            CosmeticItem? current = _profileMode ? _wardrobe.Shown(_kind) : _wardrobe.EquippedFor(_selected, _kind);
            if (options.Count == 0)
            {
                TextMeshProUGUI empty = UiFactory.CreateText("Empty", _items, Loc.T("wardrobe.more"), 36f, UiTheme.Text);
                UiFactory.Stretch(empty.rectTransform);
                return;
            }

            for (int slot = 0; slot < PerPage; slot++)
            {
                int index = (_pageIndex * PerPage) + slot;
                if (index >= options.Count)
                {
                    break;
                }

                CosmeticItem? item = options[index];
                float x0 = (slot % Columns) / (float)Columns;
                float y1 = 1f - ((slot / Columns) / (float)Rows);
                bool on = item?.Id == current?.Id;
                Button button = UiFactory.CreateButton(item?.Id ?? "none", _items, string.Empty, on ? UiTheme.Light(UiTheme.Accent) : UiTheme.Panel, () => Choose(item));
                UiFactory.Place((RectTransform)button.transform, x0 + 0.01f, y1 - (1f / Rows) + 0.01f, x0 + (1f / Columns) - 0.01f, y1 - 0.01f);
                if (item != null)
                {
                    if (item.Kind == CosmeticKind.Skin)
                    {
                        BloomlingFigure swatch = BloomlingFigure.Create("Swatch", button.transform);
                        UiFactory.Place(swatch.Rect, 0.3f, 0.35f, 0.7f, 0.92f);
                        swatch.Show(_selected, UiTheme.SlotLocked, new Outfit(item, null, null, null));
                    }
                    else
                    {
                        Image icon = UiFactory.CreateImage("Icon", button.transform, Icon(item), BloomlingFigure.Tint(item));
                        icon.preserveAspect = true;
                        UiFactory.Place(icon.rectTransform, 0.3f, 0.35f, 0.7f, 0.92f);
                    }
                }

                TextMeshProUGUI label = UiFactory.CreateText("Name", button.transform, item == null ? Loc.T("wardrobe.none") : Name(item), 28f, UiTheme.Text);
                label.enableAutoSizing = true;
                label.fontSizeMin = 18f;
                label.fontSizeMax = 28f;
                UiFactory.Place(label.rectTransform, 0.04f, 0.02f, 0.96f, item == null ? 0.98f : 0.34f);
            }
        }

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
