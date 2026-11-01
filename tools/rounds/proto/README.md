# Route-first prototype (2026-11-01)

A prototype of the owner's idea with Yuri (2026-11-01): split the colours into pods, place them at random, find a
winning route, tune the queue on that route, overlay mechanics that keep the route, and accept the level with today's
checks. **Prototype only**: it does not touch the engine, the rules, `levels.mjs` or the content; it reads the approved
pictures from a private copy (`content/work/proto-route/rounds-copy`, `ROUNDS_DIR`) and writes to
`content/work/proto-route/` (gitignored). The comparison with the current builder is in [REPORT.md](REPORT.md).

```sh
cp -r content/rounds content/work/proto-route/rounds-copy          # once: the private copy
node tools/rounds/proto/route-first.mjs P0166 --cls Hard --recipe links+hidden --minutes 10 --want 1
node tools/rounds/proto/compare.mjs proto L0025 L0040              # route-first on the comparison jobs (PROTO_MINUTES, PROTO_WANT)
node tools/rounds/proto/compare.mjs old                             # the current builder on the same jobs (levels.mjs build on the copy)
node tools/rounds/proto/compare.mjs report                          # out/table.md, out/table.json, preview/L1001–L1020
node tools/gameplay-pilot-v12/build.mjs --levels 1001,1002 --preview content/work/proto-route/preview --out content/work/proto-route/pilot --name proto-route
```

| Step | What `route-first.mjs` does |
|---|---|
| 1 Base | `splitPods` (17 §3), columns by N; a random deal (accent pods in rows 2..middle, a launch with work on top, a regular Normal's holding pod); calm playouts on the idle engine find winning routes; the least canonical one is kept (not "leftmost column first", not round-robin rows) |
| 2 Tune | swaps in a column, swaps across columns at about one depth, count moves between pods of one colour (§3, ±25 %), each kept only if the route still wins (replay, ms); objective: places held without work before each launch in the first 70 % of the route, near a target steered by the measured class |
| 3 Overlays | links (pairs at one depth, the engine's rule; Hard: the pair after which "most work now" gets least far, a trap first), hidden pods (reveal a new colour, launched soon after), hidden cells (one chunk the route clears in its middle), latch (key in a picture hole the route reaches a few launches before it needs the locked pod); Hard: a last greedy-breaking pass |
| 4 Accept | 20-game quick screen, forced and (Hard) calm 40 / opening pre-screens, then `levels.mjs evaluate` and the placer's filters of the slot (`placerFilters`), and the zone of the slot |

`compare.mjs report` writes the preview levels for the pilot: odd numbers the level as built today (the placed level,
or for L54/L66 the current builder's best candidate of the run), even numbers the route-first level on the same picture.
