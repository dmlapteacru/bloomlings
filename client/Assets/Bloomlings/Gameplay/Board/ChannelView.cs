using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// The Drop water channel on a round board in the Unity client (rules 17 §8 «Drop Водный канал», spec 006 FR-069; the owner's
    /// look A of 2026-11-05; the playtest's <c>Kit.Channel</c> and <c>Kit.ChannelRiders</c>), drawn from the kit's
    /// <see cref="ChannelLook"/> over the cubes: the routes' dots until the choice, the chosen route's stream (band by band
    /// with its running head, then whole) and its foam, the spring waking with its calling ring and the Drop's drop, the exits
    /// faint in their dashed ring, then breathing in the glow ring and taking the tap (<see cref="ExitTapped"/>; the game keeps
    /// running), the chosen one splashing open and the other fading; and the walkers riding the stream in bubbles of their
    /// colour. Each image is placed in screen pixels relative to the board's grid. Presentation only.
    /// </summary>
    public sealed class ChannelView
    {
        private readonly RectTransform _root;
        private readonly List<Image> _dots = new List<Image>();
        private readonly List<int> _dotExit = new List<int>();
        private readonly List<Image> _bands = new List<Image>();
        private readonly Image _whole;
        private readonly List<Image> _foam = new List<Image>();
        private readonly Image _head;
        private readonly Image _springStill;
        private readonly Image _springAwake;
        private readonly Image _springRing;
        private readonly Image _springDropBack;
        private readonly Image _springDrop;
        private readonly Exit[] _exits;
        private readonly List<Image> _riders = new List<Image>();
        private readonly List<(float X, float Y)> _points = new List<(float X, float Y)>();
        private readonly List<(Box Box, bool Across)> _dashes = new List<(Box Box, bool Across)>();
        private readonly List<Box> _splash = new List<Box>();
        private readonly Dictionary<Image, (string Key, int W, int H)> _shown = new Dictionary<Image, (string Key, int W, int H)>();
        private ChannelLook _look;
        private Box _grid;
        private int _wholeExit = -1;
        private int _bandsExit = -1;

        private sealed class Exit
        {
            public Image Glow = null!;
            public Image Dash = null!;
            public Image Still = null!;
            public Image Awake = null!;
            public Image DropBack = null!;
            public Image Drop = null!;
            public Button Touch = null!;
            public readonly List<Image> Splash = new List<Image>();
        }

        private ChannelView(Transform parent, ChannelLook look, Box grid)
        {
            _look = look;
            _grid = grid;
            _root = UiFactory.Stretch(UiFactory.CreateRect("Channel", parent));
            for (int e = 0; e < look.ExitCount; e++)
            {
                look.Dots(e, _points);
                foreach ((float X, float Y) _ in _points)
                {
                    _dots.Add(NewImage("Dot"));
                    _dotExit.Add(e);
                }
            }

            _whole = NewImage("Stream");
            _head = NewImage("Head");
            _springStill = NewImage("Spring");
            _springAwake = NewImage("SpringAwake");
            _springRing = NewImage("SpringRing");
            _springDropBack = Shape("DropBack", C.WaterDeep);
            _springDrop = Shape("Drop", Rgba.White);
            _exits = new Exit[look.ExitCount];
            for (int e = 0; e < _exits.Length; e++)
            {
                var exit = new Exit
                {
                    Glow = NewImage("ExitGlow"),
                    Dash = NewImage("ExitDash"),
                };
                for (int i = 0; i < 10; i++)
                {
                    exit.Splash.Add(NewImage("Splash"));
                }

                exit.Still = NewImage("Exit");
                exit.Awake = NewImage("ExitAwake");
                exit.DropBack = Shape("ExitDropBack", C.WaterDeep);
                exit.Drop = Shape("ExitDrop", Rgba.White);
                Image touchImage = UiFactory.CreateImage("ExitTouch", _root, null, Color.clear, raycast: true);
                Button touch = touchImage.gameObject.AddComponent<Button>();
                touch.transition = Selectable.Transition.None;
                touch.targetGraphic = touchImage;
                int choice = e;
                touch.onClick.AddListener(() => ExitTapped?.Invoke(choice));
                exit.Touch = touch;
                _exits[e] = exit;
            }

            PlaceStatics();
        }

        /// <summary>A calling exit was tapped (its index, the <c>channel exit=i</c> command).</summary>
        public event Action<int>? ExitTapped;

        public static ChannelView Create(Transform parent, ChannelLook look, Box grid) => new ChannelView(parent, look, grid);

        /// <summary>The same channel on a new layout of the board (a new screen size).</summary>
        public void Relayout(ChannelLook look, Box grid)
        {
            _look = look;
            _grid = grid;
            _wholeExit = -1;
            _bandsExit = -1;
            PlaceStatics();
        }

        /// <summary>
        /// Shows the channel in <paramref name="pose"/> at <paramref name="seconds"/> (the display clock), its exits taking taps
        /// while <paramref name="live"/> and the rules let them be chosen (<paramref name="choices"/>).
        /// </summary>
        public void Show(ChannelPose pose, float seconds, bool live, IReadOnlyList<int> choices)
        {
            ChannelLook look = _look;
            for (int i = 0; i < _dots.Count; i++)
            {
                float alpha = pose.DotsAlpha(_dotExit[i]);
                _dots[i].gameObject.SetActive(alpha > 0.01f);
                _dots[i].color = new Color(1f, 1f, 1f, alpha);
            }

            ShowStream(pose, seconds);

            // The spring, waking.
            ShowWell(_springStill, _springAwake, look.SpringBox(), 1f, pose.Awake, seconds);
            _springRing.gameObject.SetActive(pose.Calling);
            if (pose.Calling)
            {
                Box ring = look.SpringRing(0f);
                float share = look.GlowStroke * 0.75f / Mathf.Max(1f, Mathf.Min(ring.Width, ring.Height));
                Picture(_springRing, ring, UiRaster.ChannelGlowKey(share), (w, h) => UiRaster.ChannelGlowRing(w, h, share), look.SpringRing(pose.Pulse).Width / Mathf.Max(1f, ring.Width));
                _springRing.color = new Color(1f, 1f, 1f, 0.35f + (0.3f * pose.Pulse));
            }

            ShowDrop(_springDropBack, _springDrop, look.Drop(seconds), pose.Awake);

            // The exits.
            for (int e = 0; e < _exits.Length; e++)
            {
                Exit exit = _exits[e];
                float alpha = pose.ExitAlpha(e);
                bool shown = alpha > 0.01f;
                bool calling = shown && pose.Calling;
                exit.Glow.gameObject.SetActive(calling);
                if (calling)
                {
                    Box ring = look.GlowRing(e, 0f);
                    float share = look.GlowStroke / Mathf.Max(1f, ring.Width);
                    Picture(exit.Glow, ring, UiRaster.ChannelGlowKey(share), (w, h) => UiRaster.ChannelGlowRing(w, h, share), look.GlowRing(e, pose.Pulse).Width / Mathf.Max(1f, ring.Width));
                    exit.Glow.color = new Color(1f, 1f, 1f, RoundBoardLayout.BridgeRingAlpha(pose.Pulse));
                }

                bool faint = shown && !pose.Calling && pose.Chosen < 0;
                exit.Dash.gameObject.SetActive(faint);
                if (faint)
                {
                    Picture(exit.Dash, look.DashRing(e), UiRaster.ChannelDashKey, (w, h) => UiRaster.ChannelDashRing(Math.Min(w, h)));
                    exit.Dash.color = new Color(1f, 1f, 1f, Mathf.Min(1f, alpha * 1.4f));
                }

                look.Splash(e, pose.ExitOpen(e), _splash);
                for (int i = 0; i < exit.Splash.Count; i++)
                {
                    bool on = i < _splash.Count;
                    exit.Splash[i].gameObject.SetActive(on);
                    if (on)
                    {
                        Box drop = _splash[i];
                        float rest = look.Head(e, 0f).Width;
                        Picture(exit.Splash[i], Box.FromCenter(drop.CenterX, drop.CenterY, rest, rest), UiRaster.ChannelDropKey, (w, h) => UiRaster.ChannelDrop(Math.Min(w, h)), drop.Width / Mathf.Max(1f, rest));
                    }
                }

                float scale = ChannelLook.ExitScale(pose, e);
                exit.Still.gameObject.SetActive(shown);
                exit.Awake.gameObject.SetActive(shown);
                if (shown)
                {
                    ShowWell(exit.Still, exit.Awake, look.ExitBox(e), scale, pose.ExitAwake(e), seconds + (0.4f * (e + 1)), alpha);
                    ShowDrop(exit.DropBack, exit.Drop, look.ExitDrop(e, seconds, scale), pose.ExitAwake(e) * alpha);
                }
                else
                {
                    ShowDrop(exit.DropBack, exit.Drop, default, 0f);
                }

                exit.Touch.gameObject.SetActive(live && pose.Calling && Contains(choices, e));
            }
        }

        /// <summary>The walkers riding the stream at display time <paramref name="nowUs"/>, each in a bubble of its colour.</summary>
        public void ShowRiders(IReadOnlyList<RoundWalk> walkers, long nowUs)
        {
            int n = 0;
            foreach (RoundWalk walk in walkers)
            {
                if (!_look.Rider(walk, nowUs, out Box bubble))
                {
                    continue;
                }

                if (n == _riders.Count)
                {
                    _riders.Add(NewImage("Rider"));
                }

                Image image = _riders[n++];
                image.gameObject.SetActive(true);
                image.transform.SetAsLastSibling();
                RoundMaterial m = RoundMaterial.All[Math.Max(0, walk.Material)];
                Rgba color = RoundLook.ColorOf(m);
                float rest = _look.Head(0, 0f).Width / (ChannelLook.StreamCells * ChannelLook.WaterShare * 1.2f) * ChannelLook.BubbleCells;
                Picture(image, Box.FromCenter(bubble.CenterX, bubble.CenterY, rest, rest), UiRaster.ChannelBubbleKey(m.Code), (w, h) => UiRaster.ChannelBubble(Math.Min(w, h), color), bubble.Width / Mathf.Max(1f, rest));
            }

            for (int i = n; i < _riders.Count; i++)
            {
                _riders[i].gameObject.SetActive(false);
            }
        }

        // The dots and the touch targets: placed once a layout.
        private void PlaceStatics()
        {
            int k = 0;
            for (int e = 0; e < _look.ExitCount; e++)
            {
                _look.Dots(e, _points);
                foreach ((float x, float y) in _points)
                {
                    if (k < _dots.Count)
                    {
                        Picture(_dots[k++], _look.DotBox((x, y)), UiRaster.ChannelDotKey, (w, h) => UiRaster.ChannelDot(Math.Min(w, h)));
                    }
                }
            }

            float min = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            for (int e = 0; e < _exits.Length; e++)
            {
                UiKit.PlaceBox((RectTransform)_exits[e].Touch.transform, _look.Touch(e, min), _grid);
                _exits[e].Touch.transform.SetAsLastSibling();
            }
        }

        private void ShowStream(ChannelPose pose, float seconds)
        {
            int chosen = pose.Chosen;
            bool grown = chosen >= 0 && pose.Run >= 1f;
            _whole.gameObject.SetActive(grown);
            if (grown && _wholeExit != chosen)
            {
                _wholeExit = chosen;
                (Box box, string key, Func<int, int, byte[]> render) = _look.Stream(chosen, 0, _look.Bands(chosen) - 1);
                Picture(_whole, box, key, render);
            }

            int bands = chosen >= 0 && !grown && pose.Run > 0f ? _look.GrownBands(chosen, pose.Run) : 0;
            if (chosen >= 0 && _bandsExit != chosen)
            {
                _bandsExit = chosen;
                foreach (Image old in _bands)
                {
                    UnityEngine.Object.Destroy(old.gameObject);
                }

                _bands.Clear();
                for (int b = 0; b < _look.Bands(chosen); b++)
                {
                    Image band = NewImage("StreamBand");
                    band.transform.SetSiblingIndex(_whole.transform.GetSiblingIndex());
                    (Box box, string key, Func<int, int, byte[]> render) = _look.Stream(chosen, b, b);
                    Picture(band, box, key, render);
                    _bands.Add(band);
                }
            }

            for (int b = 0; b < _bands.Count; b++)
            {
                _bands[b].gameObject.SetActive(b < bands);
            }

            _dashes.Clear();
            if (chosen >= 0 && pose.Run > 0f)
            {
                _look.Foam(chosen, seconds, pose.Run, _dashes);
            }

            for (int i = 0; i < _dashes.Count; i++)
            {
                if (i == _foam.Count)
                {
                    Image dash = NewImage("Foam");
                    dash.transform.SetSiblingIndex(_head.transform.GetSiblingIndex());
                    _foam.Add(dash);
                }

                bool across = _dashes[i].Across;
                _foam[i].gameObject.SetActive(true);
                Picture(_foam[i], _dashes[i].Box, UiRaster.ChannelFoamKey(across), (w, h) => UiRaster.ChannelFoam(w, h));
            }

            for (int i = _dashes.Count; i < _foam.Count; i++)
            {
                _foam[i].gameObject.SetActive(false);
            }

            bool running = chosen >= 0 && pose.Run > 0f && pose.Run < 1f;
            _head.gameObject.SetActive(running);
            if (running)
            {
                Picture(_head, _look.Head(chosen, pose.Run), UiRaster.ChannelDropKey, (w, h) => UiRaster.ChannelDrop(Math.Min(w, h)));
            }
        }

        private void ShowWell(Image still, Image awakeImage, Box box, float scale, float awake, float seconds, float alpha = 1f)
        {
            // the well breathes and pops round its middle: its picture keeps its resting size, scaled
            float middle = box.Top + (box.Width / 2f / UiRaster.WellSide * UiRaster.WellRowShare * UiRaster.WellTop);
            Box at = box.Offset(0f, (middle - box.CenterY) * (1f - scale));
            still.gameObject.SetActive(awake < 1f && alpha > 0f);
            if (awake < 1f)
            {
                Picture(still, at, ChannelLook.SpringKey(false, 0), (w, h) => UiRaster.ChannelSpring(w, h, false, 0), scale);
                still.color = new Color(1f, 1f, 1f, alpha);
            }

            awakeImage.gameObject.SetActive(awake > 0f && alpha > 0f);
            if (awake > 0f)
            {
                int f = ChannelLook.RingFrame(seconds);
                Picture(awakeImage, at, ChannelLook.SpringKey(true, f), (w, h) => UiRaster.ChannelSpring(w, h, true, f), scale);
                awakeImage.color = new Color(1f, 1f, 1f, awake * alpha);
            }
        }

        private void ShowDrop(Image back, Image drop, Box box, float alpha)
        {
            bool on = alpha > 0.01f;
            back.gameObject.SetActive(on);
            drop.gameObject.SetActive(on);
            if (!on)
            {
                return;
            }

            UiKit.PlaceBox(back.rectTransform, box.Inset(-Mathf.Max(1.5f, box.Width * 0.12f)), _grid);
            UiKit.PlaceBox(drop.rectTransform, box, _grid);
            Color deep = UiTheme.Of(C.WaterDeep);
            back.color = new Color(deep.r, deep.g, deep.b, alpha);
            drop.color = new Color(1f, 1f, 1f, alpha);
        }

        // An image's picture at its box's pixel size rounded up to 8 (a new sprite only when the key or that size changes),
        // placed in the grid and scaled by scale round its middle.
        private void Picture(Image image, Box box, string key, Func<int, int, byte[]> render, float scale = 1f)
        {
            UiKit.PlaceBox(image.rectTransform, box, _grid);
            image.rectTransform.localScale = new Vector3(scale, scale, 1f);
            int w = UiRaster.Quantize(box.Width);
            int h = UiRaster.Quantize(box.Height);
            if (!_shown.TryGetValue(image, out (string Key, int W, int H) was) || !ReferenceEquals(was.Key, key) && was.Key != key || was.W != w || was.H != h)
            {
                _shown[image] = (key, w, h);
                image.sprite = ProceduralSprites.Picture(key, w, h, render);
                image.type = Image.Type.Simple;
            }
        }

        private Image NewImage(string name)
        {
            Image image = UiFactory.CreateImage(name, _root, null, Color.white);
            image.raycastTarget = false;
            return image;
        }

        private Image Shape(string name, Rgba color)
        {
            Image image = UiKit.ShapeImage(name, _root, "fx.droplet", color);
            image.raycastTarget = false;
            return image;
        }

        private static bool Contains(IReadOnlyList<int> list, int value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
