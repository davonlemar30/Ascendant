# Atrium visual-cohesion assessment

Reviewed against the approved ClickUp Visual Language (`2kyd583p-24354`), art direction (`2kyd583p-24954`), and Decisions Log (`2kyd583p-24214`). The Stage 1 scene remains dusty, muted, and shadow-led. No golden-hour light was added; later light overlays remain separate. The sealed opening stays on the left, and the owner-approved wording and gameplay layout are unchanged.

## Most visible cohesion problems

| Order | What reads as disjointed | Source | Recommended correction |
| --- | --- | --- | --- |
| 1 | The Atrium background already paints stone arches around its three recesses. The old open and sealed door cutouts added their own masonry arch and jamb, creating a second portal outline at phone scale. | `Resources/Art/atrium.png`; `Resources/Art/door-open.png`; `Resources/Art/door-sealed.png` | Let the room illustration own the masonry. Keep the transparent door cutouts to the door leaves and their narrow leaf hardware, at the existing 140×200 source size and current gameplay placements. **Implemented in this pass.** |
| 2 | The small gray labels cross high-detail stone, banners, shelving, and floor. “Shelves, mostly bare” overlaps the shelf; “Sealed door” sits on the recess; “Covered furniture” falls over the floor, while the opening line crosses the upper architecture. At 360×800 the type loses contrast and reads as loose debug annotation. | `atrium.png`; Atrium label/interaction presentation in the current scene implementation | Keep the approved wording and interaction layout. Before repositioning or restyling, get an explicit decision on whether labels should be anchored to objects, placed in reserved negative space, or given another treatment; the approved docs reviewed here do not settle that choice. |
| 3 | The Caspar dialogue rectangle covers a large part of the lower room and has a clean, screen-space edge against the detailed floor. The scene reads as a full illustration behind an unrelated interface layer. | Existing dialogue presentation and `atrium.png` | Preserve the approved copy and current gameplay layout. Decide how the dialogue treatment should relate to the illustrated room before changing its panel, border, or position; no such treatment is specified in the sources reviewed. |
| 4 | The door hardware retains crisp brass and burgundy accents while the surrounding stone is broad, dusty, and low-contrast. These details compete slightly at phone size, though the current room palette and existing door palette are both approved. | Door cutouts and `atrium.png` | Keep the door ornament subdued and avoid adding a new light source. Revisit only if a later approved palette decision calls for further desaturation; do not brighten the Stage 1 room or bake in progression lighting. |

## Correction and review

The door cutouts were regenerated to remove the separate stone casing. The sealed door still reads as a door leaf inside the existing left recess; the open version retains narrow leaves and a clear opening. Sprite dimensions, alpha, slots, scene placements, labels, and wording are unchanged.

The updated scene was reviewed in the local WebGL build at 390×844 and 360×800. At both sizes the door stays inside the left recess and no longer brings an extra stone jamb into the composition. The phone captures from the earlier implementation remain at `Evidence/atrium-pass-2026-09-18/390-atrium.png` and `Evidence/atrium-pass-2026-09-18/360-atrium.png`; updated review captures were shown in the task conversation.

## Validation

- Unity `6000.3.24f1`, clean detached checkout at `aeda8fa`, with only the two updated door PNGs copied in: WebGL build succeeded, BuildReport 0 errors / 0 warnings, output size 17,608,253 bytes. Required loader, framework, wasm, data, and `index.html` files were present.
- Local browser review completed at 390×844 and 360×800. Browser console returned no warnings or errors.
- The shared production site was not changed. Pull request CI remains required before merge.

See also: [the scene pass this assessment reviews](ATRIUM-SCENE-PASS-2026-09-18.md) (PR #30). The light overlay slots this document defers to were added in Build H ([ART-SLOTS.md](ART-SLOTS.md)).
