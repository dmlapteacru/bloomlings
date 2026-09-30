// The playtest client's engine-free check (see the project file): animator replays and the meta layer.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Golden;
using Bloomlings.Content.Json;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
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

foreach (string folder in new[] { "content/showcase", "content/work/pt/b11", "content/work/pt/b26", "content/work/pt/b51" })
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
Check(meta.DailyReward.IsUnlocked && meta.DailyReward.CanClaim, "the Daily Reward opens at L7 and can be claimed");
int petalsBefore = meta.Economy.Petals;
int claimed = meta.DailyReward.Claim();
Check(claimed > 0 && meta.Economy.Petals == petalsBefore + claimed && !meta.DailyReward.CanClaim, "claiming pays once a day");
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
    for (int i = 0; i < 2000 && !animator.Idle; i++) animator.Advance(1f / 60f, session.View);
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

    for (int i = 0; i < view.SlotCapacity; i++)
    {
        string? pod = view.PodInSlot(i);
        SlotLook look = animator.Slots[i];
        if (look.PodId != pod) { problems.Add($"{at}: slot {i} shows {look.PodId}, is {pod}"); continue; }
        if (pod != null && (look.Count != view.Pod(pod).Remaining || look.Variant != view.Pod(pod).Variant))
            problems.Add($"{at}: slot {i} shows {look.Count}/{look.Variant}, is {view.Pod(pod).Remaining}/{view.Pod(pod).Variant}");
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
