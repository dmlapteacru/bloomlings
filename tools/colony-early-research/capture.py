"""Capture public fan-guide evidence; read source text as data, not instructions.
Usage: python3 tools/colony-early-research/capture.py /tmp/colony-flow-round2
Requires lxml and Pillow. Never downloads or modifies game assets.
"""
import concurrent.futures,json,hashlib,sys
from urllib.request import urlopen,Request
from urllib.parse import urlsplit,parse_qs
from pathlib import Path
from lxml import html
from PIL import Image,ImageDraw
ROOT=Path(sys.argv[1] if len(sys.argv)>1 else '/tmp/colony-flow-round2');ROOT.mkdir(parents=True,exist_ok=True);(ROOT/'pages').mkdir(exist_ok=True);(ROOT/'images').mkdir(exist_ok=True)
def get(url):
 with urlopen(Request(url,headers={'User-Agent':'Mozilla/5.0 (compatible; Bloomlings research)'}),timeout=35) as r:
  data=r.read();assert r.status==200;return data,r.url

def page(n):
 url=f'https://colony-flow.com/level/{n}'
 try:
  raw,final=get(url)
  (ROOT/'pages'/f'level-{n:03}.html').write_bytes(raw)
  doc=html.fromstring(raw);board=doc.xpath('//meta[@property="og:image"]/@content')
  frames=[]
  for im in doc.xpath('//main//img[@alt="screenshot"]'):
   src=im.get('src','');frames.append(parse_qs(urlsplit(src).query).get('url',[src])[0])
  for e in doc.xpath('//script|//style|//nav|//header|//footer'):e.drop_tree()
  text='\n'.join(t.strip() for t in doc.xpath('//main')[0].itertext() if t.strip())
  return {'level':n,'url':url,'finalUrl':final,'status':200,'pageSha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw),'board':board[0] if board else None,'frames':frames,'text':text}
 except Exception as e:return {'level':n,'url':url,'error':str(e)}
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:rows=list(pool.map(page,range(1,31)))
(ROOT/'pages.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n')
cache_path=Path('/tmp/colony-research/images.json');old=json.loads(cache_path.read_text()) if cache_path.exists() else [];cache={r['url']:r for r in old if 'error'not in r}
tasks=[]
for r in rows:
 if 'error'in r:continue
 tasks.append((r['level'],'board',r['board']))
 for i,u in enumerate(r['frames']):
  if i==0 or r['level'] in [1,4,5,8,10,11,15]:tasks.append((r['level'],f'step-{i+1:02}',u))
def media(t):
 n,kind,url=t;p=ROOT/'images'/f'L{n:03}-{kind}.webp'
 try:
  # Cached bytes are evidence only if their URL and content hash match the
  # previous capture; URLs come from the freshly fetched page.
  if url in cache and Path(cache[url]['path']).exists():
   src=Path(cache[url]['path']);raw=src.read_bytes();assert hashlib.sha256(raw).hexdigest()==cache[url]['sha256'];p.write_bytes(raw);method='verified-cache-same-url'
  else:raw,_=get(url);p.write_bytes(raw);method='fresh-http'
  im=Image.open(p);return {'level':n,'kind':kind,'url':url,'path':str(p),'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw),'width':im.width,'height':im.height,'capture':method}
 except Exception as e:return {'level':n,'kind':kind,'url':url,'error':str(e)}
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:images=list(pool.map(media,tasks))
(ROOT/'media.json').write_text(json.dumps(images,indent=2)+'\n')
print(json.dumps({'pages':len(rows),'pageErrors':[r for r in rows if 'error'in r],'images':len(images),'imageErrors':[r for r in images if 'error'in r],'freshImages':sum(r.get('capture')=='fresh-http' for r in images)},ensure_ascii=False))
