# Feature Specification: Bloomlings Launch Game (Colony Flow–style buffer puzzle)

**Feature Branch**: `001-core-game-mvp`

**Created**: 2026-09-29

**Status**: Draft

**Input**: User description: "мы будем делать концептуальную копию игры Colony Flow. в репозитории есть направление игры в папке "product". это скорее не правила которым надо следовать бесприкословно, а потенциальное направление , чтоб как-то отличить нас от Colony FLow. Но мы хотим сделать еще однку такую же игру, концепт геймлпея, идея, должна сохраниться, простота, развитие игры, уровней, лэйауты должны быть схожи своей простотой и удобством."

## Overview

Bloomlings is a conceptual re-creation of **Colony Flow!** (ABI Games, iOS/Android). Colony Flow is the
structural reference. From it Bloomlings takes:

- a dense board that looks like a picture;
- numbered source groups in a stacked tray;
- five waiting slots;
- workers that match automatically;
- partial completion;
- the risk of a jam;
- no timer;
- sequential "Level N" progression;
- a minimal Home screen and a Play → Level → Next flow.

**What makes Bloomlings different**:

- Four Bloomling character families (Sprig, Bloom, Drop, Twig) replace the ants.
- Each family contains several *exact target variants*, which are the real matching colors.
- Clearing the board reveals the finished picture underneath.
- Bloomlings-specific mechanics: layered tiles, the Fountain and garden gates.
- Every level can be won without boosters.

**Source documents**:

- `product/LOCKED_CONCEPT_v0.5.md` and `product/01`–`15` (v0.5, "draft for lock") are the detailed design.
- `product/CONCEPT.md` (v0.1) is the original vision.

This spec turns them into testable requirements. Where the documents leave a decision open, the spec either records
a default or asks a question.

| Aspect | Colony Flow (reference) | Bloomlings |
|---|---|---|
| Workers | Ants from a nest hole | Bloomlings of 4 families that emerge from the Garden Entry |
| Matching colors | Many picture colors | 8 exact target variants at launch (2 per family); 12+ supported |
| Board | Pixel-art picture that is cleared away | A small garden picture drawn with target tiles; clearing reveals its finished version beneath |
| Core loop | Tray → 5 slots → workers clear reachable matching cells → jam = fail | Same |
| Boosters | Extra Slot (L3), Shuffle (L4), Pick Up (L6), Vacuum (L9) | Extra Slot (L3), Shuffle (L4), Return (L6), Bloom Burst (L9) |
| Progression | Linear levels | Linear Level N (5000+ at launch); no map, no level groupings |
| Hard levels | Hard / Super Hard; players report that some need boosters | Hard (from L5) / Super Hard (from L10); none need boosters |
| Meta | Collection of finished pictures | Milestones, Leaderboard (L10), Wardrobe (L40), Daily Challenge (L50), Collection |

## Clarifications

### Session 2026-09-29

- **Q: Should the release keep only four cell types, or expand the palette?**
  A: Families ≠ gameplay types. There are 4 Bloomling families and 8 launch target variants (2 per family). The
  model supports 12+ variants, and a level typically uses 3–6. Matching is exact: a Leaf Sprig pod cannot clear
  Moss. See docs 01, 03, 05, 06, 12 and 14.
- **Q: What is the scope of the first release?**
  A: Option B. It is the full launch game including the free-to-play layer: store, ads, in-app purchases and daily
  rewards. There are no lives; see the lives question below.
- **Q: Are levels grouped?**
  A: No. There are no garden areas and no level map, only sequential levels as in Colony Flow.
- **Q: How many levels, and how are they produced?**
  A: 5000+ levels. We generate them, but each level must be identical for all players. A level may be stored as a
  compact config from which the game builds the cells, as in Colony Flow.
- **Q: What is a level visually?**
  A: As in Colony Flow, every level is a picture.
- **Q: What exactly makes a level a picture?**
  A: Option A, a picture-first mosaic.
  - Each level is a small picture of a garden-world subject, and the tile colors themselves draw it: every cell's
    visible tile is the exact variant that matches that pixel's color role.
  - The finished version of the same picture lies beneath the tiles. It is revealed as the board clears and is then
    saved to the Collection.
  - One base picture can produce several levels through a different role-to-variant mapping, mirroring, background
    and Source design.
  - The generator derives the dependencies from the picture and designs the pods and the tray solution-first.

  See FR-006, FR-007, FR-079 and FR-083.
- **Q: Are there lives?**
  A: Option A, no lives, as in doc 10. A failed or abandoned attempt is free, restarts are unlimited, and there is no
  energy timer. See FR-040.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Play a level: commit pods, restore the picture, avoid the jam (Priority: P1)

A player starts a level and sees, from top to bottom:

- a top bar;
- a dense board that is a small picture of a garden subject (for example, a flower in a pot), drawn with target tiles in 3–6 exact variants;
- the Garden Entry and five Waiting Slots below the board;
- the stacked Source Tray, whose exposed pods show a variant icon, a color and a count.

The player taps an exposed Spirit Pod. It moves into the first free slot, and Bloomlings of its family and variant
run out of the Garden Entry. They clear reachable tiles of that exact variant, one work unit per tile-layer, while
the pod's count goes down. Clearing tiles opens routes to deeper tiles and reveals the finished picture beneath.

When a pod's count reaches zero, it leaves and its slot frees up. The player wins when every required tile-layer is
cleared. The player loses when every usable slot holds a pod that cannot reach its variant and nothing else can
change.

**Why this priority**: This loop is the game. It alone is enough to test clarity, fun and fairness.

**Independent Test**: Play a curated level with 4 variants, including two from the same family, from start to win.
Replay it in a deliberately bad order until it jams.

**Acceptance Scenarios**:

1. **Given** a level has started, **When** the player taps an exposed pod while a usable slot is free, **Then** the pod moves into the first free slot. Its Bloomlings clear reachable tiles of its exact variant, and the count drops by one per cleared tile-layer.
2. **Given** a Leaf Sprig pod is active and a Moss tile is reachable, **Then** the Moss tile is not cleared, because the family is not a wildcard.
3. **Given** a `Moss Sprig ×20` pod with only 12 reachable Moss tiles, **When** those 12 are cleared, **Then** the pod shows `×8` and stays in its slot. It resumes on its own when more Moss becomes reachable.
4. **Given** two active pods of the same exact variant, **Then** the pod in the older slot receives tiles first.
5. **Given** a pod's count reaches zero, **Then** it leaves and its slot is free at once for the next tap.
6. **Given** every usable slot is occupied, **When** the player taps a pod, **Then** the tap is refused with visible feedback and nothing changes.
7. **Given** every usable slot is occupied but at least one pod is still clearing tiles, **Then** there is no jam.
8. **Given** every usable slot is occupied, every pod has zero reachable matching tiles, and no automatic event is pending, **Then** the Jam screen appears with the board still visible. It offers the eligible recovery options and Restart.
9. **Given** the last required tile-layer is cleared and all mandatory specials are resolved, **Then** the finished picture is fully revealed, the reward is shown, and "Next" leads to the next level.
10. **Given** the player restarts a level, **Then** it restarts from exactly the same definition: same board, same tray and same pods.

---

### User Story 2 - Endless linear progression: Home → Play → Level N → Next (Priority: P1)

A new player launches the game and goes straight into Level 1. After that, the Home screen shows:

- the current Level N;
- a big Play button;
- the player's Petals;
- a teaser for the next milestone.

Winning always leads to the next level. There is no map and no level chooser. Early levels unlock new systems
quickly: boosters at L3, L4, L6 and L9, the Hard label at L5, Daily Reward at L7, and Leaderboard plus Super Hard at
L10. Difficulty rises and falls in waves. Level N is the same level for every player on every device.

**Why this priority**: Sequential Level N progression is the product's backbone, just as in the reference game.

**Independent Test**: A new player completes Levels 1–10 without outside help. Along the way they meet every unlock
in the roadmap. They restart the app and continue from Level 11. A second device shows the same Level 11 board.

**Acceptance Scenarios**:

1. **Given** the game is launched for the first time, **When** it finishes loading, **Then** Level 1 starts directly, with no sign-in and no menus, and the first tap is guided.
2. **Given** the player wins Level N, **When** they tap "Next", **Then** Level N+1 starts. The Home screen always shows the current Level N with one Play button.
3. **Given** two players on different devices reach Level N, **Then** both play the identical level: same picture, board, tray, pods and mechanics.
4. **Given** the player reaches a level listed in the unlock roadmap, **Then** the new system is demonstrated at that level or within the next 1–2 levels. A booster unlock grants one free charge.
5. **Given** the next level is Hard or Super Hard, **When** it is about to start, **Then** its label is shown with a distinct visual treatment.
6. **Given** the player closes the app at any moment, **When** they reopen it, **Then** their progress, Petals, boosters, unlocks, cosmetics and settings are restored.

---

### User Story 3 - 5000+ deterministic levels built from a validated catalog (Priority: P2)

The content team produces the launch catalog of at least 5000 sequential levels:

- Levels 1–100 are heavily curated.
- Levels 101–500 are generated and strongly reviewed.
- Levels 501–5000+ are generated from progression profiles.

Every level is built from a base picture in the picture library. It is generated solution-first, checked by the
solver, scored and classified as Normal, Hard or Super Hard. It is then published as a stable, versioned level
definition. The game builds each level from its definition. The
same definition always produces the same level.

**Why this priority**: Without a validated catalog, the product cannot sustain long-run players. The level scale is
a launch requirement.

**Independent Test**:

1. Generate a batch of levels for one progression band.
2. Run the validation suite and check that invalid candidates are rejected.
3. Publish the batch.
4. On two devices, check that the published levels build identically.
5. Change the content version and check that already-shipped levels do not change silently.

**Acceptance Scenarios**:

1. **Given** a generation profile for a level band, **When** a batch is generated, **Then** every accepted level passes all invariants and every rejected candidate records the reason. The invariants are: solvable without boosters, exact per-variant accounting, no mechanic before its unlock level, no unavoidable hidden-information failure, and a readable palette.
2. **Given** a published catalog, **When** the app or the content is updated, **Then** no shipped level number silently gets a different level unless the change is a deliberate, versioned content fix.
3. **Given** a bug report with app version, level number and content version, **Then** the team can rebuild and replay exactly that level.
4. **Given** the launch catalog, **Then** all of its 5000+ levels are playable offline right after install.
5. **Given** a new content pack is published, **Then** players receive the new or fixed levels without installing a new app version.
6. **Given** a base picture from the library, **When** a level is generated from it, **Then** the visible top layer of every cell follows the picture's color roles mapped to exact variants. The subject is recognizable at level start, and the finished picture is available for the reveal and the Collection.
7. **Given** a base picture that was already used, **When** it is used again, **Then** the new level differs in role-to-variant mapping or mirroring and in Source design, and it comes at least 50 levels after the previous use.

---

### User Story 4 - Mechanics expand on a planned roadmap (Priority: P3)

As Level N grows, new board and source mechanics appear one at a time. Each follows the same pattern: showcase,
then practice, then combination. The main ones are:

- stones;
- keys and locked pods;
- connected pods;
- layered tiles;
- garden gates (heavy blockers);
- the Fountain;
- a locked waiting slot;
- mystery pods and mystery tiles, only if their fairness validation passes.

After about Level 500, new core mechanics become rare. Variety then comes from pictures, variant sets, source layouts
and difficulty profiles.

**Why this priority**: The core loop is complete without these mechanics. They provide long-run depth and variety.

**Independent Test**: For each mechanic, play its showcase level and one combination level. Check every acceptance
scenario below.

**Acceptance Scenarios**:

1. **Given** a key lies on a target tile, **When** that tile's supporting layer is cleared, **Then** the key is collected without costing extra work, and its paired lock opens. The lock can be on a pod, a slot or a gate.
2. **Given** a locked pod is exposed, **When** the player taps it, **Then** it is not committed and the game highlights the key it needs. Pods behind it stay buried until it leaves.
3. **Given** connected pods are exposed and there are enough free usable slots, **When** either is tapped, **Then** all of them are committed together, each to its own slot. **Given** there are not enough free usable slots, **Then** the tap is refused with feedback.
4. **Given** a layered tile `Leaf → Violet Bud`, **When** its Leaf layer is cleared, **Then** Violet Bud appears in the same cell and an active Violet pod starts on it automatically.
5. **Given** a Fountain requires, for example, 6 Water tiles around it to be restored, **When** the condition is met, **Then** a visible board change happens, such as a route opening or a layer being revealed.
6. **Given** a level with a locked waiting slot, **Then** only four slots are usable until its key is collected.
7. **Given** a mystery pod (`? ×12`) is committed, **Then** its exact variant is revealed. The variant is fixed in the level data, and the level never forces a blind guess.
8. **Given** a mystery tile becomes reachable, **Then** its exact variant is revealed.

---

### User Story 5 - Boosters and Petals help recover from mistakes (Priority: P3)

Four boosters unlock during onboarding, each with one free charge:

- **Extra Slot** (L3) adds a sixth slot for the level.
- **Shuffle** (L4) rearranges the Source Tray.
- **Return** (L6) sends a waiting pod back to the Source Tray.
- **Bloom Burst** (L9) clears one exact variant.

Petals are earned from wins and milestones and are spent on boosters. When a level jams, the player can use an
eligible booster or a limited rewarded-ad rescue, or restart.

**Why this priority**: Boosters turn tactical mistakes into recoverable moments and drive the economy. No level
requires them.

**Independent Test**: Reach L9 and use each booster's free charge in a level. Earn Petals and buy a booster. Jam a
level and recover it with Extra Slot and then with the ad rescue.

**Acceptance Scenarios**:

1. **Given** the player uses Extra Slot, **Then** a sixth usable slot appears until the level ends. No second Extra Slot can be active in the same level.
2. **Given** the player uses Shuffle, **Then** only the remaining eligible Source Pods are rearranged; waiting pods are unaffected, locks stay attached to their pods, and connected pods stay connected. If any arrangement can still be won, the result can be won.
3. **Given** the player uses Return on an unfinished waiting pod, **Then** its remaining count goes back to the top of its original Source stack, and the slot frees up. Tiles already cleared stay cleared.
4. **Given** the player uses Bloom Burst on a visible exact variant, **Then** that variant is removed from the level consistently (see FR-050), and per-variant accounting still reconciles.
5. **Given** a jam, **When** the player watches a rewarded ad for a rescue, **Then** they receive the rescue effect. The rescue is available at most once per attempt.
6. **Given** a booster could have no effect in the current state, **Then** its button is disabled.
7. **Given** the player wins without boosters, **Then** they earn the clean-clear bonus on top of the base Petals.

---

### User Story 6 - Store, ads and daily rewards (Priority: P4)

- **Daily Reward** unlocks at L7: the player claims a small reward once a day.
- **The full Store** opens at L12. It sells Petal packs, boosters, Remove Ads and an optional starter pack. Cosmetics
  join after the Wardrobe unlock.
- **Rewarded ads** are always optional and started by the player. They offer a jam rescue, a free booster, a doubled
  win reward or a daily bonus.
- **Interstitial ads** appear only at natural post-win transitions, outside onboarding, with a frequency cap. Remove
  Ads turns them off.
- **Purchases** can be restored on a new device.

**Why this priority**: This is the business layer the user chose for launch (answer B). It must never block or break
the puzzle.

**Independent Test**:

1. Play through L12.
2. Claim the daily reward on two different days.
3. Buy Remove Ads and check that no interstitial appears afterwards.
4. Reinstall on another device and restore the purchase.
5. Play 30 levels without Remove Ads and check that interstitials follow the placement and frequency rules.

**Acceptance Scenarios**:

1. **Given** a level is in progress or the player has just failed, **Then** no interstitial ad is ever shown.
2. **Given** the player owns Remove Ads, **Then** interstitials never appear, while optional rewarded ads stay available.
3. **Given** the player reinstalls or changes device, **When** they restore purchases, **Then** permanent entitlements such as Remove Ads return.
4. **Given** the player is offline, **Then** gameplay, progression and owned boosters work, while store purchases and ads are unavailable and clearly marked as such.
5. **Given** any level, **Then** it can be completed without spending money or watching ads.

---

### User Story 7 - Long-run motivation: leaderboard, milestones, cosmetics, collection (Priority: P5)

- **Leaderboard** (L10): the player sees their rank by highest completed level.
- **Milestones**: every 25 levels bring a bundle, every 50 a cosmetic or profile reward, every 100 a major
  milestone, and larger prestige rewards come at 250, 500, 1000 and so on.
- **Wardrobe** (L40): cosmetic skins for Bloomlings.
- **Daily Challenge** (L50, may be cut from launch): one optional puzzle a day with its own reward.
- **Collection**: every finished picture is stored and can be viewed.
- **Background themes** rotate automatically by level band, for example daylight garden, pond, orchard and moonlit
  garden.

**Why this priority**: These features keep players at Level 1000–5000 motivated without changing the core rules.

**Independent Test**: Reach L50 and check each unlock. Check the leaderboard rank after completing levels. Equip a
skin. Complete the daily challenge. Open the Collection.

**Acceptance Scenarios**:

1. **Given** the player completes a level after L10, **Then** the leaderboard reflects their new highest completed level once online.
2. **Given** the player reaches a milestone level such as 25, 50 or 100, **Then** its reward is granted once, with a short celebration.
3. **Given** the player equips a skin, **Then** Bloomlings look different but gameplay and tile readability are unchanged.
4. **Given** the player completes the daily challenge, **Then** they get its reward and their main Level N is unchanged.
5. **Given** the player wins a level, **Then** its finished picture appears in the Collection. The Collection is never a level selector.

---

### Edge Cases

- **Fast taps while Bloomlings are moving**: every accepted tap is applied in order against the current logical state. The same sequence of accepted taps always produces the same outcome.
- **A pod is committed while its variant has no reachable tiles**: this is allowed. The pod waits; this is the core risk the player manages.
- **The last free slot is filled while routes are still opening**: a jam is checked only once no automatic event is pending.
- **Two variants of the same family are in the buffer together**: they stay visually distinct, and each responds only to its own tiles.
- **A layered tile reveals a variant for which a pod is waiting**: that pod starts automatically.
- **A key is collected while its locked pod is still buried**: the pod is unlocked and becomes selectable once exposed.
- **No waiting pod can progress, and no Source Pod can be committed** (for example, only locked pods remain, or connected pods need more slots than are free), **while required work remains**: this is treated as a jam, with the same recovery options.
- **Win and jam conditions are met at the same moment**: the win takes precedence.
- **The app goes to the background or is interrupted during a level**: the level pauses and resumes exactly where it was. If the app is killed, the level restarts from its beginning.
- **A content update changes the level the player is currently on** (a deliberate fix): the player's level number is kept, and the new version applies from the next attempt.
- **Offline at a jam**: ad rescue and store purchases are unavailable. Owned boosters and Restart still work.
- **Bloom Burst is used on a variant that also exists in hidden layers**: see FR-050. Accounting must still reconcile, and the level must stay completable.
- **Leaderboard submission from a modified client** that jumps levels impossibly or uses an incompatible content version: the submission is rejected by sanity checks.
- **A generated candidate reaches the solver with an ambiguous palette** (two confusing variants in one board): it is rejected.
- **Layered tiles, keys or specials inside the picture**: only the visible top layer must follow the picture. Hidden layers, keys and specials may deviate, as long as the subject is still recognizable at level start.
- **A picture needs a color that no allowed variant can express in its band**: the role is mapped to the closest allowed variant in the color language, or the picture is not used in that band.

## Requirements *(mandatory)*

### Functional Requirements

#### A. Board, target variants and pictures (docs 01, 03, 05, 12)

- **FR-001**: The board MUST be a dense grid of full cells. Each cell MUST be exactly one thing:
  - open (restored) ground;
  - a target tile of one exact variant, possibly with layers beneath;
  - a mystery tile;
  - a blocker (stone, heavy blocker or gate);
  - part of an environmental object (for example, the Fountain).

  A key may lie on a target tile. No cell may look partially occupied.
- **FR-002**: The four Bloomling families (Sprig, Bloom, Drop, Twig) MUST be character families only. The gameplay type MUST be the exact target variant. The launch pool MUST contain these 8 variants:

  | Family | Variants |
  |---|---|
  | Sprig | Leaf (green), Moss (teal) |
  | Bloom | Flower (pink), Violet Bud (purple) |
  | Drop | Water (blue), Dew (cyan) |
  | Twig | Wood (brown), Acorn (orange) |

  The game MUST support at least 12 variants without new rules. Examples of future variants: Vine (lime), Berry (red), Mist (indigo), Bark (gold).
- **FR-003**: A pod MUST clear only tiles of its exact target variant. A family is never a wildcard.
- **FR-004**: Active variants per level MUST follow these ranges:

  | Level type | Active variants |
  |---|---|
  | Tutorial | 2–3 |
  | Early | 3–4 |
  | Standard | 4–5 |
  | Advanced | 5–6 |
  | Exceptional | 7, only if readability checks pass |
- **FR-005**: Each variant MUST be identified by at least hue and icon. Tiles MUST show simple target symbols, never character faces. Two variants of the same family MUST be as easy to tell apart as two unrelated colors. No pair of variants may appear together in a level until it has passed the readability tests: grayscale/icon, small size, color distance, pod, slot and moving character.
- **FR-006**: Every level MUST be a picture, built as a picture-first mosaic:
  - The level comes from a **base picture**: a small image of a garden-world subject, drawn at board resolution (at most 14×16 cells). The image uses abstract color roles such as petal, leaf, stem, pot or background. Example subjects: flowers, fruit, insects, small animals, garden tools, cozy objects, seasonal motifs.
  - The level definition MUST map each color role to one exact target variant that fits the color language. For example, leaves map to Leaf or Moss; petals to Flower, Violet Bud or Acorn; a pot to Wood; the background to Water or Dew.
  - The visible top layer of every cell MUST follow this mapping, so that the board reads as the subject from the first second.
  - Stones, empty holes and background regions MAY be part of the picture, as long as all mandatory content stays reachable.
  - Hidden layers, keys and specials MAY deviate from the picture, as long as the subject is still recognizable at level start.
  - A base picture MAY be reused in several levels with a different role-to-variant mapping, mirroring, background and Source design, within the limits of FR-083.
- **FR-007**: Each cleared cell MUST reveal the matching part of the level's finished picture beneath it (restoration reveal). The finished picture is the same subject in its restored look: clean, bright art without tile symbols. It MAY be rendered automatically from the base picture; bespoke illustrations are optional, for example for milestones. Open cells MUST stay visually distinct from active target tiles. On a win, the finished picture MUST be shown in full.
- **FR-008**: Board size MUST range from 7×8 cells in tutorials to at most 14×16 cells. The whole board MUST be visible without scrolling or zooming. The initial occupancy inside the picture's playable area MUST be between 75% and 95%.
- **FR-009**: Each level MUST have at least one Garden Entry, by default at the bottom center. Some levels MAY use two entries or a side entry.
- **FR-010**: A target MUST count as reachable only when an orthogonally connected route of open cells leads from a Garden Entry to a side of that target. Diagonal contact does not count. Blockers are never walkable.

#### B. Source Tray and Waiting Buffer (doc 02)

- **FR-011**: The Source Tray MUST consist of stacks of Spirit Pods. Only the exposed pod of each stack is selectable, and removing it exposes the next one. The number of stacks varies by level.
- **FR-012**: Each pod MUST show, in this order of prominence: exact variant icon, exact variant color, count, and family silhouette. It MUST also show any state: connected, locked or mystery.
- **FR-013**: Visibility MUST follow these rules:

  | Pod type | What is visible |
  |---|---|
  | Ordinary | Variant and count |
  | Locked | Variant, count and lock |
  | Mystery | `?` and count |
  | Connected | A visible link across all members |
- **FR-014**: Tapping an exposed, selectable pod MUST move it to the first free usable slot. If no usable slot is free, the tap MUST be refused with feedback and no state change.
- **FR-015**: The Waiting Buffer MUST have exactly 5 slots by default. Slots show the pod's variant, remaining count and waiting/active state. Pods in slots cannot be reordered manually.
- **FR-016**: The player MUST be able to commit more pods while Bloomlings are working. The player MUST never tap target cells.

#### C. Work rules (doc 01)

- **FR-017**: One work unit MUST clear exactly one visible tile-layer of the pod's exact variant and lower the pod's count by 1.
- **FR-018**: Active pods MUST work automatically and at the same time on reachable tiles of their exact variants.
- **FR-019**: When fewer matching tiles are reachable than the pod's count, the pod MUST clear what it can, stay in its slot, and resume automatically when more tiles become reachable.
- **FR-020**: When several active pods share an exact variant, the oldest slot MUST get priority. Pods of different variants of the same family are independent.
- **FR-021**: When more matching tiles are reachable than a pod needs, the tiles MUST be chosen by a fixed rule that players can anticipate: nearest to a Garden Entry by route distance first, then a fixed tie-break order.
- **FR-022**: A pod MUST leave only when its count reaches 0. Its slot MUST become free at once.
- **FR-023**: For each exact variant, the pod counts MUST add up to the visible plus hidden tile-layers of that variant. Accounting is never per family.
- **FR-024**: The same level definition and the same sequence of accepted taps and booster uses MUST always give the same logical outcome. The outcome MUST never depend on animation timing, the 2× speed setting or the device. Mystery values are fixed in the level data.

#### D. Win, jam and restart (docs 01, 09, 11)

- **FR-025**: A level MUST be won when all required target layers are cleared and all mandatory specials are resolved. The win sequence is: reveal of the finished picture → reward → Next.
- **FR-026**: A level MUST jam only when all of the following hold:
  - the level is incomplete;
  - every usable slot is occupied;
  - every active pod has zero reachable matching tiles;
  - no automatic event is pending.

  A full buffer in which any pod is still progressing is not a jam. The situation where no pod can progress and no Source Pod can be committed MUST be treated as a jam.
- **FR-027**: The Jam screen MUST keep the board visible. It MUST offer eligible boosters, a rewarded-ad rescue limited to one per attempt, and Restart. It MUST never force the Store screen.
- **FR-028**: Restart MUST rebuild the level from the same definition.
- **FR-029**: There MUST be no timer and no move limit.
- **FR-030**: The player MUST be able to pause, restart or leave a level at any time without penalty (FR-040).

#### E. Mechanics and unlock roadmap (docs 03, 04, 07, 13)

- **FR-031**: Systems, boosters, mechanics and new variants MUST unlock at the levels in the *Unlock Roadmap* below. A level MUST NOT contain a mechanic before its unlock level. Two major mechanics MUST NOT unlock at the same level. Each new system MUST be demonstrated at its unlock level or within 1–2 levels. Each new mechanic MUST follow showcase → practice → combination. A new variant needs only one clean level, then one mixed level.
- **FR-032**: **Stone**: MUST be permanent, not clearable and not walkable.
- **FR-033**: **Key**:
  - A key MUST lie over a target tile without hiding its icon or color.
  - It MUST be collected when that supporting layer is cleared, at no extra work.
  - It MUST resolve exactly one paired lock: a locked pod, a locked slot or a gate.
- **FR-034**: **Locked pod**: MUST stay unselectable until its key is collected, and MAY bury the pods behind it.
- **FR-035**: **Connected pods**:
  - Tapping any member MUST commit all members together, each into its own slot.
  - If there are not enough free usable slots, the tap MUST be refused.
  - Members MAY mix families and variants.
  - After placement, each member behaves independently.
  - Pairs unlock first; triples are an optional late candidate.
- **FR-036**: **Layered tile**:
  - It shows its current variant plus a clear indicator of the next layer's variant.
  - Clearing the top layer reveals the next variant, which may belong to another family, and MAY wake an active pod.
  - Allowed depth is 2 at first and 3 later; deeper stacks are exceptional only.
  - Every layer counts in per-variant demand.
- **FR-037**: **Garden Gate / heavy blocker**: MUST block a route until a clearly visible condition or counter is met. It then opens, making cells walkable or revealing targets.
- **FR-038**: **Fountain**: MUST show a visible exact-variant condition, for example "restore 6 Water around it". Meeting the condition MUST produce a visible board change.
- **FR-039**: **Locked slot**: one of the five slots MUST stay unusable until its key is collected. **Mystery pod** and **mystery tile**:
  - A mystery pod MUST reveal its exact variant when committed.
  - A mystery tile MUST reveal its exact variant when it becomes reachable.
  - Both MAY ship only if the player-information solver shows that no level forces a blind guess.
  - Otherwise the roadmap substitutes another validated mechanic.

  Other late mechanics (chest, statue, bridge, connected triple) ship only if validated before launch. The game MUST NOT contain combat, timers, random spawning, manual path drawing or permanent hero power upgrades.

#### F. Boosters, Petals and recovery (docs 09, 10)

- **FR-040**: A failed or abandoned attempt MUST cost nothing. Restarts MUST be unlimited and free, and there MUST be no lives or energy system (doc 10, "No lives baseline").
- **FR-041**: Petals (soft currency) MUST be earned per level: a base amount, plus a clean-clear (no-booster) bonus, plus a Hard/Super Hard bonus. Petals MUST also come from milestones and optional rewarded ads. Prices MUST NOT inflate with the level number.
- **FR-042**: The four boosters MUST unlock at L3 (Extra Slot), L4 (Shuffle), L6 (Return) and L9 (Bloom Burst). Each unlock MUST come with a demonstration and one free charge.
- **FR-043**: **Extra Slot** MUST add one extra usable slot until the end of the current level. At most one extra slot can be active.
- **FR-044**: **Shuffle** MUST rearrange only the remaining eligible Source Pods. Waiting pods are unaffected, locks stay attached to their pods, and connected pods stay connected. If any arrangement can still be won without further boosters, the result MUST be winnable. Each use consumes a charge.
- **FR-045**: **Return** MUST move one unfinished waiting pod, with its remaining count, back to the top of its original Source stack. Cleared tiles stay cleared.
- **FR-046**: Every level MUST be winnable without boosters; FR-080 validates this. A booster that can have no effect MUST be disabled.
- **FR-047**: Boosters MUST be obtainable through unlock grants, level-completion drops, milestone rewards, Petal purchases, rewarded ads and in-app purchase bundles.
- **FR-048**: Recovery MUST stay limited: at most one extra slot at a time, each booster use consumes a charge, Bloom Burst is the most expensive booster, and the ad rescue is available once per attempt.
- **FR-049**: No booster, cosmetic or reward may change the exact-matching rule or give Bloomlings extra power.
- **FR-050**: **Bloom Burst** MUST let the player choose one visible exact variant. It then removes every remaining layer of that variant, visible and hidden, and every pod of that variant from the tray and the slots. Accounting stays reconciled. This default follows the doc 09 main proposal; the "limited number of cells" alternative in doc 09 may replace it after solver and economy review.

#### G. Store, ads and daily reward (doc 10)

- **FR-051**: The Store MUST open fully at L12. It sells Petal packs, boosters, Remove Ads and an optional starter pack. Cosmetics join after the Wardrobe unlock.
- **FR-052**: Rewarded ads MUST always be started by the player and optional. They MAY be used for: jam rescue, a free booster, an extra win reward, and an optional daily bonus.
- **FR-053**: Interstitial ads MUST appear only at post-win transitions. They MUST never appear during a level, immediately after a fail, or during onboarding (Levels 1–10). They MUST be capped by both time and level count.
- **FR-054**: Remove Ads MUST disable interstitials and keep the optional rewarded ads. Permanent purchases MUST be restorable on reinstall or on a new device, and MUST NOT depend only on local storage.
- **FR-055**: Daily Reward MUST unlock at L7, with one claim per calendar day.
- **FR-056**: No level may require spending money or watching ads. This follows from FR-046 and FR-080.

#### H. Progression, Home and long-run motivation (docs 07, 08, 11, 13)

- **FR-057**: Progression MUST be one linear sequence of levels, Level 1 → 2 → … → 5000+. Each win unlocks the next level. There MUST be no level map, no level chooser and no level groupings. The flow is Launch → Home → Play → Level N → Win → Next.
- **FR-058**: Home MUST show: the logo, Level N, Play/Continue, Petals, Settings, the Store (once unlocked), a teaser for the next milestone (for example, "Level 100 reward in 12"), and the leaderboard rank (after L10).
- **FR-059**: Every level MUST have a class: Normal, Hard (label from L5) or Super Hard (label from L10). The class is shown before the level starts, with a distinct visual treatment and higher rewards. From L11 on, every 100 consecutive levels MUST contain 15–25 Hard and 6–10 Super Hard levels, spaced irregularly (tuning targets from doc 07: Hard every 4–6 levels, Super Hard every 10–15). The level after a Super Hard is a relief level. Difficulty moves in waves and does not rise monotonically.
- **FR-060**: The number of active variants MUST grow gradually:

  | Level | Active variants |
  |---|---|
  | L1 | 2 |
  | L2 | 3 |
  | L11–25 | 3–4, with all four families present by L20 |
  | from L32 | 5 is normal |
  | from L70 | 6 in Hard levels |
  | from ~L300 | 6 in advanced profiles |

  New variants join the global pool at milestones (for example L45 and L200) without any player choice.
- **FR-061**: Milestone rewards MUST be granted exactly once each:

  | Cadence | Reward |
  |---|---|
  | Every 25 levels | A bundle |
  | Every 50 levels | A cosmetic or profile reward |
  | Every 100 levels | A major milestone |
  | 250, 500, 1000 and every 250/500/1000 levels after | Prestige: frame, skin, badge or leaderboard marker |

  When a level matches several cadences (for example, L100 is a multiple of 25, 50 and 100), only the reward of the largest cadence is granted.

  After about L500, new core mechanics MUST be rare.
- **FR-062**: The Leaderboard MUST unlock at L10 and rank players globally by highest completed level. Ties are ordered by who completed that level first. Submissions MUST pass sanity checks: progress only moves forward, no impossible jumps, and the content version is compatible.
- **FR-063**: Wardrobe MUST unlock at L40. It offers cosmetic skins, hats, trails and expressions with no gameplay effect. Cosmetics MUST NOT reduce tile or pod readability.
- **FR-064**: Daily Challenge SHOULD unlock at L50. It is one optional puzzle per day, the same for all players, with a separate reward, and it does not change Level N. If it is cut from launch, the roadmap MUST put another unlock at L50.
- **FR-065**: Every finished picture MUST be added to a Collection that the player can view. The Collection is never a level selector.
- **FR-066**: Background themes MUST rotate automatically by level band. This is visual only; there are no navigable areas.

#### I. Screen layout and ease of use (doc 11)

- **FR-067**: The game MUST be playable one-handed in portrait orientation on phones.
- **FR-068**: The gameplay screen MUST be laid out as follows, with no goals panel (the board itself shows the remaining work):
  - Top: Pause, Level N, 2× speed.
  - Center: the board.
  - Below the board: the Garden Entry and the Waiting Slots.
  - Bottom: the stacked Source Tray, with a compact booster bar.
- **FR-069**: The 2× speed setting MUST change only animation speed, never the outcome.
- **FR-070**: Every tap MUST get immediate feedback. Selectable, locked, waiting, active, stuck and jam-risk states MUST be communicated visually, with minimal text. Bloomlings MUST stay small enough not to hide tile state.
- **FR-071**: When the second variant of a family first appears, the game MUST show both side by side with one short message, for example "Match the exact symbol". It MUST then show the first variant's pod ignoring the sibling's tile. The explanation is not repeated later.
- **FR-072**: Accessibility: every variant MUST have its own icon, with enough color distance and readable counts. Palettes MUST be tested for colorblind safety. Hue alone MUST never carry meaning.
- **FR-073**: Settings MUST offer music, sound effects and haptics toggles, plus restore purchases.
- **FR-074**: Gameplay, progression, owned boosters, locally earned rewards and settings MUST work offline. Leaderboard, cloud sync, ads, purchases and remote tuning MAY require a connection.

#### J. Level catalog and content pipeline (docs 06, 14, 15)

- **FR-075**: The launch catalog MUST contain at least 5000 sequential levels. Each level MUST be deterministic and verified by the solver.
- **FR-076**: Each level number MUST map to one stable, versioned level definition, identified by level number, definition version, seed and content version. The definition MUST be identical for all players and devices. An app or content update MUST NOT change a shipped level unless a fix is deliberately versioned.
- **FR-077**: The game MUST build each level from its definition. A definition MAY be a compact generation config that the game expands into cells, layers and pods deterministically. The same definition MUST always produce the same level.
- **FR-078**: The launch catalog MUST be included with the app for offline play. Later content packs and level fixes MUST be deliverable without a new app release.
- **FR-079**: Generated levels MUST be produced picture-first and solution-first, in this order:
  1. The base picture and its role-to-variant mapping fix the visible top layer of every cell.
  2. The generator derives the dependency structure from the picture's shape, that is, which regions shield others from the Garden Entry.
  3. It MAY add hidden layers, keys and specials.
  4. It splits each variant's demand into pods and arranges the Source Tray around at least one planned solution.
  5. The solver validates the level.

  Random color painting is forbidden. Hand-curated levels (the Levels 1–100 tier and mechanic showcases) MUST also start from an approved base picture and pass the same validation (FR-080). Each generation profile sets: level band, board size, picture pool (by tags), variant count and allowed variants, entry layout, layer depth, allowed mechanics, source stacks, pod sizes, buffer-pressure target, target difficulty, target duration and Hard/Super Hard mode.
- **FR-080**: Every shipped level MUST satisfy all of the following, and the check MUST fail the release if any level violates them:
  - it is winnable without boosters, with at least one stored solution trace;
  - its per-variant accounting reconciles exactly;
  - it has no inaccessible mandatory content;
  - it has no mechanic before its unlock level;
  - it has no unavoidable hidden-information failure;
  - it has no unreadable palette combination.
- **FR-081**: Every non-tutorial level MUST be losable: at least one sequence of legal taps jams it, so bad choices matter.
- **FR-082**: Every level MUST receive a difficulty score and a Normal/Hard/Super Hard class. The score is based on structure, buffer pressure, variants, specials and scale. The class MAY be overridden manually during curation.
- **FR-083**: The catalog MUST avoid repetition. It MUST reject repetitive sequences: no 3 consecutive levels may share the same active variant set, no 3 consecutive levels may share the same set of mechanics, and the same Source layout signature (stack count plus the ordered pod counts per stack) MUST NOT appear twice within any 50 consecutive levels. Picture and topology repetition is covered by the base-picture rules below. Levels 1–100 MUST each use a different base picture. The same base picture MUST NOT appear twice within any 50 consecutive levels. When a base picture reappears, the new level MUST differ in role-to-variant mapping or mirroring and in Source design.
- **FR-084**: Quality assurance MUST follow the curation tiers:

  | Levels | Required checks |
  |---|---|
  | 1–100 | Manual playtest of every level |
  | 101–500 | Solver plus manual review of every level |
  | 501–5000+ | Solver plus automated invariants plus human sampling |

  Milestone, Hard and Super Hard levels MUST get stronger review in every tier. Every base picture MUST be reviewed by a person for recognizability and gameplay usability before any level uses it. Pictures may be hand-drawn, generated, or generated and then edited.
- **FR-085**: Economy values, ad cadence, rewards, feature flags and store offers MUST be tunable remotely without an app release. Core puzzle rules and shipped level definitions MUST NOT be remotely mutable, except through versioned content updates.
- **FR-086**: The team MUST be able to see per-level start, win, jam and booster-use rates for difficulty tuning. Every crash or error report MUST include the app version, level number and content version.

#### K. Profile, save, security and compliance (doc 15)

- **FR-087**: A local player profile MUST be created automatically on first launch, with no sign-in. Signing in with a platform identity (Apple / Google) is optional and enables cloud sync. Sync conflicts MUST preserve the furthest valid progression and all purchased entitlements.
- **FR-088**: The saved state MUST include: highest completed level, Petals, booster inventory, unlock flags, cosmetics, milestone rewards claimed, settings and basic statistics.
- **FR-089**: Purchases MUST be validated. Leaderboard submissions and premium currency MUST be protected in proportion to their abuse risk. Core gameplay MUST NOT require an always-online connection.
- **FR-090**: The game MUST obtain the user consents that platform policies and regional privacy laws require for ads and analytics before using personal data for them.
- **FR-091**: The game MUST NOT reuse Colony Flow's name, characters, art, audio, level pictures or interface graphics. The similarity is limited to gameplay rules and structure.

### Unlock Roadmap (from doc 13; indicative level numbers)

| Level | Unlock |
|---|---|
| 1 | Core play: Source Pods, 5 Waiting Slots, 2 variants, Win → Next |
| 2 | Third active variant |
| 3 | Extra Slot (+1 free charge) |
| 4 | Shuffle (+1 free charge) |
| 5 | Hard label/profile |
| 6 | Return (+1 free charge) |
| 7 | Daily Reward |
| 8 | Key preview: the Key mechanic unlocks here by default. A Mystery Pod may take this slot only once the fairness solver passes; the Key then unlocks at L14 |
| 9 | Bloom Burst (+1 free charge) |
| 10 | Leaderboard + Super Hard |
| 11 | Stones |
| 12 | Full Store (Petal packs, boosters, Remove Ads) |
| 14 | Key practice (Key unlock if L8 went to the Mystery Pod) |
| 16 | Locked Source Pod |
| 18 | Connected Pair |
| 20 | All four families regular |
| 25 | Milestone reward |
| 28 | Layered Tile (depth 2) |
| 32 | 5-variant levels routine |
| 35 | Garden Gate / heavy blocker |
| 40 | Wardrobe / skins |
| 45 | Additional variant enters pool |
| 50 | Daily Challenge (or substitute) |
| 60 | Fountain |
| 70 | 6-variant Hard levels |
| 75 | Cosmetic/profile milestone |
| 80 | Locked Waiting Slot |
| 90 | Mystery Tile if fair, otherwise a substitute |
| 100 | Major milestone + theme rotation |
| 125 | Depth-3 layers |
| 150 | Chest (optional) |
| 175 | Advanced connected/locked combinations (profile only) |
| 200 | Major milestone + new variant |
| 225 | Advanced Hard profile |
| 250 | Environmental object #2 (e.g., Statue / Bridge / Seal) |
| 300 | 6 variants normal in advanced profiles |
| 400 | Connected Triple (optional) |
| 500 | Core-system completion milestone; new mechanics rare afterwards |
| 600+ | Prestige every 250/500/1000 levels; occasional new variant/theme |

### Level Band Guidelines (docs 02, 05, 06, 07)

| Band | Levels | Board (cells) | Active variants | Source Pods | Work (tile-layers) | Typical duration |
|---|---|---|---|---|---|---|
| Onboarding | 1–10 | 7×8–8×8 | 2–3 | 3–7 | 30–60 | 20–45 s |
| Early | 11–25 | 9×10–10×10 | 3–4 | 6–12 | 50–100 | 45–120 s |
| Early-mid | 26–50 | 10×10–12×12 | 4–5 | 10–20 | 90–180 | 45–120 s |
| Core completion | 51–100 | 10×12–14×14 | 5 (6 in Hard) | 10–24 | 90–180 (Hard 150–300) | 45 s–4 min |
| Combination | 101–500 | up to 14×16 | 5–6 | 15–30 | 150–300+ | 1–4 min |
| Long run | 501–5000+ | up to 14×16 | 4–6 (7 rare) | 10–30+ | 90–300+ | 45 s–4 min |

Pod sizes:

| Size | Tiles |
|---|---|
| Small | 5–15 |
| Medium | 16–40 |
| Large | 41–100 |
| Exceptional | 100+ |

Buffer-pressure targets (peak number of occupied slots):

| Pressure | Peak occupied slots |
|---|---|
| Relaxed | 1–2 |
| Normal | 2–3 |
| Tense | 3–4 |
| Critical | 4–5 |

Layout principles:

- The board looks full and reads as a picture of its subject.
- The picture's color regions form the tile clusters: the outline, the parts of the subject and the background. Subjects with organic shapes (petals, leaves, rounded objects) are preferred over rectangular blocks.
- Outer regions shield inner ones, so the order of pods matters. The generator picks pictures and mappings whose nesting fits the band's target difficulty.
- One variant's demand is usually split into several pods.
- Difficulty comes from source ordering and dependencies, never from tile hit points.

### Key Entities

- **Level definition**: level number, definition version, seed, content version, base picture, role-to-variant mapping, mirroring, board mask, Garden Entries, active variant set, cells and layers, specials, Source stacks and pods, keys and locks, connections, difficulty class and score, solution trace(s), reward profile.
- **Base picture**: a subject with a grid of color roles (at most 14×16), an optional background region, a finished (restored) look, tags (theme, season, suitable bands) and a review status. It may be reused across levels (FR-006, FR-083).
- **Bloomling family**: Sprig, Bloom, Drop or Twig. A character and animation family.
- **Target variant**: the exact matching type. It has a family, color, icon, tile art and pod skin.
- **Cell / tile-layer**: a board position with its content and an ordered stack of layers.
- **Special object**: stone, key, gate or heavy blocker, Fountain, and later chest, statue or bridge. Each has its own visible condition and effect.
- **Spirit Pod**: an exact variant, a remaining count, a stack position, and a state: ordinary, locked, mystery, connected, waiting, active or stuck.
- **Waiting Slot**: its state: free, occupied, locked or extra.
- **Booster**: Extra Slot, Shuffle, Return or Bloom Burst, with unlock level, charges and price.
- **Player profile / save**: progression, Petals, inventory, unlocks, cosmetics, milestones, settings, statistics and linked identity.
- **Milestone**: level number and reward, granted once.
- **Leaderboard entry**: player, highest completed level, and the time it was reached.
- **Generation profile**: the parameters of a level band (FR-079).
- **Content pack**: a version and a set of level definitions and assets.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In playtests, at least 90% of first-time players finish Level 1 within 2 minutes of first launch without outside help.
- **SC-002**: After Level 10, at least 80% of playtesters can explain in their own words when a level jams.
- **SC-003**: In a glance test with 6 active variants on a 14×16 board, players identify the exact variant of any tile with at least 95% accuracy, and confuse same-family siblings in under 2% of answers. They also tell active tiles apart from restored (open) cells with at least 95% accuracy. The same test passes under a colorblind simulation.
- **SC-004**: 100% of the 5000+ launch levels pass every invariant in FR-080 before release.
- **SC-005**: Replaying a stored tap sequence gives an identical outcome in 100% of automated replays, across devices and at both 1× and 2× speed.
- **SC-006**: Median completion times fall within the band targets: tutorial 20–45 s, Normal 45–120 s, Hard 2–4 min.
- **SC-007**: First-attempt win rates meet the targets (tunable): Normal at least 70%, Hard 35–60%, Super Hard 15–40%.
- **SC-008**: Every tap shows feedback within 0.1 s. Animation stays smooth on the lowest supported devices with the largest boards: at least 30 fps, with no frame hitch longer than 100 ms.
- **SC-009**: Progress survives app restarts in 100% of test cases. Permanent purchases are restorable in 100% of test cases.
- **SC-010**: At least 60% of new players reach Level 10 (the Leaderboard unlock) in their first session.
- **SC-011**: The same level number shows an identical level on every tested device and account (100% match).
- **SC-012**: Levels 1–100 use 100 different base pictures, and no base picture repeats within any 50 consecutive levels of the catalog.
- **SC-013**: In ad-placement tests, no interstitial is ever shown during a level, right after a failure, or before Level 11.
- **SC-014**: In playtest surveys, the game averages at least 4 out of 5 for "easy to understand" and for "relaxing".
- **SC-015**: In a recognition test, at least 80% of players correctly name a level's subject from the board at level start, before any tile is cleared.

## Assumptions

- **Precedence**: the product documents v0.5 (`product/01`–`15`, `LOCKED_CONCEPT_v0.5.md`) are the detailed design. Colony Flow is the structural reference. This spec consolidates both, and where the documents are silent, the spec's own rules apply. The spec adds:
  - FR-021: the deterministic tile-choice rule;
  - FR-026: jam-like "stuck" handling;
  - FR-036: the layer peek indicator;
  - FR-044: the Shuffle winnability rule;
  - FR-045: Return goes to the top of the original stack;
  - FR-062: leaderboard tie-break by time;
  - FR-064: the daily challenge is the same for all players.

  The picture-first mosaic (FR-006 and FR-079, answer A) replaces the silhouette-first approach in doc 05 §5, doc 06
  §1, §5 and §7–8, and doc 12 §10; docs 14 and 15 also referenced board masks. Those documents were updated on 2026-09-29 to match (see `product/CHANGELOG_v0.5.md`); each carries a "Revision 2026-09-29" note. Doc 10 already
  matches the no-lives answer.
- **Open decisions** in the documents, and the defaults used here:
  - Level 8: Mystery if fair, otherwise Key.
  - Bloom Burst: full variant removal (FR-050).
  - Daily Challenge: desired at launch, may be cut.
  - Hard/Super Hard cadence: tunable.
  - Leaderboard: global only.
  - Level 40 cosmetic: to be decided.
  - Variants entering at L45/L200: to be decided.
- **Platforms**: Android and iOS phones, portrait. The high-level technical direction in doc 15 (Unity/C#, deterministic simulation, offline generator and solver, lightweight backend) is input for planning, not part of this spec.
- **Picture library**: if each base picture is used in at most about 5 levels, a 5000-level catalog needs roughly 1000–1500 base pictures. The exact size and sourcing (drawn, generated, or generated and edited) are decided in planning.
- **Economy numbers** are tuning parameters: Petal rewards, booster prices, ad caps and milestone bundles. Starting points are taken from the reference game: a win pays roughly 12–30 Petals, a booster costs roughly 40–60 Petals.
- **Out of scope for launch** (doc 14):
  - world map, room builder, PvP, clans, narrative campaign;
  - permanent hero power upgrades;
  - battle pass;
  - 5000 hand-crafted scenes;
  - live events other than the Daily Challenge.
- **Language**: English first. The minimal-text UI keeps localization cheap.
- **Art**: strict flat 2D, calm and minimal, with 4 reusable family animation rigs. A new variant needs an icon, a palette, a tile skin, a pod skin and a small character accent, not a new character (doc 12).
