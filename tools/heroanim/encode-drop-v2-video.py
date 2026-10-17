"""Encode actual GLB frames with action labels; no synthesized character frames."""
import json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]/'content/work/drop-v2'
sequence=json.loads((ROOT/'video-frames/sequences.json').read_text());filters=['pad=400:526:0:0:color=0xf4f0e6']
for item in sequence:
 assert item['clip'] in ['Idle','Walk','Wave','Joy','Sad']
 filters.append("drawtext=fontfile=/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf:text='Drop - "+item['clip']+"':fontsize=20:fontcolor=0x253f51:x=18:y=18:enable='between(t,"+str(item['start']/10)+','+str(item['end']/10)+")'")
subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate','10','-i',str(ROOT/'video-frames/%04d.png'),'-vf',','.join(filters),'-c:v','libx264','-threads','2','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(ROOT/'motions.mp4')],check=True)
print('ENCODED',ROOT/'motions.mp4')
