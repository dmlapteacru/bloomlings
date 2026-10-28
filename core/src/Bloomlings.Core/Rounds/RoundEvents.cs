using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Bloomlings.Core.Rounds
{
    /// <summary>
    /// The canonical event log and the extended state signature of the round timeline (spec 006 research R4, contract
    /// <c>conformance-fixture.md</c>). Each event is one UTF-8 line ending in '\n', its model time in µs first:
    /// <code>
    /// t commit pod=&lt;p&gt; slot=&lt;s&gt;
    /// t reveal pod=&lt;p&gt; mode=&lt;depth|sealed&gt;
    /// t bridge cell=&lt;c&gt;
    /// t depart id=&lt;j&gt; pod=&lt;p&gt; cell=&lt;c&gt; arrive=&lt;µs&gt; clear=&lt;µs&gt; done=&lt;µs&gt; route=&lt;len&gt;
    /// t arrive id=&lt;j&gt;
    /// t clear id=&lt;j&gt; cell=&lt;c&gt;
    /// t return id=&lt;j&gt;
    /// t complete pod=&lt;p&gt;
    /// </code>
    /// The reference's <c>assign</c> is omitted (it always precedes its depart). Boosters, which the reference does not
    /// play and no fixture holds, add <c>t extra-slot slot=&lt;s&gt;</c>, <c>t recall pod=&lt;p&gt; slot=&lt;s&gt;
    /// column=&lt;c&gt;</c>, <c>t shuffle changed=&lt;0|1&gt;</c>, <c>t burst code=&lt;X&gt; cells=&lt;n&gt;</c> and
    /// <c>t burst-clear cell=&lt;c&gt;</c>. The events of one window (a command and everything up to the next command, or
    /// to the end) are hashed together with SHA-256 and written <c>sha256:&lt;hex&gt;</c>.
    /// </summary>
    public static class RoundEvents
    {
        /// <summary>The canonical line of one event, without the line end.</summary>
        public static string Line(RoundEvent e)
        {
            var sb = new StringBuilder(96);
            AppendLine(sb, e);
            return sb.ToString(0, sb.Length - 1);
        }

        /// <summary>Appends the canonical line of one event, with its '\n'.</summary>
        public static void AppendLine(StringBuilder sb, RoundEvent e)
        {
            sb.Append(Num(e.At)).Append(' ');
            switch (e.Type)
            {
                case RoundEventType.Commit:
                    sb.Append("commit pod=").Append(Num(e.Pod)).Append(" slot=").Append(Num(e.Slot));
                    break;
                case RoundEventType.Reveal:
                    sb.Append("reveal pod=").Append(Num(e.Pod)).Append(" mode=").Append(e.Mode == RoundMystery.Sealed ? "sealed" : "depth");
                    break;
                case RoundEventType.Bridge:
                    sb.Append("bridge cell=").Append(Num(e.Cell));
                    break;
                case RoundEventType.Depart:
                    sb.Append("depart id=").Append(Num(e.Id)).Append(" pod=").Append(Num(e.Pod)).Append(" cell=").Append(Num(e.Cell))
                        .Append(" arrive=").Append(Num(e.ArriveAt)).Append(" clear=").Append(Num(e.ClearAt)).Append(" done=").Append(Num(e.DoneAt))
                        .Append(" route=").Append(Num(e.Route?.Count ?? 0));
                    break;
                case RoundEventType.Arrive:
                    sb.Append("arrive id=").Append(Num(e.Id));
                    break;
                case RoundEventType.Clear:
                    sb.Append("clear id=").Append(Num(e.Id)).Append(" cell=").Append(Num(e.Cell));
                    break;
                case RoundEventType.Return:
                    sb.Append("return id=").Append(Num(e.Id));
                    break;
                case RoundEventType.Complete:
                    sb.Append("complete pod=").Append(Num(e.Pod));
                    break;
                case RoundEventType.ExtraSlot:
                    sb.Append("extra-slot slot=").Append(Num(e.Slot));
                    break;
                case RoundEventType.Recall:
                    sb.Append("recall pod=").Append(Num(e.Pod)).Append(" slot=").Append(Num(e.Slot)).Append(" column=").Append(Num(e.Column));
                    break;
                case RoundEventType.Shuffle:
                    sb.Append("shuffle changed=").Append(Num(e.Count));
                    break;
                case RoundEventType.Burst:
                    sb.Append("burst code=").Append(e.Code).Append(" cells=").Append(Num(e.Count));
                    break;
                case RoundEventType.BurstClear:
                    sb.Append("burst-clear cell=").Append(Num(e.Cell));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e.Type, "Unknown round event.");
            }

            sb.Append('\n');
        }

        /// <summary>The canonical text of a list of events: each line with its '\n'.</summary>
        public static string Text(IEnumerable<RoundEvent> events)
        {
            var sb = new StringBuilder();
            foreach (RoundEvent e in events)
            {
                AppendLine(sb, e);
            }

            return sb.ToString();
        }

        /// <summary>SHA-256 of the UTF-8 text, written <c>sha256:&lt;64 lowercase hex&gt;</c>.</summary>
        public static string Sha256(string text)
        {
            byte[] hash;
            using (SHA256 sha = SHA256.Create())
            {
                hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            }

            var sb = new StringBuilder(7 + hash.Length * 2);
            sb.Append("sha256:");
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        /// <summary>The SHA-256 of a window's canonical event text.</summary>
        public static string WindowHash(IEnumerable<RoundEvent> events) => Sha256(Text(events));

        /// <summary>
        /// The extended signature: the reference's <c>signature(state)</c> followed by <c>|now|steps</c>, with
        /// <c>now</c> in µs and <c>steps</c> as <c>at:type:id-or-pod</c> joined by commas in step order. When a booster
        /// changed the columns, <c>|columns</c> follows (each column's pods joined by commas, columns by ';'); that never
        /// happens in the conformance fixtures.
        /// </summary>
        public static string Signature(RoundTimeline timeline)
        {
            RoundState s = timeline.State;
            var sb = new StringBuilder(s.Board.Length + 512);
            s.AppendSignature(sb);
            sb.Append('|').Append(Num(timeline.Now)).Append('|');
            IReadOnlyList<RoundStep> steps = timeline.Steps;
            for (int i = 0; i < steps.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(Num(steps[i].At)).Append(':').Append(RoundStep.TypeText(steps[i].Type)).Append(':').Append(Num(steps[i].Key));
            }

            if (s.ColumnsChanged)
            {
                sb.Append('|');
                for (int c = 0; c < s.Columns.Length; c++)
                {
                    if (c > 0)
                    {
                        sb.Append(';');
                    }

                    RoundState.AppendJoined(sb, s.Columns[c].ToArray());
                }
            }

            return sb.ToString();
        }

        /// <summary>The SHA-256 of the extended signature.</summary>
        public static string SignatureHash(RoundTimeline timeline) => Sha256(Signature(timeline));

        private static string Num(long value) => value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Collects the canonical log of a replay window by window: everything the timeline emits from one command up to the
    /// next one. Call <see cref="Add"/> after every command and advance with the events it produced.
    /// </summary>
    public sealed class RoundEventLog
    {
        private readonly StringBuilder _window = new StringBuilder();

        /// <summary>Appends the events (normally <see cref="RoundTimeline.LastEvents"/> or a command's result).</summary>
        public void Add(IEnumerable<RoundEvent> events)
        {
            foreach (RoundEvent e in events)
            {
                RoundEvents.AppendLine(_window, e);
            }
        }

        /// <summary>The canonical text of the current window.</summary>
        public string Current => _window.ToString();

        /// <summary>Ends the current window: returns its text and starts an empty one.</summary>
        public string Take()
        {
            string text = _window.ToString();
            _window.Clear();
            return text;
        }
    }
}
