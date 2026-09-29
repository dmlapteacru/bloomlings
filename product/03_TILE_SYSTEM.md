# 03 — Tile & Target Variant System

**Status:** DRAFT FOR LOCK

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
