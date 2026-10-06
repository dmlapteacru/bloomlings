# tools/catalog: build the level catalog

Generates and certifies `content/catalog/` (Levels 1–5000, spec 001 T153) band by band with the content pipeline
(`specs/001-core-game-mvp/contracts/pipeline-cli.md`). Two scripts run the same procedure:

- `build-catalog.ps1` for Windows PowerShell 5.1 and PowerShell 7;
- `build-catalog.sh` for bash (Linux, macOS, Git Bash).

Both need the .NET 10 SDK (`core/global.json`) and git. They never commit or push; at the end they say what to commit.

```powershell
# Windows, from the repository root
powershell -ExecutionPolicy Bypass -File tools\catalog\build-catalog.ps1            # every band, -Jobs = logical cores - 2
powershell -ExecutionPolicy Bypass -File tools\catalog\build-catalog.ps1 -ToLevel 1000
powershell -ExecutionPolicy Bypass -File tools\catalog\build-catalog.ps1 -Check 0026-0050
```

```sh
tools/catalog/build-catalog.sh                 # every band
tools/catalog/build-catalog.sh --jobs 3 --to-level 50
tools/catalog/build-catalog.sh --check 0026-0050
```

| PowerShell | bash | Default | Meaning |
|---|---|---|---|
| `-Jobs N` | `--jobs N` | logical cores − 2 | Threads for `generate`. The levels never depend on it |
| `-ToLevel N` | `--to-level N` | 5000 | Stop after the bands that start at or before level N |
| `-Retries N` | `--retries N` | 2 | Seeds tried after the band's own seed for a level that gets no accepted candidate |
| `-Work DIR` | `--work DIR` | `content/work/catalog-build` | Work folder (gitignored): the pipeline build, the batches, the logs |
| `-NoBuild` | `--no-build` | build | Reuse the pipeline build in the work folder |
| `-NoFinal` | `--no-final` | run | Skip the whole-catalog `validate` and `score` at the end |
| `-Check BAND` | `--check BAND` | | Regenerate a finished band and compare it with `content/catalog` file by file |

## The bands

Each band uses its profile, seed 1 and a fixed number of segments. `generate --segments` cuts the range into that many
contiguous parts that run in parallel on `--jobs` threads (a segment holds at least 50 levels). The levels depend on
the segments and never on the threads, so any machine gives the same catalog. A machine with fewer threads than
segments is only slower.

| Band | Levels | Profile | Seed | Segments |
|---|---|---|---|---|
| 0011-0025 | 11–25 | `content/profiles/band-0011-0025.json` | 1 | 1 |
| 0026-0050 | 26–50 | `band-0026-0050.json` | 1 | 1 |
| 0051-0100 | 51–100 | `band-0051-0100.json` | 1 | 1 |
| 0101-0250 | 101–250 | `band-0101-0250.json` | 1 | 3 |
| 0251-0500 | 251–500 | `band-0251-0500.json` | 1 | 5 |
| 0501-1000 | 501–1000 | `band-0501-1000.json` | 1 | 10 |
| 1001-2000 | 1001–2000 | `band-1001-2000.json` | 1 | 14 |
| 2001-5000 | 2001–5000 | `band-2001-5000.json` | 1 | 14 |

## The procedure

1. **Build** the pipeline once: `dotnet build core/src/Bloomlings.Pipeline -c Release -o <work>/pipe`. Every later
   step runs `dotnet <work>/pipe/bloomlings-pipeline.dll`.
2. **Levels 1–10** are the curated levels: `content/curated/level-000N.json` copied as `content/catalog/levels/level-0000N.json`
   (the same bytes), with validation records from `validate --defs <copy> --write-records`.
3. **Each band, in level order:**
   1. `generate --profile <profile> --levels <first>-<last> --seed 1 --segments <segments> --jobs <jobs>
      --catalog content/catalog --keep content/showcase --history <previous band> --out <work>/<band>/gen`.
      `content/catalog` then holds exactly the earlier bands, so every FR-083 window sees its earlier neighbours.
      `--history` names the previous band's batch when the work folder still has it; it holds the same levels as the
      catalog, so the result is the same with or without it. The showcase and practice levels in `content/showcase/`
      are kept as they are, and `generate` fills the levels around them.
   2. A level that gets no accepted candidate (most often a Super Hard one) is generated again with seeds 2 and 3
      (`-Retries`), with the band's other levels as `--history`, so it is judged against its neighbours on both sides.
      When every seed fails, the level stays a **gap**: it is recorded, and the band goes on.
   3. The band's generated levels, its fills and its showcase levels are assembled in `<work>/<band>/assembled` and
      validated against the catalog so far: `validate --defs <assembled> --context content/catalog`. On any error the
      script stops and copies nothing. Warnings (today the provisionally approved readability pairs) are counted.
   4. The band is copied into `content/catalog/levels/` and `content/catalog/validation/`, and one line goes into
      `content/catalog/build-manifest.jsonl`: the band, its profile, seed, segments, retries, the jobs used, the
      levels generated and kept, the seam repairs, the fills (`"level:seed"`), the gaps, the seconds, the validation
      counts and the pipeline commit (`+changes` when core, profiles, pictures, showcases, curated levels or readability
      pairs had uncommitted changes).
4. **The whole catalog** at the end: `validate --catalog content/catalog --context content/curated` (the release gate's
   checks, SC-004) and `score --defs content/catalog --curated content/curated` (FR-059's counts per 100 levels and
   the picture statistics of SC-012, in `<work>/score.txt`). The summary is in `<work>/summary.txt`.

**Resuming.** A band listed in the manifest is skipped, so a stopped run continues with the first unfinished band.
A partial copy of that band in the catalog is removed first; a level past it stops the script, since it would change
the band's history. To redo a band, delete its manifest line and its levels and every later band's.

**Gaps.** A gap needs a fill before `publish` (which wants Levels 1..N without gaps). Generate it with another seed,
with the whole catalog as its neighbours, validate it in context, and copy it in:

```sh
dotnet <work>/pipe/bloomlings-pipeline.dll generate --profile content/profiles/<band>.json --levels N-N --seed 4 --out content/work/fill-N
dotnet <work>/pipe/bloomlings-pipeline.dll validate --defs content/work/fill-N --context content/catalog
```

Then copy its `levels/` and `validation/` files into `content/catalog/` and note the fill in the band's manifest line.

## Same levels on every machine

Core, Solver and Generator use no floating point and never iterate in hash order (research R3), and `.gitattributes`
keeps LF line endings on every OS, so the profiles, pictures and showcase files hash the same on Windows. A Windows
build therefore writes byte-identical levels and validation records. To check it, regenerate a band that is already
in the catalog and compare every file:

```powershell
powershell -ExecutionPolicy Bypass -File tools\catalog\build-catalog.ps1 -Check 0026-0050 -Work content/work/catalog-check
```

`-Check` builds the band in `<work>/check-<band>/` with only the catalog's earlier levels as history, with the same
seed, segments and retries, and reports every file that differs. It writes nothing into `content/catalog`. "0 differ"
means the machine reproduces the catalog.

## Time and disk

Measured on the 4-core cloud machine (one core per segment): a Normal or Hard level takes about 20–40 s, a Super Hard
one 10–20 min when most of its 60 candidates fail the tray tuner (and up to three seeds when the first two fail), a big
level about 25 s, and `validate` about 0.5 s per level. That is about 100 s per level on average, about 145
core-hours for Levels 11–5000, before the engine speed-up of the search. With 14 threads, a band of 14 segments runs
about 14 times faster, plus the seam repairs, which run one after another at the end of the band. A level takes about
9 KB on disk (definition and validation record), about 45 MB for 5000 levels.

## What to commit

`content/catalog/` (levels, validation records and `build-manifest.jsonl`). The work folder stays out of git.
