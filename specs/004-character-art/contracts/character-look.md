# Contract: how the characters look (FR-004 to FR-007, FR-016)

The approved concepts are `concept-gameplay-2d.jpg` and `concept-home-3d.jpg` in this folder. `tools/artgen` must
reproduce them, with the corrections below.

## 2D characters (design square 100 × 100, y down)

Common recipe:

| Part | Rule |
|---|---|
| body | variant `Base` with a radial gradient: center (38%, 30%), radius 78%; `Base` lightened 0.42 → `Base` at 55% → `Base` darkened 0.14 |
| outline | `Line` = `Base` darkened 0.38, width 3.4; compound shapes share one outline (stroke every part, then fill every part) |
| highlight | a white ellipse at 55% opacity, rotated −30°, on the upper left of the body |
| eyes | dark brown `#3A2418` ellipses 3.3 × 4.3, 18 apart, each with a white dot of radius 1.25 up and right |
| mouth | a smile: a quadratic curve 9 wide and 4.5 deep, stroke 2.2, round caps |
| blush | two `#FF6F8E` ellipses 5.2 × 3.2 at 42%, 15 out and 6 below the eyes |
| asleep | eyes become closed arcs (a downward curve 8 wide); colors mixed 45% toward `#A7A39A` |
| worried | the mouth curves down; colors greyed (`Grey().Mix(state.stuck, 0.35)`) |
| blank | no eyes and no mouth; blush kept |

Shapes (centers in the design square):

| Icon | Shape | Face center | Details |
|---|---|---|---|
| `leaf` | a leaf blade: round bottom, its tip leaning up and to the right (78, 7), width 64 | (50, 64) | a stalk at the lower left; a light midrib with two side veins near the tip (the first build's upright leaf-drop was only 13% from the drop's silhouette) |
| `moss` | a cushion: the union of 6 circles (a large lower body and five bumps on top) | (50, 62) | a tiny two-leaf sprout on top; faint highlights on the bumps |
| `flower` | five petals (radius 19.5 at 25 from the center) around a center disc | (50, 54) | petal gap lines; a lighter center disc behind the face |
| `bud` | a tulip bud: a round cup with three pointed petal tips | (50, 66) | two green sepals at the bottom sides; two petal folds |
| `drop` | a pointed water drop | (52, 66) | a long highlight and a small dot; a light rim on the right |
| `dew` | a round dewdrop (radius 33) | (50, 60) | an inner light ring; two four-point sparkles at the upper right |
| `log` | a stump with a ring-patterned top ellipse | (50, 60) | two side stubs; two bark grooves; a small sprout on top |
| `acorn` | a nut with a cross-hatched cap and a stalk | (50, 72) | the cap overhangs the nut |
| `vine` | a curling vine sprout: a rounded body with a spiral tendril | (50, 64) | two small leaves on the tendril |
| `berry` | three round berries joined, with a leafy calyx | (50, 62) | small seed dots |
| `mist` | a soft cloud (union of circles, flat bottom) | (50, 62) | wisp lines below |
| `bark` | a chunky bark slab with vertical grooves | (50, 60) | a lichen spot |

Drawing colors:
- `Base` is the catalog color, with these exceptions so the face reads:
  - `moss`: lightened 0.22;
  - `bud`: lightened 0.2;
  - `log`: mixed 55% toward `#C58A52`;
  - `acorn`: lightened 0.06.
- Accent colors (leaf greens, cap browns, the stump top) are fixed art colors, listed in the tool's palette file.

## 3D heroes

| Family | Hero | Face |
|---|---|---|
| Sprig | a round green bean (ellipsoid 0.47 × 0.5 × 0.43), a big curled leaf with veins behind it and over its left side, a tiny sprout on top, two small feet | dot eyes and a smile on the bean |
| Bloom | a flower: a pale pink fluffy center disc with five big front petals and five deeper ones behind, cupped forward; a green stem and two leaves | happy closed eyes, an open smile with a tongue |
| Drop | a glossy blue water drop with a pointed tip, little arms raised in joy, short legs; slightly see-through edges | happy closed eyes, an open smile |
| Twig | a wooden stump with bark grooves and a ringed top, a twig with a round knob, stubby arms and feet | dot eyes and a smile on the bark |

Rendering:
- **Light:** a warm key light from the upper left with soft shadows; ambient occlusion; a golden back light wrapping
  the edges; translucency for leaves, petals and water.
- **Output:** gamma-corrected, with a mild tone curve; transparent background with the contact shadow in the alpha.
- **Solo framing:** the camera looks at (0, 0.76, 0) from 6 units away and 0.4 up, zoom 3.0, so the tallest hero (the
  flower) keeps the 2% margin; the heroes keep their slight turn, so their faces sit off center (`FaceCenterHero`).
- **Group:** the four on a round stone pedestal (worn edge, tile lines), seen from slightly above, in the order Sprig,
  Bloom, Drop, Twig, the outer two turned toward the middle.
- **Blank versions:** the same render without eye and mouth decals; blush stays.

## Corrections to the concept

These come from the owner's review and the readability rules:
- **Within-family shapes:** the eight launch shapes stay as in the concept. Moss must keep at least five visible top
  bumps, so it never reads as a circle next to Dew.
- **Flower:** the flower's petals stay clearly separated (visible gap lines), so the flower never reads as a blob.
- **Walkers:** no white halo on the light tiles; a soft ground shadow instead.
