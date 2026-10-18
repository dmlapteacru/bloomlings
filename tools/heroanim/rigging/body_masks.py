"""Postprocess a completed rig with modular clothing skin masks, without changing geometry."""
import bpy
import hashlib
import json
import sys
from pathlib import Path
import numpy as np

HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
from skinning import canonical
FAMILY=sys.argv[sys.argv.index('--')+1]
ROOT=HERE.parents[2]/'content/work/rigging'/FAMILY
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'rig-cache.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
arm.animation_data.action=None;arm.data.pose_position='REST';arm['body_style']=0
counts={'torso':0,'upper':0,'forearm':0}
for obj in list(bpy.context.scene.objects):
    if obj.type!='MESH' or not (obj.get('part_key') in ('base','Base_Body') or obj.get('part_key','').startswith('Base_Arm_')):
        continue
    names={g.index:g.name for g in obj.vertex_groups}
    points=canonical(np.array([obj.matrix_world@v.co for v in obj.data.vertices]),FAMILY)
    arm_weight=np.array([sum(g.weight for g in v.groups if names[g.group].startswith(('UpperArm.','Forearm.','Hand.','ShoulderBlend.','ElbowBlend.'))) for v in obj.data.vertices])
    hand_weight=np.array([sum(g.weight for g in v.groups if names[g.group].startswith('Hand.')) for v in obj.data.vertices])
    torso_weight=np.array([sum(g.weight for g in v.groups if names[g.group] in ('Hips','Spine','Chest')) for v in obj.data.vertices])
    shoulder=np.array([.085,.32]) if FAMILY=='drop' else np.array([.1752,.3245])
    wrist=np.array([.224,.178]) if FAMILY=='drop' else np.array([.2726,.2077])
    axis=wrist-shoulder
    t=((np.abs(points[:,0])-shoulder[0])*axis[0]+(points[:,2]-shoulder[1])*axis[1])/np.dot(axis,axis)
    materials={}
    for polygon in obj.data.polygons:
        existing=obj.data.materials[polygon.material_index]
        if existing and '_RigMask_' in existing.name:
            counts[existing.name.split('_RigMask_')[1].split('_')[0]]+=1
            continue
        ids=list(polygon.vertices)
        if hand_weight[ids].mean()>.60 or points[ids,2].max()>.40:
            continue
        if torso_weight[ids].mean()>.55 and points[ids,2].mean()>.105 and points[ids,2].max()<.375:
            region='torso'
        else:
            at=float(t[ids].mean())
            # Keep skin beyond the short / long cuff and every hand.
            if arm_weight[ids].mean()<.30 or at>.78:
                continue
            region='upper' if at<.38 else 'forearm'
        original=polygon.material_index
        key=(original,region)
        if key not in materials:
            src=obj.data.materials[original]
            mat=src.copy();mat.name=FAMILY+'_RigMask_'+region+'_'+src.name
            mat['rig_mask_region']=region
            if hasattr(mat,'surface_render_method'):mat.surface_render_method='DITHERED'
            elif hasattr(mat,'blend_method'):mat.blend_method='HASHED'
            bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
            socket=bs.inputs['Alpha']
            for link in list(socket.links):mat.node_tree.links.remove(link)
            socket.default_value=1
            d=socket.driver_add('default_value').driver;v=d.variables.new();v.name='style';v.type='SINGLE_PROP';v.targets[0].id=arm;v.targets[0].data_path='["body_style"]'
            d.expression='0 if style == 1 or style == 2 else 1' if region=='forearm' else '1 if style == 0 else 0'
            obj.data.materials.append(mat);materials[key]=len(obj.data.materials)-1
        polygon.material_index=materials[key];counts[region]+=1
assert counts['upper']>0 and counts['forearm']>0,(FAMILY,'missing body masks',counts)
arm.data.pose_position='POSE'
for p in arm.pose.bones:
    p.location=(0,0,0);p.rotation_quaternion=(1,0,0,0);p.scale=(1,1,1)
bpy.context.scene.frame_set(1)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'after.glb'),export_format='GLB',export_animations=True,export_animation_mode='ACTIONS',export_force_sampling=True,export_anim_single_armature=True,export_morph_animation=False)
arm.data.pose_position='REST'
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'rig-cache.blend'))
report=json.loads((ROOT/'rig-report.json').read_text())
report['bodyMasks']={'scriptSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'faces':counts,'defaultBodyStyle':0}
report['tools']['body_masks.py']=report['bodyMasks']['scriptSha256']
(ROOT/'rig-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('BODY MASKS',FAMILY,counts,flush=True)
