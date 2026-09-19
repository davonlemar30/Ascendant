# Zodiac Wing scene cohesion pass — 2026-09-19

## Authority and scope

Followed the approved Ascendant art direction (ClickUp Doc `2kyd583p-6954`, Decisions Log `2kyd583p-24214`) and the First Room / Stage 1 guidance: dusty, muted, shadow-led; no exterior or golden-hour illumination; keep the sealed opening on the Atrium's left; reserve the transparent lighting overlays for later progression. Gameplay layout, slot dimensions, and owner-approved wording are unchanged.

## Visual assessment

The original 390 px phone capture is [`390-wing-room.png`](Evidence/atrium-pass-2026-09-18/390-wing-room.png). It shows the room before this Wing slot art pass.

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
| `wing.png` | 720 × 1600 | Frontal, quiet stone room; plain central wall; left arch retained; wall-to-floor seam raised to about 45% of the image so it meets the fixed table footprint at phone scale; muted, shadow-led palette; no golden-hour light or mural. |
| `dial-face.png` | 664 × 664 | Dark muted circular Phoenix plate; fully transparent pixels outside its circle. |
| `shelf.png` | 100 × 72 | Compact aged-wood and iron wall shelf. |
| `shelf-book.png` | 20 × 52 | Single restrained oxblood book with pale page edge; used by the existing Wing and Stage 4 Atrium shelf slots. |
| `table.png` | 120 × 60 | Compact table with exactly twelve cells in a 3 × 4 arrangement, without drawers. |
| `chair.png` | 88 × 72 | Front-facing, short-legged chair in subdued crimson and dark wood. |

No code, layout, text, light overlays, or other game systems changed. Unity generated the PNG importer metadata for the new slots.

## Review and validation

- The final local WebGL build was reviewed in the actual Wing room at 390 × 844 and 360 × 800. The raised seam meets the table footprint at both sizes. Final captures: [`390-wing-after.png`](Evidence/wing-pass-2026-09-19/390-wing-after.png) and [`360-wing-after.png`](Evidence/wing-pass-2026-09-19/360-wing-after.png). Captures immediately before the floor-contact refinement: [`390-wing-before-seam.png`](Evidence/wing-pass-2026-09-19/390-wing-before-seam.png) and [`360-wing-before-seam.png`](Evidence/wing-pass-2026-09-19/360-wing-before-seam.png).
- The full repository browser validation recorded 262 passing checks across 390 × 844 and 360 × 800. It explicitly reached and asserted the Stage 4 Atrium return at each size. The shelf-book slot is unchanged by the final background edit; its three-book Atrium display is visible in later-stage captures [`390-atrium-books.png`](Evidence/wing-pass-2026-09-19/390-atrium-books.png) and [`360-atrium-books.png`](Evidence/wing-pass-2026-09-19/360-atrium-books.png). The style page reports 19 of 30 art slots populated and lists all six target slots as `file`.
- A clean Unity `Library` import and shared `Ascendant.Build.WebBuild.Build` completed on Unity 6000.3.24f1. After the final seam edit, Unity re-imported the background and rebuilt WebGL. Final build summary: succeeded, 0 errors, 0 warnings; output 17,692,562 bytes. Required output files are present in ignored local `Builds/Web`.
- The complete browser validation ran before the last background-only seam refinement; the final build then passed a focused Wing-room capture at both requested phone sizes. The Stage 4 verification exercised the same unchanged shelf-book art and game logic. The WebGL output was served only from localhost. Production was not changed.

## Review evidence

The before-and-after pairs show the seam refinement in the same Wing-room scene at both phone sizes. The earlier 390 px capture linked above shows the full slot-art pass against the original background. The Atrium book captures show the books after the Stage 4 return, at a later stage when they remain visible on the shelves.
