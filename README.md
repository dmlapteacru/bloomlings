# Bloomlings: Garden Puzzle

A light, minimal 2D puzzle game: send groups of garden spirits in the right order to restore an
enchanted garden, reveal deeper layers and mechanisms, and never clog the limited staging buffer.

## Status

Design phase.

- Gameplay reference: *Colony Flow!* (ABI Games). Bloomlings keeps its core loop, its simplicity, its
  level progression and its layouts.
- How Bloomlings differs: [`product/CONCEPT.md`](product/CONCEPT.md). This is a direction, not
  binding rules.
- First feature spec: [`specs/001-core-game-mvp/spec.md`](specs/001-core-game-mvp/spec.md).

## Workflow

The project uses [GitHub Spec Kit](https://github.com/github/spec-kit) for spec-driven development
with Claude Code. Run the skills in order:

```
/speckit-constitution  →  /speckit-specify  →  /speckit-clarify  →  /speckit-plan
→  /speckit-tasks  →  /speckit-analyze  →  /speckit-implement
```

Feature specs are stored in `specs/NNN-<name>/`. See [`CLAUDE.md`](CLAUDE.md) for details.

## Repository layout

```
product/            Game design documents (concept, future design docs)
specs/              Spec Kit feature specs, plans and tasks (created per feature)
.specify/           Spec Kit templates, scripts, constitution and workflow
.claude/skills/     Spec Kit skills for Claude Code (/speckit-*)
CLAUDE.md           Guidance for Claude Code in this repository
```
