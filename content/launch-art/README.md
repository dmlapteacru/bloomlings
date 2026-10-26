# Launch art: 2500 unique compositions

**Owner amendment · 2026-10-26:** new rounds take their pictures from the skill `bloomlings-round-pictures` and their levels from `bloomlings-round-levels`, under [`product/17_LEVEL_GENERATION_RULES.md`](../../product/17_LEVEL_GENERATION_RULES.md) (`content/rounds/`). This folder's sizes, difficulty and mechanic plan describe the existing launch catalog.

The launch contains exactly 2500 different compositions. Each picture has one assigned level; scaling, mirroring,
rotation and recoloring do not create another composition. L1–10 retain their distinct curated teaching pictures and
definitions. L11–2500 replace the old catalog with definition version 2 (the ten hero-milestone revision slots use version 3). Daily Challenge remains separate.

`compositions.json` is the inventory of real, reviewed cell grids. `production-briefs.json` contains the full authoring
queue, including unfinished work. **2500 briefs do not mean 2500 finished pictures or generated puzzles.** Use the
status command for the actual inventory and blockers. The old catalog remains active until a complete local build is
installed.

The checked-in library now covers all 2500 slots and passes the complete art gate.
[readiness.json](readiness.json) records coverage and hashes; full puzzle generation is still a local step.

## Readability ceiling

The owner's production revision 3: one dominant recognizable silhouette and at most one large unusual feature. A bigger
board permits a clearer contour, not a busier illustration. Small fields show simple icons. Larger fields can show a
single hybrid object or one character with one oversized prop. Avoid miniature villages, interior scenes, shelves,
crowds, textures and details that need a caption to understand. A meaningful feature spans at least three target cells,
four on the largest fields. Remaining sources use roughly 16×18 logical pixels, 5–8 broad flat regions and a clean
margin. No fine outlines, spokes, clock ticks, leaf veins, patterns or illustrations inside another object. The already
authored detailed sources supply enough complex pictures; do not continue adding detailed scenes.

Review the actual cell grid at phone width with `BoardPainter.Picture`, rather than accepting the source illustration
alone. Drop or simplify a composition whose idea disappears in cells. Complexity of the puzzle comes from Source
choices and readable mechanics, while the drawing remains readable.

## Frozen production plan

`plan.json` and `plan.tsv` assign every L1–2500 its size, class, pace, drawing type, variant budget and mandatory mechanic.
The exact size quotas follow [the art direction](../pictures/art-direction.md). There are no compact 14×16–16×18 boards
after L300. XL fields do not follow each other; a Super Hard level is followed by a Normal relief level.

The geometry hash excludes the artwork inventory. Adding an illustration must not move any level's field or class.
`launch plan` refreshes only the inventory projection when the plan already exists. Do not delete the frozen plan to
add pictures. Vine appears in the art at L45–46 and Berry at L200–201. Mechanic introductions/practice and the cell-size
readability rules are validated independently of art quality.

## Hero milestones (owner revision)

| Level | Unique hero picture | Board | Wearable achievement badge |
|---|---|---|---|
| 100 | Sprig | 36×36 | `badge.achievement.hero_sprig` |
| 500 | Bloom | 36×36 | `badge.achievement.hero_bloom` |
| 1000 | Drop | 36×36 | `badge.achievement.hero_drop` |
| 2000 | Twig | 36×36 | `badge.achievement.hero_twig` |

`hero-milestones.json` records the four assignments and six relocations. Whole existing pictures moved with their
original dimensions; none were resized or duplicated. This deliberate revision changes only L63, 100, 101, 118, 500,
501, 1000, 1002, 1997 and 2000. Difficulty classes, all band quotas, mechanic introductions, relief levels and the first
36×36 appearance at L85 stay intact. The ten changed definitions advance to version 3. Other 2490 slots and artwork
are unchanged; valid cached recipes remain reusable. Do not delete `content/work/launch-catalog`.

Let an already running build finish before pulling this revision. Then pull the branch and rerun the same generation
command with `-Install`; fingerprints rebuild the changed slots and final validation certifies the whole catalog.
A medal unlocks from the exact hero picture collected at its milestone, then can be equipped on the profile/leaderboard.
A debug level change or a win using the previous picture does not unlock it. Old collected art remains in the save;
winning the replacement picture adds it once. `references/hero-milestones.png` is owned ImageGen source artwork;
`tools/catalog/import-hero-milestones.py` reads it into exact 36×36 grids for finished-board review.

## Generate on the owner's computer

Requires .NET SDK 10. Python, Pillow, image generation tools and cloud credentials are not required to build levels from
the checked-in canonical grids. The commands below run locally; they do not build an APK or upload anything.

From the repository root, first inspect coverage:

```powershell
pwsh ./tools/catalog/build-launch.ps1 -Status
```

When all 2500 compositions pass the art gate, build and connect the complete catalog to the game:

```powershell
pwsh ./tools/catalog/build-launch.ps1 -Jobs 2 -Install
```

On Linux/macOS:

```bash
bash tools/catalog/build-launch.sh --jobs 2 --install
```

Generation writes `content/work/launch-catalog`. Rerun the same command to resume. Certified `gen-1.6.0` levels are retained
when their complete old recipe still matches; the patch regenerates failed slots and, when needed, one optional
neighbour blocking a mandatory clean introduction. Do not delete the working catalog to apply this update.
Every accepted level is saved immediately with an atomic definition, solver certificate and recipe fingerprint. The fingerprint includes the exact
picture, palette, slot, generation version, profile, thresholds and approved color pairs. A changed recipe regenerates
that level. Missing/unreviewed art blocks the requested build before writing a catalog; it never silently reuses another
picture. Failed candidates are reported in `rejections/level-NNNNN.json`. `-Retries 2` / `--retries 2` adds seeds while
keeping the same assigned picture.

For engineering samples, use `-Levels 11-30` / `--levels 11-30` and a separate `-Out` / `--out` directory under
`content/work`. A sample cannot be installed as the full launch. Full puzzle generation stays on the owner's computer.

On Windows PowerShell without `pwsh`, the same script runs with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\catalog\build-launch.ps1 -Jobs 12 -Install
```

`Bypass` applies to this child process only. `Jobs` controls parallel level generation; checkpoints and fixed segments
keep the catalog independent of the worker count.

## Difficulty and release checks

The new generator is `gen-1.6.1`. Authored silhouettes get a deliberately constructed Source route and a replayable
premature sequence. If needed, interior-color pods are split within the existing pod-size and count limits. Cell counts,
palette and gameplay rules do not change. Every accepted layout must be winnable without boosters and losable, meet its
planned computed class and winning buffer-pressure limits, and pass accounting, unlock, mechanic and fairness checks.

`difficulty-thresholds-launch.json` measures decision pressure. Occupied buffer slots dominate; unsafe choices,
dependencies and mechanisms contribute smaller costs. Board work and pod count still bound duration but do not make a
large simple drawing Hard by themselves. Hard starts at 3000, Super Hard at 4000 on every supported size. This is a
versioned production preset requiring player playtesting, not a claim of completed balance testing. Legacy levels and
Daily retain their original calibration. Authored puzzles use class-specific winning pressure at every board
size: Normal 1–2 slots, Hard 2–3, Super Hard 3–4. Legacy late-band tense/critical targets do not apply to this preset.
Filling quotas by overriding stored classes is prohibited.

`-Install` accepts only a complete L1–2500 artifact matching the current registry. It checks hashes/certificates,
replays winning and losing traces, audits computed classes and level-sequence rules, and verifies unchanged tutorials.
It stages the replacement before promoting it. Previous catalog and picture directories are retained under
`content/work/launch-backups/<installation>/directory-0` and `directory-1`; a failed promotion restores them.

The installation keeps exactly the new launch pictures plus the pictures referenced by the unchanged Daily catalog.
Generated `installed-assets.props` connects that list to Android and preview builds. Source atlases, briefs, authoring
grids, certificates and backups are not APK assets. The game still uses its usual `content/catalog` and
`content/pictures/lib` paths. Publication can additionally enforce the production registry using `--art-plan` and
`--art-registry`.

## Authoring workflow

Source atlases live in `references/`; batch descriptions retain row-major identities and original prompts. Each panel
is a separately authored composition. `src/` holds exact-size text grids and role metadata; `library/` holds canonical
picture JSON. No source PNG is needed at runtime.

After building `core/src/Bloomlings.Pipeline` and `playtest/preview` in Release, an author can prepare a batch:

```bash
python3 tools/catalog/review-launch-art.py --batch 40 --detect-layout --use-source-shades --fit-background
```

Pillow reads source pixels into cell data; it does not edit reference images. Optional source-shade preservation uses
actual green/pink tones when an illustration lacks a playable palette. A genuine accent may receive the minimum
five-cell footprint. Bounded sampling zoom and entry clearance may fit the existing structure limits. These changes
still require visual inspection of the finished-board review. Missing colors or an unreadable idea require art work,
not bypassing the gate. Static entry checks also require enough hidden-color work; actual win/loss certificates
remain the puzzle generator's responsibility.

After inspecting that batch's finished-board sheet, stage it:

```bash
python3 tools/catalog/review-launch-art.py --batch 40 --apply
```

The staging step revalidates against the current inventory, copies only the batch's reviewed sources/pictures and
refreshes plan inventory without rerolling fields. Production review under the approved direction is recorded
separately from the owner's individual approval of the pilot. Never mark an ungenerated brief as approved artwork.
