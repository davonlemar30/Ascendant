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
        public static System.Action Moved; // a corner piece moved (a new screen shape or band)
        public static float Simulated = -1; // test-only: a top band in layout units from the screen's top edge, or -1 for the screen's own
        // The lines under a title give up this much of their gap before they move, so a header keeps its order without pushing into what
        // sits below it (a title's glyphs end about 17 px above the next line's: 4 px stay).
        public const float UnderTitle = 13;

        // The unsafe band at the top of the frame, in layout units: 0 when the band ends above the frame (or there is none).
        public static float TopInset(Canvas canvas)
        {
            if (canvas == null) return 0; var px = canvas.pixelRect; if (px.width < 1 || px.height < 1) return 0;
            float scale = Mathf.Min(px.width / 360f, px.height / 800f), frameTop = (px.height - 800 * scale) / 2 / scale; // in layout units
            float band = Simulated >= 0 ? Simulated : Mathf.Max(0, Screen.height - Screen.safeArea.yMax) / scale;
            return Mathf.Round(Mathf.Max(0, band - frameTop) * 100) / 100; // to a hundredth of a layout pixel, so a simulated band reads back exactly
        }
        // A top-row piece: it moves down by the band, less its clearance (0 for the title, the gear and the mini-menu button).
        public static T Top<T>(T piece, float clearance = 0) where T : Component { piece.gameObject.AddComponent<SafeTop>().Clearance = clearance; return piece; }
        // A corner piece (Part 2): +1 the top right (the gear), -1 the top left (the mini-menu button).
        public static T Corner<T>(T piece, int corner) where T : Component { var t = piece.gameObject.AddComponent<SafeTop>(); t.Corner = corner; return piece; }
        // Part 2 (doc 2kyd583p-7114, point 1): every phone from 9:16 to 9:23 fills; wider shapes (tablets, the Fold's inner screen, desktop
        // windows) keep the column, with room art down the sides. A 9:16 screen is the widest phone.
        public static bool Phone(Canvas canvas) { if (canvas == null) return false; var px = canvas.pixelRect; return px.height >= 1 && px.width / px.height <= 9f / 16f + .01f; }
        // The screen's safe area against the column, in layout units: x its left edge and y its right edge from the column's centre, z its
        // top edge down from the column's top (negative when the screen reaches above the column).
        public static Vector3 Edges(Canvas canvas)
        {
            if (canvas == null) return new Vector3(-180, 180, 0); var px = canvas.pixelRect; if (px.width < 1 || px.height < 1) return new Vector3(-180, 180, 0);
            float scale = Mathf.Min(px.width / 360f, px.height / 800f), frameTop = (px.height - 800 * scale) / 2 / scale;
            var safe = Simulated >= 0 ? new UnityEngine.Rect(0, 0, px.width, px.height) : Screen.safeArea;
            float band = Simulated >= 0 ? Simulated : Mathf.Max(0, px.height - safe.yMax) / scale;
            float left = (safe.xMin - px.width / 2) / scale, right = (safe.xMax - px.width / 2) / scale;
            return new Vector3(Mathf.Round(left * 100) / 100, Mathf.Round(right * 100) / 100, Mathf.Round((band - frameTop) * 100) / 100);
        }

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

    // Keeps a top-row piece at its designed place, moved down by SafeArea.TopInset less its clearance. A corner piece (the gear, the
    // mini-menu button: Corner +1 or -1) on a phone keeps instead to the screen's safe corner, as far in from it as it sits in from the
    // column's corner today (Part 2: "edge controls anchor to the safe area"); on a tablet or a desktop window it stays with the column.
    // A piece that Follows another keeps its offset from it (the mini-menu's panel under its button).
    public sealed class SafeTop : MonoBehaviour
    {
        public float Clearance; public int Corner; public SafeTop Follow; public float Shift { get; private set; }
        public Vector2 Home => home; public Vector2 Placed { get; private set; } // anchored positions: designed, and where it is now
        public Vector2 At => new Vector2(Placed.x, -Placed.y); // where it sits now: x from the column's centre, y down from the column's top (layout units)
        Vector2 home; bool homed; Canvas canvas;
        // Home is read the moment the piece is made (its place is set before), so the web state has it before the first frame: a scene
        // that loads and publishes straight away (a DEV jump) would otherwise publish (0, 0) where nothing later moves the piece.
        void Awake() { home = ((RectTransform)transform).anchoredPosition; Placed = home; homed = true; }
        void LateUpdate()
        {
            var rect = (RectTransform)transform; if (!homed) { home = rect.anchoredPosition; Placed = home; homed = true; }
            if (canvas == null) { canvas = GetComponentInParent<Canvas>(); if (canvas != null) canvas = canvas.rootCanvas; }
            Vector2 place;
            if (Follow != null) { if (!Follow.homed) return; place = Follow.Placed + (home - Follow.home); Shift = Follow.Shift; }
            else if (Corner != 0 && SafeArea.Phone(canvas))
            {
                var edges = SafeArea.Edges(canvas); float inset = 180 - Mathf.Abs(home.x); // today's distance from the column's side
                place = new Vector2(Corner > 0 ? edges.y - inset : edges.x + inset, home.y - edges.z); Shift = edges.z; // edges.z: the safe top, down from the column's top (above it: negative)
            }
            else { Shift = Mathf.Max(0, SafeArea.TopInset(canvas) - Clearance); place = home - new Vector2(0, Shift); }
            if ((place - Placed).sqrMagnitude < .0001f) return; Placed = place; rect.anchoredPosition = place;
            if (Corner != 0) SafeArea.Moved?.Invoke(); // the web state republishes where the system buttons are
        }
    }
}
