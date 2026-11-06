# Observatory validation evidence

Player source `470508a2e774c516f95c090002294d1c68c0eb1a`; complete targeted flow artifact `bb09ddc` (`stage2-bb09ddc-47848428`). The final player difference draws Caspar behind the dialogue panel. No gameplay change follows the complete targeted run.

- `phone/validation.txt`: 222 checks passed, 360 × 800, scale 2, mobile/touch emulation.
- `desktop/validation.txt`: 222 checks passed, 1280 × 800, scale 1.
- `phone/*glyph-*`: ten cards; `phone/*spheres-*`: ten sphere draws, with raw browser screenshots and game snapshot JSON. Includes the proper Pluto circle/crescent/cross.
- Other phone captures show each new room/lesson/journal/Practice/ceremony/Book screen.
- `source-record.json` states artifact provenance and limitations.

Reproduce with pinned Unity 6000.3.24f1, shared `Ascendant.Build.WebBuild.Build` (`-buildTarget WebGL`), the brief's Playwright module, an owned local server and `node Tools/validate-observatory-web.cjs`. Set `VIEWPORTS=1280x800`, then `VIEWPORTS=360x800 DEVICE_SCALE=2 MOBILE=1`; supply `GREYBOX_URL` and a distinct `EVIDENCE_DIR` for each run.

The final four-PR proof is a normal single-parent commit `21d1f24464c257d7dadaa5b878451a95e272c4c6`, not a merge. It combines Stage 2 with the approved #168 date/time submit and 80px Back fixes, #167 runner and #169 fixture synchronization/release tooling. The Stage 2 PR remains independently based on origin/main. Full-run outcomes and hard stops are recorded in PR #170. No production deployment or physical Android test was performed.
