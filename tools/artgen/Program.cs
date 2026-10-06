// The character art generator of spec 004 (contracts/art-files.md).
// Usage: dotnet run --project tools/artgen -- build|check|sheet|faces [--only 2d|3d|<picture>]
//        dotnet run --project tools/artgen -- adopt <picture> [--record <file>]   (an owner picture, spec 005 pictures.md A)
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
string? record = null;
bool force = false;
var operands = new List<string>();
for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--only" && i + 1 < args.Length)
    {
        only = args[++i];
    }
    else if (args[i] == "--record" && i + 1 < args.Length)
    {
        record = args[++i];
    }
    else if (args[i] == "--force")
    {
        force = true;
    }
    else
    {
        operands.Add(args[i]);
    }
}

string root = FindRoot();
string folder = Path.Combine(root, "client", "Assets", "Bloomlings", "Art", "Characters", "Resources", "Characters");
IReadOnlyList<string> all = CharacterArt.AllPictures(VariantCatalog.Default.All);
// --only takes a group (2d, 3d) or one picture name (3d/group).
List<string> names = all.Where(n => only == null || n == only || n.StartsWith(only + "/", StringComparison.Ordinal)).ToList();

switch (command)
{
    case "build":
    {
        // Never overwrite the owner's pictures: the adopted ones, and a file changed since the last build (an owner picture
        // not adopted yet). --force overwrites the latter.
        var clock = Stopwatch.StartNew();
        Manifest previous = Manifest.Read(folder);
        var written = new HashSet<string>(StringComparer.Ordinal);
        int kept = 0;
        int foreign = 0;
        foreach (string name in names)
        {
            string file = Path.Combine(folder, name + ".png");
            ManifestFile? entry = previous.Find(name + ".png");
            if (File.Exists(file) && entry?.IsOwner == true)
            {
                Console.WriteLine($"{name}.png kept (the owner's picture)");
                kept++;
                continue;
            }

            if (File.Exists(file) && !force && (entry == null ? previous.Files.Count > 0 : entry.Sha256 != Manifest.Hash(file)))
            {
                Console.WriteLine($"SKIP {name}.png: it changed since the last build. An owner picture: run adopt {name}; otherwise delete it or build --force.");
                foreign++;
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllBytes(file, ArtSet.Render(name));
            written.Add(name);
            Console.WriteLine($"{name}.png ({clock.Elapsed.TotalSeconds:0.0} s)");
        }

        if (names.Count > 0)
        {
            Manifest manifest = Manifest.Write(folder, all, written, CharacterArt.SlotOf);
            long bytes = manifest.Files.Sum(f => new FileInfo(Path.Combine(folder, f.Path)).Length);
            Console.WriteLine($"wrote {written.Count} pictures{(kept > 0 ? $", kept {kept} of the owner's" : string.Empty)}; the set has {manifest.Files.Count} files, {bytes / 1024} KB");
        }

        return foreign == 0 ? 0 : 1;
    }

    case "check":
    {
        var notes = new List<string>();
        List<string> problems = names.Count > 0 ? ArtCheck.Run(root, folder, all, names, notes) : new List<string>();

        foreach (string note in notes)
        {
            Console.WriteLine("NOTE " + note);
        }

        foreach (string problem in problems)
        {
            Console.WriteLine("FAIL " + problem);
        }

        int owners = Manifest.Read(folder).Files.Count(f => f.IsOwner);
        string theirs = owners > 0 ? $"; {owners} owner picture(s) checked as adopted" : string.Empty;
        Console.WriteLine(problems.Count == 0 ? $"art check: OK ({names.Count} pictures{theirs})" : $"art check: {problems.Count} problem(s)");
        return problems.Count == 0 ? 0 : 1;
    }

    case "adopt":
    {
        // Records the owner's picture (spec 005 pictures.md A) so that build keeps it and check accepts it.
        if (operands.Count != 1)
        {
            Console.WriteLine("usage: adopt <picture> [--record <file>]   e.g. adopt 3d/sprig-cheer.png --record " + Adopt.DefaultRecord);
            return 2;
        }

        string recordPath = record ?? Adopt.DefaultRecord;
        string recordFull = Path.IsPathRooted(recordPath) ? recordPath : File.Exists(Path.Combine(root, recordPath)) ? Path.Combine(root, recordPath) : Path.GetFullPath(recordPath);
        var problems = new List<string>();
        ManifestFile? adopted = Adopt.Run(root, folder, all, operands[0], Path.GetRelativePath(root, recordFull), problems);
        foreach (string problem in problems)
        {
            Console.WriteLine("FAIL " + problem);
        }

        if (adopted == null)
        {
            Console.WriteLine("not adopted; see tools/artgen/README.md (\"The owner's pictures\")");
            return 1;
        }

        Console.WriteLine($"adopted {adopted.Path} ({adopted.Width} × {adopted.Height}, slot {adopted.Slot}, record {adopted.Record}); commit it with {Manifest.FileName} and the record");
        return 0;
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
        Console.WriteLine("usage: build [--force] | check | sheet | faces [--only 2d|3d|<picture>]; adopt <picture> [--record <file>]");
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
