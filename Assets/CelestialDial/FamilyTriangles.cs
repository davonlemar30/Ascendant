using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Batch 2, step 5 (owner, Oct 3: the family triangles overlay approved, mixed strength; the board on 86bcbn6w6, Oct 2). The four element
    // triangles lie ON TOP of the Cast Dial as light, a screen blend (Shaders/LightLines), so nothing under them darkens or disappears:
    // - resting, every time the Dial shows: thin lines and a small four-point star at each corner, at level A (34% / glow 10% / stars 55%),
    //   dusty on the worn Dial (65% of that, no glow) and clean from today's look on;
    // - teaching, while a lesson teaches a family: that triangle in its element's light at level A (70% / glow 55% / bloom 20%), and its
    //   three seats' frames glow (80%), the glow stopping short of their words;
    // - the payoff, once, the moment the whole wheel lights: all four as ribbons of flame in their element's colours at level B (88%), a few
    //   seconds, then they burn down into the resting lines.
    // The corners sit on the hub ring just under each seat's window (r 80.5), and every side breaks round every word it would cross (the eye's
    // challenge and count, the ribbon's name and facts): the light stops 2.5 px short of a word's box, like a label on a map. The seats' words
    // (their symbols, names and facts) are kept clear the same way. The lines turn with the ring, so a lit triangle follows its seats.
    // The light is the board's own (its sRGB values, screened); the shader turns it into linear colour with a gentler curve than sRGB's
    // (power 1.7), measured on today's look against the board: 49.6 levels of line contrast, the board's 50.9.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FamilyTriangles : MaskableGraphic
    {
        // the board's values (REPORT.md and tools/looks.cjs), on the 360 x 800 layout
        public const float CornerRadius = 80.5f;                                     // the hub ring, under each seat's window
        public const float Rest = .34f, RestGlow = .10f, Star = .55f, Dust = .65f;    // level A, resting; dust: the worn Dial's share of it
        public const float Teach = .70f, TeachGlow = .55f, TeachBloom = .20f, Seat = .80f; // level A, teaching
        public const float Flame = .88f;                                             // level B, the payoff (owner, Oct 3: it plays once, so it hits hard)
        public const float LineHalfWidth = .4f, WordGap = 2.5f, BloomGap = 9.5f, SeatGap = 3f, FlameGap = 5f; // the light's margins round a word's box
        public const float PayoffFull = 2.2f, PayoffBurn = 1.2f;                     // the flames' few seconds, then their burn down into the grooves
        public static readonly Color RestLight = new Color32(255, 238, 202, 255), DustLight = new Color32(176, 166, 148, 255); // pale warm gold; a dull bone grey
        public static readonly string[] Families = { "Fire", "Earth", "Air", "Water" };
        public static readonly Color[] FamilyGlow = { new Color32(240, 104, 48, 255), new Color32(118, 186, 78, 255), new Color32(236, 206, 120, 255), new Color32(78, 158, 240, 255) }; // batch 1: ember orange, moss green, pale gold, cool blue
        public static readonly Color[] FamilyCore = { new Color32(255, 212, 150, 255), new Color32(222, 255, 186, 255), new Color32(255, 248, 214, 255), new Color32(214, 240, 255, 255) };
        // a thin line's glow is a blur of its 0.8 px core: its peak, times the board's gain (looks.cjs), at each sigma
        static float GlowPeak(float sigma, float gain) => Mathf.Min(1, gain * 2 * LineHalfWidth / (sigma * 2.5066f));
        // a family's three sides: its seats are every fourth one (Fire: Aries, Leo, Sagittarius)
        public static int[][] Sides(int family) => new[] { new[] { family, family + 4 }, new[] { family + 4, family + 8 }, new[] { family + 8, family } };
        // ArcText's bend (a label's units onto its circle) and back again, so a point on the Dial can be tested against a curved word's box
        public static Vector2 Bend(Vector2 p, float r) => new Vector2((r + p.y) * Mathf.Sin(p.x / r), (r + p.y) * Mathf.Cos(p.x / r) - r);
        public static Vector2 Unbend(Vector2 q, float r) { float sign = Mathf.Sign(r), y = q.y + r; return new Vector2(Mathf.Atan2(sign * q.x, sign * y) * r, sign * Mathf.Sqrt(q.x * q.x + y * y) - r); }

        public DialView View;
        public Text[] HubWords = new Text[0];               // the eye's challenge and count, the ribbon's name and facts
        public readonly Text[][] SeatWords = new Text[12][]; // each seat's symbol, name and fact
        public int Teaching = -1;                            // the family taught now (its triangle lit), or -1
        public float Wake = 1;                               // 0 the worn Dial (dust), 1 today's look and the bright one
        float payoffAt = -1; Material lightMaterial; bool reduced;
        public bool PayoffPlaying => payoffAt >= 0 && Time.unscaledTime - payoffAt < PayoffFull + PayoffBurn;
        // evidence for the checks: what is drawn now
        public string Mode => View == null || !View.RingArt ? "" : PayoffPlaying ? "payoff" : Teaching >= 0 ? "teaching" : "resting";
        public System.Action Changed; // asks the Dial to publish its state again once the drawn evidence below has changed
        bool evidenceChanged; float publishedAt = -1;
        public float Shown { get; private set; }   // the share of the twelve sides' length drawn
        public int Gaps { get; private set; }      // the breaks round words
        public int Crossing { get; private set; }  // samples of drawn light inside a word's box (always 0)
        public readonly Vector2[] Corners = new Vector2[12];

        sealed class Words
        {
            public Text Text; public ArcText Arc; public Matrix4x4 ToLocal; public readonly List<Rect> Boxes = new List<Rect>();
            readonly TextGenerator generator = new TextGenerator(); string seen; Vector2 seenSize; int seenFont;
            // each word's box in the text's own units, before any bend: from a layout of its own, the same the text draws
            public void Measure()
            {
                bool on = Text != null && Text.isActiveAndEnabled && Text.text != "" && Text.canvasRenderer.GetInheritedAlpha() > .01f;
                if (!on) { Boxes.Clear(); seen = null; return; }
                var size = Text.rectTransform.rect.size; int font = Text.font != null ? Text.font.GetInstanceID() : 0;
                if (Text.text == seen && size == seenSize && font == seenFont) return;
                seen = Text.text; seenSize = size; seenFont = font; Boxes.Clear();
                generator.Populate(Text.text, Text.GetGenerationSettings(size)); var chars = generator.characters; var lines = generator.lines; float ppu = Text.pixelsPerUnit;
                for (int l = 0; l < lines.Count; l++)
                {
                    int from = lines[l].startCharIdx, to = l + 1 < lines.Count ? lines[l + 1].startCharIdx : chars.Count; float top = lines[l].topY / ppu, bottom = (lines[l].topY - lines[l].height) / ppu;
                    float x0 = 0, x1 = 0; bool inWord = false;
                    for (int i = from; i < to && i < chars.Count; i++)
                    {
                        bool ink = chars[i].charWidth > .01f && i < Text.text.Length && !char.IsWhiteSpace(Text.text[i]);
                        if (ink) { float a = chars[i].cursorPos.x / ppu, b = (chars[i].cursorPos.x + chars[i].charWidth) / ppu; if (!inWord) { x0 = a; inWord = true; } x1 = Mathf.Max(x1, b); }
                        else if (inWord) { Boxes.Add(Rect.MinMaxRect(x0, bottom, x1, top)); inWord = false; }
                    }
                    if (inWord) Boxes.Add(Rect.MinMaxRect(x0, bottom, x1, top));
                }
            }
            // whether a point (the triangles' units) lies within the margin of a word: back into the text's own units, and unbent off its arc
            public bool Near(Vector2 p, float margin)
            {
                if (Boxes.Count == 0) return false;
                Vector2 q = ToLocal.MultiplyPoint3x4(p);
                if (Arc != null && Mathf.Abs(Arc.Radius) >= 1) q = Unbend(q, Arc.Radius);
                for (int i = 0; i < Boxes.Count; i++) { var b = Boxes[i]; if (q.x > b.xMin - margin && q.x < b.xMax + margin && q.y > b.yMin - margin && q.y < b.yMax + margin) return true; }
                return false;
            }
        }
        readonly List<Words> hub = new List<Words>(); readonly List<Words>[] seats = new List<Words>[12];
        Words Wrap(Text t) => new Words { Text = t, Arc = t != null ? t.GetComponent<ArcText>() : null };

        protected override void OnEnable()
        {
            base.OnEnable(); raycastTarget = false;
            if (lightMaterial == null) { var shader = Resources.Load<Shader>("Shaders/LightLines"); if (shader != null) lightMaterial = new Material(shader); }
            if (lightMaterial != null) material = lightMaterial;
            if (canvas != null) canvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;
        }
        public bool ShaderLoaded => lightMaterial != null;
        // the moment the whole wheel lights (DialView sees the lesson reach it live, never on a load)
        public void Payoff(bool reducedMotion) { payoffAt = Time.unscaledTime; reduced = reducedMotion; Redraw(); }
        void Update()
        {
            if (evidenceChanged && Time.unscaledTime - publishedAt >= .3f) { evidenceChanged = false; publishedAt = Time.unscaledTime; Changed?.Invoke(); } // at most every 0.3 s: the ring's turn redraws every frame, and the state is large
            if (payoffAt < 0) return;
            float t = Time.unscaledTime - payoffAt, fade = t < PayoffFull ? 1 : Mathf.Clamp01(1 - (t - PayoffFull) / PayoffBurn);
            if (lightMaterial != null) { lightMaterial.SetFloat("_Fade", fade); lightMaterial.SetFloat("_Still", reduced ? 1 : 0); }
            if (t >= PayoffFull + PayoffBurn) { payoffAt = -1; Redraw(); evidenceChanged = true; }
        }
        public void Redraw()
        {
            if (hub.Count != HubWords.Length) { hub.Clear(); foreach (var t in HubWords) if (t != null) hub.Add(Wrap(t)); }
            for (int i = 0; i < 12; i++) if (seats[i] == null && SeatWords[i] != null) { seats[i] = new List<Words>(); foreach (var t in SeatWords[i]) if (t != null) seats[i].Add(Wrap(t)); }
            SetVerticesDirty();
        }

        // ---- the geometry ----
        Vector2 Corner(int seat) { var p = View != null ? View.SeatPosition(seat) : Vector2.zero; return p.sqrMagnitude > 0 ? p.normalized * CornerRadius : Vector2.zero; }
        // seats: -2 the hub's words only (a line: the sides run inside r 80.5, the seats' words sit outside r 86), -1 every word, or one seat's
        // words with the hub's (that seat's glow)
        bool NearWord(Vector2 p, float margin, int seats = -1)
        {
            foreach (var w in hub) if (w.Near(p, margin)) return true;
            if (seats == -2) return false;
            for (int i = 0; i < 12; i++) if (this.seats[i] != null && (seats < 0 || seats == i)) foreach (var w in this.seats[i]) if (w.Near(p, margin)) return true;
            return false;
        }
        // the parts of a side that are drawn, as fractions along it: every sample within the margin of a word is left out
        List<Vector2> Pieces(Vector2 a, Vector2 b, float margin, int seatOnly = -2)
        {
            var pieces = new List<Vector2>(); float length = Vector2.Distance(a, b); int n = Mathf.Max(2, Mathf.CeilToInt(length / .5f)); int from = -1;
            for (int i = 0; i <= n; i++)
            {
                bool clear = i < n && !NearWord(Vector2.Lerp(a, b, (i + .5f) / n), margin, seatOnly);
                if (clear && from < 0) from = i;
                if (!clear && from >= 0) { if ((i - from) * length / n >= 1.5f) pieces.Add(new Vector2((float)from / n, (float)i / n)); from = -1; }
            }
            return pieces;
        }

        // ---- the mesh: each quad carries its profile (see the shader) ----
        static Vector4 C(Color c, float k) => new Vector4(c.r * k, c.g * k, c.b * k, 0);
        void Quad(VertexHelper vh, Vector2 a, Vector2 b, float half, float kind, Vector4 glow, Vector4 hot, float length, Vector4 core, float coreHalf, float along0 = 0)
        {
            var dir = (b - a).normalized; var n = new Vector2(-dir.y, dir.x); if (kind > 1.5f && n.y < 0) n = -n; // a flame's tongues lick up the screen
            float len = Vector2.Distance(a, b); int start = vh.currentVertCount;
            for (int k = 0; k < 4; k++)
            {
                float s = k < 2 ? -half : half; bool end = k == 1 || k == 2; var p = (end ? b : a) + n * s;
                var v = UIVertex.simpleVert; v.position = p; v.color = Color.white;
                v.uv0 = new Vector4(s, along0 + (end ? len : 0), coreHalf, kind); v.uv1 = glow; v.uv2 = hot; v.uv3 = new Vector4(length, core.x, core.y, core.z);
                vh.AddVert(v);
            }
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
        void StarQuad(VertexHelper vh, Vector2 at, Vector4 core, Vector4 glow)
        {
            var radial = at.normalized; var tangent = new Vector2(-radial.y, radial.x); const float r = 4f; int start = vh.currentVertCount;
            for (int k = 0; k < 4; k++)
            {
                float x = k == 0 || k == 3 ? -r : r, y = k < 2 ? -r : r; var v = UIVertex.simpleVert; v.position = at + radial * x + tangent * y; v.color = Color.white;
                v.uv0 = new Vector4(x, y, 0, 1); v.uv1 = glow; v.uv2 = Vector4.zero; v.uv3 = new Vector4(0, core.x, core.y, core.z); vh.AddVert(v);
            }
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            float wasShown = Shown; int wasGaps = Gaps, wasCrossing = Crossing; vh.Clear(); Shown = 0; Gaps = 0; Crossing = 0;
            if (View == null || View.Lesson == null || !View.RingArt) return;
            var self = rectTransform.localToWorldMatrix;
            foreach (var w in hub) { w.Measure(); if (w.Text != null) w.ToLocal = w.Text.rectTransform.worldToLocalMatrix * self; }
            for (int i = 0; i < 12; i++) if (seats[i] != null) foreach (var w in seats[i]) { w.Measure(); if (w.Text != null) w.ToLocal = w.Text.rectTransform.worldToLocalMatrix * self; }
            for (int i = 0; i < 12; i++) Corners[i] = Corner(i);
            bool payoff = PayoffPlaying; int taught = payoff ? -1 : Teaching;
            float wake = Mathf.Clamp01(Wake), restK = Mathf.Lerp(Dust, 1, wake); var restColour = Color.Lerp(DustLight, RestLight, wake);
            var restCore = C(restColour, Rest * restK); var restGlow = C(restColour, RestGlow * GlowPeak(1.1f, 2.2f) * wake); restGlow.w = 1.1f;
            var starCore = C(restColour, Star * restK); var starGlow = C(restColour, RestGlow * GlowPeak(1.1f, 2.2f) * .8f * wake); starGlow.w = 1.1f;
            float total = 0, drawn = 0;
            for (int f = 0; f < 4; f++)
                for (int k = 0; k < 3; k++)
                {
                    var side = Sides(f)[k]; Vector2 a = Corners[side[0]], b = Corners[side[1]]; float length = Vector2.Distance(a, b); total += length;
                    var pieces = Pieces(a, b, WordGap); Gaps += Mathf.Max(0, pieces.Count - 1) + (pieces.Count > 0 && pieces[0].x > .001f ? 1 : 0) + (pieces.Count > 0 && pieces[pieces.Count - 1].y < .999f ? 1 : 0);
                    foreach (var piece in pieces)
                    {
                        Vector2 p0 = Vector2.Lerp(a, b, piece.x), p1 = Vector2.Lerp(a, b, piece.y); drawn += (piece.y - piece.x) * length;
                        if (f == taught)
                        {
                            var core = C(FamilyCore[f], Teach); var glow = C(FamilyGlow[f], TeachGlow * GlowPeak(1.3f, 2.4f)); glow.w = 1.3f;
                            Quad(vh, p0, p1, 4f, 0, glow, Vector4.zero, length, core, LineHalfWidth);
                        }
                        else Quad(vh, p0, p1, 3.5f, 0, restGlow, Vector4.zero, length, restCore, LineHalfWidth);
                        for (int s = 0; s <= 8; s++) if (NearWord(Vector2.Lerp(p0, p1, s / 8f), 0, -2)) Crossing++; // evidence: no drawn light inside a word's box
                    }
                    if (f == taught) // the bloom keeps further from the words
                        foreach (var piece in Pieces(a, b, BloomGap)) { var bloom = C(FamilyGlow[f], TeachBloom * GlowPeak(3.5f, 5f)); bloom.w = 3.5f; Quad(vh, Vector2.Lerp(a, b, piece.x), Vector2.Lerp(a, b, piece.y), 11f, 0, bloom, Vector4.zero, length, Vector4.zero, 0); }
                    if (payoff) // the flames, along the same sides
                        foreach (var piece in Pieces(a, b, FlameGap)) Quad(vh, Vector2.Lerp(a, b, piece.x), Vector2.Lerp(a, b, piece.y), 14f, 2, C(FamilyGlow[f], Flame), C(new Color(Mathf.Max(0, FamilyCore[f].r - FamilyGlow[f].r), Mathf.Max(0, FamilyCore[f].g - FamilyGlow[f].g), Mathf.Max(0, FamilyCore[f].b - FamilyGlow[f].b)), Flame) + new Vector4(0, 0, 0, f * 7 + 3 + k * 13), length, Vector4.zero, 0, piece.x * length);
                }
            Shown = total > 0 ? drawn / total : 0; if (!Mathf.Approximately(Shown, wasShown) || Gaps != wasGaps || Crossing != wasCrossing) evidenceChanged = true;
            // the corners' stars, each left out if a word comes within its reach
            for (int i = 0; i < 12; i++)
            {
                if (NearWord(Corners[i], WordGap + 2.1f, -1)) continue;
                int f = i % 4; bool lit = f == taught; var core = lit ? C(FamilyCore[f], Teach) : starCore; var glow = lit ? C(FamilyGlow[f], TeachGlow * GlowPeak(1.3f, 2.4f)) : starGlow; glow.w = lit ? 1.3f : 1.1f;
                StarQuad(vh, Corners[i], core, glow);
            }
            // the taught seats' frames glow (the window and the name's recess), the glow stopping short of their words
            if (taught >= 0)
                for (int k = 0; k < 3; k++)
                {
                    int seat = taught + 4 * k; float deg = Mathf.Atan2(Corners[seat].y, Corners[seat].x) * Mathf.Rad2Deg; var glow = C(FamilyGlow[taught], Seat * .43f); glow.w = 1.9f; // the board's rim: a 1.6 px band blurred at 3.6 file px, times 2.4, peaks near .43 of the level
                    foreach (var face in new[] { new Vector3(91, 127, 11.75f), new Vector3(136, 155, 13.25f) })
                    {
                        var outline = new List<Vector2>();
                        for (int s = 0; s <= 8; s++) outline.Add(Polar(face.x, deg - face.z + 2 * face.z * s / 8)); // the inner arc
                        for (int s = 8; s >= 0; s--) outline.Add(Polar(face.y, deg - face.z + 2 * face.z * s / 8)); // the outer arc, back
                        outline.Add(outline[0]);
                        for (int s = 0; s + 1 < outline.Count; s++) foreach (var piece in Pieces(outline[s], outline[s + 1], SeatGap, seat)) Quad(vh, Vector2.Lerp(outline[s], outline[s + 1], piece.x), Vector2.Lerp(outline[s], outline[s + 1], piece.y), 6f, 0, glow, Vector4.zero, 0, Vector4.zero, 0);
                    }
                }
        }
        static Vector2 Polar(float r, float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)) * r;
    }
}
