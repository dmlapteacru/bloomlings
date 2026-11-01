#!/bin/bash
export ROUNDS_DIR=/tmp/cb51/rounds-copy PIC=P0469 CLS=Hard KIND=better WIN_ANCHOR=Z APPROACH='["Z","D"]' ENTRY=N OUT='["S","G","W"]' CELLS=1 ACCENTS=1 QG=40 ALO=.25 AHI=.45 GAP=.12 OFFMIN=8 COLS=4
S=/home/user/bloomlings/content/rounds/experiments/contour-bridges-51/search.mjs
cd /tmp/cb51
(MID='["Y","J"]' LAST=R LPLO=.05 LPHI=.45 APGAP=3 TLO=.3 THI=.8 EARLYN=3 FARSIZE=22 SLIP=.3 node $S out/train-c 33 60 > out/train-c.log 2>&1) &
(MID='["R","Y","J"]' APGAP=3 TLO=.25 THI=.75 EARLYN=2 FARSIZE=24 SLIP=.3 node $S out/train-d 34 60 > out/train-d.log 2>&1) &
wait
