using System;
using System.Collections;
using System.Collections.Generic;
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
        public Action<string> ExtraActions;       // Slice commands arrive through the same WebAction entry.
        public RectTransform Root => root;
        public RectTransform Ring => ring;
        public Canvas UiCanvas => canvas;
        public Font UiFont => font;
        public bool Busy => busy;
        public void RegisterNavigation(Selectable s) { navigation.Add(s); }
        public void ForceRefresh() { Refresh(); }
        public bool firstDragResistance = false; // Test variable. Owner dropped it after the solo playtest: it went unnoticed.
        public bool inertiaEnabled = true;
        public const float PixelsPerDetent = 55;
        public const float SnapSeconds = .12f;
        readonly List<Selectable> navigation = new List<Selectable>();
        readonly Button[] seats = new Button[12];
        readonly Text[] seatTexts = new Text[12];
        Text message, destination, start, count, phase, sealText, motionText;
        Button seal, back, forward, countButton, next, optional;
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
            public bool active, canContinue, canOptional, reducedMotion, keyEarned;
            public string screen = "wing", playerName = "", caspar = "", note = "";
            public bool keyRevealed, keyInserted, ended, canInsert, canSliceContinue, canName, canBirth;
            public int locksFilled;
        }
        void Awake()
        {
            Application.runInBackground=true;
            gameObject.name = "CelestialDial";
            Lesson = new DialLesson(() => Time.realtimeSinceStartupAsDouble);
            Lesson.Dial.Logged += e => Debug.Log("[CelestialDial] " + JsonUtility.ToJson(e));
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Greybox Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ScaleCanvas();
            var bg = canvasObject.AddComponent<Image>(); bg.color = Charcoal;
            root = Rect("Portrait", canvasObject.transform, 0, 0, 360, 800);
            Label(root, "THE ZODIAC WING", 0, 32, 340, 24, 18);
            Label(root, "The Elemental Pattern", 0, 62, 330, 22, 14);
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
                var seatRect=(RectTransform)seats[i].transform; seatRect.anchorMin=seatRect.anchorMax=new Vector2(.5f,.5f);
            }
            bracket = Rect("Fixed focus bracket", root, -136,270,58,58);
            var outline = bracket.gameObject.AddComponent<Image>(); outline.color = Color.clear; outline.raycastTarget = false;
            var edge = bracket.gameObject.AddComponent<Outline>(); edge.effectColor = Bone; edge.effectDistance = new Vector2(2,2);
            // Four short bars visibly frame exactly one seat, without relying on color.
            Bar(bracket,-27,29,3,58); Bar(bracket,27,29,3,58);
            Bar(bracket,0,1,56,3); Bar(bracket,0,57,56,3);
            start = Label(root,"",0,223,188,30,13);
            destination = Label(root,"",0,271,188,50,17);
            count = Label(root,"",0,319,178,36,15);
            Label(root,"Move  >  Inspect  >  Seal",0,441,340,24,14);
            var panel = Rect("Caspar instruction panel",root,0,526,324,128);
            panel.gameObject.AddComponent<Image>().color = new Color(.13f,.13f,.15f);
            Label(panel,"CASPAR",0,18,290,22,13);
            message = Label(panel,"",0,74,306,96,14);
            phase = Label(root,"",0,608,330,28,13);
            back = MakeButton(root,"Previous",-122,654,64,56,()=>Step(-1,ClickMethod(DialInput.BackStep)));
            seal = MakeButton(root,"KEEPER'S\nSEAL",0,654,146,56,Commit);
            seal.GetComponent<Image>().color=Crimson; sealText=seal.GetComponentInChildren<Text>();
            forward = MakeButton(root,"Next",122,654,64,56,()=>Step(1,ClickMethod(DialInput.ForwardStep)));
            countButton=MakeButton(root,"Count",0,714,80,48,()=> { Lesson.Dial.Count(); Refresh(); });
            next=MakeButton(root,"Continue",0,654,190,56,ContinueLesson);
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
        public void Commit()
        {
            if(busy || dragging) return;
            // A seal commits the selected detent, never a passing seat between detents.
            turns=targetTurns; LayoutRing();
            var result=Lesson.Seal(); if(result==null)return;
            Refresh();
            if(result.correctness) StartCoroutine(Correct(result));
            else if(Lesson.Dial.Attempts>=3) StartCoroutine(Demonstrate());
        }
        IEnumerator Correct(DialEvent result)
        {
            busy=true;
            Lesson.Lit[result.selected_destination] |= Lesson.Phase != LessonPhase.Optional;
            message.text="Yes. "+Zodiac.Seats[result.selected_destination].Name+" is "+Zodiac.Seats[result.selected_destination].Element+", like your start sign.\n"+(result.evidence_eligible ? "You found that one on your own." : "We found that one together.");
            RefreshSeats(); Publish();
            yield return new WaitForSecondsRealtime(.65f);
            yield return ReturnHome();
            Lesson.AfterCorrect(result); busy=false; AlignStart();
        }
        IEnumerator Demonstrate()
        {
            busy=true; int beginning=Lesson.Dial.Start;
            Lesson.Dial.PositionSilently(beginning); targetTurns=turns=beginning; LayoutRing();
            for(int n=1;n<=4;n++)
            {
                yield return new WaitForSecondsRealtime(Lesson.Dial.ReducedMotion ? .08f : .22f);
                Lesson.Dial.PositionSilently(beginning+n); targetTurns=turns=beginning+n;
                message.text="Watch. I start at "+Zodiac.Seats[beginning].Name+" and count each sign after it.\n"+n+": "+Zodiac.Seats[Zodiac.Wrap(beginning+n)].Name;
                LayoutRing(); RefreshSeats(); Publish();
            }
            Lesson.RevealDemonstration();
            yield return new WaitForSecondsRealtime(.65f);
            yield return ReturnHome(); Lesson.AfterDemonstration(); busy=false; AlignStart();
        }
        IEnumerator ReturnHome()
        {
            Lesson.Dial.Home(); targetTurns=0;
            if(Lesson.Dial.ReducedMotion)turns=0;
            yield return new WaitForSecondsRealtime(.25f);
            turns=0; LayoutRing();
        }
        void ContinueLesson() { if(busy)return; Lesson.Continue(); AlignStart(); }
        void AlignStart() { targetTurns=Lesson.Dial.Selected; turns=targetTurns; LayoutRing(); Refresh(); }
        void ToggleMotion() { Lesson.Dial.ReducedMotion=!Lesson.Dial.ReducedMotion; if(Lesson.Dial.ReducedMotion) turns=targetTurns; Refresh(); }
        void Refresh()
        {
            message.text=Lesson.Message;
            destination.text=(dragging ? "Passing: " : "Framed: ")+Zodiac.Seats[Lesson.Dial.Selected].Name;
            start.text=Lesson.IsProblem ? "Start: "+Zodiac.Seats[Lesson.Dial.Start].Name : "Teaching sign: Taurus";
            count.text=Lesson.Dial.Counting ? "Count: "+Lesson.Dial.MovementCount : "";
            phase.text=Lesson.Phase==LessonPhase.Complete ? "Six seats lit, six still dark · The room is waking" :
                Lesson.Phase==LessonPhase.Paused ? "Paused for now · No Key yet" :
                Lesson.IsProblem ? "Help level "+Lesson.Dial.HintLevel+" · "+(Lesson.Phase==LessonPhase.Guided ? "Together" : Lesson.Phase==LessonPhase.Optional ? "Just for fun" : "On your own") : "Practice example";
            bool active=Lesson.IsProblem && Lesson.Dial.Active && !busy;
            back.gameObject.SetActive(Lesson.IsProblem); forward.gameObject.SetActive(Lesson.IsProblem); seal.gameObject.SetActive(Lesson.IsProblem);
            back.interactable=forward.interactable=seal.interactable=active;
            countButton.gameObject.SetActive(Lesson.IsProblem); countButton.interactable=active;
            next.gameObject.SetActive(Lesson.Phase==LessonPhase.Encounter || Lesson.Phase==LessonPhase.Rule || Lesson.Phase==LessonPhase.Transfer);
            optional.gameObject.SetActive(Lesson.Phase==LessonPhase.Complete && Lesson.KeyEarned);
            motionText.text="Reduced motion: "+(Lesson.Dial.ReducedMotion ? "on" : "off");
            RefreshSeats(); LayoutRing(); Publish();
        }
        void RefreshSeats()
        {
            for(int i=0;i<12;i++)
            {
                bool selected=Lesson.Dial.Selected==i;
                seatTexts[i].text=(selected && Lesson.Dial.Rejected ? "× " : "")+Zodiac.Seats[i].Name+(Lesson.Lit[i] ? "\n"+Zodiac.Seats[i].Element : "");
                seats[i].GetComponent<Image>().color=Lesson.Lit[i] ? new Color(.29f,.27f,.28f) : new Color(.13f,.13f,.15f);
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
            var state=new WebState {message=message.text,destination=destination.text,start=start.text,phase=phase.text,count=count.text,seats=labels,
                active=Lesson.Dial.Active && !busy,canContinue=next.gameObject.activeSelf,canOptional=optional.gameObject.activeSelf,
                reducedMotion=Lesson.Dial.ReducedMotion,keyEarned=Lesson.KeyEarned};
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
