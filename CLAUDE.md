# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Bloomlings** is a light, minimal, strictly 2D mobile puzzle game modeled on the structure of
*Colony Flow!* (ABI Games). The player taps numbered Spirit Pods from a stacked Source Tray into
5 Waiting Slots. Bloomlings then automatically clear reachable board tiles of the pod's exact
target variant. The level is lost when the slots jam. There are 4 character families (Sprig,
Bloom, Drop, Twig) and 8 exact target variants at launch (Leaf/Moss, Flower/Violet Bud,
Water/Dew, Wood/Acorn). Progression is a linear sequence of 5000+ levels, with no map.

## Current stage: pre-production, no code yet

- **Gameplay reference: Colony Flow! (ABI Games).** Keep its core gameplay idea, its simplicity,
  how it paces new levels and mechanics, and how simple and convenient its screen and level
  layouts are.
- **Design docs: `product/` v0.5** ("draft for lock"). `LOCKED_CONCEPT_v0.5.md` is the summary,
  and `01`–`15` are the detailed documents: rules, tray/buffer, tiles and variants, mechanics,
  level structure, generator/solver, difficulty, meta, boosters, economy, UX, art, unlock
  roadmap, MVP scope and technical architecture. `CONCEPT.md` is the original v0.1 vision. Treat
  the docs as the current design direction, not as immutable rules, and flag any conflicts to
  the user.
- **Consolidated requirements: `specs/001-core-game-mvp/spec.md`.** Do not implement gameplay
  that is not specified there.
- **Implementation plan: `specs/001-core-game-mvp/plan.md`** (+ `research.md`, `data-model.md`,
  `contracts/`, `quickstart.md`): monorepo `core/` (pure C# rules, solver, generator, pipeline CLI),
  `client/` (Unity 6.3 LTS), `content/`, `backend/`.
- **Development gate** (from `LOCKED_CONCEPT_v0.5.md`): do not start full implementation until
  all of these are locked: the gameplay docs, the unlock roadmap, the generator/solver rules,
  the launch content strategy and the technical architecture.
- **Technical direction** (doc 15, not yet locked): Unity + C#, a deterministic data-driven
  gameplay core, an offline generator and solver, versioned level definitions, and a lightweight
  backend. No build, lint or test tooling exists yet; when it does, add the commands here.

## Spec-Driven Development (GitHub Spec Kit)

The repo is initialized with [Spec Kit](https://github.com/github/spec-kit) for the Claude
integration (`.specify/` + `.claude/skills/speckit-*`). Feature work goes through these skills,
in order:

1. `/speckit-constitution` — project principles → `.specify/memory/constitution.md`
   (ratified **v1.0.0**, 2026-09-29; amend only via PR with a version bump — see its Governance).
2. `/speckit-specify <description>` — feature spec → `specs/NNN-<name>/spec.md`
3. `/speckit-clarify` (optional) — resolve ambiguities before planning
4. `/speckit-plan` — implementation plan, research, data model, contracts
5. `/speckit-checklist` (optional) — requirement quality checklists
6. `/speckit-tasks` — `tasks.md` broken down by user story
7. `/speckit-analyze` (optional) — cross-artifact consistency check
8. `/speckit-implement` — execute tasks
9. `/speckit-converge` — compare code against spec and append remaining tasks

Layout:

- `.specify/templates/` — spec/plan/tasks/checklist/constitution templates
- `.specify/scripts/bash/` — helper scripts the skills call (feature numbering, paths, prerequisites)
- `.specify/memory/constitution.md` — project constitution
- `specs/NNN-<short-name>/` — per-feature artifacts (created by `/speckit-specify`)

Features are numbered sequentially (`001-`, `002-`, …). `.specify/feature.json` points the
skills at the current feature. It is machine-local state and is gitignored, so a fresh clone
(every cloud session) does not have it, and `/speckit-plan`, `/speckit-tasks` and the other skills
then fail with "Feature directory not found". Recreate the file before running them:
`echo '{"feature_directory":"specs/001-core-game-mvp"}' > .specify/feature.json`.
Alternatively, set `SPECIFY_FEATURE_DIRECTORY`.

To upgrade Spec Kit templates/skills: install the CLI with
`uv tool install specify-cli --from git+https://github.com/github/spec-kit.git`, then run
`specify init --here --force --non-interactive --integration claude --script sh`.

## Design invariants to respect in any spec or code

Summary of the constitution (`.specify/memory/constitution.md` v1.0.0, principles I–VII). The
constitution is authoritative; it adds the Colony Flow structure, fair monetization (no lives,
no pay-to-win), simplicity/offline-first and the workflow gates.

- Exact matching: a pod clears only its exact target variant. A family (Sprig, Bloom, Drop,
  Twig) is never a wildcard.
- Deterministic rules: the same level definition plus the same tap sequence always gives the
  same outcome, independent of animation, 2x speed or device.
- Every gameplay cell is unambiguous: fully one thing, never partially occupied.
- Difficulty comes from source ordering, dependencies, variants and mechanics, not from tile HP
  or repetitive tapping.
- Every shipped level is solver-validated, winnable without boosters, and has an exact
  per-variant work accounting.
- There are no gameplay power upgrades; progression rewards are cosmetic or convenience only.
