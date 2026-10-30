# A big pagoda on an island, one whole picture (the owner, 2026-10-30: «концептуально одна большая картина, имеющая смысл,
# и просто с разделителем»): the subject (the island with its pagoda and pine grove) is outlined by a neutral line, the
# divider; round it are the sky, the pond, the near shore and a sakura on the left bank. Two short approaches lead to the
# bridges: the near shore to the bottom one, the sakura's crown to the left one (as the garden's beds did). The subject
# touches no edge (nor does its line), so there is no way round the bridges; the queue alone makes the bottom bridge decide: it lands on the
# beach before the steps, the left one in the pine grove, whose pods come late; the sky and the pond are finished last.
import math,json,sys
W,H=30,32
g=[['S']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
for y in range(H):
    for x in range(W):
        X,Y=x+.5,y+.5
        if Y>=18.6: g[y][x]='B'
        if Y>=30.0+.4*math.sin(x*.8): g[y][x]='G'
# the sakura on the left bank (near region): its crown reaches the left bridge
for (cx,cy,r) in [(2.6,12.6,2.4),(4.0,15.4,2.5),(2.2,17.6,2.4),(4.4,18.6,2.0)]:
    for y in range(H):
        for x in range(W):
            if (x+.5-cx)**2+(y+.5-cy)**2<=r*r: g[y][x]='P'
for y in range(20,30):
    for x in (2,3): g[y][x]='D'
xc=18.5
isl=lambda x,y:((x-17.4)/10.4)**2+((y-25.0)/4.0)**2<=1
tiers=[(5,8,2.8),(9,13,4.6),(14,21,6.2)]
roof=lambda x,y,y0,h:int(y)==y0 and abs(x-xc)<=h+1.6
body=lambda x,y,y0,y1,h:y0<int(y)<=y1 and abs(x-xc)<=h
spire=lambda x,y:int(x)==18 and 2<=int(y)<=4
pines=[(10.0,11.4,2.0),(9.2,14.2,2.4),(11.6,13.0,2.1),(9.0,17.6,2.5),(11.8,16.2,2.5),(9.2,21.0,2.5),(12.2,20.0,2.5),(10.8,23.0,2.2)]
pine=lambda x,y:any((x-cx)**2+((y-cy)*1.15)**2<=r*r for cx,cy,r in pines)
for y in range(H):
    for x in range(W):
        X,Y=x+.5,y+.5
        if isl(X,Y) or spire(X,Y) or pine(X,Y) or any(roof(X,Y,y0,h) or body(X,Y,y0,y1,h) for y0,y1,h in tiers): far[y][x]=True
for y in range(H):
    for x in range(W):
        if not far[y][x]: continue
        X,Y=x+.5,y+.5
        c='L'
        if pine(X,Y): c='M'
        for y0,y1,h in tiers:
            if body(X,Y,y0,y1,h): c='R'
            if roof(X,Y,y0,h): c='D'
        if spire(X,Y): c='Y'
        g[y][x]=c
for (x,y) in [(17,6),(20,6),(16,10),(19,10),(22,10),(15,16),(17,16),(21,16),(23,16)]: g[y][x]='Y'
for y in range(18,22): g[y][19]='D'
line=[[not far[y][x] and any(0<=x+dx<W and 0<=y+dy<H and far[y+dy][x+dx] for dx,dy in ((1,0),(-1,0),(0,1),(0,-1))) for x in range(W)] for y in range(H)]
for y in range(H):
    for x in range(W):
        if line[y][x]: g[y][x]='#'
bx=24; by=max(y for y in range(H) if far[y][bx])+1; g[by][bx]='b'
ly=18; lx=min(x for x in range(W) if far[ly][x])-1; g[ly][lx]='b'
for (sx,sy,ax,ay) in [(bx,by,bx,by+1),(lx,ly,lx-1,ly)]:
    for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
        x,y=sx+dx,sy+dy
        if (x,y)!=(ax,ay) and 0<=x<W and 0<=y<H and not far[y][x]: g[y][x]='#'
# a spit of the near shore reaches up to the bottom bridge (its approach is the shore, as the left one's is the sakura)
for y in range(by+1,H):
    for x in range(bx-2,bx+3):
        if g[y][x]=='B': g[y][x]='G'
json.dump({'rows':[''.join(r) for r in g]},sys.stdout)
