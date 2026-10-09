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
        Image atriumPose, returnPose; // Build P: Caspar behind the chat box on the story screens, one pose per page
        Image chamberPose; string chamberPoseName = ""; // Build R: Caspar behind the Chamber's box on its story beats only
        Text hubText, hubNote, hubCaption, endCard, reviewProgress, reviewQuestion, reviewNote, reviewSummary, reviewGlyph;
        Text glyphCard, glyphProgress, glyphCaspar, glyphNote, glyphQuestion;
        // The Book comes alive (owner, Oct 7-8; task 86bcf0x71): the symbol rises from the open Book, and once it is answered it becomes its element's
        // emblem, then sinks into the pages as ink; the pages keep every symbol learned. Nothing shows an element before the answer.
        Image glyphEmblem; RectTransform bookCardRect, glyphPanel; readonly Text[] bookInk = new Text[12]; readonly Vector2[] glyphPlateHome = new Vector2[4];
        int revealSeat = -1, revealSlot = -1, shownGlyph = -2; bool revealOwn, bookPagesShown, glyphRising; string[] revealNames = new string[4];
        public const float EmblemY = 186, EmblemSize = 185, CardY = 200, RiseFrom = 372, RiseSeconds = .45f, BookPagesSeconds = 2.2f;
        public static readonly Color DarkPlate = new Color(.34f, .32f, .32f), BookInk = new Color(.23f, .14f, .09f);
        public const float InkFull = .78f, InkFaint = .3f; public const int InkSize = 30;
        // twelve places on the open pages, six to a page; a sign's place is not its wheel order (owner, Oct 7, answer 2A). Measured from book-room's parchment (Oct 8):
        // each symbol's laid-out box lies wholly on the parchment (the web suite holds it there against the art), 24 px or more off the gutter, and places on a page sit 32 px apart or more (left page 0-5, right 6-11)
        public static readonly Vector2[] InkSpots = { new Vector2(-70, 352), new Vector2(-110, 386), new Vector2(-38, 326), new Vector2(-40, 378), new Vector2(-90, 320), new Vector2(-102, 354),
            new Vector2(68, 352), new Vector2(108, 386), new Vector2(40, 374), new Vector2(62, 312), new Vector2(32, 338), new Vector2(94, 328) };
        public static readonly int[] InkPlace = { 7, 2, 10, 4, 0, 11, 5, 8, 1, 9, 3, 6 };
        static Color Clear(Color c) => new Color(c.r, c.g, c.b, 0);
        public static string EmblemSlot(int seat) => "emblem-" + Zodiac.Seats[Zodiac.Wrap(seat)].Name.ToLowerInvariant();
        // a symbol's ink: 2 learned on the player's own (Practicing in the deck), 1 named with Caspar's help, 0 not yet (owner, Oct 7, answer 1A)
        public int InkOf(int seat) => !Dial.Lesson.GlyphNamed[seat] ? 0 : Flow.Deck.Item(seat, ItemKind.Glyph).State == ItemState.Practicing ? 2 : 1;
        // where a symbol's ink really lands, in layout px (x from the centre, y down from the top): its laid-out glyph, so the web check can hold it to the parchment
        static float[] InkBox(Text ink)
        {
            var verts = ink != null ? ink.cachedTextGenerator.verts : null; if (verts == null || verts.Count < 4) return new float[4];
            var r = ink.rectTransform; float unit = 1 / ink.pixelsPerUnit, x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var v in verts) { float x = r.anchoredPosition.x + v.position.x * unit, y = -(r.anchoredPosition.y + v.position.y * unit); x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y); }
            return new[] { x0, y0, x1, y1 };
        }
        // v0.4 tap-to-move (Q06 phase 2): two walkable rooms, a placeholder marker, fades at doorways.
        RectTransform wingRoom, avatar, avatarHead; Image fadeImage; Text wingRoomCaption, walkSpeedLabel;
        Button enterDial, enterShelf, wingRoomBack, walkSpeed, closeBook; Image shelfGlow;
        CanvasGroup shelfLight; float shelfLightLevel; // Build Y: the shelf's own glow, 0 to 1
        string shelfEdge = ""; // APK Session 2, bug 1 (86bcbn6ct): "halo" once the shelf's edge is drawn from its silhouette
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
        // The Table comes alive (owner, Oct 7-8; task 86bcf0x71): wells carved in the table's top, wooden plates in a tray, dragged or tapped into a
        // well; Seal still commits. A correct Seal on the player's own turns the plate gold and its lettering takes its element's material; a sign
        // placed with Caspar's help stays wood with its lettering alive (answer 1A). Nothing shows an element before the Seal.
        readonly Image[] gridWellLight = new Image[12], gridCellWood = new Image[12], gridCellGold = new Image[12]; readonly RectTransform[] gridGoldMask = new RectTransform[12];
        readonly Material[] gridCellGlyphLive = new Material[12], gridCellNameLive = new Material[12]; readonly Vector2[] gridTileHome = new Vector2[12];
        int goldSeat = -1, dragSeat = -1, hoverCell = -1; bool gridPlates;
        public const float GoldSeconds = .5f, LiftScale = 1.06f;
        public static readonly Color BurnedInk = new Color(.23f, .14f, .09f);
        // a live name's colour: its element's (Build S's hues, darkened), deep enough to read: 4.5:1 or better on the gold plate, and on the darker
        // wood as legible as the burned-in lettering beside it (about 2.5:1; even black reaches only 3.6:1 there), measured from the plate art
        public static Color DeepElement(int seat) { switch (Zodiac.Seats[Zodiac.Wrap(seat)].Element) { case "Fire": return new Color(.62f, .16f, .07f); case "Earth": return new Color(.19f, .36f, .12f); case "Air": return new Color(.42f, .30f, .08f); default: return new Color(.08f, .24f, .48f); } }
        public const float LiveOnGold = .8f, LiveOnWood = .5f;
        public static Color LiveName(int seat, bool gold) { var c = DeepElement(seat) * (gold ? LiveOnGold : LiveOnWood); c.a = 1; return c; }
        public static string LetterSlot(int seat) => "letter-" + Zodiac.Seats[Zodiac.Wrap(seat)].Element.ToLowerInvariant();
        public static Vector2 LetterDrift(int seat) { switch (Zodiac.Seats[Zodiac.Wrap(seat)].Element) { case "Fire": return new Vector2(0, .25f); case "Water": return new Vector2(.18f, 0); case "Air": return new Vector2(.12f, .06f); default: return Vector2.zero; } }
        // a placed sign is the player's own when the deck holds it as practising (Level 0/1 at the Seal), as the journal reads it
        public bool PlacedOwn(int seat) => Grid.Placed[seat] && Flow.Deck.Item(seat, ItemKind.Grid).State == ItemState.Practicing;
        const float KeeperScale = 100f / 44f; // Build L (owner, Sept 24): the Keeper stands 100 px tall, the Wing mockup's scale; his feet stay on the band
        const float BandY = 436f, FadeSeconds = .35f; // floor band and fade length are test variables (Q06 phase 2, decision 7)
        readonly Button[] glyphNameButtons = new Button[4];
        readonly Button[] reviewGlyphButtons = new Button[4];
        InputField nameField, dateField, timeField, placeField;
        // batch 2 (owner, Oct 2 evening: the Big Three approved): the chart path's time and place steps, the known path's sign label and "I don't know"
        RectTransform birthTimeEntry, birthPlaceEntry; Text signsLabel, birthHeading; Button timeUnknown, placeUnknown, birthCancel; readonly Button[] placeMatchButtons = new Button[4]; List<Place> placeMatches = new List<Place>();
        // the cusp day (owner ruling, Oct 2 evening): the question, its two signs, and "Why?" with its reason (Oct 7: no "I'm not sure")
        RectTransform birthCusp; Text cuspQuestion, cuspWhy; Button cuspWhyLink; readonly Button[] cuspButtons = new Button[2]; bool cuspWhyOpen;
        RectTransform birthMoon; Text moonQuestion; readonly Button[] moonButtons = new Button[3]; // Oct 7: the Moon's options (two, rarely three), where the cusp question sits
        static string cuspDayFor; // DEV Mode's cusp-day sample: the name to carry into the opening's question across the reload
        static string birthSampleFor, birthSampleId; // Oct 7: the same for each other path's sample
        Button leaveDial; // Build T
        Button birthContinue, wingContinue, insert, chamberContinue, atriumContinue, returnContinue, changeChoice;
        Button enterWing, hubRestart, leavePractice;
        // Build F: the fork and the practice exit on the Dial, the journal's buttons and screen.
        Button forkLesson, forkPractice, leavePracticeDial, journalHub, journalWing, journalChamber, journalPrev, journalNext, journalClose;
        RectTransform journal; Text journalTitle; Image journalPage;
        // Build J / AA: a sign's picture (Illumination) and its ribbon; the rest of the book's parts are declared with BuildJournal.
        Image journalSignArt, journalRibbon; Material journalIllumination;
        readonly Image[][] journalChevrons = new Image[2][]; // the page arrows, drawn (the fonts carry no arrow glyphs)
        bool forkShown, gating;
        // Build H (the Sept 17 lighting decision, Option C): one golden-hour overlay per room, over the background and under everything else, faded by the Atrium stage.
        readonly List<Image> lightOverlays = new List<Image>(); float lightAlpha; Coroutine lightFade;
        public float LightAlpha => lightAlpha; // fixture evidence
        public static readonly string[] LightSlots = { "atrium-light", "wing-light", "chamber-light" };
        public static float LightAlphaFor(int stage, int locks) => stage <= 1 ? 0f : stage == 2 ? .25f : Mathf.Lerp(.5f, 1f, locks / (float)SliceFlow.LocksTotal); // nothing at Stage 1, half once the wheel lights, full only at the 21st Key (owner, Sept 27)
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
        // Build J: ink on the journal's vellum (the Art Bible's line colour, crimson, amber); the greybox panel keeps bone and muted.
        static readonly Color JournalInk = new Color(.17f,.11f,.086f), JournalFaint = new Color(.17f,.11f,.086f,.6f), Rubric = new Color(.49f,.16f,.2f), Gilt = new Color(.89f,.64f,.29f);
        static readonly string[] Elements = { "Fire", "Earth", "Air", "Water" };
        public static readonly string[] AtriumPages = {
            "You are awake. Good. I hope the trip was not too rough. You were... let us say 'unavailable' for most of it.",
            "I am Caspar. I have lived here for centuries, keeping what remains of what your ancestor built. It has been a long time since one of his blood stepped through these halls.\nThank you for coming. Truly.",
            "I will not waste your time with a long tour of empty rooms. I must be honest with you. I have done what I can, but I am only a caretaker. The Library does not answer to me. It answers to a Keeper.",
            "The rooms are sealed. The lights have gone out. I could not stop it.\nBut you carry his blood, and that changes things. I need your help to wake this place. Come, I will show you where to begin." };
        public static readonly string[] ReturnPages = {
            "A Keeper Key.\nI spent centuries wondering if another one would ever surface. Now here it is, in your hands.",
            "Follow me. There is a door that has been locked since your ancestor left. Let us find out if it too will respond to you." };
        // Build P (note 3; the owner's B2 ruling and the first-poses working choice, Sept 28): Caspar's pose on each story page, a caspar-<pose> slot.
        public static readonly string[] CasparPoses = { "calm", "explain", "warm", "wry", "moved", "solemn" };
        public static readonly string[] AtriumPoses = { "wry", "warm", "solemn", "explain" };
        public static readonly string[] ReturnPoses = { "moved", "explain" };
        // Build R (the owner handed Claude the remaining poses, Sept 28): the Chamber's introduction, then his two spoken lines after the first Key.
        public static readonly string[] ChamberPoses = { "explain", "solemn", "calm" };
        public const string ChamberBreathesPose = "moved", ChamberContinuePose = "warm";
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
        // Build T (owner, APK playtest, Sept 29): the opening's walk. Caspar sends the player to the Zodiac Wing; in the Wing he points to the Dial.
        public const string HubOpeningLine = "Our work begins in the Zodiac Wing. That door there. Go on, it will open for you."; // owner (Sept 29)
        public const string WingOpeningLine = "The Zodiac Wing. Mind the dust. The Dial is waiting for you; tap it when you are ready."; // owner (Sept 29)
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
        public SettingsMenu Settings { get; private set; } // Build U
        public TravelMenu MiniMenu { get; private set; } // the room mini-menu (owner, Oct 1; 86bca07wv)

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
            atrium = BuildAtrium("Atrium", out atriumText, out atriumContinue, out atriumPose);
            atriumReturn = BuildAtrium("Atrium return", out returnText, out returnContinue, out returnPose);
            BuildChamber(); BuildHub(); BuildReview(); BuildGlyphs(); BuildGrid(); BuildWingExtras(); BuildWingRoom(); BuildJournal(); BuildAvatar(); BuildTravel(); BuildFade();
            var flashObject = new GameObject("White light", typeof(RectTransform), typeof(Canvas));
            flashObject.transform.SetParent(transform, false);
            flashCanvas = flashObject.GetComponent<Canvas>(); flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay; flashCanvas.sortingOrder = 10;
            flash = flashObject.AddComponent<Image>(); flash.color = new Color(1, 1, 1, 0); flash.raycastTarget = false;
            // Build U (owner, APK playtest, Sept 29): Settings, from a gear at the top right of every screen.
            Settings = gameObject.AddComponent<SettingsMenu>();
            Settings.Muted = () => Sound.Muted; Settings.Reduced = () => ReducedMotion; Settings.WalkSpeed = () => Flow.Walk.SpeedName;
            Settings.WakeLabel = () => WakeLabel; Settings.CycleWake = CycleWake; // the Dial's wake-up preview
            Settings.ToggleSound = ToggleMute; Settings.ToggleMotion = () => Dial.WebAction("motion"); Settings.CycleWalk = CycleWalkSpeed; Settings.StartOver = Restart; Settings.Changed = Publish; Settings.Jump = JumpTo; Settings.CuspDay = CuspDay; Settings.BirthOpening = BirthOpening;
            Settings.Build(font);
            SafeArea.FitAndroidText(transform, font); // Platform fit, Part 1: on Android a line that no longer fits its box shrinks up to two points
            SafeArea.Moved = Publish; // Part 2: the gear and the mini-menu button keep the web page's boxes on them
            if (Slots.StyleRequested) Settings.GearShown = false; // the style page is a test page, not the game
            if (Slots.StyleRequested) { BuildStyle(); ShowStyle(); Publish(); return; } // Build E: the style page instead of the game; the save is not touched
            TryRestore();
            if (cuspDayFor != null) OpenCuspDay(); // DEV Mode's cusp-day sample
            if (birthSampleFor != null) OpenBirthOpening(); // Oct 7: DEV Mode's sample for each other path
            Show(); Publish();
        }
        void Update()
        {
            if (canvas.pixelRect.width >= 1) canvas.scaleFactor = Mathf.Min(canvas.pixelRect.width / 360f, canvas.pixelRect.height / 800f);
            if (styleShown) return;
            if (avatar != null && avatar.gameObject.activeInHierarchy) PlaceAvatar();
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.KeyEarned && !revealStarted && !Dial.Busy) StartCoroutine(Reveal());
            if (!Dial.Showing) Dial.WakeStep = WakeTarget; // the wake-up: a step lands the next time the player comes to the Dial, never mid-lesson
            WakeKitDial();
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.Phase == LessonPhase.AllLit && !Flow.WheelComplete) { Flow.MarkWheelComplete(); Save(); LightWing(); Show(); Publish(); }
            if ((Flow.Screen == SliceScreen.Wing || Flow.Screen == SliceScreen.Practice) && Dial.Lesson.Key2Earned && Flow.Keys < 2) { Flow.MarkKey2(); Save(); StartCoroutine(KeyCeremony(2)); }
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.ModalitiesComplete && !Flow.ModalitiesComplete) { Flow.MarkModalitiesComplete(); Save(); Publish(); } // Build B: the table wakes
            if (Flow.Screen == SliceScreen.Grid && Grid.Key3Earned && Flow.Keys < 3 && !busy) { Flow.MarkKey3(); Save(); StartCoroutine(KeyCeremony(3)); } // a fallback (a restored save); the seating runs the ceremony itself
            if (Flow.Screen == SliceScreen.Wing && Dial.Lesson.Key4Earned && Flow.Keys < 4) { Flow.MarkKey4(); Save(); StartCoroutine(KeyCeremony(4)); } // Build C; Build I: every Key rises
            if (insertGlow != null && insert.gameObject.activeInHierarchy && insert.interactable && (!Flow.KeyInserted || Flow.CanSpend))
            {
                float a = ReducedMotion ? .35f : .15f + .3f * Mathf.PingPong(Time.unscaledTime / 1.2f, 1f);
                insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, a);
            }
            if (Flow.Screen == SliceScreen.WingRoom && DialUnitWaiting && !ReducedMotion) dialGlow.color = new Color(.95f, .8f, .5f, .35f + .35f * Mathf.PingPong(Time.unscaledTime / 1.4f, 1f)); // Build I: reduced motion holds it still
            if (shelfLight != null && Flow.Screen == SliceScreen.WingRoom && shelfLightLevel >= 1 && !ReducedMotion) shelfLight.alpha = .6f + .4f * Mathf.PingPong(Time.unscaledTime / 1.4f, 1f); // Build Y: the waiting shelf breathes, like the Dial; reduced motion holds it still
            PulseDoors(); // Build N: unlocked doors breathe light at their edges
            if (Flow.Gated && !busy && !Dial.Busy && !gating) StartCoroutine(Gate()); // Build F: the third strike closes the instrument once the answer's beat has settled
            if (leaveDial != null)
            {
                bool leave = Flow.Screen == SliceScreen.Wing && (Flow.AtriumStage >= 2 || !Flow.KeyRevealed), can = leave && !busy && !Dial.Busy; // Build T: at all times; Key 1's Continue carries the opening on
                if (leaveDial.gameObject.activeSelf != leave || leaveDial.interactable != can) { leaveDial.gameObject.SetActive(leave); leaveDial.interactable = can; Publish(); }
            }
            if (wingContinue != null && Flow.AtriumStage >= 2)
            {
                // From Stage 2 the slice's Continue is no longer the Dial's exit (Build T moved Leave the Dial to its own button); it stays off.
                bool idle = Flow.Screen == SliceScreen.Wing && !Dial.Lesson.Dial.Active && !Dial.Busy && !busy && Dial.Lesson.Phase != LessonPhase.Review && !Dial.ControlsShown; // Build C: nor over the beat's Continue or the builder's buttons
                if (wingContinue.gameObject.activeSelf) { wingContinue.gameObject.SetActive(false); Publish(); }
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
        // the place step (Oct 7): the matches close up 2 px and start 10 px lower for the nearest-city line; "I don't know where" sits under them
        public const float PlaceMatchTop = 132f, PlaceMatchStep = 44f, PlaceUnknownY = 310f;
        void BuildBirth()
        {
            birth = ScreenPanel("Birth");
            birthHeading = Label(birth, SliceFlow.BirthQuestion, 0, 200, 330, 30, 18); // "Your Birth" when it is opened from the journal (Oct 7)
            birthChoices = Rect("Choices", birth, 0, 400, 360, 220);
            // the birth-time build (owner, Oct 7): two answers, at the retired three's size and spacing; "Enter what I already know" and "I don't know" (the random sun) are retired
            string[] labels = { SliceFlow.KnowBirthday, SliceFlow.SkipBirthday };
            string[] choices = { "chart", "skip" };
            for (int i = 0; i < 2; i++) { string choice = choices[i]; MakeButton(birthChoices, labels[i], 0, 10 + i * 64, 300, 52, () => ChooseBirth(choice)); }
            // batch 2 (owner, Oct 2 evening: the Big Three approved as proposed): the chart path asks the date, then the time, then the place;
            // the known path, the sun, then the moon and the rising sign. The lines are Claude's drafts (owner: "fine for now")
            birthDate = Rect("Date entry", birth, 0, 400, 360, 220);
            Label(birthDate, "Your birth date: day, month, year", 0, 0, 320, 22, 14);
            dateField = TextBox(birthDate, "Date box", 0, 48, 200, 48, "DD/MM/YYYY", v => UseDate(v));
            dateHint = Label(birthDate, "Like 25/04/1990.", 0, 88, 320, 20, 12); dateHint.color = Muted;
            MakeButton(birthDate, "Use this date", 0, 130, 200, 52, () => UseDate(dateField.text));
            birthTimeEntry = Rect("Time entry", birth, 0, 400, 360, 220);
            Label(birthTimeEntry, "Your birth time", 0, 0, 320, 22, 14);
            timeField = TextBox(birthTimeEntry, "Time box", 0, 48, 200, 48, "HH:MM", v => UseTime(v));
            Label(birthTimeEntry, "Like 14:30, or 2:30 pm.", 0, 88, 320, 20, 12).color = Muted;
            MakeButton(birthTimeEntry, "Use this time", 0, 130, 200, 52, () => UseTime(timeField.text));
            timeUnknown = MakeButton(birthTimeEntry, "I don't know my birth time", 0, 192, 260, 52, () => { if (!busy && Flow.SetBirthTime(-1)) AfterBirthEntry(); });
            birthPlaceEntry = Rect("Place entry", birth, 0, 400, 360, 220);
            Label(birthPlaceEntry, "Your birth town or city", 0, 0, 320, 22, 14);
            placeField = TextBox(birthPlaceEntry, "Place box", 0, 48, 260, 48, "Town or city", v => SearchPlace(v)); placeField.onValueChanged.AddListener(SearchPlace);
            Label(birthPlaceEntry, "Type its first letters, then pick it below.", 0, 86, 320, 18, 12).color = Muted;
            Label(birthPlaceEntry, SliceFlow.NearestCity, 0, 104, 320, 18, 12).color = Muted; // Oct 7: a second grey line under the first
            for (int i = 0; i < placeMatchButtons.Length; i++) { int k = i; placeMatchButtons[i] = MakeButton(birthPlaceEntry, "", 0, PlaceMatchTop + i * PlaceMatchStep, 300, 42, () => PickPlace(k)); placeMatchButtons[i].GetComponentInChildren<Text>().fontSize = 13; }
            placeUnknown = MakeButton(birthPlaceEntry, SliceFlow.PlaceUnknown, 0, PlaceUnknownY, 260, 44, () => { if (!busy && Flow.SetBirthPlace(null)) AfterBirthEntry(); }); // under the matches, clear of Continue
            birthCusp = Rect("Cusp question", birth, 0, 400, 360, 220);
            cuspQuestion = Label(birthCusp, "", 0, 40, 320, 88, 14);
            for (int i = 0; i < cuspButtons.Length; i++) { int choice = i; cuspButtons[i] = MakeButton(birthCusp, "", 0, 110 + i * 52, 300, 44, () => PickCusp(choice)); }
            cuspWhyLink = MakeButton(birthCusp, SliceFlow.CuspWhyLink, 0, 256, 96, 24, ToggleCuspWhy); cuspWhyLink.GetComponent<Image>().color = new Color(0, 0, 0, 0); cuspWhyLink.GetComponent<Image>().canvasRenderer.cullTransparentMesh = false;
            var whyLabel = cuspWhyLink.GetComponentInChildren<Text>(); whyLabel.fontSize = 13; whyLabel.color = Muted; whyLabel.fontStyle = FontStyle.Italic; ButtonLook.HitArea(cuspWhyLink, ButtonLook.MinTarget, ButtonLook.MinTarget); // a small link, 44 px to tap
            cuspWhy = Label(birthCusp, SliceFlow.CuspWhy, 0, 296, 320, 46, 12); cuspWhy.color = Muted;
            birthMoon = Rect("Moon question", birth, 0, 400, 360, 220);
            moonQuestion = Label(birthMoon, "", 0, 40, 320, 88, 14);
            for (int i = 0; i < moonButtons.Length; i++) { int choice = i; moonButtons[i] = MakeButton(birthMoon, "", 0, 110 + i * 52, 300, 44, () => PickMoonSign(choice)); }
            signsLabel = Label(birth, "", 0, 290, 320, 22, 14);
            birthSigns = Rect("Sign choices", birth, 0, 430, 360, 240);
            for (int i = 0; i < 12; i++) { int seat = i; MakeButton(birthSigns, Zodiac.Seats[i].Name, -110 + (i % 3) * 110, 26 + (i / 3) * 58, 100, 52, () => PickBirthSign(seat)); }
            changeChoice = MakeButton(birth, "Change my answer", 0, 250, 200, 48, () => ChooseBirth("")); changeChoice.GetComponent<Image>().color = PanelColor;
            birthCancel = MakeButton(birth, SliceFlow.BackToJournal, 0, 250, 200, 48, CancelBirthAmend); birthCancel.GetComponent<Image>().color = PanelColor; // from "Your Birth": back, nothing changed
            birthNote = Label(birth, "", 0, 545, 320, 60, 14);
            birthContinue = MakeButton(birth, "Continue", 0, 654, 190, 56, () => Continue());
            ShowBirth();
        }
        RectTransform BuildAtrium(string name, out Text caspar, out Button next, out Image pose)
        {
            var screen = ScreenPanel(name, "atrium"); LightOverlay(screen, "atrium-light");
            if (AtriumKitted) BuildAtriumKit(screen, name == "Atrium return" ? SliceScreen.AtriumReturn : SliceScreen.Atrium); // Build N
            SafeArea.Top(Label(screen, "THE GRAND ATRIUM", 0, 32, 340, 24, 18));
            var sealedDoor = Block(screen, "Sealed door", -118, 310, 62, 128, "door-sealed"); // Build K: sized to the painted arch (Sept 24 Atrium)
            var shelvesBlock = Block(screen, "Shelves, mostly bare", -130, 200, 60, 120, "shelves");
            var furniture = Block(screen, "Covered furniture", 50, 422, 120, 70, "furniture-covered");
            var cloth = Rect("Dust cloth", screen, 50, 410, 128, 30); cloth.gameObject.AddComponent<Image>().color = new Color(.3f, .3f, .32f); cloth.gameObject.SetActive(!HasArt(furniture));
            var candle = Rect("Candle", screen, 115, 375, 8, 20); var candleImage = candle.gameObject.AddComponent<Image>(); Slots.Dress(candleImage, "candle"); Slots.Paint(candleImage, new Color(.5f, .42f, .3f), .7f);
            if (AtriumKitted) { foreach (var b in new[] { sealedDoor, shelvesBlock, furniture }) RetireProps(b, false); cloth.gameObject.SetActive(false); candle.gameObject.SetActive(false); }
            SafeArea.Top(Label(screen, "Dust. Covered furniture. Sealed doors. One weak candle.", 0, 92, 330, 20, 12), SafeArea.UnderTitle).color = Muted;
            // Build P (note 3; the owner's B2 ruling, Sept 28): with the chat box's art, Caspar stands unframed behind the box, one still pose
            // per page, his name on a plate at the box's top left, Continue inside the box. Without the art the old panel stays (no file, no change).
            bool chatBox = Slots.Image("chat-box") != null;
            var poseRect = Rect("Caspar pose", screen, 5, 408, 264, 468); pose = poseRect.gameObject.AddComponent<Image>(); pose.raycastTarget = false; pose.enabled = false;
            var panel = Rect("Caspar panel", screen, 0, chatBox ? 600 : 560, 324, chatBox ? 240 : 150); var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.color = new Color(.045f, .025f, .03f, .92f);
            if (chatBox)
            {
                Slots.DressChatBox(panelImage, null, font); // Build R: the shared chat box, the same frame and plate as every Caspar panel
                caspar = Label(panel, "", 0, 78, 276, 104, 14); caspar.alignment = TextAnchor.UpperLeft; caspar.resizeTextForBestFit = true; caspar.resizeTextMinSize = 10; caspar.resizeTextMaxSize = 14;
                next = MakeButton(screen, "Continue", 0, 671, 150, 44, () => Continue());
                ButtonLook.Room(next, font); // batch 2 (3c C): a room's button in the chat box's language; 3d: 44 px
                // Build X: the box fits the page; Continue rides at its bottom; the figure behind it is clipped at its bottom edge.
                var fit = panel.gameObject.AddComponent<ChatFit>(); fit.Line = caspar; fit.Next = (RectTransform)next.transform; fit.Top = 480; fit.Max = 240; fit.Clip = ClipBehind(screen, poseRect); fit.Fitted = Publish;
                if (name == "Atrium") atriumFit = fit; else returnFit = fit;
            }
            else
            {
                Label(panel, "CASPAR", 0, 16, 290, 22, 13);
                caspar = Label(panel, "", 0, 86, 306, 110, 13);
                next = MakeButton(screen, "Continue", 0, 690, 190, 56, () => Continue());
                ButtonLook.Room(next, font); // batch 2 (3c C)
            }
            StyleAtriumButton(next);
            return screen;
        }
        static void SetPose(Image pose, string name) { var sprite = Slots.Image("caspar-" + name); pose.sprite = sprite; pose.enabled = sprite != null; pose.color = Color.white; pose.preserveAspect = true; } // Build P: no file, no figure
        void ChamberPose(string name) { chamberPoseName = name ?? ""; if (name == null) chamberPose.enabled = false; else SetPose(chamberPose, name); } // Build R: null puts him away
        // Build X: a clip from the screen's top to the chat box's bottom edge (ChatFit keeps it there), with Caspar's figure moved inside it.
        static RectTransform ClipBehind(RectTransform screen, RectTransform figure)
        {
            var clip = new GameObject("Caspar clip", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>(); clip.SetParent(screen, false);
            clip.anchorMin = clip.anchorMax = clip.pivot = new Vector2(.5f, 1); clip.anchoredPosition = Vector2.zero; clip.sizeDelta = new Vector2(360, 800);
            clip.SetSiblingIndex(figure.GetSiblingIndex()); figure.SetParent(clip, false); // the same place: both hang from the screen's top centre
            return clip;
        }
        ChatFit atriumFit, returnFit, chamberFit, hubFit; // Build X
        static void ChatText(Text line) { line.alignment = TextAnchor.MiddleLeft; line.rectTransform.sizeDelta -= new Vector2(22, 0); } // Build R: left-aligned, clear of the frame
        void BuildChamber()
        {
            chamber = ScreenPanel("Chamber", "chamber"); LightOverlay(chamber, "chamber-light");
            if (ChamberKitted) { chamberKit = BuildKit(chamber, "ckit-", ChamberKit, "chamber-grime", null, ChamberGrime, ChamberVeil, null, () => Flow.KeysSpent >= SliceFlow.LocksTotal ? 4 : Flow.KeysSpent >= 14 ? 3 : Flow.KeysSpent >= 7 ? 2 : Flow.KeysSpent >= 1 ? 1 : 0, p => Flow.KeysSpent >= p.Key); chamberKit.VeilTint = new Color(.02f, .035f, .09f); FinishKit(chamber, chamberKit); atriumKits.Add(chamberKit); } // Build O
            SafeArea.Top(Label(chamber, "THE CRYSTAL BOOK CHAMBER", 0, 32, 340, 24, 18));
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
            chamberBand = Rect("Floor band", chamber, 0, ChamberBandY, 340, 30); var chamberBandImage = chamberBand.gameObject.AddComponent<Image>(); chamberBandImage.color = new Color(.16f, .16f, .19f, ChamberKitted ? 0 : 1); chamberBandImage.raycastTarget = false; chamberBand.gameObject.SetActive(false); // before the doorway, so its label draws over the band; Build O: the painted floor carries it
            chamberDoorGlow = DoorwayGlow(chamber, ChamberKitted ? -145 : -150, ChamberKitted ? 287 : 347, ChamberKitted ? 40 : 30, ChamberKitted ? 165 : 124); // the art pass (86bcex5kc 2A)
            chamberDoor = Block(chamber, "Doorway back", ChamberKitted ? -145 : -150, ChamberKitted ? 287 : 347, ChamberKitted ? 40 : 30, ChamberKitted ? 165 : 124, ChamberKitted ? null : "door-open"); if (ChamberKitted) RetireProps(chamberDoor, true); HitArea(chamberDoor, 48); Tappable(chamberDoor, () => Walk("atrium-door")); chamberDoor.gameObject.SetActive(false); // Build K: the painted doorway
            chamberBooksTap = Rect("The Books, tap to walk", chamber, 0, 300, ChamberKitted ? 236 : 330, 90); var booksTapImage = chamberBooksTap.gameObject.AddComponent<Image>(); booksTapImage.color = new Color(0, 0, 0, 0); Tappable(chamberBooksTap, () => Walk("books")); chamberBooksTap.gameObject.SetActive(false); // kitted, as wide as the painted Books (x -116 to 116), clear of the doorway at the far left
            var chamberPoseRect = Rect("Caspar pose", chamber, 5, 363, 264, 468); chamberPose = chamberPoseRect.gameObject.AddComponent<Image>(); chamberPose.raycastTarget = false; chamberPose.enabled = false; // Build R: his waist at the box's top edge (435)
            var panel = Rect("Caspar panel", chamber, 0, 520, 324, 170); var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.color = PanelColor;
            var casparLabel = Label(panel, "CASPAR", 0, 16, 290, 22, 13);
            chamberText = Label(panel, "", 0, 96, 306, 136, 13);
            if (Slots.DressChatBox(panelImage, casparLabel, font)) { ChatText(chamberText); chamberFit = panel.gameObject.AddComponent<ChatFit>(); chamberFit.Line = chamberText; chamberFit.Top = 435; chamberFit.Max = 170; chamberFit.Clip = ClipBehind(chamber, chamberPoseRect); chamberFit.Fitted = Publish; } // Build R; Build X: fitted
            chamberEnd = Label(chamber, "The first Key is spent. The Library has taken her first breath.", 0, 720, 330, 40, 12); chamberEnd.color = Muted; chamberEnd.gameObject.SetActive(false);
            var glow = Rect("Insert glow", chamber, 0, 654, 214, 80); insertGlow = glow.gameObject.AddComponent<Image>(); insertGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); insertGlow.raycastTarget = false;
            insert = MakeButton(chamber, "Insert Key", 0, 654, 190, 56, Insert); insert.GetComponent<Image>().color = Crimson; // owner (worksheet section 13)
            chamberContinue = MakeButton(chamber, "Continue", 0, 654, 190, 56, () => Continue()); chamberContinue.gameObject.SetActive(false);
            chamberBack = MakeButton(chamber, "Return to the Atrium", 0, 714, 300, 48, LeaveChamber); chamberBack.gameObject.SetActive(false); // Build D // owner (worksheet section 13)
            journalChamber = MakeButton(chamber, "Your journal", 0, 774, 216, 44, OpenJournal); journalChamber.GetComponentInChildren<Text>().fontSize = 13; journalChamber.gameObject.SetActive(false); // Build F
            ButtonLook.Room(chamberContinue, font); ButtonLook.Room(chamberBack, font); ButtonLook.Room(journalChamber, font); // batch 2 (3c C; 3d: Your journal at 44 px). Insert Key keeps its crimson (owner, worksheet section 13)
        }
        void BuildHub()
        {
            // Q05 decisions 1, 6: the Atrium in Stage 2 "Stirring" with two entrances.
            hub = ScreenPanel("Hub", "atrium"); LightOverlay(hub, "atrium-light");
            if (AtriumKitted) hubKit = BuildAtriumKit(hub, SliceScreen.Hub); // Build N
            SafeArea.Top(Label(hub, "THE GRAND ATRIUM", 0, 32, 340, 24, 18));
            var shelves = Block(hub, "Shelves, mostly bare", -130, 200, 60, 120, "shelves"); shelvesLabel = shelves.GetComponentInChildren<Text>(); // owner (worksheet section 13)
            for (int i = 0; i < 3; i++) { var book = Rect("Book", shelves, -16 + i * 16, 30 + (i % 2) * 40, 10, 26); shelfBooks[i] = book.gameObject.AddComponent<Image>(); shelfBooks[i].color = new Color(.3f, .28f, .3f); shelfBooks[i].raycastTarget = false; Slots.Dress(shelfBooks[i], "shelf-book"); book.gameObject.SetActive(false); } // Build D: the shelves take their books back at Stage 4
            var floor = Rect("Floor band", hub, 0, BandY, 340, 30); var floorImage = floor.gameObject.AddComponent<Image>(); floorImage.color = new Color(.16f, .16f, .19f, 0); floorImage.raycastTarget = false;
            var desk = Block(hub, "Desk, uncovered", -125, 426, 70, 30, "desk"); Tappable(desk, () => Walk("desk"));
            ButtonLook.HitArea(desk, 0, ButtonLook.MinTarget); // batch 2 (3d): the desk takes taps over 44 px of height; it is drawn at 30
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
                var door = Block(hub, doors[i], i == 0 ? -118 : i == 1 ? 0 : 118, 310, i == 1 ? 72 : 62, 128, i == 0 ? "door-sealed" : "door-open"); string poi = i == 0 ? "sealed-left" : i == 1 ? "wing-door" : "chamber-door"; Tappable(door, () => Walk(poi)); // Build K: each door fills its painted arch
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
            hubCaption = SafeArea.Top(Label(hub, "", 0, 92, 340, 36, 11), SafeArea.UnderTitle); hubCaption.color = Muted;
            var panel = Rect("Caspar panel", hub, 0, 536, 324, 120); var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.color = new Color(.045f, .025f, .03f, .92f);
            var casparLabel = Label(panel, "CASPAR", 0, 14, 290, 20, 13);
            hubText = Label(panel, "", 0, 70, 306, 90, 12);
            if (Slots.DressChatBox(panelImage, casparLabel, font)) { ChatText(hubText); hubFit = panel.gameObject.AddComponent<ChatFit>(); hubFit.Line = hubText; hubFit.Top = 476; hubFit.Max = 120; hubFit.Fitted = Publish; } // Build R: the box alone, the room stays in view (owner, Sept 28); Build X: fitted
            enterWing = MakeButton(hub, "The Zodiac Wing", -78, 624, 150, 52, EnterWing); StyleAtriumButton(enterWing);
            enterChamber = MakeButton(hub, "The Crystal Book Chamber", 78, 624, 150, 52, EnterChamber); StyleAtriumButton(enterChamber); enterChamber.GetComponentInChildren<Text>().fontSize = 12; // Build D; owner (worksheet section 13)
            journalHub = MakeButton(hub, "Your journal", 0, 680, 300, 52, OpenJournal); StyleAtriumButton(journalHub); journalHub.gameObject.SetActive(false); // Build F: the journal takes the row Check the Seals held (retired Sept 15)
            ButtonLook.Room(enterWing, font); ButtonLook.Room(enterChamber, font); ButtonLook.Room(journalHub, font);
            enterWing.gameObject.SetActive(false); enterChamber.gameObject.SetActive(false); // the art pass (owner, Oct 7, 86bcex5kc, 1A): the player taps the doors themselves; the buttons go, and nothing moves into their place // batch 2 (3c C): the Atrium's doors and journal in the chat box's language
            hubNote = Label(hub, "", 0, 722, 330, 28, 10); hubNote.color = Muted; // two lines: the owner's sealed-door and desk lines are long
            endCard = Label(hub, "The whole wheel burns. The symbols await you next.", 0, 736, 330, 36, 11); endCard.gameObject.SetActive(false); // owner (worksheet section 1); two lines at 330 wide
            walkSpeed = TestButton(hub, "Walk: normal (test)", -118, 774, CycleWalkSpeed); walkSpeedLabel = walkSpeed.GetComponentInChildren<Text>();
            mute = TestButton(hub, MuteLabel, 0, 774, ToggleMute); // Build E: sound on or off, apart from reduced motion
            hubRestart = TestButton(hub, "Start over (test)", 118, 774, Restart);
            foreach (var test in new[] { walkSpeed, mute, hubRestart }) test.gameObject.SetActive(false); // Build U: off the Atrium, into Settings (owner, Sept 26 playtest, note 8); their web actions stay
        }
        void BuildReview()
        {
            // Q05 decision 2: direct-tap items live here; compressed Dial items use the Wing canvas.
            review = ScreenPanel("Practice");
            SafeArea.Top(Label(review, "PRACTICE", 0, 32, 340, 24, 18)); // Build F: the tap forms of practice; the wheel forms use the Dial canvas with a "Practice · n of N" header
            reviewProgress = SafeArea.Top(Label(review, "", 0, 62, 300, 20, 12), SafeArea.UnderTitle); reviewProgress.color = Muted;
            reviewQuestion = Label(review, "", 0, 200, 330, 48, 15); // two lines for the owner's element question
            for (int i = 0; i < 4; i++) { string element = Elements[i]; elementButtons[i] = MakeButton(review, element, -78 + (i % 2) * 156, 300 + (i / 2) * 64, 150, 56, () => AnswerTap(element)); }
            for (int i = 0; i < 3; i++) { string modality = Zodiac.Modalities[i]; modalityButtons[i] = MakeButton(review, modality, 0, 300 + i * 64, 300, 56, () => AnswerModalityTap(modality)); modalityButtons[i].gameObject.SetActive(false); }
            var glyphBox = Rect("Review glyph", review, 0, 150, 90, 90); reviewGlyph = Label(glyphBox, "", 0, 45, 90, 90, 60); reviewGlyph.font = Dial.GlyphFont; reviewGlyph.horizontalOverflow = HorizontalWrapMode.Overflow; reviewGlyph.verticalOverflow = VerticalWrapMode.Overflow; reviewGlyph.gameObject.SetActive(false);
            for (int i = 0; i < 4; i++) { int slot = i; reviewGlyphButtons[i] = MakeButton(review, "", -78 + (i % 2) * 156, 300 + (i / 2) * 64, 150, 56, () => AnswerGlyphReview(slot)); reviewGlyphButtons[i].gameObject.SetActive(false); }
            reviewNote = Label(review, "", 0, 440, 330, 50, 14);
            reviewSummary = Label(review, "", 0, 520, 330, 30, 16);
            leavePractice = MakeButton(review, "Leave the instrument", 0, 654, 190, 56, LeavePractice); // Build F: an exit at any point (the Sept 15 defect)
            foreach (var b in elementButtons) ButtonLook.Plate(b); foreach (var b in modalityButtons) ButtonLook.Plate(b); foreach (var b in reviewGlyphButtons) ButtonLook.Plate(b); ButtonLook.Rule(leavePractice); // batch 2 (3b C, 3c C): practice is an instrument: bronze plates, and the way out on a rule
        }
        void BuildGlyphs()
        {
            // v0.3 Part A: name the glyph by direct tap. Same layout as a review item so the two read as one family.
            glyphs = ScreenPanel("Glyphs", "book-room"); Bleed.Add(glyphs.GetComponent<Image>(), root, Bleed.Mode.Clamp, .35f); // the Book open on its lectern behind the layout (owner, Oct 7, 86bcex5kc 4A; 86bcf0x71)
            // the symbols learned, inked on the open pages: symbols only, scattered rather than in wheel order (owner, Oct 7, answer 2A)
            for (int seat = 0; seat < 12; seat++) { var spot = InkSpots[InkPlace[seat]]; bookInk[seat] = Label(glyphs, Zodiac.Seats[seat].Glyph, spot.x, spot.y, 48, 48, InkSize); bookInk[seat].font = Dial.GlyphFont; bookInk[seat].color = Clear(BookInk); bookInk[seat].raycastTarget = false; bookInk[seat].horizontalOverflow = HorizontalWrapMode.Overflow; bookInk[seat].verticalOverflow = VerticalWrapMode.Overflow; } // the symbol font's line is taller than its box: truncated, it drew nothing
            SafeArea.Top(Label(glyphs, "THE BOOK OF SYMBOLS", 0, 32, 340, 24, 18)); // the instrument's own name (owner, Sept 26 playtest, note 13; name picked by the owner, Sept 27)
            glyphProgress = SafeArea.Top(Label(glyphs, "", 0, 62, 300, 20, 12), SafeArea.UnderTitle); glyphProgress.color = Muted;
            var card = Rect("Glyph card", glyphs, 0, 200, 140, 140); bookCard = card.gameObject.AddComponent<Image>(); bookCard.color = PanelColor; // Build E: book-cover shut, book-page open
            glyphCard = Label(card, "", 0, 70, 130, 130, 84); glyphCard.font = Dial.GlyphFont; glyphCard.horizontalOverflow = HorizontalWrapMode.Overflow; glyphCard.verticalOverflow = VerticalWrapMode.Overflow;
            bookCardRect = card; if (Slots.Image("book-room") != null) bookCard.enabled = false; // the symbol rises from the open Book, not from a card (86bcf0x71)
            var glow = glyphCard.gameObject.AddComponent<Outline>(); glow.effectColor = new Color(1, .84f, .55f, .55f); glow.effectDistance = new Vector2(2, -2); // the Library's neutral gold: no element shows before the answer (owner, Oct 7)
            var emblemRect = Rect("Glyph emblem", glyphs, 0, EmblemY, EmblemSize, EmblemSize); glyphEmblem = emblemRect.gameObject.AddComponent<Image>(); glyphEmblem.raycastTarget = false; glyphEmblem.preserveAspect = true; emblemRect.gameObject.SetActive(false);
            glyphQuestion = Label(glyphs, "This symbol belongs to which sign?", 0, 290, 330, 24, 15); // owner (worksheet section 4)
            for (int i = 0; i < 4; i++) { int slot = i; glyphNameButtons[i] = MakeButton(glyphs, "", -78 + (i % 2) * 156, 340 + (i / 2) * 64, 150, 56, () => AnswerGlyphName(slot)); }
            var panel = Rect("Caspar panel", glyphs, 0, 520, 324, 120); var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.color = PanelColor; glyphPanel = panel;
            var casparLabel = Label(panel, "CASPAR", 0, 14, 290, 20, 13);
            glyphCaspar = Label(panel, "", 0, 70, 306, 90, 12);
            Slots.DressInstrumentBox(panelImage, casparLabel, glyphCaspar, font); // Build R: the slim box fitted to the line on the Book of Symbols (owner, Sept 29)
            glyphNote = Label(glyphs, "", 0, 446, 330, 24, 14);
            closeBook = MakeButton(glyphs, "Close the Book", 0, 680, 300, 52, CloseBook); // owner (worksheet section 7) // between the name buttons (to 432) and the Caspar panel (from 460)
            foreach (var b in glyphNameButtons) ButtonLook.Plate(b); ButtonLook.Rule(closeBook); // batch 2 (owner, Oct 2: "bronze on the Table and the Book too")
            for (int i = 0; i < 4; i++) // the owner, Oct 8: near-black plates, the words kept simple and floating over them
            {
                var feel = glyphNameButtons[i].GetComponent<ButtonFeel>(); glyphPlateHome[i] = ((RectTransform)glyphNameButtons[i].transform).anchoredPosition;
                if (feel == null) continue; feel.Tone = DarkPlate; feel.Unavailable = 1; feel.Repaint(); // solid through the answer's pause: half-strength dark plates read as glass
                if (feel.Label != null) { var lift = feel.Label.GetComponent<Shadow>(); if (lift != null) { lift.effectColor = new Color(0, 0, 0, .85f); lift.effectDistance = new Vector2(0, -4); } }
            }
        }
        void BuildGrid()
        {
            // Build B: the table. Four element rows by three kind columns, twelve sign tiles below. Tap a sign, tap a cell, Seal.
            gridScreen = ScreenPanel("Grid", "table-room"); Bleed.Add(gridScreen.GetComponent<Image>(), root, Bleed.Mode.Clamp, .35f); // the table's carved top (86bcf0x71)
            gridPlates = Slots.Image("table-plate") != null && Slots.Image("table-well") != null;
            SafeArea.Top(Label(gridScreen, "THE ELEMENTAL TABLE", 0, 32, 340, 24, 18)); // the instrument's own name (owner, Sept 26 playtest, note 13; name picked by the owner, Sept 27)
            gridKeys = SafeArea.Top(Label(gridScreen, "Keeper Keys: 2", 100, 62, 140, 20, 12), SafeArea.UnderTitle); gridKeys.alignment = TextAnchor.MiddleRight; // ten px in from the edge: at 360 wide the Dial's indicator touches it
            for (int c = 0; c < GridModel.Columns; c++) BurnIntoEdge(Label(gridScreen, GridModel.ColumnName(c), -72 + c * 92, 96, 86, 18, 11));
            for (int r = 0; r < GridModel.Rows; r++) BurnIntoEdge(Label(gridScreen, GridModel.RowName(r), -150, 128 + r * 52, 56, 48, 12));
            for (int cell = 0; cell < 12; cell++)
            {
                int index = cell;
                gridCells[cell] = MakeButton(gridScreen, "Cell " + (cell + 1), -72 + (cell % 3) * 92, 128 + (cell / 3) * 52, 86, 48, () => ChooseCell(index));
                var name = gridCells[cell].GetComponentInChildren<Text>(); name.text = ""; name.fontSize = 11; name.horizontalOverflow = HorizontalWrapMode.Overflow; // the cell shows the seated sign, not a number
                var nameRect = (RectTransform)name.transform; nameRect.anchoredPosition = new Vector2(10, -24); nameRect.sizeDelta = new Vector2(58, 44); gridCellNames[cell] = name;
                gridCellGlyphs[cell] = Label(gridCells[cell].transform, "", -30, 24, 24, 40, 16); gridCellGlyphs[cell].font = Dial.GlyphFont; gridCellGlyphs[cell].horizontalOverflow = HorizontalWrapMode.Overflow; gridCellGlyphs[cell].verticalOverflow = VerticalWrapMode.Overflow;
                if (gridPlates) DressWell(cell);
            }
            for (int seat = 0; seat < 12; seat++)
            {
                int index = seat;
                gridTiles[seat] = MakeButton(gridScreen, Zodiac.Seats[seat].Name, -129 + (seat % 4) * 86, 342 + (seat / 4) * 46, 82, 44, () => PickSign(index));
                var name = gridTiles[seat].GetComponentInChildren<Text>(); name.fontSize = 11; name.horizontalOverflow = HorizontalWrapMode.Overflow;
                var nameRect = (RectTransform)name.transform; nameRect.anchoredPosition = new Vector2(10, -22); nameRect.sizeDelta = new Vector2(58, 36); gridTileNames[seat] = name;
                gridTileGlyphs[seat] = Label(gridTiles[seat].transform, Zodiac.Seats[seat].Glyph, -28, 22, 24, 36, 16); gridTileGlyphs[seat].font = Dial.GlyphFont; gridTileGlyphs[seat].horizontalOverflow = HorizontalWrapMode.Overflow; gridTileGlyphs[seat].verticalOverflow = VerticalWrapMode.Overflow;
                gridTileHome[seat] = ((RectTransform)gridTiles[seat].transform).anchoredPosition;
                if (gridPlates) { Slots.Dress(gridTiles[seat].GetComponent<Image>(), "table-plate"); BurnIn(gridTileNames[seat]); BurnIn(gridTileGlyphs[seat]); var drag = gridTiles[seat].gameObject.AddComponent<PlateDrag>(); drag.View = this; drag.Seat = seat; }
            }
            gridReadout = Label(gridScreen, "", 0, 470, 340, 20, 12);
            var panel = Rect("Caspar panel", gridScreen, 0, 540, 324, 112); var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.color = PanelColor;
            var casparLabel = Label(panel, "CASPAR", 0, 14, 290, 20, 13);
            gridCaspar = Label(panel, "", 0, 66, 306, 84, 12);
            Slots.DressInstrumentBox(panelImage, casparLabel, gridCaspar, font); // Build R: the slim box fitted to the line on the Elemental Table (owner, Sept 29)
            gridStatus = Label(gridScreen, "", 0, 614, 330, 24, 12); gridStatus.color = Muted;
            gridSeal = MakeButton(gridScreen, "SEAL", 0, 654, 146, 56, GridSeal); gridSeal.GetComponent<Image>().color = Crimson;
            leaveGrid = MakeButton(gridScreen, "Leave the Table", -72, 714, 128, 48, LeaveGrid); leaveGrid.GetComponentInChildren<Text>().fontSize = 13; // owner (worksheet section 11)
            gridAsk = MakeButton(gridScreen, "Ask Caspar", 78, 714, 164, 48, GridAsk); gridAsk.GetComponentInChildren<Text>().fontSize = 13; gridAsk.gameObject.SetActive(false); // owner (worksheet section 8)
            ButtonLook.Plate(gridSeal, 31); ButtonLook.Rule(leaveGrid); ButtonLook.Plate(gridAsk); // batch 2 (owner, Oct 2: "bronze on the Table and the Book too"); 3d: its sign tiles are 44 px tall, on 46 px rows
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
            if (Dial.RoomArt) foreach (var retired in new[] { floor, shelf, chair }) retired.gameObject.SetActive(false); // Build Z (owner pick, Sept 30): the room behind the wheel replaces them on the Dial screen
            var cloth = Rect("Dust cloth", chair, 0, 10, 48, 14); cloth.gameObject.AddComponent<Image>().color = new Color(.3f, .3f, .32f); cloth.gameObject.SetActive(!HasArt(chair));
            // Build Z (owner, Sept 30): the Dial screen's candle is cut (it read as "!"); the candle slot still draws the opening's and the Chamber's
            var s = Rect("Seam", r, 0, 270, 332, 2); seam = s.gameObject.AddComponent<Image>(); seam.color = new Color(Bone.r, Bone.g, Bone.b, 0); seam.raycastTarget = false;
            var glow = Rect("Key glow", r, 0, 270, 140, 140); keyGlow = glow.gameObject.AddComponent<Image>(); keyGlow.sprite = SoftGlow(); keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); keyGlow.raycastTarget = false;
            keyRect = Rect("Keeper Key", r, 0, 270, 84, 40); var keyImage = keyRect.gameObject.AddComponent<Image>(); keyImage.raycastTarget = false; bool keyArt = Slots.Dress(keyImage, "keeper-key"); Slots.Paint(keyImage, new Color(Bone.r, Bone.g, Bone.b, 0), 1f);
            keyLabel = Label(keyRect, "KEEPER KEY", 0, 20, 80, 36, 12); keyLabel.color = new Color(Charcoal.r, Charcoal.g, Charcoal.b, 0); keyLabel.gameObject.SetActive(!keyArt); // the file draws its own Key
            keyIndicator = SafeArea.Top(Label(r, "Keeper Key: 1", 110, 92, 140, 20, 12), SafeArea.UnderTitle); keyIndicator.alignment = TextAnchor.MiddleRight; keyIndicator.gameObject.SetActive(false);
            wingContinue = MakeButton(r, "Continue", 0, 654, 190, 56, WingContinue); wingContinue.name = "Slice Continue"; wingContinue.gameObject.SetActive(false);
            // Build T (owner, APK playtest, Sept 29): Leave the Dial shows at all times, the first lesson included. It has the bottom row's left half
            // (Claude's working choice), clear of Seal and of the fork's row; Build U gave it the whole row once Reduced motion moved into Settings.
            leaveDial = MakeButton(r, "Leave the Dial", 0, 768, 216, 44, LeaveDial); leaveDial.name = "Leave the Dial"; leaveDial.GetComponentInChildren<Text>().fontSize = 13; leaveDial.gameObject.SetActive(false);
            // Build F: the fork (Sept 15 ruling) on the row below Back; the practice exit where the table's exit sits, clear of Ask Caspar.
            forkLesson = MakeButton(r, "Continue the lesson", -78, 714, 150, 48, ContinueLesson); forkLesson.GetComponentInChildren<Text>().fontSize = 12; forkLesson.gameObject.SetActive(false);
            forkPractice = MakeButton(r, "Practice what you know", 78, 714, 150, 48, EnterPractice); forkPractice.GetComponentInChildren<Text>().fontSize = 12; forkPractice.gameObject.SetActive(false);
            leavePracticeDial = MakeButton(r, "Leave the instrument", -72, 714, 128, 48, LeavePractice); leavePracticeDial.GetComponentInChildren<Text>().fontSize = 12; leavePracticeDial.gameObject.SetActive(false);
            ButtonLook.Plate(wingContinue); ButtonLook.Rule(leaveDial); ButtonLook.Plate(forkLesson); ButtonLook.Plate(forkPractice); ButtonLook.Rule(leavePracticeDial); // batch 2 (3b C): the Dial's own row in bronze; Leave the Dial on its rule
        }

        void BuildWingRoom()
        {
            // Q06 phase 2, decision 2: the Wing as a room with two points of interest, the Dial and the doorway back.
            wingRoom = ScreenPanel("Wing room", "wing"); wingLight = LightOverlay(wingRoom, "wing-light");
            wingRoomKit = BuildKit(wingRoom, "kit-", WingKit, "wing-grime", wingLight, KitGrime, KitVeil, KitLight, () => Mathf.Clamp(Flow.Keys, 0, 4), KitRestoredNow); // Build M/N: grime under the light, the pieces, then the veil
            var wingPlate = wingKit.FirstOrDefault(p => p.P.Name == "plate"); if (wingPlate?.Restored != null) PlateText((RectTransform)wingPlate.Restored.transform, WingPlateName);
            FinishKit(wingRoom, wingRoomKit); // everything built after this draws above the veil
            BuildDialEyes(); // Build Z
            SafeArea.Top(Label(wingRoom, "THE ZODIAC WING", 0, 32, 340, 24, 18));
            SafeArea.Top(Label(wingRoom, "The Elemental Pattern", 0, 62, 300, 20, 12), SafeArea.UnderTitle).color = Muted;
            // Build L (Wing composition, owner-approved mockup B, Sept 24): the room art carries the Dial, the table, the chair, and the shelf,
            // painted in place. Each object keeps an invisible tap area and its glow over its painted footprint; without room art the greybox shows.
            bool wingBaked = HasArt(wingRoom);
            var shelf = Block(wingRoom, "Collapsed bookshelf", 157, 295, 45, 290, wingBaked ? null : "shelf"); if (wingBaked) shelf.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            if (!wingBaked) for (int i = 0; i < 3; i++) { var book = Rect("Book", shelf, -14 + i * 14, 18, 8, 24); var bookImage = book.gameObject.AddComponent<Image>(); bookImage.color = new Color(.3f, .28f, .3f); Slots.Dress(bookImage, "shelf-book"); book.localRotation = Quaternion.Euler(0, 0, i * 9 - 9); }
            var glow = Rect("Shelf glow", shelf, 0, 145, 90, 320); shelfGlow = glow.gameObject.AddComponent<Image>(); shelfGlow.sprite = wingBaked ? SoftRing() : SoftGlow(); shelfGlow.color = new Color(.95f, .8f, .5f, 0); shelfGlow.raycastTarget = false; glow.SetAsFirstSibling();
            // Build Y (owner, Sept 26 playtest note 11 and the APK playtest: "a portal ring instead of a glow"): with the shelf's own kit file, the
            // shelf itself glows. A gold copy of its restored file lies over it and a soft gold edge hugs its silhouette; the ring is retired.
            var kitShelf = wingKit.FirstOrDefault(p => p.P.Name == "shelf")?.Restored;
            if (kitShelf != null)
            {
                glow.gameObject.SetActive(false);
                var shelfLit = new GameObject("Shelf light", typeof(RectTransform)).GetComponent<RectTransform>(); shelfLit.SetParent(kitShelf.transform, false);
                shelfLit.anchorMin = Vector2.zero; shelfLit.anchorMax = Vector2.one; shelfLit.offsetMin = shelfLit.offsetMax = Vector2.zero;
                var shelfLitImage = shelfLit.gameObject.AddComponent<Image>(); shelfLitImage.sprite = kitShelf.GetComponent<Image>().sprite; shelfLitImage.preserveAspect = kitShelf.GetComponent<Image>().preserveAspect; shelfLitImage.raycastTarget = false;
                shelfLitImage.color = new Color(1f, .82f, .5f, .22f); // a warm wash over the shelf, not a flat fill: its own detail shows through
                // APK Session 2, bug 1 (owner, Oct 1: "the bookshelf smears into a stretched, motion-blurred look"): Build Y drew the edge with two
                // Outline effects, and an Outline repeats the whole picture at its offset: eight shifted copies of the shelf, the smear. The edge is
                // now a halo made once from the file's own alpha (SilhouetteHalo), clear over the shelf itself, so no copy of its detail is offset.
                var halo = SilhouetteHalo(shelfLitImage.sprite);
                if (halo != null)
                {
                    var edge = new GameObject("Shelf edge", typeof(RectTransform)).GetComponent<RectTransform>(); edge.SetParent(shelfLit, false);
                    float perPixel = ((RectTransform)kitShelf.transform).sizeDelta.x / shelfLitImage.sprite.rect.width; // the layout's units per file pixel
                    edge.anchorMin = edge.anchorMax = new Vector2(.5f, .5f); edge.anchoredPosition = Vector2.zero; edge.sizeDelta = halo.rect.size * perPixel;
                    var edgeImage = edge.gameObject.AddComponent<Image>(); edgeImage.sprite = halo; edgeImage.raycastTarget = false; edgeImage.color = new Color(1f, .8f, .45f, .85f);
                    shelfEdge = "halo";
                }
                shelfLight = shelfLit.gameObject.AddComponent<CanvasGroup>(); shelfLight.alpha = 0; shelfLight.blocksRaycasts = false;
            }
            Tappable(shelf, () => Walk("shelf")); // v0.3 revision: the book of symbols lives here once the wheel is lit
            var dial = Rect("The Dial", wingRoom, 40, 337, 175, 205); var dialImage = dial.gameObject.AddComponent<Image>(); dialImage.color = new Color(0, 0, 0, 0);
            var rings = Rect("Rings", dial, 0, 102, 175, 175); rings.gameObject.SetActive(!wingBaked && !Slots.Dress(dialImage, "dial-face")); if (wingBaked) dialImage.color = new Color(0, 0, 0, 0); // the face seen from the room
            RingLines(rings, 88, new Color(Bone.r, Bone.g, Bone.b, .4f), null); RingLines(rings, 30, new Color(Bone.r, Bone.g, Bone.b, .25f), null);
            var waitingGlow = Rect("Dial glow", wingRoom, 40, 330, 250, 250); dialGlow = waitingGlow.gameObject.AddComponent<Image>(); dialGlow.sprite = wingBaked ? SoftRing() : SoftGlow(); dialGlow.color = new Color(.95f, .8f, .5f, 0); dialGlow.raycastTarget = false; waitingGlow.SetSiblingIndex(dial.GetSiblingIndex()); // Build I (86bc1brxd): behind the Dial
            var dialLabel = Label(wingRoom, "The Dial", 40, 452, 120, 16, 10); dialLabel.gameObject.SetActive(wingKit.Count == 0); dialLabel.color = Muted; // Build M: the grey labels go once the kit is in (owner, Sept 24)
            Tappable(dial, () => Walk("dial"));
            // Build B (07 Room Scope amendment): a second interactive object, the table with its board of twelve, dark until the modality unit is complete.
            var table = Block(wingRoom, "The table", -62, 400, 90, 90, wingBaked ? null : "table"); if (wingBaked) table.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var board = Rect("Board", table, 0, 45, 60, 30); board.gameObject.SetActive(!wingBaked && !HasArt(table));
            for (int i = 0; i < 12; i++) { var square = Rect("Square", board, -20 + (i % 3) * 20, 5 + (i / 3) * 7, 16, 5); var squareImage = square.gameObject.AddComponent<Image>(); squareImage.color = new Color(.3f, .28f, .3f); squareImage.raycastTarget = false; }
            var tableGlow = Rect("Table glow", table, 0, 45, 120, 120); gridGlow = tableGlow.gameObject.AddComponent<Image>(); gridGlow.sprite = wingBaked ? SoftRing() : SoftGlow(); gridGlow.color = new Color(.95f, .8f, .5f, 0); gridGlow.raycastTarget = false; tableGlow.SetAsFirstSibling();
            Tappable(table, () => Walk("grid"));
            wingDoorGlow = DoorwayGlow(wingRoom, -138, 292, 42, 145); // the art pass (86bcex5kc 2A): it breathes light at its edges like the Atrium's open doors
            var door = Block(wingRoom, "Doorway back", -138, 292, 42, 145, "door-open"); Tappable(door, () => Walk("atrium-door")); HitArea(door, 48); // Build K: the painted doorway
            if (wingKit.Count > 0) foreach (var block in new[] { shelf, table, door }) foreach (var t in block.GetComponentsInChildren<Text>(true)) t.gameObject.SetActive(false); // Build M: the plates name the doors now
            var light = Rect("Doorway light", door, 0, 72, 30, 118); var lightImage = light.gameObject.AddComponent<Image>(); lightImage.color = new Color(.95f, .8f, .5f, HasArt(door) ? .05f : .25f); lightImage.raycastTarget = false;
            var floor = Rect("Floor band", wingRoom, 0, BandY, 340, 30); var floorImage = floor.gameObject.AddComponent<Image>(); floorImage.color = new Color(.16f, .16f, .19f, wingBaked ? 0 : 1); floorImage.raycastTarget = false;
            wingRoomCaption = Label(wingRoom, "The Dial stands at the center of the room. The doorway behind you leads back to the Atrium.", 0, 466, 340, 36, 12); // owner (worksheet section 6); two lines at 340 wide; Bone, not Muted: grey can't be read over the painted floor (owner, Oct 8)
            enterGrid = MakeButton(wingRoom, "The Table", 0, 512, 300, 52, () => Walk("grid")); enterGrid.gameObject.SetActive(false); // Build B: shown once the table has woken // owner (worksheet section 11)
            enterDial = MakeButton(wingRoom, "The Dial", 0, 624, 300, 52, () => Walk("dial"));
            enterShelf = MakeButton(wingRoom, "The Bookshelf", 0, 568, 300, 52, () => Walk("shelf")); enterShelf.gameObject.SetActive(false); // owner (worksheet section 7)
            wingRoomBack = MakeButton(wingRoom, "Return to the Atrium", 0, 680, 300, 52, LeaveWing); wingRoomBack.gameObject.SetActive(false); // owner (worksheet section 6); since the art pass (86bcex5kc 1A) the doorway back takes the tap
            journalWing = MakeButton(wingRoom, "Your journal", 0, 774, 216, 44, OpenJournal); journalWing.GetComponentInChildren<Text>().fontSize = 13; journalWing.gameObject.SetActive(false); // Build F
            ButtonLook.Room(enterGrid, font); ButtonLook.Room(enterDial, font); ButtonLook.Room(enterShelf, font); ButtonLook.Room(wingRoomBack, font); ButtonLook.Room(journalWing, font); // batch 2 (3c C, as the board shows it); 3d: Your journal at 44 px
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
            Bleed.Add(fadeImage, root); // Part 2: a fade to black covers the whole screen
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
            var image = r.GetComponent<Image>(); image.raycastTarget = true; image.canvasRenderer.cullTransparentMesh = false; // a target retired to invisible (the kits, the baked Wing) is culled otherwise, and the raycaster skips it
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
            if (command.StartsWith("safe-inset:") && float.TryParse(command.Substring(11), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float band)) { SafeArea.Simulated = band; Publish(); return; } // test-only (Platform fit, Part 1): a top band in layout units; -1 restores the screen's own
            if (command == "wake-cycle") { CycleWake(); return; } // DEV Mode: the Dial's wake-up preview, a step at a time
            if (command.StartsWith("wake:") && int.TryParse(command.Substring(5), out int wake)) { wakePreview = Mathf.Clamp(wake, -1, 4); Dial.WakeStep = WakeTarget; Settings?.Refresh(); Publish(); return; } // test-only: a step, or -1 for as earned
            if (command == "next-screen") Continue();
            else if (command.StartsWith("birth:")) ChooseBirth(command.Substring(6));
            else if (command.StartsWith("birthdate:")) UseDate(command.Substring(10));
            else if (command.StartsWith("birthtime:")) UseTime(command.Substring(10)); // batch 2: the time, the place's search and pick
            else if (command == "birthtime-unknown") { if (!busy && Flow.SetBirthTime(-1)) AfterBirthEntry(); }
            else if (command.StartsWith("birthplace-search:")) SearchPlace(command.Substring(18));
            else if (command.StartsWith("birthplace:") && int.TryParse(command.Substring(11), out int placeIndex)) PickPlace(placeIndex);
            else if (command.StartsWith("sign:") && int.TryParse(command.Substring(5), out int seat)) PickBirthSign(seat); // the skip path's sun, moon and rising, the facts' rising, or the rising changed on "Your Birth" (Oct 7)
            else if (command == "birthplace-unknown") { if (!busy && Flow.SetBirthPlace(null)) AfterBirthEntry(); } // "I don't know where" (Oct 7)
            else if (command == "birth-cancel") CancelBirthAmend();
            else if (command == "journal-birth") JournalBirthOpen();
            else if (command == "birth-add") BirthAdd(false);
            else if (command == "birth-add-time") BirthAdd(true);
            else if (command == "birth-rising") BirthChooseRising();
            else if (command.StartsWith("jump-birth:")) BirthSample(command.Substring(11)); // test-only: a fresh opening at a path's first question, or a version 4 save to convert
            else if (command.StartsWith("cusp:") && int.TryParse(command.Substring(5), out int cuspChoice)) PickCusp(cuspChoice); // the cusp day: 0 or 1 (the sign it left, the one it entered)
            else if (command == "cusp-why") ToggleCuspWhy();
            else if (command.StartsWith("moon:") && int.TryParse(command.Substring(5), out int moonChoice)) PickMoonSign(moonChoice); // Oct 7: the Moon's options by index
            else if (command == "jump-cusp") CuspDay(); // DEV Mode's cusp-day sample
            else if (command.StartsWith("name:")) { Flow.SetName(command.Substring(5)); if (nameField != null) nameField.text = Flow.PlayerName; Publish(); }
            else if (command == "insert") Insert();
            else if (command == "enter-chamber") EnterChamber();
            else if (command == "leave-chamber") LeaveChamber();
            else if (command == "restart") Restart();
            else if (command == "reload") { if (!busy) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); } // test-only: resume from the local save
            else if (command == "travel") MiniMenu?.Toggle(); // the room mini-menu (86bca07wv): the button, then a row
            else if (command.StartsWith("travel:")) MiniMenu?.Pick(command.Substring(7));
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
            else if (command == "walk-speed") { CycleWalkSpeed(); Settings.Refresh(); }
            else if (command == "settings") Settings.Toggle(); // Build U: the gear
            else if (command.StartsWith("jump:")) JumpTo(command.Substring(5)); // Build W: DEV Mode
            else if (command == "leave-wing") LeaveWing();
            else if (command == "leave-dial") LeaveDial(); // note 10
            else if (command == "continue-lesson") ContinueLesson(); // Build F
            else if (command == "enter-practice") EnterPractice();
            else if (command == "leave-practice") LeavePractice();
            else if (command == "open-journal") OpenJournal();
            else if (command == "close-journal") CloseJournal();
            else if (command == "journal-next") JournalTurn(1);
            else if (command == "journal-prev") JournalTurn(-1);
            else if (command == "journal-wheel") JournalWheel(); // Build AA: the Wheel, its views and tabs, a seat's preview, a page's links
            else if (command == "journal-contents") JournalContents(); // batch 2: the landing's Contents door, and "‹ Contents" on a chapter
            else if (command == "journal-landing") JournalHome();      // "‹ Your Journal" on Contents
            else if (command.StartsWith("journal-chapter:")) JournalChapter(command.Substring(16)); // a row on Contents: wheel or map
            else if (command == "journal-practice") JournalPracticeOpen(); // Oct 3, Practice: the landing's door, a concept's row, a choice, Next, and back to the list
            else if (command.StartsWith("practice-row:") && int.TryParse(command.Substring(13), out int practiceRow)) StartPracticeRound(practiceRow);
            else if (command.StartsWith("quiz-choice:") && int.TryParse(command.Substring(12), out int quizPick)) PickQuizChoice(quizPick);
            else if (command == "quiz-next") QuizNext();
            else if (command == "quiz-back") QuizBack();
            else if (command.StartsWith("journal-view:")) JournalSetView(command.Substring(13) == "table");
            else if (command.StartsWith("journal-lens:") && int.TryParse(command.Substring(13), out int jlens) && jlens >= 0 && jlens < 4) JournalLensTap((JournalLens)jlens);
            else if (command.StartsWith("journal-seat:") && int.TryParse(command.Substring(13), out int jseat) && jseat >= 0 && jseat < 12) JournalSeatTap(jseat);
            else if (command == "journal-open") JournalOpenSelected();
            else if (command.StartsWith("journal-chip:") && int.TryParse(command.Substring(13), out int jchip)) JournalChipTap(jchip);
            else if (command.StartsWith("element:") && int.TryParse(command.Substring(8), out int element) && element >= 0 && element < 4) AnswerTap(Elements[element]);
            else if (command.StartsWith("modality:") && int.TryParse(command.Substring(9), out int modality) && modality >= 0 && modality < 3) AnswerModalityTap(Zodiac.Modalities[modality]);
            else if (command.StartsWith("glyph-name:") && int.TryParse(command.Substring(11), out int slot) && slot >= 0 && slot < 4) { if (Flow.AtPractice) AnswerGlyphReview(slot); else AnswerGlyphName(slot); }
        }
        void ChooseBirth(string choice) { if (busy) return; Flow.ChooseBirth(choice); placeMatches.Clear(); cuspWhyOpen = false; foreach (var f in new[] { dateField, timeField, placeField }) if (f != null) f.SetTextWithoutNotify(""); AfterBirthEntry(); }
        void UseDate(string text)
        {
            var parts = (text ?? "").Trim().Split('/', '-', '.'); int year = 0, month = 0, day = 0; bool ok = false;
            if (parts.Length == 3 && parts[0].Length == 4) ok = int.TryParse(parts[0], out year) && int.TryParse(parts[1], out month) && int.TryParse(parts[2], out day); // the Web's date picker: YYYY-MM-DD
            else if (parts.Length == 3) ok = int.TryParse(parts[0], out day) && int.TryParse(parts[1], out month) && int.TryParse(parts[2], out year); // typed: day, month, year
            if (!ok || !Flow.SetBirthDate(year, month, day)) { birthNote.text = "That is not a date I know. Try day, month and year, like 25/04/1990."; Publish(); return; } // placeholder (owner writes)
            AfterBirthEntry();
        }
        // a time typed as 24-hour or with am / pm (the Web's time picker sends HH:MM)
        void UseTime(string text)
        {
            var t = (text ?? "").Trim().ToLowerInvariant(); bool pm = t.EndsWith("pm") || t.EndsWith("p.m."), am = t.EndsWith("am") || t.EndsWith("a.m.");
            var parts = t.Replace("p.m.", "").Replace("a.m.", "").Replace("pm", "").Replace("am", "").Trim().Split(':', '.'); int h = 0, m = 0;
            bool ok = parts.Length == 2 && int.TryParse(parts[0], out h) && int.TryParse(parts[1], out m) && m >= 0 && m < 60 && (am || pm ? h >= 1 && h <= 12 : h >= 0 && h < 24);
            if (ok && pm && h < 12) h += 12; if (ok && am && h == 12) h = 0;
            if (!ok || !Flow.SetBirthTime(h * 60 + m)) { birthNote.text = "That is not a time I know. Try hours and minutes, like 14:30."; Publish(); return; } // placeholder (owner writes)
            AfterBirthEntry();
        }
        void SearchPlace(string text) { if (Flow.BirthStep != "place") return; placeMatches = Places.Search(text, placeMatchButtons.Length); ShowBirth(); Publish(); }
        void PickPlace(int index) { if (busy || index < 0 || index >= placeMatches.Count || !Flow.SetBirthPlace(placeMatches[index])) return; placeMatches.Clear(); AfterBirthEntry(); }
        void PickBirthSign(int seat) { if (busy || !Flow.PickSign(seat)) return; AfterBirthEntry(); } // the skip path's sun, or the rising chosen on "Your Birth"
        void CancelBirthAmend() { if (busy || !Flow.CancelAmending()) return; AfterBirthEntry(); }
        void PickCusp(int choice) { if (busy || !Flow.PickCuspSun(choice)) return; cuspWhyOpen = false; AfterBirthEntry(); }
        void PickMoonSign(int choice) { if (busy || !Flow.PickMoon(choice)) return; AfterBirthEntry(); }
        void ToggleCuspWhy() { if (busy || Flow.BirthStep != "cusp") return; cuspWhyOpen = !cuspWhyOpen; ShowBirth(); Publish(); }
        // DEV Mode's cusp-day sample (owner, Oct 2 evening): a fresh opening at the cusp question (London, Apr 20 1990, no birth time), the name kept
        // test-only (Oct 7): a version 4 save from each retired or older path, written over the After Key 1 checkpoint and reloaded, so the
        // conversion runs as it will on a phone: v4-chart (London, Apr 20 1990, no time, the cusp's Taurus picked), v4-known (Leo, Scorpio,
        // rising unknown), v4-chosen (the random sun: Capricorn), v4-legacy (a sun-only save from before #110: Gemini)
        void BirthSample(string id)
        {
            if (busy) return; if (SettingsMenu.BirthSamples.Any(b => b.id == id)) { BirthOpening(id); return; } // an opening sample needs no save
            SaveData save;
            try { save = DevCheckpoints.Play("key1", Flow.PlayerName, 1); } catch (Exception e) { Debug.LogWarning("[CelestialDial] " + e.Message); return; }
            save.version = 4; save.birth = new BirthFacts(); save.choices = new ChoiceRecord[0]; save.chart = new WorkedChart();
            save.moonSign = save.risingSign = save.moonFrom = save.moonTo = -1; save.sunBasis = save.birthDate = save.birthPlace = save.birthZone = ""; save.birthMinute = -1; save.birthLatitude = save.birthLongitude = 0;
            switch (id)
            {
                case "v4-chart":
                    BirthChart.CuspSample(out int y, out int m, out int d, out var london); var c = BirthChart.Work(y, m, d, -1, london);
                    save.chartFrom = "chart"; save.sunSign = 1; save.sunBasis = "picked"; save.moonSign = c.Moon; save.moonFrom = c.Moon < 0 ? c.MoonFrom : -1; save.moonTo = c.Moon < 0 ? c.MoonTo : -1;
                    save.birthDate = "1990-04-20"; save.birthPlace = london.Name; save.birthZone = Places.ZoneName(london.Zone); save.birthLatitude = london.Latitude; save.birthLongitude = london.Longitude; break;
                case "v4-known": save.chartFrom = "known"; save.sunSign = 4; save.moonSign = 7; break;
                case "v4-chosen": save.chartFrom = "chosen"; save.sunSign = 9; break;
                case "v4-legacy": save.chartFrom = ""; save.sunSign = 2; break;
                default: return;
            }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save)); PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        // DEV Mode (Oct 7, 86bced0tc): a fresh opening at a path's first question, the name kept: skip (Which sign do you go by?), no-time
        // (London, May 2 1990, no birth time: the rising asked), no-place (May 10 1990, 2:30 pm, I don't know where: the moon asked, then the rising),
        // neither (Apr 20 1990, no time, no place: the cusp question with no clock time, then the moon, then the rising)
        void BirthOpening(string id) { if (busy || !SettingsMenu.BirthSamples.Any(b => b.id == id)) return; birthSampleFor = Flow.PlayerName; birthSampleId = id; PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        void OpenBirthOpening()
        {
            var name = birthSampleFor; var id = birthSampleId; birthSampleFor = birthSampleId = null; Flow.SetName(name);
            if (!(Flow.Continue() && Flow.Screen == SliceScreen.Birth)) return;
            if (id == "skip") Flow.ChooseBirth("skip");
            else if (id == "no-time") { Flow.ChooseBirth("chart"); Flow.SetBirthDate(1990, 5, 2); Flow.SetBirthTime(-1); Flow.SetBirthPlace(Places.Find("London, Britain (UK)")); }
            else if (id == "no-place") { Flow.ChooseBirth("chart"); Flow.SetBirthDate(1990, 5, 10); Flow.SetBirthTime(14 * 60 + 30); Flow.SetBirthPlace(null); }
            else if (id == "neither") { Flow.ChooseBirth("chart"); Flow.SetBirthDate(1990, 4, 20); Flow.SetBirthTime(-1); Flow.SetBirthPlace(null); } // a cusp day: the question with no clock time, then the moon, then the rising
            if (nameField != null) nameField.SetTextWithoutNotify(Flow.PlayerName); ShowBirth();
        }
        void CuspDay() { if (busy) return; cuspDayFor = Flow.PlayerName; PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        void OpenCuspDay()
        {
            var name = cuspDayFor; cuspDayFor = null; Flow.SetName(name); BirthChart.CuspSample(out int year, out int month, out int day, out var place);
            if (!(Flow.Continue() && Flow.Screen == SliceScreen.Birth)) return; Flow.ChooseBirth("chart"); Flow.SetBirthDate(year, month, day); Flow.SetBirthTime(-1); Flow.SetBirthPlace(place);
            if (nameField != null) nameField.SetTextWithoutNotify(Flow.PlayerName); ShowBirth();
        }
        // a step on the birth screen; when one opened from "Your Birth" finishes, the record has changed: back to the page, the lessons told, saved
        void AfterBirthEntry()
        {
            if (Flow.Screen == SliceScreen.Journal) { SyncLessonSun(); Save(); Sound.Play("page"); Show(); Publish(); return; }
            ShowBirth(); Publish();
        }
        // only lessons not yet played start from a sun that arrives or changes (Oct 7); nothing lit or earned moves
        void SyncLessonSun() { if (sunSent && (Dial.Lesson.Sun != Flow.LessonSun || Dial.Lesson.SunKnown != Flow.HasSunSign)) { Dial.Lesson.UpdateSun(Flow.LessonSun, Flow.HasSunSign); Dial.ForceRefresh(); } }
        string BirthAsk()
        {
            switch (Flow.BirthStep)
            {
                case "date": return " Enter your birth date: day, month and year.";
                case "time": return " Enter your birth time, if you know it.";
                case "place": return " Enter your birth town or city, then pick it. " + SliceFlow.NearestCity;
                case "sun-pick": return " " + SliceFlow.SkipAsk;
                case "moon-pick": return " " + SliceFlow.SkipMoonAsk;
                case "rising-pick": return Flow.BirthChoice == "rising" ? " Choose your rising sign." : " " + SliceFlow.SkipRisingAsk;
                case "cusp": return " " + Flow.CuspQuestion + (cuspWhyOpen ? " " + SliceFlow.CuspWhy : "");
                case "moon": return " " + Flow.MoonQuestion;
            }
            return "";
        }
        void ShowBirth()
        {
            bool chosen = Flow.BirthChoice != ""; string step = Flow.BirthStep; bool chart = Flow.BirthChoice == "chart", signs = step == "sun-pick" || step == "moon-pick" || step == "rising-pick", amending = Flow.Amending;
            birthHeading.text = amending ? SliceFlow.BirthPageTitle : SliceFlow.BirthQuestion;
            birthChoices.gameObject.SetActive(!chosen);
            birthDate.gameObject.SetActive(chart && step == "date"); birthTimeEntry.gameObject.SetActive(chart && step == "time"); birthPlaceEntry.gameObject.SetActive(chart && step == "place");
            birthSigns.gameObject.SetActive(signs); signsLabel.gameObject.SetActive(signs); // no "I'm not sure" since Oct 7 (86bced0tc): every missing sign is picked
            bool cusp = chart && step == "cusp"; birthCusp.gameObject.SetActive(cusp); cuspWhy.gameObject.SetActive(cusp && cuspWhyOpen);
            bool moon = chart && step == "moon"; birthMoon.gameObject.SetActive(moon);
            if (moon) { moonQuestion.text = Flow.MoonQuestion; for (int i = 0; i < moonButtons.Length; i++) { bool show = i < Flow.MoonAsk.Length; moonButtons[i].gameObject.SetActive(show); if (show) { moonButtons[i].GetComponentInChildren<Text>().text = Zodiac.Seats[Flow.MoonAsk[i]].Name; ((RectTransform)moonButtons[i].transform).anchoredPosition = new Vector2(0, -(110 + i * 52)); } } }
            if (cusp) { cuspQuestion.text = Flow.CuspQuestion; cuspButtons[0].GetComponentInChildren<Text>().text = Zodiac.Seats[Flow.CuspFrom].Name; cuspButtons[1].GetComponentInChildren<Text>().text = Zodiac.Seats[Flow.CuspTo].Name; } // the cusp's "I'm not sure" is removed (owner, Oct 7, 86bced0tc)
            signsLabel.text = step == "sun-pick" ? SliceFlow.SkipAsk : step == "moon-pick" ? SliceFlow.SkipMoonAsk : step == "rising-pick" ? (Flow.BirthChoice == "rising" ? SliceFlow.RisingAsk : SliceFlow.SkipRisingAsk) : ""; // drafts (owner writes); "Your rising sign" when it is changed on "Your Birth"
            for (int i = 0; i < placeMatchButtons.Length; i++) { bool show = i < placeMatches.Count; placeMatchButtons[i].gameObject.SetActive(show); if (show) placeMatchButtons[i].GetComponentInChildren<Text>().text = placeMatches[i].Name; }
            placeUnknown.gameObject.SetActive(chart && step == "place"); placeUnknown.interactable = !busy;
            changeChoice.gameObject.SetActive(chosen && !amending); birthCancel.gameObject.SetActive(amending); birthCancel.interactable = !busy;
            birthNote.text = Flow.Note; birthNote.color = Bone;
            birthContinue.gameObject.SetActive(!amending); birthContinue.interactable = Flow.CanContinue;
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
        void WingContinue() { Continue(); } // Build T: only the opening's Continue after Key 1; the Dial's exit is its own button
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
            chamberBack.gameObject.SetActive(false); chamberBack.interactable = !busy; chamberContinue.gameObject.SetActive(!room && chamberContinue.gameObject.activeSelf); // the doorway back takes the tap (86bcex5kc 1A)
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
        // Build W (DEV Mode, task 86bca0163): write the checkpoint's save, from one scripted run with this player's name and sign, and reload into it like Start over.
        void JumpTo(string id)
        {
            if (busy || !DevCheckpoints.Known(id)) return;
            SaveData save;
            try { save = DevCheckpoints.Play(id, Flow.PlayerName, Flow.HasBirthRecord ? Flow.LessonSun : 1); }
            catch (Exception e) { Debug.LogWarning("[CelestialDial] " + e.Message); return; }
            if (Flow.HasBirthRecord) Flow.WriteBirth(save); // Oct 7: the jump keeps the player's birth record; the London sample only stands in when there is none
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save)); PlayerPrefs.Save();
            Debug.Log("[CelestialDial] jumped to checkpoint " + id);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        // Q06 phase 2, decision 5: the buttons and the taps do the same thing through the same walk.
        void EnterWing() { Walk("wing-door"); }
        // Build F: whether the wheel has a lesson to offer at the fork (Part A stays on the shelf; the book's replay is the shelf's too).
        bool LessonAvailable => Dial.Lesson.CanContinueUnit || (Dial.Lesson.CanBeginGlyphs && Dial.Lesson.AllNamed) || (Dial.Lesson.Phase != LessonPhase.GlyphWheel && Dial.Lesson.CanBeginModalities) || Dial.Lesson.CanBeginOpposites;
        void EnterDialNow()
        {
            if (!Flow.EnterDial()) return;
            if (Flow.AtriumStage >= 2) Dial.SliceHidesOptional = true; Dial.Lesson.SetKey3(Flow.Keys >= 3); // Build T: the first lesson, now reached through the Wing, keeps its optional extra
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
        // the row and column names, burned into the table's edge: bone ink pressed into the wood, legible on its dark top
        void BurnIntoEdge(Text label) { if (Slots.Image("table-room") == null) { label.color = Muted; return; } label.font = ButtonLook.EngravedFont ?? label.font; label.color = new Color(Bone.r, Bone.g, Bone.b, .82f); var press = label.gameObject.AddComponent<Shadow>(); press.effectColor = new Color(0, 0, 0, .7f); press.effectDistance = new Vector2(.8f, -1.2f); }
        // a plate's symbol and name, burned into the wood
        void BurnIn(Text label) { if (label.font != Dial.GlyphFont) { label.font = ButtonLook.EngravedFont ?? label.font; label.resizeTextForBestFit = true; label.resizeTextMaxSize = label.fontSize; label.resizeTextMinSize = 8; label.horizontalOverflow = HorizontalWrapMode.Wrap; } label.color = BurnedInk; var edge = label.gameObject.AddComponent<Shadow>(); edge.effectColor = new Color(1, .9f, .75f, .3f); edge.effectDistance = new Vector2(.6f, -.8f); }
        // A well: the carved recess, a light that shows when a plate is held over it, and the plate that sits in it, wood with gold over it behind a
        // mask that opens from the centre on a correct Seal. Each well always holds the same sign, so its live lettering is made once.
        void DressWell(int cell)
        {
            var well = gridCells[cell]; Slots.Dress(well.GetComponent<Image>(), "table-well"); var t = (RectTransform)well.transform;
            var light = new GameObject("Well light", typeof(RectTransform)).AddComponent<Image>(); light.rectTransform.SetParent(t, false); Centre(light.rectTransform, 112, 74);
            light.sprite = SoftGlow(); light.raycastTarget = false; light.color = new Color(Bone.r, Bone.g, Bone.b, 0); gridWellLight[cell] = light;
            var wood = new GameObject("Plate", typeof(RectTransform)).AddComponent<Image>(); wood.rectTransform.SetParent(t, false); Centre(wood.rectTransform, 78, 42); wood.raycastTarget = false; Slots.Dress(wood, "table-plate"); gridCellWood[cell] = wood;
            var mask = new GameObject("Gold", typeof(RectTransform)).GetComponent<RectTransform>(); mask.SetParent(wood.rectTransform, false); Centre(mask, 0, 42); mask.gameObject.AddComponent<RectMask2D>(); gridGoldMask[cell] = mask;
            var gold = new GameObject("Gold plate", typeof(RectTransform)).AddComponent<Image>(); gold.rectTransform.SetParent(mask, false); Centre(gold.rectTransform, 78, 42); gold.raycastTarget = false; Slots.Dress(gold, "table-plate-gold"); gridCellGold[cell] = gold;
            light.rectTransform.SetSiblingIndex(0); wood.rectTransform.SetSiblingIndex(1); // under the cell's symbol and name
            BurnIn(gridCellNames[cell]); BurnIn(gridCellGlyphs[cell]); gridCellGlyphs[cell].fontSize = 18;
            foreach (var label in new[] { gridCellNames[cell], gridCellGlyphs[cell] }) { var line = label.gameObject.AddComponent<Outline>(); line.effectColor = new Color(.17f, .11f, .09f, .95f); line.effectDistance = new Vector2(1, -1); line.enabled = false; } // live lettering keeps the Art Bible's dark-brown line, so every element reads on gold
            int seat = GridModel.SeatOf(cell); var fill = Slots.Image(LetterSlot(seat)); var shader = Resources.Load<Shader>("Shaders/LetterFill");
            if (fill != null && shader != null) { gridCellGlyphLive[cell] = LiveLetters(shader, fill, gridCellGlyphs[cell]); gridCellNameLive[cell] = LiveLetters(shader, fill, gridCellNames[cell]); }
        }
        static void Centre(RectTransform r, float w, float h) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(w, h); }
        static Material LiveLetters(Shader shader, Sprite fill, Text label)
        {
            var m = new Material(shader); m.SetTexture("_FillTex", fill.texture); var r = label.rectTransform.rect; m.SetVector("_Rect", new Vector4(r.x, r.y, r.width, r.height)); return m;
        }
        // Drag (owner, Oct 7: "drag and tap"): lifting a plate picks it, letting it go over a well chooses that well, and Seal still commits, the same
        // results as the taps (the Dial lock: the same results from every input). Dropped anywhere else, the plate goes back to its place in hand.
        public bool PlateDragBegin(int seat) { if (busy || Flow.Screen != SliceScreen.Grid || (Grid.Sign != seat && !Grid.Pick(seat))) return false; dragSeat = seat; hoverCell = -1; ShowGrid(); Publish(); return true; }
        public void PlateDragMove(Vector2 screen, Camera eye)
        {
            if (dragSeat < 0) return; var tile = (RectTransform)gridTiles[dragSeat].transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(gridScreen, screen, eye, out var local)) tile.anchoredPosition = new Vector2(local.x, local.y - 400); // the tile is placed from the screen's top centre, gridScreen's pivot is its middle
            int over = -1; for (int i = 0; i < 12; i++) if (Grid.CanChoose(i) && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)gridCells[i].transform, screen, eye)) over = i;
            if (over != hoverCell) { hoverCell = over; ShowGrid(); Publish(); }
        }
        public void PlateDragEnd()
        {
            if (dragSeat < 0) return; int cell = hoverCell, seat = dragSeat; dragSeat = -1; hoverCell = -1; ((RectTransform)gridTiles[seat].transform).anchoredPosition = gridTileHome[seat];
            if (cell >= 0) { Grid.InputMethod = "Drag"; Grid.Choose(cell); Grid.InputMethod = "DirectCell"; } // the log names the input, as the Dial does (DialInput.Drag)
            ShowGrid(); Publish();
        }
        public int DragSeat => dragSeat;
        // the gold opening from the plate's centre outward, hard-edged (Art Bible rules 2 and 8)
        IEnumerator GoldSpreads(int cell)
        {
            var mask = gridGoldMask[cell]; if (mask == null) yield break;
            for (float t = 0; t < GoldSeconds; t += Time.unscaledDeltaTime) { mask.sizeDelta = new Vector2(78 * Mathf.SmoothStep(0, 1, t / GoldSeconds), 42); yield return null; }
            mask.sizeDelta = new Vector2(78, 42);
        }
        void PickSign(int seat) { if (busy || Flow.Screen != SliceScreen.Grid || !Grid.Pick(seat)) return; ShowGrid(); Publish(); }
        void ChooseCell(int cell) { if (busy || Flow.Screen != SliceScreen.Grid || !Grid.Choose(cell)) return; ShowGrid(); Publish(); }
        void GridAsk() { if (busy || Flow.Screen != SliceScreen.Grid || !Grid.Ask()) return; ShowGrid(); Publish(); }
        void GridSeal()
        {
            if (busy || Flow.Screen != SliceScreen.Grid) return;
            int sealing = Grid.Sign; var result = Grid.Seal(); if (result == null) return;
            if (result.correctness) { goldSeat = sealing; StartCoroutine(GridSeated()); }
            else if (Grid.Demonstrating) StartCoroutine(GridDemonstrate());
            else { ShowGrid(); Publish(); }
        }
        IEnumerator GridSeated()
        {
            busy = true; ShowGrid(); Publish();
            int cell = goldSeat >= 0 ? GridModel.CellOf(goldSeat) : -1; bool gold = goldSeat >= 0 && gridPlates && PlacedOwn(goldSeat);
            if (gold && !ReducedMotion) { gridGoldMask[cell].sizeDelta = new Vector2(0, 42); yield return GoldSpreads(cell); yield return new WaitForSecondsRealtime(1.1f - GoldSeconds); } // the gold plays inside today's hold
            else yield return new WaitForSecondsRealtime(ReducedMotion ? .6f : 1.1f);
            goldSeat = -1;
            // Key 3's ceremony follows the seating inside the same busy span. Run side by side, the seating cleared busy mid-ceremony, and the
            // ceremony's end later cleared the walk home's busy mid-fade, so the fade ate the first tap in the Atrium (Sept 25).
            if (Grid.Key3Earned && Flow.Keys < 3) { Flow.MarkKey3(); Save(); yield return KeyCeremony(3); yield break; }
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
        // The room mini-menu (owner, Oct 1; task 86bca07wv): quick travel between the rooms open to the player. Doors stay the main way to
        // move; a row takes the doorways' own steps (out to the Atrium, then in), behind one fade instead of the walk.
        void BuildTravel()
        {
            MiniMenu = gameObject.AddComponent<TravelMenu>();
            MiniMenu.Here = TravelHere; MiniMenu.IsOpen = TravelOpen; MiniMenu.Busy = () => busy; MiniMenu.Go = id => StartCoroutine(TravelTo(id)); MiniMenu.Changed = Publish;
            MiniMenu.Build(root, font);
        }
        string TravelHere() => Flow.Screen == SliceScreen.Hub ? "atrium" : Flow.Screen == SliceScreen.WingRoom ? "wing" : Flow.Screen == SliceScreen.ChamberRoom ? "chamber" : "";
        bool TravelOpen(string id) => id == "atrium" || id == "wing" || (id == "chamber" && Flow.KeyInserted); // the Chamber is a room once Key 1 is in its lock; before, it folds into Sealed
        IEnumerator TravelTo(string id)
        {
            string here = TravelHere(); if (busy || here == "" || id == here || !TravelOpen(id)) yield break;
            busy = true; hubNote.text = ""; Show(); Publish();
            Sound.Play("door"); yield return FadeTo(1);
            if (Flow.Screen == SliceScreen.WingRoom) { Flow.LeaveWing(); Save(); } else if (Flow.Screen == SliceScreen.ChamberRoom) { Flow.LeaveChamber(); Save(); }
            if (id == "wing") Flow.EnterWing(); else if (id == "chamber") { Flow.EnterChamber(); chamberLine = DefaultChamberLine(); }
            Show(); PlaceAvatar(); Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion ? 0 : .12f);
            yield return FadeTo(0);
            busy = false; Show(); Publish();
        }
        // ---- The Dial's wake-up (owner, Oct 1; 86bcbn6w6: 2a A, a step per Key; 2d A, the Wing room's small Dial follows the same steps) ----
        int wakePreview = -1; // DEV Mode's preview of a step (-1: as earned)
        int WakeTarget => wakePreview >= 0 ? wakePreview : Mathf.Clamp(Flow.Keys, 0, 4);
        static readonly string[] WakeNames = { "first visit", "Key 1", "Key 2", "Key 3", "Key 4" };
        string WakeLabel => wakePreview < 0 ? "as earned" : WakeNames[wakePreview];
        void CycleWake() { wakePreview = wakePreview >= 4 ? -1 : wakePreview + 1; Dial.WakeStep = WakeTarget; Settings?.Refresh(); Publish(); } // as earned, then each step in turn
        // The small Dial: worn, half today's over it, today's, half bright over it, bright (a look without its file stands in with today's).
        void WakeKitDial()
        {
            if (wingRoomKit == null || wingRoomKit.Fade != null) return; var piece = wingRoomKit.Pieces.FirstOrDefault(p => p.P.Name == "dial"); if (piece == null) return;
            int s = WakeTarget; float worn = s <= 1 ? 1 : 0, today = s == 1 ? .5f : s >= 2 ? 1 : 0, bright = s == 3 ? .5f : s == 4 ? 1 : 0;
            if (piece.Worn != null) piece.Worn.alpha = worn; if (piece.Restored != null) piece.Restored.alpha = today; if (piece.Bright != null) piece.Bright.alpha = bright;
        }
        void Walk(string id)
        {
            if (busy || (Flow.Screen != SliceScreen.Hub && Flow.Screen != SliceScreen.WingRoom && Flow.Screen != SliceScreen.ChamberRoom)) return;
            var poi = Flow.Walk.Find(id); if (poi == null) return;
            if (!poi.Walkable || (id == "chamber-door" && Flow.Screen == SliceScreen.Hub && !Flow.CanEnterChamber)) { if (Flow.TouchSealedDoor()) { hubNote.text = Flow.Note; Publish(); } return; } // Build T: before Key 1 the Chamber is sealed
            if (id == "shelf" && !Flow.WheelComplete) { if (Flow.TouchDarkShelf()) { wingRoomCaption.text = DialLesson.ShelfDark; Publish(); } return; }
            if (id == "grid" && !Flow.CanOpenGrid) { if (Flow.TouchDarkGrid()) { wingRoomCaption.text = GridModel.DarkLine; Publish(); } return; } // Build B: dark and tappable with a note before the unit
            if (!Flow.Walk.GoTo(id)) return;
            if (id == "dial" && Flow.Screen == SliceScreen.WingRoom) StartCoroutine(OpenDialEye()); // Build Z (owner, Sept 30): the Dial opens its eye as the player comes to it
            StartCoroutine(Travel(id));
        }
        IEnumerator Travel(string id)
        {
            busy = true; hubNote.text = ""; Publish();
            var walk = Flow.Walk;
            if (ReducedMotion) walk.Jump(); // decision 4: reduced motion jumps
            else while (!walk.Tick(Time.unscaledDeltaTime)) { PlaceAvatar(); yield return null; }
            PlaceAvatar();
            while (id == "dial" && dialEyes.Count > 0 && dialEyeOpen < 1) yield return null; // Build Z: the eye finishes opening before the Dial screen
            if (id == "dial" && dialEyes.Count > 0 && !ReducedMotion) yield return new WaitForSecondsRealtime(DialEyeHold); // Build AB: and stays open a beat, so it is seen even with no walk
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
            var options = Dial.Lesson.GlyphOptions(target); int seat = options[slot]; bool own = Dial.Lesson.GlyphMisses <= 1; // a right answer alone or after a nudge counts as the player's own
            bool correct = Dial.Lesson.AnswerGlyphName(seat);
            if (Dial.Lesson.GlyphNamed[target]) // answered, or shown after a second miss: it comes alive (owner, Oct 7: only after the answer)
            {
                revealSeat = target; revealSlot = System.Array.IndexOf(options, target); revealOwn = correct && own;
                for (int i = 0; i < 4; i++) revealNames[i] = Zodiac.Seats[options[i]].Name;
            }
            glyphNote.text = Dial.Lesson.GlyphNameResult; Publish();
            StartCoroutine(AfterGlyphName());
        }
        IEnumerator AfterGlyphName()
        {
            busy = true; ShowGlyphs(); Publish();
            float hold = ReducedMotion ? .7f : 1.1f; // the answer's pause, as before; the emblem plays inside it
            if (revealSeat >= 0) yield return EmblemComesAlive(revealSeat, hold); else yield return new WaitForSecondsRealtime(hold);
            revealSeat = -1;
            if (Dial.Lesson.Phase == LessonPhase.GlyphWheel) // Part A done: a short look at the full pages (owner, Oct 7, 2A), then back to the room; the wheel runs Part B
            {
                bookPagesShown = true; ShowGlyphs(); Publish();
                yield return new WaitForSecondsRealtime(ReducedMotion ? 1.2f : BookPagesSeconds);
                bookPagesShown = false; busy = false; Flow.LeaveBook(); Save(); Show(); Publish(); yield break;
            }
            busy = false; Show(); Publish();
        }
        // The answered symbol becomes its element's emblem (the set the owner approved, Oct 8), holds, then sinks to its place on the page as ink.
        IEnumerator EmblemComesAlive(int seat, float hold)
        {
            var art = Slots.Image(EmblemSlot(seat)); var rect = (RectTransform)glyphEmblem.transform; var ink = InkSpots[InkPlace[seat]];
            glyphEmblem.sprite = art; glyphEmblem.gameObject.SetActive(art != null); glyphCard.enabled = art == null; Publish(); // the web state sees the emblem from its first frame
            if (art == null || ReducedMotion) { rect.anchoredPosition = new Vector2(0, -EmblemY); rect.localScale = Vector3.one; glyphEmblem.color = Color.white; ShowGlyphs(); yield return new WaitForSecondsRealtime(hold); }
            else
            {
                float sink = .4f, swell = .35f;
                for (float t = 0; t < hold; t += Time.unscaledDeltaTime)
                {
                    float k = t < swell ? Mathf.SmoothStep(0, 1, t / swell) : 1, d = Mathf.Clamp01((t - (hold - sink)) / sink);
                    float scale = Mathf.Lerp(.82f, 1f, k) + .05f * Mathf.Sin(Mathf.Clamp01(t / swell) * Mathf.PI); // it blooms a touch past its size, then settles
                    rect.localScale = Vector3.one * Mathf.Lerp(scale, .2f, d); rect.anchoredPosition = Vector2.Lerp(new Vector2(0, -EmblemY), new Vector2(ink.x, -ink.y), Mathf.SmoothStep(0, 1, d));
                    glyphEmblem.color = new Color(1, 1, 1, Mathf.Lerp(k, 0, d)); yield return null;
                }
            }
            glyphEmblem.gameObject.SetActive(false); glyphCard.enabled = true; rect.localScale = Vector3.one; rect.anchoredPosition = new Vector2(0, -EmblemY);
        }
        // A new question rises out of the open pages: the symbol, then its four plates, all alike so nothing in the motion points to the answer.
        IEnumerator RiseQuestion()
        {
            glyphRising = true;
            for (float t = 0; t < RiseSeconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / RiseSeconds), plates = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .1f) / (RiseSeconds - .1f)));
                bookCardRect.anchoredPosition = new Vector2(0, -Mathf.Lerp(RiseFrom, CardY, k)); glyphCard.color = new Color(Bone.r, Bone.g, Bone.b, k);
                for (int i = 0; i < 4; i++) ((RectTransform)glyphNameButtons[i].transform).anchoredPosition = glyphPlateHome[i] + new Vector2(0, -18 * (1 - plates));
                yield return null;
            }
            EndRise();
        }
        void EndRise() { glyphRising = false; Publish(); bookCardRect.anchoredPosition = new Vector2(0, -CardY); glyphCard.color = Bone; for (int i = 0; i < 4; i++) ((RectTransform)glyphNameButtons[i].transform).anchoredPosition = glyphPlateHome[i]; }
        void AnswerGlyphReview(int slot)
        {
            if (busy) return;
            var task = Flow.CurrentReview; if (task == null || task.Mode != ReviewMode.Glyph) return;
            int seat = Flow.GlyphReviewOptions(task.seat)[slot];
            Sound.Play(Flow.AnswerGlyph(seat) ? "seal" : "miss");
            reviewNote.text = Flow.Note; Save(); Publish();
            if (task.done) StartCoroutine(AfterTap());
        }
        // Leaving the Dial lands the player in the Zodiac Wing; the room's own button returns to the Atrium (owner, Sept 26 playtest, note 10).
        void LeaveDial()
        {
            if (busy || Flow.Screen != SliceScreen.Wing || Dial.Busy || !Flow.LeaveDial()) return; // Build T: mid-challenge too; the wheel resumes where it was
            forkShown = false; Save(); Show(); Publish();
        }
        void LeaveWing() { if (busy || Flow.Screen != SliceScreen.WingRoom) return; Walk("atrium-door"); }
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
            identity.gameObject.SetActive(s == SliceScreen.Identity); birth.gameObject.SetActive(s == SliceScreen.Birth); if (s == SliceScreen.Birth) ShowBirth(); // Oct 7: "Your Birth" opens the opening's boxes
            journal.gameObject.SetActive(s == SliceScreen.Journal); if (s == SliceScreen.Journal) ShowJournal();
            atrium.gameObject.SetActive(s == SliceScreen.Atrium); atriumReturn.gameObject.SetActive(s == SliceScreen.AtriumReturn);
            chamber.gameObject.SetActive(s == SliceScreen.Chamber || s == SliceScreen.ChamberRoom); hub.gameObject.SetActive(s == SliceScreen.Hub);
            wingRoom.gameObject.SetActive(s == SliceScreen.WingRoom);
            Dial.Showing = s == SliceScreen.Wing || practiceOnDial; // Build Z: a pattern's reveal plays only where the player sees the wheel
            if (s == SliceScreen.WingRoom && shownScreen != SliceScreen.WingRoom && dialEyeOpen > 0) SetDialEye(0); // Build Z: back in the room, the Dial's eye rests closed. Build AB (owner, Oct 1: "the eye no longer blinks open"): only on coming back, not on every refresh, which shut it mid-opening when the Keeper already stood at the Dial
            shownScreen = s;
            gridScreen.gameObject.SetActive(s == SliceScreen.Grid); if (s == SliceScreen.Grid) ShowGrid();
            bool book = s == SliceScreen.Book;
            bool roomScreen = s == SliceScreen.Hub || s == SliceScreen.WingRoom || s == SliceScreen.ChamberRoom;
            ApplyLight(roomScreen); // Build H
            foreach (var k in atriumKits) if (k.Container.gameObject.activeInHierarchy) ApplyKit(k, true); // Build N: the Atrium shows its stage, turning what it newly restores
            if (roomScreen) { avatar.SetParent(s == SliceScreen.Hub ? hub : s == SliceScreen.WingRoom ? wingRoom : chamber, false); avatar.SetAsLastSibling(); PlaceAvatar(); }
            avatar.gameObject.SetActive(roomScreen);
            MiniMenu?.Refresh(roomScreen); // the mini-menu: rooms only, the instruments keep their own exits (owner, Oct 1)
            if (s == SliceScreen.WingRoom)
            {
                ApplyKit(wingRoomKit, true); // Build M: the room shows the Keys earned, turning what they newly restore
                enterDial.interactable = !busy; wingRoomBack.interactable = !busy;
                enterShelf.gameObject.SetActive(Flow.WheelComplete); enterShelf.interactable = !busy;
                shelfGlow.color = new Color(.95f, .8f, .5f, Flow.WheelComplete && !Dial.Lesson.AllNamed ? .35f : Flow.WheelComplete ? .12f : 0);
                shelfLightLevel = Flow.WheelComplete && !Dial.Lesson.AllNamed ? 1f : Flow.WheelComplete ? .3f : 0; // Build Y: the book waiting glows fully; after it, a faint warmth
                if (shelfLight != null) shelfLight.alpha = shelfLightLevel * (ReducedMotion || shelfLightLevel < 1 ? 1 : .8f);
                enterGrid.gameObject.SetActive(Flow.CanOpenGrid); enterGrid.interactable = !busy;
                journalWing.gameObject.SetActive(Flow.CanOpenJournal); journalWing.interactable = !busy; // Build F
                gridGlow.color = new Color(.95f, .8f, .5f, Flow.ModalitiesComplete && !Grid.Key3Earned ? .35f : Flow.ModalitiesComplete ? .12f : 0); // an instrument with a unit waiting glows, like the shelf
                if (!DialUnitWaiting) dialGlow.color = new Color(.95f, .8f, .5f, 0); else if (ReducedMotion) dialGlow.color = new Color(.95f, .8f, .5f, .55f); // Build I: the Dial glows too; Update breathes it
                wingRoomCaption.text = Flow.Note == "gated" ? SliceFlow.GateLine // Build F: the instrument closed on the third strike; the journal is below
                    : Flow.Note == "shelf-dark" ? DialLesson.ShelfDark
                    : Flow.Note == "grid-dark" ? GridModel.DarkLine
                    : Flow.AtriumStage == 1 ? WingOpeningLine // Build T: the first lesson waits at the Dial
                    : Dial.Lesson.Phase == LessonPhase.GlyphWheel ? "Ah. The Dial has turned sly. It wears only its symbols now, twelve marks with no names beneath them. Shall we find out which of them you truly know?" // owner (APK playtest rewrite, Sept 29; was worksheet, Sept 14 flags)
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
                if (!sunSent && Flow.BirthDone) { Dial.Lesson.SetSunSign(Flow.LessonSun, Flow.HasSunSign); sunSent = true; } // no sun: the lessons start from Aries (Oct 7)
                if (Flow.AtriumStage >= 2) { keyRect.gameObject.SetActive(false); keyGlow.color = new Color(Bone.r, Bone.g, Bone.b, 0); seam.color = new Color(Bone.r, Bone.g, Bone.b, 0); Dial.Ring.localScale = Vector3.one; }
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
            for (int seat = 0; seat < 12; seat++) { int ink = InkOf(seat); bool sinking = revealSeat == seat; bookInk[seat].color = new Color(BookInk.r, BookInk.g, BookInk.b, sinking || ink == 0 ? 0 : ink == 2 ? InkFull : InkFaint); }
            bool chrome = !bookPagesShown; glyphQuestion.gameObject.SetActive(chrome); glyphPanel.gameObject.SetActive(chrome); bookCardRect.gameObject.SetActive(chrome); glyphProgress.gameObject.SetActive(chrome); glyphNote.gameObject.SetActive(chrome);
            foreach (var b in glyphNameButtons) b.gameObject.SetActive(chrome);
            var page = Slots.Image(naming ? "book-page" : "book-cover"); bookCard.sprite = page; bookCard.color = page != null ? Color.white : PanelColor; // Build E
            if (!naming) { glyphProgress.text = ""; glyphCard.text = ""; for (int i = 0; i < 4; i++) glyphNameButtons[i].GetComponentInChildren<Text>().text = ""; glyphCaspar.text = lesson.Message; glyphNote.text = ""; for (int i = 0; i < 4; i++) glyphNameButtons[i].interactable = false; return; } // after the twelfth answer the wheel already owns the index; the last card stays up through the hold
            int shown = revealSeat >= 0 ? revealSeat : target; // during the answer's pause the answered symbol and its plates stay
            if (naming)
            {
                glyphProgress.text = "Symbol " + Mathf.Min(lesson.GlyphIndex + (revealSeat >= 0 ? 0 : 1), 12) + " of 12";
                glyphCard.text = shown >= 0 && shown < 12 ? Zodiac.Seats[shown].Glyph : "";
                var options = target >= 0 && target < 12 ? lesson.GlyphOptions(target) : new int[4];
                for (int i = 0; i < 4; i++) glyphNameButtons[i].GetComponentInChildren<Text>().text = revealSeat >= 0 ? revealNames[i] : target >= 0 ? Zodiac.Seats[options[i]].Name : "";
            }
            for (int i = 0; i < 4; i++)
            {
                glyphNameButtons[i].interactable = naming && !busy && target >= 0;
                var label = glyphNameButtons[i].GetComponentInChildren<Text>(); bool picked = revealSeat >= 0 && revealSlot == i && revealOwn; // the right answer floats a little higher, warm
                var warm = label.GetComponent<Outline>(); if (warm == null) { warm = label.gameObject.AddComponent<Outline>(); warm.effectDistance = new Vector2(1.5f, -1.5f); }
                warm.effectColor = new Color(1, .66f, .3f, picked ? .8f : 0); label.rectTransform.anchoredPosition = new Vector2(0, picked ? 3 : 0);
            }
            glyphCaspar.text = DialLesson.GlyphIntro; glyphNote.text = busy ? glyphNote.text : "";
            if (naming && revealSeat < 0 && target >= 0 && target != shownGlyph && !busy) { shownGlyph = target; if (glyphRising) EndRise(); if (!ReducedMotion && Flow.Screen == SliceScreen.Book && isActiveAndEnabled) StartCoroutine(RiseQuestion()); }
        }
        int inHandCell(int cell) => Grid.Sign >= 0 && Grid.Cell == cell && !Grid.Placed[GridModel.SeatOf(cell)] ? Grid.Sign : -1; // the plate in hand, sitting in the well it was put in, until the Seal
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
                int seat = GridModel.SeatOf(i); bool filled = g.Placed[seat];
                gridCells[i].interactable = active && g.CanChoose(i);
                if (!gridPlates)
                {
                    gridTiles[i].GetComponent<Image>().color = seated ? TileGone : inHand ? Held : Dim;
                    gridTileNames[i].color = gridTileGlyphs[i].color = seated ? new Color(Bone.r, Bone.g, Bone.b, .3f) : Bone;
                    gridCells[i].GetComponent<Image>().color = filled ? Seated : i == g.Cell || i == demoCell ? Held : Dim;
                    gridCellNames[i].text = filled ? Zodiac.Seats[seat].Name : i == g.Rejected ? "×" : "";
                    gridCellGlyphs[i].text = filled ? Zodiac.Seats[seat].Glyph : "";
                    continue;
                }
                // the tray: a placed plate has left it; the one in hand sits in its chosen well, or lifts in place until a well is chosen
                bool inWell = inHand && g.Cell >= 0, gone = seated || inWell, lifted = inHand && !inWell;
                var tile = (RectTransform)gridTiles[i].transform; var tileImage = gridTiles[i].GetComponent<Image>();
                tileImage.color = gone ? Color.clear : Color.white; gridTileNames[i].color = gridTileGlyphs[i].color = gone ? Color.clear : BurnedInk;
                tile.localScale = Vector3.one * (lifted || dragSeat == i ? LiftScale : 1); var shade = tileImage.GetComponent<Shadow>(); if (shade == null) { shade = tileImage.gameObject.AddComponent<Shadow>(); shade.effectDistance = new Vector2(3, -5); }
                shade.effectColor = new Color(0, 0, 0, lifted || dragSeat == i ? .5f : 0); // a lifted plate casts a hard drawn shadow (Art Bible rule 8)
                // the well: its light when a plate is held over it (every empty well alike, owner Oct 7), its plate when filled or chosen
                int pending = inHandCell(i); bool shows = filled || pending >= 0; int sign = filled ? seat : pending;
                gridCells[i].GetComponent<Image>().color = Color.white;
                gridWellLight[i].color = new Color(Bone.r, Bone.g, Bone.b, !filled && (i == hoverCell || i == demoCell || (pending >= 0 && dragSeat < 0)) ? .42f : 0);
                gridCellWood[i].gameObject.SetActive(shows); bool gold = filled && PlacedOwn(seat);
                if (gold && goldSeat != seat) gridGoldMask[i].sizeDelta = new Vector2(78, 42); else if (!gold) gridGoldMask[i].sizeDelta = new Vector2(0, 42);
                bool live = filled; // the lettering comes alive once the sign is placed, on its own or with help (answer 1A)
                gridCellNames[i].text = shows ? Zodiac.Seats[sign].Name : i == g.Rejected ? "×" : ""; gridCellGlyphs[i].text = shows ? Zodiac.Seats[sign].Glyph : "";
                gridCellNames[i].material = null; gridCellGlyphs[i].material = live ? gridCellGlyphLive[i] : null; // the symbol takes its element's material; the name, its element's deep colour, crisp at its size
                foreach (var label in new[] { gridCellNames[i], gridCellGlyphs[i] }) { var line = label.GetComponent<Outline>(); if (line != null) line.enabled = live && label == gridCellGlyphs[i]; }
                gridCellGlyphs[i].color = live && gridCellGlyphLive[i] != null ? Color.white : BurnedInk;
                gridCellNames[i].color = live ? LiveName(seat, gold) : i == g.Rejected && !shows ? Bone : BurnedInk;
                var drift = ReducedMotion ? Vector2.zero : LetterDrift(seat); if (gridCellNameLive[i] != null) { gridCellNameLive[i].SetVector("_Drift", drift); gridCellGlyphLive[i].SetVector("_Drift", drift); }
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
            // The caption is said once: on the first return, while the Atrium is at Stage 2. After that the room shows its own state (owner, Sept 26 playtest, note 9).
            hubCaption.text = stage == 2 ? "Stirring: one lamp lit, one desk uncovered, the Zodiac Wing open." : ""; // owner (worksheet section 1, kept as written, marked X)
            hubText.text = stage == 1 ? HubOpeningLine : Flow.KeysInHand > 1 ? string.Format(HubKeysInHandLine, Flow.KeysInHand) : Flow.KeysInHand == 1 ? HubKeyInHandLine : Flow.WingWhole ? HubWholeLine : Flow.LocksFilled >= 3 ? HubSpent3Line : Flow.LocksFilled >= 2 ? HubSpent2Line : Flow.V02Complete ? HubCompleteLine : Resumed || Flow.Sittings > 0 ? HubLaterLine : HubFirstLine;
            enterChamber.interactable = !busy && Flow.CanEnterChamber; enterChamber.gameObject.SetActive(false); // the doors take the tap (86bcex5kc 1A); Build T: the sealed door itself answers a tap
            { var wingRect = (RectTransform)enterWing.transform; wingRect.anchoredPosition = new Vector2(stage >= 2 ? -78 : 0, wingRect.anchoredPosition.y); } // Build U: alone on the opening walk, the Zodiac Wing button sits centred
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
            SetPose(atriumPose, AtriumPoses[Mathf.Min(Page, AtriumPoses.Length - 1)]); SetPose(returnPose, ReturnPoses[Mathf.Min(Page, ReturnPoses.Length - 1)]); // Build P
            ChamberPose(Flow.Screen == SliceScreen.Chamber && !Flow.KeyInserted ? ChamberPoses[Mathf.Min(Page, ChamberPoses.Length - 1)] : null); // Build R: the introduction only; the room and the Key's spectacle stay in view
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
        void PaintCandle(Color color, float alpha) { if (candle != null) Slots.Paint(candle, color, alpha); } // Build Z: the Dial screen's candle was cut
        // ---- Build Z (owner, Sept 30): the Wing room's Dial rests with its eye closed, worn and restored alike, and opens it when the player taps the Dial.
        // The open art is identical to the closed piece outside the eye, so the opening is a mask at the eye that grows from the seam: the lids part.
        readonly List<(RectTransform box, float height)> dialEyes = new List<(RectTransform, float)>(); float dialEyeOpen;
        public const float WornEyeWidth = .42f, WornEyeHeight = .14f; // the worn eye's opening, as shares of the file: the eye spans .37 by .11 of kit-dial-worn-open (x .31 to .69, y .38 to .49), with a margin
        CanvasGroup wornEyeAbove, wornEyeOf; RectTransform wornEyeBox; // the worn Dial's eye, drawn above the layers over it (owner's Oct 3 playtest, 86bcex5kc)
        public const float DialEyeSeconds = .35f; public const float DialEyeHold = .25f; SliceScreen shownScreen = SliceScreen.Identity; // Build AB: the open eye's beat; the screen last shown
        void BuildDialEyes()
        {
            dialEyes.Clear(); wornEyeBox = null; wornEyeAbove = wornEyeOf = null; var piece = wingKit.FirstOrDefault(p => p.P.Name == "dial"); if (piece == null) return;
            AddDialEye(piece.Worn, "kit-dial-worn-open", .428f); AddDialEye(piece.Restored, "kit-dial-restored-open", .404f);
            // The owner's Oct 3 playtest (86bcex5kc): at Key 1 today's Dial sits at half over the worn one (the wake-up, 2b B), and their eyes
            // sit 8 px apart (the files' canvases differ), so the worn eye opened under today's half-closed lids and read as shut. The worn
            // eye's opening is drawn just above the layers over it, at the worn layer's own strength and only over the eye, so the eye reads
            // open at every Key and the look around it is unchanged.
            if (piece.Worn != null && wornEyeBox != null)
            {
                var worn = (RectTransform)piece.Worn.transform; var above = new GameObject("Worn eye (above)", typeof(RectTransform)).GetComponent<RectTransform>(); above.SetParent(worn.parent, false);
                above.anchorMin = worn.anchorMin; above.anchorMax = worn.anchorMax; above.pivot = worn.pivot; above.anchoredPosition = worn.anchoredPosition; above.sizeDelta = worn.sizeDelta;
                int top = new[] { piece.Worn, piece.Restored, piece.Bright }.Where(g => g != null).Max(g => g.transform.GetSiblingIndex());
                above.SetSiblingIndex(top + 1); wornEyeBox.SetParent(above, false); wornEyeBox.sizeDelta = new Vector2(worn.sizeDelta.x * WornEyeWidth, 0); wornEyeBox.GetComponent<RectMask2D>().softness = new Vector2Int(6, 4); wornEyeAbove = above.gameObject.AddComponent<CanvasGroup>(); wornEyeAbove.blocksRaycasts = false; wornEyeOf = piece.Worn; // the eye only, soft at its edges (Jeffrey, #131 B1)
            } // the eye's centre, as a share of the file's height (measured on the Cast Dial pieces, Build AC)
            if (piece.Bright != null) AddDialEye(piece.Bright, "kit-dial-bright-open", .404f); // the wake-up's bright dial opens its eye too (its file: kit-dial-restored's canvas)
            SetDialEye(0);
        }
        void AddDialEye(CanvasGroup state, string slot, float eyeAt)
        {
            if (state == null || Slots.Image(slot) == null) return;
            var piece = (RectTransform)state.transform; float w = piece.sizeDelta.x, h = piece.sizeDelta.y, y = h / 2 - eyeAt * h;
            var box = new GameObject("Eye opening", typeof(RectTransform)).GetComponent<RectTransform>(); box.SetParent(piece, false);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(.5f, .5f); box.anchoredPosition = new Vector2(0, y); box.sizeDelta = new Vector2(w, 0); box.gameObject.AddComponent<RectMask2D>();
            var open = new GameObject("Open", typeof(RectTransform)).GetComponent<RectTransform>(); open.SetParent(box, false);
            open.anchorMin = open.anchorMax = open.pivot = new Vector2(.5f, .5f); open.anchoredPosition = new Vector2(0, -y); open.sizeDelta = new Vector2(w, h);
            var image = open.gameObject.AddComponent<Image>(); image.raycastTarget = false; Slots.Dress(image, slot);
            dialEyes.Add((box, h)); if (slot == "kit-dial-worn-open") wornEyeBox = box;
        }
        void SetDialEye(float k)
        {
            dialEyeOpen = k; if (wornEyeAbove != null) wornEyeAbove.alpha = wornEyeOf.alpha; // it follows the worn layer: full at the first visit and Key 1, gone from Key 2
            foreach (var (box, h) in dialEyes) box.sizeDelta = new Vector2(box.sizeDelta.x, box == wornEyeBox ? Mathf.Lerp(0, h * WornEyeHeight, Mathf.Min(k, 1)) : k >= 1 ? h * 2 : Mathf.Lerp(0, h * .2f, k)); // the worn eye stays the eye's band, so the half-and-half look around it holds
        }
        IEnumerator OpenDialEye()
        {
            if (dialEyes.Count == 0) yield break;
            if (ReducedMotion) { SetDialEye(1); Publish(); yield break; }
            for (float t = 0; t < DialEyeSeconds; t += Time.unscaledDeltaTime) { SetDialEye(Mathf.SmoothStep(0, 1, t / DialEyeSeconds) * .999f); yield return null; }
            SetDialEye(1); Publish();
        }
        void LightWing() { FloorLight(.8f); PaintCandle(LampLit, 1f); }
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
                s == SliceScreen.Birth ? (Flow.Amending ? SliceFlow.BirthPageTitle + "." : SliceFlow.BirthQuestion) + BirthAsk() :
                s == SliceScreen.Atrium ? atriumText.text : s == SliceScreen.AtriumReturn ? returnText.text :
                s == SliceScreen.Chamber ? chamberText.text + (Flow.Ended ? " " + chamberEnd.text : "") :
                s == SliceScreen.Hub ? hubText.text + " " + hubCaption.text :
                s == SliceScreen.Practice && !practiceOnDial ? (Flow.PracticeDone ? Flow.PracticeSummary + " " + reviewNote.text : reviewProgress.text + ". " + reviewQuestion.text + " " + reviewNote.text) :
                s == SliceScreen.Journal ? JournalSpoken() : "";
            state.casparPose = s == SliceScreen.Atrium && atriumPose.enabled ? AtriumPoses[Mathf.Min(Page, AtriumPoses.Length - 1)] : s == SliceScreen.AtriumReturn && returnPose.enabled ? ReturnPoses[Mathf.Min(Page, ReturnPoses.Length - 1)] : s == SliceScreen.Chamber && chamberPose.enabled ? chamberPoseName : ""; // Build P, Build R
            state.keyRevealed = Flow.KeyRevealed; state.keyInserted = Flow.KeyInserted; state.ended = Flow.Ended; state.locksFilled = Flow.LocksFilled;
            state.sunSign = Flow.HasSunSign ? Zodiac.Seats[Flow.SunSign].Name : "";
            state.canInsert = (s == SliceScreen.Chamber && !Flow.KeyInserted && Page >= ChamberPages.Length - 1 && !busy) || (s == SliceScreen.ChamberRoom && Flow.CanSpend && Flow.Walk.At == "books" && !busy);
            state.canSliceContinue = !busy && ((s == SliceScreen.Atrium && Page < AtriumPages.Length - 1) || (s == SliceScreen.AtriumReturn && Page < ReturnPages.Length - 1) || (s == SliceScreen.Chamber && !Flow.Ended && Page < ChamberPages.Length - 1) || ((s != SliceScreen.Chamber || Flow.Ended) && s != SliceScreen.Wing && Flow.CanContinue) || (s == SliceScreen.Wing && Flow.AtriumStage == 1 && Flow.CanContinue));
            state.canName = s == SliceScreen.Identity;
            state.canBirth = s == SliceScreen.Birth && !busy && Flow.BirthChoice == "";
            bool birthNow = s == SliceScreen.Birth && !busy; string birthStep = Flow.BirthStep; state.birthStep = s == SliceScreen.Birth ? birthStep : "";
            state.canBirthDate = birthNow && Flow.BirthChoice == "chart" && birthStep == "date"; state.canBirthTime = birthNow && Flow.BirthChoice == "chart" && birthStep == "time"; state.canBirthPlace = birthNow && Flow.BirthChoice == "chart" && birthStep == "place";
            state.placeMatches = state.canBirthPlace ? placeMatches.Select(p => p.Name).ToArray() : new string[0];
            state.canSignPick = birthNow && (birthStep == "sun-pick" || birthStep == "moon-pick" || birthStep == "rising-pick"); state.signsAsk = state.canSignPick ? signsLabel.text : ""; state.canSignUnknown = false; // Oct 7 (86bced0tc): no "I'm not sure" // Oct 7: the skip path's sun, or the rising from "Your Birth"
            state.canPlaceUnknown = state.canBirthPlace; state.canBirthCancel = birthNow && Flow.CanCancelAmending; state.amending = Flow.Amending; state.birthHeading = s == SliceScreen.Birth ? birthHeading.text : "";
            state.birthChoices = s == SliceScreen.Birth && Flow.BirthChoice == "" ? new[] { SliceFlow.KnowBirthday, SliceFlow.SkipBirthday } : new string[0];
            state.canMoon = birthNow && birthStep == "moon"; state.moonOptions = state.canMoon ? Flow.MoonAsk.Select(m => Zodiac.Seats[m].Name).ToArray() : new string[0]; state.moonQuestion = s == SliceScreen.Birth ? Flow.MoonQuestion : ""; state.moonBasis = Flow.FromChoice("moon") ? "picked" : Flow.ChoiceFor("moon") != null ? Flow.ChoiceFor("moon").how : "";
            state.canCusp = birthNow && birthStep == "cusp"; state.cuspSigns = Flow.CuspFrom >= 0 ? new[] { Zodiac.Seats[Flow.CuspFrom].Name, Zodiac.Seats[Flow.CuspTo].Name } : new string[0];
            state.cuspTime = Flow.CuspMinute >= 0 ? SliceFlow.ClockTime(Flow.CuspMinute) : ""; state.cuspQuestion = s == SliceScreen.Birth ? Flow.CuspQuestion : ""; state.cuspWhy = state.canCusp && cuspWhyOpen; state.sunBasis = Flow.SunBasis;
            state.moonSign = Flow.MoonSign >= 0 ? Zodiac.Seats[Flow.MoonSign].Name : ""; state.risingSign = Flow.RisingSign >= 0 ? Zodiac.Seats[Flow.RisingSign].Name : ""; state.bigThree = Flow.BirthDone ? Flow.BigThreeLine : "";
            state.canChangeBirth = s == SliceScreen.Birth && !busy && Flow.BirthChoice != "" && !Flow.Amending;
            // the record's three parts, kept apart (Oct 7), and the hidden flag: test evidence only, never drawn
            state.risingChosen = Flow.RisingChosen; state.sunKnown = Flow.HasSunSign; state.openingPath = Flow.OpeningPath; state.saveVersion = SaveData.Current;
            state.lessonSun = Dial != null && Dial.Lesson != null ? Zodiac.Seats[Zodiac.Wrap(Dial.Lesson.Sun)].Name + (Dial.Lesson.SunKnown ? "" : " (starting sign)") : "";
            state.birthRecord = Flow.BirthDone ? JsonUtility.ToJson(new BirthRecordState { birth = Flow.Facts, choices = Flow.Choices.ToArray(), chart = Flow.Chart }) : "";
            // v0.2
            state.atriumStage = Flow.AtriumStage; state.dueCount = Flow.DueCount; state.resumed = Resumed;
            state.canEnterWing = s == SliceScreen.Hub && !busy;
            state.canLeaveDial = s == SliceScreen.Wing && (Flow.AtriumStage >= 2 || !Flow.KeyRevealed) && !busy && !Dial.Busy; // the same rule the button follows, read now rather than from last frame's button // Build T: the Dial's own exit, shown at all times; canLeaveWing is the room's
            state.settingsOpen = Settings != null && Settings.Open; state.canQuit = SettingsMenu.CanQuit; // Build U
            state.safeTop = SafeArea.TopInset(canvas); // Platform fit, Part 1: how far the top row moves down, in layout units
            state.masters = Bleed.Masters(root, Dial != null ? Dial.Root : null); // batch 2: the layers on screen drawing a master whole
            state.buttons = ButtonLook.OnScreen(root, Dial != null && Dial.Showing ? Dial.Root : null, Settings != null ? Settings.Gear : null); // batch 2: each button's look and target
            state.wakePreview = wakePreview; // the Dial's wake-up: DEV Mode's preview (-1: as earned)
            if (MiniMenu != null) { state.travelShown = MiniMenu.ButtonShown; state.travelOpen = MiniMenu.Open; state.travelRows = MiniMenu.Rows; var t = MiniMenu.ButtonAt; state.travelAt = new[] { t.x, t.y }; } // the room mini-menu (86bca07wv); Part 2: where its button sits
            if (Settings != null) { var g = Settings.GearAt; state.gearAt = new[] { g.x, g.y }; } // Part 2: where the gear sits
            state.jumpsShown = Settings != null && Settings.JumpsShown; // Build W
            { var fit = s == SliceScreen.Atrium ? atriumFit : s == SliceScreen.AtriumReturn ? returnFit : s == SliceScreen.Hub ? hubFit : s == SliceScreen.Chamber || s == SliceScreen.ChamberRoom ? chamberFit : null; state.chatBoxHeight = fit != null && fit.isActiveAndEnabled ? fit.Height : 0; } // Build X
            // Build F: the fork, practice, the gate, the journal
            state.practicing = s == SliceScreen.Practice; state.canLeavePractice = state.practicing && !busy;
            state.practiceMode = !state.practicing ? "" : Flow.PracticeDone ? "done" : task != null && (task.Mode == ReviewMode.Dial || task.Mode == ReviewMode.DialModality) ? "dial" : task != null && task.Mode == ReviewMode.TapModality ? "modality" : "tap";
            state.practiceIndex = Flow.ReviewIndex; state.practiceCount = Flow.ReviewQueue.Count; state.practiceSign = task != null && !Flow.PracticeDone ? Zodiac.Seats[task.seat].Name : "";
            state.practiceSummary = Flow.PracticeSummary; state.strikes = Flow.Strikes; state.sitting = Flow.Sittings; state.gated = Flow.Note == "gated";
            bool forkOpen = s == SliceScreen.Wing && forkShown && Flow.CanEnterPractice && !busy && !Dial.Busy;
            state.canEnterPractice = forkOpen; state.canContinueLesson = forkOpen && LessonAvailable;
            state.fork = !forkOpen ? "none" : LessonAvailable ? "both" : "practice";
            state.journal = s == SliceScreen.Journal; state.canOpenJournal = Flow.CanOpenJournal && !busy; state.canCloseJournal = state.journal && !busy;
            state.canJournalNext = Flow.CanJournalNext && !busy; state.canJournalPrev = Flow.CanJournalPrev && !busy; state.canJournalWheel = Flow.CanJournalWheel && !busy;
            // Build AA: which view, the tabs, each seat's state and ribbon, the preview, a sign page's facts and links, its Illumination and ribbon; the text on screen as evidence
            var at = Flow.JournalAt; bool onSign = state.journal && at == JournalView.Sign, onWheel = state.journal && at == JournalView.Wheel; int signSeat = Flow.JournalSignSeat;
            state.journalView = !state.journal ? "" : onSign ? "sign" : onWheel ? (Flow.JournalTableView ? "table" : "wheel") : at.ToString().ToLowerInvariant(); // batch 2: title, landing, contents, map
            bool front = state.journal && (at == JournalView.Title || at == JournalView.Landing);
            state.journalKeeper = front ? new[] { journalKeeperHeading.text, journalKeys.text }.Concat(bigThreeLine.gameObject.activeSelf ? new[] { string.Concat(Enumerable.Range(0, 3).Select(i => bigThreeGlyphs[i].text + bigThreeWords[i].text)) } : new string[0]).Concat(inscriptionLines).ToArray() : new string[0];
            state.inscriptionId = Flow.InscriptionId; state.inscriptionGroup = Flow.InscriptionGroup; state.inscriptionWriting = inscriptionWriting != null; state.inscriptionRecent = Flow.RecentInscriptions.ToArray(); // the living inscription (Oct 7)
            state.canJournalContents = Flow.CanJournalContents && at != JournalView.Title && !busy; state.canJournalHome = Flow.CanJournalHome && !busy; state.canJournalPractice = Flow.CanJournalPractice && !busy && journalBeat == null;
            state.journalDoors = state.journal && at == JournalView.Landing ? new[] { journalPracticeDoor, journalContentsDoor }.SelectMany(b => { var r = (RectTransform)b.transform; return new[] { r.anchoredPosition.x, -r.anchoredPosition.y, r.sizeDelta.x, r.sizeDelta.y }; }).ToArray() : new float[0];
            state.journalChapters = state.journal && at == JournalView.Contents ? new[] { SliceFlow.WheelTitle, SliceFlow.MapTitle }.Concat(Enumerable.Repeat(SliceFlow.SealedChapter, SliceFlow.SealedChapters)).ToArray() : new string[0];
            state.journalMapRooms = state.journal && at == JournalView.Map ? Enumerable.Range(0, journalMapNames.Length).Where(i => journalMapNames[i].gameObject.activeSelf).Select(i => TravelMenu.Rooms[i].name).ToArray() : new string[0];
            state.journalLink = state.journal ? (at == JournalView.Contents || at == JournalView.Practice || at == JournalView.Birth ? SliceFlow.BackToJournal : at == JournalView.Wheel || at == JournalView.Map ? SliceFlow.BackToContents : at == JournalView.Quiz ? SliceFlow.BackToPractice : "") : "";
            state.journalScriptFont = journalItalic != null ? journalItalic.name : "";
            // Oct 7, "Your Birth" as drawn: the rows, the lines under them, the buttons, and the Big Three's target on the landing
            bool onBirth = state.journal && at == JournalView.Birth;
            state.canBirthPage = Flow.CanOpenBirthPage && !busy && journalBeat == null; state.canBirthAdd = Flow.CanAddFacts && !busy; state.canBirthAddTime = Flow.CanAddTime && !busy; state.canBirthRising = Flow.CanChooseRising && !busy;
            state.journalBigThreeBox = state.journal && at == JournalView.Landing && journalBigThree.gameObject.activeSelf ? PageBox(journalBigThree) : new float[0];
            state.journalBirthRows = onBirth ? Enumerable.Range(0, 3).Select(i => birthRowNames[i].text + ": " + birthRowValues[i].text).ToArray() : new string[0];
            state.journalBirthLines = onBirth ? birthPageLines.Where(t => t.gameObject.activeSelf).Select(t => t.text).ToArray() : new string[0];
            state.birthRisingWords = onBirth && birthRisingButton.gameObject.activeSelf ? birthRisingButton.GetComponent<ButtonFeel>().Label.text : "";
            state.journalBirthBoxes = onBirth ? birthAddButtons.Concat(new[] { birthRisingButton, birthTimeButton }).SelectMany(b => b.gameObject.activeSelf ? PageBox(b) : new float[4]).ToArray() : new float[0];
            // Oct 3, Practice as drawn: the concepts listed, the question, the choices in the order shown, the pick and the right one, the why, the round's end, and the targets
            bool onPractice = state.journal && at == JournalView.Practice, onQuiz = state.journal && at == JournalView.Quiz && Flow.OnQuiz;
            state.journalPractice = onPractice ? Flow.PracticeConcepts.Select(c => c.name).ToArray() : new string[0];
            state.journalPracticeRows = onPractice ? practiceRows.Where(b => b.gameObject.activeSelf).SelectMany(b => PageBox(b)).ToArray() : new float[0];
            state.quizConcept = onQuiz ? Flow.QuizConcept.id : ""; state.quizCounter = onQuiz ? quizCounter.text : ""; state.quizGlyph = onQuiz && quizGlyph.gameObject.activeSelf ? quizGlyph.text : "";
            state.quizAsk = onQuiz ? string.Join(" ", quizAsk.Where(t => t.gameObject.activeSelf).Select(t => t.text)) : ""; state.quizAskLines = onQuiz ? quizAsk.Count(t => t.gameObject.activeSelf) : 0;
            state.quizChoices = onQuiz ? Flow.QuizChoices : new string[0]; state.quizChoiceBoxes = onQuiz ? quizChoices.Where(b => b.gameObject.activeSelf).SelectMany(b => PageBox(b)).ToArray() : new float[0];
            state.quizPicked = onQuiz ? Flow.QuizPicked : -1; state.quizRight = onQuiz ? Flow.QuizRight : -1; state.quizOver = onQuiz && Flow.QuizOver;
            state.quizFeedback = onQuiz ? string.Join(" ", quizFeedback.Where(t => t.gameObject.activeSelf).Select(t => t.text)) : ""; state.quizEnd = onQuiz ? string.Join(" ", quizEnd.Where(t => t.gameObject.activeSelf).Select(t => t.text)) : "";
            state.quizButtons = onQuiz ? new[] { quizNext, quizBack }.SelectMany(b => b.gameObject.activeSelf ? PageBox(b) : new float[4]).ToArray() : new float[0];
            state.canQuizChoice = onQuiz && Flow.CanQuizAnswer && !busy; state.canQuizNext = onQuiz && Flow.CanQuizNext && !busy; state.canQuizBack = onQuiz && Flow.CanQuizBack && !busy;
            state.journalPage = Flow.JournalSign; state.journalCount = Flow.JournalSigns.Count;
            state.journalLenses = onWheel && Flow.JournalLenses.Count > 1 ? Flow.JournalLenses.Select(SliceFlow.LensTitle).ToArray() : new string[0]; state.journalLens = state.journal ? SliceFlow.LensTitle(Flow.Lens) : ""; state.canJournalTable = onWheel && Flow.CanJournalTable;
            state.journalSeats = state.journal ? Enumerable.Range(0, 12).Select(seat => SliceFlow.SeatStateNames[Flow.SeatState(seat)]).ToArray() : new string[0];
            state.journalDue = state.journal ? Enumerable.Range(0, 12).Select(seat => Flow.SeatState(seat) > 0 && Flow.SignDue(seat)).ToArray() : new bool[0];
            state.journalSelected = state.journal && Flow.JournalSelected >= 0 ? Zodiac.Seats[Flow.JournalSelected].Name : ""; state.canJournalOpen = onWheel && Flow.CanOpenSelected && !busy;
            state.journalPreview = onWheel && Flow.JournalSelected >= 0 ? PlainText(journalCardTitle.text) + ": " + PlainText(journalCardLine.text) : "";
            state.journalChips = onSign ? Enumerable.Range(0, journalChips.Length).Where(i => journalChips[i].gameObject.activeSelf).Select(i => Zodiac.Seats[journalChipSeats[i]].Name).ToArray() : new string[0];
            state.journalChipBoxes = onSign ? JournalChipBoxes() : new float[0];
            state.journalSign = onSign ? Zodiac.Seats[signSeat].Name : ""; state.journalFacts = onSign ? Flow.SignFacts(signSeat).Select(f => f[0] + ": " + f[1]).ToArray() : new string[0];
            state.journalGlyph = onSign && Flow.SignKnows(signSeat, ItemKind.Glyph) ? Zodiac.Seats[signSeat].Glyph : ""; state.journalSeatState = onSign ? SliceFlow.SeatStateNames[Flow.SeatState(signSeat)] : "";
            state.journalInk = onSign ? Flow.SignInk(signSeat) : 0; state.journalColour = onSign ? Flow.SignColour(signSeat) : 0; state.journalGilt = onSign && Flow.SignGilt(signSeat);
            state.journalRibbon = onSign ? Flow.SignLadder(signSeat) : 0; state.journalRibbonOut = onSign && Flow.SignDue(signSeat);
            state.journalArt = onSign ? Slots.Source(SignSlot(signSeat)) : ""; state.journalShader = journalIllumination != null; state.journalTitleFont = journalTitle != null && journalTitle.font != null ? journalTitle.font.name : "";
            state.journalText = state.journal ? string.Join(" | ", journal.GetComponentsInChildren<Text>(false).Select(t => PlainText(t.text)).Where(t => t != "")) : "";
            state.hubNote = hubNote != null ? hubNote.text : ""; state.v02Complete = Flow.V02Complete;
            bool partA = s == SliceScreen.Book && Dial.Lesson.Phase == LessonPhase.GlyphNames;
            bool glyphItem = s == SliceScreen.Practice && task != null && !Flow.PracticeDone && task.Mode == ReviewMode.Glyph;
            state.glyphMode = partA ? "name" : glyphItem ? "review" : "";
            if (partA || glyphItem)
            {
                int target = partA ? Dial.Lesson.CurrentGlyph : task.seat;
                var opts = partA ? Dial.Lesson.GlyphOptions(target) : Flow.GlyphReviewOptions(target);
                state.glyphChar = Zodiac.Seats[target].Glyph; state.glyphOptions = new[] { Zodiac.Seats[opts[0]].Name, Zodiac.Seats[opts[1]].Name, Zodiac.Seats[opts[2]].Name, Zodiac.Seats[opts[3]].Name };
            }
            {
                state.bookEmblem = glyphEmblem != null && glyphEmblem.gameObject.activeInHierarchy && glyphEmblem.sprite != null ? glyphEmblem.sprite.name : ""; state.bookPages = bookPagesShown; state.bookRising = glyphRising;
                state.bookInk = Enumerable.Range(0, 12).Select(InkOf).ToArray(); state.bookInkDrawn = bookInk.Count(t => t != null && t.isActiveAndEnabled && t.color.a > 0 && t.cachedTextGenerator.characterCountVisible > 0); state.bookArt = Slots.Image("book-room") != null; state.bookPicked = revealSeat >= 0 && revealOwn && revealSlot >= 0 ? revealNames[revealSlot] : ""; // the ink: inked and really laid out (a cut-off line draws nothing)
                state.bookInkBox = bookInk.SelectMany(InkBox).ToArray();
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
            state.dialEye = dialEyes.Count > 0 ? dialEyeOpen : -1; // Build Z: the Wing room Dial's eye, -1 without the open art
            state.shelfLight = shelfLight != null ? shelfLightLevel : -1; state.shelfRing = shelfGlow != null && shelfGlow.gameObject.activeInHierarchy && shelfGlow.color.a > 0; // Build Y
            state.shelfEdge = shelfEdge; // APK Session 2, bug 1: "halo" when the shelf's edge comes from its silhouette
            state.kitLevel = KitLevel; state.kitPieces = wingKit.Count; state.kitRestored = KitRestored; state.grime = wingGrime != null && wingGrime.gameObject.activeSelf ? wingGrime.color.a : -1; state.wingLight = wingLight != null && wingLight.gameObject.activeSelf ? wingLight.color.a : -1; // Build M
            state.kitUp = wingKit.Where(p => p.Shown).Select(p => p.P.Name).Distinct().ToArray();
            if (chamberKit != null) { state.chamberKitLevel = chamberKit.Shown; state.chamberKitRestored = chamberKit.Pieces.Count(p => p.Shown); state.chamberKitPieces = chamberKit.Pieces.Count; } // Build O
            if (hubKit != null) { state.atriumKitLevel = hubKit.Shown; state.atriumKitPieces = hubKit.Pieces.Count; state.atriumKitRestored = hubKit.Pieces.Count(p => p.Shown); state.atriumGrime = hubKit.GrimeImage.gameObject.activeSelf ? hubKit.GrimeImage.color.a : -1; state.doors = hubKit.Doors.Select(d => d.Id + ":" + (d.Shown ? "unlocked" : "locked")).ToArray(); state.lastDoorOpened = LastDoorOpened; } // Build N
            if (s == SliceScreen.Grid)
            {
                state.caspar = Grid.Message; state.gridReadout = Grid.Readout; state.gridStatus = gridStatus.text;
                state.gridSign = Grid.Sign >= 0 ? Zodiac.Seats[Grid.Sign].Name : ""; state.gridCell = Grid.Cell; state.gridLocked = Grid.Locked;
                state.gridTiles = Enumerable.Range(0, 12).Select(i => Grid.TileLabel(i)).ToArray(); state.gridCells = Enumerable.Range(0, 12).Select(i => Grid.CellLabel(i)).ToArray();
                state.gridArt = gridPlates; state.gridDrag = dragSeat >= 0 ? Zodiac.Seats[dragSeat].Name : ""; state.gridHover = hoverCell;
                state.gridPlates = Enumerable.Range(0, 12).Select(i => { int seat = GridModel.SeatOf(i); return Grid.Placed[seat] ? (PlacedOwn(seat) ? "gold" : "wood") : inHandCell(i) >= 0 ? "pending" : ""; }).ToArray();
                state.gridLive = Enumerable.Range(0, 12).Select(i => gridPlates && gridCellGlyphLive[i] != null && gridCellGlyphs[i].material == gridCellGlyphLive[i]).ToArray();
                state.canGridPick = Grid.Active && !busy; state.canGridSeal = Grid.CanSeal && !busy; state.canGridAsk = Grid.CanAsk && !busy; state.canLeaveGrid = !busy;
            }
        }

        // ---- Build H: the light overlays. A file in a room's light slot is drawn over the background and under everything else; without one nothing exists. ----
        Image LightOverlay(RectTransform panel, string slot)
        {
            var r = Rect("Light overlay", panel, 0, 400, 360, 800); var image = r.gameObject.AddComponent<Image>(); image.raycastTarget = false; Bleed.Add(image, root); // Part 2: the light reaches the edges with its room
            bool file = Slots.Dress(image, slot); image.gameObject.SetActive(file); image.color = new Color(1, 1, 1, 0); r.SetAsFirstSibling();
            lightOverlays.Add(image); return image;
        }
        void ApplyLight(bool animate)
        {
            float target = LightAlphaFor(Flow.AtriumStage, Flow.LocksFilled);
            if (Mathf.Approximately(target, lightAlpha)) return;
            if (lightFade != null) { StopCoroutine(lightFade); lightFade = null; }
            if (!animate || ReducedMotion) { SetLight(target); return; }
            lightFade = StartCoroutine(FadeLight(target));
        }
        void SetLight(float alpha) { lightAlpha = alpha; foreach (var overlay in lightOverlays) overlay.color = new Color(1, 1, 1, alpha); }
        IEnumerator FadeLight(float target) { float from = lightAlpha; yield return Tween(.8f, k => SetLight(Mathf.Lerp(from, target, k))); lightFade = null; Publish(); } // the settled alpha reaches the web state

        // ---- Build F: the journal. In the inventory (owner, Sept 15): a button in every room, never a room object. It renders straight from the
        // deck and reading changes nothing. Build AA (owner, Sept 30; Sept 26 notes 7 and 14): one index page, the Wheel, with a Wheel / Table
        // switch and tabs that appear as each pattern is learned; tap a seat for its preview, then its page; a sign's page links the signs
        // that share each fact. The look is the Black Hours, silver-ruled: black-blue vellum in gold and silver (painterly, the owner's
        // journal-only exception to the Art Bible). Gold means mastery: a sign met is a silver ring, one practised a gold ring, a mastered one
        // gold leaf (Illumination, re-dressed); Ribbons still carry the ladder and what is due. A part with no file keeps the greybox look. ----
        public const float JournalTop = 26f; // the page's head edge on the 360 x 800 layout: a sign's ribbon hangs from it; a due ribbon stands out above it
        public const float JournalX = 7f;    // the page's calm column (the art's inner border, x 50 to 325, centred at 187.5)
        const float JournalTitleClearance = 36; // Platform fit, Part 1: the journal's title sits inside the book, its glyphs about 40 px below the frame's top; it moves only once the safe band reaches them
        public const float JournalWidth = 266f;
        public const float SignPictureTop = 150f, SignPictureSize = 112f; // a sign's picture on its page (the fixture reads Illumination off the screen here)
        public const float WheelTop = 296f, WheelSize = 270f; // the wheel art, 600 px across: sockets on radius 216 (socket 1 at 9 o'clock), 92 across
        public const float SeatRadius = WheelSize * 216f / 600f, SocketSize = WheelSize * 92f / 600f, SeatHit = 46f;
        public const float RuleFirst = 96f, RuleGap = 26f, RuleLast = 590f; // the silver rules the game draws (the art has none), under the text
        public const float TableTop = 152f, TableRowTop = 202f, TableRowGap = 66f, TableColGap = 70f, TableSeat = 44f;
        public const float CardTop = 498f, CardHeight = 92f, JournalRowY = 654f, JournalCloseY = 714f;
        static readonly Color Silver = new Color(.79f, .81f, .86f), VellumPanel = new Color(.04f, .05f, .09f, .74f), PageText = new Color(.94f, .9f, .82f), PageFaintText = new Color(.84f, .85f, .91f, .62f), Vermilion = new Color(.89f, .38f, .25f);
        public static float TableColumnX(int column) => JournalX + 30 + (column - 1) * TableColGap;
        public static float TableRowY(int row) => TableRowTop + row * TableRowGap;
        // Where a seat sits: on the wheel, or in its Table cell (element row, modality column). The web template's boxes match.
        public static Vector2 SeatCentre(int seat, bool table)
        {
            if (table) return new Vector2(TableColumnX(seat % 3), TableRowY(seat % 4));
            float a = (180 + seat * 30) * Mathf.Deg2Rad; return new Vector2(JournalX + Mathf.Cos(a) * SeatRadius, WheelTop - Mathf.Sin(a) * SeatRadius);
        }
        public static float LensX(int index, int count) => JournalX + (index - (count - 1) / 2f) * 66f;
        public const string TitleFont = "Fonts/UnifrakturMaguntia"; public const int TitleSize = 30, SignTitleSize = 34, CapitalSize = 52; // the page titles in blackletter, the Library's hand (owner, Sept 26); the facts stay plain
        public static string SignTitle(string name, Color capital) => "<size=" + CapitalSize + "><color=#" + ColorUtility.ToHtmlStringRGB(capital) + ">" + name.Substring(0, 1) + "</color></size>" + name.Substring(1); // a sign's name with its illuminated capital
        static string SignSlot(int seat) => "sign-" + Zodiac.Seats[Zodiac.Wrap(seat)].Name.ToLowerInvariant();
        static Color ElementColour(int seat) { var hex = FitBox.Elements[Zodiac.Wrap(seat) % 4].hex; return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Gilt; } // Build S's colours (the owner's default for the journal)
        static readonly Color[] ModalityColours = { new Color(.89f, .64f, .29f), new Color(.79f, .81f, .86f), new Color(.89f, .55f, .45f) }; // Cardinal gold, Fixed silver, Mutable faded vermilion (working choice)
        // A disc, made once (no file): the round mask a seat's picture is clipped to, and the badge behind its symbol.
        static Sprite disc;
        static Sprite Disc()
        {
            if (disc != null) return disc;
            const int size = 96; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) - size / 2f, dy = (y + .5f) - size / 2f, d = Mathf.Sqrt(dx * dx + dy * dy);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(size / 2f - d)));
            }
            texture.Apply(); disc = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f)); return disc;
        }
        // One seat on the index: a hit area, the picture clipped round (its own Illumination material), the state ring, the frame when tapped, the symbol's badge, a ribbon when due.
        class JournalSeatParts { public Button hit; public RectTransform root; public CanvasGroup group; public Image picture, ring, frame, badge, ribbon; public Text glyph; public Material illumination; }
        readonly JournalSeatParts[] journalSeats = new JournalSeatParts[12];
        // A sign's picture clipped round by its own Illumination material (_Round), so the deck's ink and colour reach the screen
        Image MaskedPicture(Transform parent, float x, float top, float size, out Material material)
        {
            var round = Rect("Round", parent, x, top, size, size);
            var picture = Rect("Picture", round, 0, size / 2, size, size).gameObject.AddComponent<Image>(); picture.raycastTarget = false; picture.preserveAspect = true;
            material = null; var shader = Resources.Load<Shader>("Shaders/Illumination"); if (shader != null) { material = new Material(shader); material.SetFloat("_Round", 1); picture.material = material; }
            return picture;
        }
        // A thin border drawn as four edges (Outline copies the whole quad, which shows through a see-through fill as a tint)
        static void Frame(RectTransform r, Color colour)
        {
            for (int k = 0; k < 4; k++)
            {
                var edge = new GameObject("Edge", typeof(RectTransform)).GetComponent<RectTransform>(); edge.SetParent(r, false);
                edge.anchorMin = new Vector2(k == 3 ? 1 : 0, k == 0 ? 1 : 0); edge.anchorMax = new Vector2(k == 2 ? 0 : 1, k == 1 ? 0 : 1); edge.pivot = new Vector2(.5f, .5f);
                edge.sizeDelta = k < 2 ? new Vector2(0, 1) : new Vector2(1, 0); edge.anchoredPosition = Vector2.zero;
                var line = edge.gameObject.AddComponent<Image>(); line.color = colour; line.raycastTarget = false;
            }
        }
        Image Line(Transform parent, Color colour) { var line = Rect("Line", parent, 0, 0, 1, 1).gameObject.AddComponent<Image>(); line.color = colour; line.raycastTarget = false; return line; }
        static void PlaceLine(Image line, Vector2 from, Vector2 to, float width)
        {
            var r = line.rectTransform; var mid = (from + to) / 2; var d = to - from;
            r.anchoredPosition = new Vector2(mid.x, -mid.y); r.sizeDelta = new Vector2(d.magnitude, width); r.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
        Button JournalChoice(Transform parent, string text, float x, float top, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var b = MakeButton(parent, text, x, top, width, height, action); var image = b.GetComponent<Image>(); image.canvasRenderer.cullTransparentMesh = false;
            Frame(b.GetComponent<RectTransform>(), new Color(Gilt.r, Gilt.g, Gilt.b, .7f));
            var label = b.GetComponentInChildren<Text>(); label.fontSize = 11; return b;
        }
        static void PaintChoice(Button b, bool on)
        {
            b.GetComponent<Image>().color = on ? Gilt : new Color(.04f, .05f, .09f, .6f); b.GetComponentInChildren<Text>().color = on ? new Color(.08f, .08f, .13f) : Gilt;
            var tint = b.colors; tint.normalColor = Color.white; tint.highlightedColor = new Color(1, 1, 1, .9f); b.colors = tint;
        }
        RectTransform journalWheelLayer, journalTableLayer, journalCard, journalSignPage; Image journalWheelArt, journalCardPicture, journalCardRing; Material journalCardIllumination;
        Text journalCardTitle, journalCardLine, journalSignGlyph; Button journalOpen, journalBack, journalViewWheel, journalViewTable; readonly Button[] journalLensButtons = new Button[4];
        readonly Image[] journalLines = new Image[12]; readonly Text[] journalRowLabels = new Text[4], journalColumnLabels = new Text[3];
        readonly RectTransform[] journalPanels = new RectTransform[4]; readonly Text[] journalPanelLabels = new Text[4], journalPanelValues = new Text[4], journalPanelNotes = new Text[4];
        readonly Button[] journalChips = new Button[6]; readonly Text[] journalChipGlyphs = new Text[6]; readonly int[] journalChipSeats = new int[6];
        Image journalSignRing; Text journalCardMark;
        static Sprite RingSprite() { var line = Slots.Image("journal-seat-line"); return line != null ? line : Disc(); }
        void BuildJournal()
        {
            journal = ScreenPanel("Journal", "journal-page"); journalPage = journal.GetComponent<Image>();
            Bleed.Add(journalPage, root, Bleed.Mode.Clamp, .5f); // Part 2: the book stays in the column; past it the desk's own edge carries on, darker (a mirrored book would show a second spine)
            // the silver rules and the faded vermilion margin, drawn by the game so they sit under the text (the art has none)
            for (float y = RuleFirst; y <= RuleLast; y += RuleGap) { var rule = Rect("Rule", journal, JournalX, y, 274, 1).gameObject.AddComponent<Image>(); rule.color = new Color(Silver.r, Silver.g, Silver.b, .09f); rule.raycastTarget = false; }
            var margin = Rect("Margin", journal, JournalX - 125, 318, 1, 556).gameObject.AddComponent<Image>(); margin.color = new Color(Vermilion.r, Vermilion.g, Vermilion.b, .28f); margin.raycastTarget = false;
            journalTitle = SafeArea.Top(Label(journal, "", JournalX, 56, 250, 60, TitleSize), JournalTitleClearance); journalTitle.supportRichText = true; journalTitle.font = Resources.Load<Font>(TitleFont) ?? journalTitle.font; journalTitle.color = Gilt;
            journalSignGlyph = Label(journal, "", JournalX, 60, 40, 40, 22); journalSignGlyph.font = Dial.GlyphFont; journalSignGlyph.horizontalOverflow = HorizontalWrapMode.Overflow;
            // the Wheel / Table switch and the tabs
            journalViewWheel = JournalChoice(journal, "Wheel", JournalX - 36, SwitchY, 72, 24, () => JournalSetView(false)); // placeholder labels (owner writes)
            journalViewTable = JournalChoice(journal, "Table", JournalX + 36, SwitchY, 72, 24, () => JournalSetView(true));
            for (int i = 0; i < journalLensButtons.Length; i++) { var lens = (JournalLens)i; journalLensButtons[i] = JournalChoice(journal, SliceFlow.LensTitle(lens), 0, TabsY, 62, 22, () => JournalLensTap(lens)); }
            // the Wheel: the gold wheel art, the pattern's lines, then the seats
            journalWheelLayer = Rect("Wheel view", journal, 0, 400, 360, 800);
            journalWheelArt = Rect("Wheel", journalWheelLayer, JournalX, WheelTop, WheelSize, WheelSize).gameObject.AddComponent<Image>(); journalWheelArt.raycastTarget = false; journalWheelArt.color = new Color(1, 1, 1, .1f);
            if (Slots.Dress(journalWheelArt, "journal-wheel")) journalWheelArt.color = Color.white;
            for (int i = 0; i < journalLines.Length; i++) journalLines[i] = Line(journalWheelLayer, Gilt);
            // the Table: its row and column names (the seats move into the cells)
            journalTableLayer = Rect("Table view", journal, 0, 400, 360, 800);
            var tablePanel = Rect("Table", journalTableLayer, JournalX, TableTop + 140, JournalWidth, 280).gameObject.AddComponent<Image>(); tablePanel.color = VellumPanel; tablePanel.raycastTarget = false;
            Frame(tablePanel.rectTransform, new Color(Gilt.r, Gilt.g, Gilt.b, .5f));
            for (int c = 0; c < 3; c++) { journalColumnLabels[c] = Label(journalTableLayer, Zodiac.Modalities[c].ToUpperInvariant(), TableColumnX(c), TableTop + 12, 68, 16, 9); journalColumnLabels[c].color = Silver; }
            for (int r = 0; r < 4; r++) { journalRowLabels[r] = Label(journalTableLayer, Elements[r], JournalX - 98, TableRowY(r), 60, 30, 17); journalRowLabels[r].font = journalTitle.font; journalRowLabels[r].alignment = TextAnchor.MiddleLeft; }
            for (int seat = 0; seat < 12; seat++)
            {
                int s = seat; var parts = new JournalSeatParts(); journalSeats[seat] = parts;
                parts.root = Rect("Seat " + Zodiac.Seats[seat].Name, journal, 0, 0, SeatHit, SeatHit); parts.group = parts.root.gameObject.AddComponent<CanvasGroup>();
                parts.picture = MaskedPicture(parts.root, 0, SeatHit / 2, 34, out parts.illumination);
                parts.ring = Rect("Ring", parts.root, 0, SeatHit / 2, 50, 50).gameObject.AddComponent<Image>(); parts.ring.raycastTarget = false;
                parts.frame = Rect("Frame", parts.root, 0, SeatHit / 2, 60, 60).gameObject.AddComponent<Image>(); parts.frame.raycastTarget = false; parts.frame.color = new Color(.95f, .78f, .47f, .9f);
                parts.badge = Rect("Badge", parts.root, 15, SeatHit / 2 + 15, 16, 16).gameObject.AddComponent<Image>(); parts.badge.sprite = Disc(); parts.badge.raycastTarget = false;
                parts.glyph = Label(parts.badge.transform, "", 0, 8, 16, 16, 11); parts.glyph.font = Dial.GlyphFont; parts.glyph.horizontalOverflow = HorizontalWrapMode.Overflow; parts.glyph.verticalOverflow = VerticalWrapMode.Overflow;
                parts.ribbon = Rect("Due", parts.root, 17, 2, 8, 22).gameObject.AddComponent<Image>(); parts.ribbon.raycastTarget = false; parts.ribbon.color = Crimson;
                var hit = parts.root.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0); hit.canvasRenderer.cullTransparentMesh = false; // taps reach an invisible hit area only with culling off
                parts.hit = parts.root.gameObject.AddComponent<Button>(); parts.hit.targetGraphic = hit; parts.hit.onClick.AddListener(() => JournalSeatTap(s)); Dial.RegisterNavigation(parts.hit);
            }
            // the preview card under the wheel
            journalCard = Rect("Preview", journal, JournalX, CardTop, JournalWidth, CardHeight); var cardBack = journalCard.gameObject.AddComponent<Image>(); cardBack.color = VellumPanel; cardBack.raycastTarget = false;
            Frame(journalCard, new Color(Gilt.r, Gilt.g, Gilt.b, .55f));
            journalCardPicture = MaskedPicture(journalCard, -95, CardHeight / 2, 58, out journalCardIllumination);
            journalCardRing = Rect("Ring", journalCard, -95, CardHeight / 2, 72, 72).gameObject.AddComponent<Image>(); journalCardRing.raycastTarget = false;
            journalCardTitle = Label(journalCard, "", 38, 20, 172, 30, 22); journalCardTitle.font = journalTitle.font; journalCardTitle.alignment = TextAnchor.MiddleLeft; journalCardTitle.supportRichText = true; journalCardTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            journalCardMark = Label(journalCard, "", 0, 24, 24, 24, 15); journalCardMark.font = Dial.GlyphFont; journalCardMark.horizontalOverflow = HorizontalWrapMode.Overflow;
            journalCardLine = Label(journalCard, "", 38, 46, 172, 30, 12); journalCardLine.alignment = TextAnchor.MiddleLeft; journalCardLine.supportRichText = true; journalCardLine.color = PageFaintText;
            journalOpen = JournalChoice(journalCard, "Open the page", -4, 77, 108, 22, JournalOpenSelected); PaintChoice(journalOpen, true); // placeholder (owner writes)
            // a sign's page: its picture in the state ring, the ribbon, a panel per fact learned with the signs that share it
            journalSignPage = Rect("Sign page", journal, 0, 400, 360, 800);
            journalSignArt = MaskedPicture(journalSignPage, JournalX, SignPictureTop, SignPictureSize, out journalIllumination);
            journalSignRing = Rect("Ring", journalSignPage, JournalX, SignPictureTop, 140, 140).gameObject.AddComponent<Image>(); journalSignRing.raycastTarget = false;
            var ribbon = Rect("Ribbon", journalSignPage, 92, JournalTop + 20, 16, 40); journalRibbon = ribbon.gameObject.AddComponent<Image>(); journalRibbon.raycastTarget = false; journalRibbon.color = Crimson;
            if (Slots.Dress(journalRibbon, "journal-ribbon")) // nine-sliced so the forked tail and the head keep their shape at every length
            {
                var file = journalRibbon.sprite; var r = file.rect;
                journalRibbon.sprite = Sprite.Create(file.texture, r, new Vector2(.5f, .5f), file.pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(0, r.height * .2f, 0, r.height * .06f));
                journalRibbon.type = Image.Type.Sliced; journalRibbon.pixelsPerUnitMultiplier = r.height / Slots.Find("journal-ribbon").Height; // the file's pixels at the slot's scale
            }
            foreach (var parts in journalSeats) { parts.ribbon.sprite = journalRibbon.sprite; parts.ribbon.type = journalRibbon.type; parts.ribbon.pixelsPerUnitMultiplier = journalRibbon.pixelsPerUnitMultiplier; if (journalRibbon.sprite != null) parts.ribbon.color = Color.white; }
            if (journalRibbon.sprite != null) journalRibbon.color = Color.white;
            for (int i = 0; i < journalPanels.Length; i++)
            {
                journalPanels[i] = Rect("Fact", journalSignPage, JournalX, 0, JournalWidth, 46); var back = journalPanels[i].gameObject.AddComponent<Image>(); back.color = VellumPanel; back.raycastTarget = false;
                Frame(journalPanels[i], new Color(Gilt.r, Gilt.g, Gilt.b, .5f));
                journalPanelLabels[i] = Label(journalPanels[i], "", -66, 14, 120, 16, 10); journalPanelLabels[i].alignment = TextAnchor.MiddleLeft; journalPanelLabels[i].color = PageFaintText;
                journalPanelValues[i] = Label(journalPanels[i], "", 66, 14, 120, 22, 15); journalPanelValues[i].alignment = TextAnchor.MiddleRight; journalPanelValues[i].fontStyle = FontStyle.Bold;
                journalPanelNotes[i] = Label(journalPanels[i], "", 0, 34, JournalWidth - 22, 18, 11); journalPanelNotes[i].alignment = TextAnchor.MiddleLeft; journalPanelNotes[i].color = PageFaintText;
            }
            for (int i = 0; i < journalChips.Length; i++)
            {
                int chip = i; journalChips[i] = JournalChoice(journalSignPage, "", 0, 0, 70, 20, () => JournalChipTap(chip));
                var label = journalChips[i].GetComponentInChildren<Text>(); label.alignment = TextAnchor.MiddleRight; label.color = PageText; var lr = label.rectTransform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(4, 0); lr.offsetMax = new Vector2(-6, 0);
                journalChips[i].GetComponent<Image>().color = new Color(.04f, .05f, .09f, .6f); foreach (Transform child in journalChips[i].transform) if (child.name == "Edge") child.GetComponent<Image>().color = new Color(Silver.r, Silver.g, Silver.b, .45f);
                journalChipGlyphs[i] = Label(journalChips[i].transform, "", 0, 10, 14, 18, 11); journalChipGlyphs[i].font = Dial.GlyphFont; journalChipGlyphs[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            journalPrev = MakeButton(journal, "", -122, JournalRowY, 64, 56, () => JournalTurn(-1)); StyleAtriumButton(journalPrev); journalChevrons[0] = Chevron(journalPrev, -1); // owner, Sept 25: an arrow flips the page
            journalBack = MakeButton(journal, "Back to the Wheel", 0, JournalRowY, 146, 56, JournalWheel); StyleAtriumButton(journalBack); journalBack.GetComponentInChildren<Text>().fontSize = 14; // placeholder (owner writes)
            journalNext = MakeButton(journal, "", 122, JournalRowY, 64, 56, () => JournalTurn(1)); StyleAtriumButton(journalNext); journalChevrons[1] = Chevron(journalNext, 1);
            journalClose = MakeButton(journal, "Close the journal", 0, JournalCloseY, 190, 48, CloseJournal); StyleAtriumButton(journalClose);
            BuildJournalFront();
        }
        // ---- Batch 2 (owner, Oct 1: the journal's architecture, polished on the approved board, 86bcbn6w6; Oct 2 evening: the owner's
        // inscription and the fonts). The title page on the first-ever open (1e A) settles, then fades into the landing; the landing (the
        // first open of each session, 1b C) holds the Keeper's record between two gold flourishes and the Practice and Contents doors;
        // Contents lists the chapters (the Wheel, the Library Map, two Sealed); the Library Map is the parchment plan moved in from the
        // mini-menu concept's Option C, the rooms you have woken named in gold. Every chapter links back to Contents, top left. The record's
        // lines sit on the page's rules, as on the board; the script lines are EB Garamond Italic, the upright ones EB Garamond Bold. ----
        public const string ItalicFont = "Fonts/EBGaramond-Italic", SunFont = "Fonts/NotoSansSymbols2-Regular"; // bundled (OFL): the script lines; ☉ (U+2609) for the Big Three's line
        public const float TitleSettle = 1f, TitleFade = .6f;           // 1e A: the title page settles for a second, then fades into the landing
        public const float TitleRoom = 190f;                            // a page title's width between the page's corner flourishes (the art's moon and leaves)
        public const float KeeperFirst = 96f, KeeperRule = 26f, KeeperWidth = 244f; // the opening flourish on the first rule; a line on each rule below, 3 px above it; no wider than the page's calm column inside its margin
        public const float DoorWidth = 238f, DoorHeight = 120f, DoorStep = 134f, DoorBelow = 20f; // the board's doors, 238 x 120; closer than the board's (150 apart, 29 under the flourish) now the record holds the Big Three and the owner's inscription
        public const float PageOrnamentTop = 556f; // the page's lower corner ornaments (the moon at the right) begin about here: the doors end above them
        public const float LinkLeft = 88f, LinkBaseline = 89f, LinkWidth = 96f; // "‹ Your Journal" on Contents, "‹ Contents" on a chapter: the board's place, top left under the title
        public const float SwitchY = 119f, TabsY = 143f;                 // the Wheel / Table switch and the tabs, a band lower than Build AA's 96 and 126: the link above keeps its 44 px target
        public const float ContentsFlourish = 122f, ChapterRingX = -80f, ChapterNameLeft = 142f; // the ring's centre and the names' left edge, on the 360 layout (the board)
        public static readonly float[] ChapterTop = { 148, 252, 356, 434 }, ChapterHeight = { 104, 104, 78, 78 }; // the board's rows: the two chapters, then two Sealed
        public const float PlanTop = 106f, PlanWidth = 234f, PlanHeight = 350f; // the Library Map: the plan at its own 2x, centred on the page's column
        // each room's name on the plan (layout px from the plan's top-left) and the width it wraps to, in TravelMenu.Rooms' order: in the hall
        // under its star, in the Wing's lower half, in the Chamber under its crystal
        public static readonly Vector3[] PlanNames = { new Vector3(116.5f, 262, 100), new Vector3(55.5f, 108, 52), new Vector3(178.5f, 113, 64) };
        const float GaramondLift = .3545f, FrakLift = .267f;             // a line's baseline below its centre, per point: EB Garamond (ascender 1007, descender 298 of 1000) and UnifrakturMaguntia (1607 and 513 of 2048)
        static readonly Color GiltInk = new Color32(0xe3, 0xa3, 0x4a, 0xff), GiltDown = new Color32(0xb4, 0x80, 0x3a, 0xff), SilverInk = new Color32(0xc9, 0xcf, 0xdb, 0xff), SealedInk = new Color32(0x7d, 0x81, 0x8c, 0xff), SealedRing = new Color32(0x6d, 0x71, 0x7c, 0xcc); // the board's gilt (and pressed), silver, and the sealed grey
        RectTransform journalTitlePage, journalLanding, journalContents, journalMap, journalFlourishBottom; CanvasGroup journalTitlePageGroup, journalLandingGroup, journalTitleGroup;
        Text journalKeeperHeading, journalKeys; readonly List<Text> keeperLines = new List<Text>(); readonly Text[] journalMapNames = new Text[3];
        Button journalPracticeDoor, journalContentsDoor, journalHomeLink, journalContentsLink; readonly Button[] journalChapterRows = new Button[2];
        Font journalItalic; Coroutine journalBeat;
        // the Big Three's line (owner, Oct 2 evening: approved, unknowns A): each glyph in the font that carries it, gilt; the words in the italic
        RectTransform bigThreeLine; readonly Text[] bigThreeGlyphs = new Text[3], bigThreeWords = new Text[3]; Button journalBigThree; // Oct 7: the line opens "Your Birth" for every player
        // "Your Birth" (owner, Oct 7): three rows (Date, Time, Place: the value or unknown, with Add), the journal's lines under them, and its buttons
        public const float BirthRowTop = 148f, BirthRowHeight = 46f, BirthNameLeft = 64f, BirthValueLeft = 124f, BirthAddX = 280f, BirthAddWidth = 72f, BirthLineWidth = 256f, BirthButtonWidth = 214f;
        RectTransform journalBirth; readonly Text[] birthRowNames = new Text[3], birthRowValues = new Text[3]; readonly Button[] birthAddButtons = new Button[3]; readonly List<Text> birthPageLines = new List<Text>(); Button birthRisingButton, birthTimeButton;
        public static readonly string[] BigThreeMarks = { "\u2609", "\u263d", "\u2191" }; // ☉ (Noto Sans Symbols 2), ☽ (Noto Sans Symbols), ↑ (EB Garamond)
        public static readonly int[] BigThreeMarkSizes = { 15, 19, 17 };                      // each drawn about the words' cap height
        static readonly float[] BigThreeMarkLift = { .2195f, .455f, .3545f };                 // each font's baseline below its line's centre, per point (its ascender and descender)
        string JournalPageTitle(JournalView at) => at == JournalView.Title || at == JournalView.Landing ? SliceFlow.JournalTitle : at == JournalView.Contents ? SliceFlow.ContentsTitle : at == JournalView.Map ? SliceFlow.MapTitle : at == JournalView.Birth ? SliceFlow.BirthPageTitle : at == JournalView.Practice ? SliceFlow.PracticeTitle : at == JournalView.Quiz ? (Flow.QuizConcept != null ? Flow.QuizConcept.name : SliceFlow.PracticeTitle) : SliceFlow.WheelTitle;
        // ---- Practice (owner, Oct 3: step 4 approved; the board on 86bcbn6w6, Oct 2). The list of concepts learned, each row a gilt star, its
        // name and its line; a question page: the counter, the ask (a sign's mark drawn large above it, for the Symbols), up to four choices in
        // the board's hairline frames, then right or wrong (the right one gilded with its star, a wrong pick in muted crimson with ×, the rest
        // at half), the why and Next; the round's end, its line and Back to Practice. The frames and the star are drawn by the engine (no file). ----
        public const float PracticeRowTop = 148f, PracticeRowHeight = 78f, PracticeStarX = 80f, PracticeNameLeft = 96f;     // the board's list: rows 78 tall from 148, the star at x 80, the names from x 96
        public const float QuizCounterY = 145f, QuizAskY = 197f, QuizGlyphY = 180f, QuizLineStep = 26f, QuizAskWidth = 256f; // the counter's and the ask's baselines; a sign's mark centred above a one-line ask
        public const float ChoiceWidth = 238f, ChoiceHeight = 44f, ChoiceStep = 52f, QuizNextWidth = 120f, QuizBackWidth = 180f; // the board's choices, 52 apart, and its two buttons: every target 44 px tall
        public const float QuizBottom = 595f; // the page's inner border runs at about y 607: a question's Next ends 12 above it
        // where a question's pieces go (x: the ask's first baseline, y: the choices' top, z: their step, w: Next's top). The why's lines are
        // counted before the pick (the longer of right and wrong), so nothing moves when the answer shows; the choices close up, to 46 apart
        // at most, when the page would run past its border (a symbol, a two-line ask and a two-line why)
        public static Vector4 QuizLayout(bool mark, int askLines, int choices, int whyLines)
        {
            float first = mark ? QuizAskY + QuizLineStep : QuizAskY, top = first + QuizLineStep * (Mathf.Max(1, askLines) - 1) + 27, step = ChoiceStep;
            float Next(float s) => top + s * (Mathf.Max(1, choices) - 1) + ChoiceHeight + 37 + QuizLineStep * (Mathf.Max(1, whyLines) - 1) + 27;
            if (choices > 1 && Next(step) + ChoiceHeight > QuizBottom) step = Mathf.Max(ChoiceHeight + 2, step - (Next(step) + ChoiceHeight - QuizBottom) / (choices - 1));
            return new Vector4(first, top, step, Next(step));
        }
        static readonly Color CrimsonEdge = new Color32(176, 64, 70, 242), CrimsonInk = new Color32(0xc9, 0x88, 0x8a, 0xff), GiltWash = new Color(227 / 255f, 163 / 255f, 74 / 255f, .05f); // the board's wrong pick, and the right answer's wash (the board's .16 in a browser's blend; the game blends in linear colour, where .05 looks the same)
        RectTransform journalPractice, journalQuiz; Button journalPracticeLink, quizNext, quizBack; Text quizCounter, quizGlyph; CanvasGroup quizChoicesGroup;
        readonly Button[] practiceRows = new Button[5]; readonly Text[] practiceRowLines = new Text[5]; readonly Image[] practiceRowRules = new Image[5];
        readonly Text[] quizAsk = new Text[3], quizFeedback = new Text[3], quizEnd = new Text[3], quizCrosses = new Text[4];
        readonly Button[] quizChoices = new Button[4]; readonly Image[] quizChoicePanels = new Image[4], quizStars = new Image[4]; readonly RectTransform[] quizThin = new RectTransform[4], quizThick = new RectTransform[4]; readonly CanvasGroup[] quizChoiceGroups = new CanvasGroup[4];
        // the board's four-point star (14 px, drawn at 4x and sampled 16 times a pixel): Practice's row marks and the right answer's
        static Sprite practiceStar;
        public static Sprite StarSprite()
        {
            if (practiceStar != null) return practiceStar;
            const int size = 56; const float scale = size / 14f; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var points = new[] { new Vector2(0, -6.5f), new Vector2(1.1f, -1.1f), new Vector2(6.5f, 0), new Vector2(1.1f, 1.1f), new Vector2(0, 6.5f), new Vector2(-1.1f, 1.1f), new Vector2(-6.5f, 0), new Vector2(-1.1f, -1.1f) };
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++) if (InPolygon(points, new Vector2((x + (sx + .5f) / 4) / scale - 7, (y + (sy + .5f) / 4) / scale - 7))) inside++;
                    texture.SetPixel(x, y, new Color(1, 1, 1, inside / 16f));
                }
            texture.Apply(); practiceStar = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f)); practiceStar.name = "practice-star"; return practiceStar;
        }
        static bool InPolygon(Vector2[] polygon, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                if ((polygon[i].y > p.y) != (polygon[j].y > p.y) && p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x) inside = !inside;
            return inside;
        }
        // a border inside the rect's edges, drawn as four edges: the board's inset 2 px mark on a right or a wrong choice
        static void InsetFrame(RectTransform r, Color colour, float width)
        {
            for (int k = 0; k < 4; k++)
            {
                var edge = new GameObject("Edge", typeof(RectTransform)).GetComponent<RectTransform>(); edge.SetParent(r, false);
                edge.anchorMin = new Vector2(k == 3 ? 1 : 0, k == 0 ? 1 : 0); edge.anchorMax = new Vector2(k == 2 ? 0 : 1, k == 1 ? 0 : 1);
                edge.pivot = new Vector2(k == 2 ? 0 : k == 3 ? 1 : .5f, k == 0 ? 1 : k == 1 ? 0 : .5f); edge.sizeDelta = k < 2 ? new Vector2(0, width) : new Vector2(width, 0); edge.anchoredPosition = Vector2.zero;
                var line = edge.gameObject.AddComponent<Image>(); line.color = colour; line.raycastTarget = false;
            }
        }
        static float[] PageBox(Button b) { var r = (RectTransform)b.transform; return new[] { r.anchoredPosition.x, -r.anchoredPosition.y, r.sizeDelta.x, r.sizeDelta.y }; } // a target on the page: x, centre y, width, height
        // A line of the page set on a baseline (layout y): its box is centred on the line, so the baseline falls where the board has it.
        Text PageLine(Transform parent, string text, float x, float baseline, float width, int size, Font face, Color colour, TextAnchor align = TextAnchor.MiddleCenter, float lift = GaramondLift)
        {
            var line = Label(parent, text, x, baseline - lift * size, width, Mathf.Ceil(size * 1.6f), size); line.font = face; line.color = colour; line.alignment = align;
            line.horizontalOverflow = HorizontalWrapMode.Overflow; line.verticalOverflow = VerticalWrapMode.Overflow; return line;
        }
        RectTransform Flourish(Transform parent, float y)
        {
            var r = Rect("Flourish", parent, JournalX, y, 220, 20); var image = r.gameObject.AddComponent<Image>(); image.raycastTarget = false;
            if (!Slots.Dress(image, "journal-flourish")) { image.color = new Color(GiltInk.r, GiltInk.g, GiltInk.b, .6f); r.sizeDelta = new Vector2(208, 1); } // no file: a gilt hairline
            return r;
        }
        // A ring the engine draws (no file): the board's 1.5 px circle of radius 27 round a chapter's emblem, made once at 2x.
        static Sprite chapterRing;
        static Sprite ChapterRingSprite()
        {
            if (chapterRing != null) return chapterRing;
            const int size = 112; const float radius = 54, half = 1.5f; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) { float d = Mathf.Sqrt((x + .5f - size / 2f) * (x + .5f - size / 2f) + (y + .5f - size / 2f) * (y + .5f - size / 2f)); texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(half + .5f - Mathf.Abs(d - radius)))); }
            texture.Apply(); chapterRing = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f)); return chapterRing;
        }
        Image Picture(Transform parent, string slot, float x, float top, float width, float height)
        {
            var image = Rect(slot, parent, x, top, width, height).gameObject.AddComponent<Image>(); image.raycastTarget = false;
            if (!Slots.Dress(image, slot)) image.color = new Color(PanelColor.r, PanelColor.g, PanelColor.b, .5f); return image;
        }
        // A door on the landing: the game's vellum panel inside the fine gold frame (nine-sliced, its corners whole at any size), the emblem,
        // the name in blackletter and its line in italic, as on the board. It keeps the 3d states (pressed darkens and sinks, unavailable half).
        Button JournalDoor(Transform parent, string name, string line, string emblem, Vector2 emblemSize, UnityEngine.Events.UnityAction action)
        {
            var b = MakeButton(parent, name, JournalX, 0, DoorWidth, DoorHeight, action); b.name = name + " door"; var feel = ButtonLook.Custom(b, "door"); var look = feel.Look;
            var panel = Rect("Panel", look, 0, DoorHeight / 2, DoorWidth - 10, DoorHeight - 10).gameObject.AddComponent<Image>(); panel.color = VellumPanel; panel.raycastTarget = false; panel.transform.SetAsFirstSibling();
            var frame = Rect("Frame", look, 0, DoorHeight / 2, DoorWidth, DoorHeight).gameObject.AddComponent<Image>(); frame.raycastTarget = false;
            if (Slots.Dress(frame, "journal-door-frame")) { var file = frame.sprite.rect; float border = Mathf.Round(file.height * 92f / 240f); frame.sprite = ButtonLook.SlicedArt("journal-door-frame", Vector4.one * border); frame.type = Image.Type.Sliced; frame.pixelsPerUnitMultiplier = file.height / DoorHeight; feel.Pieces.Add(frame); }
            else { frame.color = new Color(0, 0, 0, 0); Frame(frame.rectTransform, new Color(GiltInk.r, GiltInk.g, GiltInk.b, .7f)); }
            var art = Picture(look, emblem, -67, DoorHeight / 2, emblemSize.x, emblemSize.y); if (art.sprite != null) feel.Pieces.Add(art);
            var title = feel.Label; title.text = name; title.font = journalTitle.font; title.fontSize = 28; title.alignment = TextAnchor.MiddleLeft; title.horizontalOverflow = HorizontalWrapMode.Overflow; title.verticalOverflow = VerticalWrapMode.Overflow;
            var tr = title.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(.5f, 1); tr.sizeDelta = new Vector2(130, 46); tr.anchoredPosition = new Vector2(-DoorWidth / 2 + 100 + 65, -((line == "" ? 70 : 58) - FrakLift * 28));
            if (line != "") PageLine(look, line, -DoorWidth / 2 + 101 + 65, 83, 130, 16, journalItalic, SilverInk, TextAnchor.MiddleLeft);
            feel.LabelUp = GiltInk; feel.LabelDown = GiltDown; return b;
        }
        // A link on the page, top left: gold words with a 44 px target, kept under the title when the safe band moves it.
        Button JournalLink(string words, UnityEngine.Events.UnityAction action)
        {
            var b = MakeButton(journal, words, -180 + LinkLeft + LinkWidth / 2, LinkBaseline - GaramondLift * 14, LinkWidth, 22, action); b.name = words.Substring(2) + " link"; var feel = ButtonLook.Custom(b, "link");
            var label = feel.Label; label.font = ButtonLook.EngravedFont ?? font; label.fontSize = 14; label.alignment = TextAnchor.MiddleLeft; label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var lr = label.rectTransform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
            feel.LabelUp = GiltInk; feel.LabelDown = GiltDown; ButtonLook.HitArea(b, ButtonLook.MinTarget, ButtonLook.MinTarget);
            var follow = b.gameObject.AddComponent<SafeTop>(); follow.Follow = journalTitle.GetComponent<SafeTop>(); return b;
        }
        // A row on Contents: the emblem in its ring on a dark disc, the chapter's name and its line; a Sealed row is the same, dim, with a lock, and opens nothing.
        void ChapterRow(int row, string name, string line, string emblem, Vector2 emblemSize, UnityEngine.Events.UnityAction action)
        {
            float top = ChapterTop[row], h = ChapterHeight[row], ringY = h / 2 + 2; bool sealedRow = action == null;
            RectTransform look; ButtonFeel feel = null; Text title;
            if (!sealedRow) { var b = MakeButton(journalContents, name, JournalX, top + h / 2, DoorWidth, h, action); b.name = name + " row"; feel = ButtonLook.Custom(b, "row"); look = feel.Look; title = feel.Label; journalChapterRows[row] = b; }
            else { look = Rect(name + " (sealed)", journalContents, JournalX, top + h / 2, DoorWidth, h); title = Label(look, name, 0, 0, 10, 10, 20); }
            float ringX = ChapterRingX - JournalX;
            var disc = Rect("Disc", look, ringX, ringY, 52, 52).gameObject.AddComponent<Image>(); disc.sprite = Disc(); disc.color = VellumPanel; disc.raycastTarget = false;
            var ring = Rect("Ring", look, ringX, ringY, 56, 56).gameObject.AddComponent<Image>(); ring.sprite = ChapterRingSprite(); ring.color = sealedRow ? SealedRing : GiltInk; ring.raycastTarget = false;
            var art = Picture(look, emblem, ringX, ringY, emblemSize.x, emblemSize.y); if (feel != null && art.sprite != null) feel.Pieces.Add(art);
            float left = ChapterNameLeft - 180 - JournalX; // the names' left edge, from the row's centre
            title.text = name; title.font = ButtonLook.EngravedFont ?? font; title.fontSize = sealedRow ? 20 : 21; title.color = sealedRow ? SealedInk : GiltInk; title.alignment = TextAnchor.MiddleLeft; title.horizontalOverflow = HorizontalWrapMode.Overflow;
            var tr = title.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(.5f, 1); tr.sizeDelta = new Vector2(170, 34); tr.anchoredPosition = new Vector2(left + 85, -(49 - GaramondLift * title.fontSize));
            if (line != "") PageLine(look, line, left + 1 + 85, 75, 170, 15, journalItalic, SilverInk, TextAnchor.MiddleLeft);
            if (feel != null) { feel.LabelUp = GiltInk; feel.LabelDown = GiltDown; }
            var rule = Rect("Rule", journalContents, JournalX, top + h, DoorWidth, 1).gameObject.AddComponent<Image>(); rule.color = new Color(Silver.r, Silver.g, Silver.b, .3f); rule.raycastTarget = false;
        }
        // A row on Practice's list: the gilt star, the concept's name and its line, and a rule under it; ShowJournalPractice sets the words
        void PracticeRow(int row)
        {
            float top = PracticeRowTop + PracticeRowHeight * row, h = PracticeRowHeight, left = PracticeNameLeft - 180 - JournalX; // the names' left edge, from the row's centre
            var b = MakeButton(journalPractice, "Concept " + row, JournalX, top + h / 2, ChoiceWidth, h, () => StartPracticeRound(row)); b.name = "Concept row " + row; var feel = ButtonLook.Custom(b, "row");
            var star = Rect("Star", feel.Look, PracticeStarX - 180 - JournalX, 31, 14, 14).gameObject.AddComponent<Image>(); star.sprite = StarSprite(); star.color = GiltInk; star.raycastTarget = false;
            var title = feel.Label; title.font = ButtonLook.EngravedFont ?? font; title.fontSize = 21; title.color = GiltInk; title.alignment = TextAnchor.MiddleLeft; title.horizontalOverflow = HorizontalWrapMode.Overflow;
            var tr = title.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(.5f, 1); tr.sizeDelta = new Vector2(210, 34); tr.anchoredPosition = new Vector2(left + 105, -(37 - GaramondLift * title.fontSize));
            practiceRowLines[row] = PageLine(feel.Look, "", left + 1 + 105, 61, 210, 15, journalItalic, SilverInk, TextAnchor.MiddleLeft);
            feel.LabelUp = GiltInk; feel.LabelDown = GiltDown; practiceRows[row] = b;
            var rule = Rect("Rule", journalPractice, JournalX, top + h, ChoiceWidth, 1).gameObject.AddComponent<Image>(); rule.color = new Color(Silver.r, Silver.g, Silver.b, .3f); rule.raycastTarget = false; practiceRowRules[row] = rule;
        }
        // A choice on the question page: the board's hairline frame on the vellum, the words in EB Garamond Bold; once answered, ShowJournalQuiz
        // gilds the right one (its 2 px mark, the wash and the star), marks a wrong pick in muted crimson with ×, and sets the rest at half
        void QuizChoice(RectTransform parent, int i)
        {
            var b = MakeButton(parent, "Choice " + i, JournalX, 0, ChoiceWidth, ChoiceHeight, () => PickQuizChoice(i)); b.name = "Choice " + i; var feel = ButtonLook.Custom(b, "choice");
            quizChoiceGroups[i] = feel.Look.gameObject.AddComponent<CanvasGroup>();
            var panel = Rect("Panel", feel.Look, 0, ChoiceHeight / 2, ChoiceWidth, ChoiceHeight).gameObject.AddComponent<Image>(); panel.color = VellumPanel; panel.raycastTarget = false; panel.transform.SetAsFirstSibling(); quizChoicePanels[i] = panel;
            var thin = Rect("Hairline", feel.Look, 0, ChoiceHeight / 2, ChoiceWidth, ChoiceHeight); Frame(thin, new Color(GiltInk.r, GiltInk.g, GiltInk.b, .7f)); quizThin[i] = thin;
            var mark = Rect("Mark", feel.Look, 0, ChoiceHeight / 2, ChoiceWidth, ChoiceHeight); InsetFrame(mark, GiltInk, 2); quizThick[i] = mark;
            var label = feel.Label; label.font = ButtonLook.EngravedFont ?? font; label.fontSize = 18; label.alignment = TextAnchor.MiddleCenter; label.horizontalOverflow = HorizontalWrapMode.Overflow; label.transform.SetAsLastSibling();
            var lr = label.rectTransform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(30, 0); lr.offsetMax = new Vector2(-30, 0);
            feel.LabelUp = GiltInk; feel.LabelDown = GiltDown;
            var star = Rect("Star", feel.Look, 99, ChoiceHeight / 2, 14, 14).gameObject.AddComponent<Image>(); star.sprite = StarSprite(); star.color = GiltInk; star.raycastTarget = false; quizStars[i] = star;
            var cross = Label(feel.Look, "\u00d7", 97, ChoiceHeight / 2, 20, 30, 20); cross.font = ButtonLook.EngravedFont ?? font; cross.color = CrimsonInk; quizCrosses[i] = cross;
            quizChoices[i] = b;
        }
        // Next and Back to Practice: the board's small framed buttons (the vellum, a gilt hairline, the words in EB Garamond Bold), 44 px tall
        Button JournalFrameButton(Transform parent, string words, float width, UnityEngine.Events.UnityAction action)
        {
            var b = MakeButton(parent, words, JournalX, 0, width, ChoiceHeight, action); b.name = words + " button"; var feel = ButtonLook.Custom(b, "frame");
            var panel = Rect("Panel", feel.Look, 0, ChoiceHeight / 2, width, ChoiceHeight).gameObject.AddComponent<Image>(); panel.color = VellumPanel; panel.raycastTarget = false; panel.transform.SetAsFirstSibling();
            Frame(Rect("Hairline", feel.Look, 0, ChoiceHeight / 2, width, ChoiceHeight), new Color(GiltInk.r, GiltInk.g, GiltInk.b, .7f));
            var label = feel.Label; label.font = ButtonLook.EngravedFont ?? font; label.fontSize = 16; label.alignment = TextAnchor.MiddleCenter; label.horizontalOverflow = HorizontalWrapMode.Overflow; label.transform.SetAsLastSibling();
            var lr = label.rectTransform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
            feel.LabelUp = GiltInk; feel.LabelDown = GiltDown; return b;
        }
        void BuildJournalFront()
        {
            journalItalic = Resources.Load<Font>(ItalicFont) ?? font; var serif = ButtonLook.EngravedFont ?? font;
            journalTitleGroup = journalTitle.gameObject.AddComponent<CanvasGroup>(); journalTitleGroup.blocksRaycasts = false; journalTitleGroup.interactable = false;
            // the title page: the first-ever open only
            journalTitlePage = Rect("Title page", journal, 0, 400, 360, 800); journalTitlePageGroup = journalTitlePage.gameObject.AddComponent<CanvasGroup>(); journalTitlePageGroup.blocksRaycasts = false;
            var big = PageLine(journalTitlePage, SliceFlow.JournalTitle, JournalX, 296, 300, 40, journalTitle.font, Gilt, TextAnchor.MiddleCenter, FrakLift); big.supportRichText = false;
            Flourish(journalTitlePage, 331);
            PageLine(journalTitlePage, SliceFlow.KeeperHeading, JournalX, 363, 200, 16, serif, GiltInk).gameObject.AddComponent<Tracking>().Spacing = 16 * .3f;
            PageLine(journalTitlePage, SliceFlow.TitlePageLine, JournalX, 392, 280, 16, journalItalic, new Color(SilverInk.r, SilverInk.g, SilverInk.b, .86f));
            // the landing: the Keeper's record, then the two doors (placed by ShowJournalLanding, under the record's lines)
            journalLanding = Rect("Landing", journal, 0, 400, 360, 800); journalLandingGroup = journalLanding.gameObject.AddComponent<CanvasGroup>();
            var inkTap = Rect("Finish the line", journalLanding, 0, InkTapBottom / 2, 360, InkTapBottom); var inkTapImage = inkTap.gameObject.AddComponent<Image>(); inkTapImage.color = Color.clear; // a tap on the page finishes the line (Oct 7); the doors sit above it
            inkTap.gameObject.AddComponent<Button>().onClick.AddListener(InscriptionTap); inkTap.GetComponent<Button>().transition = Selectable.Transition.None;
            Flourish(journalLanding, KeeperFirst); journalFlourishBottom = Flourish(journalLanding, KeeperFirst + KeeperRule * 6);
            journalKeeperHeading = PageLine(journalLanding, SliceFlow.KeeperHeading, JournalX, 0, 200, 16, serif, GiltInk); journalKeeperHeading.gameObject.AddComponent<Tracking>().Spacing = 16 * .3f; // the board: small capitals spaced .3 em
            journalKeys = PageLine(journalLanding, "", JournalX, 0, KeeperWidth + 24, 17, journalItalic, SilverInk);
            for (int i = 0; i < 4; i++) keeperLines.Add(PageLine(journalLanding, "", JournalX, 0, KeeperWidth + 24, 16, journalItalic, new Color(SilverInk.r, SilverInk.g, SilverInk.b, .86f)));
            Flow.InscriptionFits = text => WrapWords(keeperLines[0], text, KeeperWidth).Count <= SliceFlow.InscriptionMostLines; // a line that would take a fourth line for this name sits out (Oct 7)
            bigThreeLine = Rect("Big Three", journalLanding, JournalX, 0, KeeperWidth, 30);
            var markFonts = new[] { Resources.Load<Font>(SunFont) ?? font, Dial.GlyphFont ?? font, ButtonLook.EngravedFont ?? font };
            for (int i = 0; i < 3; i++)
            {
                bigThreeGlyphs[i] = PageLine(bigThreeLine, BigThreeMarks[i], 0, 0, 30, BigThreeMarkSizes[i], markFonts[i], GiltInk, TextAnchor.MiddleCenter, BigThreeMarkLift[i]);
                bigThreeWords[i] = PageLine(bigThreeLine, "", 0, 0, 120, 17, journalItalic, SilverInk);
                foreach (var piece in new[] { bigThreeGlyphs[i], bigThreeWords[i] }) piece.rectTransform.anchorMin = piece.rectTransform.anchorMax = new Vector2(.5f, .5f); // placed about the line's centre, its baseline
            }
            journalBigThree = MakeButton(journalLanding, "Your Birth", JournalX, 0, KeeperWidth + 24, ButtonLook.MinTarget, JournalBirthOpen); journalBigThree.name = "Big Three line"; // no look of its own: the line as it was, a 44 px target over it
            { var image = journalBigThree.GetComponent<Image>(); image.color = new Color(0, 0, 0, 0); image.canvasRenderer.cullTransparentMesh = false; journalBigThree.GetComponentInChildren<Text>().text = ""; }
            journalPracticeDoor = JournalDoor(journalLanding, SliceFlow.PracticeDoor, SliceFlow.PracticeDoorLine, "journal-emblem-practice", new Vector2(56, 72), JournalPracticeOpen); // owner, Oct 3: it opens Practice, with no count
            journalContentsDoor = JournalDoor(journalLanding, SliceFlow.ContentsDoor, SliceFlow.ContentsDoorLine, "journal-emblem-contents", new Vector2(76, 60), JournalContents);
            // Contents
            journalContents = Rect("Contents", journal, 0, 400, 360, 800); Flourish(journalContents, ContentsFlourish);
            ChapterRow(0, SliceFlow.WheelTitle, SliceFlow.WheelChapterLine, "journal-emblem-wheel", new Vector2(48, 48), () => JournalChapter("wheel"));
            ChapterRow(1, SliceFlow.MapTitle, SliceFlow.MapChapterLine, "journal-emblem-map", new Vector2(40, 40), () => JournalChapter("map"));
            for (int i = 0; i < SliceFlow.SealedChapters; i++) ChapterRow(2 + i, SliceFlow.SealedChapter, "", "journal-emblem-lock", new Vector2(24, 28), null);
            // the Library Map
            journalMap = Rect("Library Map", journal, 0, 400, 360, 800);
            var plan = Picture(journalMap, "journal-library-plan", JournalX, PlanTop + PlanHeight / 2, PlanWidth, PlanHeight);
            for (int i = 0; i < journalMapNames.Length; i++)
            {
                var at = PlanNames[i]; var name = Label(plan.transform, TravelMenu.Rooms[i].name, at.x - PlanWidth / 2, at.y, at.z, 30, 10); name.font = serif; name.color = GiltInk;
                name.horizontalOverflow = HorizontalWrapMode.Wrap; name.verticalOverflow = VerticalWrapMode.Overflow; name.lineSpacing = .95f;
                var edge = name.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(.08f, .05f, .03f, .92f); edge.effectDistance = new Vector2(1, -1); journalMapNames[i] = name;
            }
            // Practice (owner, Oct 3): its list, then a question page
            journalPractice = Rect("Practice", journal, 0, 400, 360, 800); Flourish(journalPractice, ContentsFlourish);
            for (int i = 0; i < practiceRows.Length; i++) PracticeRow(i);
            journalQuiz = Rect("Practice question", journal, 0, 400, 360, 800); Flourish(journalQuiz, ContentsFlourish);
            quizCounter = PageLine(journalQuiz, "", JournalX, QuizCounterY, 200, 14, journalItalic, new Color(SilverInk.r, SilverInk.g, SilverInk.b, .8f));
            quizGlyph = Label(journalQuiz, "", JournalX, QuizGlyphY, 80, 56, 36); quizGlyph.font = Dial.GlyphFont ?? font; quizGlyph.color = GiltInk; quizGlyph.horizontalOverflow = HorizontalWrapMode.Overflow; quizGlyph.verticalOverflow = VerticalWrapMode.Overflow;
            for (int i = 0; i < quizAsk.Length; i++)
            {
                quizAsk[i] = PageLine(journalQuiz, "", JournalX, 0, QuizAskWidth + 24, 19, journalItalic, SilverInk);
                quizFeedback[i] = PageLine(journalQuiz, "", JournalX, 0, QuizAskWidth + 24, 16, journalItalic, SilverInk);
                quizEnd[i] = PageLine(journalQuiz, "", JournalX, 0, QuizAskWidth + 24, 17, journalItalic, SilverInk);
            }
            var choices = Rect("Choices", journalQuiz, 0, 400, 360, 800); quizChoicesGroup = choices.gameObject.AddComponent<CanvasGroup>();
            for (int i = 0; i < quizChoices.Length; i++) QuizChoice(choices, i);
            quizNext = JournalFrameButton(journalQuiz, SliceFlow.QuizNextWords, QuizNextWidth, QuizNext); quizBack = JournalFrameButton(journalQuiz, SliceFlow.QuizBackWords, QuizBackWidth, QuizBack);
            // "Your Birth" (Oct 7)
            journalBirth = Rect("Your Birth", journal, 0, 400, 360, 800); Flourish(journalBirth, ContentsFlourish);
            string[] rowNames = { SliceFlow.DateRow, SliceFlow.TimeRow, SliceFlow.PlaceRow };
            for (int i = 0; i < 3; i++)
            {
                float mid = BirthRowTop + BirthRowHeight * i + BirthRowHeight / 2;
                birthRowNames[i] = PageLine(journalBirth, rowNames[i], BirthNameLeft - 180 + 50, mid + 6, 100, 18, serif, GiltInk, TextAnchor.MiddleLeft);
                birthRowValues[i] = PageLine(journalBirth, "", BirthValueLeft - 180 + 100, mid + 6, 200, 16, journalItalic, SilverInk, TextAnchor.MiddleLeft);
                birthAddButtons[i] = JournalFrameButton(journalBirth, SliceFlow.AddWords, BirthAddWidth, () => BirthAdd(false)); ((RectTransform)birthAddButtons[i].transform).anchoredPosition = new Vector2(BirthAddX - 180, -mid);
                var rule = Rect("Rule", journalBirth, JournalX, BirthRowTop + BirthRowHeight * (i + 1), ChoiceWidth + 30, 1).gameObject.AddComponent<Image>(); rule.color = new Color(Silver.r, Silver.g, Silver.b, .3f); rule.raycastTarget = false;
            }
            for (int i = 0; i < 8; i++) birthPageLines.Add(PageLine(journalBirth, "", JournalX, 0, BirthLineWidth + 24, 16, journalItalic, SilverInk));
            birthRisingButton = JournalFrameButton(journalBirth, SliceFlow.ChooseRising, BirthButtonWidth, BirthChooseRising); birthTimeButton = JournalFrameButton(journalBirth, SliceFlow.AddBirthTime, BirthButtonWidth, () => BirthAdd(true));
            // the links, top left: back to the landing from Contents and from Practice; back to Contents from a chapter; back to Practice from a question
            journalHomeLink = JournalLink(SliceFlow.BackToJournal, JournalHome); journalContentsLink = JournalLink(SliceFlow.BackToContents, JournalContents); journalPracticeLink = JournalLink(SliceFlow.BackToPractice, QuizBack);
            foreach (var page in new[] { journalTitlePage, journalLanding, journalContents, journalMap, journalPractice, journalQuiz, journalBirth }) page.gameObject.SetActive(false);
        }
        // the words in lines of at most the width, broken between words: the owner's wording is kept, only its breaks flex (Oct 2 evening)
        // the words in the lines WrapWords needs, with a two-line break moved so the lines come out even (a break after a comma or a full
        // stop is favoured a little), so no word is left alone on a line: Practice's asks, whys and round ends
        static readonly HashSet<string> SmallWords = new HashSet<string> { "a", "an", "the", "of", "to", "for", "and", "or", "in", "on", "at", "with", "from", "is", "its", "this", "that", "which" };
        public static List<string> BalancedWords(Text probe, string words, float width)
        {
            var lines = WrapWords(probe, words, width); if (lines.Count != 2) return lines;
            var settings = probe.GetGenerationSettings(Vector2.zero); var gen = probe.cachedTextGeneratorForLayout; var parts = words.Split(' ');
            float Width(string t) => gen.GetPreferredWidth(t, settings) / probe.pixelsPerUnit;
            var best = lines; float bestScore = float.MaxValue;
            for (int k = 1; k < parts.Length; k++)
            {
                string a = string.Join(" ", parts, 0, k), b = string.Join(" ", parts, k, parts.Length - k); float w = Mathf.Max(Width(a), Width(b));
                if (w > width) continue;
                char end = a[a.Length - 1]; string last = parts[k - 1].ToLowerInvariant(); float score = w - (end == '.' || end == '?' || end == '!' ? 60 : end == ',' || end == ';' ? 36 : 0) + (SmallWords.Contains(last) ? 24 : 0);
                if (score < bestScore) { bestScore = score; best = new List<string> { a, b }; }
            }
            return best;
        }
        public static List<string> WrapWords(Text probe, string words, float width)
        {
            var settings = probe.GetGenerationSettings(Vector2.zero); var gen = probe.cachedTextGeneratorForLayout; var lines = new List<string>(); string line = "";
            foreach (var word in words.Split(' '))
            {
                string next = line == "" ? word : line + " " + word;
                if (line != "" && gen.GetPreferredWidth(next, settings) / probe.pixelsPerUnit > width) { lines.Add(line); line = word; } else line = next;
            }
            if (line != "") lines.Add(line); return lines;
        }
        // The Keeper's record on the page's rules: KEEPER, the Keys and Books, then the inscription; the closing flourish and the doors follow.
        // The Big Three's line sits between the Keys and the inscription (owner, Oct 2 late evening: approved, unknowns A).
        void ShowJournalLanding()
        {
            int row = 1; void Place(Text line) { line.rectTransform.anchoredPosition = new Vector2(JournalX, -(KeeperFirst + KeeperRule * row++ - 3 - GaramondLift * line.fontSize)); }
            journalKeeperHeading.text = SliceFlow.KeeperHeading; Place(journalKeeperHeading);
            journalKeys.text = Flow.KeysLine; Place(journalKeys);
            bool record = Flow.BirthDone; bigThreeLine.gameObject.SetActive(record); journalBigThree.gameObject.SetActive(record); // every player's, "☉ unknown" included (Oct 7)
            if (record) { float baseline = KeeperFirst + KeeperRule * row++ - 3; PlaceBigThree(baseline); ((RectTransform)journalBigThree.transform).anchoredPosition = new Vector2(JournalX, -(baseline - 5)); journalBigThree.interactable = Flow.CanOpenBirthPage && !busy && journalBeat == null; }
            // the living inscription (owner, Oct 7): the breaks are worked out before the first letter, so no word jumps lines while it writes.
            // Under the first-ever open's title page, the Oct 2 line's rows are kept and stay blank until the page has faded.
            bool blank = Flow.InscriptionText == "";
            var inscription = blank || Flow.InscriptionId == SliceFlow.FirstLineId ? FirstInscriptionLines(keeperLines[0], Flow.DisplayName) : WrapWords(keeperLines[0], Flow.InscriptionText, KeeperWidth);
            inscriptionLines = blank ? new List<string>() : inscription;
            if (!blank && inscriptionWritten != Flow.InscriptionId) StartInscription();
            for (int i = 0; i < keeperLines.Count; i++)
            {
                bool show = i < inscription.Count; keeperLines[i].gameObject.SetActive(show); if (show) Place(keeperLines[i]);
                if (inscriptionWriting == null) { keeperLines[i].supportRichText = false; keeperLines[i].text = show && !blank ? inscription[i] : ""; } // written: plain text, so a name is never read as a tag
            }
            if (inscription.Count > keeperLines.Count) Debug.LogWarning("[Journal] the inscription needs " + inscription.Count + " lines; the record holds " + keeperLines.Count);
            float closing = KeeperFirst + KeeperRule * row; journalFlourishBottom.anchoredPosition = new Vector2(JournalX, -closing);
            float door = closing + DoorBelow + DoorHeight / 2; journalPracticeDoor.GetComponent<RectTransform>().anchoredPosition = new Vector2(JournalX, -door); journalContentsDoor.GetComponent<RectTransform>().anchoredPosition = new Vector2(JournalX, -(door + DoorStep));
            journalPracticeDoor.interactable = Flow.CanJournalPractice && !busy && journalBeat == null; journalContentsDoor.interactable = !busy && journalBeat == null; // Practice is unavailable (half) until a concept is learned
        }
        // the Oct 2 line keeps its own breaks, each wrapped only if a screen can't fit it
        public static List<string> FirstInscriptionLines(Text probe, string name)
        {
            var lines = WrapWords(probe, SliceFlow.InscriptionGreeting(name), KeeperWidth);
            foreach (var part in SliceFlow.InscriptionBreaks) lines.AddRange(WrapWords(probe, part, KeeperWidth));
            return lines;
        }
        // The ink (the approved scope, Oct 7): each letter fades up over 0.15 s, 35 ms after the one before, with a short rest at a comma or a
        // full stop, so it reads as a pen moving, not a typewriter. It starts 0.3 s after the landing shows; on the first-ever open, as the
        // title page finishes fading. Nothing waits for it: the doors work throughout, and a tap anywhere else on the page finishes the line.
        // Reduced motion shows it whole at once (decision 4); the screen reader hears it whole from the start.
        public const float InkTapBottom = JournalCloseY - 24 - 8; // the tap layer ends above Close the journal (Jeffrey, #128 B1)
        public const float InkFade = .15f, InkStep = .035f, InkComma = .12f, InkStop = .22f, InkDelay = .3f;
        List<string> inscriptionLines = new List<string>(); string inscriptionWritten = ""; Coroutine inscriptionWriting;
        public static float[] InkTimes(IList<string> lines) // when each letter starts, line by line, in seconds after the first
        {
            var times = new List<float>(); float t = 0;
            foreach (var line in lines) foreach (char c in line) { times.Add(t); t += c == ' ' ? InkStep * .5f : InkStep; if (c == ',' || c == ';') t += InkComma; else if (c == '.' || c == '!' || c == '?') t += InkStop; }
            return times.ToArray();
        }
        public static float InkSeconds(IList<string> lines) { var times = InkTimes(lines); return times.Length == 0 ? 0 : times[times.Length - 1] + InkFade; }
        void StartInscription()
        {
            inscriptionWritten = Flow.InscriptionId;
            if (ReducedMotion || Flow.InscriptionText.IndexOf('<') >= 0) return; // whole at once; a "<" in a name would read as a tag while it writes
            inscriptionWriting = StartCoroutine(WriteInscription(new List<string>(inscriptionLines), Flow.InscriptionId == SliceFlow.FirstLineId ? 0 : InkDelay));
        }
        IEnumerator WriteInscription(List<string> lines, float delay)
        {
            var times = InkTimes(lines); var ink = keeperLines[0].color; string hex = ColorUtility.ToHtmlStringRGB(ink);
            for (int i = 0; i < lines.Count && i < keeperLines.Count; i++) keeperLines[i].supportRichText = true;
            float start = Time.unscaledTime + delay;
            while (true)
            {
                float now = Time.unscaledTime - start; int at = 0; bool done = true;
                for (int i = 0; i < lines.Count && i < keeperLines.Count; i++)
                {
                    var text = new System.Text.StringBuilder();
                    foreach (char c in lines[i])
                    {
                        float k = Mathf.Clamp01((now - times[at++]) / InkFade);
                        if (k >= 1 || c == ' ') text.Append(c);
                        else { done = false; text.Append("<color=#").Append(hex).Append(Mathf.RoundToInt(ink.a * k * 255).ToString("X2")).Append('>').Append(c).Append("</color>"); }
                    }
                    keeperLines[i].text = text.ToString();
                }
                if (done || ReducedMotion) break; // Reduced motion turned on mid-line shows it whole (decision 4)
                yield return null;
            }
            FinishInscription();
        }
        void FinishInscription()
        {
            if (inscriptionWriting != null) { StopCoroutine(inscriptionWriting); inscriptionWriting = null; }
            for (int i = 0; i < keeperLines.Count; i++) { keeperLines[i].supportRichText = false; keeperLines[i].text = i < inscriptionLines.Count ? inscriptionLines[i] : ""; }
            Publish();
        }
        void InscriptionTap() { if (inscriptionWriting != null) FinishInscription(); }
        // the line's six pieces set side by side, centred on the page, every piece on the same baseline
        public const float BigThreeSmallestThree = 9f; // the owner, Oct 7: a moon with three options (an older save's "I'm not sure", no time and no place) shrinks further, to fit; 9 px is the whole size that fits 274
        public const float BigThreeRoom = 274f, BigThreeSmallest = 11f; // a long line (a moon that changed sign: "Pisces or Aries") may use the page's calm column, x 50 to 325, and shrinks to fit it, to 11 px at the least (the longest of all, a Sagittarius sun with a Sagittarius-or-Capricorn moon); "Cancer or Leo" with a Taurus sun takes 16
        void PlaceBigThree(float baseline)
        {
            bigThreeLine.anchoredPosition = new Vector2(JournalX, -baseline); string[] words = { SliceFlow.SignOrUnknown(Flow.SunSign), Flow.MoonWords, SliceFlow.SignOrUnknown(Flow.RisingSign) }; float total = 0, scale = 1;
            float floor = Flow.MoonOptions.Length >= 3 ? BigThreeSmallestThree : BigThreeSmallest; // Oct 7: only the rare three-option moon goes below 11 px, to fit inside the page's border
            for (int pass = 0; pass < 3; pass++)
            {
                total = 0;
                for (int i = 0; i < 3; i++) { bigThreeGlyphs[i].fontSize = Mathf.RoundToInt(BigThreeMarkSizes[i] * scale); bigThreeWords[i].fontSize = Mathf.RoundToInt(17 * scale); bigThreeWords[i].text = " " + words[i] + (i < 2 ? " \u00b7 " : ""); total += bigThreeGlyphs[i].preferredWidth + bigThreeWords[i].preferredWidth; }
                if (total <= BigThreeRoom || scale <= floor / 17f + .001f) break;
                scale = Mathf.Max(floor / 17f, scale * BigThreeRoom / total * .99f);
            }
            float x = -total / 2;
            for (int i = 0; i < 3; i++)
                foreach (var piece in new[] { bigThreeGlyphs[i], bigThreeWords[i] })
                {
                    float w = piece.preferredWidth, lift = piece == bigThreeGlyphs[i] ? BigThreeMarkLift[i] : GaramondLift;
                    piece.rectTransform.sizeDelta = new Vector2(w + 2, piece.rectTransform.sizeDelta.y); piece.rectTransform.anchoredPosition = new Vector2(x + w / 2, lift * piece.fontSize); x += w;
                }
        }
        void ShowJournalFront(JournalView at)
        {
            bool beat = journalBeat != null;
            journalTitlePage.gameObject.SetActive(at == JournalView.Title); journalLanding.gameObject.SetActive(at == JournalView.Landing || at == JournalView.Title);
            journalContents.gameObject.SetActive(at == JournalView.Contents); journalMap.gameObject.SetActive(at == JournalView.Map);
            journalPractice.gameObject.SetActive(at == JournalView.Practice); journalQuiz.gameObject.SetActive(at == JournalView.Quiz); journalBirth.gameObject.SetActive(at == JournalView.Birth);
            journalHomeLink.gameObject.SetActive(at == JournalView.Contents || at == JournalView.Practice || at == JournalView.Birth); journalContentsLink.gameObject.SetActive(at == JournalView.Wheel || at == JournalView.Map); journalPracticeLink.gameObject.SetActive(at == JournalView.Quiz);
            journalHomeLink.interactable = journalContentsLink.interactable = journalPracticeLink.interactable = !busy;
            if (inscriptionWriting != null && at != JournalView.Landing) FinishInscription(); // back from Contents, Practice or a chapter, it shows written
            if (at == JournalView.Landing || at == JournalView.Title) ShowJournalLanding();
            if (at == JournalView.Practice) ShowJournalPractice();
            if (at == JournalView.Quiz) ShowJournalQuiz();
            if (at == JournalView.Birth) ShowBirthPage();
            foreach (var row in journalChapterRows) if (row != null) row.interactable = !busy;
            for (int i = 0; i < journalMapNames.Length; i++) journalMapNames[i].gameObject.SetActive(TravelOpen(TravelMenu.Rooms[i].id)); // only the rooms you have woken are named
            if (!beat) { journalTitlePageGroup.alpha = 1; journalLandingGroup.alpha = 1; journalTitleGroup.alpha = 1; journalLandingGroup.blocksRaycasts = true; }
            if (at == JournalView.Title && !beat) journalBeat = StartCoroutine(JournalTitleBeat());
        }
        // "Your Birth" (owner, Oct 7): each fact as given or unknown, Add beside a missing one; under the rows, what changed (after an Add or a
        // choice), else the journal's paragraph while the rising is unknown or chosen; then Choose / Change my rising and Add my birth time
        static readonly string[] MonthNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        public static string BirthDateWords(BirthFacts f) => f.HasDate ? f.Day + " " + MonthNames[f.Month - 1] + " " + f.Year : SliceFlow.Unknown;
        public static string BirthTimeWords(BirthFacts f) => f.ExactTime ? SliceFlow.ClockTime(f.timeFrom) : f.HasTime ? SliceFlow.ClockTime(f.timeFrom) + " to " + SliceFlow.ClockTime(f.timeTo) : SliceFlow.Unknown;
        public static string BirthPlaceWords(BirthFacts f) => f.HasPlace ? f.place : SliceFlow.Unknown;
        void ShowBirthPage()
        {
            var f = Flow.Facts; string[] values = { BirthDateWords(f), BirthTimeWords(f), BirthPlaceWords(f) }; bool[] missing = { !f.HasDate, !f.HasTime, !f.HasPlace };
            for (int i = 0; i < 3; i++)
            {
                var value = birthRowValues[i]; value.text = values[i]; value.fontSize = 16; float room = missing[i] ? BirthAddX - BirthAddWidth / 2 - 8 - BirthValueLeft : 326 - BirthValueLeft;
                while (value.preferredWidth > room && value.fontSize > 11) value.fontSize--; // a long place name shrinks to fit, like the Big Three's line
                birthAddButtons[i].gameObject.SetActive(missing[i]); birthAddButtons[i].interactable = Flow.CanAddFacts && !busy;
            }
            var words = Flow.BirthLines.Count > 0 ? Flow.BirthLines : Flow.ShowsRisingWhy ? new List<string> { SliceFlow.RisingWhy, Flow.RisingChosen ? SliceFlow.RisingKept : SliceFlow.RisingMeanwhile } : new List<string>();
            var lines = new List<string>(); foreach (var w in words) lines.AddRange(BalancedWords(birthPageLines[0], w, BirthLineWidth));
            float baseline = BirthRowTop + BirthRowHeight * 3 + 36;
            for (int i = 0; i < birthPageLines.Count; i++) { bool show = i < lines.Count; birthPageLines[i].gameObject.SetActive(show); birthPageLines[i].text = show ? lines[i] : ""; if (show) SetLine(birthPageLines[i], baseline + QuizLineStep * i); }
            if (lines.Count > birthPageLines.Count) Debug.LogWarning("[Journal] \"Your Birth\" needs " + lines.Count + " lines; the page holds " + birthPageLines.Count);
            float top = baseline + QuizLineStep * (Mathf.Max(1, Mathf.Min(lines.Count, birthPageLines.Count)) - 1) + 30;
            birthRisingButton.gameObject.SetActive(Flow.CanChooseRising); birthRisingButton.GetComponent<ButtonFeel>().Label.text = Flow.RisingChosen ? SliceFlow.ChangeRising : SliceFlow.ChooseRising; birthRisingButton.interactable = !busy;
            if (Flow.CanChooseRising) { ((RectTransform)birthRisingButton.transform).anchoredPosition = new Vector2(JournalX, -(top + ChoiceHeight / 2)); top += ChoiceStep; }
            birthTimeButton.gameObject.SetActive(Flow.CanAddTime); birthTimeButton.interactable = !busy;
            if (Flow.CanAddTime) ((RectTransform)birthTimeButton.transform).anchoredPosition = new Vector2(JournalX, -(top + ChoiceHeight / 2));
        }
        void JournalBirthOpen() { if (busy || journalBeat != null || !Flow.OpenBirthPage()) return; Sound.Play("page"); ShowJournal(); Publish(); } // the Big Three's line (Oct 7)
        void BirthAdd(bool forTime) { if (busy || !Flow.StartAddFacts(forTime)) return; Sound.Play("page"); placeMatches.Clear(); cuspWhyOpen = false; foreach (var f in new[] { dateField, timeField, placeField }) if (f != null) f.SetTextWithoutNotify(""); Show(); Publish(); }
        void BirthChooseRising() { if (busy || !Flow.StartChooseRising()) return; Sound.Play("page"); Show(); Publish(); }
        // Practice's list: a row for each concept learned, in the data's order; the rest hidden
        void ShowJournalPractice()
        {
            var learned = Flow.PracticeConcepts;
            for (int i = 0; i < practiceRows.Length; i++)
            {
                bool show = i < learned.Count; practiceRows[i].gameObject.SetActive(show); practiceRowRules[i].gameObject.SetActive(show);
                if (!show) continue;
                practiceRows[i].GetComponent<ButtonFeel>().Label.text = learned[i].name; practiceRowLines[i].text = learned[i].line; practiceRows[i].interactable = !busy;
            }
        }
        void SetLine(Text line, float baseline) { line.rectTransform.anchoredPosition = new Vector2(JournalX, -(baseline - GaramondLift * line.fontSize)); }
        // A question as the board sets it: the counter, a sign's mark for the Symbols, the ask in lines 26 apart, the choices 52 apart below it,
        // then (once answered) the marks, the why and Next; at the round's end, the journal's line and Back to Practice under it
        void ShowJournalQuiz()
        {
            var q = Flow.QuizQuestion; bool over = Flow.QuizOver, answered = !over && Flow.QuizPicked >= 0;
            quizCounter.text = Flow.QuizCounter; quizCounter.gameObject.SetActive(!over);
            bool mark = !over && q != null && q.glyph >= 0 && q.glyph < 12; quizGlyph.gameObject.SetActive(mark); quizGlyph.text = mark ? Zodiac.Seats[q.glyph].Glyph : "";
            var ask = over || q == null ? new List<string>() : BalancedWords(quizAsk[0], q.ask, QuizAskWidth);
            int whyLines = over || q == null ? 1 : Mathf.Max(BalancedWords(quizFeedback[0], PracticeBook.Data.right + " " + q.why, QuizAskWidth).Count, BalancedWords(quizFeedback[0], PracticeBook.Data.wrong + " " + q.why, QuizAskWidth).Count);
            var layout = QuizLayout(mark, ask.Count, Flow.QuizChoices.Length, whyLines); float first = layout.x, step = layout.z; // the ask's first baseline: 197, or 223 under a sign's symbol
            for (int i = 0; i < quizAsk.Length; i++) { bool show = i < ask.Count; quizAsk[i].gameObject.SetActive(show); quizAsk[i].text = show ? ask[i] : ""; if (show) SetLine(quizAsk[i], first + QuizLineStep * i); }
            float top = layout.y; var choices = Flow.QuizChoices; int right = Flow.QuizRight, picked = Flow.QuizPicked;
            quizChoicesGroup.blocksRaycasts = !answered && !busy;
            for (int i = 0; i < quizChoices.Length; i++)
            {
                bool show = i < choices.Length; quizChoices[i].gameObject.SetActive(show);
                if (!show) continue;
                ((RectTransform)quizChoices[i].transform).anchoredPosition = new Vector2(JournalX, -(top + step * i + ChoiceHeight / 2));
                var feel = quizChoices[i].GetComponent<ButtonFeel>(); feel.Label.text = choices[i];
                bool isRight = answered && i == right, isWrong = answered && i == picked && i != right;
                quizChoicePanels[i].color = isRight ? GiltWash : VellumPanel; quizThin[i].gameObject.SetActive(!isRight && !isWrong); quizThick[i].gameObject.SetActive(isRight || isWrong);
                foreach (var edge in quizThick[i].GetComponentsInChildren<Image>(true)) edge.color = isWrong ? CrimsonEdge : GiltInk;
                quizStars[i].gameObject.SetActive(isRight); quizCrosses[i].gameObject.SetActive(isWrong);
                feel.LabelUp = isWrong ? CrimsonInk : GiltInk; feel.Label.color = feel.LabelUp; quizChoiceGroups[i].alpha = answered && !isRight && !isWrong ? .5f : 1;
                quizChoices[i].interactable = !busy;
            }
            float below = top + step * (choices.Length - 1) + ChoiceHeight; // the last choice's bottom
            var why = answered ? BalancedWords(quizFeedback[0], Flow.QuizFeedback, QuizAskWidth) : new List<string>();
            for (int i = 0; i < quizFeedback.Length; i++) { bool show = i < why.Count; quizFeedback[i].gameObject.SetActive(show); quizFeedback[i].text = show ? why[i] : ""; if (show) SetLine(quizFeedback[i], below + 37 + QuizLineStep * i); }
            quizNext.gameObject.SetActive(answered); quizNext.interactable = Flow.CanQuizNext && !busy;
            if (answered) ((RectTransform)quizNext.transform).anchoredPosition = new Vector2(JournalX, -(layout.w + ChoiceHeight / 2));
            var end = over ? BalancedWords(quizEnd[0], Flow.QuizEnd, QuizAskWidth + 10) : new List<string>();
            for (int i = 0; i < quizEnd.Length; i++) { bool show = i < end.Count; quizEnd[i].gameObject.SetActive(show); quizEnd[i].text = show ? end[i] : ""; if (show) SetLine(quizEnd[i], QuizAskY + QuizLineStep * i); }
            quizBack.gameObject.SetActive(over); quizBack.interactable = Flow.CanQuizBack && !busy;
            if (over) ((RectTransform)quizBack.transform).anchoredPosition = new Vector2(JournalX, -(QuizAskY + QuizLineStep * (Mathf.Max(1, end.Count) - 1) + 39 + ChoiceHeight / 2));
            if (ask.Count > quizAsk.Length || why.Count > quizFeedback.Length || end.Count > quizEnd.Length) Debug.LogWarning("[Practice] a line runs past the page's lines: " + (q != null ? q.ask : Flow.QuizEnd));
        }
        // 1e A: the title page settles for a second, then fades into the landing, which shows under it from the start
        IEnumerator JournalTitleBeat()
        {
            journalTitlePageGroup.alpha = 1; journalLandingGroup.alpha = 0; journalTitleGroup.alpha = 0; journalLandingGroup.blocksRaycasts = false;
            yield return new WaitForSecondsRealtime(TitleSettle);
            yield return Tween(TitleFade, k => { journalTitlePageGroup.alpha = 1 - k; journalLandingGroup.alpha = k; journalTitleGroup.alpha = k; });
            journalBeat = null; Flow.JournalLand(); Save();
            if (Flow.AtJournal) { ShowJournal(); Publish(); }
        }
        // A seat's ring and picture from its state (SliceFlow.SeatState): met silver, practising gold, mastered gold leaf; the picture's ink and colour from the deck.
        void DressSeatPicture(Image picture, Material material, Image ring, int seat, int state)
        {
            var file = Slots.Image(SignSlot(seat)); picture.sprite = file; picture.color = file != null ? Color.white : PanelColor;
            float ink = Flow.SignInk(seat), colour = Flow.SignColour(seat);
            if (material != null) { material.SetFloat("_Saturation", colour); material.SetFloat("_Ink", ink); } else if (file != null) picture.color = new Color(1, 1, 1, ink);
            var leaf = Slots.Image("journal-seat-leaf"); var line = Slots.Image("journal-seat-line");
            ring.sprite = state == 3 && leaf != null ? leaf : line != null ? line : Disc();
            ring.color = state == 3 ? (leaf != null ? Color.white : Gilt) : state == 2 ? Gilt : new Color(Silver.r, Silver.g, Silver.b, .6f);
            if (line == null && state != 3) ring.color = new Color(ring.color.r, ring.color.g, ring.color.b, .25f);
        }
        Color LensColour(int seat)
        {
            switch (Flow.Lens)
            {
                case JournalLens.Modality: return ModalityColours[seat % 3];
                case JournalLens.Polarity: return Zodiac.PolarityAt(seat) == "Yang" ? Gilt : Silver;
                case JournalLens.Opposites: return Flow.JournalSelected >= 0 && (seat == Flow.JournalSelected || seat == Zodiac.Opposite(Flow.JournalSelected)) ? Gilt : Silver;
                default: return ElementColour(seat);
            }
        }
        void ShowJournal()
        {
            var at = Flow.JournalAt; bool sign = at == JournalView.Sign, wheel = at == JournalView.Wheel, table = wheel && Flow.JournalTableView;
            var lenses = Flow.JournalLenses; var met = Flow.JournalSigns;
            journalTitle.fontSize = TitleSize; journalTitle.text = JournalPageTitle(at); journalSignGlyph.gameObject.SetActive(false);
            if (at != JournalView.Sign && journalTitle.preferredWidth > TitleRoom) journalTitle.fontSize = Mathf.FloorToInt(TitleSize * TitleRoom / journalTitle.preferredWidth); // fits between the corner flourishes
            journalViewWheel.gameObject.SetActive(wheel && Flow.CanJournalTable); journalViewTable.gameObject.SetActive(wheel && Flow.CanJournalTable);
            PaintChoice(journalViewWheel, !table); PaintChoice(journalViewTable, table); journalViewWheel.interactable = journalViewTable.interactable = !busy;
            for (int i = 0; i < journalLensButtons.Length; i++)
            {
                int place = lenses.IndexOf((JournalLens)i); bool show = wheel && lenses.Count > 1 && place >= 0; journalLensButtons[i].gameObject.SetActive(show);
                if (!show) continue;
                journalLensButtons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(LensX(place, lenses.Count), -TabsY); PaintChoice(journalLensButtons[i], Flow.Lens == (JournalLens)i); journalLensButtons[i].interactable = !busy;
            }
            journalWheelLayer.gameObject.SetActive(wheel && !table); journalTableLayer.gameObject.SetActive(table);
            // the pattern's lines on the wheel: the element triangles, the modality crosses, or the six opposites across (Polarity draws none)
            for (int i = 0; i < journalLines.Length; i++) journalLines[i].gameObject.SetActive(false);
            if (wheel && !table)
            {
                int n = 0;
                if (Flow.Lens == JournalLens.Element || Flow.Lens == JournalLens.Modality)
                {
                    int groups = Flow.Lens == JournalLens.Element ? 4 : 3, size = 12 / groups;
                    for (int g = 0; g < groups; g++) for (int k = 0; k < size; k++)
                    {
                        int from = g + k * groups, to = g + (k + 1) % size * groups; var line = journalLines[n++];
                        line.gameObject.SetActive(true); PlaceLine(line, SeatCentre(from, false), SeatCentre(to, false), 1.5f);
                        var c = Flow.Lens == JournalLens.Element ? ElementColour(g) : ModalityColours[g]; line.color = new Color(c.r, c.g, c.b, .45f);
                    }
                }
                else if (Flow.Lens == JournalLens.Opposites)
                    for (int p = 0; p < 6; p++)
                    {
                        var line = journalLines[n++]; bool lit = Flow.JournalSelected >= 0 && Flow.JournalSelected % 6 == p;
                        line.gameObject.SetActive(true); PlaceLine(line, SeatCentre(p, false), SeatCentre(p + 6, false), lit ? 2.5f : 1f); line.color = lit ? Gilt : new Color(Silver.r, Silver.g, Silver.b, .3f);
                    }
            }
            for (int seat = 0; seat < 12; seat++)
            {
                var parts = journalSeats[seat]; parts.root.gameObject.SetActive(wheel);
                if (!wheel) continue;
                int state = Flow.SeatState(seat); var centre = SeatCentre(seat, table); float scale = table ? TableSeat / SeatHit : 1;
                parts.root.anchoredPosition = new Vector2(centre.x, -centre.y); parts.root.localScale = Vector3.one * (table ? 1.1f : 1);
                parts.picture.transform.parent.gameObject.SetActive(state > 0); parts.ring.gameObject.SetActive(state > 0 || table);
                if (state > 0) DressSeatPicture(parts.picture, parts.illumination, parts.ring, seat, state);
                else { parts.ring.sprite = RingSprite(); parts.ring.color = new Color(Silver.r, Silver.g, Silver.b, .2f); }
                bool dim = Flow.Lens == JournalLens.Polarity && Zodiac.PolarityAt(seat) != "Yang" || Flow.Lens == JournalLens.Opposites && Flow.JournalSelected >= 0 && seat % 6 != Flow.JournalSelected % 6;
                parts.group.alpha = dim ? .45f : 1;
                parts.frame.gameObject.SetActive(seat == Flow.JournalSelected); parts.frame.sprite = RingSprite();
                bool glyph = Flow.SignKnows(seat, ItemKind.Glyph); var tone = LensColour(seat);
                parts.badge.gameObject.SetActive(state > 0); parts.badge.color = glyph ? new Color(.05f, .06f, .11f) : tone;
                parts.badge.rectTransform.sizeDelta = Vector2.one * (glyph ? 16 : 7); parts.badge.rectTransform.anchoredPosition = glyph ? new Vector2(15, -(SeatHit / 2 + 15)) : new Vector2(0, -(SeatHit / 2 + 22));
                parts.glyph.text = glyph ? Zodiac.Seats[seat].Glyph : ""; parts.glyph.color = tone;
                parts.ribbon.gameObject.SetActive(state > 0 && Flow.SignDue(seat));
                parts.hit.interactable = state > 0 && !busy;
            }
            ShowJournalCard(wheel);
            journalSignPage.gameObject.SetActive(sign);
            if (sign) ShowJournalSign();
            journalPrev.gameObject.SetActive(sign); journalNext.gameObject.SetActive(sign); journalBack.gameObject.SetActive(sign);
            journalPrev.interactable = Flow.CanJournalPrev && !busy; journalNext.interactable = Flow.CanJournalNext && !busy; journalBack.interactable = Flow.CanJournalWheel && !busy; journalClose.interactable = !busy;
            foreach (var bar in journalChevrons[0]) bar.color = journalPrev.interactable ? Bone : new Color(Muted.r, Muted.g, Muted.b, .4f);
            foreach (var bar in journalChevrons[1]) bar.color = journalNext.interactable ? Bone : new Color(Muted.r, Muted.g, Muted.b, .4f);
            ShowJournalFront(at);
        }
        // The preview: the tapped seat's picture, name and symbol, its facts in one line (on the Opposites tab, what the pair shares), and Open the page.
        void ShowJournalCard(bool wheel)
        {
            int seat = Flow.JournalSelected; bool show = wheel && seat >= 0; journalCard.gameObject.SetActive(show);
            if (!show) return;
            var name = Zodiac.Seats[seat].Name; bool glyph = Flow.SignKnows(seat, ItemKind.Glyph);
            DressSeatPicture(journalCardPicture, journalCardIllumination, journalCardRing, seat, Flow.SeatState(seat));
            string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
            if (Flow.Lens == JournalLens.Opposites && Flow.SignKnows(seat, ItemKind.Opposite))
            {
                int other = Zodiac.Opposite(seat);
                journalCardTitle.text = name + " <size=13>and</size> " + Zodiac.Seats[other].Name; journalCardTitle.fontSize = 19;
                journalCardLine.text = SliceFlow.PairShares(seat) + ": <color=" + Hex(ElementColour(seat)) + ">" + Zodiac.Seats[seat].Element + "</color> / <color=" + Hex(ElementColour(other)) + ">" + Zodiac.Seats[other].Element + "</color>";
            }
            else
            {
                journalCardTitle.text = name; journalCardTitle.fontSize = 22;
                var summary = Flow.SeatSummary(seat); var element = Zodiac.Seats[seat].Element;
                journalCardLine.text = Flow.SignKnows(seat, ItemKind.Element) && summary.StartsWith(element) ? "<color=" + Hex(ElementColour(seat)) + ">" + element + "</color>" + summary.Substring(element.Length) : summary;
            }
            journalCardLine.horizontalOverflow = HorizontalWrapMode.Wrap; journalCardLine.verticalOverflow = VerticalWrapMode.Overflow;
            // the symbol, beside the name (a second font, so its own label)
            var mark = journalCardMark;
            mark.gameObject.SetActive(glyph && Flow.Lens != JournalLens.Opposites); mark.text = Zodiac.Seats[seat].Glyph; mark.color = ElementColour(seat);
            mark.rectTransform.anchoredPosition = new Vector2(38 - 86 + journalCardTitle.preferredWidth + 12, -20);
            journalOpen.interactable = Flow.CanOpenSelected && !busy;
        }
        void ShowJournalSign()
        {
            int seat = Flow.JournalSignSeat; var name = Zodiac.Seats[seat].Name; int state = Flow.SeatState(seat);
            journalTitle.fontSize = SignTitleSize; journalTitle.text = SignTitle(name, Gilt); // the illuminated capital
            bool glyph = Flow.SignKnows(seat, ItemKind.Glyph); journalSignGlyph.gameObject.SetActive(glyph);
            journalSignGlyph.text = Zodiac.Seats[seat].Glyph; journalSignGlyph.color = ElementColour(seat); journalSignGlyph.rectTransform.anchoredPosition = new Vector2(JournalX + journalTitle.preferredWidth / 2 + 16, -60);
            DressSeatPicture(journalSignArt, journalIllumination, journalSignRing, seat, state);
            // the ribbon: its length is the ladder climbed; due, it is pulled out past the page's head edge (position and length carry it, not colour alone)
            float length = 40 + Flow.SignLadder(seat) * 20, top = Flow.SignDue(seat) ? JournalTop - 22 : JournalTop;
            journalRibbon.rectTransform.sizeDelta = new Vector2(16, length); journalRibbon.rectTransform.anchoredPosition = new Vector2(92, -(top + length / 2));
            // a panel per fact learned, in reading order; Element, Modality and Opposite link the signs that share them
            for (int i = 0; i < journalChips.Length; i++) journalChips[i].gameObject.SetActive(false);
            var facts = Flow.SignFacts(seat).Where(f => f[0] != "Opposite").ToList(); bool opposite = Flow.SignKnows(seat, ItemKind.Opposite);
            var rows = new List<(string label, string value, Color colour, ItemKind kind, string note)>();
            foreach (var f in facts)
            {
                var kind = f[0] == "Element" ? ItemKind.Element : f[0] == "Modality" ? ItemKind.Modality : ItemKind.Opposite; // Polarity rides on the opposites item
                if (f[0] == "Polarity") rows.Add((f[0], f[1], PageText, kind, SliceFlow.PolarityWords(seat)));
                else rows.Add((f[0], f[1], f[0] == "Element" ? ElementColour(seat) : PageText, kind, ""));
            }
            if (opposite) rows.Add(("Opposite", "", PageText, ItemKind.Opposite, SliceFlow.PairShares(seat)));
            float y = SignPictureTop + SignPictureSize / 2 + 26; int chip = 0;
            for (int i = 0; i < journalPanels.Length; i++)
            {
                bool show = i < rows.Count; journalPanels[i].gameObject.SetActive(show);
                if (!show) continue;
                var row = rows[i]; journalPanelLabels[i].text = row.label.ToUpperInvariant(); journalPanelValues[i].text = row.value; journalPanelValues[i].color = row.colour;
                var kin = row.label == "Polarity" ? new List<int>() : Flow.SignKin(seat, row.kind); float height = 30, x = -JournalWidth / 2 + 11, chipTop = 34;
                if (row.label == "Opposite") { x = JournalWidth / 2 - 11; chipTop = 14; } // the opposite's chip stands where a value would
                else if (kin.Count > 0) { journalPanelNotes[i].text = "with"; x += 28; height = 54; }
                journalPanelNotes[i].text = row.label == "Opposite" || row.label == "Polarity" ? row.note : kin.Count > 0 ? "with" : "";
                foreach (var other in kin)
                {
                    if (chip >= journalChips.Length) break;
                    var b = journalChips[chip]; var label = b.GetComponentInChildren<Text>(); bool mark = Flow.SignKnows(other, ItemKind.Glyph);
                    label.text = Zodiac.Seats[other].Name; float w = label.preferredWidth + (mark ? 26 : 14);
                    if (row.label != "Opposite" && x + w > JournalWidth / 2 - 8) { x = -JournalWidth / 2 + 39; chipTop += 24; height += 24; } // wrap to a second line
                    float cx = row.label == "Opposite" ? x - w / 2 : x + w / 2;
                    b.transform.SetParent(journalPanels[i], false); var r = b.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(.5f, 1); r.sizeDelta = new Vector2(w, 20); r.anchoredPosition = new Vector2(cx, -chipTop);
                    journalChipGlyphs[chip].gameObject.SetActive(mark); journalChipGlyphs[chip].text = Zodiac.Seats[other].Glyph; journalChipGlyphs[chip].color = ElementColour(other); journalChipGlyphs[chip].rectTransform.anchoredPosition = new Vector2(-w / 2 + 10, -10);
                    b.gameObject.SetActive(true); b.interactable = !busy; journalChipSeats[chip++] = other;
                    if (row.label != "Opposite") x += w + 5;
                }
                if (row.label == "Opposite" || row.label == "Polarity") height = 50;
                journalPanelNotes[i].rectTransform.anchoredPosition = new Vector2(row.label == "Opposite" || row.label == "Polarity" ? 0 : -JournalWidth / 2 + 11 + 12, -(row.label == "Opposite" || row.label == "Polarity" ? 36 : 34));
                journalPanelNotes[i].rectTransform.sizeDelta = new Vector2(row.label == "Opposite" || row.label == "Polarity" ? JournalWidth - 22 : 26, 18);
                journalPanels[i].sizeDelta = new Vector2(JournalWidth, height); journalPanels[i].anchoredPosition = new Vector2(JournalX, -(y + height / 2)); y += height + 6;
            }
        }
        // Practice's question page as words: the counter, the ask and the choices, then the why; a sign's symbol is only said to be there
        // (a screen reader would read the symbol's name aloud, which is the answer)
        string QuizSpoken()
        {
            if (!Flow.OnQuiz) return "Your journal. " + SliceFlow.PracticeTitle + ".";
            var lead = "Your journal. " + Flow.QuizConcept.name + ". "; if (Flow.QuizOver) return lead + Flow.QuizEnd;
            var q = Flow.QuizQuestion; var mark = q.glyph >= 0 && q.glyph < 12 ? "A sign's symbol is drawn. " : "";
            return lead + Flow.QuizCounter + ". " + mark + q.ask + " Choices: " + string.Join(", ", Flow.QuizChoices) + "." + (Flow.QuizPicked >= 0 ? " " + Flow.QuizFeedback : "");
        }
        // What a screen reader hears: the page as words, and what the rings and ribbons mean, since they are only seen. placeholder (owner writes)
        string JournalSpoken()
        {
            switch (Flow.JournalAt)
            {
                case JournalView.Title: return "Your journal. " + SliceFlow.KeeperHeading.Substring(0, 1) + SliceFlow.KeeperHeading.Substring(1).ToLowerInvariant() + ". " + SliceFlow.TitlePageLine;
                case JournalView.Landing: return "Your journal. Keeper: " + Flow.KeysLine + ". " + Flow.InscriptionText + " " + SliceFlow.PracticeDoor + (SliceFlow.PracticeDoorLine != "" ? ": " + SliceFlow.PracticeDoorLine.ToLowerInvariant() : "") + (Flow.PracticeConcepts.Count > 0 ? ". " : ", not open yet. ") + SliceFlow.ContentsDoor + ": " + SliceFlow.ContentsDoorLine.ToLowerInvariant() + ".";
                case JournalView.Contents: return "Your journal. " + SliceFlow.ContentsTitle + ". " + SliceFlow.WheelTitle + ": " + SliceFlow.WheelChapterLine.ToLowerInvariant() + ". " + SliceFlow.MapTitle + ": " + SliceFlow.MapChapterLine.ToLowerInvariant() + ". " + SliceFlow.SealedChapters + " chapters sealed.";
                case JournalView.Map: return "Your journal. " + SliceFlow.MapTitle + ". " + SliceFlow.MapChapterLine + ": " + string.Join(", ", TravelMenu.Rooms.Where(r => TravelOpen(r.id)).Select(r => r.name)) + ".";
                case JournalView.Practice: return "Your journal. " + SliceFlow.PracticeTitle + ". " + string.Join(" ", Flow.PracticeConcepts.Select(c => c.name + ": " + c.line + "."));
                case JournalView.Quiz: return QuizSpoken();
                case JournalView.Birth:
                    {
                        var f = Flow.Facts; var heard = "Your journal. " + SliceFlow.BirthPageTitle + ". " + SliceFlow.DateRow + ": " + BirthDateWords(f) + ". " + SliceFlow.TimeRow + ": " + BirthTimeWords(f) + ". " + SliceFlow.PlaceRow + ": " + BirthPlaceWords(f) + ".";
                        var said = Flow.BirthLines.Count > 0 ? Flow.BirthLines : Flow.ShowsRisingWhy ? new List<string> { SliceFlow.RisingWhy, Flow.RisingChosen ? SliceFlow.RisingKept : SliceFlow.RisingMeanwhile } : new List<string>();
                        return heard + (said.Count > 0 ? " " + string.Join(" ", said) : "");
                    }
            }
            if (Flow.JournalAt == JournalView.Wheel)
            {
                var lead = "Your journal. " + SliceFlow.WheelTitle + (Flow.JournalTableView ? ", as the table" : "") + (Flow.JournalLenses.Count > 1 ? ", showing " + SliceFlow.LensTitle(Flow.Lens).ToLowerInvariant() : "") + ". ";
                var signs = Flow.JournalSigns.Select(s => Zodiac.Seats[s].Name + ", " + SliceFlow.SeatStateNames[Flow.SeatState(s)] + (Flow.SignDue(s) ? ", ready for practice" : ""));
                var preview = Flow.JournalSelected >= 0 ? " Selected: " + Zodiac.Seats[Flow.JournalSelected].Name + ". " + Flow.SeatSummary(Flow.JournalSelected) + "." : "";
                return lead + string.Join("; ", signs) + "." + preview;
            }
            int seat = Flow.JournalSignSeat; var facts = Flow.SignFacts(seat).Select(f => f[0] + ": " + f[1]).ToList();
            if (Flow.SignKnows(seat, ItemKind.Glyph)) facts.Insert(0, "Symbol: " + Zodiac.Seats[seat].Glyph);
            return "Your journal. " + Zodiac.Seats[seat].Name + ", " + SliceFlow.SeatStateNames[Flow.SeatState(seat)] + ". " + string.Join(". ", facts) + (Flow.SignDue(seat) ? ". Ready for practice." : ".");
        }
        void OpenJournal() { if (busy || !Flow.OpenJournal()) return; Sound.Play("page"); if (Flow.JournalAt == JournalView.Landing) Save(); Show(); Publish(); } // saved now: the line just picked must not come round next time, even if the game closes (Oct 7); the first-ever open saves after its title beat, so a close mid-beat brings the title page back (Jeffrey, #128 B2)
        void CloseJournal() { if (busy || !Flow.CloseJournal()) return; if (journalBeat != null) { StopCoroutine(journalBeat); journalBeat = null; } Sound.Play("page"); Save(); Show(); Publish(); }
        void JournalContents() { if (busy || !Flow.JournalToContents()) return; Sound.Play("page"); ShowJournal(); Publish(); } // the Contents door, and "‹ Contents" on a chapter
        void JournalHome() { if (busy || !Flow.JournalToLanding()) return; Sound.Play("page"); ShowJournal(); Publish(); }   // "‹ Your Journal" on Contents
        void JournalChapter(string chapter) { if (busy || !Flow.OpenChapter(chapter)) return; Sound.Play("page"); ShowJournal(); Publish(); }
        void JournalPracticeOpen() { if (busy || journalBeat != null || !Flow.OpenPractice()) return; Sound.Play("page"); ShowJournal(); Publish(); } // the landing's Practice door (owner, Oct 3)
        void StartPracticeRound(int row) { if (busy || !Flow.StartQuiz(row)) return; Sound.Play("page"); ShowJournal(); Publish(); }
        void PickQuizChoice(int choice) { if (busy || !Flow.AnswerQuiz(choice)) return; ShowJournal(); Publish(); }
        void QuizNext() { if (busy || !Flow.NextQuiz()) return; Sound.Play("page"); ShowJournal(); Publish(); }
        void QuizBack() { if (busy || !Flow.QuizToPractice()) return; Sound.Play("page"); ShowJournal(); Publish(); } // "‹ Practice", and Back to Practice at the round's end
        void JournalTurn(int direction) { if (busy || !(direction > 0 ? Flow.JournalNext() : Flow.JournalPrev())) return; Sound.Play("page"); ShowJournal(); Publish(); }
        void JournalWheel() { if (busy || !Flow.JournalToWheel()) return; Sound.Play("page"); ShowJournal(); Publish(); }
        void JournalSetView(bool table) { if (busy || !Flow.SetJournalTable(table)) return; ShowJournal(); Publish(); }
        void JournalLensTap(JournalLens lens) { if (busy || !Flow.SetLens(lens)) return; ShowJournal(); Publish(); }
        // a tap frames a seat and shows its preview; a second tap on the framed seat opens its page (working choice)
        void JournalSeatTap(int seat) { if (busy) return; if (Flow.JournalSelected == seat && Flow.CanOpenSelected) { JournalOpenSelected(); return; } if (!Flow.SelectSeat(seat)) return; ShowJournal(); Publish(); }
        void JournalOpenSelected() { if (busy || !Flow.OpenSelected()) return; Sound.Play("page"); ShowJournal(); Publish(); }
        void JournalChipTap(int chip) { if (busy || chip < 0 || chip >= journalChips.Length || !journalChips[chip].gameObject.activeSelf || !Flow.OpenSign(journalChipSeats[chip])) return; Sound.Play("page"); ShowJournal(); Publish(); }
        // The chips' boxes on the 360 x 800 layout, for the web template's semantic buttons (x, top, width, height per chip shown)
        float[] JournalChipBoxes()
        {
            var boxes = new List<float>();
            for (int i = 0; i < journalChips.Length; i++)
            {
                if (!journalChips[i].gameObject.activeInHierarchy) continue;
                var r = journalChips[i].GetComponent<RectTransform>(); var panel = r.parent as RectTransform;
                boxes.Add(panel.anchoredPosition.x + r.anchoredPosition.x); boxes.Add(-panel.anchoredPosition.y - panel.sizeDelta.y / 2 - r.anchoredPosition.y); boxes.Add(r.sizeDelta.x); boxes.Add(r.sizeDelta.y);
            }
            return boxes.ToArray();
        }
        // Rich text without its tags (the web state carries words, not markup)
        static string PlainText(string text)
        {
            var sb = new System.Text.StringBuilder(); bool tag = false;
            foreach (var c in text ?? "") { if (c == '<') tag = true; else if (c == '>') tag = false; else if (!tag) sb.Append(c); }
            return sb.ToString();
        }
        // A chevron of two bars, pointing the way the page turns (the symbols font is imported with the twelve signs only; no arrow glyphs)
        Image[] Chevron(Button arrow, int direction)
        {
            var bars = new Image[2];
            for (int k = 0; k < 2; k++)
            {
                float up = k == 0 ? 1 : -1; var bar = Rect("Chevron", arrow.transform, 0, 0, 16, 3);
                bar.anchorMin = bar.anchorMax = new Vector2(.5f, .5f); bar.anchoredPosition = new Vector2(-direction * 2, up * 5.5f); bar.localRotation = Quaternion.Euler(0, 0, direction * up * -45);
                bars[k] = bar.gameObject.AddComponent<Image>(); bars[k].color = Bone; bars[k].raycastTarget = false;
            }
            return bars;
        }

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
            Dial.Lesson.RestoreProgress(Flow.LessonSun, Flow.HasSunSign, save.lit, save.kin, save.keyEarned); // the record's sun, or Aries with none (Oct 7)
            Dial.Lesson.RevealsPlayed.Clear(); if (save.revealsPlayed != null) Dial.Lesson.RevealsPlayed.UnionWith(save.revealsPlayed); // Build Z
            Dial.Lesson.RestoreGlyphs(save.glyphStage, save.glyphIndex, save.keys >= 2); Dial.Lesson.SetCleanRuns(save.cleanRuns);
            Dial.Lesson.RestoreModalities(save.litMod, save.kinMod, save.modalitiesStarted);
            Grid.Restore(save.gridPlaced, save.gridEvidence, save.gridStarted, save.keys >= 3);
            Dial.Lesson.SetKey3(save.keys >= 3); Dial.Lesson.RestoreOpposites(save.polarityShown, save.oppKnown, save.oppositesStarted, save.built, save.builderEvidence, save.keys >= 4);
            if (save.keys >= 2) keyIndicator.text = "Keeper Keys: " + Math.Max(2, save.keys);
            sunSent = true; revealStarted = true; Resumed = true; Dial.SliceHidesOptional = true; SetLight(LightAlphaFor(save.atriumStage, Mathf.Max(save.locksFilled, save.keyEarned ? 1 : 0))); // Build H: no fade on a reload
            keyIndicator.gameObject.SetActive(save.keyEarned); if (save.wheelComplete) LightWing(); else if (save.keyEarned) { FloorLight(.55f); PaintCandle(Bone, 1f); }
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
            PaintCandle(Bone, 1f); keyIndicator.gameObject.SetActive(true);
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
            public string Name, Wake; public float X, Bottom, Scale, WornScale, WornX; public int Key, Lock;
            public KitPlacement(string name, float x, float bottom, int key, float scale = 1, float wornScale = 0, float wornX = float.NaN, string wake = null, int arcLock = 0) { Name = name; X = x; Bottom = bottom; Key = key; Scale = scale; WornScale = wornScale; WornX = wornX; Wake = wake; Lock = arcLock; }
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
            new KitPlacement("dial", 40, 440, 2), // the wake-up (owner, Oct 1; 86bcbn6w6 2d A): today's look at Key 2; WakeKitDial blends the steps between
            new KitPlacement("telescope", 139, 445, 4),
            new KitPlacement("table", -62, 445, 3, wake: "modalities"), // the Table lesson happens on it: it rights itself when it wakes, not at Key 3 (owner, Sept 25)
            new KitPlacement("candles", -144, 185, 1),
            new KitPlacement("candles", 125, 205, 1, .75f),
            new KitPlacement("books", 160, 490, 2),
            new KitPlacement("chair", 144, 475, 3),
            new KitPlacement("plate", -138, 222, 1, 1.35f), // 35% larger so the name has room (Sept 26 playtest, note 12)
        };
        public static readonly float[] KitGrime = { 1, .75f, .5f, .25f, 0 }, KitVeil = { .45f, .3f, .18f, .08f, 0 }, KitLight = { 0, .25f, .5f, .75f, 1 }; // by Keys earned, 0 to 4 (tuning variables)
        const string WingPlateName = "THE GRAND\nATRIUM"; // the Wing's doorway leads back to the Atrium; two balanced lines, like the Atrium's plates (Sept 26 playtest, note 12)
        class KitPiece { public KitPlacement P; public CanvasGroup Worn, Restored, Bright; public Image Flash; public bool Shown; } // Bright: the wake-up's Key 4 look (the dial only)
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
            Bleed.Add(k.GrimeImage, root); // Part 2: the grime reaches the edges with its shell
            g.gameObject.SetActive(Slots.Dress(k.GrimeImage, grimeSlot)); g.SetAsFirstSibling(); // under the light overlay, over the shell
            k.Container = Rect("Kit", panel, 0, 400, 360, 800);
            foreach (var p in placements)
            {
                if (Slots.Image(prefix + p.Name + "-restored") == null) continue; // no file, no piece: the greybox stays as it was
                k.Pieces.Add(new KitPiece { P = p, Worn = KitState(k, p, "worn"), Restored = KitState(k, p, "restored"), Bright = p.Name == "dial" && prefix == "kit-" ? KitState(k, p, "bright") : null });
            }
            return k;
        }
        void FinishKit(RectTransform panel, RoomKit k)
        {
            var v = Rect("Veil", panel, 0, 400, 360, 800); k.VeilImage = v.gameObject.AddComponent<Image>(); k.VeilImage.color = new Color(0, 0, 0, 0); k.VeilImage.raycastTarget = false;
            Bleed.Add(k.VeilImage, root);
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
            // The text box is the plate's engraved field (about 74% by 52% of the file), not the whole plate; a line of air between the lines.
            // The old 80% by 70% box let bold caps run into the border - the "squished" plates of the Sept 26 playtest (notes 6 and 12).
            var name = Label(plate, text, 0, plate.sizeDelta.y / 2, plate.sizeDelta.x * .74f, plate.sizeDelta.y * .52f, 9); name.lineSpacing = 1f;
            name.color = new Color(.23f, .14f, .07f); name.fontStyle = FontStyle.Bold; name.resizeTextForBestFit = true; name.resizeTextMinSize = 6; name.resizeTextMaxSize = 12; return name;
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
        // ---- Build N: the Atrium kit. The opening pieces restore by stage (Key 2-3); the rest by Keys spent across the arc (arcLock), the light following the arc too (owner, Sept 27).
        static readonly KitPlacement[] AtriumKit =
        {
            // Build Q (the domed Atrium, owner-approved Sept 28): the banners, pennants, lanterns, and armillary ring are measured on the new
            // restored scene (moved 22 px up to meet the game's doors); the rug, busts, plants, candlestands, bench, and desk keep Build N's places.
            // Key 2-3 pieces are the opening's beats and follow the stage. Everything else carries arcLock, its Key spent on the
            // Library's whole arc of 21 (owner, Sept 27): a small change most Keys, the armillary ring last.
            new KitPlacement("rug", 0, 800, 4, 1, 1, 60, arcLock: 4),
            new KitPlacement("chandelier", -3.6f, 195, 6, 1, 1.58f, arcLock: 21), // the armillary ring keeps the chandelier's slot; the tarnished one is drawn smaller, so it is scaled to hang as far
            new KitPlacement("lamp", -92, 119, 6, 1, 1.45f, arcLock: 3), new KitPlacement("lamp", 92, 119, 5, 1, 1.45f, arcLock: 12), // the balcony lanterns (the dark ones are drawn smaller)
            new KitPlacement("banner", -121, 261, 5, 1, .9f, arcLock: 8), new KitPlacement("banner", 121, 261, 5, 1, .9f, arcLock: 11), // the torn banners hang as long as the whole ones
            new KitPlacement("pennant", -58, 370, 5, arcLock: 15), new KitPlacement("pennant", 58, 370, 5, arcLock: 17),
            new KitPlacement("lamp", -57, 223, 2, 1, 1.45f), new KitPlacement("lamp", 57, 223, 3, 1, 1.45f), // the lanterns beside the statue's niche
            new KitPlacement("bust", -63, 380, 4, arcLock: 2), new KitPlacement("bust", 64, 380, 4, arcLock: 6),
            new KitPlacement("plant", -83, 380, 5, arcLock: 7), new KitPlacement("plant", 84, 380, 5, arcLock: 10),
            new KitPlacement("candlestand", -156, 385, 2), new KitPlacement("candlestand", 169, 385, 2),
            new KitPlacement("bench", 147, 405, 3),
            new KitPlacement("desk", -107, 445, 2),
            new KitPlacement("plant", -164, 495, 5, 1.3f, arcLock: 13), new KitPlacement("plant", 160, 485, 5, 1.8f, arcLock: 16),
        };
        public static readonly float[] AtriumGrime = { 1, .8f, .6f, .4f, .2f, 0 }, AtriumVeil = { .62f, .46f, .32f, .2f, .09f, 0 }; // by arc level 0 to 5 (tuning variables)
        // The Atrium's grime, veil and level: the opening's beats carry it to level 2 (stage 3), then only the long arc lifts it further -
        // level 3 at the 7th Key spent, 4 at the 14th, clean at the 21st (owner, Sept 27).
        int AtriumArcLevel => Flow.LocksFilled >= SliceFlow.LocksTotal ? 5 : Flow.LocksFilled >= 14 ? 4 : Flow.LocksFilled >= 7 ? 3 : Mathf.Clamp(Flow.AtriumStage - 1, 0, 2);
        RoomKit BuildAtriumKit(RectTransform panel, SliceScreen screen)
        {
            var k = BuildKit(panel, "akit-", AtriumKit, "atrium-grime", null, AtriumGrime, AtriumVeil, null,
                () => screen == SliceScreen.Atrium ? 0 : AtriumArcLevel,
                p => screen != SliceScreen.Atrium && (p.Lock > 0 ? Flow.LocksFilled >= p.Lock : Flow.AtriumStage >= p.Key));
            // The Zodiac Wing's door is open to the Keeper from the start; the Chamber's unlocks with the first Key (the return); the sealed door stays sealed.
            k.VeilTint = new Color(.02f, .035f, .09f); // the Atrium shell carries warm lantern light; asleep, a cold blue night sits over it
            k.DoorUnlocked = id => id == "wing-door" || (id == "chamber-door" && screen != SliceScreen.Atrium && Flow.AtriumStage >= 2); // Build T: sealed on the opening's walk, before Key 1
            AddDoor(k, "sealed-left", null, -117.5f, 385, 60, 130, 264, 88); // plates ~30% larger for phone reading (owner, Sept 25), then 18% more so the names sit inside the engraved field (Sept 26 playtest, note 6)
            AddDoor(k, "wing-door", "THE ZODIAC\nWING", 0, 385, 70, 135, 258, 100);
            AddDoor(k, "chamber-door", "THE CRYSTAL\nBOOK CHAMBER", 117.5f, 385, 60, 130, 266, 102);
            FinishKit(panel, k); atriumKits.Add(k); return k;
        }
        // ---- Build O: the Chamber kit. It restores by Keys spent across the whole arc of 21 (owner, Sept 27); the Books stand on the altar, sealed or open.
        static readonly KitPlacement[] ChamberKit =
        {
            // Key = the lock count that restores the piece, across the whole arc of 21 (owner, Sept 27): the mechanism and a
            // candle per Key first, the braziers and banners through the middle Books, the crystal and the reliquary last.
            new KitPlacement("banner", -56, 175, 10), new KitPlacement("banner", 43, 175, 12),
            new KitPlacement("mechanism", 0, 235, 1),
            new KitPlacement("brazier", -108, 405, 14), new KitPlacement("brazier", 108, 405, 16),
            new KitPlacement("crystal", 140, 398, 20), new KitPlacement("reliquary", 163, 404, 21),
            new KitPlacement("candle", -120, 388, 1), new KitPlacement("candle", -90, 388, 2), new KitPlacement("candle", -60, 388, 3), new KitPlacement("candle", -30, 388, 4), new KitPlacement("candle", 0, 388, 5),
            new KitPlacement("candle", 30, 388, 6), new KitPlacement("candle", 60, 388, 7), new KitPlacement("candle", 90, 388, 8), new KitPlacement("candle", 120, 388, 9),
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
        // The doorways back (the art pass, owner Oct 7, 86bcex5kc 2A): the same soft glow and breath as the Atrium's open doors, behind the doorway.
        Image wingDoorGlow, chamberDoorGlow;
        Image DoorwayGlow(RectTransform parent, float x, float y, float w, float h)
        {
            var glow = Rect("Doorway glow", parent, x, y, w * 1.55f + 20, h * 1.25f + 20).gameObject.AddComponent<Image>(); glow.sprite = SoftGlow(); glow.raycastTarget = false; glow.color = new Color(1, .82f, .5f, 0); return glow;
        }
        void PulseDoors()
        {
            float breath = ReducedMotion ? .4f : .28f + .24f * Mathf.PingPong(Time.unscaledTime / 1.1f, 1f);
            if (wingDoorGlow != null) wingDoorGlow.color = new Color(1, .82f, .5f, Flow.Screen == SliceScreen.WingRoom ? breath : 0);
            if (chamberDoorGlow != null) chamberDoorGlow.color = new Color(1, .82f, .5f, Flow.Screen == SliceScreen.ChamberRoom ? breath : 0);
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
        // APK Session 2, bug 1 (86bcbn6ct): a soft edge that follows a picture's own silhouette. The file's alpha is read back once through the
        // GPU (slot files import unreadable), spread by HaloSpread file pixels, softened twice by HaloSoften, and cleared where the picture
        // itself is opaque. It is white, so the Image's color tints it. Made once per sprite; null when the GPU readback is unavailable.
        public const int HaloSpread = 3, HaloSoften = 4; // file pixels (2x): the edge reaches about 6 px on the layout, most of it in the first 3
        static readonly Dictionary<Sprite, Sprite> halos = new Dictionary<Sprite, Sprite>();
        static Sprite SilhouetteHalo(Sprite source)
        {
            if (source == null || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return null; // -nographics: no GPU to read back from
            if (halos.TryGetValue(source, out var made)) return made;
            var box = source.textureRect; int w = Mathf.RoundToInt(box.width), h = Mathf.RoundToInt(box.height), pad = HaloSpread + 2 * HaloSoften + 2, W = w + 2 * pad, H = h + 2 * pad;
            var target = RenderTexture.GetTemporary(source.texture.width, source.texture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source.texture, target); var was = RenderTexture.active; RenderTexture.active = target;
            var read = new Texture2D(w, h, TextureFormat.RGBA32, false); read.ReadPixels(new Rect(box.x, box.y, w, h), 0, 0); read.Apply(false);
            RenderTexture.active = was; RenderTexture.ReleaseTemporary(target);
            var pixels = read.GetPixels32(); UnityEngine.Object.Destroy(read);
            var own = new float[W * H]; for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) own[(y + pad) * W + x + pad] = pixels[y * w + x].a / 255f;
            var spread = Spread(Spread(Spread(own, W, H, HaloSpread, true), W, H, HaloSoften, false), W, H, HaloSoften, false);
            var halo = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var o = new Color32[W * H];
            for (int i = 0; i < o.Length; i++) o[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255 * Mathf.Clamp01(spread[i] * (1 - own[i]))));
            halo.SetPixels32(o); halo.Apply(false, true);
            return halos[source] = Sprite.Create(halo, new Rect(0, 0, W, H), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        }
        // One separable pass each way over a (2r + 1) window: its maximum (spread) or its mean (soften).
        static float[] Spread(float[] a, int W, int H, int r, bool max)
        {
            var mid = new float[a.Length]; var o = new float[a.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                var src = pass == 0 ? a : mid; var dst = pass == 0 ? mid : o; int n = pass == 0 ? W : H, lines = pass == 0 ? H : W;
                for (int line = 0; line < lines; line++) for (int i = 0; i < n; i++)
                {
                    float v = 0; for (int k = Mathf.Max(0, i - r); k <= Mathf.Min(n - 1, i + r); k++) { float s = src[pass == 0 ? line * W + k : k * W + line]; v = max ? Mathf.Max(v, s) : v + s; }
                    dst[pass == 0 ? line * W + i : i * W + line] = max ? v : v / (2 * r + 1);
                }
            }
            return o;
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
            ChamberPose(ChamberBreathesPose); Publish(); // Build R: he speaks, so he steps into view
            yield return new WaitForSecondsRealtime(hold * 2.4f);
            chamberText.text = "He opens his eyes and looks at you like he is seeing you for the first time.\n\"It is faint though. Let us continue, shall we?\"";
            ChamberPose(ChamberContinuePose); Publish(); // Build R
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
        RectTransform ScreenPanel(string name, string slot = null) { var s = Rect(name, root, 0, 400, 360, 800); var image = s.gameObject.AddComponent<Image>(); image.color = Charcoal; if (slot != null) Slots.Dress(image, slot); Bleed.Add(image, root); return s; } // Build E: a room's background is a slot
        // Build K: a narrow door keeps a finger-sized target; an invisible child catches the tap and it bubbles to the door's Button.
        void HitArea(RectTransform door, float width) { var hit = Rect("Hit area", door, 0, door.sizeDelta.y / 2, Mathf.Max(width, door.sizeDelta.x), door.sizeDelta.y); var image = hit.gameObject.AddComponent<Image>(); image.color = new Color(0, 0, 0, 0); image.raycastTarget = true; image.canvasRenderer.cullTransparentMesh = false; hit.SetAsFirstSibling(); }
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
