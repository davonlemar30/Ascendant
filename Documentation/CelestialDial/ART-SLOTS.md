# Art slots and sound hooks

Every placeholder in the greybox is a **named slot**. A slot takes one file, named after it, dropped into a folder; the file is drawn in the placeholder's rect (or played on its action) and nothing else changes. No file, no change: the grey box and the silence stay. This is the door the art pass walks through: a folder of files, not a code change per piece, so the cost of each room can be measured.

The manifest lives in code, in [`Assets/CelestialDial/Slots.cs`](../../Assets/CelestialDial/Slots.cs) (`Slots.Art`, `Slots.Sounds`). That list is the source of truth; this page mirrors it. **Add a slot when a new object appears**, in the manifest first.

## How to drop art in

1. Name the file after the slot, exactly: `atrium.png`, `keeper-idle.png`, `door-open.png`.
2. Put it in `Assets/CelestialDial/Resources/Art/` (images, PNG) or `Assets/CelestialDial/Resources/Audio/` (sounds, WAV).
3. Open the project once so Unity imports it (the import settings below are applied automatically, and a name that is not a slot is flagged in the Console).
4. The light: each room also takes a transparent golden-hour overlay (`atrium-light.png`, `wing-light.png`, `chamber-light.png`, 360 × 800), drawn over the room and faded in by the Atrium's stage as the Library wakes. Draw the room dormant; draw its light separately.
5. Play. The file is in place. Open the style page (Editor menu **Ascendant › Greybox › Play the style page**, or `?style` on the web build) to see every slot at once with its source.

A file fills its placeholder's rect exactly (no letterboxing), so draw at the slot's proportions: a 360 × 800 room background, a 44 × 100 Keeper. Draw at twice the listed size for phone density (720 × 1600 for a room), never more than the slot's cap. Make both sides multiples of 4 so the texture compresses; anything else imports uncompressed and larger, and the Console says so. Transparent pixels are honoured (the Keeper and Caspar should be cut-outs).

**Masters (Platform fit, Part 2, Oct 2).** A room's background or the Dial's room (`dial-room`) can also take a master: 1200 × 1840 in the file (600 × 920 design units, inside the 2048 cap), dropped into the same slot, which `Bleed` (`Bleed.IsMaster`, by the file's 600:920 shape) draws whole, centred on the 360 × 800 column; a 720 × 1600 file still works as the stop-gap (the rules are in [VALIDATION.md](VALIDATION.md), Platform fit, Part 2). Each file is judged on its own. The masters landed on Oct 2 (batch 2): the Atrium, the Zodiac Wing and the Chamber with their light and grime layers (nine files, PR #106), and the Dial's room in all three looks (`dial-room`, `dial-room-worn`, `dial-room-bright`; PR #107, which also brought in the bright ring and its light and the small Dial's bright pair). A file that is not a master still gets the stop-gap. The Dial's glow (`dial-room-light` and its worn and bright looks) keeps its 720 × 1600 files: it is a radial fill over the column, which `Bleed` leaves alone, so a master would squeeze into the column. The Dial's looks in the tables below still read "until its file lands, today's … stands in". That is the manifest's wording for the fallback, which still holds if a file is removed; every one of those files is in.

Where the placeholder used color for state (a lit lamp, an open Book, a lit seat), the file is drawn at full brightness in that state and dimmed in the other; the tints are in `SliceView`/`DialView` next to the placeholder colors. Labels under the props (“Shelves, mostly empty”) stay until the owner says otherwise; whether they go once art arrives is an open owner call.

## Image slots

Sizes are the placeholder's rect on the 360 × 800 reference layout; the cap is the imported texture's longest side. This table holds the slots that are not kit pieces; the kit pieces, the doors and plates, and the three grime layers have their sizes in the tables in the kit sections below.

| Slot | Size | Cap | Where it is drawn |
| --- | --- | --- | --- |
| `atrium` | 360 × 800 | 2048 | the Grand Atrium, behind the opening, the return, and the room; since Build Q the domed Atrium's restored shell: the dome, the dark wood shelves and balcony, the statue's niche, the empty doorways |
| `wing` | 360 × 800 | 2048 | the Zodiac Wing's shell (Build M): the restored room's architecture only; the kit pieces, grime, and light layer over it (see the kit section below). Build L had painted the Dial, the table, the chair and the shelf into this file; since Build M they are separate kit pieces |
| `chamber` | 360 × 800 | 2048 | the Crystal Book Chamber, first visit and room |
| `caspar` | 64 × 112 | 256 | Caspar standing in the Atrium |
| `keeper-idle` | 44 × 100 | 256 | the Keeper standing; flipped to face the way it last walked |
| `keeper-walk` | 44 × 100 | 256 | the Keeper mid-step; alternates with idle every step while walking (either frame alone serves for both) |
| `dial-face` | 332 × 332 | 1024 | the wheel alone, cut from the Dial room (Build Z): the Wing room's fallback Dial when its kit piece has no file (200 × 200); the Dial screen draws `dial-room` instead |
| `dial-room` | 360 × 800 | 2048 | the Dial screen's background (Build Z; Build AB; Build AC, owner Oct 1: the Cast Dial): Room A behind the Dial, the stand, the thin bronze rim with its ticks and its four diamonds, the hub ring, and the still hub: the phoenix holding a cast ribbon above the eye, the eye, its tail below; the turning ring (`dial-ring`) covers the band between the hub ring and the rim |
| `dial-room-light` | 360 × 800 | 2048 | the Dial's fixed glow over `dial-room` (Build Z; Build AC): the rim, the diamonds, the hub ring, the ribbon's and the phoenix's bright brass; clear over the turning ring; off while the Dial sleeps, swept in by the reveal |
| `dial-ring` | 360 × 360 | 1024 | the Cast Dial's turning ring (Build AC, owner Oct 1), centred on the wheel: twelve identical segments, each a cast name recess (faces r 136–155) and a seat window in its spoke (faces r 91–127), a rivet at each junction; clear outside r 156 and inside r 82; it turns with the seats |
| `dial-ring-light` | 360 × 360 | 1024 | the turning ring's glow (Build AC): the recesses' and windows' bevels, the spokes' enamel lines; turns with the ring, off while the Dial sleeps |
| `dial-room-worn` | 360 × 800 | 2048 | the Dial's room in its worn look: the dust and cobwebs, the tarnished bronze and a dull glow of the first visit (the wake-up, owner Oct 1; 86bcbn6w6 2a A, 2b B, 2c A to C): dial-room's exact canvas, centre and radii, the eye, the phoenix's outline, the room and the stand as today; until its file lands, today's dial-room stands in |
| `dial-room-light-worn` | 360 × 800 | 2048 | dial-room-light for the worn look (the wake-up): registered to dial-room-worn; until its file lands, today's dial-room-light stands in |
| `dial-ring-worn` | 360 × 360 | 1024 | the turning ring in the worn look (the wake-up): dial-ring's twelve identical segments and radii, cut from one segment so the twelve stay exact; until its file lands, today's dial-ring stands in |
| `dial-ring-light-worn` | 360 × 360 | 1024 | dial-ring-light for the worn look (the wake-up): turns with the ring; until its file lands, today's dial-ring-light stands in |
| `dial-room-bright` | 360 × 800 | 2048 | the Dial's room in its bright and new look: polished bronze catching light, the recesses and windows glowing stronger than today's, the outer glow held back (owner, Oct 1: polished bronze, not lit from inside) (the wake-up, owner Oct 1; 86bcbn6w6 2a A, 2b B, 2c A to C): dial-room's exact canvas, centre and radii, the eye, the phoenix's outline, the room and the stand as today; until its file lands, today's dial-room stands in |
| `dial-room-light-bright` | 360 × 800 | 2048 | dial-room-light for the bright look (the wake-up): registered to dial-room-bright; until its file lands, today's dial-room-light stands in |
| `dial-ring-bright` | 360 × 360 | 1024 | the turning ring in the bright look (the wake-up): dial-ring's twelve identical segments and radii, cut from one segment so the twelve stay exact; until its file lands, today's dial-ring stands in |
| `dial-ring-light-bright` | 360 × 360 | 1024 | dial-ring-light for the bright look (the wake-up): turns with the ring; until its file lands, today's dial-ring-light stands in |
| `seat` | 52 × 52 | 256 | one seat's cap, twelve times (Build Z): the greybox fallback, used only when the Dial has no turning ring (no `dial-ring` file, or no `dial-room` to hold it); with the Cast Dial's `dial-ring` each seat is its segment's name recess and window |
| `bracket` | 72 × 88 | 256 | the fixed frame over the framed seat (Build AC: a gold wedge outline over the 9 o'clock recess and window, under the rim's diamond) |
| `floor-markings` | 320 × 320 | 1024 | the faded floor pattern under the wheel; brightens as the wheel wakes |
| `shelf` | 50 × 36 | 256 | the collapsed bookshelf, in the Wing room and beside the wheel |
| `chair` | 44 × 36 | 256 | the covered chair beside the wheel (its dust cloth goes) |
| `table` | 60 × 30 | 256 | the table with its board of twelve, in the Wing room (the twelve squares go) |
| `shelf-book` | 10 × 26 | 64 | a book on a shelf: three on the Wing's, three that return to the Atrium's at Stage 4 |
| `book-cover` | 140 × 140 | 512 | the book on the shelf, closed |
| `book-page` | 140 × 140 | 512 | the book's open page behind each symbol |
| `book-room` | 360 × 800 | 2048 | the Book of Symbols' screen: the Book open on a carved lectern in the Zodiac Wing's moonlight, two candles; behind today's layout (owner, Oct 7, 86bcex5kc 4A; 86bcf0x71) |
| `table-room` | 360 × 800 | 2048 | the Elemental Table's screen: the Zodiac Wing's own lectern table from above, alive (owner's pick, Oct 8): gold inlay glowing from within, blackened iron fittings, a carved scorpion and phoenix on its header, dark mist; twelve slate squares on the cells, a wide left rail and header for the names, a tray on its front lip (86bcf0x71) |
| `table-well` | 86 × 48 | 256 | one slate square, cut from table-room's centre square and drawn at each of the twelve, so all twelve are identical; empty |
| `table-plate` | 82 × 44 | 256 | a plain pale wooden plate, one per sign (owner's pick, Oct 8); the game burns its symbol and name in |
| `table-plate-gold` | 82 × 44 | 256 | the same plate turned gold, when a sign is placed on the player's own (owner, Oct 7) |
| `letter-fire` | 128 × 128 | 512 | a tileable fill of drawn flames and embers for the Table's live lettering (Shaders/LetterFill) |
| `letter-earth` | 128 × 128 | 512 | a tileable fill of drawn moss and stone for the Table's live lettering |
| `letter-air` | 128 × 128 | 512 | a tileable fill of drawn wind and cloud for the Table's live lettering |
| `letter-water` | 128 × 128 | 512 | a tileable fill of drawn deep water for the Table's live lettering |
| `emblem-aries` | 180 × 180 | 512 | the Aries symbol as a living emblem of fire, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-taurus` | 180 × 180 | 512 | the Taurus symbol as a living emblem of earth, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-gemini` | 180 × 180 | 512 | the Gemini symbol as a living emblem of air, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-cancer` | 180 × 180 | 512 | the Cancer symbol as a living emblem of water, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-leo` | 180 × 180 | 512 | the Leo symbol as a living emblem of fire, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-virgo` | 180 × 180 | 512 | the Virgo symbol as a living emblem of earth, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-libra` | 180 × 180 | 512 | the Libra symbol as a living emblem of air, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-scorpio` | 180 × 180 | 512 | the Scorpio symbol as a living emblem of water, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-sagittarius` | 180 × 180 | 512 | the Sagittarius symbol as a living emblem of fire, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-capricorn` | 180 × 180 | 512 | the Capricorn symbol as a living emblem of earth, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-aquarius` | 180 × 180 | 512 | the Aquarius symbol as a living emblem of air, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `emblem-pisces` | 180 × 180 | 512 | the Pisces symbol as a living emblem of water, raised by the Book once it is answered (owner, Oct 8: the set approved) |
| `journal-page` | 360 × 800 | 2048 | the journal's page (Build F; Build AA, Sept 30: the Black Hours: one tall black-blue vellum page in the black book, gold corner flourishes, a calm field; painterly by the owner's journal-only exception; no rules, the engine draws the silver rules and the margin) |
| `journal-cover` | 60 × 60 | 256 | the journal's closed cover: black leather, the gold wheel inlay (Build F; Build AA) |
| `journal-ribbon` | 16 × 120 | 256 | a faded vermilion silk ribbon bookmark, forked tail at the bottom; nine-sliced to the ladder's length on a sign page, a short tab on a seat when due |
| `journal-wheel` | 300 × 300 | 1024 | the journal's index: the gold wheel with twelve empty sockets on radius 216 of 600 (socket 1 at 9 o'clock), spokes between them, the rosette; transparent outside the silver ring (Build AA) |
| `journal-seat-leaf` | 56 × 56 | 256 | a mastered seat's raised gold-leaf ring with a soft glow, clear inside (Build AA) |
| `journal-seat-line` | 56 × 56 | 256 | a plain pale ring the engine tints silver (met) or gold (practising), clear inside (Build AA) |
| `journal-door-frame` | 238 × 120 | 512 | a door on the journal's landing (Practice, Contents): the fine gold hairline frame of the approved round-2 art, its corner flourishes whole; nine-sliced (92 px in at 2x) to the door; the game draws the vellum panel inside it |
| `journal-emblem-practice` | 56 × 72 | 256 | the Practice door's emblem: a closed black book with a gold sun and a faded vermilion ribbon (the approved board) |
| `journal-emblem-contents` | 76 × 60 | 256 | the Contents door's emblem: an open book in polished gold, a gold star on each page |
| `journal-emblem-wheel` | 48 × 48 | 256 | The Wheel's row on Contents: the journal's twelve-seat wheel (journal-wheel) at emblem size, a touch brighter; the game rings it in gold |
| `journal-emblem-map` | 40 × 40 | 256 | The Library Map's row on Contents: the Library's parchment plan folded in three |
| `journal-emblem-lock` | 24 × 28 | 256 | a Sealed chapter's lock on Contents, dark iron, dim; the game rings it in grey |
| `journal-flourish` | 220 × 20 | 1024 | the gold flourish rule: a hairline each side of an eight-point star, two dots, diamond ends; above and below the Keeper's record, under Contents' title, on the title page |
| `journal-library-plan` | 234 × 350 | 1024 | The Library Map's page: the Library's parchment plan (the mini-menu concept's Option C, moved into the journal, owner Oct 1): the round Grand Atrium, the Zodiac Wing top left, the Crystal Book Chamber top right; the game names the rooms you have woken |
| `sign-aries` | 200 × 200 | 512 | Aries's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-taurus` | 200 × 200 | 512 | Taurus's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-gemini` | 200 × 200 | 512 | Gemini's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-cancer` | 200 × 200 | 512 | Cancer's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-leo` | 200 × 200 | 512 | Leo's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-virgo` | 200 × 200 | 512 | Virgo's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-libra` | 200 × 200 | 512 | Libra's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-scorpio` | 200 × 200 | 512 | Scorpio's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-sagittarius` | 200 × 200 | 512 | Sagittarius's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-capricorn` | 200 × 200 | 512 | Capricorn's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-aquarius` | 200 × 200 | 512 | Aquarius's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `sign-pisces` | 200 × 200 | 512 | Pisces's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination) |
| `shelves` | 60 × 180 | 512 | the Atrium's shelves, mostly empty (60 × 120 in the room) |
| `furniture-covered` | 120 × 70 | 512 | the covered furniture of the opening (its dust cloth goes) |
| `desk` | 70 × 30 | 256 | the desk, uncovered, in the Atrium room |
| `lamp` | 8 × 22 | 64 | a wall lamp, four in the Atrium; dark until its stage |
| `candle` | 8 × 20 | 64 | a candle: the opening's one and the Chamber's nine; dark until lit (the Wing's, beside the Dial's step hint, was cut in Build Z: it read as "!", owner, Sept 30) |
| `door-open` | 64 × 128 | 512 | an open door leaf filling a painted arch: the Wing's (72 × 128) and the Chamber's (62 × 128) in the Atrium, the doorways back (36 × 140 in the Wing, 30 × 124 in the Chamber); no frame of its own, the painted arch is the frame; the doorway glows stay |
| `door-sealed` | 64 × 128 | 512 | the sealed door leaf filling the Atrium's left arch (62 × 128); no frame of its own; the light behind it stays |
| `mechanism` | 110 × 110 | 512 | the Chamber's old mechanism; turns one degree at the first Key |
| `crystal-book` | 34 × 70 | 256 | one Crystal Book, seven times; brightens when it opens |
| `crystal-page` | 26 × 58 | 256 | the page that rises from an open Book |
| `lock` | 7 × 7 | 32 | one lock, three per Book; lights when filled |
| `keeper-key` | 84 × 40 | 256 | the Keeper Key rising from the Dial (the “KEEPER KEY” label goes) |
| `atrium-light` | 360 × 800 | 2048 | the Atrium's golden-hour light (Build H): shafts through the arches, glowing dust, warmth on the stone; a transparent overlay drawn over the background and under everything else, faded by the Atrium stage (nothing at Stage 1, a quarter at 2, half at 3, three quarters at 4, full at 5 and 6) |
| `wing-light` | 360 × 800 | 2048 | the Zodiac Wing room's golden-hour light, the same fade |
| `chamber-light` | 360 × 800 | 2048 | the Crystal Book Chamber's golden-hour light, the same fade |
| `chat-box` | 324 × 240 | 1024 | the chat box (Build P; since Build R also on the Atrium hub and the Chamber, while the Dial, the Book of Symbols and the Elemental Table draw a slim box in code): the dark see-through fill and the gold frame in one image, imported sliced (borders 56, 64, 56, 56 at the file's 2x) so each panel keeps the corner stars whole at its own height |
| `chat-plate` | 170 × 22 | 512 | the speaker's plate on the chat box's top-left edge (Build P); blank, the game writes the name |
| `btn-plate` | 146 × 56 | 512 | the bronze plate: an action on an instrument (the Dial, the Elemental Table, the Book of Symbols, practice); its frame without its centre marks, nine-sliced (40 px in at 2x) to each button; the game engraves the word |
| `btn-plate-notch` | 12 × 12 | 256 | the plate's top notch, laid at its top centre so the slice never stretches it |
| `btn-plate-diamond` | 16 × 18 | 256 | the plate's bottom diamond, laid at its bottom centre, 1 px above its edge |
| `btn-arrow` | 64 × 56 | 512 | Previous on an instrument, a bronze arrow (3b C: engraved arrows); Next is it mirrored; no word |
| `btn-rule-left` | 100 × 14 | 512 | the way out of an instrument, gold lettering on a rule (3b C): the rule's left line; its tapered end (12 px) keeps its shape and the line stretches |
| `btn-rule-right` | 100 × 14 | 512 | the rule's right line, its tapered end at the right |
| `btn-rule-centre` | 16 × 14 | 256 | the rule's centre: its diamond and the gaps beside it, at the rule's centre |
| `caspar-calm` | 264 × 468 | 1024 | Caspar from the waist up behind the chat box (Build P), calm: the default, short instructions |
| `caspar-explain` | 264 × 468 | 1024 | Caspar behind the chat box, explaining: teaching, the lessons, the history (the opening's fourth page, the return's second) |
| `caspar-warm` | 264 × 468 | 1024 | Caspar behind the chat box, warm: welcomes, thanks, praise (the opening's second page) |
| `caspar-wry` | 264 × 468 | 1024 | Caspar behind the chat box, wry: his dry humour (the opening's first page) |
| `caspar-moved` | 264 × 468 | 1024 | Caspar behind the chat box, moved: wonder at the Library waking (the return's first page, the Keeper Key) |
| `caspar-solemn` | 264 × 468 | 1024 | Caspar behind the chat box, solemn: honesty and the backstory (the opening's third page) |
| `prologue-city` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 1, the city: a New York street at 3 a.m. (shown by the picture only, no clock or text), empty, one lit window high on a building; the push-in holds on the window (measured on the final art: 58.1% across, 23.7% down) |
| `prologue-desk` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene, shown as a framed comic panel: a window on part of the frame (SliceView.PrologueShots sets which part, from the art); shot 2's first panel (on screen 54 to 354 down, showing the frame from 22.5% to 60% down): wide on the Keeper at his desk in his room, headphones on, writing; shot 3 opens on the whole frame, then crossfades to prologue-light, so the two line up |
| `prologue-notebook` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene, shown as a framed comic panel: a window on part of the frame (SliceView.PrologueShots sets which part, from the art); shot 2's second panel (on screen 374 to 674 down, showing the frame from 34% to 72% down; measured on the Oct 9 redraw: the pages 42.6% to 61.8%, the book's edge about 64%, the pen 39.5% to 51.1%, its tip 50.7%): close on his hand writing in the plain notebook (D2's left half) |
| `prologue-notebook-2` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene, shown as a framed comic panel: a window on part of the frame (SliceView.PrologueShots sets which part, from the art); shot 2's writing loop (the owner, Oct 9: slight animations, frame swaps within ruling 4): prologue-notebook with his hand and pen moved on along the line, the same framing; the panel swaps notebook, notebook-2, notebook-3 at 4 frames a second |
| `prologue-notebook-3` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene, shown as a framed comic panel: a window on part of the frame (SliceView.PrologueShots sets which part, from the art); shot 2's writing loop (the owner, Oct 9: slight animations, frame swaps within ruling 4): the third step of the hand writing, the same framing as prologue-notebook |
| `prologue-light` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 3: the desk frame again, the same framing as prologue-desk, white-gold light flooding the room and his page from the window |
| `prologue-light-up` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 3 (the owner, Oct 9: slight animations, frame swaps within ruling 4): prologue-light with his head lifted toward the window; a quick 0.15 s crossfade from prologue-light makes the head turn |
| `prologue-look` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 3: close on his eyes looking up toward the light, headphones still on |
| `prologue-headphones-mid` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 3 (the owner, Oct 9: slight animations, frame swaps within ruling 4): the close of prologue-look with the headphones halfway down; look, mid, headphones swap 0.14 s apart (reduced motion skips it) |
| `prologue-headphones` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 3: the same close as prologue-look, his hand pulling the headphones down (a frame swap: the two must line up) |
| `prologue-street-above` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 4: looking down from his window at the empty street, Caspar small below with his back turned in a fading ring of light; the pan runs from the top of the frame to the bottom and a little left, ending with him and the whole ring in view (measured: 47.5% across, 69% down) |
| `prologue-puzzled` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene, shown as a framed comic panel: a window on part of the frame (SliceView.PrologueShots sets which part, from the art); shot 5's panel (on screen 205 to 595 down, over shot 4 dimmed, showing the frame from 17% to 66% down): his face at the window, puzzled |
| `prologue-caspar-back` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 6: street level, Caspar in his canon coat with the hood of his own navy coat fabric and its brass clasp (B1), his back to us; the turn trio must line up |
| `prologue-caspar-turn` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 6: Caspar turned three-quarter toward us, the same framing as prologue-caspar-back |
| `prologue-caspar-face` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 6: Caspar facing us, the same framing as prologue-caspar-turn, his eyes glowing white-gold from the crossing (ruling 2: this scene only, the eyes only; the owner, Oct 8: the glow does not fade); a slow push-in toward his eyes starts on it (measured: 56.6% across, 21.3% down) |
| `prologue-caspar-eyes` | 360 × 800 | 2048 | a full 360 × 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 6's end (the owner, Oct 8: the intro ends zooming into his glowing eyes, then a flash into nothingness): an extreme close-up of Caspar's glowing white-gold eyes under the hood; the game crossfades to it from prologue-caspar-face mid push-in and keeps pushing in on the glow (measured: 50.7% across, 47.1% down), then the white blooms out of the glow |
| `orb` | 120 × 120 | 512 | the orb (C2: dark stone with gold seams), cut out to transparency, floating over the question on the opening's name and birth screens with a gentle bob (none under reduced motion); it has no name, lore or lines (ruling 3) |
| `orb-star` | 16 × 16 | 64 | one faint star, cut out to transparency: one fades in around the orb for each step answered in the opening, eight places at most; they leave with the white light |
| `studio-logo` | 320 × 320 | 1024 | the logo screen at every launch (owner, Oct 9): TSG Games' Signature Crown Mark on black, centred on the black screen and drawn whole inside the box (its shape kept); it fades in, holds about 2 s and fades out |
| `intro-stars` | 360 × 800 | 2048 | a full 360 × 800 frame of the launch movie (owner, Oct 9; the opening's painterly look): the one painted starry sky the wheel and Earth layers turn and change over; it fades in behind the wheel as it comes alive (round 4: the wheel and Earth are cut-out layers over one sky) |
| `intro-wheel-pencil-lines` | 360 × 360 | 1024 | a launch-movie layer (round 4), 720 × 720 in the file with transparency, centred on the screen (the disc's outer radius 306 of the 720): the zodiac wheel's pencil lines (rings and spokes), no symbols; it draws itself, revealed by a radial sweep (the owner: "its own classic unique zodiac wheel"; "make it look like its drawing itself") |
| `intro-wheel-pencil-glyphs` | 360 × 360 | 1024 | a launch-movie layer, framed as intro-wheel-pencil-lines: the twelve symbols in pencil only, which appear one at a time in zodiac order after the lines are drawn |
| `intro-wheel-lit` | 360 × 360 | 1024 | a launch-movie layer, framed as the pencil layers: the wheel come alive, its lines joined by light and its symbols glowing softly |
| `intro-wheel-burning` | 360 × 360 | 1024 | a launch-movie layer, framed the same: the wheel burning alive and glowing; it starts to spin, and in the transform spins on and fades away, its ring of fire on Earth's rim (both radius 306 of the 720) |
| `intro-earth` | 360 × 360 | 1024 | a launch-movie layer, framed the same: the calm Earth, whole, where the wheel was; it fades in level under the burning wheel as that spins away (it never turns), then a slow push-in |
| `intro-continents` | 360 × 800 | 2048 | a full 360 × 800 frame of the launch movie; shot 7, flying down: the continents; a push-in toward the centre (the target is measured again on the art) |
| `intro-america` | 360 × 800 | 2048 | a full 360 × 800 frame of the launch movie; shot 8: America; a push-in toward the centre |
| `intro-newyork-state` | 360 × 800 | 2048 | a full 360 × 800 frame of the launch movie; shot 9: New York State; a push-in toward the centre |
| `intro-newyork-city` | 360 × 800 | 2048 | a full 360 × 800 frame of the launch movie; shot 10: New York City (the owner: night all the way down the Brooklyn descent); a push-in toward the centre |
| `intro-brooklyn` | 360 × 800 | 2048 | a full 360 × 800 frame of the launch movie; shot 11: Brooklyn at night; a push-in toward the centre |
| `intro-block` | 360 × 800 | 2048 | a full 360 × 800 frame of the launch movie; shot 12: apartment buildings in a semi-busy neighborhood at night; then shot 13 ends on prologue-city (one building, every window dark but one), and the movie fades to the menu |
| `menu-city` | 360 × 800 | 2048 | the main menu's background (owner, Oct 9: M1b, the night city recomposed as a title screen, the street of prologue-city); room for the title near the top and the four buttons from 496 down; no text in the picture |
| `menu-title` | 270 × 196 | 1024 | the main menu's title (owner, Oct 9; approved in round 4): the owner's ASCENDANT "Library Seal" logo, landscape (about 1.38:1 trimmed), its black ground keyed out to transparency, drawn whole inside the box (its shape kept), about 270 wide, centred 115 down (below any cutout's band), its top about 16 px clear of the safe area |
| `menu-rat-1` | 48 × 24 | 256 | the menu's rat, run frame 1 of 4 (extended; then gathering, bunched, pushing off): a small rat in side view facing right, cut out to transparency (96 × 48 in the file), its feet on a line shared by the four frames (47 of 48 down) and its body centred the same in each, so the cycle doesn't slide; it runs across the street now and then while the menu is idle, about 12 frames a second, flipped when it runs left |
| `menu-rat-2` | 48 × 24 | 256 | the menu's rat, run frame 2 of 4, framed as menu-rat-1 |
| `menu-rat-3` | 48 × 24 | 256 | the menu's rat, run frame 3 of 4, framed as menu-rat-1 |
| `menu-rat-4` | 48 × 24 | 256 | the menu's rat, run frame 4 of 4, framed as menu-rat-1 |

### The Wing room kit (Build M, Sept 25)

The journal is the Black Hours (Build AA, owner Sept 30; it replaces Build J's crimson ring binder). One tall page of black-blue vellum written in gold and silver, painterly by the owner's journal-only exception to the Art Bible; the style reference is `Documentation/ConceptArt/JournalMockup/journal-black-hours-v3-2-silver-ruled.png`. Draw `journal-page` at 720 × 1600: the page runs x 24–696, y 32–1232, its calm field x 72–648, y 160–1100 (nothing there: the engine draws the silver rules 26 px apart from y 96 and the faded vermilion margin, `RuleFirst` and `RuleGap` in `SliceView`), and below y 1260 a quiet dark desk where the buttons sit. `journal-wheel` is drawn at 600 × 600 with its twelve sockets on radius 216 (socket 1 at 9 o'clock, counter-clockwise, as on the Dial) and shown at 270 px (`WheelSize`), so a socket is 41 px; the layout guides are in `Documentation/ConceptArt/JournalMockup/production-2026-09-30/`. The seat rings are 112 × 112 files: `journal-seat-leaf` (mastered) is drawn as painted, `journal-seat-line` is near-white so the engine can tint it silver (met) or gold (practising). Each `sign-*` picture is **one full-colour file on a transparent background** (400 × 400), clipped round in a seat and on its page. The engine draws it as line art and brings its colour up as the player learns the sign (Illumination, the `Ascendant/Illumination` shader in `Resources/Shaders`). The ribbon is nine-sliced (its top 6% and bottom 20% keep their shape), so keep its middle a plain band.

**The journal's front pages (batch 2, owner Oct 1: the journal's architecture; the approved board on task 86bcbn6w6).** The landing, Contents and the Library Map wear eight pieces cut from the board's own sources (`Documentation/ConceptArt/BatchRun1-2026-10-02/1-journal/`). `journal-door-frame` is the round-2 art's fine gold hairline frame at 476 × 240, nine-sliced 92 px in (its corner flourishes stay whole at any size); the game draws the vellum panel inside it, so keep its middle clear. The emblems are 3x their boxes (`journal-emblem-practice` 168 × 216, `-contents` 228 × 180, `-wheel` 144 × 144, `-map` 120 × 120, `-lock` 72 × 84) on transparent grounds; the game rings each Contents emblem itself (gold, or grey when Sealed). `journal-flourish` is the board's rule at 3x (660 × 60), centred on its star. `journal-library-plan` is the mini-menu concept's Option C parchment plan at its own 2x (468 × 700); the game writes the rooms' names on it, the Grand Atrium in the round hall under its star, the Zodiac Wing in its lower half and the Crystal Book Chamber under its crystal, so a redraw keeps those floors plain.

The Zodiac Wing is built as a kit (owner, Sept 24): `wing` is the restored **shell** (architecture only), `wing-grime` (360 × 800, cap 2048) is the dust, cobwebs, and cold tint over it that fade out by Keys, and each **piece** has a worn and a restored file, `kit-<piece>-worn` / `kit-<piece>-restored`. Piece files are drawn at **half their pixel size**, bottom-centred on their placement in `SliceView.WingKit` (x, bottom, the Key that restores it, a scale); the sizes are in the table after this paragraph. The pieces: `window`, `carpet`, `chandelier`, `banner` (three placements), `orrery`, `armillary`, `lectern`, `globe`, `shelf`, `dial`, `telescope`, `table`, `candles` (two placements), `books`, `chair`, `plate` (the doorway's name plate; the engine writes the name on the restored one). A piece restores on its Key, except the shelf (when the book of symbols wakes: the wheel lit) and the table (when it wakes: the modalities complete), because their lessons happen on them.

Sizes on the 360 × 800 layout (draw at twice that), cap 1024 for every piece. The dial's eye-open files, `kit-dial-worn-open` and `kit-dial-restored-open`, are the same size as its closed pieces (see *The Wing Dial's eye*, below).

| Piece | Restored | Worn |
| --- | --- | --- |
| `window` | 76 × 192 | 76 × 192 |
| `carpet` | 352 × 110 | 352 × 110 |
| `chandelier` | 84 × 106 | 84 × 106 |
| `banner` | 26 × 130 | 26 × 130 |
| `orrery` | 80 × 106 | 80 × 106 |
| `armillary` | 66 × 80 | 66 × 80 |
| `lectern` | 76 × 90 | 76 × 90 |
| `globe` | 34 × 50 | 34 × 50 |
| `shelf` | 96 × 290 | 96 × 290 |
| `dial` | 188 × 206 | 186 × 200 |
| `telescope` | 66 × 116 | 66 × 116 |
| `table` | 108 × 90 | 108 × 90 |
| `candles` | 16 × 40 | 16 × 40 |
| `books` | 68 × 46 | 68 × 46 |
| `chair` | 50 × 82 | 50 × 82 |
| `plate` | 64 × 28 | 64 × 28 |

The dial has a third state for the Dial's wake-up (owner, Oct 1; 86bcbn6w6 2d A): `kit-dial-bright` and `kit-dial-bright-open`, 188 × 206 like the restored pair. The small Dial follows the Dial screen's five steps: worn, half today's over it (Key 1), restored (Key 2), half bright over it (Key 3), bright (Key 4). The bright pair landed on Oct 2 (PR #107), and `kit-dial-worn` and `kit-dial-worn-open` were redrawn to the worn pass in the same slots, so the restored pair no longer stands in for it.

Not slots, on purpose (except the story screens' chat box and plate, Build P): the Caspar panels, buttons, and text (interface, not placeholder art); the glows (doorway light, shelf and table glow, the Key's glow, the seam) and the fade, which are effects drawn over whatever is there; the Dial screen's charcoal backdrop; the table's board of cells and tiles, which is a control.

### The chat box and Caspar's poses (Build P, Sept 28)

The owner's B2 ruling (Sept 28, note 3): on the story screens (the Grand Atrium opening and the return with the first Keeper Key) Caspar stands unframed behind a chat box, from the waist up, in one still pose per page. `chat-box` is drawn at the box's rect (324 × 240, centre 600 on the 360 × 800 layout); `chat-plate` sits on its top-left edge and the game writes CASPAR on it; the line is left-aligned in the box and Continue sits inside it. The six `caspar-<pose>` files share one registration: each is the whole pose canvas, scaled together, so the head lands in the same place in every pose and a swap does not jump; the game draws each at 264 × 468 centred at (5, 408). The pose per page is `SliceView.AtriumPoses` (wry, warm, solemn, explain) and `SliceView.ReturnPoses` (moved, explain), the working choice recorded on the Decisions Log (Sept 28). No `chat-box` file, no change: the old panel and Continue stay where they were. No pose file, no figure.

**The Astrolabe (Build AB, owner Sept 30; superseded by Build AC below, never deployed).** The Dial is drawn in layers so its words always fit and its ring can turn. `dial-room` carries everything that stays put (the room, the rim with its degree ticks, the brass pointer at 9 o'clock, the phoenix and the eye); `dial-ring` is a 720 × 720 file centred on the wheel holding the twelve-segment name band and the twelve tablets, with one segment centred at 9 o'clock, and it turns with the seats; `dial-ring-light` and `dial-room-light` split the glow the same way. The engine writes each name along its segment of the name band (EB Garamond, bent by `ArcText`), and each tablet's symbol and its one fact along the ring, upright on the lower half; so the bands must stay plain where the words go. The medallion inside the ring carries the words' other seats, measured on the art: a plaque above the eye (face 150 × 34 at 105, 215.5: the framed sign's name and facts), the eye's glass (about 160 × 36 at y 270: the challenge), and a small plaque below (64 × 17 at 147, 316: the count); a redraw must keep those faces plain and where they are (`DialView.NamePlate`, `CountPlate`, `EyeBox`). The Wing room's `kit-dial-*` files are the same Astrolabe at room size. Radii on the 360 × 800 layout: name band 138–155 (names at 146.5), tablet faces 97–131 (symbol at 120.5, fact at 104.5), `DialView.NameRadius` / `TabletRadius`. The final concept and its cut are in `Documentation/ConceptArt/DialRedesign-2026-09-30/final/`. Superseded: Build AC replaced the medallion and its plaques, the tablets and their radii with the Cast Dial below (`DialView.NamePlate` and `CountPlate` no longer exist); what carries over is the layer split, the turning `dial-ring` with one segment centred at 9 o'clock, `ArcText`, and the `kit-dial-*` files being the Dial's own wheel at room size.

**The Cast Dial (Build AC, owner Oct 1).** The owner held the Astrolabe as a backup and chose a reimagined Dial, one cast bronze object (concept A, the ribbon hub, no count field): the twelve names in cast recesses on the rim band, each seat a window in its spoke, the still hub the phoenix holding a cast ribbon above the eye. The layers are the same as the Astrolabe's. `dial-ring` was cut from one segment of the approved reference (`Documentation/ConceptArt/DialRedesign-2026-10-01/dial-A3-ribbon.png`, main checkout, untracked) resampled to an exact 30° wedge and repeated twelve times, its spoke zones compressed so the recess faces reach 26.5° and the window faces 23.5°, with a round rivet pasted at each junction; the hub ring is lathed from one clean angle; the drawn rim stays. The framed sign's name and facts curve along the ribbon (`ArcText`, the name's arc on radius 117.5 on the layout, the facts 11 px inside it, one centre); the challenge sits in the eye's glass (150 × 33 at y 267); while a count runs it folds onto one line (10 to 12 px) and the count word takes the line under it. The Wing room's `dial` kit pieces were redrawn with the same wheel on their stand, closed and open. Tools and measurements: `DialRedesign-2026-10-01/tools/` and `production/`.

**Build R (Sept 28): the chat box on every Caspar panel.** The owner ruled that where the player taps the room or the Dial only the chat box shows, and handed Claude the remaining poses. `Slots.DressChatBox` dresses a panel as the box: the `chat-box` frame and fill sliced to the panel's size (`pixelsPerUnitMultiplier` 2, since the file is drawn at 2x), the old CASPAR label hidden, and the `chat-plate` on the top-left edge with the name written on it. It dresses the rooms: the Atrium hub and the Chamber, each at its old rect, so no button moves; the line is left-aligned and kept clear of the frame. The instrument screens take a slimmer box instead (the owner rejected the ornate frame on the Dial, Sept 28, and picked option A, fitted, Sept 29): `Slots.DressInstrumentBox` draws a flat dark panel with a hairline gold border (a sprite made in code, no slot) and C A S P A R small in gold at the top left over a short rule, pinned at its top and fitted to the line by `FitBox` (`Slots.InstrumentBoxHeight`: 32 above the line, 12 below, at least 60, at most the old panel's height, past which the text's best fit shrinks it). It dresses the Dial, the Book of Symbols, and the Elemental Table. Caspar's figure stands behind the box only on story beats: the opening and the return (Build P), and the Chamber's introduction (explain, solemn, calm; `SliceView.ChamberPoses`), where the figure sits with its waist at the panel's top (435). On the first Key he steps away while the Chamber wakes, and back in for his two lines (moved: "She breathes"; warm: "Let us continue, shall we?"). The Chamber as a room and the hub show the chat box alone; the book, the table, and the Dial show the slim box.

### The Grand Atrium kit (Build N, Sept 25)

The Grand Atrium is built as a kit, on the same `RoomKit` code as the Wing: `atrium` is the restored **shell** (architecture only), `atrium-grime` (360 × 800, cap 2048) is the dust, cobwebs, and cold blue tint over it that fades out by the Atrium's stage, and each **piece** has a worn and a restored file, `akit-<piece>-worn` / `akit-<piece>-restored`. Piece files are drawn at **half their pixel size**, positioned from `SliceView.AtriumKit`; the sizes are in the table at the end of this section. The pieces: `banner`, `bench`, `bust`, `candlestand`, `chandelier`, `desk`, `lamp`, `pennant`, `plant`, `rug`.

**The domed Atrium (Build Q, Sept 28).** The owner approved the new Grand Atrium (the B2 world: a glass dome on the night sky, dark wood Gothic shelves on a balcony, navy banners, the statue in a lit niche above the Zodiac Wing door) and its layout check. The art pass made it as an edit of the old restored scene, so the doors, plates, steps, desk, bench, busts, plants, candlestands, and rug keep their places; the new scene sat 22 px low (at 1×) and every file is moved 22 px up to meet the game's doors. `atrium` is the new restored shell, and `atrium-grime` is now the same empty room asleep (cold moonlight, a grimy cracked dome, dust, cobwebs), opaque, so the kit's grime fade is a clean cross-fade from the dormant room to the awake one (the two shells line up at offset 0, 0). New art for `banner` (the long navy banner), `pennant` (new: the narrow star pennant beside the niche), `lamp` (the wall lantern), and `chandelier` (now the armillary ring under the dome; the slot keeps its name); their worn files come from the dormant scene and are scaled in `SliceView.AtriumKit` (`wornScale`) to hang as far as the restored ones. The old `chart` and `shelf` pieces are retired from the placements (the new shell paints its own shelves); their files stay. The kit is 20 pieces; the lanterns take the shelf's Key 3, and the armillary ring stays last (Key 21). `atrium-light` is unchanged.

The three doors and the doorway plates are their own slots, tracked by lock state rather than worn/restored: `akit-door-closed` / `akit-door-locked` / `akit-door-open` (each 70 × 136), and `akit-plate-clean` / `akit-plate-locked` (each 64 × 28; from `SliceView.AtriumKit` / `AddDoor`). A door goes locked (weathered, chains and a padlock) → unlocked (clean, edges glowing) → open as the Keeper reaches it; the Wing door starts unlocked, the Chamber's unlocks at the first Key, and the sealed door stays locked. The clean plate is where the engine writes the room's name (THE ZODIAC WING, THE CRYSTAL BOOK CHAMBER); the sealed door's plate stays locked and unreadable.

Piece sizes on the 360 × 800 layout (draw at twice that), cap 1024. `chart` and `shelf` are retired from the placements (Build Q); their slots and files stay.

| Piece | Restored | Worn |
| --- | --- | --- |
| `banner` | 64 × 166 | 66 × 184 |
| `bench` | 96 × 52 | 104 × 40 |
| `bust` | 28 × 76 | 44 × 68 |
| `candlestand` | 28 × 90 | 36 × 88 |
| `chandelier` | 148 × 218 | 86 × 138 |
| `chart` | 34 × 46 | 38 × 48 |
| `desk` | 146 × 96 | 142 × 78 |
| `lamp` | 22 × 62 | 18 × 42 |
| `pennant` | 48 × 132 | 42 × 132 |
| `plant` | 36 × 46 | 34 × 48 |
| `rug` | 316 × 420 | 200 × 108 |
| `shelf` | 80 × 300 | 82 × 300 |

### The Wing Dial's eye (Build Z, Sept 30)

The Wing kit's `dial` piece rests with its **eye closed**, worn and restored alike (owner, Sept 30), and opens it when the player taps the Dial. Build AC redrew all four pieces to the Cast Dial (the eye's centre now at .404 of the restored file's height and .428 of the worn one's). The open eye has a slot per state, `kit-dial-worn-open` and `kit-dial-restored-open`, each **the same canvas as its closed piece and identical to it outside the eye**. The game lays the open file under a mask at the eye that grows from the seam (about 0.35 s), so only the lids move; the Keeper then walks over and the Dial screen opens. Back in the room the eye rests closed again. Reduced motion opens it at once.

### The Crystal Book Chamber kit (Build O, Sept 25)

The Chamber is built on the same kit code: `chamber` is the restored shell, `chamber-grime` (360 × 800, cap 2048) is the dust, cobwebs, clouded crystals, and cold tint that fades out by Keys spent, and each piece has a worn and a restored file, `ckit-<piece>-worn` / `ckit-<piece>-restored`, drawn at half their pixel size from `SliceView.ChamberKit`. The pieces: `banner`, `book`, `brazier`, `candle`, `crystal`, `mechanism`, `reliquary`. The seven Books stand on the altar and are drawn separately, sealed or open by `BooksOpen`, with a lock at each Book's foot; the old candles, mechanism, pages, labels, and floor band hide once the kit is in.

Sizes on the 360 × 800 layout (draw at twice that), cap 1024 for every piece.

| Piece | Restored | Worn |
| --- | --- | --- |
| `banner` | 26 × 90 | 32 × 100 |
| `book` | 40 × 66 | 40 × 64 |
| `brazier` | 18 × 56 | 22 × 48 |
| `candle` | 8 × 22 | 16 × 34 |
| `crystal` | 22 × 54 | 28 × 52 |
| `mechanism` | 70 × 110 | 74 × 112 |
| `reliquary` | 30 × 46 | 26 × 38 |

### The opening scene (Oct 8, 86bcfhmha)

The owner's rulings of Oct 8 open a new game on a short movie before WHO ARE YOU? (Q01 amended). The sixteen `prologue-*` slots are its frames: each is a whole 360 × 800 picture (720 × 1600 in the file), and the game moves only inside it: a push-in or a pan at scale 1 or more, so no edge ever shows; crossfades and cuts; framed comic panels that slide in, each a window on part of a frame (shot 2's two panels sit at 54 to 354 and 374 to 674 down (clear of the gear and of Skip; a cutout's band moves them down with the gear) and show the desk from 22.5% to 60% down and the notebook from 34% to 72% (on the Oct 9 redraw the pages measure 42.6% to 61.8% and the pen's tip 50.7%); shot 5's sits at 205 to 595 and shows 17% to 66%; 340 or 300 wide, centred); and the flash. The owner, Oct 8, replaced the brief's fading glow: the scene ends zooming into Caspar's glowing eyes (a slow push-in on `prologue-caspar-face`, a crossfade to `prologue-caspar-eyes`, the push carrying on), and the white blooms out of the glow into darkness. The owner's Oct 9 slight animations are frame swaps (ruling 4): in shot 2 the notebook's panel loops `prologue-notebook`, `-2` and `-3` at 4 frames a second once it has landed (the hand writes); in shot 3 `prologue-light-up` turns his head to the window with a 0.15 s crossfade, and the headphones fall `prologue-look`, `prologue-headphones-mid`, `prologue-headphones`, 0.14 s apart. Reduced motion holds the notebook still, keeps the head turn as a 0.4 s crossfade and skips the in-between. No frame needs transparency. The frame pairs and the turn trio must line up (`prologue-desk` and `prologue-light`; `prologue-look` and `prologue-headphones`; `prologue-caspar-back`, `-turn` and `-face`; and `prologue-caspar-eyes`, which must continue the face's framing pushed in toward the eyes). The shot list is data in `SliceView.PrologueShots` (shot, frames, scale and view from and to, the steps, each panel's source, each frame's own push, the spoken line); its timings are working choices. The push-in targets were measured on the final art (Oct 8): the city's lit window at 58.1% across and 23.7% down, his eyes on the face at 56.6% and 21.3%, the glow on the close-up at 50.7% and 47.1%; the street's pan ends with Caspar and his ring (47.5%, 69%) in view. Until a file lands each frame shows its labelled placeholder: a grey frame with its slot's name and a faint grid, so a pan reads.

`orb` and `orb-star` are cut out to transparency. The orb floats over the question on the opening's name and birth screens only, centred in the band between the top of the screen (below any cutout's band) and the question; one star gathers round it per step answered, at eight even places taken in a spread order (top, bottom, right, left, then the diagonals), so any count sits balanced. Until their files land the orb is a grey disc labelled "orb" and a star a small bone dot.

### The launch (Oct 9, 86bcg62x3)

The owner's rounds 3 and 4 (Oct 9) open every launch on a logo screen, a short launch movie and the main menu. `studio-logo` is TSG Games' Signature Crown Mark on black, drawn whole inside a 320 × 320 box centred on the black screen (its shape kept); it eases in over 1.2 s, holds 2.5 s and eases out over 1.2 s, 4.9 s in all (the owner, Oct 9: "a lil more screen time ... a nice fade in and fade out", then "shortened a bit before the fade out").

The movie (`SliceView.LaunchShots`, on the opening's machinery) is layered (round 4): five round cut-out layers, each 360 × 360 on the layout (720 × 720 in the file, with transparency, the disc's outer radius 306 of the 720), centred on the screen, over one painted sky, `intro-stars` (a full 360 × 800 frame). The wheel draws itself in pencil: `intro-wheel-pencil-lines` is revealed by a radial sweep (about 2.5 s), then `intro-wheel-pencil-glyphs` shows its twelve symbols one at a time in zodiac order (0.12 s apart); the sweeps' start and direction are constants (`SweepOrigin`, `SweepClockwise`, `SweepOffset`, and the same for the glyphs), set from the art lane's measurements: both start at the Aries cusp, 136.5° (0° at 3 o'clock), and run counter-clockwise, a 30° cell per symbol, Aries first; a soft warm glint rides the lines' sweep. From the draw to the end of coming alive the wheel's layers zoom in slowly from 0.8 to 1 (the owner, Oct 9), eased out, never past 1; the sky holds still. It comes alive (`intro-wheel-lit` crossfades in while the black behind becomes `intro-stars`), burns alive (`intro-wheel-burning`, a spin starting; the lit wheel under it turns with it, at one angle, until it goes), and spins on while it fades away and a level, calm `intro-earth` fades in under it, the ring of fire sitting on Earth's rim (both radius 306 of the 720) and leaving Earth behind; Earth never turns (the owner, after the merged build, Oct 9: the baked wheel-to-Earth frame `intro-wheel-earth` left the manifest). The five layers must share one framing. The calm Earth gets a slow push; then the descent, full frames each pushing in and each coming in over what shows: `intro-continents`, `intro-america`, `intro-newyork-state`, `intro-newyork-city`, `intro-brooklyn`, `intro-block`, ending on `prologue-city` (the one lit window) before the fade to the menu. The pushes hold each frame's centre until the art lands and is measured. About 30 s in all; the timings are working choices. Until their files land, the layers show as labelled circles on transparency (so the layering shows) and the full frames as labelled grey frames.

`menu-city` is the menu's background (M1b, the night city recomposed as a title screen), with room for the title near the top and the four buttons from 496 down; no text in the picture. `menu-title` is the owner's ASCENDANT "Library Seal" logo (approved in round 4), landscape at about 1.38:1, its black ground keyed out to transparency, drawn whole inside a 270 × 196 box centred 115 down (below any cutout's band), its top about 16 px clear; the box is a working choice, easy to tune when the art lands.

On the menu (the owner, after the merged build, Oct 9): a soft warm glow pulses over `menu-city`'s lit window (drawn in code, measured on M1b at 17.7 right of the middle and 274 down, about 3x the window, a 3.5 s sine between 0.15 and 0.5; steady at 0.3 under reduced motion; hidden while Settings is open), and now and then a rat runs across the street: `menu-rat-1` to `-4`, a 4-frame run cycle (96 × 48 files, facing right, flipped to run left, feet on a shared line 47 of 48 down), its feet on the asphalt at about 762, behind the title and the buttons, across the width in 2.2 s at 12 frames a second, first 6 to 10 s after the menu turns idle, then every 15 to 30 s; only on the menu with nothing open over it; none under reduced motion; no sound.

## Sound slots

| Slot | Plays when |
| --- | --- |
| `step` | the wheel crosses one detent: a button, a drag, an arrow key, a count beat, or a worked example; a tile or cell picked on the table |
| `seal` | a Seal or a tap answer is accepted (on the wheel, the table, the book, the builder, and in practice) |
| `miss` | a Seal or a tap answer is rejected (same places) |
| `key` | a Key is earned (Key 1 when it rises from the Dial; Keys 2–4 on the last answer) and a Key is spent on a lock |
| `page` | Caspar's page turns in the opening, the return, and the Chamber; the book opens or closes; a Book's page rises |
| `door` | the Keeper crosses a doorway (at the fade) |
| `ambient` | the room loop, from the first screen, looping (browsers start it at the first tap) |

A cue plays at most once per frame (a direct seat tap that crosses five detents is one step). The **Sound** row in Settings (the gear at the top right; before Build U a test button on the Atrium's bottom row) and the button on the style page mute everything for the session; it has nothing to do with reduced motion.

## Import settings

Applied by [`Assets/Editor/CelestialDial/SlotImport.cs`](../../Assets/Editor/CelestialDial/SlotImport.cs) to every file under the two folders, on import, so a drop needs no settings pass:

- **Images:** Sprite (2D and UI), single, full-rect mesh, no mipmaps, alpha is transparency, sRGB, not readable, clamp, bilinear, no power-of-two scaling; **max size per slot** (the cap above); **compressed with crunch at quality 50** (the download shrinks several times over; the on-device format follows the WebGL texture setting, DXT by default). A side that is not a multiple of 4 is warned about.
- **Sounds:** Vorbis at quality 0.5, optimized sample rate, preloaded; cues forced to mono and decompressed on load (no latency), the ambient loop stereo and compressed in memory.

Budget: at Build H the shipped test set (33 images, 7 sounds, 436 KB on disk) added well under 1 MB to the WebGL build; the measured increase is in [VALIDATION.md](VALIDATION.md) under Build E. The set has grown with the manifest since: it now holds one image per image slot (224). Real art will cost more; the style page is where to judge a set before it goes in, and the build's `.data` size before and after is the number to watch.

## The test set and the style page

`Assets/CelestialDial/Resources/Art/test/` and `.../Audio/test/` hold a generated set: one flat colored box per image slot with a border, a diagonal (a flip shows), transparent corners, and the slot's name; seven short synthesized cues. They are not art. They exist so the wiring can be checked end to end, and so the owner can see where every slot lands. [`Tools/make-test-set.py`](../../Tools/make-test-set.py) (standard library only) regenerates them.

- `?art=test` on the web build plays the game with the test set in every slot; the Editor menu **Ascendant › Greybox › Play with the test art set** does the same.
- `?style` shows the style page on the Art and Audio folders (the owner's files), `?style=test` on the test set; **Ascendant › Greybox › Play the style page** in the Editor. The page lists every slot with its size and source (`file`, `placeholder`, `test set`; sounds `file`, `silent`, `test set`), a thumbnail of each image, and a button per sound that plays it. The semantic layer carries the same list for a screen reader and for the browser suite.
- With no query and no menu, the game plays on the Art folder, which is the owner's; the shipped test set is never used unless asked for.
