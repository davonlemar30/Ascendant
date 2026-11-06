#!/usr/bin/env bash
# Fast orchestration regressions. No Unity, browser, or player save is touched.
set -euo pipefail
SOURCE=$(cd "$(dirname "$0")" && pwd)
TEST_ROOT=$(mktemp -d)
TEST_ROOT=$(cd "$TEST_ROOT" && pwd -P)
trap 'rm -rf "$TEST_ROOT"' EXIT
mkdir -p "$TEST_ROOT/Tools" "$TEST_ROOT/bin" "$TEST_ROOT/Logs" "$TEST_ROOT/Temp"
cp "$SOURCE/validate-all.sh" "$TEST_ROOT/Tools/"
cat > "$TEST_ROOT/bin/unity-stub" <<'STUB'
#!/usr/bin/env bash
set -eu
method= log= build=
while [ $# -gt 0 ]; do
 case "$1" in
 -executeMethod) method=$2; shift;;
 -logFile) log=$2; shift;;
 -buildOutput) build=$2; shift;;
 esac
 shift
done
case "$method" in
*GreyboxValidation*)
 echo '[GreyboxValidation] PASS: 1 checks.' > "$log"
 [ "$CASE" = mechanical-missing ] || echo 'mechanical check' > Logs/greybox-mechanical-validation.txt;;
*WebBuild*)
 mkdir -p "$build/Build"
 for file in index.html Build/Web.loader.js Build/Web.framework.js.unityweb Build/Web.data.unityweb Build/Web.wasm.unityweb; do echo stub > "$build/$file"; done
 echo '[WebBuild] Result: Succeeded' > "$log";;
*SlicePlay*)
 case "$CASE" in
 fixture-exit) exit 7;;
 missing) echo '[SlicePlayValidation] PASS: 1 checks.' > "$log";;
 stale) echo 'PASS: stale' > Logs/slice-play-validation.txt; touch -t 200001010000 Logs/slice-play-validation.txt; echo '[SlicePlayValidation] PASS: 1 checks.' > "$log";;
 truncated) echo 'PASS: partial' > Logs/slice-play-validation.txt; echo '[SlicePlayValidation] PASS: 2 checks.' > "$log";;
 *) sleep 1; echo 'PASS: fixture' > Logs/slice-play-validation.txt; echo '[SlicePlayValidation] PASS: 1 checks.' > "$log";;
 esac;;
esac
STUB
cat > "$TEST_ROOT/bin/node" <<'STUB'
#!/usr/bin/env bash
set -eu
mkdir -p "$EVIDENCE_DIR"
case "$CASE" in
 suite-exit) echo 'child stopped'; exit 9;;
 suite-missing) echo 'PASS: browser'; exit 0;;
 suite-truncated) echo 'PASS: browser' > "$EVIDENCE_DIR/validation.txt"; echo 'PASS: other'; exit 0;;
esac
printf 'PASS: browser' > "$EVIDENCE_DIR/validation.txt"
cat "$EVIDENCE_DIR/validation.txt"; echo
STUB
chmod +x "$TEST_ROOT/bin/"*
export PATH="$TEST_ROOT/bin:$PATH" UNITY="$TEST_ROOT/bin/unity-stub"
PORT=$(python3 - <<'PY'
import socket
s=socket.socket();s.bind(('127.0.0.1',0));print(s.getsockname()[1]);s.close()
PY
)
export PORT
pass=0
run() {
 export CASE=$1
 local expected=$2 actual=0
 if bash "$TEST_ROOT/Tools/validate-all.sh" > "$TEST_ROOT/result.log" 2>&1; then actual=0; else actual=$?; fi
 if { [ "$expected" = pass ] && [ "$actual" = 0 ]; } || { [ "$expected" = fail ] && [ "$actual" != 0 ]; }; then
  printf 'PASS: %s (%s, exit %s)\n' "$CASE" "$expected" "$actual"; tail -n 2 "$TEST_ROOT/result.log"; pass=$((pass+1))
 else cat "$TEST_ROOT/result.log"; echo "FAIL: $CASE expected $expected, exit $actual"; exit 1; fi
}
for scenario in mechanical-missing suite-exit fixture-exit missing stale truncated suite-missing suite-truncated; do run "$scenario" fail; done
# A real unrelated listener must be refused and must survive the runner.
python3 -m http.server "$PORT" --bind 127.0.0.1 > "$TEST_ROOT/listener.log" 2>&1 &
listener=$!
trap 'kill "$listener" 2>/dev/null || true; rm -rf "$TEST_ROOT"' EXIT
sleep .5
run wrong-server fail
kill -0 "$listener"
kill "$listener"; wait "$listener" 2>/dev/null || true
trap 'rm -rf "$TEST_ROOT"' EXIT
touch "$TEST_ROOT/Temp/UnityLockfile"
run active-editor-lock fail
[ -f "$TEST_ROOT/Temp/UnityLockfile" ]
rm "$TEST_ROOT/Temp/UnityLockfile"
# A live process with Unity's project arguments is refused even before its lock exists.
bash -c 'exec -a "Unity -projectPath $1" sleep 30' _ "$TEST_ROOT" &
editor=$!
trap 'kill "$editor" 2>/dev/null || true; rm -rf "$TEST_ROOT"' EXIT
sleep .2
run active-editor-process fail
kill -0 "$editor"
kill "$editor"; wait "$editor" 2>/dev/null || true
trap 'rm -rf "$TEST_ROOT"' EXIT
run clean pass
ART_ONLY=1 run art-only pass
grep -q 'fixture: SKIPPED' "$TEST_ROOT/result.log"
echo "PASS: $pass orchestration scenarios"
