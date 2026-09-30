using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Build U (owner, APK playtest, Sept 29; task 86bc8ddzd): a gear at the top right of every screen opens Settings.
    // The player's settings: Sound, Reduced motion, and Quit (the phone app only; a web page cannot close itself).
    // Below them, a Testing section takes the Atrium's old test buttons (Walk speed, Start over) off the player's screen;
    // DEV Mode's skip-ahead waits for its own design. The panel wears the instruments' slim box (Claude's working choice).
    public sealed class SettingsMenu : MonoBehaviour
    {
        public static readonly bool CanQuit = !(Application.platform == RuntimePlatform.WebGLPlayer);
        public Func<bool> Muted, Reduced; public Func<string> WalkSpeed;
        public Action ToggleSound, ToggleMotion, CycleWalk, StartOver, Changed;
        public bool Open { get; private set; }
        public bool GearShown { get => gear != null && gear.gameObject.activeSelf; set { if (gear != null) gear.gameObject.SetActive(value); if (!value) Close(); } }
        Canvas canvas; RectTransform menu; Button gear; Text sound, motion, walk; Font font;
        static readonly Color Gold = new Color(.84f, .69f, .38f), Bone = new Color(.93f, .89f, .8f), RowColor = new Color(.16f, .15f, .18f);

        public void Build(Font uiFont)
        {
            font = uiFont;
            var canvasObject = new GameObject("Settings Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5; // over the slice (1), under the white light (10)
            var root = new GameObject("Settings Portrait", typeof(RectTransform)).GetComponent<RectTransform>(); root.SetParent(canvasObject.transform, false);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f); root.sizeDelta = new Vector2(360, 800);
            // The gear: drawn in code (the web font has no gear glyph), gold on a dark disc, at the top right.
            gear = MakeButton(root, "", 158, 22, 36, 36, Toggle); gear.name = "Settings gear";
            var gearImage = gear.GetComponent<Image>(); gearImage.sprite = GearSprite(); gearImage.color = Color.white;
            // The menu: a dim veil (a tap on it closes) and the slim box in the middle.
            menu = Rect("Settings", root, 0, 400, 360, 800);
            var veil = menu.gameObject.AddComponent<Image>(); veil.color = new Color(0, 0, 0, .6f);
            var veilButton = menu.gameObject.AddComponent<Button>(); veilButton.targetGraphic = veil; veilButton.transition = Selectable.Transition.None; veilButton.onClick.AddListener(Close);
            bool quit = CanQuit; float height = quit ? 410 : 350;
            var panel = Rect("Settings box", menu, 0, 400, 280, height);
            var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.sprite = Slots.InstrumentBoxSprite(); panelImage.type = Image.Type.Sliced; panelImage.pixelsPerUnitMultiplier = 2;
            panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // taps on the box itself do not close it
            var title = Label(panel, "S E T T I N G S", 0, 24, 240, 18, 12); title.color = Gold; title.fontStyle = FontStyle.Bold;
            float y = 66;
            sound = Row(panel, y, () => ToggleSound?.Invoke()); y += 56;
            motion = Row(panel, y, () => ToggleMotion?.Invoke()); y += 56;
            if (quit) { var q = Row(panel, y, Application.Quit); q.text = "Quit the game"; q.transform.parent.name = "Quit the game"; y += 56; }
            var testing = Label(panel, "T E S T I N G", 0, y - 6, 240, 16, 10); testing.color = new Color(Gold.r, Gold.g, Gold.b, .7f); y += 22;
            walk = Row(panel, y, () => CycleWalk?.Invoke(), 40); y += 48;
            var over = Row(panel, y, () => StartOver?.Invoke(), 40); over.text = "Start over"; y += 56;
            var close = Row(panel, y, Close); close.text = "Close"; close.color = Gold;
            menu.gameObject.SetActive(false);
            Refresh();
        }
        public void Toggle() { if (Open) Close(); else { Open = true; menu.gameObject.SetActive(true); Refresh(); Changed?.Invoke(); } }
        public void Close() { if (!Open) return; Open = false; menu.gameObject.SetActive(false); Changed?.Invoke(); }
        public void Refresh()
        {
            if (sound == null) return;
            sound.text = "Sound: " + (Muted != null && Muted() ? "off" : "on");
            motion.text = "Reduced motion: " + (Reduced != null && Reduced() ? "on" : "off");
            walk.text = "Walk: " + (WalkSpeed != null ? WalkSpeed() : "normal");
        }
        void Update() { if (canvas != null && canvas.pixelRect.width >= 1) canvas.scaleFactor = Mathf.Min(canvas.pixelRect.width / 360f, canvas.pixelRect.height / 800f); }

        // ---- building ----
        Text Row(RectTransform panel, float top, UnityEngine.Events.UnityAction action, float height = 48)
        {
            var b = MakeButton(panel, "", 0, top, 232, height, () => { action(); Refresh(); Changed?.Invoke(); });
            b.GetComponent<Image>().color = RowColor; var t = b.GetComponentInChildren<Text>(); t.fontSize = 14; return t;
        }
        // x from the parent's centre, top = the centre's distance below the parent's top edge (the slice's 360 x 800 convention).
        static RectTransform Rect(string name, Transform parent, float x, float top, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -top); return rect;
        }
        Button MakeButton(Transform parent, string text, float x, float top, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(text == "" ? "Button" : text, parent, x, top, width, height);
            var image = rect.gameObject.AddComponent<Image>(); image.color = RowColor; image.canvasRenderer.cullTransparentMesh = false;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            var label = Label(rect, text, 0, height / 2, width, height, 14); label.raycastTarget = false;
            return button;
        }
        Text Label(Transform parent, string text, float x, float top, float width, float height, int size)
        {
            var label = Rect("Label", parent, x, top, width, height).gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.color = Bone; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }
        // A gear: a dark disc, a gold ring with eight teeth, a dark hub. Made once, no file.
        static Sprite gearSprite;
        static Sprite GearSprite()
        {
            if (gearSprite != null) return gearSprite;
            const int size = 72; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color disc = new Color(.086f, .078f, .094f, .85f), gold = new Color(.84f, .69f, .38f, 1);
            float c = size / 2f;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = x + .5f - c, dy = y + .5f - c, r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
                float tooth = Mathf.Clamp01((Mathf.Cos(a * 8) - .35f) * 3f); // eight flat-ish teeth
                float outer = 20 + 6 * tooth, inner = 9;
                float gearA = Mathf.Clamp01(outer + .5f - r) * Mathf.Clamp01(r - inner + .5f);
                float discA = Mathf.Clamp01(c - 1 - r + .5f) * disc.a;
                var col = Color.Lerp(new Color(disc.r, disc.g, disc.b, discA), gold, gearA); col.a = Mathf.Max(discA, gearA);
                texture.SetPixel(x, y, col);
            }
            texture.Apply(); gearSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100); return gearSprite;
        }
    }
}
