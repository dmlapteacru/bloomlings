# Profile — implemented arch and medal showcase

Reference: `../profile-inventory-concepts-v1/01-profile.png`, approved by owner. New editor direction is a proposal under `../profile-editor-shelves-v2/`; the live editor remains unchanged.

## Implementation

- Transparent illustrated body skin preserves the arch, climbing vines, signs, arrow/button faces, nine sockets and three shelf ledges as one composition. The whole skin fits uniformly to the available body area; decorative art is not stretched with nine-slice.
- All avatar images, chosen frames/badges, profile name, ID, joining month, level, medal art, counts, completion/lock state, achievement names and pagination are runtime content. Existing background and top navigation are reused.
- A separate leaf frame skin reproduces the reference material on main Profile while retaining selectable frames. The editor's existing frames/summary/layout are unchanged.
- Native Unity and playtest share `ProfileGameDesign` source-space geometry. Larger arrow/pencil touch areas are separated from portrait and medal targets. Long achievement names fit inside the caption tags. Native scene labels follow the same font-height cap as the preview.
- The last achievement page leaves empty physical sockets for missing items. Earned badges still equip on tap; all three pages remain reachable. No new economy, rewards or mechanics.

## Verification

Native client compilation against Unity API stubs succeeded. 33 relevant .NET tests passed (Profile layout/service, achievements, purchase confirmation and number formatting). Cached NUnit 4 was used via a temporary local build project because the normal NUnit 3 dependency is unavailable offline; production project files were not changed. An exploratory run also exposed a pre-existing AssetSlotTests rule that permits only frames 1–17 despite the registry already containing Shop frames 54–56. That unrelated stale test is excluded from the scoped passing run.

21 fixture/shape render checks passed: 39,57,58,59,60,63,65 on 16x9,19.5x9,21x9. After final portrait and caption tuning, main Profile and last achievement page were rendered and checked again on all three shapes. Tall/short main Profile and last page were visually reviewed. Unity Editor/device/APK execution was not performed.

Curated captures: main Profile on three shapes, the old profile fixture with different customization, and the last achievement page. All assets were created with built-in ImageGen; exact prompts are in `prompts.json`, source provenance and hashes in the Shop art records. Original alpha remains unchanged.
