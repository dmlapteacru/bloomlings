# usage: ytpl.py PLAYLIST_ID OUTFILE  (fetches playlist via youtube web + innertube continuation)
import sys,re,json,urllib.request,time,ssl
pid,outf=sys.argv[1],sys.argv[2]
UA="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124 Safari/537.36"
ctx=ssl.create_default_context(cafile='/root/.ccr/ca-bundle.crt')
def get(url,data=None):
    h={'User-Agent':UA,'Accept-Language':'en-US','Cookie':'CONSENT=YES+1; SOCS=CAI'}
    if data is not None:
        h['Content-Type']='application/json'; data=json.dumps(data).encode()
    req=urllib.request.Request(url,data=data,headers=h)
    return urllib.request.urlopen(req,context=ctx,timeout=40).read().decode('utf-8','replace')
s=get(f'https://www.youtube.com/playlist?list={pid}&hl=en')
key=re.search(r'"INNERTUBE_API_KEY":"([^"]+)"',s).group(1)
cver=re.search(r'"INNERTUBE_CLIENT_VERSION":"([^"]+)"',s).group(1)
d=json.loads(re.search(r'var ytInitialData = (\{.*?\});</script>',s,re.S).group(1))
rows=[]
def walk(o,conts):
    if isinstance(o,dict):
        if 'playlistVideoRenderer' in o:
            v=o['playlistVideoRenderer']
            t=''.join(r.get('text','') for r in v.get('title',{}).get('runs',[])) or v.get('title',{}).get('simpleText','')
            info=''.join(r.get('text','') for r in v.get('videoInfo',{}).get('runs',[]))
            rows.append({'id':v.get('videoId'),'title':t,'info':info})
        if 'lockupViewModel' in o:
            v=o['lockupViewModel']
            t=v.get('metadata',{}).get('lockupMetadataViewModel',{}).get('title',{}).get('content','')
            rows.append({'id':v.get('contentId'),'title':t})
        if 'continuationCommand' in o:
            t=o['continuationCommand'].get('token')
            if t: conts.append(t)
        for x in o.values(): walk(x,conts)
    elif isinstance(o,list):
        for x in o: walk(x,conts)
conts=[]; walk(d,conts)
n=0
while conts and n<60:
    tok=conts[0]; n+=1
    r=get(f'https://www.youtube.com/youtubei/v1/browse?key={key}&prettyPrint=false',{'context':{'client':{'clientName':'WEB','clientVersion':cver,'hl':'en'}},'continuation':tok})
    conts=[]; walk(json.loads(r),conts); time.sleep(0.5)
json.dump(rows,open(outf,'w'),indent=0)
print(len(rows))
