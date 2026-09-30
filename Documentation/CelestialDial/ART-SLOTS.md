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

Sizes are the placeholder's rect on the 360 × 800 reference layout; the cap is the imported texture's longest side.

| Slot | Size | Cap | Where it is drawn |
| --- | --- | --- | --- |
| `atrium` | 360 × 800 | 2048 | the Grand Atrium, behind the opening, the return, and the room; since Build Q the domed Atrium's restored shell |
| `wing` | 360 × 800 | 2048 | the Zodiac Wing room **with its objects painted in** (Build L, the owner's mockup B, Sept 24): the Dial on its dais, the table with its board, the covered chair, the shelf and ladder. With this file the `shelf`, `table`, and `dial-face` slots are not drawn in the room; their tap areas and glows sit over the painted objects. A new `wing` must keep the objects where they are or the layout in `SliceView.BuildWingRoom` moves with it |
| `chamber` | 360 × 800 | 2048 | the Crystal Book Chamber, first visit and room |
| `caspar` | 64 × 112 | 256 | Caspar standing in the Atrium |
| `keeper-idle` | 44 × 100 | 256 | the Keeper standing; flipped to face the way it last walked |
| `keeper-walk` | 44 × 100 | 256 | the Keeper mid-step; alternates with idle every step while walking (either frame alone serves for both) |
| `dial-face` | 332 × 332 | 1024 | the wheel alone, cut from the Dial room (Build Z): the Wing room's fallback Dial when its kit piece has no file (200 × 200); the Dial screen draws `dial-room` instead |
| `dial-room` | 360 × 800 | 2048 | the Dial screen's background (Build Z, owner, Sept 29–30): Room A, the Zodiac Wing behind the Dial, drawn dormant. The wheel's twelve sockets sit under the seats (radius 136 around y 270) and the eye's glass is centred on the wheel; with this file the floor markings, shelf and chair leave the Dial screen and the face slot is not drawn there |
| `dial-room-light` | 360 × 800 | 2048 | the Dial's glow over `dial-room` (Build Z): the amber in the sockets, the ring's line and the rims, transparent elsewhere. Off while the Dial sleeps; swept in round the ring, clockwise from the top, as each pattern opens |
| `seat` | 52 × 52 | 256 | one seat: a dark glass cap in a bronze bezel (Build Z), twelve times, sitting in the wheel's sockets; the glass lets the socket glow through; dimmed while dormant or unlit |
| `bracket` | 58 × 58 | 256 | the fixed focus bracket over the framed seat (the four bars go) |
| `floor-markings` | 320 × 320 | 1024 | the faded floor pattern under the wheel; brightens as the wheel wakes |
| `shelf` | 50 × 36 | 256 | the collapsed bookshelf, in the Wing room and beside the wheel |
| `chair` | 44 × 36 | 256 | the covered chair beside the wheel (its dust cloth goes) |
| `table` | 60 × 30 | 256 | the table with its board of twelve, in the Wing room (the twelve squares go) |
| `shelf-book` | 10 × 26 | 64 | a book on a shelf: three on the Wing's, three that return to the Atrium's at Stage 4 |
| `book-cover` | 140 × 140 | 512 | the book on the shelf, closed |
| `book-page` | 140 × 140 | 512 | the book's open page behind each symbol |
| `journal-page` | 360 × 800 | 2048 | the journal's page (Build F; Build J: the left page of an open crimson binder, loose-leaf with twelve ruled lines the engine writes on; a section's entries and a sign's page sit on it) |
| `journal-cover` | 60 × 60 | 256 | the journal's cover, at the head of the contents page (Build F) |
| `journal-contents` | 360 × 800 | 2048 | the journal's contents page: the same page with the Library's illuminated border and vine; the engine writes the entries on every other line |
| `journal-ribbon` | 16 × 120 | 256 | a silk ribbon bookmark, forked tail at the bottom; nine-sliced to the ladder's length on a sign page, a short tab on the contents |
| `journal-plate` | 112 × 34 | 512 | an engraved brass plate a sign's fact is written on (element, modality, polarity, opposite) |
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
| `atrium-light` | 360 × 800 | 2048 | the Atrium's golden-hour light (Build H): a transparent overlay drawn over the background and under everything else, faded by the Atrium stage (nothing at Stage 1, a quarter at 2, half at 3, three quarters at 4, full at 5 and 6) |
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

The journal is a book (Build J, owner Sept 23 and 25). The screen shows the left page of an open ring binder: a loose-leaf sheet in the Library's palette, with the brass rings and the edge of the facing page at the right. Draw `journal-page` and `journal-contents` at 720 × 1600 with the same camera. The left page runs x 28–612, y 52–1224, and the writing column x 92–560. The page carries the loose-leaf lines (owner, Sept 25: "the fat double spaced lines"): twelve rules 80 px apart, the first at y 264, and a crimson margin line at x 80. The engine writes each entry on a line (`RuleTop` and `RuleGap` in `SliceView`), so a new page must keep those rules where they are. Each `sign-*` picture is **one full-colour file on a transparent background** (400 × 400). The engine draws it as line art and brings its colour up as the player learns the sign (Illumination, the `Ascendant/Illumination` shader in `Resources/Shaders`), so clean outlines and flat colour shapes matter more than detail. The ribbon is nine-sliced (its top 6% and bottom 20% keep their shape), so keep its middle a plain band.

The Zodiac Wing is built as a kit (owner, Sept 24): `wing` is the restored **shell** (architecture only), `wing-grime` (360 × 800) is the dust, cobwebs, and cold tint over it that fade out by Keys, and each **piece** has a worn and a restored file, `kit-<piece>-worn` / `kit-<piece>-restored`. Piece files are drawn at **half their pixel size**, bottom-centred on their placement in `SliceView.WingKit` (x, bottom, the Key that restores it, a scale); the slot sizes below are the restored piece on the 360 × 800 layout, cap 1024. The pieces: `window`, `carpet`, `chandelier`, `banner` (three placements), `orrery`, `armillary`, `lectern`, `globe`, `shelf`, `dial`, `telescope`, `table`, `candles` (two placements), `books`, `chair`, `plate` (the doorway's name plate; the engine writes the name on the restored one). A piece restores on its Key, except the shelf (when the book of symbols wakes: the wheel lit) and the table (when it wakes: the modalities complete), because their lessons happen on them.

Not slots, on purpose (except the story screens' chat box and plate, Build P): the Caspar panels, buttons, and text (interface, not placeholder art); the glows (doorway light, shelf and table glow, the Key's glow, the seam) and the fade, which are effects drawn over whatever is there; the Dial screen's charcoal backdrop; the table's board of cells and tiles, which is a control.

### The chat box and Caspar's poses (Build P, Sept 28)

The owner's B2 ruling (Sept 28, note 3): on the story screens (the Grand Atrium opening and the return with the first Keeper Key) Caspar stands unframed behind a chat box, from the waist up, in one still pose per page. `chat-box` is drawn at the box's rect (324 × 240, centre 600 on the 360 × 800 layout); `chat-plate` sits on its top-left edge and the game writes CASPAR on it; the line is left-aligned in the box and Continue sits inside it. The six `caspar-<pose>` files share one registration: each is the whole pose canvas, scaled together, so the head lands in the same place in every pose and a swap does not jump; the game draws each at 264 × 468 centred at (5, 408). The pose per page is `SliceView.AtriumPoses` (wry, warm, solemn, explain) and `SliceView.ReturnPoses` (moved, explain), the working choice recorded on the Decisions Log (Sept 28). No `chat-box` file, no change: the old panel and Continue stay where they were. No pose file, no figure.

**Build R (Sept 28): the chat box on every Caspar panel.** The owner ruled that where the player taps the room or the Dial only the chat box shows, and handed Claude the remaining poses. `Slots.DressChatBox` dresses a panel as the box: the `chat-box` frame and fill sliced to the panel's size (`pixelsPerUnitMultiplier` 2, since the file is drawn at 2x), the old CASPAR label hidden, and the `chat-plate` on the top-left edge with the name written on it. It dresses the rooms: the Atrium hub and the Chamber, each at its old rect, so no button moves; the line is left-aligned and kept clear of the frame. The instrument screens take a slimmer box instead (the owner rejected the ornate frame on the Dial, Sept 28, and picked option A, fitted, Sept 29): `Slots.DressInstrumentBox` draws a flat dark panel with a hairline gold border (a sprite made in code, no slot) and C A S P A R small in gold at the top left over a short rule, pinned at its top and fitted to the line by `FitBox` (`Slots.InstrumentBoxHeight`: 32 above the line, 12 below, at least 60, at most the old panel's height, past which the text's best fit shrinks it). It dresses the Dial, the Book of Symbols, and the Elemental Table. Caspar's figure stands behind the box only on story beats: the opening and the return (Build P), and the Chamber's introduction (explain, solemn, calm; `SliceView.ChamberPoses`), where the figure sits with its waist at the panel's top (435). On the first Key he steps away while the Chamber wakes, and back in for his two lines (moved: "She breathes"; warm: "Let us continue, shall we?"). The Chamber as a room and the hub show the chat box alone; the book, the table, and the Dial show the slim box.

### The Grand Atrium kit (Build N, Sept 25)

The Grand Atrium is built as a kit, on the same `RoomKit` code as the Wing: `atrium` is the restored **shell** (architecture only), `atrium-grime` (360 × 800) is the dust, cobwebs, and cold blue tint over it that fades out by the Atrium's stage, and each **piece** has a worn and a restored file, `akit-<piece>-worn` / `akit-<piece>-restored`. Piece files are drawn at **half their pixel size**, positioned from `SliceView.AtriumKit`; the slot sizes below are the restored piece on the 360 × 800 layout, cap 1024. The pieces: `banner`, `bench`, `bust`, `candlestand`, `chandelier`, `desk`, `lamp`, `pennant`, `plant`, `rug`.

**The domed Atrium (Build Q, Sept 28).** The owner approved the new Grand Atrium (the B2 world: a glass dome on the night sky, dark wood Gothic shelves on a balcony, navy banners, the statue in a lit niche above the Zodiac Wing door) and its layout check. The art pass made it as an edit of the old restored scene, so the doors, plates, steps, desk, bench, busts, plants, candlestands, and rug keep their places; the new scene sat 22 px low (at 1×) and every file is moved 22 px up to meet the game's doors. `atrium` is the new restored shell, and `atrium-grime` is now the same empty room asleep (cold moonlight, a grimy cracked dome, dust, cobwebs), opaque, so the kit's grime fade is a clean cross-fade from the dormant room to the awake one (the two shells line up at offset 0, 0). New art for `banner` (the long navy banner), `pennant` (new: the narrow star pennant beside the niche), `lamp` (the wall lantern), and `chandelier` (now the armillary ring under the dome; the slot keeps its name); their worn files come from the dormant scene and are scaled in `SliceView.AtriumKit` (`wornScale`) to hang as far as the restored ones. The old `chart` and `shelf` pieces are retired from the placements (the new shell paints its own shelves); their files stay. The kit is 20 pieces; the lanterns take the shelf's Key 3, and the armillary ring stays last (Key 21). `atrium-light` is unchanged.

The three doors and the doorway plates are their own slots, tracked by lock state rather than worn/restored: `akit-door-closed` / `akit-door-locked` / `akit-door-open`, and `akit-plate-clean` / `akit-plate-locked` (from `SliceView.AtriumKit` / `AddDoor`). A door goes locked (weathered, chains and a padlock) → unlocked (clean, edges glowing) → open as the Keeper reaches it; the Wing door starts unlocked, the Chamber's unlocks at the first Key, and the sealed door stays locked. The clean plate is where the engine writes the room's name (THE ZODIAC WING, THE CRYSTAL BOOK CHAMBER); the sealed door's plate stays locked and unreadable.

### The Wing Dial's eye (Build Z, Sept 30)

The Wing kit's `dial` piece rests with its **eye closed**, worn and restored alike (owner, Sept 30), and opens it when the player taps the Dial. The open eye has a slot per state, `kit-dial-worn-open` and `kit-dial-restored-open`, each **the same canvas as its closed piece and identical to it outside the eye**. The game lays the open file under a mask at the eye that grows from the seam (about 0.35 s), so only the lids move; the Keeper then walks over and the Dial screen opens. Back in the room the eye rests closed again. Reduced motion opens it at once.

### The Crystal Book Chamber kit (Build O, Sept 25)

The Chamber is built on the same kit code: `chamber` is the restored shell, `chamber-grime` (360 × 800) is the dust, cobwebs, clouded crystals, and cold tint that fades out by Keys spent, and each piece has a worn and a restored file, `ckit-<piece>-worn` / `ckit-<piece>-restored`, drawn at half their pixel size from `SliceView.ChamberKit`. The pieces: `banner`, `book`, `brazier`, `candle`, `crystal`, `mechanism`, `reliquary`. The seven Books stand on the altar and are drawn separately, sealed or open by `BooksOpen`, with a lock at each Book's foot; the old candles, mechanism, pages, labels, and floor band hide once the kit is in.

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

Budget: the shipped test set (33 images, 7 sounds, 436 KB on disk) adds well under 1 MB to the WebGL build; the measured increase is in [VALIDATION.md](VALIDATION.md) under Build E. Real art will cost more; the style page is where to judge a set before it goes in, and the build's `.data` size before and after is the number to watch.

## The test set and the style page

`Assets/CelestialDial/Resources/Art/test/` and `.../Audio/test/` hold a generated set: one flat colored box per image slot with a border, a diagonal (a flip shows), transparent corners, and the slot's name; seven short synthesized cues. They are not art. They exist so the wiring can be checked end to end, and so the owner can see where every slot lands. [`Tools/make-test-set.py`](../../Tools/make-test-set.py) (standard library only) regenerates them.

- `?art=test` on the web build plays the game with the test set in every slot; the Editor menu **Ascendant › Greybox › Play with the test art set** does the same.
- `?style` shows the style page on the Art and Audio folders (the owner's files), `?style=test` on the test set; **Ascendant › Greybox › Play the style page** in the Editor. The page lists every slot with its size and source (`file`, `placeholder`, `test set`; sounds `file`, `silent`, `test set`), a thumbnail of each image, and a button per sound that plays it. The semantic layer carries the same list for a screen reader and for the browser suite.
- With no query and no menu, the game plays on the Art folder, which is the owner's; the shipped test set is never used unless asked for.
