using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The bottom menu in uGUI (spec 005 FR-030, contracts/look.md §6.7), the twin of the playtest's <c>Kit.BottomNav</c>:
    /// Home and the four pages (the Store, the Wardrobe, the Leaderboard and the Collection) each show one
    /// (<see cref="BottomNavView"/>), laid out from the kit's <see cref="ScreenLayout.BottomNav"/>, its five places always
    /// shown (a locked one with the padlock badge, <see cref="LockBadge"/>); and the notice a locked place's page shows
    /// (<see cref="LockedNotice"/>).
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>
        /// A bottom menu over the whole screen (its parent covers the screen), calling <paramref name="onSelect"/> with the
        /// place a finger taps (with the click; the active place takes no tap). Show it with <see cref="BottomNavView.Show"/>.
        /// </summary>
        public static BottomNavView BottomNav(string name, Transform parent, Action<NavPlace> onSelect)
        {
            RectTransform root = UiFactory.CreateRect(name, parent);
            UiFactory.Stretch(root);
            var view = root.gameObject.AddComponent<BottomNavView>();
            view.Build(onSelect);
            return view;
        }

        /// <summary>
        /// Shows a place's icon on an image: the owner's picture (<see cref="OwnerPictures.NavIcon"/>, pictures.md D9–D13)
        /// with its aspect kept, or while it is missing the place's glyph over its darker outline
        /// (<see cref="Design.BottomNav.Fallback"/>). Returns whether the owner's picture shows (the glyph takes a smaller box,
        /// <see cref="Design.BottomNav.GlyphBox"/>).
        /// </summary>
        public static bool SetNavIcon(Image image, NavPlace place)
        {
            Sprite? picture = OwnerArt.Icon(OwnerPictures.NavIcon(place));
            if (picture != null)
            {
                OwnerArt.Show(image, picture);
                return true;
            }

            (string shape, Rgba fill, Rgba line) = Design.BottomNav.Fallback(place);
            Sprite glyph = ProceduralSprites.Haloed(shape, fill, line, 0.07f);
            PictureFit.On(image, (w, h) => glyph, square: true);
            return false;
        }

        /// <summary>
        /// A locked place's padlock badge (<c>ui.nav.lock</c>; the playtest's <c>Kit.LockBadge</c>, the outfit cards'
        /// recipe): a domed cream disc (<c>cream.top</c> to <c>cream.face</c>) in a <c>cream.line</c> ring (8% of the disc a
        /// side) with the brown padlock (<c>ui.lock</c>, 56% of the disc), over a soft shadow. The returned rect is the
        /// whole badge, ring included (<see cref="Design.BottomNav.LockBox"/> of an icon); it takes no tap.
        /// </summary>
        public static RectTransform LockBadge(string name, Transform parent)
        {
            Image root = UiFactory.CreateImage(name, parent, null, Color.clear);
            root.raycastTarget = false;
            BoxLayout layout = BoxLayout.On(root.rectTransform);
            Box Disc(Box b)
            {
                float size = Design.BottomNav.LockDisc(b);
                return Box.FromCenter(b.CenterX, b.CenterY, size, size);
            }

            Box Outer(Box b)
            {
                Box d = Disc(b);
                return d.Inset(-d.Width * 0.08f);
            }

            SoftShadow(layout, Outer, b => b.Width / 2f, 0.25f, 0.06f);
            Image ring = RoundRect("LockRing", root.transform, UiTheme.Of(C.CreamLine));
            // The switches' round cream knob (spec 005 FR-048, the playtest's Kit.LockBadge).
            Image face = UiFactory.CreateImage("LockBall", root.transform, null, Color.white);
            face.raycastTarget = false;
            PictureFit.On(face, (w, h) => ProceduralSprites.ToggleKnob(Mathf.Min(w, h)), square: true);
            Image glyph = ShapeImage("Lock", root.transform, "ui.lock", C.InkBrown);
            glyph.raycastTarget = false;
            layout.Add(ring.rectTransform, Outer);
            layout.Add(face.rectTransform, Disc);
            layout.Add(glyph.rectTransform, b =>
            {
                Box d = Disc(b);
                return Box.FromCenter(d.CenterX, d.CenterY, d.Width * 0.56f, d.Width * 0.56f);
            });
            return root.rectTransform;
        }

        /// <summary>
        /// The notice of a locked place (<c>ui.locked.notice</c>; the playtest's <c>Kit.LockedNotice</c>), laid out from its
        /// own rect by <see cref="ScreenLayout.LockedNotice"/>: place the returned view's rect on the notice's area (a locked
        /// page's <see cref="LockedPageRegions.Notice"/>) and fill it with <see cref="LockedNoticeView.Show"/>.
        /// It takes no tap.
        /// </summary>
        public static LockedNoticeView LockedNotice(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<LockedNoticeView>();
            view.Build(layout);
            return view;
        }
    }

    /// <summary>
    /// One opaque wooden console with five live icons/captions and a green selected inset. The backing blocks scroll
    /// gestures through gaps; locked destinations retain their badge and callback. Twin of the playtest's Kit.BottomNav.
    /// </summary>
    public sealed class BottomNavView : MonoBehaviour
    {
        private readonly Dictionary<NavPlace,(Image Touch,Image Selected,Image Icon,TextMeshProUGUI Caption,RectTransform Lock)> _places=new Dictionary<NavPlace,(Image,Image,Image,TextMeshProUGUI,RectTransform)>();
        private Image _bar=null!,_panel=null!;
        private readonly List<Image> _dividers=new List<Image>();
        private NavPlace _active=NavPlace.Home;
        private bool _shown;
        public NavPlace Active=>_active;
        internal void Build(Action<NavPlace> onSelect)
        {
            _bar=UiFactory.CreateImage("ConsoleInput",transform,null,UiTheme.Of(GameNavigationDesign.ConsoleFill),raycast:true);
            _panel=GameNavigationView.Surface("WoodConsole",_bar.transform,GameNavigationDesign.Console);
            for(int i=1;i<Design.BottomNav.Order.Count;i++)_dividers.Add(UiFactory.CreateImage("Divider"+i,_bar.transform,null,UiTheme.Of(NavigationSkin.DividerInk)));
            foreach(NavPlace place in Design.BottomNav.Order)
            {
                NavPlace p=place;Image touch=UiFactory.CreateImage(Design.BottomNav.Key(place),transform,null,Color.clear,raycast:true);
                UiKit.TapTarget(touch,()=>{if(p!=_active)onSelect(p);},press:true);
                Image selected=GameNavigationView.Surface("SelectedInset",touch.transform,GameNavigationDesign.Selected);
                Image icon=UiFactory.CreateImage("Illustration",touch.transform,null,Color.white);UiKit.SetNavIcon(icon,place);icon.raycastTarget=false;
                TextMeshProUGUI caption=GameNavigationView.Label("Name",touch.transform,Loc.T(GameNavigationDesign.NameKey(place)),GameNavigationDesign.Caption,NavigationSkin.TabLook(false),.90f);
                RectTransform badge=UiKit.LockBadge("Lock",touch.transform);
                _places[place]=(touch,selected,icon,caption,badge);
            }
        }
        public void Show(NavPlace active,HomeLook look)
        {
            var frame=UiKit.ScreenFrame();BottomNavRegions r=ScreenLayout.BottomNav(frame.Width,frame.Height,frame.Insets,Design.BottomNav.Order,active);
            _active=active;_shown=true;Box backing=GameNavigationDesign.NavBacking(r,frame.Width);UiKit.PlaceScreen(_bar.rectTransform,backing);
            UiKit.PlaceBox(_panel.rectTransform,GameNavigationDesign.NavSurface(r,frame.Width),backing);
            for(int i=0;i<_dividers.Count;i++)UiKit.PlaceBox(_dividers[i].rectTransform,GameNavigationDesign.NavDivider(r,i+1),backing);
            for(int i=0;i<r.Places.Count;i++)
            {
                NavPlace place=r.Places[i];var parts=_places[place];bool chosen=place==active;Box touch=GameNavigationDesign.NavTouch(r,i),face=GameNavigationDesign.NavButton(r,i);
                UiKit.PlaceScreen(parts.Touch.rectTransform,touch);parts.Touch.raycastTarget=true;
                parts.Selected.gameObject.SetActive(chosen);UiKit.PlaceBox(parts.Selected.rectTransform,face,touch);
                Box icon=GameNavigationDesign.NavIcon(face,chosen);UiKit.PlaceBox(parts.Icon.rectTransform,icon,touch);
                parts.Caption.gameObject.SetActive(chosen);
                UiKit.ApplyLook(parts.Caption,GameNavigationDesign.Caption,NavigationSkin.TabLook(chosen));
                UiKit.PlaceBox(parts.Caption.rectTransform,GameNavigationDesign.NavCaption(face),touch);
                bool locked=!chosen&&!Design.BottomNav.IsOpen(place,look);parts.Lock.gameObject.SetActive(locked);
                if(locked)UiKit.PlaceBox(parts.Lock,Design.BottomNav.LockBox(icon),touch);
            }
        }
        public RectTransform? PlaceRect(NavPlace place)=>!_shown||!isActiveAndEnabled ? null : _places[place].Touch.rectTransform;
    }


    /// <summary>
    /// The notice of a locked place built by <see cref="UiKit.LockedNotice"/> (spec 005 FR-030, contracts/look.md §6.7; the
    /// playtest's <c>Kit.LockedNotice</c>), laid out from its own box by <see cref="ScreenLayout.LockedNotice"/>: the
    /// place's icon (the owner's picture, or its stand-in glyph in the smaller <see cref="Design.BottomNav.GlyphBox"/>) with
    /// the padlock badge on its lower right, then "Available from level N" (<c>locked.message</c>) in <c>type.title</c>
    /// <c>ink.brown</c> and "Keep playing to unlock it!" (<c>locked.hint</c>) in <c>type.body</c> <c>ink.brown_soft</c>,
    /// each <see cref="LockedNoticeRegions.MessageTextShare"/> or <see cref="LockedNoticeRegions.HintTextShare"/> of its
    /// line tall and shrunk to its width.
    /// </summary>
    public sealed class LockedNoticeView : MonoBehaviour
    {
        private BoxLayout _layout = null!;
        private Image _icon = null!;
        private TextMeshProUGUI _message = null!;
        private TextMeshProUGUI _hint = null!;
        private bool _owner;

        /// <summary>The message's label ("Available from level N").</summary>
        public TextMeshProUGUI Message => _message;

        internal void Build(BoxLayout layout)
        {
            _layout = layout;
            Transform root = layout.transform;
            _icon = UiFactory.CreateImage("Icon", root, null, Color.white);
            _icon.raycastTarget = false;
            RectTransform badge = UiKit.LockBadge("Lock", root);
            _message = UiKit.KitLabel("Message", root, string.Empty, T.Title, TextLook.Plain(C.InkBrown));
            _hint = UiKit.KitLabel("Hint", root, Loc.T("locked.hint"), T.Body, TextLook.Plain(C.InkBrownSoft));
            _message.raycastTarget = false;
            _hint.raycastTarget = false;
            layout.Add(_icon.rectTransform, b =>
            {
                Box icon = ScreenLayout.LockedNotice(b).Icon;
                return _owner ? icon : Design.BottomNav.GlyphBox(icon);
            });
            layout.Add(badge, b => ScreenLayout.LockedNotice(b).Badge);
            layout.Watch(_message).Watch(_hint).Then(b =>
            {
                LockedNoticeRegions r = ScreenLayout.LockedNotice(b);
                Line(_message, T.Title, r.Message, LockedNoticeRegions.MessageTextShare);
                Line(_hint, T.Body, r.Hint, LockedNoticeRegions.HintTextShare);
            });
        }

        /// <summary>Shows the notice of <paramref name="place"/>, available from <paramref name="level"/> (<see cref="Design.BottomNav.UnlockLevel"/>).</summary>
        public void Show(NavPlace place, int level)
        {
            _owner = UiKit.SetNavIcon(_icon, place);
            _message.text = Loc.F("locked.message", NumberText.Group(level));
            _hint.text = Loc.T("locked.hint");
            _layout.Apply();
        }

        /// <summary>A line of the notice: <paramref name="share"/> of its box tall, shrunk to the box's width, centered.</summary>
        private static void Line(TextMeshProUGUI label, TypeStyle style, Box line, float share)
        {
            float size = line.Height * share;
            float natural = KitText.Measure(label, size);
            if (natural > line.Width && natural > 0f)
            {
                size *= line.Width / natural;
            }

            KitText.Place(label, style, line.CenterX, line.CenterY, size, line.Width);
        }
    }
}
