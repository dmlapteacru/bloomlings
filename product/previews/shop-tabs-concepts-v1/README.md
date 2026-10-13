# Shop Cosmetics and Animations: game UI concepts

Prepared 2026-10-13. These are generated visual concepts for the two remaining Shop pages, based on the approved Packs screen and current content; they are not implemented screens. Built-in OpenAI ImageGen was used, with `transparent_background: false`. Project runtime code, localization, catalog, economy and assets remain unchanged by this iteration.

## Shared direction

Reuse the approved existing Shop garden background on all Shop tabs. Keep the wooden Shop title, balance/round plus, connected cream secondary tabs and bottom navigation. Replace the old full-page app-like parchment frame with individual cream molded cards over the garden. Tight perimeter shadows, smoothly raised colored rims, large artwork and legible green Petals purchase buttons. Only body content scrolls vertically; fixed navigation is outside the scroll viewport.

## Cosmetics

Four family choices remain Sprig/Bloom/Drop/Twig. Two-column cards give larger outfit previews. The first viewport shows Default/Spots, Stripes/Petal Print, Bucket Hat/Bell Cap; the next row peeks beneath them. Remaining items continue in the vertical list, not removed. Default has a check/status, not a price.

| Item | Current price (Petals) |
|---|---:|
| Spots | 200 |
| Stripes | 250 |
| Petal Print | 300 |
| Bucket Hat | 200 |
| Bell Cap | 250 |
| Pollen Trail | 180 |
| Breeze | 220 |
| Sleepy | 150 |
| Bramble Frame | 350 |
| Watering Can Badge | 300 |

Frames and badges preview profile decoration rather than a worn accessory. Owned purchases are not sold again. Milestone-only, starter and universally free items are not added as shop products. Item order and prices are from `CosmeticCatalog.json`; all ten current purchasable items remain available. Keep the L40 cosmetics unlock and current purchase-confirmation/insufficient-Petals behavior. Illustrative outfit artwork is a design target; final accessories must retain the underlying variant color with subtle neutral patterns.

## Animations

A two-column grid for the first six styles and a wide final Confetti Parade card resolves the odd item count and long final title. Large mini-board previews show actual clearing behavior. The production design should retain the existing live ClearPreview loops; these concept illustrations must not become baked static boards.

| Style | Current price (Petals) | Actual effect |
|---|---:|---|
| Munchers | Free | Takes three bites from a tile and returns full |
| Blossom | 2 500 | Tile sinks/turns and a flower opens |
| Fireflies | 3 500 | Tile breaks into glowing fireflies swarming to the slot |
| Bubbles | 3 500 | A soap bubble carries the tile to the slot and pops |
| Pushers | 3 500 | Creature physically rolls/pushes the tile home |
| Fireworks | 5 000 | Tile swells/pops and sparkles fly to the slot |
| Confetti Parade | 5 000 | Small creatures form a line and the tile pops into confetti |

Current states remain Choose / Chosen / Buy / Locked (L40 for unowned paid styles). The concept inherits the existing preview fixture's owned, chosen Fireflies and free but unselected Munchers. Selected style plays on every level; all styles have equal gameplay timing. Source of catalog/behavior: `ClearStyles.cs`, `ClearLook.cs`, `ClearingService.cs`, and RemoteConfigKeys bundled defaults. No new stronger effects, sales or prices are proposed.

## Reference inputs and exact prompts

Each initial generation used:

1. `product/previews/navigation-cream-v1/17-store-19.5x9.png` as approved visual/chrome reference.
2. The matching current-content screenshot: `26-store-cosmetics-19.5x9.png` or `42-store-animations-19.5x9.png`.

### Cosmetics

Use case: ui-mockup. High-fidelity FULL HEIGHT portrait mobile Bloomlings game Shop screen concept, same 1080x2340 proportions as references, fully visible. Image 1 is the APPROVED STYLE AND CHROME REFERENCE: the current Packs screen with lush turquoise garden backdrop, a wood Shop title sign, back button, Petals balance and round green +, one connected CREAM segmented bar labelled exactly 'Packs', 'Cosmetics', 'Animations', and bottom main navigation. Image 2 is the EXISTING CONTENT AND CHARACTER REFERENCE for the target tab. Preserve the approved header proportions, top back button, wood Shop plaque, balance, circular plus, connected cream navigation material and bottom main navigation arrangement. Redesign ONLY the shop body in the colorful polished cartoon garden-game direction of image 1. Use that same existing turquoise garden backdrop behind floating individual cream cards, no large flat beige app panel, no large brown picture-frame bezel enclosing the page. This is a game item catalog: chunky smooth shallow molded cream cards, narrow colored raised rims, tight even soft shadows, generous internal padding, warm highlights, vivid large artwork, cream-white chunky rounded title text with brown outline, bright green rounded purchase buttons with white prices outlined dark green and a pink lotus currency icon. Never make money prices: ALL purchases on this tab are Petals. Maintain legible text and sane touch targets. Top and bottom navigation fixed; only body content vertically scrolls. No invented sale discounts, banners, loot, currency or game mechanics. No watermark, no annotations, no extra UI outside phone. TARGET: Cosmetics selected in the connected top cream bar; Packs/Animations dark brown inactive labels. Balance '1 240'. Beneath it preserve a more compact connected cream family selector labelled 'Sprig', 'Bloom', 'Drop', 'Twig', using green leaf / pink flower / blue water drop / brown acorn icons, Sprig selected green. The body is a TWO-COLUMN grid of big squarish cream item cards, soft petal-pink/gold trim for cosmetics, hero preview takes most of each card. The Sprig character is the same recognizable cute green leafy garden creature from image2: big head, leafy pointed crown, friendly eyes, small limbs; not a human, not a generic blob or another game mascot. Show outfit differences CLEARLY while retaining his green body: subtle spots / stripe patterns / faint pink petal print on body, soft neutral taupe Bucket Hat and Bell Cap. Preserve catalog ORDER and exact visible labels/prices. Row1: 'Default' with normal Sprig and a small green check, muted cream 'Chosen' button (FREE, no price); 'Spots' with pale small spots on Sprig, lotus '200'. Row2: 'Stripes' with subtle stripes, lotus '250'; 'Petal Print' with faint petal markings, lotus '300'. Row3: 'Bucket Hat', lotus '200'; 'Bell Cap', lotus '250'. At bottom of scroll viewport just above unchanged bottom nav, a SMALL PART of next row can peek naturally: 'Pollen Trail' and 'Breeze' artwork indicating more content; do not squeeze all items in. Other EXISTING scroll items retained below are Pollen Trail 180, Breeze 220, Sleepy 150, Bramble Frame 350, Watering Can Badge 300. Do not invent filters, spotlight hero panel, exclusive clothing or premium characters. No category headings or separator ribbons. The body should match the game Packs cards and let the beautiful actual character wear the item; images fully within cards, prices with bottom padding. One clear selected state only on Default card. Highly finished illustrative screenshot, not a wireframe.

### Animations

Use case: ui-mockup. High-fidelity FULL HEIGHT portrait mobile Bloomlings game Shop screen concept, same 1080x2340 proportions as references, fully visible. Image 1 is the APPROVED STYLE AND CHROME REFERENCE: the current Packs screen with lush turquoise garden backdrop, a wood Shop title sign, back button, Petals balance and round green +, one connected CREAM segmented bar labelled exactly 'Packs', 'Cosmetics', 'Animations', and bottom main navigation. Image 2 is the EXISTING CONTENT AND CHARACTER REFERENCE for the target tab. Preserve the approved header proportions, top back button, wood Shop plaque, balance, circular plus, connected cream navigation material and bottom main navigation arrangement. Redesign ONLY the shop body in the colorful polished cartoon garden-game direction of image 1. Use that same existing turquoise garden backdrop behind floating individual cream cards, no large flat beige app panel, no large brown picture-frame bezel enclosing the page. This is a game item catalog: chunky smooth shallow molded cream cards, narrow colored raised rims, tight even soft shadows, generous internal padding, warm highlights, vivid large artwork, cream-white chunky rounded title text with brown outline, bright green rounded purchase buttons with white prices outlined dark green and a pink lotus currency icon. Never make money prices: ALL purchases on this tab are Petals. Maintain legible text and sane touch targets. Top and bottom navigation fixed; only body content vertically scrolls. No invented sale discounts, banners, loot, currency or game mechanics. No watermark, no annotations, no extra UI outside phone. TARGET: Animations selected in top connected cream bar, other tabs inactive dark brown. Balance '9 740'. NO family selector on this page. Arrange seven existing animation styles as SIX generous cards in a two-column by three-row grid plus ONE shorter full-width final 'Confetti Parade' card (its long name naturally fits the wider layout). Make all seven fit in the viewport above fixed bottom nav, with a concise final caption if room. Each card has a large BEAUTIFUL MINIATURE ACTUAL GAMEPLAY BOARD PREVIEW, matching the reference's stone-fence board, small rounded vivid tile cells with water-drop/leaf/pink-flower icons, and a small recognizable green leafy Bloomling creature acting on tiles. These are snapshots from live looping previews, not abstract effect symbols or unrelated portraits. Different previews must show the actual effect clearly, and selected Fireflies has green rim and check. Row1 LEFT: 'Munchers', little creature taking bites from a leaf tile, small 'FREE' tag, cream action 'Choose'. RIGHT: 'Blossom', pink flowers opening where tiles sink, lotus green price button '2 500'. Row2 LEFT: 'Fireflies', glowing golden/green fireflies swarming from a tile toward the bottom board arch, check and cream/green status 'Chosen', no purchase price because owned. RIGHT: 'Bubbles', iridescent soap bubble carrying a water tile toward the arch, lotus price '3 500'. Row3 LEFT: 'Pushers', small creature physically pushing a rounded tile along the board, lotus price '3 500'. RIGHT: 'Fireworks', tile sparkling and bursting into colorful particles, lotus price '5 000'. Last full-width card: 'Confetti Parade', small line of green Bloomlings and gentle colorful paper confetti over a miniature board, lotus price '5 000' on the right with balanced padding. Keep cards in precisely this order. Narrow cream/gold rims with a subtle violet-blue tint on nonselected animation cards, restrained game depth, short soft perimeter shadows. Preserve these seven names and prices exactly. Show an unobtrusive one-line caption 'Chosen style plays on every level.' if it fits; this refers to cosmetic animation only. No time-limited rewards, no unlocking different levels or stronger boost mechanics. This mockup is the unlocked state at level40+; keep locked state out of this screenshot. Clearly legible tall mobile screenshot, polished colorful game art.

### Animations refinement

Use case: precise-object-edit. EDIT TARGET: supplied Bloomlings Shop Animations concept. Keep the ENTIRE image, screenshot dimensions, layout, backdrop, every border, text label, price, title, balance, all other previews, header, tab bar and bottom navigation EXACTLY unchanged. Make ONLY these two local refinements: (1) on the Munchers card (top left), add a small tasteful cream/gold corner badge labelled exactly 'FREE' in dark brown bold lettering near the upper left corner of its preview, without hiding the main creature or price/action. Munchers is the free style; leave its existing 'Choose' button. (2) in the Pushers mini-board preview (left card in the third row), remove the blue magic/breath arrow from the creature's mouth. Show the same little green leafy creature leaning forward with BOTH HANDS physically pressing against the edge of the blue water tile beside him and pushing it toward the board arch, with a small subtle movement trail BEHIND the tile. It must read as physical pushing, not blowing bubbles, magic, shooting or buying a booster. Preserve the name 'Pushers' and lotus '3 500' button. No other changes, no extra cards, no cropping, no added prose, no watermark. Keep polished illustrated cartoon garden game material.

## Saved image outputs

- `01-cosmetics.png`: unchanged copy of `exec-16c75021-e372-49e2-8a12-273472f164c3.png`.
- `02-animations.png`: unchanged copy of refined `exec-2bf2a454-818a-4a22-a5b2-7e7638d712d2.png`; edit input was initial `exec-b29d5876-96b2-41fb-9153-4b22f123e433.png`.

Review: visible prices match the catalog, seven animation styles are represented, selected/free states are distinguishable. These are static concept images; minor typography/character-material differences from actual runtime remain subject to final asset and live-text implementation.
