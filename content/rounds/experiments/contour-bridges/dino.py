# L43 «Диплодок в озере» (Доисторический мир, motif: динозавр): a long-necked dinosaur standing in a lake, facing right,
# its long tail reaching the left bank, a dark volcano smoking behind. The dinosaur is outlined (17 §8); the bridges cross right from the left bank: one
# from the ferns onto the tip of its tail, one from a rocky ledge onto its back.
import math,json,sys
sys.path.insert(0,'.')
from contour import finish
W,H=34,28
g=[['S']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,mask=None):
    for y in range(H):
        for x in range(W):
            if pred(x+.5,y+.5) and (mask is None or mask[y][x]): g[y][x]=c
# near: the lake, the left bank (sand), a rocky ledge and ferns on it
fill(lambda x,y:y>=17.5+.4*math.sin(x*.8),'B')
fill(lambda x,y:x<=4.2+1.2*math.sin(y*.5) and y>=9,'A')
fill(lambda x,y:x<=10.4 and 11<=y<=14.6,'E')   # the rocky ledge reaches the dinosaur's back
fill(lambda x,y:x<=5.2 and 18<=y<=24,'L')
# a dark smoking volcano far behind the dinosaur's back, glowing lava in its crater and down one flank
fill(lambda x,y:3.8<=y<11 and abs(x-15)<=1.4+(y-3.8)*.85,'D')
fill(lambda x,y:3.8<=y<5.0 and abs(x-15)<=1.4+(y-3.8)*.85,'O')
fill(lambda x,y:5<=y<9.2 and abs(x-(15.6+(y-5)*.55))<=.55,'O')
fill(lambda x,y:((x-14.0)/1.7)**2+((y-2.2)/.9)**2<=1 or ((x-16.6)/1.9)**2+((y-0.9)/1.0)**2<=1,'C')
# far: the dinosaur
body=lambda x,y:((x-18)/7.2)**2+((y-14.6)/4.0)**2<=1
neck=lambda x,y:22<=x<=28.2 and abs(y-(13.2-(x-22)*1.45))<=1.5
head=lambda x,y:((x-28.2)/2.5)**2+((y-3.8)/1.5)**2<=1
tail=lambda x,y:4.4<=x<=12 and abs(y-(15.5+(12-x)*.42))<=.6+(x-4.4)*.18
legs=lambda x,y:any(abs(x-lx)<=1.1 and 16<=y<=21.5 for lx in (13.5,16.5,20,23))
for y in range(H):
    for x in range(W):
        X,Y=x+.5,y+.5
        if body(X,Y) or neck(X,Y) or head(X,Y) or tail(X,Y) or legs(X,Y): far[y][x]=True
fill(lambda x,y:True,'G',far)
fill(lambda x,y:body(x,y) and y>=15.6,'C',far)                      # pale belly
fill(lambda x,y:tail(x,y) and not body(x,y) and (y>=15.5+(12-x)*.42-.1 or x<=6.2),'C',far)   # and the tail's underside, pale to the tip
g[4][29]='D'                                                       # the eye
rows,anchors=finish(g,far,[('left',18),('left',13)],fill=None)
json.dump({'rows':rows},sys.stdout)
