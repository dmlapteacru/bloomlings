# tools/catalog: build the level catalog

Generates and certifies the launch range (Levels 1–2500 by default) in `content/catalog/`, band by band with the content pipeline
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
| `-ToLevel N` | `--to-level N` | `content/launch.props` (2500) | Exact last level, including inside a band |
| `-Plan` | `--plan` | off | Print the selected ranges without generation, build or writes |
| `-Retries N` | `--retries N` | 2 | Seeds tried after the band's own seed for a level that gets no accepted candidate |
| `-Work DIR` | `--work DIR` | `content/work/catalog-build` | Work folder (gitignored): the pipeline build, the batches, the logs |
| `-NoBuild` | `--no-build` | build | Reuse the pipeline build in the work folder |
| `-NoFinal` | `--no-final` | run | Skip the whole-catalog `validate` and `score` at the end |
| `-Check BAND` | `--check BAND` | | Regenerate a finished band and compare it with `content/catalog` file by file |

## The bands

Each band uses its profile, seed 1 and a fixed number of segments. `generate --segments` cuts the range into that many
contiguous parts that run in parallel on `--jobs` threads (a segment holds at least 50 levels). The levels depend on
the segments and never on the threads, so any machine gives the same catalog. A machine with fewer threads than
segments is only slower. The segments are generated blind to each other, so at each seam the later segment's first
levels that break a repetition rule with the levels before them (the same picture or Source layout within 50 levels,
three in a row) are generated again between their neighbours. Only the later level of such a pair is redone, and the
earlier segment keeps its levels. Last, every level is judged against all the levels before it for a picture used
again far away with the same look (mirroring and mapping) or the same Source design (FR-083; L437 repeated L326 in the
owner's run of 2026-10-07, two segments apart), and the later one is generated again.

| Band | Levels | Profile | Seed | Segments |
|---|---|---|---|---|
| 0011-0025 | 11–25 | `content/profiles/band-0011-0025.json` | 1 | 1 |
| 0026-0050 | 26–50 | `band-0026-0050.json` | 1 | 1 |
| 0051-0100 | 51–100 | `band-0051-0100.json` | 1 | 1 |
| 0101-0250 | 101–250 | `band-0101-0250.json` | 1 | 3 |
| 0251-0500 | 251–500 | `band-0251-0500.json` | 1 | 5 |
| 0501-1000 | 501–1000 | `band-0501-1000.json` | 1 | 10 |
| 1001-2000 | 1001–2000 | `band-1001-2000.json` | 1 | 14 |
| 2001-2500 | 2001–2500 (launch ends here) | `band-2001-5000.json` | 1 | 10 |
| 2501-3000 | 2501–3000 (future update) | `band-2001-5000.json` | 1 | 10 |
| 3001-4000 | 3001–4000 (future update) | `band-2001-5000.json` | 1 | 14 |
| 4001-5000 | 4001–5000 (future update) | `band-2001-5000.json` | 1 | 14 |

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
4. **The selected launch range** at the end (copied to `<work>/launch-view`; later source levels are left intact): `validate --catalog content/catalog --context content/curated` (the release gate's
   checks, SC-004) and `score --defs content/catalog --curated content/curated` (FR-059's counts per 100 levels and
   the picture statistics of SC-012, in `<work>/score.txt`). The summary is in `<work>/summary.txt`.

**Resuming.** A completed range covered by the manifest is skipped only when every requested definition and validation record exists. This accepts the first 2500 of the historical 5000-level catalog without regenerating it. A stopped run continues with the first unfinished band.
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
about 14 times faster, plus the seam repairs (a few levels per seam), which run one after another at the end of
the band. A level takes about
9 KB on disk (definition and validation record), about 45 MB for 5000 levels.

## What to commit

`content/catalog/` (levels, validation records and `build-manifest.jsonl`). The work folder stays out of git.

## Launch boundary and later updates (2026-10-15)

`content/launch.props` is the shared source of truth: **2500 sequential launch levels**. Both scripts read it; the
Android and desktop-preview builds include only its production range, and pipeline `publish` defaults to the same
limit. The existing L2501–5000 definitions remain in the source catalog as reserve and are not launch content.
Developer-only prototype levels remain separate. The complete picture source library stays available for local work.

```powershell
powershell -ExecutionPolicy Bypass -File tools\catalog\build-catalog.ps1 -Plan
powershell -ExecutionPolicy Bypass -File tools\catalog\build-catalog.ps1 -Jobs 8
```

```sh
bash tools/catalog/build-catalog.sh --plan
bash tools/catalog/build-catalog.sh --jobs 8
```

The default final certification checks `<work>/launch-view`, L1–2500, rather than the reserved later levels. `score`
failures now fail the script too. Generation and solver work stay on the owner's computer; no artwork service or GPU
is required. Committed, curated picture grids are the generator's inputs. See [the new picture direction](../../content/pictures/art-direction.md).

`publish --catalog content/catalog --content-version N --out build/content` requires a complete 1–2500 range and
publishes only referenced pictures (plus explicitly requested Daily content). For a smaller review pack, explicitly
pass `--max-level 100`; use `--launch-config <file.props>` to select another reviewed scope. Daily count is independent.

For a future content update, deliberately raise `LaunchLevelCount`, generate/certify the added range locally and publish
another content version. The canonical split at 2500 prevents an expansion to 3000 from rebuilding the already shipped
2001–2500 band. Saved level IDs and existing definitions stay stable. The historical 2001–3000 recipe used 14 segments;
a fresh split build uses 10 per half, so its generated bytes can differ from that historical batch. Never overwrite
already published definitions without the required versioning and review. Keeping the old source catalog does not
redesign its pictures; the new art library is a separate production step.

## Checks for the launch boundary

`python3 tools/catalog/check-scope.py --powershell pwsh` compares Bash and PowerShell plans at 50, 2401, 2500 and
5000, then exercises resume and final certification with a fake pipeline. It never generates or replaces real levels.
The test verifies preservation of reserved definitions, bounds of the validation view, and failure propagation from
`score`. Without PowerShell installed it tests Bash only. This optional development check needs Python and Bash;
normal Windows catalog generation still needs only PowerShell, .NET 10 and Git.

Pipeline tests additionally publish a real small fixture through the solver gate, decode its output packs, verify
that unreferenced pictures/future definitions are excluded, and reject incomplete requested ranges.

## Unique story-art pilot (2026-10-15)

The replacement launch art requires **2500 unique compositions, used once each**. The existing catalog and normal
resume scripts still implement legacy repetition rules; running them does not turn the old art into 2500 unique scenes.
The first three independently drawn grids and solved examples live outside the production library in
[`content/pictures/pilot`](../../content/pictures/pilot/README.md). To reproduce this small pilot on your own computer:

```powershell
./tools/catalog/build-art-pilot.ps1 -Render
```

It imports, generates, solves, validates and scores into `content/work/art-pilot-local/` only; no APK is built.
`check-compositions.py` audits registered picture identities and structural copies. Its `--complete` mode additionally
requires full launch coverage and visual approvals; the three-scene pilot intentionally cannot pass that release check.
The audit is a separate command until the replacement library and catalog are ready to connect to publication.
