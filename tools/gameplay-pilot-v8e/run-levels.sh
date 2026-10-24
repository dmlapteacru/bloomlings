#!/bin/sh
# Builds every v8E level that has no content/pilot-v8e/work/L<id>.json yet, JOBS at a time (default 3), hard ones first.
# Usage (from the repo root): sh tools/gameplay-pilot-v8e/run-levels.sh [jobs] [ids...]
cd "$(dirname "$0")/../.." || exit 1
jobs=${1:-3}; [ $# -gt 0 ] && shift
ids=${*:-"22 10 29 15 25 24 27 18 20 14 8 12 21 19 13 17 26 30 9 5 7 23 28 11 16 6 4 3 2 1"}
mkdir -p content/pilot-v8e/work content/pilot-v8e/logs
for id in $ids; do [ -f content/pilot-v8e/work/L$id.json ] || echo $id; done |
 xargs -P "$jobs" -I{} sh -c 'timeout 10800 node tools/gameplay-pilot-v8e/fix.mjs level {} > content/pilot-v8e/logs/L{}.log 2>&1 || echo "L{} failed: $(tail -n 3 content/pilot-v8e/logs/L{}.log | head -n 1)"; exit 0'
