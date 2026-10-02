using System;
using System.Collections.Generic;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The cards at a level's end and before it (spec 002 FR-019 to FR-021) in the reference look of spec 005
    /// (<c>specs/005-reference-look/contracts/look.md</c> §4.3, §4.4):
    /// <list type="bullet">
    /// <item><description>the win (frame 15): the wooden "Level complete!" sign with flowers, the finished picture, the
    /// heroes on a stone pedestal in light rays and falling petals, the reward pill and Next;</description></item>
    /// <item><description>the milestone (frame 16) in the same language;</description></item>
    /// <item><description>the jam sheet (frame 10): the slot contents in a well and one big colored choice per recovery
    /// with its cost pill;</description></item>
    /// <item><description>the demo and unlock cards (frame 21).</description></item>
    /// </list>
    /// Positions and order stay as in spec 002; only the looks change.
    /// </summary>
    public static class EndCards
    {
        /// <summary>
        /// The win card: the wooden sign across its top edge, the finished picture, the Petals earned counting up in a
        /// cream pill, Next, and the optional ×2 reward (the playtest has no ads, so it shows as unavailable). The heroes
        /// celebrate on their pedestal above the card. The spec 001 sequence stays: reveal, then reward, then Next.
        /// </summary>
        public static void Win(IPainter p, LevelScreen s, float since)
        {
            LevelReward? reward = s.Payout?.Reward;
            bool drop = reward?.DroppedBooster != null;
            // The finished picture is a little smaller on a short phone, so the heroes keep their room above the card.
            float pictureUnits = Math.Max(400f, Math.Min(DesignTokens.Size.WinPictureHeight, ScreenLayout.SafeArea(p.Width, p.Height, p.Insets).Height / p.Scale * 0.24f));
            float content = pictureUnits + 26f + (reward != null ? RewardUnits : 0f) + (drop ? 64f : 0f) + 22f + DesignTokens.Size.CardPrimaryHeight + 26f + DesignTokens.Size.SecondaryHeight + 60f;
            CardRegions r = Kit.Card(p, content, string.Empty, null, Kit.Pop(since));
            Box sign = Header(p, r, PlaytestText.T("win.title"), since, celebrate: true, Visuals.MainFamily(s.Session.Definition));
            float y = r.Body.Top + p.U(10f);

            // The finished picture, its cells in full color in a thin stone frame (BoardPainter.Picture).
            p.Mark("fx.win_shine");
            var picture = new Box(r.Body.Left + p.U(16f), y, r.Body.Right - p.U(16f), y + p.U(pictureUnits));
            BoardPainter.Picture(p, picture, s.Session.Definition, s.Session.Picture);
            if (since < 1.2f)
            {
                // A light band sweeps the finished picture once.
                float band = picture.Left + ((picture.Width + p.U(200f)) * (since / 1.2f)) - p.U(100f);
                p.PushClip(picture);
                p.Line(band - p.U(60f), picture.Bottom, band + p.U(60f), picture.Top, p.U(70f), Rgba.White.WithAlpha(0.3f));
                p.PopClip();
            }

            if (s.Payout?.Milestone != null)
            {
                MilestoneMark(p, picture);
            }

            y = picture.Bottom + p.U(26f);

            // The reward rises in after the reveal (motion.reward).
            float rise = Kit.Ease((since - 0.15f) / DesignTokens.Motion.Reward.Seconds);
            p.PushAlpha(rise);
            p.PushTransform(0f, (1f - rise) * p.U(30f), 1f, 0f, 0f);
            if (reward != null)
            {
                // The earned Petals count up from 0 in a big cream pill with the lotus (spec 003 FR-020); Next works at once.
                RewardPill(p, r.Body.CenterX, y + (p.U(RewardUnits) / 2f), reward.Petals, since - 0.15f);
                y += p.U(RewardUnits);
                if (reward.DroppedBooster.HasValue)
                {
                    DroppedBooster(p, r.Body.CenterX, y + p.U(32f), reward.DroppedBooster.Value);
                    y += p.U(64f);
                }
            }

            p.PopTransform();
            p.PopAlpha();

            Box next = ScreenLayout.CardButton(r.Body, y + p.U(22f), true, p.Scale);
            Kit.PrimaryButton(p, next, PlaytestText.T("common.next"), s.Next, decorate: true, breathe: true);
            Box twice = ScreenLayout.CardButton(r.Body, next.Bottom + p.U(26f), false, p.Scale).Inset(p.U(50f), 0f);
            Kit.SecondaryButton(p, twice, PlaytestText.T("win.double"), null, "ui.ad");
            p.Text(PlaytestText.T("win.no_ads"), r.Body.CenterX, twice.Bottom + p.U(34f), T.Caption, C.InkBrownSoft, r.Body.Width);
            Kit.EndCard(p);

            Petals(p, r, sign, since);
            Confetti(p, s, r.Card.Top, since);
        }

        /// <summary>
        /// The milestone card in the win's language: the wooden sign with "Level N", "Milestone reached!", each reward on
        /// a cream tile with its amount in a cream pill, and Continue. The heroes celebrate above it.
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

            const float rowUnits = 270f;
            CardRegions r = Kit.Card(p, 64f + rowUnits + 50f + DesignTokens.Size.CardPrimaryHeight + 40f, string.Empty, null, Kit.Pop(since));
            Box sign = Header(p, r, PlaytestText.F("common.level", NumberText.Group(grant.Level)), since, celebrate: true, Visuals.MainFamily(s.Session.Definition));
            p.Text(PlaytestText.T("milestone.reached"), r.Body.CenterX, r.Body.Top + p.U(30f), T.ButtonSecondary, C.InkBrownSoft, r.Body.Width, look: TextLook.Plain(C.InkBrownSoft));

            // Each reward: its icon on a cream tile, the amount in a cream pill over the tile's bottom edge.
            p.Mark("ui.pill.reward");
            var row = new Box(r.Body.Left, r.Body.Top + p.U(84f), r.Body.Right, r.Body.Top + p.U(84f + rowUnits));
            Box[] cells = ScreenLayout.Row(row, Math.Max(1, items.Count), p.U(36f), p.U(230f), square: false);
            float rise = Kit.Ease((since - 0.15f) / DesignTokens.Motion.Reward.Seconds);
            p.PushAlpha(rise);
            p.PushTransform(0f, (1f - rise) * p.U(30f), 1f, 0f, 0f);
            for (int i = 0; i < items.Count; i++)
            {
                (Action<Box> icon, string amount) = items[i];
                Box cell = cells[i];
                float tile = Math.Min(cell.Width * 0.86f, p.U(190f));
                Box tileBox = Box.FromCenter(cell.CenterX, cell.Top + (tile / 2f), tile, tile);
                Box face = Kit.IconFace(p, tileBox, GardenLook.White, tile * 0.26f, 0f);
                icon(Box.FromCenter(face.CenterX, face.CenterY, face.Width * 0.92f, face.Width * 0.92f));
                float pillHeight = tile * 0.36f;
                float pillWidth = Math.Min(cell.Width, Math.Max(tile * 0.9f, p.MeasureText(amount, T.Count, pillHeight * 0.56f / p.U(T.Count.Size)) + (pillHeight * 1.1f)));
                Kit.CostPill(p, Box.FromCenter(tileBox.CenterX, tileBox.Bottom + (pillHeight * 0.2f), pillWidth, pillHeight), Cost.Charges(0), amount);
            }

            p.PopTransform();
            p.PopAlpha();

            Box go = ScreenLayout.CardButton(r.Body, r.Body.Bottom - p.U(DesignTokens.Size.CardPrimaryHeight) - p.U(20f), true, p.Scale);
            Kit.PrimaryButton(p, go, PlaytestText.T("milestone.continue"), s.Next, decorate: true, breathe: true);
            Kit.EndCard(p);
            Petals(p, r, sign, since);
            Confetti(p, s, r.Card.Top, since);
        }

        /// <summary>
        /// The jam bottom sheet (frame 10): "No more space!" and its subtitle, the Waiting Slots' contents in an inset
        /// well, one big colored choice per recovery the player can use now (green Extra Slot and Shuffle, blue Return and
        /// Bloom Burst) with its cost pill (×N charges, or the lotus and the price), the free rescue once per attempt (▶
        /// Free), and Restart. The board stays visible above it (spec 001 FR-027).
        /// </summary>
        public static void Jam(IPainter p, LevelScreen s, float since)
        {
            var choices = new List<(string Id, ColorSet Set, string Label, Cost Cost, Action Action)>();
            foreach (Recovery recovery in s.Session.EligibleRecoveries())
            {
                foreach ((BoosterKind kind, Recovery r, string id) in LevelScreen.Boosters)
                {
                    if (r == recovery && s.Meta.Economy.IsUnlocked(kind) && s.Meta.Economy.CanAfford(kind))
                    {
                        int charges = s.Meta.Economy.Charges(kind);
                        Cost cost = charges > 0 ? Cost.Charges(charges) : Cost.Petals(s.Meta.Economy.Price(kind));
                        BoosterKind used = kind;
                        Recovery chosen = r;
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

            int columns = choices.Count <= 3 ? Math.Max(1, choices.Count) : choices.Count == 4 ? 2 : 3;
            int rows = (choices.Count + columns - 1) / columns;
            bool stuck = s.Session.Status == LevelStatus.Stuck;
            string title = PlaytestText.T(stuck ? "jam.stuck" : "jam.title");
            float subtitleWidth = ScreenLayout.Sheet(p.Width, p.Height, p.Insets, 0f).Subtitle.Width;
            List<string> subtitle = Lines(p, PlaytestText.T(stuck ? "jam.stuck_subtitle" : "jam.subtitle"), T.Body, subtitleWidth * 0.94f, 2);

            // Wanted heights (units): the subtitle's second line, the well, the rows of choices (a button and its pill's
            // overhang), Restart. A short phone shrinks the well, the choices and the gaps together.
            float lineUnits = subtitle.Count > 1 ? 50f : 0f;
            const float wellUnits = 196f;
            float rowUnits = columns <= 2 ? 236f : 222f;
            const float rowGap = 30f;
            const float gap = 34f;
            float flexible = wellUnits + gap + (rows * rowUnits) + (Math.Max(0, rows - 1) * rowGap) + (rows > 0 ? gap : 0f);
            // Restart is as big as a card's main button, as on the reference's jam card.
            float fixedUnits = lineUnits + DesignTokens.Size.CardPrimaryHeight + 34f;
            SheetRegions sheet = Kit.Sheet(p, flexible + fixedUnits + 10f, title, subtitle[0], Kit.SheetRise(since));
            float k = Math.Max(0.62f, Math.Min(1f, ((sheet.Body.Height / p.Scale) - fixedUnits) / flexible));
            float y = sheet.Body.Top;
            if (subtitle.Count > 1)
            {
                p.Text(subtitle[1], sheet.Subtitle.CenterX, sheet.Subtitle.CenterY + p.U(48f), T.Body, C.InkBrownSoft, sheet.Subtitle.Width);
                y += p.U(lineUnits);
            }

            // The slots' contents, as in the reference's inset row.
            var well = new Box(sheet.Body.Left + p.U(14f), y, sheet.Body.Right - p.U(14f), y + (p.U(wellUnits) * k));
            SlotContents(p, well, s);
            y = well.Bottom + (p.U(gap) * k);

            float rowHeight = p.U(rowUnits) * k;
            float gapX = p.U(30f);
            float cellWidth = Math.Min(p.U(columns <= 2 ? 440f : 300f), (sheet.Body.Width - (gapX * (columns - 1))) / columns);
            for (int row = 0; row < rows; row++)
            {
                int first = row * columns;
                int inRow = Math.Min(columns, choices.Count - first);
                Box[] cells = ScreenLayout.Row(new Box(sheet.Body.Left, y, sheet.Body.Right, y + rowHeight), inRow, gapX, cellWidth, square: false);
                for (int i = 0; i < inRow; i++)
                {
                    (string id, ColorSet set, string label, Cost cost, Action action) = choices[first + i];
                    Kit.ChoiceButton(p, cells[i], set, GardenLook.BoosterIcon(id), label, cost, action);
                }

                y += rowHeight + (p.U(row < rows - 1 ? rowGap : gap) * k);
            }

            Box restart = ScreenLayout.CardButton(sheet.Body, y + p.U(8f), true, p.Scale);
            Kit.SecondaryButton(p, restart, PlaytestText.T("common.restart"), s.Restart, "ui.restart", T.Button);
            Kit.EndSheet(p);
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

        // ---- The celebration (spec 005 §4.4) ----

        private const float RewardUnits = 118f;

        /// <summary>
        /// The card's header: a wooden sign with white flower clusters across the card's top edge (§3.2), and, when
        /// <paramref name="celebrate"/>, the heroes on their stone pedestal in the light rays above it. Returns the sign.
        /// </summary>
        private static Box Header(IPainter p, CardRegions r, string title, float since, bool celebrate, Family family = Family.Bloom)
        {
            float h = p.U(DesignTokens.Size.WinSignHeight);
            TypeStyle style = T.LevelHome;
            float width = Math.Min(r.Card.Width * 0.8f, p.MeasureText(title, style) + (h * 1.5f));
            Box sign = Box.FromCenter(r.Card.CenterX, r.Card.Top + p.U(30f), width, h);
            if (celebrate)
            {
                Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
                Celebration(p, new Box(r.Card.Left, safe.Top + p.U(12f), r.Card.Right, sign.Top + (h * 0.3f)), r.Card.Top, since, family);
            }

            Kit.WoodSign(p, sign, title, style, SignDecor.Flowers);
            return sign;
        }

        /// <summary>
        /// The heroes celebrating in <paramref name="stage"/> (spec 004 FR-017, spec 005 §4.4,
        /// <see cref="HomeStage.Celebration"/>): slowly turning light rays behind them (above the card's top edge only), a
        /// stone pedestal, and on it the celebrating hero of the level's main family when the owner's picture exists
        /// (pictures.md A7), else the four 3D heroes. On a phone too short for them only the rays show, so the card never
        /// moves.
        /// </summary>
        private static void Celebration(IPainter p, Box stage, float cardTop, float since, Family family)
        {
            if (stage.Height < p.U(150f))
            {
                return;
            }

            (Box pedestal, Box group, float raysX, float raysY) = HomeStage.Celebration(stage);
            float rays = 0.85f * Kit.Ease(since / 0.6f);
            p.PushClip(new Box(0f, 0f, p.Width, cardTop));
            p.PushAlpha(rays);
            Kit.LightRays(p, raysX, raysY, Math.Max(p.Width * 0.62f, stage.Height), since);
            p.PopAlpha();
            p.PopClip();
            Kit.StonePedestal(p, pedestal);
            string cheer = CharacterArt.Cheer(family);
            if (p.HasSprite(cheer))
            {
                // One celebrating hero, as on the reference's win card: its feet where the group's stand.
                p.Mark(CharacterArt.CheerSlot(family));
                float feet = group.Top + (group.Height * CharacterArt.GroupFeetShare);
                float height = Math.Min((feet - stage.Top) / HomeStage.FeetShare, pedestal.Width * 0.75f * CharacterArt.HeroHeight / CharacterArt.HeroWidth);
                p.Sprite(cheer, HomeStage.Figure(stage.CenterX, feet, height));
                return;
            }

            Visuals.Group(p, group);
        }

        /// <summary>Pink petals drifting down around the heroes and over the card's top (fx.petals).</summary>
        private static void Petals(IPainter p, CardRegions r, Box sign, float since)
        {
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            p.PushAlpha(Kit.Ease(since / 0.5f));
            Kit.FallingPetals(p, new Box(safe.Left, safe.Top, safe.Right, Math.Min(r.Card.Bottom, sign.Bottom + p.U(260f))), since + 3f);
            p.PopAlpha();
        }

        /// <summary>
        /// The reward (§4.4): a big cream pill with the lotus and "+N" counting up from 0, a sparkle burst at the lotus,
        /// and petals bursting out around it. Returns the pill.
        /// </summary>
        private static Box RewardPill(IPainter p, float cx, float cy, int petals, float since)
        {
            p.Mark("ui.pill.reward");
            float h = p.U(DesignTokens.Size.RewardPillHeight);
            float scale = h * 0.56f / p.U(T.Count.Size);
            string final = NumberText.Plus(petals);
            float icon = h * 0.86f;
            float gap = h * 0.16f;
            float width = Math.Max(p.U(320f), p.MeasureText(final, T.Count, scale) + icon + gap + (h * 1.1f));
            Box pill = Box.FromCenter(cx, cy, width, h);
            string amount = NumberText.Plus(GardenLook.CountUp(petals, since));
            Kit.CostPill(p, pill, Cost.Petals(petals), amount);

            // The lotus sits where the pill put it: left of the amount, the pair centered.
            float textWidth = Math.Min(p.MeasureText(amount, T.Count, scale), width - icon - gap - (h * 0.5f));
            float lotusX = cx - ((icon + gap + textWidth) / 2f) + (icon / 2f);
            Kit.SparkleBurst(p, lotusX, cy, icon * 1.3f, since);
            if (since >= 0f && since < 1.25f)
            {
                // Petals burst out around the reward.
                float k = Visuals.Clamp01(since / 1.2f);
                p.PushAlpha(1f - k);
                for (int i = 0; i < 6; i++)
                {
                    double a = i * Math.PI / 3;
                    float d = (width * 0.42f) + p.U(70f * k);
                    float size = p.U(34f) * (1f - (0.5f * k));
                    p.Shape("fx.petal_burst", Box.FromCenter(cx + (float)(Math.Cos(a) * d), cy + (float)(Math.Sin(a) * d * 0.5f), size, size), C.LotusFill);
                }

                p.PopAlpha();
            }

            return pill;
        }

        /// <summary>A booster charge dropped by the win: its icon and "+1 Extra Slot" in brown.</summary>
        private static void DroppedBooster(IPainter p, float cx, float cy, BoosterKind kind)
        {
            string text = PlaytestText.F("win.drop", BoosterName(kind));
            float icon = p.U(56f);
            float gap = p.U(12f);
            float width = p.MeasureText(text, T.ButtonSecondary, 0.8f);
            float start = cx - ((icon + gap + width) / 2f);
            Kit.BoosterIcon(p, IdOf(kind), Box.FromCenter(start + (icon / 2f), cy, icon, icon));
            p.Text(text, start + icon + gap + (width / 2f), cy, T.ButtonSecondary, C.InkBrownSoft, width + p.U(4f), 0.8f, TextLook.Plain(C.InkBrownSoft));
        }

        /// <summary>The milestone mark over the finished picture's top edge: a cream pill with the gold medal and its text.</summary>
        private static void MilestoneMark(IPainter p, Box picture)
        {
            string text = PlaytestText.T("milestone.reached");
            float h = p.U(70f);
            float scale = h * 0.56f / p.U(T.Count.Size);
            float width = p.MeasureText(text, T.Count, scale) + (h * 1.9f);
            Box pill = Box.FromCenter(picture.CenterX, picture.Top, width, h);
            Kit.CostPill(p, pill, Cost.Charges(0), text);
            p.Shape("ui.medal", Box.FromCenter(pill.Left + (h * 0.62f), pill.CenterY, h * 0.8f, h * 0.8f), C.MedalGold);
        }

        // ---- The jam (spec 005 §4.3) ----

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
        /// The Waiting Slots' contents in an inset well (§4.3): each slot's sticker tile with the count below it, a free
        /// slot as a small dashed plate, a locked one with its padlock.
        /// </summary>
        private static void SlotContents(IPainter p, Box well, LevelScreen s)
        {
            p.Mark("ui.jam.slots");
            Kit.Well(p, well, Math.Min(p.U(36f), well.Height * 0.2f));
            LevelView view = s.Session.View;
            var slots = new List<int>();
            for (int i = 0; i < view.SlotCapacity; i++)
            {
                if (view.SlotStateOf(i) != SlotState.Absent)
                {
                    slots.Add(i);
                }
            }

            if (slots.Count == 0)
            {
                return;
            }

            float tile = Math.Min(well.Height * 0.54f, p.U(112f));
            float cell = Math.Min(tile * 1.62f, (well.Width - p.U(24f)) / slots.Count);
            Box[] cells = ScreenLayout.Row(well.Inset(p.U(12f), 0f), slots.Count, 0f, cell, square: false);
            float top = well.Top + ((well.Height - (tile * 1.5f)) / 2f);
            for (int n = 0; n < slots.Count; n++)
            {
                int slot = slots[n];
                Box tileBox = Box.FromCenter(cells[n].CenterX, top + (tile / 2f), tile, tile);
                SlotLook look = s.Animator.Slots[slot];
                if (view.SlotStateOf(slot) == SlotState.Locked || s.Animator.HeldSlotLocks.Contains(slot))
                {
                    Kit.SlotPlate(p, tileBox, SlotPlateState.Locked);
                }
                else if (look.PodId == null)
                {
                    Kit.SlotPlate(p, tileBox, SlotPlateState.Empty);
                }
                else
                {
                    Kit.CandyTile(p, tileBox, look.Variant, TileStyle.Sticker);
                    Kit.CountBelow(p, new Box(tileBox.Left - (tile * 0.2f), tileBox.Bottom + (tile * 0.06f), tileBox.Right + (tile * 0.2f), tileBox.Bottom + (tile * 0.5f)), look.Count, false);
                }
            }
        }

        /// <summary>
        /// A light sprinkle of confetti in the level's variant colors for the first seconds of a win (fx.confetti), only in
        /// the heroes' room above the card (above <paramref name="cardTop"/>), so the picture, the reward and Next stay
        /// clean.
        /// </summary>
        private static void Confetti(IPainter p, LevelScreen s, float cardTop, float since)
        {
            if (since > 2.2f)
            {
                return;
            }

            p.Mark("fx.confetti");
            p.PushClip(new Box(0f, 0f, p.Width, cardTop));
            var colors = new List<Rgba>();
            foreach (Core.Definitions.PodDef pod in s.Session.Definition.Pods)
            {
                Rgba c = Visuals.ColorOf(pod.Variant);
                if (!colors.Contains(c))
                {
                    colors.Add(c);
                }
            }

            colors.Add(C.LotusFill);
            colors.Add(C.PetalCenter);
            for (int i = 0; i < 18; i++)
            {
                float seed = (i * 0.6180339f) % 1f;
                float x = p.Width * ((seed + (0.05f * (float)Math.Sin((since * 2f) + i))) % 1f);
                float y = (-p.U(40f)) + ((since * (cardTop * (0.35f + (0.25f * ((i * 0.37f) % 1f))))) % (cardTop + p.U(80f)));
                float size = p.U(14f + (8f * ((i * 0.53f) % 1f)));
                p.PushAlpha(Visuals.Clamp01(2.2f - since));
                p.FillRound(Box.FromCenter(x, y, size, size * 0.6f), p.U(3f), colors[i % colors.Count]);
                p.PopAlpha();
            }

            p.PopClip();
        }
    }
}
