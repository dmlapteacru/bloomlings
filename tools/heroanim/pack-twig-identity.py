"""Publish current Twig profile/fallback art. Sources remain outside runtime; item IDs are stable.

Run the V2 and simple-hero bakes first, plus bake-twig-blank.mjs for the optional blank twin.
Does not touch level sources, catalog, or L2000 mosaic.
"""
from pathlib import Path
import hashlib,json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
HERE=ROOT/'tools/heroanim'
ART=ROOT/'client/Assets/Bloomlings/Art'
REF=HERE/'references/twig-v2/identity'
MOTION=ART/'Heroes/Resources/HeroMotion'
OUT=[]
def save(image,path,**kwargs):
    image.save(path,**kwargs)
    OUT.append({'path':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'width':image.width,'height':image.height})
for source,target in [('avatar-default.png','twig_default.jpg'),('avatar-glasses-scarf.png','twig_glasses_scarf_clay.jpg'),('avatar-autumn-wreath.png','twig_autumn_wreath_3d.jpg')]:
    image=Image.open(REF/source).convert('RGB').resize((384,384),Image.Resampling.LANCZOS)
    save(image,ART/'Avatars/Resources/Avatars'/target,quality=92,optimize=True,subsampling=0)
# Build fallback from exactly the same current baked sprite, preserving logical scale/feet.
v2=json.loads((HERE/'twig-v2.json').read_text())
f=next(f for f in v2['outputs'] if f['key']=='base-pumpkinhat')
canvas=Image.new('RGBA',(448,504));canvas.alpha_composite(Image.open(MOTION/f['file']).convert('RGBA'),(f['crop'][0],f['crop'][1]))
canvas=canvas.resize((512,576),Image.Resampling.LANCZOS)
chars=ART/'Characters/Resources/Characters'
for name in ['twig.png','twig-cheer.png']:
    save(canvas,chars/'3d'/name,optimize=True,compress_level=9)
blank=Image.open(ROOT/'content/work/twig-v2/blank-render.png').convert('RGBA').resize((512,576),Image.Resampling.LANCZOS)
save(blank,chars/'3d/twig-blank.png',optimize=True,compress_level=9)
# Group fallback uses current production renders; consistent foot line and no duplicate hero masters.
group=Image.new('RGBA',(1200,720));foot=round(720*.62)
for i,family in enumerate(['sprig','bloom','drop','twig']):
    file=MOTION/('twigv2-base-pumpkinhat.png' if family=='twig' else 'simplehero-'+family+'.png')
    image=Image.open(file).convert('RGBA');image=image.crop(image.getbbox());height=round(720*(.62-.15));width=round(image.width*height/image.height)
    image=image.resize((width,height),Image.Resampling.LANCZOS)
    group.alpha_composite(image,(round(150+300*i-width/2),foot-height))
save(group,chars/'3d/group.png',optimize=True,compress_level=9)
m=json.loads((chars/'manifest.json').read_text())
for f in m['files']:
    if f['path'] in ['3d/twig.png','3d/twig-cheer.png','3d/twig-blank.png','3d/group.png']:
        file=chars/f['path'];f.update(sha256=hashlib.sha256(file.read_bytes()).hexdigest(),source='owner',record='tools/heroanim/SOURCE.md')
(chars/'manifest.json').write_text(json.dumps(m,indent=2)+'\n')
# Badge is published separately; include it in the same identity verification record.
badge=ART/'Badges/Resources/Badges/hero_twig.png'
with Image.open(badge) as image:
    OUT.append({'path':str(badge.relative_to(ROOT)),'sha256':hashlib.sha256(badge.read_bytes()).hexdigest(),'width':image.width,'height':image.height})
# Record outputs and every image input, including the temporary blank render's derivation.
inputs=[REF/p for p in ['avatar-default.png','avatar-glasses-scarf.png','avatar-autumn-wreath.png','hero-medal.png']]
inputs += [MOTION/f for f in ['simplehero-sprig.png','simplehero-bloom.png','simplehero-drop.png','twigv2-base-pumpkinhat.png']]
inputs += [HERE/p for p in ['pack-twig-identity.py','bake-twig-blank.mjs','models/twig-v2/build-report.json']]
inputs.append(ROOT/'tools/pack_badges.py')
(HERE/'twig-identity.json').write_text(json.dumps({'version':1,'sources':[{'path':str(p.relative_to(ROOT)),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in inputs],'outputs':OUT},indent=2)+'\n')
print('Packed',len(OUT),'current Twig identity / fallback assets')
