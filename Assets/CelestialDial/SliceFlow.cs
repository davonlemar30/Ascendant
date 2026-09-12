using System;

namespace Ascendant.CelestialDial
{
    public enum SliceScreen { Identity, Birth, Atrium, Wing, AtriumReturn, Chamber }

    // The locked v0.1 flow (Q06): five Continue-linked screens around the Dial. Pure state, no Unity types.
    public sealed class SliceFlow
    {
        public SliceScreen Screen { get; private set; } = SliceScreen.Identity;
        public string PlayerName { get; private set; } = "";
        public string BirthChoice { get; private set; } = "";
        public string Note { get; private set; } = "";
        // Sept 11 decision: the birth prompt determines the sun sign for the session. No chart.
        public int SunSign { get; private set; } = -1;
        public bool HasSunSign => SunSign >= 0;
        public bool KeyRevealed { get; private set; }
        public bool KeyInserted { get; private set; }
        public bool Ended { get; private set; }
        public int LocksFilled { get; private set; }
        public const int LocksPerBook = 3;
        public const int Books = 7;
        public event Action<string> Logged;
        readonly Func<int> random;
        public SliceFlow() : this(null) { }
        public SliceFlow(Func<int> randomSeat) { random = randomSeat ?? (() => new Random().Next(12)); }
        public string DisplayName => string.IsNullOrEmpty(PlayerName) ? "Keeper" : PlayerName;
        public void SetName(string name) { PlayerName = (name ?? "").Trim(); }
        public void ChooseBirth(string choice)
        {
            if (Screen != SliceScreen.Birth) return;
            BirthChoice = choice; SunSign = -1; Note = "";
            if (choice == "unknown") { SunSign = Zodiac.Wrap(random()); Note = "Then I will choose one for you.\nYour sun sign is " + Zodiac.Seats[SunSign].Name + "."; }
            Logged?.Invoke("birth_choice:" + choice);
        }
        // Choice 1: a birth date. Only the sun sign is derived; time and place are not used in v0.1.
        public bool SetBirthDate(int month, int day)
        {
            if (Screen != SliceScreen.Birth || BirthChoice != "chart") return false;
            int seat = Zodiac.SunSign(month, day);
            if (seat < 0) return false;
            SunSign = seat; Note = "Your sun sign is " + Zodiac.Seats[seat].Name + "."; Logged?.Invoke("sun_sign_derived"); return true;
        }
        // Choice 2: the player already knows their sign.
        public bool SetKnownSign(int seat)
        {
            if (Screen != SliceScreen.Birth || BirthChoice != "known" || seat < 0 || seat > 11) return false;
            SunSign = seat; Note = "Your sun sign is " + Zodiac.Seats[seat].Name + "."; Logged?.Invoke("sun_sign_entered"); return true;
        }
        public bool CanContinue =>
            Screen == SliceScreen.Identity || (Screen == SliceScreen.Birth && HasSunSign) ||
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
