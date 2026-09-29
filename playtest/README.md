# Temporary playtest client (Android, no Unity)

A small .NET for Android app that plays the real levels on the shared deterministic core (`core/src/Bloomlings.Core`,
`core/src/Bloomlings.Content`). It exists so a playable APK can be built on a stock GitHub runner with **no secrets**:
the .NET Android workload and the runner's Android SDK are free, while the Unity client (`client/`) needs a Unity
licence. The Unity client stays the product client (doc 15). This one is for playtesting the rules and levels until
the Unity build runs.

## What it has

- Levels 1–94 in `playtest/content/levels/`:
  - the curated Levels 1–10;
  - the mechanic showcases;
  - the Levels 11–94 preview generated with `generate --allow-draft`.

  Past L94 the levels repeat. Draft pictures are used as in-memory previews, as `publish --allow-draft` does.
- One screen drawn on a canvas:
  - the board, where restored cells show the finished picture's colors;
  - the Waiting Slots, the Source Tray and free boosters (+Slot, Shuffle, Return, Burst);
  - the win and jam overlays.
  Taps apply at once, exactly as the core resolves them; there are no walker animations.
- Tester controls: ◀ ▶ skip levels, ↻ restarts. Progress is kept on the device.
- Variants show a two-letter code: Lf Leaf, Ms Moss, Fl Flower, Vb Violet Bud, Wa Water, Dw Dew, Wd Wood, Ac Acorn.
  A small square in a tile's corner shows the next layer's variant, 🔑 marks a key tile, 🔒 a locked pod or slot, and ∞
  a connected pod.

There is no store, ads, meta, sign-in or analytics.

## Build

- **GitHub**: Actions → **android-apk** → Run workflow. The APK is the `bloomlings-playtest-apk` artifact of the run.
  The workflow is manual only and keeps only the newest APK.
- **Locally** (needs the Android SDK and JDK 17):
  `dotnet workload install android`, then `dotnet publish playtest/android -c Release -f net10.0-android`.

To refresh the preview levels after regenerating them, copy the batch files into `playtest/content/levels/`. Keep
exactly one file per level number, and keep Levels 1–10 from `content/curated/`.
