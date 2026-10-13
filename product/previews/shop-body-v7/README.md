# Garden store, revision 7

Actual shared-layout Skia renders; not concept mockups or Unity device captures.

- `17-store-19.5x9.png`: `BEST SALE` moved left to the leaf's center.
- `53-store-packs-19.5x9.png`: fuller booster-price capsule faces and larger currency icons centered in the gap before the price.
- `54-store-body-purchases-19.5x9.png`: scrolled content after a three-charge booster purchase.
- `board-sheet.png`: the three fixtures at 16:9, 19.5:9 and 21:9.

Unity and the playtest use the same `ButtonPetal` and `ButtonLabel` geometry. The capsule's extra face highlight and shade are specific to booster-price buttons. Other panels, navigation, purchase logic and source PNGs remain unchanged.

Final result: `checks: OK` on all three shapes, including purchase, cancellation, fixed navigation, vertical scrolling and featured-card containment. Runtime code compiled against the repository's .NET Unity API stubs with zero warnings/errors. Device rendering was not tested.
