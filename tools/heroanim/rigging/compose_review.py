"""Compose checked real-rig renders into a motion preview and before/after contact sheet."""
import hashlib
import json
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
WORK=HERE.parents[2]/'content/work/rigging'
OUT=HERE.parent/'products/rig-fix-v2'
FAMILIES=('twig','sprig','bloom','drop')
OUT.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',20)
sha=lambda path:hashlib.sha256(path.read_bytes()).hexdigest()
baseline_root=WORK/'astra-baseline'
baseline=json.loads((baseline_root/'baseline.json').read_text()) if (baseline_root/'baseline.json').exists() else None
rows=[('before','Original weights')]+([('previous','Previous rig')] if baseline else [])+[('after','Rig V2')]
canvas=Image.new('RGB',(1440,456*len(rows)),'#f3ead9');draw=ImageDraw.Draw(canvas)
outfits=Image.new('RGB',(1440,456*4),'#f3ead9');outfit_draw=ImageDraw.Draw(outfits)
sides=Image.new('RGB',(1440,384*3),'#f3ead9');side_draw=ImageDraw.Draw(sides)
sequences=None
for column,family in enumerate(FAMILIES):
    root=WORK/family
    record=json.loads((root/'review/review.json').read_text())
    assert record.get('complete') and record['recipeSha256']==sha(HERE/'review.mjs'),(family,'incomplete/stale render recipe')
    for version in ('before','after'):
        assert record['sourceHashes'][version]==sha(root/(version+'.glb')),(family,version,'stale review')
    if family=='twig':
        video_record=json.loads((root/'review/video.json').read_text())
        assert video_record.get('complete') and video_record['recipeSha256']==sha(HERE/'review.mjs'),'Incomplete/stale motion render recipe'
        assert video_record['sourceHashes']['after']==record['sourceHashes']['after'],'Stale Twig motion render'
        sequences=json.loads((root/'video/sequences.json').read_text())
        assert video_record['options'].get('allActions') and {s['clip'] for s in sequences}=={'Idle','Walk','Wave','Joy','Sad','Curiosity'},'Publish all six body actions'
    for row,(version,label) in enumerate(rows):
        filename=f'{"after" if version=="previous" else version}-Jacket-Wave-45.png'
        if version=='previous':
            source=baseline_root/family/filename
            expected=next(f['sha256'] for f in baseline[family]['files'] if f['file']==filename)
            assert sha(source)==expected,(family,'previous rig render changed')
        else:
            assert any(c['file']==filename for c in record['captures']),(family,filename,'not in completed review')
            source=root/'review'/filename
        image=Image.open(source).convert('RGB');image.thumbnail((360,432))
        canvas.paste(image,(column*360,row*456+24))
        draw.text((column*360+10,row*456+4),family.title()+' / '+label,fill='#3f2d1d',font=font)
    for row,body in enumerate(('none','Jacket','Coat','Overalls')):
        filename=f'after-{body}-QA_ArmLift-45.png'
        assert any(c['file']==filename for c in record['captures']),(family,filename,'not in completed review')
        image=Image.open(root/'review'/filename).convert('RGB');image.thumbnail((360,432))
        outfits.paste(image,(column*360,row*456+24))
        outfit_draw.text((column*360+10,row*456+4),family.title()+' / '+body,fill='#3f2d1d',font=font)
    detail=json.loads((root/'review/detail.json').read_text())
    assert detail.get('complete') and detail['recipeSha256']==sha(HERE/'review.mjs'),(family,'incomplete/stale side render recipe')
    assert detail['sourceHashes']['after']==record['sourceHashes']['after'],(family,'stale side review')
    for row,body in enumerate(('Jacket','Coat','Overalls')):
        filename=f'detail-after-{body}-Wave-90.png'
        assert any(c['file']==filename for c in detail['captures']),(family,filename,'not in completed side review')
        image=Image.open(root/'review'/filename).convert('RGB');image.thumbnail((360,360))
        sides.paste(image,(column*360,row*384+24))
        side_draw.text((column*360+10,row*384+4),family.title()+' / '+body,fill='#3f2d1d',font=font)
canvas.save(OUT/'rig-comparison.jpg',quality=92)
outfits.save(OUT/'outfit-checks.jpg',quality=92)
sides.save(OUT/'side-checks.jpg',quality=92)
args=['ffmpeg','-hide_banner','-loglevel','error','-y']
args+=['-framerate','12','-i',str(WORK/'twig/video/%05d.png')]
filters=[]
filters.append('[0:v]pad=iw:ih+40:0:0:color=0xf3ead9,drawtext=fontfile=/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf:text=Twig:x=12:y=12:fontsize=22:fontcolor=0x3f2d1d[grid]')
label='[grid]'
for index,s in enumerate(sequences):
    target=f'[clip{index}]'
    filters.append(label+f"drawtext=fontfile=/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf:text={s['clip']} / {s['body']}:x=(w-tw)/2:y=h-30:fontsize=22:fontcolor=0x3f2d1d:enable='between(t,{s['start']/12},{(s['end']+1)/12-.001})'"+target)
    label=target
args+=['-filter_complex',';'.join(filters),'-map',label,'-frames:v',str(sequences[-1]['end']+1),'-an','-c:v','libx264','-threads','2','-filter_complex_threads','2','-crf','22','-pix_fmt','yuv420p','-movflags','+faststart',str(OUT/'baseline-actions.mp4')]
subprocess.run(args,check=True)
print('Composed actual rig comparison, outfit/side checks and six-action motion preview')
