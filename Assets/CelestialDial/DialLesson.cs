using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum LessonPhase { Encounter, Rule, Guided, Transfer, Independent, Complete, Paused, Optional, Continuation, AllLit, Review, GlyphNames, GlyphWheel, Key2, ModalityGuided, ModalityOwn, ModalityPaused, ModalityComplete, Polarity, OppositeGuided, OppositeOwn, OppositePaused, OppositesComplete, BuilderName, BuilderOpposite, BuilderShare, BuilderPaused, Key4 }

    public sealed class DialLesson
    {
        public readonly DialModel Dial;
        // Persistence observes completed transitions, never the evaluator's intermediate answer events.
        public event Action ProgressCommitted;
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
        public bool IsProblem => Phase == LessonPhase.Guided || Phase == LessonPhase.Independent || Phase == LessonPhase.Optional || Phase == LessonPhase.Continuation || Phase == LessonPhase.Review || Phase == LessonPhase.GlyphWheel || Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn || InOppositeProblem;
        // The element-family problems, the only phases whose answer_correct is sign → element evidence (evidence routing audit, Sept 17).
        public bool InElementProblem => Phase == LessonPhase.Guided || Phase == LessonPhase.Independent || Phase == LessonPhase.Optional || Phase == LessonPhase.Continuation;
        // Build F: a unit begun and neither complete nor paused resumes on the next visit without the fork (a reload restores no phase, so this reads the unit's own state).
        public bool UnitInProgress => (AllNamed && !Key2Earned) || (ModalityUnitStarted && !ModalitiesComplete && Phase != LessonPhase.ModalityPaused) || (OppositesStarted && !Key4Earned && Phase != LessonPhase.OppositePaused && Phase != LessonPhase.BuilderPaused);
        public bool InModalities => Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn || Phase == LessonPhase.ModalityPaused || Phase == LessonPhase.ModalityComplete;
        // ---- Build C (Unit 1.3): polarity, the six opposite pairs, and the builder on the same Dial. Key 4 for three signs built. ----
        // Build I (owner playtest, Sept 23): the wheel poses its own challenge on its face; Caspar teaches and answers.
        // The symbol challenge names its target, fixed while the wheel turns (owner ruling: the name, so the symbol is still recalled).
        public string Challenge
        {
            get
            {
                if (!IsProblem || !Dial.Active) return "";
                if (Phase == LessonPhase.GlyphWheel) return Dial.Target >= 0 ? SignName(Dial.Target) : "";
                int s = Dial.Start;
                return Dial.Forward == 6 ? "Across from " + SignName(s)
                    : Dial.Forward == 3 ? "Next " + Zodiac.ModalityAt(s) + " after " + SignName(s)
                    : "Next " + Element(s) + " after " + SignName(s);
            }
        }
        public bool InOppositeProblem => Phase == LessonPhase.OppositeGuided || Phase == LessonPhase.OppositeOwn || Phase == LessonPhase.BuilderOpposite;
        public bool InOpposites => Phase == LessonPhase.Polarity || Phase == LessonPhase.OppositeGuided || Phase == LessonPhase.OppositeOwn || Phase == LessonPhase.OppositePaused || Phase == LessonPhase.OppositesComplete;
        public bool InBuilder => Phase == LessonPhase.BuilderName || Phase == LessonPhase.BuilderOpposite || Phase == LessonPhase.BuilderShare || Phase == LessonPhase.BuilderPaused || Phase == LessonPhase.Key4;
        public bool Key3Held { get; private set; }                 // the table's Key, held by the slice; the last pattern waits for it
        public void SetKey3(bool held) { Key3Held = held; }
        public bool PolarityShown { get; private set; }            // after the beat every lit seat shows its side (day / night)
        public int PolarityStep { get; private set; }
        public readonly bool[] OppKnown = new bool[Zodiac.OppositePairs];
        public bool OppositesStarted { get; private set; }
        public bool OppositesComplete => OppKnown.All(v => v);
        public int PairsKnown => OppKnown.Count(v => v);
        int oppAssisted;                                           // worked examples this sitting: the reappearance cap, as in the modality unit
        public int Built { get; private set; }                     // signs built so far (0..3)
        public bool BuilderEvidence { get; private set; }          // any sign built with every step at Level 0/1: the Key 4 rule
        public bool Key4Earned { get; private set; }
        public int BuilderStep { get; private set; }               // 1 name the sign, 2 turn to its opposite, 3 tap what they share; 0 between signs
        public int BuilderTarget { get; private set; } = -1;
        int builderMisses; bool builderAssisted;
        public readonly bool[] Shared = new bool[3];               // kind, side, element marked in step 3
        public static readonly string[] ShareLabels = { "Modality", "Polarity", "Element" }; // owner words (worksheet section 12)
        public int[] BuilderTargets => new[] { Sun, Zodiac.Wrap(Sun + 5), Zodiac.Wrap(Sun + 7) }; // three signs in different rows and columns of the table, opposites distinct from them
        public bool CanBeginOpposites => Key3Held && !Key4Earned && ModalitiesComplete && (Phase == LessonPhase.ModalityComplete || Phase == LessonPhase.Key2 || Phase == LessonPhase.AllLit || Phase == LessonPhase.Complete || Phase == LessonPhase.Paused || InOpposites || InBuilder);
        public const string PolarityLine0 = "The wheel holds one last secret, acolyte: every sign carries a polarity.\nFire and Air signs are Yang, outward and active. Earth and Water signs are Yin, inward and receptive."; // owner (worksheet section 12)
        public const string PolarityLine1 = "Some traditions say masculine and feminine. The Hellenistic astrologers called them day and night. We shall use Yin and Yang.\nLook, the wheel shows each sign's polarity now."; // owner (worksheet section 12)
        public const string OppositesDoneLine = "Six pairs, acolyte. Every sign has a partner straight across the wheel, and none of them are enemies: they share a modality and a polarity, only the element differs.\nNow then, let us see if you can build a sign from its parts."; // owner (worksheet section 12)
        // Build F: every pause points somewhere now: the journal (the Sept 15 brief's missing destination). Placeholder (owner writes).
        public const string JournalNudge = "Your journal holds what we have covered, if you want to read before we go on.";
        public const string OppositesPausedLine = "That is enough for now, acolyte. Rest, and we shall continue where you left off when you return. " + JournalNudge; // owner (worksheet section 12); the journal sentence is Build F's placeholder (section 14)
        public const string BuilderNoEvidenceLine = "Three signs built, though I confess my hand was in most of the building. The Key shall wait for you, acolyte.\nGo, study your journal, and when you return we shall build again. I would see you claim it on your own."; // owner (worksheet section 12)
        public const string Key4Line = "Three signs built from their parts, three partners found across the wheel. Every pattern the wheel holds is yours now, acolyte: elements, symbols, modalities, polarities, and opposites.\nThe Library trembles. Keeper Key 4 is yours."; // owner (worksheet section 12)
        static string Side(int seat) => Zodiac.PolarityAt(seat);
        static string Kind(int seat) => Zodiac.ModalityAt(seat).ToLowerInvariant();
        public bool BeginOpposites()
        {
            if (!CanBeginOpposites) return false;
            if (InOppositeProblem && Dial.Active) return true;
            if (Phase == LessonPhase.BuilderName || Phase == LessonPhase.BuilderShare || Phase == LessonPhase.Polarity || Phase == LessonPhase.OppositesComplete) return true;
            bool first = !OppositesStarted; OppositesStarted = true; oppAssisted = 0;
            if (!PolarityShown) { Phase = LessonPhase.Polarity; PolarityStep = 0; Dial.Home(); Message = PolarityLine0; Dial.Log(first ? "opposites_unit_started" : "polarity_beat_resumed"); return true; }
            if (!OppositesComplete) { StartNextOpposite(); Dial.Log("opposites_resumed"); return true; }
            if (Built >= 3 && BuilderEvidence) { FinishBuilder(); ProgressCommitted?.Invoke(); return true; } // reload during the final sign's presentation hold
            if (Built >= 3 && !Key4Earned) { Built = 0; BuilderEvidence = false; Dial.Log("builder_cleared"); } // three built with help: build again
            StartBuilderSign(); return true;
        }
        void StartNextOpposite()
        {
            for (int k = 0; k < Zodiac.OppositePairs; k++) { int start = Zodiac.Wrap(Sun + k); if (!OppKnown[Zodiac.PairOf(start)]) { StartOppositeProblem(start, 0); return; } }
        }
        void StartOppositeProblem(int start, int hint)
        {
            lastStart = Zodiac.Wrap(start); Phase = hint >= 2 ? LessonPhase.OppositeGuided : LessonPhase.OppositeOwn;
            Dial.Begin(lastStart, hint, -1, 6); CountBeatPending = hint >= 2;
            Message = hint >= 2 ? "Now the final pattern, acolyte: every sign has an opposite, the sign that sits straight across the wheel.\nFrom " + SignName(lastStart) + ", count six signs forward: one, two, three, four, five, six.\nWhen the sign is selected, press Seal." // owner (worksheet section 12)
                : "Your turn now, acolyte. Find the sign that sits across the wheel from " + SignName(lastStart) + ".\nWhen it is selected, press Seal."; // owner (worksheet section 12)
        }
        void AfterOppositeCorrect(DialEvent result)
        {
            OppKnown[Zodiac.PairOf(Dial.Start)] = true;
            if (OppositesComplete) { Dial.Home(); Phase = LessonPhase.OppositesComplete; Message = OppositesDoneLine; Dial.Log("opposites_completed"); return; }
            StartNextOpposite();
        }
        void AfterOppositeDemonstration()
        {
            OppKnown[Zodiac.PairOf(Dial.Start)] = true; oppAssisted++;
            if (oppAssisted >= 3 && !OppositesComplete) { Dial.Home(); Phase = LessonPhase.OppositePaused; Message = OppositesPausedLine; Dial.Log("opposites_paused"); return; }
            AfterOppositeCorrect(Dial.Events.Last());
        }
        // The builder: element and kind given, name the sign, turn to its opposite, tap what the two share.
        public int[] BuilderOptions
        {
            get
            {
                int t = BuilderTarget < 0 ? Sun : BuilderTarget;
                int[] o = { t, Zodiac.Wrap(t + 4), Zodiac.Wrap(t + 8), Zodiac.Wrap(t + 3) }; // the two same-element signs and one same-kind sign
                int r = (t + Built) % 4; var result = new int[4];
                for (int i = 0; i < 4; i++) result[(i + r) % 4] = o[i];
                return result;
            }
        }
        void StartBuilderSign()
        {
            if (Built >= 3) { FinishBuilder(); return; }
            BuilderTarget = BuilderTargets[Built]; BuilderStep = 1; builderMisses = 0; builderAssisted = false; for (int i = 0; i < 3; i++) Shared[i] = false;
            Phase = LessonPhase.BuilderName; Dial.Home();
            Message = Built == 0 ? "Build me a sign from its parts, acolyte. " + Element(BuilderTarget) + " and " + Kind(BuilderTarget) + ". Which sign is that?" : Built == 1 ? "Again. " + Element(BuilderTarget) + " and " + Kind(BuilderTarget) + ". Name it." : "One more. " + Element(BuilderTarget) + " and " + Kind(BuilderTarget) + ". Which sign?"; // owner (worksheet section 12)
            Dial.Log("builder_sign_started");
        }
        public bool AnswerBuilderName(int seat)
        {
            if (Phase != LessonPhase.BuilderName) return false;
            int t = BuilderTarget; seat = Zodiac.Wrap(seat);
            if (seat == t)
            {
                Dial.Log("builder_named", true, builderMisses <= 1, "DirectSeat");
                Message = "Yes, " + SignName(t) + ": " + Element(t) + " and " + Kind(t) + ".\nNow find the sign that sits across the wheel from it, and press Seal."; StartBuilderOpposite(); return true; // owner (worksheet section 12)
            }
            builderMisses++;
            if (builderMisses == 1)
            {
                // Level 1 names the property the wrong sign misses.
                Message = "Not that one, acolyte. " + SignName(seat) + " is " + (Element(seat) != Element(t) ? Element(seat) : Kind(seat)) + ". You are looking for the " + Element(t) + " sign that is " + Kind(t) + "."; // owner (worksheet section 12)
                Dial.Log("hint_requested"); return false;
            }
            builderAssisted = true; Dial.Log("hint_escalated"); Dial.Log("builder_named", false, false, "DirectSeat");
            Message = "It is " + SignName(t) + ": " + Element(t) + " and " + Kind(t) + ".\nNow find the sign that sits across the wheel from it, and press Seal."; StartBuilderOpposite(); return false; // Level 2 reveals the name; no evidence (owner wording, section 12)
        }
        void StartBuilderOpposite()
        {
            Phase = LessonPhase.BuilderOpposite; BuilderStep = 2; lastStart = BuilderTarget;
            Dial.Begin(BuilderTarget, 0, -1, 6); CountBeatPending = false;
        }
        void StartBuilderShare()
        {
            Phase = LessonPhase.BuilderShare; BuilderStep = 3; builderMisses = 0; Dial.Home();
            Message = SignName(BuilderTarget) + " and " + SignName(Zodiac.Opposite(BuilderTarget)) + ", straight across the wheel from one another.\nNow tap what the two share."; // owner (worksheet section 12)
        }
        public bool AnswerBuilderShare(int property)
        {
            if (Phase != LessonPhase.BuilderShare || property < 0 || property > 2) return false;
            int t = BuilderTarget, o = Zodiac.Opposite(t);
            if (property < 2)
            {
                Shared[property] = true; Dial.Log("builder_share_marked");
                if (Shared[0] && Shared[1]) { Message = "Yes. Same modality, same polarity. Only the element differs: " + Element(t) + " against " + Element(o) + "."; Dial.Log("builder_shared", true, builderMisses <= 1); FinishBuilderSign(); return true; } // owner (worksheet section 12)
                Message = "Yes, the " + (property == 0 ? "modality" : "polarity") + ". What else do they share?"; return true; // owner (worksheet section 12)
            }
            builderMisses++;
            if (builderMisses == 1) { Message = "Not the element, acolyte. " + SignName(t) + " is " + Element(t) + " and " + SignName(o) + " is " + Element(o) + ", those differ. Tap what the two do share."; Dial.Log("hint_requested"); return false; } // owner (worksheet section 12)
            builderAssisted = true; Shared[0] = Shared[1] = true; Dial.Log("hint_escalated"); Dial.Log("builder_shared", false, false);
            Message = "Opposites share the modality and the polarity. Only their elements differ: " + Element(t) + " against " + Element(o) + ". Remember this, acolyte."; FinishBuilderSign(); return false; // owner (worksheet section 12)
        }
        void FinishBuilderSign()
        {
            Built++; if (!builderAssisted) BuilderEvidence = true; BuilderStep = 0;
            Dial.Log("builder_sign_built", true, !builderAssisted);
            ProgressCommitted?.Invoke();
        }
        // The view holds on the last line, then asks for the next sign (or the Key).
        public void NextBuilderSign()
        {
            NextBuilderSignCore();
            ProgressCommitted?.Invoke();
        }
        void NextBuilderSignCore()
        {
            if (Phase != LessonPhase.BuilderShare || BuilderStep != 0) return;
            if (Built >= 3) FinishBuilder(); else StartBuilderSign();
        }
        void FinishBuilder()
        {
            Dial.Home();
            if (BuilderEvidence) { Key4Earned = true; Phase = LessonPhase.Key4; Message = Key4Line; Dial.Log("key4_earned", true, true); }
            else { Phase = LessonPhase.BuilderPaused; Message = BuilderNoEvidenceLine; Dial.Log("builder_no_evidence"); }
        }
        public void RestoreOpposites(bool polarityShown, bool[] oppKnown, bool started, int built, bool builderEvidence, bool key4)
        {
            PolarityShown = polarityShown; PolarityStep = polarityShown ? 1 : 0; OppositesStarted = started || polarityShown;
            for (int p = 0; p < Zodiac.OppositePairs; p++) OppKnown[p] = oppKnown != null && p < oppKnown.Length && oppKnown[p];
            Built = Math.Max(0, Math.Min(3, built)); BuilderEvidence = builderEvidence || key4; Key4Earned = key4; BuilderStep = 0; BuilderTarget = -1;
            if (key4) { Phase = LessonPhase.Key4; Message = "Four patterns learned, four Keys earned. The wheel is yours, acolyte, every secret it held now belongs to you."; } // owner (worksheet section 12)
        }
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
                ? "The wheel holds a second pattern, acolyte. Every third sign shares a modality: cardinal, fixed, or mutable.\nYour sign, " + SignName(Sun) + ", is " + ModalityName(Sun) + ", and so are " + ModalityMembers(f) + ".\nNow find the next " + ModalityName(Sun) + " sign after " + SignName(Sun) + " upon the wheel. Count each sign forward: one, two, three. When the sign is selected, press Seal." // owner (worksheet section 10)
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
            Message = hint >= 2 ? string.Format(RuleFor(3), SignName(lastStart)) + "\nWhen the sign is selected, press Seal."
                : ""; // Build I: the wheel poses the challenge on its face (was worksheet section 10's "Your turn now, acolyte. Find the next …")
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
            if (next < 0) { Phase = LessonPhase.ModalityComplete; Message = "Twelve signs, three modalities, all accounted for. Cardinal signs are the initiators, they begin each season and set things in motion. Fixed signs are the anchors, they hold steady in the middle and do not waver. Mutable signs are the adapters, they close each season and prepare the way for what comes next.\nThe wheel has one more pattern to reveal, acolyte, and a table beside the Dial has begun to stir."; Dial.Log("modalities_completed"); return; } // owner (worksheet section 10)
            Phase = LessonPhase.ModalityOwn; modalityFamily = next;
            int start = FirstUnlitInModalityFamily(next, out _); LitMod[start] = true;
            StartModalityProblem(start, 0);
            Message = ModalityFamiliesComplete == 1 ? "Well done. Now the " + ModalityName(next).ToLowerInvariant() + " signs await you: " + ModalityMembers(next) + ".\nFrom " + SignName(start) + ", count three forward upon the wheel each time." : "The final modality, acolyte. " + ModalityName(next) + ": " + ModalityMembers(next) + ".\nBegin at " + SignName(start) + ". I shall let you lead."; // owner (worksheet section 10)
            Dial.Log("modality_family_started");
        }
        void AfterModalityDemonstration()
        {
            // A worked example lights the seat without evidence; three of them in one sitting pause the unit (reappearance cap).
            LitMod[Zodiac.Destination(lastStart, 3)] = true; modalityAssisted++;
            if (modalityAssisted >= 3) { Dial.Home(); Phase = LessonPhase.ModalityPaused; Message = "That is enough of the second pattern for now, acolyte. Rest, and we shall continue where you left off when you return. " + JournalNudge; return; } // owner (worksheet section 10); the journal sentence is Build F's placeholder
            AfterModalityCorrect(Dial.Events.Last());
        }
        public string RuleFor(int step) => step == 3
            ? "Find the next sign that shares the same modality after {0} upon the wheel.\nCount each sign forward: one, two, three." // owner (worksheet section 10)
            : step == 6 ? "The opposite sits six signs forward from {0} upon the wheel.\nCount each sign after it: one, two, three, four, five, six." // owner (worksheet section 12)
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
            Message = Hard ? "The book tests you again, acolyte, but do not expect the same ease. The symbols come in my order now, and the choices before you will try to deceive.\nName the sign each symbol belongs to, and trust what you have learned." : "Once more from the beginning, acolyte. The book presents the symbols again.\nName the sign each one belongs to."; // owner (worksheet section 9)
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
        public const string ShelfFirst = "You know the names, acolyte, but the symbols are another matter. There is a book upon the shelf that will teach you each one.\nRead it first, for when you return to the Dial the names shall vanish and only the symbols will remain."; // owner (worksheet section 7)
        public const string ShelfDark = "Dark and quiet, that shelf. It was not always so. I recall it waking once before, when the wheel burned bright enough to reach it."; // owner (worksheet section 7)
        public const string ShelfRead = "Twelve symbols, all accounted for. The book has gone quiet, acolyte, and it does not wake again without reason.\nWhen the time comes, you will know."; // owner (worksheet section 7)
        // Four names: the answer plus three others, in a stable order per seat so tests and the page agree.
        public int[] GlyphOptions(int seat) => OptionsFor(seat, Hard, Hard ? CleanRuns : 0);
        public const string GlyphIntro = "Every sign upon this wheel carries a symbol of its own, twelve in all, older than the names themselves.\nI shall show you each one, and you shall tell me which sign it belongs to."; // owner (worksheet section 4)
        public string GlyphNameResult { get; private set; } = ""; // the last Part A result line, kept while the wheel takes over
        public bool AnswerGlyphName(int seat)
        {
            if (Phase != LessonPhase.GlyphNames || GlyphIndex >= 12) return false;
            int target = SeatAt(GlyphIndex); bool correct = Zodiac.Wrap(seat) == target;
            if (correct)
            {
                bool eligible = GlyphMisses <= 1; // Level 0 or a Level 1 nudge
                GlyphNamed[target] = true; Message = GlyphNameResult = "Yes. " + SignName(target) + ". That is correct"; // owner (worksheet section 4), as written
                GlyphNamedEvent?.Invoke(target, true, eligible); Dial.Log("glyph_named", true, eligible, "DirectSeat");
                NextGlyphName(); ProgressCommitted?.Invoke(); return true;
            }
            GlyphMisses++;
            if (GlyphMisses == 1) { Message = GlyphNameResult = "Not that one. This symbol belongs to " + Zodiac.Article(Element(target)) + " " + Element(target) + " sign."; Dial.Log("hint_requested"); return false; }
            // Second miss: Level 2 reveals the name; no evidence; move on.
            GlyphNamed[target] = true; Message = GlyphNameResult = "This is the symbol of " + SignName(target) + ". Remember it.";
            GlyphNamedEvent?.Invoke(target, false, false); Dial.Log("hint_escalated"); Dial.Log("glyph_named", false, false, "DirectSeat");
            NextGlyphName(); ProgressCommitted?.Invoke(); return false;
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
            Message = "Now the wheel hides its names. Only the symbols remain.\nI shall name a sign, and you shall turn until its symbol is selected, then press Seal."; // owner (worksheet section 5)
            BeginGlyphProblem(SeatAt(GlyphIndex));
        }
        void BeginGlyphProblem(int seat)
        {
            Dial.Begin(Zodiac.Wrap(seat + 3 + (seat * 5) % 7), 0, seat); CountBeatPending = false; // start three to nine seats away, never on or beside the answer (owner playtest, Sept 14)
            Message = ""; // Build I: the wheel names the target on its face; Caspar only teaches and answers (was worksheet section 5's "Find the symbol of …")
        }
        void FinishGlyphs()
        {
            Dial.Home();
            if (Practice)
            {
                Practice = false; Phase = LessonPhase.Key2; bool clean = GlyphEvidence;
                if (clean) { CleanRuns++; Message = "Well done, acolyte. Every symbol found without my hand upon the wheel.\nEnjoy this victory, for the book has noticed your progress and it shall raise the bar when next you open it."; Dial.Log("symbol_practice_clean", true, true); } // owner (worksheet section 9)
                else Message = "The symbols are done, though I confess I did much of the work for you.\nThere is no shame in it, acolyte, but the book will not yield its reward until you can find them on your own. It shall wait for you."; // owner (worksheet section 9)
                PracticeFinished?.Invoke(clean);
                return;
            }
            if (GlyphEvidence) { Phase = LessonPhase.Key2; Key2Earned = true; Message = "Twelve signs, twelve symbols, all in their rightful order. You read the wheel true now, acolyte.\nLook, another Key rises from the Dial, and it is yours, for you have earned it."; Dial.Log("key2_earned", true, true); } // owner (worksheet section 5)
            else { Phase = LessonPhase.Paused; Message = "We have reached the end, acolyte, though I confess my hand was upon the wheel more than yours.\nRest now and study your journal, for the symbols will test you again when you return, and I would see you claim that Key on your own."; } // owner (worksheet section 5)
        }
        // v0.2 (Q05): Unit 1.1 continuation on the same Dial, and compressed review problems.
        public int FamiliesComplete => Enumerable.Range(0, 4).Count(f => Kin[f]);
        public bool WheelComplete => Lit.All(v => v);
        LessonPhase phaseBeforeReview;
        public string ReviewHeader { get; set; } = "Practice"; // Build F: the slice names the item ("Practice · 2 of 6") so a practice never looks like the lesson
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
                ? "Now the " + Element(family) + " signs. You have done this twice and the wheel has yielded to you each time.\nStart at " + SignName(family) + ", and I shall only watch." // owner (worksheet section 3)
                : "Now for the last of the four, the " + Element(family) + " signs. Begin at " + SignName(family) + ", acolyte.\nYou have proven yourself thrice upon this wheel, I need not guide you further. You have this."; // owner (worksheet section 3)
            Dial.Log("unit11_family_started");
            return true;
        }
        public bool BeginReview(int seat) { return BeginReview(seat, 4); }
        public bool BeginReview(int seat, int step)
        {
            if (Phase == LessonPhase.Review || IsProblem) return false;
            phaseBeforeReview = Phase; Phase = LessonPhase.Review;
            Dial.Begin(seat, 0, -1, step); CountBeatPending = false;
            Message = step == 3 ? "" : "Before you turn the wheel, know this: the signs are not scattered without purpose. Three signs share an element: Fire, three share Earth, three share Air, and three share Water. These are a sort of elemental family. I shall test whether you can trace them upon the wheel, starting now."; // owner (worksheet sections 2 and 10); the compressed form: start already framed, Seal
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
            Message = WheelComplete ? "The whole wheel burns." : "You return, acolyte. The wheel has held your place and it stirs at your presence."; // owner (worksheet section 3)
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
                default: return "Your sun sign is " + SignName(Sun) + ". " + SignName(Sun) + " is " + Zodiac.Article(Element(Sun)) + " " + Element(Sun) + " sign.\nIn your world, the sun sign is the one most people know. There is much more to a chart than that, but this is where we start.";
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
            else if (Phase == LessonPhase.Polarity)
            {
                if (PolarityStep == 0) { PolarityStep = 1; PolarityShown = true; Message = PolarityLine1; Dial.Log("polarity_shown"); ProgressCommitted?.Invoke(); return; }
                StartOppositeProblem(Sun, 2); // the first pair from the sun sign, guided with the six-count
            }
            else if (Phase == LessonPhase.OppositesComplete) StartBuilderSign();
        }
        void StartProblem(int start, int hint)
        {
            lastStart = Zodiac.Wrap(start);
            Dial.Begin(lastStart, hint);
            if (hint >= 2) exposedProblems.Add(lastStart);
            CountBeatPending = hint >= 2;
            string sign = SignName(lastStart);
            Message = hint >= 2 ? "Start at " + sign + ". Count each sign after it: one, two, three, four.\nInspect the framed sign, then press Seal." :
                ""; // Build I: the wheel poses the challenge on its face (was "Your turn. Find the next sign in this family from …")
        }
        public void CountBeatShown() { CountBeatPending = false; }
        public const string RuleLine = "Find the next sign that shares the same element after {0} upon the wheel.\nCount each sign after it: one, two, three, four."; // Level 2 (owner wording, Sept 17, worksheet section 8)
        public bool CanAsk => Dial.CanAsk && (Phase == LessonPhase.Independent || Phase == LessonPhase.Optional || Phase == LessonPhase.Continuation || Phase == LessonPhase.Review || Phase == LessonPhase.GlyphWheel || Phase == LessonPhase.ModalityOwn || Phase == LessonPhase.OppositeOwn || Phase == LessonPhase.BuilderOpposite);
        // Ask Caspar: the Level 2 reminder on request, wherever a rule exists. An answer after it earns no evidence.
        public bool AskCaspar()
        {
            if (!CanAsk || !Dial.Ask()) return false;
            if (Phase == LessonPhase.GlyphWheel) { NameRevealed[Dial.Target] = true; Message = "Look, the name has revealed itself upon the wheel. Turn to " + SignName(Dial.Target) + " and press Seal."; } // owner (worksheet sections 5 and 8)
            else { Message = string.Format(RuleFor(Dial.Forward), SignName(Dial.Start)) + "\nWhen the sign is selected, press Seal."; if (Phase != LessonPhase.Review) { CountBeatPending = true; exposedProblems.Add(Dial.Start); } } // owner (worksheet section 8)
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
                    Message = "Yes. " + SignName(target) + ", that is correct"; Dial.Log("glyph_placed", true, result.evidence_eligible); // owner (worksheet section 5), as written
                }
                else if (step == 1) Message = Hard ? "Not so. You are rushing, acolyte. Return your gaze to the symbol, trace its shape in your mind, and then seek it upon the wheel." : "Not that one, acolyte. Look at the symbol before you, find it upon the wheel, and count forward from where you stand."; // owner (worksheet sections 5 and 9); no Aries anchor
                else if (step == 2) { NameRevealed[target] = true; Message = "Look, the name has revealed itself upon the wheel. Turn to " + SignName(target) + " and press Seal."; } // owner (worksheet section 5)
                else Message = "Watch me find it. Then the next symbol."; // owner (worksheet section 5)
                return result;
            }
            if (Phase == LessonPhase.Review)
            {
                if (result.correctness) { Message = Dial.Forward == 3 ? "Yes, " + SignName(result.selected_destination) + " is " + ModalityName(result.selected_destination).ToLowerInvariant() + "." : "Yes. " + SignName(result.selected_destination) + " is " + Zodiac.Article(Element(result.selected_destination)) + " " + Element(result.selected_destination) + " sign."; Dial.Home(); ReviewFinished?.Invoke(Dial.Start, true, result.hint_level <= 1); }
                else if (Dial.Attempts >= 2) { Message = "It is " + SignName(Zodiac.Destination(Dial.Start, Dial.Forward)) + ", acolyte. The wheel shall test you on it again."; Dial.Home(); ReviewFinished?.Invoke(Dial.Start, false, false); } // owner (worksheet section 2)
                else { Message = "Not that one, acolyte. Look again and try once more."; } // owner (worksheet section 2)
                return result;
            }
            if (!result.correctness)
            {
                if (RecoveryEncounters == 0) RecoveryEncounters = 1;
                if (Dial.HintLevel >= 2) exposedProblems.Add(Dial.Start);
                if (step == 2) CountBeatPending = true;
                Message = step == 1 ? "Not that one. Count your steps again.\nYou can move on from where you are." :
                    step == 2 ? string.Format(RuleFor(Dial.Forward), SignName(Dial.Start)) + "\nWhen the sign is selected, press Seal." : // owner (worksheet section 8)
                    InOppositeProblem ? "Watch me find it, acolyte.\n" + (Phase == LessonPhase.BuilderOpposite ? "Then tell me what the two signs share." : "Then we move to the next pair.") : // owner (worksheet section 12)
                    "Watch me do one.\nThen you will try again from a new sign.";
            }
            return result;
        }
        public string CorrectLine(DialEvent result) =>
            (InOppositeProblem ? "Yes, " + SignName(result.selected_destination) + " sits across from " + SignName(Dial.Start) + ". Both are " + Kind(Dial.Start) + ", both are " + Side(Dial.Start) + ", only the element differs: " + Element(Dial.Start) + " against " + Element(result.selected_destination) + "." // owner (worksheet section 12)
                : InModalities ? "Yes, " + SignName(result.selected_destination) + " is " + ModalityName(result.selected_destination).ToLowerInvariant() + ", just as " + SignName(Dial.Start) + " is." // owner (worksheet section 10)
                : "Yes. " + SignName(result.selected_destination) + " is " + Zodiac.Article(Element(result.selected_destination)) + " " + Element(result.selected_destination) + " sign, like your sun sign.") + "\n" +
            (result.evidence_eligible ? (InOppositeProblem || InModalities ? "You found that one on your own, acolyte." : "You found that one on your own.") : "We found that one together.");
        public void AfterCorrect(DialEvent result)
        {
            AfterCorrectCore(result);
            ProgressCommitted?.Invoke();
        }
        void AfterCorrectCore(DialEvent result)
        {
            if (result == null || !result.correctness) return;
            if (Phase == LessonPhase.Review) return;
            if (Phase == LessonPhase.GlyphWheel) { NextGlyphProblem(); return; }
            if (Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn) { AfterModalityCorrect(result); return; }
            if (Phase == LessonPhase.OppositeGuided || Phase == LessonPhase.OppositeOwn) { AfterOppositeCorrect(result); return; }
            if (Phase == LessonPhase.BuilderOpposite) { if (result.hint_level >= 2) builderAssisted = true; StartBuilderShare(); return; }
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
            if (InOppositeProblem) return; // the wheel is already lit; opposites mark pairs, not seats
            if (Phase != LessonPhase.Optional) Lit[Zodiac.Destination(lastStart)] = true;
            // Worked exposure illuminates but never writes eligible answer evidence.
        }
        public void AfterDemonstration()
        {
            AfterDemonstrationCore();
            ProgressCommitted?.Invoke();
        }
        void AfterDemonstrationCore()
        {
            if (Phase == LessonPhase.GlyphWheel) { Dial.Home(); NextGlyphProblem(); return; }
            if (Phase == LessonPhase.ModalityGuided || Phase == LessonPhase.ModalityOwn) { AfterModalityDemonstration(); return; }
            if (Phase == LessonPhase.OppositeGuided || Phase == LessonPhase.OppositeOwn) { AfterOppositeDemonstration(); return; }
            if (Phase == LessonPhase.BuilderOpposite)
            {
                builderAssisted = true; oppAssisted++;
                if (oppAssisted >= 3) { Dial.Home(); Phase = LessonPhase.BuilderPaused; Message = OppositesPausedLine; Dial.Log("builder_paused"); return; }
                StartBuilderShare(); return;
            }
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
                if (next < 0) { Phase = LessonPhase.AllLit; Message = "The whole wheel burns, acolyte, Fire, Earth, Air, and Water, all twelve signs in their place. I confess the room feels different for it, alive in a way I had almost forgotten.\nCome, let us return to the Atrium and see what the Library has made of your work."; Dial.Log("wheel_completed"); } // owner (worksheet section 3)
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
            // Stage 0: next naming card. Stage 1: all names read, next placement. Stage 2: Key 2 earned.
            index = Math.Max(0, Math.Min(12, index));
            for (int i = 0; i < 12; i++)
            {
                order[i] = i;
                GlyphNamed[i] = key2 || stage >= 1 || i < index;
                GlyphPlaced[i] = key2 || stage >= 2 || (stage == 1 && i < index);
                NameRevealed[i] = false;
            }
            GlyphIndex = index; GlyphMisses = 0; Practice = false;
            Key2Earned = key2; GlyphEvidence = key2;
            if (key2) { Phase = LessonPhase.Key2; Message = "Twelve signs, twelve symbols. You read the wheel true, acolyte."; } // restored Key 2, in the owner's words
        }
        public void Say(string text) { Message = text; } // Slice beats speak through the same panel.
        public string SeatLabel(int seat)
        {
            var sign = Zodiac.Seats[seat];
            if (NamesHidden && !NameRevealed[seat]) return "Symbol " + sign.Glyph + ", position " + (seat + 1) + " of 12, " + (Dial.Selected == seat ? "selected" : "not selected");
            return sign.Name + (GlyphsShown ? ", symbol " + sign.Glyph : "") + (LitMod[seat] ? ", " + ModalityName(seat).ToLowerInvariant() : "") + (PolarityShown ? ", " + Zodiac.PolarityAt(seat) : "") + ", position " + (seat + 1) + " of 12, " +
                (Dial.Selected == seat ? "selected" : "not selected") +
                (Lit[seat] ? ", " + sign.Element + (Kin[seat] ? ", family complete" : ", lit") : ", dormant");
        }
    }
}
