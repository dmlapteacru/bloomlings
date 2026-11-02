using System;
using System.Collections.Generic;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Whether an introduction card explains a mechanic (its first meeting) or reminds of it (its second).</summary>
    public enum RoundIntroKind
    {
        /// <summary>The first meeting (L6 hidden pods, L11 links, L16 the bridge, L61 the latch): the explanation card.</summary>
        Explanation,

        /// <summary>The second meeting (L8, L13, L18, L63): a short reminder card.</summary>
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
        /// Our Twig hero holding the padlock in the held pod's colour in the pod's place (<see cref="RoundLook.HeroCell"/>) beside
        /// a patch of the board with its key standing upright and turning over its cell between cubes
        /// (<see cref="UiRaster.UprightKey"/>; 17 §8, the owner's look of 2026-11-02, laid out by <see cref="RoundIntroLatch"/>); in
        /// a loop (<see cref="RoundIntroLatch.Since"/>) the key flies to the keyhole, the padlock opens and Twig steps aside from
        /// the pod.
        /// </summary>
        Latch,
    }

    /// <summary>
    /// The latch card's picture (<see cref="RoundIntroPicture.Latch"/>), the same in both builds: the held pod's place at the
    /// left with our Twig hero and his padlock in it, a patch of the board at the right with cubes round an empty cell in its
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

        /// <summary>The padlock's square in the hero's hands (<see cref="RoundLook.HeroLock"/>).</summary>
        public Box Lock => RoundLook.HeroLock(RoundLook.HeroCell(Pod));

        /// <summary>The picture laid out in <paramref name="area"/>.</summary>
        public static RoundIntroLatch In(Box area)
        {
            float h = area.Height;
            float pw = h * 0.7f;
            var pod = Box.FromCenter(area.Left + (area.Width * 0.25f), area.CenterY + (h * 0.12f), pw, pw);
            float cell = Math.Min(h * 0.2f, area.Width * 0.1f);
            float row = cell * RoundBoardLayout.RowShare;
            const int cols = 5;
            const int rows = 3;
            float px = area.Left + (area.Width * 0.66f);
            var patch = Box.FromCenter(px, area.CenterY + (h * 0.12f), (cols * cell) + (cell * 0.5f), (rows * row) + (cell * RoundBoardLayout.FrontShare) + (cell * 0.5f));
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

                    cubes.Add((new Box(box.Left, box.Top, box.Right, box.Bottom + (cell * RoundBoardLayout.FrontShare)), materials[(x + (y * 2)) % materials.Length]));
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
        /// The mechanic: <see cref="RoundIntroCards.Hidden"/>, <see cref="RoundIntroCards.Links"/>, <see cref="RoundIntroCards.Bridge"/>
        /// or <see cref="RoundIntroCards.Latch"/>.
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
    /// The introduction cards of the round levels' four mechanics (spec 006 FR-045, FR-066, contracts/presentation.md,
    /// rules 17 §9): a level whose <c>introduction</c> names a mechanic opens with that mechanic's explanation card at its
    /// first meeting (L6 hidden pods, L11 links, L16 the bridge, L61 the latch) and with a short reminder card at its
    /// second (<c>second: true</c>; L8, L13, L18, L63). Hidden cells (L51, L53) open with no card (the owner, 2026-11-01). Each card shows once (<see cref="ToShow"/> with the save's seen ids, as the
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

        /// <summary>The cards' button.</summary>
        public const string ButtonKey = "round.intro.got_it";

        /// <summary>The four mechanics with a card, in the order the levels meet them.</summary>
        public static IReadOnlyList<string> Mechanics { get; } = new[] { Hidden, Links, Bridge, Latch };

        private static readonly Dictionary<string, RoundIntroCard> Explanations = new Dictionary<string, RoundIntroCard>(StringComparer.Ordinal)
        {
            [Hidden] = new RoundIntroCard(Hidden, RoundIntroKind.Explanation, RoundIntroPicture.HiddenPod, "round.intro.hidden.title", new[] { "round.intro.hidden.1", "round.intro.hidden.2", "round.intro.hidden.3" }),
            [Links] = new RoundIntroCard(Links, RoundIntroKind.Explanation, RoundIntroPicture.LinkedPair, "round.intro.links.title", new[] { "round.intro.links.1", "round.intro.links.2", "round.intro.links.3" }),
            [Bridge] = new RoundIntroCard(Bridge, RoundIntroKind.Explanation, RoundIntroPicture.Bridge, "round.intro.bridge.title", new[] { "round.intro.bridge.1", "round.intro.bridge.2", "round.intro.bridge.3" }),
            [Latch] = new RoundIntroCard(Latch, RoundIntroKind.Explanation, RoundIntroPicture.Latch, "round.intro.latch.title", new[] { "round.intro.latch.1", "round.intro.latch.2", "round.intro.latch.3" }),
        };

        private static readonly Dictionary<string, RoundIntroCard> Reminders = new Dictionary<string, RoundIntroCard>(StringComparer.Ordinal)
        {
            [Hidden] = new RoundIntroCard(Hidden, RoundIntroKind.Reminder, RoundIntroPicture.HiddenPod, "round.intro.hidden.title", new[] { "round.intro.hidden.reminder" }),
            [Links] = new RoundIntroCard(Links, RoundIntroKind.Reminder, RoundIntroPicture.LinkedPair, "round.intro.links.title", new[] { "round.intro.links.reminder" }),
            [Bridge] = new RoundIntroCard(Bridge, RoundIntroKind.Reminder, RoundIntroPicture.Bridge, "round.intro.bridge.title", new[] { "round.intro.bridge.reminder" }),
            [Latch] = new RoundIntroCard(Latch, RoundIntroKind.Reminder, RoundIntroPicture.Latch, "round.intro.latch.title", new[] { "round.intro.latch.reminder" }),
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
