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

### C# details (`core/src/Bloomlings.Pipeline/Commands/RoundCommands.cs`, `Rounds/`)

- Every command takes `--rounds <dir>` (default `content/game/rounds`) and `--json` (report on stdout). Exit codes:
  0 success, 1 validation errors, 2 usage or I/O.
- `import` checks the export together with the rounds already in the folder (the export's files replace those of the same
  name): schemas, definition rules, picture ↔ board, proofs, fixture structure, fingerprints (also against
  `export.json`'s `engine` when present). The replays and the palette are left to `validate`. Nothing is copied on an
  error; otherwise the files are copied byte for byte.
- `validate --readability-report <file>` (default `<rounds>/readability-report.json`) writes every palette pair under
  normal vision and the three simulated deficiencies; only normal vision is a gate. Proof completeness also checks the
  random test against `measure.mjs` (share and Wilson interval rounded to 0.001, 400 runs when 100 cross a border,
  `classOfShare` = the level's `class` unless the level's exception names the rule `class matches the random test`) and
  the class checks (`pressure` missing is a warning). Levels must be numbered 1..N without gaps.
- The definition rules (T008) and the conformance replay (T032–T035) are plugged in through `RoundSeams`
  (`IRoundDefinitionRules`, `IRoundConformance`); until then their stand-ins fail every level.
- `diff --from <git-ref|published-dir|rounds-dir> --to <rounds-dir|published-dir> [--git-folder <dir>]`: a git ref is
  read from the rounds folder (`--to` when it is one). A picture's approval record is not part of the comparison.
- `publish --out <dir> [--content-version N]`: without N, the version in `<dir>` is kept when the packs are the same
  bytes and raised by one otherwise (1 for a new folder). Layout:
  - `manifest.json` (`round-content-manifest.v1`, `round-content-manifest.schema.json`);
  - `levels/round_levels_0001_0250.jsonl.gz`, … (gzip JSON Lines, one canonical `round-level.v1` per line, 250
    consecutive levels a pack);
  - `pictures/round_pictures.jsonl.gz` (the `round-picture.v1` documents the levels use, by id and version).

  Proofs and fixtures are not published. Round packs no longer listed are removed.

## Transfer log

`content/game/rounds/transfers.jsonl`, one entry per wave (data model). The skill appends and updates it:
`exported → validated → published (apk) → confirmed`. `rounds.mjs confirm <from> <to> --sample <results.json>` records
the owner's confirmation of the sample played in the game.
