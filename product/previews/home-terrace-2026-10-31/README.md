# Home on the terrace (2026-10-31)

The owner: a new background (their sunlit stone terrace), no fountain, the heroes placed on the terrace at different
distances and sizes, and the floating buttons (No Ads, Daily, …) in the reference game's style: a gentle outline, a
one-colour back, the animations a little out in front over the outline, a little smaller.

- `home.jpg` is the terrace (941 × 1672); the fountain's basin, lotus and front bushes are gone (`HomeLayers`, both
  builds, `tools/heroanim/layers.json`, `backdrops.json`).
- The heroes far to near: Bloom by the balustrade (0.165 of the width tall), Drop by the bench (0.205), Twig by the column
  (0.22), Sprig in front (0.31) (`HomeLayers.Placement`).
- The floating buttons: a round badge in `ui.promo.badge` (#3F86D8) in a white outline, the scene's props on it scaled
  0.85 and reaching out over the outline, the label in white on a pill (`ui.promo.badge_label`); the badge is 0.62 of the
  old scene's width.

The owner's second round the same day ("the badges' back too contrasting, more neutral; their content bigger, out past
the outline; the heroes closer and bigger"):

- `ui.promo.badge` is a quiet grey blue a little deeper than the sky (#94B8D6, was #3F86D8), the label pill a muted
  deeper blue (#5F84AD) for its white letters.
- The props on a badge are scaled 1.25 about their feet (was 0.85) and stand 0.95 as far apart across
  (`HomePromo.BadgeSpread`), so they gather over the badge and reach out over its outline at the top and the sides; the
  Daily Challenge's badge is its box's width, its board 0.9 of the badge and raised so the sun and top corners stand out.
- The heroes far to near: Bloom 0.22 of the width tall, Drop 0.28, Twig 0.31, Sprig 0.4, their feet lower (closer);
  Twig's hat clears the Challenge label on a 16:9 screen.

Previews: `home-progressed-19.5x9.png`, `home-early-19.5x9.png`, `home-16x9-19.5x9-21x9.png`, `badges-19.5x9.png`.
