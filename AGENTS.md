# Ascendant Agent Instructions

These instructions apply to the entire repository.

## Design authority

- The approved Ascendant design documentation in ClickUp is the canonical source for product, narrative, curriculum, character, interaction, and visual decisions.
- Repository assets and implementation must follow that documentation. When repository content and an approved ClickUp specification conflict, stop and surface the conflict before changing either source.
- Treat an area as open unless the design authority explicitly resolves it. Agents must not invent missing mechanics, content, lore, character traits, visual language, curriculum, UX behavior, or acceptance criteria.
- When a task depends on an open design area, identify the missing decision and request direction. A clearly labeled, reversible placeholder is acceptable only when the task explicitly authorizes one.

### The obvious-call rule (owner, 2026-10-06)

- Agents settle the calls any reasonable owner would obviously make, log each one, and keep building. An obvious call makes the build match what is already approved or already shipped: centering what the layout means to center, aligning to the existing grid, matching existing spacing, sizes, and patterns, fixing typos, and staying consistent with shipped screens.
- It never adds or changes lore, curriculum, characters, mechanics, UX behavior, or visual language, never decides something a brief leaves open on purpose, and never touches a locked ruling. Those still stop and ask. When in doubt, it's a real decision.
- Each obvious call taken in a build is listed in its PR and logged on the Decisions Log as a working choice, dated. All of them are reversible, and Jeffrey checks that none crossed the line. Asking the owner an obvious call ("do you want it centered?") counts as a miss.

## Unity project

- Use Unity `6000.3.24f1`, the version pinned in `ProjectSettings/ProjectVersion.txt`.
- Preserve project structure, serialized Unity assets, `.meta` files, GUIDs, and existing import settings.
- Do not hand-edit Unity-generated files when the same change should be made through Unity. Do not commit generated directories such as `Library`, `Temp`, `Logs`, or local build output.
- The shared headless WebGL entry point is `Ascendant.Build.WebBuild.Build` in `Assets/Editor/Build/WebBuild.cs`. Local and CI builds must use the same entry point.

## Git workflow

- Start work from an up-to-date `main` and make scoped changes on a feature branch. Use the `codex/` branch prefix unless the task specifies another branch name.
- Keep commits focused. Do not mix unrelated editor-generated changes, package changes, migrations, or local settings into a task.
- Never discard, overwrite, or include pre-existing uncommitted work without explicit approval.
- Open a pull request into `main`; do not merge until the required validation is green and the user has authorized the merge.

## Validation

- Before handing off code, confirm Unity imports and compiles with zero new errors. Report warnings and distinguish pre-existing warnings from regressions.
- For gameplay or UI changes, test the affected scene or flow in the Unity Editor and verify the task's acceptance criteria. Include visual evidence when appearance or interaction is material.
- For WebGL-affecting changes, run the shared headless WebGL build and verify the required output files. Pull requests must pass the GitHub Actions Web build; production deployment changes must also pass the Pages deploy job and be checked at `https://davonlemar30.github.io/Ascendant/`.
- Record what was tested, the result, and anything not tested. A successful compile alone is not proof that a visual or interaction requirement is correct.

## Where things are

- `Documentation/README.md` is the engineering index and the build log; `Documentation/CelestialDial/VALIDATION.md` is the per-build technical record; `Documentation/CelestialDial/ART-SLOTS.md` mirrors the slot manifest in `Assets/CelestialDial/Slots.cs` (the manifest is the source of truth; add a slot there first). The root `README.md` is for players and the owner, not engineers.
- The validation ladder for a build, in order: mechanical checks (`Ascendant.Build.GreyboxValidation.Run`, batch mode), the slice Play Mode fixture (`Ascendant.Build.SlicePlayValidation.Begin`), the headless WebGL build, then the browser suite (`Tools/validate-greybox-web.cjs`) at desktop and phone density against the served build, and against production after the deploy. A build's pass record and captures go on its ClickUp task.
- Copy and counts are quoted in several places at once: the Editor checks, the browser suite, the web template's labels, and the docs. A change to a line of Caspar's or to the slot count must sweep all of them.
- Decisions live in the ClickUp Decisions Log (doc 2kyd583p-6954, page 2kyd583p-24214); build briefs are ClickUp tasks in the Development list. Record a design choice taken inside a build's latitude on the Decisions Log, dated, as a working choice.
- Docs upkeep belongs to Whitney, the documentation agent defined in `.claude/agents/whitney.md` (owner, Sept 25). Claude calls her at the end of every build, once its PR has merged; she also runs on Mondays (opening the week's Decisions Log page) and Thursdays (a stale sweep and a dead-branch sweep). She fixes mirrors, indexes, and records, never rulings, and posts her report on the ClickUp task "Whitney — docs upkeep".

## The studio team (owner, 2026-10-06)

Claude builds and orchestrates. Five agents, each defined in `.claude/agents/`, check the work at fixed points or answer questions. None of them rules on design; every ruling stays with the owner.

| Agent | Job | Claude calls them | Model |
| --- | --- | --- | --- |
| Ashantis | Task prep: settles obvious calls, lists the real decisions | Before building a ClickUp task | Opus |
| Dante | Copywriter: the owner's writing rules and Caspar's voice | On every PR that adds or changes player-facing text | Sonnet |
| Imani | Researcher, read-only: what the rulings, canon, and code already say; deep reports from outside sources | Whenever a design, build, or task prep needs what's already known (owner, 2026-10-07) | Sonnet |
| Jeffrey | Reviewer: brief, AGENTS.md, sweeps, correctness | On every PR, before asking the owner to merge | Opus |
| Whitney | Docs and continuity: build log, `VALIDATION.md`, mirrors, indexes, memory | After every merge, and Monday and Thursday | Sonnet |

- **A build's order:** Ashantis preps the task, the owner answers only the "Needs you" list, Claude builds, Dante reviews the text, Jeffrey reviews the PR, the owner merges, Whitney records.
- **Jeffrey's BLOCKING findings hold the merge** until they're fixed or the owner waves them through in their own words.
- **Claude does no docs pass of its own.** The PR body carries the full validation record, and Claude hands Whitney a short "what changed" note after the merge. The build's pass record and captures still go on its ClickUp task.
- **Models:** each agent's file sets a default. Claude may override the model for a single job; when Opus misses the same fix twice, rerun that job on Fable.

## Writing tasks for the owner

- Every ClickUp task and brief opens with a **Quick read**: two or three plain sentences on what's happening, what the owner needs to decide, and by when. A divider follows; everything below it can be as detailed as the agents need.
- Task names use plain words ("Center the Aries symbol on the dial").

## Scope discipline

- Implement only approved requirements. Do not expand a greybox into polished art, final narrative, new systems, or additional curriculum without authorization.
- Prefer small, reversible changes that expose unresolved decisions early.
- If progress requires guessing, stop at the decision boundary and ask for the missing design choice.
