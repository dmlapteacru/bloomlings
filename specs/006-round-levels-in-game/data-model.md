# Data Model: Round Levels in the Game (spec 006)

Entities, fields, validation rules and states. Wire formats are in `contracts/`. "Reference" means the round engine at
the fingerprints recorded in the level's proof (research R12).

## RoundMaterial (16, fixed)

| Field | Meaning |
|---|---|
| `code` | one letter of `GPBACDVYOLMRKNSE`; the wire id in matrices and pods |
| `name` | Лист, Лепесток, Вода, Янтарь, Жемчуг, Земля, Слива, Солнце, Тыква, Лайм, Мох, Ягода, Румянец, Глубина, Небо, Кора (localized keys in the client) |
| `family` | sprig, bloom, drop or twig (G sprig, P bloom, B drop, A twig, C drop, D twig, V bloom, Y sprig, O twig, L sprig, M sprig, R bloom, K bloom, N drop, S drop, E twig) |
| `color` | normal palette hex (`palette.cjs normal`) |
| `accessibleColor` | accessible palette hex |
| `symbol` | the symbol shown in the symbols mode (♧ ✿ ◆ ● ◇ ▰ ✦ ☀ ▲ ✚ ♣ ♥ ❀ ▼ ○ ■) |

Rules:
- A pod clears only cells of its own code.
- Accounting is per code.
- Under normal vision every pair has a smallest CIEDE2000 ≥ 10 (FR-047). This is checked on the palette, once.

## RoundPicture

| Field | Meaning |
|---|---|
| `id` | `P####`, unique, never reused |
| `version` | integer ≥ 1; a changed matrix is a new version |
| `title`, `family`, `direction`, `subject`, `source` | theme catalog tags |
| `rows` | matrix, top row first: material codes, `.` empty, `#` stream, `b` bridge site |
| `w`, `h` | ≤ 44 each |
| `approval` | `{status: "approved", decidedAt, by: "owner"}`; only approved pictures transfer |
| `rights` | `own` for SVG drafts and hand matrices; franchise pictures are refused |

Validation:
- the matrix is rectangular;
- codes are known;
- every coloured cell is reachable through non-wall cells;
- a bridge picture (with its level's overlay) has exactly 2 `b` sites, each with exactly one orthogonal neighbour the entry reaches (its anchor: below, above or beside it);
- `source` is not a franchise or meme reference.

## RoundLevel (definition)

| Field | Meaning |
|---|---|
| `number` | the game position (1 …) |
| `definitionVersion` | integer ≥ 1; bumps on any change after first publish |
| `class` | Normal, Hard, Super Hard (calendar of 17 §5) |
| `introduction` | none, hidden, links, bridge; plus `second: true` on the second meeting |
| `picture` | `{id, version}` of an approved RoundPicture |
| `board` | `{w, h, rowsBottomFirst, entries, hero?, overlay?}`: rows with the frame ring and wall, bottom first; `entries` cell indices (one gate, bottom middle); `hero.choices[] = {cell, anchor, label}` for the Sprig bridge; `overlay = {walls: [[x, y]…], sites: [[x, y]…]}` in picture coordinates (x from the left, y from the top): a strip laid over the picture (17 §8 "Где стоит мост", 2026-10-28), its walls `#`, its sites `b` |
| `capacity` | 5 |
| `pods[]` | `{code, count ≥ 1, mystery?: "depth", group?: "A".."Z…"}` |
| `tray[]` | columns (2–6) of pod indices, top first |
| `mechanics` | subset of hidden, links, bridge |
| `exception?` | `{rule, reason, decidedBy, decidedAt}`, e.g. L18: rule "one deciding bridge from the second introduction", reason "owner kept both winning bridges" |
| `engine` | reference fingerprints (rules, timeline, palette, clock) |

Validation (all must hold; reference `rules.cjs validate` plus 17):
- board 7–48 wide, 8–48 high;
- tray is a partition of the pods;
- exact accounting: cells of each code equal the sum of counts;
- groups are pairs in different columns at equal start depth;
- hidden pods are at most 6, never in a group, never in the first row on an introduction;
- bridge sites are walls with an orthogonal neighbour as anchor (`|cell − anchor| == w`, or `== 1` in the same row; 17 §8 as amended on 2026-10-28);
- `mechanics` match the pods and hero;
- `class` matches the calendar;
- introductions follow 17 §9;
- the board is the picture's board (`boardOf`) with the overlay laid: every strip cell on the picture's edge closes the ring cell outward, each site's anchor is its one neighbour the entry reaches while every site is a wall, sites in reading order;
- the proof exists and passes (below).

## RoundState (runtime, rules only)

| Field | Meaning |
|---|---|
| `board[]` | per cell: open, wall or material index |
| `remaining[]` | per pod: unsent count |
| `columns[][]` | per column: pod list (copy of `tray`; mutable for Return and Shuffle) |
| `heads[]` | per column: index of the top pod |
| `slots[]` | per place: pod index or empty (5, or 6 after Extra Slot) |
| `ages[]` | per pod: commit order |
| `revealed[]` | per pod |
| `bridge` | chosen site or none |
| `status` | Playing, Won, Jammed, Stuck |
| `serial` | commit counter |

## RoundTimeline (runtime, rules only)

| Field | Meaning |
|---|---|
| `now` | model time, integer µs |
| `steps` | scheduled events `{type: arrive / clear / return / release / check, at, id? / pod?}`, kept sorted (research R3) |
| `jobs` | couriers `{id, pod, cell, start, arrive, clear, done, route[]}` |
| `reserved[]` | per cell: courier id or none |
| `inFlight[]` | per pod: couriers on the way |
| `departReady[]` | per pod: µs of its next allowed departure |
| `processedCells` | count |

Transitions:
- **commit(tap / bridge / booster, at)**: check legality, apply, reveal tops, dispatch.
- **advance(Δµs)**: process due steps in order; a `clear` frees its cell, a `return` ends its job, a `release` empties
  the group's slots; dispatch after each moment.
- **Status** while steps are pending: Playing (shown as working). With no steps: Won, Jammed or Stuck as in research R3.
- **Tail** (Auto) when every column is empty and no bridge is left to choose. It is a flag for presentation only.

Invariants, checked after every transition:
- cells of each material = unsent remaining + outbound reserved;
- flights match the jobs;
- no orphan reservations;
- every live pod is in exactly one place or column;
- no job remains when nothing is pending.

## Booster commands (live time, FR-012)

| Command | Allowed when | Effect |
|---|---|---|
| `extraSlot` | Playing, not used yet | capacity becomes 6 at once; the new slot is last |
| `return(pod)` | the pod is in a slot with remaining > 0 and no courier in flight; for a link, both members satisfy this | the pod goes to the top of its original column with its remaining count; its slot frees; its pending check step is dropped |
| `shuffle` | Playing, a column has 2+ pods | the column pods reorder (seeded); links stay paired at equal depth in different columns; hidden stay hidden; the patient solver must find a win, else no change |
| `bloomBurst(code)` | the code is revealed and still has cells | every unreserved cell of the code clears; the code's pods lose that many remaining (slots by age, then columns from the top); pods at 0 release normally |

Each is recorded with its µs time and replays deterministically. None is used in a proof.

## RoundProof

| Field | Meaning |
|---|---|
| `solution` | `{commands: [{type, pod? / choice?, atUs}], outcome: "Won", endUs}`; the patient replay of the reference: each command when all prior work has settled |
| `randomTest` | `{policy: "17§5 extended: uniform over legal actions + wait before a state event", seedBase, runs (100 / 400), wins, share, wilson95, classOfShare, borderline?, bridge?}`; `bridge`, on a bridge level from wave L21–L50 on, is the only bridge choice the test's bot may build (17 §5 as amended on 2026-10-30) |
| `checks` | `{opening, forced, hold, greedyLoses, hiddenMatters, bridgeKind, bridgeSingle, pressure}`, as computed by `tools/rounds/measure.mjs` |
| `audit` | `{result: "OK", report: "content/rounds/audit-L####-L####.md", notes[]}` (layer B notes included) |
| `engine` | fingerprints |

## ConformanceFixture

See `contracts/conformance-fixture.md`.
- **Solution windows**: per command, the hash of the canonical events and the state signature hash after it.
- **Random games**: per game, the outcome and the final signature hash.
- **Owner attempts**: commands with µs times, outcome, end time and final signature hash.

## TransferLogEntry (`content/game/rounds/transfers.jsonl`)

`{wave: [from, to], levels: [..], exceptions: [..], contentVersion, engine, exportedAt, validatedAt, apk, ownerConfirmedAt | null, note}`

States: exported → validated → published (APK built) → confirmed (owner checked the sample on the phone).

## Relationships

- RoundLevel 1 → 1 RoundPicture (`picture.id`, `version`).
- RoundLevel 1 → 1 RoundProof → 1 ConformanceFixture.
- A TransferLogEntry covers N RoundLevels and one content version.
