# tools/artgen: the character art generator (spec 004)

Draws the 2D variant characters and renders the 3D family heroes, then writes them as PNG files with a manifest into
`client/Assets/Bloomlings/Art/Characters/Resources/Characters/`. The Unity client loads them from there
(`CharacterSprites`), and the full playtest and the preview embed them. It is a dev tool: it never ships, and it is not
in `core/Bloomlings.sln`.

## Commands

Run from the repository root (needs the .NET 10 SDK):

| Command | Does |
|---|---|
| `dotnet run --project tools/artgen -- build` | Renders every picture (57 files) and writes `manifest.json`. The 3D set takes about 10 minutes on 4 cores. It never overwrites an owner picture (below), nor a file changed since the last build (it prints `SKIP`; `--force` overwrites those). |
| `dotnet run --project tools/artgen -- adopt <picture> [--record <file>]` | Records one of the owner's pictures (below) in `manifest.json`. |
| `dotnet run --project tools/artgen -- build --only 2d` | Only the 48 2D pictures (about a second). `--only 3d` renders the 9 3D pictures; `--only 3d/group` (any picture name) renders that one picture. |
| `dotnet run --project tools/artgen -- check` | Compares every committed picture with a fresh render (at most 2 per channel, at most 0.1% of pixels; every fourth row of the 3D pictures), checks the 2% transparent margins, the shape difference of the launch characters at 48 px, the hero face places of the kit, and the manifest. Owner pictures are checked as adopted instead (below). Exits non-zero on a problem. About 3 minutes. |
| `dotnet run --project tools/artgen -- sheet` | Writes the review sheet to `tools/artgen/out/sheet.png` (gitignored): every character in every mood, the launch characters at play size with their shape differences, the heroes and the group. |
| `dotnet run --project tools/artgen -- faces` | Prints where each hero's face lands in its solo picture (`CharacterArt.FaceCenterHero`). |

## Files

| Path | Content |
|---|---|
| `2d/{icon}-{mood}.png` | 12 icons × happy, asleep, worried, blank; 256 × 256 |
| `3d/{family}.png`, `3d/{family}-blank.png` | the four heroes, with a face and without (for worn expressions); 512 × 576 |
| `3d/group.png` | the four heroes side by side with their soft contact shadows, no base of their own (the hosts stand them on their stone pedestal); 1200 × 720 |
| `manifest.json` | every file with its size, SHA-256 and asset slot; an owner picture also has `"source": "owner"` and its `"record"` |

Names, sizes and placements come from the shared kit, `client/Assets/Bloomlings/UI/Design/CharacterArt.cs`, which this
tool links.

## Changing the art

1. Edit the drawing (`Characters2D.cs`, `Palette.cs`) or the renderer (`Heroes3D.cs`, `Sdf.cs`).
2. `build` (or `build --only 2d` while iterating), then look at `sheet`.
3. Run the preview (`dotnet run --project playtest/preview`) to see the art in the screens.
4. `check` must pass, and the content tests check the manifest (`dotnet test core/Bloomlings.sln`).
5. Commit the pictures and the manifest with the code change.

The output depends only on this code and the .NET and SkiaSharp versions: no randomness, no clock, no GPU. Rows render
in parallel, each row independently. See `OWNERSHIP.md` for the ownership record.

## The owner's pictures

The owner may replace the generated 3D pictures and add the celebrating heroes (`specs/005-reference-look/pictures.md`
section A: `3d/{family}.png`, `3d/{family}-blank.png`, `3d/group.png`, `3d/{family}-cheer.png`). They live in the same
folder under the same names, so both builds load them with no code change. The tool keeps them apart from its own art:

1. Write the source record first: a Markdown file in the repository with, per picture, the tool, the author, the date
   and the licence (by default `tools/artgen/models/owner-pictures.md`). For instance:

   | File | Made with | By | Licence |
   |---|---|---|---|
   | `3d/sprig-cheer.png` | Blender 4.2, rendered from the owner's model | the owner | the project's own |

2. Drop the PNG into `client/Assets/Bloomlings/Art/Characters/Resources/Characters/3d/` with its exact name and size.
3. `dotnet run --project tools/artgen -- adopt 3d/sprig-cheer.png` (or `--record <file>` for another record). It checks
   the picture (in `3d/`, the size the hosts expect, the 2% transparent margin, the record exists) and lists it in
   `manifest.json` with `"source": "owner"`, its hash and its record.
4. `check` must pass; commit the picture, `manifest.json` and the record together.

From then on:
- `build` keeps the file (`... kept (the owner's picture)`);
- `check` verifies its hash, size, margin and record instead of comparing it with a fresh render, and prints a `NOTE`
  when a hero is the owner's but its `-blank` twin is not, or when `CharacterArt.FaceCenterHero` (where worn expressions
  go) must be set by hand to the owner's face;
- the content tests accept it (`CharacterArtTests`), and the originality test counts it only with its record
  (`OriginalityTests`).

A changed owner picture fails `check` until it is adopted again. To go back to the generated picture, delete the owner's
file and run `build` (with `--only` for one picture). The hosts stand each picture on its feet line, so keep the feet
there and the space below them clear: 62% down the group (`CharacterArt.GroupFeetShare`) and 90% down a solo or
celebrating hero (`HomeStage.FeetShare`).
