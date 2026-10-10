using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.EraDemo
{
    // The walkable-era demo (task 86bcg8az2; owner, Oct 9): a top-down map walked square by square, tap-to-move, the camera following.
    // Pure state, no Unity types, like Walker. The map is a placeholder greybox; its people, lines and layout are not lore.
    public enum Facing { Down, Up, Left, Right }
    public enum CellKind { Void, Floor, Stairs, Wall, Prop }

    public struct Cell : IEquatable<Cell>
    {
        public readonly int X, Y; // x right, y down from the map's top row (north)
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell o) => X == o.X && Y == o.Y;
        public override bool Equals(object o) => o is Cell c && Equals(c);
        public override int GetHashCode() => X * 397 ^ Y;
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
        public override string ToString() => X + "," + Y;
        public int Distance(Cell o) => Math.Abs(X - o.X) + Math.Abs(Y - o.Y);
    }

    // Someone or something on the map the Keeper can walk up to: a person, the portal home, or Caspar (who follows).
    public sealed class EraPoint
    {
        public readonly string Id, Label, Speaker; public Cell At; public readonly bool Portal, Follower;
        public readonly List<EraLine> Lines;
        public EraPoint(string id, string label, string speaker, Cell at, List<EraLine> lines, bool portal = false, bool follower = false)
        { Id = id; Label = label; Speaker = speaker; At = at; Lines = lines ?? new List<EraLine>(); Portal = portal; Follower = follower; }
    }
    // One line in the chat box; a line with choices waits for one, and every choice leads on to the next line (no branching, Oct 9).
    public sealed class EraLine
    {
        public readonly string Text; public readonly string[] Choices;
        public EraLine(string text, params string[] choices) { Text = text; Choices = choices ?? new string[0]; }
    }

    public sealed class EraMap
    {
        // 40 layout px to a square: nine squares fill the 360 column, twenty fill its height.
        public const float SquarePx = 40;
        public readonly int Width, Height; readonly CellKind[,] kinds; readonly char[,] marks;
        public readonly List<EraPoint> Points = new List<EraPoint>();
        public Cell Arrival { get; private set; }

        // Rows top (north) to bottom. '#' wall, '.' floor, '=' stairs, '~' nothing (outside the map), 'o' 'w' 's' 'm' 'x' 'n' props
        // (oven, well, salt, camel, trunk, niche), 'K' the Keeper's arrival, letters in Point ids mark people and the portal ('D').
        public EraMap(string[] rows)
        {
            Height = rows.Length; Width = rows.Max(r => r.Length);
            kinds = new CellKind[Width, Height]; marks = new char[Width, Height];
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
            {
                char c = x < rows[y].Length ? rows[y][x] : '~'; marks[x, y] = c;
                kinds[x, y] = c == '~' ? CellKind.Void : c == '#' ? CellKind.Wall : c == '=' ? CellKind.Stairs : c == '.' || c == 'K' ? CellKind.Floor : "owsmxn".IndexOf(c) >= 0 ? CellKind.Prop : CellKind.Wall; // a person's or the portal's square is not walked on
                if (c == 'K') Arrival = new Cell(x, y);
            }
        }
        public Cell Find(char mark) { for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) if (marks[x, y] == mark) return new Cell(x, y); throw new ArgumentException("no " + mark + " on the map"); }
        public char Mark(Cell c) => In(c) ? marks[c.X, c.Y] : '~';
        public bool In(Cell c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;
        public CellKind Kind(Cell c) => In(c) ? kinds[c.X, c.Y] : CellKind.Void;
        public EraPoint PointAt(Cell c) => Points.FirstOrDefault(p => p.At == c);
        public EraPoint Point(string id) => Points.FirstOrDefault(p => p.Id == id);
        // Walkable: floor or stairs, and nobody standing there (Caspar, who follows, can be walked through: he steps aside).
        public bool Open(Cell c) { var k = Kind(c); if (k != CellKind.Floor && k != CellKind.Stairs) return false; var p = PointAt(c); return p == null || p.Follower; }

        static readonly Cell[] Steps = { new Cell(0, 1), new Cell(0, -1), new Cell(-1, 0), new Cell(1, 0) };
        // Breadth-first, four directions: the shortest route in squares from one square to the nearest of the goals, or null.
        public List<Cell> Route(Cell from, ICollection<Cell> goals)
        {
            if (goals.Count == 0) return null; if (goals.Contains(from)) return new List<Cell>();
            var before = new Dictionary<Cell, Cell> { [from] = from }; var queue = new Queue<Cell>(); queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var s in Steps)
                {
                    var n = new Cell(c.X + s.X, c.Y + s.Y);
                    if (before.ContainsKey(n) || !Open(n)) continue;
                    before[n] = c;
                    if (goals.Contains(n)) { var path = new List<Cell>(); for (var k = n; k != from; k = before[k]) path.Add(k); path.Reverse(); return path; }
                    queue.Enqueue(n);
                }
            }
            return null;
        }
        // The open squares beside a square (where the Keeper stands to talk to someone, or to step through the portal).
        public List<Cell> Beside(Cell c) => Steps.Select(s => new Cell(c.X + s.X, c.Y + s.Y)).Where(Open).ToList();
    }

    public sealed class EraWalker
    {
        public readonly EraMap Map;
        public Cell At { get; private set; }              // the square he stands on, or last left
        public float X { get; private set; }               // his position in squares (centre of a square is a whole number)
        public float Y { get; private set; }
        public Facing Facing { get; private set; } = Facing.Up;
        public bool Walking => route.Count > 0;
        public string TargetId { get; private set; } = ""; // the point he is walking to, or "" for a bare square
        public float Speed { get; private set; } = 170;    // layout px a second, the shipped walk speed (Walker.NormalSpeed)
        public float Traveled { get; private set; }        // squares walked on the current leg, for the step frames
        public EraPoint Follower { get; private set; }     // Caspar on the first trip (owner, Oct 9): one square behind the Keeper
        public event Action<string> Logged;
        public event Action<string> Arrived;               // a point's id, or "" for a bare square
        readonly List<Cell> route = new List<Cell>();
        public IReadOnlyList<Cell> Route => route;

        public EraWalker(EraMap map, Cell start, Facing facing = Facing.Up)
        {
            Map = map; At = start; X = start.X; Y = start.Y; Facing = facing;
        }
        public void SetSpeed(float pxPerSecond) { Speed = Math.Max(1, pxPerSecond); }
        public void PlaceFollower(EraPoint p) { Follower = p; }

        // A tap on a square: a person or the portal walks him beside it; a floor square walks him there; anything else does nothing.
        public bool Tap(Cell c)
        {
            var p = Map.PointAt(c);
            if (p != null) return GoTo(p.Id);
            if (!Map.Open(c)) return false;
            return Begin(Map.Route(Current, new[] { c }), "", c);
        }
        public bool GoTo(string id)
        {
            var p = Map.Point(id); if (p == null) return false;
            return Begin(Map.Route(Current, Map.Beside(p.At)), id, p.At);
        }
        // Where a new route starts: the square he is stepping into, so a new tap mid-step finishes the step first.
        Cell Current => route.Count > 0 ? route[0] : At;
        bool Begin(List<Cell> path, string id, Cell face)
        {
            if (path == null) { Logged?.Invoke("walk_blocked:" + (id == "" ? face.ToString() : id)); return false; }
            bool mid = route.Count > 0; var keep = mid ? route[0] : At;
            route.Clear(); if (mid) route.Add(keep);
            route.AddRange(path.Where(c => !(mid && c == keep)));
            TargetId = id; faceAtEnd = face; Traveled = 0;
            Logged?.Invoke("walk_started:" + (id == "" ? face.ToString() : id));
            if (route.Count == 0) Arrive(); else FaceToward(route[0]);
            return true;
        }
        Cell faceAtEnd;
        void FaceToward(Cell c)
        {
            float dx = c.X - X, dy = c.Y - Y;
            if (Math.Abs(dx) < .01f && Math.Abs(dy) < .01f) return;
            Facing = Math.Abs(dx) >= Math.Abs(dy) ? (dx > 0 ? Facing.Right : Facing.Left) : (dy > 0 ? Facing.Down : Facing.Up);
        }
        // Advances the walk by dt seconds; returns true on the tick he arrives.
        public bool Tick(float dt)
        {
            if (route.Count == 0) return false;
            float step = Speed / EraMap.SquarePx * Math.Max(0, dt);
            while (step > 0 && route.Count > 0)
            {
                var next = route[0]; float dx = next.X - X, dy = next.Y - Y, left = Math.Abs(dx) + Math.Abs(dy);
                FaceToward(next);
                if (left <= step) { X = next.X; Y = next.Y; step -= left; Traveled += left; StepInto(next); }
                else { X += Math.Sign(dx) * step; Y += Math.Sign(dy) * step; Traveled += step; step = 0; }
            }
            if (route.Count == 0) { Arrive(); return true; }
            return false;
        }
        // Reduced motion: no walk, he is simply there.
        public bool Jump()
        {
            if (route.Count == 0) return false;
            while (route.Count > 0) { var next = route[0]; Traveled += Math.Abs(next.X - X) + Math.Abs(next.Y - Y); X = next.X; Y = next.Y; StepInto(next); }
            Arrive(); return true;
        }
        void StepInto(Cell c) { route.RemoveAt(0); if (Follower != null && c != At) Follower.At = At; At = c; } // he steps where the Keeper just was
        void Arrive()
        {
            if (TargetId != "" && faceAtEnd != At) FaceToward(faceAtEnd);
            string id = TargetId; Logged?.Invoke("walk_arrived:" + (id == "" ? At.ToString() : id)); Arrived?.Invoke(id);
        }
        // One step every half square: the step frames' rhythm. 0 standing.
        public int StepFrame => Walking ? 1 + (int)(Traveled * 2) % 2 : 0;
    }

    // The chat box's conversation with one point: line by line, a choice waiting where a line has them.
    public sealed class EraTalk
    {
        public EraPoint With { get; private set; }
        public int Line { get; private set; } = -1;
        public string Chosen { get; private set; } = "";
        public bool Open => With != null;
        public EraLine Current => Open && Line >= 0 && Line < With.Lines.Count ? With.Lines[Line] : null;
        public bool Waiting => Current != null && Current.Choices.Length > 0;
        public event Action<string> Logged;
        public event Action<EraPoint> Ended;
        public bool Begin(EraPoint p)
        {
            if (p == null || p.Lines.Count == 0) return false;
            With = p; Line = 0; Chosen = ""; Logged?.Invoke("talk_started:" + p.Id); return true;
        }
        // Continue: the next line, or the end. A line waiting for a choice does not continue.
        public bool Next()
        {
            if (!Open || Waiting) return false;
            Line++; if (Line >= With.Lines.Count) End(); return true;
        }
        public bool Choose(int i)
        {
            if (!Waiting || i < 0 || i >= Current.Choices.Length) return false;
            Chosen = Current.Choices[i]; Logged?.Invoke("talk_chose:" + With.Id + ":" + i);
            Line++; if (Line >= With.Lines.Count) End(); return true;
        }
        void End() { var p = With; With = null; Line = -1; Logged?.Invoke("talk_ended:" + p.Id); Ended?.Invoke(p); }
    }
}
