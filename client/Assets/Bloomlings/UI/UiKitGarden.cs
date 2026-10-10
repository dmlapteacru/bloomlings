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

        /// <summary>
        /// A board cell of the picture's background (spec 005 FR-020, <c>tile.grass</c>; the playtest's
        /// <c>Kit.GrassCell</c>): a square of lawn filling the rect, the cell's own inset and soft rim included. Never a
        /// touch target.
        /// </summary>
        public static Image GrassCell(string name, Transform parent, int seed)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            PictureFit.On(image, (w, h) => ProceduralSprites.Grass(seed, w, h));
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
        /// A wooden sign (§3.2; the gameplay level, the win and banner titles, the popups' titles, the Home level plaque): a
        /// plank of the buttons' plate laminate (<see cref="ProceduralSprites.LaminateSign"/>, spec 005 FR-045) filling the
        /// rect (radius 28% of its height) over a soft shadow, the text centered in <c>ink.brown</c>
        /// (<c>ink.title</c> on the win's flower sign, or <paramref name="letters"/>) with a light emboss, at most 82% of the plank wide and 62% of its height tall, and its
        /// decoration: ivy over both ends (clusters 1.25 × the height, the back leaves behind the plank), or flower clusters
        /// at the top-left and bottom-right ends (1.35 × the height); <paramref name="ivyScale"/> sizes the ivy clusters (the
        /// pages' banners). Never a touch target.
        /// </summary>
        public static WoodSignView WoodSign(string name, Transform parent, string text, TypeStyle style, SignDecor decor = SignDecor.None, Rgba? letters = null, float ivyScale = 1f)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<WoodSignView>();
            SoftShadow(layout, b => b, b => b.Height * 0.28f, 0.22f, 0.07f);
            if (decor == SignDecor.Ivy)
            {
                layout.Add(IvyCluster("IvyBackLeft", root, flipped: false, back: true).rectTransform, b => GardenLook.IvyBox(b, left: true, ivyScale));
                layout.Add(IvyCluster("IvyBackRight", root, flipped: true, back: true).rectTransform, b => GardenLook.IvyBox(b, left: false, ivyScale));
            }

            // The plank in the buttons' plate laminate (spec 005 FR-045, the playtest's UiRaster.LaminateSign).
            Image plank = UiFactory.CreateImage("Plank", root, null, Color.white);
            plank.raycastTarget = false;
            PictureFit.On(plank, ProceduralSprites.LaminateSign);
            layout.Add(plank.rectTransform, b => b);
            // The win's flower sign titles a card: its letters in ink.title (the playtest's Kit.WoodSign).
            TextMeshProUGUI label = KitLabel("Label", root, text, style, GardenLook.SignLetters(letters ?? (decor == SignDecor.Flowers ? C.InkTitle : C.InkBrown)));
            view.Init(label, plank, style);
            layout.Watch(label).Then(b => KitText.Place(label, style, b.CenterX, b.CenterY - (b.Height * 0.04f), Mathf.Min(Units(style.Size), b.Height * 0.62f), SignLetterRoom(b, decor, ivyScale)));
            switch (decor)
            {
                case SignDecor.Ivy:
                    layout.Add(IvyCluster("IvyLeft", root, flipped: false, back: false).rectTransform, b => GardenLook.IvyBox(b, left: true, ivyScale));
                    layout.Add(IvyCluster("IvyRight", root, flipped: true, back: false).rectTransform, b => GardenLook.IvyBox(b, left: false, ivyScale));
                    break;
                case SignDecor.Flowers:
                    layout.Add(FlowerCluster("FlowersLeft", root, flipped: false).rectTransform, b => GardenLook.FlowerBox(b, left: true));
                    layout.Add(FlowerCluster("FlowersRight", root, flipped: true).rectTransform, b => GardenLook.FlowerBox(b, left: false));
                    break;
            }

            return view;
        }

        /// <summary>
        /// The width a wooden sign's letters may take: 82% of the plank, and clear of the owner's ivy clusters (pictures.md
        /// D5, 1.25 × the height over both ends, so the plank less 0.625 × the height each side) or of the flower clusters
        /// (0.68 × the height each side), as the reference's "Level 88" between its clover ends; ivy clusters at
        /// <paramref name="ivyScale"/> of their size leave that much more.
        /// </summary>
        public static float SignLetterRoom(Box b, SignDecor decor, float ivyScale = 1f)
        {
            float room = b.Width * 0.82f;
            if (decor == SignDecor.Ivy && OwnerArt.Decor(OwnerPictures.Ivy) != null)
            {
                return Mathf.Max(1f, Mathf.Min(room, b.Width - (b.Height * GardenLook.IvyShare * ivyScale)));
            }

            return decor == SignDecor.Flowers ? Mathf.Max(1f, Mathf.Min(room, b.Width - (b.Height * 1.35f * 0.95f))) : room;
        }

        /// <summary>
        /// A cluster of clover leaves (§3.9, <c>ui.sign.ivy</c>) as a square picture: soft pointed leaflets in yellow-green
        /// <c>ivy.leaf</c> shades over a soft <c>ivy.line</c> shadow, with thin outlines of their own darker shade, a light
        /// top-left side and faint midribs, mirrored when
        /// <paramref name="flipped"/>; <paramref name="back"/> keeps only the leaves behind a sign (true) or in front
        /// (false). Never a touch target.
        /// </summary>
        public static Image IvyCluster(string name, Transform parent, bool flipped, bool? back = null)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            Sprite? picture = OwnerArt.Decor(OwnerPictures.Ivy);
            if (picture != null)
            {
                // The owner's cluster (pictures.md D5) is one picture over the plank's end, mirrored for the right end.
                OwnerArt.Show(image, picture, mirror: flipped);
                image.enabled = back != true;
                return image;
            }

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
            Sprite? picture = OwnerArt.Decor(OwnerPictures.Flowers);
            if (picture != null)
            {
                // The owner's cluster (pictures.md D6), mirrored for the other end.
                OwnerArt.Show(image, picture, mirror: flipped);
                return image;
            }

            PictureFit.On(image, (w, h) => ProceduralSprites.FlowerCluster(flipped, Mathf.Min(w, h)), square: true);
            return image;
        }

        // ---- Jam choices and cost pills (§3.3, §3.4) ----

        /// <summary>A jam choice's corners, as a share of its height (the playtest's <c>Kit.ChoiceRadiusShare</c>).</summary>
        public const float ChoiceRadiusShare = 0.22f;

        /// <summary>
        /// A jam choice (§3.3): a glossy rounded rectangle in <paramref name="set"/> (green or blue, radius 22% of its
        /// height) raised on its wooden plate (<see cref="RaisedButton"/>, spec 005 FR-045) with the icon (44% of the height) in its upper half, the white outlined label (17% of the height) below
        /// it, and the cost pill centered on its bottom edge, overlapping by 40% of the pill's height. The rect holds the
        /// button and the pill below it and is the touch target. Without <paramref name="onClick"/> it is disabled: greyed,
        /// 55% alpha, the icon grey.
        /// </summary>
        public static ChoiceButtonView ChoiceButton(string name, Transform parent, ColorSet set, IReadOnlyList<IconPart>? icon, string label, Cost? cost, Action? onClick)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent, raycast: true);
            var view = root.gameObject.AddComponent<ChoiceButtonView>();
            CanvasGroup fade = root.gameObject.AddComponent<CanvasGroup>();
            // The glossy face raised on its wooden plate (spec 005 FR-045, the playtest's Kit.RaisedButton).
            GardenButton face = RaisedButton("Button", root, set, ChoiceRadiusShare, gloss: true, raycast: false);
            float buttonHeight = 1f;
            BoxLayout.On(face.Body).Then(b => buttonHeight = b.Height);

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
            pill.SetChargeIcon(icon);
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
        /// A cost pill (§3.4; jam choices, booster tiles, the purchase confirmation; the playtest's <c>Kit.CostPill</c>): since
        /// spec 005 FR-047 a cream pill raised like the rows (<see cref="RaisedRow"/>) holding the lotus and a brown price, or,
        /// as a <paramref name="button"/> (every price that buys: the Store's Shop and outfits, the Wardrobe's outfits, the
        /// avatars; FR-049), the glossy green face raised on its wooden plate holding the lotus and a white price; the green
        /// clapperboard and "Free" (a rewarded ad, spec 005 FR-051), or "×N" charges, centered as a group on its top; with <see cref="CostPillView.SetChargeIcon"/> the charges
        /// show in bigger digits after the booster's icon (80% of the pill's height).
        /// </summary>
        public static CostPillView CostPill(string name, Transform parent, Cost cost, bool button = false)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<CostPillView>();
            Func<Box, Box> top;
            if (button)
            {
                RaisedPlate(layout, root, b => b, 0.5f);
                Image face = UiFactory.CreateImage("Face", root, null, Color.white);
                face.raycastTarget = false;
                // Every price that buys (spec 005 FR-049): the glossy green face, as the Animations tab's Buy.
                PictureFit.On(face, (w, h) => ProceduralSprites.ButtonFace(GardenLook.Green, w, h, 0.5f, true));
                layout.Add(face.rectTransform, UiRaster.RaisedFaceBox);
                top = b =>
                {
                    Box f = UiRaster.RaisedFaceBox(b);
                    return new Box(f.Left, f.Top, f.Right, f.Bottom - (b.Height * UiRaster.FaceSide));
                };
            }
            else
            {
                RaisedRow(layout, root, 0.5f);
                top = b => new Box(b.Left, b.Top, b.Right, b.Bottom - (b.Height * RaisedRowSide));
            }

            Image lotus = PetalIcon("Lotus", root);

            // The rewarded choice: the clapperboard, every rewarded ad's mark (spec 005 FR-051), green over its line.
            RectTransform free = UiFactory.CreateRect("Free", root);
            BoxLayout freeLayout = BoxLayout.On(free);
            ColorSet green = GardenLook.Green;
            Image markLine = ShapeImage("AdLine", free, GardenLook.AdMark, green.Line);
            Image mark = ShapeImage("Ad", free, GardenLook.AdMark, green.Face);
            markLine.raycastTarget = false;
            mark.raycastTarget = false;
            freeLayout.Add(markLine.rectTransform, b => b.Offset(0f, b.Height * 0.06f));
            freeLayout.Add(mark.rectTransform, b => b);

            // Charges ("×2") with the booster's icon before them (the jam choices; hidden until SetChargeIcon).
            Image charge = UiFactory.CreateImage("Charge", root, null, Color.white);
            charge.preserveAspect = true;
            charge.gameObject.SetActive(false);

            TextMeshProUGUI label = KitLabel("Amount", root, string.Empty, T.Count, button ? TextLook.OnColor(GardenLook.Green) : TextLook.Plain(C.InkBrown));
            view.Init(label, lotus, free.gameObject, charge, layout);
            layout.Watch(label).Then(whole =>
            {
                Box b = top(whole);
                float h = b.Height;
                bool charges = view.ShowsChargeIcon;
                float icon = view.Cost.Kind == CostKind.Petals ? h * 0.86f : view.Cost.Kind == CostKind.Free ? h * GardenLook.AdMarkPillShare : charges ? h * 0.8f : 0f;
                float gap = icon > 0f ? h * 0.16f : 0f;

                // Charges get bigger digits, as large as the reference's prices look next to their icons.
                float size = h * (charges ? 0.66f : 0.56f);
                float measured = KitText.Measure(label, size);
                float room = Mathf.Max(1f, b.Width - icon - gap - (h * 0.5f));
                float textWidth = Mathf.Min(measured > 0f ? measured : room, room);
                float start = b.CenterX - ((icon + gap + textWidth) / 2f);
                Box iconBox = Box.FromCenter(start + (icon / 2f), b.CenterY, icon, icon);
                BoxLayout.Place(lotus.rectTransform, iconBox);
                BoxLayout.Place(free, iconBox);
                BoxLayout.Place(charge.rectTransform, iconBox);
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
        /// whose lengths alternate 1.0 and 0.8 cell (nearly rectangular, rounded 14%), square blocks rounded 30% at the
        /// corners and thin dark joints. The cell is
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
        /// (radius 18%, border 11% of the width), the variant's sticker tile at 62% of the panel near its top and the count
        /// below it in big plain digits. Queued pods are dimmed, a pressed one sinks and darkens a little (its tile sinking
        /// into its lip), a locked one shows the padlock; a null variant is a
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
        /// Places a pod's or slot's count under its tile (§3.7, the playtest's <c>Kit.CountBelow</c>): plain digits in
        /// <c>type.count</c> at <paramref name="fill"/> of the area's height (slots 0.86, pods 1.05: the digits' caps fill
        /// it), <c>ink.brown</c> or softer when <paramref name="dim"/>.
        /// </summary>
        public static void PlaceCount(TextMeshProUGUI label, Box area, bool dim, float fill = 0.86f)
        {
            Rgba ink = dim ? C.InkBrownSoft.Mix(C.ParchmentBottom, 0.3f) : C.InkBrown;
            ApplyLook(label, T.Count, TextLook.Plain(ink));
            KitText.Place(label, T.Count, area.CenterX, area.CenterY, area.Height * fill, area.Width);
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
        /// A booster tile (§3.7; the booster bar): a cream squircle (radius 26%) raised on its wooden plate (spec 005 FR-047)
        /// with the booster's colored icon, and the green count badge over its bottom-right corner, or the cost pill under
        /// it and a small green "+" raised on its plate when no charges are left. A selected tile is raised with the pulsing golden glow; a
        /// disabled one is greyed at 55% (spec 003 FR-031). Set it with <see cref="BoosterTileView.Show"/>.
        /// </summary>
        public static BoosterTileView BoosterTile(string name, Transform parent, string boosterId, Action? onPress)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<BoosterTileView>();
            view.Build(layout, boosterId, onPress);
            return view;
        }

        /// <summary>
        /// A booster's colored icon (§3.8, <see cref="GardenLook.BoosterIcon"/>), all grey when <paramref name="grey"/>, or
        /// the owner's icon picture (pictures.md D1–D4) when it exists (<see cref="SetIconParts"/>).
        /// </summary>
        public static Image BoosterIcon(string name, Transform parent, string boosterId, bool grey = false) =>
            IconParts(name, parent, GardenLook.BoosterIcon(boosterId), grey);

        /// <summary>Changes a <see cref="BoosterIcon"/> image's booster or greyness.</summary>
        public static void SetBoosterIcon(Image image, string boosterId, bool grey) =>
            SetIconParts(image, GardenLook.BoosterIcon(boosterId), grey);

        /// <summary>
        /// Changes an <see cref="IconParts"/> image's parts or greyness. A booster's icon is the owner's picture when it
        /// exists (pictures.md D1–D4), and so is the lotus (<see cref="GardenLook.Lotus"/>, <see cref="OwnerPictures.CurrencyLotus"/>),
        /// faded to <see cref="GardenLook.PictureDisabledAlpha"/> when grey.
        /// </summary>
        public static void SetIconParts(Image image, IReadOnlyList<IconPart> parts, bool grey)
        {
            string? booster = GardenLook.BoosterOf(parts);
            Sprite? picture = booster != null ? OwnerArt.Icon(OwnerPictures.BoosterIcon(booster))
                : ReferenceEquals(parts, GardenLook.Lotus) ? OwnerArt.Icon(OwnerPictures.CurrencyLotus) : null;
            if (picture != null)
            {
                OwnerArt.Show(image, picture);
                image.color = new Color(1f, 1f, 1f, grey ? GardenLook.PictureDisabledAlpha : 1f);
                return;
            }

            image.color = Color.white;
            PictureFit.On(image, (w, h) => ProceduralSprites.IconParts(parts, grey, Mathf.Min(w, h)), square: true);
        }

        // ---- Celebration (§3.9) ----

        /// <summary>The largest side in pixels of the light rays' picture (<see cref="LightRays"/>).</summary>
        public const int LightRaysMaxPixels = 256;

        /// <summary>
        /// Light rays behind the celebrating heroes (§3.9, <c>fx.rays</c>): ten soft <c>ray.light</c> wedges from the
        /// rect's center to its edge (place a square of twice the rays' radius), turning 0.05 turn per second on unscaled
        /// time. Never a touch target. The picture is at most <see cref="LightRaysMaxPixels"/> wide and the image stretches it:
        /// the soft wedges keep their look under bilinear filtering, at a sixteenth of a screen-sized render's cost.
        /// </summary>
        public static LightRaysView LightRays(string name, Transform parent)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            PictureFit.On(image, (w, h) => ProceduralSprites.LightRays(Mathf.Min(LightRaysMaxPixels, Mathf.Min(w, h))), square: true);
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

        /// <summary>
        /// A light sprinkle of confetti for the first seconds of a win or a milestone (§3.9, <c>fx.confetti</c>; the
        /// playtest's <c>EndCards.Confetti</c>): eighteen small rounded bits in the level's colors
        /// (<see cref="ConfettiView.ColorsOf"/>) falling through the rect and fading out by 2.2 s, on unscaled time since the
        /// view was enabled. Put it in the heroes' room above the card (a clip from the screen's top to the card's top edge),
        /// so the picture, the reward and Next stay clean. Never a touch target.
        /// </summary>
        public static ConfettiView Confetti(string name, Transform parent)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect(name, parent));
            var view = root.gameObject.AddComponent<ConfettiView>();
            view.Build();
            return view;
        }

        // ---- The wordmark (§4.5) ----

        /// <summary>
        /// The wooden wordmark (§4.5, <c>ui.logo.wood</c>; the stand-in for the owner's logo; the playtest's
        /// <c>Kit.WoodLogo</c>): <paramref name="text"/> in <c>type.wordmark</c> fitted into the rect (80% of its width, 72%
        /// of its height) with light wood letters, a <c>wood.line</c> outline and a darker extrusion
        /// (<see cref="GardenLook.WoodLetters"/>) inside an olive mossy band, as the reference's logo, broad leaves behind
        /// both ends and two small pink flowers over them. Never a touch target. <see cref="OwnerArt.Logo"/> shows the
        /// owner's picture instead when it exists.
        /// </summary>
        public static RectTransform WoodLogo(string name, Transform parent, string text)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            TypeStyle style = T.Wordmark;
            Image leftLeaves = UiFactory.CreateImage("LeavesLeft", root, null, Color.white);
            Image rightLeaves = UiFactory.CreateImage("LeavesRight", root, null, Color.white);
            Sprite? leaves = OwnerArt.Decor(OwnerPictures.LogoLeaves);
            if (leaves != null)
            {
                // The owner's leaves (pictures.md D8), mirrored for the right end.
                OwnerArt.Show(leftLeaves, leaves);
                OwnerArt.Show(rightLeaves, leaves, mirror: true);
            }
            else
            {
                PictureFit.On(leftLeaves, (w, h) => ProceduralSprites.LogoLeaves(false, Mathf.Min(w, h)), square: true);
                PictureFit.On(rightLeaves, (w, h) => ProceduralSprites.LogoLeaves(true, Mathf.Min(w, h)), square: true);
            }

            // The mossy band: the same letters in olive, outlined and extruded thickly, behind the wooden ones.
            Rgba moss = C.IvyLine.Mix(C.WoodLine, 0.35f).Lighten(0.12f);
            TextMeshProUGUI band = KitLabel("Moss", root, text, style, new TextLook(moss, moss, moss, 0.14f, 0.14f, 0.36f));
            TextMeshProUGUI label = KitLabel("Letters", root, text, style, GardenLook.WoodLetters);
            Image flowerLeft = UiFactory.CreateImage("FlowerLeft", root, null, Color.white);
            PictureFit.On(flowerLeft, (w, h) => ProceduralSprites.LogoFlower(Mathf.Min(w, h)), square: true);
            Image flowerRight = UiFactory.CreateImage("FlowerRight", root, null, Color.white);
            PictureFit.On(flowerRight, (w, h) => ProceduralSprites.LogoFlower(Mathf.Min(w, h)), square: true);
            layout.Watch(label).Then(b =>
            {
                float size = Units(style.Size);
                float natural = KitText.Measure(label, size);
                if (natural <= 0f)
                {
                    natural = b.Width * 0.8f;
                }

                float scale = Mathf.Min(b.Width * 0.8f / natural, b.Height * 0.72f / size);
                float width = natural * scale;
                float em = size * scale;
                float cy = b.CenterY - (em * 0.04f);
                float left = b.CenterX - (width / 2f);
                float right = b.CenterX + (width / 2f);
                float leaf = em * 1.7f;
                BoxLayout.Place(leftLeaves.rectTransform, Box.FromCenter(left + (em * 0.02f), cy - (em * 0.04f), leaf, leaf));
                BoxLayout.Place(rightLeaves.rectTransform, Box.FromCenter(right - (em * 0.02f), cy - (em * 0.14f), leaf, leaf));
                KitText.Place(band, style, b.CenterX, cy, em, width + 1f);
                KitText.Place(label, style, b.CenterX, cy, em, width + 1f);
                BoxLayout.Place(flowerLeft.rectTransform, Box.FromCenter(left - (em * 0.12f), cy + (em * 0.42f), em * 0.36f, em * 0.36f));
                BoxLayout.Place(flowerRight.rectTransform, Box.FromCenter(right + (em * 0.08f), cy - (em * 0.5f), em * 0.4f, em * 0.4f));
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
        /// An outfit card (§4.6, <c>ui.card.outfit</c>; the Wardrobe and the Store's cosmetics; the playtest's
        /// <c>Kit.OutfitCard</c>): a cream card raised like the rows (spec 005 FR-047, radius 11% of its width) with a beige
        /// picture well (64% of the face tall, which <see cref="OutfitCardView.Picture"/>
        /// fills and clips: put the hero wearing the item there) and the item's name below it. The worn one
        /// (<see cref="OutfitCardView.Show"/>) has a green-tinted well with a green border and the check badge on its corner.
        /// A <paramref name="cost"/> adds the cost pill on the card's bottom edge (the Store); the rect then holds the card
        /// and the pill below it (<paramref name="pillRoom"/> keeps that room without a pill, so cards in a row line up).
        /// With <paramref name="onClick"/> the whole rect is the touch target; without one a cost pill is faded (it cannot
        /// be bought now).
        /// </summary>
        public static OutfitCardView OutfitCard(string name, Transform parent, string label, Action? onClick, Cost? cost = null, bool pillRoom = false)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent, raycast: onClick != null);
            var view = root.gameObject.AddComponent<OutfitCardView>();
            view.Build(layout, label, cost, pillRoom || cost.HasValue, faded: onClick == null);
            if (onClick != null)
            {
                var button = root.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = root.GetComponent<Image>();
                button.onClick.AddListener(() =>
                {
                    GameFeedback.Current?.Play(SoundCue.Click);
                    onClick();
                });
                root.gameObject.AddComponent<PressMotion>().Tile = true;
            }

            return view;
        }

        /// <summary>
        /// The worn item's badge (§4.6, the playtest's <c>Kit.CheckBadge</c>): a glossy green ball in a white ring (10% of the
        /// disc; spec 005 FR-048) and a white check, over a soft shadow, in the box <paramref name="disc"/> gives for the
        /// element's box. Its images go into <paramref name="layout"/>'s element.
        /// </summary>
        internal static void CheckBadge(BoxLayout layout, Func<Box, Box> disc)
        {
            Transform root = layout.transform;
            ColorSet green = GardenLook.Green;
            Box Outer(Box b)
            {
                Box d = disc(b);
                return d.Inset(-d.Width * 0.1f);
            }

            SoftShadow(layout, Outer, b => b.Width / 2f, 0.25f, 0.06f);
            Image ring = RoundRect("CheckRing", root, Color.white);
            // A glossy green ball (spec 005 FR-048, the playtest's Kit.CheckBadge).
            Image face = UiFactory.CreateImage("CheckBall", root, null, Color.white);
            face.raycastTarget = false;
            PictureFit.On(face, (w, h) => ProceduralSprites.Ball(green, Mathf.Min(w, h)), square: true);
            Image check = ShapeImage("Check", root, "ui.check", Rgba.White);
            layout.Add(ring.rectTransform, Outer);
            layout.Add(face.rectTransform, disc);
            layout.Add(check.rectTransform, b =>
            {
                Box d = disc(b);
                return Box.FromCenter(d.CenterX, d.CenterY, d.Width * 0.58f, d.Width * 0.58f);
            });
        }

        private static float Frac(float v) => v - (float)Math.Floor(v);

        internal static float Fraction(float v) => Frac(v);
    }

    /// <summary>
    /// A candy tile built by <see cref="UiKit.CandyTile(string, Transform, VariantId?, TileStyle, TileState)"/>. With the
    /// owner's icon picture of its variant (spec 005 pictures.md G9–G24, <see cref="OwnerArt.TileIcon"/>) the tile picture is
    /// the drawn face alone (<see cref="ProceduralSprites.CandyFace"/>) and the picture lies over its middle
    /// (<see cref="OwnerPictures.TileIconBox"/>): faded on a queued pod, its grey copy on a stuck slot, sinking with the
    /// face when pressed; the playtest's <c>Kit.CandyTile</c>. Without it the drawn symbol stays.
    /// </summary>
    public sealed class CandyTileView : MonoBehaviour
    {
        private BoxLayout _layout = null!;
        private TileStyle _style;
        private bool _pressed;
        private Image? _icon;

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
            if (!variant.HasValue || !VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info))
            {
                Show(DesignTokens.Colors.TileMystery, "mystery", TileState.Mystery);
                return;
            }

            Show(Rgba.FromHex(info.ColorHex), info.IconId, state);
        }

        /// <summary>Shows any color with a variant icon in a state.</summary>
        public void Show(Rgba color, string iconId, TileState state = TileState.Normal)
        {
            TileStyle style = _style;
            Sprite? picture = OwnerArt.TileIcon(iconId, style, state);
            if (picture == null)
            {
                PictureFit.On(Image, (w, h) => ProceduralSprites.CandyTile(color, iconId, style, state, w), false, PictureShape.SquareByWidth);
                if (_icon != null)
                {
                    _icon.enabled = false;
                }

                return;
            }

            PictureFit.On(Image, (w, h) => ProceduralSprites.CandyFace(color, style, state, w), false, PictureShape.SquareByWidth);
            if (_icon == null)
            {
                // Over the tile picture (a later sibling), placed on the face's middle.
                _icon = UiFactory.CreateImage("Icon", transform, picture, Color.white);
                _icon.raycastTarget = false;
                _icon.preserveAspect = true;
                _layout.Add(_icon.rectTransform, b => OwnerPictures.TileIconBox(TileBox(b), _style));
            }

            _icon.sprite = picture;
            _icon.color = new Color(1f, 1f, 1f, OwnerPictures.TileIconAlpha(state));
            _icon.enabled = true;
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

        /// <summary>The letters' color (<c>ink.brown</c>; <c>badge.super_hard</c> darkened on a Super Hard level), embossed.</summary>
        public void SetLetters(Rgba ink) => UiKit.ApplyLook(Label, _style, GardenLook.SignLetters(ink));
    }

    /// <summary>A cost pill built by <see cref="UiKit.CostPill"/>.</summary>
    public sealed class CostPillView : MonoBehaviour
    {
        private TextMeshProUGUI _label = null!;
        private Image _lotus = null!;
        private GameObject _free = null!;
        private Image _charge = null!;
        private IReadOnlyList<IconPart>? _chargeParts;
        private BoxLayout _layout = null!;

        /// <summary>What the pill shows.</summary>
        public Cost Cost { get; private set; }

        /// <summary>Whether the pill shows charges after the booster's icon (<see cref="SetChargeIcon"/>).</summary>
        public bool ShowsChargeIcon => Cost.Kind == CostKind.Charges && _chargeParts != null;

        internal void Init(TextMeshProUGUI label, Image lotus, GameObject free, Image charge, BoxLayout layout)
        {
            _label = label;
            _lotus = lotus;
            _free = free;
            _charge = charge;
            _layout = layout;
        }

        /// <summary>Shows a price with the lotus, Free with the green clapperboard, or ×N charges (after the booster's icon when set).</summary>
        public void SetCost(Cost cost)
        {
            Cost = cost;
            _label.text = Text(cost);
            _lotus.gameObject.SetActive(cost.Kind == CostKind.Petals);
            _free.SetActive(cost.Kind == CostKind.Free);
            _charge.gameObject.SetActive(ShowsChargeIcon);
            _layout.Apply();
        }

        /// <summary>
        /// The booster's icon shown before charges (the playtest's <c>Kit.CostPill</c> with its charge icon: the jam
        /// choices); null shows charges alone.
        /// </summary>
        public void SetChargeIcon(IReadOnlyList<IconPart>? parts)
        {
            _chargeParts = parts;
            if (parts != null)
            {
                UiKit.SetIconParts(_charge, parts, false);
            }

            SetCost(Cost);
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
        private Image _shade = null!;
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

        /// <summary>The padlock of a locked pod (it grows as the key lands).</summary>
        public Image Lock => _lock;

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
            _shade = UiKit.RoundRect("PressShade", root, UiTheme.Of(C.GardenShadow.WithAlpha(0.08f)), _ => _radius);
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
                _tile.Pressed = look == PodLook.Pressed;
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

            // Pressed: the whole pod is a little darker as it sinks.
            _shade.gameObject.SetActive(look == PodLook.Pressed);
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
            BoxLayout.Place(_shadow.rectTransform, frame.Offset(0f, w * (pressed ? 0.01f : 0.05f)).Inset(w * 0.03f, 0f));
            float handleHeight = w * 0.15f;
            Box knob = Box.FromCenter(frame.CenterX, frame.Top - (handleHeight * 0.12f), w * 0.34f, handleHeight);
            BoxLayout.Place(_stem.rectTransform, Box.FromCenter(knob.CenterX, knob.Top - (handleHeight * 0.12f), w * 0.04f, handleHeight * 0.45f));
            BoxLayout.Place(_knob.rectTransform, knob);
            BoxLayout.Place(_panel.rectTransform, frame.Inset(border * 0.8f));
            BoxLayout.Place(_frame.rectTransform, frame);
            BoxLayout.Place(_veil.rectTransform, frame);
            BoxLayout.Place(_shade.rectTransform, frame);
            Box inner = frame.Inset(border);
            BoxLayout.Place(_content, inner);

            // The sticker tile at 62% of the panel near its top; the count below it in big digits (about 17% of the pod).
            float tile = inner.Width * 0.62f;
            Box tileBox = Box.FromCenter(inner.CenterX, inner.Top + (inner.Height * 0.03f) + (tile / 2f), tile, tile);
            BoxLayout.Place((RectTransform)_tile.transform, tileBox);
            BoxLayout.Place(_lock.rectTransform, Box.FromCenter(tileBox.CenterX, tileBox.CenterY, tile * 0.62f, tile * 0.62f));
            UiKit.PlaceCount(_count, new Box(inner.Left, tileBox.Bottom, inner.Right, inner.Bottom), Look == PodLook.Next || Look == PodLook.Locked, 1.05f);
            foreach (Image image in new[] { _shadow, _panel, _veil, _shade })
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
        private Image _emptyLip = null!;
        private Image _emptyFill = null!;
        private Image _emptyShade = null!;
        private Image _emptyRing = null!;
        private Image _emptyLine = null!;
        private Image _dashLight = null!;
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
        private float _lineWidth;
        private float _ringWidth;

        /// <summary>The slot's state.</summary>
        public SlotPlateState State { get; private set; }

        /// <summary>Whether the slot carries the Extra Slot's green "+".</summary>
        public bool Extra { get; private set; }

        /// <summary>The count shown under the tile.</summary>
        public int CountValue { get; private set; }

        /// <summary>The variant tile.</summary>
        public CandyTileView Tile => _tile;

        /// <summary>The count under the tile (it bumps as Bloomlings land).</summary>
        public TextMeshProUGUI Count => _count;

        /// <summary>The padlock of a locked slot (it grows as the key lands).</summary>
        public Image Lock => _lock;

        internal void Build(BoxLayout layout)
        {
            _layout = layout;
            Transform root = layout.transform;
            _empty = UiFactory.CreateRect("Empty", root).gameObject;
            UiFactory.Stretch((RectTransform)_empty.transform);

            // A plate pressed into the parchment (the playtest's Kit.SlotPlate): a thin lower edge, the face a little sunk,
            // a light ring inside the outline, and the dashed inner outline stitched in with a light line under each dash.
            _emptyLip = UiKit.RoundRect("Lip", _empty.transform, UiTheme.Of(C.CreamLip.WithAlpha(0.55f)), _ => _radius);
            _emptyFill = UiKit.RoundRect("Fill", _empty.transform, UiTheme.Of(C.CreamFace.Mix(C.ParchmentWell, 0.35f)), _ => _radius);
            _emptyShade = UiKit.RoundRect("Shade", _empty.transform, Color.white, _ => _radius);
            UiKit.Gradient(_emptyShade, UiTheme.Of(C.GardenShadow.WithAlpha(0.1f)), UiTheme.Of(C.GardenShadow.WithAlpha(0f)));
            _emptyShade.GetComponent<VerticalGradient>().Stop = 0.3f;
            _emptyRing = UiKit.RoundRing("Ring", _empty.transform, UiTheme.Of(C.CreamTop.WithAlpha(0.75f)), _ => Mathf.Max(0f, _radius - _lineWidth), _ => _ringWidth);
            _emptyLine = UiKit.RoundRing("Line", _empty.transform, UiTheme.Of(C.CreamLine.WithAlpha(0.6f)), _ => _radius, _ => _lineWidth);
            _dangerFill = UiKit.RoundRect("DangerFill", _empty.transform, UiTheme.Of(C.StateDanger.WithAlpha(0.07f)), _ => _radius * 0.7f);
            _dashLight = UiFactory.CreateImage("DashedLight", _empty.transform, null, UiTheme.Of(C.CreamTop.WithAlpha(0.8f)));
            PictureFit.On(_dashLight, (w, h) => ProceduralSprites.DashedOutline(w, h, 0.085f, 0.14f, 0.03f, 0.09f, 0.06f));
            _dashed = UiFactory.CreateImage("Dashed", _empty.transform, null, Color.white);
            PictureFit.On(_dashed, (w, h) => ProceduralSprites.DashedOutline(w, h, 0.085f, 0.14f, 0.03f, 0.09f, 0.06f));
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
            float px = 1f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            _radius = s * 0.2f;
            _lineWidth = Mathf.Max(px, s * 0.018f);
            _ringWidth = Mathf.Max(px, s * 0.022f);
            BoxLayout.Place(_emptyLip.rectTransform, box.Offset(0f, s * 0.025f));
            BoxLayout.Place(_emptyFill.rectTransform, box);
            BoxLayout.Place(_emptyShade.rectTransform, box);
            BoxLayout.Place(_emptyRing.rectTransform, box.Inset(_lineWidth));
            BoxLayout.Place(_emptyLine.rectTransform, box);
            Box dashed = box.Inset(s * 0.085f);
            BoxLayout.Place(_dangerFill.rectTransform, dashed);
            BoxLayout.Place(_dashLight.rectTransform, box.Offset(0f, Mathf.Max(1.5f * px, s * 0.03f) * 0.45f));
            BoxLayout.Place(_dashed.rectTransform, box);
            BoxLayout.Place(_dangerMark.rectTransform, box.Inset(s * 0.32f));

            // The filled plate: a lip of 5.5% of the shorter side; the tile 8% of the face below its top (74% of a
            // portrait plate's width, at most 66% of the face's height, as the reference's), so the count below it keeps
            // about a quarter of the face.
            float lip = s * 0.055f;
            var face = new Box(box.Left, box.Top, box.Right, box.Bottom - lip);
            BoxLayout.Place(_lip.rectTransform, box);
            BoxLayout.Place(_face.rectTransform, face);
            BoxLayout.Place(_line.rectTransform, box);
            BoxLayout.Place(_lock.rectTransform, Box.FromCenter(face.CenterX, face.CenterY, s * 0.44f, s * 0.44f));
            float tile = Mathf.Min(face.Width * 0.74f, face.Height * 0.66f);
            Box tileBox = Box.FromCenter(face.CenterX, face.Top + (face.Height * 0.08f) + (tile / 2f), tile, tile);
            BoxLayout.Place((RectTransform)_tile.transform, tileBox);
            UiKit.PlaceCount(_count, new Box(face.Left, tileBox.Bottom, face.Right, face.Bottom - (face.Height * 0.03f)), State == SlotPlateState.Stuck);

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
            foreach (Image image in new[] { _emptyLip, _emptyFill, _emptyShade, _emptyRing, _emptyLine, _dangerFill, _line, _lip, _face })
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
        private GardenButton _plus = null!;
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

            // The cream face raised on its wooden plate (the playtest's Kit.BoosterBezel, UiKitGameplay.cs; spec 005 FR-047).
            Face = UiKit.BoosterBezel("Tile", root, 0.26f, raycast: true);
            Face.TileSquash = true;
            _icon = UiKit.BoosterIcon("Icon", Face.Content, boosterId);
            // The icon's box: its shapes keep a margin, so the icon itself is about two thirds of the tile (the playtest's
            // 0.74 of the tile; IconSide is the face's share of it).
            float IconBox() => Face.IconSide * 0.74f / GardenLook.IconRimFaceShare;
            BoxLayout.On(Face.Content).Add(_icon.rectTransform, f => Box.FromCenter(f.CenterX, f.CenterY, IconBox(), IconBox()));

            _badge = UiKit.CountBadge("Count", root, out _badgeDisc);
            _cost = UiKit.CostPill("Cost", root, Cost.Petals(0));
            // The small green "+" raised on its plate (spec 005 FR-047).
            _plus = UiKit.RaisedButton("Plus", root, GardenLook.Green, 0.5f, gloss: true, raycast: false, square: true);
            Image plusGlyph = UiKit.ShapeImage("Glyph", _plus.Content, "ui.plus", Rgba.White);
            BoxLayout.On(_plus.Content).Add(plusGlyph.rectTransform, f => Box.FromCenter(f.CenterX, f.CenterY, f.Width * 0.86f, f.Width * 0.86f));

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
            _plus.gameObject.SetActive(!state.ShowsCharges);
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
            // Over the tile's lower right corner, mostly on the tile, as in the reference (the playtest's Kit.BoosterTile).
            BoxLayout.Place(_badgeDisc.rectTransform, Box.FromCenter(tile.Right - (badge * 0.55f), tile.Bottom - (badge * 0.55f), badge * 1.26f, badge * 1.26f));
            float pill = s * 0.3f;
            BoxLayout.Place((RectTransform)_cost.transform, Box.FromCenter(tile.CenterX, tile.Bottom + (pill * 0.12f), s * 0.86f, pill));
            float plus = s * 0.34f;
            BoxLayout.Place((RectTransform)_plus.transform, Box.FromCenter(tile.Right - (plus * 0.3f), tile.Top + (plus * 0.3f), plus, plus));
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
            _joints = UiKit.RoundRect("Joints", transform, UiTheme.Of(C.StoneLine.WithAlpha(0.45f)), _ => _jointRadius);
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
            blocks.Add((new Box(outer.Left, outer.Top, inner.Left, inner.Top).Inset(half), 1, 0.3f));
            blocks.Add((new Box(inner.Right, outer.Top, outer.Right, inner.Top).Inset(half), 2, 0.3f));
            blocks.Add((new Box(outer.Left, inner.Bottom, inner.Left, outer.Bottom).Inset(half), 3, 0.3f));
            blocks.Add((new Box(inner.Right, inner.Bottom, outer.Right, outer.Bottom).Inset(half), 4, 0.3f));
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
                blocks.Add((place(at, next).Inset(half), seed, 0.14f));
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

    /// <summary>The win's confetti (<see cref="UiKit.Confetti"/>): the same time gives the same frame.</summary>
    public sealed class ConfettiView : MonoBehaviour
    {
        /// <summary>How many bits fall.</summary>
        public const int Count = 18;

        /// <summary>How long the confetti shows, in seconds.</summary>
        public const float Seconds = 2.2f;

        private readonly Image[] _bits = new Image[Count];
        private float _startedAt;

        internal void Build()
        {
            for (int i = 0; i < Count; i++)
            {
                _bits[i] = UiKit.RoundRect("Bit" + i, transform, Color.white, _ => UiKit.Units(3f));
            }
        }

        /// <summary>The bits' colors, in turn (<see cref="ColorsOf"/>).</summary>
        public void SetColors(IReadOnlyList<Rgba> colors)
        {
            for (int i = 0; i < Count; i++)
            {
                _bits[i].color = UiTheme.Of(colors.Count > 0 ? colors[i % colors.Count] : C.LotusFill);
            }
        }

        /// <summary>The confetti's colors for a level: each variant of its pods once, in pod order, then the lotus pinks.</summary>
        public static List<Rgba> ColorsOf(IEnumerable<PodDef> pods)
        {
            var colors = new List<Rgba>();
            foreach (PodDef pod in pods)
            {
                Rgba color = VariantCatalog.Default.TryGet(pod.Variant, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : C.StateStuck;
                if (!colors.Contains(color))
                {
                    colors.Add(color);
                }
            }

            colors.Add(C.LotusFill);
            colors.Add(C.PetalCenter);
            return colors;
        }

        /// <summary>
        /// Bit <paramref name="i"/>'s box (top-down, in an area <paramref name="width"/> × <paramref name="height"/> from the
        /// screen's top to the card's) and alpha at <paramref name="seconds"/> after the card showed (the playtest's recipe).
        /// </summary>
        public static (Box Box, float Alpha) Bit(int i, float width, float height, float seconds, float unit)
        {
            float seed = (i * 0.6180339f) % 1f;
            float x = width * ((seed + (0.05f * (float)Math.Sin((seconds * 2f) + i))) % 1f);
            float y = (-40f * unit) + ((seconds * (height * (0.35f + (0.25f * ((i * 0.37f) % 1f))))) % (height + (80f * unit)));
            float size = unit * (14f + (8f * ((i * 0.53f) % 1f)));
            return (Box.FromCenter(x, y, size, size * 0.6f), Math.Max(0f, Math.Min(1f, Seconds - seconds)));
        }

        private void OnEnable() => _startedAt = Time.unscaledTime;

        private void Update()
        {
            float seconds = Time.unscaledTime - _startedAt;
            Rect rect = ((RectTransform)transform).rect;
            bool show = seconds <= Seconds && rect.width > 0f && rect.height > 0f;
            float unit = UiKit.Units(1f);
            for (int i = 0; i < Count; i++)
            {
                Image bit = _bits[i];
                bit.gameObject.SetActive(show);
                if (!show)
                {
                    continue;
                }

                (Box box, float alpha) = Bit(i, rect.width, rect.height, seconds, unit);
                BoxLayout.Place(bit.rectTransform, box);
                Color color = bit.color;
                bit.color = new Color(color.r, color.g, color.b, alpha);
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

    /// <summary>An outfit card built by <see cref="UiKit.OutfitCard"/> (the playtest's <c>Kit.OutfitCard</c>).</summary>
    public sealed class OutfitCardView : MonoBehaviour
    {
        /// <summary>The cost pill's height over the card's (§4.6).</summary>
        public const float PillShare = 0.2f;

        /// <summary>The card's corners in its rim, as a share of its shorter side (the playtest's <c>Kit.OutfitRadiusShare</c>).</summary>
        public const float RadiusShare = 0.11f;

        private BoxLayout _layout = null!;
        private Image _well = null!;
        private Image _wellLine = null!;
        private GameObject _check = null!;
        private TextMeshProUGUI _label = null!;
        private bool _pillRoom;
        private float _radius;
        private float _wellRadius;
        private float _line;
        private float _border;

        /// <summary>Where the outfit's picture goes: the whole well, clipped to its rounded shape.</summary>
        public RectTransform Picture { get; private set; } = null!;

        /// <summary>The outfit's name.</summary>
        public TextMeshProUGUI Label => _label;

        /// <summary>The cost pill under the card (null without a cost).</summary>
        public CostPillView? CostPill { get; private set; }

        /// <summary>Whether the outfit is worn (the green well, its border and the check).</summary>
        public bool Worn { get; private set; }

        /// <summary>The card's body in the rect: above the pill's lower part when the rect keeps the pill's room.</summary>
        public static Box CardBox(Box box, bool pillRoom) =>
            pillRoom ? new Box(box.Left, box.Top, box.Right, box.Top + (box.Height / (1f + (0.6f * PillShare)))) : box;

        /// <summary>The well in the rect: inset 5.5% of the card's width, 64% of the face tall.</summary>
        public static Box WellBox(Box box, bool pillRoom)
        {
            Box face = FaceBox(box, pillRoom);
            float pad = CardBox(box, pillRoom).Width * 0.055f;
            return new Box(face.Left + pad, face.Top + pad, face.Right - pad, face.Top + pad + (face.Height * 0.64f));
        }

        /// <summary>
        /// Places a hero picture rect in an outfit card's well as the playtest's previews do: filling the well with its
        /// feet near the bottom (clipped by the well), or with a hat a little smaller and lower, so the hat stays inside.
        /// </summary>
        public static void PlaceHero(RectTransform hero, RectTransform well, bool hat) =>
            BoxLayout.On(well).Add(hero, w => HomeStage.Figure(w.CenterX, w.Bottom - (w.Height * (hat ? 0.02f : 0.05f)), w.Height * (hat ? 0.96f : 1.1f)));

        private static Box FaceBox(Box box, bool pillRoom)
        {
            Box card = CardBox(box, pillRoom);
            return CardLook.TileTop(card);
        }

        internal void Build(BoxLayout layout, string label, Cost? cost, bool pillRoom, bool faded)
        {
            _layout = layout;
            _pillRoom = pillRoom;
            Transform root = layout.transform;
            Box Card(Box b) => CardBox(b, _pillRoom);
            // The card in a thin wooden rim (spec 005 FR-047, the playtest's Kit.FramedTile), its corners 11% of its shorter side.
            UiKit.FramedTile(layout, root, RadiusShare, Card);
            _well = UiKit.RoundGradient("Well", root, C.ParchmentWell.Mix(C.CreamTop, 0.35f), C.ParchmentWell, _ => _wellRadius);
            _well.gameObject.AddComponent<Mask>();
            Image shade = UiKit.RoundRect("Shade", _well.transform, Color.white, _ => _wellRadius);
            UiKit.Gradient(shade, UiTheme.Of(C.GardenShadow.WithAlpha(0.1f)), UiTheme.Of(C.GardenShadow.WithAlpha(0f)));
            shade.GetComponent<VerticalGradient>().Stop = 0.2f;
            UiFactory.Stretch(shade.rectTransform);
            Picture = UiFactory.Stretch(UiFactory.CreateRect("Picture", _well.transform));
            _wellLine = UiKit.RoundRing("WellLine", root, UiTheme.Of(C.ParchmentEdge.Darken(0.08f)), _ => _wellRadius, _ => _border);
            _label = UiKit.KitLabel("Name", root, label, T.ButtonSecondary, TextLook.Plain(C.InkBrown));

            RectTransform check = UiFactory.Stretch(UiFactory.CreateRect("Check", root));
            _check = check.gameObject;
            UiKit.CheckBadge(BoxLayout.On(check), b =>
            {
                Box w = WellBox(b, _pillRoom);
                float badge = CardBox(b, _pillRoom).Width * 0.22f;
                return Box.FromCenter(w.Right - (badge * 0.42f), w.Bottom - (badge * 0.42f), badge, badge);
            });

            layout.Then(b =>
            {
                Box card = Card(b);
                _radius = card.Width * 0.11f;
                _line = Mathf.Max(UiKit.Units(1f), card.Width * 0.011f);
                _wellRadius = _radius * 0.7f;
                _border = Worn ? Mathf.Max(UiKit.Units(4f), card.Width * 0.022f) : _line;
            });
            layout.Add(_well.rectTransform, b => WellBox(b, _pillRoom));
            layout.Add(_wellLine.rectTransform, b => WellBox(b, _pillRoom));
            if (cost.HasValue)
            {
                CostPill = UiKit.CostPill("Cost", root, cost.Value, button: true);
                if (faded)
                {
                    CostPill.gameObject.AddComponent<CanvasGroup>().alpha = 0.45f;
                }

                layout.Add((RectTransform)CostPill.transform, b =>
                {
                    Box card = Card(b);
                    float h = card.Height * PillShare;
                    return Box.FromCenter(card.CenterX, card.Bottom + (h * 0.1f), card.Width * 0.78f, h);
                });
            }

            layout.Watch(_label).Then(b =>
            {
                Box card = Card(b);
                float top = WellBox(b, _pillRoom).Bottom;
                float bottom = _pillRoom ? FaceBox(b, _pillRoom).Bottom - ((b.Height - card.Height) * 0.5f) : FaceBox(b, _pillRoom).Bottom;
                float size = Mathf.Min(UiKit.Units(T.ButtonSecondary.Size), (bottom - top) * 0.62f);
                KitText.Place(_label, T.ButtonSecondary, card.CenterX, (top + bottom) / 2f, size, card.Width * 0.88f);
                foreach (Image image in new[] { _well, shade, _wellLine })
                {
                    image.GetComponent<RoundShape>().Apply();
                }
            });
            Show(false);
        }

        /// <summary>Shows the card worn (the green-tinted well with its green border and the check) or not.</summary>
        public void Show(bool worn)
        {
            Worn = worn;
            ColorSet green = GardenLook.Green;
            if (worn)
            {
                UiKit.Gradient(_well, UiTheme.Of(green.Top.Mix(C.CreamTop, 0.55f)), UiTheme.Of(green.Face.Mix(C.CreamTop, 0.5f)));
            }
            else
            {
                UiKit.Gradient(_well, UiTheme.Of(C.ParchmentWell.Mix(C.CreamTop, 0.35f)), UiTheme.Of(C.ParchmentWell));
            }

            _wellLine.color = UiTheme.Of(worn ? green.Face : C.ParchmentEdge.Darken(0.08f));
            _check.SetActive(worn);
            _layout.Apply();
        }
    }
}
