using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Vertical slice v0.1: the locked opening and Chamber bookends around the Dial (Q01, Q04, Q06, Q07).
    // Placeholder screens only. Every line is placeholder copy except the locked ones marked below.
    public sealed class SliceView : MonoBehaviour, DialView.ISliceState
    {
        public SliceFlow Flow { get; private set; } = new SliceFlow();
        public DialView Dial { get; private set; }
        public bool Busy => busy;
        Canvas canvas, flashCanvas; RectTransform root; Font font;
        RectTransform identity, birth, atrium, atriumReturn, chamber;
        Text birthNote, atriumText, returnText, chamberText, chamberEnd, keyIndicator, keyLabel;
        InputField nameField;
        Button birthContinue, wingContinue, insert, restart;
        readonly Button[] birthOptions = new Button[3];
        Image flash, seam, keyGlow, candle;
        readonly Image[] floorLines = new Image[24];
        readonly Image[] candles = new Image[9];
        readonly Image[] locks = new Image[SliceFlow.Books * SliceFlow.LocksPerBook];
        RectTransform keyRect, mechanism;
        bool busy, revealStarted;
        static readonly Color Bone = new Color(.94f,.91f,.86f), Charcoal = new Color(.075f,.075f,.09f), Crimson = new Color(.46f,.09f,.15f);
        static readonly Color PanelColor = new Color(.13f,.13f,.15f), Dim = new Color(.21f,.21f,.24f), Muted = new Color(.62f,.57f,.53f);
        const string AtriumLine = "You are awake. Good. This is the Library. It has been asleep a long while.\nI am Caspar. I keep it as best I can, but only a Keeper can wake it. Thank you for coming.\nThe Zodiac Wing is this way.";
        const string ReturnLine = "You have it. I did not think I would see one again.\nCome. There is a room you have not seen.";
        const string ChamberLine = "Seven Books. Three locks each. None has opened in a very long time.\nChoose the first, and give it your Key.";
        bool ReducedMotion => Dial.Lesson.Dial.ReducedMotion;

        void Awake()
        {
            gameObject.name = "VerticalSlice";
            var dialObject = new GameObject("CelestialDial"); dialObject.transform.SetParent(transform, false);
            Dial = dialObject.AddComponent<DialView>();
            Dial.Slice = this; Dial.ExtraActions = WebAction; font = Dial.UiFont;
            Flow.Logged += name => Dial.Lesson.Dial.Log(name.ToLowerInvariant().Replace(':', '_'), false, false, "slice");
            var canvasObject = new GameObject("Slice Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1;
            root = Rect("Slice Portrait", canvasObject.transform, 0, 0, 360, 800);
            BuildIdentity(); BuildBirth(); atrium = BuildAtrium("Atrium", AtriumLine, out atriumText, () => Continue());
            atriumReturn = BuildAtrium("Atrium return", ReturnLine, out returnText, () => Continue());
            BuildChamber(); BuildWingExtras();
            var flashObject = new GameObject("White light", typeof(RectTransform), typeof(Canvas));
            flashObject.transform.SetParent(transform, false);
            flashCanvas = flashObject.GetComponent<Canvas>(); flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay; flashCanvas.sortingOrder = 10;
            flash = flashObject.AddComponent<Image>(); flash.color = new Color(1, 1, 1, 0); flash.raycastTarget = false;
            Show(); Publish();
        }
        void Update()
        {
            if (canvas.pixelRect.width >= 1) canvas.scaleFactor = Mathf.Min(canvas.pixelRect.width / 360f, canvas.pixelRect.height / 800f);
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.KeyEarned && !revealStarted && !Dial.Busy) StartCoroutine(Reveal());
        }

        // ---- screens ----
        void BuildIdentity()
        {
            identity = ScreenPanel("Identity");
            Label(identity, "WHO ARE YOU?", 0, 200, 320, 34, 22);
            FaintRing(identity, new Vector2(0, -420), 110, .18f);
            var box = Rect("Name box", identity, 0, 300, 260, 48); box.gameObject.AddComponent<Image>().color = Dim;
            var text = Label(box, "", 0, 24, 244, 40, 16); text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            var placeholder = Label(box, "Your name", 0, 24, 244, 40, 16); placeholder.alignment = TextAnchor.MiddleLeft; placeholder.color = Muted; placeholder.fontStyle = FontStyle.Italic;
            nameField = box.gameObject.AddComponent<InputField>(); nameField.textComponent = text; nameField.placeholder = placeholder; nameField.characterLimit = 24;
            nameField.onEndEdit.AddListener(v => { Flow.SetName(v); Publish(); });
            Dial.RegisterNavigation(nameField);
#if UNITY_WEBGL && !UNITY_EDITOR
            nameField.interactable = false; // On the Web the HTML input over this box carries the name.
#endif
            Label(identity, "Placeholder. No account, nothing is saved.", 0, 340, 320, 20, 12).color = Muted;
            MakeButton(identity, "Continue", 0, 654, 190, 56, () => Continue());
        }
        void BuildBirth()
        {
            birth = ScreenPanel("Birth");
            Label(birth, "Do you know when you were born?", 0, 200, 330, 30, 18);
            string[] labels = { "Enter birth date, time, place", "Enter what I already know", "I don't know" };
            string[] choices = { "chart", "known", "unknown" };
            for (int i = 0; i < 3; i++) { string choice = choices[i]; birthOptions[i] = MakeButton(birth, labels[i], 0, 300 + i * 64, 300, 52, () => ChooseBirth(choice)); }
            birthNote = Label(birth, "", 0, 520, 320, 60, 13); birthNote.color = Muted;
            birthContinue = MakeButton(birth, "Continue", 0, 654, 190, 56, () => Continue());
        }
        RectTransform BuildAtrium(string name, string line, out Text caspar, UnityEngine.Events.UnityAction next)
        {
            var screen = ScreenPanel(name);
            Label(screen, "THE GRAND ATRIUM", 0, 32, 340, 24, 18);
            Block(screen, "Shelves, mostly empty", -130, 260, 60, 200);
            Block(screen, "Covered furniture", 20, 330, 120, 70);
            var cloth = Rect("Dust cloth", screen, 20, 318, 128, 30); cloth.gameObject.AddComponent<Image>().color = new Color(.3f, .3f, .32f);
            Block(screen, "Sealed door", 135, 250, 50, 150);
            var candle = Rect("Candle", screen, -60, 210, 6, 18); candle.gameObject.AddComponent<Image>().color = new Color(.5f, .42f, .3f);
            Label(screen, "Dust. Covered furniture. Sealed doors. One weak candle.", 0, 450, 330, 20, 12).color = Muted;
            var panel = Rect("Caspar panel", screen, 0, 526, 324, 128); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 18, 290, 22, 13);
            caspar = Label(panel, line, 0, 74, 306, 96, 13);
            MakeButton(screen, "Continue", 0, 654, 190, 56, next);
            return screen;
        }
        void BuildChamber()
        {
            chamber = ScreenPanel("Chamber");
            Label(chamber, "THE CRYSTAL BOOK CHAMBER", 0, 32, 340, 24, 18);
            for (int i = 0; i < candles.Length; i++)
            { var c = Rect("Candle", chamber, -120 + i * 30, 100, 8, 20); candles[i] = c.gameObject.AddComponent<Image>(); candles[i].color = new Color(.3f, .27f, .24f); candles[i].raycastTarget = false; }
            Label(chamber, "The chandelier, dark", 0, 128, 300, 18, 11).color = Muted;
            mechanism = Rect("Mechanism", chamber, 0, 200, 120, 120);
            RingLines(mechanism, 50, new Color(.62f, .57f, .53f, .5f), null);
            var tick = Rect("Mechanism tick", mechanism, 0, 60 - 50, 3, 14); tick.gameObject.AddComponent<Image>().color = Bone;
            for (int b = 0; b < SliceFlow.Books; b++)
            {
                float x = -138 + b * 46;
                var book = Rect("Book " + (b + 1), chamber, x, 340, 34, 70); book.gameObject.AddComponent<Image>().color = Dim;
                book.gameObject.AddComponent<Outline>().effectColor = new Color(.35f, .35f, .38f);
                for (int l = 0; l < SliceFlow.LocksPerBook; l++)
                { var dot = Rect("Lock", chamber, x - 10 + l * 10, 388, 7, 7); locks[b * 3 + l] = dot.gameObject.AddComponent<Image>(); locks[b * 3 + l].color = new Color(.3f, .3f, .33f); }
            }
            Label(chamber, "Seven sealed Books, three locks each", 0, 416, 330, 20, 12).color = Muted;
            var panel = Rect("Caspar panel", chamber, 0, 526, 324, 128); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 18, 290, 22, 13);
            chamberText = Label(panel, ChamberLine, 0, 74, 306, 96, 13);
            chamberEnd = Label(chamber, "End of prototype.", 0, 608, 330, 28, 14); chamberEnd.gameObject.SetActive(false);
            insert = MakeButton(chamber, "Insert the Key", 0, 654, 190, 56, Insert); insert.GetComponent<Image>().color = Crimson;
            restart = MakeButton(chamber, "Start over (test only)", 0, 768, 216, 48, Restart); restart.GetComponent<Image>().color = PanelColor; restart.gameObject.SetActive(false);
        }
        void BuildWingExtras()
        {
            var r = Dial.Root;
            // Q07 props: static placeholders, drawn behind the ring, never raycast targets.
            var floor = Rect("Floor markings", r, 0, 270, 320, 320); floor.SetAsFirstSibling();
            RingLines(floor, 150, new Color(.62f, .57f, .53f, .2f), floorLines);
            var shelf = Block(r, "Collapsed bookshelf", -125, 90, 50, 36); shelf.SetAsFirstSibling();
            for (int i = 0; i < 3; i++) { var book = Rect("Book", shelf, -14 + i * 14, 18, 8, 24); book.gameObject.AddComponent<Image>().color = new Color(.3f, .28f, .3f); book.localRotation = Quaternion.Euler(0, 0, i * 9 - 9); }
            var chair = Block(r, "Covered chair", 125, 90, 44, 36); chair.SetAsFirstSibling();
            var cloth = Rect("Dust cloth", chair, 0, 10, 48, 14); cloth.gameObject.AddComponent<Image>().color = new Color(.3f, .3f, .32f);
            var c = Rect("Candle", r, -160, 445, 6, 18); c.SetAsFirstSibling(); candle = c.gameObject.AddComponent<Image>(); candle.color = new Color(.3f, .27f, .24f); candle.raycastTarget = false;
            var s = Rect("Seam", r, 0, 270, 332, 2); seam = s.gameObject.AddComponent<Image>(); seam.color = new Color(Bone.r, Bone.g, Bone.b, 0); seam.raycastTarget = false;
            var glow = Rect("Key glow", r, 0, 270, 140, 140); keyGlow = glow.gameObject.AddComponent<Image>(); keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); keyGlow.raycastTarget = false;
            keyRect = Rect("Keeper Key", r, 0, 270, 84, 40); var keyImage = keyRect.gameObject.AddComponent<Image>(); keyImage.color = new Color(Bone.r, Bone.g, Bone.b, 0); keyImage.raycastTarget = false;
            keyLabel = Label(keyRect, "KEEPER KEY", 0, 20, 80, 36, 12); keyLabel.color = new Color(Charcoal.r, Charcoal.g, Charcoal.b, 0);
            keyIndicator = Label(r, "Keeper Key: 1", 110, 92, 140, 20, 12); keyIndicator.alignment = TextAnchor.MiddleRight; keyIndicator.gameObject.SetActive(false);
            wingContinue = MakeButton(r, "Continue", 0, 654, 190, 56, () => Continue()); wingContinue.name = "Slice Continue"; wingContinue.gameObject.SetActive(false);
        }

        // ---- actions (all input paths, including the Web bridge, arrive here) ----
        public void WebAction(string command)
        {
            if (command == "next-screen") Continue();
            else if (command.StartsWith("birth:")) ChooseBirth(command.Substring(6));
            else if (command.StartsWith("name:")) { Flow.SetName(command.Substring(5)); if (nameField != null) nameField.text = Flow.PlayerName; Publish(); }
            else if (command == "insert") Insert();
            else if (command == "restart") Restart();
        }
        void ChooseBirth(string choice)
        {
            if (busy) return;
            Flow.ChooseBirth(choice); birthNote.text = Flow.Note; birthContinue.interactable = Flow.CanContinue; Publish();
        }
        public void Continue()
        {
            if (busy) return;
            var from = Flow.Screen;
            if (!Flow.Continue()) return;
            if (from == SliceScreen.Birth) StartCoroutine(WhiteLight()); else { Show(); Publish(); }
        }
        void Insert() { if (busy || !Flow.InsertKey()) return; StartCoroutine(Chandelier()); }
        void Restart() { if (busy) return; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }

        void Show()
        {
            var s = Flow.Screen;
            identity.gameObject.SetActive(s == SliceScreen.Identity); birth.gameObject.SetActive(s == SliceScreen.Birth);
            atrium.gameObject.SetActive(s == SliceScreen.Atrium); atriumReturn.gameObject.SetActive(s == SliceScreen.AtriumReturn);
            chamber.gameObject.SetActive(s == SliceScreen.Chamber);
            Dial.UiCanvas.gameObject.SetActive(s == SliceScreen.Wing);
            if (birthContinue != null) birthContinue.interactable = Flow.CanContinue;
            if (s == SliceScreen.Wing) Dial.ForceRefresh();
        }
        public void Publish() => Dial.Publish();
        public void Fill(DialView.WebState state)
        {
            var s = Flow.Screen;
            state.screen = s.ToString().ToLowerInvariant(); state.playerName = Flow.DisplayName; state.note = Flow.Note;
            state.caspar = s == SliceScreen.Identity ? "Who are you? Enter a name, then continue." :
                s == SliceScreen.Birth ? "Do you know when you were born?" :
                s == SliceScreen.Atrium ? atriumText.text : s == SliceScreen.AtriumReturn ? returnText.text :
                s == SliceScreen.Chamber ? chamberText.text + (Flow.Ended ? " End of prototype." : "") : "";
            state.keyRevealed = Flow.KeyRevealed; state.keyInserted = Flow.KeyInserted; state.ended = Flow.Ended; state.locksFilled = Flow.LocksFilled;
            state.canInsert = s == SliceScreen.Chamber && !Flow.KeyInserted && !busy;
            state.canSliceContinue = Flow.CanContinue && !busy;
            state.canName = s == SliceScreen.Identity; state.canBirth = s == SliceScreen.Birth && !busy;
        }

        // ---- beats ----
        IEnumerator WhiteLight()
        {
            busy = true; Publish();
            if (!ReducedMotion) yield return Fade(flash, 0, 1, .35f);
            else flash.color = Color.white;
            Show();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .15f : .25f);
            if (!ReducedMotion) yield return Fade(flash, 1, 0, .45f);
            flash.color = new Color(1, 1, 1, 0);
            busy = false; Publish();
        }
        IEnumerator Reveal()
        {
            // Q04 Beat 1. The locked curriculum line is already on the panel ("Two of four. The rest will wait for you.").
            revealStarted = true; busy = true; Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .3f : 1.2f);
            float t = ReducedMotion ? 0 : .25f;
            yield return Tween(t, k => { seam.color = new Color(Bone.r, Bone.g, Bone.b, k); Dial.Ring.localScale = Vector3.one * (1 + .03f * k); });
            var keyImage = keyRect.GetComponent<Image>();
            yield return Tween(ReducedMotion ? 0 : .7f, k => {
                keyRect.anchoredPosition = new Vector2(0, -270 + 100 * k);
                keyImage.color = new Color(Bone.r, Bone.g, Bone.b, k); keyLabel.color = new Color(Charcoal.r, Charcoal.g, Charcoal.b, k);
                keyGlow.rectTransform.anchoredPosition = keyRect.anchoredPosition; keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, .35f * k);
            });
            yield return new WaitForSecondsRealtime(ReducedMotion ? .1f : .4f);
            yield return Tween(ReducedMotion ? 0 : .5f, k => keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, .35f - .23f * k));
            Dial.Lesson.Say("Aah... the Library stirs."); // Q04 locked line.
            Dial.ForceRefresh();
            foreach (var line in floorLines) if (line != null) line.color = new Color(.62f, .57f, .53f, .55f);
            candle.color = Bone; keyIndicator.gameObject.SetActive(true);
            Flow.RevealKey(); wingContinue.gameObject.SetActive(true);
            busy = false; Publish();
        }
        IEnumerator Chandelier()
        {
            // Q04 Beat 2, the locked ending.
            busy = true; Publish();
            locks[0].color = Bone; chamberText.text = "Something inside the crystal moves.";
            yield return new WaitForSecondsRealtime(ReducedMotion ? .3f : .6f);
            if (!ReducedMotion)
            {
                Candles(true); yield return new WaitForSecondsRealtime(.08f); Candles(false); yield return new WaitForSecondsRealtime(.12f);
                Candles(true); yield return new WaitForSecondsRealtime(.08f); Candles(false); yield return new WaitForSecondsRealtime(.16f);
                for (int i = 0; i < candles.Length; i++) { candles[i].color = Bone; yield return new WaitForSecondsRealtime(.06f); }
            }
            else Candles(true);
            chamberText.text = "The chandelier flickers, then every candle lights.\nDust shakes from the ceiling. The old mechanism turns one degree.";
            yield return Tween(ReducedMotion ? 0 : .4f, k => mechanism.localRotation = Quaternion.Euler(0, 0, -1f * k));
            yield return new WaitForSecondsRealtime(ReducedMotion ? .3f : .9f);
            chamberText.text = "Caspar looks upward. His composure cracks.\n\"So he was right.\""; // Locked Opening Sequence line.
            yield return new WaitForSecondsRealtime(ReducedMotion ? .3f : .7f);
            Flow.End(); chamberEnd.gameObject.SetActive(true); insert.gameObject.SetActive(false); restart.gameObject.SetActive(true);
            busy = false; Publish();
        }
        void Candles(bool on) { foreach (var c in candles) c.color = on ? Bone : new Color(.3f, .27f, .24f); }
        IEnumerator Tween(float seconds, Action<float> apply)
        {
            if (seconds <= 0) { apply(1); yield break; }
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < seconds) { apply(Mathf.Clamp01((Time.unscaledTime - start) / seconds)); yield return null; }
            apply(1);
        }
        IEnumerator Fade(Image image, float from, float to, float seconds) => Tween(seconds, k => image.color = new Color(1, 1, 1, Mathf.Lerp(from, to, k)));

        // ---- placeholder geometry helpers (same reference layout as the Dial: 360 x 800, top-anchored) ----
        RectTransform ScreenPanel(string name) { var s = Rect(name, root, 0, 400, 360, 800); s.gameObject.AddComponent<Image>().color = Charcoal; return s; }
        RectTransform Block(Transform parent, string name, float x, float top, float width, float height)
        {
            var r = Rect(name, parent, x, top, width, height); var image = r.gameObject.AddComponent<Image>(); image.color = PanelColor; image.raycastTarget = false;
            var label = Label(r, name, 0, height + 10, Mathf.Max(width, 110), 16, 10); label.color = Muted; label.horizontalOverflow = HorizontalWrapMode.Overflow; return r;
        }
        void FaintRing(Transform parent, Vector2 center, float radius, float alpha)
        { var ring = Rect("Faint chart wheel", parent, center.x, -center.y, radius * 2, radius * 2); RingLines(ring, radius, new Color(Bone.r, Bone.g, Bone.b, alpha), null); }
        void RingLines(RectTransform parent, float radius, Color color, Image[] store)
        {
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2 / 24, b = (i + 1) * Mathf.PI * 2 / 24;
                Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, q = new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius;
                var line = new GameObject("Line", typeof(RectTransform), typeof(Image)); line.transform.SetParent(parent, false);
                var rt = (RectTransform)line.transform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
                rt.anchoredPosition = (p + q) / 2; rt.sizeDelta = new Vector2(Vector2.Distance(p, q), 2);
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2((q - p).y, (q - p).x) * Mathf.Rad2Deg);
                var image = line.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
                if (store != null) store[i] = image;
            }
        }
        RectTransform Rect(string name, Transform parent, float x, float top, float width, float height)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(.5f, 1); r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = new Vector2(x, -top); r.sizeDelta = new Vector2(width, height);
            if (parent.GetComponent<Canvas>() != null) { r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = Vector2.zero; }
            return r;
        }
        Text Label(Transform parent, string text, float x, float top, float width, float height, int size)
        {
            var label = Rect(text, parent, x, top, width, height).gameObject.AddComponent<Text>(); label.font = font; label.text = text;
            label.fontSize = size; label.color = Bone; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }
        Button MakeButton(Transform parent, string text, float x, float top, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var r = Rect(text, parent, x, top, width, height); r.gameObject.AddComponent<Image>().color = Dim;
            var button = r.gameObject.AddComponent<Button>(); button.onClick.AddListener(action);
            var colors = button.colors; colors.selectedColor = new Color(.85f, .7f, .55f); colors.highlightedColor = Color.white; button.colors = colors;
            Label(r, text, 0, height / 2, width - 4, height - 4, 15); Dial.RegisterNavigation(button); return button;
        }
    }
}
