// Renders the full playtest's designed screens for each design board frame, checks them, and writes the asset inventory.
// See the project file. Usage: dotnet run --project playtest/preview [-- --out <dir>] [--inventory] [--frames 7,8,9]
// [--shape 19.5x9] (one screen shape only, for a quick look; the checks then cover that shape alone)
// [--before <sheet.png>] (also writes before-after.jpg: that sheet above the new one, spec 003 FR-029)
// [--sounds] (only writes the synthesized clips as WAV files and a listening schedule to <out>/sounds, spec 005 FR-042)
// [--perf] [--csv <file>] (only runs the frame-time harness, Perf.cs: a journey through the screens at 60 fps with the
// APK's picture cache, per screen ms a frame, C# picture rasters and cache misses; --csv also writes every frame;
// --perf-serial renders the pictures' rows on one thread, --perf-draw really draws the frames, --perf-verbose prints each,
// --perf-store <dir> keeps the made pictures there as the APK does between launches: run twice to see a later launch,
// --perf-allocs lists what the level's frames allocate by type)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Playtest;
using Bloomlings.Playtest.Design;
using Bloomlings.Playtest.Preview;
using SkiaSharp;

string root = FindRoot();
string outDir = Path.Combine(root, "playtest", "preview", "out");
bool inventory = false;
string? before = null;
HashSet<int>? only = null;
bool sounds = false;
string? shape = null;
bool perf = false;
string? csv = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--out" && i + 1 < args.Length)
    {
        outDir = Path.GetFullPath(args[++i]);
    }
    else if (args[i] == "--inventory")
    {
        inventory = true;
    }
    else if (args[i] == "--before" && i + 1 < args.Length)
    {
        before = Path.GetFullPath(args[++i]);
    }
    else if (args[i] == "--frames" && i + 1 < args.Length)
    {
        only = new HashSet<int>(args[++i].Split(',').Select(int.Parse));
    }
    else if (args[i] == "--sounds")
    {
        sounds = true;
    }
    else if (args[i] == "--shape" && i + 1 < args.Length)
    {
        shape = args[++i];
    }
    else if (args[i] == "--perf")
    {
        perf = true;
    }
    else if (args[i] == "--perf-verbose")
    {
        perf = true;
        Perf.Verbose = true;
    }
    else if (args[i] == "--perf-draw")
    {
        perf = true;
        Perf.Draw = true;
    }
    else if (args[i] == "--perf-store" && i + 1 < args.Length)
    {
        perf = true;
        Perf.StoreFolder = Path.GetFullPath(args[++i]);
    }
    else if (args[i] == "--perf-allocs")
    {
        perf = true;
        Perf.Allocations = true;
    }
    else if (args[i] == "--perf-serial")
    {
        // The pictures' rows on one thread (UiRaster.ParallelRows off): their whole work, steadier on a busy machine.
        perf = true;
        UiRaster.ParallelRows = false;
    }
    else if (args[i] == "--csv" && i + 1 < args.Length)
    {
        csv = Path.GetFullPath(args[++i]);
    }
}

if (sounds)
{
    SoundExport.Write(outDir);
    return 0;
}

Directory.CreateDirectory(outDir);
ContentSet content = PlaytestContent.Load();
var shapes = new (string Name, float Width, float Height, Insets Insets)[]
{
    ("16x9", 1080, 1920, new Insets(63, 0)),
    ("19.5x9", 1080, 2340, new Insets(110, 63)),
    ("21x9", 1080, 2520, new Insets(120, 66)),
};
if (shape != null)
{
    shapes = shapes.Where(s => s.Name == shape).ToArray();
}

if (perf)
{
    // The frame-time harness: one journey through the screens on one shape (19.5x9 unless --shape says another).
    return Perf.Run(content, shape != null ? shapes[0] : shapes[1], csv);
}

var usedSlots = new HashSet<string>(StringComparer.Ordinal);
var problems = new List<string>();
var sheetImages = new List<(int Number, string Title, SKImage Image)>();
foreach (Fixture frame in Fixtures.All(content, root).Concat(Fixtures.Extras(content)).Concat(Fixtures.Guides(content)).Concat(Fixtures.Profile(content)).Concat(Fixtures.Clearing(content)).Concat(Fixtures.Purchases(content)).Concat(Fixtures.Loading(content)))
{
    if (only != null && !only.Contains(frame.Number))
    {
        continue;
    }

    foreach ((string shapeName, float w, float h, Insets insets) in shapes)
    {
        using var painter = new SkiaPainter((int)w, (int)h, insets);
        string data = Path.Combine(Path.GetTempPath(), "bloomlings-preview", frame.Slug + "-" + shapeName);
        if (Directory.Exists(data))
        {
            Directory.Delete(data, recursive: true);
        }

        Directory.CreateDirectory(data);
        try
        {
            frame.Render(painter, data);
        }
        catch (Exception e)
        {
            problems.Add(frame.Slug + " " + shapeName + ": " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            continue;
        }

        string file = Path.Combine(outDir, frame.Number.ToString("00") + "-" + frame.Slug + "-" + shapeName + ".png");
        File.WriteAllBytes(file, painter.Png());
        usedSlots.UnionWith(painter.Slots);
        problems.AddRange(Checks.Run(painter, frame, shapeName));
        if (shapeName == "19.5x9")
        {
            sheetImages.Add((frame.Number, frame.Title, painter.Snapshot()));
        }
    }
}

if (sheetImages.Count > 0)
{
    byte[] sheet = Sheet.Render(sheetImages);
    File.WriteAllBytes(Path.Combine(outDir, "board-sheet.png"), sheet);
    if (before != null)
    {
        File.WriteAllBytes(Path.Combine(outDir, "before-after.jpg"), Sheet.BeforeAfter(File.ReadAllBytes(before), sheet));
    }
}

Console.WriteLine($"frames: {sheetImages.Count}, images in {outDir}");
(int heroDecoded, long heroBytes) = SkiaPainter.HeroFrameStats;
Console.WriteLine($"hero frames: {heroDecoded} decoded on first use, {heroBytes / (1024.0 * 1024.0):0.0} MB held");
problems.AddRange(HeroFrameCheck.Run());

// Animations draw the pictures their first frame made, and the APK's picture store gives back what it kept (PictureChecks).
problems.AddRange(PictureChecks.Run());
Console.WriteLine($"slots used by the playtest screens: {usedSlots.Count} of {AssetSlots.All.Count}");
if (only == null)
{
    problems.AddRange(Coverage.Check(usedSlots, root));
}

if (inventory)
{
    string path = Path.Combine(root, "specs", "002-ux-design-board", "asset-inventory.md");
    File.WriteAllText(path, Inventory.Markdown(usedSlots, root));
    Console.WriteLine("wrote " + path);
}

foreach (string problem in problems)
{
    Console.WriteLine("FAIL " + problem);
}

Console.WriteLine(problems.Count == 0 ? "checks: OK" : $"checks: {problems.Count} failed");
return problems.Count == 0 ? 0 : 1;

static string FindRoot()
{
    string? dir = AppContext.BaseDirectory;
    while (dir != null && !File.Exists(Path.Combine(dir, "CLAUDE.md")))
    {
        dir = Path.GetDirectoryName(dir);
    }

    return dir ?? Directory.GetCurrentDirectory();
}

namespace Bloomlings.Playtest.Preview
{
    /// <summary>A silent sound output for previews.</summary>
    public sealed class Silence : ISoundOut
    {
        public bool Enabled { get; set; } = true;

        public void Play(SoundCue cue)
        {
        }

        public void PlayClear(ClearSound sound, int index)
        {
        }

        public void Collect(Bloomlings.Client.UI.Design.ClearStyle style, int step)
        {
        }
    }
}
