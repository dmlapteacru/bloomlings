"""Offline Bloom texture projection study. Keeps the owner's mesh, UVs, rig and clips.
Prepare an orthographic albedo reference, then bake a generated cleanup back onto the original UVs.
blender -b --factory-startup --python tools/heroanim/bloom-projection.py -- prepare
"""
import bpy, math, sys, json
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
HERE=Path(__file__).resolve().parent
ROOT=HERE.parent.parent
OUT=ROOT/'content/work/bloom-texture/projection'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(HERE/'models/heroes.glb'))
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE' and 'Bloom' in o.name)
mesh=next(o for o in bpy.data.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers))
arm.animation_data.action=next(a for a in bpy.data.actions if 'Bloom_Breathing' in a.name)
scene=bpy.context.scene
scene.frame_set(1)
for o in list(bpy.data.objects):
    if o not in (arm,mesh): bpy.data.objects.remove(o,do_unlink=True)
scene.render.engine='CYCLES';scene.cycles.samples=1;scene.cycles.use_denoising=False
scene.render.resolution_x=scene.render.resolution_y=1024
scene.render.resolution_percentage=100
scene.render.film_transparent=True
scene.view_settings.view_transform='Standard'
scene.view_settings.look='None'
scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
deps=bpy.context.evaluated_depsgraph_get();evaluated=mesh.evaluated_get(deps);posed=evaluated.to_mesh()
points=[evaluated.matrix_world@v.co for v in posed.vertices]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
center=(lo+hi)/2
camdata=bpy.data.cameras.new('Bloom_albedo_front');cam=bpy.data.objects.new('Bloom_albedo_front',camdata)
scene.collection.objects.link(cam);cam.location=(center.x,lo.y-2,center.z)
cam.rotation_euler=(Vector((0,1,0))).to_track_quat('-Z','Y').to_euler()
camdata.type='ORTHO';camdata.ortho_scale=max(hi.x-lo.x,hi.z-lo.z)*1.16
scene.camera=cam;bpy.context.view_layer.update()
mat=mesh.data.materials[0];nodes=mat.node_tree.nodes;links=mat.node_tree.links
base=next(n.image for n in nodes if n.type=='TEX_IMAGE' and 'BaseColor' in n.image.name)
nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');em=nodes.new('ShaderNodeEmission');tex=nodes.new('ShaderNodeTexImage');tex.image=base
links.new(tex.outputs['Color'],em.inputs['Color']);links.new(em.outputs[0],out.inputs['Surface'])
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['prepare']
if args[0]=='prepare':
    scene.render.filepath=str(OUT/'front-albedo.png');bpy.ops.render.render(write_still=True)
    base.filepath_raw=str(OUT/'original-basecolor.png');base.file_format='PNG';base.save()
    (OUT/'source.json').write_text(json.dumps({'mesh':mesh.name,'vertices':len(mesh.data.vertices),'bones':len(arm.data.bones),'camera':list(cam.location),'scale':camdata.ortho_scale,'bounds':[list(lo),list(hi)]},indent=2))
    print('BLOOM PREPARED',OUT)
else:
    # Define the edited orthographic view on the posed surface, then freeze those coordinates in the original UV atlas.
    # Visibility is measured against the original surface so the front never leaks through onto the back or hidden petals.
    from mathutils.bvhtree import BVHTree
    polygons=[tuple(p.vertices) for p in posed.polygons]
    tree=BVHTree.FromPolygons(points,polygons,all_triangles=False)
    project_name=mesh.data.uv_layers.new(name='BloomFrontProjection').name
    mask=mesh.data.color_attributes.new(name='BloomFrontWeight',type='FLOAT_COLOR',domain='CORNER')
    project=mesh.data.uv_layers[project_name]
    for loop in mesh.data.loops:
        p=points[loop.vertex_index];uv=world_to_camera_view(scene,cam,p)
        project.data[loop.index].uv=(uv.x,uv.y)
        # Ortho front rays travel along +Y. Only the first visible intersection receives the new albedo.
        origin=Vector((p.x,lo.y-1,p.z));hit,normal,face,distance=tree.ray_cast(origin,Vector((0,1,0)))
        visible=hit is not None and (hit-p).length<.0015
        n=(evaluated.matrix_world.to_3x3().inverted().transposed() @ posed.corner_normals[loop.index].vector).normalized()
        front=max(0,min(1,(-n.y-.02)/.32))
        weight=front if visible else 0
        mask.data[loop.index].color=(weight,weight,weight,1)
    edited=bpy.data.images.load(str(Path(args[1]).resolve()))
    overlay=nodes.new('ShaderNodeTexImage');overlay.image=edited;overlay.extension='EXTEND'
    coord=nodes.new('ShaderNodeUVMap');coord.uv_map=project_name
    links.new(coord.outputs['UV'],overlay.inputs['Vector'])
    attr=nodes.new('ShaderNodeVertexColor');attr.layer_name=mask.name
    mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MIX'
    links.new(attr.outputs['Color'],mix.inputs[0]);links.new(tex.outputs['Color'],mix.inputs[1]);links.new(overlay.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs['Color'])
    # The source texture must keep reading its original UVs during baking.
    originaluv=nodes.new('ShaderNodeUVMap');originaluv.uv_map=mesh.data.uv_layers[0].name
    links.new(originaluv.outputs['UV'],tex.inputs['Vector'])
    scene.render.filepath=str(OUT/'projected-front.png');bpy.ops.render.render(write_still=True)
    target=bpy.data.images.new('Bloom_clean_basecolor',width=2048,height=2048,alpha=False)
    targetnode=nodes.new('ShaderNodeTexImage');targetnode.image=target;nodes.active=targetnode
    mesh.data.uv_layers.active_index=0
    mesh.data.uv_layers[0].active_render=True
    bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);bpy.context.view_layer.objects.active=mesh
    scene.render.bake.margin=16;scene.render.bake.use_clear=True
    bpy.ops.object.bake(type='EMIT',use_selected_to_active=False,uv_layer=mesh.data.uv_layers[0].name)
    target.filepath_raw=str(OUT/'bloom-clean-basecolor.png');target.file_format='PNG';target.save()
    print('BLOOM BAKED',target.filepath_raw)
    # Repeat the unlit front review using the baked atlas, not the generated reference.
    links.new(tex.outputs['Color'],em.inputs['Color']);tex.image=target
    scene.render.filepath=str(OUT/'baked-front.png');bpy.ops.render.render(write_still=True)
evaluated.to_mesh_clear()
