# Enchanted Garden Puzzle — Locked Concept v0.1

## Name: Bloomlings: Garden Puzzle

## Status

**LOCKED BASELINE CONCEPT**

This document captures the current agreed game concept and should be treated as the baseline for future design work.

The game is a light, minimal 2D puzzle game inspired structurally by **Colony Flow**, but adapted into a new world, visual identity, progression model, and expanded mechanic set.

---

# 1. Core Fantasy

The player restores a forgotten enchanted garden.

Each level starts with a dense board filled with different types of blocked, overgrown, flooded, or unfinished garden cells.

The player sends groups of small garden spirits to restore matching cells.

As the board is cleared, the damaged layer disappears and a clean, beautiful restored garden scene is revealed underneath.

The fantasy is:

> Turn a blocked, forgotten garden into a living, beautiful place by sending the right spirits in the right order.

---

# 2. Visual Direction

## Locked Art Direction

The visual style should be:

- flat 2D;
- bright and calm;
- minimal;
- readable at a glance;
- soft and friendly;
- low visual noise;
- no heavy 3D rendering;
- no overly detailed environment art;
- no shiny toy-like materials;
- no dense decoration competing with gameplay.

The gameplay board should visually dominate the screen.

### General composition

- roughly 70–80% of attention should go to the board;
- background/environment should establish mood, not compete with the puzzle;
- UI should stay consistent between worlds;
- the world changes through palette, background, small decorative motifs and level rewards;
- gameplay cells remain extremely readable.

## Cell readability rule

Every active gameplay cell must be clearly one thing.

A tile is either:

- fully occupied by a gameplay type;
- empty;
- a special tile;
- a blocker.

Avoid partially occupied cells that make the player wonder whether they count.

This is especially important because spirit-group counters are based on the number of cells they can process.

---

# 3. Board Structure

The board is technically based on a square grid, but visually it should not feel like a collection of simple rectangles.

Groups of matching tiles form irregular clusters such as:

- blobs;
- L-shapes;
- snakes;
- islands;
- rings;
- corridors;
- pockets;
- layered structures;
- asymmetric masses.

The player should visually perceive **clusters**, not rectangles.

## Density

The board should normally feel full.

Target direction:

- around 65–80% of the playable board occupied;
- most occupied cells are normal target tiles;
- the remainder is made of special tiles, blockers, empty connectors or environmental objects.

Each resource type should usually have at least one meaningful cluster.

---

# 4. Core Tile Types

The baseline world uses four main cell types.

| Type | Visual | Spirit |
|---|---|---|
| Greenery | Leaf | Sprig |
| Flowers | Flower | Bloom |
| Water | Water Drop | Drop |
| Wood | Log / stump | Twig |

These four types are the initial core language of the game.

They are mechanically symmetric at the start.

---

# 5. Main Heroes / Spirits

## Sprig

**Target:** Leaf / greenery cells

Role in the world:
- revives green areas;
- clears overgrowth;
- restores plants.

Visual behavior:
- small green spirits move toward matching cells;
- vegetation changes into a restored state.

## Bloom

**Target:** Flower cells

Role:
- restores flower beds;
- revives decorative plant zones.

Visual behavior:
- flowers bloom;
- petals or soft particles can appear briefly.

## Drop

**Target:** Water cells

Role:
- restores ponds, streams, fountains and wet garden areas.

Visual behavior:
- small blue spirits flow or hop toward matching cells.

## Twig

**Target:** Wood cells

Role:
- removes old roots, stumps and broken wooden barriers;
- restores wooden garden structures.

Visual behavior:
- small wooden spirits dismantle or transform matching cells.

## Important rule

The four baseline spirits should **not have unique power abilities** in the core puzzle.

At first:

> spirit type = matching target type

This keeps the rules clear and preserves deterministic puzzle design.

Character differentiation should primarily be visual and cosmetic.

---

# 6. Main Gameplay Loop

Each level has:

1. a dense grid-based board;
2. matching target cells;
3. a queue/selection of spirit groups;
4. a limited staging buffer;
5. layered or blocked content;
6. a win condition based on restoring the board.

A spirit group has:

- a type;
- a size/count.

Example:

`Drop x6`

This means the group can process six available Water cells.

## Player action

The player selects a spirit group.

The group moves into one of the staging/buffer slots.

If enough valid matching cells are currently available, the spirits process those cells.

When the group finishes its work, its staging slot is freed.

If it cannot currently complete its work, it waits in the buffer.

---

# 7. Buffer / Main Failure State

The baseline buffer contains:

**5 slots**

Example:

`[ ][ ][ ][ ][ ]`

Selected groups enter these slots.

A group can remain stuck if its matching cells are inaccessible or insufficiently available.

## Main lose condition

If all five buffer slots are occupied and no waiting group can make progress, the level is lost.

Example:

`[Bloom][Twig][Drop][Sprig][Bloom]`

and none of them can currently process enough valid cells.

Result:

**JAM → FAIL**

This should be the primary failure mechanic.

The game should not rely on a move limit as its default failure system.

---

# 8. Restoration Payoff

Clearing cells should not feel like merely deleting colored blocks.

Under the puzzle layer sits a restored scene.

Examples:

- a path;
- pond;
- fountain;
- statue;
- gazebo;
- bridge;
- flower garden;
- ancient mosaic;
- small courtyard.

As gameplay tiles disappear, the player progressively reveals the clean final environment.

At the end of a level, the player gets a short visual reward showing the completed restored area.

This is an important emotional payoff.

---

# 9. Layered Tiles

Layered cells are one of the key planned differentiators.

A cell can reveal another gameplay type underneath.

Example:

`Leaf -> Wood -> Water`

This creates dependency chains.

The player must think not only about what is visible now, but what will become active next.

Example:

- clearing Green may reveal Wood;
- clearing Wood may reveal Water;
- Water may then unlock a fountain.

Layered cells should become an important mid-game mechanic.

---

# 10. Core Additional Mechanics

The following mechanic set is part of the current planned design direction.

## Mystery Group

A spirit group appears as unknown.

Example:

`? x7`

Once selected, its type is revealed.

Purpose:
- adds uncertainty;
- creates buffer risk;
- forces the player to plan around incomplete information.

Use carefully to avoid unfair randomness.

---

## Linked Pair

Two groups are linked and must be selected together.

Example:

`Sprig x6 — Drop x4`

They immediately occupy two buffer slots.

One group may complete while the other remains stuck.

Purpose:
- creates planning around available buffer space;
- increases risk without adding complicated rules.

Later extension:
- linked triple groups.

---

## Locked Group

A spirit group cannot be selected until a condition is met.

Possible unlock conditions:

- collect a key;
- clear a region;
- activate an environmental object.

---

## Mystery Tile

The type of a board cell is hidden until revealed.

Purpose:
- creates information progression;
- changes planning after partial clearing.

---

## Chest

A chest blocks one or more cells.

It may require a key.

Possible rewards/effects:
- unlock board region;
- reveal hidden cells;
- unlock a spirit group;
- restore a buffer slot.

---

## Stone / Permanent Blocker

A cell that cannot be processed by spirits.

Purpose:
- shapes clusters;
- creates access constraints;
- changes how regions connect.

---

## Gate / Door

A gate blocks access to part of the board.

It can open when:

- a key is collected;
- an environmental requirement is completed;
- a specific cluster is cleared;
- a special object is activated.

---

## Blocked Buffer Slot

Instead of five active slots, one can be locked.

Example:

`[ ][ ][LOCKED][ ][ ]`

The player temporarily has only four usable slots.

The slot can later be unlocked through gameplay.

Purpose:
- strong difficulty modifier without changing the core rules.

---

## Large Group

A larger-than-normal spirit group.

Example:

`Sprig x15`

Purpose:
- dangerous to select too early;
- can remain stuck longer;
- increases buffer pressure.

---

# 11. Environmental Objects

The garden world should include special objects that interact with the board.

These create small environmental puzzles inside the main puzzle.

## Key

A key can be revealed by clearing surrounding or covering cells.

The key can unlock:

- a chest;
- a gate;
- a locked spirit group;
- a blocked buffer slot.

---

## Fountain

Example rule:

Restore six Water cells around it.

Result:

- fountain activates;
- nearby cells become accessible;
- a new layer is revealed.

---

## Ancient Statue

Possible rule:

Clear all cells surrounding the statue.

Result:

- statue awakens;
- a hidden section opens;
- a new environmental effect triggers.

---

## Future possibilities

Later worlds can add other objects such as:

- bridges;
- switches;
- magical seals;
- roots;
- portals;
- ancient mechanisms.

These should remain secondary to the core buffer puzzle.

---

# 12. Boosters

Boosters should mainly help the player recover from mistakes.

They should not solve the puzzle automatically.

## Extra Slot

Temporarily adds a sixth staging slot.

`[ ][ ][ ][ ][ ][+]`

---

## Return

Returns the last selected group from the buffer back to the queue.

---

## Magic Spirit / Wild Spirit

A universal group that can process any normal tile type.

---

## Reveal

Later booster.

Reveals:
- a Mystery Group;
- a Mystery Tile;
- or hidden information.

---

# 13. Difficulty Progression

Difficulty should grow mainly by **combining mechanics**, not by increasing tile HP or requiring repetitive tapping.

## Early

- normal groups;
- 2–3 tile types;
- full 5-slot buffer;
- simple clusters.

## Early-mid

- all 4 tile types;
- Mystery Group;
- simple blockers;
- basic keys.

## Mid-game

- Linked Pairs;
- Mystery Tiles;
- Gates;
- Chests;
- layered cells;
- blocked buffer slot.

## Later

- linked triples;
- complex dependency chains;
- multiple gates;
- keys hidden below layers;
- environmental objects;
- combinations of several mechanics.

Example difficult level:

- 4 tile types;
- linked pair;
- mystery group;
- one locked buffer slot;
- key under layered tiles;
- gate protecting the final region.

---

# 14. World / Area Progression

The game should not feel like an endless list of numbered levels only.

Levels are grouped into garden areas.

Possible structure:

## Garden 1 — Forgotten Courtyard

Introduces:
- normal groups;
- core types;
- buffer.

## Garden 2 — Lily Ponds

Introduces:
- keys;
- water-focused objects;
- simple gates.

## Garden 3 — Old Orchard

Introduces:
- linked groups;
- hidden cells;
- larger wood clusters.

## Garden 4 — Ancient Greenhouse

Introduces:
- more layered tiles;
- environmental mechanisms;
- advanced gates.

A good conceptual target is roughly:

**20–30 levels per area**

The exact pacing will be designed later.

---

# 15. Meta Progression

The current direction is intentionally light.

Do **not** start with a heavy builder where the player spends stars to place every bench, tree and lamp.

Instead:

- completing levels restores parts of a larger garden map;
- finishing an area visually completes that zone;
- the player gradually moves deeper into the enchanted garden.

Example map:

`Entrance -> Rose Garden -> Lily Pond -> Old Orchard -> Ancient Temple`

Possible later meta features:

- collectible decorations;
- spirit skins;
- rare plants;
- seasonal gardens;
- visual collections;
- albums.

These are **not required for MVP**.

---

# 16. Spirit Progression

Avoid gameplay power upgrades that break puzzle determinism.

Do not use upgrades such as:

- Sprig now clears 2 cells instead of 1;
- Drop now ignores blockers;
- Bloom now processes more than its displayed count.

Instead, use cosmetic progression.

Examples:

- hats;
- skins;
- seasonal costumes;
- trails;
- animations;
- expressions;
- special idle behaviors.

Puzzle rules should stay stable.

---

# 17. Level Generation Philosophy

The long-term goal may be thousands of levels, but they should not be manually built as thousands of unique rooms.

The game should use:

- a limited number of environment kits;
- a procedural or semi-procedural puzzle builder;
- reusable shape patterns;
- validated solution graphs.

## Shape library examples

- blob;
- L-shape;
- snake;
- island;
- ring;
- corner patch;
- bridge;
- split island;
- spiral;
- nested layers.

## Important

The generator should not simply randomize colors.

It should first generate a **valid dependency structure / solution graph**, then translate that into a board.

Example:

`Green -> Wood -> Water`

or

`Green -> Key -> Gate -> Flower`

The level should always have at least one valid solution.

Bad decisions should be able to cause a jam.

---

# 18. Core Design Principles

These principles are currently locked:

1. The game is primarily a **buffer-ordering puzzle**.
2. The board is dense and visually simple.
3. Every gameplay cell is unambiguous.
4. Clusters should feel organic and irregular.
5. The visual world should stay calm and minimal.
6. Difficulty comes from dependency and order, not repetitive HP.
7. The main fail state is buffer jam.
8. Restoration is the emotional payoff.
9. New mechanics should combine with old mechanics.
10. Hero power progression should not break deterministic puzzle design.
11. The game should be scalable to thousands of levels.
12. Procedural generation must be solution-aware.
13. Development should not begin before core rules are fully specified.

---

# 19. One-Sentence Game Definition

> Choose groups of garden spirits in the right order so they can restore matching areas, reveal deeper layers and environmental mechanisms, and never clog the limited staging buffer.

---

# 20. Next Design Phase

Before implementation, the next document should define **Core Gameplay v1** in precise rules.

Topics to specify:

- what exactly makes a cell “available”;
- how spirits choose between several available cells;
- whether a group must complete its full count before leaving the buffer;
- what happens if a group has 8 capacity but only 5 valid cells exist;
- how layered tiles become active;
- how access/reachability works;
- how the queue is presented;
- how many future groups the player can see;
- exact buffer jam detection;
- win condition;
- interaction timing;
- simultaneous vs sequential spirit animation;
- how linked groups behave;
- deterministic behavior requirements;
- procedural solvability validation.

After that:

1. mechanic catalog;
2. progression plan;
3. level archetypes;
4. generator rules;
5. world/area structure;
6. economy/boosters;
7. monetization;
8. analytics;
9. MVP scope;
10. implementation roadmap.

---

**Locked on:** 2026-09-28
