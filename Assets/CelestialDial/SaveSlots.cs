using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendant.CelestialDial
{
    // Three save slots (owner, Oct 9, 86bcg62x3, round 3: working choices 3 to 6). Slot 1 keeps today's key (SliceView.SaveKey), so a
    // save from before the menu is slot 1 as it stands: nothing migrates. Slots 2 and 3 take sibling keys. One more key remembers the
    // slot played last, as a slot number (no dates anywhere: no in-game time, Sept 13). A slot "can load" by the test a reload uses
    // today: SliceFlow.Restore. The store is PlayerPrefs; the mechanical checks swap in a dictionary.
    public static class SaveSlots
    {
        public const int Count = 3;
        public const string LastKey = SliceView.SaveKey + ".last"; // the slot played last, 1 to 3
        public static string Key(int slot) => slot <= 1 ? SliceView.SaveKey : SliceView.SaveKey + "." + slot;
        public sealed class Store { public Func<string, string> Get; public Action<string, string> Set; public Action<string> Delete; public Func<string, bool> Has; public Action Flush; }
        public static readonly Store Prefs = new Store { Get = k => PlayerPrefs.GetString(k, ""), Set = (k, v) => PlayerPrefs.SetString(k, v), Delete = PlayerPrefs.DeleteKey, Has = PlayerPrefs.HasKey, Flush = PlayerPrefs.Save };
        public static Store Current = Prefs;
        static int Clamp(int slot) => Mathf.Clamp(slot, 1, Count);
        // the slot in play: the one played last, slot 1 if none has been (Start over and DEV Jump to act on it)
        public static int InPlay
        {
            get { return int.TryParse(Current.Get(LastKey), out int n) ? Clamp(n) : 1; }
            set { Current.Set(LastKey, Clamp(value).ToString()); Current.Flush(); }
        }
        public static bool HasLast => Current.Has(LastKey);
        public static SaveData Read(int slot)
        {
            string json = Current.Get(Key(Clamp(slot))); if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<SaveData>(json); } catch (Exception e) { Debug.LogWarning("[CelestialDial] save unreadable: " + e.Message); return null; }
        }
        public static void Write(int slot, string json) { Current.Set(Key(Clamp(slot)), json); Current.Flush(); }
        public static void Clear(int slot) { Current.Delete(Key(Clamp(slot))); Current.Flush(); }
        static SliceFlow Restored(int slot) { var save = Read(slot); var flow = new SliceFlow(); return save != null && flow.Restore(save) ? flow : null; }
        public static bool CanLoad(int slot) => Restored(slot) != null;
        public static bool AnySave => Enumerable.Range(1, Count).Any(CanLoad);
        // Continue: the slot played last, or, if it holds nothing to load, the first slot that does; 0 when none does
        public static int ContinueSlot { get { int last = InPlay; if (CanLoad(last)) return last; for (int s = 1; s <= Count; s++) if (CanLoad(s)) return s; return 0; } }
        // A slot's card (owner's pick: the name, the Keeper Keys and the Big Three; an empty slot reads "Empty"; no date or time)
        public sealed class Card { public int Slot; public bool Filled; public string Name = "", Keys = "", BigThree = ""; public string Spoken => "Slot " + Slot + ": " + (Filled ? string.Join(", ", new[] { Name, Keys, BigThree }.Where(t => t != "")) : EmptyWords); }
        public const string EmptyWords = "Empty";
        public static Card Describe(int slot)
        {
            var flow = Restored(slot); var card = new Card { Slot = Clamp(slot), Filled = flow != null };
            if (flow == null) { card.Name = EmptyWords; return card; }
            card.Name = flow.DisplayName; card.Keys = "Keeper Keys: " + flow.Keys; card.BigThree = BigThree(flow.SunSign, flow.MoonSign, flow.RisingSign);
            return card;
        }
        // the Big Three as plain words (the web font draws no astrological glyphs): only the signs the save holds, in order
        public static string BigThree(int sun, int moon, int rising)
        {
            var parts = new List<string>();
            if (sun >= 0 && sun < 12) parts.Add(Zodiac.Seats[sun].Name + " sun");
            if (moon >= 0 && moon < 12) parts.Add(Zodiac.Seats[moon].Name + " moon");
            if (rising >= 0 && rising < 12) parts.Add(Zodiac.Seats[rising].Name + " rising");
            return string.Join(" · ", parts);
        }
    }
}
