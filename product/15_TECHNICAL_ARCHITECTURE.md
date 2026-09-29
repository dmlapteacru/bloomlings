# 15 — Technical Architecture

**Status:** DRAFT FOR LOCK  
**Scope:** conceptual technology and system architecture only.  
**Out of scope:** low-level class design, folder structure, dependency injection, exact interfaces, frame-by-frame implementation details.

---

# 1. Technology choice

## Client engine

**Unity + C#**

Bloomlings is a 2D mobile puzzle game, but Unity is selected because the product needs:

- many animated 2D entities on screen;
- deterministic game simulation;
- large data-driven level catalog;
- mobile deployment to Android and iOS;
- ads and in-app purchases;
- analytics and crash reporting;
- leaderboards;
- remote configuration;
- long-term content updates;
- mature tooling for performance profiling and release automation.

The project should remain strictly **2D**.

No 3D gameplay architecture is required.

---

# 2. Rendering and presentation

Use Unity's normal 2D rendering stack.

Primary visual technologies:

- sprites;
- 2D animation;
- lightweight particles/effects;
- UI canvas/UI layer;
- object pooling for frequently reused visual entities.

The rendering layer should be treated as presentation only.

Gameplay rules must not depend on:
- physics;
- frame rate;
- animation timing;
- visual object lifetime.

This preserves deterministic puzzle behavior.

---

# 3. Core game simulation

The gameplay core should be implemented as a deterministic C# simulation.

It owns concepts such as:

- board state;
- target variants;
- Source Pods;
- Waiting Buffer;
- reachability;
- keys/locks;
- layered cells;
- special mechanics;
- win state;
- Jam state.

The Unity scene visualizes this state.

Conceptual rule:

> Game logic decides what happens. Unity visuals show what happened.

This separation is important for:
- reproducible gameplay;
- solver reuse;
- automated testing;
- level validation;
- 2× speed;
- replay/debugging;
- future server-side or tooling validation if needed.

---

# 4. Level content model

Levels are **data**, not Unity scenes.

There is one reusable gameplay runtime.

A level definition contains the data required to construct:

- board mask;
- tile/layer layout;
- target variants;
- Source Pod stacks;
- pod counts;
- keys/locks;
- connected groups;
- environmental mechanics;
- difficulty profile;
- rewards;
- deterministic seed/version.

The game should not contain thousands of Unity scenes such as:

`Level_0001.unity`
`Level_0002.unity`
`...`

Instead:

`Gameplay Runtime + LevelDefinition N`

---

# 5. 5000+ level content strategy

Launch target:

**5000+ solver-validated levels**

These should be represented as compact data definitions.

Recommended content split:

- first 100: heavily curated;
- 101–500: generated + strong review;
- 501–5000+: profile-generated + solver-validated + QA sampling.

The shipped game may include the launch catalog locally.

Future content should be deliverable independently from the binary where practical.

---

# 6. Generator and solver

The Level Generator and Solver are first-class product systems.

They should be implemented as reusable C# logic with minimal dependency on Unity runtime.

Conceptually:

### Generator
Creates candidate level definitions based on:
- progression band;
- allowed mechanics;
- target difficulty;
- board size;
- variant count;
- topology/archetype;
- Source ordering.

### Solver
Validates:
- solvability;
- exact target accounting;
- valid mechanic interactions;
- Jam paths;
- hidden-information fairness;
- difficulty metrics.

### Content pipeline
`Generation → Validation → Scoring → Review → Publish`

This pipeline is required to make 5000+ levels economically feasible.

---

# 7. Content versioning

Every level must have a stable identity.

Recommended concepts:

- level number;
- level definition version;
- generation seed;
- content-pack version.

A player who is on Level 2187 should not receive a completely different Level 2187 simply because the app was updated, unless a deliberate content fix/version migration is made.

This matters for:
- support;
- leaderboard fairness;
- QA;
- analytics;
- reproducing bugs.

---

# 8. Content delivery

## Launch

The initial 5000+ level catalog can be bundled with the application as compact level data.

This ensures:
- offline play;
- fast startup;
- no dependency on network availability.

## Future

Additional level packs/configuration may be delivered through:

- remote content bundles;
- CDN/object storage;
- remote configuration;
- versioned content manifests.

The gameplay client should therefore distinguish:

- application version;
- level-content version.

This allows adding/fixing content without requiring a full app release in every case.

---

# 9. Backend scope

The core puzzle game should remain playable without a backend connection.

Backend services are needed for online/meta functions, not for resolving every move.

Expected backend responsibilities:

- player identity/profile;
- cloud save;
- leaderboard;
- remote configuration;
- economy/purchase validation where required;
- live-event configuration later;
- progression backup;
- server-authoritative reward validation where abuse risk justifies it.

Core level simulation remains client-side.

---

# 10. Player identity and save model

Baseline should support frictionless play.

Recommended account model:

1. local player profile created automatically;
2. optional platform identity integration;
3. cloud synchronization when available.

Potential integrations:

- Google Play Games Services;
- Apple Game Center;
- platform/cloud identity provider if later required.

Saved state should include at minimum:

- highest unlocked/completed level;
- currencies;
- booster inventory;
- unlock flags;
- cosmetics;
- settings;
- milestone rewards;
- relevant statistics.

Level definitions themselves are content, not save data.

---

# 11. Leaderboard

Leaderboard unlock target:

**Level 10**

Primary metric:

**highest completed level**

This maps naturally to Bloomlings' linear progression.

Potential later boards:

- global highest level;
- weekly progression;
- event leaderboard.

Baseline should remain simple.

Leaderboard services may use:

- platform leaderboards;
- or a lightweight backend leaderboard service.

The final choice depends on desired cross-platform behavior.

---

# 12. Remote configuration

Remote Config is strongly recommended.

Values suitable for remote tuning:

- ad cadence;
- rewarded-ad availability;
- booster prices;
- booster grants;
- milestone rewards;
- Hard/Super Hard cadence;
- feature flags;
- Daily Challenge availability;
- content-pack activation;
- Store offers.

Core puzzle rules should not become remotely mutable in unsafe ways that invalidate level solvability.

---

# 13. Analytics

Analytics is not required to finish pre-production design, but the technical architecture should reserve a clean analytics integration point.

Expected event categories later:

- level start;
- level complete;
- level fail/Jam;
- booster use;
- ad impression/reward;
- purchase;
- progression unlock;
- retention/session;
- difficulty performance;
- generator/content quality signals.

Provider should be replaceable at the architecture level.

Typical choices may include:
- Firebase Analytics;
- Unity Analytics;
- another mobile analytics stack.

The final provider can be selected later.

---

# 14. Crash reporting and observability

Production mobile build should include:

- crash reporting;
- non-fatal error reporting;
- app/version metadata;
- device/OS metadata;
- content-pack version;
- current level ID/version.

This is especially important with thousands of data-driven levels.

A crash or invalid level report must be reproducible from:
- app version;
- level number;
- content version;
- seed/version.

---

# 15. Ads

Monetization integration needs:

- interstitial ads;
- rewarded ads;
- Remove Ads entitlement.

Ads must remain outside gameplay simulation.

The ad provider should be integrated behind a replaceable boundary because mediation/provider strategy may change.

Potential technology direction:

- Google Mobile Ads / mediation stack;
- or another mature mobile ad mediation provider.

Provider selection should be commercial/operational, not gameplay architectural.

---

# 16. In-app purchases

IAP categories:

- Remove Ads;
- Petal/currency packs;
- booster bundles;
- cosmetic packs.

Use platform stores:

- Google Play Billing;
- Apple StoreKit via Unity-compatible integration.

Purchase entitlements should be recoverable/restorable.

Permanent products such as Remove Ads should not depend only on local storage.

---

# 17. Asset/content packaging

Separate conceptually:

### Application assets
- UI;
- Bloomling characters;
- common tile art;
- booster art;
- special mechanic art;
- common backgrounds.

### Level data
- masks;
- tile definitions;
- Source Pods;
- mechanic parameters.

### Optional downloadable content
- new background themes;
- cosmetic sets;
- additional target variants;
- future content packs.

This keeps the application reusable while content grows.

---

# 18. Performance strategy

Target platforms:

- Android;
- iOS.

Primary performance risks:

- large cell counts;
- many simultaneous Bloomling animations;
- particles/effects;
- low-end mobile devices.

Conceptual mitigations:

- pooled visual entities;
- lightweight 2D rendering;
- deterministic simulation independent of rendering;
- bounded number of simultaneously animated workers;
- scalable visual quality if required.

Do not solve performance by reducing core board complexity before profiling.

---

# 19. Offline behavior

Main numbered levels should be playable offline.

Offline-capable:

- gameplay;
- progression;
- locally cached rewards;
- boosters already owned;
- settings.

Online-dependent:

- leaderboard refresh;
- cloud synchronization;
- ads;
- store purchases;
- remote events/config refresh.

Synchronization conflicts should favor preserving valid player progression and purchased entitlements.

---

# 20. CI/CD

Recommended automated pipeline:

### Pull request
- compile;
- unit tests;
- solver/generator tests;
- content validation;
- static checks.

### Content validation
- validate all changed level definitions;
- detect exact-count mismatch;
- verify mechanic unlock constraints;
- solve changed/generated levels;
- report difficulty metrics.

### Release
- Android/iOS build;
- signed artifacts;
- staged deployment;
- environment-specific configuration.

The 5000-level catalog should be treated as validated build input.

---

# 21. Testing strategy

High-level test layers:

### Game-core tests
Pure deterministic puzzle rules.

### Solver tests
Known solvable/unsolvable cases.

### Generator tests
Generated content invariants and quality bounds.

### Content validation
Every shipped level.

### Unity integration tests
Rendering/input/state integration.

### Device QA
Representative Android/iOS devices.

### Progression QA
Unlock roadmap, rewards and long-session behavior.

The game should be testable without watching animations.

---

# 22. Environments

At minimum:

- Development
- Staging
- Production

Environment-specific values:

- analytics project;
- ads configuration;
- backend endpoint;
- leaderboard environment;
- Store product IDs;
- remote-config namespace.

Do not hard-code production credentials/configuration in gameplay content.

---

# 23. Security and anti-cheat

Bloomlings is primarily a casual client game, so security should remain proportional.

Protect:

- purchases;
- permanent entitlements;
- leaderboard submissions;
- premium currency where abuse materially affects product/business.

Do not make the core puzzle require always-online server authority.

Leaderboard submissions may include sanity checks such as:
- progression monotonicity;
- impossible-level jumps;
- content/version compatibility.

---

# 24. Technology summary

## Client
- Unity
- C#
- Unity 2D rendering/UI stack

## Game Core
- deterministic pure C# simulation

## Content
- data-driven level definitions
- versioned 5000+ level catalog

## Generation
- offline/profile-driven generator

## Validation
- deterministic solver + automated content checks

## Backend
- lightweight online services for save/meta/leaderboard/config

## Delivery
- app-bundled launch levels + optional remote content packs

## Monetization
- mobile ads
- IAP
- rewarded recovery

## Operations
- analytics
- crash reporting
- remote configuration
- CI/CD
- content validation

---

# 25. Explicitly not decided here

This document intentionally does **not** lock:

- exact Unity project folder layout;
- exact class names;
- MonoBehaviour composition;
- DI framework;
- ECS/DOTS;
- exact UI framework choice;
- exact networking library;
- exact backend vendor;
- exact analytics vendor;
- exact database;
- exact cloud provider.

Those choices belong to implementation design only when they materially affect the product.

---

# 26. Architecture lock candidate

The current conceptual technical direction is:

> **Unity 2D + C# client, deterministic data-driven gameplay core, offline generator/solver pipeline, 5000+ versioned level definitions, offline-first gameplay, lightweight backend for meta/cloud/leaderboards, standard mobile services for monetization/config/analytics.**

This is sufficient as the high-level technical foundation before low-level implementation planning.
