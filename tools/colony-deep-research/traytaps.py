"""Tap times from a Colony Flow recording: a tap moves capsules in the tray, so the tray area changes a lot between two
frames. Reports the start of every burst of change (4 frames per second); merge with slottaps.py when two taps come
within half a second.
usage: python3 -I traytaps.py <video.mp4> [y0,y1] [threshold]   (defaults: the 368x816 XRecorder layout)"""
import sys, subprocess
import numpy as np
video = sys.argv[1]
y0, y1 = [int(v) for v in sys.argv[2].split(',')] if len(sys.argv) > 2 else (560, 700)
thr = float(sys.argv[3]) if len(sys.argv) > 3 else 6
W, H, FPS = 368, 816, 4
raw = subprocess.run(['ffmpeg', '-v', 'error', '-i', video, '-vf', f'fps={FPS}', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'],
                     capture_output=True).stdout
fr = np.frombuffer(raw, np.uint8).reshape(-1, H, W, 3)[:, y0:y1, 40:330].astype(np.int16)
d = np.abs(np.diff(fr, axis=0)).mean(axis=(1, 2, 3))
on = False
for i, v in enumerate(d):
    if v > thr and not on: print(f'{(i + 1) / FPS:7.2f}s  {v:5.1f}')
    on = v > thr * .5
