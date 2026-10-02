using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI
{
    /// <summary>How a pod is drawn (frame 12, spec 005 contracts/look.md §3.7; the playtest's <c>PodLook</c>).</summary>
    public enum PodLook
    {
        /// <summary>The tappable pod on top of its column: bright, with the wooden handle.</summary>
        Exposed,

        /// <summary>A pod waiting below in its column: dimmed toward <c>parchment.bottom</c>.</summary>
        Next,

        /// <summary>The finger is down: the frame sinks a little.</summary>
        Pressed,

        /// <summary>A locked pod: the grey inner panel and the padlock.</summary>
        Locked,
    }

    /// <summary>
    /// The reference look's garden pieces in uGUI (spec 005 contracts/look.md §3), the twins of the playtest's
    /// <c>KitGarden.cs</c>: materials drawn as cached <see cref="UiRaster"/> pictures, candy tiles, wooden signs, jam
    /// choices and cost pills, the stone furniture of the board and the win, pods, Waiting Slots, booster tiles, light rays,
    /// falling petals and the wooden wordmark. Each returns a view whose <c>Show</c> methods change its state; callers place
    /// the returned rect (anchors from <see cref="ScreenLayout"/> boxes, <see cref="UiKit.PlaceBox"/>).
    /// </summary>
    public static partial class UiKit
    {
        // ---- Materials (§2) ----

        /// <summary>
        /// A wooden plank picture filling the image's rect (<c>mat.wood.light</c> or <c>mat.wood.dark</c>), rendered at its
        /// pixel size and sliced: corner radius <paramref name="radiusShare"/> of its height, outline
        /// <paramref name="outlineShare"/> (at least 2 px), deeper bottom band <paramref name="lipShare"/>.
        /// </summary>
        public static Image WoodPlank(string name, Transform parent, float radiusShare, int seed, WoodTone tone = WoodTone.Light, float outlineShare = 0.025f, float lipShare = UiRaster.PlankLip)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            PictureFit.On(image, (w, h) => ProceduralSprites.Plank(tone, w, h, radiusShare, outlineShare, seed, lipShare), sliced: true);
            return image;
        }

        /// <summary>A stone block picture filling the image's rect (<c>mat.stone</c>), its corners rounded by <paramref name="radiusShare"/> of its shorter side.</summary>
        public static Image StoneBlock(string name, Transform parent, int seed, float radiusShare = 0.3f)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            PictureFit.On(image, (w, h) => ProceduralSprites.Stone(w, h, seed, radiusShare));
            return image;
        }

        // ---- Candy tiles (§3.1) ----

        /// <summary>
        /// A variant's candy tile (§3.1) as the largest square in its rect: the board style (a small raised bead of the
        /// symbol) or the sticker style (pods, slots, the jam row). A null <paramref name="variant"/> is a mystery tile.
        /// The view changes variant and state, and sinks the face into its lip while <see cref="CandyTileView.Pressed"/>.
        /// </summary>
        public static CandyTileView CandyTile(string name, Transform parent, VariantId? variant, TileStyle style, TileState state = TileState.Normal)
        {
            CandyTileView view = NewTile(name, parent, style);
            view.Show(variant, state);
            return view;
        }

        /// <summary>A candy tile of any color and variant icon (the win picture draws its roles' colors this way).</summary>
        public static CandyTileView CandyTile(string name, Transform parent, Rgba color, string iconId, TileStyle style, TileState state = TileState.Normal)
        {
            CandyTileView view = NewTile(name, parent, style);
            view.Show(color, iconId, state);
            return view;
        }

        private static CandyTileView NewTile(string name, Transform parent, TileStyle style)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<CandyTileView>();
            Image image = UiFactory.CreateImage("Tile", root, null, Color.white);
            view.Init(image, layout, style);
            return view;
        }

        // ---- Wooden signs (§3.2) ----

        /// <summary>
        /// A wooden sign (§3.2; the gameplay level, the win and banner titles, the Home level plaque): a light wood plank
        /// filling the rect (radius 28% of its height) over a soft shadow, the text centered in <c>ink.brown</c> (or
        /// <paramref name="letters"/>) with a light emboss, at most 82% of the plank wide and 62% of its height tall, and its
        /// decoration: ivy over both ends (clusters 1.25 × the height, the back leaves behind the plank), or flower clusters
        /// at the top-left and bottom-right ends (1.35 × the height). Never a touch target.
        /// </summary>
        public static WoodSignView WoodSign(string name, Transform parent, string text, TypeStyle style, SignDecor decor = SignDecor.None, Rgba? letters = null)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<WoodSignView>();
            SoftShadow(layout, b => b, b => b.Height * 0.28f, 0.22f, 0.07f);
            if (decor == SignDecor.Ivy)
            {
                layout.Add(IvyCluster("IvyBackLeft", root, flipped: false, back: true).rectTransform, b => IvyBox(b, left: true));
                layout.Add(IvyCluster("IvyBackRight", root, flipped: true, back: true).rectTransform, b => IvyBox(b, left: false));
            }

            Image plank = WoodPlank("Plank", root, 0.28f, 7);
            layout.Add(plank.rectTransform, b => b);
            TextMeshProUGUI label = KitLabel("Label", root, text, style, GardenLook.SignLetters(letters ?? C.InkBrown));
            view.Init(label, plank, style);
            layout.Watch(label).Then(b => KitText.Place(label, style, b.CenterX, b.CenterY - (b.Height * 0.04f), Mathf.Min(Units(style.Size), b.Height * 0.62f), b.Width * 0.82f));
            switch (decor)
            {
                case SignDecor.Ivy:
                    layout.Add(IvyCluster("IvyLeft", root, flipped: false, back: false).rectTransform, b => IvyBox(b, left: true));
                    layout.Add(IvyCluster("IvyRight", root, flipped: true, back: false).rectTransform, b => IvyBox(b, left: false));
                    break;
                case SignDecor.Flowers:
                    layout.Add(FlowerCluster("FlowersLeft", root, flipped: false).rectTransform, b => Box.FromCenter(b.Left + (b.Height * 0.1f), b.Top + (b.Height * 0.08f), b.Height * 1.35f, b.Height * 1.35f));
                    layout.Add(FlowerCluster("FlowersRight", root, flipped: true).rectTransform, b => Box.FromCenter(b.Right - (b.Height * 0.08f), b.Bottom - (b.Height * 0.04f), b.Height * 1.35f * 0.92f, b.Height * 1.35f * 0.92f));
                    break;
            }

            return view;
        }

        /// <summary>The box of a sign end's ivy cluster: 1.25 × the sign's height, centered just inside its end.</summary>
        private static Box IvyBox(Box sign, bool left)
        {
            float size = sign.Height * 1.25f;
            float x = left ? sign.Left + (sign.Height * 0.06f) : sign.Right - (sign.Height * 0.06f);
            return Box.FromCenter(x, sign.CenterY, size, size);
        }

        /// <summary>
        /// A cluster of clover leaves (§3.9, <c>ui.sign.ivy</c>) as a square picture: pointed leaflets in yellow-green
        /// <c>ivy.leaf</c> shades over dark <c>ivy.line</c> outlines with light spots and midribs, mirrored when
        /// <paramref name="flipped"/>; <paramref name="back"/> keeps only the leaves behind a sign (true) or in front
        /// (false). Never a touch target.
        /// </summary>
        public static Image IvyCluster(string name, Transform parent, bool flipped, bool? back = null)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            PictureFit.On(image, (w, h) => ProceduralSprites.IvyCluster(flipped, back, Mathf.Min(w, h)), square: true);
            return image;
        }

        /// <summary>
        /// A lush cluster for the win sign's ends (§3.9): five big leaves in three greens with veins and two white
        /// five-petal flowers with yellow middles, turned half way when <paramref name="flipped"/>. Never a touch target.
        /// </summary>
        public static Image FlowerCluster(string name, Transform parent, bool flipped)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            PictureFit.On(image, (w, h) => ProceduralSprites.FlowerCluster(flipped, Mathf.Min(w, h)), square: true);
            return image;
        }

        // ---- Jam choices and cost pills (§3.3, §3.4) ----

        /// <summary>
        /// A jam choice (§3.3): a glossy rounded rectangle in <paramref name="set"/> (green or blue, radius 22% of its
        /// height) with the icon (44% of the height) in its upper half, the white outlined label (17% of the height) below
        /// it, and the cost pill centered on its bottom edge, overlapping by 40% of the pill's height. The rect holds the
        /// button and the pill below it and is the touch target. Without <paramref name="onClick"/> it is disabled: greyed,
        /// 55% alpha, the icon grey.
        /// </summary>
        public static ChoiceButtonView ChoiceButton(string name, Transform parent, ColorSet set, IReadOnlyList<IconPart>? icon, string label, Cost? cost, Action? onClick)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent, raycast: true);
            var view = root.gameObject.AddComponent<ChoiceButtonView>();
            CanvasGroup fade = root.gameObject.AddComponent<CanvasGroup>();
            GardenButton face = NewButton("Button", root, set, raycast: false);
            BoxLayout body = BoxLayout.On(face.Body);
            SoftShadow(body, b => b, b => b.Height * 0.22f, 0.24f, 0.05f);
            RectTransform faceRect = UiFactory.CreateRect("Face", face.Body);
            BuildFace(face, faceRect, FaceKind.Raised, gloss: true);
            float buttonHeight = 1f;
            body.Then(b =>
            {
                buttonHeight = b.Height;
                BoxLayout.Place(faceRect, b);
                float lip = b.Height * 0.075f;
                face.SetGeometry(Units(DesignTokens.Garden.Outline(b.Height / Mathf.Max(0.0001f, Units(1f)))), lip, Mathf.Max(0f, lip - Units(3f)), b.Height * 0.22f);
            });

            Image? iconImage = null;
            if (icon != null)
            {
                iconImage = IconParts("Icon", face.Content, icon);
            }

            TypeStyle s = T.ButtonSecondary;
            TextMeshProUGUI text = KitLabel("Label", face.Content, label, s, TextLook.OnGloss(set));
            face.Track(text, s, TextLook.OnGloss);
            BoxLayout content = BoxLayout.On(face.Content).Watch(text);
            if (iconImage != null)
            {
                // As measured on the reference's jam card (the playtest's Kit.ChoiceButton): the icon's box half the
                // button's height (the icon about 44% of the face) at 36% of the face, the letters 21% of the height.
                content.Add(iconImage.rectTransform, f => Box.FromCenter(f.CenterX, f.Top + (f.Height * 0.36f), buttonHeight * 0.5f, buttonHeight * 0.5f));
            }

            content.Then(f =>
            {
                float y = iconImage != null ? f.Top + (f.Height * 0.78f) : f.CenterY;
                KitText.Place(text, s, f.CenterX, y, Mathf.Min(Units(s.Size), buttonHeight * 0.21f), f.Width * 0.88f);
            });

            CostPillView pill = CostPill("Cost", root, cost ?? Cost.Free);
            view.Init(face, text, pill, fade, iconImage, icon, layout);
            layout.Add((RectTransform)face.transform, view.ButtonBox);
            layout.Add((RectTransform)pill.transform, b =>
            {
                Box button = view.ButtonBox(b);
                float pillHeight = button.Height * ChoiceButtonView.PillShare;
                return Box.FromCenter(button.CenterX, button.Bottom + (pillHeight * 0.1f), button.Width * 0.64f, pillHeight);
            });
            view.SetCost(cost);

            var press = root.gameObject.AddComponent<PressRelay>();
            press.Target = face;
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = root.GetComponent<Image>();
            button.interactable = onClick != null;
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick?.Invoke();
            });
            face.Button = button;
            view.Button = button;
            return view;
        }

        /// <summary>
        /// A cost pill (§3.4; jam choices, booster tiles, the Store): a cream pill (a soft shadow, the <c>cream.lip</c>
        /// below, the <c>cream.top</c> to <c>parchment.bottom</c> face and a <c>cream.line</c> outline) holding the lotus and
        /// a brown price, a green ▶ square and "Free", or "×N" charges, centered as a group.
        /// </summary>
        public static CostPillView CostPill(string name, Transform parent, Cost cost)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<CostPillView>();
            SoftShadow(layout, b => b, b => b.Height / 2f, 0.2f, 0.12f);
            Image lip = RoundRect("Lip", root, UiTheme.Of(C.CreamLip));
            Image face = RoundGradient("Face", root, C.CreamTop, C.ParchmentBottom);
            Image line = RoundRing("Line", root, UiTheme.Of(C.CreamLine), null, b => Mathf.Max(Units(2f), b.Height * 0.05f));
            layout.Add(lip.rectTransform, b => b.Offset(0f, b.Height * 0.07f));
            layout.Add(face.rectTransform, b => b);
            layout.Add(line.rectTransform, b => b);

            Image lotus = PetalIcon("Lotus", root);

            // The rewarded choice: a white ▶ on a small green square.
            RectTransform free = UiFactory.CreateRect("Free", root);
            BoxLayout freeLayout = BoxLayout.On(free);
            ColorSet green = GardenLook.Green;
            Image freeLip = RoundRect("Lip", free, UiTheme.Of(green.Lip), b => b.Width * 0.26f);
            Image freeFace = RoundGradient("Face", free, green.Top, green.Face, b => b.Width * 0.26f);
            Image freeLine = RoundRing("Line", free, UiTheme.Of(green.Line), b => b.Width * 0.26f, b => Mathf.Max(Units(1f), b.Width * 0.04f));
            Image play = ShapeImage("Play", free, "ui.play", Rgba.White);
            freeLayout.Add(freeLip.rectTransform, b => b.Offset(0f, b.Height * 0.08f));
            freeLayout.Add(freeFace.rectTransform, b => b);
            freeLayout.Add(freeLine.rectTransform, b => b);
            freeLayout.Add(play.rectTransform, b => b.Inset(b.Width * 0.2f).Offset(b.Width * 0.03f, 0f));

            TextMeshProUGUI label = KitLabel("Amount", root, string.Empty, T.Count, TextLook.Plain(C.InkBrown));
            view.Init(label, lotus, free.gameObject, layout);
            layout.Watch(label).Then(b =>
            {
                float h = b.Height;
                float icon = view.Cost.Kind == CostKind.Petals ? h * 0.86f : view.Cost.Kind == CostKind.Free ? h * 0.6f : 0f;
                float gap = icon > 0f ? h * 0.16f : 0f;
                float size = h * 0.56f;
                float measured = KitText.Measure(label, size);
                float room = Mathf.Max(1f, b.Width - icon - gap - (h * 0.5f));
                float textWidth = Mathf.Min(measured > 0f ? measured : room, room);
                float start = b.CenterX - ((icon + gap + textWidth) / 2f);
                Box iconBox = Box.FromCenter(start + (icon / 2f), b.CenterY, icon, icon);
                BoxLayout.Place(lotus.rectTransform, iconBox);
                BoxLayout.Place(free, iconBox);
                KitText.Place(label, T.Count, start + icon + gap + (textWidth / 2f), b.CenterY, size, textWidth + 1f);
            });
            view.SetCost(cost);
            return view;
        }

        /// <summary>A price tag (a booster without charges): since spec 005 a <see cref="CostPill"/> with the lotus.</summary>
        public static CostPillView PriceTag(string name, Transform parent, int price) => CostPill(name, parent, Cost.Petals(price));

        // ---- Board furniture (§3.6) ----

        /// <summary>
        /// The stone border around a board's grid (§3.6), as the first child of <paramref name="grid"/> (so the tiles draw
        /// over it): a dark gap of 0.04 cell around the grid (it also shows between the tiles as their thin dark lines),
        /// then blocks of stone <paramref name="thickness"/> cells thick (0.42 on the board, 0.3 around the win picture)
        /// whose lengths alternate 1.0 and 0.8 cell, square rounded blocks at the corners and dark joints. The cell is
        /// the grid's width over <paramref name="columns"/> (or its height over <paramref name="rows"/>, the smaller).
        /// </summary>
        public static StoneBorderView StoneBorder(RectTransform grid, int columns, int rows, float thickness = 0.42f)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect("StoneBorder", grid));
            root.SetAsFirstSibling();
            var view = root.gameObject.AddComponent<StoneBorderView>();
            view.Init(columns, rows, thickness);
            return view;
        }

        /// <summary>
        /// A Garden Entry's stone arch box (§3.6, the playtest's <c>Kit.StoneArch</c>) in top-down coordinates: a half ring
        /// of outer radius 1.5 cells whose open base's middle is (<paramref name="cx"/>, <paramref name="cy"/>), on the
        /// <paramref name="side"/> the entry is on (for <see cref="EntrySide.Bottom"/> it stands below the board, crown up).
        /// </summary>
        public static Box ArchBox(float cx, float cy, float cell, EntrySide side)
        {
            float r = cell * 1.5f;
            return side switch
            {
                EntrySide.Left => new Box(cx, cy - r, cx + r, cy + r),
                EntrySide.Top => new Box(cx - r, cy, cx + r, cy + r),
                EntrySide.Right => new Box(cx - r, cy - r, cx, cy + r),
                _ => new Box(cx - r, cy - r, cx + r, cy),
            };
        }

        /// <summary>The quarter turns of an entry's arch crown from up (<see cref="UiRaster.Arch"/>).</summary>
        public static int ArchTurns(EntrySide side) => side switch
        {
            EntrySide.Left => 1,
            EntrySide.Top => 2,
            EntrySide.Right => 3,
            _ => 0,
        };

        /// <summary>
        /// A Garden Entry's stone arch (§3.6, <c>board.arch</c>): nine sandy stone blocks in a half ring around an opening
        /// that shows the lawn; place it at <see cref="ArchBox"/>. Never a touch target.
        /// </summary>
        public static Image StoneArch(string name, Transform parent, EntrySide side)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            int turns = ArchTurns(side);
            PictureFit.On(image, (w, h) => ProceduralSprites.Arch(w, h, turns));
            return image;
        }

        /// <summary>
        /// The stone pedestal the heroes stand on (§3.6, <c>ui.pedestal</c>; win, milestone, Home, Wardrobe): an
        /// ellipse-topped stone drum filling the rect over a soft ground shadow. Put the heroes on <see cref="PedestalTop"/>.
        /// </summary>
        public static RectTransform StonePedestal(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            Image shadow = Ellipse("Ground", root, UiTheme.Of(C.GardenShadow.WithAlpha(0.22f)));
            layout.Add(shadow.rectTransform, b => Box.FromCenter(b.CenterX, b.Bottom, b.Width * 1.12f, b.Width * 1.12f * 0.22f));
            Image drum = UiFactory.CreateImage("Pedestal", root, null, Color.white);
            PictureFit.On(drum, (w, h) => ProceduralSprites.Pedestal(w, h));
            layout.Add(drum.rectTransform, b => b);
            return root;
        }

        /// <summary>The top ellipse's box of a pedestal filling <paramref name="box"/> (top-down): where the heroes' feet go.</summary>
        public static Box PedestalTop(Box box)
        {
            float rx = (box.Width / 2f) - 1f;
            float ry = Math.Max(1f, Math.Min(rx * 0.28f, (box.Height - 4f) * 0.3f));
            return new Box(box.Left, box.Top + 1f, box.Right, box.Top + 1f + (2f * ry));
        }

        // ---- Tray pieces (§3.7) ----

        /// <summary>
        /// A pod (§3.7): a soft shadow, the short wooden handle on top of an exposed pod, the inner panel tinted by the
        /// variant (plain cream when queued, locked or a mystery; <c>state.lock_bg</c> when locked), the dark wood frame
        /// (radius 18%, border 11% of the width), the variant's sticker tile at 70% of the panel near its top and the plain
        /// count below it. Queued pods are dimmed, a pressed one sinks, a locked one shows the padlock; a null variant is a
        /// mystery pod. Set it with <see cref="KitPodView.Show"/>; add the link bar or the "+N" badge on top.
        /// </summary>
        public static KitPodView Pod(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<KitPodView>();
            view.Build(layout);
            return view;
        }

        /// <summary>
        /// A pod's wooden frame alone (§3.7, the playtest's <c>Kit.PodFrame</c>): set it with
        /// <see cref="KitPodView.ShowFrame"/> and put the content in <see cref="KitPodView.Content"/>.
        /// </summary>
        public static KitPodView PodFrame(string name, Transform parent, PodLook look, bool handle = true, Rgba? tint = null)
        {
            KitPodView view = Pod(name, parent);
            view.ShowFrame(look, handle, tint);
            return view;
        }

        /// <summary>
        /// Places a pod's or slot's count under its tile (§3.7): plain digits in <c>type.count</c>, 86% of the area's
        /// height, <c>ink.brown</c> or softer when <paramref name="dim"/>.
        /// </summary>
        public static void PlaceCount(TextMeshProUGUI label, Box area, bool dim)
        {
            Rgba ink = dim ? C.InkBrownSoft.Mix(C.ParchmentBottom, 0.3f) : C.InkBrown;
            ApplyLook(label, T.Count, TextLook.Plain(ink));
            KitText.Place(label, T.Count, area.CenterX, area.CenterY, area.Height * 0.86f, area.Width);
        }

        /// <summary>
        /// A Waiting Slot (§3.7): a raised cream plate (radius 20%) with the sticker tile at 64% of its width and the count
        /// below while a pod works; the grey tile with the hourglass badge while it is stuck; a slightly sunk face with a
        /// dashed inner outline when empty (in <c>state.danger</c> with "!" for the last free slot); a grey face with the
        /// padlock when locked; the green "+" badge of the Extra Slot. Set it with <see cref="SlotPlateView.Show"/>.
        /// </summary>
        public static SlotPlateView SlotPlate(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<SlotPlateView>();
            view.Build(layout);
            return view;
        }

        /// <summary>
        /// A booster tile (§3.7; the booster bar): a cream squircle (radius 26%) in a cool silver-grey rim with the
        /// booster's colored icon at 62%, and the green count badge over its bottom-right corner, or the cost pill under it
        /// and a small green "+" when no charges are left. A selected tile is raised with the pulsing golden glow; a
        /// disabled one is greyed at 55% (spec 003 FR-031). Set it with <see cref="BoosterTileView.Show"/>.
        /// </summary>
        public static BoosterTileView BoosterTile(string name, Transform parent, string boosterId, Action? onPress)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<BoosterTileView>();
            view.Build(layout, boosterId, onPress);
            return view;
        }

        /// <summary>A booster's colored icon (§3.8, <see cref="GardenLook.BoosterIcon"/>), all grey when <paramref name="grey"/>.</summary>
        public static Image BoosterIcon(string name, Transform parent, string boosterId, bool grey = false) =>
            IconParts(name, parent, GardenLook.BoosterIcon(boosterId), grey);

        /// <summary>Changes a <see cref="BoosterIcon"/> image's booster or greyness.</summary>
        public static void SetBoosterIcon(Image image, string boosterId, bool grey) =>
            SetIconParts(image, GardenLook.BoosterIcon(boosterId), grey);

        /// <summary>Changes an <see cref="IconParts"/> image's parts or greyness.</summary>
        public static void SetIconParts(Image image, IReadOnlyList<IconPart> parts, bool grey) =>
            PictureFit.On(image, (w, h) => ProceduralSprites.IconParts(parts, grey, Mathf.Min(w, h)), square: true);

        // ---- Celebration (§3.9) ----

        /// <summary>
        /// Light rays behind the celebrating heroes (§3.9, <c>fx.rays</c>): ten soft <c>ray.light</c> wedges from the
        /// rect's center to its edge (place a square of twice the rays' radius), turning 0.05 turn per second on unscaled
        /// time. Never a touch target.
        /// </summary>
        public static LightRaysView LightRays(string name, Transform parent)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            PictureFit.On(image, (w, h) => ProceduralSprites.LightRays(Mathf.Min(w, h)), square: true);
            return image.gameObject.AddComponent<LightRaysView>();
        }

        /// <summary>
        /// Ten pink petals drifting down through the rect and swaying (§3.9, <c>fx.petals</c>), turning over as they fall,
        /// on unscaled time since the view was enabled. Never a touch target.
        /// </summary>
        public static FallingPetalsView FallingPetals(string name, Transform parent)
        {
            RectTransform root = UiFactory.CreateRect(name, parent);
            var view = root.gameObject.AddComponent<FallingPetalsView>();
            view.Build();
            return view;
        }

        // ---- The wordmark (§4.5) ----

        /// <summary>
        /// The wooden wordmark (§4.5, <c>ui.logo.wood</c>; the stand-in for the owner's logo): <paramref name="text"/> in
        /// <c>type.wordmark</c> fitted into the rect (86% of its width, 72% of its height) with light wood letters, a
        /// <c>wood.line</c> outline and a darker extrusion (<see cref="GardenLook.WoodLetters"/>), ivy over both ends and a
        /// small pink flower. Never a touch target. <see cref="OwnerArt.Logo"/> shows the owner's picture instead when it
        /// exists.
        /// </summary>
        public static RectTransform WoodLogo(string name, Transform parent, string text)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            TypeStyle style = T.Wordmark;
            Image backLeft = IvyCluster("IvyBackLeft", root, flipped: false, back: true);
            Image backRight = IvyCluster("IvyBackRight", root, flipped: true, back: true);
            TextMeshProUGUI label = KitLabel("Letters", root, text, style, GardenLook.WoodLetters);
            Image frontLeft = IvyCluster("IvyLeft", root, flipped: false, back: false);
            Image frontRight = IvyCluster("IvyRight", root, flipped: true, back: false);
            Image flower = UiFactory.CreateImage("Flower", root, null, Color.white);
            PictureFit.On(flower, (w, h) => ProceduralSprites.LogoFlower(Mathf.Min(w, h)), square: true);
            layout.Watch(label).Then(b =>
            {
                float size = Units(style.Size);
                float natural = KitText.Measure(label, size);
                if (natural <= 0f)
                {
                    natural = b.Width * 0.86f;
                }

                float scale = Mathf.Min(b.Width * 0.86f / natural, b.Height * 0.72f / size);
                float width = natural * scale;
                float em = size * scale;
                float cy = b.CenterY - (em * 0.04f);
                KitText.Place(label, style, b.CenterX, cy, em, width + 1f);
                float leaf = em;
                Box left = Box.FromCenter(b.CenterX - (width / 2f) + (leaf * 0.05f), cy - (em * 0.12f), leaf, leaf);
                Box right = Box.FromCenter(b.CenterX + (width / 2f) - (leaf * 0.05f), cy - (em * 0.12f), leaf, leaf);
                BoxLayout.Place(backLeft.rectTransform, left);
                BoxLayout.Place(frontLeft.rectTransform, left);
                BoxLayout.Place(backRight.rectTransform, right);
                BoxLayout.Place(frontRight.rectTransform, right);
                float bloom = em * 0.4f;
                BoxLayout.Place(flower.rectTransform, Box.FromCenter(b.CenterX + (width / 2f) + (bloom * 0.1f), cy - (em * 0.46f), bloom, bloom));
            });
            return root;
        }

        // ---- Wardrobe pieces (§4.6; Unity only, the playtest has no Wardrobe) ----

        /// <summary>
        /// A Wardrobe family tab (§4.6, <c>ui.tab.family</c>): a cream tab with rounded top corners and a square bottom,
        /// the family's picture (put it in <see cref="FamilyTabView.Picture"/>) over its name. Selected, it is lighter,
        /// its name bold, and it joins the panel below (no bottom line); otherwise it is a little sunk and closed by a
        /// <c>cream.line</c> bottom. The whole tab is the touch target.
        /// </summary>
        public static FamilyTabView FamilyTab(string name, Transform parent, string label, Action onClick)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent, raycast: true);
            var view = root.gameObject.AddComponent<FamilyTabView>();
            view.Build(layout, label);
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = root.GetComponent<Image>();
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick();
            });
            root.gameObject.AddComponent<PressMotion>();
            return view;
        }

        /// <summary>
        /// A Wardrobe outfit card (§4.6, <c>ui.card.outfit</c>): a raised cream card with a beige picture well (put the
        /// outfit's picture in <see cref="OutfitCardView.Picture"/>) and the name below it. The worn outfit has a green tint,
        /// a 4-unit green border and a green check badge on the well's corner. The whole card is the touch target.
        /// </summary>
        public static OutfitCardView OutfitCard(string name, Transform parent, string label, Action onClick)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent, raycast: true);
            var view = root.gameObject.AddComponent<OutfitCardView>();
            view.Build(layout, label);
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = root.GetComponent<Image>();
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick();
            });
            root.gameObject.AddComponent<PressMotion>();
            return view;
        }

        private static float Frac(float v) => v - (float)Math.Floor(v);

        internal static float Fraction(float v) => Frac(v);
    }

    /// <summary>A candy tile built by <see cref="UiKit.CandyTile(string, Transform, VariantId?, TileStyle, TileState)"/>.</summary>
    public sealed class CandyTileView : MonoBehaviour
    {
        private BoxLayout _layout = null!;
        private TileStyle _style;
        private bool _pressed;

        /// <summary>The tile picture.</summary>
        public Image Image { get; private set; } = null!;

        /// <summary>Whether the face is sunk into its lip (the press, §3.1).</summary>
        public bool Pressed
        {
            get => _pressed;
            set
            {
                if (_pressed != value)
                {
                    _pressed = value;
                    _layout.Apply();
                }
            }
        }

        internal void Init(Image image, BoxLayout layout, TileStyle style)
        {
            Image = image;
            _layout = layout;
            _style = style;
            layout.Add(image.rectTransform, TileBox);
        }

        /// <summary>Shows a variant (null: the mystery tile) in a state.</summary>
        public void Show(VariantId? variant, TileState state = TileState.Normal)
        {
            TileStyle style = _style;
            PictureFit.On(Image, (w, h) => ProceduralSprites.CandyTile(variant, style, state, w), false, PictureShape.SquareByWidth);
        }

        /// <summary>Shows any color with a variant icon in a state.</summary>
        public void Show(Rgba color, string iconId, TileState state = TileState.Normal)
        {
            TileStyle style = _style;
            PictureFit.On(Image, (w, h) => ProceduralSprites.CandyTile(color, iconId, style, state, w), false, PictureShape.SquareByWidth);
        }

        /// <summary>
        /// The tile's square in the rect, or pressed, the same square less its top 70% of the lip: the picture is drawn
        /// a little shorter so the face sits lower and the lip shows less.
        /// </summary>
        private Box TileBox(Box box)
        {
            float s = Mathf.Min(box.Width, box.Height);
            Box square = Box.FromCenter(box.CenterX, box.CenterY, s, s);
            if (!_pressed)
            {
                return square;
            }

            float sink = s * UiRaster.TileLipShare(_style) * 0.7f;
            return new Box(square.Left, square.Top + sink, square.Right, square.Bottom);
        }
    }

    /// <summary>A wooden sign built by <see cref="UiKit.WoodSign"/>.</summary>
    public sealed class WoodSignView : MonoBehaviour
    {
        private TypeStyle _style = DesignTokens.Type.LevelPill;

        /// <summary>The sign's letters.</summary>
        public TextMeshProUGUI Label { get; private set; } = null!;

        /// <summary>The plank picture.</summary>
        public Image Plank { get; private set; } = null!;

        /// <summary>The sign's text.</summary>
        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        internal void Init(TextMeshProUGUI label, Image plank, TypeStyle style)
        {
            Label = label;
            Plank = plank;
            _style = style;
        }

        /// <summary>The letters' color (<c>ink.brown</c>; <c>badge.super_hard</c> on a Super Hard level), embossed.</summary>
        public void SetLetters(Rgba ink) => UiKit.ApplyLook(Label, _style, GardenLook.SignLetters(ink));
    }

    /// <summary>A cost pill built by <see cref="UiKit.CostPill"/>.</summary>
    public sealed class CostPillView : MonoBehaviour
    {
        private TextMeshProUGUI _label = null!;
        private Image _lotus = null!;
        private GameObject _free = null!;
        private BoxLayout _layout = null!;

        /// <summary>What the pill shows.</summary>
        public Cost Cost { get; private set; }

        internal void Init(TextMeshProUGUI label, Image lotus, GameObject free, BoxLayout layout)
        {
            _label = label;
            _lotus = lotus;
            _free = free;
            _layout = layout;
        }

        /// <summary>Shows a price with the lotus, Free with the green ▶, or ×N charges.</summary>
        public void SetCost(Cost cost)
        {
            Cost = cost;
            _label.text = Text(cost);
            _lotus.gameObject.SetActive(cost.Kind == CostKind.Petals);
            _free.SetActive(cost.Kind == CostKind.Free);
            _layout.Apply();
        }

        /// <summary>The pill's text: the grouped price, "Free" or "×N".</summary>
        public static string Text(Cost cost) => cost.Kind switch
        {
            CostKind.Petals => NumberText.Group(cost.Amount),
            CostKind.Free => Loc.T("common.free"),
            _ => Loc.F("common.charges", cost.Amount),
        };
    }

    /// <summary>A jam choice built by <see cref="UiKit.ChoiceButton"/>.</summary>
    public sealed class ChoiceButtonView : MonoBehaviour
    {
        /// <summary>The cost pill's height over the button's (§3.3).</summary>
        public const float PillShare = 0.3f;

        private CanvasGroup _fade = null!;
        private Image? _icon;
        private IReadOnlyList<IconPart>? _parts;
        private BoxLayout _layout = null!;
        private bool _hasCost;

        /// <summary>The touch target and click.</summary>
        public Button Button { get; internal set; } = null!;

        /// <summary>The glossy face (its press and colors).</summary>
        public GardenButton Face { get; private set; } = null!;

        /// <summary>The white outlined label.</summary>
        public TextMeshProUGUI Label { get; private set; } = null!;

        /// <summary>The cost pill under the button.</summary>
        public CostPillView CostPill { get; private set; } = null!;

        /// <summary>Whether the choice can be taken (else greyed at 55%, the icon grey).</summary>
        public bool Interactable
        {
            get => Button.interactable;
            set => Button.interactable = value;
        }

        internal void Init(GardenButton face, TextMeshProUGUI label, CostPillView pill, CanvasGroup fade, Image? icon, IReadOnlyList<IconPart>? parts, BoxLayout layout)
        {
            Face = face;
            Label = label;
            CostPill = pill;
            _fade = fade;
            _icon = icon;
            _parts = parts;
            _layout = layout;
            face.Recolored += OnRecolored;
        }

        /// <summary>Shows a cost under the button, or none (the button then fills the rect).</summary>
        public void SetCost(Cost? cost)
        {
            _hasCost = cost.HasValue;
            CostPill.gameObject.SetActive(cost.HasValue);
            if (cost.HasValue)
            {
                CostPill.SetCost(cost.Value);
            }

            _layout.Apply();
        }

        /// <summary>The button's box in the rect: above the pill's lower part when it has a cost.</summary>
        internal Box ButtonBox(Box box)
        {
            float height = _hasCost ? box.Height / (1f + (0.6f * PillShare)) : box.Height;
            return new Box(box.Left, box.Top, box.Right, box.Top + height);
        }

        private void OnRecolored(ColorSet shown, bool enabled)
        {
            _fade.alpha = enabled ? 1f : 0.55f;
            if (_icon != null && _parts != null)
            {
                UiKit.SetIconParts(_icon, _parts, !enabled);
            }
        }
    }

    /// <summary>A pod built by <see cref="UiKit.Pod"/>.</summary>
    public sealed class KitPodView : MonoBehaviour
    {
        private BoxLayout _layout = null!;
        private Image _shadow = null!;
        private Image _stem = null!;
        private Image _knob = null!;
        private Image _panel = null!;
        private Image _frame = null!;
        private Image _veil = null!;
        private Image _lock = null!;
        private CandyTileView _tile = null!;
        private TextMeshProUGUI _count = null!;
        private RectTransform _content = null!;
        private float _radius;
        private float _panelRadius;

        /// <summary>The pod's state.</summary>
        public PodLook Look { get; private set; }

        /// <summary>Whether the exposed pod shows its wooden handle.</summary>
        public bool Handle { get; private set; } = true;

        /// <summary>The inner panel inside the frame (marks over the pod go here).</summary>
        public RectTransform Content => _content;

        /// <summary>The variant tile.</summary>
        public CandyTileView Tile => _tile;

        /// <summary>The count label.</summary>
        public TextMeshProUGUI Count => _count;

        internal void Build(BoxLayout layout)
        {
            _layout = layout;
            Transform root = layout.transform;
            _shadow = UiKit.RoundRect("Shadow", root, UiTheme.Of(C.GardenShadow.WithAlpha(0.26f)), _ => _radius);
            _stem = UiKit.RoundRect("HandleStem", root, UiTheme.Of(C.WoodDarkLine));
            _knob = UiKit.WoodPlank("Handle", root, 0.5f, 9, WoodTone.Dark);
            _panel = UiKit.RoundGradient("Panel", root, C.CreamTop, C.CreamFace, _ => _panelRadius);
            _frame = UiFactory.CreateImage("Frame", root, null, Color.white);
            PictureFit.On(_frame, (w, h) => ProceduralSprites.Frame(WoodTone.Dark, w, h), sliced: true);
            _veil = UiKit.RoundRect("Veil", root, UiTheme.Of(C.ParchmentBottom.WithAlpha(0.45f)), _ => _radius);
            _content = UiFactory.CreateRect("Content", root);
            _tile = UiKit.CandyTile("Tile", root, null, TileStyle.Sticker);
            _lock = UiKit.ShapeImage("Lock", root, "ui.lock", C.StateLock.Darken(0.2f));
            _count = UiKit.KitLabel("Count", root, string.Empty, T.Count, TextLook.Plain(C.InkBrown));
            layout.Then(Lay);
        }

        /// <summary>Shows a pod: its variant (null: a mystery), its count, its look and whether it has the handle.</summary>
        public void Show(VariantId? variant, int count, PodLook look, bool handle = true)
        {
            bool queued = look == PodLook.Next;
            bool locked = look == PodLook.Locked;
            Rgba? tint = variant.HasValue && !queued && !locked && VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : (Rgba?)null;
            ShowFrame(look, handle, tint);
            _lock.gameObject.SetActive(locked);
            _tile.gameObject.SetActive(!locked);
            _count.gameObject.SetActive(true);
            if (!locked)
            {
                _tile.Show(variant, queued ? TileState.Dimmed : TileState.Normal);
            }

            _count.text = count.ToString(CultureInfo.InvariantCulture);
            _layout.Apply();
        }

        /// <summary>
        /// Shows only the wooden frame (the playtest's <c>Kit.PodFrame</c>): its handle, the inner panel tinted by
        /// <paramref name="tint"/> (a variant color; plain cream without one, <c>state.lock_bg</c> when locked), dimmed when
        /// queued, sunk when pressed. Put custom content in <see cref="Content"/>; <see cref="Show"/> brings back the tile
        /// and the count.
        /// </summary>
        public void ShowFrame(PodLook look, bool handle = true, Rgba? tint = null)
        {
            Look = look;
            Handle = handle;
            bool queued = look == PodLook.Next;
            bool locked = look == PodLook.Locked;
            _shadow.color = UiTheme.Of(C.GardenShadow.WithAlpha(queued ? 0.12f : 0.26f));
            _stem.gameObject.SetActive(handle && !queued);
            _knob.gameObject.SetActive(handle && !queued);
            if (locked)
            {
                UiKit.Gradient(_panel, UiTheme.Of(C.StateLockBg.Lighten(0.2f)), UiTheme.Of(C.StateLockBg));
            }
            else if (tint.HasValue)
            {
                UiKit.Gradient(_panel, UiTheme.Of(tint.Value.Mix(C.CreamTop, 0.78f)), UiTheme.Of(C.CreamFace));
            }
            else
            {
                UiKit.Gradient(_panel, UiTheme.Of(C.CreamTop), UiTheme.Of(C.CreamFace));
            }

            _veil.gameObject.SetActive(queued);
            _lock.gameObject.SetActive(false);
            _tile.gameObject.SetActive(false);
            _count.gameObject.SetActive(false);
            _layout.Apply();
        }

        private void Lay(Box box)
        {
            float w = box.Width;
            bool pressed = Look == PodLook.Pressed;
            Box frame = pressed ? box.Offset(0f, w * 0.035f) : box;
            _radius = w * 0.18f;
            float border = w * 0.11f;
            _panelRadius = Mathf.Max(0f, _radius - (border * 0.8f));
            BoxLayout.Place(_shadow.rectTransform, frame.Offset(0f, w * (pressed ? 0.02f : 0.05f)).Inset(w * 0.03f, 0f));
            float handleHeight = w * 0.15f;
            Box knob = Box.FromCenter(frame.CenterX, frame.Top - (handleHeight * 0.12f), w * 0.34f, handleHeight);
            BoxLayout.Place(_stem.rectTransform, Box.FromCenter(knob.CenterX, knob.Top - (handleHeight * 0.12f), w * 0.04f, handleHeight * 0.45f));
            BoxLayout.Place(_knob.rectTransform, knob);
            BoxLayout.Place(_panel.rectTransform, frame.Inset(border * 0.8f));
            BoxLayout.Place(_frame.rectTransform, frame);
            BoxLayout.Place(_veil.rectTransform, frame);
            Box inner = frame.Inset(border);
            BoxLayout.Place(_content, inner);
            float tile = inner.Width * 0.7f;
            Box tileBox = Box.FromCenter(inner.CenterX, inner.Top + (inner.Height * 0.04f) + (tile / 2f), tile, tile);
            BoxLayout.Place((RectTransform)_tile.transform, tileBox);
            BoxLayout.Place(_lock.rectTransform, Box.FromCenter(tileBox.CenterX, tileBox.CenterY, tile * 0.62f, tile * 0.62f));
            UiKit.PlaceCount(_count, new Box(inner.Left, tileBox.Bottom, inner.Right, inner.Bottom), Look == PodLook.Next || Look == PodLook.Locked);
            foreach (Image image in new[] { _shadow, _panel, _veil })
            {
                image.GetComponent<RoundShape>().Apply();
            }
        }
    }

    /// <summary>A Waiting Slot built by <see cref="UiKit.SlotPlate"/>.</summary>
    public sealed class SlotPlateView : MonoBehaviour
    {
        private BoxLayout _layout = null!;
        private GameObject _empty = null!;
        private Image _emptyFill = null!;
        private Image _emptyShade = null!;
        private Image _emptyLine = null!;
        private Image _dashed = null!;
        private Image _dangerFill = null!;
        private Image _dangerMark = null!;
        private GameObject _filled = null!;
        private Image _lip = null!;
        private Image _face = null!;
        private Image _line = null!;
        private Image _lock = null!;
        private CandyTileView _tile = null!;
        private TextMeshProUGUI _count = null!;
        private GameObject _hourglass = null!;
        private Image _hourShadow = null!;
        private Image _hourRing = null!;
        private Image _hourDisc = null!;
        private Image _hourGlyph = null!;
        private GameObject _extra = null!;
        private Image _extraRing = null!;
        private Image _extraDisc = null!;
        private Image _extraGlyph = null!;
        private float _radius;

        /// <summary>The slot's state.</summary>
        public SlotPlateState State { get; private set; }

        /// <summary>Whether the slot carries the Extra Slot's green "+".</summary>
        public bool Extra { get; private set; }

        /// <summary>The count shown under the tile.</summary>
        public int CountValue { get; private set; }

        /// <summary>The variant tile.</summary>
        public CandyTileView Tile => _tile;

        internal void Build(BoxLayout layout)
        {
            _layout = layout;
            Transform root = layout.transform;
            _empty = UiFactory.CreateRect("Empty", root).gameObject;
            UiFactory.Stretch((RectTransform)_empty.transform);
            _emptyFill = UiKit.RoundRect("Fill", _empty.transform, UiTheme.Of(C.CreamFace.Mix(C.ParchmentWell, 0.35f)), _ => _radius);
            _emptyShade = UiKit.RoundRect("Shade", _empty.transform, Color.white, _ => _radius);
            UiKit.Gradient(_emptyShade, UiTheme.Of(C.GardenShadow.WithAlpha(0.1f)), UiTheme.Of(C.GardenShadow.WithAlpha(0f)));
            _emptyShade.GetComponent<VerticalGradient>().Stop = 0.3f;
            _emptyLine = UiKit.RoundRing("Line", _empty.transform, UiTheme.Of(C.CreamLine.WithAlpha(0.35f)), _ => _radius, b => Mathf.Max(UiKit.Units(1f), Mathf.Min(b.Width, b.Height) * 0.018f));
            _dangerFill = UiKit.RoundRect("DangerFill", _empty.transform, UiTheme.Of(C.StateDanger.WithAlpha(0.07f)), _ => _radius * 0.7f);
            _dashed = UiFactory.CreateImage("Dashed", _empty.transform, null, Color.white);
            PictureFit.On(_dashed, (w, h) => ProceduralSprites.DashedOutline(w, h, 0.09f, 0.14f, 0.028f, 0.09f, 0.06f));
            _dangerMark = UiKit.ShapeImage("Danger", _empty.transform, "slot.state.jam_risk", C.StateDanger);

            _filled = UiFactory.CreateRect("Filled", root).gameObject;
            UiFactory.Stretch((RectTransform)_filled.transform);
            BoxLayout shadowLayout = BoxLayout.On((RectTransform)_filled.transform);
            UiKit.SoftShadow(shadowLayout, b => b, b => Mathf.Min(b.Width, b.Height) * 0.2f, 0.18f, 0.04f);
            _lip = UiKit.RoundRect("Lip", _filled.transform, Color.white, _ => _radius);
            _face = UiKit.RoundGradient("Face", _filled.transform, C.CreamTop, C.CreamFace, _ => _radius);
            _line = UiKit.RoundRing("Line", _filled.transform, Color.white, _ => _radius, b => Mathf.Max(UiKit.Units(1f), Mathf.Min(b.Width, b.Height) * 0.018f));
            _lock = UiKit.ShapeImage("Lock", _filled.transform, "ui.lock", C.StateLock.Darken(0.15f));
            _tile = UiKit.CandyTile("Tile", _filled.transform, null, TileStyle.Sticker);
            _count = UiKit.KitLabel("Count", _filled.transform, string.Empty, T.Count, TextLook.Plain(C.InkBrown));
            _hourglass = UiFactory.CreateRect("Hourglass", _filled.transform).gameObject;
            UiFactory.Stretch((RectTransform)_hourglass.transform);
            _hourShadow = UiKit.RoundRect("Shadow", _hourglass.transform, UiTheme.Of(C.GardenShadow.WithAlpha(0.2f)));
            _hourRing = UiKit.RoundRect("Ring", _hourglass.transform, UiTheme.Of(C.CreamLine));
            _hourDisc = UiKit.RoundRect("Disc", _hourglass.transform, Color.white);
            _hourGlyph = UiKit.ShapeImage("Glyph", _hourglass.transform, "slot.state.waiting", C.InkBrownSoft);

            _extra = UiFactory.CreateRect("Extra", root).gameObject;
            UiFactory.Stretch((RectTransform)_extra.transform);
            _extraRing = UiKit.RoundRect("Ring", _extra.transform, Color.white);
            _extraDisc = UiKit.RoundRect("Disc", _extra.transform, UiTheme.Of(GardenLook.Green.Face));
            _extraGlyph = UiKit.ShapeImage("Plus", _extra.transform, "ui.plus", C.TextOnColor);
            layout.Then(Lay);
            Show(SlotPlateState.Empty);
        }

        /// <summary>
        /// Shows the slot: empty (dashed), danger (dashed red with "!"), working (tile and count), stuck (grey tile and the
        /// hourglass) or locked (padlock); <paramref name="extra"/> adds the Extra Slot's green "+". A null
        /// <paramref name="variant"/> in a filled slot is a mystery pod.
        /// </summary>
        public void Show(SlotPlateState state, VariantId? variant = null, int count = 0, bool extra = false)
        {
            State = state;
            Extra = extra;
            CountValue = count;
            bool empty = state == SlotPlateState.Empty || state == SlotPlateState.Danger;
            bool danger = state == SlotPlateState.Danger;
            bool locked = state == SlotPlateState.Locked;
            bool stuck = state == SlotPlateState.Stuck;
            _empty.SetActive(empty);
            _filled.SetActive(!empty);
            _dangerFill.gameObject.SetActive(danger);
            _dangerMark.gameObject.SetActive(danger);
            _dashed.color = UiTheme.Of(danger ? C.StateDanger : C.CreamLine.WithAlpha(0.8f));
            _line.color = UiTheme.Of(locked ? C.StateLock : C.CreamLine);
            _lip.color = UiTheme.Of(locked ? C.StateLockBg.Darken(0.15f) : C.CreamLip);
            UiKit.Gradient(_face, UiTheme.Of(locked ? C.StateLockBg.Lighten(0.25f) : C.CreamTop), UiTheme.Of(locked ? C.StateLockBg : C.CreamFace));
            _lock.gameObject.SetActive(locked);
            _tile.gameObject.SetActive(!empty && !locked);
            _count.gameObject.SetActive(!empty && !locked);
            if (!empty && !locked)
            {
                _tile.Show(variant, stuck && variant.HasValue ? TileState.Grey : TileState.Normal);
                _count.text = count.ToString(CultureInfo.InvariantCulture);
            }

            _hourglass.SetActive(stuck && count > 0);
            _extra.SetActive(extra);
            _layout.Apply();
        }

        private void Lay(Box box)
        {
            float s = Mathf.Min(box.Width, box.Height);
            _radius = s * 0.2f;
            BoxLayout.Place(_emptyFill.rectTransform, box);
            BoxLayout.Place(_emptyShade.rectTransform, box);
            BoxLayout.Place(_emptyLine.rectTransform, box);
            Box dashed = box.Inset(s * 0.09f);
            BoxLayout.Place(_dangerFill.rectTransform, dashed);
            BoxLayout.Place(_dashed.rectTransform, box);
            BoxLayout.Place(_dangerMark.rectTransform, box.Inset(s * 0.32f));

            float lip = s * 0.07f;
            var face = new Box(box.Left, box.Top, box.Right, box.Bottom - lip);
            BoxLayout.Place(_lip.rectTransform, box);
            BoxLayout.Place(_face.rectTransform, face);
            BoxLayout.Place(_line.rectTransform, box);
            BoxLayout.Place(_lock.rectTransform, Box.FromCenter(face.CenterX, face.CenterY, s * 0.44f, s * 0.44f));
            float tile = face.Width * 0.64f;
            Box tileBox = Box.FromCenter(face.CenterX, face.Top + (face.Height * 0.06f) + (tile / 2f), tile, tile);
            BoxLayout.Place((RectTransform)_tile.transform, tileBox);
            UiKit.PlaceCount(_count, new Box(face.Left, tileBox.Bottom, face.Right, face.Bottom - (face.Height * 0.02f)), State == SlotPlateState.Stuck);

            float b = s * 0.17f;
            float ring = Mathf.Max(UiKit.Units(1f), s * 0.012f);
            float bx = box.Right - (b * 0.55f);
            float by = box.Top + (b * 0.55f);
            BoxLayout.Place(_hourShadow.rectTransform, Box.FromCenter(bx, by + (b * 0.12f), 2f * (b + ring), 2f * (b + ring)));
            BoxLayout.Place(_hourRing.rectTransform, Box.FromCenter(bx, by, 2f * (b + ring), 2f * (b + ring)));
            BoxLayout.Place(_hourDisc.rectTransform, Box.FromCenter(bx, by, 2f * b, 2f * b));
            BoxLayout.Place(_hourGlyph.rectTransform, Box.FromCenter(bx, by, b * 1.3f, b * 1.3f));

            float r = s * 0.17f;
            float ex = box.Left + (r * 0.55f);
            float ey = box.Top + (r * 0.55f);
            float edge = Mathf.Max(UiKit.Units(1f), s * 0.02f);
            BoxLayout.Place(_extraRing.rectTransform, Box.FromCenter(ex, ey, 2f * (r + edge), 2f * (r + edge)));
            BoxLayout.Place(_extraDisc.rectTransform, Box.FromCenter(ex, ey, 2f * r, 2f * r));
            BoxLayout.Place(_extraGlyph.rectTransform, Box.FromCenter(ex, ey, r * 1.2f, r * 1.2f));
            foreach (Image image in new[] { _emptyFill, _emptyShade, _emptyLine, _dangerFill, _line, _lip, _face })
            {
                image.GetComponent<RoundShape>().Apply();
            }
        }
    }

    /// <summary>A booster tile built by <see cref="UiKit.BoosterTile"/>.</summary>
    public sealed class BoosterTileView : MonoBehaviour
    {
        private BoxLayout _layout = null!;
        private CanvasGroup _fade = null!;
        private RectTransform _glow = null!;
        private CanvasGroup _glowFade = null!;
        private Image[] _halos = Array.Empty<Image>();
        private Image _ring = null!;
        private Image _icon = null!;
        private Image _badgeDisc = null!;
        private TextMeshProUGUI _badge = null!;
        private CostPillView _cost = null!;
        private GameObject _plus = null!;
        private Image _plusLine = null!;
        private Image _plusFace = null!;
        private Image _plusGlyph = null!;
        private string _boosterId = string.Empty;
        private float _radius;
        private readonly float[] _haloRadii = new float[3];

        /// <summary>The tile's face (press, colors, its <see cref="GardenButton.Button"/>).</summary>
        public GardenButton Face { get; private set; } = null!;

        /// <summary>The tile's button.</summary>
        public Button Button { get; private set; } = null!;

        /// <summary>What the tile shows.</summary>
        public BoosterTileState State { get; private set; }

        internal void Build(BoxLayout layout, string boosterId, Action? onPress)
        {
            _layout = layout;
            _boosterId = boosterId;
            Transform root = layout.transform;
            _fade = gameObject.AddComponent<CanvasGroup>();
            _glow = UiFactory.Stretch(UiFactory.CreateRect("Glow", root));
            _glowFade = _glow.gameObject.AddComponent<CanvasGroup>();
            _glowFade.blocksRaycasts = false;
            _halos = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                int ring = 3 - i;
                _halos[i] = UiKit.RoundRect("Halo" + ring, _glow, UiTheme.Of(C.GardenGlow.WithAlpha(0.22f)), _ => _haloRadii[ring - 1]);
            }

            _ring = UiKit.RoundRing("Ring", _glow, UiTheme.Of(C.GardenGlow), _ => _radius + (Side() * 0.035f), _ => Side() * 0.035f);

            Rgba rim = GardenLook.BoosterRim;
            var set = new ColorSet("set.cream.booster_tile", C.CreamFace, rim.Lighten(0.62f), rim.Lighten(0.08f), rim.Darken(0.22f));
            Face = UiKit.IconFace("Tile", root, set, b => Mathf.Min(b.Width, b.Height) * 0.26f, square: false, raycast: true);
            Face.TileSquash = true;
            _icon = UiKit.BoosterIcon("Icon", Face.Content, boosterId);
            BoxLayout.On(Face.Content).Add(_icon.rectTransform, f => Box.FromCenter(f.CenterX, f.CenterY, Face.IconSide * 0.62f, Face.IconSide * 0.62f));

            _badge = UiKit.CountBadge("Count", root, out _badgeDisc);
            _cost = UiKit.CostPill("Cost", root, Cost.Petals(0));
            _plus = UiFactory.Stretch(UiFactory.CreateRect("Plus", root)).gameObject;
            _plusLine = UiKit.RoundRect("Line", _plus.transform, UiTheme.Of(GardenLook.Green.Line));
            _plusFace = UiKit.RoundRect("Face", _plus.transform, UiTheme.Of(GardenLook.Green.Face));
            _plusGlyph = UiKit.ShapeImage("Glyph", _plus.transform, "ui.plus", Rgba.White);

            Button = Face.gameObject.AddComponent<Button>();
            Button.transition = Selectable.Transition.None;
            Button.targetGraphic = Face.Top;
            Button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onPress?.Invoke();
            });
            Face.Button = Button;
            Face.Recolored += (shown, enabled) => UiKit.SetBoosterIcon(_icon, _boosterId, !enabled);
            layout.Then(Lay);
            Show(new BoosterTileState(1, 0, false, true, true));
        }

        /// <summary>Shows the tile's state: charges or price, selected (raised, glowing) or disabled (greyed at 55%).</summary>
        public void Show(BoosterTileState state)
        {
            State = state;
            Button.interactable = !state.Disabled;
            _fade.alpha = state.Disabled ? 0.55f : 1f;
            _glow.gameObject.SetActive(state.Selected);
            _badgeDisc.gameObject.SetActive(state.ShowsCharges);
            _badge.text = state.Charges.ToString(CultureInfo.InvariantCulture);
            _cost.gameObject.SetActive(!state.ShowsCharges);
            _plus.SetActive(!state.ShowsCharges);
            if (!state.ShowsCharges)
            {
                _cost.SetCost(Cost.Petals(state.Price));
            }

            _layout.Apply();
        }

        private float Side()
        {
            Rect rect = ((RectTransform)transform).rect;
            return Mathf.Min(rect.width, rect.height);
        }

        private void Lay(Box box)
        {
            float s = Mathf.Min(box.Width, box.Height);
            Box tile = State.Selected ? box.Offset(0f, -s * 0.08f) : box;
            _radius = s * 0.26f;
            for (int i = 0; i < 3; i++)
            {
                int ring = 3 - i;
                float grow = s * 0.18f * ring / 3f;
                _haloRadii[ring - 1] = _radius + grow;
                BoxLayout.Place(_halos[i].rectTransform, tile.Inset(-grow));
                _halos[i].GetComponent<RoundShape>().Apply();
            }

            BoxLayout.Place(_ring.rectTransform, tile.Inset(-s * 0.035f));
            _ring.GetComponent<RoundShape>().Apply();
            BoxLayout.Place((RectTransform)Face.transform, tile);
            float badge = s * 0.34f;
            BoxLayout.Place(_badgeDisc.rectTransform, Box.FromCenter(tile.Right - (badge / 6f), tile.Bottom - (badge / 6f), badge * 1.26f, badge * 1.26f));
            float pill = s * 0.3f;
            BoxLayout.Place((RectTransform)_cost.transform, Box.FromCenter(tile.CenterX, tile.Bottom + (pill * 0.12f), s * 0.86f, pill));
            float plus = s * 0.3f;
            float px = tile.Right - (plus * 0.3f);
            float py = tile.Top + (plus * 0.3f);
            float edge = Mathf.Max(UiKit.Units(1f), s * 0.012f);
            BoxLayout.Place(_plusLine.rectTransform, Box.FromCenter(px, py + (plus * 0.07f), plus + (2f * edge), plus + (2f * edge)));
            BoxLayout.Place(_plusFace.rectTransform, Box.FromCenter(px, py, plus, plus));
            BoxLayout.Place(_plusGlyph.rectTransform, Box.FromCenter(px, py, plus * 0.62f, plus * 0.62f));
        }

        private void Update()
        {
            if (State.Selected)
            {
                _glowFade.alpha = GardenLook.Glow(Time.unscaledTime);
            }
        }
    }

    /// <summary>
    /// The stone border built by <see cref="UiKit.StoneBorder"/>: it lays its blocks out again whenever the grid changes
    /// size or shape.
    /// </summary>
    public sealed class StoneBorderView : MonoBehaviour
    {
        private readonly List<Image> _blocks = new List<Image>();
        private Image _shadow = null!;
        private Image _joints = null!;
        private Image _gap = null!;
        private int _columns = 1;
        private int _rows = 1;
        private float _thickness = 0.42f;
        private float _shadowRadius;
        private float _jointRadius;
        private float _gapRadius;

        internal void Init(int columns, int rows, float thickness)
        {
            _shadow = UiKit.RoundRect("Shadow", transform, UiTheme.Of(C.GardenShadow.WithAlpha(0.22f)), _ => _shadowRadius);
            _joints = UiKit.RoundRect("Joints", transform, UiTheme.Of(C.StoneLine.WithAlpha(0.6f)), _ => _jointRadius);
            _gap = UiKit.RoundRect("Gap", transform, UiTheme.Of(GardenLook.BoardGap), _ => _gapRadius);
            SetGrid(columns, rows, thickness);
        }

        /// <summary>The grid's columns and rows (the cell is the smaller of its width and height over them) and the thickness in cells.</summary>
        public void SetGrid(int columns, int rows, float thickness = 0.42f)
        {
            _columns = Math.Max(1, columns);
            _rows = Math.Max(1, rows);
            _thickness = thickness;
            Lay();
        }

        /// <summary>
        /// The border's blocks for a grid of <paramref name="grid"/> (top-down) with cells of <paramref name="cell"/>: each
        /// block's box, stone seed and corner radius share (the playtest's <c>Kit.StoneBorder</c>).
        /// </summary>
        public static List<(Box Box, int Seed, float RadiusShare)> Blocks(Box grid, float cell, float thickness)
        {
            var blocks = new List<(Box, int, float)>();
            float gap = cell * 0.04f;
            float t = cell * thickness;
            float half = cell * 0.02f;
            Box inner = grid.Inset(-gap);
            Box outer = inner.Inset(-t);
            blocks.Add((new Box(outer.Left, outer.Top, inner.Left, inner.Top).Inset(half), 1, 0.4f));
            blocks.Add((new Box(inner.Right, outer.Top, outer.Right, inner.Top).Inset(half), 2, 0.4f));
            blocks.Add((new Box(outer.Left, inner.Bottom, inner.Left, outer.Bottom).Inset(half), 3, 0.4f));
            blocks.Add((new Box(inner.Right, inner.Bottom, outer.Right, outer.Bottom).Inset(half), 4, 0.4f));
            Run(blocks, inner.Left, inner.Right, cell, 0, (a, b) => new Box(a, outer.Top, b, inner.Top), half);
            Run(blocks, inner.Left, inner.Right, cell, 1, (a, b) => new Box(a, inner.Bottom, b, outer.Bottom), half);
            Run(blocks, inner.Top, inner.Bottom, cell, 2, (a, b) => new Box(outer.Left, a, inner.Left, b), half);
            Run(blocks, inner.Top, inner.Bottom, cell, 3, (a, b) => new Box(inner.Right, a, outer.Right, b), half);
            return blocks;
        }

        /// <summary>One side of the border: blocks of 1.0 and 0.8 cell, stretched together to fill the side exactly.</summary>
        private static void Run(List<(Box, int, float)> blocks, float from, float to, float cell, int side, Func<float, float, Box> place, float half)
        {
            float length = to - from;
            int count = Math.Max(1, (int)Math.Round(length / (cell * 0.9f)));
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                total += Pattern(i, side);
            }

            float k = length / total;
            float at = from;
            for (int i = 0; i < count; i++)
            {
                float next = i == count - 1 ? to : at + (Pattern(i, side) * k);

                // A few stone looks, chosen by the block's place, keep the picture cache small.
                int seed = 11 + (((side * 7) + (i * 3)) % 8);
                blocks.Add((place(at, next).Inset(half), seed, 0.3f));
                at = next;
            }
        }

        private static float Pattern(int i, int side) => ((i + side) % 2) == 0 ? 1f : 0.8f;

        private void OnRectTransformDimensionsChange() => Lay();

        private void OnEnable() => Lay();

        private void Lay()
        {
            if (_shadow == null)
            {
                return;
            }

            Rect rect = ((RectTransform)transform).rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            var grid = new Box(0f, 0f, rect.width, rect.height);
            float cell = Mathf.Min(rect.width / _columns, rect.height / _rows);
            float gap = cell * 0.04f;
            float t = cell * _thickness;
            Box inner = grid.Inset(-gap);
            Box outer = inner.Inset(-t);
            _shadowRadius = t * 0.5f;
            _jointRadius = t * 0.4f;
            _gapRadius = gap * 2f;
            BoxLayout.Place(_shadow.rectTransform, outer.Offset(0f, t * 0.22f).Inset(-t * 0.05f, 0f));
            BoxLayout.Place(_joints.rectTransform, outer.Inset(cell * 0.02f));
            BoxLayout.Place(_gap.rectTransform, inner);
            foreach (Image image in new[] { _shadow, _joints, _gap })
            {
                image.GetComponent<RoundShape>().Apply();
            }

            List<(Box Box, int Seed, float RadiusShare)> blocks = Blocks(grid, cell, _thickness);
            for (int i = 0; i < blocks.Count; i++)
            {
                if (i >= _blocks.Count)
                {
                    _blocks.Add(UiFactory.CreateImage("Stone" + i, transform, null, Color.white));
                }

                Image block = _blocks[i];
                block.gameObject.SetActive(true);
                (Box box, int seed, float share) = blocks[i];
                BoxLayout.Place(block.rectTransform, box);
                PictureFit.On(block, (w, h) => ProceduralSprites.Stone(w, h, seed, share));
            }

            for (int i = blocks.Count; i < _blocks.Count; i++)
            {
                _blocks[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>The win's turning light rays (<see cref="UiKit.LightRays"/>): 0.05 turn per second on unscaled time.</summary>
    public sealed class LightRaysView : MonoBehaviour
    {
        private float _startedAt;

        private void OnEnable() => _startedAt = Time.unscaledTime;

        private void Update()
        {
            float seconds = Time.unscaledTime - _startedAt;
            transform.localEulerAngles = new Vector3(0f, 0f, -360f * 0.05f * seconds);
        }
    }

    /// <summary>The win's falling petals (<see cref="UiKit.FallingPetals"/>): the same time gives the same frame.</summary>
    public sealed class FallingPetalsView : MonoBehaviour
    {
        private const int Count = 10;
        private readonly Image[] _petals = new Image[Count];
        private float _startedAt;

        internal void Build()
        {
            for (int i = 0; i < Count; i++)
            {
                _petals[i] = UiKit.ShapeImage("Petal" + i, transform, "fx.petals", GardenLook.PetalShade(i));
            }
        }

        /// <summary>A petal's box (top-down, in the area) and squash at <paramref name="seconds"/> after the win (the playtest's recipe).</summary>
        public static (Box Box, float ScaleX, float ScaleY) Petal(int i, Box area, float seconds, float unit)
        {
            float a = UiKit.Fraction(i * 0.6180339f);
            float b = UiKit.Fraction((i * 0.3819660f) + 0.17f);
            float size = unit * 40f * (0.75f + (0.5f * b));
            float fall = area.Height * (0.1f + (0.07f * b));
            float span = area.Height + (size * 2f);
            float y = area.Top - size + (((a * span) + (seconds * fall)) % span);
            float x = area.Left + (area.Width * (0.05f + (0.9f * UiKit.Fraction(a * 7.31f)))) + ((float)Math.Sin((seconds * 1.3f) + (i * 1.7f)) * area.Width * 0.04f);
            float turn = (float)Math.Cos((seconds * 2.1f) + (i * 1.3f));
            return (Box.FromCenter(x, y, size, size), Math.Max(0.25f, Math.Abs(turn)), 0.8f + (0.2f * b));
        }

        private void OnEnable() => _startedAt = Time.unscaledTime;

        private void Update()
        {
            Rect rect = ((RectTransform)transform).rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            var area = new Box(0f, 0f, rect.width, rect.height);
            float seconds = Time.unscaledTime - _startedAt;
            float unit = UiKit.Units(1f);
            for (int i = 0; i < Count; i++)
            {
                (Box box, float sx, float sy) = Petal(i, area, seconds, unit);
                BoxLayout.Place(_petals[i].rectTransform, box);
                _petals[i].rectTransform.localScale = new Vector3(sx, sy, 1f);
            }
        }
    }

    /// <summary>A Wardrobe family tab built by <see cref="UiKit.FamilyTab"/>.</summary>
    public sealed class FamilyTabView : MonoBehaviour
    {
        private BoxLayout _layout = null!;
        private Image _face = null!;
        private Image _foot = null!;
        private Image _line = null!;
        private Image _leftEdge = null!;
        private Image _rightEdge = null!;
        private Image _bottomEdge = null!;
        private TextMeshProUGUI _label = null!;
        private float _radius;
        private float _stroke;

        /// <summary>Where the family's picture goes (a square over the name).</summary>
        public RectTransform Picture { get; private set; } = null!;

        /// <summary>The family's name.</summary>
        public TextMeshProUGUI Label => _label;

        /// <summary>Whether this tab is the selected one.</summary>
        public bool Selected { get; private set; }

        internal void Build(BoxLayout layout, string label)
        {
            _layout = layout;
            Transform root = layout.transform;
            _face = UiKit.RoundRect("Face", root, Color.white, _ => _radius);
            _line = UiKit.RoundRing("Line", root, UiTheme.Of(C.CreamLine), _ => _radius, _ => _stroke);
            _foot = UiKit.RoundRect("Foot", root, Color.white, _ => 0.5f);
            _leftEdge = UiKit.RoundRect("LeftEdge", root, UiTheme.Of(C.CreamLine), _ => 0.5f);
            _rightEdge = UiKit.RoundRect("RightEdge", root, UiTheme.Of(C.CreamLine), _ => 0.5f);
            _bottomEdge = UiKit.RoundRect("BottomEdge", root, UiTheme.Of(C.CreamLine), _ => 0.5f);
            Picture = UiFactory.CreateRect("Picture", root);
            _label = UiKit.KitLabel("Name", root, label, T.Body, TextLook.Plain(C.InkBrown));
            layout.Watch(_label).Then(Lay);
            Select(false);
        }

        /// <summary>Shows the tab selected (lighter, bold name, joined to the panel below) or not.</summary>
        public void Select(bool selected)
        {
            Selected = selected;
            Color face = UiTheme.Of(selected ? C.CreamTop : C.CreamFace.Mix(C.ParchmentWell, 0.5f));
            _face.color = face;
            _foot.color = face;
            _bottomEdge.gameObject.SetActive(!selected);
            UiKit.Style(_label, selected ? T.ButtonSecondary : T.Body, TextLook.Plain(C.InkBrown));
            _layout.Apply();
        }

        private void Lay(Box box)
        {
            _radius = box.Width * 0.14f;
            _stroke = Mathf.Max(UiKit.Units(2f), box.Width * 0.015f);

            // The rounded face and outline, their lower half squared off by the foot, and the sides redrawn straight there.
            BoxLayout.Place(_face.rectTransform, box);
            BoxLayout.Place(_line.rectTransform, box);
            // Selected, the foot reaches a little into the panel below and covers its top line there: the tab joins it.
            float foot = Selected ? box.Bottom + (_stroke * 2.5f) : box.Bottom;
            BoxLayout.Place(_foot.rectTransform, new Box(box.Left, box.CenterY, box.Right, foot));
            BoxLayout.Place(_leftEdge.rectTransform, new Box(box.Left, box.CenterY, box.Left + _stroke, foot));
            BoxLayout.Place(_rightEdge.rectTransform, new Box(box.Right - _stroke, box.CenterY, box.Right, foot));
            BoxLayout.Place(_bottomEdge.rectTransform, new Box(box.Left, box.Bottom - _stroke, box.Right, box.Bottom));
            float picture = box.Height * 0.56f;
            BoxLayout.Place(Picture, Box.FromCenter(box.CenterX, box.Top + (box.Height * 0.38f), picture, picture));
            TypeStyle style = Selected ? T.ButtonSecondary : T.Body;
            KitText.Place(_label, style, box.CenterX, box.Top + (box.Height * 0.8f), Mathf.Min(UiKit.Units(style.Size), box.Height * 0.2f), box.Width * 0.9f);
            foreach (Image image in new[] { _face, _foot, _line, _leftEdge, _rightEdge, _bottomEdge })
            {
                image.GetComponent<RoundShape>().Apply();
            }
        }
    }

    /// <summary>A Wardrobe outfit card built by <see cref="UiKit.OutfitCard"/>.</summary>
    public sealed class OutfitCardView : MonoBehaviour
    {
        private BoxLayout _layout = null!;
        private Image _lip = null!;
        private Image _face = null!;
        private Image _well = null!;
        private Image _border = null!;
        private GameObject _check = null!;
        private Image _checkRing = null!;
        private Image _checkDisc = null!;
        private Image _checkGlyph = null!;
        private TextMeshProUGUI _label = null!;
        private float _radius;
        private float _wellRadius;
        private float _stroke;

        /// <summary>Where the outfit's picture goes (inside the beige well).</summary>
        public RectTransform Picture { get; private set; } = null!;

        /// <summary>The outfit's name.</summary>
        public TextMeshProUGUI Label => _label;

        /// <summary>Whether the outfit is worn (green, with the check).</summary>
        public bool Worn { get; private set; }

        internal void Build(BoxLayout layout, string label)
        {
            _layout = layout;
            Transform root = layout.transform;
            UiKit.SoftShadow(layout, b => b, b => b.Width * 0.12f, 0.18f, 0.04f);
            _lip = UiKit.RoundRect("Lip", root, UiTheme.Of(C.CreamLip), _ => _radius);
            _face = UiKit.RoundGradient("Face", root, C.CreamTop, C.CreamFace, _ => _radius);
            _well = UiKit.RoundRect("Well", root, UiTheme.Of(C.ParchmentWell), _ => _wellRadius);
            Picture = UiFactory.CreateRect("Picture", root);
            _border = UiKit.RoundRing("Border", root, UiTheme.Of(C.CreamLine), _ => _radius, _ => _stroke);
            _label = UiKit.KitLabel("Name", root, label, T.ButtonSecondary, TextLook.Plain(C.InkBrown));
            _check = UiFactory.CreateRect("Check", root).gameObject;
            UiFactory.Stretch((RectTransform)_check.transform);
            _checkRing = UiKit.RoundRect("Ring", _check.transform, Color.white);
            _checkDisc = UiKit.RoundGradient("Disc", _check.transform, GardenLook.Green.Top, GardenLook.Green.Face);
            _checkGlyph = UiKit.ShapeImage("Glyph", _check.transform, "ui.check", Rgba.White);
            layout.Watch(_label).Then(Lay);
            Show(false);
        }

        /// <summary>Shows the card worn (green tint, green border, check) or not.</summary>
        public void Show(bool worn)
        {
            Worn = worn;
            ColorSet green = GardenLook.Green;
            UiKit.Gradient(_face, UiTheme.Of(worn ? green.Top.Mix(C.CreamTop, 0.7f) : C.CreamTop), UiTheme.Of(worn ? green.Face.Mix(C.CreamFace, 0.62f) : C.CreamFace));
            _well.color = UiTheme.Of(worn ? green.Top.Mix(C.CreamTop, 0.45f) : C.ParchmentWell);
            _border.color = UiTheme.Of(worn ? green.Face : C.CreamLine);
            _check.SetActive(worn);
            _layout.Apply();
        }

        private void Lay(Box box)
        {
            float w = box.Width;
            _radius = w * 0.12f;
            _wellRadius = w * 0.08f;
            _stroke = Worn ? UiKit.Units(4f) : Mathf.Max(UiKit.Units(2f), w * 0.015f);
            BoxLayout.Place(_lip.rectTransform, box.Offset(0f, box.Height * 0.025f));
            BoxLayout.Place(_face.rectTransform, box);
            var well = new Box(box.Left + (w * 0.08f), box.Top + (w * 0.08f), box.Right - (w * 0.08f), box.Top + (w * 0.08f) + (w * 0.84f));
            BoxLayout.Place(_well.rectTransform, well);
            BoxLayout.Place(Picture, well.Inset(w * 0.04f));
            BoxLayout.Place(_border.rectTransform, box);
            float labelTop = Math.Min(well.Bottom, box.Bottom);
            KitText.Place(_label, T.ButtonSecondary, box.CenterX, (labelTop + box.Bottom) / 2f, Mathf.Min(UiKit.Units(T.ButtonSecondary.Size), Mathf.Min(w * 0.13f, (box.Bottom - labelTop) * 0.5f)), w * 0.9f);
            float badge = w * 0.26f;
            float bx = well.Right - (badge * 0.35f);
            float by = well.Bottom - (badge * 0.35f);
            BoxLayout.Place(_checkRing.rectTransform, Box.FromCenter(bx, by, badge * 1.2f, badge * 1.2f));
            BoxLayout.Place(_checkDisc.rectTransform, Box.FromCenter(bx, by, badge, badge));
            BoxLayout.Place(_checkGlyph.rectTransform, Box.FromCenter(bx, by, badge * 0.62f, badge * 0.62f));
            foreach (Image image in new[] { _lip, _face, _well, _border })
            {
                image.GetComponent<RoundShape>().Apply();
            }
        }
    }
}
