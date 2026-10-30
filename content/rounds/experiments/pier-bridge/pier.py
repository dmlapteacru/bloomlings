# A night fair on a pier, one whole picture (the owner, 2026-10-30: «сделай одну общую картинку, и там дальше уже где-то
# сделай разрыв и место для мостов, как ты сделал с предыдущей картинкой … чтобы это подходило вообще к концепции»):
# the Ferris wheel and the big top stand on a wooden pier over the sea; a neutral line (the divider, a gap strip) outlines
# the pier with the wheel and the tent, as the island's did. The bridges are gangways: from the beach up to the pier's
# front (it decides; it lands on the boardwalk) and from a rowboat to the pier's left end (it lands on the wheel's green
# platform, whose pods come last). The sea and the night sky are the background, finished last.
import math,json,sys
W,H=32,32
g=[['N']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,y0=0,y1=H,only=None):
    for y in range(y0,y1):
        for x in range(W):
            if pred(x+.5,y+.5) and (only is None or only(x,y)): g[y][x]=c
# near region: the night sky, the sea, the beach with a spit up to the front gangway, a rowboat on the left
fill(lambda x,y:y>=17.5,'B')
fill(lambda x,y:y>=28.4+.5*math.sin(x*.7),'A')
# far region: the pier deck, the wheel's platform at its left end, the Ferris wheel, the big top
deck=lambda x,y:19<=int(y)<=23 and 3<=int(x)<=28
wc,wr,wy=9.5,7.0,9.8
wheel=lambda x,y:math.hypot(x-wc,y-wy)<=wr
legs=lambda x,y:15<=int(y)<=18 and abs(x-wc)<=1.6+(int(y)-15)*0.9   # the wheel's stand: a wooden base on the deck
tx,ay,eave=23.0,4.4,11.5
roof=lambda x,y:5<=int(y)<=11 and abs(x-tx)<=(y-ay)/(eave-ay)*6.0
walls=lambda x,y:12<=int(y)<=18 and 17<=int(x)<=28
pole=lambda x,y:int(x)==23 and 2<=int(y)<=4
for y in range(H):
    for x in range(W):
        X,Y=x+.5,y+.5
        if deck(X,Y) or wheel(X,Y) or legs(X,Y) or roof(X,Y) or walls(X,Y) or pole(X,Y): far[y][x]=True
for y in range(H):
    for x in range(W):
        if not far[y][x]: continue
        X,Y=x+.5,y+.5
        c='N'                                              # the sky seen through the wheel
        if deck(X,Y): c='L' if x<=10 else 'E'
        if legs(X,Y): c='E'
        if wheel(X,Y):
            d=math.hypot(X-wc,Y-wy)
            if d>=wr-1.05: c='Y'
            elif d<=1.3: c='Y'
        if roof(X,Y) or walls(X,Y):
            c='R' if int(math.floor((tx+(X-tx)*((eave-ay)/max(.5,Y-ay) if Y<eave else 1)-tx+1)/2))%2==0 else 'C'
        if pole(X,Y): c='E'
        g[y][x]=c
# the wheel's spokes and cabins, the tent's dark door with orange curtains, flags
for (dx,dy) in [(1,0),(0,1),(1,1),(1,-1)]:
    for t in range(-7,8):
        x,y=int(round(wc-.5+dx*t)),int(round(wy-.5+dy*t))
        if 0<=x<W and 0<=y<H and far[y][x] and math.hypot(x+.5-wc,y+.5-wy)<wr-1.1 and math.hypot(x+.5-wc,y+.5-wy)>1.3: g[y][x]='C'
for k in range(8):
    a=k*math.pi/4; x0,y0=wc+wr*math.cos(a),wy+wr*math.sin(a)
    for y in range(H):
        for x in range(W):
            if far[y][x] and abs(x+.5-x0)<=1.0 and 0<=y+.5-y0+.3<=1.8: g[y][x]='R' if k%2 else 'O'
for y in range(14,19):
    half=(y-13.5)*0.75
    for x in range(W):
        if far[y][x] and abs(x+.5-tx)<=half: g[y][x]='N' if abs(x+.5-tx)<=half-1 else 'O'
for (x,y) in [(24,2),(25,2),(24,3)]:
    far[y][x]=True; g[y][x]='O'
# the divider: every near cell beside a far cell
# a pocket of sky that the line would shut in (between the wheel, its base and the tent) belongs to the subject
from collections import deque
while True:
    line=[[not far[y][x] and any(0<=x+dx<W and 0<=y+dy<H and far[y+dy][x+dx] for dx,dy in ((1,0),(-1,0),(0,1),(0,-1))) for x in range(W)] for y in range(H)]
    open_=lambda x,y:not far[y][x] and not line[y][x]
    seen=[[False]*W for _ in range(H)];q=deque((x,y) for y in range(H) for x in range(W) if (x in (0,W-1) or y in (0,H-1)) and open_(x,y))
    for x,y in q: seen[y][x]=True
    while q:
        x,y=q.popleft()
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            u,v=x+dx,y+dy
            if 0<=u<W and 0<=v<H and open_(u,v) and not seen[v][u]: seen[v][u]=True;q.append((u,v))
    shut=[(x,y) for y in range(H) for x in range(W) if open_(x,y) and not seen[y][x]]
    # a line cell with no open neighbour is inside the subject too (it would split it)
    inner=[(x,y) for y in range(H) for x in range(W) if line[y][x] and not any(0<=x+dx<W and 0<=y+dy<H and (open_(x+dx,y+dy) and seen[y+dy][x+dx]) for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)))]
    if not shut and not inner: break
    for x,y in shut+inner: far[y][x]=True; g[y][x]='N'
for y in range(H):
    for x in range(W):
        if line[y][x]: g[y][x]='#'
# gangways: from the beach to the pier's front (crossing up) and from the rowboat to its left end (crossing right)
bx=17; by=max(y for y in range(H) if far[y][bx])+1; g[by][bx]='b'
ly=21; lx=min(x for x in range(W) if far[ly][x])-1; g[ly][lx]='b'
for (sx,sy,ax,ay2) in [(bx,by,bx,by+1),(lx,ly,lx-1,ly)]:
    for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
        x,y=sx+dx,sy+dy
        if (x,y)!=(ax,ay2) and 0<=x<W and 0<=y<H and not far[y][x]: g[y][x]='#'
# the beach's spit to the front gangway and the rowboat at the left gangway
for y in range(by+1,H):
    for x in range(bx-2,bx+3):
        if g[y][x]=='B': g[y][x]='A'
for (x,y) in [(lx-1,ly),(lx-1,ly-1),(lx-1,ly+1),(lx-2,ly),(lx-2,ly+1),(lx-2,ly-1)]:
    if 0<=x and g[y][x]=='B': g[y][x]='V'
json.dump({'rows':[''.join(r) for r in g]},sys.stdout)
