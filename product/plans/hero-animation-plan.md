# Hero animation ideas — recorded 2026-10-16

Owner-requested plan, not a completed feature or final pricing specification. Twig is the preferred reference for clean
material/texture quality. Updated responsibility split (2026-10-17): the owner maintains hero/accessory geometry in
one Blender master; the assistant supplies textures and animations for import into it. See `hero-asset-workflow.md`.

## Free baseline

Six actions per hero, reusing existing clips where they fit:

| Action | Motion | Context |
|---|---|---|
| Idle | Breathing and restrained sway | Home, profile, waiting |
| Curiosity | Look around, tilt head, notice a neighbour | Home and manual action |
| Greeting | Wave or whole-body greeting | Home and manual action |
| Walk | Short steps and characteristic body bounce; reusable loop | Leave/return on Home |
| Joy | Jump, open arms or a personal celebration | Win and manual action |
| Sadness | Lower head/shoulders, then recover gently | Loss and manual action |

Personality: energetic Sprig, expressive Bloom, springy Drop, slower/unhurried Twig. Use distinct performances on the
existing rigs rather than identical motion copied between different bodies. Small walks and hand contacts need tuning;
facial deformation is a separate task and is not promised by this baseline.

## Home scenes

- **Hello, neighbour:** Sprig turns/waves at Bloom; Bloom notices and answers with a happy little hop.
- **Are you asleep?:** Twig starts dozing; a neighbour leans toward him; Twig wakes and looks around sheepishly.
- **Be right back:** one hero walks behind the plants near the fountain, disappears briefly, then returns. A neighbour
  may watch. Use a path and scene-layer occlusion, not an arbitrarily shrinking/fading hero.

One automatic scene about every 20–30 seconds, at most two active participants; the other heroes remain calm. Treat
this interval as an initial tuning value. Synchronize separate clips/positions. Defer hugs and exact object hand-offs.
A user action should take priority over an automatic scene; a temporarily absent hero needs a clear return/state rule.

## Manual action menu

On tapping a hero on Home or the win screen, show 3–4 small illustrated action buttons around the hero, inspired by the
Sims interaction flow and styled for this game's garden UI. Initial actions: Greeting, Joy, Curiosity and an equipped
special emote. Return to idle after playback. Support a growing action library without filling the screen with buttons.
Minimum win/loss reactions are free and automatic; manual emotes never affect level rewards or gameplay strength.

## Initial paid emotes

- **Signature dance:** several expressive moves, a small turn and a finishing pose.
- **Heart:** hands toward the chest or an air kiss, plus a heart effect; adapt to each rig's limbs.

Suggested purchase unit: an action usable by all four heroes, with a performance per family. Pricing and final purchase
unit remain to be decided. Keep baseline idle/greeting/win/loss free. Reserve the same unlock/equip system for later
event rewards, but event design and implementation are deferred.

## Implementation order and constraints

1. Free win/loss reactions, greeting and manual action menu.
2. Home interactions and the leave/return walk.
3. Two special emotes, ownership/unlock/equip and purchase integration.

Keep the game flat 2D: animate the rig offline, render sprites with attachment anchors, then play short clips and paths.
Reuse short loops/holds/rest poses instead of baking every second of a long scene. Measure compressed frame payload,
decoded frame-cache use and transition behaviour before expanding the library. Do not bake every clothing combination.
Existing Sprig animation experiments: `tools/heroanim/LAB.md`. Implement equipment first; the action menu and animation
library are a separate feature after the current modular Sprig integration.
