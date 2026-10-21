import json,re,collections,sys
d=json.load(open(sys.argv[1]))
def sents(t): return [s.strip() for s in re.split(r'(?<=[.!?])\s+|\n',t) if s.strip()]
norm=lambda s: re.sub(r'\d+','N',s.lower())
cnt=collections.Counter()
for r in d:
    for s in set(norm(s) for s in sents(r['text'])): cnt[s]+=1
boiler={s for s,n in cnt.items() if n>=4}
print(len(boiler))
for s,n in cnt.most_common(60): print(n, s[:150])
for r in d:
    keep=[s for s in sents(r['text']) if norm(s) not in boiler]
    # cut navigation/header: start from first sentence after header
    r['article']='\n'.join(keep)
    r['articleChars']=len(r['article'])
json.dump(d,open(sys.argv[2],'w'),indent=1)
