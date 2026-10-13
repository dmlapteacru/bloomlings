# Profile tuning, revision 2

Owner correction: compact Edit profile summary, current wooden level plaque, seamless edit button with larger/heavier pencil.

- Shared editor summary height reduced from 0.24 W to 0.18 W (25%); width reduced from 0.92 W to 0.86 W. Avatar/name/level positions remain one shared layout for all four tabs; the body gains vertical room.
- Level now uses the existing `WoodSign` with `LevelPill`, which selects `navigation/level-sign-v1`. Native Unity and the device painter use the same component/material.
- The square edit button now renders its complete smooth green face at its actual dimensions via `ShopBodyRaster.Framed`; it no longer stretches the wide purchase-button texture into a square. This removes its slice seam. Purchase buttons retain their existing nine-slice skins.
- Pencil box increased from 50% to 64% of button width, with small white silhouette offsets to thicken its stroke. Enlarged invisible touch target retained.
- No new bitmap assets were generated. Existing backgrounds, avatar art, prices and actions are reused.

Verified: native compilation against Unity API stubs and 74 tests passed; 21 fixture/shape render checks passed (39, 57, 58, 59, 60, 61, 65 on 16x9, 19.5x9 and 21x9). Reviewed editor/main/short-screen captures. At 19.5x9 the summary crop `(68,470)-(1012,672)` matches pixel-for-pixel in the Avatar, Badge, Name and Frame captures. Image inspection/comparison was read-only. Unity Editor/device execution was not performed.

The curated captures here show current main Profile and all four edit tabs, plus short-screen layouts. The full passing render batch was checked before removing redundant copies. Prior implementation snapshots remain in `../profile-game-implemented-v1/`.
