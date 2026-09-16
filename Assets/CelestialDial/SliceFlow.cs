using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum SliceScreen { Identity, Birth, Atrium, Wing, AtriumReturn, Chamber, Hub, Review, WingRoom, Book, Grid }
    public enum ReviewMode { Dial, Tap, Glyph, TapModality, DialModality }

    [Serializable]
    public sealed class ReviewTask { public int seat; public int mode; public int misses; public bool done; public bool correct; public ReviewMode Mode => (ReviewMode)mode; }

    // The locked v0.1 flow (Q06) plus the locked v0.2 loop (Q05): Hub with two entrances, Check the Seals,
    // Unit 1.1 continuation, session day, local save. Pure state, no Unity types.
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
        public int ReviewsChecked { get; private set; }
        // v0.4: the marker that walks the Atrium and the Wing room (Q06 phase 2).
        public readonly Walker Walk = new Walker();
        public readonly List<ReviewTask> ReviewQueue = new List<ReviewTask>();
        public int ReviewIndex { get; private set; }
        public ReviewTask CurrentReview => ReviewIndex < ReviewQueue.Count ? ReviewQueue[ReviewIndex] : null;
        public string ReviewSummary { get; private set; } = "";
        public event Action<string> Logged;
        readonly Func<int> random;
        public SliceFlow() : this(null) { }
        public SliceFlow(Func<int> randomSeat)
        {
            random = randomSeat ?? (() => new Random().Next(12));
            Deck.Logged += n => Logged?.Invoke(n);
            Walk.Logged += n => Logged?.Invoke(n);
        }
        // No in-game time (owner, Sept 13): the deck counts sittings, one per completed Check the Seals batch.
        public int Sitting => ReviewsChecked;
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
            KeyRevealed = true; Logged?.Invoke("key_revealed"); return true;
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
            if (Keys >= 2 && AtriumStage < 4) { AtriumStage = 4; Logged?.Invoke("atrium_stage_4"); }
            if (Keys >= 3 && AtriumStage < 5) { AtriumStage = 5; Logged?.Invoke("atrium_stage_5"); } // Build B: one more step after Key 3
            if (Keys >= 4 && AtriumStage < 6) { AtriumStage = 6; Logged?.Invoke("atrium_stage_6"); } // Build C: and one more after Key 4
            Walk.Enter(Room.Atrium, "wing-door"); Logged?.Invoke("screen_entered:hub"); return true;
        }
        // Sealed doors only say they are sealed (Q06 phase 2, decision 2).
        public bool TouchSealedDoor()
        {
            if (!AtHub) return false;
            Note = "Sealed. It does not answer to you yet."; Logged?.Invoke("sealed_door_touched"); return true; // placeholder (owner writes)
        }
        public bool ApproachCaspar()
        {
            if (!AtHub) return false;
            Note = "Caspar looks up from the desk and waits."; Logged?.Invoke("caspar_approached"); return true; // placeholder (owner writes)
        }
        public void MarkWheelComplete() { if (!WheelComplete) { WheelComplete = true; Logged?.Invoke("wheel_completed"); } }
        public void MarkKeyEarned() { if (Keys < 1) { Keys = 1; } }
        public void StartGlyphs() { if (!GlyphsStarted) { GlyphsStarted = true; Deck.IntroduceAll(Sitting, ItemKind.Glyph); Logged?.Invoke("glyph_unit_started"); } }
        public void SetGlyphProgress(int stage, int index) { GlyphStage = stage; GlyphIndex = index; }
        public void MarkKey2() { if (Keys < 2) { Keys = 2; GlyphStage = 2; } } // the lesson logs key2_earned once
        public void RecordGlyphAnswer(int seat, bool eligible) { Deck.RecordLesson(seat, eligible, Sitting, ItemKind.Glyph); }
        public void RecordLessonAnswer(int seat, bool eligible) { if (AtriumStage >= 2 || Deck.Items[seat].entered) Deck.RecordLesson(seat, eligible, Sitting); else Deck.RecordLesson(seat, eligible, Sitting); }
        public int DueCount => Deck.DueForReview(Sitting).Count; // grid items are data only until the review fork ships
        public bool CanCheckSeals => AtHub && DueCount > 0;
        public bool EnterSeals()
        {
            if (!AtHub) return false;
            var due = Deck.DueForReview(Sitting);
            if (due.Count == 0) { Note = "The seals hold for now. Come back after the Wing."; Logged?.Invoke("seals_nothing_due"); return false; } // placeholder (owner writes)
            ReviewQueue.Clear(); ReviewIndex = 0; ReviewSummary = ""; Note = "";
            // Form is a test variable (Q05 decision 2): alternate compressed Dial and direct tap.
            for (int i = 0; i < due.Count && i < ReviewDeck.BatchSize; i++)
                ReviewQueue.Add(new ReviewTask { seat = due[i].seat, mode = (int)(due[i].Kind == ItemKind.Glyph ? ReviewMode.Glyph : due[i].Kind == ItemKind.Modality ? (i % 2 == 0 ? ReviewMode.DialModality : ReviewMode.TapModality) : i % 2 == 0 ? ReviewMode.Dial : ReviewMode.Tap) });
            Screen = SliceScreen.Review; Logged?.Invoke("review_started"); return true;
        }
        // Direct-tap item: which element does this sign belong to? One nudge, then reveal and move on.
        // Glyph review item: which sign is this mark? Four names, stable per seat (same options as the lesson).
        public int[] GlyphReviewOptions(int seat) => DialLesson.OptionsFor(seat, CleanRuns > 0, CleanRuns); // harder names once a clean run is on record
        public bool AnswerGlyph(int seat)
        {
            var task = CurrentReview; if (task == null || task.Mode != ReviewMode.Glyph || task.done) return false;
            bool correct = Zodiac.Wrap(seat) == task.seat;
            if (correct) { FinishReview(true, task.misses == 0); return true; }
            task.misses++;
            if (task.misses >= 2) { FinishReview(false, false); return false; }
            Note = "Not that one. Try once more."; return false;
        }
        public bool AnswerModalityTap(string modality)
        {
            var task = CurrentReview; if (task == null || task.Mode != ReviewMode.TapModality || task.done) return false;
            bool correct = Zodiac.ModalityAt(task.seat) == modality;
            if (correct) { FinishReview(true, task.misses == 0); return true; }
            task.misses++;
            if (task.misses >= 2) { FinishReview(false, false); return false; }
            Note = "Not that one. Try once more."; return false;
        }
        public bool AnswerTap(string element)
        {
            var task = CurrentReview; if (task == null || task.Mode != ReviewMode.Tap || task.done) return false;
            bool correct = Zodiac.Seats[task.seat].Element == element;
            if (correct) { FinishReview(true, task.misses == 0); return true; }
            task.misses++;
            if (task.misses >= 2) { FinishReview(false, false); return false; }
            Note = "Not that one. Try once more."; return false;
        }
        public void FinishReview(bool correct, bool eligible)
        {
            var task = CurrentReview; if (task == null || task.done) return;
            task.done = true; task.correct = correct;
            var kind = task.Mode == ReviewMode.Glyph ? ItemKind.Glyph : (task.Mode == ReviewMode.TapModality || task.Mode == ReviewMode.DialModality) ? ItemKind.Modality : ItemKind.Element;
            Deck.RecordReview(task.seat, correct, eligible, Sitting, kind);
            string name = Zodiac.Seats[task.seat].Name, element = Zodiac.Seats[task.seat].Element, modality = Zodiac.ModalityAt(task.seat).ToLowerInvariant();
            Note = kind == ItemKind.Glyph
                ? (correct ? "Yes. That is the symbol of " + name + "." : "That is the symbol of " + name + ". We will come back to it.")
                : kind == ItemKind.Modality
                ? (correct ? "Yes. " + name + " is " + modality + "." : name + " is " + modality + ". We will come back to it.")
                : (correct ? "Yes. " + name + " is " + element + "." : name + " is " + element + ". We will come back to it.");
            ReviewIndex++;
            if (ReviewIndex >= ReviewQueue.Count)
            {
                int right = ReviewQueue.Count(t => t.correct);
                ReviewSummary = right + " of " + ReviewQueue.Count + " seals held."; ReviewsChecked++;
                Logged?.Invoke("review_finished");
            }
        }
        public bool ReviewDone => Screen == SliceScreen.Review && ReviewIndex >= ReviewQueue.Count;
        public bool LeaveReview()
        {
            if (Screen != SliceScreen.Review || !ReviewDone) return false;
            Screen = SliceScreen.Hub; Note = ""; Walk.Enter(Room.Atrium, "desk"); Logged?.Invoke("screen_entered:hub"); return true;
        }
        public bool V02Complete => WheelComplete && AtriumStage >= 3;
        // ---- save / restore ----
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned) { return ToSave(lit, kin, keyEarned, null, null); }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned, bool[] litMod, bool[] kinMod) { return ToSave(lit, kin, keyEarned, litMod, kinMod, null, false); }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned, bool[] litMod, bool[] kinMod, bool[] gridPlaced, bool gridEvidence) { return ToSave(lit, kin, keyEarned, litMod, kinMod, gridPlaced, gridEvidence, false, null, 0, false); }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned, bool[] litMod, bool[] kinMod, bool[] gridPlaced, bool gridEvidence, bool polarityShown, bool[] oppKnown, int built, bool builderEvidence)
        {
            return new SaveData { playerName = PlayerName, sunSign = SunSign, lit = (bool[])lit.Clone(), kin = (bool[])kin.Clone(), keyEarned = keyEarned, litMod = litMod != null ? (bool[])litMod.Clone() : new bool[12], kinMod = kinMod != null ? (bool[])kinMod.Clone() : new bool[3], modalitiesStarted = ModalitiesStarted,
                gridPlaced = gridPlaced != null ? (bool[])gridPlaced.Clone() : new bool[12], gridEvidence = gridEvidence, gridStarted = GridStarted,
                polarityShown = polarityShown, oppKnown = oppKnown != null ? (bool[])oppKnown.Clone() : new bool[Zodiac.OppositePairs], oppositesStarted = OppositesStarted, built = built, builderEvidence = builderEvidence,
                wheelComplete = WheelComplete, atriumStage = AtriumStage, keys = Keys, glyphStage = GlyphStage, glyphIndex = GlyphIndex, cleanRuns = CleanRuns, deck = Deck.Items.Select(i => new ReviewItem { seat = i.seat, kind = i.kind, state = i.state, streak = i.streak, interval = i.interval, dueDay = i.dueDay, entered = i.entered }).ToArray(), reviewsChecked = ReviewsChecked };
        }
        // Resumes at the Hub (a second sitting). Only meaningful once the Key was earned and the Hub reached.
        public bool Restore(SaveData save)
        {
            if (save == null || save.atriumStage < 2 || save.sunSign < 0) return false;
            PlayerName = save.playerName ?? ""; SunSign = save.sunSign; BirthChoice = "saved";
            KeyRevealed = save.keyEarned; KeyInserted = save.keyEarned; LocksFilled = save.keyEarned ? 1 : 0; Ended = save.keyEarned;
            WheelComplete = save.wheelComplete; AtriumStage = save.atriumStage; ReviewsChecked = save.reviewsChecked;
            Keys = Math.Max(save.keys, save.keyEarned ? 1 : 0); GlyphStage = save.glyphStage; GlyphIndex = save.glyphIndex; CleanRuns = save.cleanRuns; ModalitiesStarted = save.modalitiesStarted; GlyphsStarted = save.glyphStage > 0 || save.glyphIndex > 0 || (save.deck != null && save.deck.Any(d => d.kind == (int)ItemKind.Glyph && d.entered));
            ModalitiesComplete = save.litMod != null && save.litMod.Length == 12 && save.litMod.All(v => v); GridStarted = save.gridStarted || save.keys >= 3;
            OppositesStarted = save.oppositesStarted || save.polarityShown || save.keys >= 4;
            if (save.deck != null) foreach (var d in save.deck) if (d.seat >= 0 && d.seat < 12 && d.kind >= 0 && d.kind < ReviewDeck.Kinds) { var i = Deck.Item(d.seat, (ItemKind)d.kind); i.state = d.state; i.streak = d.streak; i.interval = d.interval; i.dueDay = d.dueDay; i.entered = d.entered; }
            Screen = SliceScreen.Hub; Walk.Enter(Room.Atrium, "entry"); Logged?.Invoke("session_resumed"); return true;
        }
    }
}
