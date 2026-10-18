"""Fit owner-delivered Sprig/Twig additions; retain one owner-maintained Blender master."""
import bpy, json, hashlib, zipfile, sys
from pathlib import Path
from mathutils import Matrix, Vector

HERE=Path(__file__).resolve().parent; ROOT=HERE.parent.parent
SOURCE=HERE/'models/wardrobe-additions-v1'; OUT=ROOT/'content/work/wardrobe-additions-v1/build'
OUT.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
fit=json.loads((SOURCE/'fit.json').read_text()); inspection='--inspect' in sys.argv
for a in fit['archives']:assert sha(SOURCE/a['path'])==a['sha256']
handoff=zipfile.ZipFile(OUT/'SprigTwig_Additions_Fitted.zip','w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) if not inspection else None
for family in ['sprig','twig']:
    folder=OUT/family;folder.mkdir(exist_ok=True);(folder/'fbx').mkdir(exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    accepted=json.loads((HERE/'models'/f'{family}-v2/build-report.json').read_text())
    model=ROOT/f'content/work/{family}-v2/build'/accepted['export']['file'];assert sha(model)==accepted['export']['sha256']
    bpy.ops.import_scene.gltf(filepath=str(model))
    originals=[o for o in bpy.data.objects if o.type=='MESH']
    original_geometry={o.name:[tuple(o.matrix_world@v.co) for v in o.data.vertices] for o in originals};items=[]
    for item in [i for i in fit['items'] if i['family']==family]:
        with zipfile.ZipFile(SOURCE/item['archive']) as z:data=z.read(item['entry'])
        assert hashlib.sha256(data).hexdigest()==item['sha256']
        source=folder/item['entry'];source.write_bytes(data)
        previous=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(source))
        added=set(bpy.data.objects)-previous;meshes=[o for o in added if o.type=='MESH'];rigs=[o for o in added if o.type=='ARMATURE']
        assert len(meshes)==1 and len(rigs)==1,(item['entry'],len(meshes),len(rigs))
        obj=meshes[0];rig=rigs[0];rig.data.pose_position='REST';bpy.context.view_layer.update()
        world=obj.matrix_world.copy();inverse=world.inverted();before=[tuple(world@v.co) for v in obj.data.vertices]
        uv=[tuple(l.uv) for l in obj.data.uv_layers.active.data]
        weights=[[(g.group,g.weight) for g in v.groups] for v in obj.data.vertices];names={g.index:g.name for g in obj.vertex_groups}
        rigid=all(len([g for g in w if g[1]>1e-6])==1 and all(names[i]=='Head' and abs(wt-1)<1e-5 for i,wt in w if wt>1e-6) for w in weights)
        if item['slot']=='Head':assert rigid,item['entry']+' must bind rigidly to Head'
        if not inspection:
            pivot=Vector(item['pivot']);transform=Matrix.Translation(Vector(item['translation']))@Matrix.Translation(pivot)@Matrix.Diagonal((*item['scale'],1))@Matrix.Translation(-pivot)
            for v in obj.data.vertices:v.co=inverse@(transform@Vector(before[v.index]))
            smoothing=item.get('boundarySmooth')
            if smoothing:
                edges={}
                for p in obj.data.polygons:
                    for edge in p.edge_keys:edges[tuple(sorted(edge))]=edges.get(tuple(sorted(edge)),0)+1
                boundary={}
                for (a,b),count in edges.items():
                    if count==1:boundary.setdefault(a,set()).add(b);boundary.setdefault(b,set()).add(a)
                coords=[v.co.copy() for v in obj.data.vertices]
                for _ in range(smoothing['iterations']):
                    next_coords=[v.copy() for v in coords]
                    for i,neighbours in boundary.items():
                        if len(neighbours)==2:next_coords[i]=coords[i].lerp(sum((coords[j] for j in neighbours),Vector())/2,smoothing['strength'])
                    coords=next_coords
                for v,c in zip(obj.data.vertices,coords):v.co=c
        assert uv==[tuple(l.uv) for l in obj.data.uv_layers.active.data]
        assert weights==[[(g.group,g.weight) for g in v.groups] for v in obj.data.vertices]
        expected=[tuple(world@v.co) for v in obj.data.vertices];error=None;final=None
        if not inspection:
            bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
            final=folder/'fbx'/item['entry']
            bpy.ops.export_scene.fbx(filepath=str(final),use_selection=True,object_types={'MESH','ARMATURE'},bake_anim=False,add_leaf_bones=False,path_mode='COPY',embed_textures=True)
            previous=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(final));returned=set(bpy.data.objects)-previous
            for o in returned:
                if o.type=='ARMATURE':o.data.pose_position='REST'
            bpy.context.view_layer.update();mesh=next(o for o in returned if o.type=='MESH');assert len(mesh.data.vertices)==len(expected)
            error=max(abs((mesh.matrix_world@v.co)[i]-expected[v.index][i]) for v in mesh.data.vertices for i in range(3));assert error<1e-5
            for o in returned:bpy.data.objects.remove(o,do_unlink=True)
        obj.parent=None;obj.matrix_world=world
        for m in list(obj.modifiers):obj.modifiers.remove(m)
        obj.data.transform(world,shape_keys=True);obj.matrix_world=Matrix.Identity(4);obj.name='Addition_'+item['key']
        for o in added:
            if o!=obj and o.name in bpy.data.objects:bpy.data.objects.remove(o,do_unlink=True)
        for image in bpy.data.images:
            if image.source=='FILE' and not image.packed_file:
                try:image.pack()
                except RuntimeError:pass
        source.unlink()
        bounds=[[min(v.co[i] for v in obj.data.vertices),max(v.co[i] for v in obj.data.vertices)] for i in range(3)]
        items.append(dict(key=item['key'],id=item['id'],vertices=len(obj.data.vertices),bounds=bounds,rigidHead=rigid,uvUnchanged=True,weightsUnchanged=True,maxFitDisplacement=max((Vector(a)-Vector(b)).length for a,b in zip(before,expected)),fbxRoundTripError=error,fbx=final.name if final else None,fbxSha256=sha(final) if final else None))
        if final:
            handoff.write(final,final.name);final.unlink()
    for o in originals:assert original_geometry[o.name]==[tuple(o.matrix_world@v.co) for v in o.data.vertices]
    export=folder/'WardrobeAdditions.glb';bpy.ops.export_scene.gltf(filepath=str(export),export_format='GLB',export_animations=False)
    report=dict(version=1,family=family,inspection=inspection,scriptSha256=sha(__file__),fitSha256=sha(SOURCE/'fit.json'),acceptedModelSha256=sha(model),acceptedGeometryUnchanged=True,items=items,export=dict(file=export.name,sha256=sha(export)))
    (folder/'build-report.json').write_text(json.dumps(report,indent=2)+'\n')
    if handoff:handoff.write(folder/'build-report.json',family+'/build-report.json')
    print(family,': prepared',len(items),'items',flush=True)

if handoff:
    handoff.write(SOURCE/'fit.json','fit.json');handoff.close()
