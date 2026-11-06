namespace Ascendant.CelestialDial
{
    public static class CorridorCopy
    {
        // New draft teaching lines alternate these two approved forms of address.
        public static string Speak(string text, ref string previous)
        {
            if(!text.Contains("{student}"))return text;
            string next=previous=="pupil"?"apprentice":"pupil";previous=next;return text.Replace("{student}",next);
        }
        public const string Title = "The Rulership Corridor"; // Codex draft (goal 03); owner rewrites
        public const string Door = "The brass has answered, seeker. The Rulership Corridor is open."; // Codex draft (goal 03); owner rewrites
        public const string First = "A long stretch of dust, {student}, and a wheel waiting for its planets. Tap the instrument when you are ready."; // Codex draft (goal 03); owner rewrites
        public const string TraditionalIntro = "Begin with the lights, {student}: the Moon in Cancer and the Sun in Leo. Then Mercury, Venus, Mars, Jupiter and Saturn each take two signs. Set their places on the brass."; // Codex draft (goal 03); owner rewrites
        public const string Place = "Place {planet} on a sign it rules."; // Codex draft (goal 03); owner rewrites
        public const string Nudge = "Look along the Cancer and Leo axis, {student}. The traditional scheme reaches outward in pairs."; // Codex draft (goal 03); owner rewrites
        public const string Rule = "The traditional places for {planet} are {signs}, {student}."; // Codex draft (goal 03); owner rewrites
        public const string Placed = "Here is the place, {student}. Let the brass hold it while you study the pair."; // Codex draft (goal 03); owner rewrites
        public const string Key7 = "The traditional wheel is set, seeker. Keeper Key 7 is yours. The Chamber has a lock for it."; // Codex draft (goal 03); owner rewrites
        public const string Paused7 = "My hand placed every planet this time, initiate. The Key will wait. Return to the wheel for another turn."; // Codex draft (goal 03); owner rewrites
        public const string ModernIntro = "Three more plaques, {student}. Uranus with Aquarius, Neptune with Pisces, Pluto with Scorpio: a modern convention that traditional practitioners do not all adopt. Set each beside its traditional ruler."; // Codex draft (goal 03); owner rewrites
        public const string ModernPlace = "Place {planet} beside the traditional ruler. Modern convention."; // Codex draft (goal 03); owner rewrites
        public const string ModernRule = "In this modern convention, {planet} rules {signs}, {student}."; // Codex draft (goal 03); owner rewrites
        public const string DetrimentIntro = "Turn the brass again, {student}. A planet's detriment is opposite a sign it rules. We shall use the seven traditional planets here."; // Codex draft (goal 03); owner rewrites
        public const string DetrimentAsk = "{planet} rules {sign}. Find the opposite sign, then Seal."; // Codex draft (goal 03); owner rewrites
        public const string DetrimentNudge = "Look across the centre, {student}. Six signs separate opposite places."; // Codex draft (goal 03); owner rewrites
        public const string DetrimentRule = "Find a ruled sign, {student}, then follow its line across the centre to the opposite sign."; // Codex draft (goal 03); owner rewrites
        public const string DetrimentShow = "The opposite place is {sign}, {student}. Turn there and Seal."; // Codex draft (goal 03); owner rewrites
        public const string CardsIntro = "One last task, {student}. Read a sign through its ruler's work, its element and its modality. Choose the line that keeps all three together."; // Codex draft (goal 03); owner rewrites
        public const string ExplainAsk = "Which line describes {sign} through its traditional ruler?"; // Codex draft (goal 03); owner rewrites
        public const string ExplainLine = "{planet}: {work}; {element}; {modality}."; // Codex draft (goal 03); owner rewrites
        public const string ExplainWhy = "{sign} has {planet} as its traditional ruler. Its element is {element} and its modality is {modality}."; // Codex draft (goal 03); owner rewrites
        public const string Key8 = "The old places and the modern plaques are set, initiate. Keeper Key 8 is yours. Carry it to the Chamber."; // Codex draft (goal 03); owner rewrites
        public const string Paused8 = "The brass is set, seeker, though my hand found each opposite. The Key will wait for a turn you find yourself."; // Codex draft (goal 03); owner rewrites
        public const string Hub7 = "Seven Keys, initiate. The Chamber has room for the one you carry."; // Codex draft (goal 03); owner rewrites
        public const string Spent7 = "Seven locks turned, seeker. The Corridor has three more plaques to show you."; // Codex draft (goal 03); owner rewrites
        public const string Hub8 = "Eight Keys, seeker. Bring the new one to the Books."; // Codex draft (goal 03); owner rewrites
        public const string End = "Eight locks turned, initiate. The Observatory's lower level is lit."; // Codex draft (goal 03); owner rewrites
        public const string Wheel = "The rulership wheel"; // Codex draft (goal 03); owner rewrites
        public const string BackToObservatory = "The Observatory"; // Codex draft (goal 03); owner rewrites
        public const string Back = "Back"; // Codex draft (goal 03); owner rewrites
        public const string Continue = "Continue"; // Codex draft (goal 03); owner rewrites
        public const string Seal = "Seal"; // Codex draft (goal 03); owner rewrites
        public const string Ask = "Ask Caspar"; // Codex draft (goal 03); owner rewrites
        public const string Journal = "Your journal"; // Codex draft (goal 03); owner rewrites
        public const string Ruler = "Ruler"; // Codex draft (goal 03); owner rewrites
        public const string ModernRuler = "Modern ruler"; // Codex draft (goal 03); owner rewrites
        public const string Modern = "Modern convention"; // Codex draft (goal 03); owner rewrites
        public const string Sealed = "The sealed door toward the Zodiac Wing"; // Codex draft (goal 03); owner rewrites
    }
}
