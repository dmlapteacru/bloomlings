import sys,re,json
s=open(sys.argv[1],encoding='utf-8',errors='replace').read()
m=re.search(r'var ytInitialData = (\{.*?\});</script>',s,re.S)
if not m: print('no data'); sys.exit()
d=json.loads(m.group(1))
out=[]
def walk(o):
    if isinstance(o,dict):
        if 'videoRenderer' in o:
            v=o['videoRenderer']
            t=''.join(r.get('text','') for r in v.get('title',{}).get('runs',[]))
            ch=''.join(r.get('text','') for r in v.get('ownerText',{}).get('runs',[]))
            pub=v.get('publishedTimeText',{}).get('simpleText','')
            vc=v.get('viewCountText',{}).get('simpleText','')
            desc=''.join(r.get('text','') for sn in v.get('detailedMetadataSnippets',[]) for r in sn.get('snippetText',{}).get('runs',[]))
            out.append((v.get('videoId'),t,ch,pub,vc,desc[:200]))
        for x in o.values(): walk(x)
    elif isinstance(o,list):
        for x in o: walk(x)
walk(d)
for r in out: print(' | '.join(r))
