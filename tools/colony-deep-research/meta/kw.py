import sys,json,re,collections
rs={}
for f in sys.argv[2:]:
    for r in json.load(open(f)): rs[r['id']]=r
rs=list(rs.values())
pat=re.compile(sys.argv[1],re.I)
for r in sorted(rs,key=lambda r:-(r['thumbs'] or 0)):
    t=r['text'] or ''
    if pat.search(t):
        import datetime
        d=datetime.datetime.utcfromtimestamp(r['ts']).strftime('%Y-%m-%d')
        print(f"[{r['score']}* {d} v{r['ver']} +{r['thumbs']}] {t[:700]}")
