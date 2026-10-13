# Garden gameplay, revision 6

Actual shared-code Skia renders, not concept mockups or Unity device captures.

- Exposed icons are about 9% smaller than revision 5, with space on all sides inside the upper face. Covered icons retain their near-half-card height.
- Counts move up toward the lower inner edge: exposed count boxes rise by 10.5% of pod height; covered ones by 3.5%. Font size and right alignment remain unchanged.
- The lower side is one continuous plane with a smooth shadow gradient. Repeated seam/reflection bands are removed; the contour highlight is softened.
- Pod rim colors use slightly reduced saturation and 10% darkening. Warm cream interiors retain the original variant tint; source icons and board colors remain unchanged.

Validation: native runtime/UI importer compile against Unity API stubs has zero warnings/errors; 30 existing layout/link tests passed. Four fixtures at 19.5:9 passed the render harness: normal/hard/super-hard gameplay and pod states. Inspected hard gameplay and all pod states.

Background artwork, navigation, boosters, board geometry and game rules are unchanged. Source PNGs are unchanged. Unity Editor/device and Android APK builds were not run.
