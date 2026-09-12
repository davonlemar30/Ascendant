using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            GreyboxPlayValidation.SetSize(390,844);SessionState.SetBool("AscendantSlicePlayValidation",true);
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
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Chamber,"atrium return leads to the chamber");Act("insert");Check(!View.Flow.KeyInserted,"insert waits for Caspar to finish");for(int i=1;i<SliceView.ChamberPages.Length;i++)Act("next-screen");Capture("slice-390-chamber.png");});
            Steps.Enqueue(()=>{Act("insert");});
            Steps.Enqueue(()=>{Check(View.Flow.KeyInserted && View.Flow.LocksFilled==1 && View.Flow.Ended && !View.Busy,"one key fills one of three locks and ends the prototype");Capture("slice-390-chamber-end.png");});
            Steps.Enqueue(()=>{GreyboxPlayValidation.SetSize(360,800);});
            Steps.Enqueue(()=>{
                Check(UnityEngine.Object.FindFirstObjectByType<Canvas>().pixelRect.size==new Vector2(360,800),"small portrait viewport for the chamber");Capture("slice-360-chamber-end.png");
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
