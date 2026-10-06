# Device and Editor Checklist: UX Design Board

**Purpose**: The checks of spec 002 that need a phone, the Unity Editor or players. They cannot run in the cloud
session where the design was built.

**Feature**: [spec.md](../spec.md) | **Created**: 2026-09-30

Before these steps, the automated checks must pass (see [quickstart.md](../quickstart.md)):
- the client tests;
- `playtest/check`;
- `playtest/preview`;
- the core and backend suites.

## Full playtest APK (Actions → android-apk → `apks: playtest`)

- [ ] On a fresh install, the splash shows, then Level 1 starts with no tap in between (FR-016).
- [ ] After a few wins, Home looks like frame 2: Petals pill, Settings, LEVEL N and PLAY.
- [ ] With the dev row (+10), the features appear as they unlock (FR-017):
  - L10: the rank row;
  - L12: the "+" on the Petals pill;
  - L40: the hero, Wardrobe and the avatar;
  - L50: the Daily Challenge card;
  - L7: the Daily Reward popup opens once a day.
- [ ] The gameplay screen matches frames 7–9 on the phone:
  - the top bar, the badge and the board;
  - the slots, the tray and the booster bar at the bottom;
  - nothing cut off at the notch or the navigation bar.
- [ ] The pod states of frame 12 and the slot states of frame 13 look right on a real level, including the red dashed
  danger slot.
- [ ] The booster bar appears only from L3, and adds a booster at L4, L6 and L9 (frame 14).
- [ ] The pause card, the jam sheet, the win card and the milestone card (at L25) match frames 11, 10, 15 and 16.
- [ ] The Store buys boosters with Petals, and the Collection shows won pictures (frames 17 and 6).
- [ ] 2× speed changes only the animation pace (FR-028, spec 001 FR-069).
- [ ] Sound and haptics follow the Settings toggles.

## Level tester APK (`apks: tester`)

- [ ] It looks and behaves as before: ◀ ▶ between levels, free boosters, instant results (FR-003).

## Unity client (Unity 6.3 LTS, `client/README.md`)

- [ ] The project compiles in the Editor with no errors. The client scripts were only type-checked against stubs.
- [ ] The splash shows while Boot loads, then fades.
- [ ] Home, the gameplay HUD and every card look like the playtest previews. Fix anything that differs, such as
  anchors or pill corner sizes.
- [ ] TextMeshPro outlines on labels do not blur at small sizes. If they do, drop the outline on body text.
- [ ] The HUD layout is right on 16:9, 19.5:9 and 21:9 in the Game view (SC-007).

## Players and devices

- [ ] SC-004: in 10 first-time players, at least 9 tell apart 5 variants on one board within 5 seconds.
- [ ] SC-005: on the reference low-end device, the level screen runs at 30+ fps with no hitch over 100 ms. This
  covers the backdrop, the rasterized shapes and a full board.
- [ ] SC-006: first-time players find and tap PLAY within 3 seconds of Home appearing, in 9 of 10 attempts.
- [ ] SC-001: the product owner reviews `playtest/preview/out/board-sheet.png` side by side with the board and
  accepts at least 15 of the 17 frames. Deviations are recorded in `contracts/screen-map.md`.

## Notes

- The playtest has no ads, real money, server or Wardrobe screen. Those buttons say so.
