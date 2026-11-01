# L85 «Паровозик» (Наземный транспорт): a small red steam engine with a navy cab, puffing white smoke; the engine is
# outlined (17 §8 «Контур вместо прямой полосы»), both bridges cross left onto its cab from the right: from the sandy
# station platform (Z) onto the cab, and from the earth heap (D) onto its red lower panel.
import math,json,sys
sys.path.insert(0,'.')
from contour import finish
W,H=33,29
g=[['S']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,mask=None):
    for y in range(H):
        for x in range(W):
            if pred(x+.5,y+.5) and (mask is None or mask[y][x]): g[y][x]=c
fill(lambda x,y:y>=25.6,'G')
# smoke puffs over the chimney (outside the outline, part of the sky's work)
for (cx,cy,r) in [(9.5,2.6,2.2),(13.6,1.8,1.8),(5.6,2.4,1.5)]:
    fill(lambda x,y,cx=cx,cy=cy,r=r:(x-cx)**2+(y-cy)**2<=r*r,'W')
boiler=lambda x,y:3<=x<=20 and 11.5<=y<=20.5
front=lambda x,y:1.4<=x<=4 and 12<=y<=20.5
chimney=lambda x,y:(7<=x<=10 and 7<=y<=12) or (6.2<=x<=10.8 and 6.2<=y<=7.6)
dome=lambda x,y:((x-14)/2.2)**2+((y-11.4)/1.6)**2<=1
cab=lambda x,y:19.5<=x<=28.6 and 6<=y<=22.5
roof=lambda x,y:18.6<=x<=29.4 and 5<=y<=6.8
frame=lambda x,y:1<=x<=28.6 and 20<=y<=22.5
catcher=lambda x,y:y>=20 and y<=24.4 and x>=.6+(24.4-y)*.0 and x<=4.2-(y-20)*.6 and x>=1.0
wheels=[(7.2,23.6,2.9),(14.2,23.6,2.9),(23.6,23.4,3.2)]
wheel=lambda x,y:any((x-cx)**2+(y-cy)**2<=r*r for cx,cy,r in wheels)
for y in range(H):
    for x in range(W):
        X,Y=x+.5,y+.5
        if boiler(X,Y) or front(X,Y) or chimney(X,Y) or dome(X,Y) or cab(X,Y) or roof(X,Y) or frame(X,Y) or wheel(X,Y): far[y][x]=True
fill(lambda x,y:boiler(x,y) or dome(x,y),'R',far)
fill(lambda x,y:boiler(x,y) and (abs(x-8)<=.6 or abs(x-12.5)<=.6 or abs(x-17)<=.6),'Y',far)   # gold bands
fill(lambda x,y:dome(x,y),'Y',far)
fill(lambda x,y:front(x,y) or chimney(x,y),'N',far)
fill(lambda x,y:cab(x,y),'N',far)
fill(lambda x,y:cab(x,y) and y>=15.5,'R',far)                     # the cab's red lower panel
fill(lambda x,y:21.5<=x<=26.6 and 8<=y<=12.6,'Y',far)             # the window
fill(roof,'D',far)
fill(lambda x,y:frame(x,y),'N',far)
fill(wheel,'J',far)
for cx,cy,r in wheels: fill(lambda x,y,cx=cx,cy=cy:(x-cx)**2+(y-cy)**2<=1.1,'Y',far)
fill(lambda x,y:2.2<=x<=3.6 and 13.2<=y<=14.6,'Y',far)            # the lamp
for y in range(H): g[y].insert(0,'S' if y<25 else 'G'); far[y].insert(0,False)
W+=1     # a column of sky before the engine's front, so the outline closes round it
rows,anchors=finish(g,far,[('right',9),('right',19)],fill='S')
rows=[list(r) for r in rows]
(ax1,ay1),(ax2,ay2)=anchors
for y in range(H):
    for x in range(W):
        if rows[y][x] in 'SG' and x>=ax1 and ay1-1<=y<=ay1+3: rows[y][x]='Z'                        # the platform
        if rows[y][x] in 'SG' and x>=ax2 and ((x-ax2-2.2)**2/7+(y-ay2-1.8)**2/7)<=1: rows[y][x]='D'  # the earth heap
json.dump({'rows':[''.join(r) for r in rows]},sys.stdout)
