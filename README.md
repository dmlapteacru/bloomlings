# Bloomlings: Garden Puzzle

A light, minimal 2D puzzle game: send groups of garden spirits in the right order to restore an
enchanted garden, reveal deeper layers and mechanisms, and never clog the limited staging buffer.

## Status

Spec 001 (the launch game) is implemented, and its tasks are tracked in
[`specs/001-core-game-mvp/tasks.md`](specs/001-core-game-mvp/tasks.md). The deterministic core, the solver, the
generator, the content pipeline, the Unity client scripts and the backend scripts are in place, and all automated tests
pass. Still open:
- the steps that need the Unity Editor, devices, live service projects, human picture approval and playtests;
- production of the launch catalog.

- Gameplay reference: *Colony Flow!* (ABI Games). Bloomlings keeps its core loop, its
  simplicity, its level progression and its layouts.
- Design documents v0.5: [`product/`](product/). Start with
  [`LOCKED_CONCEPT_v0.5.md`](product/LOCKED_CONCEPT_v0.5.md).
- Consolidated feature spec:
  [`specs/001-core-game-mvp/spec.md`](specs/001-core-game-mvp/spec.md).

## Workflow

The project uses [GitHub Spec Kit](https://github.com/github/spec-kit) for spec-driven development
with Claude Code. Run the skills in order:

```
/speckit-constitution  →  /speckit-specify  →  /speckit-clarify  →  /speckit-plan
→  /speckit-tasks  →  /speckit-analyze  →  /speckit-implement
```

Feature specs are stored in `specs/NNN-<name>/`. See [`CLAUDE.md`](CLAUDE.md) for details.

## Build and test

Requires the .NET 10 SDK (pinned by `core/global.json`); the client needs Unity 6.3 LTS.

```bash
dotnet test core/Bloomlings.sln                                            # rules, solver, generator, content, pipeline
dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj        # client scripts against Unity stubs
dotnet run --project core/src/Bloomlings.Pipeline -- --help                # content pipeline CLI
```

- **Content flow**: `pictures validate`, then `generate`, `validate`, `score`, `review`, `publish` and `diff`, with
  `daily generate` for the Daily Challenge pool. The CLI is described in
  [`contracts/pipeline-cli.md`](specs/001-core-game-mvp/contracts/pipeline-cli.md).
- **Client**: open `client/` with Unity 6.3 LTS and see [`client/README.md`](client/README.md). The first-open steps
  cover the scenes, the content import, the SDK packages, localization and the device determinism player.
- **Backend**: see [`backend/README.md`](backend/README.md) for Remote Config defaults and the Cloud Code scripts.
- **CI**:
  - `.github/workflows/core-tests.yml` runs on every push;
  - `content-validate.yml` and `catalog-nightly.yml` check content;
  - `android-apk.yml` builds the Android APK, run by hand only (Actions → android-apk → Run workflow). It keeps just
    the newest APK artifact and needs the Unity licence secrets.

## Repository layout

```
core/               Deterministic rules core, solver, generator, content packs, pipeline CLI, tests (.NET)
client/             Unity 6.3 client (scripts, Editor tools, EditMode tests, DotnetCheck stub build)
content/            Pictures, curated levels, generation profiles, roadmap data, readability reports
backend/            UGS Remote Config defaults and Cloud Code scripts
product/            Game design documents (v0.5: 01–15, locked concept, changelog)
specs/              Spec Kit feature specs, plans, tasks and checklists
.specify/           Spec Kit templates, scripts, constitution and workflow
.claude/skills/     Spec Kit skills for Claude Code (/speckit-*)
CLAUDE.md           Guidance for Claude Code in this repository
```
