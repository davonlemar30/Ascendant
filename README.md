# Ascendant

*A game about learning to read the sky, set in a library that only answers to you.*

**Play it in your browser:** https://davonlemar30.github.io/Ascendant/ — nothing to install, works on a phone.

Ascendant teaches Western astrology the way a patient tutor would: one pattern at a time, on an instrument you turn with your own hands, with a guide who never lets you fake it. You are the Keeper, the last of a bloodline that built the Celestial Library. Caspar, its caretaker, has kept the lights on for centuries and cannot touch a single instrument. You can.

## What you are playing right now

This is an early, playable build — a **greybox** with its first art landing. The shapes, rooms, and instruments are real and the learning is real; the Atrium and the characters now have illustrated art, the other rooms and instruments are still placeholder blocks, the sounds are not in yet, and Caspar speaks in the writer's own lines. Expect a museum at night rendered in charcoal and bone, not a finished painting.

The current build is **the Zodiac Wing**, the first wing of the Library. It holds one curriculum: the twelve signs, and how to *derive* what a sign is instead of memorizing twelve personalities.

**Your journey through the Wing:**

1. **Wake up.** You are asked your name and when you were born. From that the game finds your sun sign (or picks one for you if you don't know). Everything Caspar teaches starts from *your* sign.
2. **The Grand Atrium.** Dust, covered furniture, sealed doors, one weak candle. Caspar explains what he can. Every Key you spend in the Chamber wakes it one more step.
3. **The Celestial Dial.** A great wheel of twelve seats that wakes when you step close. Caspar teaches the first pattern on it: the four elements, and how signs of one element sit evenly spaced around the wheel. You turn the wheel, count the seats, and press **Seal** when you're sure. Your first **Keeper Key** rises out of it.
4. **The Crystal Book Chamber.** Seven sealed Books that give the Library its life, three locks each. You spend your Key on the first lock and the Library takes her first breath. From then on the Chamber is a room you can walk back into whenever you have a Key in hand.
5. **Back to the Wing, on your own time.** The Wing keeps giving:
   - **The symbols.** A book on the collapsed shelf teaches each sign's symbol; then the wheel hides its names and asks you to find each symbol by its shape. **Key 2.**
   - **The second pattern.** Every third sign shares a *kind* — cardinal, fixed, or mutable — taught on the same wheel, three seats at a time.
   - **The table.** A board of four elements by three kinds wakes beside the wheel. Every sign has exactly one seat on it. Seat all twelve. **Key 3.**
   - **The last pattern.** Every sign has a *side* (day or night) and a partner straight across the wheel that shares its kind and its side but not its element. Then Caspar hands you parts — an element, a kind — and asks you to build the sign, find its partner, and say what the two share. Three signs built. **Key 4.**
   - **Practice, and your journal.** Once the wheel has taught you anything, stepping up to it gives you a choice: **Continue the lesson** or **Practice what you know**. Practice asks a few of the things you have learned, spaced further apart each time you remember them. Three misses in one practice and the instrument closes for a moment; you are back in the room, free to read your **journal** (a button in every room) or step straight back up and try again. The journal shows everything the wheel has shown you, as it stands. Reading it proves nothing; the wheel does that.
6. **Spend what you earn.** Every Key goes back to the Chamber. Keys 2 and 3 fill the first Book's remaining locks and it opens; Key 4 starts the second Book. Each Key spent wakes the Atrium one more step — lamps, the shelves taking their books back, light behind a sealed door — until the Wing is whole. That is the end of this build: **the Zodiac Wing, complete**, with two doors still sealed for another day.

## How Caspar teaches

- **You are never told the answer first.** You get one problem at a time. Get it wrong once and Caspar nudges. Wrong twice and he states the rule. Wrong three times and he shows you, then hands you a fresh one.
- **Ask Caspar for help** appears after your first miss on any problem that has a rule. Asking is never counted against you — but an answer you get after asking doesn't count as *yours*.
- **Keys are earned, not given.** Each Key needs at least one problem you solved with no help at all. If Caspar did all the work, he says so, and you try again another time.
- **Practice is optional and honest.** After you've earned a unit's Key, the book on the shelf will test you again — harder each clean run — and the wheel offers practice on what you know. Neither ever takes a Key away, and the journal is always there to read first.

## Controls

- **Phone:** tap and drag the wheel; tap a seat to jump to it; tap **Seal** to commit. In the rooms, tap a doorway, the desk, the table, the Books, or Caspar to walk there. Buttons under each room do the same thing as tapping. **Your journal** is a button in every room; **Leave the instrument** is on every practice item.
- **Keyboard:** left and right arrows turn the wheel one seat; Tab moves between controls; Enter or Space activates. Only Seal submits — selecting a seat never counts as an answer.
- **Screen readers:** every seat, cell, and button is labeled, and Caspar's lines are announced as they change.
- **Reduced motion:** honored from your system setting, and there's a toggle on the Dial. With it on, the wheel cuts instead of spinning and the walks skip to the door.

## Saving

Your progress saves on the device you're playing on, in the browser, the moment anything changes. Come back later and you'll be in the Atrium with your Keys and everything the wheel remembers. There is no account and nothing leaves your device. **Start over (test)** on the Atrium wipes it — that button is there for testing and will go away.

## What's placeholder, what's coming

- **Art:** placeholder blocks and lines. One illustrated look for the two rooms, Caspar, the Keeper, the Dial, and the book is on the plan. Every grey box is now a named slot that takes an image, so the art can arrive one file at a time (see below).
- **Sound:** none yet. One ambient loop and a few interface sounds are planned; the hooks are in, and each is a named slot that takes a sound file.
- **Caspar's voice:** the Wing's lines are now the writer's own, from the first return to the Chamber's end. The few stand-ins left are around practice and the journal (the fork, the gate, the journal's pages); they follow.
- **Next up:** Caspar's real lines, then art and sound for the Wing, then the rest of the Library — planets, houses, aspects, and the people who come to have their charts read.

The full game teaches the whole of beginner-to-intermediate natal astrology across eight stages and twenty-one Keys. The Zodiac Wing is stage one.

## How to drop art in

For the owner, and anyone helping with the look. Every placeholder has a named slot: a file named after the slot, dropped into the project, replaces the grey box with no code change; a sound file does the same for an action. The full list of slots, their sizes, and where each is drawn is in [`Documentation/CelestialDial/ART-SLOTS.md`](Documentation/CelestialDial/ART-SLOTS.md).

1. Name the file after the slot: `atrium.png`, `keeper-idle.png`, `door-open.png`; for sounds `step.wav`, `seal.wav`, `ambient.wav`.
2. Put it in `Assets/CelestialDial/Resources/Art/` (images) or `Assets/CelestialDial/Resources/Audio/` (sounds). Unity imports it with the right settings on its own.
3. Play. To see every slot at once with what is in it, open the **style page**: add `?style` to the game's address, or use the Editor menu *Ascendant › Greybox › Play the style page*. Add `?art=test` to the address to see the game with a test image in every slot, so you know where each one lands.

Until real files arrive nothing changes: with no files the game looks and sounds exactly as it does today.

## Feedback

If something feels wrong, confusing, or too easy, that is exactly what this build is for. The design notes and decisions live in the project's ClickUp workspace; the technical notes for anyone poking at the code are under [`Documentation/`](Documentation/README.md).

## Credits

Ascendant is designed and written by Davon G. The zodiac symbols are drawn with Noto Sans Symbols (SIL Open Font License, see [`Assets/CelestialDial/Resources/Fonts/OFL-NotoSansSymbols.txt`](Assets/CelestialDial/Resources/Fonts/OFL-NotoSansSymbols.txt)). Built with Unity.
