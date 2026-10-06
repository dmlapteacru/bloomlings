# Contract: Garden tokens (additions to spec 002 `contracts/design-tokens.md`)

Sizes are in reference units: the 1080-unit-wide design, scaled by `DesignTokens.ScaleFor`. Colors are sRGB hex. The 7
`char.*` colors of the kawaii Bloomlings (FR-032) are in [bloomling-look.md](bloomling-look.md#colors).

## Colors

| Token | Value | Use |
|---|---|---|
| `garden.plate_top` | `#FCF5E4` | plate gradient top |
| `garden.plate_bottom` | `#EBDDBE` | plate gradient bottom |
| `garden.plate_depth_color` | `#A88A5C` | the plate's thickness below it |
| `garden.outline` | `#8C6B45` | plate outline; the line of the cream and white sets |
| `garden.wood` | `#8C6B45` | board, slot-row and card frames |
| `garden.wood_depth` | `#A88A5C` | frame thickness |
| `garden.paper_top` | `#FFF9EC` | card and board surface top |
| `garden.paper_bottom` | `#F6EBD3` | card and board surface bottom |
| `garden.well` | `#E6D6B3` | empty slot well |
| `garden.well_edge` | `#B39668` | empty slot well outline |
| `garden.tab_sunk` | `#E9D9B7` | unselected tabs, the off toggle track |
| `garden.badge` | `#3B2A1A` | count badge |
| `garden.badge_ring` | `#FBF3E1` | the cream ring of a count badge |
| `garden.shadow` | `#3C2814` | soft shadows and block outlines, always with an alpha |
| `garden.label_fill_top` | `#FFFFFF` | label gradient top on colored faces |
| `garden.label_fill_bottom` | `#EEF2DA` | label gradient bottom on colored faces |
| `garden.label_plain` | `#5A3F24` | labels on cream faces |
| `garden.leaf_1`, `garden.leaf_2`, `garden.leaf_3` | `#6DBE45`, `#8BD35A`, `#5BAA3A` | decoration leaves |
| `garden.leaf_line` | `#2F6B22` | leaf outline |
| `garden.flower` | `#FFFFFF` | flower petals |
| `garden.flower_line` | `#B9B09A` | flower outline |
| `garden.flower_center` | `#FFD35C` | flower center |
| `garden.flower_center_line` | `#D29B2E` | flower center outline |
| `garden.glow` | `#FFD54A` | booster selected ring and glow |

## Color sets (base colors, derived per data-model.md)

| Set | Base | Notes |
|---|---|---|
| `set.green` | `#62B83A` (spec 005 look.md §1.3; was `#5DBB46`) | primary buttons, selected tab, the "+" |
| `set.cream` | `#F7EDD6` | secondary buttons; `Line` = `garden.outline` |
| `set.white` | `#F4EFE4` | round icon buttons, Petals pill; `Line` = `#7A6E58` |
| `set.blue` | `#8FC6F0` | level pill, pause header |
| `set.lilac` | `#B59AF0` | level pill on Super Hard |
| `set.dark` | `#3A4050` | 2× pill |
| `set.red` | `#E5484D` | close button, HARD |
| `set.purple` | `#8E4FD8` | SUPER HARD |
| `set.booster.*` | the four booster colors of spec 002 | booster tiles |

## Geometry

| Token | Value |
|---|---|
| `garden.outline_width` | 3 (2 on small elements) |
| `garden.plate_inset` | large 11, medium 8, small 5 |
| `garden.plate_depth` | 6 (3 on small elements) |
| `garden.lip` | large 13, medium 10, small 6 |
| `garden.highlight_alpha` | 0.5 |
| `garden.highlight_height` | 0.36 of the face |
| `garden.frame_width` | 6 (cards and board), 5 (slot row) |
| `garden.frame_depth` | 12 (cards), 10 (board), 8 (slot row) |
| `garden.cell_lip` | 14 |
| `garden.cell_highlight_alpha` | 0.4 |
| `garden.pod_lip` | 20 |
| `garden.deco_size` | 0.75 × button height |
| `garden.decorations` | true |
| `size.play` | 540 × 204 |
| `size.card_primary` | 620 × 160 (the owner, 2026-10-06: taller card buttons; it was 620 × 140) |
| `size.card_secondary_width` | 580 |
| `size.card_secondary_height` | 136 (it was the 110 of `size.secondary`) |
| `size.booster_tile` | 152 × 156 |

## Label look

| Token | Value |
|---|---|
| `look.outline_em` | 0.036 |
| `look.extrude_em` | 0.09 |
| `look.shadow_alpha` | 0.3 |

## Motion (unchanged from spec 002, approved on the mockup)

`motion.press` (down within the frame, 0.36 s spring back with one overshoot), `motion.pop`, `motion.sheet` (now
with one small bounce), `motion.reward`, and three new ones:

| Token | Value |
|---|---|
| `motion.breathe` | 1.6 s period, scale up to 1.03 (FR-019) |
| `motion.count_up` | 0.8 s, easing out (FR-020) |
| `motion.glow` | 1.2 s, 55% to 100% alpha (FR-031) |

The booster glow is the screen's one idle loop when shown (FR-019).
