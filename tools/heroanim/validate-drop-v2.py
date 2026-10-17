"""Measure actual deformation, then test the delivered FBX assembly and animation roundtrip."""
import bpy,numpy as np,json,sys,hashlib
from pathlib import Path
from mathutils.kdtree import KDTree
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent/'content/work/drop-v2'
def albedos(o):
 result=set()
 for mat in o.data.materials:
  if not mat or not mat.use_nodes:continue
  for node in mat.node_tree.nodes:
   if node.type=='TEX_IMAGE' and node.image:
    im=node.image;raw=bytes(im.packed_file.data) if im.packed_file else Path(bpy.path.abspath(im.filepath)).read_bytes();result.add(hashlib.sha256(raw).hexdigest())
 return sorted(result)
def coords(o):
 ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();a=np.empty(len(me.vertices)*3,dtype=np.float32);me.vertices.foreach_get('co',a);a=a.reshape(-1,3);m=np.array(ev.matrix_world);a=a@m[:3,:3].T+m[:3,3];ev.to_mesh_clear();return a
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'rig-cache.blend'));arm=bpy.data.objects['DropHeroV2Rig'];objects={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'};actions=sorted(bpy.data.actions,key=lambda a:a.name);assert len(actions)==5;source_albedos={n:albedos(o) for n,o in objects.items()}
arm.data.pose_position='REST';bpy.context.view_layer.update();rest={n:coords(o) for n,o in objects.items()};edges={};lengths={};uvs={};sk={};bones={b.name:np.array(b.matrix_local) for b in arm.data.bones}
for n,o in objects.items():
 e=np.array([e.vertices[:] for e in o.data.edges]);l=np.linalg.norm(rest[n][e[:,0]]-rest[n][e[:,1]],axis=1);mask=l>1e-4;edges[n]=e[mask];lengths[n]=l[mask];uvs[n]=np.array([l.uv[:] for l in o.data.uv_layers.active.data]);sk[n]=[k.name for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else []
 for v in o.data.vertices:
  assert 1<=len(v.groups)<=4 and abs(sum(g.weight for g in v.groups)-1)<1e-5,(n,v.index)
report={'version':1,'meshCount':len(objects),'boneCount':len(bones),'sampledPoses':[],'fbxFiles':[],'roundtrip':[]};samples={};arm.data.pose_position='POSE'
for act in actions:
 arm.animation_data.action=act;selected=[round(float(act.frame_range[0]+(act.frame_range[1]-act.frame_range[0])*v)) for v in [0,.23,.61]]
 for f in sorted(set(selected+[round(float(v)) for v in np.linspace(*act.frame_range,13)])):
  bpy.context.scene.frame_set(f);bpy.context.view_layer.update();row={'clip':act.name,'frame':f,'parts':{}};snapshot={}
  for n,o in objects.items():
   a=coords(o);e=edges[n];ratio=np.linalg.norm(a[e[:,0]]-a[e[:,1]],axis=1)/lengths[n];head=all(o.vertex_groups[g.group].name=='Head' and abs(g.weight-1)<1e-6 for g in o.data.vertices[0].groups)
   row['parts'][n]={'p01':float(np.quantile(ratio,.01)),'p99':float(np.quantile(ratio,.99)),'min':float(ratio.min()),'max':float(ratio.max()),'maxEdgeLengthChange':float(np.max(abs(ratio-1)*lengths[n]))}
   assert row['parts'][n]['p99']<1.6 and row['parts'][n]['p01']>.55,(act.name,f,n,'excessive joint strain',row['parts'][n])
   assert row['parts'][n]['maxEdgeLengthChange']<.04,(act.name,f,n,'visible edge stretch')
   if head:assert np.max(abs(ratio-1))<.002,(act.name,f,n,'head stretched')
   assert np.isfinite(a).all(),(act.name,f,n)
   if f in selected:snapshot[n]=a.astype(np.float32)
  if f in selected:samples[(act.name,f)]=snapshot
  report['sampledPoses'].append(row)
# Collect summaries before strict limits: visible failures must be fixed, never hidden by passing export alone.
for clip in [a.name for a in actions]:
 rows=[(p['max'],p['p99'],p['min'],n,r['frame']) for r in report['sampledPoses'] if r['clip']==clip for n,p in r['parts'].items()];print('STRAIN',clip,sorted(rows,reverse=True)[:4],flush=True)
(ROOT/'deformation-report.json').write_text(json.dumps(report,indent=2)+'\n')
if '--measure-only' in sys.argv:print('MEASURED',len(report['sampledPoses']),'poses',flush=True);raise SystemExit
# Import exactly the FBX handoff into an empty scene, unifying only identical skeletons.
bpy.ops.wm.read_factory_settings(use_empty=True)
manifest=json.loads((ROOT/'handoff/inventory.json').read_text());shared=None;imported={}
for entry in manifest['files']:
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(ROOT/'handoff'/entry['path']),use_anim=True);added=set(bpy.data.objects)-before;rigs=[o for o in added if o.type=='ARMATURE'];assert len(rigs)==1
 rig=rigs[0];assert set(b.name for b in rig.data.bones)==set(bones)
 error=max(float(np.max(abs(np.array(b.matrix_local)-bones[b.name]))) for b in rig.data.bones);assert error<3e-6,(entry['path'],error)
 if shared is None:shared=rig
 for o in added:
  if o.type!='MESH':continue
  assert o.name in objects,o.name;assert len(o.data.vertices)==len(rest[o.name]),o.name
  assert albedos(o)==source_albedos[o.name],(o.name,'embedded albedo changed')
  # Same export positions/UV loops, no remesh or atlas conversion.
  meshkeys=[k.name for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else [];assert meshkeys==sk[o.name],(o.name,meshkeys,sk[o.name])
  nowuv=np.array([l.uv[:] for l in o.data.uv_layers.active.data]);assert nowuv.shape==uvs[o.name].shape and np.max(abs(nowuv-uvs[o.name]))<2e-6,(o.name,'UV changed')
  for mod in o.modifiers:
   if mod.type=='ARMATURE':mod.object=shared
  w=o.matrix_world.copy();o.parent=shared;o.matrix_world=w;imported[o.name]=o
 if rig!=shared:bpy.data.objects.remove(rig,do_unlink=True)
 report['fbxFiles'].append({'path':entry['path'],'skeletonMaxError':error})
assert set(imported)==set(objects);shared.animation_data_create();shared.animation_data.action=None;shared.data.pose_position='REST';bpy.context.view_layer.update();mapping={}
for n,o in imported.items():
 tree=KDTree(len(rest[n]));[tree.insert(p,i) for i,p in enumerate(rest[n])];tree.balance();matches=[tree.find(p) for p in coords(o)];mapping[n]=np.array([m[1] for m in matches]);assert max(m[2] for m in matches)<2e-6,(n,'rest roundtrip')
shared.data.pose_position='POSE'
for (clip,f),snapshot in samples.items():
 candidates=[a for a in bpy.data.actions if clip in a.name and any('pose.bones' in fc.data_path for fc in a.fcurves)];assert len(candidates)==1,(clip,[a.name for a in candidates]);shared.animation_data.action=candidates[0];bpy.context.scene.frame_set(f);bpy.context.view_layer.update();row={'clip':clip,'frame':f,'errors':{}}
 for n,o in imported.items():
  a=coords(o);target=snapshot[n][mapping[n]];err=np.linalg.norm(a-target,axis=1);row['errors'][n]=float(err.max());assert err.max()<3e-4,(clip,f,n,'FBX motion mismatch',err.max())
 report['roundtrip'].append(row)
report['maxRoundtripError']=max(v for row in report['roundtrip'] for v in row['errors'].values());report['complete']=True
(ROOT/'validation-report.json').write_text(json.dumps(report,indent=2)+'\n');print('VALIDATED',len(report['fbxFiles']),'FBX files;',len(samples),'roundtrip poses; max error',report['maxRoundtripError'],flush=True)
