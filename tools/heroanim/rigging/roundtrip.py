"""Reimport every delivered FBX and compare actual animated vertex positions."""
import bpy
import hashlib
import json
import sys
from pathlib import Path

import numpy as np
from mathutils.kdtree import KDTree

HERE = Path(__file__).resolve().parent
FAMILY = sys.argv[sys.argv.index('--') + 1]
ROOT = HERE.parents[2] / 'content/work/rigging' / FAMILY


def coords(obj):
    ev = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = ev.to_mesh()
    a = np.empty(len(mesh.vertices)*3, dtype=np.float32)
    mesh.vertices.foreach_get('co', a)
    a = a.reshape(-1, 3)
    m = np.array(ev.matrix_world)
    a = a @ m[:3, :3].T + m[:3, 3]
    ev.to_mesh_clear()
    return a


def skin(obj):
    names = sorted(g.name for g in obj.vertex_groups)
    indices = {g.index:names.index(g.name) for g in obj.vertex_groups}
    a = np.zeros((len(obj.data.vertices),len(names)), dtype=np.float32)
    for v in obj.data.vertices:
        for g in v.groups:
            a[v.index,indices[g.group]] = g.weight
    return names,a


def albedos(obj):
    result=set()
    for mat in obj.data.materials:
        if not mat or not mat.use_nodes:
            continue
        bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        if not bs:
            continue
        for link in bs.inputs['Base Color'].links:
            if link.from_node.type=='TEX_IMAGE':
                image=link.from_node.image
                data=bytes(image.packed_file.data) if image.packed_file else Path(bpy.path.abspath(image.filepath)).read_bytes()
                result.add(hashlib.sha256(data).hexdigest())
    return sorted(result)


bpy.ops.wm.open_mainfile(filepath=str(ROOT/'rig-cache.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
objects={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
arm.animation_data.action=None
arm.data.pose_position='REST'
bpy.context.view_layer.update()
rest={n:coords(o) for n,o in objects.items()}
weights={n:skin(o) for n,o in objects.items()}
uvs={n:np.array([l.uv[:] for l in o.data.uv_layers.active.data]) for n,o in objects.items()}
morphs={n:{k.name:np.array([p.co[:] for p in k.data]) for k in o.data.shape_keys.key_blocks} if o.data.shape_keys else {} for n,o in objects.items()}
colors={n:albedos(o) for n,o in objects.items()}
bones={b.name:np.array(b.matrix_local) for b in arm.data.bones}
parents={b.name:b.parent.name if b.parent else None for b in arm.data.bones}
report={'family':FAMILY,'files':[],'samples':[],'maxRestError':0.,'maxSkinError':0.,'maxMotionError':0.,'maxUvError':0.,'boneCount':len(bones)}
report['rigReportSha256']=hashlib.sha256((ROOT/'rig-report.json').read_bytes()).hexdigest()
samples={}
arm.data.pose_position='POSE'
for action in sorted(bpy.data.actions,key=lambda a:a.name):
    if '_QA_' in action.name:
        continue
    arm.animation_data.action=action
    for fraction in [0,.23,.61]:
        frame=round(float(action.frame_range[0]+(action.frame_range[1]-action.frame_range[0])*fraction))
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        samples[(action.name,frame)]={n:coords(o).astype(np.float32) for n,o in objects.items()}

bpy.ops.wm.read_factory_settings(use_empty=True)
inventory=json.loads((ROOT/'handoff/inventory.json').read_text())
report['inventorySha256']=hashlib.sha256((ROOT/'handoff/inventory.json').read_bytes()).hexdigest()
shared=None
imported={}
for entry in inventory['files']:
    file=ROOT/'handoff'/entry['path']
    assert hashlib.sha256(file.read_bytes()).hexdigest()==entry['sha256']
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(file),use_anim=True)
    added=set(bpy.data.objects)-before
    rigs=[o for o in added if o.type=='ARMATURE']
    assert len(rigs)==1
    rig=rigs[0]
    assert set(b.name for b in rig.data.bones)==set(bones),(file.name,'bone names')
    error=max(float(np.abs(np.array(b.matrix_local)-bones[b.name]).max()) for b in rig.data.bones)
    # Animation-only FBX decomposes float32 bone axes once more than a skinned
    # model FBX. Allow 1e-5 in its matrix, then still verify every animated vertex.
    assert error<1e-5,(file.name,'bone rest mismatch',error)
    assert all(parents[b.name]==(b.parent.name if b.parent else None) for b in rig.data.bones)
    if shared is None:
        shared=rig
    for obj in added:
        if obj.type!='MESH':
            continue
        assert obj.name in objects,(file.name,obj.name,'mesh name')
        world=obj.matrix_world.copy()
        obj.parent=shared
        obj.matrix_world=world
        for modifier in obj.modifiers:
            if modifier.type=='ARMATURE':
                modifier.object=shared
        imported[obj.name]=obj
    if rig!=shared:
        bpy.data.objects.remove(rig,do_unlink=True)
    report['files'].append({'file':file.name,'boneRestError':error})
assert set(imported)==set(objects),'Missing delivered mesh'
shared.animation_data_create()
shared.animation_data.action=None
shared.data.pose_position='REST'
bpy.context.view_layer.update()
mapping={}
for name,obj in imported.items():
    a=coords(obj)
    assert a.shape==rest[name].shape,(name,'vertex count',a.shape,rest[name].shape)
    old_names,old_weights=weights[name]
    new_names,new_weights=skin(obj)
    # FBX can omit empty groups; compare their bone-aligned dense rows.
    aligned=np.zeros_like(old_weights)
    for i,n in enumerate(new_names):
        assert n in old_names or np.max(new_weights[:,i])<1e-8,(name,n,'unexpected skin group')
        if n in old_names:
            aligned[:,old_names.index(n)]=new_weights[:,i]
    if np.max(np.abs(a-rest[name]))<3e-6 and np.max(np.abs(aligned-old_weights))<3e-5:
        indices=np.arange(len(a))
    else:
        tree=KDTree(len(rest[name]))
        for i,p in enumerate(rest[name]):
            tree.insert(p,i)
        tree.balance()
        indices=[]
        for p,row in zip(a,aligned):
            candidates=tree.find_range(p,3e-6)
            assert candidates,(name,'lost rest vertex',p.tolist())
            # Seam vertices share a position and UV but have different bone ownership.
            selected=min(candidates,key=lambda item:np.max(np.abs(old_weights[item[1]]-row)))
            indices.append(selected[1])
        indices=np.array(indices)
    mapping[name]=indices
    error=float(np.max(np.abs(a-rest[name][indices])))
    skin_error=float(np.max(np.abs(aligned-old_weights[indices])))
    assert error<3e-6 and skin_error<3e-5,(name,'rest/weights',error,skin_error)
    report['maxRestError']=max(report['maxRestError'],error)
    report['maxSkinError']=max(report['maxSkinError'],skin_error)
    uv=np.array([l.uv[:] for l in obj.data.uv_layers.active.data])
    assert uv.shape==uvs[name].shape
    uv_error=float(np.max(np.abs(uv-uvs[name])))
    assert uv_error<2e-6,(name,'UV error',uv_error)
    report['maxUvError']=max(report['maxUvError'],uv_error)
    keys={k.name:np.array([p.co[:] for p in k.data]) for k in obj.data.shape_keys.key_blocks} if obj.data.shape_keys else {}
    assert set(keys)==set(morphs[name]),(name,'lost fitting morph')
    for key in keys:
        assert np.max(np.abs(keys[key]-morphs[name][key][indices]))<3e-6,(name,key,'fitting morph changed')
    assert albedos(obj)==colors[name],(name,'albedo changed')
shared.data.pose_position='POSE'
for (clip,frame),snapshot in samples.items():
    actions=[a for a in bpy.data.actions if a.name.endswith(clip) and any('pose.bones' in fc.data_path for fc in a.fcurves)]
    assert len(actions)==1,(clip,[a.name for a in actions])
    shared.animation_data.action=actions[0]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    errors={}
    for name,obj in imported.items():
        error=float(np.linalg.norm(coords(obj)-snapshot[name][mapping[name]],axis=1).max())
        assert error<3e-4,(clip,frame,name,'FBX motion changed',error)
        errors[name]=error
    report['samples'].append({'clip':clip,'frame':frame,'maxError':max(errors.values()),'parts':errors})
    report['maxMotionError']=max(report['maxMotionError'],max(errors.values()))
report['complete']=True
np.savez_compressed(ROOT/'roundtrip-mapping.npz',**mapping)
(ROOT/'roundtrip.json').write_text(json.dumps(report,indent=2)+'\n')
print('ROUNDTRIP OK',FAMILY,len(report['files']),'FBXs,',len(samples),'poses, max error',report['maxMotionError'],flush=True)
