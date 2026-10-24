# Separate v9 gameplay experiment

See [design](../../product/audits/pilot-v9-plan-2026-10-24.md) and [playable pilot](../../product/previews/gameplay-pilot-v9/README.md).

Only `generate.mjs` authors replacements. Source artwork: `scenes.mjs` plus `scene-overrides.mjs`; quotas/links: `author.mjs`; hidden placement: `fix-lib.mjs`. It replaces six explicit ids and copies all other definitions/certificates from v8E frozen at c6b0bfd4. `assemble` rejects stale candidates outside current bounds.

`rules.cjs`, `timeline.cjs`, `idle-rules.cjs`, clock and palette retain v8E mechanics. `horizon.mjs` adds an optional reverse tie order, without additional visibility. `botsc.mjs` memoizes read-only authoring transitions; it does not affect the browser simulator. Cached result states must remain read-only.

```sh
node tools/gameplay-pilot-v9/rhythm-check.mjs
node tools/gameplay-pilot-v9/verify.mjs
node tools/gameplay-pilot-v9/authoring-check.mjs
python3 tools/gameplay-pilot-v9/build.py
node tools/gameplay-pilot-v9/comparison.mjs
node tools/gameplay-pilot-v9/clock-check.mjs
node tools/gameplay-pilot-v9/browser-check.mjs
node tools/gameplay-pilot-v9/browser-smoke.mjs
node tools/gameplay-pilot-v9/feedback-check.mjs
node tools/gameplay-pilot-v9/storage-check.mjs  # consumes browser-check results
python3 tools/gameplay-pilot-v9/package.py
```

Browser checks need Chromium and Playwright core at `tools/heroanim/node_modules/playwright-core`. Art sheets require Python Pillow/Numpy. Nothing installs the pilot into production or builds an APK.

`rhythm.mjs` measures event-time wall clocks with an explicit 1.2 s reaction interval, patient/brisk policies and actual ×1/×2/×2.5 transitions. It never replaces a failed brisk playthrough with a patient time. `compare-colony.mjs` rebuilds model witnesses for complete competitor inputs; it does not claim identical competitor physics.

The search records policy diagnostics, not human difficulty/retention. Early-error checking remains optimistic on exhausted budgets. Human tests must include recognition and waiting, not just elapsed time. The checked-in JSON/HTML are the reproducible input; seed-based regeneration is expensive and may pick a different acceptable candidate after generator changes.
