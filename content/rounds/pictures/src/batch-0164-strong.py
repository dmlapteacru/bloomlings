# More strong pictures for the open positions of wave L21-L50 (colours kept inside the subject, 17 §2): a mushroom
# terrarium, an ice-cream van, a fireplace. python3 strong2.py <name> > <name>.json
import math,json,sys
def canvas(W,H,bg):
    g=[[bg]*W for _ in range(H)]
    def fill(pred,c,mask=None):
        for y in range(H):
            for x in range(W):
                if pred(x+.5,y+.5) and (mask is None or mask(x+.5,y+.5)): g[y][x]=c
    return g,fill
def terrarium():
    W,H=28,32;g,fill=canvas(W,H,'K')
    fill(lambda x,y:y>=28,'E')                                                            # the table
    jar=lambda x,y:(((x-14)/11)**2+((y-17.5)/10)**2<=1 and y<=27.5) or (abs(x-14)<=6 and 5<=y<=9)
    fill(jar,'C')                                                                         # the glass rim
    glass=lambda x,y:(((x-14)/10)**2+((y-17.5)/9)**2<=1 and y<=26.6) or (abs(x-14)<=5 and 5<=y<=9)
    fill(glass,'S')                                                                       # the air inside, tinted
    fill(lambda x,y:abs(x-14)<=6.8 and 3<=y<5,'A')                                        # the cork lid
    inside=glass
    fill(lambda x,y:y>=22.5+.8*math.sin(x*.7),'M',inside)                                 # moss
    fill(lambda x,y:y>=25.2,'E',inside)                                                   # soil
    fill(lambda x,y:abs(x-13)<=1.3 and 15<=y<=23.5,'C',inside)                            # the stem
    fill(lambda x,y:abs(x-13)<=2.1 and abs(y-18.6)<=.5,'C',inside)                        # its ring
    fill(lambda x,y:((x-13)/7.0)**2+((y-15.4)/4.4)**2<=1 and y<=15.6,'R',inside)          # the red cap
    for (sx,sy) in [(9.8,13.4),(13,12.0),(16.2,13.6)]:
        fill(lambda x,y,sx=sx,sy=sy:(x-sx)**2+(y-sy)**2<=.8,'C',inside)                   # white spots
    fill(lambda x,y:abs(x-19.6)<=.6 and 19.5<=y<=23.5,'C',inside)                         # a small one
    fill(lambda x,y:((x-19.6)/2.4)**2+((y-19.8)/1.6)**2<=1 and y<=19.9,'R',inside)
    for (bx,by) in [(9,24),(17,24),(22,23)]:
        fill(lambda x,y,bx=bx,by=by:(x-bx)**2+(y-by)**2<=1.0,'B',inside)                  # blueberries
    fill(lambda x,y:abs(x-6.5)<=.5 and 16<=y<=22.5,'G',inside)                            # a fern
    for k in range(4): fill(lambda x,y,k=k:abs(y-(17+k*1.3)-(x-6.5)*.5)<=.5 and 6.5<=x<=8.6-k*.3,'G',inside)
    for k in range(4): fill(lambda x,y,k=k:abs(y-(17+k*1.3)+(x-6.5)*.5)<=.5 and 4.4+k*.3<=x<=6.5,'G',inside)
    return g
def van():
    W,H=34,26;g,fill=canvas(W,H,'S')
    fill(lambda x,y:y>=21,'D')                                                            # the road
    fill(lambda x,y:abs(y-23.5)<=.5 and int(x)%5<3,'C')                                   # road marks
    body=lambda x,y:3<=x<=30 and 7<=y<=20 and not (x>24 and y<11.5-(x-24)*.0 and x>25.5) 
    fill(lambda x,y:3<=x<=24.5 and 6<=y<=19.5,'P')                                        # the box (pink)
    fill(lambda x,y:24.5<x<=30.5 and 10<=y<=19.5,'P')                                     # the cab
    fill(lambda x,y:3<=x<=30.5 and 16.5<=y<=19.5,'V')                                     # a plum stripe
    fill(lambda x,y:5<=x<=19 and 8<=y<=14.5,'C')                                          # the serving window
    for i,c in enumerate('YRVR'):
        cx=7+i*3.6
        fill(lambda x,y,cx=cx:((x-cx)/1.3)**2+((y-10.2)/1.3)**2<=1,c)                     # scoops of ice cream
        fill(lambda x,y,cx=cx:abs(x-cx)<=1.2-(y-11.5)*.4 and 11.5<=y<=14.4,'O')            # cones
    fill(lambda x,y:26<=x<=29.5 and 11<=y<=14,'S')                                        # the cab window
    fill(lambda x,y:4<=x<=20 and 4<=y<6,'V')                                              # the roof sign
    fill(lambda x,y:((x-12)/2.4)**2+((y-2.6)/1.8)**2<=1,'Y')                              # a big cone on the roof
    fill(lambda x,y:abs(x-12)<=1.6-(y-3.6)*.7 and 3.6<=y<=5.4,'O')
    for wx in (9,25):
        fill(lambda x,y,wx=wx:(x-wx)**2+(y-20)**2<=7.3,'N')                               # wheels
        fill(lambda x,y,wx=wx:(x-wx)**2+(y-20)**2<=1.6,'C')                               # hubs
    return g
def fireplace():
    W,H=30,30;g,fill=canvas(W,H,'A')
    for y in range(H):                                                                    # a striped wallpaper
        for x in range(W):
            if x%6==0: g[y][x]='O'
    fill(lambda x,y:y>=26,'E')                                                            # the floor
    fill(lambda x,y:4<=x<=26 and 9<=y<=26,'R')                                            # the brick surround
    for y in range(9,26):
        for x in range(4,27):
            if (y%3==0) or ((x+(y//3)*3)%6==0): g[y][x]='K'                               # mortar lines
    fill(lambda x,y:2.5<=x<=27.5 and 7.5<=y<=9.5,'C')                                     # the mantel shelf
    fill(lambda x,y:9<=x<=21 and 13<=y<=26 and not (y<15.5 and abs(x-15)>6-(15.5-y)*2.2),'N')   # the dark hearth
    fill(lambda x,y:9.5<=x<=20.5 and abs(y-24.6)<=.9,'E',lambda x,y:g[int(y)][int(x)]=='N')    # logs
    fill(lambda x,y:abs(x-15)<=4.6-(23.5-y)*.55 and 16<=y<=23.6,'O',lambda x,y:g[int(y)][int(x)]=='N')  # flames
    fill(lambda x,y:abs(x-15)<=2.6-(23.5-y)*.45 and 18<=y<=23.6,'Y',lambda x,y:g[int(y)][int(x)]=='O')
    fill(lambda x,y:((x-15)/3.2)**2+((y-4.6)/3.0)**2<=1,'E')                              # a clock on the mantel
    fill(lambda x,y:((x-15)/2.2)**2+((y-4.6)/2.0)**2<=1,'C')
    fill(lambda x,y:abs(x-15)<=.4 and 3.2<=y<=4.8 or (15<=x<=16.4 and abs(y-4.7)<=.4),'N')
    for cx in (6,24):                                                                     # candles
        fill(lambda x,y,cx=cx:abs(x-cx)<=.9 and 3.6<=y<=7.5,'C')
        fill(lambda x,y,cx=cx:((x-cx)/.7)**2+((y-2.6)/1.0)**2<=1,'Y')
    return g
f={'terrarium':terrarium,'van':van,'fireplace':fireplace}[sys.argv[1]]
json.dump({'rows':[''.join(r) for r in f()]},sys.stdout)
