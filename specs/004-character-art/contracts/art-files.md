# Contract: art files and the generator (FR-019 to FR-021)

## Tool

`tools/artgen` is a .NET 10 console project. It uses SkiaSharp 3.119 and references `core/src/Bloomlings.Core`. It is
not in `core/Bloomlings.sln`.

| Command | Does |
|---|---|
| `dotnet run --project tools/artgen -- build` | Renders every picture and writes the files and `manifest.json` into the client folder below. Overwrites the set, except the owner's pictures and files changed since the last build (spec 005). |
| `dotnet run --project tools/artgen -- check` | Renders in memory and compares each picture with its committed file: at most 2 per channel and at most 0.1% of pixels differ. Also checks the readability masks, the transparent margins and the manifest. Exits non-zero on any failure. |
| `dotnet run --project tools/artgen -- sheet` | Writes the review sheet (every character in every mood, the 3D heroes) to `tools/artgen/out/sheet.png`. The `out/` folder is gitignored. |
| `... -- build --only 2d` / `--only 3d` | Renders just one set (for iterating). |
| `... -- adopt <picture> [--record <file>]` | *(spec 005)* Records an owner picture of `3d/` in the manifest (`"source": "owner"`); `build` then keeps it and `check` verifies it as adopted (`tools/artgen/README.md`, data-model.md "ArtManifest"). |

The tool is deterministic: no randomness, no time, no GPU. It renders rows in parallel, and each row is written
independently.

## Files

Root: `client/Assets/Bloomlings/Art/Characters/Resources/Characters/`

| Path | Count | Size | Content |
|---|---|---|---|
| `2d/{icon}-{mood}.png` | 48 | 256 × 256 | `icon` ∈ leaf, moss, flower, bud, drop, dew, log, acorn, vine, berry, mist, bark; `mood` ∈ happy, asleep, worried, blank |
| `3d/{family}.png` | 4 | 512 × 576 | `family` ∈ sprig, bloom, drop, twig; the hero with its face |
| `3d/{family}-blank.png` | 4 | 512 × 576 | the same hero without eyes or mouth |
| `3d/group.png` | 1 | 1200 × 720 | the four heroes side by side with their contact shadows, no base (spec 005: the hosts' stone pedestal carries them) |
| `manifest.json` | 1 | | see data-model.md |

Format:
- PNG, 8-bit RGBA, straight alpha, sRGB.
- A transparent border of at least 2% on every side.
- 3D pictures include their soft contact shadow in the alpha.

## Loading

| Host | Name to load | How |
|---|---|---|
| Unity | `Characters/2d/leaf-happy` | `Resources.Load<Texture2D>`, then `Sprite.Create` (cached by `CharacterSprites`) |
| Full playtest (APK) | `characters/2d/leaf-happy.png` | embedded resource, `BitmapFactory.DecodeStream`, cached |
| Preview tool | `characters/2d/leaf-happy.png` | embedded resource, `SKBitmap.Decode`, cached |
| Level tester | — | does not use them |

Embedding:
- `playtest/android/Bloomlings.Playtest.Android.csproj` and `playtest/preview/Bloomlings.Playtest.Preview.csproj`
  embed `Characters/**/*.png` with `LogicalName="characters/%(RecursiveDir)%(Filename)%(Extension)"`. The path
  separator is normalized to `/`.
- The tester project embeds nothing new.

Unity import:
- An Editor postprocessor (`Editor/CharacterArtImporter.cs`) sets the texture settings for files under
  `Art/Characters`: alpha is transparency, mipmaps on, no non-power-of-two scaling, clamp wrap, high-quality
  compression.

## Fallback (FR-021)

A missing or undecodable file:
- falls back to the spec 002 figure (`char.<family>` silhouette in the variant color, the variant symbol in ink);
- logs one warning per name.

The preview tool counts a missing picture as a check failure.

## Originality (FR-019)

- `client/THIRD_PARTY_NOTICES.md` gets a row whose file column is the folder above (with a trailing `/`) and whose
  licence file is `tools/artgen/OWNERSHIP.md`.
- `OriginalityTests` accepts any file under a recorded folder that the folder's `manifest.json` lists.
- `Bloomlings.Content.Tests` gets `CharacterArt_ManifestMatchesFiles`: the manifest lists exactly the files present,
  the sha256 values match, and the expected 57 names are all there.
