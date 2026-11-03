// Renders the full playtest's designed screens for each design board frame, checks them, and writes the asset inventory.
// See the project file. Usage: dotnet run --project playtest/preview [-- --out <dir>] [--inventory] [--frames 7,8,9]
// [--shape 19.5x9] (one screen shape only, for a quick look; the checks then cover that shape alone)
// [--before <sheet.png>] (also writes before-after.jpg: that sheet above the new one, spec 003 FR-029)
// [--sounds] (only writes the synthesized clips as WAV files and a listening schedule to <out>/sounds, spec 005 FR-042)
// [--perf] [--csv <file>] (only runs the frame-time harness, Perf.cs: a journey through the screens at 60 fps with the
// APK's picture cache, per screen ms a frame, C# picture rasters and cache misses; --csv also writes every frame;
// --perf-serial renders the pictures' rows on one thread, --perf-draw really draws the frames, --perf-verbose prints each,
// --perf-store <dir> keeps the made pictures there as the APK does between launches: run twice to see a later launch,
// --perf-allocs lists what the level's frames allocate by type, --perf-warm warms pictures ahead as the APK does: an idle
// Home makes the next likely screens' pictures on a worker, IPictureWarmer)
// [--rounds [<published dir>]] (only the round levels as the APKs run them, spec 006: Home, the round level placeholder,
// the dev row's jump and "new levels soon", from a published round set; RoundsPreview.cs)
// [--board-cubes <published dir> <level> <out dir>] (only the board cube look-dev sheet of 2026-11-03: the level in each
// look of BoardCubeLook.Variants; BoardCubeSheet.cs)
// [--board-after <published dir> <out dir> <level>...] (only those levels in the game's cube look, after-*.png)
// [--board-compare <published dir> <level> <out dir> <ref close-up> <its cell px> <ref screen> <x,y,w,h>] (E, E2, reference)
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
string? mosaicPilotRoot = null;
string? artLibraryRegistry = null;
string artLibraryLevels = "1-2500";
bool mosaicGameStates = false;
string outDir = Path.Combine(root, "playtest", "preview", "out");
bool inventory = false;
string? before = null;
HashSet<int>? only = null;
bool sounds = false;
bool promoMotion = false;
bool mosaicPilot = false;
bool auditLaunch = false;
string? shape = null;
bool perf = false;
bool roundsPreview = false;
(string Rounds, string Out)? latchStills = null;
(string Rounds, string Out, int Level, string Tag)? linkStills = null;
(int Level, float Seconds, string Out, string? Style)? clearVideo = null;
(string Rounds, int Level, string Out)? boardCubes = null;
(string Rounds, string Out, List<int> Levels)? boardAfter = null;
string[]? boardCompare = null;
string? roundsFolder = null;
string? csv = null;
float purchaseSize = ProfileGameDesign.PurchaseScale;
string? purchaseComparison = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--audit-launch")
    {
        auditLaunch = true;
    }
    else if (args[i] == "--clear-video" && i + 3 < args.Length)
    {
        // --clear-video <level> <seconds> <out dir> [style id]: review frames of a round level's walkers (ClearVideo)
        string? styleId = i + 4 < args.Length && !args[i + 4].StartsWith("--", StringComparison.Ordinal) ? args[i + 4] : null;
        clearVideo = (int.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(args[i + 2], System.Globalization.CultureInfo.InvariantCulture), args[i + 3], styleId);
        i += styleId != null ? 4 : 3;
    }
    else if (args[i] == "--board-compare" && i + 7 < args.Length)
    {
        // --board-compare <published dir> <level> <out dir> <reference close-up> <its cell px> <reference screen> <x,y,w,h>
        boardCompare = args[(i + 1)..(i + 8)];
        i += 7;
    }
    else if (args[i] == "--board-after" && i + 3 < args.Length)
    {
        // --board-after <published dir> <out dir> <level>...: levels in the game's cube look (BoardCubeSheet.After)
        var afterLevels = new List<int>();
        int first = i + 3;
        for (i = first; i < args.Length && !args[i].StartsWith("--", StringComparison.Ordinal); i++)
        {
            afterLevels.Add(int.Parse(args[i], System.Globalization.CultureInfo.InvariantCulture));
        }

        boardAfter = (Path.GetFullPath(args[first - 2]), Path.GetFullPath(args[first - 1]), afterLevels);
        i--;
    }
    else if (args[i] == "--board-cubes" && i + 3 < args.Length)
    {
        // --board-cubes <published dir> <level> <out dir>: the board cube looks side by side (BoardCubeSheet)
        boardCubes = (Path.GetFullPath(args[i + 1]), int.Parse(args[i + 2], System.Globalization.CultureInfo.InvariantCulture), Path.GetFullPath(args[i + 3]));
        i += 3;
    }
    else if (args[i] == "--latch-stills" && i + 2 < args.Length)
    {
        // --latch-stills <rounds dir> <out dir>: the latch's look for the owner's review (LatchStills)
        latchStills = (Path.GetFullPath(args[i + 1]), Path.GetFullPath(args[i + 2]));
        i += 2;
    }
    else if (args[i] == "--link-stills" && i + 2 < args.Length)
    {
        // --link-stills <rounds dir> <out dir> [level] [tag]: the hidden link partner's marks and the walkers' size (LinkStills)
        int linkLevel = 54;
        string linkTag = "now";
        int used = 2;
        if (i + 3 < args.Length && int.TryParse(args[i + 3], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int parsed))
        {
            linkLevel = parsed;
            used = 3;
            if (i + 4 < args.Length && !args[i + 4].StartsWith("--", StringComparison.Ordinal))
            {
                linkTag = args[i + 4];
                used = 4;
            }
        }

        linkStills = (Path.GetFullPath(args[i + 1]), Path.GetFullPath(args[i + 2]), linkLevel, linkTag);
        i += used;
    }
    else if (args[i] == "--rounds")
    {
        roundsPreview = true;
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            roundsFolder = Path.GetFullPath(args[++i]);
        }
    }
    else if (args[i] == "--out" && i + 1 < args.Length)
    {
        outDir = Path.GetFullPath(args[++i]);
    }
    else if (args[i] == "--packed-assets" && i + 1 < args.Length)
    {
        PlaytestFiles.Source = new PackedAssetFiles(args[++i]);
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
    else if (args[i] == "--promo-motion")
    {
        promoMotion = true;
    }
    else if (args[i] == "--art-library" && i + 1 < args.Length)
    {
        artLibraryRegistry = Path.GetFullPath(args[++i]);
    }
    else if (args[i] == "--art-levels" && i + 1 < args.Length)
    {
        artLibraryLevels = args[++i];
    }
    else if (args[i] == "--mosaic-pilot")
    {
        mosaicPilot = true;
    }
    else if (args[i] == "--mosaic-pilot-root" && i + 1 < args.Length)
    {
        mosaicPilotRoot = Path.GetFullPath(args[++i]);
    }
    else if (args[i] == "--mosaic-game-states")
    {
        mosaicGameStates = true;
    }
    else if (args[i] == "--sounds")
    {
        sounds = true;
    }
    else if (args[i] == "--shape" && i + 1 < args.Length)
    {
        shape = args[++i];
    }
    else if (args[i] == "--compare-purchases" && i + 1 < args.Length)
    {
        purchaseComparison = Path.GetFullPath(args[++i]);
    }
    else if (args[i] == "--purchase-size" && i + 1 < args.Length)
    {
        purchaseSize = float.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
        if (purchaseSize < .5f || purchaseSize > 1f) throw new ArgumentOutOfRangeException("purchase-size");
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
    else if (args[i] == "--perf-warm")
    {
        // As the APK: an idle Home warms the likely next screens' pictures on a worker (IPictureWarmer).
        perf = true;
        SkiaPainter.Warms = true;
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

if (artLibraryRegistry != null)
{
    int[] range = artLibraryLevels.Split('-').Select(int.Parse).ToArray();
    ArtLibraryPreview.Write(root, artLibraryRegistry, outDir, range[0], range.Length > 1 ? range[1] : range[0]);
    return 0;
}

if (mosaicPilot)
{
    MosaicPilotPreview.Write(root, outDir, mosaicPilotRoot, mosaicGameStates);
    return 0;
}

if (purchaseComparison != null)
{
    PurchaseComparison.Write(purchaseComparison);
    return 0;
}

if (promoMotion)
{
    PromoMotion.Write(outDir);
    return 0;
}

if (sounds)
{
    SoundExport.Write(outDir);
    return 0;
}

if (roundsPreview)
{
    return RoundsPreview.Write(root, outDir, roundsFolder);
}

if (linkStills != null)
{
    return LinkStills.Write(linkStills.Value.Rounds, linkStills.Value.Out, linkStills.Value.Level, linkStills.Value.Tag);
}

if (boardCompare != null)
{
    float[] rect = boardCompare[6].Split(',').Select(v => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
    return BoardCubeSheet.Compare(root, Path.GetFullPath(boardCompare[0]), int.Parse(boardCompare[1], System.Globalization.CultureInfo.InvariantCulture), Path.GetFullPath(boardCompare[2]),
        Path.GetFullPath(boardCompare[3]), float.Parse(boardCompare[4], System.Globalization.CultureInfo.InvariantCulture), Path.GetFullPath(boardCompare[5]), new Box(rect[0], rect[1], rect[0] + rect[2], rect[1] + rect[3]));
}

if (boardAfter != null)
{
    return BoardCubeSheet.After(boardAfter.Value.Rounds, boardAfter.Value.Out, boardAfter.Value.Levels);
}

if (boardCubes != null)
{
    return BoardCubeSheet.Write(root, boardCubes.Value.Rounds, boardCubes.Value.Level, boardCubes.Value.Out);
}

if (latchStills != null)
{
    return LatchStills.Write(latchStills.Value.Rounds, latchStills.Value.Out);
}

if (clearVideo != null)
{
    return ClearVideo.Write(root, clearVideo.Value.Level, clearVideo.Value.Seconds, Path.GetFullPath(clearVideo.Value.Out), clearVideo.Value.Style);
}

Directory.CreateDirectory(outDir);
ContentSet content = PlaytestContent.Load();
if (auditLaunch) return LaunchAudit.Run(content, root, outDir);
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
foreach (Fixture frame in Fixtures.All(content, root).Concat(Fixtures.Extras(content)).Concat(Fixtures.Guides(content)).Concat(Fixtures.Profile(content).Concat(Fixtures.ProfileGame(content))).Concat(Fixtures.NavigationControls(content)).Concat(Fixtures.Clearing(content)).Concat(Fixtures.Purchases(content)).Concat(Fixtures.PurchaseStates(content)).Concat(Fixtures.Loading(content)).Concat(Fixtures.Accounts(content)).Concat(Fixtures.Competition(content)).Concat(Fixtures.ServerEconomyStates(content,root)).Concat(Fixtures.Album(root)))
{
    if (only != null && !only.Contains(frame.Number))
    {
        continue;
    }

    foreach ((string shapeName, float w, float h, Insets insets) in shapes)
    {
        using var painter = new SkiaPainter((int)w, (int)h, insets) { PurchaseSize = purchaseSize };
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
problems.AddRange(LoadingChecks.Run(content));
problems.AddRange(NavigationLoadingChecks.Run(content));
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
