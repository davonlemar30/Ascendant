using System.Collections.Generic;

namespace Ascendant.EraDemo
{
    // The demo's map and people. A greybox: the layout loosely follows Langston's draft slice (task 86bcg8az2: an oven lane, a square, a
    // courtyard, a roof). The era is Timbuktu, spring 1583 (owner, Oct 9: "That era is fine" for the demo). The lines are Langston's draft
    // dialogue (his slice, section 5), drafts the owner rewrites: his optional "nobody remembers her face" hint and the Founder hint are left
    // out (they commit lore), names stay roles (his item 14 is open), and Caspar's tether line follows the owner's Oct 9 ruling ("he makes it
    // clear he cant stay long, as he is tethered to the library"). All of it is draft lore for the owner to keep or strike (Langston on 751ae8d), not working choices. Dante's two fixes are in (no negation pivots); Caspar says only what the
    // owner ruled ("he cant stay long"), and teaches the twenty-year meeting the scene leans on (Langston's framing, without the era door the portals ruling replaced).
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
            "~~~D.K..#~~~", // the way the Keeper came in, and goes back by
            "~~~######~~~",
        };
        public const string Title = "Era demo (greybox)";
        public const string PlacesTitle = "P L A C E S";

        public static EraMap Build()
        {
            var map = new EraMap(Rows);
            var a = map.Arrival;
            map.Points.Add(new EraPoint("baker", "The baker", "THE BAKER", map.Find('b'), Lines(
                "Up early, stranger, or up late? The bread doesn't care which.",
                "Nobody's slept. They say the two great stars rise together before the sun, in the last of the signs. The end of the water, then the end of everything.",
                "If you want trouble, go to the old teacher's house. His grandson was out here an hour ago, asking if anyone would talk sense to him.")));
            map.Points.Add(new EraPoint("copyist", "The copyist", "THE COPYIST", map.Find('c'), Lines(
                "Did the baker send you? You don't look like a scholar. Good. He's stopped listening to scholars.",
                "He's copied books his whole life, and his father did before him. Now he says, what's the use, if the world ends with the meeting? At first light he'll give them to the river.",
                "Star tables. Prayer times. A hundred years of the sky in our family's hands. He's on the roof, waiting to watch them rise. Please.")));
            map.Points.Add(new EraPoint("teacher", "The teacher", "THE TEACHER", map.Find('t'), new List<EraLine>
            {
                new EraLine("My grandson found a stranger to send up. Sit, then. Watch the east with me."),
                new EraLine("There. The two lords, rising together in the last sign. The old books say great changes ride on meetings like this one."),
                new EraLine("Nine years to the thousandth year, and a meeting at the end of the signs. What use is keeping pages, if the water is coming for all of it?",
                    "They meet every twenty years. You'd know that better than me.", "If it's the end, why are you up here watching?"),
                new EraLine("A meeting is a mark in the margin. The book goes on. My grandson knew it. It took a stranger to say it to me."),
                new EraLine("We won't give them to the river. We'll keep them in the walls, where the water can't reach."),
            }));
            map.Points.Add(new EraPoint("caspar", "Caspar", "CASPAR", new Cell(a.X + 1, a.Y), Lines(
                "Timbuktu, in the spring of 1583. Every twenty years or so Jupiter and Saturn meet, and the Library keeps its history by those meetings.",
                "Listen more than you speak, acolyte. I can't stay long. The Library holds me."), follower: true));
            map.Points.Add(new EraPoint("portal", "The way back", "", map.Find('D'), null, portal: true)); // neutral: no ruling names the Library his home (Langston, #150)
            return map;
        }
        static List<EraLine> Lines(params string[] text) { var list = new List<EraLine>(); foreach (var t in text) list.Add(new EraLine(t)); return list; }
    }
}
