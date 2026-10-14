using System;
using System.Collections.Generic;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Virtualized two-column collection cabinet. A mosaic opens a detail; back preserves the scroll position.</summary>
    public sealed class CollectionScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private PageHeaderView _header = null!;
        private RectTransform _viewport = null!, _content = null!, _detail = null!;
        private ScrollRect _scroll = null!;
        private Image _countPlate = null!, _lockedFrame = null!;
        private TextMeshProUGUI _count = null!;
        private LockedNoticeView _notice = null!;
        private BottomNavView? _nav;
        private Func<HomeLook>? _navLook;
        private Func<long>? _petals;
        private bool _store, _locked, _detailOpen;
        private long _shownPetals = -1;
        private float _lastOffset = -1;
        private CollectionGalleryRegions _regions = null!;
        private IReadOnlyList<CollectionEntry> _entries = Array.Empty<CollectionEntry>();
        private Func<CollectionEntry, int, Texture2D?> _render = (_, _) => null;
        private readonly Dictionary<int, (GameObject Root, Texture2D? Texture)> _cells = new Dictionary<int, (GameObject, Texture2D?)>();
        private readonly Dictionary<int, Image> _shelves = new Dictionary<int, Image>();
        private Texture2D? _detailTexture;
        public bool IsOpen => _root.activeSelf;
        private Box ContentBox => new Box(_regions.Area.Left, _regions.Area.Top, _regions.Area.Right,
            _regions.Area.Top + _regions.ContentHeight(_entries.Count) + _regions.Viewport.Height - _regions.Area.Height);

        public static CollectionScreen Create(Transform parent, Func<long>? petals = null, Action? onStore = null,
            Action<NavPlace>? onNav = null, Func<HomeLook>? navLook = null)
        {
            Image root = UiFactory.CreateImage("Collection", parent, null, Color.clear, raycast: true);
            UiFactory.Stretch(root.rectTransform);
            var s = root.gameObject.AddComponent<CollectionScreen>();
            s._root = root.gameObject; s._petals = petals; s._store = onStore != null; s._navLook = navLook;
            BackdropView.Create(root.rectTransform, OwnerPictures.Collection, BackdropScene.Home);
            Image viewport = UiFactory.CreateImage("GalleryViewport", root.transform, null, Color.clear, raycast: true);
            s._viewport = viewport.rectTransform; viewport.gameObject.AddComponent<RectMask2D>();
            s._content = UiFactory.CreateRect("Shelves", viewport.transform);
            s._scroll = viewport.gameObject.AddComponent<ScrollRect>();
            s._scroll.viewport = s._viewport; s._scroll.content = s._content;
            s._scroll.horizontal = false; s._scroll.vertical = true; s._scroll.inertia = true;
            s._scroll.movementType = ScrollRect.MovementType.Clamped; s._scroll.decelerationRate = .015f;
            s._scroll.scrollSensitivity = 40f;
            s._countPlate = MetaShelfView.Skin("CountPlate", root.transform, MetaScreenArt.Tag);
            s._count = MetaShelfView.Label("Count", root.transform, "", ShopCatalogDesign.Name);
            s._detail = UiFactory.CreateRect("PictureDetail", root.transform); s._detail.gameObject.SetActive(false);
            s._lockedFrame = UiKit.CardFrame("LockedPanel", root.transform, false);
            s._notice = UiKit.LockedNotice("Locked", root.transform);
            if (onNav != null) s._nav = UiKit.BottomNav("BottomNav", root.transform, place => { if (place != NavPlace.Collection) onNav(place); });
            s._header = UiKit.PageHeader(root.transform, Loc.T("collection.title"), s.Back, petals != null, onStore);
            s._root.SetActive(false); return s;
        }

        public void Show(IReadOnlyList<CollectionEntry> entries, Func<CollectionEntry, int, Texture2D?> render)
        {
            _entries = entries; _render = render; _locked = false; _shownPetals = -1;
            _root.SetActive(true); CloseDetail(); ClearVisible();
            var f = UiKit.ScreenFrame(); _regions = CollectionGalleryDesign.Page(f.Width, f.Height, f.Insets);
            var r = _regions; _header.Place(r.Header);
            UiKit.PlaceScreen(_viewport, r.Viewport); UiKit.PlaceBox(_content, ContentBox, r.Viewport);
            _scroll.StopMovement(); _content.anchoredPosition = Vector2.zero;
            UiKit.PlaceScreen(_countPlate.rectTransform, r.Count); UiKit.PlaceScreen(_count.rectTransform, r.Count.Inset(r.W * .015f));
            _count.text = Loc.F(entries.Count == 1 ? "collection.count_one" : "collection.count_many", entries.Count);
            _notice.gameObject.SetActive(false); _lockedFrame.gameObject.SetActive(false);
            SetGallery(true); UpdateVisible(); _nav?.Show(NavPlace.Collection, _navLook?.Invoke() ?? HomeLook.All);
        }

        public void ShowLocked(int level)
        {
            _locked = true; _shownPetals = -1; _root.SetActive(true); CloseDetail(); ClearVisible(); SetGallery(false);
            var f = UiKit.ScreenFrame(); var r = ScreenLayout.LockedPage(f.Width, f.Height, f.Insets);
            _header.Place(r.Header); UiKit.PlaceScreen(_lockedFrame.rectTransform, r.Panel);
            UiKit.PlaceScreen((RectTransform)_notice.transform, r.Notice);
            _notice.Show(NavPlace.Collection, level); _notice.gameObject.SetActive(true); _lockedFrame.gameObject.SetActive(true);
            _nav?.Show(NavPlace.Collection, _navLook?.Invoke() ?? HomeLook.All);
        }

        public void Hide() { _scroll.StopMovement(); CloseDetail(); ClearVisible(); _root.SetActive(false); }
        private void Back() { if (_detailOpen) { CloseDetail(); SetGallery(true); } else Hide(); }
        private void SetGallery(bool shown)
        {
            _viewport.gameObject.SetActive(shown); _countPlate.gameObject.SetActive(shown); _count.gameObject.SetActive(shown);
        }
        private void Update()
        {
            if (_petals != null && _header.Petals != null)
            {
                long now = _petals();
                if (now != _shownPetals) { _header.Petals.Show(now, _store && (_navLook == null || _navLook().Store)); _shownPetals = now; }
            }
            if (!_locked && !_detailOpen && _regions != null && Mathf.Abs(_content.anchoredPosition.y * UiKit.PixelsPerUnit - _lastOffset) > .2f) UpdateVisible();
        }
        private void UpdateVisible()
        {
            var r = _regions;
            float offset = Mathf.Clamp(_content.anchoredPosition.y * UiKit.PixelsPerUnit, 0, r.Maximum(_entries.Count));
            var rows = r.VisibleRows(_entries.Count, offset);
            var remove = new List<int>();
            foreach (var pair in _cells) if (pair.Key / 2 < rows.First || pair.Key / 2 > rows.Last) remove.Add(pair.Key);
            foreach (int slot in remove) { ReleaseCell(slot); _cells.Remove(slot); }
            remove.Clear();
            foreach (var pair in _shelves) if (pair.Key < rows.First || pair.Key > rows.Last) remove.Add(pair.Key);
            foreach (int row in remove) { _shelves[row].gameObject.SetActive(false); Destroy(_shelves[row].gameObject); _shelves.Remove(row); }
            for (int row = rows.First; row <= rows.Last; row++)
            {
                if (!_shelves.ContainsKey(row))
                {
                    var shelf = MetaShelfView.Skin("Shelf" + row, _content, MetaScreenArt.Shelf);
                    UiKit.PlaceBox(shelf.rectTransform, r.Row(row), ContentBox); shelf.transform.SetAsFirstSibling(); _shelves[row] = shelf;
                }
                for (int col = 0; col < 2; col++)
                {
                    int slot = row * 2 + col;
                    if (slot >= _entries.Count || _cells.ContainsKey(slot)) continue;
                    CollectionEntry entry = _entries[_entries.Count - 1 - slot];
                    Box cell = r.Cell(slot);
                    Image hit = UiFactory.CreateImage("Picture" + slot, _content, null, Color.clear, raycast: true);
                    UiKit.PlaceBox(hit.rectTransform, cell, ContentBox); UiKit.TapTarget(hit, () => OpenDetail(entry));
                    Texture2D? texture = _render(entry, Mathf.CeilToInt(CollectionGalleryDesign.Art(cell).Width));
                    UiKit.PlaceBox(Frame("Mosaic", hit.transform, texture, null), CollectionGalleryDesign.Art(cell), cell);
                    Box name = CollectionGalleryDesign.Name(cell);
                    UiKit.PlaceBox(MetaShelfView.Skin("NamePlate", hit.transform, MetaScreenArt.Tag).rectTransform, name, cell);
                    var title = MetaShelfView.Label("Title", hit.transform, PictureName(entry.PictureId), CollectionGalleryDesign.Caption);
                    UiKit.PlaceBox(title.rectTransform, name.Inset(name.Height * .1f), cell);
                    var level = MetaShelfView.Label("Level", hit.transform, Loc.F("common.level", NumberText.Group(entry.LevelNumber)), ProfileGameDesign.Info, ShopBodyDesign.Letters);
                    UiKit.PlaceBox(level.rectTransform, CollectionGalleryDesign.Level(cell), cell);
                    _cells[slot] = (hit.gameObject, texture);
                }
            }
            _lastOffset = offset;
        }
        private void ReleaseCell(int slot)
        {
            var cell = _cells[slot]; cell.Root.SetActive(false); Destroy(cell.Root);
            if (cell.Texture != null) Destroy(cell.Texture);
        }
        private void ClearVisible()
        {
            foreach (int slot in _cells.Keys) ReleaseCell(slot);
            _cells.Clear(); foreach (var shelf in _shelves.Values) { shelf.gameObject.SetActive(false); Destroy(shelf.gameObject); } _shelves.Clear(); _lastOffset = -1;
        }
        private void OpenDetail(CollectionEntry entry)
        {
            CloseDetail(); _scroll.StopMovement(); SetGallery(false); _detailOpen = true; _detail.gameObject.SetActive(true);
            var r = _regions;
            var card = UiKit.CardFrame("DetailPanel", _detail, false);
            UiKit.PlaceScreen(card.rectTransform, new Box(r.Area.Left, r.Area.Top, r.Area.Right, Mathf.Min(r.Area.Bottom, r.Level.Bottom + r.W * .08f)));
            _detailTexture = _render(entry, 0);
            UiKit.PlaceScreen(Frame("Picture", _detail, _detailTexture, null), r.Picture);
            UiKit.PlaceScreen(MetaShelfView.Skin("NamePlate", _detail, MetaScreenArt.Tag).rectTransform, r.Name);
            UiKit.PlaceScreen(MetaShelfView.Label("Name", _detail, PictureName(entry.PictureId), ProfileGameDesign.Name).rectTransform, r.Name.Inset(r.W * .03f));
            UiKit.PlaceScreen(MetaShelfView.Label("Completed", _detail, Loc.F("collection.completed", NumberText.Group(entry.LevelNumber)), ProfileGameDesign.Info).rectTransform, r.Level);
        }
        private void CloseDetail()
        {
            _detailOpen = false;
            for (int i = _detail.childCount - 1; i >= 0; i--) { var child = _detail.GetChild(i); child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _detail.gameObject.SetActive(false);
            if (_detailTexture != null) { Destroy(_detailTexture); _detailTexture = null; }
        }
        private static string PictureName(string id)
        {
            string name = id.Replace('_', ' '); return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
        }
        private static RectTransform Frame(string name, Transform parent, Texture2D? texture, Action? onTap)
        {
            Image rootImage = UiFactory.CreateImage(name, parent, null, Color.clear, raycast: onTap != null);
            RectTransform root = rootImage.rectTransform;
            BoxLayout layout = BoxLayout.On(root);
            float r = 0f;
            float line = 0f;
            float wellRadius = 0f;
            float Side(Box b) => Mathf.Min(b.Width, b.Height);
            Box Well(Box b) => b.Inset(Side(b)*.035f).Inset(Side(b)*.06f);

            // The frame: a tile in a thin wooden rim (spec 005 FR-047, the playtest's Kit.PictureFrame), its corners 10% of its side.
            Image wood=UiKit.RoundGradient("Wood",root,C.WoodLight,C.WoodMid,b=>Side(b)*.1f);
            Image face=UiKit.RoundGradient("Face",root,C.CreamTop,C.CreamFace,b=>Side(b)*.065f);
            layout.Add(wood.rectTransform,b=>b);layout.Add(face.rectTransform,b=>b.Inset(Side(b)*.035f));
            Image well = UiKit.RoundGradient("Well", root, C.ParchmentWell.Mix(C.CreamTop, 0.35f), C.ParchmentWell, _ => wellRadius);
            Image wellLine = UiKit.RoundRing("WellLine", root, UiTheme.Of(C.ParchmentEdge.Darken(0.08f)), _ => wellRadius, _ => line);
            RectTransform area = UiFactory.CreateRect("Area", root);

            layout.Then(b =>
            {
                r = Side(b) * 0.1f;
                line = Mathf.Max(UiKit.Units(1f), Side(b) * 0.01f);
                wellRadius = r * 0.6f;
            });
            layout.Add(well.rectTransform, Well);
            layout.Add(wellLine.rectTransform, Well);
            layout.Add(area, b => Well(b).Inset(Well(b).Width * 0.04f));
            layout.Then(_ =>
            {
                foreach (Image image in new[] { well, wellLine })
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

    }
}
