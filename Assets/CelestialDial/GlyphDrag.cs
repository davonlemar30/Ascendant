using UnityEngine;
using UnityEngine.EventSystems;

namespace Ascendant.CelestialDial
{
    // The Book of Symbols' waiting glyph can be dragged as well as tapped (owner, Oct 7: "drag and tap"; the Quill Line, owner, Oct 17,
    // Concept 1). A press that barely moves stays a tap: uGUI starts a drag only past the event system's threshold, and a drag stops
    // the Button's click. Mirrors PlateDrag's pattern exactly; the Book has one glyph in hand, not twelve plates, so there is no Seat.
    public sealed class GlyphDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public SliceView View;
        bool dragging;
        public void OnBeginDrag(PointerEventData e) { dragging = View != null && View.GlyphDragBegin(); if (dragging) View.GlyphDragMove(e.position, e.pressEventCamera); }
        public void OnDrag(PointerEventData e) { if (dragging) View.GlyphDragMove(e.position, e.pressEventCamera); }
        public void OnEndDrag(PointerEventData e) { if (!dragging) return; dragging = false; View.GlyphDragEnd(); }
        // uGUI sends no OnEndDrag to a hidden glyph: a drag cut short sends it home and answers nothing
        void OnDisable() { if (!dragging) return; dragging = false; if (View != null) View.GlyphDragCancel(); }
    }
}
