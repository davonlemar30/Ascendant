# Observatory art record

Source: main `8f24cf6f5b7ecd4943d4a31cd41ee40a33948e07`. Davon explicitly authorized starting Part1 from this main while the bounded #169 harness repair runs separately. Art branch: `codex/observatory-art`. Art PR stays open for Davon's visual judgment.

## Approved authority and reversible fitting choices

The Art Bible (ClickUp doc `2kyd583p-6994`, page `2kyd583p-25174`) and goal02's four approved art calls govern: cool dormant domed library, warm awake lighting with retained cool shadows, distinct planet colors, full-bleed9:20 composition. No painted text, glyphs, characters or standalone furniture in the room/shell.

- Shipped art lives beside the Wing kit in `Assets/CelestialDial/Resources/Art/`; all22 test images remain untouched. Existing slots, code, layout and import policy remain unchanged.
- Full room canvases are720×1600; frame and rings600×600; planet sprites96×96; dormant sprite64×64; lecterns128×200; charts200×160. Native image output is fitted to the established2× canvas without letterboxing.
- The room carries full architecture; shells are transparent structural trim, aligned to the same composition. This allows the existing light/grime layers beneath the kit to remain visible. Worn/restored pieces retain their pose and silhouette.
- The room is architecture only. The central floor ellipse is an inlay; the orrery, lectern, charts and spheres load separately from existing slots.
- Grime alpha is multiplied by0.60 and warm light by0.58, close to the shipped Wing's overlay strength. The awake light is one upper-right shaft and floor pool, leaving the cool left/deep shadows.
- The generated frame's circular head is fitted to the existing center; its stepped base is compressed within the existing canvas. Generated ring annuli are fitted to the game's radii20,75,95,109,123,137,151,165. No geometry code changes.
- Generated sphere edits were fitted to a clean common silhouette, exact flat palettes and retained alpha, removing generator gradients/alpha blemishes. Jupiter retains its generated bands; Saturn retains its generated ring. Palette fitting explicitly disables alpha for RGB replacement and restores it separately, so near-opaque pixels are recolored too. No glyph is painted on a sphere.
- Every generator output is preserved with its prompt, dimensions, MD5 and chosen/rejected status in `Documentation/ConceptArt/Observatory-2026-11/art-pass/prompts.json`. Each hash was checked against every earlier output before the next job; all26 are distinct.

## Production check before the art pass

Main8f deployed successfully in run42176532471. The live Web.data.unityweb matched that Pages artifact by SHA256. Main8f's own unmodified production suite at390/360, scale2, mobile exited1 at390: `reduced motion: no writing loop, the notebook held (desk light light-up look headphones)`. This is a recorded production failure, not a complete pass; see `production-8f24cf6/record.json` and console. The bounded #169 repair is tracked separately. No production game change or retry was made for this art pass.

## Validation results

Pinned Unity6000.3.24f1 mechanical checks: exit0,1028 checks;243 art slots unchanged. No full-game fixture is authorized or run for this art-only pass. Final shared WebGL build: exit0, zeroerrors/warnings. Stage2:222PASS desktop1280×800 atscale1;222PASS phone360×800 atscale2/mobile. Frozen Wing:40 statechecks,14 exact pixelcomparisons, allidentical. Evidence-only captures include the actual normal-motion door reveal, awake room, allten48pxglyphs and a ring-center overlay. Source art is commit3b0145dd513a1763670f2e20d1c91752191c11f5; tested PNG hashes are inasset-inventory.json. The first concurrent desktop Chrome process stalled before producing any evidence; it was terminated and the unchanged sequential retry passed. No failed assertion was changed. CI is pending.

The whole Observatory checkpoint reports existing `observatoryLevel:2`; the current code applies its level2 grime/veil/light strengths. Remaining visible dust at that checkpoint is the existing state model, not dust painted into the clean restored shell. Code and narration are unchanged.
