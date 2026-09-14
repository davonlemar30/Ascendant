using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ascendant.CelestialDial;

namespace Ascendant.Build
{
    // Scripted Play Mode pass through the whole v0.1 slice: identity, birth prompt, atrium, Dial, key reveal,
    // atrium return, chamber ending. Captures each screen. Not a human playtest.
    [InitializeOnLoad]
    public static class SlicePlayValidation
    {
        static readonly Queue<Action> Steps=new Queue<Action>();
        static readonly List<string> Report=new List<string>();
        static readonly List<string> RuntimeErrors=new List<string>();
        static double nextAt;
        const string ReportPath="Logs/slice-play-validation.txt";
        static SliceView View => UnityEngine.Object.FindFirstObjectByType<SliceView>();
        static SlicePlayValidation()
        {
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.EnteredEditMode && Environment.GetCommandLineArgs().Contains("-sliceAutoExit")) EditorApplication.Exit(File.Exists(ReportPath) && File.ReadAllText(ReportPath).StartsWith("PASS") ? 0 : 1);
                if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("AscendantSlicePlayValidation",false)) QueueChecks();
            };
        }
        [MenuItem("Ascendant/Greybox/Run vertical slice Play Mode validation")]
        public static void Begin()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/VerticalSlice.unity");
            PlayerPrefs.DeleteKey(SliceView.SaveKey);PlayerPrefs.Save(); // a stale local save from an earlier run would resume at the Hub
            int scale=Environment.GetCommandLineArgs().Contains("-sliceScale2") ? 2 : 1; // phone pixel density: the same layout at twice the pixels
            GreyboxPlayValidation.SetSize(390*scale,844*scale);SessionState.SetBool("AscendantSlicePlayValidation",true);
            EditorApplication.EnterPlaymode();
        }
        static void Check(bool value,string text) { if(!value)throw new Exception(text);Report.Add("PASS: "+text); }
        static void Act(string command) => View.Dial.WebAction(command);
        static void QueueChecks()
        {
            Report.Clear();Steps.Clear();RuntimeErrors.Clear();Application.logMessageReceived+=CaptureLog;
            Steps.Enqueue(()=>{Check(View!=null && View.Flow.Screen==SliceScreen.Identity,"slice opens on the identity screen");Capture("slice-390-identity.png");});
            Steps.Enqueue(()=>{Act("name:Tester");Check(View.Flow.DisplayName=="Tester","name reaches the flow through the bridge");Act("next-screen");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Birth,"birth prompt follows identity");Capture("slice-390-birth.png");});
            Steps.Enqueue(()=>{Act("next-screen");Check(View.Flow.Screen==SliceScreen.Birth,"birth prompt blocks continue until a choice");Act("birth:unknown");Check(View.Flow.HasSunSign && View.Flow.Note.Contains("choose one for you"),"I don't know assigns a sun sign");Act("birth:");Check(!View.Flow.HasSunSign,"the answer can be changed");Act("birth:known");Act("sign:1");Check(View.Flow.SunSign==1 && View.Flow.Note.Contains("Taurus"),"a known sign is accepted");Act("next-screen");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Atrium && !View.Busy,"white light leads to the atrium");Capture("slice-390-atrium.png");});
            Steps.Enqueue(()=>{for(int i=0;i<SliceView.AtriumPages.Length;i++)Act("next-screen");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.UiCanvas.gameObject.activeSelf && View.Dial.Lesson.DialDormant && View.Dial.Lesson.Sun==1,"atrium continues into the wing; the Dial is dormant and knows the sun sign");Capture("slice-390-wing.png");});
            for(int i=0;i<7;i++) Steps.Enqueue(()=>Act("continue"));
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.Phase==LessonPhase.Guided && !View.Dial.Lesson.DialDormant,"seven intro beats reach the guided problem");Capture("slice-390-wing-guided.png");});
            Steps.Enqueue(()=>{Act("seat:5");Act("seal");});
            Steps.Enqueue(()=>{Act("seat:9");Act("seal");});
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.Phase==LessonPhase.Transfer,"guided family completes inside the slice");Act("continue");for(int i=0;i<4;i++)Act("keyboard-forward");Act("seal");});
            Steps.Enqueue(()=>{Act("seat:8");Act("seal");});
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.KeyEarned,"six-seat lesson earns Key 1 inside the slice");Check(View.Dial.Lesson.Message.StartsWith("Two of four") || View.Flow.KeyRevealed,"locked completion line precedes the reveal");});
            Steps.Enqueue(()=>{Check(View.Flow.KeyRevealed && !View.Busy,"the Dial reveals the Key after completion");Check(View.Dial.Lesson.Message.StartsWith("Aah"),"Caspar's reveal line follows the locked line");Capture("slice-390-wing-reveal.png");});
            Steps.Enqueue(()=>{Act("next-screen");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.AtriumReturn,"wing continues to the atrium return");Capture("slice-390-atrium-return.png");});
            Steps.Enqueue(()=>{for(int i=0;i<SliceView.ReturnPages.Length;i++)Act("next-screen");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Chamber,"atrium return leads to the chamber");Act("insert");Check(!View.Flow.KeyInserted,"insert waits for Caspar to finish");Check(View.ChamberPaging,"a visible Continue turns the pages and Insert is not yet shown");for(int i=1;i<SliceView.ChamberPages.Length;i++)Act("next-screen");Check(!View.ChamberPaging && View.Flow.Screen==SliceScreen.Chamber,"on the last page Continue gives way to Insert the Key");Capture("slice-390-chamber.png");});
            Steps.Enqueue(()=>{Act("insert");});
            Steps.Enqueue(()=>{Check(View.Flow.KeyInserted && View.Flow.LocksFilled==1 && View.Flow.Ended && !View.Busy,"one key fills one of three locks and ends the prototype");Capture("slice-390-chamber-end.png");});
            // ---- v0.2: the return ----
            Steps.Enqueue(()=>{Act("next-screen");Check(View.Flow.Screen==SliceScreen.Hub && View.Flow.AtriumStage==2 && View.Flow.DueCount>=6 && View.Flow.DueCount==12-View.Flow.Deck.Practicing && UnityEngine.PlayerPrefs.HasKey(SliceView.SaveKey),"the Chamber leads to the Hub in Stage 2 with a save written and a batch of seals ready at once");Check(View.Flow.Walk.Room==Room.Atrium && View.Flow.Walk.At=="entry","the marker stands where you came in");Capture("slice-390-hub.png");Act("walk:sealed-left");Check(View.Flow.Note.StartsWith("Sealed") && !View.Busy,"a sealed door only says it is sealed");});
            Steps.Enqueue(()=>{Act("enter-seals");Check(View.Flow.Walk.TargetId=="desk","Check the Seals walks to the desk first");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Review && View.Flow.ReviewQueue.Count==6 && View.Dial.Lesson.Phase==LessonPhase.Review && View.Dial.UiCanvas.gameObject.activeSelf,"the desk opens Check the Seals at once, no waiting for a day, with a compressed Dial item");Capture("slice-390-review-dial.png");});
            for(int i=0;i<6;i++) Steps.Enqueue(()=>{var task=View.Flow.CurrentReview;Check(task!=null && !task.done,"a review item is waiting");if(task.Mode==ReviewMode.Dial){Act("seat:"+Zodiac.Destination(task.seat));Act("seal");}else{if(View.Flow.ReviewIndex==1)Capture("slice-390-review-tap.png");Act("element:"+System.Array.IndexOf(new[]{"Fire","Earth","Air","Water"},Zodiac.Seats[task.seat].Element));}});
            Steps.Enqueue(()=>{Check(View.Flow.ReviewDone && View.Flow.ReviewSummary.StartsWith("6 of 6") && View.Flow.Deck.Practicing>=3,"six seals held; eligible answers advance their items");Capture("slice-390-review-done.png");Act("leave-review");Check(View.Flow.Screen==SliceScreen.Hub,"back to the Hub after the review");});
            Steps.Enqueue(()=>{Act("walk:wing-door");Check(View.Flow.Walk.TargetId=="wing-door","tapping the Wing doorway walks the marker");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && View.Flow.Walk.Room==Room.Wing && View.Flow.Walk.At=="atrium-door" && !View.Busy,"the doorway fades into the Wing room at its doorway");Capture("slice-390-wing-room.png");Act("enter-dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Flow.Walk.At=="dial" && View.Dial.Lesson.Phase==LessonPhase.Continuation && View.Dial.Lesson.Dial.Start==2 && View.Dial.Lesson.Dial.HintLevel==0,"the Wing continues with the third family on the player's own");Capture("slice-390-wing-unit11.png");});
            for(int i=0;i<4;i++) Steps.Enqueue(()=>{Act("seat:"+Zodiac.Destination(View.Dial.Lesson.Dial.Start));Act("seal");});
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.Phase==LessonPhase.AllLit && View.Flow.WheelComplete && View.Dial.Lesson.Lit.All(v=>v) && !View.Dial.Lesson.KeyEarned==false && View.Dial.Lesson.Dial.Events.Count(e=>e.event_name=="key1_earned")==1,"twelve seats lit with no second Key");Capture("slice-390-wing-lit.png");Act("leave-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && View.Flow.AtriumStage==3 && View.Flow.V02Complete,"one more return completes v0.2");Capture("slice-390-hub-complete.png");});
            // ---- v0.3: glyphs and Key 2 ----
            Steps.Enqueue(()=>Act("enter-wing"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy && View.Flow.CanOpenBook,"the Wing button walks through the same doorway; the lit wheel wakes the shelf");Capture("slice-390-wing-room-lit.png");Act("walk:dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Lesson.Phase!=LessonPhase.GlyphNames && View.Dial.Lesson.Message==DialLesson.ShelfFirst,"the Dial before the book only points at the shelf");Act("leave-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy,"back out to the Atrium");Act("enter-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy,"and in again");Act("walk:shelf");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Book && View.Dial.Lesson.Phase==LessonPhase.GlyphNames && View.Flow.GlyphsStarted && View.Flow.Deck.Items.Count(i=>i.Kind==ItemKind.Glyph && i.entered)==12,"the shelf opens the book: Part A, and introduces the symbol items");Capture("slice-390-glyphs-a.png");});
            for(int i=0;i<14;i++) Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(l.Phase!=LessonPhase.GlyphNames)return;int target=l.CurrentGlyph;int slot=System.Array.IndexOf(l.GlyphOptions(target),target);if(l.GlyphIndex==2)Act("glyph-name:"+((slot+1)%4));Act("glyph-name:"+slot);});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && View.Dial.Lesson.Phase==LessonPhase.GlyphWheel && !View.Busy,"the twelfth name closes the book and returns to the room");Act("walk:dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Lesson.Phase==LessonPhase.GlyphWheel && View.Dial.Lesson.NamesHidden && View.Dial.Lesson.Dial.Target==0,"the Dial runs Part B: names hidden, Aries first");Capture("slice-390-glyphs-b.png");});
            bool missedOnce=false; // one deliberate miss on the fourth mark; a second would climb the ladder and skip the placement
            for(int i=0;i<12;i++) Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(l.Phase!=LessonPhase.GlyphWheel)return;int target=l.Dial.Target;if(l.GlyphIndex==3 && !missedOnce){missedOnce=true;Act("seat:"+Zodiac.Wrap(target+2));Act("seal");return;}Act("seat:"+target);Act("seal");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(l.Phase==LessonPhase.GlyphWheel){Act("seat:"+l.Dial.Target);Act("seal");}});
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.Key2Earned && View.Flow.Keys==2 && View.Dial.Lesson.Dial.Events.Count(e=>e.event_name=="key2_earned")==1,"twelve marks placed earns Key 2 once");Capture("slice-390-key2.png");});
            Steps.Enqueue(()=>Act("leave-wing"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && View.Flow.AtriumStage==4 && View.Flow.V03Complete,"one more return completes v0.3");Capture("slice-390-hub-v03.png");Act("enter-seals");});
            for(int i=0;i<6;i++) Steps.Enqueue(()=>{var task=View.Flow.CurrentReview;if(task==null||task.done)return;if(task.Mode==ReviewMode.Dial){Act("seat:"+Zodiac.Destination(task.seat));Act("seal");}else if(task.Mode==ReviewMode.Tap){Act("element:"+System.Array.IndexOf(new[]{"Fire","Earth","Air","Water"},Zodiac.Seats[task.seat].Element));}else{int slot=System.Array.IndexOf(View.Flow.GlyphReviewOptions(task.seat),task.seat);if(View.Flow.ReviewIndex==0)Capture("slice-390-review-glyph.png");Act("glyph-name:"+slot);}});
            Steps.Enqueue(()=>{Check(View.Flow.ReviewDone,"a review batch with the deck open completes");Act("leave-review");Act("reload");});
            Steps.Enqueue(()=>{Check(View!=null && View.Resumed && View.Flow.Screen==SliceScreen.Hub && View.Flow.WheelComplete && View.Flow.AtriumStage==4 && View.Flow.Keys==2 && View.Dial.Lesson.Key2Earned && View.Flow.Walk.At=="entry","a reload resumes at the Hub from the local save with Key 2");Act("restart");});
            Steps.Enqueue(()=>{Check(View!=null && !View.Resumed && View.Flow.Screen==SliceScreen.Identity && !UnityEngine.PlayerPrefs.HasKey(SliceView.SaveKey),"Start over wipes the save and begins again");GreyboxPlayValidation.SetSize(360,800);});
            Steps.Enqueue(()=>{
                Check(UnityEngine.Object.FindFirstObjectByType<Canvas>().pixelRect.size==new Vector2(360,800),"small portrait viewport");Capture("slice-360-identity.png");
                Check(RuntimeErrors.Count==0,"no runtime errors: "+string.Join("; ",RuntimeErrors));
                Application.logMessageReceived-=CaptureLog;
                Directory.CreateDirectory("Logs");File.WriteAllLines(ReportPath,Report);
                Debug.Log("[SlicePlayValidation] PASS: "+Report.Count+" checks.");
                SessionState.SetBool("AscendantSlicePlayValidation",false);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
            });
            nextAt=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;
        }
        static void CaptureLog(string message,string stack,LogType type)
        { if((type==LogType.Error || type==LogType.Exception) && !stack.Contains("UnityEditor.Search.SearchDatabase")) RuntimeErrors.Add(message); }
        static void Tick()
        {
            if(EditorApplication.isPaused){Debug.LogWarning("[SlicePlayValidation] Resuming paused Editor for fixture.");EditorApplication.isPaused=false;}
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup<nextAt || Steps.Count==0)return;
            var view=View; if(view!=null && (view.Busy || view.Dial.Busy))return; // Beats own the frame.
            nextAt=EditorApplication.timeSinceStartup+4;
            try{Steps.Dequeue()();}
            catch(Exception e)
            {
                Debug.LogException(e);Directory.CreateDirectory("Logs");File.WriteAllText(ReportPath,"FAIL: "+e);
                SessionState.SetBool("AscendantSlicePlayValidation",false);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
            }
        }
        static void Capture(string file) { Directory.CreateDirectory("Logs/Evidence");ScreenCapture.CaptureScreenshot("Logs/Evidence/"+file); }
    }
}
