#!/bin/bash
export ROUNDS_THREADS=1 PIC=P0469 CLS=Hard KIND=better WIN_ANCHOR=Z APPROACH='["Z","D"]' ENTRY=N OUT='["S","G","W"]' CELLS=1 ACCENTS=1 QG=40 ALO=.25 AHI=.45 GAP=.12 OFFMIN=8 COLS=4 ROUNDS_THREADS=1
SR=/home/user/bloomlings/content/rounds/experiments/contour-bridges-51/search.mjs
(MID='["R","Y","J"]' APGAP=3 TLO=.25 THI=.75 EARLYN=2 FARSIZE=34 SLIP=.3 node $SR out/train-g 41 300 > train-g.log 2>&1) &
(MID='["Y","R","J"]' APGAP=4 TLO=.3 THI=.8 EARLYN=3 FARSIZE=40 SLIP=.3 APMIX=1 node $SR out/train-h 42 300 > train-h.log 2>&1) &
wait
