# Mechanics and board readability · 2026-10-15

**Owner amendment · 2026-10-26:** doses, sizes and the approved set for new rounds are in [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md); the stack limit of 24×26 is kept there (§8).

Owner direction: visible stacks contain one exact target type and show a decreasing count. Mechanics must stay
readable across board sizes; a mechanic may be excluded from dense boards. New launch art follows the approved
size distribution in `content/pictures/art-direction.md`.

## Implemented: visible counted stacks

- Sparse cell overlay `stackCount: 2..9`, including the top tile. Every item is a separate target tile of the same
  exact variant, cleared automatically for one work unit. The cell becomes walkable after its last item is taken.
- A stack has two visible slab edges and a large digit on cream. The counter reaches 1 before the cell opens;
  counters change at worker arrivals in the presentation, even if the rules have already resolved later work.
- No key, mystery or hidden different-variant layer may share that cell. Other mechanics can occur elsewhere.
- Counts participate in exact pod accounting, solver searches, state hashes, clone/restore and boosters. They are
  public information and stay fixed in sampled hidden-layer fairness worlds.
- Planned introduction L110, practice L111, with counts 2–4 initially and up to 9 from L150. The roadmap entry is
  optional until production migration: the existing launch catalog has no stacks and is not rewritten here.
- Authoring ceiling 24×26, to retain readable single digits. Core rules also support a stack on 36×36; generation
  and validation refuse those tiny counters. An oversized stack platform is a separate future extension.

## Implemented: objects with readable footprints

New `readableMechanics: true` generation profiles use `gen-1.5.0`. Gates, Fountains, Chests and Statues/Bridges occupy
2×2 cells, or 3×3 when width exceeds 24 or height exceeds 26. They are placed in background regions, avoiding entries
and other objects. If no valid footprint/solution exists, the candidate is rejected or the optional mechanic dropped.
The full footprint blocks routes and opens together. Chest requirements include all adjacent target layers around it.

Both clients draw one object over the full footprint, with a counter of remaining work. Condition marks, triggers,
particles, accounting and the original unlock rules still apply. Single-digit stack and object counters retain at least 8 dp text across the
reference phone layouts. Fountain variant chips and counted-cell outlines are shown in both clients. Keys use a large centered mark over their tile.
Legacy definitions remain playable; new footprints require newly generated definitions, not a visual enlargement of
the old one-cell blockers.
The normal band presets and the Daily preset enable this policy for future generation. Existing Daily and ordinary
catalog files remain intact. Custom profiles that allow `counted_stack` enable it automatically.

## Authoring compatibility

| Mechanic | 14×16 / 18×20 | 21×26 / 24×24 | 22×28 / 28×28 | 30×32 / 36×36 |
|---|---|---|---|---|
| Stones | Yes | Yes | Yes | Yes |
| Connected pairs/triples | Yes, after unlock | Yes | Yes | Yes |
| Key / locked pod / locked slot | Up to 20×24 | Excluded | Excluded | Excluded |
| Visible counted stack | Yes, from L110 | Yes | Excluded | Excluded |
| Hidden different-variant layers | Yes, from L28 | Yes | Up to 28×28 | Excluded |
| Gate / Fountain / Chest / Statue / Bridge | 2×2 object | 2×2 object | 3×3 object | 3×3 object |
| Mystery Tile / Pod | Only up to 288 cells; Pod is not on the default roadmap | Excluded | Excluded | Excluded |

These are deterministic content rules, shared by generation and validation, not device-dependent changes to a
level. Existing `gen-1.3.0`/`gen-1.4.0` records retain their previous restrictions. Readability on actual devices still
needs playtesting: solver success proves the logical puzzle, not comfort or recognizability.

## Further feature proposals — not implemented or added to the roadmap

1. **Garden order.** A large 3×2 sign requests an explicit number of one visible variant anywhere on the board.
   Fulfilling it opens a route or restores a bridge. Suits dense mosaics: one large counter, no tiny keys, and a choice
   of which reachable part to restore first. Candidate introduction around L180–220.
2. **Double seal.** A wide barrier shows two large variant marks with separate remaining counts. Both must be
   completed, in either order. Creates dependencies between two colors without hidden information or extra tapping.
   Candidate introduction around L300–350; first demonstrate one seal before combining it with other mechanics.
3. **New Garden Entry.** Restoring a visibly marked garden patch activates a second entry, exposing a different
   front of the picture. Distinctive opening animation and route changes, with a large marker suited to 30×32/36×36.
   Candidate introduction around L550–650. Needs new entry-state rules and reachability/solver validation.

Each proposal needs a pilot with clear player information, a win without boosters, a jam witness and on-phone
readability before it becomes launch content. No timer, random refill or additional mandatory tap is proposed.

## Reproduction and review

- L5004: `content/curated/dev/level-dev-counted-stack-24x24.json`, profile
  `content/profiles/dev/band-counted-stack-24x24.json`, seed 1, class `normal`, forced mechanic `counted_stack`.
- L5005: `content/curated/dev/level-dev-large-gate-36x36.json`, profile
  `content/profiles/dev/band-large-gate-36x36.json`, seed 2, class `normal`, forced mechanic `gate`.
- L5006: `content/curated/dev/level-dev-large-fountain-36x36.json`, profile
  `content/profiles/dev/band-large-fountain-36x36.json`, seed 2, class `normal`, forced mechanic `fountain`.
- All use `content/pictures/dev/lib`, empty history/catalog/curated/keep directories, and a separate work output.
  These are engineering fixtures reusing existing test pictures, not additional unique launch compositions.
- Desktop preview frames 96/97 show the initial stack and the actual 9→5 arrival state; frame 98 shows the 3×3 gate and frame 99 the Fountain with its exact-variant chip.
  The tester has direct shortcuts and Unity's Play Dev Level menu has these fixtures. The ordinary APK excludes them.
- Cross-size tests cover 14×16, 18×20, 21×26, 24×24, 22×28, 28×28, 30×32 and 36×36. The Unity-stub suite cannot execute uGUI or
  coroutines; actual client device/Editor checks remain required before release.

## Owner amendment · 2026-10-26 — level generation rules

Kept: stacks of one material, readable counters, grid not over 24×26 for stacks (17 §8, heights 2–4 in generation).
Changed: the per-size mechanic table above, the hidden-layer rules, Gate, Fountain, Chest and Statue do not apply to
new rounds; the proposals "Garden order", "Double seal" and "New Garden Entry" are not in the approved set.
