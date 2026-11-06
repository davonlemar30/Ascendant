# Goal 03 Hall of Houses work record

The approved brief and merged gate were verified against main 8f24cf6. PR #173 stacks on #171; no PR is merged. Final source adjustments are isolated in codex-houses-final while the one full fixture continues against the earlier combined source in codex-hall-of-houses, so no Assets file is edited beneath a running Editor.

## Implementation and caps

- New isolated Houses model, flow, copy and view; only hooks in existing screens. Existing enums append values, preserving prior numbers.
- Save v7 carries both new models; v6 migration retains prior deck96, chart, Keys and Observatory progress. The combined deck appends kinds to156 cards; no legacy items are renumbered.
- Add the brief-authorized Atrium right-wall door after spending Key8. Room/screen/travel, journal-map and native HTML controls extend to Houses.
- DEV exact list extends by key9/key10/houseswhole, all played through model calls. Practice exact list extends to11 concepts, two new concepts of six questions each.
- Key9 independent evidence comes from overlay turns; Key10 evidence comes from house turns. Reveals and Ask Caspar never qualify. Assisted passes pause each Key; independent replay preserves completed cards.
- Third Book opens through the shipped ceremony at lock9; lock10 starts Book4. Existing lock capacity was sufficient.
- Houses chapter replaces the last Sealed row only after Key9. The original row remains intact before the threshold.
- Ten hoh slots extend253 to263. All primary hashes differ from their test counterpart. Both mockups are720×1600, full bleed. `art-audit.json` lists dimensions, alpha ranges and md5s.

## Validation commands and source

- Unity6000.3.24f1: `-batchmode -nographics -quit -projectPath <worktree> -executeMethod Ascendant.Build.GreyboxValidation.Run -logFile Logs/houses-final-mechanical.log`:1560PASS. Final art source397e2b5.
- Same mechanical command after phone display changes on6e1c2ef:1560PASS, zero compile errors. `mechanical-display.txt`.
- `python3 ~/.claude/skills/ai-ism-pass/check.py Tools/ValidationEvidence/houses/copy-table.md`:0sectionA. SectionC "always" warnings occur in misconception distractors.
- CI workflow dispatch42192545171, source397e2b5: Web success; Guard42192682704 success. ZIP SHA2567971de1967d64069156f67345d5212f4fc9ce6aa4937a9119c7594bff012a4d8 verified.
- `PLAYWRIGHT_MODULE=<pw>/node_modules/playwright GREYBOX_URL=http://127.0.0.1:8793 EVIDENCE_DIR=.../phone-replay-sync node Tools/validate-houses-web.cjs`:PASS,111 phone captures360×800 at density2. First attempt stopped at retained replay intros; test then followed the same two Continue calls already exercised by the mechanical suite. Key10 assertion retained.
- `BASELINE_URL=http://127.0.0.1:8790 GREYBOX_URL=http://127.0.0.1:8793 BASELINE_IDENTITY=main-8f24cf6-local CURRENT_IDENTITY=ci-397e2b5-run42192545171 ... node Tools/validate-corridor-frozen.cjs`, then `python3 Tools/compare-corridor-frozen.py <evidence>`:82/82 pixel-identical at360×800 density2 and1280×800 density1.
- Full browser suite started once on source397e2b5, `DEVICE_SCALE=2 MOBILE=1 ... node Tools/validate-greybox-web.cjs`; outcome appended when complete. Full capture archive stays in the original worktree Logs/evidence; its report and manifest are retained for review.
- Native slice fixture: `Unity -batchmode -projectPath <combined-worktree> -executeMethod Ascendant.Build.SlicePlayValidation.Begin -sliceAutoExit -sliceStep 1.5 -logFile Logs/houses-final-fixture.log`. First run failed waiting for launch studio-logo; one permitted rerun uses the same command with a separate log. Outcome appended when complete. No assertion removed.

## Reversible obvious calls

Use existing controls, hint ladders, key/book ceremonies and semantic layer; choose the two alternative Ascendants four/eight signs from the personal rising sign; keep the old journal row before Key9; separate shell, instrument and props; align each new room's Keeper feet to its painted floor; allow zodiac glyph text overflow as the shipped glyph renderer does; keep horizon/meridian axes fixed while the zodiac overlay turns; mark the computed MC with a visible diagram point and label. These make the approved layout work and add no curriculum fact.

## Stopped work

Local player builds stopped after three disk/cache failures under the goal01 hard-stop rule. The exact question and three logs are retained in #171 and `../corridor/local-build-stop`. CI artifacts unblocked browser work; the slice fixture is independent. No fourth local Web/Android build, APK, emulator install or ClickUp attachment is claimed. The owner's answer is still pending for the stopped player builds.

## Excluded local files

ProjectSettings import churn and rejected dormant/light art drafts. No owner checkout, existing worktree or original Observatory image was edited. Unity-generated metadata is retained without manual whitespace cleanup.

## Final fixture outcome

First invocation: FAIL, launch studio-logo timeout, exit1. The single permitted rerun: **471 PASS, zero runtime errors, exit0**. `fixture-first-report.txt`, `fixture-first.txt`, `fixture-rerun-report.txt` and `fixture-rerun-log.txt` preserve both outcomes. The rerun includes the two small authorized Practice explanation wording cleanups; its player model and UI source is the earlier combined397e2b5, before the final phone-only display corrections on6e1c2ef. Those corrections receive focused phone captures and a fresh compile/mechanical run, without another full fixture.

## Final display and copy verification

CI source6e1c2ef, Web42194673408 and Guard42194682339 passed. The verified artifact SHA256 is440ed51e99801b45a475e5d052f22a7af99c81801d8e8d669f43830b9d301c67. `validate-goal03-display.cjs` passes, with12 focused360×800 captures at density2: both painted-floor positions, angle/overlay levels1–3, visible zodiac overlay, Book3 and the computed MC outside house10. The final room and chart captures were visually inspected. Common buttons were then moved from references to CorridorCopy into identical annotated const strings in HousesCopy so all Hall labels appear in its own draft table; rendered text is unchanged. `mechanical-copy-final.txt` confirms1560PASS and zero compile errors after that move.

## Full browser outcome

**1041 PASS, zero failures**, first run, exit0, no rerun used. Report bytes match the subprocess log after trailing-newline normalization. Source397e2b5, combined CI artifact noted above. DEVICE_SCALE=2 MOBILE=1; default390×844 and360×800 flows, then the suite’s existing wide/art/shape checks. `full-browser/validation.txt` is the complete report; `archive-manifest.json` hashes every capture and names the retained local archive. Final phone-only corrections were separately compiled and visually checked; no shipped screen was changed.
