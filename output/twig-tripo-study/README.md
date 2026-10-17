# Twig: segmented Tripo fitting study

The three preview variants use one unchanged base body / head. Extracted acorn and cap pieces replace headwear; the
different faces / bodies that Tripo generated with them are excluded. The completed head exports share exactly the
same head, ears, mouth, UV, paint and world size, verified from their GLB buffers and transforms.

`comparison.png`: front, same camera / scale. `angles.png`: three-quarter view, same camera / scale.
`acorn-side.png`, `acorn-back.png`, `cap-side.png`, `cap-back.png`: actual 3D fit checks.
`TwigHero_Tripo_Blender_Import.zip`: bald base, two complete heads, two fitted headwear-only GLBs and Russian import notes.

The original ZIPs are preserved under `tools/heroanim/models/twig-tripo/source/`. Build recipe, segmentation decisions
and reproducible checks: [source record](../../tools/heroanim/models/twig-tripo/README.md).

This is a static fitting study: the delivered models have no skeleton / animations, and their original generated
paint still contains gray eye whites and small facial / surface artifacts. The restored Twig in the game remains
active. No APK is built or changed by this study, and no separate editable Blender master is created.
