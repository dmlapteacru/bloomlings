#!/bin/sh
# L175 Hard on P1184 «Воздушный шар над лугом»: bridge + links, kind single, the basket bridge (site 1, up from the meadow) decides.
# usage: sh candidates/run-balloon.sh <outDir> <seed> <n>   (other settings from the caller's environment)
cd "$(dirname "$0")/.."
PIC=P1184 CLS=Hard KIND=single WIN_SITE=${WIN_SITE:-1} APPROACH=${APPROACH:-'["X"]'} ENTRY=${ENTRY-E} \
MID=${MID:-'["Y","R","C"]'} LAST=${LAST-O} OUT=${OUT:-'["G","S","M"]'} COLS=${COLS:-4} QG=${QG:-40} ACCENTS=${ACCENTS-1} ALO=${ALO:-.22} AHI=${AHI:-.45} \
TLO=${TLO:-.2} THI=${THI:-.7} EARLYN=${EARLYN:-3} FARSIZE=${FARSIZE:-26} SLIP=${SLIP:-.3} PRE=${PRE:-2} \
node search.mjs "$1" "$2" "$3"
