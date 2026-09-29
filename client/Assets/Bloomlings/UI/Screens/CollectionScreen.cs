using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Services.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The Collection (FR-065, T145): every finished picture, newest first, a page at a time. It only shows pictures:
    /// there is no way to open or replay a level from here (it is never a level selector).
    /// </summary>
    public sealed class CollectionScreen : MonoBehaviour
    {
        private const int Columns = 3;
        private const int Rows = 4;
        private const int PageSize = Columns * Rows;

        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private GameObject _root = null!;
        private RectTransform _grid = null!;
        private TextMeshProUGUI _count = null!;
        private TextMeshProUGUI _page = null!;
        private IReadOnlyList<CollectionEntry> _entries = Array.Empty<CollectionEntry>();
        private Func<CollectionEntry, Texture2D?> _render = _ => null;
        private int _pageIndex;

        public bool IsOpen => _root.activeSelf;

        public static CollectionScreen Create(Transform parent)
        {
            RectTransform card = UiFactory.CreateModal("Collection", parent, 0.85f, out GameObject root);
            var screen = root.AddComponent<CollectionScreen>();
            screen._root = root;
            TextMeshProUGUI title = UiFactory.CreateText("Title", card, Loc.T("collection.title"), 72f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.91f, 1f, 0.98f);
            screen._count = UiFactory.CreateText("Count", card, string.Empty, 40f, UiTheme.Text);
            UiFactory.Place(screen._count.rectTransform, 0f, 0.86f, 1f, 0.91f);
            screen._grid = UiFactory.Place(UiFactory.CreateRect("Grid", card), 0.04f, 0.16f, 0.96f, 0.85f);
            Button previous = UiFactory.CreateButton("Previous", card, "‹", UiTheme.SlotLocked, () => screen.Turn(-1), 56f);
            UiFactory.Place((RectTransform)previous.transform, 0.05f, 0.08f, 0.22f, 0.15f);
            screen._page = UiFactory.CreateText("Page", card, string.Empty, 36f, UiTheme.Text);
            UiFactory.Place(screen._page.rectTransform, 0.25f, 0.08f, 0.75f, 0.15f);
            Button next = UiFactory.CreateButton("Next", card, "›", UiTheme.SlotLocked, () => screen.Turn(1), 56f);
            UiFactory.Place((RectTransform)next.transform, 0.78f, 0.08f, 0.95f, 0.15f);
            Button close = UiFactory.CreateButton("Close", card, Loc.T("common.close"), UiTheme.Text, screen.Hide, 44f);
            UiFactory.Place((RectTransform)close.transform, 0.3f, 0.01f, 0.7f, 0.07f);
            root.SetActive(false);
            return screen;
        }

        /// <param name="render">Draws an entry's finished picture, or null when this content version cannot redraw it.</param>
        public void Show(IReadOnlyList<CollectionEntry> entries, Func<CollectionEntry, Texture2D?> render)
        {
            _entries = entries;
            _render = render;
            _pageIndex = 0;
            _count.text = entries.Count == 1 ? Loc.F("collection.count_one", 1) : Loc.F("collection.count_many", entries.Count);
            BuildPage();
            _root.SetActive(true);
        }

        public void Hide()
        {
            ReleaseTextures();
            _root.SetActive(false);
        }

        private int PageCount => Mathf.Max(1, (_entries.Count + PageSize - 1) / PageSize);

        private void Turn(int delta)
        {
            _pageIndex = Mathf.Clamp(_pageIndex + delta, 0, PageCount - 1);
            BuildPage();
        }

        private void BuildPage()
        {
            ReleaseTextures();
            for (int i = _grid.childCount - 1; i >= 0; i--)
            {
                Destroy(_grid.GetChild(i).gameObject);
            }

            _page.text = (_pageIndex + 1).ToString(CultureInfo.InvariantCulture) + " / " + PageCount.ToString(CultureInfo.InvariantCulture);
            for (int slot = 0; slot < PageSize; slot++)
            {
                // Newest first.
                int index = _entries.Count - 1 - ((_pageIndex * PageSize) + slot);
                if (index < 0)
                {
                    break;
                }

                CollectionEntry entry = _entries[index];
                int column = slot % Columns;
                int row = slot / Columns;
                float x0 = column / (float)Columns;
                float y1 = 1f - (row / (float)Rows);
                Image frame = UiFactory.CreateImage("Picture", _grid, ProceduralSprites.RoundedSquare, UiTheme.Panel);
                UiFactory.Place(frame.rectTransform, x0 + 0.01f, y1 - (1f / Rows) + 0.01f, x0 + (1f / Columns) - 0.01f, y1 - 0.01f);
                Texture2D? texture = _render(entry);
                if (texture != null)
                {
                    _textures.Add(texture);
                    // The picture keeps its own proportions inside the card (boards are up to 14×16, not square).
                    RectTransform area = UiFactory.Place(UiFactory.CreateRect("Area", frame.transform), 0.08f, 0.2f, 0.92f, 0.95f);
                    RectTransform holder = UiFactory.Stretch(UiFactory.CreateRect("Image", area));
                    RawImage image = holder.gameObject.AddComponent<RawImage>();
                    image.texture = texture;
                    image.raycastTarget = false;
                    AspectRatioFitter fitter = holder.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fitter.aspectRatio = texture.width / (float)Mathf.Max(1, texture.height);
                }

                TextMeshProUGUI label = UiFactory.CreateText("Level", frame.transform, Loc.F("common.level", entry.LevelNumber), 30f, UiTheme.Text);
                UiFactory.Place(label.rectTransform, 0f, 0.02f, 1f, 0.2f);
            }
        }

        private void ReleaseTextures()
        {
            foreach (Texture2D texture in _textures)
            {
                Destroy(texture);
            }

            _textures.Clear();
        }
    }
}
