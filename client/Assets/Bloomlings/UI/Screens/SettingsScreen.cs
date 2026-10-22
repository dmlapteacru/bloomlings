using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Consent;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Art;
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
    /// <summary>The optional account section of Settings (FR-087, T138).</summary>
    /// <param name="Status">"Local profile", "Signed in" or the linked identity.</param>
    /// <param name="Link">Links a platform identity; <paramref name="Link"/>'s callback reports success.</param>
    public sealed record LevelActions(Func<int> Current, Action<int> Change);
    public sealed record PetalActions(Func<int> Current, Action<int> Change);

    public sealed record AccountActions(Func<string> Status, bool CanLinkApple, bool CanLinkGooglePlayGames, Action<LinkProvider, Action<bool>> Link, Action? Open = null);

    /// <summary>Sound and Haptics, optional account/privacy actions and retained level controls on one calm card.</summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        private const float RowUnits = 126f;
        private const float RowGapUnits = 12f;
        private const float StatusUnits = 100f;
        private const float ButtonGapUnits = 24f;

        private GameObject _root = null!;
        private SettingsData _settings = null!;
        private Action _persist = () => { };
        private readonly List<(ToggleView View, Func<bool> On)> _toggles = new List<(ToggleView, Func<bool>)>();
        private TextMeshProUGUI _sfx = null!;
        private TextMeshProUGUI _haptics = null!;
        private TextMeshProUGUI? _account;
        private Func<string>? _accountStatus;
        private TextMeshProUGUI? _levelLabel;
        private LevelActions? _levels;
        private TextMeshProUGUI? _petalLabel;
        private PetalActions? _petals;
        private Button _privacy = null!;
        private IConsentService? _consent;
        private GameObject _policyRoot=null!;

        public bool IsOpen => _root.activeSelf;

        /// <param name="consent">Shows the privacy options entry when the consent rules require one (FR-090).</param>
        public static SettingsScreen Create(
            Transform parent,
            SettingsData settings,
            Action persist,
            AccountActions? account = null,
            IConsentService? consent = null,
            LevelActions? levels = null,
            PetalActions? petals = null)
        {
            bool links = account != null && (account.CanLinkApple || account.CanLinkGooglePlayGames);
            float content = 12f + (2f * RowUnits) + RowGapUnits
                + (account != null ? ButtonGapUnits + StatusUnits : 0f)
                + (links || account?.Open != null ? 16f + DesignTokens.Size.SecondaryHeight : 0f)
                + (consent != null && consent.PrivacyOptionsRequired ? ButtonGapUnits + DesignTokens.Size.SecondaryHeight : 0f)
                + (levels != null ? 20f + DesignTokens.Size.TouchMin : 0f)
                + (petals != null ? 12f + DesignTokens.Size.TouchMin : 0f) + ButtonGapUnits + DesignTokens.Size.SecondaryHeight + 30f;

            SettingsScreen screen = null!;
            CardView view = UiKit.Card("SettingsScreen", parent, Loc.T("settings.title"), content, () => screen.Hide(), T.Title);
            GameObject root = view.Root;
            screen = root.AddComponent<SettingsScreen>();
            screen._root = root;
            screen._settings = settings;
            screen._persist = persist;
            screen._consent = consent; screen._levels=levels;screen._petals=petals;

            Box body = view.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            Box[] lines = ScreenLayout.Column(new Box(body.Left, body.Top + (12f * u), body.Right, body.Bottom), 2, RowUnits * u, RowGapUnits * u);
            screen._sfx=Toggle(view.Body,body,lines[0],"Sound","settings.sound",()=>settings.Sfx,()=>settings.Sfx=!settings.Sfx,screen,u);
            screen._haptics=Toggle(view.Body,body,lines[1],"Haptics","settings.haptics",()=>settings.Haptics,()=>settings.Haptics=!settings.Haptics,screen,u);
            float y=lines[1].Bottom;

            if (account != null)
            {
                y += ButtonGapUnits * u;
                screen._accountStatus = account.Status;
                screen._account = UiKit.Label("Account", view.Body, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft));
                UiKit.PlaceBox(screen._account.rectTransform, new Box(body.Left, y, body.Right, y + (StatusUnits * u)), body);
                y += StatusUnits * u;
                if (account.Open != null)
                {
                    y += 16f * u;
                    Button open = UiKit.SecondaryButton("AccountDetails", view.Body, Loc.T("account.panel.title"), account.Open);
                    UiKit.PlaceBox((RectTransform)open.transform, Pair(new Box(body.Left, y, body.Right, y + DesignTokens.Size.SecondaryHeight * u), 1, u)[0], body);
                    y += DesignTokens.Size.SecondaryHeight * u;
                }
                else if (links)
                {
                    y += 16f * u;
                    var row = new Box(body.Left, y, body.Right, y + (DesignTokens.Size.SecondaryHeight * u));
                    Box[] cells = Pair(row, account.CanLinkApple && account.CanLinkGooglePlayGames ? 2 : 1, u);
                    int cell = 0;
                    if (account.CanLinkApple)
                    {
                        Button apple = UiKit.SecondaryButton("LinkApple", view.Body, Loc.T("settings.link_apple"), () => account.Link(LinkProvider.Apple, _ => screen.Refresh()));
                        UiKit.PlaceBox((RectTransform)apple.transform, cells[cell++], body);
                    }

                    if (account.CanLinkGooglePlayGames)
                    {
                        Button google = UiKit.SecondaryButton("LinkGoogle", view.Body, Loc.T("settings.link_google"), () => account.Link(LinkProvider.GooglePlayGames, _ => screen.Refresh()));
                        UiKit.PlaceBox((RectTransform)google.transform, cells[cell], body);
                    }

                    y = row.Bottom;
                }
            }

            if(levels != null)
            {
                y+=20f*u; Box dev=new Box(body.Left,y,body.Right,y+DesignTokens.Size.TouchMin*u);
                screen._levelLabel=UiKit.Label("Level",view.Body,string.Empty,T.Caption,UiTheme.Of(C.InkBrownSoft));
                float left=dev.Left+dev.Width*.25f; UiKit.PlaceBox(screen._levelLabel.rectTransform,new Box(dev.Left,dev.Top,left,dev.Bottom),body);
                Box[] cells=ScreenLayout.Row(new Box(left,dev.Top,dev.Right,dev.Bottom),4,10*u,float.MaxValue,false);
                string[] labels={"−1","+1","+10","Reset"};int[] deltas={-1,1,10,0};
                for(int i=0;i<cells.Length;i++) { int delta=deltas[i]; Button action=UiKit.SecondaryButton("Level"+i,view.Body,labels[i],()=>{levels.Change(delta);screen.Refresh();},style:PopupDesign.Label);UiKit.PlaceBox((RectTransform)action.transform,cells[i],body); }
                y=dev.Bottom;
            }
            if(petals != null)
            {
                y+=12f*u;Box row=new Box(body.Left,y,body.Right,y+DesignTokens.Size.TouchMin*u);
                screen._petalLabel=UiKit.Label("Petals",view.Body,string.Empty,T.Caption,UiTheme.Of(C.InkBrownSoft));
                float left=row.Left+row.Width*.30f;UiKit.PlaceBox(screen._petalLabel.rectTransform,new Box(row.Left,row.Top,left,row.Bottom),body);
                Box[] cells=ScreenLayout.Row(new Box(left,row.Top,row.Right,row.Bottom),4,10*u,float.MaxValue,false);
                string[] labels={"−100","+100","+1000","Reset"};int[] deltas={-100,100,1000,0};
                for(int i=0;i<cells.Length;i++){int delta=deltas[i];Button action=UiKit.SecondaryButton("Petals"+i,view.Body,labels[i],()=>{petals.Change(delta);screen.Refresh();},style:PopupDesign.Label);UiKit.PlaceBox((RectTransform)action.transform,cells[i],body);}
                y=row.Bottom;
            }
            y += ButtonGapUnits * u;
            Box policyBox=Pair(new Box(body.Left,y,body.Right,y+DesignTokens.Size.SecondaryHeight*u),1,u)[0];
            Button policy=UiKit.SecondaryButton("PrivacyPolicy",view.Body,Loc.T("settings.privacy_policy"),()=>screen._policyRoot.SetActive(true));
            UiKit.PlaceBox((RectTransform)policy.transform,policyBox,body);y=policyBox.Bottom;
            screen._privacy=UiKit.SecondaryButton("PrivacyOptions",view.Body,Loc.T("settings.privacy"),()=>screen._consent?.ShowPrivacyOptions(screen.Refresh));
            UiKit.PlaceBox((RectTransform)screen._privacy.transform,Pair(new Box(body.Left,y+ButtonGapUnits*u,body.Right,y+(ButtonGapUnits+DesignTokens.Size.SecondaryHeight)*u),1,u)[0],body);
            CardView mock=UiKit.Card("PrivacyPolicyMock",root.transform,Loc.T("settings.privacy_policy"),180,()=>screen._policyRoot.SetActive(false),sign:SignDecor.None);
            TextMeshProUGUI message=UiKit.Label("Mock",mock.Body,Loc.T("settings.privacy_mock"),T.Body,UiTheme.Of(C.InkBrown),look:TextLook.Plain(C.InkBrown));message.textWrappingMode=TextWrappingModes.Normal;
            UiFactory.Place(message.rectTransform,.03f,.12f,.97f,.88f);screen._policyRoot=mock.Root;mock.Root.SetActive(false);

            root.SetActive(false);
            return screen;
        }

        public void Show()
        {
            _policyRoot.SetActive(false);
            Refresh();
            _root.SetActive(true);
        }

        public void Hide() { if(_policyRoot.activeSelf){_policyRoot.SetActive(false);return;} _root.SetActive(false); }

        private void Refresh()
        {
            _sfx.text = Loc.T("settings.sound_label");
            _haptics.text = Loc.T("settings.haptics_label");
            if(_levelLabel != null && _levels != null) _levelLabel.text="Level "+_levels.Current();
            if(_petalLabel != null && _petals != null) _petalLabel.text=Loc.T("settings.petals_label")+" "+NumberText.Group(_petals.Current());
            foreach ((ToggleView view, Func<bool> on) in _toggles)
            {
                view.Show(on());
            }

            _privacy.gameObject.SetActive(_consent != null && _consent.PrivacyOptionsRequired);
            if (_account != null && _accountStatus != null)
            {
                _account.text = _accountStatus();
            }
        }

        /// <summary>
        /// A settings row (the playtest's <c>MenuCards.Settings</c>; spec 005 FR-045, the owner's mockup of 2026-10-08): a
        /// raised cream row, its icon (<see cref="CardLook.SettingsIconOf"/>), its brown label after it
        /// (<see cref="CardLook.SettingsLabelScale"/> of <c>type.button_secondary</c>), and the garden switch (136 × 70
        /// units) near its right end.
        /// </summary>
        private static TextMeshProUGUI Toggle(RectTransform cardBody, Box body, Box line, string name, string key, Func<bool> on, Action flip, SettingsScreen screen, float u)
        {
            if(key!="settings.sound")
            {
                Image separator=UiKit.RoundRect("Divider",cardBody,UiTheme.Of(C.ParchmentEdge.WithAlpha(.35f)));
                UiKit.PlaceBox(separator.rectTransform,new Box(line.Left+line.Width*.04f,line.Top-7*u,line.Right-line.Width*.04f,line.Top-5*u),body);
            }
            Image row = UiFactory.CreateImage(name,cardBody,null,Color.clear);
            UiKit.PlaceBox(row.rectTransform, line, body);
            Icon(row.transform, CardLook.SettingsIconOf(key), CardLook.SettingsIconBox(line, u), line);
            TextMeshProUGUI label = UiKit.Label("Label", row.transform, string.Empty, PopupDesign.Label, UiTheme.Of(C.InkBrown), TextAlignmentOptions.Left, TextLook.Plain(C.InkBrown));
            label.fontSizeMax = UiKit.Units(T.ButtonSecondary.Size * CardLook.SettingsLabelScale);
            label.fontSize = label.fontSizeMax;
            float left = CardLook.SettingsLabelLeft(line, u);
            UiKit.PlaceBox(label.rectTransform, new Box(left, line.Top, CardLook.SettingsLabelRight(line, u), line.Bottom), line);
            ToggleView toggle = UiKit.Toggle("Switch", row.transform, () =>
            {
                flip();
                screen._persist();
                screen.Refresh();
            });
            UiKit.PlaceBox((RectTransform)toggle.transform, CardLook.SettingsToggleBox(line, u), line);
            screen._toggles.Add((toggle, on));
            return label;
        }

        /// <summary>A settings row's icon: the owner's picture, the raised glyph, or the drawn stand-in (the playtest's <c>MenuCards</c>).</summary>
        private static void Icon(Transform row, SettingsIcon icon, Box box, Box line)
        {
            Sprite? picture=icon.Picture!=null ? PopupDesign.SettingsIcon(icon.Picture)?NavigationArt.Get(icon.Picture):OwnerArt.Icon(icon.Picture) : null;
            Image image;
            if (icon.Glyph != null)
            {
                image = UiKit.RaisedGlyph(row, ProceduralSprites.RaisedGlyph(icon.Glyph, C.MedalGold));
                box = box.Inset(box.Width * 0.06f);
            }
            else if (picture != null)
            {
                image = UiFactory.CreateImage("Icon", row, null, Color.white);
                OwnerArt.Show(image, picture);
            }
            else
            {
                image = UiFactory.CreateImage("Icon", row, ProceduralSprites.Shape(icon.StandIn), UiTheme.Of(C.InkBrown));
                image.preserveAspect = true;
            }

            image.raycastTarget = false;
            UiKit.PlaceBox(image.rectTransform, box, line);
        }

        /// <summary>One or two cream buttons in a row: two halves with a gap, or one at the card's secondary width.</summary>
        private static Box[] Pair(Box row, int count, float u) =>
            ScreenLayout.Row(row, count, ButtonGapUnits * u, count == 1 ? DesignTokens.Size.CardSecondaryWidth * u : float.MaxValue, square: false);

    }
}
