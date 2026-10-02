using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Platform fit, Part 2 (owner, Oct 1; task 86bcbn6mf; the owner's record, doc 2kyd583p-7114, points 1 to 4). The 360 x 800 frame is now
    // the centred design column, and a full-screen layer reaches past it to every edge of the screen: one master painting per room covers
    // every supported shape (600 x 920 on the layout, 1200 x 1840 in the file: a 9:23 phone up to a 3:4 tablet), cropped from the centre.
    // A file at the master's size draws whole, centred on the column. Until a room's master lands, its 720 x 1600 painting stands in (the
    // stop-gap, point 2 B): the column draws as today and its edges continue outward, mirrored, dimmed toward the screen's edges. So a master
    // is a file drop: the same slot, a bigger file, no code change. Plain fills and gradients just grow (their edge colour carries on).
    // Only the sides that lie on the column's edge grow (a band across the top grows up and sideways, not down). Taps stay in the column.
    public sealed class Bleed : BaseMeshEffect
    {
        public const float Width = 600, Height = 920, ColumnWidth = 360, ColumnHeight = 800;
        public enum Mode { Mirror, Clamp }
        public Mode How = Mode.Mirror; public float EdgeShade = .55f; public RectTransform Column;
        public static Bleed Add(Graphic graphic, RectTransform column, Mode how = Mode.Mirror, float edgeShade = .55f)
        {
            if (graphic == null) return null; var b = graphic.GetComponent<Bleed>(); if (b == null) b = graphic.gameObject.AddComponent<Bleed>(); // not ??: the Editor hands back a stand-in for a missing component
            b.How = how; b.EdgeShade = edgeShade; b.Column = column; graphic.SetVerticesDirty(); return b;
        }
        // A file is a master when its shape is the master's (600:920), within a pixel.
        public static bool IsMaster(Sprite sprite) => sprite != null && Mathf.Abs(sprite.rect.width / sprite.rect.height - Width / Height) < .002f;
        public bool ShowsMaster { get { var image = graphic as Image; return image != null && IsMaster(image.sprite); } }
        // Batch 2 (owner, Oct 2: the bleed masters approved): the full-screen layers on screen drawing a master whole, by file, for the web state.
        public static string[] Masters(params Transform[] roots) => roots.Where(r => r != null).SelectMany(r => r.GetComponentsInChildren<Bleed>())
            .Where(b => b.isActiveAndEnabled && b.ShowsMaster && b.graphic.canvas != null && b.graphic.canvas.enabled).Select(b => ((Image)b.graphic).sprite.name)
            .Distinct().OrderBy(n => n, System.StringComparer.Ordinal).ToArray();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount != 4) return; // a simple quad only (a filled or sliced picture keeps its own mesh)
            var rect = graphic.rectTransform.rect; var image = graphic as Image; var sprite = image != null ? image.sprite : null;
            UIVertex v = default; vh.PopulateUIVertex(ref v, 0); var colour = v.color; // the graphic's own tint, set by the Image
            // Where this graphic's rect sits in the column (the column's centre at 0, 0), and how far each side may grow.
            Vector2 min = rect.min, max = rect.max, offset = Vector2.zero;
            if (Column != null && Column != graphic.rectTransform) { var c = Column.InverseTransformPoint(graphic.rectTransform.TransformPoint(Vector3.zero)); offset = new Vector2(c.x, c.y) - Column.rect.center; }
            float colL = -ColumnWidth / 2 - offset.x, colR = ColumnWidth / 2 - offset.x, colB = -ColumnHeight / 2 - offset.y, colT = ColumnHeight / 2 - offset.y; // the column, in this rect's space
            float growL = Mathf.Abs(min.x - colL) < 1 ? (Width - ColumnWidth) / 2 : 0, growR = Mathf.Abs(max.x - colR) < 1 ? (Width - ColumnWidth) / 2 : 0;
            float growB = Mathf.Abs(min.y - colB) < 1 ? (Height - ColumnHeight) / 2 : 0, growT = Mathf.Abs(max.y - colT) < 1 ? (Height - ColumnHeight) / 2 : 0;
            Vector4 uv = sprite != null ? UnityEngine.Sprites.DataUtility.GetOuterUV(sprite) : new Vector4(0, 0, 1, 1);
            vh.Clear();
            if (IsMaster(sprite) && growL > 0 && growR > 0 && growB > 0 && growT > 0)
            {
                // the master, whole: the column sits in its centre
                Quad(vh, new Vector2(min.x - growL, min.y - growB), new Vector2(max.x + growR, max.y + growT), new Vector2(uv.x, uv.y), new Vector2(uv.z, uv.w), colour, colour, colour, colour);
                return;
            }
            // The stop-gap: a 3 x 3 grid around the rect. The middle cell is the picture as it was; the others continue it outward.
            float[] xs = { min.x - growL, min.x, max.x, max.x + growR }, ys = { min.y - growB, min.y, max.y, max.y + growT };
            float w = Mathf.Max(1, max.x - min.x), h = Mathf.Max(1, max.y - min.y);
            float U(float x) => How == Mode.Clamp ? Mathf.Lerp(uv.x, uv.z, Mathf.Clamp01((x - min.x) / w)) : Mathf.Lerp(uv.x, uv.z, Mirror((x - min.x) / w));
            float V(float y) => How == Mode.Clamp ? Mathf.Lerp(uv.y, uv.w, Mathf.Clamp01((y - min.y) / h)) : Mathf.Lerp(uv.y, uv.w, Mirror((y - min.y) / h));
            Color32 Shade(int ix, int iy) { bool outer = (ix == 0 && growL > 0) || (ix == 3 && growR > 0) || (iy == 0 && growB > 0) || (iy == 3 && growT > 0); if (!outer || sprite == null || EdgeShade >= 1) return colour; var c = (Color)colour; return new Color(c.r * EdgeShade, c.g * EdgeShade, c.b * EdgeShade, c.a); } // a plain fill keeps its colour to the edge
            for (int iy = 0; iy < 3; iy++) for (int ix = 0; ix < 3; ix++)
            {
                if (xs[ix + 1] - xs[ix] < .001f || ys[iy + 1] - ys[iy] < .001f) continue; // a side that does not grow has no cell
                Quad(vh, new Vector2(xs[ix], ys[iy]), new Vector2(xs[ix + 1], ys[iy + 1]), new Vector2(U(xs[ix]), V(ys[iy])), new Vector2(U(xs[ix + 1]), V(ys[iy + 1])),
                    Shade(ix, iy), Shade(ix + 1, iy), Shade(ix + 1, iy + 1), Shade(ix, iy + 1));
            }
        }
        // 0..1 inside; outside, the picture reflected at its edge (-0.2 reads 0.2, 1.2 reads 0.8)
        static float Mirror(float t) => t < 0 ? -t : t > 1 ? 2 - t : t;
        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 uvA, Vector2 uvB, Color32 cBL, Color32 cBR, Color32 cTR, Color32 cTL)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(a.x, a.y), cBL, new Vector2(uvA.x, uvA.y)); vh.AddVert(new Vector3(b.x, a.y), cBR, new Vector2(uvB.x, uvA.y));
            vh.AddVert(new Vector3(b.x, b.y), cTR, new Vector2(uvB.x, uvB.y)); vh.AddVert(new Vector3(a.x, b.y), cTL, new Vector2(uvA.x, uvB.y));
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
