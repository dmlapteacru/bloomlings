import sys,json,re
rs={}
for f in sys.argv[1:]:
    for r in json.load(open(f)): rs[r['id']]=r
rs=[r for r in rs.values() if r['text']]
T={
 'ads (any)':r'\bad(s|vert)?\b|advert',
 'ad after every level / forced':r'(after|between) (every|each)|forced ad|ad after',
 'ads open browser/store unprompted':r'browser|chrome|opens? (up )?(the )?(play ?store|google play|website)|pop ?up',
 'coins scarce / boosters expensive':r'coins?|booster|power ?up|helper|perk|tool',
 'removed watch-ad option':r"(took|taken|removed?|no longer|can'?t|cannot).{0,40}(watch|ad)",
 'impossible / pay to win':r'impossible|unbeatable|pay to win|money grab|cash grab|forced to (pay|buy|spend)|without (spending|paying|buying)',
 'hard/super hard mention':r'\bhard\b',
 'super hard mention':r'super ?hard',
 'luck/rigged/cheat feel':r'luck|rigged|cheat|bypass|skip(s|ped)? (the )?colou?r|ignore',
 'lives/hearts':r'\blives\b|\blife\b|heart',
 'progress lost/reset':r'(lost|lose|reset|back to level|restart).{0,40}(progress|level)',
 'speed / 2x paid':r'2x|speed|slow',
 'similar colours':r'similar|shade|colou?r ?blind|hue',
 'relaxing/satisfying/cute':r'relax|satisf|cute|adorable|calm|asmr',
 'fun/love':r'\bfun\b|love',
 'repetitive levels':r'repet|repeat|same level',
 'too many updates':r'update',
}
n=len(rs)
for k,p in T.items():
    c=[r for r in rs if re.search(p,r['text'],re.I)]
    lo=sum(1 for r in c if r['score']<=2)
    print(f"{k:38s} {len(c):4d} {100*len(c)/n:5.1f}%  (1-2*: {lo})")
print('N',n)
