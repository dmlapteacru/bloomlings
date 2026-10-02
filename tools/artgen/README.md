# tools/artgen: the character art generator (spec 004)

Draws the 2D variant characters and renders the 3D family heroes, then writes them as PNG files with a manifest into
`client/Assets/Bloomlings/Art/Characters/Resources/Characters/`. The Unity client loads them from there
(`CharacterSprites`), and the full playtest and the preview embed them. It is a dev tool: it never ships, and it is not
in `core/Bloomlings.sln`.

## Commands

Run from the repository root (needs the .NET 10 SDK):

| Command | Does |
|---|---|
| `dotnet run --project tools/artgen -- build` | Renders every picture (57 files) and writes `manifest.json`. The 3D set takes about 10 minutes on 4 cores. |
| `dotnet run --project tools/artgen -- build --only 2d` | Only the 48 2D pictures (about a second). `--only 3d` renders the 9 3D pictures; `--only 3d/group` (any picture name) renders that one picture. |
| `dotnet run --project tools/artgen -- check` | Compares every committed picture with a fresh render (at most 2 per channel, at most 0.1% of pixels; every fourth row of the 3D pictures), checks the 2% transparent margins, the shape difference of the launch characters at 48 px, the hero face places of the kit, and the manifest. Exits non-zero on a problem. About 3 minutes. |
| `dotnet run --project tools/artgen -- sheet` | Writes the review sheet to `tools/artgen/out/sheet.png` (gitignored): every character in every mood, the launch characters at play size with their shape differences, the heroes and the group. |
| `dotnet run --project tools/artgen -- faces` | Prints where each hero's face lands in its solo picture (`CharacterArt.FaceCenterHero`). |
| `... -- build --only experiments` / `check --only experiments` | The Leafling experiment only (about 5 seconds): the owner's Meshy model `models/leafling.fbx`, painted and rendered to `client/Assets/Bloomlings/Art/Experiments/Resources/Characters/experiments/leafling.png`. A plain `build` or `check` includes it. |

## Files

| Path | Content |
|---|---|
| `2d/{icon}-{mood}.png` | 12 icons × happy, asleep, worried, blank; 256 × 256 |
| `3d/{family}.png`, `3d/{family}-blank.png` | the four heroes, with a face and without (for worn expressions); 512 × 576 |
| `3d/group.png` | the four heroes side by side with their soft contact shadows, no base of their own (the hosts stand them on their stone pedestal); 1200 × 720 |
| `manifest.json` | every file with its size, SHA-256 and asset slot |

The Leafling experiment (spec 004 research R17) is not part of the set or its manifest: its picture lives in
`Art/Experiments/` with its own source record, `models/leafling.md`.

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
