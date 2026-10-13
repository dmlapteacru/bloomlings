# Approved cream secondary navigation

These are actual shared Skia renders from the implemented UI, not ImageGen screen mockups. Production surfaces are the generated `cream-bar-v1.png` and `cream-selected-v1.png`, both rendered with 9-slice. Text, icons, selection, dividers and touch targets remain code driven.

Shop uses one cream bar for Packs / Cosmetics / Animations; selected green with white text, inactive dark brown. Profile editor, family navigation and native Wardrobe kind filters use the same component. Source prompts and crop/slice metadata: `client/Assets/Bloomlings/UI/Resources/Navigation/SOURCE.md`.

Validation: client C# compilation with Unity API stubs, 37 existing layout/localization/pod-link tests passed. Seven preview fixtures (17,26,27,40,41,42,43) at 16x9, 19.5x9 and 21x9 passed the render, touch-target and safe-area checks (21 renders). Saved examples include all seven tall-screen views and selected short-screen views. No Unity Editor/device run or APK build was performed.

Reproduce with preview CLI: `--frames 17,26,27,40,41,42,43`.
