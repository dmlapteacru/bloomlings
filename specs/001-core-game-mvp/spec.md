# Feature Specification: Bloomlings Core Game (Colony Flow–style buffer puzzle)

**Feature Branch**: `001-core-game-mvp`

**Created**: 2026-09-29

**Status**: Draft

**Input**: User description: "мы будем делать концептуальную копию игры Colony Flow. в репозитории есть направление игры в папке "product". это скорее не правила которым надо следовать бесприкословно, а потенциальное направление , чтоб как-то отличить нас от Colony FLow. Но мы хотим сделать еще однку такую же игру, концепт геймлпея, идея, должна сохраниться, простота, развитие игры, уровней, лэйауты должны быть схожи своей простотой и удобством."

## Overview

Bloomlings is a conceptual re-creation of **Colony Flow!** (ABI Games, iOS/Android). Like the original, it is a
relaxing puzzle with one-tap controls and no time pressure. The player sends groups of small workers from a tray
into five waiting slots. The workers clear the matching cells they can reach on the board. The player wins by
clearing the board and loses if the waiting slots jam.

**We keep from the reference game**: the core loop and its rules, simple controls and screens, the pace at which
levels get harder and new mechanics appear, and simple, readable level layouts.

**What makes Bloomlings different** comes from the direction in `product/CONCEPT.md`. That document is a direction,
not binding rules. The differences:

- an enchanted-garden setting, with garden spirits instead of ants;
- clearing an overgrown layer reveals a restored garden scene underneath;
- progress through garden areas on a map;
- a guarantee that every level can be won without boosters;
- a few extra mechanics taken from the concept.

| Aspect | Colony Flow (reference) | Bloomlings |
|---|---|---|
| Workers | Ants that leave a nest hole | Garden spirits (Sprig, Bloom, Drop, Twig) that enter through a garden gate |
| Board | A pixel-art picture that is cleared away | Overgrown garden cells; clearing them reveals a restored scene underneath |
| Core loop | Tray → 5 waiting slots → workers clear reachable matching cells → jam = fail | Same |
| Mechanics | Hidden boxes, locked boxes + keys, connected boxes, a second nest | Same, plus layered cells, mystery cells, stones and a locked slot |
| Boosters | Extra Slot, Shuffle, Pick Up, Vacuum | The same four roles: Extra Slot, Shuffle, Return, Clear Type |
| Meta | A collection of finished pictures | Garden areas restored step by step on a garden map |
| Hard levels | Hard / Super Hard; players report that some need boosters | Hard / Super Hard; every level can be won without boosters |

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Play a level: send spirits, restore the garden, avoid the jam (Priority: P1)

A player opens a level and sees three parts, from top to bottom:

- the overgrown board, with a garden gate on its bottom edge;
- a row of five empty waiting slots;
- the tray of spirit groups. Each group shows its spirit type and a number.

The player taps a group in the front row of the tray, and the group moves into a free slot. Its spirits walk out of
the gate to the nearest reachable cells of their type and clear them one by one. The group's number counts down
with each cleared cell. Clearing outer cells opens the way to deeper cells. When the number reaches zero, the group
leaves and its slot is free again. Each cleared cell uncovers a piece of a restored garden scene.

The player wins when every clearable cell is gone. The player loses when all slots are full and none of the waiting
groups can reach a cell of its type.

**Why this priority**: This loop is the game. Every other story builds on it, and on its own it is enough to test
whether the design is fun and clear.

**Independent Test**: Play one hand-made level with 3 cell types and about 8 groups from start to win. Then replay
it in a deliberately bad order until it jams.

**Acceptance Scenarios**:

1. **Given** a level has just started, **When** the player taps a group in the front row of the tray while a slot is free, **Then** the group moves into the leftmost free slot. Its spirits start clearing reachable cells of its type, and its number goes down by one for each cleared cell.
2. **Given** a group in a slot needs more cells than are currently reachable, **When** it has cleared all reachable matching cells, **Then** it stays in its slot with its remaining number shown. It resumes on its own as soon as more matching cells become reachable.
3. **Given** a group's number reaches zero, **When** its last cell is cleared, **Then** the group leaves and its slot is at once available for the next selection.
4. **Given** every usable slot is occupied, **When** the player taps a group in the tray, **Then** the selection is refused with clear feedback and nothing else changes.
5. **Given** every usable slot is occupied and no waiting group can reach a cell of its type, **When** all spirit movement has finished, **Then** the level ends in a jam. The player is offered to continue with a booster or to retry.
6. **Given** the last clearable cell is cleared, **Then** the level is won. The full restored scene is shown with a short celebration, followed by the rewards and a button to go to the next level.
7. **Given** the player retries a level, **Then** the level restarts with exactly the same board and tray as the first attempt.
8. **Given** only one usable slot is still free, **Then** the slot row shows a clear warning that the next selection could cause a jam.

---

### User Story 2 - Progress through levels with a gentle learning curve (Priority: P2)

A new player launches the game and goes straight into level 1, where a pointer shows which group to tap. Each win
unlocks the next level. Early levels are small and teach one idea at a time. At most one new mechanic or booster appears per level,
always on a Normal level and with a short visual demonstration. Some levels are announced as
Hard or Super Hard before they start. Progress is saved automatically. On later launches, the player lands on the
garden map with one big button to play the next level.

**Why this priority**: Progression and onboarding turn a single puzzle into a game players come back to. They also
reproduce the reference game's gradual rise in difficulty.

**Independent Test**: A new player installs the game and completes levels 1–10 without outside help. They close the
game, reopen it, and continue from level 11.

**Acceptance Scenarios**:

1. **Given** the game is launched for the first time, **When** it finishes loading, **Then** level 1 starts directly, with no sign-in and no menus, and the first tap is guided.
2. **Given** the player wins level N, **When** they tap "Next", **Then** level N+1 starts.
3. **Given** a level introduces a new mechanic or booster, **When** it starts, **Then** a short, mostly visual demonstration explains it, and the level contains no other new element.
4. **Given** the next level is Hard or Super Hard, **When** it is about to start, **Then** the player sees its difficulty label with a distinct visual treatment.
5. **Given** the player has played before, **When** they launch the game, **Then** they land on the garden map, which has one prominent Play button for their next level.
6. **Given** the player closes the game at any moment, **When** they reopen it, **Then** their current level, coins, boosters and settings are restored. A level left unfinished starts again from the beginning, without penalty.

---

### User Story 3 - Advanced mechanics keep levels fresh (Priority: P3)

As the player advances, levels combine new elements:

- mystery groups, whose type is revealed only when they reach the front of the tray;
- locked groups, which open with a key found on the board;
- linked groups, which must be placed together;
- a second garden gate;
- layered cells, which reveal another type underneath;
- mystery cells;
- stones, which can never be cleared;
- a waiting slot that starts locked.

**Why this priority**: The core loop is complete without these elements. They provide the long-term variety and the
rise in difficulty that the reference game relies on.

**Independent Test**: For each mechanic, play a dedicated level that uses only the core loop plus that mechanic.
Check every acceptance scenario below.

**Acceptance Scenarios**:

1. **Given** a mystery group is not in the front row, **Then** its type is hidden and its number is visible. **When** it reaches the front row, **Then** its type is revealed before the player can select it.
2. **Given** a locked group is in the front row, **When** the player taps it, **Then** it is not selected and the game highlights the key it needs. **When** the cell that carries that key is cleared, **Then** the key flies to the lock and the group becomes selectable.
3. **Given** two linked groups are in the front row and at least two usable slots are free, **When** the player taps either of them, **Then** both move into slots at the same time. **Given** there are fewer free usable slots than linked groups, **Then** the selection is refused with clear feedback.
4. **Given** a level has two gates, **Then** a cell counts as reachable if it touches open ground connected to either gate.
5. **Given** a layered cell, **Then** it shows its current type and signals what lies beneath. **When** its top layer is cleared, **Then** the next type appears in the same cell and can be cleared by its own spirits.
6. **Given** a mystery cell becomes reachable, **Then** its type is revealed and stays revealed.
7. **Given** a stone cell, **Then** it can never be cleared or walked through, and it is not required to win.
8. **Given** a level starts with a locked slot, **Then** only four slots are usable until the slot's key is collected from the board.

---

### User Story 4 - Boosters and coins help recover from mistakes (Priority: P4)

The player earns coins for each win. Harder levels pay more, and winning without boosters adds a bonus. Four
boosters unlock one by one during the first levels. Each comes with a demonstration and free uses:

- **Extra Slot**: a sixth slot for the rest of the level.
- **Shuffle**: rearranges the remaining tray.
- **Return**: sends a group from a slot back to the tray.
- **Clear Type**: removes one type from the level.

When the slots jam, the player can continue by using a booster that resolves the jam.

**Why this priority**: Boosters soften frustration on hard levels and give coins a purpose. The game is fully
playable without them.

**Independent Test**: Win levels to earn coins, buy each booster, and use each booster in a level. Then continue a
jammed level with Extra Slot.

**Acceptance Scenarios**:

1. **Given** the player wins a level without boosters, **Then** they receive the level's base coins plus the no-booster bonus.
2. **Given** the player reaches a booster's unlock level, **Then** its demonstration plays and the player receives its free uses.
3. **Given** the player owns an Extra Slot, **When** they use it, **Then** a sixth usable slot appears until the end of the level. Extra Slot cannot be used twice in the same level.
4. **Given** the player uses Return on a group in a slot, **Then** the group goes back to the front of its tray column with its remaining number, and the slot becomes free.
5. **Given** the player uses Shuffle, **Then** the groups left in the tray are rearranged. Locked groups stay locked and linked groups stay together. If any tray group has a reachable matching cell, at least one such group ends up selectable.
6. **Given** the player uses Clear Type on a type, **Then** every cell and layer of that type and every group of that type, in the tray and in the slots, are removed from the level.
7. **Given** a level has jammed, **When** the player owns or can afford a booster that resolves the jam, **Then** they can use it and continue. Otherwise, only Retry is offered.
8. **Given** a booster can have no effect in the current state, **Then** its button is disabled.

---

### User Story 5 - Restore the enchanted garden (Priority: P5)

Levels belong to garden areas, for example Forgotten Courtyard, Lily Ponds, Old Orchard and Ancient Greenhouse. A
garden map shows the areas in order and where the player is. Each win restores a visible part of the current area.
Finishing an area shows it fully restored and opens the next one.

**Why this priority**: This is the long-term emotional payoff that sets Bloomlings apart from the reference game.
It is not needed to play and enjoy the levels.

**Independent Test**: Complete every level of the first area. Check that the map fills in step by step and that the
second area opens with a celebration.

**Acceptance Scenarios**:

1. **Given** the player opens the garden map, **Then** they see all areas in order. The map shows which areas are restored, which is in progress (with the number of levels left) and which are still locked.
2. **Given** the player wins a level, **When** they return to the map, **Then** the part of the area that matches that level appears restored.
3. **Given** the player wins the last level of an area, **Then** a short celebration shows the fully restored area, and the next area unlocks.
4. **Given** an area is fully restored, **Then** the player can view its restored scene at any time.

---

### Edge Cases

- **Tap on a group outside the front row**: nothing is selected. A subtle hint shows that only front-row groups can be selected.
- **Fast taps while spirits are still moving**: each accepted tap is applied in order. The result is the same as with slow taps.
- **Two groups of the same type in slots at the same time**: the group placed earlier gets cells first, and the later group takes what remains.
- **Group selected while its type has no reachable cells**: allowed. The group waits in its slot; managing this risk is the core of the game.
- **Last free slot filled while spirits are still opening new paths**: a jam is checked only after all movement has settled. If the new paths let a waiting group work, there is no jam.
- **Cleared layered cell reveals a type that a group is already waiting for**: that group starts clearing it on its own.
- **Key collected while its locked group is still behind other groups**: the group is unlocked. It becomes selectable once it reaches the front row.
- **Locked group blocks the front of its column and its key can never be reached**: level validation prevents this. Every key must be collectable in at least one winning sequence.
- **Tray empty or unselectable, groups waiting in slots, none can work, and clearable cells remain**: the level is lost as "stuck". This can only happen with locks or linked groups. It offers the same continue and retry options as a jam.
- **Win and jam at the same moment** (the last cell is cleared while the slots are full): the win takes precedence.
- **App sent to the background or interrupted** (a call, a notification): the level pauses and resumes exactly where it was. If the app is closed, the level restarts from the beginning next time.
- **Jam with no boosters and not enough coins**: only Retry is offered, without any penalty.
- **Return used on a group that belonged to a linked set**: only that group returns. Once the groups are placed, the link no longer exists.
- **Clear Type removes the cell that carries a key**: the key is collected.
- **Level needs a group larger than the remaining cells of its type, or has cells that can never be reached**: level validation rejects it before release.

## Requirements *(mandatory)*

### Functional Requirements

#### Board and cells

- **FR-001**: Each level MUST have a board of square cells in which every cell is exactly one thing: open ground, a clearable garden cell of a single type, a special cell (layered or mystery), or a stone. No cell may ever look partially occupied.
- **FR-002**: The game MUST launch with four cell types, each cleared only by its own spirit: Greenery (leaf) by Sprig, Flowers by Bloom, Water by Drop, and Wood by Twig. [NEEDS CLARIFICATION: should the release keep only these four types, or add new garden types with their own spirits in later areas, to come closer to the reference game's color variety?]
- **FR-003**: Every cell type and every spirit MUST be recognizable by its shape or icon, not by color alone.
- **FR-004**: Each board MUST have at least one garden gate on its edge, usually at the bottom. Spirits enter the board only through gates.
- **FR-005**: A clearable cell MUST count as reachable only when it is orthogonally adjacent to a gate, or to open ground that connects to a gate through orthogonally adjacent open ground. Diagonal contact does not count.
- **FR-006**: Spirits MUST never clear an unreachable cell, even when a matching group is waiting.
- **FR-007**: A cleared cell MUST become open ground, unless it is a layered cell with layers left. It MUST reveal the part of the level's restored garden scene that lies beneath it.

#### Tray and selection

- **FR-008**: The tray MUST show every remaining group, arranged in columns, with its spirit type and number. The type of a mystery group stays hidden until it reaches the front row.
- **FR-009**: Only the group at the front of each column MUST be selectable. When it leaves, the next group in that column moves to the front.
- **FR-010**: Selecting a group MUST take a single tap. Gameplay MUST use no dragging, swiping, timing or multi-touch.
- **FR-011**: A selected group MUST move to the leftmost free usable slot. If no usable slot is free, the selection MUST be refused with visible feedback and no change to the game state.

#### Waiting slots and spirit behavior

- **FR-012**: A level MUST start with five usable waiting slots, unless it uses the locked-slot mechanic.
- **FR-013**: A group in a slot MUST automatically send its spirits to reachable cells of its type. Each cleared cell lowers the group's number by one.
- **FR-014**: When fewer matching cells are reachable than the group's number, the group MUST clear what it can and stay in its slot. It MUST resume on its own when new matching cells become reachable.
- **FR-015**: When a group's number reaches zero, the group MUST leave and its slot MUST become free at once.
- **FR-016**: When several waiting groups share a type, cells MUST go first to the group placed earliest.
- **FR-017**: When more matching cells are reachable than a group needs, spirits MUST pick cells by a fixed rule that players can anticipate: nearest to a gate by walking distance first, then a fixed tie-break order.
- **FR-018**: Groups in different slots MUST work at the same time.
- **FR-019**: The same sequence of accepted selections and booster uses MUST always give the same result. The result MUST never depend on tap speed, animation speed, device or frame rate. The Shuffle booster is the only random element.
- **FR-020**: The player MUST be able to make the next selection while spirits are still moving.
- **FR-021**: In every level, the group numbers of each type MUST add up to the number of cells of that type, counting layers and hidden cells.

#### Win, jam and retry

- **FR-022**: A level MUST be won when every clearable cell has been cleared; stones do not count. A win MUST show the fully restored scene with a celebration of at most 5 seconds that a tap can skip, followed by the rewards.
- **FR-023**: A level MUST be lost by jam when, after all spirit movement has settled, every usable slot is occupied and no waiting group can clear a cell.
- **FR-024**: A level MUST be lost as stuck when, after all movement has settled, all three hold: no waiting group can clear a cell, no tray group can be selected, and clearable cells remain.
- **FR-025**: Jam or stuck MUST never be declared while spirits are still working.
- **FR-026**: When only one usable slot remains free, the slot row MUST show a visible warning until a second slot frees up.
- **FR-027**: On jam or stuck, the player MUST be offered two choices: use a booster that can resolve the situation, whether owned or bought with coins, or Retry. Declining ends the attempt.
- **FR-028**: Retry MUST restart the level with the same initial board and tray. Retries MUST be unlimited and free.
- **FR-029**: There MUST be no time limit and no move limit. Jam and stuck are the only ways to lose.
- **FR-030**: The player MUST be able to pause, restart or leave a level at any time without penalty.

#### Mechanics introduced over time

- **FR-031**: **Mystery group**: its type MUST stay hidden, with the number visible, until it reaches the front row. There it is revealed before it can be selected.
- **FR-032**: **Locked group and key**: a locked group MUST stay unselectable until its matching key is collected. Each key MUST be clearly paired with exactly one lock by the same color or symbol. A key sits on a clearable cell and is collected automatically when that cell, or its top layer, is cleared.
- **FR-033**: **Linked groups**: two groups (three in later levels) are visibly linked in the tray. Tapping any of them MUST select them all together, each taking its own slot. If there are not enough free usable slots, the selection MUST be refused. Once placed, each group behaves on its own.
- **FR-034**: **Multiple gates**: a level MAY have two or more gates. Reachability is counted from all of them.
- **FR-035**: **Layered cell**: MUST show its current type and signal how many layers lie beneath and the type of the next one. Clearing the top layer reveals the next type in the same cell. The cell becomes open ground only after its last layer is cleared. Each layer counts as one cell of its type.
- **FR-036**: **Mystery cell**: its type MUST stay hidden until the cell becomes reachable, then stay revealed.
- **FR-037**: **Stone**: MUST never be cleared or passed through, and is not required to win.
- **FR-038**: **Locked slot**: a level MAY start with one slot locked. The slot becomes usable when its key is collected.

#### Progression, difficulty and onboarding

- **FR-039**: Levels MUST be played in a fixed, linear order. Winning a level unlocks the next.
- **FR-040**: Levels MUST be grouped into garden areas of 20–30 levels each.
- **FR-041**: A level MUST introduce at most one new mechanic or booster. The level that introduces it MUST be labeled Normal and MUST include a skippable, mostly visual demonstration with at most one short sentence of text.
- **FR-042**: New elements MUST be introduced in the order given in *Level Progression Guidelines* below. Exact level numbers may shift by a few levels during tuning.
- **FR-043**: Every level MUST carry a difficulty label, Normal, Hard or Super Hard, shown before the level starts. Hard and Super Hard MUST have a distinct visual treatment and higher rewards.
- **FR-044**: From level 10 onward, any 10 consecutive levels MUST contain at least one Hard or Super Hard level and at most one Super Hard level. The level right after a Super Hard MUST be Normal.
- **FR-045**: The first launch MUST go straight into level 1 with a guided first tap, without sign-in or menus.
- **FR-046**: Later launches MUST open on the garden map, with one prominent button to play the next level.
- **FR-047**: Progress MUST be saved automatically on the device and restored on the next launch: current level, restored areas, coins, boosters and settings. An unfinished level restarts from the beginning.

#### Boosters and coins

- **FR-048**: Winning MUST award coins: a base amount, more for Hard and Super Hard levels, and a bonus for winning without boosters.
- **FR-049**: The game MUST offer four boosters. Each one unlocks at a set early level with a demonstration and two free uses:
  - **Extra Slot**: adds a sixth usable slot until the end of the level. It can be used once per level.
  - **Shuffle**: rearranges the groups remaining in the tray. Locked groups stay locked and linked groups stay together. If any tray group has a reachable matching cell, at least one such group MUST end up selectable.
  - **Return**: moves one chosen group from a slot back to the front of its tray column, keeping its remaining number.
  - **Clear Type**: removes one chosen type from the level. This covers all its cells and layers, including hidden ones, and all its groups in the tray and in the slots.
- **FR-050**: Boosters MUST be purchasable with coins at fixed prices. The booster bar MUST show, for each booster, how many the player owns or its price.
- **FR-051**: A booster that can have no effect in the current state MUST be disabled.
- **FR-052**: No booster or progression reward may change the matching rule or give spirits extra power.

#### Garden map and restoration

- **FR-053**: Each level MUST have a restored garden scene. The scene is revealed step by step as cells are cleared and shown in full on a win. Each level's scene is a part of its garden area.
- **FR-054**: A garden map MUST show all areas in order: which are restored, which is in progress (with the number of levels left) and which are locked.
- **FR-055**: Each win MUST visibly restore the matching part of the current area on the map. Completing an area MUST play a celebration that a tap can skip, then unlock the next area.
- **FR-056**: The player MUST be able to view any fully restored area.

#### Screen layout and ease of use

- **FR-057**: The game MUST be playable with one hand in portrait orientation.
- **FR-058**: The level screen MUST be laid out from top to bottom as follows. The booster bar sits next to the tray, within thumb reach.
  1. Top bar: level number, difficulty label and pause.
  2. Board: the largest element, with its gate or gates on the bottom edge.
  3. Row of waiting slots.
  4. Tray.
- **FR-059**: The whole board MUST be visible without scrolling or zooming. Boards MUST be at most 14 columns × 18 rows, so that every cell can be recognized at a glance on phone screens.
- **FR-060**: Every tap MUST give immediate visual feedback. Sound and haptic feedback are optional.
- **FR-061**: Spirit animations MUST be paced so that the player never waits long. A single group's work MUST play out in no more than 3 seconds, however large its number.
- **FR-062**: The game MUST show every state visually, with minimal text: selectable, locked, waiting, jam warning, win and jam.
- **FR-063**: Settings MUST let the player turn music, sound effects and haptics on and off.
- **FR-064**: The game MUST be fully playable without an internet connection.

#### Level quality

- **FR-065**: Every released level MUST be verified to have at least one winning sequence without boosters.
- **FR-066**: Every released level except tutorial levels MUST be verified to be losable, meaning that at least one sequence of choices jams.
- **FR-067**: Every released level MUST pass these consistency checks:
  - group numbers per type match cells per type (FR-021);
  - every clearable cell and every key can become reachable;
  - each lock has exactly one key;
  - linked groups are valid;
  - the board fits the size limit in FR-059.
- **FR-068**: Levels MUST follow the layout principles in *Level Progression Guidelines*.

#### Release scope and originality

- **FR-069**: This release MUST include [NEEDS CLARIFICATION: how much content (number of garden areas and levels), and is the free-to-play layer (lives, ads, in-app purchases, daily rewards) part of this release or a later feature?]
- **FR-070**: The game MUST NOT reuse Colony Flow's name, characters, art, audio, level pictures or interface graphics. The similarity is limited to gameplay rules and structure.

### Level Progression Guidelines (indicative)

| Stage | Levels | Board size (cells) | Types per level | Groups per level | Tray columns | New elements (at level) |
|---|---|---|---|---|---|---|
| Onboarding | 1–5 | about 7×8 | 2–3 | 4–8 | 2–3 | Core loop (1), Extra Slot (3), Shuffle (4) |
| Early | 6–20 | about 9×11 | 3–4 | 8–14 | 3–4 | Return (6), Mystery group (8), Clear Type (9), Locked group + key (12), Second gate (18) |
| Early-mid | 21–50 | about 10×13 | 4 | 12–20 | 4 | Linked pair, Stones, Mystery cells |
| Mid | 51–100 | up to 14×18 | 4+ | 16–30 | 4–5 | Layered cells, Locked slot, Linked triple, combinations of mechanics |

Layout principles, from the reference game and the concept:

- The board looks full: about 65–80% of cells are occupied.
- Matching cells form organic clusters, such as blobs, rings, snakes, islands and layered shells, rather than rectangles.
- Outer shells of one type protect inner cells of other types, so the order of groups matters.
- Difficulty grows by combining mechanics and tray order, never by making cells need several hits.

### Key Entities

- **Level**: its number, garden area, difficulty label, board, gates, tray, number of usable slots, the new element it introduces (if any), restored scene and rewards.
- **Board cell**: its position and its content: open ground, a garden cell of one type, a layered cell with an ordered list of types, a mystery cell, or a stone. A key may lie on it.
- **Cell type / spirit**: a garden type (Greenery, Flowers, Water, Wood, …) and the spirit that clears it, with an icon and a color.
- **Garden gate**: a position on the board edge where spirits enter.
- **Spirit group**: its spirit type, its number (the cells it will clear), its tray column and position, and any modifiers: mystery, locked (with a key identity), or part of a linked set.
- **Waiting slot**: its position and state: free, occupied by a group, locked (with a key identity), or extra.
- **Key**: its identity (color or symbol), the cell it lies on, and the lock it opens (a group or a slot).
- **Booster**: its kind, unlock level, price and owned count.
- **Garden area**: its name, ordered levels, restored scene pieces and status (locked, in progress or restored).
- **Player progress**: current level, completed levels, coins, owned boosters and settings.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In playtests, at least 90% of first-time players finish level 1 within 2 minutes of first launch without outside help.
- **SC-002**: After level 5, at least 80% of playtesters can explain in their own words why a level is lost (all slots full and no group can work).
- **SC-003**: In a 5-second glance test, players name the type of any cell and the spirit that clears it with at least 95% accuracy, on the largest allowed board.
- **SC-004**: 100% of released levels are verified winnable without boosters. 100% of non-tutorial levels are verified losable through a wrong order.
- **SC-005**: Replaying the same sequence of selections gives the same outcome in 100% of automated replays, across supported devices and animation speeds.
- **SC-006**: The median time to finish a Normal level is between 1 and 4 minutes.
- **SC-007**: First-attempt win rates in playtests meet these targets: Normal at least 70%, Hard 35–60%, Super Hard 15–40%.
- **SC-008**: Every tap shows feedback within 0.1 s, and no single group's clearing takes more than 3 s to play out.
- **SC-009**: Progress (level, coins, boosters, restored areas) survives app restarts in 100% of test cases.
- **SC-010**: At least 60% of new players reach level 10 in their first session.
- **SC-011**: In playtest surveys, the game averages at least 4 out of 5 for "easy to understand" and for "relaxing".

## Assumptions

- **Reference game**: Colony Flow! by ABI Games ([App Store](https://apps.apple.com/us/app/colony-flow/id6779167923), [Google Play](https://play.google.com/store/apps/details?id=com.abi.colony.flow)) is the gameplay benchmark. Its public description and community guides were the source for:
  - the core loop;
  - the five slots;
  - the tray with a front row;
  - the mechanics;
  - the early pacing: hidden boxes around level 8, locks and keys around level 12, a second nest around level 18, and boosters at levels 3, 4, 6 and 9.

  Where the reference game's exact behavior is unknown, the rules in this spec apply.
- **Concept document**: `product/CONCEPT.md` is a direction for how to differ from the reference game: setting, spirits, restoration reveal, garden map and extra mechanics. It is not binding. Where it differs from the reference gameplay, this spec follows the reference:
  - mystery groups are revealed when they reach the front row, not when selected;
  - Return takes any group from a slot, not only the last one placed.
- **Platform**: smartphones in portrait orientation with touch input. Tablets are supported by scaling. The exact platform list is decided during planning.
- **Single player, offline**: no accounts, cloud saves, social features or leaderboards in this release.
- **Out of scope for this feature** (future features):
  - chests, and doors or gates as board objects;
  - environmental objects such as fountains and statues;
  - the Wild Spirit and Reveal boosters;
  - spirit cosmetics;
  - live events, streaks and card collections;
  - the procedural level generator;
  - depending on FR-069: lives, ads and in-app purchases.
- **Economy values** are tuning parameters: coins per win, bonuses, booster prices and free uses. Starting targets follow the reference game: a win pays roughly 12–30 coins, and a booster costs roughly 40–60 coins.
- **Level production**: levels for this release may be made by hand. Each level must pass the automated checks in FR-065 to FR-067.
- **Reachability** uses orthogonal (4-direction) adjacency. Spirits never move diagonally.
- **Replaying** completed levels is not required in this release.
- **Language**: English first. The interface uses minimal text so that localization is cheap later.
- **Art and audio**: flat, calm, minimal 2D art as described in the concept (§2), with soothing music and soft sound effects.
