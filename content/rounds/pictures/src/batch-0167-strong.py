# Strong pictures for the last open positions of wave L21-L50 (17 §2: colours kept inside the subject): a mine cart full
# of gems (stones family, in place of the rejected geode) and a phoenix on its nest. python3 strong3.py <name> > <name>.json
import math,json,sys
def canvas(W,H,bg):
    g=[[bg]*W for _ in range(H)]
    def fill(pred,c,mask=None):
        for y in range(H):
            for x in range(W):
                if pred(x+.5,y+.5) and (mask is None or mask(x+.5,y+.5)): g[y][x]=c
    return g,fill
def cart():
    W,H=32,28;g,fill=canvas(W,H,'N')                                                      # the dark mine
    fill(lambda x,y:y>=24,'D')                                                            # the ground
    for x0 in range(0,W,4): fill(lambda x,y,x0=x0:x0<=x<x0+1.6 and 23.4<=y<=24.4,'E')    # sleepers
    fill(lambda x,y:abs(y-23.2)<=.5,'C')                                                  # the rail
    for px in (3,28):                                                                     # timber props and a beam
        fill(lambda x,y,px=px:abs(x-px)<=1.0 and 4<=y<=23,'E')
    fill(lambda x,y:1.5<=x<=29.5 and 3<=y<=5,'E')
    fill(lambda x,y:abs(x-7)<=.4 and 5<=y<=8,'C')                                         # a hanging lantern cord
    fill(lambda x,y:((x-7)/1.3)**2+((y-9.2)/1.5)**2<=1,'Y')                               # its glow
    cartb=lambda x,y:abs(x-16)<=9.5-(y-13)*.18 and 13<=y<=20.5
    fill(cartb,'A')                                                                       # the wooden cart
    fill(lambda x,y:cartb(x,y) and (abs(y-16.5)<=.5),'D')                                 # an iron band
    fill(lambda x,y:abs(x-16)<=8.6 and 8.5<=y<13,'C')                                     # a heap of gems above the rim
    gems=[(9.5,11,'R'),(12.5,9.6,'B'),(15.5,10.8,'V'),(18.5,9.4,'Y'),(21.5,11,'R'),(11,12.3,'R'),(14,12.4,'Y'),(17,12.4,'B'),(20,12.3,'V'),(23,12.4,'B'),(16,8.6,'R')]
    for gx,gy,c in gems: fill(lambda x,y,gx=gx,gy=gy:abs(x-gx)+abs(y-gy)*1.2<=1.7,c,lambda x,y:abs(x-16)<=9 and 7.5<=y<13)
    for wx in (10,22):
        fill(lambda x,y,wx=wx:(x-wx)**2+(y-21.6)**2<=5.0,'D')                             # wheels
        fill(lambda x,y,wx=wx:(x-wx)**2+(y-21.6)**2<=1.0,'C')
    return g
def phoenix():
    W,H=30,30;g,fill=canvas(W,H,'V')                                                      # a plum dusk sky
    fill(lambda x,y:((x-15)/6.5)**2+((y-11)/6.5)**2<=1,'K')                               # a pale moon behind
    nest=lambda x,y:((x-15)/11)**2+((y-24)/4.4)**2<=1 and y>=21
    fill(nest,'E')                                                                        # the twig nest
    for k in range(5): fill(lambda x,y,k=k:nest(x,y) and abs(y-(22+k*1.4)-(x-15)*.18*(1 if k%2 else -1))<=.45,'A')
    fill(lambda x,y:((x-15)/7.5)**2+((y-22)/1.6)**2<=1,'D')                               # the nest's dark hollow
    for ex in (11.5,15,18.5): fill(lambda x,y,ex=ex:((x-ex)/1.5)**2+((y-21.4)/1.9)**2<=1,'C')   # eggs
    body=lambda x,y:((x-15)/3.6)**2+((y-14.5)/5.0)**2<=1
    fill(body,'R')                                                                        # the bird's body
    fill(lambda x,y:((x-15)/2.0)**2+((y-15.5)/3.2)**2<=1,'O')                             # a flame-coloured breast
    fill(lambda x,y:((x-15)/2.2)**2+((y-8.4)/2.2)**2<=1,'R')                              # the head
    fill(lambda x,y:abs(x-17.6)<=.9-(x-16.8)*.0 and abs(y-8.6)<=.5,'Y')                   # the beak
    g[8][15]='D'                                                                          # the eye
    for s in (-1,1):                                                                      # wings of fire
        for k in range(4):
            fill(lambda x,y,s=s,k=k:abs((y-(13+k*1.6))+(x-15)*s*.55)<=.75 and 3.5<=s*(x-15)<=10.5-k*1.3,'OYRO'[k])
    for k,c in enumerate('YOY'):                                                           # a crest of flames
        fill(lambda x,y,k=k,c=c:abs(x-(13.6+k*1.4))<=.5 and 4.4+(k%2)*.8<=y<=6.6,c)
    return g
f={'cart':cart,'phoenix':phoenix}[sys.argv[1]]
json.dump({'rows':[''.join(r) for r in f()]},sys.stdout)
