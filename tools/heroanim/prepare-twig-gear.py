"""Derive a runtime render input from the owner's Twig exports; never a second Blender master.
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-gear.py
Only temporary vertex-colour materials are added. No mesh coordinates, topology, rest bones or weights change.
"""
import hashlib
import json
import math
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
OUT = ROOT / 'content/work/twig-gear'
SOURCE = HERE / 'models/twig/source'
NAMES = ['TwigHero.fbx', 'TwigHero_AcornHat.fbx', 'TwigHero_FlowerCap.fbx', 'TwigHero@Anims.fbx']
RECIPE = HERE / 'twig-materials.json'
COLORS = json.loads(RECIPE.read_text())['colors']


def sha(file):
    return hashlib.sha256(Path(file).read_bytes()).hexdigest()


def signature(obj):
    points = np.array([tuple(v.co) for v in obj.data.vertices], dtype=np.float32)
    weights = [[(obj.vertex_groups[g.group].name, round(g.weight, 7)) for g in v.groups] for v in obj.data.vertices]
    return dict(vertices=len(points), triangles=sum(len(p.vertices)-2 for p in obj.data.polygons),
                coordinates=hashlib.sha256(points.tobytes()).hexdigest(),
                weights=hashlib.sha256(json.dumps(weights).encode()).hexdigest(),
                uvLayers=len(obj.data.uv_layers), unweighted=sum(not v.groups for v in obj.data.vertices))


def linear(hex_color):
    rgb = [int(hex_color[i:i+2], 16)/255 for i in (1, 3, 5)]
    return np.array([c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4 for c in rgb])


PALETTE = {name: linear(value) for name, value in COLORS.items()}


def blend(a, b, factor):
    k = min(1., max(0., factor))
    return a*(1-k)+b*k


def smooth(value, lo, hi):
    t = min(1., max(0., (value-lo)/(hi-lo)))
    return t*t*(3-2*t)


def colour(name, point, group, eye, flower):
    x, y, z = point
    if name == 'TwigHero_AcornHat':
        return PALETTE['leaf'] if z > 1.49 and x > .065 else PALETTE['wood']
    if name == 'TwigHero_FlowerCap':
        c = PALETTE['olive']
        if (y < -.27 and z < 1.23) or (y < -.55 and z < 1.31): c = PALETTE['visor']
        if flower:
            c = PALETTE['petal']
            radius = ((x-.307)/.055)**2+((z-1.426)/.055)**2
            c = blend(c, PALETTE['pollen'], smooth(1.2-radius, 0, .4))
        if z > 1.755: c = PALETTE['visor']
        return c
    if eye: return PALETTE['eye']
    c = PALETTE['gold']
    if any(group.startswith(p) for p in ('UpperArm', 'Forearm', 'Hand', 'Thigh', 'Shin', 'Foot')):
        c = PALETTE['wood']
    if .56 < z < .88 and abs(x) > .49 and y > -.20: c = PALETTE['wood']
    if .18 < z < .48 and y < -.12 and abs(x) < .23: c = PALETTE['bib']
    if y < -.28:
        # Whites around the owner's separate dark eye surfaces, without painting over them.
        eye_distance = min(((x+.222)/.165)**2+((z-.863)/.15)**2,
                           ((x-.245)/.165)**2+((z-.862)/.15)**2)
        c = blend(c, PALETTE['sclera'], smooth(1-eye_distance, 0, .25))
        brow = min(((x+.224)/.13)**2+((z-1.055)/.025)**2,
                   ((x-.244)/.13)**2+((z-1.055)/.025)**2)
        c = blend(c, PALETTE['wood'], smooth(1-brow, 0, .4))
        # Paint the modeled mouth recess, not a dark ellipse on the surrounding face.
        if abs(x-.01) < .16 and .59 < z < .735:
            c = blend(c, PALETTE['mouth'], smooth(y, -.465, -.445))
        tongue = ((x-.015)/.065)**2+((z-.632)/.034)**2
        if y > -.48: c = blend(c, PALETTE['tongue'], smooth(1-tongue, 0, .4))
        cheek = min(((x+.358)/.125)**2+((z-.708)/.085)**2,
                    ((x-.378)/.125)**2+((z-.708)/.085)**2)
        c = blend(c, PALETTE['blush'], max(0., 1-cheek)*.65)
    return c


def paint(obj):
    attr = obj.data.color_attributes.new(name='TwigTemporaryPalette', type='FLOAT_COLOR', domain='CORNER')
    groups = {g.index: g.name for g in obj.vertex_groups}
    # The flower is a separate connected surface in the owner's export; color that actual geometry.
    parents = list(range(len(obj.data.vertices)))
    def find(i):
        while parents[i] != i:
            parents[i] = parents[parents[i]]; i = parents[i]
        return i
    for edge in obj.data.edges:
        a,b = map(find, edge.vertices); parents[a] = b
    counts = {}
    for i in range(len(parents)): counts[find(i)] = counts.get(find(i), 0)+1

    slots = {}
    for i, old in enumerate(list(obj.data.materials)):
        mat = bpy.data.materials.new(obj.name+('_Eyes' if 'Eye' in old.name else '_Temporary'))
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Roughness'].default_value = .22 if 'Eye' in old.name else .78
        node = mat.node_tree.nodes.new('ShaderNodeVertexColor'); node.layer_name = attr.name
        mat.node_tree.links.new(node.outputs['Color'], bsdf.inputs['Base Color'])
        obj.data.materials[i] = mat
        slots[i] = 'Eye' in old.name
    for poly in obj.data.polygons:
        for index in poly.loop_indices:
            v = obj.data.vertices[obj.data.loops[index].vertex_index]
            group = groups[max(v.groups, key=lambda g:g.weight).group] if v.groups else ''
            c = colour(obj.name, obj.matrix_world@v.co, group, slots[poly.material_index], obj.name == 'TwigHero_FlowerCap' and counts[find(v.index)] < 2000)
            attr.data[index].color = (*c, 1)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    report = dict(version=1, inputs=[], skeletonChecks=[], meshes={}, sourceAnimations=[],
                  materialRecipe=str(RECIPE.relative_to(ROOT)), materialRecipeSha256=sha(RECIPE),
                  sourceGeometryChanged=False, hasSourceUVs=False)
    master = None
    meshes = []
    imported_actions = []
    for file_name in NAMES:
        file = SOURCE/file_name
        before = set(bpy.data.objects); actions_before = set(bpy.data.actions)
        bpy.ops.import_scene.fbx(filepath=str(file))
        objects = set(bpy.data.objects)-before
        arms = [o for o in objects if o.type == 'ARMATURE']
        assert len(arms) == 1, file_name
        arm = arms[0]
        if master is None:
            master = arm
        else:
            assert list(arm.data.bones.keys()) == list(master.data.bones.keys()), 'Rig bone names differ'
            error = max(float(np.max(abs(np.array(master.data.bones[b.name].matrix_local)-np.array(b.matrix_local)))) for b in arm.data.bones)
            if file_name != 'TwigHero@Anims.fbx':
                assert error < 1e-4, f'Incompatible accessory rest rig: {file_name}: {error}'
            assert all((b.parent.name if b.parent else None) == (master.data.bones[b.name].parent.name if master.data.bones[b.name].parent else None) for b in arm.data.bones)
            report['skeletonChecks'].append(dict(file=file_name, bones=len(arm.data.bones), maxRestMatrixError=error, requiresAnimationRetarget=error>=1e-4))
        report['inputs'].append(dict(path=str(file.relative_to(ROOT)), original=file_name.replace('TwigHero', 'BaseHero'), sha256=sha(file), bytes=file.stat().st_size))
        for obj in objects:
            if obj.type != 'MESH': continue
            obj.name = obj.name.replace('BaseHero', 'TwigHero')
            sig = signature(obj); assert sig['unweighted'] == 0
            world = obj.matrix_world.copy(); obj.parent = master; obj.matrix_world = world
            for mod in obj.modifiers:
                if mod.type == 'ARMATURE': mod.object = master
            paint(obj)
            assert signature(obj) == sig, 'Changed source geometry/UV/weights'
            report['meshes'][obj.name] = sig
            meshes.append(obj)
        imported_actions.extend(sorted(set(bpy.data.actions)-actions_before, key=lambda a:a.name))
        if arm != master: bpy.data.objects.remove(arm, do_unlink=True)
    master.name = 'TwigHeroRig'
    for action in imported_actions:
        action.name = action.name.split('|')[-1].replace('BaseHero', 'TwigHero')
        action.use_fake_user = True
        varying = sum(1 for f in action.fcurves if f.keyframe_points and max(k.co.y for k in f.keyframe_points)-min(k.co.y for k in f.keyframe_points)>1e-6)
        report['sourceAnimations'].append(dict(name=action.name, frames=list(action.frame_range), varyingCurves=varying))
    # Preserve unretargeted motion in the original FBX only, not as misleading clips in the derived model.
    for action in imported_actions: bpy.data.actions.remove(action)
    master.animation_data_create()
    # The animation-only FBX has different rest axes. Keep its source actions in FBX;
    # do not silently play them on the mesh rig. This equipment pilot uses the base rest pose.
    preview = bpy.data.actions.new('TwigHero_Preview')
    master.animation_data.action = preview
    for bone in master.pose.bones:
        bone.rotation_mode = 'QUATERNION'
        bone.location = (0, 0, 0); bone.rotation_quaternion = (1, 0, 0, 0); bone.scale = (1, 1, 1)
        for frame in (1, 2): bone.keyframe_insert('rotation_quaternion', frame=frame)
    report['runtimePose'] = 'Unchanged base rig rest pose; source animation axes require retargeting in later action feature'
    bpy.context.scene.frame_set(1)
    model = OUT/'TwigHero.glb'
    bpy.ops.export_scene.gltf(filepath=str(model), export_format='GLB', export_animations=True,
        export_animation_mode='ACTIONS', export_force_sampling=True, export_frame_range=False,
        export_skins=True, export_all_influences=False, export_influence_nb=4)
    report['derivedGlbSha256'] = sha(model)
    (OUT/'build-report.json').write_text(json.dumps(report, indent=2)+'\n')
    print('Twig prepared:', json.dumps(dict(meshes=len(meshes), sourceAnimations=report['sourceAnimations'], glbBytes=model.stat().st_size)))


if __name__ == '__main__': main()
