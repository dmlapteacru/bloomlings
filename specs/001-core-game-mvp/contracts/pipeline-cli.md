# Contract: Content Pipeline CLI (`bloomlings-pipeline`)

The content team and CI use this CLI to run the pipeline
`Picture library → Generation → Validation → Scoring → Review → Publish` (doc 15 §6, spec FR-075 to FR-084, research
R7–R9). It is a .NET 10 tool in `core/src/Bloomlings.Pipeline`, run with
`dotnet run --project core/src/Bloomlings.Pipeline -- <command>`.

## Conventions

- **Exit codes**: `0` success, `1` validation failures found, `2` usage or I/O error.
- **Output**: every command prints a human summary. With `--json` it writes a machine-readable report to stdout.
- **Determinism**: every command is deterministic for its inputs. Seeds and node budgets are always explicit or taken
  from the profile. `generate` gives the same levels for the same profile, range, seed, segments and history on any
  machine and with any number of threads (`--jobs`).

## Commands

| Command | Purpose | Key options | Output |
|---|---|---|---|
| `pictures import` | Convert indexed PNGs and sidecars into `base-picture.v1` JSON, and compute `structure` | `--src content/pictures/src --out content/pictures/lib` | Picture JSON files, import report |
| `pictures validate` | Check pictures against the schema: size limits, role/color-group consistency, licence present, review status | `--lib content/pictures/lib` | Report; fails on errors |
| `generate` | Generate candidates for a level range from a profile (R9). The range is cut into `--segments` contiguous parts (at most one per 50 levels; default `--jobs`), generated in parallel on `--jobs` threads. Seam by seam, a later segment's first levels that break a repetition rule with the levels before them are generated again between their neighbours (of two levels that clash across a seam only the later one), so the earlier segment keeps its levels. Then every level is judged against all the levels before it: a picture used again beyond 50 levels with the same look (mirroring and mapping) or Source design is generated again, the later level of the pair (FR-083). The levels depend on the segments, never on the threads | `--profile content/profiles/<band>.json --levels 501-750 --seed <n> --out content/work/<batch>` (`--history <batch>…`, `--keep content/showcase`, `--jobs <n>`, `--segments <n>`) | Definitions, validation records and `rejections.json` (a tray rejection names the score it reached) |
| `readability` | Compute the variant-pair color distances (normal vision and simulated protanopia, deuteranopia, tritanopia) and grayscale contrast for FR-005 | `--out content/readability`, `--write-provisional` | `pairs-report.json` (and a provisional `approved-pairs.json`) |
| `solve` | Solve one level or a set; produce a trace, a jam witness and metrics (R8) | `--level <n>` or `--catalog <dir>` (alias `--defs`), `--node-budget <n>` | Validation records |
| `validate` | Run every FR-080 invariant, plus FR-081 (losable) and FR-083 (similarity), over a set or the whole catalog | `--catalog content/catalog` (alias `--defs <dir>`), `--context content/curated`, `--changed-only` | Report; fails on any violation |
| `score` | Compute each level's difficulty score and class from its metrics with its band's weights and thresholds (FR-082), flag levels whose stored class or score disagree, and report the band distribution against FR-059 (Hard 15–25 and Super Hard 6–10 per 100 levels) | `--catalog …` (alias `--defs`), `--profiles`, `--thresholds` | Report; fails on disagreements or FR-059 violations |
| `review` | Create the review sheet for a batch: board and finished-picture renders, metrics, flags for manual QA tiers (FR-084) | `--catalog <dir>` (alias `--defs`) `--out <dir>` | HTML/PNG review pack |
| `publish` | Validate the whole catalog again, like `validate` (the release gate; the daily pool level by level), and refuse on any error; then assemble packs of 250 levels, the picture pack and the daily pack; compute SHA-256; write the `content-manifest.v1`. `--allow-draft` (playtest builds only) tolerates unapproved pictures, nothing else | `--catalog content/catalog --content-version <n> --out build/content` (`--pairs`, `--node-budget`, `--daily`, `--allow-draft`) | Packs and manifest; exit 1 when the gate fails |
| `replay` | Replay a command log against a level definition and print the event summary and final `StateHash` (support, doc 15 §14). With `--content` the level, picture, content version and Shuffle budget come from that published content version, as on the player's device | `--level <n> --content <published dir> [--content-version <v>] --log <file>`, or `--catalog <dir>` for unpublished levels | Summary |
| `diff` | Compare two content versions. Lists changed levels, which must be deliberate `definitionVersion` bumps (FR-076), and changed pictures, which need a new picture version | `--from <published dir, catalog dir or git ref> --to <published dir or catalog dir>` | Report; fails on unversioned changes |
| `daily generate` | Build the Daily Challenge pool with the `daily` profile (R19) | `--count <n> --seed <n>` | Daily definitions |

## Repository layout used by the pipeline

```text
content/
├── pictures/src/        # indexed PNG + sidecar JSON (authoring)
├── pictures/lib/        # imported base-picture.v1 JSON (reviewed)
├── profiles/            # generation profiles per band (data-model §1.5)
├── curated/             # hand-authored levels 1–100 (level-definition.v1)
├── catalog/             # accepted definitions by level number + validation records
└── work/                # generation scratch (gitignored)
```

## CI usage

Every workflow runs by hand only for now (Actions → Run workflow), to spend no Actions minutes; the same commands run
locally before each push.

- **Pull request** (`content-validate.yml`):
  - `pictures validate`
  - `validate --changed-only`
  - `diff --from <main> --to <branch>`
- **Nightly** (`catalog-nightly.yml`, run by hand until the launch catalog exists):
  - `validate --catalog content/catalog`, the full solve (SC-004)
  - `score`
  - the similarity statistics (SC-012)
- **Release**: `publish`. Its output is committed or uploaded as the release content artifact.
