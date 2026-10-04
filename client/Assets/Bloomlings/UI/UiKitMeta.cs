using System;
using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The reference look's celebration and meta pieces in uGUI (spec 005 contracts/look.md §4.4 to §4.6), the twins of
    /// the playtest's <c>KitMeta.cs</c> that the Unity kit did not have yet: the parchment pill of Home's rows, glyphs over
    /// their darker outline (the gift, the trophy, the sun), a cream pill with any text (the milestone mark, reward
    /// amounts), the cream ‹ › arrow buttons and the lotus fountain of the drawn Home stage. Like the rest of the kit, each
    /// element lays its parts out from its own box (<see cref="BoxLayout"/>), and decorations never take taps.
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>
        /// A parchment pill (Home's milestone teaser and rank rows; spec 005 §3.5, the playtest's <c>Kit.ParchmentPill</c>):
        /// the parchment surface rounded to a pill over a soft shadow. Children drawn on it go into the returned root.
        /// </summary>
        public static Image ParchmentPill(string name, Transform parent, bool raycast = false) =>
            Paper(name, parent, b => b.Height / 2f, DesignTokens.Garden.OutlineWidth, 5f, raycast);

        /// <summary>
        /// A glyph in a saturated color over its darker outline, as the reference's icons (the pink gift, the gold trophy,
        /// the sun; the playtest's <c>OutlinedShape</c>: the shape grown by 0.07 shape units in <paramref name="line"/>, then
        /// the shape in <paramref name="fill"/>). The caller places it; it keeps its square aspect.
        /// </summary>
        public static Image OutlinedGlyph(string name, Transform parent, string shapeId, Rgba fill, Rgba line)
        {
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.Haloed(shapeId, fill, line, 0.07f), Color.white);
            image.preserveAspect = true;
            return image;
        }

        /// <summary>
        /// A cream pill (the <see cref="CostPill"/> style) showing <paramref name="text"/> in <c>ink.brown</c>: with
        /// <see cref="CostKind.Petals"/> the lotus before it (the win's reward "+N"), with <see cref="CostKind.Charges"/> the
        /// text alone (the milestone mark, a milestone reward's amount). Change the text through
        /// <see cref="PillText"/>; the pill re-centers it.
        /// </summary>
        public static CostPillView TextPill(string name, Transform parent, CostKind kind, string text)
        {
            CostPillView pill = CostPill(name, parent, kind == CostKind.Petals ? Cost.Petals(0) : Cost.Charges(0));
            PillText(pill).text = text;
            return pill;
        }

        /// <summary>The label of a cream pill (its only text).</summary>
        public static TextMeshProUGUI PillText(CostPillView pill) => pill.GetComponentInChildren<TextMeshProUGUI>();

        /// <summary>
        /// A full-screen page's header row (§6.5, §6.6; the Wardrobe and the Store page; the playtest's
        /// <c>Kit.PageHeader</c>): the cream round back button calling <paramref name="onBack"/>, the wooden banner with ivy
        /// carrying <paramref name="title"/> and, when <paramref name="petals"/>, the Petals pill (its "+" calls
        /// <paramref name="onPlus"/>). <see cref="PageHeaderView.Place"/> puts the three on the kit's one line
        /// (<see cref="Design.PageHeader"/>); their parent covers the whole screen.
        /// </summary>
        public static PageHeaderView PageHeader(Transform parent, string title, Action onBack, bool petals, Action? onPlus = null)
        {
            var back = (RectTransform)RoundIconButton("Back", parent, "ui.back", onBack).transform;
            var banner = (RectTransform)WoodSign("Banner", parent, title, DesignTokens.Type.Title, SignDecor.Ivy).transform;
            PetalsPill? pill = petals ? PetalsPill("Petals", parent, onPlus) : null;
            return new PageHeaderView(back, banner, pill);
        }

        /// <summary>
        /// A cream round ‹ or › button (§4.6: the Wardrobe's hero arrows and its pages; the playtest's
        /// <c>Kit.ArrowButton</c>): the round button's domed cushion with a brown chevron in its cream halo, pointing right
        /// when <paramref name="next"/>. Not interactable, it fades to 55%. The button is the largest square in its rect.
        /// </summary>
        public static Button ArrowButton(string name, Transform parent, bool next, Action onClick)
        {
            GardenButton view = IconFace(name, parent, GardenLook.White, b => b.Height / 2f, square: true, raycast: true);
            view.GreyWhenDisabled = false;
            view.FadeWhenDisabled = true;
            Image glyph = UiFactory.CreateImage("Chevron", view.Content, ProceduralSprites.Haloed("ui.chevron", C.InkBrown, C.CreamTop), Color.white);
            glyph.preserveAspect = true;
            if (!next)
            {
                // The ‹ is the › mirrored (the UI shader draws both faces).
                glyph.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }

            float side = next ? 1f : -1f;
            BoxLayout.On(view.Content).Add(glyph.rectTransform, f =>
            {
                float g = view.IconSide * 0.5f;
                return Box.FromCenter(f.CenterX + (view.IconSide * 0.03f * side), f.CenterY, g, g);
            });
            return Clickable(view, onClick);
        }

        /// <summary>
        /// The lotus fountain of the drawn Home stage (<c>ui.fountain</c>; the reference's Home diorama, until the owner's
        /// picture; the playtest's <c>Kit.LotusFountain</c>): a small stone pedestal as its basin filling the rect, water with
        /// a light rim and shine, two lily pads and the pink lotus rising from the middle. Never a touch target.
        /// </summary>
        public static RectTransform LotusFountain(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            RectTransform basin = StonePedestal("Basin", root);
            layout.Add(basin, b => b);
            Rgba water = C.ButtonBlue.Lighten(0.42f);
            Image rim = Ellipse("Rim", root, UiTheme.Of(C.StoneLine.WithAlpha(0.5f)));
            Image pool = Ellipse("Water", root, UiTheme.Of(water));
            Image shine = Ellipse("Shine", root, UiTheme.Of(water.Lighten(0.35f).WithAlpha(0.7f)));
            Image ring = UiFactory.CreateImage("Ring", root, ProceduralSprites.Round(RoundFill.Ring, 0.04f), UiTheme.Of(Rgba.White.WithAlpha(0.6f)));
            ring.type = Image.Type.Simple;
            var pads = new Image[6];
            for (int i = 0; i < 2; i++)
            {
                pads[i * 3] = Ellipse("PadLine" + i, root, UiTheme.Of(C.IvyLine));
                pads[(i * 3) + 1] = Ellipse("Pad" + i, root, UiTheme.Of(C.LawnLight));
                pads[(i * 3) + 2] = Ellipse("PadLight" + i, root, UiTheme.Of(C.IvyLeaf.Lighten(0.2f)));
            }

            Image lotus = PetalIcon("Lotus", root);
            layout.Then(b =>
            {
                // The playtest squashes circles by ry/rx about the water's middle: these are the resulting ellipses.
                Box top = PedestalTop(b);
                float rx = top.Width * 0.4f;
                float ry = top.Height * 0.36f;
                float k = ry / Mathf.Max(0.001f, rx);
                float cx = top.CenterX;
                float cy = top.CenterY + (top.Height * 0.04f);
                float edge = rx + Mathf.Max(Units(1f), rx * 0.04f);
                BoxLayout.Place(rim.rectTransform, Box.FromCenter(cx, cy, edge * 2f, edge * 2f * k));
                BoxLayout.Place(pool.rectTransform, Box.FromCenter(cx, cy, rx * 2f, ry * 2f));
                BoxLayout.Place(shine.rectTransform, Box.FromCenter(cx - (rx * 0.18f), cy - (ry * 0.12f), rx * 1.24f, ry * 1.24f));
                BoxLayout.Place(ring.rectTransform, Box.FromCenter(cx, cy, rx * 1.6f, ry * 1.6f));
                float pad = rx * 0.42f;
                for (int i = 0; i < 2; i++)
                {
                    float px = cx + ((i == 0 ? -1f : 1f) * rx * 0.5f);
                    float line = pad + Mathf.Max(Units(1f), pad * 0.08f);
                    BoxLayout.Place(pads[i * 3].rectTransform, Box.FromCenter(px, cy, line * 2f, line * 0.9f));
                    BoxLayout.Place(pads[(i * 3) + 1].rectTransform, Box.FromCenter(px, cy, pad * 2f, pad * 0.9f));
                    BoxLayout.Place(pads[(i * 3) + 2].rectTransform, Box.FromCenter(px - (pad * 0.2f), cy - (pad * 0.09f), pad * 1.1f, pad * 0.495f));
                }

                float size = b.Width * 0.5f;
                BoxLayout.Place(lotus.rectTransform, Box.FromCenter(cx, cy - (size * 0.3f), size, size));
            });
            return root;
        }

        /// <summary>
        /// Fades a decoration in when it is shown (the win's rays and petals, spec 005 §4.4): its group's alpha eases from 0
        /// to <paramref name="alpha"/> over <paramref name="seconds"/> on unscaled time. Decorations only: it blocks no taps.
        /// </summary>
        public static FadeIn FadeInOnShow(GameObject target, float alpha, float seconds)
        {
            FadeIn fade = target.GetComponent<FadeIn>();
            if (fade == null)
            {
                fade = target.AddComponent<FadeIn>();
            }

            fade.Alpha = alpha;
            fade.Seconds = seconds;
            return fade;
        }
    }

    /// <summary>A page header built by <see cref="UiKit.PageHeader"/>: the back button, the banner and the Petals pill (or none).</summary>
    public sealed class PageHeaderView
    {
        public PageHeaderView(RectTransform back, RectTransform banner, PetalsPill? petals)
        {
            Back = back;
            Banner = banner;
            Petals = petals;
        }

        public RectTransform Back { get; }

        public RectTransform Banner { get; }

        public PetalsPill? Petals { get; }

        /// <summary>Places the three on the kit's header row (screen pixels; their parent covers the whole screen).</summary>
        public void Place(PageHeader header)
        {
            UiKit.PlaceScreen(Back, header.Back);
            UiKit.PlaceScreen(Banner, header.Banner);
            if (Petals != null)
            {
                UiKit.PlaceScreen((RectTransform)Petals.transform, header.Petals);
            }
        }
    }

    /// <summary>A decoration's fade-in on show (<see cref="UiKit.FadeInOnShow"/>), eased like the playtest's <c>Kit.Ease</c>.</summary>
    public sealed class FadeIn : MonoBehaviour
    {
        private CanvasGroup? _group;

        /// <summary>The alpha it ends at.</summary>
        public float Alpha { get; set; } = 1f;

        /// <summary>How long the fade takes.</summary>
        public float Seconds { get; set; } = 0.5f;

        /// <summary><c>1 - (1 - t)³</c> on 0..1: fast, then settling (the playtest's <c>Kit.Ease</c>).</summary>
        public static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - ((1f - t) * (1f - t) * (1f - t));
        }

        private void OnEnable()
        {
            if (_group == null)
            {
                _group = gameObject.AddComponent<CanvasGroup>();
            }

            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;
            if (isActiveAndEnabled)
            {
                StartCoroutine(Run());
            }
        }

        private IEnumerator Run()
        {
            float seconds = Mathf.Max(0.01f, Seconds);
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _group!.alpha = Alpha * Ease(t / seconds);
                yield return null;
            }

            _group!.alpha = Alpha;
        }
    }
}
