"""Derive a runtime render input from the owner's Twig exports; never a second Blender master.
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-gear.py
Projects the clean front paint onto a derived UV layer, applies the recorded acorn fit and filters render normals.
Original FBX files, body coordinates, topology, rest bones and weights are retained.
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
SETTINGS = json.loads(RECIPE.read_text())
COLORS = SETTINGS['colors']


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
        bsdf.inputs['Roughness'].default_value = SETTINGS['materials']['eyes' if 'Eye' in old.name else 'accessories' if obj.name != 'TwigHero' else 'skin']
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


def fit_acorn(obj):
    fit = SETTINGS['acornFit']
    inverse = obj.matrix_world.inverted()
    for vertex in obj.data.vertices:
        p = obj.matrix_world@vertex.co
        p.x *= fit['width']; p.y *= fit['depth']
        k = min(1., max(0., (p.z-fit['crownStart'])/(fit['crownEnd']-fit['crownStart'])))
        p.z += fit['lift']+fit['crownLift']*k
        vertex.co = inverse@p


def polish_normals(obj):
    """Filter scanned surface noise in render normals only; retain vertices, weights and silhouette."""
    recipe = SETTINGS['surface']
    mesh = obj.data
    # Imported custom split normals retain the generator's tiny dents. Start from smooth vertex normals.
    normals = np.array([tuple(v.normal) for v in mesh.vertices], dtype=np.float64)
    edges = np.array([tuple(e.vertices) for e in mesh.edges], dtype=np.int32)
    a, b = edges[:, 0], edges[:, 1]
    for _ in range(recipe['normalPasses']):
        # Do not blend across a sharp feature or between disconnected surfaces (eyes / cap flower).
        allowed = (normals[a]*normals[b]).sum(axis=1) > recipe['featureCosine']
        left, right = a[allowed], b[allowed]
        summed = normals.copy(); count = np.ones(len(normals))
        np.add.at(summed, left, normals[right]); np.add.at(summed, right, normals[left])
        np.add.at(count, left, 1); np.add.at(count, right, 1)
        filtered = summed/count[:, None]
        normals = normals*(1-recipe['normalBlend'])+filtered*recipe['normalBlend']
        normals /= np.maximum(np.linalg.norm(normals, axis=1)[:, None], 1e-10)
    for polygon in mesh.polygons: polygon.use_smooth = True
    mesh.normals_split_custom_set_from_vertices(normals.tolist())
    return dict(**recipe, scope='Derived render normals only; no geometry, weights or silhouette edits')


def project_front(obj):
    """A dedicated planar UV pass for the current frontal meta pose; rear surfaces keep their base materials."""
    recipe = SETTINGS['frontPaint']
    image = bpy.data.images.load(str(ROOT/recipe['path']), check_existing=True)
    image.pack()
    width, height = image.size
    pixels = np.array(image.pixels[:], dtype=np.float32).reshape((height, width, 4))
    crop_x, crop_y, crop_w, crop_h = recipe['sourceCrop']
    left, top, right, bottom = recipe['paintBox']
    half = recipe['sourceHalfHeight']
    uv = obj.data.uv_layers.new(name='TwigFrontColor')
    def pixel(point):
        x,y,z = point
        screen_x = 224+x*504/(2*half)
        screen_y = (recipe['sourceCameraY']+half-z)*504/(2*half)
        return (left+(screen_x-crop_x)/crop_w*(right-left), top+(screen_y-crop_y)/crop_h*(bottom-top))
    painted_slots = {}
    for i, old in enumerate(list(obj.data.materials)):
        mat = bpy.data.materials.new(old.name+'_FrontColor')
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Roughness'].default_value = SETTINGS['materials']['eyes' if 'Eyes' in old.name else 'skin']
        tex = mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image = image; tex.extension = 'EXTEND'
        coords = mat.node_tree.nodes.new('ShaderNodeUVMap'); coords.uv_map = uv.name
        mat.node_tree.links.new(coords.outputs['UV'], tex.inputs['Vector'])
        mat.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
        painted_slots[i] = len(obj.data.materials); obj.data.materials.append(mat)
    painted = 0
    for poly in obj.data.polygons:
        points = [obj.matrix_world@obj.data.vertices[obj.data.loops[i].vertex_index].co for i in poly.loop_indices]
        for i, point in zip(poly.loop_indices, points):
            x,y = pixel(point); uv.data[i].uv = (x/width, 1-y/height)
        center = sum(points, Vector())/len(points)
        x,y = pixel(center)
        px,py = int(x),height-1-int(y)
        facing = center.y < (.05 if center.z > .5 else -.09)
        if facing and 0 <= px < width and 0 <= py < height and pixels[py,px,3] > .5:
            poly.material_index = painted_slots[poly.material_index]; painted += 1
            # glTF exports COLOR_0 on all primitives: paint must not be multiplied by the old palette.
            for loop in poly.loop_indices: obj.data.color_attributes['TwigTemporaryPalette'].data[loop].color = (1, 1, 1, 1)
    return dict(texture=recipe['path'], textureSha256=sha(ROOT/recipe['path']), uv=uv.name, frontPolygons=painted,
                coverage='Current front pose; rear/hidden surfaces retain clean palette materials')


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    report = dict(version=3, inputs=[], skeletonChecks=[], meshes={}, sourceAnimations=[], derivedFits={}, frontPaint={}, surface={},
                  materialRecipe=str(RECIPE.relative_to(ROOT)), materialRecipeSha256=sha(RECIPE),
                  originalSourceFilesChanged=False, hasSourceUVs=False)
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
            assert signature(obj) == sig, 'Palette pass changed source geometry/UV/weights'
            if obj.name == 'TwigHero': report['frontPaint'] = project_front(obj)
            if obj.name == 'TwigHero_AcornHat':
                fit_acorn(obj)
                report['derivedFits'][obj.name] = SETTINGS['acornFit']
            report['surface'][obj.name] = polish_normals(obj)
            derived = signature(obj)
            assert derived['weights'] == sig['weights'] and derived['vertices'] == sig['vertices'] and derived['triangles'] == sig['triangles']
            if obj.name != 'TwigHero_AcornHat': assert derived['coordinates'] == sig['coordinates']
            report['meshes'][obj.name] = dict(source=sig, derived=derived)
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
