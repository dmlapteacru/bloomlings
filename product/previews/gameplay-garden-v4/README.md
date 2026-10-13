# Garden gameplay, revision 4

Actual shared-code Skia renders, not concept mockups or Unity device captures.

- Gameplay: normal, hard and super-hard fixtures with thicker colored pod rims, pale faces and taller lower lips.
- Pod states: unchanged half overlap, full-opacity lower icons and lower-corner metadata.
- Slot states: compact square plates, larger centered stickers and outlined counts at the bottom right. Mystery, stuck, locked, danger, extra-slot and Return states remain supported.
- Booster and win fixtures retain the existing artwork and behavior.
- `board-sheet.png`: ten fixtures across 16:9, 19.5:9 and 21:9, including demo, Shuffle and Bloom Burst.

Geometry:

- Procedural pod rim: 5.5% of the shorter side, with an additional 8.5% lower lip, pale tinted inner surface and the existing 24% radius. Mild lower-edge shading provides depth without restoring the old tube bevel.
- Waiting row: 14% of safe width high, with the existing short-screen scale. `SlotTile` and `SlotCount` supply matching native/playtest layout; counts preserve their existing animation.
- Fence docks to the bottom of the available board region at the same fitted cell size. Arches grow to 2.1 cells wide, retain the original illustration aspect, and reach the waiting-slot panel. Side entries retain their rotation and walker origins.

Validation:

- Native runtime/UI importer compile against Unity API stubs: zero warnings/errors.
- `ReferenceLayoutTests` and `PodLinksTests`: 30 passed, including square selected slots, corner-count overlap and bottom-arch connection across phone/board shapes.
- Ten fixtures across three screen shapes: `checks: OK`.
- Inspected normal/hard gameplay, short-screen super-hard gameplay, and slot states.

Background artwork and upper navigation are unchanged. Unity Editor/device and Android APK builds were not run.
