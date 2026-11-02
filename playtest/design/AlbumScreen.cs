using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Album;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The Scene Album page (spec 007-scene-album; the owner's UX and mockup of 2026-11-02; Unity's <c>AlbumScreen</c>), a
    /// place of the bottom menu in the Collection's place:
    /// <list type="bullet">
    /// <item><description>the pages' header (back, the "Scene Album" sign, the Petals pill) and the scene's name on the name plate;</description></item>
    /// <item><description>one big vertical card, the owner's layered scene contain-fitted in the popups' wooden frame (locked
    /// objects grey at 82 %, open ones in colour, new reveals coming back to colour), the neighbours' edges showing; a
    /// horizontal swipe or the existing raised ‹ › buttons turn it one card at a time with a snap, never cycling;</description></item>
    /// <item><description>the progress strip: the objects' medallions (tappable, the green check on the open ones), under them
    /// the progress bar with "1 / 4" filling with the reveals and the scene chest at its end (the owner's change of
    /// 2026-11-02): dimmed while the scene fills, glowing and hopping when complete, with the check once claimed; a tap on
    /// it opens its tooltip (the contents, the green Claim when ready); "Scene complete" or "Reward claimed", "Scene 2 / N";</description></item>
    /// <item><description>a tapped object (its alpha, never its transparent area) or medallion: a golden outline and the
    /// object sheet from the bottom (the colour preview, the name, "Unlocks after Level N" and "Next level: M" with
    /// "Continue playing", or "Level N completed"), closed by its ×, a swipe down or a tap outside; the card shrinks over it
    /// when the sheet would cover the object.</description></item>
    /// </list>
    /// Before L27 the page shows the locked notice. It only draws and calls <see cref="DesignApp"/> and the album service.
    /// </summary>
    public static class AlbumScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            HomeLook look = HomeScreen.Look(app);
            AlbumService album = app.Meta.Album;
            DesignApp.DrawBackdrop(p, BackdropScene.Home, app.Meta.CurrentLevel, OwnerPictures.Collection);
            IReadOnlyList<AlbumScene> shown = album.Shown;
            bool sheetOpen = app.AlbumSheet != null;
            app.AlbumMoving = false;
            if (!BottomNav.IsOpen(NavPlace.Album, look) || shown.Count == 0)
            {
                var locked = ScreenLayout.LockedPage(p.Width, p.Height, p.Insets);
                Kit.CardFrame(p, locked.Panel);
                Kit.LockedNotice(p, locked.Notice, NavPlace.Album, app.UnlockLevel(NavPlace.Album));
                Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Album), look, app.Navigate);
                Kit.PageHeader(p, locked.Header, PlaytestText.T("album.heading"), app.AlbumBack, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);
                return;
            }

            int index = Math.Max(0, Math.Min(shown.Count - 1, app.AlbumIndex));
            AlbumScene scene = shown[index];
            AlbumSceneView view = album.View(scene);
            AlbumRegions r = AlbumLook.Page(p.Width, p.Height, p.Insets, scene.CanvasWidth, scene.CanvasHeight);
            AlbumSheetRegions sheet = AlbumLook.Sheet(p.Width, p.Height, p.Insets);

            // The snap: after a turn the new centre card slides the rest of its way in from the side it came from.
            (int step, float since) = app.AlbumTurn;
            float snap = step == 0 ? 0f : AlbumLook.Snap(since);
            float slide = snap * step * r.Step;
            app.AlbumMoving |= snap > 0f;
            for (int offset = -1; offset <= 1; offset += 2)
            {
                int neighbour = index + offset;
                if (neighbour >= 0 && neighbour < shown.Count)
                {
                    AlbumScene other = shown[neighbour];
                    Card(p, r, other, album.View(other), r.CardAt(offset, slide), r.SceneAt(offset, slide), null, 0f, null, 0f, false);
                }
            }

            // The centre card, lifted (shrunk about its top's middle) while the sheet would cover the chosen object.
            AlbumObjectView? chosen = sheetOpen ? view.Find(app.AlbumSheet!) : null;
            float lift = 1f;
            if (chosen != null)
            {
                Box layer = AlbumLook.LayerBox(r.Scene, scene.CanvasWidth, chosen.Item.X, chosen.Item.Y, chosen.Item.Width, chosen.Item.Height);
                lift = AlbumLook.LiftAt(AlbumLook.Lift(r.Card, layer.Bottom, sheet.Top, r.W), app.AlbumSheetSince);
                app.AlbumMoving |= app.AlbumSheetSince < 1f;
            }

            (IReadOnlyList<string> reveals, float revealSince) = app.AlbumReveals;
            app.AlbumMoving |= AlbumLook.Revealing(revealSince, reveals.Count);
            p.PushTransform(0f, 0f, lift, r.Card.CenterX, r.Card.Top);
            Card(p, r, scene, view, r.CardAt(0, slide), r.SceneAt(0, slide), reveals, revealSince, chosen?.Item.Id, app.AlbumSheetSince, true);
            p.PopTransform();
            if (!sheetOpen && snap <= 0f)
            {
                // A tap on the scene chooses the topmost object under the finger by its alpha; a transparent area chooses none.
                Box sceneBox = r.Scene;
                p.Hit(sceneBox, () =>
                {
                    var boxes = new List<Box>();
                    foreach (AlbumObject item in scene.Objects)
                    {
                        boxes.Add(AlbumLook.LayerBox(sceneBox, scene.CanvasWidth, item.X, item.Y, item.Width, item.Height));
                    }

                    (float x, float y) = p.TapPoint;
                    int hit = AlbumLook.HitObject(boxes, i => p.SpriteAlpha(PainterBase.AlbumPrefix + scene.Objects[i].Layer), x, y);
                    if (hit >= 0)
                    {
                        app.OpenAlbumObject(scene.Objects[hit].Id);
                    }
                });
            }

            // The scene's name on the name plate, the arrows (greyed at the ends: no cycling).
            Kit.SlicedSprite(p, MetaScreenArt.Tag, r.Ribbon, .16f, .30f, .30f);
            ProfileGamePainter.Label(p, r.Ribbon.Inset(r.W * .03f), PlaytestText.T(scene.TitleKey), ShopCatalogDesign.Name);
            Kit.ArrowButton(p, r.Previous.CenterX, r.Previous.CenterY, r.Previous.Width, false, index > 0 && !sheetOpen ? () => app.TurnAlbum(-1) : (Action?)null);
            Kit.ArrowButton(p, r.Next.CenterX, r.Next.CenterY, r.Next.Width, true, index < shown.Count - 1 && !sheetOpen ? () => app.TurnAlbum(1) : (Action?)null);

            AlbumChestState chest = AlbumLook.ChestState(view.Complete, view.RewardClaimed);
            app.AlbumMoving |= chest == AlbumChestState.Ready || (app.AlbumTipOpen && app.AlbumTipSince < AlbumLook.TipSeconds + .1f);
            Strip(p, app, r, scene, view, index, shown.Count, sheetOpen ? null : app.OpenAlbumObject, reveals, revealSince, chest);

            // A horizontal swipe on the card band turns one card (registered after the content, so it never taps).
            if (!sheetOpen)
            {
                p.Scroll(new Box(r.Safe.Left, r.Ribbon.Bottom, r.Safe.Right, r.Strip.Top), index > 0 ? () => app.TurnAlbum(-1) : (Action?)null, index < shown.Count - 1 ? () => app.TurnAlbum(1) : (Action?)null);
            }

            Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Album), look, app.Navigate);
            Kit.PageHeader(p, r.Header, PlaytestText.T("album.heading"), app.AlbumBack, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);
            if (chosen != null)
            {
                Sheet(p, app, sheet, scene, chosen, album.NextLevel);
            }
            else if (app.AlbumTipOpen)
            {
                ChestTip(p, app, r, album, chest, index > 0 ? () => app.TurnAlbum(-1) : (Action?)null, index < shown.Count - 1 ? () => app.TurnAlbum(1) : (Action?)null);
            }

            if (app.HomeToastText != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Card.Top, r.Safe.Right, r.Strip.Top), app.HomeToastText);
            }
        }

        /// <summary>
        /// A scene card: the popups' wooden frame round the scene, the background and the objects back to front (a locked one
        /// grey at 82 %, an open one in colour, a new reveal crossfading from grey to colour), the chosen object's golden
        /// outline, and a complete scene's check badge.
        /// </summary>
        private static void Card(IPainter p, AlbumRegions r, AlbumScene scene, AlbumSceneView view, Box card, Box sceneBox,
            IReadOnlyList<string>? reveals, float revealSince, string? chosen, float chosenSince, bool centre)
        {
            if (card.Right < 0f || card.Left > p.Width)
            {
                return;
            }

            p.Mark("ui.album.card");
            p.Mark("ui.album.scene");
            Kit.CardFrame(p, card);
            p.PushClipRound(sceneBox, p.U(AlbumLook.SceneRadiusUnits));
            string background = PainterBase.AlbumPrefix + scene.Background;
            if (p.HasSprite(background))
            {
                p.Sprite(background, sceneBox);
            }
            else
            {
                p.FillRect(sceneBox, C.CreamTop);
            }

            foreach (AlbumObjectView item in view.Objects)
            {
                Box layer = AlbumLook.LayerBox(sceneBox, scene.CanvasWidth, item.Item.X, item.Item.Y, item.Item.Width, item.Item.Height);
                string name = PainterBase.AlbumPrefix + item.Item.Layer;
                if (item.Item.Id == chosen)
                {
                    Outline(p, name, layer, r.W * AlbumLook.OutlineShare, AlbumLook.OutlineAlpha(chosenSince));
                }

                int reveal = reveals == null ? -1 : IndexOf(reveals, item.Item.Id);
                float colour = !item.Open ? 0f : reveal >= 0 ? AlbumLook.Reveal(revealSince, reveal) : 1f;
                Layer(p, name, layer, colour);
            }

            p.PopClip();
            if (view.Complete)
            {
                Box badge = r.CompleteBadge(card);
                Kit.CheckBadge(p, badge.CenterX, badge.CenterY, badge.Width);
            }
        }

        /// <summary>
        /// An object's layer: grey at 82 % (the grey copy under a black tint of 18 % in its silhouette) under its colour
        /// picture at <paramref name="colour"/> (0 locked, 1 open, between them the colour's return).
        /// </summary>
        private static void Layer(IPainter p, string name, Box box, float colour)
        {
            if (!p.HasSprite(name))
            {
                return;
            }

            if (colour < 1f)
            {
                p.Sprite(name + PainterBase.GreySuffix, box);
                p.SpriteTint(name, box, Rgba.Black.WithAlpha(1f - AlbumLook.LockedBrightness));
            }

            if (colour > 0f)
            {
                p.PushAlpha(colour);
                p.Sprite(name, box);
                p.PopAlpha();
            }
        }

        /// <summary>The thin golden outline round an object (<c>ui.album.outline</c>): its silhouette in gold, eight offset copies behind it.</summary>
        private static void Outline(IPainter p, string name, Box box, float width, float alpha)
        {
            if (alpha <= 0f || !p.HasSprite(name))
            {
                return;
            }

            p.Mark("ui.album.outline");
            Rgba gold = C.MedalGold.WithAlpha(alpha);
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4.0;
                p.SpriteTint(name, box.Offset((float)Math.Cos(a) * width, (float)Math.Sin(a) * width), gold);
            }
        }

        /// <summary>
        /// The progress strip: a raised cream row with the objects' medallions (each object's layer in a cream disc in a wooden
        /// ring, grey or in colour with the green check; tappable), under them the progress bar ("1 / 4" on it, its fill
        /// growing with the reveals) running into the chest at its right end, and the caption row ("Scene complete" or
        /// "Reward claimed", "Scene 2 / N").
        /// </summary>
        private static void Strip(IPainter p, DesignApp app, AlbumRegions r, AlbumScene scene, AlbumSceneView view, int index, int count, Action<string>? tap,
            IReadOnlyList<string> reveals, float revealSince, AlbumChestState chest)
        {
            Kit.RaisedRow(p, r.Strip, r.W * .06f);
            for (int i = 0; i < view.Objects.Count; i++)
            {
                AlbumObjectView item = view.Objects[i];
                Box disc = r.Medallion(i, view.Objects.Count);
                int reveal = IndexOf(reveals, item.Item.Id);
                bool open = item.Open && (reveal < 0 || AlbumLook.Reveal(revealSince, reveal) >= 1f);
                Medallion(p, disc, PainterBase.AlbumPrefix + item.Item.Layer, open, item.Item.Id == app.AlbumSheet);
                if (tap != null)
                {
                    string id = item.Item.Id;
                    p.Hit(r.MedallionTouch(i, view.Objects.Count), () => tap(id));
                }
            }

            float share = AlbumLook.BarShare(view.OpenCount, view.Total, CountOpen(view, reveals), revealSince);
            ProgressBar(p, r, share, PlaytestText.F("album.count", view.OpenCount, view.Total));
            Chest(p, r, chest, revealSince, tap != null ? app.ToggleAlbumChest : (Action?)null);
            string? status = chest == AlbumChestState.Claimed ? PlaytestText.T("album.reward_claimed") : view.Complete ? PlaytestText.T("album.complete") : null;
            if (status != null)
            {
                ProfileGamePainter.Label(p, r.StatusLine, status, ProfileGameDesign.Item, left: true, heightShare: .9f);
            }

            ProfileGamePainter.Label(p, r.SceneLine, PlaytestText.F("album.scene_index", index + 1, count), ProfileGameDesign.Info, heightShare: .9f);
        }

        /// <summary>A medallion (<c>ui.album.medallion</c>): a wooden ring, a cream disc, the object's layer fitted in it; the green check when open, a gold ring when chosen.</summary>
        public static void Medallion(IPainter p, Box disc, string layer, bool open, bool chosen)
        {
            p.Mark("ui.album.medallion");
            float radius = disc.Width / 2f;
            p.FillCircle(disc.CenterX, disc.CenterY + (radius * .06f), radius, C.GardenShadow.WithAlpha(.18f));
            p.FillCircle(disc.CenterX, disc.CenterY, radius, chosen ? C.MedalGold : C.WoodDarkTop);
            p.FillCircle(disc.CenterX, disc.CenterY, radius * .86f, C.CreamTop);
            p.FillCircle(disc.CenterX, disc.CenterY, radius * .8f, C.ParchmentWell.Mix(C.CreamTop, .5f));
            if (p.HasSprite(layer))
            {
                (int Width, int Height)? size = p.SpriteSize(layer);
                Box art = AlbumLook.Fit(disc.Inset(radius * .3f), size?.Width ?? 1, size?.Height ?? 1);
                Layer(p, layer, art, open ? 1f : 0f);
            }

            if (open)
            {
                float check = disc.Width * .34f;
                Kit.CheckBadge(p, disc.Right - (check * .45f), disc.Bottom - (check * .45f), check);
            }
        }

        /// <summary>How many of <paramref name="reveals"/> are open objects of <paramref name="view"/> (the bar grows with them).</summary>
        private static int CountOpen(AlbumSceneView view, IReadOnlyList<string> reveals)
        {
            int n = 0;
            foreach (string id in reveals)
            {
                n += view.Find(id)?.Open == true ? 1 : 0;
            }

            return n;
        }

        /// <summary>
        /// The progress bar (<c>ui.album.progress</c>): a light wood track pressed into the strip (<see cref="UiRaster.Groove"/>)
        /// and the glossy green fill raised in it (the green button's face, <see cref="UiRaster.ButtonFace(int, int, ColorSet, float, bool)"/>),
        /// "n / 4" in white letters on it.
        /// </summary>
        public static void ProgressBar(IPainter p, AlbumRegions r, float share, string label)
        {
            p.Mark("ui.album.progress");
            p.Picture("ui.groove", r.Bar, UiRaster.Groove);
            Box? fill = r.BarFill(share);
            if (fill != null)
            {
                p.Picture("ui.album.progress.fill", fill.Value, (w, h) => UiRaster.ButtonFace(w, h, GardenLook.Green, .5f, true));
            }

            Box text = new Box(r.Bar.Left, r.Bar.Top, r.Chest.Left, r.Bar.Bottom);
            ProfileGamePainter.Label(p, text, label, ProfileGameDesign.Progress, ShopBodyDesign.Letters, heightShare: .78f);
        }

        /// <summary>
        /// The scene chest (<c>ui.album.chest</c>, the owner's picture) over the bar's end: always in full colour (the owner, 2026-11-02: «Сундук не серый… всегда активный»; while the scene fills its tooltip
        /// shows the contents without Claim), with a soft glow, a breath and
        /// a gentle hop when ready, in colour with the green check ball once claimed. A tap toggles its tooltip.
        /// </summary>
        public static void Chest(IPainter p, AlbumRegions r, AlbumChestState state, float seconds, Action? tap)
        {
            p.Mark("ui.album.chest");
            string name = PainterBase.AlbumPrefix + AlbumCatalog.ChestPicture;
            (float scale, float hop, float glow) = AlbumLook.ChestMotion(state, seconds);
            Box box = r.Chest;
            if (glow > 0f)
            {
                // The glow: soft rings of garden.glow behind the chest, as the lit speed pill's halo.
                for (int i = GardenLook.SpeedGlowLayers; i >= 1; i--)
                {
                    float radius = box.Width * (.36f + (.05f * i));
                    p.FillCircle(box.CenterX, box.CenterY + (box.Height * .05f), radius, C.GardenGlow.WithAlpha(glow * .3f));
                }
            }

            p.PushTransform(0f, hop * box.Height, scale, box.CenterX, box.Bottom);
            if (!p.HasSprite(name))
            {
                p.FillRound(box.Inset(box.Width * .15f), box.Width * .12f, C.WoodDarkTop);
            }
            else
            {
                p.Sprite(name, box);
            }

            p.PopTransform();
            if (state == AlbumChestState.Claimed)
            {
                Box check = r.ChestCheck;
                Kit.CheckBadge(p, check.CenterX, check.CenterY, check.Width);
            }

            if (tap != null)
            {
                p.Hit(r.ChestTouch, tap);
            }
        }

        /// <summary>
        /// The chest's tooltip (<c>ui.album.chest.tip</c>): a tap anywhere outside closes it (the swipe still turns the card);
        /// the guides' parchment bubble above the chest with its tail on the chest, the title, the contents (the Petals lotus
        /// and "200 Petals", the Return and Extra Slot booster icons and "1 Return", "1 Extra Slot") and the footer: the green
        /// Claim when the chest is ready (no purchase confirmation: a gift), the check and "Reward claimed" once claimed, or
        /// "Open every object to get it" while the scene fills.
        /// </summary>
        private static void ChestTip(IPainter p, DesignApp app, AlbumRegions r, AlbumService album, AlbumChestState state, Action? previous, Action? next)
        {
            p.Mark("ui.album.chest.tip");
            p.Hit(new Box(0f, 0f, p.Width, p.Height), app.CloseAlbumChest);

            // The swipe registered again over the tap-outside target, so the tooltip never blocks it (a turn closes the tooltip).
            p.Scroll(new Box(r.Safe.Left, r.Ribbon.Bottom, r.Safe.Right, r.Strip.Top), previous, next);
            bool claim = state == AlbumChestState.Ready;
            AlbumChestTipRegions t = AlbumLook.ChestTip(r, claim, state == AlbumChestState.Claimed);
            float pop = AlbumLook.TipPop(app.AlbumTipSince);
            p.PushTransform(0f, 0f, pop, t.TailX, t.Bubble.Bottom + (t.Tail / 2f));
            Box b = t.Bubble;
            p.PushRotate(45f, t.TailX, b.Bottom);
            Box diamond = Box.FromCenter(t.TailX, b.Bottom, t.Tail * 1.1f, t.Tail * 1.1f);
            p.FillRound(diamond, p.U(6f), C.ParchmentBottom);
            p.StrokeRound(diamond, p.U(6f), p.U(DesignTokens.Garden.OutlineWidth) * .8f, C.ParchmentLine);
            p.PopTransform();
            Kit.CardFrame(p, b);
            p.Hit(b, () => { });
            // The prizes as big icons with only their amounts (the owner, 2026-11-02: no words).
            AlbumChest chest = album.Chest;
            Kit.Petal(p, t.Icons[0]);
            ProfileGamePainter.Label(p, t.Texts[0], NumberText.Group(chest.Petals), ProfileGameDesign.Name, heightShare: .95f);
            Kit.BoosterIcon(p, "return", t.Icons[1]);
            ProfileGamePainter.Label(p, t.Texts[1], PlaytestText.F("album.chest.count", chest.Return), ProfileGameDesign.Name, heightShare: .95f);
            Kit.BoosterIcon(p, "extra_slot", t.Icons[2]);
            ProfileGamePainter.Label(p, t.Texts[2], PlaytestText.F("album.chest.count", chest.ExtraSlot), ProfileGameDesign.Name, heightShare: .95f);
            switch (state)
            {
                case AlbumChestState.Ready:
                    Kit.PrimaryButton(p, t.Footer, PlaytestText.T("album.claim"), album.CanPay ? app.ClaimAlbumReward : (Action?)null, ProfileGameDesign.Action);
                    break;
                case AlbumChestState.Claimed:
                    Kit.CheckBadge(p, t.FooterIcon.CenterX, t.FooterIcon.CenterY, t.FooterIcon.Width);
                    break;
            }

            p.PopTransform();
        }

        /// <summary>
        /// The object sheet (<c>ui.album.sheet</c>): a light scrim (a tap outside closes it), the popups' card frame rising from
        /// the bottom with the jam sheet's grip, the close, the object's colour preview in a well, its name and either "Unlocks
        /// after Level N" with "Next level: M" and the green "Continue playing", or the check and "Level N completed".
        /// </summary>
        private static void Sheet(IPainter p, DesignApp app, AlbumSheetRegions s, AlbumScene scene, AlbumObjectView item, int nextLevel)
        {
            p.Mark("ui.album.sheet");
            float rise = Kit.SheetRise(app.AlbumSheetSince);
            p.FillRect(new Box(0f, 0f, p.Width, p.Height), C.SurfaceScrim.WithAlpha(.3f * Math.Min(1f, rise)));
            p.Hit(new Box(0f, 0f, p.Width, p.Height), app.CloseAlbumSheet);
            p.PushTransform(0f, (1f - rise) * (p.Height - s.Top), 1f, 0f, 0f);
            Kit.CardFrame(p, s.Panel);
            p.Hit(s.Panel, () => { });
            p.Scroll(s.Panel, app.CloseAlbumSheet, null);
            p.FillRound(s.Grip, s.Grip.Height / 2f, C.ParchmentEdge.Darken(.12f));

            Box well = Kit.PictureFrame(p, s.Preview);
            string layer = PainterBase.AlbumPrefix + item.Item.Layer;
            if (p.HasSprite(layer))
            {
                (int Width, int Height)? size = p.SpriteSize(layer);
                p.Sprite(layer, AlbumLook.Fit(well.Inset(well.Width * .08f), size?.Width ?? 1, size?.Height ?? 1));
            }

            ProfileGamePainter.Label(p, s.Name, PlaytestText.T(item.Item.TitleKey), ProfileGameDesign.Name, left: true, heightShare: .8f);
            if (item.Open)
            {
                Kit.CheckBadge(p, s.Icon1.CenterX, s.Icon1.CenterY, s.Icon1.Width * .8f);
                ProfileGamePainter.Label(p, s.Line1, PlaytestText.F("album.level_completed", NumberText.Group(item.Level ?? 0)), ProfileGameDesign.Info, left: true, heightShare: .8f);
            }
            else
            {
                p.Mark("ui.lock");
                p.Shape("ui.lock", s.Icon1.Inset(s.Icon1.Width * .12f), C.InkBrownSoft);
                string line = item.Level.HasValue ? PlaytestText.F("album.unlocks_after", NumberText.Group(item.Level.Value)) : PlaytestText.T("album.coming_soon");
                ProfileGamePainter.Label(p, s.Line1, line, ProfileGameDesign.Info, left: true, heightShare: .8f);
                ProfileGamePainter.Label(p, s.Line2, PlaytestText.F("album.next_level", NumberText.Group(nextLevel)), ProfileGameDesign.Info, left: true, heightShare: .7f);
                Kit.PrimaryButton(p, s.Button, PlaytestText.T("album.continue"), app.AlbumContinue);
            }

            Kit.RoundButton(p, s.Close.CenterX, s.Close.CenterY, s.Close.Width, "ui.close", app.CloseAlbumSheet);
            p.PopTransform();
        }

        private static int IndexOf(IReadOnlyList<string> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
