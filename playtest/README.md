# Temporary playtest client (Android, no Unity)

A small .NET for Android app that plays the real levels on the shared deterministic core (`core/src/Bloomlings.Core`,
`core/src/Bloomlings.Content`). It exists so a playable APK can be built on a stock GitHub runner with **no secrets**:
the .NET Android workload and the runner's Android SDK are free, while the Unity client (`client/`) needs a Unity
licence. The Unity client stays the product client (doc 15). This one is for playtesting the rules and levels until
the Unity build runs.

The same sources build two APKs, which install side by side:

| APK | Project | For |
|---|---|---|
| **Bloomlings Playtest** (`com.bloomlings.playtest`) | `playtest/android` + `playtest/design` | the game as a player meets it, in the look of the UX design board (spec 002): splash, Home, progression and unlocks, Petals and booster charges, milestones, Daily Reward, Collection, Store, demos, animations |
| **Bloomlings Tester** (`com.bloomlings.playtest.tester`) | `playtest/tester` (`PLAYTEST_TESTER`) | quick level testing in the first playtest's minimal look (`TesterView`): levels open straight away, ◀ ▶ move between them, every booster is free, a tap shows its result at once, no Home, progression, economy or demos |

`PlaytestFlavor` holds the difference, and `MainActivity` picks `Droid.DesignView` or `TesterView`.
`playtest/Playtest.Shared.props` holds everything the two share. The tester compiles only `playtest/android/*.cs`; the
full playtest also compiles the designed screens of `playtest/design/` and the host of `playtest/android/Design/`.

## What the full playtest has

- Levels 1–94 in `playtest/content/levels/` (refreshed 2026-09-29):
  - the curated Levels 1–10;
  - the mechanic showcases and their practice levels (`content/showcase/`);
  - 34 levels generated with `gen-1.2.0 --allow-draft`, which follows the Level Band Guidelines;
  - 33 older preview levels (21, 23–25, 42, 55–94 where the new generator found no level). The 107-picture library
    is too small for the current rules: Levels 1–100 use distinct pictures (FR-083), and from L51 a level needs 5
    variants, so a picture with 5+ color roles; only 44 pictures have them. These older levels predate the band rules
    (4 variants, pods under 5 tiles) and some repeat a picture. They are playable, but not catalog levels.

  Past L94 the levels repeat. Draft pictures are used as in-memory previews, as `publish --allow-draft` does.
- The design board's screens (spec 002, `specs/002-ux-design-board/`), drawn without art assets by the engine-free
  screens of `playtest/design/` through `IPainter` (`AndroidPainter` on the phone):
  - a splash (frame 1), then Level 1 on the very first launch and Home later; over the owner's layered Home its four
    animated heroes fade in on the fountain already in Home's motion, so Home takes over without a jump;
  - Home in the reference layout (spec 005 FR-024, `contracts/look.md` §6.4) in its early look (frame 2) and, once the
    features unlock, the progressed look (frame 3): Settings and the Petals pill on top, the wooden logo, the owner's
    layered Home with the four animated heroes on its fountain (spec 005 FR-028: each idles, they take turns to react,
    a tap on one makes it react, petals drift over them; without the owner's pictures the drawn stand-in's four still
    heroes around the lotus fountain), each in its outfit once the Wardrobe is open, the level plaque, the big Play,
    "N levels to reward", and cream round side buttons for the Wardrobe, the Collection, the profile avatar, the Daily
    Challenge and the Store, with the rank pill (offline);
  - the Wardrobe (frame 27, spec 005 FR-025, §6.5), opened from Home: the hero on its pedestal between ‹ › family
    arrows, the name card, the family tabs and outfit cards three to a page (owned items to wear, items for sale to buy
    with Petals, and the ones earned later with a padlock), through the client's `WardrobeService`;
  - the level (frames 7–9) in the reference's layout (spec 005 FR-020, FR-021): the cream Pause, the wooden level sign
    with ivy and the HARD or SUPER HARD badge, the cream 2× pill, the board of candy tiles wide in its stone border on
    the lawn, the entry strip with the arch, and one parchment tray to the bottom of the screen with the Waiting Slots,
    the four booster boxes and one deck per Source stack (the front pod, the next two peeking above it, "+N"), with the
    pod, slot and booster states of frames 12–14;
  - cards: pause and Settings (frame 11), the jam bottom sheet (frame 10), the win card (frame 15) and the milestone
    card (frame 16) with the level's animated hero (its reaction as it appears, then its idle), the Daily Reward
    (frame 4), the Leaderboard in its offline form (frame 5), the Collection (frame 6) and the Store (frame 17).

  The design kit (tokens, shapes, garden backdrop, layouts, asset slots) is the Unity client's engine-free
  `client/Assets/Bloomlings/UI/Design/`, linked. Everything is drawn in the reference look of spec 005
  (`specs/005-reference-look/`, recipes in `contracts/look.md`) on the spec 003 Garden kit: sentence-case labels in
  Nunito (embedded from `client/Assets/Bloomlings/UI/Fonts/Resources/`, SIL OFL); glossy green main buttons in a light
  wood rim, cream secondary and round buttons; wooden signs; parchment cards with a cream round close; the board as
  candy tiles in a stone border with stone arch entries on a lawn; wooden pods and cream Waiting Slots, both holding the
  variant's candy tile and its plain count; cream booster tiles with green count badges; Petals as a pink lotus.
  Material pictures (planks, frames, stones, arch, pedestal, candy tiles) come from the kit's `UiRaster` through
  `IPainter.Picture`, cached by key and size; `BoardLayout` places the board. The Bloomlings are the generated character
  art of spec 004 (`specs/004-character-art/`), embedded from
  `client/Assets/Bloomlings/Art/Characters/Resources/Characters/`: 2D characters whose shape is the variant symbol, as
  walkers and on the Bloomlings sheet; 3D heroes on the splash, Home, the win and milestone cards and the leaderboard
  row. The owner's pictures (`specs/005-reference-look/pictures.md`) are embedded from `Art/Backgrounds/`, `Art/Brand/`,
  `Art/Icons/` and `Art/Decor/` when they exist, and replace the drawn backdrop, wordmark, booster icons or leaves
  (mirrored with `IPainter.PushSquash(-1, 1, …)`). The owner's animated heroes (spec 005 FR-028, made by
  `tools/heroanim`) are embedded from `Art/Heroes/Resources/HeroMotion/` under `heromotion/`: both painters decode a
  frame when it is first drawn and keep the frames in a cache bounded by bytes (the least recently drawn dropped first),
  never all 288; the layered Home's pictures (`home.jpg`, `home-*.png`) come with the backgrounds. The level tester
  keeps the system font and its minimal look. There are no ads or real-money purchases here, so those buttons show as
  unavailable, and the jam rescue is granted without an ad. A small dev row at the very bottom of Home (−1, +1, +10,
  Reset) moves the progression for testing.
- Progression and economy are the Unity client's own engine-free services, linked from `client/` (never copied):
  the save file, the unlock roadmap (boosters open at L3, L4, L6 and L9 with a free charge; mechanics, Hard and Super
  Hard as in the spec), Petals for wins, booster charges bought with Petals, level drops, milestone rewards, the Daily
  Reward, the Collection and the Wardrobe's cosmetics for sale.
- Animation: the rules resolve a tap at once in the core; `LevelAnimator` then plays the events round by round, like
  the Unity client's timeline: a pod flies from the tray to its slot, Bloomlings walk from the Garden Entry to their
  tiles, each tile shrinks away when its Bloomling arrives, slot counts drop, a finished pod leaves, locks stay until
  their key's wave, and the win or jam card waits for the last wave. 2× speed and backlog compression change only the
  pace. A harness replays every golden case and every showcase solution, with pauses and with rapid taps, and checks
  that the settled screen equals the rules state.
- Demos once each, with the Unity client's texts (`Strings_en.csv`, embedded): the Level 1 tap hint, each booster at
  its unlock, each mechanic the first time a level uses it, a new variant, and "Match the exact symbol".

The Daily Challenge is not in the playtest (its Home button says so), nor is the Wardrobe's profile tab (the Store
still sells frames and badges in its cosmetics tab, frame 26); sign-in, cloud save, ads and analytics live in the
Unity client.

## Preview without a phone

`dotnet run --project playtest/preview` renders the full playtest's screens with SkiaSharp. It writes one PNG per
design board frame (1–17) plus extras 18–27 (themes, Settings, a Collection picture, a demo, boosters in use, the
Bloomlings sheet, 25 the reference-look kit sheet, 26 the Store cosmetics in the Wardrobe look and 27 the Wardrobe,
reached by taps that the frame checks) at 16:9,
19.5:9 and 21:9 into `playtest/preview/out/`, and a contact sheet `board-sheet.png` to compare with the board. It fails
when a drawn shape or slot is not registered, a touch target is too small or overlaps another, or text leaves the
safe area. It also checks that every animated hero frame is embedded and decodes to its size in the kit, and prints
how many frames it decoded. `-- --inventory` also writes `specs/002-ux-design-board/asset-inventory.md` from the asset
slot registry.
`-- --before <sheet.png>` also writes `before-after.jpg`, that older sheet above the new one (the spec 003 review).


## Build

- **GitHub**: Actions → **android-apk** → Run workflow, and pick `both`, `playtest` or `tester`. The APKs are the
  `bloomlings-playtest-apk` and `bloomlings-tester-apk` artifacts of the run. The workflow is manual only and keeps
  only the newest APK of each kind.
- **Locally** (needs the Android SDK and JDK 17): `dotnet workload install android`, then
  `dotnet publish playtest/android -c Release -f net10.0-android` or the same for `playtest/tester`.
- `dotnet run --project playtest/check` checks the animator and the meta layer without Android.

To refresh the preview levels after regenerating them, copy the batch files into `playtest/content/levels/`. Keep
exactly one file per level number, and keep Levels 1–10 from `content/curated/`.
