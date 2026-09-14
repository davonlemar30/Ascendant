# Ascendant documentation

Approved ClickUp documentation is the design authority. This directory provides an engineering entry point, not replacement product canon. Read the repository's [agent instructions](../AGENTS.md) before changing the project.

## Celestial Dial milestone

| Record | Authority |
| --- | --- |
| [Celestial Dial greybox specification](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24294) | Active prototype and test contract; gameplay validation pending |
| [Visual direction only](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24314) | Hierarchy and broad visual direction; not final art or zodiac geometry |
| [Canonical Celestial Dial](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24094) | Locked interaction and evaluator |
| [Mastery & Mistakes](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24194) | Locked assistance, recovery, evidence, and reappearance rules |
| [Curriculum Architecture — Canon](https://app.clickup.com/90141007990/docs/2kyd583p-6854/2kyd583p-24274) | Curriculum, including amendments and operational clarification |
| [Implementation/test task](https://app.clickup.com/t/86bbyd0vw) | Open milestone and observation record |

Read the complete records and their appended clarifications. If requirements conflict without an explicit reconciliation, stop and surface the conflict before changing either source. Do not infer decisions for areas marked open.

## Engineering checkpoint — September 11, 2026

The completed deployment proof supersedes the older ClickUp text describing WebGL and deployment prerequisites as unfinished. [PR #1](https://github.com/davonlemar30/Ascendant/pull/1) introduced the shared build and deployment pipeline. Local WebGL, GitHub Actions, and production deployment were validated in that milestone, as confirmed by the milestone owner's engineering handoff.

Production: [Ascendant](https://davonlemar30.github.io/Ascendant/).

- Use Unity `6000.3.24f1`, pinned in `ProjectSettings/ProjectVersion.txt`.
- Local and CI WebGL builds use `Ascendant.Build.WebBuild.Build` in `Assets/Editor/Build/WebBuild.cs`.
- `.github/workflows/web.yml` builds pull requests into `main`; only `main` publishes to production. PR preview deployment is intentionally unavailable.
- Before starting the greybox, merge the documentation prerequisite with required checks green, then branch from updated `main` as `codex/celestial-dial-greybox`.
- Preserve unrelated local Unity changes and untracked files. Do not include them in the milestone.

## Current vertical-slice status — September 12, 2026

The Prototype Brief's locks are complete: Q05 First Repeatable Loop was locked on September 12 with all seven decisions as proposed ([05](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24134), [Decisions Log continuation](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24574)). The **v0.2 "the return"** build (branch `codex/v02-the-return`, [task 86bbzm11t](https://app.clickup.com/t/86bbzm11t)) adds the loop on top of the copy-session slice:

- **The return.** After the Chamber ending, Continue leads to the Grand Atrium in Stage 2 "Stirring": one lamp lit, the desk uncovered, the Zodiac Wing door open, the other doors sealed with locks. Two entrances: the Zodiac Wing and Check the Seals. Test-only controls: "Advance one day" and "Start over."
- **Check the Seals.** The twelve sign-element items enter the review deck as Introduced when the Hub is first reached, on the locked 1 → 3 → 7 → 14 → 30 day ladder. A batch of six due items alternates the compressed Dial (start seat framed, one line, Seal; one nudge then reveal) and direct tap of the element. Only Level 0/1 answers advance items; a miss steps back one interval. Reviews never light seats.
- **Unit 1.1.** The Wing continues with the two remaining families at Level 0 on the same lesson model, lights the wheel, awards no Key (Key 2 belongs to glyphs), and returns the player to the Atrium in Stage 3, the v0.2 end boundary.
- **Session save.** Local browser save of sun sign, lit seats, families, Key, deck, day offset, and stage; a second sitting resumes at the Hub; Start over wipes it. No account, no server.
- **Also in this build (owner requests during the pass):** the center readout that names the sign under the bracket is back, as the sign name alone; the accessibility overlay's stale "framed" lookup is fixed, which had left a focus box labeled with the first sign parked at the bracket, and that focus box now shows only for keyboard users. Caspar's v0.2 lines are placeholders for the owner to write (copy deck section 7).

The **v0.3 "glyphs and Key 2"** build (branch `codex/v03-glyphs`, [task 86bbznawx](https://app.clickup.com/t/86bbznawx)) finishes Unit 1.1 in the form the owner chose on September 12, both options combined:

- **Part A, name the mark.** From the lit Wing, twelve cards in zodiac order, each showing one mark with four names (the mark's own sign and the three signs in the same seat family). One nudge names the element; a second miss reveals the name and moves on. Twelve glyph items enter the review deck as Introduced when Part A begins.
- **Part B, find the mark.** The wheel hides its names and keeps its marks. Caspar names a sign; the player turns until its mark sits under the bracket and presses Seal. Same hint ladder; Level 2 reveals the name on its seat, Level 3 counts forward from Aries. Seat labels never name a hidden seat.
- **Key 2.** Earned after both parts when at least one Part B placement was unassisted (Level 0 or 1); twelve assisted placements pause without a Key. The Atrium reaches Stage 4 with a second lamp, "Keeper Keys: 2", and the v0.3 end card. Check the Seals now reviews glyph items in a glyph form alongside the element items. Reload resumes with Key 2.
- **Marks.** Unicode zodiac symbols in Noto Sans Symbols (OFL, under `Assets/CelestialDial/Resources/Fonts`), placeholder glyph art. Caspar's glyph lines are placeholders (copy deck section 8).

The **v0.4 "tap-to-move"** build (branch `codex/v04-tap-to-move`, [task 86bbznax8](https://app.clickup.com/t/86bbznax8)) is the movement greybox locked under Q06 phase 2 ([brief](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24614)): does walking the Library make it feel inhabited, on a phone, without slowing the loop?

- **Two walkable rooms.** The Grand Atrium (Wing doorway, desk, Caspar; sealed doors only say they are sealed) and the Zodiac Wing as a room (the Dial, the doorway back). The opening and the Chamber stay on Continue.
- **The Keeper.** A placeholder upright marker with a walk bob and one idle pose; no face, no clothing, no Create-a-Player. Straight-line walk at a fixed speed along a floor band, no pathfinding. Arriving at a doorway fades to the next room; reduced motion jumps and cuts.
- **Parity.** The Atrium buttons and "Back to the Atrium" stay and route through the same walk; the semantic layer lists every point of interest. Fixed portrait framing per room at 360 × 800.
- **Chamber page-turn (fix after the owner's first v0.4 attempt).** Since the copy-session build the Chamber's three Caspar pages could only be turned through the hidden screen-reader Continue, so touch players stalled on a disabled "Insert the Key". A visible Continue now turns the pages and the glowing Insert appears on the last page; the browser suite turns those pages by tapping the canvas button.
- **After the owner's v0.3 marks playtest (Sept 13).** The seat marks were blank on the Web since the v0.4 layout edit, which had turned the rest of the seat-mark setup line into a comment and left those labels on the plain text font; restored, and the symbol font is now baked at import (a static twelve-character texture, no runtime rasterization). The Count button no longer shows in Part B. The v0.3 decision is REVISE (owner); the design requests from that pass (a separate instrument for the marks, an ask-Caspar hint, clearer objective wording, difficulty after a clean run) are recorded on task 86bbznawx for decision briefs.
- **Owner rulings after marks test 2 (Sept 13).** No in-game time: the review ladder keeps its 1, 3, 7, 14, 30 shape but counts sittings (completed Check the Seals batches), learned items are ready at the next check, and the "Next day (test)" button is gone (Q05 amendment on the Decisions Log). "Symbol" replaces "mark" in every player-facing line. The Part B readouts show the symbol under the bracket, never a name.
- **v0.3 revision, build 1 (Sept 13 lock): the book on the shelf.** Part A of the symbol unit (name the symbol, four names) now lives in a book on the Wing's collapsed bookshelf, which wakes once all twelve seats are lit; the Keeper walks to it, a book opens, and the twelfth name closes it. Part B (find the symbol by turning the wheel) and Key 2 stay on the Dial. Tapping the Dial before the book only points at the shelf; tapping the shelf before the wheel is lit says it is dark. Amends 07 First Room Scope (one interactive object besides the Dial) and Q06 phase 2 (a third point of interest, with a button for parity).
- **Test variables.** Walk speed (test toggle: normal, fast, slow), floor band, fade length. The event log carries walk and room events with response times so the Chamber-to-Dial time can be compared with v0.2.

Validation (Unity 6000.3.24f1, local): mechanical validation 182/182 (adds the room tables, the walker's legs, arrival, jump, speed toggle, and the flow through the Wing room); slice Play Mode fixture 49/49 with captures of the Atrium with the Keeper, the Wing room, and the rest; Dial-only fixture 25/25; headless WebGL build 0 errors / 0 warnings; browser suite 91/91 against the served build in both viewports plus the recovery path, including a sealed door, walking to Caspar, the doorway fade, the Dial from the room, and the walk back. iPhone VoiceOver and Android TalkBack remain untested (skipped by approval). The owner tests after this build.

Previous validation, v0.3: mechanical validation 162/162 (adds the sun-sign table, glyph Part A and B, Key 2 evidence rule, glyph deck items and review, restore with Key 2, and the font's twelve symbols); slice Play Mode fixture 43/43 with captures of Part A, Part B, Key 2, the Stage 4 Hub, and the resume; Dial-only fixture 25/25; headless WebGL build 0 errors / 0 warnings; browser suite 77/77 against the served build in both viewports plus the recovery path, through Part A, Part B, Key 2, Stage 4, reload-resume, and Start over. iPhone VoiceOver and Android TalkBack remain untested (skipped by approval). The owner tested after v0.4 instead.

**Versioning:** the Unity project reports bundle version `1.0`. `0.1.0` would be a clearer early-prototype label; changing it needs a separate release/versioning decision.

## Greybox boundaries and delivery

The prototype tests the six-seat Unit 0.1 learning flow at portrait phone sizes using placeholder geometry and text. All input methods share selection and evaluation; learning mode uses explicit Move → Inspect → Seal. The linked specification governs assistance, fresh-equivalent evidence, capped recovery, accessibility, event logging, and manual and automated validation.

Reward structure, the larger repeatable loop, navigation, room scope, final artwork, and final timing remain open. Do not expand this milestone into those areas. Temporary awakening and conditional Keeper Key 1 do not claim Retained, Sealed, or permanent restoration.

The implementation PR must separate:

- Implemented and mechanically verified.
- Human-tested and observed.
- Still provisional.
- Still open by design authority.

Prepare observation materials for three to five people and record human testing as pending until it actually occurs. The implementation task stays open until observations support a documented PASS, PASS AS TEACHING TOOL, REVISE, or FAIL decision. A successful software build does not validate the mechanic.
