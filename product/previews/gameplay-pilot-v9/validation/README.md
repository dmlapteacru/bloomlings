# Validation of the all-30 v9 synthesis

Automated model/browser checks, not human playtest results or native Core equivalence. `artifact.json` identifies the exact final catalog, HTML and ZIP.

- `engine.json`: all-30 review gates, 30 patient certificates and 30 visible-horizon planner replays; 207 hidden-data swap checks; 240 concurrent random runs; 1,127 checkpoint restores; conservation, slot reuse, pair release, frame invariance and mystery rejection/checkpoint fixtures. Primary-route decisions are checked independently of the selected timing route. All six timing reports are recomputed. Rebuilt queue bot bounds, held-out measurements, phase spread and number-only shortcut limits are checked.
- `browser.json`: 30 wins through actual DOM input; exported commands replay in the live model; counts, trips, reservations and releases agree; three-row visibility, mysteries and 36 responsive layouts checked. Browser clock accelerated for QA.
- `motion.json`: both linked pods fly together; decorative arrivals never commit again; tube strokes use neutral material rather than revealing hidden colors.
- `feedback.json`: all waiting answers select, persist across reload and survive JSON download.
- `lifecycle.json` / `clock.json`: modal/visibility pauses, reload during stream, event logging and clock regression checks.
- `storage.json`: 32 attempts, 30 completed levels restored and exported; compact local storage 3,144,993 bytes.
- `reference-times.json`: serial model milliseconds accumulated per command, not wall-clock human duration. Explicit six-policy event timing is in `content/pilot-v9/quality.json` and the synthesis review.
- `artifact.json`: v8 file hashes verified unchanged, and initial v9 HTML/ZIP archive verified byte-identical to 128c3196. It also records the 18 authoring memo equivalence comparisons against v8E.

`rhythm-check.mjs` verifies exact ×1/×2/×2.5 wall-clock conversion, idle occupancy, held linked halves, reserve-policy voluntary delays and rejection of invalid schedules. Every level has a winning brisk or reserve route at both ×1/×2; other schedules can fail and remain reported as failures. Reserve is a diagnostic player policy, not a new mechanic. It keeps one place free while pending work remains; its withholding and unconditional one/two-working-place phases are measured separately.

Risk means full-information patient continuations on a specified route. It is not universal live fairness; a selected alternate route may avoid risk. Unknown search outcomes are not wins/losses. Policy rates and measured durations are not human difficulty or retention. Preserved L10/L15/L21 retain explicit pacing warnings.

[Commands](../../../../tools/gameplay-pilot-v9/README.md) · [All-30 construction review](../../../audits/pilot-v9-synthesis-review-2026-10-24.md).
