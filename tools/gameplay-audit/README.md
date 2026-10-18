# Gameplay audit

Read-only analysis of the installed Bloomlings catalogue using the real `LevelSession` rules. It loads curated L1–10 and catalog L11–2500, matching the launch build. It never changes levels, pictures, saves or runtime code. Requires the existing .NET 10 SDK; the summary uses Python 3's standard library. No image API or paid service is involved.

From the repository root (PowerShell or Bash):

```sh
dotnet run --project tools/gameplay-audit -- 1 100 256 full
dotnet run --project tools/gameplay-audit -- 101 2500 16 full
dotnet run --project tools/gameplay-audit -- 1 2500 1 sequential
dotnet run --project tools/gameplay-audit -- 1 2500 1 doors
python tools/gameplay-audit/summarize.py
```

Arguments: `first last trials mode`. Outputs and build products stay under ignored `content/work/gameplay-audit/`. The first two commands can be split into non-overlapping level ranges and run in parallel. The summarizer requires complete L1–2500 coverage by default; `--expected N` supports a shorter L1–N sample. An overlapping rerun keeps the largest trial sample, not the sum of identical seeded trials.

Modes:

- `full`: random exposed legal taps; always choose the leftmost legal top; random choice among pods whose visible variant is on the currently reachable frontier (otherwise random). Only the two randomized policies use `trials` repetitions.
- `sequential`: one run each for leftmost, rightmost, round robin and the largest-charge pod of a currently reachable visible variant (otherwise the largest legal pod). Ties follow visible column order. These policies never inspect hidden variants or future layers and never compare internal pod IDs.
- `doors`: replay the stored solution on the original definition, then on a counterfactual version where one opening gate's footprint stays permanent stone. Remove its administrative trigger goal and its own key/lock pair. Keep all target work, pod charges, tray order and other mechanics. A success certifies that opening that footprint was unnecessary for this solution; a failure does not prove the gate essential. The original solution and unchanged accounting are required controls. Other lock types are outside this experiment.

Trials use `Random(unchecked(level * 1000003 + trial * 101 + 17))`; the seed resets for each policy. Only legal exposed top pods are candidates. `SearchChild` applies the real fixed-point rules without building visual event logs. Boosters, ad rescue and auto-win logic are absent. For L1–10, memoized enumeration also computes the exact probability of winning under uniformly random legal taps and counts complete winning/losing orders.

The summary checks the definition/picture hashes across strategies and the validation definition hash, rejects unfinished trials, and reports both sample sizes and **equal-weight mean per-level win probabilities**. This avoids overweighting the 256-trial early levels compared with the 16-trial later levels. Rates are properties of these policies and this content snapshot, not measured probabilities of a person winning.

Stored `unsafeChoicePermille` and `peakBuffer` values come from old validation files. The old files omit metric completeness, so zero unsafe choices must not be read as a proof that no dangerous choice exists. Observed buffer peaks in the JSONL are after automatic settling; the old solver's peak includes the just-committed pod before settling. Do not compare those two peak definitions as if identical.

For an archived report:

```sh
python tools/gameplay-audit/summarize.py --out product/audits/gameplay-2026-10-18
```

The input fingerprint identifies the actual definitions/pictures, independently of later commits containing only audit documentation. See [the audit](../../product/audits/gameplay-vs-colony-flow-2026-10-18.md) and [the proposed redesign](../../product/plans/gameplay-evolution-v2.md).
