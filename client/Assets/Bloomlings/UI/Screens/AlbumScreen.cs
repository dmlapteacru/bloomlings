using System;
using System.Collections.Generic;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Album;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The Scene Album page in uGUI (spec 007-scene-album, the owner's UX of 2026-11-02; the twin of the playtest's
    /// <c>AlbumScreen</c>), a place of the bottom menu in the Collection's place: the pages' header, the scene's name on the
    /// name plate, one big vertical card (the owner's layers in the popups' wooden card frame, locked objects grey at 82 %,
    /// new reveals coming back to colour) with its neighbours' edges, the existing raised ‹ › buttons and a horizontal swipe
    /// turning one card with a snap (no cycling), the medallions and counts, a complete scene's reward strip (Claim, then
    /// "Reward claimed"), and the object sheet from the bottom (the colour preview, the name, "Unlocks after Level N",
    /// "Next level: M" and "Continue playing", or "Level N completed"; closed by its ×, a swipe down or a tap outside; the
    /// card shrinks over it where it would cover the object). A tap picks an object by its layer's alpha
    /// (<see cref="AlbumLook.HitObject"/>). Before L27 the locked notice. Layout and motion are the kit's
    /// <see cref="AlbumLook"/>.
    /// </summary>
    public sealed class AlbumScreen : MonoBehaviour
    {
        private AlbumService _album = null!;
        private GameObject _root = null!;
        private PageHeaderView _header = null!;
        private Image _lockedFrame = null!;
        private LockedNoticeView _notice = null!;
        private BottomNavView? _nav;
        private Func<HomeLook>? _navLook;
        private Func<long>? _petals;
        private Action? _continue;
        private bool _store;
        private long _shownPetals = -1;
        private RectTransform _page = null!, _cards = null!, _band = null!, _medallions = null!, _sheet = null!, _rise = null!;
        private Image _plate = null!;
        private TextMeshProUGUI _plateLabel = null!, _count = null!, _status = null!, _sceneLine = null!;
        private Button _previous = null!, _next = null!;
        private RectTransform _strip = null!, _reward = null!, _claimed = null!;
        private Image _petal = null!;
        private TextMeshProUGUI _amount = null!, _claimedLabel = null!;
        private Button _claim = null!;
        private readonly CardView?[] _cardViews = new CardView?[3];
        private int _index;
        private int _turnStep;
        private float _turnAt = -10f, _shownAt = -10f, _sheetAt = -10f;
        private IReadOnlyList<string> _reveals = Array.Empty<string>();
        private string? _sheetObject;
        private AlbumRegions _r = null!;
        private AlbumSheetRegions _s = null!;

        // The sheet's parts.
        private RawImage _preview = null!;
        private TextMeshProUGUI _sheetName = null!, _line1 = null!, _line2 = null!;
        private Image _lockIcon = null!;
        private RectTransform _openCheck = null!;
        private Button _continueButton = null!;
        private Image _scrim = null!, _panel = null!, _grip = null!, _well = null!;
        private RectTransform _close = null!;

        public bool IsOpen => _root.activeSelf;

        /// <summary>The page was left (its back, the system back, another place): Home redraws its menu's "!".</summary>
        public event Action? Closed;

        public static AlbumScreen Create(Transform parent, AlbumService album, Func<long>? petals, Action? onStore, Action<NavPlace>? onNav, Func<HomeLook>? navLook, Action onContinue)
        {
            Image root = UiFactory.CreateImage("Album", parent, null, Color.clear, raycast: true);
            UiFactory.Stretch(root.rectTransform);
            var s = root.gameObject.AddComponent<AlbumScreen>();
            s._album = album;
            s._root = root.gameObject;
            s._petals = petals;
            s._store = onStore != null;
            s._navLook = navLook;
            s._continue = onContinue;
            BackdropView.Create(root.rectTransform, OwnerPictures.Collection, BackdropScene.Home);
            s._page = UiFactory.Stretch(UiFactory.CreateRect("Page", root.transform));
            s._band = UiFactory.CreateRect("SwipeBand", s._page);
            s._cards = UiFactory.Stretch(UiFactory.CreateRect("Cards", s._page));
            s._plate = MetaShelfView.Skin("NamePlate", s._page, MetaScreenArt.Tag);
            s._plateLabel = MetaShelfView.Label("SceneName", s._page, string.Empty, ShopCatalogDesign.Name);
            s._previous = UiKit.ArrowButton("Previous", s._page, false, () => s.Turn(-1));
            s._next = UiKit.ArrowButton("Next", s._page, true, () => s.Turn(1));
            (RectTransform strip, BoxLayout stripLayout) = UiKit.Element("Strip", s._page);
            s._strip = strip;
            UiKit.RaisedRow(stripLayout, s._strip, 0.3f);
            s._medallions = UiFactory.Stretch(UiFactory.CreateRect("Medallions", s._page));
            s._count = MetaShelfView.Label("Count", s._page, string.Empty, ProfileGameDesign.Name);
            s._status = MetaShelfView.Label("Status", s._page, Loc.T("album.complete"), ProfileGameDesign.Item);
            s._sceneLine = MetaShelfView.Label("SceneIndex", s._page, string.Empty, ProfileGameDesign.Info);
            (RectTransform reward, BoxLayout rewardLayout) = UiKit.Element("Reward", s._page);
            s._reward = reward;
            UiKit.RaisedRow(rewardLayout, s._reward, 0.4f);
            s._petal = UiKit.PetalIcon("Petals", s._reward);
            s._amount = MetaShelfView.Label("Amount", s._reward, string.Empty, ProfileGameDesign.Name);
            s._amount.alignment = TextAlignmentOptions.Left;
            s._claim = UiKit.PrimaryButton("Claim", s._reward, Loc.T("album.claim"), s.Claim, ProfileGameDesign.Action);
            s._claimed = UiFactory.Stretch(UiFactory.CreateRect("Claimed", s._reward));
            s._claimedLabel = MetaShelfView.Label("ClaimedLabel", s._claimed, Loc.T("album.reward_claimed"), ProfileGameDesign.Name);
            s._claimedLabel.alignment = TextAlignmentOptions.Left;
            s._lockedFrame = UiKit.CardFrame("LockedPanel", root.transform, false);
            s._notice = UiKit.LockedNotice("Locked", root.transform);
            // A horizontal swipe on the card band turns one card (spec 005 FR-041: a drag never taps).
            UiKit.Scrolls(root.gameObject, step => s.Turn(step), s._band);
            if (onNav != null)
            {
                s._nav = UiKit.BottomNav("BottomNav", root.transform, place => { if (place != NavPlace.Album) onNav(place); });
            }

            s._header = UiKit.PageHeader(root.transform, Loc.T("album.heading"), s.Back, petals != null, onStore);
            s.BuildSheet(root.transform);
            s._root.SetActive(false);
            return s;
        }

        /// <summary>Opens the album (spec 007 FR-004) on the scene with news, else the last viewed incomplete one, else the nearest incomplete one.</summary>
        public void Show()
        {
            _root.SetActive(true);
            _shownPetals = -1;
            _sheetObject = null;
            _sheet.gameObject.SetActive(false);
            _turnAt = -10f;
            _lockedFrame.gameObject.SetActive(false);
            _notice.gameObject.SetActive(false);
            _page.gameObject.SetActive(true);
            _index = Math.Max(0, _album.OpeningIndex());
            var f = UiKit.ScreenFrame();
            _header.Place(AlbumLook.Page(f.Width, f.Height, f.Insets, 853, 1844).Header);
            ShowScene();
            _nav?.Show(NavPlace.Album, _navLook?.Invoke() ?? HomeLook.All);
        }

        /// <summary>The locked page (before L27): the notice "Available from level 27".</summary>
        public void ShowLocked(int level)
        {
            _root.SetActive(true);
            _shownPetals = -1;
            _sheet.gameObject.SetActive(false);
            _page.gameObject.SetActive(false);
            var f = UiKit.ScreenFrame();
            LockedPageRegions r = ScreenLayout.LockedPage(f.Width, f.Height, f.Insets);
            _header.Place(r.Header);
            UiKit.PlaceScreen(_lockedFrame.rectTransform, r.Panel);
            UiKit.PlaceScreen((RectTransform)_notice.transform, r.Notice);
            _notice.Show(NavPlace.Album, level);
            _notice.gameObject.SetActive(true);
            _lockedFrame.gameObject.SetActive(true);
            _nav?.Show(NavPlace.Album, _navLook?.Invoke() ?? HomeLook.All);
        }

        public void Hide()
        {
            bool open = _root.activeSelf;
            _sheetObject = null;
            _root.SetActive(false);
            if (open)
            {
                Closed?.Invoke();
            }
        }

        private void Back()
        {
            if (_sheetObject != null)
            {
                CloseSheet();
            }
            else
            {
                Hide();
            }
        }

        private void Turn(int step)
        {
            int next = _index + Math.Sign(step);
            if (step == 0 || _sheetObject != null || next < 0 || next >= _album.Shown.Count || !_page.gameObject.activeSelf)
            {
                return;
            }

            _index = next;
            _turnStep = Math.Sign(step);
            _turnAt = Time.unscaledTime;
            ShowScene();
        }

        /// <summary>Rebuilds the three cards, the strip and the reward for the shown scene; its new reveals start and it counts as viewed.</summary>
        private void ShowScene()
        {
            IReadOnlyList<AlbumScene> shown = _album.Shown;
            if (shown.Count == 0)
            {
                return;
            }

            _index = Math.Min(_index, shown.Count - 1);
            AlbumScene scene = shown[_index];
            _reveals = _album.NewReveals(scene);
            _shownAt = Time.unscaledTime;
            _album.Viewed(scene);
            var f = UiKit.ScreenFrame();
            _r = AlbumLook.Page(f.Width, f.Height, f.Insets, scene.CanvasWidth, scene.CanvasHeight);
            _s = AlbumLook.Sheet(f.Width, f.Height, f.Insets);
            for (int i = 0; i < 3; i++)
            {
                _cardViews[i]?.Destroy();
                int at = _index + i - 1;
                _cardViews[i] = at >= 0 && at < shown.Count ? new CardView(_cards, shown[at], i == 1 ? OnSceneTap : (Action<Vector2>?)null) : null;
            }

            AlbumSceneView view = _album.View(scene);
            _plateLabel.text = Loc.T(scene.TitleKey);
            _previous.interactable = _index > 0;
            _next.interactable = _index < shown.Count - 1;
            _count.text = Loc.F("album.count", view.OpenCount, view.Total);
            _status.gameObject.SetActive(view.Complete);
            _sceneLine.text = Loc.F("album.scene_index", _index + 1, shown.Count);
            BuildMedallions(view);
            ShowReward(view);
            Layout();
        }

        private void BuildMedallions(AlbumSceneView view)
        {
            for (int i = _medallions.childCount - 1; i >= 0; i--)
            {
                Transform child = _medallions.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            for (int i = 0; i < view.Objects.Count; i++)
            {
                AlbumObjectView item = view.Objects[i];
                Box disc = _r.Medallion(i, view.Objects.Count);
                Box touch = _r.MedallionTouch(i, view.Objects.Count);
                Image hit = UiFactory.CreateImage("Medallion" + i, _medallions, null, Color.clear, raycast: true);
                UiKit.PlaceScreen(hit.rectTransform, touch);
                string id = item.Item.Id;
                UiKit.TapTarget(hit, () => OpenSheet(id), press: true);
                Image ring = UiKit.RoundRect("Ring", hit.transform, UiTheme.Of(C.WoodDarkTop), b => b.Width / 2f);
                Image cream = UiKit.RoundRect("Cream", hit.transform, UiTheme.Of(C.CreamTop), b => b.Width / 2f);
                Image well = UiKit.RoundRect("Well", hit.transform, UiTheme.Of(C.ParchmentWell.Mix(C.CreamTop, 0.5f)), b => b.Width / 2f);
                UiKit.PlaceBox(ring.rectTransform, disc, touch);
                UiKit.PlaceBox(cream.rectTransform, disc.Inset(disc.Width * 0.07f), touch);
                UiKit.PlaceBox(well.rectTransform, disc.Inset(disc.Width * 0.1f), touch);
                Texture2D? picture = item.Open ? AlbumArt.Picture(item.Item.Layer) : AlbumArt.LockedPicture(item.Item.Layer);
                if (picture != null)
                {
                    RawImage art = new GameObject("Object", typeof(RectTransform)).AddComponent<RawImage>();
                    art.transform.SetParent(hit.transform, false);
                    art.texture = picture;
                    art.raycastTarget = false;
                    UiKit.PlaceBox(art.rectTransform, AlbumLook.Fit(disc.Inset(disc.Width * 0.15f), picture.width, picture.height), touch);
                }

                if (item.Open)
                {
                    float check = disc.Width * 0.34f;
                    RectTransform badge = CheckBadge("Check", hit.transform);
                    UiKit.PlaceBox(badge, Box.FromCenter(disc.Right - (check * 0.45f), disc.Bottom - (check * 0.45f), check, check), touch);
                }
            }
        }

        private void ShowReward(AlbumSceneView view)
        {
            _reward.gameObject.SetActive(view.Complete);
            if (!view.Complete)
            {
                return;
            }

            Box row = _r.Reward;
            UiKit.PlaceScreen(_reward, row);
            bool claimed = view.RewardClaimed;
            _petal.gameObject.SetActive(!claimed);
            _amount.gameObject.SetActive(!claimed);
            _claim.gameObject.SetActive(!claimed);
            _claimed.gameObject.SetActive(claimed);
            float icon = row.Height * 0.62f;
            if (claimed)
            {
                UiKit.PlaceBox(_claimed, row, row);
                for (int i = _claimed.childCount - 1; i >= 0; i--)
                {
                    if (_claimed.GetChild(i).name == "Check")
                    {
                        Destroy(_claimed.GetChild(i).gameObject);
                    }
                }

                RectTransform check = CheckBadge("Check", _claimed);
                UiKit.PlaceBox(check, Box.FromCenter(row.Left + (row.Height * 0.55f), row.CenterY, icon * 0.8f, icon * 0.8f), row);
                UiKit.PlaceBox(_claimedLabel.rectTransform, new Box(row.Left + row.Height, row.Top + (row.Height * 0.2f), row.Right - (row.Height * 0.3f), row.Bottom - (row.Height * 0.2f)), row);
                return;
            }

            _amount.text = Loc.F("album.reward_amount", NumberText.Group(_album.RewardPetals));
            UiKit.PlaceBox(_petal.rectTransform, Box.FromCenter(row.Left + (row.Height * 0.6f), row.CenterY, icon, icon), row);
            UiKit.PlaceBox(_amount.rectTransform, new Box(row.Left + (row.Height * 1.05f), row.Top + (row.Height * 0.2f), row.Left + (row.Width * 0.45f), row.Bottom - (row.Height * 0.2f)), row);
            UiKit.PlaceBox((RectTransform)_claim.transform, _r.ClaimButton, row);
            _claim.interactable = _album.CanPay;
        }

        /// <summary>Places the page for the current slide (a turn's snap) and lift (the sheet over the chosen object).</summary>
        private void Layout()
        {
            if (_r == null)
            {
                return;
            }

            AlbumRegions r = _r;
            float slide = _turnStep == 0 ? 0f : AlbumLook.Snap(Time.unscaledTime - _turnAt) * _turnStep * r.Step;
            UiKit.PlaceScreen(_band, new Box(r.Safe.Left, r.Ribbon.Bottom, r.Safe.Right, r.Strip.Top));
            UiKit.PlaceScreen(_plate.rectTransform, r.Ribbon);
            UiKit.PlaceScreen(_plateLabel.rectTransform, r.Ribbon.Inset(r.W * 0.03f));
            UiKit.PlaceScreen((RectTransform)_previous.transform, r.Previous);
            UiKit.PlaceScreen((RectTransform)_next.transform, r.Next);
            UiKit.PlaceScreen(_strip, r.Strip);
            UiKit.PlaceScreen(_count.rectTransform, r.CountLine);
            UiKit.PlaceScreen(_status.rectTransform, r.StatusLine);
            UiKit.PlaceScreen(_sceneLine.rectTransform, r.SceneLine);
            float lift = 1f;
            CardView? centre = _cardViews[1];
            if (_sheetObject != null && centre != null)
            {
                AlbumObject? item = centre.Scene.Find(_sheetObject);
                if (item != null)
                {
                    Box layer = AlbumLook.LayerBox(r.Scene, centre.Scene.CanvasWidth, item.X, item.Y, item.Width, item.Height);
                    lift = AlbumLook.LiftAt(AlbumLook.Lift(r.Card, layer.Bottom, _s.Top, r.W), Time.unscaledTime - _sheetAt);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                CardView? card = _cardViews[i];
                if (card == null)
                {
                    continue;
                }

                Box cardBox = r.CardAt(i - 1, slide);
                Box sceneBox = r.SceneAt(i - 1, slide);
                if (i == 1 && lift < 1f)
                {
                    cardBox = Scale(cardBox, lift, r.Card.CenterX, r.Card.Top);
                    sceneBox = Scale(sceneBox, lift, r.Card.CenterX, r.Card.Top);
                }

                card.Place(cardBox, sceneBox, r.CompleteBadge(cardBox), _album.View(card.Scene).Complete, r.W * AlbumLook.OutlineShare);
            }

            if (_sheetObject != null)
            {
                float rise = AlbumLook.LiftAt(0f, Time.unscaledTime - _sheetAt);
                (float w, float h, Insets _) = UiKit.ScreenFrame();
                _rise.anchoredPosition = new Vector2(0f, -(rise * (h - _s.Top)) / UiKit.PixelsPerUnit);
                _scrim.color = UiTheme.Of(C.SurfaceScrim.WithAlpha(0.3f * (1f - rise)));
            }
        }

        private static Box Scale(Box b, float s, float cx, float top) =>
            new Box(cx + ((b.Left - cx) * s), top + ((b.Top - top) * s), cx + ((b.Right - cx) * s), top + ((b.Bottom - top) * s));

        private void Update()
        {
            if (_petals != null && _header.Petals != null)
            {
                long now = _petals();
                if (now != _shownPetals)
                {
                    _header.Petals.Show(now, _store && (_navLook == null || _navLook().Store));
                    _shownPetals = now;
                }
            }

            if (!_page.gameObject.activeSelf)
            {
                return;
            }

            float since = Time.unscaledTime - _turnAt;
            float sheetSince = Time.unscaledTime - _sheetAt;
            if (since < AlbumLook.SnapSeconds + 0.05f || (_sheetObject != null && sheetSince < 1f))
            {
                Layout();
            }

            // The colour's return of the new reveals, and the chosen object's golden outline.
            CardView? centre = _cardViews[1];
            if (centre != null)
            {
                float shown = Time.unscaledTime - _shownAt;
                AlbumSceneView view = _album.View(centre.Scene);
                foreach (AlbumObjectView item in view.Objects)
                {
                    int reveal = IndexOf(_reveals, item.Item.Id);
                    float colour = !item.Open ? 0f : reveal >= 0 ? AlbumLook.Reveal(shown, reveal) : 1f;
                    centre.SetColour(item.Item.Id, colour, item.Item.Id == _sheetObject ? AlbumLook.OutlineAlpha(sheetSince) : 0f);
                }
            }
        }

        private void OnSceneTap(Vector2 screen)
        {
            CardView? centre = _cardViews[1];
            if (centre == null || _sheetObject != null || Time.unscaledTime - _turnAt < AlbumLook.SnapSeconds)
            {
                return;
            }

            (float _, float h, Insets _) = UiKit.ScreenFrame();
            var boxes = new List<Box>();
            foreach (AlbumObject item in centre.Scene.Objects)
            {
                boxes.Add(AlbumLook.LayerBox(_r.Scene, centre.Scene.CanvasWidth, item.X, item.Y, item.Width, item.Height));
            }

            // Unity's screen points run from the bottom; the kit's from the top.
            int hit = AlbumLook.HitObject(boxes, i => AlbumArt.Alpha(centre.Scene.Objects[i].Layer), screen.x, h - screen.y);
            if (hit >= 0)
            {
                OpenSheet(centre.Scene.Objects[hit].Id);
            }
        }

        private void Claim()
        {
            IReadOnlyList<AlbumScene> shown = _album.Shown;
            if (_index < shown.Count && _album.Claim(shown[_index]) > 0)
            {
                ShowReward(_album.View(shown[_index]));
            }
        }

        // ---- The object sheet ----

        private void BuildSheet(Transform parent)
        {
            _sheet = UiFactory.Stretch(UiFactory.CreateRect("ObjectSheet", parent));
            _scrim = UiFactory.CreateImage("Scrim", _sheet, null, UiTheme.Of(C.SurfaceScrim.WithAlpha(0.3f)), raycast: true);
            UiFactory.Stretch(_scrim.rectTransform);
            UiKit.TapTarget(_scrim, CloseSheet);
            _rise = UiFactory.Stretch(UiFactory.CreateRect("Rise", _sheet));
            _panel = UiKit.CardFrame("Panel", _rise, true);
            // A swipe down on the sheet closes it (TouchGesture.PageStep: down is the previous page).
            UiKit.Scrolls(_panel.gameObject, step => { if (step < 0) CloseSheet(); });
            _grip = UiKit.RoundRect("Grip", _rise, UiTheme.Of(C.ParchmentEdge.Darken(0.12f)), b => b.Height / 2f);
            _well = UiKit.RoundRect("Well", _rise, UiTheme.Of(C.ParchmentWell), b => b.Width * 0.1f);
            _preview = new GameObject("Preview", typeof(RectTransform)).AddComponent<RawImage>();
            _preview.transform.SetParent(_rise, false);
            _preview.raycastTarget = false;
            _sheetName = MetaShelfView.Label("Name", _rise, string.Empty, ProfileGameDesign.Name);
            _sheetName.alignment = TextAlignmentOptions.Left;
            _lockIcon = UiKit.ShapeImage("Lock", _rise, "ui.lock", C.InkBrownSoft);
            _openCheck = CheckBadge("Check", _rise);
            _line1 = MetaShelfView.Label("Line1", _rise, string.Empty, ProfileGameDesign.Info);
            _line1.alignment = TextAlignmentOptions.Left;
            _line2 = MetaShelfView.Label("Line2", _rise, string.Empty, ProfileGameDesign.Info);
            _line2.alignment = TextAlignmentOptions.Left;
            _continueButton = UiKit.PrimaryButton("Continue", _rise, Loc.T("album.continue"), () =>
            {
                CloseSheet();
                Hide();
                _continue?.Invoke();
            });
            _close = (RectTransform)UiKit.RoundIconButton("Close", _rise, "ui.close", CloseSheet).transform;
            _sheet.gameObject.SetActive(false);
        }

        private void OpenSheet(string id)
        {
            CardView? centre = _cardViews[1];
            AlbumObjectView? item = centre == null ? null : _album.View(centre.Scene).Find(id);
            if (item == null)
            {
                return;
            }

            _sheetObject = id;
            _sheetAt = Time.unscaledTime;
            AlbumSheetRegions s = _s;
            _sheet.gameObject.SetActive(true);
            _sheet.SetAsLastSibling();
            UiKit.PlaceScreen(_panel.rectTransform, s.Panel);
            UiKit.PlaceScreen(_grip.rectTransform, s.Grip);
            UiKit.PlaceScreen(_well.rectTransform, s.Preview);
            Texture2D? picture = AlbumArt.Picture(item.Item.Layer);
            _preview.texture = picture;
            _preview.gameObject.SetActive(picture != null);
            if (picture != null)
            {
                UiKit.PlaceScreen(_preview.rectTransform, AlbumLook.Fit(s.Preview.Inset(s.Preview.Width * 0.1f), picture.width, picture.height));
            }

            _sheetName.text = Loc.T(item.Item.TitleKey);
            UiKit.PlaceScreen(_sheetName.rectTransform, s.Name);
            UiKit.PlaceScreen(_line1.rectTransform, s.Line1);
            UiKit.PlaceScreen(_line2.rectTransform, s.Line2);
            UiKit.PlaceScreen(_lockIcon.rectTransform, s.Icon1.Inset(s.Icon1.Width * 0.12f));
            UiKit.PlaceScreen(_openCheck, s.Icon1.Inset(s.Icon1.Width * 0.1f));
            UiKit.PlaceScreen((RectTransform)_continueButton.transform, s.Button);
            UiKit.PlaceScreen(_close, s.Close);
            _lockIcon.gameObject.SetActive(!item.Open);
            _openCheck.gameObject.SetActive(item.Open);
            _line2.gameObject.SetActive(!item.Open);
            _continueButton.gameObject.SetActive(!item.Open);
            _line1.text = item.Open
                ? Loc.F("album.level_completed", NumberText.Group(item.Level ?? 0))
                : item.Level.HasValue ? Loc.F("album.unlocks_after", NumberText.Group(item.Level.Value)) : Loc.T("album.coming_soon");
            _line2.text = Loc.F("album.next_level", NumberText.Group(_album.NextLevel));
            Layout();
        }

        private void CloseSheet()
        {
            _sheetObject = null;
            _sheet.gameObject.SetActive(false);
            Layout();
        }

        private static RectTransform CheckBadge(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = UiKit.Element(name, parent);
            UiKit.CheckBadge(layout, b => b);
            return root;
        }

        private static int IndexOf(IReadOnlyList<string> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == id)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>One scene card: the wooden frame, the scene's layers in a clip, the outline copies and the complete badge.</summary>
        private sealed class CardView
        {
            private readonly Image _frame;
            private readonly RectTransform _clip;
            private readonly RectTransform _check;
            private readonly Dictionary<string, (RawImage Locked, RawImage Colour, RawImage[] Outline, Box Box)> _layers = new Dictionary<string, (RawImage, RawImage, RawImage[], Box)>();

            public CardView(Transform parent, AlbumScene scene, Action<Vector2>? tap)
            {
                Scene = scene;
                _frame = UiKit.CardFrame("Card-" + scene.Id, parent, false);
                Image clip = UiFactory.CreateImage("Scene-" + scene.Id, parent, null, Color.white, raycast: tap != null);
                _clip = clip.rectTransform;
                clip.gameObject.AddComponent<RectMask2D>();
                clip.color = UiTheme.Of(C.CreamTop);
                if (tap != null)
                {
                    clip.gameObject.AddComponent<AlbumSceneTap>().Tapped = tap;
                }

                var sceneBox = new Box(0f, 0f, scene.CanvasWidth, scene.CanvasHeight);
                Texture2D? background = AlbumArt.Picture(scene.Background);
                if (background != null)
                {
                    RawImage image = Raw("Background", _clip, background);
                    UiKit.PlaceBox(image.rectTransform, sceneBox, sceneBox);
                }

                foreach (AlbumObject item in scene.Objects)
                {
                    var box = new Box(item.X, item.Y, item.X + item.Width, item.Y + item.Height);
                    var outline = new RawImage[8];
                    Texture2D? silhouette = AlbumArt.Silhouette(item.Layer);
                    for (int i = 0; i < 8; i++)
                    {
                        outline[i] = Raw("Outline" + i, _clip, silhouette);
                        outline[i].color = UiTheme.Of(C.MedalGold);
                        outline[i].gameObject.SetActive(false);
                    }

                    RawImage locked = Raw("Locked-" + item.Id, _clip, AlbumArt.LockedPicture(item.Layer));
                    RawImage colour = Raw("Colour-" + item.Id, _clip, AlbumArt.Picture(item.Layer));
                    UiKit.PlaceBox(locked.rectTransform, box, sceneBox);
                    UiKit.PlaceBox(colour.rectTransform, box, sceneBox);
                    _layers[item.Id] = (locked, colour, outline, box);
                }

                _check = CheckBadge("Complete", parent);
            }

            public AlbumScene Scene { get; }

            public void Place(Box card, Box scene, Box badge, bool complete, float outline)
            {
                UiKit.PlaceScreen(_frame.rectTransform, card);
                UiKit.PlaceScreen(_clip, scene);
                UiKit.PlaceScreen(_check, badge);
                _check.gameObject.SetActive(complete);
                var canvas = new Box(0f, 0f, Scene.CanvasWidth, Scene.CanvasHeight);
                float k = Scene.CanvasWidth / Math.Max(1f, scene.Width);
                foreach (var layer in _layers.Values)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        double a = i * Math.PI / 4.0;
                        UiKit.PlaceBox(layer.Outline[i].rectTransform, layer.Box.Offset((float)Math.Cos(a) * outline * k, (float)Math.Sin(a) * outline * k), canvas);
                    }
                }
            }

            /// <summary>An object's colour (0 locked, 1 open) and its golden outline's alpha (0: none).</summary>
            public void SetColour(string id, float colour, float outline)
            {
                if (!_layers.TryGetValue(id, out var layer))
                {
                    return;
                }

                layer.Locked.gameObject.SetActive(colour < 1f);
                layer.Colour.gameObject.SetActive(colour > 0f);
                layer.Colour.color = new Color(1f, 1f, 1f, colour);
                foreach (RawImage copy in layer.Outline)
                {
                    copy.gameObject.SetActive(outline > 0f);
                    Color gold = UiTheme.Of(C.MedalGold);
                    copy.color = new Color(gold.r, gold.g, gold.b, outline);
                }
            }

            public void Destroy()
            {
                foreach (GameObject part in new[] { _frame.gameObject, _clip.gameObject, _check.gameObject })
                {
                    part.SetActive(false);
                    UnityEngine.Object.Destroy(part);
                }
            }

            private static RawImage Raw(string name, Transform parent, Texture2D? texture)
            {
                RawImage image = new GameObject(name, typeof(RectTransform)).AddComponent<RawImage>();
                image.transform.SetParent(parent, false);
                image.texture = texture;
                image.raycastTarget = false;
                return image;
            }
        }
    }

    /// <summary>The scene's tap (spec 007): its screen point goes to the album, which picks the object by its layer's alpha. A drag never clicks.</summary>
    public sealed class AlbumSceneTap : MonoBehaviour, IPointerClickHandler
    {
        public Action<Vector2>? Tapped { get; set; }

        public void OnPointerClick(PointerEventData e) => Tapped?.Invoke(e.position);
    }
}
