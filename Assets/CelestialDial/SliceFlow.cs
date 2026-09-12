using System;

namespace Ascendant.CelestialDial
{
    public enum SliceScreen { Identity, Birth, Atrium, Wing, AtriumReturn, Chamber }

    // The locked v0.1 flow (Q06): five Continue-linked screens around the Dial. Pure state, no Unity types.
    public sealed class SliceFlow
    {
        public const string TeachingSign = "Taurus";
        public SliceScreen Screen { get; private set; } = SliceScreen.Identity;
        public string PlayerName { get; private set; } = "";
        public string BirthChoice { get; private set; } = "";
        public string Note { get; private set; } = "";
        public bool KeyRevealed { get; private set; }
        public bool KeyInserted { get; private set; }
        public bool Ended { get; private set; }
        public int LocksFilled { get; private set; }
        public const int LocksPerBook = 3;
        public const int Books = 7;
        public event Action<string> Logged;
        public string DisplayName => string.IsNullOrEmpty(PlayerName) ? "Keeper" : PlayerName;
        public void SetName(string name) { PlayerName = (name ?? "").Trim(); }
        public void ChooseBirth(string choice)
        {
            if (Screen != SliceScreen.Birth) return;
            BirthChoice = choice;
            // v0.1 never calculates a chart. Every path leads to the labeled teaching sign.
            Note = choice == "unknown" ? "" : "Chart entry is not part of this build. We will use a teaching sign for now.";
            Logged?.Invoke("birth_choice:" + choice);
        }
        public bool CanContinue =>
            Screen == SliceScreen.Identity || (Screen == SliceScreen.Birth && BirthChoice != "") ||
            Screen == SliceScreen.Atrium || (Screen == SliceScreen.Wing && KeyRevealed) || Screen == SliceScreen.AtriumReturn;
        public bool Continue()
        {
            if (!CanContinue) return false;
            Screen = Screen == SliceScreen.Identity ? SliceScreen.Birth :
                Screen == SliceScreen.Birth ? SliceScreen.Atrium :
                Screen == SliceScreen.Atrium ? SliceScreen.Wing :
                Screen == SliceScreen.Wing ? SliceScreen.AtriumReturn : SliceScreen.Chamber;
            Logged?.Invoke("screen_entered:" + Screen);
            return true;
        }
        public bool RevealKey()
        {
            if (Screen != SliceScreen.Wing || KeyRevealed) return false;
            KeyRevealed = true; Logged?.Invoke("key_revealed"); return true;
        }
        public bool InsertKey()
        {
            if (Screen != SliceScreen.Chamber || !KeyRevealed || KeyInserted) return false;
            KeyInserted = true; LocksFilled = 1; Logged?.Invoke("key_inserted"); return true;
        }
        public bool End()
        {
            if (!KeyInserted || Ended) return false;
            Ended = true; Logged?.Invoke("prototype_ended"); return true;
        }
    }
}
