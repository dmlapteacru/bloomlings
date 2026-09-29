# Showcase levels (T111)

Each mechanic is introduced at its roadmap level with a clean showcase level (FR-031: showcase → practice →
combination). The Key preview is curated Level 8 (`content/curated/level-0008.json`); the others live here:

| Level | Mechanic | Band profile |
|---|---|---|
| 11 | Stones | band-0011-0025 |
| 14 | Key (key door) | band-0011-0025 |
| 16 | Locked pod | band-0011-0025 |
| 18 | Connected pair | band-0011-0025 |
| 28 | Layered tile | band-0026-0050 |
| 35 | Garden Gate | band-0026-0050 |
| 60 | Fountain | band-0051-0100 |
| 80 | Locked slot | band-0051-0100 |
| 90 | Mystery tile (optional) | band-0051-0100 |

They were produced with the generator in showcase mode (only the new mechanic, Normal class), and regenerated on
2026-09-29 with `gen-1.1.0`, which follows the Level Band Guidelines (5+ tile pods, 5 variants from L51), for
example:

```sh
dotnet run --project core/src/Bloomlings.Pipeline -- generate --profile content/profiles/band-0011-0025.json \
  --levels 16-16 --mechanics locked_pod --class normal --seed 1 --out content/showcase --allow-draft
```

`generate` keeps these level numbers fixed (`--keep content/showcase`, the default), so band generation fills the
levels around them.

**Status: drafts.** They use draft pictures, so `validate --defs content/showcase` fails them on `picture-approved`
only. Before they move into `content/catalog/`, a person approves their pictures (FR-084), signs off the readability
pairs, and playtests each showcase (the FR-084 tier for Levels 1–100).
