# Contract: Content Pipeline CLI (`bloomlings-pipeline`)

The content team and CI use this CLI to run the pipeline
`Picture library → Generation → Validation → Scoring → Review → Publish` (doc 15 §6, spec FR-075 to FR-084, research
R7–R9). It is a .NET 10 tool in `core/src/Bloomlings.Pipeline`, run with
`dotnet run --project core/src/Bloomlings.Pipeline -- <command>`.

## Conventions

- **Exit codes**: `0` success, `1` validation failures found, `2` usage or I/O error.
- **Output**: every command prints a human summary. With `--json` it writes a machine-readable report to stdout.
- **Determinism**: every command is deterministic for its inputs. Seeds and node budgets are always explicit or taken
  from the profile.

## Commands

| Command | Purpose | Key options | Output |
|---|---|---|---|
| `pictures import` | Convert indexed PNGs and sidecars into `base-picture.v1` JSON, and compute `structure` | `--src content/pictures/src --out content/pictures/lib` | Picture JSON files, import report |
| `pictures validate` | Check pictures against the schema: size limits, role/color-group consistency, licence present, review status | `--lib content/pictures/lib` | Report; fails on errors |
| `generate` | Generate candidates for a level range from a profile (R9) | `--profile content/profiles/<band>.json --levels 501-750 --seed <n> --out content/work/<batch>` | Definitions and validation records |
| `solve` | Solve one level or a set; produce a trace, a jam witness and metrics (R8) | `--level <n>` or `--defs <dir>`, `--node-budget <n>` | Validation records |
| `validate` | Run every FR-080 invariant, plus FR-081 (losable) and FR-083 (similarity), over a set or the whole catalog | `--catalog content/catalog` or `--defs <dir>`, `--changed-only` | Report; fails on any violation |
| `score` | Compute the difficulty score and class, and report the band distribution against FR-059 (Hard 15–25 and Super Hard 6–10 per 100 levels) | `--catalog …` | Report |
| `review` | Create the review sheet for a batch: board and finished-picture renders, metrics, flags for manual QA tiers (FR-084) | `--defs <dir> --out <dir>` | HTML/PNG review pack |
| `publish` | Assemble packs of 250 levels, the picture pack and the daily pack; compute SHA-256; write the `content-manifest.v1` | `--catalog content/catalog --content-version <n> --out build/content` | Packs and manifest |
| `replay` | Replay a command log against a level definition and print the event summary and final `StateHash` (support, doc 15 §14) | `--level <n> --content-version <v> --log <file>` | Summary |
| `diff` | Compare two content versions. Lists changed levels, which must be deliberate `definitionVersion` bumps (FR-076) | `--from <v> --to <v>` | Report; fails on unversioned changes |
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

- **Pull request**:
  - `pictures validate`
  - `validate --changed-only`
  - `diff --from <main> --to <branch>`
- **Nightly**:
  - `validate --catalog content/catalog`, the full solve (SC-004)
  - `score`
  - the similarity statistics (SC-012)
- **Release**: `publish`. Its output is committed or uploaded as the release content artifact.
