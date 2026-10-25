# Pilot v9 synthesis

All 30 levels reviewed against concrete Colony Flow rounds. Pinned base: v8E 51c7bd76. All v8 catalogs remain immutable; the first v9 is archived in `content/pilot-v9-initial`.

14 definitions differ from the first v9; 10 queues were rebuilt. L4/L10 adopt initial v9. Remaining levels adopt the reviewed latest v8E definitions. `levels.json` identifies both hashes and actual changed ids.

- `certificates.json`: winning visible-horizon routes and planner settings.
- `quality.json`: all-30 dispositions, concrete competitor references, primary and selected route decisions, independent held-out bot checks for rebuilt queues, six timing policies, explicit remaining warnings.
- `baseline-v8e-51c7bd76/`: pinned current base; older c6b0bfd4 snapshot remains historical.

[Playable build and comparison guidance](../../product/previews/gameplay-pilot-v9/README.md). [All-30 review](../../product/audits/pilot-v9-synthesis-review-2026-10-24.md).

Rebuild from checked-in definitions without regenerating levels:

```sh
python3 tools/gameplay-pilot-v9/build.py
node tools/gameplay-pilot-v9/verify.mjs
python3 tools/gameplay-pilot-v9/package.py
```

Authoring commands and limits: [tools README](../../tools/gameplay-pilot-v9/README.md). Nothing installs this experiment into the production launch catalog. Bot rates, full-information patient decisions and automated clocks are diagnostics, not human difficulty or native Core equivalence.
