using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The Collection of the design board's frame 6 (spec 002 FR-024; FR-065, T145): every finished picture as a framed
    /// tile, newest first, a page at a time. Tapping a tile shows it larger with its name and "Completed at Level N". It
    /// only shows pictures: there is no way to open or replay a level from here (it is never a level selector).
    /// <para>
    /// In the reference look of spec 005 (contracts/look.md §4.3, §4.6; the playtest's <c>MetaCards.Collection</c>, frames
    /// 6 and 20): a parchment card under a wooden sign, the count in soft brown, the pictures in raised cream frames
    /// (<c>collection.frame</c>) three to a row, the cream page arrows, and the detail as its own card whose close goes back
    /// to the grid.
    /// </para>
    /// </summary>
    public sealed class CollectionScreen : MonoBehaviour
    {
        private const int Columns = 3;
        private const int Rows = 4;
        private const int PageSize = Columns * Rows;
        private const float CellUnits = 250f;
        private const float FooterUnits = 110f;

        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private RectTransform _host = null!;
        private CardView? _grid;
        private CardView? _detail;
        private IReadOnlyList<CollectionEntry> _entries = Array.Empty<CollectionEntry>();
        private Func<CollectionEntry, Texture2D?> _render = _ => null;
        private int _pageIndex;

        public bool IsOpen => _host.gameObject.activeSelf;

        public static CollectionScreen Create(Transform parent)
        {
            // The cards are built when the Collection opens (the grid's height follows the number of pictures).
            RectTransform host = UiFactory.Stretch(UiFactory.CreateRect("Collection", parent));
            var screen = host.gameObject.AddComponent<CollectionScreen>();
            screen._host = host;
            host.gameObject.SetActive(false);
            return screen;
        }

        /// <param name="render">Draws an entry's finished picture, or null when this content version cannot redraw it.</param>
        public void Show(IReadOnlyList<CollectionEntry> entries, Func<CollectionEntry, Texture2D?> render)
        {
            _entries = entries;
            _render = render;
            _pageIndex = 0;
            _host.gameObject.SetActive(true);
            BuildGrid();
        }

        public void Hide()
        {
            CloseDetail(showGrid: false);
            DestroyCard(ref _grid);
            ReleaseTextures();
            _host.gameObject.SetActive(false);
        }

        private int PageCount => Mathf.Max(1, (_entries.Count + PageSize - 1) / PageSize);

        private static float Scale => DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);

        private void Turn(int delta)
        {
            _pageIndex = Mathf.Clamp(_pageIndex + delta, 0, PageCount - 1);
            BuildGrid();
        }

        private void DestroyCard(ref CardView? card)
        {
            if (card != null)
            {
                Destroy(card.Root);
                card = null;
            }
        }

        /// <summary>
        /// The grid card: the count, then this page's pictures (newest first) in frames of up to 250 units, three to a
        /// row, and the page arrows when there is more than one page.
        /// </summary>
        private void BuildGrid()
        {
            DestroyCard(ref _grid);
            ReleaseTextures();
            int pages = PageCount;
            int first = _pageIndex * PageSize;
            int onPage = Mathf.Clamp(_entries.Count - first, 0, PageSize);
            int rows = Mathf.Clamp((onPage + Columns - 1) / Columns, 1, Rows);
            float content = (rows * CellUnits) + 80f + (pages > 1 ? FooterUnits : 0f);
            CardView card = UiKit.Card("Grid", _host, Loc.T("collection.title"), content, Hide, sign: SignDecor.None);
            _grid = card;
            Box body = card.Regions.Body;
            float u = Scale;
            TextMeshProUGUI count = UiKit.Label("Count", card.Body, _entries.Count == 1 ? Loc.F("collection.count_one", 1) : Loc.F("collection.count_many", _entries.Count), T.Caption, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(count.rectTransform, Box.FromCenter(body.CenterX, body.Top + (22f * u), body.Width, 44f * u), body);

            float cell = Mathf.Min((body.Width - (40f * u)) / Columns, CellUnits * u);
            float x0 = body.CenterX - (cell * Columns / 2f);
            float y0 = body.Top + (60f * u);
            for (int slot = 0; slot < onPage; slot++)
            {
                // Newest first.
                CollectionEntry entry = _entries[_entries.Count - 1 - (first + slot)];
                float x = x0 + ((slot % Columns) * cell);
                float y = y0 + ((slot / Columns) * cell);
                Texture2D? texture = _render(entry);
                if (texture != null)
                {
                    _textures.Add(texture);
                }

                RectTransform frame = Frame("Picture", card.Body, texture, () => OpenDetail(entry, texture));
                UiKit.PlaceBox(frame, new Box(x, y, x + cell, y + cell).Inset(12f * u), body);
            }

            if (pages > 1)
            {
                float top = y0 + (rows * cell) + (10f * u);
                var line = new Box(body.Left, top, body.Right, top + ((FooterUnits - 10f) * u));
                // The arrows' rects are touch-sized; their cushions are 100 units.
                float touch = DesignTokens.Size.TouchMin * u;
                TextMeshProUGUI page = UiKit.Label("Page", card.Body, Loc.F("common.page", _pageIndex + 1, pages), T.Caption, UiTheme.Of(C.InkBrownSoft));
                UiKit.PlaceBox(page.rectTransform, new Box(line.Left + touch, line.Top, line.Right - touch, line.Bottom), body);
                Button previous = UiKit.PageArrow("Previous", card.Body, next: false, () => Turn(-1));
                previous.interactable = _pageIndex > 0;
                UiKit.PlaceBox((RectTransform)previous.transform, Box.FromCenter(line.Left + (touch / 2f), line.CenterY, touch, touch), body);
                Button next = UiKit.PageArrow("Next", card.Body, next: true, () => Turn(1));
                next.interactable = _pageIndex < pages - 1;
                UiKit.PlaceBox((RectTransform)next.transform, Box.FromCenter(line.Right - (touch / 2f), line.CenterY, touch, touch), body);
            }
        }

        /// <summary>
        /// The detail card (frame 20): the picture larger in its frame (at most 540 units), its name in brown and
        /// "Completed at Level N" below it; the close goes back to the grid.
        /// </summary>
        private void OpenDetail(CollectionEntry entry, Texture2D? texture)
        {
            // The grid only hides, so its pictures (this one among them) stay until the Collection closes or turns a page.
            CloseDetail(showGrid: false);
            _grid?.Root.SetActive(false);
            CardView card = UiKit.Card("Detail", _host, Loc.T("collection.title"), 700f, () => CloseDetail(showGrid: true), sign: SignDecor.None);
            _detail = card;
            Box body = card.Regions.Body;
            float u = Scale;
            float side = Mathf.Min(body.Width - (120f * u), 540f * u);
            var frameBox = new Box(body.CenterX - (side / 2f), body.Top + (10f * u), body.CenterX + (side / 2f), body.Top + (10f * u) + side);
            RectTransform frame = Frame("Picture", card.Body, texture, null);
            UiKit.PlaceBox(frame, frameBox, body);
            TextMeshProUGUI name = UiKit.Label("Name", card.Body, PictureName(entry.PictureId), T.Title, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
            UiKit.PlaceBox(name.rectTransform, Box.FromCenter(body.CenterX, frameBox.Bottom + (56f * u), body.Width, 76f * u), body);
            TextMeshProUGUI level = UiKit.Label("Completed", card.Body, Loc.F("collection.completed", NumberText.Group(entry.LevelNumber)), T.Body, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(level.rectTransform, Box.FromCenter(body.CenterX, frameBox.Bottom + (114f * u), body.Width, 56f * u), body);
        }

        private void CloseDetail(bool showGrid)
        {
            DestroyCard(ref _detail);
            if (showGrid)
            {
                _grid?.Root.SetActive(true);
            }
        }

        /// <summary>A picture's name from its id, in sentence case ("tulip_pot" → "Tulip pot").</summary>
        private static string PictureName(string id)
        {
            string name = id.Replace('_', ' ');
            return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// A finished picture in its frame (the playtest's <c>Kit.PictureFrame</c>, <c>collection.frame</c>): a raised cream
        /// frame with a lip and a soft shadow around a beige well with a shadow along its top, and the picture inside the
        /// well keeping its own proportions. With <paramref name="onTap"/> the frame squashes like a tile and opens it.
        /// </summary>
        private static RectTransform Frame(string name, Transform parent, Texture2D? texture, Action? onTap)
        {
            Image rootImage = UiFactory.CreateImage(name, parent, null, Color.clear, raycast: onTap != null);
            RectTransform root = rootImage.rectTransform;
            BoxLayout layout = BoxLayout.On(root);
            float r = 0f;
            float line = 0f;
            float wellRadius = 0f;
            float Side(Box b) => Mathf.Min(b.Width, b.Height);
            Box Face(Box b) => new Box(b.Left, b.Top, b.Right, b.Bottom - (Side(b) * 0.035f));
            Box Well(Box b) => Face(b).Inset(Side(b) * 0.07f);

            UiKit.SoftShadow(layout, b => b, b => Side(b) * 0.1f, 0.2f, 0.035f);
            Image lip = UiKit.RoundRect("Lip", root, UiTheme.Of(C.CreamLip), _ => r);
            Image face = UiKit.RoundGradient("Face", root, C.CreamTop, C.CreamFace, _ => r);
            Image outline = UiKit.RoundRing("Line", root, UiTheme.Of(C.CreamLine), _ => r, _ => line);
            Image well = UiKit.RoundGradient("Well", root, C.ParchmentWell.Mix(C.CreamTop, 0.35f), C.ParchmentWell, _ => wellRadius);
            Image shade = UiKit.RoundRect("Shade", root, Color.white, _ => wellRadius);
            UiKit.Gradient(shade, UiTheme.Of(C.GardenShadow.WithAlpha(0.12f)), UiTheme.Of(C.GardenShadow.WithAlpha(0f)));
            shade.GetComponent<VerticalGradient>().Stop = 0.16f;
            Image wellLine = UiKit.RoundRing("WellLine", root, UiTheme.Of(C.ParchmentEdge.Darken(0.08f)), _ => wellRadius, _ => line);
            RectTransform area = UiFactory.CreateRect("Area", root);

            layout.Add(lip.rectTransform, b =>
            {
                r = Side(b) * 0.1f;
                line = Mathf.Max(UiKit.Units(1f), Side(b) * 0.01f);
                wellRadius = r * 0.6f;
                return b;
            });
            layout.Add(face.rectTransform, Face);
            layout.Add(outline.rectTransform, b => b);
            layout.Add(well.rectTransform, Well);
            layout.Add(shade.rectTransform, Well);
            layout.Add(wellLine.rectTransform, Well);
            layout.Add(area, b => Well(b).Inset(Well(b).Width * 0.04f));
            layout.Then(_ =>
            {
                foreach (Image image in new[] { lip, face, outline, well, shade, wellLine })
                {
                    image.GetComponent<RoundShape>().Apply();
                }
            });

            if (texture != null)
            {
                // The picture keeps its own proportions inside the frame (boards are up to 14×16, not square).
                RectTransform holder = UiFactory.Stretch(UiFactory.CreateRect("Image", area));
                RawImage image = holder.gameObject.AddComponent<RawImage>();
                image.texture = texture;
                image.raycastTarget = false;
                AspectRatioFitter fitter = holder.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = texture.width / (float)Mathf.Max(1, texture.height);
            }

            if (onTap != null)
            {
                UiKit.TapTarget(rootImage, onTap, press: true);
            }

            return root;
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
