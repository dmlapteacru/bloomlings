"""Keep the previous Drop pictures outside mobile Resources for the owner's model comparison / rollback."""
import json,hashlib,shutil
from pathlib import Path
HERE=Path(__file__).resolve().parent;REPO=HERE.parent.parent;FRAMES=REPO/'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion';ARCHIVE=HERE/'archive/drop';ARCHIVE.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
# Idempotent: once retired, preserve the original archive proof rather than replacing it with an empty set.
if not (ARCHIVE/'manifest.json').exists():
 files=[]
 for p in sorted(FRAMES.glob('*.png')):
  if p.name.startswith(('drop-idle-','drop-react-')) or p.name=='simplehero-drop.png':
   target=ARCHIVE/p.name;shutil.move(p,target);files.append({'path':p.name,'sha256':sha(target)})
 assert len(files)==133,len(files)
 p=REPO/'client/Assets/Bloomlings/UI/Design/HeroMotionData.cs';shutil.copyfile(p,ARCHIVE/'LegacyHeroMotionData.cs');files.append({'path':'LegacyHeroMotionData.cs','sha256':sha(ARCHIVE/'LegacyHeroMotionData.cs')})
 (ARCHIVE/'manifest.json').write_text(json.dumps({'sourceCommit':'db4b7da98e5e67f350ce9e059422bdd384537417','reason':'Drop V2 in-game trial; old sprites are kept outside runtime for comparison / rollback.','files':files},indent=2)+'\n')
config=HERE/'heroes.json';d=json.loads(config.read_text());d['heroes']=[h for h in d['heroes'] if h['family']!='drop'];config.write_text(json.dumps(d,indent=2)+'\n')
data=REPO/'client/Assets/Bloomlings/UI/Design/HeroMotionData.cs';s=data.read_text();start=s.index('        private static readonly ClipData[] ClipTable =');s=s[:start]+'        private static readonly ClipData[] ClipTable = System.Array.Empty<ClipData>();\n    }\n}\n';data.write_text(s)
p=HERE/'manifest.json';d=json.loads(p.read_text());d['heroes']=[h for h in d['heroes'] if h['family']!='drop'];d['configSha256']=sha(config);d['dataSha256']=sha(data);p.write_text(json.dumps(d,indent=2)+'\n')
p=HERE/'simple-heroes.json';d=json.loads(p.read_text());d['outputs']=[o for o in d['outputs'] if o['family']!='drop'];p.write_text(json.dumps(d,indent=2)+'\n')
p=FRAMES/'manifest.json';d=json.loads(p.read_text());d['files']=[f for f in d['files'] if not f['path'].startswith(('drop-idle-','drop-react-')) and f['path']!='simplehero-drop.png'];p.write_text(json.dumps(d,indent=1)+'\n')
print('Retired 133 legacy Drop PNGs outside mobile Resources')
