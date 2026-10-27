using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Tests.Golden;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>
    /// Round levels for the tests: the placed levels of <c>content/rounds/levels/</c> (their <c>definition</c> is the
    /// reference engine's input), the reference fixture, and small hand-made boards.
    /// </summary>
    internal static class RoundTestLevels
    {
        public static string RepoRoot => Path.GetFullPath(Path.Combine(GoldenReplayTests.GoldenFolder, "..", "..", ".."));

        public static string FixturePath => Path.Combine(RepoRoot, "core", "tests", "Bloomlings.Core.Tests", "Rounds", "Fixtures", "reference-rounds.json");

        private static JsonDocument? _reference;

        /// <summary>The reference fixture written by <c>Fixtures/export-reference.cjs</c>.</summary>
        public static JsonElement Reference => (_reference ??= JsonDocument.Parse(File.ReadAllText(FixturePath))).RootElement;

        public static IEnumerable<JsonElement> ReferenceLevels => Reference.GetProperty("levels").EnumerateArray();

        public static JsonElement ReferenceLevel(int number) => ReferenceLevels.First(l => l.GetProperty("number").GetInt32() == number);

        /// <summary>A placed level from <c>content/rounds/levels/L####.json</c>, with its class, introduction and mechanics.</summary>
        public static RoundLevel Content(int number)
        {
            string path = Path.Combine(RepoRoot, "content", "rounds", "levels", "L" + number.ToString("0000") + ".json");
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = doc.RootElement;
            string cls = root.GetProperty("cls").GetString()!;
            string? intro = root.TryGetProperty("intro", out JsonElement i) && i.ValueKind == JsonValueKind.String ? i.GetString() : null;
            RoundException? exception = null;
            if (root.TryGetProperty("exception", out JsonElement e) && e.ValueKind == JsonValueKind.String)
            {
                exception = new RoundException(RoundLevelValidator.BridgeDecides, e.GetString()!, "owner", "2026-10-27");
            }

            return FromDefinition(number, root.GetProperty("definition"), cls, intro, exception);
        }

        /// <summary>A level from a reference fixture entry.</summary>
        public static RoundLevel FromReference(JsonElement level)
        {
            string? intro = level.GetProperty("intro").ValueKind == JsonValueKind.String ? level.GetProperty("intro").GetString() : null;
            return FromDefinition(level.GetProperty("number").GetInt32(), level.GetProperty("definition"), level.GetProperty("cls").GetString()!, intro, null);
        }

        /// <summary>A level from a reference engine definition (<c>color</c> or <c>code</c> per pod; <c>hero.choices</c>).</summary>
        public static RoundLevel FromDefinition(int number, JsonElement d, string cls = "Normal", string? intro = null, RoundException? exception = null)
        {
            var rows = d.GetProperty("rowsBottomFirst").EnumerateArray().Select(r => r.GetString()!).ToArray();
            var pods = new List<RoundPod>();
            foreach (JsonElement p in d.GetProperty("pods").EnumerateArray())
            {
                string code = p.TryGetProperty("color", out JsonElement c) ? c.GetString()! : p.GetProperty("code").GetString()!;
                RoundMystery mystery = RoundMystery.None;
                if (p.TryGetProperty("mystery", out JsonElement m))
                {
                    mystery = m.ValueKind == JsonValueKind.String && m.GetString() == "sealed" ? RoundMystery.Sealed : RoundMystery.Depth;
                }

                string? group = p.TryGetProperty("group", out JsonElement g) ? g.GetString() : null;
                pods.Add(new RoundPod(code[0], p.GetProperty("count").GetInt32(), mystery, group));
            }

            var tray = d.GetProperty("tray").EnumerateArray().Select(col => (IReadOnlyList<int>)col.EnumerateArray().Select(x => x.GetInt32()).ToArray()).ToArray();
            int[]? entries = d.TryGetProperty("entries", out JsonElement en) ? en.EnumerateArray().Select(x => x.GetInt32()).ToArray() : null;
            RoundBridgeChoice[]? bridge = null;
            if (d.TryGetProperty("hero", out JsonElement hero))
            {
                bridge = hero.GetProperty("choices").EnumerateArray()
                    .Select(ch => new RoundBridgeChoice(ch.GetProperty("cell").GetInt32(), ch.GetProperty("anchor").GetInt32(), ch.TryGetProperty("label", out JsonElement l) ? l.GetString()! : ""))
                    .ToArray();
            }

            var mechanics = new List<string>();
            if (pods.Any(p => p.Mystery != RoundMystery.None))
            {
                mechanics.Add("hidden");
            }

            if (pods.Any(p => p.Group != null))
            {
                mechanics.Add("links");
            }

            if (bridge != null)
            {
                mechanics.Add("bridge");
            }

            RoundIntroduction? introduction = null;
            if (intro != null)
            {
                var row = RoundLevelValidator.IntroductionTable.First(t => t.Mechanic == intro);
                introduction = new RoundIntroduction(intro, number == row.Second);
            }

            int capacity = d.TryGetProperty("capacity", out JsonElement cap) ? cap.GetInt32() : 5;
            return new RoundLevel(number, d.GetProperty("w").GetInt32(), d.GetProperty("h").GetInt32(), rows, pods, tray, entries, bridge, capacity,
                ClassOf(cls), introduction: introduction, mechanics: mechanics, exception: exception);
        }

        public static RoundClass ClassOf(string cls) => cls switch
        {
            "Hard" => RoundClass.Hard,
            "Super Hard" => RoundClass.SuperHard,
            _ => RoundClass.Normal,
        };

        public static RoundAction Action(JsonElement a) =>
            a.GetProperty("type").GetString() == "tap" ? RoundAction.Tap(a.GetProperty("pod").GetInt32()) : RoundAction.BuildBridge(a.GetProperty("choice").GetInt32());

        /// <summary>
        /// A hand-made board from rows written top first (easier to read), with the frame implied: the rows are used as
        /// given, the entry is the bottom row's cell at <paramref name="entryX"/>.
        /// </summary>
        public static RoundLevel Board(string[] rowsTopFirst, RoundPod[] pods, int[][] tray, int entryX, RoundBridgeChoice[]? bridge = null, int number = 1)
        {
            string[] rows = rowsTopFirst.Reverse().ToArray();
            int w = rows[0].Length;
            return new RoundLevel(number, w, rows.Length, rows, pods, tray.Select(c => (IReadOnlyList<int>)c).ToArray(), new[] { entryX }, bridge);
        }

        /// <summary>The cell index of (x, y), y counted from the bottom row.</summary>
        public static int Cell(RoundLevel level, int x, int y) => y * level.Width + x;
    }
}
