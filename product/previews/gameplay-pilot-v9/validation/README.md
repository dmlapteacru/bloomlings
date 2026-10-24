# Validation of the v9 browser experiment

These are automated model/browser checks, not human playtest results and not a proof of native Core equivalence.

- `engine.json`: 30 patient live certificates, 30 visible-horizon replays, 202 hidden-data swap checks, 240 concurrent random runs, 1,047 checkpoint restores, conservation, slot reuse, pair release, frame invariance and explicit changed-level gates. The 24 controls match v8E frozen at c6b0bfd4 definitions exactly.
- `browser.json`: 30 wins through actual DOM inputs; real exported commands replay in the live model, counts/trip events/reservations/releases agree, three-row visibility/mysteries/accessibility and responsive touch targets are checked. Clock accelerated for QA.
- `feedback.json`: all three waiting answers select, persist across reload and survive the real JSON download. Its HTML hash identifies the final artifact.
- `lifecycle.json`: modal/visibility pauses, reload and stream event logging.
- `clock.json`: hidden/dialog dwell excluded and clock regression rejected.
- `storage.json`: completed 30-level reports reload and export correctly within local-storage budget.
- `reference-times.json`: serial **model** milliseconds accumulated per command; it is not a wall-clock human duration. Use `quality.json` or the comparison audit for actual explicit timing policies.
- `artifact.json`: source/catalog, final HTML and phone ZIP identities, plus authoring memo comparison result.

`rhythm-check.mjs` also verifies exact ×1/×2/×2.5 wall-clock conversion, zero-time first command, idle occupancy, held linked halves and refusal of invalid schedules. `authoring-check.mjs` compares 18 policy/pattern results against the untouched v8E bot implementation on identical definitions, including repeated cached calls.

The replacement queues have winning brisk routes at both ×1 and ×2. Some preserved controls do not win under that particular brisk certificate schedule (L18); a failed schedule is reported as failed, without substituting a fabricated duration. Legal moves do not imply strategically safe moves. Early-error diagnostics remain optimistic on search-budget exhaustion.

Reproduction commands are in [tools/gameplay-pilot-v9/README.md](../../../../tools/gameplay-pilot-v9/README.md).
