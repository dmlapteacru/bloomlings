#!/bin/bash
export ROUNDS_DIR=/tmp/cb51/rounds-copy PIC=P0468 CLS=Hard KIND=better WIN_ANCHOR=A APPROACH='["A","E"]' ENTRY=W MID='["M","G","R","Y","N"]' OUT='["H"]' COLS=4 ACCENTS=1 QG=40
S=/home/user/bloomlings/content/rounds/experiments/contour-bridges-51/search.mjs
cd /tmp/cb51
(TLO=.35 THI=.85 EARLYN=4 FARSIZE=22 SLIP=.3 node $S out/globe-c 11 60 > out/globe-c.log 2>&1) &
(TLO=.3 THI=.8 EARLYN=3 PRE=2 FARSIZE=26 SLIP=.4 node $S out/globe-d 12 60 > out/globe-d.log 2>&1) &
wait
