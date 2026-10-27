---
name: "bloomlings-round-wave"
description: "Run a whole wave of Bloomlings round levels (usually 50, e.g. L21–L70): plan the positions and the picture order, get pictures made and approved, build and place levels by script, audit them against rules 17, build the owner's sample pilot, and after the owner plays it review the results and log the notes. Use when the owner asks for a wave, the next levels, 'levels N–M', or sends pilot results of a wave sample."
argument-hint: "The wave range (e.g. 'L21–L70'), or a results JSON of a wave sample to review"
user-invocable: true
disable-model-invocation: false
---

# Round wave

The process of 17 §13 «Волна и контроль качества», after the owner's decisions of 2026-10-27
([`product/plans/round-generation-flow-proposal-2026-10-27.md`](../../../product/plans/round-generation-flow-proposal-2026-10-27.md)).

The wave is run by scripts. Your part:
- the picture themes and drawings;
- reading the summaries;
- fixing what fails;
- turning the owner's notes into rules.

Do not re-do by hand what a script does: no manual tables of levels, no ad-hoc analysis when `rounds.mjs` already
reports it.

Before anything, read:
- the owner's notes log [`content/rounds/owner-notes.md`](../../../content/rounds/owner-notes.md);
- the quality bounds `content/rounds/quality.json`.

## Steps

1. **Plan.**
   - Run `node tools/rounds/rounds.mjs plan <from> <to>`. It writes `content/rounds/runs/L####-L####/plan.md`.
   - Tell the owner which positions the engine cannot fill yet (an introduction of an unsupported mechanic, 17 §14).
     They stay open and the rest of the wave runs; never fake one mechanic with another.
2. **Pictures**, only if the plan orders any. Skill `bloomlings-round-pictures` with the plan's order: counts by stage
   and size, strong pictures, streams, motifs to avoid, families to prefer.
   - The mass is SVG drafts plus `svg2cells.mjs`.
   - Special pictures are hand matrices.
   - Everything passes `screen.mjs`, then goes on a numbered contact sheet.
   - The owner approves every picture. Wait for the decision, never approve on the owner's behalf.
3. **Build and place.** Run `node tools/rounds/rounds.mjs run <from> <to> --jobs 4` in the background.
   - It is resumable: run it again after an interruption.
   - It does, in order:
     - a dry placement;
     - candidates for open positions only;
     - missing checks;
     - placement;
     - `audit.mjs`;
     - `summary.md`;
     - the sample pilot in `content/rounds/runs/L####-L####/sample/`.
   - Exit code 1 means a position stayed open or a mandatory rule failed. For an open position, re-plan and order
     pictures (back to step 1). For a failed rule, read `audit.md`, fix the cause in the tools or rules, rerun.
   - Commit `content/rounds/` (levels, bank, needs, runs) periodically; the bank changes during builds.
4. **Hand over.** Send the owner:
   - `summary.md`, the sample pilot (`index.html` and its ZIP) and `audit.md`;
   - which levels are in the sample and why: every Super Hard, 2 Hard, every introduction, 2 random regular Normals,
     2 outliers closest to a metric's bound.
5. **Review** when the owner sends results.
   - Run `node tools/rounds/rounds.mjs review <results.json>`. It writes
     `content/rounds/reports/<pilot>-review-<date>.md` and a compact copy under `product/playtests/`.
   - For each mismatch, find the metric that missed it and propose a new bound in `content/rounds/quality.json`.
     Mismatches are a Hard or Super Hard rated easy, a regular Normal rated boring, or a note about forcing, waiting or
     "did not think".
   - Changing a bound is the owner's call when it changes what passes. Then rerun `audit.mjs` and the placement
     preference by running `rounds.mjs run` again (no rebuild needed).
   - Copy the owner's notes into the log `content/rounds/owner-notes.md` with what each one leads to.
   - A layer B bound becomes `"mode": "hard"` after two samples in a row confirm it.
6. **Drift.** Every 100 levels, compare the summaries of the waves: class shares, Normal median, doses and recipes,
   sizes, families and motifs, durations, open positions. Report any sharp shift to the owner before the next wave.

## Quality, in short (17 §13)

- **A — mandatory**, by `audit.mjs`: every rule of 17. A НЕТ removes the level.
- **B — interest**, by `quality.json`: pods without work. Bounds: regular Normal ≥ 0.9, Hard ≥ 1.5, Super Hard ≥ 2.0,
  introductions free. Soft until two samples confirm.
- **C — the owner's sample**, about 10 per wave.

## Do not

- Do not widen boards, pile on mechanics or weaken a check to fill a number (17 §13). An open position goes back to the
  picture order.
- Do not install round levels into the production catalog or the Unity client. The wave ends at the pilot and the
  committed content.
- Do not spend tokens re-deriving what the scripts print.
