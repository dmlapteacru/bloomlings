"""Independently compare accepted and finished base/Head rest triangles, including UVs."""
import bpy,json,hashlib
from pathlib import Path
import numpy as np
H=Path(__file__).resolve().parent;R=H.parent.parent;S=H/'models/wardrobe-finish-v1';config=json.loads((S/'fit.json').read_text())
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()

def fingerprint(obj):
    rows=[]
    for o in [obj,*obj.children_recursive]:
        if o.type!='MESH':continue
        uv=o.data.uv_layers.active
        for polygon in o.data.polygons:
            face=[]
            for i in polygon.loop_indices:
                v=o.data.vertices[o.data.loops[i].vertex_index];point=o.matrix_world@v.co
                face.append(tuple(round(float(c),5) for c in [*point,*uv.data[i].uv]))
            rows.append(tuple(sorted(face)))
    return sorted(rows)

report=dict(version=1,scriptSha256=sha(__file__),families=[])
for family in ['sprig','bloom','twig','drop']:
    if family=='drop':source=R/'content/work/drop-v2/game/DropHeroV2.glb'
    else:
        meta=json.loads((H/'models'/f'{family}-v2/build-report.json').read_text());source=R/f'content/work/{family}-v2/build'/meta['export']['file']
    finished=R/f'content/work/wardrobe-finish-v1/{family}/WardrobeFinish.glb'
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source))
    if family=='drop':keys=[o.name for o in bpy.data.objects if o.name.startswith('DropHeroV2_Base_')]+['DropHeroV2_'+key for key in json.loads((H/'drop-v2.json').read_text())['heads']]
    else:keys=[family.title()+'V2_base']+[family.title()+'V2_'+k for k in config['families'][family]['heads'] if bpy.data.objects.get(family.title()+'V2_'+k)]
    expected={key:fingerprint(bpy.data.objects[key]) for key in keys};assert all(expected.values()), 'No empty base / Head comparisons'
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(finished))
    checks=[]
    for key,triangles in expected.items():
        target=key if family=='drop' else 'Finish_'+key.split('_',1)[1]
        assert triangles==fingerprint(bpy.data.objects[target]),(family,key,'Base/Head positions or UVs changed')
        checks.append(dict(part=target,polygonCount=len(triangles),worldGeometryAndUvUnchanged=True))
    report['families'].append(dict(family=family,acceptedModelSha256=sha(source),finishedModelSha256=sha(finished),checks=checks))
    print('Verified accepted skin and Head UV/geometry:',family,len(checks),'parts',flush=True)
(S/'base-validation.json').write_text(json.dumps(report,indent=2)+'\n')
