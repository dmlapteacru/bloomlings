# Data Model: Bloomlings Launch Game

**Feature**: `001-core-game-mvp` · **Spec**: [spec.md](spec.md) · **Research**: [research.md](research.md)

The model is split into three parts:

1. **Content**: static and versioned, produced by the pipeline and shipped.
2. **Runtime simulation state**: owned by the deterministic core.
3. **Player and meta data**: local save, cloud and backend.

The field lists are logical. Wire formats are in [`contracts/`](contracts/).

---

## 1. Content entities

### 1.1 TargetVariant

The exact gameplay matching type (FR-002, FR-003).

| Field | Type | Rules |
|---|---|---|
| `id` | string | Unique, lowercase: `leaf`, `moss`, `flower`, `violet_bud`, `water`, `dew`, `wood`, `acorn`; later `vine`, `berry`, `mist`, `bark` |
| `family` | enum `sprig\|bloom\|drop\|twig` | Character and animation family only; never used for matching |
| `colorGroup` | enum `green\|pink_purple\|blue_cyan\|brown_orange\|lime\|red\|indigo\|gold` | Used for role→variant mapping (FR-006) |
| `color` | hex string | Must pass the pairwise readability tests before two variants may share a level (FR-005) |
| `iconId` | string | Unique symbol; hue alone must never carry meaning (FR-072) |
| `introducedAtLevel` | int | From the unlock roadmap (FR-031, FR-060) |
| `status` | enum `launch\|expansion` | The 8 launch variants are `launch` |

A variant catalog of at least 12 entries must be supported without rule changes (FR-002).

### 1.2 BasePicture

A base picture of the picture library (FR-006, doc 06 §23). Wire format:
[`contracts/base-picture.schema.json`](contracts/base-picture.schema.json).

| Field | Type | Rules |
|---|---|---|
| `id`, `version` | string, int | Stable identity; a change creates a new version |
| `subject` | string | Human-readable name, e.g. "tulip in pot" |
| `width`, `height` | int | 7 ≤ width ≤ 14, 8 ≤ height ≤ 16 (FR-008) |
| `roles[]` | list of `{roleId, name, colorGroup, isBackground}` | At least 2 roles; each role has exactly one color group |
| `grid` | height × width of role index, `EMPTY` or `STONE` | Every non-empty cell has one role (FR-001) |
| `finishedLook` | `{mode: auto\|illustration, illustrationKey?}` | `auto` by default (FR-007) |
| `tags` | `{themes[], seasons[], bands[]}` | Used by generation profiles |
| `structure` | `{regionCount, nestingDepth, backgroundShare}` | Computed on import (R7) |
| `review` | `{status: draft\|approved\|rejected, reviewer, date, notes}` | Only `approved` pictures can be used (FR-084) |
| `source` | `{kind: hand\|generated\|generated_edited, origin, licence}` | Owned or licensed only (FR-091) |

### 1.3 LevelDefinition

One per level number (FR-075 to FR-077). Wire format:
[`contracts/level-definition.schema.json`](contracts/level-definition.schema.json).

| Field | Type | Rules |
|---|---|---|
| `levelNumber` | int ≥ 1 | Unique within a content version |
| `definitionVersion` | int ≥ 1 | Bumped only for a deliberate fix (FR-076) |
| `seed` | uint64 (string) | Generator seed; also salts the Shuffle PRNG (R3) |
| `generatorVersion` | string | Traceability |
| `picture` | `{id, version, mirror: none\|horizontal, backgroundTreatment}` | Must reference an `approved` picture |
| `mapping` | map of `roleId → variantId` | Each role maps to a variant of the same `colorGroup` that is allowed in the band. Several roles may share one variant (merge) |
| `entries[]` | list of `{x, y, side}` | At least 1 entry; default is bottom-center (FR-009) |
| `overlays.cells[]` | list of `{x, y, layersBelow[], mystery?, stone?, hole?, keyId?}` | Sparse. `layersBelow` holds the hidden variants under the top layer, top first: depth ≤ 2 before L125 and ≤ 3 after (FR-036). Keys sit on target cells only (FR-033) |
| `specials[]` | list of `{id, type: gate\|fountain\|chest\|statue\|bridge, cells[], condition, effect}` | Type must be unlocked for the level (FR-031). Condition and effect must be visible (FR-037, FR-038) |
| `locks[]` | list of `{keyId, target: {kind: pod\|slot\|special, id}}` | Exactly one lock per key and one key per lock (FR-033) |
| `slots` | `{count: 5, locked?: {slotIndex, keyId}}` | At most 1 locked slot, and only from L80 (FR-039) |
| `tray.stacks[]` | ordered pod ids, top first | 2–6 stacks (tuned per band) |
| `pods[]` | list of `{id, variantId, count, mystery?, lockKeyId?, connectedGroupId?}` | `count ≥ 1`. Connected groups have 2 members (3 only if that mechanic is unlocked). Members of a connected group sit at the **same depth** in different stacks |
| `difficulty` | `{class: normal\|hard\|super_hard, score, overridden}` | FR-082. `score` is an integer fixed-point value (× 1000) |
| `rewardProfile` | string | Links to an economy config entry |
| `mechanics[]` | list of strings | Derived; used by unlock validation |

**Derived board.** The board is derived at load time as follows (R5):

1. `cell(x, y)` starts as `grid[y][x']`, where `x' = mirror ? width-1-x : x`.
2. The top-layer variant is `mapping[role]`.
3. `layersBelow`, `stone`, `hole`, `mystery` and `keyId` overlays are applied.

**Validation rules** (all enforced by the pipeline, FR-080):

- **Exact accounting** (FR-023): for every variant `v`, the sum of `count` over pods of `v` equals the number of top
  layers of `v` plus the number of hidden layers of `v`. Mystery pods and tiles count toward their fixed hidden
  variant.
- **Top layer follows the picture**: each non-stone, non-hole top layer equals `mapping[role]` (FR-006).
- **Active variants**: the count of distinct variants is within the band range (FR-004), and every pair passes the
  readability tests (FR-005).
- **Reachability**: every mandatory layer, key and special can become reachable (FR-080).
- **Unlocks**: no mechanic and no variant appears before its unlock level (FR-031).
- **Solver**: the level is solvable without boosters and has a jam witness unless it is a tutorial (FR-080, FR-081).
- **Similarity**: FR-083 holds against the catalog.

### 1.4 ValidationRecord (pipeline only, not shipped)

| Field | Type |
|---|---|
| `levelNumber`, `definitionVersion`, `definitionHash` | identity |
| `solverVersion`, `nodeBudget`, `nodesUsed` | reproducibility |
| `result` | `solvable \| unsolvable \| unknown` |
| `solutionTrace[]` | commands |
| `jamWitness[]` | commands, or null for tutorials |
| `playerInfoFair` | bool, or null if the level has no mystery |
| `metrics` | depth, branching, unsafe-choice density, dead-end depth, peak/mean buffer, connected commitments, variant load, special load, total work, estimated duration |
| `checks[]` | the list of passed invariant ids |

### 1.5 GenerationProfile

One profile per progression band (FR-079, doc 06 §4). It defines:

- `bandId` and `levelRange`;
- `boardSize` (min and max);
- `picturePool` (tags) and `structureTargets`;
- `variantCount` (min and max) and `allowedVariants`;
- `mappingConstraints`;
- `entryLayouts`;
- `maxLayerDepth`;
- `allowedMechanics`;
- `stacks` (min and max) and `podSizes` (min and max per size class);
- `bufferPressureTarget` (relaxed, normal, tense or critical);
- `difficultyTarget` (class and score range);
- `durationTarget`;
- `hardMode`;
- `milestoneConstraints`.

### 1.6 Catalog, UnlockRoadmap and Milestones

- **ContentManifest** ([schema](contracts/content-manifest.schema.json)): `contentVersion`, `minAppVersion`,
  `pictureLibraryVersion`, `shuffleNodeBudget` (fixed per content version, R10), and `packs[]`. Each pack has
  `{id, kind: levels|pictures|daily, levelRange?, path|url, sha256, bytes}`.
- **UnlockEntry**: `{level, unlockId, kind: system|booster|mechanic|variant|profile, demoWithin: 0–2}`. The source is
  the spec's *Unlock Roadmap*.
- **Milestone**: `{level, rewardBundleId, prestige?: frame|skin|badge|marker}` (FR-061).
- **DailyPoolEntry**: `{index, levelDefinition}`, where the index is selected by UTC date (R19).

---

## 2. Runtime simulation state (deterministic core)

### 2.1 LevelState

| Field | Notes |
|---|---|
| `definition` | Immutable reference |
| `cells[]` | Per cell: `open: bool`, `layers: stack of variantId` (top = current), `mysteryHidden: bool`, `stone`, `keyId?`, `specialId?` |
| `entries[]` | From the definition |
| `slots[]` | Per slot: `state: free\|occupied\|locked\|absent`, `podId?`, `ageOrder` (monotonic counter at commit time) |
| `stacks[]` | Current tray stacks (pod ids, top first) |
| `pods{}` | Per pod: `remaining`, `revealedVariant?`, `locked: bool`, `location: tray\|slot\|done\|removed` |
| `keysCollected` | Set of key ids (sorted list) |
| `specials{}` | Counters, open flags |
| `extraSlotUsed` | bool (FR-043) |
| `shuffleUses` | int (PRNG salt, R10) |
| `boostersUsedThisAttempt` | For the clean-clear bonus (FR-041) |
| `status` | `playing \| won \| jammed \| stuck` |
| `commandLog[]` | For replays, analytics and support |
| `stateHash` | Zobrist hash, updated incrementally |

### 2.2 Commands and events

The full contract is in [`contracts/simulation-api.md`](contracts/simulation-api.md).

- **Commands**:
  - `TapPod(podId)`
  - `UseExtraSlot`
  - `UseShuffle`
  - `UseReturn(slotIndex)`
  - `UseBloomBurst(variantId)`
  - `Restart`
- **Events** are ordered, and each one carries its round index:
  - `PodCommitted`, `MysteryPodRevealed`
  - `TileCleared(cell, podId, round, routeFromEntry)`
  - `LayerRevealed`, `CellOpened`, `MysteryTileRevealed`
  - `KeyCollected`, `LockOpened`, `SpecialProgressed`, `SpecialTriggered`
  - `PodCompleted`, `SlotFreed`
  - `ExtraSlotAdded`, `TrayShuffled`, `PodReturned`, `VariantBurst`
  - `LevelWon`, `LevelJammed`, `LevelStuck`

### 2.3 State transitions

**Spirit Pod**

```text
Buried ──(pods above leave)──▶ Exposed ──TapPod──▶ InSlot{Active|Stuck} ──remaining=0──▶ Done
   │                              │  ▲                    │
   │                              │  └──── UseReturn ─────┘  (back to top of original stack, remaining kept)
   │                              └── Locked (lockKeyId, key not collected): TapPod refused
   └── UseShuffle may reorder Buried/Exposed eligible pods
Any (tray or slot) ──UseBloomBurst(variant)──▶ Removed
Mystery: variant hidden until TapPod → revealed (fixed in data)
```

`Active` means the pod has candidates. `Stuck` means `remaining > 0` with no reachable tile of its variant.

**Tile layer / cell**

```text
Visible&Unreachable ──route opens──▶ Reachable ──claimed in round──▶ Cleared
Cleared ──layersBelow non-empty──▶ next layer Visible (may be immediately Reachable)
Cleared ──last layer──▶ Open (walkable; shows the finished picture)
Mystery tile: variant hidden while Unreachable → revealed when Reachable
Stone: never changes; Specials: Closed ──condition met──▶ Triggered (effect applied)
```

**Waiting slot**

```text
Free ⇄ Occupied (commit / pod done or returned)
Locked ──key collected──▶ Free
Absent (6th) ──UseExtraSlot──▶ Free   (once per level)
```

**Level**

```text
Loading → Playing ─┬─ fixpoint & all required work done ─▶ Won ──▶ Reward → Next
                   ├─ fixpoint & jam condition ─────────▶ Jammed ─┬─ booster / ad rescue ─▶ Playing
                   │                                               └─ Restart ─▶ Playing (same definition)
                   └─ fixpoint & stuck condition ───────▶ Stuck (same options as Jammed)
Won takes precedence when conditions coincide.
```

---

## 3. Player and meta data

### 3.1 PlayerSave

Wire format: [`contracts/player-save.schema.json`](contracts/player-save.schema.json) (FR-087, FR-088).

| Section | Fields |
|---|---|
| Identity | `schemaVersion`, `localPlayerId`, `linkedIdentity?` (`apple\|google_play_games`), `deviceId`, `updatedAt` |
| Progression | `highestCompletedLevel`, `currentLevel` (= highest + 1), `contentVersionSeen` |
| Wallet | `petals` |
| Purchases | `ledger[]` of `{transactionId, productId, grantedAt, grants{petals, boosters}}`, `entitlements{removeAds}`, `starterPackOffered` |
| Boosters | `{extraSlot, shuffle, return, bloomBurst}` charges |
| Unlocks | Flags keyed by `unlockId`, plus `demosSeen[]` |
| Milestones | `claimed[]` of level numbers (each granted exactly once, FR-061) |
| Cosmetics | `owned[]`, `equipped{family → skinId}` |
| Daily | `dailyReward{lastClaimUtcDate, streak}`, `dailyChallenge{lastCompletedUtcDate}` |
| Collection | `entries[]` of `{pictureId, pictureVersion, mappingHash, levelNumber}` (FR-065) |
| Settings | `music`, `sfx`, `haptics`, `speed2x`, `language` |
| Stats | `levelsWon`, `jams`, `boostersUsed{}`, `adsWatched`, `firstSessionMaxLevel` |

**Rules:**

- Petals and charges are never negative.
- `claimed` and `ledger.transactionId` are unique.
- Merging follows R15.

### 3.2 LeaderboardEntry

`{playerId, displayName, highestCompletedLevel, reachedAtUtc, score}`, with
`score = level × 10⁷ + (10⁷ − 1 − minutesSince(2026-01-01))` (R11, FR-062).

### 3.3 Remote configuration

The keys and defaults are listed in [`contracts/backend-services.md`](contracts/backend-services.md). They cover:

- the economy: rewards, bonuses, prices and grants;
- the ad policy: caps and the first eligible level;
- feature flags: `dailyChallenge`, `wardrobe` and others;
- the content manifest URL;
- the animation backlog threshold.
