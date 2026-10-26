# Round tools

Tools of the skills `bloomlings-round-pictures` and `bloomlings-round-levels`. The rules are in
[`product/17_LEVEL_GENERATION_RULES.md`](../../product/17_LEVEL_GENERATION_RULES.md) and the content in
[`content/rounds`](../../content/rounds/README.md). Node only: no install, PNGs are written without an image library.

```sh
# pictures (17 §2)
node tools/rounds/pictures.mjs next
node tools/rounds/pictures.mjs verify content/rounds/pictures/src/batch-0031.mjs --out /tmp/check.png   # skill bloomlings-board-picture
node tools/rounds/pictures.mjs render content/rounds/pictures/src/batch-0031.mjs
node tools/rounds/pictures.mjs check --status draft
node tools/rounds/pictures.mjs sheet --status draft --out content/rounds/pictures/sheets/batch-0031.png
node tools/rounds/pictures.mjs approve 31 32 33
node tools/rounds/pictures.mjs reject 34 --note "too small details"
node tools/rounds/pictures.mjs replace P0034 content/rounds/pictures/src/batch-0031-fix.mjs
# levels (17 §3–§14)
node tools/rounds/levels.mjs calendar 1 60
node tools/rounds/levels.mjs build --approved --candidates 64
node tools/rounds/levels.mjs bank
node tools/rounds/levels.mjs place 1 30
node tools/rounds/levels.mjs card 15
```

`ROUNDS_DIR=<folder>` points every command at another content folder, for trial runs.

| File | What |
|---|---|
| `lib.mjs` | paths, manifest, palette, the rules' tables (stages, budgets, calendar, introductions, supported mechanics), board around a picture, PNG writer |
| `pictures.mjs` | sequential numbering, render, rules check, contact sheet, approve/reject/replace |
| `engine.mjs` | the round engine: pilot v9 rules/timeline copied with the §8 link rule, boards up to 48×48, 4–5 places |
| `measure.mjs` | saved solution (search + live replay), §5 random test with waiting and Wilson 100 → 400, hidden-pod fairness, §11 strategies, §10 rhythm |
| `levels.mjs` | §3 pod split, candidates (queue by access depth, doses of §7), build → bank, calendar placement with the §11 variety rules, cards |

The engine plays hidden pods, linked pairs and the bridge. Latch + key, canal, shutters, repair, bud, seed, well,
hidden cells and stacks need engine and pilot work first (`SUPPORTED` in `lib.mjs`). The search behind the saved
solution assumes each action's work settles before the next. A search that runs out of budget means "not checked",
never "impossible".
