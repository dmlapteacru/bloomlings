"""Fit segmented Tripo headwear onto the owner's ONE unchanged base head.

blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/twig-tripo-study.py
No second Blender master, no rig invention, no runtime assets. Original ZIPs stay intact.
"""
import hashlib
import json
import math
import zipfile
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Euler, Vector

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
SOURCE = HERE/'models/twig-tripo'
OUT = ROOT/'content/work/twig-tripo-study/build'
RECIPE = json.loads((HERE/'twig-tripo-fit.json').read_text())


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def signature(obj):
    positions = np.array([tuple(v.co) for v in obj.data.vertices], dtype=np.float32)
    uv = np.array([tuple(v.uv) for v in obj.data.uv_layers.active.data], dtype=np.float32)
    return dict(vertices=len(positions), triangles=sum(len(p.vertices)-2 for p in obj.data.polygons),
                coordinates=hashlib.sha256(positions.tobytes()).hexdigest(), uv=hashlib.sha256(uv.tobytes()).hexdigest())


def parent(obj, target):
    world = obj.matrix_world.copy()
    obj.parent = target
    obj.matrix_world = world


def empty(name, target=None, location=(0, 0, 0)):
    obj = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent = target
    obj.location = location
    bpy.context.view_layer.update()
    return obj


def clean_materials(obj):
    """Retain original base color / UV. Remove noisy normal and metallic shading on the render copy only."""
    for old in list(obj.data.materials):
        bsdf = old.node_tree.nodes.get('Principled BSDF')
        for name in ['Metallic', 'Roughness', 'Normal']:
            for link in list(bsdf.inputs[name].links): old.node_tree.links.remove(link)
        bsdf.inputs['Metallic'].default_value = RECIPE['material']['metallic']
        bsdf.inputs['Roughness'].default_value = RECIPE['material']['roughness']


def eye_materials(obj):
    """Add eye gloss while retaining the original painted colors and geometry."""
    original = obj.data.materials[0]
    iris = original.copy(); iris.name = 'TwigHero_EyesGloss'
    iris.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = .18
    iris_index = len(obj.data.materials); obj.data.materials.append(iris)
    counts = dict(glossPolygons=0, textureRepainted=False)
    for poly in obj.data.polygons:
        points = [obj.matrix_world@obj.data.vertices[i].co for i in poly.vertices]
        center = sum(points, Vector())/len(points)
        distance = min(((center.x-x)/.105)**2+((center.z-.604)/.103)**2 for x in [-.164, .164])
        if center.y > -.17 or distance > 1: continue
        poly.material_index = iris_index
        counts['glossPolygons'] += 1
    return counts


def export(file, objects):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/file), export_format='GLB', use_selection=True,
                             export_animations=False, export_extras=True)
    return dict(file=file, sha256=sha(OUT/file), bytes=(OUT/file).stat().st_size)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source_record = json.loads((SOURCE/'source-manifest.json').read_text())
    report = dict(version=1, sources=[], parts={}, excludedParts={}, exports=[], rigs=[],
                  recipeSha256=sha(HERE/'twig-tripo-fit.json'), scriptSha256=sha(__file__),
                  runtimeAssetsChanged=False, originalSourceFilesChanged=False, baseGeometryChanged=False)
    groups = {}
    for entry in source_record['files']:
        archive = SOURCE/'source'/entry['file']
        assert sha(archive) == entry['sha256'], archive
        folder = OUT/'extracted'/entry['key']; folder.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(archive) as zipped:
            for item in zipped.infolist():
                assert (folder/item.filename).resolve().is_relative_to(folder.resolve())
            zipped.extractall(folder)
        before = set(bpy.data.objects)
        fbx = next(folder.glob('*.fbx'))
        bpy.ops.import_scene.fbx(filepath=str(fbx))
        imported = set(bpy.data.objects)-before
        assert not any(o.type == 'ARMATURE' for o in imported), 'Unexpected source rig; audit before assigning headwear'
        non_meshes = [o for o in imported if o.type != 'MESH']
        by_part = {int(o.name.split('tripo_part_')[-1].split('.')[0]):o for o in imported if o.type == 'MESH'}
        key = entry['key']
        keep = set(by_part) if key == 'base' else set(RECIPE[key]['parts'])
        report['excludedParts'][key] = sorted(set(by_part)-keep)
        groups[key] = {}
        for number, obj in by_part.items():
            if number not in keep:
                bpy.data.objects.remove(obj, do_unlink=True)
                continue
            original = signature(obj)
            obj.name = f'TwigHero_{key}_part_{number}'
            clean_materials(obj)
            if key == 'base' and number == 0 and RECIPE['material']['polishEyes']:
                report['eyeMaterials'] = eye_materials(obj)
            assert signature(obj) == original, 'Material cleanup changed geometry or UV'
            obj['tripoSource'] = key; obj['tripoPart'] = number
            report['parts'][obj.name] = dict(source=original, derived=signature(obj))
            groups[key][number] = obj
        for obj in non_meshes: bpy.data.objects.remove(obj, do_unlink=True)
        report['sources'].append(dict(**entry, fbxSha256=sha(fbx), rigged=False))
    root = empty('TwigHero_Tripo')
    root['headVariant'] = 'Bare / Acorn / FlowerCap; one shared face, no source skeleton'
    body_root = empty('TwigHero_Body', root)
    head_root = empty('TwigHero_HeadPivot', root, RECIPE['headPivot'])
    head = [groups['base'][i] for i in RECIPE['headParts']]
    body = [o for i,o in groups['base'].items() if i not in RECIPE['headParts']]
    for obj in body: parent(obj, body_root)
    for obj in head: parent(obj, head_root)
    accessories = {}
    for key in ['acorn','cap']:
        group = empty('TwigHero_AcornHat' if key == 'acorn' else 'TwigHero_FlowerCap', head_root)
        group['variant'] = key
        fit = RECIPE[key]
        transform = Matrix.Translation(fit['translation']) @ Matrix.Diagonal((*fit['scale'], 1))
        if 'rotationDegrees' in fit:
            pivot = Matrix.Translation(fit['rotationPivot'])
            rotation = Euler(tuple(math.radians(v) for v in fit['rotationDegrees']), 'XYZ').to_matrix().to_4x4()
            transform = pivot @ rotation @ pivot.inverted() @ transform
        for obj in groups[key].values():
            obj.matrix_world = transform @ obj.matrix_world
            parent(obj, group)
            assert signature(obj) == report['parts'][obj.name]['source'], 'Fitting must only change object transforms'
        accessories[key] = [group, *groups[key].values()]
    common = [root, body_root, head_root]
    report['exports'].append(export('TwigHero_Base.glb', common+body+head))
    for key, label in [('acorn','Acorn'),('cap','FlowerCap')]:
        report['exports'].append(export(f'TwigHero_{label}Head.glb', [root, head_root]+head+accessories[key]))
        report['exports'].append(export(f'TwigHero_{label}Hat.glb', [root, head_root]+accessories[key]))
    report['exports'].append(export('TwigHero_Review.glb', common+body+head+accessories['acorn']+accessories['cap']))
    report['headParts'] = [obj.name for obj in head]
    report['headPivot'] = RECIPE['headPivot']
    report['fitting'] = {k:RECIPE[k] for k in ['acorn','cap']}
    report['notes'] = ['All variants share the same base head, ears and mouth geometry / UV / textures.',
                       'Only headwear extracted from head generations; hallucinated bodies / faces are excluded.',
                       'No rig or animations in source exports. This is a static fitting study, not an animated integration.',
                       'No new editable Blender master is saved. Import exchange GLBs into the owner master.']
    (OUT/'build-report.json').write_text(json.dumps(report, indent=2)+'\n')
    print('Twig Tripo fit:', json.dumps(dict(parts=len(report['parts']), headParts=report['headParts'], exports=len(report['exports']))))


if __name__ == '__main__': main()
