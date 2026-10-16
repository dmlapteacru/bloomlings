"""Owner's Tripo Sprig experiment: custom weights, compact rig, four original clips.

blender -b --factory-startup -t 4 --python tools/heroanim/plant-rig-study.py -- \
  --source /path/to/owner.fbx --out content/work/plant-rig --mode build
Then use --mode stills or --mode render with the same --out. Never changes runtime art.
Coordinates/weights are fitted to this owner's particular plant model, not an auto-rigger.
"""
import argparse
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector

FPS = 24
CX = -.0175
CLIPS = {'Idle': 3.2, 'Wave': 3.2, 'Walk': 1.6, 'Joy': 3.2}


def smooth(a, b, x):
    v = max(0., min(1., (x-a)/(b-a)))
    return v*v*(3-2*v)


def ease_event(t, start, end, fade=.35):
    return smooth(start, start+fade, t)*(1-smooth(end-fade, end, t))


def aim(obj, point):
    obj.rotation_euler = (Vector(point)-obj.location).to_track_quat('-Z', 'Y').to_euler()


def build(args):
    out = args.out
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(args.source))
    imported = list(bpy.context.scene.objects)
    assert not any(o.type == 'ARMATURE' for o in imported), 'Study expects the unrigged owner FBX'
    meshes = [o for o in imported if o.type == 'MESH']
    assert len(meshes) == 1, 'This fitted recipe expects exactly one mesh'
    mesh = meshes[0]
    mesh.name = 'SprigPlant'
    original_vertices = len(mesh.data.vertices)
    original_faces = len(mesh.data.polygons)
    uv_before = np.array([tuple(loop.uv) for loop in mesh.data.uv_layers.active.data])
    for img in bpy.data.images:
        if img.source == 'FILE':
            candidates = list(args.source.parent.rglob(Path(img.filepath.replace('\\', '/')).name))
            assert candidates, f'Missing texture: {img.name}'
            img.filepath = str(candidates[0])
            img.reload()
    coords = [mesh.matrix_world@v.co for v in mesh.data.vertices]
    lo = Vector([min(v[i] for v in coords) for i in range(3)])
    hi = Vector([max(v[i] for v in coords) for i in range(3)])
    norm = Matrix.Scale(2/(hi.z-lo.z), 4) @ Matrix.Translation(
        Vector((-(lo.x+hi.x)/2, -(lo.y+hi.y)/2, -lo.z)))
    mesh.matrix_world = norm@mesh.matrix_world
    bpy.context.view_layer.objects.active = mesh
    mesh.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    mesh.data.update()
    reference = np.array([tuple(v.co) for v in mesh.data.vertices])
    for poly in mesh.data.polygons:
        poly.use_smooth = True

    data = bpy.data.armatures.new('SprigStudySkeleton')
    arm = bpy.data.objects.new('SprigStudyRig', data)
    bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    mesh.select_set(False)
    arm.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')

    def bone(name, head, tail, parent=None, deform=True):
        b = data.edit_bones.new(name)
        b.head, b.tail = head, tail
        b.use_deform = deform
        if parent:
            b.parent = data.edit_bones[parent]
        return b

    bone('Root', (CX, 0, 0), (CX, 0, .12), deform=False)
    bone('Hips', (CX, .025, .29), (CX, .025, .38), 'Root')
    bone('Spine', (CX, .025, .38), (CX, .025, .48), 'Hips')
    bone('Chest', (CX, .025, .48), (CX, .025, .62), 'Spine')
    bone('Head', (CX, .025, .64), (CX, .025, 1.27), 'Chest')
    bone('Crown', (CX, .025, 1.30), (CX, .025, 1.90), 'Head')
    for side, sign in [('L', 1), ('R', -1)]:
        x = lambda distance: CX+sign*distance
        bone('UpperArm.'+side, (x(.27), -.015, .55), (x(.455), -.015, .55), 'Chest')
        bone('Forearm.'+side, (x(.455), -.015, .55), (x(.61), -.015, .55), 'UpperArm.'+side)
        bone('Hand.'+side, (x(.61), -.015, .55), (x(.72), -.015, .55), 'Forearm.'+side)
        bone('Thigh.'+side, (x(.17), .025, .29), (x(.17), -.015, .17), 'Hips')
        bone('Shin.'+side, (x(.17), -.015, .17), (x(.17), .0, .075), 'Thigh.'+side)
        bone('Foot.'+side, (x(.17), .0, .075), (x(.17), -.17, .075), 'Shin.'+side)
        bone('FootIK.'+side, (x(.17), .0, .075), (x(.17), -.17, .075), 'Root', False)
        bone('KneePole.'+side, (x(.17), -.6, .17), (x(.17), -.6, .27), 'Root', False)
    bone('socket_hat', (CX, .02, 1.4), (CX, .02, 1.5), 'Head', False)
    bone('socket_glasses', (CX, -.37, 1.1), (CX, -.37, 1.2), 'Head', False)
    bone('socket_back', (CX, .18, .44), (CX, .18, .54), 'Chest', False)
    bpy.ops.object.mode_set(mode='OBJECT')
    arm.show_in_front = True
    data.display_type = 'STICK'

    # Spatial skin fitted to the separated limbs. Rigid face protection is deliberate:
    # human bone-heat envelopes readily attach a short sprite's cheeks to its arms.
    groups = {b.name: mesh.vertex_groups.new(name=b.name) for b in data.bones if b.use_deform}
    adjacency = [[] for _ in mesh.data.vertices]
    for edge in mesh.data.edges:
        a, b = edge.vertices
        adjacency[a].append(b)
        adjacency[b].append(a)
    # The thumbs overlap the cheek leaves in height. Select each actual connected
    # arm outside the torso before blending shoulders, rather than cutting at a Z plane.
    arm_vertices = set()
    seeds = [v.index for v in mesh.data.vertices if abs(v.co.x-CX) > .6 and .46 < v.co.z < .63]
    stack = list(seeds)
    arm_vertices.update(seeds)
    while stack:
        for n in adjacency[stack.pop()]:
            if n not in arm_vertices and abs(mesh.data.vertices[n].co.x-CX) > .28:
                arm_vertices.add(n)
                stack.append(n)
    assert all(mesh.data.vertices[i].co.z < .72 for i in arm_vertices), 'Arm flood crossed into head'
    # Extend just into the soft shoulder transition; keep the front chest collar out.
    stack = list(arm_vertices)
    while stack:
        for n in adjacency[stack.pop()]:
            v = mesh.data.vertices[n]
            if n not in arm_vertices and .245 < abs(v.co.x-CX) < .30 and .40 < v.co.z < .65 and v.co.y > -.27:
                arm_vertices.add(n)
                stack.append(n)
    weights = []
    for v in mesh.data.vertices:
        x, y, z = v.co
        ax = abs(x-CX)
        side = 'L' if x >= CX else 'R'
        w = {}

        def add(name, value):
            if value > 1e-7:
                w[name] = w.get(name, 0.)+value

        arm_share = smooth(.25, .365, ax) if v.index in arm_vertices else 0.
        arm_share *= smooth(-.25,-.12,y)
        if arm_share:
            elbow = smooth(.41, .50, ax)
            wrist = smooth(.565, .625, ax)
            add('UpperArm.'+side, arm_share*(1-elbow))
            add('Forearm.'+side, arm_share*elbow*(1-wrist))
            add('Hand.'+side, arm_share*elbow*wrist)
        torso_share = 1-arm_share
        head = smooth(.59, .675, z)
        crown = smooth(1.38, 1.68, z)
        add('Head', torso_share*head*(1-crown))
        add('Crown', torso_share*head*crown)
        body = torso_share*(1-head)
        leg = (1-smooth(.19, .32, z))*smooth(.02, .09, ax)
        foot = 1-smooth(.065, .14, z)
        thigh = smooth(.135, .23, z)
        add('Foot.'+side, body*leg*foot)
        add('Shin.'+side, body*leg*(1-foot)*(1-thigh))
        add('Thigh.'+side, body*leg*(1-foot)*thigh)
        core = body*(1-leg)
        chest = smooth(.38, .53, z)
        spine = smooth(.29, .42, z)
        add('Chest', core*chest)
        add('Spine', core*(1-chest)*spine)
        add('Hips', core*(1-chest)*(1-spine))
        # GLB/Unity compatibility: at most four nonzero weights, normalized per vertex.
        w = dict(sorted(w.items(), key=lambda item: -item[1])[:4])
        total = sum(w.values())
        assert total > 0
        w = {k: v/total for k, v in w.items()}
        weights.append(w)
    # Continuous spatial blends are retained; smoothing across this irregular AI mesh
    # makes the narrow neck and the short legs less predictable, not more consistent.
    for i,w in enumerate(weights):
        for name,value in w.items(): groups[name].add([i],value,'REPLACE')
    modifier = mesh.modifiers.new('Study skin', 'ARMATURE')
    modifier.object = arm
    # Linear blend skin is used in both preview and GLB, avoiding a Blender-only DQ look.
    modifier.use_deform_preserve_volume = False
    mesh.parent = arm
    assert np.array_equal(uv_before, np.array([tuple(l.uv) for l in mesh.data.uv_layers.active.data]))
    for side in ('L', 'R'):
        ik = arm.pose.bones['Shin.'+side].constraints.new('IK')
        ik.name = 'Grounded two bone leg'
        ik.target = arm
        ik.subtarget = 'FootIK.'+side
        ik.chain_count = 2
        ik.use_stretch = False
        # A slightly forward rest knee supplies a stable bend plane without pole flips.
        keep = arm.pose.bones['Foot.'+side].constraints.new('COPY_ROTATION')
        keep.target = arm
        keep.subtarget = 'FootIK.'+side
        keep.owner_space = 'POSE'
        keep.target_space = 'POSE'

    scene = bpy.context.scene
    scene.render.fps = FPS
    local_axes = {b.name: [b.bone.matrix_local.to_quaternion().inverted()@Vector(axis)
                          for axis in ((1,0,0), (0,1,0), (0,0,1))] for b in arm.pose.bones}

    def turn(name, axis, degrees):
        pb = arm.pose.bones[name]
        pb.rotation_quaternion = Quaternion(local_axes[name][axis], math.radians(degrees))@pb.rotation_quaternion

    def move(name, delta):
        arm.pose.bones[name].location += data.bones[name].matrix_local.to_quaternion().inverted()@Vector(delta)

    def reset():
        for pb in arm.pose.bones:
            pb.rotation_mode = 'QUATERNION'
            pb.location = (0, 0, 0)
            pb.rotation_quaternion = (1, 0, 0, 0)
            pb.scale = (1, 1, 1)

    def idle_pose(u):
        turn('UpperArm.L', 1, 55)
        turn('UpperArm.R', 1, -55)
        turn('Forearm.L', 1, 5)
        turn('Forearm.R', 1, -5)
        breath = math.sin(math.tau*u)
        move('Chest', (0, 0, .004*breath))
        turn('Head', 1, 1.3*breath)
        turn('Crown', 1, -.9*breath)

    arm.animation_data_create()
    actions = []
    for clip, seconds in CLIPS.items():
        action = bpy.data.actions.new('SprigStudy_'+clip)
        arm.animation_data.action = action
        count = round(seconds*FPS)
        for frame in range(1, count+2):
            scene.frame_set(frame)
            reset()
            u = (frame-1)/count
            t = u*seconds
            idle_pose(u)
            if clip == 'Wave':
                e = ease_event(t, .12, seconds-.12, .5)
                turn('UpperArm.R', 1, 60*e)
                turn('UpperArm.R', 2, 17*e)
                turn('Forearm.R', 1, 62*e)
                turn('Hand.R', 0, 17*math.sin(math.tau*3*u)*e)
                turn('Hand.R', 2, 8*math.sin(math.tau*3*u)*e)
                turn('Head', 2, -7*e)
                turn('Head', 1, -3*e)
            elif clip == 'Walk':
                phase = math.tau*u
                move('Hips', (.006*math.sin(phase), 0, -.004+.003*math.cos(2*phase)))
                turn('Chest', 2, 3*math.sin(phase))
                turn('Head', 2, -3*math.sin(phase))
                for side, offset, sign in [('L',0,1), ('R',math.pi,-1)]:
                    p = phase+offset
                    lift = .025*max(0, -math.sin(p))**1.5
                    move('FootIK.'+side, (0, -.04*math.cos(p), lift))
                    turn('FootIK.'+side, 0, -6*max(0, -math.sin(p)))
                    turn('UpperArm.'+side, 0, 20*sign*math.sin(phase))
                    turn('Forearm.'+side, 0, 5*sign*math.sin(phase))
                turn('Crown', 0, 1.4*math.sin(phase-.4))
            elif clip == 'Joy':
                hop = 0.
                for start, end in ((.55,1.12),(1.55,2.12)):
                    if start <= t <= end:
                        h = (t-start)/(end-start)
                        hop = math.sin(math.pi*h)**2
                crouch = 0.
                for center in (.4,1.4,2.25):
                    crouch += .022*math.exp(-((t-center)/.14)**2)
                move('Root', (0, 0, .11*hop))
                move('Hips', (0, 0, -crouch))
                for side, sign in [('L',1), ('R',-1)]:
                    turn('UpperArm.'+side, 1, -sign*30*hop)
                    move('FootIK.'+side, (0, -.007*hop, .009*hop))
                turn('Head', 0, -3*hop)
                turn('Crown', 0, 4*hop)
            for pb in arm.pose.bones:
                pb.keyframe_insert('location', frame=frame, group=pb.name)
                pb.keyframe_insert('rotation_quaternion', frame=frame, group=pb.name)
                pb.keyframe_insert('scale', frame=frame, group=pb.name)
        for curve in action.fcurves:
            for point in curve.keyframe_points:
                point.interpolation = 'LINEAR'
        action.use_fake_user = True
        actions.append(action)
    arm.animation_data.action = None
    for action in actions:
        track = arm.animation_data.nla_tracks.new()
        track.name = action.name
        track.strips.new(action.name, 1, action)
        track.mute = True

    # Verify rest, weights, loop seam, and finite/stable deformations over every keyframe.
    reset()
    arm.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    eval_mesh = mesh.evaluated_get(deps).to_mesh()
    rest_error = float(np.max(np.linalg.norm(np.array([tuple(v.co) for v in eval_mesh.vertices])-reference, axis=1)))
    mesh.evaluated_get(deps).to_mesh_clear()
    arm.data.pose_position = 'POSE'
    edge_index = np.array([tuple(e.vertices) for e in mesh.data.edges])
    edge_length = np.linalg.norm(reference[edge_index[:,1]]-reference[edge_index[:,0]], axis=1)
    protected = np.array([i for i,(x,y,z) in enumerate(reference) if y < -.20 and .72 < z < 1.29])
    samples = []
    for action in actions:
        arm.animation_data.action = action
        first = last = None
        max_stretch = 0.
        max_p99 = 0.
        max_extension = 0.
        rigid_error = 0.
        lowest = 10.
        for frame in range(1, round(action.frame_range[1])+1):
            scene.frame_set(frame)
            bpy.context.view_layer.update()
            evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
            current = evaluated.to_mesh()
            xyz = np.array([tuple(v.co) for v in current.vertices])
            assert np.isfinite(xyz).all()
            deformed_length = np.linalg.norm(xyz[edge_index[:,1]]-xyz[edge_index[:,0]],axis=1)
            ratio = deformed_length/np.maximum(edge_length,1e-7)
            max_stretch = max(max_stretch,float(np.max(ratio)))
            max_p99 = max(max_p99,float(np.quantile(ratio,.99)))
            max_extension = max(max_extension,float(np.max(deformed_length-edge_length)))
            # Pairwise distances in the entire front face must be rigid under animation.
            face = xyz[protected]
            anchor = face[0]
            base_dist = np.linalg.norm(reference[protected]-reference[protected[0]],axis=1)
            rigid_error = max(rigid_error,float(np.max(abs(np.linalg.norm(face-anchor,axis=1)-base_dist))))
            lowest = min(lowest,float(np.min(xyz[:,2])))
            if first is None: first = xyz.copy()
            last = xyz.copy()
            evaluated.to_mesh_clear()
        seam = float(np.max(np.linalg.norm(last-first,axis=1)))
        samples.append(dict(clip=action.name,frames=round(action.frame_range[1]),loopSeamMaxError=seam,
                            maxEdgeStretch=max_stretch,p99EdgeStretch=max_p99,maxEdgeExtension=max_extension,
                            protectedFaceMaxError=rigid_error,minZ=lowest))
    assert rest_error < 1e-5
    assert all(s['loopSeamMaxError'] < .0001 for s in samples)
    assert all(s['protectedFaceMaxError'] < .0001 for s in samples)
    report = dict(source=args.source.name,sourceSha256=hashlib.sha256(args.source.read_bytes()).hexdigest(),
                  vertices=original_vertices,faces=original_faces,triangles=original_faces,
                  meshTopologyChanged=False,uvChanged=False,sourceTexturesChanged=False,
                  normalizedHeight=2,rigBones=len(data.bones),deformBones=len(groups),
                  connectedArmVertices=len(arm_vertices),maxInfluences=max(map(len,weights)),maxWeightSumError=max(abs(sum(w.values())-1) for w in weights),
                  unweightedVertices=sum(not w for w in weights),restMaxError=rest_error,
                  protectedFaceVertices=len(protected),fps=FPS,clips=samples,
                  note='Prototype fitted skin. Fingers follow rigid hand bones; face expression remains in the owner texture.')
    (out/'rig-report.json').write_text(json.dumps(report,indent=2)+'\n')
    # Relative ratios over tiny sliver triangles are noisy. Gate the visible extension
    # in units of a height-2 model as well as the 99th percentile of edge ratios.
    # This is a reviewed prototype envelope, not a final retopology quality certificate:
    # catch gross spikes (>5% of height) while recording the full local extrema for review.
    if any(s['maxEdgeExtension'] > .10 or s['p99EdgeStretch'] > 1.6 for s in samples):
        bpy.ops.wm.save_as_mainfile(filepath=str(out/'debug-weights.blend'),compress=True)
        print('SKIN DEBUG', json.dumps(report), flush=True)
        raise AssertionError('Skin has a locally stretched seam; inspect weights')

    for ob in list(bpy.context.selected_objects): ob.select_set(False)
    arm.select_set(True); mesh.select_set(True)
    bpy.context.view_layer.objects.active = arm
    reset()
    arm.animation_data.action = None
    for track in arm.animation_data.nla_tracks: track.mute = False
    scene.frame_start, scene.frame_end = 1, 78
    bpy.ops.export_scene.gltf(filepath=str(out/'sprig-plant-rigged.glb'),export_format='GLB',
        use_selection=True,export_animations=True,export_animation_mode='NLA_TRACKS',
        export_force_sampling=True,export_frame_range=False,export_skins=True,
        export_all_influences=False,export_influence_nb=4,export_image_format='AUTO')
    for track in arm.animation_data.nla_tracks: track.mute = True
    arm.animation_data.action = actions[0]
    scene.frame_set(1)
    studio(scene)
    for img in bpy.data.images:
        if img.source == 'FILE': img.pack()
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'sprig-plant-rigged.blend'),compress=True)
    print('PLANT STUDY',json.dumps(report))


def studio(scene):
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 32
    scene.cycles.use_denoising = False
    scene.render.resolution_x = 512
    scene.render.resolution_y = 576
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new('Neutral study studio')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.77,.79,.77,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value = .65
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    for name, loc, power, size in [('Key',(-3,-4,5),260,4),('Fill',(3,-2,3),160,3),('Rim',(0,3,4),180,3)]:
        light_data = bpy.data.lights.new(name,'AREA')
        light_data.energy,light_data.size = power,size
        light = bpy.data.objects.new(name,light_data)
        scene.collection.objects.link(light)
        light.location = loc
        aim(light,(0,0,1))
    camera_data = bpy.data.cameras.new('Review camera')
    camera = bpy.data.objects.new('Review camera',camera_data)
    scene.collection.objects.link(camera)
    camera_data.type = 'ORTHO'
    camera_data.ortho_scale = 2.65
    camera.location = (0,-5,1.08)
    aim(camera,(0,0,1.08))
    scene.camera = camera


def render(args):
    playback = args.out/'sprig-plant-playback.blend'
    assert playback.exists(), 'Run --mode roundtrip before rendering the exported GLB'
    bpy.ops.wm.open_mainfile(filepath=str(playback))
    scene = bpy.context.scene
    arm = bpy.data.objects['SprigStudyRig']
    if args.mode == 'stills':
        scene.render.resolution_x = scene.render.resolution_y = 768
        scene.cycles.samples = 64
        for name, clip, frame in [('idle','Idle',1),('wave','Wave',36),('walk','Walk',13),('joy','Joy',21)]:
            action = next(a for a in bpy.data.actions if a.name.startswith('SprigStudy_'+clip))
            arm.animation_data.action = action
            scene.frame_set(round(action.frame_range[0])+frame-1)
            scene.render.filepath = str(args.out/f'{name}.png')
            bpy.ops.render.render(write_still=True)
        # One profile wave exposes accidental face stretching/arm intersections.
        scene.camera.location = (4,-4,1.08)
        aim(scene.camera,(0,0,1.08))
        action = next(a for a in bpy.data.actions if a.name.startswith('SprigStudy_Wave'))
        arm.animation_data.action = action
        scene.frame_set(round(action.frame_range[0])+35)
        scene.render.filepath = str(args.out/'wave-three-quarter.png')
        bpy.ops.render.render(write_still=True)
    else:
        selected = [args.clip] if args.clip else list(CLIPS)
        scene.cycles.samples = args.samples
        for clip in selected:
            action = next(a for a in bpy.data.actions if a.name.startswith('SprigStudy_'+clip))
            arm.animation_data.action = action
            folder = args.out/'frames'/clip.lower()
            folder.mkdir(parents=True,exist_ok=True)
            # Last endpoint duplicates frame one for looping; omit it in the movie.
            start, end = map(round, action.frame_range)
            for frame in range(start, end):
                path = folder/f'{frame-start+1:04}.png'
                if path.exists(): continue
                scene.frame_set(frame)
                scene.render.filepath = str(path)
                bpy.ops.render.render(write_still=True)


def roundtrip(args):
    """Preview the real GLB and check its baked skin against the editable authoring file."""
    from mathutils.kdtree import KDTree
    bpy.ops.wm.open_mainfile(filepath=str(args.out/'sprig-plant-rigged.blend'))
    scene = bpy.context.scene
    arm,mesh = bpy.data.objects['SprigStudyRig'],bpy.data.objects['SprigPlant']
    expected = []
    for clip, middle in [('Idle',40),('Wave',36),('Walk',13),('Joy',21)]:
        action = bpy.data.actions['SprigStudy_'+clip]
        arm.animation_data.action = action
        for frame in [1,middle,round(action.frame_range[1])]:
            scene.frame_set(frame)
            ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
            m = ev.to_mesh()
            points = [ev.matrix_world@v.co for v in m.vertices]
            expected.append((clip,frame-1,points))
            ev.to_mesh_clear()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = FPS
    bpy.ops.import_scene.gltf(filepath=str(args.out/'sprig-plant-rigged.glb'))
    arm = next(o for o in scene.objects if o.type == 'ARMATURE')
    mesh = next(o for o in scene.objects if o.type == 'MESH')
    arm.name,mesh.name = 'SprigStudyRig','SprigPlant'
    for track in arm.animation_data.nla_tracks: track.mute = True
    clip_actions = {clip:next(a for a in bpy.data.actions if a.name.startswith('SprigStudy_'+clip)) for clip in CLIPS}
    results = []
    for clip, offset, points in expected:
        action = clip_actions[clip]
        arm.animation_data.action = action
        scene.frame_set(round(action.frame_range[0])+offset)
        ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        m = ev.to_mesh()
        tree = KDTree(len(points))
        for i,p in enumerate(points): tree.insert(p,i)
        tree.balance()
        error = max(tree.find(ev.matrix_world@v.co)[2] for v in m.vertices)
        assert len(m.vertices) >= len(points), 'Export lost source geometry'
        results.append(dict(clip=clip,offsetFrame=offset,maxVertexDistance=error))
        ev.to_mesh_clear()
    report = dict(glbSha256=hashlib.sha256((args.out/'sprig-plant-rigged.glb').read_bytes()).hexdigest(),
                  clips=list(clip_actions),bones=len(arm.data.bones),exportedVertices=len(mesh.data.vertices),
                  sourceComparisonSamples=results,maxVertexDistance=max(r['maxVertexDistance'] for r in results))
    (args.out/'roundtrip-report.json').write_text(json.dumps(report,indent=2)+'\n')
    assert report['maxVertexDistance'] < .0001, 'Exported animation differs from the authoring skin'
    studio(scene)
    arm.animation_data.action = clip_actions['Idle']
    scene.frame_set(round(clip_actions['Idle'].frame_range[0]))
    bpy.ops.wm.save_as_mainfile(filepath=str(args.out/'sprig-plant-playback.blend'),compress=True)
    print('ROUNDTRIP',json.dumps(report))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--source',type=Path)
    parser.add_argument('--out',type=Path,required=True)
    parser.add_argument('--mode',choices=['build','roundtrip','stills','render'],default='build')
    parser.add_argument('--clip',choices=list(CLIPS))
    parser.add_argument('--samples',type=int,default=32)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    args.out = args.out.resolve()
    args.out.mkdir(parents=True,exist_ok=True)
    if args.mode == 'build':
        assert args.source and args.source.exists()
        args.source = args.source.resolve()
        build(args)
    elif args.mode == 'roundtrip':
        roundtrip(args)
    else:
        render(args)
