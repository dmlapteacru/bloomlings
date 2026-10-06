using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The lotus loader and the lotus iris (spec 005 FR-039, contracts/look.md §6.13; the playtest's <c>LotusPainter</c>
    /// is its twin): a top-most canvas that draws a <see cref="LotusPose"/> of the kit's <see cref="LotusIris"/> over the
    /// screen, back to front: the parchment cover (the hole picture scaled to the hole and four plain boxes round it), the
    /// pink rim, the glow, the ring of petals, the lotus, the splash's logo and the text. While anything of the cover
    /// shows, a clear full-screen image takes every tap. <see cref="SplashScreen"/> and <see cref="LevelTransition"/> drive
    /// it.
    /// </summary>
    public sealed class LotusIrisView : MonoBehaviour
    {
        private readonly Image[] _panels = new Image[4];
        private readonly Image[] _petalLines = new Image[LotusIris.PetalCount];
        private readonly Image[] _petals = new Image[LotusIris.PetalCount];
        private CanvasGroup _group = null!;
        private Image _hole = null!;
        private Image _rim = null!;
        private Image _rimLine = null!;
        private Image _glow = null!;
        private RectTransform _lotus = null!;
        private CanvasGroup _lotusGroup = null!;
        private RectTransform? _logo;
        private CanvasGroup? _logoGroup;
        private TextMeshProUGUI _text = null!;
        private Color _textColor;
        private bool _splash;

        /// <summary>
        /// A top-most canvas under <paramref name="parent"/>, which should survive scene loads (the Boot object). The splash
        /// (<paramref name="splash"/>) also shows the logo, and its text is "Loading..."; the transition's is "Level N".
        /// </summary>
        public static LotusIrisView Create(Transform parent, bool splash, string text)
        {
            Canvas canvas = UiFactory.CreateCanvas(splash ? "Splash" : "LotusIris", 1000);
            canvas.transform.SetParent(parent, false);
            var view = canvas.gameObject.AddComponent<LotusIrisView>();
            view._splash = splash;
            view._group = canvas.gameObject.AddComponent<CanvasGroup>();
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect("Root", canvas.transform));

            // A clear image over the whole screen takes the taps while the cover shows (a second Next, the screen under it).
            Image blocker = UiFactory.CreateImage("Blocker", root, null, Color.clear, raycast: true);
            UiFactory.Stretch(blocker.rectTransform);

            Color cover = UiTheme.Of(LotusIris.Cover);
            for (int i = 0; i < view._panels.Length; i++)
            {
                view._panels[i] = UiFactory.CreateImage("Cover" + i, root, null, cover);
            }

            Sprite hole = ProceduralSprites.Picture(LotusIris.HoleKey, LotusIris.HoleRaster, LotusIris.HoleRaster, (w, h) => UiRaster.IrisHole(w, h));
            view._hole = UiFactory.CreateImage("Hole", root, hole, Color.white);
            view._rim = UiKit.RoundRing("Rim", root, UiTheme.Of(LotusIris.Rim), b => b.Width / 2f, _ => UiKit.Units(LotusIris.RimUnits));
            view._rimLine = UiKit.RoundRing("RimLine", root, UiTheme.Of(LotusIris.RimLine), b => b.Width / 2f, _ => UiKit.Units(LotusIris.RimLineUnits));
            Sprite glow = ProceduralSprites.Picture(LotusIris.GlowKey, LotusIris.GlowRaster, LotusIris.GlowRaster, (w, h) => UiRaster.IrisGlow(w, h));
            view._glow = UiFactory.CreateImage("Glow", root, glow, Color.white);
            for (int i = 0; i < LotusIris.PetalCount; i++)
            {
                view._petalLines[i] = UiKit.ShapeImage("PetalLine" + i, root, LotusIris.PetalShape, LotusIris.PetalLine(0f));
                view._petals[i] = UiKit.ShapeImage("Petal" + i, root, LotusIris.PetalShape, LotusIris.PetalFill(0f));
            }

            view._lotus = UiFactory.CreateRect("Lotus", root);
            view._lotusGroup = view._lotus.gameObject.AddComponent<CanvasGroup>();
            Image lotus = UiKit.IconParts("Picture", view._lotus, GardenLook.Lotus);
            UiFactory.Stretch(lotus.rectTransform);
            if (splash)
            {
                view._logo = OwnerArt.Logo("Logo", root, Loc.T("home.logo"));
                view._logoGroup = view._logo.gameObject.AddComponent<CanvasGroup>();
            }

            TypeStyle style = splash ? DesignTokens.Type.ButtonSecondary : DesignTokens.Type.LevelHome;
            view._textColor = UiTheme.Of(LotusIris.TextColor(splash));
            view._text = UiKit.Label("Text", root, text, style, view._textColor);
            if (!splash)
            {
                view._text.fontSize *= LotusIris.LevelTextScale;
            }

            view.Show(LotusIris.Transition(LotusIris.TransitionSeconds), block: false);
            return view;
        }

        /// <summary>The text under the lotus ("Level N").</summary>
        public void SetText(string text) => _text.text = text;

        /// <summary>
        /// Lays the pieces out for <paramref name="pose"/> on the current screen; <paramref name="block"/> takes the taps even
        /// before the cover shows (the transition's first frame, so Next cannot be tapped twice).
        /// </summary>
        public void Show(LotusPose pose, bool block = true)
        {
            bool covers = pose.Covers;
            _group.alpha = covers ? 1f : 0f;
            _group.blocksRaycasts = covers || block;
            if (!covers)
            {
                return;
            }

            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            LotusIrisLayout l = LotusIris.Layout(w, h, insets);
            Cover(l, pose);

            _glow.gameObject.SetActive(pose.GlowAlpha > 0f);
            UiKit.PlaceScreen(_glow.rectTransform, l.Glow);
            _glow.color = new Color(1f, 1f, 1f, pose.GlowAlpha);

            for (int i = 0; i < LotusIris.PetalCount; i++)
            {
                (float x, float y, float turn) = l.PetalAt(i, pose.RingSpin);
                (float lit, float scale) = LotusIris.PetalState(i, pose.RingLit);
                float side = l.Petal * scale;
                Petal(_petalLines[i], Box.FromCenter(x, y, side * LotusIris.PetalLineScale, side * LotusIris.PetalLineScale), turn, LotusIris.PetalLine(lit), pose.RingAlpha);
                Petal(_petals[i], Box.FromCenter(x, y, side, side), turn, LotusIris.PetalFill(lit), pose.RingAlpha);
            }

            UiKit.PlaceScreen(_lotus, l.Lotus);
            _lotus.localScale = Vector3.one * pose.LotusScale;
            _lotus.localEulerAngles = new Vector3(0f, 0f, -pose.LotusTurn);
            _lotusGroup.alpha = pose.LotusAlpha;

            if (_logo != null && _logoGroup != null)
            {
                ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets);
                UiKit.PlaceScreen(_logo, OwnerArt.LogoBox(r));
                _logo.localScale = Vector3.one * pose.LogoScale;
                _logoGroup.alpha = pose.LogoAlpha;
            }

            float y0 = l.TextY + (pose.TextRise * l.Unit);
            UiKit.PlaceScreen((RectTransform)_text.transform, Box.FromCenter(l.CenterX, y0, w * 0.8f, 180f * l.Unit));
            _text.color = new Color(_textColor.r, _textColor.g, _textColor.b, _textColor.a * pose.TextAlpha);
        }

        /// <summary>The cover: the hole picture round the hole, plain boxes round it and the pink rim; all plain when closed.</summary>
        private void Cover(LotusIrisLayout l, LotusPose pose)
        {
            float radius = l.HoleRadius(pose);
            bool closed = radius < 0.5f;
            Box[] panels = closed
                ? new[] { new Box(0f, 0f, l.Width, l.Height) }
                : LotusIris.CoverPanels(l.Width, l.Height, LotusIris.HoleBox(l.CenterX, l.CenterY, radius));
            for (int i = 0; i < _panels.Length; i++)
            {
                _panels[i].gameObject.SetActive(i < panels.Length);
                if (i < panels.Length)
                {
                    UiKit.PlaceScreen(_panels[i].rectTransform, panels[i]);
                }
            }

            _hole.gameObject.SetActive(!closed);
            _rim.gameObject.SetActive(!closed);
            _rimLine.gameObject.SetActive(!closed);
            if (closed)
            {
                return;
            }

            UiKit.PlaceScreen(_hole.rectTransform, LotusIris.HoleBox(l.CenterX, l.CenterY, radius));
            float rim = radius + (LotusIris.RimUnits * l.Unit);
            float line = rim + (LotusIris.RimLineUnits * l.Unit);
            UiKit.PlaceScreen(_rim.rectTransform, Box.FromCenter(l.CenterX, l.CenterY, 2f * rim, 2f * rim));
            UiKit.PlaceScreen(_rimLine.rectTransform, Box.FromCenter(l.CenterX, l.CenterY, 2f * line, 2f * line));
        }

        private static void Petal(Image image, Box box, float turn, Rgba color, float alpha)
        {
            image.gameObject.SetActive(alpha > 0f);
            UiKit.PlaceScreen(image.rectTransform, box);
            image.rectTransform.localEulerAngles = new Vector3(0f, 0f, -turn);
            image.color = UiTheme.Of(color.WithAlpha(alpha * (color.A / 255f)));
        }
    }
}
