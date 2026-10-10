using System;
using System.Collections.Generic;
using System.Linq;
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
        public Action<string> Jump; // Build W: DEV Mode's Jump to…, a checkpoint id (DevCheckpoints)
        public Action CuspDay; // batch 2: DEV Mode's cusp-day sample (owner, Oct 2 evening): a fresh opening at the cusp question
        public Action<string> BirthOpening; // Oct 7 (86bced0tc): a fresh opening at each other path's first question: skip, no-time, no-place, neither
        public Func<string> WakeLabel; public Action CycleWake; // the Dial's wake-up (86bcbn6w6): DEV Mode previews each step
        // the launch (owner, Oct 9, 86bcg62x3, round 3): a Main menu row in the player's section, above Testing; before Key 1 it asks first (Ask).
        // On the menu the panel leaves out the in-game rows, Start over and Main menu (InGame false).
        public Action MainMenu, MainMenuConfirmed; public Func<bool> InGame;
        public const string MainMenuWords = "Main menu";
        public bool JumpsShown => jumpPanel != null && jumpPanel.gameObject.activeSelf;
        public bool Open { get; private set; }
        public Vector2 GearAt => gear != null && gear.GetComponent<SafeTop>() != null ? gear.GetComponent<SafeTop>().At : new Vector2(158, 22); // Part 2: where the gear sits (x from the column's centre, y down from its top)
        public Transform Gear => gear != null ? gear.transform : null; // batch 2: the gear's button, for the web state's list of buttons
        public bool GearShown { get => gear != null && gear.gameObject.activeSelf; set { if (gear != null) gear.gameObject.SetActive(value); if (!value) Close(); } }
        Canvas canvas; RectTransform menu, mainPanel, jumpPanel, askPanel; Button gear; Text sound, motion, walk, wakeRow, askLine; Font font;
        RectTransform soundRow, motionRow, quitRow, mainMenuRow, testingLabel, walkRow, jumpRow, overRow, closeRow;
        public bool Asking => askPanel != null && askPanel.gameObject.activeSelf;
        public string AskLine => askLine != null ? askLine.text : "";
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
            gear = MakeButton(root, "", 158, 22, 36, 36, Toggle); gear.name = "Settings gear"; SafeArea.Corner(gear, 1); // Platform fit: below the cutout's band (Part 1); on a phone, at the screen's safe top-right corner (Part 2)
            var gearImage = gear.GetComponent<Image>(); gearImage.sprite = GearSprite(); gearImage.color = Color.white;
            ButtonLook.HitArea(gear); // batch 2 (3d): the gear takes taps over 44 x 44; it is drawn at 36
            // The menu: a dim veil (a tap on it closes) and the slim box in the middle.
            menu = Rect("Settings", root, 0, 400, 360, 800);
            var veil = menu.gameObject.AddComponent<Image>(); veil.color = new Color(0, 0, 0, .6f); Bleed.Add(veil, root); // Part 2: the veil dims the whole screen
            var veilButton = menu.gameObject.AddComponent<Button>(); veilButton.targetGraphic = veil; veilButton.transition = Selectable.Transition.None; veilButton.onClick.AddListener(Close);
            bool quit = CanQuit; float height = quit ? 510 : 454; // Build W: one more Testing row, Jump to...; the launch (Oct 9): Main menu (Layout sets it again)
            var panel = mainPanel = Rect("Settings box", menu, 0, 400, 280, height);
            var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.sprite = Slots.InstrumentBoxSprite(); panelImage.type = Image.Type.Sliced; panelImage.pixelsPerUnitMultiplier = 2;
            panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // taps on the box itself do not close it
            var title = Label(panel, "S E T T I N G S", 0, 24, 240, 18, 12); title.color = Gold; title.fontStyle = FontStyle.Bold;
            float y = 66; // the rows' places are set by Layout, which leaves out the in-game rows on the menu
            sound = Row(panel, y, () => ToggleSound?.Invoke()); soundRow = RowOf(sound);
            motion = Row(panel, y, () => ToggleMotion?.Invoke()); motionRow = RowOf(motion);
            if (quit) { var q = Row(panel, y, Application.Quit); q.text = "Quit the game"; q.transform.parent.name = "Quit the game"; quitRow = RowOf(q); }
            var main = Row(panel, y, () => MainMenu?.Invoke()); main.text = MainMenuWords; main.transform.parent.name = MainMenuWords; mainMenuRow = RowOf(main); // the launch (owner, Oct 9)
            var testing = Label(panel, "T E S T I N G", 0, y - 6, 240, 16, 10); testing.color = new Color(Gold.r, Gold.g, Gold.b, .7f); testingLabel = testing.rectTransform;
            walk = Row(panel, y, () => CycleWalk?.Invoke(), 40); walkRow = RowOf(walk);
            var jump = Row(panel, y, ShowJumps, 40); jump.text = "Jump to..."; jump.transform.parent.name = "Jump to"; jumpRow = RowOf(jump); // Latin-1 dots: the web font has no ellipsis
            var over = Row(panel, y, () => StartOver?.Invoke(), 40); over.text = "Start over"; overRow = RowOf(over);
            var close = Row(panel, y, Close); close.text = "Close"; close.color = Gold; closeRow = RowOf(close);
            // Build W: the checkpoint list, in place of the main box while it shows.
            float jumpHeight = 62 + DevCheckpoints.All.Length * 46 + 46 * (1 + BirthSamples.Length) + 56 + 24 + 22; // the checkpoints, the opening's samples (the cusp day first), then the Dial's wake-up preview
            jumpPanel = Rect("Jump to box", menu, 0, 400, 280, jumpHeight);
            var jumpImage = jumpPanel.gameObject.AddComponent<Image>(); jumpImage.sprite = Slots.InstrumentBoxSprite(); jumpImage.type = Image.Type.Sliced; jumpImage.pixelsPerUnitMultiplier = 2;
            jumpPanel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            var jumpTitle = Label(jumpPanel, "J U M P   T O", 0, 24, 240, 18, 12); jumpTitle.color = Gold; jumpTitle.fontStyle = FontStyle.Bold;
            float jy = 62;
            foreach (var (id, label) in DevCheckpoints.All) { string target = id; var row = Row(jumpPanel, jy, () => Jump?.Invoke(target), 40); row.text = label; row.transform.parent.name = "Jump " + id; jy += 46; }
            var cusp = Row(jumpPanel, jy, () => CuspDay?.Invoke(), 40); cusp.text = "A cusp day (the opening)"; cusp.transform.parent.name = "Jump cusp"; jy += 46; // the cusp-day sample (owner, Oct 2 evening): the opening's question
            foreach (var (id, label) in BirthSamples) { string sample = id; var row = Row(jumpPanel, jy, () => BirthOpening?.Invoke(sample), 40); row.text = label; row.transform.parent.name = "Jump birth " + id; jy += 46; } // Oct 7: each path's first question
            wakeRow = Row(jumpPanel, jy, () => CycleWake?.Invoke(), 40); wakeRow.transform.parent.name = "Dial wake"; jy += 46; // DEV Mode: a tap steps the Dial's look (first visit, Key 1 to Key 4), then back to as earned
            var back = Row(jumpPanel, jy + 10, ShowMain); back.text = "Back"; back.color = Gold;
            jumpPanel.gameObject.SetActive(false);
            // the launch (Oct 9): Main menu's question before Key 1, in place of the main box (as Jump to's list): the line, Main menu, Back
            askPanel = Rect("Ask box", menu, 0, 400, 280, 238);
            var askImage = askPanel.gameObject.AddComponent<Image>(); askImage.sprite = Slots.InstrumentBoxSprite(); askImage.type = Image.Type.Sliced; askImage.pixelsPerUnitMultiplier = 2;
            askPanel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            askLine = Label(askPanel, "", 0, 66, 240, 60, 14); askLine.horizontalOverflow = HorizontalWrapMode.Wrap;
            var askYes = Row(askPanel, 136, () => MainMenuConfirmed?.Invoke()); askYes.text = MainMenuWords; askYes.transform.parent.name = "Ask " + MainMenuWords;
            var askBack = Row(askPanel, 192, ShowMain); askBack.text = "Back"; askBack.color = Gold; askBack.transform.parent.name = "Ask back";
            askPanel.gameObject.SetActive(false);
            menu.gameObject.SetActive(false);
            Refresh();
        }
        static RectTransform RowOf(Text label) => (RectTransform)label.transform.parent;
        // the main box's rows top to bottom: 56 apart in the player's section, 48 apart under Testing, 56 before Close; on the menu, no Main menu and no Start over
        void Layout()
        {
            if (mainPanel == null) return; bool inGame = InGame == null || InGame();
            float y = 66;
            foreach (var r in new[] { soundRow, motionRow, quitRow }) if (r != null) { Place(r, y); y += 56; }
            mainMenuRow.gameObject.SetActive(inGame); if (inGame) { Place(mainMenuRow, y); y += 56; }
            testingLabel.anchoredPosition = new Vector2(0, -(y - 6)); y += 22;
            overRow.gameObject.SetActive(inGame); var tests = inGame ? new[] { walkRow, jumpRow, overRow } : new[] { walkRow, jumpRow };
            for (int i = 0; i < tests.Length; i++) { Place(tests[i], y); y += i < tests.Length - 1 ? 48 : 56; }
            Place(closeRow, y); mainPanel.sizeDelta = new Vector2(280, y + 46);
        }
        static void Place(RectTransform row, float y) => row.anchoredPosition = new Vector2(0, -y);
        // the rows of the box showing, as words and as their centres down the 360 x 800 layout (the web state; the checks tap them)
        public string[] RowWords => ShownRows().Select(b => b.GetComponentInChildren<Text>().text).ToArray();
        public float[] RowTops { get { var box = ShownBox(); return box == null ? new float[0] : ShownRows().Select(b => Mathf.Round((400 - box.sizeDelta.y / 2 - ((RectTransform)b.transform).anchoredPosition.y) * 100) / 100).ToArray(); } }
        RectTransform ShownBox() => !Open ? null : Asking ? askPanel : JumpsShown ? jumpPanel : mainPanel;
        IEnumerable<Button> ShownRows() { var box = ShownBox(); return box == null ? Enumerable.Empty<Button>() : box.GetComponentsInChildren<Button>().Where(b => b.transform.parent == box && b.GetComponentInChildren<Text>() != null); }
        public void Ask(string line) { if (!Open) Toggle(); askLine.text = line; mainPanel.gameObject.SetActive(false); jumpPanel.gameObject.SetActive(false); askPanel.gameObject.SetActive(true); Changed?.Invoke(); }
        public void ShowJumps() { mainPanel.gameObject.SetActive(false); jumpPanel.gameObject.SetActive(true); Changed?.Invoke(); }
        public void ShowMain() { jumpPanel.gameObject.SetActive(false); if (askPanel != null) askPanel.gameObject.SetActive(false); mainPanel.gameObject.SetActive(true); Changed?.Invoke(); }
        public void Toggle() { if (Open) Close(); else { Open = true; menu.gameObject.SetActive(true); Refresh(); Changed?.Invoke(); } }
        public void Close() { if (!Open) return; Open = false; menu.gameObject.SetActive(false); if (jumpPanel != null) { jumpPanel.gameObject.SetActive(false); mainPanel.gameObject.SetActive(true); } if (askPanel != null) askPanel.gameObject.SetActive(false); Changed?.Invoke(); }
        public void Refresh()
        {
            if (sound == null) return;
            sound.text = "Sound: " + (Muted != null && Muted() ? "off" : "on");
            motion.text = "Reduced motion: " + (Reduced != null && Reduced() ? "on" : "off");
            walk.text = "Walk: " + (WalkSpeed != null ? WalkSpeed() : "normal");
            if (wakeRow != null) wakeRow.text = "Dial wake: " + (WakeLabel != null ? WakeLabel() : "as earned");
            Layout();
        }
        void Update() { if (canvas != null && canvas.pixelRect.width >= 1) canvas.scaleFactor = Mathf.Min(canvas.pixelRect.width / 360f, canvas.pixelRect.height / 800f); }

        // ---- building ----
        public static readonly (string id, string label)[] BirthSamples = { ("skip", "I'll skip it (the opening)"), ("no-time", "No birth time (the opening)"), ("no-place", "No birth place (the opening)"), ("neither", "No birth time or place (the opening)") };
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
