#!/bin/sh
# L75 Super Hard on P1183 «Замок на острове» (the owner's amendment of 2026-11-03): bridge + links + three latches, kind single.
# usage: sh candidates/run-castle.sh <outDir> <seed> <n> [extra env assignments are taken from the caller's environment]
cd "$(dirname "$0")/.."
PIC=P1183 CLS="Super Hard" KIND=single WIN_ANCHOR=${WIN_ANCHOR:-G} APPROACH=${APPROACH:-'["G"]'} ENTRY=${ENTRY-X} \
MID=${MID:-'["C","R","D","L"]'} LAST=${LAST-M} OUT=${OUT:-'["S","W"]'} LATCHES=3 COLS=${COLS:-4} QG=${QG:-40} \
TLO=${TLO:-.15} THI=${THI:-.6} EARLYN=${EARLYN:-3} FARSIZE=${FARSIZE:-24} SLIP=${SLIP:-.3} PRE=${PRE:-2} \
node search.mjs "$1" "$2" "$3"
