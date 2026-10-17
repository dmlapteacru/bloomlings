# TwigHeroV2 modeling references — current

[Contact sheet](contact-sheet.jpg).

These are the conversation's original generated images, preserved unchanged. The owner modeled / assembled the
approved delivery in their own Blender project. Geometry, alignment and material delivery are authoritative in
[the supplied ZIP](../../models/twig-v2/source/TwigHeroV2.zip); illustrations guide future modeling and color.
`manifest.json` pins the exact bytes. They are offline references and are never packaged in the game.

| Image | Purpose |
| --- | --- |
| [base-full-front.png](base-full-front.png) | Bald base: short body, common face, ears and proportions |
| [acorn-head-front.png](acorn-head-front.png) | Default acorn hat on the common head |
| [panama-head-front.png](panama-head-front.png) | Summer Panama on the common head |
| [base-and-two-hairstyles.png](base-and-two-hairstyles.png) | Base and early separate hair choices |
| [acorn-hair.png](acorn-hair.png) | Hair under default acorn hat |
| [woodland-hairstyles.png](woodland-hairstyles.png) | Additional woodland hair shapes |
| [glasses-clear.png](glasses-clear.png) | Rounded green glasses |
| [glasses-sun.png](glasses-sun.png) | Brown eyewear color reference; current replacement mesh is clear round glasses |
| [beret-scarf.png](beret-scarf.png) | Autumn beret, leaf and golden scarf |
| [earmuffs-hair.png](earmuffs-hair.png) | Winter earmuffs with restrained short hair |
| [holiday-hat.png](holiday-hat.png) | Holiday hat with acorn ornament |
| [seasonal-clothes.png](seasonal-clothes.png) | Winter / autumn / summer clothing references; delivered as Jacket / Coat / Overalls |

`identity/` holds the full-resolution new Twig avatar and medal sources. `pack-twig-identity.py` publishes the three
384px JPEG avatars under their existing game IDs; `pack_badges.py --only hero_twig` publishes the 256px medal.
No prices, unlock requirements or user-chosen avatar / frame / badge selections change.

The installed L2000 mosaic intentionally retains its previous illustration until the owner requests a level-art update.
