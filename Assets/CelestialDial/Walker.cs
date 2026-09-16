using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendant.CelestialDial
{
    // v0.4 tap-to-move greybox (Q06 phase 2 lock, September 12): two walkable rooms, a placeholder marker,
    // straight-line walking at a fixed speed along a floor band, no pathfinding. Pure state, no Unity types.
    public enum Room { None, Atrium, Wing, Chamber }

    public sealed class PointOfInterest
    {
        public readonly string Id, Label; public readonly float X; public readonly bool Walkable;
        public PointOfInterest(string id, string label, float x, bool walkable = true) { Id = id; Label = label; X = x; Walkable = walkable; }
    }

    public static class Rooms
    {
        // X is the position along the floor band in the 360-wide reference layout, centered at 0.
        public static readonly PointOfInterest[] Atrium =
        {
            new PointOfInterest("desk", "the desk", -125),
            new PointOfInterest("wing-door", "the Zodiac Wing doorway", 0),
            new PointOfInterest("caspar", "Caspar", 76),   // stops beside him, not on him
            new PointOfInterest("sealed-left", "a sealed door", -120, false),
            new PointOfInterest("chamber-door", "the Crystal Book Chamber doorway", 120), // Build D: the third doorway; the Chamber is a room once the first visit is behind the player
            new PointOfInterest("entry", "where you came in", 40),   // arrival spot from the Chamber; not shown as a point of interest
        };
        // Build D: the Chamber as a room. The Keeper walks between the doorway and the Books.
        public static readonly PointOfInterest[] Chamber =
        {
            new PointOfInterest("atrium-door", "the doorway back to the Atrium", -140),
            new PointOfInterest("books", "the Crystal Books", 0),
        };
        public static readonly PointOfInterest[] Wing =
        {
            new PointOfInterest("atrium-door", "the doorway back to the Atrium", -130),
            new PointOfInterest("grid", "the table", -60),        // Build B: the 4 × 3 table wakes once the modality unit is complete (07 Room Scope amendment)
            new PointOfInterest("dial", "the Dial", 30),
            new PointOfInterest("shelf", "the bookshelf", 120),   // v0.3 revision: Part A lives here once the wheel is lit
        };
        public static PointOfInterest[] Of(Room room) => room == Room.Atrium ? Atrium : room == Room.Wing ? Wing : room == Room.Chamber ? Chamber : Array.Empty<PointOfInterest>();
        public static PointOfInterest Find(Room room, string id) => Of(room).FirstOrDefault(p => p.Id == id);
        public static IEnumerable<PointOfInterest> Visible(Room room) => Of(room).Where(p => p.Id != "entry");
    }

    public sealed class Walker
    {
        public const float SlowSpeed = 110f, NormalSpeed = 170f, FastSpeed = 260f; // px per second on the reference layout; a test variable
        public Room Room { get; private set; }
        public float X { get; private set; }
        public float TargetX { get; private set; }
        public string TargetId { get; private set; } = "";
        public string At { get; private set; } = "";        // the point of interest the marker stands at, or "" between them
        public bool Walking { get; private set; }
        public int Facing { get; private set; } = 1;        // +1 right, -1 left
        public float Speed { get; private set; } = NormalSpeed;
        public string SpeedName => Speed <= SlowSpeed ? "slow" : Speed >= FastSpeed ? "fast" : "normal";
        public float Traveled { get; private set; }         // distance walked on the current leg, drives the walk bob
        public event Action<string> Logged;

        public void Enter(Room room, string atPoi)
        {
            var poi = Rooms.Find(room, atPoi);
            Room = room; X = TargetX = poi != null ? poi.X : 0; At = poi != null ? poi.Id : ""; TargetId = ""; Walking = false; Traveled = 0;
            Logged?.Invoke("room_entered:" + room.ToString().ToLowerInvariant());
        }
        public PointOfInterest Find(string id) => Rooms.Find(Room, id);
        public bool CanWalkTo(string id) { var p = Find(id); return p != null && p.Walkable && !Walking; }
        // Returns false for unknown or sealed points; the room says "sealed" instead of walking.
        public bool GoTo(string id)
        {
            var poi = Find(id);
            if (poi == null || !poi.Walkable || Walking) return false;
            TargetId = poi.Id; TargetX = poi.X; Walking = true; Traveled = 0; At = "";
            if (Math.Abs(TargetX - X) > 0.5f) Facing = TargetX > X ? 1 : -1;
            Logged?.Invoke("walk_started:" + poi.Id);
            return true;
        }
        // Advances the walk by dt seconds; returns true on the tick the marker arrives.
        public bool Tick(float dt)
        {
            if (!Walking) return false;
            float step = Speed * Math.Max(0, dt), remaining = TargetX - X;
            if (Math.Abs(remaining) <= step) { X = TargetX; Traveled += Math.Abs(remaining); return Arrive(); }
            X += Math.Sign(remaining) * step; Traveled += step; return false;
        }
        // Reduced motion: no walk, the marker is simply there.
        public bool Jump()
        {
            if (!Walking) return false;
            Traveled += Math.Abs(TargetX - X); X = TargetX; return Arrive();
        }
        bool Arrive() { Walking = false; At = TargetId; Logged?.Invoke("walk_arrived:" + TargetId); return true; }
        public float WalkSeconds(string id) { var p = Find(id); return p == null ? 0 : Math.Abs(p.X - X) / Speed; }
        // Vertical bob while walking: one step every 14 px, up to 3 px high. Still when standing.
        public float Bob => Walking ? (float)Math.Abs(Math.Sin(Traveled / 14f * Math.PI)) * 3f : 0f;
        public void SetSpeed(float speed) { Speed = speed; }
        public void CycleSpeed() { Speed = Speed <= SlowSpeed ? NormalSpeed : Speed >= FastSpeed ? SlowSpeed : FastSpeed; Logged?.Invoke("test_walk_speed:" + SpeedName); }
    }
}
