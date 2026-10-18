"""Finalize the single portable FBX handoff after preparing all four families."""
import json,hashlib,zipfile
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
product=H/'products/Wardrobe_Clothes_Cleaned_Drop_Additions.zip'
expected={};reports={}
for family in ['sprig','bloom','twig','drop']:
 p=R/'content/work/wardrobe-finish-v1'/family/'build-report.json';j=json.loads(p.read_text());reports[family]=j
 for item in j['items']:expected[family+'/'+item['fbx']]=item['fbxSha256']
assert len(expected)==26
with zipfile.ZipFile(product) as z:
 assert len(z.namelist())==len(set(z.namelist())), 'No duplicate ZIP entries'
 actual={i.filename:hashlib.sha256(z.read(i)).hexdigest() for i in z.infolist() if i.filename.endswith('.fbx')}
 assert actual==expected,'FBXs must match the repaired, round-tripped game mesh reports'
with zipfile.ZipFile(product) as z:
 if any(n in z.namelist() for n in ['README.md','fit.json','drop-fit.json','inventory.json']):kept=[(i,z.read(i)) for i in z.infolist() if i.filename.endswith('.fbx') or i.filename.endswith('build-report.json')]
 else:kept=None
if kept is not None:
 with zipfile.ZipFile(product,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
  for info,data in kept:z.writestr(info,data)
with zipfile.ZipFile(product,'a',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for name,p in [('README.md',H/'models/wardrobe-finish-v1/README.md'),('fit.json',H/'models/wardrobe-finish-v1/fit.json'),('drop-fit.json',H/'models/drop-extras-v1/fit.json')]:
  assert name not in z.namelist(),'Reprepare families before finalizing a package again'
  z.write(p,name)
 z.writestr('inventory.json',json.dumps({'version':1,'files':expected},indent=2)+'\n')
print('Packaged',len(expected),'verified FBXs;',round(product.stat().st_size/1024/1024,1),'MiB')
