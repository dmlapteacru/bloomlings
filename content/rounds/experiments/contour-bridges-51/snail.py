# L75 «Улитка с домиком» (Насекомые и маленькие существа): a big garden snail crawling left with a spiral shell; the snail is
# outlined (17 §8 «Контур вместо прямой полосы»), two bridges on two sides (Super Hard, 17 §8: sites on different
# strips): up from the pebble path (X) onto its foot, and left from the strawberry (R) onto its shell.
import math,json,sys
sys.path.insert(0,'.')
from contour import finish
W,H=35,31
g=[['S']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,mask=None):
    for y in range(H):
        for x in range(W):
            if pred(x+.5,y+.5) and (mask is None or mask[y][x]): g[y][x]=c
fill(lambda x,y:y>=17+1.0*math.sin(x*.4),'G')                         # the lawn
SX,SY,SR=20.6,12.6,10.6
shell=lambda x,y:(x-SX)**2+(y-SY)**2<=SR*SR
foot=lambda x,y:3<=x<=31 and 21.5<=y<=25.6 and ((x-29)**2/9+(y-24)**2/3<=1 or x<=28.5)
head=lambda x,y:((x-5.8)/3.9)**2+((y-19.4)/4.2)**2<=1 or (2.6<=x<=8.5 and 19<=y<=25.6)
stalk=lambda x,y:(abs(x-(4.6-(16-y)*.3))<=1.05 and 10.4<=y<=17) or (abs(x-(7.2+(16-y)*.08))<=1.05 and 9.6<=y<=17)
eye=lambda x,y:(x-2.9)**2+(y-10.2)**2<=2.6 or (x-7.9)**2+(y-9.2)**2<=2.6
for y in range(H):
    for x in range(W):
        X,Y=x+.5,y+.5
        if shell(X,Y) or foot(X,Y) or head(X,Y) or stalk(X,Y) or eye(X,Y): far[y][x]=True
fill(lambda x,y:True,'Z',far)                                          # the sandy body
def spiral(x,y):
    r=math.hypot(x-SX,y-SY);t=math.atan2(y-SY,x-SX)
    return (r-1.9*t/(math.pi))%3.8
fill(lambda x,y:shell(x,y),'O',far)
fill(lambda x,y:shell(x,y) and spiral(x,y)<1.25,'E',far)               # the spiral groove
fill(lambda x,y:shell(x,y) and 1.25<=spiral(x,y)<2.1,'A',far)          # a lighter band beside it
fill(eye,'D',far)
fill(lambda x,y:3.4<=x<=6.6 and abs(y-(21.4+(x-5)**2*.12))<=.5,'D',far)   # a smile
rows,anchors=finish(g,far,[('bottom',16),('right',12)],fill='G')
rows=[list(r) for r in rows]
(ax1,ay1),(ax2,ay2)=anchors
for y in range(H):
    for x in range(W):
        if rows[y][x]=='G' and y>=ay1 and abs(x-ax1)<=1.4+(y-ay1)*.3: rows[y][x]='X'          # pebble path up to the foot
        if rows[y][x] in 'SG' and x>=ax2 and ((x-ax2-1.2)**2/3.2+(y-ay2-.6)**2/5.0)<=1: rows[y][x]='R'   # a strawberry at the shell
json.dump({'rows':[''.join(r) for r in rows]},sys.stdout)
