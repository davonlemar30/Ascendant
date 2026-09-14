using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Vertical slice v0.2 "the return": the locked opening and Chamber bookends (Q01, Q04, Q06, Q07), the September 11
    // copy session, and the locked loop (Q05): two-entrance Atrium, Check the Seals, Unit 1.1 continuation, local save.
    // Placeholder art. Copy is the v0.1 Copy Deck; v0.2 lines marked "placeholder (owner writes)" are not final.
    public sealed class SliceView : MonoBehaviour, DialView.ISliceState
    {
        public SliceFlow Flow { get; private set; } = new SliceFlow();
        public DialView Dial { get; private set; }
        public bool Busy => busy;
        public int Page { get; private set; }
        public bool Resumed { get; private set; }
        public bool ChamberPaging => chamberContinue.gameObject.activeSelf && !insert.gameObject.activeSelf; // fixture evidence
        public const string SaveKey = "ascendant.v02.save";
        Canvas canvas, flashCanvas; RectTransform root; Font font;
        RectTransform identity, birth, atrium, atriumReturn, chamber, hub, review, birthChoices, birthDate, birthSigns, glyphs;
        Text birthNote, atriumText, returnText, chamberText, chamberEnd, keyIndicator, keyLabel, dateHint;
        Text hubText, hubNote, hubCaption, endCard, reviewProgress, reviewQuestion, reviewNote, reviewSummary, reviewGlyph;
        Text glyphCard, glyphProgress, glyphCaspar, glyphNote;
        // v0.4 tap-to-move (Q06 phase 2): two walkable rooms, a placeholder marker, fades at doorways.
        RectTransform wingRoom, avatar, avatarHead; Image fadeImage; Text wingRoomCaption, walkSpeedLabel;
        Button enterDial, wingRoomBack, walkSpeed;
        const float BandY = 436f, FadeSeconds = .35f; // floor band and fade length are test variables (Q06 phase 2, decision 7)
        readonly Button[] glyphNameButtons = new Button[4];
        readonly Button[] reviewGlyphButtons = new Button[4];
        InputField nameField, dateField;
        Button birthContinue, wingContinue, insert, chamberContinue, atriumContinue, returnContinue, changeChoice;
        Button enterWing, enterSeals, hubRestart, leaveReview;
        readonly Button[] elementButtons = new Button[4];
        Image flash, seam, keyGlow, candle, insertGlow, lampOne, lampTwo, deskCloth, doorOpenLight;
        Text enterSealsLabel;
        readonly Image[] floorLines = new Image[24];
        readonly Image[] candles = new Image[9];
        readonly Image[] locks = new Image[SliceFlow.Books * SliceFlow.LocksPerBook];
        RectTransform keyRect, mechanism;
        bool busy, revealStarted, sunSent;
        static readonly Color Bone = new Color(.94f,.91f,.86f), Charcoal = new Color(.075f,.075f,.09f), Crimson = new Color(.46f,.09f,.15f);
        static readonly Color PanelColor = new Color(.13f,.13f,.15f), Dim = new Color(.21f,.21f,.24f), Muted = new Color(.62f,.57f,.53f);
        static readonly Color LampDark = new Color(.3f,.27f,.24f), LampLit = new Color(.95f,.8f,.5f);
        static readonly string[] Elements = { "Fire", "Earth", "Air", "Water" };
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
        // v0.2 placeholder lines (owner writes; Q05 decision 7).
        const string HubFirstLine = "Look at it. One lamp, and the dust already knows it.\nSix seats still dark in the Wing, when you are ready. And the seals: a Keeper checks them on every return. For now they hold.";
        const string HubLaterLine = "Welcome back. The Wing waits, and the seals are yours to check.";
        const string HubCompleteLine = "The whole wheel. I have not seen it lit since he left.\nRest now. The seals will want checking when you return, and there is more to wake.";
        const string HubKey2Line = "Two Keys. He left twenty-one locks, and you have opened the way to two of them.\nThat is enough for tonight. The seals will keep."; // placeholder (owner writes)
        bool ReducedMotion => Dial.Lesson.Dial.ReducedMotion;

        void Awake()
        {
            gameObject.name = "VerticalSlice";
            var dialObject = new GameObject("CelestialDial"); dialObject.transform.SetParent(transform, false);
            Dial = dialObject.AddComponent<DialView>();
            Dial.Slice = this; Dial.ExtraActions = WebAction; font = Dial.UiFont;
            Flow.Logged += name => Dial.Lesson.Dial.Log(name.ToLowerInvariant().Replace(':', '_'), false, false, "slice");
            Dial.Lesson.Dial.Logged += e => { if (e.event_name == "answer_correct" && Dial.Lesson.Phase != LessonPhase.Review) { Flow.RecordLessonAnswer(e.selected_destination, e.evidence_eligible); Save(); } };
            Dial.Lesson.ReviewFinished += (seat, correct, eligible) => StartCoroutine(AfterDialReview(correct, eligible));
            Dial.Lesson.GlyphNamedEvent += (seat, correct, eligible) => { Flow.RecordGlyphAnswer(seat, eligible); Flow.SetGlyphProgress(Dial.Lesson.Phase == LessonPhase.GlyphWheel ? 1 : 0, Dial.Lesson.GlyphIndex); Save(); };
            Dial.Lesson.Dial.Logged += e => { if (e.event_name == "glyph_placed") { Flow.RecordGlyphAnswer(e.selected_destination, e.evidence_eligible); Save(); } };
            var canvasObject = new GameObject("Slice Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1;
            root = Rect("Slice Portrait", canvasObject.transform, 0, 0, 360, 800);
            BuildIdentity(); BuildBirth();
            atrium = BuildAtrium("Atrium", out atriumText, out atriumContinue);
            atriumReturn = BuildAtrium("Atrium return", out returnText, out returnContinue);
            BuildChamber(); BuildHub(); BuildReview(); BuildGlyphs(); BuildWingExtras(); BuildWingRoom(); BuildAvatar(); BuildFade();
            var flashObject = new GameObject("White light", typeof(RectTransform), typeof(Canvas));
            flashObject.transform.SetParent(transform, false);
            flashCanvas = flashObject.GetComponent<Canvas>(); flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay; flashCanvas.sortingOrder = 10;
            flash = flashObject.AddComponent<Image>(); flash.color = new Color(1, 1, 1, 0); flash.raycastTarget = false;
            TryRestore();
            Show(); Publish();
        }
        void Update()
        {
            if (canvas.pixelRect.width >= 1) canvas.scaleFactor = Mathf.Min(canvas.pixelRect.width / 360f, canvas.pixelRect.height / 800f);
            if (avatar != null && avatar.gameObject.activeInHierarchy) PlaceAvatar();
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.KeyEarned && !revealStarted && !Dial.Busy) StartCoroutine(Reveal());
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.Phase == LessonPhase.AllLit && !Flow.WheelComplete) { Flow.MarkWheelComplete(); Save(); LightWing(); Show(); Publish(); }
            if ((Flow.Screen == SliceScreen.Wing || Flow.Screen == SliceScreen.Review) && Dial.Lesson.Key2Earned && Flow.Keys < 2) { Flow.MarkKey2(); keyIndicator.text = "Keeper Keys: 2"; Save(); Show(); Publish(); }
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.Phase == LessonPhase.GlyphNames && !glyphs.gameObject.activeSelf && !busy) { Show(); Publish(); }
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.Phase == LessonPhase.GlyphWheel && glyphs.gameObject.activeSelf && !busy) { Show(); Dial.Realign(); Publish(); }
            if (insertGlow != null && insert.gameObject.activeInHierarchy && insert.interactable && !Flow.KeyInserted)
            {
                float a = ReducedMotion ? .35f : .15f + .3f * Mathf.PingPong(Time.unscaledTime / 1.2f, 1f);
                insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, a);
            }
            if (wingContinue != null && Flow.Screen == SliceScreen.Wing && Flow.AtriumStage >= 2)
            {
                bool idle = !Dial.Lesson.Dial.Active && !Dial.Busy && !busy && Dial.Lesson.Phase != LessonPhase.Review;
                if (wingContinue.gameObject.activeSelf != idle) { wingContinue.gameObject.SetActive(idle); Publish(); }
            }
        }

        // ---- screens ----
        void BuildIdentity()
        {
            identity = ScreenPanel("Identity");
            Label(identity, "WHO ARE YOU?", 0, 200, 320, 34, 22);
            FaintRing(identity, new Vector2(0, -420), 110, .18f);
            nameField = TextBox(identity, "Name box", 0, 300, 260, 48, "Your name", v => { Flow.SetName(v); Publish(); });
            Label(identity, "Placeholder. No account. Progress is kept only on this device.", 0, 340, 330, 20, 12).color = Muted;
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
            birthDate = Rect("Date entry", birth, 0, 400, 360, 220);
            Label(birthDate, "Your birth date, month and day", 0, 0, 320, 22, 14);
            dateField = TextBox(birthDate, "Date box", 0, 48, 200, 48, "MM/DD", v => UseDate(v));
            dateHint = Label(birthDate, "Like 04/25. Time and place are not needed yet.", 0, 88, 320, 20, 12); dateHint.color = Muted;
            MakeButton(birthDate, "Use this date", 0, 130, 200, 52, () => UseDate(dateField.text));
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
            { var c = Rect("Candle", chamber, -120 + i * 30, 90, 8, 20); candles[i] = c.gameObject.AddComponent<Image>(); candles[i].color = LampDark; candles[i].raycastTarget = false; }
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
            chamberEnd = Label(chamber, "The first Key is spent. The Library has taken her first breath.", 0, 720, 330, 28, 12); chamberEnd.color = Muted; chamberEnd.gameObject.SetActive(false);
            var glow = Rect("Insert glow", chamber, 0, 654, 214, 80); insertGlow = glow.gameObject.AddComponent<Image>(); insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); insertGlow.raycastTarget = false;
            insert = MakeButton(chamber, "Insert the Key", 0, 654, 190, 56, Insert); insert.GetComponent<Image>().color = Crimson;
            chamberContinue = MakeButton(chamber, "Continue", 0, 654, 190, 56, () => Continue()); chamberContinue.gameObject.SetActive(false);
        }
        void BuildHub()
        {
            // Q05 decisions 1, 6: the Atrium in Stage 2 "Stirring" with two entrances.
            hub = ScreenPanel("Hub");
            Label(hub, "THE GRAND ATRIUM", 0, 32, 340, 24, 18);
            Block(hub, "Shelves, mostly empty", -130, 200, 60, 120);
            var floor = Rect("Floor band", hub, 0, BandY, 340, 30); var floorImage = floor.gameObject.AddComponent<Image>(); floorImage.color = new Color(.16f, .16f, .19f); floorImage.raycastTarget = false;
            var desk = Block(hub, "Desk, uncovered", -125, 426, 70, 30); Tappable(desk, () => Walk("desk"));
            var casparMark = Rect("Caspar", hub, 100, 418, 14, 40); var casparBody = casparMark.gameObject.AddComponent<Image>(); casparBody.color = Muted; casparBody.raycastTarget = false;
            var casparHead = Rect("Caspar head", hub, 100, 392, 12, 12); var casparHeadImage = casparHead.gameObject.AddComponent<Image>(); casparHeadImage.color = Muted; casparHeadImage.raycastTarget = false;
            Label(hub, "Caspar", 100, 452, 60, 14, 10).color = Muted;
            var casparTap = Rect("Caspar, tap to walk", hub, 100, 420, 44, 76); var casparTapImage = casparTap.gameObject.AddComponent<Image>(); casparTapImage.color = new Color(0, 0, 0, 0); Tappable(casparTap, () => Walk("caspar"));
            var lamp1 = Rect("Lamp", hub, -40, 150, 8, 22); lampOne = lamp1.gameObject.AddComponent<Image>(); lampOne.color = LampLit; lampOne.raycastTarget = false;
            var lamp2 = Rect("Lamp", hub, 60, 150, 8, 22); lampTwo = lamp2.gameObject.AddComponent<Image>(); lampTwo.color = LampDark; lampTwo.raycastTarget = false;
            string[] doors = { "Sealed", "Zodiac Wing, open", "Sealed" };
            for (int i = 0; i < 3; i++)
            {
                var door = Block(hub, doors[i], -120 + i * 120, 325, 70, 100); string poi = i == 0 ? "sealed-left" : i == 1 ? "wing-door" : "sealed-right"; Tappable(door, () => Walk(poi));
                if (i == 1) { var light = Rect("Doorway light", door, 0, 50, 50, 82); doorOpenLight = light.gameObject.AddComponent<Image>(); doorOpenLight.color = new Color(.95f, .8f, .5f, .35f); doorOpenLight.raycastTarget = false; }
                else { var lockRect = Rect("Lock", door, 0, 50, 12, 16); var li = lockRect.gameObject.AddComponent<Image>(); li.color = new Color(.45f, .45f, .5f); li.raycastTarget = false; }
            }
            hubCaption = Label(hub, "", 0, 470, 340, 20, 12); hubCaption.color = Muted;
            var panel = Rect("Caspar panel", hub, 0, 536, 324, 120); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 14, 290, 20, 13);
            hubText = Label(panel, "", 0, 70, 306, 90, 12);
            enterWing = MakeButton(hub, "The Zodiac Wing", 0, 624, 300, 52, EnterWing);
            enterSeals = MakeButton(hub, "Check the Seals", 0, 680, 300, 52, EnterSeals); enterSealsLabel = enterSeals.GetComponentInChildren<Text>();
            hubNote = Label(hub, "", 0, 718, 330, 20, 12); hubNote.color = Muted;
            endCard = Label(hub, "End of prototype v0.2. Glyphs and Key 2 come next.", 0, 738, 330, 20, 12); endCard.gameObject.SetActive(false);
            walkSpeed = TestButton(hub, "Walk: normal (test)", -60, 774, CycleWalkSpeed); walkSpeedLabel = walkSpeed.GetComponentInChildren<Text>();
            hubRestart = TestButton(hub, "Start over (test)", 60, 774, Restart);
        }
        void BuildReview()
        {
            // Q05 decision 2: direct-tap items live here; compressed Dial items use the Wing canvas.
            review = ScreenPanel("Review");
            Label(review, "CHECK THE SEALS", 0, 32, 340, 24, 18);
            reviewProgress = Label(review, "", 0, 62, 300, 20, 12); reviewProgress.color = Muted;
            reviewQuestion = Label(review, "", 0, 200, 330, 44, 18);
            for (int i = 0; i < 4; i++) { string element = Elements[i]; elementButtons[i] = MakeButton(review, element, -78 + (i % 2) * 156, 300 + (i / 2) * 64, 150, 56, () => AnswerTap(element)); }
            var glyphBox = Rect("Review glyph", review, 0, 150, 90, 90); reviewGlyph = Label(glyphBox, "", 0, 45, 90, 90, 60); reviewGlyph.font = Dial.GlyphFont; reviewGlyph.horizontalOverflow = HorizontalWrapMode.Overflow; reviewGlyph.verticalOverflow = VerticalWrapMode.Overflow; reviewGlyph.gameObject.SetActive(false);
            for (int i = 0; i < 4; i++) { int slot = i; reviewGlyphButtons[i] = MakeButton(review, "", -78 + (i % 2) * 156, 300 + (i / 2) * 64, 150, 56, () => AnswerGlyphReview(slot)); reviewGlyphButtons[i].gameObject.SetActive(false); }
            reviewNote = Label(review, "", 0, 440, 330, 50, 14);
            reviewSummary = Label(review, "", 0, 520, 330, 30, 16);
            leaveReview = MakeButton(review, "Back to the Atrium", 0, 654, 190, 56, LeaveReview);
        }
        void BuildGlyphs()
        {
            // v0.3 Part A: name the glyph by direct tap. Same layout as a review item so the two read as one family.
            glyphs = ScreenPanel("Glyphs");
            Label(glyphs, "THE ZODIAC WING", 0, 32, 340, 24, 18);
            glyphProgress = Label(glyphs, "", 0, 62, 300, 20, 12); glyphProgress.color = Muted;
            var card = Rect("Glyph card", glyphs, 0, 200, 140, 140); card.gameObject.AddComponent<Image>().color = PanelColor;
            glyphCard = Label(card, "", 0, 70, 130, 130, 84); glyphCard.font = Dial.GlyphFont; glyphCard.horizontalOverflow = HorizontalWrapMode.Overflow; glyphCard.verticalOverflow = VerticalWrapMode.Overflow;
            Label(glyphs, "Which sign carries this symbol?", 0, 290, 330, 24, 15);
            for (int i = 0; i < 4; i++) { int slot = i; glyphNameButtons[i] = MakeButton(glyphs, "", -78 + (i % 2) * 156, 340 + (i / 2) * 64, 150, 56, () => AnswerGlyphName(slot)); }
            var panel = Rect("Caspar panel", glyphs, 0, 520, 324, 120); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 14, 290, 20, 13);
            glyphCaspar = Label(panel, "", 0, 70, 306, 90, 12);
            glyphNote = Label(glyphs, "", 0, 446, 330, 24, 14); // between the name buttons (to 432) and the Caspar panel (from 460)
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
            var c = Rect("Candle", r, -160, 445, 6, 18); c.SetAsFirstSibling(); candle = c.gameObject.AddComponent<Image>(); candle.color = LampDark; candle.raycastTarget = false;
            var s = Rect("Seam", r, 0, 270, 332, 2); seam = s.gameObject.AddComponent<Image>(); seam.color = new Color(Bone.r, Bone.g, Bone.b, 0); seam.raycastTarget = false;
            var glow = Rect("Key glow", r, 0, 270, 140, 140); keyGlow = glow.gameObject.AddComponent<Image>(); keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); keyGlow.raycastTarget = false;
            keyRect = Rect("Keeper Key", r, 0, 270, 84, 40); var keyImage = keyRect.gameObject.AddComponent<Image>(); keyImage.color = new Color(Bone.r, Bone.g, Bone.b, 0); keyImage.raycastTarget = false;
            keyLabel = Label(keyRect, "KEEPER KEY", 0, 20, 80, 36, 12); keyLabel.color = new Color(Charcoal.r, Charcoal.g, Charcoal.b, 0);
            keyIndicator = Label(r, "Keeper Key: 1", 110, 92, 140, 20, 12); keyIndicator.alignment = TextAnchor.MiddleRight; keyIndicator.gameObject.SetActive(false);
            wingContinue = MakeButton(r, "Continue", 0, 654, 190, 56, WingContinue); wingContinue.name = "Slice Continue"; wingContinue.gameObject.SetActive(false);
        }

        void BuildWingRoom()
        {
            // Q06 phase 2, decision 2: the Wing as a room with two points of interest, the Dial and the doorway back.
            wingRoom = ScreenPanel("Wing room");
            Label(wingRoom, "THE ZODIAC WING", 0, 32, 340, 24, 18);
            Label(wingRoom, "The Elemental Pattern", 0, 62, 300, 20, 12).color = Muted;
            var shelf = Block(wingRoom, "Collapsed bookshelf", 120, 140, 50, 36);
            for (int i = 0; i < 3; i++) { var book = Rect("Book", shelf, -14 + i * 14, 18, 8, 24); book.gameObject.AddComponent<Image>().color = new Color(.3f, .28f, .3f); book.localRotation = Quaternion.Euler(0, 0, i * 9 - 9); }
            var dial = Rect("The Dial", wingRoom, 30, 250, 200, 200); var dialImage = dial.gameObject.AddComponent<Image>(); dialImage.color = new Color(0, 0, 0, 0);
            RingLines(dial, 88, new Color(Bone.r, Bone.g, Bone.b, .4f), null); RingLines(dial, 30, new Color(Bone.r, Bone.g, Bone.b, .25f), null);
            var dialLabel = Label(wingRoom, "The Dial", 30, 352, 120, 16, 10); dialLabel.color = Muted;
            Tappable(dial, () => Walk("dial"));
            var door = Block(wingRoom, "Doorway back", -130, 325, 70, 100); Tappable(door, () => Walk("atrium-door"));
            var light = Rect("Doorway light", door, 0, 50, 50, 82); var lightImage = light.gameObject.AddComponent<Image>(); lightImage.color = new Color(.95f, .8f, .5f, .25f); lightImage.raycastTarget = false;
            var floor = Rect("Floor band", wingRoom, 0, BandY, 340, 30); var floorImage = floor.gameObject.AddComponent<Image>(); floorImage.color = new Color(.16f, .16f, .19f); floorImage.raycastTarget = false;
            wingRoomCaption = Label(wingRoom, "The Dial waits at the center of the room. The doorway leads back.", 0, 478, 340, 36, 12); // two lines at 360 wide wingRoomCaption.color = Muted; // placeholder (owner writes)
            enterDial = MakeButton(wingRoom, "The Dial", 0, 624, 300, 52, () => Walk("dial"));
            wingRoomBack = MakeButton(wingRoom, "Back to the Atrium", 0, 680, 300, 52, LeaveWing);
        }
        void BuildAvatar()
        {
            // Q06 phase 2, decision 3: a placeholder upright marker with a walk bob and one idle pose. No face, no clothing.
            avatar = Rect("Keeper", root, 0, BandY - 16, 20, 44);
            var body = Rect("Body", avatar, 0, 26, 16, 30); var bodyImage = body.gameObject.AddComponent<Image>(); bodyImage.color = new Color(.85f, .8f, .72f); bodyImage.raycastTarget = false;
            avatarHead = Rect("Head", avatar, 0, 7, 12, 12); var headImage = avatarHead.gameObject.AddComponent<Image>(); headImage.color = new Color(.85f, .8f, .72f); headImage.raycastTarget = false;
            avatar.gameObject.SetActive(false);
        }
        void BuildFade()
        {
            var fade = Rect("Fade", root, 0, 400, 360, 800); fadeImage = fade.gameObject.AddComponent<Image>(); fadeImage.color = new Color(0, 0, 0, 0); fadeImage.raycastTarget = false;
        }
        void PlaceAvatar()
        {
            var walk = Flow.Walk;
            avatar.anchoredPosition = new Vector2(walk.X, -(BandY - 16) + walk.Bob);
            avatarHead.anchoredPosition = new Vector2(walk.Facing * 2, -7);
        }
        void Tappable(RectTransform r, Action action)
        {
            var image = r.GetComponent<Image>(); image.raycastTarget = true;
            var button = r.gameObject.AddComponent<Button>(); button.onClick.AddListener(() => action());
            var colors = button.colors; colors.highlightedColor = new Color(1, 1, 1, .9f); colors.pressedColor = new Color(.8f, .8f, .8f); colors.selectedColor = new Color(.85f, .7f, .55f); button.colors = colors;
            Dial.RegisterNavigation(button);
        }
        Button TestButton(Transform parent, string text, float x, float top, UnityEngine.Events.UnityAction action)
        {
            var b = MakeButton(parent, text, x, top, 112, 40, action); b.GetComponent<Image>().color = PanelColor;
            var t = b.GetComponentInChildren<Text>(); t.fontSize = 11; t.horizontalOverflow = HorizontalWrapMode.Overflow; return b;
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
            else if (command == "reload") { if (!busy) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); } // test-only: resume from the local save
            else if (command == "enter-wing") EnterWing();
            else if (command == "enter-dial") Walk("dial");
            else if (command.StartsWith("walk:")) Walk(command.Substring(5));
            else if (command == "walk-speed") CycleWalkSpeed();
            else if (command == "enter-seals") EnterSeals();
            else if (command == "leave-wing") LeaveWing();
            else if (command == "leave-review") LeaveReview();
            else if (command.StartsWith("element:") && int.TryParse(command.Substring(8), out int element) && element >= 0 && element < 4) AnswerTap(Elements[element]);
            else if (command.StartsWith("glyph-name:") && int.TryParse(command.Substring(11), out int slot) && slot >= 0 && slot < 4) { if (Flow.Screen == SliceScreen.Review) AnswerGlyphReview(slot); else AnswerGlyphName(slot); }
        }
        void ChooseBirth(string choice) { if (busy) return; Flow.ChooseBirth(choice); AfterBirthEntry(); }
        void UseDate(string text)
        {
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
            if (s == SliceScreen.Atrium && Page < AtriumPages.Length - 1) { Page++; ShowPage(); Publish(); return; }
            if (s == SliceScreen.AtriumReturn && Page < ReturnPages.Length - 1) { Page++; ShowPage(); Publish(); return; }
            if (s == SliceScreen.Chamber && !Flow.Ended) { if (Page < ChamberPages.Length - 1) { Page++; ShowPage(); Publish(); } return; }
            var from = Flow.Screen;
            if (!Flow.Continue()) return;
            Page = 0;
            if (Flow.Screen == SliceScreen.Hub) Save();
            if (from == SliceScreen.Birth) StartCoroutine(WhiteLight()); else { Show(); Publish(); }
        }
        void WingContinue() { if (Flow.AtriumStage >= 2) LeaveWing(); else Continue(); }
        void Insert() { if (busy || Page < ChamberPages.Length - 1 || !Flow.InsertKey()) return; StartCoroutine(Chandelier()); }
        void Restart() { if (busy) return; PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        // Q06 phase 2, decision 5: the buttons and the taps do the same thing through the same walk.
        void EnterWing() { Walk("wing-door"); }
        void EnterDialNow()
        {
            if (!Flow.EnterDial()) return;
            Dial.SliceHidesOptional = true;
            if (Dial.Lesson.CanContinueUnit) Dial.Lesson.BeginContinuation();
            else if (Dial.Lesson.CanBeginGlyphs) { if (Dial.Lesson.BeginGlyphs()) { Flow.StartGlyphs(); Save(); } }
            Show(); Dial.Realign(); Publish();
        }
        void Walk(string id)
        {
            if (busy || (Flow.Screen != SliceScreen.Hub && Flow.Screen != SliceScreen.WingRoom)) return;
            var poi = Flow.Walk.Find(id); if (poi == null) return;
            if (!poi.Walkable) { if (Flow.TouchSealedDoor()) { hubNote.text = Flow.Note; Publish(); } return; }
            if (!Flow.Walk.GoTo(id)) return;
            StartCoroutine(Travel(id));
        }
        IEnumerator Travel(string id)
        {
            busy = true; hubNote.text = ""; Publish();
            var walk = Flow.Walk;
            if (ReducedMotion) walk.Jump(); // decision 4: reduced motion jumps
            else while (!walk.Tick(Time.unscaledDeltaTime)) { PlaceAvatar(); yield return null; }
            PlaceAvatar();
            yield return Arrive(id);
            busy = false; Show(); Publish();
        }
        IEnumerator Arrive(string id)
        {
            if (id == "wing-door" || id == "atrium-door")
            {
                yield return FadeTo(1);
                if (id == "wing-door") Flow.EnterWing(); else { Flow.LeaveWing(); Save(); }
                Show(); PlaceAvatar(); Publish();
                yield return new WaitForSecondsRealtime(ReducedMotion ? 0 : .12f);
                yield return FadeTo(0);
            }
            else if (id == "desk") { if (!Flow.EnterSeals()) hubNote.text = Flow.Note; else { Show(); StartReviewItem(); } }
            else if (id == "caspar") { Flow.ApproachCaspar(); hubNote.text = Flow.Note; }
            else if (id == "dial") EnterDialNow();
        }
        IEnumerator FadeTo(float alpha)
        {
            fadeImage.raycastTarget = true; float from = fadeImage.color.a;
            yield return Tween(ReducedMotion ? 0 : FadeSeconds, k => fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(from, alpha, k)));
            fadeImage.raycastTarget = alpha > 0;
        }
        void CycleWalkSpeed()
        {
            if (busy) return;
            Flow.Walk.CycleSpeed(); walkSpeedLabel.text = "Walk: " + Flow.Walk.SpeedName + " (test)"; Publish();
        }
        void AnswerGlyphName(int slot)
        {
            if (busy || Dial.Lesson.Phase != LessonPhase.GlyphNames) return;
            int target = Dial.Lesson.CurrentGlyph; if (target < 0) return;
            int seat = Dial.Lesson.GlyphOptions(target)[slot];
            Dial.Lesson.AnswerGlyphName(seat);
            glyphNote.text = Dial.Lesson.GlyphNameResult; Publish();
            StartCoroutine(AfterGlyphName());
        }
        IEnumerator AfterGlyphName()
        {
            busy = true; ShowGlyphs(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .7f : 1.1f);
            busy = false; Show(); if (Dial.Lesson.Phase == LessonPhase.GlyphWheel) Dial.Realign(); Publish();
        }
        void AnswerGlyphReview(int slot)
        {
            if (busy) return;
            var task = Flow.CurrentReview; if (task == null || task.Mode != ReviewMode.Glyph) return;
            int seat = Flow.GlyphReviewOptions(task.seat)[slot];
            Flow.AnswerGlyph(seat);
            reviewNote.text = Flow.Note; Save(); Publish();
            if (task.done) StartCoroutine(AfterTap());
        }
        void LeaveWing()
        {
            if (busy) return;
            if (Flow.Screen == SliceScreen.Wing) { if (Dial.Busy || Dial.Lesson.Dial.Active || !Flow.LeaveDial()) return; Save(); Show(); Publish(); }
            if (Flow.Screen == SliceScreen.WingRoom) Walk("atrium-door"); // one press from the Dial walks back out through the room
        }
        void EnterSeals() { Walk("desk"); }
        void StartReviewItem()
        {
            var task = Flow.CurrentReview;
            reviewNote.text = "";
            if (task == null) { Show(); Publish(); return; }
            if (task.Mode == ReviewMode.Dial)
            {
                Dial.SliceHidesOptional = true;
                Dial.Lesson.BeginReview(task.seat); Show(); Dial.Realign(); Publish();
            }
            else { Show(); Publish(); }
        }
        IEnumerator AfterDialReview(bool correct, bool eligible)
        {
            busy = true; Flow.FinishReview(correct, eligible); Save(); Dial.ForceRefresh(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .8f : 1.4f);
            Dial.Lesson.EndReview(); Dial.Realign(); busy = false; StartReviewItem();
        }
        void AnswerTap(string element)
        {
            if (busy) { Dial.Lesson.Dial.Log("tap_ignored_busy"); return; }
            var task = Flow.CurrentReview; if (task == null || task.Mode != ReviewMode.Tap) { Dial.Lesson.Dial.Log("tap_ignored_task"); return; }
            Flow.AnswerTap(element); Dial.Lesson.Dial.Log("tap_answered");
            reviewNote.text = Flow.Note; Save(); Publish();
            if (task.done) StartCoroutine(AfterTap());
        }
        IEnumerator AfterTap()
        {
            busy = true; ShowReview(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .8f : 1.2f);
            busy = false; StartReviewItem();
        }
        void LeaveReview() { if (busy || !Flow.LeaveReview()) return; Save(); Show(); Publish(); }

        void Show()
        {
            var s = Flow.Screen;
            var task = Flow.CurrentReview;
            bool reviewOnDial = s == SliceScreen.Review && task != null && task.Mode == ReviewMode.Dial && !task.done;
            identity.gameObject.SetActive(s == SliceScreen.Identity); birth.gameObject.SetActive(s == SliceScreen.Birth);
            atrium.gameObject.SetActive(s == SliceScreen.Atrium); atriumReturn.gameObject.SetActive(s == SliceScreen.AtriumReturn);
            chamber.gameObject.SetActive(s == SliceScreen.Chamber); hub.gameObject.SetActive(s == SliceScreen.Hub);
            wingRoom.gameObject.SetActive(s == SliceScreen.WingRoom);
            bool roomScreen = s == SliceScreen.Hub || s == SliceScreen.WingRoom;
            if (roomScreen) { avatar.SetParent(s == SliceScreen.Hub ? hub : wingRoom, false); avatar.SetAsLastSibling(); PlaceAvatar(); }
            avatar.gameObject.SetActive(roomScreen);
            if (s == SliceScreen.WingRoom) { enterDial.interactable = !busy; wingRoomBack.interactable = !busy; }
            bool partA = s == SliceScreen.Wing && Dial.Lesson.Phase == LessonPhase.GlyphNames;
            review.gameObject.SetActive(s == SliceScreen.Review && !reviewOnDial);
            glyphs.gameObject.SetActive(partA);
            Dial.UiCanvas.gameObject.SetActive((s == SliceScreen.Wing && !partA) || reviewOnDial);
            if (partA) ShowGlyphs();
            if (birthContinue != null) birthContinue.interactable = Flow.CanContinue;
            if (s == SliceScreen.Wing || reviewOnDial)
            {
                if (!sunSent && Flow.HasSunSign) { Dial.Lesson.SetSunSign(Flow.SunSign); sunSent = true; }
                if (Flow.AtriumStage >= 2) { keyRect.gameObject.SetActive(false); keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); seam.color = new Color(Bone.r, Bone.g, Bone.b, 0); Dial.Ring.localScale = Vector3.one; }
                if (Flow.AtriumStage >= 2) { var t = wingContinue.GetComponentInChildren<Text>(); t.text = "Back to the Atrium"; }
                Dial.ForceRefresh();
            }
            if (s == SliceScreen.Hub) ShowHub();
            if (s == SliceScreen.Review) ShowReview();
            ShowPage();
        }
        void ShowGlyphs()
        {
            var lesson = Dial.Lesson; int target = lesson.CurrentGlyph;
            bool naming = lesson.Phase == LessonPhase.GlyphNames; // after the twelfth answer the wheel already owns the index; the last card stays up through the hold
            if (naming)
            {
                glyphProgress.text = "Symbol " + Mathf.Min(lesson.GlyphIndex + 1, 12) + " of 12";
                glyphCard.text = target >= 0 && target < 12 ? Zodiac.Seats[target].Glyph : "";
                var options = target >= 0 && target < 12 ? lesson.GlyphOptions(target) : new int[4];
                for (int i = 0; i < 4; i++) glyphNameButtons[i].GetComponentInChildren<Text>().text = target >= 0 ? Zodiac.Seats[options[i]].Name : "";
            }
            for (int i = 0; i < 4; i++) glyphNameButtons[i].interactable = naming && !busy && target >= 0;
            glyphCaspar.text = DialLesson.GlyphIntro; glyphNote.text = busy ? glyphNote.text : "";
        }
        void ShowHub()
        {
            int stage = Flow.AtriumStage;
            lampOne.color = stage >= 2 ? LampLit : LampDark; lampTwo.color = stage >= 3 ? LampLit : LampDark;
            hubCaption.text = stage >= 3 ? "Stirring: two lamps, a clear desk, the Wing open." : "Stirring: one lamp lit, one desk uncovered, the Wing open.";
            hubText.text = Flow.Keys >= 2 ? HubKey2Line : Flow.V02Complete ? HubCompleteLine : Resumed || Flow.ReviewsChecked > 0 ? HubLaterLine : HubFirstLine;
            int due = Flow.DueCount;
            enterSealsLabel.text = due > 0 ? "Check the Seals · " + due + " due" : "Check the Seals";
            enterWing.GetComponentInChildren<Text>().text = Flow.Keys >= 2 ? "The Zodiac Wing (read)" : Flow.WheelComplete ? "The Zodiac Wing (lit)" : "The Zodiac Wing";
            endCard.text = Flow.Keys >= 2 ? "End of prototype v0.4. The Library can be walked." : "End of prototype v0.2. Glyphs and Key 2 come next.";
            hubNote.text = Flow.Note;
            endCard.gameObject.SetActive(Flow.V02Complete || Flow.V03Complete);
        }
        void ShowReview()
        {
            var task = Flow.CurrentReview;
            bool done = Flow.ReviewDone;
            reviewProgress.text = done ? "" : (Flow.ReviewIndex + 1) + " of " + Flow.ReviewQueue.Count;
            bool glyphItem = !done && task != null && task.Mode == ReviewMode.Glyph;
            reviewQuestion.text = done ? "" : task != null && task.Mode == ReviewMode.Tap ? Zodiac.Seats[task.seat].Name + ". Which family?" : glyphItem ? "Which sign carries this symbol?" : "";
            foreach (var b in elementButtons) { b.gameObject.SetActive(!done && task != null && task.Mode == ReviewMode.Tap); b.interactable = !busy && task != null && !task.done; }
            reviewGlyph.gameObject.SetActive(glyphItem); reviewGlyph.text = glyphItem ? Zodiac.Seats[task.seat].Glyph : "";
            var opts = glyphItem ? Flow.GlyphReviewOptions(task.seat) : new int[4];
            for (int i = 0; i < 4; i++) { reviewGlyphButtons[i].gameObject.SetActive(glyphItem); reviewGlyphButtons[i].GetComponentInChildren<Text>().text = glyphItem ? Zodiac.Seats[opts[i]].Name : ""; reviewGlyphButtons[i].interactable = !busy && glyphItem && !task.done; }
            reviewSummary.text = done ? Flow.ReviewSummary : "";
            leaveReview.gameObject.SetActive(done);
            if (done) reviewNote.text = "The seals are checked. Placeholder line: the owner writes Caspar's close."; // owner writes
        }
        void ShowPage()
        {
            atriumText.text = AtriumPages[Mathf.Min(Page, AtriumPages.Length - 1)];
            returnText.text = ReturnPages[Mathf.Min(Page, ReturnPages.Length - 1)];
            if (!Flow.KeyInserted) chamberText.text = ChamberPages[Mathf.Min(Page, ChamberPages.Length - 1)];
            bool chamberReady = Flow.Screen == SliceScreen.Chamber && Page >= ChamberPages.Length - 1;
            // A visible Continue turns Caspar's pages; the glowing Insert appears only on his last page. (The hidden
            // screen-reader Continue was the only page-turn before, which left touch players stuck here.)
            insert.gameObject.SetActive(Flow.Screen == SliceScreen.Chamber && !Flow.KeyInserted && chamberReady);
            insert.interactable = chamberReady && !busy;
            chamberContinue.gameObject.SetActive(Flow.Screen == SliceScreen.Chamber && !busy && (Flow.Ended || (!Flow.KeyInserted && !chamberReady)));
            if (!chamberReady && insertGlow != null) insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0);
        }
        void LightWing() { foreach (var line in floorLines) if (line != null) line.color = new Color(.62f, .57f, .53f, .8f); candle.color = LampLit; }
        public void Publish() => Dial.Publish();
        public void Fill(DialView.WebState state)
        {
            var s = Flow.Screen; var task = Flow.CurrentReview;
            bool reviewOnDial = s == SliceScreen.Review && task != null && task.Mode == ReviewMode.Dial && !task.done;
            state.screen = s.ToString().ToLowerInvariant(); state.playerName = Flow.DisplayName; state.note = Flow.Note;
            state.busy = state.busy || busy; // The semantic layer must see the slice's own beats as busy too.
            state.caspar = s == SliceScreen.Identity ? "Who are you? Enter a name, then continue." :
                s == SliceScreen.Birth ? "Do you know when you were born?" + (Flow.BirthChoice == "chart" && !Flow.HasSunSign ? " Enter your birth month and day." : Flow.BirthChoice == "known" && !Flow.HasSunSign ? " Choose your sign." : "") :
                s == SliceScreen.Atrium ? atriumText.text : s == SliceScreen.AtriumReturn ? returnText.text :
                s == SliceScreen.Chamber ? chamberText.text + (Flow.Ended ? " " + chamberEnd.text : "") :
                s == SliceScreen.Hub ? hubText.text + " " + hubCaption.text :
                s == SliceScreen.Review && !reviewOnDial ? (Flow.ReviewDone ? Flow.ReviewSummary + " " + reviewNote.text : reviewQuestion.text + " " + reviewNote.text) : "";
            state.keyRevealed = Flow.KeyRevealed; state.keyInserted = Flow.KeyInserted; state.ended = Flow.Ended; state.locksFilled = Flow.LocksFilled;
            state.sunSign = Flow.HasSunSign ? Zodiac.Seats[Flow.SunSign].Name : "";
            state.canInsert = s == SliceScreen.Chamber && !Flow.KeyInserted && Page >= ChamberPages.Length - 1 && !busy;
            state.canSliceContinue = !busy && ((s == SliceScreen.Atrium && Page < AtriumPages.Length - 1) || (s == SliceScreen.AtriumReturn && Page < ReturnPages.Length - 1) || (s == SliceScreen.Chamber && !Flow.Ended && Page < ChamberPages.Length - 1) || ((s != SliceScreen.Chamber || Flow.Ended) && s != SliceScreen.Wing && Flow.CanContinue) || (s == SliceScreen.Wing && Flow.AtriumStage == 1 && Flow.CanContinue));
            state.canName = s == SliceScreen.Identity;
            state.canBirth = s == SliceScreen.Birth && !busy && Flow.BirthChoice == "";
            state.canBirthDate = s == SliceScreen.Birth && !busy && Flow.BirthChoice == "chart" && !Flow.HasSunSign;
            state.canSignPick = s == SliceScreen.Birth && !busy && Flow.BirthChoice == "known" && !Flow.HasSunSign;
            state.canChangeBirth = s == SliceScreen.Birth && !busy && Flow.BirthChoice != "";
            // v0.2
            state.atriumStage = Flow.AtriumStage; state.dueCount = Flow.DueCount; state.resumed = Resumed;
            state.canEnterWing = s == SliceScreen.Hub && !busy; state.canEnterSeals = s == SliceScreen.Hub && !busy;
            state.canLeaveWing = s == SliceScreen.Wing && Flow.AtriumStage >= 2 && wingContinue.gameObject.activeSelf && !busy && Dial.Lesson.Phase != LessonPhase.GlyphNames;
            state.canLeaveReview = s == SliceScreen.Review && Flow.ReviewDone && !busy;
            state.reviewMode = s != SliceScreen.Review ? "" : Flow.ReviewDone ? "done" : task != null && task.Mode == ReviewMode.Dial ? "dial" : "tap";
            state.reviewIndex = Flow.ReviewIndex; state.reviewTotal = Flow.ReviewQueue.Count; state.reviewSign = task != null && !Flow.ReviewDone ? Zodiac.Seats[task.seat].Name : "";
            state.reviewSummary = Flow.ReviewSummary; state.hubNote = hubNote != null ? hubNote.text : ""; state.v02Complete = Flow.V02Complete;
            bool partA = s == SliceScreen.Wing && Dial.Lesson.Phase == LessonPhase.GlyphNames;
            bool glyphItem = s == SliceScreen.Review && task != null && !Flow.ReviewDone && task.Mode == ReviewMode.Glyph;
            state.glyphMode = partA ? "name" : glyphItem ? "review" : "";
            if (partA || glyphItem)
            {
                int target = partA ? Dial.Lesson.CurrentGlyph : task.seat;
                var opts = partA ? Dial.Lesson.GlyphOptions(target) : Flow.GlyphReviewOptions(target);
                state.glyphChar = Zodiac.Seats[target].Glyph; state.glyphOptions = new[] { Zodiac.Seats[opts[0]].Name, Zodiac.Seats[opts[1]].Name, Zodiac.Seats[opts[2]].Name, Zodiac.Seats[opts[3]].Name };
                if (partA) state.caspar = "Which sign carries this symbol? " + (glyphNote.text ?? "") + " " + DialLesson.GlyphIntro;
            }
            if (glyphItem) state.reviewMode = "glyph";
            state.v03Complete = Flow.V03Complete;
            // v0.4
            bool roomScreen = s == SliceScreen.Hub || s == SliceScreen.WingRoom;
            var walk = Flow.Walk;
            state.room = !roomScreen ? "" : walk.Room == Room.Atrium ? "atrium" : "wing";
            state.avatarX = walk.X; state.walking = walk.Walking; state.walkTarget = walk.TargetId; state.avatarAt = walk.At; state.walkSpeed = walk.SpeedName;
            var pois = roomScreen ? Rooms.Visible(walk.Room).ToArray() : new PointOfInterest[0];
            state.pois = pois.Select(p => p.Id).ToArray(); state.poiLabels = pois.Select(p => (p.Walkable ? "Walk to " : "") + p.Label).ToArray();
            state.canWalk = roomScreen && !busy; state.canEnterDial = s == SliceScreen.WingRoom && !busy;
            if (s == SliceScreen.WingRoom) { state.caspar = wingRoomCaption.text; state.canLeaveWing = !busy; }
            if (roomScreen && !busy) state.note = hubNote.text;
        }

        // ---- save / restore (Q05 decision 5) ----
        void Save()
        {
            if (Flow.AtriumStage < 2) return;
            try { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Flow.ToSave(Dial.Lesson.Lit, Dial.Lesson.Kin, Dial.Lesson.KeyEarned))); PlayerPrefs.Save(); }
            catch (Exception e) { Debug.LogWarning("[CelestialDial] save failed: " + e.Message); }
        }
        void TryRestore()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) return;
            SaveData save = null;
            try { save = JsonUtility.FromJson<SaveData>(json); } catch (Exception e) { Debug.LogWarning("[CelestialDial] save unreadable: " + e.Message); }
            if (save == null || !Flow.Restore(save)) return;
            Dial.Lesson.RestoreProgress(save.sunSign, save.lit, save.kin, save.keyEarned);
            Dial.Lesson.RestoreGlyphs(save.glyphStage, save.glyphIndex, save.keys >= 2);
            if (save.keys >= 2) keyIndicator.text = "Keeper Keys: 2";
            sunSent = true; revealStarted = true; Resumed = true; Dial.SliceHidesOptional = true;
            keyIndicator.gameObject.SetActive(save.keyEarned); if (save.wheelComplete) LightWing(); else if (save.keyEarned) { foreach (var line in floorLines) if (line != null) line.color = new Color(.62f, .57f, .53f, .55f); candle.color = Bone; }
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
            Flow.End(); chamberEnd.gameObject.SetActive(true); insert.gameObject.SetActive(false); insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0);
            busy = false; ShowPage(); Publish();
        }
        void Candles(bool on) { foreach (var c in candles) c.color = on ? Bone : LampDark; }
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
