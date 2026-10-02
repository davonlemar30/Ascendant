using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Platform fit, Part 1 (owner, Oct 1: "Hide the bar and draw behind it"; APK Session 2, finding 1a; task 86bcbn6mf). On Android the
    // game runs in immersive fullscreen and draws behind the strip the status bar and the camera cutout used, so nothing in the top row
    // may sit under that strip: the title, the gear and the mini-menu button move down by the screen's unsafe top band where it reaches
    // into the 360 x 800 frame. The web and the Editor report no band; tests set one with the web action safe-inset:<px> (layout units).
    public static class SafeArea
    {
        public static float Simulated = -1; // test-only: a top band in layout units from the screen's top edge, or -1 for the screen's own
        // The lines under a title give up this much of their gap before they move, so a header keeps its order without pushing into what
        // sits below it (a title's glyphs end about 17 px above the next line's: 4 px stay).
        public const float UnderTitle = 13;

        // The unsafe band at the top of the frame, in layout units: 0 when the band ends above the frame (or there is none).
        public static float TopInset(Canvas canvas)
        {
            if (canvas == null) return 0; var px = canvas.pixelRect; if (px.width < 1 || px.height < 1) return 0;
            float scale = Mathf.Min(px.width / 360f, px.height / 800f), frameTop = (px.height - 800 * scale) / 2;
            float band = Simulated >= 0 ? Simulated * scale : Mathf.Max(0, Screen.height - Screen.safeArea.yMax);
            return Mathf.Max(0, band - frameTop) / scale;
        }
        // A top-row piece: it moves down by the band, less its clearance (0 for the title, the gear and the mini-menu button).
        public static T Top<T>(T piece, float clearance = 0) where T : Component { piece.gameObject.AddComponent<SafeTop>().Clearance = clearance; return piece; }

        // Immersive fullscreen on Android: the status and navigation bars hide, and a swipe from an edge shows them for a moment.
        // Set again whenever the game regains focus, since the system can bring the bars back after another app or a dialog.
        public static void Immersive() { if (Application.platform != RuntimePlatform.Android) return; Screen.fullScreenMode = FullScreenMode.FullScreenWindow; Screen.fullScreen = true; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Application.platform != RuntimePlatform.Android) return;
            var keeper = new GameObject("Platform fit"); Object.DontDestroyOnLoad(keeper); keeper.AddComponent<ImmersiveKeeper>(); Immersive();
        }
        sealed class ImmersiveKeeper : MonoBehaviour { void OnApplicationFocus(bool focused) { if (focused) Immersive(); } }

        // Android draws the built-in UI font (LegacyRuntime) in its own system sans, a little wider than the web's and the Editor's. Filling a
        // 9:20 phone edge to edge draws the frame at exactly 3x, and there a line tuned to its box can spill over and lose its end (the emulator,
        // Oct 2: Caspar's "...when you are ready." in the Atrium, the name screen's second sentence). On Android such a line shrinks by up to
        // two points to fit its box; a line that fits keeps its size, and the web and the Editor draw exactly as before.
        public static int FitAndroidText(Transform root, Font builtin, bool force = false)
        {
            if ((!force && Application.platform != RuntimePlatform.Android) || root == null || builtin == null) return 0; int fitted = 0;
            foreach (var t in root.GetComponentsInChildren<Text>(true))
                if (t.font == builtin && !t.resizeTextForBestFit && t.fontSize > 8 && t.horizontalOverflow == HorizontalWrapMode.Wrap && t.verticalOverflow == VerticalWrapMode.Truncate)
                { t.resizeTextMaxSize = t.fontSize; t.resizeTextMinSize = Mathf.Max(8, t.fontSize - 2); t.resizeTextForBestFit = true; fitted++; }
            return fitted;
        }
    }

    // Keeps a top-row piece at its designed place, moved down by SafeArea.TopInset less its clearance.
    public sealed class SafeTop : MonoBehaviour
    {
        public float Clearance; public float Shift { get; private set; }
        Vector2 home; bool homed; Canvas canvas;
        void LateUpdate()
        {
            var rect = (RectTransform)transform; if (!homed) { home = rect.anchoredPosition; homed = true; }
            if (canvas == null) { canvas = GetComponentInParent<Canvas>(); if (canvas != null) canvas = canvas.rootCanvas; }
            float shift = Mathf.Max(0, SafeArea.TopInset(canvas) - Clearance);
            if (Mathf.Abs(shift - Shift) < .01f) return; Shift = shift; rect.anchoredPosition = home - new Vector2(0, shift);
        }
    }
}
