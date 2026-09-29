# Showcase and practice levels (T111)

Each mechanic is introduced at its roadmap level with a clean showcase level, and the next level practises it alone
(FR-031: showcase → practice → combination). The Key preview is curated Level 8 (`content/curated/level-0008.json`);
the others live here:

| Showcase | Practice | Mechanic | Band profile |
|---|---|---|---|
| 11 | 12 | Stones | band-0011-0025 |
| (8, curated) | 14 | Key (the roadmap's key practice row) | band-0011-0025 |
| 16 | 17 | Locked pod | band-0011-0025 |
| 18 | 19 | Connected pair | band-0011-0025 |
| 28 | 29 | Layered tile | band-0026-0050 |
| 35 | 36 | Garden Gate | band-0026-0050 |
| 60 | 61 | Fountain | band-0051-0100 |
| 80 | 81 | Locked slot | band-0051-0100 |
| 90 | 91 | Mystery tile (optional) | band-0051-0100 |
| 150 | 151 | Chest (optional) | band-0101-0250 |
| 250 | 251 | Statue or Bridge, `environment_2` (optional) | band-0101-0250, band-0251-0500 |
| 400 | 401 (being regenerated) | Connected triple (optional, Hard and Super Hard only) | band-0251-0500 |

A showcase is generated in showcase mode (only the new mechanic, Normal class; the triple's is Hard). A practice level
is generated in band mode: the generator gives the practice level its mechanic alone and keeps the level's scheduled
difficulty class (the triple's practice is the first Hard or Super Hard level after L400, L401 under the generator's
schedule). All were regenerated on 2026-09-29 with `gen-1.2.0`, which follows the Level Band Guidelines, for example:

```sh
dotnet run --project core/src/Bloomlings.Pipeline -- generate --profile content/profiles/band-0011-0025.json \
  --levels 16-16 --mechanics locked_pod --class normal --seed 1 --out content/work/showcase-16 --allow-draft
dotnet run --project core/src/Bloomlings.Pipeline -- generate --profile content/profiles/band-0011-0025.json \
  --levels 17-17 --seed 1 --out content/work/practice-17 --allow-draft
```

Generate them in level order, each with the earlier ones kept (`--keep`), so the FR-083 windows see their
neighbours. `generate` keeps these level numbers fixed (`--keep content/showcase`, the default), so band generation
fills the levels around them.

**Status: drafts.** They use draft pictures, so `validate --catalog content/showcase --context content/curated` fails
them on `picture-approved` only. Before they move into `content/catalog/`, a person approves their pictures (FR-084),
signs off the readability pairs, and playtests each showcase (the FR-084 tier for Levels 1–100).
