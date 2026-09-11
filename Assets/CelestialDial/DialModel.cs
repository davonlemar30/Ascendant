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
        public ZodiacSeat(string name, string element) { Name = name; Element = element; }
    }

    public static class Zodiac
    {
        // One ordered source for geometry, labels, lesson content, and evaluation.
        public static readonly IReadOnlyList<ZodiacSeat> Seats = Array.AsReadOnly(new[] {
            new ZodiacSeat("Aries", "Fire"), new ZodiacSeat("Taurus", "Earth"),
            new ZodiacSeat("Gemini", "Air"), new ZodiacSeat("Cancer", "Water"),
            new ZodiacSeat("Leo", "Fire"), new ZodiacSeat("Virgo", "Earth"),
            new ZodiacSeat("Libra", "Air"), new ZodiacSeat("Scorpio", "Water"),
            new ZodiacSeat("Sagittarius", "Fire"), new ZodiacSeat("Capricorn", "Earth"),
            new ZodiacSeat("Aquarius", "Air"), new ZodiacSeat("Pisces", "Water") });
        public static int Wrap(int position) => (position % 12 + 12) % 12;
        public static int Destination(int start, int forward = 4) => Wrap(start + forward);
        public static bool Evaluate(int start, int offset, int destination) => Destination(start, offset) == destination;
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

        public void Begin(int start, int hintLevel)
        {
            Start = Zodiac.Wrap(start);
            Selected = Start; // Automatic positioning does not emit a movement tick.
            HintLevel = hintLevel;
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
        public void Escalate(int level)
        {
            HintLevel = Math.Max(HintLevel, level);
            Log("hint_escalated");
        }
        public DialEvent Commit()
        {
            if (!Active) return null;
            Attempts++;
            bool correct = Zodiac.Evaluate(Start, 4, Selected);
            bool evidence = correct && HintLevel <= 1;
            Log("answer_committed", correct, evidence);
            var result = Log(correct ? "answer_correct" : "answer_rejected", correct, evidence);
            Rejected = !correct;
            if (correct) { FirstSuccessfulSeal = true; Active = false; }
            else if (Attempts == 1) Escalate(1);
            else if (Attempts == 2) Escalate(2);
            else { Escalate(3); Active = false; }
            return result;
        }
        public DialEvent Log(string name, bool correct = false, bool evidence = false, string method = null)
        {
            var item = new DialEvent {
                event_name = name, problem_id = problemId, start_seat = Start,
                start_sign = Zodiac.Seats[Start].Name, requested_relationship = "forward_offset_4",
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
