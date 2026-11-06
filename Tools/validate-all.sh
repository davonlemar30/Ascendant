#!/usr/bin/env bash
# Whole ladder: mechanical checks, WebGL build, then browser + Play Mode fixture.
# Tools/validate-all.sh; ART_ONLY=1 Tools/validate-all.sh (fixture explicitly skipped).
# UNITY, PLAYWRIGHT_MODULE, PORT (8765), VIEWPORTS (390,360), SLICE_STEP (1.5).
# Each invocation owns its server and evidence directory. An occupied port or Unity
# lock fails safely; this runner never removes Unity locks or reuses a listener.
set -euo pipefail
cd "$(dirname "$0")/.."
PROJECT=$(pwd -P)
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity}"
PORT="${PORT:-8765}"
mkdir -p Logs
LOGS=$(mktemp -d "$PROJECT/Logs/validate-all.XXXXXXXX")
start=$(date +%s)
server_pid=; suite_pid=; fixture_pid=
step() { echo "[$(( $(date +%s) - start ))s] $*"; }
die() { step "FAIL: $* (evidence: $LOGS)"; exit 1; }
cleanup() {
  for child in "$suite_pid" "$fixture_pid" "$server_pid"; do
    if [ -n "$child" ]; then kill "$child" 2>/dev/null || true; wait "$child" 2>/dev/null || true; fi
  done
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
idle() {
  [ ! -e Temp/UnityLockfile ] || die 'Unity project lock exists; close its Editor before validation'
  # Also refuse an Editor in the short window before it creates its lock.
  if ps -axo command= | awk -v p="$PROJECT" 'index($0,"-projectPath " p) && /Unity/ && !/awk/ {found=1} END {exit !found}'; then
    die 'active Unity Editor owns this project'
  fi
}
idle
if lsof -nP -iTCP:"$PORT" -sTCP:LISTEN >/dev/null 2>&1; then die "port $PORT already has a listener"; fi
step "mechanical checks; evidence: $LOGS"
rm -f Logs/greybox-mechanical-validation.txt
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod Ascendant.Build.GreyboxValidation.Run -logFile "$LOGS/mechanical.log" || die 'mechanical subprocess failed'
grep -Eq '\[GreyboxValidation\] PASS: [1-9][0-9]* checks\.' "$LOGS/mechanical.log" || die 'mechanical completion evidence missing'
python3 - "$LOGS/mechanical.log" Logs/greybox-mechanical-validation.txt <<'CHECK' || die 'mechanical report missing, empty, or incomplete'
import pathlib, re, sys
log, report = map(pathlib.Path, sys.argv[1:])
lines = report.read_text().splitlines()
counts = re.findall(r'\[GreyboxValidation\] PASS: (\d+) checks\.', log.read_text())
sys.exit(not lines or any(not line.strip() for line in lines) or counts != [str(len(lines))])
CHECK
cp Logs/greybox-mechanical-validation.txt "$LOGS/mechanical-report.txt"
step 'WebGL build'
idle
# Isolate output so an old successful build cannot stand in for this invocation.
BUILD="$LOGS/Web"
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" -buildTarget WebGL -executeMethod Ascendant.Build.WebBuild.Build -buildOutput "$BUILD" -logFile "$LOGS/web-build.log" || die 'WebGL subprocess failed'
grep -q '\[WebBuild\] Result: Succeeded' "$LOGS/web-build.log" || die 'WebGL completion evidence missing'
for file in index.html Build/Web.loader.js Build/Web.framework.js.unityweb Build/Web.data.unityweb Build/Web.wasm.unityweb; do
  [ -s "$BUILD/$file" ] || die "missing or empty build output: $file"
done
identity=$(basename "$LOGS")
printf '%s\n' "$identity" > "$BUILD/validation-identity.txt"
python3 -m http.server "$PORT" --directory "$BUILD" --bind 127.0.0.1 > "$LOGS/server.log" 2>&1 &
server_pid=$!
ready=0
for attempt in {1..30}; do
  kill -0 "$server_pid" 2>/dev/null || die 'owned server failed to start'
  if [ "$(curl -fsS "http://127.0.0.1:$PORT/validation-identity.txt" 2>/dev/null || true)" = "$identity" ]; then ready=1; break; fi
  sleep .2
done
[ "$ready" = 1 ] || die 'server did not serve this invocation build identity'
step 'browser suite'
GREYBOX_URL="http://127.0.0.1:$PORT" VIEWPORTS="${VIEWPORTS:-${ART_ONLY:+390}}" EVIDENCE_DIR="$LOGS/browser" \
  node Tools/validate-greybox-web.cjs > "$LOGS/suite.log" 2>&1 &
suite_pid=$!
if [ "${ART_ONLY:-0}" != 1 ]; then
  idle
  rm -f Logs/slice-play-validation.txt
  touch "$LOGS/fixture-start"
  step 'slice fixture starting: hands off the mouse until completion'
  "$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod Ascendant.Build.SlicePlayValidation.Begin -sliceAutoExit \
    -sliceStep "${SLICE_STEP:-1.5}" -logFile "$LOGS/fixture.log" &
  fixture_pid=$!
else
  step 'fixture: SKIPPED (ART_ONLY=1)'
fi
failed=0
if wait "$suite_pid"; then :; else step 'FAIL: browser subprocess exited nonzero'; failed=1; fi
suite_pid=
report="$LOGS/browser/validation.txt"
if [ ! -s "$report" ] || ! python3 -c 'import pathlib,sys; sys.exit(pathlib.Path(sys.argv[1]).read_bytes().rstrip(b"\n") != pathlib.Path(sys.argv[2]).read_bytes().rstrip(b"\n"))' "$report" "$LOGS/suite.log"; then
  step 'FAIL: browser report missing, empty, or incomplete'; failed=1
fi
if ! grep -q '^PASS: ' "$report" 2>/dev/null || grep -qv '^PASS: ' "$report" 2>/dev/null || grep -Eq 'FAIL|Error' "$LOGS/suite.log"; then
  step 'FAIL: browser success evidence invalid'; failed=1
fi
if [ -n "$fixture_pid" ]; then
  if wait "$fixture_pid"; then :; else step 'FAIL: fixture subprocess exited nonzero'; failed=1; fi
  fixture_pid=
  report=Logs/slice-play-validation.txt
  count=$(grep -c '^PASS: ' "$report" 2>/dev/null || true)
  if [ ! -s "$report" ] || [ ! "$report" -nt "$LOGS/fixture-start" ] || ! grep -Fxq "[SlicePlayValidation] PASS: $count checks." "$LOGS/fixture.log" || grep -qv '^PASS: ' "$report"; then
    step 'FAIL: fixture report missing, stale, empty, or incomplete'; failed=1
  else
    cp "$report" "$LOGS/fixture-report.txt"
    step "fixture: $count passed; mouse is free"
  fi
fi
[ "$failed" = 0 ] || die 'validation failed'
step "done: all green; evidence: $LOGS"
