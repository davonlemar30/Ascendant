# Goal 02 Part 0: Linux cinematic harness hard stop

No PR has merged. No production deployment has occurred. Part 1 remains gated on Part 0.

The goal 01 brief, carried into goal 02, requires stopping a problem after three attempts or about 90 minutes. The Linux cinematic synchronization problem has reached that stop. No further repair or rehearsal is authorized by elapsed time alone.

## Current failure

Rehearsal 42168038641, source a7b253f (combined local tree 71e5441): Build Web player passed. Both Linux browser jobs failed:

- Phone job127731697602: `in the launch movie a canvas tap shows Skip and advances nothing at 390`.
- Desktop job127731697617: `the opening scene: shot 3 (light), prologue-light, its spoken line announced ("A white-gold light floods his room. He looks up and pulls his headphones down."), the semantic Skip offered at 390`.

The Linux fixture also failed at `at half, six symbols (Aries to Virgo)` (job127731697601). Release artifact cleared, deploy and production jobs were skipped. The independent Mac full ladder completed with exit0 at1,548seconds: mechanical1,028PASS, fixture471PASS, browser1,065/1,065PASS at390/360scale2/mobile. Browser report/stdout equality and complete fixture evidence were verified. This local pass does not clear the Linux failures.

## Attempts and evidence

1. cd9f941 observes the short glyph animation inside the page and waits for the writing loop to run notebook-2, notebook-3 and back to notebook. The complete Mac browser pass on an earlier combined source is preserved in the parent goal02 evidence (1,041/1,041 phone density).
2. The first completed Linux browser attempt on 5d91f7b, run42164794999, still caught alive-shot snapshot drift (phone) and a tap across the desk/notebook transition (desktop).
3. 95fbeb2/87b6a50 observe launch phase samples in-page after initialization; 09734dd moves the prologue tap to the stable wide shot before screenshot latency. The extracted actual helpers pass at390 and360 phone density on the Mac. Full Linux run42168038641 now catches the remaining launch-input/prologue-light synchronization above.

All original cinematic assertions remain. There is no game timing change, skipped check, retry-until-green gate, or claimed positive release clearance. Rat timing a7b253f uses the existing real-player-frame relaunch mechanism with its motion assertions unchanged; this is not a cinematic pass claim.

## Written question for Davon

May I continue beyond the brief's repeated-attempt/time hard stop with a separate, bounded Linux cinematic-harness repair, using the existing Web artifact for focused diagnosis, preserving every interaction and animation assertion and making no game changes? All four PRs will remain unmerged until the complete hardened ladder and a normal full release rehearsal pass. My recommendation is yes; stop again if diagnosis shows a game bug or requires weakening coverage.

## Local focused counts

- local-prologue-focused.txt: 42 PASS, exit0.
- local-launch-focused.txt: 52 PASS, exit0.

## Measured Linux job runtimes

- Build Web player: success, 444 seconds.
- Release slice fixture: failure, 341 seconds.
- Release browser (phone): failure, 93 seconds.
- Release browser (desktop): failure, 117 seconds.
- Release artifact cleared: skipped, not run.
- Deploy to GitHub Pages: skipped, not run.
- Check published player: skipped, not run.

## Complete local proof

- Combined source:71e5441be7e61f9da641bc4250396ebb8be9ecff, tree identical to #169a7b253f. Later commits add evidence only.
- `PORT=8792 VIEWPORTS=390,360 DEVICE_SCALE=2 MOBILE=1 PLAYWRIGHT_MODULE=<brief path> bash Tools/validate-all.sh`: exit0, all green, 1,548seconds.
- Mechanical:1,028PASS. Fixture:471PASS and complete marker. Browser:1,065PASS, zeroFAIL; exactreport/stdout equality verified by `python3 Tools/check-validation-report.py <browser report> --browser-log <stdout>`.
- Shared Unity6000.3.24f1 WebGL:47,849,252bytes, zeroerrors/warnings,18.576751seconds cached build.
- Full local fixture, mechanical, browser reports and console are beside this question. Existing focused scripts/reports are retained for reproducibility; their passes are not substituted for Linux proof.
- No merge, deploy, production check, art generation, proof-branch deletion or worktree removal occurred. Part1 remains gated on Part0.
