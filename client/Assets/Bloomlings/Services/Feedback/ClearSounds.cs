using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Client.Services.Feedback
{
    /// <summary>
    /// The clearing styles' sounds (spec 005 FR-042). Each style has a collect, played when a tile's clear lands in its
    /// slot and pitched on its pod's ladder (<see cref="ClearSounds.Ladder"/>), and the textures of its act on the way,
    /// soft and unpitched, in <see cref="ClearSounds.Variations"/> small variations. Every clip is synthesized
    /// (<see cref="ToneSynth.Clear"/>).
    /// </summary>
    public enum ClearSound
    {
        /// <summary>Blossom's collect: a soft kalimba note.</summary>
        BlossomPluck,

        /// <summary>Munchers' collect, as the full Bloomling hops home: a round, woody "bloop".</summary>
        MunchGulp,

        /// <summary>Fireflies' collect, as the swarm reaches the slot: a small glass bell.</summary>
        FireflyChime,

        /// <summary>Bubbles' collect, as the bubble pops on the slot: a rising water-drop "plip".</summary>
        BubblePop,

        /// <summary>Pushers' collect, as the tile hops into the slot: a low marimba "plonk".</summary>
        PushPlonk,

        /// <summary>Fireworks' collect, as the sparkles land: two bell pings an octave apart.</summary>
        FireworkSparkle,

        /// <summary>The Confetti Parade's collect, the pop into confetti: a springy "boing" with a crackle.</summary>
        ParadeBoing,

        /// <summary>Blossom's flower opening: a breathy rising "fwip".</summary>
        BlossomFwip,

        /// <summary>A Muncher's bite: a short crunch on a soft thump, a little higher each bite.</summary>
        MunchCrunch,

        /// <summary>The tile breaking into fireflies: three tiny high pings.</summary>
        FireflyTwinkle,

        /// <summary>A bubble being blown round the tile: a soft rising "fwoop".</summary>
        BubbleBlow,

        /// <summary>A pushed tile landing on the next cell: a small wood-block knock.</summary>
        PushKnock,

        /// <summary>A firework's tile swelling white: a quiet fizz.</summary>
        FireworkFizz,

        /// <summary>The firework bursting: a soft, low "pomf".</summary>
        FireworkBang,

        /// <summary>A Parade Bloomling's little hop on its tile: a tiny "pip".</summary>
        ParadeHop,
    }

    /// <summary>One sound of a walker's trip before its clear: when (a share of the trip), what and which variation.</summary>
    public readonly struct ClearBeat
    {
        public ClearBeat(float share, ClearSound sound, int variation)
        {
            Share = share;
            Sound = sound;
            Variation = variation;
        }

        /// <summary>The beat's time as a share of the trip (0 when the walker sets off, 1 at the clear).</summary>
        public float Share { get; }

        public ClearSound Sound { get; }

        /// <summary>0 to <see cref="ClearSounds.Variations"/> - 1.</summary>
        public int Variation { get; }
    }

    /// <summary>
    /// Which sound each clearing style plays when (spec 005 FR-042, contracts/look.md §6.16), shared by both builds: the
    /// act's beats come from the same legs and moments that <see cref="ClearLook"/> draws (Munchers' three bites, a
    /// Pusher's tumble onto each cell, the firework's burst), the collect and its micro haptic come with the rules'
    /// clear (the slot's count going down). Every pitched note is on the C major pentatonic scale of the music, so
    /// sounds that overlap never clash. Engine-free.
    /// </summary>
    public static class ClearSounds
    {
        /// <summary>How many variations each texture has (and the clip count of a texture).</summary>
        public const int Variations = 3;

        /// <summary>
        /// The ladder a pod's collects climb, G4 to E6 on the pentatonic scale: a pod's first tile plays the lowest note
        /// and its last the highest, then the pod's done chord resolves it (<see cref="SoundCue.PodDone"/>).
        /// </summary>
        public static IReadOnlyList<double> Ladder { get; } = new[]
        {
            392.00, 440.00, 523.25, 587.33, 659.26, 783.99, 880.00, 1046.50, 1174.66, 1318.51,
        };

        public static int Steps => Ladder.Count;

        /// <summary>Every sound, in the enum's order.</summary>
        public static IReadOnlyList<ClearSound> All { get; } = (ClearSound[])Enum.GetValues(typeof(ClearSound));

        /// <summary>The sound a style plays at each clear.</summary>
        public static ClearSound CollectOf(ClearStyle style) => style switch
        {
            ClearStyle.Munchers => ClearSound.MunchGulp,
            ClearStyle.Fireflies => ClearSound.FireflyChime,
            ClearStyle.Bubbles => ClearSound.BubblePop,
            ClearStyle.Pushers => ClearSound.PushPlonk,
            ClearStyle.Fireworks => ClearSound.FireworkSparkle,
            ClearStyle.Parade => ClearSound.ParadeBoing,
            _ => ClearSound.BlossomPluck,
        };

        /// <summary>Whether the sound is a collect (pitched on the ladder) rather than a texture (with variations).</summary>
        public static bool IsCollect(ClearSound sound) => sound <= ClearSound.ParadeBoing;

        /// <summary>How many clips a sound has: one per ladder step for a collect, one per variation for a texture.</summary>
        public static int ClipCount(ClearSound sound) => IsCollect(sound) ? Steps : Variations;

        /// <summary>
        /// The micro haptic of a style's clear: one short transient a tile, its feel matching the style (a soft low tick
        /// for a flower, a crisper tick for a pop, a heavier one for a pushed tile, two quick ticks for the Parade).
        /// </summary>
        public static HapticPattern CollectHaptic(ClearStyle style) => style switch
        {
            ClearStyle.Munchers => HapticPattern.Micro(12, 70, new HapticNote(HapticPrimitive.LowTick, 0.42f)),
            ClearStyle.Fireflies => HapticPattern.Micro(8, 55, new HapticNote(HapticPrimitive.Tick, 0.28f)),
            ClearStyle.Bubbles => HapticPattern.Micro(9, 70, new HapticNote(HapticPrimitive.Tick, 0.4f)),
            ClearStyle.Pushers => HapticPattern.Micro(14, 85, new HapticNote(HapticPrimitive.LowTick, 0.5f)),
            ClearStyle.Fireworks => HapticPattern.Micro(10, 80, new HapticNote(HapticPrimitive.Click, 0.35f)),
            ClearStyle.Parade => HapticPattern.Micro(10, 70, new HapticNote(HapticPrimitive.Tick, 0.3f), new HapticNote(HapticPrimitive.Tick, 0.42f, 40)),
            _ => HapticPattern.Micro(10, 60, new HapticNote(HapticPrimitive.LowTick, 0.32f)),
        };

        /// <summary>
        /// The ladder step of a pod's <paramref name="done"/>-th collect (1 for its first tile) out of
        /// <paramref name="total"/>: spread evenly from the lowest step to the highest, so the last tile is always on
        /// top whatever the pod's count (a pod of one plays the top note).
        /// </summary>
        public static int Step(int done, int total)
        {
            if (total <= 1)
            {
                return Steps - 1;
            }

            double share = (double)(Math.Max(1, Math.Min(done, total)) - 1) / (total - 1);
            return Math.Max(0, Math.Min(Steps - 1, (int)Math.Floor((share * (Steps - 1)) + 0.5)));
        }

        /// <summary>
        /// Adds to <paramref name="into"/> the beats of a walker's trip (<paramref name="cells"/> route cells, setting off
        /// at <paramref name="start"/> and clearing <paramref name="arrival"/> seconds later on the timeline clock) whose
        /// time is after <paramref name="from"/> and at or before <paramref name="to"/>: the act's textures, never the
        /// collect (it comes with the clear). <paramref name="seed"/> (the tile's cell, say) picks the variations.
        /// </summary>
        public static void Crossed(ClearStyle style, int cells, float start, float arrival, float from, float to, int seed, List<ClearBeat> into)
        {
            if (to <= from || arrival <= 0f || to <= start || from >= start + arrival)
            {
                return;
            }

            var window = new Window(start, arrival, from, to, into);
            ClearLegs legs = ClearStyles.LegsOf(style, cells);
            float total = legs.Total;
            if (total <= 0f)
            {
                return;
            }

            float act = legs.Out / total;
            float actShare = legs.Act / total;
            float back = (legs.Out + legs.Act) / total;
            float backShare = legs.Back / total;
            float fin = (legs.Out + legs.Act + legs.Back) / total;
            int variation = ((seed % Variations) + Variations) % Variations;
            switch (style)
            {
                case ClearStyle.Munchers:
                    // The three bites (ClearLook's crumbs fly at these moments of the act), each a little higher.
                    window.Add(act + (0.32f * actShare), ClearSound.MunchCrunch, 0);
                    window.Add(act + (0.56f * actShare), ClearSound.MunchCrunch, 1);
                    window.Add(act + (0.80f * actShare), ClearSound.MunchCrunch, 2);
                    break;
                case ClearStyle.Fireflies:
                    // The light breaking into fireflies as the last leg begins.
                    window.Add(fin + 0.01f, ClearSound.FireflyTwinkle, variation);
                    break;
                case ClearStyle.Bubbles:
                    // The bubble growing round the tile.
                    window.Add(act + (0.3f * actShare), ClearSound.BubbleBlow, variation);
                    break;
                case ClearStyle.Pushers:
                {
                    // The tile landing on each cell of its way home (ClearLook's tumble squashes at 0.72–0.9 of a step).
                    int segments = Math.Max(1, cells);
                    for (int s = 0; s < segments; s++)
                    {
                        window.Add(back + (((s + 0.8f) / segments) * backShare), ClearSound.PushKnock, (variation + s) % Variations);
                    }

                    break;
                }

                case ClearStyle.Fireworks:
                    // The swell under the walker, then the burst as the last leg begins.
                    window.Add(act + 0.005f, ClearSound.FireworkFizz, variation);
                    window.Add(fin + 0.005f, ClearSound.FireworkBang, variation);
                    break;
                case ClearStyle.Parade:
                    // Its two little hops on the tile.
                    window.Add(act + (0.275f * actShare), ClearSound.ParadeHop, 0);
                    window.Add(act + (0.55f * actShare), ClearSound.ParadeHop, 1);
                    break;
                default:
                    // Blossom: the flower opening where the tile sank.
                    window.Add(act + (0.32f * actShare), ClearSound.BlossomFwip, variation);
                    break;
            }
        }

        // The beats in (from, to], as shares of one walker's trip.
        private readonly struct Window
        {
            private readonly float _start;
            private readonly float _arrival;
            private readonly float _from;
            private readonly float _to;
            private readonly List<ClearBeat> _into;

            public Window(float start, float arrival, float from, float to, List<ClearBeat> into)
            {
                _start = start;
                _arrival = arrival;
                _from = from;
                _to = to;
                _into = into;
            }

            public void Add(float share, ClearSound sound, int variation)
            {
                if (share >= 1f)
                {
                    return;
                }

                float at = _start + (share * _arrival);
                if (at > _from && at <= _to)
                {
                    _into.Add(new ClearBeat(share, sound, variation));
                }
            }
        }
    }

    /// <summary>
    /// Which ladder step each pod's next collect plays (<see cref="ClearSounds.Step"/>): it counts the clears of each pod
    /// since the level started. One per level screen; <see cref="Reset"/> on a new attempt.
    /// </summary>
    public sealed class ClearLadder
    {
        private readonly Dictionary<string, int> _done = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>The step of the pod's next clear, out of its <paramref name="total"/> tiles, counting it.</summary>
        public int Next(string podId, int total)
        {
            int done = (_done.TryGetValue(podId, out int n) ? n : 0) + 1;
            _done[podId] = done;
            return ClearSounds.Step(done, total);
        }

        public void Reset() => _done.Clear();
    }
}
