# Garden gameplay, revision 2

Actual shared-code Skia renders, not concept mockups or Unity device captures.

- `07-gameplay-normal-19.5x9.png`, `08-gameplay-hard-19.5x9.png`, `09-gameplay-super-hard-19.5x9.png`: updated gameplay on three difficulty classes.
- `12-pod-states-19.5x9.png`: pod states and a stack whose lower cards are half covered, with smaller icons in their visible halves.
- `14-booster-bar-19.5x9.png`: cream booster surfaces without wood, progressive unlock, owned charges, prices, targeting and disabled states.
- `15-win-19.5x9.png`: finished board using the same variant icons.
- `board-sheet.png`: ten fixtures across 16:9, 19.5:9 and 21:9, also covering slot states, demo, Shuffle and Bloom Burst.

Changes:

- Pods overlap exactly 50% of the lower card's height. Covered icons are square, 39% of that card's height, centered at 75% down the card; their full bounds, counts and badges remain below the overlap.
- Pod rims are reduced from 10% to 4% and use softer shading. Waiting cards are a little larger, so their visible halves remain readable.
- Booster cards use a thin cream/gold molded rim and larger atlas icons. The first and last card edges align with the actual fitted stone fence, including on narrow/tall boards. Waiting-slot plates use small equal gaps.
- Live board tiles, pods, slots, flights and the native finished-board baker use the same generated atlas. Existing color-only mosaic presentation rules are preserved.
- The fence uses touching straight stones and four curved quarter-ring corner pieces, with the same geometry in Unity and Skia/Android. Source PNGs, backdrop assets and top-navigation styles/positions are unchanged.

Validation:

- Runtime/UI importer and preview compile: zero warnings/errors.
- `ReferenceLayoutTests` and `PodLinksTests`: 30 passed, including half-overlap visibility and actual-board control alignment on multiple phone/board shapes.
- Ten scenarios on three screen shapes: `checks: OK`, including embedded art, registered slots, target sizes/overlaps, text/safe-area bounds and animation/cache checks.
- Inspected full-size hard gameplay, pod states and short-screen super-hard gameplay.

Unity Editor/device and Android APK builds were not run; these previews do not assert device rendering parity.
