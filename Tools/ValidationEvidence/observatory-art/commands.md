# Validation commands

Run from the art worktree. Final tested shipped PNG hashes equal art commit `3b0145dd513a1763670f2e20d1c91752191c11f5`. The later evidence commit changes no player source.

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity
PROJECT=/Users/damusthadon/Documents/Ascendant-worktrees/codex-observatory-art
PLAYWRIGHT_MODULE=/Users/damusthadon/Documents/Ascendant-worktrees/pw/node_modules/playwright
```

Before each Unity invocation, checked process ownership and the project lock; no other Editor owned this worktree. No Assets edits while Unity ran.

```sh
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" -executeMethod Ascendant.Build.GreyboxValidation.Run -logFile Logs/observatory-art/mechanical.log
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" -buildTarget WebGL -executeMethod Ascendant.Build.WebBuild.Build -buildOutput Builds/Web -logFile Logs/observatory-art/web-build.log
```

The first build exited0. Its sphere review caught incomplete RGB palette replacement on alpha253/254 pixels; the corrected variants were prepared outside Assets while that build finished. Once Unity exited, imported the corrected8 variants, then ran the exact shared build again:

```sh
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" -buildTarget WebGL -executeMethod Ascendant.Build.WebBuild.Build -buildOutput Builds/Web -logFile Logs/observatory-art/web-build-final.log
```

Final build exited0:1028 mechanical checks,243 slots, WebBuild errors0/warnings0. Artifact was identified by `validation-identity.txt` (`observatory-art-8f24cf6-final-palettes`) and its Web.data SHA256 in validation-record.json. Free owned ports8794(final) and8795(previously proved unchanged player, identity `validate-all.DTswdq50`) were served with `python3 -m http.server --bind127.0.0.1 --directory <owned artifact>`.

```sh
GREYBOX_URL=http://127.0.0.1:8794 VIEWPORTS=360x800 DEVICE_SCALE=2 MOBILE=1 EVIDENCE_DIR=Logs/observatory-art/phone PLAYWRIGHT_MODULE="$PLAYWRIGHT_MODULE" node Tools/validate-observatory-web.cjs
GREYBOX_URL=http://127.0.0.1:8794 VIEWPORTS=1280x800 EVIDENCE_DIR=Logs/observatory-art/desktop PLAYWRIGHT_MODULE="$PLAYWRIGHT_MODULE" node Tools/validate-observatory-web.cjs
```

Phone exit0,222PASS. The first concurrent desktop Chrome stalled before producing evidence; owned node/Chrome processes were terminated with SIGTERM, no result claimed. Exact desktop command repeated with `EVIDENCE_DIR=Logs/observatory-art/desktop-retry`: exit0,222PASS. The suite was not edited.

```sh
GREYBOX_URL=http://127.0.0.1:8794 CURRENT_IDENTITY=observatory-art-8f24cf6-final-palettes BASELINE_URL=http://127.0.0.1:8795 BASELINE_IDENTITY=validate-all.DTswdq50 EVIDENCE_DIR=Logs/observatory-art/frozen PLAYWRIGHT_MODULE="$PLAYWRIGHT_MODULE" node Tools/validate-observatory-visuals.cjs
```

Exit0,40 statechecks. Compared unscaled RGB screenshots with `Pillow ImageChops.difference`: six baseline/current Wing checkpoints at each width360/1280, plus current whole versus obswhole at each width:14 comparisons, all exact, no normalization. Baseline player code equals main8f player code; its old full-run proof is retained separately.

Evidence-only Playwright helpers (source in capture-procedure.txt) run through `node /private/tmp/obs-art-extra.cjs` and `node /private/tmp/obs-door-capture.cjs`, each with the same current URL, Playwright module and `EVIDENCE_DIR=Logs/observatory-art/details`. Both exited0. Extra captured awake room, allten glyph crops and actual snapshot ring radii overlaid as cyan SVG centers. Door capture used normal motion and waited for hub + busy + observatoryDoorSeen to capture the reveal before completion. No helper changes game code.

Source hashes: Python hashlib MD5 for all26 generated source outputs, checked against every earlier output after each job; final all26 unique. PNG dimensions/alpha inspected using Pillow metadata only. Asset-inventory SHA256 verifies all22 committed sprites. Native sips fits generated canvases; explicit ImageMagick palette, alpha and annulus fitting is recorded in the concept manifest/recipes and ring-fit-result.json.

No full-game fixture run for this art pass. CI results live on the art PR. Main8f production command/result is in production-8f24cf6 and is a failure, not a positive art validation.
