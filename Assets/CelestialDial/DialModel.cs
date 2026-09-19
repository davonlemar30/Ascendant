using System;
using System.Collections.Generic;

namespace Ascendant.CelestialDial
{
    public enum DialInput { Drag, ForwardStep, BackStep, DirectSeat, Keyboard, Accessible }
    public enum SeatState { Dormant, Passing, Framed, Rejected, Lit, KinComplete }

    public sealed class ZodiacSeat
    {
        public readonly string Name;
        public readonly string Element;
        public string Modality => Zodiac.ModalityOf(this);
        public readonly string Glyph; // Placeholder: the Unicode zodiac symbol. Not final glyph art.
        public ZodiacSeat(string name, string element, string glyph) { Name = name; Element = element; Glyph = glyph; }
    }

    public static class Zodiac
    {
        // One ordered source for geometry, labels, lesson content, and evaluation.
        public static readonly IReadOnlyList<ZodiacSeat> Seats = Array.AsReadOnly(new[] {
            new ZodiacSeat("Aries", "Fire", "\u2648"), new ZodiacSeat("Taurus", "Earth", "\u2649"),
            new ZodiacSeat("Gemini", "Air", "\u264A"), new ZodiacSeat("Cancer", "Water", "\u264B"),
            new ZodiacSeat("Leo", "Fire", "\u264C"), new ZodiacSeat("Virgo", "Earth", "\u264D"),
            new ZodiacSeat("Libra", "Air", "\u264E"), new ZodiacSeat("Scorpio", "Water", "\u264F"),
            new ZodiacSeat("Sagittarius", "Fire", "\u2650"), new ZodiacSeat("Capricorn", "Earth", "\u2651"),
            new ZodiacSeat("Aquarius", "Air", "\u2652"), new ZodiacSeat("Pisces", "Water", "\u2653") });
        public static int Wrap(int position) => (position % 12 + 12) % 12;
        public static readonly string[] Modalities = { "Cardinal", "Fixed", "Mutable" }; // every third sign shares a modality (Curriculum Rev 2, Stage 1)
        public static string ModalityOf(ZodiacSeat seat) { for (int i = 0; i < Seats.Count; i++) if (ReferenceEquals(Seats[i], seat)) return Modalities[i % 3]; return Modalities[0]; }
        public static string ModalityAt(int seat) => Modalities[Wrap(seat) % 3];
        // Build C: polarity is a property of the element. Fire and Air are day signs, Earth and Water night signs (Curriculum Rev 2, Stage 1:
        // "the game picks one label pair and notes the older terms"). The pair is the brief's recommendation, one constant, the owner's to swap.
        public static readonly string[] Polarities = { "Yang", "Yin" }; // owner, Sept 17 (worksheet section 12): Fire and Air are Yang, Earth and Water Yin
        public const string OlderPolarityTerms = "masculine and feminine, or yang and yin";
        public static string Article(string element) => element == "Earth" || element == "Air" ? "an" : "a"; // "an Earth sign", "a Fire sign": the bracket substitution keeps its article
        public static string PolarityAt(int seat) => Polarities[Wrap(seat) % 2];   // the elements alternate Fire, Earth, Air, Water, so even seats are Yang
        public static int Opposite(int seat) => Wrap(seat + 6);                     // six seats on: the sign straight across the wheel
        public const int OppositePairs = 6;
        public static int PairOf(int seat) => Wrap(seat) % OppositePairs;          // a pair is named by its lower seat
        public static int Destination(int start, int forward = 4) => Wrap(start + forward);
        public static bool Evaluate(int start, int offset, int destination) => Destination(start, offset) == destination;
        // Tropical sun-sign date ranges (common almanac boundaries; cusp days can vary by year). Returns -1 for an invalid date.
        static readonly int[] SignStartDay = { 20, 19, 21, 20, 21, 21, 23, 23, 23, 23, 22, 22 }; // Jan..Dec: day the next sign begins
        static readonly int[] SignStartSeat = { 10, 11, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };            // sign that begins in that month
        public static int SunSign(int month, int day)
        {
            if (month < 1 || month > 12 || day < 1 || day > 31) return -1;
            int m = month - 1;
            return day >= SignStartDay[m] ? SignStartSeat[m] : SignStartSeat[(m + 11) % 12];
        }
    }

    [Serializable]
    public sealed class DialEvent
    {
        public string event_name;
        public int problem_id;
        public int start_seat;
        public string start_sign;
        public string requested_relationship;
        public int selected_destination;
        public string destination_sign;
        public bool correctness;
        public string input_method;
        public int hint_level;
        public int attempt_number;
        public double response_time;
        public int movement_count;
        public bool evidence_eligible;
    }

    public sealed class DialModel
    {
        readonly Func<double> now;
        double startedAt;
        int problemId;
        readonly List<DialEvent> events = new List<DialEvent>();
        public IReadOnlyList<DialEvent> Events => events;
        public event Action<DialEvent> Logged;
        public int Start { get; private set; }
        public int Selected { get; private set; }
        public int HintLevel { get; private set; }
        public bool Asked { get; private set; }      // the player asked Caspar for the rule (Level 2 on request); the next miss goes to Level 3
        public int Attempts { get; private set; }
        public int MovementCount { get; private set; }
        public bool Counting { get; private set; }
        public bool Active { get; private set; }
        public bool Rejected { get; private set; }
        public bool FirstSuccessfulSeal { get; private set; }
        public bool ReducedMotion { get; set; }
        public bool CanInertia => !ReducedMotion && HintLevel < 2;
        public DialInput LastInput { get; private set; }
        public DialModel(Func<double> clock) { now = clock; }

        public int Target { get; private set; } = -1;   // -1: the default forward-offset-4 relationship
        public int Forward { get; private set; } = 4;   // seats forward per problem: 4 for the elemental families, 3 for the modalities
        public string Relationship => Target >= 0 ? "seat_of_sign" : "forward_offset_" + Forward;
        public void Begin(int start, int hintLevel) { Begin(start, hintLevel, -1, 4); }
        public void Begin(int start, int hintLevel, int targetSeat) { Begin(start, hintLevel, targetSeat, 4); }
        public void Begin(int start, int hintLevel, int targetSeat, int step)
        {
            Start = Zodiac.Wrap(start); Target = targetSeat < 0 ? -1 : Zodiac.Wrap(targetSeat); Forward = step;
            Selected = Start; // Automatic positioning does not emit a movement tick.
            HintLevel = hintLevel; Asked = false;
            Attempts = MovementCount = 0;
            Counting = Rejected = false;
            Active = true;
            startedAt = now();
            problemId++;
            Log("problem_started", false, false, "automatic");
        }

        public void Home()
        {
            Selected = 0;
            Active = Rejected = false;
            MovementCount = 0;
        }

        public void PositionSilently(int seat) { Selected = Zodiac.Wrap(seat); Rejected = false; }
        public void Step(int delta, DialInput method)
        {
            if (!Active || delta == 0) return;
            // Each crossed detent has exactly the same event/feedback path, including number four.
            int direction = Math.Sign(delta);
            for (int i = 0; i < Math.Abs(delta); i++)
            {
                Selected = Zodiac.Wrap(Selected + direction);
                MovementCount += direction;
                LastInput = method;
                Rejected = false;
                Log("dial_rotated");
            }
        }
        public void Select(int seat, DialInput method)
        {
            if (!Active) return;
            int distance = Zodiac.Wrap(seat - Selected);
            if (distance > 6) distance -= 12;
            Step(distance, method);
            LastInput = method;
            Frame();
        }
        public void Frame() { if (Active) Log("seat_framed"); }
        public void Count()
        {
            if (!Active) return;
            Log("hint_requested");
            Counting = true;
            HintLevel = Math.Max(1, HintLevel); // A counter never lowers rule-revealing assistance.
        }
        // Ask Caspar (v0.3 revision, build 2): after a first miss the player may request the Level 2 rule reminder.
        public bool CanAsk => Active && Attempts >= 1 && HintLevel < 2;
        public bool Ask()
        {
            if (!CanAsk) return false;
            Asked = true; Escalate(2); Log("hint_asked"); return true;
        }
        public void Escalate(int level)
        {
            HintLevel = Math.Max(HintLevel, level);
            Log("hint_escalated");
        }
        public DialEvent Commit()
        {
            if (!Active) return null;
            Attempts++;
            bool correct = Target >= 0 ? Selected == Target : Zodiac.Evaluate(Start, Forward, Selected);
            bool evidence = correct && HintLevel <= 1;
            Log("answer_committed", correct, evidence);
            var result = Log(correct ? "answer_correct" : "answer_rejected", correct, evidence);
            Rejected = !correct;
            if (correct) { FirstSuccessfulSeal = true; Active = false; }
            else if (Attempts == 1) Escalate(1);
            else if (Attempts == 2 && !Asked) Escalate(2);
            else { Escalate(3); Active = false; } // a miss after the asked reminder is the third step
            return result;
        }
        public DialEvent Log(string name, bool correct = false, bool evidence = false, string method = null)
        {
            var item = new DialEvent {
                event_name = name, problem_id = problemId, start_seat = Start,
                start_sign = Zodiac.Seats[Start].Name, requested_relationship = Relationship,
                selected_destination = Selected, destination_sign = Zodiac.Seats[Selected].Name,
                correctness = correct, input_method = method ?? LastInput.ToString(),
                hint_level = HintLevel, attempt_number = Attempts,
                response_time = Math.Max(0, now() - startedAt), movement_count = MovementCount,
                evidence_eligible = evidence
            };
            events.Add(item);
            Logged?.Invoke(item);
            return item;
        }
    }
}
