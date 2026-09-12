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

## Current vertical-slice status — September 11, 2026

The Celestial Dial greybox is built, deployed, and solo-accepted (owner is the only playtester; REVISE narrowed to copy clarity, then addressed in PR #6). The last three prototype locks closed on September 11: [First Reward](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24114), [Navigation](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24154), and [Room Scope](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24174). The **vertical slice v0.1** (branch `codex/vertical-slice`, [task 86bbzm11t](https://app.clickup.com/t/86bbzm11t)) wraps the Dial in the locked bookends:

1. Darkness / Identity: name entry (blank becomes "Keeper") and the birth prompt with three choices. No chart is calculated; every path uses the labeled teaching sign.
2. Grand Atrium: Stage 1 "Forgotten" placeholders and Caspar's orientation.
3. Zodiac Wing: the Dial with the Q07 props. On the sixth lit seat Caspar says the locked "Two of four. The rest will wait for you."; the Dial splits, a Keeper Key rises, "Aah... the Library stirs."
4. Grand Atrium: Caspar leads onward.
5. Crystal Book Chamber: seven sealed Books, three locks each; the Key fills 1/3; chandelier, mechanism one degree, "So he was right." End of prototype.

Screens link by Continue only. Placeholder art and copy throughout; only locked lines are verbatim. No save, no inventory (a Key indicator), no hub, no map. The shipped scene is `Assets/Scenes/VerticalSlice.unity`; `Assets/Scenes/CelestialDial.unity` stays for the Dial-only fixture. iPhone VoiceOver and Android TalkBack remain untested.

Validation of the slice (Unity 6000.3.24f1, local): mechanical validation 85/85 (flow rules and the locked completion line included); slice Play Mode fixture 16/16 with captures of every screen at 390 × 844 and the ending at 360 × 800; Dial-only Play Mode fixture 24/24; headless WebGL build 0 errors / 0 warnings; browser suite 44/44 against the served build, walking the bookends before and after the Dial in both viewports plus the recovery path.

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
