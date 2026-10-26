# Bloomlings — Locked Concept v0.5

**Owner amendment · 2026-10-26:** level generation of new rounds follows [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md) (base version v8; 12 mechanic types; calendar every 15th Super Hard, other multiples of 5 Hard).

## Structural reference

Bloomlings intentionally uses Colony Flow as its product/gameplay structural reference:

- dense picture-like board;
- numbered Source groups;
- 5 Waiting Slots;
- automatic exact-match workers;
- partial completion;
- Jam risk;
- no timer;
- sequential Level N progression;
- minimal Home;
- Play → Level → Next.

## Bloomlings identity

Four character families:
- Sprig
- Bloom
- Drop
- Twig

Families contain multiple exact target variants.

Launch global pool:
- 8 variants.

Typical level:
- 3–6 variants.

## Launch content scale

Bloomlings must support **5000+ sequential levels at launch**.

The solution is:
- curated onboarding;
- progression profiles;
- deterministic generation;
- solver validation;
- tiered QA.

## Unlock philosophy

Early levels unlock systems quickly.

Current anchors:
- L3 Extra Slot
- L4 Shuffle
- L6 Return
- L9 Bloom Burst
- L10 Leaderboard
- L40 Wardrobe/Skins
- L50 Daily Challenge candidate
- L100 major milestone

After ~L500, the game relies primarily on:
- board variety;
- target variants;
- mechanic combinations;
- Hard/Super Hard profiles;
- milestone rewards;
- cosmetics;
- leaderboard progression.

## Visual lock

Strict flat 2D.

Tiles are simple target symbols.

Bloomlings are moving workers.

No 3D/isometric drift.

## Navigation lock

No level map.

No Garden Areas.

Home → Play → current Level N.

## Technical direction

High-level technology choice:

- Unity
- C#
- 2D rendering/presentation
- deterministic gameplay simulation
- data-driven levels
- one reusable gameplay runtime rather than one scene per level
- offline generator + solver pipeline
- 5000+ versioned level definitions
- offline-first main gameplay
- lightweight backend for save/meta/leaderboard/config
- standard mobile integrations for ads, IAP, analytics and crash reporting

Low-level code architecture is intentionally not locked yet.

## Development gate

Do not begin full implementation until:
- gameplay/pre-production docs are locked;
- unlock roadmap is locked;
- generator/solver rules are locked;
- launch content strategy is locked;
- this high-level Technical Architecture is locked.

Only then move into implementation planning.

## Owner amendment · 2026-10-26 — level generation rules

New rounds: pictures by the skill `bloomlings-round-pictures`, levels by `bloomlings-round-levels`, under [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md).
Materials are the pilot's 16 colours (4 per hero; up to 9 per level, 10–11 rarely), which supersedes the 8-variant
launch pool for rounds.
