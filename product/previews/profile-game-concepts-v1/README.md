# Profile: game UI design proposal

2026-10-13. Five generated visual concepts for review, not implemented screens or production assets. Built-in OpenAI ImageGen was used with existing Shop/profile screenshots and existing avatar/badge illustrations as references. Exact prompts are in `prompts.json`; source outputs and dimensions are recorded in `manifest.json`.

## Proposed screens

1. `01-profile.png`: Profile landing page. Independent player card, large existing portrait, editable name, short ID, joining month, current level. Independent gold-rim achievement cards, nine per page, current progress/check/lock states and pagination. No invented stats, Claim actions or rewards.
2. `02-avatar.png`: full-page Edit profile, Avatar selected. Four free avatars and ten purchased variants retain catalog order and price tiers (300 / 600 / 1,200 Petals). Three-column portrait cards with compact price/status capsules. Fourteen choices remain available; short screens scroll. A selected unowned avatar switches the fixed footer from Save to the existing purchase action, with confirmation and insufficient-balance handling.
3. `03-frame.png`: Frame selected. Two-column previews of the current avatar inside each available frame. Five free frames from level 1 plus owned frames after the Wardrobe unlock. The concept uses the existing seven-frame fixture: Wooden Frame, Leaf Frame, Flower Wreath, Stone Frame, Golden Ribbon, Daisy Frame, Ivy Frame. No prices for owned items.
4. `04-badge.png`: Badge selected. Explicit No badge option plus owned badges. Existing catalogue examples illustrate a progressed profile; this is not a reward-granting proposal or a claim about every player's inventory. Badge selection appears in the common profile preview. Achievement badges follow existing availability, including earned choices available before the Wardrobe unlock. The Badge tab stays usable for No badge; do not introduce a whole-tab L40 lock.
5. `05-name.png`: Name selected. One recessed text field and current length/character hint, with native keyboard on tap and the shared Save footer. No duplicate Change name button is needed with direct field input.

## Shared direction

- Continue the approved Shop's cream material, smooth dimensional colored rims, tight shadows, chunky readable type and green Save/purchase skin.
- Retain the existing profile garden background asset, wooden header, back/wallet/plus controls. Generated concepts approximate the scenery; implementation must use the original image.
- Recommend changing the current edit modal into a full-page editor to give all four tabs space. Shared cream segmented Avatar / Frame / Badge / Name navigation, compact live profile preview, body-only vertical scroll and fixed Save footer. Back discards pending edits as Close does currently.
- Preserve the profile's current absence of main bottom navigation; no new menu is introduced.
- Reuse actual existing avatar portraits and badge art in implementation. Full concept images are illustrations, not replacement hero assets. Item names/IDs, ownership rules, level gates, avatar prices and transactions remain in the existing services.
- Empty owned collections use the current explanatory copy, while No badge remains available. Invalid/empty name and insufficient-Petals feedback stays near the field/action without moving navigation.
- Achievement taps retain existing help/equip behavior. There are 23 achievement lines, currently nine per page (three pages). All pages share the main achievement layout.

## Sources inspected

`UI/Screens/ProfileScreen.cs`, `ProfileEditCard.cs`, `playtest/design/ProfileScreen.cs`, `Meta/Profile/ProfileEditor.cs`, `AvatarCatalog.cs`, `Achievements.cs`, `Meta/Wardrobe/Resources/CosmeticCatalog.json`, English localization and profile fixtures 39–41. No runtime code, catalogue, economy or production art is changed by this proposal.

## Reference inputs

All five: `product/previews/shop-catalog-game-v1/26-store-cosmetics-19.5x9.png` for approved game material; matching existing profile screenshot specified in `prompts.json` for current content/background; `Art/Avatars/Resources/Avatars/bloom_default.jpg` for avatar identity. Badge additionally uses `Art/Badges/Resources/Badges/first_bloom.png` and `young_gardener.png` for existing emblem art. Reference files remain unchanged.
