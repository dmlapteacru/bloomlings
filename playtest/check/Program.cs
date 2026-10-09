// The playtest client's engine-free check (see the project file): animator replays and the meta layer.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Golden;
using Bloomlings.Content.Json;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Search;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Playtest;

string Root = FindRoot();
int cases = 0, failures = 0;

var pictures = Directory.GetFiles(Path.Combine(Root, "content/pictures/lib"), "*.json").Select(f => BasePictureJson.Read(File.ReadAllText(f))).Select(p => p with { Review = p.Review with { Status = ReviewStatus.Approved } }).ToDictionary(p => p.Id);
var runs = new List<(string Name, LevelDefinition Level, BasePicture Picture, IReadOnlyList<Command> Commands, int Budget)>();
foreach (string file in Directory.GetFiles(Path.Combine(Root, "core/tests/golden"), "*.golden.json"))
{
    GoldenCase g = GoldenCase.Read(File.ReadAllText(file));
    runs.Add((g.Name, g.Definition, g.Picture, g.Commands, g.ShuffleNodeBudget));
}

// The showcases and the local playtest preview batches (content/work is gitignored; pt5 is the 2026-10-05 run at the
// bigger band sizes: the band batches and the levels filled in one by one).
var folders = new List<string> { "content/showcase" };
string preview = Path.Combine(Root, "content/work/pt5");
if (Directory.Exists(preview))
{
    folders.AddRange(Directory.GetDirectories(preview, "*", SearchOption.AllDirectories)
        .Where(d => Directory.Exists(Path.Combine(d, "levels")))
        .Select(d => Path.GetRelativePath(Root, d))
        .OrderBy(d => d, StringComparer.Ordinal));
}

foreach (string folder in folders)
{
    string levels = Path.Combine(Root, folder, "levels");
    if (!Directory.Exists(levels)) continue;
    foreach (string file in Directory.GetFiles(levels, "*.json"))
    {
        LevelDefinition level = DefinitionJson.Read(File.ReadAllText(file));
        string record = Path.Combine(Root, folder, "validation", Path.GetFileName(file));
        if (!File.Exists(record)) continue;
        ValidationRecord v = ValidationRecord.Read(File.ReadAllText(record));
        runs.Add((folder + "/L" + level.LevelNumber, level, pictures[level.Picture.Id], v.SolutionTrace, 20000));
        if (v.JamWitness != null) runs.Add((folder + "/L" + level.LevelNumber + "-jam", level, pictures[level.Picture.Id], v.JamWitness, 20000));
    }
}

// A big level's 22×28 icons board (spec 001 FR-008 and FR-036 as amended on 2026-10-06): the generator tests' fixture,
// made before the picture library had big pictures. Its winning line comes from the core's search.
string fixtures = Path.Combine(Root, "core/tests/Bloomlings.Generator.Tests/Fixtures");
if (File.Exists(Path.Combine(fixtures, "big-level.level.json")))
{
    LevelDefinition big = DefinitionJson.Read(File.ReadAllText(Path.Combine(fixtures, "big-level.level.json")));
    BasePicture bigPicture = BasePictureJson.Read(File.ReadAllText(Path.Combine(fixtures, "big-level.picture.json")));
    SearchResult line = StateSearch.Find(LevelSession.Load(big, bigPicture, new SessionOptions(1, 20000)), StateSearch.IsWon, MoveOrder.ProgressFirst, 20000);
    if (line.Outcome != SearchOutcome.Found)
    {
        failures++;
        Console.WriteLine("FAIL the 22×28 fixture has no winning line");
    }

    runs.Add(("fixture/big-level-22x28", big, bigPicture, line.Path, 20000));
}

foreach (var run in runs)
{
    foreach (bool rapid in new[] { false, true })
    {
        cases++;
        LevelSession session = LevelSession.Load(run.Level, run.Picture, new SessionOptions(1, run.Budget));
        var animator = new LevelAnimator();
        animator.Reset(session.View);
        var problems = new List<string>();
        int step = 0;
        foreach (Command command in run.Commands)
        {
            step++;
            if (!session.Check(command).IsAllowed) { continue; } // A refused tap changes nothing (golden refusal cases).
            if (command is TapPod tap)
            {
                var before = new Dictionary<string, (int, Bloomlings.Core.Variants.VariantId?, float, float)>();
                foreach (string m in session.View.ConnectedGroup(tap.PodId).Append(tap.PodId))
                {
                    PodInfo p = session.View.Pod(m);
                    before[m] = (p.Remaining, p.Variant, 0f, 0f);
                }
                CommandResult result = session.Apply(command);
                animator.Tapped(result, session.View, before);
            }
            else if (command is Restart)
            {
                session.Apply(command);
                animator.Reset(session.View);
            }
            else
            {
                animator.Flush(session.View);
                CommandResult result = session.Apply(command);
                animator.Boosted(result, session.View);
            }

            if (rapid) animator.Advance(0.03f, session.View);
            else Settle(animator, session, problems, $"step {step} {command}");
        }

        Settle(animator, session, problems, "end");
        if (problems.Count > 0)
        {
            failures++;
            Console.WriteLine($"FAIL {run.Name} rapid={rapid}: {string.Join(" | ", problems.Take(4))}");
        }
    }
}

Console.WriteLine($"animator: {cases} runs, {failures} failed");

// Two taps in quick succession (the owner's report of 2026-10-03): the second pod shows in a slot of its own while the
// first still shows, and the two taps' waves play side by side unless one waits for cells the other changes.
int pairs = 0, together = 0, sameSlot = 0, working = 0;
foreach (var run in runs)
{
    var taps = run.Commands.OfType<TapPod>().Take(2).ToList();
    if (taps.Count < 2) continue;
    LevelSession session = LevelSession.Load(run.Level, run.Picture, new SessionOptions(1, run.Budget));
    var animator = new LevelAnimator();
    animator.Reset(session.View);
    var shown = new List<string>();
    bool ok = true;
    int withWork = 0;
    foreach (TapPod tap in taps)
    {
        if (!session.Check(tap).IsAllowed) { ok = false; break; }
        var before = new Dictionary<string, (int, Bloomlings.Core.Variants.VariantId?, float, float)>();
        foreach (string m in session.View.ConnectedGroup(tap.PodId).Append(tap.PodId))
        {
            PodInfo p = session.View.Pod(m);
            before[m] = (p.Remaining, p.Variant, 0f, 0f);
        }
        CommandResult tapped = session.Apply(tap);
        withWork += tapped.Events.Any(e => e is TileCleared) ? 1 : 0;
        animator.Tapped(tapped, session.View, before);
        shown.Add(tap.PodId);
        animator.Advance(0.03f, session.View);
    }
    if (!ok) continue;
    pairs++;
    int first = animator.PlaceOf(shown[0]), second = animator.PlaceOf(shown[1]);
    if (first >= 0 && first == second) sameSlot++;
    bool both = false;
    for (int i = 0; i < 2000 && !animator.Idle; i++)
    {
        both |= animator.Playing >= 2;
        animator.Advance(1f / 60f, session.View);
    }
    if (withWork == 2)
    {
        working++;
        if (both) together++;
    }
}
Console.WriteLine($"two quick taps: {pairs} levels ({working} with work for both), waves side by side in {together}, the same slot shown in {sameSlot}");
if (sameSlot > 0 || together < working) { failures++; Console.WriteLine("FAIL two quick taps"); }

// The owner's L1 (2026-10-04): all three pods tapped at once. A Bloomling sets off as soon as its own route is clear, so
// the leaf pod's first Bloomlings walk while the first water pod's still play, not after it finishes.
{
    LevelDefinition l1 = DefinitionJson.Read(File.ReadAllText(Path.Combine(Root, "content/catalog/levels/level-00001.json")));
    LevelSession session = LevelSession.Load(l1, pictures[l1.Picture.Id], new SessionOptions(1, 20000));
    var animator = new LevelAnimator();
    animator.Reset(session.View);
    foreach (string id in new[] { "w1", "w2", "l1" })
    {
        var before = new Dictionary<string, (int, Bloomlings.Core.Variants.VariantId?, float, float)>();
        foreach (string m in session.View.ConnectedGroup(id).Append(id))
        {
            PodInfo p = session.View.Pod(m);
            before[m] = (p.Remaining, p.Variant, 0f, 0f);
        }

        animator.Tapped(session.Apply(new TapPod(id)), session.View, before);
        animator.Advance(0.3f, session.View);
    }

    float leafGoes = float.NaN, waterDone = float.NaN;
    for (int i = 0; i < 4000 && !animator.Idle && (float.IsNaN(leafGoes) || float.IsNaN(waterDone)); i++)
    {
        if (float.IsNaN(leafGoes) && animator.Walkers.Any(w => w.Variant == l1.Pods.First(p => p.Id == "l1").Variant && animator.Now >= w.Start)) leafGoes = animator.Now;
        if (float.IsNaN(waterDone) && animator.Slots.Any(look => look.PodId == "w1" && look.IsLeaving)) waterDone = animator.Now;
        animator.Advance(1f / 60f, session.View);
    }

    Console.WriteLine($"L1, three quick taps: the leaf pod's first Bloomling sets off at {leafGoes:0.00} s, the first water pod finishes at {waterDone:0.00} s");
    if (!(leafGoes < waterDone)) { failures++; Console.WriteLine("FAIL L1 three quick taps"); }
}

// The owner's report of 2026-10-05: quick taps must not stack pods behind the ones still working. The rules free a
// finished pod's slot at once, but a tap goes in only to a slot free on screen (LevelScreen.Tap), so the screen's free
// slots, not the rules', decide: right after L1's first water pod is tapped the rules have all five slots free again (its
// 44 tiles settle at once), while the screen shows it working in one of them until its Bloomlings are done.
{
    LevelDefinition l1 = DefinitionJson.Read(File.ReadAllText(Path.Combine(Root, "content/catalog/levels/level-00001.json")));
    LevelSession session = LevelSession.Load(l1, pictures[l1.Picture.Id], new SessionOptions(1, 20000));
    var animator = new LevelAnimator();
    animator.Reset(session.View);
    int RulesFree() => Enumerable.Range(0, session.View.SlotCapacity).Count(i => session.View.SlotStateOf(i) != SlotState.Locked && session.View.SlotStateOf(i) != SlotState.Absent && session.View.PodInSlot(i) == null);
    var before = new Dictionary<string, (int, Bloomlings.Core.Variants.VariantId?, float, float)> { ["w1"] = (session.View.Pod("w1").Remaining, session.View.Pod("w1").Variant, 0f, 0f) };
    animator.Tapped(session.Apply(new TapPod("w1")), session.View, before);
    animator.Advance(0.1f, session.View);
    int rulesFree = RulesFree(), screenFree = animator.FreeOnScreen(session.View);
    for (int i = 0; i < 4000 && !animator.Idle; i++) animator.Advance(1f / 60f, session.View);
    int settledFree = animator.FreeOnScreen(session.View);
    Console.WriteLine($"L1, a slot free on screen: after the first tap the rules have {rulesFree} free, the screen {screenFree}; once it settles {settledFree}");
    if (!(rulesFree == 5 && screenFree == 4 && settledFree == 5)) { failures++; Console.WriteLine("FAIL a slot free on screen"); }
}

// The meta layer on the shared client services.
string temp = Path.Combine(Path.GetTempPath(), "pt-meta-" + Guid.NewGuid().ToString("N"));
var meta = new PlaytestMeta(temp);
Check(meta.FirstLaunch && meta.CurrentLevel == 1, "a new profile starts at L1");
Check(!meta.Economy.IsUnlocked(BoosterKind.ExtraSlot), "boosters are locked at L1");
meta.CompleteLevel(1, DifficultyClass.Normal, 0);
meta.CompleteLevel(2, DifficultyClass.Normal, 0);
Check(meta.CurrentLevel == 3 && meta.Economy.IsUnlocked(BoosterKind.ExtraSlot) && meta.Economy.Charges(BoosterKind.ExtraSlot) >= 1, "Extra Slot opens at L3 with a free charge");
Check(!meta.Economy.IsUnlocked(BoosterKind.Shuffle), "Shuffle still locked at L3");
int petals = meta.Economy.Petals;
Check(petals > 0, "wins pay Petals");
Check(meta.CompleteLevel(5, DifficultyClass.Normal, 0) == null, "only the current level can be completed");
meta.SkipTo(24);
WinPayout? payout = meta.CompleteLevel(25, DifficultyClass.Normal, 0);
Check(payout?.Milestone != null && payout.Milestone.Level == 25, "L25 pays its milestone");
Check(meta.DailyReward.IsUnlocked && meta.DailyReward.CanClaim && meta.DailyReward.ShowsBadge, "the Daily Reward opens at L7 with its steps and Home's \"!\"");
int petalsBefore = meta.Economy.Petals;
int claimed = 0;
foreach (DailyRewardStep step in meta.DailyReward.Steps)
{
    claimed += meta.DailyReward.Claim(step.Number, adWatched: step.Ad);
}

Check(claimed == 220 && meta.Economy.Petals == petalsBefore + claimed && !meta.DailyReward.CanClaim, "the five steps pay 20 + 30 + 40 + 50 + 80 Petals once a day");
LevelDefinition level26 = runs.First(r => r.Level.LevelNumber > 0).Level;
meta.CompleteLevel(26, DifficultyClass.Normal, 0, level26);
Check(meta.Collection.Count == 1 && meta.Collection.Entries[0].LevelNumber == 26, "a won picture joins the Collection");
meta.SkipTo(40);
Check(meta.Wardrobe.IsAvailable, "the Wardrobe opens at L40");
var reloaded = new PlaytestMeta(temp);
Check(!reloaded.FirstLaunch && reloaded.CurrentLevel == 41 && reloaded.Economy.Petals == meta.Economy.Petals && reloaded.Collection.Count == 1, "progress survives a restart");
var fresh = PlaytestMeta.ResetProfile(temp);
Check(fresh.CurrentLevel == 1 && fresh.Economy.Petals == 0, "reset starts over");
Directory.Delete(temp, true);
Console.WriteLine(failures == 0 && metaOk ? "ALL OK" : "FAILURES");
return failures == 0 && metaOk ? 0 : 1;

static string FindRoot()
{
    DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, "core", "Bloomlings.sln")))
    {
        dir = dir.Parent;
    }

    return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
}

void Settle(LevelAnimator animator, LevelSession session, List<string> problems, string at)
{
    // Up to 8 minutes of play (the owner's calm pace of 2026-10-06; all of a level's taps may be queued at once here).
    for (int i = 0; i < 30000 && !animator.Idle; i++) animator.Advance(1f / 60f, session.View);
    if (!animator.Idle) { problems.Add(at + ": never idle"); return; }
    LevelView view = session.View;
    for (int y = 0; y < view.Height; y++)
    for (int x = 0; x < view.Width; x++)
    {
        var pos = new CellPos(x, y);
        CellInfo shown = animator.Cell(pos), real = view.Cell(pos);
        if (shown.Kind != real.Kind || shown.Visible != real.Visible || shown.MysteryHidden != real.MysteryHidden || shown.KeyId != real.KeyId
            || (real.Kind == CellKind.Target && shown.RemainingLayers != real.RemainingLayers))
            problems.Add($"{at}: cell {pos} shows {shown.Kind}/{shown.Visible}/{shown.KeyId}/{shown.RemainingLayers}, is {real.Kind}/{real.Visible}/{real.KeyId}/{real.RemainingLayers}");
    }

    // A pod shows in the first slot free on screen (LevelAnimator.Place), not always in its slot in the rules: every pod
    // the rules hold shows once, in a usable slot, with its count and variant; no other pod shows.
    var held = new HashSet<string>();
    for (int i = 0; i < view.SlotCapacity; i++)
    {
        string? pod = view.PodInSlot(i);
        if (pod == null) continue;
        held.Add(pod);
        int place = animator.PlaceOf(pod);
        if (place < 0) { problems.Add($"{at}: pod {pod} of slot {i} does not show"); continue; }
        SlotLook look = animator.Slots[place];
        if (look.Count != view.Pod(pod).Remaining || look.Variant != view.Pod(pod).Variant)
            problems.Add($"{at}: slot {place} shows {look.Count}/{look.Variant}, is {view.Pod(pod).Remaining}/{view.Pod(pod).Variant}");
        if (view.SlotStateOf(place) == SlotState.Locked || view.SlotStateOf(place) == SlotState.Absent)
            problems.Add($"{at}: pod {pod} shows in the unusable slot {place}");
    }

    for (int i = 0; i < animator.Slots.Length; i++)
    {
        SlotLook look = animator.Slots[i];
        if (look.PodId != null && !held.Contains(look.PodId)) problems.Add($"{at}: slot {i} shows {look.PodId}, which the rules no longer hold");
        if (look.Pending.Count > 0) problems.Add($"{at}: slot {i} still has a queue");
    }

    if (animator.HeldPodLocks.Count > 0 || animator.HeldSlotLocks.Count > 0) problems.Add($"{at}: locks still held");
    foreach (SpecialInfo s in view.Specials)
    {
        var (p, t, tr) = animator.Special(s.Id);
        if (tr != s.Triggered || (!s.Triggered && p != s.Progress)) problems.Add($"{at}: special {s.Id} shows {p}/{t}/{tr}, is {s.Progress}/{s.Total}/{s.Triggered}");
    }
}

void Check(bool ok, string what)
{
    if (!ok) { metaOk = false; Console.WriteLine("META FAIL: " + what); }
}

partial class Program { static bool metaOk = true; }
