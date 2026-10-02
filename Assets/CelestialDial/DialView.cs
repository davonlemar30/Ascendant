using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    public sealed class DialView : MonoBehaviour
    {
        public DialLesson Lesson { get; private set; }
        public interface ISliceState { void Fill(WebState state); }
        public ISliceState Slice;                 // The vertical slice adds its screen state to the same bridge.
        public bool SliceHidesOptional;           // v0.2: once the loop exists, the optional probe is retired after the first completion.
        public Action<string> ExtraActions;       // Slice commands arrive through the same WebAction entry.
        public RectTransform Root => root;
        public RectTransform Ring => ring;
        public Canvas UiCanvas => canvas;
        public Font UiFont => font;
        public bool Busy => busy;
        public bool ControlsShown => next!=null && (next.gameObject.activeSelf || builderNames[0].gameObject.activeSelf || builderShares[0].gameObject.activeSelf); // the slice keeps its Back button off these rows
        public bool Inert;                        // Build E: the style page shows no game; every action goes to the slice
        public void RegisterNavigation(Selectable s) { navigation.Add(s); }
        public void ForceRefresh() { Refresh(); }
        public void Realign() { AlignStart(); }
        public bool firstDragResistance = false; // Test variable. Owner dropped it after the solo playtest: it went unnoticed.
        public bool inertiaEnabled = true;
        public const float PixelsPerDetent = 55;
        public const float SnapSeconds = .12f;
        readonly List<Selectable> navigation = new List<Selectable>();
        readonly Button[] seats = new Button[12];
        readonly Text[] seatTexts = new Text[12];
        readonly Text[] seatGlyphs = new Text[12];
        public Font GlyphFont { get; private set; } // Placeholder glyph rendering: Noto Sans Symbols (OFL), the Unicode zodiac symbols.
        Text message, destination, start, count, phase, sealText, motionText, subtitle;
        Button seal, back, forward, countButton, next, optional, askButton;
        RectTransform panel;
        readonly Button[] builderNames = new Button[4], builderShares = new Button[3]; // Build C: the builder's name and share steps
        RectTransform root, ring, bracket;
        Image faceImage; // Build E: the dial-face slot under the seats
        // Build Z (owner, Sept 29-30): Room A behind the wheel, the Dial's glow, the eye that speaks, a reveal as each pattern opens
        Image roomImage, roomLight; bool roomArt; Font voiceFont; CanvasGroup eyeGroup;
        readonly CanvasGroup[] seatGroups = new CanvasGroup[12], nameGroups = new CanvasGroup[12];
        bool revealing, skipReveal; string revealNow = ""; float revealStarted, lastSweepPublish;
        public bool RoomArt => roomArt;
        // Build AB (owner, Sept 30: the Astrolabe): a ring that turns with the seats, a name band and twelve tablets; one fact per seat
        RectTransform ringLayer; Image ringLight; bool ringArt; Text destinationFacts; ArcText ribbonName, ribbonFacts; bool countShown, eyeLaidOut; string eyeLine=""; // Build AC: the ribbon's two arcs; the eye's layout with a running count, and its line as the lesson set it
        readonly Text[] seatFacts = new Text[12]; readonly ArcText[] seatArcs = new ArcText[12]; readonly bool[] seatLower = new bool[12];
        public const float SeatRadius = 136f, AstroSeatRadius = 123f, NameRadius = 146f, TabletRadius = 109f, FactLift = 9f; // Build AC (the Cast Dial, owner Oct 1): the recess band r 136-155 and the windows r 91-127 on the 360 x 800 layout; the fact rides the window's outer half, the symbol its inner
        public const float RecessSpan = 26.5f, WindowSpan = 23.5f; // degrees of plain face per segment, measured on the production ring
        public bool RingArt => ringArt;
        // The eye's glass, measured on the Cast Dial's art: 150 x 33 at y 267, widest at its centre row. The challenge sits in a box inside it, one or two lines.
        public const float EyeY = 267f; public static readonly Vector2 EyeBox = new Vector2(128, 34); public const int EyeMin = 13, EyeMax = 16;
        // Build AC (owner, Oct 1: the count word goes inside the eye, under the challenge line): while a count runs the challenge folds onto one line across the glass's widest rows and the count word takes the line under it; two lines and a count would not fit a 33 px glass at any readable size
        public const float EyeYCount = 263.5f, CountY = 277f; public static readonly Vector2 EyeBoxCount = new Vector2(140, 14), CountBox = new Vector2(128, 11); public const int EyeMinCount = 10, EyeMaxCount = 12; // the challenge on one line across the glass's widest rows, 10 to 12 px, the count word under it
        // Build AC (owner, Oct 1: the phoenix holds a cast ribbon above the eye): the framed sign's name and facts curve along the ribbon, a bow on the 360 x 800 layout whose centre lies below it; each line may use so much of the ribbon's plain face along its arc
        public const float RibbonY = 222.5f, RibbonFactsY = 233.5f, RibbonRadius = 117.5f, RibbonNameRoom = 120f, RibbonFactsRoom = 116f; // the ribbon's centreline bows on a radius of about 112; the name's arc rides 5.5 above it, the facts' 5.5 below, one centre
        public const int NameSize = 14, FactSize = 11, CountSize = 11;
        public Text NameText => destination; public Text FactsText => destinationFacts; public Text EyeText => start; public Text CountText => count;
        public Text[] SeatNameTexts => seatTexts; public Text[] SeatFactTexts => seatFacts; // the fixture measures the words on them
        public static float NameRoom => NameRadius*RecessSpan*Mathf.Deg2Rad-6;            // a recess's plain face along the name's arc, less a margin: about 61
        public static float TabletRoom => (TabletRadius+FactLift)*WindowSpan*Mathf.Deg2Rad-6; // a window's plain face along the fact's arc, less a margin: about 42
        public ArcText RibbonNameArc => ribbonName; public ArcText RibbonFactsArc => ribbonFacts;
        public float RingTurn => ringLayer!=null ? ringLayer.localEulerAngles.z : 0;
        public float Turns => turns;
        public void PoseTurns(float t){ turns=t; target=t; LayoutRing(); } // the fixture's sweep: the wheel posed at any turn, mid-turn included
        public float SeatR => ringArt ? AstroSeatRadius : SeatRadius;
        public float FamilyScale => ringArt ? .7f : .77f; // the family lines run just inside the tablets
        static readonly Color Gold = new Color(.957f,.812f,.498f), FactInk = new Color(.965f,.925f,.84f), Engrave = new Color(.08f,.05f,.03f,.9f);
        public bool Showing = true; // the slice sets it: the Dial is the screen in front (a reveal plays only where the player sees it)
        public const float RevealSweepSeconds = 1.2f, RevealNamesSeconds = .35f, RevealEyeSeconds = .4f;
        static readonly Color EyeInk = new Color(.95f, .91f, .84f);
        Canvas canvas;
        DialGeometry geometry;
        Font font;
        bool busy, dragging, webPublished; string lastBoxKey="";
        float turns, target, snapFrom, snapAt, dragTurns, velocity, lastDragTime;
        float targetTurns { get => target; set { snapFrom=turns; snapAt=Time.unscaledTime; target=value; } }
        int dragDetent;
        string announced = "";
        static readonly Color Bone = new Color(.94f,.91f,.86f);
        static readonly Color Charcoal = new Color(.075f,.075f,.09f);
        static readonly Color Crimson = new Color(.46f,.09f,.15f);
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void DialPublish(string json);
#endif
        [Serializable] public sealed class WebState
        {
            public string message, destination, start, phase, count, challenge; // Build I: challenge = the line the wheel shows
            public string[] seats;
            public bool active, canContinue, canOptional, reducedMotion, keyEarned, dormant, introAuto, busy, review, wheelComplete, canAsk;
            public int hintLevel; // for test evidence; never shown to the player
            public int cleanRuns; public bool practice, hard;
            public string unit = ""; public bool modalitiesComplete; public int litModCount, step;
            // Build C
            public bool polarityShown, oppositesComplete, key4, canBuilderName, canBuilderShare;
            public int pairsKnown, built, builderIndex; public string builderStep = "", builderAsk = "";
            public string[] builderOptions, shared;
            public int familiesComplete, keys;
            public string[] glyphs;
            public bool namesHidden, glyphWheel, key2, v03Complete;
            public string glyphMode = "", glyphChar = "", glyphTarget = "";
            public string[] glyphOptions;
            public string screen = "wing", playerName = "", caspar = "", note = "";
            public string casparPose = ""; // Build P: the pose Caspar holds on a story screen; empty when no figure shows
            public float chatBoxHeight;
            public float shelfLight; public bool shelfRing; // Build Y: the shelf's own glow level (-1 without its kit file) and whether the old ring shows // Build X: the gold chat box on screen, fitted to its line (0 when none shows)
            public string shelfEdge; // APK Session 2, bug 1: "halo" when the lit shelf's edge is drawn from its silhouette (no Outline copies)
            public bool settingsOpen, canQuit, jumpsShown; // Build W: the Jump to list shows // Build U: the Settings menu is open; the app (not a web page) can quit
            public float safeTop; // Platform fit, Part 1: the top row's move down from the safe area, in layout units (0 without a band)
            public int dialWake; public string dialLook; public int wakePreview; // the Dial's wake-up (86bcbn6w6): the step it shows, the looks it blends, DEV Mode's preview (-1: as earned)
            public float[] gearAt, travelAt; // Part 2: where the gear and the mini-menu button sit (x from the column's centre, y down from its top; on a phone at the screen's safe corners)
            public bool travelShown, travelOpen; public string[] travelRows; // the room mini-menu (86bca07wv): its button in a room, its panel, its rows ("The Zodiac Wing, here", "Sealed")
            public string speaker = ""; // Build V: who speaks in the Dial's box, "caspar" or "dial" ("" when it is hidden)
            public float dialBoxHeight; // Build R: the Dial's Caspar box, fitted to its line (0 when hidden)
            public bool dialRing; public float ringTurn; public string[] seatNames, seatFacts; public string framedFacts = ""; // Build AB: the Astrolabe
            public float seatRadius; public float[] seatSize; // Build AC follow-up: the radius the seats sit on and one seat's size as drawn (along the ring, across it); the suite holds the template's seat boxes to them
            public bool dialRoom, dialVoice; public float dialLit, dialEye; public string revealing = "", eyeText = "", signLabel = ""; public string[] revealsPlayed; public int eyeSize; // Build Z: the room, the glow, the reveal, the eye's words, the sign label; dialVoice = the box wears the Dial's blue; dialEye = the Wing room Dial's eye, 0 shut to 1 open
            public int casparPage, casparPages; // Build S: the page of Caspar's line shown in the slim box on screen, and how many (0 when none shows)
            public string casparShown = ""; // Build S: that page's words, colour tags and all
            public bool keyRevealed, keyInserted, ended, canInsert, canSliceContinue, canName, canBirth, canBirthDate, canSignPick, canChangeBirth;
            public int atriumStage, dueCount;
            public bool canEnterWing, canLeaveWing, canLeaveDial, v02Complete, resumed;
            public string hubNote = "";
            // Build F: the practice fork, the sitting, the gate, the journal
            public string fork = "", practiceMode = "", practiceSign = "", practiceSummary = "";
            public bool practicing, gated, journal, canContinueLesson, canEnterPractice, canLeavePractice, canOpenJournal, canCloseJournal, canJournalNext, canJournalPrev;
            public int practiceIndex, practiceCount, strikes, sitting, journalPage, journalCount;
            // Build J / AA: the journal (journalSeats, journalSeatState and journalText are test evidence: the state words are never drawn)
            public string journalView = "", journalSign = "", journalGlyph = "", journalArt = "", journalText = "", journalTitleFont = "", journalLens = "", journalSelected = "", journalPreview = "", journalSeatState = "";
            public string[] journalLenses, journalSeats, journalFacts, journalChips; public bool[] journalDue; public float[] journalChipBoxes;
            public float journalInk, journalColour; public int journalRibbon; public bool journalRibbonOut, journalGilt, journalShader, canJournalWheel, canJournalTable, canJournalOpen;
            public string sunSign = "";
            public int locksFilled;
            // v0.4 tap-to-move
            public string room = "", walkTarget = "", avatarAt = "", walkSpeed = "";
            public float avatarX;
            public bool walking, canWalk, canEnterDial, canEnterShelf, canCloseBook;
            public string[] pois, poiLabels;
            // Build B: the table
            public string gridReadout = "", gridStatus = "", gridSign = "";
            public string[] gridTiles, gridCells;
            public int gridPlaced, gridCell = -1, gridHintLevel; // the level is test evidence only, never shown
            public bool gridOpen, gridStarted, gridComplete, gridPaused, gridLocked, key3, canEnterGrid, canGridPick, canGridSeal, canGridAsk, canLeaveGrid;
            // Build D: the finished loop
            public int keysInHand, keysSpent, booksOpen; public bool wingWhole, canEnterChamber, canLeaveChamber, atBooks;
            // Build E: art slots and sound hooks
            public string artSet = "", lastCue = ""; public int artFiles, soundFiles, cuesPlayed; public bool muted, style;
            public float lightAlpha; public int lightFiles; // Build H: the light overlays
            public int keyCeremony; public float dialGlow; public int kitLevel, kitPieces, kitRestored; public float grime, wingLight; public string[] kitUp; public int atriumKitLevel = -1, atriumKitPieces, atriumKitRestored; public float atriumGrime = -1; public string[] doors; public string lastDoorOpened = ""; // Build N // Build M: the Wing kit; kitUp = the pieces standing restored // Build I: the last Key ceremony shown; the Dial's waiting-unit glow in the Wing room
            public int chamberKitLevel = -1, chamberKitPieces, chamberKitRestored; // Build O: the Chamber kit
            public string[] styleSlots, styleSounds;
        }
        void Awake()
        {
            Application.runInBackground=true;
            gameObject.name = "CelestialDial";
            Lesson = new DialLesson(() => Time.realtimeSinceStartupAsDouble);
            Lesson.Dial.Logged += e => Debug.Log("[CelestialDial] " + JsonUtility.ToJson(e));
            Sound.Ensure(); Lesson.Dial.Logged += e => Sound.Play(Sound.Cue(e.event_name, e.correctness, TapPhase)); // Build E: the model's events name the cues
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GlyphFont = Resources.Load<Font>("Fonts/NotoSansSymbols") ?? font;
            var canvasObject = new GameObject("Greybox Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ScaleCanvas();
            var bg = canvasObject.AddComponent<Image>(); bg.color = Charcoal;
            root = Rect("Portrait", canvasObject.transform, 0, 0, 360, 800);
            // Build Z: the Zodiac Wing behind the wheel (Room A), its glow swept in over it, and dark fades under the title and under the wheel so the words read
            var room = Rect("Dial room", root, 0, 400, 360, 800); roomImage = room.gameObject.AddComponent<Image>(); roomImage.raycastTarget = false;
            Bleed.Add(roomImage, root, Bleed.Mode.Clamp, .35f); // Part 2: the Dial's room reaches the screen's edges (the Dial stays in the column). Its rim runs to within 4 px of the column's sides, so a mirror would hang half-wheels in the margins: the stop-gap carries the edge's own colours outward instead, darkened
            roomArt = Slots.Dress(roomImage, "dial-room"); room.gameObject.SetActive(roomArt);
            var glowRect = Rect("Dial room light", root, 0, 400, 360, 800); roomLight = glowRect.gameObject.AddComponent<Image>(); roomLight.raycastTarget = false;
            glowRect.gameObject.SetActive(roomArt && Slots.Dress(roomLight, "dial-room-light"));
            roomLight.type = Image.Type.Filled; roomLight.fillMethod = Image.FillMethod.Radial360; roomLight.fillOrigin = (int)Image.Origin360.Top; roomLight.fillClockwise = true; roomLight.fillAmount = 0;
            if (roomArt) { Fade(root, 0, 0, 140, true, .55f); Fade(root, 0, 430, 370, false, .62f); }
            voiceFont = Resources.Load<Font>("Fonts/EBGaramond-Bold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SafeArea.Top(Shade(Label(root, "THE CELESTIAL DIAL", 0, 32, 340, 24, 18))); // the instrument's own name, not the room's (owner, Sept 26 playtest, note 13; name picked by the owner, Sept 27)
            subtitle = SafeArea.Top(Shade(Label(root, "The Elemental Pattern", 0, 62, 330, 22, 14)), SafeArea.UnderTitle);
            if (roomArt) // Build AB: the Astrolabe's ring and its glow turn with the seats; the room, rim, pointer, phoenix and eye stay put
            {
                ringLayer = Rect("Dial ring layer", root, 0, 270, 360, 360); var ringImage = ringLayer.gameObject.AddComponent<Image>(); ringImage.raycastTarget = false;
                ringArt = Slots.Dress(ringImage, "dial-ring");
                var ringGlow = Rect("Dial ring light", ringLayer, 0, 180, 360, 360); ringLight = ringGlow.gameObject.AddComponent<Image>(); ringLight.raycastTarget = false;
                ringGlow.gameObject.SetActive(ringArt && Slots.Dress(ringLight, "dial-ring-light"));
                ringLight.type = Image.Type.Filled; ringLight.fillMethod = Image.FillMethod.Radial360; ringLight.fillOrigin = (int)Image.Origin360.Top; ringLight.fillClockwise = true; ringLight.fillAmount = 0;
                ringLayer.gameObject.SetActive(ringArt);
            }
            // The wake-up (owner, Oct 1; 86bcbn6w6): each of the Dial's four layers can blend toward its next look (WakeStep)
            if (roomArt) { WakeLayerFor(roomImage, "dial-room"); if (roomLight.gameObject.activeSelf) WakeLayerFor(roomLight, "dial-room-light"); }
            if (ringArt) { WakeLayerFor(ringLayer.GetComponent<Image>(), "dial-ring"); if (ringLight.gameObject.activeSelf) WakeLayerFor(ringLight, "dial-ring-light"); }
            ring = Rect("Twelve-seat Dial", root, 0, 270, 332, 332);
            var hit = ring.gameObject.AddComponent<Image>(); hit.color = new Color(0,0,0,.001f);
            ring.gameObject.AddComponent<DialDrag>().View = this;
            var face = Rect("Dial face", ring, 0,166,332,332); faceImage = face.gameObject.AddComponent<Image>(); faceImage.raycastTarget = false; face.gameObject.SetActive(!roomArt && Slots.Dress(faceImage, "dial-face")); // Build Z: the room carries the wheel; the face is the Wing room's fallback now // Build E: the face under the seats; the ring's lines stay without it
            var drawing = Rect("Ring and family connections", ring, 0,166,332,332);
            geometry = drawing.gameObject.AddComponent<DialGeometry>(); geometry.View = this; geometry.RingHidden = face.gameObject.activeSelf || roomArt; geometry.Soft = roomArt;
            for (int i=0;i<12;i++)
            {
                int seat = i;
                seats[i] = MakeButton(ring, Zodiac.Seats[i].Name, 0,166,52,52, () => SelectSeat(seat, ClickMethod(DialInput.DirectSeat)));
                seats[i].gameObject.AddComponent<DialDrag>().View = this; if (!ringArt) Slots.Dress(seats[i].GetComponent<Image>(), "seat"); // Build AB: with the Astrolabe the tablets are in the ring
                seatTexts[i] = seats[i].GetComponentInChildren<Text>(); seatTexts[i].fontSize = 11; seatGroups[i] = seats[i].gameObject.AddComponent<CanvasGroup>(); nameGroups[i] = seatTexts[i].gameObject.AddComponent<CanvasGroup>(); // Build Z: the reveal pops each seat and fades the names up
                seatTexts[i].horizontalOverflow = HorizontalWrapMode.Overflow; // Long names spill past the tile instead of breaking mid-word.
                // The symbol font sits low in its box, so the box sits high in the tile. (A v0.4 edit turned the rest of this line into a comment,
                // which left the seats on the plain text font: no marks on the Web, owner playtest v0.3.)
                seatGlyphs[i] = Label(seats[i].transform, "", 0, 6, 48, 24, 18); seatGlyphs[i].font = GlyphFont; seatGlyphs[i].horizontalOverflow = HorizontalWrapMode.Overflow; seatGlyphs[i].verticalOverflow = VerticalWrapMode.Overflow; seatGlyphs[i].gameObject.SetActive(false);
                var seatRect=(RectTransform)seats[i].transform; seatRect.anchorMin=seatRect.anchorMax=new Vector2(.5f,.5f);
                if (ringArt) BuildAstrolabeSeat(i);
            }
            bracket = Rect("Fixed focus bracket", root, -136,270,58,58);
            var outline = bracket.gameObject.AddComponent<Image>(); outline.color = Color.clear; outline.raycastTarget = false;
            bool bracketArt = Slots.Dress(outline, "bracket"); // Build E: the file frames the seat; the bars and the edge stay without it
            var edge = bracket.gameObject.AddComponent<Outline>(); edge.effectColor = Bone; edge.effectDistance = new Vector2(2,2); edge.enabled = !bracketArt;
            // Four short bars visibly frame exactly one seat, without relying on color.
            if(!bracketArt) { Bar(bracket,-27,29,3,58); Bar(bracket,27,29,3,58); Bar(bracket,0,1,56,3); Bar(bracket,0,57,56,3); }
            if(ringArt) { bracket.anchoredPosition=new Vector2(-AstroSeatRadius,-270); bracket.sizeDelta=new Vector2(72,88); } // Build AB: the gold wedge over the 9 o'clock segment; Build AC: over the recess and the window, under the diamond
            start = Label(root,"",0,223,220,30,13); start.resizeTextForBestFit=true; start.resizeTextMinSize=10; start.resizeTextMaxSize=13; // Build I: the wheel's challenge line
            if (roomArt) // Build Z (owner, Sept 30): the Dial speaks through the eye: its line sits in the glass, in the Dial's own serif, one or two lines, 20 px down to 15
            {
                var eyeRect = start.rectTransform; eyeRect.anchoredPosition = new Vector2(0, -EyeY); eyeRect.sizeDelta = EyeBox; // Build AB (owner, Oct 1: the words "aren't seated properly inside the eye"): the box sits inside the glass
                start.font = voiceFont; start.resizeTextMinSize = EyeMin; start.resizeTextMaxSize = EyeMax; start.fontSize = EyeMax; start.lineSpacing = .92f; start.color = EyeInk;
                var glow = start.gameObject.AddComponent<Outline>(); glow.effectColor = new Color(Slots.DialVoice.r, Slots.DialVoice.g, Slots.DialVoice.b, .5f); glow.effectDistance = new Vector2(1, -1);
                eyeGroup = start.gameObject.AddComponent<CanvasGroup>();
            }
            destination = Label(root,"",0,271,188,50,20); // Names the sign under the bracket, live while dragging (owner request, Sept 12; reverses the Sept 11 "no label" row).
            if (roomArt && !ringArt) { destination.rectTransform.anchoredPosition = new Vector2(0, -222); destination.rectTransform.sizeDelta = new Vector2(188, 22); destination.fontSize = 15; destination.fontStyle = FontStyle.Bold; Shade(destination); } // Build Z (owner pick, Sept 30): above the eye
            if (ringArt) // Build AC (owner, Oct 1): the framed sign's name and its learned facts curve along the cast ribbon the phoenix holds above the eye
            {
                destination.rectTransform.anchoredPosition = new Vector2(0, -RibbonY); destination.rectTransform.sizeDelta = new Vector2(220, 20); destination.verticalOverflow = VerticalWrapMode.Overflow; destination.fontSize = NameSize; destination.fontStyle = FontStyle.Normal; destination.color = Gold; destination.horizontalOverflow = HorizontalWrapMode.Overflow; Engraved(destination);
                ribbonName = destination.gameObject.AddComponent<ArcText>(); ribbonName.Radius = RibbonRadius; // after the outline, so its copies bend too
                destinationFacts = Label(root, "", 0, 0, 220, 15, FactSize); destinationFacts.rectTransform.anchoredPosition = new Vector2(0, -RibbonFactsY); destinationFacts.verticalOverflow = VerticalWrapMode.Overflow; Engraved(destinationFacts); destinationFacts.font = voiceFont; destinationFacts.supportRichText = true; destinationFacts.color = FactInk; destinationFacts.horizontalOverflow = HorizontalWrapMode.Overflow;
                ribbonFacts = destinationFacts.gameObject.AddComponent<ArcText>(); ribbonFacts.Radius = RibbonRadius - (RibbonFactsY - RibbonY); // the same centre as the name's arc
            }
            count = ringArt ? Label(root,"",0,319,178,36,15) : Shade(Label(root,"",0,319,178,36,15));
            if (ringArt) { count.rectTransform.anchoredPosition = new Vector2(0, -CountY); count.rectTransform.sizeDelta = CountBox; count.fontSize = CountSize; count.font = voiceFont; count.verticalOverflow = VerticalWrapMode.Overflow; count.horizontalOverflow = HorizontalWrapMode.Overflow; count.color = EyeInk; var countGlow = count.gameObject.AddComponent<Outline>(); countGlow.effectColor = new Color(Slots.DialVoice.r, Slots.DialVoice.g, Slots.DialVoice.b, .5f); countGlow.effectDistance = new Vector2(1, -1); } // Build AC (owner, Oct 1): the worked count inside the eye, under the challenge line, in the eye's own voice
            Shade(Label(root,"Find the sign, seal it.",0,441,340,24,14)); // the step hint, now in the Library's voice (owner pick, APK playtest, Sept 29; was "Move > Inspect > Seal")
            panel = Rect("Caspar instruction panel",root,0,526,340,128);
            var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.color = new Color(.13f,.13f,.15f);
            var casparLabel = Label(panel,"CASPAR",0,18,290,22,13);
            message = Label(panel,"",0,74,326,100,13); message.resizeTextForBestFit=true; message.resizeTextMinSize=9; message.resizeTextMaxSize=13; // Build I: the text shrinks to fit instead of being cut off (owner playtest, Sept 23)
            Slots.DressInstrumentBox(panelImage,casparLabel,message,font); // Build R: the slim box fitted to the line, not the chat box; the wheel stays in view (owner, Sept 28-29)
            phase = Shade(Label(root,"",0,608,330,28,13));
            back = MakeButton(root,"Previous",-122,654,64,56,()=>Step(-1,ClickMethod(DialInput.BackStep)));
            seal = MakeButton(root,"SEAL",0,654,146,56,Commit); // Amended canon (Sept 11): "Keeper's Seal" renamed to "Seal".
            seal.GetComponent<Image>().color=Crimson; sealText=seal.GetComponentInChildren<Text>();
            forward = MakeButton(root,"Next",122,654,64,56,()=>Step(1,ClickMethod(DialInput.ForwardStep)));
            countButton=MakeButton(root,"Count",-60,714,100,48,()=> { Lesson.Dial.Count(); Refresh(); });
            askButton=MakeButton(root,"Ask Caspar",78,714,164,48,AskCaspar); askButton.GetComponentInChildren<Text>().fontSize=13; askButton.gameObject.SetActive(false); // owner (worksheet section 8)
            next=MakeButton(root,"Continue",0,654,190,56,ContinueLesson);
            for(int i=0;i<4;i++){ int slot=i; builderNames[i]=MakeButton(root,"",-78+(i%2)*156,654+(i/2)*60,150,56,()=>BuilderName(slot)); builderNames[i].gameObject.SetActive(false); }
            for(int i=0;i<3;i++){ int property=i; builderShares[i]=MakeButton(root,DialLesson.ShareLabels[i],-110+i*110,654,104,56,()=>BuilderShare(property)); builderShares[i].GetComponentInChildren<Text>().fontSize=13; builderShares[i].gameObject.SetActive(false); }
            optional=MakeButton(root,"Try one more (optional)",0,714,244,48,()=> { Lesson.BeginOptional(); AlignStart(); });
            var motion = MakeButton(root,"Reduced motion: off",92,768,160,44,ToggleMotion);
            motionText=motion.GetComponentInChildren<Text>(); motionText.fontSize=13; motion.gameObject.SetActive(false); // Build U: Reduced motion lives in Settings now (owner, Sept 29); the web action stays
            if (EventSystem.current == null)
            {
                var events=new GameObject("Dial EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform,false);
                var input=events.GetComponent<InputSystemUIInputModule>(); input.AssignDefaultActions();
                input.move=null; input.submit=null; // Explicit, predictable keyboard order below.
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput=false;
#endif
            Refresh();
        }
        static DialInput ClickMethod(DialInput pointer) => Keyboard.current != null &&
            (Keyboard.current.enterKey.isPressed || Keyboard.current.spaceKey.isPressed) ? DialInput.Keyboard : pointer;
        void ScaleCanvas() { if(canvas.pixelRect.width<1)return; canvas.scaleFactor=Mathf.Min(canvas.pixelRect.width/360f,canvas.pixelRect.height/800f); }
        void Update()
        {
            ScaleCanvas();
            if(!webPublished)Publish();
            var shownBox=FitBox.Visible; string boxKey=shownBox!=null?shownBox.Page+"/"+shownBox.Pages+"/"+shownBox.Source.Length:""; if(boxKey!=lastBoxKey){lastBoxKey=boxKey;Publish();} // Build S: a page turned or a line paged
#if !UNITY_WEBGL || UNITY_EDITOR
            var k=Keyboard.current;
            if(k!=null)
            {
                if(k.rightArrowKey.wasPressedThisFrame) Step(1,DialInput.Keyboard);
                if(k.leftArrowKey.wasPressedThisFrame) Step(-1,DialInput.Keyboard);
                if(k.tabKey.wasPressedThisFrame) FocusNext(k.shiftKey.isPressed ? -1 : 1);
                if(k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)
                {
                    var selected=EventSystem.current?.currentSelectedGameObject;
                    var button=selected == null ? null : selected.GetComponent<Button>();
                    if(button != null && button.interactable) button.onClick.Invoke();
                }
            }
#endif
            if(!busy && !dragging && Lesson.IntroAuto) StartCoroutine(IntroBeat());
            if(!busy && !dragging && CanReveal && PatternOpening is string opening && !Lesson.RevealsPlayed.Contains(opening)) StartCoroutine(Reveal(opening)); // Build Z (owner, Sept 30): once as each pattern opens
            if(!busy && !dragging && Lesson.CountBeatPending && Lesson.Dial.Active) StartCoroutine(CountBeat());
            if(!dragging && Mathf.Abs(turns-targetTurns)>.001f)
            {
                turns=Lesson.Dial.ReducedMotion ? targetTurns : Mathf.Lerp(snapFrom,targetTurns,Mathf.Clamp01((Time.unscaledTime-snapAt)/SnapSeconds));
                LayoutRing();
            }
        }
        public Vector2 SeatPosition(int seat)
        {
            float angle=(180+seat*30-turns*30)*Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*SeatR;
        }
        void LayoutRing()
        {
            if(bracket!=null)bracket.localScale=Vector3.one*(dragging ? 1.04f : 1f);
            if(ringLayer!=null) ringLayer.localRotation=Quaternion.Euler(0,0,-turns*30); // Build AB: the ring turns with the seats, live while dragging
            for(int i=0;i<12;i++){ var p=SeatPosition(i); ((RectTransform)seats[i].transform).anchoredPosition=p; if(ringArt) OrientSeat(i,p); }
            geometry.Redraw();
        }
        public void Step(int delta,DialInput method)
        {
            if(busy || dragging || !Lesson.Dial.Active) return;
            Lesson.Dial.Step(delta,method); targetTurns+=delta; Lesson.Dial.Frame(); Refresh();
        }
        public void SelectSeat(int index,DialInput method)
        {
            if(busy || dragging || !Lesson.Dial.Active) return;
            int previous=Lesson.Dial.Selected;
            Lesson.Dial.Select(index,method);
            int delta=Zodiac.Wrap(index-previous); if(delta>6)delta-=12;
            targetTurns+=delta; Refresh();
        }
        public void BeginDrag(Vector2 position)
        {
            if(busy || !Lesson.Dial.Active) return;
            dragging=true; dragTurns=targetTurns; dragDetent=0; velocity=0; lastDragTime=Time.unscaledTime;
        }
        public void Drag(Vector2 position,Vector2 delta)
        {
            if(!dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ring,position,null,out var local);
            Vector2 tangent=new Vector2(local.y,-local.x).normalized; // Clockwise screen motion.
            float scale=root.GetComponentInParent<Canvas>().scaleFactor;
            float movement=Vector2.Dot(delta/scale,tangent)/PixelsPerDetent;
            if(firstDragResistance && !Lesson.Dial.FirstSuccessfulSeal)movement*=.7f;
            float dt=Mathf.Max(.001f,Time.unscaledTime-lastDragTime);
            velocity=movement/dt; lastDragTime=Time.unscaledTime;
            dragTurns+=movement;
            int detent=Mathf.RoundToInt(dragTurns-targetTurns);
            if(detent!=dragDetent) { Lesson.Dial.Step(detent-dragDetent,DialInput.Drag); dragDetent=detent; }
            turns=Lesson.Dial.ReducedMotion ? targetTurns+dragDetent : dragTurns;
            LayoutRing(); Refresh();
        }
        public void EndDrag()
        {
            if(!dragging) return;
            dragging=false;
            int inertia=inertiaEnabled && Lesson.Dial.CanInertia && Time.unscaledTime-lastDragTime<.08f && Mathf.Abs(velocity)>3 ? (int)Mathf.Sign(velocity) : 0;
            if(inertia!=0) Lesson.Dial.Step(inertia,DialInput.Drag);
            targetTurns+=dragDetent+inertia; Lesson.Dial.Frame(); Refresh();
        }
        void AskCaspar() { if(busy || dragging || !Lesson.AskCaspar()) return; Refresh(); Publish(); }
        public void Commit()
        {
            if(busy || dragging) return;
            // A seal commits the selected detent, never a passing seat between detents.
            turns=targetTurns; LayoutRing();
            var result=Lesson.Seal(); if(result==null)return;
            Refresh();
            if(Lesson.Phase==LessonPhase.Review) { AlignStart(); return; } // The lesson settles review outcomes itself; the slice paces the next item.
            if(result.correctness) StartCoroutine(Correct(result));
            else if(Lesson.Dial.HintLevel>=3) StartCoroutine(Demonstrate());
        }
        IEnumerator Correct(DialEvent result)
        {
            busy=true;
            Lesson.Lit[result.selected_destination] |= Lesson.Phase != LessonPhase.Optional && Lesson.Phase != LessonPhase.Review && !Lesson.InModalities && !Lesson.InOppositeProblem; // Reviews never light seats; the modality unit lights its own array; opposites mark pairs.
            message.text=Lesson.Phase==LessonPhase.Review ? Lesson.Message : Lesson.CorrectLine(result);
            RefreshSeats(); Publish();
            yield return new WaitForSecondsRealtime(Lesson.Dial.ReducedMotion ? .6f : 1.1f);
            yield return ReturnHome();
            Lesson.AfterCorrect(result); busy=false; AlignStart();
        }
        // One click per count beat, synchronized with the displayed number (Sept 11 build note).
        public const float BeatSeconds=.9f, ReducedBeatSeconds=.5f;
        float Beat => Lesson.Dial.ReducedMotion ? ReducedBeatSeconds : BeatSeconds;
        bool waking;
        static string Number(int n) => n==1 ? "one" : n==2 ? "two" : n==3 ? "three" : n==4 ? "four" : n==5 ? "five" : "six"; // the opposite sits six seats on
        IEnumerator IntroBeat()
        {
            busy=true; Refresh();
            if(Lesson.IntroStep==1)
            {
                // The Dial wakes to the player: seats brighten one by one.
                waking=true;
                if(CanReveal) yield return RevealBody("elements"); // Build Z: the first approach's wake is the elements' reveal
                else if(Lesson.Dial.ReducedMotion) { RefreshSeats(); yield return new WaitForSecondsRealtime(.6f); }
                else for(int i=0;i<12;i++){ RefreshSeats(); yield return new WaitForSecondsRealtime(.09f); }
                yield return new WaitForSecondsRealtime(.6f);
            }
            else yield return new WaitForSecondsRealtime(Lesson.Dial.ReducedMotion ? 1f : 2.2f); // Simulated hesitation: no input asked.
            Lesson.Continue(); busy=false; AlignStart();
        }
        IEnumerator CountBeat()
        {
            // Level 2 shows the count once: start seat, then one click per number. Then the ring goes back where it was.
            busy=true; int beginning=Lesson.Dial.Start, before=Lesson.Dial.Selected; string rule=Lesson.Message;
            Lesson.Dial.PositionSilently(beginning); targetTurns=turns=beginning; LayoutRing(); RefreshSeats(); Publish();
            yield return new WaitForSecondsRealtime(Beat);
            for(int n=1;n<=Lesson.Dial.Forward;n++)
            {
                Lesson.Dial.PositionSilently(beginning+n); targetTurns=turns=beginning+n; Sound.Play("step");
                count.text=Number(n);
                LayoutRing(); RefreshSeats(); Publish();
                yield return new WaitForSecondsRealtime(Beat);
            }
            Lesson.Dial.PositionSilently(before); targetTurns=turns=before; count.text="";
            Lesson.CountBeatShown(); busy=false; LayoutRing(); Refresh(); message.text=rule;
        }
        IEnumerator Demonstrate()
        {
            busy=true; int beginning=Lesson.Dial.Start; int steps=Lesson.Phase==LessonPhase.GlyphWheel ? Zodiac.Wrap(Lesson.Dial.Target-beginning) : Lesson.Dial.Forward;
            Lesson.Dial.PositionSilently(beginning); targetTurns=turns=beginning; LayoutRing();
            message.text=Lesson.Phase==LessonPhase.GlyphWheel ? "Watch me, acolyte. I search the wheel until the symbol of "+Zodiac.Seats[Lesson.Dial.Target].Name+" is selected. There, you see how it is done." : "Watch. I start at "+Zodiac.Seats[beginning].Name+" and count each sign after it."; RefreshSeats(); Publish();
            yield return new WaitForSecondsRealtime(Beat);
            for(int n=1;n<=steps;n++)
            {
                Lesson.Dial.PositionSilently(beginning+n); targetTurns=turns=beginning+n; Sound.Play("step");
                if(Lesson.Phase!=LessonPhase.GlyphWheel) message.text="Watch. I start at "+Zodiac.Seats[beginning].Name+" and count each sign after it.\n"+n+": "+Zodiac.Seats[Zodiac.Wrap(beginning+n)].Name;
                count.text=n<=6 ? Number(n) : n.ToString();
                LayoutRing(); RefreshSeats(); Publish();
                yield return new WaitForSecondsRealtime(Beat);
            }
            count.text="";
            Lesson.RevealDemonstration();
            yield return new WaitForSecondsRealtime(Lesson.Dial.ReducedMotion ? .6f : 1.2f);
            yield return ReturnHome(); Lesson.AfterDemonstration(); busy=false; AlignStart();
        }
        // ---- Build Z: the reveal (owner, Sept 29-30). The glow sweeps round the ring from the top, each seat pops as the sweep passes it, the names fade up,
        // and the pattern's first line rises in the eye; small built motions, about two seconds; a tap skips it; Reduced motion shows the end at once.
        bool CanReveal => roomArt && roomLight != null && roomLight.gameObject.activeSelf && Showing && !Inert;
        string PatternOpening => Lesson.Practice || Lesson.Phase==LessonPhase.Review ? null : Lesson.Phase==LessonPhase.GlyphWheel ? "symbols" :
            Lesson.Phase==LessonPhase.ModalityGuided || Lesson.Phase==LessonPhase.ModalityOwn ? "modalities" : Lesson.InBuilder ? "builder" :
            Lesson.Phase==LessonPhase.Polarity || Lesson.InOppositeProblem ? "opposites" : null;
        IEnumerator Reveal(string pattern) { busy=true; Refresh(); yield return RevealBody(pattern); busy=false; AlignStart(); }
        float SeatSweep(int i) { var v=SeatPosition(i); float a=Mathf.Atan2(v.x,v.y)*Mathf.Rad2Deg; if(a<0)a+=360; return a/360f; } // clockwise from the top, as the fill runs
        bool SkipPressed => skipReveal || (Time.unscaledTime-revealStarted>.2f && Pointer.current!=null && Pointer.current.press.wasPressedThisFrame);
        IEnumerator RevealBody(string pattern)
        {
            Lesson.RevealsPlayed.Add(pattern); revealing=true; revealNow=pattern; skipReveal=false; revealStarted=Time.unscaledTime;
            bool instant=Lesson.Dial.ReducedMotion; RefreshSeats();
            roomLight.fillAmount=0; if(ringLight!=null) ringLight.fillAmount=0; for(int i=0;i<12;i++){ seatGroups[i].alpha=0; nameGroups[i].alpha=0; } if(eyeGroup!=null) eyeGroup.alpha=0; Publish();
            for(float t=0; !instant && t<RevealSweepSeconds && !SkipPressed; t+=Time.unscaledDeltaTime)
            {
                float f=t/RevealSweepSeconds; roomLight.fillAmount=f; if(ringLight!=null) ringLight.fillAmount=f;
                for(int i=0;i<12;i++){ float past=(f-SeatSweep(i))*RevealSweepSeconds; seatGroups[i].alpha=past>=0 ? 1 : 0; seats[i].transform.localScale=Vector3.one*(past>=0 ? 1+.25f*(1-Mathf.Clamp01(past/.15f)) : 1); }
                if(Time.unscaledTime-lastSweepPublish>.1f){ lastSweepPublish=Time.unscaledTime; Publish(); } // the web state follows the sweep (test evidence)
                yield return null;
            }
            roomLight.fillAmount=1; if(ringLight!=null) ringLight.fillAmount=1; for(int i=0;i<12;i++){ seatGroups[i].alpha=1; seats[i].transform.localScale=Vector3.one; }
            for(float t=0; !instant && t<RevealNamesSeconds && !SkipPressed; t+=Time.unscaledDeltaTime) { for(int i=0;i<12;i++) nameGroups[i].alpha=t/RevealNamesSeconds; yield return null; }
            for(int i=0;i<12;i++) nameGroups[i].alpha=1;
            var eye=start.rectTransform;
            for(float t=0; eyeGroup!=null && !instant && t<RevealEyeSeconds && !SkipPressed; t+=Time.unscaledDeltaTime) { float k=t/RevealEyeSeconds; eyeGroup.alpha=k; eye.anchoredPosition=new Vector2(0,-EyeY-8*(1-k)); yield return null; }
            if(eyeGroup!=null){ eyeGroup.alpha=1; eye.anchoredPosition=new Vector2(0,-EyeY); } // Build AC: the eye's own height, not Build Z's 270
            revealing=false; revealNow=""; skipReveal=false; Publish();
        }
        IEnumerator ReturnHome()
        {
            Lesson.Dial.Home(); targetTurns=0;
            if(Lesson.Dial.ReducedMotion)turns=0;
            yield return new WaitForSecondsRealtime(.25f);
            turns=0; LayoutRing();
        }
        void ContinueLesson() { if(busy || Lesson.IntroAuto)return; Lesson.Continue(); AlignStart(); }
        // Build C: the builder's name and share steps answer by tap; a short hold lets the line land before the next step.
        void BuilderName(int slot)
        {
            if(busy || dragging || Lesson.Phase!=LessonPhase.BuilderName || slot<0 || slot>3) return;
            Lesson.AnswerBuilderName(Lesson.BuilderOptions[slot]);
            if(Lesson.Phase==LessonPhase.BuilderOpposite) StartCoroutine(BuilderHold(false)); else Refresh();
        }
        void BuilderShare(int property)
        {
            if(busy || dragging || Lesson.Phase!=LessonPhase.BuilderShare) return;
            Lesson.AnswerBuilderShare(property);
            if(Lesson.BuilderStep==0) StartCoroutine(BuilderHold(true)); else Refresh();
        }
        IEnumerator BuilderHold(bool nextSign)
        {
            busy=true; Refresh();
            yield return new WaitForSecondsRealtime(Lesson.Dial.ReducedMotion ? .7f : 1.3f);
            if(nextSign) Lesson.NextBuilderSign();
            busy=false; AlignStart();
        }
        void AlignStart() { targetTurns=Lesson.Dial.Selected; turns=targetTurns; LayoutRing(); Refresh(); }
        bool TapPhase => Lesson.Phase==LessonPhase.GlyphNames || Lesson.Phase==LessonPhase.BuilderName || Lesson.Phase==LessonPhase.BuilderShare; // where a first miss logs only hint_requested
        void ToggleMotion() { Lesson.Dial.ReducedMotion=!Lesson.Dial.ReducedMotion; if(Lesson.Dial.ReducedMotion) turns=targetTurns; Refresh(); }
        void Refresh()
        {
            message.text=Lesson.Message; panel.gameObject.SetActive(Lesson.Message!=""); // Build I: Caspar's panel shows only when he speaks
            ShowSpeaker(); // Build V
            // In the marks (Part B) the names are the answer: the center shows the mark under the bracket, no start line, no count
            // (owner playtest v0.3, test 2: the readout gave the sign away).
            bool marks=Lesson.Phase==LessonPhase.GlyphWheel;
            // Build I (owner playtest, Sept 23): in the symbols the center holds the target's name, fixed while the wheel turns;
            // elsewhere the line above the center is the wheel's own challenge, and the center names the framed sign live.
            string challenge=Lesson.Challenge;
            destination.text=marks ? (roomArt ? "" : challenge) : Zodiac.Seats[Lesson.Dial.Selected].Name; // Build Z: with the eye, the symbols' target is posed in the eye and the label above it stays empty (a name there would give the answer away)
            destination.font=ringArt ? voiceFont : font; // Build AB: the Astrolabe's lettering is the Dial's serif
            if(destinationFacts!=null) destinationFacts.text=marks ? "" : FramedFacts(Lesson.Dial.Selected);
            start.text=marks ? (challenge=="" ? "" : roomArt ? "Find the symbol of\n"+challenge : "Find the symbol of") : challenge!="" ? challenge : Lesson.IsProblem ? "" : (Lesson.DialDormant ? "" : "Your sign: "+Zodiac.Seats[Lesson.Sun].Name);
            eyeLine=start.text; if(countShown) start.text=eyeLine.Replace("\n"," "); // Build AC: the line as set; folded onto one line while a count runs under it
            count.text=""; // Build I: no Count button, no running count (owner playtest, Sept 23); the worked demonstration still counts aloud
            phase.text=Lesson.Phase==LessonPhase.Complete ? "Two families complete. Two remain." :
                Lesson.Phase==LessonPhase.Paused ? "Paused for now · No Key yet" :
                Lesson.Phase==LessonPhase.AllLit ? "All twelve signs alight. The whole wheel burns." : // owner (worksheet section 3)
                Lesson.Phase==LessonPhase.GlyphNames ? "Name the symbols · " + Lesson.GlyphIndex + " of 12 named" : // owner (worksheet section 5)
                Lesson.Phase==LessonPhase.GlyphWheel ? "Find the symbols on the wheel · " + Lesson.GlyphIndex + " of 12 found" :
                Lesson.Phase==LessonPhase.Key2 ? "All twelve symbols mastered. Keeper Key 2 earned." :
                Lesson.Phase==LessonPhase.ModalityComplete ? "Three modalities complete. The wheel keeps both patterns." : // owner (worksheet section 10)
                Lesson.Phase==LessonPhase.ModalityPaused ? "Paused for now" :
                Lesson.Phase==LessonPhase.Polarity ? "The final pattern" : // owner (worksheet section 12)
                Lesson.Phase==LessonPhase.OppositePaused || Lesson.Phase==LessonPhase.BuilderPaused ? "Paused for now" :
                Lesson.Phase==LessonPhase.OppositesComplete ? "Six pairs found. The wheel's last pattern is yours." :
                Lesson.Phase==LessonPhase.Key4 ? "Three signs built. Keeper Key 4 earned." :
                Lesson.InBuilder ? "The builder · sign " + Mathf.Clamp(Lesson.BuilderStep == 0 ? Lesson.Built : Lesson.Built + 1, 1, 3) + " of 3" :
                Lesson.Phase==LessonPhase.Review ? Lesson.ReviewHeader :
                Lesson.IsProblem ? (Lesson.Phase==LessonPhase.Guided || Lesson.Phase==LessonPhase.ModalityGuided ? "Together" : Lesson.Phase==LessonPhase.Optional ? "Just for fun" : "On your own") : "Practice example"; // no level numbers on screen (owner, Sept 13)
            subtitle.text=Lesson.InBuilder ? "The Builder" : Lesson.InOpposites ? "Polarity and Opposites" : Lesson.InModalities ? "The Modalities" : "The Elemental Pattern"; // owner unit names (worksheet sections 10 and 12)
            bool active=Lesson.IsProblem && Lesson.Dial.Active && !busy;
            back.gameObject.SetActive(Lesson.IsProblem); forward.gameObject.SetActive(Lesson.IsProblem); seal.gameObject.SetActive(Lesson.IsProblem);
            back.interactable=forward.interactable=seal.interactable=active;
            next.gameObject.SetActive(((Lesson.Phase==LessonPhase.Encounter && !Lesson.IntroAuto) || Lesson.Phase==LessonPhase.Rule || Lesson.Phase==LessonPhase.Transfer || Lesson.Phase==LessonPhase.Polarity || Lesson.Phase==LessonPhase.OppositesComplete) && !busy);
            bool naming=Lesson.Phase==LessonPhase.BuilderName, sharing=Lesson.Phase==LessonPhase.BuilderShare;
            var options=naming ? Lesson.BuilderOptions : null;
            for(int i=0;i<4;i++){ builderNames[i].gameObject.SetActive(naming); builderNames[i].interactable=naming && !busy; builderNames[i].GetComponentInChildren<Text>().text=naming ? Zodiac.Seats[options[i]].Name : ""; }
            for(int i=0;i<3;i++){ builderShares[i].gameObject.SetActive(sharing); builderShares[i].interactable=sharing && !busy && !Lesson.Shared[i]; builderShares[i].GetComponentInChildren<Text>().text=DialLesson.ShareLabels[i]+(sharing && Lesson.Shared[i] ? ": shared" : ""); builderShares[i].GetComponent<Image>().color=sharing && Lesson.Shared[i] ? new Color(.29f,.27f,.28f) : new Color(.21f,.21f,.24f); }
            optional.gameObject.SetActive(Lesson.Phase==LessonPhase.Complete && Lesson.KeyEarned && Lesson.FamiliesComplete<=2 && (Slice==null || !SliceHidesOptional));
            countButton.gameObject.SetActive(false); // Build I: the Count button is off (owner playtest, Sept 23)
            askButton.gameObject.SetActive(Lesson.CanAsk && !busy); askButton.interactable=active; // no count in the marks: the target is a symbol, not a distance (owner playtest v0.3)
            motionText.text="Reduced motion: "+(Lesson.Dial.ReducedMotion ? "on" : "off");
            RefreshSeats(); LayoutRing(); Publish();
        }
        void RefreshSeats()
        {
            for(int i=0;i<12;i++)
            {
                bool selected=Lesson.Dial.Selected==i;
                bool showGlyph=Lesson.GlyphsShown && (Lesson.Lit[i] || Lesson.NamesHidden);
                bool hideName=Lesson.NamesHidden && !Lesson.NameRevealed[i];
                seatGlyphs[i].gameObject.SetActive(showGlyph); seatGlyphs[i].text=Zodiac.Seats[i].Glyph; seatGlyphs[i].color=Bone;
                var seatRectTransform=(RectTransform)seatTexts[i].transform; if(!ringArt) seatRectTransform.anchoredPosition=new Vector2(0,showGlyph ? -36 : -26); // Build AB: the Astrolabe places its own (OrientSeat) // top-anchored rect: -26 is the tile center; below the mark when one shows (owner playtest 3: names sat on the tile's top edge)
                seatTexts[i].fontSize=showGlyph ? 9 : 11;
                bool showMod=Lesson.LitMod[i] && !hideName; // Build A: the modality joins the element once the seat is lit in that unit
                seatTexts[i].text=(selected && Lesson.Dial.Rejected ? "× " : "")+(hideName ? "" : Zodiac.Seats[i].Name+(Lesson.Lit[i] && !showGlyph ? "\n"+Zodiac.Seats[i].Element : "")+(showMod ? (Lesson.Lit[i] && !showGlyph ? " · " : "\n")+Zodiac.ModalityAt(i) : "")+(showMod && Lesson.PolarityShown ? " · "+Zodiac.PolarityAt(i) : "")); // Build C: the side joins the kind, as a word, never color alone
                seatTexts[i].fontSize=showGlyph ? 9 : showMod ? 9 : 11;
                bool dormant=Lesson.DialDormant && !waking;
                seatTexts[i].color=dormant ? new Color(Bone.r,Bone.g,Bone.b,.3f) : Bone;
                if(ringArt) RefreshAstrolabeSeat(i,selected,showGlyph,hideName,showMod,dormant); // Build AB: the name on the band, the symbol and one fact on the tablet
                else Slots.Paint(seats[i].GetComponent<Image>(),dormant ? new Color(.1f,.1f,.12f) : Lesson.Lit[i] ? new Color(.29f,.27f,.28f) : new Color(.13f,.13f,.15f),dormant ? .35f : Lesson.Lit[i] ? 1f : .6f); // Build E: a seat file dims the same way
                geometry.Dormant=dormant; geometry.Dim=Lesson.Phase==LessonPhase.Review; if(faceImage.gameObject.activeSelf) faceImage.color=dormant ? new Color(.4f,.4f,.4f) : Color.white;
                if(!revealing && roomLight!=null) roomLight.fillAmount=dormant ? 0 : 1; // Build Z: the glow sleeps with the Dial
                if(!revealing && ringLight!=null) ringLight.fillAmount=dormant ? 0 : 1; // Build AB: and the ring's
                seats[i].interactable=Lesson.Dial.Active && !busy;
            }
        }
        void FocusNext(int direction)
        {
            int index=navigation.FindIndex(b=>b.gameObject==EventSystem.current.currentSelectedGameObject);
            for(int n=0;n<navigation.Count;n++)
            {
                index=(index+direction+navigation.Count)%navigation.Count;
                var b=navigation[index]; if(b.gameObject.activeInHierarchy && b.interactable) { b.Select(); break; }
            }
        }
        public WebState Snapshot()
        {
            var labels=new string[12];for(int i=0;i<12;i++) labels[i]=Lesson.SeatLabel(i);
            var glyphs=new string[12];for(int i=0;i<12;i++) glyphs[i]=Zodiac.Seats[i].Glyph;
            var box=FitBox.Visible; var state=new WebState {message=FitBox.Whole(message),casparPage=box!=null?box.Page+1:0,casparPages=box!=null?box.Pages:0,casparShown=box!=null?box.Shown:"",destination=(dragging ? "Passing: " : "Selected: ")+(Lesson.Phase==LessonPhase.GlyphWheel ? "symbol "+Zodiac.Seats[Lesson.Dial.Selected].Glyph : Zodiac.Seats[Lesson.Dial.Selected].Name),start=Lesson.Phase==LessonPhase.GlyphWheel ? "" : Lesson.IsProblem ? "Start: "+Zodiac.Seats[Lesson.Dial.Start].Name : start.text,challenge=Lesson.Challenge,phase=phase.text,count=count.text,seats=labels,
                active=Lesson.Dial.Active && !busy,canContinue=next.gameObject.activeSelf,canOptional=optional.gameObject.activeSelf,
                reducedMotion=Lesson.Dial.ReducedMotion,keyEarned=Lesson.KeyEarned,dormant=Lesson.DialDormant,introAuto=Lesson.IntroAuto,busy=busy,review=Lesson.Phase==LessonPhase.Review,wheelComplete=Lesson.WheelComplete,familiesComplete=Lesson.FamiliesComplete,
                glyphs=glyphs,namesHidden=Lesson.NamesHidden,glyphWheel=Lesson.Phase==LessonPhase.GlyphWheel,canAsk=Lesson.CanAsk && !busy,hintLevel=Lesson.Dial.HintLevel,cleanRuns=Lesson.CleanRuns,practice=Lesson.Practice,hard=Lesson.Hard,unit=Lesson.InBuilder ? "builder" : Lesson.InOpposites ? "opposites" : Lesson.InModalities ? "modalities" : Lesson.GlyphsShown && !(Lesson.WheelComplete && Lesson.Key2Earned && Lesson.Phase==LessonPhase.Key2) ? "symbols" : "elements",modalitiesComplete=Lesson.ModalitiesComplete,litModCount=Lesson.LitMod.Count(v=>v),step=Lesson.Dial.Forward,key2=Lesson.Key2Earned,keys=Lesson.Keys,glyphTarget=Lesson.Phase==LessonPhase.GlyphWheel && Lesson.Dial.Target>=0 ? Zodiac.Seats[Lesson.Dial.Target].Name : "",
                polarityShown=Lesson.PolarityShown,oppositesComplete=Lesson.OppositesComplete,pairsKnown=Lesson.PairsKnown,key4=Lesson.Key4Earned,built=Lesson.Built,builderIndex=Lesson.Built,
                builderStep=Lesson.Phase==LessonPhase.BuilderName ? "name" : Lesson.Phase==LessonPhase.BuilderOpposite ? "opposite" : Lesson.Phase==LessonPhase.BuilderShare ? "share" : "",
                builderAsk=Lesson.InBuilder && Lesson.BuilderTarget>=0 ? Zodiac.Seats[Lesson.BuilderTarget].Element+", "+Zodiac.ModalityAt(Lesson.BuilderTarget).ToLowerInvariant() : "",
                builderOptions=Lesson.Phase==LessonPhase.BuilderName ? Lesson.BuilderOptions.Select(o=>Zodiac.Seats[o].Name).ToArray() : new string[0],
                shared=Lesson.Phase==LessonPhase.BuilderShare ? Enumerable.Range(0,3).Where(i=>Lesson.Shared[i]).Select(i=>DialLesson.ShareLabels[i]).ToArray() : new string[0],
                canBuilderName=Lesson.Phase==LessonPhase.BuilderName && !busy,canBuilderShare=Lesson.Phase==LessonPhase.BuilderShare && !busy,
                speaker=panel.gameObject.activeInHierarchy?(DialSpeaking?DialLesson.DialSpeaker:DialLesson.CasparSpeaker):"",dialRoom=roomArt,dialLit=roomLight!=null && roomLight.gameObject.activeSelf ? roomLight.fillAmount : 0,revealing=revealNow,revealsPlayed=Lesson.RevealsPlayed.OrderBy(p=>p).ToArray(),eyeText=roomArt ? start.text : "",eyeSize=roomArt && start.text!="" ? Mathf.RoundToInt(start.cachedTextGenerator.fontSizeUsedForBestFit/Mathf.Max(.01f,canvas.scaleFactor)) : 0,signLabel=destination.text,dialVoice=panel.GetComponent<FitBox>()!=null && panel.GetComponent<FitBox>().DialVoiceShown && panel.gameObject.activeInHierarchy,dialBoxHeight=panel.gameObject.activeInHierarchy?panel.sizeDelta.y:0,artSet=Slots.Set,artFiles=Slots.ArtFiles,soundFiles=Slots.SoundFiles,muted=Sound.Muted,lastCue=Sound.LastCue,cuesPlayed=Sound.Played};
            state.dialRing=ringArt; state.ringTurn=ringLayer!=null ? ringLayer.localEulerAngles.z : 0; // Build AB
            state.dialWake=wakeStep; state.dialLook=WakeLook; // the wake-up: its step (0 to 4) and the looks it blends
            if(seats[0]!=null){ var seatSize=((RectTransform)seats[0].transform).sizeDelta; state.seatRadius=SeatR; state.seatSize=new[]{seatSize.x,seatSize.y}; } // Build AC follow-up: read, not set
            state.seatNames=seatTexts.Select(t=>t!=null ? t.text : "").ToArray(); state.seatFacts=Enumerable.Range(0,12).Select(i=>ringArt ? seatFacts[i].text : "").ToArray();
            state.framedFacts=destinationFacts!=null ? System.Text.RegularExpressions.Regex.Replace(destinationFacts.text,"<[^>]+>","") : "";
            Slice?.Fill(state); return state;
        }
        // Build AC (owner, Oct 1): while a count word shows, the challenge keeps the glass's upper part and the count its lower line; otherwise the challenge has the whole glass
        void LayoutEye()
        {
            if(!ringArt || start==null || count==null) return; bool show=count.text.Length>0; if(eyeLaidOut && show==countShown) return; countShown=show; eyeLaidOut=true;
            var r=start.rectTransform; r.anchoredPosition=new Vector2(0,-(show ? EyeYCount : EyeY)); r.sizeDelta=show ? EyeBoxCount : EyeBox; start.resizeTextMinSize=show ? EyeMinCount : EyeMin; start.resizeTextMaxSize=show ? EyeMaxCount : EyeMax;
            start.text=show ? eyeLine.Replace("\n"," ") : eyeLine;
        }
        public void Publish()
        {
            if(message==null || next==null)return;
            LayoutEye();
#if UNITY_WEBGL && !UNITY_EDITOR
            if(!UnityEngine.Rendering.SplashScreen.isFinished)return;
#endif
            webPublished=true;
            string json=JsonUtility.ToJson(Snapshot()); if(json==announced)return; announced=json;
#if UNITY_WEBGL && !UNITY_EDITOR
            DialPublish(json);
#endif
        }
        // Build V: the plate names the Dial when the line shown is its challenge, Caspar otherwise.
        bool DialSpeaking => Lesson.Speaker==DialLesson.DialSpeaker && FitBox.Whole(message)==Lesson.Message && Lesson.Message!="";
        void ShowSpeaker() { var fit=panel.GetComponent<FitBox>(); if(fit!=null) fit.SetSpeaker(DialSpeaking); }
        void LateUpdate() { ShowSpeaker(); SyncWake(); }
        // ---- The Dial's wake-up (owner, Oct 1; 86bcbn6w6: 2a A, 2b B, 2c A to C) ----
        // Five steps by the Keys earned, blended from three drawn looks: worn on the first visit, halfway to today's at Key 1, today's at Key 2,
        // halfway to bright at Key 3, bright and new at Key 4. A look whose file is not in yet stands in with today's, so the final passes are a
        // file drop. The eye is drawn the same in every look (2c D is excluded): it never changes.
        public static readonly string[] WakeLooks = { "-worn", "", "-bright" }, WakeLookNames = { "worn", "today", "bright" };
        public static (int from, int to, float t) WakeBlend(int step) => step <= 0 ? (0, 0, 0f) : step == 1 ? (0, 1, .5f) : step == 2 ? (1, 1, 0f) : step == 3 ? (1, 2, .5f) : (2, 2, 0f);
        public int WakeStep { get => wakeStep; set { int v = Mathf.Clamp(value, 0, 4); if (v == wakeStep) return; wakeStep = v; ApplyWake(); } }
        int wakeStep = -1;
        sealed class WakeLayer { public Image Base, Over; public string Slot; }
        readonly List<WakeLayer> wakeLayers = new List<WakeLayer>();
        void WakeLayerFor(Image baseImage, string slot)
        {
            if (baseImage == null) return;
            var r = new GameObject(slot + " (the next look)", typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(baseImage.transform, false);
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; r.SetAsFirstSibling(); // over its own layer, under the layers above it
            var over = r.gameObject.AddComponent<Image>(); over.raycastTarget = false; over.type = baseImage.type; over.fillMethod = baseImage.fillMethod; over.fillOrigin = baseImage.fillOrigin; over.fillClockwise = baseImage.fillClockwise;
            var bleed = baseImage.GetComponent<Bleed>(); if (bleed != null) Bleed.Add(over, bleed.Column, bleed.How, bleed.EdgeShade); // Part 2: it reaches the screen's edges as its layer does
            r.gameObject.SetActive(false); wakeLayers.Add(new WakeLayer { Base = baseImage, Over = over, Slot = slot });
        }
        Sprite LookOf(string slot, int look) => Slots.Image(slot + WakeLooks[look]) ?? Slots.Image(slot); // a look without its file stands in with today's
        void ApplyWake()
        {
            var (a, b, t) = WakeBlend(Mathf.Max(0, wakeStep));
            foreach (var w in wakeLayers)
            {
                var sa = LookOf(w.Slot, a); var sb = LookOf(w.Slot, b); if (sa != null && w.Base.sprite != sa) w.Base.sprite = sa;
                bool blend = t > 0 && sb != null && sb != sa; w.Over.gameObject.SetActive(blend); if (blend) w.Over.sprite = sb;
            }
            SyncWake();
        }
        void SyncWake()
        {
            if (wakeLayers.Count == 0) return; float t = WakeBlend(Mathf.Max(0, wakeStep)).t;
            foreach (var w in wakeLayers) if (w.Over.gameObject.activeSelf) { w.Over.fillAmount = w.Base.fillAmount; var c = w.Base.color; w.Over.color = new Color(c.r, c.g, c.b, c.a * t); }
        }
        // For the web state: the looks the step blends ("worn", "worn + today", "today", "today + bright", "bright"), each marked when its files stand in.
        public string WakeLook
        {
            get
            {
                if (wakeStep < 0 || wakeLayers.Count == 0) return "";
                var (a, b, t) = WakeBlend(wakeStep); string Name(int look) => WakeLookNames[look] + (look != 1 && wakeLayers.Any(w => Slots.Image(w.Slot + WakeLooks[look]) == null) ? " (stand-in: today's)" : "");
                return t > 0 ? Name(a) + " + " + Name(b) : Name(a);
            }
        }
        [Preserve] public void WebAction(string command)
        {
            if(Inert) { ExtraActions?.Invoke(command); return; }
            if(command=="forward")Step(1,DialInput.Accessible);
            else if(command=="back")Step(-1,DialInput.Accessible);
            else if(command=="keyboard-forward")Step(1,DialInput.Keyboard);
            else if(command=="keyboard-back")Step(-1,DialInput.Keyboard);
            else if(command=="seal")Commit();
            else if(command=="continue")ContinueLesson();
            else if(command=="optional") { if(!busy) { Lesson.BeginOptional(); AlignStart(); } }
            else if(command=="motion")ToggleMotion();
            else if(command=="skip-reveal") skipReveal=true; // Build Z: the tap that skips a reveal
            else if(command=="motion-on") { Lesson.Dial.ReducedMotion=true; Refresh(); }
            else if(command=="ask-caspar") AskCaspar();
            else if(command=="caspar-page") { var box=FitBox.Visible; if(box!=null && box.More!=null && box.More.gameObject.activeInHierarchy) box.Turn(); Publish(); } // Build S: the box's Continue
            else if(command.StartsWith("builder-name:") && int.TryParse(command.Substring(13),out int nameSlot)) BuilderName(nameSlot);
            else if(command.StartsWith("builder-share:") && int.TryParse(command.Substring(14),out int shareProperty)) BuilderShare(shareProperty);
            else if(command.StartsWith("seat:") && int.TryParse(command.Substring(5),out int seat) && seat>=0 && seat<12)SelectSeat(seat,DialInput.Accessible);
            else ExtraActions?.Invoke(command);
        }
        // ---- Build AB (owner, Sept 30): the Astrolabe. Each seat is a tap area over its segment (name band and tablet), turned to face the
        // rim; its name curves along the name band, its symbol and one fact run along the tablet; on the lower half they turn upright. ----
        static void Centre(RectTransform r,float y,float w,float h){ r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=new Vector2(0,y); r.sizeDelta=new Vector2(w,h); }
        void Engraved(Text t){ var e=t.gameObject.AddComponent<Outline>(); e.effectColor=Engrave; e.effectDistance=new Vector2(1,-1); var sh=t.gameObject.AddComponent<Shadow>(); sh.effectColor=new Color(0,0,0,.6f); sh.effectDistance=new Vector2(0,-1.2f); }
        void BuildAstrolabeSeat(int i)
        {
            var seat=(RectTransform)seats[i].transform; seat.sizeDelta=new Vector2(58,66); // 58 along the ring, 66 across it (r 90 to 156): one segment, the recess and the window
            var hit=seats[i].GetComponent<Image>(); hit.color=new Color(1,1,1,0); hit.canvasRenderer.cullTransparentMesh=false; // an invisible tap area takes taps only with culling off
            var name=seatTexts[i]; Centre(name.rectTransform,NameRadius-AstroSeatRadius,110,20); name.font=voiceFont; name.color=Gold; name.horizontalOverflow=HorizontalWrapMode.Overflow; name.verticalOverflow=VerticalWrapMode.Overflow;
            Engraved(name); seatArcs[i]=name.gameObject.AddComponent<ArcText>(); seatArcs[i].Radius=NameRadius; // after the outline, so its copies bend too
            Centre(seatGlyphs[i].rectTransform,TabletRadius-FactLift-AstroSeatRadius,30,24); seatGlyphs[i].fontSize=18; seatGlyphs[i].alignment=TextAnchor.MiddleCenter; Engraved(seatGlyphs[i]);
            seatFacts[i]=Label(seat,"",0,0,70,16,11); Centre(seatFacts[i].rectTransform,TabletRadius+FactLift-AstroSeatRadius,70,16); seatFacts[i].font=voiceFont; seatFacts[i].color=FactInk; seatFacts[i].horizontalOverflow=HorizontalWrapMode.Overflow; Engraved(seatFacts[i]);
        }
        // Turn a seat to face the rim; on the lower half turn it upright, which moves its parts to the other side of its centre and bends the name the other way
        void OrientSeat(int i,Vector2 p)
        {
            float a=Mathf.Atan2(p.y,p.x)*Mathf.Rad2Deg; bool lower=p.y<-.01f*SeatR;
            seats[i].transform.localRotation=Quaternion.Euler(0,0,a-90+(lower?180:0));
            if(lower==seatLower[i] && (seatArcs[i].Radius<0)==lower) return; seatLower[i]=lower; float s=lower?-1:1; // the arc's sign records the side it was set for
            seatTexts[i].rectTransform.anchoredPosition=new Vector2(0,s*(NameRadius-AstroSeatRadius)); seatArcs[i].Radius=s*NameRadius;
            seatGlyphs[i].rectTransform.anchoredPosition=new Vector2(0,s*(TabletRadius-FactLift-AstroSeatRadius));
            seatFacts[i].rectTransform.anchoredPosition=new Vector2(0,s*(TabletRadius+FactLift-AstroSeatRadius));
        }
        // One fact per seat (owner, Sept 30): the newest the seat has learned, as a word, never colour alone
        public string SeatFact(int i)
        {
            bool hideName=Lesson.NamesHidden && !Lesson.NameRevealed[i]; if(hideName) return "";
            bool showMod=Lesson.LitMod[i];
            return showMod && Lesson.PolarityShown ? Zodiac.PolarityAt(i) : showMod ? Zodiac.ModalityAt(i) : Lesson.Lit[i] ? Zodiac.Seats[i].Element : "";
        }
        void RefreshAstrolabeSeat(int i,bool selected,bool showGlyph,bool hideName,bool showMod,bool dormant)
        {
            var name=Zodiac.Seats[i].Name; seatTexts[i].text=(selected && Lesson.Dial.Rejected ? "× " : "")+(hideName ? "" : name); seatTexts[i].fontSize=name.Length>=9 ? 13 : 14; // Build AC: Capricorn and Sagittarius at 13 px, the rest at 14, inside a recess of about 67 px
            seatFacts[i].text=SeatFact(i); // title case in the names' serif: narrower than capitals, so it can be larger
            float alpha=dormant ? .3f : Lesson.Lit[i] || Lesson.NamesHidden ? 1f : .72f; // lit seats in full gold, the rest a shade dimmer
            bool kin=!dormant && Lesson.Kin[i%4]; // a completed element family: its tablets' facts turn gold (the family lines would cross the medallion's words)
            seatTexts[i].color=new Color(Gold.r,Gold.g,Gold.b,alpha); seatGlyphs[i].color=new Color(Gold.r,Gold.g,Gold.b,alpha); seatFacts[i].color=kin ? Gold : new Color(FactInk.r,FactInk.g,FactInk.b,alpha);
        }
        // The framed sign's learned facts, under its name above the eye; its element in its own colour (Build S)
        string FramedFacts(int i)
        {
            if(Lesson.NamesHidden && !Lesson.NameRevealed[i]) return "";
            var facts=new System.Collections.Generic.List<string>();
            if(Lesson.Lit[i]) facts.Add("<color="+FitBox.Elements[i%4].hex+">"+Zodiac.Seats[i].Element+"</color>");
            if(Lesson.LitMod[i]) facts.Add(Zodiac.ModalityAt(i));
            if(Lesson.LitMod[i] && Lesson.PolarityShown) facts.Add(Zodiac.PolarityAt(i));
            return string.Join(" · ",facts);
        }
        RectTransform Rect(string name,Transform parent,float x,float top,float width,float height)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,.5f);
            r.anchoredPosition=new Vector2(x,-top);r.sizeDelta=new Vector2(width,height);
            if(parent.GetComponent<Canvas>()!=null){r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;}
            return r;
        }
        Text Label(Transform parent,string text,float x,float top,float width,float height,int size)
        {
            var label=Rect(text,parent,x,top,width,height).gameObject.AddComponent<Text>();label.font=font;label.text=text;
            label.fontSize=size;label.color=Bone;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
            return label;
        }
        Button MakeButton(Transform parent,string text,float x,float top,float width,float height,UnityEngine.Events.UnityAction action)
        {
            var r=Rect(text,parent,x,top,width,height);r.gameObject.AddComponent<Image>().color=new Color(.21f,.21f,.24f);
            var button=r.gameObject.AddComponent<Button>();button.onClick.AddListener(action);
            var colors=button.colors;colors.selectedColor=new Color(.85f,.7f,.55f);colors.highlightedColor=Color.white;button.colors=colors;
            Label(r,text,0,height/2,width-4,height-4,15);navigation.Add(button);return button;
        }
        // Build Z: a dark fade (the room's art stays readable under the title and under the wheel) and a soft shadow under a label
        static Sprite fadeDown, fadeUp;
        void Fade(Transform parent,float x,float top,float height,bool fromTop,float alpha)
        {
            ref Sprite cache=ref (fromTop ? ref fadeDown : ref fadeUp);
            if(cache==null){ var tex=new Texture2D(1,64,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp}; for(int y=0;y<64;y++){ float k=y/63f; tex.SetPixel(0,y,new Color(.08f,.07f,.09f,fromTop ? k*k : (1-k)*(1-k))); } tex.Apply(); cache=Sprite.Create(tex,new UnityEngine.Rect(0,0,1,64),new Vector2(.5f,.5f),100); }
            var r=Rect(fromTop ? "Fade under the title" : "Fade under the wheel",parent,x,top+height/2,360,height); var image=r.gameObject.AddComponent<Image>(); image.sprite=cache; image.color=new Color(1,1,1,alpha); image.raycastTarget=false;
            Bleed.Add(image, root, Bleed.Mode.Clamp, 1); // Part 2: the fades run to the screen's sides (and off its top or bottom), their gradient unchanged
        }
        Text Shade(Text label) { if(roomArt){ var shadow=label.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.85f); shadow.effectDistance=new Vector2(1,-1.5f); } return label; }
        void Bar(Transform parent,float x,float top,float width,float height)
        { var r=Rect("Bracket bar",parent,x,top,width,height);var image=r.gameObject.AddComponent<Image>();image.color=Bone;image.raycastTarget=false; }
    }
}
