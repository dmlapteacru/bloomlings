# 11 — UX / UI Flow

**Status:** LOCKED (2026-09-29)

## 1. Primary flow

`Launch → Home → Play → Gameplay → Win → Next`

No map or level chooser.

## 2. Home

Minimal:
- Bloomlings logo;
- Level N;
- Play/Continue;
- Petals;
- Settings;
- optional Store;
- milestone teaser.

## 3. Gameplay layout

### Top
- Pause
- Level N
- 2× speed

### Center
- dense board

### Below board
- Garden Entry
- 5 Waiting Slots

### Bottom
- stacked Source Tray

### Compact side/bottom
- boosters

No Goals panel.

## 4. Pod information hierarchy

Because there can be 5–6 active target variants in a level, every pod must show:

1. exact variant icon;
2. exact variant color;
3. count;
4. family character/silhouette as secondary identity.

The family must not overpower the actual matching-type signal.

Example:
- Leaf Sprig pod uses Sprig face + leaf icon + green.
- Moss Sprig pod uses same Sprig family + moss icon + teal.

## 5. Waiting Slot information

Waiting pod shows:
- exact variant;
- remaining count;
- waiting vs active state.

Same-family variants must remain instantly distinguishable when adjacent.

## 6. Board information

Tiles show target variant icons, not hero faces.

This reduces visual noise and keeps 5–6 active variants readable.

## 7. Character animation

Bloomlings inherit pod variant styling:
- palette/accent;
- tiny variant-specific effect/accessory if needed.

Examples:
- Leaf Sprig = fresh green
- Moss Sprig = teal with moss tuft

Same base rig may be reused.

## 8. Speed

2× affects animation only.

## 9. Tutorial for second same-family variant

When Moss is introduced after Leaf:
- show both side-by-side;
- one short message such as `Match the exact symbol`;
- visually demonstrate Leaf pod ignoring Moss tile.

After this, no repeated explanation.

## 10. Accessibility

At 5–6 active variants:
- icon differentiation mandatory;
- sufficient color-distance mandatory;
- counts readable;
- colorblind-safe testing required.

Never rely on hue alone.

## 11. Win/Jam

Unchanged:
- Win → reveal → reward → Next.
- Jam → recover or Restart.
