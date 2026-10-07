# The level catalog

The certified levels of the game, `level-NNNNN.json` in `levels/` with their validation records in `validation/`
(`contracts/pipeline-cli.md`, data-model §1.3). `publish --catalog content/catalog` packs them once the catalog holds
Levels 1..N without gaps.

- **L1–10** are the curated onboarding levels (`content/curated/`), the same bytes under five-digit names, with their
  validation records.
- **The showcase and practice levels** come from `content/showcase/` as they are.
- **Every other level from L11** is generated band by band with `tools/catalog/build-catalog.ps1` or `.sh`
  (`tools/catalog/README.md`): each band's profile, seed 1, its fixed segments, the earlier bands as history, and up to
  two more seeds for a level that gets no accepted candidate.

`build-manifest.jsonl` records every band as it lands: profile, seed, segments, retries, the jobs used (they never
change the levels), the levels generated and kept, the seam repairs, the fills (`"level:seed"`), the gaps, the time,
the validation counts and the pipeline commit. To rebuild a band, or to check that another machine gives the same
files, run the script with `-Check <band>` / `--check <band>`.

Do not edit these files by hand: a changed level needs a new `definitionVersion` (FR-076), and `diff` refuses
unversioned changes.

## Status

| Band | Levels | Generated | Kept (showcases) | Fills | Gaps | Time | Validation |
|---|---|---|---|---|---|---|---|
| curated | 1–10 | | 10 | | | | 0 errors |
| 0011-0025 | 11–25 | 8 | 7 | L21 seed 3 | none | 1362 s | 0 errors |
| 0026-0050 | 26–50 | 21 | 4 | none | none | 78 s | 0 errors |

Built with `tools/catalog/build-catalog.sh --jobs 3` on a 4-core cloud machine: L1–25 on 2026-10-06 (pipeline 97f1dc6),
L26–50 again on 2026-10-07 after the picture library gained its lime and red roles (Vine joins at L45, Berry at L200),
with the faster search (L11–25 regenerated with it matches file for file). L26–44 came out the same as before, since
the picker keeps each expansion picture out of the levels before its variant joins; L45 introduces Vine on its own and
L46, L48 and L49 use it again. `validate --catalog content/catalog --context content/curated` passes with 0 errors over
L1–50, and `score` reports no disagreement. The warnings are the provisionally approved readability pairs
(`content/readability/approved-pairs.json`) until the human readability sign-off. Still open for Levels 1–100: a person
playtests every level (FR-084).

**Next.** The owner builds L51–5000 with `tools/catalog/` (the README there), which resumes after L50.
