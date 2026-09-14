# Solo playtest 3 — v0.4 on production (September 12, 2026)

Owner's record, copied from the ClickUp page [Solo Playtest 3 — v0.4 on production](https://app.clickup.com/90141007990/docs/2kyd583p-6954/2kyd583p-24654). Build under test: production at `e34adcf` (v0.4 tap-to-move with the Chamber page-turn fix). iPhone, Chrome, thumb, upright. Computer checks, VoiceOver, and the small-window size check were skipped this session.

## What was checked

- **Setup, first touch, the first family with Caspar, the second family, getting it wrong on purpose:** all boxes ticked except the two button-only and tap-a-sign items in the second family. "Drag the wheel. Thumb felt good, wheel moves and drags well." Next moved the way expected. Flicks and releases landed cleanly both times. Caspar names the sign, explains the elements well, and lets the player take it from there. The extra problem was taken and was good practice.
- **Other ways to play:** reduced motion on a problem worked. Keyboard, VoiceOver, and the every-way comparison were not done.
- **Opening and ending:** ticked through the Chamber. "The ending was perfect." The wheel waking, the dialogue, and Caspar's surprise all made sense. The birth-prompt three-way check was not ticked.
- **The return (v0.2):** the changed Atrium, the three test buttons, Check the Seals before and after a day, the six-item review, and the Wing continuation were all ticked. The twelfth-seat and reload items were not ticked.
- **The marks (v0.3):** not reached this session (no boxes ticked).
- **Walking (v0.4):** every box ticked. First thumb action on seeing the Atrium: tapped the Zodiac Wing door. "Walking was smooth. Did not slow me down at all." Reduced motion worked. Timing not measured; "smooth and did not feel slow."
- **Fit on the phone:** fit well; lit, selected, and wrong seats told apart.

## Interview

1. Next and drag went the way expected. 2. Step size felt perfect. 3. Did not miss the stiffer first drag. 4. Landed cleanly. 5. Seal as the answer was very clear. 6. Extra problem: good practice. 7. Wake-up and surprise made sense, about right. 8. Chamber ending: perfect, about right. 9. Wanted to go back into the Wing to play with the touch features; "pleasantly surprised I could touch where I wanted to go and the player would walk there. Very dope!" 10. "Checking the seals" as a phrase was unclear; the wheel felt like an interactive tool or instrument. 11. "Marks" was unclear (the glyph unit was not reached); the lessons felt learnable, engaging, and fun. 12. Walking made the Library feel like a place: "Smooth experience, loved it." 13. The walk did not slow the return to the wheel.

## Defects reported

- **Seat names sit on the top edge of their tiles** (screenshot on the ClickUp page). Every name on the ring, including the one inside the bracket, is drawn at the tile's top edge instead of its center, and the misalignment turns with the wheel. Cause: the v0.3 change that moves a name below its mark set the no-mark position to the top-anchored rect's origin instead of the tile center. Fixed on branch `codex/seat-name-centering`.

## Decision

Evidence line as written by the owner: "Wheel sign-name alignment is off (see screenshot). Walking, reduced motion, phone fit, audio, Caspar dialogue, wheel feel, and Chamber ending all passed clean. Seals and marks terminology unclear to tester; may not have been specifically tested. Computer tests and 'getting it wrong on purpose' skipped this session."

The decision word was not chosen on the page. Proposed to the owner for confirmation: **PASS** for the v0.4 movement greybox (every walking item ticked, no counterexamples, first thumb action was the doorway tap), with the seat-name centering fixed as a follow-up. The v0.3 marks decision stays open until the glyph unit is played.

## Addendum, September 13: the marks (v0.3), played on production 50b83a3

Owner's paper notes and one screenshot (iPhone, Chrome, at work), converted by the agent; evidence appended to the ClickUp page.

- **Reached.** Birth date entry with the calendar ("It's dope how you can use the calendar now to choose your birthday"). Part A, name the mark ("The Zodiac Wing, Mark 1 of 12"). Part B, find the mark, to "Find the mark of Cancer", 3 of 12, Help level 1, Aries named on its seat after the first miss.
- **Defect 1.** In Part B the seat boxes were blank on the phone; no marks rendered, so the task could not be done. Reproduced in Chromium at device scale 2 (the marks vanish from the seats while the big card still renders) and not in the Editor at the same pixel size, so it is the Web build's font rasterization of the variable font at that size. Fix: ship a static Regular instance of Noto Sans Symbols (fontTools instancer, wght 400, same file and license).
- **Defect 2.** The Count button showed in Part B, where counting is not the task ("Count does not belong here"). Hidden in the marks phase.
- **Design requests (owner).** Clearer objective wording on the Elemental Pattern ("Find the next elemental sign after [sign] on the wheel" instead of "Start at Pisces. Count each sign after it: one, two, three, four"); "Find the mark" may read better as "Find the symbol"; the marks unit should either transform the wheel or unlock a separate instrument in the same room (owner prefers the separate instrument); a visible "ask Caspar for a hint" option for beginners; difficulty that rises after the first successful run.
- **Not reached.** Key 2, glyph review items, reload with two Keys.
- **Decision.** REVISE proposed (the owner said the same); awaiting confirmation on task 86bbznawx.

## Marks, test 2 after the fix (September 13, production 8b842f3)

- **Confirmed.** Part B shows the marks on the seats.
- **Defect 3.** During "Find the mark of [sign]" the center readout still named the sign under the bracket as the wheel turned, which gave the answer away; the "Your sign / Start" line was unnecessary in that challenge. Fixed: in Part B the center shows the mark under the bracket, no start line, no count; the screen-reader readout says "Selected: mark [glyph]"; the browser suite asserts no sign name in any Part B readout.
- **Owner question.** The purpose of the day system ("Next day (test)", "N due"): it is the locked Q05 review ladder (items come due 1, 3, 7, 14, 30 days after they are learned); "Next day" is a test-only shortcut so the ladder can be exercised in one sitting. Decision on how it is presented pending with the owner.
- **Decision.** Stays REVISE.

## Symbols replay, test 2 (September 14, production c17b094): the owner's paper notes

- **Defect 4, stuck in Check the Seals.** On the first return, Check the Seals opened the compressed Dial item with the Wing's "Back to the Atrium" button drawn on top of Seal (screenshot). The button did nothing there and Seal could not be pressed; only a refresh got out. Cause: the button was switched on at the Key reveal and only managed while on the Wing screen. Fixed: managed wherever the Dial shows; the suite now taps the review's Seal on the canvas.
- **Defect 5, "The seals hold for now" after a refresh.** Cause: since v0.3 the `[Serializable]` marker sat on the `ItemKind` enum instead of `ReviewItem`, so the JSON save skipped the deck and every reload restored an empty one. Fixed; a mechanical check round-trips the deck through JSON; the fixture and the suite reload mid-review.
- **Part B started on or beside the answer** ("the wheel should randomize the starting point"). Fixed: each problem starts three to nine seats away; the first hint reads "Find Aries first, then count forward to it."; the worked example turns to the symbol without naming a start seat.
- **Wing room caption** showed the Dial's prompt ("Find the symbol of Aries...") while still in the room. Replaced with a room line.
- **"A weird number 10 next to it."** The Seals button no longer shows a count; the review screen shows n of m.
- **Copy (owner writes):** "Six seats?" (Hub first line); "Now the Fire family. You have done this twice" (continuation intro); "The names you know. Their symbols wait on the shelf." Better intros to each challenge. All flagged on the worksheet.
- **Design request:** the Dial should unlock a book the player can study before the symbol challenge (a study guide or journal that unlocks new concepts), since the game should not assume the player already knows the symbols. Recorded for a brief; it matches the canon's Introduced-before-retrieval rule.
- **Known, planned:** no way back to the Key Room (the Chamber) from the Atrium; Build D makes the Chamber walkable.
- **Decision:** stays REVISE.
