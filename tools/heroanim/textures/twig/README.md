# Twig front color cleanup

Owner-requested correction after the initial equipment pilot: the temporary vertex paint made brows look angry/jagged,
the face was too dark, and the default acorn hat looked stretched low over the head.

`front-color.png` is a clean color pass generated with OpenAI image generation from the actual bare Twig render,
using `references/twig/TwigHero_Base_Front.png` as the light-face / friendly-expression guide. The paint preserves the
actual mesh's pose and broad anatomy. It replaces the coarse facial paint with curved brows, clean sclera, brown
irises, a bright cream-gold face, peach cheeks and a clean mouth. It is a projection reference, not a conventional
unwrapped multi-view UV atlas. The source includes some stylized highlights/shading; no final all-angle PBR claim is made.

`prepare-twig-gear.py` adds `TwigFrontColor`, a planar UV layer derived from the recorded camera/crop, to the render
copy of the body. Front polygons use the image; rear/hidden surfaces retain the clean palette. Vertex colors on
painted polygons are white so glTF's COLOR_0 does not multiply the texture by the old brown/gold masks. Body vertex
positions, topology and weights are preserved. Original FBXs remain byte-for-byte unchanged in `models/twig/source/`.
This layer targets the existing front pose; full side/back paint and deformation-aware UV authoring remain future work.

The same recipe `twig-materials.json` records the acorn-only fit correction: width/depth ×1.035, upward shift 0.06,
and an additional crown lift up to 0.075 between original heights 0.9 and 1.5. It relaxes the hat and raises the crown
without changing the base head. This adjustment is applied only to the derived render copy, not the owner's FBX or
Blender master. The recipe can be reproduced in that master. The cap's geometry is retained.

Neutral warm lighting and less directional contrast keep the face clear and avoid green/muddy underside shading.
The bake retains the original runtime resolution and five-state/icon structure. Source, recipe and runtime hashes are
recorded by `twig-gear.json` and checked by `node tools/heroanim/check.mjs`.
