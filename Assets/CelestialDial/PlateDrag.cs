using UnityEngine;
using UnityEngine.EventSystems;

namespace Ascendant.CelestialDial
{
    // The Elemental Table's plates can be dragged as well as tapped (owner, Oct 7: "drag and tap"; task 86bcf0x71). A press that barely moves
    // stays a tap: uGUI starts a drag only past the event system's threshold, and a drag stops the Button's click.
    public sealed class PlateDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public SliceView View; public int Seat;
        bool dragging;
        public void OnBeginDrag(PointerEventData e) { dragging = View != null && View.PlateDragBegin(Seat); if (dragging) View.PlateDragMove(e.position, e.pressEventCamera); }
        public void OnDrag(PointerEventData e) { if (dragging) View.PlateDragMove(e.position, e.pressEventCamera); }
        public void OnEndDrag(PointerEventData e) { if (!dragging) return; dragging = false; View.PlateDragEnd(); }
    }
}
