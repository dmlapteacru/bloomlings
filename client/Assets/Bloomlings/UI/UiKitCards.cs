using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The small pieces the popups and cards share in the reference look (spec 005 contracts/look.md §3.5, §4.3, §4.6;
    /// task T015): a tap target with the click sound, the cream round page arrows of the Store and the Collection (the
    /// playtest's <c>Kit.ArrowButton</c>), a glyph over its darker outline (the medals, the Daily Challenge sun, the
    /// difficulty stars), and text broken into balanced lines (the playtest's <c>EndCards.Lines</c>).
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>
        /// Makes <paramref name="target"/> (a row, a framed picture) a tap target: a button without a color transition
        /// that plays the click and runs <paramref name="onClick"/>. With <paramref name="press"/> it squashes like a tile
        /// while the finger is down (spec 003 FR-017).
        /// </summary>
        public static Button TapTarget(Graphic target, Action onClick, bool press = false)
        {
            target.raycastTarget = true;
            var button = target.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = target;
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick();
            });
            if (press)
            {
                target.gameObject.AddComponent<PressMotion>().Tile = true;
            }

            return button;
        }

        /// <summary>
        /// A cream ‹ or › button (spec 005 §4.6: the Store's and the Collection's pages; the playtest's
        /// <c>Kit.ArrowButton</c>): the icon buttons' rounded square on its wooden plate, 100/132 of the rect (place a touch-sized square,
        /// <c>size.touch_min</c>, so the 100-unit arrow keeps a full touch target), with the brown chevron (half the
        /// cushion, nudged the way it points) raised on it (spec 005 FR-044), pointing right when <paramref name="next"/>. Not
        /// interactable, it fades to 55% and takes no tap.
        /// </summary>
        public static Button PageArrow(string name, Transform parent, bool next, Action onClick)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent, raycast: true);
            GardenButton face = IconFace("Face", root, GardenLook.White, GardenLook.IconRimFaceRadius, square: true, rim: true);
            face.GreyWhenDisabled = false;
            face.FadeWhenDisabled = true;
            // The chevron points right; the previous page's arrow is its mirror, its light still from the upper left.
            Image glyph = RaisedGlyph(face.Content, ProceduralSprites.RaisedChevron(next, GardenLook.GlyphOn(face.Set)));
            float m = next ? 1f : -1f;

            BoxLayout.On(face.Content).Add(glyph.rectTransform, f =>
            {
                float g = face.IconSide * 0.5f * GardenLook.IconRimGlyphOfFace;
                return Box.FromCenter(f.CenterX + (face.IconSide * 0.03f * m / GardenLook.IconRimFaceShare), f.CenterY, g, g);
            });
            layout.Add((RectTransform)face.transform, b =>
            {
                float side = Mathf.Min(b.Width, b.Height) * 100f / DesignTokens.Size.TouchMin;
                return Box.FromCenter(b.CenterX, b.CenterY, side, side);
            });

            var relay = root.gameObject.AddComponent<PressRelay>();
            relay.Target = face;
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = root.GetComponent<Image>();
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick();
            });
            face.Button = button;
            return button;
        }

        /// <summary>
        /// A glyph in a saturated color over its darker outline, baked into one sprite at the image's pixel size (the
        /// playtest's outlined shapes: the leaderboard medals, the Daily Challenge sun, the difficulty stars): the shape
        /// grown by <paramref name="grow"/> shape units in <paramref name="line"/>, then the shape in
        /// <paramref name="fill"/>. Never a touch target; it keeps its square aspect.
        /// </summary>
        public static Image OutlinedIcon(string name, Transform parent, string shapeId, Rgba fill, Rgba line, float grow = 0.07f)
        {
            Func<float, float, float> sdf = ShapeLibrary.Get(shapeId);
            var layers = new List<(Func<float, float, float> Sdf, Rgba Color)> { ((x, y) => sdf(x, y) - grow, line), (sdf, fill) };
            string key = shapeId + "/outlined/" + fill.Hex + "/" + line.Hex + "/" + grow.ToString("0.###", CultureInfo.InvariantCulture);
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            PictureFit.On(image, (w, h) => ProceduralSprites.Baked(key, Mathf.Max(8, Mathf.Min(w, h)), layers), square: true);
            return image;
        }

        /// <summary>
        /// <paramref name="text"/> in at most two lines no wider than <paramref name="width"/> canvas units at
        /// <paramref name="fontSize"/>, measured with <paramref name="probe"/> (the playtest's <c>EndCards.Lines</c>): one
        /// line when it fits, else two balanced lines that prefer to break after a sentence or a comma, else a greedy
        /// break (the last line may shrink to fit). When the text engine cannot measure yet, a long text breaks after its
        /// first sentence. The probe's text is cleared afterwards.
        /// </summary>
        public static List<string> BalancedLines(TextMeshProUGUI probe, string text, float fontSize, float width)
        {
            var lines = new List<string>();
            float Measure(string s)
            {
                probe.text = s;
                return KitText.Measure(probe, fontSize);
            }

            float whole = Measure(text);
            if (whole <= 0f)
            {
                int stop = text.IndexOf(". ", StringComparison.Ordinal);
                if (stop > 0 && stop + 2 < text.Length)
                {
                    lines.Add(text.Substring(0, stop + 1));
                    lines.Add(text.Substring(stop + 2));
                }
                else
                {
                    lines.Add(text);
                }

                probe.text = string.Empty;
                return lines;
            }

            if (whole <= width)
            {
                lines.Add(text);
                probe.text = string.Empty;
                return lines;
            }

            string[] words = text.Split(' ');
            int best = -1;
            float bestCost = float.MaxValue;
            for (int i = 1; i < words.Length; i++)
            {
                string a = string.Join(" ", words, 0, i);
                string b = string.Join(" ", words, i, words.Length - i);
                float wa = Measure(a);
                float wb = Measure(b);
                if (wa > width || wb > width)
                {
                    continue;
                }

                char end = a[a.Length - 1];
                bool pause = end == '.' || end == ',' || end == ':' || end == '!' || end == '?';
                float cost = Mathf.Max(wa, wb) - (pause ? width * 0.25f : 0f);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = i;
                }
            }

            if (best > 0)
            {
                lines.Add(string.Join(" ", words, 0, best));
                lines.Add(string.Join(" ", words, best, words.Length - best));
            }
            else
            {
                // Greedy: the first line as long as it fits, the rest on the second (shrinking to fit).
                string current = string.Empty;
                int used = 0;
                for (int i = 0; i < words.Length; i++)
                {
                    string candidate = current.Length == 0 ? words[i] : current + " " + words[i];
                    if (current.Length > 0 && Measure(candidate) > width)
                    {
                        break;
                    }

                    current = candidate;
                    used = i + 1;
                }

                lines.Add(current);
                if (used < words.Length)
                {
                    lines.Add(string.Join(" ", words, used, words.Length - used));
                }
            }

            probe.text = string.Empty;
            return lines;
        }
    }

    /// <summary>
    /// One close button per card stack (spec 005 T021, the playtest's <c>DesignApp.CardClose</c>): every popup card made
    /// by <see cref="UiKit.Card"/> carries this on its backdrop, and while a card opened after it is open, its own cream
    /// ✕ hides, so Settings over the pause card shows one ✕, not two stacked at the same edge. The open cards are kept
    /// in the order they opened; the last one is on top.
    /// </summary>
    public sealed class CardStackMember : MonoBehaviour
    {
        private static readonly List<CardStackMember> Open = new List<CardStackMember>();
        private GameObject? _close;

        /// <summary>The card's close button (none for a card without one).</summary>
        public GameObject? Close
        {
            get => _close;
            set
            {
                _close = value;
                Refresh();
            }
        }

        /// <summary>Whether a card opened after this one is still open (its close button then hides).</summary>
        public bool Covered => Open.Contains(this) && Open[Open.Count - 1] != this;

        private void OnEnable()
        {
            Open.Remove(this);
            Open.Add(this);
            Refresh();
        }

        private void OnDisable()
        {
            Open.Remove(this);
            Refresh();
        }

        private static void Refresh()
        {
            for (int i = 0; i < Open.Count; i++)
            {
                if (Open[i]._close != null)
                {
                    Open[i]._close!.SetActive(i == Open.Count - 1);
                }
            }
        }
    }
}
