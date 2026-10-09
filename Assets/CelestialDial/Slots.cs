using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Build E: named doors for art and sound (Sept 13 scope ruling, decision 4: presentation wiring only).
    // An art slot is a PNG named after it in Assets/CelestialDial/Resources/Art; a sound slot a WAV in .../Resources/Audio.
    // A file present is drawn in the placeholder's rect or played on its action; a file missing leaves the grey box and the
    // silence. The shipped test set lives in the "test" subfolders and is used only when asked for: ?art=test on the Web,
    // -artSet test in batch, the Editor menu in play. The manifest below is the list of slot names; add a slot when a new object appears.
    public static class Slots
    {
        public sealed class ArtSlot
        {
            public readonly string Name, Where; public readonly int Width, Height, MaxSize;
            public ArtSlot(string name, int width, int height, int maxSize, string where) { Name = name; Width = width; Height = height; MaxSize = maxSize; Where = where; }
        }
        public sealed class SoundSlot
        {
            public readonly string Name, When;
            public SoundSlot(string name, string when) { Name = name; When = when; }
        }
        // Width and height are the placeholder's rect on the 360 x 800 reference layout; MaxSize caps the imported texture.
        public static readonly ArtSlot[] Art =
        {
            new ArtSlot("atrium", 360, 800, 2048, "the Grand Atrium, behind the opening, the return, and the room; since Build Q the domed Atrium's restored shell: the dome, the dark wood shelves and balcony, the statue's niche, the empty doorways"),
            new ArtSlot("wing", 360, 800, 2048, "the Zodiac Wing's shell (Build M): the restored room's architecture only; the kit pieces, grime, and light layer over it"),
            new ArtSlot("chamber", 360, 800, 2048, "the Crystal Book Chamber, first visit and room"),
            new ArtSlot("caspar", 64, 112, 256, "Caspar standing in the Atrium"),
            new ArtSlot("keeper-idle", 44, 100, 256, "the Keeper standing; faces the way it last walked"),
            new ArtSlot("keeper-walk", 44, 100, 256, "the Keeper mid-step; alternates with idle every step while walking"),
            new ArtSlot("dial-face", 332, 332, 1024, "the wheel alone, cut from the Dial room (Build Z): the Wing room's fallback Dial when its kit piece has no file (200 x 200); the Dial screen draws dial-room instead"),
            new ArtSlot("dial-room", 360, 800, 2048, "the Dial screen's background (Build Z; Build AB; Build AC, owner Oct 1: the Cast Dial): Room A behind the Dial, the stand, the thin bronze rim with its ticks and its four diamonds, the hub ring, and the still hub: the phoenix holding a cast ribbon above the eye, the eye, its tail below; the turning ring (dial-ring) covers the band between the hub ring and the rim"),
            new ArtSlot("dial-room-light", 360, 800, 2048, "the Dial's fixed glow over dial-room (Build Z; Build AC): the rim, the diamonds, the hub ring, the ribbon's and the phoenix's bright brass; clear over the turning ring; off while the Dial sleeps, swept in by the reveal"),
            new ArtSlot("dial-ring", 360, 360, 1024, "the Cast Dial's turning ring (Build AC, owner Oct 1), centred on the wheel: twelve identical segments, each a cast name recess (faces r 136-155) and a seat window in its spoke (faces r 91-127), a rivet at each junction; clear outside r 156 and inside r 82; it turns with the seats"),
            new ArtSlot("dial-ring-light", 360, 360, 1024, "the turning ring's glow (Build AC): the recesses' and windows' bevels, the spokes' enamel lines; turns with the ring, off while the Dial sleeps"),
            new ArtSlot("dial-room-worn", 360, 800, 2048, "the Dial's room in its worn look: the dust and cobwebs, the tarnished bronze and a dull glow of the first visit (the wake-up, owner Oct 1; 86bcbn6w6 2a A, 2b B, 2c A to C): dial-room's exact canvas, centre and radii, the eye, the phoenix's outline, the room and the stand as today; until its file lands, today's dial-room stands in"),
            new ArtSlot("dial-room-light-worn", 360, 800, 2048, "dial-room-light for the worn look (the wake-up): registered to dial-room-worn; until its file lands, today's dial-room-light stands in"),
            new ArtSlot("dial-ring-worn", 360, 360, 1024, "the turning ring in the worn look (the wake-up): dial-ring's twelve identical segments and radii, cut from one segment so the twelve stay exact; until its file lands, today's dial-ring stands in"),
            new ArtSlot("dial-ring-light-worn", 360, 360, 1024, "dial-ring-light for the worn look (the wake-up): turns with the ring; until its file lands, today's dial-ring-light stands in"),
            new ArtSlot("dial-room-bright", 360, 800, 2048, "the Dial's room in its bright and new look: polished bronze catching light, the recesses and windows glowing stronger than today's, the outer glow held back (owner, Oct 1: polished bronze, not lit from inside) (the wake-up, owner Oct 1; 86bcbn6w6 2a A, 2b B, 2c A to C): dial-room's exact canvas, centre and radii, the eye, the phoenix's outline, the room and the stand as today; until its file lands, today's dial-room stands in"),
            new ArtSlot("dial-room-light-bright", 360, 800, 2048, "dial-room-light for the bright look (the wake-up): registered to dial-room-bright; until its file lands, today's dial-room-light stands in"),
            new ArtSlot("dial-ring-bright", 360, 360, 1024, "the turning ring in the bright look (the wake-up): dial-ring's twelve identical segments and radii, cut from one segment so the twelve stay exact; until its file lands, today's dial-ring stands in"),
            new ArtSlot("dial-ring-light-bright", 360, 360, 1024, "dial-ring-light for the bright look (the wake-up): turns with the ring; until its file lands, today's dial-ring-light stands in"),
            new ArtSlot("seat", 52, 52, 256, "one seat's cap, twelve times (Build Z): the greybox fallback, used only when the Dial has no turning ring (no dial-ring file, or no dial-room to hold it); with the Cast Dial's dial-ring each seat is its segment's name recess and window"),
            new ArtSlot("bracket", 72, 88, 256, "the fixed frame over the framed seat (Build AC: a gold wedge outline over the 9 o'clock recess and window, under the rim's diamond)"),
            new ArtSlot("floor-markings", 320, 320, 1024, "the faded floor pattern under the wheel; brightens as the wheel wakes"),
            new ArtSlot("shelf", 50, 36, 256, "the collapsed bookshelf, in the Wing room and beside the wheel"),
            new ArtSlot("chair", 44, 36, 256, "the covered chair beside the wheel"),
            new ArtSlot("table", 60, 30, 256, "the table with its board of twelve, in the Wing room"),
            new ArtSlot("shelf-book", 10, 26, 64, "a book on a shelf: three on the Wing's, three that return to the Atrium's at Stage 4"),
            new ArtSlot("book-cover", 140, 140, 512, "the book on the shelf, closed"),
            new ArtSlot("book-page", 140, 140, 512, "the book's open page behind each symbol"),
            // The Table and the Book come alive (owner, Oct 7-8; task 86bcf0x71): the Book's room and the Table's carved top behind today's layouts,
            // the Table's well and plates, the four fills its live lettering takes, and an emblem per sign that the Book raises once it is answered.
            new ArtSlot("book-room", 360, 800, 2048, "the Book of Symbols' screen: the Book lying open on a carved lectern in the Zodiac Wing's moonlight, two candles at its edge; behind today's layout (owner, Oct 7, 86bcex5kc 4A)"),
            new ArtSlot("table-room", 360, 800, 2048, "the Elemental Table's screen: the Zodiac Wing's own lectern table seen from above, alive (owner's pick, Oct 8): its gold inlay glowing from within, blackened iron corner fittings, a carved scorpion and phoenix on its header, dark mist; twelve slate squares on the cells, a wide left rail and header for the names, a tray on its front lip for the plates"),
            new ArtSlot("table-well", 86, 48, 256, "one slate square of the Table, cut from table-room's centre square and drawn at each of the twelve, so all twelve are identical; nothing inside it"),
            new ArtSlot("table-plate", 82, 44, 256, "a plain pale wooden plate, one for each sign (owner's pick, Oct 8: pale plates); the game burns its symbol and name in"),
            new ArtSlot("table-plate-gold", 82, 44, 256, "the same plate turned gold, when a sign is placed on the player's own (owner, Oct 7: it comes alive on a correct Seal)"),
            new ArtSlot("letter-fire", 128, 128, 512, "a tileable fill of drawn flames and embers for the Table's live lettering"),
            new ArtSlot("letter-earth", 128, 128, 512, "a tileable fill of drawn moss and stone for the Table's live lettering"),
            new ArtSlot("letter-air", 128, 128, 512, "a tileable fill of drawn wind and cloud for the Table's live lettering"),
            new ArtSlot("letter-water", 128, 128, 512, "a tileable fill of drawn deep water for the Table's live lettering"),
            new ArtSlot("emblem-aries", 180, 180, 512, "the Aries symbol as a living emblem of fire, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-taurus", 180, 180, 512, "the Taurus symbol as a living emblem of earth, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-gemini", 180, 180, 512, "the Gemini symbol as a living emblem of air, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-cancer", 180, 180, 512, "the Cancer symbol as a living emblem of water, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-leo", 180, 180, 512, "the Leo symbol as a living emblem of fire, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-virgo", 180, 180, 512, "the Virgo symbol as a living emblem of earth, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-libra", 180, 180, 512, "the Libra symbol as a living emblem of air, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-scorpio", 180, 180, 512, "the Scorpio symbol as a living emblem of water, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-sagittarius", 180, 180, 512, "the Sagittarius symbol as a living emblem of fire, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-capricorn", 180, 180, 512, "the Capricorn symbol as a living emblem of earth, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-aquarius", 180, 180, 512, "the Aquarius symbol as a living emblem of air, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("emblem-pisces", 180, 180, 512, "the Pisces symbol as a living emblem of water, rising from the Book once it is answered (owner, Oct 8: the set approved)"),
            new ArtSlot("journal-page", 360, 800, 2048, "the journal's page (Build F; Build AA, Sept 30: the Black Hours: one tall black-blue vellum page in the black book, gold corner flourishes, a calm field; painterly by the owner's journal-only exception; no rules, the engine draws the silver rules and the margin)"),
            new ArtSlot("journal-cover", 60, 60, 256, "the journal's closed cover: black leather, the gold wheel inlay (Build F; Build AA)"),
            // Build J (owner, Sept 23: Illumination plus Ribbons); Build AA (owner, Sept 30): the Wheel index, gold for mastery
            new ArtSlot("journal-ribbon", 16, 120, 256, "a faded vermilion silk ribbon bookmark, forked tail at the bottom; nine-sliced to the ladder's length on a sign page, a short tab on a seat when due"),
            new ArtSlot("journal-wheel", 300, 300, 1024, "the journal's index: the gold wheel with twelve empty sockets on radius 216 of 600 (socket 1 at 9 o'clock), spokes between them, the rosette; transparent outside the silver ring (Build AA)"),
            new ArtSlot("journal-seat-leaf", 56, 56, 256, "a mastered seat's raised gold-leaf ring with a soft glow, clear inside (Build AA)"),
            new ArtSlot("journal-seat-line", 56, 56, 256, "a plain pale ring the engine tints silver (met) or gold (practising), clear inside (Build AA)"),
            // batch 2 (owner, Oct 1: the journal's architecture, the approved board on 86bcbn6w6): the landing, Contents and the Library Map
            new ArtSlot("journal-door-frame", 238, 120, 512, "a door on the journal's landing (Practice, Contents): the fine gold hairline frame of the approved round-2 art, its corner flourishes whole; nine-sliced (92 px in at 2x) to the door; the game draws the vellum panel inside it"),
            new ArtSlot("journal-emblem-practice", 56, 72, 256, "the Practice door's emblem: a closed black book with a gold sun and a faded vermilion ribbon (the approved board)"),
            new ArtSlot("journal-emblem-contents", 76, 60, 256, "the Contents door's emblem: an open book in polished gold, a gold star on each page"),
            new ArtSlot("journal-emblem-wheel", 48, 48, 256, "The Wheel's row on Contents: the journal's twelve-seat wheel (journal-wheel) at emblem size, a touch brighter; the game rings it in gold"),
            new ArtSlot("journal-emblem-map", 40, 40, 256, "The Library Map's row on Contents: the Library's parchment plan folded in three"),
            new ArtSlot("journal-emblem-lock", 24, 28, 256, "a Sealed chapter's lock on Contents, dark iron, dim; the game rings it in grey"),
            new ArtSlot("journal-flourish", 220, 20, 1024, "the gold flourish rule: a hairline each side of an eight-point star, two dots, diamond ends; above and below the Keeper's record, under Contents' title, on the title page"),
            new ArtSlot("journal-library-plan", 234, 350, 1024, "The Library Map's page: the Library's parchment plan (the mini-menu concept's Option C, moved into the journal, owner Oct 1): the round Grand Atrium, the Zodiac Wing top left, the Crystal Book Chamber top right; the game names the rooms you have woken"),
            new ArtSlot("sign-aries", 200, 200, 512, "Aries's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-taurus", 200, 200, 512, "Taurus's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-gemini", 200, 200, 512, "Gemini's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-cancer", 200, 200, 512, "Cancer's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-leo", 200, 200, 512, "Leo's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-virgo", 200, 200, 512, "Virgo's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-libra", 200, 200, 512, "Libra's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-scorpio", 200, 200, 512, "Scorpio's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-sagittarius", 200, 200, 512, "Sagittarius's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-capricorn", 200, 200, 512, "Capricorn's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-aquarius", 200, 200, 512, "Aquarius's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("sign-pisces", 200, 200, 512, "Pisces's picture on its journal page: one full-colour file, drawn as line art until the deck colours it (Illumination)"),
            new ArtSlot("shelves", 60, 180, 512, "the Atrium's shelves, mostly empty (60 x 120 in the room)"),
            new ArtSlot("furniture-covered", 120, 70, 512, "the covered furniture of the opening"),
            new ArtSlot("desk", 70, 30, 256, "the desk, uncovered, in the Atrium room"),
            new ArtSlot("lamp", 8, 22, 64, "a wall lamp, four in the Atrium; dark until its stage"),
            new ArtSlot("candle", 8, 20, 64, "a candle: the opening's one and the Chamber's nine; dark until lit (the Wing's, beside the Dial's step hint, was cut in Build Z: it read as \"!\", owner, Sept 30)"),
            new ArtSlot("door-open", 64, 128, 512, "an open door leaf filling a painted arch: the Wing's (72 x 128) and the Chamber's (62 x 128) in the Atrium, the doorways back (36 x 140 in the Wing, 30 x 124 in the Chamber)"),
            new ArtSlot("door-sealed", 64, 128, 512, "the sealed door leaf filling the Atrium's left arch (62 x 128)"),
            new ArtSlot("mechanism", 110, 110, 512, "the Chamber's old mechanism; turns one degree at the first Key"),
            new ArtSlot("crystal-book", 34, 70, 256, "one Crystal Book, seven times; brightens when it opens"),
            new ArtSlot("crystal-page", 26, 58, 256, "the page that rises from an open Book"),
            new ArtSlot("lock", 7, 7, 32, "one lock, three per Book; lights when filled"),
            new ArtSlot("keeper-key", 84, 40, 256, "the Keeper Key rising from the Dial"),
            // Build H (the Sept 17 lighting decision, Option C): one transparent golden-hour overlay per room, over the dormant background and
            // under everything else, faded by the Atrium stage from nothing at Stage 1 to full at Stages 5–6.
            new ArtSlot("atrium-light", 360, 800, 2048, "the Atrium's golden-hour light: shafts through the arches, glowing dust, warmth on the stone; over the background, faded by stage"),
            new ArtSlot("wing-light", 360, 800, 2048, "the Zodiac Wing room's golden-hour light, over the background, faded by stage"),
            new ArtSlot("atrium-grime", 360, 800, 2048, "the Atrium's grime (Build N): since Build Q the domed shell asleep, opaque (cold moonlight, a grimy dome, dust, cobwebs), drawn over the shell and faded out by the Atrium's level"),
            new ArtSlot("akit-banner-restored", 64, 166, 1024, "the Atrium kit: the long navy banner, restored (Build Q); drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-banner-worn", 66, 184, 1024, "the Atrium kit: the long navy banner, worn (Build Q); drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-bench-restored", 96, 52, 1024, "the Atrium kit (Build N): bench-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-bench-worn", 104, 40, 1024, "the Atrium kit (Build N): bench-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-bust-restored", 28, 76, 1024, "the Atrium kit (Build N): bust-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-bust-worn", 44, 68, 1024, "the Atrium kit (Build N): bust-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-candlestand-restored", 28, 90, 1024, "the Atrium kit (Build N): candlestand-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-candlestand-worn", 36, 88, 1024, "the Atrium kit (Build N): candlestand-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-chandelier-restored", 148, 218, 1024, "the Atrium kit: the armillary ring under the dome, restored (Build Q; the slot keeps the chandelier's name); drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-chandelier-worn", 86, 138, 1024, "the Atrium kit: the armillary ring, tarnished (Build Q; the slot keeps the chandelier's name); drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-chart-restored", 34, 46, 1024, "the Atrium kit (Build N): chart-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-chart-worn", 38, 48, 1024, "the Atrium kit (Build N): chart-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-desk-restored", 146, 96, 1024, "the Atrium kit (Build N): desk-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-desk-worn", 142, 78, 1024, "the Atrium kit (Build N): desk-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-door-closed", 70, 136, 1024, "the Atrium kit (Build N): door-closed; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-door-locked", 70, 136, 1024, "the Atrium kit (Build N): door-locked; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-door-open", 70, 136, 1024, "the Atrium kit (Build N): door-open; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-lamp-restored", 22, 62, 1024, "the Atrium kit: the wall lantern, lit (Build Q); drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-lamp-worn", 18, 42, 1024, "the Atrium kit: the wall lantern, dark (Build Q); drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-pennant-restored", 48, 132, 1024, "the Atrium kit: the narrow star pennant beside the statue's niche, restored (Build Q); drawn at half the file's size from SliceView.AtriumKit"),
            new ArtSlot("akit-pennant-worn", 42, 132, 1024, "the Atrium kit: the narrow star pennant, worn (Build Q); drawn at half the file's size from SliceView.AtriumKit"),
            new ArtSlot("akit-plant-restored", 36, 46, 1024, "the Atrium kit (Build N): plant-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-plant-worn", 34, 48, 1024, "the Atrium kit (Build N): plant-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-plate-clean", 64, 28, 1024, "the Atrium kit (Build N): plate-clean; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-plate-locked", 64, 28, 1024, "the Atrium kit (Build N): plate-locked; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-rug-restored", 316, 420, 1024, "the Atrium kit (Build N): rug-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-rug-worn", 200, 108, 1024, "the Atrium kit (Build N): rug-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-shelf-restored", 80, 300, 1024, "the Atrium kit (Build N): shelf-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-shelf-worn", 82, 300, 1024, "the Atrium kit (Build N): shelf-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("chamber-grime", 360, 800, 2048, "the Chamber's grime (Build O): dust, cobwebs, clouded crystals, a cold tint; fades out by Keys spent"),
            new ArtSlot("ckit-banner-restored", 26, 90, 1024, "the Chamber kit (Build O): banner-restored; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-banner-worn", 32, 100, 1024, "the Chamber kit (Build O): banner-worn; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-book-restored", 40, 66, 1024, "the Chamber kit (Build O): book-restored; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-book-worn", 40, 64, 1024, "the Chamber kit (Build O): book-worn; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-brazier-restored", 18, 56, 1024, "the Chamber kit (Build O): brazier-restored; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-brazier-worn", 22, 48, 1024, "the Chamber kit (Build O): brazier-worn; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-candle-restored", 8, 22, 1024, "the Chamber kit (Build O): candle-restored; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-candle-worn", 16, 34, 1024, "the Chamber kit (Build O): candle-worn; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-crystal-restored", 22, 54, 1024, "the Chamber kit (Build O): crystal-restored; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-crystal-worn", 28, 52, 1024, "the Chamber kit (Build O): crystal-worn; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-mechanism-restored", 70, 110, 1024, "the Chamber kit (Build O): mechanism-restored; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-mechanism-worn", 74, 112, 1024, "the Chamber kit (Build O): mechanism-worn; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-reliquary-restored", 30, 46, 1024, "the Chamber kit (Build O): reliquary-restored; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("ckit-reliquary-worn", 26, 38, 1024, "the Chamber kit (Build O): reliquary-worn; the Books stand on the altar, the rest from SliceView.ChamberKit"),
            new ArtSlot("wing-grime", 360, 800, 2048, "the Wing's grime (Build M): dust, cobwebs, cracks, a cold dark tint over the shell; fades out as the Keys restore the room"),
            new ArtSlot("kit-window-worn", 76, 192, 1024, "the Wing kit (Build M): the window, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-window-restored", 76, 192, 1024, "the Wing kit (Build M): the window, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-carpet-worn", 352, 110, 1024, "the Wing kit (Build M): the carpet, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-carpet-restored", 352, 110, 1024, "the Wing kit (Build M): the carpet, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-chandelier-worn", 84, 106, 1024, "the Wing kit (Build M): the chandelier, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-chandelier-restored", 84, 106, 1024, "the Wing kit (Build M): the chandelier, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-banner-worn", 26, 130, 1024, "the Wing kit (Build M): the banner, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-banner-restored", 26, 130, 1024, "the Wing kit (Build M): the banner, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-orrery-worn", 80, 106, 1024, "the Wing kit (Build M): the orrery, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-orrery-restored", 80, 106, 1024, "the Wing kit (Build M): the orrery, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-armillary-worn", 66, 80, 1024, "the Wing kit (Build M): the armillary, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-armillary-restored", 66, 80, 1024, "the Wing kit (Build M): the armillary, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-lectern-worn", 76, 90, 1024, "the Wing kit (Build M): the lectern, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-lectern-restored", 76, 90, 1024, "the Wing kit (Build M): the lectern, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-globe-worn", 34, 50, 1024, "the Wing kit (Build M): the globe, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-globe-restored", 34, 50, 1024, "the Wing kit (Build M): the globe, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-shelf-worn", 96, 290, 1024, "the Wing kit (Build M): the shelf, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-shelf-restored", 96, 290, 1024, "the Wing kit (Build M): the shelf, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-dial-worn", 186, 200, 1024, "the Wing kit (Build M): the dial, worn; since Build Z its eye rests closed (owner, Sept 30); drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit; Build AC: redrawn to the Cast Dial"),
            new ArtSlot("kit-dial-restored", 188, 206, 1024, "the Wing kit (Build M): the dial, restored; since Build Z its eye rests closed (owner, Sept 30); drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit; Build AC: redrawn to the Cast Dial"),
            new ArtSlot("kit-dial-worn-open", 186, 200, 1024, "the Wing kit (Build Z): the worn dial with its eye open; identical to kit-dial-worn outside the eye; the eye opens when the player taps the Dial (owner, Sept 30); Build AC: redrawn to the Cast Dial"),
            new ArtSlot("kit-dial-restored-open", 188, 206, 1024, "the Wing kit (Build Z): the restored dial with its eye open; identical to kit-dial-restored outside the eye; the eye opens when the player taps the Dial, even with every challenge done (owner, Sept 30); Build AC: redrawn to the Cast Dial"),
            new ArtSlot("kit-dial-bright", 188, 206, 1024, "the Wing kit (the wake-up, owner Oct 1; 86bcbn6w6 2d A): the dial bright and new, the Dial screen's Key 4 look on the stand; kit-dial-restored's canvas, its eye rests closed; until its file lands, kit-dial-restored stands in"),
            new ArtSlot("kit-dial-bright-open", 188, 206, 1024, "the Wing kit (the wake-up): the bright dial with its eye open; identical to kit-dial-bright outside the eye"),
            new ArtSlot("kit-telescope-worn", 66, 116, 1024, "the Wing kit (Build M): the telescope, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-telescope-restored", 66, 116, 1024, "the Wing kit (Build M): the telescope, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-table-worn", 108, 90, 1024, "the Wing kit (Build M): the table, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-table-restored", 108, 90, 1024, "the Wing kit (Build M): the table, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-candles-worn", 16, 40, 1024, "the Wing kit (Build M): the candles, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-candles-restored", 16, 40, 1024, "the Wing kit (Build M): the candles, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-books-worn", 68, 46, 1024, "the Wing kit (Build M): the books, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-books-restored", 68, 46, 1024, "the Wing kit (Build M): the books, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-chair-worn", 50, 82, 1024, "the Wing kit (Build M): the chair, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-chair-restored", 50, 82, 1024, "the Wing kit (Build M): the chair, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-plate-worn", 64, 28, 1024, "the Wing kit (Build M): the plate, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-plate-restored", 64, 28, 1024, "the Wing kit (Build M): the plate, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("chamber-light", 360, 800, 2048, "the Crystal Book Chamber's golden-hour light, over the background, faded by stage"),
            // Build P (note 3; the owner's B2 ruling, Sept 28): the chat box on the story screens, and Caspar unframed behind it, one pose per page.
            new ArtSlot("chat-box", 324, 240, 1024, "the chat box (Build P): the dark see-through fill and the gold frame in one image, sliced to each panel; on the story screens (Build P), the Atrium hub and the Chamber (Build R); the instrument screens draw a slim box in code instead"),
            new ArtSlot("chat-plate", 170, 22, 512, "the speaker's plate on the chat box's top-left edge (Build P); blank, the game writes the name"),
            // Batch 2: the button art by place (owner, Oct 1: 3b C, 3c C, 3d; Oct 2: "Buttons at 44 px, bronze on the Table and the Book too"; the art
            // lane's board on 86bcbn6w6). The pieces are cut from the approved art so they fit any button; the game writes the words (ButtonLook).
            new ArtSlot("btn-plate", 146, 56, 512, "the bronze plate: an action on an instrument (the Dial, the Elemental Table, the Book of Symbols, practice); its frame without its centre marks, nine-sliced (40 px in at 2x) to each button; the game engraves the word"),
            new ArtSlot("btn-plate-notch", 12, 12, 256, "the plate's top notch, laid at its top centre so the slice never stretches it"),
            new ArtSlot("btn-plate-diamond", 16, 18, 256, "the plate's bottom diamond, laid at its bottom centre, 1 px above its edge"),
            new ArtSlot("btn-arrow", 64, 56, 512, "Previous on an instrument, a bronze arrow (3b C: engraved arrows); Next is it mirrored; no word"),
            new ArtSlot("btn-rule-left", 100, 14, 512, "the way out of an instrument, gold lettering on a rule (3b C): the rule's left line; its tapered end (12 px) keeps its shape and the line stretches"),
            new ArtSlot("btn-rule-right", 100, 14, 512, "the rule's right line, its tapered end at the right"),
            new ArtSlot("btn-rule-centre", 16, 14, 256, "the rule's centre: its diamond and the gaps beside it, at the rule's centre"),
            new ArtSlot("caspar-calm", 264, 468, 1024, "Caspar from the waist up behind the chat box (Build P), calm: the default, short instructions; all six poses share one registration"),
            new ArtSlot("caspar-explain", 264, 468, 1024, "Caspar behind the chat box (Build P), explaining: teaching, the lessons, the history (the opening's fourth page, the return's second)"),
            new ArtSlot("caspar-warm", 264, 468, 1024, "Caspar behind the chat box (Build P), warm: welcomes, thanks, praise (the opening's second page)"),
            new ArtSlot("caspar-wry", 264, 468, 1024, "Caspar behind the chat box (Build P), wry: his dry humour (the opening's first page)"),
            new ArtSlot("caspar-moved", 264, 468, 1024, "Caspar behind the chat box (Build P), moved: wonder at the Library waking (the return's first page, the Keeper Key)"),
            new ArtSlot("caspar-solemn", 264, 468, 1024, "Caspar behind the chat box (Build P), solemn: honesty and the backstory (the opening's third page)"),
            // The opening scene (owner, Oct 8, 86bcfhmha): twelve frames for the prologue, then the orb and its stars over the opening's questions
            new ArtSlot("prologue-city", 360, 800, 2048, "a full 360 x 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 1, the city: a New York street at 3 a.m. (shown by the picture only, no clock or text), empty, one lit window high on a building; the push-in holds on the window (measured on the final art: 58.1% across, 23.7% down)"),
            new ArtSlot("prologue-desk", 360, 800, 2048, "a full 360 x 800 frame of the opening scene, shown as a framed comic panel: a window on part of the frame (SliceView.PrologueShots sets which part, from the art); shot 2's first panel (on screen 54 to 354 down, showing the frame from 22.5% to 60% down): wide on the Keeper at his desk in his room, headphones on, writing; shot 3 opens on the whole frame, then crossfades to prologue-light, so the two line up"),
            new ArtSlot("prologue-notebook", 360, 800, 2048, "a full 360 x 800 frame of the opening scene, shown as a framed comic panel: a window on part of the frame (SliceView.PrologueShots sets which part, from the art); shot 2's second panel (on screen 374 to 674 down, showing the frame from 34% to 72% down): close on his hand writing in the plain notebook (D2's left half)"),
            new ArtSlot("prologue-light", 360, 800, 2048, "a full 360 x 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 3: the desk frame again, the same framing as prologue-desk, white-gold light flooding the room and his page from the window"),
            new ArtSlot("prologue-look", 360, 800, 2048, "a full 360 x 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 3: close on his eyes looking up toward the light, headphones still on"),
            new ArtSlot("prologue-headphones", 360, 800, 2048, "a full 360 x 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 3: the same close as prologue-look, his hand pulling the headphones down (a frame swap: the two must line up)"),
            new ArtSlot("prologue-street-above", 360, 800, 2048, "a full 360 x 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 4: looking down from his window at the empty street, Caspar small below with his back turned in a fading ring of light; the pan runs from the top of the frame to the bottom and a little left, ending with him and the whole ring in view (measured: 47.5% across, 69% down)"),
            new ArtSlot("prologue-puzzled", 360, 800, 2048, "a full 360 x 800 frame of the opening scene, shown as a framed comic panel: a window on part of the frame (SliceView.PrologueShots sets which part, from the art); shot 5's panel (on screen 205 to 595 down, over shot 4 dimmed, showing the frame from 17% to 66% down): his face at the window, puzzled"),
            new ArtSlot("prologue-caspar-back", 360, 800, 2048, "a full 360 x 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 6: street level, Caspar in his canon coat with the hood of his own navy coat fabric and its brass clasp (B1), his back to us; the turn trio must line up"),
            new ArtSlot("prologue-caspar-turn", 360, 800, 2048, "a full 360 x 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 6: Caspar turned three-quarter toward us, the same framing as prologue-caspar-back"),
            new ArtSlot("prologue-caspar-face", 360, 800, 2048, "a full 360 x 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 6: Caspar facing us, the same framing as prologue-caspar-turn, his eyes glowing white-gold from the crossing (ruling 2: this scene only, the eyes only; the owner, Oct 8: the glow does not fade); a slow push-in toward his eyes starts on it (measured: 56.6% across, 21.3% down)"),
            new ArtSlot("prologue-caspar-eyes", 360, 800, 2048, "a full 360 x 800 frame of the opening scene (owner, Oct 8, 86bcfhmha; the softer look, ruling 10); the game moves only inside it (push-in or pan at scale 1 or more, crossfades, cuts); shot 6's end (the owner, Oct 8: the intro ends zooming into his glowing eyes, then a flash into nothingness): an extreme close-up of Caspar's glowing white-gold eyes under the hood; the game crossfades to it from prologue-caspar-face mid push-in and keeps pushing in on the glow (measured: 50.7% across, 47.1% down), then the white blooms out of the glow"),
            new ArtSlot("orb", 120, 120, 512, "the orb (C2: dark stone with gold seams), cut out to transparency, floating over the question on the opening's name and birth screens with a gentle bob (none under reduced motion); it has no name, lore or lines (ruling 3)"),
            new ArtSlot("orb-star", 16, 16, 64, "one faint star, cut out to transparency: one fades in around the orb for each step answered in the opening, eight places at most; they leave with the white light"),
        };
        public static readonly SoundSlot[] Sounds =
        {
            new SoundSlot("step", "one wheel detent, by button, drag, keyboard, or a count beat; a tile or cell picked on the table"),
            new SoundSlot("seal", "a Seal or a tap answer accepted"),
            new SoundSlot("miss", "a Seal or a tap answer rejected"),
            new SoundSlot("key", "a Key earned; a Key spent on a lock"),
            new SoundSlot("page", "Caspar's page turned; the book opened or closed; a Book's page rising"),
            new SoundSlot("door", "a doorway crossed"),
            new SoundSlot("ambient", "the room loop, from the first screen"),
        };
        public const string TestSet = "test";
        public static ArtSlot Find(string name) => Art.FirstOrDefault(a => a.Name == name);
        public static SoundSlot FindSound(string name) => Sounds.FirstOrDefault(s => s.Name == name);

        // ---- which set, and whether the style page was asked for ----
        static string requestedSet; static bool? requestedStyle; static string resolvedSet; static bool resolvedStyle, resolved;
        // A test fixture (or the Editor menu) chooses the set and the page in play; null clears the request.
        public static void Request(string set, bool? style)
        {
            requestedSet = set; requestedStyle = style; resolved = false; images.Clear(); clips.Clear();
        }
        public static string Set { get { Resolve(); return requestedSet ?? resolvedSet; } }
        public static bool StyleRequested { get { Resolve(); return requestedStyle ?? resolvedStyle; } }
        static void Resolve()
        {
            if (resolved) return; resolved = true;
            ParseQuery(Application.absoluteURL, out resolvedSet, out resolvedStyle);
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-artSet" && i + 1 < args.Length) resolvedSet = args[i + 1];
                if (args[i] == "-style") resolvedStyle = true;
            }
#if UNITY_EDITOR
            if (!Application.isBatchMode)
            {
                string menuSet = UnityEditor.SessionState.GetString("AscendantArtSet", ""); if (menuSet != "") resolvedSet = menuSet;
                if (UnityEditor.SessionState.GetBool("AscendantStylePage", false)) resolvedStyle = true;
            }
#endif
        }
        // The page's URL chooses the set (?art=test) and the style page (?style, or ?style=test for both). Pure, for the checks.
        public static void ParseQuery(string url, out string set, out bool style)
        {
            set = ""; style = false;
            if (string.IsNullOrEmpty(url)) return;
            int mark = url.IndexOf('?'); if (mark < 0) return;
            string query = url.Substring(mark + 1); int hash = query.IndexOf('#'); if (hash >= 0) query = query.Substring(0, hash);
            foreach (var pair in query.Split('&'))
            {
                int eq = pair.IndexOf('='); string key = eq < 0 ? pair : pair.Substring(0, eq), value = eq < 0 ? "" : pair.Substring(eq + 1);
                if (key == "art") set = Clean(value);
                else if (key == "style") { style = true; if (value != "") set = Clean(value); }
            }
        }
        static string Clean(string value) => new string(value.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        // ---- loading: one lookup per slot per set; null means no file ----
        static readonly Dictionary<string, Sprite> images = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static string PathOf(string kind, string slot) => kind + "/" + (string.IsNullOrEmpty(Set) ? "" : Set + "/") + slot;
        public static Sprite Image(string slot)
        {
            string key = Set + "/" + slot; if (images.TryGetValue(key, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>(PathOf("Art", slot));
            if (sprite == null) { var texture = Resources.Load<Texture2D>(PathOf("Art", slot)); if (texture != null) sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect); }
            images[key] = sprite; return sprite;
        }
        public static AudioClip Clip(string slot)
        {
            string key = Set + "/" + slot; if (clips.TryGetValue(key, out var cached)) return cached;
            var clip = Resources.Load<AudioClip>(PathOf("Audio", slot)); clips[key] = clip; return clip;
        }
        public static string Source(string slot) => Image(slot) != null ? (string.IsNullOrEmpty(Set) ? "file" : Set + " set") : "placeholder";
        public static string SoundSource(string slot) => Clip(slot) != null ? (string.IsNullOrEmpty(Set) ? "file" : Set + " set") : "silent";
        public static int ArtFiles => Art.Count(a => Image(a.Name) != null);
        public static int SoundFiles => Sounds.Count(s => Clip(s.Name) != null);

        // ---- dressing: a placeholder Image takes its slot's file when there is one ----
        public sealed class Dressing { public string Slot; public Image Image; }
        public static readonly List<Dressing> Dressed = new List<Dressing>(); // test evidence: which placeholders took a file
        public static bool Dress(Image image, string slot)
        {
            var sprite = Image(slot);
            Dressed.RemoveAll(d => d.Image == null); Dressed.Add(new Dressing { Slot = slot, Image = image });
            if (sprite == null) return false;
            image.sprite = sprite; image.type = UnityEngine.UI.Image.Type.Simple; image.preserveAspect = false; image.color = Color.white;
            return true;
        }
        public static int DressedCount { get { Dressed.RemoveAll(d => d.Image == null); return Dressed.Count(d => d.Image.sprite != null); } }
        public static bool IsDressed(string slot) { Dressed.RemoveAll(d => d.Image == null); return Dressed.Any(d => d.Slot == slot && d.Image.sprite != null && d.Image.isActiveAndEnabled); }
        // A state color: the placeholder's own color, or the file at a brightness (the placeholder's alpha carries over).
        public static void Paint(Image image, Color placeholder, float brightness)
        {
            if (image == null) return;
            image.color = image.sprite != null ? new Color(brightness, brightness, brightness, placeholder.a) : placeholder;
        }
        // Build R (the owner's B2 ruling, and "only the chat box" where the player acts, Sept 28): a Caspar panel dressed as the chat box.
        // The frame and its see-through fill are sliced to the panel's own size (the file is drawn at 2x), the old CASPAR label goes,
        // and the speaker's plate sits on the box's top-left edge with the name written on it. No chat-box file, no change.
        public static bool DressChatBox(Image panel, Text oldLabel, Font font)
        {
            if (Image("chat-box") == null) return false;
            Dress(panel, "chat-box"); panel.type = UnityEngine.UI.Image.Type.Sliced; panel.pixelsPerUnitMultiplier = 2;
            if (oldLabel != null) oldLabel.gameObject.SetActive(false);
            var plate = new GameObject("Caspar plate", typeof(RectTransform)).GetComponent<RectTransform>(); plate.SetParent(panel.transform, false);
            plate.anchorMin = plate.anchorMax = new Vector2(0, 1); plate.pivot = new Vector2(0, .5f); plate.anchoredPosition = new Vector2(12, 0); plate.sizeDelta = new Vector2(170, 22);
            var plateImage = plate.gameObject.AddComponent<Image>(); plateImage.raycastTarget = false; plateImage.color = new Color(.18f, .12f, .07f); Dress(plateImage, "chat-plate");
            var nameRect = new GameObject("CASPAR", typeof(RectTransform)).GetComponent<RectTransform>(); nameRect.SetParent(plate, false);
            nameRect.anchorMin = Vector2.zero; nameRect.anchorMax = Vector2.one; nameRect.offsetMin = new Vector2(10, 2); nameRect.offsetMax = new Vector2(-10, -2);
            var name = nameRect.gameObject.AddComponent<Text>(); name.font = font; name.text = "CASPAR"; name.fontSize = 12; name.fontStyle = FontStyle.Bold;
            name.alignment = TextAnchor.MiddleCenter; name.color = new Color(.91f, .76f, .48f); name.raycastTarget = false;
            return true;
        }
        // Build R (owner, Sept 29): the instrument screens (the Dial, the Book of Symbols, the Elemental Table) take a slim box, not the chat box:
        // a flat dark panel with a hairline gold border and CASPAR small at its top left, pinned at its top and fitted to the line (FitBox).
        public const float InstrumentHead = 32, InstrumentFoot = 12, InstrumentMin = 60, InstrumentMore = 22;
        // Build S: a page with more to come keeps a row at the bottom for its Continue.
        public static float InstrumentBoxHeight(float lineHeight, float max, bool more = false) => Mathf.Clamp(InstrumentHead + lineHeight + InstrumentFoot + (more ? InstrumentMore : 0), Mathf.Min(InstrumentMin, max), max);
        static Sprite instrumentBox, dialBox;
        public static readonly Color DialVoice = new Color(.39f, .65f, .88f); // Build Z: the Dial's own voice is sea blue (#63A6E0, the Water hue), Caspar's gold
        public static Sprite InstrumentBoxSprite(bool dial = false) // Build U: the Settings box wears it too; Build Z: the Dial's voice wears a sea-blue edge
        {
            if (!dial && instrumentBox != null) return instrumentBox; if (dial && dialBox != null) return dialBox;
            const int size = 32; const float radius = 10, stroke = 2; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color fill = new Color(.086f, .078f, .094f, .96f), gold = dial ? new Color(DialVoice.r, DialVoice.g, DialVoice.b, .6f) : new Color(.84f, .69f, .38f, .55f);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                // the distance from the pixel's centre to the rounded rectangle's edge, inset half a pixel: negative inside
                float px = Mathf.Abs(x + .5f - size / 2f) - (size / 2f - .5f - radius), py = Mathf.Abs(y + .5f - size / 2f) - (size / 2f - .5f - radius);
                float d = new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0) - radius;
                float inside = Mathf.Clamp01(.5f - d), line = Mathf.Clamp01(stroke / 2 + .5f - Mathf.Abs(d + stroke / 2));
                Color c = Color.Lerp(fill, gold, line / Mathf.Max(inside, .0001f)); c.a = Mathf.Max(fill.a * inside, gold.a * line);
                texture.SetPixel(x, y, c);
            }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
            if (dial) dialBox = sprite; else instrumentBox = sprite; return sprite;
        }
        static Sprite eyeMark;
        // Build Z: the small eye beside THE CELESTIAL DIAL on the plate: an almond outline and a pupil, drawn here (the web font has no eye glyph).
        public static Sprite EyeMarkSprite()
        {
            if (eyeMark != null) return eyeMark;
            const int w = 40, h = 24; var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float u = (x + .5f - w / 2f) / (w / 2f - 1), v = (y + .5f - h / 2f) / (h / 2f - 1); // -1..1
                float lid = Mathf.Abs(v) - (1 - u * u) * .95f; // the almond: two arcs meeting at the corners
                float edge = Mathf.Clamp01(1.4f - Mathf.Abs(lid) * 9f), pupil = Mathf.Clamp01((.34f - new Vector2(u * 1.7f, v).magnitude) * 8f);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(lid < 0 ? edge : edge, pupil)));
            }
            texture.Apply(); return eyeMark = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100);
        }
        public static void DressInstrumentBox(Image panel, Text oldLabel, Text line, Font font)
        {
            var rect = panel.rectTransform; float max = rect.sizeDelta.y;
            rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition += new Vector2(0, max / 2); // pinned at its top edge
            panel.sprite = InstrumentBoxSprite(); panel.type = UnityEngine.UI.Image.Type.Sliced; panel.pixelsPerUnitMultiplier = 2; panel.color = Color.white;
            if (oldLabel != null) oldLabel.gameObject.SetActive(false);
            var nameRect = new GameObject("CASPAR", typeof(RectTransform)).GetComponent<RectTransform>(); nameRect.SetParent(rect, false);
            nameRect.anchorMin = nameRect.anchorMax = nameRect.pivot = new Vector2(0, 1); nameRect.anchoredPosition = new Vector2(14, -7); nameRect.sizeDelta = new Vector2(rect.sizeDelta.x - 28, 16); // Build V: wide enough for the Dial's own name
            var name = nameRect.gameObject.AddComponent<Text>(); name.font = font; name.text = "C A S P A R"; name.fontSize = 11; name.fontStyle = FontStyle.Bold; // spaced: the legacy Text has no letter spacing
            name.alignment = TextAnchor.UpperLeft; name.color = new Color(.84f, .69f, .38f); name.raycastTarget = false;
            var rule = new GameObject("Rule", typeof(RectTransform)).GetComponent<RectTransform>(); rule.SetParent(rect, false);
            rule.anchorMin = rule.anchorMax = rule.pivot = new Vector2(0, 1); rule.anchoredPosition = new Vector2(14, -24); rule.sizeDelta = new Vector2(60, 1);
            var ruleImage = rule.gameObject.AddComponent<Image>(); ruleImage.color = new Color(.84f, .69f, .38f, .55f); ruleImage.raycastTarget = false;
            var lr = line.rectTransform; lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0, 1); lr.anchoredPosition = new Vector2(14, -InstrumentHead);
            lr.sizeDelta = new Vector2(rect.sizeDelta.x - 28, max - InstrumentHead - InstrumentFoot); line.alignment = TextAnchor.UpperLeft;
            var fit = panel.gameObject.AddComponent<FitBox>(); fit.Name = name; fit.Line = line; fit.Max = max + InstrumentMore; // Build S: room for the page's Continue
            var eyeRect = new GameObject("Dial's eye", typeof(RectTransform)).GetComponent<RectTransform>(); eyeRect.SetParent(rect, false); // Build Z: the Dial's mark, shown when it speaks
            eyeRect.anchorMin = eyeRect.anchorMax = eyeRect.pivot = new Vector2(0, 1); eyeRect.anchoredPosition = new Vector2(14, -9); eyeRect.sizeDelta = new Vector2(16, 10);
            var eye = eyeRect.gameObject.AddComponent<Image>(); eye.sprite = EyeMarkSprite(); eye.color = DialVoice; eye.raycastTarget = false; eyeRect.gameObject.SetActive(false);
            fit.Panel = panel; fit.Rule = ruleImage; fit.Eye = eye; fit.NameX = 14;
            // Build S (owner, APK playtest, Sept 29): a long line turns in pages; Continue sits at the box's bottom right while more is to come.
            var moreRect = new GameObject("Caspar Continue", typeof(RectTransform)).GetComponent<RectTransform>(); moreRect.SetParent(rect, false);
            moreRect.anchorMin = moreRect.anchorMax = moreRect.pivot = new Vector2(1, 0); moreRect.anchoredPosition = new Vector2(-8, 4); moreRect.sizeDelta = new Vector2(96, 26);
            var hit = moreRect.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, .001f); hit.canvasRenderer.cullTransparentMesh = false; // a see-through button still takes taps
            var more = moreRect.gameObject.AddComponent<Button>(); more.targetGraphic = hit; more.onClick.AddListener(fit.Turn);
            ButtonLook.HitArea(more, 0, ButtonLook.MinTarget); // batch 2 (3d): the link takes taps over 44 px of height; it reads at 26
            var moreTextRect = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>(); moreTextRect.SetParent(moreRect, false);
            moreTextRect.anchorMin = Vector2.zero; moreTextRect.anchorMax = Vector2.one; moreTextRect.offsetMin = new Vector2(0, 0); moreTextRect.offsetMax = new Vector2(-6, 0);
            var moreText = moreTextRect.gameObject.AddComponent<Text>(); moreText.font = font; moreText.text = "Continue"; moreText.fontSize = 12; moreText.fontStyle = FontStyle.Bold;
            moreText.alignment = TextAnchor.MiddleRight; moreText.color = new Color(.84f, .69f, .38f); moreText.raycastTarget = false; // Latin-1 only: the web font has no arrow glyphs
            fit.More = more; moreRect.gameObject.SetActive(false);
        }
    }
}
