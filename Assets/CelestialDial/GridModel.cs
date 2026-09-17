using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum GridPhase { Closed, Open, Paused, Complete }

    // Build B (Unit 1.2, part 2): the table in the Wing, four element rows by three modality columns. Each sign has exactly one
    // cell (Curriculum Rev 2, Stage 1: "each element-modality combination appears exactly once"). Tap a sign, tap a cell, Seal.
    // The same ladder as the Dial (Level 1 nudge, Level 2 rule, Level 3 Caspar seats it), Ask Caspar after a first miss, the
    // Level 0/1 evidence rule for Key 3, and the reappearance cap (08 Mastery & Mistakes). Pure state, no Unity types.
    public sealed class GridModel
    {
        public const int Rows = 4, Columns = 3;
        public static int RowOf(int seat) => Zodiac.Wrap(seat) % Rows;          // Fire, Earth, Air, Water: the element repeats every fourth sign
        public static int ColumnOf(int seat) => Zodiac.Wrap(seat) % Columns;    // Cardinal, Fixed, Mutable: the modality repeats every third
        public static int CellOf(int seat) => RowOf(seat) * Columns + ColumnOf(seat);
        public static int SeatOf(int cell) => (9 * (cell / Columns) + 4 * (cell % Columns)) % 12; // the one sign whose element is the row and whose modality is the column
        public static string RowName(int row) => Zodiac.Seats[row].Element;     // seats 0..3 carry the four elements in row order
        public static string ColumnName(int column) => Zodiac.Modalities[column];

        readonly Func<double> now;
        double startedAt;
        int problemId;
        readonly List<DialEvent> events = new List<DialEvent>();
        public IReadOnlyList<DialEvent> Events => events;
        public event Action<DialEvent> Logged;
        public event Action ProgressCommitted;
        public readonly bool[] Placed = new bool[12];
        public readonly bool[] Assisted = new bool[12];     // seated at Level 2 or 3; never evidence
        public GridPhase Phase { get; private set; } = GridPhase.Closed;
        public bool Started { get; private set; }
        public bool Evidence { get; private set; }          // any Level 0/1 seating: the Key 3 rule, kept across sittings
        public bool Key3Earned { get; private set; }
        public int Sign { get; private set; } = -1;         // the sign in hand
        public int Cell { get; private set; } = -1;         // the cell under the hand
        public int Rejected { get; private set; } = -1;     // the last wrong cell, marked until the next tap
        public int Attempts { get; private set; }
        public int HintLevel { get; private set; }
        public bool Asked { get; private set; }
        public bool Demonstrating { get; private set; }     // Level 3: the view plays the beat, then AfterDemonstration seats the sign
        public int AssistedThisSitting { get; private set; }
        public string Message { get; private set; } = "";
        public GridModel(Func<double> clock) { now = clock; }

        public int PlacedCount => Placed.Count(v => v);
        public bool Complete => Placed.All(v => v);
        public bool Locked => Sign >= 0 && Attempts > 0;    // after a miss the sign in hand is seated before another is picked (a problem runs its ladder)
        public bool Active => Phase == GridPhase.Open && !Demonstrating;
        public bool CanPick(int seat) => Active && !Placed[Zodiac.Wrap(seat)] && (!Locked || Zodiac.Wrap(seat) == Sign);
        public bool CanChoose(int cell) => Active && Sign >= 0 && cell >= 0 && cell < 12 && !Placed[SeatOf(cell)];
        public bool CanSeal => Active && Sign >= 0 && Cell >= 0;
        public bool CanAsk => Active && Sign >= 0 && Attempts >= 1 && HintLevel < 2;
        public int DemonstrationCell => Sign >= 0 ? CellOf(Sign) : -1;

        // Placeholder lines (owner writes; worksheet section 11).
        public const string IntroLine = "Four elements, three kinds. Every sign has one seat at this table, and only one.\nTap a sign, then the cell where it belongs, then press Seal.";
        public const string ClearLine = "The table is clear again. This time, find their places yourself.\nTap a sign, then the cell where it belongs, then press Seal.";
        public const string FullLine = "The table is full. Every sign in its one place.";
        public const string DarkLine = "The table is bare. The wheel has more to show you first.";
        public const string PausedLine = "Let us stop the table here for now.\nWe will pick it up when you return.";
        public const string NoEvidenceLine = "Twelve seated, but I did most of the seating.\nRest. We will clear the table and try again when you return.";
        public const string Key3Line = "Twelve seated, every sign in its one place. You know the table now.\nKeeper Key 3 is yours.";
        static string SignName(int seat) => Zodiac.Seats[Zodiac.Wrap(seat)].Name;
        static string Element(int seat) => Zodiac.Seats[Zodiac.Wrap(seat)].Element;
        static string Kind(int seat) => Zodiac.ModalityAt(seat).ToLowerInvariant();
        static string Article(string element) => element == "Earth" || element == "Air" ? "an" : "a";
        // Level 2 (the brief's rule): "[Sign] is [element]; it is [modality]."
        public static string Rule(int seat) => SignName(seat) + " is " + Element(seat) + "; it is " + Kind(seat) + ".";
        // Level 1 names one of the two: the element when the row was wrong, otherwise the kind.
        public static string Nudge(int seat, int wrongCell) => wrongCell / Columns != RowOf(seat)
            ? "Not that cell. " + SignName(seat) + " is " + Article(Element(seat)) + " " + Element(seat) + " sign."
            : "Not that cell. " + SignName(seat) + " is " + Kind(seat) + ".";
        public string Readout => !Active ? "" : Sign < 0 ? "Tap a sign, then its cell, then Seal." : Cell < 0 ? "In hand: " + SignName(Sign)
            : SignName(Sign) + " to " + RowName(Cell / Columns) + ", " + ColumnName(Cell % Columns).ToLowerInvariant() + ". Press Seal.";

        // Opens the table, or comes back to it. Per-sitting counters reset; a full table without evidence is cleared for another try.
        public bool Begin()
        {
            if (Phase == GridPhase.Complete) { Message = FullLine; return true; }
            bool first = !Started, cleared = Complete && !Key3Earned;
            Started = true; AssistedThisSitting = 0; Demonstrating = false;
            Sign = Cell = Rejected = -1; Attempts = HintLevel = 0; Asked = false;
            if (cleared) { for (int i = 0; i < 12; i++) { Placed[i] = false; Assisted[i] = false; } Evidence = false; }
            Phase = GridPhase.Open;
            Message = first ? IntroLine : cleared ? ClearLine : "The table remembers. " + PlacedCount + " of twelve seated.\nTap a sign, then its cell, then press Seal."; // placeholder (owner writes)
            Log(first ? "grid_unit_started" : cleared ? "grid_cleared" : "grid_resumed", false, false, "automatic");
            return true;
        }
        public bool Pick(int seat)
        {
            seat = Zodiac.Wrap(seat);
            if (!CanPick(seat)) return false;
            if (seat != Sign)
            {
                Sign = seat; Cell = Rejected = -1; Attempts = HintLevel = 0; Asked = false;
                startedAt = now(); problemId++;
                Log("problem_started", false, false, "automatic");
            }
            Log("sign_picked");
            return true;
        }
        public bool Choose(int cell)
        {
            if (!CanChoose(cell)) return false;
            Cell = cell; Rejected = -1; Log("cell_chosen"); return true;
        }
        public DialEvent Seal()
        {
            if (!CanSeal) return null;
            Attempts++;
            bool correct = Cell == CellOf(Sign), evidence = correct && HintLevel <= 1;
            Log("answer_committed", correct, evidence);
            var result = Log(correct ? "answer_correct" : "answer_rejected", correct, evidence);
            if (correct) { Seat(Sign, evidence); ProgressCommitted?.Invoke(); return result; }
            int wrong = Cell; Rejected = wrong; Cell = -1;
            if (Attempts == 1) { HintLevel = 1; Message = Nudge(Sign, wrong); }
            else if (Attempts == 2 && !Asked) { HintLevel = 2; Message = Rule(Sign) + "\nFind that cell, then press Seal."; }
            else { HintLevel = 3; Demonstrating = true; Message = "Watch me seat it.\n" + Rule(Sign); } // a miss after the asked rule is the third step
            Log("hint_escalated");
            return result;
        }
        // Ask Caspar (as in the v0.3 revision, build 2): the Level 2 rule on request after a first miss; a seating after it earns no evidence.
        public bool Ask()
        {
            if (!CanAsk) return false;
            Asked = true; HintLevel = 2; Message = Rule(Sign) + "\nFind that cell, then press Seal."; Log("hint_asked"); return true;
        }
        // Level 3: Caspar seats the sign. No evidence; three in one sitting pause the table (the reappearance cap).
        public void AfterDemonstration()
        {
            AfterDemonstrationCore();
            ProgressCommitted?.Invoke();
        }
        void AfterDemonstrationCore()
        {
            if (!Demonstrating) return;
            Demonstrating = false; AssistedThisSitting++;
            int seat = Sign; Seat(seat, false);
            if (Phase != GridPhase.Open) return;
            if (AssistedThisSitting >= 3) { Phase = GridPhase.Paused; Message = PausedLine; Log("grid_paused"); }
            else Message = SignName(seat) + " is seated: " + Element(seat) + ", " + Kind(seat) + ".\nNow the next sign.";
        }
        void Seat(int seat, bool evidence)
        {
            Placed[seat] = true; Assisted[seat] = !evidence; if (evidence) Evidence = true;
            Cell = CellOf(seat); Rejected = -1; // the event names the cell the sign went to, whoever seated it
            Message = "Yes. " + SignName(seat) + ": " + Element(seat) + ", " + Kind(seat) + ".\n" + (evidence ? "You found its place on your own." : "We found it together.");
            Log("grid_placed", true, evidence);
            Sign = Cell = Rejected = -1; Attempts = HintLevel = 0; Asked = false;
            if (Complete) Finish();
        }
        void Finish()
        {
            if (Evidence) { Key3Earned = true; Phase = GridPhase.Complete; Message = Key3Line; Log("key3_earned", true, true); }
            else { Phase = GridPhase.Paused; Message = NoEvidenceLine; Log("grid_no_evidence"); }
        }
        // Second sitting: the seated signs and the evidence flag come back from the save; a sign in hand does not.
        public void Restore(bool[] placed, bool evidence, bool started, bool key3)
        {
            for (int i = 0; i < 12; i++) Placed[i] = placed != null && i < placed.Length && placed[i];
            Key3Earned = key3; Evidence = evidence || key3; Started = started || key3 || PlacedCount > 0;
            Phase = key3 ? GridPhase.Complete : GridPhase.Closed;
            Sign = Cell = Rejected = -1; Attempts = HintLevel = 0; Asked = false; Demonstrating = false;
            Message = key3 ? FullLine : "";
        }
        public string TileLabel(int seat) => SignName(seat) + ", symbol " + Zodiac.Seats[Zodiac.Wrap(seat)].Glyph + ", " + (Placed[Zodiac.Wrap(seat)] ? "seated" : Zodiac.Wrap(seat) == Sign ? "in hand" : "not seated");
        public string CellLabel(int cell)
        {
            int seat = SeatOf(cell);
            return RowName(cell / Columns) + ", " + ColumnName(cell % Columns).ToLowerInvariant() + ": " + (Placed[seat] ? SignName(seat) : "empty") +
                (cell == Cell ? ", selected" : "") + (cell == Rejected ? ", not that one" : "");
        }
        public DialEvent Log(string name, bool correct = false, bool evidence = false, string method = null)
        {
            int cell = Cell >= 0 ? Cell : Rejected >= 0 ? Rejected : Sign >= 0 ? CellOf(Sign) : 0;
            var item = new DialEvent {
                event_name = name, problem_id = problemId, start_seat = Math.Max(0, Sign),
                start_sign = Sign >= 0 ? SignName(Sign) : "", requested_relationship = "cell_of_sign",
                selected_destination = cell, destination_sign = SignName(SeatOf(cell)),
                correctness = correct, input_method = method ?? "DirectCell",
                hint_level = HintLevel, attempt_number = Attempts,
                response_time = Sign >= 0 ? Math.Max(0, now() - startedAt) : 0, movement_count = 0,
                evidence_eligible = evidence
            };
            events.Add(item);
            Logged?.Invoke(item);
            return item;
        }
    }
}
