using System.Collections.Generic;
using UnityEngine;

namespace Ascendant.CelestialDial
{
    // Build E: sound hooks. Every hook names a slot; a slot with no file is silent. One AudioSource for cues, one for the
    // ambient loop; a mute toggle for tests that has nothing to do with reduced motion.
    public sealed class Sound : MonoBehaviour
    {
        public static bool Muted { get; private set; }
        public static string LastCue { get; private set; } = "";  // test evidence: the last hook that fired, file or not
        public static int Played { get; private set; }             // test evidence: cues that had a file
        public static bool AmbientPlaying => instance != null && instance.ambient != null && instance.ambient.isPlaying;
        static Sound instance;
        static readonly Dictionary<string, int> lastFrame = new Dictionary<string, int>();
        AudioSource cues, ambient;
        public static Sound Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("Sound"); instance = go.AddComponent<Sound>(); return instance;
        }
        void Awake()
        {
            if (FindFirstObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>(); // the generated scenes carry a camera only
            cues = gameObject.AddComponent<AudioSource>(); cues.playOnAwake = false;
            ambient = gameObject.AddComponent<AudioSource>(); ambient.playOnAwake = false; ambient.loop = true; ambient.volume = .5f;
            AudioListener.volume = Muted ? 0 : 1;
            var loop = Slots.Clip("ambient");
            if (loop != null) { ambient.clip = loop; ambient.Play(); }
        }
        // Plays a slot once per frame at most: a direct seat tap crosses several detents in one frame and is one step to the ear.
        public static void Play(string slot)
        {
            if (string.IsNullOrEmpty(slot)) return;
            if (lastFrame.TryGetValue(slot, out int frame) && frame == Time.frameCount) return;
            lastFrame[slot] = Time.frameCount; LastCue = slot;
            var clip = Slots.Clip(slot); if (clip == null || instance == null) return;
            instance.cues.PlayOneShot(clip); Played++;
        }
        public static void ToggleMute() { Muted = !Muted; AudioListener.volume = Muted ? 0 : 1; }
        // The model's events name the cues: a detent is a step, a correct answer a seal, a wrong one a miss, a Key a key.
        // tapPhase: the book's names and the builder's taps, where a first miss logs only hint_requested (the Count button never shows there).
        public static string Cue(string eventName, bool correct, bool tapPhase)
        {
            switch (eventName)
            {
                case "dial_rotated": case "sign_picked": case "cell_chosen": return "step";
                case "answer_correct": case "builder_share_marked": return "seal";
                case "answer_rejected": return "miss";
                case "glyph_named": case "builder_named": return correct ? "seal" : "miss";
                case "builder_shared": return correct ? null : "miss"; // the marked share already sealed
                case "hint_requested": return tapPhase ? "miss" : null;
                case "key2_earned": case "key3_earned": case "key4_earned": return "key";   // Key 1 sounds when it rises from the Dial
                default: return null;
            }
        }
    }
}
