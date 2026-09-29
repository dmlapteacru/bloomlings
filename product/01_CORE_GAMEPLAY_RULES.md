# 01 — Core Gameplay Rules

**Status:** DRAFT FOR LOCK

## 1. Objective

Clear every required gameplay tile-layer and resolve every mandatory blocker/special object.

The board itself communicates remaining work.

## 2. Core entities

### Board
Dense grid/mosaic containing many full gameplay cells.

### Bloomling Family
Brand/character family used to visually perform work.

Baseline families:
- Sprig
- Bloom
- Drop
- Twig

### Target Variant
The actual gameplay matching type.

A target variant has:
- exact family;
- exact color;
- exact icon;
- exact tile art;
- exact Source Pod identity.

Example:
- `Sprig / Leaf / green`
- `Sprig / Moss / teal`

These are different gameplay types even though both use Sprig-family animation.

### Spirit Pod / Source Group
Contains:
- one exact target variant;
- one remaining count.

Example:

`Moss Sprig ×18`

### Waiting Buffer
Exactly 5 slots by default.

### Source Tray
Stacked source of numbered pods.

### Garden Entry
Where Bloomlings emerge and enter the board.

## 3. Families and variants

The game should not be constrained to only four matching types.

### MVP global pool
Target **8 variants**:

| Family | Variant | Color language |
|---|---|---|
| Sprig | Leaf | green |
| Sprig | Moss | teal |
| Bloom | Flower | pink |
| Bloom | Violet Bud | purple |
| Drop | Water | blue |
| Drop | Dew | cyan |
| Twig | Wood | brown |
| Twig | Acorn | orange |

### Long-term pool
System should support at least 12+ variants.

Possible future additions:
- Sprig / Vine / lime
- Bloom / Berry / red
- Drop / Mist / indigo
- Twig / Bark / gold

## 4. Active variants per level

Typical:
- tutorial: 2–3 variants;
- early: 3–4;
- standard: 4–5;
- advanced: 5–6;
- exceptional: 6–7 only if readability remains good.

A level does not need all global variants.

## 5. Exact matching rule

A Bloomling Pod works only on its exact target variant.

Example:
- Leaf Sprig cannot clear Moss.
- Moss Sprig cannot clear Leaf.
- Water Drop cannot clear Dew.

Family similarity is visual/production reuse only.

This preserves Colony Flow-like multi-color puzzle depth.

## 6. Player action

The player taps an exposed Spirit Pod.

If legal:
1. pod moves to first free Waiting Slot;
2. pod becomes active;
3. Bloomlings of its family/color variant emerge;
4. they automatically clear reachable cells of that exact target variant.

The player never taps target cells.

## 7. One work unit = one tile-layer

`Violet Bloom ×24`

means exactly 24 units of Violet-Bud work.

Each successful unit:
- clears one matching visible tile-layer;
- decrements pod count by 1.

## 8. Reachability

Bloomlings move through restored/open walkable cells from Garden Entry.

A target is reachable when an orthogonally connected open route reaches a side adjacent to that target.

Diagonal-only contact does not count.

## 9. Partial completion

Example:

`Moss Sprig ×20`

Only 12 Moss cells reachable.

Result:
- 12 clear;
- pod becomes `Moss Sprig ×8`;
- pod stays in Waiting Slot;
- remaining 8 resume automatically when Moss becomes reachable later.

## 10. Multiple active pods

Several variants may work simultaneously.

If two active pods match the same exact target variant:
- oldest Waiting Slot gets logical priority.

Different variants from the same family are independent.

Example:
- Leaf Sprig and Moss Sprig can both be active together.

## 11. Pod completion

Pod leaves only when:

`remaining count = 0`

Its Waiting Slot frees immediately in simulation state.

## 12. Exact work accounting

Accounting is done **per target variant**, not per family.

For each target variant:

`sum of Source Pod counts = total required visible + hidden tile-layers of that exact variant`

Example:
- Leaf total = 85
- Moss total = 42

These are reconciled separately.

## 13. Layered cells

A physical cell may contain sequential target variants.

Example:

`Leaf → Violet Bud → Dew → Restored`

Clearing top layer:
- consumes 1 exact matching work unit;
- reveals next variant;
- may wake another active pod.

Layered cells can deliberately cross families and colors.

## 14. Keys/unlocks

Key may overlay a visible target variant.

Clearing supporting layer collects key and resolves linked lock.

Key does not consume extra work beyond its supporting tile.

## 15. Win

Win when:
- all required target layers are cleared;
- mandatory specials resolved;
- no required pod work remains.

## 16. Jam

Jam when:
- level incomplete;
- all usable Waiting Slots occupied;
- every active pod has zero reachable matching cells;
- no pending automatic event can change state.

A full Buffer is not a Jam if any pod is still progressing.

## 17. No time pressure

Baseline:
- no timer;
- no move limit.

## 18. Determinism

Same level + same tap sequence = same logical outcome.

Mystery values are fixed in level data.

## 19. Core strategic effect of variants

More variants increase:
- Source ordering complexity;
- chance of committing a premature pod;
- number of plausible exposed choices;
- partial-pod persistence;
- layered dependency combinations.

This is why global target variety must exceed four.

## 20. Pre-lock tests

Must verify:
- two variants of same family active simultaneously;
- Leaf pod does not clear Moss;
- exact per-variant accounting;
- partial completion;
- same-variant priority;
- layered cross-family reveal;
- full Buffer still progressing;
- true Jam;
- key/lock resolution;
- connected groups with multiple variants.
