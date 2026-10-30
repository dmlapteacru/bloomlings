# Night fair, one whole picture where the divider is a box to start from (the owner, 2026-10-30: «не просто простую линию,
# а какую-то обводку вокруг или где-то ещё, или начинать с какого-то бокса конкретно … разные контуры, разные ловушки,
# разные проходы»): the team comes in through the gate into the entrance plaza (a wooden boardwalk with the ticket booth),
# fenced by a neutral line (the divider, a gap strip) that meets the bottom edge on both sides, so the gate opens into
# the plaza only. Two exits are bridges: over the top fence onto the fairground (it decides) and over the left fence into
# the hedge, whose pods come last. Beyond: the Ferris wheel, the big top, the night sky.
import math,json,sys
W,H=30,32
g=[['N']*W for _ in range(H)]
def put(x,y,c):
    if 0<=x<W and 0<=y<H: g[y][x]=c
def fill(pred,c,y0=0,y1=H):
    for y in range(y0,y1):
        for x in range(W):
            if pred(x+.5,y+.5): put(x,y,c)
# moon and stars
fill(lambda x,y:(x-26.5)**2+(y-2.6)**2<=4.0,'C',0,7)
fill(lambda x,y:(x-27.4)**2+(y-2.0)**2<=2.2,'N',0,7)
for (x,y) in [(14,1),(19,3),(2,1),(22,0),(12,4),(29,7)]: put(x,y,'Y')
# the fairground
fill(lambda x,y:y>=18.5,'D')
# Ferris wheel (left)
cx,cy,R=8.0,9.6,7.0
fill(lambda x,y:R-1.05<=math.hypot(x-cx,y-cy)<=R,'Y',0,18)
for (dx,dy) in [(1,0),(0,1),(1,1),(1,-1)]:
    for t in range(-7,8):
        x,y=cx-.5+dx*t,cy-.5+dy*t
        if math.hypot(x+.5-cx,y+.5-cy)<R-1.1: put(int(round(x)),int(round(y)),'C')
fill(lambda x,y:math.hypot(x-cx,y-cy)<=1.3,'Y',0,18)
cab=['R','O','R','O','R','O','R','O']
for k in range(8):
    a=k*math.pi/4
    x0,y0=cx+R*math.cos(a),cy+R*math.sin(a)
    fill(lambda x,y,x0=x0,y0=y0:abs(x-x0)<=1.1 and 0<=y-y0+.2<=2.0,cab[k],0,18)
for y in range(10,19):
    s=(y-9.6)*0.6
    put(int(cx-s),y,'D'); put(int(cx+s),y,'D')
# big top (right)
tx,ay,eave=22.5,4.4,11.5
stripe=lambda xb:'R' if int(math.floor((xb-tx+1)/2))%2==0 else 'C'
for y in range(12,19):
    for x in range(16,29): put(x,y,stripe(x+.5))
for y in range(5,12):
    half=(y+.5-ay)/(eave-ay)*6.5
    for x in range(W):
        d=x+.5-tx
        if abs(d)<=half: put(x,y,stripe(tx+d*(eave-ay)/(y+.5-ay)))
for y in range(14,19):
    half=(y-13.5)*0.7
    for x in range(W):
        if abs(x+.5-tx)<=half: put(x,y,'N')
for y in range(1,5): put(22,y,'D')
for (x,y) in [(23,1),(24,1),(23,2)]: put(x,y,'O')
# hedges in both bottom corners
fill(lambda x,y:x<10.5 and y>=21.6-0.8*math.sin(x*.9),'M',19,H)
fill(lambda x,y:x>=20.5 and y>=21.8-0.8*math.sin(x*.9+1),'M',19,H)
# the entrance plaza: a wooden boardwalk with the ticket booth; its fence (the divider) meets the bottom edge on both sides
bx0,bx1,by0=11,19,24                      # inside x 11..19, rows 24..31
for y in range(by0,H):
    for x in range(bx0,bx1+1): put(x,y,'E')
for y in range(25,30):
    for x in range(11,14): put(x,y,'K')    # the ticket booth by the left fence
for y in range(by0-1,H): put(bx0-1,y,'#'); put(bx1+1,y,'#')
for x in range(bx0-1,bx1+2): put(x,by0-1,'#')
put(17,by0-1,'b')                         # over the top fence onto the fairground
put(bx0-1,27,'b')                         # over the left fence into the hedge
json.dump({'rows':[''.join(r) for r in g]},sys.stdout)
