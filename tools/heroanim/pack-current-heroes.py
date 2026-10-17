"""Publish the accepted quartet without changing save IDs or maintaining another model source.

Only atlas extraction, RGBA-safe resampling and image encoding happen here. The images
are authored with ImageGen against the actual production sprites; blanks are rendered
from verified accepted GLBs by bake-current-blanks.mjs.
"""
from pathlib import Path
import hashlib,json,runpy
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];HERE=ROOT/'tools/heroanim'
REF=HERE/'references/current-identity';ART=ROOT/'client/Assets/Bloomlings/Art'
MOTION=ART/'Heroes/Resources/HeroMotion';CHAR=ART/'Characters/Resources/Characters'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
# Retain the owner's already accepted Twig portraits and publish its matching group / blank.
runpy.run_path(str(HERE/'pack-twig-identity.py'),run_name='__main__')
outputs=[];inputs=[Path(__file__),HERE/'twig-identity.json',HERE/'current-blanks.json',REF/'avatar-atlas.png',REF/'props-atlas.png']
def save(im,p,**kwargs):
 p.parent.mkdir(parents=True,exist_ok=True);im.save(p,**kwargs)
 outputs.append(dict(path=str(p.relative_to(ROOT)),sha256=sha(p),width=im.width,height=im.height))
def cell(atlas,x,y,cols,rows,inset=0):
 return atlas.crop((round(atlas.width*x/cols)+inset,round(atlas.height*y/rows)+inset,round(atlas.width*(x+1)/cols)-inset,round(atlas.height*(y+1)/rows)-inset))
# Existing eleven avatar choices retain their names, tiers, prices, motif and order.
avatars=[
 ['sprig_default','sprig_ladybug_watercolor','sprig_dewdrop_papercut','sprig_flower_crown_3d'],
 ['bloom_default','bloom_ribbon_plush','bloom_pearl_tiara_3d','bloom_jeweled_crown_magic'],
 ['drop_default','drop_sailor_sticker','drop_bubble_shell_3d']]
with Image.open(REF/'avatar-atlas.png') as atlas:
 for y,row in enumerate(avatars):
  for x,name in enumerate(row):
   im=cell(atlas,x,y,4,4,2).convert('RGB').resize((384,384),Image.Resampling.LANCZOS)
   save(im,ART/'Avatars/Resources/Avatars'/(name+'.jpg'),quality=92,optimize=True,subsampling=0)
# Only cut the authored isolated objects; transparent margins are retained on the runtime props.
with Image.open(REF/'props-atlas.png') as atlas:
 for x,family in enumerate(['sprig','bloom','drop']):
  im=cell(atlas,x,0,3,2).convert('RGBA')
  box=im.getchannel('A').point(lambda a:255 if a>2 else 0).getbbox()
  if not box:raise ValueError('Empty hero medal')
  im=im.crop((max(0,box[0]-3),max(0,box[1]-3),min(im.width,box[2]+3),min(im.height,box[3]+3)))
  save(im,ROOT/'output/badges-v1/achievements'/('hero_'+family+'.png'),optimize=True,compress_level=9)
 for x,name in enumerate(['promo-noads-sprig','promo-noads-sprig-push']):
  im=cell(atlas,x,1,3,2).convert('RGBA')
  box=im.getchannel('A').point(lambda a:255 if a>2 else 0).getbbox()
  if not box:raise ValueError('Empty promo sprite')
  # Trim only empty packing space; fit the original authored pose into the existing 512px prop canvas.
  im=im.crop(box);factor=min(460/im.width,480/im.height);im=im.resize((round(im.width*factor),round(im.height*factor)),Image.Resampling.LANCZOS)
  canvas=Image.new('RGBA',(512,512));canvas.alpha_composite(im,((512-im.width)//2,496-im.height))
  save(canvas,ART/'Decor/Resources/Decor'/(name+'.png'),optimize=True,compress_level=9)
# Source-size registered fallback portraits and matching blanks for every accepted family.
for family,head in [('sprig','hairleafy'),('bloom','hairpetals'),('drop','nativedrop')]:
 record=HERE/(family+'-v2.json');inputs.append(record);meta=json.loads(record.read_text())
 f=next(f for f in meta['outputs'] if f['key']=='base-'+head);source=MOTION/f['file'];inputs.append(source)
 canvas=Image.new('RGBA',(448,504));canvas.alpha_composite(Image.open(source).convert('RGBA'),tuple(f['crop'][:2]));canvas=canvas.resize((512,576),Image.Resampling.LANCZOS)
 for suffix in ['', '-cheer']:save(canvas,CHAR/'3d'/(family+suffix+'.png'),optimize=True,compress_level=9)
 source=REF/'blanks'/(family+'.png');inputs.append(source)
 blank=Image.open(source).convert('RGBA').resize((512,576),Image.Resampling.LANCZOS)
 save(blank,CHAR/'3d'/(family+'-blank.png'),optimize=True,compress_level=9)
# Update exact runtime provenance, retaining all unrelated images.
mfile=CHAR/'manifest.json';m=json.loads(mfile.read_text());changed={f['path']:f for f in outputs}
for f in m['files']:
 target=str((CHAR/f['path']).relative_to(ROOT))
 if target in changed:f.update(sha256=sha(ROOT/target),source='owner',record='tools/heroanim/SOURCE.md')
mfile.write_text(json.dumps(m,indent=2)+'\n')
# Keep the raw Twig medal concept current too; all four badge sources now depict accepted heroes.
source=HERE/'references/twig-v2/identity/hero-medal.png';inputs.append(source)
with Image.open(source) as im:save(im.convert('RGBA'),ROOT/'output/badges-v1/achievements/hero_twig.png',optimize=True,compress_level=9)
# Pack three updated hero medals through the normal badge pipeline; it retains all IDs and other medals.
namespace=runpy.run_path(str(ROOT/'tools/pack_badges.py'))
for family in ['sprig','bloom','drop']:
 namespace['pack']('hero_'+family)
 p=ART/'Badges/Resources/Badges'/('hero_'+family+'.png')
 with Image.open(p) as im:outputs.append(dict(path=str(p.relative_to(ROOT)),sha256=sha(p),width=im.width,height=im.height))
# The promo manifest is used by the existing art tooling and source audit.
p=ART/'Decor/garden-promos-v2.json';m=json.loads(p.read_text())
for f in m:
 if f['file'] in ['promo-noads-sprig.png','promo-noads-sprig-push.png']:
  target=ART/'Decor/Resources/Decor'/f['file'];im=Image.open(target)
  f.update(source='tools/heroanim/references/current-identity/props-atlas.png',sourceSHA256=sha(REF/'props-atlas.png'),sha256=sha(target),size=str(im.width)+'x'+str(im.height),bytes=target.stat().st_size)
p.write_text(json.dumps(m,indent=2)+'\n')
inputs.append(ROOT/'tools/pack_badges.py')
(HERE/'current-identity.json').write_text(json.dumps(dict(version=1,sources=[dict(path=str(p.relative_to(ROOT)),sha256=sha(p)) for p in inputs],outputs=outputs),indent=2)+'\n')
print('Published',len(outputs),'accepted hero identity images; all save IDs retained')
