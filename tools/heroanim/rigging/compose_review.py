"""Compose checked real-rig renders into a motion preview and before/after contact sheet."""
import hashlib
import json
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
WORK=HERE.parents[2]/'content/work/rigging'
OUT=HERE.parent/'products/rig-fix-v1'
FAMILIES=('twig','sprig','bloom','drop')
OUT.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',20)
canvas=Image.new('RGB',(1440,912),'#f3ead9');draw=ImageDraw.Draw(canvas)
sequences=None
for column,family in enumerate(FAMILIES):
    root=WORK/family
    digest=hashlib.sha256((root/'after.glb').read_bytes()).hexdigest()
    assert json.loads((root/'review/review.json').read_text())['sourceHashes']['after']==digest,(family,'stale review')
    if family=='twig':
        assert json.loads((root/'review/video.json').read_text())['sourceHashes']['after']==digest,'Stale Twig motion render'
        sequences=json.loads((root/'video/sequences.json').read_text())
    for row,version in enumerate(('before','after')):
        source=root/'review'/f'{version}-Jacket-Wave-45.png'
        image=Image.open(source).convert('RGB');image.thumbnail((360,432))
        canvas.paste(image,(column*360,row*456+24))
        draw.text((column*360+10,row*456+4),family.title()+' / '+('Source' if version=='before' else 'Repaired'),fill='#3f2d1d',font=font)
canvas.save(OUT/'rig-comparison.jpg',quality=92)
args=['ffmpeg','-hide_banner','-loglevel','error','-y']
args+=['-framerate','12','-i',str(WORK/'twig/video/%05d.png')]
filters=[]
filters.append('[0:v]pad=iw:ih+40:0:0:color=0xf3ead9,drawtext=fontfile=/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf:text=Twig:x=12:y=12:fontsize=22:fontcolor=0x3f2d1d[grid]')
label='[grid]'
for index,s in enumerate(sequences):
    target=f'[clip{index}]'
    filters.append(label+f"drawtext=fontfile=/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf:text={s['clip']}:x=(w-tw)/2:y=h-30:fontsize=22:fontcolor=0x3f2d1d:enable='between(t,{s['start']/12},{(s['end']+1)/12-.001})'"+target)
    label=target
args+=['-filter_complex',';'.join(filters),'-map',label,'-frames:v',str(sequences[-1]['end']+1),'-an','-c:v','libx264','-threads','2','-filter_complex_threads','2','-crf','22','-pix_fmt','yuv420p','-movflags','+faststart',str(OUT/'baseline-actions.mp4')]
subprocess.run(args,check=True)
print('Composed actual source / repaired contact sheet and motion preview')
