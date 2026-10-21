#!/bin/bash
# usage: fetch_pages.sh selected.txt outdir
out=$2
fetch(){ n=$1; out=$2; f=$out/L$n.html; [ -s $f ] && return; for i in 1 2 3 4; do curl -sS --max-time 60 -o $f.tmp https://colony-flow.com/level/$n && [ $(stat -c%s $f.tmp) -gt 100000 ] && mv $f.tmp $f && return; sleep $((i*3)); done; echo "FAIL $n"; }
export -f fetch
xargs -P 4 -I{} bash -c 'fetch {} '"$out"'; sleep 0.3' < $1
