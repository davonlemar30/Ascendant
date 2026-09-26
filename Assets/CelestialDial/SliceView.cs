using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Vertical slice v0.2 "the return": the locked opening and Chamber bookends (Q01, Q04, Q06, Q07), the September 11
    // copy session, and the locked loop (Q05) as amended Sept 15/17: one-entrance Atrium, the practice fork on the Dial, the three-strikes
    // gate, the journal in the inventory (Build F), Unit 1.1 continuation, local save.
    // Placeholder art. Copy is the v0.1 Copy Deck; v0.2 lines marked "placeholder (owner writes)" are not final.
    // Build E: every placeholder is a named art slot and every action a sound slot (Slots, Sound); a file in a slot replaces
    // the grey box or the silence, nothing else changes. ?style shows every slot at once.
    public sealed class SliceView : MonoBehaviour, DialView.ISliceState
    {
        public SliceFlow Flow { get; private set; } = new SliceFlow();
        public DialView Dial { get; private set; }
        public GridModel Grid { get; private set; } // Build B: the table (pure C#, beside the deck)
        public bool Busy => busy;
        public int Page { get; private set; }
        public bool Resumed { get; private set; }
        public bool ChamberPaging => chamberContinue.gameObject.activeSelf && !insert.gameObject.activeSelf; // fixture evidence
        public bool StyleShown => styleShown; // Build E: the style page instead of the game (fixture evidence)
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
        RectTransform gridScreen; Text gridCaspar, gridReadout, gridStatus, gridKeys; Button gridSeal, gridAsk, leaveGrid, enterGrid; Image gridGlow, dialGlow, lampThree, lampFour;
        // Build D: the Chamber as a room, the Books, and the Atrium's dressing per stage.
        Button enterChamber, chamberBack; Image sealedLeftLight; Text shelvesLabel, chamberBooksLabel, chandelierLabel; RectTransform chamberDoor, chamberBooksTap, chamberBand;
        readonly Image[] bookImages = new Image[SliceFlow.Books], bookPages = new Image[SliceFlow.Books], shelfBooks = new Image[3];
        const float ChamberBandY = 408f; // the Chamber's floor band sits under the Books, above Caspar's panel
        string chamberLine = "";
        // Build E: art slots that carry state or a frame, the sound toggle, and the style page.
        Image avatarArt, floorArt, bookCard; Sprite keeperIdle, keeperWalk; Button mute, styleMute; RectTransform style; bool styleShown; string styleSet; string[] styleSlots, styleSounds; // the page as built
        static readonly Color LockDark = new Color(.3f, .3f, .33f), BookOpen = new Color(.42f, .38f, .32f);
        const float DarkArt = .35f, ShutArt = .55f, LockDarkArt = .45f; // a file's brightness where the placeholder used a dark color
        readonly Button[] gridTiles = new Button[12], gridCells = new Button[12];
        readonly Text[] gridTileNames = new Text[12], gridTileGlyphs = new Text[12], gridCellNames = new Text[12], gridCellGlyphs = new Text[12];
        int demoCell = -1;
        const float KeeperScale = 100f / 44f; // Build L (owner, Sept 24): the Keeper stands 100 px tall, the Wing mockup's scale; his feet stay on the band
        const float BandY = 436f, FadeSeconds = .35f; // floor band and fade length are test variables (Q06 phase 2, decision 7)
        readonly Button[] glyphNameButtons = new Button[4];
        readonly Button[] reviewGlyphButtons = new Button[4];
        InputField nameField, dateField;
        Button birthContinue, wingContinue, insert, chamberContinue, atriumContinue, returnContinue, changeChoice;
        Button enterWing, hubRestart, leavePractice;
        // Build F: the fork and the practice exit on the Dial, the journal's buttons and screen.
        Button forkLesson, forkPractice, leavePracticeDial, journalHub, journalWing, journalChamber, journalPrev, journalNext, journalClose;
        RectTransform journal; Text journalSectionText, journalNote; Image journalCover; readonly Text[] journalGlyphs = new Text[12], journalLines = new Text[12];
        bool forkShown, gating;
        // Build H (the Sept 17 lighting decision, Option C): one golden-hour overlay per room, over the background and under everything else, faded by the Atrium stage.
        readonly List<Image> lightOverlays = new List<Image>(); float lightAlpha; Coroutine lightFade;
        public float LightAlpha => lightAlpha; // fixture evidence
        public static readonly string[] LightSlots = { "atrium-light", "wing-light", "chamber-light" };
        public static float LightAlphaFor(int stage) => stage <= 1 ? 0f : stage == 2 ? .25f : stage == 3 ? .5f : stage == 4 ? .75f : 1f; // nothing at Stage 1, full at Stages 5–6; the steps between are a tuning variable
        const string ForkLine = "The wheel is yours. We can go on with the lesson, or you can practice what you already know."; // placeholder (owner writes)
        const string ForkPracticeOnlyLine = "Nothing new waits on the wheel today. Practice what you know, or rest."; // placeholder (owner writes)
        const string ForkReturnLine = "Back at the wheel. The lesson, or more practice: your choice."; // placeholder (owner writes)
        readonly Button[] elementButtons = new Button[4];
        readonly Button[] modalityButtons = new Button[3]; // Build A: which kind?
        Image flash, seam, keyGlow, candle, insertGlow, lampOne, lampTwo, deskCloth, doorOpenLight;
        readonly Image[] floorLines = new Image[24];
        readonly Image[] candles = new Image[9];
        readonly Image[] locks = new Image[SliceFlow.Books * SliceFlow.LocksPerBook];
        RectTransform keyRect, mechanism, gridKey; Image gridKeyGlow; Text gridKeyLabel; // Build I: the table's Key
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
        const string HubFirstLine = "Ah look at that! That lamp just flickered on.\nThe wheel is still dark in the Zodiac Wing, when you are ready."; // owner (worksheet section 1)
        const string HubLaterLine = "Welcome, acolyte. Much of this place remains unawakened, halls sealed and dark beyond your reach.\nBut the Zodiac Wing stirs. The Dial awaits your hand when you are ready."; // owner (worksheet section 1)
        const string HubCompleteLine = "The whole wheel burns, acolyte, every seat alight as it was in the days before he departed this place.\nYour journal holds what you have learned, and I would counsel you to read it well, for the shelf has stirred and something within those pages calls to you now."; // owner (worksheet section 1)
        const string HubKey2Line = "Two Keys, acolyte. He left twenty-one locks upon this place, and you have turned two of them.\nThat is enough for now. Rest, and let the Library remember what you have done."; // owner (worksheet section 6)
        const string HubKey3Line = "Three Keys, acolyte. The table is full and the Zodiac Wing has one last pattern to teach you.\nRest now, and let the Library remember what you have done."; // owner (worksheet section 11)
        const string HubKey4Line = "Four Keys, acolyte. Every pattern the wheel held, you hold now.\nRest, for the Chamber will want to see what you carry."; // owner (worksheet section 12)
        // Build D placeholder lines (owner writes; worksheet section 13).
        const string HubKeyInHandLine = "You carry a Key the Chamber has not yet seen, acolyte. Its door stands open when you are ready."; // owner (worksheet section 13)
        const string HubKeysInHandLine = "You carry {0} Keys the Chamber has not yet seen. Its door stands open when you are ready."; // owner (worksheet section 13)
        const string HubSpent2Line = "Two locks filled, acolyte. The first Book is one Key from opening, and I confess the air in this room has changed.\nThe shelves are taking their books back."; // owner (worksheet section 13)
        const string HubSpent3Line = "Three locks turned, and the first Book breathes. He would not have believed it, acolyte, not in all his years.\nAnd look, there is light behind a sealed door now."; // owner (worksheet section 13)
        const string HubWholeLine = "Four Keys spent, acolyte. The Zodiac Wing is whole, and the second Book has taken its first Key.\nWhat remains is sealed, for now. Rest, and let the Library settle into what you have given it."; // owner (worksheet section 13)
        const string ChamberQuietLine = "The Books are quiet for now, acolyte. They will stir again when you carry the next Key."; // owner (worksheet section 13)
        const string ChamberWholeLine = "The first Book open, the second begun. The Zodiac Wing is whole, acolyte.\nWhat remains is sealed, for now."; // owner (worksheet section 13)
        const string ChamberBringLine = "Bring it to the Books."; // owner (worksheet section 13): "You hold a Key, acolyte. Bring it to the Books."
        const string ChamberChooseLine = "Choose a lock, acolyte, and give it your Key."; // owner (worksheet section 13)
        const string ChamberEndCard = "The Zodiac Wing is complete. The Library remembers, and the rest remains sealed, for now."; // owner (worksheet section 13); two lines at 330 wide
        bool ReducedMotion => Dial.Lesson.Dial.ReducedMotion;

        void Awake()
        {
            gameObject.name = "VerticalSlice";
            var dialObject = new GameObject("CelestialDial"); dialObject.transform.SetParent(transform, false);
            Dial = dialObject.AddComponent<DialView>();
            Dial.Slice = this; Dial.ExtraActions = WebAction; font = Dial.UiFont;
            Flow.Logged += name => Dial.Lesson.Dial.Log(name.ToLowerInvariant().Replace(':', '_'), false, false, "slice");
            Dial.Lesson.ReviewFinished += (seat, correct, eligible) => StartCoroutine(AfterDialReview(correct, eligible));
            Dial.Lesson.Dial.Logged += e => { if (e.event_name == "answer_rejected" && Flow.AtPractice && Dial.Lesson.Phase == LessonPhase.Review) Flow.RecordStrike(); }; // Build F: a wheel miss in practice is a strike; the tap forms count their own
            Grid = new GridModel(() => Time.realtimeSinceStartupAsDouble);
            Grid.Logged += e => Debug.Log("[CelestialDial] " + JsonUtility.ToJson(e));
            Flow.ObserveProgress(Dial.Lesson, Grid, Save);
            Dial.Lesson.PracticeFinished += clean => Publish();
            Grid.Logged += e => Sound.Play(Sound.Cue(e.event_name, e.correctness, false)); // Build E: a pick is a step, a seating a seal, a miss a miss, Key 3 a key
            Flow.Logged += n => { if (n == "key_spent" || n == "key_inserted") Sound.Play("key"); };
            var canvasObject = new GameObject("Slice Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1;
            root = Rect("Slice Portrait", canvasObject.transform, 0, 0, 360, 800);
            BuildIdentity(); BuildBirth();
            atrium = BuildAtrium("Atrium", out atriumText, out atriumContinue);
            atriumReturn = BuildAtrium("Atrium return", out returnText, out returnContinue);
            BuildChamber(); BuildHub(); BuildReview(); BuildGlyphs(); BuildGrid(); BuildWingExtras(); BuildWingRoom(); BuildJournal(); BuildAvatar(); BuildFade();
            var flashObject = new GameObject("White light", typeof(RectTransform), typeof(Canvas));
            flashObject.transform.SetParent(transform, false);
            flashCanvas = flashObject.GetComponent<Canvas>(); flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay; flashCanvas.sortingOrder = 10;
            flash = flashObject.AddComponent<Image>(); flash.color = new Color(1, 1, 1, 0); flash.raycastTarget = false;
            if (Slots.StyleRequested) { BuildStyle(); ShowStyle(); Publish(); return; } // Build E: the style page instead of the game; the save is not touched
            TryRestore();
            Show(); Publish();
        }
        void Update()
        {
            if (canvas.pixelRect.width >= 1) canvas.scaleFactor = Mathf.Min(canvas.pixelRect.width / 360f, canvas.pixelRect.height / 800f);
            if (styleShown) return;
            if (avatar != null && avatar.gameObject.activeInHierarchy) PlaceAvatar();
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.KeyEarned && !revealStarted && !Dial.Busy) StartCoroutine(Reveal());
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.Phase == LessonPhase.AllLit && !Flow.WheelComplete) { Flow.MarkWheelComplete(); Save(); LightWing(); Show(); Publish(); }
            if ((Flow.Screen == SliceScreen.Wing || Flow.Screen == SliceScreen.Practice) && Dial.Lesson.Key2Earned && Flow.Keys < 2) { Flow.MarkKey2(); Save(); StartCoroutine(KeyCeremony(2)); }
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.ModalitiesComplete && !Flow.ModalitiesComplete) { Flow.MarkModalitiesComplete(); Save(); Publish(); } // Build B: the table wakes
            if (Flow.Screen == SliceScreen.Grid && Grid.Key3Earned && Flow.Keys < 3) { Flow.MarkKey3(); Save(); StartCoroutine(KeyCeremony(3)); }
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.Key4Earned && Flow.Keys < 4) { Flow.MarkKey4(); Save(); StartCoroutine(KeyCeremony(4)); } // Build C; Build I: every Key rises
            if (insertGlow != null && insert.gameObject.activeInHierarchy && insert.interactable && (!Flow.KeyInserted || Flow.CanSpend))
            {
                float a = ReducedMotion ? .35f : .15f + .3f * Mathf.PingPong(Time.unscaledTime / 1.2f, 1f);
                insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, a);
            }
            if (Flow.Screen == SliceScreen.WingRoom && DialUnitWaiting && !ReducedMotion) dialGlow.color = new Color(.95f, .8f, .5f, .35f + .35f * Mathf.PingPong(Time.unscaledTime / 1.4f, 1f)); // Build I: reduced motion holds it still
            PulseDoors(); // Build N: unlocked doors breathe light at their edges
            if (Flow.Gated && !busy && !Dial.Busy && !gating) StartCoroutine(Gate()); // Build F: the third strike closes the instrument once the answer's beat has settled
            if (wingContinue != null && Flow.AtriumStage >= 2)
            {
                // The Wing's Back button belongs to the Wing screen only; left on, it sat over the review's Seal (owner playtest, Sept 14).
                bool idle = Flow.Screen == SliceScreen.Wing && !Dial.Lesson.Dial.Active && !Dial.Busy && !busy && Dial.Lesson.Phase != LessonPhase.Review && !Dial.ControlsShown; // Build C: nor over the beat's Continue or the builder's buttons
                if (wingContinue.gameObject.activeSelf != idle) { wingContinue.gameObject.SetActive(idle); Publish(); }
                // Build F: the fork sits on the row below Back, under the same idle rule, only while the fork is open.
                bool fork = idle && forkShown && Flow.CanEnterPractice, lesson = fork && LessonAvailable;
                if (forkPractice.gameObject.activeSelf != fork || forkLesson.gameObject.activeSelf != lesson) { forkPractice.gameObject.SetActive(fork); forkLesson.gameObject.SetActive(lesson); Publish(); }
            }
        }

        // ---- screens ----
        void BuildIdentity()
        {
            identity = ScreenPanel("Identity");
            Label(identity, "WHO ARE YOU?", 0, 200, 320, 34, 22);
            FaintRing(identity, new Vector2(0, -420), 110, .18f);
            nameField = TextBox(identity, "Name box", 0, 300, 260, 48, "Your name", v => { Flow.SetName(v); Publish(); });
            Label(identity, "No account required. Your progress is saved on this device only.\nClearing your browser data or switching devices will erase it.", 0, 340, 330, 36, 12).color = Muted; // owner (worksheet section 3)
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
            var screen = ScreenPanel(name, "atrium"); LightOverlay(screen, "atrium-light");
            if (AtriumKitted) BuildAtriumKit(screen, name == "Atrium return" ? SliceScreen.AtriumReturn : SliceScreen.Atrium); // Build N
            Label(screen, "THE GRAND ATRIUM", 0, 32, 340, 24, 18);
            var sealedDoor = Block(screen, "Sealed door", -118, 310, 62, 128, "door-sealed"); // Build K: sized to the painted arch (Sept 24 Atrium)
            var shelvesBlock = Block(screen, "Shelves, mostly bare", -130, 200, 60, 120, "shelves");
            var furniture = Block(screen, "Covered furniture", 50, 422, 120, 70, "furniture-covered");
            var cloth = Rect("Dust cloth", screen, 50, 410, 128, 30); cloth.gameObject.AddComponent<Image>().color = new Color(.3f, .3f, .32f); cloth.gameObject.SetActive(!HasArt(furniture));
            var candle = Rect("Candle", screen, 115, 375, 8, 20); var candleImage = candle.gameObject.AddComponent<Image>(); Slots.Dress(candleImage, "candle"); Slots.Paint(candleImage, new Color(.5f, .42f, .3f), .7f);
            if (AtriumKitted) { foreach (var b in new[] { sealedDoor, shelvesBlock, furniture }) RetireProps(b, false); cloth.gameObject.SetActive(false); candle.gameObject.SetActive(false); }
            Label(screen, "Dust. Covered furniture. Sealed doors. One weak candle.", 0, 92, 330, 20, 12).color = Muted;
            var panel = Rect("Caspar panel", screen, 0, 560, 324, 150); panel.gameObject.AddComponent<Image>().color = new Color(.045f, .025f, .03f, .92f);
            Label(panel, "CASPAR", 0, 16, 290, 22, 13);
            caspar = Label(panel, "", 0, 86, 306, 110, 13);
            next = MakeButton(screen, "Continue", 0, 690, 190, 56, () => Continue()); StyleAtriumButton(next);
            return screen;
        }
        void BuildChamber()
        {
            chamber = ScreenPanel("Chamber", "chamber"); LightOverlay(chamber, "chamber-light");
            if (ChamberKitted) { chamberKit = BuildKit(chamber, "ckit-", ChamberKit, "chamber-grime", null, ChamberGrime, ChamberVeil, null, () => Mathf.Clamp(Flow.KeysSpent, 0, 4), p => Flow.KeysSpent >= p.Key); chamberKit.VeilTint = new Color(.02f, .035f, .09f); FinishKit(chamber, chamberKit); atriumKits.Add(chamberKit); } // Build O
            Label(chamber, "THE CRYSTAL BOOK CHAMBER", 0, 32, 340, 24, 18);
            for (int i = 0; i < candles.Length; i++)
            { var c = Rect("Candle", chamber, -120 + i * 30, 90, 8, 20); candles[i] = c.gameObject.AddComponent<Image>(); candles[i].raycastTarget = false; Slots.Dress(candles[i], "candle"); Slots.Paint(candles[i], LampDark, DarkArt); }
            chandelierLabel = Label(chamber, "The chandelier, dark", 0, 116, 300, 18, 11); chandelierLabel.color = Muted;
            mechanism = Rect("Mechanism", chamber, 0, 180, 110, 110);
            var mechanismImage = mechanism.gameObject.AddComponent<Image>(); mechanismImage.raycastTarget = false; bool mechanismArt = Slots.Dress(mechanismImage, "mechanism"); mechanismImage.enabled = mechanismArt;
            var mechanismLines = Rect("Mechanism lines", mechanism, 0, 55, 110, 110); mechanismLines.gameObject.SetActive(!mechanismArt);
            RingLines(mechanismLines, 46, new Color(.62f, .57f, .53f, .5f), null);
            var tick = Rect("Mechanism tick", mechanismLines, 0, 55 - 46, 3, 14); tick.gameObject.AddComponent<Image>().color = Bone;
            for (int b = 0; b < SliceFlow.Books; b++)
            {
                float x = -138 + b * 46;
                var book = Rect("Book " + (b + 1), chamber, x, 300, 34, 70); bookImages[b] = book.gameObject.AddComponent<Image>();
                var bookOutline = book.gameObject.AddComponent<Outline>(); bookOutline.effectColor = new Color(.35f, .35f, .38f); bookOutline.enabled = !Slots.Dress(bookImages[b], "crystal-book"); Slots.Paint(bookImages[b], Dim, ShutArt);
                var page = Rect("Page", book, 0, 35, 26, 58); bookPages[b] = page.gameObject.AddComponent<Image>(); bookPages[b].raycastTarget = false; Slots.Dress(bookPages[b], "crystal-page"); Slots.Paint(bookPages[b], new Color(Bone.r, Bone.g, Bone.b, 0), 1f); // Build D: a page turns when the Book opens
                for (int l = 0; l < SliceFlow.LocksPerBook; l++)
                { var dot = Rect("Lock", chamber, x - 10 + l * 10, 348, 7, 7); locks[b * 3 + l] = dot.gameObject.AddComponent<Image>(); Slots.Dress(locks[b * 3 + l], "lock"); Slots.Paint(locks[b * 3 + l], LockDark, LockDarkArt); }
            }
            chamberBooksLabel = Label(chamber, "Seven sealed Books, three locks each", 0, 376, 330, 20, 12); chamberBooksLabel.color = Muted;
            if (ChamberKitted)
            {
                // Build O: the seven Books stand on the altar (top at 335), sealed or open; the locks sit on each Book's foot; the old candles, mechanism, and labels go.
                for (int b = 0; b < SliceFlow.Books; b++)
                {
                    var rt = bookImages[b].rectTransform; rt.anchoredPosition = new Vector2(-102 + b * 34, -302); rt.sizeDelta = new Vector2(30, 66);
                    bookImages[b].sprite = Slots.Image("ckit-book-worn"); bookImages[b].color = Color.white; bookImages[b].preserveAspect = true; var o = bookImages[b].GetComponent<Outline>(); if (o != null) o.enabled = false;
                    bookPages[b].gameObject.SetActive(false);
                    for (int l = 0; l < SliceFlow.LocksPerBook; l++) { var lt = locks[b * 3 + l].rectTransform; lt.anchoredPosition = new Vector2(-102 + b * 34 - 8 + l * 8, -326); lt.sizeDelta = new Vector2(6, 6); }
                }
                foreach (var c in candles) c.gameObject.SetActive(false); mechanism.gameObject.SetActive(false); chandelierLabel.gameObject.SetActive(false); chamberBooksLabel.text = "";
            }
            // Build D: the Chamber as a room. A doorway back, the Books as a point of interest, a floor band; all hidden on the first (Continue) visit.
            chamberBand = Rect("Floor band", chamber, 0, ChamberBandY, 340, 30); var chamberBandImage = chamberBand.gameObject.AddComponent<Image>(); chamberBandImage.color = new Color(.16f, .16f, .19f, ChamberKitted ? 0 : 1); // Build O: the painted floor carries it chamberBandImage.raycastTarget = false; chamberBand.gameObject.SetActive(false); // before the doorway, so its label draws over the band
            chamberDoor = Block(chamber, "Doorway back", ChamberKitted ? -145 : -150, ChamberKitted ? 287 : 347, ChamberKitted ? 40 : 30, ChamberKitted ? 165 : 124, ChamberKitted ? null : "door-open"); if (ChamberKitted) RetireProps(chamberDoor, true); HitArea(chamberDoor, 48); // Build K: the painted doorway Tappable(chamberDoor, () => Walk("atrium-door")); chamberDoor.gameObject.SetActive(false);
            chamberBooksTap = Rect("The Books, tap to walk", chamber, 0, 300, 330, 90); var booksTapImage = chamberBooksTap.gameObject.AddComponent<Image>(); booksTapImage.color = new Color(0, 0, 0, 0); Tappable(chamberBooksTap, () => Walk("books")); chamberBooksTap.gameObject.SetActive(false);
            var panel = Rect("Caspar panel", chamber, 0, 520, 324, 170); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 16, 290, 22, 13);
            chamberText = Label(panel, "", 0, 96, 306, 136, 13);
            chamberEnd = Label(chamber, "The first Key is spent. The Library has taken her first breath.", 0, 720, 330, 40, 12); chamberEnd.color = Muted; chamberEnd.gameObject.SetActive(false);
            var glow = Rect("Insert glow", chamber, 0, 654, 214, 80); insertGlow = glow.gameObject.AddComponent<Image>(); insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); insertGlow.raycastTarget = false;
            insert = MakeButton(chamber, "Insert Key", 0, 654, 190, 56, Insert); insert.GetComponent<Image>().color = Crimson; // owner (worksheet section 13)
            chamberContinue = MakeButton(chamber, "Continue", 0, 654, 190, 56, () => Continue()); chamberContinue.gameObject.SetActive(false);
            chamberBack = MakeButton(chamber, "Return to the Atrium", 0, 714, 300, 48, LeaveChamber); chamberBack.gameObject.SetActive(false); // Build D // owner (worksheet section 13)
            journalChamber = MakeButton(chamber, "Your journal", 0, 774, 216, 40, OpenJournal); journalChamber.GetComponentInChildren<Text>().fontSize = 13; journalChamber.gameObject.SetActive(false); // Build F
        }
        void BuildHub()
        {
            // Q05 decisions 1, 6: the Atrium in Stage 2 "Stirring" with two entrances.
            hub = ScreenPanel("Hub", "atrium"); LightOverlay(hub, "atrium-light");
            if (AtriumKitted) hubKit = BuildAtriumKit(hub, SliceScreen.Hub); // Build N
            Label(hub, "THE GRAND ATRIUM", 0, 32, 340, 24, 18);
            var shelves = Block(hub, "Shelves, mostly bare", -130, 200, 60, 120, "shelves"); shelvesLabel = shelves.GetComponentInChildren<Text>(); // owner (worksheet section 13)
            for (int i = 0; i < 3; i++) { var book = Rect("Book", shelves, -16 + i * 16, 30 + (i % 2) * 40, 10, 26); shelfBooks[i] = book.gameObject.AddComponent<Image>(); shelfBooks[i].color = new Color(.3f, .28f, .3f); shelfBooks[i].raycastTarget = false; Slots.Dress(shelfBooks[i], "shelf-book"); book.gameObject.SetActive(false); } // Build D: the shelves take their books back at Stage 4
            var floor = Rect("Floor band", hub, 0, BandY, 340, 30); var floorImage = floor.gameObject.AddComponent<Image>(); floorImage.color = new Color(.16f, .16f, .19f, 0); floorImage.raycastTarget = false;
            var desk = Block(hub, "Desk, uncovered", -125, 426, 70, 30, "desk"); Tappable(desk, () => Walk("desk"));
            var casparMark = Rect("Caspar", hub, 100, 418, 20, 80); var casparBody = casparMark.gameObject.AddComponent<Image>(); casparBody.color = Muted; casparBody.raycastTarget = false;
            var casparHead = Rect("Caspar head", hub, 100, 366, 18, 18); var casparHeadImage = casparHead.gameObject.AddComponent<Image>(); casparHeadImage.color = Muted; casparHeadImage.raycastTarget = false;
            Label(hub, "Caspar", 100, 452, 60, 14, 10).color = Muted;
            var casparTap = Rect("Caspar, tap to walk", hub, 100, 402, 64, 112); /* Build L: a head taller than the 100 px Keeper, feet where they were (458) */ var casparTapImage = casparTap.gameObject.AddComponent<Image>(); casparTapImage.color = new Color(0, 0, 0, 0); Tappable(casparTap, () => Walk("caspar"));
            if (Slots.Dress(casparTapImage, "caspar")) { casparMark.gameObject.SetActive(false); casparHead.gameObject.SetActive(false); } // Build E: his figure takes the file; the tap rect is his slot
            var lamp1 = Rect("Lamp", hub, -64, 150, 8, 22); lampOne = lamp1.gameObject.AddComponent<Image>(); lampOne.color = LampLit; lampOne.raycastTarget = false; Slots.Dress(lampOne, "lamp");
            var lamp2 = Rect("Lamp", hub, 64, 150, 8, 22); lampTwo = lamp2.gameObject.AddComponent<Image>(); lampTwo.color = LampDark; lampTwo.raycastTarget = false; Slots.Dress(lampTwo, "lamp");
            var lamp3 = Rect("Lamp", hub, 140, 150, 8, 22); lampThree = lamp3.gameObject.AddComponent<Image>(); lampThree.color = LampDark; lampThree.raycastTarget = false; Slots.Dress(lampThree, "lamp"); // Build B: Stage 5
            var lamp4 = Rect("Lamp", hub, -140, 150, 8, 22); lampFour = lamp4.gameObject.AddComponent<Image>(); lampFour.color = LampDark; lampFour.raycastTarget = false; Slots.Dress(lampFour, "lamp"); // Build C: Stage 6
            string[] doors = { "Sealed", "Zodiac Wing, open", "Crystal Book Chamber" }; // Build D: the third doorway leads back to the Chamber
            for (int i = 0; i < 3; i++)
            {
                var door = Block(hub, doors[i], i == 0 ? -118 : i == 1 ? 0 : 118, 310, i == 1 ? 72 : 62, 128, i == 0 ? "door-sealed" : "door-open"); // Build K: each door fills its painted arch string poi = i == 0 ? "sealed-left" : i == 1 ? "wing-door" : "chamber-door"; Tappable(door, () => Walk(poi));
                if (i == 1) { var light = Rect("Doorway light", door, 0, 64, 50, 105); doorOpenLight = light.gameObject.AddComponent<Image>(); doorOpenLight.color = new Color(.95f, .8f, .5f, HasArt(door) ? .06f : .35f); doorOpenLight.raycastTarget = false; }
                else if (i == 2) { var light = Rect("Doorway light", door, 0, 64, 44, 105); var li = light.gameObject.AddComponent<Image>(); li.color = new Color(.7f, .8f, .95f, HasArt(door) ? .05f : .3f); li.raycastTarget = false; }
                else { var lockRect = Rect("Lock", door, 0, 50, 12, 16); var li = lockRect.gameObject.AddComponent<Image>(); li.color = new Color(.45f, .45f, .5f); li.raycastTarget = false; lockRect.gameObject.SetActive(!HasArt(door)); var glow = Rect("Light behind the door", door, 0, 50, 50, 82); sealedLeftLight = glow.gameObject.AddComponent<Image>(); sealedLeftLight.color = new Color(.95f, .8f, .5f, 0); sealedLeftLight.raycastTarget = false; glow.SetAsFirstSibling(); }
            }
            if (AtriumKitted)
            {
                // Build N: the kit carries the shelf, desk, lamps, and doors; the greybox versions and the grey labels go, the tap areas stay.
                shelves.gameObject.SetActive(false); RetireProps(desk, true);
                foreach (var lamp in new[] { lampOne, lampTwo, lampThree, lampFour }) lamp.gameObject.SetActive(false);
                foreach (Transform child in hub) if (child.name == "Sealed" || child.name == "Zodiac Wing, open" || child.name == "Crystal Book Chamber") RetireProps((RectTransform)child, true);
                foreach (var t in hub.GetComponentsInChildren<Text>(true)) if (t.text == "Caspar" && t.transform.parent == hub) t.gameObject.SetActive(false);
            }
            hubCaption = Label(hub, "", 0, 92, 340, 36, 11); hubCaption.color = Muted;
            var panel = Rect("Caspar panel", hub, 0, 536, 324, 120); panel.gameObject.AddComponent<Image>().color = new Color(.045f, .025f, .03f, .92f);
            Label(panel, "CASPAR", 0, 14, 290, 20, 13);
            hubText = Label(panel, "", 0, 70, 306, 90, 12);
            enterWing = MakeButton(hub, "The Zodiac Wing", -78, 624, 150, 52, EnterWing); StyleAtriumButton(enterWing);
            enterChamber = MakeButton(hub, "The Crystal Book Chamber", 78, 624, 150, 52, EnterChamber); StyleAtriumButton(enterChamber); enterChamber.GetComponentInChildren<Text>().fontSize = 12; // Build D; owner (worksheet section 13)
            journalHub = MakeButton(hub, "Your journal", 0, 680, 300, 52, OpenJournal); StyleAtriumButton(journalHub); journalHub.gameObject.SetActive(false); // Build F: the journal takes the row Check the Seals held (retired Sept 15)
            hubNote = Label(hub, "", 0, 722, 330, 28, 10); hubNote.color = Muted; // two lines: the owner's sealed-door and desk lines are long
            endCard = Label(hub, "The whole wheel burns. The symbols await you next.", 0, 736, 330, 36, 11); endCard.gameObject.SetActive(false); // owner (worksheet section 1); two lines at 330 wide
            walkSpeed = TestButton(hub, "Walk: normal (test)", -118, 774, CycleWalkSpeed); walkSpeedLabel = walkSpeed.GetComponentInChildren<Text>();
            mute = TestButton(hub, MuteLabel, 0, 774, ToggleMute); // Build E: sound on or off, apart from reduced motion
            hubRestart = TestButton(hub, "Start over (test)", 118, 774, Restart);
        }
        void BuildReview()
        {
            // Q05 decision 2: direct-tap items live here; compressed Dial items use the Wing canvas.
            review = ScreenPanel("Practice");
            Label(review, "PRACTICE", 0, 32, 340, 24, 18); // Build F: the tap forms of practice; the wheel forms use the Dial canvas with a "Practice · n of N" header
            reviewProgress = Label(review, "", 0, 62, 300, 20, 12); reviewProgress.color = Muted;
            reviewQuestion = Label(review, "", 0, 200, 330, 48, 15); // two lines for the owner's element question
            for (int i = 0; i < 4; i++) { string element = Elements[i]; elementButtons[i] = MakeButton(review, element, -78 + (i % 2) * 156, 300 + (i / 2) * 64, 150, 56, () => AnswerTap(element)); }
            for (int i = 0; i < 3; i++) { string modality = Zodiac.Modalities[i]; modalityButtons[i] = MakeButton(review, modality, 0, 300 + i * 64, 300, 56, () => AnswerModalityTap(modality)); modalityButtons[i].gameObject.SetActive(false); }
            var glyphBox = Rect("Review glyph", review, 0, 150, 90, 90); reviewGlyph = Label(glyphBox, "", 0, 45, 90, 90, 60); reviewGlyph.font = Dial.GlyphFont; reviewGlyph.horizontalOverflow = HorizontalWrapMode.Overflow; reviewGlyph.verticalOverflow = VerticalWrapMode.Overflow; reviewGlyph.gameObject.SetActive(false);
            for (int i = 0; i < 4; i++) { int slot = i; reviewGlyphButtons[i] = MakeButton(review, "", -78 + (i % 2) * 156, 300 + (i / 2) * 64, 150, 56, () => AnswerGlyphReview(slot)); reviewGlyphButtons[i].gameObject.SetActive(false); }
            reviewNote = Label(review, "", 0, 440, 330, 50, 14);
            reviewSummary = Label(review, "", 0, 520, 330, 30, 16);
            leavePractice = MakeButton(review, "Leave the instrument", 0, 654, 190, 56, LeavePractice); // Build F: an exit at any point (the Sept 15 defect)
        }
        void BuildGlyphs()
        {
            // v0.3 Part A: name the glyph by direct tap. Same layout as a review item so the two read as one family.
            glyphs = ScreenPanel("Glyphs");
            Label(glyphs, "THE ZODIAC WING", 0, 32, 340, 24, 18);
            glyphProgress = Label(glyphs, "", 0, 62, 300, 20, 12); glyphProgress.color = Muted;
            var card = Rect("Glyph card", glyphs, 0, 200, 140, 140); bookCard = card.gameObject.AddComponent<Image>(); bookCard.color = PanelColor; // Build E: book-cover shut, book-page open
            glyphCard = Label(card, "", 0, 70, 130, 130, 84); glyphCard.font = Dial.GlyphFont; glyphCard.horizontalOverflow = HorizontalWrapMode.Overflow; glyphCard.verticalOverflow = VerticalWrapMode.Overflow;
            Label(glyphs, "This symbol belongs to which sign?", 0, 290, 330, 24, 15); // owner (worksheet section 4)
            for (int i = 0; i < 4; i++) { int slot = i; glyphNameButtons[i] = MakeButton(glyphs, "", -78 + (i % 2) * 156, 340 + (i / 2) * 64, 150, 56, () => AnswerGlyphName(slot)); }
            var panel = Rect("Caspar panel", glyphs, 0, 520, 324, 120); panel.gameObject.AddComponent<Image>().color = PanelColor;
            Label(panel, "CASPAR", 0, 14, 290, 20, 13);
            glyphCaspar = Label(panel, "", 0, 70, 306, 90, 12);
            glyphNote = Label(glyphs, "", 0, 446, 330, 24, 14);
            closeBook = MakeButton(glyphs, "Close the Book", 0, 680, 300, 52, CloseBook); // owner (worksheet section 7) // between the name buttons (to 432) and the Caspar panel (from 460)
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
            leaveGrid = MakeButton(gridScreen, "Leave the Table", -72, 714, 128, 48, LeaveGrid); leaveGrid.GetComponentInChildren<Text>().fontSize = 13; // owner (worksheet section 11)
            gridAsk = MakeButton(gridScreen, "Ask Caspar", 78, 714, 164, 48, GridAsk); gridAsk.GetComponentInChildren<Text>().fontSize = 13; gridAsk.gameObject.SetActive(false); // owner (worksheet section 8)
            // Build I: the table's own Key rise, over the board, drawn like the Dial's
            var tableKeyGlow = Rect("Table key glow", gridScreen, 0, GridKeyTop, 140, 140); gridKeyGlow = tableKeyGlow.gameObject.AddComponent<Image>(); gridKeyGlow.sprite = SoftGlow(); gridKeyGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); gridKeyGlow.raycastTarget = false;
            gridKey = Rect("Table Keeper Key", gridScreen, 0, GridKeyTop, 84, 40); var tableKeyImage = gridKey.gameObject.AddComponent<Image>(); tableKeyImage.raycastTarget = false; bool tableKeyArt = Slots.Dress(tableKeyImage, "keeper-key"); Slots.Paint(tableKeyImage, new Color(Bone.r, Bone.g, Bone.b, 0), 1f);
            gridKeyLabel = Label(gridKey, "KEEPER KEY", 0, 20, 80, 36, 12); gridKeyLabel.color = new Color(Charcoal.r, Charcoal.g, Charcoal.b, 0); gridKeyLabel.gameObject.SetActive(!tableKeyArt);
            gridKey.gameObject.SetActive(false);
        }
        void BuildWingExtras()
        {
            var r = Dial.Root;
            var floor = Rect("Floor markings", r, 0, 270, 320, 320); floor.SetAsFirstSibling();
            floorArt = floor.gameObject.AddComponent<Image>(); floorArt.raycastTarget = false; floorArt.enabled = Slots.Dress(floorArt, "floor-markings");
            RingLines(floor, 150, new Color(.62f, .57f, .53f, .2f), floorLines); if (floorArt.enabled) foreach (var line in floorLines) line.gameObject.SetActive(false);
            FloorLight(.2f);
            var shelf = Block(r, "Collapsed bookshelf", -125, 90, 50, 36, "shelf"); shelf.SetAsFirstSibling();
            for (int i = 0; i < 3; i++) { var book = Rect("Book", shelf, -14 + i * 14, 18, 8, 24); var bookImage = book.gameObject.AddComponent<Image>(); bookImage.color = new Color(.3f, .28f, .3f); Slots.Dress(bookImage, "shelf-book"); book.localRotation = Quaternion.Euler(0, 0, i * 9 - 9); }
            var chair = Block(r, "Covered chair", 125, 90, 44, 36, "chair"); chair.SetAsFirstSibling();
            var cloth = Rect("Dust cloth", chair, 0, 10, 48, 14); cloth.gameObject.AddComponent<Image>().color = new Color(.3f, .3f, .32f); cloth.gameObject.SetActive(!HasArt(chair));
            var c = Rect("Candle", r, -160, 445, 6, 18); c.SetAsFirstSibling(); candle = c.gameObject.AddComponent<Image>(); candle.raycastTarget = false; Slots.Dress(candle, "candle"); Slots.Paint(candle, LampDark, DarkArt);
            var s = Rect("Seam", r, 0, 270, 332, 2); seam = s.gameObject.AddComponent<Image>(); seam.color = new Color(Bone.r, Bone.g, Bone.b, 0); seam.raycastTarget = false;
            var glow = Rect("Key glow", r, 0, 270, 140, 140); keyGlow = glow.gameObject.AddComponent<Image>(); keyGlow.sprite = SoftGlow(); keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); keyGlow.raycastTarget = false;
            keyRect = Rect("Keeper Key", r, 0, 270, 84, 40); var keyImage = keyRect.gameObject.AddComponent<Image>(); keyImage.raycastTarget = false; bool keyArt = Slots.Dress(keyImage, "keeper-key"); Slots.Paint(keyImage, new Color(Bone.r, Bone.g, Bone.b, 0), 1f);
            keyLabel = Label(keyRect, "KEEPER KEY", 0, 20, 80, 36, 12); keyLabel.color = new Color(Charcoal.r, Charcoal.g, Charcoal.b, 0); keyLabel.gameObject.SetActive(!keyArt); // the file draws its own Key
            keyIndicator = Label(r, "Keeper Key: 1", 110, 92, 140, 20, 12); keyIndicator.alignment = TextAnchor.MiddleRight; keyIndicator.gameObject.SetActive(false);
            wingContinue = MakeButton(r, "Continue", 0, 654, 190, 56, WingContinue); wingContinue.name = "Slice Continue"; wingContinue.gameObject.SetActive(false);
            // Build F: the fork (Sept 15 ruling) on the row below Back; the practice exit where the table's exit sits, clear of Ask Caspar.
            forkLesson = MakeButton(r, "Continue the lesson", -78, 714, 150, 48, ContinueLesson); forkLesson.GetComponentInChildren<Text>().fontSize = 12; forkLesson.gameObject.SetActive(false);
            forkPractice = MakeButton(r, "Practice what you know", 78, 714, 150, 48, EnterPractice); forkPractice.GetComponentInChildren<Text>().fontSize = 12; forkPractice.gameObject.SetActive(false);
            leavePracticeDial = MakeButton(r, "Leave the instrument", -72, 714, 128, 48, LeavePractice); leavePracticeDial.GetComponentInChildren<Text>().fontSize = 12; leavePracticeDial.gameObject.SetActive(false);
        }

        void BuildWingRoom()
        {
            // Q06 phase 2, decision 2: the Wing as a room with two points of interest, the Dial and the doorway back.
            wingRoom = ScreenPanel("Wing room", "wing"); wingLight = LightOverlay(wingRoom, "wing-light");
            wingRoomKit = BuildKit(wingRoom, "kit-", WingKit, "wing-grime", wingLight, KitGrime, KitVeil, KitLight, () => Mathf.Clamp(Flow.Keys, 0, 4), KitRestoredNow); // Build M/N: grime under the light, the pieces, then the veil
            var wingPlate = wingKit.FirstOrDefault(p => p.P.Name == "plate"); if (wingPlate?.Restored != null) PlateText((RectTransform)wingPlate.Restored.transform, WingPlateName);
            FinishKit(wingRoom, wingRoomKit); // everything built after this draws above the veil
            Label(wingRoom, "THE ZODIAC WING", 0, 32, 340, 24, 18);
            Label(wingRoom, "The Elemental Pattern", 0, 62, 300, 20, 12).color = Muted;
            // Build L (Wing composition, owner-approved mockup B, Sept 24): the room art carries the Dial, the table, the chair, and the shelf,
            // painted in place. Each object keeps an invisible tap area and its glow over its painted footprint; without room art the greybox shows.
            bool wingBaked = HasArt(wingRoom);
            var shelf = Block(wingRoom, "Collapsed bookshelf", 157, 295, 45, 290, wingBaked ? null : "shelf"); if (wingBaked) shelf.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            if (!wingBaked) for (int i = 0; i < 3; i++) { var book = Rect("Book", shelf, -14 + i * 14, 18, 8, 24); var bookImage = book.gameObject.AddComponent<Image>(); bookImage.color = new Color(.3f, .28f, .3f); Slots.Dress(bookImage, "shelf-book"); book.localRotation = Quaternion.Euler(0, 0, i * 9 - 9); }
            var glow = Rect("Shelf glow", shelf, 0, 145, 90, 320); shelfGlow = glow.gameObject.AddComponent<Image>(); shelfGlow.sprite = wingBaked ? SoftRing() : SoftGlow(); shelfGlow.color = new Color(.95f, .8f, .5f, 0); shelfGlow.raycastTarget = false; glow.SetAsFirstSibling();
            Tappable(shelf, () => Walk("shelf")); // v0.3 revision: the book of symbols lives here once the wheel is lit
            var dial = Rect("The Dial", wingRoom, 40, 337, 175, 205); var dialImage = dial.gameObject.AddComponent<Image>(); dialImage.color = new Color(0, 0, 0, 0);
            var rings = Rect("Rings", dial, 0, 102, 175, 175); rings.gameObject.SetActive(!wingBaked && !Slots.Dress(dialImage, "dial-face")); if (wingBaked) dialImage.color = new Color(0, 0, 0, 0); // the face seen from the room
            RingLines(rings, 88, new Color(Bone.r, Bone.g, Bone.b, .4f), null); RingLines(rings, 30, new Color(Bone.r, Bone.g, Bone.b, .25f), null);
            var waitingGlow = Rect("Dial glow", wingRoom, 40, 330, 250, 250); dialGlow = waitingGlow.gameObject.AddComponent<Image>(); dialGlow.sprite = wingBaked ? SoftRing() : SoftGlow(); dialGlow.color = new Color(.95f, .8f, .5f, 0); dialGlow.raycastTarget = false; waitingGlow.SetSiblingIndex(dial.GetSiblingIndex()); // Build I (86bc1brxd): behind the Dial
            var dialLabel = Label(wingRoom, "The Dial", 40, 452, 120, 16, 10); dialLabel.gameObject.SetActive(wingKit.Count == 0); // Build M: the grey labels go once the kit is in (owner, Sept 24) dialLabel.color = Muted;
            Tappable(dial, () => Walk("dial"));
            // Build B (07 Room Scope amendment): a second interactive object, the table with its board of twelve, dark until the modality unit is complete.
            var table = Block(wingRoom, "The table", -62, 400, 90, 90, wingBaked ? null : "table"); if (wingBaked) table.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var board = Rect("Board", table, 0, 45, 60, 30); board.gameObject.SetActive(!wingBaked && !HasArt(table));
            for (int i = 0; i < 12; i++) { var square = Rect("Square", board, -20 + (i % 3) * 20, 5 + (i / 3) * 7, 16, 5); var squareImage = square.gameObject.AddComponent<Image>(); squareImage.color = new Color(.3f, .28f, .3f); squareImage.raycastTarget = false; }
            var tableGlow = Rect("Table glow", table, 0, 45, 120, 120); gridGlow = tableGlow.gameObject.AddComponent<Image>(); gridGlow.sprite = wingBaked ? SoftRing() : SoftGlow(); gridGlow.color = new Color(.95f, .8f, .5f, 0); gridGlow.raycastTarget = false; tableGlow.SetAsFirstSibling();
            Tappable(table, () => Walk("grid"));
            var door = Block(wingRoom, "Doorway back", -138, 292, 42, 145, "door-open"); Tappable(door, () => Walk("atrium-door")); HitArea(door, 48); // Build K: the painted doorway
            if (wingKit.Count > 0) foreach (var block in new[] { shelf, table, door }) foreach (var t in block.GetComponentsInChildren<Text>(true)) t.gameObject.SetActive(false); // Build M: the plates name the doors now
            var light = Rect("Doorway light", door, 0, 72, 30, 118); var lightImage = light.gameObject.AddComponent<Image>(); lightImage.color = new Color(.95f, .8f, .5f, HasArt(door) ? .05f : .25f); lightImage.raycastTarget = false;
            var floor = Rect("Floor band", wingRoom, 0, BandY, 340, 30); var floorImage = floor.gameObject.AddComponent<Image>(); floorImage.color = new Color(.16f, .16f, .19f, wingBaked ? 0 : 1); floorImage.raycastTarget = false;
            wingRoomCaption = Label(wingRoom, "The Dial stands at the center of the room. The doorway behind you leads back to the Atrium.", 0, 466, 340, 36, 12); // owner (worksheet section 6) // two lines at 360 wide wingRoomCaption.color = Muted; // placeholder (owner writes)
            enterGrid = MakeButton(wingRoom, "The Table", 0, 512, 300, 52, () => Walk("grid")); enterGrid.gameObject.SetActive(false); // Build B: shown once the table has woken // owner (worksheet section 11)
            enterDial = MakeButton(wingRoom, "The Dial", 0, 624, 300, 52, () => Walk("dial"));
            enterShelf = MakeButton(wingRoom, "The Bookshelf", 0, 568, 300, 52, () => Walk("shelf")); enterShelf.gameObject.SetActive(false); // owner (worksheet section 7)
            wingRoomBack = MakeButton(wingRoom, "Return to the Atrium", 0, 680, 300, 52, LeaveWing); // owner (worksheet section 6)
            journalWing = MakeButton(wingRoom, "Your journal", 0, 774, 216, 40, OpenJournal); journalWing.GetComponentInChildren<Text>().fontSize = 13; journalWing.gameObject.SetActive(false); // Build F
        }
        void BuildAvatar()
        {
            // Q06 phase 2, decision 3: a placeholder upright marker with a walk bob and one idle pose. No face, no clothing.
            avatar = Rect("Keeper", root, 0, BandY - 16, 20, 44); avatar.localScale = Vector3.one * KeeperScale; // the figure is built at 20 x 44 and scaled whole
            var body = Rect("Body", avatar, 0, 26, 16, 30); var bodyImage = body.gameObject.AddComponent<Image>(); bodyImage.color = new Color(.85f, .8f, .72f); bodyImage.raycastTarget = false;
            avatarHead = Rect("Head", avatar, 0, 7, 12, 12); var headImage = avatarHead.gameObject.AddComponent<Image>(); headImage.color = new Color(.85f, .8f, .72f); headImage.raycastTarget = false;
            // Build E: two frames take the marker's place, idle and walk, flipped to face the way it walks.
            var art = Rect("Keeper art", avatar, 0, 22, 20, 44); avatarArt = art.gameObject.AddComponent<Image>(); avatarArt.raycastTarget = false;
            bool keeperArt = Slots.Dress(avatarArt, "keeper-idle"); keeperWalk = Slots.Image("keeper-walk");
            if (!keeperArt && keeperWalk != null) { keeperArt = Slots.Dress(avatarArt, "keeper-walk"); } // a walk frame alone stands in for both
            keeperIdle = avatarArt.sprite; if (keeperWalk == null) keeperWalk = keeperIdle;
            art.gameObject.SetActive(keeperArt); body.gameObject.SetActive(!keeperArt); avatarHead.gameObject.SetActive(!keeperArt);
            avatar.gameObject.SetActive(false);
        }
        void BuildFade()
        {
            var fade = Rect("Fade", root, 0, 400, 360, 800); fadeImage = fade.gameObject.AddComponent<Image>(); fadeImage.color = new Color(0, 0, 0, 0); fadeImage.raycastTarget = false;
        }
        void PlaceAvatar()
        {
            var walk = Flow.Walk; float band = walk.Room == Room.Chamber ? ChamberBandY : BandY;
            avatar.anchoredPosition = new Vector2(walk.X, -(band + 6 - 22 * KeeperScale) + walk.Bob); // the feet stay at band + 6 whatever the scale
            avatarHead.anchoredPosition = new Vector2(walk.Facing * 2, -7);
            if (avatarArt.gameObject.activeSelf) { avatarArt.sprite = walk.Walking && ((int)(walk.Traveled / 14f)) % 2 == 1 ? keeperWalk : keeperIdle; avatarArt.rectTransform.localScale = new Vector3(walk.Facing, 1, 1); } // one frame per step, the walk bob's own rhythm
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
        static void StyleAtriumButton(Button button)
        {
            button.GetComponent<Image>().color = new Color(.18f, .055f, .065f, .94f);
            var edge = button.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(.48f, .22f, .16f, .65f); edge.effectDistance = new Vector2(1, -1);
        }

        // ---- actions (all input paths, including the Web bridge, arrive here) ----
        public void WebAction(string command)
        {
            if (command == "mute") { ToggleMute(); return; }
            if (command.StartsWith("sound:")) { Sound.Play(command.Substring(6)); Publish(); return; } // the style page plays a slot on request
            if (styleShown && command != "reload") return; // the style page is not the game (a reload, test-only, still gets out of it)
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
            else if (command == "leave-wing") LeaveWing();
            else if (command == "continue-lesson") ContinueLesson(); // Build F
            else if (command == "enter-practice") EnterPractice();
            else if (command == "leave-practice") LeavePractice();
            else if (command == "open-journal") OpenJournal();
            else if (command == "close-journal") CloseJournal();
            else if (command == "journal-next") JournalTurn(1);
            else if (command == "journal-prev") JournalTurn(-1);
            else if (command.StartsWith("element:") && int.TryParse(command.Substring(8), out int element) && element >= 0 && element < 4) AnswerTap(Elements[element]);
            else if (command.StartsWith("modality:") && int.TryParse(command.Substring(9), out int modality) && modality >= 0 && modality < 3) AnswerModalityTap(Zodiac.Modalities[modality]);
            else if (command.StartsWith("glyph-name:") && int.TryParse(command.Substring(11), out int slot) && slot >= 0 && slot < 4) { if (Flow.AtPractice) AnswerGlyphReview(slot); else AnswerGlyphName(slot); }
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
            if (s == SliceScreen.Atrium && Page < AtriumPages.Length - 1) { Page++; Sound.Play("page"); ShowPage(); Publish(); return; }
            if (s == SliceScreen.AtriumReturn && Page < ReturnPages.Length - 1) { Page++; Sound.Play("page"); ShowPage(); Publish(); return; }
            if (s == SliceScreen.Chamber && !Flow.Ended) { if (Page < ChamberPages.Length - 1) { Page++; Sound.Play("page"); ShowPage(); Publish(); } return; }
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
            Slots.Paint(locks[spent - 1], Bone, 1f);
            bool opened = spent % SliceFlow.LocksPerBook == 0; int book = (spent - 1) / SliceFlow.LocksPerBook;
            chamberLine = opened ? "The last lock turns, acolyte, and something inside the crystal stirs." : spent == 4 ? "The second Book takes its first Key, and the crystal trembles." : "Lock " + (spent % SliceFlow.LocksPerBook) + " of three turns. The Book accepts it and holds."; // owner (worksheet section 13)
            ShowChamberRoom(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? 1f : 1.4f);
            if (opened)
            {
                Sound.Play("page");
                yield return Tween(ReducedMotion ? 0 : .8f, k => { Slots.Paint(bookImages[book], Color.Lerp(Dim, BookOpen, k), Mathf.Lerp(ShutArt, 1f, k)); Slots.Paint(bookPages[book], new Color(Bone.r, Bone.g, Bone.b, .9f * k), 1f); bookPages[book].rectTransform.anchoredPosition = new Vector2(0, -35 + 18 * k); });
                Candles(true);
                chamberLine = "The first Book opens. Light spills from its pages and the room itself takes a breath.\nEvery lock it had is turned, and it answers to you now."; // owner (worksheet section 13)
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
            journalChamber.gameObject.SetActive(room && Flow.CanOpenJournal); journalChamber.interactable = !busy; // Build F
            if (!room) return;
            for (int l = 0; l < locks.Length; l++) Slots.Paint(locks[l], l < Flow.LocksFilled ? Bone : LockDark, l < Flow.LocksFilled ? 1f : LockDarkArt);
            if (ChamberKitted) for (int b = 0; b < SliceFlow.Books; b++) { bookImages[b].sprite = Slots.Image(b < Flow.BooksOpen ? "ckit-book-restored" : "ckit-book-worn"); bookImages[b].color = Color.white; } // Build O
            else for (int b = 0; b < SliceFlow.Books; b++) { bool open = b < Flow.BooksOpen; if (!busy) { Slots.Paint(bookImages[b], open ? BookOpen : Dim, open ? 1f : ShutArt); Slots.Paint(bookPages[b], new Color(Bone.r, Bone.g, Bone.b, open ? .9f : 0), 1f); bookPages[b].rectTransform.anchoredPosition = new Vector2(0, open ? -17 : -35); } }
            if (Flow.BooksOpen > 0) Candles(true);
            bool canSpend = Flow.CanSpend && atBooks && !busy;
            insert.gameObject.SetActive(Flow.CanSpend && atBooks); insert.interactable = canSpend; insert.GetComponentInChildren<Text>().text = Flow.KeysInHand > 1 ? "Insert Key (" + Flow.KeysInHand + " in hand)" : "Insert Key"; // owner (worksheet section 13)
            if (!canSpend) insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0);
            chamberEnd.gameObject.SetActive(Flow.WingWhole && !busy); chamberEnd.text = ChamberEndCard; chamberEnd.rectTransform.anchoredPosition = new Vector2(0, -250); // between the mechanism and the Books, clear of the Keeper; the first visit's card sits lower
            if (string.IsNullOrEmpty(chamberLine)) chamberLine = DefaultChamberLine();
            chamberText.text = chamberLine;
        }
        // Caspar's line in the Chamber room: set on arrival at the doorway or the Books, and by each spend beat; a beat's line stays until the next move.
        string DefaultChamberLine() => Flow.WingWhole && Flow.KeysInHand == 0 ? ChamberWholeLine : Flow.KeysInHand == 0 ? ChamberQuietLine : Flow.Walk.At == "books" ? ChamberChooseLine : (Flow.KeysInHand > 1 ? "You hold " + Flow.KeysInHand + " Keys, acolyte. Bring them to the Books." : "You hold a Key, acolyte. " + ChamberBringLine); // owner (worksheet section 13)
        void Restart() { if (busy) return; PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        // Q06 phase 2, decision 5: the buttons and the taps do the same thing through the same walk.
        void EnterWing() { Walk("wing-door"); }
        // Build F: whether the wheel has a lesson to offer at the fork (Part A stays on the shelf; the book's replay is the shelf's too).
        bool LessonAvailable => Dial.Lesson.CanContinueUnit || (Dial.Lesson.CanBeginGlyphs && Dial.Lesson.AllNamed) || (Dial.Lesson.Phase != LessonPhase.GlyphWheel && Dial.Lesson.CanBeginModalities) || Dial.Lesson.CanBeginOpposites;
        void EnterDialNow()
        {
            if (!Flow.EnterDial()) return;
            Dial.SliceHidesOptional = true; Dial.Lesson.SetKey3(Flow.Keys >= 3);
            Dial.ForceRefresh(); // the Dial's own buttons follow the restored lesson only after a refresh (a reload leaves Continue active by construction)
            bool forkMoment = Flow.CanEnterPractice && !Dial.Lesson.Dial.Active && !Dial.ControlsShown && Dial.Lesson.Phase != LessonPhase.GlyphWheel && !Dial.Lesson.UnitInProgress; // a wheel mid-unit resumes as before; the fork is for an idle wheel
            if (forkMoment) { forkShown = true; Dial.Lesson.Say(LessonAvailable ? ForkLine : Dial.Lesson.CanBeginGlyphs && !Dial.Lesson.AllNamed ? DialLesson.ShelfFirst : ForkPracticeOnlyLine); Show(); Dial.Realign(); Publish(); return; } // the fork (Sept 15 ruling): nothing starts until the player chooses
            StartLesson(); Show(); Dial.Realign(); Publish();
        }
        void ContinueLesson() { if (busy || Dial.Busy || !forkShown || Flow.Screen != SliceScreen.Wing) return; forkShown = false; StartLesson(); Show(); Dial.Realign(); Publish(); }
        void StartLesson()
        {
            if (Dial.Lesson.CanContinueUnit) Dial.Lesson.BeginContinuation();
            else if (Dial.Lesson.CanBeginGlyphs && Dial.Lesson.AllNamed) { if (Dial.Lesson.BeginGlyphs()) Save(); } // Part B: the wheel hides its names
            else if (Dial.Lesson.CanBeginGlyphs) Dial.Lesson.Say(DialLesson.ShelfFirst); // the lit wheel, read only, until the book is read
            else if (Dial.Lesson.Phase != LessonPhase.GlyphWheel && Dial.Lesson.CanBeginModalities) { if (Dial.Lesson.BeginModalities()) { Flow.StartModalities(); Save(); } } // Build A: the second pattern, after Key 2
            else if (Dial.Lesson.CanBeginOpposites) { if (Dial.Lesson.BeginOpposites()) { Flow.StartOpposites(); Save(); } } // Build C: the last pattern and the builder, after Key 3
        }
        void OpenBook()
        {
            if (!Flow.EnterBook()) return;
            Sound.Play("page");
            if (Dial.Lesson.CanBeginGlyphs && !Dial.Lesson.AllNamed) { if (Dial.Lesson.BeginGlyphs()) { Flow.StartGlyphs(); Save(); } }
            else if (Dial.Lesson.CanPractice) Dial.Lesson.BeginPractice(); // after Key 2 the book tests again, harder after a clean run
            else Dial.Lesson.Say(DialLesson.ShelfRead); // Part A done or Key 2 earned: the book only shows its pages closed
            Show(); Publish();
        }
        void CloseBook() { if (busy || !Flow.LeaveBook()) return; Sound.Play("page"); Dial.Lesson.AbandonPractice(); Save(); Show(); Publish(); }
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
                if (Flow.Screen == SliceScreen.Hub && hubKit != null) yield return OpenDoor(hubKit, id); // Build N: the unlocked door swings open as the Keeper reaches it
                Sound.Play("door");
                yield return FadeTo(1);
                if (id == "wing-door") Flow.EnterWing(); else if (id == "chamber-door") { Flow.EnterChamber(); chamberLine = DefaultChamberLine(); } else if (Flow.Walk.Room == Room.Chamber) { Flow.LeaveChamber(); Save(); } else { Flow.LeaveWing(); Save(); }
                Show(); PlaceAvatar(); Publish();
                yield return new WaitForSecondsRealtime(ReducedMotion ? 0 : .12f);
                yield return FadeTo(0);
            }
            else if (id == "desk") { Flow.ApproachDesk(); hubNote.text = Flow.Note; } // Build F: the desk is dressing; the journal is in the inventory
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
            Sound.Play(Flow.AnswerGlyph(seat) ? "seal" : "miss");
            reviewNote.text = Flow.Note; Save(); Publish();
            if (task.done) StartCoroutine(AfterTap());
        }
        void LeaveWing()
        {
            if (busy) return;
            if (Flow.Screen == SliceScreen.Wing) { if (Dial.Busy || Dial.Lesson.Dial.Active || !Flow.LeaveDial()) return; forkShown = false; Save(); Show(); Publish(); }
            if (Flow.Screen == SliceScreen.WingRoom) Walk("atrium-door"); // one press from the Dial walks back out through the room
        }
        // ---- Build F: practice on the Dial. One entry is one sitting; the same forms as before; an exit on every item; three strikes close the instrument. ----
        void EnterPractice()
        {
            if (busy || Dial.Busy || Flow.Screen != SliceScreen.Wing || !forkShown) return;
            forkShown = false;
            bool started = Flow.EnterPractice(); Save();
            if (!started) { forkShown = true; Dial.Lesson.Say(Flow.Note); Show(); Dial.Realign(); Publish(); return; } // nothing due: the sitting counted, Caspar says so, the fork stays
            Show(); StartReviewItem();
        }
        void StartReviewItem()
        {
            var task = Flow.CurrentReview;
            reviewNote.text = "";
            if (task == null) { Show(); Publish(); return; }
            if (task.Mode == ReviewMode.Dial || task.Mode == ReviewMode.DialModality)
            {
                Dial.SliceHidesOptional = true;
                Dial.Lesson.ReviewHeader = "Practice · " + (Flow.ReviewIndex + 1) + " of " + Flow.ReviewQueue.Count; // a check never looks like the lesson (Sept 15 defect)
                Dial.Lesson.BeginReview(task.seat, task.Mode == ReviewMode.DialModality ? 3 : 4); Show(); Dial.Realign(); Publish();
            }
            else { Show(); Publish(); }
        }
        IEnumerator AfterDialReview(bool correct, bool eligible)
        {
            busy = true; Flow.FinishReview(correct, eligible); Save(); Dial.ForceRefresh(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .8f : 1.4f);
            Dial.Lesson.EndReview(); Dial.Realign(); busy = false;
            if (Flow.Gated) { Publish(); yield break; } // the gate closes the instrument from Update once the beat has settled
            StartReviewItem();
        }
        // The gate (owner, Sept 15): the instrument closes, the Keeper is back in the room with the journal offered; re-entry is immediate with fresh strikes.
        IEnumerator Gate()
        {
            gating = true; busy = true; Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .8f : 1.4f);
            if (Dial.Lesson.Phase == LessonPhase.Review) { Dial.Lesson.EndReview(); Dial.Realign(); }
            Flow.CloseInstrument(); Save(); Sound.Play("door");
            busy = false; gating = false; Show(); Publish();
        }
        void LeavePractice()
        {
            if (busy || Dial.Busy || !Flow.AtPractice) return;
            if (Dial.Lesson.Phase == LessonPhase.Review) { Dial.Lesson.EndReview(); Dial.Realign(); }
            Flow.LeavePractice(); Save();
            forkShown = true; Dial.Lesson.Say(ForkReturnLine); Show(); Dial.Realign(); Publish(); // back to the fork on the Dial; unanswered items stay due
        }
        void AnswerModalityTap(string modality)
        {
            if (busy) return;
            var task = Flow.CurrentReview; if (task == null || task.Mode != ReviewMode.TapModality) return;
            Sound.Play(Flow.AnswerModalityTap(modality) ? "seal" : "miss"); Dial.Lesson.Dial.Log("tap_answered");
            reviewNote.text = Flow.Note; Save(); Publish();
            if (task.done) StartCoroutine(AfterTap());
        }
        void AnswerTap(string element)
        {
            if (busy) { Dial.Lesson.Dial.Log("tap_ignored_busy"); return; }
            var task = Flow.CurrentReview; if (task == null || task.Mode != ReviewMode.Tap) { Dial.Lesson.Dial.Log("tap_ignored_task"); return; }
            Sound.Play(Flow.AnswerTap(element) ? "seal" : "miss"); Dial.Lesson.Dial.Log("tap_answered");
            reviewNote.text = Flow.Note; Save(); Publish();
            if (task.done) StartCoroutine(AfterTap());
        }
        IEnumerator AfterTap()
        {
            busy = true; ShowPractice(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? .8f : 1.2f);
            busy = false;
            if (Flow.Gated) { Publish(); yield break; }
            StartReviewItem();
        }

        void Show()
        {
            var s = Flow.Screen;
            var task = Flow.CurrentReview;
            bool practiceOnDial = s == SliceScreen.Practice && task != null && (task.Mode == ReviewMode.Dial || task.Mode == ReviewMode.DialModality) && !task.done;
            identity.gameObject.SetActive(s == SliceScreen.Identity); birth.gameObject.SetActive(s == SliceScreen.Birth);
            journal.gameObject.SetActive(s == SliceScreen.Journal); if (s == SliceScreen.Journal) ShowJournal();
            atrium.gameObject.SetActive(s == SliceScreen.Atrium); atriumReturn.gameObject.SetActive(s == SliceScreen.AtriumReturn);
            chamber.gameObject.SetActive(s == SliceScreen.Chamber || s == SliceScreen.ChamberRoom); hub.gameObject.SetActive(s == SliceScreen.Hub);
            wingRoom.gameObject.SetActive(s == SliceScreen.WingRoom);
            gridScreen.gameObject.SetActive(s == SliceScreen.Grid); if (s == SliceScreen.Grid) ShowGrid();
            bool book = s == SliceScreen.Book;
            bool roomScreen = s == SliceScreen.Hub || s == SliceScreen.WingRoom || s == SliceScreen.ChamberRoom;
            ApplyLight(roomScreen); // Build H
            foreach (var k in atriumKits) if (k.Container.gameObject.activeInHierarchy) ApplyKit(k, true); // Build N: the Atrium shows its stage, turning what it newly restores
            if (roomScreen) { avatar.SetParent(s == SliceScreen.Hub ? hub : s == SliceScreen.WingRoom ? wingRoom : chamber, false); avatar.SetAsLastSibling(); PlaceAvatar(); }
            avatar.gameObject.SetActive(roomScreen);
            if (s == SliceScreen.WingRoom)
            {
                ApplyKit(wingRoomKit, true); // Build M: the room shows the Keys earned, turning what they newly restore
                enterDial.interactable = !busy; wingRoomBack.interactable = !busy;
                enterShelf.gameObject.SetActive(Flow.WheelComplete); enterShelf.interactable = !busy;
                shelfGlow.color = new Color(.95f, .8f, .5f, Flow.WheelComplete && !Dial.Lesson.AllNamed ? .35f : Flow.WheelComplete ? .12f : 0);
                enterGrid.gameObject.SetActive(Flow.CanOpenGrid); enterGrid.interactable = !busy;
                journalWing.gameObject.SetActive(Flow.CanOpenJournal); journalWing.interactable = !busy; // Build F
                gridGlow.color = new Color(.95f, .8f, .5f, Flow.ModalitiesComplete && !Grid.Key3Earned ? .35f : Flow.ModalitiesComplete ? .12f : 0); // an instrument with a unit waiting glows, like the shelf
                if (!DialUnitWaiting) dialGlow.color = new Color(.95f, .8f, .5f, 0); else if (ReducedMotion) dialGlow.color = new Color(.95f, .8f, .5f, .55f); // Build I: the Dial glows too; Update breathes it
                wingRoomCaption.text = Flow.Note == "gated" ? SliceFlow.GateLine // Build F: the instrument closed on the third strike; the journal is below
                    : Flow.Note == "shelf-dark" ? DialLesson.ShelfDark
                    : Flow.Note == "grid-dark" ? GridModel.DarkLine
                    : Dial.Lesson.Phase == LessonPhase.GlyphWheel ? "The wheel has hidden its names. Twelve symbols await you at the Dial." // owner (worksheet, Sept 14 flags)
                    : Dial.Lesson.CanBeginModalities && Dial.Lesson.Phase != LessonPhase.GlyphWheel ? "The second pattern awaits you at the Dial. Go to it." // owner (worksheet section 10)
                    : Flow.ModalitiesComplete && !Grid.Key3Earned ? (Grid.PlacedCount > 0 ? "The table waits, some signs already placed. Go to it." : "A table has woken beside the wheel. Go to it.") // owner (worksheet section 11)
                    : Flow.Keys >= 3 && !Dial.Lesson.Key4Earned ? (Dial.Lesson.OppositesStarted ? "The final pattern awaits. Go to the Dial." : "The wheel holds one last pattern. Go to the Dial.") // owner (worksheet section 12)
                    : Dial.Lesson.CanPractice ? (Dial.Lesson.Hard ? "The symbols are yours. The book stirs, ready to test you harder." : "The symbols are yours. The book stirs, ready to test you again.") // owner (worksheet section 9)
                    : Flow.WheelComplete && !Dial.Lesson.AllNamed ? "All twelve signs alight upon the wheel. The shelf glows, something within it has woken." // owner (worksheet section 7)
                    : "The Dial stands at the center of the room. The doorway behind you leads back to the Atrium."; // owner (worksheet section 6)
            }
            bool partA = book;
            review.gameObject.SetActive(s == SliceScreen.Practice && !practiceOnDial);
            glyphs.gameObject.SetActive(partA);
            Dial.UiCanvas.gameObject.SetActive(s == SliceScreen.Wing || practiceOnDial);
            leavePracticeDial.gameObject.SetActive(practiceOnDial); leavePracticeDial.interactable = !busy; // Build F: an exit on the wheel form too
            if (s != SliceScreen.Wing) { forkLesson.gameObject.SetActive(false); forkPractice.gameObject.SetActive(false); }
            if (partA) ShowGlyphs();
            if (birthContinue != null) birthContinue.interactable = Flow.CanContinue;
            if (s == SliceScreen.Wing || practiceOnDial)
            {
                if (!sunSent && Flow.HasSunSign) { Dial.Lesson.SetSunSign(Flow.SunSign); sunSent = true; }
                if (Flow.AtriumStage >= 2) { keyRect.gameObject.SetActive(false); keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); seam.color = new Color(Bone.r, Bone.g, Bone.b, 0); Dial.Ring.localScale = Vector3.one; }
                if (Flow.AtriumStage >= 2) { var t = wingContinue.GetComponentInChildren<Text>(); t.text = "Return to the Atrium"; } // owner (worksheet section 6)
                Dial.ForceRefresh();
            }
            if (s == SliceScreen.Hub) ShowHub();
            if (s == SliceScreen.Practice) ShowPractice();
            ShowPage();
            if (s == SliceScreen.Chamber || s == SliceScreen.ChamberRoom) ShowChamberRoom(); // after ShowPage: the room owns the Chamber's controls
        }
        void ShowGlyphs()
        {
            var lesson = Dial.Lesson; int target = lesson.CurrentGlyph;
            bool naming = lesson.Phase == LessonPhase.GlyphNames;
            closeBook.interactable = !busy;
            var page = Slots.Image(naming ? "book-page" : "book-cover"); bookCard.sprite = page; bookCard.color = page != null ? Color.white : PanelColor; // Build E
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
            gridStatus.text = g.Phase == GridPhase.Complete ? "All twelve placed. Keeper Key 3 earned." : g.Phase == GridPhase.Paused ? "Paused for now" : "The table · " + g.PlacedCount + " of 12 placed"; // owner (worksheet section 11) // no level numbers on screen (owner, Sept 13)
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
            Lamp(lampOne, stage >= 2); Lamp(lampTwo, stage >= 3); Lamp(lampThree, stage >= 5); Lamp(lampFour, stage >= 6);
            foreach (var book in shelfBooks) book.gameObject.SetActive(stage >= 4); shelvesLabel.text = stage >= 4 ? "Shelves, filling with books" : "Shelves, mostly bare"; // owner (worksheet section 13)
            sealedLeftLight.color = new Color(.95f, .8f, .5f, stage >= 5 ? (HasArt(sealedLeftLight.transform.parent as RectTransform) ? .06f : .3f) : 0);
            hubCaption.text = stage >= 6 ? "Four lamps, shelves filling. The Zodiac Wing is whole." : stage >= 5 ? "Three lamps burn, and light glows behind a sealed door." : stage >= 4 ? "The shelves stir, books returning to their places." : stage >= 3 ? "Stirring: two lamps, a clear desk, the Zodiac Wing open." : "Stirring: one lamp lit, one desk uncovered, the Zodiac Wing open."; // owner (worksheet sections 1 and 13; Stages 2 and 3 kept as written, marked X)
            hubText.text = Flow.KeysInHand > 1 ? string.Format(HubKeysInHandLine, Flow.KeysInHand) : Flow.KeysInHand == 1 ? HubKeyInHandLine : Flow.WingWhole ? HubWholeLine : Flow.LocksFilled >= 3 ? HubSpent3Line : Flow.LocksFilled >= 2 ? HubSpent2Line : Flow.V02Complete ? HubCompleteLine : Resumed || Flow.Sittings > 0 ? HubLaterLine : HubFirstLine;
            enterChamber.interactable = !busy && Flow.CanEnterChamber;
            journalHub.gameObject.SetActive(Flow.CanOpenJournal); journalHub.interactable = !busy; // Build F
            enterWing.GetComponentInChildren<Text>().text = "The Zodiac Wing";
            endCard.text = Flow.WingWhole ? ChamberEndCard : Flow.Keys >= 4 ? "Four Keys earned. The Chamber awaits them." : Flow.Keys >= 3 ? "Three Keys earned. The Chamber awaits them." : Flow.Keys >= 2 ? "Two Keys earned. The Chamber awaits them." : "The whole wheel burns. The symbols await you next."; // owner (worksheet sections 1 and 13)
            hubNote.text = Flow.Note;
            endCard.gameObject.SetActive(Flow.V02Complete || Flow.Keys >= 2);
        }
        void ShowPractice()
        {
            var task = Flow.CurrentReview;
            bool done = Flow.PracticeDone;
            reviewProgress.text = done ? "Practice finished" : "Practice · " + (Flow.ReviewIndex + 1) + " of " + Flow.ReviewQueue.Count;
            bool glyphItem = !done && task != null && task.Mode == ReviewMode.Glyph;
            bool modItem = !done && task != null && task.Mode == ReviewMode.TapModality;
            reviewQuestion.text = done ? "" : task != null && task.Mode == ReviewMode.Tap ? Zodiac.Seats[task.seat].Name + ". To which element does this sign owe its nature?" : modItem ? Zodiac.Seats[task.seat].Name + ". Which modality?" : glyphItem ? "Which sign does this symbol belong to?" : ""; // owner (worksheet sections 2, 5, 10)
            foreach (var b in elementButtons) { b.gameObject.SetActive(!done && task != null && task.Mode == ReviewMode.Tap); b.interactable = !busy && task != null && !task.done; }
            foreach (var b in modalityButtons) { b.gameObject.SetActive(modItem); b.interactable = !busy && task != null && !task.done; }
            reviewGlyph.gameObject.SetActive(glyphItem); reviewGlyph.text = glyphItem ? Zodiac.Seats[task.seat].Glyph : "";
            var opts = glyphItem ? Flow.GlyphReviewOptions(task.seat) : new int[4];
            for (int i = 0; i < 4; i++) { reviewGlyphButtons[i].gameObject.SetActive(glyphItem); reviewGlyphButtons[i].GetComponentInChildren<Text>().text = glyphItem ? Zodiac.Seats[opts[i]].Name : ""; reviewGlyphButtons[i].interactable = !busy && glyphItem && !task.done; }
            reviewSummary.text = done ? Flow.PracticeSummary : "";
            leavePractice.gameObject.SetActive(true); leavePractice.interactable = !busy; leavePractice.GetComponentInChildren<Text>().text = done ? "Back to the Dial" : "Leave the instrument";
            if (done) reviewNote.text = "The practice is done. Go and read your journal, for the wheel has given you much to consider."; // owner (worksheet section 2)
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
        void LightWing() { FloorLight(.8f); Slots.Paint(candle, LampLit, 1f); }
        // Build E: the floor's lines at an alpha, or the floor's file at a brightness that follows it.
        void FloorLight(float alpha) { foreach (var line in floorLines) if (line != null) line.color = new Color(.62f, .57f, .53f, alpha); if (floorArt != null && floorArt.enabled) floorArt.color = Color.white * (.45f + .55f * alpha / .8f); }
        static void Lamp(Image lamp, bool lit) => Slots.Paint(lamp, lit ? LampLit : LampDark, lit ? 1f : DarkArt);
        static bool HasArt(RectTransform r) { var image = r.GetComponent<Image>(); return image != null && image.sprite != null; }
        static string MuteLabel => "Sound: " + (Sound.Muted ? "off" : "on") + " (test)";
        void ToggleMute()
        {
            Sound.ToggleMute();
            if (mute != null) mute.GetComponentInChildren<Text>().text = MuteLabel; if (styleMute != null) styleMute.GetComponentInChildren<Text>().text = MuteLabel;
            Publish();
        }
        public void Publish() => Dial.Publish();
        public void Fill(DialView.WebState state)
        {
            if (styleShown) { FillStyle(state); return; }
            var s = Flow.Screen; var task = Flow.CurrentReview;
            bool practiceOnDial = s == SliceScreen.Practice && task != null && (task.Mode == ReviewMode.Dial || task.Mode == ReviewMode.DialModality) && !task.done;
            state.screen = s.ToString().ToLowerInvariant(); state.playerName = Flow.DisplayName; state.note = Flow.Note;
            state.busy = state.busy || busy; // The semantic layer must see the slice's own beats as busy too.
            state.caspar = s == SliceScreen.Identity ? "Who are you? Enter a name, then continue." :
                s == SliceScreen.Birth ? "Do you know when you were born?" + (Flow.BirthChoice == "chart" && !Flow.HasSunSign ? " Enter your birth month and day." : Flow.BirthChoice == "known" && !Flow.HasSunSign ? " Choose your sign." : "") :
                s == SliceScreen.Atrium ? atriumText.text : s == SliceScreen.AtriumReturn ? returnText.text :
                s == SliceScreen.Chamber ? chamberText.text + (Flow.Ended ? " " + chamberEnd.text : "") :
                s == SliceScreen.Hub ? hubText.text + " " + hubCaption.text :
                s == SliceScreen.Practice && !practiceOnDial ? (Flow.PracticeDone ? Flow.PracticeSummary + " " + reviewNote.text : reviewProgress.text + ". " + reviewQuestion.text + " " + reviewNote.text) :
                s == SliceScreen.Journal ? "Your journal. " + journalSectionText.text + " " + journalNote.text : "";
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
            state.canEnterWing = s == SliceScreen.Hub && !busy;
            state.canLeaveWing = s == SliceScreen.Wing && Flow.AtriumStage >= 2 && wingContinue.gameObject.activeSelf && !busy && Dial.Lesson.Phase != LessonPhase.GlyphNames;
            // Build F: the fork, practice, the gate, the journal
            state.practicing = s == SliceScreen.Practice; state.canLeavePractice = state.practicing && !busy;
            state.practiceMode = !state.practicing ? "" : Flow.PracticeDone ? "done" : task != null && (task.Mode == ReviewMode.Dial || task.Mode == ReviewMode.DialModality) ? "dial" : task != null && task.Mode == ReviewMode.TapModality ? "modality" : "tap";
            state.practiceIndex = Flow.ReviewIndex; state.practiceCount = Flow.ReviewQueue.Count; state.practiceSign = task != null && !Flow.PracticeDone ? Zodiac.Seats[task.seat].Name : "";
            state.practiceSummary = Flow.PracticeSummary; state.strikes = Flow.Strikes; state.sitting = Flow.Sittings; state.gated = Flow.Note == "gated";
            bool forkOpen = s == SliceScreen.Wing && forkShown && Flow.CanEnterPractice && !busy && !Dial.Busy;
            state.canEnterPractice = forkOpen; state.canContinueLesson = forkOpen && LessonAvailable;
            state.fork = !forkOpen ? "none" : LessonAvailable ? "both" : "practice";
            state.journal = s == SliceScreen.Journal; state.canOpenJournal = Flow.CanOpenJournal && !busy; state.canCloseJournal = state.journal && !busy;
            state.canJournalNext = Flow.CanJournalNext && !busy; state.canJournalPrev = Flow.CanJournalPrev && !busy;
            state.journalPage = Flow.JournalSection; state.journalCount = Flow.JournalSections.Count;
            state.journalSection = state.journal ? SliceFlow.SectionTitle(Flow.JournalKind) : ""; state.journalEntries = state.journal ? Flow.JournalEntries(Flow.JournalKind).ToArray() : new string[0];
            state.hubNote = hubNote != null ? hubNote.text : ""; state.v02Complete = Flow.V02Complete;
            bool partA = s == SliceScreen.Book && Dial.Lesson.Phase == LessonPhase.GlyphNames;
            bool glyphItem = s == SliceScreen.Practice && task != null && !Flow.PracticeDone && task.Mode == ReviewMode.Glyph;
            state.glyphMode = partA ? "name" : glyphItem ? "review" : "";
            if (partA || glyphItem)
            {
                int target = partA ? Dial.Lesson.CurrentGlyph : task.seat;
                var opts = partA ? Dial.Lesson.GlyphOptions(target) : Flow.GlyphReviewOptions(target);
                state.glyphChar = Zodiac.Seats[target].Glyph; state.glyphOptions = new[] { Zodiac.Seats[opts[0]].Name, Zodiac.Seats[opts[1]].Name, Zodiac.Seats[opts[2]].Name, Zodiac.Seats[opts[3]].Name };
                if (partA) state.caspar = "This symbol belongs to which sign? " + (glyphNote.text ?? "") + " " + DialLesson.GlyphIntro;
            }
            if (glyphItem) state.practiceMode = "glyph";
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
            // Build H: the light overlays
            state.lightAlpha = lightAlpha; state.lightFiles = LightSlots.Count(slot => Slots.Image(slot) != null);
            // Build B: the table
            state.gridOpen = Flow.ModalitiesComplete; state.gridStarted = Flow.GridStarted; state.key3 = Grid.Key3Earned; state.gridPlaced = Grid.PlacedCount;
            state.gridComplete = Grid.Complete; state.gridPaused = Grid.Phase == GridPhase.Paused; state.gridHintLevel = Grid.HintLevel;
            state.canEnterGrid = s == SliceScreen.WingRoom && Flow.CanOpenGrid && !busy;
            state.keys = Math.Max(state.keys, Flow.Keys); // the lesson counts two Keys; the table adds the third
            state.keyCeremony = LastCeremony; state.dialGlow = DialUnitWaiting ? 1 : 0; // Build I
            state.kitLevel = KitLevel; state.kitPieces = wingKit.Count; state.kitRestored = KitRestored; state.grime = wingGrime != null && wingGrime.gameObject.activeSelf ? wingGrime.color.a : -1; state.wingLight = wingLight != null && wingLight.gameObject.activeSelf ? wingLight.color.a : -1; // Build M
            state.kitUp = wingKit.Where(p => p.Shown).Select(p => p.P.Name).Distinct().ToArray();
            if (chamberKit != null) { state.chamberKitLevel = chamberKit.Shown; state.chamberKitRestored = chamberKit.Pieces.Count(p => p.Shown); state.chamberKitPieces = chamberKit.Pieces.Count; } // Build O
            if (hubKit != null) { state.atriumKitLevel = hubKit.Shown; state.atriumKitPieces = hubKit.Pieces.Count; state.atriumKitRestored = hubKit.Pieces.Count(p => p.Shown); state.atriumGrime = hubKit.GrimeImage.gameObject.activeSelf ? hubKit.GrimeImage.color.a : -1; state.doors = hubKit.Doors.Select(d => d.Id + ":" + (d.Shown ? "unlocked" : "locked")).ToArray(); state.lastDoorOpened = LastDoorOpened; } // Build N
            if (s == SliceScreen.Grid)
            {
                state.caspar = Grid.Message; state.gridReadout = Grid.Readout; state.gridStatus = gridStatus.text;
                state.gridSign = Grid.Sign >= 0 ? Zodiac.Seats[Grid.Sign].Name : ""; state.gridCell = Grid.Cell; state.gridLocked = Grid.Locked;
                state.gridTiles = Enumerable.Range(0, 12).Select(i => Grid.TileLabel(i)).ToArray(); state.gridCells = Enumerable.Range(0, 12).Select(i => Grid.CellLabel(i)).ToArray();
                state.canGridPick = Grid.Active && !busy; state.canGridSeal = Grid.CanSeal && !busy; state.canGridAsk = Grid.CanAsk && !busy; state.canLeaveGrid = !busy;
            }
        }

        // ---- Build H: the light overlays. A file in a room's light slot is drawn over the background and under everything else; without one nothing exists. ----
        Image LightOverlay(RectTransform panel, string slot)
        {
            var r = Rect("Light overlay", panel, 0, 400, 360, 800); var image = r.gameObject.AddComponent<Image>(); image.raycastTarget = false;
            bool file = Slots.Dress(image, slot); image.gameObject.SetActive(file); image.color = new Color(1, 1, 1, 0); r.SetAsFirstSibling();
            lightOverlays.Add(image); return image;
        }
        void ApplyLight(bool animate)
        {
            float target = LightAlphaFor(Flow.AtriumStage);
            if (Mathf.Approximately(target, lightAlpha)) return;
            if (lightFade != null) { StopCoroutine(lightFade); lightFade = null; }
            if (!animate || ReducedMotion) { SetLight(target); return; }
            lightFade = StartCoroutine(FadeLight(target));
        }
        void SetLight(float alpha) { lightAlpha = alpha; foreach (var overlay in lightOverlays) overlay.color = new Color(1, 1, 1, alpha); }
        IEnumerator FadeLight(float target) { float from = lightAlpha; yield return Tween(.8f, k => SetLight(Mathf.Lerp(from, target, k))); lightFade = null; Publish(); } // the settled alpha reaches the web state

        // ---- Build F: the journal. In the inventory (owner, Sept 15): a button in every room, never a room object. It renders straight from the
        // deck, one section per kind the wheel has taught, only the items that have entered, in wheel order, with 08's state word. Reading changes nothing. ----
        void BuildJournal()
        {
            journal = ScreenPanel("Journal", "journal-page");
            Label(journal, "YOUR JOURNAL", 0, 32, 340, 24, 18);
            var cover = Rect("Journal cover", journal, 0, 74, 60, 60); journalCover = cover.gameObject.AddComponent<Image>(); journalCover.color = PanelColor; journalCover.raycastTarget = false; cover.gameObject.SetActive(Slots.Dress(journalCover, "journal-cover"));
            journalSectionText = Label(journal, "", 0, 116, 330, 26, 16);
            for (int i = 0; i < 12; i++)
            {
                journalGlyphs[i] = Label(journal, "", -140, 150 + i * 36, 40, 36, 22); journalGlyphs[i].font = Dial.GlyphFont; journalGlyphs[i].horizontalOverflow = HorizontalWrapMode.Overflow; journalGlyphs[i].verticalOverflow = VerticalWrapMode.Overflow;
                journalLines[i] = Label(journal, "", 24, 150 + i * 36, 280, 36, 12); journalLines[i].alignment = TextAnchor.MiddleLeft;
            }
            journalNote = Label(journal, "", 0, 596, 330, 40, 12); journalNote.color = Muted;
            journalPrev = MakeButton(journal, "Previous", -78, 654, 150, 56, () => JournalTurn(-1));
            journalNext = MakeButton(journal, "Next", 78, 654, 150, 56, () => JournalTurn(1));
            journalClose = MakeButton(journal, "Close the journal", 0, 714, 190, 48, CloseJournal);
        }
        void ShowJournal()
        {
            var kind = Flow.JournalKind; var items = Flow.JournalItems(kind);
            journalSectionText.text = SliceFlow.SectionTitle(kind) + " · " + (Flow.JournalSection + 1) + " of " + Flow.JournalSections.Count;
            for (int i = 0; i < 12; i++)
            {
                bool show = i < items.Count;
                journalGlyphs[i].gameObject.SetActive(show && kind != ItemKind.Opposite); journalLines[i].gameObject.SetActive(show);
                if (!show) continue;
                journalGlyphs[i].text = Zodiac.Seats[items[i].seat].Glyph;
                journalLines[i].text = SliceFlow.JournalName(items[i]) + " — " + SliceFlow.JournalFact(items[i]) + "\n" + SliceFlow.StateWord(items[i]);
                journalLines[i].color = items[i].State == ItemState.Practicing ? Bone : Muted;
            }
            journalNote.text = "What the wheel has shown you, as it stands. Reading here proves nothing; the wheel does that."; // placeholder (owner writes)
            journalPrev.interactable = Flow.CanJournalPrev && !busy; journalNext.interactable = Flow.CanJournalNext && !busy; journalClose.interactable = !busy;
        }
        void OpenJournal() { if (busy || !Flow.OpenJournal()) return; Sound.Play("page"); Show(); Publish(); }
        void CloseJournal() { if (busy || !Flow.CloseJournal()) return; Sound.Play("page"); Save(); Show(); Publish(); }
        void JournalTurn(int direction) { if (busy || !(direction > 0 ? Flow.JournalNext() : Flow.JournalPrev())) return; Sound.Play("page"); ShowJournal(); Publish(); }

        // ---- save / restore (Q05 decision 5) ----
        void Save()
        {
            if (Flow.AtriumStage < 2) return;
            try { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Flow.CaptureProgress(Dial.Lesson, Grid))); PlayerPrefs.Save(); }
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
            sunSent = true; revealStarted = true; Resumed = true; Dial.SliceHidesOptional = true; SetLight(LightAlphaFor(save.atriumStage)); // Build H: no fade on a reload
            keyIndicator.gameObject.SetActive(save.keyEarned); if (save.wheelComplete) LightWing(); else if (save.keyEarned) { FloorLight(.55f); Slots.Paint(candle, Bone, 1f); }
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
            var keyImage = keyRect.GetComponent<Image>(); Sound.Play("key");
            yield return Tween(ReducedMotion ? 0 : .9f, k => {
                keyRect.anchoredPosition = new Vector2(0, -270 + 100 * k);
                Slots.Paint(keyImage, new Color(Bone.r, Bone.g, Bone.b, k), 1f); keyLabel.color = new Color(Charcoal.r, Charcoal.g, Charcoal.b, k);
                keyGlow.rectTransform.anchoredPosition = keyRect.anchoredPosition; keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, .35f * k);
            });
            yield return new WaitForSecondsRealtime(ReducedMotion ? .3f : .6f);
            yield return Tween(ReducedMotion ? 0 : .5f, k => keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, .35f - .23f * k));
            Dial.Lesson.Say("Aah... the Library stirs."); // Q04 locked line.
            Dial.ForceRefresh();
            FloorLight(.55f);
            Slots.Paint(candle, Bone, 1f); keyIndicator.gameObject.SetActive(true);
            Flow.RevealKey(); wingContinue.gameObject.SetActive(true);
            busy = false; Publish();
        }
        // Build I (task 86bc1brxu): Keys 2 to 4 get the Key 1 ceremony on the screen where they are earned: the seam splits the wheel
        // (on the Dial), the Key rises and glows, the count ticks, the Key settles into the count. Under two seconds; reduced motion cuts.
        // ---- Build M: the Wing room kit (owner, Sept 24). The shell is the restored architecture; a grime layer and a dark veil sit over it
        // at the start and wear away Key by Key; each piece has a worn and a restored file and turns when its Key comes, light burning across it.
        // Placements are bottom-centred on the 360 x 800 layout, measured on the owner's approved "restored" image (shifted up 60 with the shell).
        struct KitPlacement
        {
            public string Name, Wake; public float X, Bottom, Scale, WornScale, WornX; public int Key;
            public KitPlacement(string name, float x, float bottom, int key, float scale = 1, float wornScale = 0, float wornX = float.NaN, string wake = null) { Name = name; X = x; Bottom = bottom; Key = key; Scale = scale; WornScale = wornScale; WornX = wornX; Wake = wake; }
        }
        static readonly KitPlacement[] WingKit =
        {
            new KitPlacement("window", -10, 247, 1, .82f),
            new KitPlacement("carpet", -1, 565, 3, 1, .6f, -10),
            new KitPlacement("chandelier", -64, 135, 4),
            new KitPlacement("banner", -156, 135, 4),
            new KitPlacement("banner", -99, 190, 4),
            new KitPlacement("banner", 62, 170, 4, .83f),
            new KitPlacement("orrery", 147, 170, 4),
            new KitPlacement("armillary", -59, 285, 3),
            new KitPlacement("lectern", -110, 372, 2, .8f),
            new KitPlacement("globe", -30, 350, 3),
            new KitPlacement("shelf", 157, 440, 2, 1, .7f, 118, "wheel"), // the book of symbols wakes on it once the wheel is lit: it must stand by then (owner, Sept 25)
            new KitPlacement("dial", 40, 440, 4),
            new KitPlacement("telescope", 139, 445, 4),
            new KitPlacement("table", -62, 445, 3, wake: "modalities"), // the Table lesson happens on it: it rights itself when it wakes, not at Key 3 (owner, Sept 25)
            new KitPlacement("candles", -144, 185, 1),
            new KitPlacement("candles", 125, 205, 1, .75f),
            new KitPlacement("books", 160, 490, 2),
            new KitPlacement("chair", 144, 475, 3),
            new KitPlacement("plate", -138, 222, 1),
        };
        public static readonly float[] KitGrime = { 1, .75f, .5f, .25f, 0 }, KitVeil = { .45f, .3f, .18f, .08f, 0 }, KitLight = { 0, .25f, .5f, .75f, 1 }; // by Keys earned, 0 to 4 (tuning variables)
        const string WingPlateName = "THE GRAND ATRIUM"; // the Wing's doorway leads back to the Atrium
        class KitPiece { public KitPlacement P; public CanvasGroup Worn, Restored; public Image Flash; public bool Shown; }
        // Build N (owner, Sept 25): a door is weathered and chained while locked, clean with its edges glowing once unlocked, and swings open as the
        // Keeper reaches it. The leaves are the two halves of the closed door's file, each swinging toward its outer edge.
        class DoorPiece
        {
            public string Id, Name; public CanvasGroup Locked, Closed, Open, PlateLocked, PlateClean; public RectTransform LeafL, LeafR; public Image Glow, Flash; public bool Shown;
        }
        // Build N: the Wing's kit machinery made reusable per room: grime and a veil over the shell by level, pieces worn or restored, doors.
        class RoomKit
        {
            public string Prefix; public KitPlacement[] Placements; public float[] Grime, Veil, Light; public System.Func<int> Level; public System.Func<KitPlacement, bool> RestoredNow; public System.Func<string, bool> DoorUnlocked;
            public Image GrimeImage, VeilImage, LightImage; public bool OwnsLight; public RectTransform Container; public readonly List<KitPiece> Pieces = new List<KitPiece>(); public readonly List<DoorPiece> Doors = new List<DoorPiece>();
            public int Shown = -1; public Coroutine Fade; public Color VeilTint = Color.black;
        }
        RoomKit wingRoomKit; readonly List<RoomKit> atriumKits = new List<RoomKit>(); RoomKit hubKit;
        List<KitPiece> wingKit => wingRoomKit != null ? wingRoomKit.Pieces : new List<KitPiece>();
        Image wingGrime => wingRoomKit?.GrimeImage; Image wingLight;
        public int KitLevel => wingRoomKit != null ? wingRoomKit.Shown : -1;
        public int KitRestored => wingKit.Count(p => p.Restored != null && p.Restored.alpha > .99f);
        RoomKit BuildKit(RectTransform panel, string prefix, KitPlacement[] placements, string grimeSlot, Image light, float[] grime, float[] veil, float[] lightLevels, System.Func<int> level, System.Func<KitPlacement, bool> restoredNow)
        {
            var k = new RoomKit { Prefix = prefix, Placements = placements, Grime = grime, Veil = veil, Light = lightLevels, Level = level, RestoredNow = restoredNow, LightImage = light };
            var g = Rect("Grime", panel, 0, 400, 360, 800); k.GrimeImage = g.gameObject.AddComponent<Image>(); k.GrimeImage.raycastTarget = false;
            g.gameObject.SetActive(Slots.Dress(k.GrimeImage, grimeSlot)); g.SetAsFirstSibling(); // under the light overlay, over the shell
            k.Container = Rect("Kit", panel, 0, 400, 360, 800);
            foreach (var p in placements)
            {
                if (Slots.Image(prefix + p.Name + "-restored") == null) continue; // no file, no piece: the greybox stays as it was
                k.Pieces.Add(new KitPiece { P = p, Worn = KitState(k, p, "worn"), Restored = KitState(k, p, "restored") });
            }
            return k;
        }
        void FinishKit(RectTransform panel, RoomKit k)
        {
            var v = Rect("Veil", panel, 0, 400, 360, 800); k.VeilImage = v.gameObject.AddComponent<Image>(); k.VeilImage.color = new Color(0, 0, 0, 0); k.VeilImage.raycastTarget = false;
            v.gameObject.SetActive(k.Pieces.Count > 0 || k.Doors.Count > 0);
            if (k.Pieces.Count > 0 && k.LightImage != null && k.Light != null) { lightOverlays.Remove(k.LightImage); k.OwnsLight = true; } // the Wing's light follows its Keys (owner, Sept 25)
        }
        CanvasGroup KitState(RoomKit k, KitPlacement p, string state)
        {
            string slot = k.Prefix + p.Name + "-" + state; var sprite = Slots.Image(slot); if (sprite == null) return null;
            bool worn = state == "worn"; float scale = worn && p.WornScale > 0 ? p.WornScale : p.Scale; float x = worn && !float.IsNaN(p.WornX) ? p.WornX : p.X;
            float w = sprite.rect.width / 2 * scale, h = sprite.rect.height / 2 * scale; // files are drawn at twice their size on the layout
            var r = Rect(p.Name + " (" + state + ")", k.Container, x, p.Bottom - h / 2, w, h); var image = r.gameObject.AddComponent<Image>(); image.raycastTarget = false; Slots.Dress(image, slot);
            return r.gameObject.AddComponent<CanvasGroup>();
        }
        Text PlateText(RectTransform plate, string text)
        {
            var name = Label(plate, text, 0, plate.sizeDelta.y / 2, plate.sizeDelta.x * .8f, plate.sizeDelta.y * .7f, 9); name.lineSpacing = .85f;
            name.color = new Color(.23f, .14f, .07f); name.fontStyle = FontStyle.Bold; name.resizeTextForBestFit = true; name.resizeTextMinSize = 6; name.resizeTextMaxSize = 11; return name;
        }
        // Doors fill the painted arch openings; the plate sits on the arch's keystone.
        void AddDoor(RoomKit k, string id, string name, float x, float bottom, float w, float h, float plateBottom, float plateW)
        {
            var door = new DoorPiece { Id = id, Name = name };
            var glow = Rect(id + " glow", k.Container, x, bottom - h / 2, w * 1.55f + 20, h * 1.25f + 20); door.Glow = glow.gameObject.AddComponent<Image>(); door.Glow.sprite = SoftGlow(); door.Glow.raycastTarget = false; door.Glow.color = new Color(1, .82f, .5f, 0);
            CanvasGroup Face(string slot, string label)
            {
                if (Slots.Image(slot) == null) return null;
                var r = Rect(id + " (" + label + ")", k.Container, x, bottom - h / 2, w, h); var image = r.gameObject.AddComponent<Image>(); image.raycastTarget = false; Slots.Dress(image, slot); return r.gameObject.AddComponent<CanvasGroup>();
            }
            door.Locked = Face(k.Prefix + "door-locked", "locked"); door.Open = Face(k.Prefix + "door-open", "open");
            var closed = Slots.Image(k.Prefix + "door-closed");
            if (closed != null)
            {
                var c = Rect(id + " (closed)", k.Container, x, bottom - h / 2, w, h); door.Closed = c.gameObject.AddComponent<CanvasGroup>();
                RectTransform Leaf(bool left)
                {
                    var leaf = Rect(left ? "Leaf left" : "Leaf right", c, 0, h / 2, w / 2, h); leaf.pivot = new Vector2(left ? 0 : 1, .5f); leaf.anchoredPosition = new Vector2(left ? -w / 2 : w / 2, -h / 2);
                    var raw = leaf.gameObject.AddComponent<RawImage>(); raw.texture = closed.texture; raw.raycastTarget = false; var t = closed.textureRect; float tw = closed.texture.width, th = closed.texture.height;
                    raw.uvRect = new UnityEngine.Rect(t.x / tw + (left ? 0 : t.width / tw / 2), t.y / th, t.width / tw / 2, t.height / th); return leaf;
                }
                door.LeafL = Leaf(true); door.LeafR = Leaf(false);
            }
            if (Slots.Image(k.Prefix + "plate-clean") != null)
            {
                float ph = plateW * .42f;
                door.PlateLocked = Face2(k.Prefix + "plate-locked"); door.PlateClean = Face2(k.Prefix + "plate-clean");
                if (door.PlateClean != null && name != null) PlateText((RectTransform)door.PlateClean.transform, name);
                CanvasGroup Face2(string slot) { if (Slots.Image(slot) == null) return null; var r = Rect(id + " plate", k.Container, x, plateBottom - ph / 2, plateW, ph); var image = r.gameObject.AddComponent<Image>(); image.raycastTarget = false; Slots.Dress(image, slot); return r.gameObject.AddComponent<CanvasGroup>(); }
            }
            if (door.Closed != null || door.Locked != null) k.Doors.Add(door);
        }
        void ApplyKit(RoomKit k, bool animate)
        {
            if (k == null || (k.Pieces.Count == 0 && k.Doors.Count == 0)) return;
            int level = k.Level();
            if (k.Fade != null) { StopCoroutine(k.Fade); k.Fade = null; SetKit(k, k.Shown); }
            var turning = k.Shown < 0 ? new List<KitPiece>() : k.Pieces.Where(p => !p.Shown && k.RestoredNow(p.P)).ToList();
            var opening = k.Shown < 0 ? new List<DoorPiece>() : k.Doors.Where(d => !d.Shown && k.DoorUnlocked != null && k.DoorUnlocked(d.Id)).ToList();
            if (animate && k.Shown >= 0 && (turning.Count > 0 || opening.Count > 0 || level > k.Shown) && !ReducedMotion) k.Fade = StartCoroutine(RestoreKit(k, Mathf.Max(k.Shown, 0), level, turning, opening));
            else SetKit(k, level);
            k.Shown = level;
        }
        void SetKit(RoomKit k, int level)
        {
            foreach (var piece in k.Pieces)
            {
                bool restored = k.RestoredNow(piece.P); piece.Shown = restored;
                if (piece.Restored != null) piece.Restored.alpha = restored ? 1 : 0;
                if (piece.Worn != null) piece.Worn.alpha = restored ? 0 : 1;
                if (piece.Flash != null) piece.Flash.color = new Color(1, .85f, .55f, 0);
            }
            foreach (var d in k.Doors)
            {
                bool open = k.DoorUnlocked != null && k.DoorUnlocked(d.Id); d.Shown = open;
                if (d.Locked != null) d.Locked.alpha = open ? 0 : 1;
                if (d.Closed != null) d.Closed.alpha = open ? 1 : 0;
                if (d.Open != null) d.Open.alpha = 0;
                if (d.LeafL != null) { d.LeafL.localScale = Vector3.one; d.LeafR.localScale = Vector3.one; }
                if (d.PlateLocked != null) d.PlateLocked.alpha = open ? 0 : 1;
                if (d.PlateClean != null) d.PlateClean.alpha = open ? 1 : 0;
                if (d.Flash != null) d.Flash.color = new Color(1, .85f, .55f, 0);
                d.Glow.color = new Color(1, .82f, .5f, open ? .4f : 0);
            }
            int i = Mathf.Clamp(level, 0, k.Grime.Length - 1);
            if (k.GrimeImage != null) k.GrimeImage.color = new Color(1, 1, 1, k.Grime[i]);
            if (k.VeilImage != null) k.VeilImage.color = new Color(k.VeilTint.r, k.VeilTint.g, k.VeilTint.b, k.Veil[i]);
            if (k.OwnsLight) k.LightImage.color = new Color(1, 1, 1, k.Light[i]);
        }
        Image FlashFor(RectTransform target, string name)
        {
            var flash = Rect(name + " (light)", target.parent, target.anchoredPosition.x, -target.anchoredPosition.y, target.sizeDelta.x * 1.5f + 30, target.sizeDelta.y * 1.3f + 30);
            var image = flash.gameObject.AddComponent<Image>(); image.sprite = SoftGlow(); image.raycastTarget = false; image.color = new Color(1, .85f, .55f, 0); return image;
        }
        IEnumerator RestoreKit(RoomKit k, int from, int to, List<KitPiece> turning, List<DoorPiece> opening)
        {
            // Light burns across each piece newly earned: a gold flash swells, the worn file gives way to the restored one, the grime thins.
            // A door newly unlocked turns the same way: the chains and grime give way to the clean door, the plate clears, the edges start to glow.
            foreach (var piece in turning) if (piece.Flash == null && piece.Restored != null) piece.Flash = FlashFor((RectTransform)piece.Restored.transform, piece.P.Name);
            foreach (var d in opening) if (d.Flash == null && d.Closed != null) d.Flash = FlashFor((RectTransform)d.Closed.transform, d.Id);
            yield return new WaitForSecondsRealtime(.35f);
            int a = Mathf.Clamp(from, 0, k.Grime.Length - 1), b = Mathf.Clamp(to, 0, k.Grime.Length - 1);
            yield return Tween(1.4f, t => {
                float turn = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * 1.6f - .3f)), flare = .8f * Mathf.Sin(t * Mathf.PI);
                foreach (var piece in turning)
                {
                    if (piece.Restored != null) piece.Restored.alpha = turn;
                    if (piece.Worn != null) piece.Worn.alpha = 1 - turn;
                    if (piece.Flash != null) piece.Flash.color = new Color(1, .85f, .55f, flare);
                }
                foreach (var d in opening)
                {
                    if (d.Closed != null) d.Closed.alpha = turn; if (d.Locked != null) d.Locked.alpha = 1 - turn;
                    if (d.PlateClean != null) d.PlateClean.alpha = turn; if (d.PlateLocked != null) d.PlateLocked.alpha = 1 - turn;
                    if (d.Flash != null) d.Flash.color = new Color(1, .85f, .55f, flare); d.Glow.color = new Color(1, .82f, .5f, .4f * turn);
                }
                if (k.GrimeImage != null) k.GrimeImage.color = new Color(1, 1, 1, Mathf.Lerp(k.Grime[a], k.Grime[b], t));
                if (k.VeilImage != null) k.VeilImage.color = new Color(k.VeilTint.r, k.VeilTint.g, k.VeilTint.b, Mathf.Lerp(k.Veil[a], k.Veil[b], t));
                if (k.OwnsLight) k.LightImage.color = new Color(1, 1, 1, Mathf.Lerp(k.Light[a], k.Light[b], t));
            });
            SetKit(k, to); k.Fade = null; Publish();
        }
        // The Keeper reaches an unlocked door: the two leaves swing toward their outer edges, the open door and its light come up. About half a second.
        IEnumerator OpenDoor(RoomKit k, string id)
        {
            var d = k?.Doors.FirstOrDefault(x => x.Id == id); if (d == null || d.LeafL == null || !d.Shown) yield break;
            LastDoorOpened = id;
            if (ReducedMotion) { d.Closed.alpha = 0; if (d.Open != null) d.Open.alpha = 1; yield break; }
            yield return Tween(.5f, t => {
                float s = Mathf.Lerp(1, .12f, Mathf.SmoothStep(0, 1, t));
                d.LeafL.localScale = new Vector3(s, 1, 1); d.LeafR.localScale = new Vector3(s, 1, 1);
                if (d.Open != null) d.Open.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * 1.5f - .4f));
                d.Glow.color = new Color(1, .82f, .5f, .4f + .5f * t);
            });
            d.Closed.alpha = 0;
        }
        public string LastDoorOpened { get; private set; } = "";
        // A Wing piece restores on its Key, or, for an instrument a lesson uses, the moment that instrument wakes (the shelf's book, the table).
        bool KitRestoredNow(KitPlacement p) => p.Wake == "wheel" ? Flow.WheelComplete : p.Wake == "modalities" ? Flow.ModalitiesComplete : Flow.Keys >= p.Key;
        // ---- Build N: the Atrium kit. Pieces restore by the Atrium's stage (the Key here is the stage, 2 to 6); the light keeps following the stage too.
        static readonly KitPlacement[] AtriumKit =
        {
            new KitPlacement("rug", 0, 800, 4, 1, 1, 60),
            new KitPlacement("chandelier", -36, 150, 6),
            new KitPlacement("chart", -140, 125, 3), new KitPlacement("chart", 138, 122, 3), new KitPlacement("chart", -86, 215, 3, .65f), new KitPlacement("chart", 88, 220, 3, .6f), new KitPlacement("chart", 165, 250, 3, 1.6f),
            new KitPlacement("lamp", -94, 127, 6), new KitPlacement("lamp", 91, 127, 5),
            new KitPlacement("banner", -124, 205, 5), new KitPlacement("banner", 124, 205, 5), new KitPlacement("banner", -61, 295, 5, 1.2f), new KitPlacement("banner", 61, 295, 5, 1.2f),
            new KitPlacement("lamp", -60, 220, 2, .875f), new KitPlacement("lamp", 60, 220, 3, .875f), // the pillar lanterns use the wall lamp (the art lane's "lantern" came back as a ring chandelier)
            new KitPlacement("shelf", -175, 380, 4),
            new KitPlacement("bust", -63, 380, 4), new KitPlacement("bust", 64, 380, 4),
            new KitPlacement("plant", -83, 380, 5), new KitPlacement("plant", 84, 380, 5),
            new KitPlacement("candlestand", -156, 385, 2), new KitPlacement("candlestand", 169, 385, 2),
            new KitPlacement("bench", 147, 405, 3),
            new KitPlacement("desk", -107, 445, 2),
            new KitPlacement("plant", -164, 495, 5, 1.3f), new KitPlacement("plant", 160, 485, 5, 1.8f),
        };
        public static readonly float[] AtriumGrime = { 1, .8f, .6f, .4f, .2f, 0 }, AtriumVeil = { .62f, .46f, .32f, .2f, .09f, 0 }; // by Atrium stage 1 to 6 (tuning variables)
        RoomKit BuildAtriumKit(RectTransform panel, SliceScreen screen)
        {
            var k = BuildKit(panel, "akit-", AtriumKit, "atrium-grime", null, AtriumGrime, AtriumVeil, null, () => Mathf.Clamp(screen == SliceScreen.Atrium ? 0 : Flow.AtriumStage - 1, 0, 5), p => (screen == SliceScreen.Atrium ? 1 : Flow.AtriumStage) >= p.Key);
            // The Zodiac Wing's door is open to the Keeper from the start; the Chamber's unlocks with the first Key (the return); the sealed door stays sealed.
            k.VeilTint = new Color(.02f, .035f, .09f); // the Atrium shell carries warm lantern light; asleep, a cold blue night sits over it
            k.DoorUnlocked = id => id == "wing-door" || (id == "chamber-door" && screen != SliceScreen.Atrium);
            AddDoor(k, "sealed-left", null, -117.5f, 385, 60, 130, 264, 74); // plates ~30% larger for phone reading (owner, Sept 25)
            AddDoor(k, "wing-door", "THE ZODIAC\nWING", 0, 385, 70, 135, 258, 84);
            AddDoor(k, "chamber-door", "THE CRYSTAL\nBOOK CHAMBER", 117.5f, 385, 60, 130, 266, 86);
            FinishKit(panel, k); atriumKits.Add(k); return k;
        }
        // ---- Build O: the Chamber kit. It restores by Keys spent (the locks filled, 0 to 4); the Books stand on the altar, sealed or open.
        static readonly KitPlacement[] ChamberKit =
        {
            new KitPlacement("banner", -56, 175, 2), new KitPlacement("banner", 43, 175, 2),
            new KitPlacement("mechanism", 0, 235, 1),
            new KitPlacement("brazier", -108, 405, 3), new KitPlacement("brazier", 108, 405, 3),
            new KitPlacement("crystal", 140, 398, 4), new KitPlacement("reliquary", 163, 404, 4),
            new KitPlacement("candle", -120, 388, 1), new KitPlacement("candle", -90, 388, 1), new KitPlacement("candle", -60, 388, 1), new KitPlacement("candle", -30, 388, 1), new KitPlacement("candle", 0, 388, 1),
            new KitPlacement("candle", 30, 388, 1), new KitPlacement("candle", 60, 388, 1), new KitPlacement("candle", 90, 388, 1), new KitPlacement("candle", 120, 388, 1),
        };
        public static readonly float[] ChamberGrime = { 1, .75f, .5f, .25f, 0 }, ChamberVeil = { .6f, .42f, .26f, .12f, 0 };
        RoomKit chamberKit;
        bool ChamberKitted => Slots.Image("ckit-book-restored") != null;
        bool AtriumKitted => Slots.Image("akit-door-closed") != null || Slots.Image("akit-desk-restored") != null;
        // With the kit in, the greybox props and their grey labels go; their tap areas stay.
        void RetireProps(RectTransform block, bool keepTap)
        {
            if (block == null) return;
            if (!keepTap) { block.gameObject.SetActive(false); return; }
            var image = block.GetComponent<Image>(); if (image != null) { image.sprite = null; image.color = new Color(0, 0, 0, 0); }
            foreach (Transform child in block) child.gameObject.SetActive(false);
        }
        void PulseDoors()
        {
            foreach (var k in atriumKits)
            {
                if (k.Fade != null || !k.Container.gameObject.activeInHierarchy) continue;
                foreach (var d in k.Doors) if (d.Shown && (d.Closed == null || d.Closed.alpha > .99f)) d.Glow.color = new Color(1, .82f, .5f, ReducedMotion ? .4f : .28f + .24f * Mathf.PingPong(Time.unscaledTime / 1.1f, 1f));
            }
        }
        const float GridKeyTop = 230;
        // Build I: glows are soft discs, not flat squares (a radial falloff made once at startup, no file).
        static Sprite softGlow;
        static Sprite SoftGlow()
        {
            if (softGlow != null) return softGlow;
            const int size = 64; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2 - 1, dy = (y + .5f) / size * 2 - 1, d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(1, 0, d)));
            }
            texture.Apply(); softGlow = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f)); return softGlow;
        }
        // Build L: over an object painted into the room a filled glow would wash it out, so the waiting glow is a halo: bright at the rim, clear inside.
        static Sprite softRing;
        static Sprite SoftRing()
        {
            if (softRing != null) return softRing;
            const int size = 128; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2 - 1, dy = (y + .5f) / size * 2 - 1, d = Mathf.Sqrt(dx * dx + dy * dy);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - Mathf.Abs(d - .78f) / .2f)));
            }
            texture.Apply(); softRing = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f)); return softRing;
        }
        // Build I (86bc1brxd): one rule for the Wing room, "an instrument with a unit waiting glows": the symbols' second half,
        // the modalities once Key 2 is in hand, and the last pattern once the table's Key is.
        bool DialUnitWaiting => Flow.WheelComplete && (Dial.Lesson.Phase == LessonPhase.GlyphWheel
            || (Dial.Lesson.CanBeginModalities && !Dial.Lesson.ModalitiesComplete)
            || (Flow.Keys >= 3 && !Dial.Lesson.Key4Earned));
        public int LastCeremony { get; private set; }
        IEnumerator KeyCeremony(int n)
        {
            busy = true; LastCeremony = n; Publish();
            bool onDial = n != 3 && Dial.UiCanvas.gameObject.activeInHierarchy;
            bool onGrid = n == 3 && gridScreen.gameObject.activeInHierarchy;
            if (onDial || onGrid)
            {
                var key = onDial ? keyRect : gridKey; var glow = onDial ? keyGlow : gridKeyGlow; var label = onDial ? keyLabel : gridKeyLabel;
                float top = onDial ? 270 : GridKeyTop; var keyImage = key.GetComponent<Image>();
                key.gameObject.SetActive(true); key.SetAsLastSibling();
                if (onDial) yield return Tween(ReducedMotion ? 0 : .25f, k => { seam.color = new Color(Bone.r, Bone.g, Bone.b, k); Dial.Ring.localScale = Vector3.one * (1 + .03f * k); });
                Sound.Play("key");
                yield return Tween(ReducedMotion ? 0 : .8f, k => {
                    key.anchoredPosition = new Vector2(0, -top + 100 * k);
                    Slots.Paint(keyImage, new Color(Bone.r, Bone.g, Bone.b, k), 1f); label.color = new Color(Charcoal.r, Charcoal.g, Charcoal.b, k);
                    glow.rectTransform.anchoredPosition = key.anchoredPosition; glow.color = new Color(Bone.r, Bone.g, Bone.b, .35f * k);
                });
                keyIndicator.text = "Keeper Keys: " + n; gridKeys.text = "Keeper Keys: " + n;
                yield return new WaitForSecondsRealtime(ReducedMotion ? .2f : .5f);
                yield return Tween(ReducedMotion ? 0 : .4f, k => {
                    Slots.Paint(keyImage, new Color(Bone.r, Bone.g, Bone.b, 1 - k), 1f); label.color = new Color(Charcoal.r, Charcoal.g, Charcoal.b, 1 - k);
                    glow.color = new Color(Bone.r, Bone.g, Bone.b, .35f * (1 - k));
                    if (onDial) { seam.color = new Color(Bone.r, Bone.g, Bone.b, 1 - k); Dial.Ring.localScale = Vector3.one * (1 + .03f * (1 - k)); }
                });
                key.gameObject.SetActive(false); if (onDial) Dial.Ring.localScale = Vector3.one;
            }
            keyIndicator.text = "Keeper Keys: " + n;
            busy = false; Show(); Publish();
        }
        IEnumerator Chandelier()
        {
            busy = true; ShowPage(); insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); Publish();
            float hold = ReducedMotion ? 1.2f : 1f;
            Slots.Paint(locks[0], Bone, 1f); chamberText.text = "Something inside the crystal moves.";
            yield return new WaitForSecondsRealtime(hold * 1.6f);
            if (!ReducedMotion)
            {
                Candles(true); yield return new WaitForSecondsRealtime(.1f); Candles(false); yield return new WaitForSecondsRealtime(.18f);
                Candles(true); yield return new WaitForSecondsRealtime(.1f); Candles(false); yield return new WaitForSecondsRealtime(.25f);
                for (int i = 0; i < candles.Length; i++) { Slots.Paint(candles[i], Bone, 1f); yield return new WaitForSecondsRealtime(.09f); }
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
        void Candles(bool on) { foreach (var c in candles) Slots.Paint(c, on ? Bone : LampDark, on ? 1f : DarkArt); chandelierLabel.text = on ? "The chandelier, lit" : "The chandelier, dark"; }
        IEnumerator Tween(float seconds, Action<float> apply)
        {
            if (seconds <= 0) { apply(1); yield break; }
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < seconds) { apply(Mathf.Clamp01((Time.unscaledTime - start) / seconds)); yield return null; }
            apply(1);
        }
        IEnumerator Fade(Image image, float from, float to, float seconds) => Tween(seconds, k => image.color = new Color(1, 1, 1, Mathf.Lerp(from, to, k)));

        // ---- Build E: the style page (?style). Every slot at once, with its source, so a set can be judged before it goes in. ----
        void BuildStyle()
        {
            style = ScreenPanel("Style"); styleSet = Slots.Set;
            styleSlots = Slots.Art.Select(a => a.Name + ": " + Slots.Source(a.Name)).ToArray(); styleSounds = Slots.Sounds.Select(s => s.Name + ": " + Slots.SoundSource(s.Name)).ToArray();
            Label(style, "STYLE PAGE", 0, 22, 340, 24, 16);
            var note = Label(style, "Every art and sound slot with its source. Set: " + (Slots.Set == "" ? "the Art and Audio folders" : Slots.Set), 0, 44, 344, 18, 10); note.color = Muted;
            for (int i = 0; i < Slots.Art.Length; i++)
            {
                var slot = Slots.Art[i]; float x = -136 + (i % 5) * 68, top = 62 + (i / 5) * 78; // five per row: 33 slots (Build H) sit above the sound rows
                var cell = Rect("Slot " + slot.Name, style, x, top + 24, 62, 44); var thumb = cell.gameObject.AddComponent<Image>(); thumb.color = PanelColor; thumb.raycastTarget = false;
                if (Slots.Dress(thumb, slot.Name)) thumb.preserveAspect = true; // the sheet keeps the file's shape; in the game the placeholder's rect wins
                Label(style, slot.Name, x, top + 52, 68, 12, 8);
                var source = Label(style, slot.Width + " × " + slot.Height + " · " + Slots.Source(slot.Name), x, top + 63, 68, 12, 7); source.color = Muted;
            }
            for (int i = 0; i < Slots.Sounds.Length; i++)
            {
                string name = Slots.Sounds[i].Name; float x = -129 + (i % 4) * 86, top = 686 + (i / 4) * 42;
                var button = MakeButton(style, name, x, top, 82, 38, () => { Sound.Play(name); Publish(); }); var text = button.GetComponentInChildren<Text>(); text.fontSize = 10; text.text = name + "\n" + Slots.SoundSource(name);
            }
            styleMute = TestButton(style, MuteLabel, 0, 774, ToggleMute);
        }
        void ShowStyle()
        {
            styleShown = true; Dial.Inert = true;
            foreach (var screen in new[] { identity, birth, atrium, atriumReturn, chamber, hub, review, glyphs, gridScreen, wingRoom, avatar }) screen.gameObject.SetActive(false);
            Dial.UiCanvas.gameObject.SetActive(false); style.gameObject.SetActive(true);
        }
        void FillStyle(DialView.WebState state)
        {
            state.screen = "style"; state.style = true; state.busy = false;
            state.active = false; state.canContinue = false; state.canOptional = false; state.canAsk = false; state.canBuilderName = false; state.canBuilderShare = false;
            state.styleSlots = styleSlots; state.styleSounds = styleSounds; state.artSet = styleSet; // the page as built, not a live lookup
            state.caspar = "Style page, " + (styleSet == "" ? "the Art and Audio folders" : "the " + styleSet + " set") + ": " + styleSlots.Count(t => !t.EndsWith(": placeholder")) + " of " + Slots.Art.Length + " art slots and " + styleSounds.Count(t => !t.EndsWith(": silent")) + " of " + Slots.Sounds.Length + " sound slots have files.";
        }

        // ---- placeholder geometry helpers (same reference layout as the Dial: 360 x 800, top-anchored) ----
        RectTransform ScreenPanel(string name, string slot = null) { var s = Rect(name, root, 0, 400, 360, 800); var image = s.gameObject.AddComponent<Image>(); image.color = Charcoal; if (slot != null) Slots.Dress(image, slot); return s; } // Build E: a room's background is a slot
        // Build K: a narrow door keeps a finger-sized target; an invisible child catches the tap and it bubbles to the door's Button.
        void HitArea(RectTransform door, float width) { var hit = Rect("Hit area", door, 0, door.sizeDelta.y / 2, Mathf.Max(width, door.sizeDelta.x), door.sizeDelta.y); var image = hit.gameObject.AddComponent<Image>(); image.color = new Color(0, 0, 0, 0); image.raycastTarget = true; hit.SetAsFirstSibling(); }
        RectTransform Block(Transform parent, string name, float x, float top, float width, float height, string slot = null)
        {
            var r = Rect(name, parent, x, top, width, height); var image = r.gameObject.AddComponent<Image>(); image.color = PanelColor; image.raycastTarget = false; if (slot != null) Slots.Dress(image, slot);
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
