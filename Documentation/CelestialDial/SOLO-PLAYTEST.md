# Celestial Dial solo playtest checklist

A single designer pass on the deployed greybox. The owner is the only playtester; the solo decision is the milestone decision.

Prototype: https://davonlemar30.github.io/Ascendant/  ·  Task: https://app.clickup.com/t/86bbyd0vw

Tick what you observed. Anything unticked is untested, not passed. Write short notes inline; the point is what your hand did, not whether it sounded fun.

## Setup (2 minutes)

- [ ] Phone, portrait, the real thumb. Note device and browser: ___
- [ ] Second pass later on desktop Chrome with the device toolbar at 360 × 800 to check the floor size.
- [ ] Reduced motion off for the first pass. (Toggle at the bottom of the screen.)
- [ ] Optional: desktop DevTools console, filter `CelestialDial`, if you want the event log.

## First contact

- [ ] Before reading anything, what did your thumb try first: drag, tap a seat, tap a button? ___
- [ ] Does forward read as the ring turning clockwise beneath the fixed bracket, without thinking about it? Any "backwards" feeling: ___
- [ ] First-drag resistance is off by default (dropped after the September 11 pass). If re-enabled for comparison, does it read as weight, or as lag? ___
- [ ] The bracket sits around exactly one seat at rest. Sitting under the marker never grades anything.

## Guided Earth family (Taurus teaching sign)

- [ ] Taurus is introduced as a practice sign that stands in for your own.
- [ ] Drag four detents in one thumb sweep. Count the ticks: all four feel identical, no emphasis on the fourth.
- [ ] Release between seats: snaps to the nearest seat, about 120 ms, no bounce.
- [ ] Flick: never more than one extra detent of inertia.
- [ ] The seat under the marker shows in the readout. Nothing submits until Keeper's Seal.
- [ ] Seal on Virgo: seat lights with its element, ring returns home silently, next start (Virgo) aligns silently with no ticks.
- [ ] Complete Capricorn. The three Earth seats connect with a line. No aspect words appear.

## Fire transfer with less guidance

- [ ] Start aligns on Aries silently. Instruction is reduced. Count is still available and stays quiet.
- [ ] Use Forward and Back only (no drag): step five forward, one back, then Seal. It is accepted as correct and independent.
- [ ] Solve the second Fire problem by tapping the destination seat directly. It moves under the marker without submitting; Seal accepts it.
- [ ] Six seats lit, six dormant, two family lines. Keeper Key 1 message appears once. No "Retained", "Sealed", or "restored" wording anywhere.
- [ ] Optional Air problem is offered. Did you want to take it? Did you keep turning the Dial when nothing required it? ___

## Mistake ladder (reload and deliberately fail)

- [ ] Wrong Seal once: local rejection with the `×` cue, ring stays where it was, Caspar says "Not that one. Count your steps again." Help level 1.
- [ ] Wrong Seal twice on the same problem: Caspar states the rule: "count four signs forward. Your start sign is zero." Help level 2. Pressing Count does not lower it. Inertia is off.
- [ ] Wrong Seal a third time: one worked demonstration, reset, then a fresh problem from a different sign in the same family at help level 0.
- [ ] Repeat to exhaust the shared cap: the third recovery encounter ends with a clean pause and no Key. No fourth problem appears by switching signs.
- [ ] After a Level 2 or 3 reveal, only the fresh problem's success counts toward the Key.

## Inputs without dragging

- [ ] Desktop keyboard: Left and Right arrows step one detent, Tab moves through the controls in a sensible order, Enter or Space activates the focused control, and only the Seal submits.
- [ ] iPhone VoiceOver (or Android TalkBack): each seat announces sign, position, under-the-marker state, and element when lit; the destination under the marker and the count are announced as they change.
- [ ] Reduced motion on: no snap animation, no inertia, everything still completes.
- [ ] Every path reaches the same accepted answer as dragging.

## Layout

- [ ] 390 × 844: no vertical scrolling during the Dial, Dial dominates, Seal dominates the control row.
- [ ] 360 × 800: still no scrolling; seats and Count still comfortably tappable.
- [ ] Nothing depends on color alone: text and shape cues carry every state.

## Feel notes (the actual test variables)

- Direction at the 9 o'clock marker: ___
- 55 px per detent, too heavy or too light: ___
- First-drag resistance (now off): anything missing without it? ___
- Snap and inertia: ___
- Seal as the commit action, obvious after one guided exposure: ___
- Tedium on the optional problem: ___

## Known, not defects to re-report

- Automated pointer input in the very first frame after a phase transition is ignored; not reachable by a human.

## Solo decision (provisional)

Decision: **PASS / PASS AS TEACHING TOOL / REVISE / FAIL** (circle one), recorded by ___ on ___.

Evidence and counterexamples: ___

Next approved change, if any: ___

Post the decision as a comment on task 86bbyd0vw.
