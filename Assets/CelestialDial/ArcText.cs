using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Build AB (owner, Sept 30: the Astrolabe): bends a label's glyphs along a circle, so a sign's name follows the Dial's name band.
    // Radius is the circle's radius in the label's own units: positive bends around a centre below the label (its top faces outward),
    // negative around a centre above it (the label turned upright on the lower half of the wheel). Add it after any Shadow or Outline,
    // so their copies bend too.
    [RequireComponent(typeof(Text))]
    public sealed class ArcText : BaseMeshEffect
    {
        float radius = 100;
        public float Radius { get => radius; set { if (Mathf.Approximately(radius, value)) return; radius = value; if (graphic != null) graphic.SetVerticesDirty(); } }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0 || Mathf.Abs(radius) < 1) return;
            var verts = new List<UIVertex>(); vh.GetUIVertexStream(verts);
            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i]; var p = v.position;
                float theta = p.x / radius, rho = radius + p.y;
                v.position = new Vector3(rho * Mathf.Sin(theta), rho * Mathf.Cos(theta) - radius, p.z);
                verts[i] = v;
            }
            vh.Clear(); vh.AddUIVertexTriangleStream(verts);
        }
    }
}
