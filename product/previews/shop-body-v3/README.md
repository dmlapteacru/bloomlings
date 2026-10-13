# Framed garden store, revision 3

These PNGs are actual shared-layout Skia renders, not concept mockups or Unity device captures.

- `17-store-19.5x9.png`: corrected featured-card inset and footer, tilted glowing Best Seller leaf, framed No Ads cards and code-built reward panel.
- `53-store-packs-19.5x9.png`: gold currency frames, blue booster frames, brown Petals prices without text outlines, rounded price capsules and pink bundle frames.
- `54-store-body-purchases-19.5x9.png`: content after a booster purchase, with unavailable prices dimmed.
- `board-sheet.png`: all three fixtures at 16:9, 19.5:9 and 21:9.

Final preview result: `checks: OK` on all three shapes. Fixture 54 also checks that the four featured reward icons stay inside the artwork's inset and the price button stays inside its red footer, alongside the existing scroll, navigation and purchase checks. Unity runtime scripts compile against the repository's .NET API stubs with zero warnings/errors. Unity and Android device rendering were not tested.

Only shop body presentation changes. Existing navigation, vertical content scrolling, economy and monetary purchase stubs retain their behavior. Source artwork and slicing settings are unchanged; the new rims and reward panels are procedural.
