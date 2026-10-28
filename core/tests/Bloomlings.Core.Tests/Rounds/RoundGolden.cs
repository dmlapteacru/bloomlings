using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>
    /// A round golden replay case (<c>core/tests/golden/rounds/rounds-*.golden.json</c>, spec 006 T024): a reference
    /// engine definition, recorded commands with their model times (on the timeline's own clock) and the expected end.
    /// The replay log has the canonical event lines (<see cref="RoundEvents"/>) and one line per command
    /// (<c>&gt; tap pod=3 at=0</c>, or <c>&gt; … refused:&lt;reason&gt;</c>) where it is given; its SHA-256 is the events
    /// digest, the extended signature's SHA-256 the state hash.
    /// </summary>
    internal sealed class RoundGolden
    {
        private readonly JsonObject _json;

        private RoundGolden(JsonObject json)
        {
            _json = json;
        }

        public static string Folder => Path.Combine(Golden.GoldenReplayTests.GoldenFolder, "rounds");

        public static RoundGolden Read(string text) => new RoundGolden(JsonNode.Parse(text)!.AsObject());

        public string Name => (string)_json["name"]!;

        public string? ExpectedEvents => (string?)_json["expectedEventsSha256"];

        public string? ExpectedState => (string?)_json["expectedSignatureSha256"];

        public string? ExpectedStatus => (string?)_json["expectedStatus"];

        public long? ExpectedEndUs => (long?)_json["expectedEndUs"];

        public RoundLevel Level()
        {
            using JsonDocument doc = JsonDocument.Parse(_json["definition"]!.ToJsonString());
            return RoundTestLevels.FromDefinition((int)_json["number"]!, doc.RootElement);
        }

        public IReadOnlyList<RoundInput> Commands => _json["commands"]!.AsArray().Select(c => RoundInput.Parse((string)c!)).ToArray();

        /// <summary>The outcome of a replay: status, end time, the state hash and the log with its digest.</summary>
        public sealed class Outcome
        {
            public string Status = "";
            public long EndUs;
            public string State = "";
            public string Events = "";
            public string Log = "";
        }

        public Outcome Run()
        {
            var t = new RoundTimeline(Level());
            var events = new RoundEventLog();
            var replay = new RoundReplay(t, events);
            var log = new StringBuilder();
            foreach (RoundInput c in Commands)
            {
                replay.AdvanceTo(c.AtUs);
                log.Append(events.Take());
                RoundCommitResult r = RoundReplay.Apply(t, c);
                log.Append("> ").Append(c).Append(r.Accepted ? "" : " refused:" + r.Reason).Append('\n');
                log.Append(RoundEvents.Text(r.Events));
            }

            replay.Finish();
            log.Append(events.Take());
            string text = log.ToString();
            return new Outcome
            {
                Status = RoundState.StatusText(t.State.Status),
                EndUs = t.Now,
                State = RoundEvents.SignatureHash(t),
                Events = RoundEvents.Sha256(text),
                Log = text,
            };
        }

        /// <summary>The case with the expected values of <paramref name="outcome"/>, in its stored form.</summary>
        public string WithOutcome(Outcome outcome)
        {
            _json["expectedStatus"] = outcome.Status;
            _json["expectedEndUs"] = outcome.EndUs;
            _json["expectedSignatureSha256"] = outcome.State;
            _json["expectedEventsSha256"] = outcome.Events;
            return Write();
        }

        /// <summary>The stored form: two-space indent, the fields in their written order, a final line end.</summary>
        public string Write() => _json.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n";
    }
}
