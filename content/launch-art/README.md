# Launch art: 2500 unique compositions

The launch contains exactly 2500 different compositions. Each picture has one assigned level; scaling, mirroring,
rotation and recoloring do not create another composition. L1–10 retain their distinct curated teaching pictures and
definitions. L11–2500 replace the old catalog with definition version 2. Daily Challenge remains separate.

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

Generation writes `content/work/launch-catalog`. Rerun the same command to resume. Every accepted level is saved
immediately with an atomic definition, solver certificate and recipe fingerprint. The fingerprint includes the exact
picture, palette, slot, generation version, profile, thresholds and approved color pairs. A changed recipe regenerates
that level. Missing/unreviewed art blocks the requested build before writing a catalog; it never silently reuses another
picture. Failed candidates are reported in `rejections/level-NNNNN.json`. `-Retries 2` / `--retries 2` adds seeds while
keeping the same assigned picture.

For engineering samples, use `-Levels 11-30` / `--levels 11-30` and a separate `-Out` / `--out` directory under
`content/work`. A sample cannot be installed as the full launch. Full puzzle generation stays on the owner's computer.

## Difficulty and release checks

The new generator is `gen-1.6.0`. Authored silhouettes get a deliberately constructed Source route and a replayable
premature sequence. If needed, interior-color pods are split within the existing pod-size and count limits. Cell counts,
palette and gameplay rules do not change. Every accepted layout must be winnable without boosters and losable, meet its
planned computed class and winning buffer-pressure limits, and pass accounting, unlock, mechanic and fairness checks.

`difficulty-thresholds-launch.json` measures decision pressure. Occupied buffer slots dominate; unsafe choices,
dependencies and mechanisms contribute smaller costs. Board work and pod count still bound duration but do not make a
large simple drawing Hard by themselves. Hard starts at 3000, Super Hard at 4000 on every supported size. This is a
versioned production preset requiring player playtesting, not a claim of completed balance testing. Legacy levels and
Daily retain their original calibration. Filling quotas by overriding stored classes is prohibited.

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
