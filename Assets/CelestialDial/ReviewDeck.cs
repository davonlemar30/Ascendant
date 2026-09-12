using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum ItemState { Introduced, Practicing }

    // One atomic item per sign-element pair (Curriculum canon, Stage 0: all twelve enter the deck as Introduced).
    [Serializable]
    public enum ItemKind { Element, Glyph } // v0.3 adds twelve glyph items (sign ↔ glyph), Curriculum canon Stage 1.

    public sealed class ReviewItem
    {
        public int seat;
        public int kind;       // ItemKind
        public ItemKind Kind => (ItemKind)kind;
        public int state;      // ItemState
        public int streak;     // running evidence score (Mastery & Mistakes: +1 per eligible success, -1 per miss, floor 0)
        public int interval;   // index into ReviewDeck.Ladder
        public int dueDay;
        public bool entered;   // has entered the deck
        public ItemState State => (ItemState)state;
    }

    // Locked Mastery & Mistakes scheduler, v0.x: 1 → 3 → 7 → 14 → 30 days. Never called a test in copy.
    public sealed class ReviewDeck
    {
        public static readonly int[] Ladder = { 1, 3, 7, 14, 30 };
        public const int BatchSize = 6; // "two minutes on entering the Library"; tuning variable
        public readonly ReviewItem[] Items = Enumerable.Range(0, 24).Select(i => new ReviewItem { seat = i % 12, kind = i / 12 }).ToArray();
        public ReviewItem Item(int seat, ItemKind kind) => Items[(int)kind * 12 + Zodiac.Wrap(seat)];
        public event Action<string> Logged;

        public void IntroduceAll(int day) { IntroduceAll(day, ItemKind.Element); }
        public void IntroduceAll(int day, ItemKind kind)
        {
            foreach (var item in Items) if (item.Kind == kind && !item.entered) { item.entered = true; item.interval = 0; item.dueDay = day + Ladder[0]; }
            Logged?.Invoke(kind == ItemKind.Element ? "deck_introduced" : "deck_glyphs_introduced");
        }
        // A correct Level 0/1 answer in a lesson makes the item Practicing and starts its streak.
        public void RecordLesson(int seat, bool eligible, int day) { RecordLesson(seat, eligible, day, ItemKind.Element); }
        public void RecordLesson(int seat, bool eligible, int day, ItemKind kind)
        {
            var item = Item(seat, kind);
            if (!item.entered) { item.entered = true; item.interval = 0; item.dueDay = day + Ladder[0]; }
            if (!eligible) return;
            if (item.State == ItemState.Introduced) { item.state = (int)ItemState.Practicing; item.streak = 1; item.dueDay = day + Ladder[item.interval]; }
        }
        public List<ReviewItem> Due(int day) => Items.Where(i => i.entered && i.dueDay <= day).OrderBy(i => i.dueDay).ThenBy(i => i.kind).ThenBy(i => i.seat).ToList();
        public void RecordReview(int seat, bool correct, bool eligible, int day) { RecordReview(seat, correct, eligible, day, ItemKind.Element); }
        public void RecordReview(int seat, bool correct, bool eligible, int day, ItemKind kind)
        {
            var item = Item(seat, kind);
            if (correct && eligible)
            {
                if (item.State == ItemState.Introduced) item.state = (int)ItemState.Practicing;
                item.streak++;
                item.interval = Math.Min(item.interval + 1, Ladder.Length - 1);
            }
            else if (!correct)
            {
                item.streak = Math.Max(0, item.streak - 1);
                item.interval = Math.Max(0, item.interval - 1); // at the first interval a miss stays at one day
            }
            // Assisted (ineligible) correct answers neither advance nor set back; they schedule a fresh look at the same interval.
            item.dueDay = day + Ladder[item.interval];
            Logged?.Invoke(correct ? "review_correct" : "review_missed");
        }
        public int Practicing => Items.Count(i => i.State == ItemState.Practicing);
    }

    // Local session save (Q05 decision 5): no account, no server. Wiped by Start over.
    [Serializable]
    public sealed class SaveData
    {
        public int version = 2;
        public string playerName = "";
        public int sunSign = -1;
        public bool[] lit = new bool[12];
        public bool[] kin = new bool[12];
        public bool keyEarned;
        public bool wheelComplete;
        public int atriumStage;
        public int dayOffset;
        public int firstDay;
        public ReviewItem[] deck;
        public int keys;
        public int glyphStage;      // 0 not started, 1 Part A done, 2 Part B done (Key 2)
        public int glyphIndex;      // next glyph in zodiac order within the current part
        public int reviewsChecked;
    }
}
