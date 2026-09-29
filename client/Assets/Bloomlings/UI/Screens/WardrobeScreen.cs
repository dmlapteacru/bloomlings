using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The Wardrobe (FR-063, T143): open from L40. Pick a family, then one of the owned hats, trails or expressions
    /// for it (or none). Cosmetics only change how Bloomlings look; the variant colors and icons stay as they are.
    /// </summary>
    public sealed class WardrobeScreen : MonoBehaviour
    {
        private const int Columns = 3;

        private readonly List<Image> _familyFrames = new List<Image>();
        private readonly List<Image> _familyAccessories = new List<Image>();
        private GameObject _root = null!;
        private RectTransform _items = null!;
        private TextMeshProUGUI _profile = null!;
        private WardrobeService _wardrobe = null!;
        private Family _selected = Family.Sprig;

        public bool IsOpen => _root.activeSelf;

        public static WardrobeScreen Create(Transform parent, WardrobeService wardrobe)
        {
            RectTransform card = UiFactory.CreateModal("Wardrobe", parent, 0.8f, out GameObject root);
            var screen = root.AddComponent<WardrobeScreen>();
            screen._root = root;
            screen._wardrobe = wardrobe;
            TextMeshProUGUI title = UiFactory.CreateText("Title", card, Loc.T("wardrobe.title"), 72f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.9f, 1f, 0.98f);

            IReadOnlyList<Family> families = WardrobeService.Families;
            for (int i = 0; i < families.Count; i++)
            {
                Family family = families[i];
                float x0 = 0.04f + (i * 0.235f);
                Button button = UiFactory.CreateButton(family.ToString(), card, string.Empty, UiTheme.Panel, () => screen.Select(family));
                UiFactory.Place((RectTransform)button.transform, x0, 0.72f, x0 + 0.22f, 0.88f);
                screen._familyFrames.Add(button.GetComponent<Image>());
                Image body = UiFactory.CreateImage("Body", button.transform, ProceduralSprites.Silhouette(family), UiTheme.SlotLocked);
                body.preserveAspect = true;
                UiFactory.Place(body.rectTransform, 0.2f, 0.12f, 0.8f, 0.72f);
                Image accessory = UiFactory.CreateImage("Accessory", body.transform, null, Color.white);
                accessory.preserveAspect = true;
                screen._familyAccessories.Add(accessory);
            }

            screen._items = UiFactory.Place(UiFactory.CreateRect("Items", card), 0.04f, 0.2f, 0.96f, 0.7f);
            screen._profile = UiFactory.CreateText("Profile", card, string.Empty, 36f, UiTheme.Text);
            UiFactory.Place(screen._profile.rectTransform, 0f, 0.12f, 1f, 0.19f);
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

        public void Hide() => _root.SetActive(false);

        private void Select(Family family)
        {
            _selected = family;
            Refresh();
        }

        private void Refresh()
        {
            IReadOnlyList<Family> families = WardrobeService.Families;
            for (int i = 0; i < families.Count; i++)
            {
                _familyFrames[i].color = families[i] == _selected ? UiTheme.Light(UiTheme.Accent) : UiTheme.Panel;
                Show(_familyAccessories[i], _wardrobe.EquippedFor(families[i]));
            }

            for (int i = _items.childCount - 1; i >= 0; i--)
            {
                Destroy(_items.GetChild(i).gameObject);
            }

            var options = new List<CosmeticItem?> { null };
            foreach (CosmeticItem item in _wardrobe.Owned)
            {
                if (item.IsWorn)
                {
                    options.Add(item);
                }
            }

            CosmeticItem? equipped = _wardrobe.EquippedFor(_selected);
            int rows = Mathf.Max(4, (options.Count + Columns - 1) / Columns);
            for (int i = 0; i < options.Count; i++)
            {
                CosmeticItem? item = options[i];
                float x0 = (i % Columns) / (float)Columns;
                float y1 = 1f - ((i / Columns) / (float)rows);
                bool on = item?.Id == equipped?.Id;
                Button button = UiFactory.CreateButton(item?.Id ?? "none", _items, string.Empty, on ? UiTheme.Light(UiTheme.Accent) : UiTheme.Panel, () =>
                {
                    if (item == null)
                    {
                        _wardrobe.Unequip(_selected);
                    }
                    else
                    {
                        _wardrobe.Equip(_selected, item.Id);
                    }

                    Refresh();
                });
                UiFactory.Place((RectTransform)button.transform, x0 + 0.01f, y1 - (1f / rows) + 0.01f, x0 + (1f / Columns) - 0.01f, y1 - 0.01f);
                if (item != null)
                {
                    Image icon = UiFactory.CreateImage("Icon", button.transform, ProceduralSprites.Accessory(item.Shape), Tint(item));
                    icon.preserveAspect = true;
                    UiFactory.Place(icon.rectTransform, 0.3f, 0.35f, 0.7f, 0.92f);
                }

                TextMeshProUGUI label = UiFactory.CreateText("Name", button.transform, item == null ? Loc.T("wardrobe.none") : Name(item), 28f, UiTheme.Text);
                UiFactory.Place(label.rectTransform, 0.02f, 0.02f, 0.98f, item == null ? 0.98f : 0.34f);
            }

            CosmeticItem? decoration = _wardrobe.ProfileDecoration();
            _profile.text = decoration == null ? Loc.T("wardrobe.more") : Loc.F("wardrobe.profile", Name(decoration));
        }

        private static void Show(Image image, CosmeticItem? item)
        {
            if (item == null)
            {
                image.gameObject.SetActive(false);
                return;
            }

            image.sprite = ProceduralSprites.Accessory(item.Shape);
            image.color = Tint(item);
            switch (item.Kind)
            {
                case CosmeticKind.Hat:
                    UiFactory.Place(image.rectTransform, 0.2f, 0.72f, 0.8f, 1.22f);
                    break;
                case CosmeticKind.Expression:
                    UiFactory.Place(image.rectTransform, 0.33f, 0.3f, 0.67f, 0.55f);
                    break;
                default:
                    UiFactory.Place(image.rectTransform, -0.3f, -0.05f, 0.05f, 0.3f);
                    break;
            }

            image.gameObject.SetActive(true);
        }

        /// <summary>A cosmetic's display name: its localized entry, else the catalog name.</summary>
        public static string Name(CosmeticItem item) => Loc.T("cosmetic." + item.Id, item.Name);

        private static Color Tint(CosmeticItem item) => ColorUtility.TryParseHtmlString(item.Tint, out Color color) ? color : Color.white;
    }
}
