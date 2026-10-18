"""End-to-end import in an existing scene, including FPS, style drivers and FK playback."""
import bpy
import hashlib
import json
import math
import os
import runpy
import shutil
from pathlib import Path
import numpy as np

HERE=Path(__file__).resolve().parent
WORK=HERE.parents[2]/'content/work/rigging'
FAMILIES=('twig','sprig','bloom','drop')
SHA=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()


def coords(obj):
    ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
    a=np.empty(len(me.vertices)*3,dtype=np.float32);me.vertices.foreach_get('co',a);a=a.reshape(-1,3)
    m=np.array(ev.matrix_world);a=a@m[:3,:3].T+m[:3,3];ev.to_mesh_clear();return a


reference={}
for family in FAMILIES:
    bpy.ops.wm.open_mainfile(filepath=str(WORK/family/'rig-cache.blend'))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    meshes={o['part_key']:o for o in bpy.context.scene.objects if o.type=='MESH'}
    rig.data.pose_position='POSE'
    reference[family]={}
    for clip in ('Wave','Walk'):
        action=bpy.data.actions[family.title()+'_'+clip];rig.animation_data.action=action
        frame=1+(action.frame_range[1]-1)*.23
        bpy.context.scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update()
        reference[family][clip]={'frame':float(frame),'positions':{key:coords(o)for key,o in meshes.items()}}

folder=WORK/'import-test';folder.mkdir(parents=True,exist_ok=True)
for family in FAMILIES:
    inventory=json.loads((WORK/family/'handoff/inventory.json').read_text())
    for entry in inventory['files']:
        source=WORK/family/'handoff'/entry['path'];target=folder/family/entry['path'];target.parent.mkdir(parents=True,exist_ok=True)
        if target.exists():target.unlink()
        try:os.link(source,target)
        except OSError:shutil.copy2(source,target)
    (folder/family/'inventory.json').write_bytes((WORK/family/'handoff/inventory.json').read_bytes())
script=folder/'Import_Rigged_Heroes.py';script.write_bytes((HERE/script.name).read_bytes())
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.render.fps=30;scene.render.fps_base=1;scene.frame_start=7;scene.frame_end=133;scene.frame_set(23)
mesh=bpy.data.meshes.new('OwnerMesh');mesh.from_pydata([(0,0,0),(.1,0,0),(0,.1,0)],[],[(0,1,2)])
sentinel=bpy.data.objects.new('OwnerScene_DoNotModify',mesh);scene.collection.objects.link(sentinel);sentinel['owner_value']=42
owner_image=bpy.data.images.new('OwnerTexture_DoNotPack',width=16,height=16);owner_image.use_fake_user=True
owner_action=bpy.data.actions.new('TwigHeroV2Rig|Twig_Idle');owner_action.use_fake_user=True
owner_action['owner_value']=42
owner_curve=owner_action.fcurves.new('["owner_value"]')
owner_curve.keyframe_points.insert(9,.25);owner_curve.keyframe_points.insert(39,.75)
owner_action_keys=[tuple(point.co) for point in owner_curve.keyframe_points]
original_owner_vertices=[tuple(v.co)for v in mesh.vertices]
# A prior release must remain intact and must never satisfy the new import's reuse path.
legacy_collection=bpy.data.collections.new('Bloomlings_RigFix_v1')
scene.collection.children.link(legacy_collection)
legacy_data=bpy.data.armatures.new('PreviousRelease_RigData')
legacy_rig=bpy.data.objects.new('PreviousRelease_Twig',legacy_data)
legacy_collection.objects.link(legacy_rig)
legacy_rig['rig_fix_package']='Bloomlings_RigFix_v1';legacy_rig['rig_fix_family']='twig'
legacy_rig['owner_note']='Keep the prior release for comparison'
module=runpy.run_path(str(script))
rigs=module['RIGS'];collections=module['COLLECTIONS']
assert module['PACKAGE']=='Bloomlings_RigFix_v2'
assert len(rigs)==4
assert sum(1 for o in bpy.data.objects if o.type=='ARMATURE')==5
assert legacy_rig not in rigs.values() and legacy_rig.data==legacy_data
assert tuple(legacy_rig.users_collection)==(legacy_collection,)
assert legacy_rig['owner_note']=='Keep the prior release for comparison'
assert sum(1 for o in bpy.data.objects if o.type=='MESH' and o!=sentinel)==66
assert scene.render.fps==30 and scene.render.fps_base==1 and scene.frame_start==7 and scene.frame_end==133 and scene.frame_current==23
assert bpy.data.objects.get(sentinel.name)==sentinel and sentinel['owner_value']==42
assert [tuple(v.co)for v in sentinel.data.vertices]==original_owner_vertices
assert owner_image.packed_file is None
assert bpy.data.actions.get('TwigHeroV2Rig|Twig_Idle')==owner_action and owner_action['owner_value']==42
assert [tuple(point.co) for point in owner_curve.keyframe_points]==owner_action_keys
report={'complete':False,'package':module['PACKAGE'],'previousReleasePreserved':True,
        'blender':bpy.app.version_string,'rigs':4,'meshes':66,'importerSha256':SHA(script),'inventories':{},'styleChecks':0,'maxMotionError':0}


def update(rig):
    rig.update_tag();scene.frame_set(scene.frame_current);bpy.context.view_layer.update()


for family,rig in rigs.items():
    report['inventories'][family]=SHA(WORK/family/'handoff/inventory.json')
    assert rig['rig_fix_inventory_sha256']==report['inventories'][family]
    scene.bloomlings_rig_family=family
    parts={o.get('part_key'):o for o in rig.children_recursive if o.type=='MESH'}
    for prop,items in [('head_style',module['HEADS'][family]),('body_style',module['BODIES']),('scarf_style',module['SCARVES'][family])]:
        for index,item in enumerate(items):
            rig[prop]=index;update(rig)
            visible=[key for key in items if key!='none' and not parts[key].hide_render]
            assert visible==([] if item=='none' else [item]),(family,prop,index,visible)
            report['styleChecks']+=1
    for index in (0,1):
        rig['glasses']=index;update(rig)
        assert parts['Glasses'].hide_render==(index==0)
        report['styleChecks']+=1
    masked_materials={mat for obj in parts.values() for mat in obj.data.materials if mat and '_RigMask_' in mat.name}
    assert masked_materials,(family,'missing sleeve visibility masks')
    for index,body in enumerate(module['BODIES']):
        rig['body_style']=index;update(rig)
        for mat in masked_materials:
            bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
            alpha=bs.inputs['Alpha']
            expected=index not in (1,2) if '_RigMask_forearm_' in mat.name else index==0
            assert not alpha.is_linked and abs(alpha.default_value-float(expected))<1e-6,(family,body,mat.name,'skin mask')
        report['styleChecks']+=1
    for key,obj in parts.items():
        if key.startswith('Scarf') and obj.data.shape_keys:
            for index,body in enumerate(module['BODIES']):
                rig['body_style']=index;update(rig)
                for candidate in module['BODIES'][1:]:
                    k=obj.data.shape_keys.key_blocks.get('Over_'+candidate)
                    if k:assert abs(k.value-(candidate==body))<1e-6,(family,key,body,candidate,k.value)
    for key,obj in parts.items():
        if obj.animation_data:
            for fc in obj.animation_data.drivers:
                if fc.data_path in ('hide_viewport','hide_render'):fc.mute=True
        obj.hide_viewport=False;obj.hide_render=False
        if obj.data.shape_keys:
            if obj.data.shape_keys.animation_data:
                for fc in obj.data.shape_keys.animation_data.drivers:fc.mute=True
            for k in obj.data.shape_keys.key_blocks:k.value=0
    rig['live_ik']=0;update(rig)
    actions=json.loads(rig['review_actions'])
    assert len(actions)==6
    for clip,action_name in actions.items():
        action=bpy.data.actions[action_name]
        source=next(c for c in json.loads((WORK/family/'handoff/inventory.json').read_text())['clips']if c['name']==family.title()+'_'+clip)
        assert abs(float(action.frame_range[1])-(1+source['seconds']*30))<.002,(family,clip,'retiming')
    mapping=np.load(WORK/family/'roundtrip-mapping.npz')
    for clip,sample in reference[family].items():
        action=bpy.data.actions[actions[clip]];rig.animation_data.action=action
        if hasattr(action,'slots')and len(action.slots):rig.animation_data.action_slot=action.slots[0]
        frame=1+(sample['frame']-1)*30/24
        scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update()
        for key,obj in parts.items():
            source_name=family.title()+'HeroV2_'+key
            target=sample['positions'][key][mapping[source_name]]
            error=float(np.linalg.norm(coords(obj)-target,axis=1).max())
            assert error<3e-4,(family,clip,key,'owner import motion',error)
            report['maxMotionError']=max(report['maxMotionError'],error)
objects_before=set(bpy.data.objects);actions_before=set(bpy.data.actions);images_before=set(bpy.data.images)
runpy.run_path(str(script))
assert set(bpy.data.objects)==objects_before and set(bpy.data.actions)==actions_before and set(bpy.data.images)==images_before,'second import duplicated data'
assert bpy.data.objects.get(sentinel.name)==sentinel and owner_image.packed_file is None
assert bpy.data.actions.get('TwigHeroV2Rig|Twig_Idle')==owner_action and owner_action['owner_value']==42
assert [tuple(point.co) for point in owner_curve.keyframe_points]==owner_action_keys
assert bpy.data.objects.get(legacy_rig.name)==legacy_rig and legacy_rig.data==legacy_data
assert tuple(legacy_rig.users_collection)==(legacy_collection,)
# An edited inventory must fail explicitly rather than silently reusing stale v2 objects.
probe=folder/'twig/inventory.json';original_inventory=probe.read_bytes()
try:
    probe.write_bytes(original_inventory+b'\n')
    try:
        runpy.run_path(str(script))
    except RuntimeError as error:
        assert 'different rig revision' in str(error),str(error)
    else:
        raise AssertionError('Changed inventory silently reused an older rig')
finally:
    probe.write_bytes(original_inventory)
assert set(bpy.data.objects)==objects_before and set(bpy.data.actions)==actions_before and set(bpy.data.images)==images_before
report['staleRevisionRejected']=True
report['ownerActionsPreserved']=True
report['complete']=True
(WORK/'owner-import-check.json').write_text(json.dumps(report,indent=2)+'\n')
print('OWNER IMPORT OK',report['styleChecks'],'style states, existing scene preserved, max motion error',report['maxMotionError'],flush=True)
