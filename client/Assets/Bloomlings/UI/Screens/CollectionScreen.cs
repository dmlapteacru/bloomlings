using System;
using System.Collections.Generic;
using Bloomlings.Client.Gameplay.Themes;
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
    /// A full-screen page since the owner's request of 2026-10-04 ("All the menu's places must be a separate page. Not
    /// popups."; spec 005 FR-030, contracts/look.md §6.9), every element placed from
    /// <see cref="ScreenLayout.ReferenceCollection"/>, as the playtest's <c>CollectionScreen</c> (frames 6 and 20): over the
    /// Wardrobe's garden, the page header on one line (<see cref="UiKit.PageHeader"/>: the back button, the wooden
    /// "Collection" banner with ivy, the Petals pill, whose "+" opens the Store page over it once the Store is open); a
    /// parchment panel to the bottom of the screen with the count in soft brown, the pictures in raised cream frames
    /// (<c>collection.frame</c>) three to a row, as large as fit, as many rows as the page holds, and "n / m" between the
    /// cream page arrows when they take more than one page; and the bottom menu over the panel's foot, the Collection in its
    /// medallion. A tap on a picture shows its detail on the page (<c>collection.detail_frame</c>): the picture large, its
    /// name and level. The back button returns from the detail to the grid, then hides the page, so Home shows again.
    /// </para>
    /// <para>
    /// Before its first picture (from Level 2) the bottom menu's Collection opens the page locked (<see cref="ShowLocked"/>,
    /// the playtest's <c>CollectionScreen.Locked</c>): the same garden, header and panel, the panel holding the locked
    /// notice (<see cref="LockedNoticeView"/>: "Available from level 2") instead of the count and the pictures.
    /// </para>
    /// </summary>
    public sealed class CollectionScreen : MonoBehaviour
    {
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private GameObject _root = null!;
        private PageHeaderView _header = null!;
        private Image _panel = null!;
        private TextMeshProUGUI _count = null!;
        private RectTransform _grid = null!;
        private TextMeshProUGUI _page = null!;
        private Button _previous = null!;
        private Button _next = null!;
        private RectTransform _detail = null!;
        private LockedNoticeView _notice = null!;
        private BottomNavView? _nav;
        private Func<HomeLook>? _navLook;
        private Func<long>? _petalsSource;
        private bool _store;
        private bool _plus;
        private long _petalsShown = -1;
        private Texture2D? _detailTexture;
        private bool _detailOpen;
        private ReferenceCollectionRegions _regions = null!;
        private IReadOnlyList<CollectionEntry> _entries = Array.Empty<CollectionEntry>();
        private Func<CollectionEntry, int, Texture2D?> _render = (_, _) => null;
        private int _pageIndex;

        /// <summary>Whether the page shows, locked or not.</summary>
        public bool IsOpen => _root.activeSelf;

        /// <param name="petals">The Petals balance for the header's pill; null hides the pill.</param>
        /// <param name="onStore">Opens the Store page from the pill's "+" (once the Store is open); null shows no "+".</param>
        /// <param name="onNav">A tap on another place of the bottom menu (spec 005 FR-030); null shows no menu.</param>
        /// <param name="navLook">The look that tells which places of the bottom menu are open (<see cref="BottomNav.IsOpen"/>); null: all of them.</param>
        public static CollectionScreen Create(Transform parent, Func<long>? petals = null, Action? onStore = null, Action<NavPlace>? onNav = null, Func<HomeLook>? navLook = null)
        {
            // A full screen over Home that takes every tap, on the Wardrobe's garden (the owner's picture B7, or the drawn
            // one), as the other pages.
            Image shade = UiFactory.CreateImage("Collection", parent, null, Color.clear, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var screen = shade.gameObject.AddComponent<CollectionScreen>();
            screen._root = shade.gameObject;
            screen._petalsSource = petals;
            screen._store = onStore != null;
            Transform root = shade.transform;
            BackdropView.Create(shade.rectTransform, OwnerPictures.Wardrobe, BackdropScene.Home);

            // The parchment panel (a card's radius), the count, the grid's area and the footer between the page arrows.
            screen._panel = UiKit.Paper("Panel", root, b => Mathf.Max(UiKit.Units(DesignTokens.Radius.CardMin), b.Width * DesignTokens.Radius.Card), DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard, raycast: false);
            screen._count = UiKit.Label("Count", root, string.Empty, T.Caption, UiTheme.Of(C.InkBrownSoft));
            screen._grid = UiFactory.CreateRect("Pictures", root);
            screen._page = UiKit.Label("Page", root, string.Empty, T.Caption, UiTheme.Of(C.InkBrownSoft));
            screen._previous = UiKit.PageArrow("Previous", root, next: false, () => screen.Turn(-1));
            screen._next = UiKit.PageArrow("Next", root, next: true, () => screen.Turn(1));

            // A picture's detail in the page's area, shown in the grid's place.
            screen._detail = UiFactory.CreateRect("Detail", root);
            screen._detail.gameObject.SetActive(false);

            // The locked notice in the grid's place, shown only before the first picture (FR-030).
            screen._notice = UiKit.LockedNotice("Locked", root);
            screen._notice.gameObject.SetActive(false);

            // The bottom menu over the panel's foot, the Collection in its medallion (FR-030).
            if (onNav != null)
            {
                screen._navLook = navLook;
                screen._nav = UiKit.BottomNav("BottomNav", root, place =>
                {
                    if (place != NavPlace.Collection)
                    {
                        onNav(place);
                    }
                });
            }

            // The header last, as on the other pages; its back leaves a picture's detail for the grid, then the page.
            screen._header = UiKit.PageHeader(root, Loc.T("collection.title"), screen.Back, petals != null, onStore);
            shade.gameObject.SetActive(false);
            return screen;
        }

        /// <param name="render">
        /// Draws an entry's finished picture to fit a side in pixels (0: at full detail), or null when this content version
        /// cannot redraw it. The grid draws each picture at its frame's own resolution, the detail at full detail.
        /// </param>
        public void Show(IReadOnlyList<CollectionEntry> entries, Func<CollectionEntry, int, Texture2D?> render)
        {
            _entries = entries;
            _render = render;
            _pageIndex = 0;
            _root.SetActive(true);
            SyncPlus();
            CloseDetail();
            Layout();
            BuildGrid();
        }

        /// <summary>
        /// Shows the page locked (spec 005 FR-030, contracts/look.md §6.7; <see cref="ScreenLayout.LockedPage"/>): the page
        /// header as usual, the panel holding the locked notice of the Collection, available from <paramref name="level"/>
        /// (<see cref="BottomNav.UnlockLevel"/>, <see cref="BottomNav.CollectionLevel"/>), and the bottom menu with the
        /// Collection raised; the count, the pictures and the page arrows hide. Its back hides it as usual.
        /// </summary>
        public void ShowLocked(int level)
        {
            _entries = Array.Empty<CollectionEntry>();
            _pageIndex = 0;
            _root.SetActive(true);
            SyncPlus();
            CloseDetail();
            ClearGrid();
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            LockedPageRegions r = ScreenLayout.LockedPage(w, h, insets);
            _header.Place(r.Header);
            float radius = r.PanelRadius(DesignTokens.ScaleFor(w, h));
            UiKit.PlaceScreen(_panel.rectTransform, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            SetGrid(false);
            _notice.gameObject.SetActive(true);
            UiKit.PlaceScreen((RectTransform)_notice.transform, r.Notice);
            _notice.Show(NavPlace.Collection, level);
            _nav?.Show(NavPlace.Collection, _navLook?.Invoke() ?? HomeLook.All);
        }

        public void Hide()
        {
            CloseDetail();
            ClearGrid();
            _root.SetActive(false);
        }

        /// <summary>The header's back: from a picture's detail to the grid, from the grid (or the locked page) to Home.</summary>
        private void Back()
        {
            if (_detailOpen)
            {
                CloseDetail();
                SetGrid(true);
                BuildFooter();
                return;
            }

            Hide();
        }

        private void Update()
        {
            // The balance follows the economy (a purchase in the Store opened from the "+").
            if (_header.Petals != null && _petalsSource != null)
            {
                long now = _petalsSource();
                if (now != _petalsShown)
                {
                    _header.Petals.Show(now, _plus);
                    _petalsShown = now;
                }
            }
        }

        /// <summary>Whether the Petals pill shows its "+" as the page opens: with a Store to open, once the Store is open (as on Home).</summary>
        private void SyncPlus()
        {
            _plus = _store && (_navLook == null || _navLook().Store);
            _petalsShown = -1;
        }

        /// <summary>Places the page on the kit's regions for the screen's shape (contracts/look.md §6.9).</summary>
        private void Layout()
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            ReferenceCollectionRegions r = ScreenLayout.ReferenceCollection(w, h, insets);
            _regions = r;
            _header.Place(r.Header);

            // The panel runs to the bottom of the screen: its bottom corners go past the edge.
            float radius = r.PanelRadius(DesignTokens.ScaleFor(w, h));
            UiKit.PlaceScreen(_panel.rectTransform, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            _notice.gameObject.SetActive(false);
            UiKit.PlaceScreen(_count.rectTransform, r.Count);
            UiKit.PlaceScreen(_grid, r.Area);
            UiKit.PlaceScreen(_detail, r.Area);

            // The footer between the arrows, their 0.09 W cushions in touch-sized squares (as the Store page's).
            float touch = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(w, h);
            float room = r.Footer.Height + (12f * DesignTokens.ScaleFor(w, h));
            UiKit.PlaceScreen(_page.rectTransform, new Box(r.Footer.Left + room, r.Footer.Top, r.Footer.Right - room, r.Footer.Bottom));
            UiKit.PlaceScreen((RectTransform)_previous.transform, Box.FromCenter(r.PagePrevious.CenterX, r.PagePrevious.CenterY, touch, touch));
            UiKit.PlaceScreen((RectTransform)_next.transform, Box.FromCenter(r.PageNext.CenterX, r.PageNext.CenterY, touch, touch));
            SetGrid(true);
            _nav?.Show(NavPlace.Collection, _navLook?.Invoke() ?? HomeLook.All);
        }

        /// <summary>Shows or hides the grid's parts: the count, the pictures and the footer (the footer only with more than one page).</summary>
        private void SetGrid(bool shown)
        {
            _count.gameObject.SetActive(shown);
            _grid.gameObject.SetActive(shown);
            bool paged = shown && _regions != null && _regions.Pages(_entries.Count) > 1;
            _page.gameObject.SetActive(paged);
            _previous.gameObject.SetActive(paged);
            _next.gameObject.SetActive(paged);
        }

        private void Turn(int delta)
        {
            _pageIndex = Mathf.Clamp(_pageIndex + delta, 0, _regions.Pages(_entries.Count) - 1);
            BuildGrid();
        }

        private void ClearGrid()
        {
            for (int i = _grid.childCount - 1; i >= 0; i--)
            {
                Destroy(_grid.GetChild(i).gameObject);
            }

            ReleaseTextures();
        }

        /// <summary>
        /// The grid: the count, then this page's pictures (newest first) in square frames three to a row
        /// (<see cref="ReferenceCollectionRegions.Cell"/>, <see cref="ReferenceCollectionRegions.PerPage"/>), and the page
        /// arrows when there is more than one page.
        /// </summary>
        private void BuildGrid()
        {
            ClearGrid();
            ReferenceCollectionRegions r = _regions;
            _count.text = _entries.Count == 1 ? Loc.F("collection.count_one", 1) : Loc.F("collection.count_many", _entries.Count);
            int perPage = r.PerPage(_entries.Count);
            _pageIndex = Mathf.Clamp(_pageIndex, 0, r.Pages(_entries.Count) - 1);
            int first = _pageIndex * perPage;
            int onPage = Mathf.Clamp(_entries.Count - first, 0, perPage);
            int framePixels = Mathf.Max(1, Mathf.CeilToInt(r.CellSize));
            for (int slot = 0; slot < onPage; slot++)
            {
                // Newest first.
                CollectionEntry entry = _entries[_entries.Count - 1 - (first + slot)];
                Texture2D? texture = _render(entry, framePixels);
                if (texture != null)
                {
                    _textures.Add(texture);
                }

                RectTransform frame = Frame("Picture", _grid, texture, () => OpenDetail(entry, texture != null));
                UiKit.PlaceBox(frame, r.Cell(slot), r.Area);
            }

            SetGrid(true);
            BuildFooter();
        }

        /// <summary>The footer "n / m" and the arrows' states (greyed where there is no page to turn to).</summary>
        private void BuildFooter()
        {
            int pages = _regions.Pages(_entries.Count);
            _page.text = Loc.F("common.page", _pageIndex + 1, pages);
            _previous.interactable = _pageIndex > 0;
            _next.interactable = _pageIndex < pages - 1;
        }

        /// <summary>
        /// A picture's detail on the page (frame 20, <c>collection.detail_frame</c>): the picture large in its frame
        /// (<see cref="ReferenceCollectionRegions.Picture"/>), its name in brown and "Completed at Level N" below it, in the
        /// grid's place; the header's back returns to the grid.
        /// </summary>
        private void OpenDetail(CollectionEntry entry, bool drawn)
        {
            // The grid only hides, so its thumbnails stay until the page closes or turns; the detail draws its picture at
            // full detail and releases it when it closes.
            CloseDetail();
            SetGrid(false);
            _detailOpen = true;
            _detail.gameObject.SetActive(true);
            ReferenceCollectionRegions r = _regions;
            Texture2D? texture = drawn ? _render(entry, 0) : null;
            _detailTexture = texture;
            RectTransform frame = Frame("Picture", _detail, texture, null);
            UiKit.PlaceBox(frame, r.Picture, r.Area);
            TextMeshProUGUI name = UiKit.Label("Name", _detail, PictureName(entry.PictureId), T.Title, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
            UiKit.PlaceBox(name.rectTransform, r.Name, r.Area);
            TextMeshProUGUI level = UiKit.Label("Completed", _detail, Loc.F("collection.completed", NumberText.Group(entry.LevelNumber)), T.Body, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(level.rectTransform, r.Level, r.Area);
        }

        private void CloseDetail()
        {
            _detailOpen = false;
            for (int i = _detail.childCount - 1; i >= 0; i--)
            {
                Destroy(_detail.GetChild(i).gameObject);
            }

            _detail.gameObject.SetActive(false);
            if (_detailTexture != null)
            {
                Destroy(_detailTexture);
                _detailTexture = null;
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
