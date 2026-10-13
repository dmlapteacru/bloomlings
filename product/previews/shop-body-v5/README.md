# Garden store, revision 5

Actual shared-layout Skia renders; not concept mockups or Unity device captures.

- `17-store-19.5x9.png`: smooth raised card rims, fuller reward panels and extra purchase-button/artwork padding.
- `53-store-packs-19.5x9.png`: gold Petals rims, blue booster rims, molded price capsules and bundle spacing.
- `54-store-body-purchases-19.5x9.png`: scrolled content after a three-charge booster purchase.
- `board-sheet.png`: the three fixtures at 16:9, 19.5:9 and 21:9.

Card rims use continuous directional lighting and 2-by-2 pixel sampling, replacing abrupt concentric color bands. Reward-panel faces and booster-price capsules have a thicker rounded rim and a gentle raised highlight. Code-built bundles leave 5.5% below the money button; booster cards leave 7.5% below their Petals capsule. Original PNG assets remain unchanged.

Shop checks passed on all three screen shapes, including purchase, cancellation, fixed navigation, vertical scrolling and featured-card containment. Runtime code compiled against the repository's .NET Unity API stubs with zero warnings/errors. Device rendering was not tested.

The full harness returned one failure on two runs in the separate disk-picture-store eviction check: `store: over budget it keeps 20518 bytes, or dropped the newest picture`. The disk-store implementation and its check were not modified by this revision. All shop checks passed; this is not a claim that the whole harness is green.
