# Garden gameplay, revision 7

Actual shared-code Skia renders, not concept mockups or Unity device captures.

- A fine antialiased near-black tinted inner contour and stronger channel shadow separate each pod rim from its cream face.
- Rim saturation increases from 0.94 to 1.06. Leaf, moss, vine, water and dew rims darken by 18%; other unlocked rims retain 10% darkening. Interior tints still use the original catalog hues.
- Rim highlights brighten with a soft contour glow inside the existing silhouette.
- Selectable pods retain a single lower side plane, with stronger vertical shadow contrast and subtle shading on its right side. Covered and locked pods keep a thin lower edge.

Validation: native runtime/UI importer compile against Unity API stubs has zero warnings/errors; 30 existing layout/link tests passed. Three fixtures at 19.5:9 passed the render harness: normal/hard gameplay and pod states. Inspected hard gameplay and all pod states.

Geometry, icon sizes, count placement, background artwork, navigation, boosters, board and gameplay rules are unchanged. Source PNGs are unchanged. Unity Editor/device and Android APK builds were not run.
