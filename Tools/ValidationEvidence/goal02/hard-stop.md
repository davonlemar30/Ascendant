# Goal 02 Part 0: journal fixture hard stop

The authorized replacement of unknown rows/note with default-chart rows/note is implemented. Every other assertion is preserved. The combined fixture still fails at SlicePlayValidation.cs:208.

The setup at lines 113–122 first tests skip, changes that answer, then confirms a complete personal chart: 20 April 1990, 19:19, London. It explicitly asserts no Filled facts and all three personal facts present. That state persists until the journal assertion. The actual diagnostic snapshot is:

- Date: 20 April 1990
- Time: 7:19 PM
- Place: London, Britain (UK)
- journalBirthLines: empty
- canBirthAdd: false
- canBirthAddTime: false
- canBirthRising: false

SliceFlow.CanAddFacts requires incomplete personal facts; CanAddTime additionally requires a missing time. Thus preserving the assertion that both Add controls are available is incompatible with this setup. The following birth-add-time step would also require a missing date, which this setup supplies.

## Written question for Davon

May I broaden the test-only repair to split the journal fixture into explicit personal-chart and default-chart cases, preserving the existing Add-details flow and all its coverage? The current brief authorizes changing only the expected rows and note and says to keep every other check. I will not change game behavior, replace the valid personal chart with default behavior, or delete the Add checks to get green.

## Provenance

- Combined source: bb80d87db7dd5fb0d92e1c7bf2ec59913d49b292 (tree identical to release rehearsal b937938e1a562b590c0c98d8b40ef9fc38791e71).
- Journal expectation fix: 3012c2ef60fc3b28fe2d94d7f344aa2cc1714c9b; diagnostics only: 12b6160.
- Combined mechanical checks: 1,028 PASS.
- Shared local WebGL: 47,851,853 bytes, zero errors/warnings, 5m04s.
- Combined ladder evidence: Logs/validate-all.EKGvP1WP in codex-goal02-proof.
- Diagnostic repeat: Unity 6000.3.24f1 -batchmode -projectPath <combined worktree> -executeMethod Ascendant.Build.SlicePlayValidation.Begin -sliceAutoExit -sliceStep 1.5 -logFile Logs/goal02-journal-diagnostic.log; exit 1, exact state above.
- Linux rehearsal 42160687397 was dispatched on b937938, then cancellation requested while still building after the confirmed hard stop. No positive release proof claimed.

No PR is merged, no production deploy is performed by this goal, and Part 1 remains gated on Part 0. The independent full browser run passed 1,041/1,041 at VIEWPORTS=390,360 DEVICE_SCALE=2 MOBILE=1; report/stdout equality was verified by Tools/check-validation-report.py. The hardened ladder exits 1 at 1,753 seconds for the fixture failure. The diagnostic repeat changed only the assertion failure message; it began after the original fixture exited and while the independent browser was running. The original fixture log remains preserved separately. No game code or existing check was weakened for this finding.
