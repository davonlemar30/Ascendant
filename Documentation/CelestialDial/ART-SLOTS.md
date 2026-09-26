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
| `atrium` | 360 × 800 | 2048 | the Grand Atrium, behind the opening, the return, and the room |
| `wing` | 360 × 800 | 2048 | the Zodiac Wing room **with its objects painted in** (Build L, the owner's mockup B, Sept 24): the Dial on its dais, the table with its board, the covered chair, the shelf and ladder. With this file the `shelf`, `table`, and `dial-face` slots are not drawn in the room; their tap areas and glows sit over the painted objects. A new `wing` must keep the objects where they are or the layout in `SliceView.BuildWingRoom` moves with it |
| `chamber` | 360 × 800 | 2048 | the Crystal Book Chamber, first visit and room |
| `caspar` | 64 × 112 | 256 | Caspar standing in the Atrium |
| `keeper-idle` | 44 × 100 | 256 | the Keeper standing; flipped to face the way it last walked |
| `keeper-walk` | 44 × 100 | 256 | the Keeper mid-step; alternates with idle every step while walking (either frame alone serves for both) |
| `dial-face` | 332 × 332 | 1024 | the Dial's face under the twelve seats (the ring's lines go); also the Dial seen from the Wing room, at 200 × 200 |
| `seat` | 52 × 52 | 256 | one seat tile, twelve times; dimmed while dormant or unlit |
| `bracket` | 58 × 58 | 256 | the fixed focus bracket over the framed seat (the four bars go) |
| `floor-markings` | 320 × 320 | 1024 | the faded floor pattern under the wheel; brightens as the wheel wakes |
| `shelf` | 50 × 36 | 256 | the collapsed bookshelf, in the Wing room and beside the wheel |
| `chair` | 44 × 36 | 256 | the covered chair beside the wheel (its dust cloth goes) |
| `table` | 60 × 30 | 256 | the table with its board of twelve, in the Wing room (the twelve squares go) |
| `shelf-book` | 10 × 26 | 64 | a book on a shelf: three on the Wing's, three that return to the Atrium's at Stage 4 |
| `book-cover` | 140 × 140 | 512 | the book on the shelf, closed |
| `book-page` | 140 × 140 | 512 | the book's open page behind each symbol |
| `journal-page` | 360 × 800 | 2048 | the journal's page (Build F; Build J: one page to a screen, vellum, room for text; a section's entries and a sign's page sit on it) |
| `journal-cover` | 60 × 60 | 256 | the journal's cover, at the head of the contents page (Build F) |
| `journal-contents` | 360 × 800 | 2048 | the journal's contents page: a header, ruled rows for the entries (the engine writes them) |
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
| `candle` | 8 × 20 | 64 | a candle: the opening's one, the Wing's, the Chamber's nine; dark until lit |
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

### The Wing room kit (Build M, Sept 25)

The journal is a book (Build J, owner Sept 23 and 25). The screen shows the left page of an open ring binder: a loose-leaf sheet in the Library's palette, with the brass rings and the edge of the facing page at the right. Draw `journal-page` and `journal-contents` at 720 × 1600 with the same camera. The left page runs x 28–612, y 52–1224, and the writing column x 92–560. The page carries the loose-leaf lines (owner, Sept 25: "the fat double spaced lines"): twelve rules 80 px apart, the first at y 264, and a crimson margin line at x 80. The engine writes each entry on a line (`RuleTop` and `RuleGap` in `SliceView`), so a new page must keep those rules where they are. Each `sign-*` picture is **one full-colour file on a transparent background** (400 × 400). The engine draws it as line art and brings its colour up as the player learns the sign (Illumination, the `Ascendant/Illumination` shader in `Resources/Shaders`), so clean outlines and flat colour shapes matter more than detail. The ribbon is nine-sliced (its top 6% and bottom 20% keep their shape), so keep its middle a plain band.

The Zodiac Wing is built as a kit (owner, Sept 24): `wing` is the restored **shell** (architecture only), `wing-grime` (360 × 800) is the dust, cobwebs, and cold tint over it that fade out by Keys, and each **piece** has a worn and a restored file, `kit-<piece>-worn` / `kit-<piece>-restored`. Piece files are drawn at **half their pixel size**, bottom-centred on their placement in `SliceView.WingKit` (x, bottom, the Key that restores it, a scale); the slot sizes below are the restored piece on the 360 × 800 layout, cap 1024. The pieces: `window`, `carpet`, `chandelier`, `banner` (three placements), `orrery`, `armillary`, `lectern`, `globe`, `shelf`, `dial`, `telescope`, `table`, `candles` (two placements), `books`, `chair`, `plate` (the doorway's name plate; the engine writes the name on the restored one). A piece restores on its Key, except the shelf (when the book of symbols wakes: the wheel lit) and the table (when it wakes: the modalities complete), because their lessons happen on them.

Not slots, on purpose: the Caspar panels, buttons, and text (interface, not placeholder art); the glows (doorway light, shelf and table glow, the Key's glow, the seam) and the fade, which are effects drawn over whatever is there; the Dial screen's charcoal backdrop; the table's board of cells and tiles, which is a control.

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

A cue plays at most once per frame (a direct seat tap that crosses five detents is one step). The **Sound: on/off (test)** button on the Atrium's bottom row and on the style page mutes everything for the session; it has nothing to do with reduced motion, and it is a test control that will go.

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
