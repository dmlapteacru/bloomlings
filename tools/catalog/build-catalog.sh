#!/usr/bin/env bash
# Builds the level catalog (content/catalog) band by band, as tools/catalog/README.md describes.
# The twin of build-catalog.ps1: the same bands, seeds, segments, retries and checks, so both give the same files.
#
#   tools/catalog/build-catalog.sh [--jobs N] [--to-level N] [--retries N] [--work DIR] [--no-build] [--no-final]
#   tools/catalog/build-catalog.sh --check BAND [--jobs N]      # regenerate one band and compare it with content/catalog
#
# It never commits or pushes: it says what to commit.
set -u

ROOT=$(cd "$(dirname "$0")/../.." && pwd)
cd "$ROOT" || exit 2

nproc_all=$(getconf _NPROCESSORS_ONLN 2>/dev/null || echo 4)
JOBS=$(( nproc_all > 3 ? nproc_all - 2 : 1 ))
TO_LEVEL=5000
RETRIES=2
WORK=content/work/catalog-build
BUILD=1
FINAL=1
CHECK=""

while [ $# -gt 0 ]; do
  case "$1" in
    --jobs) JOBS=$2; shift 2 ;;
    --to-level) TO_LEVEL=$2; shift 2 ;;
    --retries) RETRIES=$2; shift 2 ;;
    --work) WORK=$2; shift 2 ;;
    --no-build) BUILD=0; shift ;;
    --no-final) FINAL=0; shift ;;
    --check) CHECK=$2; shift 2 ;;
    -h|--help) sed -n 2,9p "$0"; exit 0 ;;
    *) echo "unknown option $1" >&2; exit 2 ;;
  esac
done

CATALOG=content/catalog
MANIFEST=$CATALOG/build-manifest.jsonl
PIPE="$WORK/pipe/bloomlings-pipeline.dll"

# Band  first last  profile          seed segments  (fixed: the levels depend on them, never on --jobs)
BANDS="0011-0025 11 25 band-0011-0025 1 1
0026-0050 26 50 band-0026-0050 1 1
0051-0100 51 100 band-0051-0100 1 1
0101-0250 101 250 band-0101-0250 1 3
0251-0500 251 500 band-0251-0500 1 5
0501-1000 501 1000 band-0501-1000 1 10
1001-2000 1001 2000 band-1001-2000 1 14
2001-3000 2001 3000 band-2001-5000 1 14
3001-4000 3001 4000 band-2001-5000 1 14
4001-5000 4001 5000 band-2001-5000 1 14"

say() { echo "[$(date -u +%H:%M:%S)] $*"; }
fail() { say "STOP: $*"; exit 1; }
name() { printf 'level-%05d.json' "$1"; }
pipeline() { dotnet "$PIPE" "$@" < /dev/null; }

# Level numbers of a folder's levels/ (or of the folder itself), one per line.
levels_in() {
  local dir=$1
  [ -d "$dir/levels" ] && dir="$dir/levels"
  [ -d "$dir" ] || return 0
  for f in "$dir"/level-*.json; do
    [ -f "$f" ] || continue
    local b=${f##*/level-}
    b=${b%.json}
    [[ $b =~ ^[0-9]+$ ]] || continue
    echo $((10#$b))
  done
}

# Copies one level's definition and validation record from a batch folder into another folder.
copy_level() {
  local from=$1 to=$2 n=$3
  mkdir -p "$to/levels" "$to/validation"
  cp "$from/levels/$(name "$n")" "$to/levels/$(name "$n")" || return 1
  cp "$from/validation/$(name "$n")" "$to/validation/$(name "$n")" || return 1
}

# The "errors" count of a validate --json report.
errors_of() { grep -m1 -o '"errors": *[0-9]*' "$1" | grep -o '[0-9]*$'; }
warnings_of() { local total; total=$(grep -c '"check":' "$1"); echo $(( total - $(errors_of "$1") )); }

json_list() { local out="" x; for x in "$@"; do out="$out${out:+,}$x"; done; echo "[$out]"; }

build_pipeline() {
  if [ "$BUILD" = 1 ] || [ ! -f "$PIPE" ]; then
    say "building the pipeline into $WORK/pipe"
    dotnet build core/src/Bloomlings.Pipeline -c Release -o "$WORK/pipe" < /dev/null > "$WORK/build.log" 2>&1 || fail "the build failed, see $WORK/build.log"
  fi
}

commit_of() {
  local c
  c=$(git rev-parse --short HEAD 2>/dev/null || echo unknown)
  if [ -n "$(git status --porcelain -- core content/profiles content/pictures content/showcase content/curated content/readability 2>/dev/null)" ]; then
    c="$c+changes"
  fi
  echo "$c"
}

# Generates one band into $out (gen/, fills, assembled/) with $catalog as the earlier levels.
# Sets: GENERATED KEPT FILLS GAPS SECONDS SEAMS
build_band() {
  local band=$1 first=$2 last=$3 profile=$4 seed=$5 segments=$6 catalog=$7 out=$8 previous=$9
  local start; start=$(date +%s)
  rm -rf "$out"; mkdir -p "$out"
  local history=()
  [ -n "$previous" ] && [ -d "$previous" ] && history=(--history "$previous")
  say "L$first–$last ($band): generate --seed $seed --segments $segments --jobs $JOBS"
  pipeline generate --profile "content/profiles/$profile.json" --levels "$first-$last" --seed "$seed" \
    --segments "$segments" --jobs "$JOBS" --catalog "$catalog" --keep content/showcase ${history[@]+"${history[@]}"} \
    --out "$out/gen" --json > "$out/gen.json" 2> "$out/gen.log"
  local code=$?
  [ $code -eq 2 ] && fail "generate failed (exit 2), see $out/gen.log"
  SEAMS=$(grep -m1 -o '"seamRepairs": *[0-9]*' "$out/gen.json" | grep -o '[0-9]*$')

  local kept produced
  kept=" $(levels_in content/showcase | tr '\n' ' ') "
  produced=" $(levels_in "$out/gen" | tr '\n' ' ') "
  GENERATED=0; KEPT=0; FILLS=(); GAPS=()
  local n
  for ((n = first; n <= last; n++)); do
    case "$kept" in *" $n "*) KEPT=$((KEPT + 1)); continue ;; esac
    case "$produced" in *" $n "*) GENERATED=$((GENERATED + 1)); continue ;; esac
    # A level without an accepted candidate: the next seeds, with the band's other levels as its neighbours.
    local ok=0 s
    for ((s = seed + 1; s <= seed + RETRIES; s++)); do
      local fill="$out/fill-$n-s$s" fills_history=()
      local f
      for f in "$out"/fill-*-s*; do [ -d "$f/levels" ] && fills_history+=("$f"); done
      say "  L$n failed every candidate; seed $s"
      pipeline generate --profile "content/profiles/$profile.json" --levels "$n-$n" --seed "$s" --catalog "$catalog" \
        --keep content/showcase --history "$out/gen" ${fills_history[@]+"${fills_history[@]}"} --out "$fill" > "$fill.log" 2>&1
      [ $? -eq 2 ] && fail "generate failed (exit 2), see $fill.log"
      if [ -f "$fill/levels/$(name "$n")" ]; then
        FILLS+=("\"$n:$s\""); ok=1; GENERATED=$((GENERATED + 1)); break
      fi
      rm -rf "$fill"
    done
    [ $ok -eq 0 ] && GAPS+=("$n") && say "  L$n stays a gap after seeds $seed–$((seed + RETRIES))"
  done

  # The band: its generated levels, the fills and its fixed showcase levels.
  local asm="$out/assembled"
  mkdir -p "$asm/levels" "$asm/validation"
  for n in $(levels_in "$out/gen"); do copy_level "$out/gen" "$asm" "$n" || fail "L$n has no validation record"; done
  for f in "$out"/fill-*-s*; do
    [ -d "$f/levels" ] || continue
    for n in $(levels_in "$f"); do copy_level "$f" "$asm" "$n" || fail "L$n has no validation record"; done
  done
  for n in $(levels_in content/showcase); do
    if [ "$n" -ge "$first" ] && [ "$n" -le "$last" ]; then copy_level content/showcase "$asm" "$n" || fail "showcase L$n has no validation record"; fi
  done
  SECONDS_USED=$(( $(date +%s) - start ))
}

# The curated Levels 1–10 with their validation records.
seed_curated() {
  local have=0 n
  for n in $(levels_in "$CATALOG"); do [ "$n" -le 10 ] && have=$((have + 1)); done
  [ "$have" -eq 10 ] && return 0
  say "L1–10: the curated levels with their validation records"
  local tmp="$WORK/curated"
  rm -rf "$tmp"; mkdir -p "$tmp/levels"
  for ((n = 1; n <= 10; n++)); do cp "content/curated/$(printf 'level-%04d.json' "$n")" "$tmp/levels/$(name "$n")" || fail "content/curated lacks L$n"; done
  pipeline validate --defs "$tmp" --write-records --json > "$tmp/validate.json" 2> "$tmp/validate.log"
  local errors; errors=$(errors_of "$tmp/validate.json")
  [ "${errors:-1}" -eq 0 ] || fail "the curated levels have $errors validation errors, see $tmp/validate.json"
  for ((n = 1; n <= 10; n++)); do copy_level "$tmp" "$CATALOG" "$n"; done
  echo "{\"band\":\"0001-0010\",\"levels\":\"1-10\",\"source\":\"content/curated\",\"validation\":\"0 errors, $(warnings_of "$tmp/validate.json") warnings\",\"pipeline\":\"$(commit_of)\"}" >> "$MANIFEST"
}

band_done() { [ -f "$MANIFEST" ] && grep -q "\"band\":\"$1\"" "$MANIFEST"; }

mkdir -p "$WORK" "$CATALOG/levels" "$CATALOG/validation"
build_pipeline

# --check BAND: regenerate a finished band with only the earlier levels as history and compare it file by file.
if [ -n "$CHECK" ]; then
  line=$(echo "$BANDS" | awk -v b="$CHECK" '$1 == b')
  [ -n "$line" ] || fail "no band $CHECK"
  read -r band first last profile seed segments <<< "$line"
  view="$WORK/check-$band/catalog"
  rm -rf "$WORK/check-$band"; mkdir -p "$view/levels" "$view/validation"
  for n in $(levels_in "$CATALOG"); do [ "$n" -lt "$first" ] && copy_level "$CATALOG" "$view" "$n"; done
  build_band "$band" "$first" "$last" "$profile" "$seed" "$segments" "$view" "$WORK/check-$band/band" ""
  diffs=0; compared=0
  for n in $(levels_in "$WORK/check-$band/band/assembled"); do
    for kind in levels validation; do
      compared=$((compared + 1))
      cmp -s "$WORK/check-$band/band/assembled/$kind/$(name "$n")" "$CATALOG/$kind/$(name "$n")" || { diffs=$((diffs + 1)); say "  differs: $kind/$(name "$n")"; }
    done
  done
  say "check $band: $compared files compared, $diffs differ ($SECONDS_USED s)"
  [ $diffs -eq 0 ] && exit 0 || exit 1
fi

seed_curated
SUMMARY="$WORK/summary.txt"
: > "$SUMMARY"
previous=""
while read -r -u 3 band first last profile seed segments; do
  [ "$first" -gt "$TO_LEVEL" ] && break
  if band_done "$band"; then
    [ -n "$(levels_in "$CATALOG" | awk -v a="$first" -v b="$last" '$1 >= a && $1 <= b' | head -1)" ] \
      || fail "$MANIFEST lists band $band, but content/catalog has none of its levels"
    say "L$first–$last ($band): done earlier, skipped"
    echo "$band skipped (in $MANIFEST)" >> "$SUMMARY"
    previous="$WORK/$band/assembled"
    continue
  fi

  # Only finished bands may stand in the catalog: a stopped run's partial copy of this band is removed.
  for n in $(levels_in "$CATALOG"); do
    if [ "$n" -gt "$last" ]; then fail "content/catalog holds L$n, past the unfinished band $band; restore the catalog first"; fi
    if [ "$n" -ge "$first" ]; then rm -f "$CATALOG/levels/$(name "$n")" "$CATALOG/validation/$(name "$n")"; fi
  done

  build_band "$band" "$first" "$last" "$profile" "$seed" "$segments" "$CATALOG" "$WORK/$band" "$previous"
  asm="$WORK/$band/assembled"
  say "L$first–$last: validate ($GENERATED generated, $KEPT kept, ${#GAPS[@]} gaps)"
  pipeline validate --defs "$asm" --context "$CATALOG" --json > "$WORK/$band/validate.json" 2> "$WORK/$band/validate.log"
  errors=$(errors_of "$WORK/$band/validate.json")
  [ -n "$errors" ] || fail "validate gave no report, see $WORK/$band/validate.log"
  warnings=$(warnings_of "$WORK/$band/validate.json")
  if [ "$errors" -ne 0 ]; then
    grep -B1 -A2 '"error": true' "$WORK/$band/validate.json" | head -40
    fail "band $band has $errors validation errors (report: $WORK/$band/validate.json); nothing was copied into $CATALOG"
  fi
  for n in $(levels_in "$asm"); do copy_level "$asm" "$CATALOG" "$n"; done
  echo "{\"band\":\"$band\",\"levels\":\"$first-$last\",\"profile\":\"$profile\",\"seed\":$seed,\"segments\":$segments,\"retrySeeds\":$RETRIES,\"jobs\":$JOBS,\"generated\":$GENERATED,\"kept\":$KEPT,\"seamRepairs\":${SEAMS:-0},\"fills\":$(json_list ${FILLS[@]+"${FILLS[@]}"}),\"gaps\":$(json_list ${GAPS[@]+"${GAPS[@]}"}),\"seconds\":$SECONDS_USED,\"validation\":\"0 errors, $warnings warnings\",\"pipeline\":\"$(commit_of)\"}" >> "$MANIFEST"
  line="$band: $GENERATED generated, $KEPT kept, fills ${FILLS[*]:-none}, gaps ${GAPS[*]:-none}, ${SEAMS:-0} seam repairs, ${SECONDS_USED} s, validate 0 errors and $warnings warnings"
  say "$line"
  echo "$line" >> "$SUMMARY"
  previous="$asm"
done 3<<< "$BANDS"

if [ "$FINAL" = 1 ]; then
  say "the whole catalog: validate and score"
  pipeline validate --catalog "$CATALOG" --context content/curated --json > "$WORK/validate-catalog.json" 2> "$WORK/validate-catalog.log"
  errors=$(errors_of "$WORK/validate-catalog.json")
  pipeline score --defs "$CATALOG" --curated content/curated > "$WORK/score.txt" 2>&1
  score_code=$?
  echo "catalog: $(levels_in "$CATALOG" | wc -l) levels, validate ${errors:-?} errors, score exit $score_code (see $WORK/score.txt)" >> "$SUMMARY"
  [ "${errors:-1}" -eq 0 ] || { cat "$SUMMARY"; fail "the whole catalog has $errors validation errors, see $WORK/validate-catalog.json"; }
fi

echo
echo "Summary ($SUMMARY):"
cat "$SUMMARY"
echo
echo "Nothing was committed. To commit the catalog:"
echo "  git add content/catalog && git commit"
