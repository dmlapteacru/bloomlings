// The character art generator of spec 004 (contracts/art-files.md).
// Usage: dotnet run --project tools/artgen -- build|check|sheet|faces [--only 2d|3d|experiments]
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bloomlings.ArtGen;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;

string command = args.Length > 0 ? args[0] : "check";
string? only = null;
for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--only" && i + 1 < args.Length)
    {
        only = args[++i];
    }
}

string root = FindRoot();
string folder = Path.Combine(root, "client", "Assets", "Bloomlings", "Art", "Characters", "Resources", "Characters");

// The Leafling experiment (research R17): the owner's Meshy model, kept apart from the project's own art.
string leaflingFile = Path.Combine(root, "client", "Assets", "Bloomlings", "Art", "Experiments", "Resources", "Characters", CharacterArt.Leafling + ".png");
bool leafling = only == null || only == "experiments";
IReadOnlyList<string> all = CharacterArt.AllPictures(VariantCatalog.Default.All);
List<string> names = all.Where(n => only == null || n.StartsWith(only + "/", StringComparison.Ordinal)).ToList();

switch (command)
{
    case "build":
    {
        var clock = Stopwatch.StartNew();
        foreach (string name in names)
        {
            string file = Path.Combine(folder, name + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllBytes(file, ArtSet.Render(name));
            Console.WriteLine($"{name}.png ({clock.Elapsed.TotalSeconds:0.0} s)");
        }

        if (names.Count > 0)
        {
            Manifest manifest = Manifest.Write(folder, CharacterArt.SlotOf);
            long bytes = manifest.Files.Sum(f => new FileInfo(Path.Combine(folder, f.Path)).Length);
            Console.WriteLine($"wrote {names.Count} pictures; the set has {manifest.Files.Count} files, {bytes / 1024} KB");
        }

        if (leafling)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(leaflingFile)!);
            using SkiaSharp.SKBitmap bitmap = Png.FromRgba(Leafling.Width, Leafling.Height, Leafling.Render(root));
            File.WriteAllBytes(leaflingFile, Png.Encode(bitmap));
            Console.WriteLine($"{CharacterArt.Leafling}.png ({clock.Elapsed.TotalSeconds:0.0} s)");
        }

        return 0;
    }

    case "check":
    {
        List<string> problems = names.Count > 0 ? ArtCheck.Run(folder, all, names) : new List<string>();
        if (leafling)
        {
            if (File.Exists(leaflingFile))
            {
                ArtCheck.Compare(CharacterArt.Leafling, leaflingFile, Leafling.Width, Leafling.Height, (Leafling.Width, Leafling.Height, Leafling.Render(root)), 1, problems);
            }
            else
            {
                problems.Add(CharacterArt.Leafling + ".png is missing");
            }
        }

        foreach (string problem in problems)
        {
            Console.WriteLine("FAIL " + problem);
        }

        Console.WriteLine(problems.Count == 0 ? $"art check: OK ({names.Count + (leafling ? 1 : 0)} pictures)" : $"art check: {problems.Count} problem(s)");
        return problems.Count == 0 ? 0 : 1;
    }

    case "sheet":
    {
        string output = Path.Combine(root, "tools", "artgen", "out", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllBytes(output, Sheet.Render(folder, all));
        Console.WriteLine("wrote " + output);
        return 0;
    }

    case "faces":
    {
        // Where the renderer draws each hero's face, for CharacterArt.FaceCenterHero.
        foreach (Family family in CharacterArt.Families)
        {
            (double x, double y) = Heroes3D.FaceCenter(family);
            Console.WriteLine($"{family}: ({x:0.00}, {y:0.00})");
        }

        return 0;
    }

    default:
        Console.WriteLine("usage: build | check | sheet | faces [--only 2d|3d|experiments]");
        return 2;
}

static string FindRoot()
{
    string? dir = AppContext.BaseDirectory;
    while (dir != null && !File.Exists(Path.Combine(dir, "core", "global.json")))
    {
        dir = Path.GetDirectoryName(dir);
    }

    return dir ?? Directory.GetCurrentDirectory();
}
