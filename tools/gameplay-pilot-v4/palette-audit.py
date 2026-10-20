"""Palette diagnostics, not a colour-vision certification. No image transforms."""
import itertools,json,math,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def linear(s):return s/12.92 if s<=.04045 else ((s+.055)/1.055)**2.4
def rgb(h):return [linear(int(h[i:i+2],16)/255) for i in [1,3,5]]
def transform(c,m):return [max(0,min(1,sum(a*b for a,b in zip(row,c)))) for row in m]
def lab(c):
 xyz=[sum(a*b for a,b in zip(row,c))/white for row,white in zip([[.4124564,.3575761,.1804375],[.2126729,.7151522,.072175],[.0193339,.119192,.9503041]],[.95047,1,1.08883])]
 f=lambda t:t**(1/3) if t>(6/29)**3 else t/(3*(6/29)**2)+4/29
 x,y,z=map(f,xyz);return [116*y-16,500*(x-y),200*(y-z)]
matrices={'normal':[[1,0,0],[0,1,0],[0,0,1]],'protan':[[.152286,1.052583,-.204868],[.114503,.786281,.099216],[-.003882,-.048116,1.051998]],'deutan':[[.367322,.860646,-.227968],[.280085,.672501,.047413],[-.011820,.042940,.968881]],'tritan':[[1.255528,-.076749,-.178779],[-.078411,.930809,.147602],[.004733,.691367,.303900]]}
palettes=json.loads(subprocess.check_output(['node','-e',"const p=require('./tools/gameplay-pilot-v4/palette.cjs');console.log(JSON.stringify(Object.fromEntries(p.modes.map(m=>[m,p.palette(m)]))))"],cwd=ROOT,text=True))
out={}
for mode,items in palettes.items():
 items.pop('?');assert len({i['mark'] for i in items.values()})==7
 assert all(i['contrast']>=4.5 for i in items.values())
 pairs={k:sorted([{'pair':a+b,'deltaE76':round(math.dist(lab(transform(rgb(items[a]['color']),m)),lab(transform(rgb(items[b]['color']),m))),2)} for a,b in itertools.combinations(items,2)],key=lambda p:p['deltaE76']) for k,m in matrices.items()}
 out[mode]={'materials':items,'pairs':pairs,'forcedPatterns':mode=='accessible'}
report={'palettes':out,'limits':['CVD matrices are an approximation; ΔE76 is a diagnostic, not a universal readability threshold.','Both palettes can have similar simulated hues. Unique material symbols are always on capsules; accessible mode forces symbols on board tiles.','Real phone and colour-vision user testing still required.']}
p=ROOT/'content/pilot-v4/palette-audit.json';p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
for mode,data in out.items():print(mode,{k:v[0] for k,v in data['pairs'].items()})
