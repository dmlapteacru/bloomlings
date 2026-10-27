# Round levels in the game (spec 006)

The game's round content, transferred from `content/rounds/` wave by wave (`tools/rounds/export.mjs`, then the
pipeline's `rounds import`). Formats: [`specs/006-round-levels-in-game/contracts/`](../../../specs/006-round-levels-in-game/contracts/).

| Folder or file | What |
|---|---|
| `levels/L####.json` | `round-level.v1`: the definition the game plays |
| `pictures/P####.json` | `round-picture.v1`: approved pictures |
| `proofs/L####.json` | `round-proof.v1`: saved solution, random test, class checks, audit, engine fingerprints |
| `conformance/L####.json` | `round-conformance.v1`: reference fixtures from the JS round engine. The C# rules must replay them exactly. Never edit by hand; re-export instead |
| `export/L####-L####/` | the exporter's output for a wave, before `rounds import` |
| `published/` | `rounds publish` output: manifest and packs for the clients |
| `readability-report.json` | `rounds validate`: the palette's CIEDE2000 per pair under normal vision (the gate) and simulated colour vision deficiencies (reported only) |
| `transfers.jsonl` | one entry per transferred wave: exported → validated → published → confirmed |
