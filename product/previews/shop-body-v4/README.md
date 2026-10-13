# Garden store, revision 4

Actual shared-layout Skia renders; not concept mockups or Unity device captures.

- `17-store-19.5x9.png`: subtle card depth, separated artwork/reward panels, centered bold Best Seller lines and consistent currency artwork placement.
- `53-store-packs-19.5x9.png`: all six Petals offers and compact booster cards matched to the owner's latest reference.
- `54-store-body-purchases-19.5x9.png`: scrolled content after a three-charge booster purchase.
- `board-sheet.png`: the three fixtures at 16:9, 19.5:9 and 21:9.

The raised lower slab is removed from procedural card surfaces and replaced by an even narrow shadow. Quantity labels are positioned from fitted icon bounds. Currency illustrations are cropped by their measured visible bounds at sprite creation; no source image file is edited.

Final result: `checks: OK` on all three shapes, including existing purchase, cancellation, fixed-navigation, vertical-scroll and featured-card containment checks. Unity runtime code compiles against the repository's .NET Unity API stubs with zero warnings/errors. Device rendering was not tested.
