# Ascendant

*A game about learning to read the sky, set in a library that only answers to you.*

**Play it in your browser:** https://davonlemar30.github.io/Ascendant/ — nothing to install, works on a phone.

Ascendant teaches Western astrology the way a patient tutor would: one pattern at a time, on an instrument you turn with your own hands, with a guide who never lets you fake it. You are the Keeper, the last of a bloodline that built the Celestial Library. Caspar, its caretaker, has kept the lights on for centuries and cannot touch a single instrument. You can.

## What you are playing right now

This is an early, playable build — a **greybox**. That means the shapes, rooms, and instruments are real and the learning is real, but the art is placeholder blocks, the sounds are not in yet, and most of what Caspar says is a stand-in until the writer's lines land. Expect a museum at night rendered in charcoal and bone, not a finished painting.

The current build is **the Zodiac Wing**, the first wing of the Library. It holds one curriculum: the twelve signs, and how to *derive* what a sign is instead of memorizing twelve personalities.

**Your journey through the Wing:**

1. **Wake up.** You are asked your name and when you were born. From that the game finds your sun sign (or picks one for you if you don't know). Everything Caspar teaches starts from *your* sign.
2. **The Grand Atrium.** Dust, covered furniture, sealed doors, one weak candle. Caspar explains what he can. Every Key you earn lights one more lamp here.
3. **The Celestial Dial.** A great wheel of twelve seats that wakes when you step close. Caspar teaches the first pattern on it: the four elements, and how signs of one element sit evenly spaced around the wheel. You turn the wheel, count the seats, and press **Seal** when you're sure. Your first **Keeper Key** rises out of it.
4. **The Crystal Book Chamber.** Seven sealed books that give the Library its life. You spend your Key on the first lock and the Library takes her first breath.
5. **Back to the Wing, on your own time.** The Wing keeps giving:
   - **The symbols.** A book on the collapsed shelf teaches each sign's symbol; then the wheel hides its names and asks you to find each symbol by its shape. **Key 2.**
   - **The second pattern.** Every third sign shares a *kind* — cardinal, fixed, or mutable — taught on the same wheel, three seats at a time.
   - **The table.** A board of four elements by three kinds wakes beside the wheel. Every sign has exactly one seat on it. Seat all twelve. **Key 3.**
   - **The last pattern.** Every sign has a *side* (day or night) and a partner straight across the wheel that shares its kind and its side but not its element. Then Caspar hands you parts — an element, a kind — and asks you to build the sign, find its partner, and say what the two share. Three signs built. **Key 4.**

Four Keys is the whole Wing. The build ends there for now; spending those Keys in the Chamber is the next thing being built.

## How Caspar teaches

- **You are never told the answer first.** You get one problem at a time. Get it wrong once and Caspar nudges. Wrong twice and he states the rule. Wrong three times and he shows you, then hands you a fresh one.
- **Ask Caspar for help** appears after your first miss on any problem that has a rule. Asking is never counted against you — but an answer you get after asking doesn't count as *yours*.
- **Keys are earned, not given.** Each Key needs at least one problem you solved with no help at all. If Caspar did all the work, he says so, and you try again another time.
- **Practice is optional and honest.** After you've earned a unit's Key, the book on the shelf will test you again — harder each clean run — but it never takes a Key away.

## Controls

- **Phone:** tap and drag the wheel; tap a seat to jump to it; tap **Seal** to commit. In the rooms, tap a doorway, the desk, the table, or Caspar to walk there. Buttons under each room do the same thing as tapping.
- **Keyboard:** left and right arrows turn the wheel one seat; Tab moves between controls; Enter or Space activates. Only Seal submits — selecting a seat never counts as an answer.
- **Screen readers:** every seat, cell, and button is labeled, and Caspar's lines are announced as they change.
- **Reduced motion:** honored from your system setting, and there's a toggle on the Dial. With it on, the wheel cuts instead of spinning and the walks skip to the door.

## Saving

Your progress saves on the device you're playing on, in the browser, the moment anything changes. Come back later and you'll be in the Atrium with your Keys and everything the wheel remembers. There is no account and nothing leaves your device. **Start over (test)** on the Atrium wipes it — that button is there for testing and will go away.

## What's placeholder, what's coming

- **Art:** placeholder blocks and lines. One illustrated look for the two rooms, Caspar, the Keeper, the Dial, and the book is on the plan.
- **Sound:** none yet. One ambient loop and a few interface sounds are planned.
- **Caspar's voice:** most lines are stand-ins written in his tone. The real ones are being written.
- **Next up:** spending all four Keys in the Chamber (the Wing's ending), then art and sound, then the rest of the Library — planets, houses, aspects, and the people who come to have their charts read.

The full game teaches the whole of beginner-to-intermediate natal astrology across eight stages and twenty-one Keys. The Zodiac Wing is stage one.

## Feedback

If something feels wrong, confusing, or too easy, that is exactly what this build is for. The design notes and decisions live in the project's ClickUp workspace; the technical notes for anyone poking at the code are under [`Documentation/`](Documentation/README.md).

## Credits

Ascendant is designed and written by Davon G. The zodiac symbols are drawn with Noto Sans Symbols (SIL Open Font License, see [`Assets/CelestialDial/Resources/Fonts/OFL-NotoSansSymbols.txt`](Assets/CelestialDial/Resources/Fonts/OFL-NotoSansSymbols.txt)). Built with Unity.
