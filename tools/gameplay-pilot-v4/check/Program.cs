using System.Text.Json;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;

const string channels = "GPBACDV";
var groups = new[] { ColorGroup.Green, ColorGroup.PinkPurple, ColorGroup.BlueCyan, ColorGroup.BrownOrange, ColorGroup.BlueCyan, ColorGroup.BrownOrange, ColorGroup.PinkPurple };
var families = new[] { Family.Sprig, Family.Bloom, Family.Drop, Family.Twig, Family.Drop, Family.Twig, Family.Bloom };
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
    var pods = level.GetProperty("pods").EnumerateArray().Select((p, i) => new PodDef("p" + i, variants[channels.IndexOf(p.GetProperty("color").GetString()![0])], p.GetProperty("count").GetInt32(), p.TryGetProperty("mystery", out var mystery) && mystery.GetBoolean(), null, p.TryGetProperty("group", out var group) ? group.GetString() : null)).ToArray();
    var originalStacks = level.GetProperty("tray").EnumerateArray().Select(s => s.EnumerateArray().Select(p => p.GetInt32()).ToArray()).ToArray();
    var tray = originalStacks.Select(s => (IReadOnlyList<string>)s.Select(p => "p" + p).ToArray()).ToArray();
    var entries = level.GetProperty("entries").EnumerateArray().Select(e => new EntryDef(CellPos.FromIndex(e.GetInt32(), w), EntrySide.Bottom)).ToArray();
    var definition = new LevelDefinition(number, 1, (ulong)number, "browser-pilot-v4", new PictureRef(picture.Id, 1, Mirror.None, "solid"),
        channels.Select((c, i) => (c, i)).ToDictionary(p => p.c.ToString(), p => variants[p.i]), entries,
        Array.Empty<CellOverlay>(), Array.Empty<SpecialDef>(), Array.Empty<LockDef>(), new SlotsDef(5, null), new TrayDef(tray), pods,
        new DifficultyDef(DifficultyClass.Normal, 0, true), "pilot", Array.Empty<string>());
    var session = LevelSession.Load(definition, picture, new SessionOptions(1, 1000) { Catalog = catalog });
    int bridge = -1;
    int capacity = level.TryGetProperty("capacity", out var places) ? places.GetInt32() : 5;
    // Explicit experimental reservation adapter. Core still performs all work,
    // atomic group admission, age-ordered allocation and reveals. Its independent
    // releases cannot change work order: removed zero-work pods claim no cells.
    // This adapter reserves their display/capacity slots until the partner ends.
    var reserved = Enumerable.Repeat(-1, capacity).ToArray();
    var ages = new int[pods.Length]; int serial = 0;
    int[] Members(int p) => pods[p].ConnectedGroupId is string id
        ? Enumerable.Range(0, pods.Length).Where(i => pods[i].ConnectedGroupId == id).ToArray() : new[] { p };
    void ReleaseReservations()
    {
        for (int i = 0; i < reserved.Length; i++)
        {
            int p = reserved[i];
            if (p >= 0 && Members(p).All(m => session.State.Pods[m].Remaining == 0)) reserved[i] = -1;
        }
    }
    JsonElement[] choices = level.TryGetProperty("hero", out var hero) ? hero.GetProperty("choices").EnumerateArray().ToArray() : Array.Empty<JsonElement>();

    bool BridgeReady(JsonElement choice) => bridge < 0 && session.State.Board.KindAt(choice.GetProperty("cell").GetInt32()) == CellKind.Stone
        && session.State.Board.Reach.Distances[choice.GetProperty("anchor").GetInt32()] > 0;
    void EvaluatePilotStatus()
    {
        ReleaseReservations();
        // Explicit public-visibility adapter: expose each new top before choice.
        for (int c = 0; c < originalStacks.Length; c++)
        {
            int p = session.State.Tray.TopOf(c);
            if (p >= 0 && !session.State.Pods[p].VariantRevealed) session.State.RevealPod(p);
        }
        if (session.State.Board.CountAllLayers() == 0) { session.State.Status = LevelStatus.Won; return; }
        if (choices.Any(BridgeReady)) { session.State.Status = LevelStatus.Playing; return; }
        int free = reserved.Count(p => p < 0);
        if (free == 0) { session.State.Status = LevelStatus.Jammed; return; }
        bool legal = Enumerable.Range(0, originalStacks.Length).Select(session.State.Tray.TopOf)
            .Any(p => p >= 0 && Members(p).Length <= free && session.State.CanCommit(p) == null);
        session.State.Status = legal ? LevelStatus.Playing : LevelStatus.Stuck;
    }
    string Signature()
    {
        var board = session.State.Board;
        string cells = new(Enumerable.Range(0, board.CellCount).Select(i => board.KindAt(i) == CellKind.Open ? '.' : board.KindAt(i) == CellKind.Stone ? '#' : channels[Array.IndexOf(variants, board.TopLayer(i))]).ToArray());
        string remaining = string.Join(',', session.State.Pods.Select(p => p.Remaining));
        string heads = string.Join(',', originalStacks.Select((s, c) => s.Length - session.State.Tray.CountIn(c)));
        string slots = string.Join(',', reserved);
        string active = string.Join(',', reserved.Where(p => p >= 0).OrderBy(p => ages[p]));
        string revealed = string.Concat(session.State.Pods.Select(p => p.VariantRevealed ? "1" : "0"));
        return string.Join('|', cells, remaining, heads, slots, active, revealed, bridge.ToString(System.Globalization.CultureInfo.InvariantCulture), session.Status.ToString());
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
            int tapped = action.GetProperty("pod").GetInt32();
            int[] members = Members(tapped);
            if (reserved.Count(p => p < 0) < members.Length) throw new InvalidOperationException("Group exceeds reservation capacity.");
            foreach (int p in members)
            {
                int slot = Array.FindIndex(reserved, value => value < 0);
                reserved[slot] = p; ages[p] = ++serial;
            }
            var result = session.Apply(new TapPod("p" + tapped));
            if (!result.Accepted) throw new InvalidOperationException($"C# refused L{number}: {result.Reason}.");
        }
        EvaluatePilotStatus(); moves++;
        string actual = Signature();
        if (actual != expected[at++]) throw new InvalidOperationException($"State mismatch L{number} case {cases} move {at - 1}.\nExpected: {expected[at-1]}\nActual: {actual}");
    }
    cases++;
}
Console.WriteLine($"PASS: {cases} C#/JavaScript transcript cases, {moves} accepted actions, {bridges} bridge actions. Native exact work, atomic groups, reveals and tray agree; explicit adapters agree on capacity, top reveal, co-reserved slots, age order, bridge and status. Shipping Core is unchanged.");
