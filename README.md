# Ascendant

*A game about learning to read the sky, set in a library that only answers to you.*

**Play it now:** https://davonlemar30.github.io/Ascendant/ — nothing to install, and it works on a phone.

You are the Keeper, the last of a bloodline that built the Celestial Library. Caspar, its caretaker, has kept the lights on for centuries and cannot touch a single instrument. You can.

Ascendant teaches Western astrology the way a patient tutor would: one pattern at a time, on an instrument you turn with your own hands, with a guide who never lets you fake it.

## Where the game is

The first wing of the Library, **the Zodiac Wing**, is playable from start to finish. It holds one lesson: the twelve signs, and how to *derive* what a sign is instead of memorizing twelve personalities. The target is to have the Wing finished, polished, and sounding like itself by **November 13, 2026**.

The shapes, rooms, instruments, and learning are real. The Grand Atrium, the Zodiac Wing, and the Crystal Book Chamber are illustrated and wake up room by room as you earn Keys. Caspar, the Keeper, and the journal are illustrated too. The Celestial Dial has been redrawn and is being redrawn again, so expect it to change. The book on the shelf is still a plain block, and there is no sound yet.

## Your journey through the Wing

1. **Wake up.** Give your name and when you were born. The game finds your sun sign (or picks one if you don't know). Everything Caspar teaches starts from *your* sign.
2. **The Grand Atrium.** Dust, covered furniture, sealed doors, one weak candle. Every Key you spend in the Chamber wakes it one more step.
3. **THE CELESTIAL DIAL.** A great wheel of twelve seats that wakes when you step close. Turn it, count the seats, and press **Seal** when you're sure. Caspar teaches the four elements here, and how signs of one element sit evenly around the wheel. Your first **Keeper Key** rises out of it.
4. **The Crystal Book Chamber.** Seven sealed Books that give the Library its life, three locks each. Spend your Key on the first lock and the Library takes her first breath.
5. **Back to the Wing, on your own time.** It keeps giving:
   - **The symbols.** The Book of Symbols teaches each sign's glyph, then the wheel hides its names and asks you to find each one by its shape. **Key 2.**
   - **The modalities.** Every third sign shares a *modality* (cardinal, fixed, or mutable), taught on the same wheel, three seats at a time.
   - **THE ELEMENTAL TABLE.** Four elements by three modalities. Every sign has exactly one square. Place all twelve. **Key 3.**
   - **Polarity and opposites.** Every sign is Yang or Yin and has a partner straight across the wheel that shares its modality and polarity but not its element. Caspar hands you parts and you build the sign, find its partner, and say what the two share. **Key 4.**
6. **Spend what you earn.** Every Key goes back to the Chamber. Each one wakes the Atrium another step (lamps, shelves taking their books back, light behind a sealed door) until the Wing is whole, with two doors still sealed for another day.

## Practice and your journal

Once the wheel has taught you anything, stepping up to it offers a choice: **Continue the lesson** or **Practice what you know**. Practice asks a few things you have learned, spaced further apart each time you remember them. Three misses in one practice and the instrument closes for a moment. Read your journal, or step back up and try again.

The journal is a black book written in gold and silver. It opens on **the Wheel**: the twelve signs in their seats, silver when you have only met one, ringed in gold while you practise it, set in gold leaf once you have mastered it. A **Table** view lays the same signs out by element and modality, and tabs show them by element, modality, polarity, or as opposite pairs. Tap a sign for its page, with every sign that shares a fact one tap away. Each sign's picture starts as a bare line drawing and fills with colour as you practise. Reading the journal proves nothing; the wheel does that.

## How Caspar teaches

- **You are never told the answer first.** One problem at a time. Wrong once, Caspar nudges. Wrong twice, he states the rule. Wrong three times, he shows you and hands you a fresh one.
- **Ask Caspar** appears after your first miss on any problem that has a rule. Asking is never held against you, but an answer you get after asking doesn't count as *yours*.
- **Keys are earned, not given.** Each Key needs at least one problem you solved with no help at all. If Caspar did all the work, he says so, and you try again another time.
- **Practice is honest.** It never takes a Key away.

## Controls

- **Phone:** tap and drag the wheel; tap a seat to jump to it; tap **Seal** to commit. In the rooms, tap a doorway, the desk, the table, the Books, or Caspar to walk there. The buttons under each room do the same.
- **Keyboard:** left and right arrows turn the wheel one seat; Tab moves between controls; Enter or Space activates. Only Seal submits.
- **Screen readers:** every seat, cell, and button is labeled, and Caspar's lines are announced as they change.
- **Reduced motion:** honored from your system setting, with a toggle in Settings (the gear at the top right).

## Saving

Your progress saves in your browser the moment anything changes. Come back later and you are in the Atrium with your Keys and everything the wheel remembers. No account, and nothing leaves your device. **Start over**, under Testing in Settings, wipes it. Testing also has **Jump to...**, which loads a saved checkpoint so you can test any stage without replaying. Those buttons are for testing and will go away.

## What's next

- The book on the shelf, getting the same treatment as the rest of the Wing.
- The game's sounds: one ambient loop and a few interface sounds. The hooks are already in.
- Caspar's last few lines, around practice and the journal.
- Then the rest of the Library: planets, houses, aspects, and the people who come to have their charts read.

The full game teaches beginner-to-intermediate natal astrology across eight stages and twenty-one Keys. The Zodiac Wing is stage one.

## Feedback

If something feels wrong, confusing, or too easy, that is exactly what this build is for. Design notes and decisions live in the project's ClickUp workspace.

## For the owner and anyone helping with the look

Every placeholder is a named slot: a file named after the slot, dropped into the project, replaces the plain box with no code change (images into `Assets/CelestialDial/Resources/Art/`, sounds into `Assets/CelestialDial/Resources/Audio/`). The full list of slots, sizes, and where each is drawn is in [`Documentation/CelestialDial/ART-SLOTS.md`](Documentation/CelestialDial/ART-SLOTS.md). To see every slot at once, add `?style` to the game's address; add `?art=test` to see a test image in every slot. The technical notes for anyone poking at the code are under [`Documentation/`](Documentation/README.md).

## Credits

Ascendant is designed and written by Davon G. The zodiac symbols are drawn with Noto Sans Symbols (SIL Open Font License, see [`Assets/CelestialDial/Resources/Fonts/OFL-NotoSansSymbols.txt`](Assets/CelestialDial/Resources/Fonts/OFL-NotoSansSymbols.txt)), the journal's titles are set in UnifrakturMaguntia (SIL Open Font License, see [`Assets/CelestialDial/Resources/Fonts/OFL-UnifrakturMaguntia.txt`](Assets/CelestialDial/Resources/Fonts/OFL-UnifrakturMaguntia.txt)), and the Dial's words are set in EB Garamond (SIL Open Font License, see [`Assets/CelestialDial/Resources/Fonts/OFL-EBGaramond.txt`](Assets/CelestialDial/Resources/Fonts/OFL-EBGaramond.txt)). Built with Unity.
