using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum LessonPhase { Encounter, Rule, Guided, Transfer, Independent, Complete, Paused, Optional, Continuation, AllLit, Review, GlyphNames, GlyphWheel, Key2, ModalityGuided, ModalityOwn, ModalityPaused, ModalityComplete }

    public sealed class DialLesson
    {
        public readonly DialModel Dial;
        public readonly bool[] Lit = new bool[12];
        public readonly bool[] Kin = new bool[12];
        readonly HashSet<int> exposedProblems = new HashSet<int>();
        readonly HashSet<int> recoveryStarts = new HashSet<int>();
        public LessonPhase Phase { get; private set; } = LessonPhase.Encounter;
        public bool KeyEarned { get; private set; }
        public bool IndependentEvidence { get; private set; }
        // User-approved provisional authoring rule: initial failed encounter + two replacements,
        // shared across the whole activity. Different zodiac starts never reset this budget.
        public int RecoveryEncounters { get; private set; }
        bool recovering;
        int family;
        int lastStart;
        // The player's sun sign (Sept 11 copy session: derived or assigned at the birth prompt; no chart).
        public int Sun { get; private set; } = 1;
        public int GuidedFamily => Sun % 4;
        public int SecondFamily => (GuidedFamily + 3) % 4;
        public int OptionalFamily => (GuidedFamily + 1) % 4;
        // Zodiac Wing entrance (Sept 11 decision): dormant Dial wakes to the player, Caspar reacts,
        // a simulated hesitation, disbelief, composure, then teaching. Linear; no branching.
        public int IntroStep { get; private set; }
        public const int IntroTeaching = 7;
        public bool IntroAuto => Phase == LessonPhase.Encounter && (IntroStep == 1 || IntroStep == 4);
        public bool DialDormant => Phase == LessonPhase.Encounter && IntroStep <= 1;
        // A Level 2 problem shows the count once, one click per beat, before the player takes over.
        public bool CountBeatPending { get; private set; }
        public string Message { get; private set; }
        public bool IsProblem => Phase == LessonPhase.Guided || Phase == LessonPhase.Independent || Phase == LessonPhase.Optional || Phase == LessonPhase.Continuation || Phase == LessonPhase.Review || Phase == LessonPhase.GlyphWheel || Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn;
        public bool InModalities => Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn || Phase == LessonPhase.ModalityPaused || Phase == LessonPhase.ModalityComplete;
        // ---- v0.3: glyphs and Key 2 (owner's form, Sept 12: Part A name the glyph by tap, Part B find the glyph on the wheel) ----
        public readonly bool[] GlyphNamed = new bool[12];    // Part A done for this seat
        public readonly bool[] GlyphPlaced = new bool[12];   // Part B done for this seat
        public readonly bool[] NameRevealed = new bool[12];  // Level 2 in Part B shows the name on its seat
        public int GlyphIndex { get; private set; }          // next seat in zodiac order within the current part
        public int GlyphMisses { get; private set; }         // Part A misses on the current glyph
        public bool GlyphEvidence { get; private set; }      // any Level 0/1 answer in Part B (the central skill)
        public bool Key2Earned { get; private set; }
        // v0.3 revision, build 3: the difficulty ramp. After Key 2 the book and the wheel can be replayed; each clean replay
        // (at least one unassisted placement) counts, and from the first clean run on the symbols come shuffled, with harder
        // wrong names and no Aries anchor. Four choices always (owner, Sept 13).
        public bool Practice { get; private set; }
        public int CleanRuns { get; private set; }
        public bool Hard => CleanRuns > 0;
        readonly int[] order = Enumerable.Range(0, 12).ToArray();
        public int SeatAt(int index) => order[index];
        public event Action<bool> PracticeFinished; // clean
        public void SetCleanRuns(int runs) { CleanRuns = Math.Max(0, runs); }
        static readonly int[] LookAlike = { 1, 0, 11, 4, 3, 7, 0, 5, 9, 3, 4, 2 }; // a symbol that reads like this seat's, by shape
        public static int[] OptionsFor(int seat, bool hard, int salt)
        {
            int[] o = hard
                ? new[] { seat, LookAlike[seat], Zodiac.Wrap(seat + 4), Zodiac.Wrap(seat + 8) }   // look-alike plus the same element
                : new[] { seat, Zodiac.Wrap(seat + 3), Zodiac.Wrap(seat + 6), Zodiac.Wrap(seat + 9) };
            int r = (seat + salt) % 4; var result = new int[4];
            for (int i = 0; i < 4; i++) result[(i + r) % 4] = o[i];
            return result;
        }
        public bool CanPractice => Key2Earned && (Phase == LessonPhase.Key2 || Phase == LessonPhase.AllLit || Phase == LessonPhase.Complete || Phase == LessonPhase.Paused || Phase == LessonPhase.ModalityComplete || Phase == LessonPhase.ModalityPaused);
        // ---- Build A (Unit 1.2, part 1): the modalities on the same Dial, three seats forward, three families of four. No Key here. ----
        public readonly bool[] LitMod = new bool[12];
        public readonly bool[] KinMod = new bool[3];
        public bool ModalitiesComplete => LitMod.All(v => v);
        public int ModalityFamiliesComplete => KinMod.Count(v => v);
        public bool ModalityUnitStarted { get; private set; }
        int modalityFamily, modalityAssisted;
        public bool CanBeginModalities => Key2Earned && !ModalitiesComplete && (Phase == LessonPhase.Key2 || Phase == LessonPhase.AllLit || Phase == LessonPhase.Complete || Phase == LessonPhase.Paused || Phase == LessonPhase.ModalityPaused || Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn);
        public static string ModalityName(int seat) => Zodiac.ModalityAt(seat);
        static string ModalityMembers(int f) => SignName(f) + ", " + SignName(f + 3) + ", " + SignName(f + 6) + ", and " + SignName(f + 9);
        public bool BeginModalities()
        {
            if (!CanBeginModalities) return false;
            if ((Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn) && Dial.Active) return true;
            bool first = !ModalityUnitStarted; ModalityUnitStarted = true; modalityAssisted = 0;
            int f = NextUnlitModalityFamily(); if (f < 0) return false;
            bool guided = f == Sun % 3 && !KinMod[f] && first;
            modalityFamily = f; Phase = guided ? LessonPhase.ModalityGuided : LessonPhase.ModalityOwn;
            int start = guided ? Sun : FirstUnlitInModalityFamily(f, out _);
            if (!LitMod[start]) LitMod[start] = true;
            StartModalityProblem(start, guided ? 2 : 0);
            Message = guided
                ? "The wheel keeps a second pattern. Every third sign shares a kind: cardinal, fixed, or mutable.\n" + SignName(Sun) + " is " + ModalityName(Sun) + ". So are " + ModalityMembers(f) + ".\n" + Message // placeholder (owner writes)
                : "Now the " + ModalityName(f).ToLowerInvariant() + " signs: " + ModalityMembers(f) + ".\nStart at " + SignName(start) + ". Three forward each time. I will only watch."; // placeholder (owner writes)
            Dial.Log(first ? "modality_unit_started" : "modality_family_started");
            return true;
        }
        int NextUnlitModalityFamily() { int s = Sun % 3; for (int k = 0; k < 3; k++) { int f = (s + k) % 3; if (!KinMod[f]) return f; } return -1; }
        int FirstUnlitInModalityFamily(int f, out int prev)
        {
            // The chain runs f → f+3 → f+6 → f+9; the first unlit seat is the next target, the seat before it the start.
            for (int i = 0; i < 4; i++) { int seat = Zodiac.Wrap(f + 3 * i); if (!LitMod[seat]) { prev = Zodiac.Wrap(seat - 3); return i == 0 ? seat : prev; } }
            prev = -1; return f;
        }
        void StartModalityProblem(int start, int hint)
        {
            lastStart = Zodiac.Wrap(start);
            Dial.Begin(lastStart, hint, -1, 3);
            CountBeatPending = hint >= 2;
            Message = hint >= 2 ? string.Format(RuleFor(3), SignName(lastStart)) + "\nInspect the framed sign, then press Seal."
                : "Your turn. Find the next " + ModalityName(lastStart).ToLowerInvariant() + " sign from " + SignName(lastStart) + ".\nMove the wheel, inspect the framed sign, then press Seal.";
        }
        void AfterModalityCorrect(DialEvent result)
        {
            LitMod[result.selected_destination] = true;
            int f = modalityFamily;
            bool complete = Enumerable.Range(0, 4).All(i => LitMod[Zodiac.Wrap(f + 3 * i)]);
            if (!complete)
            {
                int target = Enumerable.Range(0, 4).Select(i => Zodiac.Wrap(f + 3 * i)).First(sd => !LitMod[sd]);
                StartModalityProblem(Zodiac.Wrap(target - 3), Phase == LessonPhase.ModalityGuided ? 2 : 0);
                return;
            }
            KinMod[f] = true; Dial.Log("modality_family_completed"); Dial.Home();
            int next = NextUnlitModalityFamily();
            if (next < 0) { Phase = LessonPhase.ModalityComplete; Message = "Twelve seats, three kinds. Cardinal begins, fixed holds, mutable turns.\nThe wheel has one more pattern to give you, and a table to fill."; Dial.Log("modalities_completed"); return; } // placeholder (owner writes)
            Phase = LessonPhase.ModalityOwn; modalityFamily = next;
            int start = FirstUnlitInModalityFamily(next, out _); LitMod[start] = true;
            StartModalityProblem(start, 0);
            Message = (ModalityFamiliesComplete == 1 ? "One kind done. " : "Last kind. ") + "Now the " + ModalityName(next).ToLowerInvariant() + " signs: " + ModalityMembers(next) + ".\nStart at " + SignName(start) + ". Three forward each time."; // placeholder (owner writes)
            Dial.Log("modality_family_started");
        }
        void AfterModalityDemonstration()
        {
            // A worked example lights the seat without evidence; three of them in one sitting pause the unit (reappearance cap).
            LitMod[Zodiac.Destination(lastStart, 3)] = true; modalityAssisted++;
            if (modalityAssisted >= 3) { Dial.Home(); Phase = LessonPhase.ModalityPaused; Message = "Let us stop the second pattern here for now.\nWe will pick it up when you return."; return; } // placeholder (owner writes)
            AfterModalityCorrect(Dial.Events.Last());
        }
        public string RuleFor(int step) => step == 3
            ? "Find the next sign of the same kind after {0} on the wheel.\nCount each sign after it: one, two, three."
            : RuleLine;

        // Closing the book mid-practice abandons that pass: the unit is already earned, so nothing is lost.
        public void AbandonPractice()
        {
            if (!Practice || Phase != LessonPhase.GlyphNames) return;
            Practice = false; Phase = LessonPhase.Key2; GlyphMisses = 0; GlyphIndex = 0;
            for (int i = 0; i < 12; i++) { GlyphNamed[i] = true; GlyphPlaced[i] = true; order[i] = i; }
            Dial.Log("symbol_practice_abandoned");
        }
        public bool BeginPractice()
        {
            if (!CanPractice) return false;
            Practice = true; GlyphEvidence = false; GlyphMisses = 0; GlyphIndex = 0; Dial.Home();
            for (int i = 0; i < 12; i++) { GlyphNamed[i] = false; GlyphPlaced[i] = false; NameRevealed[i] = false; order[i] = Hard ? Zodiac.Wrap(i * 5 + 1 + CleanRuns) : i; } // stride 5 visits every seat
            Phase = LessonPhase.GlyphNames;
            Message = Hard ? "Again, and harder this time. The order is mine now, and the wrong names will look right.\nTell me which sign each one belongs to." : "Once more, from the start.\nTell me which sign each one belongs to."; // placeholder (owner writes)
            Dial.Log("symbol_practice_started");
            return true;
        }
        public int Keys => (KeyEarned ? 1 : 0) + (Key2Earned ? 1 : 0);
        public bool GlyphsShown => Phase == LessonPhase.GlyphNames || Phase == LessonPhase.GlyphWheel || Phase == LessonPhase.Key2 || (WheelComplete && Key2Earned);
        public bool NamesHidden => Phase == LessonPhase.GlyphWheel;
        public bool CanBeginGlyphs => WheelComplete && !Key2Earned && (Phase == LessonPhase.AllLit || Phase == LessonPhase.Complete || Phase == LessonPhase.Paused || Phase == LessonPhase.GlyphNames || Phase == LessonPhase.GlyphWheel);
        public event Action<int, bool, bool> GlyphNamedEvent; // seat, correct, eligible (Part A)
        public bool BeginGlyphs()
        {
            if (!CanBeginGlyphs) return false;
            if (Phase == LessonPhase.GlyphNames || Phase == LessonPhase.GlyphWheel) return true;
            Phase = LessonPhase.GlyphNames; GlyphIndex = 0; GlyphMisses = 0; Dial.Home();
            for (int i = 0; i < 12; i++) order[i] = i;
            for (int i = 0; i < 12; i++) if (GlyphNamed[i]) GlyphIndex = i + 1;
            if (GlyphIndex >= 12) { StartGlyphWheel(); return true; }
            Message = GlyphIntro;
            Dial.Log("glyph_unit_started");
            return true;
        }
        public int CurrentGlyph => Phase == LessonPhase.GlyphNames ? (GlyphIndex < 12 ? SeatAt(GlyphIndex) : -1) : Phase == LessonPhase.GlyphWheel ? Dial.Target : -1;
        public bool AllNamed { get { for (int i = 0; i < 12; i++) if (!GlyphNamed[i]) return false; return true; } } // Part A complete
        public const string ShelfFirst = "The names you know. Their symbols wait on the shelf.\nRead them there first; then the wheel will hide its names."; // placeholder (owner writes)
        public const string ShelfDark = "The shelf is dark. Light the wheel first."; // placeholder (owner writes)
        public const string ShelfRead = "Twelve symbols, read. The book has nothing more for now."; // placeholder (owner writes)
        // Four names: the answer plus three others, in a stable order per seat so tests and the page agree.
        public int[] GlyphOptions(int seat) => OptionsFor(seat, Hard, Hard ? CleanRuns : 0);
        public const string GlyphIntro = "Every sign carries a symbol of its own. Twelve symbols, older than the names.\nTell me which sign each one belongs to."; // placeholder (owner writes)
        public string GlyphNameResult { get; private set; } = ""; // the last Part A result line, kept while the wheel takes over
        public bool AnswerGlyphName(int seat)
        {
            if (Phase != LessonPhase.GlyphNames || GlyphIndex >= 12) return false;
            int target = SeatAt(GlyphIndex); bool correct = Zodiac.Wrap(seat) == target;
            if (correct)
            {
                bool eligible = GlyphMisses <= 1; // Level 0 or a Level 1 nudge
                GlyphNamed[target] = true; Message = GlyphNameResult = "Yes. " + SignName(target) + ".";
                GlyphNamedEvent?.Invoke(target, true, eligible); Dial.Log("glyph_named", true, eligible, "DirectSeat");
                NextGlyphName(); return true;
            }
            GlyphMisses++;
            if (GlyphMisses == 1) { Message = GlyphNameResult = "Not that one. This symbol belongs to a " + Element(target) + " sign."; Dial.Log("hint_requested"); return false; }
            // Second miss: Level 2 reveals the name; no evidence; move on.
            GlyphNamed[target] = true; Message = GlyphNameResult = "This is the symbol of " + SignName(target) + ". Remember it.";
            GlyphNamedEvent?.Invoke(target, false, false); Dial.Log("hint_escalated"); Dial.Log("glyph_named", false, false, "DirectSeat");
            NextGlyphName(); return false;
        }
        void NextGlyphName()
        {
            GlyphMisses = 0; GlyphIndex++;
            if (GlyphIndex >= 12) StartGlyphWheel();
        }
        void StartGlyphWheel()
        {
            Phase = LessonPhase.GlyphWheel; GlyphIndex = 0; RecoveryEncounters = 0; exposedProblems.Clear(); recoveryStarts.Clear();
            for (int i = 0; i < 12; i++) if (GlyphPlaced[SeatAt(i)]) GlyphIndex = i + 1;
            if (GlyphIndex >= 12) { FinishGlyphs(); return; }
            Message = "Now the wheel hides its names. Only the symbols remain.\nI will name a sign; you turn until its symbol sits under the bracket, then press Seal."; // placeholder (owner writes)
            BeginGlyphProblem(SeatAt(GlyphIndex));
        }
        void BeginGlyphProblem(int seat)
        {
            Dial.Begin(Zodiac.Wrap(seat + 3 + (seat * 5) % 7), 0, seat); CountBeatPending = false; // start three to nine seats away, never on or beside the answer (owner playtest, Sept 14)
            Message = "Find the symbol of " + SignName(seat) + ".\nTurn until it sits under the bracket, then press Seal.";
        }
        void FinishGlyphs()
        {
            Dial.Home();
            if (Practice)
            {
                Practice = false; Phase = LessonPhase.Key2; bool clean = GlyphEvidence;
                if (clean) { CleanRuns++; Message = "Twelve symbols again, and sharper.\nThe book will ask harder next time."; Dial.Log("symbol_practice_clean", true, true); } // placeholder (owner writes)
                else Message = "Twelve symbols again, but I did most of the finding.\nThe book will wait for you."; // placeholder (owner writes)
                PracticeFinished?.Invoke(clean);
                return;
            }
            if (GlyphEvidence) { Phase = LessonPhase.Key2; Key2Earned = true; Message = "Twelve symbols, twelve names, in their order. You read the wheel now.\nKeeper Key 2 is yours."; Dial.Log("key2_earned", true, true); } // placeholder (owner writes)
            else { Phase = LessonPhase.Paused; Message = "We reached the end of the symbols, but I did most of the finding.\nRest, and we will try the symbols again when you return."; }
        }
        // v0.2 (Q05): Unit 1.1 continuation on the same Dial, and compressed review problems.
        public int FamiliesComplete => Enumerable.Range(0, 4).Count(f => Kin[f]);
        public bool WheelComplete => Lit.All(v => v);
        LessonPhase phaseBeforeReview;
        public event Action<int, bool, bool> ReviewFinished; // seat, correct, eligible
        public bool CanContinueUnit => KeyEarned && !WheelComplete && (Phase == LessonPhase.Complete || Phase == LessonPhase.Paused || Phase == LessonPhase.Continuation);
        int NextUnlitFamily() { for (int f = 0; f < 4; f++) if (!Kin[f]) return f; return -1; }
        public bool BeginContinuation()
        {
            if (!CanContinueUnit) return false;
            if (Phase == LessonPhase.Continuation && Dial.Active) return true;
            RecoveryEncounters = 0; exposedProblems.Clear(); recoveryStarts.Clear(); recovering = false;
            family = NextUnlitFamily(); if (family < 0) return false;
            Phase = LessonPhase.Continuation; Lit[family] = true;
            StartProblem(family, 0);
            // Placeholder copy (owner writes; Q05 decision 7): third family with less guidance, fourth on the player's own.
            Message = FamiliesComplete == 2
                ? "Now the " + Element(family) + " family. You have done this twice.\nStart at " + SignName(family) + ". I will only watch."
                : "The last family. " + Element(family) + ".\nStart at " + SignName(family) + ". This one is all yours.";
            Dial.Log("unit11_family_started");
            return true;
        }
        public bool BeginReview(int seat) { return BeginReview(seat, 4); }
        public bool BeginReview(int seat, int step)
        {
            if (Phase == LessonPhase.Review || IsProblem) return false;
            phaseBeforeReview = Phase; Phase = LessonPhase.Review;
            Dial.Begin(seat, 0, -1, step); CountBeatPending = false;
            Message = step == 3 ? "Find the next sign of the same kind." : "Find the next sign in this family."; // Compressed form: start already framed, one line, Seal.
            return true;
        }
        public void EndReview() { if (Phase != LessonPhase.Review) return; Dial.Home(); Phase = phaseBeforeReview; }
        // Second sitting (Q05 decision 5): rebuild lesson state from the local save. The intro is already behind the player.
        public void RestoreProgress(int sun, bool[] lit, bool[] kin, bool keyEarned)
        {
            Sun = Zodiac.Wrap(sun); family = GuidedFamily;
            for (int i = 0; i < 12; i++) { Lit[i] = lit != null && i < lit.Length && lit[i]; Kin[i] = kin != null && i < kin.Length && kin[i]; }
            KeyEarned = keyEarned; IndependentEvidence = keyEarned; IntroStep = IntroTeaching;
            Phase = !keyEarned ? LessonPhase.Rule : WheelComplete ? LessonPhase.AllLit : LessonPhase.Complete;
            Key2Earned = false; GlyphEvidence = false;
            Dial.Home();
            Message = WheelComplete ? "The whole wheel is lit." : "Welcome back. The wheel remembers you.";
        }
        public DialLesson(Func<double> clock) { Dial = new DialModel(clock); family = GuidedFamily; Message = IntroLine(0); }
        static string SignName(int seat) => Zodiac.Seats[Zodiac.Wrap(seat)].Name;
        static string Element(int seat) => Zodiac.Seats[Zodiac.Wrap(seat)].Element;
        static string FamilyMembers(int f) => SignName(f) + ", " + SignName(f + 4) + ", and " + SignName(f + 8);
        public void SetSunSign(int seat)
        {
            if (Phase != LessonPhase.Encounter || IntroStep != 0) return;
            Sun = Zodiac.Wrap(seat); family = GuidedFamily;
        }
        string IntroLine(int step)
        {
            switch (step)
            {
                case 0: return "This is the Zodiac Wing. The wheel at its center has been still for as long as I can remember.\nGo ahead. Step closer.";
                case 1: return "The Dial stirs. Faded symbols along the rim begin to glow. The ring shifts.";
                case 2: return "It responds to you.\nI have stood in this room a thousand times and it never so much as flickered for me. You carry the Ancestor's blood. There is no question now.";
                case 3: return "It wants you to solve it. Go ahead.";
                case 4: return "...";
                case 5: return "You... do not know astrology.\nThe Keeper of this Library does not know astrology. How is that possible?";
                case 6: return "No matter. I cannot touch the wheel. It must be you.\nBut I can teach you. Look here.";
                default: return "Your sun sign is " + SignName(Sun) + ". " + SignName(Sun) + " is a " + Element(Sun) + " sign.\nIn your world, the sun sign is the one most people know. There is much more to a chart than that, but this is where we start.";
            }
        }
        public string TeachingLine =>
            "Every sign on this wheel carries one of four elements: Fire, Earth, Air, or Water. Each element binds three signs together, like a family.\nThe " + Element(Sun) + " family is " + FamilyMembers(GuidedFamily) + ".\nLook at where they sit on the wheel. They are not side by side. They are spaced apart, evenly. That spacing is the pattern. Let me show you how to find it.";
        public void Continue()
        {
            if (Phase == LessonPhase.Encounter)
            {
                if (IntroStep < IntroTeaching) { IntroStep++; Message = IntroLine(IntroStep); if (IntroStep == IntroTeaching) { Lit[Sun] = true; Dial.PositionSilently(Sun); } return; }
                Phase = LessonPhase.Rule; Message = TeachingLine;
            }
            else if (Phase == LessonPhase.Rule)
            {
                Phase = LessonPhase.Guided;
                StartProblem(Sun, 2);
            }
            else if (Phase == LessonPhase.Transfer)
            {
                family = SecondFamily; Phase = LessonPhase.Independent; Lit[family] = true;
                StartProblem(family, 0);
            }
        }
        void StartProblem(int start, int hint)
        {
            lastStart = Zodiac.Wrap(start);
            Dial.Begin(lastStart, hint);
            if (hint >= 2) exposedProblems.Add(lastStart);
            CountBeatPending = hint >= 2;
            string sign = SignName(lastStart);
            Message = hint >= 2 ? "Start at " + sign + ". Count each sign after it: one, two, three, four.\nInspect the framed sign, then press Seal." :
                "Your turn. Find the next sign in this family from " + sign + ".\nMove the wheel, inspect the framed sign, then press Seal.";
        }
        public void CountBeatShown() { CountBeatPending = false; }
        public const string RuleLine = "Find the next elemental sign after {0} on the wheel.\nCount each sign after it: one, two, three, four."; // Level 2 (owner wording, Sept 13)
        public bool CanAsk => Dial.CanAsk && (Phase == LessonPhase.Independent || Phase == LessonPhase.Optional || Phase == LessonPhase.Continuation || Phase == LessonPhase.Review || Phase == LessonPhase.GlyphWheel || Phase == LessonPhase.ModalityOwn);
        // Ask Caspar: the Level 2 reminder on request, wherever a rule exists. An answer after it earns no evidence.
        public bool AskCaspar()
        {
            if (!CanAsk || !Dial.Ask()) return false;
            if (Phase == LessonPhase.GlyphWheel) { NameRevealed[Dial.Target] = true; Message = "Look: the name shows on its seat now. Turn to " + SignName(Dial.Target) + ", then press Seal."; }
            else { Message = string.Format(RuleFor(Dial.Forward), SignName(Dial.Start)) + "\nInspect the framed sign, then press Seal."; if (Phase != LessonPhase.Review) { CountBeatPending = true; exposedProblems.Add(Dial.Start); } }
            return true;
        }
        public DialEvent Seal()
        {
            var result = Dial.Commit();
            if (result == null) return null;
            int step = Dial.Attempts + (Dial.Asked ? 1 : 0); // the asked reminder counts as the second step of the ladder
            if (Phase == LessonPhase.GlyphWheel)
            {
                int target = Dial.Target;
                if (result.correctness)
                {
                    GlyphPlaced[target] = true; if (result.evidence_eligible) GlyphEvidence = true;
                    Message = "Yes. " + SignName(target) + ", in its place."; Dial.Log("glyph_placed", true, result.evidence_eligible);
                }
                else if (step == 1) Message = Hard ? "Not that one. Look at the shape again, then find it on the wheel." : "Not that one. Find Aries first, then count forward to it."; // no Aries anchor after a clean run
                else if (step == 2) { NameRevealed[target] = true; Message = "Look: the name shows on its seat now. Turn to " + SignName(target) + ", then press Seal."; }
                else Message = "Watch me find it.\nThen the next symbol.";
                return result;
            }
            if (Phase == LessonPhase.Review)
            {
                if (result.correctness) { Message = "Yes. " + SignName(result.selected_destination) + " is " + (Dial.Forward == 3 ? ModalityName(result.selected_destination).ToLowerInvariant() : Element(result.selected_destination)) + "."; Dial.Home(); ReviewFinished?.Invoke(Dial.Start, true, result.hint_level <= 1); }
                else if (Dial.Attempts >= 2) { Message = "It is " + SignName(Zodiac.Destination(Dial.Start, Dial.Forward)) + ". We will come back to it."; Dial.Home(); ReviewFinished?.Invoke(Dial.Start, false, false); }
                else { Message = "Not that one. Try once more."; }
                return result;
            }
            if (!result.correctness)
            {
                if (RecoveryEncounters == 0) RecoveryEncounters = 1;
                if (Dial.HintLevel >= 2) exposedProblems.Add(Dial.Start);
                if (step == 2) CountBeatPending = true;
                Message = step == 1 ? "Not that one. Count your steps again.\nYou can move on from where you are." :
                    step == 2 ? string.Format(RuleFor(Dial.Forward), SignName(Dial.Start)) + "\nInspect the framed sign, then press Seal." :
                    "Watch me do one.\nThen you will try again from a new sign.";
            }
            return result;
        }
        public string CorrectLine(DialEvent result) =>
            "Yes. " + SignName(result.selected_destination) + (InModalities ? " is " + ModalityName(result.selected_destination).ToLowerInvariant() + ", like " + SignName(Dial.Start) + "." : " is a " + Element(result.selected_destination) + " sign, like your sun sign.") + "\n" +
            (result.evidence_eligible ? "You found that one on your own." : "We found that one together.");
        public void AfterCorrect(DialEvent result)
        {
            if (result == null || !result.correctness) return;
            if (Phase == LessonPhase.Review) return;
            if (Phase == LessonPhase.GlyphWheel) { NextGlyphProblem(); return; }
            if (Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn) { AfterModalityCorrect(result); return; }
            if (Phase == LessonPhase.Optional)
            {
                Dial.Home(); Phase = LessonPhase.Complete;
                Message = "Well done. You are picking it up naturally.\nKeep it up.";
                return;
            }
            Lit[result.selected_destination] = true;
            if (Phase == LessonPhase.Independent && result.evidence_eligible) IndependentEvidence = true;
            if (recovering) { Dial.Log("problem_recovered", true, result.evidence_eligible); recovering = false; }
            AdvanceOrRecover(result.hint_level >= 2 && (Phase == LessonPhase.Independent || Phase == LessonPhase.Continuation));
        }
        void NextGlyphProblem()
        {
            GlyphIndex++;
            if (GlyphIndex >= 12) FinishGlyphs(); else BeginGlyphProblem(SeatAt(GlyphIndex));
        }
        public int DemonstrationTarget => Phase == LessonPhase.GlyphWheel ? Dial.Target : Zodiac.Destination(Dial.Start, Dial.Forward);
        public void RevealDemonstration()
        {
            if (Phase == LessonPhase.GlyphWheel) { GlyphPlaced[Dial.Target] = true; NameRevealed[Dial.Target] = true; return; } // Worked exposure: no evidence, no fresh equivalent for a fixed set.
            if (Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn) return; // lit in AfterModalityDemonstration
            if (Phase != LessonPhase.Optional) Lit[Zodiac.Destination(lastStart)] = true;
            // Worked exposure illuminates but never writes eligible answer evidence.
        }
        public void AfterDemonstration()
        {
            if (Phase == LessonPhase.GlyphWheel) { Dial.Home(); NextGlyphProblem(); return; }
            if (Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn) { AfterModalityDemonstration(); return; }
            if (Phase == LessonPhase.Optional)
            {
                Dial.Home(); Phase = LessonPhase.Complete;
                Message = "That one was practice only.\nYour lesson record is unchanged.";
                return;
            }
            QueueFresh();
        }
        void AdvanceOrRecover(bool needsFresh)
        {
            bool complete = Enumerable.Range(0, 3).All(i => Lit[family + i * 4]);
            if (complete && !Kin[family])
            {
                for (int i = 0; i < 3; i++) Kin[family + 4 * i] = true;
                Dial.Log("family_completed");
            }
            if (needsFresh) { QueueFresh(); return; }
            if (!complete)
            {
                int nextDestination = Enumerable.Range(0, 3).Select(i => family + i * 4).First(s => !Lit[s]);
                StartProblem(Zodiac.Wrap(nextDestination - 4), Phase == LessonPhase.Guided ? 2 : 0);
                return;
            }
            if (Phase == LessonPhase.Continuation)
            {
                Dial.Home();
                int next = NextUnlitFamily();
                if (next < 0) { Phase = LessonPhase.AllLit; Message = "Twelve seats. Four families. The whole wheel is lit.\nThe room is warmer for it. Come, let us go back."; Dial.Log("wheel_completed"); }
                else { Phase = LessonPhase.Complete; BeginContinuation(); }
                return;
            }
            if (Phase == LessonPhase.Guided)
            {
                Dial.Home(); Phase = LessonPhase.Transfer;
                Message = "There. Three " + Element(family) + " signs, all joined. You are a quick study.\nNow " + Element(SecondFamily) + ". Same pattern. I will let you lead.";
            }
            else if (IndependentEvidence)
            {
                Dial.Home(); Phase = LessonPhase.Complete; KeyEarned = true;
                Message = "Two families down. You are halfway through the wheel.\nThe other two will be here when you are ready."; // Amended canon line (Sept 11).
                Dial.Log("key1_earned", true, true);
                Dial.Log("optional_problem_offered");
            }
            else QueueFresh();
        }
        void QueueFresh()
        {
            if (RecoveryEncounters == 0) RecoveryEncounters = 1;
            var fresh = Enumerable.Range(0, 3).Select(i => family + i * 4)
                .Where(s => s != lastStart && !exposedProblems.Contains(s) && !recoveryStarts.Contains(s)).ToArray();
            if (RecoveryEncounters >= 3 || fresh.Length == 0)
            {
                Dial.Home(); Phase = LessonPhase.Paused;
                Message = "Let us stop here for now. No Key yet.\nWe will try again later.";
                return;
            }
            RecoveryEncounters++;
            int start = fresh[0]; recoveryStarts.Add(start); recovering = true;
            StartProblem(start, 0);
        }
        public void BeginOptional()
        {
            if (Phase != LessonPhase.Complete || !KeyEarned || FamiliesComplete > 2) return;
            Dial.Log("optional_problem_accepted");
            Phase = LessonPhase.Optional;
            // A separate one-problem third-family probe; never changes the six-seat lesson record.
            StartProblem(OptionalFamily, 0);
            Message = "One more, if you like. Start from " + SignName(OptionalFamily) + ", in the " + Element(OptionalFamily) + " family.\nIt'll be good practice.";
        }
        public void RestoreModalities(bool[] litMod, bool[] kinMod, bool started)
        {
            for (int i = 0; i < 12; i++) LitMod[i] = litMod != null && i < litMod.Length && litMod[i];
            for (int f = 0; f < 3; f++) KinMod[f] = kinMod != null && f < kinMod.Length && kinMod[f];
            ModalityUnitStarted = started;
            if (ModalitiesComplete) { Phase = LessonPhase.ModalityComplete; Message = "Three kinds, twelve seats. The wheel remembers."; } // placeholder (owner writes)
        }
        public void RestoreGlyphs(int stage, int index, bool key2)
        {
            // stage 0: none; 1: Part A done through index (or all); 2: Key 2 earned.
            for (int i = 0; i < 12; i++) { GlyphNamed[i] = stage >= 2 || (stage == 1 && i < index) || (stage == 0 && false); GlyphPlaced[i] = stage >= 2; }
            if (stage == 0) for (int i = 0; i < 12; i++) GlyphNamed[i] = i < index;
            Key2Earned = key2; GlyphEvidence = key2;
            if (key2) { Phase = LessonPhase.Key2; Message = "Twelve symbols, twelve names. You read the wheel."; }
        }
        public void Say(string text) { Message = text; } // Slice beats speak through the same panel.
        public string SeatLabel(int seat)
        {
            var sign = Zodiac.Seats[seat];
            if (NamesHidden && !NameRevealed[seat]) return "Symbol " + sign.Glyph + ", position " + (seat + 1) + " of 12, " + (Dial.Selected == seat ? "selected" : "not selected");
            return sign.Name + (GlyphsShown ? ", symbol " + sign.Glyph : "") + (LitMod[seat] ? ", " + ModalityName(seat).ToLowerInvariant() : "") + ", position " + (seat + 1) + " of 12, " +
                (Dial.Selected == seat ? "selected" : "not selected") +
                (Lit[seat] ? ", " + sign.Element + (Kin[seat] ? ", family complete" : ", lit") : ", dormant");
        }
    }
}
