"""Tap times from a Colony Flow recording: counts the filled waiting slots in every frame (4 per second) and reports
each rise as a tap (+2 for a linked pair taken by one tap). A slot is empty when its middle is near white.
usage: python3 -I slottaps.py <video.mp4> [y] [x1,x2,...]   (defaults: the 368x816 XRecorder layout)
prints one line per rise: time, slots before -> after"""
import sys, subprocess
import numpy as np
video = sys.argv[1]; Y = int(sys.argv[2]) if len(sys.argv) > 2 else 530
XS = [int(v) for v in sys.argv[3].split(',')] if len(sys.argv) > 3 else [70, 130, 185, 240, 295]
W, H, FPS = 368, 816, 4
raw = subprocess.run(['ffmpeg', '-v', 'error', '-i', video, '-vf', f'fps={FPS}', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'],
                     capture_output=True).stdout
frames = np.frombuffer(raw, np.uint8).reshape(-1, H, W, 3)
def filled(f):
    n = 0
    for x in XS:
        p = f[Y - 4:Y + 5, x - 6:x + 7].reshape(-1, 3).astype(int); m = np.median(p, 0)
        if not (m.min() > 225 and m.max() - m.min() < 18): n += 1
    return n
counts = [filled(f) for f in frames]
# smooth single-frame flickers (a slot finishing shows a grey flash)
sm = [sorted(counts[max(0, i - 1):i + 2])[len(counts[max(0, i - 1):i + 2]) // 2] for i in range(len(counts))]
prev = sm[0]
for i, c in enumerate(sm):
    if c > prev: print(f'{i / FPS:7.2f}s  {prev} -> {c}')
    prev = c
