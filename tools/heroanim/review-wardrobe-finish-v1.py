"""Contact sheets from the actual old and repaired game PNGs, never generated illustrations."""
from pathlib import Path
from PIL import Image,ImageDraw
import json
R=Path(__file__).resolve().parents[2];H=R/'tools/heroanim';D=R/'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion';O=H/'models/wardrobe-finish-v1/review';O.mkdir(exist_ok=True)
fit=json.loads((H/'models/drop-extras-v1/fit.json').read_text());data=json.loads((R/'content/work/wardrobe-finish-v1/drop/bake.json').read_text());s=data['set'];outputs=data['outputs'];old=json.loads((H/'drop-v2.json').read_text());oldbase=next(f for f in old['outputs'] if f['key']=='base-nativedrop')
sheet=Image.new('RGB',(1200,1155),(240,236,228));draw=ImageDraw.Draw(sheet)
for i,item in enumerate(fit['items']):
 head=s['heads'].index(item['key']) if item['slot']=='Head' else 0;body=s['bodies'].index(item['key']) if item['slot']=='Body' else 0;row=s['front'][(head*len(s['bodies'])+body)*len(s['scarves'])]
 canvas=Image.new('RGBA',(448,600));frames=[outputs[row[0]] if row[0]>=0 else oldbase]
 if row[1]>=0:frames.append(outputs[row[1]])
 for frame in frames:
  im=Image.open(D/frame['file']).convert('RGBA');x,y,*_=frame['crop'];canvas.alpha_composite(im,(x,y+96))
 canvas=canvas.resize((280,375),Image.Resampling.LANCZOS);x=i%3*400+60;y=i//3*385;sheet.paste(canvas,(x,y-15),canvas);draw.text((i%3*400+18,y+352),item['name'],fill=(69,60,43))
sheet.save(O/'drop-additions.jpg',quality=92)

# Preserve the old-vs-repaired icon order so changed cuts are easy to compare.
items=[]
for family in ['sprig','bloom','twig','drop']:
    meta=json.loads((H/f'{family}-v2.json').read_text())
    for frame in meta['outputs']:
        if frame.get('key') in ['icon-jacket','icon-coat','icon-overalls']:
            key=frame['key'].removeprefix('icon-');items.append((family+' '+key,'clothes.'+family+'_'+key,D/frame['file']))
for ext in ['bloom-extras-v1','wardrobe-additions-v1']:
    meta=json.loads((H/(ext+'.json')).read_text())
    for id,index in meta['icons'].items():
        if id.startswith('clothes.'):items.append((id.removeprefix('clothes.'),id,D/meta['outputs'][index]['file']))
for raw,key,id in [('item-02','drop sailor top','clothes.drop_sailor_top'),('item-06','drop mint shirt','clothes.drop_mint_shirt')]:
    items.append((key,id,R/'content/work/drop-extras-v1/inspect'/(raw+'-all.png')))
repaired={}
for family in ['sprig','bloom','twig','drop']:
    cache=json.loads((R/f'content/work/wardrobe-finish-v1/{family}/bake.json').read_text())
    repaired.update({id:D/cache['outputs'][index]['file'] for id,index in cache['icons'].items()})
for view in ['before','after']:
    sheet=Image.new('RGB',(1500,1200),(240,236,228));draw=ImageDraw.Draw(sheet)
    for i,(name,id,before) in enumerate(items):
        file=before if view=='before' else repaired[id]
        if not file.exists():continue
        im=Image.open(file).convert('RGBA');im=im.crop(im.getbbox());im.thumbnail((276,248),Image.Resampling.LANCZOS)
        x=i%5*300+(300-im.width)//2;y=i//5*300+(252-im.height)//2;sheet.paste(im,(x,y),im)
        draw.text((i%5*300+10,i//5*300+268),name,fill=(69,60,43))
    sheet.save(O/('clothes-'+view+'.jpg'),quality=93)
print('Saved actual Drop views and 19 garment comparisons to',O)
