# Japanese garden beyond a pond, after v8E L22's bridge recipe with pockets: the picture (Fuji at sunset, a three-tier
# pagoda, a sakura tree) on the far bank; under it two pockets (left moss, right azaleas) closed at the picture's
# edges and split by a divider, open only upward; the pond row with two bridge sites; on the near bank two beds
# (left azaleas, right moss) with a stone path between them, each site's anchor in its bed.
import math,json,sys,os
W=32; ph=22
P_BED,Q_BED='K','M'   # left bed azaleas, right bed moss -> pockets crossed: left moss, right azaleas
g=[['S']*W for _ in range(ph)]
def put(x,y,c):
    if 0<=x<W and 0<=y<ph: g[y][x]=c
def fill(pred,c,area=None):
    for y in range(ph):
        for x in range(W):
            if pred(x+.5,y+.5): put(x,y,c)
# setting sun behind the mountain
fill(lambda x,y:(x-15.5)**2+(y-3.2)**2<=7,'Y')
# Mount Fuji with a snow cap
fill(lambda x,y:y>=4 and abs(x-11)<=(y-3)*1.15 and y<=17,'N')
fill(lambda x,y:y>=4 and abs(x-11)<=(y-3)*1.15 and y<=7.2+0.8*math.sin(x*1.9),'C')
# ground
fill(lambda x,y:y>=17,'G')
# pagoda: three tiers, dark roofs with upturned ends, red bodies, gold windows and spire
def roof(xc,half,y0):
    for x in range(W):
        d=abs(x+.5-xc)
        if d<=half: put(x,y0,'D')
        if d<=half+1 and d>half-1.5: put(x,y0-1,'D') if d>half-.5 else None
    for x in range(W):
        if abs(x+.5-xc)<=half-1: put(x,y0-1,'D')
def body(xc,half,y0,y1,win=True):
    for y in range(y0,y1+1):
        for x in range(W):
            if abs(x+.5-xc)<=half: put(x,y,'R')
    if win:
        for dx in (-1.5,1.5):
            put(int(xc+dx),y0,'Y')
xc=24.5
for x in (24,): 
    for y in range(0,3): put(x,y,'Y')
roof(xc,3.5,4); body(xc,2.5,5,6)
roof(xc,5,8); body(xc,3.5,9,10)
roof(xc,6.5,12); body(xc,4.5,13,17)
for y in range(15,18):
    for x in (24,25): put(x,y,'D')
for x in (21,28): put(x,14,'Y')
# sakura tree on the left: pink canopy, dark trunk
for (cx,cy,r) in [(4,10,3.2),(7.5,9,3.4),(2.5,13,2.6),(6,12.5,3)]:
    fill(lambda x,y,cx=cx,cy=cy,r=r:(x-cx)**2+(y-cy)**2<=r*r,'P')
for y in range(13,19):
    for x in (5,6): put(x,y,'D')
for (x,y) in [(3,11),(8,8),(6,10),(2,14),(9,11),(4,8)]: put(x,y,'C')
rows=[''.join(r) for r in g]
mid=W//2; far_n=int(os.environ.get('FAR','6')); near_n=int(os.environ.get('NEAR','3'))
far=[''.join(('#' if x in (mid-1,mid,0,W-1) else (Q_BED if x<mid-1 else P_BED)) for x in range(W)) for _ in range(far_n)]
xl,xr=round(W*.25),round(W*.75)
pond=''.join('b' if x in (xl,xr) else '#' for x in range(W))
near=[''.join(('C' if x in (mid-1,mid) else (P_BED if x<mid-1 else Q_BED)) for x in range(W)) for _ in range(near_n)]
json.dump({'rows':rows+far+[pond]+near},sys.stdout)
