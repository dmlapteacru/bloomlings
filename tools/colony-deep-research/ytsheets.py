"""Event storyboards for the colony-flow.com level videos (640x360 YouTube captures, the phone screen in the middle).
Keeps only the moments the tray changes: for every burst of change in the tray area, the frame just before it, a frame
every 0.5 s while it lasts and the frame once it has settled, each cropped to the waiting places + tray and enlarged, 32 to a sheet with timestamps. A
static stretch in between is skipped, so a whole level reads in a few sheets.
usage: python3 -I ytsheets.py <video.mp4> <out_prefix> [x0,x1] [threshold] [WxH:crop_y0,crop_y1:tray_y0,tray_y1]
the layout defaults to the site's 640x360 videos (368x826 owner recordings: 0,368 3.0 368x826:450,730:570,720)
writes <out_prefix>_events.json (event times) and <out_prefix>_NN.jpg; also <out_prefix>_start.png (first frame)."""
import sys, json, subprocess
import numpy as np
from PIL import Image, ImageDraw, ImageFont
video, out = sys.argv[1], sys.argv[2]
x0, x1 = [int(v) for v in sys.argv[3].split(',')] if len(sys.argv) > 3 else (222, 418)
thr = float(sys.argv[4]) if len(sys.argv) > 4 else 3.0
W, H, FPS = 640, 360, 4
cy0, cy1, ty0, ty1 = 222, 360, 262, 340   # waiting places + tray; the tray alone (its movement marks the events)
if len(sys.argv) > 5:
    size, crop, tray = sys.argv[5].split(':')
    W, H = [int(v) for v in size.split('x')]; cy0, cy1 = [int(v) for v in crop.split(',')]; ty0, ty1 = [int(v) for v in tray.split(',')]
raw = subprocess.run(['ffmpeg', '-v', 'error', '-i', video, '-vf', f'fps={FPS},scale={W}:{H}', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'], capture_output=True).stdout
fr = np.frombuffer(raw, np.uint8).reshape(-1, H, W, 3)
Image.fromarray(fr[min(len(fr) - 1, 2)]).save(out + '_start.png')
crop = lambda f: f[cy0:cy1, x0:x1]
tray = fr[:, ty0:ty1, x0:x1].astype(np.int16)
d = np.abs(np.diff(tray, axis=0)).mean(axis=(1, 2, 3))
events, i = [], 0
while i < len(d):
    if d[i] > thr:
        j = i
        while j < len(d) and d[j] > thr * .4: j += 1
        events.append((i, min(len(fr) - 1, j + 2)))  # before = frame i, after = settled (2 frames later)
        i = j + 1
    else: i += 1
frames = []
for a, b in events:
    frames.append((a, 'before'))
    for m in range(a + 2, b, 2): frames.append((m, 'during'))  # every 0.5 s inside a long burst: quick taps stay apart
    frames.append((b, 'after'))
frames.append((len(fr) - 1, 'end'))
f = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 15)
k = 2 if cy1 - cy0 < 200 else 1
cw, ch, cols, per = (x1 - x0) * k, (cy1 - cy0) * k, 4, 32
for s in range(0, len(frames), per):
    chunk = frames[s:s + per]; rows = (len(chunk) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * cw, rows * (ch + 20)), 'white'); dr = ImageDraw.Draw(sheet)
    for k, (idx, kind) in enumerate(chunk):
        x, y = (k % cols) * cw, (k // cols) * (ch + 20)
        sheet.paste(Image.fromarray(crop(fr[idx])).resize((cw, ch), Image.LANCZOS), (x, y + 20))
        n = s + k
        dr.rectangle([x, y, x + cw - 1, y + 19], fill={'before': 'black', 'during': '#403010'}.get(kind, '#204020'))
        dr.text((x + 4, y + 1), f'{kind} {idx / FPS:.2f}s', fill='yellow', font=f)
    sheet.save(f'{out}_{s // per:02d}.jpg', quality=88)
json.dump({'video': video, 'fps': FPS, 'events': [[a / FPS, b / FPS] for a, b in events], 'frames': [[i / FPS, k] for i, k in frames], 'sheets': (len(frames) + per - 1) // per}, open(out + '_events.json', 'w'))
print(video, 'events', len(events), 'sheets', (len(frames) + per - 1) // per)
