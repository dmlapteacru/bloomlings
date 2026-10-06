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
| 400 | 401 | Connected triple (optional, Hard and Super Hard only) | band-0251-0500 |

A showcase is generated in showcase mode (only the new mechanic, Normal class; the triple's is Hard). A practice level
is generated in band mode: the generator gives the practice level its mechanic alone and keeps the level's scheduled
difficulty class, which is never Super Hard (spec 001 FR-059 as amended on 2026-10-06; the triple's practice is the first Hard or Super Hard level after L400, L401 under the generator's
schedule). All were regenerated on 2026-09-29 with `gen-1.2.0`, which follows the Level Band Guidelines, again on
2026-10-05 at the bigger band sizes (the owner: more, smaller cells from Level 1), and on 2026-10-06 on the regular
boards of 224–288 cells that every level from L11 needs (spec 001 FR-008 as amended on 2026-10-06; task T177).

The levels today (`gen-1.3.0` unless noted; every picture approved by the automated picture checks, FR-084 as amended):

| Level | Kind | Mechanic | Picture | Board | Class (score) | Generated |
|---|---|---|---|---|---|---|
| 11 | showcase | stone | treasure_chest_01 | 15×17 (255) | Normal (1361) | 2026-10-06, seed 1 |
| 12 | practice | stone | lollipop_02 | 14×17 (238) | Normal (1540) | 2026-10-06, seed 1 |
| 14 | practice | key | cherries_06 | 16×16 (256) | Normal (1434) | 2026-10-06, seed 1 |
| 16 | showcase | locked_pod | owl_08 | 15×18 (270) | Normal (1601) | 2026-10-06, seed 1 |
| 17 | practice | locked_pod | ferris_wheel_03 | 14×16 (224) | Normal (1781) | 2026-10-06, seed 1 |
| 18 | showcase | connected_pair | ferris_wheel_02 | 14×17 (238) | Normal (1372) | 2026-10-06, seed 1 |
| 19 | practice | connected_pair | fruit_tree_07 | 15×18 (270) | Normal (1390) | 2026-10-06, seed 1 |
| 28 | showcase | layered_tile | lighthouse_04 | 14×18 (252) | Normal (1656) | 2026-10-06, seed 1 |
| 29 | practice | layered_tile | cottage_08 | 16×17 (272) | Normal (1965) | 2026-10-06, seed 1 |
| 35 | showcase | gate | cake_04 | 15×16 (240) | Normal (1694) | 2026-10-06, seed 1 |
| 36 | practice | gate | helicopter_02 | 16×18 (288) | Normal (1723) | 2026-10-06, seed 1 |
| 60 | showcase | fountain | guitar_03 | 16×16 (256) | Normal (1780) | 2026-10-06, seed 1 |
| 61 | practice | fountain | tent_04 | 15×17 (255) | Normal (1760) | 2026-10-06, seed 1 |
| 80 | showcase | locked_slot | cactus_06 | 15×17 (255) | Normal (1825) | 2026-10-06, seed 1 |
| 81 | practice | locked_slot | tulip_bed_06 | 14×17 (238) | Normal (1741) | 2026-10-06, seed 1 |
| 90 | showcase | mystery_tile | fir_tree_01 | 16×18 (288) | Normal (1548) | 2026-10-06, seed 1 |
| 91 | practice | mystery_tile | barn_03 | 14×16 (224) | Normal (1633) | 2026-10-06, seed 1 |
| 150 | showcase | chest | butterfly_02 | 14×16 (224) | Normal (1873) | 2026-10-05, `gen-1.2.0` |
| 151 | practice | chest | rabbit_04 | 16×16 (256) | Hard (2367) | 2026-10-06, seed 1 |
| 250 | showcase | environment_2 | ladybug_03 | 14×16 (224) | Normal (1775) | 2026-10-05, `gen-1.2.0` |
| 251 | practice | environment_2 | tulip_bed_04 | 14×16 (224) | Normal (1940) | 2026-10-06, seed 1 (T175) |
| 400 | showcase | connected_triple | pear_03 | 14×16 (224) | Hard (2588) | 2026-10-06, seed 1 (T175) |
| 401 | practice | connected_triple | sailboat_03 | 14×16 (224) | Hard (2490) | 2026-10-05, `gen-1.2.0` |

The commands, without `--allow-draft` (every picture is approved), for example:

```sh
dotnet run --project core/src/Bloomlings.Pipeline -- generate --profile content/profiles/band-0011-0025.json \
  --levels 16-16 --mechanics locked_pod --class normal --seed 1 --out content/work/showcase-16
dotnet run --project core/src/Bloomlings.Pipeline -- generate --profile content/profiles/band-0011-0025.json \
  --levels 17-17 --seed 1 --out content/work/practice-17
```

Generate them in level order, each with the earlier ones kept, so the FR-083 windows see their neighbours: `generate`
keeps every level of this folder fixed (`--keep content/showcase`, the default), so band generation fills the levels
around them. To redo one level, take its two files out of `levels/` and `validation/` first (a kept level is not
generated), generate it into `content/work/`, and copy both files back. On 2026-10-06 L11–91 were redone this way in
level order, each with seed 1 and with `content/catalog/` still empty, so the history was the curated Levels 1–10 and
this folder; L151 followed with the schedule of FR-059 as amended.

**Status:** `validate --catalog content/showcase --context content/curated` passes with 0 errors; its only warnings are
the provisionally approved readability pairs (`content/readability/approved-pairs.json`, until the human sign-off).
The levels are copied into `content/catalog/` with the catalog (`content/catalog/README.md`). Still open for the
FR-084 tier of Levels 1–100: a person playtests each showcase.

**L151 (chest practice):** the old level stood on a 14×14 board (196 cells), which the band rule refuses, and was Super
Hard by the schedule. On regular boards no candidate reached the band's Super Hard minimum (3100): seeds 1–4 and 6 each
spent all 60 candidates, and the scored ones topped out at 2549 (median 2018). The owner then ruled that practice levels
are never Super Hard (2026-10-06, spec 001 FR-059 as amended): the schedule moves that Super Hard to L153, and L151 takes
L153's class, Hard. It was regenerated with `generate --profile content/profiles/band-0101-0250.json --levels 151-151
--seed 1` (Hard 2367 at candidate 2).
