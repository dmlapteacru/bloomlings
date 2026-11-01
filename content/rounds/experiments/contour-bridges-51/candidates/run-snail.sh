#!/bin/bash
export ROUNDS_DIR=/tmp/cb51/rounds-copy PIC=P0470 CLS="Super Hard" KIND=single WIN_ANCHOR=R APPROACH='["R","X"]' ENTRY=O MID='["A","E","D"]' LAST=Z OUT='["S","G"]' CELLS=1 ACCENTS=1 QG=60 ALO=.07 AHI=.18 OFFMIN=10
S=/home/user/bloomlings/content/rounds/experiments/contour-bridges-51/search.mjs
cd /tmp/cb51
(COLS=4 APFIRST=R APGAP=3 TLO=.2 THI=.6 EARLYN=4 FARSIZE=20 SLIP=.3 node $S out/snail-c 23 60 > out/snail-c.log 2>&1) &
(COLS=4 APFIRST=R APGAP=5 TLO=.15 THI=.5 EARLYN=3 FARSIZE=24 SLIP=.3 node $S out/snail-d 24 60 > out/snail-d.log 2>&1) &
wait
