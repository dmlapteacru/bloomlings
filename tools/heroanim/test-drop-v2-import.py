"""Integration smoke test of the exact script delivered to the owner, including visibility / fit drivers."""
import bpy,runpy,json
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent/'content/work/drop-v2'
bpy.ops.wm.read_factory_settings(use_empty=True);runpy.run_path(str(HERE/'import-drop-v2.py'),run_name='__main__')
rig=bpy.data.objects['DropHeroV2Rig'];parts={o.name.removeprefix('DropHeroV2_'):o for o in rig.children if o.type=='MESH'}
assert len([o for o in bpy.context.scene.objects if o.type=='ARMATURE'])==1
assert len(parts)==23 and len(bpy.data.actions)==5
heads=['NativeDrop','HairSideWave','HairTwinRipples','HairWhirlpool','BucketHat','Cap','Beret','EarmuffHair','XmasHat'];bodies=['none','Jacket','Coat','Overalls'];scarves=['none','ScarfKnit','ScarfStripe']
def update():rig.update_tag();bpy.context.view_layer.update()
for i,h in enumerate(heads):
 rig['head_style']=i;update()
 assert [n for n in heads if not parts[n].hide_viewport]==[h]
 assert parts['Base_Head'].hide_viewport==(i==0)
for i,b in enumerate(bodies):
 rig['body_style']=i;update();assert [n for n in bodies[1:] if not parts[n].hide_viewport]==([] if i==0 else [b])
 for s in scarves[1:]:
  for j,n in enumerate(bodies[1:],1):assert abs(parts[s].data.shape_keys.key_blocks['Over_'+n].value-(1 if i==j else 0))<1e-6
for i,s in enumerate(scarves):
 rig['scarf_style']=i;update();assert [n for n in scarves[1:] if not parts[n].hide_viewport]==([] if i==0 else [s])
for i in [0,1]:rig['glasses']=i;update();assert parts['Glasses'].hide_viewport==(i==0)
images={node.image for o in parts.values() for m in o.data.materials if m and m.use_nodes for node in m.node_tree.nodes if node.type=='TEX_IMAGE' and node.image};assert all(im.packed_file for im in images)
for o in parts.values():assert all(m.object==rig for m in o.modifiers if m.type=='ARMATURE')
report={'complete':True,'rigs':1,'meshes':len(parts),'actions':5,'headChoices':9,'bodyChoices':4,'scarfChoices':3,'glassesChoices':2,'packedImages':len(images),'visibilityDrivers':True,'scarfFitDrivers':True}
(ROOT/'import-report.json').write_text(json.dumps(report,indent=2)+'\n');print('IMPORT SMOKE PASSED',json.dumps(report),flush=True)
