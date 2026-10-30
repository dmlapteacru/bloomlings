# L33 «Черепаха-остров» (Морские и пресноводные обитатели, motif: черепахи): a giant sea turtle swimming left with a tiny
# island on its shell — a sand mound and a palm tree. The turtle with its island is outlined (17 §8); the bridges are
# planks from the right, from a pink coral reef onto the shell and from a sandbank onto the rear flipper (both cross left).
import math,json,sys
sys.path.insert(0,'.')
from contour import finish,edge_site
W,H=32,28
g=[['S']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,mask=None,y0=0,y1=None):
    for y in range(y0,y1 or H):
        for x in range(W):
            if pred(x+.5,y+.5) and (mask is None or mask[y][x]): g[y][x]=c
fill(lambda x,y:y>=7.5+.5*math.sin(x*.9),'B')
# far: the turtle (shell, head, flippers, tail) and its island (sand, palm)
shell=lambda x,y:((x-16)/9.2)**2+((y-15.5)/6.2)**2<=1
head=lambda x,y:((x-5.2)/2.8)**2+((y-15.2)/2.3)**2<=1
neck=lambda x,y:5<=x<=8.5 and 13.8<=y<=16.6
fl=lambda cx,cy,rx,ry,a:(lambda x,y:(((x-cx)*math.cos(a)+(y-cy)*math.sin(a))/rx)**2+((-(x-cx)*math.sin(a)+(y-cy)*math.cos(a))/ry)**2<=1)
flippers=[fl(9.0,22.0,3.6,1.5,.5),fl(9.0,9.2,3.6,1.5,-.5),fl(23.2,21.4,2.8,1.3,-.5),fl(23.2,9.8,2.8,1.3,.5)]
tail=lambda x,y:25<=x<=27.4 and abs(y-15.5)<=(27.4-x)*.6
sand=lambda x,y:((x-16.5)/4.6)**2+((y-9.6)/1.8)**2<=1 and y<=10.6
trunk=lambda x,y:abs(x-(17.4+(9-y)*.3))<=1.0 and 3.5<=y<=9.5
leaves=lambda x,y:any(((x-cx)/rx)**2+((y-cy)/ry)**2<=1 for cx,cy,rx,ry in [(17.0,3.4,2.8,1.0),(21.6,3.6,2.6,1.0),(19.4,2.6,1.6,1.2),(18.0,4.6,1.6,.9),(21.0,4.8,1.6,.9)])
for y in range(H):
    for x in range(W):
        X,Y=x+.5,y+.5
        if shell(X,Y) or head(X,Y) or neck(X,Y) or tail(X,Y) or any(f(X,Y) for f in flippers) or sand(X,Y) or trunk(X,Y) or leaves(X,Y): far[y][x]=True
fill(lambda x,y:True,'L',far)
fill(lambda x,y:shell(x,y),'M',far)
# shell plates: lighter green hexes on the dark shell
for (cx,cy) in [(12,13),(16,12.5),(20,13),(10,17),(14,17.2),(18,17.2),(22,17)]:
    fill(lambda x,y,cx=cx,cy=cy:abs(x-cx)+abs(y-cy)*1.3<=1.6 and shell(x,y),'G',far)
fill(lambda x,y:sand(x,y),'Y',far)
fill(lambda x,y:trunk(x,y),'E',far)
fill(lambda x,y:leaves(x,y),'G',far)
# eye and smile
for (x,y) in [(4,14),(3,16),(4,16)]: g[y][x]='M'
rows,anchors=finish(g,far,[('right',12),('right',20)],fill=None)
# approaches: a pink coral reef round the upper anchor, a sandbank round the lower one
rows=[list(r) for r in rows]
(ax1,ay1),(ax2,ay2)=anchors
for y in range(H):
    for x in range(W):
        if rows[y][x]=='B' and x>=ax1 and abs(y-ay1)<=1+(x-ax1)*.5 and y<=15: rows[y][x]='K'
        if rows[y][x]=='B' and x>=ax2 and abs(y-ay2)<=1+(x-ax2)*.5 and y>=16: rows[y][x]='A'
json.dump({'rows':[''.join(r) for r in rows]},sys.stdout)
