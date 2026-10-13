# Garden gameplay visual update

Actual shared-code Skia renders of the playtest, not ImageGen screen concepts or Unity device screenshots.

Kept full-size previews:

- `07-gameplay-normal-19.5x9.png`: normal level, generated pod icons, a grey stuck-slot icon and an unowned booster's price.
- `08-gameplay-hard-19.5x9.png`: hard level, overlapping colored pods, compact slot panel and reused booster icons.
- `09-gameplay-super-hard-19.5x9.png`: super-hard level with board layer peeks and danger/locked slot states.
- `12-pod-states-19.5x9.png`: exposed, pressed, locked, mystery, waiting, +N, connected and empty-column states.
- `14-booster-bar-19.5x9.png`: progressive unlock, owned charges, purchase price, targeting and disabled states.
- `board-sheet.png`: all nine fixtures on all three shapes (16:9, 19.5:9 and 21:9), also including slot states, guided demo, Shuffle and Bloom Burst.

Pod cards use smooth rims matching catalog colors, a raised tinted face, generated atlas icons and outlined Lilita One counts. Cards overlap by 18% of the smaller pod's height, with deeper cards drawn first in both Unity and the playtest. Counts, +N and link badges remain below the overlap. The full solid tray is replaced by a compact panel under waiting slots. The original backdrop files, scene selection and top-navigation positions/styles remain unchanged.

The board's stone border is 0.52 cell thick: a continuous rounded rim under molded cream blocks with smaller seams. Booster icons reuse the shop atlas. New pod art also appears on waiting-slot sticker tiles and flights; board-cell symbols retain their existing artwork.

Validation:

- Runtime/UI importer compilation against the repository's .NET Unity API stubs: zero warnings/errors.
- Existing `ReferenceLayoutTests` and `PodLinksTests`, updated for the owner's new overlap/badge design: 29 passed.
- Nine rendering scenarios across three phone shapes: `checks: OK`, covering registered slots, embedded art, target sizes/overlaps, text and safe-area bounds; shared animation/cache checks also passed.
- Inspected full-size gameplay and pod-state renders, including a short 16:9 screen.
- New PNG hash matches its manifest; source prompt and ownership record are in `client/Assets/Bloomlings/Art/Gameplay/SOURCE.md`.

Unity Editor/device and Android APK builds were not run. Both renderers consume the same shared geometry; this validation does not claim device rendering parity.
