using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Rounds;
using Bloomlings.Client.Gameplay.Tray;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Variants;
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

        /// <summary>The player's equipped outfit of a family (the wardrobe's): the latch card's hero wears his family's (stock while null).</summary>
        public Func<Family, Outfit>? OutfitOf { get; set; }

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
            Picture(view.Body, card.Picture, new Box(body.Left, y, body.Right, y + ((PictureUnits - 20f) * u)), body, OutfitOf?.Invoke(RoundIntroLatch.Hero));
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
        private static void Picture(RectTransform parent, RoundIntroPicture picture, Box area, Box body, Outfit? outfit)
        {
            RectTransform holder = UiKit.PlaceBox(UiFactory.CreateRect("Picture", parent), area, body);

            // The pieces in the holder's top-down canvas units.
            Box local = UiKit.ToLocal(area, area);
            float h = local.Height;
            float podH = h * 0.62f;
            float podW = podH;
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
                    Box frame = PodChip.Cube(right).Frame;
                    float s = Mathf.Min(frame.Width, frame.Height);
                    Color glow = UiTheme.Of(C.GardenGlow);
                    Image fill = UiKit.RoundRect("RevealGlow", holder, new Color(glow.r, glow.g, glow.b, 0.4f), b => Mathf.Min(b.Width, b.Height) * 0.3f);
                    BoxLayout.Place(fill.rectTransform, frame.Inset(-s * 0.14f));
                    Image ring = UiKit.RoundRing("RevealRing", holder, glow, b => Mathf.Min(b.Width, b.Height) * 0.25f, b => Mathf.Min(b.Width, b.Height) * 0.035f);
                    BoxLayout.Place(ring.rectTransform, frame.Inset(-s * 0.03f));
                    Pod(holder, right, 1, 12);
                    break;
                }

                case RoundIntroPicture.Latch:
                {
                    // The hero of the pod's colour holding the padlock in that colour in the pod's place, and the key upright and
                    // turning over its cell on a patch of the board between cubes (17 §8, FR-068; the owner's look of 2026-11-02;
                    // the playtest's IntroPicture, RoundIntroLatch): in a loop the key flies to the keyhole, the padlock opens and
                    // the hero steps aside from the pod (LatchIntroLoop).
                    RoundIntroLatch look = RoundIntroLatch.In(local);
                    Image surface = UiFactory.CreateImage("Surface", holder, null, Color.white);
                    surface.raycastTarget = false;
                    BoxLayout.Place(surface.rectTransform, look.Patch);
                    PictureFit.On(surface, (w, ph) => RoundSprites.Surface(w, ph, 0.1f));
                    foreach ((Box box, int m) in look.Cubes)
                    {
                        Image cube = UiFactory.CreateImage("Cube", holder, null, Color.white);
                        cube.raycastTarget = false;
                        BoxLayout.Place(cube.rectTransform, box);
                        int material = m;
                        PictureFit.On(cube, (w, ph) => RoundSprites.BoardCube(material, false, w, ph));
                    }

                    GridPodView pod = UiKit.GridPod("Pod", holder);
                    BoxLayout.Place((RectTransform)pod.transform, look.Pod);
                    pod.ShowRound(RoundIntroLatch.Material, RoundIntroLatch.Count, PodLook.Exposed, false, false);
                    holder.gameObject.AddComponent<LatchIntroLoop>().Build(holder, look, pod.gameObject, outfit);
                    break;
                }

                case RoundIntroPicture.Channel:
                case RoundIntroPicture.Shutters:
                {
                    // The water channel (the exits call, one is chosen, the water runs to it and a bubble rides it) or the wooden
                    // shutters (the tag counts the green cubes, then the slats pop off), in a loop on a small board of their own
                    // (17 §8, FR-069, FR-070; the owner's looks A and B of 2026-11-05; the playtest's IntroPicture).
                    holder.gameObject.AddComponent<MechanicIntroLoop>().Build(holder, area, picture);
                    break;
                }

                case RoundIntroPicture.LinkedPair:
                {
                    Image tube = RoundTubes.Create("LinkTube", holder);
                    RoundTubes.Show(tube, LinkTube.BetweenCubes(PodChip.Cube(left).Frame, RoundLook.ColorOf(0), PodChip.Cube(right).Frame, RoundLook.ColorOf(8)));
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
                    // The banks are the board's neutral surface (the owner's board of 2026-10-28), the stream across it.
                    Image banks = UiFactory.CreateImage("Surface", holder, null, Color.white);
                    banks.raycastTarget = false;
                    BoxLayout.Place(banks.rectTransform, new Box(x0, local.CenterY - (cell * 1.5f), x0 + (cell * cells), local.CenterY + (cell * 1.5f)));
                    PictureFit.On(banks, (w, ph) => RoundSprites.Surface(w, ph, 0.04f));
                    for (int x = 0; x < cells; x++)
                    {
                        var water = new Box(x0 + (x * cell), local.CenterY - (cell * 0.5f), x0 + ((x + 1) * cell), local.CenterY + (cell * 0.5f));
                        Image stream = UiFactory.CreateImage("Water", holder, RoundSprites.Water(x, pixels), Color.white);
                        stream.raycastTarget = false;
                        BoxLayout.Place(stream.rectTransform, water);
                    }

                    // The stream's stone banks, as on the board (the owner, 2026-10-31).
                    float band = cell * RoundBoardLayout.BankShare;
                    float rule = Mathf.Max(1f, cell * RoundBoardLayout.BankLineShare);
                    float top = local.CenterY - (cell * 0.5f);
                    float bottom = local.CenterY + (cell * 0.5f);
                    float end = x0 + (cell * cells);
                    foreach ((Box box, Rgba color) in new[]
                    {
                        (new Box(x0, top, end, top + band), C.StoneTop), (new Box(x0, bottom - band, end, bottom), C.StoneTop),
                        (new Box(x0, top + band - rule, end, top + band), C.StoneLine), (new Box(x0, bottom - band, end, bottom - band + rule), C.StoneLine),
                    })
                    {
                        Image bank = UiFactory.CreateImage("Bank", holder, null, UiTheme.Of(color));
                        bank.raycastTarget = false;
                        BoxLayout.Place(bank.rectTransform, box);
                    }

                    Box site = Box.FromCenter(local.CenterX + (cell * 0.5f), local.CenterY, cell, cell);
                    Box span = Box.FromCenter(site.CenterX, site.CenterY, cell * (1f + (2f * RoundBoardLayout.PlankReach)), cell * RoundBoardLayout.PlankWidth);
                    Image plank = UiFactory.CreateImage("Bridge", holder, null, Color.white);
                    plank.raycastTarget = false;
                    BoxLayout.Place(plank.rectTransform, span);
                    PictureFit.On(plank, (w, ph) => RoundSprites.Bridge(3, w, ph));
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

    /// <summary>
    /// The latch card's loop (<see cref="RoundIntroLatch.Since"/>; the playtest's <c>IntroPicture</c>): closed, the key turning
    /// over its cell on its shadow and the pod hidden behind its hero; then the key's flight into the keyhole with its click, the
    /// padlock opening and dropping and the hero stepping aside from the pod, in the card's own unscaled time.
    /// </summary>
    public sealed class LatchIntroLoop : MonoBehaviour
    {
        private RoundIntroLatch _look = null!;
        private GameObject _pod = null!;
        private LatchHeroView _hero = null!;
        private Image _shadow = null!;
        private Image _key = null!;
        private Image _ring = null!;
        private Outfit? _outfit;
        private bool? _back;
        private float _start;

        public void Build(RectTransform holder, RoundIntroLatch look, GameObject pod, Outfit? outfit)
        {
            _look = look;
            _pod = pod;
            _outfit = outfit;
            _start = Time.unscaledTime;
            _shadow = UiFactory.CreateImage("KeyShadow", holder, null, Color.white);
            _shadow.raycastTarget = false;
            BoxLayout.Place(_shadow.rectTransform, RoundLook.KeyShadow(look.KeyCell));
            PictureFit.On(_shadow, (w, h) => RoundSprites.SoftOval(w, h));
            _hero = LatchHeroView.Create(holder);
            _ring = UiFactory.CreateImage("KeyClick", holder, ProceduralSprites.Ring, Color.white);
            _ring.raycastTarget = false;
            _key = UiFactory.CreateImage("Key", holder, null, Color.white);
            _key.raycastTarget = false;
            Tick();
        }

        private void Update() => Tick();

        private void Tick()
        {
            if (_look == null)
            {
                return;
            }

            float seconds = Time.unscaledTime - _start;
            float t = RoundIntroLatch.Since(seconds);
            Rgba metal = RoundLook.KeyMetal(RoundIntroLatch.Material);
            _pod.SetActive(t >= 0f);
            if (t < RoundLook.LatchOpenSeconds)
            {
                _hero.Show(_look.Pod, RoundIntroLatch.Hero, _outfit, metal, 0, t, -1, false);
            }
            else
            {
                _hero.Hide();
            }

            Box rest = _look.KeyBox;
            float rise = t < 0f ? RoundLook.KeyBob(seconds) : 0f;
            _shadow.gameObject.SetActive(t < 0f);
            float k = RoundLook.KeyShadowScale(rise);
            _shadow.rectTransform.localScale = new Vector3(k, k, 1f);
            (Box box, float sx, bool back, float alpha) = t < 0f
                ? (rest.Offset(0f, -rise * _look.KeyCell.Width), RoundLook.KeySpin(RoundLook.KeyTurns(seconds)).ScaleX, RoundLook.KeySpin(RoundLook.KeyTurns(seconds)).Back, 1f)
                : RoundLook.KeyFlight(t, rest, RoundLook.KeyInHole(_look.Lock, 0), RoundLook.KeyTurns(seconds - t));
            if (_back != back)
            {
                _back = back;
                Rgba m = back ? RoundLook.KeyBack(metal) : metal;
                PictureFit.On(_key, (w, h) => RoundSprites.UprightKey(m, 0, w, h));
            }

            BoxLayout.Place(_key.rectTransform, Box.FromCenter(box.CenterX, box.CenterY, rest.Width, rest.Height));
            float scale = box.Height / Mathf.Max(0.0001f, rest.Height);
            _key.rectTransform.localScale = new Vector3(sx * scale, scale, 1f);
            _key.color = new Color(1f, 1f, 1f, alpha);
            _key.gameObject.SetActive(alpha > 0f);
            (float radius, float ringAlpha) = RoundLook.KeyClick(t);
            _ring.gameObject.SetActive(ringAlpha > 0f);
            if (ringAlpha > 0f)
            {
                (float kx, float ky) = RoundLook.Keyhole(_look.Lock, 0);
                float r = radius * _look.Lock.Width;
                BoxLayout.Place(_ring.rectTransform, Box.FromCenter(kx, ky, 2f * r, 2f * r));
                _ring.color = UiTheme.Of(C.GardenGlow.WithAlpha(ringAlpha));
            }
        }
    }

    /// <summary>
    /// The channel and shutters cards' loops (<see cref="RoundIntroChannel"/>, <see cref="RoundIntroShutters"/>; the playtest's
    /// <c>IntroPicture</c>): a small board's surface and cubes with the kit's <c>ChannelView</c> or <c>ShutterView</c> over them,
    /// played in the card's own unscaled time.
    /// </summary>
    public sealed class MechanicIntroLoop : MonoBehaviour
    {
        private static readonly IReadOnlyList<int> NoChoices = Array.Empty<int>();
        private RoundIntroChannel? _channel;
        private RoundIntroShutters? _shutters;
        private global::Bloomlings.Client.Gameplay.Board.ChannelView? _channelView;
        private global::Bloomlings.Client.Gameplay.Board.ShutterView? _shutterView;
        private readonly Dictionary<int, Image> _cubes = new Dictionary<int, Image>();
        private Image? _bubble;
        private Box _grid;
        private float _start;

        /// <summary>Builds <paramref name="picture"/>'s loop in <paramref name="holder"/>, whose screen box is <paramref name="area"/>.</summary>
        public void Build(RectTransform holder, Box area, RoundIntroPicture picture)
        {
            _start = Time.unscaledTime;
            RoundBoardLayout board;
            if (picture == RoundIntroPicture.Channel)
            {
                _channel = RoundIntroChannel.In(area);
                board = _channel.Board;
            }
            else
            {
                _shutters = RoundIntroShutters.In(area, ShutterLook.TagMin(UiKit.ScreenBox().Width));
                board = _shutters.Board;
            }

            _grid = board.Grid;
            Image surface = UiFactory.CreateImage("Surface", holder, null, Color.white);
            surface.raycastTarget = false;
            UiKit.PlaceBox(surface.rectTransform, board.Outer, area);
            PictureFit.On(surface, (w, h) => RoundSprites.Surface(w, h, 0.1f));
            RectTransform grid = UiKit.PlaceBox(UiFactory.CreateRect("Grid", holder), board.Grid, area);
            RectTransform over = UiKit.PlaceBox(UiFactory.CreateRect("Over", holder), board.Grid, area);
            int px = UiRaster.Quantize(Mathf.Max(8f, board.Cell));
            int tall = Mathf.Max(8, Mathf.RoundToInt(px * board.Look.PictureShare));
            for (int y = board.Height - 2; y >= 1; y--)
            {
                for (int x = 1; x < board.Width - 1; x++)
                {
                    int cell = (y * board.Width) + x;
                    int m = board.MaterialOf(cell);
                    if (board.KindOf(cell) != RoundCellKind.Material || m < 0)
                    {
                        continue;
                    }

                    Image cube = UiFactory.CreateImage("Cube", grid, RoundSprites.BoardCube(m, false, px, tall, board.Look), Color.white);
                    cube.raycastTarget = false;
                    UiKit.PlaceBox(cube.rectTransform, board.CubeBox(cell), board.Grid);
                    _cubes[cell] = cube;
                }

                if (_shutters != null && _shutters.Look.BottomRow == y)
                {
                    _shutterView = global::Bloomlings.Client.Gameplay.Board.ShutterView.Create(grid, over, _shutters.Look, board.Grid);
                }
            }

            if (_channel != null)
            {
                _channelView = global::Bloomlings.Client.Gameplay.Board.ChannelView.Create(over, _channel.Look, board.Grid);
                _bubble = UiFactory.CreateImage("Bubble", over, null, Color.white);
                _bubble.raycastTarget = false;
            }

            Tick();
        }

        private void Update() => Tick();

        private void Tick()
        {
            float seconds = Time.unscaledTime - _start;
            if (_channel != null && _channelView != null && _bubble != null)
            {
                _channelView.Show(RoundIntroChannel.Pose(seconds), seconds, false, NoChoices);
                bool riding = _channel.Bubble(seconds, out Box bubble);
                _bubble.gameObject.SetActive(riding);
                if (riding)
                {
                    UiKit.PlaceBox(_bubble.rectTransform, bubble, _grid);
                    if (_bubble.sprite == null)
                    {
                        Rgba color = RoundLook.ColorOf(RoundIntroChannel.BubbleMaterial);
                        char code = RoundMaterial.All[RoundIntroChannel.BubbleMaterial].Code;
                        int size = UiRaster.Quantize(_channel.Board.Cell * ChannelLook.BubbleCells);
                        _bubble.sprite = ProceduralSprites.Picture(UiRaster.ChannelBubbleKey(code), size, size, (w, h) => UiRaster.ChannelBubble(Math.Min(w, h), color));
                    }
                }
            }

            if (_shutters != null && _shutterView != null)
            {
                int gone = RoundIntroShutters.Gone(seconds);
                for (int k = 0; k < _shutters.Counted.Count; k++)
                {
                    if (_cubes.TryGetValue(_shutters.Counted[k], out Image? cube))
                    {
                        cube.gameObject.SetActive(k >= gone);
                    }
                }

                _shutterView.Show(RoundIntroShutters.Count - gone, RoundIntroShutters.SinceCount(seconds), RoundIntroShutters.Open(seconds), seconds, false, null, null);
            }
        }
    }
}
