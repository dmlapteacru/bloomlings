using System;
using System.Collections.Generic;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Whether an introduction card explains a mechanic (its first meeting) or reminds of it (its second).</summary>
    public enum RoundIntroKind
    {
        /// <summary>The first meeting (L6 hidden pods, L11 links, L16 the bridge, L61 the latch, L101 the channel, L121 the shutters): the explanation card.</summary>
        Explanation,

        /// <summary>The second meeting (L8, L13, L18, L63, L103, L123): a short reminder card.</summary>
        Reminder,
    }

    /// <summary>What an introduction card shows above its lines (the host draws it from the kit's pieces).</summary>
    public enum RoundIntroPicture
    {
        /// <summary>A pod with "?" beside the same pod revealed (and glowing, <see cref="RoundLook.RevealGlow"/>).</summary>
        HiddenPod,

        /// <summary>Two pods side by side joined by the link tube (<see cref="LinkTube"/>), each half in its pod's colour.</summary>
        LinkedPair,

        /// <summary>A strip of stream with a plank across it (<see cref="UiRaster.Water"/>, the bridge's plank).</summary>
        Bridge,

        /// <summary>
        /// The hero of the held pod's colour holding the padlock in that colour in the pod's place (<see cref="RoundLook.HeroCell"/>) beside
        /// a patch of the board with its key standing upright and turning over its cell between cubes
        /// (<see cref="UiRaster.UprightKey"/>; 17 §8, the owner's look of 2026-11-02, laid out by <see cref="RoundIntroLatch"/>); in
        /// a loop (<see cref="RoundIntroLatch.Since"/>) the key flies to the keyhole, the padlock opens and the hero steps aside from
        /// the pod.
        /// </summary>
        Latch,

        /// <summary>
        /// A patch of the board with the water channel (17 §8 «Drop Водный канал», FR-069; the owner's look A of 2026-11-05, laid out
        /// by <see cref="RoundIntroChannel"/>): the spring, a stream between cubes and an exit; in a loop the exits call, one is
        /// chosen, the water runs to it and a walker's bubble rides it.
        /// </summary>
        Channel,

        /// <summary>
        /// A patch of the board with a crate lid over cubes and its tag (17 §8 «Twig Деревянные створки», FR-070; the owner's look B
        /// of 2026-11-05, laid out by <see cref="RoundIntroShutters"/>): in a loop the tag counts down and the slats pop off.
        /// </summary>
        Shutters,
    }

    /// <summary>
    /// The channel card's picture (<see cref="RoundIntroPicture.Channel"/>), the same in both builds: a small board of its own
    /// (<see cref="Board"/>, only its surface and cubes drawn) with a spring at its left, two exits and their routes, laid out
    /// in the card's area; <see cref="Pose"/> and <see cref="Bubble"/> play its loop.
    /// </summary>
    public sealed class RoundIntroChannel
    {
        /// <summary>One loop of the card's picture, in seconds.</summary>
        public const float LoopSeconds = 4f;

        /// <summary>When in the loop the right exit is chosen.</summary>
        public const float ChooseAt = 1.4f;

        /// <summary>When in the loop the walker's bubble starts its ride, and how long it rides.</summary>
        public const float RideAt = 2.5f;

        public const float RideSeconds = 1f;

        private static readonly string[] Rows =
        {
            "###########",
            "#GGGGGGGG.#",
            "#.........#",
            "#BBBBBBBBB#",
            "###########",
        };

        private RoundIntroChannel(RoundBoardLayout board, RoundChannel channel)
        {
            Board = board;
            Look = ChannelLook.For(board, channel)!;
        }

        /// <summary>The picture's small board (its <see cref="RoundBoardLayout.Surface"/> and cubes are drawn).</summary>
        public RoundBoardLayout Board { get; }

        public ChannelLook Look { get; }

        /// <summary>The picture laid out in <paramref name="area"/>.</summary>
        public static RoundIntroChannel In(Box area)
        {
            var rows = new List<string>(Rows);
            rows.Reverse();
            int w = Rows[0].Length;
            int at(int x, int yTop) => ((Rows.Length - 1 - yTop) * w) + x;
            var straight = new List<int>();
            for (int x = 1; x <= 9; x++)
            {
                straight.Add(at(x, 2));
            }

            var up = new List<int>();
            for (int x = 1; x <= 8; x++)
            {
                up.Add(at(x, 2));
            }

            up.Add(at(9, 2));
            up.Add(at(9, 1));
            var channel = new RoundChannel(at(1, 2), new[] { new RoundChannelExit(at(9, 1), up, 1000), new RoundChannelExit(at(9, 2), straight, 1000) });
            float cell = Math.Min(area.Width / (w + 1f), area.Height / 4.6f);
            var box = Box.FromCenter(area.CenterX, area.CenterY, cell * (w + 1.4f), cell * 4.4f);
            RoundBoardLayout board = RoundBoardLayout.Fit(box, w, Rows.Length, rows, Array.Empty<int>(), null);
            return new RoundIntroChannel(board, channel);
        }

        /// <summary>The material of the cubes at <paramref name="cell"/>, or -1.</summary>
        public int MaterialOf(int cell) => Board.MaterialOf(cell);

        /// <summary>The channel's look <paramref name="seconds"/> into the card (its loop): calling, then the straight exit chosen.</summary>
        public static ChannelPose Pose(float seconds)
        {
            float t = seconds % LoopSeconds;
            if (t < ChooseAt)
            {
                return new ChannelPose(Math.Min(1f, t / ChannelLook.AwakeSeconds), t > 0.3f, RoundBoardLayout.BridgePulse(t), -1, 0f, -1f);
            }

            float since = t - ChooseAt;
            float k = Math.Min(1f, since / ChannelLook.RunSeconds);
            return new ChannelPose(1f, false, 0f, 1, k * k * (3f - (2f * k)), since);
        }

        /// <summary>The walker's bubble <paramref name="seconds"/> into the card, while it rides the chosen route.</summary>
        public bool Bubble(float seconds, out Box bubble)
        {
            float t = seconds % LoopSeconds;
            bubble = default;
            if (t < RideAt || t >= RideAt + RideSeconds)
            {
                return false;
            }

            float k = (t - RideAt) / RideSeconds;
            ChannelPath path = Look.RouteCells(1);
            (float x, float y) = Look.Screen(path.PointAt(path.Length * k * k * (3f - (2f * k))));
            float d = Board.Cell * ChannelLook.BubbleCells * (0.55f + (0.45f * Math.Min(1f, Math.Min(k, 1f - k) / 0.12f)));
            bubble = Box.FromCenter(x, y - (Board.Cell * 0.12f), d, d);
            return true;
        }

        /// <summary>The bubble's material (a Drop's water blue).</summary>
        public const int BubbleMaterial = 2;
    }

    /// <summary>
    /// The shutters card's picture (<see cref="RoundIntroPicture.Shutters"/>), the same in both builds: a small board of its own
    /// with a crate lid over a block of cubes counting the green ones round it (<see cref="Look"/>); in a loop the tag counts
    /// down from <see cref="Count"/> as the green cubes go, then the slats pop off.
    /// </summary>
    public sealed class RoundIntroShutters
    {
        /// <summary>One loop of the card's picture, in seconds.</summary>
        public const float LoopSeconds = 4f;

        /// <summary>The tag's count at the loop's start.</summary>
        public const int Count = 4;

        /// <summary>When the tag counts each cube (seconds into the loop).</summary>
        public const float FirstCount = 0.6f;

        public const float CountStep = 0.45f;

        private static readonly string[] Rows =
        {
            "#########",
            "#GGGGGGG#",
            "#GYYYYYG#",
            "#GYYYYYG#",
            "#GGGGGGG#",
            "#########",
        };

        private RoundIntroShutters(RoundBoardLayout board, ShutterLook look, int[] counted)
        {
            Board = board;
            Look = look;
            Counted = counted;
        }

        /// <summary>The picture's small board (its surface and cubes are drawn).</summary>
        public RoundBoardLayout Board { get; }

        public ShutterLook Look { get; }

        /// <summary>The green cubes the tag counts, in the order they go.</summary>
        public IReadOnlyList<int> Counted { get; }

        /// <summary>The picture laid out in <paramref name="area"/>; <paramref name="tagMin"/> the tag's least size.</summary>
        public static RoundIntroShutters In(Box area, float tagMin)
        {
            var rows = new List<string>(Rows);
            rows.Reverse();
            int w = Rows[0].Length;
            int at(int x, int yTop) => ((Rows.Length - 1 - yTop) * w) + x;
            var cells = new List<int>();
            for (int y = 2; y <= 3; y++)
            {
                for (int x = 2; x <= 6; x++)
                {
                    cells.Add(at(x, y));
                }
            }

            cells.Sort();
            var shutter = new RoundShutter(cells, 'G', Count);
            float cell = Math.Min(area.Width / (w + 1f), area.Height / 5.6f);
            var box = Box.FromCenter(area.CenterX, area.CenterY, cell * (w + 1.2f), cell * 5.4f);
            RoundBoardLayout board = RoundBoardLayout.Fit(box, w, Rows.Length, rows, Array.Empty<int>(), null);
            int[] counted = { at(1, 4), at(2, 4), at(3, 4), at(4, 4) };
            return new RoundIntroShutters(board, ShutterLook.Of(board, shutter, 0, tagMin), counted);
        }

        /// <summary>How many of <see cref="Counted"/> are gone <paramref name="seconds"/> into the card.</summary>
        public static int Gone(float seconds)
        {
            float t = seconds % LoopSeconds;
            return t < FirstCount ? 0 : Math.Min(Count, 1 + (int)((t - FirstCount) / CountStep));
        }

        /// <summary>Seconds since the last count (negative before the first).</summary>
        public static float SinceCount(float seconds)
        {
            float t = seconds % LoopSeconds;
            int gone = Gone(seconds);
            return gone == 0 ? -1f : t - (FirstCount + ((gone - 1) * CountStep));
        }

        /// <summary>The opening's share <paramref name="seconds"/> into the card (0 while closed).</summary>
        public static float Open(float seconds)
        {
            float t = seconds % LoopSeconds;
            float at = FirstCount + ((Count - 1) * CountStep) + 0.3f;
            return ShutterLook.OpenShare(t - at);
        }
    }

    /// <summary>
    /// The latch card's picture (<see cref="RoundIntroPicture.Latch"/>), the same in both builds: the held pod's place at the
    /// left with the hero of its colour and his padlock in it, a patch of the board at the right with cubes round an empty cell in its
    /// bottom row and the upright key over it, in the pod's colour.
    /// </summary>
    public sealed class RoundIntroLatch
    {
        /// <summary>The locked pod's material (the key's colour).</summary>
        public const int Material = 2;

        /// <summary>The locked pod's number.</summary>
        public const int Count = 9;

        /// <summary>One loop of the card's picture, in seconds: the latch closed for <see cref="ClosedSeconds"/>, then its opening and a pause.</summary>
        public const float LoopSeconds = 3f;

        /// <summary>How long the loop shows the latch closed, the key turning over its cell.</summary>
        public const float ClosedSeconds = 1.5f;

        /// <summary>
        /// Seconds since the loop's opening began at <paramref name="seconds"/> of the card (negative while closed): the
        /// <c>since</c> of <see cref="RoundLook.KeyFlight"/>, <see cref="RoundLook.PadlockOpen"/> and <see cref="RoundLook.HeroStep"/>.
        /// </summary>
        public static float Since(float seconds) => (seconds % LoopSeconds) - ClosedSeconds;

        private RoundIntroLatch(Box pod, Box patch, float cell, IReadOnlyList<(Box Cube, int Material)> cubes, Box keyCell)
        {
            Pod = pod;
            Patch = patch;
            Cell = cell;
            Cubes = cubes;
            KeyCell = keyCell;
        }

        /// <summary>The held pod's place (the hero's is <see cref="RoundLook.HeroCell"/> of it, the pod's cube <see cref="PodChip.Cube"/>).</summary>
        public Box Pod { get; }

        /// <summary>The patch of board surface.</summary>
        public Box Patch { get; }

        /// <summary>The patch's cell width.</summary>
        public float Cell { get; }

        /// <summary>The cubes on the patch (picture boxes, top face and front) with their materials, back rows first.</summary>
        public IReadOnlyList<(Box Cube, int Material)> Cubes { get; }

        /// <summary>The key's empty cell (its top face's box).</summary>
        public Box KeyCell { get; }

        /// <summary>The upright key's picture box over <see cref="KeyCell"/> (<see cref="RoundLook.KeyBox"/>).</summary>
        public Box KeyBox => RoundLook.KeyBox(KeyCell);

        /// <summary>The hero of the pod's colour holding its padlock (<see cref="RoundLook.LatchFamily"/>).</summary>
        public static Family Hero => RoundLook.LatchFamily(Material);

        /// <summary>The padlock's square in the hero's hands (<see cref="RoundLook.HeroLock"/>).</summary>
        public Box Lock => RoundLook.HeroLock(RoundLook.HeroCell(Pod), Hero);

        /// <summary>The picture laid out in <paramref name="area"/>.</summary>
        public static RoundIntroLatch In(Box area)
        {
            float h = area.Height;
            float pw = h * 0.7f;
            var pod = Box.FromCenter(area.Left + (area.Width * 0.25f), area.CenterY + (h * 0.12f), pw, pw);
            float cell = Math.Min(h * 0.2f, area.Width * 0.1f);
            BoardCubeLook look = BoardCubeLook.Default;
            float row = cell * look.RowShare;
            const int cols = 5;
            const int rows = 3;
            float px = area.Left + (area.Width * 0.66f);
            var patch = Box.FromCenter(px, area.CenterY + (h * 0.12f), (cols * cell) + (cell * 0.5f), (rows * row) + (cell * look.Overhang) + (cell * 0.5f));
            float left = patch.CenterX - (cols * cell / 2f);
            float top = patch.Top + (cell * 0.25f);
            var cubes = new List<(Box, int)>();
            int[] materials = { 0, 8, 0, 4, 8 };
            Box key = default;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var box = new Box(left + (x * cell), top + (y * row), left + ((x + 1) * cell), top + ((y + 1) * row));
                    if (y == rows - 1 && x == 2)
                    {
                        key = box;
                        continue;
                    }

                    // the bottom row's cubes left of the key are cleared: the way the Bloomlings came
                    if (y == rows - 1 && (x == 1 || x == 0))
                    {
                        continue;
                    }

                    cubes.Add((look.PictureBox(box, cell), materials[(x + (y * 2)) % materials.Length]));
                }
            }

            return new RoundIntroLatch(pod, patch, cell, cubes, key);
        }
    }

    /// <summary>
    /// One introduction card of a round level (spec 006 FR-045): its <see cref="Id"/> (to remember it was seen), the
    /// mechanic, its kind, its picture, its title and lines as <c>Strings_en.csv</c> keys (hosts read them with
    /// <c>Loc.T</c> / <c>PlaytestText.T</c>), and the key of its one button.
    /// </summary>
    public sealed class RoundIntroCard
    {
        internal RoundIntroCard(string mechanic, RoundIntroKind kind, RoundIntroPicture picture, string titleKey, IReadOnlyList<string> lineKeys)
        {
            Mechanic = mechanic;
            Kind = kind;
            Picture = picture;
            TitleKey = titleKey;
            LineKeys = lineKeys;
        }

        /// <summary>The id the save keeps once the card was shown: <c>round.intro.{mechanic}</c>, with <c>.reminder</c> for a reminder.</summary>
        public string Id => RoundIntroCards.IdOf(Mechanic, Kind == RoundIntroKind.Reminder);

        /// <summary>
        /// The mechanic: <see cref="RoundIntroCards.Hidden"/>, <see cref="RoundIntroCards.Links"/>, <see cref="RoundIntroCards.Bridge"/>,
        /// <see cref="RoundIntroCards.Latch"/>, <see cref="RoundIntroCards.Canal"/> or <see cref="RoundIntroCards.Shutters"/>.
        /// </summary>
        public string Mechanic { get; }

        public RoundIntroKind Kind { get; }

        public RoundIntroPicture Picture { get; }

        public string TitleKey { get; }

        /// <summary>The card's lines, in order: three for an explanation, one for a reminder.</summary>
        public IReadOnlyList<string> LineKeys { get; }

        /// <summary>The card's one button.</summary>
        public string ButtonKey => RoundIntroCards.ButtonKey;
    }

    /// <summary>
    /// The introduction cards of the round levels' six mechanics with a card (spec 006 FR-045, FR-066, FR-069, FR-070,
    /// contracts/presentation.md, rules 17 §9): a level whose <c>introduction</c> names a mechanic opens with that mechanic's
    /// explanation card at its first meeting (L6 hidden pods, L11 links, L16 the bridge, L61 the latch, L101 the water
    /// channel, L121 the wooden shutters) and with a short reminder card at its second (<c>second: true</c>; L8, L13, L18,
    /// L63, L103, L123). Hidden cells (L51, L53) open with no card (the owner, 2026-11-01). Each card shows once (<see cref="ToShow"/> with the save's seen ids, as the
    /// game's demo cards). Content only: the level screen draws the card and records <see cref="RoundIntroCard.Id"/>.
    /// Engine-free.
    /// </summary>
    public static class RoundIntroCards
    {
        public const string Hidden = "hidden";

        public const string Links = "links";

        public const string Bridge = "bridge";

        /// <summary>
        /// The latch with its acorn key (17 §8 «Засов и жёлудь-ключ», introduced on L61 and L63 by the owner's amendment of
        /// 2026-11-01). Hidden cells (L51, L53) have no card: the player sees the «?» and the cells reveal by themselves
        /// (the owner, 2026-11-01).
        /// </summary>
        public const string Latch = "latch";

        /// <summary>The Drop water channel (17 §8 «Drop Водный канал», introduced on L101 and L103; the levels' mechanic id <c>canal</c>).</summary>
        public const string Canal = "canal";

        /// <summary>The Twig wooden shutters (17 §8 «Twig Деревянные створки», introduced on L121 and L123).</summary>
        public const string Shutters = "shutters";

        /// <summary>The cards' button.</summary>
        public const string ButtonKey = "round.intro.got_it";

        /// <summary>The mechanics with a card, in the order the levels meet them.</summary>
        public static IReadOnlyList<string> Mechanics { get; } = new[] { Hidden, Links, Bridge, Latch, Canal, Shutters };

        private static readonly Dictionary<string, RoundIntroCard> Explanations = new Dictionary<string, RoundIntroCard>(StringComparer.Ordinal)
        {
            [Hidden] = new RoundIntroCard(Hidden, RoundIntroKind.Explanation, RoundIntroPicture.HiddenPod, "round.intro.hidden.title", new[] { "round.intro.hidden.1", "round.intro.hidden.2", "round.intro.hidden.3" }),
            [Links] = new RoundIntroCard(Links, RoundIntroKind.Explanation, RoundIntroPicture.LinkedPair, "round.intro.links.title", new[] { "round.intro.links.1", "round.intro.links.2", "round.intro.links.3" }),
            [Bridge] = new RoundIntroCard(Bridge, RoundIntroKind.Explanation, RoundIntroPicture.Bridge, "round.intro.bridge.title", new[] { "round.intro.bridge.1", "round.intro.bridge.2", "round.intro.bridge.3" }),
            [Latch] = new RoundIntroCard(Latch, RoundIntroKind.Explanation, RoundIntroPicture.Latch, "round.intro.latch.title", new[] { "round.intro.latch.1", "round.intro.latch.2", "round.intro.latch.3" }),
            [Canal] = new RoundIntroCard(Canal, RoundIntroKind.Explanation, RoundIntroPicture.Channel, "round.intro.canal.title", new[] { "round.intro.canal.1", "round.intro.canal.2", "round.intro.canal.3" }),
            [Shutters] = new RoundIntroCard(Shutters, RoundIntroKind.Explanation, RoundIntroPicture.Shutters, "round.intro.shutters.title", new[] { "round.intro.shutters.1", "round.intro.shutters.2", "round.intro.shutters.3" }),
        };

        private static readonly Dictionary<string, RoundIntroCard> Reminders = new Dictionary<string, RoundIntroCard>(StringComparer.Ordinal)
        {
            [Hidden] = new RoundIntroCard(Hidden, RoundIntroKind.Reminder, RoundIntroPicture.HiddenPod, "round.intro.hidden.title", new[] { "round.intro.hidden.reminder" }),
            [Links] = new RoundIntroCard(Links, RoundIntroKind.Reminder, RoundIntroPicture.LinkedPair, "round.intro.links.title", new[] { "round.intro.links.reminder" }),
            [Bridge] = new RoundIntroCard(Bridge, RoundIntroKind.Reminder, RoundIntroPicture.Bridge, "round.intro.bridge.title", new[] { "round.intro.bridge.reminder" }),
            [Latch] = new RoundIntroCard(Latch, RoundIntroKind.Reminder, RoundIntroPicture.Latch, "round.intro.latch.title", new[] { "round.intro.latch.reminder" }),
            [Canal] = new RoundIntroCard(Canal, RoundIntroKind.Reminder, RoundIntroPicture.Channel, "round.intro.canal.title", new[] { "round.intro.canal.reminder" }),
            [Shutters] = new RoundIntroCard(Shutters, RoundIntroKind.Reminder, RoundIntroPicture.Shutters, "round.intro.shutters.title", new[] { "round.intro.shutters.reminder" }),
        };

        /// <summary>The id a card is remembered by: <c>round.intro.{mechanic}</c>, <c>round.intro.{mechanic}.reminder</c>.</summary>
        public static string IdOf(string mechanic, bool reminder) => "round.intro." + mechanic + (reminder ? ".reminder" : string.Empty);

        /// <summary>The card of a mechanic's first (<paramref name="second"/> false) or second meeting, or null for an unknown mechanic.</summary>
        public static RoundIntroCard? For(string mechanic, bool second) =>
            (second ? Reminders : Explanations).TryGetValue(mechanic, out RoundIntroCard? card) ? card : null;

        /// <summary>The card a level's <c>introduction</c> calls for, or null when it introduces nothing.</summary>
        public static RoundIntroCard? For(RoundIntroduction? introduction) =>
            introduction == null ? null : For(introduction.Mechanic, introduction.Second);

        /// <summary>
        /// The card to open when the level starts: the one its <paramref name="introduction"/> calls for, unless the player
        /// has seen it (<paramref name="seen"/> of its <see cref="RoundIntroCard.Id"/>, the save's demos seen).
        /// </summary>
        public static RoundIntroCard? ToShow(RoundIntroduction? introduction, Func<string, bool> seen)
        {
            RoundIntroCard? card = For(introduction);
            return card != null && !seen(card.Id) ? card : null;
        }

        /// <summary>Every string key the cards use (titles, lines and the button), for the localization tests.</summary>
        public static IEnumerable<string> Keys()
        {
            yield return ButtonKey;
            foreach (Dictionary<string, RoundIntroCard> cards in new[] { Explanations, Reminders })
            {
                foreach (RoundIntroCard card in cards.Values)
                {
                    yield return card.TitleKey;
                    foreach (string line in card.LineKeys)
                    {
                        yield return line;
                    }
                }
            }
        }
    }
}
