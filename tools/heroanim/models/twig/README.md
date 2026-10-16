# Owner's modular Twig exports

The owner's Blender project remains the single authoring master. These are original FBX exchange exports supplied
for in-game integration, renamed on disk without rewriting their contents:

| Original delivery | Repository filename |
|---|---|
| `BaseHero.fbx` | `source/TwigHero.fbx` |
| `BaseHero_AcornHat.fbx` | `source/TwigHero_AcornHat.fbx` |
| `BaseHero_FlowerCap.fbx` | `source/TwigHero_FlowerCap.fbx` |
| `BaseHero@Anims.fbx` | `source/TwigHero@Anims.fbx` |

The meshes have no UV maps or texture images. `prepare-twig-gear.py` adds temporary vertex-colour materials in the
existing Twig color direction without changing mesh coordinates, topology or weights. It renames imported objects
in the derived render input to `TwigHero…`; original FBX internal names remain untouched. This temporary color pass
is not a transplant of the legacy UV texture. The reusable recipe is `../../twig-materials.json`.

The body and both accessories share the same 25-bone rest rig. The animation-only export has the same bone names
and hierarchy but different rest axes; its four actions contain motion and need retargeting before being used on the
mesh rig. This equipment pilot uses the owner's base rest pose and the runtime's subtle 2D breath. Original animations
remain preserved in `TwigHero@Anims.fbx`; no source animation is silently relabeled or played on incompatible axes.

Rebuild from repository root:

```bash
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-gear.py
node tools/heroanim/bake-twig-gear.mjs
node tools/heroanim/check.mjs
```

Derived GLB and intermediate renders go to ignored `content/work/twig-gear/`; no second editable Blender master is
created. Runtime sprites go to the existing bundled HeroMotion folder. FBX, GLB and reference pictures never enter
APK assets. Source hashes, rig differences and geometry signatures are in `build-report.json`; final runtime hashes
are in `../../twig-gear.json`. Final image references: `../../references/twig/`.
