# Data Model: Character Art (spec 004)

Presentation data only. No save, rule, content or economy data changes.

## CharacterMood

| Value | Look | Used by (research R8) |
|---|---|---|
| `Happy` | open eyes with highlights, a smile | exposed and pressed pods, flights, working slots, walkers, board tiles, the win cheer |
| `Asleep` | closed eyes, colors mixed 45% toward `#A7A39A` | pods queued in their stack |
| `Worried` | open eyes, a small frown, colors greyed like a stuck slot | a stuck pod in a Waiting Slot |
| `Blank` | no eyes and no mouth, blush kept | a figure wearing an expression cosmetic |

`BloomlingMood` is replaced by this enum, which lives in `UI/Design/CharacterArt.cs`.

## VariantCharacter (2D)

One per `VariantInfo` (12).

| Field | Meaning |
|---|---|
| `IconId` | the variant's icon id; it names the files |
| `Shape` | the drawing routine in `tools/artgen` (contracts/character-look.md) |
| `Base` | the variant color; for dark variants a lighter drawing color with the same hue |
| `Line` | `Base` darkened 0.38: the outline, never black |
| `Pictures` | `2d/{IconId}-{mood}.png`, 256 × 256, one per `CharacterMood` |

Rules:
- Every two launch characters differ in shape, same-family pairs included. At 48 px, at least 15% of their joined
  silhouette (alpha ≥ 128) is covered by only one of them.
- A character never carries a separate badge or symbol (FR-006).
- Every picture keeps a transparent border of at least 2%.

## Hero3D

One per family (4), plus the group picture.

| Field | Meaning |
|---|---|
| `Family` | Sprig, Bloom, Drop or Twig |
| `Pictures` | `3d/{family}.png` and `3d/{family}-blank.png`, 512 × 576 |
| `Group` | `3d/group.png`, 1200 × 720: the four families side by side, feet at 62% of the height (`GroupFeetShare`), no base of their own (spec 005) |

Rules:
- The heroes appear on meta screens only (FR-017, FR-018; constitution VII v1.0.2).
- The blank picture has the same pose and light, without eyes or mouth.

## ArtManifest

`Characters/manifest.json` is written by `tools/artgen build`:

```json
{
  "tool": "tools/artgen",
  "version": 1,
  "files": [
    { "path": "2d/leaf-happy.png", "width": 256, "height": 256, "sha256": "…", "slot": "char.v.leaf" }
  ]
}
```

Rules:
- Every file in the folder is listed, and every listed file exists with the same sha256.
- The set is complete: 12 icons × 4 moods + 4 families × 2 + the group = 57 files.
- `OriginalityTests` accepts exactly the listed files (research R15).
- *(Added by spec 005, pictures.md A.)* An owner picture in `3d/` (a hero, the group or a celebrating hero
  `3d/{family}-cheer.png`) is recorded by `tools/artgen -- adopt` with `"source": "owner"` and `"record"` (the repository
  path of its source record: tool, author, licence). `build` keeps it, `check` verifies its hash, size, margin and
  record instead of a fresh render, and `OriginalityTests` accepts it only while its record exists. The 57 names stay
  complete, each generated or the owner's.

## CharacterTile (board)

Derived values, no stored data:

| Value | Rule |
|---|---|
| `DesignTokens.CharacterTileMix` | 0.7 (1 gives plain cream, the reference look) |
| `DesignTokens.CharacterTile(variant)` | `variant.Mix(garden.paper_top, CharacterTileMix)` |
| tile edge and top | `TileEdge` and `TileTop` of that tint (spec 003 FR-023) |
| character box | 86% of the tile face, centered horizontally, 3% below center |

Rules:
- The character box stays inside the face (FR-015).
- Two launch variants' tile tints differ by an RGB distance of at least 20 (the closest pair, Moss and Violet Bud, is
  27), so the board still separates them around the characters (FR-013, tested).

## PodCount

| Value | Rule |
|---|---|
| text | `pod.count` = `x{0}` (localized) |
| style | `type.count`, `garden.label_plain` fill, white outline of 0.08 em, on every card (`text.secondary` would fall under 4.5:1 on muted cards: 2.7:1 on a stuck Wood slot) |
| place | the bottom-right corner of the card face, inset 6% |
| contrast | at least 4.5:1 against the card face (tested), at least 3:1 required (FR-008) |

## Asset slots (changes)

| Slot | Change |
|---|---|
| `char.v.<icon>` × 12 | new; `PlaceholderKind.Generated`; states: happy, asleep, worried, blank |
| `char.hero3d.<family>` × 4 | new; Generated; states: face, blank |
| `char.hero3d.group` | new; Generated |
| `char.face`, `char.accent` | retired (part of the pictures now) |
| `char.<family>` | kept: the fallback silhouette and Unity's mask-free fallback |
| `char.hero.home` | kept; its placeholder becomes the 3D solo hero |
