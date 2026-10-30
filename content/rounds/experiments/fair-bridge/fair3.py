# Night fair beyond a canal, drawn as one scene (the owner, 2026-10-30: «часть за мостом … частью общей картины … одно
# большое целое»): the pocket recipe stays, mirrored so the left bridge decides (left blush, right moss behind the canal,
# crossed with the near beds; closed at the picture's edges, split by a divider, open only upward), but drawn as the fair:
# the divider is a canal running through the fairground into the basin, the blush is a row of little pink stall tents,
# the moss a clipped hedge, the basin widens between the two bridges, and the hedge grows at the big top's foot too.
import math,json,sys
W,H=32,32
g=[['N']*W for _ in range(H)]
def put(x,y,c):
    if 0<=x<W and 0<=y<H: g[y][x]=c
def fill(pred,c,y0=0,y1=H):
    for y in range(y0,y1):
        for x in range(W):
            if pred(x+.5,y+.5): put(x,y,c)
# moon and stars
fill(lambda x,y:(x-28)**2+(y-2.6)**2<=4.2,'C',0,8)
fill(lambda x,y:(x-29)**2+(y-2.0)**2<=2.2,'N',0,8)
for (x,y) in [(17,1),(21,3),(1,1),(24,0),(15,4),(31,8)]: put(x,y,'Y')
# fairground from y 17
fill(lambda x,y:y>=17,'D',0,21)
# Ferris wheel
cx,cy,R=9.5,8.6,7.2
fill(lambda x,y:R-1.05<=math.hypot(x-cx,y-cy)<=R,'Y',0,17)
for (dx,dy) in [(1,0),(0,1),(1,1),(1,-1)]:
    for t in range(-7,8):
        x,y=cx-.5+dx*t,cy-.5+dy*t
        if math.hypot(x+.5-cx,y+.5-cy)<R-1.1: put(int(round(x)),int(round(y)),'C')
fill(lambda x,y:math.hypot(x-cx,y-cy)<=1.3,'Y',0,17)
cab=['R','B','O','B','R','O','B','O']
for k in range(8):
    a=k*math.pi/4
    x0,y0=cx+R*math.cos(a),cy+R*math.sin(a)
    fill(lambda x,y,x0=x0,y0=y0:abs(x-x0)<=1.1 and 0<=y-y0+.2<=2.0,cab[k],0,17)
for y in range(9,17):
    s=(y-8.6)*0.62
    put(int(cx-s),y,'D'); put(int(cx+s),y,'D')
# big top
tx,ay,eave=24.0,4.6,11.5
stripe=lambda xb:'R' if int(math.floor((xb-tx+1)/2))%2==0 else 'C'
for y in range(12,17):
    for x in range(18,31): put(x,y,stripe(x+.5))
for y in range(5,12):
    half=(y+.5-ay)/(eave-ay)*6.5
    for x in range(W):
        d=x+.5-tx
        if abs(d)<=half: put(x,y,stripe(tx+d*(eave-ay)/(y+.5-ay)))
for y in range(13,17):
    half=(y-12.5)*0.75
    for x in range(W):
        if abs(x+.5-tx)<=half: put(x,y,'N')
for y in range(1,5): put(23,y,'D')
for (x,y) in [(24,1),(25,1),(24,2)]: put(x,y,'O')
for x in range(17,21):
    y=7+int(round(((x-18.5)/2.5)**2))
    put(x,y,'B' if x%2 else 'O')
# hedges at the big top's foot
fill(lambda x,y:((x-19)/1.6)**2+((y-16.8)/1.0)**2<=1,'M',15,18)
fill(lambda x,y:((x-29.8)/1.8)**2+((y-16.8)/1.0)**2<=1,'M',15,18)
# --- the pocket zone, rows 19-27: pink stall tents (left), a clipped hedge (right), the canal between them
def tents(x,y):
    for c in (3.5,8.0,12.5):
        if abs(x-c)<=2.3 and y>=18.4+abs(x-c)*1.1: return True   # a pointed roof over the stall
    return y>=21.4
for y in range(19,28):
    for x in range(1,15):
        if tents(x+.5,y+.5): put(x,y,'K')
    for x in range(17,31):
        if y+.5>=18.9+2.5*abs(math.sin((x+.5-16.75)*math.pi/3.5)): put(x,y,"M")
# stall doors: cream stripes under each roof
for c in (3,8,12):
    for y in range(22,25): put(c,y,'C')
# the canal through the fairground into the basin, and the basin's edges round the far bank
for y in range(17,28):
    for x in (15,16): put(x,y,'#')
for y in range(19,28):
    put(0,y,'#'); put(W-1,y,'#')
for x in range(W): put(x,28,'#')
for x in range(11,21): put(x,27,'#')
for x in range(12,20): put(x,29,'#')
put(8,28,'b'); put(24,28,'b')
# near bank: moss beds (left), blush beds (right), the cream path from the gate
for y in range(29,32):
    for x in range(W):
        if g[y][x]=='#': continue
        put(x,y,'C' if x in (15,16) else ('M' if x<15 else 'K'))
json.dump({'rows':[''.join(r) for r in g]},sys.stdout)
