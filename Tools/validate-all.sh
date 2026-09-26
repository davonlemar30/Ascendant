#!/usr/bin/env bash
# The whole validation ladder, the fast way (Sept 25): mechanical checks, the shared WebGL build, then the
# browser suite and the slice Play Mode fixture side by side (the suite drives a browser against the served
# build, the fixture drives the Unity Editor, so they don't compete).
#
#   Tools/validate-all.sh                 # everything, both viewports
#   ART_ONLY=1 Tools/validate-all.sh      # an art-only change: no fixture, one viewport (390)
#
# Environment: UNITY (the Editor binary), PLAYWRIGHT_MODULE (an npm-installed playwright), PORT (default 8765),
# VIEWPORTS (default 390,360), SLICE_STEP (fixture step gap in seconds, default 1.5; 1.0 is too tight).
set -u
cd "$(dirname "$0")/.."
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity}"
PORT="${PORT:-8765}"
LOGS=Logs/validate-all; mkdir -p "$LOGS"
start=$(date +%s); fail=0
step() { echo "[$(( $(date +%s) - start ))s] $*"; }

rm -f Temp/UnityLockfile
step "mechanical checks"
"$UNITY" -batchmode -nographics -quit -projectPath . -executeMethod Ascendant.Build.GreyboxValidation.Run -logFile "$LOGS/mechanical.log" || fail=1
grep -E "error CS|PASS:|FAIL" "$LOGS/mechanical.log" | sort -u | tail -3
[ $fail -eq 0 ] || { echo "mechanical checks failed"; exit 1; }

step "WebGL build"
rm -f Temp/UnityLockfile
"$UNITY" -batchmode -nographics -quit -projectPath . -buildTarget WebGL -executeMethod Ascendant.Build.WebBuild.Build -buildOutput Builds/Web -logFile "$LOGS/web-build.log" || fail=1
grep "\[WebBuild\] Result" "$LOGS/web-build.log" | cut -c1-160
[ $fail -eq 0 ] || { echo "WebGL build failed"; exit 1; }

lsof -ti :"$PORT" >/dev/null 2>&1 || (python3 -m http.server "$PORT" --directory Builds/Web --bind 127.0.0.1 >/dev/null 2>&1 &)
sleep 1

step "browser suite${ART_ONLY:+ (art only: 390)}$([ -z "${ART_ONLY:-}" ] && echo ' and fixture, in parallel')"
( GREYBOX_URL="http://127.0.0.1:$PORT" VIEWPORTS="${VIEWPORTS:-${ART_ONLY:+390}}" EVIDENCE_DIR=Logs/WebEvidence \
    node Tools/validate-greybox-web.cjs > "$LOGS/suite.log" 2>&1; echo $? > "$LOGS/suite.exit" ) &
if [ -z "${ART_ONLY:-}" ]; then
  rm -f Temp/UnityLockfile
  ( "$UNITY" -batchmode -projectPath . -executeMethod Ascendant.Build.SlicePlayValidation.Begin -sliceAutoExit \
      -sliceStep "${SLICE_STEP:-1.5}" -logFile "$LOGS/fixture.log"; echo $? > "$LOGS/fixture.exit" ) &
fi
wait

suite_pass=$(grep -c '^PASS' "$LOGS/suite.log"); suite_fail=$(grep -c 'FAIL\|Error' "$LOGS/suite.log")
step "browser suite: $suite_pass passed$([ "$suite_fail" -gt 0 ] && echo ", FAILED:")"
[ "$suite_fail" -gt 0 ] && { grep -m3 'FAIL\|Error' "$LOGS/suite.log"; fail=1; }
if [ -z "${ART_ONLY:-}" ]; then
  fixture_pass=$(grep -c '^PASS' Logs/slice-play-validation.txt 2>/dev/null || echo 0)
  if grep -q '^FAIL' Logs/slice-play-validation.txt 2>/dev/null; then step "fixture FAILED:"; grep -m3 '^FAIL' Logs/slice-play-validation.txt; fail=1
  else step "fixture: $fixture_pass passed"; fi
fi
step "done$([ $fail -eq 0 ] && echo ': all green' || echo ': FAILURES above')"
exit $fail
