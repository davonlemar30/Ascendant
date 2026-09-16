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
        readonly Button[] builderNames = new Button[4], builderShares = new Button[3]; // Build C: the builder's name and share steps
        RectTransform root, ring, bracket;
        Canvas canvas;
        DialGeometry geometry;
        Font font;
        bool busy, dragging, webPublished;
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
            public string message, destination, start, phase, count;
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
            public bool keyRevealed, keyInserted, ended, canInsert, canSliceContinue, canName, canBirth, canBirthDate, canSignPick, canChangeBirth;
            public int atriumStage, dueCount, reviewIndex, reviewTotal;
            public bool canEnterWing, canEnterSeals, canLeaveWing, canLeaveReview, v02Complete, resumed;
            public string reviewMode = "", reviewSign = "", reviewSummary = "", hubNote = "";
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
        }
        void Awake()
        {
            Application.runInBackground=true;
            gameObject.name = "CelestialDial";
            Lesson = new DialLesson(() => Time.realtimeSinceStartupAsDouble);
            Lesson.Dial.Logged += e => Debug.Log("[CelestialDial] " + JsonUtility.ToJson(e));
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GlyphFont = Resources.Load<Font>("Fonts/NotoSansSymbols") ?? font;
            var canvasObject = new GameObject("Greybox Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ScaleCanvas();
            var bg = canvasObject.AddComponent<Image>(); bg.color = Charcoal;
            root = Rect("Portrait", canvasObject.transform, 0, 0, 360, 800);
            Label(root, "THE ZODIAC WING", 0, 32, 340, 24, 18);
            subtitle = Label(root, "The Elemental Pattern", 0, 62, 330, 22, 14);
            ring = Rect("Twelve-seat Dial", root, 0, 270, 332, 332);
            var hit = ring.gameObject.AddComponent<Image>(); hit.color = new Color(0,0,0,.001f);
            ring.gameObject.AddComponent<DialDrag>().View = this;
            var drawing = Rect("Ring and family connections", ring, 0,166,332,332);
            geometry = drawing.gameObject.AddComponent<DialGeometry>(); geometry.View = this; 
            for (int i=0;i<12;i++)
            {
                int seat = i;
                seats[i] = MakeButton(ring, Zodiac.Seats[i].Name, 0,166,52,52, () => SelectSeat(seat, ClickMethod(DialInput.DirectSeat)));
                seats[i].gameObject.AddComponent<DialDrag>().View = this;
                seatTexts[i] = seats[i].GetComponentInChildren<Text>(); seatTexts[i].fontSize = 11;
                seatTexts[i].horizontalOverflow = HorizontalWrapMode.Overflow; // Long names spill past the tile instead of breaking mid-word.
                // The symbol font sits low in its box, so the box sits high in the tile. (A v0.4 edit turned the rest of this line into a comment,
                // which left the seats on the plain text font: no marks on the Web, owner playtest v0.3.)
                seatGlyphs[i] = Label(seats[i].transform, "", 0, 6, 48, 24, 18); seatGlyphs[i].font = GlyphFont; seatGlyphs[i].horizontalOverflow = HorizontalWrapMode.Overflow; seatGlyphs[i].verticalOverflow = VerticalWrapMode.Overflow; seatGlyphs[i].gameObject.SetActive(false);
                var seatRect=(RectTransform)seats[i].transform; seatRect.anchorMin=seatRect.anchorMax=new Vector2(.5f,.5f);
            }
            bracket = Rect("Fixed focus bracket", root, -136,270,58,58);
            var outline = bracket.gameObject.AddComponent<Image>(); outline.color = Color.clear; outline.raycastTarget = false;
            var edge = bracket.gameObject.AddComponent<Outline>(); edge.effectColor = Bone; edge.effectDistance = new Vector2(2,2);
            // Four short bars visibly frame exactly one seat, without relying on color.
            Bar(bracket,-27,29,3,58); Bar(bracket,27,29,3,58);
            Bar(bracket,0,1,56,3); Bar(bracket,0,57,56,3);
            start = Label(root,"",0,223,188,30,13);
            destination = Label(root,"",0,271,188,50,20); // Names the sign under the bracket, live while dragging (owner request, Sept 12; reverses the Sept 11 "no label" row).
            count = Label(root,"",0,319,178,36,15);
            Label(root,"Move  >  Inspect  >  Seal",0,441,340,24,14);
            var panel = Rect("Caspar instruction panel",root,0,526,324,128);
            panel.gameObject.AddComponent<Image>().color = new Color(.13f,.13f,.15f);
            Label(panel,"CASPAR",0,18,290,22,13);
            message = Label(panel,"",0,74,306,96,13);
            phase = Label(root,"",0,608,330,28,13);
            back = MakeButton(root,"Previous",-122,654,64,56,()=>Step(-1,ClickMethod(DialInput.BackStep)));
            seal = MakeButton(root,"SEAL",0,654,146,56,Commit); // Amended canon (Sept 11): "Keeper's Seal" renamed to "Seal".
            seal.GetComponent<Image>().color=Crimson; sealText=seal.GetComponentInChildren<Text>();
            forward = MakeButton(root,"Next",122,654,64,56,()=>Step(1,ClickMethod(DialInput.ForwardStep)));
            countButton=MakeButton(root,"Count",-60,714,100,48,()=> { Lesson.Dial.Count(); Refresh(); });
            askButton=MakeButton(root,"Ask Caspar for help",78,714,164,48,AskCaspar); askButton.GetComponentInChildren<Text>().fontSize=13; askButton.gameObject.SetActive(false);
            next=MakeButton(root,"Continue",0,654,190,56,ContinueLesson);
            for(int i=0;i<4;i++){ int slot=i; builderNames[i]=MakeButton(root,"",-78+(i%2)*156,654+(i/2)*60,150,56,()=>BuilderName(slot)); builderNames[i].gameObject.SetActive(false); }
            for(int i=0;i<3;i++){ int property=i; builderShares[i]=MakeButton(root,DialLesson.ShareLabels[i],-110+i*110,654,104,56,()=>BuilderShare(property)); builderShares[i].GetComponentInChildren<Text>().fontSize=13; builderShares[i].gameObject.SetActive(false); }
            optional=MakeButton(root,"Try one more (optional)",0,714,244,48,()=> { Lesson.BeginOptional(); AlignStart(); });
            var motion = MakeButton(root,"Reduced motion: off",0,768,216,48,ToggleMotion);
            motionText=motion.GetComponentInChildren<Text>(); motionText.fontSize=13;
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
            return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*136;
        }
        void LayoutRing()
        {
            if(bracket!=null)bracket.localScale=Vector3.one*(dragging ? 1.04f : 1f);
            for(int i=0;i<12;i++) ((RectTransform)seats[i].transform).anchoredPosition=SeatPosition(i);
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
                if(Lesson.Dial.ReducedMotion) { RefreshSeats(); yield return new WaitForSecondsRealtime(.6f); }
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
                Lesson.Dial.PositionSilently(beginning+n); targetTurns=turns=beginning+n;
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
            message.text=Lesson.Phase==LessonPhase.GlyphWheel ? "Watch. I turn until the symbol of "+Zodiac.Seats[Lesson.Dial.Target].Name+" sits under the bracket." : "Watch. I start at "+Zodiac.Seats[beginning].Name+" and count each sign after it."; RefreshSeats(); Publish();
            yield return new WaitForSecondsRealtime(Beat);
            for(int n=1;n<=steps;n++)
            {
                Lesson.Dial.PositionSilently(beginning+n); targetTurns=turns=beginning+n;
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
        void ToggleMotion() { Lesson.Dial.ReducedMotion=!Lesson.Dial.ReducedMotion; if(Lesson.Dial.ReducedMotion) turns=targetTurns; Refresh(); }
        void Refresh()
        {
            message.text=Lesson.Message;
            // In the marks (Part B) the names are the answer: the center shows the mark under the bracket, no start line, no count
            // (owner playtest v0.3, test 2: the readout gave the sign away).
            bool marks=Lesson.Phase==LessonPhase.GlyphWheel;
            destination.text=marks ? Zodiac.Seats[Lesson.Dial.Selected].Glyph : Zodiac.Seats[Lesson.Dial.Selected].Name;
            destination.font=marks ? GlyphFont : font;
            start.text=marks ? "" : Lesson.IsProblem ? "Start: "+Zodiac.Seats[Lesson.Dial.Start].Name : (Lesson.DialDormant ? "" : "Your sign: "+Zodiac.Seats[Lesson.Sun].Name);
            count.text=Lesson.Dial.Counting && !marks ? "Count: "+Lesson.Dial.MovementCount : "";
            phase.text=Lesson.Phase==LessonPhase.Complete ? "Two families complete. Two remain." :
                Lesson.Phase==LessonPhase.Paused ? "Paused for now · No Key yet" :
                Lesson.Phase==LessonPhase.AllLit ? "Four families complete. The whole wheel is lit." :
                Lesson.Phase==LessonPhase.GlyphNames ? "The symbols · " + Lesson.GlyphIndex + " of 12 named" :
                Lesson.Phase==LessonPhase.GlyphWheel ? "The symbols, in order · " + Lesson.GlyphIndex + " of 12" :
                Lesson.Phase==LessonPhase.Key2 ? "Twelve symbols read. Keeper Key 2 earned." :
                Lesson.Phase==LessonPhase.ModalityComplete ? "Three kinds complete. The wheel keeps both patterns." :
                Lesson.Phase==LessonPhase.ModalityPaused ? "Paused for now" :
                Lesson.Phase==LessonPhase.Polarity ? "The last pattern" :
                Lesson.Phase==LessonPhase.OppositePaused || Lesson.Phase==LessonPhase.BuilderPaused ? "Paused for now" :
                Lesson.Phase==LessonPhase.OppositesComplete ? "Six pairs. The wheel's last pattern is yours." :
                Lesson.Phase==LessonPhase.Key4 ? "Three signs built. Keeper Key 4 earned." :
                Lesson.InBuilder ? "The builder · sign " + Mathf.Clamp(Lesson.BuilderStep == 0 ? Lesson.Built : Lesson.Built + 1, 1, 3) + " of 3" :
                Lesson.Phase==LessonPhase.Review ? "Checking the seals" :
                Lesson.IsProblem ? (Lesson.Phase==LessonPhase.Guided || Lesson.Phase==LessonPhase.ModalityGuided ? "Together" : Lesson.Phase==LessonPhase.Optional ? "Just for fun" : "On your own") : "Practice example"; // no level numbers on screen (owner, Sept 13)
            subtitle.text=Lesson.InBuilder ? "The Builder" : Lesson.InOpposites ? "The Last Pattern" : Lesson.InModalities ? "The Second Pattern" : "The Elemental Pattern"; // placeholder unit names (owner writes)
            bool active=Lesson.IsProblem && Lesson.Dial.Active && !busy;
            back.gameObject.SetActive(Lesson.IsProblem); forward.gameObject.SetActive(Lesson.IsProblem); seal.gameObject.SetActive(Lesson.IsProblem);
            back.interactable=forward.interactable=seal.interactable=active;
            countButton.gameObject.SetActive(Lesson.IsProblem); countButton.interactable=active;
            next.gameObject.SetActive(((Lesson.Phase==LessonPhase.Encounter && !Lesson.IntroAuto) || Lesson.Phase==LessonPhase.Rule || Lesson.Phase==LessonPhase.Transfer || Lesson.Phase==LessonPhase.Polarity || Lesson.Phase==LessonPhase.OppositesComplete) && !busy);
            bool naming=Lesson.Phase==LessonPhase.BuilderName, sharing=Lesson.Phase==LessonPhase.BuilderShare;
            var options=naming ? Lesson.BuilderOptions : null;
            for(int i=0;i<4;i++){ builderNames[i].gameObject.SetActive(naming); builderNames[i].interactable=naming && !busy; builderNames[i].GetComponentInChildren<Text>().text=naming ? Zodiac.Seats[options[i]].Name : ""; }
            for(int i=0;i<3;i++){ builderShares[i].gameObject.SetActive(sharing); builderShares[i].interactable=sharing && !busy && !Lesson.Shared[i]; builderShares[i].GetComponentInChildren<Text>().text=DialLesson.ShareLabels[i]+(sharing && Lesson.Shared[i] ? ": shared" : ""); builderShares[i].GetComponent<Image>().color=sharing && Lesson.Shared[i] ? new Color(.29f,.27f,.28f) : new Color(.21f,.21f,.24f); }
            optional.gameObject.SetActive(Lesson.Phase==LessonPhase.Complete && Lesson.KeyEarned && Lesson.FamiliesComplete<=2 && (Slice==null || !SliceHidesOptional));
            countButton.gameObject.SetActive(Lesson.IsProblem && Lesson.Phase!=LessonPhase.Review && Lesson.Phase!=LessonPhase.GlyphWheel);
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
                var seatRectTransform=(RectTransform)seatTexts[i].transform; seatRectTransform.anchoredPosition=new Vector2(0,showGlyph ? -36 : -26); // top-anchored rect: -26 is the tile center; below the mark when one shows (owner playtest 3: names sat on the tile's top edge)
                seatTexts[i].fontSize=showGlyph ? 9 : 11;
                bool showMod=Lesson.LitMod[i] && !hideName; // Build A: the modality joins the element once the seat is lit in that unit
                seatTexts[i].text=(selected && Lesson.Dial.Rejected ? "× " : "")+(hideName ? "" : Zodiac.Seats[i].Name+(Lesson.Lit[i] && !showGlyph ? "\n"+Zodiac.Seats[i].Element : "")+(showMod ? (Lesson.Lit[i] && !showGlyph ? " · " : "\n")+Zodiac.ModalityAt(i) : "")+(showMod && Lesson.PolarityShown ? " · "+Zodiac.PolarityAt(i) : "")); // Build C: the side joins the kind, as a word, never color alone
                seatTexts[i].fontSize=showGlyph ? 9 : showMod ? 9 : 11;
                bool dormant=Lesson.DialDormant && !waking;
                seatTexts[i].color=dormant ? new Color(Bone.r,Bone.g,Bone.b,.3f) : Bone;
                seats[i].GetComponent<Image>().color=dormant ? new Color(.1f,.1f,.12f) : Lesson.Lit[i] ? new Color(.29f,.27f,.28f) : new Color(.13f,.13f,.15f);
                geometry.Dormant=dormant;
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
            var state=new WebState {message=message.text,destination=(dragging ? "Passing: " : "Selected: ")+(Lesson.Phase==LessonPhase.GlyphWheel ? "symbol "+Zodiac.Seats[Lesson.Dial.Selected].Glyph : Zodiac.Seats[Lesson.Dial.Selected].Name),start=start.text,phase=phase.text,count=count.text,seats=labels,
                active=Lesson.Dial.Active && !busy,canContinue=next.gameObject.activeSelf,canOptional=optional.gameObject.activeSelf,
                reducedMotion=Lesson.Dial.ReducedMotion,keyEarned=Lesson.KeyEarned,dormant=Lesson.DialDormant,introAuto=Lesson.IntroAuto,busy=busy,review=Lesson.Phase==LessonPhase.Review,wheelComplete=Lesson.WheelComplete,familiesComplete=Lesson.FamiliesComplete,
                glyphs=glyphs,namesHidden=Lesson.NamesHidden,glyphWheel=Lesson.Phase==LessonPhase.GlyphWheel,canAsk=Lesson.CanAsk && !busy,hintLevel=Lesson.Dial.HintLevel,cleanRuns=Lesson.CleanRuns,practice=Lesson.Practice,hard=Lesson.Hard,unit=Lesson.InBuilder ? "builder" : Lesson.InOpposites ? "opposites" : Lesson.InModalities ? "modalities" : Lesson.GlyphsShown && !(Lesson.WheelComplete && Lesson.Key2Earned && Lesson.Phase==LessonPhase.Key2) ? "symbols" : "elements",modalitiesComplete=Lesson.ModalitiesComplete,litModCount=Lesson.LitMod.Count(v=>v),step=Lesson.Dial.Forward,key2=Lesson.Key2Earned,keys=Lesson.Keys,glyphTarget=Lesson.Phase==LessonPhase.GlyphWheel && Lesson.Dial.Target>=0 ? Zodiac.Seats[Lesson.Dial.Target].Name : "",
                polarityShown=Lesson.PolarityShown,oppositesComplete=Lesson.OppositesComplete,pairsKnown=Lesson.PairsKnown,key4=Lesson.Key4Earned,built=Lesson.Built,builderIndex=Lesson.Built,
                builderStep=Lesson.Phase==LessonPhase.BuilderName ? "name" : Lesson.Phase==LessonPhase.BuilderOpposite ? "opposite" : Lesson.Phase==LessonPhase.BuilderShare ? "share" : "",
                builderAsk=Lesson.InBuilder && Lesson.BuilderTarget>=0 ? Zodiac.Seats[Lesson.BuilderTarget].Element+", "+Zodiac.ModalityAt(Lesson.BuilderTarget).ToLowerInvariant() : "",
                builderOptions=Lesson.Phase==LessonPhase.BuilderName ? Lesson.BuilderOptions.Select(o=>Zodiac.Seats[o].Name).ToArray() : new string[0],
                shared=Lesson.Phase==LessonPhase.BuilderShare ? Enumerable.Range(0,3).Where(i=>Lesson.Shared[i]).Select(i=>DialLesson.ShareLabels[i]).ToArray() : new string[0],
                canBuilderName=Lesson.Phase==LessonPhase.BuilderName && !busy,canBuilderShare=Lesson.Phase==LessonPhase.BuilderShare && !busy};
            Slice?.Fill(state); return state;
        }
        public void Publish()
        {
            if(message==null || next==null)return;
#if UNITY_WEBGL && !UNITY_EDITOR
            if(!UnityEngine.Rendering.SplashScreen.isFinished)return;
#endif
            webPublished=true;
            string json=JsonUtility.ToJson(Snapshot()); if(json==announced)return; announced=json;
#if UNITY_WEBGL && !UNITY_EDITOR
            DialPublish(json);
#endif
        }
        [Preserve] public void WebAction(string command)
        {
            if(command=="forward")Step(1,DialInput.Accessible);
            else if(command=="back")Step(-1,DialInput.Accessible);
            else if(command=="keyboard-forward")Step(1,DialInput.Keyboard);
            else if(command=="keyboard-back")Step(-1,DialInput.Keyboard);
            else if(command=="seal")Commit();
            else if(command=="count") { if(!busy)Lesson.Dial.Count(); Refresh(); }
            else if(command=="continue")ContinueLesson();
            else if(command=="optional") { if(!busy) { Lesson.BeginOptional(); AlignStart(); } }
            else if(command=="motion")ToggleMotion();
            else if(command=="motion-on") { Lesson.Dial.ReducedMotion=true; Refresh(); }
            else if(command=="ask-caspar") AskCaspar();
            else if(command.StartsWith("builder-name:") && int.TryParse(command.Substring(13),out int nameSlot)) BuilderName(nameSlot);
            else if(command.StartsWith("builder-share:") && int.TryParse(command.Substring(14),out int shareProperty)) BuilderShare(shareProperty);
            else if(command.StartsWith("seat:") && int.TryParse(command.Substring(5),out int seat) && seat>=0 && seat<12)SelectSeat(seat,DialInput.Accessible);
            else ExtraActions?.Invoke(command);
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
        void Bar(Transform parent,float x,float top,float width,float height)
        { var r=Rect("Bracket bar",parent,x,top,width,height);var image=r.gameObject.AddComponent<Image>();image.color=Bone;image.raycastTarget=false; }
    }
}
