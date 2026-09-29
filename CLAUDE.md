# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Bloomlings** is a light, minimal, strictly 2D mobile puzzle game modeled on the structure of
*Colony Flow!* (ABI Games). The player taps numbered Spirit Pods from a stacked Source Tray into
5 Waiting Slots. Bloomlings then automatically clear reachable board tiles of the pod's exact
target variant. The level is lost when the slots jam. There are 4 character families (Sprig,
Bloom, Drop, Twig) and 8 exact target variants at launch (Leaf/Moss, Flower/Violet Bud,
Water/Dew, Wood/Acorn). Progression is a linear sequence of 5000+ levels, with no map.

## Current stage: implementation of spec 001 (development gate open)

- **Gameplay reference: Colony Flow! (ABI Games).** Keep its core gameplay idea, its simplicity,
  how it paces new levels and mechanics, and how simple and convenient its screen and level
  layouts are.
- **Design docs: `product/` v0.5** (LOCKED on 2026-09-29, see `specs/001-core-game-mvp/gate.md`). `LOCKED_CONCEPT_v0.5.md` is the summary,
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
- **Development gate** (from `LOCKED_CONCEPT_v0.5.md`): passed on 2026-09-29, all docs are locked. Changes to
  locked docs now go through the product owner.
- **Technical direction** (doc 15, locked): Unity + C#, a deterministic data-driven
  gameplay core, an offline generator and solver, versioned level definitions, and a lightweight
  backend.

## Build and test commands

Requires the .NET 10 SDK (pinned by `core/global.json`; outputs go to `core/artifacts/`).

- `dotnet build core/Bloomlings.sln` builds the shared libraries, tools and tests.
- `dotnet test core/Bloomlings.sln` runs the core, content, solver and generator tests.
- `dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj` compiles the Unity client scripts against
  API stubs and runs the engine-free EditMode tests under .NET (extend `client/DotnetCheck/UnityStubs.cs` when the
  client uses a new Unity API).
- `BLOOMLINGS_GOLDEN_REGEN=1 dotnet test core/Bloomlings.sln --filter GoldenReplayTests` regenerates golden replays
  after an intended, reviewed rules change (`core/tests/golden/README.md`).
- `dotnet run --project core/src/Bloomlings.Pipeline -- <command>` runs the content pipeline CLI
  (`contracts/pipeline-cli.md`). Generated batches go to `content/work/` (gitignored). The picture library in
  `content/pictures/lib` is mostly `draft` until a person approves it (FR-084): `generate --allow-draft` builds
  previews from drafts, `--history <batch>` chains preview batches, and `validate` still fails such levels on
  `picture-approved`. `content/readability/approved-pairs.json` is `provisional` until the readability sign-off.
  Mechanic showcase levels live in `content/showcase/` (generated with `generate --mechanics <m> --class normal`);
  `generate` keeps them fixed (`--keep`).
- Open `client/` with Unity 6.3 LTS for the game client; see `client/README.md` for the first-open steps.
- CI: `.github/workflows/core-tests.yml` builds and tests `core/` and the client check on every push and pull request.

## Spec-Driven Development (GitHub Spec Kit)

The repo is initialized with [Spec Kit](https://github.com/github/spec-kit) for the Claude
integration (`.specify/` + `.claude/skills/speckit-*`). Feature work goes through these skills,
in order:

1. `/speckit-constitution` — project principles → `.specify/memory/constitution.md`
   (ratified 2026-09-29, current **v1.0.1**; amend only via PR with a version bump — see its Governance).
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

Summary of the constitution (`.specify/memory/constitution.md` v1.0.1, principles I–VII). The
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
