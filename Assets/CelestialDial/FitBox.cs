using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Build R (owner, Sept 29): the instrument screens' Caspar box fits its line. Its top stays pinned; whenever the line changes it
    // takes the height the line needs (Slots.InstrumentBoxHeight), at most its old height, past which the text's best fit shrinks it.
    public sealed class FitBox : MonoBehaviour
    {
        public Text Line; public float Max;
        string last; float lastWidth;
        void LateUpdate()
        {
            if (Line == null) return;
            var rect = (RectTransform)transform; float width = Line.rectTransform.rect.width;
            if (Line.text == last && width == lastWidth) return; last = Line.text; lastWidth = width;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Slots.InstrumentBoxHeight(Line.preferredHeight, Max));
        }
    }
}
