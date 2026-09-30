using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Build X (owner, Sept 29: "a lot of dead space in the chat box when Caspar speaks in the Atrium"): the gold chat box fits its line, as the
    // instruments' slim box does (Build R). Its top stays where it was; its height follows the text, from Min (the sliced frame's corners whole)
    // to its old height, past which the text's best fit shrinks it as before. A Continue inside the box (the story pages) rides at its bottom,
    // and Caspar's waist-up figure behind it is clipped at the box's bottom edge, so a shorter box never shows where the figure is cut off.
    public sealed class ChatFit : MonoBehaviour
    {
        public const float Head = 22, Foot = 30, NextRow = 50, Min = 76; // Head just clears the name plate, Foot the frame's bottom border and its diamond: no dead space inside (owner, Sept 29)
        public Text Line; public RectTransform Next, Clip; public float Top, Max; // Top: the box's top edge, measured from its parent's top
        public System.Action Fitted; // the web state republishes the new height
        string last; float lastWidth = -1; bool lastNext;
        public float Height => ((RectTransform)transform).sizeDelta.y;
        public static float BoxHeight(float text, bool next, float max) => Mathf.Clamp(Head + text + (next ? NextRow : 0) + Foot, Mathf.Min(Min, max), max);
        void LateUpdate()
        {
            if (Line == null) return;
            bool next = Next != null && Next.gameObject.activeSelf;
            float width = Line.rectTransform.rect.width;
            if (Line.text == last && width == lastWidth && next == lastNext) return;
            last = Line.text; lastWidth = width; lastNext = next;
            var settings = Line.GetGenerationSettings(new Vector2(width, 0)); settings.resizeTextForBestFit = false;
            float text = Line.cachedTextGeneratorForLayout.GetPreferredHeight(Line.text ?? "", settings) / Line.pixelsPerUnit;
            float h = BoxHeight(text, next, Max);
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, h); rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -(Top + h / 2));
            var lr = Line.rectTransform; float lineHeight = Mathf.Max(8, h - Head - Foot - (next ? NextRow : 0));
            lr.sizeDelta = new Vector2(lr.sizeDelta.x, lineHeight); lr.anchoredPosition = new Vector2(lr.anchoredPosition.x, -(Head + lineHeight / 2));
            if (next) Next.anchoredPosition = new Vector2(Next.anchoredPosition.x, -(Top + h - Foot - NextRow / 2 + 4));
            if (Clip != null) Clip.sizeDelta = new Vector2(Clip.sizeDelta.x, Top + h);
            Fitted?.Invoke();
        }
    }
}
