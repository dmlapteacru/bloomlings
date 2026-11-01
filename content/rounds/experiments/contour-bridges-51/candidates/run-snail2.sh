#!/bin/bash
export ROUNDS_DIR=/tmp/cb51/rounds-copy PIC=P0470 CLS="Super Hard" KIND=single WIN_ANCHOR=R APPROACH='["R","X"]' ENTRY=O MID='["A","E","D"]' LAST=Z OUT='["S","G"]' CELLS=1 ACCENTS=1 QG=60 ALO=.08 AHI=.17 OFFMIN=12 PMIN=2 CALM=1 COLS=4 APFIRST=R
S=/home/user/bloomlings/content/rounds/experiments/contour-bridges-51/search.mjs
cd /tmp/cb51
(APGAP=5 TLO=.15 THI=.5 EARLYN=3 FARSIZE=24 SLIP=.3 node $S out/snail-e 25 150 > out/snail-e.log 2>&1) &
(APGAP=4 TLO=.1 THI=.45 EARLYN=4 FARSIZE=26 SLIP=.3 node $S out/snail-f 26 150 > out/snail-f.log 2>&1) &
wait
