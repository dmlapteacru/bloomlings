# Contract: Design Tokens

The named values of the board's visual language (FR-005, research R6). They are implemented once, engine-free, in
`client/Assets/Bloomlings/UI/Design/DesignTokens.cs`, and used by the Unity client and the full playtest. Screens use
token names, never literal colors or sizes. Final art may retune the values here without touching screens.

Sizes are in **reference units**: 1 unit = 1 px on a 1080-px-wide portrait screen. Clients scale them by
`screenWidth / 1080`, capped so tall tablets do not blow up the UI.

## Colors

| Token | Hex | Use |
|---|---|---|
| `surface.panel` | `#FFF9EE` | cards, popups, bottom sheet, Store and Leaderboard rows |
| `surface.panel_edge` | `#E8DCC4` | lower edge and outline of cards |
| `surface.sunk` | `#F1E8D6` | empty slot, inner wells, unselected tabs |
| `surface.row_highlight` | `#DDF2CF` | the player's own Leaderboard row |
| `surface.scrim` | `#1E2430` at 55% | dimmed backdrop behind popups and the jam sheet |
| `text.primary` | `#2E3440` | titles and body text on light surfaces |
| `text.secondary` | `#6B7280` | captions ("Completed at Level 10", "New today") |
| `text.on_color` | `#FFFFFF` | text on green buttons, pills and badges |
| `text.outline` | `#2E3440` at 35% | soft outline of text on color |
| `button.primary` | `#5DBB46` | PLAY, NEXT, CLAIM, RESUME, CONTINUE, Free rescue |
| `button.primary_top` | `#7ED35F` | the lighter top half of primary buttons |
| `button.primary_edge` | `#3D8B2F` | the darker lower edge of primary buttons |
| `button.secondary` | `#F4EAD5` | RESTART, SETTINGS, HOME, Restart, "×2 reward", "Get +N" |
| `button.secondary_edge` | `#D9C9A6` | the lower edge of secondary buttons |
| `button.icon` | `#FFFFFF` | round Settings, Pause and close buttons |
| `button.icon_edge` | `#C9CED8` | their rim |
| `button.icon_glyph` | `#3A4050` | their glyph |
| `button.dark` | `#3A4050` | the 2× pill |
| `pill.level` | `#8FC6F0` | the "LEVEL N" pill (Normal and Hard) |
| `pill.level_edge` | `#5E9FD3` | its lower edge |
| `pill.level_super_hard` | `#B59AF0` | the "LEVEL N" pill on Super Hard |
| `pill.petals` | `#FFFFFF` at 88% | the Petals balance pill |
| `pill.petals_edge` | `#E3DCEF` | its rim |
| `badge.hard` | `#E5484D` | HARD badge |
| `badge.super_hard` | `#8E4FD8` | SUPER HARD badge |
| `badge.count` | `#2F3A4A` | booster count badge, pod count pill |
| `accent.plus` | `#5DBB46` | the green "+" on the Petals pill and "+ Slot" |
| `petal.fill` | `#F58DB8` | the Petal symbol |
| `petal.center` | `#FFD35C` | its center |
| `petal.edge` | `#D8639A` | its outline |
| `state.danger` | `#E5484D` | the danger slot's dashed frame, the jam-risk mark |
| `state.lock` | `#8C8F99` | padlocks |
| `state.lock_bg` | `#C4C7CF` | the locked slot and locked pod card |
| `state.stuck` | `#9AA0AA` | greying of stuck pods and next-in-stack pods |
| `state.link` | `#6CC4B8` | the bar joining connected pods |
| `medal.gold` | `#F5C542` | rank 1 |
| `medal.silver` | `#C9D1DC` | rank 2 |
| `medal.bronze` | `#DA9A62` | rank 3 |
| `booster.extra_slot` | `#4CAF50` | Extra Slot button |
| `booster.shuffle` | `#5B6CE0` | Shuffle button |
| `booster.return` | `#3A8EDB` | Return button |
| `booster.bloom_burst` | `#F2622E` | Bloom Burst button (with `petal.center` sparks) |
| `backdrop.sky_top` | `#BFE3F8` | backdrop sky, top |
| `backdrop.sky_bottom` | `#EAF6F2` | backdrop sky, horizon |
| `backdrop.hill_far` | `#CFE6C0` | far hills |
| `backdrop.hill_near` | `#A9D68C` | near hills |
| `backdrop.bush` | `#86C470` | side bushes |
| `backdrop.blossom` | `#F9B8D0` | blossom dots |
| `backdrop.ruin` | `#DCD6E8` | distant arches (placeholder shapes) |
| `wordmark.fill` | `#7CCB52` | the Bloomlings wordmark placeholder |
| `wordmark.outline` | `#2F7A2A` | its outline |
| `tile.ground` | `#EFE6D2` | open ground (restored cells of the finished picture use the light variant color) |
| `tile.stone` | `#A3A6AE` | stone blocker |
| `tile.stone_edge` | `#7D818B` | its lower edge |
| `tile.mystery` | `#B8AFCB` | a hidden mystery tile |
| `pod.mystery` | `#F7D6E6` | the mystery pod card |
| `pod.mystery_mark` | `#C0508A` | its "?" |
| `special.gate` | `#6E9A5B` | Garden Gate |
| `special.fountain` | `#8FB4D6` | Fountain |
| `special.chest` | `#C08A57` | Chest |
| `special.statue` | `#A7A9BA` | Statue |
| `special.bridge` | `#A57C58` | Bridge |
| `currency.reward_basket` | `#B87B4B` | the Daily Reward basket |

Rules:

- **Theme tint.** The backdrop tokens are mixed 35% toward the level band's theme colors (`ThemeRotation`, spec 001
  FR-066).
- **Tile shading.**
  - Tiles use the variant color from the core's `VariantCatalog`, unchanged (research R7).
  - The raised look adds `variant` lightened 10% on the top half (more would drop Water's symbol below 3:1) and
    `variant` darkened 28% on the lower edge.
  - Symbols use `InkContrast` ink.
- **Pod cards.** They use `variant` mixed 70% toward `#FFFFFF` as the card, and the full variant color for the body.

## Radii (fraction of the shape's shorter side)

| Token | Value | Use |
|---|---|---|
| `radius.pill` | 0.5 | pills, primary and secondary buttons, badges |
| `radius.card` | 0.08 | popups, sheet, Home cards (at least 40 units) |
| `radius.tile` | 0.22 | board tiles |
| `radius.pod` | 0.2 | pods |
| `radius.slot` | 0.22 | slots |
| `radius.row` | 0.25 | Store and Leaderboard rows |

## Type

Bold is the platform's bold sans-serif (research R9). **Upper** means the text is rendered uppercase. **Min** is the
smallest auto-fit size.

> **Changed by spec 003** (the Garden look, `specs/003-cartoon-ui-style/`): every style is sentence case except
> `type.badge`; bold styles use Nunito ExtraBold and the others Nunito SemiBold; `type.button_large` is 92 (min 60),
> `type.button` 60 (min 40) and `type.button_secondary` 48 (min 34). The garden tokens are in spec 003
> `contracts/garden-tokens.md`, and the Bloomling colors in `contracts/bloomling-look.md`. The table below is the spec 002
> original.

| Token | Size | Bold | Upper | Outline | Min | Use |
|---|---|---|---|---|---|---|
| `type.wordmark` | 170 | yes | no | 12 | 110 | splash and Home wordmark placeholder |
| `type.title` | 64 | yes | no | 0 | 44 | card titles ("Daily Rewards", "Leaderboard") |
| `type.title_caps` | 60 | yes | yes | 0 | 42 | "PAUSED", "NO MOVES LEFT" |
| `type.level_home` | 84 | yes | yes | 0 | 60 | Home "LEVEL N" |
| `type.level_pill` | 52 | yes | yes | 3 | 38 | gameplay "LEVEL N" |
| `type.button_large` | 76 | yes | yes | 4 | 52 | PLAY |
| `type.button` | 54 | yes | yes | 3 | 38 | NEXT, CLAIM, RESUME, CONTINUE |
| `type.button_secondary` | 44 | yes | yes | 0 | 32 | RESTART, SETTINGS, HOME |
| `type.body` | 40 | no | no | 0 | 30 | rows, captions of options |
| `type.caption` | 32 | no | no | 0 | 26 | "Completed at Level 10", "New today · +40" |
| `type.count` | 40 | yes | no | 3 | 30 | pod and slot counts |
| `type.badge` | 28 | yes | yes | 0 | 22 | HARD, SUPER HARD, count badges |
| `type.reward` | 72 | yes | no | 3 | 48 | "+35", "+12" with the Petal symbol |

Numbers are grouped with a no-break space every three digits: "1 240", "12 345". This is done by the engine-free
`NumberText.Group` for Petals, levels, ranks and scores.

## Spacing and sizes

| Token | Value | Use |
|---|---|---|
| `space.xs` | 8 | icon to label |
| `space.s` | 16 | inside pills, between slots |
| `space.m` | 28 | between rows, card padding |
| `space.l` | 44 | between sections |
| `space.xl` | 72 | screen margin above the Home PLAY button |
| `size.touch_min` | 132 | minimum touch target (48 dp on a 360-dp-wide phone is 144; 132 allows the rim to count) |
| `size.icon_button` | 132 | round Pause, Settings, close, 2× height |
| `size.booster_button` | 150 | booster bar buttons |
| `size.primary_height` | 150 | PLAY; 126 for other primary buttons |
| `size.secondary_height` | 110 | secondary buttons |
| `size.margin` | 44 | screen side margin |

## Elevation

| Token | Look |
|---|---|
| `elev.raised` | a lower edge 8 units tall in the element's `_edge` color (buttons, pills, tiles, pods) |
| `elev.card` | a soft shadow 10 units down at 18% black (cards, sheet, popups) |
| `elev.pressed` | the element moves down by its edge height and the edge disappears (pressed pod, pressed button) |

## Motion (research R11)

| Token | Value |
|---|---|
| `motion.press` | 90 ms, scale 0.94 |
| `motion.pop` | 220 ms, scale 0.85 → 1.04 → 1 |
| `motion.sheet` | 260 ms slide from below |
| `motion.reward` | 300 ms rise and fade-in after the picture reveal |

Motion that plays during a level is scaled by the 2× setting. Menu motion is not.
