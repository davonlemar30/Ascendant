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

Validation (Unity 6000.3.24f1, local): mechanical validation 136/136 (deck ladder and evidence rules, the flow through hub, seals, review, continuation, save and restore, the lesson's continuation and review phases); slice Play Mode fixture 38/38 with captures of every screen including the Hub, both review forms, the lit wheel, the resume, and Start over; Dial-only fixture 25/25; headless WebGL build 0 errors / 0 warnings; browser suite 69/69 against the served build in both viewports plus the recovery path, including the full loop, reload-resume, and Start over. iPhone VoiceOver and Android TalkBack remain untested (skipped by approval). The owner is testing after this build rather than after the copy session.

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
