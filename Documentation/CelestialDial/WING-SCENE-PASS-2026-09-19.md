# Zodiac Wing scene cohesion pass — 2026-09-19

## Authority and scope

Followed the approved Ascendant art direction (ClickUp Doc `2kyd583p-6954`, Decisions Log `2kyd583p-24214`) and the First Room / Stage 1 guidance: dusty, muted, shadow-led; no exterior or golden-hour illumination; keep the sealed opening on the Atrium's left; reserve the transparent lighting overlays for later progression. Gameplay layout, slot dimensions, and owner-approved wording are unchanged.

## Visual assessment

The baseline phone capture is [`390-wing-room.png`](Evidence/atrium-pass-2026-09-18/390-wing-room.png).

1. `wing.png` put a large zodiac disc and phoenix tapestry behind the fixed Dial position. The detailed imagery competed with the Dial and made the central slot look pasted on.
2. The wall and floor had no quiet landing areas at the fixed shelf, table, and chair positions. Contact shadows and floor perspective did not support the props.
3. Its brass-heavy brightness and decorative detail were inconsistent with the Stage 1 dusty, shadow-dominated room.
4. `dial-face.png` had a visible rectangular field at phone scale, breaking the circular silhouette.
5. The target props had no authored art: the shelf, three very small shelf books, twelve-cell table, and chair all fell back to placeholder blocks, so they could not share materials or edge treatment with the room.

Correction order: simplify and recompose the room background; make the Dial face read as a clean circular cutout; add the shelf/books; add the twelve-cell table; add the chair. This pass keeps props small and subordinate to the room.

## Implemented artwork

Only the six approved named art slots were changed or populated:

| Slot | Imported size | Correction |
| --- | ---: | --- |
| `wing.png` | 720 × 1600 | Frontal, quiet stone room; plain central wall; left arch retained; floor junction placed behind the fixed table footprint; muted, shadow-led palette; no golden-hour light or mural. |
| `dial-face.png` | 664 × 664 | Dark muted circular Phoenix plate; fully transparent pixels outside its circle. |
| `shelf.png` | 100 × 72 | Compact aged-wood and iron wall shelf. |
| `shelf-book.png` | 20 × 52 | Single restrained oxblood book with pale page edge; used by the existing Wing and Stage 4 Atrium shelf slots. |
| `table.png` | 120 × 60 | Compact table with exactly twelve cells in a 3 × 4 arrangement, without drawers. |
| `chair.png` | 88 × 72 | Front-facing, short-legged chair in subdued crimson and dark wood. |

No code, layout, text, light overlays, or other game systems changed. Unity generated the PNG importer metadata for the new slots.

## Review and validation

- Reviewed the local WebGL build through the 390 × 844 and 360 × 800 browser viewport settings. At 390 × 844, reviewed the Wing Dial view after the replacement face imported. At 360 × 800, reviewed the Atrium phone composition and the style page. The style page reports 19 of 30 art slots populated and lists all six target slots as `file`.
- The target-size shelf-book is present in the existing Atrium slot and is visible in the style-page thumbnail at its specified 10 × 26 display size. The late Stage 4 progression state was not reached during this focused visual pass; the actual Stage 4 in-scene overlap remains to be checked.
- Clean Unity `Library` import and the shared `Ascendant.Build.WebBuild.Build` WebGL entry point completed on Unity 6000.3.24f1. Build summary: succeeded, 0 errors, 0 warnings; output 17,689,535 bytes. Required output files are present in the ignored local `Builds/Web` folder.
- The current WebGL output was served only from localhost. Production was not changed.

## Review evidence

Baseline phone capture: [`390-wing-room.png`](Evidence/atrium-pass-2026-09-18/390-wing-room.png). The corresponding 390 px post-import Wing screenshot and the 360 px Atrium/style-page screenshots were reviewed in the task browser; they are not stored as repo artifacts. The 360 px pass did not reproduce the same late Wing room state, so screenshots should not be treated as a same-scene before/after pair.
