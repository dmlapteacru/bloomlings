import re, html, json, sys, os, glob, hashlib
pdir, out = sys.argv[1], sys.argv[2]
rows=[]
def clean(fragment):
    fragment=re.sub(r'<(script|style)[^>]*>.*?</\1>','',fragment,flags=re.S)
    fragment=re.sub(r'</?(p|h[1-6]|li|div|br|tr|section)[^>]*>','\n',fragment)
    t=re.sub(r'<[^>]+>','',fragment)
    t=html.unescape(t)
    t=re.sub(r'[ \t]+',' ',t)
    t=re.sub(r'\n\s*\n+','\n',t)
    return t.strip()
for f in sorted(glob.glob(os.path.join(pdir,'L*.html')), key=lambda p:int(re.findall(r'L(\d+)',p)[-1])):
    n=int(re.findall(r'L(\d+)\.html',f)[0])
    s=open(f,encoding='utf8').read()
    title=html.unescape(re.search(r'<title>([^<]*)',s).group(1))
    og=re.search(r'property="og:image" content="([^"]+)',s)
    board=og.group(1) if og else None
    frames=[]
    for m in re.finditer(r'<img[^>]*alt="screenshot"[^>]*>',s):
        u=re.search(r'url=([^&"]+)',m.group(0))
        if u:
            import urllib.parse
            frames.append(urllib.parse.unquote(u.group(1)))
    vid=re.search(r'(https://www\.youtube\.com/watch\?v=[\w-]+)',s)
    i=s.find('<h1'); j=s.find('More Colony Flow Guides',i)
    body=clean(s[i:j]) if i>=0 else ''
    # drop boilerplate before the overview
    k=body.find('Overview')
    pre=body[:k] if k>0 else ''
    rows.append(dict(level=n,title=title,board=board,frames=frames,video=vid.group(1) if vid else None,
                     pageSha256=hashlib.sha256(s.encode()).hexdigest(),text=body,textChars=len(body)))
json.dump(rows,open(out,'w'),indent=1)
print(len(rows), sum(1 for r in rows if r['board']), sum(len(r['frames']) for r in rows))
