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

- All 5000 levels of the level catalog, embedded straight from `content/catalog/levels/` (the owner's build of
  2026-10-07 with `tools/catalog`, spec 001 T188): the curated Levels 1–10 on 11×12–12×12 boards, the mechanic
  showcases and their practice levels (`content/showcase/`), and from L11 the generated levels on every board size from
  14×16 to 22×28 (FR-008 as amended on 2026-10-07), all with approved pictures and validated. `PlaytestContent` reads a
  level only when a screen first asks for it, so the start stays as quick as with 100 levels. A new catalog build comes
  in with the next APK, with nothing to copy.

  Past L5000 the levels repeat. A level whose data stores the icons look (a big board, over 288 cells) draws no
  next-layer chip in either APK (FR-036 as amended).
  A pod tap goes in only when a slot shows no pod on screen (spec 001 FR-014 as amended on 2026-10-05).
- The design board's screens (spec 002, `specs/002-ux-design-board/`), drawn without art assets by the engine-free
  screens of `playtest/design/` through `IPainter` (`AndroidPainter` on the phone):
  - a splash (frame 1), the lotus loader (spec 005 FR-039): the logo and the lotus on the parchment with a ring of
    petals filling, then the lotus iris opens on Level 1 on the very first launch and on Home later; the win's Next
    closes the iris, shows "Level N" and opens it on the next level;
  - Home in the reference layout (spec 005 FR-024, `contracts/look.md` §6.4) in its early look (frame 2) and, once the
    features unlock, the progressed look (frame 3): the header row on top (the owner's request of 2026-10-04: Settings
    at the left, the large Petals pill centered with the Play button's leaves and flower on its corners, the profile
    avatar at the right with the chosen avatar picture, whose tap opens the profile page), the wooden logo, the owner's
    layered Home with the four animated heroes on its fountain (spec 005 FR-028: each idles, they take turns to react,
    a tap on one makes it react, petals drift over them; without the owner's pictures the drawn stand-in's four still
    heroes around the lotus fountain), each in its outfit once the Wardrobe is open, the owner's two animated promo
    scenes under the logo (spec 005 FR-032: No Ads at the left from level 1, whose tap opens the Remove Ads card at every
    level; the Daily Reward at the right from its unlock, whose tap opens its card; each idles and in turn plays its
    attention sequence while it calls; a wooden sign with the label while their pictures are missing), the level plaque,
    the big Play, "N levels to reward" and the Daily Challenge's cream round side button under the Daily Reward's scene;
  - the owner's wooden bottom menu (spec 005 FR-030, §6.7) on Home and the four pages: the Shop, the Wardrobe, Home, the
    Leaderboard and the Collection, always all five (the owner's request of 2026-10-04), a locked one with a padlock
    badge on its icon, the screen's own place raised in the round medallion; a tap goes straight to the Store page, the
    Wardrobe, Home, the Leaderboard page or the Collection page (every place a page, the owner's request of the same
    day: "All the menu's places must be a separate page. Not popups."), and a locked place's page says "Available from
    level N" (the roadmap's level) instead of its content (it replaced Home's Store, Wardrobe and Collection side buttons
    and the rank pill);
  - the Wardrobe (frame 27, spec 005 FR-025, §6.5), opened from the bottom menu: the header on one line (back, the "Wardrobe"
    banner, the Petals pill), the hero on its pedestal between ‹ › family
    arrows, the name card, the family tabs and outfit cards three to a page (owned items to wear, items for sale to buy
    with Petals, and the ones earned later with a padlock), through the client's `WardrobeService`;
  - the Store page (frames 17 and 26, spec 005 FR-029, §6.6), opened from the bottom menu's Shop and the Petals "+" of
    Home or the Wardrobe: the Wardrobe's header ("Store"), then on a parchment panel the Shop / Cosmetics tabs, the
    Shop's rows (boosters for Petals; the real-money rows unavailable) and the cosmetics' outfit cards; its back (and the
    system back, which also closes the other pages) returns to where it was opened;
  - the Leaderboard page (frames 5 and 31, §6.8) and the Collection page (frames 6 and 20, §6.9), opened from the bottom
    menu on the Store page's frame (the Wardrobe's garden, the header with their banner and the Petals pill, the
    parchment panel): the Leaderboard in its offline form (placeholder top ranks with medals, the gap, "You" with the
    highest completed level, the offline line and Refresh); the Collection's count and framed pictures newest first,
    three to a row, a page at a time between the page arrows, and a picture's detail on the page; back and the system
    back return to Home (from a picture's detail, to the grid first);
  - the level (frames 7–9) in the reference's layout (spec 005 FR-020, FR-021): the cream Pause, the wooden level sign
    with ivy and the HARD or SUPER HARD badge, the cream ▶▶▶ fast-forward pill (lit while on, 3×), the board of candy tiles wide in its stone border on
    the lawn (the Garden Entries have no arch: the Bloomlings set off from the border), and one parchment tray to the
    bottom of the screen with the Waiting Slots,
    the four booster boxes and a column of pods per Source stack, one after another and never on each other (the
    owner's gameplay rule of 2026-10-03): the exposed pod on top, bright and the only one taking taps, and the next
    ones muted under it, three rows or four from 19.5:9, "+N" on the last shown pod; the pods slide up a row when the
    exposed one leaves and down when Return puts one back. Frames 12–14 show the pod, slot and booster states;
  - cards: pause and Settings (frame 11), the jam card centered on the screen (frame 10), the win card (frame 15) and the milestone
    card (frame 16) with the level's animated hero (its reaction as it appears, then its idle), the Daily Reward
    (frame 4) and the Remove Ads card (spec 005 FR-033, preview frame 32: the No Ads scene idling, what it does, the
    purchase unavailable offline and Restore Purchases, which says so); the system back closes the top card as its ✕.

  The design kit (tokens, shapes, garden backdrop, layouts, asset slots) is the Unity client's engine-free
  `client/Assets/Bloomlings/UI/Design/`, linked. Everything is drawn in the reference look of spec 005
  (`specs/005-reference-look/`, recipes in `contracts/look.md`) on the spec 003 Garden kit: sentence-case labels in
  Nunito (embedded from `client/Assets/Bloomlings/UI/Fonts/Resources/`, SIL OFL); every button raised on a wooden plate
  (glossy green main buttons, cream secondaries, icon buttons with raised glyphs; FR-044, FR-045); laminate wooden
  signs; popups in a wooden frame round a cream panel with raised rows and the title on a sign (FR-045); the board as
  candy tiles, soft cubes (FR-044), in a stone border on a lawn; wooden pods (the owner's icon over the middle, a small
  outlined count at the bottom right corner) and cream Waiting Slots (the tile, the count below it); cream booster tiles with green count
  badges; Petals as a pink lotus.
  Material pictures (planks, frames, stones, pedestal, candy tiles) come from the kit's `UiRaster` through
  `IPainter.Picture`, cached by key and size; `BoardLayout` places the board. The Bloomlings are the generated character
  art of spec 004 (`specs/004-character-art/`), embedded from
  `client/Assets/Bloomlings/Art/Characters/Resources/Characters/`: 2D characters whose shape is the variant symbol, as
  walkers and on the Bloomlings sheet; 3D heroes on the splash, Home, the win and milestone cards and the leaderboard
  row. The owner's pictures (`specs/005-reference-look/pictures.md`) are embedded from `Art/Backgrounds/`, `Art/Brand/`,
  `Art/Icons/` and `Art/Decor/` when they exist, and replace the drawn backdrop, wordmark, booster icons or leaves
  (mirrored with `IPainter.PushSquash(-1, 1, …)`). The owner's animated heroes (spec 005 FR-028, made by
  `tools/heroanim`) are embedded from `Art/Heroes/Resources/HeroMotion/` under `heromotion/`: both painters decode a
  frame when it is first drawn and keep the frames in a cache bounded by bytes (the least recently drawn dropped first),
  never all 576; the layered Home's pictures (`home.jpg`, `home-*.png`) come with the backgrounds. The level tester
  keeps the system font and its minimal look. There are no ads or real-money purchases here, so those buttons show as
  unavailable, and the jam rescue is granted without an ad. A small dev row in the Settings card (from Home or a level's
  pause; "dev L<n>", then −1, +1, +10, Reset; it lay at Home's bottom before the bottom menu) moves the progression for
  testing; used from a level it returns Home.
- Progression and economy are the Unity client's own engine-free services, linked from `client/` (never copied):
  the save file, the unlock roadmap (boosters open at L3, L4, L6 and L9 with a free charge; mechanics, Hard and Super
  Hard as in the spec), Petals for wins, booster charges bought with Petals, level drops, milestone rewards, the Daily
  Reward, the Collection and the Wardrobe's cosmetics for sale.
- Animation: the rules resolve a tap at once in the core; `LevelAnimator` then plays the events round by round, like
  the Unity client's timeline: a pod flies from the tray to its slot, Bloomlings walk from the Garden Entry to their
  tiles, each tile shrinks away when its Bloomling arrives, slot counts drop, a finished pod leaves, locks stay until
  their key's wave, and the win or jam card waits for the last wave. Fast forward (3×) and backlog compression change only the
  pace. A harness replays every golden case and every showcase solution, with pauses and with rapid taps, and checks
  that the settled screen equals the rules state.
- Demos once each, with the Unity client's texts (`Strings_en.csv`, embedded): the Level 1 tap hint, each booster at
  its unlock, each mechanic the first time a level uses it, a new variant, and "Match the exact symbol".

The profile page (spec 005 FR-037, preview frames 39–41) opens from Home's avatar: the player's card (avatar, name,
ID, joining month, level), three stats and placeholder achievements; its "Edit profile" card picks one of the owner's
14 avatars (four free, ten bought once with Petals at 300 / 600 / 1200), the frame and badge (once the Wardrobe is open)
and the name, asked with Android's text dialog.

The Daily Challenge is not in the playtest (its Home button says so), nor is the Wardrobe's profile tab (the Store
still sells frames and badges in its cosmetics tab, frame 26, and the profile's edit card shows them); sign-in, cloud
save, ads and analytics live in the Unity client.

## Preview without a phone

`dotnet run --project playtest/preview` renders the full playtest's screens with SkiaSharp. It writes one PNG per
design board frame (1–17) plus extras 18–32 (themes, Settings, a Collection picture, a demo, boosters in use, the
Bloomlings sheet, 25 the reference-look kit sheet, 26 the Store page's cosmetics in the Wardrobe look, opened from the
Wardrobe's Petals "+", 27 the Wardrobe, reached by taps that the frames check (5 opens the Leaderboard page from the
menu and checks its back, the system back, the menu from page to page and its Shop; 6 opens the Collection page with
87 pictures and checks its page arrows, a picture's detail and back to the grid, then to Home; 17 opens the Store page
from Home's Petals "+" and the bottom menu's Shop and checks its back and the menu's Home; 20 taps a picture for its
detail; 27 walks the menu's Wardrobe, Shop, Leaderboard and Collection), 28 Home's animated heroes in outfits, which
also checks a tap on a hero and on Play, and the bottom menu's locked places: 29 the locked Store page at Level 5, 30
the locked Wardrobe at Level 15 (its Shop and Petals "+" open the Store page, whose back returns there) and 31 the
locked Leaderboard page on a new profile (after the locked Collection page's check), each checking what the notice
says, and 32 the Remove Ads card opened from Home's No Ads scene at Level 15, which checks its Restore and the system
back; frames 2 and 3 also check which promo scenes show and the Daily Reward scene's tap)
at 16:9,
19.5:9 and 21:9 into `playtest/preview/out/`, and a contact sheet `board-sheet.png` to compare with the board. It fails
when a drawn shape or slot is not registered, a touch target is too small or overlaps another, or text leaves the
safe area. It also checks that every animated hero frame is embedded and decodes to its size in the kit, and prints
how many frames it decoded. `-- --inventory` also writes `specs/002-ux-design-board/asset-inventory.md` from the asset
slot registry.
`-- --before <sheet.png>` also writes `before-after.jpg`, that older sheet above the new one (the spec 003 review).
`-- --sounds` only writes every synthesized clip (the cues and the clearing sounds, spec 005 FR-042) as a WAV file and
`schedule.json`, what a pod of eight tiles plays in each clearing style at 1× and 3×, to `playtest/preview/out/sounds/`.

Both APKs play the Unity client's synthesized sounds (`ToneSynth`, linked) through a SoundPool and its haptic patterns
on the vibrator (`PlaytestSound`), as the client's `FeedbackPolicy` decides: each clearing style's act sounds and its
collect climbing the pod's pentatonic ladder, and one micro haptic a tile (composed transients from Android 11, the
predefined tick from Android 10, a soft pulse where the phone has amplitude control, none where it can only buzz).


## Build

- **GitHub**: Actions → **android-apk** → Run workflow, and pick `both`, `playtest` or `tester`. The APKs are the
  `bloomlings-playtest-apk` and `bloomlings-tester-apk` artifacts of the run. The workflow is manual only and keeps
  only the newest APK of each kind.
- **Locally** (needs the Android SDK and JDK 17): `dotnet workload install android`, then
  `dotnet publish playtest/android -c Release -f net10.0-android` or the same for `playtest/tester`.
- `dotnet run --project playtest/check` checks the animator and the meta layer without Android.

The APKs and the preview take their levels from `content/catalog/levels/`, so a catalog build reaches them with no copy.
