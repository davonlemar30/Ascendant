using System.Collections.Generic;

namespace Ascendant.EraDemo
{
    // The demo's map and people. A greybox: the layout loosely follows Langston's draft slice (task 86bcg8az2: an oven lane, a square, a
    // courtyard, a roof), which the owner has not ruled on; every line is a labelled placeholder describing its beat, not game text.
    public static class EraDemoContent
    {
        public static readonly string[] Rows =
        {
            "############",
            "#..........#",
            "#....t.....#", // the roof: the teacher watches the east
            "#..........#",
            "#####==#####",
            "#n...==...x#", // the courtyard: a wall niche, the stairs up, the packed trunk
            "#..........#",
            "#....c.....#", // the copyist
            "#####..#####",
            "#..........#",
            "#.s......m.#", // the square: salt under cloth, a camel at rest
            "#..........#",
            "#....w.....#", // the well
            "#..........#",
            "####....####",
            "~~~#....#~~~",
            "~~~#..o.#~~~", // the lane: the bread oven
            "~~~#.b..#~~~", // the baker
            "~~~#....#~~~",
            "~~~#....#~~~",
            "~~~#....#~~~",
            "~~~#....#~~~",
            "~~~#....#~~~",
            "~~~#....#~~~",
            "~~~#....#~~~",
            "~~~#....#~~~",
            "~~~#....#~~~",
            "~~~D.K..#~~~", // the door the Keeper came through, and goes home by
            "~~~######~~~",
        };
        public const string Title = "Era demo (greybox)";
        public const string PlacesTitle = "P L A C E S";

        public static EraMap Build()
        {
            var map = new EraMap(Rows);
            var a = map.Arrival;
            map.Points.Add(new EraPoint("baker", "The baker", "THE BAKER", map.Find('b'), Lines(
                "[Placeholder] The baker's lines: the town believes the world ends with the meeting of the two stars.",
                "[Placeholder] She sends you across the square to the old teacher's house.")));
            map.Points.Add(new EraPoint("copyist", "The copyist", "THE COPYIST", map.Find('c'), Lines(
                "[Placeholder] The copyist's lines: his grandfather means to give the family's books to the river at first light.",
                "[Placeholder] He asks you to go up to the roof and talk to him.")));
            map.Points.Add(new EraPoint("teacher", "The teacher", "THE TEACHER", map.Find('t'), new List<EraLine>
            {
                new EraLine("[Placeholder] The teacher's lines: he watches the two planets rise together in the east."),
                new EraLine("[Placeholder] The Keeper answers him.", "[Placeholder] Choice A", "[Placeholder] Choice B"),
                new EraLine("[Placeholder] Either answer leads here: he decides to keep the books."),
            }));
            map.Points.Add(new EraPoint("caspar", "Caspar", "CASPAR", new Cell(a.X + 1, a.Y), Lines(
                "[Placeholder] Caspar's line: he can't stay long, because he is tethered to the Library."), follower: true));
            map.Points.Add(new EraPoint("portal", "The door home", "", map.Find('D'), null, portal: true));
            return map;
        }
        static List<EraLine> Lines(params string[] text) { var list = new List<EraLine>(); foreach (var t in text) list.Add(new EraLine(t)); return list; }
    }
}
