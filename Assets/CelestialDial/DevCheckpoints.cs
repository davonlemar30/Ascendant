using System;
using System.Linq;

namespace Ascendant.CelestialDial
{
    // Build W (owner, Sept 26 playtest note 5; task 86bca0163): DEV Mode's "Jump to…". One scripted run of the Zodiac Wing, played in
    // memory through the same model calls and progress subscriptions the slice makes, every answer at Level 0, stopped at a checkpoint
    // and captured as a real save. Keys are earned, not spent (owner, Sept 29); Key 1 is inserted by the opening itself, and at the
    // Wing whole all four are spent by definition.
    public static class DevCheckpoints
    {
        public static readonly (string id, string label)[] All =
        {
            ("key1", "After Key 1"), ("wheel", "The whole wheel lit"), ("key2", "After Key 2"),
            ("key3", "After Key 3"), ("key4", "After Key 4"), ("whole", "The Zodiac Wing whole"),
        };
        public static bool Known(string id) => All.Any(c => c.id == id);

        public static SaveData Play(string id, string playerName, int sunSign)
        {
            if (!Known(id)) throw new ArgumentException("unknown checkpoint " + id);
            int sun = sunSign >= 0 && sunSign < 12 ? sunSign : 1;
            var flow = new SliceFlow(); var lesson = new DialLesson(() => 0); var grid = new GridModel(() => 0);
            flow.ObserveProgress(lesson, grid, () => { });
            SaveData Stop() => flow.CaptureProgress(lesson, grid);

            // The opening: name, sign, the Atrium's pages, the walk to the Dial (Build T), the first lesson, Key 1 into the Chamber.
            flow.SetName(string.IsNullOrEmpty(playerName) ? "Tester" : playerName);
            Must(flow.Continue(), "the name"); flow.ChooseBirth("chart");
            // DEV Mode's sample chart (owner, Oct 2 evening): a real chart worked out for a sample birth with the player's sun sign (BirthChart.Sample)
            BirthChart.Sample(sun, out int year, out int month, out int day, out int minute, out var place);
            Must(flow.SetBirthDate(year, month, day) && flow.SetBirthTime(minute) && flow.SetBirthPlace(place) && flow.SunSign == sun, "the sample chart keeps the sun sign");
            Must(flow.Continue() && flow.Continue() && flow.AtHub && flow.AtriumStage == 1, "the Atrium's pages end in the Atrium at Stage 1");
            Must(flow.EnterWing() && flow.EnterDial(), "the walk to the Dial");
            lesson.SetSunSign(sun);
            for (int guard = 0; lesson.Phase == LessonPhase.Encounter && guard < 20; guard++) lesson.Continue();
            lesson.Continue();
            Answer(lesson); Answer(lesson); lesson.Continue(); Answer(lesson); Answer(lesson);
            Must(lesson.KeyEarned && flow.RevealKey(), "the first lesson earns Key 1");
            Must(flow.Continue() && flow.Continue() && flow.InsertKey() && flow.End() && flow.Continue() && flow.AtHub && flow.AtriumStage == 2, "Key 1 into the Chamber; the Atrium at Stage 2");
            if (id == "key1") return Stop();

            // The whole wheel: the element families' continuation.
            Must(flow.EnterWing() && flow.EnterDial(), "back to the Dial");
            Must(lesson.BeginContinuation(), "the continuation begins");
            for (int guard = 0; !lesson.WheelComplete && guard < 12; guard++) Answer(lesson);
            Must(lesson.WheelComplete, "the whole wheel lit"); flow.MarkWheelComplete();
            Must(flow.LeaveDial() && flow.LeaveWing() && flow.AtriumStage == 3, "back to the Atrium at Stage 3");
            if (id == "wheel") return Stop();

            // Key 2: the Book of Symbols (Part A), then the symbols on the wheel (Part B).
            Must(flow.EnterWing() && flow.EnterBook() && lesson.BeginGlyphs(), "the Book opens"); flow.StartGlyphs();
            for (int i = 0; i < 12; i++) lesson.AnswerGlyphName(lesson.CurrentGlyph);
            Must(lesson.AllNamed && flow.LeaveBook() && flow.EnterDial() && lesson.BeginGlyphs(), "Part B on the wheel");
            for (int guard = 0; !lesson.Key2Earned && guard < 24; guard++) { lesson.Dial.Select(lesson.Dial.Target, DialInput.DirectSeat); var placed = lesson.Seal(); lesson.AfterCorrect(placed); }
            Must(lesson.Key2Earned, "Key 2 earned"); flow.MarkKey2();
            Must(flow.LeaveDial() && flow.LeaveWing(), "back to the Atrium with Key 2");
            if (id == "key2") return Stop();

            // Key 3: the modalities on the wheel, then the Elemental Table.
            Must(flow.EnterWing() && flow.EnterDial() && lesson.BeginModalities(), "the second pattern begins"); flow.StartModalities();
            for (int guard = 0; !lesson.ModalitiesComplete && guard < 24; guard++) { lesson.Dial.Select(Zodiac.Destination(lesson.Dial.Start, 3), DialInput.DirectSeat); var answer = lesson.Seal(); lesson.AfterCorrect(answer); }
            Must(lesson.ModalitiesComplete, "the modalities complete"); flow.MarkModalitiesComplete();
            Must(flow.LeaveDial() && flow.EnterGrid() && grid.Begin(), "the Table opens"); flow.StartGrid();
            for (int i = 0; i < 12; i++) { grid.Pick(i); grid.Choose(GridModel.CellOf(i)); grid.Seal(); }
            Must(grid.Key3Earned, "Key 3 earned"); flow.MarkKey3(); lesson.SetKey3(true);
            Must(flow.LeaveGrid() && flow.LeaveWing(), "back to the Atrium with Key 3");
            if (id == "key3") return Stop();

            // Key 4: polarity, the six opposite pairs, the builder.
            Must(flow.EnterWing() && flow.EnterDial() && lesson.BeginOpposites(), "the last pattern begins"); flow.StartOpposites();
            lesson.Continue(); lesson.Continue();
            for (int i = 0; i < Zodiac.OppositePairs; i++) { lesson.Dial.Select(Zodiac.Opposite(lesson.Dial.Start), DialInput.DirectSeat); var pair = lesson.Seal(); lesson.AfterCorrect(pair); }
            Must(lesson.OppositesComplete, "six pairs found");
            lesson.Continue();
            for (int i = 0; i < 3; i++)
            {
                lesson.AnswerBuilderName(lesson.BuilderTarget);
                lesson.Dial.Select(Zodiac.Opposite(lesson.BuilderTarget), DialInput.DirectSeat); var built = lesson.Seal(); lesson.AfterCorrect(built);
                lesson.AnswerBuilderShare(0); lesson.AnswerBuilderShare(1); lesson.NextBuilderSign();
            }
            Must(lesson.Key4Earned, "Key 4 earned"); flow.MarkKey4();
            Must(flow.LeaveDial() && flow.LeaveWing() && flow.KeysInHand == 3, "back to the Atrium, three Keys in hand");
            if (id == "key4") return Stop();

            // The Zodiac Wing whole: the three Keys in hand spent in the Chamber.
            Must(flow.EnterChamber() && flow.SpendKey() && flow.SpendKey() && flow.SpendKey() && flow.WingWhole && flow.LeaveChamber() && flow.AtriumStage == 6, "three Keys spent: the Zodiac Wing whole");
            return Stop();
        }
        static void Answer(DialLesson lesson)
        {
            lesson.Dial.Select(Zodiac.Destination(lesson.Dial.Start), DialInput.DirectSeat);
            var result = lesson.Seal(); lesson.AfterCorrect(result);
        }
        static void Must(bool ok, string step) { if (!ok) throw new InvalidOperationException("DEV checkpoint run failed at: " + step); }
    }
}
