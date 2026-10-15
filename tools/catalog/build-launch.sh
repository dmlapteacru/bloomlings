#!/usr/bin/env bash
# Replacement launch builder; identical pipeline commands to build-launch.ps1.
set -euo pipefail
ROOT=$(cd "$(dirname "$0")/../.." && pwd)
cd "$ROOT"
JOBS=2
RETRIES=2
LEVELS=1-2500
OUT=content/work/launch-catalog
STATUS=0
PLAN_ONLY=0
INSTALL=0
while [ $# -gt 0 ]; do
  case "$1" in
    --jobs) JOBS=$2; shift 2 ;;
    --retries) RETRIES=$2; shift 2 ;;
    --levels) LEVELS=$2; shift 2 ;;
    --out) OUT=$2; shift 2 ;;
    --status) STATUS=1; shift ;;
    --plan-only) PLAN_ONLY=1; shift ;;
    --install) INSTALL=1; shift ;;
    *) echo "unknown option $1" >&2; exit 2 ;;
  esac
done
dotnet build core/src/Bloomlings.Pipeline -c Release -m:2 --nologo
pipeline() { dotnet run --project core/src/Bloomlings.Pipeline -c Release --no-build -- "$@"; }
pipeline launch plan
[ "$PLAN_ONLY" = 0 ] || exit 0
pipeline launch status
[ "$STATUS" = 0 ] || exit 0
pipeline launch generate --levels "$LEVELS" --out "$OUT" --jobs "$JOBS" --retries "$RETRIES"
if [ "$INSTALL" = 1 ]; then pipeline launch install --from "$OUT"; fi
echo "Generated catalog: $OUT. --install connects a complete validated build to the game source."
