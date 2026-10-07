# The Daily Challenge pool

A year of Daily Challenge puzzles (R19, FR-064 as amended on 2026-10-07): `level-NNNNN.json` in `levels/`, with their
validation records in `validation/`. Here the level number is the pool entry, 1 to 365, not Level N. A day plays entry
`(whole UTC days since 2026-01-01 mod 365) + 1` (`DailyChallengeService.PoolIndex`). So every player gets the same
puzzle on the same day, and the year starts over after 365 days. `publish --daily content/daily` packs the pool with
the catalog. The publish gate checks each entry by itself, without the catalog's band rules or FR-083 windows, and
checks the pool's own picture rules.

Built on 2026-10-07 with:

```sh
dotnet run --project core/src/Bloomlings.Pipeline -- daily generate --seed 1 --count 365 --segments 7 --jobs 4 --out content/daily
```

## Pictures

- **Every day a new picture.** Each entry shows a picture that no other entry and no level shows.
- **Subjects.** The pictures are 128 subjects that the levels never draw, three pictures of each at 22×28. They are drawn
  by `content/pictures/tools/sketch_pictures.py --daily` from `daily_animals.py`, `daily_places.py`, `daily_things.py`
  and `daily_food.py`.
- **The `daily` theme.** These pictures carry the theme `daily`. Only the daily profile takes them, and it takes nothing
  else. `validate` refuses one in a level (`picture-pool`).
- **Spacing.** A subject comes back only after 60 entries at least (`DailyPlan.SubjectWindow`). The plan fixes every
  entry's picture before generation, and an entry whose picture makes no level takes a spare picture.

## Board and difficulty

- **Board.** Every entry is the biggest board, 22×28 (616 cells), in the icons look of a big level.
- **Difficulty by weekday** (`--class weekly`, the default). Entry 1 is Thursday 2026-01-01:

  | Weekday | Class |
  |---|---|
  | Wednesday, Saturday | Hard |
  | Sunday | Super Hard |
  | Every other day | Normal |

  That makes 209 Normal, 104 Hard and 52 Super Hard entries. From 2027 the weekdays move by one a year, as the pool
  starts over every 365 days.
- **Mechanics.** Every entry plays with the unlocks of L50, where the challenge opens: Stone, Key, Locked Pod, Connected
  Pair, Layered Tile and Gate, with one layer below a top.
- **Profile.** `content/profiles/daily.json` holds the pods, work and durations of the big levels' row. The `daily`
  thresholds are in `difficulty-thresholds.json`.

## Rebuilding

The pool depends on the seed and `--segments`, never on `--jobs`, so `--segments 7` rebuilds it byte for byte.

Do not edit these files by hand. Regenerate the pool, and bump the content version when it changes.
