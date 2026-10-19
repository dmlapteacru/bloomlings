using System.Text.Json;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;

const string channels = "GPBACD";
var groups = new[] { ColorGroup.Green, ColorGroup.PinkPurple, ColorGroup.BlueCyan, ColorGroup.BrownOrange, ColorGroup.BlueCyan, ColorGroup.BrownOrange };
var families = new[] { Family.Sprig, Family.Bloom, Family.Drop, Family.Twig, Family.Drop, Family.Twig };
var variants = channels.Select(c => new VariantId("pilot_" + char.ToLowerInvariant(c))).ToArray();
var catalog = new VariantCatalog(variants.Select((id, i) => new VariantInfo(id, families[i], groups[i], "#888888", id.Key, VariantStatus.Launch, 1)));
var noHooks = Array.Empty<IRoundHook>();
using var document = JsonDocument.Parse(File.ReadAllText(args[0]));
var levels = document.RootElement.GetProperty("levels").EnumerateArray().ToDictionary(l => l.GetProperty("id").GetInt32());
int moves = 0, bridges = 0, cases = 0;
foreach (var test in document.RootElement.GetProperty("cases").EnumerateArray())
{
    var level = levels[test.GetProperty("level").GetInt32()];
    int w = level.GetProperty("w").GetInt32(), h = level.GetProperty("h").GetInt32(), number = level.GetProperty("id").GetInt32();
    var grid = level.GetProperty("rowsBottomFirst").EnumerateArray()
        .Select(row => (IReadOnlyList<int>)row.GetString()!.Select(c => c == '.' ? -1 : c == '#' ? -2 : channels.IndexOf(c)).ToArray()).ToArray();
    var roles = channels.Select((c, i) => new PictureRole(c.ToString(), c.ToString(), groups[i], c == 'B')).ToArray();
    var picture = new BasePicture("pilot_" + number, 1, "pilot", w, h, roles, grid,
        new FinishedLook(FinishedLookMode.Auto, null), new PictureTags(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()), null,
        new PictureReview(ReviewStatus.Approved, "pilot", "2026-10-19", "test-only"), new PictureSource(PictureSourceKind.Hand, null, "project"));
    var pods = level.GetProperty("pods").EnumerateArray().Select((p, i) => new PodDef("p" + i, variants[channels.IndexOf(p.GetProperty("color").GetString()![0])], p.GetProperty("count").GetInt32(), false, null, null)).ToArray();
    var originalStacks = level.GetProperty("tray").EnumerateArray().Select(s => s.EnumerateArray().Select(p => p.GetInt32()).ToArray()).ToArray();
    var tray = originalStacks.Select(s => (IReadOnlyList<string>)s.Select(p => "p" + p).ToArray()).ToArray();
    var entries = level.GetProperty("entries").EnumerateArray().Select(e => new EntryDef(CellPos.FromIndex(e.GetInt32(), w), EntrySide.Bottom)).ToArray();
    var definition = new LevelDefinition(number, 1, (ulong)number, "browser-pilot-v1", new PictureRef(picture.Id, 1, Mirror.None, "solid"),
        channels.Select((c, i) => (c, i)).ToDictionary(p => p.c.ToString(), p => variants[p.i]), entries,
        Array.Empty<CellOverlay>(), Array.Empty<SpecialDef>(), Array.Empty<LockDef>(), new SlotsDef(5, null), new TrayDef(tray), pods,
        new DifficultyDef(DifficultyClass.Normal, 0, true), "pilot", Array.Empty<string>());
    var session = LevelSession.Load(definition, picture, new SessionOptions(1, 1000) { Catalog = catalog });
    int bridge = -1;
    JsonElement[] choices = level.TryGetProperty("hero", out var hero) ? hero.GetProperty("choices").EnumerateArray().ToArray() : Array.Empty<JsonElement>();

    bool BridgeReady(JsonElement choice) => bridge < 0 && session.State.Board.KindAt(choice.GetProperty("cell").GetInt32()) == CellKind.Stone
        && session.State.Board.Reach.Distances[choice.GetProperty("anchor").GetInt32()] > 0;
    void EvaluatePilotStatus()
    {
        session.State.Status = StatusEvaluator.Evaluate(session.State, noHooks);
        if (session.Status != LevelStatus.Won && choices.Any(BridgeReady)) session.State.Status = LevelStatus.Playing;
    }
    string Signature()
    {
        var board = session.State.Board;
        string cells = new(Enumerable.Range(0, board.CellCount).Select(i => board.KindAt(i) == CellKind.Open ? '.' : board.KindAt(i) == CellKind.Stone ? '#' : channels[Array.IndexOf(variants, board.TopLayer(i))]).ToArray());
        string remaining = string.Join(',', session.State.Pods.Select(p => p.Remaining));
        string heads = string.Join(',', originalStacks.Select((s, c) => s.Length - session.State.Tray.CountIn(c)));
        string slots = string.Join(',', Enumerable.Range(0, 5).Select(session.State.Slots.PodIn));
        string active = string.Join(',', session.State.Slots.OccupiedByAge().Select(session.State.Slots.PodIn));
        return string.Join('|', cells, remaining, heads, slots, active, bridge.ToString(System.Globalization.CultureInfo.InvariantCulture), session.Status.ToString());
    }
    EvaluatePilotStatus();
    var expected = test.GetProperty("signatures").EnumerateArray().Select(s => s.GetString()!).ToArray();
    int at = 0;
    if (Signature() != expected[at++]) throw new InvalidOperationException($"Initial state mismatch L{number} case {cases}.");
    foreach (var action in test.GetProperty("actions").EnumerateArray())
    {
        if (action.GetProperty("type").GetString() == "bridge")
        {
            int choice = action.GetProperty("choice").GetInt32();
            if (choice < 0 || choice >= choices.Length || !BridgeReady(choices[choice])) throw new InvalidOperationException("Illegal bridge action in transcript.");
            session.State.OpenNonTargetCell(choices[choice].GetProperty("cell").GetInt32());
            bridge = choice; Settler.Settle(session.State, noHooks, null); bridges++;
        }
        else
        {
            var result = session.Apply(new TapPod("p" + action.GetProperty("pod").GetInt32()));
            if (!result.Accepted) throw new InvalidOperationException($"C# refused L{number}: {result.Reason}.");
        }
        EvaluatePilotStatus(); moves++;
        string actual = Signature();
        if (actual != expected[at++]) throw new InvalidOperationException($"State mismatch L{number} case {cases} move {at - 1}.\nExpected: {expected[at-1]}\nActual: {actual}");
    }
    cases++;
}
Console.WriteLine($"PASS: {cases} C#/JavaScript transcript cases, {moves} accepted actions, {bridges} explicit bridge adapter actions. Exact board, remaining work, physical slots, age order, tray and status agree.");
