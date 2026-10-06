# Contract: Booster tile (FR-031)

One tile per booster in the booster bar, in both clients. The tile is `size.booster_tile`: 152 × 156 reference units,
at the spec 002 bar positions (centers 244 units apart).

## Layers (back to front)

1. **Selected glow** (selected only): a golden radial glow 28 units beyond the plate, pulsing between 55% and 100%
   alpha every 1.2 s, plus a 5-unit golden ring around the plate.
2. **Plate:** a rounded square, radius 40, cream (`garden.plate`), a 3-unit brown outline, thickness `garden.plate_depth`
   below, and a soft shadow.
3. **Tile:** inset 10 units, radius 30, the booster's `ColorSet` (gradient Top → Face), a 3-unit `Line` outline, and a
   lip (a darker bottom band of 14 units).
4. **Highlight:** a white band across the top 30% of the tile, at 55% alpha.
5. **Icon:** the booster shape, 84 units, light glyph (Bloom Burst: the petal yellow), centered on the tile face above
   its lip.
6. **Tag:**
   - **charges:** a round dark brown badge (54 units) at the top-right corner, overlapping by 12 units, with a cream
     ring and a brown outline, and "×N" in white;
   - **price:** a cream tag below the tile (96 × 46), with the Petal symbol and the price in dark brown, plus a green
     "+" badge (46 units) at the top-right corner.

## States

| Look | Shown | Touch |
|---|---|---|
| Charges | the charges tag | press → use (or start targeting) |
| Price | the price tag + "+" | press → buy and use, as today |
| Selected | raised 12 units, the glow and ring, the charges tag | press → cancel targeting, as today |
| Disabled | everything at 45% alpha and grey (desaturated), no glow | none |
| Pressed | squash `scale(1.05, 0.93)` from the bottom, the icon 5 units lower; springs back with the press curve | — |

- **Selected** applies only to Return and Bloom Burst, while the level screen waits for their target (spec 001 FR-045,
  FR-050).
- **Disabled** follows spec 001 FR-046: the booster can have no effect now, or there are no charges and too few
  Petals.
- **Hidden.** The booster bar stays hidden before L3, and each tile appears at its unlock level (spec 002 FR-014).

## Data

The tile reads only what each client already has. No rule or economy change.

| Client | Source |
|---|---|
| Playtest | `BoosterBarPainter` (charges, price, `Targeting`, `applicable && affordable`) |
| Unity | `BoosterBar` (`BoosterButtonState`, `SetTargeting`) |
