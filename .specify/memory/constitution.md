# Bloomlings Constitution

## Core Principles

### I. Colony Flow Structure, Bloomlings Identity

Bloomlings keeps the gameplay structure of *Colony Flow!* (ABI Games):

- numbered source pods in a stacked tray;
- 5 Waiting Slots;
- automatic workers that clear only the reachable matching work;
- partial completion;
- the jam as the fail state;
- no timer;
- a linear Level N;
- a minimal Home → Play → Level → Next flow.

Bloomlings differs through its own identity:

- the four Bloomling families with exact target variants;
- picture-first levels whose finished picture is revealed;
- the garden theme;
- the Bloomlings-specific mechanics.

Rules:

- A change to the core loop's structure MUST go through a spec, and the product owner MUST decide on it explicitly.
- The product MUST NOT reuse Colony Flow's name, characters, art, audio, level pictures or interface graphics.

Rationale: The reference structure is proven and simple. The product's value comes from keeping that simplicity while
owning a distinct identity.

### II. Exact Matching on an Unambiguous Board (NON-NEGOTIABLE)

- A Spirit Pod clears only tiles of its **exact target variant**. A family (Sprig, Bloom, Drop, Twig) is never a
  wildcard.
- Work accounting is kept per variant, never per family.
- Every gameplay cell MUST be exactly one thing at any moment and MUST never look partially occupied.
- Every variant MUST be identifiable by at least hue and icon. Two variants of the same family MUST be as easy to tell
  apart as two unrelated colors.
- No pair of variants may share a level before it passes the readability tests.

Rationale: Colony Flow-like depth comes from many exact colors. Readability is what keeps that depth fair.

### III. Deterministic Simulation (NON-NEGOTIABLE)

- The same level definition plus the same sequence of accepted commands MUST produce the same outcome and state hash
  on every device, platform, animation speed and frame rate.
- The rules live in **one** pure C# implementation with no engine dependencies. The client, solver, generator,
  pipeline and tests all share it.
- Rules code MUST NOT use:
  - wall-clock time or floating-point logic;
  - unspecified randomness;
  - collection order that depends on hash iteration;
  - time-based budgets.

  Its only randomness is a specified, seeded PRNG, and its budgets count nodes.
- Presentation replays the core's event log and MUST never influence rules.

Rationale: Determinism is what makes solver validation, replays, support, fair leaderboards and "same level for every
player" possible.

### IV. Validated, Fair Content (NON-NEGOTIABLE)

Every shipped level MUST have all of the following:

- solver validation, with a stored winning trace;
- a solution that needs no boosters;
- exact per-variant accounting;
- no inaccessible mandatory content;
- no mechanic or variant before its unlock level;
- no failure that can only be avoided with hidden knowledge;
- a readable palette;
- a stable, versioned definition.

In addition:

- Every non-tutorial level MUST be losable through bad choices.
- Levels MUST be generated picture-first and solution-first; random painting is forbidden.
- Publishing MUST fail if any level violates these rules.
- A shipped level MUST NOT change unless the fix is deliberate and versioned.

Rationale: With 5000+ generated levels, trust depends on validation that is automatic and cannot be bypassed.

### V. Difficulty from Decisions, Not Grind

Difficulty MUST come from:

- source ordering;
- dependencies;
- the number of variants;
- combinations of known mechanics.

The following are forbidden:

- tile hit points;
- timers;
- move limits;
- random spawning;
- manual path drawing;
- repetitive tapping.

A failure must read as "I committed that pod too early", never as "the game hid information".

- Difficulty moves in waves with relief levels; it MUST NOT climb monotonically.
- Major new mechanics become rare after about Level 500.
- Each unlock introduces at most one major mechanic.

Rationale: Planning-based difficulty keeps the game relaxing and fair across thousands of levels.

### VI. Fair Monetization, No Power Creep

- Boosters are optional tools for recovering from mistakes. No level may require spending money or watching ads.
- Nothing may change the exact-matching rule or give Bloomlings extra power. This applies to boosters, cosmetics,
  rewards and progression. Cosmetics MUST NOT reduce readability.
- There are no lives or energy gates: a failed or abandoned attempt costs nothing.
- Interstitial ads never appear during a level, right after a fail, or in onboarding.
- Rewarded ads are always optional and started by the player.
- Personal data is used for ads or analytics only after the consent that the platforms and the law require.

Rationale: A long-run, relaxing puzzle keeps players through trust, not pressure.

### VII. Simplicity, Readability and Offline-First

- The product MUST stay strictly flat 2D, calm, minimal and board-dominant.
- Gameplay uses one-tap input: the player taps pods and never taps target cells.
- States are shown visually, with minimal text.
- The product MUST NOT add a level map, a level chooser, garden areas, a room builder or navigation overhead.
- The main gameplay, progression, owned boosters and settings MUST work offline. Online services are limited to
  meta functions.

Rationale: Colony Flow's convenience is its simplicity, and every added screen or system dilutes it.

## Technical and Content Constraints

**Platforms.** Android and iOS phones in portrait. Tablets are supported by scaling.

**Technology direction** (doc 15; binding once the docs are locked):

- a Unity 6.x LTS client for presentation and services only;
- the rules, solver, generator and pipeline as pure C# libraries (netstandard2.1) shared with .NET tooling.

**Content:**

- Levels are **data**: versioned definitions that reference base pictures. They are not engine scenes.
- The launch catalog of 5000+ levels ships with the app.
- Later content is delivered as hash-verified, versioned packs.
- The definition of a level number stays identical for every player.

**Backend:**

- The backend is limited to identity, cloud save, the leaderboard, remote configuration and validation of purchases
  and submissions.
- The core puzzle MUST NOT require an always-online connection.

**Vendors.** Every third-party service (backend, ads, IAP, analytics, crash reporting) MUST sit behind a replaceable
interface.

**Remote configuration** may tune economy, ads and feature flags. It MUST NOT change core rules or shipped level
definitions.

**Protection.** Purchases, permanent entitlements and leaderboard submissions MUST be protected in proportion to their
abuse risk.

## Development Workflow and Quality Gates

**Spec-driven flow.** Feature work follows Spec Kit: specify → (clarify) → plan → tasks → implement. The binding
requirements are the feature specs in `specs/`, which consolidate the product docs in `product/`. Where the product
docs, a spec and the reference game conflict, the conflict MUST be flagged to the product owner and resolved in the
spec.

**Development gate.** Full implementation MUST NOT start until all of these are locked:

- the product docs;
- the unlock roadmap;
- the generator and solver rules;
- the launch content strategy;
- the technical architecture.

Scaffolding, prototypes explicitly labelled as such, and design artifacts are allowed before that.

**Every change to rules code MUST include:**

- rule tests;
- updates to the golden replay corpus;
- passing determinism checks.

**Every change to content** (pictures, profiles, levels) MUST pass pipeline validation in CI before merge.

**The full catalog** is re-validated nightly and before every release.

**Plan compliance.** Every plan MUST include a Constitution Check against these principles. Each deviation MUST be
justified in the plan's Complexity Tracking, or the deviation MUST be removed.

## Governance

This constitution overrides other engineering practices and guidance in this repository. Where they conflict:

- a feature spec defines *what* is built;
- this constitution defines the non-negotiable *how*;
- `CLAUDE.md` is the runtime guidance file for agents and MUST stay consistent with this document.

**Amendments** are made by pull request that modifies this file. The pull request includes:

- a Sync Impact Report;
- the version bump;
- the product owner's approval.

A change to a NON-NEGOTIABLE principle also requires an updated spec that explains why the change is needed and how
existing content migrates.

**Versioning** follows semantic versioning:

- MAJOR: a principle is removed or redefined in a backward-incompatible way;
- MINOR: a principle or section is added or materially expanded;
- PATCH: clarifications and wording.

**Compliance** is checked:

- in every plan's Constitution Check;
- in pull-request review;
- by the CI gates above.

Non-compliant changes MUST NOT be merged.

**Version**: 1.0.0 | **Ratified**: 2026-09-29 | **Last Amended**: 2026-09-29
