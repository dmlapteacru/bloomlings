import sys,json,re,collections
rows=[]
for f in sys.argv[1:]:
    rows+=json.load(open(f))
lab=collections.defaultdict(set)
for r in rows:
    t=r['title']
    m=re.search(r'Level\s*[-–#]?\s*(\d+)',t,re.I) or re.search(r'#(\d+)',t)
    if not m: continue
    n=int(m.group(1))
    if re.search(r'super\s*hard',t,re.I): l='SH'
    elif re.search(r'\bhard\b',t,re.I): l='H'
    else: l='N'
    lab[n].add(l)
levels=sorted(lab)
print('levels covered',len(levels),'max',max(levels))
conf=[n for n in levels if len(lab[n]-{'N'})>1]
print('conflicts H vs SH',conf)
def L(n):
    s=lab[n]
    return 'SH' if 'SH' in s else 'H' if 'H' in s else 'N'
for lo,hi in [(1,100),(101,200),(201,300),(301,400),(401,600),(601,1000),(1001,1400)]:
    sh=[n for n in levels if lo<=n<=hi and L(n)=='SH']
    h=[n for n in levels if lo<=n<=hi and L(n)=='H']
    cov=len([n for n in levels if lo<=n<=hi])
    print(f'{lo}-{hi} covered={cov} SH={sh}\n   H={h}')
sh=[n for n in levels if L(n)=='SH']; h=[n for n in levels if L(n)=='H']
print('SH mod10', collections.Counter(n%10 for n in sh)); print('H mod10',collections.Counter(n%10 for n in h))
print('SH mod 5',collections.Counter(n%5 for n in sh))
