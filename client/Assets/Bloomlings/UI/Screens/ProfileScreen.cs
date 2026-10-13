using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Approved garden Profile: a portrait arch and a three-shelf medal showcase, without a main menu.
    /// The four full-page editor tabs share a fixed preview and navigation; current profile services own all state.</summary>
    public sealed class ProfileScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private PageHeaderView _header = null!;
        private Image _card = null!;
        private ProfileAvatar _avatar = null!;
        private RectTransform _avatarButton = null!;
        private TextMeshProUGUI _name = null!;
        private RectTransform _edit = null!;
        private TextMeshProUGUI _id = null!;
        private TextMeshProUGUI _joined = null!;
        private TextMeshProUGUI _plaque = null!;
        private TextMeshProUGUI _title = null!;
        private readonly RectTransform[] _tiles = new RectTransform[ReferenceProfileRegions.AchievementCount];
        private readonly TextMeshProUGUI[] _tileLabels = new TextMeshProUGUI[ReferenceProfileRegions.AchievementCount];
        private readonly (Image Badge, TextMeshProUGUI Count, GameObject Lock, GameObject Check, Image Progress)[] _tileParts = new (Image, TextMeshProUGUI, GameObject, GameObject, Image)[ReferenceProfileRegions.AchievementCount];
        private TextMeshProUGUI _note = null!;
        private RectTransform _previous = null!;
        private RectTransform _next = null!;
        private PlayerSave _save = null!;
        private Action? _onChanged;
        private int _page;
        private string? _message;
        private ProfileEditCard _editCard = null!;
        private ProfileService _profile = null!;
        private WardrobeService _wardrobe = null!;
        private Func<int> _level = () => 1;
        private Func<Family, Outfit>? _outfitOf;
        private Func<long>? _petalsSource;
        private Func<HomeLook>? _navLook;
        private bool _store;
        private bool _plus;
        private long _petalsShown = -1;

        public bool IsOpen => _root.activeSelf;

        /// <param name="level">The level Home shows (the plaque's).</param>
        /// <param name="wardrobeLevel">The level named if a future profile-item tab has no early choice.</param>
        /// <param name="outfitOf">What the avatar's hero wears while its picture is missing (once the Wardrobe is open).</param>
        /// <param name="petals">The Petals balance for the header's pill; null hides the pill.</param>
        /// <param name="onStore">Opens the Store page from the pill's "+" (once the Store is open); null shows no "+".</param>
        /// <param name="navLook">Tells whether the Store is open (<see cref="HomeLook.Store"/>) for the pill's "+".</param>
        /// <param name="onChanged">The profile changed (Home redraws its avatar).</param>
        public static ProfileScreen Create(
            Transform parent,
            ProfileService profile,
            WardrobeService wardrobe,
            PlayerSave save,
            Func<int> level,
            int wardrobeLevel,
            Func<Family, Outfit>? outfitOf = null,
            Func<long>? petals = null,
            Action? onStore = null,
            Func<HomeLook>? navLook = null,
            Action? onChanged = null)
        {
            // A full screen over Home that takes every tap, on the Wardrobe's garden, as the other pages.
            Image shade = UiFactory.CreateImage("Profile", parent, null, Color.clear, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var screen = shade.gameObject.AddComponent<ProfileScreen>();
            screen._root = shade.gameObject;
            screen._profile = profile;
            screen._wardrobe = wardrobe;
            screen._save = save;
            screen._onChanged = onChanged;
            screen._level = level;
            screen._outfitOf = outfitOf;
            screen._petalsSource = petals;
            screen._store = onStore != null;
            screen._navLook = navLook;
            Transform root = shade.transform;
            BackdropView.Create(shade.rectTransform, OwnerPictures.Wardrobe, BackdropScene.Home);

            // Preserve the reference skin's silhouette: uniform scale, no slicing of the arch or shelves.
            screen._card = UiFactory.CreateImage("Showcase",root,ShopArt.Get(ProfileGameDesign.Showcase),Color.white);
            screen._avatar = ProfileAvatar.Create("Avatar", root,sceneFrame:true);
            Image avatarHit = UiFactory.CreateImage("AvatarButton", root, null, Color.clear, raycast: true);
            UiKit.TapTarget(avatarHit, () => screen._editCard.Show(ProfileTab.Avatar), press: true);
            screen._avatarButton = avatarHit.rectTransform;
            screen._name = ProfileGameView.SceneLabel("Name", root, string.Empty, ProfileGameDesign.Name,ShopBodyDesign.Letters);
            screen._edit=ProfileGameView.SceneButton("Edit",root,()=>screen._editCard.Show(ProfileTab.Name));
            screen._id = ProfileGameView.SceneLabel("Id",root,string.Empty,ProfileGameDesign.Info);
            screen._joined = ProfileGameView.SceneLabel("Joined",root,string.Empty,ProfileGameDesign.Info);
            screen._plaque = ProfileGameView.SceneLabel("Level",root,Loc.F("common.level",1),ProfileGameDesign.Name,ShopBodyDesign.Letters);

            // Nine illustrated achievement tiles at a time; the standard page arrows page through the full catalog.
            screen._title = ProfileGameView.SceneLabel("Achievements",root,Loc.T("profile.achievements"),ProfileGameDesign.Name,ShopBodyDesign.Letters);
            screen._previous = ProfileGameView.SceneButton("PreviousAchievements",root,()=>screen.ChangePage(-1),false);
            screen._next = ProfileGameView.SceneButton("NextAchievements",root,()=>screen.ChangePage(1),true);
            Box Badge(Box b) => ProfileGameDesign.AchievementMark(b);
            for (int i = 0; i < screen._tiles.Length; i++)
            {
                Image hit = UiFactory.CreateImage("Achievement" + i, root, null, Color.clear, raycast: true);
                int index = i;
                UiKit.TapTarget(hit, () => screen.SelectAchievement(index), press: true);
                RectTransform tile = hit.rectTransform;
                Image well = UiFactory.CreateImage("Contents",tile,null,Color.clear);
                Image trophy = UiFactory.CreateImage("Badge", well.transform, null, Color.white);
                trophy.preserveAspect = true;
                trophy.raycastTarget = false;
                Image capsule=UiFactory.CreateImage("Progress",well.transform,null,Color.white);
                PictureFit.On(capsule,(w,h)=>ProceduralSprites.Picture("profile.progress",w,h,(pw,ph)=>ShopCatalogDesign.ActionSurface(pw,ph,false)));
                BoxLayout.On(capsule.rectTransform).Then(b=>{if(capsule.type==Image.Type.Sliced)capsule.pixelsPerUnitMultiplier=190/Mathf.Max(.01f,b.Height*.4f);});
                TextMeshProUGUI count=ProfileGameView.SceneLabel("Count",capsule.transform,"",ProfileGameDesign.Progress);UiFactory.Stretch(count.rectTransform);
                RectTransform badge = UiKit.LockBadge("Lock", well.transform);
                RectTransform check = UiFactory.Stretch(UiFactory.CreateRect("Check", well.transform));
                UiKit.CheckBadge(BoxLayout.On(check), Badge);
                TextMeshProUGUI label = ProfileGameView.SceneLabel("Label", tile, string.Empty, ProfileGameDesign.Item);
                BoxLayout.On(tile)
                    .Add(well.rectTransform, b=>b)
                    .Add(label.rectTransform, ProfileGameDesign.AchievementLabel);
                BoxLayout.On(well.rectTransform)
                    .Add(trophy.rectTransform, ProfileGameDesign.AchievementPicture)
                    .Add(capsule.rectTransform, ProfileGameDesign.AchievementCount)
                    .Add(badge, Badge);
                screen._tiles[i] = tile;
                screen._tileLabels[i] = label;
                screen._tileParts[i] = (trophy, count, badge.gameObject, check.gameObject, capsule);
            }

            screen._note = ProfileGameView.SceneLabel("Note", root, Loc.F("profile.achievements_page", 1,
                (Achievements.All.Count + ReferenceProfileRegions.AchievementCount - 1) / ReferenceProfileRegions.AchievementCount), ProfileGameDesign.Info);

            // The header stays outside the content; the editor covers this page.

            screen._header = UiKit.PageHeader(root, Loc.T("profile.title"), screen.Hide, petals != null, onStore);
            screen._editCard = ProfileEditCard.Create(root, profile, wardrobe, wardrobeLevel, () =>
            {
                screen.Refresh();
                onChanged?.Invoke();
            },level,petals,onStore,()=>screen._store&&(screen._navLook==null||screen._navLook().Store));
            shade.gameObject.SetActive(false);
            return screen;
        }

        public void Show()
        {
            _root.SetActive(true);
            _page = 0;
            _message = null;
            _plus = _store && (_navLook == null || _navLook().Store);
            _petalsShown = -1;
            Layout();
            Refresh();
        }

        public void Hide()
        {
            _editCard.Hide();
            _root.SetActive(false);
        }

        /// <summary>The name the page shows: the chosen one, else "Gardener 4821".</summary>
        public static string NameOf(ProfileService profile) => profile.Name ?? Loc.F("profile.default_name", profile.DefaultNumber);

        private void Refresh()
        {
            AvatarItem avatar = _profile.Avatar;
            _avatar.Show(_wardrobe.Profile with { Marker = null }, _wardrobe.IsAvailable ? _outfitOf?.Invoke(avatar.Family) : null, avatar);
            _name.text = NameOf(_profile);
            _id.text = Loc.F("profile.id", _profile.ShortId);
            _joined.text = Loc.F("profile.joined", _profile.JoinedMonth);
            _plaque.text = Loc.F("common.level", NumberText.Group(_level()));
            IReadOnlyList<AchievementState> achievements = Achievements.Of(_save);
            for (int i = 0; i < _tileParts.Length; i++)
            {
                int index = _page * _tileParts.Length + i;
                _tiles[i].gameObject.SetActive(index < achievements.Count);
                if (index >= achievements.Count) continue;
                AchievementState state = achievements[index];
                (Image trophy, TextMeshProUGUI count, GameObject padlock, GameObject check, Image capsule) = _tileParts[i];
                PictureFit.On(capsule,(w,h)=>state.Complete ? ShopArt.Get("shop/price-button")! : ProceduralSprites.Picture("profile.progress",w,h,(pw,ph)=>ShopCatalogDesign.ActionSurface(pw,ph,false)));
                capsule.type=state.Complete ? Image.Type.Sliced : Image.Type.Simple;
                capsule.color=state.Complete ? Color.white : Color.clear;
                UiKit.Style(count,ProfileGameDesign.Progress,state.Complete ? ShopBodyDesign.MoneyLetters : ProfileGameDesign.Ink);
                BoxLayout.On(count.rectTransform).Apply();
                trophy.sprite = OwnerArt.Badge(state.Def.Id) ?? ProceduralSprites.Shape("ui.trophy");
                trophy.color = state.Tier == 0 ? new Color(1f, 1f, 1f, 0.58f) : Color.white;
                count.text = state.Complete ? NumberText.Group(state.Value) : Loc.F("profile.achievement_progress", NumberText.Group(state.Shown), NumberText.Group(state.Goal));
                padlock.SetActive(state.Tier == 0);
                check.SetActive(state.Complete);
                _tileLabels[i].text = Loc.T(state.Def.NameKey);
                _tileLabels[i].color = UiTheme.Of(state.Tier == 0 ? C.InkBrownSoft : C.InkBrown);
            }
            _note.text = _message ?? Loc.F("profile.achievements_page", _page + 1,
                (achievements.Count + _tileParts.Length - 1) / _tileParts.Length);
        }

        private void ChangePage(int direction)
        {
            int pages = (Achievements.All.Count + _tileParts.Length - 1) / _tileParts.Length;
            _page = (_page + direction + pages) % pages;
            _message = null;
            Refresh();
        }

        private void SelectAchievement(int tile)
        {
            IReadOnlyList<AchievementState> states = Achievements.Of(_save);
            int index = _page * _tileParts.Length + tile;
            if (index >= states.Count) return;
            AchievementState state = states[index];
            if (state.Tier > 0 && _wardrobe.Show(state.Def.BadgeId))
            {
                _message = Loc.T("profile.badge_equipped");
                _onChanged?.Invoke();
            }
            else _message = Loc.T("profile.achievement_help." + state.Def.Id);
            Refresh();
        }

        private void Update()
        {
            // The balance follows the economy (an avatar bought in the edit card, a purchase in the Store).
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

        /// <summary>Places the page on the kit's regions for the screen's shape.</summary>
        private void Layout()
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            ReferenceProfileRegions r = ScreenLayout.ReferenceProfile(w, h, insets);
            _header.Place(r.Header);
            UiKit.PlaceScreen(_card.rectTransform, r.Panel);
            UiKit.PlaceScreen(_avatar.Rect, r.Avatar);
            UiKit.PlaceScreen(_avatarButton, ProfileGameDesign.AvatarTouch(r.Avatar));
            UiKit.PlaceScreen(_name.rectTransform, r.Name);
            UiKit.PlaceScreen(_edit, r.Edit);
            UiKit.PlaceScreen(_id.rectTransform, r.Id);
            UiKit.PlaceScreen(_joined.rectTransform, r.Joined);
            UiKit.PlaceScreen(_plaque.rectTransform, r.Plaque);
            UiKit.PlaceScreen(_title.rectTransform,ProfileGameDesign.ShowcaseTitle(r));
            UiKit.PlaceScreen(_previous,ProfileGameDesign.Previous(r));
            UiKit.PlaceScreen(_next,ProfileGameDesign.Next(r));
            for (int i = 0; i < _tiles.Length; i++)
            {
                UiKit.PlaceScreen(_tiles[i], r.Achievements[i]);
            }

            UiKit.PlaceScreen(_note.rectTransform, r.AchievementsNote);
        }
    }
}
