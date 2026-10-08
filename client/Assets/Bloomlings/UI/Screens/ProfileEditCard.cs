using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The profile's "Edit profile" card (spec 005 FR-037; the playtest's <c>ProfileScreen.Edit</c>), placed from
    /// <see cref="ScreenLayout.ProfileEdit"/> and run by the engine-free <see cref="ProfileEditor"/>: the tabs (Avatar,
    /// Frame, Badge, Name), the preview (the picked avatar in the picked frame and badge, the name beside it, a status line
    /// under it), the tab's grid and the main button. Avatar lists the 14 avatars, free ones first, the others with their
    /// price, the picked one checked; Frame and Badge list the owned ones on the avatar (locked until the Wardrobe opens);
    /// Name holds a text field (<c>TMP_InputField</c>; the system keyboard on a phone). The button saves every choice and
    /// closes the card, or buys the picked avatar at its price ("Buy for 600").
    /// </summary>
    public sealed class ProfileEditCard : MonoBehaviour
    {
        private readonly List<Cell> _cells = new List<Cell>();
        private GameObject _root = null!;
        private TabsView _tabs = null!;
        private ProfileAvatar _preview = null!;
        private TextMeshProUGUI _previewName = null!;
        private TextMeshProUGUI _status = null!;
        private Image _field = null!;
        private TMP_InputField _input = null!;
        private TextMeshProUGUI _hint = null!;
        private TextMeshProUGUI _note = null!;
        private RectTransform _lock = null!;
        private TextMeshProUGUI _buttonLabel = null!;
        private ProfileService _profile = null!;
        private WardrobeService _wardrobe = null!;
        private ProfileEditor? _editor;
        private int _wardrobeLevel;
        private Action _changed = () => { };
        private float _statusUntil = -1f;

        public bool IsOpen => _root.activeSelf;

        /// <param name="wardrobeLevel">The level the Wardrobe opens at (the locked Frame and Badge tabs say it).</param>
        /// <param name="changed">Something was kept or bought (the page and Home redraw the avatar).</param>
        public static ProfileEditCard Create(Transform parent, ProfileService profile, WardrobeService wardrobe, int wardrobeLevel, Action changed)
        {
            ProfileEditCard screen = null!;
            Box screenBox = UiKit.ScreenBox();
            ProfileEditRegions r = ScreenLayout.ProfileEdit(screenBox.Width, screenBox.Height, UiKit.ScreenFrame().Insets);
            CardView card = UiKit.Card("ProfileEdit", parent, Loc.T("profile.edit_title"), r.ContentUnits, () => screen.Hide());
            screen = card.Root.AddComponent<ProfileEditCard>();
            screen._root = card.Root;
            screen._profile = profile;
            screen._wardrobe = wardrobe;
            screen._wardrobeLevel = wardrobeLevel;
            screen._changed = changed;
            Box body = card.Regions.Body;
            Transform at = card.Body;

            // The tabs.
            var labels = new string[ProfileEditor.Tabs.Count];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = Loc.T(TabKey(ProfileEditor.Tabs[i]));
            }

            RectTransform tabs = UiFactory.CreateRect("Tabs", at);
            screen._tabs = UiKit.Tabs("Tabs", tabs, labels, i => screen.SelectTab(ProfileEditor.Tabs[i]));
            UiKit.PlaceBox(tabs, r.Tabs, body);

            // The preview: the picked avatar and the name, a status line under the name.
            screen._preview = ProfileAvatar.Create("Preview", at);
            UiKit.PlaceBox(screen._preview.Rect, r.Preview, body);
            screen._previewName = UiKit.Label("Name", at, string.Empty, T.Title, UiTheme.Of(C.InkTitle), TextAlignmentOptions.Left, TextLook.Plain(C.InkTitle));
            Box name = r.PreviewName;
            UiKit.PlaceBox(screen._previewName.rectTransform, new Box(name.Left, name.Top, name.Right, name.CenterY + (name.Height * 0.15f)), body);
            screen._status = UiKit.Label("Status", at, string.Empty, T.Caption, UiTheme.Of(C.InkBrownSoft), TextAlignmentOptions.Left);
            UiKit.PlaceBox(screen._status.rectTransform, new Box(name.Left, name.CenterY + (name.Height * 0.15f), name.Right, r.Preview.Bottom), body);

            // The grid's cells: a picture, a price pill and a check each.
            for (int i = 0; i < r.CellCount; i++)
            {
                screen._cells.Add(Cell.Create(at, r.Cell(i), body, screen.Pick, i));
            }

            // The Name tab: the field, its hint.
            screen._field = UiKit.Well("NameField", at, UiTheme.Of(C.CreamTop), raycast: true);
            UiKit.PlaceBox(screen._field.rectTransform, r.NameField, body);
            screen._input = NameInput(screen._field, screen);
            screen._hint = UiKit.Label("Hint", at, Loc.T("profile.name_hint"), T.Caption, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(screen._hint.rectTransform, new Box(r.NameButton.Left - (r.Grid.Width * 0.2f), r.NameButton.Top, r.NameButton.Right + (r.Grid.Width * 0.2f), r.NameHint.Bottom), body);

            // An empty or locked tab's note, with the padlock while locked.
            screen._note = UiKit.Label("Note", at, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(screen._note.rectTransform, r.Note, body);
            screen._lock = UiKit.LockBadge("Lock", at);
            UiKit.PlaceBox(screen._lock, r.NoteLock, body);

            // The main button: Save, or Buy at the picked avatar's price.
            Button button = UiKit.PrimaryButton("Confirm", at, Loc.T("profile.save"), screen.Confirm);
            UiKit.PlaceBox((RectTransform)button.transform, r.Button, body);
            screen._buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>();
            card.Root.SetActive(false);
            return screen;
        }

        /// <summary>Opens the card on <paramref name="tab"/> with the current choices.</summary>
        public void Show(ProfileTab tab)
        {
            _editor = new ProfileEditor(_profile, _wardrobe, tab);
            _input.SetTextWithoutNotify(_profile.Name ?? string.Empty);
            _statusUntil = -1f;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            SelectTab(tab);
        }

        public void Hide()
        {
            _root.SetActive(false);
            _editor = null;
        }

        private void Update()
        {
            if (_statusUntil > 0f && Time.unscaledTime > _statusUntil)
            {
                _status.text = string.Empty;
                _statusUntil = -1f;
            }
        }

        private void SelectTab(ProfileTab tab)
        {
            if (_editor == null)
            {
                return;
            }

            _editor.Tab = tab;
            _tabs.Select((int)tab);
            Redraw();
        }

        /// <summary>Shows the editor's state: the preview, the tab's grid or field, the button's label.</summary>
        private void Redraw()
        {
            ProfileEditor? editor = _editor;
            if (editor == null)
            {
                return;
            }

            _preview.Show(new ProfileLook(editor.Frame, editor.Badge, null), null, editor.Avatar);
            _previewName.text = editor.Name ?? Loc.F("profile.default_name", _profile.DefaultNumber);
            bool name = editor.Tab == ProfileTab.Name;
            _field.gameObject.SetActive(name);
            _hint.gameObject.SetActive(name);
            _note.gameObject.SetActive(false);
            _lock.gameObject.SetActive(false);
            for (int i = 0; i < _cells.Count; i++)
            {
                _cells[i].Hide();
            }

            if (editor.Tab == ProfileTab.Avatar)
            {
                IReadOnlyList<AvatarItem> avatars = _profile.Avatars;
                for (int i = 0; i < avatars.Count && i < _cells.Count; i++)
                {
                    AvatarItem avatar = avatars[i];
                    _cells[i].Show(avatar, null, null, avatar.Id == editor.AvatarId, _profile.Owns(avatar) ? (int?)null : _profile.Price(avatar));
                }
            }
            else if (!name)
            {
                CosmeticKind kind = editor.Tab == ProfileTab.Frame ? CosmeticKind.Frame : CosmeticKind.Badge;
                // The five free frames from Level 1, every owned frame and badge once the Wardrobe is open.
                IReadOnlyList<CosmeticItem> owned = editor.Owned(kind);
                bool locked = editor.IsLocked(kind);
                if (locked || owned.Count == 0)
                {
                    _note.gameObject.SetActive(true);
                    _lock.gameObject.SetActive(locked);
                    _note.text = locked
                        ? Loc.F("profile.badges_locked", _wardrobeLevel)
                        : Loc.T(kind == CosmeticKind.Frame ? "profile.no_frames" : "profile.no_badges");
                }

                for (int i = 0; i < owned.Count && i < _cells.Count; i++)
                {
                    CosmeticItem item = owned[i];
                    bool frame = kind == CosmeticKind.Frame;
                    _cells[i].Show(editor.Avatar, frame ? item : null, frame ? null : item, item.Id == (frame ? editor.FrameId : editor.BadgeId), null, framed: true);
                }
            }

            _buttonLabel.text = editor.Action == ProfileAction.Buy
                ? Loc.F("profile.buy", NumberText.Group(editor.Price))
                : Loc.T("profile.save");
        }

        /// <summary>A tap on a cell: picks its avatar, frame or badge.</summary>
        private void Pick(int index)
        {
            ProfileEditor? editor = _editor;
            if (editor == null)
            {
                return;
            }

            if (editor.Tab == ProfileTab.Avatar && index < _profile.Avatars.Count)
            {
                editor.PickAvatar(_profile.Avatars[index].Id);
            }
            else if (editor.Tab == ProfileTab.Frame || editor.Tab == ProfileTab.Badge)
            {
                IReadOnlyList<CosmeticItem> owned = editor.Owned(editor.Tab == ProfileTab.Frame ? CosmeticKind.Frame : CosmeticKind.Badge);
                if (index < owned.Count)
                {
                    if (editor.Tab == ProfileTab.Frame)
                    {
                        editor.PickFrame(owned[index].Id);
                    }
                    else
                    {
                        editor.PickBadge(owned[index].Id);
                    }
                }
            }

            Redraw();
        }

        /// <summary>The main button (<see cref="ProfileEditor.Confirm"/>).</summary>
        private void Confirm()
        {
            ProfileEditor? editor = _editor;
            if (editor == null)
            {
                return;
            }

            if (editor.Tab == ProfileTab.Name && !string.IsNullOrEmpty(_input.text) && !editor.EnterName(_input.text))
            {
                Say(Loc.T("profile.name_empty"));
                return;
            }

            if (editor.NeedsBuying)
            {
                // "Buy for N" asks the purchase confirmation first (spec 005 FR-040): the avatar is bought only on its Buy;
                // short Petals say so without asking.
                _confirm ??= UiKit.PurchaseCard(_root.transform.parent);
                AvatarItem avatar = editor.Avatar;
                long petals = App.AppServices.Current != null && App.AppServices.Current.TryGet(out Services.Economy.EconomyService? economy) && economy != null ? economy.Petals : editor.Price;
                _confirm.ShowPetals(
                    PurchaseOffer.ForPetals(Loc.T("purchase.avatar"), editor.Price),
                    petals,
                    well =>
                    {
                        ProfileAvatar picture = ProfileAvatar.Create("Avatar", well);
                        UiFactory.Stretch(picture.Rect);
                        picture.Show(null, null, avatar);
                    },
                    () => Finish(editor),
                    () => Say(Loc.T("gameplay.not_enough_petals")));
                return;
            }

            Finish(editor);
        }

        // The purchase confirmation of an avatar, made when first asked (spec 005 FR-040).
        private PurchaseCardView? _confirm;

        /// <summary>The main button's outcome, once a purchase is confirmed.</summary>
        private void Finish(ProfileEditor editor)
        {
            if (_editor != editor)
            {
                return;
            }

            switch (editor.Confirm())
            {
                case ProfileOutcome.Saved:
                    _changed();
                    Hide();
                    return;
                case ProfileOutcome.Bought:
                    GameFeedback.Current?.Play(SoundCue.PodDone);
                    Say(Loc.T("profile.bought"));
                    _changed();
                    break;
                case ProfileOutcome.NotEnoughPetals:
                    GameFeedback.Current?.Play(SoundCue.Refused);
                    Say(Loc.T("gameplay.not_enough_petals"));
                    break;
                default:
                    GameFeedback.Current?.Play(SoundCue.Refused);
                    Say(Loc.T("profile.name_empty"));
                    break;
            }

            Redraw();
        }

        private void Say(string text)
        {
            _status.text = text;
            _statusUntil = Time.unscaledTime + 1.6f;
        }

        /// <summary>
        /// The Name tab's text field on <paramref name="well"/>: a single line of up to
        /// <see cref="ProfileService.MaxNameLength"/> characters in <c>type.title</c>, the default name as its placeholder;
        /// its text goes to the editor as it is typed (kept on Save).
        /// </summary>
        private static TMP_InputField NameInput(Image well, ProfileEditCard screen)
        {
            RectTransform area = UiFactory.Stretch(UiFactory.CreateRect("TextArea", well.transform));
            area.offsetMin = new Vector2(UiKit.Units(24f), UiKit.Units(6f));
            area.offsetMax = new Vector2(-UiKit.Units(24f), -UiKit.Units(6f));
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI placeholder = UiKit.Label("Placeholder", area, Loc.F("profile.default_name", screen._profile.DefaultNumber), T.Title, UiTheme.Of(C.InkBrownSoft.WithAlpha(0.6f)));
            TextMeshProUGUI text = UiKit.Label("Text", area, string.Empty, T.Title, UiTheme.Of(C.InkBrown));
            text.enableAutoSizing = false;
            TMP_InputField input = well.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = well;
            input.characterLimit = ProfileService.MaxNameLength;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.onValueChanged.AddListener(value =>
            {
                if (screen._editor != null && screen._editor.EnterName(value))
                {
                    screen._previewName.text = screen._editor.Name ?? string.Empty;
                }
            });
            return input;
        }

        private static string TabKey(ProfileTab tab) => tab switch
        {
            ProfileTab.Avatar => "profile.tab_avatar",
            ProfileTab.Frame => "profile.tab_frame",
            ProfileTab.Badge => "profile.tab_badge",
            _ => "profile.tab_name",
        };

        /// <summary>
        /// One grid cell: the avatar disc (with a frame or badge, a little smaller so a drawn frame fits:
        /// <see cref="ProfileEditRegions.CellItemAvatar"/>), its price pill while it is to be bought, the green disc and the
        /// check when picked.
        /// </summary>
        private sealed class Cell
        {
            private readonly GameObject _root;
            private readonly Image _ring;
            private readonly ProfileAvatar _avatar;
            private readonly CostPillView _price;
            private readonly GameObject _check;
            private readonly Box _cell;

            private Cell(GameObject root, Image ring, ProfileAvatar avatar, CostPillView price, GameObject check, Box cell)
            {
                _root = root;
                _ring = ring;
                _avatar = avatar;
                _price = price;
                _check = check;
                _cell = cell;
            }

            public static Cell Create(Transform parent, Box cell, Box body, Action<int> onPick, int index)
            {
                Image hit = UiFactory.CreateImage("Cell" + index, parent, null, Color.clear, raycast: true);
                UiKit.PlaceBox(hit.rectTransform, cell, body);
                UiKit.TapTarget(hit, () => onPick(index), press: true);
                Box picture = ProfileEditRegions.CellPicture(cell);
                Image ring = UiKit.RoundRect("Picked", hit.transform, UiTheme.Of(GardenLook.Green.Face), GardenLook.IconRadius);
                UiKit.PlaceBox(ring.rectTransform, ProfileEditRegions.Picked(picture), cell);
                ProfileAvatar avatar = ProfileAvatar.Create("Avatar", hit.transform);
                UiKit.PlaceBox(avatar.Rect, picture, cell);
                CostPillView price = UiKit.CostPill("Price", hit.transform, Cost.Petals(0), button: true);
                UiKit.PlaceBox((RectTransform)price.transform, ProfileEditRegions.CellPrice(cell), cell);
                Box checkBox = ProfileEditRegions.CellCheck(cell);
                RectTransform check = UiFactory.CreateRect("Check", hit.transform);
                UiKit.PlaceBox(check, cell, cell);
                UiKit.CheckBadge(BoxLayout.On(check), b => Box.FromCenter(b.Left + (checkBox.CenterX - cell.Left), b.Top + (checkBox.CenterY - cell.Top), checkBox.Width, checkBox.Height));
                return new Cell(hit.gameObject, ring, avatar, price, check.gameObject, cell);
            }

            /// <param name="framed">A Frame or Badge cell: the avatar a little smaller, its frame inside the picture's box.</param>
            public void Show(AvatarItem avatar, CosmeticItem? frame, CosmeticItem? badge, bool picked, int? price, bool framed = false)
            {
                _root.SetActive(true);
                UiKit.PlaceBox(_avatar.Rect, framed ? ProfileEditRegions.CellItemAvatar(_cell) : ProfileEditRegions.CellPicture(_cell), _cell);
                _avatar.Show(new ProfileLook(frame, badge, null), null, avatar);
                _ring.gameObject.SetActive(picked);
                _check.SetActive(picked);
                _price.gameObject.SetActive(price.HasValue);
                if (price.HasValue)
                {
                    _price.SetCost(Cost.Petals(price.Value));
                }
            }

            public void Hide() => _root.SetActive(false);
        }
    }
}
