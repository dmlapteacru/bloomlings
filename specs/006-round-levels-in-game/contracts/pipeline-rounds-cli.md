# Contract: Round Transfer Commands

Two sides:
- **JS**, next to the reference engine and the bank: the exporter.
- **C#**, the game's content pipeline: import, validate, publish, diff.

The skill `bloomlings-round-transfer` (written after the first transfer, FR-054) runs them in this order.

## JS exporter

```sh
node tools/rounds/export.mjs <from> <to> --out content/game/rounds/export/L####-L####
     [--owner-results product/playtests/<pilot>/results.json]...
```

**Entry checks** (FR-050). Each failure is named and the exporter exits 1:
- every position from–to is placed (`content/rounds/levels/`);
- `audit.mjs from to` has no НЕТ, except a level whose `exception` names the failing rule;
- every picture is `approved` and not a franchise reference;
- `content/rounds/runs/<wave>/confirmed.json` exists (the owner's sample confirmation, written by `rounds.mjs
  confirm`).

**Writes**:
- `levels/L####.json` (`round-level.v1`);
- `pictures/P####.json` (`round-picture.v1`);
- `proofs/L####.json` (`round-proof.v1`);
- `conformance/L####.json` (`round-conformance.v1`), produced by running the reference engine;
- `export.json`: the wave, the engine fingerprints and the counts.

## C# pipeline

```sh
dotnet run --project core/src/Bloomlings.Pipeline -- rounds import <export-dir>   # validate, then copy into content/game/rounds/
dotnet run --project core/src/Bloomlings.Pipeline -- rounds validate [--rounds content/game/rounds]
dotnet run --project core/src/Bloomlings.Pipeline -- rounds diff --from <git-ref|published-dir> --to content/game/rounds
dotnet run --project core/src/Bloomlings.Pipeline -- rounds publish --out <dir>   # manifest + packs for the clients
```

**`validate` checks**:
- schemas;
- definition rules (data model);
- picture ↔ board match;
- proof completeness;
- the palette's normal-vision readability (FR-047);
- conformance (fixture replay);
- every level's engine fingerprints equal the fixture set's.

Exceptions are accepted only for their named rule.

**`diff`**: a changed published level must carry a higher `definitionVersion` (FR-052), and a changed picture a higher
`version`.

**`publish`**: refuses on any `validate` error. It then writes `round-content-manifest.v1` (content version, levels,
pictures, SHA-256 of each pack) and the packs read by the playtest and the client.

## Transfer log

`content/game/rounds/transfers.jsonl`, one entry per wave (data model). The skill appends and updates it:
`exported → validated → published (apk) → confirmed`. `rounds.mjs confirm <from> <to> --sample <results.json>` records
the owner's confirmation of the sample played in the game.
