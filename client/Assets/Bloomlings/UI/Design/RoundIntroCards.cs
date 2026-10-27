using System;
using System.Collections.Generic;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Whether an introduction card explains a mechanic (its first meeting) or reminds of it (its second).</summary>
    public enum RoundIntroKind
    {
        /// <summary>The first meeting (L6 hidden pods, L11 links, L16 the bridge): the explanation card.</summary>
        Explanation,

        /// <summary>The second meeting (L8, L13, L18): a short reminder card.</summary>
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

        /// <summary>The mechanic: <see cref="RoundIntroCards.Hidden"/>, <see cref="RoundIntroCards.Links"/> or <see cref="RoundIntroCards.Bridge"/>.</summary>
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
    /// The introduction cards of the round levels' three mechanics (spec 006 FR-045, contracts/presentation.md,
    /// rules 17 §9): a level whose <c>introduction</c> names a mechanic opens with that mechanic's explanation card at its
    /// first meeting (L6 hidden pods, L11 links, L16 the bridge) and with a short reminder card at its second
    /// (<c>second: true</c>; L8, L13, L18). Each card shows once (<see cref="ToShow"/> with the save's seen ids, as the
    /// game's demo cards). Content only: the level screen draws the card and records <see cref="RoundIntroCard.Id"/>.
    /// Engine-free.
    /// </summary>
    public static class RoundIntroCards
    {
        public const string Hidden = "hidden";

        public const string Links = "links";

        public const string Bridge = "bridge";

        /// <summary>The cards' button.</summary>
        public const string ButtonKey = "round.intro.got_it";

        /// <summary>The three mechanics in the order the levels meet them.</summary>
        public static IReadOnlyList<string> Mechanics { get; } = new[] { Hidden, Links, Bridge };

        private static readonly Dictionary<string, RoundIntroCard> Explanations = new Dictionary<string, RoundIntroCard>(StringComparer.Ordinal)
        {
            [Hidden] = new RoundIntroCard(Hidden, RoundIntroKind.Explanation, RoundIntroPicture.HiddenPod, "round.intro.hidden.title", new[] { "round.intro.hidden.1", "round.intro.hidden.2", "round.intro.hidden.3" }),
            [Links] = new RoundIntroCard(Links, RoundIntroKind.Explanation, RoundIntroPicture.LinkedPair, "round.intro.links.title", new[] { "round.intro.links.1", "round.intro.links.2", "round.intro.links.3" }),
            [Bridge] = new RoundIntroCard(Bridge, RoundIntroKind.Explanation, RoundIntroPicture.Bridge, "round.intro.bridge.title", new[] { "round.intro.bridge.1", "round.intro.bridge.2", "round.intro.bridge.3" }),
        };

        private static readonly Dictionary<string, RoundIntroCard> Reminders = new Dictionary<string, RoundIntroCard>(StringComparer.Ordinal)
        {
            [Hidden] = new RoundIntroCard(Hidden, RoundIntroKind.Reminder, RoundIntroPicture.HiddenPod, "round.intro.hidden.title", new[] { "round.intro.hidden.reminder" }),
            [Links] = new RoundIntroCard(Links, RoundIntroKind.Reminder, RoundIntroPicture.LinkedPair, "round.intro.links.title", new[] { "round.intro.links.reminder" }),
            [Bridge] = new RoundIntroCard(Bridge, RoundIntroKind.Reminder, RoundIntroPicture.Bridge, "round.intro.bridge.title", new[] { "round.intro.bridge.reminder" }),
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
