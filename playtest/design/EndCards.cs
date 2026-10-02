using System;
using System.Collections.Generic;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The screens at a level's end and the cards before it (spec 002 FR-019 to FR-021) in the reference look and the
    /// reference layouts of spec 005 (<c>specs/005-reference-look/contracts/look.md</c> §4.3, §4.4, §6.2, §6.3):
    /// <list type="bullet">
    /// <item><description>the win (frame 15): the reference's full-screen celebration (FR-023), no card and no top bar,
    /// over the win garden (<c>bg.win</c>): the wooden "Level complete!" sign with flower clusters, the finished picture
    /// large in its stone frame, the celebrating hero (or the group) on the stone pedestal over the picture's foot in
    /// light rays and falling petals, the reward pill on the pedestal and the big Next in its wood rim
    /// (<see cref="ScreenLayout.WinScreen"/>);</description></item>
    /// <item><description>the milestone (frame 16) in the same full-screen language;</description></item>
    /// <item><description>the jam (frame 10): the centered modal card over the dimmed gameplay (FR-022,
    /// <see cref="ScreenLayout.JamCard"/>): the slot contents in a well and the recoveries as a two-column grid of big
    /// colored choices with their cost pills, and Restart;</description></item>
    /// <item><description>the demo and unlock cards (frame 21).</description></item>
    /// </list>
    /// </summary>
    public static class EndCards
    {
        /// <summary>
        /// How long the win fades in over the gameplay (seconds); after it the celebration replaces the gameplay screen,
        /// its top bar included (<see cref="LevelScreen"/>).
        /// </summary>
        public const float WinFadeSeconds = 0.35f;

        /// <summary>
        /// The win (§6.3): the win garden, the finished picture under the wooden sign, the celebrating hero on the pedestal
        /// in the light rays and petals, the Petals earned counting up in the reward pill on the pedestal's front (a dropped
        /// booster charge at its left), Next at the bottom and the cream Pause button in the top-left corner, so Home,
        /// Restart and Settings stay reachable (FR-016). The ×2 reward needs a rewarded ad, which the playtest does not
        /// have, so it is never offered here (the Unity client shows it at <see cref="WinRegions.Double"/>). The spec 001
        /// sequence stays: reveal, then reward, then Next.
        /// </summary>
        public static void Win(IPainter p, LevelScreen s, float since)
        {
            LevelReward? reward = s.Payout?.Reward;
            WinRegions r = ScreenLayout.WinScreen(p.Width, p.Height, p.Insets);
            Family family = Visuals.MainFamily(s.Session.Definition);
            p.PushAlpha(Kit.Ease(since / WinFadeSeconds));
            Garden(p, s, r, since);
            Celebrant hero = Cast(p, r, family);
            Rays(p, r, hero, since);

            // The finished picture, its cells in full color in a thin stone frame (BoardPainter.Picture), as large as its
            // region allows from under the sign; it pops in and a light band sweeps it once.
            p.Mark("fx.win_shine");
            Box picture = PictureBox(r.Picture, s.Session.Picture);
            p.PushTransform(0f, 0f, Kit.Pop(since - 0.05f), picture.CenterX, picture.CenterY);
            BoardPainter.Picture(p, picture, s.Session.Definition, s.Session.Picture);
            if (since < 1.3f)
            {
                float band = picture.Left + ((picture.Width + p.U(200f)) * Visuals.Clamp01((since - 0.1f) / 1.2f)) - p.U(100f);
                p.PushClip(picture);
                p.Line(band - p.U(60f), picture.Bottom, band + p.U(60f), picture.Top, p.U(70f), Rgba.White.WithAlpha(0.3f));
                p.PopClip();
            }

            p.PopTransform();
            if (s.Payout?.Milestone != null)
            {
                MilestonePill(p, picture.CenterX, picture.Top + (p.U(MilestoneMarkUnits) * 0.62f), p.U(MilestoneMarkUnits));
            }

            Sign(p, r, PlaytestText.T("win.title"), since);
            Stand(p, r, hero, family, since);
            Petals(p, r, since);

            // The reward rises in after the reveal (motion.reward); Next works at once.
            float rise = Kit.Ease((since - 0.15f) / DesignTokens.Motion.Reward.Seconds);
            p.PushAlpha(rise);
            p.PushTransform(0f, (1f - rise) * p.U(30f), 1f, 0f, 0f);
            if (reward != null)
            {
                RewardPill(p, r.Reward, reward.Petals, since - 0.15f);
                if (reward.DroppedBooster.HasValue)
                {
                    DroppedBooster(p, r.Drop, reward.DroppedBooster.Value);
                }
            }

            p.PopTransform();
            p.PopAlpha();

            Kit.PrimaryButton(p, r.Next, PlaytestText.T("common.next"), s.Next, T.ButtonLarge, decorate: true, breathe: true);
            // While the win fades in, the gameplay's own Pause above it still takes the taps (LevelScreen).
            PauseButton(p, s, r, since >= WinFadeSeconds);
            p.PopAlpha();
        }

        /// <summary>
        /// The milestone (frame 16) in the win's full-screen language: the wooden sign with "Level N", the rewards on a
        /// parchment panel where the win shows its picture (each on a cream tile with its amount in a cream pill over the
        /// tile's bottom edge), the celebrating hero on the pedestal in the rays and petals, "Milestone reached!" with the
        /// gold medal in the pill on the pedestal's front, and Continue.
        /// </summary>
        public static void Milestone(IPainter p, LevelScreen s, float since)
        {
            MilestoneGrant grant = s.Payout!.Milestone!;
            var items = new List<(Action<Box> Icon, string Amount)>();
            if (grant.Item != null)
            {
                string itemId = grant.Item;
                CosmeticItem? item = PlaytestMeta.Cosmetics.TryGet(itemId, out CosmeticItem? found) ? found : null;
                string name = PlaytestText.Has("cosmetic." + itemId) ? PlaytestText.T("cosmetic." + itemId) : item?.Name ?? PlaytestText.T("wardrobe.kind_hat");
                items.Add((box =>
                {
                    string shape = item != null ? ShapeLibrary.CosmeticId(item.Shape) : "ui.star";
                    p.Shape(ShapeLibrary.Has(shape) ? shape : "ui.star", box, item != null ? Visuals.Tint(item) : C.MedalGold);
                }, name));
            }

            if (grant.Petals > 0)
            {
                items.Add((box => Kit.Petal(p, box), NumberText.Plus(grant.Petals)));
            }

            BoosterGrant? boosters = grant.Boosters;
            if (boosters != null)
            {
                foreach ((string id, int count) in new[] { ("extra_slot", boosters.ExtraSlot), ("shuffle", boosters.Shuffle), ("return", boosters.Return), ("bloom_burst", boosters.BloomBurst) })
                {
                    if (count > 0)
                    {
                        string booster = id;
                        items.Add((box => Kit.BoosterIcon(p, booster, box), NumberText.Plus(count)));
                    }
                }
            }

            WinRegions r = ScreenLayout.WinScreen(p.Width, p.Height, p.Insets);
            Family family = Visuals.MainFamily(s.Session.Definition);
            Garden(p, s, r, since);
            Celebrant hero = Cast(p, r, family);
            Rays(p, r, hero, since);
            Sign(p, r, PlaytestText.F("common.level", NumberText.Group(grant.Level)), since);

            // The rewards on a parchment panel in the picture's place, above the hero's head.
            float rise = Kit.Ease((since - 0.15f) / DesignTokens.Motion.Reward.Seconds);
            p.PushAlpha(rise);
            p.PushTransform(0f, (1f - rise) * p.U(30f), 1f, 0f, 0f);
            Rewards(p, new Box(r.Picture.Left, r.Picture.Top, r.Picture.Right, Math.Min(r.Picture.Bottom, hero.Picture.Top + (hero.Picture.Height * HeadRoom))), items);
            p.PopTransform();
            p.PopAlpha();

            Stand(p, r, hero, family, since);
            Petals(p, r, since);
            p.PushAlpha(rise);
            MilestonePill(p, r.Reward.CenterX, r.Reward.CenterY, Math.Min(r.Reward.Height, p.U(MilestonePillUnits)), r.Reward.Width * MilestonePillWidth);
            p.PopAlpha();

            Kit.PrimaryButton(p, r.Next, PlaytestText.T("milestone.continue"), s.Next, T.ButtonLarge, decorate: true, breathe: true);
            PauseButton(p, s, r, true);
        }

        /// <summary>
        /// The jam (frame 10; §6.2, FR-022): a centered parchment card over the warm-dimmed gameplay: "No more space!" and
        /// its subtitle, the Waiting Slots' contents in an inset well, the recoveries the player can use now as a two-column
        /// grid of big colored choices (green Extra Slot and Shuffle, blue Return and Bloom Burst), each with its cost pill
        /// hanging under it (×N charges with the booster's icon, or the lotus and the price), the free rescue once per attempt
        /// (▶ Free), and Restart. The rules do not let the jam be dismissed, so the card has no close button. The gameplay
        /// shows around the card (spec 001 FR-027); its scrim takes the taps off the board and the tray, and the top bar
        /// above it (Pause, the speed) stays usable as before.
        /// </summary>
        public static void Jam(IPainter p, LevelScreen s, float since)
        {
            var choices = new List<(string Id, ColorSet Set, string Label, Cost Cost, Action Action)>();
            foreach (Recovery recovery in s.Session.EligibleRecoveries())
            {
                foreach ((BoosterKind kind, Recovery rec, string id) in LevelScreen.Boosters)
                {
                    if (rec == recovery && s.Meta.Economy.IsUnlocked(kind) && s.Meta.Economy.CanAfford(kind))
                    {
                        int charges = s.Meta.Economy.Charges(kind);
                        Cost cost = charges > 0 ? Cost.Charges(charges) : Cost.Petals(s.Meta.Economy.Price(kind));
                        BoosterKind used = kind;
                        Recovery chosen = rec;
                        choices.Add((id, ChoiceSet(id), BoosterName(kind), cost, () => s.PressBooster(used, chosen)));
                    }
                }
            }

            (BoosterKind Kind, Command Command)? rescue = s.RescueOffer();
            if (rescue.HasValue)
            {
                // The free rescue: a green choice with the ▶ Free pill (a rewarded ad in the game).
                choices.Add((IdOf(rescue.Value.Kind), GardenLook.Green, BoosterName(rescue.Value.Kind), Cost.Free, s.UseRescue));
            }

            bool stuck = s.Session.Status == LevelStatus.Stuck;
            JamCardRegions r = ScreenLayout.JamCard(p.Width, p.Height, p.Insets, choices.Count, hasClose: false);

            // The warm scrim (surface.scrim, alpha 0.5) fades in; below the top bar it takes every tap the card does not.
            float fade = Kit.Ease(since / DesignTokens.Motion.Pop.Seconds);
            p.FillRect(new Box(0f, 0f, p.Width, p.Height), C.SurfaceScrim.WithAlpha(C.SurfaceScrim.A / 255f * fade));
            p.Hit(new Box(0f, TopBarBottom(p), p.Width, p.Height), () => { });

            // The card pops in (motion.pop).
            p.Mark("ui.sheet");
            p.PushTransform(0f, 0f, Kit.Pop(since), r.Card.CenterX, r.Card.CenterY);
            float radius = Math.Max(p.U(DesignTokens.Radius.CardMin), r.Card.Width * DesignTokens.Radius.Card);
            Kit.Paper(p, r.Card, radius, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            p.Hit(r.Card, () => { });

            // The title as big as the reference's, the subtitle in up to two lines broken after its first sentence.
            float titleScale = r.Title.Height * TitleFill / p.U(T.Title.Size);
            p.Text(PlaytestText.T(stuck ? "jam.stuck" : "jam.title"), r.Title.CenterX, r.Title.CenterY, T.Title, C.InkTitle, r.Title.Width, titleScale, TextLook.Plain(C.InkTitle));
            float line = r.Subtitle.Height / 2f;
            float bodyScale = line * SubtitleFill / p.U(T.Body.Size);
            List<string> subtitle = Lines(p, PlaytestText.T(stuck ? "jam.stuck_subtitle" : "jam.subtitle"), T.Body, r.Subtitle.Width / bodyScale, 2);
            float first = subtitle.Count > 1 ? r.Subtitle.Top + (line / 2f) : r.Subtitle.CenterY;
            for (int i = 0; i < subtitle.Count; i++)
            {
                p.Text(subtitle[i], r.Subtitle.CenterX, first + (i * line), T.Body, C.InkBrownSoft, r.Subtitle.Width, bodyScale);
            }

            SlotContents(p, r, s);
            for (int i = 0; i < choices.Count; i++)
            {
                (string id, ColorSet set, string label, Cost cost, Action action) = choices[i];
                Choice(p, r.Choices[i], r.CostPills[i], set, GardenLook.BoosterIcon(id), label, cost, action);
            }

            Kit.SecondaryButton(p, r.Restart, PlaytestText.T("common.restart"), s.Restart, "ui.restart", T.Button);
            p.PopTransform();
        }

        /// <summary>
        /// A demo or unlock card shown once before play; a tap anywhere closes it. A booster's card shows the booster's
        /// icon on a cream tile, a variant's card the variant tiles (with the ignore mark between siblings), and every
        /// card its lines in brown, wrapped to the card.
        /// </summary>
        public static void Demo(IPainter p, LevelScreen s, float since)
        {
            DemoCard demo = s.Demo!;
            string? booster = demo.Id.StartsWith("booster.", StringComparison.Ordinal) ? demo.Id.Substring("booster.".Length) : null;
            float width = ScreenLayout.Card(p.Width, p.Height, p.Insets, 0f).Body.Width * 0.94f;
            var lines = new List<(string Text, bool First)>();
            for (int i = 0; i < demo.Lines.Count; i++)
            {
                foreach (string line in Lines(p, demo.Lines[i], i == 0 ? T.ButtonSecondary : T.Body, width, 3))
                {
                    lines.Add((line, i == 0));
                }
            }

            const float lineUnits = 58f;
            float iconUnits = booster != null ? 200f : 0f;
            float tilesUnits = demo.Variants.Count > 0 ? 230f : 0f;
            // The card has no title: its content starts near the top edge, and the caption sits near the bottom one.
            float content = iconUnits + (lines.Count * lineUnits) + tilesUnits + 24f;
            CardRegions r = Kit.Card(p, content, string.Empty, null, Kit.Pop(since));
            float y = r.Card.Top + p.U(52f);
            if (booster != null)
            {
                float tile = p.U(170f);
                Box tileBox = Box.FromCenter(r.Body.CenterX, y + (tile / 2f), tile, tile);
                Box face = Kit.IconFace(p, tileBox, GardenLook.White, tile * 0.26f, 0f);
                Kit.BoosterIcon(p, booster, Box.FromCenter(face.CenterX, face.CenterY, tile * 0.7f, tile * 0.7f));
                y += p.U(iconUnits);
            }

            foreach ((string text, bool first) in lines)
            {
                Rgba ink = first ? C.InkBrown : C.InkBrownSoft;
                p.Text(text, r.Body.CenterX, y + p.U(lineUnits / 2f), first ? T.ButtonSecondary : T.Body, ink, r.Body.Width, look: TextLook.Plain(ink));
                y += p.U(lineUnits);
            }

            if (demo.Variants.Count > 0)
            {
                float size = p.U(170f);
                var row = new Box(r.Body.Left, y + p.U(24f), r.Body.Right, y + p.U(24f) + size);
                Box[] cells = ScreenLayout.Row(row, demo.Variants.Count, size * 0.7f, size, square: true);
                for (int i = 0; i < cells.Length; i++)
                {
                    Kit.CandyTile(p, cells[i], demo.Variants[i], TileStyle.Sticker);
                }

                if (demo.ShowIgnore)
                {
                    p.Shape("ui.cross", Box.FromCenter(row.CenterX, row.CenterY, size * 0.45f, size * 0.45f), C.StateDanger);
                }
            }

            p.Text(PlaytestText.T("demo.tap_continue"), r.Body.CenterX, r.Card.Bottom - p.U(46f), T.Caption, C.InkBrownSoft, r.Body.Width);
            Kit.EndCard(p);
            p.Hit(new Box(0f, 0f, p.Width, p.Height), s.CloseDemo);
        }

        public static string BoosterName(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => PlaytestText.T("booster.extra_slot"),
            BoosterKind.Shuffle => PlaytestText.T("booster.shuffle"),
            BoosterKind.Return => PlaytestText.T("booster.return"),
            _ => PlaytestText.T("booster.bloom_burst"),
        };

        /// <summary>
        /// A text in at most <paramref name="maxLines"/> lines of <paramref name="width"/>: one line when it fits, else
        /// two balanced lines that prefer to break after a sentence or a comma (as the reference's jam subtitle), else
        /// greedy lines (the last one may shrink to fit).
        /// </summary>
        public static List<string> Lines(IPainter p, string text, TypeStyle style, float width, int maxLines)
        {
            var lines = new List<string>();
            if (maxLines <= 1 || p.MeasureText(text, style) <= width)
            {
                lines.Add(text);
                return lines;
            }

            string[] words = text.Split(' ');
            int best = -1;
            float bestCost = float.MaxValue;
            for (int i = 1; i < words.Length; i++)
            {
                string a = string.Join(" ", words, 0, i);
                string b = string.Join(" ", words, i, words.Length - i);
                float wa = p.MeasureText(a, style);
                float wb = p.MeasureText(b, style);
                if (wa > width || wb > width)
                {
                    continue;
                }

                char end = a[a.Length - 1];
                float cost = Math.Max(wa, wb) - (end == '.' || end == ',' || end == ':' || end == '!' || end == '?' ? width * 0.25f : 0f);
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
                return lines;
            }

            string current = string.Empty;
            foreach (string word in words)
            {
                string next = current.Length == 0 ? word : current + " " + word;
                if (current.Length > 0 && p.MeasureText(next, style) > width && lines.Count < maxLines - 1)
                {
                    lines.Add(current);
                    current = word;
                }
                else
                {
                    current = next;
                }
            }

            lines.Add(current);
            return lines;
        }


        // ---- The celebration (spec 005 §4.4, §6.3) ----

        /// <summary>The jam title's letters fill this share of its box's height (the reference's "No More Space!" spans about 0.5 W).</summary>
        private const float TitleFill = 0.68f;

        /// <summary>The jam subtitle's letters fill this share of each of its two lines.</summary>
        private const float SubtitleFill = 0.78f;

        /// <summary>Each line of a two-line win sign is this share of the plank's height (the reference's "Level / Complete!").</summary>
        private const float SignLine = 0.34f;

        /// <summary>A one-line sign's letters fill this share of the plank's height.</summary>
        private const float SignSingle = 0.5f;

        /// <summary>The letters stay inside this share of the plank's width.</summary>
        private const float SignText = 0.78f;

        /// <summary>The win sign's corner radius as a share of its height (a tall plank, so rounder than a share of 0.28 would be).</summary>
        private const float SignRadius = 0.2f;

        /// <summary>A flower cluster is this share of the sign's height, at most <see cref="ClusterMaxShare"/> of the width.</summary>
        private const float ClusterShare = 1.2f;

        private const float ClusterMaxShare = 0.34f;

        /// <summary>The group (when the level's celebrating hero is missing) is at most this share of the pedestal wide.</summary>
        private const float GroupShare = 1.15f;

        /// <summary>The milestone's reward panel ends this share of the hero's height below the hero box's top (above the heads).</summary>
        private const float HeadRoom = 0.12f;

        /// <summary>The milestone mark over the win picture's top edge, in units.</summary>
        private const float MilestoneMarkUnits = 70f;

        /// <summary>The milestone pill on the pedestal, at most this tall in units and this share of the reward box wide.</summary>
        private const float MilestonePillUnits = 120f;

        private const float MilestonePillWidth = 1.5f;

        /// <summary>Who celebrates on the pedestal: the level's celebrating hero (<see cref="Cheer"/>) or the group, and where the rays turn.</summary>
        private readonly struct Celebrant
        {
            public Celebrant(Box picture, string? cheer, float raysX, float raysY)
            {
                Picture = picture;
                Cheer = cheer;
                RaysX = raysX;
                RaysY = raysY;
            }

            public Box Picture { get; }

            /// <summary>The owner's celebrating hero of the level's main family (pictures.md A7), or null for the group.</summary>
            public string? Cheer { get; }

            public float RaysX { get; }

            public float RaysY { get; }
        }

        /// <summary>
        /// The hero standing on the pedestal (§6.3, spec 004 FR-017): its feet just behind the middle of the pedestal's top,
        /// the level's celebrating hero filling the hero box when the owner's picture exists (pictures.md A7), else the four
        /// heroes, their heads at the hero box's top, at most <see cref="GroupShare"/> of the pedestal wide. The rays turn
        /// behind its body.
        /// </summary>
        private static Celebrant Cast(IPainter p, WinRegions r, Family family)
        {
            (float topY, float ry) = HomeStage.PedestalTop(r.Pedestal);
            float feet = Math.Max(r.Hero.Top + (r.Hero.Height * HomeStage.FeetShare), topY - (ry * 0.25f));
            string cheer = CharacterArt.Cheer(family);
            if (p.HasSprite(cheer))
            {
                Box solo = HomeStage.Figure(r.Hero.CenterX, feet, r.Hero.Height);
                return new Celebrant(solo, cheer, solo.CenterX, solo.CenterY);
            }

            float span = (CharacterArt.GroupFeetShare - CharacterArt.GroupHeadShare) * CharacterArt.GroupHeight / CharacterArt.GroupWidth;
            float width = Math.Min(r.Pedestal.Width * GroupShare, (feet - r.Hero.Top) / span);
            Box group = CharacterArt.GroupStanding(r.Hero.CenterX, feet, width);
            return new Celebrant(group, null, group.CenterX, feet - (group.Height * 0.22f));
        }

        /// <summary>
        /// The win garden behind the celebration (§4.2, <c>bg.win</c>: the owner's picture, else the level's lawn blurred and
        /// lightened with a warm glow in the middle), a sprinkle of confetti falling behind everything for the first
        /// seconds, and a catch-all target, so a tap off the buttons does nothing (and none reaches the gameplay below
        /// while the win fades in). The owner's picture is drawn from the screen's top, as large as it takes for its own
        /// stone disc to lie under the layout's pedestal (<see cref="OwnerPictures.TopAnchored"/>): the disc is then the
        /// stage, and no drawn pedestal stands on it (<see cref="PaintedStage"/>).
        /// </summary>
        private static void Garden(IPainter p, LevelScreen s, WinRegions r, float since)
        {
            var screen = new Box(0f, 0f, p.Width, p.Height);
            float stage = HomeStage.PedestalTop(r.Pedestal).CenterY;
            DesignApp.DrawBackdrop(p, BackdropScene.Win, s.Level, place: (w, h) => OwnerPictures.TopAnchored(screen, w, h, stage, OwnerPictures.WinStageShare));
            p.Hit(screen, () => { });
            Confetti(p, s, screen, since);
        }

        /// <summary>Whether the owner's win picture is drawn: its own stone disc is the hero's stage (pictures.md B8).</summary>
        private static bool PaintedStage(IPainter p) => p.HasSprite(PainterBase.BackgroundPrefix + OwnerPictures.Win);

        /// <summary>The light rays behind the hero (§3.9, <c>fx.rays</c>), fading in, turning slowly.</summary>
        private static void Rays(IPainter p, WinRegions r, Celebrant hero, float since)
        {
            p.PushAlpha(0.85f * Kit.Ease(since / 0.6f));
            Kit.LightRays(p, hero.RaysX, hero.RaysY, r.RaysRadius, since);
            p.PopAlpha();
        }

        /// <summary>
        /// The stone pedestal (unless the owner's win picture paints the stage) and the hero jumping up onto it (fading in,
        /// rising a little and settling).
        /// </summary>
        private static void Stand(IPainter p, WinRegions r, Celebrant hero, Family family, float since)
        {
            if (!PaintedStage(p))
            {
                Kit.StonePedestal(p, r.Pedestal);
            }

            float up = Kit.Ease((since - 0.1f) / 0.45f);
            p.PushAlpha(up);
            p.PushTransform(0f, (1f - up) * hero.Picture.Height * 0.08f, 1f, 0f, 0f);
            if (hero.Cheer != null)
            {
                p.Mark(CharacterArt.CheerSlot(family));
                p.Sprite(hero.Cheer, hero.Picture);
            }
            else
            {
                Visuals.Group(p, hero.Picture);
            }

            p.PopTransform();
            p.PopAlpha();
        }

        /// <summary>
        /// The finished picture's box in <paramref name="area"/>: its stone frame's outer edge (<see cref="BoardPainter.Picture"/>:
        /// the grid, the gap and a 0.3 cell border), as large as fits, centered across and hanging from the area's top.
        /// </summary>
        private static Box PictureBox(Box area, BasePicture picture)
        {
            int w = Math.Max(1, picture.Width);
            int h = Math.Max(1, picture.Height);
            float rim = 2f * (BoardLayout.Gap + 0.3f);
            float cell = Math.Min(area.Width / (w + rim), area.Height / (h + rim));
            float width = cell * (w + rim);
            float height = cell * (h + rim);
            return new Box(area.CenterX - (width / 2f), area.Top, area.CenterX + (width / 2f), area.Top + height);
        }

        /// <summary>
        /// The wooden sign at the top (§3.2, §6.3): the plank filling the sign box, sliding down into place, the title in
        /// <c>ink.title</c> with the light emboss (in two lines, as the reference's "Level / Complete!", when one line would be
        /// small), and the flower clusters (<c>ui.sign.flowers</c>, the owner's picture D6 when it exists) on the plank's
        /// top corners, the right one mirrored and a little higher, as on the reference. The letters keep to the free middle
        /// between the clusters.
        /// </summary>
        private static void Sign(IPainter p, WinRegions r, string title, float since)
        {
            Box sign = r.Sign;
            float h = sign.Height;
            float drop = 1f - Kit.Ease(since / 0.45f);
            p.PushTransform(0f, -drop * sign.Bottom, 1f, 0f, 0f);

            p.Mark("ui.sign.wood");
            Kit.SoftShadow(p, sign, h * SignRadius, 0.22f, 0.07f);
            Kit.WoodPlank(p, sign, SignRadius, 7);
            TypeStyle style = T.LevelHome;
            TextLook look = GardenLook.SignLetters(C.InkTitle);
            float size = Math.Min(h * ClusterShare, r.W * ClusterMaxShare);
            float maxWidth = Math.Min(sign.Width * SignText, sign.Width - (size * 0.9f));
            float lineScale = h * SignLine / p.U(style.Size);
            List<string> lines = Lines(p, title, style, maxWidth / lineScale, 2);
            if (lines.Count > 1)
            {
                float gap = h * SignLine * 1.02f;
                for (int i = 0; i < lines.Count; i++)
                {
                    p.Text(lines[i], sign.CenterX, sign.CenterY - (h * 0.04f) + ((i - 0.5f) * gap), style, C.InkTitle, maxWidth, lineScale, look);
                }
            }
            else
            {
                float scale = Math.Min(h * SignSingle / p.U(style.Size), maxWidth / Math.Max(1f, p.MeasureText(title, style)));
                p.Text(title, sign.CenterX, sign.CenterY - (h * 0.04f), style, C.InkTitle, maxWidth, scale, look);
            }

            p.Mark("ui.sign.flowers");
            Kit.FlowerCluster(p, Box.FromCenter(sign.Left + (size * 0.04f), sign.Top + (h * 0.18f), size, size), flipped: false);
            var right = Box.FromCenter(sign.Right - (size * 0.04f), sign.Top + (h * 0.1f), size, size);
            p.PushSquash(-1f, 1f, right.CenterX, right.CenterY);
            Kit.FlowerCluster(p, right, flipped: false);
            p.PopTransform();
            p.PopTransform();
        }

        /// <summary>Pink petals drifting down over the whole screen (fx.petals), fading in.</summary>
        private static void Petals(IPainter p, WinRegions r, float since)
        {
            p.PushAlpha(Kit.Ease(since / 0.5f));
            Kit.FallingPetals(p, r.Safe, since + 3f);
            p.PopAlpha();
        }

        /// <summary>
        /// The cream Pause button in the top-left corner (<see cref="WinRegions.Pause"/>): the win shows no top bar
        /// (FR-023), but Pause stays usable over it (FR-016), so Home, Restart and Settings stay reachable. It takes taps
        /// once <paramref name="active"/>.
        /// </summary>
        private static void PauseButton(IPainter p, LevelScreen s, WinRegions r, bool active)
        {
            p.Mark("ui.pause");
            Box pause = r.Pause;
            Kit.RoundButton(p, pause.CenterX, pause.CenterY, pause.Width, "ui.pause", active ? () => s.App.OpenOverlay(Overlay.Pause) : (Action?)null, squircle: true);
        }

        /// <summary>
        /// The reward (§4.4) in <paramref name="pill"/>: the cream pill with the lotus and "+N" counting up from 0, a sparkle
        /// burst at the lotus, and petals bursting out around it.
        /// </summary>
        private static void RewardPill(IPainter p, Box pill, int petals, float since)
        {
            p.Mark("ui.pill.reward");
            float h = pill.Height;
            float scale = h * 0.56f / p.U(T.Count.Size);
            float icon = h * 0.86f;
            float gap = h * 0.16f;
            string amount = NumberText.Plus(GardenLook.CountUp(petals, since));
            Kit.CostPill(p, pill, Cost.Petals(petals), amount);

            // The lotus sits where the pill put it: left of the amount, the pair centered.
            float textWidth = Math.Min(p.MeasureText(amount, T.Count, scale), pill.Width - icon - gap - (h * 0.5f));
            float lotusX = pill.CenterX - ((icon + gap + textWidth) / 2f) + (icon / 2f);
            Kit.SparkleBurst(p, lotusX, pill.CenterY, icon * 1.3f, since);
            if (since >= 0f && since < 1.25f)
            {
                float k = Visuals.Clamp01(since / 1.2f);
                p.PushAlpha(1f - k);
                for (int i = 0; i < 6; i++)
                {
                    double a = i * Math.PI / 3;
                    float d = (pill.Width * 0.42f) + p.U(70f * k);
                    float size = p.U(34f) * (1f - (0.5f * k));
                    p.Shape("fx.petal_burst", Box.FromCenter(pill.CenterX + (float)(Math.Cos(a) * d), pill.CenterY + (float)(Math.Sin(a) * d * 0.5f), size, size), C.LotusFill);
                }

                p.PopAlpha();
            }
        }

        /// <summary>A booster charge dropped by the win, beside the reward pill: its icon on a cream tile with a green "+1" badge.</summary>
        private static void DroppedBooster(IPainter p, Box area, BoosterKind kind)
        {
            float tile = Math.Min(area.Width, area.Height);
            Box tileBox = Box.FromCenter(area.CenterX, area.CenterY, tile, tile);
            Box face = Kit.IconFace(p, tileBox, GardenLook.White, tile * 0.26f, 0f);
            Kit.BoosterIcon(p, IdOf(kind), Box.FromCenter(face.CenterX, face.CenterY, face.Width * 0.86f, face.Width * 0.86f));
            Kit.CountBadge(p, tileBox.Right - (tile * 0.1f), tileBox.Bottom - (tile * 0.1f), tile * 0.34f, NumberText.Plus(1));
        }

        /// <summary>
        /// The milestone's rewards in <paramref name="area"/> (frame 16): a parchment panel holding a row of cream tiles, each
        /// with its icon and its amount in a cream pill over the tile's bottom edge (<c>ui.pill.reward</c>).
        /// </summary>
        private static void Rewards(IPainter p, Box area, IReadOnlyList<(Action<Box> Icon, string Amount)> items)
        {
            p.Mark("ui.pill.reward");
            int n = Math.Max(1, items.Count);
            float pad = area.Width * 0.06f;
            float gap = area.Width * 0.05f;
            float tile = Math.Min(Math.Min((area.Width - (2f * pad) - ((n - 1) * gap)) / n, p.U(230f)), area.Height * 0.5f);
            float pillHeight = tile * 0.36f;
            float panelHeight = Math.Min(area.Height, tile + (pillHeight * 0.7f) + (2f * pad));
            float panelWidth = Math.Min(area.Width, (n * tile) + ((n - 1) * gap) + (2f * pad));
            Box panel = Box.FromCenter(area.CenterX, area.CenterY, panelWidth, panelHeight);
            Kit.Paper(p, panel, Math.Max(p.U(DesignTokens.Radius.CardMin), panel.Width * DesignTokens.Radius.Card), DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            float top = panel.Top + ((panel.Height - tile - (pillHeight * 0.7f)) / 2f);
            float left = panel.CenterX - (((n * tile) + ((n - 1) * gap)) / 2f);
            for (int i = 0; i < items.Count; i++)
            {
                (Action<Box> icon, string amount) = items[i];
                Box tileBox = new Box(left + (i * (tile + gap)), top, left + (i * (tile + gap)) + tile, top + tile);
                Box face = Kit.IconFace(p, tileBox, GardenLook.White, tile * 0.26f, 0f);
                icon(Box.FromCenter(face.CenterX, face.CenterY, face.Width * 0.92f, face.Width * 0.92f));
                float pillWidth = Math.Min(tile + gap, Math.Max(tile * 0.9f, p.MeasureText(amount, T.Count, pillHeight * 0.56f / p.U(T.Count.Size)) + (pillHeight * 1.1f)));
                Kit.CostPill(p, Box.FromCenter(tileBox.CenterX, tileBox.Bottom + (pillHeight * 0.2f), pillWidth, pillHeight), Cost.Charges(0), amount);
            }
        }

        /// <summary>
        /// A cream pill with the gold medal and "Milestone reached!", centered on (<paramref name="cx"/>, <paramref name="cy"/>)
        /// and <paramref name="h"/> tall: over the win picture's top edge on a milestone level, and on the milestone's
        /// pedestal, at most <paramref name="maxWidth"/> wide.
        /// </summary>
        private static void MilestonePill(IPainter p, float cx, float cy, float h, float maxWidth = float.MaxValue)
        {
            string text = PlaytestText.T("milestone.reached");
            float scale = h * 0.5f / p.U(T.Count.Size);
            float width = Math.Min(maxWidth, p.MeasureText(text, T.Count, scale) + (h * 1.9f));
            Box pill = Box.FromCenter(cx, cy, width, h);
            Kit.CostPill(p, pill, Cost.Charges(0), " ");
            Medal(p, Box.FromCenter(pill.Left + (h * 0.62f), pill.CenterY, h * 0.78f, h * 0.78f));
            float left = pill.Left + (h * 1.1f);
            float right = pill.Right - (h * 0.4f);
            p.Text(text, (left + right) / 2f, pill.CenterY, T.Count, C.InkBrown, right - left, scale, TextLook.Plain(C.InkBrown));
        }

        /// <summary>
        /// The gold medal (<c>ui.medal</c>, as the leaderboard's rank medals without a number): two ribbon tails in a deeper
        /// gold behind a gold disc, both outlined in dark gold, with a small white star on the disc.
        /// </summary>
        private static void Medal(IPainter p, Box box)
        {
            p.Mark("ui.medal");
            Rgba line = C.MedalGold.Darken(0.3f);
            Rgba ribbon = UiRaster.Vivid(C.MedalGold.Darken(0.1f), 1.35f);
            static float Segment(float x, float y, float ax, float ay, float bx, float by)
            {
                float dx = bx - ax;
                float dy = by - ay;
                float t = Math.Max(0f, Math.Min(1f, (((x - ax) * dx) + ((y - ay) * dy)) / ((dx * dx) + (dy * dy))));
                float ex = x - (ax + (t * dx));
                float ey = y - (ay + (t * dy));
                return (float)Math.Sqrt((ex * ex) + (ey * ey));
            }

            float Ribbons(float x, float y) => Math.Min(Segment(x, y, -0.32f, 0.88f, -0.05f, 0.3f) - 0.14f, Segment(x, y, 0.32f, 0.88f, 0.05f, 0.3f) - 0.14f);
            float Disc(float x, float y) => (float)Math.Sqrt((x * x) + ((y + 0.2f) * (y + 0.2f))) - 0.55f;
            Func<float, float, float> star = ShapeLibrary.Get("ui.star");
            p.ShapeOf("ui.medal/ribbons/line", (x, y) => Ribbons(x, y) - 0.06f, box, line);
            p.ShapeOf("ui.medal/ribbons", Ribbons, box, ribbon);
            p.ShapeOf("ui.medal/disc/line", (x, y) => Disc(x, y) - 0.06f, box, line);
            p.ShapeOf("ui.medal/disc", Disc, box, C.MedalGold);
            p.ShapeOf("ui.medal/light", (x, y) => Math.Max(Disc(x + 0.08f, y - 0.08f) + 0.1f, -Disc(x - 0.05f, y + 0.05f) - 0.35f), box, Rgba.White.WithAlpha(0.35f));
            p.ShapeOf("ui.medal/star", (x, y) => star(x / 0.3f, (y + 0.2f) / 0.3f) * 0.3f, box, Rgba.White.WithAlpha(0.92f));
        }

        // ---- The jam (spec 005 §4.3, §6.2) ----

        /// <summary>The color of a recovery's choice, as on the reference's jam card: Extra Slot and Shuffle green, Return and Bloom Burst blue.</summary>
        private static ColorSet ChoiceSet(string boosterId) => boosterId == "extra_slot" || boosterId == "shuffle" ? GardenLook.Green : GardenLook.Blue;

        private static string IdOf(BoosterKind kind)
        {
            foreach ((BoosterKind k, Recovery _, string id) in LevelScreen.Boosters)
            {
                if (k == kind)
                {
                    return id;
                }
            }

            return "extra_slot";
        }

        /// <summary>
        /// The bottom of the gameplay's top bar (§6.1, or the spec 002 bar it replaces, whichever is lower): the jam's scrim
        /// leaves the taps above it to Pause and the speed button.
        /// </summary>
        private static float TopBarBottom(IPainter p) => Math.Max(
            ScreenLayout.ReferenceGameplay(p.Width, p.Height, p.Insets, Array.Empty<EntrySide>(), 1, WaitingSlots.Capacity).TopBar.Bottom,
            ScreenLayout.Gameplay(p.Width, p.Height, p.Insets, false, true).TopBar.Bottom);

        /// <summary>
        /// A jam choice (§3.3, §6.2): the big glossy button in <paramref name="button"/> with the icon over the label, its cost
        /// pill in <paramref name="pill"/> hanging under its bottom edge (×N charges after the booster's icon, the lotus and
        /// the price, or ▶ Free), and one touch box over both.
        /// </summary>
        private static void Choice(IPainter p, Box button, Box pill, ColorSet set, IReadOnlyList<IconPart> icon, string label, Cost cost, Action action)
        {
            Kit.ChoiceButton(p, button, set, icon, label, null, action);
            Kit.CostPill(p, pill, cost, chargeIcon: icon);
            var both = new Box(Math.Min(button.Left, pill.Left), button.Top, Math.Max(button.Right, pill.Right), Math.Max(button.Bottom, pill.Bottom));
            p.Hit(Kit.Touch(p, both), action);
        }

        /// <summary>
        /// The Waiting Slots' contents in the card's inset well (§4.3, <c>ui.jam.slots</c>; <see cref="JamCardRegions.WellCell"/>):
        /// each slot's sticker tile with its count below it, a free slot as a small dashed plate, a locked one with its
        /// padlock.
        /// </summary>
        private static void SlotContents(IPainter p, JamCardRegions r, LevelScreen s)
        {
            p.Mark("ui.jam.slots");
            Kit.Well(p, r.Well, r.Well.Height * 0.2f);
            LevelView view = s.Session.View;
            var slots = new List<int>();
            for (int i = 0; i < view.SlotCapacity; i++)
            {
                if (view.SlotStateOf(i) != SlotState.Absent)
                {
                    slots.Add(i);
                }
            }

            for (int n = 0; n < slots.Count; n++)
            {
                int slot = slots[n];
                (Box tile, Box count) = r.WellCell(n, slots.Count);
                SlotLook look = s.Animator.Slots[slot];
                if (view.SlotStateOf(slot) == SlotState.Locked || s.Animator.HeldSlotLocks.Contains(slot))
                {
                    Kit.SlotPlate(p, tile, SlotPlateState.Locked);
                }
                else if (look.PodId == null)
                {
                    Kit.SlotPlate(p, tile, SlotPlateState.Empty);
                }
                else
                {
                    Kit.CandyTile(p, tile, look.Variant, TileStyle.Sticker);
                    Kit.CountBelow(p, count, look.Count, false);
                }
            }
        }

        /// <summary>
        /// A light sprinkle of confetti in the level's variant colors for the first seconds of a win (fx.confetti), falling
        /// through <paramref name="area"/> behind the celebration, so the picture, the reward and Next stay clean.
        /// </summary>
        private static void Confetti(IPainter p, LevelScreen s, Box area, float since)
        {
            if (since > 2.2f)
            {
                return;
            }

            p.Mark("fx.confetti");
            var colors = new List<Rgba>();
            foreach (PodDef pod in s.Session.Definition.Pods)
            {
                Rgba c = Visuals.ColorOf(pod.Variant);
                if (!colors.Contains(c))
                {
                    colors.Add(c);
                }
            }

            colors.Add(C.LotusFill);
            colors.Add(C.PetalCenter);
            for (int i = 0; i < 24; i++)
            {
                float seed = (i * 0.6180339f) % 1f;
                float x = area.Left + (area.Width * ((seed + (0.05f * (float)Math.Sin((since * 2f) + i))) % 1f));
                float y = area.Top - p.U(40f) + ((since * (area.Height * (0.25f + (0.2f * ((i * 0.37f) % 1f))))) % (area.Height + p.U(80f)));
                float size = p.U(14f + (8f * ((i * 0.53f) % 1f)));
                p.PushAlpha(Visuals.Clamp01(2.2f - since));
                p.FillRound(Box.FromCenter(x, y, size, size * 0.6f), p.U(3f), colors[i % colors.Count]);
                p.PopAlpha();
            }
        }
    }
}
