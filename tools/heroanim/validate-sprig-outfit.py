"""Compare actual GLB reimport with the editable Blender assembly at twelve poses."""
import argparse
import json
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils.kdtree import KDTree


def world_vertices(obj):
    ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh=ev.to_mesh()
    result=np.array([tuple(ev.matrix_world@v.co) for v in mesh.vertices])
    ev.to_mesh_clear()
    return result


def validate(folder):
    bpy.ops.wm.open_mainfile(filepath=str(folder/'sprig-outfit.blend'))
    arm=bpy.data.objects['SprigHeroRig']
    names=['SprigHero','SprigHero_Cap','SprigHero_Glasses','SprigHero_Backpack']
    controls=bpy.data.objects['OutfitControls']
    for k in ['cap_head','glasses','backpack']:controls[k]=True
    controls.update_tag()
    arm.animation_data.action=None;arm.data.pose_position='REST'
    bpy.context.view_layer.update()
    rest={n:world_vertices(bpy.data.objects[n]) for n in names}
    arm.data.pose_position='POSE'
    expected={}
    for clip in ['Study_Idle','Study_Look','Study_Step','Study_Joy']:
        action=bpy.data.actions[clip];arm.animation_data.action=action
        frames=[1,round(action.frame_range[1]*.23),round(action.frame_range[1]*.61)]
        for frame in frames:
            bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
            expected[(clip,frame)]={n:world_vertices(bpy.data.objects[n]) for n in names}
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(folder/'sprig-outfit.glb'))
    arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    imported={}
    for n in names:
        root=bpy.data.objects[n]
        meshes=[o for o in [root]+list(root.children_recursive) if o.type=='MESH']
        if not meshes:
            # The glTF importer keeps an empty named after a morph-bearing node
            # and parents its *_mesh directly to the armature.
            meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name==n+'_mesh']
        assert len(meshes)==1,(n,[o.name for o in meshes])
        imported[n]=meshes[0]
    hero=imported['SprigHero']
    assert 'CapOn' in hero.data.shape_keys.key_blocks
    hero.data.shape_keys.key_blocks['CapOn'].value=1
    arm.animation_data.action=None;arm.data.pose_position='REST'
    bpy.context.view_layer.update()
    mapping={};rest_errors={}
    for n in names:
        points=world_vertices(imported[n]);tree=KDTree(len(rest[n]))
        for i,p in enumerate(rest[n]):tree.insert(p,i)
        tree.balance();matches=[tree.find(p) for p in points]
        mapping[n]=np.array([m[1] for m in matches]);rest_errors[n]=max(m[2] for m in matches)
        assert rest_errors[n]<3e-5,f'{n}: roundtrip rest error {rest_errors[n]}'
    arm.data.pose_position='POSE'
    rows=[]
    for (clip,frame),meshes in expected.items():
        actions=[a for a in bpy.data.actions if clip in a.name and any('pose.bones' in f.data_path for f in a.fcurves)]
        assert len(actions)==1,(clip,[a.name for a in actions])
        arm.animation_data.action=actions[0]
        hero.data.shape_keys.key_blocks['CapOn'].value=1
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
        errors={}
        for n in names:
            actual=world_vertices(imported[n]);target=meshes[n][mapping[n]]
            error=np.linalg.norm(actual-target,axis=1)
            errors[n]=dict(max=float(error.max()),p99=float(np.quantile(error,.99)),vertices=len(actual))
            assert error.max()<3e-4,f'{clip}/{frame}/{n}: roundtrip mismatch {error.max()}'
        rows.append(dict(clip=clip,frame=frame,meshes=errors))
    report=dict(restMaxErrors=rest_errors,poses=rows,tolerance=.0003,maxVertexError=max(m['max'] for row in rows for m in row['meshes'].values()))
    (folder/'roundtrip-report.json').write_text(json.dumps(report,indent=2)+'\n')
    print('OUTFIT ROUNDTRIP',json.dumps(dict(poses=len(rows),maxError=report['maxVertexError'])))


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True)
    args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    validate(args.out)
