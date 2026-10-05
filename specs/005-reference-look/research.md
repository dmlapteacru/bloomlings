# Research: Reference Look (spec 005)

Decisions taken from the owner's reference (`reference.jpg`) on 2026-10-02. The owner asked for no questions; each
decision records what the reference shows, what we do and why, so the owner can revise it.

## D1. Saturated variant palette

- **Reference**: the variant tiles are saturated "candy" colors (sampled: leaf lime ~`#A7DC24`, moss teal ~`#4FC7B3`,
  flower hot pink ~`#F75089`, violet bud ~`#BF70FC`, water ~`#4DB6FC`, dew ~`#43CDD6`, wood ~`#C06A26`, acorn
  ~`#FAA045`). Our catalog colors were muted and dark (moss `#356557`, violet `#512E97`, wood `#551E0A`).
- **Decision**: new catalog colors, optimized for closeness to the reference hue and chroma under the readability
  constraint (every pair a candidate: CIEDE2000 ≥ 10 under normal vision, protanopia, deuteranopia and tritanopia;
  within a family a grayscale ΔL ≥ 9):

  | Variant | Old | New | | Variant | Old | New |
  |---|---|---|---|---|---|---|
  | Leaf | `#ADCF42` | `#99D323` | | Water | `#3D82E0` | `#3485E7` |
  | Moss | `#356557` | `#0FB198` | | Dew | `#74F9F9` | `#61DAE1` |
  | Flower | `#EF8DA5` | `#FF3B89` | | Wood | `#551E0A` | `#9B4904` |
  | Violet Bud | `#512E97` | `#7F3CC4` | | Acorn | `#B55A11` | `#CF7F20` |
  | Vine | `#F7FA2E` | `#FFFF3C` | | Mist | `#8D9BCC` | `#94A8EC` |
  | Berry | `#A02C12` | `#810F00` | | Bark | `#B1A05B` | `#84794B` |

  The pipeline's `readability` command reports 66 of 66 pairs as candidates, minimum ΔE00 11.09
  (`content/readability/pairs-report.json`). The reference's own colors fail it: leaf and acorn are 2.8 apart for a
  deuteranope, water and dew and violet and water are under 10. Violet Bud, Wood and Acorn are therefore a little
  darker than in the reference. `approved-pairs.json` stays provisional (unchanged pairs).
- **Alternatives**: keep the old colors and only restyle (rejected: the board would not look like the reference);
  take the reference colors as they are (rejected: fails FR-005 readability).

## D2. Board tiles are candy tiles with an embossed symbol

- **Reference**: saturated tiles that nearly touch, each with a small symbol in a darker shade of its color, a bevel
  and a little gloss; no faces.
- **Decision**: board target tiles become candy tiles (contracts/look.md §3.1, board style). The spec 004 characters
  leave the board tiles (spec 004 FR-012 replaced). The owner's 2026-10-01 choice of "light tiles with characters"
  (spec 004 clarification) is superseded by this reference.

## D3. Pods and Waiting Slots show the variant tile and the count

- **Reference**: pods and slots hold the variant tile (sticker style: a larger symbol with a light edge) with the
  count below it ("12", no "x").
- **Decision**: as in the reference; replaces spec 004 FR-008 and FR-009. State meaning stays: queued pods are dimmed
  (was "asleep"), stuck slots grey with the hourglass (was "worried"). Since 2026-10-03 a pod's count stands beside
  its tile (D20). The 2D characters remain as walkers and on the
  Bloomlings sheet (frame 24), the 3D heroes on the meta screens.
- **Alternative**: keep the characters inside the wooden frames (rejected: the owner asked for the reference's look of
  "геймплей", and characters on same-colored tiles lose contrast).

## D4. Materials as engine-free pictures

- **Reference**: wood with grain, stone blocks, parchment.
- **Decision**: `UiRaster` renders wood planks, wood frames, stone blocks and candy tiles as RGBA pixels in the kit;
  both builds cache and draw them (`IPainter.Picture`, `ProceduralSprites.Picture`). It follows the existing
  `BackdropRaster` path, keeps one look in both builds and stays testable without an engine.
- **Alternatives**: vector approximations only (rejected: no convincing grain); generated PNG files from
  `tools/artgen` (rejected for now: 9-slicing and per-size art in two hosts, and the owner may replace these later
  anyway — listed as optional in `pictures.md`).

## D5. Buttons

- **Reference**: glossy raised faces; the main green buttons sit in a light wooden rim on Home and Win; round cream
  buttons with brown glyphs; jam choices are big green/blue rounded buttons with the icon above the label and a cream
  cost pill overlapping the bottom edge; "Pressed" is darker and sunk.
- **Decision**: contracts/look.md §3.3. The close button becomes cream with a brown ✕ (was red), as in the reference.

## D6. Wooden signs replace the level pill and the card header bands

- **Reference**: the gameplay level, the Home level, "Level Complete!" and "Wardrobe" are wooden planks with dark brown
  letters; ivy at the ends in gameplay and Wardrobe, flower clusters on the win sign.
- **Decision**: `Kit.WoodSign` (§3.2). Super Hard keeps its badge under the sign; the sign's letters turn purple.

## D7. Parchment cards

- **Reference**: cards are parchment with a brown outline and an inner line; titles are brown text (jam) or signs.
- **Decision**: `Kit.Paper`/`Card` restyled (§3.5); green header bands are dropped.

## D8. Lotus currency

- **Reference**: the Petals currency is a pink lotus.
- **Decision**: `currency.petal` is redrawn as a lotus everywhere (§3.4).

## D9. Booster icons

- **Reference**: the jam card shows Extra Slot (white "+" on a blue disc), Shuffle (turning arrows), Return (yellow
  arrow), Bloom Burst (pink flower); its booster row shows a trowel and a pinwheel instead, which do not match our
  boosters.
- **Decision**: the jam card's four icons everywhere (§3.8). Flagged to the owner.

## D10. Distinct symbols per variant

- **Reference**: Water and Dew share a drop; Wood and Acorn share an acorn.
- **Decision**: keep distinct silhouettes (spec 001 FR-005, constitution: hue alone never carries meaning). Water is a
  pointed teardrop, Dew a round droplet with a sparkle; Wood a stump with a ringed top, Acorn an acorn. Flagged.

## D11. Sentence case

- **Reference**: "PLAY", "LEVEL 88" in capitals.
- **Decision**: sentence case stays (spec 003 FR-025); only `type.badge` is uppercase. Flagged.

## D12. Lawn behind the board, stone arch entries

- **Reference**: the board lies on a lawn with flowers and bushes; Bloomlings come out of a stone arch below it.
- **Decision**: the gameplay backdrop scene becomes a lawn (§4.2); each Garden Entry is a small stone arch on its side
  (entries can be on any side, spec 001). The owner's gameplay background picture may replace the lawn later.

## D13. Redrawn symbols

- **Reference**: clear, simple symbols per variant (leaf with vein, scalloped moss, five-petal flower, bud, drops,
  acorn).
- **Decision**: `ShapeLibrary` symbols are redrawn to read like the reference strip at board size, with the family
  pairs still differing in silhouette (the existing shape-difference tests keep running). Sticker details (veins,
  centers, sparkle, rings, cap) are drawn by the tile picture, not by the symbol shape.

## D14. Win: the finished picture in full color

- **Reference**: the win screen shows the finished picture as saturated tiles with symbols.
- **Decision**: the win card draws the finished picture as flat candy tiles (no lip) of each role's variant inside a
  thin stone border. On the board itself restored cells stay pale, so clearing stays visible during play.

## D15. Jam copy

- **Reference**: "No More Space!" / "All waiting slots are full. Choose a way to continue." / "Restart Level".
- **Decision**: `jam.title` = "No more space!", `jam.subtitle` = "All Waiting Slots are full. Choose a way to
  continue." (sentence case; "Waiting Slots" is the product term). `win.title` = "Level complete!". Restart keeps its
  label. The jam keeps our choices (spec 001 recovery rules).

## D16. Owner pictures

- **Decision**: 3D heroes (poses), backgrounds and the logo are the owner's pictures (`pictures.md`), each with a slot
  and a drawn stand-in. Until they arrive, the drawn stand-ins (lawn, sky with arches, stone pedestal, wooden logo
  letters) show.
- **Review round**: the 3D heroes share `tools/artgen`'s folder, whose check rejects any file it did not render. An
  owner flag in its `manifest.json` (`"source": "owner"`, set by `artgen -- adopt` with a source record) lets the owner's
  pictures in under the same names: `build` keeps them, `check` verifies their hash, size, margin and record. A separate
  owner folder was the alternative; it would have needed a second lookup in both builds' picture loaders.

## D17. The constitution and locked docs

- Gameplay stays flat 2D: tiles, stones, wood and gloss are depth drawn in the plane (constitution VII). The 3D heroes
  stay on the meta screens. No rule changes. The variant colors are art placeholders owned by art
  (`VariantCatalog` remarks); the readability check is the gate (D1).

## D18. The animated heroes: pre-rendered offline (owner's delivery, 2026-10-02; FR-028)

- **Delivered**: four rigged FBX models made with Meshy AI, one per family, each a textured skinned mesh on a 28-bone
  Mixamo-style rig with 4 to 12 clips (many named only by Meshy library ids), and the owner's table: a 4 s idle
  ("breathing + sway") and a 2 s reaction per hero (`tools/heroanim/SOURCE.md` maps the clips).
- **Decision**: `tools/heroanim` renders the clips offline into flat frame pictures, and both builds play the frames.
  - The renderer is three.js 0.160 (`FBXLoader`, patched to read Meshy's embedded textures) in headless Chromium
    (playwright-core, SwiftShader): one camera per hero for all its frames (field of view 20°, 10° from above, turned
    toward the fountain's middle by the hero's `yaw`), rendered 2.5 times larger and drawn down into a 448 × 504 cell
    (the still heroes' 8:9 shape) with the seam pose's feet on 90% of its height and the seam pose about 84% of the
    cell tall.
  - 12 frames per second at first: 48 idle and 24 reaction frames per hero, 288 in all, half the files and memory of
    24 fps. Since the owner's 60 fps delivery (D22) the bake takes 24 frames a second: 96 and 48 a hero, 576 in all.
  - The seams: a picture cannot blend two poses at run time, so the blends happen in the bake. The idle eases into its
    own first pose over its last 0.75 s, so it loops; the reaction blends in from that pose over 0.25 s and back to it
    over its last 0.4 s. A reaction that starts on a seam therefore joins the idle without a jump. A tap between seams
    reacts at once and cross-fades from the idle frame it interrupts over 0.12 s (`HeroMotion.DissolveSeconds`); one
    asked for within 0.35 s of the next seam waits for it (`MaxSeamWait`).
  - The format: every frame is cropped to its visible bounds (its crop and two head points, the head bone at the chin
    and the head's top at the brow, are in the generated `HeroMotionData.cs`) and stored as an 8-bit palette PNG with
    one dithered 256-color palette per hero, shared by all its frames so no color flickers: 11 MB for the 288 frames
    (23 MB for the 576 at 24 fps). Decoded, the 576 cropped frames are about 280 MB as RGBA (70 MB as palette
    pictures), so the hosts load a frame when it is first drawn and keep a bounded cache, never the whole set.
  - The light: the models' normal maps show blotches at this size, so they are dropped; the albedo is lit with a
    Lambert material under a warm hemisphere light, a key light from the upper left (as in the reference), a fill and a
    rim light. The look is soft and volumetric, close to the owner's still heroes.
  - The hats follow the head: `HeroMotion.Hat` places a worn hat on the line from the head point to the head's top
    point, turned with it (`HeroFrame.Roll`).
  - On Home the four take turns: each idles from its own phase (so they do not breathe together), one reacts every 6 s
    at its next seam (Bloom first, 1.5 s after Home opens, then Sprig, Drop, Twig), and a tap makes a hero react at
    once (`HomeMotion`). The win's hero reacts from the moment it appears and then idles (`HeroMotionPlayer` with its
    idle starting then and a reaction waiting for that seam).
- **Why**: constitution VII forbids a 3D scene, camera or model in what the player plays and navigates and allows
  pre-rendered 3D as flat pictures on meta screens. Pre-rendered frames are such pictures, both builds (the Unity
  client and the .NET playtest) draw them through the same engine-free kit, and the motion is deterministic in time.
- **Alternatives**:
  - Unity's Animator on the FBX (rejected: a live 3D model, camera and lights in the game, against constitution VII,
    and the playtest could not show it at all);
  - a C# FBX reader with skinning in the kit (rejected: a large parser and skinning code for one look, still 3D at run
    time);
  - sprite atlases, one sheet per hero (rejected: a sheet is decoded whole, about 30 to 42 MB of RGBA per hero, four
    on Home, too much for the .NET playtest's memory; separate frames load one at a time);
  - blending the idle and the reaction at run time (not possible between pictures; hence the baked seams and the short
    cross-fade);
  - the guide's blink (`3.webp`): the rig has no face bones, so the clips cannot blink; left out unless the owner adds
    such clips (pictures.md H).

## D19. The layered Home (owner's delivery, 2026-10-02; FR-028)

- **Delivered**: `bloomlings_home_assets.zip`, the Home picture (852 × 1846) in five layers: the garden (sky, arches,
  flowers, paving, without the fountain), the fountain's back, the fountain's front stones and flowers, a sheet of four
  soft shadows and the drifting petals, each a full-size picture with a transparent background.
- **Decision**: `tools/heroanim/layers.mjs` prepares them (`SOURCE.md`): the garden re-encoded as JPEG (it replaces the
  earlier single Home picture), each other layer cropped to its visible bounds (a decoded layer holds no empty rows),
  their boxes in the picture written to `HomeLayersData.cs`. The hosts lay every layer in the box the backdrop
  cover-fits the garden into (`HomeLayers.Cover`, `Place`), so the heroes stay on the fountain on every screen shape.
  - **The lotus cut-out**: in the reference Bloom stands behind the lotus, which the fountain's back layer holds.
    `layers.mjs` cuts the lotus out of that layer (its pink petals and what they enclose, the edge softened, the
    bottom fading over the leaves) as a sixth picture, drawn again over Bloom, who stands between the two.
  - **One shadow**: the sheet's four shadows were painted for places that do not line up with the reference's
    (where the owner asked the heroes to stand), so the front left one, the widest, is cut out with faded edges and
    drawn under every hero, as wide as its seam pose, at 85% opacity (`HomeLayers.ShadowBox`).
  - **The petals** drift down (22 picture pixels a second) and sway (14 pixels over 7 s), drawn twice a picture height
    apart so they wrap (`HomeLayers.PetalsAt`).
  - **The placement**: each hero's feet and height were measured on the reference's Home and fitted to the layered
    fountain (`HomeLayers.Placement`, contracts/look.md §6.4).
- **Alternatives**: the four painted shadows where they are (rejected: they lie beside the heroes); asking the owner for
  a lotus layer (not needed: the cut-out follows the painted petals); placing the heroes in a box of
  their own over the picture (rejected: they would slide off the fountain on other screen shapes).

## D20. The tray's pods in columns (the owner's gameplay rule, 2026-10-03; FR-021 amended)

- **Owner**: "Карточки выбора должны идти друг за другом, а не друг на друге, вне зависимости от дизайна игры, это
  правило геймплея. Так чтоб было видно 3-4 ряда. Подстрой размеры боксов." The deck of FR-021 followed the
  reference's stacked frames: it showed the next two pods only as bands peeking above the front pod, and drew them on
  each other.
- **Decision**: each stack is a column (`ReferenceGameplayRegions.Pod`, `PodChip`, contracts/look.md §6.1). The
  exposed pod is `0.13W` tall on top, and the next pods are `0.1W` tall under it, `0.01W` apart. The tray shows four
  rows from a safe aspect of 1.95 and three below it. The slot row, the booster row and the separators shrink to
  `0.16W`, `0.18W` and `0.03W`.
- **Why the grid**: the owner calls it a gameplay rule, not a look. The player must read what each choice uncovers,
  as in spec 003 FR-022a, which the deck had replaced. A rule outranks the reference's picture, so a later restyle
  must not stack pods again.
- **Why landscape pods**: square pods as wide as a column (`0.228W` with four stacks) would make four rows about
  `0.94W` tall, more than the deck's whole tray (`0.855W`). Pods wider than tall (`0.13W` and `0.1W`) need `0.46W`,
  about half of that. The pods first held the tile at the left and big digits beside it; the owner's choice "E"
  (D21) narrowed the frame to 1.3:1 with the icon in the middle. `PodChip.MinAspect` (1.45) still bounds the places,
  so the 1.3:1 frame always fits its column, even with six stacks.
- **Why four rows only from 1.95**: the tray shrinks by `k` on shorter screens (`k` is about 0.86 at 16:9), but a
  fourth row still costs `0.11W·k`.
  - On 16:9, four rows would leave the board only about a third of the safe height with a badge and a bottom entry
    (about 0.33 H, at the layout tests' limit). Under a navigation bar it would get less: 0.32 H on 1080 × 1920 with
    a 100 px bottom inset. Three rows leave about 0.39 H.
  - From 1.95 on (`k` ≥ 0.975; 1 from 19.5:9), four rows leave at least 0.35 H.
  - 1.95 sits just under the reference's safe shape (2.0). 18:9 phones without a status bar get four rows; under one
    their safe shape falls just below 1.95, so they get three.
- **Alternatives**:
  - Keep the deck and widen its bands (rejected: the pods would still lie on each other, against the owner's rule).
  - Always three rows (rejected: the owner asked for 3-4, and tall phones have the room).
  - Always four rows (rejected: 16:9 boards would drop under a third).
  - Two rows of columns for many stacks (not needed: six stacks, `SourceTray.MaxStacks`, fit one row at `0.147W`).

## D21. The pod's look: the icon first, the count in a corner (owner's choice "E", 2026-10-03; FR-021)

- **Decision**: a pod's frame is 1.3 times as wide as tall, centered in its place in the column (`PodChip.Aspect`). It
  shows the owner's detailed icon of the variant alone (no candy tile under it) over the tinted panel's middle, a
  little larger than the panel (`PodChip.Icon`). The count is small digits (36% of the pod's height) in a white outline
  over the panel's bottom right corner (`PodChip.Count`, `CountLook`). The "+N" disc moves to the frame's top left
  corner, and the link ring of a connected pod to its top right. A mystery pod keeps its "?" sticker tile, a locked pod
  its padlock, and a variant without the owner's picture its sticker tile, all centered (`PodChip.Tile`). Both builds
  draw it from the kit's `PodChip`.
- **Why**: the owner asked for smaller digits and the focus on the icon ("Цифры можно не такие большие; фокус на
  иконку, цифры можно в углу"). Six mock-ups were rendered from the playtest's real tray (the current look, A: the
  count on a green disc, B: outlined digits over the tile's corner, C: the tile at the left and small digits, D: a cream
  count tag on the frame's bottom edge, E: B with the bare icon, larger); the owner chose E.
- **Kept**: the column grid and its sizes (D20), the tinted panel, the waiting veil, the touch boxes (the places, wider
  than the frames), the flights (from the tile's place, the icon's middle) and the slots' candy tiles.
- **Alternatives**: the other five mock-ups (not chosen by the owner).

## D22. The owner's 60 fps models: the same frames, baked at 24 fps (2026-10-03; FR-028)

- **Delivered**: the four heroes again, exported by Meshy as `…_biped_Animation_all_frame_rate_60.fbx`. The models,
  rigs and clips are the same (28 bones, the same Meshy ids, now named `target_character|target_character|<id>`), and
  each clip holds the same number of frames as before (the idle 96 or 97, the reactions 49), but stamped 1/60 s apart
  instead of 1/24 s: the idle lasts 1.6 s instead of 4 s and the reactions 0.8 s instead of 2 s. The export re-timed
  the frames instead of adding frames between them.
- **Decision**: the new files replace the old ones; every clip is played at the length of the owner's table
  (`heroes.json` `idleSeconds` = 4, `reactSeconds` = 2; `page.html` scales the clip's own time to it), so the motion is
  exactly the earlier one (the new and the old bake agree frame for frame at the same times); and the bake takes 24
  frames a second, the motion's own rate in the table's time, so every baked frame is one of the file's poses.
  - Files: 576 frames, 23 MB (was 288, 11 MB).
  - Memory: Home's four idle loops are 47 MiB as palette pictures (one byte a pixel), so the playtest's frame cache
    grows from 30 to 60 MiB (`PainterBase.HeroFrameCacheBytes`); it must stay above the loops, or Home would decode
    every frame. Unity keeps the families on screen as compressed textures, twice as many as before.
- **Why not 60 fps**: flat frames at 60 a second would be five times the 12 fps set (1440 frames, about 57 MB in the
  APK, 117 MiB of idle loops to keep or 240 frames a second to decode on Home), and every frame past 24 would only
  interpolate between the file's poses. 24 fps doubles the smoothness for twice the cost.
- **Why not the files' timing**: 1.6 s breathing and 0.8 s bounces are 2.5 times faster than the owner's table (idle
  4 s, reactions 2 s); the table stays the reference until the owner changes it.
- **Alternatives**: keep 12 fps (no visible change from the new files); 30 fps (an even cadence on 60 Hz screens, but
  interpolated poses and 25% more frames than 24).

## D23. Twig celebrates; the owner's new Twig model waits for its colors (2026-10-03; FR-028)

- **Decision**: every win, and the milestone after it, celebrates with Twig (`CharacterArt.Celebrant`, the owner's
  choice), in both builds, instead of the family of the level's main variant (the two builds' rules also broke ties
  differently; the one constant removes both).
- **The new model** (`twig.fbx`, 2026-10-03): exported by Blender 4.2 with its own rig (`Twig_rig_*`, 33 bones, the head
  bone `Twig_rig_Head`), a 122 721-vertex skinned mesh `Twig` with UVs and one shape key (`Twig_cheer_update_LidsSurface`,
  the eyelids), three clips (`Twig_Rig|Twig_Breathing` 3 s, `Twig_Rig|Twig_SmallBounce` 1.5 s, `Twig_Rig|Twig_WinCheer`
  3 s, keyed at 24 a second) and, besides, Blender's default cube, two lights and a camera. Its one material is plain
  grey (`MeshPhongMaterial`, 0.8 grey) with no texture: the export embedded none (no Texture or Video node in the
  file) and has no vertex colors, so a bake would draw Twig all grey among the colored heroes.
- **The colors** came in the third file, `Twig.glb` (Blender's glTF exporter 5.2.39): the same model, rig (24 joints)
  and clips with its three 1024 × 1024 textures embedded. (`Twig_v2.fbx` before it named four textures by a path on the
  owner's computer without embedding them.)
- **The bake** (`heroes.json`): Twig's model is the `.glb`; its clips keep their own spans (from the first keyframe, 1/24
  s in, to the last): `Twig_Breathing` the idle (3 s, 72 frames), `Twig_SmallBounce` the reaction (1.5 s, 36) and
  `Twig_WinCheer` a third clip, the win's (3 s, 72), baked as the reaction is, from the seam pose and back. Only skinned
  meshes are drawn (Blender's default cube is hidden); the camera's planes follow the model's size (metres here,
  centimetres in Meshy's). The hats follow `Twig_rig_Head` and `Twig_rig_Leaf` (the stem on the cap), so Twig's
  `HeroMotion.HatLift` is 0: the hat sits on the cap.
- **The kit**: `MotionClip.Win`, `HeroMotion.HasWin`, `HeroMotionPlayer.Celebrate` (the win and the milestone: the cheer
  when the hero has one, else its reaction). Both builds' celebrations call it; the frame sets hold the third clip.
- **Home**: the new Twig spreads its branches wider, so it stands at 0.815 of the picture's width (was 0.83), 0.31 of
  it tall (was 0.33), inside a 21:9 screen.
- **Not changed**: Twig's still pictures (A1–A7: the Wardrobe, the profile, the group, the celebrating picture) stay the
  owner's earlier ones until new ones come.
- **Its brightness** (the owner: "Twig looks dark, as if in shadow"): the bake lit Twig's brown texture with the shared
  light and it came out at a mean brightness of 0.46 (the Meshy heroes about 0.62; the Meshy Twig was as dark). The
  bake gained a hero's own `light` (any light, `exposure`, `lift`: a glow of its own albedo) and `color` grade of its
  texture (`gamma`, `gain`, `saturation`, `warm`). The first five variants (light only, up to 0.60) looked alike to the
  owner; of the bolder six (with the grade) the owner chose "H": hemisphere 4.2, key 0.9, fill 0.6, lift 0.2; gamma
  0.75, gain 1.08, saturation 1.1, warm 0.04 (0.73: even and warm, the brown still brown).

## D24. Sprig's Blender model; Twig and Sprig take turns celebrating (2026-10-03; FR-028)

- **Decision**: Sprig's model is the owner's `Sprig_Complete.glb` (Blender, one skinned mesh with its 34-joint rig and
  three embedded 2048 × 2048 textures; `tools/heroanim/SOURCE.md`), replacing the Meshy Sprig. On Home it breathes
  (`Sprig_Breathing`, 4 s) and waves (`Sprig_Wave`, 3 s) at their own lengths. The win and the milestone after it
  celebrate with Twig on odd levels and Sprig on even ones (`CharacterArt.CelebrantOf`), Sprig alternating its
  `Sprig_Celebrate` (2 s, `win`) and `Sprig_Clap` (3 s, `win2`) on its own turns (`CharacterArt.CelebrationTurn`,
  `HeroMotion.WinClip`).
- **Rationale**: the owner's words ("let it take turns with Twig … celebrate/clap, alternate"). Turns by the level number
  rather than a count of wins seen keep it deterministic and the same in both builds, after a restart or a replay, with
  nothing to save; L1, the first win a player sees, stays Twig's.
- **The first two files** (`Sprig_character_Model.glb`, the same bytes twice) held the rigged mesh with a blank color
  texture ("Mixar Sprig_character_Model Color": cream islands only), the colors only on unrigged copies beside it
  (`Sprig_design`, two `Pasted_Image` meshes) and a cube; baked, Sprig came out all cream. The owner re-baked the colors
  onto the rigged mesh.
- **The bake** gained, for this model: a list of win clips (`heroes.json` `win: [..]`, baked as `win`, `win2`); a ground
  point at the model's own base (`ground: "model"`; the mesh lies 4 m from the scene's origin, which the fit had put in
  the cell's middle, shrinking Sprig to a speck); and a top point for a rig without a head-top bone (`topOffset`
  [0, 0.3, 0] up the `Head` bone, at the face's top under the leaves, where the hats sit; `HatLift` stays 0).
- **The kit**: `MotionClip.Win2`; `HeroMotion.WinClip(family, turn)` (two celebrations take turns, one is always
  played, none falls back to the reaction); `HeroMotionPlayer.Celebrate(now, waitForSeam, turn)`; `CharacterArt.Celebrants`
  (Twig, Sprig), `CelebrantOf(level)`, `CelebrationTurn(level)` replace the constant `CharacterArt.Celebrant`. Unity's
  frame sets hold the fourth clip.
- **Files**: 288 Sprig frames, 11.27 MB (the Meshy Sprig had 144); 756 frames, 30.7 MB in all.
- **Not changed**: Sprig's still pictures (A1–A7) and its place on Home; Twig's model and clips.

## D25. Twig and Sprig toned down (2026-10-04; FR-028)

- **Decision**: Twig's bake light and grade (`heroes.json`) soften to hemisphere 3.9, key 0.9, fill 0.6, glow 0.12;
  gamma 0.84, gain 1.0, saturation 0.74, no warm shift; Sprig gains a grade of its texture: gamma 1.04, gain 0.94,
  saturation 0.74. Both re-baked; their frames keep their shapes.
- **Rationale**: the owner found the chestnut (Twig) "too bright", and Sprig too. Measured on their frames (mean
  luminance and saturation of the opaque pixels): Twig 0.73 and 0.73 (after "H", D23), Sprig 0.69 and 0.79, against
  Bloom 0.63 and 0.50 and Drop 0.61 and 0.47. Variant "B" of three per hero brings them to 0.62 and 0.59 (Twig) and
  0.65 and 0.64 (Sprig): as light as the others, still the most colorful (green leaves, an orange-brown acorn) but no
  longer glaring. Twig's lower variants (0.53, 0.47) would bring back the "in shadow" look of D23 (0.46).
- **Alternatives**: "A" (Twig 0.67, Sprig 0.67: a little softer) and "C" (Twig 0.59, Sprig 0.62: browner and greyer)
  were shown with it (`tools/heroanim` scratch bakes of the seam pose).

## D26. The owner's Heroes.glb: four heroes from one file (2026-10-04; FR-028)

- **Decision**: All four heroes are baked from `tools/heroanim/models/heroes.glb`. `heroes.json` names each hero's
  skinned mesh (`mesh`); the bake hides the other three and looks its head points up in that hero's own skeleton, by
  the bones' names in the file (`userData.name`: three.js renames a second "Head" "Head_1"). Drop's still breathing gets
  a breath from the bake (`breathe`: 2.5% across its body bone, once over the loop); Twig, delivered without clips, gets
  stand-in clips the bake makes (`synth`: the arms lowered 55° from its T-pose with its `arms_down` corrective, a breath,
  a hop, two jumps). Each hero is graded to sit with the others while the heroes' mean saturation stays at 0.656 (now
  0.659), so FR-031's backgrounds are unchanged. On Home, Bloom (its new crown of petals) stands at 0.40 of the
  picture's width and Drop at x 0.735, 0.36 tall.
- **Rationale**: every clip of the file moves only its own rig and each hero has its own material and texture, so one
  file serves as well as four; the owner may keep sending one. Baking the stand-ins keeps the four heroes alive on Home
  and the win until the owner's clips come, without a frozen T-pose.
- **Alternatives**: keeping the old Twig until its clips come (the owner chose the new one: "Bake Twig too"); separate
  files per hero (not needed); lowering the backgrounds' saturation to a lower heroes' mean (would change the owner's
  70% choice).


## D27. The guided spotlights; a booster's guided use is free (2026-10-05; FR-034, FR-035)

- **Decision**: The Garden Entry is the owner's look "B", a small stone arch in the border (`UiRaster.EntryArch`,
  turned to its side by `BoardLayout.ArchOf`). The onboarding's spotlight is one engine-free planner (`GuideTour`: which
  steps a level shows) and one layout (`Spotlight`: the scrim with soft holes, rendered at a quarter of the screen, the
  bubble above or below the lit place, the hand), drawn by `GuidePainter` (playtest) and `GuideOverlay` (Unity). A
  forced step lets taps through only inside its holes: the playtest lays the lit place's targets over the scrim's
  catch-all again, Unity's overlay is a raycast filter that is transparent inside the holes. Bloom Burst's guided tap
  takes the whole board and picks the tile under the finger (`IPainter.TapPoint`), as cells are smaller than a touch
  target. A booster's guided use takes no charge and does not count against the clean-clear bonus (the hosts subtract
  their demo uses from the session's booster count); it is marked seen at the use.
- **Rationale**: the owner's request ("make the player tap the item ... leave them the item in any case ... look at how
  other games do it"). Match-3 games give a small stock with the booster's tutorial and spend one of it in the guided
  use, so the player ends with some left: Royal Match gives three of each pre-level booster on reaching Level 7 "as the
  game shows you how they work" ([Old Cynic](https://oldcynic.com/royal-match-tips-and-tricks-cheats-for-new-players),
  [Twinfinite](https://twinfinite.net/guides/how-to-get-boosters-royal-match/)); Diamond Digger Saga gives two or three
  free samples during its tutorial ([Game Developer](https://www.gamedeveloper.com/business/match-3-monetisation-new-booster-selling-trend-in-king-games)).
  Our unlock grants one charge (spec 001 FR-042); a free guided use keeps it, which is the same outcome without
  changing the grant or the economy numbers. Marking the demo seen at the use stops a free use per relaunch.
- **Alternatives**: taking the charge in the guided use (the player would end with none, against the owner's wish to
  leave it); granting two charges and spending one (changes FR-042's grant and the economy config for the same
  result); a skippable, unforced demo card (the previous behaviour; the owner asked for a forced tap); showing the
  blocked entry on Level 5 as rendered (the same situation first happens on Level 2, where it is simpler).

## D28. The win's celebrants face the player (2026-10-05; FR-028)

- **Decision**: A hero with a `winYaw` in `heroes.json` (Twig and Sprig, 0°) is baked twice: at its `yaw` for Home
  (its idle and reaction, as before, the camera's fit still taking its celebrations in, so Home's frames keep their
  size) and at its `winYaw` for the win and the milestone (its celebrations and its idle, `winidle`), each set with its
  own palette. The kit's `HeroMotionPlayer` with `front` (both builds' win and milestone) idles on `WinIdle`, and a
  cross-fade names its idle clip (`HeroPose.FromClip`).
- **Rationale**: the owner's note; the turn suits four heroes round the fountain, not one alone on the stage.
- **Alternatives**: one bake at 0° for every screen (Home's heroes would all face the camera and lose their turn toward
  the middle); mirroring or skewing the frames (cannot turn a 3D pose).

## D29. The owner's Home tuning, from a constructor of the game's own layers (2026-10-05; FR-036)

- **Decision**: Home's look is the owner's own setting, found on a constructor page that built Home from the game's
  layers (the garden, the fountain's layers, each hero in each of six bakes, the logo, the header, the promo scenes and
  their plates, the sun, the plaque, Play, the teaser and the menu, exported by the playtest's painter at 1080 × 2340)
  and moved them with CSS: blur, brightness and saturation of the garden, the stage's and the heroes' scale, an SVG
  sharpening kernel with contrast and saturation on the heroes, the logo's, Play's and the promo scenes' size and place,
  the plates and `drop-shadow`s. Its settings move into the kit as the same quantities in W (or H) shares; what the
  game cannot do per frame is done once offline: the garden's blur in `home.jpg` (`layers.mjs` `gardenBlur`, a
  Gaussian of 4/1080 of its width) and the heroes' finish in their Home frames (`post.mjs`: the constructor's kernel,
  transparent neighbours as black as the browser takes them, then contrast and saturation, before the palette). The
  promo scenes' soft shadows are made from each picture's alpha at the size drawn (`UiRaster.SilhouetteShadow`, three
  box blurs for the Gaussian; the browser's `drop-shadow` blur is the Gaussian's deviation, measured in Chromium), the
  plate's from its rounded box (`UiRaster.RoundShadow`), both cached by the hosts. The scene factor of the Home layers'
  saturation (FR-031) was measured with the heroes before their finish.
- **Rationale**: the owner wanted the heroes, the buttons and the promo scenes "sharper, more in focus"; renders of
  fixed variants were a guess, the constructor let the owner choose each value, and its exported layers matched the
  game's render pixel for pixel at rest, so the settings carry over exactly.
- **Alternatives**: a runtime blur of the garden and a sharpening shader on the heroes (a shader per build, a cost per
  frame, and no shader in the client so far); generated shadow pictures checked in for the nine promo layers (nine more
  files with their records, out of step with the layers whenever they change); a bake with a 3D outline (offered in the
  constructor; the owner kept the current render).
