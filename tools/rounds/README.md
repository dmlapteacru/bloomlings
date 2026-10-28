# Round tools

Tools of the skills `bloomlings-round-wave`, `bloomlings-round-pictures` and `bloomlings-round-levels`. The rules are in
[`product/17_LEVEL_GENERATION_RULES.md`](../../product/17_LEVEL_GENERATION_RULES.md) and the content in
[`content/rounds`](../../content/rounds/README.md). Node only: no install, PNGs are written without an image library.

```sh
# a wave (17 §13): plan → pictures → run → the owner's sample → review
node tools/rounds/rounds.mjs plan 21 70                  # mechanics breakdown (runs/…/mechanics.json), positions, supply, picture order → runs/L0021-L0070/plan.md
node tools/rounds/rounds.mjs run 21 70 --jobs 4          # dry place, build what is open, checks, place, audit, summary, sample pilot
node tools/rounds/rounds.mjs review results.json         # owner's ratings next to the metrics → content/rounds/reports/
# pictures from SVG drafts and the screen before the sheet (17 §13)
node tools/rounds/svg2cells.mjs drafts.mjs --out content/rounds/pictures/src/batch-0120.mjs --sheet /tmp/s.png --level 30
node tools/rounds/screen.mjs content/rounds/pictures/src/batch-0120.mjs --level 30
# pictures (17 §2)
node tools/rounds/pictures.mjs next
node tools/rounds/pictures.mjs verify content/rounds/pictures/src/batch-0031.mjs --out /tmp/check.png   # skill bloomlings-board-picture
node tools/rounds/pictures.mjs render content/rounds/pictures/src/batch-0031.mjs
node tools/rounds/pictures.mjs check --status draft
node tools/rounds/pictures.mjs sheet --status draft --out content/rounds/pictures/sheets/batch-0031.png
node tools/rounds/pictures.mjs approve 31 32 33          # also rebuilds content/rounds/approved/ (locked/ L1–3, pool/)
node tools/rounds/pictures.mjs publish
node tools/rounds/pictures.mjs status --free           # approved codes not yet used by a placed level
node tools/rounds/pictures.mjs motifs                   # recurring motifs (fish, robots …): «ХВАТИТ» at 3+, not drawn again
node tools/rounds/pictures.mjs reject 34 --note "too small details"
node tools/rounds/pictures.mjs replace P0034 content/rounds/pictures/src/batch-0031-fix.mjs
# levels (17 §3–§14)
node tools/rounds/levels.mjs calendar 1 60
node tools/rounds/levels.mjs build --approved --candidates 64
node tools/rounds/levels.mjs build P0012 --overlay up,left --recipes bridge,bridge+links --classes Normal --no-intro   # on the generator's wall
node tools/rounds/levels.mjs bank
node tools/rounds/levels.mjs place 1 30
node tools/rounds/levels.mjs card 15
# transfer into the game (spec 006): the owner's sample confirmation, then the export with the conformance fixtures
node tools/rounds/rounds.mjs confirm 1 20 --sample product/playtests/pilot-v12-owner-2026-10-27/results.json
node tools/rounds/export.mjs 1 20 --out content/game/rounds/export/L0001-L0020 --owner-results product/playtests/pilot-v12-owner-2026-10-27/results.json
node tools/rounds/transfer-log.mjs exported content/game/rounds/export/L0001-L0020   # then validated / published / confirmed
node tools/rounds/conformance.mjs solution content/game/rounds/export/L0001-L0020/levels/L0005.json --window 7   # the reference's log of one window
node --test tools/rounds/test/*.test.mjs
```

`ROUNDS_DIR=<folder>` points every command at another content folder, for trial runs.

**Bridges on any strip** (17 §8 "Где стоит мост", amendment of 2026-10-28). A picture's `#` strip may run in any
direction; `b` marks a bridge site on it. `boardOf(pic, overlay?)` builds the board: each `#`/`b` cell on the picture's
outer edge closes the ring cell directly outward (a full-width stream row closes both sides, a full-height wall column
the top and the bottom, an L its two edge ends), so the far part needs a bridge; each site's anchor is its one orthogonal
neighbour reachable from the entry while all sites are walls (else `BridgeAnchorError`), and its choice carries
`dir` (`up`, `left`, `right`, `down`: the crossing) and a label (`Мостик вверх слева`, `Мостик влево снизу` …). The engine
accepts an anchor above, below or beside the site in the same row. `wallOverlay(pic, side, seed)` lays the generator's
wall over an approved picture (`up`: a full-width row; `left`/`right`: a full-height column, the entry on the open side;
`two`: an L closing a top corner): 25–45 % of the colour cells behind it, two sites at least 3 apart whose near-bank
anchors are colour cells. A level built on it stores `definition.overlay` = `{walls, sites}` in picture coordinates
(x from the left, y from the top); the exporter writes it as `board.overlay` of `round-level.v1`, the audit rebuilds the
board from the approved picture plus the overlay, N and W count the board with the wall, and the pilot draws it as
stone. `bridgeSide(level)` is the crossing of the group: the common direction, or `two` for different directions or
strips.

**The wave's mechanics plan.** `content/rounds/runs/<wave>/mechanics.json` (approved by the owner, 17 §13 step 1): per
level `recipe` (lead first), `lead`, `add`, `bridgeSide` and `strip` (`new`: a picture with its own strip; `overlay`: the
generator's wall). `rounds.mjs plan` prints its breakdown and orders pictures only for what it needs; `rounds.mjs run`
builds only each open position's planned recipe (`levels.mjs build --pairs cls:recipe@side --overlay …`); `levels.mjs
place` accepts a candidate on a planned position only with that recipe and, for a bridge, that crossing side. Recipes:
`none`, `links`, `hidden`, `bridge`, `links+hidden`, `hidden+links`, `bridge+links`, `bridge+hidden`, `hidden+bridge`,
`links+bridge`, `links+bridge+hidden`, `bridge+links+hidden`; doses by §7 (the lead its range with the dose place cycle,
an additional one its minimum admissible dose, three types the lead in its lower half).

| File | What |
|---|---|
| `lib.mjs` | paths, manifest, palette, the rules' tables (stages, budgets, calendar, introductions, supported mechanics), board around a picture (overlay, ring closure, anchors, crossing side), the generator's wall `wallOverlay`, the wave's mechanics plan (`planOf`, `fitsPlan`), PNG writer |
| `themes.json` | the theme catalog's 50 families, 24 directions and meme families (from `product/design/Bloomlings-picture-theme-catalog.md`), for the tag check |
| `pictures.mjs` | sequential numbering, render, rules check, contact sheet, approve/reject/replace |
| `engine.mjs`, `engine/` | the round engine, our own code grown from the pilot: rules, timeline, palette, clock with the §1 slot positions, the §8 link rule, boards up to 48×48, 4–5 places; the pilot v10 embeds the same files |
| `measure.mjs` | saved solution (search + live replay), §5 random test with waiting and Wilson 100 → 400, hidden-pod fairness, §11 strategies, §10 rhythm |
| `rounds.mjs` | the wave runner: `plan` (positions and picture order), `run` (resumable, parallel build of what is open, checks, placement, audit, summary, the owner's sample pilot), `review` (owner results vs metrics, notes draft) |
| `screen.mjs` | the screen of pictures before the contact sheet: §2 rules, strength, quick queue test, readability, similarity to approved, motifs |
| `svg2cells.mjs`, `svg-palette.mjs` | flat SVG drafts in the palette → cell pictures sized to a target cell count, small patches folded |
| `audit.mjs` | every placed level recomputed against 17 (layer A) with the layer B notes of `content/rounds/quality.json` |
| `analysis/` | one-off measures behind the pilot reviews (opening + colours on CF models, forced decisions, slot pressure) |
| `export.mjs` | spec 006 transfer: entry checks (placed, approved pictures, the owner's confirmation, audit without НЕТ but a named exception), then `round-level.v1`, `round-picture.v1`, `round-proof.v1`, `round-conformance.v1` and `export.json`, checked against the schemas of `specs/006-round-levels-in-game/contracts/` |
| `conformance.mjs` | the reference side of the conformance gate: canonical event log, extended signature, the §5 random policy game by game, owner attempts at whole µs; engine fingerprints |
| `schema.mjs` | a dependency-free checker for the keywords the round schemas use |
| `transfer-log.mjs` | `content/game/rounds/transfers.jsonl`: exported → validated → published → confirmed |
| `levels.mjs` | §3 pod split, candidates (queue by access depth, doses of §7), build → bank, calendar placement with the §11 variety rules, cards |

The engine plays hidden pods, linked pairs and the bridge. Latch + key, canal, shutters, repair, bud, seed, well,
hidden cells and stacks need engine and pilot work first (`SUPPORTED` in `lib.mjs`). The search behind the saved
solution assumes each action's work settles before the next. A search that runs out of budget means "not checked",
never "impossible".
