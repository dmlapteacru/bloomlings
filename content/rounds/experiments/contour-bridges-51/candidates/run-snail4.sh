#!/bin/bash
# the flipped snail: the up bridge (pebble path X → foot Z, near the entry) decides, the shell (O, its rings A and E) comes late
export ROUNDS_DIR=/tmp/cb51/rounds-copy PIC=P0470 CLS="Super Hard" KIND=single WIN_ANCHOR=X APPROACH='["R","X"]' ENTRY=Z MID='["D","A","E"]' LAST=O OUT='["S","G"]' CELLS=1 ACCENTS=1 QG=60 ALO=.08 AHI=.17 OFFMIN=12 PMIN=2 CALM=1 COLS=3 APFIRST=R
S=/home/user/bloomlings/content/rounds/experiments/contour-bridges-51/search.mjs
cd /tmp/cb51
APGAP=5 TLO=.2 THI=.6 EARLYN=3 PRE=1 FARSIZE=22 SLIP=.2 LPLO=.5 LPHI=.95 node $S out/snail-i 29 150 > out/snail-i.log 2>&1
