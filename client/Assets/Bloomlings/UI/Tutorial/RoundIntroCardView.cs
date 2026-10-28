using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Rounds;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Rounds;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Tutorial
{
    /// <summary>
    /// A round level's introduction card (spec 006 FR-045, the kit's <see cref="RoundIntroCards"/>; the playtest's
    /// <c>RoundPainter.Intro</c>): the mechanic's picture made of the level's own pieces (a hidden pod beside the same pod
    /// revealed and glowing, two pods joined by the link tube, or the stream with a plank), its title on the card's wooden sign,
    /// its lines (three for the explanation of the first meeting, one for the reminder of the second) and "Got it". The card
    /// is built again on each <see cref="Show"/>.
    /// </summary>
    public sealed class RoundIntroCardView : MonoBehaviour
    {
        private const float PictureUnits = 230f;
        private const float LineUnits = 58f;
        private const float ButtonUnits = 150f;

        private Transform _parent = null!;
        private GameObject? _card;
        private TextMeshProUGUI _probe = null!;

        public bool IsOpen => _card != null && _card.activeSelf;

        public static RoundIntroCardView Create(Transform parent)
        {
            RectTransform host = UiFactory.Stretch(UiFactory.CreateRect("RoundIntro", parent));
            var view = host.gameObject.AddComponent<RoundIntroCardView>();
            view._parent = host;
            view._probe = UiKit.Label("Probe", host, string.Empty, T.Body, Color.clear);
            return view;
        }

        /// <summary>Opens <paramref name="card"/>; its button calls <paramref name="onGotIt"/>.</summary>
        public void Show(RoundIntroCard card, Action onGotIt)
        {
            Hide();
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            float width = ScreenLayout.Card(w, h, insets, 0f).Body.Width * 0.94f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            var lines = new List<(string Text, bool First)>();
            for (int i = 0; i < card.LineKeys.Count; i++)
            {
                TypeStyle style = i == 0 ? T.ButtonSecondary : T.Body;
                foreach (string line in UiKit.BalancedLines(_probe, Loc.T(card.LineKeys[i]), UiKit.Units(style.Size), width))
                {
                    lines.Add((line, i == 0));
                }
            }

            float content = PictureUnits + (lines.Count * LineUnits) + ButtonUnits + 40f;
            CardView view = UiKit.Card("RoundIntroCard", _parent, Loc.T(card.TitleKey), content, null);
            _card = view.Root;
            Box body = view.Regions.Body;
            float y = body.Top + (10f * u);
            Picture(view.Body, card.Picture, new Box(body.Left, y, body.Right, y + ((PictureUnits - 20f) * u)), body);
            y += PictureUnits * u;
            foreach ((string text, bool first) in lines)
            {
                Rgba ink = first ? C.InkBrown : C.InkBrownSoft;
                TextMeshProUGUI label = UiKit.Label("Line", view.Body, text, first ? T.ButtonSecondary : T.Body, UiTheme.Of(ink), look: TextLook.Plain(ink));
                UiKit.PlaceBox(label.rectTransform, new Box(body.Left, y, body.Right, y + (LineUnits * u)), body);
                y += LineUnits * u;
            }

            float bw = Mathf.Min(body.Width * 0.7f, 520f * u);
            Button button = UiKit.PrimaryButton("GotIt", view.Body, Loc.T(card.ButtonKey), () =>
            {
                Hide();
                onGotIt();
            });
            UiKit.PlaceBox((RectTransform)button.transform, new Box(body.CenterX - (bw / 2f), y + (24f * u), body.CenterX + (bw / 2f), y + ((24f + 110f) * u)), body);
        }

        public void Hide()
        {
            if (_card != null)
            {
                Destroy(_card);
                _card = null;
            }
        }

        /// <summary>The card's picture in <paramref name="area"/> (screen pixels) of the body whose screen box is <paramref name="body"/>.</summary>
        private static void Picture(RectTransform parent, RoundIntroPicture picture, Box area, Box body)
        {
            RectTransform holder = UiKit.PlaceBox(UiFactory.CreateRect("Picture", parent), area, body);

            // The pieces in the holder's top-down canvas units.
            Box local = UiKit.ToLocal(area, area);
            float h = local.Height;
            float podH = h * 0.62f;
            float podW = podH * PodChip.Aspect;
            float gap = h * 0.5f;
            var left = Box.FromCenter(local.CenterX - ((podW + gap) / 2f), local.CenterY, podW, podH);
            var right = Box.FromCenter(local.CenterX + ((podW + gap) / 2f), local.CenterY, podW, podH);
            switch (picture)
            {
                case RoundIntroPicture.HiddenPod:
                {
                    Pod(holder, left, -1, 0);
                    Image arrow = UiKit.ShapeImage("Arrow", holder, "ui.chevron", C.InkBrownSoft);
                    BoxLayout.Place(arrow.rectTransform, Box.FromCenter(local.CenterX, local.CenterY, h * 0.28f, h * 0.28f));
                    Box frame = PodChip.In(right).Frame;
                    float s = Mathf.Min(frame.Width, frame.Height);
                    Color glow = UiTheme.Of(C.GardenGlow);
                    Image fill = UiKit.RoundRect("RevealGlow", holder, new Color(glow.r, glow.g, glow.b, 0.4f), b => Mathf.Min(b.Width, b.Height) * 0.3f);
                    BoxLayout.Place(fill.rectTransform, frame.Inset(-s * 0.14f));
                    Image ring = UiKit.RoundRing("RevealRing", holder, glow, b => Mathf.Min(b.Width, b.Height) * 0.25f, b => Mathf.Min(b.Width, b.Height) * 0.035f);
                    BoxLayout.Place(ring.rectTransform, frame.Inset(-s * 0.03f));
                    Pod(holder, right, 1, 12);
                    break;
                }

                case RoundIntroPicture.LinkedPair:
                {
                    Image tube = RoundTubes.Create("LinkTube", holder);
                    RoundTubes.Show(tube, LinkTube.Between(PodChip.In(left).Frame, RoundLook.ColorOf(0), PodChip.In(right).Frame, RoundLook.ColorOf(8)));
                    Pod(holder, left, 0, 20);
                    Pod(holder, right, 8, 14);
                    break;
                }

                default:
                {
                    float cell = h * 0.34f;
                    const int cells = 6;
                    float x0 = local.CenterX - (cell * cells / 2f);
                    int pixels = UiRaster.Quantize(Mathf.Max(8f, cell * UiKit.PixelsPerUnit));
                    for (int x = 0; x < cells; x++)
                    {
                        var above = new Box(x0 + (x * cell), local.CenterY - (cell * 1.5f), x0 + ((x + 1) * cell), local.CenterY - (cell * 0.5f));
                        var water = new Box(above.Left, above.Bottom, above.Right, above.Bottom + cell);
                        var below = new Box(above.Left, water.Bottom, above.Right, water.Bottom + cell);
                        BoxLayout.Place(UiKit.GrassCell("Grass", holder, x).rectTransform, above);
                        Image stream = UiFactory.CreateImage("Water", holder, RoundSprites.Water(x, pixels), Color.white);
                        stream.raycastTarget = false;
                        BoxLayout.Place(stream.rectTransform, water);
                        BoxLayout.Place(UiKit.GrassCell("Grass", holder, x + 3).rectTransform, below);
                    }

                    Box site = Box.FromCenter(local.CenterX + (cell * 0.5f), local.CenterY, cell, cell);
                    Image plank = UiKit.WoodPlank("Bridge", holder, 0.3f, 3);
                    BoxLayout.Place(plank.rectTransform, Box.FromCenter(site.CenterX, site.CenterY, cell * (1f + (2f * RoundBoardLayout.PlankReach)), cell * RoundBoardLayout.PlankWidth));
                    plank.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
                    break;
                }
            }
        }

        private static void Pod(RectTransform holder, Box box, int material, int count)
        {
            GridPodView pod = UiKit.GridPod("Pod", holder);
            BoxLayout.Place((RectTransform)pod.transform, box);
            pod.ShowRound(material, count, PodLook.Exposed, false, false);
        }
    }
}
