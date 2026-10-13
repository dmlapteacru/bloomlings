# Garden gameplay, revision 5

Actual shared-code Skia renders, not concept mockups or Unity device captures.

- Selectable source pods have a rounded front face above a darker lower side, with a curved seam and narrow reflection separating the two planes. Covered and locked pods retain only a thin lower edge.
- A narrow rim highlight and soft inner channel shadow separate the frame from the cream interior. Interior colors mix warm cream with a light variant tint.
- The exposed icon moves into the shorter front surface, leaving the new lower-side treatment visible. Covered icons retain their near-half-card size and 50% overlap.
- Booster sprite bounds remove uneven atlas margins. The four gameplay icons now use centered 80%-of-card boxes in Unity and Skia; Android uses the same crop metadata. Shop reward icons retain their layout and use the centered sprites.

Validation: native runtime/UI importer compile against Unity API stubs has zero warnings/errors; 30 existing layout/link tests passed. Six fixtures at 19.5:9 passed the render harness, including normal/hard/super-hard gameplay, pod/booster states and Store. Inspected hard gameplay, booster states and Store.

Background artwork, top navigation, board layout and game rules are unchanged. Source PNGs are unchanged. Unity Editor/device and Android APK builds were not run.
