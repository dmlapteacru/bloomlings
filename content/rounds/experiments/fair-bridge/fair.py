# Night fair beyond a canal, after v8E L22's bridge recipe with pockets (the garden's, mirrored: the left bridge decides):
# the picture (a lit Ferris wheel, a striped big top, the moon) on the far bank; under it two pockets (left blush, right
# moss) closed at the picture's edges and split by a divider, open only upward; the canal row with two bridge sites; on
# the near bank two flower beds (left moss, right blush) with a cream path between them, each site's anchor in its bed.
import math,json,sys,os
W=32; ph=22
L_BED,R_BED='M','K'   # left bed moss (cleared first), right bed blush -> pockets crossed: left blush, right moss
g=[['N']*W for _ in range(ph)]
def put(x,y,c):
    if 0<=x<W and 0<=y<ph: g[y][x]=c
def fill(pred,c):
    for y in range(ph):
        for x in range(W):
            if pred(x+.5,y+.5): put(x,y,c)
# the moon and a few stars
fill(lambda x,y:(x-28)**2+(y-2.6)**2<=4.2,'C')
fill(lambda x,y:(x-29)**2+(y-2.0)**2<=2.2,'N')
for (x,y) in [(17,1),(21,3),(1,1),(24,0),(15,4),(31,7)]: put(x,y,'Y')
# ground
fill(lambda x,y:y>=19,'D')
# Ferris wheel: lit rim, spokes, hub, eight cabins, an A-frame stand
cx,cy,R=9.5,9.5,7.6
fill(lambda x,y:R-1.05<=math.hypot(x-cx,y-cy)<=R,'Y')
for (dx,dy) in [(1,0),(0,1),(1,1),(1,-1)]:
    for t in range(-7,8):
        x,y=cx-.5+dx*t,cy-.5+dy*t
        if math.hypot(x+.5-cx,y+.5-cy)<R-1.1: put(int(round(x)),int(round(y)),'C')
fill(lambda x,y:math.hypot(x-cx,y-cy)<=1.3,'Y')
cab=['R','B','O','B','R','O','B','O']
for k in range(8):
    a=k*math.pi/4
    x0,y0=cx+R*math.cos(a),cy+R*math.sin(a)
    fill(lambda x,y,x0=x0,y0=y0,c=cab[k]:abs(x-x0)<=1.1 and 0<=y-y0+.2<=2.0,cab[k])
for y in range(10,19):
    s=(y-9.5)*0.62
    put(int(cx-s),y,'D'); put(int(cx+s),y,'D')
# big top: a striped cone roof over striped walls (the stripes meet at the eaves), a dark door, an orange flag
tx,ay,eave=24.0,4.6,11.5
stripe=lambda xb:'R' if int(math.floor((xb-tx+1)/2))%2==0 else 'C'
for y in range(12,19):
    for x in range(18,31): put(x,y,stripe(x+.5))
for y in range(5,12):
    half=(y+.5-ay)/(eave-ay)*6.5
    for x in range(W):
        d=x+.5-tx
        if abs(d)<=half: put(x,y,stripe(tx+d*(eave-ay)/(y+.5-ay)))
for y in range(14,19):
    half=(y-13.5)*0.7
    for x in range(W):
        if abs(x+.5-tx)<=half: put(x,y,'N')
for y in range(1,5): put(23,y,'D')
for (x,y) in [(24,1),(25,1),(24,2)]: put(x,y,'O')
# a string of pennants from the wheel to the tent
for x in range(17,21):
    y=7+int(round(((x-18.5)/2.5)**2))
    put(x,y,'B' if x%2 else 'O')
rows=[''.join(r) for r in g]
mid=W//2; far_n=int(os.environ.get('FAR','6')); near_n=int(os.environ.get('NEAR','3'))
far=[''.join(('#' if x in (mid-1,mid,0,W-1) else (R_BED if x<mid-1 else L_BED)) for x in range(W)) for _ in range(far_n)]
xl,xr=round(W*.25),round(W*.75)
canal=''.join('b' if x in (xl,xr) else '#' for x in range(W))
near=[''.join(('C' if x in (mid-1,mid) else (L_BED if x<mid-1 else R_BED)) for x in range(W)) for _ in range(near_n)]
json.dump({'rows':rows+far+[canal]+near},sys.stdout)
