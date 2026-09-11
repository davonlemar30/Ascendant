using UnityEngine;
using UnityEngine.EventSystems;

namespace Ascendant.CelestialDial
{
    public sealed class DialDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public DialView View;
        public void OnBeginDrag(PointerEventData e) { View.BeginDrag(e.position); }
        public void OnDrag(PointerEventData e) { View.Drag(e.position, e.delta); }
        public void OnEndDrag(PointerEventData e) { View.EndDrag(); }
    }
}
