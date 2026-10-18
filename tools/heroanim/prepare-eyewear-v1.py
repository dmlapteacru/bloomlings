"""Prepare owner head-bound eyewear without maintaining a second Blender master."""
import bpy, json, hashlib
from pathlib import Path
from mathutils import Matrix, Vector
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent;SOURCE=HERE/'models/eyewear-v1'
OUT=ROOT/'content/work/eyewear-v1/build';OUT.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
manifest=json.loads((SOURCE/'source-manifest.json').read_text());reports=[]
for item in manifest['variants']:
 source=SOURCE/item['source'];assert sha(source)==item['sha256']
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(source))
 meshes=[o for o in bpy.data.objects if o.type=='MESH'];rigs=[o for o in bpy.data.objects if o.type=='ARMATURE']
 assert len(meshes)==1 and len(rigs)==1,'Inspect changed FBX structure'
 obj=meshes[0];rig=rigs[0];rig.data.pose_position='REST';bpy.context.view_layer.update()
 groups={g.index:g.name for g in obj.vertex_groups};weights=[[(groups[g.group],g.weight) for g in v.groups if g.weight>1e-6] for v in obj.data.vertices]
 assert all(len(w)==1 and w[0][0]=='Head' and abs(w[0][1]-1)<1e-5 for w in weights),'Eyewear must be rigid to Head'
 before=[tuple(obj.matrix_world@v.co) for v in obj.data.vertices];uv=[tuple(l.uv) for l in obj.data.uv_layers.active.data]
 world=obj.matrix_world.copy();obj.parent=None;obj.matrix_world=world
 for modifier in list(obj.modifiers):obj.modifiers.remove(modifier)
 obj.data.transform(world,shape_keys=True);obj.matrix_world=Matrix.Identity(4)
 # Apply a declared fit to the disposable bake copy only; never mutate the supplied FBX.
 fit=Matrix.Translation(Vector(item['translation']))@Matrix.Diagonal((*item['scale'],1))
 obj.data.transform(fit,shape_keys=True);obj.name=item['key']
 assert uv==[tuple(l.uv) for l in obj.data.uv_layers.active.data]
 error=max(abs(v.co[i]-(fit@Vector(before[n]))[i]) for n,v in enumerate(obj.data.vertices) for i in range(3));assert error<1e-6
 bounds=[[min(v.co[i] for v in obj.data.vertices),max(v.co[i] for v in obj.data.vertices)] for i in range(3)]
 for o in list(bpy.data.objects):
  if o!=obj:bpy.data.objects.remove(o,do_unlink=True)
 file=OUT/(item['key']+'.glb');bpy.ops.export_scene.gltf(filepath=str(file),export_format='GLB',export_animations=False)
 reports.append(dict(id=item['id'],key=item['key'],sourceSha256=sha(source),vertices=len(obj.data.vertices),faces=len(obj.data.polygons),rigidHeadWeights=True,uvUnchanged=True,geometryError=error,bounds=bounds,translation=item['translation'],scale=item['scale'],file=file.name,sha256=sha(file)))
report=dict(version=1,scriptSha256=sha(__file__),manifestSha256=sha(SOURCE/'source-manifest.json'),variants=reports)
(OUT/'build-report.json').write_text(json.dumps(report,indent=2)+'\n');print('Prepared',len(reports),'head-bound eyewear variants')
