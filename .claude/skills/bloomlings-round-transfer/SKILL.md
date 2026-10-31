---
name: "bloomlings-round-transfer"
description: "Transfer a confirmed wave of Bloomlings round levels into the game: entry checks, export from the round tools with reference conformance fixtures, import, validate (rules replayed in C# against the reference), diff, publish, the transfer log, the playtest APK and the owner's phone check. Use when the owner confirmed a wave's sample and asks to move it into the game, or asks to transfer, publish or ship levels N–M."
argument-hint: "The wave range (e.g. 'L21–L70') and the owner's sample results JSON"
user-invocable: true
disable-model-invocation: false
---

# Round transfer

The path of spec 006 (`specs/006-round-levels-in-game/`, contracts `pipeline-rounds-cli.md` and
`conformance-fixture.md`), first run on L1–L20 on 2026-10-27/28. The JS round engine (`tools/rounds/engine/`) is the
reference; the game plays `Bloomlings.Core.Rounds`, proven equal to it on every transferred level. Everything below is a
script: your part is to run it in order, read the reports, and talk to the owner.

## Before you start

- The wave was run by `bloomlings-round-wave`: every position placed, the audit clean (or a named owner exception),
  pictures approved, the owner's sample played.
- Read `content/rounds/owner-notes.md` and the wave's review report.
- The .NET 10 SDK (`core/global.json`) must be installed. In a cloud container without it: `apt-get install
  dotnet-sdk-10.0` (Microsoft's download host may be blocked; the Ubuntu archive package works).

## Steps

1. **Owner's confirmation** of the sample (what they played and said):
   `node tools/rounds/rounds.mjs confirm <from> <to> --sample product/playtests/<pilot>/results.json`
   writes `content/rounds/runs/L####-L####/confirmed.json`. Never write it without the owner's word.
   - When the owner tells you to move the wave into the game before playing its sample (L21–L50, 2026-10-31:
     «Интегрируй 21-50 в игру»), record their words as said:
     `node tools/rounds/rounds.mjs confirm <from> <to> --owner-word "<their words>"` (no sample; the phone check is then
     the first play, say so in the APK message). Never paraphrase or invent the words.
2. **Export** (about 4 minutes for 20 levels; more for 50, run it in the background):
   `node tools/rounds/export.mjs <from> <to> --out content/game/rounds/export/L####-L#### --owner-results
   product/playtests/<pilot>/results.json` (repeat `--owner-results` for every results file of the wave).
   - It refuses, naming the reason, when a position is open, the audit has a НЕТ without an exception, a picture is
     not approved or is a franchise reference, or the confirmation is missing. Fix the cause in the wave, never in
     the export.
   - An owner exception (like L18's bridge, 2026-10-27) must be in `OWNER_EXCEPTIONS` of `export.mjs` with the rule
     it waives, the reason, `decidedBy: "owner"` and the date, or (from wave L21–L50) as the object in the level file's
     `exception` field (L30's calm rule, 2026-10-31); its rule must be one of `RULE_PATTERNS`. Add one only when the
     owner decided it.
   - A bridge level's class test plays its best bridge only (17 §5, 2026-10-30): the proof's `randomTest.bridge` and
     the fixture's `random.bridge` carry it, and the replay builds no other bridge.
   - The report line per level shows the commands, the random wins against the level's record and the owner
     attempts; every one must match. A changed owner outcome is a fixture error: report it to the owner.
   - Then `node tools/rounds/transfer-log.mjs exported content/game/rounds/export/L####-L####`.
3. **Import and validate** (C#):
   - `dotnet build core/Bloomlings.sln`
   - `dotnet run --no-build --project core/src/Bloomlings.Pipeline -- rounds import content/game/rounds/export/L####-L####`
   - `dotnet run --no-build --project core/src/Bloomlings.Pipeline -- rounds validate` (about 1 minute per 20
     levels): schemas, definition and placement rules, picture ↔ board, proof completeness, the palette's
     readability, and the conformance replay of every solution window, every random game and every owner attempt.
     It must end with 0 errors.
   - `node tools/rounds/transfer-log.mjs validated <from> <to> --note "…"`.
4. **Diff** against the last published content:
   `dotnet run --no-build --project core/src/Bloomlings.Pipeline -- rounds diff --from content/game/rounds/published
   --to content/game/rounds`. A changed published level needs a higher `definitionVersion`, a changed picture a
   higher `version`; a removed published level is an error.
5. **Publish**:
   `dotnet run --no-build --project core/src/Bloomlings.Pipeline -- rounds publish --out content/game/rounds/published`
   (refuses on any validate error; the content version goes up when the packs change).
6. **Tests**, then commit `content/game/rounds/` (export, levels, pictures, proofs, conformance, published,
   readability report, transfers.jsonl) and push:
   - `dotnet test core/Bloomlings.sln` (compare with `specs/006-round-levels-in-game/checklists/baseline.md`),
   - `dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj` (every round level wins through the
     client's flow),
   - `dotnet run --project playtest/check`.
7. **APK**: run the manual workflow `android-apk.yml` (Actions → Run workflow, the full playtest) on the branch, or
   build locally. Never add a push, pull_request or schedule trigger. The build packs `content/game/rounds/published/`
   and fails clearly without it.
8. **Owner's phone check**: send the APK link with what to check (quickstart §5 of spec 006: the wave's new mechanics,
   its Hard and Super Hard levels, the sample levels, "new levels soon" after the last level). Then
   `node tools/rounds/transfer-log.mjs published <from> <to> --content-version <v> --apk <link>`.
9. **Confirmation**: when the owner says the wave plays right on the phone,
   `node tools/rounds/transfer-log.mjs confirmed <from> <to>`, commit and push. Their notes go to
   `content/rounds/owner-notes.md`.

## On a conformance failure

`rounds validate` names the level and the first difference (`L0005 solution window 7 (tap pod 11 at 26962200 µs):
events differ`, with the C# canonical log of that window). Compare it with the reference:
`node tools/rounds/conformance.mjs solution content/game/rounds/levels/L0005.json --window 7` (or `random <level>
<seed>`). The C# port must change to match the reference, with a rule test for the case; or, when the reference engine
changed on purpose, re-export the wave from it. Never edit a fixture, proof or level file by hand, and never relax a
check to pass.

## Do not

- Do not transfer a wave the owner has not confirmed, and do not record `confirmed` without their word on the phone.
- Do not change the reference engine files during a transfer (they are fingerprinted in every level).
- Do not commit a published set that `rounds validate` did not pass.
