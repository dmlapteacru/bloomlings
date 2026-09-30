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
    /// The Collection of the design board's frame 6 (spec 002 FR-024; FR-065, T145): every finished picture as a framed
    /// tile, newest first, a page at a time. Tapping a tile shows it larger with its name and "Completed at Level N". It
    /// only shows pictures: there is no way to open or replay a level from here (it is never a level selector).
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

        private static readonly Color FrameColor = UiTheme.Of(Design.Rgba.FromHex("#F3E3C3"));

        private GameObject _detail = null!;
        private RawImage _detailImage = null!;
        private AspectRatioFitter _detailFitter = null!;
        private TextMeshProUGUI _detailName = null!;
        private TextMeshProUGUI _detailLevel = null!;

        public static CollectionScreen Create(Transform parent)
        {
            CardView card = UiKit.Card("Collection", parent, Loc.T("collection.title"), 1300f, null);
            var screen = card.Root.AddComponent<CollectionScreen>();
            screen._root = card.Root;
            Button close = UiKit.RoundIconButton("Close", card.CardRect, "ui.close", screen.Hide);
            UiKit.PlaceBox((RectTransform)close.transform, card.Regions.Close, card.Regions.Card);
            RectTransform body = card.Body;
            screen._count = UiKit.Label("Count", body, string.Empty, Design.DesignTokens.Type.Caption, UiTheme.TextSecondary);
            UiFactory.Place(screen._count.rectTransform, 0f, 0.94f, 1f, 1f);
            screen._grid = UiFactory.Place(UiFactory.CreateRect("Grid", body), 0f, 0.1f, 1f, 0.93f);
            Button previous = UiKit.SecondaryButton("Previous", body, "‹", () => screen.Turn(-1));
            UiFactory.Place((RectTransform)previous.transform, 0.02f, 0f, 0.22f, 0.08f);
            screen._page = UiKit.Label("Page", body, string.Empty, Design.DesignTokens.Type.Caption, UiTheme.TextSecondary);
            UiFactory.Place(screen._page.rectTransform, 0.25f, 0f, 0.75f, 0.08f);
            Button next = UiKit.SecondaryButton("Next", body, "›", () => screen.Turn(1));
            UiFactory.Place((RectTransform)next.transform, 0.78f, 0f, 0.98f, 0.08f);

            // The detail view: one picture larger, with its name and level.
            Image detail = UiKit.Rounded("Detail", body, UiTheme.Panel, 40f, raycast: true);
            UiFactory.Stretch(detail.rectTransform);
            screen._detail = detail.gameObject;
            Image frame = UiKit.Rounded("Frame", detail.transform, FrameColor, 44f);
            UiFactory.Place(frame.rectTransform, 0.08f, 0.3f, 0.92f, 0.96f);
            Image mat = UiKit.Rounded("Mat", frame.transform, Color.white, 34f);
            UiFactory.Place(mat.rectTransform, 0.05f, 0.05f, 0.95f, 0.95f);
            RectTransform holder = UiFactory.Place(UiFactory.CreateRect("Image", mat.transform), 0.06f, 0.06f, 0.94f, 0.94f);
            screen._detailImage = holder.gameObject.AddComponent<RawImage>();
            screen._detailImage.raycastTarget = false;
            screen._detailFitter = holder.gameObject.AddComponent<AspectRatioFitter>();
            screen._detailFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            screen._detailName = UiKit.Label("Name", detail.transform, string.Empty, Design.DesignTokens.Type.Title, UiTheme.Text);
            UiFactory.Place(screen._detailName.rectTransform, 0.05f, 0.19f, 0.95f, 0.28f);
            screen._detailLevel = UiKit.Label("Completed", detail.transform, string.Empty, Design.DesignTokens.Type.Caption, UiTheme.TextSecondary);
            UiFactory.Place(screen._detailLevel.rectTransform, 0.05f, 0.13f, 0.95f, 0.19f);
            Button back = UiKit.SecondaryButton("Back", detail.transform, Loc.T("common.close"), () => screen._detail.SetActive(false));
            UiFactory.Place((RectTransform)back.transform, 0.25f, 0.01f, 0.75f, 0.1f);
            screen._detail.SetActive(false);
            card.Root.SetActive(false);
            return screen;
        }

        private void OpenDetail(CollectionEntry entry, Texture2D? texture)
        {
            _detailImage.texture = texture;
            _detailImage.enabled = texture != null;
            if (texture != null)
            {
                _detailFitter.aspectRatio = texture.width / (float)Mathf.Max(1, texture.height);
            }

            _detailName.text = entry.PictureId.Replace('_', ' ');
            _detailLevel.text = Loc.F("collection.completed", Design.NumberText.Group(entry.LevelNumber));
            _detail.SetActive(true);
        }

        /// <param name="render">Draws an entry's finished picture, or null when this content version cannot redraw it.</param>
        public void Show(IReadOnlyList<CollectionEntry> entries, Func<CollectionEntry, Texture2D?> render)
        {
            _entries = entries;
            _render = render;
            _pageIndex = 0;
            _detail.SetActive(false);
            _count.text = entries.Count == 1 ? Loc.F("collection.count_one", 1) : Loc.F("collection.count_many", entries.Count);
            BuildPage();
            _root.SetActive(true);
        }

        public void Hide()
        {
            _detail.SetActive(false);
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
                Image frame = UiKit.Rounded("Picture", _grid, FrameColor, 30f, raycast: true);
                UiFactory.Place(frame.rectTransform, x0 + 0.015f, y1 - (1f / Rows) + 0.015f, x0 + (1f / Columns) - 0.015f, y1 - 0.015f);
                Image mat = UiKit.Rounded("Mat", frame.transform, Color.white, 22f);
                UiFactory.Place(mat.rectTransform, 0.06f, 0.18f, 0.94f, 0.94f);
                Texture2D? texture = _render(entry);
                var open = frame.gameObject.AddComponent<Button>();
                open.targetGraphic = frame;
                open.onClick.AddListener(() => OpenDetail(entry, texture));
                if (texture != null)
                {
                    _textures.Add(texture);
                    // The picture keeps its own proportions inside the card (boards are up to 14×16, not square).
                    RectTransform area = UiFactory.Place(UiFactory.CreateRect("Area", mat.transform), 0.06f, 0.06f, 0.94f, 0.94f);
                    RectTransform holder = UiFactory.Stretch(UiFactory.CreateRect("Image", area));
                    RawImage image = holder.gameObject.AddComponent<RawImage>();
                    image.texture = texture;
                    image.raycastTarget = false;
                    AspectRatioFitter fitter = holder.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fitter.aspectRatio = texture.width / (float)Mathf.Max(1, texture.height);
                }

                TextMeshProUGUI label = UiKit.Label("Level", frame.transform, Loc.F("common.level", Design.NumberText.Group(entry.LevelNumber)), Design.DesignTokens.Type.Badge, UiTheme.TextSecondary);
                UiFactory.Place(label.rectTransform, 0f, 0.02f, 1f, 0.17f);
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
