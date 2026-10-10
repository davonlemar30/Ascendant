# Celestial Dial greybox validation

This is an interaction test, not production art or a gameplay-validation result. The governing records are linked in [the source index](../README.md).

## The launch, after the owner watched it: one wheel, a clean blend, the logo's fades, the window's pulse and a rat (task 86bcg62x3, Oct 9-10)

PR #149, a follow-up to #141. What shipped is in the [build log](../README.md); this is the validation record, taken from PR #149's body and Claude's note. No new player-facing text.

**Checks added.** The fixture watches every tick through the burn and the transform: every wheel layer on stage shares one angle, and Earth stays at 0 (in the main run and under reduced motion). It also checks the glow pulses and holds steady under reduced motion; the rat runs behind the buttons and moves along; Settings still opens while the rat runs; the rat never appears on the slot list or under reduced motion; at 1280 x 800 the run's ends are 324 (the bleed's 300 plus half the rat), clipped to the street; and on the slot list the window still glows under the veil and pulses (new web-state fields `slotsGlow` and `slotsGlowShown`). The suite runs the same checks from the published state, taps where the running rat is and asserts nothing happens, checks the slot list's glow, and runs a 1280 x 800 menu pass (the rat first shows past the visible street, crosses all of it, leaves past the far edge, and keeps the column's speed).
- **Slot count:** 224 (221, -1 `intro-wheel-earth`, +4 `menu-rat-*`), in `Slots.cs`, `GreyboxValidation`, the suite's `ART_SLOTS` and `ART-SLOTS.md`.

**Validation (local, Unity 6000.3.24f1), on `f924d18`:**

| Rung | Result |
| --- | --- |
| Mechanical (`GreyboxValidation.Run`) | 810/810 |
| Slice fixture (`SlicePlayValidation.Begin`) | 471/471, no runtime errors |
| Headless WebGL (`WebBuild.Build`) | succeeded, 47,823,002 bytes, 0 errors, 0 warnings |
| Browser suite, desktop density | 1005/1005 |
| Browser suite, phone density (`DEVICE_SCALE=2 MOBILE=1`) | 1005/1005 |

- **Warnings:** none new (no `warning CS`). Pre-existing: the Editor's Search indexing exception at fixture start-up.
- **Not tested:** Android and the emulator, WebKit, a hand-run screen reader.
- **Production:** passed, 1005/1005 at desktop and 1005/1005 at phone density, on main 8c196a6 after the Pages deploy (task 86bcg62x3, comment "PR #149 merged and checked in production"). The production suite runs from a checkout of main (it reads repo files relative to itself).
- **Captures:** on 86bcg62x3 (the burn and transform sequence with layer angles, the logo mid-fade and at the hold, the glow low and high, the rat mid-run, the 1280 x 800 pass).

Reviews: Jeffrey at 95c5b33: B1 (the rat's run ended at the wrong place on wide windows) fixed in `feb17a8` and `13569ec`; N2 and N3 done; N1 (the "no animated characters" exception) went to the owner, who ruled "Widen it: menu can animate". N7 (the logo slot's description still says about 2 s) is open, in code.

Merged as PR #149 (main `8c196a6`, Oct 10), head `f924d18`.

## The opening scene, the launch movie and the main menu (tasks 86bcfhmha and 86bcg62x3, Oct 8-9)

PR #141 holds two builds: the movie-style opening (a new game's prologue, then WHO ARE YOU? with the orb) and the launch flow (the TSG Games logo, a 30-second launch movie, the main menu, three save slots, "Main menu" in Settings). What shipped is in the [build log](../README.md); this is the validation record, taken from PR #141's body and the pass record on 86bcg62x3 (comment 90140266922040).

**Checks added.** Counts at the end of the PR: 804 mechanical, 456 fixture, 977 suite.
- **Opening (mechanical):** a new game starts at Prologue; `Continue` and the birth questions do nothing there; `screen_entered:Prologue` is logged once; the end and Skip each reach Identity once; a restored save resumes at the Hub; the enum is appended (Prologue 13, Journal 12, Identity 0); the stars count the answers (0 to 5 on the skip path, 8 on the longest path, none on "Your Birth"); the shot list (seven shots in order, the frames as 360 x 800 slots at cap 2048, every scale 1 or more, every pan inside the frame, every panel on screen, a line per shot with none of the banned words or dashes); Skip a 44 px target; the orb and its stars fit the band; eight even star places; slot names may hold digits after the first letter (working choice 25); every old flow that opened on Identity now skips the prologue first (29 call sites).
- **Opening (Oct 9 animations):** the web state publishes each shot's frames in order (`prologueShotFrames`); the writing loop runs (notebook, -2, -3 and round, from the panel's landing to the shot's end); shot 3 in order (desk, light, light-up, look, headphones-mid, headphones), 9 s; reduced motion holds [desk, notebook] and drops the mid frame.
- **Launch (new):** the launch order (logo, movie shots in order, menu); Skip; the spoken lines (all 15 quoted); the glint on the sweep's angle and ring, gone afterwards; the zoom rising through draw (0.853), glyphs (0.948) and alive (0.992), then 1.0 at burn; Aries alone at the first glyph step and Aries to Virgo at the halfway step; the menu's four buttons and their dimmed state; the slot lists, confirm-before-replace, Continue picking the last-played slot, and the old single save showing in slot 1; "Main menu" from Settings before and after Key 1; reduced motion.
- **Slot count:** 221 (166 + #140's 21 + the opening's 18 + the launch's 16), in `GreyboxValidation`, the suite's `ART_SLOTS`, and `ART-SLOTS.md`'s test-set line.

**Validation (local, Unity 6000.3.24f1), on `e675bde` (the final art), then `bae79b1` (one docs line, Jeffrey's B1):**

| Rung | Result |
| --- | --- |
| Mechanical (`GreyboxValidation.Run`) | 804/804 |
| Slice fixture (`SlicePlayValidation.Begin`) | 456/456, no runtime errors |
| Headless WebGL (`WebBuild.Build`) | succeeded, 48,035,648 bytes, 0 errors, 0 warnings |
| Browser suite, desktop density | 977/977 |
| Browser suite, phone density (`DEVICE_SCALE=2 MOBILE=1`) | 977/977 |

- **Earlier runs:** the launch with placeholders on `0f8464c`: 801 / 446 / build clean / 971 at both densities. The opening, on `2cfed3e` (the Oct 9 animations and their art): 758 / 383 / WebGL 43,208,316 bytes / 835 at both densities; after Jeffrey's review (`a84d7bc`) 756 / 376 / 41,780,611 bytes / 823; the final-art run on `c7260aa` 754 / 376 / 41,780,355 bytes / 817; the placeholder runs on `2239562` 745 / 376 / 36,175,757 bytes / 785.
- **One suite failure first (opening):** in `validate-all.sh` the desktop suite failed one check at 360, because the notebook capture read the state after the writing loop had begun; `595a0b4` fixed the suite only (the capture settles 0.3 s and accepts a loop frame), and both suites then passed 835/835 on the same build. An earlier fixture timeout (the batch Editor's first seconds stalling while the prologue keeps real time) was fixed in `2239562`.
- **Warnings:** none new (no `warning CS` lines). Pre-existing: the Editor's Search indexing `ArgumentOutOfRangeException` at fixture start-up, which the fixture ignores.
- **Import churn:** `ProjectSettings.asset` and `UniversalRP.asset` (import noise) and the owner's URP and ShaderGraph upgrade markers were left uncommitted.
- **Seen by Claude at phone density:** the logo screen, every movie shot with the real art, the menu fresh and with a save, the slot lists, the confirm box, Settings on the menu and in-game; every prologue frame at 390 and 360, normal and reduced motion.
- **Not tested:** Android and the emulator, WebKit, a hand-run screen reader (the announcements are checked from the live region), and production at the time of the PR.
- **Production:** passed, 977/977 at desktop and 977/977 at phone density, on main 4d4d48e after the Pages deploy (task 86bcg62x3, comment 90140266943337). The first phone run stalled while a Unity run went in parallel; the rerun passed.
- **Captures:** on 86bcg62x3 (the launch sheet, the menu sheet, the two-moons frame, and a zip of all 36 phone captures); locally in the `opening-scene` worktree under `Logs/WebEvidencePhone/` (`390-prologue-*`, `390-launch-*`, `-menu-*`, `-slots-*`, `-settings*`) and `Logs/Evidence/`.

Reviews: Dante passed the text (the prologue's Skip label and lines, two edits on lines 4 and 6; the menu's two fixes: the Main menu question and commas in the spoken slot label). Jeffrey cleared the launch delta at bae79b1: B1 (`ART-SLOTS.md` said 205) fixed in bae79b1, B2 (the pass record and captures) fixed on the task; 8 NOTEs, none blocking. The owner ruled on two of them (round 7): the two moons ghosting together in the street crossfades, "Leave it"; a replaced slot clears at confirm, "Keep: clear on confirm".

Merged as PR #141 (main `4d4d48e`, Oct 9), head `bae79b1`.
## The Elemental Table and the Book of Symbols come alive (task 86bcf0x71, Oct 8)

What shipped is in the [build log](../README.md); this is the validation record, taken from PR #140's body and Claude's hand-off. Player-facing change (the Table and the Book); no words added or changed, so no Dante pass.

- **Mechanical** (`GreyboxValidation.Run`): PASS, 729 checks, no compile errors. **Slice Play Mode fixture:** 286 passed. **Headless WebGL build** (`WebBuild.Build`): succeeded, 37,288,238 bytes, 0 errors, 0 warnings.
- **Browser suite** on head b5782e1: 721 passed, 0 failed at desktop density (390 and 360) and 721 passed, 0 failed at phone density (`DEVICE_SCALE=2 MOBILE=1`).
- **New and changed checks:**
  - Every row and column name sits on the table's wood: each laid-out name is checked against `table-room` with a 95% floor (the run measured at least 98%; the pre-fix art fails it, "Fire" 67%, "Earth" 60%).
  - The Table comes alive: the live state requires both materials (symbol and name) and both outlines.
  - Reduced motion: without it the gold is not yet whole at the Seal (it spreads); with it a correct Seal shows the gold whole and the lettering awake at once, inside the hold; the next plate is gold too and the twelfth ends gold and whole after the Key 3 ceremony.
  - Mechanical: a dragged well logs "Drag", a tapped Seal logs "DirectCell", and the drag grades the same; live lettering is edged in the Art Bible's line on all four elements; the frame's names are inlaid gold. The deepened name colours and their Editor check are retired.
- **Contrast on the pale plates** (each material's average fill against the plate face; the outline against gold is 11.5:1): Fire 3.03:1 on wood, 4.38:1 on gold; Earth 2.41:1 and 3.49:1; Air 1.44:1 and 1.01:1 (why every element gets the outline); Water 3.29:1 and 4.76:1.
- **Visual:** four phone captures of the Table (`pr140-phone-grid-start`, `-drag`, `-gold`, `-key3`), taken at 4dac9ef before the fixes in b5782e1, checked by eye, on the task (comment 90140266414568); no miss capture and no Book capture is on the task.
- **Slots:** 187 in `Slots.cs` (`new ArtSlot(`); the 21 new rows in ART-SLOTS.md match the manifest's names, sizes and max sizes (Whitney, on main 2de2855).
- **Not covered by a check:** faint ink when Caspar shows the answer; a drop outside any well; a drag while the sign is locked; the slide motions themselves (skipped under Reduced motion, which is checked); the Book's Reduced motion paths (the still emblem, no rise, the shorter full-pages look) (Jeffrey's N13).
- **Not tested:** the Android APK (not cut; cut only on the owner's word).
- **Production** (every-merge tier: new art under `Assets/`): main's own suite at phone density against https://davonlemar30.github.io/Ascendant/ on main 2de2855: 721/721, 0 failures; Pages deploy run 37888181400 succeeded; names on wood at least 99%; the twelfth plate gold after Key 3 (Claude, comment 90140266494462 on 86bcf0x71). **The last main production checked is now 2de2855.**

Reviews: Jeffrey's first review had two BLOCKING items (B1 a stuck plate after a cut-short drag; B2 Reduced motion), both fixed in b5782e1; the re-review was clear. Follow-ups, not blocking: N11 (a plate grabbed during its 0.25 s slide back jitters; `PlateDragBegin` could refuse the sliding seat), N12 (the "gold not yet whole" check reads state inside the 0.5 s spread, so a slow runner could fail it falsely), N13 (above). CI: Build Web player and Guard agent files green.

Merged as PR #140 (merge commit, main `2de2855`, Oct 8); the reviewed head is `b5782e1`. Left out on purpose: an uncommitted Book edit in the `mini-menu` worktree on `codex/come-alive`, never committed or reviewed.

## The swallowed-code check catches a hidden assignment or a lone call (task 86bcf5j8k, Oct 8)

Editor-only: `Assets/Editor/CelestialDial/GreyboxValidation.cs`. What shipped is in the [build log](../README.md); this is the validation record, taken from PR #136's body and comments. No player-facing change, no Dante pass.

- **Fixtures:** on 9e4de2c in Unity (a temporary Editor probe calling the real `CommentStart` and `HidesStatement`, deleted, never committed): fixture A (3a49339's `SliceView.cs` fails with exactly `SliceView.cs:585, SliceView.cs:598`), fixture B (721/721, zero hits), synthetic 24/24 (14 swallowed, 10 prose), past swallowed lines 8/8. On 93bbb05 in a .NET 6 harness (Unity's bundled runtime, built from the file's own regex block), not in Unity: synthetic 36/36 (20 swallowed, 16 prose, including the `?.` and `?[` shapes) and history 9/9 lines (8 swallowed, 1 prose); the old check missed 21f09d8, 91d0885 and both 7cdb2bb lines. `CommentStart` agrees with a full C# tokenizer on all 33 `.cs` files (0 mismatches).
- **Not covered:** a swallowed `i++;`, `return;`, `break;` or `yield`; a swallowed `}` (breaks the compile anyway); an assignment to a cast-wrapped target; a shift-assign (`<<=`, `>>=`, `>>>=`; none in the tree today); `x is Foo f` right sides. The scan reads only `Assets/CelestialDial` and `Assets/Editor/CelestialDial`; the two `Assets/Editor/Build` scripts are not scanned and are clean today. A comment in the scanned folders that quotes a full statement with its `;` now fails and must be reworded.

**Checks:** no check added; the counts are unchanged (mechanical 721, fixture 286, suite 689).

Validation (local, Unity 6000.3.24f1):
- on 93bbb05 (the merged tree), in Unity: compile 0 errors and 0 warnings; fixture A again (exactly :585, :598); mechanical 721/721 (zero hits); slice fixture 286/286;
- on 9e4de2c with #134's `SliceView.cs` copied in: headless WebGL succeeded (36,154,769 bytes, 0 errors, 0 warnings); browser suite 689/689 at desktop density and 689/689 at phone density. Not rerun after the B1 fix (Editor-only regex constants, identical player code); Jeffrey accepted that;
- GitHub Actions "Build Web player" green on 93bbb05;
- production: waived by the owner on Oct 8 ("can we actually skip the production run this time?"; Claude's comment 90140266287133 on 86bcf5j8k has the reason). An Editor-only change cannot change the player, and the Pages deploy for main fc41242 (run 37835042917) passed. Under the three tiers the owner ruled that day (PR #138) this merge would be Skip. The waiver does not move the production baseline.

Reviews: Jeffrey, B1 BLOCKING (hidden `?.` calls passed) fixed in 93bbb05; N1 partly taken (a number and its unit read as prose); N2 left (cast-wrapped target); N3 closed. Re-review at 93bbb05 clear.

Merged as PR #136 (squash, main `fc41242`, Oct 8); the head is `93bbb05`.

## Two lines a comment hid in the Zodiac Wing room (task 86bcf3paf, Oct 8)

Two statements in `SliceView.cs` that a `//` earlier on their line had turned into comment text (lines 585 and 598). What shipped is in the [build log](../README.md); this is the validation record, taken from PR #134's body and comments. The owner chose to keep the caption Bone, so only the Dial label's line now runs.

- **Line 585** (`dialLabel.color = Muted;`) runs; the label shows only without the Wing kit art.
- **Line 598** (`wingRoomCaption.color = Muted;`) is deleted. The caption's measured text colour in the checked head equals production's, rgb(236, 228, 215), on first arrival and at stage 6.
- **Scan:** every `.cs` under `Assets/`, string-aware, a strict pass (assignments, calls, `var`/`if`/`for`/`return`/`new` up to a `;`) and a loose pass; only these two lines hid code.
- **Contrast** (390 x 844 production captures, median of the caption band / its brightest 10%): Bone 4.78 / 2.36 (first arrival), 3.36 / 1.85 (Dial glow), 3.38 / 1.85 (table awake), 4.12 / 1.49 (stage 6); Muted 1.90 / 1.07, 1.33 / 1.36, 1.34 / 1.36, 1.63 / 1.69.

**Checks:** no check added; the counts are unchanged (mechanical 721, fixture 286, suite 689).

Validation (local, Unity 6000.3.24f1):
- full ladder on `6f375d7`: compile 0 errors and 0 warnings; mechanical 721/721; slice fixture 286/286; headless WebGL succeeded (36,154,794 bytes, 0 errors, 0 warnings); browser suite 689/689 at desktop density and 689/689 at phone density;
- final head `0d89910` (a comment-only change, Jeffrey's N2): mechanical 721/721, 0 errors, 0 warnings;
- the superseded grey version `7cdb2bb`: the same ladder, all green (mechanical 721/721, fixture 286/286, suite 689/689 at both densities); production baseline on main `3a49339`, 689/689 at desktop;
- GitHub Actions: "Build Web player" and "Guard agent files" green on `0d89910`;
- "Build Web player" and "Deploy to GitHub Pages" succeeded on main `0dca10e`; Pages served the new build at 14:22:38 GMT, Oct 8;
- production, after that deploy: 689/689 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against https://davonlemar30.github.io/Ascendant/, main's own suite run from the scratchpad, 0 failures (Claude, comment 90140265991182 on 86bcf3paf).

Reviews: Jeffrey, no BLOCKING. N1 (the swallowed-code check misses plain assignments) is a follow-up outside this brief; N2 applied; N3 is the PR's record; N4 squash merge. Not tested: the no-kit fallback, the only place the label's new grey shows (the shipped art has the kit, and `?art=test` fills every slot).

Merged as PR #134 (squash, main `0dca10e`, Oct 8); the head is `0d89910`.

## Art pass: the Dial's eye at Key 1, and tap the doors (task 86bcex5kc, Oct 7)

The owner's Oct 3 playtest notes 2 to 6, answered "all recommended" (Oct 7). What shipped is in the [build log](../README.md); this is the validation record, taken from PR #131's body and comments. The Book and the Table art were split out (task 86bcf0x71) and are not in this build.

- **The eye:** the cause was found by capture. At Key 1 two dial layers show, with their eyes about 8 px apart. Showing the Key 1 look in a Key 2 room (less dust) still read half-shut, which ruled the dust out. The worn eye's opening is drawn above the half layer, 0.42 of the Dial's width by 0.14 of its height, with soft 6 px and 4 px edges, measured from where the worn file's open and closed art differ (0.37 by 0.11).
- **The doors:** the travel buttons are removed in all three rooms; their hidden screen-reader copies go with them; the first keyboard focus is the Wing's door. The doorway glows reuse the Atrium doors' values (about 15 levels of change at the peak against the Wing's lighter stone).

**Checks**
- Mechanical (+1, 721 against 720): the title page closed mid-beat shows again, then lands on the Oct 2 line (Jeffrey's #131 N5).
- Fixture 286/286, unchanged from main.
- Suite (+4, 689 against 685): the Atrium on the opening walk has no Zodiac Wing button and its door is a tap target; the Wing has no "Return to the Atrium"; the Chamber has no "Return to the Atrium", and from Stage 2 the Atrium has no Wing or Chamber button, with both doors taking the tap (Jeffrey's #131 N3). Every room trip in the suite now goes through the doors' own buttons.

Validation (local, Unity 6000.3.24f1, final head `c1d9dac`):
- mechanical checks 721/721, 0 compiler errors;
- slice fixture 286/286, no runtime errors (from a rerun after a Unity startup hang);
- headless WebGL 0 errors and 0 warnings;
- browser suite 689/689 at desktop density and 689/689 at phone density;
- GitHub Actions Web build green;
- production, after the Pages deploy of main `fcbaa9d`: 689/689 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against https://davonlemar30.github.io/Ascendant/, main's own suite, no failures (Claude, posted on 86bcex5kc).

Captures on the task: the eye at Key 1 before and after, and Key 2 for comparison (the first after-capture shows the too-wide band Jeffrey caught). Reviews: Jeffrey's B1 (the lifted opening covers the eye only) fixed; N1 (a swallowed statement restored), N2 (stale comments) and N3 (Chamber checks) applied; N5 as above; N4 (no capture of the doorway glow) was accepted, since the glow is not tested on a phone; N6 (wording slips) closed. Not tested: the doorway glow on a real phone. Open tidy-up: a whitespace-only line at `index.html:175`, left in to keep the reviewed head.

Merged as PR #131 (main `fcbaa9d`, Oct 7); the head is `c1d9dac`.

## The living inscription (task 86bceba0a, Oct 7)

The owner approved it as scoped (Oct 7), with the clock option withdrawn. What shipped is in the [build log](../README.md); this is the validation record, taken from PR #128's body and comments.

- **The data:** `Resources/Journal/inscriptions.json`, 24 lines with stable ids, a group and an unlocking lesson, each marked a draft (8 any time, 4 Key, 3 Wing, 3 absence, 6 sun).
- **The ink:** each letter fades over 0.15 s, 35 ms apart, with rests at commas and full stops; 2.0 to 3.5 s a line, starting 0.3 s after the landing shows. The first-ever open writes the Oct 2 line as the title page finishes fading.

**Checks**
- Mechanical (+22, 720 against 698): the data (24 lines, unique ids, groups, unlocks, drafts, no digit), every line filled and drawn for all twelve suns and at most three lines for a long name, the ink's timing, the first-ever open, a line held for the visit, thirty visits with no repeat of the last five, the Key and Wing lines first, absence at 3 sittings and never otherwise, an older save, no sun lines with no sun, a line that cannot fit sitting out, a name filled in last, and the tap layer ending above Close the journal.
- Fixture (+2, 286 against 284): the Oct 2 line writes itself as the title page fades, with the doors working, then is written in full.
- Suite (+20, 685 against 665): the first landing's Oct 2 line writing then written, a fresh save's first-ever open word for word, the next visit's new line written letter by letter with the screen reader's text whole, a tap on the margin finishing it, back from Contents keeping the same line, Reduced motion showing it whole, Close the journal tapped from the landing, and a reload in the middle of the first-ever title page.

Validation (local, Unity 6000.3.24f1, final head `881bd80`):
- mechanical checks 720/720, 0 compiler errors;
- slice fixture 286/286, no runtime errors;
- headless WebGL 0 errors and 0 warnings;
- browser suite 685/685 at desktop density and 685/685 at phone density;
- GitHub Actions Web build green;
- production: 689/689 at phone density against https://davonlemar30.github.io/Ascendant/. The check was deferred and ran on the later main `fcbaa9d`, which includes #128 and #131 (its own suite, so 689 not 685), with no failures (Claude, posted on 86bceba0a). Main at #128's own merge, `9b94eea`, was not checked separately.

Reviews: Jeffrey's B1 (the tap layer ends above Close the journal) and B2 (the first-ever open saves after its title beat) fixed; N1 to N3 applied; N4 (Dante's `sun-table` question) was answered on the PR, not by a change; N5 is this record and the pointers below; N6 is an open bug (closing the journal by hand during the first-ever title page, then quitting before reopening, loses the Oct 2 line). Claude folded the fix into the art pass (task 86bcex5kc, comment 90140265774355) as his own working choice: the owner was told and did not object, and it is not the owner's ruling. The fix is commit 98bb708 on codex/art-pass. Dante's flags: the owner changed one line ("Some journals stay blank forever. I got lucky, [Name]." in place of "...Not me, [Name]. Not with you around.") and kept the other two. Not tested: a real absence between real sittings in a browser (the mechanical checks fake the count), a name with "<" on screen, a phone or the APK (no cut).

Merged as PR #128 (main `9b94eea`, Oct 7); the head is `881bd80`. The inscription entries further down record the landing as of Oct 2: since #128 the line there is the first-ever open's only.

## No unknown Big Three: every missing sign picked at the opening (task 86bced0tc, Oct 7)

The owner's ruling (Oct 7): no new game shows "unknown" in the Big Three. What shipped is in the [build log](../README.md); this is the validation record, taken from PR #125's body and its validation comment.

**Checks**
- Mechanical (+2, 698 against 696): the skip path's three picks (−1 refused at each), the cusp without "I'm not sure", the rising asked after the Moon on a day with no time or no place, the notes in full, "Your Birth" with a chosen rising, an older save's "or" Moon read as before, a #123 save with a declined Sun (the lessons start from Aries), an older save's Add (the Moon, then the rising, answered and cancelled at the rising), and Jeffrey's #123 B1 case still held.
- Fixture (+3, 284 against 281).
- Suite (+8, 665 against 657): the skip path's three screens, the no-place path, the cusp then Moon then rising, the Moon question's two signs only, each of the four DEV samples landing on its first question, "Your Birth" with Change my rising, no "unknown" or " or " in any new opening's record, and the `v4-chart` conversion keeping its " or " Moon.

Validation (local, Unity 6000.3.24f1, final head `99c9ebb`):
- mechanical checks 698/698, 0 compiler errors;
- slice fixture 284/284, no runtime errors (the log has one Editor-internal `UIR.RenderChainCommand` exception, the same as in five earlier builds);
- headless WebGL 0 errors and 0 warnings;
- browser suite 665/665 at desktop density and 665/665 at phone density;
- GitHub Actions Web build green on `99c9ebb`;
- production, after the Pages deploy of b71821d (Build Web player and Deploy to GitHub Pages both green): 665/665 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against https://davonlemar30.github.io/Ascendant/, main's own suite, no failures and no runtime errors (Claude's comment 90140265736971 on 86bced0tc).

Reviews: Jeffrey's B1 (the fourth DEV sample) and B2 (the record) fixed or closed; his notes 1 to 4 applied in `99c9ebb`. Not tested: "Starting sign: Aries" on the Dial in a browser (no new game reaches it; the mechanical check covers the older save), older saves played through the opening's screens by hand, the rising asked during an Add in the fixture or the browser, a phone or the APK (no cut; the phone-density suite stands in).

Merged as PR #125 (main `b71821d`, Oct 7); the head is `99c9ebb`.

## The birth record: facts, choices and the worked-out chart; "Your Birth" (task 86bceb6fq, Oct 7)

The owner's birth-time design (Oct 7): the game never invents a birth time, a place or a sun. What shipped is in the [build log](../README.md); this is the validation record, taken from PR #123's body and comments.

- **The save** (version 5): `BirthFacts`, `ChoiceRecord`, `WorkedChart`; version 4 saves convert once (chart, known, chosen and pre-#110 paths); the record reads worked out over chosen over unknown.
- **The maths** works over a UT window; no time means the local day, no place means every zone in the table, and no rising without an exact time and a place.
- **The opening, "Your Birth", the Dial's no-sun lines** and the DEV jumps are as the build log says. The test-only command `jump-birth:v4-*` writes an old save from each path.

**Checks**
- Mechanical (+37, 696 against 659): the save's round trip and conversions, the window maths, the opening's two answers, the Moon pick, "Your Birth" and its change lines, and Jeffrey's B1 case (a cusp pick, then "‹ Your Journal" at the Moon's question, leaves the record unchanged).
- Fixture (+10, 281 against 271) and suite (+33, 657 against 624).

Validation (local, Unity 6000.3.24f1, final head `12e699d`):
- mechanical checks 696/696, 0 compiler errors or warnings;
- slice fixture 281/281, no runtime errors;
- headless WebGL 0 errors and 0 warnings (36.2 MB);
- browser suite 657/657 at desktop density and 657/657 at phone density (`DEVICE_SCALE=2 MOBILE=1`);
- GitHub Actions Web build green on `12e699d`;
- production, after the Pages deploy: 657/657 at phone density against https://davonlemar30.github.io/Ascendant/.

Reviews: Dante's two edits applied (`579dfdc`); Jeffrey's B1 fixed, B2 waived by the owner, B3 closed by the validation record above. Not tested: the per-path DEV samples (not built).

Merged as PR #123 (main `cdb4170`, Oct 7); the head is `12e699d`.

## The Moon on a day it changed sign: "Pisces or Aries" (the canon flags, Oct 3)

The owner's ruling (Oct 3, comment 90140263873037 on 86bcbn6w6, the canon flags): "Upgrade to option A for the Moon: on a day the Moon changed sign and the birth time is unknown, the record shows both signs instead of 'unknown', e.g. '☽ Pisces or Aries', the way astrologers write it. The rising stays '↑ unknown'. A sun-only save is unchanged."

- **The chart** (`BirthChart`): with no birth time, the moon's sign at the local birth day's start and end are kept (`MoonFrom`, `MoonTo`). They differ on a day it changed sign; the moon itself stays not worked out (`Moon` -1), as the canon's time-unknown state has it.
- **The opening** (`SliceFlow`): the chart path keeps both (`MoonPair`), and the screen says so in a draft line, "Your sun sign is Taurus and your moon sign Cancer or Leo (it changed sign that day). Without a birth time, your rising sign stays unknown." The known path and "I don't know" are unchanged.
- **The Keeper's record:** "☉ Taurus · ☽ Cancer or Leo · ↑ unknown" (`MoonWords`). A sun-only save still reads "☉ Taurus · ☽ unknown · ↑ unknown".
- **The save** keeps both moons (`moonFrom`, `moonTo`), read back as they were. A save from before this build has neither and reads its moon as unknown, as before.
- **The line's fit** (working choice): a long Big Three line may use the page's calm column (x 50 to 325, 274 px) and shrinks to fit it, so the record keeps its rows and the doors stay where they are. Every Big Three line from before fits at its 17 px; "☉ Taurus · ☽ Cancer or Leo · ↑ unknown" (296 px at 17) takes 16. The floor is 11 px, where the longest possible, "☉ Sagittarius · ☽ Sagittarius or Capricorn · ↑ unknown" (386 px at 17), fits; at 12 px it measured 296, the font's advances not scaling evenly at small sizes.
- **Later** (batch 3, the owner's rising ruling): a birth time added from the journal settles the moon.

**Checks**
- Mechanical (+4): the day's two moons kept (London, May 1 1990: Cancer into Leo at 01:09 BST; May 2 holds Leo; a known time works out one); the opening and the record's line; the save both ways, and a save from before; the longest line's fit.
- The suite (+1): the chart path in the browser with no birth time on May 1 1990: "☉ Taurus · ☽ Cancer or Leo · ↑ unknown".

Validation (local, worktree `mini-menu`, branch `codex/moon-or` on main `53c9933`):
- mechanical checks 634/634 (+4), 0 compiler warnings;
- slice fixture 262/262;
- headless WebGL 0 errors and 0 warnings (36.1 MB); its first run stalled after the initial asset refresh (the known batch stall), was killed and run again;
- browser suite 604/604 at desktop density, with the seven shapes (+1), and 513/513 at phone density (`DEVICE_SCALE=2 MOBILE=1`).

Merged as PR #116 (main `6d7b61b`, Oct 3 at 04:19 local, which is 12:19 UTC; a merge commit of main `61e9a90` and the head `b643b2c`; main's tree is the head's tree, checked with `git diff`). The head is one commit (Oct 3, 04:07 local), rebased onto main `61e9a90`, the triangles (#115), before the merge; only the docs conflicted. The validation above ran on main `53c9933`; after the rebase the PR reports the mechanical checks on the combined code, 640/640 (634 and the triangles' 6). The PR's GitHub Actions Web run on `b643b2c` (`37121920382`) passed (Build Web player succeeded; Deploy to GitHub Pages was skipped, as it is for a pull request); the main run (`37122477052`: Build Web player and Deploy to GitHub Pages) passed, and production served the merged build from 04:26 local (12:26 UTC). Production recheck after the Pages deploy (passed): browser suite 610/610 at phone density (`DEVICE_SCALE=2 MOBILE=1`, the seven-shape pass included) against `https://davonlemar30.github.io/Ascendant/`, using `6d7b61b`'s own suite file; it includes the opening on London, May 1 1990 with no birth time, showing "☉ Taurus · ☽ Cancer or Leo · ↑ unknown".

## The family triangles: light on top of the Cast Dial, resting, teaching and the flame payoff (batch 2, step 5)

The owner's ruling (Oct 3, comment 90140263872877 on 86bcbn6w6): the overlay is approved at mixed strength. Resting and teaching use level A (resting lines 34%, the taught triangle 70%: "The resting look shows every time the Dial opens, so it stays calm"). The flame payoff uses level B (88%: "It plays once, so it hits hard"). Everything else is as boarded: the lines on top as light (a screen blend), the corners on the hub ring under each window, the sides breaking round every word, and the teaching glow lighting the seats' frames and stopping short of their words. Reading 2 is not needed. The values are the board's (`triangles-overlay/REPORT.md` and `tools/looks.cjs`).

**What it draws** (`FamilyTriangles`, a UI graphic over the Dial's art and words and under its controls; one screen-blended draw, `Shaders/LightLines`):
- **Resting, every time the Dial shows.** The four element triangles (each joins its own three seats: Fire Aries, Leo, Sagittarius; Earth, Air and Water the same), thin lines in the resting light (RGB 255, 238, 202) with a faint glow, and a small four-point star at each corner.
  - Level A: lines 34%, glow 10%, stars 55%.
  - On the worn Dial the lines are dusty (RGB 176, 166, 148, at 65% of resting, no glow); they clear with the wake-up step (halfway at Key 1, clean from today's look on).
  - The shine on the bright Dial isn't boarded, so the bright look keeps today's strength.
- **Teaching, while a lesson teaches a family.** That family's triangle fills with its element's light: the core 70% in its element's pale core colour, a glow (55%) and a bloom (20%) in its glow colour (batch 1's ember orange, moss green, pale gold, cool blue). Its three seats' frames (the window and the name's recess) glow at 80%, the glow stopping short of their words.
  - The taught family: the guided one (the player's sun's family), then the second, then the continuation's two; none between them or once a family is complete (`DialLesson.TeachingFamily`).
- **The payoff, once.** At the moment the whole wheel lights (`DialLesson.WheelLit`, raised only when the lesson reaches it live, never on a load), all four triangles burn as ribbons of flame in their element's colours at level B (88%). They burn for 2.2 s, then down into the resting lines over 1.2 s. The flames' noise, tongues and thinning ends are the board's; with reduced motion they hold still.
- **The corners** sit on the hub ring just under each seat's window (r 80.5), and the lines turn with the ring, so a lit triangle follows its seats.

**Words stay clear.** Every side breaks round every word it would cross, like a label on a map:
- the eye's challenge and count, the ribbon's name and facts, and each seat's symbol, name and fact;
- each word's box comes from a layout of the text's own, and a curved word (the ribbon's, a seat's name) is tested on its own arc (`ArcText`'s bend, undone exactly);
- the light stops 2.5 px short of a word's box (the bloom 9.5, a flame 5, the seats' glow 3), and a corner's star is left out if a word comes within its reach;
- the gaps follow the words as they change and the ring as it turns.

The build checks every drawn side for light inside a word's box (`trianglesCrossing`, always 0).

**The light.** The board's values are screened in the browser's sRGB; the game blends in linear colour. The shader carries the board's sRGB light and turns it into linear colour with a gentler curve (power 1.7), measured against the board on today's look: 49.6 levels of line contrast, the board's 50.9.

**The web state** publishes `triangles`, `trianglesShader`, `trianglesMode` (resting, teaching or payoff), `trianglesTaught`, `trianglesShown` (the share of the twelve sides drawn), `trianglesGaps`, `trianglesCrossing` and `trianglesCorners`.

**Checks**
- Mechanical (+6): the owner's mixed strength; the shader ships and compiles; each triangle joins its family; the arc's bend undone exactly; the taught family through the element lesson for a Taurus player (Earth while guided, none between, Fire, none at Key 1, then Air and Water), the payoff raised once, as the whole wheel lights; a restored save already past it never raising it.
- The fixture and the suite: the triangles on the worn first visit (the sun's family taught, every side breaking round the words, no light on a word, the corners on the hub ring), the payoff the moment the whole wheel lights, and in the Web build its burn back down to resting.

Validation (local, worktree `platform-fit-2`, branch `codex/family-triangles` on main `53c9933`):
- mechanical checks 636/636 (+6), 0 compiler warnings;
- slice fixture 264/264 (+2);
- headless WebGL 0 errors and 0 warnings (36.1 MB);
- browser suite 609/609 at desktop density, with the seven shapes (+6). The first runs failed one older timing check (the symbols' mid-placement reload) twice, because the triangles redrew their gaps and republished the page's state every frame while the ring turned (Claude's pass record, comment 90140263882330); the evidence now tests the lines against the hub's words only, and the state is published at most every 0.3 s (`FamilyTriangles.Crossing` and `Changed`), and every run since passed;
- browser suite 518/518 at phone density (`DEVICE_SCALE=2 MOBILE=1`, +6).

Merged as PR #115 (main `61e9a90`, Oct 3 at 03:43 local, which is 11:43 UTC; a merge commit of main `b5f9d06` and the head `2fe0d7f`; main's tree is the head's tree plus the one file PR #113 changed (`.claude/agents/whitney.md`, merged first), checked with `git diff`). The head is one commit on main `53c9933` (Oct 3, 03:28 local), so the code on main is the validated code. The PR's GitHub Actions Web run on `2fe0d7f` (`37119763712`) passed (Build Web player succeeded; Deploy to GitHub Pages was skipped, as it is for a pull request); the main run (`37120567535`: Build Web player and Deploy to GitHub Pages) passed, and production served the merged build from 03:51 local (11:51 UTC). Production recheck after the Pages deploy (passed): browser suite 609/609 at phone density (`DEVICE_SCALE=2 MOBILE=1`, the seven-shape pass included) against `https://davonlemar30.github.io/Ascendant/`, using `61e9a90`'s own suite file.

Captures: [`Evidence/triangles-2026-10-03/`](Evidence/triangles-2026-10-03/), with the approved board beside the game in the three states and the hub at 3x.

## Practice in the journal: the concepts learned, a round of questions in the journal's voice (batch 2, step 4)

The owner's ruling (Oct 3, comment 90140263872651 on 86bcbn6w6): step 4 approved as scoped, with the door's count removed completely. Claude drafts the 30 questions, and the owner rewrites or approves them before they ship; the board's other copy is approved as a draft on the same terms. The art is built with the code. The look is the board on 86bcbn6w6 (Oct 2).

**The pages** (`SliceFlow.JournalView` gains Practice and Quiz):
- **The door.** The landing's Practice door opens Practice, with no count. Its line, "What you know", is a draft beside Contents' "Every chapter" (the ruling's working choice: a short line for the owner's rewrite, or blank). It shows unavailable (half) until a concept is learned.
- **The list.** The concepts learned, in the data's order: each row a gilt four-point star, the concept's name (EB Garamond Bold, 21 px) and its line (the italic, 15 px), with a rule under it, the board's rows 78 apart from y 148. A concept joins when its lesson finishes: the Elements at Key 1, the Symbols at Key 2, the Modalities with their lesson, the Elemental Table at Key 3, the Opposites at Key 4 (the scope's Keys), so no question asks about anything not yet taught. "‹ Your Journal" goes back.
- **A round.** All six of a concept's questions, in a fresh order each round, the choices shuffled; "1 of 6" under the flourish; the ask in the italic, with a sign's symbol drawn above it for the Symbols; the choices in the board's hairline frames on the vellum, 238 x 44, 52 apart.
  - An answer: the right one gilded (a 2 px gilt mark, a wash and its star); a wrong pick in muted crimson with ×; the rest at half; then "That's it." or "Not quite." and the question's why, and Next.
  - After the sixth, the concept's line ("That's all I have on the elements for now. Come back whenever you like.") and Back to Practice. "‹ Practice" leaves a round at any time.
- **Nothing is saved.** No score, streak or spaced review: the save and the deck are the same before and after a round. The Dial's own practice (the fork) is unchanged.

**The data** (`Resources/Practice/questions.json`, read by `PracticeBook`): one entry per concept, with its name, its line, the lesson that unlocks it, its round's end line and its questions (the ask, a sign's symbol or none, the choices, the right one, the why). A concept is added by adding an entry. The 30 questions are Claude's drafts from the game's own facts (the signs' data and Caspar's lines; their sources are on the step 4 comment). The mechanical checks hold 28 of them to the game's data (the signs' elements, modalities, polarities, opposites and symbols); the other two are Caspar's words for Cardinal and Fixed, read by hand.

**The page's layout.**
- The lines break between words, evened so no word is left alone; a break after a full stop or a comma is favoured.
- A question's layout is worked out before the pick, from its why's longer form, so nothing moves when the answer shows. With a symbol, a two-line ask and a two-line why, the choices close up (to 46 apart at most) so Next ends inside the page's border (y 595, the border at about 607).
- The game blends in linear colour, so the right answer's wash is .05 (the board's .16 in a browser).

**The art.** No new file. The choices' frames, their 2 px marks and the star are drawn by the engine; the page, the flourish and the door's frame and emblem are the journal's own.

**The web state** publishes `journalPractice` and `journalPracticeRows`, and for a round `quizConcept`, `quizCounter`, `quizAsk`, `quizGlyph`, `quizChoices`, `quizChoiceBoxes`, `quizPicked`, `quizRight`, `quizFeedback`, `quizEnd`, `quizButtons`, `canQuizChoice`, `canQuizNext` and `canQuizBack`. The template gains the rows', the choices', Next's and the links' semantic buttons; the screen reader hears each page (a Symbols question only says a symbol is drawn, since its name is the answer).

**Checks**
- Mechanical (+19): the data's shape (five concepts, six questions each, two to four different choices, the right one among them); the facts against the game's data; the list growing lesson by lesson, and empty before the first; a whole round (each question once, each answer marked once, the right and wrong lines, the end); a fresh order each round; "‹ Practice" mid-round; the save and the deck unchanged; every line fitting its place at the fonts' own widths; every target 44 px or more.
- The fixture (+7) and the suite (+14): the door opens the list at Key 1; a round on the canvas (a right pick, a wrong one, Next, at both widths the whole round to its end and Back to Practice); the targets; the spoken page.

Validation (local, worktree `dial-wake`, branch `codex/journal-practice` rebased on main `6d7b61b`, so with the triangles and the Moon):
- mechanical checks 659/659 (+19), 0 compiler warnings;
- slice fixture 271/271 (+7);
- headless WebGL 0 errors and 0 warnings (36.1 MB);
- browser suite 624/624 at desktop density, with the seven shapes (+14), and 533/533 at phone density (`DEVICE_SCALE=2 MOBILE=1`).

Captures: [`Evidence/practice-2026-10-03/`](Evidence/practice-2026-10-03/), with the approved board beside the game at Key 1 and at Key 4.

Held off production until the owner approved the questions (the ruling: "I rewrite or approve them before it ships"); the APK carried it for the owner's playtest. The owner played all 30 and approved them as written on Oct 7.

Merged as PR #114 (main `4a9a845`, Oct 7 at 09:39 UTC; a merge commit of main `35bf695` and the head `808e1f1`; the head is itself a merge of main `35bf695` into the branch's `7023b5f`, which resolved the build-log conflict and recorded the owner's approval). The PR's validation record is the combined-code ladder above (659/659, 271/271, 624/624 and 533/533). The main run (`37602121981`: Web) passed. Production recheck after the Pages deploy (passed): browser suite 624/624 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against `https://davonlemar30.github.io/Ascendant/`, the seven shapes and the Practice checks included, no runtime errors.

## The journal's landing and the Big Three: the title page, the Keeper's record, Contents, the Library Map and the birth chart (batch 2)

The owner's rulings: Oct 1 (task 86bcbn6w6: the journal's architecture, with 1b C, the landing on the first open of each session, and 1e A, the title page once on the first-ever open) and Oct 2 evening (comment 90140263811408 and the owner's message the same evening: the inscription in the owner's words, the fonts Claude sources with open licenses only, the Practice door an entry point only until Practice's scope is approved, and the Big Three held for the step 2 proposal). The look is the art lane's board on 86bcbn6w6 (Oct 2).

**The pages** (`SliceFlow.JournalView` gains Title, Landing, Contents and Map beside the Wheel and a sign's page):
- **The title page (1e A).** On the journal's first-ever open, "Your Journal", a gold flourish, KEEPER and "These pages fill as you learn." settle for a second, then fade into the landing over 0.6 s. The save keeps `journalTitled`, so it never shows again; a save from before this build shows it once.
- **The landing (1b C).** The first open in each session lands here; later opens in the session go back where the journal was left.
  - The Keeper's record sits between two gold flourishes, each line on the page's rules as on the board: KEEPER (EB Garamond Bold, spaced 0.3 em), the Keys and Books ("1 Key"; "4 Keys · 1 Book": a Book counts once its three locks are filled), and the owner's inscription in EB Garamond Italic.
  - The inscription, word for word: "What's up, [Player Name]. I'm your journal, and I'll keep a record of what you learn from the Library.", with the saved name. It takes its own line breaks on every screen: the greeting on its own line, then "I'm your journal, and I'll keep a record" and "of what you learn from the Library." A line wraps between words only if a screen can't fit it in 244 px. (Breaking greedily put "of" at a line's end at some sizes, since Unity rounds each glyph's advance.)
  - The Big Three's line sits between the Keys and the inscription (below).
  - Two doors, 238 x 120, in the board's fine gold frame on the vellum panel: Practice (the closed book with its ribbon) and Contents ("Every chapter"). Practice is an entry point only (owner, Oct 2 evening): it shows at half, opens nothing, and has no count until Practice is approved.
- **Contents.** The Wheel ("Signs and patterns") and The Library Map ("The rooms you have woken"), each emblem in a gold ring, then two Sealed rows with a lock, as on the board. "‹ Your Journal" at the top left goes back to the landing.
- **The Library Map.** The parchment plan moved in from the mini-menu concept's Option C, with the rooms woken so far named in gold on their floors: a room is named when the Travel list has it, which is the Grand Atrium and the Zodiac Wing always and the Crystal Book Chamber once Key 1 is in its lock (`TravelOpen`), and nothing is dimmed. The page's layout is Claude's working choice, offered in the batch 2 report. "‹ Contents" at the top left.
- **The Wheel** is now a chapter: today's Wheel, with "‹ Contents" at its top left. Its Wheel / Table switch and its tabs move down a band (y 119 and 143, were 96 and 126), so the link keeps its 44 px target. A sign's page keeps Back to the Wheel.

**The Big Three** (owner, Oct 2 evening: "Big Three approved, A, download OK", on the proposal at 86bcbn6w6, comment 90140263824229; folded into this build at the owner's word):
- **The opening.**
  - "Enter birth date, time, place" asks three things in turn:
    - the date: day, month and year, from the Web's date picker or typed as DD/MM/YYYY;
    - the time: hour and minute, 24-hour or am/pm, or "I don't know my birth time";
    - the town or city: typed, then picked from up to four matches in the bundled list, the biggest first.
  - On the pick the game works out the chart once, and Continue fixes it.
  - "Enter what I already know" takes the sun, then the moon and the rising sign from the twelve, each of those two with "I don't know".
  - "I don't know" still has the game choose the sun.
  - The new lines are Claude's drafts, which the owner accepted for now.
- **The chart** (`BirthChart`, `Sky`) uses Meeus, *Astronomical Algorithms* (2nd ed.):
  - the sun's apparent longitude (chapter 25);
  - the moon's (chapter 47, the full sixty-term table);
  - Greenwich sidereal time (chapter 12);
  - the ascendant;
  - ΔT from Espenak and Meeus.

  It is checked against:
  - Meeus's worked examples: 25.a; 47.a's sum of -1127527 and its 133.162655°; 12.a and 12.b;
  - JPL Horizons at twelve moments from 1901 to 2026: the sun within 0.0001° and the moon within 0.0022°;
  - a search of the eastern horizon, to 0.005°;
  - a published chart: Honolulu, Aug 4 1961, 19:24, a Leo sun, Gemini moon and Aquarius rising.
- **The places and their clocks** (`Places`):
  - 34,152 places from GeoNames' cities15000 (CC BY 4.0; downloaded with the owner's OK), each with one of 356 time zones.
  - Each zone's UTC offsets from 1900 to 2040 come from the IANA time zone database on this Mac (2026c, public domain), through Python's zoneinfo. `Tools/make-places.py` rebuilds both files, and `Resources/Places/SOURCES.txt` records the sources and the credit.
  - A birth's local time becomes UT with that year's clocks. A time the clocks skipped takes the offset before the change; an hour they repeated takes its first pass.
  - 21 cases are checked against zoneinfo.
- **Unknowns** (owner, A; the canon's time-unknown state, Curriculum Revision 2).
  - With no birth time, the sun and the moon are worked out every hour across the whole local birth date. A sign that holds all day shows; one that changes is unknown. There is no rising sign without a time.
  - On a day the sun itself changed sign (about twelve days a year), the sun is Uncertain, and the player is asked (the cusp day, below).
- **The save** keeps the chart (`moonSign`, `risingSign`, `chartFrom`) with the birth data it came from (`birthDate`, `birthMinute`, `birthPlace`, `birthZone`, the latitude and longitude), and reads it back without working it out again. A save from before this build keeps its sun; its moon and rising read "unknown".
- **The Keeper's record** gains its Big Three line, on its rule between the Keys and the inscription: "☉ Taurus · ☽ Pisces · ↑ Leo", with "unknown" where a sign can't be known (a sun-only save: "☉ Taurus · ☽ unknown · ↑ unknown").
  - ☉ is drawn in Noto Sans Symbols 2.
  - ☽ is drawn in the zodiac font. Its baked character set gains ☽ through Unity's importer (`FontSetup`).
  - ↑ is drawn in EB Garamond, and the words in the italic.
- **DEV Mode**: each Jump to... checkpoint carries a sample chart, a real chart worked out for a 1990 birth mid-sign at noon in London, with the player's sun sign kept.
- **The credits**: the README's Credits gain GeoNames' line and the two new fonts.
- **The web state** publishes `birthStep`, `canBirthTime`, `canBirthPlace`, `placeMatches`, `canSignUnknown`, `moonSign`, `risingSign` and `bigThree`. The template gains the time and place inputs (the browser's own pickers), four match buttons and "I don't know".
- **The cusp day** (owner ruling, Oct 2 evening; it answers the canon's time-unknown state for the sun, which is Uncertain on that day).
  - With no birth time, on a day the sun changed sign, the opening asks: "The Sun moved from Aries into Taurus on the day you were born, at 9:27 am. Your birth time decides which side of that line you landed on. Which sign do you go by?"
  - The answers are the two signs and "I'm not sure". A small "Why?" opens its reason: "The Sun reaches each sign at an exact minute, and that minute shifts a little every year. Birthdays near the change are called cusps."
  - The time is the minute the sun entered the sign, on the birth place's clock that day, from the bundled zone table.
  - A pick is saved as the sun, flagged `picked`. "I'm not sure" saves the sun at local noon, flagged `noon` (approximate), so a later screen can offer a fix (`sunBasis`).
  - The record shows the sun the same either way; the moon and the rising sign keep rule A.
  - The lines are the owner's drafts.
  - DEV Mode's Jump to... gains "A cusp day (the opening)": a fresh opening at the question (London, Apr 20 1990, no birth time), the name kept.
  - Noted as a future idea only, not built: a cusp page, or a Practice question on cusps.
- **The sun, with higher accuracy.** To give the change's minute right, the sun uses Meeus's higher-accuracy method (VSOP87 for the Earth, truncated; Appendix III). Meeus 25.b's distance comes out exactly (0.99760775), and JPL Horizons agrees within 0.0001° at the twelve moments. The Taurus ingress of Apr 20 1990 falls at 08:27 UT, JPL's minute.

**3d.** The doors, rows and links press like the other buttons (darker, 1 px down), and Practice shows unavailable (half). Every new target is 44 px or more: the doors 238 x 120, Contents' rows 238 x 104, the links 96 x 44.

**The art.** Eight new slots, 166 in all, cut from the approved board's own sources (`Documentation/ConceptArt/BatchRun1-2026-10-02/1-journal/`):
- the door frame (the round-2 art's hairline frame, nine-sliced);
- five emblems: Practice, Contents, the Wheel (the game's own `journal-wheel`), the folded plan and the lock;
- the flourish;
- the Library plan.

The ring round each Contents emblem is drawn in code.

**The fonts** (owner, Oct 2 evening: "Claude sources them"; open licenses only):
- `Resources/Fonts/EBGaramond-Italic.ttf`, Version 1.001, 602,136 bytes, from github.com/octaviopardo/EBGaramond12 (`fonts/ttf/`). It is the static Italic of the release the shipped `EBGaramond-Bold.ttf` comes from (byte for byte). SIL Open Font License 1.1, covered by the existing `OFL-EBGaramond.txt`, which is identical to the upstream `OFL.txt`.
- `Resources/Fonts/NotoSansSymbols2-Regular.ttf`, Version 2.008, 671,568 bytes, from github.com/notofonts/notofonts.github.io (`fonts/NotoSansSymbols2/unhinted/ttf/`). SIL Open Font License 1.1, with `OFL-NotoSansSymbols2.txt` (the notofonts/symbols license and a note). It carries ☉ (U+2609), which the shipped Noto Sans Symbols lacks.
- Both are static fonts with no variation table (a variable font drew blank on the Web on Sept 12), imported dynamic. The batch 2 report had proposed google/fonts' variable `EBGaramond-Italic[wght].ttf`; the static file replaces it for that reason.

**The web state** publishes:
- `journalView`: title, landing, contents, map, wheel, table or sign;
- `journalKeeper`: the Keeper's record as drawn;
- `journalDoors`: the doors' boxes;
- `journalChapters`: Contents' rows;
- `journalMapRooms`: the rooms named on the plan;
- `journalLink`: the link at the top left;
- `journalScriptFont`;
- `canJournalContents`, `canJournalHome` and `canJournalPractice`.

The web template gains the matching semantic buttons and boxes.

**Checks**
- Mechanical (+40):
  - the first-ever open, the title page's beat, the landing, Contents both ways, both chapters and their links back, a sign's page still back to the Wheel;
  - opening where it was left in a session; a new session from the save; a save from before;
  - the inscription word for word; the Keys line;
  - both fonts with their characters and licenses, both static; the eight files at their sizes;
  - the links' targets clear of the switch, and the doors clear of Close the journal;
  - the chart: Meeus's worked examples, JPL Horizons, the horizon search and the published chart; the places and the 21 clock cases; the opening's three paths and the cusp day; the save both ways; DEV Mode's samples; the glyphs' fonts.
- The fixture:
  - the cusp day in the opening: the question, Why?, a pick; then "Enter what I already know" with I don't know for the moon and the rising sign;
  - the title page, then the landing's record as drawn (KEEPER, 1 Key, the Big Three as given, the inscription for the fixture's name), the two doors and the script font;
  - Contents' rows and their targets, and the Library Map;
  - the Wheel through Contents;
  - with the test set, a new session on the landing and every new file dressed.
- The suite:
  - the known path's moon and rising steps; the chart path in the browser (London, May 1 1990, 14:30: ☉ Taurus · ☽ Leo · ↑ Virgo); the cusp day (Aries or Taurus at 9:27 am, Why?, I'm not sure at noon); DEV Mode's cusp-day sample;
  - the title page, then the landing, with the Big Three's line, the inscription and the Keys line;
  - the inscription's ink in the Web build;
  - Practice opening nothing, on the canvas;
  - the Contents door, the Library Map's row, ‹ Contents and ‹ Your Journal, and the Wheel's row, all on the canvas;
  - the targets on Contents;
  - "4 Keys · 1 Book" and DEV Mode's sample chart at the Wing's end;
  - the moved switch and tabs.

Not changed: the Wheel, a sign's page and their actions; Close the journal; the journal's own buttons (Close the journal, the arrows, Back to the Wheel, Open the page) keep their look.

Validation (local, worktree `mini-menu`, branch `codex/journal-landing` on main `1c33acb`):
- mechanical checks 630/630 (+40), 0 compiler warnings;
- slice fixture 262/262 (+15);
- headless WebGL 0 errors and 0 warnings (36.1 MB; the place list and time zones add about 0.8 MB);
- browser suite 603/603 at desktop density, with the seven shapes (+29), and 512/512 at phone density (`DEVICE_SCALE=2 MOBILE=1`).

Captures: [`Evidence/journal-landing-2026-10-02/`](Evidence/journal-landing-2026-10-02/), with the approved board beside the game and the opening's birth steps, the cusp question included.

Merged as PR #110 (main `00cd011`, Oct 2 at 23:39 local, which is 07:39 UTC on Oct 3; a merge commit of main `1c33acb` and the head `dd7af50`; main's tree is the head's tree, checked with `git diff`). The head is three commits on main `1c33acb`: the journal `f4d9864` (Oct 2, 20:51 local), the Big Three and the records `99a78f8` (23:03), and `dd7af50` (23:06), which changes three comments only, in `BirthChart.cs` and `SliceView.cs` (checked with `git diff`; no code). The ladder ran on the working tree that became `99a78f8`'s code (Claude, Oct 2); the README's credits and the records were written after it, and `dd7af50` changes comments only, so the code on main is the validated code. The PR's GitHub Actions Web run on `dd7af50` (`37105225825`) passed (Build Web player succeeded; Deploy to GitHub Pages was skipped, as it is for a pull request); the main run (`37107066175`: Build Web player and Deploy to GitHub Pages) passed, and production served the merged build from 23:51 local on Oct 2 (07:51 UTC on Oct 3). Production recheck after the Pages deploy (passed): browser suite 603/603 at phone density (`DEVICE_SCALE=2 MOBILE=1`, the seven-shape pass included) against `https://davonlemar30.github.io/Ascendant/`, using `00cd011`'s own suite file.

## The buttons: bronze on the instruments, the chat box's language in the rooms (batch 2)

The owner's rulings (Oct 1, task 86bcbn6w6: 3b C, two tiers with engraved arrows for Previous and Next; 3c C, by place, bronze on the instruments and the chat box's gold on dark in the rooms; 3d, pressed darkens and sinks a pixel, unavailable dims to half, every target at least 44 px) and answers (Oct 2, batch run 2: "Buttons at 44 px, bronze on the Table and the Book too"). The look is the art lane's board on 86bcbn6w6 (Oct 2).

**The art.** Seven pieces, cut from the approved art so they fit any button (`ButtonLook`):
- the plate's frame without its centre marks, nine-sliced (40 px in at 2x);
- the plate's top notch and bottom diamond, laid back whole at its centre;
- the bronze arrow (Next is it mirrored, exactly as the art lane drew it);
- the rule's two lines (their tapered ends kept) and its centre diamond with its gaps.

Seven new slots, 158 in all. The room frame is drawn in code, the CASPAR box's shape at 2x with the chat box's see-through fill.

**By place**
- The instruments (the Dial, the Elemental Table, the Book of Symbols, practice):
  - SEAL is a plate with its word engraved at 31 px. Every other action is a plate with its word engraved: EB Garamond Bold in the board's gold, a dark edge and a soft shadow.
  - Previous and Next are the arrows.
  - The way out (Leave the Dial, Leave the Table, Leave the instrument, Close the Book) is gold lettering on the rule.
- The rooms (the Atrium, its story pages, the Zodiac Wing, the Chamber): the chat box's see-through dark fill and gold hairline, and small gold capitals (13 px bold, tracked 2.2 px), kept in capitals when the words change.
- Not changed: Insert Key keeps its crimson (owner, worksheet section 13); the journal's buttons (the journal's own build); the opening's name and birth screens (neither a room nor an instrument); Settings and DEV Mode; the Table's cells and sign tiles and the Dial's seats, which are the instruments' own pieces.

**3d, the states.** Pressed: every piece darkens (x .78), the words take their darker gold, and the look sinks 1 px. Unavailable: the whole button at half. Keyboard focus keeps today's warm tint; a tap leaves no focus behind.

**3d, 44 px.**
- Your journal in the Wing and the Chamber goes from 40 to 44.
- The Atrium story's Continue goes from 42 to 44.
- The Table's sign tiles go from 40 to 44, on rows 46 apart starting 2 px higher, so the last row clears the readout.
- The gear and the mini-menu button keep their 36 px icons and take taps over 44 x 44.
- The Atrium's desk (70 x 30) takes taps over 70 x 44, and the Caspar box's Continue link (96 x 26) over 96 x 44.
- The web template's boxes follow.

**The web state** publishes `buttons`: each button on screen as "words:look:width x height", the target's size.

**Checks**
- Mechanical: the seven pieces at their slots' size. A test button in each look builds as the board shows it, and pressed and unavailable apply.
- The suite:
  - the Atrium and the Zodiac Wing wear the room look;
  - the Dial's SEAL is a plate, Previous and Next are arrows, and Leave the Dial is on its rule;
  - the Book of Symbols' four names are plates and Close the Book is on its rule;
  - the Table's SEAL is a plate, Leave the Table is on its rule, and its tiles are 44 px tall;
  - on each of those screens, every target is 44 px or more.

Not changed: every button's words, place and action.

Validation (local, worktree `mini-menu`, branch `codex/buttons` on the Dial's final art, then rebased onto main `0d7ee4b`, docs only): mechanical 590/590 (+1, the looks and states; 0 compiler warnings); slice fixture 247/247; headless WebGL 0 errors / 0 warnings (34.5 MB); browser suite 574/574 at desktop density, with the seven shapes (+10: the looks on five screens at each width) and 483/483 at phone density (`DEVICE_SCALE=2 MOBILE=1`; the seven-shape pass runs at desktop density only). Captures: [`Evidence/buttons-2026-10-02/`](Evidence/buttons-2026-10-02/) (the board beside the game).

Merged as PR #108 (main `749c4ee`, Oct 2 at 09:11 local, which is 17:11 UTC; head `ba24369`, three commits on main `0d7ee4b`: the code `56169fc` and `86d4907`, then the records commit, which changed only docs; main's tree is the head's tree, checked with `git diff`). The ladder ran on the two code commits as local commits `7548f4c` and `e4ab192`, stacked on the Dial's final art (never pushed). The branch was then rebased onto main `0d7ee4b` after #107 merged. Nothing under `Assets` or `Tools` differs between `7548f4c` and `56169fc`, or between `e4ab192` and `86d4907` (checked with `git diff`; only #106's and #107's own records, which the rebase brought in), so the code on main is the validated code. The PR's GitHub Actions Web run on `ba24369` (`37037811309`) passed; the main run (`37039069415`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 574/574 at phone density (`DEVICE_SCALE=2 MOBILE=1`, the seven-shape pass included) against `https://davonlemar30.github.io/Ascendant/`, using `749c4ee`'s own suite file (identical to the validated code's).

## The Dial's final art: its room as a master in every look, the bright passes, the small Dial (batch 2)

The owner's answers (Oct 2, batch run 2, given in Claude's session; Decisions Log, Sept 28 to Oct 4, Part 2; tasks 86bcbn6mf and 86bcbn6w6): "Bleed masters approved", "Make the Dial room's worn and bright masters from the colour tables", and "Keep the repeating cobwebs, leave the glow".

**The room.** dial-room is the approved 1200 x 1840 master. Today's file had 20 clear pixels at the column's top left (x 0 to 3, y 0 to 4), which the master turned black; they take the colour of the next pixel in their row.

**The worn and bright masters.** The passes change only the Dial; the room outside it is today's pixels in every look. So each look's master is the approved master with the pass's own Dial pixels in the column (exact). Past the column, the colour tables grade the only Dial pixels there: the tips of the 9 and 3 o'clock diamonds, which the outpaint completed about 8 px into the margins (182 pixels). The worn tips take the worn pass's dust from the column's edge pixel in their row; the bright tips take the rim's partial lift (.55), as the pass does.

**The diamonds, corrected.** The approved worn and bright passes graded only part of the 12, 9 and 3 o'clock diamonds. Their outer facets and tips kept today's colour behind a hard diagonal edge, because the pass's diamond outline (`diamondW`) is smaller than the painted diamonds. The 6 o'clock diamond was whole, and the stand under it rightly stays today's.
- Rerunning the art lane's own pass code (`finalize-passes.cjs`) with the original weights gives the approved files exactly (0 channel values differ).
- With every bronze pixel of those three diamonds in the finish, 2,794 pixels are added to the finish and 2,792 of them change value in each room look (two were already the graded colour), all of them in those diamonds.
- The bright light (the recipe's glow) changes at 543 pixels, all on them too.
- The small Dial's files are rebuilt from the corrected passes with the art lane's kit tool. With the original weights it reproduces the approved kits exactly; with the fix, about 420 to 460 pixels change, all at the diamonds, and the 12 o'clock tip is now whole.

**The glow keeps its column files.** The Dial's glow (`dial-room-light` and its looks) is a radial fill over the column only, for the reveal sweep, and `Bleed` leaves a filled picture alone. A master there would squeeze into the column. The light master's margins are clear and its centre is today's glow, so the 720 x 1600 files draw exactly what the master would.

**The rest of the art.** The bright ring and its light (720 x 720) are in. kit-dial-bright and -open are in (2d A), and kit-dial-worn and -open are redrawn to the worn pass, in the same slots.

**Checks**
- Mechanical: the room's three looks are 1200 x 1840 masters, the glow's three files 720 x 1600, the turning ring's worn and bright looks 720 x 720, and the small Dial's four files at their slots' size.
- The suite: at each of DEV Mode's five wake steps, the Dial draws the step's room looks as masters and no look stands in. At the seven shapes, the Dial's room draws its masters (worn and today's at Key 1).

Not changed: the Dial's geometry, words, seats, eye and lessons, and the wake-up's steps and timing.

Validation (local, worktree `platform-fit-2`, branch `codex/dial-final-art` on the bleed masters' commit `a72e4fc`, then rebased onto their records commit `81531f0`, docs only): mechanical 589/589 (+1, the Dial's final art; 0 compiler warnings); slice fixture 247/247; headless WebGL 0 errors / 0 warnings (34.4 MB, from 32.8 MB: the Dial's three masters and the bright files); browser suite 564/564 at desktop density, with the seven shapes (+17: 10 at the wake steps, 7 at the shapes) and 473/473 at phone density (`DEVICE_SCALE=2 MOBILE=1`; the seven-shape pass runs at desktop density only). Captures: [`Evidence/dial-final-art-2026-10-02/`](Evidence/dial-final-art-2026-10-02/) (the Dial at the three widest shapes, stop-gap then masters; the five steps with the final art; the diamonds before and after at 5x).

Merged as PR #107 (main `0d7ee4b`, Oct 2 at 08:16 local, which is 16:16 UTC; head `ef29d43`, two commits on main `e9451b0`: the code `cfe62f6`, then the records commit, which changed only docs; main's tree is the head's tree, checked with `git diff`). The ladder ran on the same code as local commit `66f16b8` on #106's art commit `a72e4fc` (never pushed; the PR's description names it). The branch was then rebased onto #106's records commit `81531f0` and, after #106 merged, onto main `e9451b0`. Nothing under `Assets` or `Tools` differs between `66f16b8` and `cfe62f6` (checked with `git diff`; only #106's own records, which the rebase brought in), so the code on main is the validated code. The PR's GitHub Actions Web run on `ef29d43` (`37031000216`) passed; the main run (`37032819714`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 564/564 at phone density (`DEVICE_SCALE=2 MOBILE=1`, the seven-shape pass included) against `https://davonlemar30.github.io/Ascendant/`, using `0d7ee4b`'s own suite file (identical to the validated code's).

One change the room paragraph above does not name, found by comparing the merged files with the ones they replaced (Oct 2): in today's `dial-room` and in the approved worn file, the column's fifth pixel column (x = 4, all 1,600 rows) is 80% opaque (alpha 204). In the masters it is opaque, with the colour unchanged apart from the diamond fix above. It is the same kind of change as the Atrium's bottom row in #106, and it is meant: the approved master is opaque everywhere, its colour is unchanged, and it shows no visible line at the seam (Claude, Oct 2).

## The bleed masters: the Atrium, the Zodiac Wing and the Chamber (batch 2)

The owner's answers (Oct 2, batch run 2, given in Claude's session; Decisions Log, Sept 28 to Oct 4, Part 2; task 86bcbn6mf): "Bleed masters approved", and "Bleed: i OK, ii default, iii keep, iv no repaint".

**The art.** Nine files drop into their slots as 1200 x 1840 masters: atrium, wing and chamber, and their light and grime layers. Part 2's `Bleed` draws a master whole, centred on the column, so the art needs no code. Each master's column is today's painting:
- atrium: its bottom row, 86% opaque in today's file, is now opaque (its colour is unchanged);
- wing-grime: the default file (ii), which fills the column's side fade, so the column's own grime changes at 55,177 pixels;
- the other seven: the column is byte-identical.

The Dial's room and its looks follow in their own PR.

**The web state** publishes `masters`: the full-screen layers on screen that draw a master whole, by file (`Bleed.Masters`).

**Checks**
- Mechanical: the nine files are 1200 x 1840 masters.
- The suite: the Atrium draws its master at both widths and densities; at all seven shapes, the Atrium, the Zodiac Wing and their lights draw theirs.

Not changed: the column's layout, the doors, the tap targets, and the stop-gap for any file that is not a master.

Validation (local, worktree `dial-wake`, branch `codex/bleed-masters` on main `82176e8`): mechanical 588/588 (+1, the masters; 0 compiler warnings); slice fixture 247/247; headless WebGL 0 errors / 0 warnings (32.8 MB, from 28.4 MB: the masters); browser suite 547/547 at desktop density, with the seven shapes (+16: 2 at the opening, 14 at the shapes) and 463/463 at phone density (`DEVICE_SCALE=2 MOBILE=1`; the seven-shape pass runs at desktop density only). Captures: [`Evidence/bleed-masters-2026-10-02/`](Evidence/bleed-masters-2026-10-02/) (the Atrium and the Zodiac Wing at 9:16, 10:16 and 3:4, stop-gap then master).

Merged as PR #106 (main `e9451b0`, Oct 2 at 07:51 local, which is 15:51 UTC; head `81531f0`, two commits on main `82176e8`: the code `a72e4fc`, then the records commit, which changed only docs, so the code on main is the validated code, and main's tree is the head's tree, checked with `git diff`). The PR's GitHub Actions Web run on `81531f0` (`37028723095`) passed; the main run (`37029920803`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 547/547 at phone density (`DEVICE_SCALE=2 MOBILE=1`, the seven-shape pass included) against `https://davonlemar30.github.io/Ascendant/`, using `e9451b0`'s own suite file (identical to the head `81531f0`'s).

## The Dial's wake-up: five steps by the Keys (APK Session 2, polish 1)

The owner's rulings (Oct 1, the ruling comments on task 86bcbn6w6; Decisions Log, Sept 28 to Oct 4, Part 2):
- The order: worn, then today's Dial, then bright and new.
- 2a A: a step per Key. 2b B: five steps blended from three drawn looks (the first visit worn; Key 1 halfway from worn to today's; Key 2 today's; Key 3 halfway to bright; Key 4 bright and new).
- 2c A to C: the dust thins, the tarnish polishes, the glow strengthens. D is excluded, so the eye never changes.
- 2d A: the Wing room's small Dial follows the same steps.
- Board 2, round 2 is approved, with the bright finish's outer glow dialled back. The build uses stand-ins until Lane 2's final passes are approved, and the swap is a file drop.
- DEV Mode gets a jump to each step.

**The Dial** (`DialView`)
- Each of its four layers (the room, its light, the turning ring, its light) has a twin over it that blends toward the next look. `WakeStep` 0 to 4 picks the base look and the twin's blend (`WakeBlend`): worn; worn plus half today's; today's; today's plus half bright; bright.
- A look's file is the layer's slot with `-worn` or `-bright` (`dial-room-worn`, `dial-ring-light-bright` and so on). A look whose file is not in yet stands in with today's.
- The eye is drawn the same in every look. The twins keep their layer's radial reveal and its reach to the screen's edges (Part 2).

**The step** (`SliceView`)
- The step is the Keys earned (0 to 4). It lands the next time the player comes to the Dial, never mid-lesson (Claude's working choice; the Key 4 look greets the player on the visit after the Key).

**The Wing room's small Dial** (2d A): worn; half today's over it at Key 1; restored at Key 2 (its kit placement moves from Key 4 to 2); half bright over it at Key 3; bright at Key 4 (`kit-dial-bright` and `kit-dial-bright-open`, new slots; the restored pair stands in until they land).

**DEV Mode.** Settings, Testing, Jump to... gains a "Dial wake" row. Each tap steps the look (first visit, Key 1 to Key 4, then back to as earned) without touching the save.

**The art.** Ten new slots (151): the four layers' worn and bright looks, and the small Dial's bright pair.
- The worn stand-in is Lane 2's worn pass: the approved round-2 worn concept's tarnish, dust and cobwebs on the exact geometry, cut from one segment so the twelve stay identical. Its four files are in. A revised pass is a file drop.
- The bright files are not in, waiting on the owner's approval of Lane 2's bright pass, so Keys 3 and 4 show today's Dial until they land.

**Checks**
- The mechanical checks hold the five blends and every new slot at its layer's size.
- The suite, after each DEV checkpoint: the Dial shows the Keys' step.
- The suite, through DEV Mode's preview: each step blends the looks the ruling names (`dialLook`, with the stand-ins marked), captured on the Dial at each step, and back to as earned.
- Counts: 151 slots and test files everywhere they are quoted (the mechanical checks, the fixture, the suite, this page's mirror in ART-SLOTS.md).

Not changed: the Dial's geometry, words, seats, eye and lessons; today's look (step 2) is today's files.

Validation (local, worktree `dial-wake`, branch `codex/dial-wake-steps` on Part 2): mechanical 587/587 (+1, the five blends; 0 compiler warnings); slice fixture 247/247; headless WebGL 0 errors / 0 warnings (28.4 MB); browser suite 531/531 at desktop density, with the seven shapes (+22: 11 at each width) and 461/461 at phone density (`DEVICE_SCALE=2 MOBILE=1`; the seven-shape pass runs at desktop density only). Captures: [`Evidence/dial-wake-2026-10-02/`](Evidence/dial-wake-2026-10-02/).

Merged as PR #104 (main `58d00c3`, Oct 2 at 03:58 local, which is 11:58 UTC; head `f057aee`, one commit, rebased onto main `4fd0b6f` after PR #103 merged. After the ladder only the validation numbers and the evidence board were added, and the rebase onto main changed no file (checked with `git diff`), so the code on main is the validated code). The PR's GitHub Actions Web run on `f057aee` (`37002885709`) passed; the main run (`37003941708`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 531/531 at phone density (`DEVICE_SCALE=2 MOBILE=1`, the seven-shape pass and all 22 wake-up checks included) against `https://davonlemar30.github.io/Ascendant/`, using `58d00c3`'s own suite file (identical to the validated head `f057aee`'s).

Update (Oct 2, PR #107): the bright files and the final worn and bright masters landed, so no wake step stands in any more. See "The Dial's final art" above.

## Platform fit, Part 2: the art fills the screen (APK Session 2, finding 1b)

The owner's rulings (Oct 1; task 86bcbn6mf; the owner's record doc 2kyd583p-7114, points 1 to 4):
- Every phone from 9:16 to 9:23 fills edge to edge. Tablets, the Fold's inner screen and desktop windows keep the centred column, with room art down the sides.
- Each room and the Dial's room get one 600 × 920 master (1200 × 1840 in the file), cropped from the centre. AI drafts serve only as the stop-gap until a cleaned master lands.
- The extra space is decoration only: the floor, the doors, the Dial and the chat box stay in the column with their tap targets.
- Phone browsers fill like the app.

**Code**

- **The column.** The 360 × 800 frame is now the centred design column, under the same scale rule as before (the smaller of the width over 360 and the height over 800). Every layout, door and tap target stays where it was.
- **The art reaches the edges** (`Bleed`, a mesh effect on each full-screen layer):
  - Layers: the rooms and the Atrium's opening screens, their light, grime and veil layers, the Dial's room and its two fades, the journal's page, the black fade, the Settings veil, and every plain screen.
  - A file the master's shape (600:920) draws whole, centred on the column. So a master is a file drop: the same slot, a bigger file, no code change.
  - Until it lands, today's 720 × 1600 painting stands in: the column draws as it was, and past it the painting continues outward, mirrored at its edges and dimmed toward the screen's edges (to 55%). This is point 2's stop-gap.
  - The Dial's room is the exception. Its rim runs to within 4 px of the column's sides, so a mirror would hang half-wheels in the margins; its edge colours carry outward instead, darkened to 35%.
  - The journal's book stays in the column; past it the desk's edge carries on, darker (a mirror would show a second spine).
  - Plain fills and the gradients just grow.
  - Only the sides on the column's edge grow: a band on the column's top grows up and sideways.
  - Taps stay in the column.
- **Phone or tablet.** A screen is a phone when its width over its height is at most 9:16. On a phone the system buttons keep to the screen's safe corners, as far in as they sit from the column's corners today (22 px): the gear top right, the mini-menu button top left with its panel under it. Everything else stays in the column. On a tablet or a desktop window the system buttons stay with the column.
- **The web.** The web state reports `gearAt` and `travelAt`, and the template's semantic gear, mini-menu button and rows follow them. The other semantic boxes keep the column's frame as before.

**Checks**

- The mechanical checks build the mesh. A 1200 × 1840 file draws whole across 600 × 920. A 720 × 1600 painting keeps its column and continues in a 3 × 3 grid. A band on the column's top grows up and sideways only.
- The suite's seven-shape pass runs at 9:16, 9:19.5, 9:20, 9:21, 9:23, 10:16 and 3:4, captured at 3x. At every shape:
  - the Atrium's, the Wing's, the Dial's and the journal's art reaches every edge (no flat bar in any margin);
  - the gear and the mini-menu button sit where the web state says (at the screen's corners on a phone, at the column's on a tablet) and take their canvas taps there, with the semantic gear on the gear;
  - the Zodiac Wing door takes its canvas tap in the column.
- The main suite also runs at 390 × 844 (9:19.5) and 360 × 800 (9:20), at both densities.

**The Atrium bookshelf, rechecked** (task 86bc8ddwh, ruled web-only on Sept 29). On 9:16 phones and on tablets it now stands whole on screen, over the stop-gap. On 9:19.5 to 9:23 phones the column fills the screen's width, so the shelf still runs 35 px past the edge, as painted. That remains the art question of the Sept 27 diagnosis: the Atrium's master, or a recomposed Atrium.

Not changed: the column's layout and its tap targets, the rooms' approved compositions, every word, the stand-ins' pixels inside the column.

Validation (local, worktree `platform-fit-2`, branch `codex/platform-fit-part2` on main `547b7bd`): mechanical 586/586 (+1, the mesh; 0 compiler warnings); slice fixture 247/247; headless WebGL 0 errors / 0 warnings (27.7 MB); browser suite 509/509 at desktop density, with the seven shapes (+70: each shape's four screens, the corners, the gear's tap and box, the Travel tap, the door, no runtime errors), and 439/439 at phone density. Captures: [the Atrium](Evidence/platform-fit-part2-2026-10-02/shapes-atrium.jpg) and [the Dial](Evidence/platform-fit-part2-2026-10-02/shapes-dial.jpg) at the seven shapes, at 3x. On the 10:16 and 3:4 tablets the Dial room's stop-gap shows its edge colours stretched sideways until its master lands.

Merged as PR #103 (main `4fd0b6f`, Oct 2 at 03:27 local, which is 11:27 UTC; head `9841983`, one commit on main `547b7bd`, so the code on main is the validated code). The PR's GitHub Actions Web run on `9841983` (`37000108109`) passed; the main run (`37001116108`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 509/509 at phone density (`DEVICE_SCALE=2 MOBILE=1`, the seven-shape pass included, which the local phone run had left out) against `https://davonlemar30.github.io/Ascendant/`, using `4fd0b6f`'s own suite file.

Update (Oct 2, PRs #106 and #107): the masters landed (the Atrium, the Zodiac Wing and the Chamber in #106, the Dial's room in every look in #107), so the stop-gap now covers only a file that is not a master. See "The bleed masters" and "The Dial's final art" above.

## The room mini-menu (task 86bca07wv)

The owner's ruling (Oct 1, the ruling comment on 86bca07wv; Decisions Log, Sept 28 to Oct 4, Part 2): Option A, the room list, with v2 spacing (the 280 px panel). The one dim Sealed row with its lock stays: it hints that the Library has more to wake. Rooms only: the instrument screens keep their own exits. Travel only (the Grand Atrium, the Zodiac Wing, the Crystal Book Chamber), which also answers 3a on 86bcbn6w6 as A; Your journal stays a room button. The button sits at the top left, mirroring the Settings gear, inside the safe area.

- **The menu** (`TravelMenu`). A round gold button at the top left (x −158, 22 from the top, 36 × 36: the gear's mirror) drops the slim-box panel headed TRAVEL. The panel's left edge is at 17, it is 280 wide, it hangs from 47, and its rows are 44 apart, the concept's measures.
  - Each room open to travel gets a row with its line icon (dome, wheel, crystal) and its name in 15 px type.
  - The room you are in sits on an amber bar and reads "here".
  - Last comes one dim Sealed row with a lock. The Chamber is a room once Key 1 is in its lock; until then it folds into the Sealed row.
  - The button and the panel sit below the cutout's band (Part 1).
- **Travel.** A row takes the doorways' own steps, out to the Atrium and then in, with the door's sound and one fade in place of the walk. The Atrium's stages, the save and the Keys follow as they do through the doors.
  - Picking the room you are in closes the panel; so does a tap off it. The Sealed row is no door.
  - The menu shows in the rooms only (the Atrium, the Wing, the Chamber as a room). It does not show on an instrument, in the journal, in the opening or on the style page.
- **Working choices** (Claude's, inside the build's latitude, recorded on the Decisions Log):
  - Quick travel is a fade, not a walk.
  - The button's mark (three linked stars) and the row icons are drawn in code, like the gear, so no art slot is added.
- **Checks.**
  - The web state reports `travelShown`, `travelOpen` and `travelRows`. The template has a semantic button (`aria-expanded`) and one row per open room (`aria-current` on the room you are in).
  - The suite, on the canvas at the opening: the button opens the panel, the Sealed row is no door, a tap off the panel closes it, the Wing row travels, the semantic rows sit on the panel's rows, and the Atrium row comes back.
  - The suite, after Key 3: two hops each way (the Wing to the Chamber and back, no Key spent), and no button on the Dial.
  - The fixture: the opening round trip.

Validation (local, worktree `mini-menu`, branch `codex/room-mini-menu` on main 564abae): mechanical 585/585 (0 compiler warnings); slice fixture 247/247 (+5, the round trip); headless WebGL 0 errors / 0 warnings (27.7 MB); browser suite 439/439 at desktop density (+22: 11 at each width) and 439/439 at phone density. Evidence: [the approved concept beside the game](Evidence/mini-menu-2026-10-02/board-minimenu.jpg) (the Zodiac Wing at Stage 1, TRAVEL open and closed).

Merged as PR #102 (main `547b7bd`, Oct 2 at 02:42 local, which is 10:42 UTC; head `d6635f1`: the code commit `997c2d3` validated above, then the records commit `d6635f1`, which changes only the docs and the concept board, on main `564abae`, so the code on main is the validated code). The PR's GitHub Actions Web run on `d6635f1` (`36995282594`) passed; the main run (`36996996374`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 439/439 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against `https://davonlemar30.github.io/Ascendant/`, using `547b7bd`'s own suite file.

## Platform fit, Part 1: no status bar, drawn behind the cutout (APK Session 2, finding 1a)

The owner's rulings (Oct 1; task 86bcbn6mf; the owner's record doc 2kyd583p-7114 and the Decisions Log): "Hide the bar and draw behind it"; Part 1 (immersive fullscreen, drawn behind the cutout, the top row inside the safe area) ships alone first, Part 2 (filling the screen) as its own PR.

- **Where the bar came from.** The Android build method lived only on the unmerged `codex/android-apk` branch, and it set `renderOutsideSafeArea = false`. The player settings asked for fullscreen, yet on the emulator (Android 14, 1080 × 2400, 9:20) the Oct 2 APK showed Android's grey status bar (`statusBars visible=true`, 136 px with a punch-hole cutout emulated), the game drawn below it, and the 9:20 frame pillarboxed in what was left (drawn at 2.83x: the dark strips at the sides in the owner's photo).
- **The landing** (Claude's working choice, the cleanest of the ways with that branch in play): `Assets/Editor/Build/AndroidBuild.cs` comes into main with its `.meta` (the branch's one commit, `8d316f7`), so main is the one source for an APK cut and the branch retires. It now renders outside the safe area in a fullscreen window. `SafeArea.Immersive` hides the status and navigation bars at startup on Android, and again whenever the game regains focus; a swipe from an edge shows them for a moment.
- **The top row.** `SafeArea.TopInset` is the screen's unsafe top band (`Screen.safeArea`) where it reaches into the 360 × 800 frame, in layout units. Each screen's title and the Settings gear (and the mini-menu button, when it comes) move down by it. The lines under a title (the subtitles, the Keys line, the Atrium's caption) move by what is left after a 13 px gap, so a header keeps its order without pushing into the Dial below it. The journal's title, inside the book, moves only once the band reaches its glyphs. The web has no band; the test-only web action `safe-inset:<px>` simulates one.
- **Android's font** (found on the emulator, fixed here). Android draws the built-in UI font in its own system sans, a little wider than the web's. Drawn at exactly 3x (the frame filling a 9:20 phone), a line tuned to its box spilled over and lost its end: Caspar's "...when you are ready." in the Atrium and the name screen's second sentence. At 2.83x, behind the bar, the text drew a little smaller, which hid it. On Android such a line now shrinks by up to two points to fit (`SafeArea.FitAndroidText`); a line that fits keeps its size, and the web and the Editor draw exactly as before.
- **Checks.** The suite: with a 24 px band the gear moves below it (a canvas tap at its new place opens Settings, its old place no longer does, its semantic box follows), and with none it is back. The fixture: the Atrium's title and the gear move down 24, the caption under the title 11, and back. The mechanical checks: the text net on Android, untouched elsewhere.
- **Evidence.** [The emulator, before and after](Evidence/platform-fit-part1-2026-10-02/part1-emulator-board.jpg): the Oct 2 APK with its bar and strips; Part 1 with no cutout (edge to edge); Part 1 with a punch-hole cutout, on the name screen and in the Atrium (the art behind the cutout, the title and the gear below the band). [The two Android lines, before and after the text net](Evidence/platform-fit-part1-2026-10-02/part1-text-board.jpg).

Not changed: the 360 × 800 layout and its scale rule, every tap target below the top row, the web build's look, Part 2's fill (the frame still pillarboxes on phones wider or narrower than 9:20).

Validation (local, worktree `platform-fit`, branch `codex/platform-fit-part1` on main `ec4ba1a`): mechanical 585/585 (+1, the text net; 0 compiler warnings); slice fixture 242/242 (+2, the band); headless WebGL 0 errors / 0 warnings (27.7 MB); browser suite 417/417 at desktop density (390 and 360; +4, the band at each width) and 417/417 at phone density (`DEVICE_SCALE=2 MOBILE=1`). Android: a development build of the branch (not delivered; the next cut is on the owner's word) on the emulator, with and without the punch-hole cutout overlay. Not tested: the owner's phone.

Merged as PR #101 (main `564abae`, Oct 2 at 02:12 local, which is 10:12 UTC; head `2d13d13`: the code through `703ee5c` validated above, then the records commit `2d13d13`, which changes only the docs, on main `ec4ba1a`, so the code on main is the validated code). The PR's GitHub Actions Web run on `2d13d13` (`36992774782`) passed; the main run (`36994167093`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 417/417 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against `https://davonlemar30.github.io/Ascendant/`, using `564abae`'s own suite file.

## The shelf's glow, without the smear (APK Session 2, bug 1)

The owner's APK Session 2 report (Oct 1, page 2kyd583p-25354, bug 1; task 86bcbn6ct): "After activation, the bookshelf smears into a stretched, motion-blurred look." No new ruling: Build Y's stands (the shelf itself glows, a soft gold edge hugs its silhouette); only the edge's drawing changed.

- **The cause.** Build Y's shelf light (`SliceView`, "Shelf light", PR #88) drew its edge with two `Outline` effects at 1.5 and 3.5 px. An `Outline` repeats the whole textured picture at each of its four offsets; it does not trace a silhouette. The two together laid eight shifted copies of the shelf at half strength over it, up to 3.5 px on the layout (about 10 px on a phone), and the breathing pulse moved the ghosts. Build AA met the same trap on the journal's panels.
- **The fix.** The two `Outline`s are gone. The edge is a halo made once from the shelf file's own alpha (`SilhouetteHalo`): the file is read back through the GPU (slot files import unreadable), its alpha spread by 3 file pixels and softened twice by 4, then cleared wherever the shelf itself is opaque, so it lies only around the silhouette and no copy of the picture's detail is offset. It is a child of the shelf light, so Build Y's levels (full while the book waits, .3 after) and its breathing (still under reduced motion) carry it. The warm wash (the shelf's gold copy at 22%) stays: it is an exact overlay, not an offset copy.
- **Checks.** The web state reports `shelfEdge` ("halo" when the edge comes from the silhouette); the suite holds it beside Build Y's check at both widths, and the fixture checks the shelf light has no `Outline` and a halo wider than the shelf.
- **Evidence.** [Before and after at 3x](Evidence/shelf-glow-2026-10-02/shelf-before-after-3x.jpg): the waiting and faint states, production (Build Y) against the fix, captured at device scale 3. Before, the pillar, its bust and the orrery are ghosted; after, they are sharp, and the gold sits around the shelf's outline.

Not changed: the shelf's file, its place, its levels and breathing, the state fields `shelfLight` and `shelfRing`, anything else drawn.

Validation (local, worktree `greybox`, branch `codex/shelf-glow-silhouette` from main `2599659`): mechanical 584/584 (0 compiler warnings); slice fixture 240/240 (+1, the halo check), no runtime errors; headless WebGL 0 errors / 0 warnings (27.7 MB); browser suite 413/413 at desktop density (390 and 360; +2, the halo check at each width) and 413/413 at phone density (`DEVICE_SCALE=2 MOBILE=1`). Not tested: the phone itself; the next APK cut is on the owner's word.

Merged as PR #100 (main `ec4ba1a`, Oct 2 at 01:14 local, which is 09:14 UTC; head `4d7c746`, one commit on main `2599659`, so the code on main is the validated code). The PR's GitHub Actions Web run on `4d7c746` (`36987552332`) passed; the main run (`36988619666`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 413/413 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against `https://davonlemar30.github.io/Ascendant/`, using `ec4ba1a`'s own suite file.

## The Cast Dial: the phoenix holds the words (Build AC)

The owner's rulings (Oct 1, Decisions Log week of Sept 28–Oct 4; task 86bcbhcj8): Build AB held unmerged as the backup; one more concept pass; direction A, cast and engraved; the ribbon hub; no count field; the look passes on the reference `Documentation/ConceptArt/DialRedesign-2026-10-01/dial-A3-ribbon.png` (main checkout, untracked); the count word goes inside the eye, under the challenge line.

- **Art** (art commit `15a0354`, ahead of the code; files and tools in `DialRedesign-2026-10-01/production/` and `tools/`). The reference was a generated picture: eleven segments round the wheel, and its rings not quite concentric (the bands' centre 4 px from the hub's, the rim 2 px off again). The production ring is therefore built, not copied: the bands' centre fitted from the window and recess faces (sub-pixel residuals), one clean segment (12 o'clock) resampled about it into an exact 30° wedge with its spoke zones compressed (the faces had to reach 26.5° for the recesses and 23.5° for the windows, what "Sagittarius" at 13 px and "Cardinal" at 11 px need with margins), repeated twelve times with one segment centred at 9 o'clock, a round rivet pasted unrotated at each junction, the hub ring lathed from one clean angle, the drawn rim kept and its groove tidied. The hub interior keeps the reference's pixels, re-centred on the eye. `dial-room` carries the room, the stand, the rim with its ticks and four diamonds, the hub ring and the still hub (the phoenix holding the ribbon, the eye, the tail whole below it); `dial-ring` the twelve segments; both glows from their layer's own bright brass (the medallion's method); `bracket` a gold wedge outline over the 9 o'clock recess and window (slot 64 × 88 → 72 × 88); the four `kit-dial-*` pieces the same wheel on the Wing kit's stand, the resting eye from a generated closed-eye edit merged into the reference, the worn state dulled.
- **The Dial** (`DialView`): names on radius 146 in recesses of 26.5° (`NameRoom` about 61 px; Capricorn and Sagittarius at 13 px, the rest at 14); each window (radius 109) carries its fact on its outer half (radius 118, about 42 px of room) and its symbol on its inner half, the reverse of the Astrolabe's tablets. The framed sign's name and its learned facts curve along the cast ribbon (`ArcText`, the name's arc on radius 117.5, the facts' 11 px inside it, one centre, following the ribbon's own bow). The challenge sits in the eye's glass (150 × 33 at y 267 on the layout; box 128 × 34, 13–16 px as before). While a worked count runs, the challenge folds onto one line across the glass's widest rows (10–12 px) and the count word takes the line under it, in the eye's own voice; two lines and a count would not fit a 33 px glass at any readable size (Unity's line for the serif is about 1.25 × the size), and the approved eye was not enlarged. No count plaque. Seats 58 × 66 on radius 123; the frame 72 × 88. The family lines stay off the wheel (a completed family's windows turn gold), as Build AB left them.
- **The Wing room's Dial** (`SliceView`): the eye's centre at .404 (restored) and .428 (worn) of the file's height; the suite reads the glass at (219, 317).
- **Checks:** the mechanical checks measure the ribbon's rooms with the real font, the count mode's folded lines (every challenge on one line at 10 px or more) and the count words, and the frame's size; the fixture proves the ribbon's two arcs, the eye's two layouts (and that it returns), and every name, facts line and count word in its place, with the 24-position sweep now on the windows' outer half; the Wing Dial's eye is read off the screen at its new spot.
- **Not changed:** the lessons, the challenges, Caspar's lines, the save.

Validation (Unity 6000.3.24f1, local, worktree `greybox`, branch `codex/dial-cast`): mechanical 584/584 (0 compiler warnings); slice Play Mode fixture 239/239, including the 24-position sweep, the ribbon and eye checks, nothing truncated, no family line over the hub; headless WebGL build 0 errors / 0 warnings (27.7 MB); browser suite 407/407 at desktop density (390 and 360) and 407/407 at phone density (`DEVICE_SCALE=2 MOBILE=1`). Found and fixed on the way: the reveal still parked the eye at Build Z's height (270) after it played, 3 px low in the new glass (caught by the fixture's eye check on the first full run); Unity's line height for the serif (about 1.25 × the size) ruled out two challenge lines over a count, which set the one-line count mode; the facts line measured 113 px in Unity against 107 in a browser; a batch Editor run stalled silently after its asset refresh and was killed and rerun.

Merged as PR #96 (main `12dbdcb`, Oct 1 at 20:00 local, which is 04:00 UTC on Oct 2; validated head `45b047b`, merged head `be3e706` after taking main's PR #95 (the player README and `.gitignore`), with the art commit `15a0354` ahead of the code). Between the validated head and main only docs differ (PR #95's README rewrite, `.gitignore`, and the build log's own lines): the code, the art and the Editor checks on main are the validated head's. The PR's GitHub Actions Web run on `be3e706` (`36956067053`) passed; the main run (`36962615499`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 407/407 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against `https://davonlemar30.github.io/Ascendant/`, using `12dbdcb`'s own suite file, and the Wing Dial's eye read opening on the live site (76% and 79% of its glass blue when open); captures in `Documentation/ConceptArt/DialRedesign-2026-10-01/production-recheck/` (main checkout, untracked). Build AB's PR #94 was marked merged by containment at the same moment; see its section below.

**Follow-up (Oct 1; PR #98): the seat boxes, two counts, the `seat` line.** Whitney's docs run for Build AC (PR #97) found three leftovers in code. The brief had listed the template's seat boxes "if the tap areas move"; the tap areas had moved, and the boxes had not.

- **The semantic seat boxes** (`Assets/WebGLTemplates/CelestialDial/index.html`). The screen-reader and keyboard focus boxes over the twelve seats kept Build AB's radius 127 and 56 × 56, so each sat 4 px outward of its seat. They now sit on the Cast Dial's radius 123 at the seat's own size, 58 along the ring by 66 across it (`DialView.AstroSeatRadius`, `BuildAstrolabeSeat`). The template draws every box upright, so each lays its 66 px side along the axis its seat's radius runs nearer: across for the 9 and 3 o'clock seats and the two beside each, up and down for the other six. The placement rule is unchanged: seat i at 180° + 30° × (i − the framed seat), the framed seat at 9 o'clock.
- **The check that missed it** (`Tools/validate-greybox-web.cjs`). The suite only counted twelve boxes of at least 48 × 48. The Dial's web state now reports `seatRadius` and `seatSize`, read from the seats, and the suite reads every box back onto the 360 × 800 layout and holds it to them within half a pixel, with Taurus framed and again with Virgo framed after the drag, at both widths. Against a copy of this build with Build AB's line put back, the suite stops at the new check and names all twelve boxes (Taurus at −127, 270, 56 × 56 for −123, 270, 66 × 58).
- **Two stale counts.** The style page's check said "138 art" while asserting 141 (since Build Z), and so did its twin in the mechanical checks; both now print the number they assert.
- **The `seat` slot** (`Slots.cs`, mirrored in ART-SLOTS.md): the greybox fallback, drawn only when the Dial has no turning ring; it no longer names the Astrolabe.

Not changed: the Dial's art, `DialView`'s constants, anything drawn on the canvas.

Validation (local, worktree `greybox`, branch `codex/dial-cast-followups` from main `a799d2e`): mechanical 584/584 (0 compiler warnings); slice fixture 239/239, no runtime errors; headless WebGL 0 errors / 0 warnings (27.7 MB); browser suite 411/411 at desktop density (390 and 360) and 411/411 at phone density (`DEVICE_SCALE=2 MOBILE=1`). The first fixture run stopped at "back on the Dial": a real mouse click (the owner's, taking a screenshot) reached the batch Editor through the Input System's UI module and pressed Leave the Dial; the fixture itself acts only through the web actions, and the rerun was clean. Captures at 3×, before (production, Build AC) and after, the twelve boxes outlined over the Dial and the keyboard focus ring on the framed seat: [`Evidence/seat-boxes-2026-10-01/`](Evidence/seat-boxes-2026-10-01/). Code commit `8920b18`.

Merged as PR #98 (main `3f33ef6`, Oct 1 at 21:51 local, which is 05:51 UTC on Oct 2; merged head `abb23ee`: the code commit `8920b18` validated above, then the records commit `abb23ee`, which changes only the docs and the captures, on main `a799d2e`, so the code on main is the validated code). The PR's GitHub Actions Web run on `abb23ee` (`36970055365`) passed; the main run (`36970814489`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 411/411 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against `https://davonlemar30.github.io/Ascendant/`, using `3f33ef6`'s own suite file; the four new seat-box checks passed on the live site.

## The Astrolabe: the Dial redesigned around its words (Build AB, superseded by Build AC)

**Superseded by Build AC (above) and never deployed.** The owner held this build unmerged as the backup after its third pass (Oct 1: "One more art design pass. I do like this but let's keep it as back up."), then approved Build AC's Cast Dial on the real captures. Build AC's branch grew from this build's head (`e2310af`), so GitHub marked PR #94 merged by containment when PR #96 merged: the Astrolabe's code (the turning ring, `ArcText`, the seat sweep checks, the Wing Dial's eye fix) is the base Build AC stands on, and its art was replaced in the tree by the Cast Dial's. Its look (the medallion with its plaques, the tablets, the count plate) never reached production; the Pages deploy history has no deploy of it. Its art commits (`8307b2f`, `4f9c5a3`, `e2310af`) stay in the history of `main`, and its concept art, eye fix and tools are in `Documentation/ConceptArt/DialRedesign-2026-09-30/` (main checkout, untracked). Task `86bcawkef` is closed as superseded. Everything below records what the Astrolabe was when it was held, not what is in production.

The owner's rulings (Sept 30, Decisions Log week of Sept 28–Oct 4; task 86bcawkef): a complete redesign of the wheel around its words, A, the Astrolabe, polished from the first concept; one fact per seat. Superseded on the way: the cartouche sockets (the oval art pass is on file, unused), because bigger sockets could not hold a name plus three facts.

- **Art** (art commit `8307b2f`, ahead of the code): the polished final concept (ChatGPT, from the approved concept A) cut into layers. `dial-room` and `dial-room-light` keep what stays put (room, rim with degree ticks, the brass pointer at 9 o'clock, the phoenix and the eye); the new `dial-ring` and `dial-ring-light` (720 × 720, centred on the wheel) carry the twelve-segment name band and the twelve brass tablets and turn with the seats. The generator drew the bands narrower than asked, so the ring is remapped radially (tablet faces 42 → 68 px deep, name band 26 → 34, at 2×; plain areas only), and its name band turned 2.8° into line with its dividers. The generated glow flooded the tablets; it was toned so the bright edges stay. `bracket` is a gold wedge over the 9 o'clock segment. Manifest 139 → 141.
- **The Dial** (`DialView`): the ring layer turns by −30° per turn, live while dragging. Each seat is a 56 × 62 tap area over its segment, turned to face the rim; its name curves along the name band in EB Garamond Bold (gold, engraved), bent by the new `ArcText` mesh effect; its tablet holds the symbol (18 px, once taught) and one fact (11 px, title case), the newest the seat has learned: element once lit, modality once its modality is lit, polarity once shown. On the lower half the seat turns upright. The symbols unit still hides unrevealed names; the rejected seat keeps its ×. Above the eye, under the framed sign's name (now in the Dial's serif), its learned facts, the element in its Build S colour. The family lines run just inside the tablets. Seats sit on radius 127 (was 136); the web template's seat boxes follow.
- **Found on the way:** the old seat code re-placed the name label on every refresh, which put the upper half's names inside the tablets (caught by eye on the first real render, then pinned by a fixture check of every word's radius); the first lettering pass (9 px capitals) read small and pale, so the facts moved to title case at 11 px, which is narrower.
- **Second pass (owner, Oct 1, watching the run: the words not seated, the eye tilted, the wheel "almost like a carnival game", the eye no longer blinking open; and redraw the Wing room's Dial):**
  - *Every word in a drawn place.* A new medallion (concept B, from the approved level phoenix and eye): a plaque above the eye carries the framed sign's name and its learned facts, the eye's glass the challenge, a small plaque below it the worked count. Each is measured on the art (`DialView.NamePlate`, `CountPlate`, `EyeBox` 128 × 36 at y 270, the glass's own height), and the eye's lines best-fit 13–16 px (every eye line fits, 14–16 px). The family triangles are hidden on the Astrolabe: chords between seats 120° apart always cross the medallion's words; a completed family's tablets turn gold instead (working choice).
  - *The eye level again.* The polished concept had redrawn the phoenix and eye slightly askew; the medallion keeps the approved Build Z eye.
  - *Calmer.* The ring's brass darker and less saturated, its glow at 40%, the room's at 50%.
  - *The eye blinks open again.* `SliceView.Show` shut the Wing Dial's eye on every refresh in the room, which closed it mid-opening when the Keeper already stood at the Dial; it now shuts only on coming back into the room, and the open eye holds 0.25 s before the Dial screen. The old check read the state (which said open); the suite now reads the glass off the screen (0% shut, 80% open).
  - *The Wing room's Dial* is the Astrolabe: the four `kit-dial-*` files remapped (ring and rim from the Dial screen, the eye and phoenix kept, the worn state dulled), each open file the same transform as its closed one.
  - *Found by eye on the real captures, then pinned by checks:* the name was not drawn at all (its box was shorter than Garamond's line, and Unity drops a line that does not fit; now every centre label overflows nothing and a check measures heights); the family chords crossed the plaque and the eye.
- **Third pass (owner, Oct 1, art commit `e2310af`): the medallion carried a double eye** (a second brass lid, a second blue band and a second gold point under the glass). The approved Build Z eye replaces that zone: the glass, its lids and tips, and the strip under the lower lid down to the lower plaque; the two plaques stay as drawn, the lower plaque's top bevel is continued where the old point's tip crossed it, and the glow is rebuilt inside the zone. Re-measured on the fixed art: the glass's inner blue runs y 253 to 287.5 with its widest row 168.5 px at y 269.5 (centre x 178.75), so `DialView.EyeBox` (128 × 36 at `EyeY` 270) stands unchanged. Tools and before/after crops: `Documentation/ConceptArt/DialRedesign-2026-09-30/final/production/eye-fix/` (main checkout, untracked).
- **Not changed:** the lessons, the challenges, Caspar's lines, the save.

Validation (Unity 6000.3.24f1, local, branch `codex/dial-astrolabe`, after the second pass): mechanical 581/581 (0 compiler warnings); slice Play Mode fixture 239/239, including a sweep of all 24 half-turns (every name on the name band, every fact on its tablet, each centred on its own segment of the turned ring within 1° and 1.5 px, and upright), the centre's words inside their plaques and the glass, nothing truncated, no family line over the medallion, and, including, at the wheel lit, the modality unit's opening (the ring turned to Taurus), the modalities complete and polarity shown, that every name fits its band (within 69 px) and every fact its tablet (within 47), that each word sits on its band's radius within 1.5 px, that each seat shows the newest fact, and that the ring has turned with the seats; headless WebGL build 0 errors / 0 warnings (27.9 MB); browser suite 407/407 at desktop density (390, 360), with the eye read off the screen. Browser suite (first pass) 405/405 at phone density (`DEVICE_SCALE=2 MOBILE=1`). After the third pass (head `e2310af`): mechanical 581/581 (0 compiler warnings), slice fixture 239/239, headless WebGL build 0 errors / 0 warnings (27.8 MB), browser suite 407/407 at desktop density (390 and 360) and 407/407 at phone density, the Wing Dial's glass read 0% blue shut and 78% and 80% open. The PR's GitHub Actions Web runs passed on its three heads (`fa737c3`, `15df04e`, `e2310af`). No production recheck was run: the build was never deployed.

## The journal redesign: the Wheel index, the linked sign page, the Black Hours (Build AA)

The owner's rulings (Sept 30, Decisions Log week of Sept 28–Oct 4; the Sept 26 playtest's notes 7 and 14; task 86bcatxnm): the journal's contents page and its five lists give way to one index page, the Wheel, with a Wheel / Table switch and tabs that appear as each pattern is learned; a seat's preview opens its page, and the page links the signs that share each fact; the look is the Black Hours, silver-ruled, painterly by the owner's journal-only exception to the Art Bible.

- **Art** (`journal-page`, `journal-cover`, `journal-ribbon` redrawn; new `journal-wheel`, `journal-seat-leaf`, `journal-seat-line`; `journal-contents` and `journal-plate` retired; manifest 138 → 139 slots). Committed on its own at the owner-approved files (`6b09d0c`), before the code. The page came 200 px too tall from the generator: one spine period was cut from its plain middle and the desk faded below so the book clears the buttons; the wheel's sockets land within a few pixels of the guide (radius 216 of 600) and the game places the seats on the guide's positions.
- **The index** (`SliceView.BuildJournal`, `ShowJournal`): the silver rules (26 px apart from y 96) and the faded vermilion margin are drawn by the game over the page art. The Wheel at y 296, 270 px; the seats on radius 97.2 in Dial order, each a 46 px tap target with the sign's picture clipped round, its state ring (met: the plain ring in silver; practising: in gold; mastered: the gold-leaf ring), its symbol on a badge once learned (a small element dot before), and a ribbon tab when due. The Table moves the same seats into the element rows and modality columns. The tabs recolour either view: Element draws the four triangles and colours the badges in Build S's element colours; Modality the three crosses (Cardinal gold, Fixed silver, Mutable faded vermilion, a working choice); Polarity dims the Yin seats; Opposites draws the six pairs and lights the framed one. A tab shows only once its pattern has entered the deck, and no tab row shows with one tab.
- **The preview and the page**: a tap frames a seat and shows its card (picture, name, symbol, the facts in one line, or on the Opposites tab what the pair shares); Open the page, or a second tap on the framed seat, opens the page. The page: the title with its gold capital and the symbol beside it, the picture in its state ring, the ribbon as before (its length the ladder, pulled out when due), and a panel per fact learned. Element and Modality list the other signs that share them as chips; Polarity carries Caspar's words for it (worksheet section 12); Opposite is a chip with what the pair shares. A chip opens that sign's page. Back to the Wheel replaces Contents.
- **Illumination under a round clip**: the first pass clipped the pictures with a UI `Mask`. The fixture's saturation check failed (0.58 at 20%, 0.55 at full): a Mask renders a copy of the material made once, so the deck's live `_Saturation` and `_Ink` never reached the screen. The `Ascendant/Illumination` shader now clips round itself (`_Round`), and a mechanical check forbids a Mask in the journal.
- **Borders**: the see-through panels first rendered tan in the Web build: `Outline` draws offset copies of the whole quad, which show through a translucent fill. Borders are now four drawn edges (`SliceView.Frame`).
- **Web state and the semantic layer**: `journalView` (wheel / table / sign), `journalLenses`, `journalLens`, `journalSeats` (met / practising / mastered, never drawn), `journalDue`, `journalSelected`, `journalPreview`, `journalChips` and `journalChipBoxes`, `journalSeatState`, `canJournalTable`, `canJournalOpen`, `canJournalWheel`. The template carries a button per seat (on the wheel or in its cell), the switch, the tabs, Open the page, the chips (placed from the game's boxes) and Back to the Wheel; a seat's name carries its state for a screen reader.

- **Import settings**: the three new files were imported before their manifest entries, so they took the default 512 px cap (the 600 px wheel downscaled); `SlotImport`'s version goes to 5 so every slot file re-imports with its manifest cap (wheel 1024, rings 256). Two test-set keeper metas that the re-import also touched are left out (unrelated drift).

Validation (Unity 6000.3.24f1, local, worktree `greybox`, branch `codex/journal-black-hours`): mechanical 579/579 (0 compiler warnings), again after the import bump; slice Play Mode fixture 219/219 (captures `slice-390-journal-*`, `slice-390-art-journal-*`); headless WebGL build 0 errors / 0 warnings (27.8 MB); browser suite 405/405 at desktop density (390 and 360) and 405/405 at phone density (`DEVICE_SCALE=2 MOBILE=1`). Found and fixed on the way: the Mask freezing Illumination, the Outline tint, a capture step that also acted, and a ribbon check made on a page where the ribbon is hidden.

Merged as PR #92 (main `444064b`, validated head `301a883`, with the art commit `6b09d0c` ahead of the code, Sept 30). The PR's GitHub Actions Web run passed; the main run (`36822741600`: Build Web player and Deploy to GitHub Pages) passed. Production recheck after the Pages deploy (passed): browser suite 405/405 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against `https://davonlemar30.github.io/Ascendant/`, using `444064b`'s own suite file.

## The Dial redesign: Room A, the eye, the pattern reveal (Build Z)

The owner's rulings (Sept 29–30, Decisions Log week of Sept 28–Oct 4; task 86bca6fmv): Room A behind the Dial, its glow kept, the Dial's voice is the eye, a reveal each time a pattern opens, the Dial-screen candle cut, the shelf, chair and floor markings retired from the Dial screen, the Wing room's Dial redrawn with the eye (closed at rest, opening when tapped, worn and restored), and EB Garamond for the Dial's words. The art pass was approved by the owner before the build.

- **Art** (`dial-room`, `dial-room-light`, `seat`, `dial-face`, `kit-dial-worn` / `-restored`, and the new `kit-dial-worn-open` / `-restored-open`; manifest 134 → 138 slots). The room is fitted so the sockets' cups sit under the seats: measured from the dormant art (a dark cup inside a bright rim, robust circle fit), centre (360.4, 540.3) and radius 272.8 on the 720 × 1600 file, against the seats' (360, 540) and 272. The glow layer is the exact difference between the lit and dormant plates, masked to the Dial, at 65% strength (the owner found the first cut bright). The open Wing pieces are identical to the closed ones outside the eye (measured change 0).
- **The Dial screen** (`DialView`): the room and its glow sit behind everything; the glow is off while the Dial sleeps. The challenge line moves into the eye (EB Garamond Bold, 150 × 54 at y 270, best fit 15–20 px; the symbols unit's "Find the symbol of / Sign" too); the sign-name label moves above the eye (y 226, 15 px bold) and stays empty in the symbols unit (a name there would give the answer away). Title, hint, count and phase labels get a shadow; dark fades under the title and under the wheel. The family lines are a faint gold over the room so the eye's words read through them.
- **The reveal**: once per pattern (elements at the first approach's wake, symbols, modalities, polarity and opposites, the builder), saved in `revealsPlayed`. The glow sweeps round clockwise from the top (a radial fill), each seat pops as the sweep passes, the names fade up, the line rises in the eye; 1.2 + 0.35 + 0.4 s. A tap or `skip-reveal` skips it; Reduced motion shows the end at once.
- **The slim box**: when the Dial speaks the plate, rule and frame turn sea blue (#63A6E0) with an eye mark; Caspar keeps gold.
- **The Wing room**: the Dial's eye opens (a mask growing from the seam, 0.35 s) when the player taps it, before the Keeper walks; it rests closed again back in the room.
- **Removed** from the Dial screen: the candle (not built), the floor markings, shelf and chair (inactive with the room art). The `candle` slot still draws the opening's and the Chamber's.
- Web state: `dialRoom`, `dialLit`, `dialEye`, `revealing`, `revealsPlayed`, `eyeText`, `eyeSize` (layout px), `signLabel`, `dialVoice`.

New checks: mechanical, the slots and files, the open pieces' canvases, the font and its licence, every line the eye can show fitting at 15 px or more on two lines (the tightest is 18 px), the save field, the motion budget; fixture, the room in place and the retired pieces gone; suite, the Wing Dial's eye closed, opening and closed again, the dormant arrival, the elements' reveal mid-sweep and lit, the challenge in the eye with the sign above, the symbols' reveal and empty label, each of the five reveals played once, and the box's colour matching its speaker, at each viewport.

Validation: mechanical 571/571, slice fixture 208/208, WebGL clean (27.7 MB), browser suite 395/395 at desktop density and 395/395 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports, on the branch merged with main (Build Y). Captures: [`Evidence/dial-redesign-2026-09-30/`](Evidence/dial-redesign-2026-09-30/).

Merged as PR #89 (main `4e2c4f4`, reviewed head `9a26634`, Sept 30). Production recheck after the Pages deploy (passed): browser suite 395/395 at phone density (`DEVICE_SCALE=2 MOBILE=1`) against `https://davonlemar30.github.io/Ascendant/`.

## The bookshelf glows itself (Build Y)

The Sept 26 playtest's note 11 ("the glow should imbue the bookshelf itself") and the APK playtest ("a portal ring instead of a glow"); task 86bc8ddxp. Since the Wing became art-dressed, the shelf's glow was `SoftRing()`, a 90 × 320 ring behind the shelf.

- With the Wing kit's shelf file (`kit-shelf-restored`), "Shelf light" is a child of that piece: the same sprite, tinted warm gold at 22%, with two `Outline` edges (1.5 and 3.5, gold at 50%) that follow the file's silhouette, under a `CanvasGroup` for its level. The ring is switched off; without the kit file the greybox keeps its soft disc.
- Level: 1 while the Book waits (the wheel lit, the symbols unread), breathing .6–1 like the Dial's glow; still under reduced motion; .3 after the Book is read; 0 before the wheel is lit. Web state `shelfLight` (-1 without the kit file) and `shelfRing`.

New check: suite, when the lit wheel wakes the shelf, the shelf's own light is at full and no ring shows, at each viewport.

Validation: mechanical 502/502, slice fixture 207/207, WebGL clean (26.4 MB), browser suite 375/375 at desktop density and 375/375 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/shelf-glow-2026-09-30/`](Evidence/shelf-glow-2026-09-30/).

## The gold chat box fits its line (Build X)

The owner (Sept 29): dead space inside the chat box when Caspar speaks. The first pass fitted only the box's outer height; the owner's screenshot showed the gap meant was *inside*, between the plate and the first line. The padding was tightened and approved from a phone-size capture before this record.

- `ChatFit` (on the story pages' box, the hub's, and the Chamber's): the top stays at the old top (story 480, hub 476, Chamber 435); height = `Head` 22 + the line's height + `NextRow` 50 when a Continue sits inside + `Foot` 30, clamped from `Min` 76 (the sliced frame's corners whole) to the old height (240, 120, 170), past which the text's best fit shrinks it as before. The line is placed just under the plate. The story pages' Continue rides at the box's bottom. Caspar's figure (story pages, Chamber) sits in a `RectMask2D` clip from the screen's top to the box's bottom edge. `Fitted` republishes the web state.
- Web state `chatBoxHeight` (0 when no gold box shows).

New checks: mechanical, the height rule; suite, the opening's first page and the Atrium hub come out shorter than their old heights, and a canvas tap on Continue at the fitted box's bottom turns the page, at each viewport.

Validation: mechanical 502/502, slice fixture 207/207, WebGL clean (26.4 MB), browser suite 373/373 at desktop density and 373/373 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/chat-box-fit-2026-09-29/`](Evidence/chat-box-fit-2026-09-29/).

PR #86 merged as `3bf74c8` (GitHub Actions Web run: build and Pages deploy both passed). Production suite against `?v=3bf74c8`: 373/373 at desktop density and 373/373 at phone density (`DEVICE_SCALE=2 MOBILE=1`).

## DEV Mode: Jump to a checkpoint (Build W)

The Sept 26 playtest's note 5 ("testing means doing every interactive all the way through, every time"). Brief: task 86bca0163, approved Sept 29 with the six checkpoints and Keys earned, not spent (Decisions Log week page 2kyd583p-25254).

- `DevCheckpoints.Play(id, name, sun)` runs one continuous, scripted Wing in memory through `SliceFlow`, `DialLesson` and `GridModel`, with the production progress subscriptions (`ObserveProgress`). It plays the opening (Build T's walk, Key 1 into the Chamber), the element continuation, the Book (Parts A and B), the modalities and the Table, the opposites and the builder, and, for the last checkpoint, spends the three Keys in hand. Every answer is at Level 0. It stops at the checkpoint and returns `CaptureProgress`, and it throws rather than write a partial save if a step fails.
- `SliceView.JumpTo` writes that save and reloads, as Start over does. `SettingsMenu` gains a Testing row, Jump to..., which swaps the box for the checkpoint list and Back. The Settings box is 48 taller on every screen.
- Web state `jumpsShown`; web action `jump:<id>`; semantic buttons `#jump-<id>`.

New checks: mechanical, each checkpoint's save holds its Keys, locks, stage, units and proven items (after Key 1, two element items proven, as a played run leaves them), and play continues from After Key 2 and After Key 4; fixture, Jump to After Key 3 reloads into the Atrium with Keys 2 and 3 in hand and the last pattern waiting; suite, a canvas tap through Settings → Jump to... → After Key 4, then every checkpoint through its web button with its Keys and stage checked, the Dial offering the next lesson and practice from After Key 2, and no runtime exceptions, at each viewport. Build U's canvas taps moved with the taller box.

Validation: mechanical 501/501, slice fixture 207/207, WebGL clean (26.4 MB), browser suite 367/367 at desktop density and 367/367 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. A first run failed on the suite's `jumpsShown` check: the assignment had been appended after a trailing `// Build U` comment. Fixed. Captures: [`Evidence/dev-mode-2026-09-29/`](Evidence/dev-mode-2026-09-29/).

PR #85 merged as `75aa74f` (GitHub Actions Web run: build and Pages deploy both passed). Production suite against `?v=75aa74f`: 367/367 at desktop density and 367/367 at phone density (`DEVICE_SCALE=2 MOBILE=1`).

## The Dial speaks its own challenges (Build V)

From the owner's APK playtest (Sept 29): "Build me a sign from its parts" and similar challenge prompts are the Dial's, plate THE CELESTIAL DIAL; the Dial's own look waits for concept art (Decisions Log week page 2kyd583p-25254, item 7).

- `DialLesson.Speaker` is `CasparSpeaker` for every line set through `Message` and `DialSpeaker` for a line set through `DialSays`: the builder's asks (three), the builder's share step, and the opposites' own-pair prompt. Build I already put the per-problem targets on the wheel's face (`DialLesson.Challenge`); those are unchanged.
- `FitBox.Name` is the plate; `FitBox.SetSpeaker` writes C A S P A R or T H E  C E L E S T I A L  D I A L (spaced; the plate is now as wide as the box). `DialView` sets it whenever the line shown is the lesson's own (`FitBox.Whole(message) == Lesson.Message`). Web state `speaker`.

- **The opening beat is cut (owner, Sept 29 evening).** The first lesson's "This is the Zodiac Wing. The wheel at its center has been still..." (Sept 11 lock) is gone: since Build T Caspar has just spoken in the room, so the Dial arrives dormant with Caspar silent and Continue wakes it (Decisions Log week page 2kyd583p-25254). The Dial also says "acolyte", and the wheel face's target label counts as the Dial speaking (both the owner's, no code change).

New checks: mechanical, the Dial asks for the sign built from its parts and Caspar answers a wrong name; suite, the builder's first ask is the Dial's at each viewport.

Validation: mechanical 487/487, slice fixture 205/205, WebGL clean (26.4 MB), browser suite 349/349 at desktop density and 349/349 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/dial-speaker-2026-09-29/`](Evidence/dial-speaker-2026-09-29/).

Production suite after the deploy of `207952b` (PR #83; GitHub Actions Web run 36673078488, build and Pages deploy both passed): 349/349 at desktop density and 349/349 at phone density (`DEVICE_SCALE=2 MOBILE=1`), run against `https://davonlemar30.github.io/Ascendant/?v=207952b`.

## Settings (Build U)

From the owner's APK playtest (Sept 29): Quit and Reduced motion live in a Settings menu, the gear at the top right of every screen (Decisions Log week page 2kyd583p-25254); the Sept 26 playtest's note 8 moved the Atrium's test buttons into it (task 86bc8ddzd).

- `SettingsMenu` (its own overlay canvas, sort order 5: over the slice, under the white light) draws the gear in code (the web font has no gear glyph) at (158, 22), 36 × 36, and the menu: a dim veil (a tap closes), the instruments' slim-box sprite (`Slots.InstrumentBoxSprite`, now public), S E T T I N G S, the rows Sound, Reduced motion, Quit the game (`SettingsMenu.CanQuit`, false on WebGL), a T E S T I N G section with Walk and Start over, and Close. Working choices (Claude): the slim-box look, the Testing section in the same menu, no Quit on the web.
- The Atrium's test buttons and the Dial's Reduced motion button are hidden; their web actions stay. Leave the Dial takes the Dial's bottom row (0, 768, 216 × 44). At Stage 1 the Zodiac Wing button is centred (the Chamber button is hidden until Key 1).
- Web state `settingsOpen`, `canQuit`; web action and semantic button `settings`.

New checks: fixture, the gear opens and closes Settings over the Atrium; suite, a canvas tap on the gear opens Settings with no Quit on the web, the Sound and Reduced motion rows work on the canvas, and Close shuts it with both settings as they were, at each viewport.

Validation: mechanical 485/485, slice fixture 205/205, WebGL clean (26.4 MB), browser suite 347/347 at desktop density and 347/347 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/settings-2026-09-29/`](Evidence/settings-2026-09-29/).

Production suite after the deploy of `ff268c8` (PR #82; GitHub Actions Web run 36671138446, build and Pages deploy both passed): 347/347 at desktop density and 347/347 at phone density (`DEVICE_SCALE=2 MOBILE=1`), run against `https://davonlemar30.github.io/Ascendant/?v=ff268c8`.

## Caspar leads to the Zodiac Wing first (Build T)

From the owner's APK playtest (Sept 29, page 2kyd583p-25274), rulings on the Decisions Log week page 2kyd583p-25254 (items 1 and 6 and the follow-up answers).

- **The opening's walk.** `SliceFlow.Continue` takes the Atrium's last page to the Hub, not the Dial; Stage 2 (and `Deck.IntroduceAll`) still begins only on the return from the Chamber (`KeyInserted`). At Stage 1 the hub says `SliceView.HubOpeningLine`, the Wing room `WingOpeningLine` (both the owner's); `EnterWing`, then the Dial point of interest, opens the lesson. The Chamber door stays locked in the Atrium kit until Stage 2 (`DoorUnlocked`), its button is hidden at Stage 1, and a tap on the doorway gives the sealed-door line. `LeaveWing` works at Stage 1 too. The first lesson keeps its optional extra round (`SliceHidesOptional` only from Stage 2).
- **Leave the Dial at all times.** Its own button (`leaveDial`) on the bottom row (768), left half, Reduced motion the right half (Claude's working choice, until Settings holds Reduced motion). It shows on the Dial whenever the Dial may be left: from Stage 2 always, at Stage 1 until Key 1 shows (then Continue carries the opening on). It leaves mid-challenge; the problem waits on return. `canLeaveDial` follows the same rule; the old Continue at 654 is the opening's Continue only.

Checks swept: every mechanical test flow now walks to the Dial (`EnterWing`, `EnterDial`) before Key 1; the suite's three opening paths (main, recovery, art set) walk through the Atrium and the Wing; the page-cue check moved to the Atrium (the walk plays the door cue); the Stage 2 door check waits for the Chamber door's unlock fade (it was seen locked at Stage 1); the old "Back button stays off" assertions now expect the exit on its own row.

New checks: mechanical, the opening lands in the Atrium at Stage 1 with the Chamber shut, and the Dial and the Wing can be left and entered again at Stage 1; fixture, the Atrium's and the Wing's opening lines; suite, the same lines, the sealed Chamber door on a tap, a canvas tap on Leave the Dial mid-challenge in the first lesson, and the same problem waiting on return, at each viewport.

Validation: mechanical 485/485, slice fixture 203/203, WebGL clean (26.4 MB), browser suite 339/339 at desktop density and 339/339 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/wing-first-2026-09-29/`](Evidence/wing-first-2026-09-29/).

PR #81 merged as `5880ac7` (GitHub Actions Web run 36670527692, build and Pages deploy both passed). The production suite was run once for Builds T and U together against the next deploy, `ff268c8` (see Build U, 347/347 at both densities).

## Caspar's lines in pages (Build S)

From the owner's APK playtest (Sept 29, page 2kyd583p-25274), rulings on the Decisions Log week page 2kyd583p-25254.

- **Pages.** `FitBox` (the slim box on the Dial, the Book of Symbols, the Elemental Table) keeps the whole line the code writes as `Source` and cuts it at its sentences (". ? !" and Caspar's "..." before a space) into pages of at most `FitBox.PageLines` (3) lines at the box's width. A page with more to come keeps a 22 row (`Slots.InstrumentMore`) for a small gold Continue at the box's bottom right, which turns the page; the wheel stays live meanwhile. The same line written again keeps its page; a new line starts at page 1. Web state: `message` stays the whole line; `casparPage`, `casparPages` and `casparShown` report the page drawn; web action `caspar-page`; semantic button `#caspar-page`.
- **Element colours.** `FitBox.Colour` tags whole-word Fire #E0643C, Earth #8DB36A, Air #E8D38F and Water #63A6E0 on the page drawn (Claude's working colours).
- **Copy.** The Wing's Part B line is the owner's rewrite; the Dial's step hint reads "Find the sign, seal it."

New checks: mechanical, the paged box height and the colour tagging (whole words only); suite, the practice intro shows page 1 of 3 with the whole line in the state, a canvas tap on the box's Continue turns the page with the wheel live, and the pages name Fire and Water in their colours, at each viewport.

Validation: mechanical 484/484, slice fixture 201/201, WebGL clean (26.4 MB), browser suite 323/323 at desktop density and 323/323 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. The first phone run failed once on the unrelated "mid-placement reload resumes at Virgo" check (a reload 300 ms after a seal); the rerun passed. Captures: [`Evidence/caspar-pages-2026-09-29/`](Evidence/caspar-pages-2026-09-29/).

Production suite after the deploy of `9f03eee` (PR #79; GitHub Actions Web run 36641373027, build and Pages deploy both passed): 323/323 at desktop density and 323/323 at phone density (`DEVICE_SCALE=2 MOBILE=1`), run against `https://davonlemar30.github.io/Ascendant/?v=9f03eee`.

## The chat box everywhere Caspar speaks (Build R)

The owner ruled (Sept 28) that where the player taps the room or the Dial only a box shows, no figure, and handed Claude the remaining poses. On the Dial's first mock the owner rejected the ornate chat box there ("looks ugly") and picked a slim one, fitted to each line, for the three instrument screens (Sept 29).

- **The rooms** (the Atrium hub, the Chamber) and the story screens: `Slots.DressChatBox` dresses the panel as the gold chat box (the `chat-box` frame and fill sliced to the panel, import borders 56, 64, 56, 56 at the file's 2x, `pixelsPerUnitMultiplier` 2; the old CASPAR label hidden; the `chat-plate` on the top-left edge with the name written on it), each at its old rect so no button moves; the Atrium opening and return use it too (Build P's own plate code goes). `SlotImport` goes to version 4.
- **The instrument screens** (the Dial, the Book of Symbols, the Elemental Table): `Slots.DressInstrumentBox` draws a flat dark panel with a hairline gold border (a sliced sprite made in code, no slot) and C A S P A R small in gold over a short rule, pinned at its top; `FitBox` sizes it to the line whenever the line changes (`Slots.InstrumentBoxHeight`: 32 above, 12 below, at least 60, at most the old panel's height, past which the text's best fit shrinks it). The Dial publishes `dialBoxHeight`.
- **Caspar's figure** stays on story beats: the Chamber's introduction joins the opening and the return (`SliceView.ChamberPoses` explain, solemn, calm; the figure's waist at the Chamber panel's top, 435). On the first Key he steps away while the Chamber wakes and returns for his two lines (`ChamberBreathesPose` moved, `ChamberContinuePose` warm); after the ending, and in the Chamber as a room, the box stands alone. `casparPose` reports the Chamber's pose.

New checks: mechanical, the Chamber's poses and the instrument box's height rule; suite, the Chamber opens on `explain`, Caspar steps in as `moved` for "She breathes", no figure after the ending, and the Dial's box fits the fork's short line (under 128), at each viewport.

Validation: mechanical 482/482, slice fixture 201/201, WebGL clean (26.4 MB), browser suite 317/317 at desktop density and 317/317 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/chat-box-everywhere-2026-09-28/`](Evidence/chat-box-everywhere-2026-09-28/) (the Chamber's introduction with Caspar, the hub's chat box, and the slim fitted box on the Dial, the Book, and the Table).

Production suite after the deploy of `bca47d5` (PR #76; GitHub Actions Web run 36598071162, build and Pages deploy both passed): 317/317 at desktop density and 317/317 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports, run against `https://davonlemar30.github.io/Ascendant/?v=bca47d5`. Run from a worktree without `node_modules`, the suite needs `PLAYWRIGHT_MODULE` pointing at a Playwright install.

## The domed Grand Atrium (Build Q)

The owner approved the new Grand Atrium (Sept 28, the B2 world) and its layout check, and asked for the art pass now. The Atrium kit moves onto it: `atrium` is the new restored shell (glass dome on the night sky, dark wood Gothic shelves and balcony, the statue's lit niche, the empty doorways) and `atrium-grime` is the same room asleep (cold moonlight, grimy cracked dome, dust, cobwebs), opaque, so the kit's grime fade is a clean cross-fade; the two shells line up at offset (0, 0) by edge correlation. The new scene was an edit of the old restored scene and sat 22 px low (at 1×); both files are moved 22 px up so the doors fill the game's door rects. New art for `banner`, `pennant` (new slot, both states), `lamp`, and `chandelier` (now the armillary ring), cut from full-canvas isolations of the restored and dormant scenes, so their placements were measured, not guessed; the worn lanterns, banners, and ring are scaled (`wornScale`) to hang as far as the restored ones. The chart and shelf placements are retired (files kept). 20 pieces (was 26); the lanterns take Key 3 from the shelf; the armillary ring is last (Key 21). Two slots (134).

Checks swept: the slot count to 134 (mechanical, fixture, suite), the suite's Atrium kit to 20 pieces and 9 restored with the Wing whole (was 26 and 14), and the fixture's "the Atrium takes its files" list (the pennant replaces the retired shelf).

Validation: mechanical 480/480, slice fixture 201/201, WebGL clean (26.4 MB), browser suite 311/311 at desktop density and 311/311 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/domed-atrium-2026-09-28/`](Evidence/domed-atrium-2026-09-28/) (the opening asleep, Stage 2, the Wing whole).

## Caspar behind the chat box (Build P)

The owner's Sept 26 playtest (note 3) asked for the speaker's art above the text box at the start of the game. After the side-by-side tests the owner ruled for B2 (Sept 28): Caspar unframed, from the waist up, standing behind a gold-framed see-through chat box, one still pose per page. On the Grand Atrium opening (four pages) and the return with the first Keeper Key (two pages) the box replaces the old panel: `chat-box` at 324 × 240 (centre 600), the `chat-plate` on its top-left edge with CASPAR written by the game, the line left-aligned and best-fit (14 to 10 pt), Continue (150 × 42) inside the box at 671. Caspar's pose (264 × 468 at (5, 408), behind the box) turns with the page: `SliceView.AtriumPoses` wry, warm, solemn, explain; `SliceView.ReturnPoses` moved, explain. No `chat-box` file keeps the old panel; no pose file shows no figure. The web state carries `casparPose`. Eight new slots (132).

New checks: mechanical, six pose slots at 264 × 468 plus the box and plate, and a pose for every page of the opening and the return; suite, the opening's first page shows `wry`, the next `warm`, the return's first `moved`, at each viewport. The slot count is swept to 132 in the mechanical checks, the fixture, and the suite; `Tools/make-test-set.py` appends the eight test images at the end of its list (its run also rewrote five older hand-edited test files, which were restored).

Validation: mechanical 480/480, slice fixture 201/201, WebGL clean (26.7 MB, +0.9 MB for the poses and box), browser suite 311/311 at desktop density and 311/311 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/chat-box-2026-09-28/`](Evidence/chat-box-2026-09-28/) (the opening at phone density, the return from the fixture).

## The interactives carry their own names

The owner's Sept 26 playtest (note 13): the header on the Dial, the Bookshelf's book and the Table said THE ZODIAC WING, the room's name, and for the longest time read as the game's title. Now each instrument's screen is headed with its own name — THE CELESTIAL DIAL, THE BOOK OF SYMBOLS, THE ELEMENTAL TABLE, the owner's pick from a set of options (Sept 27) — and only the room's screen says THE ZODIAC WING. Three labels; the room's buttons keep their short names (The Dial, The Bookshelf, The Table). The Table's placeholder subtitle ("The Table") is gone, the owner's call, since it only repeated the header.

Validation: mechanical 478/478, slice fixture 200/200, WebGL clean (25.8 MB), browser suite 305/305 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/instrument-names-2026-09-27/`](Evidence/instrument-names-2026-09-27/).

Production suite after the deploy of `2200c29` (which also carries #70 and #71): 307/307 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports, main's own suite. The production capture of the book shows THE BOOK OF SYMBOLS; no suite check quotes the headers, so the branch's 360 captures are the visual record for all three.

## Leaving the Dial keeps the player in the Zodiac Wing

The owner's Sept 26 playtest (note 10): exiting the wheel sent the player all the way back to the Grand Atrium, when the player should leave the Dial and still be standing in the Zodiac Wing. One press of the Dial's exit did both steps: `SliceView.LeaveWing` left the Dial, then walked the Keeper out through the room.

Now the Dial's exit (`LeaveDial`, web action `leave-dial`) lands in the Wing room, and the room's own button ("Return to the Atrium", `leave-wing`) walks back to the Atrium. From Stage 2 the Dial's exit button reads "Leave the Dial" (it read "Return to the Atrium"), after the Table's "Leave the Table"; the label waits on the owner's word. The web state splits the flag: `canLeaveDial` on the Dial, `canLeaveWing` in the room, with a semantic button for each. Caspar's line on the lit wheel still says "let us return to the Atrium"; that is now two taps away, and his line is unchanged.

Checks: the suite asserts that leaving the Dial lands in the room with the way back offered, at both widths, and every walk out from the Dial takes the two presses; the fixture's ten Dial exits use `leave-dial` and its ten room exits `leave-wing`.

Validation: mechanical 478/478, WebGL clean (25.8 MB), browser suite 307/307 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports, slice fixture 201/201. Capture: [`Evidence/leave-the-dial-2026-09-27/`](Evidence/leave-the-dial-2026-09-27/).

Production suite after the deploy of `2200c29` (which also carries #69 and #70): 307/307 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports, main's own suite. Both note 10 checks pass in production: leaving the Dial lands in the Zodiac Wing with the way back offered, at 390 and 360.

## The blue box, gone: no native focus ring on the semantic layer

The owner's Sept 26 playtest (note 2, with two screenshots): a blue rectangle sat above and overlapping the Continue button on the Grand Atrium opening screen, offset from the drawn button. It was iOS Safari's own focus ring, painted around the invisible `#semantic` accessibility button the page auto-focuses after every state change; nothing told Safari not to paint a native ring around a scripted focus. It was never a tap zone — those buttons are `pointer-events:none` and taps go to the canvas.

One CSS rule, `#semantic button:focus{outline:0}`, suppresses it. The deliberate bone-white ring shown to keyboard players (`.kb` plus `:focus-visible`) is untouched — it has higher specificity and still applies.

Probe (Chrome via Playwright, served build): plain scripted focus computes `outline: none 0px`; keyboard mode computes `solid 3px rgb(240, 232, 220)`.

Noted for later, not fixed here: the semantic layer's boxes also sit slightly offset from where Unity draws the buttons on a phone (visible in the owner's screenshot). Invisible now, but if the keyboard ring ever looks misplaced, the `box()` mapping in the template is where to look.

Validation: mechanical 478/478, WebGL build clean (25.8 MB), slice fixture 200/200, browser suite 305/305 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports, against the served build of this branch.

Production suite after the deploy of `9c6eb1f` (which carries #65–#68): 305/305 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports, main's own suite. A focus probe on production (Chrome, 390 wide at 2x, mobile) found the same result as the branch: a scripted focus on a `#semantic` button computes `outline: none 0px`; after a Tab keypress the focused button computes `solid 3px rgb(240, 232, 220)`.

## The door plates, unsquished

The owner's Sept 26 playtest (notes 6 and 12): the names on the Atrium's door plates and on the Wing's plate back to the Atrium were squeezed into their signs. The cause was geometry, not the names: `PlateText` set its box to 80% by 70% of the plate, larger than the plate file's engraved field, with the lines at 0.85 spacing, so bold caps ran into the border; and the Wing-room plate, 64 px wide, carried "THE GRAND ATRIUM" on one line at 6 px.

Now the text box is the engraved field (74% by 52%), the lines a full line apart, best fit up to 12. The Atrium's plates are 18% larger (sealed 88, the Zodiac Wing 100, the Crystal Book Chamber 102 — after the 30% of Sept 25), the Wing-room plate 35% larger with "THE GRAND / ATRIUM" on two balanced lines. The names, caps and weight are unchanged. An offline mock of four settings (today's, this one, regular weight, blackletter in title case) sits in the evidence folder for the owner's eye.

Validation: mechanical 478/478, slice fixture 200/200, WebGL clean (25.8 MB), browser suite 305/305 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/door-plates-2026-09-27/`](Evidence/door-plates-2026-09-27/).

Production suite after the deploy of `9c6eb1f` (which carries #65–#68): 305/305 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports, main's own suite.

## The long restoration: the shared rooms restore across the whole game

The owner's Sept 26 playtest (note 15) found the Grand Atrium and the Crystal Book Chamber fully restored the moment the Zodiac Wing was — too soon for a Library with six more Books to earn. The owner's ruling (Sept 27, Decisions Log): a wing restores on its own challenges; the shared rooms restore across the Library's whole arc — 7 Books, 21 Keys (`SliceFlow.LocksTotal`) — in small, subtle steps, with the Atrium at its stage-3 look when the Zodiac Wing finishes, and no protection for saves that had seen more.

The stage counter keeps its role: `AtriumStage` still steps 4, 5, 6 on the returns with Keys spent, so Caspar's return script and every stage check stand. What moved to the arc is what the player sees:

- **The Atrium kit.** The opening's pieces (Key 2–3: the pillar lantern, candle stands, desk, charts, bench) still restore by stage. The other fifteen carry `arcLock`, the lock count that restores each — a bust at the 2nd, the shelf at the 3rd, the rug at the 4th, then a piece most Keys, the chandelier at the 21st. The kit's level (grime, veil) climbs to 2 on the opening's beats and then only at the 7th, 14th and 21st Keys (`AtriumArcLevel`).
- **The Chamber kit.** Its pieces' Keys are lock counts across the arc: the mechanism and a candle per Key first (nine candles over the first nine), the banners and braziers through the middle Books, the crystal and the reliquary at the 20th and 21st. Level 1 from the first Key, then the 7th, 14th, 21st.
- **The light.** `LightAlphaFor(stage, locks)`: nothing at Stage 1, a quarter at Stage 2, then half plus the arc — about .6 with the Wing whole, full only at the last lock.
- **The hub caption is said once** (playtest note 9): the owner's Stage 2 line, "Stirring: one lamp lit, one desk uncovered, the Zodiac Wing open.", on the first return only. After that the room shows its own state; the old stage 3–6 lines are gone. No new player-facing copy.

With the Wing whole (four Keys spent) the Atrium holds 14 of 26 pieces at grime .6, the Chamber the mechanism and four candles. Caspar's "what remains is sealed, for now" stays true.

Checks: the mechanical light table (Stage 1–2, half at Stage 3, ~.6 at four locks, 1 at 21, `LocksTotal` 21); the slice fixture's Atrium light check takes the locks; the browser suite expects the Chamber at level 1 with three pieces after the second Key, the Atrium at level 2 with 14 pieces and its grime with the Wing whole, the light between .59 and .6, and the caption on the arc — each kit count read after the restore fade settles (the fade is not part of `busy`).

Validation: mechanical 478/478, slice fixture 200/200, WebGL clean (25.8 MB), browser suite 305/305 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports. Captures: [`Evidence/long-restoration-2026-09-27/`](Evidence/long-restoration-2026-09-27/).

Production suite after the deploy of `9c6eb1f` (which carries #65–#68): 305/305 at phone density (`DEVICE_SCALE=2 MOBILE=1`), both viewports, main's own suite.

## The journal's titles in blackletter

The owner's ruling (Sept 26), after a preview: the journal's page titles are set in UnifrakturMaguntia (SIL Open Font License; `Resources/Fonts/UnifrakturMaguntia.ttf` with `OFL-UnifrakturMaguntia.txt`), and bigger. `SliceView` sets the one title label's font (`TitleFont`) and its sizes. Page titles use `TitleSize` 28. A sign's name uses `SignTitleSize` 32, with its illuminated capital at `CapitalSize` 58, built by `SignTitle`. The label is 64 tall, since a line that doesn't fit its box is truncated away. The font is imported dynamic, so the Web player rasterizes it as it draws. It is a static font (no variation tables), unlike the variable Noto Sans Symbols that drew blank on the Web on Sept 12. The web state carries `journalTitleFont`.

Checks:

- Mechanical: the font and its license load from Resources, and the font has every letter of the 19 titles.
- Slice fixture: on a sign page only the title is blackletter, at the sign size. Every title fits its box in blackletter: the twelve sign names at 32 with the capital, and the five sections, The Signs, and Contents at 28.
- Browser suite: the Web build's title font is UnifrakturMaguntia, and the contents title draws. That means at least 3% dark ink in its box; about 13% was measured, and blank parchment reads 0%.

Validation: mechanical 478/478, slice fixture 200/200, WebGL clean (25.8 MB; the font adds about 90 KB), and browser suite 305/305 at 390 and 360, at phone density (`DEVICE_SCALE=2 MOBILE=1`). Captures: [`Evidence/journal-blackletter-2026-09-26/`](Evidence/journal-blackletter-2026-09-26/).

Production suite after the deploy of `3f0ce66`: 305/305 at phone density (`DEVICE_SCALE=2 MOBILE=1`), the Web build using UnifrakturMaguntia for the titles and the contents title drawing at 13% ink, the same as the local run.

## Key 3's ceremony and the table's seating

Sealing the twelfth seat started two coroutines side by side, and both cleared the one `busy` flag: `GridSeated` (1.1 s) and Key 3's `KeyCeremony`, started from `Update` (about 1.7 s). The game reported idle mid-ceremony, and the ceremony's own end could land during the walk home to the Atrium and clear that walk's busy mid-fade; since the fade overlay is a raycast target during a fade, the first tap in the Atrium hit it instead of the door. It happened on every Key 3 return. Only Key 3 did this — Keys 2 and 4 are earned on the Dial, whose ceremony doesn't overlap a second busy coroutine this way. `GridSeated` now runs the ceremony inside its own busy span (`yield return KeyCeremony(3)`); the `Update` trigger stays as a fallback for a restored save, guarded (`!busy`) to fire only when nothing else is.

Checks: the slice fixture requires the game idle (`Table Keeper Key` inactive) only once the ceremony is over. The browser suite's `tapToWalk` now fails a room tap that takes no walk on the first try instead of retapping (the `NOTE: retaps` line is gone).

Validation: mechanical 477/477, slice fixture 198/198, WebGL clean, browser suite 301/301 at 390 and 360 with no retaps. Production suite after the deploy of `f3aea9c` (which also carries Whitney's docs-upkeep and branch-health PRs, #58 and #59): 301/301 at phone density (`DEVICE_SCALE=2 MOBILE=1`), no retaps.

## Build J's art: the journal drawn

The journal's 17 files in `Resources/Art`, drawn to the Art Bible by the art lane: `journal-page` and `journal-contents` (the left page of a crimson ring binder; loose-leaf with the owner's twelve ruled lines, which the engine writes on; the contents page with the Library's illuminated border and vine), `journal-cover`, `journal-ribbon`, `journal-plate`, and `sign-aries` … `sign-pisces` (one set; the four figure signs Black, per the owner's ruling). Tuned to the art: unpracticed ink at 60%, the plates' labels in ink at 78%, and the gilt edge on the painted page (y 28–578).

Validation: mechanical 476/476, slice fixture 195/195 (new: at the Wing's end on the Art folder, Aries' page with every fact in full colour, then as line art, and the contents), WebGL clean (25.7 MB, about 1 MB more for the art), browser suite 297/297. Captures: [`Evidence/journal-art-2026-09-25/`](Evidence/journal-art-2026-09-25/).

## Build J: the journal as a book

The owner's Sept 23 REVISE on the journal ("functional but not yet a journal"), rebuilt on main after Builds I–O; PR #39 is superseded. Opening the journal shows a **contents page**: the sections the deck holds (the Elements, Symbols, Modalities, Table, and Opposites, in curriculum order), then **The Signs**. Nothing not yet learned appears. An entry opens its section. The Signs opens **a page per sign met**, in zodiac order, showing only what the player has learned: the name with an illuminated capital, the sign's picture, its symbol (after the symbols unit), its table cell (a 4 × 3 mini-table, after the table), and plates for element, modality, polarity, and opposite.

- **Illumination (owner, Sept 23):** the picture is one full-colour file drawn through the `Ascendant/Illumination` shader (`Resources/Shaders`). It is line art at 35% ink while introduced, then 40, 70, or 100% colour by streak, with a gilt edge at full colour, and a little faded when due. A section's entries show the same state in ink, with a gilding symbol.
- **Ribbons:** a sign's ribbon is as long as the ladder climbed, and pulled up past the page's head when the sign is due for practice. The contents flag a section, and The Signs, with a ribbon tab when anything in it is due. "Due" means due for practice: the table and the opposites are data only, so they never pull a ribbon.
- **No state words** are drawn. `journalStates` keeps them for the tests.
- **One tall page to a screen (owner, Sept 25):** the left page of an open binder. Drawn chevron arrows flip pages, and Contents returns. The page art carries the loose-leaf lines (owner, Sept 25), and the engine writes on them: a section's rows on every line, the contents' rows on every other line (`RuleTop` 132, `RuleGap` 40 on the 360 × 800 layout).
- **Slots:** 124 (15 new): `journal-contents`, `journal-ribbon`, `journal-plate`, and `sign-aries` … `sign-pisces`.

Validation: mechanical 476/476 (new in `ValidateBuildJ`: the contents, the sign facts unit by unit, no state words, Illumination's levels, the ribbons and "due", the contents' flags, reading changes nothing, the slots and the shader), slice fixture 192/192 (new: the contents, the early sign page, the elements; on the test set, the late sign page, Illumination read off the screen at saturation 0.17 at 20% against 0.55 at full, the ribbon lying and pulled out, and every line of every section fitting its rule), WebGL clean (24.8 MB), browser suite 297/297 (new: the contents, the sign page, and the elements, with the arrow and a contents row tapped on the canvas). Captures: [`Evidence/journal-book-2026-09-25/`](Evidence/journal-book-2026-09-25/).

## Build O: the Chamber kit

The Crystal Book Chamber, the Zodiac Wing's last room, as a kit (owner-approved pair, Sept 25). `ChamberKit` over the shell (`chamber`, shifted up 160 px at 2x so the long altar's top lands at y 335 and the walk line meets the floor in front of it), `chamber-grime` (the art lane's mechanism ghost erased), a cold blue veil, and 16 pieces restored by Keys spent (0–4): nine candles and the mechanism (1), two banners (2), two braziers (3), a crystal cluster and a reliquary (4). The seven Books stand on the altar (`ckit-book-worn` sealed / `ckit-book-restored` open by `BooksOpen`), their locks at each Book's foot; the old candles, mechanism, pages, labels, and floor band hide with the kit. The doorway back's tap area moves to the painted arch. Slots: 109 (15 new).

Validation: mechanical 458/458, slice fixture 177/177, WebGL clean (24.7 MB), browser suite 291/291 (new: the Chamber kit at two Keys spent). Capture: [`Evidence/chamber-kit-2026-09-25/`](Evidence/chamber-kit-2026-09-25/).

## Build N: the Atrium kit and the doors

Owner decisions, Sept 25 (Decisions Log, week of Sept 21–27): the Atrium restores stage by stage like the Wing, and each unlockable door goes from weathered and chained to clean with its edges glowing, and swings open as the Keeper reaches it.

The Wing's kit code is now a reusable `RoomKit` (`BuildKit`, `FinishKit`, `ApplyKit`, `SetKit`, `RestoreKit`); the Wing runs on it unchanged. `BuildAtriumKit` builds one kit per Atrium panel (the opening, the return, the Hub): the shell (`atrium`), `atrium-grime`, a cold blue veil (`VeilTint`, since the shell carries warm lantern light), and 26 pieces from `AtriumKit` (rug, chandelier, five star charts, four lamps, four banners, shelf, two busts, four plants, two candle stands, bench, desk), restored by the Atrium's stage (2 to 6). **Doors** (`AddDoor`): locked (`akit-door-locked`, chains and padlock), unlocked (`akit-door-closed` split into two leaves, a breathing edge glow, `PulseDoors`), and opening (`OpenDoor`: the leaves swing toward their outer edges and `akit-door-open` comes up, 0.5 s, before the fade; reduced motion cuts). The Zodiac Wing door is unlocked from the start, the Chamber's from the first Key, the sealed door stays locked. **Plates** (`akit-plate-locked` / `akit-plate-clean`) carry engraved names: THE ZODIAC WING, THE CRYSTAL BOOK CHAMBER; the sealed door's stays tarnished and unreadable. With the kit present the greybox props, lamps, and grey labels go; the tap areas stay. The art lane's "lantern" came back as a ring chandelier, so the two pillar lanterns reuse the wall lamp. Slots: 94 (28 new); the test set gains 28 placeholders.

Web state: `atriumKitLevel`, `atriumKitPieces`, `atriumKitRestored`, `atriumGrime`, `doors` (id:locked/unlocked), `lastDoorOpened`. Suite checks: Stage 2 (pieces worn, grime in, sealed door locked, Wing and Chamber unlocked), Stage 6 (all restored, grime gone), the Wing door opening as the Keeper reaches it. The fixture's Atrium check expects the kit files.

Validation: mechanical 458/458, slice fixture 177/177, WebGL clean (23.6 MB), browser suite 289/289. Captures: [`Evidence/atrium-kit-2026-09-25/`](Evidence/atrium-kit-2026-09-25/).

## Build M: the Wing room kit

Owner decisions, Sept 24–25 (Decisions Log, week of Sept 21–27; ClickUp 86bc74f9p): the Library starts dark and worn and each Key brings it to life; rooms are kits; the grey labels go; name plates on the door arches; instruments restore when they wake.

`SliceView.BuildWingKit` builds, over the shell: `wing-grime` (under the light overlay), the 19 pieces from `WingKit` (each a worn and a restored `CanvasGroup`, bottom-centred, files at half size), and a black veil. `ApplyWingKit` on every Wing room show: grime and veil by Keys (`KitGrime` 1 → 0, `KitVeil` .45 → 0), each piece restored by `KitRestoredNow` (its Key; the shelf on `WheelComplete`; the table on `ModalitiesComplete`). Pieces newly restored since the last show turn with a gold flare (`RestoreKit`, 1.75 s; reduced motion cuts). The restored plate carries "THE GRAND ATRIUM" in engraved text. With the kit present, the shelf, table, doorway, and Dial labels hide. Tap areas, walk points, and glows follow the approved layout (doorway −138, table −62, Dial 40, shelf 157). The kit art is the owner-approved pair's pieces (the art lane), cropped and scaled by Claude to the measured scene sizes, the room layers shifted up 120 px so the walk line meets the floor band; the test set gains 33 generated placeholders. Slots: 66.

**Follow-up (owner, Sept 25):** with the kit present, the Wing's light overlay leaves the Atrium-stage group and follows the Wing's Keys (`KitLight` 0 → 1), fading with the grime and veil in the same transition.

Web state: `kitLevel`, `kitPieces`, `kitRestored`, `grime`, `kitUp` (the restored piece names), `wingLight`. Suite checks: worn at the first visit, the shelf and table worn before they wake and restored when they wake, everything restored with no grime at Stage 6.

Validation: mechanical 458/458, slice fixture 177/177, WebGL clean (22.0 MB), browser suite 283/283. Captures: [`Evidence/wing-kit-2026-09-25/`](Evidence/wing-kit-2026-09-25/).

## Build L: the 100 px Keeper and the composed Wing

Owner decisions, Sept 24 (Decisions Log, week of Sept 21–27): the Keeper stands 100 px tall (the Wing mockup's scale), and the Wing is the owner's approved mockup B painted as one scene. A stopgap until the Wing room kit (ClickUp 86bc74f9p).

- **Keeper:** `SliceView.KeeperScale = 100/44` scales the figure whole; `PlaceAvatar` keeps the feet at band + 6 in every room. `keeper-idle`/`keeper-walk` become 44 × 100 (the owner's Sept 17 design, drawn by the art lane; the idle pose mirrored to face right like the walk). Caspar becomes 64 × 112, a head taller than the Keeper (working choice), feet where they were.
- **Wing:** `wing.png` is mockup B2 with the painted Keeper removed (patched from the art lane's empty room). `BuildWingRoom` reads `HasArt(wingRoom)`: with room art, the shelf, table, and Dial face aren't drawn in the room; their tap areas, walk points (`Walker`: doorway −136, table −67, Dial 38, shelf 150), and glows sit over the painted objects, and the grey floor band hides. Without room art the greybox shows as before. Over painted objects the waiting glows are halos (`SoftRing`), not filled discs. `wing-light` is the art lane's golden-hour layer at half alpha (full strength fogged the room in the game's linear blending).
- **Suite:** a new capture of the Wing at Stage 6 (`*-wing-room-stage6.png`). **Fixture:** the Wing check expects the composed room, its light, and the Keeper, and no separate table file.

Validation: mechanical 458/458, slice fixture 177/177, WebGL clean (20.3 MB), browser suite 273/273. Capture: [`Evidence/wing-composition-2026-09-24/390-wing-stages.png`](Evidence/wing-composition-2026-09-24/390-wing-stages.png).

## Build I: playtest fixes, every Key's ceremony, the Dial's waiting glow

[ClickUp task: Build I — Playtest fixes](https://app.clickup.com/t/86bc6kx5v). The owner's combined playtest of Sept 23 (Builds A–H) and two rulings the same day: the symbol challenge's fixed centre shows the target's name; the wheel poses the challenge and Caspar teaches (moving his teaching out of the wheel waits for the journal build).

`DialLesson.Challenge` is the wheel's own line, derived from state: the target's name in the symbols (Part B), otherwise "Next ‹element› after ‹sign›" (four forward), "Next ‹modality› after ‹sign›" (three), "Across from ‹sign›" (six). `DialView` shows it above the centre (the old "Start:" line, best-fit 13 → 10) and, in Part B, puts the name in the centre where the turning symbol was. The web state keeps `start` ("Start: ‹sign›") as a machine field and adds `challenge`. Four Caspar lines that only restated the challenge are empty strings now; his panel hides while `Message` is empty. The Count button, its web action, and its page control are gone; the Level 2 count beat stays. The message box is 326 wide with best-fit 13 → 9.

`SliceView.KeyCeremony(n)` runs for Keys 2 and 4 on the Dial (seam, ring swell, the Key rises and glows, fades into the count) and for Key 3 over the table (its own Key and glow on the grid screen). About 1.95 s; reduced motion cuts every tween to zero. `LastCeremony` is published as `keyCeremony`. `DialUnitWaiting` (symbols Part B, the modalities with Key 2 in hand, the last pattern with Key 3) drives a glow behind the Dial in the Wing room, breathing in `Update`, static under reduced motion; published as `dialGlow`.

Validation (local, branch `codex/playtest-fixes-keys`): mechanical 458/458 (adds the fixed symbol target, the challenge line, Caspar's silence on a bare challenge); slice fixture 177/177 (the Key 2, 3, and 4 checks now require the ceremony); WebGL build clean (18.4 MB, 0 errors, 0 warnings); browser suite 273/273 at desktop and 273/273 at phone density (DEVICE_SCALE=2 MOBILE=1), with captures of each Key mid-rise and the Dial's halo in the Wing room. The glows (Key, table Key, Dial, shelf, table) are soft discs from a radial sprite made at startup instead of flat squares.

## Build H: the light overlays

[ClickUp task: Build H — Light overlay slots](https://app.clickup.com/t/86bc3jd1j). The Sept 17 lighting decision (Option C): one transparent golden-hour overlay per room.

`Slots.Art` gains `atrium-light`, `wing-light`, `chamber-light` (360 × 800, cap 2048; 33 slots). `SliceView.LightOverlay` puts one Image per room panel (the opening and the return share the Atrium's file with the hub) as the panel's first child, so it draws over the background and under every prop, character, caption, and control; with no file the object is inactive and nothing changes. `LightAlphaFor(stage)` is the fade: 0 at Stage 1, .25, .5, .75, then 1 at Stages 5 and 6 (the steps are a tuning variable; every room follows the Atrium stage). `ApplyLight` runs from `Show`: on a room screen a stage change fades over 0.8 s (reduced motion: at once); a reload sets the alpha before the first frame. Web state `lightAlpha`, `lightFiles`. The style page lays its slots five per row so 33 sit above the sound rows. The test set carries three translucent amber overlays. Checks: mechanical (the three slots, the alpha table, the test set at 33), the slice fixture (the three overlays dressed and the alpha matching the stage on the test set), the browser suite (the overlays resolve and stay dark in the opening; a quarter at the first Hub; full once the Wing is whole; the style page at 33).

## Build G: Caspar's lines

[ClickUp task: Build G — Caspar's lines](https://app.clickup.com/t/86bc2ck78).

Copy only: the owner's worksheet rewrites (sections 1–13) applied verbatim across `SliceView`, `SliceFlow`, `DialLesson`, `DialView`, `GridModel`, and the web template's labels, with the game's bracket substitutions kept (`[sign]`, `[element]`, `[modality]`, `[n]`). `Zodiac.Polarities` is `Yang` / `Yin` (even seats, Fire and Air, are Yang); `DialLesson.ShareLabels` is Modality / Polarity / Element; `GridModel.Rule` and the table's misses say "square"; the tile labels say "placed". No mechanic, rule, or layout changed; two labels grew a line (the identity note, the end cards) and the Chamber button's text is smaller. The checks quote the new fragments: "try once more", ", Yang" / ", Yin", "six signs forward", "Not that square, acolyte. …", "Yes, Taurus belongs to Earth, fixed.", "Watch me place it, acolyte.", "Opposites share the modality and the polarity", "Before you turn the wheel", "what the two signs share", "placed", "proven at the wheel"; the browser suite matches "book upon the shelf", "Dark and quiet", "dark and bare", "already placed", "Three lamps", "Four lamps", "Wing is whole", "shares the same element after", "The Zodiac Wing is complete", "The wheel holds one last secret", "Four elements and three modalities", and "Modality" among the shared labels.

## Build F: the practice fork, the sitting rule, the gate, the journal

[ClickUp task: Build F — The practice fork on the Dial and the study journal](https://app.clickup.com/t/86bc291fn).

`SliceScreen.Review` is `Practice`; `Journal` is new. `SliceFlow`: `Sittings` (the clock; `Sitting` reads it), `PracticeAvailable` (any entered reviewable item), `CanEnterPractice` (on the Dial, Stage 2 on), `EnterPractice` (counts the sitting first, then queues up to six due items in the existing forms; with nothing due it sets `NothingDueLine` and returns false, the sitting counted), `LeavePractice` (at any point; unanswered items keep their due sitting), `Strikes` / `RecordStrike` / `StrikeLimit` / `Gated` (three wrong answers across one practice; the tap forms count their own, the wheel form counts `answer_rejected` through the slice), `CloseInstrument` (the gate: back to the Wing room with `Note == "gated"`), `ApproachDesk`, the journal (`JournalSections` in curriculum order, `OpenJournal` / `CloseJournal` / `JournalNext` / `JournalPrev`, `JournalEntries` rendered from the deck, `StateWord`). `DialLesson.ReviewHeader` names a practice item; `DialGeometry.Dim` fades the family lines while `Phase == Review`; every pause line ends with `JournalNudge`. `SliceView`: `EnterDialNow` shows the fork only at an idle wheel (a unit mid-way resumes as before), `StartLesson` is the old entry, `ContinueLesson` / `EnterPractice` / `LeavePractice` / `Gate` (from `Update`, once the answer's beat has settled) / `BuildJournal` / `ShowJournal`; the Atrium's Check the Seals row holds the journal button, the Wing and Chamber rooms carry one on their bottom row; the fork sits on the row under the Dial's Back button; the practice exit sits where the table's exit sits on the wheel form. Web actions `continue-lesson`, `enter-practice`, `leave-practice`, `open-journal`, `close-journal`, `journal-next`, `journal-prev`; web state `fork`, `practicing`, `practiceMode`, `practiceIndex`, `practiceCount`, `practiceSign`, `practiceSummary`, `strikes`, `sitting`, `gated`, `journal`, `journalSection`, `journalPage`, `journalCount`, `journalEntries`, and the `can*` flags. Save version 4 writes `sittings` and `reviewsChecked` alike; restore takes the larger.

Checks: mechanical (no fork or journal before anything enters; the fork on the Dial only; the sitting on entry, on leaving early, and with nothing due; strikes and the gate on both forms; re-entry with fresh strikes; the journal's sections, order, state words, and no deck field changed by reading; the desk; the save both ways and an older save; a thirty-entry liveness simulation with every item reaching the last interval; the audit's stall as a regression), the slice fixture (the desk, the journal from the Atrium, the fork, practice as the first sitting with a mid-practice reload, the second and third sittings, nothing due at the fourth, the gate at the fifth with the journal from the room, re-entry, the fork before every later unit, practice with the symbols and with the modality items), and the browser suite (the same at both viewports and phone density, with canvas taps on the fork, the practice's Seal, the journal's close, and the room's journal button).

## Evidence routing

[ClickUp task: Fix symbol answers bleeding into element mastery](https://app.clickup.com/t/86bc2338n).

Every answer records exactly one review-deck item kind, chosen by the lesson's phase, and the table is written out above the subscriptions in `SliceFlow.ObserveProgress`: element-family problems (`DialLesson.InElementProblem`: Guided, Independent, Optional, Continuation) record `ItemKind.Element` by the destination seat; symbol Part A records `ItemKind.Glyph` through `GlyphNamedEvent` and Part B through the `glyph_placed` event, so a wheel placement's `answer_correct` is never element evidence; modality problems record `ItemKind.Modality`; opposite and builder problems record `ItemKind.Opposite` by pair; the table records `ItemKind.Grid`; a review answer records nothing through the lesson subscription (the batch records its own item). The element filter is a positive list: a phase added later records nothing until it is routed on purpose.

Checks (`ValidateEvidenceRouting`, through the production subscriptions): the symbol unit end to end, Part A and twelve Level 0 placements, leaves every element, modality, grid, and opposite item byte-identical and advances all twelve symbol items; the element unit (guided, transfer, continuation) leaves every other kind untouched; the modality, opposite-and-builder, and table units each move only their own kind; a compressed Dial review answer records no lesson evidence of any kind.

## Post-Wing save checkpoints

[ClickUp task: Fix save capture ordering and partial symbol restoration](https://app.clickup.com/t/86bc2338p).

Lesson answer events record deck evidence but do not write a save. `DialLesson.ProgressCommitted` writes the checkpoint after naming advances, a Dial answer finishes `AfterCorrect`, a demonstration completes, or a builder sign commits. `GridModel.ProgressCommitted` follows the completed seating transition, including the final Key 3 award. `SliceFlow.ObserveProgress` provides the same subscriptions to the live slice and mechanical regression tests; `CaptureProgress` snapshots model progress and rewards without waiting for the view's next frame.

The existing `glyphStage` / `glyphIndex` fields now consistently mean:

- Stage 0: the index of the next naming card; lower indices have been read.
- Stage 1: all twelve names have been read; the index is the next wheel placement, and lower indices have been placed.
- Stage 2: Key 2 is earned; both parts are complete, even during a disposable practice replay.

The initial unit remains in zodiac order, so stage and index reconstruct its completed prefix without a new placement array. Resuming still starts at the Atrium; the player walks back to the book or Dial. In-flight answers and animation frames are not resumable checkpoints. Saves still begin after the first arrival at the Hub. Review scheduling and evidence routing are unchanged in this task.

Regression checks use the production subscriptions and actual JSON serialization after committed naming, placement, element-family, modality, opposite-pair, builder-sign, and table transitions. The Editor and browser suites additionally reload during Part A and Part B; their evidence includes `symbol-naming-restored` and `symbol-placement-restored` captures. First-Key reveal checks require `Flow.Keys == 1` immediately.

## Provisional implementation decisions

The milestone owner approved this recovery variation on September 11, 2026: use a different starting sign in the same elemental family as a fresh equivalent; use one shared three-encounter recovery budget for the move-four procedure, so changing signs cannot reset the budget. The first failed encounter occupies slot one. Fresh recovery problems occupy slots two and three across the activity. Ordinary local corrections remain within their encounter. If no unrevealed equivalent remains or the budget is exhausted, pause for conceptual transfer to later Review Deck work. There is no persistent Review Deck implementation.

Taurus is explicitly a teaching sign, not the player's Sun sign. Its guided Earth family precedes the Fire transfer family. Guided rule reminders are Level 2; adding Count never makes a rule-revealing response eligible. Worked demonstrations are exposure only. An optional one-problem Air probe has no additional reward and preserves the completed six-seat lesson record.

Sensitivity (55 logical pixels per detent), 120 ms snap, maximum one-detent inertia, screen dimensions, and presentation remain provisional. First-drag resistance (70%) is off by default after the September 11 solo playtest found it unnoticeable; `DialView.firstDragResistance` re-enables it and `inertiaEnabled` can be disabled for test comparisons. All Caspar panel copy is placeholder text written plainly in his canon tone (patient, observant, learned, restrained); it is not final narrative. Reduced motion always disables inertia. The test does not settle the larger repeatable loop, final review ratios, final art, rewards, room scope, or navigation.

## Reproducible checks

**The whole ladder in one command (Sept 25):** `PLAYWRIGHT_MODULE=<path to an npm-installed playwright> Tools/validate-all.sh` runs the mechanical checks, the shared WebGL build, then the browser suite and the slice fixture **in parallel** (the suite drives a browser against the served build, the fixture drives the Editor). The suite plays both viewports at once (`VIEWPORTS=390` for one), and the fixture's gap between steps is a setting (`-sliceStep`, default 1.5 s; 1.0 s fails the reveal-timing step). `ART_ONLY=1` skips the fixture and plays one viewport, for art-only PRs. Measured: suite ~20 → 7.3 min, fixture ~15 → 10.5 min, and the two now overlap.


Use Unity 6000.3.24f1. Run **Ascendant → Greybox → Run mechanical validation** or:

```sh
Unity -batchmode -nographics -quit -projectPath . \
  -executeMethod Ascendant.Build.GreyboxValidation.Run -logFile Logs/greybox-tests.log
```

The deterministic checks throw on failure and write `Logs/greybox-mechanical-validation.txt`. The shared `Ascendant.Build.WebBuild.Build` entry also runs them, so the PR Web workflow exercises them before building.

Run **Ascendant → Greybox → Run Play Mode validation** in the graphical Editor. It opens the dedicated scene, invokes the actual UI buttons and bridge actions, exercises the full lesson, and writes `Logs/greybox-play-validation.txt` plus portrait captures under `Logs/Evidence/`. This is scripted UI verification, not a human usability test. It adds fixed Game View sizes locally through the Editor API.

The dedicated scene and Web template are configured through **Ascendant → Greybox → Create scene and configure Web template**. The scene, its GUID, and import settings are generated by Unity. The original SampleScene remains available.

Build and serve:

```sh
Unity -batchmode -nographics -quit -projectPath . -buildTarget WebGL \
  -executeMethod Ascendant.Build.WebBuild.Build -buildOutput Builds/Web -logFile Logs/web-build.log
python3 -m http.server 8000 --directory Builds/Web --bind 127.0.0.1
```

Required output: `index.html`, `Build/Web.loader.js`, `Build/Web.framework.js.unityweb`, `Build/Web.data.unityweb`, and `Build/Web.wasm.unityweb` when the output directory is named Web. PR builds publish an artifact only. No PR preview deployment is configured or requested.

## Vertical slice v0.1

The shipped scene is `Assets/Scenes/VerticalSlice.unity` (created through **Ascendant → Greybox → Create vertical slice scene**), which wraps the Dial in the locked bookends: identity and birth prompt, Grand Atrium, Zodiac Wing with the Q07 props and the Q04 Key reveal, Atrium return, Crystal Book Chamber ending. `SliceFlow` is the pure five-screen state machine; `SliceView` builds the placeholder screens and beats. The Dial-only scene remains for its fixture.

September 11 copy session (v0.1 Copy Deck, Decisions Log continuation): all player-facing text comes from the deck. The birth prompt sets the sun sign for the session: a date derives it (`Zodiac.SunSign`, common almanac boundaries, no time or place), a known sign is picked from twelve, and "I don't know" assigns one at random with Caspar saying so. The Dial then teaches the sun sign's own family first, hands over a second family, and offers a third. The Zodiac Wing opens with a dormant Dial that wakes to the player, a simulated hesitation with no input, Caspar's disbelief, then teaching: seven beats, two automatic. Level 2 problems show the count once, one click per beat with the number, then return the ring; Level 3 demonstrations use the same beats. "Seal" replaces "Keeper's Seal"; the "Framed:" readout is gone from the screen (screen-reader labels say selected); the completion line and the Chamber ending are the amended lines. Long Caspar passages are paged by Continue; the Insert the Key button glows once he finishes. Ending beats hold longer by owner request.

Run **Ascendant → Greybox → Run vertical slice Play Mode validation** or:

```sh
Unity -batchmode -projectPath . -executeMethod Ascendant.Build.SlicePlayValidation.Begin -sliceAutoExit -logFile Logs/slice-play.log
```

It walks every screen through the same bridge the HTML layer uses (known sign Taurus so expectations stay fixed, plus the random and change-answer paths), waits out every automatic beat, writes `Logs/slice-play-validation.txt`, and captures each screen under `Logs/Evidence/slice-*.png`. The mechanical validation covers the flow rules, the sun-sign table, family selection for a non-Taurus sign, and the intro beats; the browser suite walks the bookends before and after the Dial, derives a sun sign from a date through the HTML date input, and waits for the count beats. Only deck lines are verbatim; nothing is saved between runs. No inventory system: a Key indicator only.

## v0.2: the return (Q05)

After the Chamber ending, Continue leads to the Atrium in Stage 2 "Stirring" with two entrances: the Zodiac Wing and Check the Seals. `ReviewDeck` holds the twelve sign-element items on the locked 1/3/7/14/30-day ladder; items enter as Introduced when the Hub is first reached, become Practicing on Level 0/1 evidence, step forward on eligible review success and back on a miss. A review batch is six due items alternating the compressed Dial (start seat framed, one line, Seal; one nudge then reveal) and direct tap of the element; the proportion is a test variable. The Wing continues Unit 1.1 with the two remaining families at Level 0 on the same lesson model (`BeginContinuation`), lights the wheel, awards no Key, and returns the player to the Atrium in Stage 3, which is the v0.2 end boundary. A local save (`PlayerPrefs`, key `ascendant.v02.save`) keeps sun sign, lit seats, families, Key, deck, day offset, and stage; a second sitting resumes at the Hub; Start over wipes it. "Advance one day (test)" shifts the review clock and is not shipped copy. Caspar's v0.2 lines are placeholders for the owner to write.

The mechanical validation covers the deck ladder and evidence rules, the flow through hub, seals, review, continuation, save and restore, and the lesson's continuation and review phases. The slice Play Mode fixture and the browser suite walk the loop end to end, including the review batch, both families, the reload-resume, and Start over.

## v0.3: glyphs and Key 2

Unit 1.1's second half, in the form the owner chose on September 12: **Part A** names each glyph by direct tap (four names, one nudge then reveal), **Part B** finds each named sign's glyph on the wheel in zodiac order with names hidden (`DialModel.Begin(start, hint, targetSeat)` gives the evaluator a target seat; the relationship logs as `seat_of_sign`; same hint ladder, Level 2 reveals the name on its seat, Level 3 demonstrates by counting forward from Aries). Key 2 after both parts with at least one Level 0/1 answer in Part B; twelve assisted placements pause without a Key. Twelve glyph items enter the review deck as Introduced when Part A begins and review in a glyph form. Glyphs render with the Unicode zodiac symbols in Noto Sans Symbols (OFL, `Assets/CelestialDial/Resources/Fonts`); not final glyph art. Seat labels never name a hidden seat. Key 2 is earned, not spent, in v0.3; the Atrium reaches Stage 4 and shows the v0.3 end card. Caspar's glyph lines are placeholders (copy deck section 8).

## v0.4: tap-to-move (Q06 phase 2)

The movement greybox in its locked form: two walkable rooms (Atrium, Wing room), a placeholder marker (`Walker`, pure C#: rooms and points of interest in `Rooms`, straight-line legs at a fixed speed, arrival, reduced-motion jump, test speed toggle), fades at doorways, parity buttons. `SliceFlow` gains `WingRoom` between the Hub and the Dial: `EnterWing` opens the room at its doorway, `EnterDial` and `LeaveDial` open and close the Dial, `LeaveWing` returns to the Hub (stage bumps unchanged) with the marker at the Wing doorway; the review leaves it at the desk; the Chamber and a resume place it at the entry spot. Sealed doors set a note and do not walk. The mechanical validation covers the tables, the walker's timing (distance over speed), and the flow placements; the slice fixture and the browser suite tap points of interest and buttons alike, capture the Atrium with the marker, the walk, and the Wing room, and check `walk_started`, `walk_arrived`, and `room_entered` events. Web actions: `walk:<poi>`, `enter-dial`, `walk-speed` (test), plus the existing `enter-wing`, `enter-seals`, `leave-wing`, which now route through the walk.

## Modalities on the Dial (Build A)

`DialModel.Begin(start, hint, target, step)` and `Forward` (4 for the elemental families, 3 for the modalities; `Relationship` logs `forward_offset_3`); `Zodiac.ModalityAt`. `DialLesson` phases `ModalityGuided`, `ModalityOwn`, `ModalityPaused`, `ModalityComplete`; `LitMod` / `KinMod`; `BeginModalities` (sun sign's family guided at Level 2 with the three-count, then the other two on the player's own); `RuleFor(step)` drives the second-miss and Ask Caspar wording; a worked example lights the seat without evidence and three in one sitting pause the unit. `ReviewDeck` gains `ItemKind.Modality` (36 items); `SliceFlow.StartModalities` introduces them; reviews use `ReviewMode.DialModality` (step 3) and `ReviewMode.TapModality` (Cardinal / Fixed / Mutable). Save v4 fields `litMod`, `kinMod`, `modalitiesStarted`. Web state: `unit`, `step`, `modalitiesComplete`, `litModCount`; overlay `modality-0..2`. Closing the book mid-practice abandons that pass (`AbandonPractice`) so the next unit stays reachable. Checks: mechanical (tables, three-step evaluator, guided → own, Ask Caspar wording, completion with no Key, deck items, review forms, save), slice fixture (unit end to end with one miss, review batches until a modality item appears, reload), browser suite (same, both viewports and phone density; the modality loop waits through each count beat and lets the last answer settle before checking completion).

## The finished loop (Build D)

`SliceFlow`: `KeysSpent` (= `LocksFilled`), `KeysInHand` (Keys earned minus spent), `BooksOpen` (`LocksFilled / LocksPerBook`), `WingWhole` (four spent), `CanEnterChamber` (at the Hub, after the first visit), `EnterChamber` / `LeaveChamber` (the Chamber as `Room.Chamber` with points of interest `atrium-door` and `books`; the Atrium's right door becomes `chamber-door`, walkable), `CanSpend` / `SpendKey` (one lock per Key; `key_spent`, `book_opened:<n>`, `wing_whole`; nothing accepts a Key the player does not have). Stages 4–6 move on `LeaveChamber` by Keys spent (2 → 4, 3 → 5, 4 → 6); `LeaveWing` keeps only Stage 3 (the wheel lit). Save field `locksFilled` (older saves restore `max(locksFilled, keyEarned)` and keep their stage). `SliceView`: `SliceScreen.ChamberRoom` reuses the Chamber panel in room mode (doorway block, the Books as a tappable point of interest, a floor band at `ChamberBandY`, "Back to the Atrium"; the first-visit label and Continue hidden), `Insert` spends at the Books and runs the `Spend` beat (the lock lights; on the third lock the Book brightens, a page rises, the candles light; on the fourth Key the closing line and the end card), `DefaultChamberLine` on arrival and a beat's line until the next move; the Atrium's `ShowHub` dresses per stage (shelf books at 4, light behind the sealed door at 5, four lamps at 6) and speaks by Keys in hand or spent; the end card reads "End of the Zodiac Wing" once the Wing is whole. Web actions `enter-chamber`, `leave-chamber`, `walk:chamber-door`, `walk:books`, `insert` (room mode); web state `keysInHand`, `keysSpent`, `booksOpen`, `wingWhole`, `canEnterChamber`, `canLeaveChamber`, `atBooks`, `room` = `chamber`. Checks: mechanical (the rooms and doorways, no spend without a Key, each lock, Book 1 at three, Book 2's first lock at four, the stage mapping on the return, the save, an old save), slice fixture and browser suite (after each of Keys 2–4: the Chamber room, the walk to the Books, the canvas Insert, the lock / the Book opening / the Wing whole with the end card, the return's stage and caption, reloads with the Books intact).

## Art slots and sound hooks (Build E)

`Slots` (pure manifest and loader): `Slots.Art` (image slots, 28 at Build E and 33 since Build H: name, the placeholder's rect, a size cap, where it is drawn) and `Slots.Sounds` (7); `Slots.Set` (`""` = the owner's `Resources/Art` and `Resources/Audio` folders, `"test"` = the shipped test set) and `Slots.StyleRequested`, resolved from the page URL (`ParseQuery`: `?art=<set>`, `?style`, `?style=<set>`), the command line (`-artSet <set>`, `-style`), the Editor menus, or a fixture's `Request(set, style)`; `Image(slot)` / `Clip(slot)` (`Resources.Load`, one lookup per slot per set, null without a file); `Source` / `SoundSource` (`file`, `placeholder`, `<set> set`; `silent` for sounds); `Dress(image, slot)` puts the file on a placeholder Image and registers it (`Dressed`, `DressedCount`, `IsDressed` are test evidence); `Paint(image, placeholderColor, brightness)` keeps a state color for the placeholder and turns it into a brightness for a file. `Sound` (one cue source, one loop source, an `AudioListener` if the scene has none): `Play(slot)` at most once per frame, `Cue(event, correct, tapPhase)` maps the model's events to slots, `ToggleMute`, `Muted`, `LastCue`, `Played`. `SlotImport` (`AssetPostprocessor`) applies the import settings to anything under the two folders; `SlotMenus` are the Editor toggles. The views dress every placeholder at build time (`ScreenPanel` and `Block` take a slot), hide the drawn-on details a file replaces (ring lines, bracket bars, dust cloths, the board's squares, the lock mark, the Key's label, the Keeper's marker), and route state colors through `Paint`; the style page (`BuildStyle`, `ShowStyle`, `FillStyle`) replaces the game when asked for, with `DialView.Inert` so no action reaches the hidden Dial. The web state carries `artSet`, `artFiles`, `soundFiles`, `muted`, `lastCue`, `cuesPlayed`, `style`, `styleSlots`, `styleSounds`; actions `mute` and `sound:<slot>`; the template positions the hub's third test button and the style page's sound buttons and lists the slots for a screen reader. The test set is generated by `Tools/make-test-set.py`.

Checks: mechanical (the manifest, the URL parser, the loader on the test set and on an unknown slot, the import settings on a test PNG and WAV, the size of the test set on disk, the cue table, the mute toggle); the slice fixture (the whole run on the Art folder, then the same save with the test set: the Atrium, the Wing room, the Dial, and the Chamber dressed at both viewports with captures, the door cue played, the style page on the test set and on the Art folder, a sound slot played from it, the mute toggle, and no save or set request left behind); the browser suite (no query: the Art folder, no style page, the page hook firing without files, the mute toggle from the Atrium; `?art=test` at both viewports: every slot resolved, the opening and the Dial captured, the page and step cues played; `?style=test` and `?style`: 28 + 7 slots with sources, the semantic list and sound buttons, a sound played, the mute toggle, no vertical scroll). Build size: the test set and the wiring add 182 KB to the compressed WebGL build (`.data` +74 KB, `.wasm` +103 KB for the audio module and the code, `.framework.js` +4 KB; 16,892,733 → 17,078,580 bytes), against a 1 MB budget.

## Polarity, opposites, the builder, and Key 4 (Build C)

`Zodiac.Polarities` (one constant; Build C shipped `day` / `night`, Build G made it `Yang` / `Yin`), `PolarityAt` (Fire and Air are Yang, Earth and Water Yin: the elements alternate, so even seats are Yang), `OlderPolarityTerms`, `Opposite` (six seats on), `PairOf` (a pair is named by its lower seat, six pairs). `DialLesson` phases `Polarity` (a two-line beat on Continue; the second line sets `PolarityShown`), `OppositeGuided` / `OppositeOwn` / `OppositePaused` / `OppositesComplete` (`Dial.Begin(start, hint, -1, 6)`, `RuleFor(6)` = "the opposite sits six seats on"; the first pair from the sun sign at Level 2 with a six-count, then five pairs at Level 0 in wheel order from the sun; the reappearance cap as in the modality unit; `OppKnown[6]`), and the builder `BuilderName` / `BuilderOpposite` / `BuilderShare` / `BuilderPaused` / `Key4` (`BuilderTargets` = sun, sun+5, sun+7; `BuilderOptions` = the sign, its two same-element signs, one same-kind sign; `AnswerBuilderName` one nudge naming the missing property then a reveal; the wheel step with the full ladder; `AnswerBuilderShare` marks kind and side, one nudge on the element then the rule; a sign is unassisted when every step stayed at Level 0/1; `NextBuilderSign` after the view's hold; `Key4Earned` on three built with `BuilderEvidence`, else `BuilderPaused` and a cleared builder next visit). `CanBeginOpposites` needs `Key3Held` (set by the slice from the Keys held) and the modality unit complete. Seats show the side as a word after the beat (`RefreshSeats`, `SeatLabel`). The view adds four name buttons and three share buttons in the wheel's control rows, a hold after a name and after a built sign, and the count words five and six. `SliceFlow`: `StartOpposites` introduces six `ItemKind.Opposite` items (data only, `Reviewable` false), `RecordOppositeAnswer` by pair, `MarkKey4`, Stage 6 on the return with four Keys. Save fields `polarityShown`, `oppKnown`, `oppositesStarted`, `built`, `builderEvidence`; `keys` carries Key 4; the deck array is 60 items. Web actions `builder-name:<slot>`, `builder-share:<0 kind|1 side|2 element>`; web state `unit` (`opposites` / `builder`), `polarityShown`, `pairsKnown`, `oppositesComplete`, `builderStep`, `builderAsk`, `builderOptions`, `shared`, `built`, `key4`, `canBuilderName`, `canBuilderShare`. Checks: mechanical (the tables, the six-step evaluator, the beat, guided → own, the nudge, Ask Caspar with the six-count, the worked example and the cap, completion, the builder's three steps with each miss kind, Key 4 once, no Key without evidence and the cleared builder, restores mid-pattern and mid-builder, data-only items, Stage 6, the save), slice fixture (the beat, six pairs with one miss and one Ask Caspar, the builder with a nudge of each kind and one reveal, Key 4, Stage 6, reload), browser suite (the same on both viewports and phone density, with a canvas tap for the first name).

## The table and Key 3 (Build B)

`GridModel` (pure C#, beside `ReviewDeck`): `RowOf` / `ColumnOf` / `CellOf` / `SeatOf` (each element-modality cell holds exactly one sign), `Begin` (opens or resumes; a full table without Key 3 is cleared for another try), `Pick` (a sign in hand; free until a miss, then locked until seated), `Choose`, `Seal` (the same ladder: Level 1 `Nudge` names the element when the row was wrong, otherwise the kind; Level 2 `Rule` states both; Level 3 sets `Demonstrating` and the view's beat calls `AfterDemonstration`), `Ask` (the rule once after a first miss; a seating after it earns no evidence; a miss after it goes to Level 3), the reappearance cap (three seatings by Caspar in one sitting → `GridPhase.Paused`), `Key3Earned` on twelve seated with `Evidence` (any Level 0/1 seating; note that the last sign always seats at Level 0 by elimination, so in practice Key 3 follows twelve seated). Events log on their own stream with `requested_relationship` `cell_of_sign` (`grid_unit_started`, `grid_resumed`, `sign_picked`, `cell_chosen`, `answer_*`, `hint_escalated`, `hint_asked`, `grid_placed`, `grid_paused`, `key3_earned`). `Rooms.Wing` gains `grid` ("the table", x −60); `SliceFlow` gains `ModalitiesComplete` (marked by the view when the lesson finishes the unit; derived from `litMod` on restore), `CanOpenGrid`, `EnterGrid` / `LeaveGrid`, `TouchDarkGrid`, `StartGrid` (introduces `ItemKind.Grid`), `RecordGridAnswer`, `MarkKey3`, and Atrium Stage 5 on the return with three Keys. Deck as data (owner, Sept 15): `ReviewDeck.Reviewable(kind)` and `DueForReview` keep grid items out of `DueCount` and every batch. Save fields `gridPlaced`, `gridEvidence`, `gridStarted`; `keys` carries Key 3; the deck array is 48 items. Web actions `enter-grid`, `walk:grid`, `grid-sign:<seat>`, `grid-cell:<cell>`, `grid-seal`, `grid-ask`, `leave-grid`; web state `grid*`, `key3`, `canEnterGrid`, `canGrid*`, `canLeaveGrid` (`gridHintLevel` is test evidence only). Checks: mechanical (cell table, the ladder both ways, Ask Caspar, the cap, Key 3 once, restore, the room and flow rules, data-only items, Stage 5, the save), slice fixture (dark table on the first visit; after the unit: the room button, twelve seatings with one miss of each kind, one Ask Caspar, one seating by Caspar, a reload at five seated, Key 3, Stage 5, reload), browser suite (the same on both viewports and phone density, with canvas taps for the first seating).

## The difficulty ramp (v0.3 revision, build 3)

`DialLesson.BeginPractice` (after Key 2) replays Part A then Part B without touching `Key2Earned`; `FinishGlyphs` in practice fires `PracticeFinished(clean)` and, on a clean pass, `CleanRuns++` and `symbol_practice_clean`. `Hard` (`CleanRuns > 0`) drives: the order (`order[i] = (i*5 + 1 + CleanRuns) mod 12`, a full permutation), `OptionsFor(seat, hard, salt)` (look-alike plus both same-element signs, rotated by seat + salt), and the Part B first hint (no Aries anchor). `SliceFlow.CleanRuns` persists (save field `cleanRuns`) and hardens `GlyphReviewOptions`. Checks: mechanical (option sets, first replay in order, clean run counted once, hard shuffle, anchor dropped, review names, save), slice fixture (clean replay then a hard book, close mid-practice, reload keeps the count), browser suite (same on both viewports).

## Ask Caspar (v0.3 revision, build 2)

`DialModel.CanAsk` (active, at least one miss, below Level 2) and `Ask()` (sets `Asked`, escalates to Level 2, logs `hint_asked`); a later miss with `Asked` goes straight to Level 3. `DialLesson.AskCaspar` writes the reminder (`RuleLine` with the count beat for family problems; the name on its seat in the symbols) and `CanAsk` limits it to phases that carry a rule (Independent, Optional, Continuation, Review, symbol placement; never Guided, which starts at Level 2). The view's `ask-caspar` action and button, `canAsk` and `hintLevel` in the web state (the level is test evidence only; the status line no longer prints it). Checks: mechanical (offer timing, reminder once, no evidence after, miss after ask → Level 3, none in Guided, symbols reveal), browser recovery path (asks instead of a second miss).

## The book on the shelf (v0.3 revision, build 1)

`SliceScreen.Book` sits between the Wing room and the naming cards: `SliceFlow.EnterBook` (needs `CanOpenBook`: in the room with the wheel lit) and `LeaveBook`; `TouchDarkShelf` sets the note before the wheel is lit. `SliceView.OpenBook` begins Part A on the lesson (or shows the closed book once all twelve are named or Key 2 is held); the twelfth name calls `LeaveBook`. `EnterDialNow` starts Part B only when `DialLesson.AllNamed`; otherwise the lit wheel shows `ShelfFirst`. The shelf is a Wing point of interest (`Rooms.Wing`, x 120) with a room button and `enter-shelf` / `close-book` web actions. Checks: mechanical (dark shelf, book open/close, hand-off to the wheel), slice fixture (Dial-before-book, shelf, book, room, Dial Part B), browser suite (same path plus the dark shelf on the first return).

## No in-game time (Q05 amendment, September 13)

`SliceFlow.Sitting` is the number of completed Check the Seals batches and replaces the day counter; `ReviewDeck` schedules in sittings with the same 1, 3, 7, 14, 30 ladder, and newly entered items are ready at once (`dueDay = sitting`). There is no `AdvanceDay`, no `advance-day` web action, and no `dayOffset` / `firstDay` in the save (version 3; older saves still load). The mechanical checks, the slice fixture, and the browser suite open Check the Seals on the first visit and expect six items.

## Symbol font and the browser suite's phone mode

The zodiac symbols come from a static Regular instance of Noto Sans Symbols baked at import (custom character set, 96 px), so the Web player never rasterizes them at runtime. The Editor cannot catch a label that lost this font: macOS substitutes a system font for missing symbols, the Web player draws nothing. The browser suite therefore is the check that matters for symbols; `DEVICE_SCALE=2 MOBILE=1` runs it at phone pixel density with touch emulation, and `BROWSER=webkit` runs it on WebKit, which every iPhone browser uses. The template exposes `window.ascendantDial.act(command)` for test probes; it is the same bridge the buttons use.

## Manual acceptance checklist

Record device, viewport, input, observed result, and evidence for each check. Untested entries are pending, never assumed passed.

- [ ] Twelve ordered seats, Aries at the fixed 9 o'clock home marker, counterclockwise zodiac order; forward rotates the ring clockwise.
- [ ] 390 × 844 and 360 × 800 fit without vertical scrolling; all seats and Count have at least 48 × 48 effective targets; primary controls are at least 56 high.
- [ ] Drag four detents, release between detents, and verify nearest-seat snapping, no bounce, and no more than one additional detent of inertia.
- [ ] Next, Previous, direct seat selection, keyboard arrows, Tab/Shift-Tab + Enter/Space, and screen-reader/switch activation reach equivalent committed outcomes.
- [ ] Selection never submits. Keeper's Seal submits once. Five forward then one back before Seal is not a mistake.
- [ ] Starting seat counts as zero. Pisces-to-Aries wraparound is correct. Every neutral count has identical emphasis, including four.
- [ ] First incorrect Seal leaves the ring in place and nudges. Second leaves it in place and reveals the Level 2 rule. Third demonstrates, resets, and queues a different start within the same family if allowed.
- [ ] Level 2 plus Count remains Level 2. Worked or immediately revealed answers never become independent evidence. Fresh Level 0/1 success can qualify.
- [ ] A third recovery encounter cannot create a fourth through sign changes. Pause without a Key if eligible evidence is missing.
- [ ] Start positioning and successful-problem home returns produce no player detent events. Home is visible before the next start aligns.
- [ ] Reduced motion disables inertia; default OS reduced-motion preference is respected in WebGL. Knowledge does not depend on animation, sound, or color.
- [ ] Seat labels expose sign, position, framed state, and revealed element. Browser focus is visible; selected destination and neutral count are announced where the assistive technology supports live regions.
- [ ] Complete both families: six lit, six dormant, two connections. Key 1 appears only with eligible evidence. No Retained, Sealed, permanent restoration, or durable-mastery claim appears.
- [ ] Optional probe is voluntary, logs offer and acceptance, and awards no additional Key.
- [ ] Answer logs contain start, relationship, destination, correctness, selected input method, hint level, attempt, response time, and evidence eligibility.

## Evidence and limitations

Local JSON events are prefixed `[CelestialDial]` in the Editor/player or browser console. They use session-relative timing and no production analytics service. Browser semantic controls call `WebAction`, which reaches the same selection and evaluator used by Unity buttons and drag. Native Editor screen-reader support is not claimed; WebGL uses native HTML buttons and a polite live region via Unity's documented JavaScript plug-in bridge. Actual screen-reader and switch hardware compatibility must be observed on the target device/browser combination.

Gameplay validation is the owner's solo playtest against [the solo checklist](SOLO-PLAYTEST.md); decisions are recorded on the ClickUp task. The September 11 pass recorded **REVISE**, which this revision addresses.
