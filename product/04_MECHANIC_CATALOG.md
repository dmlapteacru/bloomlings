# 04 — Mechanic Catalog

**Status:** LOCKED (2026-09-29)
**Owner amendment · 2026-10-26:** the approved set for generating new rounds is the **12 types** of
[`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md) §7–§8 (table at the end of this document). Where this
catalog differs, 17 wins.

## A. Reference-core mechanics

### A1. Exact-Variant Spirit Pod
Pod matches one exact target variant and has numeric capacity.

### A2. Five Waiting Slots
Core risk system.

### A3. Reachability
Only reachable exact-match cells can be cleared.

### A4. Partial Completion
Pod may work partially, then wait.

### A5. Stacked Source Tray
Removing one pod exposes another.

### A6. Jam
All usable slots full + no progress.

### A7. Connected Pods
Multiple groups commit together.

### A8. Locked Pods + Keys
Board dependency controls Source availability.

### A9. Large blocker / door
Board region blocked until clear condition.

## B. Target-variant mechanic

### B1. Multiple Variants per Family
Four Bloomling families support multiple exact matching types.

Gameplay impact:
- greater color/type combinatorics;
- more pod-order possibilities;
- deeper source-stack puzzles;
- more interesting layers.

This is foundational, not optional.

## C. Bloomlings-specific additions

### C1. Layered Tile
Clearing one variant reveals another.

### C2. Fountain
Environmental dependency.

### C3. Locked Waiting Slot
Temporary 4-slot capacity.

### C4. Garden Gate / Hedge Seal
Themed route blocker.

### C5. Restoration Reveal
Clearing board reveals finished picture.

## D. Optional/advanced

- Mystery Pod
- Mystery Tile
- Linked Triple
- Chest
- Statue
- Bridge

## E. Interaction examples

| Combination | Result |
|---|---|
| Leaf layer → Violet layer | allowed |
| Moss pod + Leaf cell | no match |
| two Sprig variants active | independent |
| Connected Leaf + Dew pods | allowed |
| Key + locked Violet pod | allowed |
| Mystery pod | hides exact variant, not only family |
| Fountain + Water/Dew condition | allowed if exact condition is visible |
| Locked Slot + Key | allowed |

## F. Introduction principle

New target variants are simpler than new mechanics.

They can be introduced with:
1. one clean level dominated by new variant;
2. one mixed level;
3. normal inclusion.

Do not treat every new color as a major tutorial mechanic.

## G. Avoid feature creep

No combat, timers, random spawning, manual path drawing or permanent hero power upgrades in baseline.

## Owner amendment · 2026-10-15

Visible same-variant stacks with decreasing counters, readable multi-cell environmental objects and the size
compatibility policy are specified in [16 — Mechanics and board readability](16_MECHANICS_AND_BOARD_READABILITY.md).
The document separates implemented mechanics from further feature proposals.

## Owner amendment · 2026-10-26 — the approved set of 12 types

Level generation rules: [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md). Behaviour of each type: the full
mechanic catalog of 26.10.2026, [`design/Bloomlings-gameplay-mechanics.md`](design/Bloomlings-gameplay-mechanics.md); the numbers, calendar and recipes are in 17.

| # | Type (17) | Family for the calendar | Hero | Former name here | What it does |
|---:|---|---|---|---|---|
| 1 | Hidden pods | general | — | Mystery Pod (D) | material and count hidden until the pod is on top (before the tap) |
| 2 | Linked pods | general | — | A7 Connected Pods, D Linked Triple | 2–4 pods, one launch; all slots released together after the last member |
| 3 | Latch + acorn key | general | Twig | A8 Locked Pods + Keys | the latch is a column element, not a pod; it goes when it is on top and the key is reachable |
| 4 | Wooden shutters | general | Twig | A9 Large blocker / door, "coloured box" | cover cells; count the work of one exact material done anywhere |
| 5 | Closed bud | general | Bloom | C4 Garden Gate (partly) | cover cells; count the work of any material |
| 6 | Well | general | Drop | — | takes deliveries of one exact material (each spends quota); then becomes a passage |
| 7 | Hidden cells | general | Drop | D Mystery Tile | cell material hidden until an open reachable neighbour appears |
| 8 | Stacks of one material | general (puzzle) | — | `16` counted stack | several units of one material in one position |
| 9 | Bridge | hero | Sprig | D Bridge | one choice of two places across an impassable strip; no quota, no slot |
| 10 | Water canal | hero | Drop | — | one canal with two exits; the chosen exit becomes an extra entry |
| 11 | Repair of the fifth slot | hero | Twig | C3 Locked Waiting Slot | the level starts with 4 slots; the 5th is repaired after work of one material |
| 12 | Seed | hero | Bloom | — | a parent pod carries a visible future capsule; on ripening the player plants it in one of two columns |

Not in the approved set for new rounds: Layered Tile (C1), Fountain (C2), Chest, Statue, Stones and other objects
of `16` not listed above. Their code may stay, but the generator of new rounds does not use them. Old proposals
(`design/hero-extras-proposals-2026-10-26.md`) are superseded by this set.
