# Quickstart: validating the character art

Commands run from the repository root. Every CI workflow is manual only (CLAUDE.md), so run the local checks before
pushing.

## Prerequisites

- The .NET 10 SDK (`core/global.json`).
- For device checks: an Android phone for the playtest APK, and Unity 6.3 LTS for the game client.

## 1. The art set is current and readable

```bash
dotnet run --project tools/artgen -- check
```

Expected: exit code 0.
- **Files:** every picture re-renders within the tolerance of [contracts/art-files.md](contracts/art-files.md).
- **Readability:** at 48 px, every pair of launch characters differs in at least 10% of alpha pixels.
- **Margins:** every picture keeps a transparent border.
- **Manifest:** `manifest.json` matches the 57 files.

After an intended art change, run `-- build`, review `-- sheet` (`tools/artgen/out/sheet.png`), and commit the
pictures with the tool change.

## 2. Kit and client scripts

```bash
dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj
```

Expected: the existing tests pass (without the removed kawaii tests), plus the new `CharacterArtTests`:
- every variant has a picture name per mood, and every family has a hero and a blank hero;
- "xN" reaches 4.5:1 on every card tint;
- the board tile tints differ pairwise;
- the character boxes stay inside the pod and tile faces;
- the new asset slots are registered, and `char.face` and `char.accent` are retired.

## 3. Core content tests (originality and manifest)

```bash
dotnet test core/Bloomlings.sln
```

Expected: all green.
- `OriginalityTests` accepts the generated folder through its notices row and manifest.
- `CharacterArt_ManifestMatchesFiles` passes.

## 4. Previews

```bash
dotnet run --project playtest/preview -- --out playtest/preview/out
dotnet run --project playtest/preview -- --inventory
```

Expected: exit code 0.
- **Gameplay frames (7–9, 12, 13, 22, 23):** pods, slots, walkers and board tiles show the 2D characters, with "xN"
  counts.
- **Meta frames (1–3, 15, 16):** they show the 3D heroes.
- **Frame 24 ("Extra: Bloomlings"):** every character in every mood.
- **Checks:** no missing picture, every drawn picture is a registered slot, no overlap, the safe area holds.
- **Inventory:** it lists the new `char.v.*`, `char.hero3d.*` slots as generated pictures.

## 5. The playtest still plays correctly

```bash
dotnet run --project playtest/check
node --test backend/tests/*.test.js
```

Expected: 0 failed animator runs, and the meta checks pass (presentation only, SC-008).

## 6. On a phone (manual)

- **Full playtest** (Actions → "Android playtest APKs" → `apks: playtest`):
  1. Level 1–12: name each pod's variant from its character at a glance; check the queued pods are asleep, and a
     stuck slot looks worried.
  2. The board shows characters on light tinted tiles. The picture is recognizable, and cleared cells show the
     finished picture.
  3. Home and the win card show the 3D heroes. A worn hat or skin shows on the Home hero.
  4. No stutter on a low-end phone (SC-006).
- **Unity client:** open `client/` in Unity 6.3 LTS and play the same path.
- **Readability with people:** run the SC-002 to SC-004 checks with players and record them in
  `specs/001-core-game-mvp/checklists/` (playtests, accessibility).
