using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Vertical slice v0.1: the locked opening and Chamber bookends around the Dial (Q01, Q04, Q06, Q07),
    // with the September 11 copy session applied. Copy is the v0.1 Copy Deck; placeholder art only.
    public sealed class SliceView : MonoBehaviour, DialView.ISliceState
    {
        public SliceFlow Flow { get; private set; } = new SliceFlow();
        public DialView Dial { get; private set; }
        public bool Busy => busy;
        public int Page { get; private set; }
        Canvas canvas, flashCanvas; RectTransform root; Font font;
        RectTransform identity, birth, atrium, atriumReturn, chamber, birthChoices, birthDate, birthSigns;
        Text birthNote, atriumText, returnText, chamberText, chamberEnd, keyIndicator, keyLabel, dateHint;
        InputField nameField, dateField;
        Button birthContinue, wingContinue, insert, restart, atriumContinue, returnContinue, changeChoice;
        Image flash, seam, keyGlow, candle, insertGlow;
        readonly Image[] floorLines = new Image[24];
        readonly Image[] candles = new Image[9];
        readonly Image[] locks = new Image[SliceFlow.Books * SliceFlow.LocksPerBook];
        RectTransform keyRect, mechanism;
        bool busy, revealStarted, sunSent;
        static readonly Color Bone = new Color(.94f,.91f,.86f), Charcoal = new Color(.075f,.075f,.09f), Crimson = new Color(.46f,.09f,.15f);
        static readonly Color PanelColor = new Color(.13f,.13f,.15f), Dim = new Color(.21f,.21f,.24f), Muted = new Color(.62f,.57f,.53f);
        // v0.1 Copy Deck, sections 3, 5, 6. Paged so each page fits the panel; Continue turns the page.
        public static readonly string[] AtriumPages = {
            "You are awake. Good. I hope the trip was not too rough. You were... let us say 'unavailable' for most of it.",
            "I am Caspar. I have lived here for centuries, keeping what remains of what your ancestor built. It has been a long time since one of his blood stepped through these halls.\nThank you for coming. Truly.",
            "I will not waste your time with a long tour of empty rooms. I must be honest with you. I have done what I can, but I am only a caretaker. The Library does not answer to me. It answers to a Keeper.",
            "The rooms are sealed. The lights have gone out. I could not stop it.\nBut you carry his blood, and that changes things. I need your help to wake this place. Come, I will show you where to begin." };
        public static readonly string[] ReturnPages = {
            "A Keeper Key.\nI spent centuries wondering if another one would ever surface. Now here it is, in your hands.",
            "Follow me. There is a door that has been locked since your ancestor left. Let us find out if it too will respond to you." };
        public static readonly string[] ChamberPages = {
            "This is the Crystal Chamber. These are the Crystal Books. Seven in all.\nThey are connected to the Library the way a heart is connected to a body. They are what give these halls life, what pushes knowledge through every room, every shelf, every door.",
            "Before your ancestor left, he sealed them. All seven. He knew that the power inside these Books, paired with the knowledge this Library holds, could unbalance the world in the wrong hands.",
            "So he made sure the next Keeper would have to learn the language of the stars before they could open even one. That is why the wheel tested you first.\nChoose a Book. Feed it your Key." };
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
            BuildIdentity(); BuildBirth();
            atrium = BuildAtrium("Atrium", out atriumText, out atriumContinue);
            atriumReturn = BuildAtrium("Atrium return", out returnText, out returnContinue);
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
            if (insertGlow != null && insert.gameObject.activeInHierarchy && insert.interactable && !Flow.KeyInserted)
            {
                // First time spending an earned Key: the button should read as a reward action (Sept 11 UI note).
                float a = ReducedMotion ? .35f : .15f + .3f * Mathf.PingPong(Time.unscaledTime / 1.2f, 1f);
                insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, a);
            }
        }

        // ---- screens ----
        void BuildIdentity()
        {
            identity = ScreenPanel("Identity");
            Label(identity, "WHO ARE YOU?", 0, 200, 320, 34, 22);
            FaintRing(identity, new Vector2(0, -420), 110, .18f);
            nameField = TextBox(identity, "Name box", 0, 300, 260, 48, "Your name", v => { Flow.SetName(v); Publish(); });
            Label(identity, "Placeholder. No account, nothing is saved.", 0, 340, 320, 20, 12).color = Muted;
            MakeButton(identity, "Continue", 0, 654, 190, 56, () => Continue());
        }
        void BuildBirth()
        {
            birth = ScreenPanel("Birth");
            Label(birth, "Do you know when you were born?", 0, 200, 330, 30, 18);
            birthChoices = Rect("Choices", birth, 0, 400, 360, 220);
            string[] labels = { "Enter birth date, time, place", "Enter what I already know", "I don't know" };
            string[] choices = { "chart", "known", "unknown" };
            for (int i = 0; i < 3; i++) { string choice = choices[i]; MakeButton(birthChoices, labels[i], 0, 10 + i * 64, 300, 52, () => ChooseBirth(choice)); }
            // Choice 1: a date. Only the sun sign is derived in v0.1; time and place are not asked.
            birthDate = Rect("Date entry", birth, 0, 400, 360, 220);
            Label(birthDate, "Your birth date, month and day", 0, 0, 320, 22, 14);
            dateField = TextBox(birthDate, "Date box", 0, 48, 200, 48, "MM/DD", v => UseDate(v));
            dateHint = Label(birthDate, "Like 04/25. Time and place are not needed yet.", 0, 88, 320, 20, 12); dateHint.color = Muted;
            MakeButton(birthDate, "Use this date", 0, 130, 200, 52, () => UseDate(dateField.text));
            // Choice 2: the player already knows the sign.
            birthSigns = Rect("Sign choices", birth, 0, 400, 360, 240);
            for (int i = 0; i < 12; i++) { int seat = i; MakeButton(birthSigns, Zodiac.Seats[i].Name, -110 + (i % 3) * 110, 26 + (i / 3) * 58, 100, 52, () => { Flow.SetKnownSign(seat); AfterBirthEntry(); }); }
            changeChoice = MakeButton(birth, "Change my answer", 0, 250, 200, 48, () => ChooseBirth("")); changeChoice.GetComponent<Image>().color = PanelColor;
            birthNote = Label(birth, "", 0, 545, 320, 60, 14);
            birthContinue = MakeButton(birth, "Continue", 0, 654, 190, 56, () => Continue());
            ShowBirth();
        }
        RectTransform BuildAtrium(string name, out Text caspar, out Button next)
        {
            var screen = ScreenPanel(name);
            Label(screen, "THE GRAND ATRIUM", 0, 32, 340, 24, 18);
            Block(screen, "Shelves, mostly empty", -130, 240, 60, 180);
            Block(screen, "Covered furniture", 20, 300, 120, 70);
            var cloth = Rect("Dust cloth", screen, 20, 288, 128, 30); cloth.gameObject.AddComponent<Image>().color = new Color(.3f, .3f, .32f);
            Block(screen, "Sealed door", 135, 230, 50, 150);
            var candle = Rect("Candle", screen, -60, 200, 6, 18); candle.gameObject.AddComponent<Image>().color = new Color(.5f, .42f, .3f);
            Label(screen, "Dust. Covered furniture. Sealed doors. One weak candle.", 0, 405, 330, 20, 12).color = Muted;
            var panel = Rect("Caspar panel", screen, 0, 520, 324, 170); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 16, 290, 22, 13);
            caspar = Label(panel, "", 0, 96, 306, 136, 13);
            next = MakeButton(screen, "Continue", 0, 654, 190, 56, () => Continue());
            return screen;
        }
        void BuildChamber()
        {
            chamber = ScreenPanel("Chamber");
            Label(chamber, "THE CRYSTAL BOOK CHAMBER", 0, 32, 340, 24, 18);
            for (int i = 0; i < candles.Length; i++)
            { var c = Rect("Candle", chamber, -120 + i * 30, 90, 8, 20); candles[i] = c.gameObject.AddComponent<Image>(); candles[i].color = new Color(.3f, .27f, .24f); candles[i].raycastTarget = false; }
            Label(chamber, "The chandelier, dark", 0, 116, 300, 18, 11).color = Muted;
            mechanism = Rect("Mechanism", chamber, 0, 180, 110, 110);
            RingLines(mechanism, 46, new Color(.62f, .57f, .53f, .5f), null);
            var tick = Rect("Mechanism tick", mechanism, 0, 55 - 46, 3, 14); tick.gameObject.AddComponent<Image>().color = Bone;
            for (int b = 0; b < SliceFlow.Books; b++)
            {
                float x = -138 + b * 46;
                var book = Rect("Book " + (b + 1), chamber, x, 300, 34, 70); book.gameObject.AddComponent<Image>().color = Dim;
                book.gameObject.AddComponent<Outline>().effectColor = new Color(.35f, .35f, .38f);
                for (int l = 0; l < SliceFlow.LocksPerBook; l++)
                { var dot = Rect("Lock", chamber, x - 10 + l * 10, 348, 7, 7); locks[b * 3 + l] = dot.gameObject.AddComponent<Image>(); locks[b * 3 + l].color = new Color(.3f, .3f, .33f); }
            }
            Label(chamber, "Seven sealed Books, three locks each", 0, 376, 330, 20, 12).color = Muted;
            var panel = Rect("Caspar panel", chamber, 0, 520, 324, 170); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 16, 290, 22, 13);
            chamberText = Label(panel, "", 0, 96, 306, 136, 13);
            chamberEnd = Label(chamber, "End of prototype.", 0, 720, 330, 28, 14); chamberEnd.gameObject.SetActive(false);
            var glow = Rect("Insert glow", chamber, 0, 654, 214, 80); insertGlow = glow.gameObject.AddComponent<Image>(); insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); insertGlow.raycastTarget = false;
            insert = MakeButton(chamber, "Insert the Key", 0, 654, 190, 56, Insert); insert.GetComponent<Image>().color = Crimson;
            restart = MakeButton(chamber, "Start over (test only)", 0, 768, 216, 48, Restart); restart.GetComponent<Image>().color = PanelColor; restart.gameObject.SetActive(false);
        }
        void BuildWingExtras()
        {
            var r = Dial.Root;
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
            else if (command.StartsWith("birthdate:")) UseDate(command.Substring(10));
            else if (command.StartsWith("sign:") && int.TryParse(command.Substring(5), out int seat)) { if (Flow.SetKnownSign(seat)) AfterBirthEntry(); }
            else if (command.StartsWith("name:")) { Flow.SetName(command.Substring(5)); if (nameField != null) nameField.text = Flow.PlayerName; Publish(); }
            else if (command == "insert") Insert();
            else if (command == "restart") Restart();
        }
        void ChooseBirth(string choice)
        {
            if (busy) return;
            Flow.ChooseBirth(choice); AfterBirthEntry();
        }
        void UseDate(string text)
        {
            // Accepts MM/DD, MM-DD, or YYYY-MM-DD (the Web date input).
            var parts = (text ?? "").Trim().Split('/', '-', '.');
            int month = 0, day = 0; bool ok = false;
            if (parts.Length == 3 && int.TryParse(parts[1], out month) && int.TryParse(parts[2], out day)) ok = true;
            else if (parts.Length == 2 && int.TryParse(parts[0], out month) && int.TryParse(parts[1], out day)) ok = true;
            if (!ok || !Flow.SetBirthDate(month, day)) { birthNote.text = "That is not a date I know. Try month and day, like 04/25."; Publish(); return; }
            AfterBirthEntry();
        }
        void AfterBirthEntry() { ShowBirth(); Publish(); }
        void ShowBirth()
        {
            bool chosen = Flow.BirthChoice != "";
            birthChoices.gameObject.SetActive(!chosen);
            birthDate.gameObject.SetActive(Flow.BirthChoice == "chart" && !Flow.HasSunSign);
            birthSigns.gameObject.SetActive(Flow.BirthChoice == "known" && !Flow.HasSunSign);
            changeChoice.gameObject.SetActive(chosen);
            birthNote.text = Flow.Note; birthNote.color = Bone;
            birthContinue.interactable = Flow.CanContinue;
        }
        public void Continue()
        {
            if (busy) return;
            var s = Flow.Screen;
            // Paged monologues: Continue turns the page first, then leaves the screen.
            if (s == SliceScreen.Atrium && Page < AtriumPages.Length - 1) { Page++; ShowPage(); Publish(); return; }
            if (s == SliceScreen.AtriumReturn && Page < ReturnPages.Length - 1) { Page++; ShowPage(); Publish(); return; }
            if (s == SliceScreen.Chamber) { if (Page < ChamberPages.Length - 1) { Page++; ShowPage(); Publish(); } return; }
            var from = Flow.Screen;
            if (!Flow.Continue()) return;
            Page = 0;
            if (from == SliceScreen.Birth) StartCoroutine(WhiteLight()); else { Show(); Publish(); }
        }
        void Insert() { if (busy || Page < ChamberPages.Length - 1 || !Flow.InsertKey()) return; StartCoroutine(Chandelier()); }
        void Restart() { if (busy) return; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }

        void Show()
        {
            var s = Flow.Screen;
            identity.gameObject.SetActive(s == SliceScreen.Identity); birth.gameObject.SetActive(s == SliceScreen.Birth);
            atrium.gameObject.SetActive(s == SliceScreen.Atrium); atriumReturn.gameObject.SetActive(s == SliceScreen.AtriumReturn);
            chamber.gameObject.SetActive(s == SliceScreen.Chamber);
            Dial.UiCanvas.gameObject.SetActive(s == SliceScreen.Wing);
            if (birthContinue != null) birthContinue.interactable = Flow.CanContinue;
            if (s == SliceScreen.Wing)
            {
                if (!sunSent && Flow.HasSunSign) { Dial.Lesson.SetSunSign(Flow.SunSign); sunSent = true; }
                Dial.ForceRefresh();
            }
            ShowPage();
        }
        void ShowPage()
        {
            atriumText.text = AtriumPages[Mathf.Min(Page, AtriumPages.Length - 1)];
            returnText.text = ReturnPages[Mathf.Min(Page, ReturnPages.Length - 1)];
            if (!Flow.KeyInserted) chamberText.text = ChamberPages[Mathf.Min(Page, ChamberPages.Length - 1)];
            bool chamberReady = Flow.Screen == SliceScreen.Chamber && Page >= ChamberPages.Length - 1;
            insert.gameObject.SetActive(Flow.Screen == SliceScreen.Chamber && !Flow.KeyInserted);
            insert.interactable = chamberReady && !busy;
            if (!chamberReady && insertGlow != null) insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0);
        }
        public void Publish() => Dial.Publish();
        public void Fill(DialView.WebState state)
        {
            var s = Flow.Screen;
            state.screen = s.ToString().ToLowerInvariant(); state.playerName = Flow.DisplayName; state.note = Flow.Note;
            state.caspar = s == SliceScreen.Identity ? "Who are you? Enter a name, then continue." :
                s == SliceScreen.Birth ? "Do you know when you were born?" + (Flow.BirthChoice == "chart" && !Flow.HasSunSign ? " Enter your birth month and day." : Flow.BirthChoice == "known" && !Flow.HasSunSign ? " Choose your sign." : "") :
                s == SliceScreen.Atrium ? atriumText.text : s == SliceScreen.AtriumReturn ? returnText.text :
                s == SliceScreen.Chamber ? chamberText.text + (Flow.Ended ? " End of prototype." : "") : "";
            state.keyRevealed = Flow.KeyRevealed; state.keyInserted = Flow.KeyInserted; state.ended = Flow.Ended; state.locksFilled = Flow.LocksFilled;
            state.sunSign = Flow.HasSunSign ? Zodiac.Seats[Flow.SunSign].Name : "";
            state.canInsert = s == SliceScreen.Chamber && !Flow.KeyInserted && Page >= ChamberPages.Length - 1 && !busy;
            state.canSliceContinue = !busy && ((s == SliceScreen.Atrium && Page < AtriumPages.Length - 1) || (s == SliceScreen.AtriumReturn && Page < ReturnPages.Length - 1) || (s == SliceScreen.Chamber && Page < ChamberPages.Length - 1) || (s != SliceScreen.Chamber && Flow.CanContinue));
            state.canName = s == SliceScreen.Identity;
            state.canBirth = s == SliceScreen.Birth && !busy && Flow.BirthChoice == "";
            state.canBirthDate = s == SliceScreen.Birth && !busy && Flow.BirthChoice == "chart" && !Flow.HasSunSign;
            state.canSignPick = s == SliceScreen.Birth && !busy && Flow.BirthChoice == "known" && !Flow.HasSunSign;
            state.canChangeBirth = s == SliceScreen.Birth && !busy && Flow.BirthChoice != "";
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
            // Q04 Beat 1. The amended completion line is already on the panel.
            revealStarted = true; busy = true; Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .8f : 2.2f);
            float t = ReducedMotion ? 0 : .25f;
            yield return Tween(t, k => { seam.color = new Color(Bone.r, Bone.g, Bone.b, k); Dial.Ring.localScale = Vector3.one * (1 + .03f * k); });
            var keyImage = keyRect.GetComponent<Image>();
            yield return Tween(ReducedMotion ? 0 : .9f, k => {
                keyRect.anchoredPosition = new Vector2(0, -270 + 100 * k);
                keyImage.color = new Color(Bone.r, Bone.g, Bone.b, k); keyLabel.color = new Color(Charcoal.r, Charcoal.g, Charcoal.b, k);
                keyGlow.rectTransform.anchoredPosition = keyRect.anchoredPosition; keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, .35f * k);
            });
            yield return new WaitForSecondsRealtime(ReducedMotion ? .3f : .6f);
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
            // Q04 Beat 2 with the amended ending (Sept 11). Holds are generous: the owner asked for slower beats.
            busy = true; ShowPage(); insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); Publish();
            float hold = ReducedMotion ? 1.2f : 1f;
            locks[0].color = Bone; chamberText.text = "Something inside the crystal moves.";
            yield return new WaitForSecondsRealtime(hold * 1.6f);
            if (!ReducedMotion)
            {
                Candles(true); yield return new WaitForSecondsRealtime(.1f); Candles(false); yield return new WaitForSecondsRealtime(.18f);
                Candles(true); yield return new WaitForSecondsRealtime(.1f); Candles(false); yield return new WaitForSecondsRealtime(.25f);
                for (int i = 0; i < candles.Length; i++) { candles[i].color = Bone; yield return new WaitForSecondsRealtime(.09f); }
            }
            else Candles(true);
            chamberText.text = "The chandelier flickers, then every candle lights.\nDust shakes from the ceiling. The old mechanism turns one degree.";
            yield return Tween(ReducedMotion ? 0 : .6f, k => mechanism.localRotation = Quaternion.Euler(0, 0, -1f * k));
            yield return new WaitForSecondsRealtime(hold * 2f);
            chamberText.text = "Caspar presses his palm flat against the nearest pillar. His eyes close. The whole room hums faintly.";
            yield return new WaitForSecondsRealtime(hold * 2.2f);
            chamberText.text = "\"She breathes. After all this time... she breathes.\""; // Amended canon line (Sept 11).
            yield return new WaitForSecondsRealtime(hold * 2.4f);
            chamberText.text = "He opens his eyes and looks at you like he is seeing you for the first time.\n\"It is faint though. Let us continue, shall we?\"";
            yield return new WaitForSecondsRealtime(hold * 2f);
            Flow.End(); chamberEnd.gameObject.SetActive(true); insert.gameObject.SetActive(false); insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); restart.gameObject.SetActive(true);
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
        InputField TextBox(Transform parent, string name, float x, float top, float width, float height, string placeholderText, Action<string> onEndEdit)
        {
            var box = Rect(name, parent, x, top, width, height); box.gameObject.AddComponent<Image>().color = Dim;
            var text = Label(box, "", 0, height / 2, width - 16, height - 8, 16); text.alignment = TextAnchor.MiddleLeft;
            var placeholder = Label(box, placeholderText, 0, height / 2, width - 16, height - 8, 16); placeholder.alignment = TextAnchor.MiddleLeft; placeholder.color = Muted; placeholder.fontStyle = FontStyle.Italic;
            var field = box.gameObject.AddComponent<InputField>(); field.textComponent = text; field.placeholder = placeholder; field.characterLimit = 24;
            field.onEndEdit.AddListener(v => onEndEdit(v));
            Dial.RegisterNavigation(field);
#if UNITY_WEBGL && !UNITY_EDITOR
            field.interactable = false; // On the Web the HTML input over this box carries the text.
#endif
            return field;
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
