# Contract: how the hosts draw the characters (FR-008 to FR-018)

## Shared kit (`client/Assets/Bloomlings/UI/Design/CharacterArt.cs`, engine-free)

| Member | Gives |
|---|---|
| `CharacterMood` | `Happy`, `Asleep`, `Worried`, `Blank` |
| `CharacterArt.Picture2D(string iconId, CharacterMood mood)` | `"2d/{icon}-{mood}"` |
| `CharacterArt.Hero(Family family, bool blank)` | `"3d/{family}"` or `"3d/{family}-blank"` |
| `CharacterArt.Group` | `"3d/group"` |
| `CharacterArt.Slot2D(iconId)`, `HeroSlot(family)`, `GroupSlot` | the asset slot ids |
| `CharacterArt.OnCard(Box face)` | the character box on a pod or slot face: 84% of the width, centered at 40% of the height |
| `CharacterArt.CountBox(Box face)` | where "xN" goes: the bottom-right corner, inset 6% |
| `CharacterArt.OnTile(Box face)` | the board character box: 86% of the face, centered, 3% low |
| `CharacterArt.HatBox(Box picture)`, `ExpressionBox(Box picture)`, `TrailBox(Box picture)` | the cosmetic overlay places |
| `DesignTokens.CharacterTile(Rgba variant)`, `CharacterTileMix` | the board tile tint (data-model.md) |

The playtest names are prefixed with `characters/` and suffixed with `.png`. Unity names are prefixed with
`Characters/` and have no extension.

## Playtest painter

`IPainter` changes:
- **Added: `void Sprite(string name, Box box)`.** Draws the picture fitted into `box` (aspect kept, centered). It uses
  the alpha stack and the transform stack. Pictures are decoded once and cached.
- **Added: `bool HasSprite(string name)`.** Whether the picture exists (embedded) and decodes.
- **Added: `void SpriteSkin(string name, Box box, string skinShape, Rgba tint)`.** Draws the cosmetic skin pattern
  only where the picture is opaque (a layer with source-atop blending).
- **Removed: `Picture(...)`.** It was the code-drawn kawaii figure of spec 003 FR-032.

`Visuals` changes:
- **`Character(IPainter p, Box box, VariantId variant, CharacterMood mood)`.** Draws the 2D picture. It falls back to
  the spec 002 figure when `HasSprite` is false (FR-021). It marks `char.v.<icon>` and `char.<family>`.
- **`Hero(IPainter p, Box box, Family family, Outfit? outfit)`.** Draws the 3D solo hero. With a worn expression it
  uses the blank picture with the expression overlay. It also draws the skin (`SpriteSkin`), the hat and the trail.
- **`Group(IPainter p, Box box)`.** Draws the 3D group picture.
- **`GroundShadow(p, box)`.** A flat shadow under a 2D character's feet.

## Where each host element draws what

| Element | Playtest | Unity | Picture | Mood |
|---|---|---|---|---|
| exposed / pressed pod | `PodPainter.Pod` | `PodView` | 2D at `OnCard`, "xN" at `CountBox` | Happy |
| queued pod | same | same | same, card `PodQueued` tint | Asleep |
| locked / mystery pod | unchanged (padlock, "?") | unchanged | none | — |
| flight to a slot | `PodPainter.DrawFlights` | slot animation | 2D | Happy |
| working slot | `SlotPainter` | `SlotRowView` | 2D at `OnCard`, "xN" | Happy |
| stuck slot | same | same | same, card greyed, hourglass | Worried |
| board tile | `BoardPainter.Tile` | `TileView` | `CharacterTile` block + 2D at `OnTile` | Happy |
| layer peek, key, mystery tile | unchanged | unchanged | — | — |
| walker | `BoardPainter.DrawWalkers` | `BloomlingWorker` | 2D + ground shadow + outfit overlays | Happy |
| win cheer below the board | — | `WorkerPool.Celebrate` | 2D | Happy |
| splash | `SplashScreen.Draw` | `SplashScreen` | 3D group | — |
| Home early | `HomeScreen.Hero` | `HomeScreen` | 3D group | — |
| Home progressed | `HomeScreen.Hero` | `HomeScreen` | 3D solo + outfit | — |
| win / milestone card | `EndCards` | `WinScreen`, `MilestoneCard` | 3D group on the card's top edge | — |
| Wardrobe | (not in the playtest) | `WardrobeScreen` | 3D solo per family + outfit | — |
| profile, leaderboard row | `MetaCards` | `ProfileAvatar`, leaderboard | 3D solo, small | — |

## Unity

- **`Art/Characters/CharacterSprites.cs`.** Methods: `Get(string name)` (a `Sprite` or null, cached),
  `Character(VariantId, CharacterMood)` and `Hero(Family, bool blank)`.
- **`BloomlingFigure.Show(...)`.** Gains a picture name instead of the procedural sprite. The skin image is masked by
  the figure (`Mask`, `showMaskGraphic`). The expression overlay shows only with the blank picture.
- **Removed:** `ProceduralSprites.Bloomling` and the kawaii look. `ProceduralSprites.Silhouette` stays as the
  fallback.

## Readability guarantees (tested)

- "xN" reaches at least 4.5:1 against every card face: exposed, queued and stuck tints.
- The tile tints of the launch variants differ pairwise by at least the minimum color distance of the readability
  tool.
- The character boxes stay inside the pod face and the tile face.
- Mystery pods and tiles never draw a character picture.
