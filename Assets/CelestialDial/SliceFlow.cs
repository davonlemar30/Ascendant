using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum SliceScreen { Identity, Birth, Atrium, Wing, AtriumReturn, Chamber, Hub, Practice, WingRoom, Book, Grid, ChamberRoom, Journal }
    public enum ReviewMode { Dial, Tap, Glyph, TapModality, DialModality }
    public enum JournalView { Wheel, Sign, Title, Landing, Contents, Map, Practice, Quiz, Birth } // Build AA (owner, Sept 30): the Wheel and a page per sign met; batch 2 (owner, Oct 1): the title page, the landing, Contents and the Library Map; Oct 3: Practice's list and its question page; Oct 7: "Your Birth"
    public enum JournalLens { Element, Modality, Polarity, Opposites } // Build AA: the tabs that recolour the Wheel or the Table, each once learned

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
        public bool KeyRevealed { get; private set; }
        public bool KeyInserted { get; private set; }
        public bool Ended { get; private set; }
        public int LocksFilled { get; private set; }
        public const int LocksPerBook = 3;
        public const int Books = 7;
        public const int LocksTotal = Books * LocksPerBook; // the Library's whole arc: 21 Keys spent restores everything (owner, Sept 27)
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
            // Stages 4-6 stay the return script's beats. The rooms' visible restoration paces on LocksFilled across the whole arc instead (owner, Sept 27).
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
        public SliceFlow() : this(null) { }
        public SliceFlow(Func<int> randomSeat) // the random sun is retired (owner, Oct 7); the seat source is ignored, kept so the checks construct as before
        {
            Deck.Logged += n => Logged?.Invoke(n);
            Walk.Logged += n => Logged?.Invoke(n);
        }
        // No in-game time (owner, Sept 13): the deck counts sittings. The sitting rule (owner, Sept 17): one entry to the practice
        // fork on the Dial is one sitting, due items or not, so the ladder can never stall (the Sept 16 audit's deadlock).
        public int Sittings { get; private set; }
        public int Sitting => Sittings;
        public string DisplayName => string.IsNullOrEmpty(PlayerName) ? "Keeper" : PlayerName;
        public void SetName(string name) { PlayerName = (name ?? "").Trim(); }
        // ---- The birth record (the birth-time build, owner, Oct 7: the recommendation on 86bceb6fq approved, comment 90140265369479) ----
        // Facts come from the player, the player's choices fill what the facts can't decide, and the game works out the rest and never guesses:
        // worked out (exact or stable) beats chosen, and chosen beats unknown; an uncertain point shows its options ("Pisces or Aries", Oct 3).
        // The opening's question has two answers. "Yes, I know my birthday": the date, then the time (or "I don't know my birth time"), then the
        // place (or "I don't know where"), the chart worked out from whatever is known, and on a cusp day the sun asked (Oct 2). "I'll skip it":
        // "Which sign do you go by?", a sign or "I'm not sure", which leaves the sun unknown and starts the lessons from Aries. "Enter what I
        // already know" and the random sun are retired (Oct 7). Later, "Your Birth" in the journal adds a missing fact (the opening's boxes, in
        // order: date, time, place) or chooses the rising (Oct 3: no label, a hidden flag). Adding facts only narrows the window, so a point
        // already worked out keeps its sign; a choice gives way to a sign the facts decide; nothing already played changes.
        public BirthFacts Facts { get; private set; } = new BirthFacts();
        public WorkedChart Chart { get; private set; } = new WorkedChart();
        readonly List<ChoiceRecord> choices = new List<ChoiceRecord>();
        public IReadOnlyList<ChoiceRecord> Choices => choices;
        public ChoiceRecord ChoiceFor(string point) => choices.FirstOrDefault(c => c.point == point);
        void Choose(string point, int sign, string how) { choices.RemoveAll(c => c.point == point); choices.Add(new ChoiceRecord { point = point, sign = sign, how = how }); }
        bool Worked(string point) { var p = Chart.Point(point); return p != null && p.Settled && p.signs.Length == 1; }
        // a point's sign as the record shows it: worked out, else the player's choice, else -1 (uncertain or unknown)
        public int Resolved(string point) { if (Worked(point)) return Chart.Point(point).signs[0]; var c = ChoiceFor(point); return c != null && c.sign >= 0 && c.sign < 12 ? c.sign : -1; }
        public bool FromChoice(string point) => !Worked(point) && Resolved(point) >= 0;
        public int SunSign => Resolved("sun");
        public bool HasSunSign => SunSign >= 0;
        public int LessonSun => HasSunSign ? SunSign : 0; // with no sun the lessons start from Aries, the Dial's first seat (owner, Oct 7); never saved as a sun
        public int MoonSign => Resolved("moon");
        public int[] MoonOptions { get { var p = Chart.Point("moon"); return MoonSign < 0 && p != null && !p.Settled ? p.signs : new int[0]; } }
        public bool MoonPair => MoonOptions.Length >= 2;
        public int MoonFrom => MoonPair ? MoonOptions[0] : -1;
        public int MoonTo => MoonPair ? MoonOptions[MoonOptions.Length - 1] : -1;
        public static string Options(int[] signs) => string.Join(", ", signs.Take(signs.Length - 1).Select(s => Zodiac.Seats[s].Name)) + " or " + Zodiac.Seats[signs[signs.Length - 1]].Name; // "Pisces or Aries"; three, rarely, with no time or place: "Sagittarius, Capricorn or Aquarius"
        public string MoonWords => MoonSign >= 0 ? Zodiac.Seats[MoonSign].Name : MoonPair ? Options(MoonOptions) : "unknown";
        public int RisingSign => Resolved("rising");
        public bool RisingChosen => FromChoice("rising"); // the hidden flag (owner, Oct 3): the record shows a chosen rising like any other
        public string SunBasis => FromChoice("sun") ? ChoiceFor("sun").how : ""; // picked, noon; a converted save's entered, assigned or legacy
        public string OpeningPath { get; private set; } = ""; // chart or skip; a converted save keeps its old path's name
        public int BirthYear => Facts.Year; public int BirthMonth => Facts.Month; public int BirthDay => Facts.Day;
        public int BirthMinute => Facts.Minute;   // minutes after local midnight; -1 unknown
        public Place BirthPlace => Facts.AsPlace;
        public string BirthStep { get; private set; } = ""; // the chart path: date, time, place (cusp on a cusp day); the skip path: sun-pick; on "Your Birth" also rising-pick; then done
        public bool BirthDone => BirthStep == "done";
        public bool Amending { get; private set; }          // on the birth screen from "Your Birth": adding a missing fact, or choosing the rising
        BirthFacts draft = new BirthFacts(); BirthChart pending; string choiceBefore = "";
        // The cusp day (owner ruling, Oct 2 evening): with no birth time, on a day the sun changed sign, the player is asked which sign they
        // go by, and why; the answer is the player's choice: picked, or noon ("I'm not sure": the sun at local noon, approximate, so a later
        // screen can offer a fix; with no place, the window's middle). A birth time added later settles it (Oct 7: worked out beats chosen).
        public int CuspFrom { get; private set; } = -1; public int CuspTo { get; private set; } = -1; public int CuspNoon { get; private set; } = -1;
        public int CuspMinute { get; private set; } = -1; // the minute the sun changed sign, on the birth place's clock that day; -1 with no place
        void ClearCusp() { CuspFrom = CuspTo = CuspNoon = CuspMinute = -1; MoonAsk = new int[0]; }
        // The Moon pick (owner, Oct 7: "Ask, like the cusp Sun"): when the Moon could be in more than one sign (no birth time, or no place), the
        // opening asks which the player goes by, after the cusp question if there is one. A pick is the player's choice, shown like any other
        // sign and flagged picked; "I'm not sure" keeps the options ("Pisces or Aries", Oct 3), flagged declined; a fact added later settles it.
        public int[] MoonAsk { get; private set; } = new int[0];
        public string MoonQuestion => MoonAsk.Length < 2 ? "" : "The Moon was in " + Options(MoonAsk) + " on the day you were born. Which do you go by?"; // Claude's draft from the cusp question (owner writes)
        bool AskMoon()
        {
            var moon = pending?.Worked.Point("moon"); var chosen = ChoiceFor("moon");
            if (moon == null || moon.Settled || (chosen != null && moon.signs.Contains(chosen.sign))) return false;
            MoonAsk = moon.signs; BirthStep = "moon"; Note = ""; Logged?.Invoke("moon_uncertain"); return true;
        }
        public bool PickMoon(int choice) // an index into MoonAsk, or -1: "I'm not sure"
        {
            if (Screen != SliceScreen.Birth || BirthStep != "moon" || choice < -1 || choice >= MoonAsk.Length) return false;
            Choose("moon", choice < 0 ? -1 : MoonAsk[choice], choice < 0 ? "declined" : "picked");
            Logged?.Invoke(choice < 0 ? "moon_declined" : "moon_picked"); Commit(); return true;
        }
        public static string ClockTime(int minute) { int h = minute / 60, m = minute % 60; return (h % 12 == 0 ? 12 : h % 12) + ":" + m.ToString("00") + (h < 12 ? " am" : " pm"); }
        public string CuspQuestion => CuspFrom < 0 ? "" : CuspMinute >= 0
            ? "The Sun moved from " + Zodiac.Seats[CuspFrom].Name + " into " + Zodiac.Seats[CuspTo].Name + " on the day you were born, at " + ClockTime(CuspMinute) + ". Your birth time decides which side of that line you landed on. Which sign do you go by?" // the owner's draft (owner writes)
            : "The Sun moved from " + Zodiac.Seats[CuspFrom].Name + " into " + Zodiac.Seats[CuspTo].Name + " on the day you were born. Your birth time and place decide which side of that line you landed on. Which sign do you go by?"; // with no place there is no clock to give the minute on: Claude's draft from the owner's (owner writes)
        public const string CuspUnsure = "I'm not sure", CuspWhyLink = "Why?", CuspWhy = "The Sun reaches each sign at an exact minute, and that minute shifts a little every year. Birthdays near the change are called cusps."; // the owner's drafts
        public bool PickCuspSun(int choice) // 0: the sign it left, 1: the sign it entered, -1: "I'm not sure"
        {
            if (Screen != SliceScreen.Birth || BirthStep != "cusp" || choice < -1 || choice > 1) return false;
            Choose("sun", choice == 0 ? CuspFrom : choice == 1 ? CuspTo : CuspNoon, choice < 0 ? "noon" : "picked");
            Logged?.Invoke(choice < 0 ? "cusp_sun_noon" : "cusp_sun_picked"); if (!AskMoon()) Commit(); return true;
        }
        // The opening's words and "Your Birth"'s (Oct 7): Claude's drafts from the approved recommendation, checked by Dante; the owner rewrites them
        public const string BirthQuestion = "Do you know when you were born?", KnowBirthday = "Yes, I know my birthday", SkipBirthday = "I'll skip it";
        public const string SkipAsk = "Which sign do you go by?", SkipUnsure = "I'm not sure";
        public const string StartFromAries = "Then we'll start from Aries, the first sign on the wheel. Your journal keeps a place for your birthday."; // after "I'm not sure", in the note where the retired random sun's line was
        public const string NearestCity = "The nearest city you know works well.", PlaceUnknown = "I don't know where";
        public const string BirthPageTitle = "Your Birth", DateRow = "Date", TimeRow = "Time", PlaceRow = "Place", AddWords = "Add", Unknown = "unknown";
        public const string RisingWhy = "Your rising is the sign that was coming up over the eastern horizon at the minute you were born. It changes through the day, so I need your birth time and place to find it.";
        public const string RisingMeanwhile = "Until then, you can choose a rising, and I'll keep it.";
        public const string ChooseRising = "Choose my rising", ChangeRising = "Change my rising", AddBirthTime = "Add my birth time", RisingAsk = "Your rising sign";
        public const string NeedDayAndPlace = "To find your rising, I also need the day and the place.";
        public const string TakesThePlace = "It takes the place of the one you chose.", StaysLearned = "Everything you've learned stays as it is.";
        public static string RisingIs(int seat) => "Your rising is " + Zodiac.Seats[seat].Name + ".";
        public static string MoonIs(int seat) => "Your moon is " + Zodiac.Seats[seat].Name + ".";
        public static string SunAtThatMinute(int seat) => "At that minute, your sun was in " + Zodiac.Seats[seat].Name + ".";
        public static string SunIs(int seat) => "Your sun is " + Zodiac.Seats[seat].Name + ".";
        public static string ItIs(int seat) => Zodiac.Seats[seat].Name + " it is.";
        public const int FirstBirthYear = 1900;             // the time zone table runs from 1900
        public void ChooseBirth(string choice)
        {
            if (Screen != SliceScreen.Birth || Amending) return;
            BirthChoice = choice == "chart" || choice == "skip" ? choice : ""; OpeningPath = BirthChoice;
            Facts = new BirthFacts(); draft = new BirthFacts(); Chart = new WorkedChart(); choices.Clear(); pending = null; Note = ""; ClearCusp();
            BirthStep = BirthChoice == "chart" ? "date" : BirthChoice == "skip" ? "sun-pick" : "";
            Logged?.Invoke("birth_choice:" + BirthChoice);
        }
        // the steps in order; on "Your Birth" only the facts still missing are asked
        static readonly string[] FactSteps = { "date", "time", "place" };
        bool Asks(string step) => !Amending || (step == "date" ? !Facts.HasDate : step == "time" ? !Facts.HasTime : !Facts.HasPlace);
        void Advance(string after)
        {
            for (int i = Array.IndexOf(FactSteps, after) + 1; i < FactSteps.Length; i++) if (Asks(FactSteps[i])) { BirthStep = FactSteps[i]; return; }
            Settle();
        }
        bool OnFactStep(string step) => Screen == SliceScreen.Birth && BirthChoice == "chart" && BirthStep == step;
        public bool SetBirthDate(int year, int month, int day)
        {
            if (!OnFactStep("date")) return false;
            if (year < FirstBirthYear || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month) || new DateTime(year, month, day) > DateTime.UtcNow.Date.AddDays(1)) return false;
            draft.SetDate(year, month, day); Note = ""; Logged?.Invoke("birth_date_entered"); Advance("date"); return true;
        }
        public bool SetBirthTime(int minuteOfDay) // -1: "I don't know my birth time"
        {
            if (!OnFactStep("time") || minuteOfDay < -1 || minuteOfDay >= 24 * 60) return false;
            draft.SetTime(minuteOfDay); Note = ""; Logged?.Invoke(minuteOfDay < 0 ? "birth_time_unknown" : "birth_time_entered"); Advance("time"); return true;
        }
        public bool SetBirthPlace(Place place) // null: "I don't know where"
        {
            if (!OnFactStep("place")) return false;
            draft.SetPlace(place); Logged?.Invoke(place == null ? "birth_place_unknown" : "birth_place_entered"); Advance("place"); return true;
        }
        // the facts are in: work the chart out; on a cusp day ask the sun, unless the player already chose one of its two signs
        void Settle()
        {
            pending = BirthChart.Work(draft); var sun = pending.Worked.Point("sun"); var chosen = ChoiceFor("sun");
            if (sun != null && !sun.Settled && !(chosen != null && sun.signs.Contains(chosen.sign)))
            {
                CuspFrom = pending.SunFrom; CuspTo = pending.SunTo; CuspNoon = pending.SunNoon; var zone = draft.AsPlace?.Zone ?? -1; CuspMinute = zone >= 0 ? BirthChart.LocalMinuteOf(pending.Ingress, zone) : -1;
                BirthStep = "cusp"; Note = ""; Logged?.Invoke("sun_cusp"); return;
            }
            if (!Amending) Logged?.Invoke("chart_worked_out");
            if (!AskMoon()) Commit();
        }
        struct Shown { public int sun, moon, rising; public bool sunChosen, risingChosen; }
        Shown Snapshot() => new Shown { sun = SunSign, moon = MoonSign, rising = RisingSign, sunChosen = FromChoice("sun"), risingChosen = RisingChosen };
        // The record takes the facts. A point already worked out keeps its saved sign (a later fix to the maths never moves it); a choice gives
        // way to a sign the facts now decide.
        void Commit()
        {
            var before = Snapshot(); bool timeAdded = !Facts.HasTime && draft.HasTime;
            if (pending != null)
            {
                var fresh = pending.Worked; var merged = new List<ChartPoint>();
                foreach (var name in new[] { "sun", "moon", "rising" })
                {
                    var old = Chart.Point(name); var now = fresh.Point(name);
                    bool moved = old != null && old.Settled && (now == null || !now.Settled || now.signs[0] != old.signs[0]); // only a change to the maths could move it
                    var keep = moved ? old : now ?? old; if (keep != null) merged.Add(keep); // agreeing, the fresh point's narrower range is kept
                }
                Chart = new WorkedChart { maths = fresh.maths, points = merged.ToArray() };
                Facts = draft.Clone();
            }
            choices.RemoveAll(c => Worked(c.point));
            ClearCusp(); pending = null; BirthStep = "done";
            if (!Amending) { Note = ChartNote(); return; }
            var lines = new List<string>();
            if (RisingSign >= 0 && !RisingChosen && (before.rising < 0 || before.risingChosen)) { lines.Add(RisingIs(RisingSign)); if (before.risingChosen && before.rising != RisingSign) lines.Add(TakesThePlace); }
            if (MoonSign >= 0 && before.moon != MoonSign) lines.Add(MoonIs(MoonSign));
            if (before.sun < 0 && HasSunSign) lines.Add(SunIs(SunSign));
            else if (before.sunChosen && HasSunSign && !FromChoice("sun") && before.sun != SunSign) lines.Add(timeAdded ? SunAtThatMinute(SunSign) : SunIs(SunSign));
            if (lines.Count > 0) lines.Add(StaysLearned);
            Logged?.Invoke("birth_added"); FinishAmending(lines);
        }
        // the skip path's question (and nothing else asks the sun this way): a sign, or -1 for "I'm not sure"
        public bool PickSkipSun(int seat)
        {
            if (Screen != SliceScreen.Birth || Amending || BirthStep != "sun-pick" || seat < -1 || seat > 11) return false;
            Choose("sun", seat, seat < 0 ? "declined" : "picked"); Chart = new WorkedChart { maths = BirthChart.Maths }; BirthStep = "done";
            Note = seat < 0 ? StartFromAries : ChartNote(); Logged?.Invoke(seat < 0 ? "sun_declined" : "sun_picked"); return true;
        }
        // the sign grid answers whichever question it is showing (the Moon's has its own buttons, its options)
        public bool PickSign(int seat) => BirthStep == "rising-pick" ? PickRising(seat) : PickSkipSun(seat);
        // what the birth screen says once the chart is known (Claude's drafts, owner writes)
        string ChartNote()
        {
            if (!HasSunSign) return StartFromAries;
            string Sign(int seat) => Zodiac.Seats[seat].Name; bool dated = Facts.HasDate, noTime = dated && !Facts.HasTime, noPlace = dated && Facts.HasTime && !Facts.HasPlace;
            string why = noPlace ? " Without a birth place, your rising sign stays unknown." : noTime ? " Without a birth time, your rising sign stays unknown." : " Your rising sign stays unknown."; // the second: Dante-checked (Oct 7)
            var line = "Your sun sign is " + Sign(SunSign);
            if (MoonPair) return line + " and your moon sign " + MoonWords + (noPlace ? " (your birth place decides which)." : " (it changed sign that day).") + (RisingSign >= 0 ? " Your rising sign is " + Sign(RisingSign) + "." : why); // drafts (owner writes); with a time and no place, the place decides (Oct 7)
            if (MoonSign >= 0 && RisingSign >= 0) return line + ", your moon sign " + Sign(MoonSign) + ", and your rising sign " + Sign(RisingSign) + ".";
            if (MoonSign >= 0) return line + " and your moon sign " + Sign(MoonSign) + "." + why;
            return line + "." + (noTime ? " Without a birth time, your moon and rising signs stay unknown." : " Your moon and rising signs stay unknown.");
        }
        // the Keeper's record's Big Three line, as words (the record draws each glyph in its own font); "unknown" where it can't be known (owner, A)
        public static string SignOrUnknown(int seat) => seat >= 0 && seat < 12 ? Zodiac.Seats[seat].Name : "unknown";
        public string BigThreeLine => "☉ " + SignOrUnknown(SunSign) + " · ☽ " + MoonWords + " · ↑ " + SignOrUnknown(RisingSign);
        // ---- "Your Birth" (Oct 7): the Big Three line opens it for every player. It shows the date, the time and the place as given, or unknown
        // with Add; with the rising unknown or chosen, the journal's paragraph and Choose / Change my rising; after a change, a line for each.
        public bool CanOpenBirthPage => AtJournal && JournalAt == JournalView.Landing && BirthDone;
        public bool OpenBirthPage() { if (!CanOpenBirthPage) return false; JournalAt = JournalView.Birth; BirthLines = new List<string>(); Logged?.Invoke("journal_birth"); return true; }
        public bool OnBirthPage => AtJournal && JournalAt == JournalView.Birth;
        public List<string> BirthLines { get; private set; } = new List<string>(); // what changed, shown on "Your Birth" until it is left
        public bool CanAddFacts => OnBirthPage && !(Facts.HasDate && Facts.HasTime && Facts.HasPlace);
        public bool CanAddTime => CanAddFacts && !Facts.HasTime;
        public bool CanChooseRising => OnBirthPage && (RisingSign < 0 || RisingChosen);
        public bool ShowsRisingWhy => RisingSign < 0 || RisingChosen;
        // Add: the opening's boxes for whatever is missing, in order (date, time, place); the record changes only when they finish
        public bool StartAddFacts(bool forTime = false)
        {
            if (!CanAddFacts) return false;
            choiceBefore = BirthChoice; Amending = true; BirthChoice = "chart"; draft = Facts.Clone(); pending = null; ClearCusp();
            Note = forTime && !Facts.HasDate ? NeedDayAndPlace : ""; Screen = SliceScreen.Birth; BirthStep = "";
            for (int i = 0; i < FactSteps.Length; i++) if (Asks(FactSteps[i])) { BirthStep = FactSteps[i]; break; }
            Logged?.Invoke("birth_add:" + BirthStep); return true;
        }
        public bool StartChooseRising()
        {
            if (!CanChooseRising) return false;
            choiceBefore = BirthChoice; Amending = true; BirthChoice = "rising"; Note = ""; Screen = SliceScreen.Birth; BirthStep = "rising-pick";
            Logged?.Invoke("rising_choose"); return true;
        }
        public bool PickRising(int seat)
        {
            if (Screen != SliceScreen.Birth || !Amending || BirthStep != "rising-pick" || seat < 0 || seat > 11 || Worked("rising")) return false;
            Choose("rising", seat, "picked"); BirthStep = "done"; Logged?.Invoke("rising_chosen"); FinishAmending(new List<string> { ItIs(seat) }); return true;
        }
        public bool CanCancelAmending => Screen == SliceScreen.Birth && Amending;
        public bool CancelAmending() { if (!CanCancelAmending) return false; Logged?.Invoke("birth_add_cancelled"); FinishAmending(new List<string>()); return true; }
        void FinishAmending(List<string> lines)
        {
            Amending = false; BirthChoice = choiceBefore; BirthStep = "done"; draft = Facts.Clone(); pending = null; ClearCusp(); Note = "";
            Screen = SliceScreen.Journal; JournalAt = JournalView.Birth; BirthLines = lines;
        }
        // ---- the save's birth record: the three parts, and the old flat fields as copies an older build can still read ----
        public void WriteBirth(SaveData save)
        {
            save.birth = Facts.Clone(); save.choices = choices.Select(c => new ChoiceRecord { point = c.point, sign = c.sign, how = c.how }).ToArray();
            save.chart = new WorkedChart { maths = Chart.maths, points = (Chart.points ?? new ChartPoint[0]).Select(p => new ChartPoint { point = p.point, signs = (int[])p.signs.Clone(), from = p.from, to = p.to, status = p.status }).ToArray() };
            save.chartFrom = OpeningPath; save.sunSign = SunSign; save.moonSign = MoonSign; save.risingSign = RisingSign; save.moonFrom = MoonFrom; save.moonTo = MoonTo;
            save.sunBasis = SunBasis == "picked" || SunBasis == "noon" ? SunBasis : ""; save.birthDate = Facts.date ?? ""; save.birthMinute = Facts.Minute;
            save.birthPlace = Facts.place ?? ""; save.birthZone = Facts.zone ?? ""; save.birthLatitude = Facts.latitude; save.birthLongitude = Facts.longitude;
        }
        void ReadBirth(SaveData save)
        {
            choices.Clear(); OpeningPath = save.chartFrom ?? "";
            bool parts = (save.birth != null && save.birth.Any) || (save.choices != null && save.choices.Length > 0) || (save.chart?.points != null && save.chart.points.Length > 0);
            if (save.version >= 5 && (parts || save.sunSign < 0)) // a save with empty parts and a flat sun is read the old way below
            {
                Facts = (save.birth ?? new BirthFacts()).Clone(); Chart = save.chart ?? new WorkedChart(); if (Chart.points == null) Chart.points = new ChartPoint[0];
                if (save.choices != null) foreach (var c in save.choices) if (c != null && !string.IsNullOrEmpty(c.point)) Choose(c.point, c.sign, c.how ?? "");
                return;
            }
            // a version 4 save, converted once (owner, Oct 7): the chart path's facts move over and the chart is worked out again, the saved
            // signs winning where they differ (Oct 3: never change data already calculated); the other paths keep their signs as choices
            Facts = new BirthFacts(); Chart = new WorkedChart { maths = BirthChart.Maths };
            if (OpeningPath == "chart" && !string.IsNullOrEmpty(save.birthDate) && save.birthDate.Length == 10)
            {
                Facts.date = save.birthDate; Facts.SetTime(save.birthMinute); Facts.place = save.birthPlace ?? ""; Facts.zone = save.birthZone ?? ""; Facts.latitude = save.birthLatitude; Facts.longitude = save.birthLongitude;
                Chart = BirthChart.Work(Facts).Worked;
                foreach (var (name, saved) in new[] { ("sun", save.sunSign), ("moon", save.moonSign), ("rising", save.risingSign) })
                {
                    var p = Chart.Point(name); if (p != null && p.Settled && saved >= 0 && saved < 12 && p.signs[0] != saved) p.signs = new[] { saved };
                }
                if (!Worked("sun") && (save.sunBasis == "picked" || save.sunBasis == "noon")) Choose("sun", save.sunSign, save.sunBasis);
                return;
            }
            string how = OpeningPath == "known" ? "entered" : OpeningPath == "chosen" ? "assigned" : "legacy";
            Choose("sun", save.sunSign, how);
            if (OpeningPath == "known") { if (save.moonSign >= 0) Choose("moon", save.moonSign, how); if (save.risingSign >= 0) Choose("rising", save.risingSign, how); }
        }
        public bool CanContinue =>
            Screen == SliceScreen.Identity || (Screen == SliceScreen.Birth && !Amending && BirthDone) ||
            Screen == SliceScreen.Atrium || (Screen == SliceScreen.Wing && KeyRevealed && AtriumStage == 1) || Screen == SliceScreen.AtriumReturn ||
            (Screen == SliceScreen.Chamber && Ended);
        public bool Continue()
        {
            if (!CanContinue) return false;
            Screen = Screen == SliceScreen.Identity ? SliceScreen.Birth :
                Screen == SliceScreen.Birth ? SliceScreen.Atrium :
                Screen == SliceScreen.Atrium ? SliceScreen.Hub : // Build T (owner, APK playtest, Sept 29): the opening hands the player the Atrium; Caspar leads them to the Zodiac Wing, and the Dial is tapped from there
                Screen == SliceScreen.Wing ? SliceScreen.AtriumReturn :
                Screen == SliceScreen.AtriumReturn ? SliceScreen.Chamber : SliceScreen.Hub;
            if (Screen == SliceScreen.Hub) { Note = ""; if (AtriumStage < 2 && KeyInserted) { AtriumStage = 2; Deck.IntroduceAll(Sitting); } Walk.Enter(Room.Atrium, "entry"); } // Stage 2 still begins on the return from the Chamber, not on the opening's walk
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
            if (Screen != SliceScreen.Wing || (AtriumStage < 2 && KeyRevealed)) return false; // Build T: from the first lesson on; once Key 1 shows, Continue carries the opening on
            Screen = SliceScreen.WingRoom; Logged?.Invoke("screen_entered:wingroom"); return true;
        }
        public bool LeaveWing()
        {
            if (Screen != SliceScreen.WingRoom) return false; // Build T: the Stage 1 walk goes both ways too
            Screen = SliceScreen.Hub; Note = "";
            if (WheelComplete && AtriumStage == 2) { AtriumStage = 3; Logged?.Invoke("atrium_stage_3"); }
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
        // Build AA (owner, Sept 30; Sept 26 notes 7 and 14): one index page, the Wheel, replaces the contents and its five lists. A Wheel /
        // Table switch lays the same twelve seats out as the wheel or as the Elemental Table; tabs recolour either view. A view or a tab
        // appears only once its pattern has entered the deck, so the book starts blank and fills in. Tapping a seat shows a preview; its
        // page links the signs that share each fact. Illumination plus Ribbons (Sept 23) still carry the deck's state; no state word is drawn.
        public JournalView JournalAt { get; private set; }
        public bool JournalTableView { get; private set; }  // the Wheel / Table switch
        public JournalLens Lens { get; private set; }
        public int JournalSelected { get; private set; } = -1; // the seat whose preview shows, or -1
        public int JournalSign { get; private set; } // an index into JournalSigns
        public SliceScreen JournalFrom { get; private set; } = SliceScreen.Hub;
        public bool AtJournal => Screen == SliceScreen.Journal;
        public bool Knows(ItemKind kind) => Deck.Items.Any(i => i.Kind == kind && i.entered);
        public bool CanOpenJournal => (AtHub || AtWingRoom || AtChamberRoom) && JournalSigns.Count > 0;
        // Batch 2 (owner, Oct 1: the journal's architecture; 1b C, 1e A): the first-ever open shows the title page, once (saved); the first open
        // in each session shows the landing; after that the journal opens where it was left. A session is one run of the game.
        public bool JournalTitled { get; private set; }
        bool journalLanded;
        public bool OpenJournal()
        {
            if (!CanOpenJournal) return false;
            JournalFrom = Screen;
            if (!JournalTitled) { JournalAt = JournalView.Title; JournalTitled = true; }
            else if (!journalLanded || JournalAt == JournalView.Title) JournalAt = JournalView.Landing;
            if (JournalAt == JournalView.Landing) journalLanded = true;
            if (!JournalLenses.Contains(Lens)) Lens = JournalLens.Element; if (!CanJournalTable) JournalTableView = false;
            Screen = SliceScreen.Journal; Logged?.Invoke("journal_opened"); return true;
        }
        public bool CloseJournal()
        {
            if (!AtJournal) return false;
            Screen = JournalFrom; if (Note == "gated") Note = ""; BirthLines = new List<string>(); Logged?.Invoke("journal_closed"); return true;
        }
        // The pages (the approved board, 86bcbn6w6, Oct 1-2): the title page's beat ends on the landing; the landing holds the Keeper's
        // record and two doors, Practice and Contents; Contents lists the chapters (the Wheel, the Library Map, and Sealed rows for those
        // to come) and links back to the landing; each chapter links back to Contents. The Practice door opens Practice (owner, Oct 3, below).
        public const string JournalTitle = "Your Journal", TitlePageLine = "These pages fill as you learn.", KeeperHeading = "KEEPER"; // 1e A (owner, Oct 1); the board's words
        public const string PracticeDoor = "Practice", ContentsDoor = "Contents", ContentsDoorLine = "Every chapter", ContentsTitle = "Contents";
        public const string WheelChapterLine = "Signs and patterns", MapTitle = "The Library Map", MapChapterLine = "The rooms you have woken", SealedChapter = "Sealed";
        public const string BackToJournal = "\u2039 Your Journal", BackToContents = "\u2039 Contents"; // the board's links, top left
        public const int SealedChapters = 2; // the board's two Sealed rows: the chapters to come, such as the Books
        // The inscription, the owner's line (Oct 2 evening, 1c B): its line breaks may flex to fit the Keeper's record; its words may not.
        public const string InscriptionRest = "I'm your journal, and I'll keep a record of what you learn from the Library.";
        public static readonly string[] InscriptionBreaks = { "I'm your journal, and I'll keep a record", "of what you learn from the Library." }; // the breaks it takes wherever they fit, so every screen sets it alike
        public static string InscriptionGreeting(string name) => "What's up, " + name + ".";
        public static string Inscription(string name) => InscriptionGreeting(name) + " " + InscriptionRest;
        // The Keys collected and the Books opened, one line (Oct 1, 1d): a Book counts once all its locks are filled.
        public string KeysLine => Keys + (Keys == 1 ? " Key" : " Keys") + (BooksOpen > 0 ? " \u00b7 " + BooksOpen + (BooksOpen == 1 ? " Book" : " Books") : "");
        public bool CanJournalLand => AtJournal && JournalAt == JournalView.Title;
        public bool JournalLand() { if (!CanJournalLand) return false; JournalAt = JournalView.Landing; journalLanded = true; Logged?.Invoke("journal_landing"); return true; }
        public bool CanJournalContents => AtJournal && (JournalAt == JournalView.Landing || JournalAt == JournalView.Wheel || JournalAt == JournalView.Map);
        public bool JournalToContents() { if (!CanJournalContents) return false; JournalAt = JournalView.Contents; Logged?.Invoke("journal_contents"); return true; }
        public bool CanJournalHome => AtJournal && (JournalAt == JournalView.Contents || JournalAt == JournalView.Practice || JournalAt == JournalView.Birth); // "‹ Your Journal" on Contents, on Practice's list and on "Your Birth"
        public bool JournalToLanding() { if (!CanJournalHome) return false; JournalAt = JournalView.Landing; BirthLines = new List<string>(); Logged?.Invoke("journal_landing"); return true; }
        public static readonly string[] Chapters = { "wheel", "map" };
        public bool OpenChapter(string chapter)
        {
            if (!CanJournalHome || !Chapters.Contains(chapter)) return false;
            JournalAt = chapter == "wheel" ? JournalView.Wheel : JournalView.Map; Logged?.Invoke("journal_chapter:" + chapter); return true;
        }
        // Practice (owner, Oct 3: step 4 approved as scoped; the board on 86bcbn6w6). The door opens the list of concepts learned, and only
        // those: a concept joins when its lesson finishes (the Elements at Key 1, the Symbols at Key 2, the Modalities with their lesson, the
        // Elemental Table at Key 3, the Opposites at Key 4), so nothing is asked before it is taught. A round asks all of a concept's questions
        // in a fresh order, the choices shuffled; an answer shows right or wrong and its why, then Next; the round ends with the journal's line
        // and Back to Practice; "‹ Practice" leaves at any time. No score, streaks or spaced review: none of it is saved and the deck is untouched.
        public const string PracticeTitle = "Practice", PracticeDoorLine = "What you know", BackToPractice = "\u2039 Practice", QuizNextWords = "Next", QuizBackWords = "Back to Practice"; // the board's words, Claude's drafts (owner, Oct 3: rewritten or approved before they ship); the door's count is removed, and its line is a draft beside Contents' "Every chapter"
        public bool ConceptLearned(PracticeConcept concept)
        {
            switch (concept.unlock) { case "key1": return Keys >= 1; case "key2": return Keys >= 2; case "modalities": return ModalitiesComplete; case "key3": return Keys >= 3; case "key4": return Keys >= 4; default: return false; }
        }
        public List<PracticeConcept> PracticeConcepts => PracticeBook.Data.concepts.Where(ConceptLearned).ToList();
        public bool CanJournalPractice => AtJournal && JournalAt == JournalView.Landing && PracticeConcepts.Count > 0;
        public bool OpenPractice() { if (!CanJournalPractice) return false; JournalAt = JournalView.Practice; Logged?.Invoke("journal_practice"); return true; }
        Random practiceDice = new Random();
        public void SeedPractice(int seed) { practiceDice = new Random(seed); } // the checks' rounds come out the same on every run
        int[] Shuffled(int count) { var order = Enumerable.Range(0, count).ToArray(); for (int i = count - 1; i > 0; i--) { int j = practiceDice.Next(i + 1); int t = order[i]; order[i] = order[j]; order[j] = t; } return order; }
        int[] quizOrder = new int[0], choiceOrder = new int[0];
        public PracticeConcept QuizConcept { get; private set; }
        public int QuizAt { get; private set; }
        public int QuizPicked { get; private set; } = -1; // the choice picked, in the order shown, or -1
        public bool QuizOver { get; private set; }
        public bool OnQuiz => AtJournal && JournalAt == JournalView.Quiz && QuizConcept != null;
        public bool StartQuiz(int row)
        {
            var learned = PracticeConcepts;
            if (!AtJournal || JournalAt != JournalView.Practice || row < 0 || row >= learned.Count || learned[row].questions.Length == 0) return false;
            QuizConcept = learned[row]; quizOrder = Shuffled(QuizConcept.questions.Length); QuizAt = 0; QuizOver = false; NewQuestion();
            JournalAt = JournalView.Quiz; Logged?.Invoke("practice_round:" + QuizConcept.id); return true;
        }
        void NewQuestion() { QuizPicked = -1; choiceOrder = Shuffled(QuizQuestion.choices.Length); }
        public PracticeQuestion QuizQuestion => QuizConcept != null && QuizAt < quizOrder.Length ? QuizConcept.questions[quizOrder[QuizAt]] : null;
        public int QuizCount => QuizConcept != null ? QuizConcept.questions.Length : 0;
        public string QuizCounter => !OnQuiz || QuizOver ? "" : (QuizAt + 1) + " of " + QuizCount;
        public string[] QuizChoices => !OnQuiz || QuizOver ? new string[0] : choiceOrder.Select(i => QuizQuestion.choices[i]).ToArray();
        public int QuizRight => !OnQuiz || QuizOver ? -1 : Array.IndexOf(choiceOrder, QuizQuestion.answer); // the right choice, in the order shown
        public bool CanQuizAnswer => OnQuiz && !QuizOver && QuizPicked < 0;
        public bool AnswerQuiz(int choice)
        {
            if (!CanQuizAnswer || choice < 0 || choice >= choiceOrder.Length) return false;
            QuizPicked = choice; Logged?.Invoke(choice == QuizRight ? "practice_right" : "practice_wrong"); return true;
        }
        public string QuizFeedback => !OnQuiz || QuizOver || QuizPicked < 0 ? "" : (QuizPicked == QuizRight ? PracticeBook.Data.right : PracticeBook.Data.wrong) + " " + QuizQuestion.why;
        public bool CanQuizNext => OnQuiz && !QuizOver && QuizPicked >= 0;
        public bool NextQuiz()
        {
            if (!CanQuizNext) return false;
            if (QuizAt + 1 >= quizOrder.Length) { QuizOver = true; Logged?.Invoke("practice_end:" + QuizConcept.id); return true; }
            QuizAt++; NewQuestion(); Logged?.Invoke("practice_next"); return true;
        }
        public string QuizEnd => OnQuiz && QuizOver ? QuizConcept.end : "";
        public bool CanQuizBack => OnQuiz; // "‹ Practice" at any time, and Back to Practice at the round's end
        public bool QuizToPractice() { if (!CanQuizBack) return false; QuizConcept = null; QuizOver = false; QuizPicked = -1; JournalAt = JournalView.Practice; Logged?.Invoke("practice_list"); return true; }
        // The views and tabs learned so far: the Table once the table has entered the deck (Key 3); Element at Key 1, Modality with the
        // modalities, Polarity and Opposites with the opposites (Key 4).
        public bool CanJournalTable => Knows(ItemKind.Grid);
        public List<JournalLens> JournalLenses
        {
            get
            {
                var lenses = new List<JournalLens>();
                if (Knows(ItemKind.Element)) lenses.Add(JournalLens.Element);
                if (Knows(ItemKind.Modality)) lenses.Add(JournalLens.Modality);
                if (Knows(ItemKind.Opposite)) { lenses.Add(JournalLens.Polarity); lenses.Add(JournalLens.Opposites); }
                return lenses;
            }
        }
        public static string LensTitle(JournalLens lens) => lens == JournalLens.Element ? "Element" : lens == JournalLens.Modality ? "Modality" : lens == JournalLens.Polarity ? "Polarity" : "Opposites"; // placeholder (owner writes)
        public const string WheelTitle = "The Wheel"; // placeholder (owner writes)
        bool OnWheel => AtJournal && JournalAt == JournalView.Wheel;
        public bool SetJournalTable(bool table)
        {
            if (!OnWheel || (table && !CanJournalTable) || JournalTableView == table) return false;
            JournalTableView = table; Logged?.Invoke("journal_view:" + (table ? "table" : "wheel")); return true;
        }
        public bool SetLens(JournalLens lens)
        {
            if (!OnWheel || !JournalLenses.Contains(lens) || Lens == lens) return false;
            Lens = lens; Logged?.Invoke("journal_lens:" + LensTitle(lens).ToLowerInvariant()); return true;
        }
        // A seat: tap it to frame it and show its preview; the preview opens its page.
        public bool SelectSeat(int seat)
        {
            if (!OnWheel || seat < 0 || seat >= 12 || !JournalSigns.Contains(seat)) return false;
            JournalSelected = seat; Logged?.Invoke("journal_seat:" + seat); return true;
        }
        public bool CanOpenSelected => OnWheel && JournalSelected >= 0;
        public bool OpenSign(int seat)
        {
            if (!AtJournal || !JournalSigns.Contains(seat)) return false;
            JournalAt = JournalView.Sign; JournalSign = JournalSigns.IndexOf(seat); JournalSelected = seat; Logged?.Invoke("journal_sign:" + seat); return true;
        }
        public bool OpenSelected() => CanOpenSelected && OpenSign(JournalSelected);
        public bool CanJournalWheel => AtJournal && JournalAt == JournalView.Sign;
        public bool JournalToWheel() { if (!CanJournalWheel) return false; JournalAt = JournalView.Wheel; JournalSelected = JournalSignSeat; Logged?.Invoke("journal_wheel"); return true; }
        public bool CanJournalNext => AtJournal && JournalAt == JournalView.Sign && JournalSign < JournalSigns.Count - 1;
        public bool CanJournalPrev => AtJournal && JournalAt == JournalView.Sign && JournalSign > 0;
        public bool JournalNext() { if (!CanJournalNext) return false; JournalSign++; JournalSelected = JournalSignSeat; Logged?.Invoke("journal_page:" + JournalSignSeat); return true; }
        public bool JournalPrev() { if (!CanJournalPrev) return false; JournalSign--; JournalSelected = JournalSignSeat; Logged?.Invoke("journal_page:" + JournalSignSeat); return true; }
        // A seat's state, gold for mastery (owner, Sept 30), re-dressing Illumination: 0 not met (an empty socket), 1 met (a silver ring,
        // the picture at line-art ink), 2 practising (a gold ring, colour by streak), 3 mastered (today's gilt edge: the gold-leaf ring).
        public int SeatState(int seat) => !SignItems(seat).Any(i => i.entered) ? 0 : SignGilt(seat) ? 3 : SignInk(seat) >= 1 ? 2 : 1;
        public static readonly string[] SeatStateNames = { "", "met", "practising", "mastered" }; // test evidence only, never drawn
        // The sign page's links: the other signs met that share a fact the player has learned (none for a fact not learned yet).
        public List<int> SignKin(int seat, ItemKind kind)
        {
            seat = Zodiac.Wrap(seat); if (!SignKnows(seat, kind)) return new List<int>();
            if (kind == ItemKind.Opposite) return JournalSigns.Contains(Zodiac.Opposite(seat)) ? new List<int> { Zodiac.Opposite(seat) } : new List<int>();
            return JournalSigns.Where(other => other != seat && (kind == ItemKind.Element ? other % 4 == seat % 4 : other % 3 == seat % 3)).ToList();
        }
        // Caspar's own words for the polarities (owner, worksheet section 12: "Fire and Air signs are Yang, outward and active. Earth and Water signs are Yin, inward and receptive.")
        public static string PolarityWords(int seat) => Zodiac.PolarityAt(seat) == "Yang" ? "outward and active" : "inward and receptive";
        // What a pair of opposites shares (his section 12 line: "they share a modality and a polarity, only the element differs"). placeholder (owner writes)
        public static string PairShares(int seat) => "shares " + Zodiac.ModalityAt(seat) + " · " + Zodiac.PolarityAt(seat) + "; only the element differs";
        // The preview's one line: the facts learned, in reading order.
        public string SeatSummary(int seat) => string.Join(" · ", SignFacts(seat).Where(f => f[0] != "Opposite").Select(f => f[1]));
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
            save.revealsPlayed = lesson.RevealsPlayed.OrderBy(p => p).ToArray(); // Build Z
            save.keys = Math.Max(Keys, lesson.Key4Earned ? 4 : grid.Key3Earned ? 3 : lesson.Key2Earned ? 2 : lesson.KeyEarned ? 1 : 0);
            return save;
        }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned) { return ToSave(lit, kin, keyEarned, null, null); }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned, bool[] litMod, bool[] kinMod) { return ToSave(lit, kin, keyEarned, litMod, kinMod, null, false); }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned, bool[] litMod, bool[] kinMod, bool[] gridPlaced, bool gridEvidence) { return ToSave(lit, kin, keyEarned, litMod, kinMod, gridPlaced, gridEvidence, false, null, 0, false); }
        public SaveData ToSave(bool[] lit, bool[] kin, bool keyEarned, bool[] litMod, bool[] kinMod, bool[] gridPlaced, bool gridEvidence, bool polarityShown, bool[] oppKnown, int built, bool builderEvidence)
        {
            var save = new SaveData { playerName = PlayerName, lit = (bool[])lit.Clone(), kin = (bool[])kin.Clone(), keyEarned = keyEarned, litMod = litMod != null ? (bool[])litMod.Clone() : new bool[12], kinMod = kinMod != null ? (bool[])kinMod.Clone() : new bool[3], modalitiesStarted = ModalitiesStarted,
                gridPlaced = gridPlaced != null ? (bool[])gridPlaced.Clone() : new bool[12], gridEvidence = gridEvidence, gridStarted = GridStarted,
                polarityShown = polarityShown, oppKnown = oppKnown != null ? (bool[])oppKnown.Clone() : new bool[Zodiac.OppositePairs], oppositesStarted = OppositesStarted, built = built, builderEvidence = builderEvidence, locksFilled = LocksFilled,
                wheelComplete = WheelComplete, atriumStage = AtriumStage, keys = Keys, journalTitled = JournalTitled, glyphStage = GlyphStage, glyphIndex = GlyphIndex, cleanRuns = CleanRuns, deck = Deck.Items.Select(i => new ReviewItem { seat = i.seat, kind = i.kind, state = i.state, streak = i.streak, interval = i.interval, dueDay = i.dueDay, entered = i.entered }).ToArray(), sittings = Sittings, reviewsChecked = Sittings };
            WriteBirth(save); return save;
        }
        // Resumes at the Hub (a second sitting). Only meaningful once the Key was earned and the Hub reached.
        public bool Restore(SaveData save)
        {
            if (save == null || save.atriumStage < 2 || (save.version < 5 && save.sunSign < 0)) return false; // a version 4 save always had a sun; version 5 may not (Oct 7)
            PlayerName = save.playerName ?? ""; BirthChoice = "saved"; JournalTitled = save.journalTitled;
            ReadBirth(save); BirthStep = "done"; // read, never worked out again: only a fact added on "Your Birth" works it out
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
