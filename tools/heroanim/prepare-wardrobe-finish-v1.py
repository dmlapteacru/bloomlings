"""Fit Drop additions and repair all garments; export the exact same meshes to FBX and game GLB.
Owner .blend remains authoritative. This script only writes reproducible build data.
"""
import bpy,json,zipfile,hashlib,sys,math
from pathlib import Path
from mathutils import Matrix,Vector
from mathutils.kdtree import KDTree
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent;sys.path.insert(0,str(HERE))
from garment_cleanup import repair_garment
SOURCE=HERE/'models/wardrobe-finish-v1';OUT=ROOT/'content/work/wardrobe-finish-v1';OUT.mkdir(parents=True,exist_ok=True)
config=json.loads((SOURCE/'fit.json').read_text());drop=json.loads((HERE/'models/drop-extras-v1/fit.json').read_text())
family=sys.argv[sys.argv.index('--family')+1] if '--family' in sys.argv else 'drop';axis=config['families'][family]
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
product=HERE/'products/Wardrobe_Clothes_Cleaned_Drop_Additions.zip';product.parent.mkdir(exist_ok=True)
folder=OUT/family;folder.mkdir(exist_ok=True);temp=folder/'source';temp.mkdir(exist_ok=True)
inspection='--inspect' in sys.argv
if not inspection and product.exists():
 with zipfile.ZipFile(product) as z:kept=[(i,z.read(i)) for i in z.infolist() if not i.filename.startswith(family+'/')]
 with zipfile.ZipFile(product,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
  for info,data in kept:z.writestr(info,data)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(ROOT/axis['model']))
old=list(bpy.data.objects)
# Normalize the static assembly's names. Drop retains its separate rounded head.
for o in old:
 if o.name.startswith(('Addition_','BloomExtra_')):o.name='Finish_'+o.name.split('_',1)[1]
 elif o.name.startswith(family.title()+'V2_'):o.name='Finish_'+o.name.split('_',1)[1]
base_original={o.name:[tuple(o.matrix_world@v.co) for v in o.data.vertices] for o in old if o.type=='MESH' and ('base' in o.name or 'Base_' in o.name)}
reports=[]
def load(item):
 if 'sourceSha256' in item:assert sha(ROOT/item.get('source',item.get('archive')))==item['sourceSha256']
 if 'archive' in item:
  with zipfile.ZipFile(ROOT/item['archive']) as z:data=z.read(item['entry'])
  p=temp/Path(item['entry']).name;p.write_bytes(data)
 else:p=ROOT/item['source']
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(p),use_anim=False);added=set(bpy.data.objects)-before
 rigs=[o for o in added if o.type=='ARMATURE'];meshes=[o for o in added if o.type=='MESH'];assert len(meshes)==1
 for r in rigs:r.data.pose_position='REST'
 bpy.context.view_layer.update();o=meshes[0];world=o.matrix_world.copy();o.data.transform(world);o.matrix_world=Matrix.Identity(4)
 if 'archive' in item:p.unlink()
 for image in bpy.data.images:
  if image.source=='FILE' and image.users and not image.packed_file and Path(bpy.path.abspath(image.filepath)).is_file():image.pack()
 return o,rigs,added

def dispose(objects):
 for o in objects:
  if o.name in bpy.data.objects:bpy.data.objects.remove(o,do_unlink=True)

def static(o,added,key,rotation=None):
 world=o.matrix_world.copy();o.parent=None;o.matrix_world=world
 for m in list(o.modifiers):o.modifiers.remove(m)
 if rotation:o.data.transform(rotation@world,shape_keys=True)
 else:o.data.transform(world,shape_keys=True)
 o.matrix_world=Matrix.Identity(4);o.name='Finish_'+key;dispose([x for x in added if x!=o])

def export(o,rigs,key,entry):
 final=folder/(family.title()+'HeroV2_'+key+'.fbx');bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
 for rig in rigs:rig.select_set(True)
 bpy.context.view_layer.objects.active=rigs[0] if rigs else o
 bpy.ops.export_scene.fbx(filepath=str(final),use_selection=True,object_types={'MESH','ARMATURE'},bake_anim=False,add_leaf_bones=False,path_mode='COPY',embed_textures=True)
 expected=[tuple(o.matrix_world@v.co) for v in o.data.vertices];uv=[tuple(l.uv) for l in o.data.uv_layers.active.data]
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(final),use_anim=False);returned=set(bpy.data.objects)-before
 for r in returned:
  if r.type=='ARMATURE':r.data.pose_position='REST'
 bpy.context.view_layer.update();m=next(x for x in returned if x.type=='MESH');assert len(m.data.vertices)==len(expected)
 error=max(abs((m.matrix_world@v.co)[a]-expected[v.index][a]) for v in m.data.vertices for a in range(3));assert error<1e-5
 assert len(m.data.uv_layers.active.data)==len(uv)
 assert all(v.groups and abs(sum(g.weight for g in v.groups)-1)<1e-4 for v in m.data.vertices)
 dispose(returned);entry.update(fbx=final.name,fbxSha256=sha(final),fbxRoundTripError=error)
 if not inspection:
  with zipfile.ZipFile(product,'a',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:z.write(final,family+'/'+final.name)
 final.unlink()

# Bloom's extra headwear is kept exactly at its already-reviewed fit.
if family=='bloom':
 extra_root=HERE/'models/bloom-extras-v1'
 for item in json.loads((extra_root/'fit.json').read_text())['items']:
  if item['slot']!='Head':continue
  record={**item,'source':str((extra_root/item['source']).relative_to(ROOT))};o,rigs,added=load(record)
  pivot=Vector(item['pivot']);transform=Matrix.Translation(Vector(item['translation']))@Matrix.Translation(pivot)@Matrix.Diagonal((*item['scale'],1))@Matrix.Translation(-pivot)
  o.data.transform(transform);static(o,added,item['key'])

# Replace every garment with its repaired, UV-mapped source FBX. Accepted skin/head
# vertices never enter the repair operation.
tops={'sprig':{'Coat':.365,'Jacket':.397,'Overalls':.365,'VestExplorer':.400},'twig':{'Coat':.385,'Jacket':.413,'Overalls':.383,'VestExplorer':.380,'ShirtExplorer':.413},'bloom':{'Coat':.376,'Jacket':.377,'Overalls':.376,'DressPurple':.380,'TopFlower':.376},'drop':{'Coat':.348,'Jacket':.346,'Overalls':.348}}
for item in [i for i in config['items'] if i['family']==family]:
 key=item['key'];o,rigs,added=load(item);assert rigs
 if 'scale' in item:
  pivot=Vector(item['pivot']);o.data.transform(Matrix.Translation(Vector(item['translation']))@Matrix.Translation(pivot)@Matrix.Diagonal((*item['scale'],1))@Matrix.Translation(-pivot))
 entry=dict(key=key,id=item['id'],slot='Body',**repair_garment(o,{'topCut':tops[family][key],**({'neckSkin':'green','skinMinZ':.355} if family=='sprig' and key=='VestExplorer' else {'neckSkin':'blue','skinMinZ':.325,'skinHalfWidth':.055} if family=='drop' and key=='Overalls' else {})}))
 export(o,rigs,key,entry)
 prior=bpy.data.objects.get('Finish_'+key) or bpy.data.objects.get('DropHeroV2_'+key)
 if prior:dispose([prior,*list(prior.children_recursive)])
 static(o,added,key,Matrix.Rotation(-math.pi/2,4,'Z') if family=='drop' else None);reports.append(entry)

if family=='drop':
 # Use the already accepted Drop skeleton; no new animation authoring here.
 original=HERE/'models/drop-v2/products/DropHeroV2_FBX.zip'
 with zipfile.ZipFile(original) as z:
  entry=next(n for n in z.namelist() if n.endswith('/DropHeroV2_Coat.fbx'));data=z.read(entry)
 donor=temp/'donor.fbx';donor.write_bytes(data);body,rigs,donor_added=load({'source':str(donor.relative_to(ROOT))});rig=rigs[0]
 tree=KDTree(len(body.data.vertices))
 for v in body.data.vertices:tree.insert(v.co,v.index)
 tree.balance();group_names={g.index:g.name for g in body.vertex_groups}
 for item in drop['items']:
  archive=HERE/'models/drop-extras-v1'/item['source'];assert sha(archive)==item['sha256']
  with zipfile.ZipFile(archive) as z:
   for f in z.infolist():assert (temp/f.filename).resolve().is_relative_to(temp.resolve())
   z.extractall(temp);fbx=next(n for n in z.namelist() if n.endswith('.fbx'))
  o,unused,added=load({'source':str((temp/fbx).relative_to(ROOT))});assert not unused
  o.data.transform(Matrix.Translation(Vector(item['translation']))@Matrix.Diagonal((*item['scale'],1)))
  # Mesh is in the original +X-front Drop coordinate system, matching the rig.
  for g in list(o.vertex_groups):o.vertex_groups.remove(g)
  groups={name:o.vertex_groups.new(name=name) for name in (['Head'] if item['slot']=='Head' else list(group_names.values()))}
  for v in o.data.vertices:
   if item['slot']=='Head':groups['Head'].add([v.index],1,'REPLACE')
   else:
    _,idx,_=tree.find(v.co)
    for g in body.data.vertices[idx].groups:groups[group_names[g.group]].add([v.index],g.weight,'REPLACE')
  mod=o.modifiers.new('Drop accepted skin','ARMATURE');mod.object=rig;world=o.matrix_world.copy();o.parent=rig;o.matrix_world=world
  entry=dict(key=item['key'],id=item['id'],slot=item['slot'])
  if item['slot']=='Body':entry.update(repair_garment(o,{'topCut':.323 if item['key']=='TopSailor' else .340,**({'neckSkin':'blue','skinMinZ':.20} if item['key']=='ShirtMint' else {})}))
  export(o,rigs,item['key'],entry);static(o,added,item['key'],Matrix.Rotation(-math.pi/2,4,'Z'));reports.append(entry)
  for f in list(temp.iterdir()):
   if f.name=='donor.fbx':continue
   if f.is_file():f.unlink()
   elif f.is_dir():
    import shutil;shutil.rmtree(f)
 dispose(donor_added);donor.unlink()

for name,points in base_original.items():
 o=bpy.data.objects[name];assert points==[tuple(o.matrix_world@v.co) for v in o.data.vertices]
# Only albedo enters the static bake model. FBXs keep supplied full materials.
for mat in bpy.data.materials:
 if not mat.users or not mat.use_nodes:continue
 bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 if not bs:continue
 keep={l.from_node for l in bs.inputs['Base Color'].links}
 for n in list(mat.node_tree.nodes):
  if n.type=='TEX_IMAGE' and n not in keep:mat.node_tree.nodes.remove(n)
for image in list(bpy.data.images):
 if not image.users:bpy.data.images.remove(image)
model=folder/'WardrobeFinish.glb';bpy.ops.export_scene.gltf(filepath=str(model),export_format='GLB',export_animations=False)
report=dict(version=1,family=family,scriptSha256=sha(__file__),cleanupSha256=sha(HERE/'garment_cleanup.py'),fitSha256=sha(SOURCE/'fit.json'),dropFitSha256=sha(HERE/'models/drop-extras-v1/fit.json'),baseGeometryUnchanged=True,items=reports,export=dict(file=model.name,sha256=sha(model)))
(folder/'build-report.json').write_text(json.dumps(report,indent=2)+'\n')
if not inspection:
 with zipfile.ZipFile(product,'a',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:z.write(folder/'build-report.json',family+'/build-report.json')
print('Prepared',family,len(reports),'items; base geometry unchanged',flush=True)
