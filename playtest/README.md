# Temporary playtest client (Android, no Unity)

A small .NET for Android app that plays the real levels on the shared deterministic core (`core/src/Bloomlings.Core`,
`core/src/Bloomlings.Content`). It exists so a playable APK can be built on a stock GitHub runner with **no secrets**:
the .NET Android workload and the runner's Android SDK are free, while the Unity client (`client/`) needs a Unity
licence. The Unity client stays the product client (doc 15). This one is for playtesting the rules and levels until
the Unity build runs.

The same sources build two APKs, which install side by side:

| APK | Project | For |
|---|---|---|
| **Bloomlings Playtest** (`com.bloomlings.playtest`) | `playtest/android` | the game as a player meets it: Home, progression and unlocks, Petals and booster charges, milestones, demos, animations |
| **Bloomlings Tester** (`com.bloomlings.playtest.tester`) | `playtest/tester` (`PLAYTEST_TESTER`) | quick level testing: levels open straight away, ◀ ▶ move between them, every booster is free, a tap shows its result at once, no Home, progression, economy or demos |

`PlaytestFlavor` holds the difference; `playtest/Playtest.Shared.props` holds everything the two share.

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
- Two screens drawn on a canvas:
  - **Home**: Level N, Play/Continue, Petals, booster charges (or the level each booster opens at), the next milestone,
    and tester controls (◀ −1 and +1 ▶ move the progression, Reset starts a new profile). The very first launch goes
    straight into Level 1; later launches open Home, and ⌂ returns to it from a level.
  - **Level**: the board (restored cells show the finished picture), the Waiting Slots, the boosters, the Source Tray,
    and the demo, win and jam cards.
- Progression and economy are the Unity client's own engine-free services, linked from `client/` (never copied):
  the save file, the unlock roadmap (boosters open at L3, L4, L6 and L9 with a free charge; mechanics, Hard and Super
  Hard as in the spec), Petals for wins, booster charges bought with Petals, level drops and milestone rewards.
- Animation: the rules resolve a tap at once in the core; `LevelAnimator` then plays the events round by round, like
  the Unity client's timeline: a pod flies from the tray to its slot, Bloomlings walk from the Garden Entry to their
  tiles, each tile shrinks away when its Bloomling arrives, slot counts drop, a finished pod leaves, locks stay until
  their key's wave, and the win or jam card waits for the last wave. 2× speed and backlog compression change only the
  pace. A harness replays every golden case and every showcase solution, with pauses and with rapid taps, and checks
  that the settled screen equals the rules state.
- Demos once each, with the Unity client's texts (`Strings_en.csv`, embedded): the Level 1 tap hint, each booster at
  its unlock, each mechanic the first time a level uses it, a new variant, and "Match the exact symbol".
- Tester controls in a level: ⌂ Home, ♪ sound and vibration, 1×/2× speed (saved), ↻ restart.
- Variants show a two-letter code: Lf Leaf, Ms Moss, Fl Flower, Vb Violet Bud, Wa Water, Dw Dew, Wd Wood, Ac Acorn.
  A small square in a tile's corner shows the next layer's variant, 🔑 marks a key tile, 🔒 a locked pod or slot, ∞
  a connected pod, ⌛ a waiting pod and ! the last free slot.

There is no store, ads, sign-in, analytics, Wardrobe or Collection; those live in the Unity client.

## Build

- **GitHub**: Actions → **android-apk** → Run workflow, and pick `both`, `playtest` or `tester`. The APKs are the
  `bloomlings-playtest-apk` and `bloomlings-tester-apk` artifacts of the run. The workflow is manual only and keeps
  only the newest APK of each kind.
- **Locally** (needs the Android SDK and JDK 17): `dotnet workload install android`, then
  `dotnet publish playtest/android -c Release -f net10.0-android` or the same for `playtest/tester`.
- `dotnet run --project playtest/check` checks the animator and the meta layer without Android.

To refresh the preview levels after regenerating them, copy the batch files into `playtest/content/levels/`. Keep
exactly one file per level number, and keep Levels 1–10 from `content/curated/`.
