# 03 — Tile & Target Variant System

**Status:** LOCKED (2026-09-29)
**Owner amendment · 2026-10-26:** tiles and board objects of new rounds follow [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md) §2 and §8.
**Revision 2026-10-06 (product owner):** on a big level's icons board a layered tile hides its next layer (§7; spec
`001` FR-036 as amended).

## 1. Core rule

Board is made of many full cells.

Every active target cell is completely and unambiguously occupied.

## 2. Four families, multiple target variants

The four Bloomling families are character/brand families.

Gameplay matching uses target variants.

### MVP variants

| Family | Target variant | Color | Icon |
|---|---|---|---|
| Sprig | Leaf | green | leaf |
| Sprig | Moss | teal | moss/soft leaf |
| Bloom | Flower | pink | flower |
| Bloom | Violet Bud | purple | bud |
| Drop | Water | blue | drop/wave |
| Drop | Dew | cyan | dew droplet |
| Twig | Wood | brown | stump/log |
| Twig | Acorn | orange | acorn |

## 3. Future variants

System supports expansion, e.g.:
- Vine / lime
- Berry / red
- Mist / indigo
- Bark / gold

Adding a target variant should ideally reuse a family animation rig with:
- palette change;
- small accessory/effect;
- unique target icon.

## 4. Exact-match gameplay

Every variant is mechanically distinct.

Leaf != Moss.  
Flower != Violet Bud.  
Water != Dew.  
Wood != Acorn.

Family is not a wildcard.

## 5. Bloomlings are workers, not tile graphics

Tiles show simple target symbols.

Characters run from Garden Entry to targets.

Do not place character faces inside every target cell.

## 6. Tile lifecycle

`visible/unreachable`
→ `reachable`
→ `processing`
→ `restored/open`

## 7. Layered Tile

A cell may stack variants.

Examples:
- `Leaf → Moss`
- `Flower → Water`
- `Acorn → Violet Bud → Dew`

Guideline:
- early max depth 2;
- later normal max depth 3;
- deeper exceptional only.

Layering creates reveal/dependency, not HP.

Since 2026-10-06 boards of up to 288 cells show the next layer as a small peek, while a big level's board (over 288
cells, up to 22×28) shows icons only: its layered tiles keep the next layer a surprise until the top layer clears. The
look is stored in each level, never chosen per device.

## 8. Stone

Permanent blocker:
- not clearable by normal pods;
- not walkable.

## 9. Mystery Tile

Optional:
- displays `?`;
- reveals exact target variant when it becomes reachable.

## 10. Key

Overlay on target tile.

Key must not hide:
- target icon;
- target color.

When support layer clears:
- key collected;
- lock resolves.

## 11. Gate

Blocks route until condition.

## 12. Chest

Temporary blocker that may reveal targets/route.

## 13. Heavy garden blocker

Examples:
- root knot;
- hedge wall;
- stone garden door.

Uses clear visible condition/counter.

## 14. Environmental object

Examples:
- Fountain;
- Statue;
- Bridge;
- Magical Seal.

Must create board-state change.

## 15. Restoration underlay

Cleared cell reveals finished garden image/pattern beneath.

## 16. Accessibility

Variant identity must use at least:
- hue;
- icon.

Ideally also subtle shape/surface distinction.

Do not rely on color alone, especially once 6 variants appear in one level.

## 17. Readability rule for same-family variants

Leaf Sprig vs Moss Sprig must be as easy to distinguish as two unrelated colors.

Shared family design must never reduce puzzle readability.

## Owner amendment · 2026-10-26 — level generation rules

- N counts coloured coordinates; hidden cells, cells under shutters or a bud and stack positions count once; holes and
  the areas of keys, wells, bridges and canal exits do not.
- Board objects of the approved set: acorn key (in an area without work), wooden shutters (exact-material counter),
  closed bud (any-material counter), well (deliveries of one material), hidden cells (reveal on an open reachable
  neighbour), stacks of one material (heights 2–4; grid not over 24×26). Object areas do not overlap.
- Layered tiles, Stone, Gate, Chest and environmental objects of this document are not in the approved set of 12 types
  for new rounds (`04_MECHANIC_CATALOG.md`).

## Owner amendment · 2026-10-27 — round materials and cubes (spec 006)

- **Materials.** Round levels use the 16 materials of the round palette, 4 per hero (Лист … Кора), each its own exact
  target.
- **Cubes.** Board cubes keep the game's soft cube drawing in the material colour and carry no variant icon.
- **Readability.** Under normal vision every pair passes CIEDE2000 ≥ 10. Colour-blind players get a symbols mode,
  off by default, that shows each material's symbol (constitution 1.1.0, principle II).
- **Sprig bridge.** It crosses a stream: two sites, each buildable once its approach is open. The choice is permanent,
  and building takes no pod or place.
