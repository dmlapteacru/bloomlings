# Garden gameplay, revision 3

Actual shared-code Skia renders, not concept mockups or Unity device captures.

- `07-gameplay-normal-19.5x9.png`, `08-gameplay-hard-19.5x9.png`, `09-gameplay-super-hard-19.5x9.png`: colorful gameplay with larger half-card icons and flatter rounded pod rims.
- `12-pod-states-19.5x9.png`: all pod states; standalone mystery marks and larger waiting padlocks.
- `14-booster-bar-19.5x9.png`: booster states, preserving revision 2's layout and cream surfaces.
- `15-win-19.5x9.png`: finished-board icons using the same cropped art.
- `board-sheet.png`: ten fixtures across 16:9, 19.5:9 and 21:9, including slot states, demo, Shuffle and Bloom Burst.

Changes:

- Each icon is cropped at sprite creation to its measured subject bounds (alpha >= 160, with a 3-pixel gutter). This removes inconsistent transparent atlas margins without editing the PNG. Unity sprites, both playtest painters and native finished-board pixels share `GameplaySkin.PodCrop`.
- Covered icons have 47%-of-card-height boxes with enough width to display wide subjects at the same height. They remain fully inside the visible half. Waiting icons are now fully opaque.
- Pod rims use a separate flat raster recipe: 2.5% border, 24% corner radius, saturated color and a tiny contour/shadow. Tube lighting, bevels and inner recessed shading are removed. Native underlying panel geometry is hidden to preserve the rounded silhouette.
- Pod faces retain more color; mosaic board faces have less white wash and a modest saturation increase. Mystery pods show a plain outlined question mark without an inner framed tile.

Validation:

- Runtime/UI importer compile: zero warnings/errors.
- `ReferenceLayoutTests` and `PodLinksTests`: 30 passed, including half-overlap visibility, near-full-height waiting icon bounds and actual-board control alignment.
- Ten fixtures across three screen shapes: `checks: OK` (embedded art, registered slots, target sizes/overlaps, text/safe-area bounds and animation/cache checks).
- Inspected hard gameplay, pod states and short-screen super-hard gameplay.

Source PNGs, background image files, top navigation and revision 2's control/fence layout remain unchanged. Unity Editor/device and Android APK builds were not run.
