# Ascendant style pass — 2026-09-24

## Authority and scope

This pass follows the attached Scorpio and Capricorn references as the shared style authority for regenerated raster art: warm amber cinematic lighting, clean dark contours, simplified hand-painted cel-shading, rich graphic shadows, restrained detail, and grounded mature proportions. The work is art-only: no code, layout, slot sizes, labels, wording, importer settings, or light-overlay crunch settings changed.

## PR 1 — rooms and light overlays

Regenerated `wing.png` and `chamber.png` in the Atrium's cinematic 2D animated-film rendering style. Both backgrounds remain dormant and moonlit at most: deep charcoal, muted crimson, bone stone, and cool blue accents with quiet gameplay surfaces.

Regenerated `atrium-light.png`, `wing-light.png`, and `chamber-light.png` as separate RGBA overlays. The Atrium overlay is centred on the corrected Atrium window and warms the dome, frieze, arches, and phoenix floor inlay. The Wing and Chamber overlays use motivated upper shafts and restrained amber bounce while leaving broad foreground areas transparent.

### Placement check

The fixed rects were checked against the 360 × 800 composition before replacing the room art. The Wing return doorway remains at `x=-130, top=325, 70 × 100`; the floor band remains centred at `top=436, 340 × 30`; shelf/table/chair landing areas remain clear. The Chamber return remains at `x=-140, top=384, 40 × 60`; the Books remain centred at `top=300`; and the Chamber floor band remains at `top=408, 340 × 30`. The generated rooms keep the left opening, central gameplay wall, and lower floor plane aligned to those targets.

## PR 2 — Dial face, props, and character style check

Regenerated the hand-painted phoenix `dial-face` with transparent pixels outside its circle, plus `door-open`, `door-sealed`, `shelves`, `desk`, `furniture-covered`, `shelf`, `shelf-book`, `chair`, `candle`, `lamp`, and `keeper-key`. `caspar` was also regenerated to match the same warm cinematic linework while preserving his identity. The existing `table.png` is retained because it already has the required exact 3 × 4 / twelve-cell arrangement; generated candidates were rejected when they produced nine cells. After the attached Scorpio character references were supplied, `keeper-idle` and `keeper-walk` were regenerated again: African-American teen, semi-curly wet afro, black shirt, dark pants, light sneakers, warm amber rim light, rich charcoal shadows, and the attached clean cinematic linework.

This is the standing checklist for future raster art: use the attached character references, preserve registered dimensions and transparency, keep backgrounds dormant, put golden-hour energy only in `-light` overlays, and add no text or labels.

## Validation record

- PNG dimensions and alpha were checked after normalization against the registered slot sizes in `ART-SLOTS.md`.
- PR 1 shared WebGL build: passed through `Ascendant.Build.WebBuild.Build` on Unity 6000.3.24f1. The full browser suite passed at desktop density and at `DEVICE_SCALE=2 MOBILE=1`, covering 390 × 844 and 360 × 800, including the style page and all 33 art-slot sources. PR 1 captures are in `Evidence/style-pass-2026-09-24/pr1/`.
- PR 2 shared WebGL build: passed through the same entry point. The full browser suite passed at desktop density and at `DEVICE_SCALE=2 MOBILE=1` before the final two-file Keeper refresh; the refreshed branch then passed the focused 390 × 844 phone-density style-page check with both Keeper slots resolving as `file`. PR 2 captures are in `Evidence/style-pass-2026-09-24/pr2/`.
- Generated source images were kept in the local Codex generated-images folder; only selected normalized project PNGs are committed.
