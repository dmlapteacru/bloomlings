#!/bin/bash
# usage: fetch_imgs.sh list("name url") outdir ; fetched through the page's own Next.js image endpoint on colony-flow.com
out=$2
fetch(){ n=$1; u=$2; out=$3; f=$out/$n.webp; [ -s $f ] && return; e=$(python3 -I -c "import urllib.parse,sys;print(urllib.parse.quote(sys.argv[1],safe=''))" "$u"); for i in 1 2 3 4; do curl -sS --max-time 60 -H "Accept: image/webp,*/*" -o $f.tmp "https://colony-flow.com/_next/image?url=$e&w=1920&q=100" && [ $(stat -c%s $f.tmp) -gt 1000 ] && mv $f.tmp $f && return; sleep $((i*3)); done; echo "FAIL $n"; }
export -f fetch
xargs -P 3 -L 1 bash -c 'fetch $0 $1 '"$out" < $1
