# Goal 03 Corridor working record

Base: merged main `8f24cf6f5b7ecd4943d4a31cd41ee40a33948e07`. Owner resumed after #167, #168 and #170 merged. No merges performed by this goal.

- Isolated worktree `codex-corridor`, branch `codex/corridor`.
- Observatory pattern retained: separate CorridorModel, Flow, Copy, View partials; SliceView hooks only for room/travel/journal/progression.
- Ten primary cor- art files and ten corresponding test files. Two full-bleed 720 x1600 concepts. Alpha, sizes and distinct md5s recorded in the art audit.
- Mechanical: 1,203 PASS after finished art import. Earlier stops: missing deck-size mirrors (96 to120), then assisted-placement test exposed inability to reject an occupied sign; both corrected and rerun green. Shipped checks retained.
- Copy checker: zero Section A hits. Two Section C uses of "always" explicitly address the required rulership misconception.
- Native phone script and frozen Wing/Observatory script prepared; browser runs pending final Web artifact. No full-game run yet (reserved for PR B).
- Baseline Web artifact from merged main: succeeded,47,848,692 bytes,0errors,0warnings,5m31.688s. Saved in codex-corridor-baseline/Builds/Web.
- First Corridor Web attempt failed because disk filled during Bee compilation. Confirmed exit1 and failed build report; no artifact accepted. Removed ONLY this run's disposable baseline Library after its successful build. Baseline artifact preserved. Two retries also failed; the third reached IL2CPP and filled the disk again. Local builds stopped at the three-attempt hard stop; see local-build-stop/question.md.
- Main checkout and all existing worktrees were preserved. Unity import churn in current worktree: Assets/Settings/UniversalRP.asset, ProjectSettings/ProjectSettings.asset, ProjectSettings/ShaderGraphSettings.asset; exclude from commits.

Working choices within brief: reuse shipped font glyphs for all fact-bearing marks; blank painted plaques avoid generated wrong rulerships. Seven code-rendered planet/sign plaques light at Keys7/8. Reuse the shipped door kit for the back arch's plate burn. Compact only the expanded Practice list after seven learned concepts, retaining44px targets and unchanged earlier layouts.

Remaining: final phone UI repairs/checks, frozen pixel comparisons, code/art commit and PR A/CI record; PR B Houses in full; combined full suite and fixture once (one rerun per failure); local-only combined Android branch, install/launch and room smoke captures; copy APK to Ascendant-APKs and upload to86bc8ddy2. Never merge.
