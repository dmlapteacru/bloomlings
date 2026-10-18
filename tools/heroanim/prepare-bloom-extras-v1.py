"""Fit nine owner FBXs to accepted Bloom; preserve the owner's sole Blender master."""
import bpy, json, hashlib, zipfile
from pathlib import Path
from mathutils import Matrix, Vector

HERE=Path(__file__).resolve().parent; ROOT=HERE.parent.parent
SOURCE=HERE/'models/bloom-extras-v1'; OUT=ROOT/'content/work/bloom-extras-v1/build'
OUT.mkdir(parents=True,exist_ok=True); (OUT/'fbx').mkdir(exist_ok=True)
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
fit=json.loads((SOURCE/'fit.json').read_text()); reports=[]
bpy.ops.wm.read_factory_settings(use_empty=True)
old_report=json.loads((HERE/'models/bloom-v2/build-report.json').read_text())
old_model=ROOT/'content/work/bloom-v2/build'/old_report['export']['file']
assert sha(old_model)==old_report['export']['sha256']
bpy.ops.import_scene.gltf(filepath=str(old_model))
base_meshes=[o for o in bpy.data.objects if o.type=='MESH']
base_geometry={o.name:[tuple(o.matrix_world@v.co) for v in o.data.vertices] for o in base_meshes}
for item in fit['items']:
    source=SOURCE/item['source']; assert sha(source)==item['sha256']
    previous=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=str(source))
    added=set(bpy.data.objects)-previous
    meshes=[o for o in added if o.type=='MESH']; rigs=[o for o in added if o.type=='ARMATURE']
    assert len(meshes)==1 and len(rigs)==1
    obj=meshes[0]; rig=rigs[0]; rig.data.pose_position='REST'; bpy.context.view_layer.update()
    world=obj.matrix_world.copy(); inverse=world.inverted()
    before=[tuple(world@v.co) for v in obj.data.vertices]
    uv=[tuple(l.uv) for l in obj.data.uv_layers.active.data]
    weights=[[(g.group,g.weight) for g in v.groups] for v in obj.data.vertices]
    if item['slot']=='Head':
        names={g.index:g.name for g in obj.vertex_groups}
        assert all(len([g for g in w if g[1]>1e-6])==1 and all(names[i]=='Head' and abs(wt-1)<1e-5 for i,wt in w if wt>1e-6) for w in weights)
    pivot=Vector(item['pivot'])
    transform=Matrix.Translation(Vector(item['translation']))@Matrix.Translation(pivot)@Matrix.Diagonal((*item['scale'],1))@Matrix.Translation(-pivot)
    for v in obj.data.vertices:v.co=inverse@(transform@Vector(before[v.index]))
    # Local rear-edge smoothing is restricted to declared garment vertices; UVs and weights stay intact.
    smooth=item.get('rearSmooth',{})
    if smooth:
        neighbours=[set() for _ in obj.data.vertices]
        for edge in obj.data.edges:
            a,b=edge.vertices;neighbours[a].add(b);neighbours[b].add(a)
        coords=[world@v.co for v in obj.data.vertices]
        for _ in range(smooth['iterations']):
            next_coords=[v.copy() for v in coords]
            for i,v in enumerate(coords):
                if not neighbours[i]:continue
                amount=max(0,min(1,(v.y-smooth['startY'])/(smooth['fullY']-smooth['startY'])))*smooth['strength']
                if amount:next_coords[i]=v.lerp(sum((coords[j] for j in neighbours[i]),Vector())/len(neighbours[i]),amount)
            coords=next_coords
        for v,p in zip(obj.data.vertices,coords):v.co=inverse@p
    assert uv==[tuple(l.uv) for l in obj.data.uv_layers.active.data]
    assert weights==[[(g.group,g.weight) for g in v.groups] for v in obj.data.vertices]
    expected=[tuple(world@v.co) for v in obj.data.vertices]
    # Retain the supplied skeleton in the fitted owner handoff. The game copy below is static.
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
    final=OUT/'fbx'/source.name
    bpy.ops.export_scene.fbx(filepath=str(final),use_selection=True,object_types={'MESH','ARMATURE'},bake_anim=False,add_leaf_bones=False,path_mode='COPY',embed_textures=True)
    after=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(final));roundtrip=set(bpy.data.objects)-after
    returned=next(o for o in roundtrip if o.type=='MESH')
    for r in roundtrip:
        if r.type=='ARMATURE':r.data.pose_position='REST'
    bpy.context.view_layer.update()
    assert len(returned.data.vertices)==len(expected)
    error=max(abs((returned.matrix_world@v.co)[i]-expected[v.index][i]) for v in returned.data.vertices for i in range(3))
    assert error<1e-5,('FBX rest geometry round trip',item['key'],error)
    for o in roundtrip:bpy.data.objects.remove(o,do_unlink=True)
    obj.parent=None;obj.matrix_world=world
    for modifier in list(obj.modifiers):obj.modifiers.remove(modifier)
    obj.data.transform(world,shape_keys=True);obj.matrix_world=Matrix.Identity(4);obj.name='BloomExtra_'+item['key']
    for o in added:
        if o!=obj and o.name in bpy.data.objects:bpy.data.objects.remove(o,do_unlink=True)
    bounds=[[min(v.co[i] for v in obj.data.vertices),max(v.co[i] for v in obj.data.vertices)] for i in range(3)]
    reports.append(dict(key=item['key'],id=item['id'],vertices=len(obj.data.vertices),uvUnchanged=True,weightsUnchanged=True,rigidHead=item['slot']=='Head',maxFitDisplacement=max((Vector(a)-Vector(b)).length for a,b in zip(before,expected)),fbxRoundTripError=error,bounds=bounds,fbx=final.name,fbxSha256=sha(final)))
for obj in base_meshes:
    assert base_geometry[obj.name]==[tuple(obj.matrix_world@v.co) for v in obj.data.vertices]
model=OUT/'BloomExtras.glb';bpy.ops.export_scene.gltf(filepath=str(model),export_format='GLB',export_animations=False)
report=dict(version=1,scriptSha256=sha(__file__),fitSha256=sha(SOURCE/'fit.json'),acceptedModelSha256=sha(old_model),acceptedGeometryUnchanged=True,items=reports,export=dict(file=model.name,sha256=sha(model)))
(OUT/'build-report.json').write_text(json.dumps(report,indent=2)+'\n')
archive=OUT/'BloomHeroV2_Extras_Fitted.zip'
with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for f in sorted((OUT/'fbx').glob('*.fbx')):z.write(f,f.name)
    z.write(SOURCE/'fit.json','fit.json');z.write(OUT/'build-report.json','build-report.json')
print('Prepared',len(reports),'fitted FBXs; accepted Bloom geometry unchanged')
