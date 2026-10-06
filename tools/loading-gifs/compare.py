import glob, os, subprocess, sys
from lib import *
CONCEPTS = [("1-sign", "1 · Вывеска"), ("2-lotus", "2 · Лотос"), ("3-tiles", "3 · Плитки"), ("4-heroes", "4 · Герои и лепестки")]
PW, PH, TOP, GAP = 270, 585, 64, 16
for kind, title in (("boot", "Старт игры"), ("level", "Переход между уровнями")):
    seqs = [sorted(glob.glob("out/frames/%s-%s/*.png" % (n, kind))) for n, _ in CONCEPTS]
    n = max(len(s) for s in seqs)
    folder = "out/frames/compare-" + kind
    os.makedirs(folder, exist_ok=True)
    head = Image.new("RGBA", (GAP + 4 * (PW + GAP), TOP + PH + GAP), PARCH_TOP + (255,))
    for i, (_, label) in enumerate(CONCEPTS):
        blit(head, text_sprite(label, 26, INK_TITLE), GAP + i * (PW + GAP) + PW / 2, TOP / 2 + 2)
    for f in range(n):
        im = head.copy()
        for i, s in enumerate(seqs):
            fr = Image.open(s[min(f, len(s) - 1)]).convert("RGBA").resize((PW, PH), Image.LANCZOS)
            im.alpha_composite(fr, (GAP + i * (PW + GAP), TOP))
        im.convert("RGB").save("%s/%03d.png" % (folder, f))
    out = "out/compare-%s.gif" % kind
    subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-framerate", str(FPS), "-i", folder + "/%03d.png", "-vf",
                    "split[a][b];[a]palettegen=max_colors=256:stats_mode=full[p];[b][p]paletteuse=dither=bayer:bayer_scale=4",
                    "-loop", "0", out], check=True)
    print(out, os.path.getsize(out) // 1024, "KB")
