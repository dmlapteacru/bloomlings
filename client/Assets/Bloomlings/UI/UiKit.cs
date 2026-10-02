using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The design board's components in uGUI (spec 002 FR-005, FR-007; tasks T015), in the reference look of spec 005
    /// (<c>specs/005-reference-look/contracts/look.md</c> §3): the Unity twin of the playtest's <c>Kit</c>, with the same
    /// names and the same recipes.
    /// <list type="bullet">
    /// <item><description>glossy primary buttons in a wooden rim, cream secondary and icon buttons, the speed pill;</description></item>
    /// <item><description>wooden signs, the Petals pill, count badges and cost pills;</description></item>
    /// <item><description>parchment cards, the bottom sheet, rows, wells, tabs and toggles;</description></item>
    /// <item><description>text in the board's type styles;</description></item>
    /// <item><description>candy tiles, pods, slots, booster tiles, jam choices and the stone furniture (<c>UiKitGarden.cs</c>).</description></item>
    /// </list>
    /// Sizes come from <see cref="DesignTokens"/> in reference units (<see cref="Units"/>) or from shares of the element's
    /// own box, laid out by <see cref="BoxLayout"/> as the playtest computes its boxes. Positions come from
    /// <see cref="ScreenLayout"/> boxes in screen pixels, turned into normalized anchors, so the Unity client and the
    /// playtest share one layout rule. The canvas matches the screen width at 1080 units (<see cref="UiFactory.CreateCanvas"/>).
    /// Decorations never take taps (raycast off).
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>The corner radius, in sprite pixels, of <see cref="ProceduralSprites.RoundedSquare"/>.</summary>
        private const float RoundedCornerPixels = 10.4f;

        private static readonly Dictionary<string, Material> EmbossMaterials = new Dictionary<string, Material>(StringComparer.Ordinal);

        /// <summary>The screen size and its safe insets, in pixels (Unity's safe area is bottom-left based).</summary>
        public static (float Width, float Height, Insets Insets) ScreenFrame()
        {
            float w = Mathf.Max(1, Screen.width);
            float h = Mathf.Max(1, Screen.height);
            Rect safe = Screen.safeArea;
            return (w, h, new Insets(h - safe.yMax, safe.yMin, safe.xMin, w - safe.xMax));
        }

        /// <summary>The whole screen as a <see cref="Box"/>.</summary>
        public static Box ScreenBox()
        {
            (float w, float h, Insets _) = ScreenFrame();
            return new Box(0f, 0f, w, h);
        }

        /// <summary>Canvas units for a size in reference units (tablets are capped by <see cref="DesignTokens.ScaleFor"/>).</summary>
        public static float Units(float reference)
        {
            (float w, float h, Insets _) = ScreenFrame();
            return reference * DesignTokens.ScaleFor(w, h) * DesignTokens.ReferenceWidth / w;
        }

        /// <summary>Screen pixels per canvas unit (the canvas is <see cref="DesignTokens.ReferenceWidth"/> units wide).</summary>
        public static float PixelsPerUnit => ScreenFrame().Width / DesignTokens.ReferenceWidth;

        /// <summary>Anchors a rect to a screen box, relative to its parent's screen box.</summary>
        public static RectTransform PlaceBox(RectTransform rect, Box box, Box parent)
        {
            float pw = Mathf.Max(1f, parent.Width);
            float ph = Mathf.Max(1f, parent.Height);
            return UiFactory.Place(
                rect,
                (box.Left - parent.Left) / pw,
                1f - ((box.Bottom - parent.Top) / ph),
                (box.Right - parent.Left) / pw,
                1f - ((box.Top - parent.Top) / ph));
        }

        /// <summary>Anchors a rect to a screen box; its parent covers the whole screen.</summary>
        public static RectTransform PlaceScreen(RectTransform rect, Box box) => PlaceBox(rect, box, ScreenBox());

        /// <summary>A screen box (pixels) in a parent's top-down local canvas units, for <see cref="BoxLayout"/>.</summary>
        public static Box ToLocal(Box screenBox, Box parentScreenBox)
        {
            float k = 1f / Mathf.Max(0.0001f, PixelsPerUnit);
            return new Box((screenBox.Left - parentScreenBox.Left) * k, (screenBox.Top - parentScreenBox.Top) * k, (screenBox.Right - parentScreenBox.Left) * k, (screenBox.Bottom - parentScreenBox.Top) * k);
        }

        // ---- Text ----

        /// <summary>
        /// Text in a board type style: Nunito by weight, sentence case unless the style says uppercase, shrinking to fit
        /// (FR-026, edge cases). With a <paramref name="look"/> the label gets its volume (spec 003 FR-009) or, for a plain
        /// look, its light emboss (spec 005).
        /// </summary>
        public static TextMeshProUGUI Label(string name, Transform parent, string text, TypeStyle style, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center, TextLook? look = null)
        {
            TextMeshProUGUI label = UiFactory.CreateText(name, parent, text, Units(style.Size), color, alignment);
            Style(label, style, look);
            UiFactory.Stretch(label.rectTransform);
            return label;
        }

        public static void Style(TextMeshProUGUI label, TypeStyle style, TextLook? look = null)
        {
            TMP_FontAsset? font = UiFonts.For(style);
            if (font != null)
            {
                label.font = font;
            }

            // The bundled weights carry the boldness; the system fallback still needs the bold style.
            FontStyles fontStyle = style.Bold && font == null ? FontStyles.Bold : FontStyles.Normal;
            if (style.Upper)
            {
                fontStyle |= FontStyles.UpperCase;
            }

            label.fontStyle = fontStyle;
            label.fontSize = Units(style.Size);
            label.enableAutoSizing = true;
            label.fontSizeMax = Units(style.Size);
            label.fontSizeMin = Units(style.Min);
            if (look != null)
            {
                ApplyLook(label, style, look);
            }
            else if (style.Outline > 0f)
            {
                label.outlineWidth = 0.18f;
                label.outlineColor = UiTheme.TextOutline;
            }
        }

        /// <summary>
        /// Gives a label its look (spec 003 FR-009, spec 005): volumetric looks through <see cref="UiFonts.Apply"/>; plain
        /// looks one color with, when the look has one, the light emboss line 0.05 em under the letters (the playtest's
        /// <c>Emboss</c>), as a shared underlay material per font and color.
        /// </summary>
        public static void ApplyLook(TextMeshProUGUI label, TypeStyle style, TextLook look)
        {
            if (look.Volumetric)
            {
                UiFonts.Apply(label, style, look);
                return;
            }

            label.enableVertexGradient = false;
            label.color = UiTheme.Of(look.FillTop);
            TMP_FontAsset? font = UiFonts.For(style);
            if (font != null)
            {
                label.fontSharedMaterial = look.Emboss.HasValue ? EmbossMaterial(font, look.Emboss.Value) : font.material;
            }
        }

        /// <summary>The shared material of a plain label's emboss: a hard underlay 0.05 em below in the emboss color.</summary>
        private static Material EmbossMaterial(TMP_FontAsset font, Rgba emboss)
        {
            string key = font.name + "|emboss|" + emboss.Hex;
            if (EmbossMaterials.TryGetValue(key, out Material? material))
            {
                return material;
            }

            // One em is 10 units of the underlay offset (UiFonts.Material, contracts/fonts.md "Materials").
            material = new Material(font.material) { name = "Emboss " + key };
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", UiTheme.Of(emboss));
            material.SetFloat("_UnderlayOffsetX", 0f);
            material.SetFloat("_UnderlayOffsetY", -0.5f);
            material.SetFloat("_UnderlayDilate", 0f);
            material.SetFloat("_UnderlaySoftness", 0f);
            EmbossMaterials[key] = material;
            return material;
        }

        /// <summary>A label for the kit's measured layouts (<see cref="KitText.Place"/>): the style, the look, no stretch.</summary>
        internal static TextMeshProUGUI KitLabel(string name, Transform parent, string text, TypeStyle style, TextLook look)
        {
            TextMeshProUGUI label = UiFactory.CreateText(name, parent, text, Units(style.Size), UiTheme.Of(look.FillTop));
            Style(label, style, look);
            return label;
        }

        /// <summary>A grouped number ("1 240", research R9).</summary>
        public static string Number(long value) => NumberText.Group(value);

        // ---- Surfaces (spec 002, spec 003) ----

        /// <summary>A rounded rectangle whose corner radius is <paramref name="radiusUnits"/> reference units.</summary>
        public static Image Rounded(string name, Transform parent, Color color, float radiusUnits, bool raycast = false)
        {
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.RoundedSquare, color, raycast);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = RoundedCornerPixels / Mathf.Max(1f, Units(radiusUnits));
            return image;
        }

        /// <summary>A pill (fully rounded ends) that keeps its shape at any height.</summary>
        public static Image Pill(string name, Transform parent, Color color, bool raycast = false)
        {
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.PillSprite, color, raycast);
            image.type = Image.Type.Sliced;
            image.gameObject.AddComponent<PillShape>();
            return image;
        }

        /// <summary>Stretches a rect over its parent less the given insets, in canvas units.</summary>
        private static RectTransform Inset(RectTransform rect, float left, float bottom, float right, float top)
        {
            UiFactory.Stretch(rect);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>A vertical gradient on a graphic (the plate, the button faces, the paper).</summary>
        public static void Gradient(Graphic graphic, Color top, Color bottom)
        {
            graphic.color = Color.white;
            VerticalGradient gradient = graphic.GetComponent<VerticalGradient>();
            if (gradient == null)
            {
                gradient = graphic.gameObject.AddComponent<VerticalGradient>();
            }

            gradient.Top = top;
            gradient.Bottom = bottom;
            graphic.SetVerticesDirty();
        }

        /// <summary>
        /// A raised surface (spec 002 elev.raised, kept for older callers): the edge color fills the rect and the face sits
        /// on top of it, lifted by the edge height. Children go into the returned face.
        /// </summary>
        public static Image Raised(string name, Transform parent, Color face, Color edge, bool pill, float radiusUnits = 36f, bool raycast = false)
        {
            Image edgeImage = pill ? Pill(name, parent, edge, raycast) : Rounded(name, parent, edge, radiusUnits, raycast);
            Image faceImage = pill ? Pill("Face", edgeImage.transform, face) : Rounded("Face", edgeImage.transform, face, radiusUnits);
            RectTransform rect = UiFactory.Stretch(faceImage.rectTransform);
            rect.offsetMin = new Vector2(0f, Units(DesignTokens.Elevation.RaisedEdge));
            return faceImage;
        }

        /// <summary>A card shadow (elev.card) under a graphic.</summary>
        public static void CardShadow(Graphic graphic)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, DesignTokens.Elevation.CardShadowAlpha);
            shadow.effectDistance = new Vector2(0f, -Units(DesignTokens.Elevation.CardShadowOffset));
        }

        // ---- Shapes of the reference look (the playtest's FillRound, FillRoundGradient, StrokeRound) ----

        /// <summary>
        /// A rounded rectangle in <paramref name="color"/> at any size: the corner radius in canvas units comes from the
        /// image's own box (null: a pill), never more than half its shorter side (<see cref="RoundShape"/>).
        /// </summary>
        public static Image RoundRect(string name, Transform parent, Color color, Func<Box, float>? radius = null, bool raycast = false)
        {
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.Round(), color, raycast);
            RoundShape.On(image, radius);
            return image;
        }

        /// <summary>An ellipse filling the image's rect (the round sprite drawn unsliced; ground shadows, discs in non-square boxes).</summary>
        public static Image Ellipse(string name, Transform parent, Color color, bool raycast = false)
        {
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.Round(), color, raycast);
            image.type = Image.Type.Simple;
            return image;
        }

        /// <summary>A rounded rectangle with a vertical gradient from <paramref name="top"/> to <paramref name="bottom"/>.</summary>
        public static Image RoundGradient(string name, Transform parent, Rgba top, Rgba bottom, Func<Box, float>? radius = null, bool raycast = false)
        {
            Image image = RoundRect(name, parent, Color.white, radius, raycast);
            Gradient(image, UiTheme.Of(top), UiTheme.Of(bottom));
            return image;
        }

        /// <summary>An outline <paramref name="width"/> canvas units wide along the inside of a rounded rectangle's edge.</summary>
        public static Image RoundRing(string name, Transform parent, Color color, Func<Box, float>? radius, Func<Box, float> width)
        {
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.Round(RoundFill.Ring, 0.1f), color);
            RoundShape.On(image, radius, RoundFill.Ring, width);
            return image;
        }

        /// <summary>A band along a rounded rectangle's edge fading inward over <paramref name="band"/> canvas units (aged edges).</summary>
        public static Image RoundFade(string name, Transform parent, Color color, Func<Box, float>? radius, Func<Box, float> band)
        {
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.Round(RoundFill.Fade, 0.5f), color);
            RoundShape.On(image, radius, RoundFill.Fade, band);
            return image;
        }

        /// <summary>
        /// The reference look's soft drop shadow (the playtest's <c>Kit.SoftShadow</c>): four <c>garden.shadow</c> rounded
        /// rectangles under the element's <paramref name="body"/> box, each grown by 1.5% of its shorter side at a quarter of
        /// <paramref name="alpha"/>, moved down by <paramref name="offsetShare"/> of the shorter side. The images go into
        /// <paramref name="layout"/>'s element, so create them before the element's own layers.
        /// </summary>
        public static void SoftShadow(BoxLayout layout, Func<Box, Box> body, Func<Box, float> radius, float alpha, float offsetShare = 0.05f)
        {
            var radii = new float[4];
            for (int k = 0; k < 4; k++)
            {
                int layer = k;
                Image image = RoundRect("Shadow" + layer, layout.transform, UiTheme.Of(C.GardenShadow.WithAlpha(alpha / 4f)), _ => radii[layer]);
                layout.Add(image.rectTransform, box =>
                {
                    Box b = body(box);
                    float s = Mathf.Min(b.Width, b.Height);
                    float grow = s * 0.015f * layer;
                    radii[layer] = Mathf.Max(0f, radius(b)) + grow;
                    return b.Offset(0f, s * offsetShare).Inset(-grow);
                });
            }
        }

        /// <summary>An element root: a rect (with a clear raycast image when it takes taps) and its <see cref="BoxLayout"/>.</summary>
        internal static (RectTransform Root, BoxLayout Layout) Element(string name, Transform parent, bool raycast = false)
        {
            RectTransform root = raycast ? UiFactory.CreateImage(name, parent, null, Color.clear, raycast: true).rectTransform : UiFactory.CreateRect(name, parent);
            return (root, BoxLayout.On(root));
        }

        /// <summary>A shape sprite fitted into a box as an image (glyphs, marks), never a touch target.</summary>
        internal static Image ShapeImage(string name, Transform parent, string shapeId, Rgba color)
        {
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.Shape(shapeId), UiTheme.Of(color));
            image.preserveAspect = true;
            return image;
        }

        // ---- The Garden recipe (spec 003 FR-006 to FR-008; spec 005 §3.3) ----

        /// <summary>
        /// A garden element (the playtest's <c>Kit.GardenButton</c>): the cream plate (a soft shadow, its
        /// <c>parchment.edge</c> thickness, a cream gradient and a <c>cream.line</c> outline) and the raised face on it (the
        /// set's line, lip, gradient top and highlight band, or with <paramref name="gloss"/> the reference's smooth gloss),
        /// with a <see cref="GardenButton"/> that presses, springs back, breathes and greys out. The recipe follows the
        /// element's real height (<paramref name="heightUnits"/> is kept for older callers); <paramref name="radiusUnits"/>
        /// null makes pills and discs. Children go into <see cref="GardenButton.Content"/>, above the lip.
        /// </summary>
        public static GardenButton Garden(string name, Transform parent, ColorSet set, float heightUnits, float? radiusUnits = null, bool raycast = true, bool plate = true, bool gloss = false)
        {
            GardenButton view = NewButton(name, parent, set, raycast);
            BoxLayout layout = BoxLayout.On(view.Body);
            float plateRadius = float.MaxValue;
            float plateLineWidth = Units(DesignTokens.Garden.Outline(heightUnits));
            Image? plateShadow = null;
            Image? plateDepth = null;
            Image? plateFace = null;
            Image? plateLine = null;
            if (plate)
            {
                // The playtest's Kit.Plate, in its order: shadow, thickness, the cream gradient, the outline stroke on top.
                plateShadow = RoundRect("PlateShadow", view.Body, UiTheme.Of(C.GardenShadow.WithAlpha(0.16f)), _ => plateRadius);
                plateDepth = RoundRect("PlateDepth", view.Body, UiTheme.Of(C.ParchmentEdge.Darken(0.12f)), _ => plateRadius);
                plateFace = RoundGradient("Plate", view.Body, C.CreamTop, C.ParchmentBottom, _ => plateRadius);
                plateLine = RoundRing("PlateLine", view.Body, UiTheme.Of(C.CreamLine), _ => plateRadius, _ => plateLineWidth);
            }

            RectTransform face = UiFactory.CreateRect("Face", view.Body);
            BuildFace(view, face, FaceKind.Raised, gloss);
            layout.Then(box =>
            {
                float u = Mathf.Max(0.0001f, Units(1f));
                Box faceBox = box;
                float faceRadius = radiusUnits.HasValue ? Units(radiusUnits.Value) : float.MaxValue;
                if (plate && plateShadow != null && plateDepth != null && plateLine != null && plateFace != null)
                {
                    float h = box.Height / u;
                    float depth = Units(DesignTokens.Garden.Depth(h));
                    plateLineWidth = Units(DesignTokens.Garden.Outline(h));
                    var plateBox = new Box(box.Left, box.Top, box.Right, box.Bottom - depth);
                    plateRadius = Mathf.Min(faceRadius, plateBox.Height / 2f);
                    BoxLayout.Place(plateShadow.rectTransform, plateBox.Offset(0f, depth * 2f).Inset(-Units(2f), 0f));
                    BoxLayout.Place(plateDepth.rectTransform, plateBox.Offset(0f, depth));
                    BoxLayout.Place(plateFace.rectTransform, plateBox);
                    BoxLayout.Place(plateLine.rectTransform, plateBox);
                    float inset = Units(DesignTokens.Garden.PlateInset(h));
                    faceBox = plateBox.Inset(inset);
                    faceRadius = Mathf.Max(0f, plateRadius - inset);
                    foreach (Image image in new[] { plateShadow, plateDepth, plateFace, plateLine })
                    {
                        image.GetComponent<RoundShape>().Apply();
                    }
                }

                BoxLayout.Place(face, faceBox);
                float fh = faceBox.Height / u;
                float lip = Units(DesignTokens.Garden.Lip(fh));
                view.SetGeometry(Units(DesignTokens.Garden.Outline(fh)), lip, Mathf.Max(0f, lip - Units(3f)), faceRadius);
            });
            return view;
        }

        /// <summary>A garden button's root and body (the press squashes the body about its bottom center).</summary>
        private static GardenButton NewButton(string name, Transform parent, ColorSet set, bool raycast)
        {
            Image root = UiFactory.CreateImage(name, parent, null, Color.clear, raycast);
            var view = root.gameObject.AddComponent<GardenButton>();
            RectTransform body = UiFactory.Stretch(UiFactory.CreateRect("Body", root.transform));
            body.pivot = new Vector2(0.5f, 0f);
            view.Attach(set, body);
            return view;
        }

        /// <summary>How a face is built: the playtest's <c>Kit.Face</c> (raised) or <c>Kit.IconFace</c> (cushion).</summary>
        private enum FaceKind
        {
            Raised,
            Icon,
        }

        /// <summary>
        /// The parts of a raised face inside <paramref name="face"/> (spec 005 §3.3): the set's line under everything, the
        /// lip, and the moving top box with the top gradient, its sheen (highlight band or gloss) or cushion, and the
        /// content. <see cref="GardenButton.SetGeometry"/> sizes them from the face's box.
        /// </summary>
        private static void BuildFace(GardenButton view, RectTransform face, FaceKind kind, bool gloss)
        {
            // The playtest's order: the lip (the whole face), the top with its sheen or cushion, then the outline stroke.
            Image lip = RoundRect("Lip", face, Color.white, _ => view.FaceRadius);
            UiFactory.Stretch(lip.rectTransform);
            RectTransform topBox = UiFactory.Stretch(UiFactory.CreateRect("TopBox", face));
            Image top = RoundRect("Top", topBox, Color.white, b => Mathf.Min(view.FaceRadius, b.Height / 2f));
            UiFactory.Stretch(top.rectTransform);
            Gradient(top, Color.white, Color.white);
            BoxLayout topLayout = BoxLayout.On(topBox);
            float TopRadius(Box b) => Mathf.Min(view.FaceRadius, b.Height / 2f);
            bool cream = !GardenLook.LabelOn(view.Set).Volumetric;
            CanvasGroup? sheen = null;
            if (kind == FaceKind.Icon)
            {
                if (cream)
                {
                    // One domed cushion: the face's peach, a little deeper toward the bottom, and a lighter middle in three
                    // feathered steps (no ring, no dish).
                    view.TopColors = (s, d) => (s.Face.Darken(d), s.Face.Darken(0.04f + d));
                    var radii = new float[3];
                    for (int k = 0; k < 3; k++)
                    {
                        int step = k;
                        Image dome = RoundRect("Dome" + step, topBox, Color.white, _ => radii[step]);
                        Gradient(dome, Color.white, Color.clear);
                        view.AddExtra(dome, (s, d) => (s.Top.Darken(d).WithAlpha(0.5f), s.Top.WithAlpha(0f)));
                        topLayout.Add(dome.rectTransform, b =>
                        {
                            float inset = view.IconSide * (0.08f + (0.04f * step));
                            radii[step] = Mathf.Max(0f, TopRadius(b) - inset);
                            return b.Inset(inset);
                        });
                    }
                }
                else
                {
                    float innerRadius = 0f;
                    Image inner = RoundRect("Inner", topBox, Color.white, _ => innerRadius);
                    Gradient(inner, Color.white, Color.white);
                    view.AddExtra(inner, (s, d) => (s.Top.Darken(d), s.Face.Darken(d)));
                    Image band = RoundRect("Band", topBox, Color.white, b => Mathf.Min(b.Height / 2f, innerRadius));
                    Gradient(band, new Color(1f, 1f, 1f, 0.4f), new Color(1f, 1f, 1f, 0f));
                    topLayout.Add(inner.rectTransform, b =>
                    {
                        innerRadius = Mathf.Max(0f, TopRadius(b) - view.Rim);
                        return b.Inset(view.Rim);
                    });
                    topLayout.Add(band.rectTransform, b =>
                    {
                        Box i = b.Inset(view.Rim);
                        return new Box(i.Left + (i.Width * 0.12f), i.Top + (i.Height * 0.06f), i.Right - (i.Width * 0.12f), i.Top + (i.Height * 0.42f));
                    });
                }
            }
            else
            {
                RectTransform sheenRect = UiFactory.Stretch(UiFactory.CreateRect("Sheen", topBox));
                sheen = sheenRect.gameObject.AddComponent<CanvasGroup>();
                sheen.blocksRaycasts = false;
                sheen.interactable = false;
                if (gloss)
                {
                    // The reference's smooth gloss: the lightened top color fading down from the top edge, feathered in three
                    // steps so no edge shows, and a thin light line along the straight part of the top edge.
                    var radii = new float[3];
                    for (int k = 0; k < 3; k++)
                    {
                        int step = k;
                        Image band = RoundRect("Gloss" + step, sheenRect, Color.white, _ => radii[step]);
                        Gradient(band, Color.white, Color.clear);
                        view.AddExtra(band, (s, d) => (s.Top.Lighten(0.35f).WithAlpha(0.35f / 3f), s.Top.Lighten(0.35f).WithAlpha(0f)));
                        topLayout.Add(band.rectTransform, b =>
                        {
                            var glossBox = new Box(b.Left + (b.Width * 0.03f), b.Top + (b.Height * 0.04f), b.Right - (b.Width * 0.03f), b.Top + (b.Height * 0.46f));
                            Box stepBox = glossBox.Inset(b.Width * 0.02f * step, b.Height * 0.02f * step);
                            radii[step] = Mathf.Min(stepBox.Height / 2f, TopRadius(b));
                            return stepBox;
                        });
                    }

                    Image shine = RoundRect("Shine", sheenRect, new Color(1f, 1f, 1f, 0.55f));
                    topLayout.Add(shine.rectTransform, b =>
                    {
                        float r = TopRadius(b);
                        float width = Mathf.Max(Units(1f), view.LineWidth * 0.8f);
                        float y = view.LineWidth * 1.6f;
                        return new Box(b.Left + (r * 0.6f), y - (width / 2f), b.Right - (r * 0.6f), y + (width / 2f));
                    });
                }
                else
                {
                    Image band = RoundRect("Highlight", sheenRect, Color.white, b => b.Height / 2f);
                    Gradient(band, new Color(1f, 1f, 1f, DesignTokens.Garden.HighlightAlpha), new Color(1f, 1f, 1f, 0f));
                    topLayout.Add(band.rectTransform, b => new Box(b.Left + (b.Width * 0.07f), b.Top + (b.Height * 0.07f), b.Right - (b.Width * 0.07f), b.Top + (b.Height * DesignTokens.Garden.HighlightHeight)));
                }
            }

            Image line = RoundRing("Line", face, Color.white, _ => view.FaceRadius, _ => view.LineWidth);
            UiFactory.Stretch(line.rectTransform);
            RectTransform content = UiFactory.Stretch(UiFactory.CreateRect("Content", face));
            view.BuildFace(line, lip, topBox, top, sheen, content, topLayout);
        }

        /// <summary>A label on a garden face, in the set's look (FR-009): volumetric on colors, dark brown on cream.</summary>
        public static TextMeshProUGUI GardenLabel(GardenButton button, string text, TypeStyle style)
        {
            TextLook look = GardenLook.LabelOn(button.Set);
            TextMeshProUGUI label = Label("Label", button.Content, text, style, UiTheme.Of(look.FillTop), look: look);
            UiFactory.Place(label.rectTransform, 0.06f, 0.04f, 0.94f, 0.96f);
            button.Track(label, style);
            return label;
        }

        /// <summary>
        /// A glyph on a garden face (FR-010, spec 005 §3.3): light with a dark line under it (6% of its size) on colored
        /// faces; <c>ink.brown</c> with a thin <c>cream.top</c> halo all around it on cream. The caller places it.
        /// </summary>
        public static Image GardenGlyph(GardenButton button, Transform parent, string shapeId)
        {
            Image glyph = UiFactory.CreateImage("Glyph", parent, ProceduralSprites.Shape(shapeId), UiTheme.Of(GardenLook.GlyphOn(button.Set)));
            glyph.preserveAspect = true;
            if (GardenLook.LabelOn(button.Set).Volumetric)
            {
                var line = glyph.gameObject.AddComponent<Shadow>();
                line.effectColor = UiTheme.Of(button.Set.Line);
                line.useGraphicAlpha = true;
                EffectFit.On(line, new Vector2(0f, -0.06f));
            }
            else
            {
                // The shape grown by 0.06 shape units: about 2.8% of the glyph's box on every side.
                var halo = glyph.gameObject.AddComponent<Outline>();
                halo.effectColor = UiTheme.Of(C.CreamTop);
                halo.useGraphicAlpha = true;
                EffectFit.On(halo, new Vector2(0.025f, -0.025f));
            }

            return glyph;
        }

        /// <summary>
        /// The leaves and white flower over a main button's corners (FR-011a): never a touch target, laid out from the
        /// button's height (<see cref="GardenLook.DecorationBoxes"/>), and off when the decoration switch is off.
        /// </summary>
        public static void Decoration(GardenButton button)
        {
            if (!DesignTokens.Garden.Decorations)
            {
                return;
            }

            Sprite? picture = OwnerArt.Decor(OwnerPictures.ButtonLeaves);
            Image topLeft = UiFactory.CreateImage("DecoTopLeft", button.transform, picture == null ? ProceduralSprites.Decoration(false) : null, Color.white);
            Image bottomRight = UiFactory.CreateImage("DecoBottomRight", button.transform, picture == null ? ProceduralSprites.Decoration(true) : null, Color.white);
            if (picture != null)
            {
                // The owner's sprig (pictures.md D7) on the top-left corner, turned half way for the bottom-right one.
                OwnerArt.Show(topLeft, picture);
                OwnerArt.Show(bottomRight, picture, mirror: true, turn: true);
            }

            var layout = button.gameObject.AddComponent<DecorationLayout>();
            layout.TopLeft = topLeft.rectTransform;
            layout.BottomRight = bottomRight.rectTransform;
        }

        // ---- Buttons (spec 005 §3.3) ----

        /// <summary>
        /// The green primary button (Play, Next, Claim, Resume, Continue, Free rescue; spec 005 §3.3): a glossy green pill
        /// in a light wood rim (a soft shadow, the plank with only a thin deeper band, the face inset by 8.5% of the height
        /// on every side) with a volumetric white label outlined in dark green (<see cref="TextLook.OnGloss"/>). Pressed,
        /// the face sinks into its lip and darkens. <paramref name="decorate"/> adds the leaves and flower,
        /// <paramref name="playArrow"/> the ▶ as tall as the letters, <paramref name="iconId"/> a glyph before the label,
        /// <paramref name="breathe"/> the idle breath of the one waiting button, and <paramref name="set"/> another color
        /// (<see cref="GardenLook.Orange"/>).
        /// </summary>
        public static Button PrimaryButton(string name, Transform parent, string label, Action onClick, TypeStyle? style = null, bool decorate = false, bool playArrow = false, ColorSet? set = null, string? iconId = null, bool breathe = false)
        {
            TypeStyle s = style ?? DesignTokens.Type.Button;
            ColorSet colors = set ?? GardenLook.Green;
            GardenButton view = NewButton(name, parent, colors, raycast: true);
            BoxLayout layout = BoxLayout.On(view.Body);
            SoftShadow(layout, b => b, b => b.Height / 2f, 0.24f, 0.07f);
            Image rim = UiFactory.CreateImage("Rim", view.Body, null, Color.white);
            PictureFit.On(rim, (w, h) => ProceduralSprites.Plank(WoodTone.Light, w, h, 0.5f, 0.018f, 3, 0.03f), sliced: true);
            layout.Add(rim.rectTransform, b => b);
            RectTransform face = UiFactory.CreateRect("Face", view.Body);
            BuildFace(view, face, FaceKind.Raised, gloss: true);
            layout.Then(box =>
            {
                Box faceBox = box.Inset(box.Height * 0.085f);
                BoxLayout.Place(face, faceBox);
                float lip = faceBox.Height * 0.1f;
                float fh = faceBox.Height / Mathf.Max(0.0001f, Units(1f));
                view.SetGeometry(Units(DesignTokens.Garden.Outline(fh)), lip, Mathf.Max(0f, lip - Units(3f)), float.MaxValue);
            });

            TextLook Look(ColorSet shown) => TextLook.OnGloss(shown);
            TextMeshProUGUI text = KitLabel("Label", view.Content, label, s, Look(colors));
            view.Track(text, s, Look);
            BoxLayout content = BoxLayout.On(view.Content).Watch(text);
            if (playArrow)
            {
                RectTransform arrow = PlayArrow(view, s);
                content.Then(f =>
                {
                    // The label and the ▶ as one group, centered, shrunk together to the face less its rounded ends.
                    float em = Units(s.Size);
                    float textWidth = KitText.Measure(text, em);
                    float arrowSize = em * 0.92f;
                    float gap = em * 0.1f;
                    float side = f.Height * 0.45f;
                    if (textWidth <= 0f)
                    {
                        textWidth = Mathf.Max(1f, f.Width - (side * 2f) - arrowSize - gap);
                    }

                    float k = Mathf.Min(1f, (f.Width - (side * 2f)) / Mathf.Max(1f, textWidth + gap + arrowSize));
                    float start = f.CenterX - (((textWidth + gap + arrowSize) * k) / 2f);
                    KitText.Place(text, s, start + (textWidth * k / 2f), f.CenterY, em * k, textWidth * k + 1f);
                    BoxLayout.Place(arrow, Box.FromCenter(start + ((textWidth + gap) * k) + (arrowSize * k / 2f), f.CenterY + (em * k * 0.02f), arrowSize * k, arrowSize * k));
                });
            }
            else if (iconId != null)
            {
                Image glyph = GardenGlyph(view, view.Content, iconId);
                content.Then(f => IconAndText(glyph.rectTransform, text, s, f, f.Height * 0.5f, Units(14f), f.Width - (f.Height * 1.4f)));
            }
            else
            {
                content.Then(f => KitText.Place(text, s, f.CenterX, f.CenterY, Units(s.Size), f.Width - (f.Height * 0.9f)));
            }

            if (decorate)
            {
                Decoration(view);
            }

            view.Breathe = breathe;
            return Clickable(view, onClick);
        }

        /// <summary>
        /// The cream secondary button (Restart, Settings, Home, ×2 reward, Get +N; spec 005 §3.3): a cream face on a cream
        /// plate with a brown label and an optional brown glyph on the left (Restart's ⟳). Not interactable, it fades to
        /// 55%.
        /// </summary>
        public static Button SecondaryButton(string name, Transform parent, string label, Action onClick, string? iconId = null, TypeStyle? style = null)
        {
            TypeStyle s = style ?? DesignTokens.Type.ButtonSecondary;
            GardenButton view = Garden(name, parent, GardenLook.Cream, DesignTokens.Size.SecondaryHeight);
            view.GreyWhenDisabled = false;
            view.FadeWhenDisabled = true;
            TextMeshProUGUI text = KitLabel("Label", view.Content, label, s, GardenLook.LabelOn(GardenLook.Cream));
            view.Track(text, s);
            BoxLayout content = BoxLayout.On(view.Content).Watch(text);
            if (iconId != null)
            {
                Image glyph = GardenGlyph(view, view.Content, iconId);
                // The glyph a little taller than the letters (70% of the face), as the reference's ⟳ on "Restart Level".
                content.Then(f => IconAndText(glyph.rectTransform, text, s, f, f.Height * 0.7f, Units(16f), f.Width - (f.Height * 1.6f)));
            }
            else
            {
                content.Then(f => KitText.Place(text, s, f.CenterX, f.CenterY, Units(s.Size), f.Width - (f.Height * 0.7f)));
            }

            return Clickable(view, onClick);
        }

        /// <summary>A glyph and a label as one centered group (the playtest's icon buttons): glyph, gap, then the text.</summary>
        private static void IconAndText(RectTransform glyph, TextMeshProUGUI text, TypeStyle style, Box f, float icon, float gap, float maxText)
        {
            float textWidth = Mathf.Min(KitText.Measure(text, Units(style.Size)), Mathf.Max(1f, maxText));
            if (textWidth <= 0f)
            {
                textWidth = Mathf.Max(1f, maxText);
            }

            float start = f.CenterX - ((icon + gap + textWidth) / 2f);
            BoxLayout.Place(glyph, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon));
            KitText.Place(text, style, start + icon + gap + (textWidth / 2f), f.CenterY, Units(style.Size), textWidth + 1f);
        }

        /// <summary>
        /// A round or squircle icon button (Settings, Pause, close, back, Wardrobe, Collection; spec 005 §3.3): one domed
        /// cream cushion with a lip, an outline and a soft shadow, and a brown glyph with a light halo at about 46% of the
        /// size. Pause is a squircle (<paramref name="squircle"/>: radius 34% of the size; by default only Pause), the
        /// others circles; close is cream with a brown ✕ like every round button. <paramref name="set"/> paints another
        /// face (the green "+"). The button is the largest square in its rect.
        /// </summary>
        public static Button RoundIconButton(string name, Transform parent, string shapeId, Action onClick, ColorSet? set = null, bool? squircle = null)
        {
            ColorSet colors = set ?? GardenLook.White;
            bool rounded = squircle ?? shapeId == "ui.pause";
            GardenButton view = IconFace(name, parent, colors, b => rounded ? b.Height * 0.34f : b.Height / 2f, square: true, raycast: true);
            Image glyph = GardenGlyph(view, view.Content, shapeId);
            BoxLayout.On(view.Content).Add(glyph.rectTransform, f =>
            {
                float g = view.IconSide * GlyphShare(shapeId);
                return Box.FromCenter(f.CenterX, f.CenterY, g, g);
            });
            return Clickable(view, onClick);
        }

        /// <summary>
        /// The box share of a glyph on a round button, so each glyph looks about 46% of the button as in the reference:
        /// the shapes fill their boxes differently (the pause bars are slim, the gear is wide).
        /// </summary>
        public static float GlyphShare(string shapeId) => shapeId switch
        {
            "ui.pause" => 0.8f,
            "ui.close" => 0.74f,
            "ui.settings" => 0.66f,
            "ui.back" => 0.64f,
            _ => 0.6f,
        };

        /// <summary>
        /// The face of a round or squircle icon button, the speed pill, the booster tile and the green "+" (the playtest's
        /// <c>Kit.IconFace</c>, spec 005 §3.3): a soft shadow, the lip and the outline around a single domed cushion on
        /// cream sets (peach toward the edges, a lighter middle feathered in), or a light rim around a domed face in the
        /// set's color on colored sets. <paramref name="radius"/> gets the face's box; <paramref name="square"/> keeps the
        /// largest centered square of the rect. Children go into <see cref="GardenButton.Content"/>: 9% of the shorter side
        /// inside the face, moving with the press.
        /// </summary>
        public static GardenButton IconFace(string name, Transform parent, ColorSet set, Func<Box, float> radius, bool square = true, bool raycast = false)
        {
            GardenButton view = NewButton(name, parent, set, raycast);
            BoxLayout layout = BoxLayout.On(view.Body);
            Func<Box, Box> faceBox = b => square ? Box.FromCenter(b.CenterX, b.CenterY, Mathf.Min(b.Width, b.Height), Mathf.Min(b.Width, b.Height)) : b;
            SoftShadow(layout, faceBox, b => Mathf.Min(radius(b), Mathf.Min(b.Width, b.Height) / 2f), 0.2f, 0.06f);
            RectTransform face = UiFactory.CreateRect("Face", view.Body);
            BuildFace(view, face, FaceKind.Icon, gloss: false);
            layout.Then(box =>
            {
                Box f = faceBox(box);
                BoxLayout.Place(face, f);
                float s = Mathf.Min(f.Width, f.Height);
                bool cream = !GardenLook.LabelOn(view.Set).Volumetric;
                float lip = s * (cream ? 0.07f : 0.085f);
                float line = Mathf.Max(Units(2f), s * (cream ? 0.02f : 0.024f));
                view.SetGeometry(line, lip, lip * 0.7f, Mathf.Min(radius(f), s / 2f), s * 0.09f, s);
            });
            return view;
        }

        /// <summary>
        /// The speed pill of the gameplay top bar (spec 005 §3.3): the cream squircle style (radius 34% of its height),
        /// wider, with the speed (<paramref name="label"/>, "1×" or "2×") in <c>ink.brown</c> at half its height and the
        /// <c>ui.fast</c> chevrons (▶▶, 52% of its height) after it, both centered as a group. The label is the pill's only
        /// text, so callers find it with <c>GetComponentInChildren&lt;TextMeshProUGUI&gt;</c>.
        /// </summary>
        public static Button SpeedPill(string name, Transform parent, string label, Action onClick)
        {
            GardenButton view = IconFace(name, parent, GardenLook.White, b => b.Height * 0.34f, square: false, raycast: true);
            TypeStyle s = DesignTokens.Type.LevelPill;
            TextMeshProUGUI text = KitLabel("Label", view.Content, label, s, TextLook.Plain(C.InkBrown));
            Image fast = UiFactory.CreateImage("Fast", view.Content, ProceduralSprites.Haloed(GardenLook.FastGlyph.ShapeId, GardenLook.FastGlyph.Fill, C.CreamTop), Color.white);
            fast.preserveAspect = true;
            BoxLayout.On(view.Content).Watch(text).Then(f =>
            {
                float h = view.IconSide;
                float size = h * 0.5f;
                float glyph = h * 0.52f;
                float gap = h * 0.04f;
                float measured = KitText.Measure(text, size);
                float textWidth = Mathf.Min(measured > 0f ? measured : size * 1.4f, f.Width - glyph - gap - (h * 0.2f));
                float start = f.CenterX - ((textWidth + gap + glyph) / 2f);
                KitText.Place(text, s, start + (textWidth / 2f), f.CenterY, size, textWidth + 1f);
                BoxLayout.Place(fast.rectTransform, Box.FromCenter(start + textWidth + gap + (glyph / 2f), f.CenterY, glyph, glyph));
            });
            return Clickable(view, onClick);
        }

        /// <summary>The 2× control of the gameplay top bar: since spec 005 the cream <see cref="SpeedPill"/>.</summary>
        public static Button DarkPill(string name, Transform parent, string label, Action onClick) => SpeedPill(name, parent, label, onClick);

        /// <summary>Makes a garden element a button: the click sound and action, the press and the greyed state.</summary>
        private static Button Clickable(GardenButton view, Action onClick)
        {
            var button = view.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = view.Top;
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick();
            });
            view.Button = button;
            return button;
        }

        /// <summary>The ▶ after Play's label, as tall as the letters and in the label's look (FR-010): its line, then its fill.</summary>
        private static RectTransform PlayArrow(GardenButton view, TypeStyle style)
        {
            TextLook look = TextLook.OnGloss(view.Set);
            Func<float, float, float> sdf = ShapeLibrary.Get("ui.play");

            // The outline as the letters': the look's outline in ems over the arrow's half box in shape units.
            float grow = look.OutlineEm * ShapeRaster.Margin / 0.46f;
            RectTransform box = UiFactory.CreateRect("Play", view.Content);
            Image line = UiFactory.CreateImage("Line", box, ProceduralSprites.Composite("ui.play/line/" + grow.ToString("0.###", CultureInfo.InvariantCulture), (x, y) => sdf(x, y) - grow), UiTheme.Of(look.Outline));
            UiFactory.Stretch(line.rectTransform);
            var extrusion = line.gameObject.AddComponent<Shadow>();
            extrusion.effectColor = UiTheme.Of(look.Outline);
            EffectFit.On(extrusion, new Vector2(0f, -look.ExtrudeEm / 0.92f));
            Image fill = UiFactory.CreateImage("Fill", box, ProceduralSprites.Shape("ui.play"), UiTheme.Of(look.FillTop));
            UiFactory.Stretch(fill.rectTransform);
            return box;
        }

        // ---- Pills and badges (spec 005 §3.2, §3.4) ----

        /// <summary>
        /// The gameplay level label: since spec 005 a wooden sign with ivy at both ends (§3.2, research D6); returns its
        /// label. <paramref name="pill"/> recolors it like the old pill: <see cref="GardenLook.Lilac"/> or
        /// <see cref="GardenLook.Purple"/> turn the letters <c>badge.super_hard</c> darkened 0.35 (a Super Hard level), any
        /// other set keeps them <c>ink.brown</c>.
        /// </summary>
        public static TextMeshProUGUI LevelPill(string name, Transform parent, out GardenButton pill)
        {
            WoodSignView sign = WoodSign(name, parent, string.Empty, DesignTokens.Type.LevelPill, SignDecor.Ivy);
            pill = sign.gameObject.AddComponent<GardenButton>();
            pill.AttachSign(sign);
            return sign.Label;
        }

        /// <summary>A small sticker badge on a plate: HARD in red or SUPER HARD in purple (frames 8 and 9, FR-014).</summary>
        public static TextMeshProUGUI Badge(string name, Transform parent, string text, ColorSet set, out GardenButton badge)
        {
            badge = Garden(name, parent, set, 50f, raycast: false);
            TextMeshProUGUI label = GardenLabel(badge, text, DesignTokens.Type.Badge);
            UiFactory.Place(label.rectTransform, 0.08f, 0.04f, 0.92f, 0.96f);
            return label;
        }

        /// <summary>
        /// A count badge (booster charges, "+N" on a stack; spec 005 §3.4): white digits on a <c>badge.green</c> disc with a
        /// white ring (10% of the disc) and a thin dark outline, over a soft shadow. The returned <paramref name="disc"/>
        /// rect is the whole badge (ring and outline included) and the disc's size is its height; a longer number widens
        /// the badge about its center. Returns the label.
        /// </summary>
        public static TextMeshProUGUI CountBadge(string name, Transform parent, out Image disc)
        {
            disc = UiFactory.CreateImage(name, parent, null, Color.clear);
            BoxLayout layout = BoxLayout.On(disc.rectTransform);
            TextMeshProUGUI? label = null;
            float width = 0f;
            float size = 0f;
            Box Disc(Box b)
            {
                size = b.Height / 1.26f;
                float measured = label != null ? KitText.Measure(label, size * 28f / 48f) : 0f;
                width = Mathf.Max(size, measured + (size * 0.55f));
                return Box.FromCenter(b.CenterX, b.CenterY, width, size);
            }

            Box Outer(Box b) => Disc(b).Inset(-(size * 0.1f) - Mathf.Max(Units(1f), size * 0.03f));
            SoftShadow(layout, Outer, b => b.Height / 2f, 0.25f, 0.07f);
            Image outline = RoundRect("Outline", disc.transform, UiTheme.Of(C.GardenShadow.WithAlpha(0.8f)));
            Image ring = RoundRect("Ring", disc.transform, Color.white);
            Image fill = RoundGradient("Fill", disc.transform, C.BadgeGreen.Lighten(0.14f), C.BadgeGreen);
            label = KitLabel("Count", disc.transform, string.Empty, DesignTokens.Type.Badge, TextLook.Plain(C.TextOnColor) with { Emboss = null });
            layout.Add(outline.rectTransform, Outer);
            layout.Add(ring.rectTransform, b => Disc(b).Inset(-size * 0.1f));
            layout.Add(fill.rectTransform, Disc);
            TextMeshProUGUI text = label;
            layout.Watch(text).Then(b =>
            {
                Box d = Disc(b);
                KitText.Place(text, DesignTokens.Type.Badge, d.CenterX, d.CenterY - (size * 0.02f), size * 28f / 48f, width * 0.9f);
            });
            return text;
        }

        /// <summary>
        /// The Petals symbol: the owner's lotus picture (<see cref="OwnerPictures.CurrencyLotus"/>) when it exists, else the
        /// drawn pink lotus with its outline and light tips (FR-006; spec 005 contracts/look.md §3.4; <see cref="SetIconParts"/>).
        /// </summary>
        public static Image PetalIcon(string name, Transform parent) => IconParts(name, parent, GardenLook.Lotus);

        /// <summary>
        /// A multi-part icon (spec 005 contracts/look.md §3.4, §3.8: the lotus, the colored booster icons) baked into one
        /// colored sprite at the image's pixel size: each part's outline, then its fill, back to front; all grey when
        /// <paramref name="grey"/>. A booster's icon (<see cref="GardenLook.BoosterOf"/>) is the owner's picture instead
        /// when it exists (pictures.md D1–D4), faded when grey. The caller places the image; it keeps its square aspect.
        /// </summary>
        public static Image IconParts(string name, Transform parent, IReadOnlyList<IconPart> parts, bool grey = false)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            SetIconParts(image, parts, grey);
            return image;
        }

        /// <summary>
        /// The Petals balance pill (frames 2, 3 and 17; spec 005 §3.4): a cream raised pill (a soft shadow, the
        /// <c>cream.lip</c> band, the cream face and a <c>cream.line</c> outline) with the lotus over its left end, the
        /// grouped balance in <c>ink.brown</c>, and the round green "+" over its right end (to the Store, once unlocked;
        /// FR-013). The whole pill is the touch target when <paramref name="onPlus"/> is set.
        /// </summary>
        public static PetalsPill PetalsPill(string name, Transform parent, Action? onPlus)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent, raycast: onPlus != null);
            var view = root.gameObject.AddComponent<PetalsPill>();
            SoftShadow(layout, b => b, b => b.Height / 2f, 0.2f, 0.08f);
            Image lip = RoundRect("Lip", root, UiTheme.Of(C.CreamLip));
            Image face = RoundGradient("Face", root, C.CreamTop, C.CreamFace);
            Image line = RoundRing("Line", root, UiTheme.Of(C.CreamLine), null, b => Mathf.Max(Units(2f), b.Height * 0.04f));
            layout.Add(lip.rectTransform, b => b);
            layout.Add(face.rectTransform, b => new Box(b.Left, b.Top, b.Right, b.Bottom - (b.Height * 0.09f)));
            layout.Add(line.rectTransform, b => b);
            Image icon = PetalIcon("Petal", root);
            layout.Add(icon.rectTransform, b => Box.FromCenter(b.Left + (b.Height * 0.42f), b.CenterY - (b.Height * 0.045f) - (b.Height * 0.02f), b.Height * 1.08f, b.Height * 1.08f));
            TextMeshProUGUI balance = KitLabel("Balance", root, "0", DesignTokens.Type.Count, TextLook.Plain(C.InkBrown));
            view.Balance = balance;
            GardenButton plus = IconFace("Plus", root, GardenLook.Green, b => b.Height / 2f, square: true);
            Image glyph = GardenGlyph(plus, plus.Content, "ui.plus");
            BoxLayout.On(plus.Content).Add(glyph.rectTransform, f => Box.FromCenter(f.CenterX, f.CenterY, plus.IconSide * 0.6f, plus.IconSide * 0.6f));
            layout.Add((RectTransform)plus.transform, b => Box.FromCenter(b.Right - (b.Height * 0.3f), b.CenterY, b.Height, b.Height));
            layout.Watch(balance).Then(b =>
            {
                float h = b.Height;
                bool showsPlus = view.Plus != null && view.Plus.activeSelf;
                float right = showsPlus ? b.Right - (h * 0.75f) : b.Right - (h * 0.3f);
                float left = b.Left + (h * 0.95f);
                float faceCenter = (b.Top + b.Bottom - (h * 0.09f)) / 2f;
                if (showsPlus)
                {
                    KitText.Place(balance, DesignTokens.Type.Count, (left + right) / 2f, faceCenter, h * 0.5f, right - left);
                    return;
                }

                // Without the "+" (the Store still locked) the amount starts right after the lotus, as on the reference,
                // instead of floating in the pill's middle.
                float measured = KitText.Measure(balance, h * 0.5f);
                float width = Mathf.Min(measured > 0f ? measured : right - left, right - left);
                KitText.Place(balance, DesignTokens.Type.Count, left + (width / 2f), faceCenter, h * 0.5f, width + 1f);
            });
            view.Plus = plus.gameObject;
            view.Relayout = layout.Apply;
            if (onPlus != null)
            {
                var relay = root.gameObject.AddComponent<PressRelay>();
                relay.Target = plus;
                var button = root.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = root.GetComponent<Image>();
                button.onClick.AddListener(() =>
                {
                    GameFeedback.Current?.Play(SoundCue.Click);
                    onPlus();
                });
                plus.Button = button;
            }

            return view;
        }

        // ---- Cards and sheets (spec 005 §3.5) ----

        /// <summary>
        /// Parchment (spec 005 §3.5; cards, the sheet, the tray panel, the slot band, toasts): a soft shadow
        /// <paramref name="depthUnits"/> below, the <c>parchment.top</c> to <c>parchment.bottom</c> gradient with a warm
        /// aged band over its outer 6% (fading inward), a thin <c>parchment.edge</c> line inside it and a thinner
        /// <c>parchment.line</c> outline (0.8 × <paramref name="frameUnits"/>). No wooden frame. Children drawn on the
        /// parchment go into the returned root, after its layers.
        /// </summary>
        public static Image Paper(string name, Transform parent, float radiusUnits, float frameUnits, float depthUnits, bool raycast = true) =>
            Paper(name, parent, _ => Units(radiusUnits), frameUnits, depthUnits, raycast);

        /// <summary><see cref="Paper(string, Transform, float, float, float, bool)"/> with a radius from the paper's own box.</summary>
        public static Image Paper(string name, Transform parent, Func<Box, float> radius, float frameUnits, float depthUnits, bool raycast = true)
        {
            Image root = UiFactory.CreateImage(name, parent, null, Color.clear, raycast);
            BoxLayout layout = BoxLayout.On(root.rectTransform);
            float depth = Units(depthUnits);
            float line = Units(frameUnits) * 0.8f;
            float R(Box b) => Mathf.Min(radius(b), b.Height / 2f);
            float r = 0f;
            Image shadowFar = RoundRect("ShadowFar", root.transform, UiTheme.Of(C.GardenShadow.WithAlpha(0.18f)), _ => r);
            Image shadowNear = RoundRect("ShadowNear", root.transform, UiTheme.Of(C.GardenShadow.WithAlpha(0.14f)), _ => r);
            Image paper = RoundGradient("Paper", root.transform, C.ParchmentTop, C.ParchmentBottom, _ => r);
            Image aged = RoundFade("Aged", root.transform, UiTheme.Of(C.ParchmentEdge.WithAlpha(0.42f)), _ => r - line, b => (Mathf.Min(b.Width, b.Height) + (2f * line)) * 0.005f * 12.8f);
            float innerWidth = 0f;
            float inner = 0f;
            Image innerLine = RoundRing("Inner", root.transform, UiTheme.Of(C.ParchmentEdge), _ => Mathf.Max(0f, r - inner + (innerWidth / 2f)), _ => innerWidth);
            Image outline = RoundRing("Line", root.transform, UiTheme.Of(C.ParchmentLine), _ => r, _ => line);
            layout.Add(shadowFar.rectTransform, b =>
            {
                r = R(b);
                return b.Offset(0f, depth * 1.6f).Inset(-Units(3f), 0f);
            });
            layout.Add(shadowNear.rectTransform, b => b.Offset(0f, depth * 0.7f));
            layout.Add(paper.rectTransform, b => b);
            layout.Add(aged.rectTransform, b => b.Inset(line));
            layout.Add(innerLine.rectTransform, b =>
            {
                innerWidth = Mathf.Max(Units(1f), line * 0.4f);
                inner = line + Mathf.Max(Units(3f), Mathf.Min(b.Width, b.Height) * 0.012f);
                return b.Inset(inner - (innerWidth / 2f));
            });
            layout.Add(outline.rectTransform, b => b);
            layout.Then(_ =>
            {
                foreach (Image image in new[] { shadowFar, shadowNear, paper, aged, innerLine, outline })
                {
                    image.GetComponent<RoundShape>().Apply();
                }
            });
            return root;
        }

        /// <summary>
        /// A popup card (FR-007, spec 005 §3.5): a dimmed backdrop, parchment (radius 8% of its width, at least
        /// <c>radius.card_min</c>), the title in <c>type.title</c> <c>ink.title</c> or, with <paramref name="sign"/>, a wooden
        /// sign across the card's top edge with that decoration (the Store, the milestone), and the cream round close
        /// button over the top-right corner when <paramref name="onClose"/> is given (hidden while a card opened after it is
        /// open: one close button per stack, <see cref="CardStackMember"/>). <paramref name="contentHeight"/> is in
        /// reference units. <paramref name="header"/> is kept for older callers: the reference has no header band.
        /// </summary>
        public static CardView Card(string name, Transform parent, string title, float contentHeight, Action? onClose, TypeStyle? titleStyle = null, ColorSet? header = null, SignDecor? sign = null)
        {
            (float w, float h, Insets insets) = ScreenFrame();
            CardRegions regions = ScreenLayout.Card(w, h, insets, contentHeight);
            Box screen = new Box(0f, 0f, w, h);

            Image shade = UiFactory.CreateImage(name, parent, null, UiTheme.PanelShade, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            Image card = Paper("Card", shade.transform, b => Mathf.Max(Units(DesignTokens.Radius.CardMin), b.Width * DesignTokens.Radius.Card), DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            PlaceBox(card.rectTransform, regions.Card, screen);
            card.gameObject.AddComponent<PopMotion>();
            BoxLayout layout = BoxLayout.On(card.rectTransform);

            TypeStyle style = titleStyle ?? DesignTokens.Type.Title;
            TextMeshProUGUI titleLabel;
            Box titleLocal = ToLocal(regions.Title, regions.Card);
            if (sign.HasValue)
            {
                WoodSignView signView = WoodSign("Title", card.transform, title, style, sign.Value);
                titleLabel = signView.Label;
                layout.Watch(titleLabel).Add((RectTransform)signView.transform, b =>
                {
                    float signHeight = Units(118f);
                    float width = Mathf.Min(b.Width * 0.78f, KitText.Measure(signView.Label, Units(style.Size)) + (signHeight * 1.4f));
                    return Box.FromCenter(b.CenterX, titleLocal.CenterY - Units(30f), width, signHeight);
                });
            }
            else
            {
                titleLabel = KitLabel("Title", card.transform, title, style, TextLook.Plain(C.InkTitle));
                Box closeLocal = ToLocal(regions.Close, regions.Card);
                layout.Then(b =>
                {
                    float closeRoom = onClose != null ? closeLocal.Width + Units(20f) : 0f;
                    float titleWidth = Mathf.Min(titleLocal.Width, b.Width - (2f * closeRoom) - Units(60f));
                    KitText.Place(titleLabel, style, titleLocal.CenterX, titleLocal.CenterY, Units(style.Size), titleWidth);
                });
            }

            // Only the top card of a stack shows its close button (CardStackMember).
            CardStackMember member = shade.gameObject.AddComponent<CardStackMember>();
            if (onClose != null)
            {
                // Over the top-right corner, as in the reference.
                Button close = RoundIconButton("Close", card.transform, "ui.close", onClose);
                float shift = regions.Close.Width * 0.3f;
                PlaceBox((RectTransform)close.transform, regions.Close.Offset(shift, -shift), regions.Card);
                member.Close = close.gameObject;
            }

            RectTransform body = PlaceBox(UiFactory.CreateRect("Body", card.transform), regions.Body, regions.Card);
            return new CardView(shade.gameObject, card.rectTransform, titleLabel, body, regions);
        }

        /// <summary>
        /// The jam bottom sheet (frame 10, spec 005 §4.3): parchment that rises from the bottom and settles with a small
        /// bounce (spec 003 FR-018), with a grip, the title in <c>ink.title</c> and the subtitle in <c>ink.brown_soft</c>,
        /// over a light shade that keeps the board visible (spec 001 FR-027).
        /// </summary>
        public static SheetView Sheet(string name, Transform parent, string title, string subtitle, float contentHeight)
        {
            (float w, float h, Insets insets) = ScreenFrame();
            SheetRegions regions = ScreenLayout.Sheet(w, h, insets, contentHeight);
            Box screen = new Box(0f, 0f, w, h);

            // A light shade that takes no taps: the board stays visible and the top bar (Pause) stays usable.
            Image shade = UiFactory.CreateImage(name, parent, null, new Color(UiTheme.PanelShade.r, UiTheme.PanelShade.g, UiTheme.PanelShade.b, 0.3f), raycast: false);
            UiFactory.Stretch(shade.rectTransform);
            Image sheet = Paper("Sheet", shade.transform, 64f, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            PlaceBox(sheet.rectTransform, regions.Sheet, screen);
            sheet.gameObject.AddComponent<SheetMotion>();
            Image grip = RoundRect("Grip", sheet.transform, UiTheme.Of(C.ParchmentEdge.Darken(0.12f)));
            PlaceBox(grip.rectTransform, regions.Grip, regions.Sheet);
            TextMeshProUGUI titleLabel = Label("Title", sheet.transform, title, DesignTokens.Type.Title, UiTheme.Of(C.InkTitle), look: TextLook.Plain(C.InkTitle));
            PlaceBox(titleLabel.rectTransform, regions.Title, regions.Sheet);
            TextMeshProUGUI subtitleLabel = Label("Subtitle", sheet.transform, subtitle, DesignTokens.Type.Body, UiTheme.Of(C.InkBrownSoft));
            PlaceBox(subtitleLabel.rectTransform, regions.Subtitle, regions.Sheet);
            RectTransform body = PlaceBox(UiFactory.CreateRect("Body", sheet.transform), regions.Body, regions.Sheet);
            return new SheetView(shade.gameObject, sheet.rectTransform, titleLabel, subtitleLabel, body, regions);
        }

        /// <summary>
        /// A list row (Store, Leaderboard, Settings; spec 005 §3.5): a cream rounded panel (radius 25% of its height) with a
        /// <c>cream.line</c> outline and a lip; the player's own row is raised and green-tinted. Children go into the
        /// returned root.
        /// </summary>
        public static Image Row(string name, Transform parent, bool highlighted)
        {
            Image root = UiFactory.CreateImage(name, parent, null, Color.clear);
            BoxLayout layout = BoxLayout.On(root.rectTransform);
            float R(Box b) => b.Height * DesignTokens.Radius.Row;
            float line = Units(DesignTokens.Garden.OutlineWidth);
            Rgba lipColor = highlighted ? GardenLook.Green.Lip.Mix(C.CreamLip, 0.4f) : C.CreamLip;
            Image lip = RoundRect("Lip", root.transform, UiTheme.Of(lipColor), R);
            Image face = highlighted
                ? RoundGradient("Panel", root.transform, C.SurfaceRowHighlight.Lighten(0.4f), C.SurfaceRowHighlight, R)
                : RoundGradient("Panel", root.transform, C.CreamTop, C.CreamFace, R);
            Image outline = RoundRing("Line", root.transform, UiTheme.Of(highlighted ? GardenLook.Green.Lip : C.CreamLine), R, _ => line);
            layout.Add(lip.rectTransform, b => b.Offset(0f, Units(highlighted ? 6f : 4f)));
            layout.Add(face.rectTransform, b => b);
            layout.Add(outline.rectTransform, b => b);
            return root;
        }

        /// <summary>
        /// A sunk well (spec 005 §3.5: the jam's slot row, unselected tabs, toggle tracks, empty plates): <paramref name="fill"/>
        /// with a shadow along its top (45% of its height, at most 40 units) and a <c>parchment.edge</c> outline. The
        /// returned image is the fill; children go into it.
        /// </summary>
        public static Image Well(string name, Transform parent, Color fill, float? radiusUnits = null, bool raycast = false)
        {
            Func<Box, float>? radius = radiusUnits.HasValue ? b => Units(radiusUnits.Value) : (Func<Box, float>?)null;
            Image root = RoundRect(name, parent, fill, radius, raycast);
            BoxLayout layout = BoxLayout.On(root.rectTransform);
            Image shadow = RoundRect("Shadow", root.transform, Color.white, radius);
            Gradient(shadow, UiTheme.Of(C.GardenShadow.WithAlpha(0.16f)), UiTheme.Of(C.GardenShadow.WithAlpha(0f)));
            Image outline = RoundRing("Line", root.transform, UiTheme.Of(C.ParchmentEdge.Darken(0.08f)), radius, _ => Units(DesignTokens.Garden.OutlineWidth));
            layout.Add(shadow.rectTransform, b => b);
            layout.Add(outline.rectTransform, b => b);
            layout.Then(b => shadow.GetComponent<VerticalGradient>().Stop = Mathf.Clamp01(Mathf.Min(b.Height * 0.45f, Units(40f)) / Mathf.Max(1f, b.Height)));
            return root;
        }

        /// <summary>A sunk well in <c>parchment.well</c> (the reference's inset rows).</summary>
        public static Image Well(string name, Transform parent, float? radiusUnits = null, bool raycast = false) =>
            Well(name, parent, UiTheme.Of(C.ParchmentWell), radiusUnits, raycast);

        /// <summary>
        /// Tabs (Store, Wardrobe; FR-016, spec 005): the selected one a glossy green button on a cream plate with a white
        /// label, the others parchment wells with brown labels.
        /// </summary>
        public static TabsView Tabs(string name, Transform parent, string[] labels, Action<int> onSelect)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect(name, parent));
            var view = root.gameObject.AddComponent<TabsView>();
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                RectTransform cell = UiFactory.Place(UiFactory.CreateRect("Tab" + i, root), i / (float)labels.Length, 0f, (i + 1) / (float)labels.Length, 1f);
                cell.offsetMin = new Vector2(Units(8f), 0f);
                cell.offsetMax = new Vector2(-Units(8f), 0f);
                GardenButton on = Garden("On", cell, GardenLook.Green, 100f, gloss: true);
                UiFactory.Stretch((RectTransform)on.transform);
                TextMeshProUGUI onLabel = KitLabel("Label", on.Content, labels[i], DesignTokens.Type.ButtonSecondary, TextLook.OnColor(GardenLook.Green));
                on.Track(onLabel, DesignTokens.Type.ButtonSecondary, TextLook.OnColor);
                BoxLayout.On(on.Content).Watch(onLabel).Then(f => KitText.Place(onLabel, DesignTokens.Type.ButtonSecondary, f.CenterX, f.CenterY, Units(DesignTokens.Type.ButtonSecondary.Size), f.Width * 0.86f));
                Clickable(on, () => onSelect(index));

                Image off = Well("Off", cell, raycast: true);
                Inset(off.rectTransform, Units(6f), Units(8f) - Units(2f), Units(6f), Units(8f) + Units(2f));
                TextMeshProUGUI offLabel = KitLabel("Label", off.transform, labels[i], DesignTokens.Type.ButtonSecondary, TextLook.Plain(C.InkBrown));
                BoxLayout.On(off.rectTransform).Watch(offLabel).Then(b => KitText.Place(offLabel, DesignTokens.Type.ButtonSecondary, b.CenterX, b.CenterY + Units(3f), Units(DesignTokens.Type.ButtonSecondary.Size), b.Width * 0.86f));
                var offButton = off.gameObject.AddComponent<Button>();
                offButton.transition = Selectable.Transition.None;
                offButton.targetGraphic = off;
                offButton.onClick.AddListener(() =>
                {
                    GameFeedback.Current?.Play(SoundCue.Click);
                    onSelect(index);
                });
                view.Add(on.gameObject, off.gameObject);
            }

            return view;
        }

        /// <summary>
        /// A switch (Settings; FR-016, spec 005 §4.3; the playtest's <c>Kit.Toggle</c>): off, a parchment well; on, the green
        /// set's glossy track (its lip color fading to its face, a shadow along the top half, a light band along the bottom
        /// and a green outline) with a white ✓ where the knob was, so the state never rests on the hue alone; the knob a
        /// domed cream cushion like the round buttons (one track height plus 10 units) at the off or on end.
        /// </summary>
        public static ToggleView Toggle(string name, Transform parent, Action onClick)
        {
            Image track = Well(name, parent, raycast: true);
            var view = track.gameObject.AddComponent<ToggleView>();
            RectTransform on = UiFactory.Stretch(UiFactory.CreateRect("On", track.transform));
            Image onFace = RoundGradient("Face", on, GardenLook.Green.Lip, GardenLook.Green.Face);
            UiFactory.Stretch(onFace.rectTransform);
            Image onShadow = RoundRect("Shadow", on, Color.white);
            Gradient(onShadow, UiTheme.Of(C.GardenShadow.WithAlpha(0.22f)), UiTheme.Of(C.GardenShadow.WithAlpha(0f)));
            onShadow.GetComponent<VerticalGradient>().Stop = 0.5f;
            UiFactory.Stretch(onShadow.rectTransform);
            Image onShine = RoundRect("Shine", on, Color.white);
            Gradient(onShine, UiTheme.Of(GardenLook.Green.Top.WithAlpha(0f)), UiTheme.Of(GardenLook.Green.Top.WithAlpha(0.55f)));
            Image onLine = RoundRing("Line", on, UiTheme.Of(GardenLook.Green.Line), null, _ => Units(DesignTokens.Garden.OutlineWidth));
            UiFactory.Stretch(onLine.rectTransform);
            Image check = ShapeImage("Check", on, "ui.check", Rgba.White);
            BoxLayout.On(on)
                .Add(onShine.rectTransform, b => new Box(b.Left + (b.Height * 0.3f), b.Bottom - (b.Height * 0.34f), b.Right - (b.Height * 0.3f), b.Bottom - (b.Height * 0.12f)))
                .Add(check.rectTransform, b => Box.FromCenter(b.Left + (b.Height * 0.56f), b.CenterY, b.Height * 0.5f, b.Height * 0.5f));

            GardenButton knob = IconFace("Knob", track.transform, GardenLook.White, b => Mathf.Min(b.Width, b.Height) / 2f, square: true);
            var relay = track.gameObject.AddComponent<PressRelay>();
            relay.Target = knob;
            view.Init(on.gameObject, (RectTransform)knob.transform);
            var button = track.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = track;
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick();
            });
            knob.Button = button;
            return view;
        }

        /// <summary>A reward item of the milestone card and the jam sheet: an icon over an amount ("+200", "×2").</summary>
        public static void IconWithAmount(Transform parent, string name, Sprite icon, Color iconColor, string amount, bool petal)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect(name, parent));
            Image image = UiFactory.CreateImage("Icon", root, icon, iconColor);
            image.preserveAspect = true;
            UiFactory.Place(image.rectTransform, 0.15f, 0.36f, 0.85f, 1f);
            TextMeshProUGUI text = Label("Amount", root, amount, DesignTokens.Type.Count, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
            UiFactory.Place(text.rectTransform, petal ? 0f : 0.05f, 0f, petal ? 0.78f : 0.95f, 0.34f);
            if (petal)
            {
                Image symbol = PetalIcon("Petal", root);
                UiFactory.Place(symbol.rectTransform, 0.78f, 0.04f, 0.98f, 0.3f);
            }
        }

        /// <summary>"+35" followed by the Petal symbol, for costs and rewards.</summary>
        public static string PetalAmount(long amount, bool plus) =>
            plus ? NumberText.Plus(amount) : NumberText.Group(amount);

        internal static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>A popup card built by <see cref="UiKit.Card"/>.</summary>
    public sealed class CardView
    {
        public CardView(GameObject root, RectTransform card, TextMeshProUGUI title, RectTransform body, CardRegions regions)
        {
            Root = root;
            CardRect = card;
            Title = title;
            Body = body;
            Regions = regions;
        }

        /// <summary>The dimmed backdrop holding the card: show or hide this.</summary>
        public GameObject Root { get; }

        public RectTransform CardRect { get; }

        public TextMeshProUGUI Title { get; }

        public RectTransform Body { get; }

        public CardRegions Regions { get; }
    }

    /// <summary>A bottom sheet built by <see cref="UiKit.Sheet"/>.</summary>
    public sealed class SheetView
    {
        public SheetView(GameObject root, RectTransform sheet, TextMeshProUGUI title, TextMeshProUGUI subtitle, RectTransform body, SheetRegions regions)
        {
            Root = root;
            SheetRect = sheet;
            Title = title;
            Subtitle = subtitle;
            Body = body;
            Regions = regions;
        }

        public GameObject Root { get; }

        public RectTransform SheetRect { get; }

        public TextMeshProUGUI Title { get; }

        public TextMeshProUGUI Subtitle { get; }

        public RectTransform Body { get; }

        public SheetRegions Regions { get; }
    }

    /// <summary>The Petals balance pill (<see cref="UiKit.PetalsPill"/>).</summary>
    public sealed class PetalsPill : MonoBehaviour
    {
        public TextMeshProUGUI Balance { get; set; } = null!;

        public GameObject Plus { get; set; } = null!;

        /// <summary>Lays the pill out again (the balance takes the room of a hidden "+").</summary>
        internal Action? Relayout { get; set; }

        private long _shown = -1;
        private CountUp? _count;

        /// <summary>Shows the balance; a rise counts up from the old balance (spec 003 FR-020).</summary>
        public void Show(long petals, bool storeUnlocked)
        {
            if (_shown >= 0 && petals > _shown)
            {
                _count ??= CountUp.On(Balance, NumberText.Group);
                _count.Run(_shown, petals);
            }
            else
            {
                Balance.text = NumberText.Group(petals);
            }

            _shown = petals;
            if (Plus.activeSelf != storeUnlocked)
            {
                Plus.SetActive(storeUnlocked);
                Relayout?.Invoke();
            }
        }
    }

    /// <summary>Keeps a sliced circle sprite fully rounded at the rect's height (a pill).</summary>
    public sealed class PillShape : MonoBehaviour
    {
        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            var image = GetComponent<Image>();
            RectTransform rect = (RectTransform)transform;
            if (image != null && rect.rect.height > 0f)
            {
                image.pixelsPerUnitMultiplier = 32f / (rect.rect.height / 2f);
            }
        }
    }

    /// <summary>
    /// The press of elements that are not garden buttons (pods, tiles; spec 003 FR-017): a squash while the finger is
    /// down, then a spring back with one overshoot, on unscaled time so 2× never changes it.
    /// </summary>
    public sealed class PressMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private bool _down;
        private float _releasedAt = -10f;

        /// <summary>Tiles squash a little more (contracts/booster-tile.md).</summary>
        public bool Tile { get; set; }

        /// <summary>Whether the finger is down on the element (pods sink their frame, spec 005 §3.7).</summary>
        public bool Down => _down;

        public void OnPointerDown(PointerEventData e) => _down = true;

        public void OnPointerUp(PointerEventData e) => Release();

        public void OnPointerExit(PointerEventData e) => Release();

        private void Release()
        {
            if (_down)
            {
                _down = false;
                _releasedAt = Time.unscaledTime;
            }
        }

        private float _applied = float.NaN;

        private void Update()
        {
            // Only touch the transform when the press changes, so idle elements never dirty the canvas (FR-027).
            float depth = GardenLook.PressDepth(_down, Time.unscaledTime - _releasedAt);
            if (depth == _applied)
            {
                return;
            }

            _applied = depth;
            (float sx, float sy) = GardenLook.Squash(depth, Tile);
            transform.localScale = new Vector3(sx, sy, 1f);
        }
    }

    /// <summary>
    /// A garden element built by <see cref="UiKit.Garden"/>, <see cref="UiKit.PrimaryButton"/>,
    /// <see cref="UiKit.IconFace"/> and the jam choices (spec 003 FR-006, FR-007, FR-012, FR-017, FR-019; spec 005 §3.3):
    /// <list type="bullet">
    /// <item><description>the press: the face sinks into its lip at once, darkens by 8% and the body squashes about its
    /// bottom center; on release it springs back with one overshoot; the click is never delayed;</description></item>
    /// <item><description>the breath of the one waiting button (<see cref="Breathe"/>);</description></item>
    /// <item><description>the disabled look of a button that is not interactable: greyed (the same shapes, no color, no
    /// sheen, no press) or faded to 55% (<see cref="FadeWhenDisabled"/>, the cream secondary button).</description></item>
    /// </list>
    /// Everything runs on unscaled time, so the 2× setting never changes it (FR-021). The level sign uses it only to take
    /// a Super Hard color (<see cref="SetColors"/>).
    /// </summary>
    public sealed class GardenButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private readonly List<(TextMeshProUGUI Label, TypeStyle Style, Func<ColorSet, TextLook>? Look)> _labels = new List<(TextMeshProUGUI, TypeStyle, Func<ColorSet, TextLook>?)>();
        private readonly List<(Graphic Graphic, Func<ColorSet, float, (Rgba Top, Rgba Bottom)> Colors)> _extras = new List<(Graphic, Func<ColorSet, float, (Rgba, Rgba)>)>();
        private Image? _line;
        private Image? _lip;
        private RectTransform? _topBox;
        private CanvasGroup? _sheen;
        private CanvasGroup? _fade;
        private BoxLayout? _topLayout;
        private RectTransform _content = null!;
        private WoodSignView? _sign;
        private float _lineWidth;
        private float _lipHeight;
        private float _travel;
        private bool _down;
        private float _releasedAt = -10f;
        private bool? _shownEnabled;
        private ColorSet? _labelSet;
        private float _appliedDark = -1f;
        private float _appliedDepth = float.NaN;
        private float _appliedBreath = float.NaN;
        private float _appliedShift = float.NaN;

        /// <summary>The element's color set.</summary>
        public ColorSet Set { get; private set; } = GardenLook.Green;

        /// <summary>The face's top part (the button's target graphic).</summary>
        public Image Top { get; private set; } = null!;

        /// <summary>
        /// Where labels and glyphs go: the face above its lip (inside the cushion's rim on icon faces), drawn over the face's
        /// outline and moving with the press.
        /// </summary>
        public RectTransform Content => _content;

        /// <summary>The element's body: everything but the root; the press squashes it about its bottom center.</summary>
        public RectTransform Body { get; private set; } = null!;

        /// <summary>The button this element is, if any (greys out when not interactable).</summary>
        public Button? Button { get; set; }

        /// <summary>Whether this is the screen's one waiting button, breathing gently (FR-019).</summary>
        public bool Breathe { get; set; }

        /// <summary>Booster tiles squash a little more (contracts/booster-tile.md "States").</summary>
        public bool TileSquash { get; set; }

        /// <summary>Whether a disabled element takes the greyed set (the default) or keeps its colors.</summary>
        public bool GreyWhenDisabled { get; set; } = true;

        /// <summary>Whether a disabled element fades to 55% (the cream secondary button, the jam choices).</summary>
        public bool FadeWhenDisabled { get; set; }

        /// <summary>The face's corner radius in canvas units (very large for pills).</summary>
        public float FaceRadius { get; private set; } = float.MaxValue;

        /// <summary>The face's outline width in canvas units.</summary>
        public float LineWidth => _lineWidth;

        /// <summary>The cushion's rim of an icon face (9% of its shorter side), in canvas units.</summary>
        public float Rim { get; private set; }

        /// <summary>The shorter side of an icon face's box in canvas units (glyph sizes are shares of it).</summary>
        public float IconSide { get; private set; }

        /// <summary>The top gradient for a set and a press darkening (the cream cushion is deeper toward its edge).</summary>
        internal Func<ColorSet, float, (Rgba Top, Rgba Bottom)> TopColors { get; set; } = (s, d) => (s.Top.Darken(d), s.Face.Darken(d));

        /// <summary>Raised after the element takes its colors: the shown set and whether it is enabled (grey icons).</summary>
        public event Action<ColorSet, bool>? Recolored;

        internal void Attach(ColorSet set, RectTransform body)
        {
            Set = set;
            Body = body;
            _content = body;
        }

        internal void BuildFace(Image line, Image lip, RectTransform topBox, Image top, CanvasGroup? sheen, RectTransform content, BoxLayout topLayout)
        {
            _line = line;
            _lip = lip;
            _topBox = topBox;
            Top = top;
            _sheen = sheen;
            _content = content;
            _topLayout = topLayout;
            Recolor(true, 0f);
        }

        /// <summary>The level sign as a garden element: its letters take the Super Hard color (<see cref="UiKit.LevelPill"/>).</summary>
        internal void AttachSign(WoodSignView sign)
        {
            _sign = sign;
            Body = (RectTransform)sign.transform;
            _content = Body;
            Top = sign.Plank;
            Recolor(true, 0f);
        }

        /// <summary>A gradient part of the face recolored with the set and the press (domes, gloss, the colored cushion).</summary>
        internal void AddExtra(Graphic graphic, Func<ColorSet, float, (Rgba Top, Rgba Bottom)> colors)
        {
            _extras.Add((graphic, colors));
            (Rgba top, Rgba bottom) = colors(Set, 0f);
            UiKit.Gradient(graphic, UiTheme.Of(top), UiTheme.Of(bottom));
        }

        /// <summary>
        /// The face's geometry in canvas units, from its box (the layouts call it): the outline width, the lip, how far the
        /// face sinks when pressed, the corner radius, and for icon faces the cushion's rim and the box's shorter side.
        /// </summary>
        public void SetGeometry(float line, float lip, float travel, float radius, float rim = 0f, float iconSide = 0f)
        {
            _lineWidth = line;
            _lipHeight = lip;
            _travel = travel;
            FaceRadius = radius;
            Rim = rim;
            IconSide = iconSide;
            _appliedShift = float.NaN;
            ApplyGeometry(CurrentShift());
            if (_line != null)
            {
                foreach (Graphic graphic in new Graphic[] { _line, _lip!, Top })
                {
                    RoundShape shape = graphic.GetComponent<RoundShape>();
                    if (shape != null)
                    {
                        shape.Apply();
                    }
                }
            }

            _topLayout?.Apply();
        }

        /// <summary>A label in the element's look, recolored with it (by default <see cref="GardenLook.LabelOn"/>).</summary>
        public void Track(TextMeshProUGUI label, TypeStyle style) => Track(label, style, null);

        /// <summary>A label in a look of the element's shown set (the primary button's <see cref="TextLook.OnGloss"/>).</summary>
        public void Track(TextMeshProUGUI label, TypeStyle style, Func<ColorSet, TextLook>? look)
        {
            _labels.Add((label, style, look));
            _labelSet = null;
            _shownEnabled = null;
        }

        /// <summary>Gives the element another color set (the level sign on Super Hard, the HARD badge, tabs).</summary>
        public void SetColors(ColorSet set)
        {
            Set = set;
            _shownEnabled = null;
            _labelSet = null;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (Interactable && _sign == null)
            {
                _down = true;
            }
        }

        public void OnPointerUp(PointerEventData e) => Release();

        public void OnPointerExit(PointerEventData e) => Release();

        private bool Interactable => Button == null || Button.interactable;

        private void Release()
        {
            if (_down)
            {
                _down = false;
                _releasedAt = Time.unscaledTime;
            }
        }

        private float CurrentShift()
        {
            float depth = float.IsNaN(_appliedDepth) ? 0f : _appliedDepth;
            return _travel * Mathf.Clamp(depth, -0.25f, 1f);
        }

        private void Recolor(bool enabled, float dark)
        {
            if (_sign != null)
            {
                bool super = ReferenceEquals(Set, GardenLook.Lilac) || ReferenceEquals(Set, GardenLook.Purple);
                // badge.super_hard darkened 0.35 keeps its contrast on the pale wood (the playtest's Kit.LevelPill).
                _sign.SetLetters(super ? DesignTokens.Colors.BadgeSuperHard.Darken(0.35f) : DesignTokens.Colors.InkBrown);
                _shownEnabled = enabled;
                _appliedDark = dark;
                return;
            }

            ColorSet shown = enabled || !GreyWhenDisabled ? Set : Set.Disabled();
            if (_line != null && _lip != null)
            {
                _line.color = UiTheme.Of(shown.Line);
                _lip.color = UiTheme.Of(shown.Lip.Darken(dark));
                (Rgba top, Rgba bottom) = TopColors(shown, dark);
                UiKit.Gradient(Top, UiTheme.Of(top), UiTheme.Of(bottom));
            }

            foreach ((Graphic graphic, Func<ColorSet, float, (Rgba Top, Rgba Bottom)> colors) in _extras)
            {
                (Rgba top, Rgba bottom) = colors(shown, dark);
                UiKit.Gradient(graphic, UiTheme.Of(top), UiTheme.Of(bottom));
            }

            if (_sheen != null)
            {
                _sheen.gameObject.SetActive(enabled);
                _sheen.alpha = Mathf.Max(0f, 1f - (dark * 4f));
            }

            if (!ReferenceEquals(_labelSet, shown))
            {
                foreach ((TextMeshProUGUI label, TypeStyle style, Func<ColorSet, TextLook>? look) in _labels)
                {
                    UiKit.ApplyLook(label, style, look != null ? look(shown) : GardenLook.LabelOn(shown));
                }

                _labelSet = shown;
            }

            // The fade group is made the first time a fading element is disabled; others never need one.
            if (_fade == null && FadeWhenDisabled && !enabled)
            {
                _fade = gameObject.AddComponent<CanvasGroup>();
            }

            if (_fade != null)
            {
                _fade.alpha = !enabled && FadeWhenDisabled ? 0.55f : 1f;
            }

            bool wasEnabled = _shownEnabled ?? !enabled;
            _shownEnabled = enabled;
            _appliedDark = dark;
            if (wasEnabled != enabled || dark == 0f)
            {
                Recolored?.Invoke(shown, enabled);
            }
        }

        private void ApplyGeometry(float shift)
        {
            if (_line == null || _lip == null || _topBox == null)
            {
                return;
            }

            // The playtest's Kit.Face: the lip fills the face from max(0, shift) down, the top sits `shift` lower and `lip`
            // short of the bottom, and the outline goes around both (from the top's top edge down).
            _line.rectTransform.offsetMin = Vector2.zero;
            _line.rectTransform.offsetMax = new Vector2(0f, -shift);
            _lip.rectTransform.offsetMin = Vector2.zero;
            _lip.rectTransform.offsetMax = new Vector2(0f, -Mathf.Max(0f, shift));
            _topBox.offsetMin = new Vector2(0f, _lipHeight - shift);
            _topBox.offsetMax = new Vector2(0f, -shift);
            _content.offsetMin = new Vector2(Rim, _lipHeight - shift + Rim);
            _content.offsetMax = new Vector2(-Rim, -shift - Rim);
            _appliedShift = shift;
        }

        private void Update()
        {
            bool enabled = Interactable;
            if (_sign != null)
            {
                if (_shownEnabled != enabled)
                {
                    Recolor(enabled, 0f);
                }

                return;
            }

            float depth = enabled ? GardenLook.PressDepth(_down, Time.unscaledTime - _releasedAt) : 0f;
            float dark = 0.08f * Mathf.Clamp01(depth);
            if (_shownEnabled != enabled || dark != _appliedDark)
            {
                Recolor(enabled, dark);
            }

            float breath = Breathe && enabled && depth == 0f ? GardenLook.Breathe(Time.unscaledTime) : 1f;

            // Only touch the rects when the press or the breath changes, so idle buttons never dirty the canvas (FR-027).
            if (depth == _appliedDepth && breath == _appliedBreath && !float.IsNaN(_appliedShift))
            {
                return;
            }

            _appliedDepth = depth;
            _appliedBreath = breath;
            ApplyGeometry(_travel * Mathf.Clamp(depth, -0.25f, 1f));
            (float sx, float sy) = GardenLook.Squash(depth, TileSquash);
            Body.localScale = new Vector3(sx, sy, 1f);
            transform.localScale = new Vector3(breath, breath, 1f);
        }
    }

    /// <summary>
    /// A vertical two-color gradient over a graphic's mesh (plates, faces, paper; spec 003 FR-007): <see cref="Top"/> at
    /// the top edge to <see cref="Bottom"/> at <see cref="Stop"/> of the height from the top (1: the bottom edge), then flat.
    /// </summary>
    public sealed class VerticalGradient : BaseMeshEffect
    {
        public Color Top { get; set; } = Color.white;

        public Color Bottom { get; set; } = Color.white;

        /// <summary>Where the gradient reaches <see cref="Bottom"/>, as a share of the height from the top (inner shadows).</summary>
        public float Stop { get; set; } = 1f;

        public override void ModifyMesh(VertexHelper vh)
        {
            int count = vh.currentVertCount;
            if (count == 0)
            {
                return;
            }

            var vertex = new UIVertex();
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                min = Mathf.Min(min, vertex.position.y);
                max = Mathf.Max(max, vertex.position.y);
            }

            float height = Mathf.Max(0.0001f, max - min);
            float stop = Mathf.Max(0.0001f, Stop);
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float fromTop = (max - vertex.position.y) / height;
                vertex.color = Color.Lerp(Top, Bottom, Mathf.Clamp01(fromTop / stop));
                vh.SetUIVertex(vertex, i);
            }
        }
    }

    /// <summary>Lays the decoration over a main button's corners from the button's height (spec 003 FR-011a).</summary>
    public sealed class DecorationLayout : MonoBehaviour
    {
        public RectTransform TopLeft { get; set; } = null!;

        public RectTransform BottomRight { get; set; } = null!;

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (TopLeft == null || BottomRight == null)
            {
                return;
            }

            Rect rect = ((RectTransform)transform).rect;
            (Box topLeft, Box bottomRight) = GardenLook.DecorationBoxes(new Box(0f, 0f, rect.width, rect.height));
            BoxLayout.Place(TopLeft, topLeft);
            BoxLayout.Place(BottomRight, bottomRight);
        }
    }

    /// <summary>Keeps a rect square at its height, centered where its anchors put it (older callers).</summary>
    public sealed class SquareInParent : MonoBehaviour
    {
        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            var rect = (RectTransform)transform;
            float h = rect.rect.height;
            float w = rect.rect.width;
            if (h > 0f && w > h)
            {
                float trim = (w - h) / 2f;
                rect.offsetMin = new Vector2(trim, rect.offsetMin.y);
                rect.offsetMax = new Vector2(-trim, rect.offsetMax.y);
            }
        }
    }

    /// <summary>Tabs built by <see cref="UiKit.Tabs"/>: shows the raised button for the selected tab, the well for the others.</summary>
    public sealed class TabsView : MonoBehaviour
    {
        private readonly List<(GameObject On, GameObject Off)> _tabs = new List<(GameObject, GameObject)>();

        public void Add(GameObject on, GameObject off) => _tabs.Add((on, off));

        public void Select(int index)
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].On.SetActive(i == index);
                _tabs[i].Off.SetActive(i != index);
            }
        }
    }

    /// <summary>A switch built by <see cref="UiKit.Toggle"/>: the green track and the knob at the right end when on.</summary>
    public sealed class ToggleView : MonoBehaviour
    {
        private GameObject _on = null!;
        private RectTransform _knob = null!;
        private bool _state;

        public void Init(GameObject on, RectTransform knob)
        {
            _on = on;
            _knob = knob;
            BoxLayout.On((RectTransform)transform).Add(knob, Knob);
            Show(false);
        }

        public void Show(bool on)
        {
            _state = on;
            _on.SetActive(on);
            BoxLayout.On((RectTransform)transform).Apply();
        }

        /// <summary>The knob: a square one track height plus 10 units, centered on the track's end, 2 units up.</summary>
        private Box Knob(Box track)
        {
            float size = track.Height + UiKit.Units(10f);
            float cx = _state ? track.Right - (track.Height / 2f) : track.Left + (track.Height / 2f);
            return Box.FromCenter(cx, track.CenterY - UiKit.Units(2f), size, size);
        }
    }

    /// <summary>
    /// A number that counts up to its value (spec 003 FR-020), from 0 or from where it was, on unscaled time; it never
    /// blocks anything.
    /// </summary>
    public sealed class CountUp : MonoBehaviour
    {
        private TextMeshProUGUI _label = null!;

        /// <summary>How a value is written ("+20 Petals").</summary>
        public Func<long, string> Format { get; set; } = NumberText.Group;
        private long _from;
        private long _to;
        private float _startedAt = -10f;

        public static CountUp On(TextMeshProUGUI label, Func<long, string> format)
        {
            var count = label.gameObject.AddComponent<CountUp>();
            count._label = label;
            count.Format = format;
            return count;
        }

        /// <summary>Counts from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public void Run(long from, long to)
        {
            _from = from;
            _to = to;
            _startedAt = Time.unscaledTime;
            _label.text = Format(from);
        }

        private void Update()
        {
            float since = Time.unscaledTime - _startedAt;
            if (since < 0f || since > DesignTokens.Motion.CountUp.Seconds + 0.1f)
            {
                return;
            }

            _label.text = Format(_from + GardenLook.CountUp(_to - _from, since));
        }
    }

    /// <summary>The pop-in of popup cards (motion.pop): one small overshoot, settled within 0.35 s; unscaled by 2×.</summary>
    public sealed class PopMotion : MonoBehaviour
    {
        private void OnEnable()
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(Run());
            }
        }

        private IEnumerator Run()
        {
            float seconds = DesignTokens.Motion.Pop.Seconds;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                float scale = k < 0.7f ? Mathf.Lerp(DesignTokens.Motion.Pop.Scale, 1.04f, k / 0.7f) : Mathf.Lerp(1.04f, 1f, (k - 0.7f) / 0.3f);
                transform.localScale = Vector3.one * scale;
                yield return null;
            }

            transform.localScale = Vector3.one;
        }
    }

    /// <summary>The bottom sheet sliding up (motion.sheet).</summary>
    public sealed class SheetMotion : MonoBehaviour
    {
        private void OnEnable()
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(Run());
            }
        }

        private IEnumerator Run()
        {
            RectTransform rect = (RectTransform)transform;
            Vector2 min = rect.anchorMin;
            Vector2 max = rect.anchorMax;
            float height = max.y - min.y;
            // Rises past its place by a little and settles back: one small bounce (spec 003 FR-018).
            float seconds = DesignTokens.Motion.Sheet.Seconds * 1.35f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = SheetRise(t / DesignTokens.Motion.Sheet.Seconds);
                float drop = height * (1f - k);
                rect.anchorMin = new Vector2(min.x, min.y - drop);
                rect.anchorMax = new Vector2(max.x, max.y - drop);
                yield return null;
            }

            rect.anchorMin = min;
            rect.anchorMax = max;
        }

        private static float SheetRise(float t)
        {
            if (t < 1f)
            {
                return (1f - ((1f - t) * (1f - t) * (1f - t))) * 1.03f;
            }

            float k = Mathf.Min(1f, (t - 1f) / 0.35f);
            return 1.03f - (0.03f * (1f - ((1f - k) * (1f - k) * (1f - k))));
        }
    }
}
