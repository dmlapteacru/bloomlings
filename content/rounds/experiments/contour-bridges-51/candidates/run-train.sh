#!/bin/bash
export ROUNDS_DIR=/tmp/cb51/rounds-copy PIC=P0469 CLS=Hard KIND=better WIN_ANCHOR=Z APPROACH='["Z","D"]' ENTRY=N MID='["Y","J"]' LAST=R OUT='["S","G","W"]' CELLS=1 ACCENTS=1 QG=40 ALO=.25 AHI=.45 GAP=.12 OFFMIN=8
S=/home/user/bloomlings/content/rounds/experiments/contour-bridges-51/search.mjs
cd /tmp/cb51
(COLS=4 APGAP=3 TLO=.3 THI=.8 EARLYN=3 FARSIZE=22 SLIP=.3 node $S out/train-a 31 60 > out/train-a.log 2>&1) &
(COLS=3 APGAP=2 TLO=.25 THI=.7 EARLYN=2 FARSIZE=26 SLIP=.3 node $S out/train-b 32 60 > out/train-b.log 2>&1) &
wait
