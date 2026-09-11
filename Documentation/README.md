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

The Celestial Dial greybox is implemented, deployed, and solo-playtested by the owner (the only playtester). The mechanic and flow are provisionally accepted: movement felt smooth, snapping and inertia were acceptable, alternate inputs matched dragging, the six-seat lesson and the optional problem completed, and both portrait layouts fit. The recorded decision on [task 86bbyd0vw](https://app.clickup.com/t/86bbyd0vw) is **REVISE for copy clarity only**. iPhone VoiceOver and Android TalkBack remain untested.

The clarity issue was wording, not rotation: "Forward" felt visually contradictory because the wheel appears to move downward when advancing and upward when reversing. The copy-clarity pass (branch `codex/celestial-dial-copy-clarity`) therefore uses less visually loaded wording and changes nothing else:

- Action sequence **Move > Inspect > Seal**; readout **Framed: [sign]**; step controls **Previous** and **Next**; introductory label **Teaching sign: Taurus**.
- Counting explained as "Start at [sign]. Count each sign after it: one, two, three, four. Inspect the framed sign, then press Seal."
- No player-facing "under the marker", "counts as 0", "your start sign is zero", or up/down/clockwise wording, in Unity UI, the WebGL template, or the accessibility labels.
- Evaluator, rotation direction, snapping, inertia, recovery limits, evidence rules, reward logic, input parity, and layout geometry are unchanged.

Validation of the copy pass (Unity 6000.3.24f1, local): `git diff --check` clean; stale-string sweep clean; mechanical validation 75/75; Play Mode validation 24/24 with captures at 390 × 844 and 360 × 800; headless WebGL build 0 errors / 0 warnings; browser suite 29/29 against the served build. Untested: VoiceOver / TalkBack on a device.

**Versioning:** the Unity project reports bundle version `1.0`. `0.0.0.1` is not the configured version and is a four-part label rather than semantic versioning. `0.1.0` would be a clearer early-prototype label, but changing the project version is outside the copy pass and needs a separate release/versioning decision.

## Greybox boundaries and delivery

The prototype tests the six-seat Unit 0.1 learning flow at portrait phone sizes using placeholder geometry and text. All input methods share selection and evaluation; learning mode uses explicit Move → Inspect → Seal. The linked specification governs assistance, fresh-equivalent evidence, capped recovery, accessibility, event logging, and manual and automated validation.

Reward structure, the larger repeatable loop, navigation, room scope, final artwork, and final timing remain open. Do not expand this milestone into those areas. Temporary awakening and conditional Keeper Key 1 do not claim Retained, Sealed, or permanent restoration.

The implementation PR must separate:

- Implemented and mechanically verified.
- Human-tested and observed.
- Still provisional.
- Still open by design authority.

Prepare observation materials for three to five people and record human testing as pending until it actually occurs. The implementation task stays open until observations support a documented PASS, PASS AS TEACHING TOOL, REVISE, or FAIL decision. A successful software build does not validate the mechanic.
