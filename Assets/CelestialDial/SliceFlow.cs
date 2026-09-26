using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum SliceScreen { Identity, Birth, Atrium, Wing, AtriumReturn, Chamber, Hub, Practice, WingRoom, Book, Grid, ChamberRoom, Journal }
    public enum ReviewMode { Dial, Tap, Glyph, TapModality, DialModality }
    public enum JournalView { Contents, Section, Sign } // Build J: the journal's three kinds of page

    [Serializable]
    public sealed class ReviewTask { public int seat; public int mode; public int misses; public bool done; public bool correct; public ReviewMode Mode => (ReviewMode)mode; }

    // The locked v0.1 flow (Q06) plus the locked v0.2 loop (Q05) as amended Sept 15 and 17 (Build F): the Atrium's one entrance,
    // the practice fork on the Dial (one entry = one sitting), the three-strikes gate, the journal in the inventory,
    // Unit 1.1 continuation, local save. Pure state, no Unity types.
    public sealed class SliceFlow
    {
        public SliceScreen Screen { get; private set; } = SliceScreen.Identity;
        public string PlayerName { get; private set; } = "";
        public string BirthChoice { get; private set; } = "";
        public string Note { get; private set; } = "";
        public int SunSign { get; private set; } = -1;
        public bool HasSunSign => SunSign >= 0;
        public bool KeyRevealed { get; private set; }
        public bool KeyInserted { get; private set; }
        public bool Ended { get; private set; }
        public int LocksFilled { get; private set; }
        public const int LocksPerBook = 3;
        public const int Books = 7;
        // v0.2
        public readonly ReviewDeck Deck = new ReviewDeck();
        public int AtriumStage { get; private set; } = 1;   // 1 Forgotten, 2 Stirring, 3 one more change
        public bool WheelComplete { get; private set; }
        // v0.3
        public int Keys { get; private set; } = 0;
        public int GlyphStage { get; private set; }   // 0 none, 1 Part A done, 2 Key 2 earned
        public int GlyphIndex { get; private set; }
        public bool GlyphsStarted { get; private set; }
        public int CleanRuns { get; private set; }     // v0.3 revision, build 3: clean symbol replays after Key 2
        public bool ModalitiesStarted { get; private set; } // Build A
        public void StartModalities() { if (!ModalitiesStarted) { ModalitiesStarted = true; Deck.IntroduceAll(Sitting, ItemKind.Modality); Logged?.Invoke("modality_unit_started"); } }
        public void RecordModalityAnswer(int seat, bool eligible) { Deck.RecordLesson(seat, eligible, Sitting, ItemKind.Modality); }
        // Build B: the table wakes once the modality unit is complete; its twelve sign → cell items enter the deck as data (owner, Sept 15).
        public bool ModalitiesComplete { get; private set; }
        public void MarkModalitiesComplete() { if (!ModalitiesComplete) { ModalitiesComplete = true; Logged?.Invoke("modalities_complete"); } }
        public bool GridStarted { get; private set; }
        public void StartGrid() { if (!GridStarted) { GridStarted = true; Deck.IntroduceAll(Sitting, ItemKind.Grid); Logged?.Invoke("grid_unit_started"); } }
        public void RecordGridAnswer(int seat, bool eligible) { Deck.RecordLesson(seat, eligible, Sitting, ItemKind.Grid); }
        public void MarkKey3() { if (Keys < 3) Keys = 3; } // the table logs key3_earned once
        // Build C: the last pattern and the builder on the Dial; six opposite-pair items enter the deck as data (owner, Sept 15).
        public bool OppositesStarted { get; private set; }
        public void StartOpposites() { if (!OppositesStarted) { OppositesStarted = true; Deck.IntroduceAll(Sitting, ItemKind.Opposite, Zodiac.OppositePairs); Logged?.Invoke("opposites_unit_started"); } }
        public void RecordOppositeAnswer(int seat, bool eligible) { Deck.RecordLesson(Zodiac.PairOf(seat), eligible, Sitting, ItemKind.Opposite); }
        public void MarkKey4() { if (Keys < 4) Keys = 4; } // the lesson logs key4_earned once
        // Build D: the finished loop. Every Key earned is spent in the Chamber; Book 1 has three locks, the fourth Key starts Book 2.
        // The Atrium restores one step per Key spent (stages 4–6), on the return from the Chamber (04 First Reward: spending is the beat).
        public int KeysSpent => LocksFilled;
        public int KeysInHand => Math.Max(0, Keys - LocksFilled);
        public int BooksOpen => LocksFilled / LocksPerBook;
        public bool WingWhole => LocksFilled >= 4;                 // the Wing milestone: four Keys spent
        public bool AtChamberRoom => Screen == SliceScreen.ChamberRoom;
        public bool CanEnterChamber => AtHub && KeyInserted;       // the first visit is the Continue bookend; after it the doorway is a room
        public bool EnterChamber()
        {
            if (!CanEnterChamber) return false;
            Screen = SliceScreen.ChamberRoom; Note = ""; Walk.Enter(Room.Chamber, "atrium-door"); Logged?.Invoke("screen_entered:chamberroom"); return true;
        }
        public bool CanSpend => AtChamberRoom && KeysInHand > 0;
        public bool SpendKey()
        {
            if (!CanSpend) return false;                            // nothing accepts a Key the player does not have
            LocksFilled++; Logged?.Invoke("key_spent");
            if (LocksFilled % LocksPerBook == 0) Logged?.Invoke("book_opened:" + BooksOpen);
            if (WingWhole) Logged?.Invoke("wing_whole");
            return true;
        }
        public bool LeaveChamber()
        {
            if (!AtChamberRoom) return false;
            Screen = SliceScreen.Hub; Note = "";
            if (LocksFilled >= 2 && AtriumStage < 4) { AtriumStage = 4; Logged?.Invoke("atrium_stage_4"); }
            if (LocksFilled >= 3 && AtriumStage < 5) { AtriumStage = 5; Logged?.Invoke("atrium_stage_5"); }
            if (LocksFilled >= 4 && AtriumStage < 6) { AtriumStage = 6; Logged?.Invoke("atrium_stage_6"); }
            Walk.Enter(Room.Atrium, "chamber-door"); Logged?.Invoke("screen_entered:hub"); return true;
        }
        public bool CanOpenGrid => AtWingRoom && ModalitiesComplete;
        public bool EnterGrid()
        {
            if (!CanOpenGrid) return false;
            Screen = SliceScreen.Grid; Note = ""; Logged?.Invoke("screen_entered:grid"); return true;
        }
        public bool LeaveGrid()
        {
            if (Screen != SliceScreen.Grid) return false;
            Screen = SliceScreen.WingRoom; Note = ""; Logged?.Invoke("screen_entered:wingroom"); return true;
        }
        public bool TouchDarkGrid()
        {
            if (!AtWingRoom || ModalitiesComplete) return false;
            Note = "grid-dark"; Logged?.Invoke("grid_dark_touched"); return true;
        }
        public void RecordCleanRun() { CleanRuns++; Logged?.Invoke("clean_run_recorded"); }
        public bool V03Complete => Keys >= 2 && AtriumStage >= 4;
        // v0.4: the marker that walks the Atrium and the Wing room (Q06 phase 2).
        public readonly Walker Walk = new Walker();
        public readonly List<ReviewTask> ReviewQueue = new List<ReviewTask>();
        public int ReviewIndex { get; private set; }
        public ReviewTask CurrentReview => ReviewIndex < ReviewQueue.Count ? ReviewQueue[ReviewIndex] : null;
        public string PracticeSummary { get; private set; } = "";
        public event Action<string> Logged;
        readonly Func<int> random;
        public SliceFlow() : this(null) { }
        public SliceFlow(Func<int> randomSeat)
        {
            random = randomSeat ?? (() => new Random().Next(12));
            Deck.Logged += n => Logged?.Invoke(n);
            Walk.Logged += n => Logged?.Invoke(n);
        }
        // No in-game time (owner, Sept 13): the deck counts sittings. The sitting rule (owner, Sept 17): one entry to the practice
        // fork on the Dial is one sitting, due items or not, so the ladder can never stall (the Sept 16 audit's deadlock).
        public int Sittings { get; private set; }
        public int Sitting => Sittings;
        public string DisplayName => string.IsNullOrEmpty(PlayerName) ? "Keeper" : PlayerName;
        public void SetName(string name) { PlayerName = (name ?? "").Trim(); }
        public void ChooseBirth(string choice)
        {
            if (Screen != SliceScreen.Birth) return;
            BirthChoice = choice; SunSign = -1; Note = "";
            if (choice == "unknown") { SunSign = Zodiac.Wrap(random()); Note = "Then I will choose one for you.\nYour sun sign is " + Zodiac.Seats[SunSign].Name + "."; }
            Logged?.Invoke("birth_choice:" + choice);
        }
        public bool SetBirthDate(int month, int day)
        {
            if (Screen != SliceScreen.Birth || BirthChoice != "chart") return false;
            int seat = Zodiac.SunSign(month, day);
            if (seat < 0) return false;
            SunSign = seat; Note = "Your sun sign is " + Zodiac.Seats[seat].Name + "."; Logged?.Invoke("sun_sign_derived"); return true;
        }
        public bool SetKnownSign(int seat)
        {
            if (Screen != SliceScreen.Birth || BirthChoice != "known" || seat < 0 || seat > 11) return false;
            SunSign = seat; Note = "Your sun sign is " + Zodiac.Seats[seat].Name + "."; Logged?.Invoke("sun_sign_entered"); return true;
        }
        public bool CanContinue =>
            Screen == SliceScreen.Identity || (Screen == SliceScreen.Birth && HasSunSign) ||
            Screen == SliceScreen.Atrium || (Screen == SliceScreen.Wing && KeyRevealed && AtriumStage == 1) || Screen == SliceScreen.AtriumReturn ||
            (Screen == SliceScreen.Chamber && Ended);
        public bool Continue()
        {
            if (!CanContinue) return false;
            Screen = Screen == SliceScreen.Identity ? SliceScreen.Birth :
                Screen == SliceScreen.Birth ? SliceScreen.Atrium :
                Screen == SliceScreen.Atrium ? SliceScreen.Wing :
                Screen == SliceScreen.Wing ? SliceScreen.AtriumReturn :
                Screen == SliceScreen.AtriumReturn ? SliceScreen.Chamber : SliceScreen.Hub;
            if (Screen == SliceScreen.Hub) { Note = ""; if (AtriumStage < 2) { AtriumStage = 2; Deck.IntroduceAll(Sitting); } Walk.Enter(Room.Atrium, "entry"); }
            Logged?.Invoke("screen_entered:" + Screen);
            return true;
        }
        public bool RevealKey()
        {
            if (Screen != SliceScreen.Wing || KeyRevealed) return false;
            KeyRevealed = true; MarkKeyEarned(); Logged?.Invoke("key_revealed"); return true;
        }
        public bool InsertKey()
        {
            if (Screen != SliceScreen.Chamber || !KeyRevealed || KeyInserted) return false;
            KeyInserted = true; LocksFilled = 1; Logged?.Invoke("key_inserted"); return true;
        }
        public bool End()
        {
            if (!KeyInserted || Ended) return false;
            Ended = true; Logged?.Invoke("prototype_ended"); return true;
        }
        // ---- v0.2: the return ----
        public bool AtHub => Screen == SliceScreen.Hub;
        public bool CanEnterWing => AtHub;
        // v0.4: the Wing doorway leads into the Wing room; the Dial is a point of interest inside it.
        public bool EnterWing()
        {
            if (!CanEnterWing) return false;
            Screen = SliceScreen.WingRoom; Note = ""; Walk.Enter(Room.Wing, "atrium-door"); Logged?.Invoke("screen_entered:wingroom"); return true;
        }
        public bool AtWingRoom => Screen == SliceScreen.WingRoom;
        public bool EnterDial()
        {
            if (!AtWingRoom) return false;
            Screen = SliceScreen.Wing; Note = ""; Logged?.Invoke("screen_entered:wing"); return true; // a dark-object note does not outlive the visit
        }
        // v0.3 revision (Sept 13 lock): Part A, the symbol-naming cards, lives in a book on the Wing's bookshelf.
        public bool CanOpenBook => AtWingRoom && WheelComplete;
        public bool EnterBook()
        {
            if (!CanOpenBook) return false;
            Screen = SliceScreen.Book; Note = ""; Logged?.Invoke("screen_entered:book"); return true;
        }
        public bool LeaveBook()
        {
            if (Screen != SliceScreen.Book) return false;
            Screen = SliceScreen.WingRoom; Logged?.Invoke("screen_entered:wingroom"); return true;
        }
        public bool TouchDarkShelf()
        {
            if (!AtWingRoom || WheelComplete) return false;
            Note = "shelf-dark"; Logged?.Invoke("shelf_dark_touched"); return true;
        }
        public bool LeaveDial()
        {
            if (Screen != SliceScreen.Wing || AtriumStage < 2) return false;
            Screen = SliceScreen.WingRoom; Logged?.Invoke("screen_entered:wingroom"); return true;
        }
        public bool LeaveWing()
        {
            if (Screen != SliceScreen.WingRoom || AtriumStage < 2) return false;
            Screen = SliceScreen.Hub; Note = "";
            if (WheelComplete && AtriumStage < 3) { AtriumStage = 3; Logged?.Invoke("atrium_stage_3"); }
            // Stages 4–6 follow Keys spent, on the return from the Chamber (Build D); Keys earned show in hand until then.
            Walk.Enter(Room.Atrium, "wing-door"); Logged?.Invoke("screen_entered:hub"); return true;
        }
        // Sealed doors only say they are sealed (Q06 phase 2, decision 2).
        public bool TouchSealedDoor()
        {
            if (!AtHub) return false;
            Note = "Sealed. Gather more Keys, acolyte, and perhaps the door shall stir and reveal what lies beyond."; Logged?.Invoke("sealed_door_touched"); return true; // owner (worksheet section 6)
        }
        public bool ApproachCaspar()
        {
            if (!AtHub) return false;
            Note = "Caspar raises his gaze from the desk and watches you approach, saying nothing."; Logged?.Invoke("caspar_approached"); return true; // owner (worksheet section 6)
        }
        public void MarkWheelComplete() { if (!WheelComplete) { WheelComplete = true; Logged?.Invoke("wheel_completed"); } }
        public void MarkKeyEarned() { if (Keys < 1) { Keys = 1; } }
        public void StartGlyphs() { if (!GlyphsStarted) { GlyphsStarted = true; Deck.IntroduceAll(Sitting, ItemKind.Glyph); Logged?.Invoke("glyph_unit_started"); } }
        public void SetGlyphProgress(int stage, int index) { GlyphStage = stage; GlyphIndex = index; }
        public void MarkKey2() { if (Keys < 2) { Keys = 2; GlyphStage = 2; } } // the lesson logs key2_earned once
        public void RecordGlyphAnswer(int seat, bool eligible) { Deck.RecordLesson(seat, eligible, Sitting, ItemKind.Glyph); }
        public void RecordLessonAnswer(int seat, bool eligible) { if (AtriumStage >= 2 || Deck.Items[seat].entered) Deck.RecordLesson(seat, eligible, Sitting); else Deck.RecordLesson(seat, eligible, Sitting); }
        public int DueCount => Deck.DueForReview(Sitting).Count; // grid and opposite items are data only (no practice form yet)
        // ---- Build F: the practice fork on the Dial (Sept 15 ruling), the sitting rule (Sept 17), the three-strikes gate, the journal ----
        public const int StrikeLimit = 3;                          // three wrong answers across one practice close the instrument (owner, Sept 15)
        public int Strikes { get; private set; }                  // session only, never saved; fresh on every entry
        public bool PracticeAvailable => Deck.Items.Any(i => i.entered && ReviewDeck.Reviewable(i.Kind));
        public bool AtDial => Screen == SliceScreen.Wing;
        public bool AtPractice => Screen == SliceScreen.Practice;
        public bool CanEnterPractice => AtDial && AtriumStage >= 2 && PracticeAvailable;
        public const string NothingDueLine = "Nothing is ready to practice yet. The wheel will have more for you after the next lesson."; // placeholder (owner writes)
        public const string GateLine = "Three misses. Let the instrument rest a moment.\nYour journal is below, if you want it; the wheel is ready when you are."; // placeholder (owner writes); two lines: the room caption holds two
        public const string DeskLine = "The desk is clear. What Caspar used to keep here, you carry now: your journal."; // placeholder (owner writes)
        // One entry is one sitting, whether or not anything is due; nothing due says so and the fork stays.
        public bool EnterPractice()
        {
            if (!CanEnterPractice) return false;
            Sittings++; Logged?.Invoke("sitting:" + Sittings);
            Strikes = 0; ReviewQueue.Clear(); ReviewIndex = 0; PracticeSummary = "";
            var due = Deck.DueForReview(Sitting);
            if (due.Count == 0) { Note = NothingDueLine; Logged?.Invoke("practice_nothing_due"); return false; }
            Note = "";
            // Form is a test variable (Q05 decision 2): alternate compressed Dial and direct tap.
            for (int i = 0; i < due.Count && i < ReviewDeck.BatchSize; i++)
                ReviewQueue.Add(new ReviewTask { seat = due[i].seat, mode = (int)(due[i].Kind == ItemKind.Glyph ? ReviewMode.Glyph : due[i].Kind == ItemKind.Modality ? (i % 2 == 0 ? ReviewMode.DialModality : ReviewMode.TapModality) : i % 2 == 0 ? ReviewMode.Dial : ReviewMode.Tap) });
            Screen = SliceScreen.Practice; Logged?.Invoke("practice_started"); return true;
        }
        public bool PracticeDone => AtPractice && ReviewIndex >= ReviewQueue.Count;
        // An exit at any point: the unanswered items simply stay due at this sitting.
        public bool LeavePractice()
        {
            if (!AtPractice) return false;
            bool early = !PracticeDone;
            ReviewQueue.Clear(); ReviewIndex = 0; Note = "";
            Screen = SliceScreen.Wing; Logged?.Invoke(early ? "practice_left" : "practice_closed"); return true;
        }
        public void RecordStrike() { if (!AtPractice) return; Strikes++; Logged?.Invoke("strike:" + Strikes); }
        public bool Gated => AtPractice && Strikes >= StrikeLimit;
        // The gate: the instrument closes and the player is in the room, journal in hand, never locked out; re-entry is immediate.
        public bool CloseInstrument()
        {
            if (!Gated) return false;
            ReviewQueue.Clear(); ReviewIndex = 0;
            Screen = SliceScreen.WingRoom; Note = "gated"; Logged?.Invoke("practice_gated"); return true;
        }
        public bool ApproachDesk()
        {
            if (!AtHub) return false;
            Note = DeskLine; Logged?.Invoke("desk_approached"); return true;
        }
        // The journal: in the inventory, opened from any room; renders straight from the deck; reading changes nothing (08: exposure only).
        // Build J (owner, Sept 23: the journal as a book): a contents page, a page per section, a page per sign met. Illumination plus
        // Ribbons carry the deck's state; no state word is drawn.
        public static readonly ItemKind[] JournalOrder = { ItemKind.Element, ItemKind.Glyph, ItemKind.Modality, ItemKind.Grid, ItemKind.Opposite };
        public static string SectionTitle(ItemKind kind) => kind == ItemKind.Element ? "The Elements" : kind == ItemKind.Glyph ? "The Symbols" : kind == ItemKind.Modality ? "The Modalities" : kind == ItemKind.Grid ? "The Table" : "The Opposites"; // placeholder (owner writes)
        public const string SignsTitle = "The Signs"; // placeholder (owner writes)
        public List<ItemKind> JournalSections => JournalOrder.Where(k => Deck.Items.Any(i => i.Kind == k && i.entered)).ToList();
        public JournalView JournalAt { get; private set; }
        public int JournalSection { get; private set; }
        public int JournalSign { get; private set; } // an index into JournalSigns
        public SliceScreen JournalFrom { get; private set; } = SliceScreen.Hub;
        public bool AtJournal => Screen == SliceScreen.Journal;
        public bool CanOpenJournal => (AtHub || AtWingRoom || AtChamberRoom) && JournalSections.Count > 0;
        public bool OpenJournal()
        {
            if (!CanOpenJournal) return false;
            JournalFrom = Screen; JournalAt = JournalView.Contents; JournalSection = 0; JournalSign = 0; Screen = SliceScreen.Journal; Logged?.Invoke("journal_opened"); return true;
        }
        public bool CloseJournal()
        {
            if (!AtJournal) return false;
            Screen = JournalFrom; if (Note == "gated") Note = ""; Logged?.Invoke("journal_closed"); return true;
        }
        // The contents: the sections in curriculum order, then the signs. Only what has entered the deck appears: the book starts blank and fills in.
        public List<string> JournalContents => JournalSections.Select(SectionTitle).Concat(JournalSigns.Count > 0 ? new[] { SignsTitle } : new string[0]).ToList();
        public bool JournalEntryDue(int entry) { var sections = JournalSections; return entry < sections.Count ? SectionDue(sections[entry]) : entry == sections.Count && JournalSigns.Any(SignDue); }
        public bool JournalOpenEntry(int entry)
        {
            if (!AtJournal || JournalAt != JournalView.Contents) return false;
            var sections = JournalSections;
            if (entry >= 0 && entry < sections.Count) { JournalAt = JournalView.Section; JournalSection = entry; Logged?.Invoke("journal_section:" + entry); return true; }
            if (entry != sections.Count || JournalSigns.Count == 0) return false;
            JournalAt = JournalView.Sign; JournalSign = 0; Logged?.Invoke("journal_sign:" + JournalSignSeat); return true;
        }
        public bool CanJournalContents => AtJournal && JournalAt != JournalView.Contents;
        public bool JournalToContents() { if (!CanJournalContents) return false; JournalAt = JournalView.Contents; Logged?.Invoke("journal_contents"); return true; }
        public bool CanJournalNext => AtJournal && (JournalAt == JournalView.Section ? JournalSection < JournalSections.Count - 1 : JournalAt == JournalView.Sign && JournalSign < JournalSigns.Count - 1);
        public bool CanJournalPrev => AtJournal && (JournalAt == JournalView.Section ? JournalSection > 0 : JournalAt == JournalView.Sign && JournalSign > 0);
        public bool JournalNext() { if (!CanJournalNext) return false; if (JournalAt == JournalView.Sign) JournalSign++; else JournalSection++; Logged?.Invoke("journal_page:" + JournalPageIndex); return true; }
        public bool JournalPrev() { if (!CanJournalPrev) return false; if (JournalAt == JournalView.Sign) JournalSign--; else JournalSection--; Logged?.Invoke("journal_page:" + JournalPageIndex); return true; }
        int JournalPageIndex => JournalAt == JournalView.Sign ? JournalSignSeat : JournalSection; // events name a page by number: a seat, or a section's place
        public ItemKind JournalKind => JournalSections.Count == 0 ? ItemKind.Element : JournalSections[Math.Min(JournalSection, JournalSections.Count - 1)];
        public List<ReviewItem> JournalItems(ItemKind kind) => Deck.Items.Where(i => i.Kind == kind && i.entered).OrderBy(i => i.seat).ToList();
        public static string JournalName(ReviewItem item) => item.Kind == ItemKind.Opposite ? Zodiac.Seats[item.seat].Name + " and " + Zodiac.Seats[Zodiac.Opposite(item.seat)].Name : Zodiac.Seats[item.seat].Name;
        public static string JournalFact(ReviewItem item) =>
            item.Kind == ItemKind.Element ? Zodiac.Seats[item.seat].Element :
            item.Kind == ItemKind.Glyph ? "its symbol" :
            item.Kind == ItemKind.Modality ? Zodiac.ModalityAt(item.seat) :
            item.Kind == ItemKind.Grid ? Zodiac.Seats[item.seat].Element + " · " + Zodiac.ModalityAt(item.seat) :
            "opposites: six seats apart";
        public static string StateWord(ReviewItem item) => item.State == ItemState.Practicing ? "practicing" : "introduced"; // 08's names for the two states; test evidence only since Build J, never drawn
        public List<string> JournalEntries(ItemKind kind) => JournalItems(kind).Select(i => JournalName(i) + " — " + JournalFact(i)).ToList();
        public List<string> JournalStates(ItemKind kind) => JournalItems(kind).Select(StateWord).ToList();
        // A page per sign met, in zodiac order. A sign's items: the four kinds at its seat and its pair's opposite item (a pair is named by its lower seat).
        public List<int> JournalSigns => Enumerable.Range(0, 12).Where(seat => SignItems(seat).Any(i => i.entered)).ToList();
        public int JournalSignSeat => JournalSigns.Count == 0 ? 0 : JournalSigns[Math.Min(JournalSign, JournalSigns.Count - 1)];
        public IEnumerable<ReviewItem> SignItems(int seat)
        {
            seat = Zodiac.Wrap(seat);
            yield return Deck.Item(seat, ItemKind.Element); yield return Deck.Item(seat, ItemKind.Glyph); yield return Deck.Item(seat, ItemKind.Modality); yield return Deck.Item(seat, ItemKind.Grid);
            yield return Deck.Item(Zodiac.PairOf(seat), ItemKind.Opposite);
        }
        public ReviewItem SignItem(int seat, ItemKind kind) => kind == ItemKind.Opposite ? Deck.Item(Zodiac.PairOf(seat), kind) : Deck.Item(seat, kind);
        public bool SignKnows(int seat, ItemKind kind) => SignItem(seat, kind).entered;
        // Only what the player has learned, in reading order; a fact not learned yet leaves no row. The symbol and the table cell are drawn, not written.
        public List<string[]> SignFacts(int seat)
        {
            seat = Zodiac.Wrap(seat); var facts = new List<string[]>();
            if (SignKnows(seat, ItemKind.Element)) facts.Add(new[] { "Element", Zodiac.Seats[seat].Element }); // placeholder labels (owner writes)
            if (SignKnows(seat, ItemKind.Modality)) facts.Add(new[] { "Modality", Zodiac.ModalityAt(seat) });
            if (SignKnows(seat, ItemKind.Opposite)) { facts.Add(new[] { "Polarity", Zodiac.PolarityAt(seat) }); facts.Add(new[] { "Opposite", Zodiac.Seats[Zodiac.Opposite(seat)].Name }); }
            return facts;
        }
        // Illumination (owner, Sept 23): an introduced item is line art at about a third of its ink; a practicing one gains colour with its
        // streak (1 → 40%, 2 → 70%, 3 and on → full, with a gilt edge); an item due for practice shows its colour a little faded.
        public const float IntroducedInk = .35f, DueFade = .75f;
        public static float Ink(ReviewItem item) => !item.entered ? 0 : item.State == ItemState.Introduced ? IntroducedInk : 1;
        public static float Colour(ReviewItem item) => !item.entered || item.State == ItemState.Introduced ? 0 : item.streak >= 3 ? 1 : item.streak == 2 ? .7f : item.streak == 1 ? .4f : .2f;
        // "Due" is due for practice: practice would ask it now. The table and the opposites are data only (Sept 15) and never asked, so never due.
        public bool DueForPractice(ReviewItem item) => item.entered && ReviewDeck.Reviewable(item.Kind) && item.dueDay <= Sitting;
        public float ItemColour(ReviewItem item) => Colour(item) * (DueForPractice(item) ? DueFade : 1);
        public bool SectionDue(ItemKind kind) => JournalItems(kind).Any(DueForPractice);
        public bool SignDue(int seat) => SignItems(seat).Any(DueForPractice);
        // A sign's page reads its best-known fact, so learning something new never takes colour away.
        public float SignInk(int seat) => SignItems(seat).Select(Ink).Max();
        public float SignColour(int seat) => SignItems(seat).Select(Colour).Max() * (SignDue(seat) ? DueFade : 1);
        public bool SignGilt(int seat) => SignItems(seat).Any(i => Colour(i) >= 1);
        // Ribbons (owner, Sept 23): the length is how far the sign has climbed the practice ladder; due pulls it out past the page's edge.
        public int SignLadder(int seat) => SignItems(seat).Where(i => i.entered && ReviewDeck.Reviewable(i.Kind)).Select(i => i.interval).DefaultIfEmpty(0).Max();
        // Direct-tap item: which element does this sign belong to? One nudge, then reveal and move on.
        // Glyph review item: which sign is this mark? Four names, stable per seat (same options as the lesson).
        public int[] GlyphReviewOptions(int seat) => DialLesson.OptionsFor(seat, CleanRuns > 0, CleanRuns); // harder names once a clean run is on record
        public bool AnswerGlyph(int seat)
        {
            var task = CurrentReview; if (task == null || task.Mode != ReviewMode.Glyph || task.done) return false;
            bool correct = Zodiac.Wrap(seat) == task.seat;
            if (correct) { FinishReview(true, task.misses == 0); return true; }
            task.misses++; RecordStrike();
            if (task.misses >= 2) { FinishReview(false, false); return false; }
            Note = "Not that one, acolyte. Look again and try once more."; return false; // owner (worksheet section 2)
        }
        public bool AnswerModalityTap(string modality)
        {
            var task = CurrentReview; if (task == null || task.Mode != ReviewMode.TapModality || task.done) return false;
            bool correct = Zodiac.ModalityAt(task.seat) == modality;
            if (correct) { FinishReview(true, task.misses == 0); return true; }
            task.misses++; RecordStrike();
            if (task.misses >= 2) { FinishReview(false, false); return false; }
            Note = "Not that one, acolyte. Look again and try once more."; return false; // owner (worksheet section 2)
        }
        public bool AnswerTap(string element)
        {
            var task = CurrentReview; if (task == null || task.Mode != ReviewMode.Tap || task.done) return false;
            bool correct = Zodiac.Seats[task.seat].Element == element;
            if (correct) { FinishReview(true, task.misses == 0); return true; }
            task.misses++; RecordStrike();
            if (task.misses >= 2) { FinishReview(false, false); return false; }
            Note = "Not that one, acolyte. Look again and try once more."; return false; // owner (worksheet section 2)
        }
        public void FinishReview(bool correct, bool eligible)
        {
            var task = CurrentReview; if (task == null || task.done) return;
            task.done = true; task.correct = correct;
            var kind = task.Mode == ReviewMode.Glyph ? ItemKind.Glyph : (task.Mode == ReviewMode.TapModality || task.Mode == ReviewMode.DialModality) ? ItemKind.Modality : ItemKind.Element;
            Deck.RecordReview(task.seat, correct, eligible, Sitting, kind);
            string name = Zodiac.Seats[task.seat].Name, element = Zodiac.Seats[task.seat].Element, modality = Zodiac.ModalityAt(task.seat).ToLowerInvariant();
            Note = kind == ItemKind.Glyph
                ? (correct ? "Yes, this is the symbol of " + name + "." : "This symbol belongs to " + name + ". The wheel shall test you on it again.") // owner (worksheet section 5)
                : kind == ItemKind.Modality
                ? (correct ? "Yes, " + name + " is " + modality + "." : name + " is " + modality + ". The wheel shall test you on it again.") // owner (worksheet section 10)
                : (correct ? "Yes. " + name + " is " + Zodiac.Article(element) + " " + element + " sign." : name + " is " + Zodiac.Article(element) + " " + element + " sign. We will come back to it."); // owner (worksheet section 2)
            ReviewIndex++;
            if (ReviewIndex >= ReviewQueue.Count)
            {
                int right = ReviewQueue.Count(t => t.correct);
                PracticeSummary = right + " of " + ReviewQueue.Count + " proven at the wheel."; // owner (worksheet section 2); the sitting was counted on entry
                Logged?.Invoke("practice_finished");
            }
        }
        public bool V02Complete => WheelComplete && AtriumStage >= 3;
        // ---- save / restore ----
        // Record answer evidence as before, but persist only after the owning model commits its progress.
        // Shared by the live slice and the reload regression checks so they exercise the same subscriptions.
        public void ObserveProgress(DialLesson lesson, GridModel grid, Action checkpoint)
        {
            // Evidence routing (audit of Sept 16, task 86bc2338n): every answer records exactly one item kind, chosen by the lesson's phase.
            //   element-family problems (Guided, Independent, Optional, Continuation) → ItemKind.Element, by the destination seat
            //   symbol Part A (GlyphNames), by GlyphNamedEvent                        → ItemKind.Glyph, by the named seat
            //   symbol Part B (GlyphWheel), by the glyph_placed event                  → ItemKind.Glyph, by the placed seat (its answer_correct is not element evidence)
            //   modality problems (ModalityGuided, ModalityOwn)                        → ItemKind.Modality, by the destination seat
            //   opposite problems (OppositeGuided, OppositeOwn, BuilderOpposite)       → ItemKind.Opposite, by the pair of the start seat
            //   the table (GridModel grid_placed)                                      → ItemKind.Grid, by the seated sign
            //   Review answers record nothing here: FinishReview records the reviewed item by its own kind.
            // The element filter is a positive list so a new phase records nothing until it is routed on purpose.
            lesson.Dial.Logged += e =>
            {
                if (e.event_name == "answer_correct" && lesson.InElementProblem)
                    RecordLessonAnswer(e.selected_destination, e.evidence_eligible);
                if (e.event_name == "answer_correct" && lesson.InOppositeProblem)
                    RecordOppositeAnswer(e.start_seat, e.evidence_eligible);
                if (e.event_name == "answer_correct" && lesson.InModalities)
                    RecordModalityAnswer(e.selected_destination, e.evidence_eligible);
                if (e.event_name == "glyph_placed") RecordGlyphAnswer(e.selected_destination, e.evidence_eligible);
            };
            lesson.GlyphNamedEvent += (seat, correct, eligible) => RecordGlyphAnswer(seat, eligible);
            lesson.ProgressCommitted += checkpoint;
            lesson.PracticeFinished += clean => { if (clean) RecordCleanRun(); checkpoint(); };
            grid.Logged += e => { if (e.event_name == "grid_placed") RecordGridAnswer(e.start_seat, e.evidence_eligible); };
            grid.ProgressCommitted += checkpoint;
        }
        // One snapshot after a committed transition; presentation polling is not a persistence boundary.
        public SaveData CaptureProgress(DialLesson lesson, GridModel grid)
        {
            // Replays are disposable; never replace the earned unit's saved progress with a practice index.
            SetGlyphProgress(lesson.Key2Earned ? 2 : lesson.AllNamed ? 1 : 0,
                lesson.Key2Earned ? 12 : lesson.AllNamed ? lesson.GlyphPlaced.Count(v => v) : lesson.GlyphNamed.Count(v => v));
            var save = ToSave(lesson.Lit, lesson.Kin, lesson.KeyEarned, lesson.LitMod, lesson.KinMod,
                grid.Placed, grid.Evidence, lesson.PolarityShown, lesson.OppKnown, lesson.Built, lesson.BuilderEvidence);
            save.wheelComplete = lesson.WheelComplete;
            save.keys = Math.Max(Keys, lesson.Key4Earned ? 4 : grid.Key3Earned ? 3 : lesson.Key2Earned ? 2 : lesson.KeyEarned ? 1 : 0);
            return save;
        }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned) { return ToSave(lit, kin, keyEarned, null, null); }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned, bool[] litMod, bool[] kinMod) { return ToSave(lit, kin, keyEarned, litMod, kinMod, null, false); }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned, bool[] litMod, bool[] kinMod, bool[] gridPlaced, bool gridEvidence) { return ToSave(lit, kin, keyEarned, litMod, kinMod, gridPlaced, gridEvidence, false, null, 0, false); }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned, bool[] litMod, bool[] kinMod, bool[] gridPlaced, bool gridEvidence, bool polarityShown, bool[] oppKnown, int built, bool builderEvidence)
        {
            return new SaveData { playerName = PlayerName, sunSign = SunSign, lit = (bool[])lit.Clone(), kin = (bool[])kin.Clone(), keyEarned = keyEarned, litMod = litMod != null ? (bool[])litMod.Clone() : new bool[12], kinMod = kinMod != null ? (bool[])kinMod.Clone() : new bool[3], modalitiesStarted = ModalitiesStarted,
                gridPlaced = gridPlaced != null ? (bool[])gridPlaced.Clone() : new bool[12], gridEvidence = gridEvidence, gridStarted = GridStarted,
                polarityShown = polarityShown, oppKnown = oppKnown != null ? (bool[])oppKnown.Clone() : new bool[Zodiac.OppositePairs], oppositesStarted = OppositesStarted, built = built, builderEvidence = builderEvidence, locksFilled = LocksFilled,
                wheelComplete = WheelComplete, atriumStage = AtriumStage, keys = Keys, glyphStage = GlyphStage, glyphIndex = GlyphIndex, cleanRuns = CleanRuns, deck = Deck.Items.Select(i => new ReviewItem { seat = i.seat, kind = i.kind, state = i.state, streak = i.streak, interval = i.interval, dueDay = i.dueDay, entered = i.entered }).ToArray(), sittings = Sittings, reviewsChecked = Sittings };
        }
        // Resumes at the Hub (a second sitting). Only meaningful once the Key was earned and the Hub reached.
        public bool Restore(SaveData save)
        {
            if (save == null || save.atriumStage < 2 || save.sunSign < 0) return false;
            PlayerName = save.playerName ?? ""; SunSign = save.sunSign; BirthChoice = "saved";
            KeyRevealed = save.keyEarned; KeyInserted = save.keyEarned; LocksFilled = Math.Max(save.locksFilled, save.keyEarned ? 1 : 0); Ended = save.keyEarned;
            WheelComplete = save.wheelComplete; AtriumStage = save.atriumStage; Sittings = Math.Max(save.sittings, save.reviewsChecked); // an older save's batches count as sittings
            Keys = Math.Max(save.keys, save.keyEarned ? 1 : 0); GlyphStage = save.glyphStage; GlyphIndex = save.glyphIndex; CleanRuns = save.cleanRuns; ModalitiesStarted = save.modalitiesStarted; GlyphsStarted = save.glyphStage > 0 || save.glyphIndex > 0 || (save.deck != null && save.deck.Any(d => d.kind == (int)ItemKind.Glyph && d.entered));
            ModalitiesComplete = save.litMod != null && save.litMod.Length == 12 && save.litMod.All(v => v); GridStarted = save.gridStarted || save.keys >= 3;
            OppositesStarted = save.oppositesStarted || save.polarityShown || save.keys >= 4;
            if (save.deck != null) foreach (var d in save.deck) if (d.seat >= 0 && d.seat < 12 && d.kind >= 0 && d.kind < ReviewDeck.Kinds) { var i = Deck.Item(d.seat, (ItemKind)d.kind); i.state = d.state; i.streak = d.streak; i.interval = d.interval; i.dueDay = d.dueDay; i.entered = d.entered; }
            Screen = SliceScreen.Hub; Walk.Enter(Room.Atrium, "entry"); Logged?.Invoke("session_resumed"); return true;
        }
    }
}
