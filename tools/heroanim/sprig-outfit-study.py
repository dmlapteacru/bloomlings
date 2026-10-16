"""Assemble the owner's modular Sprig FBXs without changing meshes, UVs or weights.

blender -b --factory-startup -t 4 --python-exit-code 1 --python \
  tools/heroanim/sprig-outfit-study.py -- --source-dir path/to/five-fbxs \
  --out content/work/sprig-outfit-study/build

CapOn is the OWNER'S existing head variant. The source animation FBX is audited,
not assumed to contain motion. Study_* actions are separate original test motions.
No runtime game assets are written.
"""
import argparse
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Quaternion, Vector

FILES = {'hero':'SprigHero.fbx','anims':'SprigHero@Anims.fbx',
         'cap':'SprigHero_Cap.fbx','glasses':'SprigHero_Glasses.fbx',
         'backpack':'SprigHero_Backpack.fbx'}
FPS = 24


def sha(file):
    return hashlib.sha256(Path(file).read_bytes()).hexdigest()


def skeleton(arm):
    return {b.name:(b.parent.name if b.parent else None, np.array(b.matrix_local))
            for b in arm.data.bones}


def curves_report(action):
    varying=[]
    max_delta=0.
    for f in action.fcurves:
        values=[float(k.co.y) for k in f.keyframe_points]
        delta=max(values)-min(values) if values else 0.
        max_delta=max(max_delta,delta)
        if delta>1e-6: varying.append(dict(path=f.data_path,index=f.array_index,range=delta))
    return dict(name=action.name,frames=list(action.frame_range),curves=len(action.fcurves),
                varyingCurves=len(varying),maxKeyValueRange=max_delta,variation=varying)


def signature(obj):
    coords=np.array([tuple(v.co) for v in obj.data.vertices],dtype=np.float32)
    uv=np.array([tuple(l.uv) for l in obj.data.uv_layers.active.data],dtype=np.float32) if obj.data.uv_layers else np.zeros((0,2),dtype=np.float32)
    weights=[[(obj.vertex_groups[g.group].name,round(g.weight,7)) for g in v.groups] for v in obj.data.vertices]
    return dict(vertices=len(coords),triangles=sum(len(p.vertices)-2 for p in obj.data.polygons),
                coordinateSha256=hashlib.sha256(coords.tobytes()).hexdigest(),
                uvSha256=hashlib.sha256(uv.tobytes()).hexdigest(),
                weightSha256=hashlib.sha256(json.dumps(weights).encode()).hexdigest(),
                unweighted=sum(not v.groups for v in obj.data.vertices),
                maxWeightSumError=max(abs(sum(g.weight for g in v.groups)-1) for v in obj.data.vertices))


def build(args):
    args.out.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    report={'version':1,'inputs':[],'skeletonChecks':[],'meshes':{},'productionAssetsChanged':False}
    master=None
    mesh_objects=[]
    imported_actions=[]
    for key, name in FILES.items():
        file=args.source_dir/name
        assert file.is_file(),f'Missing {file}'
        before=set(bpy.data.objects);actions_before=set(bpy.data.actions)
        bpy.ops.import_scene.fbx(filepath=str(file))
        objects=set(bpy.data.objects)-before
        arms=[o for o in objects if o.type=='ARMATURE']
        assert len(arms)==1,f'{name}: expected one rig'
        arm=arms[0]
        report['inputs'].append(dict(file=name,sha256=sha(file),bytes=file.stat().st_size))
        if master is None:
            master=arm;reference=skeleton(master)
        else:
            check=skeleton(arm)
            assert reference.keys()==check.keys(),f'{name}: bone names differ'
            error=max(float(np.max(abs(reference[n][1]-check[n][1]))) for n in reference)
            assert all(reference[n][0]==check[n][0] for n in reference)
            assert error<1e-5,f'{name}: bone rest axes differ: {error}'
            assert np.max(abs(np.array(master.matrix_world)-np.array(arm.matrix_world)))<1e-5
            report['skeletonChecks'].append(dict(file=name,maxRestMatrixError=error,bones=len(check)))
        for o in objects:
            if o.type=='MESH':
                sig=signature(o)
                assert sig['unweighted']==0 and sig['maxWeightSumError']<1e-4
                world=o.matrix_world.copy();o.parent=master;o.matrix_world=world
                for m in o.modifiers:
                    if m.type=='ARMATURE':m.object=master
                assert signature(o)==sig,'Assembly changed source geometry/UV/weights'
                report['meshes'][o.name]=sig
                mesh_objects.append(o)
        for action in set(bpy.data.actions)-actions_before:
            imported_actions.append(action)
        if arm!=master:bpy.data.objects.remove(arm,do_unlink=True)
    report['sourceAnimations']=[curves_report(a) for a in imported_actions]
    for a in imported_actions:
        a.name='Source_'+a.name.split('SprigHero_')[-1].replace('.001','')
        a.use_fake_user=True
    hero=bpy.data.objects['SprigHero']
    assert hero.data.shape_keys and 'CapOn' in hero.data.shape_keys.key_blocks
    basis=np.array([tuple(v.co) for v in hero.data.shape_keys.key_blocks['Basis'].data])
    capped=np.array([tuple(v.co) for v in hero.data.shape_keys.key_blocks['CapOn'].data])
    delta=np.linalg.norm(capped-basis,axis=1)
    assert np.max(delta)>0.1,'CapOn has no useful shape change'
    report['headVariant']=dict(source='Owner supplied CapOn shape key',changedVertices=int(np.sum(delta>1e-5)),
                               maxDisplacement=float(np.max(delta)),binarySwitch=True)
    controls=bpy.data.objects.new('OutfitControls',None)
    bpy.context.collection.objects.link(controls)
    controls.empty_display_type='PLAIN_AXES';controls.empty_display_size=.12
    controls.location=(1.3,0,0)
    for prop,description in [('cap_head','Кепка + подготовленная голова CapOn'),('glasses','Очки'),('backpack','Рюкзак')]:
        controls[prop]=True
        controls.id_properties_ui(prop).update(description=description)
    def driver(target,property,control,invert=False):
        f=target.driver_add(property);d=f.driver;d.type='SCRIPTED'
        v=d.variables.new();v.name='enabled';v.type='SINGLE_PROP'
        v.targets[0].id=controls;v.targets[0].data_path='["'+control+'"]'
        d.expression='not enabled' if invert else 'enabled'
    driver(hero.data.shape_keys.key_blocks['CapOn'],'value','cap_head')
    for name,prop in [('SprigHero_Cap','cap_head'),('SprigHero_Glasses','glasses'),('SprigHero_Backpack','backpack')]:
        o=bpy.data.objects[name]
        driver(o,'hide_render',prop,True);driver(o,'hide_viewport',prop,True)

    # New motions use source rest axes/weights; source static actions are not relabeled as moving.
    master.animation_data_create()
    axes={b.name:[b.bone.matrix_local.to_quaternion().inverted()@Vector(a) for a in ((1,0,0),(0,1,0),(0,0,1))] for b in master.pose.bones}
    def reset():
        for b in master.pose.bones:
            b.rotation_mode='QUATERNION';b.location=(0,0,0);b.rotation_quaternion=(1,0,0,0);b.scale=(1,1,1)
    def turn(name,axis,degrees):
        b=master.pose.bones[name];b.rotation_quaternion=Quaternion(axes[name][axis],math.radians(degrees))@b.rotation_quaternion
    def move(name,delta):
        master.pose.bones[name].location+=master.data.bones[name].matrix_local.to_quaternion().inverted()@Vector(delta)
    scene=bpy.context.scene;scene.render.fps=FPS
    studies=[]
    for clip,seconds in [('Idle',3),('Look',3),('Step',2),('Joy',3)]:
        action=bpy.data.actions.new('Study_'+clip);action.use_fake_user=True
        master.animation_data.action=action
        count=round(seconds*FPS)
        for frame in range(1,count+2):
            scene.frame_set(frame);reset()
            u=(frame-1)/count;t=u*seconds;phase=math.tau*u
            move('Chest',(0,0,.003*math.sin(phase)))
            turn('Head',1,1.0*math.sin(phase))
            if clip=='Look':
                turn('Head',2,16*math.sin(phase));turn('Head',0,3*(1-math.cos(phase))/2)
                turn('Chest',2,-2*math.sin(phase))
            elif clip=='Step':
                move('Root',(0,0,.012*(1-math.cos(2*phase))))
                turn('Chest',2,2*math.sin(phase))
                for side,offset,sign in [('L',0,1),('R',math.pi,-1)]:
                    p=phase+offset
                    turn('Thigh.'+side,0,9*math.sin(p))
                    turn('Shin.'+side,0,-7*max(0,math.sin(p)))
                    turn('Foot.'+side,0,-3*math.sin(p))
                    turn('UpperArm.'+side,0,12*sign*math.sin(phase))
            elif clip=='Joy':
                hop=sum(math.sin(math.pi*(t-a)/(b-a))**2 if a<t<b else 0 for a,b in ((.45,1.05),(1.5,2.1)))
                move('Root',(0,0,.10*hop));turn('Head',0,-3*hop)
                for side,sign in [('L',1),('R',-1)]:turn('UpperArm.'+side,1,-sign*15*hop)
            for b in master.pose.bones:
                for prop in ['location','rotation_quaternion','scale']:b.keyframe_insert(prop,frame=frame,group=b.name)
        for f in action.fcurves:
            for k in f.keyframe_points:k.interpolation='LINEAR'
        assert curves_report(action)['varyingCurves']>0
        studies.append(action)
    report['studyAnimations']=[curves_report(a) for a in studies]
    report['textures']=[dict(name=i.name,packed=bool(i.packed_file),width=i.size[0],height=i.size[1]) for i in bpy.data.images if i.type=='IMAGE']
    assert all(i['packed'] for i in report['textures'])

    # Actual mesh evaluations, both cap states, at three poses per new action.
    validations=[]
    for action in studies:
        master.animation_data.action=action
        poses=[]
        for hat in (False,True):
            controls['cap_head']=hat;controls.update_tag()
            for frame in [1,round(action.frame_range[1]/3),round(action.frame_range[1]*2/3)]:
                scene.frame_set(frame);bpy.context.view_layer.update()
                deps=bpy.context.evaluated_depsgraph_get()
                row=dict(cap=hat,frame=frame)
                for o in mesh_objects:
                    ev=o.evaluated_get(deps);me=ev.to_mesh()
                    coords=np.array([tuple(v.co) for v in me.vertices])
                    assert np.isfinite(coords).all()
                    row[o.name]=dict(min=coords.min(0).tolist(),max=coords.max(0).tolist())
                    ev.to_mesh_clear()
                poses.append(row)
        validations.append(dict(clip=action.name,poses=poses))
    report['evaluatedPoses']=validations
    controls['cap_head']=True;controls.update_tag()
    master.animation_data.action=studies[0];scene.frame_start=1;scene.frame_end=73;scene.frame_set(1)
    bpy.context.view_layer.update()
    for o in bpy.context.scene.objects:o.select_set(False)
    for o in [master]+mesh_objects:o.select_set(True)
    bpy.context.view_layer.objects.active=master
    glb=args.out/'sprig-outfit.glb'
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,
        export_animations=True,export_animation_mode='ACTIONS',export_force_sampling=True,
        export_frame_range=False,export_skins=True,export_all_influences=False,export_influence_nb=4,
        export_morph=True,export_extras=True)
    report['glbSha256']=sha(glb)
    for o in mesh_objects:assert signature(o)==report['meshes'][o.name]
    text=bpy.data.texts.new('READ_ME')
    text.write('Sprig outfit study. Select OutfitControls, Object Properties > Custom Properties.\n'
               'cap_head toggles the supplied CapOn head variant and cap together.\n'
               'glasses/backpack toggle accessories. Study_* are new test motions.\n'
               'Source_* preserves the supplied constant FBX clips.\n'
               'Use Dope Sheet > Action Editor on SprigHeroRig to select an action.\n')
    for o in bpy.context.scene.objects:o.select_set(False)
    controls.select_set(True);bpy.context.view_layer.objects.active=controls
    from mathutils import Quaternion as Q
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                area.spaces.active.region_3d.view_distance=3.5
                area.spaces.active.region_3d.view_location=(.043,0,1.)
                area.spaces.active.region_3d.view_rotation=Q((1,0,0),math.pi/2)
                area.spaces.active.shading.type='MATERIAL'
    bpy.ops.wm.save_as_mainfile(filepath=str(args.out/'sprig-outfit.blend'),compress=True)
    (args.out/'build-report.json').write_text(json.dumps(report,indent=2)+'\n')
    print('OUTFIT BUILD',json.dumps(dict(meshes=len(mesh_objects),sourceMotion=[(a['name'],a['varyingCurves']) for a in report['sourceAnimations']],glbSha256=report['glbSha256'])))


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--source-dir',type=Path,required=True)
    parser.add_argument('--out',type=Path,required=True)
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    build(args)
