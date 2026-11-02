#!/usr/bin/env bash
# Builds one wave of round levels on your own computer: everything the scripts can do (Normal, Hard, Super Hard,
# breathers; links, hidden pods, hidden cells, the latch), with every bot test of rules 17 §5. Hand positions (bridges
# and other custom mechanics) are skipped and listed: Claude builds those. See tools/rounds/local/README.md.
#
#   tools/rounds/local/build-wave.sh <from> <to> [--jobs N] [--reassign] [--minutes M] [--rebuild auto|59,62,...]
#
# --rebuild (default off): on a wave already built, take back and build again the levels the rules no longer accept
# (auto: tools/rounds/wave.mjs rebuildSet; or a list of levels); see tools/rounds/local/README.md.
#
# Resumable: run it again after a stop and it carries on. Same inputs give the same levels on any computer and any
# number of cores. It never commits or pushes: it says what to commit.
set -u
ROOT=$(cd "$(dirname "$0")/../../.." && pwd); cd "$ROOT" || exit 2
[ $# -ge 2 ] || { sed -n "5p" "$0"; exit 2; }
FROM=$1; TO=$2; shift 2
JOBS=2; REASSIGN=""; MINUTES=""; REBUILD=""
while [ $# -gt 0 ]; do case "$1" in
  --jobs) JOBS=$2; shift 2 ;; --reassign) REASSIGN=--reassign; shift ;; --minutes) MINUTES="--minutes $2"; shift 2 ;; --rebuild) REBUILD="--rebuild $2"; shift 2 ;;
  *) echo "unknown option $1"; exit 2 ;; esac; done
command -v node >/dev/null || { echo "Node.js 22 or newer is needed (https://nodejs.org)"; exit 2; }
NODE_MAJOR=$(node -p 'process.versions.node.split(".")[0]')
[ "$NODE_MAJOR" -ge 22 ] || { echo "Node.js 22 or newer is needed, found $(node -v)"; exit 2; }
WAVE=$(printf 'L%04d-L%04d' "$FROM" "$TO"); DIR=content/rounds/runs/$WAVE
[ -f "$DIR/mechanics.json" ] || { echo "No wave plan $DIR/mechanics.json yet: ask Claude to prepare the wave's mechanics plan first."; exit 2; }
echo "== plan $WAVE"; node tools/rounds/rounds.mjs plan "$FROM" "$TO" $REASSIGN > "$DIR/plan.log" 2>&1 || { tail -20 "$DIR/plan.log"; exit 1; }
echo "== run $WAVE (jobs $JOBS)"; node tools/rounds/rounds.mjs run "$FROM" "$TO" --jobs "$JOBS" $MINUTES $REBUILD 2>&1 | tee "$DIR/run.log"
CODE=${PIPESTATUS[0]}
echo
echo "== done (exit $CODE). Read: $DIR/summary.md and $DIR/audit.md; the sample pilot: $DIR/sample/"
echo "   Hand positions and anything still open are listed at the end of summary.md."
echo "   To hand it over:  git add content/rounds && git commit -m \"Wave $WAVE built locally\" && git push"
exit "$CODE"
