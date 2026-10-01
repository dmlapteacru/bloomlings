# Research: Character Art (spec 004)

Decisions for the plan. Each has the decision, why, and what else was considered.

## R1. One .NET generator tool, not the browser concept tooling

**Decision.** A new console project `tools/artgen` (.NET 10, SkiaSharp 3.119) draws all character art and writes PNG
files into the client. It references `Bloomlings.Core` for the variant catalog (colors, icon ids, families). It is
not part of `core/Bloomlings.sln`, so `core-tests` stays as it is.

**Why.**
- The repository already requires .NET 10.
- SkiaSharp 3.119 is already used by `playtest/preview`, so no new dependency family is added.
- One toolchain: no Node, Playwright or Chromium to regenerate art.
- The output is deterministic for a given tool version (no GPU, no WebGL driver).

**Alternatives.**
- **Keep the concept scripts** (Python SVG + headless Chromium screenshots, a WebGL raymarcher). They need Node,
  Playwright and a software GPU. Rejected: the output depends on the browser and driver version.
- **Draw characters at runtime in the kit** (as spec 003 FR-032 did). The owner rejected that look. Bezier shapes with
  gradients and outlines would also need a vector rasterizer in both hosts.
- **Blender.** A large external install. It is not scriptable in this environment without downloads.

## R2. 2D characters: vector drawing with SkiaSharp

**Decision.** Each variant's character is a short drawing routine on a 100 × 100 design square:
- `SKPath` shapes ported from the approved concept (`concept-gameplay-2d.jpg`);
- radial gradient fills;
- an outline in a darker shade of the body;
- highlights and the face.

Compound shapes (moss cushion, flower) use one shared outline: every part is stroked first, then filled.

Each drawing is rendered at 256 × 256 px with 4% padding, in four moods:
- **happy:** the base look;
- **asleep:** closed eyes; the colors mixed 45% toward `#A7A39A`;
- **worried:** a frown; the colors greyed like the stuck slot (`Grey().Mix(state.stuck, 0.35)`);
- **blank:** no eyes or mouth, blush kept.

The muted and grey looks are baked into the files, so hosts never tint characters.

**Why.**
- The concept's paths are already approved.
- SkiaSharp draws SVG-like paths, gradients and strokes directly.
- 256 px covers the largest pod on a 1440 px wide phone (about 200 px) with sharp downscaling.

**Alternatives.**
- **One master size, tinted by hosts.** Tinting changes the outline and the face too, so the muted look would differ
  between Android, Skia and Unity.
- **Several sizes per character.** That multiplies files. Downscaling from 256 px with filtering is enough.

## R3. 3D heroes: a CPU signed-distance raymarcher in C#

**Decision.** `tools/artgen` ports the approved WebGL concept (`concept-home-3d.jpg`) to a CPU raymarcher:
- **Shapes:** the heroes are signed-distance shapes with smooth unions (ellipsoids, round cones, capsules, thin
  curled leaves and petals, a rounded cylinder with bark grooves).
- **Lighting:** a warm key light with soft shadows, ambient occlusion, a golden back light that wraps the edges, and
  translucency for leaves, petals and water.
- **Rendering:** 2 × 2 supersampling, straight alpha, rows rendered in parallel (`Parallel.For`, each row written
  independently, so results do not depend on scheduling).

Pictures:

| File | Content | Size |
|---|---|---|
| `3d/{family}.png` | one hero, facing slightly toward the viewer, with its face | 512 × 576 |
| `3d/{family}-blank.png` | the same without eyes and mouth (for worn expressions) | 512 × 576 |
| `3d/group.png` | the four on the round stone pedestal, warm light | 1200 × 720 |

**Why.**
- The concept renders already match the owner's first reference closely enough to be approved.
- A CPU port keeps one toolchain (R1) and is deterministic.
- At about 3 M rays the group takes well under a minute on a laptop.

**Alternatives.**
- **WebGL in Chromium (the concept).** See R1.
- **Mesh modelling.** No 3D package is available, and organic shapes are simpler as signed distances.

## R4. File layout, names and manifest

**Decision.**
```text
client/Assets/Bloomlings/Art/Characters/Resources/Characters/
├── 2d/{icon}-{mood}.png       # 12 icons × happy | asleep | worried | blank
├── 3d/{family}.png            # sprig | bloom | drop | twig
├── 3d/{family}-blank.png
├── 3d/group.png
└── manifest.json              # every file: path, width, height, sha256, asset slot
```
- **Icons:** the `VariantInfo.IconId` values (`leaf`, `moss`, `flower`, `bud`, `drop`, `dew`, `log`, `acorn`,
  `vine`, `berry`, `mist`, `bark`).
- **Families:** the lower-case `Family` names.

**Why.** Unity loads files under a `Resources` folder by name at runtime (`Resources.Load<Texture2D>`). The playtest
embeds the same files as resources (`characters/...`). The manifest makes the set checkable without decoding images.

**Alternatives.**
- **Atlases.** They need packing metadata in three hosts. The file count is small (56).
- **`StreamingAssets`.** These are not loadable synchronously on Android.

## R5. Drawing pictures in the hosts

**Decision.**
- **Playtest.**
  - `IPainter` gains `Sprite(string name, Box box)`. It draws an embedded character picture fitted into the box. The
    painter decodes the file once (`BitmapFactory` on Android, `SKBitmap.Decode` in the preview), caches it and
    honors the alpha stack.
  - `IPainter` also gains `SpriteSkin(string name, Box box, string skinShape, Rgba tint)`. It draws a skin pattern
    only where the picture is opaque: a layer with source-atop blending.
  - `bool HasSprite(string name)` lets screens fall back.
- **Unity.** `CharacterSprites` (in `Art/Characters/`) loads a `Texture2D` from Resources, makes a `Sprite` once
  (`Sprite.Create`) and caches it. The skin image becomes a child of the figure image with a UI `Mask`, so the pattern
  stays inside the picture's alpha.

**Why.** Pictures need no new rendering technique in either host. Skins keep working on any picture without
pre-rendering 4 skins × 16 heroes.

**Alternatives.**
- **Bake skins into pictures.** That adds 112 files, too much for SC-007.
- **Keep the SDF silhouettes as skin masks.** They no longer match the pictures.

## R6. The board tile

**Decision.** A target tile stays the volumetric block of spec 003 FR-023, with these changes:
- **Face color:** `DesignTokens.CharacterTile(variant) = variant.Mix(garden.paper_top, 0.7)`.
- **Edge and lip:** from that tint.
- **Character:** the variant's happy character at 86% of the face, centered, slightly lower. It never crosses the
  face edge (FR-015).

These stay as they are:
- the layer-peek corner and the key corner;
- the mystery tile ("?");
- stones, specials and open cells.

**Why.**
- The tint keeps the picture readable (FR-013): neighboring roles stay distinct, because the characters carry the full
  colors and the tints carry the regions.
- Open cells show the finished picture's light colors without a frame or character, so they stay distinct.

**Alternatives.**
- **One plain cream for all tiles** (the reference). The picture would read only from the characters. That is weaker
  for large boards. The owner can switch to it with one value (`DesignTokens.CharacterTileMix = 1`).

## R7. The pod and slot count "xN"

**Decision.**
- **Text:** the count is the localized string `pod.count` = `x{0}`, drawn in the corner of the card face (bottom right
  for pods and slots). It uses the `type.count` style in `garden.label_plain` with a white outline (`TextLook`, 0.08 em).
- **Muted pods:** the count is drawn in `text.secondary`.
- **Character:** centered at 40% of the face height, at 84% of its width. The count sits over the character's lower
  right edge, as in the reference, and never covers the face.

**Why.** It matches the owner's reference. Dark brown on the light card reaches more than 4.5:1 (tested). The white
outline keeps it readable where it overlaps the character.

**Alternatives.** Keep the dark pill. It is too heavy next to big characters, and the reference has none.

## R8. Moods and where they are used

| Host element | Mood |
|---|---|
| exposed pod, pressed pod, pod in flight | happy |
| queued pod (FR-022a) | asleep |
| working slot | happy |
| stuck slot | worried |
| walker, board tile, win cheer below the board | happy |
| a figure wearing an expression cosmetic | blank + the expression overlay |

Locked, mystery and connected states keep their spec 002/003 looks on top.

## R9. Cosmetics

- **Hats and trails:** stay overlays, placed against the picture box (hat over the top 30%, trail behind the left
  edge).
- **Expressions:** the blank picture plus the cosmetic expression shape over the face area.
- **Skins:** the pattern clipped to the picture (R5).

On 3D heroes the same overlays apply. They are drawn flat over the 3D picture, which is acceptable for the playtest
and the placeholder cosmetics. Final cosmetic art may later be rendered per hero.

## R10. Fallback when a picture is missing

**Decision.** If `HasSprite` is false (playtest) or `Resources.Load` returns null (Unity), the figure is drawn in the
spec 002 look:
- the family silhouette shape (`char.<family>`) in the variant color;
- the variant symbol in ink on it.

A warning is logged once per name. The playtest preview treats a missing picture as a check failure (FR-021).

## R11. Removing the code-drawn kawaii figures (spec 003 FR-032)

**Decision.** Remove these:
- `BloomlingArt`, `BloomlingLook`, their tests and the 7 `char.*` color tokens. The token count goes back to 89.
- `IPainter.Picture`, `ProceduralSprites.Bloomling` and `Visuals.GroundShadow`'s dependency on `BloomlingArt`.

Restore the spec 002 family silhouettes for the `char.*` shapes. They are the fallback (R10).

`BloomlingMood` moves to the new kit file `CharacterArt.cs`, with `Happy`, `Asleep`, `Worried` and `Blank`.

**Why.** FR-001 replaces them. Keeping dead code invites drift.

## R12. Determinism and the art check

**Decision.** `dotnet run --project tools/artgen -- check` re-renders every picture in memory and compares it with
the committed file. A picture passes when:
- no channel differs by more than 2;
- at most 0.1% of pixels differ at all.

The tolerance covers `MathF` differences between CPUs. The check also verifies:
- **Readability (FR-022, SC-003):** at 48 px, the alpha masks of every pair of launch characters differ in at least
  10% of pixels. Same-family pairs are included.
- **Margins:** every picture has a transparent border of at least 2% (nothing is clipped).
- **Manifest:** `manifest.json` matches the files (sha256, sizes).

`build` writes the files and the manifest. `sheet` writes the review sheet (FR-023) to `tools/artgen/out/`, which is
gitignored.

Like the other checks, it runs locally before a push (CI is manual only). A core content test checks the manifest
against the files without decoding images, so `core-tests` also catches a stale set.

## R13. Size budget

| Set | Count | Approximate size |
|---|---|---|
| 2D | 48 files at 256² (flat-ish art compresses well) | about 1.2 MB |
| 3D | 8 solo at 512 × 576 (about 180 KB each) and the group (about 500 KB) | about 2 MB |

That is well under SC-007 (6 MB).

**Decoded memory in a level.** At most 8 variants × 3 moods × 256 KB is about 6 MB. Meta screens decode one or two 3D
pictures.

## R14. Where the 3D heroes go

| Screen | Picture | Placement |
|---|---|---|
| Splash (frame 1) | `3d/group.png` | in place of the four figures |
| Home, early (frame 2) | `3d/group.png` | in the hero area, in place of the two sitters |
| Home, progressed (frame 3) | `3d/{family}.png` (`-blank` with an expression) | the player's family (as today: Sprig in the playtest, Bloom in Unity) with its outfit |
| Win and milestone (frames 15, 16) | `3d/group.png` | standing on the top edge of the card, above the finished picture |
| Wardrobe | `3d/{family}.png` | the four family buttons and the avatar |
| Profile and leaderboard row | `3d/bloom.png` | small |

The finished picture stays on the win card (spec 001 FR-007).

The win cheer below the board (Unity `WorkerPool.Celebrate`) is inside the level screen, so it uses the 2D happy
characters (FR-018).

## R15. Originality record

**Decision.**
- **Notices.** `client/THIRD_PARTY_NOTICES.md` gets one row for the generated folder,
  `client/Assets/Bloomlings/Art/Characters/Resources/Characters/`. Its licence file is
  `tools/artgen/OWNERSHIP.md`, which states that the pictures are the project's own work, generated by the tool.
- **Test.** `OriginalityTests.Client_HasNoImportedArtAudioOrFonts` accepts a file under a recorded folder when the
  folder's `manifest.json` lists it.
- **Checklist.** The originality checklist of spec 001 records the review (no reference image copied, no Colony Flow
  material).

## R16. Asset slots

New slots, all `PlaceholderKind.Generated` (a new kind: "generated picture, `tools/artgen`"):

| Slot | Category | States | Frames |
|---|---|---|---|
| `char.v.<icon>` (12) | Character | happy, asleep, worried, blank | pods, slots, walkers, board |
| `char.hero3d.<family>` (4) | Character | face, blank | Home, Wardrobe, profile |
| `char.hero3d.group` | Character | — | splash, Home early, win, milestone |

`char.face` and `char.accent` are retired: faces and symbols are now part of the pictures. `char.<family>` stays (the
fallback shape). `char.hero.home` stays and points to the 3D solo hero. The inventory is regenerated.
