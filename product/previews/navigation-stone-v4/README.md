# Stone navigation and Home portrait

Owner revision, 2026-10-14. Captures are rendered from the shared game code.

- Home no longer draws the name ribbon or player name. Avatar, selected frame and optional live badge remain, and tapping the portrait opens the full profile with the stored name.
- All five bottom-navigation destinations rest on the illustrated cream limestone wall from `reference-menu.png`. Existing button atlas, caption, selected state and unlocks remain. The wall extends below the display edge. Source PNG pixels are preserved and cropped at runtime identically in Unity, Skia and Android.
- Packs, Cosmetics and Animations clip their scrolling catalog at the physical bottom of the screen rather than above the navigation. The menu renders in front. Bottom padding retains the original maximum scroll offset so final cards can still be lifted above the menu. Header and secondary tabs remain fixed.
- The native navigation raycast surface now matches the visible stone wall, and selected tiles consume input without navigating. The playtest blocks taps and drag starts on visible foreground menu surfaces. Render checks exclude only labels fully covered by the opaque stone ledge.

Validation: native scripts compile against Unity API stubs; 61 relevant .NET tests passed. 21 fixture/shape checks passed (17,26,42,66,67,68,71 on 16x9,19.5x9,21x9), including no-name Home, selected frame/badge, all three Shop tabs and scrolled catalog behind navigation. Fixture 71 checks that dragging selected navigation or the wall cannot scroll the catalog, while dragging content does scroll. Native Unity/device execution was not performed.

Asset generated with built-in ImageGen. Exact prompt, source output and saved asset path are in `prompts.json`; hash and dimensions are in the Navigation manifest. Temporary offline build projects are removed after validation.
