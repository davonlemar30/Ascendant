using System;
using UnityEngine;

namespace Ascendant.CelestialDial
{
    // The living inscription (owner, Oct 7: approved as scoped, all drafts, on 86bcbn6w6, comment 90140263883109; the build is 86bceba0a).
    // The lines live in data, Resources/Journal/inscriptions.json, like Practice's questions: each has a stable id, a group, and the lesson
    // that unlocks it, so a rewrite is a data edit and the save's history of ids survives it. Groups: any, key (after the first Key), wing
    // (once the Zodiac Wing is whole), absence (after a few sittings away from the journal; never drawn otherwise), and sun (the player's own
    // sun, each line once its fact is learned). Unlock: "" (none), key1, key2, modalities, key3, key4 or wing (SliceFlow.Learned).
    [Serializable] public sealed class InscriptionLine
    {
        public string id = "", group = "", unlock = "", text = "";
        public bool draft = true;
    }
    [Serializable] public sealed class InscriptionBook
    {
        public string about = "";
        public InscriptionLine[] lines = new InscriptionLine[0];
        static InscriptionBook loaded;
        public static InscriptionBook Data
        {
            get
            {
                if (loaded != null) return loaded;
                var file = Resources.Load<TextAsset>("Journal/inscriptions"); loaded = file != null ? JsonUtility.FromJson<InscriptionBook>(file.text) : null;
                if (loaded == null) { Debug.LogWarning("[Journal] Resources/Journal/inscriptions.json is missing or empty"); loaded = new InscriptionBook(); }
                return loaded;
            }
        }
    }
}
