# 02 — Source Tray + Waiting Buffer

**Status:** LOCKED (2026-09-29)

## 1. Reference model

The Source Tray / 5-slot Buffer follows Colony Flow-like source-box logic.

The player chooses numbered source groups, not permanent hero buttons.

## 2. Source Pod identity

Every pod is tied to one exact target variant.

Pod must communicate:
- family character/silhouette;
- exact target icon;
- exact target color;
- count;
- linked/locked/mystery state.

Example:
- Leaf Sprig ×16
- Moss Sprig ×22

Both use Sprig-family animation but are different gameplay pods.

## 3. Source Tray

Multiple stacks/columns of pods.

Only exposed/front pod in each stack is selectable.

Removing one reveals the next.

The number of stacks varies per level.

## 4. Visibility

Ordinary:
- variant visible;
- count visible.

Locked:
- variant/count visible;
- lock visible.

Mystery:
- count visible;
- variant hidden.

Connected:
- visible link across all connected pods.

## 5. Waiting Buffer

Exactly 5 slots by default.

Slots show:
- pod variant;
- remaining count;
- waiting/active state.

No manual reorder.

## 6. Continuous play

While Bloomlings work, player may commit more exposed pods if slots are available.

This creates flow and risk.

## 7. Stuck pod

Pod is stuck if:
- count > 0;
- zero cells of its exact target variant are reachable.

It stays until matching work becomes reachable or player uses recovery.

## 8. Same family ≠ same type

Important UX rule:

If Leaf Sprig and Moss Sprig are both in Buffer:
- they must remain visually distinguishable;
- each only responds to its exact matching target.

Family art reuse cannot blur gameplay identity.

## 9. Connected groups

Two or more pods linked.

On tap:
- all enter Buffer together;
- each consumes one slot;
- each behaves independently afterward.

Connections can mix families/variants.

Example:
`Leaf Sprig ×12 — Dew Drop ×9`

## 10. Locked pod

Cannot be selected until unlock condition.

May block deeper pods.

## 11. Mystery pod

Shows `? + count`.

On commitment:
- exact target variant is revealed;
- remains fixed for the level.

Fairness solver must ensure hidden variant does not create forced coin-flip failure.

## 12. Locked Waiting Slot

Advanced custom mechanic:
- one of 5 slots unavailable until unlocked.

## 13. Extra Slot

Booster:
- adds temporary sixth slot.

## 14. Jam

Trigger only when:
- all usable slots full;
- every pod stuck;
- no automatic state change pending.

## 15. Source depth

Typical:
- tutorial: 3–7 pods;
- early: 6–12;
- standard: 10–24;
- advanced: 15–30+ if readable.

## 16. Group sizes

Because boards may contain 100–300+ layers:
- small: 5–15
- medium: 16–40
- large: 41–100
- exceptional: 100+

## 17. No permanent hero-selection row

There is no fixed UI row with four hero buttons.

Target variety is expressed through actual Source Pods.
