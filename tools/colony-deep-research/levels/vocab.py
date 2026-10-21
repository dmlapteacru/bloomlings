import json,re,collections,sys
d=json.load(open(sys.argv[1]))
c=collections.Counter(); lv=collections.defaultdict(set)
cap=collections.Counter()
for r in d:
    t=r['text']
    words=re.findall(r"[a-z]+",t.lower())
    for a,b in zip(words,words[1:]):
        c[a+' '+b]+=1; lv[a+' '+b].add(r['level'])
    for m in re.findall(r"\b([A-Z][a-z]+(?: [A-Z][a-z]+)+)\b",t):
        cap[m]+=1
keys=['block','tile','door','chocolate','key','lock','mystery','question','hidden','linked','connected','chain','counter','ice','bomb','conveyor','portal','wall','box','crate','barrier','frozen','hard','tricky','difficult','easy','boss','skull','pipe','arrow','switch','coin','star','egg','rock','stone','vine','gate','cage','chest','jar','glass','magnet','tunnel','rope','bridge','paw','gift']
for k in keys:
    top=[(bg,n,len(lv[bg])) for bg,n in c.most_common() if k in bg.split()][:12]
    print(k,'::',', '.join(f'{bg}({n}/{l})' for bg,n,l in top))
print('CAPS', [x for x in cap.most_common(150)])
