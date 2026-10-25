# All-30 v9 synthesis

[Playable pilot](../../product/previews/gameplay-pilot-v9/README.md) · [Review and competitor basis](../../product/audits/pilot-v9-synthesis-review-2026-10-24.md).

`content/pilot-v9` is the reproducible final input. The first v9 is archived separately. Pinned base: v8E 51c7bd76; no v8 paths are written. `synthesis.mjs` reviews all 30 and authors ten replacement queues. `author.mjs` controls quotas/links; `scene-overrides.mjs` controls revised artwork; `fix-lib.mjs` controls hidden placement. `refine-quota.mjs` makes the recorded small, color-conserving L25 refinement.

`quality.json` records every source/disposition, primary planner/options/actions, timing witness, held-out checks, exact seeds and remaining warnings. `cf-patterns.mjs` maps all 30 to concrete observed competitor patterns. `review.mjs` measures initial reachable color volume as well as quotas and risk progress. It does not simulate arbitrary multi-color combinations as a single frontier.

```sh
node tools/gameplay-pilot-v9/rhythm-check.mjs
node tools/gameplay-pilot-v9/verify.mjs
node tools/gameplay-pilot-v9/authoring-check.mjs
python3 tools/gameplay-pilot-v9/build.py
node tools/gameplay-pilot-v9/review.mjs
node tools/gameplay-pilot-v9/clock-check.mjs
node tools/gameplay-pilot-v9/browser-check.mjs
node tools/gameplay-pilot-v9/browser-smoke.mjs
node tools/gameplay-pilot-v9/feedback-check.mjs
node tools/gameplay-pilot-v9/motion-check.mjs
node tools/gameplay-pilot-v9/slot-layout-check.mjs
node tools/gameplay-pilot-v9/storage-check.mjs  # consumes current browser-check results
python3 tools/gameplay-pilot-v9/package.py
```

Browser checks need Chromium and Playwright core at `tools/heroanim/node_modules/playwright-core`. Art sheets require Pillow/Numpy. No APK or production install is performed.

For content authors only: `node synthesis.mjs audit` records retained-source assessments; `node synthesis.mjs search ID [startAttempt] [bannedPatternsJSON]` searches a replacement; `node synthesis.mjs assemble` checks and assembles all 30 work records. Run from repository root using the full tool path. Searches are expensive and work/log files are ignored. Final JSON is authoritative: changing the generator may produce a different candidate from the same starting seed. The old `generate.mjs` entry point now redirects to this all-30 pipeline; it cannot silently restore the six-level prototype. Historical comparison scripts/reports describe the archived initial v9 and must not be rerun over those reports for synthesis analysis.

`rhythm.mjs` measures actual ×1/×2/×2.5 wall clocks with explicit 1.2 s reaction time. Patient, brisk and reserve policies are each measured at ×1/×2. Reserve deliberately keeps one free place while work remains; this is a diagnostic player strategy, not a rule change. Its voluntary delays and unconditional one/two-productive-place intervals are disclosed. A failed schedule is never replaced with another schedule's time.

Primary-route risk is checked before trying timing alternatives. Full-information patient continuation search distinguishes unknown from loss; unresolved branches are not proof. Chosen alternate routes may legitimately avoid risk. Held-out bot measurements are separate from search samples; neither predicts human retention. `rules.cjs`/`timeline.cjs` retain v8E mechanics. Memoization in `botsc.mjs` affects authoring only; cached states remain read-only. `compare-colony.mjs` replays model witnesses only for complete competitor inputs, without claiming identical competitor physics.

Slot positions are authoritative: a single admission fills the first hole; release leaves gaps; only an accepted pair compacts occupants in current left-to-right order. `compactSlots` is shared by settled and live admission. The DOM draws those exact positions. `slot-layout-check.mjs` uses a separate accounting-valid fixture to exercise holes, single/pair branches, rejected admission, browser reload and top-edge tube anchors; it does not alter the 30 shipped definitions.
