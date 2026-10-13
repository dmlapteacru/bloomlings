# Primary actions, reference Play and progress

Owner revision, 2026-10-14. Final direction: reproduce the rounded Play and compact progress banner in `reference-play-progress.png`. The organic silhouette was rejected and is not used. Captures are rendered from shared game code.

## Changes

- Separate floral Play asset, broad rounded domed green face, live Lilita text with cream fill and brown outline. Home action proportions are adapted around this taller face and compact reward banner. No backgrounds or character art changed.
- The new green primary atlas is used by Save, Next, Resume, Claim and other standard green actions. Decorated actions use its floral variant; ordinary actions use its plain variant. Explicit alternate-color actions retain their existing skins. Purchase price buttons are unchanged. Native GardenButton callbacks, press/spring, breath and disabled fade are retained. Avatar editing's bottom Save/Buy action now uses the shared primary button; its card prices keep the shop purchase skin.
- Home progress uses a separate cream/gold gift banner, live localized remaining-level text and seven leaf markers. Fill derives from the previous configured milestone to the next one, rather than the larger cadence tier; reward logic is unchanged. The lower Home group preserves touch space for the optional free-booster offer.
- Shop catalog clipping extends behind the fixed upper tabs and family navigation. Top padding preserves initial card positions and maximum scroll offsets. Tabs draw above content and intercept tap/drag starts.
- The bottom menu has a continuous stone backing behind every tile and gap, in addition to its front ledge. Shared stone rows continue through the bottom inset. The visible backing blocks covered catalog input; selected navigation does not trigger an underlying purchase or drag.

## Validation

Native scripts compile against Unity API stubs and 62 relevant .NET tests pass, including milestone progress boundaries. 33 fixture/shape checks passed: 3,4,11,15,16,17,26,41,42,63,71 on 16x9,19.5x9,21x9. Two final Cosmetics/Animations renders recheck content culling at the new upper viewport edge. Three action fixtures (58,59,61 on 19.5x9) check actual badge/name saving and avatar purchase cancellation. Gesture fixture 71 verifies upper tabs, selected menu, stone front and gaps block catalog drags while content scrolls normally. Save/profile fixtures retain their live editing flow.

Assets use shared runtime crops and nine-slice borders in Unity, Skia and Android; original generated RGBA pixels are preserved. Exact prompts/source outputs are saved in `prompts.json`; manifest version 6 records dimensions and hashes. Temporary offline projects use cached assemblies/NUnit and are removed after verification. Native Unity/device execution and a zero-pixel-difference comparison were not performed.
