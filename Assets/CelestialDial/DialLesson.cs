using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum LessonPhase { Encounter, Rule, Guided, Transfer, Independent, Complete, Paused, Optional, Continuation, AllLit, Review }

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
        public bool IsProblem => Phase == LessonPhase.Guided || Phase == LessonPhase.Independent || Phase == LessonPhase.Optional || Phase == LessonPhase.Continuation || Phase == LessonPhase.Review;
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
        public bool BeginReview(int seat)
        {
            if (Phase == LessonPhase.Review || IsProblem) return false;
            phaseBeforeReview = Phase; Phase = LessonPhase.Review;
            Dial.Begin(seat, 0); CountBeatPending = false;
            Message = "Find the next sign in this family."; // Compressed form: start already framed, one line, Seal.
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
        public DialEvent Seal()
        {
            var result = Dial.Commit();
            if (result == null) return null;
            if (Phase == LessonPhase.Review)
            {
                if (result.correctness) { Message = "Yes. " + SignName(result.selected_destination) + " is " + Element(result.selected_destination) + "."; Dial.Home(); ReviewFinished?.Invoke(Dial.Start, true, result.hint_level <= 1); }
                else if (Dial.Attempts >= 2) { Message = "It is " + SignName(Zodiac.Destination(Dial.Start)) + ". We will come back to it."; Dial.Home(); ReviewFinished?.Invoke(Dial.Start, false, false); }
                else { Message = "Not that one. Try once more."; }
                return result;
            }
            if (!result.correctness)
            {
                if (RecoveryEncounters == 0) RecoveryEncounters = 1;
                if (Dial.HintLevel >= 2) exposedProblems.Add(Dial.Start);
                if (Dial.Attempts == 2) CountBeatPending = true;
                Message = Dial.Attempts == 1 ? "Not that one. Count your steps again.\nYou can move on from where you are." :
                    Dial.Attempts == 2 ? "Start at " + SignName(Dial.Start) + ". Count each sign after it: one, two, three, four.\nInspect the framed sign, then press Seal." :
                    "Watch me do one.\nThen you will try again from a new sign.";
            }
            return result;
        }
        public string CorrectLine(DialEvent result) =>
            "Yes. " + SignName(result.selected_destination) + " is a " + Element(result.selected_destination) + " sign, like your sun sign.\n" +
            (result.evidence_eligible ? "You found that one on your own." : "We found that one together.");
        public void AfterCorrect(DialEvent result)
        {
            if (result == null || !result.correctness) return;
            if (Phase == LessonPhase.Review) return;
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
        public void RevealDemonstration()
        {
            if (Phase != LessonPhase.Optional) Lit[Zodiac.Destination(lastStart)] = true;
            // Worked exposure illuminates but never writes eligible answer evidence.
        }
        public void AfterDemonstration()
        {
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
        public void Say(string text) { Message = text; } // Slice beats speak through the same panel.
        public string SeatLabel(int seat)
        {
            var sign = Zodiac.Seats[seat];
            return sign.Name + ", position " + (seat + 1) + " of 12, " +
                (Dial.Selected == seat ? "selected" : "not selected") +
                (Lit[seat] ? ", " + sign.Element + (Kin[seat] ? ", family complete" : ", lit") : ", dormant");
        }
    }
}
