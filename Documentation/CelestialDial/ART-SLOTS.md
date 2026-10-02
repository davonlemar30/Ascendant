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
| `journal-page` | 360 × 800 | 2048 | the journal's page (Build F; Build AA, Sept 30: the Black Hours: one tall black-blue vellum page in the black book, gold corner flourishes, a calm field; painterly by the owner's journal-only exception; no rules, the engine draws the silver rules and the margin) |
| `journal-cover` | 60 × 60 | 256 | the journal's closed cover: black leather, the gold wheel inlay (Build F; Build AA) |
| `journal-ribbon` | 16 × 120 | 256 | a faded vermilion silk ribbon bookmark, forked tail at the bottom; nine-sliced to the ladder's length on a sign page, a short tab on a seat when due |
| `journal-wheel` | 300 × 300 | 1024 | the journal's index: the gold wheel with twelve empty sockets on radius 216 of 600 (socket 1 at 9 o'clock), spokes between them, the rosette; transparent outside the silver ring (Build AA) |
| `journal-seat-leaf` | 56 × 56 | 256 | a mastered seat's raised gold-leaf ring with a soft glow, clear inside (Build AA) |
| `journal-seat-line` | 56 × 56 | 256 | a plain pale ring the engine tints silver (met) or gold (practising), clear inside (Build AA) |
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
| `caspar-calm` | 264 × 468 | 1024 | Caspar from the waist up behind the chat box (Build P), calm: the default, short instructions |
| `caspar-explain` | 264 × 468 | 1024 | Caspar behind the chat box, explaining: teaching, the lessons, the history (the opening's fourth page, the return's second) |
| `caspar-warm` | 264 × 468 | 1024 | Caspar behind the chat box, warm: welcomes, thanks, praise (the opening's second page) |
| `caspar-wry` | 264 × 468 | 1024 | Caspar behind the chat box, wry: his dry humour (the opening's first page) |
| `caspar-moved` | 264 × 468 | 1024 | Caspar behind the chat box, moved: wonder at the Library waking (the return's first page, the Keeper Key) |
| `caspar-solemn` | 264 × 468 | 1024 | Caspar behind the chat box, solemn: honesty and the backstory (the opening's third page) |

### The Wing room kit (Build M, Sept 25)

The journal is the Black Hours (Build AA, owner Sept 30; it replaces Build J's crimson ring binder). One tall page of black-blue vellum written in gold and silver, painterly by the owner's journal-only exception to the Art Bible; the style reference is `Documentation/ConceptArt/JournalMockup/journal-black-hours-v3-2-silver-ruled.png`. Draw `journal-page` at 720 × 1600: the page runs x 24–696, y 32–1232, its calm field x 72–648, y 160–1100 (nothing there: the engine draws the silver rules 26 px apart from y 96 and the faded vermilion margin, `RuleFirst` and `RuleGap` in `SliceView`), and below y 1260 a quiet dark desk where the buttons sit. `journal-wheel` is drawn at 600 × 600 with its twelve sockets on radius 216 (socket 1 at 9 o'clock, counter-clockwise, as on the Dial) and shown at 270 px (`WheelSize`), so a socket is 41 px; the layout guides are in `Documentation/ConceptArt/JournalMockup/production-2026-09-30/`. The seat rings are 112 × 112 files: `journal-seat-leaf` (mastered) is drawn as painted, `journal-seat-line` is near-white so the engine can tint it silver (met) or gold (practising). Each `sign-*` picture is **one full-colour file on a transparent background** (400 × 400), clipped round in a seat and on its page. The engine draws it as line art and brings its colour up as the player learns the sign (Illumination, the `Ascendant/Illumination` shader in `Resources/Shaders`). The ribbon is nine-sliced (its top 6% and bottom 20% keep their shape), so keep its middle a plain band.

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

The dial has a third state for the Dial's wake-up (owner, Oct 1; 86bcbn6w6 2d A): `kit-dial-bright` and `kit-dial-bright-open`, 188 × 206 like the restored pair. The small Dial follows the Dial screen's five steps: worn, half today's over it (Key 1), restored (Key 2), half bright over it (Key 3), bright (Key 4); until the bright files land, the restored pair stands in.

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

Budget: at Build H the shipped test set (33 images, 7 sounds, 436 KB on disk) added well under 1 MB to the WebGL build; the measured increase is in [VALIDATION.md](VALIDATION.md) under Build E. The set has grown with the manifest since: it now holds one image per image slot (151). Real art will cost more; the style page is where to judge a set before it goes in, and the build's `.data` size before and after is the number to watch.

## The test set and the style page

`Assets/CelestialDial/Resources/Art/test/` and `.../Audio/test/` hold a generated set: one flat colored box per image slot with a border, a diagonal (a flip shows), transparent corners, and the slot's name; seven short synthesized cues. They are not art. They exist so the wiring can be checked end to end, and so the owner can see where every slot lands. [`Tools/make-test-set.py`](../../Tools/make-test-set.py) (standard library only) regenerates them.

- `?art=test` on the web build plays the game with the test set in every slot; the Editor menu **Ascendant › Greybox › Play with the test art set** does the same.
- `?style` shows the style page on the Art and Audio folders (the owner's files), `?style=test` on the test set; **Ascendant › Greybox › Play the style page** in the Editor. The page lists every slot with its size and source (`file`, `placeholder`, `test set`; sounds `file`, `silent`, `test set`), a thumbnail of each image, and a button per sound that plays it. The semantic layer carries the same list for a screen reader and for the browser suite.
- With no query and no menu, the game plays on the Art folder, which is the owner's; the shipped test set is never used unless asked for.
