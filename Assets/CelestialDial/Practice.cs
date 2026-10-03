using System;
using UnityEngine;

namespace Ascendant.CelestialDial
{
    // The journal's Practice (owner, Oct 3: step 4 approved as scoped on 86bcbn6w6, the door's count removed). The questions live in data,
    // Resources/Practice/questions.json, so a concept is added by adding an entry: its name, its line on the list, the lesson that unlocks
    // it, the line that ends its round, and its questions (the ask, the choices, the right one, a short why). The words are Claude's drafts,
    // which the owner rewrites or approves before they ship.
    [Serializable] public sealed class PracticeQuestion
    {
        public string ask = "", why = "";
        public int glyph = -1;      // a sign's mark drawn above the ask (0 Aries ... 11 Pisces), or -1
        public string[] choices = new string[0];
        public int answer;          // the right choice's place in choices (the game shuffles them)
    }
    [Serializable] public sealed class PracticeConcept
    {
        public string id = "", name = "", line = "", unlock = "", end = ""; // unlock: key1, key2, modalities, key3 or key4 (SliceFlow.ConceptLearned)
        public PracticeQuestion[] questions = new PracticeQuestion[0];
    }
    [Serializable] public sealed class PracticeBook
    {
        public string right = "", wrong = ""; // said before the why: after a right pick, after a wrong one
        public PracticeConcept[] concepts = new PracticeConcept[0];
        static PracticeBook loaded;
        public static PracticeBook Data
        {
            get
            {
                if (loaded != null) return loaded;
                var file = Resources.Load<TextAsset>("Practice/questions"); loaded = file != null ? JsonUtility.FromJson<PracticeBook>(file.text) : null;
                if (loaded == null) { Debug.LogWarning("[Practice] Resources/Practice/questions.json is missing or empty"); loaded = new PracticeBook(); }
                return loaded;
            }
        }
    }
}
