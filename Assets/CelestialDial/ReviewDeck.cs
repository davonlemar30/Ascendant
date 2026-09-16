using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public enum ItemState { Introduced, Practicing }

    // One atomic item per sign-element pair (Curriculum canon, Stage 0: all twelve enter the deck as Introduced).
    public enum ItemKind { Element, Glyph, Modality, Grid } // v0.3 adds twelve glyph items; Build A twelve sign ↔ modality items; Build B twelve sign → cell items (Curriculum canon Stage 1).

    [Serializable] // JsonUtility skips a nested class without this: the deck had not been saving since v0.3 (owner playtest, Sept 14).
    public sealed class ReviewItem
    {
        public int seat;
        public int kind;       // ItemKind
        public ItemKind Kind => (ItemKind)kind;
        public int state;      // ItemState
        public int streak;     // running evidence score (Mastery & Mistakes: +1 per eligible success, -1 per miss, floor 0)
        public int interval;   // index into ReviewDeck.Ladder
        public int dueDay;     // the sitting (completed Check the Seals count) at which the item is ready again
        public bool entered;   // has entered the deck
        public ItemState State => (ItemState)state;
    }

    // Mastery & Mistakes scheduler with the Q05 ladder 1 → 3 → 7 → 14 → 30, counted in sittings (completed Check the Seals
    // batches) rather than days: the owner removed in-game time on September 13 (Q05 amendment). Never called a test in copy.
    public sealed class ReviewDeck
    {
        public static readonly int[] Ladder = { 1, 3, 7, 14, 30 };
        public const int BatchSize = 6; // "two minutes on entering the Library"; tuning variable
        public const int Kinds = 4;
        public readonly ReviewItem[] Items = Enumerable.Range(0, 12 * Kinds).Select(i => new ReviewItem { seat = i % 12, kind = i / 12 }).ToArray();
        public ReviewItem Item(int seat, ItemKind kind) => Items[(int)kind * 12 + Zodiac.Wrap(seat)];
        // Deck as data (owner, Sept 15): the grid items are scheduled like the rest but have no review form until the
        // lesson/review fork on the instrument ships; Check the Seals never asks them.
        public static bool Reviewable(ItemKind kind) => kind != ItemKind.Grid;
        public event Action<string> Logged;

        public void IntroduceAll(int day) { IntroduceAll(day, ItemKind.Element); }
        public void IntroduceAll(int day, ItemKind kind)
        {
            foreach (var item in Items) if (item.Kind == kind && !item.entered) { item.entered = true; item.interval = 0; item.dueDay = day; } // ready at the next check
            Logged?.Invoke(kind == ItemKind.Element ? "deck_introduced" : kind == ItemKind.Glyph ? "deck_glyphs_introduced" : kind == ItemKind.Modality ? "deck_modalities_introduced" : "deck_grid_introduced");
        }
        // A correct Level 0/1 answer in a lesson makes the item Practicing and starts its streak.
        public void RecordLesson(int seat, bool eligible, int day) { RecordLesson(seat, eligible, day, ItemKind.Element); }
        public void RecordLesson(int seat, bool eligible, int day, ItemKind kind)
        {
            var item = Item(seat, kind);
            if (!item.entered) { item.entered = true; item.interval = 0; item.dueDay = day; }
            if (!eligible) return;
            if (item.State == ItemState.Introduced) { item.state = (int)ItemState.Practicing; item.streak = 1; item.dueDay = day + Ladder[item.interval]; }
        }
        public List<ReviewItem> Due(int day) => Items.Where(i => i.entered && i.dueDay <= day).OrderBy(i => i.dueDay).ThenBy(i => i.kind).ThenBy(i => i.seat).ToList();
        public List<ReviewItem> DueForReview(int day) => Due(day).Where(i => Reviewable(i.Kind)).ToList();
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
                item.interval = Math.Max(0, item.interval - 1); // at the first interval a miss stays at one sitting
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
        public int version = 3;
        public string playerName = "";
        public int sunSign = -1;
        public bool[] lit = new bool[12];
        public bool[] kin = new bool[12];
        public bool keyEarned;
        public bool wheelComplete;
        public int atriumStage;
        public ReviewItem[] deck;
        public int keys;
        public int glyphStage;      // 0 not started, 1 Part A done, 2 Part B done (Key 2)
        public int glyphIndex;      // next glyph in zodiac order within the current part
        public int reviewsChecked;
        public int cleanRuns;       // v0.3 revision, build 3
        public bool[] litMod = new bool[12]; // Build A: modality unit
        public bool[] kinMod = new bool[3];
        public bool modalitiesStarted;
        public bool[] gridPlaced = new bool[12]; // Build B: the table; keys carries Key 3
        public bool gridEvidence;
        public bool gridStarted;
    }
}
