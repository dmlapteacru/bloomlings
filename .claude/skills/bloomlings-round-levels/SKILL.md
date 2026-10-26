---
name: "bloomlings-round-levels"
description: "Turn the owner-approved Bloomlings round pictures into solved levels: pod queues, mechanics, a saved winning solution, the random-test difficulty class, placement into the difficulty calendar, level cards, and a list of positions still needing pictures. Use after pictures are approved, or when the owner asks to generate/build/place levels or rounds, fill levels N–M, or re-check levels after an engine change."
argument-hint: "Which levels to fill or which pictures to use (e.g. 'levels 31–60' or 'all approved pictures')"
user-invocable: true
disable-model-invocation: false
---

# Round levels (step 2 of 2)

This is the second half of process §13 of
[`product/17_LEVEL_GENERATION_RULES.md`](../../../product/17_LEVEL_GENERATION_RULES.md): §13.2–§13.10, plus the
acceptance and card of §14.

Read 17 §1, §3, §5–§8, §10, §11 and §14 before building, and the section of each mechanic you use in the full catalog
[`product/design/Bloomlings-gameplay-mechanics.md`](../../../product/design/Bloomlings-gameplay-mechanics.md). The pictures come from the skill
`bloomlings-round-pictures` and must be `approved`. Where this skill and 17 differ, 17 wins.

**Where the pictures are.** `content/rounds/approved/` (built by `pictures.mjs publish`):
- `locked/L0001`–`L0003`: the pictures of levels 1–3, used there and nowhere else.
- `pool/P####`: every other approved picture. They are interchangeable between rounds. A picture's number, its
  `forLevel` or the order of the pool never assigns it to a level.
- Each code is **free** or **in a round** (`state`, `usedBy`; `pictures.mjs status --free`). Build and place use only free
  codes. `place` marks a code as used by writing its level, then rebuilds the folder. A code becomes free again only when
  its level file is removed or rebuilt with another picture.

## Principle

Pictures are not tied to levels (except the locked levels 1–3).
- A picture is built into candidates of every class the rules allow, and the **measured** class decides where a
  candidate may go. A picture that turns out Normal goes to a Normal position, Hard to Hard. The calendar does not
  turn a picture into a class.
- A picture that fits no open position stays in the **bank** for the next calendar iteration.
- Positions left empty are written to `content/rounds/needs.json`. For them the picture skill draws new pictures.
- Already made pictures are not forced into a position.

## Calendar (17 §5, §9)

- Every 15th level is Super Hard, other multiples of 5 are Hard, the rest Normal.
- Mechanic introductions are two Normal levels each, alone, with a random-test target of 90–100 %: hidden pods 6/8,
  links 11/13, Sprig bridge 16/18, latch + acorn key 51/53, canal 101/103, shutters 151/153, repair 201/203, bud
  251/253, seed 301/303, well 351/353, hidden cells 401/403, stacks 451/453.
- A type is not used before its first introduction, nor between its two introductions. From the third appearance it
  may combine with known types.
- L1–L5 have no extras. L1 must have no losing choice; L2–L3 aim at 90–100 %.
- `node tools/rounds/levels.mjs calendar <from> <to>` prints class, introduction and allowed types per level, and
  marks types the engine does not play yet.

## Engine support (17 §14: every mechanic used must really be supported)

`tools/rounds/engine.mjs` is the pilot v9 engine (v8 base, slot positions of §1) with the §8 link rule.
- **It plays:** hidden pods, linked pairs, Sprig's bridge.
- **It does not play yet:** latch + acorn key, canal, shutters, repair, bud, seed, well, hidden cells, stacks
  (`SUPPORTED` in `tools/rounds/lib.mjs`).

The builder only uses supported types. For a position that needs an unsupported type, stop and tell the owner:
- which levels need it (the `calendar` output marks them);
- that the engine and the playable pilot need that mechanic first, per its §8 contract.

Do not fake a mechanic with another one: a bridge is not a canal, shutters are not a bud. Levels up to 50 need only
the supported three.

## Steps

1. **What to fill.** The owner's range of levels; otherwise the next empty positions after the last placed level
   (`content/rounds/levels/`). Run `calendar` for the range and check engine support.
2. **Build candidates.**
   - Command: `node tools/rounds/levels.mjs build --approved` (or a list of `P####` ids). Options:
     `--classes Normal,Hard,"Super Hard"`, `--recipes none,links,hidden,links+hidden[,bridge,bridge+links]`,
     `--candidates 64` (the start budget per picture and class, 17 §13.5), `--seed`.
   - Run it in the background for big batches; it logs per picture/class/recipe how many candidates were found and
     the reasons for the rest.
   - For each candidate the tool:
     - splits the work of each material into pods by §3. Numbers are 1–60; a colour of 20+ gets at least 2 pods; the
       largest pod is min(60, ceil(volume/2)); the split is near even; the budget of pods follows W. L1–L3 use the
       fixed splits of §4.
     - orders the pods by the real access depth of their cells with class-dependent variation, and pushes one colour
       deeper on Hard/Super Hard (§6). It deals the pods into 3 or 4 columns by N, with start heights within 1.
     - adds the recipe's mechanics at the doses of §7 (one type: full range; two: the additional at its minimum;
       small queue under 16 pods: one type). Introductions use teaching doses: 1 hidden pod, 1 pair, 1 bridge choice.
       Hidden pods start from the second row and are never in a group.
     - checks the exact balance of every material and the pod rules.
     - screens with 40 random games, then saves a winning solution: a full-information search, replayed on the live
       timeline. It also records the same actions tapped at once, as the lower bound of time.
     - runs the §5 random test: 100 games, widened to 400 when the Wilson interval crosses a border; uniform over
       legal actions plus waiting before a scheduled event; ×1.
     - rechecks the doses against the **measured** class.
     - checks fairness of hidden pods: every world where the hidden pods swap material and count must still win after
       following the saved solution until a differing pod shows.
     - runs the simple strategies of §11 (left/right hero choice) and the rhythm by fifths of W of §10.
   - Accepted candidates go to `content/rounds/bank/P####.json` with all measures. A picture whose pods cannot fit
     §3 is recorded with its reason.
3. **Read the reasons.**
   - "Не тот класс" or "ниже 5 %": that class did not come out of this picture within the budget. This is normal: it
     stays available for other classes.
   - "Решение не найдено за бюджет" means **not checked**, not impossible (17 §1, §13).
   - Do not widen the board, add every mechanic or weaken a check to fill a number (17 §13).
4. **Place.**
   - Command: `node tools/rounds/levels.mjs place <from> <to>`.
   - It fills each empty position with a banked candidate of exactly that class whose picture fits the level's stage
     (§2). Only allowed types are used, introductions only by their own introduction candidates, L1–L3 only by their
     fixed pictures.
   - It keeps the §11 variety rules (no lead mechanic or identical recipe more than 2 levels in a row), at most 2
     pictures with 10–11 colours in any 20, and prefers the class's preferred zone (75–95 % for the first Normal after
     a Hard or Super Hard) and the size mix per 30.
   - It writes `content/rounds/levels/L####.json`: the definition in the pilot format plus the card data. It marks
     the picture used and lists empty positions in `content/rounds/needs.json`.
5. **Look at the result** (17 §13.9).
   - Check each card with `node tools/rounds/levels.mjs card <n>`.
   - Watch for: a dull or overloaded finish (launches after 80 % W should be 1–4, up to 6 on a long Super Hard; last
     launch at about 80–92 % W), empty waiting, and Hard or Super Hard won by all simple strategies (check the
     marking).
   - When a level should be played to judge it, say so. Bot results are not human win rates.
6. **Report and loop.**
   - Report to the owner the filled levels with class, picture, recipe and random-test share, then the empty
     positions from `needs.json`.
   - Suggest `bloomlings-round-pictures` for those needs, with each need's class, stage and required mechanic.
   - Leftover banked candidates are used in the next calendar iteration.
7. **Commit** `content/rounds/bank`, `content/rounds/levels`, `content/rounds/needs.json` and the manifest. The levels
   are not installed into the production catalog or the Unity client by this skill.

## Acceptance (17 §14) — what must hold for every placed level

- **Picture:** approved and recognisable.
- **Balance:** exact per material; pods and groups within the rules.
- **Mechanics:** supported by the round engine.
- **Solution:** a complete solution without boosters at the real places (5; 4 → 5 once repair exists), replayed by the
  engine.
- **Fairness:** no forced guessing of hidden values; no short time window.
- **Class:** confirmed by the single random test, or a borderline case (`borderline: true`) shown to the owner.
- **Introductions:** the introduction order is respected.
- **Card:** carries everything §14 lists: picture, number, class, N and W, materials, P, columns, recipe and doses,
  random test, saved solution, buffer by fifths, simple strategies, launches after 80 % W, time.

After any change of movement, slot release or bot policy, re-run the random test of the placed levels (17 §5). A level
whose class moves is re-placed.
