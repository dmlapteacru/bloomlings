# 09 — Boosters & Recovery

**Status:** DRAFT FOR LOCK

## 1. Booster philosophy

Boosters are optional recovery tools.

Every level must be solvable without them.

## 2. Launch core booster set

Bloomlings should use a four-tool structure similar in functional coverage to the reference game:

### Extra Slot
Unlock target: **Level 3**

Effect:
- adds one extra Waiting Slot for current level.

Baseline starts with 5 slots.

### Shuffle
Unlock target: **Level 4**

Effect:
- reshuffles/rearranges remaining eligible Source Pods in the Source Tray.

Exact rules:
- already active Waiting Pods unaffected;
- locks stay logically attached to their pods;
- connected groups remain connected;
- result must not make level invalid.

This is primarily a Source-order recovery tool.

### Return
Unlock target: **Level 6**

Bloomlings equivalent of picking a waiting pod back up.

Effect:
- choose one unfinished Waiting Pod;
- return its remaining count to Source;
- already-cleared tiles stay cleared.

### Bloom Burst
Unlock target: **Level 9**

Bloomlings functional analogue of a color-clearing power tool.

Effect proposal:
- choose one visible exact target variant;
- clear all currently valid board layers of that exact variant and resolve/remove remaining Source/Waiting capacity for that variant consistently.

Because this is extremely powerful, final semantics require solver/economy review before lock.

Alternative safer version:
- clear a limited number of cells of chosen exact variant.

One of these versions must be selected before implementation.

## 3. Additional situational tools

### Reveal
Only after Mystery mechanics unlock.

Reveals one Mystery Pod/Tile.

### Wild Spirit
Not part of the four primary toolbar boosters in v0.4.

Can exist later as rare reward/event consumable if useful.

## 4. Unlock philosophy

Boosters unlock very early so the player:
- learns the economy;
- has recovery options;
- sees new rewards frequently during onboarding.

Exact roadmap: `13_UNLOCK_AND_MILESTONE_ROADMAP.md`.

## 5. Jam recovery

At Jam:
- keep board visible;
- allow eligible booster;
- optional rewarded-ad rescue;
- Restart.

Avoid forcing Store screen.

## 6. Booster acquisition

Possible:
- free unlock grant;
- level completion drop;
- milestone rewards;
- Petal purchase;
- rewarded ads;
- IAP bundle.

## 7. Booster-use quality

A booster should solve a tactical mistake, not compensate for an impossible level.

## 8. Anti-trivialization

- cap Extra Slot stacking;
- Shuffle cannot endlessly reroll for free;
- Return consumes one charge;
- Bloom Burst is expensive/rare;
- ad rescue count per attempt limited.

## 9. Reference note

Third-party Colony Flow reference data currently lists:
- Extra Slot: L3
- Shuffle: L4
- Pick Up: L6
- Vacuum: L9

Bloomlings intentionally uses a comparable early unlock rhythm while applying its own theme and exact rules.
