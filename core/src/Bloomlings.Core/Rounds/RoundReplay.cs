using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bloomlings.Core.Rounds
{
    /// <summary>The kinds of recorded round commands: the two player actions and the four boosters.</summary>
    public enum RoundCommandType
    {
        Tap,
        Bridge,
        ExtraSlot,
        Return,
        Shuffle,
        BloomBurst,
    }

    /// <summary>
    /// A recorded command with its model time in µs (constitution III: a command carries its time as an integer). Its
    /// text is <c>tap pod=3 at=0</c>, <c>bridge choice=1 at=…</c>, <c>extra-slot at=…</c>, <c>return pod=4 at=…</c>,
    /// <c>shuffle at=…</c> or <c>burst code=G at=…</c>.
    /// </summary>
    public readonly struct RoundCommand
    {
        private RoundCommand(RoundCommandType type, long atUs, int value = -1, char code = '\0')
        {
            Type = type;
            AtUs = atUs;
            Value = value;
            Code = code;
        }

        public RoundCommandType Type { get; }

        public long AtUs { get; }

        /// <summary>The pod of a tap or return, the site of a bridge, else -1.</summary>
        public int Value { get; }

        /// <summary>The material of a Bloom Burst.</summary>
        public char Code { get; }

        public static RoundCommand Tap(int pod, long atUs) => new RoundCommand(RoundCommandType.Tap, atUs, pod);

        public static RoundCommand Bridge(int choice, long atUs) => new RoundCommand(RoundCommandType.Bridge, atUs, choice);

        public static RoundCommand ExtraSlot(long atUs) => new RoundCommand(RoundCommandType.ExtraSlot, atUs);

        public static RoundCommand Return(int pod, long atUs) => new RoundCommand(RoundCommandType.Return, atUs, pod);

        public static RoundCommand Shuffle(long atUs) => new RoundCommand(RoundCommandType.Shuffle, atUs);

        public static RoundCommand BloomBurst(char code, long atUs) => new RoundCommand(RoundCommandType.BloomBurst, atUs, code: code);

        /// <summary>The command of a player action at a time.</summary>
        public static RoundCommand Of(RoundAction action, long atUs) =>
            action.Type == RoundActionType.Tap ? Tap(action.Pod, atUs) : Bridge(action.Choice, atUs);

        /// <summary>The same command at another time.</summary>
        public RoundCommand At(long atUs) => new RoundCommand(Type, atUs, Value, Code);

        public override string ToString()
        {
            string at = " at=" + AtUs.ToString(CultureInfo.InvariantCulture);
            string value = Value.ToString(CultureInfo.InvariantCulture);
            return Type switch
            {
                RoundCommandType.Tap => "tap pod=" + value + at,
                RoundCommandType.Bridge => "bridge choice=" + value + at,
                RoundCommandType.ExtraSlot => "extra-slot" + at,
                RoundCommandType.Return => "return pod=" + value + at,
                RoundCommandType.Shuffle => "shuffle" + at,
                _ => "burst code=" + Code + at,
            };
        }

        /// <summary>Parses the text written by <see cref="ToString"/>.</summary>
        public static RoundCommand Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            string[] parts = text.Split(' ');
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 1; i < parts.Length; i++)
            {
                int eq = parts[i].IndexOf('=');
                if (eq <= 0)
                {
                    throw new FormatException("Bad round command field: " + parts[i]);
                }

                fields[parts[i].Substring(0, eq)] = parts[i].Substring(eq + 1);
            }

            long at = long.Parse(Field(fields, "at"), NumberStyles.None, CultureInfo.InvariantCulture);
            int Int(string key) => int.Parse(Field(fields, key), NumberStyles.None, CultureInfo.InvariantCulture);
            switch (parts[0])
            {
                case "tap":
                    return Tap(Int("pod"), at);
                case "bridge":
                    return Bridge(Int("choice"), at);
                case "extra-slot":
                    return ExtraSlot(at);
                case "return":
                    return Return(Int("pod"), at);
                case "shuffle":
                    return Shuffle(at);
                case "burst":
                    string code = Field(fields, "code");
                    if (code.Length != 1)
                    {
                        throw new FormatException("Bad burst material: " + code);
                    }

                    return BloomBurst(code[0], at);
                default:
                    throw new FormatException("Unknown round command: " + parts[0]);
            }
        }

        private static string Field(Dictionary<string, string> fields, string key) =>
            fields.TryGetValue(key, out string? value) ? value : throw new FormatException("Missing round command field: " + key);
    }

    /// <summary>
    /// Replays recorded commands on a <see cref="RoundTimeline"/> on one running clock, as the reference's trace
    /// (<c>level-stats/trace.mjs run</c>): the timeline's model time stands still while nothing is pending, so the time
    /// that passes then is kept apart in <see cref="IdleUs"/>, and <see cref="ClockUs"/> = model time + idle is the clock
    /// the commands were recorded on. With an <see cref="RoundEventLog"/> it collects the canonical log.
    /// </summary>
    public sealed class RoundReplay
    {
        /// <summary>The reference's wait for a refused recorded command: at most this many chunks of 100 ms.</summary>
        public const int WaitGuard = 2000;

        public const long WaitChunkUs = 100_000;

        public RoundReplay(RoundTimeline timeline, RoundEventLog? log = null)
        {
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            Log = log;
        }

        public RoundTimeline Timeline { get; }

        public RoundEventLog? Log { get; }

        /// <summary>Time that passed while nothing was pending, in µs.</summary>
        public long IdleUs { get; private set; }

        /// <summary>The recording clock: model time plus idle time.</summary>
        public long ClockUs => Timeline.Now + IdleUs;

        /// <summary>The clock time of the next scheduled step (only while something is pending).</summary>
        public long NextAtUs => Timeline.Steps[0].At + IdleUs;

        /// <summary>
        /// Moves the clock to <paramref name="clockUs"/> (reference trace <c>advanceTo</c>): every step due by then runs,
        /// one step's moment at a time; the rest of the time advances the timeline while work is pending, else it is idle.
        /// </summary>
        public void AdvanceTo(long clockUs)
        {
            RoundTimeline t = Timeline;
            while (t.Pending && t.Steps[0].At + IdleUs <= clockUs)
            {
                t.Advance(Math.Max(0, t.Steps[0].At - t.Now));
                Log?.Add(t.LastEvents);
            }

            if (clockUs > ClockUs)
            {
                if (t.Pending)
                {
                    t.Advance(clockUs - ClockUs);
                    Log?.Add(t.LastEvents);
                }
                else
                {
                    IdleUs += clockUs - ClockUs;
                }
            }
        }

        /// <summary>Runs until nothing is pending.</summary>
        public void Finish()
        {
            while (Timeline.Pending)
            {
                AdvanceTo(NextAtUs);
            }
        }

        /// <summary>
        /// Executes a recorded command at its time: the clock moves to <see cref="RoundCommand.AtUs"/> first (never back).
        /// With <paramref name="patient"/>, all work settles first (the reference's saved-solution replay). With
        /// <paramref name="waitUntilAccepted"/>, a refused player action waits in 100 ms chunks while work is pending, as
        /// the reference's trace does for recorded taps.
        /// </summary>
        public RoundCommitResult Execute(RoundCommand command, bool patient = false, bool waitUntilAccepted = false)
        {
            if (command.AtUs > ClockUs)
            {
                AdvanceTo(command.AtUs);
            }

            if (patient)
            {
                Finish();
            }

            if (waitUntilAccepted && (command.Type == RoundCommandType.Tap || command.Type == RoundCommandType.Bridge))
            {
                RoundAction action = command.Type == RoundCommandType.Tap ? RoundAction.Tap(command.Value) : RoundAction.BuildBridge(command.Value);
                for (int guard = 0; guard < WaitGuard && Timeline.Check(action) != RoundRefusal.None && Timeline.Pending; guard++)
                {
                    AdvanceTo(Math.Min(NextAtUs, ClockUs + WaitChunkUs));
                }
            }

            RoundCommitResult result = Apply(Timeline, command);
            if (result.Accepted)
            {
                Log?.Add(result.Events);
            }

            return result;
        }

        /// <summary>Applies a command at the timeline's current model time (no clock movement).</summary>
        public static RoundCommitResult Apply(RoundTimeline timeline, RoundCommand command) => command.Type switch
        {
            RoundCommandType.Tap => timeline.Commit(RoundAction.Tap(command.Value)),
            RoundCommandType.Bridge => timeline.Commit(RoundAction.BuildBridge(command.Value)),
            RoundCommandType.ExtraSlot => RoundBoosters.ExtraSlot(timeline),
            RoundCommandType.Return => RoundBoosters.Return(timeline, command.Value),
            RoundCommandType.Shuffle => RoundBoosters.Shuffle(timeline),
            _ => RoundBoosters.BloomBurst(timeline, command.Code),
        };
    }
}
