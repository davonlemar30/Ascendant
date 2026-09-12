using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum LessonPhase { Encounter, Rule, Guided, Transfer, Independent, Complete, Paused, Optional }

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
        int family = 1;
        int lastStart;
        // Caspar speaks every line: patient, observant, learned, restrained, plainly worded.
        // Placeholder copy for the greybox; no lore, no dialogue system.
        public string Message { get; private set; } = "Welcome to the wheel. We will practice with Taurus.\nIt stands in for your own sign today.";
        public bool IsProblem => Phase == LessonPhase.Guided || Phase == LessonPhase.Independent || Phase == LessonPhase.Optional;
        public DialLesson(Func<double> clock) { Dial = new DialModel(clock); }
        public void Continue()
        {
            if (Phase == LessonPhase.Encounter)
            {
                Phase = LessonPhase.Rule; Lit[1] = true; Dial.PositionSilently(1);
                Message = "Every sign belongs to one of four families:\nFire, Earth, Air, or Water. Taurus is Earth.";
            }
            else if (Phase == LessonPhase.Rule)
            {
                Phase = LessonPhase.Guided;
                StartProblem(1, 2);
            }
            else if (Phase == LessonPhase.Transfer)
            {
                family = 0; Phase = LessonPhase.Independent; Lit[0] = true;
                StartProblem(0, 0);
            }
        }
        void StartProblem(int start, int hint)
        {
            lastStart = Zodiac.Wrap(start);
            Dial.Begin(lastStart, hint);
            if (hint >= 2) exposedProblems.Add(lastStart);
            string sign = Zodiac.Seats[lastStart].Name;
            Message = hint >= 2 ? "Start at " + sign + ". Count each sign after it: one, two, three, four.\nInspect the framed sign, then press Seal." :
                "Your turn. Find the next sign in this family from " + sign + ".\nMove the wheel, inspect the framed sign, then press Seal.";
        }
        public DialEvent Seal()
        {
            var result = Dial.Commit();
            if (result == null) return null;
            if (!result.correctness)
            {
                if (RecoveryEncounters == 0) RecoveryEncounters = 1;
                if (Dial.HintLevel >= 2) exposedProblems.Add(Dial.Start);
                Message = Dial.Attempts == 1 ? "Not that one. Count your steps again.\nYou can move on from where you are." :
                    Dial.Attempts == 2 ? "Start at " + Zodiac.Seats[Dial.Start].Name + ". Count each sign after it: one, two, three, four.\nInspect the framed sign, then press Seal." :
                    "Watch me do one.\nThen you will try again from a new sign.";
            }
            return result;
        }
        public void AfterCorrect(DialEvent result)
        {
            if (result == null || !result.correctness) return;
            if (Phase == LessonPhase.Optional)
            {
                Dial.Home(); Phase = LessonPhase.Complete;
                Message = "Well done. That one was for its own sake.\nYour six lit seats stay as they are.";
                return;
            }
            Lit[result.selected_destination] = true;
            if (Phase == LessonPhase.Independent && result.evidence_eligible) IndependentEvidence = true;
            if (recovering) { Dial.Log("problem_recovered", true, result.evidence_eligible); recovering = false; }
            AdvanceOrRecover(result.hint_level >= 2 && Phase == LessonPhase.Independent);
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
            if (Phase == LessonPhase.Guided)
            {
                Dial.Home(); Phase = LessonPhase.Transfer;
                Message = "The three Earth signs are joined. You see how it goes.\nNow the Fire family. I will say less this time.";
            }
            else if (IndependentEvidence)
            {
                Dial.Home(); Phase = LessonPhase.Complete; KeyEarned = true;
                Message = "Two of four. The rest will wait for you."; // Locked First Curriculum Unit line.
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
                Message = "Let us stop here for now. No Key yet.\nWe will come back to this another day.";
                return;
            }
            RecoveryEncounters++;
            int start = fresh[0]; recoveryStarts.Add(start); recovering = true;
            StartProblem(start, 0);
        }
        public void BeginOptional()
        {
            if (Phase != LessonPhase.Complete || !KeyEarned) return;
            Dial.Log("optional_problem_accepted");
            Phase = LessonPhase.Optional;
            // A separate one-problem third-family probe; never changes the six-seat lesson record.
            StartProblem(2, 0);
            Message = "One more, if you like. Start from Gemini, in the Air family.\nNo reward for this one. Just the wheel.";
        }
        public void Say(string text) { Message = text; } // Slice beats speak through the same panel.
        public string SeatLabel(int seat)
        {
            var sign = Zodiac.Seats[seat];
            return sign.Name + ", position " + (seat + 1) + " of 12, " +
                (Dial.Selected == seat ? "framed" : "not framed") +
                (Lit[seat] ? ", " + sign.Element + (Kin[seat] ? ", family complete" : ", lit") : ", dormant");
        }
    }
}
