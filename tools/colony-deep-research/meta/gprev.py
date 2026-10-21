# usage: gprev.py OUT.json SORT(1=relevant,2=newest) PAGES
import sys,json,urllib.request,urllib.parse,ssl,time,re
out,sort,pages=sys.argv[1],int(sys.argv[2]),int(sys.argv[3])
ctx=ssl.create_default_context(cafile='/root/.ccr/ca-bundle.crt')
url='https://play.google.com/_/PlayStoreUi/data/batchexecute?rpcids=UsvDTd&f.sid=-2417434988450146470&bl=boq_playuiserver_20190424.04_p0&hl=en&gl=us&authuser&soc-app=121&soc-platform=1&soc-device=1&_reqid=1065213'
allr=[];tok=None
for p in range(pages):
    inner=[None,None,[2,sort,[150,None,tok],None,[]],["com.abi.colony.flow",7]] if tok else [None,None,[2,sort,[150],None,[]],["com.abi.colony.flow",7]]
    body='f.req='+urllib.parse.quote(json.dumps([[["UsvDTd",json.dumps(inner),None,"generic"]]]))
    req=urllib.request.Request(url,data=body.encode(),headers={'Content-Type':'application/x-www-form-urlencoded;charset=UTF-8','User-Agent':'Mozilla/5.0'})
    r=urllib.request.urlopen(req,context=ctx,timeout=40).read().decode()
    line=[l for l in r.split('\n') if l.startswith('[["wrb.fr"')][0]
    data=json.loads(json.loads(line)[0][2])
    for rv in data[0]:
        try:
            allr.append({'id':rv[0],'user':rv[1][0],'score':rv[2],'text':rv[4],'ts':rv[5][0],'thumbs':rv[6],'ver':rv[10] if len(rv)>10 else None,'reply':(rv[7][1] if rv[7] else None)})
        except Exception as e: pass
    tok=None
    for el in data[1:]:
        if isinstance(el,list) and el and isinstance(el[-1],str): tok=el[-1]
    print('page',p,len(data[0]),repr(data[1:])[:200],file=sys.stderr)
    if not tok or not isinstance(tok,str): break
    time.sleep(0.7)
json.dump(allr,open(out,'w'))
print(len(allr))
