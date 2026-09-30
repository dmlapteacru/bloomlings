# Contract: Asset Slots and the Asset Inventory

Every visual or sound that final art will replace is drawn or played through an **asset slot**: a stable id registered
in `client/Assets/Bloomlings/UI/Design/AssetSlots.cs` (engine-free). A slot has two faces:

- the **placeholder** the game uses now (a shape, a procedural backdrop, a synthesized sound, the system font);
- the **inventory entry** that tells art production what to make (FR-029, FR-030).

## Id scheme

`<prefix>.<name>[.<state>]`, lowercase with underscores, for example:
- `booster.shuffle`;
- `pod.state.mystery`;
- `bg.theme.pond`;
- `char.sprig.idle`;
- `audio.cue.clear`.

| Prefix | Category (FR-029) | Examples |
|---|---|---|
| `brand.` | Brand | `brand.wordmark`, `brand.app_icon`, `brand.splash_art` |
| `bg.` | Background | `bg.theme.daylight_garden`, `bg.theme.pond`, `bg.home`, `bg.splash` |
| `char.` | Character | `char.sprig.idle`, `char.bloom.walk`, `char.hero.home`, `char.face` |
| `symbol.` | VariantSymbol | `symbol.leaf`, `symbol.moss`, …, `symbol.bark` (8 launch + 4 expansion) |
| `tile.` | BoardTile | `tile.base`, `tile.layer_peek`, `tile.mystery`, `tile.stone`, `tile.key`, `tile.ground`, `tile.entry`, `tile.picture` |
| `special.` | Special | `special.gate`, `special.fountain`, `special.chest`, `special.statue`, `special.bridge`, `special.bridge_broken` |
| `pod.` / `slot.` | PodSlot | `pod.card`, `pod.state.locked`, `pod.state.mystery`, `pod.link`, `pod.count`, `slot.empty`, `slot.state.danger`, `slot.extra` |
| `booster.` | Booster | `booster.extra_slot`, `booster.shuffle`, `booster.return`, `booster.bloom_burst` |
| `ui.` | UiKit | `ui.button.primary`, `ui.pill.level`, `ui.badge.hard`, `ui.card`, `ui.sheet`, `ui.close`, `ui.pause`, `ui.medal.gold`, `ui.ad`, `ui.gift`, `ui.check` |
| `currency.` | Currency | `currency.petal`, `currency.reward_basket`, `currency.milestone.skin` |
| `collection.` | CollectionFrame | `collection.frame`, `collection.frame.new` |
| `cosmetic.` | Cosmetic | `cosmetic.hat.cap`, `cosmetic.skin.spots`, `cosmetic.frame`, `cosmetic.badge` |
| `fx.` | Effect | `fx.sparkle`, `fx.petal_burst`, `fx.confetti`, `fx.droplet`, `fx.shuffle_swirl`, `fx.burst` |
| `font.` | Typography | `font.display`, `font.body` |
| `audio.` | Audio | `audio.music.<theme>`, `audio.cue.<SoundCue>` |

## Registry entry

The fields are those of `AssetSlot` in [`../data-model.md`](../data-model.md):
- `Id`, `Category`, `Title`;
- `Frames`, `UsedIn`, `States`;
- `SizeClass`, `Readability`, `Priority`;
- `Placeholder`.

## Rules (tested)

1. **Ids.** Ids are unique, and each id's prefix matches its category (the table above).
2. **Every placeholder has a slot.**
   - Every shape either client draws is registered (a shape id is its slot id).
   - So is every backdrop layer set, every `SoundCue` and every font role.
   - The client tests enumerate the shapes the clients reference.
   - The preview tool records every slot the screens draw while rendering frames 1–17.
3. **Every slot has a use.** Every registered slot is referenced by at least one client or by the preview recording.
   Sound cues count as used, since the synthesizer plays every `SoundCue`. Slots with no placeholder in the game yet
   (`External`, such as the app icon, and silent music tracks) are listed as "not in the game yet". This makes SC-003
   hold both ways.
4. **Readability.** Every `VariantSymbol`, `PodSlot` and `BoardTile` slot has `Readability = true`. Its inventory entry
   says that it must pass spec 001's readability checks (grayscale and icon, small size, color distance, pod, slot and
   moving character).
5. **Launch priority.** `Priority = Launch` for:
   - everything shown in frames 1–17;
   - the 8 launch symbols;
   - the four families' idle and walk.

   Expansion symbols, later cosmetics and extra music themes may be `Later`.

## Inventory document

`dotnet run --project playtest/preview -- --inventory` writes `specs/002-ux-design-board/asset-inventory.md` from the
registry, grouped by category. Each row has:
- **Id**;
- **What**;
- **Where** (screens and board frames);
- **States or variants**;
- **Size class**;
- **Readability** (yes or no);
- **Priority**;
- **Placeholder now**.

The document is generated; edits go to the registry. A test fails when the committed document differs from the
generated one.
