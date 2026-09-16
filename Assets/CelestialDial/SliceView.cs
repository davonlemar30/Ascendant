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
        public GridModel Grid { get; private set; } // Build B: the table (pure C#, beside the deck)
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
        Button enterDial, enterShelf, wingRoomBack, walkSpeed, closeBook; Image shelfGlow;
        // Build B: the table in the Wing room and its screen.
        RectTransform gridScreen; Text gridCaspar, gridReadout, gridStatus, gridKeys; Button gridSeal, gridAsk, leaveGrid, enterGrid; Image gridGlow, lampThree, lampFour;
        // Build D: the Chamber as a room, the Books, and the Atrium's dressing per stage.
        Button enterChamber, chamberBack; Image sealedLeftLight; Text shelvesLabel, chamberBooksLabel, chandelierLabel; RectTransform chamberDoor, chamberBooksTap, chamberBand;
        readonly Image[] bookImages = new Image[SliceFlow.Books], bookPages = new Image[SliceFlow.Books], shelfBooks = new Image[3];
        const float ChamberBandY = 408f; // the Chamber's floor band sits under the Books, above Caspar's panel
        string chamberLine = "";
        readonly Button[] gridTiles = new Button[12], gridCells = new Button[12];
        readonly Text[] gridTileNames = new Text[12], gridTileGlyphs = new Text[12], gridCellNames = new Text[12], gridCellGlyphs = new Text[12];
        int demoCell = -1;
        const float BandY = 436f, FadeSeconds = .35f; // floor band and fade length are test variables (Q06 phase 2, decision 7)
        readonly Button[] glyphNameButtons = new Button[4];
        readonly Button[] reviewGlyphButtons = new Button[4];
        InputField nameField, dateField;
        Button birthContinue, wingContinue, insert, chamberContinue, atriumContinue, returnContinue, changeChoice;
        Button enterWing, enterSeals, hubRestart, leaveReview;
        readonly Button[] elementButtons = new Button[4];
        readonly Button[] modalityButtons = new Button[3]; // Build A: which kind?
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
        static readonly Color Held = new Color(.45f,.36f,.28f), Seated = new Color(.29f,.27f,.28f), TileGone = new Color(.1f,.1f,.12f);
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
        const string HubKey3Line = "Three Keys. The table is full, and the Wing has one more thing to teach you.\nRest now. The seals will keep."; // placeholder (owner writes; Build B)
        const string HubKey4Line = "Four Keys. Every pattern the wheel keeps, you keep now.\nRest. The Chamber will want to see them."; // placeholder (owner writes; Build C)
        // Build D placeholder lines (owner writes; worksheet section 13).
        const string HubKeyInHandLine = "You carry a Key the Chamber has not seen. Its door is open when you are ready.";
        const string HubKeysInHandLine = "You carry {0} Keys the Chamber has not seen. Its door is open when you are ready.";
        const string HubSpent2Line = "Two locks filled. The first Book is one Key from opening.\nThe shelves are taking their books back.";
        const string HubSpent3Line = "Three locks. The first Book breathes. He would not have believed it.\nThere is light behind a sealed door now.";
        const string HubWholeLine = "Four Keys spent. The Wing is whole, and the second Book has begun.\nWhat remains is sealed, for now. Rest.";
        const string ChamberQuietLine = "The Books are quiet. They will want the next Key.";
        const string ChamberWholeLine = "The first Book open, the second begun. The Wing is whole.\nWhat remains is sealed, for now.";
        const string ChamberBringLine = "Bring it to the Books.";
        const string ChamberChooseLine = "Choose a lock. Feed it your Key.";
        const string ChamberEndCard = "End of the Zodiac Wing. The rest is sealed for now."; // one line at 330 wide
        bool ReducedMotion => Dial.Lesson.Dial.ReducedMotion;

        void Awake()
        {
            gameObject.name = "VerticalSlice";
            var dialObject = new GameObject("CelestialDial"); dialObject.transform.SetParent(transform, false);
            Dial = dialObject.AddComponent<DialView>();
            Dial.Slice = this; Dial.ExtraActions = WebAction; font = Dial.UiFont;
            Flow.Logged += name => Dial.Lesson.Dial.Log(name.ToLowerInvariant().Replace(':', '_'), false, false, "slice");
            Dial.Lesson.Dial.Logged += e => { if (e.event_name == "answer_correct" && Dial.Lesson.Phase != LessonPhase.Review && !Dial.Lesson.InModalities && !Dial.Lesson.InOppositeProblem) { Flow.RecordLessonAnswer(e.selected_destination, e.evidence_eligible); Save(); } };
            Dial.Lesson.Dial.Logged += e => { if (e.event_name == "answer_correct" && Dial.Lesson.InOppositeProblem) { Flow.RecordOppositeAnswer(e.start_seat, e.evidence_eligible); Save(); } }; // Build C: a pair learned, on the wheel or in the builder
            Dial.Lesson.Dial.Logged += e => { if (e.event_name == "polarity_shown" || e.event_name == "builder_sign_built" || e.event_name == "opposites_completed") Save(); };
            Dial.Lesson.ReviewFinished += (seat, correct, eligible) => StartCoroutine(AfterDialReview(correct, eligible));
            Dial.Lesson.GlyphNamedEvent += (seat, correct, eligible) => { Flow.RecordGlyphAnswer(seat, eligible); Flow.SetGlyphProgress(Dial.Lesson.Phase == LessonPhase.GlyphWheel ? 1 : 0, Dial.Lesson.GlyphIndex); Save(); };
            Dial.Lesson.Dial.Logged += e => { if (e.event_name == "glyph_placed") { Flow.RecordGlyphAnswer(e.selected_destination, e.evidence_eligible); Save(); } };
            Dial.Lesson.PracticeFinished += clean => { if (clean) Flow.RecordCleanRun(); Save(); Publish(); };
            Dial.Lesson.Dial.Logged += e => { if (e.event_name == "answer_correct" && Dial.Lesson.InModalities) { Flow.RecordModalityAnswer(e.selected_destination, e.evidence_eligible); Save(); } };
            Grid = new GridModel(() => Time.realtimeSinceStartupAsDouble);
            Grid.Logged += e => Debug.Log("[CelestialDial] " + JsonUtility.ToJson(e));
            Grid.Logged += e => { if (e.event_name == "grid_placed") { Flow.RecordGridAnswer(e.start_seat, e.evidence_eligible); Save(); } };
            var canvasObject = new GameObject("Slice Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1;
            root = Rect("Slice Portrait", canvasObject.transform, 0, 0, 360, 800);
            BuildIdentity(); BuildBirth();
            atrium = BuildAtrium("Atrium", out atriumText, out atriumContinue);
            atriumReturn = BuildAtrium("Atrium return", out returnText, out returnContinue);
            BuildChamber(); BuildHub(); BuildReview(); BuildGlyphs(); BuildGrid(); BuildWingExtras(); BuildWingRoom(); BuildAvatar(); BuildFade();
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
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.ModalitiesComplete && !Flow.ModalitiesComplete) { Flow.MarkModalitiesComplete(); Save(); Publish(); } // Build B: the table wakes
            if (Flow.Screen == SliceScreen.Grid && Grid.Key3Earned && Flow.Keys < 3) { Flow.MarkKey3(); keyIndicator.text = "Keeper Keys: 3"; Save(); Show(); Publish(); }
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.Key4Earned && Flow.Keys < 4) { Flow.MarkKey4(); keyIndicator.text = "Keeper Keys: 4"; Save(); Show(); Publish(); } // Build C
            if (insertGlow != null && insert.gameObject.activeInHierarchy && insert.interactable && (!Flow.KeyInserted || Flow.CanSpend))
            {
                float a = ReducedMotion ? .35f : .15f + .3f * Mathf.PingPong(Time.unscaledTime / 1.2f, 1f);
                insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, a);
            }
            if (wingContinue != null && Flow.AtriumStage >= 2)
            {
                // The Wing's Back button belongs to the Wing screen only; left on, it sat over the review's Seal (owner playtest, Sept 14).
                bool idle = Flow.Screen == SliceScreen.Wing && !Dial.Lesson.Dial.Active && !Dial.Busy && !busy && Dial.Lesson.Phase != LessonPhase.Review && !Dial.ControlsShown; // Build C: nor over the beat's Continue or the builder's buttons
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
            chandelierLabel = Label(chamber, "The chandelier, dark", 0, 116, 300, 18, 11); chandelierLabel.color = Muted;
            mechanism = Rect("Mechanism", chamber, 0, 180, 110, 110);
            RingLines(mechanism, 46, new Color(.62f, .57f, .53f, .5f), null);
            var tick = Rect("Mechanism tick", mechanism, 0, 55 - 46, 3, 14); tick.gameObject.AddComponent<Image>().color = Bone;
            for (int b = 0; b < SliceFlow.Books; b++)
            {
                float x = -138 + b * 46;
                var book = Rect("Book " + (b + 1), chamber, x, 300, 34, 70); bookImages[b] = book.gameObject.AddComponent<Image>(); bookImages[b].color = Dim;
                book.gameObject.AddComponent<Outline>().effectColor = new Color(.35f, .35f, .38f);
                var page = Rect("Page", book, 0, 35, 26, 58); bookPages[b] = page.gameObject.AddComponent<Image>(); bookPages[b].color = new Color(Bone.r, Bone.g, Bone.b, 0); bookPages[b].raycastTarget = false; // Build D: a page turns when the Book opens
                for (int l = 0; l < SliceFlow.LocksPerBook; l++)
                { var dot = Rect("Lock", chamber, x - 10 + l * 10, 348, 7, 7); locks[b * 3 + l] = dot.gameObject.AddComponent<Image>(); locks[b * 3 + l].color = new Color(.3f, .3f, .33f); }
            }
            chamberBooksLabel = Label(chamber, "Seven sealed Books, three locks each", 0, 376, 330, 20, 12); chamberBooksLabel.color = Muted;
            // Build D: the Chamber as a room. A doorway back, the Books as a point of interest, a floor band; all hidden on the first (Continue) visit.
            chamberBand = Rect("Floor band", chamber, 0, ChamberBandY, 340, 30); var chamberBandImage = chamberBand.gameObject.AddComponent<Image>(); chamberBandImage.color = new Color(.16f, .16f, .19f); chamberBandImage.raycastTarget = false; chamberBand.gameObject.SetActive(false); // before the doorway, so its label draws over the band
            chamberDoor = Block(chamber, "Doorway back", -140, 384, 40, 60); Tappable(chamberDoor, () => Walk("atrium-door")); chamberDoor.gameObject.SetActive(false);
            chamberBooksTap = Rect("The Books, tap to walk", chamber, 0, 300, 330, 90); var booksTapImage = chamberBooksTap.gameObject.AddComponent<Image>(); booksTapImage.color = new Color(0, 0, 0, 0); Tappable(chamberBooksTap, () => Walk("books")); chamberBooksTap.gameObject.SetActive(false);
            var panel = Rect("Caspar panel", chamber, 0, 520, 324, 170); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 16, 290, 22, 13);
            chamberText = Label(panel, "", 0, 96, 306, 136, 13);
            chamberEnd = Label(chamber, "The first Key is spent. The Library has taken her first breath.", 0, 720, 330, 28, 12); chamberEnd.color = Muted; chamberEnd.gameObject.SetActive(false);
            var glow = Rect("Insert glow", chamber, 0, 654, 214, 80); insertGlow = glow.gameObject.AddComponent<Image>(); insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); insertGlow.raycastTarget = false;
            insert = MakeButton(chamber, "Insert the Key", 0, 654, 190, 56, Insert); insert.GetComponent<Image>().color = Crimson;
            chamberContinue = MakeButton(chamber, "Continue", 0, 654, 190, 56, () => Continue()); chamberContinue.gameObject.SetActive(false);
            chamberBack = MakeButton(chamber, "Back to the Atrium", 0, 714, 300, 48, LeaveChamber); chamberBack.gameObject.SetActive(false); // Build D
        }
        void BuildHub()
        {
            // Q05 decisions 1, 6: the Atrium in Stage 2 "Stirring" with two entrances.
            hub = ScreenPanel("Hub");
            Label(hub, "THE GRAND ATRIUM", 0, 32, 340, 24, 18);
            var shelves = Block(hub, "Shelves, mostly empty", -130, 200, 60, 120); shelvesLabel = shelves.GetComponentInChildren<Text>();
            for (int i = 0; i < 3; i++) { var book = Rect("Book", shelves, -16 + i * 16, 30 + (i % 2) * 40, 10, 26); shelfBooks[i] = book.gameObject.AddComponent<Image>(); shelfBooks[i].color = new Color(.3f, .28f, .3f); shelfBooks[i].raycastTarget = false; book.gameObject.SetActive(false); } // Build D: the shelves take their books back at Stage 4
            var floor = Rect("Floor band", hub, 0, BandY, 340, 30); var floorImage = floor.gameObject.AddComponent<Image>(); floorImage.color = new Color(.16f, .16f, .19f); floorImage.raycastTarget = false;
            var desk = Block(hub, "Desk, uncovered", -125, 426, 70, 30); Tappable(desk, () => Walk("desk"));
            var casparMark = Rect("Caspar", hub, 100, 418, 14, 40); var casparBody = casparMark.gameObject.AddComponent<Image>(); casparBody.color = Muted; casparBody.raycastTarget = false;
            var casparHead = Rect("Caspar head", hub, 100, 392, 12, 12); var casparHeadImage = casparHead.gameObject.AddComponent<Image>(); casparHeadImage.color = Muted; casparHeadImage.raycastTarget = false;
            Label(hub, "Caspar", 100, 452, 60, 14, 10).color = Muted;
            var casparTap = Rect("Caspar, tap to walk", hub, 100, 420, 44, 76); var casparTapImage = casparTap.gameObject.AddComponent<Image>(); casparTapImage.color = new Color(0, 0, 0, 0); Tappable(casparTap, () => Walk("caspar"));
            var lamp1 = Rect("Lamp", hub, -40, 150, 8, 22); lampOne = lamp1.gameObject.AddComponent<Image>(); lampOne.color = LampLit; lampOne.raycastTarget = false;
            var lamp2 = Rect("Lamp", hub, 60, 150, 8, 22); lampTwo = lamp2.gameObject.AddComponent<Image>(); lampTwo.color = LampDark; lampTwo.raycastTarget = false;
            var lamp3 = Rect("Lamp", hub, 140, 150, 8, 22); lampThree = lamp3.gameObject.AddComponent<Image>(); lampThree.color = LampDark; lampThree.raycastTarget = false; // Build B: Stage 5
            var lamp4 = Rect("Lamp", hub, -90, 150, 8, 22); lampFour = lamp4.gameObject.AddComponent<Image>(); lampFour.color = LampDark; lampFour.raycastTarget = false; // Build C: Stage 6
            string[] doors = { "Sealed", "Zodiac Wing, open", "Crystal Book Chamber" }; // Build D: the third doorway leads back to the Chamber
            for (int i = 0; i < 3; i++)
            {
                var door = Block(hub, doors[i], -120 + i * 120, 325, 70, 100); string poi = i == 0 ? "sealed-left" : i == 1 ? "wing-door" : "chamber-door"; Tappable(door, () => Walk(poi));
                if (i == 1) { var light = Rect("Doorway light", door, 0, 50, 50, 82); doorOpenLight = light.gameObject.AddComponent<Image>(); doorOpenLight.color = new Color(.95f, .8f, .5f, .35f); doorOpenLight.raycastTarget = false; }
                else if (i == 2) { var light = Rect("Doorway light", door, 0, 50, 50, 82); var li = light.gameObject.AddComponent<Image>(); li.color = new Color(.7f, .8f, .95f, .3f); li.raycastTarget = false; }
                else { var lockRect = Rect("Lock", door, 0, 50, 12, 16); var li = lockRect.gameObject.AddComponent<Image>(); li.color = new Color(.45f, .45f, .5f); li.raycastTarget = false; var glow = Rect("Light behind the door", door, 0, 50, 50, 82); sealedLeftLight = glow.gameObject.AddComponent<Image>(); sealedLeftLight.color = new Color(.95f, .8f, .5f, 0); sealedLeftLight.raycastTarget = false; glow.SetAsFirstSibling(); }
            }
            hubCaption = Label(hub, "", 0, 470, 340, 20, 12); hubCaption.color = Muted;
            var panel = Rect("Caspar panel", hub, 0, 536, 324, 120); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 14, 290, 20, 13);
            hubText = Label(panel, "", 0, 70, 306, 90, 12);
            enterWing = MakeButton(hub, "The Zodiac Wing", -78, 624, 150, 52, EnterWing);
            enterChamber = MakeButton(hub, "The Chamber", 78, 624, 150, 52, EnterChamber); // Build D
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
            for (int i = 0; i < 3; i++) { string modality = Zodiac.Modalities[i]; modalityButtons[i] = MakeButton(review, modality, 0, 300 + i * 64, 300, 56, () => AnswerModalityTap(modality)); modalityButtons[i].gameObject.SetActive(false); }
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
            glyphNote = Label(glyphs, "", 0, 446, 330, 24, 14);
            closeBook = MakeButton(glyphs, "Close the book", 0, 680, 300, 52, CloseBook); // between the name buttons (to 432) and the Caspar panel (from 460)
        }
        void BuildGrid()
        {
            // Build B: the table. Four element rows by three kind columns, twelve sign tiles below. Tap a sign, tap a cell, Seal.
            gridScreen = ScreenPanel("Grid");
            Label(gridScreen, "THE ZODIAC WING", 0, 32, 340, 24, 18);
            Label(gridScreen, "The Table", 0, 62, 200, 22, 14); // placeholder unit name (owner writes)
            gridKeys = Label(gridScreen, "Keeper Keys: 2", 100, 62, 140, 20, 12); gridKeys.alignment = TextAnchor.MiddleRight; // ten px in from the edge: at 360 wide the Dial's indicator touches it
            for (int c = 0; c < GridModel.Columns; c++) Label(gridScreen, GridModel.ColumnName(c), -72 + c * 92, 96, 86, 18, 11).color = Muted;
            for (int r = 0; r < GridModel.Rows; r++) Label(gridScreen, GridModel.RowName(r), -150, 128 + r * 52, 56, 48, 12).color = Muted;
            for (int cell = 0; cell < 12; cell++)
            {
                int index = cell;
                gridCells[cell] = MakeButton(gridScreen, "Cell " + (cell + 1), -72 + (cell % 3) * 92, 128 + (cell / 3) * 52, 86, 48, () => ChooseCell(index));
                var name = gridCells[cell].GetComponentInChildren<Text>(); name.text = ""; name.fontSize = 11; name.horizontalOverflow = HorizontalWrapMode.Overflow; // the cell shows the seated sign, not a number
                var nameRect = (RectTransform)name.transform; nameRect.anchoredPosition = new Vector2(10, -24); nameRect.sizeDelta = new Vector2(58, 44); gridCellNames[cell] = name;
                gridCellGlyphs[cell] = Label(gridCells[cell].transform, "", -30, 24, 24, 40, 16); gridCellGlyphs[cell].font = Dial.GlyphFont; gridCellGlyphs[cell].horizontalOverflow = HorizontalWrapMode.Overflow; gridCellGlyphs[cell].verticalOverflow = VerticalWrapMode.Overflow;
            }
            for (int seat = 0; seat < 12; seat++)
            {
                int index = seat;
                gridTiles[seat] = MakeButton(gridScreen, Zodiac.Seats[seat].Name, -129 + (seat % 4) * 86, 344 + (seat / 4) * 44, 82, 40, () => PickSign(index));
                var name = gridTiles[seat].GetComponentInChildren<Text>(); name.fontSize = 11; name.horizontalOverflow = HorizontalWrapMode.Overflow;
                var nameRect = (RectTransform)name.transform; nameRect.anchoredPosition = new Vector2(10, -20); nameRect.sizeDelta = new Vector2(58, 36); gridTileNames[seat] = name;
                gridTileGlyphs[seat] = Label(gridTiles[seat].transform, Zodiac.Seats[seat].Glyph, -28, 20, 24, 36, 16); gridTileGlyphs[seat].font = Dial.GlyphFont; gridTileGlyphs[seat].horizontalOverflow = HorizontalWrapMode.Overflow; gridTileGlyphs[seat].verticalOverflow = VerticalWrapMode.Overflow;
            }
            gridReadout = Label(gridScreen, "", 0, 470, 340, 20, 12);
            var panel = Rect("Caspar panel", gridScreen, 0, 540, 324, 112); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 14, 290, 20, 13);
            gridCaspar = Label(panel, "", 0, 66, 306, 84, 12);
            gridStatus = Label(gridScreen, "", 0, 614, 330, 24, 12); gridStatus.color = Muted;
            gridSeal = MakeButton(gridScreen, "SEAL", 0, 654, 146, 56, GridSeal); gridSeal.GetComponent<Image>().color = Crimson;
            leaveGrid = MakeButton(gridScreen, "Leave the table", -72, 714, 128, 48, LeaveGrid); leaveGrid.GetComponentInChildren<Text>().fontSize = 13;
            gridAsk = MakeButton(gridScreen, "Ask Caspar for help", 78, 714, 164, 48, GridAsk); gridAsk.GetComponentInChildren<Text>().fontSize = 13; gridAsk.gameObject.SetActive(false);
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
            var glow = Rect("Shelf glow", shelf, 0, 18, 60, 46); shelfGlow = glow.gameObject.AddComponent<Image>(); shelfGlow.color = new Color(.95f, .8f, .5f, 0); shelfGlow.raycastTarget = false; glow.SetAsFirstSibling();
            Tappable(shelf, () => Walk("shelf")); // v0.3 revision: the book of symbols lives here once the wheel is lit
            var dial = Rect("The Dial", wingRoom, 30, 250, 200, 200); var dialImage = dial.gameObject.AddComponent<Image>(); dialImage.color = new Color(0, 0, 0, 0);
            RingLines(dial, 88, new Color(Bone.r, Bone.g, Bone.b, .4f), null); RingLines(dial, 30, new Color(Bone.r, Bone.g, Bone.b, .25f), null);
            var dialLabel = Label(wingRoom, "The Dial", 30, 352, 120, 16, 10); dialLabel.color = Muted;
            Tappable(dial, () => Walk("dial"));
            // Build B (07 Room Scope amendment): a second interactive object, the table with its board of twelve, dark until the modality unit is complete.
            var table = Block(wingRoom, "The table", -60, 372, 60, 30);
            for (int i = 0; i < 12; i++) { var square = Rect("Square", table, -20 + (i % 3) * 20, 5 + (i / 3) * 7, 16, 5); var squareImage = square.gameObject.AddComponent<Image>(); squareImage.color = new Color(.3f, .28f, .3f); squareImage.raycastTarget = false; }
            var tableGlow = Rect("Table glow", table, 0, 15, 72, 44); gridGlow = tableGlow.gameObject.AddComponent<Image>(); gridGlow.color = new Color(.95f, .8f, .5f, 0); gridGlow.raycastTarget = false; tableGlow.SetAsFirstSibling();
            Tappable(table, () => Walk("grid"));
            var door = Block(wingRoom, "Doorway back", -130, 325, 70, 100); Tappable(door, () => Walk("atrium-door"));
            var light = Rect("Doorway light", door, 0, 50, 50, 82); var lightImage = light.gameObject.AddComponent<Image>(); lightImage.color = new Color(.95f, .8f, .5f, .25f); lightImage.raycastTarget = false;
            var floor = Rect("Floor band", wingRoom, 0, BandY, 340, 30); var floorImage = floor.gameObject.AddComponent<Image>(); floorImage.color = new Color(.16f, .16f, .19f); floorImage.raycastTarget = false;
            wingRoomCaption = Label(wingRoom, "The Dial waits at the center of the room. The doorway leads back.", 0, 466, 340, 36, 12); // two lines at 360 wide wingRoomCaption.color = Muted; // placeholder (owner writes)
            enterGrid = MakeButton(wingRoom, "The table", 0, 512, 300, 52, () => Walk("grid")); enterGrid.gameObject.SetActive(false); // Build B: shown once the table has woken
            enterDial = MakeButton(wingRoom, "The Dial", 0, 624, 300, 52, () => Walk("dial"));
            enterShelf = MakeButton(wingRoom, "The bookshelf", 0, 568, 300, 52, () => Walk("shelf")); enterShelf.gameObject.SetActive(false);
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
            var walk = Flow.Walk; float band = walk.Room == Room.Chamber ? ChamberBandY : BandY;
            avatar.anchoredPosition = new Vector2(walk.X, -(band - 16) + walk.Bob);
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
            else if (command == "enter-chamber") EnterChamber();
            else if (command == "leave-chamber") LeaveChamber();
            else if (command == "restart") Restart();
            else if (command == "reload") { if (!busy) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); } // test-only: resume from the local save
            else if (command == "enter-wing") EnterWing();
            else if (command == "enter-dial") Walk("dial");
            else if (command == "enter-shelf") Walk("shelf");
            else if (command == "close-book") CloseBook();
            else if (command == "enter-grid") Walk("grid");
            else if (command == "leave-grid") LeaveGrid();
            else if (command == "grid-seal") GridSeal();
            else if (command == "grid-ask") GridAsk();
            else if (command.StartsWith("grid-sign:") && int.TryParse(command.Substring(10), out int gridSign) && gridSign >= 0 && gridSign < 12) PickSign(gridSign);
            else if (command.StartsWith("grid-cell:") && int.TryParse(command.Substring(10), out int gridCell) && gridCell >= 0 && gridCell < 12) ChooseCell(gridCell);
            else if (command.StartsWith("walk:")) Walk(command.Substring(5));
            else if (command == "walk-speed") CycleWalkSpeed();
            else if (command == "enter-seals") EnterSeals();
            else if (command == "leave-wing") LeaveWing();
            else if (command == "leave-review") LeaveReview();
            else if (command.StartsWith("element:") && int.TryParse(command.Substring(8), out int element) && element >= 0 && element < 4) AnswerTap(Elements[element]);
            else if (command.StartsWith("modality:") && int.TryParse(command.Substring(9), out int modality) && modality >= 0 && modality < 3) AnswerModalityTap(Zodiac.Modalities[modality]);
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
        void Insert()
        {
            if (busy) return;
            if (Flow.AtChamberRoom) { if (Flow.Walk.At != "books" || !Flow.SpendKey()) return; Save(); StartCoroutine(Spend()); return; } // Build D
            if (Page < ChamberPages.Length - 1 || !Flow.InsertKey()) return; StartCoroutine(Chandelier());
        }
        // ---- Build D: the Chamber as a room ----
        void EnterChamber() { Walk("chamber-door"); }
        void LeaveChamber() { if (busy || !Flow.AtChamberRoom) return; Walk("atrium-door"); }
        // A Key spent: its lock lights; the third lock opens the Book (light, a page turning); the fourth Key starts the second Book and ends the Wing.
        IEnumerator Spend()
        {
            busy = true; int spent = Flow.LocksFilled; insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0);
            locks[spent - 1].color = Bone;
            bool opened = spent % SliceFlow.LocksPerBook == 0; int book = (spent - 1) / SliceFlow.LocksPerBook;
            chamberLine = opened ? "The last lock turns. Something inside the crystal moves." : spent == 4 ? "The second Book takes its first Key." : "Lock " + (spent % SliceFlow.LocksPerBook) + " of three. The Book holds it."; // placeholder (owner writes)
            ShowChamberRoom(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? 1f : 1.4f);
            if (opened)
            {
                yield return Tween(ReducedMotion ? 0 : .8f, k => { bookImages[book].color = Color.Lerp(Dim, new Color(.42f, .38f, .32f), k); bookPages[book].color = new Color(Bone.r, Bone.g, Bone.b, .9f * k); bookPages[book].rectTransform.anchoredPosition = new Vector2(0, -35 + 18 * k); });
                Candles(true);
                chamberLine = "The first Book opens. Light spills from its pages, and the room takes a breath.\nEvery lock it had is turned."; // placeholder (owner writes)
                ShowChamberRoom(); Publish();
                yield return new WaitForSecondsRealtime(ReducedMotion ? 1.2f : 2.2f);
            }
            if (Flow.WingWhole)
            {
                chamberLine = ChamberWholeLine; ShowChamberRoom(); Publish();
                yield return new WaitForSecondsRealtime(ReducedMotion ? 1f : 1.8f);
            }
            busy = false; Show(); Publish();
        }
        void ShowChamberRoom()
        {
            bool room = Flow.AtChamberRoom; bool atBooks = Flow.Walk.At == "books";
            chamberDoor.gameObject.SetActive(room); chamberBooksTap.gameObject.SetActive(room); chamberBand.gameObject.SetActive(room); chamberBooksLabel.gameObject.SetActive(!room);
            chamberBack.gameObject.SetActive(room); chamberBack.interactable = !busy; chamberContinue.gameObject.SetActive(!room && chamberContinue.gameObject.activeSelf);
            if (!room) return;
            for (int l = 0; l < locks.Length; l++) locks[l].color = l < Flow.LocksFilled ? Bone : new Color(.3f, .3f, .33f);
            for (int b = 0; b < SliceFlow.Books; b++) { bool open = b < Flow.BooksOpen; if (!busy) { bookImages[b].color = open ? new Color(.42f, .38f, .32f) : Dim; bookPages[b].color = new Color(Bone.r, Bone.g, Bone.b, open ? .9f : 0); bookPages[b].rectTransform.anchoredPosition = new Vector2(0, open ? -17 : -35); } }
            if (Flow.BooksOpen > 0) Candles(true);
            bool canSpend = Flow.CanSpend && atBooks && !busy;
            insert.gameObject.SetActive(Flow.CanSpend && atBooks); insert.interactable = canSpend; insert.GetComponentInChildren<Text>().text = Flow.KeysInHand > 1 ? "Insert a Key (" + Flow.KeysInHand + " in hand)" : "Insert the Key";
            if (!canSpend) insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0);
            chamberEnd.gameObject.SetActive(Flow.WingWhole && !busy); chamberEnd.text = ChamberEndCard; chamberEnd.rectTransform.anchoredPosition = new Vector2(0, -250); // between the mechanism and the Books, clear of the Keeper; the first visit's card sits lower
            if (string.IsNullOrEmpty(chamberLine)) chamberLine = DefaultChamberLine();
            chamberText.text = chamberLine;
        }
        // Caspar's line in the Chamber room: set on arrival at the doorway or the Books, and by each spend beat; a beat's line stays until the next move.
        string DefaultChamberLine() => Flow.WingWhole && Flow.KeysInHand == 0 ? ChamberWholeLine : Flow.KeysInHand == 0 ? ChamberQuietLine : Flow.Walk.At == "books" ? ChamberChooseLine : (Flow.KeysInHand > 1 ? Flow.KeysInHand + " Keys in your hand. " : "A Key in your hand. ") + ChamberBringLine; // placeholder (owner writes)
        void Restart() { if (busy) return; PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        // Q06 phase 2, decision 5: the buttons and the taps do the same thing through the same walk.
        void EnterWing() { Walk("wing-door"); }
        void EnterDialNow()
        {
            if (!Flow.EnterDial()) return;
            Dial.SliceHidesOptional = true; Dial.Lesson.SetKey3(Flow.Keys >= 3);
            if (Dial.Lesson.CanContinueUnit) Dial.Lesson.BeginContinuation();
            else if (Dial.Lesson.CanBeginGlyphs && Dial.Lesson.AllNamed) { if (Dial.Lesson.BeginGlyphs()) Save(); } // Part B: the wheel hides its names
            else if (Dial.Lesson.CanBeginGlyphs) Dial.Lesson.Say(DialLesson.ShelfFirst); // the lit wheel, read only, until the book is read
            else if (Dial.Lesson.Phase != LessonPhase.GlyphWheel && Dial.Lesson.CanBeginModalities) { if (Dial.Lesson.BeginModalities()) { Flow.StartModalities(); Save(); } } // Build A: the second pattern, after Key 2
            else if (Dial.Lesson.CanBeginOpposites) { if (Dial.Lesson.BeginOpposites()) { Flow.StartOpposites(); Save(); } } // Build C: the last pattern and the builder, after Key 3
            Show(); Dial.Realign(); Publish();
        }
        void OpenBook()
        {
            if (!Flow.EnterBook()) return;
            if (Dial.Lesson.CanBeginGlyphs && !Dial.Lesson.AllNamed) { if (Dial.Lesson.BeginGlyphs()) { Flow.StartGlyphs(); Save(); } }
            else if (Dial.Lesson.CanPractice) Dial.Lesson.BeginPractice(); // after Key 2 the book tests again, harder after a clean run
            else Dial.Lesson.Say(DialLesson.ShelfRead); // Part A done or Key 2 earned: the book only shows its pages closed
            Show(); Publish();
        }
        void CloseBook() { if (busy || !Flow.LeaveBook()) return; Dial.Lesson.AbandonPractice(); Save(); Show(); Publish(); }
        // ---- Build B: the table ----
        void EnterGridNow()
        {
            if (!Flow.EnterGrid()) return;
            if (Grid.Begin()) { Flow.StartGrid(); Save(); } // the first opening introduces the twelve sign → cell items (deck as data)
            demoCell = -1; Show(); Publish();
        }
        void LeaveGrid() { if (busy || !Flow.LeaveGrid()) return; Save(); Show(); Publish(); }
        void PickSign(int seat) { if (busy || Flow.Screen != SliceScreen.Grid || !Grid.Pick(seat)) return; ShowGrid(); Publish(); }
        void ChooseCell(int cell) { if (busy || Flow.Screen != SliceScreen.Grid || !Grid.Choose(cell)) return; ShowGrid(); Publish(); }
        void GridAsk() { if (busy || Flow.Screen != SliceScreen.Grid || !Grid.Ask()) return; ShowGrid(); Publish(); }
        void GridSeal()
        {
            if (busy || Flow.Screen != SliceScreen.Grid) return;
            var result = Grid.Seal(); if (result == null) return;
            if (result.correctness) StartCoroutine(GridSeated());
            else if (Grid.Demonstrating) StartCoroutine(GridDemonstrate());
            else { ShowGrid(); Publish(); }
        }
        IEnumerator GridSeated()
        {
            busy = true; ShowGrid(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .6f : 1.1f);
            busy = false; ShowGrid(); Publish();
        }
        // Level 3: Caspar names the rule, the cell lights, the sign lands. Exposure only, never evidence.
        IEnumerator GridDemonstrate()
        {
            busy = true; ShowGrid(); Publish();
            float beat = ReducedMotion ? DialView.ReducedBeatSeconds : DialView.BeatSeconds;
            yield return new WaitForSecondsRealtime(beat);
            demoCell = Grid.DemonstrationCell; ShowGrid(); Publish();
            yield return new WaitForSecondsRealtime(beat);
            demoCell = -1; Grid.AfterDemonstration(); Save(); ShowGrid(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .6f : 1.2f);
            busy = false; ShowGrid(); Publish();
        }
        void Walk(string id)
        {
            if (busy || (Flow.Screen != SliceScreen.Hub && Flow.Screen != SliceScreen.WingRoom && Flow.Screen != SliceScreen.ChamberRoom)) return;
            var poi = Flow.Walk.Find(id); if (poi == null) return;
            if (!poi.Walkable) { if (Flow.TouchSealedDoor()) { hubNote.text = Flow.Note; Publish(); } return; }
            if (id == "shelf" && !Flow.WheelComplete) { if (Flow.TouchDarkShelf()) { wingRoomCaption.text = DialLesson.ShelfDark; Publish(); } return; }
            if (id == "grid" && !Flow.CanOpenGrid) { if (Flow.TouchDarkGrid()) { wingRoomCaption.text = GridModel.DarkLine; Publish(); } return; } // Build B: dark and tappable with a note before the unit
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
            if (id == "wing-door" || id == "atrium-door" || id == "chamber-door")
            {
                yield return FadeTo(1);
                if (id == "wing-door") Flow.EnterWing(); else if (id == "chamber-door") { Flow.EnterChamber(); chamberLine = DefaultChamberLine(); } else if (Flow.Walk.Room == Room.Chamber) { Flow.LeaveChamber(); Save(); } else { Flow.LeaveWing(); Save(); }
                Show(); PlaceAvatar(); Publish();
                yield return new WaitForSecondsRealtime(ReducedMotion ? 0 : .12f);
                yield return FadeTo(0);
            }
            else if (id == "desk") { if (!Flow.EnterSeals()) hubNote.text = Flow.Note; else { Show(); StartReviewItem(); } }
            else if (id == "caspar") { Flow.ApproachCaspar(); hubNote.text = Flow.Note; }
            else if (id == "dial") EnterDialNow();
            else if (id == "shelf") OpenBook();
            else if (id == "grid") EnterGridNow();
            else if (id == "books") { chamberLine = DefaultChamberLine(); ShowChamberRoom(); } // the Insert button waits at the Books
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
            busy = false;
            if (Dial.Lesson.Phase == LessonPhase.GlyphWheel) { Flow.LeaveBook(); Save(); } // Part A done: back to the room; the wheel runs Part B
            Show(); Publish();
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
            if (task.Mode == ReviewMode.Dial || task.Mode == ReviewMode.DialModality)
            {
                Dial.SliceHidesOptional = true;
                Dial.Lesson.BeginReview(task.seat, task.Mode == ReviewMode.DialModality ? 3 : 4); Show(); Dial.Realign(); Publish();
            }
            else { Show(); Publish(); }
        }
        IEnumerator AfterDialReview(bool correct, bool eligible)
        {
            busy = true; Flow.FinishReview(correct, eligible); Save(); Dial.ForceRefresh(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .8f : 1.4f);
            Dial.Lesson.EndReview(); Dial.Realign(); busy = false; StartReviewItem();
        }
        void AnswerModalityTap(string modality)
        {
            if (busy) return;
            var task = Flow.CurrentReview; if (task == null || task.Mode != ReviewMode.TapModality) return;
            Flow.AnswerModalityTap(modality); Dial.Lesson.Dial.Log("tap_answered");
            reviewNote.text = Flow.Note; Save(); Publish();
            if (task.done) StartCoroutine(AfterTap());
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
            bool reviewOnDial = s == SliceScreen.Review && task != null && (task.Mode == ReviewMode.Dial || task.Mode == ReviewMode.DialModality) && !task.done;
            identity.gameObject.SetActive(s == SliceScreen.Identity); birth.gameObject.SetActive(s == SliceScreen.Birth);
            atrium.gameObject.SetActive(s == SliceScreen.Atrium); atriumReturn.gameObject.SetActive(s == SliceScreen.AtriumReturn);
            chamber.gameObject.SetActive(s == SliceScreen.Chamber || s == SliceScreen.ChamberRoom); hub.gameObject.SetActive(s == SliceScreen.Hub);
            wingRoom.gameObject.SetActive(s == SliceScreen.WingRoom);
            gridScreen.gameObject.SetActive(s == SliceScreen.Grid); if (s == SliceScreen.Grid) ShowGrid();
            bool book = s == SliceScreen.Book;
            bool roomScreen = s == SliceScreen.Hub || s == SliceScreen.WingRoom || s == SliceScreen.ChamberRoom;
            if (roomScreen) { avatar.SetParent(s == SliceScreen.Hub ? hub : s == SliceScreen.WingRoom ? wingRoom : chamber, false); avatar.SetAsLastSibling(); PlaceAvatar(); }
            avatar.gameObject.SetActive(roomScreen);
            if (s == SliceScreen.WingRoom)
            {
                enterDial.interactable = !busy; wingRoomBack.interactable = !busy;
                enterShelf.gameObject.SetActive(Flow.WheelComplete); enterShelf.interactable = !busy;
                shelfGlow.color = new Color(.95f, .8f, .5f, Flow.WheelComplete && !Dial.Lesson.AllNamed ? .35f : Flow.WheelComplete ? .12f : 0);
                enterGrid.gameObject.SetActive(Flow.CanOpenGrid); enterGrid.interactable = !busy;
                gridGlow.color = new Color(.95f, .8f, .5f, Flow.ModalitiesComplete && !Grid.Key3Earned ? .35f : Flow.ModalitiesComplete ? .12f : 0); // an instrument with a unit waiting glows, like the shelf
                wingRoomCaption.text = Flow.Note == "shelf-dark" ? DialLesson.ShelfDark
                    : Flow.Note == "grid-dark" ? GridModel.DarkLine
                    : Dial.Lesson.Phase == LessonPhase.GlyphWheel ? "The wheel has hidden its names. Go to the Dial and find each symbol in turn." // placeholder (owner writes)
                    : Dial.Lesson.CanBeginModalities && Dial.Lesson.Phase != LessonPhase.GlyphWheel ? "The wheel keeps a second pattern. Go to the Dial." // placeholder (owner writes)
                    : Flow.ModalitiesComplete && !Grid.Key3Earned ? (Grid.PlacedCount > 0 ? "The table waits, part seated. Go to it." : "A table has woken beside the wheel. Go to it.") // placeholder (owner writes; Build B)
                    : Flow.Keys >= 3 && !Dial.Lesson.Key4Earned ? (Dial.Lesson.OppositesStarted ? "The wheel's last pattern waits. Go to the Dial." : "The wheel keeps one last pattern for you. Go to the Dial.") // placeholder (owner writes; Build C)
                    : Dial.Lesson.CanPractice ? (Dial.Lesson.Hard ? "The symbols are yours. The book will test you again, harder." : "The symbols are yours. The book will test you again.") // placeholder (owner writes)
                    : Flow.WheelComplete && !Dial.Lesson.AllNamed ? "The wheel is lit. Something on the shelf has woken with it." // placeholder (owner writes)
                    : "The Dial waits at the center of the room. The doorway leads back."; // placeholder (owner writes)
            }
            bool partA = book;
            review.gameObject.SetActive(s == SliceScreen.Review && !reviewOnDial);
            glyphs.gameObject.SetActive(partA);
            Dial.UiCanvas.gameObject.SetActive(s == SliceScreen.Wing || reviewOnDial);
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
            if (s == SliceScreen.Chamber || s == SliceScreen.ChamberRoom) ShowChamberRoom(); // after ShowPage: the room owns the Chamber's controls
        }
        void ShowGlyphs()
        {
            var lesson = Dial.Lesson; int target = lesson.CurrentGlyph;
            bool naming = lesson.Phase == LessonPhase.GlyphNames;
            closeBook.interactable = !busy;
            if (!naming) { glyphProgress.text = ""; glyphCard.text = ""; for (int i = 0; i < 4; i++) glyphNameButtons[i].GetComponentInChildren<Text>().text = ""; glyphCaspar.text = lesson.Message; glyphNote.text = ""; for (int i = 0; i < 4; i++) glyphNameButtons[i].interactable = false; return; } // after the twelfth answer the wheel already owns the index; the last card stays up through the hold
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
        void ShowGrid()
        {
            var g = Grid; bool active = g.Active && !busy;
            gridCaspar.text = g.Message; gridReadout.text = g.Readout;
            gridStatus.text = g.Phase == GridPhase.Complete ? "Twelve seated. Keeper Key 3 earned." : g.Phase == GridPhase.Paused ? "Paused for now" : "The table · " + g.PlacedCount + " of 12 seated"; // no level numbers on screen (owner, Sept 13)
            gridKeys.text = "Keeper Keys: " + Flow.Keys; gridKeys.gameObject.SetActive(Flow.Keys > 0);
            for (int i = 0; i < 12; i++)
            {
                bool seated = g.Placed[i], inHand = g.Sign == i;
                gridTiles[i].interactable = active && g.CanPick(i);
                gridTiles[i].GetComponent<Image>().color = seated ? TileGone : inHand ? Held : Dim;
                gridTileNames[i].color = gridTileGlyphs[i].color = seated ? new Color(Bone.r, Bone.g, Bone.b, .3f) : Bone;
                int seat = GridModel.SeatOf(i); bool filled = g.Placed[seat];
                gridCells[i].interactable = active && g.CanChoose(i);
                gridCells[i].GetComponent<Image>().color = filled ? Seated : i == g.Cell || i == demoCell ? Held : Dim;
                gridCellNames[i].text = filled ? Zodiac.Seats[seat].Name : i == g.Rejected ? "×" : "";
                gridCellGlyphs[i].text = filled ? Zodiac.Seats[seat].Glyph : "";
            }
            gridSeal.interactable = active && g.CanSeal;
            gridAsk.gameObject.SetActive(g.CanAsk && !busy); gridAsk.interactable = active && g.CanAsk;
            leaveGrid.interactable = !busy;
        }
        void ShowHub()
        {
            int stage = Flow.AtriumStage;
            // Build D: one step of dressing per Key spent (placeholder): lamps, the shelves take their books back, light behind a sealed door.
            lampOne.color = stage >= 2 ? LampLit : LampDark; lampTwo.color = stage >= 3 ? LampLit : LampDark; lampThree.color = stage >= 5 ? LampLit : LampDark; lampFour.color = stage >= 6 ? LampLit : LampDark;
            foreach (var book in shelfBooks) book.gameObject.SetActive(stage >= 4); shelvesLabel.text = stage >= 4 ? "Shelves, filling" : "Shelves, mostly empty";
            sealedLeftLight.color = new Color(.95f, .8f, .5f, stage >= 5 ? .3f : 0);
            hubCaption.text = stage >= 6 ? "Awake: four lamps, shelves filling. The Wing is whole." : stage >= 5 ? "Waking: three lamps, and light behind a sealed door." : stage >= 4 ? "Waking: the shelves take their books back." : stage >= 3 ? "Stirring: two lamps, a clear desk, the Wing open." : "Stirring: one lamp lit, one desk uncovered, the Wing open."; // placeholder (owner writes)
            hubText.text = Flow.KeysInHand > 1 ? string.Format(HubKeysInHandLine, Flow.KeysInHand) : Flow.KeysInHand == 1 ? HubKeyInHandLine : Flow.WingWhole ? HubWholeLine : Flow.LocksFilled >= 3 ? HubSpent3Line : Flow.LocksFilled >= 2 ? HubSpent2Line : Flow.V02Complete ? HubCompleteLine : Resumed || Flow.ReviewsChecked > 0 ? HubLaterLine : HubFirstLine;
            enterChamber.interactable = !busy && Flow.CanEnterChamber;
            int due = Flow.DueCount;
            enterSealsLabel.text = "Check the Seals"; // no count on the button (owner, Sept 14); the review screen shows n of m
            enterWing.GetComponentInChildren<Text>().text = "The Zodiac Wing";
            endCard.text = Flow.WingWhole ? ChamberEndCard : Flow.Keys >= 4 ? "Four Keys earned. The Chamber will take them." : Flow.Keys >= 3 ? "Three Keys earned. The Chamber will take them." : Flow.Keys >= 2 ? "Two Keys earned. The Chamber will take them." : "End of prototype v0.2. Glyphs and Key 2 come next."; // the prototype end cards give way to the Wing's (Build D)
            hubNote.text = Flow.Note;
            endCard.gameObject.SetActive(Flow.V02Complete || Flow.Keys >= 2);
        }
        void ShowReview()
        {
            var task = Flow.CurrentReview;
            bool done = Flow.ReviewDone;
            reviewProgress.text = done ? "" : (Flow.ReviewIndex + 1) + " of " + Flow.ReviewQueue.Count;
            bool glyphItem = !done && task != null && task.Mode == ReviewMode.Glyph;
            bool modItem = !done && task != null && task.Mode == ReviewMode.TapModality;
            reviewQuestion.text = done ? "" : task != null && task.Mode == ReviewMode.Tap ? Zodiac.Seats[task.seat].Name + ". Which family?" : modItem ? Zodiac.Seats[task.seat].Name + ". Which kind?" : glyphItem ? "Which sign carries this symbol?" : "";
            foreach (var b in elementButtons) { b.gameObject.SetActive(!done && task != null && task.Mode == ReviewMode.Tap); b.interactable = !busy && task != null && !task.done; }
            foreach (var b in modalityButtons) { b.gameObject.SetActive(modItem); b.interactable = !busy && task != null && !task.done; }
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
            if (Flow.Screen == SliceScreen.Chamber) { chamberEnd.text = "The first Key is spent. The Library has taken her first breath."; chamberEnd.rectTransform.anchoredPosition = new Vector2(0, -720); }
            if (!chamberReady && insertGlow != null) insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0);
        }
        void LightWing() { foreach (var line in floorLines) if (line != null) line.color = new Color(.62f, .57f, .53f, .8f); candle.color = LampLit; }
        public void Publish() => Dial.Publish();
        public void Fill(DialView.WebState state)
        {
            var s = Flow.Screen; var task = Flow.CurrentReview;
            bool reviewOnDial = s == SliceScreen.Review && task != null && (task.Mode == ReviewMode.Dial || task.Mode == ReviewMode.DialModality) && !task.done;
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
            state.canInsert = (s == SliceScreen.Chamber && !Flow.KeyInserted && Page >= ChamberPages.Length - 1 && !busy) || (s == SliceScreen.ChamberRoom && Flow.CanSpend && Flow.Walk.At == "books" && !busy);
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
            state.reviewMode = s != SliceScreen.Review ? "" : Flow.ReviewDone ? "done" : task != null && (task.Mode == ReviewMode.Dial || task.Mode == ReviewMode.DialModality) ? "dial" : task != null && task.Mode == ReviewMode.TapModality ? "modality" : "tap";
            state.reviewIndex = Flow.ReviewIndex; state.reviewTotal = Flow.ReviewQueue.Count; state.reviewSign = task != null && !Flow.ReviewDone ? Zodiac.Seats[task.seat].Name : "";
            state.reviewSummary = Flow.ReviewSummary; state.hubNote = hubNote != null ? hubNote.text : ""; state.v02Complete = Flow.V02Complete;
            bool partA = s == SliceScreen.Book && Dial.Lesson.Phase == LessonPhase.GlyphNames;
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
            bool roomScreen = s == SliceScreen.Hub || s == SliceScreen.WingRoom || s == SliceScreen.ChamberRoom;
            var walk = Flow.Walk;
            state.room = !roomScreen ? "" : walk.Room == Room.Atrium ? "atrium" : walk.Room == Room.Wing ? "wing" : "chamber";
            state.avatarX = walk.X; state.walking = walk.Walking; state.walkTarget = walk.TargetId; state.avatarAt = walk.At; state.walkSpeed = walk.SpeedName;
            var pois = roomScreen ? Rooms.Visible(walk.Room).ToArray() : new PointOfInterest[0];
            state.pois = pois.Select(p => p.Id).ToArray(); state.poiLabels = pois.Select(p => (p.Walkable ? "Walk to " : "") + p.Label).ToArray();
            state.canWalk = roomScreen && !busy; state.canEnterDial = s == SliceScreen.WingRoom && !busy; state.canEnterShelf = s == SliceScreen.WingRoom && Flow.WheelComplete && !busy;
            if (s == SliceScreen.WingRoom) { state.caspar = wingRoomCaption.text; state.canLeaveWing = !busy; }
            // Build D
            state.keysInHand = Flow.KeysInHand; state.keysSpent = Flow.KeysSpent; state.booksOpen = Flow.BooksOpen; state.wingWhole = Flow.WingWhole;
            state.canEnterChamber = s == SliceScreen.Hub && Flow.CanEnterChamber && !busy; state.canLeaveChamber = s == SliceScreen.ChamberRoom && !busy; state.atBooks = s == SliceScreen.ChamberRoom && walk.At == "books";
            if (s == SliceScreen.ChamberRoom) state.caspar = chamberText.text + (Flow.WingWhole && !busy ? " " + ChamberEndCard : "");
            state.canCloseBook = s == SliceScreen.Book && !busy;
            if (s == SliceScreen.Book && !partA) state.caspar = Dial.Lesson.Message;
            if (roomScreen && !busy) state.note = hubNote.text;
            // Build B: the table
            state.gridOpen = Flow.ModalitiesComplete; state.gridStarted = Flow.GridStarted; state.key3 = Grid.Key3Earned; state.gridPlaced = Grid.PlacedCount;
            state.gridComplete = Grid.Complete; state.gridPaused = Grid.Phase == GridPhase.Paused; state.gridHintLevel = Grid.HintLevel;
            state.canEnterGrid = s == SliceScreen.WingRoom && Flow.CanOpenGrid && !busy;
            state.keys = Math.Max(state.keys, Flow.Keys); // the lesson counts two Keys; the table adds the third
            if (s == SliceScreen.Grid)
            {
                state.caspar = Grid.Message; state.gridReadout = Grid.Readout; state.gridStatus = gridStatus.text;
                state.gridSign = Grid.Sign >= 0 ? Zodiac.Seats[Grid.Sign].Name : ""; state.gridCell = Grid.Cell; state.gridLocked = Grid.Locked;
                state.gridTiles = Enumerable.Range(0, 12).Select(i => Grid.TileLabel(i)).ToArray(); state.gridCells = Enumerable.Range(0, 12).Select(i => Grid.CellLabel(i)).ToArray();
                state.canGridPick = Grid.Active && !busy; state.canGridSeal = Grid.CanSeal && !busy; state.canGridAsk = Grid.CanAsk && !busy; state.canLeaveGrid = !busy;
            }
        }

        // ---- save / restore (Q05 decision 5) ----
        void Save()
        {
            if (Flow.AtriumStage < 2) return;
            try { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Flow.ToSave(Dial.Lesson.Lit, Dial.Lesson.Kin, Dial.Lesson.KeyEarned, Dial.Lesson.LitMod, Dial.Lesson.KinMod, Grid.Placed, Grid.Evidence, Dial.Lesson.PolarityShown, Dial.Lesson.OppKnown, Dial.Lesson.Built, Dial.Lesson.BuilderEvidence))); PlayerPrefs.Save(); }
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
            Dial.Lesson.RestoreGlyphs(save.glyphStage, save.glyphIndex, save.keys >= 2); Dial.Lesson.SetCleanRuns(save.cleanRuns);
            Dial.Lesson.RestoreModalities(save.litMod, save.kinMod, save.modalitiesStarted);
            Grid.Restore(save.gridPlaced, save.gridEvidence, save.gridStarted, save.keys >= 3);
            Dial.Lesson.SetKey3(save.keys >= 3); Dial.Lesson.RestoreOpposites(save.polarityShown, save.oppKnown, save.oppositesStarted, save.built, save.builderEvidence, save.keys >= 4);
            if (save.keys >= 2) keyIndicator.text = "Keeper Keys: " + Math.Max(2, save.keys);
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
                chandelierLabel.text = "The chandelier, lit";
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
        void Candles(bool on) { foreach (var c in candles) c.color = on ? Bone : LampDark; chandelierLabel.text = on ? "The chandelier, lit" : "The chandelier, dark"; }
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
