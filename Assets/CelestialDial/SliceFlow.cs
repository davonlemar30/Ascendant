using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum SliceScreen { Identity, Birth, Atrium, Wing, AtriumReturn, Chamber, Hub, Review }
    public enum ReviewMode { Dial, Tap }

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
        public int DayOffset { get; private set; }          // test-only "advance one day"
        public int FirstDay { get; private set; } = -1;
        public int ReviewsChecked { get; private set; }
        public readonly List<ReviewTask> ReviewQueue = new List<ReviewTask>();
        public int ReviewIndex { get; private set; }
        public ReviewTask CurrentReview => ReviewIndex < ReviewQueue.Count ? ReviewQueue[ReviewIndex] : null;
        public string ReviewSummary { get; private set; } = "";
        public event Action<string> Logged;
        readonly Func<int> random;
        readonly Func<int> realDay;
        public SliceFlow() : this(null, null) { }
        public SliceFlow(Func<int> randomSeat) : this(randomSeat, null) { }
        public SliceFlow(Func<int> randomSeat, Func<int> realDayNumber)
        {
            random = randomSeat ?? (() => new Random().Next(12));
            realDay = realDayNumber ?? (() => (int)(DateTime.UtcNow - new DateTime(2026, 1, 1)).TotalDays);
            Deck.Logged += n => Logged?.Invoke(n);
        }
        public int Day { get { if (FirstDay < 0) FirstDay = realDay(); return realDay() - FirstDay + DayOffset; } }
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
            if (Screen == SliceScreen.Hub) { Note = ""; if (AtriumStage < 2) { AtriumStage = 2; Deck.IntroduceAll(Day); } }
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
        public bool EnterWing()
        {
            if (!CanEnterWing) return false;
            Screen = SliceScreen.Wing; Logged?.Invoke("screen_entered:wing"); return true;
        }
        public bool LeaveWing()
        {
            if (Screen != SliceScreen.Wing || AtriumStage < 2) return false;
            Screen = SliceScreen.Hub;
            if (WheelComplete && AtriumStage < 3) { AtriumStage = 3; Logged?.Invoke("atrium_stage_3"); }
            Logged?.Invoke("screen_entered:hub"); return true;
        }
        public void MarkWheelComplete() { if (!WheelComplete) { WheelComplete = true; Logged?.Invoke("wheel_completed"); } }
        public void RecordLessonAnswer(int seat, bool eligible) { if (AtriumStage >= 2 || Deck.Items[seat].entered) Deck.RecordLesson(seat, eligible, Day); else Deck.RecordLesson(seat, eligible, Day); }
        public int DueCount => Deck.Due(Day).Count;
        public bool CanCheckSeals => AtHub && DueCount > 0;
        public bool EnterSeals()
        {
            if (!AtHub) return false;
            var due = Deck.Due(Day);
            if (due.Count == 0) { Note = "Nothing is due today. Come back tomorrow."; Logged?.Invoke("seals_nothing_due"); return false; }
            ReviewQueue.Clear(); ReviewIndex = 0; ReviewSummary = ""; Note = "";
            // Form is a test variable (Q05 decision 2): alternate compressed Dial and direct tap.
            for (int i = 0; i < due.Count && i < ReviewDeck.BatchSize; i++)
                ReviewQueue.Add(new ReviewTask { seat = due[i].seat, mode = (int)(i % 2 == 0 ? ReviewMode.Dial : ReviewMode.Tap) });
            Screen = SliceScreen.Review; Logged?.Invoke("review_started"); return true;
        }
        // Direct-tap item: which element does this sign belong to? One nudge, then reveal and move on.
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
            Deck.RecordReview(task.seat, correct, eligible, Day);
            Note = correct ? "Yes. " + Zodiac.Seats[task.seat].Name + " is " + Zodiac.Seats[task.seat].Element + "." :
                Zodiac.Seats[task.seat].Name + " is " + Zodiac.Seats[task.seat].Element + ". We will come back to it.";
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
            Screen = SliceScreen.Hub; Note = ""; Logged?.Invoke("screen_entered:hub"); return true;
        }
        public void AdvanceDay() { DayOffset++; Logged?.Invoke("test_day_advanced"); }
        public bool V02Complete => WheelComplete && AtriumStage >= 3;
        // ---- save / restore ----
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned)
        {
            return new SaveData { playerName = PlayerName, sunSign = SunSign, lit = (bool[])lit.Clone(), kin = (bool[])kin.Clone(), keyEarned = keyEarned,
                wheelComplete = WheelComplete, atriumStage = AtriumStage, dayOffset = DayOffset, firstDay = FirstDay, deck = Deck.Items.Select(i => new ReviewItem { seat = i.seat, state = i.state, streak = i.streak, interval = i.interval, dueDay = i.dueDay, entered = i.entered }).ToArray(), reviewsChecked = ReviewsChecked };
        }
        // Resumes at the Hub (a second sitting). Only meaningful once the Key was earned and the Hub reached.
        public bool Restore(SaveData save)
        {
            if (save == null || save.atriumStage < 2 || save.sunSign < 0) return false;
            PlayerName = save.playerName ?? ""; SunSign = save.sunSign; BirthChoice = "saved";
            KeyRevealed = save.keyEarned; KeyInserted = save.keyEarned; LocksFilled = save.keyEarned ? 1 : 0; Ended = save.keyEarned;
            WheelComplete = save.wheelComplete; AtriumStage = save.atriumStage; DayOffset = save.dayOffset; FirstDay = save.firstDay; ReviewsChecked = save.reviewsChecked;
            if (save.deck != null) foreach (var d in save.deck) if (d.seat >= 0 && d.seat < 12) { var i = Deck.Items[d.seat]; i.state = d.state; i.streak = d.streak; i.interval = d.interval; i.dueDay = d.dueDay; i.entered = d.entered; }
            Screen = SliceScreen.Hub; Logged?.Invoke("session_resumed"); return true;
        }
    }
}
