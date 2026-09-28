# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Bloomlings: Garden Puzzle** — a light, minimal 2D buffer-ordering puzzle game. The player
sends groups of garden spirits (Sprig, Bloom, Drop, Twig) to restore matching cells (Greenery,
Flowers, Water, Wood) on a dense grid; the main fail state is a jam of the 5-slot staging buffer.

## Current stage: design, no code yet

- `product/CONCEPT.md` is the **locked baseline concept (v0.1)**. Treat it as the source of truth
  for game design. Do not change its locked rules without an explicit request.
- Principle #13 of the concept: *development should not begin before core rules are fully
  specified.* The next step is a precise **Core Gameplay v1** spec (see §20 of the concept for the
  list of open questions). Do not start implementing gameplay before that spec exists.
- No tech stack, build, lint or test tooling has been chosen yet. When one is chosen, add the
  commands here.

## Spec-Driven Development (GitHub Spec Kit)

The repo is initialized with [Spec Kit](https://github.com/github/spec-kit) for the Claude
integration (`.specify/` + `.claude/skills/speckit-*`). Feature work goes through these skills,
in order:

1. `/speckit-constitution` — project principles → `.specify/memory/constitution.md`
   (currently an unfilled template; derive it from §18 "Core Design Principles" of the concept).
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

Features are numbered sequentially (`001-`, `002-`, …). `.specify/feature.json` is machine-local
state and is gitignored.

To upgrade Spec Kit templates/skills: install the CLI with
`uv tool install specify-cli --from git+https://github.com/github/spec-kit.git`, then run
`specify init --here --force --non-interactive --integration claude --script sh`.

## Design invariants to respect in any spec or code

- Deterministic puzzle rules: spirit type = matching target type; no gameplay power upgrades
  (progression is cosmetic only).
- Every gameplay cell is unambiguous (fully one type, empty, special tile, or blocker).
- Difficulty comes from dependencies and ordering, not tile HP or repetitive tapping.
- Procedural level generation must be solution-aware: build a valid dependency/solution graph
  first, then translate it into a board; every level has at least one valid solution.
